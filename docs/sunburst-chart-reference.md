# Sunburst Chart source contract and acceptance

Sunburst Chart is accepted for the 2026-10-29 x64 milestone. Its exact
Light/Dark catalog states and passive nonfocusable image semantics pass, as do
four native 150% no-clip cases, static reduced-motion behavior, and
independent package-consumer compilation. ARM64 runtime remains unverified.
Earlier checkpoint status statements below are historical and do not add gates to this milestone.

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
remain unchanged. The full regression suite has 55 passing tests, and generated
tokens match the pinned source.

The expanded 94-case native run passes both themes, including actual font
advances, stroke-before-fill glyph geometry, Tab traversal, pointer input and
live tree updates. The complete Sunburst/Sankey/Funnel/Radial Bar/Radar/Scatter/
Sparkline sequence passes. Shared paths, collection reorder/removal/reset,
explicit totals, literal keys, font family/size/weight, hierarchical automation,
detach/rapid reparent and template replacement are covered. x64 and ARM64
solution builds pass with zero warnings; ARM64 is compilation evidence only.
The clean `4489fbd99f9b32a7e6069d30f6dacb07f50f94eb` source produces package
SHA-256 `e16470293ee39da6f17cb62e7dc6146fa289e3d01629e0211e268ea4b216bdb2`.
The independent x64 consumer passes both themes, including source sector
geometry, outlined values, original shared nodes/brushes, hierarchy updates,
missing literal keys and collection reset. The independent ARM64 consumer
builds with runtime verification explicitly false. Both isolated restore/build
directories are removed.

The first complete published run timed out at the unchanged 360-second deadline
near the final Dark checks. All 94 Sunburst cases and both lifecycle suites
passed within that run. Nonanimated setup waits were replaced with actual presentation/lifecycle
completion while preserving the existing assertions and timed motion samples.
The focused presentation suite passes all 18 affected fixtures in both themes,
including actual menu Closed-event/reopen checks. Both architecture builds have
zero warnings. Twelve whole Sunburst viewports, using their recorded native
bounds and zero excluded pixels, all differ from the browser (1,526–4,408
changed pixels, maximum channel difference 251). This is an explicit open
visual acceptance gap.

Performance investigation must preserve all 94 source cases, all native glyph
figure/segment coordinates and fill rules, and every captured viewport pixel.
Record hashes from the original native glyph geometries before changing their
materialization. Keep native pointer/presentation checks, motion windows and
the full 360-second deadline unchanged.

The CPU profile identifies repeated file-font and format setup for each label.
Sunburst and Radial Bar now select/validate one `BleeckerChartTextStyle` within
each render, retain separate per-value layouts, and dispose the format/font set
when that render finishes. No data or font selection is cached across renders.
Consumer family/size/weight changes therefore select a fresh style. Missing
font assets still fail explicitly, with a native regression at that boundary.
The pinned Win2D 1.4.0 [release notes](https://github.com/microsoft/Win2D/blob/winappsdk/main/CHANGELOG.md),
[text format](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Text_CanvasTextFormat.htm)
and [layout](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Text_CanvasTextLayout.htm)
contracts were consulted.

All 94 Sunburst native glyph hashes match the original figures, fill rules and
curve coordinates exactly; all 80 Radial Bar hashes also match. Diagnostic
Sunburst setup totals fall from 9.53 seconds over 592 layout calls to 1.37 over
579 calls (async rerenders vary). The separate Radial Bar baseline took
14.00/13.32 seconds per Light/Dark phase, versus 12.29/11.66 with the scoped
style. Temporary counters are removed. Native raster comparisons stay open:
unchanged-code repeats themselves differ in 9 of 12 viewports by 3–76 pixels,
with maximum channel difference 70. No pixels are masked or declared equal.
The final seven-chart sequence passes in both themes, including the missing
font rejection and corrected exact absolute mouse input. Both architecture
builds have zero warnings.

The second complete published run uses catalog source
`c3902c3acce7bdce6543951985b95ededf7ced1b` and also reached the unchanged
360-second deadline near the final Dark checks. Both Sunburst themes and
lifecycle suites passed. This run is retained as failed verification.
Capture profiling on the unchanged 140-image Input sequence attributes about
5.7 seconds to rendering and 1.3 seconds to PNG encoding. Bitmap reuse and
render-event submission show no useful improvement and are not retained.

The final clean source `3b3c836e07c7c988f74606d431d34ccd51e6d98d` produces the
current `0.1.0` package, SHA-256
`57832285ebc784d125460656faeb55679cff2ba2e7977c6b6352132b3ff51b2d`.
The fresh independent x64 consumer passes both themes with the scoped font
style; ARM64 consumption compiles with runtime verification false. Both
temporary restore/build workspaces are removed. The runnable catalog is
`artifacts/catalog-chart-font-style`, with actual ProductVersion
`0.1.0+3b3c836e07c7c988f74606d431d34ccd51e6d98d`.

Its complete published Light/Dark run passes in **354.531 seconds**, with the
360-second deadline unchanged, at `artifacts/smoke-chart-font-style-published-full`.
All 964 prior PNGs remain and 12 Sunburst captures bring the total to 976.
All 1,684 pointer presentation gates pass at two stable transitions or more;
maximum settling is 180.264 ms. Eight of 85 scroll commits catch changed bounds
before input. All changed-position moves have exact nongenerated receipts;
86 stationary moves reuse only a prior real receipt at the exact current point.
All final points are owned. All 46 click releases and 28 tooltip-opening gates
pass, with the latter retaining two stable transitions and a maximum 50.568 ms.
All 16 final capture reports retain at least two stable transitions. The 94
Sunburst and 80 Radial Bar native glyph hashes still match their baselines.
This establishes the current source/native/package/runtime foundation, while
the visual and platform acceptance gaps below remain open.

This entry is in progress and is not accepted. Complete raster/motion, tree/data/
override/RTL, component theme/automation behavior, display/text scaling, measured
collection performance and native ARM64 runtime remain required. This native
presentation component has no backend or Prometheus surface.
