# Tabs

The `.55` x64 preview accepts Tabs under the five-check milestone. The extracted
catalog passed Light/Dark states across nine variant/size pairs and physical
keyboard, pointer and focus sequences. The independent package consumer
compiled and ran 36 native 150% Light/Dark layouts across variants, sizes and
widths; buttons stayed in bounds. Selection paint and positions stayed static
with animations disabled. ARM64 built and its consumer compiled; ARM64 runtime
is unverified. Pixel differences and normal-motion diagnostics below are
historical, not milestone gates.

`BleeckerTabs` is a reusable, consumer-controlled strip of `BleeckerTabItem`
records. Variants are `Underline`, `Segmented` and `Enclosed`; sizes are `Small`,
`Medium` and `Large`. `Stretch` distributes the available width while retaining
the minimum width of each non-wrapping label. The catalog's Tabs page demonstrates
all nine combinations, accepted/declined requests, navigation, custom links,
disabled state and an empty collection.

```csharp
var tabs = new BleeckerTabs { ActiveTab = "overview" };
tabs.Items.Add(new("overview", "Overview", panelId: "overview-panel"));
tabs.Items.Add(new("activity", "Activity", panelId: "activity-panel"));
tabs.ActiveTabRequested += (_, request) => tabs.ActiveTab = request.Id;
```

`ActiveTab` remains authoritative: activation requests a value and never changes
it internally. A consumer can accept or decline the request. Null or an unknown
identifier selects nothing. Item identifiers are ordinal and unique; empty
identifiers are supported. Replace an entry to update an immutable item. Moving
an item preserves its native control; removing the active item retains the
consumer's identifier, which becomes selected again if reinserted. Panel content
and `PanelId` interpretation belong to the consumer.

For panel tabs, Left/Right wrap among enabled items, Home/End reach the ends,
and native Space/Enter activate. Horizontal navigation respects Windows RTL
layout. A keyboard Tab traversal enters the active enabled tab, or the first
enabled tab if no enabled selection exists. Disabled groups/items reject
activation. UI Automation exposes Tab/TabItem and single-selection patterns.

An item with a nonempty `Href` is a navigation link. Its original `Href` and
`External` metadata are delivered through `NavigationRequested`; an optional
item command receives its original parameter. The library does not launch a
browser, reconstruct a destination, change `ActiveTab`, or own a router. A strip
containing destinations has navigation-group semantics, with normal Tab traversal.
`LinkTemplate` receives a `BleeckerTabLinkContext` containing the original `Item`,
`Href`, `Label`, prepared `Content`, source `Style`, and a guarded `Command`.
It creates the actual link element, so consumers can supply routing behavior and
native semantics. The catalog demonstrates this with a command-bound Button.

The frozen source is Bleecker `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/tabs.tsx`, SHA-256
`812294042eecf36f03ccfe890bac6f1542a6573c1fd8c24b42e963bb560c6827`.
Its literal dimensions, colors and variants inform the native preview. The
reference collector freezes 44 rendered cases, 36 complete source PNGs and four
accepted/declined selection sequences. `-Component tabs-reference` measures the
native controls and captures their composed client pixels. `verify-tabs.mjs`
checks geometry, paint, padding, border thickness, weight, line height and radius;
the current x64 result has zero geometry error and no measured paint errors.
Nine altered reports are rejected. Full-frame pixels still differ, with no
masks, resampling or pixel tolerance. These diagnostics retain their failures;
visual acceptance follows the [component acceptance contract](acceptance-contract.md).

Stretch allocation retains the browser's 1/64px rounding, including the small
differences between adjacent tracks. Native labels use the same script-specific
font runs as the other reference controls. Bundled-font validation permits a
nested layout callback to resolve the same asset URI while its outer validation
finishes; validation errors still propagate and remain cached.

