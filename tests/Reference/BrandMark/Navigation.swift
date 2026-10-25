import SwiftUI

public struct BleeckerBrandMark: View {
    @Environment(\.colorScheme) private var scheme
    private let size: CGFloat

    public init(size: CGFloat = 64) { self.size = size }

    public var body: some View {
        Image("BleeckerLogo", bundle: .module)
            .resizable()
            .scaledToFit()
            .foregroundStyle(BleeckerPalette.resolve(scheme).textPrimary.opacity(0.9))
            .frame(width: size, height: size)
    }
}

public struct BleeckerBrandLockup: View {
    @Environment(\.colorScheme) private var scheme
    let name: String
    public init(name: String) { self.name = name }
    public var body: some View {
        let p = BleeckerPalette.resolve(scheme)
        HStack(spacing: 16) {
            BleeckerBrandMark(size: logoWidth).frame(height: logoHeight)
            Text(name).font(BleeckerTypography.primary(labelSize, weight: .bold)).tracking(-0.4).foregroundStyle(p.textPrimary)
        }
    }

    private var logoWidth: CGFloat {
#if os(tvOS)
        64
#else
        25
#endif
    }

    private var logoHeight: CGFloat {
#if os(tvOS)
        64
#else
        32
#endif
    }

    private var labelSize: CGFloat {
#if os(tvOS)
        27
#else
        20
#endif
    }
}

public struct BleeckerSidebarItem: View {
    @Environment(\.colorScheme) private var scheme
#if os(tvOS)
    @FocusState private var focused: Bool
#endif
    let title: String; let systemImage: String; let selected: Bool; let action: () -> Void
    public init(_ title: String, systemImage: String, selected: Bool, action: @escaping () -> Void) { self.title = title; self.systemImage = systemImage; self.selected = selected; self.action = action }
    public init(title: String, systemImage: String, selected: Bool, action: @escaping () -> Void) { self.init(title, systemImage: systemImage, selected: selected, action: action) }
    public var body: some View {
#if os(tvOS)
        sidebarButton.focused($focused)
#else
        sidebarButton
#endif
    }

    private var sidebarButton: some View {
        let p = BleeckerPalette.resolve(scheme)
        return Button(action: action) {
            HStack(spacing: BleeckerSpacing.control) { Image(systemName: systemImage).font(.system(size: sidebarIconSize, weight: .medium)).frame(width: sidebarIconWidth); Text(title).font(BleeckerTypography.primary(sidebarTextSize, weight: .medium)); Spacer() }
                .foregroundStyle(selected ? p.sea : p.textSecondary).padding(.horizontal, BleeckerSpacing.control).frame(minHeight: sidebarHeight)
                .background(isHighlighted ? p.muted.opacity(0.8) : .clear).clipShape(RoundedRectangle(cornerRadius: BleeckerRadius.ui))
                .overlay(alignment: .leading) { if selected { Rectangle().fill(p.sea).frame(width: 2, height: 24) } }.contentShape(Rectangle())
        }.buttonStyle(.plain)
    }

    private var isHighlighted: Bool {
#if os(tvOS)
        selected || focused
#else
        selected
#endif
    }

    private var sidebarHeight: CGFloat {
#if os(tvOS)
        72
#else
        40
#endif
    }

    private var sidebarIconSize: CGFloat {
#if os(tvOS)
        22
#else
        15
#endif
    }

    private var sidebarIconWidth: CGFloat {
#if os(tvOS)
        30
#else
        20
#endif
    }

    private var sidebarTextSize: CGFloat {
#if os(tvOS)
        20
#else
        14
#endif
    }
}

public struct BleeckerPageHeader<Actions: View>: View {
    @Environment(\.colorScheme) private var scheme
    let eyebrow: String; let title: String; let description: String; @ViewBuilder let actions: Actions
    public init(eyebrow: String, title: String, description: String, @ViewBuilder actions: () -> Actions) { self.eyebrow = eyebrow; self.title = title; self.description = description; self.actions = actions() }
    public var body: some View {
        let p = BleeckerPalette.resolve(scheme)
        HStack(alignment: .center, spacing: BleeckerSpacing.group) {
            VStack(alignment: .leading, spacing: BleeckerSpacing.detail) {
                Text(eyebrow.uppercased()).font(BleeckerTypography.primary(10, weight: .bold)).tracking(1.2).foregroundStyle(p.desert)
                Text(title).font(BleeckerTypography.primary(25, weight: .semibold)).tracking(-0.5).foregroundStyle(p.textPrimary)
                Text(description).font(BleeckerTypography.secondary(13)).foregroundStyle(p.textSecondary)
            }
            Spacer(); HStack(spacing: BleeckerSpacing.control) { actions }
        }
    }
}

