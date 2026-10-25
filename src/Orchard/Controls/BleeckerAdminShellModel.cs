namespace Orchard;

internal static class BleeckerAdminShellModel
{
    internal readonly record struct Spacing(double Horizontal, double Vertical);
    internal static Spacing ContentSpacing(double viewport) => viewport >= 1024 ? new(40, 40) : viewport >= 640 ? new(32, 32) : new(20, 24);
    internal static double ContentHeight(double natural, double footer, double viewport) => Math.Max(natural, viewport - footer);
    internal static double ShellHeight(double minimum, double header, double sidebar, double main, double? height)
        => height.HasValue ? Math.Max(minimum, height.Value) : Math.Max(minimum, header + Math.Max(sidebar, main));
}
