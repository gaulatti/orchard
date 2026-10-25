using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

namespace Orchard;

/// <summary>Sabella's value-bound single selection, with native Windows radio semantics.</summary>
public sealed class BleeckerSegmentedControl : Control
{
    private readonly record struct Key(object? Value);
    private readonly Dictionary<Key, SegmentButton> buttons = [];
    private readonly ResourceDictionary resources = new() { Source = new Uri("ms-appx:///Orchard/Themes/SegmentedControl.xaml") };
    private ObservableCollection<BleeckerSegmentOption>? observed;
    private SegmentPanel? panel;
    private Grid? root;
    private CompositionRoundedRectangleGeometry? clip;
    private int revision;

    public BleeckerSegmentedControl()
    {
        DefaultStyleKey = typeof(BleeckerSegmentedControl);
        IsTabStop = false; TabFocusNavigation = KeyboardNavigationMode.Once;
        Options = [];
        Loaded += (_, _) => { Observe(); Reconcile(); UpdateClip(); };
        GotFocus += (_, _) => { if (FocusState != FocusState.Unfocused && XamlRoot?.IsHostVisible == true) Entry()?.Focus(FocusState); };
        Unloaded += (_, _) => { StopObserving(); ClearClip(); };
        SizeChanged += (_, _) => UpdateClip();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => Synchronize());
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateClip());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        GettingFocus += (_, args) =>
        {
            if (args.FocusState != FocusState.Keyboard || args.Direction is not (FocusNavigationDirection.Next or FocusNavigationDirection.Previous) ||
                args.NewFocusedElement is not SegmentButton target || !ReferenceEquals(target.Owner, this) ||
                args.OldFocusedElement is SegmentButton previous && ReferenceEquals(previous.Owner, this)) return;
            if (Entry() is { } entry && !ReferenceEquals(entry, target)) args.TrySetNewFocusedElement(entry);
        };
    }

    public static readonly DependencyProperty OptionsProperty = DependencyProperty.Register(nameof(Options), typeof(object), typeof(BleeckerSegmentedControl), new PropertyMetadata(null, (owner, args) =>
    {
        if (args.NewValue is not ObservableCollection<BleeckerSegmentOption> options) throw new ArgumentException("Options must be an ObservableCollection<BleeckerSegmentOption>.");
        BleeckerSegmentedModel.Validate(options); var group = (BleeckerSegmentedControl)owner; group.Observe(); group.Reconcile();
    }));
    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(nameof(SelectedValue), typeof(object), typeof(BleeckerSegmentedControl), new PropertyMetadata(null, (owner, _) =>
    {
        var group = (BleeckerSegmentedControl)owner; group.Synchronize(); group.SelectionChanged?.Invoke(group, EventArgs.Empty);
        FrameworkElementAutomationPeer.FromElement(group)?.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
    }));
    public ObservableCollection<BleeckerSegmentOption> Options { get => (ObservableCollection<BleeckerSegmentOption>)GetValue(OptionsProperty); set { BleeckerSegmentedModel.Validate(value); SetValue(OptionsProperty, value); } }
    public object? SelectedValue { get => GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }
    public event EventHandler? SelectionChanged;

    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        root = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Segmented Control requires PART_Root.");
        var host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("Segmented Control requires PART_Items.");
        panel = new SegmentPanel { UseLayoutRounding = false }; host.Children.Add(panel);
        Reconcile(); UpdateClip();
    }
    private void Detach() { revision++; ClearClip(); panel?.Children.Clear(); buttons.Clear(); panel = null; root = null; }
    private void StopObserving() { if (observed is not null) observed.CollectionChanged -= CollectionChanged; observed = null; }
    private void Observe() { StopObserving(); if (IsLoaded) { observed = Options; observed.CollectionChanged += CollectionChanged; } }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) => Reconcile();
    private bool Owns(SegmentButton button) => panel?.Children.Contains(button) == true && buttons.TryGetValue(new(button.Option.Value), out var actual) && ReferenceEquals(actual, button);
    private SegmentButton? Entry() => Options.Select(option => buttons.GetValueOrDefault(new(option.Value))).FirstOrDefault(button => button is { IsEnabled: true } && Equals(button.Option.Value, SelectedValue))
        ?? Options.Select(option => buttons.GetValueOrDefault(new(option.Value))).FirstOrDefault(button => button is { IsEnabled: true });

    private void Reconcile()
    {
        BleeckerSegmentedModel.Validate(Options); if (panel is null) return;
        var focused = XamlRoot is null ? null : FocusManager.GetFocusedElement(XamlRoot) as SegmentButton;
        var restore = focused is not null && ReferenceEquals(focused.Owner, this) && Owns(focused);
        var current = ++revision; var wanted = Options.Select(option => new Key(option.Value)).ToHashSet();
        foreach (var key in buttons.Keys.Where(key => !wanted.Contains(key)).ToArray()) { panel.Children.Remove(buttons[key]); buttons.Remove(key); }
        var ordered = new List<SegmentButton>();
        foreach (var option in Options)
        {
            var key = new Key(option.Value);
            if (!buttons.TryGetValue(key, out var button)) { button = new SegmentButton(this, option) { Template = (ControlTemplate)resources["BleeckerSegmentedButtonTemplate"] }; buttons.Add(key, button); }
            button.Update(option); ordered.Add(button);
            AutomationProperties.SetPositionInSet(button, ordered.Count); AutomationProperties.SetSizeOfSet(button, Options.Count);
        }
        for (var index = 0; index < ordered.Count; index++)
        {
            var button = ordered[index];
            if (index < panel.Children.Count && ReferenceEquals(panel.Children[index], button)) continue;
            panel.Children.Remove(button); panel.Children.Insert(index, button);
        }
        panel.InvalidateMeasure(); Synchronize();
        FrameworkElementAutomationPeer.FromElement(this)?.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
        if (restore && XamlRoot?.IsHostVisible == true) DispatcherQueue.TryEnqueue(() =>
        {
            if (current != revision || !IsLoaded || XamlRoot?.IsHostVisible != true) return;
            var target = focused is not null && Owns(focused) && focused.IsEnabled ? focused : Entry();
            target?.Focus(FocusState.Keyboard);
        });
    }
    private void Synchronize()
    {
        foreach (var button in buttons.Values)
        {
            button.IsEnabled = IsEnabled && !button.Option.IsDisabled;
            button.IsTabStop = button.IsEnabled;
            button.RefreshSelection();
        }
    }
    private void Select(SegmentButton button)
    {
        Guard(button);
        SelectedValue = button.Option.Value;
    }
    private void Guard(SegmentButton button)
    {
        if (!IsLoaded || !button.IsLoaded || !Owns(button)) throw new ElementNotAvailableException();
        if (!IsEnabled || !button.IsEnabled) throw new ElementNotEnabledException();
    }
    private void Navigate(SegmentButton button, KeyRoutedEventArgs args)
    {
        if (args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Home or VirtualKey.End) || !Owns(button)) return;
        var list = Options.Select(option => buttons[new(option.Value)]).ToArray();
        var direction = args.Key == VirtualKey.Left ? -1 : 1;
        if (FlowDirection == FlowDirection.RightToLeft) direction = -direction;
        var next = BleeckerSegmentedModel.Navigate(list.Select(item => item.IsEnabled).ToArray(), Array.IndexOf(list, button), direction, args.Key == VirtualKey.Home, args.Key == VirtualKey.End);
        args.Handled = true;
        if (next >= 0 && XamlRoot?.IsHostVisible == true && list[next].Focus(FocusState.Keyboard) &&
            !Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down)) Select(list[next]);
    }
    private void ClearClip() { if (root is not null) ElementCompositionPreview.GetElementVisual(root).Clip = null; clip = null; }
    private void UpdateClip()
    {
        if (root is null || !IsLoaded) return;
        var visual = ElementCompositionPreview.GetElementVisual(root);
        clip ??= visual.Compositor.CreateRoundedRectangleGeometry();
        clip.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
        clip.CornerRadius = new Vector2((float)CornerRadius.TopLeft);
        visual.Clip = visual.Compositor.CreateGeometricClip(clip);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);
    private sealed class GroupPeer(BleeckerSegmentedControl owner) : FrameworkElementAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IRawElementProviderSimple[] GetSelection() => owner.Options.Select(option => owner.buttons.GetValueOrDefault(new(option.Value))).Where(button => button is not null && Equals(button.Option.Value, owner.SelectedValue)).Select(button => ProviderFromPeer(CreatePeerForElement(button!))).ToArray();
        protected override string GetClassNameCore() => nameof(BleeckerSegmentedControl);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Selection ? this : base.GetPatternCore(pattern);
    }
    private sealed class SegmentButton : Button
    {
        private bool? publishedSelection;
        internal BleeckerSegmentedControl Owner { get; }
        internal BleeckerSegmentOption Option { get; private set; }
        internal SegmentButton(BleeckerSegmentedControl owner, BleeckerSegmentOption option)
        {
            Owner = owner; Option = option; MinWidth = 0; MinHeight = 32; Padding = new Thickness(12, 0, 12, 0); BorderThickness = new Thickness(0); CornerRadius = new CornerRadius(7);
            UseSystemFocusVisuals = true; FocusVisualPrimaryThickness = new Thickness(2); FocusVisualSecondaryThickness = new Thickness(0); FocusVisualMargin = new Thickness(-2);
            foreach (var (property, name) in new[] { (FontFamilyProperty, nameof(FontFamily)), (FontSizeProperty, nameof(FontSize)), (FontWeightProperty, nameof(FontWeight)), (IsTextScaleFactorEnabledProperty, nameof(IsTextScaleFactorEnabled)), (ForegroundProperty, nameof(Foreground)) })
                SetBinding(property, new Binding { Source = owner, Path = new PropertyPath(name) });
            Click += (_, _) => Owner.Select(this);
        }
        internal void Update(BleeckerSegmentOption option) { Option = option; DataContext = option; Content = option.Label; AutomationProperties.SetName(this, option.Label); }
        internal void RefreshSelection()
        {
            var selected = Equals(Owner.SelectedValue, Option.Value);
            VisualStateManager.GoToState(this, selected ? "SegmentSelected" : "SegmentUnselected", false);
            if (publishedSelection is { } previous && previous != selected)
                FrameworkElementAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, previous, selected);
            publishedSelection = selected;
        }
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild("SegmentFocusPalette") is TextBlock palette) SetBinding(FocusVisualPrimaryBrushProperty, new Binding { Source = palette, Path = new PropertyPath("Foreground") });
            RefreshSelection();
        }
        protected override void OnKeyDown(KeyRoutedEventArgs args) { Owner.Navigate(this, args); if (!args.Handled) base.OnKeyDown(args); }
        protected override AutomationPeer OnCreateAutomationPeer() => new OptionPeer(this);
        private sealed class OptionPeer(SegmentButton button) : ButtonAutomationPeer(button), ISelectionItemProvider
        {
            public bool IsSelected => Equals(button.Owner.SelectedValue, button.Option.Value);
            public IRawElementProviderSimple SelectionContainer => ProviderFromPeer(CreatePeerForElement(button.Owner));
            public void Select() => button.Owner.Select(button);
            public void AddToSelection() { button.Owner.Guard(button); if (button.Owner.buttons.Values.Any(item => !ReferenceEquals(item, button) && Equals(item.Option.Value, button.Owner.SelectedValue))) throw new InvalidOperationException("Segmented Control has a single selection."); Select(); }
            public void RemoveFromSelection() { button.Owner.Guard(button); if (IsSelected) throw new InvalidOperationException("An option cannot clear Segmented Control selection."); }
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;
            protected override string GetClassNameCore() => "BleeckerSegmentOption";
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.SelectionItem ? this : base.GetPatternCore(pattern);
        }
    }
    private sealed class SegmentPanel : Panel
    {
        private double[] widths = [];
        protected override Size MeasureOverride(Size available)
        {
            foreach (var child in Children) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            widths = BleeckerSegmentedModel.Widths(Children.Select(child => Math.Max(24, child.DesiredSize.Width)).ToArray(), Math.Max(0, available.Width - Math.Max(0, Children.Count - 1) * 2));
            var height = 0d;
            for (var index = 0; index < Children.Count; index++) { Children[index].Measure(new Size(widths[index], double.PositiveInfinity)); height = Math.Max(height, Children[index].DesiredSize.Height); }
            return new Size(widths.Sum() + Math.Max(0, Children.Count - 1) * 2, height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var x = 0d;
            // Coordinates here are logical. WinUI mirrors the RTL visual at
            // the flow-direction boundary; reversing again would cancel it.
            for (var index = 0; index < Children.Count; index++) { var height = Children[index].DesiredSize.Height; Children[index].Arrange(new Rect(x, (final.Height - height) / 2, widths[index], height)); x += widths[index] + 2; }
            return final;
        }
    }
}
