using Microsoft.UI.Input;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private BleeckerDisabledPointer? disabledPointer;
    private readonly InputSystemCursor textCursor = InputSystemCursor.Create(InputSystemCursorShape.IBeam);
    private readonly InputSystemCursor disabledCursor = InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);

    private void InitializePointer()
    {
        disabledPointer = new(this, () => this, SetPointerOver);
        PointerEntered += (_, _) => SetPointerOver(true);
        PointerExited += (_, _) => SetPointerOver(false);
        Loaded += (_, _) => UpdatePointerObservation();
        Unloaded += (_, _) => { disabledPointer.Detach(); pointerOver = false; };
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
        if (enabledUnderPointer) BleeckerPointerCursor.SetSystem(InputSystemCursorShape.IBeam);
    }
}
