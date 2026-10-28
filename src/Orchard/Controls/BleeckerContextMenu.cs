using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A native context flyout with Bleecker's distinct 160-DIP context-menu recipe.</summary>
/// <remarks>Assign to FrameworkElement.ContextFlyout. Native items retain caller commands and parameters.</remarks>
public sealed partial class BleeckerContextMenu : MenuFlyout
{
    private MenuFlyoutPresenter? presenter;
    private PortalSession? portal;
    private FrameworkElement? nativeTarget;
    private XamlRoot? nativeRoot;
    private object? priorNativePassThrough;
    private bool nativePassThroughActive;
    public static readonly DependencyProperty IsModalProperty = DependencyProperty.Register(
        nameof(IsModal), typeof(bool), typeof(BleeckerContextMenu), new PropertyMetadata(true, (owner, _) => ((BleeckerContextMenu)owner).ModalityChanged()));
    /// <summary>Whether the open menu traps input; defaults to the source root's modal behavior.</summary>
    public bool IsModal { get => (bool)GetValue(IsModalProperty); set => SetValue(IsModalProperty, value); }
    public BleeckerContextMenu()
    {
        var resources = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Orchard.xaml") };
        MenuFlyoutPresenterStyle = (Style)resources["BleeckerContextMenuPresenterStyle"];
        ShouldConstrainToRootBounds = false;
        AreOpenCloseAnimationsEnabled = false;
        Opening += (_, _) =>
        {
            StopNativeTargetObservation();
            portal?.Dispose(); portal = null;
            if (Target is not { } target) throw new InvalidOperationException("Context Menu requires its native target before opening.");
            var host = BleeckerModalHost.Find(target);
            if (host?.ActiveSurface is not null)
                portal = new PortalSession(this, presenter ?? throw new InvalidOperationException("Context Menu has no native presenter."), target, host);
            else
            {
                nativeTarget = target;
                nativeRoot = target.XamlRoot ?? throw new InvalidOperationException("Context Menu requires the target's actual XamlRoot.");
                if (!IsModal) EnableNativePassThrough();
                target.Unloaded += NativeTargetUnloaded;
            }
        };
        Opened += (_, _) => portal?.QueueUpdate();
        Closed += (_, _) => { StopNativeTargetObservation(); var retired = portal; portal = null; retired?.Dispose(); };
    }

    private void ModalityChanged()
    {
        if (!IsOpen || Target is not { } target || presenter is null) return;
        if (portal is null && nativeTarget is not null)
        {
            if (IsModal) RestoreNativePassThrough(); else EnableNativePassThrough();
            return;
        }
        StopNativeTargetObservation();
        portal ??= new PortalSession(this, presenter, target, BleeckerModalHost.Find(target));
        portal.ModalityChanged();
    }

    private void StopNativeTargetObservation()
    {
        RestoreNativePassThrough();
        if (nativeTarget is not null) nativeTarget.Unloaded -= NativeTargetUnloaded;
        nativeTarget = null; nativeRoot = null;
    }

    private void EnableNativePassThrough()
    {
        if (nativePassThroughActive) return;
        var input = nativeRoot?.Content ?? throw new InvalidOperationException("Nonmodal Context Menu requires root content.");
        priorNativePassThrough = ReadLocalValue(OverlayInputPassThroughElementProperty);
        OverlayInputPassThroughElement = input;
        nativePassThroughActive = true;
    }

    private void RestoreNativePassThrough()
    {
        if (!nativePassThroughActive) return;
        if (ReferenceEquals(OverlayInputPassThroughElement, nativeRoot?.Content))
        {
            if (priorNativePassThrough is DependencyObject prior) OverlayInputPassThroughElement = prior;
            else ClearValue(OverlayInputPassThroughElementProperty);
        }
        nativePassThroughActive = false;
        priorNativePassThrough = null;
    }

    private void NativeTargetUnloaded(object sender, RoutedEventArgs args)
    {
        var observed = nativeTarget; var root = nativeRoot;
        if (observed is null || root is null) return;
        if (!observed.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (!ReferenceEquals(nativeTarget, observed) || !IsOpen || observed.IsLoaded && ReferenceEquals(observed.XamlRoot, root)) return;
            // Structural retirement also applies to the unprojected native path.
            // Normal Closing cancellation cannot retain an orphaned popup.
            var marker = VisualTreeHelper.GetOpenPopupsForXamlRoot(root).SingleOrDefault(p => ReferenceEquals(p.Child, presenter))
                ?? throw new InvalidOperationException("The retiring Context Menu has no native popup.");
            marker.IsOpen = false;
        })) throw new InvalidOperationException("Context Menu could not schedule native target retirement.");
    }

    protected override Control CreatePresenter()
    {
        presenter = (MenuFlyoutPresenter)base.CreatePresenter();
        presenter.Loaded += (_, _) => portal?.QueueUpdate();
        return presenter;
    }
}
