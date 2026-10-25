using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A caller-owned row or column of host-routed navigation links.</summary>
[ContentProperty(Name = nameof(Items))]
public sealed partial class BleeckerNavMenu : Control
{
    private Grid? host;
    private LinkPanel? panel;
    private readonly List<LinkSlot> slots = [];
    private readonly HashSet<BleeckerNavItem> observedItems = new(ReferenceEqualityComparer.Instance);
    private INotifyCollectionChanged? observedCollection;
    private ICommand? observedCommand;
    private readonly ResourceDictionary links = new() { Source = new Uri("ms-appx:///Orchard/Themes/NavMenuLinks.xaml") };
    private readonly UISettings settings = new();
    private readonly Microsoft.UI.Dispatching.DispatcherQueue uiQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Nav Menu requires a UI dispatcher.");
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private TextBlock? hoverProbe;
    private long? hoverToken;
    private Style? sourceStyle;
    private object? styleKey;
    private bool rendering, motionSubscribed;
    private long focusVersion;

    public BleeckerNavMenu()
    {
        DefaultStyleKey = typeof(BleeckerNavMenu); Items = new ObservableCollection<BleeckerNavItem>();
        Loaded += (_, _) => { Observe(); if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += MotionChanged; Render(); };
        Unloaded += (_, _) => { Disconnect(); StopMotion(); if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged; foreach (var slot in slots) { slot.Hovering = false; slot.StopObservingPaint(); slot.Paint(false); slot.Context.Notify(); } };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var property in new[] { FlowDirectionProperty, LanguageProperty, IsEnabledProperty, FontFamilyProperty, FontWeightProperty, ForegroundProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
        ActualThemeChanged += (_, _) => { foreach (var slot in slots) slot.Paint(true); };
        GotFocus += (_, _) => focusVersion++; LostFocus += (_, _) => focusVersion++;
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerNavMenu), new PropertyMetadata(value, (owner, _) => { var menu = (BleeckerNavMenu)owner; if (menu.IsLoaded) menu.Observe(); menu.Render(); }));
    public static readonly DependencyProperty ItemsProperty = Property(nameof(Items), typeof(object), null);
    public static readonly DependencyProperty DirectionProperty = Property(nameof(Direction), typeof(BleeckerNavDirection), BleeckerNavDirection.Horizontal);
    public static readonly DependencyProperty LinkTemplateProperty = Property(nameof(LinkTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty LinkStyleProperty = Property(nameof(LinkStyle), typeof(Style), null);
    public static readonly DependencyProperty CommandProperty = Property(nameof(Command), typeof(ICommand), null);
    public static readonly DependencyProperty CommandParameterProperty = Property(nameof(CommandParameter), typeof(object), null);
    public IEnumerable<BleeckerNavItem> Items { get => GetValue(ItemsProperty) as IEnumerable<BleeckerNavItem> ?? throw new InvalidOperationException("Nav Menu Items must resolve before mounting."); set => SetValue(ItemsProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public BleeckerNavDirection Direction { get => (BleeckerNavDirection)GetValue(DirectionProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(DirectionProperty, value); } }
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public Style? LinkStyle { get => (Style?)GetValue(LinkStyleProperty); set => SetValue(LinkStyleProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event EventHandler<BleeckerNavItem>? ItemInvoked;

    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate(); if (Template is null) return;
        host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("Nav Menu requires PART_Items.");
        panel = new(this) { FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false }; host.Children.Add(panel);
        hoverProbe = (TextBlock)XamlReader.Load("<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{ThemeResource BleeckerSeaBrush}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>");
        host.Children.Add(hoverProbe); hoverToken = hoverProbe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { foreach (var slot in slots) slot.Paint(true); });
        if (IsLoaded) Observe(); Render();
    }
    internal bool CanInvoke(BleeckerNavLinkContext context)
    {
        if (!IsLoaded || !IsEnabled || !context.Mounted || context.Element?.IsLoaded != true || !slots.Any(slot => ReferenceEquals(slot.Context, context)) || !(Command?.CanExecute(CommandParameter) ?? true)) return false;
        var currentHost = false;
        for (DependencyObject? node = context.Element; node is not null; node = VisualTreeHelper.GetParent(node))
        {
            if (node is FrameworkElement element && element.Visibility != Visibility.Visible) return false;
            currentHost |= ReferenceEquals(node, host);
        }
        return currentHost;
    }
    internal void Invoke(BleeckerNavLinkContext context)
    {
        if (!CanInvoke(context)) return;
        var item = context.Item; Command?.Execute(CommandParameter); ItemInvoked?.Invoke(this, item);
    }
    private Style SourceStyle()
    {
        if (LinkStyle is { } provided) return provided;
        var key = new { Direction, family = FontFamily.Source, weight = FontWeight.Weight, Foreground };
        if (!Equals(styleKey, key))
        {
            sourceStyle = new(typeof(HyperlinkButton)) { BasedOn = (Style)links[typeof(HyperlinkButton)] };
            sourceStyle.Setters.Add(new Setter(FontSizeProperty, Direction == BleeckerNavDirection.Horizontal ? 16d : 18d));
            sourceStyle.Setters.Add(new Setter(CharacterSpacingProperty, Direction == BleeckerNavDirection.Horizontal ? -20 : 0));
            sourceStyle.Setters.Add(new Setter(FontFamilyProperty, FontFamily)); sourceStyle.Setters.Add(new Setter(FontWeightProperty, FontWeight)); sourceStyle.Setters.Add(new Setter(ForegroundProperty, Foreground)); styleKey = key;
        }
        return sourceStyle!;
    }
    private void Render()
    {
        if (rendering || panel is null || host is null) return;
        if (GetValue(ItemsProperty) is not IEnumerable<BleeckerNavItem> values) { if (!IsLoaded && GetBindingExpression(ItemsProperty) is not null) return; throw new InvalidOperationException("Nav Menu Items must resolve before mounting."); }
        if (!Enum.IsDefined(Direction)) throw new ArgumentOutOfRangeException(nameof(Direction)); rendering = true;
        try
        {
            var items = values.ToArray(); if (items.Any(item => item is null || item.Href is null || item.Label is null)) throw new ArgumentException("Nav Menu items require original non-null destinations and labels.");
            var focused = XamlRoot is null ? null : Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot) as Control; var focusState = focused?.FocusState ?? FocusState.Unfocused;
            var unused = slots.ToList(); var next = new List<LinkSlot>(); var style = SourceStyle();
            foreach (var item in items)
            {
                var key = BleeckerNavMenuModel.Key(item.Href, item.Label); var slot = unused.FirstOrDefault(slot => slot.Key == key && ReferenceEquals(slot.Template, LinkTemplate));
                if (slot is null) slot = new(this, key, LinkTemplate); else unused.Remove(slot);
                slot.Update(item, style); next.Add(slot);
            }
            foreach (var slot in unused) { slot.Retire(); panel.Children.Remove(slot.Root); }
            if (!slots.SequenceEqual(next)) { slots.Clear(); slots.AddRange(next); }
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index]; var current = panel.Children.IndexOf(slot.Root); if (current != index) { if (current >= 0) panel.Children.RemoveAt(current); panel.Children.Insert(index, slot.Root); }
                slot.Context.Mounted = true; slot.Context.Notify(); slot.Paint(false);
            }
            panel.InvalidateMeasure(); InvalidateMeasure();
            if (focused is not null && focusState != FocusState.Unfocused && slots.Any(slot => DescendsFrom(focused, slot.Root)) && !ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), focused))
            {
                var displaced = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot); var version = focusVersion;
                DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (IsLoaded && focused.IsLoaded && focusVersion == version && slots.Any(slot => DescendsFrom(focused, slot.Root)) && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), displaced)) focused.Focus(focusState); });
            }
        }
        finally { rendering = false; }
    }
    private static bool DescendsFrom(DependencyObject target, DependencyObject root) { for (DependencyObject? node = target; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, root)) return true; return false; }
    private void Observe()
    {
        Disconnect(); observedCollection = Items as INotifyCollectionChanged; if (observedCollection is not null) observedCollection.CollectionChanged += ProducerChanged;
        foreach (var item in Items) { if (item is null) throw new ArgumentException("Nav Menu items cannot be null."); if (observedItems.Add(item)) item.Changed += ItemChanged; }
        observedCommand = Command; if (observedCommand is not null) observedCommand.CanExecuteChanged += CommandChanged;
    }
    private void Disconnect()
    {
        if (observedCollection is not null) observedCollection.CollectionChanged -= ProducerChanged; observedCollection = null;
        foreach (var item in observedItems) item.Changed -= ItemChanged; observedItems.Clear();
        if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged; observedCommand = null;
    }
    private void ProducerChanged(object? sender, NotifyCollectionChangedEventArgs args) => Dispatch(() => { Observe(); Render(); });
    private void ItemChanged(object? sender, EventArgs args) => Dispatch(Render);
    private void CommandChanged(object? sender, EventArgs args) => Dispatch(() => { foreach (var slot in slots) slot.Context.Notify(); });
    private void Dispatch(Action action)
    {
        if (uiQueue.HasThreadAccess) { if (IsLoaded) action(); }
        else if (!uiQueue.TryEnqueue(() => { if (IsLoaded) action(); })) throw new InvalidOperationException("Nav Menu could not dispatch a producer update.");
    }
    private void MotionChanged(UISettings sender, object args) => uiQueue.TryEnqueue(() => { if (IsLoaded) { foreach (var slot in slots) slot.Paint(false); StopMotion(); } });
    private void EnsureMotion() { if (motionSubscribed || !IsLoaded) return; motionSubscribed = true; CompositionTarget.Rendering += MotionFrame; }
    private void StopMotion() { if (!motionSubscribed) return; CompositionTarget.Rendering -= MotionFrame; motionSubscribed = false; }
    private void MotionFrame(object? sender, object args) { foreach (var slot in slots) slot.Present(); if (!slots.Any(slot => slot.Running)) StopMotion(); }
    private void Detach() { Disconnect(); StopMotion(); foreach (var slot in slots) slot.Retire(); slots.Clear(); panel?.Children.Clear(); if (hoverProbe is not null && hoverToken is { } token) hoverProbe.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, token); hoverToken = null; host?.Children.Clear(); panel = null; host = null; hoverProbe = null; sourceStyle = null; styleKey = null; }
    protected override AutomationPeer OnCreateAutomationPeer() => new NavPeer(this);
    private sealed class NavPeer(BleeckerNavMenu owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerNavMenu);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetLocalizedControlTypeCore() => "navigation";
        protected override string GetNameCore() => AutomationProperties.GetName(owner) is { Length: > 0 } name ? name : "Navigation";
    }
}
