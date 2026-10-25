import SwiftUI

/// Native representation of Bleecker's platform-neutral token source.
public enum BleeckerSpacing {
    public static let detail: CGFloat = 4
    public static let inline: CGFloat = 8
    public static let control: CGFloat = 12
    public static let component: CGFloat = 16
    public static let group: CGFloat = 24
    public static let container: CGFloat = 32
    public static let section: CGFloat = 48
    public static let page: CGFloat = 64
}

public enum BleeckerRadius {
    public static let button: CGFloat = 7
    public static let card: CGFloat = 12
    public static let ui: CGFloat = 7
    public static let dialog: CGFloat = 14
    public static let pill: CGFloat = 9_999
}

public enum BleeckerDuration {
    public static let fast = 0.15
    public static let standard = 0.20
    public static let deliberate = 0.30
    public static let enter = 0.22
    public static let exit = 0.32
    public static let control = 0.19
    public static let surface = 0.22
    public static let overlay = 0.24
}

public enum BleeckerTypography {
    public static func primary(_ size: CGFloat, weight: Font.Weight = .regular) -> Font {
        SabellaFonts.register()
        return .custom(encodeSansName(for: weight), size: size)
    }

    public static func secondary(_ size: CGFloat, weight: Font.Weight = .regular) -> Font {
        SabellaFonts.register()
        return .custom(libreFranklinName(for: weight), size: size)
    }

    public static func mono(_ size: CGFloat, weight: Font.Weight = .regular) -> Font {
        .system(size: size, weight: weight, design: .monospaced)
    }

    private static func encodeSansName(for weight: Font.Weight) -> String {
        if weight == .ultraLight { return "EncodeSans-Th" }
        if weight == .thin { return "EncodeSans-XLt" }
        if weight == .light { return "EncodeSans-Lt" }
        if weight == .medium { return "EncodeSans-Md" }
        if weight == .semibold { return "EncodeSans-SmBold" }
        if weight == .bold { return "EncodeSans-Bold" }
        if weight == .heavy { return "EncodeSans-XBd" }
        if weight == .black { return "EncodeSans-Black" }
        return "EncodeSans-Regular"
    }

    private static func libreFranklinName(for weight: Font.Weight) -> String {
        "LibreFranklin-\(weightName(for: weight))"
    }

    private static func weightName(for weight: Font.Weight) -> String {
        if weight == .ultraLight { return "Thin" }
        if weight == .thin { return "ExtraLight" }
        if weight == .light { return "Light" }
        if weight == .medium { return "Medium" }
        if weight == .semibold { return "SemiBold" }
        if weight == .bold { return "Bold" }
        if weight == .heavy { return "ExtraBold" }
        if weight == .black { return "Black" }
        return "Regular"
    }
}

public struct BleeckerPalette: Sendable {
    public let sand, desert, terracotta, sea, deepSea, lightSand, darkSand, sunset: Color
    public let accentGold, accentBlue, accentOxblood, accentBronze, accentRed, accentYellow: Color
    public let textPrimary, textSecondary, background, foreground, card, cardForeground: Color
    public let popover, popoverForeground, muted, mutedForeground, border, input, ring: Color
    public let primary, primaryForeground, secondary, secondaryForeground: Color
    public let destructive, destructiveForeground, accent, accentForeground, live: Color

    public static func resolve(_ scheme: ColorScheme) -> Self {
        scheme == .dark ? .dark : .light
    }

    public static let light = Self(
        sand: .hex(0xE6D5B8), desert: .hex(0xC1814D), terracotta: .hex(0xA65D57),
        sea: .hex(0x2C5784), deepSea: .hex(0x1A374D), lightSand: .hex(0xF9F6F2),
        darkSand: .hex(0xD4C4A9), sunset: .hex(0xFF9677), accentGold: .hex(0xC6A760),
        accentBlue: .hex(0x2C5784), accentOxblood: .hex(0x76323F), accentBronze: .hex(0xCD7F32),
        accentRed: .hex(0xD94F4F), accentYellow: .hex(0xD4AF37), textPrimary: .hex(0x2D2D2D),
        textSecondary: .hex(0x595959), background: .white, foreground: .hex(0x2D2D2D),
        card: .white, cardForeground: .hex(0x2D2D2D), popover: .white,
        popoverForeground: .hex(0x2D2D2D), muted: .hex(0xF9F6F2), mutedForeground: .hex(0x595959),
        border: .black.opacity(0.08), input: .black.opacity(0.08), ring: .hex(0x2C5784),
        primary: .hex(0x2C5784), primaryForeground: .white, secondary: .hex(0xF9F6F2),
        secondaryForeground: .hex(0x2D2D2D), destructive: .hex(0xA65D57), destructiveForeground: .white,
        accent: .hex(0xF9F6F2), accentForeground: .hex(0x2D2D2D), live: .hex(0x10B981)
    )

