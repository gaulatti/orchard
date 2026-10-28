# Bar Chart source contract and evidence

The DataList independent consumer exposed a shared remount/focus race. Its
failed `artifacts/package-consumer-data-list-x64-v2/runtime.json` records actual
keyboard focus, intact original data, empty inspection and no pointer events.
An instrumented retry passed without a repair; that historical pass is not
evidence that the earlier defect was resolved.

The native `chart-load-focus` fixture distinguishes two real event orders.
Requesting focus during `Loading` normally routes `GotFocus` after `Loaded`.
Immediate remove/reinsert can instead route `GotFocus` first; the old `Loaded`
reset then erased inspection while the chart still owned focus. The unchanged
control fails all 12 eager-reparent cases across Bar/Sankey and both themes,
losing both initial inspection and the next-record Right result. Those 24
assertion failures remain at `artifacts/smoke-chart-load-focus-before-v2`.

`BleeckerLineChart` now resets the old connection and initializes inspection
from the current `FocusManager` owner before rendering. A second captured event order
delivers an old `LostFocus` after focus has returned; the blur handler ignores
that stale notification while the control is focused. Genuine blur still hides
inspection and retains the source keyboard index; genuine unload still resets
both channels. The initial fix's one remaining late-blur failure is preserved
at `artifacts/smoke-chart-load-focus-fixed`. The first property-based guard still
failed one of three cold runs of the same consumer binary, with index 0 retained
but inactive while the current focus manager owned the chart. Reconciliation
now uses the same current-owner identity as the native keyboard input path,
and also rejects stale `GotFocus`. The consumer IL is held unchanged in
`artifacts/package-chart-focus-owner-prototype`; swapping only the working-tree
library passes three cold runs. This prototype is separate from fresh-package
release verification.

`artifacts/smoke-chart-load-focus-fixed-v2` passes all 24 focused cases, both
event orders, genuine blur and 24 physical owned Right pairs. The independent
`scripts/verify-chart-load-focus.mjs` audits the traces and original failures.
No data, geometry, source navigation semantics, deadline, timer or public API
changes. Full release/package and native ARM64 evidence must retain their own
source revisions and scope; this focus check does not establish final visual
or component acceptance.

Final clean source `4d14c0e` passes the published 24-case regression, two fresh
x64 NuGet consumers in both themes, and the complete 1,104-capture catalog in
434.4379521 seconds. All prior names and 174 protected glyph hashes remain.
The consumer retains failure-only state snapshots; temporary per-event focus
instrumentation is removed. The original and intermediate failures stay
separate evidence. See [the current checkpoint](native-validation-checkpoint.md)
for artifact hashes and still-open visual/platform acceptance.

Independent package mutation checks subsequently exposed a shared chart
remount defect: a delayed `Unloaded` ran after the new `Loaded` while the
renderer was connected, detaching the original collection's observers. See
[the lifecycle diagnosis and repair](area-chart-reference.md). The repair
preserves data, geometry, pointer behavior and verification deadlines.

