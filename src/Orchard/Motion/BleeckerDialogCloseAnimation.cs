namespace Orchard;

internal sealed class BleeckerDialogCloseAnimation
{
    internal readonly record struct Frame(BleeckerCssColor Foreground, BleeckerCssColor Background, BleeckerCssColor Border, double Shadow);
    private readonly BleeckerCssTransition<BleeckerCssColor> foreground = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<BleeckerCssColor> background = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<BleeckerCssColor> border = new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<double> shadow = new((from, to, progress) => from + (to - from) * progress);
    internal void Retarget(Frame target, double milliseconds, bool animate)
    {
        foreground.Retarget(target.Foreground, milliseconds, 190, animate);
        background.Retarget(target.Background, milliseconds, 190, animate);
        border.Retarget(target.Border, milliseconds, 190, animate);
        shadow.Retarget(target.Shadow, milliseconds, 190, animate);
    }
    internal Frame At(double milliseconds) => new(foreground.ValueAt(milliseconds), background.ValueAt(milliseconds), border.ValueAt(milliseconds), shadow.ValueAt(milliseconds));
    internal bool IsRunning(double milliseconds) => foreground.IsRunning(milliseconds) || background.IsRunning(milliseconds) || border.IsRunning(milliseconds) || shadow.IsRunning(milliseconds);
    internal static void ValidateIconSize(double size)
    { if (!double.IsFinite(size) || size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Icon size must be finite and non-negative."); }
}
