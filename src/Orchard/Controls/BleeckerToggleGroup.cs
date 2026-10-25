using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Orchard;

public enum BleeckerSelectionMode { Single, Multiple }
public enum BleeckerToggleVariant { Default, Outline }
public enum BleeckerControlSize { Small, Medium, Large }

/// <summary>A value-based group of consumer-owned toggle items.</summary>
public sealed class BleeckerToggleGroup : ItemsControl
{
    private readonly HashSet<BleeckerToggleGroupItem> containers = [];
    private bool synchronizing;

    public BleeckerToggleGroup()
    {
        DefaultStyleKey = typeof(BleeckerToggleGroup);
        SelectedValues = new ObservableCollection<string>();
        KeyDown += OnKeyDown;
        Loaded += (_, _) =>
        {
            SelectedValues.CollectionChanged -= OnCollectionChanged;
            SelectedValues.CollectionChanged += OnCollectionChanged;
            Synchronize();
        };
        Unloaded += (_, _) => SelectedValues.CollectionChanged -= OnCollectionChanged;
    }

    public static readonly DependencyProperty SelectionModeProperty = DependencyProperty.Register(nameof(SelectionMode), typeof(BleeckerSelectionMode), typeof(BleeckerToggleGroup), new PropertyMetadata(BleeckerSelectionMode.Single, OnChanged));
    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(nameof(SelectedValue), typeof(string), typeof(BleeckerToggleGroup), new PropertyMetadata(null, OnChanged));
    public static readonly DependencyProperty SelectedValuesProperty = DependencyProperty.Register(nameof(SelectedValues), typeof(object), typeof(BleeckerToggleGroup), new PropertyMetadata(null, OnValuesChanged));
    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(BleeckerToggleVariant), typeof(BleeckerToggleGroup), new PropertyMetadata(BleeckerToggleVariant.Default, OnChanged));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(BleeckerControlSize), typeof(BleeckerToggleGroup), new PropertyMetadata(BleeckerControlSize.Medium, OnChanged));

    public BleeckerSelectionMode SelectionMode { get => (BleeckerSelectionMode)GetValue(SelectionModeProperty); set => SetValue(SelectionModeProperty, value); }
    public string? SelectedValue { get => (string?)GetValue(SelectedValueProperty); set => SetValue(SelectedValueProperty, value); }
    public ObservableCollection<string> SelectedValues { get => (ObservableCollection<string>)GetValue(SelectedValuesProperty); set => SetValue(SelectedValuesProperty, value); }
    public BleeckerToggleVariant Variant { get => (BleeckerToggleVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerControlSize Size { get => (BleeckerControlSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public event EventHandler? SelectionChanged;

    protected override bool IsItemItsOwnContainerOverride(object item) => item is BleeckerToggleGroupItem;
    protected override DependencyObject GetContainerForItemOverride() => throw new InvalidOperationException("Toggle groups accept BleeckerToggleGroupItem children. Bind ItemsSource to a collection of those items.");

    protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        var toggle = (BleeckerToggleGroupItem)element;
        if (containers.Add(toggle))
        {
            toggle.Group = this;
            toggle.Checked += OnToggled;
            toggle.Unchecked += OnToggled;
            toggle.Indeterminate += OnToggled;
        }
        Synchronize();
    }

    protected override void ClearContainerForItemOverride(DependencyObject element, object item)
    {
        var toggle = (BleeckerToggleGroupItem)element;
        toggle.Checked -= OnToggled;
        toggle.Unchecked -= OnToggled;
        toggle.Indeterminate -= OnToggled;
        toggle.Group = null;
        if (ReferenceEquals(toggle.Style, toggle.AssignedGroupStyle)) toggle.ClearValue(StyleProperty);
        toggle.AssignedGroupStyle = null;
        containers.Remove(toggle);
        base.ClearContainerForItemOverride(element, item);
    }

    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var group = (BleeckerToggleGroup)owner;
        group.Synchronize();
        if ((args.Property == SelectedValueProperty && group.SelectionMode == BleeckerSelectionMode.Single) || args.Property == SelectionModeProperty) group.NotifySelection();
    }

    private static void OnValuesChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var group = (BleeckerToggleGroup)owner;
        if (args.OldValue is ObservableCollection<string> oldValues) oldValues.CollectionChanged -= group.OnCollectionChanged;
        if (args.NewValue is not ObservableCollection<string> values) throw new ArgumentException("SelectedValues must be a non-null ObservableCollection<string>.");
        if (group.IsLoaded) values.CollectionChanged += group.OnCollectionChanged;
        group.Synchronize();
        if (group.SelectionMode == BleeckerSelectionMode.Multiple) group.NotifySelection();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        Synchronize();
        if (SelectionMode == BleeckerSelectionMode.Multiple) NotifySelection();
    }

    internal void Synchronize()
    {
        if (synchronizing) return;
        if (!Enum.IsDefined(SelectionMode) || !Enum.IsDefined(Variant) || !Enum.IsDefined(Size)) throw new ArgumentException("Unknown toggle group configuration.");
        if (containers.Any(item => string.IsNullOrWhiteSpace(item.Value)) || containers.Select(item => item.Value).Distinct(StringComparer.Ordinal).Count() != containers.Count)
            throw new InvalidOperationException("Toggle group values must be nonempty and unique.");
        if (SelectedValues is { } values && (values.Any(string.IsNullOrWhiteSpace) || values.Distinct(StringComparer.Ordinal).Count() != values.Count))
            throw new InvalidOperationException("SelectedValues must contain unique, nonempty values.");
        synchronizing = true;
        try
        {
            var sizeName = Size == BleeckerControlSize.Medium ? "" : Size.ToString();
            var variantName = Variant == BleeckerToggleVariant.Outline ? "Outline" : "";
            Style? groupStyle = null;
            foreach (var item in containers)
            {
                if (item.ReadLocalValue(StyleProperty) == DependencyProperty.UnsetValue || ReferenceEquals(item.Style, item.AssignedGroupStyle))
                {
                    if (groupStyle is null)
                    {
                        groupStyle = new Style(typeof(BleeckerToggleGroupItem))
                        {
                            BasedOn = (Style)Application.Current.Resources[$"Bleecker{sizeName}{variantName}ToggleStyle"]
                        };
                        groupStyle.Setters.Add(new Setter(CornerRadiusProperty, new CornerRadius(8)));
                    }
                    item.AssignedGroupStyle = groupStyle;
                    item.Style = item.AssignedGroupStyle;
                }
                item.IsThreeState = false;
                item.IsChecked = SelectionMode == BleeckerSelectionMode.Single ? SelectedValue == item.Value : SelectedValues?.Contains(item.Value) == true;
            }
        }
        finally { synchronizing = false; }
    }

    private void OnToggled(object sender, RoutedEventArgs args)
    {
        if (synchronizing) return;
        var item = (BleeckerToggleGroupItem)sender;
        if (!Owns(item)) return;
        SetSelected(item, item.IsChecked == true, exclusive: false);
    }

    internal bool Owns(BleeckerToggleGroupItem item) => IsLoaded && item.IsLoaded && ReferenceEquals(item.Group, this) && containers.Contains(item);

    internal void SetSelected(BleeckerToggleGroupItem item, bool selected, bool exclusive)
    {
        if (!Owns(item)) throw new Microsoft.UI.Xaml.Automation.ElementNotAvailableException();
        if (SelectionMode == BleeckerSelectionMode.Single)
            SelectedValue = selected ? item.Value : SelectedValue == item.Value ? null : SelectedValue;
        else
        {
            if (exclusive) SelectedValues.Clear();
            if (selected && !SelectedValues.Contains(item.Value)) SelectedValues.Add(item.Value);
            else if (!selected) SelectedValues.Remove(item.Value);
        }
        Synchronize();
    }

    private void NotifySelection()
    {
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        FrameworkElementAutomationPeer.FromElement(this)?.RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key is not (VirtualKey.Left or VirtualKey.Right or VirtualKey.Home or VirtualKey.End)) return;
        var candidates = Items.OfType<BleeckerToggleGroupItem>().Where(item => item.IsEnabled && item.Visibility == Visibility.Visible).ToArray();
        var current = Array.FindIndex(candidates, item => item.FocusState != FocusState.Unfocused);
        if (current < 0 || candidates.Length == 0) return;
        var forward = (args.Key == VirtualKey.Right) != (FlowDirection == FlowDirection.RightToLeft);
        var next = args.Key switch { VirtualKey.Home => 0, VirtualKey.End => candidates.Length - 1, _ => (current + (forward ? 1 : candidates.Length - 1)) % candidates.Length };
        candidates[next].Focus(FocusState.Keyboard);
        candidates[next].StartBringIntoView();
        args.Handled = true;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);

    private sealed class GroupPeer(BleeckerToggleGroup group) : FrameworkElementAutomationPeer(group), ISelectionProvider
    {
        public bool CanSelectMultiple => group.SelectionMode == BleeckerSelectionMode.Multiple;
        public bool IsSelectionRequired => false;
        public IRawElementProviderSimple[] GetSelection() => group.containers.Where(item => item.IsChecked == true)
            .Select(item => ProviderFromPeer(CreatePeerForElement(item))).ToArray();
        protected override string GetClassNameCore() => nameof(BleeckerToggleGroup);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override object GetPatternCore(PatternInterface patternInterface) => patternInterface == PatternInterface.Selection ? this : base.GetPatternCore(patternInterface);
    }
}
