# Sunburst Chart source contract and acceptance

The frozen [Sunburst Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/sunburst-chart.tsx)
has SHA-256 `8bd60a556edd2feac675c9f473ac1895639948936e8cc42607cf3b3c45a2aa3a`.
It uses React 19.2.4 and Recharts 3.8.1. Orchard pins .NET SDK 10.0.401,
Windows App SDK 2.5.1 and Win2D 1.4.0. The exact
[Sunburst implementation](https://github.com/recharts/recharts/blob/v3.8.1/src/chart/SunburstChart.tsx)
was consulted. The managed standard is current; the public repository wiki
remains unavailable, so the contract is retained here.

Acceptance starts with unchanged-browser measurements before native code.
Consumer-owned recursive nodes supply names, optional values, children and
per-node native fill. Defaults are Height 350, DataKey `value` and NameKey
`name`. Both keys must follow the actual source's property lookup behavior.
Preserve original root/child collections and records, including shared records
appearing at multiple tree paths, with live property and collection changes.
Derived palette/layout state must not mutate those records or replace original
consumer brushes.

The wrapper assigns sibling-index palette colors recursively and computes
leaf totals. It supplies only a missing, positive root total; an explicit
numeric root value remains authoritative. Do not infer internal branch values
or replace the producer's layout with a conventional normalized partition.
Capture root-only/empty trees, missing/explicit/conflicting root and branch
values, zero/negative/decimal data, unbalanced/deep/shared branches, duplicate
names, custom literal keys, paints, Unicode, and constrained/fractional sizes
in both themes. Preserve sector geometry, preorder, clipping, labels, text
metrics, bold weight, white outline and paint order.

Measure actual pointer, click, focus and keyboard behavior. The wrapper supplies
no Tooltip child or interaction callbacks; do not add a visible inspection
surface without evidence. Native accessibility must describe the consumer's
tree and values with clear names and appropriate roles. Test actual normal
keyboard traversal, disabled state and observable browser parity.

Trace root/collection/record replacement, addition/removal/reordering, value and
key changes, theme/font changes, resize, real detach/rapid reparent and template
replacement through every rendered sector, label and automation consumer.
Retain explicit cycle/error probes. Register catalog fixtures/navigation and
coverage, boundary regressions and focused/full native checks. Verify both
architecture builds, a fresh independent NuGet consumer and the complete
published catalog. Keep all existing checks, tolerances and the 360-second
full-runtime deadline intact; its previous run had only 0.428 seconds of margin.

## Native API and source behavior

`BleeckerSunburstChart.Data` accepts the original recursive
`IReadOnlyDictionary<string, object?>`. A `BleeckerSunburstNode` supplies Name,
optional Value/Fill and its own observable Children collection. Raw records can
supply custom literal keys and implement `INotifyPropertyChanged`; child
collections can implement `INotifyCollectionChanged`. The control observes each
distinct record/collection once and retains each occurrence's layout path.
Detached controls remove their subscriptions; replacing the root or Children
disconnects discarded records. Invalid child collections/null children and
cycles fail explicitly. A shared record at separate paths is valid.

The root is not painted. The measured ring thickness is `(outerRadius - 50) /
treeDepth`, including the root in depth; child rings have a two-unit gap. The
root's raw total drives an unclamped linear angular scale. Numeric zero roots
give every finite child 180 degrees, while missing/nonpositive inferred totals
leave unusable shapes. Negative roots preserve reversed normalization. Missing
branch values can suppress their own and later sibling ink while descendants
still appear from a finite start. Very narrow charts can have labels without
sectors. These are measured source behaviors, preserved without normalization.

Sectors have white two-unit strokes in both themes. Values use 12-unit bold
Encode Sans, black fill and white one-unit outline, with stroke before fill and
the source's unshifted baseline. Chromium selects the bundled SemiBold face for
CSS weight 700; native text uses that same face. Consumer FontFamily, FontSize
and FontWeight remain effective. The glyph outline is shaped once and sent to
two independent WinUI geometries because a geometry cannot have two owners.
Breaking whitespace follows Recharts Text and SVG whitespace handling.

The wrapper has no Tooltip or keyboard inspection surface. Native hover/click
preserves the sector/label objects and bubbles input to the consumer parent.
The chart is a named, nonfocusable Image; help text exposes each occurrence's
ancestor names and value. This is a Windows accessibility adaptation; NameKey
has no visible effect in the frozen wrapper.

## Evidence and remaining acceptance

The unchanged browser capture contains 94 Light/Dark cases and four explicit
invalid-tree probes. Golden layout SHA-256 is
`a51fb458b6fb75e602adf5d4d4da008a5c1a1631a133920c344dcb6b9b03b089`.
The capture and derivation audit the actual bundled dependency directories,
versions and lockfile integrities, including one React 19.2.4 runtime. Browser
hover/click/keyboard states contain no inspection surface. All 94 captured
paths, label strings and baselines pass the pure regression; original records
remain unchanged. The full regression suite has 53 passing tests, and generated
tokens match the pinned source.

The expanded 94-case native run passes both themes, including actual font
advances, stroke-before-fill glyph geometry, Tab traversal, pointer input and
live tree updates. The complete Sunburst/Sankey/Funnel/Radial Bar/Radar/Scatter/
Sparkline sequence passes. Shared paths, collection reorder/removal/reset,
explicit totals, literal keys, font family/size/weight, hierarchical automation,
detach/rapid reparent and template replacement are covered. x64 and ARM64
solution builds pass with zero warnings; ARM64 is compilation evidence only.
Fresh package and complete published runtime checks are pending.

This entry is in progress and is not accepted. Complete raster/motion, tree/data/
override/RTL, actual contrast, Narrator, display/text scaling, measured
collection performance and native ARM64 runtime remain required. This native
presentation component has no backend or Prometheus surface.
