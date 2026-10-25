using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Dispatching;

namespace Orchard;

// Keep the viewport recipe out of the local-value store. Suppress the private
// state while the caller owns the minimum, including equal-valued overrides.
// WinUI emits no value-change notification when local zero is cleared to its
// zero metadata default; only that loaded boundary needs a short observation.
internal sealed class BleeckerViewportMinimum
{
    private readonly Control owner;
    private readonly FrameworkElement templateRoot;
    private readonly VisualStateGroup group;
    private readonly VisualState viewport, caller;
    private double? height;
    private bool initialized, updating;
    private DispatcherQueueTimer? zeroObserver;
    internal long ObservationCount { get; private set; }

    internal BleeckerViewportMinimum(Control owner)
    {
        this.owner = owner;
        templateRoot = (FrameworkElement)VisualTreeHelper.GetChild(owner, 0);
        var id = Guid.NewGuid().ToString("N");
        group = (VisualStateGroup)XamlReader.Load($"<VisualStateGroup xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' x:Name='ViewportGroup{id}'><VisualState x:Name='ViewportDefault{id}'/><VisualState x:Name='ViewportCaller{id}'/></VisualStateGroup>");
        viewport = group.States[0]; caller = group.States[1];
        VisualStateManager.GetVisualStateGroups(templateRoot).Add(group);
    }
    private bool Styled()
    {
        for (var style = owner.Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == FrameworkElement.MinHeightProperty)) return true;
        return false;
    }
    internal void Update()
    {
        if (updating) return;
        if (owner.XamlRoot is null) { Suspend(); return; }
        var local = owner.ReadLocalValue(FrameworkElement.MinHeightProperty) != DependencyProperty.UnsetValue || owner.GetBindingExpression(FrameworkElement.MinHeightProperty) is not null;
        var styled = Styled();
        WatchZero(local && !styled && owner.MinHeight == 0);
        double? next = local || styled ? null : owner.XamlRoot.Size.Height;
        if (initialized && next == height) return;
        updating = true;
        try
        {
            if (!VisualStateManager.GoToState(owner, caller.Name, false)) throw new InvalidOperationException("Viewport minimum caller state is unavailable.");
            viewport.Setters.Clear();
            if (next is { } value)
            {
                viewport.Setters.Add(new Setter { Target = new TargetPropertyPath { Target = owner, Path = new PropertyPath("MinHeight") }, Value = value });
                if (!VisualStateManager.GoToState(owner, viewport.Name, false)) throw new InvalidOperationException("Viewport minimum default state is unavailable.");
            }
            height = next; initialized = true;
        }
        finally { updating = false; }
    }
    private void WatchZero(bool enabled)
    {
        if (!enabled || !owner.IsLoaded) { Suspend(); return; }
        if (zeroObserver is not null) return;
        zeroObserver = owner.DispatcherQueue.CreateTimer();
        zeroObserver.Interval = TimeSpan.FromMilliseconds(16); zeroObserver.IsRepeating = true;
        zeroObserver.Tick += ObserveZero; zeroObserver.Start();
    }
    private void ObserveZero(DispatcherQueueTimer sender, object args)
    {
        if (!owner.IsLoaded || owner.XamlRoot is null) { Suspend(); return; }
        ObservationCount++;
        if (owner.ReadLocalValue(FrameworkElement.MinHeightProperty) == DependencyProperty.UnsetValue && owner.GetBindingExpression(FrameworkElement.MinHeightProperty) is null) Update();
    }
    internal void Suspend()
    {
        if (zeroObserver is null) return;
        zeroObserver.Stop(); zeroObserver.Tick -= ObserveZero; zeroObserver = null;
    }
    internal void Release()
    {
        Suspend();
        if (updating) return;
        updating = true;
        try { VisualStateManager.GoToState(owner, caller.Name, false); viewport.Setters.Clear(); VisualStateManager.GetVisualStateGroups(templateRoot).Remove(group); }
        finally { updating = false; }
    }
}
