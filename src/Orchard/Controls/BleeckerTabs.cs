using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

/// <summary>A consumer-controlled tab strip, with separate host-owned navigation links.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class BleeckerTabs : Control
{
    private readonly Dictionary<string, TabButton> buttons = new(StringComparer.Ordinal);
    private readonly HashSet<ICommand> commands = [];
    private ObservableCollection<BleeckerTabItem>? observed;
    private TabsPanel? panel;
    private Grid? root;
    private Border? frame;
    private BleeckerCssShadow? shadow;
    private readonly ResourceDictionary styles = new() { Source = new Uri("ms-appx:///Orchard/Themes/Tabs.xaml") };

    public BleeckerTabs()
    {
        DefaultStyleKey = typeof(BleeckerTabs);
        IsTabStop = false;
        TabFocusNavigation = KeyboardNavigationMode.Once;
        Items = [];
        AutomationProperties.SetName(this, "Tabs");
        Loaded += (_, _) => { Observe(); Reconcile(); };
        Unloaded += (_, _) => { if (!IsLoaded) StopObserving(); };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        GettingFocus += (_, args) =>
        {
            if (IsNavigation || args.FocusState != FocusState.Keyboard ||
                args.Direction is not (FocusNavigationDirection.Next or FocusNavigationDirection.Previous) ||
                args.NewFocusedElement is not TabButton target || !ReferenceEquals(target.Owner, this)) return;
            if (args.OldFocusedElement is TabButton previous && ReferenceEquals(previous.Owner, this)) return;
            if (Entry() is { } entry && !ReferenceEquals(entry, target)) args.TrySetNewFocusedElement(entry);
        };
    }

    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(nameof(Items), typeof(object), typeof(BleeckerTabs), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty ActiveTabProperty = DependencyProperty.Register(nameof(ActiveTab), typeof(string), typeof(BleeckerTabs), new PropertyMetadata(null, Changed));
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(BleeckerTabsVariant), typeof(BleeckerTabs), new PropertyMetadata(BleeckerTabsVariant.Underline, Changed));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(BleeckerControlSize), typeof(BleeckerTabs), new PropertyMetadata(BleeckerControlSize.Medium, Changed));
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(nameof(Stretch), typeof(bool), typeof(BleeckerTabs), new PropertyMetadata(false, Changed));
    public static readonly DependencyProperty LinkTemplateProperty = DependencyProperty.Register(nameof(LinkTemplate), typeof(DataTemplate), typeof(BleeckerTabs), new PropertyMetadata(null, Changed));

    public ObservableCollection<BleeckerTabItem> Items { get => (ObservableCollection<BleeckerTabItem>)GetValue(ItemsProperty); set { BleeckerTabsModel.Validate(value); SetValue(ItemsProperty, value); } }
    public string? ActiveTab { get => (string?)GetValue(ActiveTabProperty); set => SetValue(ActiveTabProperty, value); }
    public BleeckerTabsVariant Variant { get => (BleeckerTabsVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerControlSize Size { get => (BleeckerControlSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool Stretch { get => (bool)GetValue(StretchProperty); set => SetValue(StretchProperty, value); }
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public bool IsNavigation => Items.Any(item => !string.IsNullOrEmpty(item.Href));
    public event EventHandler<BleeckerTabRequestedEventArgs>? ActiveTabRequested;
    public event EventHandler<BleeckerTabNavigationEventArgs>? NavigationRequested;
    internal IReadOnlyList<Button> TabButtons => Items.Select(item => (Button)buttons[item.Id]).ToArray();

    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var tabs = (BleeckerTabs)owner;
        if (args.Property == ItemsProperty) tabs.Observe();
        if (args.Property == LinkTemplateProperty) tabs.DetachEntries();
        tabs.Reconcile();
    }

    protected override void OnApplyTemplate()
    {
        Detach();
        base.OnApplyTemplate();
        root = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Tabs requires PART_Root.");
        frame = GetTemplateChild("TabsFrame") as Border;
        if (frame is not null)
            shadow = new(this, frame, (Grid)GetTemplateChild("TabsShadowHost"), (Microsoft.UI.Xaml.Shapes.Path)GetTemplateChild("TabsShadowMask"), (Microsoft.UI.Xaml.Shapes.Path)GetTemplateChild("TabsShadowExterior"));
        panel = new TabsPanel { UseLayoutRounding = false };
        root.Children.Add(panel);
        Reconcile();
    }

    private void DetachEntries()
    {
        foreach (var button in buttons.Values) button.Release();
        panel?.Children.Clear();
        buttons.Clear();
    }

    private void Detach() { DetachEntries(); shadow?.Dispose(); shadow = null; panel = null; root = null; frame = null; }
    private void StopObserving()
    {
        if (observed is not null) observed.CollectionChanged -= CollectionChanged;
        observed = null;
        foreach (var command in commands) command.CanExecuteChanged -= CommandChanged;
        commands.Clear();
    }
    private void Observe()
    {
        StopObserving();
        BleeckerTabsModel.Validate(Items);
        if (!IsLoaded) return;
        observed = Items;
        observed.CollectionChanged += CollectionChanged;
        foreach (var command in Items.Select(item => item.Command).OfType<ICommand>().Distinct())
        { commands.Add(command); command.CanExecuteChanged += CommandChanged; }
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) { Observe(); Reconcile(); }
    private void CommandChanged(object? sender, EventArgs args) => Synchronize();

    private void Reconcile()
    {
        BleeckerTabsModel.Validate(Items);
        if (!Enum.IsDefined(Variant) || !Enum.IsDefined(Size)) throw new ArgumentException("Unknown tab variant or size.");
        if (panel is null || root is null) return;
        var focused = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as TabButton;
        var focusState = focused?.FocusState ?? FocusState.Unfocused;
        var restoreFocus = focused is not null && ReferenceEquals(focused.Owner, this);
        var wanted = Items.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var id in buttons.Keys.Where(id => !wanted.Contains(id)).ToArray())
        { panel.Children.Remove(buttons[id].Presentation); buttons[id].Release(); buttons.Remove(id); }
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
            if (!buttons.TryGetValue(item.Id, out var button))
            {
                button = new TabButton(this, item) { Style = (Style)styles["BleeckerTabButtonStyle"] };
                buttons.Add(item.Id, button);
            }
            var oldPresentation = button.Presentation;
            button.Update(item);
            if (!ReferenceEquals(oldPresentation, button.Presentation)) panel.Children.Remove(oldPresentation);
            if (panel.Children.IndexOf(button.Presentation) != index)
            { panel.Children.Remove(button.Presentation); panel.Children.Insert(index, button.Presentation); }
            AutomationProperties.SetPositionInSet(button, index + 1);
            AutomationProperties.SetSizeOfSet(button, Items.Count);
        }
        panel.Stretch = Stretch;
        if (frame is not null) frame.HorizontalAlignment = Stretch || Variant == BleeckerTabsVariant.Underline ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        TabFocusNavigation = IsNavigation ? KeyboardNavigationMode.Local : KeyboardNavigationMode.Once;
        VisualStateManager.GoToState(this, Variant.ToString(), false);
        shadow?.Set(Variant == BleeckerTabsVariant.Enclosed ? new(2, 1, 0, .04f, 21, 48, 66) : null);
        Synchronize();
        panel.InvalidateMeasure();
        if (restoreFocus && focused is not null)
        {
            var target = buttons.GetValueOrDefault(focused.Item.Id);
            if (target?.IsEnabled != true) target = Entry();
            if (target is not null && !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), target))
                target.Focus(focusState);
        }
    }

    private TabButton? Entry() => Items.Select(item => buttons.GetValueOrDefault(item.Id))
        .Where(button => button?.IsEnabled == true).OrderByDescending(button => button!.Item.Id == ActiveTab).FirstOrDefault();
    private void Synchronize()
    {
        foreach (var button in buttons.Values) button.Synchronize();
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private bool Owns(TabButton button) => IsLoaded && button.Presentation.IsLoaded &&
        buttons.TryGetValue(button.Item.Id, out var current) && ReferenceEquals(current, button);
    private bool CanActivate(TabButton button) => Owns(button) && IsEnabled && button.IsEnabled && !button.Item.Disabled &&
        (button.Item.Command?.CanExecute(button.Item.CommandParameter) ?? true);
    private void Request(TabButton button)
    {
        if (!CanActivate(button)) return;
        if (!string.IsNullOrEmpty(button.Item.Href))
        {
            var item = button.Item;
            if (item.Command is { } command) command.Execute(item.CommandParameter);
            NavigationRequested?.Invoke(this, new(item));
        }
        else ActiveTabRequested?.Invoke(this, new(button.Item.Id));
    }

    private void Navigate(TabButton current, KeyRoutedEventArgs args)
    {
        if (IsNavigation || !Owns(current) || args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Home or VirtualKey.End)) return;
        var enabled = Items.Select(item => buttons[item.Id]).Where(button => button.IsEnabled).ToArray();
        var index = Array.IndexOf(enabled, current);
        if (index < 0) return;
        var forward = (args.Key == VirtualKey.Right) != (FlowDirection == FlowDirection.RightToLeft);
        var next = args.Key switch { VirtualKey.Home => 0, VirtualKey.End => enabled.Length - 1, _ => (index + (forward ? 1 : enabled.Length - 1)) % enabled.Length };
        args.Handled = true;
        Request(enabled[next]);
        if (!Owns(enabled[next]) || !enabled[next].IsEnabled) return;
        enabled[next].Focus(FocusState.Keyboard);
        enabled[next].StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TabsPeer(this);
    private sealed class TabsPeer(BleeckerTabs owner) : FrameworkElementAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IRawElementProviderSimple[] GetSelection() => owner.buttons.Values
            .Where(button => button.Item.Id == owner.ActiveTab && string.IsNullOrEmpty(button.Item.Href))
            .Select(button => ProviderFromPeer(CreatePeerForElement(button))).ToArray();
        protected override string GetClassNameCore() => nameof(BleeckerTabs);
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.IsNavigation ? AutomationControlType.Group : AutomationControlType.Tab;
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Selection ? owner.IsNavigation ? null! : this : base.GetPatternCore(pattern);
    }

    private sealed partial class TabButton : Button
    {
        internal BleeckerTabs Owner { get; }
        internal BleeckerTabItem Item { get; private set; }
        private bool hovered;
        private bool selected;
        private TextBlock? label;
        private Grid? labelHost;
        private readonly BleeckerReferenceText.IntrinsicWidth labelWidth = new();
        private readonly BleeckerReferenceText.Presentation labelPresentation = new();
        private BleeckerTabLinkContext? context;
        private FrameworkElement? renderedLink;
        private BleeckerTabSurface? surfacePaint;
        internal FrameworkElement Presentation => renderedLink ?? this;
        internal TabButton(BleeckerTabs owner, BleeckerTabItem item)
        {
            Owner = owner; Item = item;
            Click += (_, _) => Owner.Request(this);
            PointerEntered += (_, _) => { hovered = true; Paint(); };
            PointerExited += (_, _) => { hovered = false; Paint(); };
            IsEnabledChanged += (_, _) => Paint();
        }
        protected override void OnApplyTemplate() { base.OnApplyTemplate(); AttachSurface(this); Paint(); }
        internal void Detach() { if (context is not null) context.Mounted = false; }
        internal void Release() { Detach(); surfacePaint?.Dispose(); surfacePaint = null; }
        private void AttachSurface(Control surface)
        {
            surfacePaint?.Dispose(); surfacePaint = null;
            T? Part<T>(DependencyObject parent, string name) where T : FrameworkElement
            {
                for (var index = 0; index < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(parent); index++)
                {
                    var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(parent, index);
                    if (child is T candidate && candidate.Name == name) return candidate;
                    if (Part<T>(child, name) is { } found) return found;
                }
                return null;
            }
            // Arbitrary consumer renderers own their own visual template.
            if (Part<Grid>(surface, "TabShadowHost") is { } host && Part<Border>(surface, "TabBorder") is { } rounded &&
                Part<Microsoft.UI.Xaml.Shapes.Path>(surface, "TabShadowMask") is { } mask && Part<Microsoft.UI.Xaml.Shapes.Path>(surface, "TabShadowExterior") is { } exterior &&
                Part<Border>(surface, "TabPalette") is { } palette && Part<TextBlock>(surface, "TabForegroundPalette") is { } foreground && Part<ContentPresenter>(surface, "TabContent") is { } content)
                surfacePaint = new(surface, rounded, palette, foreground, content, host, mask, exterior);
        }
        internal void Update(BleeckerTabItem item)
        {
            var replacement = !ReferenceEquals(Item, item) || (Content is null && renderedLink is null);
            Item = item;
            if (replacement)
            {
                Detach();
                if (renderedLink is not null) { surfacePaint?.Dispose(); surfacePaint = null; }
                label = new TextBlock { Name = "TabLabel", UseLayoutRounding = false, TextWrapping = TextWrapping.NoWrap, TextReadingOrder = TextReadingOrder.UseFlowDirection, LineStackingStrategy = LineStackingStrategy.BlockLineHeight };
                labelHost = new Grid { UseLayoutRounding = false };
                labelHost.Children.Add(label);
                BleeckerTextLayout.SetUseReferenceLineBox(label, true);
                if (!string.IsNullOrEmpty(item.Href) && Owner.LinkTemplate is { } template)
                {
                    context = new BleeckerTabLinkContext(item, new NavigationCommand(this, item), labelHost, Style);
                    var content = template.LoadContent() as FrameworkElement ?? throw new InvalidOperationException("Tab LinkTemplate must create a FrameworkElement.");
                    content.DataContext = context;
                    renderedLink = content;
                    content.Loaded += (_, _) => { if (content is Control surface) AttachSurface(surface); Synchronize(); };
                    content.PointerEntered += (_, _) => { hovered = true; Paint(); };
                    content.PointerExited += (_, _) => { hovered = false; Paint(); };
                }
                else { context = null; renderedLink = null; Content = labelHost; }
            }
            AutomationProperties.SetName(this, item.Label);
            Synchronize();
        }
        internal void Synchronize()
        {
            var enabled = Owner.IsEnabled && !Item.Disabled && (Item.Command?.CanExecute(Item.CommandParameter) ?? true);
            IsEnabled = enabled;
            if (renderedLink is Control custom) custom.IsEnabled = enabled;
            IsTabStop = enabled;
            var now = Item.Id == Owner.ActiveTab;
            if (now != selected)
            {
                selected = now;
                FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, !now, now);
            }
            var size = Owner.Size;
            MinHeight = size == BleeckerControlSize.Small ? 32 : size == BleeckerControlSize.Large ? 48 : 40;
            Padding = new Thickness(size == BleeckerControlSize.Small ? 12 : size == BleeckerControlSize.Large ? 20 : 16, 0,
                size == BleeckerControlSize.Small ? 12 : size == BleeckerControlSize.Large ? 20 : 16, 0);
            FontSize = size == BleeckerControlSize.Small ? 12 : size == BleeckerControlSize.Large ? 15 : 14;
            if (label is not null) label.LineHeight = size == BleeckerControlSize.Small ? 16 : size == BleeckerControlSize.Large ? 22.5 : 20;
            BorderThickness = Owner.Variant == BleeckerTabsVariant.Underline ? new Thickness(0, 0, 0, 2) : new Thickness(1);
            CornerRadius = new CornerRadius(Owner.Variant == BleeckerTabsVariant.Underline ? 0 : 7);
            if (renderedLink is Control customControl)
            {
                customControl.MinHeight = MinHeight;
                customControl.Padding = Padding;
                customControl.FontSize = FontSize;
                customControl.BorderThickness = BorderThickness;
                customControl.CornerRadius = CornerRadius;
            }
            if (context?.Command is NavigationCommand navigation) navigation.Notify();
            PrepareLabel();
            Paint();
        }
        private void PrepareLabel()
        {
            if (label is null || labelHost is null) return;
            label.FontFamily = FontFamily; label.FontSize = FontSize; label.FontWeight = FontWeight;
            label.FontStyle = FontStyle; label.FontStretch = FontStretch; label.CharacterSpacing = CharacterSpacing;
            label.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; label.FlowDirection = FlowDirection;
            if (label.Language != Language) label.Language = Language;
            labelPresentation.Update(label, Item.Label);
            var width = Math.Ceiling(labelWidth.Measure(label, Item.Label) * 64) / 64;
            label.Width = width; labelHost.Width = width;
        }
        protected override Size MeasureOverride(Size available) { PrepareLabel(); return base.MeasureOverride(available); }
        private void Paint()
        {
            var state = Owner.Variant + (selected ? "Active" : hovered && IsEnabled ? "Hover" : "Inactive");
            void Apply() => VisualStateManager.GoToState(renderedLink as Control ?? this, state, false);
            if (surfacePaint is not null) surfacePaint.Update(Owner.Variant, selected, hovered && IsEnabled, Apply);
            else Apply();
        }
        protected override void OnKeyDown(KeyRoutedEventArgs args) { Owner.Navigate(this, args); if (!args.Handled) base.OnKeyDown(args); }
        protected override AutomationPeer OnCreateAutomationPeer() => new TabPeer(this);
        private sealed class NavigationCommand(TabButton button, BleeckerTabItem item) : ICommand
        {
            public event EventHandler? CanExecuteChanged;
            internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            public bool CanExecute(object? parameter) => ReferenceEquals(button.Item, item) && button.Owner.CanActivate(button);
            public void Execute(object? parameter) { if (CanExecute(parameter)) button.Owner.Request(button); }
        }
        private sealed class TabPeer(TabButton button) : ButtonAutomationPeer(button), ISelectionItemProvider, IInvokeProvider
        {
            public bool IsSelected => button.Item.Id == button.Owner.ActiveTab;
            public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(CreatePeerForElement(button.Owner));
            private void Guard()
            {
                if (!button.Owner.Owns(button)) throw new ElementNotAvailableException();
                if (!button.Owner.CanActivate(button)) throw new ElementNotEnabledException();
            }
            public void Select() { Guard(); button.Owner.Request(button); }
            void IInvokeProvider.Invoke() { Guard(); button.Owner.Request(button); }
            public void AddToSelection() { Guard(); if (button.Owner.ActiveTab is not null && !IsSelected) throw new InvalidOperationException("Tabs supports one selected item."); Select(); }
            public void RemoveFromSelection() { Guard(); if (IsSelected) throw new InvalidOperationException("A tab cannot clear the consumer's selection."); }
            protected override string GetClassNameCore() => "BleeckerTab";
            protected override AutomationControlType GetAutomationControlTypeCore() => string.IsNullOrEmpty(button.Item.Href) ? AutomationControlType.TabItem : AutomationControlType.Hyperlink;
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.SelectionItem ? string.IsNullOrEmpty(button.Item.Href) ? this : null! :
                pattern == PatternInterface.Invoke ? string.IsNullOrEmpty(button.Item.Href) ? null! : this : base.GetPatternCore(pattern);
        }
    }

    private sealed partial class TabsPanel : Panel
    {
        internal bool Stretch { get; set; }
        protected override Size MeasureOverride(Size available)
        {
            foreach (var child in Children) child.Measure(new Size(double.PositiveInfinity, available.Height));
            return new Size(Children.Sum(child => child.DesiredSize.Width) + Math.Max(0, Children.Count - 1) * 4,
                Children.Count == 0 ? 0 : Children.Max(child => child.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var widths = BleeckerTabsModel.Widths(Children.Select(child => child.DesiredSize.Width).ToArray(), final.Width - Math.Max(0, Children.Count - 1) * 4, Stretch);
            var x = 0d;
            for (var index = 0; index < Children.Count; index++)
            {
                Children[index].Arrange(new Rect(x, 0, widths[index], final.Height));
                x += widths[index] + 4;
            }
            return final;
        }
    }
}

/// <summary>Original link metadata and a guarded activation command for a custom link renderer.</summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed record BleeckerTabLinkContext
{
    internal BleeckerTabLinkContext(BleeckerTabItem item, ICommand command, UIElement content, Style style)
    { Item = item; Command = command; Content = content; Style = style; }
    public BleeckerTabItem Item { get; }
    public ICommand Command { get; }
    public UIElement Content { get; }
    public Style Style { get; }
    public string Href => Item.Href!;
    public string Label => Item.Label;
    internal bool Mounted { get; set; } = true;
}
