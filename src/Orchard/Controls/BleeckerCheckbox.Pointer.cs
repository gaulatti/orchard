using Microsoft.UI.Input;

namespace Orchard;

public sealed partial class BleeckerCheckbox
{
    private BleeckerDisabledPointer? disabledPointer;
    private readonly InputSystemCursor arrowCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);

    private void InitializePointer()
    {
        ProtectedCursor = arrowCursor;
        disabledPointer = new(this, () => box, SetPointerOver);
        PointerEntered += (_, _) => { if (IsEnabled) SetPointerOver(true); };
        PointerExited += (_, _) => { if (IsEnabled) SetPointerOver(false); };
        Loaded += (_, _) => { UpdatePointerObservation(); disabledPointer.ReconcileLifecycle(); };
        Unloaded += (_, _) => { disabledPointer.Detach(); pointerOver = false; disabledPointer.ReconcileLifecycle(); };
    }

    private void SetPointerOver(bool value)
    {
        if (pointerOver == value) return;
        pointerOver = value;
        UpdateVisuals(true);
    }

    private void UpdatePointerObservation()
    {
        var enabledUnderPointer = IsEnabled && pointerOver && disabledPointer!.OwnsCursor;
        disabledPointer!.Update();
        if (enabledUnderPointer) BleeckerPointerCursor.Select(this, arrowCursor);
    }
}
