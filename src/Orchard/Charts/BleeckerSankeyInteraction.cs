namespace Orchard;

// Source inspection keys distinguish node and link indexes. A flat index
// would accidentally select a node when a consumer adds a node during flow
// inspection. Retain each channel's kind/index even while its item is absent.
internal sealed class BleeckerSankeyInteraction
{
    private readonly record struct Item(bool Link, int Index);
    private Item? keyboard, pointer;
    private bool keyboardActive;
    internal int? Index(int nodes, int links)
    {
        var selected = pointer ?? (keyboardActive ? keyboard : null);
        return selected is Item item && item.Index >= 0 && item.Index < (item.Link ? links : nodes) ? (item.Link ? nodes : 0) + item.Index : null;
    }
    internal void Focus() { Reset(); keyboard = new(false, 0); keyboardActive = true; }
    internal void Blur() => keyboardActive = false;
    internal void Pointer(bool link, int index) { pointer = new(link, index); keyboardActive = false; }
    internal void Exit() => pointer = null;
    internal void Key(int direction, int nodes, int links)
    {
        if (direction == 0)
        {
            if (pointer is Item hovered) { keyboard = hovered; keyboardActive = true; pointer = null; }
            if (keyboard is not null) keyboardActive = !keyboardActive;
            return;
        }
        pointer = null; var count = nodes + links; if (count == 0) return;
        var valid = keyboard is Item item && item.Index >= 0 && item.Index < (item.Link ? links : nodes);
        var current = valid ? (keyboard!.Value.Link ? nodes : 0) + keyboard.Value.Index : direction > 0 ? -1 : count;
        var next = Math.Clamp(current + direction, 0, count - 1);
        keyboard = next < nodes ? new(false, next) : new(true, next - nodes); keyboardActive = true;
    }
    internal void Reset() { keyboard = pointer = null; keyboardActive = false; }
}