`BleeckerBarChart` implements the pinned [Bleecker Bar Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/bar-chart.tsx).
Source SHA-256: `dcd8c302e50d0b8255207a57603999a5498cf4361034021d71aab96cd6f65235`.
The capture uses the frozen React 19.2.4 / Recharts 3.8.1 dependency lock and the
complete source stylesheet, plus the bundled fonts. The exact-version
[Recharts release](https://github.com/recharts/recharts/releases/tag/v3.8.1)
and installed source establish the framework boundary. Orchard remains on
Windows App SDK [2.5.1](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1).

```csharp
var chart = new BleeckerBarChart { XAxisKey = "category", Height = 300 };
chart.Series.Add(new BleeckerChartSeries { Key = "total", Name = "Local total" });
chart.Data.Add(new Dictionary<string, object?> { ["category"] = "Harbor", ["total"] = 100d });
```

The consumer owns observable `Data` and `Series` collections and record values.
Their original references reach the shared native renderer. Records implementing
`INotifyPropertyChanged` and live series metadata also request rendering.
`XAxisKey` must be supplied before mounting. Literal keys take precedence over
nested paths. Null collections and unknown layout values fail explicitly.
The component adds no data fetching or application workflow.

`Layout` preserves the source names: `Horizontal` draws vertical grouped columns;
`Vertical` draws horizontal grouped bars. Height defaults to 300. `ShowGrid` and
`ShowLegend` default to true. `YAxisTickFormatter` receives an object because
the actual source passes numeric Y values in Horizontal and original category
values in Vertical. `TooltipFormatter` receives raw numeric values; strings and
Boolean/range content follow the rendered producer's separate display path.

Margins are top 10, right 20, bottom 5 and left 0, with a 60px Y axis, 30px X axis
and measured legend. Even an empty series list retains the source legend's 16px
top padding when shown. Recharts ResponsiveContainer rounds its internal
viewport: an outer 512.5×244.5 allocation produces 513×245 chart geometry.
The native wrapper retains its consumer allocation and uses that same rounded
viewport for the chart. Numeric axes reuse the pinned adaptive five-tick domain.
Negative-only bars start at the upper domain bound, rather than extending to an
off-plot zero. Zero-size and missing-value bars produce no visible path; finite
two-value ranges use their own endpoints. Boolean geometry follows the producer's
numeric conversion without adding Boolean values to the numeric domain.

Groups reserve 10% of each category band on both sides and 4px between series.
Bar thickness above 1px truncates to an integer before offsets are placed.
Each signed path preserves `[4,4,0,0]` source corners, including negative values
and horizontal bars. Series use the shared ten-color palette or the exact
consumer brush, including transparent paint. The legend sorts by series name,
uses a 14px rectangle icon, 4px icon gap, 10px item margin and 16px top padding.
Grid lines include both numeric ticks and visible category ticks plus boundaries.

Axis glyphs are 12px. Passive instrumentation of the unchanged source's
measurement calls (`artifacts/bar-chart-browser/probe-trace.json`) establishes a
16px body-text allocation ruler, even when the later request names 12px. This
explains the captured narrow chart's Reed/North filtering. An explicit font
preload gives the same result. Category wrapping uses that allocation ruler with
12px line baselines; native regression compares the actual source tspan lines.

Inspection has one native keyboard stop, named by the consumer's
`AutomationProperties.Name`. The wrapper adds no second stop. Initial focus
inspects the first category; Left/Right and Enter reuse the source's retained
keyboard channel. The pinned vertical source reverses Left/Right movement:
Left advances downward, Right moves upward, and Up/Down do not select categories.
Pointer hover selects a complete category band, overrides keyboard inspection,
and hides on exit while retaining the keyboard index. Disabled inheritance keeps
the child disabled. Complete native keyboard/focus and automation metadata
acceptance remains open; Narrator testing is excluded by the
[current library scope](library-scope.md).

The cursor is the source muted-color rectangle behind bars and above the grid,
with half-pixel inset on the numeric axis. It has no stroke. Tooltip headings
show the category. Rows sort by series name but deliberately omit displayed
series names because the source formatter returns an empty name. Duplicate
categories inspect the first matching record. Boolean values display an empty
value; the custom source formatter renders `[10,30]` as `1030`. A heading without
payload stays hidden while the category cursor can remain visible. The tooltip
shares the existing scoped card/border, shadow and backdrop implementation.

## Verification

`tests/Reference/BarChartLayout.json` contains 68 actual rendered cases:
17 data/configuration specimens × two layouts × Light/Dark. SHA-256:
`428ad805fa1e33ae25c8e5a86bd4f40f8339b1d951b242981b9f1d9aae1d6141`.
Import with `scripts/derive-bar-chart-reference.mjs`. Captures, dependency checks,
source bundle, inspection markup and passive probes remain at
`artifacts/bar-chart-browser`.

The initial native run reproduces fractional viewport, empty-legend and category
filter mismatches. Corrected path geometry, colors, grids, tick text, cursor bands,
tooltip text/visibility and physical key checks pass in both themes at
`artifacts/smoke-bar-chart-ruler`. The catalog registers both layouts after Line
Charts; run `scripts/smoke-catalog.ps1 -Component bar-chart`. Additional native
checks pass at `artifacts/smoke-bar-chart-live`: exact source category tspan
wrapping/baselines, original collection and brush identity, observable records,
live orientation/grid/legend, pointer bands and retained keyboard inspection,
physical Tab/Shift+Tab/Enter/arrows, disabled inheritance, remount and template
restoration, replacement collections, range tooltip content and invalid enums.
The Tab regression checks the actual focus destination because its key release
naturally belongs to the new focus target. Arrow/Enter delivery retains the
normal exactly-one-press/release checks. These checks run in focused and full
catalog modes.
Both x64 and ARM64 solution builds pass with zero warnings. All 43 unit tests
and the pinned token-generation check pass at `artifacts/tests-bar-chart`.
The full Light/Dark catalog passes at `artifacts/smoke-bar-chart-full-restored`,
with the six-minute deadline unchanged. This covers the existing Line Chart,
Sparkline, Stat Card, tooltip, input and cursor checks alongside the new Bar Chart
matrix. The package and published-output results follow below.
The focused `bar-chart-sequence` route also runs Sparkline and Stat Card after
Bar Chart in each theme. It exposed and now verifies the native input-session
restoration correction described in [Sparkline evidence](sparkline-reference.md).

Fresh NuGet 0.1.0 from clean implementation commit
`8e2ee80b2ca83ddf4e0a291833c2cf3c6d102329` has SHA-256
`71e0ccc1a22c4f4d1e0a17b09c4293d14a7afe39a6a0adb365f4561f1cdeae47`.
The isolated x64 consumer passes both themes at
`artifacts/package-consumer-bar-chart`, including grouped geometry, original
collection identity, native inspection, live orientation and replacement records.
The isolated ARM64 consumer cross-build passes at
`artifacts/package-consumer-bar-chart-arm64`; runtimeVerified remains false.
Both GUID-scoped temporary caches are removed. The separately published x64
catalog passes the Bar Chart → Sparkline → Stat Card sequence in both themes at
`artifacts/smoke-bar-chart-published-sequence`. Runnable output is
`artifacts/catalog-bar-chart`; the package is
`artifacts/packages-bar-chart/Gaulatti.Orchard.0.1.0.nupkg`.

This component remains in progress. Complete raster/glyph/legend/tooltip effects,
normal/interrupted motion, full formatter/content/key/override/RTL matrix,
component theme and automation behavior, display/text scaling and native ARM64
acceptance remain required. A successful static path comparison does not establish those
requirements. Push-triggered Actions remain paused at the user's request.

The pinned React 19.2.4/Recharts 3.8.1 browser confirms that active mouse hover
keeps priority over a real keyboard action; leaving the chart reveals the
remembered keyboard category. `tests/Reference/ChartPointerPriority.json`
records that six-step sequence, with source/bundle/lock verification in
`scripts/derive-chart-pointer-priority.mjs`. The native fixture now exercises
real hover, Right while hovered, pointer exit and Left restoration before its
original keyboard sequence. It awaits the actual GotFocus receipt. All
existing source assertions, two-second deadlines and exact physical-event
receipts remain. Focused checks pass both themes; 57 local tests pass. The
complete revised published run is pending.
