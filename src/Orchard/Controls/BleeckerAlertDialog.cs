using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerAlertDialogVariant { Default, Destructive }

/// <summary>A caller-controlled confirmation surface hosted by the window's BleeckerModalHost.</summary>
public sealed partial class BleeckerAlertDialog : Control
{
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly UISettings settings = new();
    private readonly HashSet<ICommand> commands = new(ReferenceEqualityComparer.Instance);
    private BleeckerAlertDialogPresence? presence;
    private BleeckerAlertDialogMotion? motion;
    private BleeckerModalHost.Registration? registration;
    private DispatcherQueueTimer? timer;
    private XamlRoot? observedRoot;
    private Grid? declaration;
    private bool mountReady, rendering, renderAgain, queued, subscribed, lastVisible;
    private int generation;
    private double visibleDeadline, removeDeadline, motionDeadline;

    public BleeckerAlertDialog()
    {
        DefaultStyleKey = typeof(BleeckerAlertDialog); IsTabStop = false; UseLayoutRounding = false;
        Loaded += LoadedOwner; Unloaded += UnloadedOwner;
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
        foreach (var property in new[] { IsEnabledProperty, FlowDirectionProperty, LanguageProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Refresh());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerAlertDialog), new PropertyMetadata(value, (owner, _) => ((BleeckerAlertDialog)owner).Refresh()));
    public static readonly DependencyProperty IsOpenProperty = Property(nameof(IsOpen), typeof(bool), false);
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string), null);
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(string), null);
    public static readonly DependencyProperty CancelLabelProperty = Property(nameof(CancelLabel), typeof(string), "Cancel");
    public static readonly DependencyProperty ConfirmLabelProperty = Property(nameof(ConfirmLabel), typeof(string), "Continue");
    public static readonly DependencyProperty ConfirmLoadingProperty = Property(nameof(ConfirmLoading), typeof(bool), false);
    public static readonly DependencyProperty VariantProperty = Property(nameof(Variant), typeof(BleeckerAlertDialogVariant), BleeckerAlertDialogVariant.Default);
    public static readonly DependencyProperty SurfaceStyleProperty = Property(nameof(SurfaceStyle), typeof(Style), null);
    public static readonly DependencyProperty CancelCommandProperty = Property(nameof(CancelCommand), typeof(ICommand), null);
    public static readonly DependencyProperty CancelCommandParameterProperty = Property(nameof(CancelCommandParameter), typeof(object), null);
    public static readonly DependencyProperty ConfirmCommandProperty = Property(nameof(ConfirmCommand), typeof(ICommand), null);
    public static readonly DependencyProperty ConfirmCommandParameterProperty = Property(nameof(ConfirmCommandParameter), typeof(object), null);
    public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public string CancelLabel { get => (string)GetValue(CancelLabelProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(CancelLabelProperty, value); } }
    public string ConfirmLabel { get => (string)GetValue(ConfirmLabelProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(ConfirmLabelProperty, value); } }
    public bool ConfirmLoading { get => (bool)GetValue(ConfirmLoadingProperty); set => SetValue(ConfirmLoadingProperty, value); }
    public BleeckerAlertDialogVariant Variant { get => (BleeckerAlertDialogVariant)GetValue(VariantProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(VariantProperty, value); } }
    public Style? SurfaceStyle { get => (Style?)GetValue(SurfaceStyleProperty); set => SetValue(SurfaceStyleProperty, value); }
    public ICommand? CancelCommand { get => (ICommand?)GetValue(CancelCommandProperty); set => SetValue(CancelCommandProperty, value); }
    public object? CancelCommandParameter { get => GetValue(CancelCommandParameterProperty); set => SetValue(CancelCommandParameterProperty, value); }
    public ICommand? ConfirmCommand { get => (ICommand?)GetValue(ConfirmCommandProperty); set => SetValue(ConfirmCommandProperty, value); }
    public object? ConfirmCommandParameter { get => GetValue(ConfirmCommandParameterProperty); set => SetValue(ConfirmCommandParameterProperty, value); }
    public event RoutedEventHandler? CancelRequested;
    public event RoutedEventHandler? ConfirmRequested;
    public void Cancel() => Request(false);
    public void Confirm() => Request(true);
    private void Request(bool confirm)
    {
        if (!IsLoaded || !IsOpen || ConfirmLoading || !IsEnabled || registration?.Layer.Active != true) return;
        var command = confirm ? ConfirmCommand : CancelCommand; var parameter = confirm ? ConfirmCommandParameter : CancelCommandParameter;
        if (command?.CanExecute(parameter) == false) return;
        (confirm ? ConfirmRequested : CancelRequested)?.Invoke(this, new RoutedEventArgs()); command?.Execute(parameter);
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        declaration = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("AlertDialog requires a Grid named PART_Layout."); QueueRefresh();
    }
    private void DetachTemplate() { generation++; mountReady = false; StopPresentation(); declaration = null; }
    private void LoadedOwner(object sender, RoutedEventArgs args)
    {
        if (!subscribed && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) { settings.AnimationsEnabledChanged += AnimationsChanged; subscribed = true; }
        LayoutUpdated += LayoutOwner; QueueRefresh();
    }
    private void UnloadedOwner(object sender, RoutedEventArgs args)
    {
        generation++; mountReady = false; LayoutUpdated -= LayoutOwner; StopPresentation(); DisconnectRoot(); UnhookCommands();
        if (subscribed && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= AnimationsChanged; subscribed = false;
    }
    private void LayoutOwner(object? sender, object args) { if (IsLoaded && !ReferenceEquals(observedRoot, XamlRoot)) QueueRefresh(); }
    private void QueueRefresh()
    {
        if (queued) return; queued = true; var current = generation;
        if (!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () => { queued = false; if (current != generation) { if (IsLoaded) QueueRefresh(); return; } mountReady = true; Refresh(); })) throw new InvalidOperationException("AlertDialog could not queue its presentation.");
    }
    private void DisconnectRoot() { if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { overlay?.InvalidateMeasure(); }
    private void AnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => { if (motion is not null) { var now = clock.Elapsed.TotalMilliseconds; motion.SetVisible(lastVisible, now, reduced: !settings.AnimationsEnabled); motionDeadline = now + Math.Max(motion.Durations.Surface, motion.Durations.Backdrop); UpdatePresentation(); } UpdateSpinner(); });
    private void RewireCommands()
    {
        var next = IsLoaded ? new[] { CancelCommand, ConfirmCommand }.OfType<ICommand>().ToHashSet<ICommand>(ReferenceEqualityComparer.Instance) : [];
        foreach (var old in commands.Where(command => !next.Contains(command)).ToArray()) { old.CanExecuteChanged -= CanExecuteChanged; commands.Remove(old); }
        foreach (var command in next.Where(command => !commands.Contains(command))) { commands.Add(command); command.CanExecuteChanged += CanExecuteChanged; }
    }
    private void CanExecuteChanged(object? sender, EventArgs args) { if (!DispatcherQueue.TryEnqueue(Refresh)) throw new InvalidOperationException("AlertDialog could not update command state."); }
    private void UnhookCommands() { foreach (var command in commands) command.CanExecuteChanged -= CanExecuteChanged; commands.Clear(); }
    private void Refresh()
    {
        RewireCommands(); if (!mountReady || !IsLoaded || declaration is null) return;
        if (rendering) { renderAgain = true; return; } rendering = true;
        try
        {
            if (!Enum.IsDefined(Variant) || CancelLabel is null || ConfirmLabel is null) throw new ArgumentException("AlertDialog requires valid variant and action labels.");
            if (!ReferenceEquals(observedRoot, XamlRoot)) { StopPresentation(); DisconnectRoot(); observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged; }
            if (IsOpen && Title is null) throw new InvalidOperationException("AlertDialog requires Title before opening; an empty title is allowed.");
            if (IsOpen && presence is null) { presence = new(); motion = new(); lastVisible = false; }
            if (presence is not null)
            {
                var now = clock.Elapsed.TotalMilliseconds;
                if (presence.DesiredOpen != IsOpen)
                {
                    presence.SetOpen(IsOpen, now); if (IsOpen) visibleDeadline = now + 20; else removeDeadline = now + 150;
                    if (IsOpen) MountPresentation(); else if (registration is { IsAttached: true, Layer.Active: true }) registration.Host.Deactivate(registration);
                }
                RenderContent(); UpdatePresentation();
            }
        }
        finally { rendering = false; }
        if (renderAgain) { renderAgain = false; Refresh(); }
    }
    private void UpdatePresentation()
    {
        if (presence is null || motion is null) return; var now = clock.Elapsed.TotalMilliseconds; presence.Advance(now);
        if (presence.Visible != lastVisible) { lastVisible = presence.Visible; motion.SetVisible(lastVisible, now, reduced: !settings.AnimationsEnabled); motionDeadline = now + Math.Max(motion.Durations.Surface, motion.Durations.Backdrop); }
        if (!presence.Present) { StopPresentation(); return; }
        ApplyMotion(motion.SampleAt(now));
        if (presence.DesiredOpen && presence.Visible && now >= motionDeadline) { timer?.Stop(); return; }
        var remaining = presence.DesiredOpen && !presence.Visible ? visibleDeadline - now : !presence.DesiredOpen ? removeDeadline - now : motionDeadline - now;
        timer ??= DispatcherQueue.CreateTimer(); timer.IsRepeating = false; timer.Tick -= Tick; timer.Tick += Tick; timer.Interval = TimeSpan.FromMilliseconds(Math.Max(1, Math.Min(16, remaining))); timer.Start();
    }
    private void Tick(DispatcherQueueTimer sender, object args) => UpdatePresentation();
    private void StopPresentation()
    {
        timer?.Stop(); if (timer is not null) timer.Tick -= Tick; timer = null; presence?.Dispose(); presence = null; motion = null;
        registration?.Dispose(); registration = null; ClearPresentation();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new DeclarationPeer(this);
    private sealed class DeclarationPeer(BleeckerAlertDialog owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
        protected override IList<AutomationPeer> GetChildrenCore() => [];
    }
}
