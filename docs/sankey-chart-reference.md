# Sankey Chart source contract and acceptance

The frozen [Sankey Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/sankey-chart.tsx)
has SHA-256 `026b636fa2441ac68933be304bc25689be66efc8f6b669b524cc580c78af578e`.
Its pinned runtime is React 19.2.4 and Recharts 3.8.1. Orchard pins .NET SDK
10.0.401, Windows App SDK 2.5.1 and Win2D 1.4.0. The exact
[Recharts layout and event source](https://github.com/recharts/recharts/blob/v3.8.1/src/chart/Sankey.tsx)
and [release](https://github.com/recharts/recharts/releases/tag/v3.8.1) were
consulted before implementation. The public repository wiki is unavailable;
this document retains the contract in the code repository.

Acceptance requires original consumer-owned node/link collections and records
to drive the native graph. Nodes supply names and optional native fill; links
supply numeric source/target indexes and flow values. Defaults are Height 400,
NodeWidth 12, LinkCurvature 0.5, LinkOpacity 0.35 and NodeRadius 4. The producer
fixes node padding at 16 and margins at top/bottom 10, left/right 20. Preserve
its 32 relaxation iterations, collision ordering, node value derivation, node
palette/per-node overrides, card-colored two-pixel borders and closed cubic
flow bands. Capture actual flow geometry and colors; do not infer them from a
conventional Sankey recipe. Consumer brush identity must survive updates.

Before implementation, capture the unchanged component with its exact frozen
dependency lock and bundled fonts loaded. Cover both themes, chains, branching,
merging, crossing/unbalanced flows, duplicate links/names, disconnected and
empty graphs, zero/negative/decimal values, paint/palette and all numeric options,
and constrained/fractional sizes. Record rect/path attributes and paint,
inspection payload/geometry, actual node/link hover, leave and flow opacity.
Measure the browser's focus/keyboard support before defining the corresponding
native Windows behavior. Retain explicit probes for invalid graph indexes and
recursion failures rather than inventing silent graph repairs.

Trace collection and record changes through graph layout, source/target index
relationships, native shapes, theme/template resources, inspection and
automation peers. Verify native keyboard and pointer input, one named group
stop, disabled traversal, stationary updates, real detach/rapid reparent,
template/collection replacement, reordering, resize and empty data. Register
catalog navigation/coverage and focused/full runtime checks. Validate a fresh
NuGet consumer, both architecture builds and the full published catalog without
weakening existing checks, coordinate tolerances or deadlines.

The unchanged browser capture contains 76 cases in both themes and five separate
graph-error probes. Its SHA-256 is
`b1f3af5ece35d54c2f9abc3fa29be9eb35f0be6a349da992da4e17786f810141`
at `tests/Reference/SankeyChartLayout.json`. The derivation script audits the
31 actually bundled runtime package locations against the frozen dependency
lock, including nested packages. One canonical React 19.2.4 runtime is required:
the capture initially found a second React copy through the frozen source's
dependency links, which prevented its hooks from rendering. The unchanged
component now uses the same pinned React instance as React DOM.

The custom producer adds source/target-relative offsets a second time when
forming each band. Bands can therefore extend below nodes and outside the
viewport; Orchard preserves that geometry and clips the plot. Computed Recharts
flow paths use the measured SVG `nonzero` fill rule, including folded curves;
native paint and precise hit geometry both use that rule. Computed Recharts
source nodes have no synthetic `index`, so default links all use palette zero,
while node colors use their separate renderer index. Per-node fill takes
precedence. An explicitly empty palette produces SVG's initial black fill,
including in Dark. Native brushes retain identity; opacity is applied to shapes
without modifying their consumer brushes. Flow hover uses the measured 0.8
opacity with the source's 300 ms default CSS curve; Windows reduced-motion
settings settle it immediately. Complete motion timeline/reversal proof remains
open.

Invalid source/target indexes omit their flows. A rootless cycle/self-loop has
no horizontal coordinates and paints no usable graph. A rooted cycle exhausts
the browser stack; Orchard detects the active traversal cycle and reports an
explicit error without risking a native stack overflow. It does not repair or
reorder consumer data. Derived values and positions are separate layout state.
Raw dictionary source/target/value fields must satisfy the numeric source
contract; string or boolean values fail explicitly instead of being coerced
into different relationships or flow totals. Typed records also require finite
flow values.

The source SVG has no keyboard stop and Tab/arrows/Enter produce no inspection.
Orchard provides an explicit Windows adaptation: one named group stop, bounded
Left/Right inspection through original nodes then original flows, Enter to hide
or restore the current node/flow (including a hovered flow), and normal
forward/reverse Tab. This is an added native convention;
browser keyboard parity is not claimed. Pointer inspection follows actual
native shapes. Node payloads use derived flow totals, link payloads use source
and target names plus original values, and popup anchors use upstream Recharts
centerlines rather than the custom band's extra offsets.

Supplemental unchanged-browser probes at
`artifacts/sankey-chart-browser/stationary-mutations.json` show that flow
inspection retains its kind/index when a node is added or removed. Removing
flow zero selects the new flow at index zero, and reinsertion restores that
index's original payload, without moving the pointer. Native inspection stores
node/link kinds separately rather than treating a flow as a flat node offset.
The native lifecycle and independent package checks exercise node-count changes
while preserving the current flow, and verify actual band geometry too.

The decimal-flow regression retains the captured native pointer point
`(424,375)`. Browser probing confirms that point excludes the neighboring
curved band's fill; XAML's approximate path hit included it. Native flow
inspection therefore tests the same point against the actual Win2D cubic fill
with 0.001 flattening precision, in reverse paint order. It does not enlarge
shape bounds or move the pointer. Win2D 1.4.0's installed API headers and the
[official geometry methods](https://microsoft.github.io/Win2D/WinUI3/html/Methods_T_Microsoft_Graphics_Canvas_Geometry_CanvasGeometry.htm)
establish the fill-query boundary. Popup placement uses the source's 20/10
horizontal/vertical margins.

The pure geometry regression passes all 76 normal captures and four
non-throwing error captures, plus the explicit rooted-cycle failure boundary.
All 76 native Light/Dark cases and both final lifecycle suites pass at
`artifacts/smoke-sankey-chart-final-sequence`. The same run continues through
Funnel, Radial Bar, Radar, Scatter and Sparkline in both themes. The checks
cover geometry/paint, exact
inspection payloads and tooltip dimensions/placement, real native shape hover,
stationary record updates, brush identity and palette changes, options/fonts,
one named group, disabled Tab traversal, detach/rapid remount, template/root
collection replacement, reordering, resize and empty data. Native keyboard
inspection resets to the first node on focus and takes over stationary pointer
inspection; subsequent actual pointer movement resumes hover. All 50 current
regressions and generated tokens pass. x64 and ARM64 cross-builds have zero
warnings; native ARM64 execution remains open. Independent package and full
published verification are pending.

Whole chart viewport comparisons for twelve captured normal states retain
every pixel, including background and clipped flow bands, with no exclusions.
All twelve differ from the browser raster: changed counts range from 60 to
129,342 pixels, with maximum per-channel differences of 14 to 47. Exact
geometry and paint-property checks do not resolve these raster differences.
The complete measured report is `artifacts/sankey-raster-comparison.json`;
full visual acceptance remains open.

Work is in progress and this inventory entry is not accepted. Complete raster,
motion/reversal, full graph/data/override/RTL coverage, actual contrast,
Narrator, display/text scaling, measured collection performance and native
ARM64 runtime remain required. This native-only presentation work has no
backend/Prometheus surface.
