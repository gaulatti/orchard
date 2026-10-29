using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace Orchard;

/// <summary>A native context flyout with Bleecker's distinct 160-DIP context-menu recipe.</summary>
/// <remarks>Assign to FrameworkElement.ContextFlyout. Native items retain caller commands and parameters.</remarks>
public sealed partial class BleeckerContextMenu : MenuFlyout
{
    private MenuFlyoutPresenter? presenter;
    private FrameworkElement? nativeTarget;
    private XamlRoot? nativeRoot;
    private object? priorNativePassThrough;
    private bool nativePassThroughActive;
    private BleeckerModalHost? nativeOwner;
    private FrameworkElement? nativeOwnerSurface;
    private Popup? nativePopup;
    private object? priorNativeVisibility;
    private BleeckerPopupScope.Registration? nativeScope;
    private bool observingNativeLayout;
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
            if (Target is not { } target) throw new InvalidOperationException("Context Menu requires its native target before opening.");
            var host = BleeckerModalHost.Find(target);
            if (host?.ActiveSurface is not null)
            {
                nativeOwner = host;
                nativeOwnerSurface = host.ActiveSurface;
                host.ScopeChanged += NativeOwnerScopeChanged;
            }
            nativeTarget = target;
            nativeRoot = target.XamlRoot ?? throw new InvalidOperationException("Context Menu requires the target's actual XamlRoot.");
            if (!IsModal) EnableNativePassThrough();
            if (nativeOwner is not null) nativeRoot.Content.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(NativePreviewKeyDown), true);
            AttachNativeScope();
            target.Unloaded += NativeTargetUnloaded;
        };
        Opened += (_, _) =>
        {
            UpdateNativeOwnerVisibility(); AttachNativeScope();
            if (presenter is not null)
            {
                presenter.LayoutUpdated += NativePresenterLayoutUpdated;
                observingNativeLayout = true;
                UpdateNativeTextBounds();
            }
        };
        Closed += (_, _) => StopNativeTargetObservation();
    }

    private void ModalityChanged()
    {
        if (!IsOpen || nativeTarget is null) return;
        if (IsModal) RestoreNativePassThrough(); else EnableNativePassThrough();
        AttachNativeScope();
    }

    private void StopNativeTargetObservation()
    {
        if (observingNativeLayout && presenter is not null) presenter.LayoutUpdated -= NativePresenterLayoutUpdated;
        observingNativeLayout = false;
        nativeScope?.Dispose(); nativeScope = null;
        RestoreNativePassThrough();
        if (nativeOwner is not null)
        {
            nativeOwner.ScopeChanged -= NativeOwnerScopeChanged;
            nativeRoot?.Content.RemoveHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(NativePreviewKeyDown));
        }
        if (nativePopup is not null)
        {
            if (ReferenceEquals(priorNativeVisibility, DependencyProperty.UnsetValue)) nativePopup.ClearValue(UIElement.VisibilityProperty);
            else nativePopup.SetValue(UIElement.VisibilityProperty, priorNativeVisibility);
        }
        if (nativeTarget is not null) nativeTarget.Unloaded -= NativeTargetUnloaded;
        nativeTarget = null; nativeRoot = null; nativeOwner = null; nativeOwnerSurface = null; nativePopup = null; priorNativeVisibility = null;
    }

    private void NativeOwnerScopeChanged() => UpdateNativeOwnerVisibility();

    private void NativePresenterLayoutUpdated(object? sender, object args) => UpdateNativeTextBounds();

    private void UpdateNativeTextBounds()
    {
        if (!IsOpen) return;
        foreach (var item in Items.OfType<MenuFlyoutItem>())
        {
            if (!BleeckerMenuFocus.GetIsEnabled(item) || item.ActualWidth <= 0) continue;
            var label = FindNativePart<TextBlock>(item, "TextBlock");
            if (label is null || label.TextWrapping != TextWrapping.Wrap) continue;
            var available = Math.Max(0, item.ActualWidth - item.Padding.Left - item.Padding.Right);
            if (Math.Abs(label.MaxWidth - available) > .5) label.MaxWidth = available;
            var layout = FindNativePart<Grid>(item, "LayoutRoot");
            // WinUI centers a wrapped TextBlock after the native item is measured;
            // allow its second line to settle without painting past the item edge.
            var neededHeight = label.ActualHeight + item.Padding.Top + item.Padding.Bottom
                + (label.ActualHeight > 19.5 ? 8 : 0);
            if (layout is not null && Math.Abs(layout.MinHeight - neededHeight) > .5) layout.MinHeight = neededHeight;
        }
    }

    private static T? FindNativePart<T>(DependencyObject root, string name) where T : FrameworkElement
    {
        if (root is T element && element.Name == name) return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            if (FindNativePart<T>(VisualTreeHelper.GetChild(root, index), name) is { } found) return found;
        return null;
    }

    private void AttachNativeScope()
    {
        nativeScope?.Dispose(); nativeScope = null;
        if (nativeOwner is null || nativeTarget is null || presenter is null) return;
        nativeScope = BleeckerPopupScope.Attach(nativeTarget, presenter, IsModal,
            () => nativeOwner is not null && ReferenceEquals(nativeOwner.ActiveSurface, nativeOwnerSurface) && nativePopup?.Visibility != Visibility.Collapsed);
    }

    private void UpdateNativeOwnerVisibility()
    {
        if (!IsOpen || nativeOwner is null || nativeRoot is null) return;
        nativePopup ??= VisualTreeHelper.GetOpenPopupsForXamlRoot(nativeRoot).SingleOrDefault(p => ReferenceEquals(p.Child, presenter));
        if (nativePopup is null) return;
        priorNativeVisibility ??= nativePopup.ReadLocalValue(UIElement.VisibilityProperty);
        nativePopup.Visibility = ReferenceEquals(nativeOwner.ActiveSurface, nativeOwnerSurface) ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NativePreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (!IsOpen || nativeOwner is null || args.Handled || args.Key != VirtualKey.Escape) return;
        args.Handled = true;
        Hide();
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
        return presenter;
    }
}
