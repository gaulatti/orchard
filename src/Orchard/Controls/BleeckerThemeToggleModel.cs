namespace Orchard;

public enum BleeckerThemeMode { Light, Dark, System }

internal static class BleeckerThemeToggleModel
{
    internal static BleeckerThemeMode Next(BleeckerThemeMode mode) => mode switch
    {
        BleeckerThemeMode.Light => BleeckerThemeMode.Dark,
        BleeckerThemeMode.Dark => BleeckerThemeMode.System,
        BleeckerThemeMode.System => BleeckerThemeMode.Light,
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    internal static string Label(BleeckerThemeMode mode) => Next(mode) switch
    {
        BleeckerThemeMode.Light => "Switch to light theme",
        BleeckerThemeMode.Dark => "Switch to dark theme",
        _ => "Switch to system theme"
    };

    // Lucide 0.563.0, the exact dependency in the frozen Bleecker lockfile.
    internal static string Glyph(BleeckerThemeMode mode) => mode switch
    {
        BleeckerThemeMode.Light => "<Path Data='M16 12A4 4 0 1 1 8 12A4 4 0 1 1 16 12Z'/><Path Data='M12 2v2'/><Path Data='M12 20v2'/><Path Data='m4.93 4.93 1.41 1.41'/><Path Data='m17.66 17.66 1.41 1.41'/><Path Data='M2 12h2'/><Path Data='M20 12h2'/><Path Data='m6.34 17.66-1.41 1.41'/><Path Data='m19.07 4.93-1.41 1.41'/>",
        BleeckerThemeMode.Dark => "<Path Data='M20.985 12.486a9 9 0 1 1-9.473-9.472c.405-.022.617.46.402.803a6 6 0 0 0 8.268 8.268c.344-.215.825-.004.803.401'/>",
        BleeckerThemeMode.System => "<Path Data='M4 3H20A2 2 0 0 1 22 5V15A2 2 0 0 1 20 17H4A2 2 0 0 1 2 15V5A2 2 0 0 1 4 3Z'/><Path Data='M8 21H16'/><Path Data='M12 17V21'/>",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };
}
