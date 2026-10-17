using Microsoft.UI.Input;

namespace Orchard;

public sealed partial class BleeckerTextarea
{
    private BleeckerDisabledPointer? disabledPointer;
    private BleeckerDisabledPointer? resizePointerObserver;
    private readonly InputSystemCursor textCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
    private readonly InputSystemCursor disabledCursor = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);

    private void InitializePointer()
    {
        disabledPointer = new(this, () => this, _ => SetPointerOver(), observeEnabled: true, hitRegion: point => resizePointer is null && !InResizeGrip(point));
        resizePointerObserver = new(this, () => resizeGrip, _ => { SetPointerOver(); UpdateResizeCursor(); }, observeEnabled: true,
            blockDisabledCursor: false);
        Loaded += (_, _) => { Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnResizeRendering; Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += OnResizeRendering; UpdateResizeRoot(); UpdatePointerObservation(); disabledPointer.ReconcileLifecycle(); resizePointerObserver.ReconcileLifecycle(); };
        Unloaded += (_, _) => { if (!IsLoaded) Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= OnResizeRendering; UpdateResizeRoot(); disabledPointer.Detach(); resizePointerObserver.Detach(); pointerOver = false; disabledPointer.ReconcileLifecycle(); resizePointerObserver.ReconcileLifecycle(); };
    }

    private void OnResizeRendering(object? sender, object args)
    {
        if (IsLoaded && (resizePointer is not null || resizePointerObserver?.IsOver == true)) UpdateResizeCursor();
    }

    private void SetPointerOver()
    {
        var value = disabledPointer?.IsOver == true || resizePointerObserver?.IsOver == true;
        if (pointerOver == value) return;
        pointerOver = value;
        UpdateAppearance();
    }

    private void UpdatePointerObservation()
    {
        var enabledUnderPointer = IsEnabled && pointerOver && disabledPointer!.OwnsCursor;
        ProtectedCursor = IsEnabled ? textCursor : disabledCursor;
        disabledPointer!.Update();
        resizePointerObserver!.Update();
        UpdateResizeCursor();
        if (enabledUnderPointer) BleeckerPointerCursor.Select(this, textCursor);
    }
}
