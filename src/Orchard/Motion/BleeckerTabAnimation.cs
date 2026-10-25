namespace Orchard;

// The same per-property clocks drive native paint and frozen browser timelines.
internal sealed class BleeckerTabAnimation
{
    internal readonly record struct Frame(BleeckerCssColor Foreground, BleeckerCssColor Background, BleeckerCssColor Border, double Shadow);
    private readonly BleeckerCssTransition<BleeckerCssColor> foreground = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<BleeckerCssColor> background = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<BleeckerCssColor> border = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<double> shadow = new((from, to, progress) => from + (to - from) * progress);
    internal static (bool BackgroundLegacy, bool BorderLegacy) ColorSpaces(BleeckerTabsVariant variant, bool selected, bool hovered) =>
        (variant != BleeckerTabsVariant.Enclosed || !(selected || hovered), variant == BleeckerTabsVariant.Underline ? selected || !hovered : !selected);
    internal void Retarget(Frame target, double milliseconds, bool animate)
    {
        foreground.Retarget(target.Foreground, milliseconds, 190, animate);
        background.Retarget(target.Background, milliseconds, 190, animate);
        border.Retarget(target.Border, milliseconds, 190, animate);
        shadow.Retarget(target.Shadow, milliseconds, 190, animate);
    }
    internal Frame At(double milliseconds) => new(foreground.ValueAt(milliseconds), background.ValueAt(milliseconds), border.ValueAt(milliseconds), shadow.ValueAt(milliseconds));
    internal bool IsRunning(double milliseconds) => foreground.IsRunning(milliseconds) || background.IsRunning(milliseconds) || border.IsRunning(milliseconds) || shadow.IsRunning(milliseconds);
}
