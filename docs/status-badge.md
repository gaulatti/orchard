# Status badge

`BleeckerStatusBadge` displays a consumer-owned Label, optional Description and Variant (`Default`, `Live`, `Offline`, `Warning`, `Info`). It does not infer service state, fetch data or announce changes automatically.

```xml
<orchard:BleeckerStatusBadge Label="LIVE" Variant="Live"
    Description="Real-time updates active" />
```

The pinned reference has one size: minimum badge height 24px, 1px border, 6px corners, horizontal/vertical padding 10/2px, a 6px dot and a 6px label gap. Label typography is bundled Encode Sans Semibold at 11px with a 16.5px line box. Description uses Encode Sans Regular at 12px, a 16px line box and an 8px gap. Empty descriptions collapse. Surrounding layout owns placement; short labels leave room for descriptions to wrap under width constraints. Extremely long labels competing with descriptions still need full reference comparison.

Generated palettes preserve all five source recipes in light/dark appearance. Live uses Tailwind 4.2.2 emerald OKLCH values from Bleecker's pinned dependency, converted through Oklab to linear sRGB, encoded and clipped to 8-bit channels for WinUI brushes. This conversion is explicit in `scripts/generate-tokens.mjs`; browser gamut mapping and wide-gamut displays still require visual acceptance. Other variants use canonical Bleecker tokens and the source's literal offline light text `#874641`. Windows contrast resources use Window/WindowText colors.

Variant updates mutate instance-owned brushes and leave consumer Label bindings, Style, Background, Foreground and BorderBrush precedence intact. This control has no interactive, selected, loading, disabled-opacity or animated variant. It is not a keyboard stop. Its UI Automation text peer exposes the label as Name and description as HelpText; explicit consumer automation properties take precedence. Internal dot/text elements are grouped into that single peer. Applications choose whether their workflow needs live announcements.

## Evidence and remaining acceptance

The registered fixture covers all variants, optional descriptions, international text, narrower description wrapping, runtime variant updates and consumer label binding updates. Native x64 checks inspect actual templates, geometry, typography, color/opacity and automation peers in both themes. Captures are retained in `artifacts/smoke-status-badges`. The package consumer includes an Info badge.

The rendered Bleecker Live reference measures 57.625 × 24px, with the typography, dot, padding and gaps above. Full baseline/shaping and gamut comparison, very long label/description layout, actual contrast settings, Narrator, text/display scaling, physical keyboard traversal and ARM64 runtime remain open. The inventory entry remains in progress.
