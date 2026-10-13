using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

/// <summary>Overlapping consumer-owned avatars and the pinned overflow count.</summary>
[ContentProperty(Name = nameof(Avatars))]
public sealed class BleeckerAvatarGroup : Control
{
    private AvatarPanel? panel;
    private Grid? host;
    private readonly List<BleeckerAvatar> positions = [];
    private readonly BleeckerAvatar overflow = new();
    private readonly HashSet<BleeckerAvatarData> observed = [];
    private bool observing;

    public BleeckerAvatarGroup()
    {
        DefaultStyleKey = typeof(BleeckerAvatarGroup);
        Avatars = new ObservableCollection<BleeckerAvatarData>();
        Loaded += (_, _) => { StartObserving(); Synchronize(); };
        Unloaded += (_, _) => StopObserving();
    }
    public static readonly DependencyProperty AvatarsProperty = DependencyProperty.Register(nameof(Avatars), typeof(object), typeof(BleeckerAvatarGroup),
        new PropertyMetadata(null, (owner, args) =>
        {
            var group = (BleeckerAvatarGroup)owner;
            // ElementName bindings can supply null before the XAML namescope
            // resolves. This is an unmounted binding placeholder, not data.
            if (args.NewValue is null && !group.IsLoaded && group.GetBindingExpression(AvatarsProperty) is not null)
            {
                group.StopObserving(args.OldValue as ObservableCollection<BleeckerAvatarData>);
                group.Synchronize();
                return;
            }
            if (args.NewValue is not ObservableCollection<BleeckerAvatarData>)
                throw new ArgumentException($"Avatars requires a non-null ObservableCollection<BleeckerAvatarData>; received {args.NewValue?.GetType().FullName ?? "<null>"}, loaded={group.IsLoaded}, binding={group.GetBindingExpression(AvatarsProperty) is not null}.");
            group.StopObserving(args.OldValue as ObservableCollection<BleeckerAvatarData>);
            if (group.IsLoaded) group.StartObserving();
            group.Synchronize();
        }));
    public ObservableCollection<BleeckerAvatarData> Avatars
    {
        get => GetValue(AvatarsProperty) as ObservableCollection<BleeckerAvatarData>
            ?? throw UnresolvedCollection();
        set => SetValue(AvatarsProperty, value ?? throw new ArgumentNullException(nameof(value)));
    }
    private InvalidOperationException UnresolvedCollection()
    {
        var binding = GetBindingExpression(AvatarsProperty);
        return new InvalidOperationException($"Avatars must resolve to a non-null ObservableCollection<BleeckerAvatarData> before the group is loaded; name={Name}, element={binding?.ParentBinding.ElementName}, path={binding?.ParentBinding.Path?.Path}, sourceType={binding?.DataItem?.GetType().FullName ?? "<null>"}.");
    }
    public static readonly DependencyProperty MaxProperty = DependencyProperty.Register(nameof(Max), typeof(int), typeof(BleeckerAvatarGroup),
        new PropertyMetadata(4, OnChanged));
    public int Max { get => (int)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }
    public static readonly DependencyProperty AvatarSizeProperty = DependencyProperty.Register(nameof(AvatarSize), typeof(BleeckerAvatarSize), typeof(BleeckerAvatarGroup),
        new PropertyMetadata(BleeckerAvatarSize.Medium, OnChanged));
    public BleeckerAvatarSize AvatarSize { get => (BleeckerAvatarSize)GetValue(AvatarSizeProperty); set => SetValue(AvatarSizeProperty, value); }
    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerAvatarGroup)owner).Synchronize();

    protected override void OnApplyTemplate()
    {
        if (panel is not null) { panel.Children.Clear(); host?.Children.Remove(panel); }
        panel = null; host = null;
        base.OnApplyTemplate();
        if (Template is null) return;
        host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("AvatarGroup template requires PART_Items.");
        panel = new AvatarPanel();
        host.Children.Add(panel);
        Synchronize();
    }
    private void StartObserving()
    {
        if (!observing) { Avatars.CollectionChanged += OnCollectionChanged; observing = true; }
        ObserveItems();
    }
    private void StopObserving(ObservableCollection<BleeckerAvatarData>? previous = null)
    {
        if (observing) (previous ?? Avatars).CollectionChanged -= OnCollectionChanged;
        observing = false;
        foreach (var item in observed) item.Changed -= OnItemChanged;
        observed.Clear();
    }
    private void ObserveItems()
    {
        foreach (var item in observed) item.Changed -= OnItemChanged;
        observed.Clear();
        foreach (var item in Avatars)
        {
            if (item is null) throw new ArgumentException("Avatar entries must not be null.");
            if (observed.Add(item)) item.Changed += OnItemChanged;
        }
    }
    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs _) { ObserveItems(); Synchronize(); }
    private void OnItemChanged(object? sender, EventArgs _) => Synchronize();
    private void Synchronize()
    {
        _ = BleeckerAvatar.Metrics(AvatarSize);
        if (panel is null) return;
        var avatars = GetValue(AvatarsProperty) as ObservableCollection<BleeckerAvatarData>;
        if (avatars is null)
        {
            if (IsLoaded) _ = Avatars; // Required-data validation, without empty-data substitution.
            panel.Children.Clear();
            return;
        }
        var count = Max >= 0 ? Math.Min(avatars.Count, Max) : (int)Math.Max(0, (long)avatars.Count + Max);
        var excess = (long)avatars.Count - Max;
        // React uses index keys. Keep the same native component at each retained
        // position, including its image-error latch when consumer data changes.
        while (positions.Count > count) { panel.Children.Remove(positions[^1]); positions.RemoveAt(positions.Count - 1); }
        panel.Children.Remove(overflow);
        for (var index = 0; index < count; index++)
        {
            if (index == positions.Count) { var added = new BleeckerAvatar(); added.SetGroupPresentation(false); positions.Add(added); panel.Children.Add(added); }
            var item = avatars[index] ?? throw new ArgumentException("Avatar entries must not be null.");
            var avatar = positions[index];
            avatar.AvatarSize = AvatarSize; avatar.Source = item.Source; avatar.Alt = item.Alt; avatar.Fallback = item.Fallback;
            if (!panel.Children.Contains(avatar)) panel.Children.Add(avatar);
        }
        if (excess > 0) { overflow.AvatarSize = AvatarSize; overflow.SetGroupPresentation(true, excess); panel.Children.Add(overflow); }
        panel.InvalidateMeasure();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BleeckerAvatarGroupAutomationPeer(this);

    private sealed class AvatarPanel : Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            foreach (var child in Children) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            return new Size(Children.Sum(child => child.DesiredSize.Width) - Math.Max(0, Children.Count - 1) * 8,
                Children.Count == 0 ? 0 : Children.Max(child => child.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var offset = 0d;
            foreach (var child in Children) { child.Arrange(new Rect(offset, 0, child.DesiredSize.Width, child.DesiredSize.Height)); offset += child.DesiredSize.Width - 8; }
            return finalSize;
        }
    }
}

internal sealed class BleeckerAvatarGroupAutomationPeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => nameof(BleeckerAvatarGroup);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    protected override bool IsControlElementCore() => false;
    protected override bool IsContentElementCore() => false;
}
