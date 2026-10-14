namespace Orchard;

// The frozen Recharts interaction channels retain keyboard selection separately
// from pointer hover. Hiding inspection does not discard the keyboard index.
internal sealed class BleeckerChartInteraction
{
    private int? keyboardIndex, pointerIndex;
    private bool keyboardActive;
    internal double? PointerY { get; private set; }

    internal int? Index(int count)
    {
        if (count == 0) return null;
        if (pointerIndex is int pointer) return Math.Clamp(pointer, 0, count - 1);
        return keyboardActive && keyboardIndex is int keyboard ? Math.Clamp(keyboard, 0, count - 1) : null;
    }
    internal void Focus()
    {
        if (!keyboardActive && keyboardIndex is null) { keyboardIndex = 0; keyboardActive = true; }
    }
    internal void Blur() => keyboardActive = false;
    internal void Pointer(int index, double y)
    {
        pointerIndex = index; PointerY = y; keyboardActive = false;
    }
    internal void Exit() { pointerIndex = null; PointerY = null; }
    internal void Key(int direction, int count)
    {
        if (direction == 0)
        {
            if (keyboardIndex is not null) keyboardActive = !keyboardActive;
            return;
        }
        if (count == 0) return;
        var next = keyboardIndex is int current ? Math.Clamp(current, 0, count - 1) + direction : direction > 0 ? 0 : count - 1;
        if (next < 0 || next >= count) return;
        keyboardIndex = next; keyboardActive = true;
    }
    internal void Reset() { keyboardIndex = null; pointerIndex = null; keyboardActive = false; PointerY = null; }
}
