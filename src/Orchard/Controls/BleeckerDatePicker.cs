using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A controlled local-date trigger and nonmodal calendar surface.</summary>
public sealed partial class BleeckerDatePicker : Control
{
    private Grid? host;
    private TriggerButton? trigger;
    private TextBlock? caption;
    private Popup? popup;
    private DateDialog? dialog;
    private BleeckerOverlaySurface? surface;
    private StackPanel? content;
    private BleeckerCalendar? calendar;
    private Button? clear;
    private readonly ResourceDictionary palette = BleeckerDateVisual.Palette();
    private readonly Func<DateTimeOffset> now;
    private bool initialFocus, keyboardOpening;
    internal bool RangeAppearance { get; set; }
    public BleeckerDatePicker() : this(() => DateTimeOffset.Now) { }
    internal BleeckerDatePicker(Func<DateTimeOffset> now)
    {
        this.now = now; DefaultStyleKey = typeof(BleeckerDatePicker);
        Unloaded += (_, _) => { if (!IsLoaded) Close(false); };
        ActualThemeChanged += (_, _) => { if (dialog is not null) dialog.RequestedTheme = ActualTheme; };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (!IsEnabled) Close(false); Refresh(); });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => Refresh());
        RegisterPropertyChangedCallback(FlowDirectionProperty, (_, _) => { if (dialog is not null) dialog.FlowDirection = FlowDirection; });
    }
    private static DependencyProperty Property(string name, Type type, object? initial = null) => DependencyProperty.Register(name, type, typeof(BleeckerDatePicker), new PropertyMetadata(initial, (owner, _) => ((BleeckerDatePicker)owner).Refresh()));
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value), typeof(object));
    public static readonly DependencyProperty MinDateProperty = Property(nameof(MinDate), typeof(object));
    public static readonly DependencyProperty MaxDateProperty = Property(nameof(MaxDate), typeof(object));
    public static readonly DependencyProperty DisabledDatesProperty = Property(nameof(DisabledDates), typeof(object));
    public static readonly DependencyProperty PlaceholderProperty = Property(nameof(Placeholder), typeof(string), "Pick a date");
    public DateTimeOffset? Value { get => (DateTimeOffset?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public DateTimeOffset? MinDate { get => (DateTimeOffset?)GetValue(MinDateProperty); set => SetValue(MinDateProperty, value); }
    public DateTimeOffset? MaxDate { get => (DateTimeOffset?)GetValue(MaxDateProperty); set => SetValue(MaxDateProperty, value); }
    public Func<DateTimeOffset, bool>? DisabledDates { get => (Func<DateTimeOffset, bool>?)GetValue(DisabledDatesProperty); set => SetValue(DisabledDatesProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(PlaceholderProperty, value); } }
    public bool IsDropDownOpen => popup?.IsOpen == true;
    public event EventHandler<BleeckerDateRequestedEventArgs>? DateRequested;
    internal Button Trigger => trigger ?? throw new InvalidOperationException("Date picker is not templated.");
    internal BleeckerCalendar? Calendar => calendar;
    internal Button? ClearButton => clear;
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        var measured = base.MeasureOverride(availableSize);
        return new(Math.Min(availableSize.Width, BleeckerDates.LayoutWidth(measured.Width)), measured.Height);
    }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        host = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Date picker requires PART_Root.");
        trigger = new TriggerButton(this) { Name = "PART_Trigger", Style = (Style)palette["BleeckerDateTriggerStyle"], HorizontalAlignment = HorizontalAlignment.Stretch, UseLayoutRounding = false };
        if (RangeAppearance) trigger.Style = (Style)palette["BleeckerRangeDateTriggerStyle"];
        trigger.SetBinding(FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
        var row = new Grid { ColumnSpacing = 10, UseLayoutRounding = false }; row.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new());
        var icon = (Viewbox)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='15' Height='15' IsHitTestVisible='False'><Canvas Width='24' Height='24'><Path Data='M8 2V6 M16 2V6 M3 10H21 M5 4H19A2 2 0 0 1 21 6V20A2 2 0 0 1 19 22H5A2 2 0 0 1 3 20V6A2 2 0 0 1 5 4 M8 14H8.01 M12 14H12.01 M16 14H16.01 M8 18H8.01 M12 18H12.01 M16 18H16.01' Stroke='{ThemeResource BleeckerSeaBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
        row.Children.Add(icon); caption = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, UseLayoutRounding = false };
        caption.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
        Grid.SetColumn(caption, 1); row.Children.Add(caption); trigger.Content = row; trigger.Click += TriggerClicked; host.Children.Add(trigger);
        Refresh();
    }
    private void TriggerClicked(object sender, RoutedEventArgs args) { if (IsDropDownOpen) Close(true); else Open(); }
    private void Refresh()
    {
        if (trigger is null || caption is null) return;
        caption.Text = Value is { } value ? BleeckerDates.Display(value) : Placeholder;
        caption.Style = (Style)palette[Value is null ? "BleeckerDateSecondaryTextStyle" : "BleeckerDatePrimaryTextStyle"];
        var name = AutomationProperties.GetName(this); AutomationProperties.SetName(trigger, string.IsNullOrEmpty(name) ? caption.Text : name);
        trigger.IsEnabled = IsEnabled;
        if (calendar is not null) { calendar.Value = Value; calendar.MinDate = MinDate; calendar.MaxDate = MaxDate; calendar.DisabledDates = DisabledDates; }
        if (dialog is not null) AutomationProperties.SetName(dialog, string.IsNullOrEmpty(name) ? Placeholder : name);
        if (clear is not null) clear.Visibility = Value is null ? Visibility.Collapsed : Visibility.Visible;
        trigger.RefreshState();
    }
    private void Open()
    {
        if (!IsLoaded || !IsEnabled || trigger is null || host is null || IsDropDownOpen) return;
        calendar = new BleeckerCalendar(now) { Value = Value, MinDate = MinDate, MaxDate = MaxDate, DisabledDates = DisabledDates, UseLayoutRounding = false };
        calendar.DateRequested += Requested;
        content = new StackPanel { UseLayoutRounding = false }; content.Children.Add(calendar);
        clear = new Button { Name = "PART_Clear", Content = "Clear date", Style = (Style)palette["BleeckerDateClearStyle"] }; clear.Click += ClearClicked; content.Children.Add(clear);
        surface = (BleeckerOverlaySurface)XamlReader.Load("<local:BleeckerOverlaySurface xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:local='using:Orchard' Width='288' Padding='16' BorderThickness='1' CornerRadius='7' Background='{ThemeResource BleeckerDateSurfaceBrush}' BorderBrush='{ThemeResource BleeckerDateSurfaceBorderBrush}' UseLayoutRounding='False'/>");
        surface.Resources.MergedDictionaries.Add(palette); surface.Content = content;
        dialog = new DateDialog { Name = "PART_Dialog", Content = surface, RequestedTheme = ActualTheme, FlowDirection = FlowDirection, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, UseLayoutRounding = false };
        popup = new Popup { Name = "PART_Popup", Child = dialog, XamlRoot = XamlRoot, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
        popup.Opened += Opened; popup.Closed += Closed; surface.SizeChanged += SurfaceChanged;
        host.Children.Add(popup); initialFocus = true; Refresh(); popup.IsOpen = true;
    }
    private void Requested(object? sender, BleeckerDateRequestedEventArgs args) { if (!ReferenceEquals(sender, calendar) || !IsLoaded || !IsEnabled) return; DateRequested?.Invoke(this, args); Close(true); }
    private void ClearClicked(object sender, RoutedEventArgs args) { if (!ReferenceEquals(sender, clear) || !IsLoaded || !IsEnabled || !IsDropDownOpen || Value is null) return; DateRequested?.Invoke(this, new(null)); Close(true); }
    private void Detach() { Close(false); if (trigger is not null) trigger.Click -= TriggerClicked; host?.Children.Clear(); trigger = null; caption = null; host = null; }
    private sealed class DateDialog : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
        private sealed class Peer(DateDialog owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
            protected override string GetClassNameCore() => "OrchardDatePopover";
            protected override string GetLocalizedControlTypeCore() => "dialog";
        }
    }
    private sealed class TriggerButton : Button
    {
        private readonly BleeckerDatePicker owner; private bool over;
        internal TriggerButton(BleeckerDatePicker owner)
        {
            this.owner = owner; PointerEntered += (_, _) => { over = true; RefreshState(); }; PointerExited += (_, _) => { over = false; RefreshState(); };
            RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => RefreshState());
            PointerPressed += (_, _) => owner.keyboardOpening = false;
            KeyDown += (_, args) => { if (args.Key is Windows.System.VirtualKey.Enter or Windows.System.VirtualKey.Space) owner.keyboardOpening = true; };
        }
        protected override void OnApplyTemplate() { base.OnApplyTemplate(); RefreshState(); }
        internal void RefreshState() => VisualStateManager.GoToState(this, !IsEnabled ? "TriggerDisabled" : owner.IsDropDownOpen || FocusState == FocusState.Keyboard ? "TriggerFocused" : over ? "TriggerHovered" : "TriggerRest", false);
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this, owner);
        private sealed class Peer(TriggerButton button, BleeckerDatePicker owner) : ButtonAutomationPeer(button), IExpandCollapseProvider
        {
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.ExpandCollapse ? this : base.GetPatternCore(pattern);
            public ExpandCollapseState ExpandCollapseState => owner.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            private void Guard() { if (!owner.IsEnabled) throw new ElementNotEnabledException(); if (!owner.IsLoaded || !button.IsLoaded || !ReferenceEquals(button, owner.trigger)) throw new ElementNotAvailableException(); }
            public void Expand() { Guard(); owner.keyboardOpening = true; owner.Open(); }
            public void Collapse() { Guard(); owner.Close(true); }
        }
    }
}
