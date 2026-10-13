using Microsoft.UI.Input;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private BleeckerDisabledPointer? disabledPointer;
    private readonly InputSystemCursor textCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
    private readonly InputSystemCursor disabledCursor = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);

    private void InitializePointer()
    {
        disabledPointer = new(this, () => this, SetPointerOver, observeEnabled: true);
        Loaded += (_, _) => { UpdatePointerObservation(); disabledPointer.ReconcileLifecycle(); };
        Unloaded += (_, _) => { disabledPointer.Detach(); pointerOver = false; disabledPointer.ReconcileLifecycle(); };
    }

    private void SetPointerOver(bool value)
    {
        if (pointerOver == value) return;
        pointerOver = value;
        UpdateAppearance();
    }

    private void UpdatePointerObservation()
    {
        var enabledUnderPointer = IsEnabled && pointerOver && disabledPointer!.OwnsCursor;
        ProtectedCursor = IsEnabled ? textCursor : disabledCursor;
        disabledPointer!.Update();
        if (enabledUnderPointer) BleeckerPointerCursor.Select(this, textCursor);
    }
}