public struct BleeckerSectionHeader<Actions: View>: View {
    @Environment(\.colorScheme) private var scheme
    let title: String; let subtitle: String?; @ViewBuilder let actions: Actions
    public init(_ title: String, subtitle: String? = nil, @ViewBuilder actions: () -> Actions) { self.title = title; self.subtitle = subtitle; self.actions = actions() }
    public var body: some View {
        let p = BleeckerPalette.resolve(scheme)
        HStack { VStack(alignment: .leading, spacing: 3) { Text(title).font(BleeckerTypography.primary(17, weight: .semibold)); if let subtitle { Text(subtitle).font(BleeckerTypography.secondary(12)).foregroundStyle(p.textSecondary) } }; Spacer(); actions }
    }
}

public struct BleeckerBreadcrumbItem: Identifiable, Sendable {
    public let id = UUID(); public let title: String; public let systemImage: String?
    public init(_ title: String, systemImage: String? = nil) { self.title = title; self.systemImage = systemImage }
}

public struct BleeckerBreadcrumb: View {
    @Environment(\.colorScheme) private var scheme
    let items: [BleeckerBreadcrumbItem]; let onSelect: ((Int) -> Void)?
    public init(_ items: [BleeckerBreadcrumbItem], onSelect: ((Int) -> Void)? = nil) { self.items = items; self.onSelect = onSelect }
    public var body: some View {
        let p = BleeckerPalette.resolve(scheme)
        HStack(spacing: 8) {
            ForEach(items.indices, id: \.self) { index in
                if index > 0 { Image(systemName: "chevron.right").font(.caption2).foregroundStyle(p.textSecondary) }
                Button { onSelect?(index) } label: {
                    HStack(spacing: 5) {
                        if let icon = items[index].systemImage { Image(systemName: icon) }
                        Text(items[index].title)
                    }
                }
                .buttonStyle(.plain)
                .foregroundStyle(index == items.count - 1 ? p.textPrimary : p.sea)
            }
        }.font(BleeckerTypography.primary(12))
    }
}

public struct BleeckerTabs<Value: Hashable>: View {
    @Binding var selection: Value; let options: [BleeckerSelectOption<Value>]
    public init(selection: Binding<Value>, options: [BleeckerSelectOption<Value>]) { _selection = selection; self.options = options }
    public var body: some View { BleeckerSegmentedControl(selection: $selection, options: options) }
}

public struct BleeckerPagination: View {
    let page: Int; let pageCount: Int; let onPage: (Int) -> Void
    public init(page: Int, pageCount: Int, onPage: @escaping (Int) -> Void) { self.page = page; self.pageCount = pageCount; self.onPage = onPage }
    public var body: some View {
        HStack(spacing: 8) { BleeckerIconButton("chevron.left", accessibilityLabel: "Previous page") { onPage(max(1, page - 1)) }.disabled(page <= 1); Text("Page \(page) of \(max(1, pageCount))").font(BleeckerTypography.primary(12)); BleeckerIconButton("chevron.right", accessibilityLabel: "Next page") { onPage(min(pageCount, page + 1)) }.disabled(page >= pageCount) }
    }
}

public struct BleeckerAccordion<Content: View>: View {
    @Environment(\.colorScheme) private var scheme
    let title: String; @State private var expanded: Bool; @ViewBuilder let content: Content
    public init(_ title: String, expanded: Bool = false, @ViewBuilder content: () -> Content) { self.title = title; _expanded = State(initialValue: expanded); self.content = content() }
    public var body: some View {
        let p = BleeckerPalette.resolve(scheme)
        VStack(alignment: .leading, spacing: 10) { Button { withAnimation(.easeOut(duration: BleeckerDuration.standard)) { expanded.toggle() } } label: { HStack { Text(title).font(BleeckerTypography.primary(14, weight: .semibold)); Spacer(); Image(systemName: "chevron.down").rotationEffect(.degrees(expanded ? 180 : 0)) } }.buttonStyle(.plain); if expanded { content.transition(.opacity.combined(with: .move(edge: .top))) } }.padding(14).background(p.card).clipShape(RoundedRectangle(cornerRadius: BleeckerRadius.ui)).overlay { RoundedRectangle(cornerRadius: BleeckerRadius.ui).strokeBorder(p.border) }
    }
}