The shared CSS shadow renderer supplies the enclosed strip's outer shadow and
the segmented active item's shadow. Foreground, background, border and shadow
transition over 190ms with the source premium curve. `TabsMotion.json` freezes
96 actual browser sequences: hover/leave, selection/deselection, press/release
and theme changes in both themes and motion preferences. The production clocks
match 528 presented samples and 192 color-space mappings. The one-pixel pressed
shift is immediate, matching CSS's individual `translate` property. Reduced
motion snaps to the requested paint. This machine currently reports animations
disabled; normal native frame scheduling and presentation remain unverified.

Portable model/reference and catalog coverage tests pass.
The focused x64 runtime passes both themes: nine rendered stretch combinations,
accepted/declined consumer requests, disabled/stale-provider rejection, keyed
updates, template replacement, navigation metadata, custom rendering, command
parameter/CanExecute boundaries, real arrow/Home/End/Space/Tab navigation,
disabled-item skipping, RTL, pointer selection and held-pointer displacement.
x64 and ARM64 builds are tracked separately from native runtime.

Local package `0.1.0-tabs-motion-local` passes the independent x64 consumer in both
themes, including selection requests, all nine stretch combinations, the bundled
font and a custom link renderer with unchanged destination metadata. A fresh
ARM64 consumer builds against the same package; ARM64 runtime remains unverified.
The consumer helpers confirm removal of their isolated temporary sources/caches.

The historical package evidence in `artifacts/current/tabs-motion-checkpoint.json`
does not establish the `.55` milestone; use the current release and inventory
for its five x64 checks.
Narrator and OS contrast campaigns are excluded by the [library scope](library-scope.md).

The 2026-10-25 visual review found a selected-tab border compositing defect.
The default native tab-button and strip styles now set `BackgroundSizing` to
`OuterBorderEdge`; `TabBorder` and `TabsFrame` bind that existing native property.
This paints each background under its translucent outline while allowing caller
overrides. The pinned WinUI 2.3.9 API metadata confirms the inner-edge default
and outer-edge behavior; see the [Microsoft property documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.border.backgroundsizing?view=windows-app-sdk-2.0).

The shared hidden fixture reuses the catalog's existing 44-case reference
factory. The unchanged package passes geometry and brush checks but fails all
2,536 straight-border samples across 24 bordered variant/size/theme/stretch
cases, plus 2,128 of the outer strip's 4,256 samples. The first candidate fixed
the inner border but left that outer-strip defect and remains unselected.
The complete `0.1.0-tabs-border-v2-local` package and extracted catalog each keep
zero geometry error and pass all 6,792 native border-over-own-fill compositing
samples at one channel level across both surfaces.
The separate source-PNG comparison still fails 634 samples in six Light Enclosed
cases, each by two channel levels; its threshold and failed report are retained,
and the residual cause is unverified. The outer strip's source-pixel checks
now pass. The remaining full-image diagnostics and component/platform acceptance
are unchanged. This repair changes four declarations in one existing template
file and introduces no renderer, token, geometry or public API change.

The x64/ARM64 source and fresh independent consumer builds pass. Both fictional
component demos pass their 66 checks and 577 guards in the extracted catalog.
The v2 local preview checkpoint is
`artifacts/current/tabs-border-paint-checkpoint-v2.json`; these results do not
establish native ARM64 execution or complete component acceptance.

`scripts/verify-tabs-native.ps1` runs the 44 cases in an invisible window,
checks geometry/brush metadata and native border layering, and records the
source-image result separately. It accepts frozen catalog or independent-consumer
apps with immutable manifests. It never activates, focuses or shows the window
or sends hardware input. Physical input, normal-motion scheduling, scaling and
native ARM64 execution still need their own evidence.

The later font-mapping review confirms that the reference collector embeds the
same unchanged Encode Sans Medium asset used by the current native labels,
which declare its canonical family and weight 500. The font's typographic family
is Encode Sans; Encode Sans Md is its legacy family name. The older raw alias
comparison at `artifacts/smoke-font-audit-final` is no longer available, so its
documentation summary is not current verification. No font-mapping defect or
cause for the remaining text-image differences was established; no font or
renderer change was adopted. Full typography review remains open.
