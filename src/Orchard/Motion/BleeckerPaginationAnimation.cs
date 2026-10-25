namespace Orchard;

internal sealed class BleeckerPaginationAnimation
{
    internal readonly record struct Frame(BleeckerCssColor Foreground, BleeckerCssColor Background, BleeckerCssColor Border, double Opacity);
    private readonly BleeckerCssTransition<BleeckerCssColor> foreground = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<BleeckerCssColor> background = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<BleeckerCssColor> border = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<double> opacity = new((from, to, progress) => from + (to - from) * progress);

    internal void Retarget(Frame destination, double time, bool animate)
    {
        var duration = BleeckerTokens.Durations["control"];
        foreground.Retarget(destination.Foreground, time, duration, animate);
        background.Retarget(destination.Background, time, duration, animate);
        border.Retarget(destination.Border, time, duration, animate);
        opacity.Retarget(destination.Opacity, time, duration, animate);
    }

    internal Frame At(double time) => new(foreground.ValueAt(time), background.ValueAt(time), border.ValueAt(time), opacity.ValueAt(time));
    internal bool IsRunning(double time) => foreground.IsRunning(time) || background.IsRunning(time) || border.IsRunning(time) || opacity.IsRunning(time);
    internal static (bool BackgroundLegacy, bool BorderLegacy) ColorSpaces(bool selected, bool hovered, bool arrow) =>
        selected ? (true, true) : (!hovered, !hovered && !arrow);
}
