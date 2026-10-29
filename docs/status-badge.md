# Status badge

`BleeckerStatusBadge` displays a consumer-owned Label, optional Description and Variant (`Default`, `Live`, `Offline`, `Warning`, `Info`). It does not infer service state, fetch data or announce changes automatically.

```xml
<orchard:BleeckerStatusBadge Label="LIVE" Variant="Live"
    Description="Real-time updates active" />
```

The pinned reference has one size: minimum badge height 24px, 1px border, 6px corners, horizontal/vertical padding 10/2px, a 6px dot and a 6px label gap. Label typography is bundled Encode Sans Semibold at 11px with a 16.5px line box. Description uses Encode Sans Regular at 12px, a 16px line box and an 8px gap. Empty descriptions collapse. Surrounding layout owns placement. Under finite width, the built-in template distributes space proportionally between label and description content, excluding fixed badge padding/borders and the inter-item gap from the shrinking calculation. The dot also shrinks horizontally when label content is constrained, as in the source flex layout; its height remains 6px. Both text regions can wrap, and resizing restores their natural layout. Windows word segmentation and font measurements establish minimum text widths so proportional shrinking does not squeeze readable glyphs away. Browser line-breaking equivalence for every language and punctuation case remains unverified. Consumer replacement templates own their layout.

Generated palettes preserve all five source recipes in light/dark appearance. Live uses Tailwind 4.2.2 emerald OKLCH values from Bleecker's pinned dependency, converted through Oklab to linear sRGB, encoded and clipped to 8-bit channels for WinUI brushes. This conversion is explicit in `scripts/generate-tokens.mjs`; browser gamut mapping and wide-gamut displays still require visual acceptance. Other variants use canonical Bleecker tokens and the source's literal offline light text `#874641`. Windows contrast resources use Window/WindowText colors.

Variant updates mutate instance-owned brushes and leave consumer Label bindings, Style, Background, Foreground and BorderBrush precedence intact. This control has no interactive, selected, loading, disabled-opacity or animated variant. It is not a keyboard stop. Its UI Automation text peer exposes the label as Name and description as HelpText; explicit consumer automation properties take precedence. Internal dot/text elements are grouped into that single peer. Applications choose whether their workflow needs live announcements.

## Evidence and remaining acceptance

The registered fixture covers all variants, optional descriptions, international text, narrower description wrapping, runtime variant updates and consumer label binding updates. Native x64 checks inspect actual templates, geometry, typography, color/opacity and automation peers in both themes. Captures are retained in `artifacts/smoke-status-badges`. The package consumer includes an Info badge.

The rendered Bleecker Live reference measures 57.625 × 24px, with the typography, dot, padding and gaps above. A 320px fixture with label `REGIONAL COVERAGE TEMPORARILY DEGRADED` and description `Updates continue during local review` gives the browser badge 190.203125 × 39px and the description 121.796875px of width. The native regression checks the same 39px badge height and both allocated widths within 3px, then widens to 640px and narrows again to verify reflow. It retains a visible fixture and captures in `artifacts/smoke-status-layout-dot`.

Full baseline/shaping and gamut comparison, widths below unbreakable-word minima,
system text scaling, physical monitor-DPI changes and current native ARM64 runtime remain open. The badge is
display-only; its input contract is the absence of an unwanted focus stop.
Narrator and OS contrast campaigns are excluded by the current
[library scope](library-scope.md). These historical checks do not add gates to
the 2026-10-29 x64 milestone.

The current package consumer passes 18 Light/Dark states and 26 metadata,
binding and lifecycle contracts on the published preview DLL. Its isolated
150% XAML rasterization check keeps the badge, label and description visible
inside their bounds in both themes. Status Badge is display-only, has no focus
stop or animation, and the package consumer compiles. All five x64 milestone
checks pass; the component is accepted. ARM64 runtime is unverified.

## Caller metadata

Explicit `AutomationProperties.Name` and `AutomationProperties.HelpText` values
take precedence over the label/description defaults, including empty strings,
bindings, inherited explicit styles and ancestor implicit styles. A caller's
`LabeledBy` association is retained even when its label is empty. Clearing the
override restores the applicable style or label/description default. Live label
updates, template replacement and remounting preserve caller bindings.

The catalog's changing badge demonstrates empty caller metadata. The focused
`--probe-hidden-status-badge` test runs in a source-built catalog or independent
NuGet consumer. It covers all five palettes in both themes, constrained and wide
layout in both text directions, and the caller metadata cases above. The
preceding published library reproduces the local-empty override failure.
Source raster/gamut comparison, physical monitor DPI, system text scaling and
current native ARM64 execution remain separate open checks.

The existing hidden catalog scaling fixture now checks the badge at 100%, 125%,
150% and 200% isolated native XAML rasterization in both themes. It verifies the
24px logical badge surface, 6px dot, visible label and description, caller-empty
automation metadata, non-tab-stop semantics, placement inside the owned island,
physical PNG dimensions and visible surface paint. Its 22 damaged-report cases
include missing badge content, collapsed surface and unwanted tab focus. The
fixture changes no Windows setting and does not establish system text scaling,
physical monitor-DPI behavior, complete browser/native glyph parity or ARM64
runtime. See `artifacts/current/status-badge-isolated-scale-v1` for the local
report and PNGs.

The source catalog, independent NuGet consumer and catalog archive each pass
18 visual/layout states, 26 metadata/binding/style/lifecycle contracts and
11 damaged-report checks with library `87755ce3…`. All nine earlier baseline
captures and snapshots are unchanged. The source/catalog hosts mount all eight
registered examples in both themes, including the explicit-empty metadata case.
The tests stay hidden and record zero focus or activation. Their audit verifies
source palette/typography recipes and existing constrained geometry; it does not
claim complete browser/native glyph or gamut equivalence.

```powershell
./scripts/verify-status-badge.ps1 -AppDirectory <frozen-catalog-app> -Output <new-output-directory>
```
