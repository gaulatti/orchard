using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

/// <summary>Sabella's compact value-bound app destinations, adapted to native Windows tab semantics.</summary>
public sealed class BleeckerAppTabBar : Control
{
    private readonly record struct Key(object? Value);
    private readonly Dictionary<Key, TabButton> buttons = [];
    private readonly ResourceDictionary resources = new() { Source = new Uri("ms-appx:///Orchard/Themes/AppTabBar.xaml") };
    private ObservableCollection<BleeckerAppTab>? observed;
    private TabPanel? panel;
    private int revision;

    public BleeckerAppTabBar()
    {
        DefaultStyleKey = typeof(BleeckerAppTabBar);
        IsTabStop = false; TabFocusNavigation = KeyboardNavigationMode.Once; Tabs = [];
        Loaded += (_, _) => { Observe(); Reconcile(); };
        Unloaded += (_, _) => { revision++; StopObserving(); };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        GettingFocus += (_, args) =>
        {
            if (args.FocusState != FocusState.Keyboard || args.Direction is not (FocusNavigationDirection.Next or FocusNavigationDirection.Previous) ||
                args.NewFocusedElement is not TabButton target || !ReferenceEquals(target.Owner, this) ||
                args.OldFocusedElement is TabButton previous && ReferenceEquals(previous.Owner, this)) return;
            if (Entry() is { } entry && !ReferenceEquals(entry, target)) args.TrySetNewFocusedElement(entry);
        };
    }
    public static readonly DependencyProperty TabsProperty = DependencyProperty.Register(nameof(Tabs), typeof(object), typeof(BleeckerAppTabBar), new PropertyMetadata(null, (owner, args) =>
    {
        if (args.NewValue is not ObservableCollection<BleeckerAppTab> tabs) throw new ArgumentException("Tabs must be an ObservableCollection<BleeckerAppTab>.");
        Validate(tabs); var bar = (BleeckerAppTabBar)owner; bar.Observe(); bar.Reconcile();
    }));
    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(BleeckerAppTabBar), new PropertyMetadata(null, (owner, _) =>
    {
        var bar = (BleeckerAppTabBar)owner; bar.Synchronize(); bar.SelectionChanged?.Invoke(bar, EventArgs.Empty);
        FrameworkElementAutomationPeer.FromElement(bar)?.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
    }));
    public ObservableCollection<BleeckerAppTab> Tabs { get => (ObservableCollection<BleeckerAppTab>)GetValue(TabsProperty); set { Validate(value); SetValue(TabsProperty, value); } }
    public object? SelectedValue { get => GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }
    public event EventHandler? SelectionChanged;
    private static void Validate(IEnumerable<BleeckerAppTab> tabs)
    {
        ArgumentNullException.ThrowIfNull(tabs);
        BleeckerAppTabModel.ValidateValues(tabs.Select(tab => { ArgumentNullException.ThrowIfNull(tab); return tab.Value; }));
    }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        var host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("App Tab Bar requires PART_Items.");
        panel = new TabPanel { UseLayoutRounding = false }; host.Children.Add(panel); Reconcile();
    }
    private void Detach() { revision++; panel?.Children.Clear(); buttons.Clear(); panel = null; }
    private void StopObserving() { if (observed is not null) observed.CollectionChanged -= CollectionChanged; observed = null; }
    private void Observe() { StopObserving(); if (IsLoaded) { observed = Tabs; observed.CollectionChanged += CollectionChanged; } }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Reconcile();
    private bool Owns(TabButton button) => panel?.Children.Contains(button) == true && buttons.TryGetValue(new(button.Tab.Value), out var actual) && ReferenceEquals(actual, button);
    private TabButton? Entry() => Tabs.Select(tab => buttons.GetValueOrDefault(new(tab.Value))).FirstOrDefault(button => button is { IsEnabled: true } && Equals(button.Tab.Value, SelectedValue))
        ?? Tabs.Select(tab => buttons.GetValueOrDefault(new(tab.Value))).FirstOrDefault(button => button is { IsEnabled: true });
    private void Reconcile()
    {
        Validate(Tabs); if (panel is null) return;
        var focused = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as TabButton;
        var restore = focused is not null && ReferenceEquals(focused.Owner, this) && Owns(focused);
        var current = ++revision; var wanted = Tabs.Select(tab => new Key(tab.Value)).ToHashSet();
        foreach (var key in buttons.Keys.Where(key => !wanted.Contains(key)).ToArray()) { panel.Children.Remove(buttons[key]); buttons.Remove(key); }
        var ordered = new List<TabButton>();
        foreach (var tab in Tabs)
        {
            var key = new Key(tab.Value);
            if (!buttons.TryGetValue(key, out var button)) { button = new TabButton(this, tab) { Template = (ControlTemplate)resources["BleeckerAppTabButtonTemplate"] }; buttons.Add(key, button); }
            button.Update(tab); ordered.Add(button);
            AutomationProperties.SetPositionInSet(button, ordered.Count); AutomationProperties.SetSizeOfSet(button, Tabs.Count);
        }
        for (var index = 0; index < ordered.Count; index++)
        {
            var button = ordered[index]; if (index < panel.Children.Count && ReferenceEquals(panel.Children[index], button)) continue;
            panel.Children.Remove(button); panel.Children.Insert(index, button);
        }
        panel.InvalidateMeasure(); Synchronize();
        FrameworkElementAutomationPeer.FromElement(this)?.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
        if (restore && XamlRoot?.IsHostVisible == true) DispatcherQueue.TryEnqueue(() =>
        {
            if (current != revision || !IsLoaded || XamlRoot?.IsHostVisible != true) return;
            (focused is not null && Owns(focused) && focused.IsEnabled ? focused : Entry())?.Focus(FocusState.Keyboard);
        });
    }
    private void Synchronize()
    {
        foreach (var button in buttons.Values) { button.IsEnabled = IsEnabled && !button.Tab.IsDisabled; button.IsTabStop = button.IsEnabled; button.RefreshSelection(); }
    }
    private void Guard(TabButton button)
    {
        if (!IsLoaded || !button.IsLoaded || !Owns(button)) throw new ElementNotAvailableException();
        if (!IsEnabled || !button.IsEnabled) throw new ElementNotEnabledException();
    }
    private void Select(TabButton button) { Guard(button); SelectedValue = button.Tab.Value; }
    private void Navigate(TabButton button, KeyRoutedEventArgs args)
    {
        if (args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Home or VirtualKey.End) || !Owns(button)) return;
        var list = Tabs.Select(tab => buttons[new(tab.Value)]).ToArray(); var direction = args.Key == VirtualKey.Left ? -1 : 1;
        if (FlowDirection == FlowDirection.RightToLeft) direction = -direction;
        var next = BleeckerAppTabModel.Navigate(list.Select(item => item.IsEnabled).ToArray(), Array.IndexOf(list, button), direction, args.Key == VirtualKey.Home, args.Key == VirtualKey.End);
        args.Handled = true;
        if (next >= 0 && XamlRoot?.IsHostVisible == true && list[next].Focus(FocusState.Keyboard) &&
            !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) Select(list[next]);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BarPeer(this);
    private sealed class BarPeer(BleeckerAppTabBar owner) : FrameworkElementAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IRawElementProviderSimple[] GetSelection() => owner.Tabs.Select(tab => owner.buttons.GetValueOrDefault(new(tab.Value))).Where(button => button is not null && Equals(button.Tab.Value, owner.SelectedValue)).Select(button => ProviderFromPeer(CreatePeerForElement(button!))).ToArray();
        protected override string GetClassNameCore() => nameof(BleeckerAppTabBar);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Selection ? this : base.GetPatternCore(pattern);
    }
    private sealed class TabButton : Button
    {
        private bool? publishedSelection;
        private TextBlock? selectedPalette, inactivePalette;
        private readonly TabContent content;
        internal BleeckerAppTabBar Owner { get; }
        internal BleeckerAppTab Tab { get; private set; }
        internal TabButton(BleeckerAppTabBar owner, BleeckerAppTab tab)
        {
            Owner = owner; Tab = tab; MinWidth = 0; MinHeight = 54; Padding = new Thickness(0); BorderThickness = new Thickness(0);
            UseLayoutRounding = false; HorizontalAlignment = HorizontalAlignment.Stretch; VerticalAlignment = VerticalAlignment.Stretch; HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
            UseSystemFocusVisuals = true; FocusVisualPrimaryThickness = new Thickness(2); FocusVisualSecondaryThickness = new Thickness(0); FocusVisualMargin = new Thickness(-2);
            content = new TabContent(owner); Content = content;
            Click += (_, _) => Owner.Select(this);
        }
        internal void Update(BleeckerAppTab tab)
        {
            var previous = Tab; Tab = tab; DataContext = tab; content.Update(tab);
            AutomationProperties.SetName(this, tab.AccessibilityLabel); AutomationProperties.SetItemStatus(this, BleeckerAppTabModel.BadgeStatus(tab.Badge));
            if (previous.Badge != tab.Badge) FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, BleeckerAppTabModel.BadgeStatus(previous.Badge), BleeckerAppTabModel.BadgeStatus(tab.Badge));
            if (previous.AccessibilityLabel != tab.AccessibilityLabel) FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, previous.AccessibilityLabel, tab.AccessibilityLabel);
        }
        internal void RefreshSelection()
        {
            var selected = Equals(Owner.SelectedValue, Tab.Value); content.Select(selected, selected ? selectedPalette : inactivePalette);
            if (publishedSelection is { } previous && previous != selected) FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, previous, selected);
            publishedSelection = selected;
        }
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate(); selectedPalette = (TextBlock)GetTemplateChild("AppTabSelectedPalette"); inactivePalette = (TextBlock)GetTemplateChild("AppTabInactivePalette");
            SetBinding(FocusVisualPrimaryBrushProperty, Bind(selectedPalette, "Foreground"));
            content.Palettes((TextBlock)GetTemplateChild("AppTabBadgePalette"), (TextBlock)GetTemplateChild("AppTabBadgeTextPalette")); RefreshSelection();
        }
        protected override void OnKeyDown(KeyRoutedEventArgs args) { Owner.Navigate(this, args); if (!args.Handled) base.OnKeyDown(args); }
        protected override AutomationPeer OnCreateAutomationPeer() => new TabPeer(this);
        private sealed class TabPeer(TabButton button) : ButtonAutomationPeer(button), ISelectionItemProvider
        {
            public bool IsSelected => Equals(button.Owner.SelectedValue, button.Tab.Value);
            public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(CreatePeerForElement(button.Owner));
            public void Select() => button.Owner.Select(button);
            public void AddToSelection() { button.Owner.Guard(button); if (button.Owner.buttons.Values.Any(item => !ReferenceEquals(item, button) && Equals(item.Tab.Value, button.Owner.SelectedValue))) throw new InvalidOperationException("App Tab Bar has a single selection."); Select(); }
            public void RemoveFromSelection() { button.Owner.Guard(button); if (IsSelected) throw new InvalidOperationException("A tab cannot clear App Tab Bar selection."); }
            protected override string GetNameCore() => button.Tab.AccessibilityLabel;
            protected override string GetItemStatusCore() => BleeckerAppTabModel.BadgeStatus(button.Tab.Badge);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
            protected override string GetClassNameCore() => nameof(BleeckerAppTab);
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.SelectionItem ? this : base.GetPatternCore(pattern);
        }
    }
    private static Binding Bind(object source, string path) => new() { Source = source, Path = new PropertyPath(path) };
    private sealed class TabPanel : Panel
    {
        private double track;
        protected override Size MeasureOverride(Size available)
        {
            foreach (var child in Children) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            track = BleeckerAppTabModel.TrackWidth(Children.Count, available.Width, Children.Count == 0 ? 0 : Children.Max(child => child.DesiredSize.Width));
            var height = 0d;
            foreach (var child in Children) { child.Measure(new Size(track, double.PositiveInfinity)); height = Math.Max(height, child.DesiredSize.Height); }
            return new Size(track * Children.Count, height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var width = Children.Count == 0 ? 0 : final.Width / Children.Count;
            // WinUI mirrors logical coordinates at the RTL boundary.
            for (var index = 0; index < Children.Count; index++) Children[index].Arrange(new Rect(index * width, 0, width, final.Height));
            return final;
        }
    }
    private sealed class TabContent : Panel
    {
        private static readonly DependencyProperty LabelSizeProperty = DependencyProperty.Register("LabelSize", typeof(double), typeof(TabContent), new PropertyMetadata(10d, (owner, _) => ((TabContent)owner).InvalidateMeasure()));
        private FontIconSource? source;
        private readonly Dictionary<DependencyProperty, long> sourceCallbacks = [];
        private readonly FontIcon icon = new() { Name = "AppTabIcon", FontSize = 18, UseLayoutRounding = false };
        private readonly TextBlock label = new() { Name = "AppTabLabel", TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis, UseLayoutRounding = false };
        private readonly TextBlock badgeLabel = new() { Name = "AppTabBadgeText", FontSize = 8, FontWeight = new() { Weight = 700 }, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false };
        private readonly Border badge;
        private readonly TextBlock probe = new() { TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false };
        private readonly TextBlock lineProbe = new() { Text = "Mg", UseLayoutRounding = false };
        internal TabContent(BleeckerAppTabBar owner)
        {
            Name = "AppTabContent"; UseLayoutRounding = false;
            badge = new Border { Name = "AppTabBadge", MinWidth = 15, MinHeight = 15, Padding = new Thickness(4, 0, 4, 0), CornerRadius = new CornerRadius(100), Child = badgeLabel, UseLayoutRounding = false };
            Children.Add(icon); Children.Add(label); Children.Add(badge);
            foreach (var element in new FrameworkElement[] { icon, label, badge, badgeLabel }) AutomationProperties.SetAccessibilityView(element, AccessibilityView.Raw);
            foreach (var text in new[] { label, badgeLabel, probe, lineProbe }) text.SetBinding(TextBlock.IsTextScaleFactorEnabledProperty, Bind(owner, nameof(IsTextScaleFactorEnabled)));
            icon.SetBinding(FontIcon.IsTextScaleFactorEnabledProperty, Bind(owner, nameof(IsTextScaleFactorEnabled)));
            SetBinding(LabelSizeProperty, Bind(owner, nameof(FontSize)));
            Loaded += (_, _) => ObserveIcon(); Unloaded += (_, _) => StopObservingIcon();
        }
        internal void Update(BleeckerAppTab tab)
        {
            label.Text = probe.Text = tab.Label;
            StopObservingIcon(); source = tab.Icon; ApplyIcon(); if (IsLoaded) ObserveIcon();
            badgeLabel.Text = BleeckerAppTabModel.BadgeText(tab.Badge); badge.Visibility = badgeLabel.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            InvalidateMeasure();
        }
        private void ApplyIcon()
        {
            if (source is null) return;
            icon.Glyph = source.Glyph; icon.FontFamily = source.FontFamily; icon.FontStyle = source.FontStyle; icon.MirroredWhenRightToLeft = source.MirroredWhenRightToLeft;
        }
        private void ObserveIcon()
        {
            StopObservingIcon(); if (source is null) return; ApplyIcon();
            foreach (var property in new[] { FontIconSource.GlyphProperty, FontIconSource.FontFamilyProperty, FontIconSource.FontStyleProperty, FontIconSource.MirroredWhenRightToLeftProperty })
                sourceCallbacks.Add(property, source.RegisterPropertyChangedCallback(property, (_, _) => ApplyIcon()));
        }
        private void StopObservingIcon()
        {
            if (source is not null) foreach (var pair in sourceCallbacks) source.UnregisterPropertyChangedCallback(pair.Key, pair.Value);
            sourceCallbacks.Clear();
        }
        internal void Palettes(TextBlock background, TextBlock text)
        {
            badge.SetBinding(Border.BackgroundProperty, Bind(background, "Foreground")); badgeLabel.SetBinding(TextBlock.ForegroundProperty, Bind(text, "Foreground")); badgeLabel.SetBinding(TextBlock.FontFamilyProperty, Bind(text, "FontFamily"));
        }
        internal void Select(bool selected, TextBlock? palette)
        {
            icon.FontWeight = new() { Weight = (ushort)(selected ? 600 : 400) };
            foreach (var text in new[] { label, probe, lineProbe })
            {
                text.FontWeight = new() { Weight = (ushort)(selected ? 600 : 500) };
                if (palette is not null) text.SetBinding(TextBlock.FontFamilyProperty, Bind(palette, "FontFamily"));
            }
            if (palette is not null) { label.SetBinding(TextBlock.ForegroundProperty, Bind(palette, "Foreground")); icon.SetBinding(FontIcon.ForegroundProperty, Bind(palette, "Foreground")); }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            icon.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); badge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var size = (double)GetValue(LabelSizeProperty); probe.FlowDirection = lineProbe.FlowDirection = FlowDirection;
            bool Fits(double candidate)
            {
                probe.FontSize = lineProbe.FontSize = candidate; probe.Measure(new Size(available.Width, double.PositiveInfinity)); lineProbe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return probe.DesiredSize.Height <= lineProbe.DesiredSize.Height * 2 + .01;
            }
            if (!double.IsPositiveInfinity(available.Width) && !Fits(size))
            {
                var low = size * .8; var high = size;
                if (Fits(low)) for (var i = 0; i < 10; i++) { var middle = (low + high) / 2; if (Fits(middle)) low = middle; else high = middle; }
                size = low;
            }
            if (label.FontSize != size) label.FontSize = size;
            label.Measure(new Size(available.Width, double.PositiveInfinity));
            return new Size(Math.Max(icon.DesiredSize.Width, label.DesiredSize.Width), icon.DesiredSize.Height + 3 + label.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var y = (final.Height - icon.DesiredSize.Height - 3 - label.DesiredSize.Height) / 2;
            var x = (final.Width - icon.DesiredSize.Width) / 2;
            icon.Arrange(new Rect(x, y, icon.DesiredSize.Width, icon.DesiredSize.Height));
            label.Arrange(new Rect(0, y + icon.DesiredSize.Height + 3, final.Width, label.DesiredSize.Height));
            if (badge.Visibility == Visibility.Visible) badge.Arrange(new Rect(x + icon.DesiredSize.Width - badge.DesiredSize.Width + 10, y - 7, badge.DesiredSize.Width, badge.DesiredSize.Height));
            return final;
        }
    }
}
