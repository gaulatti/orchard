# Area Chart source contract and acceptance

For the 2026-10-29 x64 milestone, Area Chart is accepted: the exact .81
catalog passes 40 Light and 40 Dark cases with physical pointer/keyboard/focus;
the independent .81 package consumer compiles and passes four native 150%
no-clip cases with static geometry under reduced motion. ARM64 runtime is
unverified. Older checkpoint gaps below are historical and outside this milestone.

`BleeckerAreaChart` implements the public properties of the pinned [Bleecker
Area Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/area-chart.tsx).
Source SHA-256 is `885d1f48264dd0b2f55a0fde90c554bd12fa983dee7c2c50b7f794faf8b6fdce`.
The lock pins React 19.2.4 and [Recharts 3.8.1](https://github.com/recharts/recharts/releases/tag/v3.8.1),
whose installed `Area`, `Curve`, `ChartUtils`, and D3 Shape 3.2.0 sources establish
the geometry and stacking rules. Native dependencies retain the repository's
pinned [Windows App SDK 2.5.1](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1).

Consumers supply original observable `Data` records, `Series`, and `XAxisKey`.
Height defaults to 300, grid and legend are visible, and `Stacked` defaults to
false. Numeric Y tick and tooltip formatters are optional. Literal record keys
take priority over nested dotted/bracketed paths. Records implementing
`INotifyPropertyChanged`, collection changes, series changes, and original
consumer brushes remain live. A missing mounted data/series/key contract fails
explicitly. There is one native keyboard inspection stop with the consumer's
accessible name and Area Chart class identity.

```csharp
var chart = new BleeckerAreaChart { XAxisKey = "category", Stacked = true };
chart.Series.Add(new BleeckerChartSeries { Key = "count", Name = "Consumer count" });
chart.Data.Add(new Dictionary<string, object?> { ["category"] = "Harbor", ["count"] = 12d });
```

The source rounds the internal responsive viewport to whole pixels, reserves
60px for Y and 30px for X, and uses margins top 10/right 20/bottom 5/left 0.
The legend uses the shared line icon, source name ordering, 16px top padding,
and measured 24px rows. Even an empty visible legend reserves its padding.
Only horizontal dashed grid lines render. Category ticks use the observed 16px
DOM allocation ruler while the glyphs render at 12px; complete glyph/raster
equivalence remains open.

Each series has a monotone upper boundary, a fill at opacity 0.2, and a 2px
stroke using its consumer brush or the shared ten-color palette. Gaps split
curves and fills. A single record renders a radius-3 dot. Numeric ranges use
both endpoints and draw a second boundary; their active inspection shows both
radius-4 dots. The source's default active-dot border remains white in both
themes. Separate theme resources provide its cursor and border, with Windows
system colors for contrast mode.

Stacking follows series order with D3 `stackOffsetNone`, including mixed signs.
It does not split positive and negative accumulations. Two-number ranges replace
their endpoints after accumulation and do not affect later accumulated sums.
Stacked all-zero data has no numeric axis or area paths, while its category
inspection and raw tooltip remain available. Nonstacked all-negative data fills
to the numeric maximum. Boolean coordinates coerce numerically, but Boolean
tooltip content is empty. Tooltip rows sort by series name and display an empty
name followed by the source separator. A range `[10,30]` displays `1030`.
Duplicate categories use the first matching payload while the cursor retains
the inspected category position.

Left/Right inspect categories, Enter toggles inspection, and pointer hover takes
over until exit while retaining the keyboard position. Tab and Shift+Tab traverse
one inspection stop. The shared native input, observation, scoped tooltip
backdrop/shadow, and lifecycle infrastructure remain in use.

`tests/Reference/AreaChartLayout.json` freezes 80 actual browser cases (20
specimens × two stacking modes × two themes), including negative/zero/empty,
gaps, numeric strings/Boolean/range values, duplicate and nested/literal keys,
consumer transparency, fractional/narrow sizes, formatters, long labels and
12-series legends. Canonical SHA-256 is
`dc5652d060d5048edb17ba2f7ee517445eb6004a6b31f8323ec3ccc760987ea1`.
Import with `scripts/derive-area-chart-reference.mjs`; reproducible source
captures and PNGs are under `artifacts/area-chart-browser`.

The registered fixture follows Bar Charts and precedes Pie and donut charts. Run
`scripts/smoke-catalog.ps1 -Component area-chart` for frozen geometry/paint,
grid/tick/legend, dot and native inspection comparisons plus live mutation and
lifecycle checks. All 80 comparisons and the live checks pass at
`artifacts/smoke-area-chart-theme`; the complete Light/Dark catalog, including
live stack/unstack/restack geometry, passes at `artifacts/smoke-area-chart-full`
under the unchanged six-minute deadline. x64 and ARM64 solution builds report
zero warnings; all 43 regression tests pass at `artifacts/tests-area-chart`,
and token generation matches its pinned source. Captured native and browser
PNG specimens were visually inspected; this does not establish complete raster
acceptance.

The first native run matched geometry and payloads but rejected automatic
palette colors (`artifacts/smoke-area-chart-initial`). An unattached temporary
path resolved its brush before inheriting the chart theme. Fill/stroke/dot theme
references now stay on the rendered elements; captured colors and explicit
consumer brush identity both pass.

Independent package verification exposed two further boundaries. The native
opacity getter returns the exact float representation `0.20000000298023224`;
the consumer compares `(double)(float).2` while retaining its geometry, fill,
closure and stroke assertions. The original decimal equality rejected an
otherwise correct `(60,87.5)` filled/closed boundary.

More significantly, the original package intermittently stopped observing
live data after a quick remount. The unchanged binary's native control run at
`artifacts/package-consumer-chart-observer-control` records `Loaded` followed
by the previous `Unloaded`, with `IsLoaded` still true in that delayed callback.
The attached renderer and original collection still match, focus and keyboard
index remain correct, and no pointer events intervene, but `observedData` is
null and the tooltip retains `B : 20` after the consumer changes B to 30.
This establishes a lifecycle defect rather than a pointer or value-formatting
failure. A temporary diagnostic build altered the timing and passed; those
passes alone do not establish the repair.

The shared renderer now resets inspection and reconnects observations/effects
on the current `Loaded`, and skips stale unload cleanup while it is connected
again. This applies to Line Chart, Bar Chart, Sparkline and Stat Card history
as well as Area Chart. The native Area Chart fixture proves both real detached
observer cleanup and live path changes after rapid reparenting at
`artifacts/smoke-area-chart-observer`, in both themes. Temporary product tracing
was removed. Package failures retain actual data/observer, focus/input and
lifecycle diagnostics.

The regular 0.1.0 package from clean commit
`15375de8cff98b144a179530786cf280cc27eed6` has SHA-256
`c58dd09226a6e56b2837a12aed4bb7ca30d64afffd84dd0eab6f7440f200576d`;
its repository metadata matches that exact commit. The fresh isolated x64
consumer passes both themes at `artifacts/package-consumer-area-chart-observer`,
including physical Right, original collection identity, fill/boundary geometry,
live accumulation and replacement records after remount. Bar Chart's formerly
stale replacement value also passes. The fresh ARM64 consumer builds at
`artifacts/package-consumer-area-chart-observer-arm64`; its runtimeVerified
remains false. Both isolated temporary caches are removed. The package is
`artifacts/packages-area-chart-observer/Gaulatti.Orchard.0.1.0.nupkg`, and the
runnable x64 catalog is `artifacts/catalog-area-chart-observer`.

The complete published catalog passes both themes at
`artifacts/smoke-area-chart-published-full`, including all 80 Area Chart cases,
real detach/rapid-reparent updates, the existing Line/Bar/Sparkline/Stat Card
sequence and shared Checkbox/Input ownership. The six-minute deadline remains
unchanged. All six stationary Input remounts record two rendered, owned,
settled native cursor/border observations. The published focused Checkbox mode
also passes both themes at `artifacts/smoke-checkbox-remount-published`.

At this earlier checkpoint, broader raster, motion, content, override, RTL,
scaling, performance and native ARM64 verification remained open. Narrator
and OS contrast campaigns are excluded by the
[current library scope](library-scope.md).
This native presentation change has no backend or metrics surface.

The shared Recharts 3.8.1 mouse-priority contract is now verified with a real
hover, Right while hovered, pointer exit and Left restoration before the
fixture's original keyboard sequence. Focus waits for the actual GotFocus
receipt. All geometry, cursor/dot, paint, keyboard and timing assertions
remain. The Bar sequence now includes Area before Sparkline and Stat Card;
this complete four-profile sequence passes both themes at
`artifacts/smoke-bar-area-source-handover`. No production behavior changed.