    public static let dark = Self(
        sand: .hex(0x1A2332), desert: .hex(0xE0AC69), terracotta: .hex(0xD47B75),
        sea: .hex(0x5BA3F5), deepSea: .hex(0x182533), lightSand: .hex(0x0D1821),
        darkSand: .hex(0x243447), sunset: .hex(0xFF9677), accentGold: .hex(0xD4AF37),
        accentBlue: .hex(0x5BA3F5), accentOxblood: .hex(0x76323F), accentBronze: .hex(0xCD7F32),
        accentRed: .hex(0xD94F4F), accentYellow: .hex(0xD4AF37), textPrimary: .hex(0xF0F4F8),
        textSecondary: .hex(0xB8C5D6), background: .hex(0x0D1821), foreground: .hex(0xF0F4F8),
        card: .hex(0x182533), cardForeground: .hex(0xF0F4F8), popover: .hex(0x182533),
        popoverForeground: .hex(0xF0F4F8), muted: .hex(0x1A2332), mutedForeground: .hex(0xB8C5D6),
        border: .white.opacity(0.08), input: .white.opacity(0.08), ring: .hex(0x5BA3F5),
        primary: .hex(0x5BA3F5), primaryForeground: .hex(0x0D1821), secondary: .hex(0x1A2332),
        secondaryForeground: .hex(0xF0F4F8), destructive: .hex(0xD47B75), destructiveForeground: .hex(0x0D1821),
        accent: .hex(0x1A2332), accentForeground: .hex(0xF0F4F8), live: .hex(0x34D399)
    )
}

public extension Color {
    static func hex(_ value: UInt32, opacity: Double = 1) -> Color {
        Color(
            red: Double((value >> 16) & 0xFF) / 255,
            green: Double((value >> 8) & 0xFF) / 255,
            blue: Double(value & 0xFF) / 255,
            opacity: opacity
        )
    }
}

public struct BleeckerSurfaceModifier: ViewModifier {
    @Environment(\.colorScheme) private var scheme
    let radius: CGFloat

    public func body(content: Content) -> some View {
        let palette = BleeckerPalette.resolve(scheme)
        content
            .background(palette.card)
            .clipShape(RoundedRectangle(cornerRadius: radius, style: .continuous))
            .overlay { RoundedRectangle(cornerRadius: radius, style: .continuous).strokeBorder(palette.border) }
            .shadow(color: palette.deepSea.opacity(scheme == .dark ? 0 : 0.055), radius: 14, y: 5)
    }
}

public extension View {
    func bleeckerSurface(radius: CGFloat = BleeckerRadius.card) -> some View {
        modifier(BleeckerSurfaceModifier(radius: radius))
    }
}

public extension Text {
    func bleeckerEyebrow() -> some View {
        modifier(BleeckerEyebrowModifier())
    }

    func bleeckerPill() -> some View {
        modifier(BleeckerPillModifier())
    }
}

private struct BleeckerEyebrowModifier: ViewModifier {
    @Environment(\.colorScheme) private var scheme
    func body(content: Content) -> some View {
        let p = BleeckerPalette.resolve(scheme)
        content.font(BleeckerTypography.primary(10, weight: .bold)).tracking(1.15).foregroundStyle(p.desert)
    }
}

private struct BleeckerPillModifier: ViewModifier {
    @Environment(\.colorScheme) private var scheme
    func body(content: Content) -> some View {
        let p = BleeckerPalette.resolve(scheme)
        content.font(BleeckerTypography.mono(9, weight: .bold)).padding(.horizontal, 7).padding(.vertical, 4)
            .foregroundStyle(p.sea).background(p.muted).clipShape(Capsule())
    }
}
