using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A controlled sequence of native step buttons. Invocation never advances ActiveStep.</summary>
[ContentProperty(Name = nameof(Steps))]
public sealed partial class BleeckerStepper : Control
{
    private Grid? host;
    private StepperLayout? layout;
    private readonly List<Entry> entries = [];
    private readonly HashSet<BleeckerStep> observed = [];
    private ObservableCollection<BleeckerStep>? observedCollection;
    private ICommand? observedCommand;
    private EventHandler<BleeckerStep>? invoked;
    private readonly UISettings settings = new();
    private readonly ResourceDictionary actions = new() { Source = new Uri("ms-appx:///Orchard/Themes/StepperActions.xaml") };
    private readonly Dictionary<string, TextBlock> palette = [];
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> paletteObservers = [];
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private bool rendering;
    private double textScale = 1;

    public BleeckerStepper()
    {
        DefaultStyleKey = typeof(BleeckerStepper);
        Steps = [];
        Loaded += (_, _) =>
        {
            settings.ColorValuesChanged += ColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += MotionChanged;
            Observe(); ObservePalette(); Render(false);
        };
        Unloaded += (_, _) =>
        {
            settings.ColorValuesChanged -= ColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= MotionChanged;
            StopObserving(); StopPalette(); StopRendering();
            foreach (var entry in entries) entry.DetachPointer();
            DispatcherQueue.TryEnqueue(() => { if (IsLoaded) { Observe(); ObservePalette(); Render(false); } });
        };
        ActualThemeChanged += (_, _) => Render(false);
        LayoutUpdated += (_, _) =>
        {
            if (textScale != settings.TextScaleFactor) { textScale = settings.TextScaleFactor; layout?.InvalidateMeasure(); }
            foreach (var entry in entries) entry.AlignText();
        };
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, IsTextScaleFactorEnabledProperty, CharacterSpacingProperty, FlowDirectionProperty, IsEnabledProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render(false));
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }

    public static readonly DependencyProperty StepsProperty = DependencyProperty.Register(nameof(Steps), typeof(object), typeof(BleeckerStepper), new PropertyMetadata(null, Changed));
    public ObservableCollection<BleeckerStep> Steps
    {
        get => GetValue(StepsProperty) as ObservableCollection<BleeckerStep> ?? throw new InvalidOperationException("Stepper Steps must resolve before mounting.");
        set => SetValue(StepsProperty, value ?? throw new ArgumentNullException(nameof(value)));
    }
    public static readonly DependencyProperty ActiveStepProperty = DependencyProperty.Register(nameof(ActiveStep), typeof(string), typeof(BleeckerStepper), new PropertyMetadata(null, Changed));
    public string? ActiveStep { get => (string?)GetValue(ActiveStepProperty); set => SetValue(ActiveStepProperty, value); }
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(BleeckerStepper), new PropertyMetadata(Orientation.Horizontal, Changed));
    public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public static readonly DependencyProperty StepCommandProperty = DependencyProperty.Register(nameof(StepCommand), typeof(ICommand), typeof(BleeckerStepper), new PropertyMetadata(null, Changed));
    /// <summary>Receives the original BleeckerStep as its command parameter.</summary>
    public ICommand? StepCommand { get => (ICommand?)GetValue(StepCommandProperty); set => SetValue(StepCommandProperty, value); }
    public event EventHandler<BleeckerStep>? StepInvoked
    {
        add { invoked += value; Render(false); }
        remove { invoked -= value; Render(false); }
    }

    private static void Changed(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (BleeckerStepper)sender;
        if (args.Property == StepsProperty && args.NewValue is not ObservableCollection<BleeckerStep> &&
            !(args.NewValue is null && !owner.IsLoaded && owner.GetBindingExpression(StepsProperty) is not null))
            throw new ArgumentException("Stepper Steps requires a non-null ObservableCollection<BleeckerStep>.");
        if (args.Property == OrientationProperty && (args.NewValue is not Orientation orientation || !Enum.IsDefined(orientation))) throw new ArgumentOutOfRangeException(nameof(Orientation));
        if (owner.IsLoaded) owner.Observe();
        owner.Render(true);
    }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        if (Template is null) return;
        host = GetTemplateChild("PART_Items") as Grid ?? throw new InvalidOperationException("Stepper requires PART_Items.");
        foreach (var name in new[] { "Surface", "Accent", "OnAccent", "Error", "PendingRing", "Line", "Primary", "Secondary" })
            palette[name] = GetTemplateChild("PART_" + name) as TextBlock ?? throw new InvalidOperationException("Stepper requires PART_" + name + ".");
        layout = new StepperLayout(this); host.Children.Add(layout);
        if (IsLoaded) ObservePalette();
        Render(false);
    }

    private void DetachTemplate()
    {
        StopRendering(); StopPalette();
        foreach (var entry in entries) entry.Dispose();
        entries.Clear(); layout?.Children.Clear(); host?.Children.Clear();
        host = null; layout = null; palette.Clear();
    }

    private void Observe()
    {
        StopObserving();
        if (GetValue(StepsProperty) is not ObservableCollection<BleeckerStep> steps) return;
        observedCollection = steps; steps.CollectionChanged += CollectionChanged;
        foreach (var step in steps)
        {
            if (step is null) throw new ArgumentException("Stepper steps cannot contain null.");
            if (observed.Add(step)) step.PropertyChanged += ItemChanged;
        }
        if (StepCommand is { } command) { observedCommand = command; command.CanExecuteChanged += CommandChanged; }
    }
    private void StopObserving()
    {
        if (observedCollection is not null) observedCollection.CollectionChanged -= CollectionChanged;
        foreach (var step in observed) step.PropertyChanged -= ItemChanged;
        if (observedCommand is not null) observedCommand.CanExecuteChanged -= CommandChanged;
        observedCollection = null; observedCommand = null; observed.Clear();
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) { Observe(); Render(true); }
    private void ItemChanged(object? sender, PropertyChangedEventArgs args) => Render(true);
    private void CommandChanged(object? sender, EventArgs args) => Render(false);
    private bool CanInvoke(BleeckerStep step) => IsLoaded && IsEnabled && !step.IsDisabled &&
        (invoked is not null || StepCommand is not null) && (StepCommand?.CanExecute(step) ?? true);
    private void Invoke(Entry entry)
    {
        if (layout is null || !entries.Contains(entry) || !Steps.Contains(entry.Step) || !CanInvoke(entry.Step)) return;
        var step = entry.Step;
        StepCommand?.Execute(step);
        invoked?.Invoke(this, step);
    }

    private void Render(bool animate)
    {
        if (layout is null || GetValue(StepsProperty) is not ObservableCollection<BleeckerStep> steps) return;
        var unused = entries.ToList(); var next = new List<Entry>();
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i] ?? throw new ArgumentException("Stepper steps cannot contain null.");
            var at = unused.FindIndex(entry => entry.Key == step.Id);
            var entry = at < 0 ? new Entry(this, step) : unused[at];
            if (at >= 0) unused.RemoveAt(at);
            entry.Update(step, i, steps.Count, BleeckerStepperState.Resolve(steps, i, ActiveStep), animate);
            next.Add(entry);
        }
        foreach (var old in unused) { old.Dispose(); layout.Children.Remove(old); }
        entries.Clear(); entries.AddRange(next);
        for (var i = 0; i < entries.Count; i++)
        {
            var child = entries[i];
            if (i < layout.Children.Count && ReferenceEquals(layout.Children[i], child)) continue;
            layout.Children.Remove(child); layout.Children.Insert(i, child);
        }
        layout.InvalidateMeasure();
    }

    private Brush Paint(string name) => palette[name].Foreground;
    private void ObservePalette()
    {
        StopPalette();
        foreach (var probe in palette.Values)
        {
            paletteObservers.Add((probe, TextBlock.ForegroundProperty, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { ObservePalette(); Render(false); })));
            if (probe.Foreground is SolidColorBrush brush)
                foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                    paletteObservers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => Render(false))));
        }
    }
    private void StopPalette()
    {
        foreach (var (owner, property, token) in paletteObservers) owner.UnregisterPropertyChangedCallback(property, token);
        paletteObservers.Clear();
    }
    private void ColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(() => Render(false));
    private void MotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => Render(false));
    private void StartRendering()
    {
        if (rendering || !IsLoaded) return;
        rendering = true; CompositionTarget.Rendering += OnRendering;
    }
    private void StopRendering() { if (rendering) CompositionTarget.Rendering -= OnRendering; rendering = false; }
    private void OnRendering(object? sender, object args)
    {
        var now = clock.Elapsed.TotalMilliseconds;
        foreach (var entry in entries) entry.Animate(now);
        if (!entries.Any(entry => entry.IsAnimating(now))) StopRendering();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new StepperPeer(this);
    private sealed class StepperPeer(BleeckerStepper owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerStepper);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
