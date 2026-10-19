// Generated from Bleecker 0bd7032e42d4c346c6e2201ad2feff96e635f73f; do not edit.
using System.Collections.Frozen;

namespace Orchard;

public enum BleeckerColorScheme { Light, Dark }

public static class BleeckerTokens
{
    public static FrozenDictionary<string, string> FontFamilies { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["primary"] = "Encode Sans",
            ["secondary"] = "Libre Franklin",
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, string> FontWeights { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["regular"] = "400",
            ["medium"] = "500",
            ["semibold"] = "600",
            ["bold"] = "700",
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, double> Spacing { get; } =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["detail"] = 4,
            ["inline"] = 8,
            ["control"] = 12,
            ["component"] = 16,
            ["group"] = 24,
            ["container"] = 32,
            ["section"] = 48,
            ["page"] = 64,
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, double> Radii { get; } =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["button"] = 7,
            ["card"] = 12,
            ["ui"] = 7,
            ["dialog"] = 14,
            ["pill"] = 9999,
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, double> Durations { get; } =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["fast"] = 150,
            ["standard"] = 200,
            ["deliberate"] = 300,
            ["enter"] = 220,
            ["exit"] = 320,
            ["control"] = 190,
            ["surface"] = 220,
            ["overlay"] = 240,
            ["motionExit"] = 150,
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, double> LetterSpacing { get; } =
        new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["refined"] = -0.02,
            ["ui"] = 0,
            ["elegant"] = 0.05,
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, string> LightColors { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["dusk"] = "#FF694F5D",
            ["deepSea"] = "#FF1A374D",
            ["accentOxblood"] = "#FF76323F",
            ["accentBronze"] = "#FFCD7F32",
            ["accentRed"] = "#FFD94F4F",
            ["accentYellow"] = "#FFD4AF37",
            ["persgray50"] = "#FFF9FAFB",
            ["sand"] = "#FFE6D5B8",
            ["desert"] = "#FFC1814D",
            ["terracotta"] = "#FFA65D57",
            ["sea"] = "#FF2C5784",
            ["lightSand"] = "#FFF9F6F2",
            ["darkSand"] = "#FFD4C4A9",
            ["sunset"] = "#FFFF9677",
            ["accentGold"] = "#FFC6A760",
            ["accentText"] = "#FF8A5735",
            ["accentBlue"] = "#FF2C5784",
            ["textPrimary"] = "#FF2D2D2D",
            ["textSecondary"] = "#FF595959",
            ["background"] = "#FFFFFFFF",
            ["foreground"] = "#FF2D2D2D",
            ["card"] = "#FFFFFFFF",
            ["cardForeground"] = "#FF2D2D2D",
            ["popover"] = "#FFFFFFFF",
            ["popoverForeground"] = "#FF2D2D2D",
            ["muted"] = "#FFF9F6F2",
            ["mutedForeground"] = "#FF595959",
            ["border"] = "#14000000",
            ["input"] = "#14000000",
            ["ring"] = "#FF2C5784",
            ["primary"] = "#FF2C5784",
            ["primaryForeground"] = "#FFFFFFFF",
            ["secondary"] = "#FFF9F6F2",
            ["secondaryForeground"] = "#FF2D2D2D",
            ["destructive"] = "#FFA65D57",
            ["destructiveForeground"] = "#FFFFFFFF",
            ["accent"] = "#FFF9F6F2",
            ["accentForeground"] = "#FF2D2D2D",
        }.ToFrozenDictionary(StringComparer.Ordinal);
    public static FrozenDictionary<string, string> DarkColors { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["dusk"] = "#FF694F5D",
            ["deepSea"] = "#FF182533",
            ["accentOxblood"] = "#FF76323F",
            ["accentBronze"] = "#FFCD7F32",
            ["accentRed"] = "#FFD94F4F",
            ["accentYellow"] = "#FFD4AF37",
            ["persgray50"] = "#FFF9FAFB",
            ["sand"] = "#FF1A2332",
            ["desert"] = "#FFE0AC69",
            ["terracotta"] = "#FFD47B75",
            ["sea"] = "#FF5BA3F5",
            ["lightSand"] = "#FF0D1821",
            ["darkSand"] = "#FF243447",
            ["sunset"] = "#FFFF9677",
            ["accentGold"] = "#FFD4AF37",
            ["accentText"] = "#FFE0AC69",
            ["accentBlue"] = "#FF5BA3F5",
            ["textPrimary"] = "#FFF0F4F8",
            ["textSecondary"] = "#FFB8C5D6",
            ["background"] = "#FF0D1821",
            ["foreground"] = "#FFF0F4F8",
            ["card"] = "#FF182533",
            ["cardForeground"] = "#FFF0F4F8",
            ["popover"] = "#FF182533",
            ["popoverForeground"] = "#FFF0F4F8",
            ["muted"] = "#FF1A2332",
            ["mutedForeground"] = "#FFB8C5D6",
            ["border"] = "#14FFFFFF",
            ["input"] = "#14FFFFFF",
            ["ring"] = "#FF5BA3F5",
            ["primary"] = "#FF5BA3F5",
            ["primaryForeground"] = "#FF0D1821",
            ["secondary"] = "#FF1A2332",
            ["secondaryForeground"] = "#FFF0F4F8",
            ["destructive"] = "#FFD47B75",
            ["destructiveForeground"] = "#FF0D1821",
            ["accent"] = "#FF1A2332",
            ["accentForeground"] = "#FFF0F4F8",
        }.ToFrozenDictionary(StringComparer.Ordinal);
    internal static IReadOnlyList<(float X, float Y, float Blur, float Spread, float Opacity, byte R, byte G, byte B)> SurfaceShadows { get; } =
        Array.AsReadOnly(new (float, float, float, float, float, byte, byte, byte)[]
        {
            (0f, 1f, 2f, 0f, 0.025f, 26, 55, 77),
            (0f, 10f, 30f, -18f, 0.18f, 26, 55, 77),
        });
    internal static IReadOnlyList<(float X, float Y, float Blur, float Spread, float Opacity, byte R, byte G, byte B)> RaisedShadows { get; } =
        Array.AsReadOnly(new (float, float, float, float, float, byte, byte, byte)[]
        {
            (0f, 2f, 5f, 0f, 0.045f, 26, 55, 77),
            (0f, 18f, 42f, -22f, 0.24f, 26, 55, 77),
        });
    internal static IReadOnlyList<(float X, float Y, float Blur, float Spread, float Opacity, byte R, byte G, byte B)> OverlayShadows { get; } =
        Array.AsReadOnly(new (float, float, float, float, float, byte, byte, byte)[]
        {
            (0f, 24f, 64f, -20f, 0.28f, 26, 55, 77),
            (0f, 4f, 14f, 0f, 0.06f, 26, 55, 77),
        });
    public static FrozenDictionary<string, string> Colors(BleeckerColorScheme scheme) => scheme switch
    {
        BleeckerColorScheme.Light => LightColors,
        BleeckerColorScheme.Dark => DarkColors,
        _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unknown color scheme.")
    };
}
