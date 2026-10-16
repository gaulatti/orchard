using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using VirtualKey = Windows.System.VirtualKey;

namespace Orchard;

internal sealed partial class BleeckerSelectSurface : ItemsControl
{
    private readonly Func<bool> updating;
    private readonly Func<object?> currentSource;
    private readonly Func<string> displayText;
    private readonly Func<BleeckerSelectItem, bool> selectionMatches;
    private readonly string listboxName, automationClassName;
    internal readonly ResourceDictionary tokens = new() { Source = new Uri("ms-appx:///Orchard/Themes/Tokens.g.xaml") };
    internal readonly ResourceDictionary palette = new() { Source = new Uri("ms-appx:///Orchard/Themes/DataTablePaginationPalette.xaml") };
    private Popup? positionedPopup;
    private ContentPresenter? displayedPresenter;
    private Border? trigger, ring;
    private BleeckerSelectItem? selected;
    private bool pointerOver, openingFromKeyboard, restoreFocus;
    private bool initialFocus;
    private double? typeaheadOffset;
    private readonly DispatcherQueueTimer triggerSearchReset, contentSearchReset;
    private string triggerSearch = "", contentSearch = "", displayed = "";
    internal event EventHandler? SelectionChanged;

    internal BleeckerSelectSurface(Func<bool> updating, Func<object?> currentSource, Func<string> displayText, Func<BleeckerSelectItem, bool> selectionMatches, string listboxName, string automationClassName)
    {
        this.updating = updating; this.currentSource = currentSource; this.displayText = displayText; this.selectionMatches = selectionMatches; this.listboxName = listboxName; this.automationClassName = automationClassName; IsTabStop = true; UseSystemFocusVisuals = false;
        ItemsPanel = (ItemsPanelTemplate)XamlReader.Load("<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel /></ItemsPanelTemplate>");
        Template = (ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ItemsControl">
             <Grid UseLayoutRounding="False">
              <Border x:Name="Ring" Margin="-2" BorderThickness="2" CornerRadius="9" IsHitTestVisible="False" Visibility="Collapsed" />
              <Border x:Name="Trigger" BorderThickness="1" CornerRadius="{StaticResource BleeckerUiCornerRadius}" Control.IsTemplateFocusTarget="True">
               <Grid Margin="{TemplateBinding Padding}"><Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="12"/><ColumnDefinition Width="17"/></Grid.ColumnDefinitions>
                <ContentPresenter x:Name="ContentPresenter" VerticalAlignment="Center" Foreground="{TemplateBinding Foreground}" FontFamily="{TemplateBinding FontFamily}" FontSize="{TemplateBinding FontSize}"/>
                <Viewbox Grid.Column="2" Width="17" Height="17" IsHitTestVisible="False"><Canvas Width="24" Height="24"><Path Data="M6 9L12 15L18 9" Stroke="{ThemeResource BleeckerTextSecondaryBrush}" StrokeThickness="2" StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round"/></Canvas></Viewbox>
               </Grid>
              </Border>
              <Popup x:Name="Popup" IsLightDismissEnabled="True" ShouldConstrainToRootBounds="True">
               <Border x:Name="PopupBorder" Background="{ThemeResource BleeckerCardBrush}" BorderBrush="{ThemeResource BleeckerPaginationChooserBorderBrush}" BorderThickness="1" CornerRadius="{StaticResource BleeckerUiCornerRadius}" UseLayoutRounding="False">
                <ScrollViewer x:Name="ScrollViewer" MaxHeight="288" HorizontalScrollMode="Disabled" HorizontalScrollBarVisibility="Disabled" VerticalScrollMode="Enabled" VerticalScrollBarVisibility="Hidden" ZoomMode="Disabled" IsTabStop="True" AutomationProperties.AccessibilityView="Raw"><ItemsPresenter Margin="4"/></ScrollViewer>
               </Border>
              </Popup>
             </Grid>
            </ControlTemplate>
            """);
        Items.VectorChanged += (_, _) => { if (IsLoaded) RefreshOpenValue(); };
        GotFocus += (_, _) => RenderChooser(); LostFocus += (_, _) => RenderChooser();
        ActualThemeChanged += (_, _) => RenderChooser();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { if (!IsEnabled) IsDropDownOpen = false; RenderChooser(); });
        triggerSearchReset = DispatcherQueue.CreateTimer(); triggerSearchReset.Interval = TimeSpan.FromSeconds(1); triggerSearchReset.IsRepeating = false; triggerSearchReset.Tick += (_, _) => triggerSearch = "";
        contentSearchReset = DispatcherQueue.CreateTimer(); contentSearchReset.Interval = TimeSpan.FromSeconds(1); contentSearchReset.IsRepeating = false; contentSearchReset.Tick += (_, _) => contentSearch = "";
        Unloaded += (_, _) => { restoreFocus = false; IsDropDownOpen = false; StopScroll(); triggerSearchReset.Stop(); contentSearchReset.Stop(); triggerSearch = contentSearch = ""; };
        CharacterReceived += Typeahead;
    }

    internal BleeckerSelectItem? SelectedItem
    {
        get => selected;
        set
        {
            if (ReferenceEquals(selected,value)) return;
            if (!updating() && (!IsLoaded || !IsEnabled)) return;
            if (!updating() && value is { IsEnabled: false }) return;
            if (value is not null && !Items.Contains(value)) throw new ArgumentException("The selected page size must belong to the current options.");
            var previous = selected; selected = value; previous?.NotifySelection(true,false); selected?.NotifySelection(false,true); RenderChooser(); SelectionChanged?.Invoke(this,EventArgs.Empty);
        }
    }
    internal bool IsDropDownOpen
    {
        get => positionedPopup?.IsOpen == true;
        set
        {
            if (positionedPopup is null) { if (value) throw new InvalidOperationException("The mounted page-size chooser requires its popup template."); return; }
            if (value && (!IsLoaded || !IsEnabled)) return;
            if (positionedPopup.IsOpen != value) positionedPopup.IsOpen = value;
        }
    }

    protected override void OnApplyTemplate()
    {
        DetachScrollChrome();
        if (positionedPopup is not null) { positionedPopup.Opened -= PopupOpened; positionedPopup.Closed -= PopupClosed; positionedPopup.IsOpen = false; }
        base.OnApplyTemplate();
        positionedPopup = GetTemplateChild("Popup") as Popup ?? throw new InvalidOperationException("The page-size template requires Popup.");
        positionedPopup.DesiredPlacement = PopupPlacementMode.BottomEdgeAlignedLeft; positionedPopup.VerticalOffset = 4;
        positionedPopup.Opened += PopupOpened; positionedPopup.Closed += PopupClosed;
        trigger = (Border)GetTemplateChild("Trigger"); ring = (Border)GetTemplateChild("Ring"); displayedPresenter = (ContentPresenter)GetTemplateChild("ContentPresenter");
        positionedPopup.PlacementTarget = trigger;
        var border = (Border)GetTemplateChild("PopupBorder"); border.Width = double.IsNaN(Width) ? ActualWidth : Width;
        var scroll = (ScrollViewer)GetTemplateChild("ScrollViewer"); scroll.BringIntoViewOnFocusChange = false; scroll.IsTabStop = false; border.Child = null;
        var focusHost = new ChooserViewport(this) { Name = "PART_Listbox", Content = scroll, IsTabStop = true, UseSystemFocusVisuals = false, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        AutomationProperties.SetName(focusHost,listboxName); border.Child = focusHost; AttachScrollChrome(border,scroll,focusHost);
        border.KeyDown += (_, args) => HandleOpenKey(args); border.CharacterReceived += Typeahead;
        RefreshOpenValue();
    }
    private void PopupOpened(object? sender, object args)
    {
        ((Border)positionedPopup!.Child).Width = ActualWidth;
        triggerSearch = contentSearch = ""; triggerSearchReset.Stop(); contentSearchReset.Stop(); typeaheadOffset = null;
        scrollInitialized = false; positionedPopup!.Child.UpdateLayout(); InitializeScrollIndicators(); RenderChooser(); NotifyExpansion(false,true);
        void FocusInitial()
        {
            if (!IsDropDownOpen) return;
            var option = selected is { IsEnabled: true } ? selected : Items.OfType<BleeckerSelectItem>().FirstOrDefault(item => item.IsEnabled);
            initialFocus = true;
            try
            {
                if (option is not null && !option.Focus(openingFromKeyboard ? FocusState.Keyboard : FocusState.Pointer)) throw new InvalidOperationException("The opened page-size chooser could not focus its option.");
                if (option is null && !popupFocus!.Focus(openingFromKeyboard ? FocusState.Keyboard : FocusState.Pointer)) throw new InvalidOperationException("The empty page-size chooser could not focus its viewport.");
            }
            finally { initialFocus = false; }
            QueueOpenPosition();
        }
        if (Items.OfType<BleeckerSelectItem>().Any(item => item.IsLoaded)) FocusInitial();
        else if (!DispatcherQueue.TryEnqueue(FocusInitial)) throw new InvalidOperationException("The opened chooser could not schedule option focus.");
    }
    private void PopupClosed(object? sender, object args)
    { StopScroll(); contentSearch = ""; contentSearchReset.Stop(); typeaheadOffset = null; if (restoreFocus && IsLoaded && IsEnabled) Focus(FocusState.Programmatic); restoreFocus = false; RenderChooser(); NotifyExpansion(true,false); }
    private void Close(bool focus) { restoreFocus = focus; IsDropDownOpen = false; }
    protected override void OnPointerEntered(PointerRoutedEventArgs args) { base.OnPointerEntered(args); pointerOver = true; RenderChooser(); }
    protected override void OnPointerExited(PointerRoutedEventArgs args) { base.OnPointerExited(args); pointerOver = false; RenderChooser(); }
    protected override void OnPointerPressed(PointerRoutedEventArgs args)
    {
        base.OnPointerPressed(args);
        if (!IsEnabled || !args.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        // Keep this genuine trigger press/release on its native owner while
        // opening a new popup input site and moving keyboard focus into it.
        if (!CapturePointer(args.Pointer)) throw new InvalidOperationException("The Select trigger could not retain its original pointer press.");
        Focus(FocusState.Pointer); openingFromKeyboard = false; IsDropDownOpen = !IsDropDownOpen; args.Handled = true;
    }
    protected override void OnPointerReleased(PointerRoutedEventArgs args)
    { ReleasePointerCapture(args.Pointer); base.OnPointerReleased(args); }
    protected override void OnPointerCanceled(PointerRoutedEventArgs args)
    { ReleasePointerCapture(args.Pointer); base.OnPointerCanceled(args); }
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        if (IsDropDownOpen) { HandleOpenKey(args); if (args.Handled) return; }
        else if (args.Key == VirtualKey.Space && triggerSearch.Length > 0) { args.Handled = true; return; }
        else if (IsEnabled && args.Key is VirtualKey.Down or VirtualKey.Up or VirtualKey.Enter or VirtualKey.Space)
        { openingFromKeyboard = true; IsDropDownOpen = true; args.Handled = true; return; }
        base.OnKeyDown(args);
    }
    internal void HandleOpenKey(KeyRoutedEventArgs args)
    {
        if (args.Handled || !IsDropDownOpen) return;
        if (args.Key == VirtualKey.Tab) { args.Handled = true; return; }
        if (args.Key == VirtualKey.Space && contentSearch.Length > 0) { args.Handled = true; return; }
        if (args.Key == VirtualKey.Escape)
        {
            args.Handled = true;
            if (!DispatcherQueue.TryEnqueue(() => { if (IsLoaded) Close(true); })) throw new InvalidOperationException("The page-size chooser could not finish keyboard dismissal.");
            return;
        }
        var options = Items.OfType<BleeckerSelectItem>().Where(item => item.IsEnabled).ToArray(); var focused = FocusManager.GetFocusedElement(XamlRoot) as BleeckerSelectItem; var index = Array.IndexOf(options,focused);
        var next = args.Key switch { VirtualKey.Down => Math.Min(options.Length-1,index+1), VirtualKey.Up => index < 0 ? options.Length-1 : Math.Max(0,index-1), VirtualKey.Home => 0, VirtualKey.End => options.Length-1, _ => -1 };
        if (next >= 0 && next < options.Length) { typeaheadOffset = null; options[next].Focus(FocusState.Keyboard); args.Handled = true; }
        else if (args.Key is VirtualKey.Enter or VirtualKey.Space && focused is not null) { QueueCommit(focused); args.Handled = true; }
    }
    internal void QueueCommit(BleeckerSelectItem option)
    {
        // Finish the genuine routed input event before detaching its popup.
        // The normal held-key/pointer observers must see their original node.
        var source = currentSource();
        if (!DispatcherQueue.TryEnqueue(() => { if (ReferenceEquals(currentSource(),source)) Commit(option); })) throw new InvalidOperationException("The page-size chooser could not finish its selection request.");
    }
    internal void Commit(BleeckerSelectItem option)
    { if (!IsEnabled || !IsLoaded || !option.IsEnabled || !Items.Contains(option)) return; SelectedItem = option; Close(true); }
    internal bool SelectionMatches(BleeckerSelectItem item) => selectionMatches(item);
    internal void RefreshOpenValue()
    {
        if (displayedPresenter is null) return;
        var value = displayText();
        var label = new TextBlock { Text = value, FontFamily = FontFamily, FontSize = FontSize, LineHeight = 20, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextTrimming = TextTrimming.CharacterEllipsis, IsHitTestVisible = false }; AutomationProperties.SetAccessibilityView(label,AccessibilityView.Raw); displayedPresenter.Content = label;
        if (displayed != value) { var old = displayed; displayed = value; FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty,old,value); }
        RenderChooser();
    }
    internal Brush Brush(ResourceDictionary dictionary, string key)
    {
        var theme = new Windows.UI.ViewManagement.AccessibilitySettings().HighContrast ? "HighContrast" : ActualTheme == ElementTheme.Light ? "Light" : "Default";
        return (Brush)((ResourceDictionary)dictionary.ThemeDictionaries[theme])[key];
    }
    private void RenderChooser()
    {
        if (trigger is null || ring is null) return;
        var focused = FocusState != FocusState.Unfocused || IsDropDownOpen;
        trigger.Background = Brush(palette,IsEnabled ? "BleeckerPaginationChooserBackgroundBrush" : "BleeckerPaginationChooserDisabledBrush");
        trigger.BorderBrush = Brush(palette,focused ? "BleeckerPaginationChooserFocusBrush" : pointerOver ? "BleeckerPaginationChooserHoverBrush" : "BleeckerPaginationChooserBorderBrush");
        ring.BorderBrush = Brush(palette,"BleeckerPaginationChooserRingBrush"); ring.Visibility = focused ? Visibility.Visible : Visibility.Collapsed; trigger.Opacity = IsEnabled ? 1 : .6;
        foreach (var item in Items.OfType<BleeckerSelectItem>()) item.Render();
    }
    internal void QueueOpenPosition()
    {
        if (!IsDropDownOpen || positionedPopup is null) return;
        ((Border)positionedPopup.Child).Width = ActualWidth;
        positionedPopup.Child.UpdateLayout(); InitializeScrollIndicators();
        var popupTop = positionedPopup.Child.TransformToVisual(XamlRoot.Content).TransformPoint(new Point()).Y;
        var triggerTop = TransformToVisual(XamlRoot.Content).TransformPoint(new Point()).Y;
        var offset = popupTop < triggerTop ? -4d : 4d;
        if (positionedPopup.VerticalOffset != offset) { positionedPopup.VerticalOffset = offset; positionedPopup.Child.UpdateLayout(); }
        if (FocusManager.GetFocusedElement(XamlRoot) is BleeckerSelectItem focused && Items.Contains(focused) && popupScroll is { } scroll)
        {
            if (typeaheadOffset is { } wanted) { if (Math.Abs(scroll.VerticalOffset-wanted) > 1d/64) scroll.ChangeView(null,wanted,null,true); else typeaheadOffset = null; return; }
            if (focused.FocusState == FocusState.Pointer && !initialFocus) return;
            var index = Items.IndexOf(focused);
            // Radix's focusFirst includes viewport padding when focusing
            // its first or last option, then uses nearest for other rows.
            if (index == 0) { if (scroll.VerticalOffset != 0) scroll.ChangeView(null,0,null,true); return; }
            if (index == Items.Count-1) { if (scroll.VerticalOffset != scroll.ScrollableHeight) scroll.ChangeView(null,scroll.ScrollableHeight,null,true); return; }
            var bounds = focused.TransformToVisual(scroll).TransformBounds(new Rect(0,0,focused.ActualWidth,focused.ActualHeight));
            if (bounds.Top < 0) scroll.ChangeView(null,Math.Max(0,scroll.VerticalOffset+bounds.Top),null,true);
            else if (bounds.Bottom > scroll.ActualHeight) scroll.ChangeView(null,scroll.VerticalOffset+bounds.Bottom-scroll.ActualHeight,null,true);
        }
    }
    private void Typeahead(UIElement sender, CharacterReceivedRoutedEventArgs args)
    {
        if (args.Handled || !IsEnabled || char.IsControl(args.Character)) return;
        var search = IsDropDownOpen ? contentSearch : triggerSearch;
        if (args.Character == ' ' && search.Length == 0) { args.Handled = true; return; }
        search += args.Character;
        if (IsDropDownOpen) { contentSearch = search; contentSearchReset.Stop(); contentSearchReset.Start(); } else { triggerSearch = search; triggerSearchReset.Stop(); triggerSearchReset.Start(); }
        var text = search.All(character => character == search[0]) ? search[..1] : search;
        var options = Items.OfType<BleeckerSelectItem>().Where(item => item.IsEnabled).ToArray(); var current = IsDropDownOpen ? FocusManager.GetFocusedElement(XamlRoot) as BleeckerSelectItem : selected; var start = Array.IndexOf(options,current);
        var wrappedStart = Math.Max(0,start); var ordered = options.Skip(wrappedStart).Concat(options.Take(wrappedStart)); if (text.Length == 1) ordered = ordered.Where(item => !ReferenceEquals(item,current));
        var target = ordered.FirstOrDefault(item => item.Content?.ToString()?.StartsWith(text,StringComparison.OrdinalIgnoreCase) == true);
        if (target is not null && !ReferenceEquals(target,current))
        {
            if (IsDropDownOpen)
            {
                var viewport = popupScroll ?? throw new InvalidOperationException("The opened chooser requires its native scroll viewport.");
                var bounds = target.TransformToVisual(viewport).TransformBounds(new Rect(0,0,target.ActualWidth,target.ActualHeight));
                if (bounds.Top < 0 || bounds.Bottom > viewport.ActualHeight) typeaheadOffset = Math.Clamp(viewport.VerticalOffset+(bounds.Top+bounds.Bottom-viewport.ActualHeight)/2,0,viewport.ScrollableHeight);
                target.Focus(FocusState.Keyboard);
            }
            else SelectedItem = target;
        }
        args.Handled = true;
    }
    private void NotifyExpansion(bool before, bool after) => FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,before?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed,after?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed);
    private sealed class ChooserViewport(BleeckerSelectSurface chooser) : ContentControl
    {
        protected override void OnKeyDown(KeyRoutedEventArgs args) { chooser.HandleOpenKey(args); if (!args.Handled) base.OnKeyDown(args); }
        protected override AutomationPeer OnCreateAutomationPeer() => new ViewportPeer(this);
        private sealed class ViewportPeer(ChooserViewport viewport) : FrameworkElementAutomationPeer(viewport)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Scroll && viewport.Content is ScrollViewer scroll ? CreatePeerForElement(scroll).GetPattern(pattern) : base.GetPatternCore(pattern);
        }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ChooserPeer(this);
    private sealed class ChooserPeer(BleeckerSelectSurface chooser) : FrameworkElementAutomationPeer(chooser), IExpandCollapseProvider, ISelectionProvider, IValueProvider
    {
        protected override string GetClassNameCore() => chooser.automationClassName;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;
        protected override object GetPatternCore(PatternInterface pattern) => pattern is PatternInterface.ExpandCollapse or PatternInterface.Selection or PatternInterface.Value ? this : base.GetPatternCore(pattern);
        public ExpandCollapseState ExpandCollapseState => chooser.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public void Expand() { if (!chooser.IsEnabled) throw new ElementNotEnabledException(); if (!chooser.IsLoaded) throw new ElementNotAvailableException(); chooser.openingFromKeyboard = true; chooser.IsDropDownOpen = true; }
        public void Collapse() { if (!chooser.IsEnabled) throw new ElementNotEnabledException(); chooser.Close(true); }
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IRawElementProviderSimple[] GetSelection() => chooser.selected is { } item ? [ProviderFromPeer(CreatePeerForElement(item))] : [];
        public bool IsReadOnly => true;
        public string Value => chooser.displayText();
        public void SetValue(string value) => throw new InvalidOperationException("Page size is selected from the consumer's options.");
    }
}

internal sealed class BleeckerSelectItem : Button
{
    internal BleeckerSelectSurface Chooser { get; }
    internal BleeckerSelectItem(BleeckerSelectSurface chooser)
    {
        Chooser = chooser; IsTabStop = true; UseSystemFocusVisuals = false; BorderThickness = new Thickness(0); Padding = new Thickness(12,6,36,6); HorizontalContentAlignment = HorizontalAlignment.Left; HorizontalAlignment = HorizontalAlignment.Stretch; MinWidth = 0; UseLayoutRounding = false;
        Template = (ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="Button">
             <Grid Background="{TemplateBinding Background}" CornerRadius="5" Padding="{TemplateBinding Padding}" UseLayoutRounding="False"><ContentPresenter Content="{TemplateBinding Content}" Foreground="{TemplateBinding Foreground}" FontFamily="{TemplateBinding FontFamily}" FontSize="{TemplateBinding FontSize}" VerticalAlignment="Center"/><Viewbox x:Name="Checkmark" Width="15" Height="15" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,-24,0" IsHitTestVisible="False"><Canvas Width="24" Height="24"><Path Data="M20 6L9 17L4 12" Stroke="{TemplateBinding Foreground}" StrokeThickness="2" StrokeStartLineCap="Round" StrokeEndLineCap="Round" StrokeLineJoin="Round"/></Canvas></Viewbox></Grid>
            </ControlTemplate>
            """);
        Click += (_, _) => Chooser.QueueCommit(this); GotFocus += (_, _) => { Render(); if (Chooser.IsDropDownOpen) Chooser.QueueOpenPosition(); }; LostFocus += (_, _) => Render();
        PointerMoved += (_, args) => { if (!args.IsGenerated && Chooser.IsDropDownOpen) Focus(FocusState.Pointer); };
    }
    protected override void OnApplyTemplate() { base.OnApplyTemplate(); Render(); }
    protected override void OnKeyDown(KeyRoutedEventArgs args) { Chooser.HandleOpenKey(args); if (!args.Handled) base.OnKeyDown(args); }
    internal void Render()
    {
        FontFamily = Chooser.SelectionMatches(this) ? (FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"] : Chooser.FontFamily;
        Foreground = Chooser.Brush(Chooser.tokens,Chooser.SelectionMatches(this) ? "BleeckerSeaBrush" : "BleeckerTextPrimaryBrush");
        Background = Chooser.Brush(Chooser.palette,FocusState != FocusState.Unfocused ? "BleeckerPaginationOptionHighlightBrush" : "BleeckerPaginationOptionBackgroundBrush");
        Opacity = IsEnabled ? 1 : .4;
        if (GetTemplateChild("Checkmark") is FrameworkElement mark) mark.Visibility = Chooser.SelectionMatches(this) ? Visibility.Visible : Visibility.Collapsed;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new OptionPeer(this);
    internal void NotifySelection(bool before, bool after) => FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty,before,after);
    private sealed class OptionPeer(BleeckerSelectItem item) : ButtonAutomationPeer(item), ISelectionItemProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.SelectionItem ? this : base.GetPatternCore(pattern);
        public bool IsSelected => ReferenceEquals(item.Chooser.SelectedItem,item);
        public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(CreatePeerForElement(item.Chooser));
        public void Select() { if (!item.IsEnabled || !item.Chooser.IsEnabled) throw new ElementNotEnabledException(); if (!item.Chooser.IsLoaded || !item.Chooser.Items.Contains(item)) throw new ElementNotAvailableException(); item.Chooser.SelectedItem = item; }
        public void AddToSelection() => Select();
        public void RemoveFromSelection() => throw new InvalidOperationException("The consumer owns its controlled page size.");
    }
}
