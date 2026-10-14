# Line Chart frozen contract and acceptance

The frozen [Line Chart source](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/line-chart.tsx)
has SHA-256 `673539e9a2c5a264fbfb6c35f09e7d956d2dbde9670f3ae6cf1888e752ec403a`.
Its package lock pins Recharts 3.8.1, victory-vendor 37.3.6, D3 Shape 3.2.0,
React/React DOM 19.2.4 and Tailwind 4.2.2. The upstream
[3.8.1 release](https://github.com/recharts/recharts/releases/tag/v3.8.1)
and installed version's source establish the version boundary; current Recharts
documentation describes later behavior and cannot replace it.

The consumer supplies record-shaped data, series and an X-axis data key. Height
defaults to 300. Grid and legend default to visible. The X/Y labels and numeric
tick/tooltip formatters are optional. Margins are top 10, right 20, bottom 25
when the X label is nonempty (otherwise 5), and left 20 when the Y label is
nonempty (otherwise 0). Recharts adds its 60px Y axis and 30px X axis inside
these margins. The legend's measured size also removes space from the plot.
At width 640 with the default two-series specimen, the plot runs X=60..620
and Y=10..225. Hiding the legend increases the plot bottom to 265. A narrow
240px chart wraps its legend and reduces the plot bottom to 201.

Each series uses its key/name unchanged, an explicit consumer color or its
index in the shared ten-color palette, a monotone curve, stroke width 2 and no
ordinary dots. Active dots have radius 5, stroke width 2 and the card-resource
stroke. Null/nonfinite coordinates divide the curve into separate segments;
connectNulls remains false. A single data row does not produce an ordinary
visible line or dot. The plot, legend, tooltip and active dot must preserve
explicit transparent paint rather than replacing it with a palette color.

Grid strokes use border/3-3 dashes and only horizontal lines. Axis tick text
uses secondary text at size 12; tick and axis lines are disabled. Numeric Y
domains/ticks use the pinned Recharts adaptive algorithm, including negative
and zero data. The observed default data maximum 310 produces 0/80/160/240/320
ticks. All-zero data produces 0/1/2/3/4. Category labels are measured and
filtered for spacing; long labels can remove earlier ticks. Native acceptance
must preserve the algorithm and text-measurement boundary rather than hardcode
these examples.

The default legend sorts entries by their displayed names. Its source line
icon is 14px with a 4px gap and 10px item margin. The wrapper adds 16px top
padding. Tooltip content uses the chart-specific theme, label and series names,
numeric formatter results when provided, and the source's empty/null behavior.
The source chart is one keyboard stop with application semantics. Initial focus
opens the first category. Left/Right navigation retains a separate keyboard
index and ignores moves beyond either boundary. Enter toggles inspection; blur
hides it without discarding that index. Refocus alone leaves retained inspection
hidden; Enter reopens it. Pointer hover takes precedence and disables keyboard
inspection; pointer exit hides hover without replacing the retained keyboard
index. A subsequent arrow moves from the retained keyboard category. Data
shrink clips the resolved index; empty data has no inspection output.

`LineInteraction.json` records 18 actual browser actions in each theme against
the unchanged source and pinned Recharts 3.8.1. Native state regression coverage
uses these captured outcomes. Catalog and independent package fixtures exercise
the normal WinUI focus and native keyboard path; lifecycle remounts wait for
actual Loaded/Unloaded events. Native accessible names, values,
keyboard/pointer focus and ownership still require actual UIA/Narrator evidence.

## Evidence and implementation boundary

The unchanged React component is bundled with all 43 runtime dependency entries
checked against the frozen package lock. The complete frozen source tree—not
the earlier partial component tree—supplies the Tailwind stylesheet. That
distinction matters: a partial build omitted desert and hid the second series.
The corrected capture refuses missing default palette paint or axis ticks.
It uses local bundled fonts, Edge at raster scale 1, and reduced motion without
changing Windows preferences.

`artifacts/line-chart-browser/reference.json` retains 22 Light/Dark specimens:
default, grid/legend off, labeled/formatted axes, empty, single, negative, zero,
gaps, explicit/transparent colors, narrow and long-category data. The directory
also retains source snapshots, lock, capture/bundle scripts, DOM curves/ticks,
keyboard tooltip/active-dot markup and PNGs. The initial partial-styles captures
are superseded and do not establish parity.

`BleeckerMonotoneCurve` is an internal geometry foundation adapted from D3 Shape
3.2.0. It retains finite-coordinate segment boundaries, coincident-point
suppression, one-point closure, two-point lines and exact cubic control points.
`tests/Reference/LineCurve.json` contains 64 cases evaluated by the unmodified
pinned D3 context, including increasing/reversed/vertical/coincident inputs,
gaps, nonfinite values and varied curves. The regression compares native
commands and coordinates to that producer. `LineData.json` adds 71 source
five-tick domains, 11 literal/nested data-key cases and 23 numeric conversions
evaluated by the frozen selector. All 36 unit regressions pass at
`artifacts/tests-line-numeric`. D3, es-toolkit and Recharts notices are retained
and required by the package verifier.

## Native API and current boundary

`BleeckerLineChart` exposes observable `Data` records and `Series` collections.
Each record implements `IReadOnlyDictionary<string, object?>`; notifications
from records implementing `INotifyPropertyChanged` schedule a render, as do
collection replacements and series changes. Supply `XAxisKey` before mounting;
a missing key fails explicitly. Literal keys take precedence over nested paths.
Empty strings, whitespace, radix strings and finite decimal/exponent strings
follow the frozen numeric-domain conversion. Booleans and invalid/nonfinite
domain values do not contribute ticks.

```csharp
var chart = new BleeckerLineChart { Width = 640, XAxisKey = "category" };
chart.Series.Add(new BleeckerChartSeries { Key = "total", Name = "Consumer total" });
chart.Data.Add(new Dictionary<string, object?> { ["category"] = "Harbor", ["total"] = 100d });
```

`ShowGrid`/`ShowLegend`, axis labels, `YAxisTickFormatter` and `TooltipFormatter`
are consumer-owned dependency properties. Scoped theme expressions supply
palette paint; an explicit series brush, including transparent paint, keeps
precedence. The control renders native monotone paths, category/numeric axes,
a wrapping name-sorted legend and category inspection with Arrow/pointer input.
Supply an accessible name through `AutomationProperties.Name`.

The registered Line Charts fixture exercises live collection/series changes,
formatters, transparent paint, empty data and actual Arrow input in both themes.
Its initial local run passes at `artifacts/smoke-line-chart-labels`. The fixture
also embeds `Reference/LineLayout.json`, derived from all 22 full-source browser
captures, and compares native curve commands, grid coordinates, visible tick
labels, series/axis paint and legend order. The 0.000501 curve tolerance follows the source SVG
producer's three-decimal coordinate rounding. These checks do not prove font
raster, every label position, tooltip effects or motion parity. The independent
package consumer includes real chart templates and live consumer mutations.
The local x64 run passes all 22 specimens, including paint assertions, at
`artifacts/smoke-line-chart-paint`. Derive the embedded reference from the
retained capture with
`node scripts/derive-line-chart-reference.mjs artifacts/line-chart-browser/reference.json`;
the output records the input's
SHA-256. The independently restored/published x64
consumer passes in both themes at `artifacts/package-consumer-line-chart` for
package SHA-256 `24db115d2aa000d7ea9694fef184b4380111a3c27034b7447ffa818dee9f257d`.
Both solution architectures build without warnings; native ARM64 verification
of the new public control remains required.
The complete local x64 catalog also passes in Light/Dark at
`artifacts/smoke-line-chart-full`, with display/text scale 1, animations disabled
and contrast off. This does not establish enabled-motion, contrast or scaling
acceptance.

## Tooltip source and composed-render evidence

The native tooltip now uses 24px text lines and 4px top/bottom padding for each
item, producing source heights of 110px for two items and 78px for one. Numeric
formatters leave raw string values unchanged, including strings excluded from
the numeric axis. A category with only null values retains its cursor while
the tooltip stays hidden. Pointer inspection follows the physical pointer's Y
coordinate; keyboard inspection uses `(plotTop + chartHeight) / 2`. Recharts'
10px offset and edge-flip/clamping rules are preserved, including oversized
tooltips extending beyond a narrow plot.

`TooltipPlacement.json` contains 60 outputs from the locked Recharts placement
function. All 37 regressions pass at `artifacts/tests-tooltip-placement`.
`Reference/LineTooltip.json` records 26 Light/Dark source specimens with actual
tooltip payloads, dimensions and placements. Derive it with
`node scripts/derive-line-tooltip-reference.mjs artifacts/line-chart-browser/tooltip-reference.json`.
The capture moves the pointer outside the chart before keyboard inspection to
keep the previous specimen's hover from contaminating the next one.

The chart owns a rounded, clipped Composition shadow and Win2D Gaussian
backdrop effect. Its rounded vector mask is now owned by Composition and sampled
through a `CompositionVisualSurface`, independent of XAML mask visibility and
offscreen layout. Effects are cleared when closed,
unloaded or retemplated, and suppressed under Windows contrast. The shadow is
excluded from the border box, preserving translucent consumer card paint.
Implementation uses the pinned Windows App SDK 2.5.1 and Win2D 1.4.0 APIs;
[Microsoft's visual-layer guidance](https://learn.microsoft.com/en-us/windows/apps/develop/composition/using-the-visual-layer-with-xaml)
documents the mask/shadow and backdrop-effect approach. The
[CSS filter definition](https://drafts.csswg.org/filter-effects/#funcdef-filter-blur)
establishes the backdrop's 8px Gaussian standard deviation.

The local `artifacts/smoke-line-tooltip-mask` run passes the 22 prior chart
specimens, all 26 tooltip specimens, actual keyboard/pointer and raw-string/gap
checks, composition cleanup, and both effects' real pixels in Light/Dark.
`LineBlur.json` compares eight samples from blurred black/white 8px stripes
under a consumer-owned white card at opacity 0.25. `LineShadow.json` compares
eight samples across the outside shadow. Each RGB channel must be within two
levels of the frozen browser producer; the first shadow implementation failed
this check because it painted no visible shadow. These are sampled effect
profiles, not complete tooltip/font raster acceptance. Native/source tooltip
widths and captures remain recorded for review.
Run `38517156846` exposed an unpainted shadow on both hosted architectures,
despite local success. The vector-mask candidate passes the unchanged local
pixel checks at `artifacts/smoke-line-tooltip-vector-mask`. The fixture waits for
two actual rendering frames and an explicit compositor commit before sampling
the first tooltip and blur probe. Both stages share the existing two-second deadline;
RGB tolerances remain unchanged. Run `38519659390` passes the shadow profile
on native ARM64 and the complete focused chart on x64. ARM64 then fails the
zero-data tooltip's left edge placement; the full hosted chart is not accepted.
Both updated solution builds, the complete local x64 catalog at
`artifacts/smoke-render-corrections-all` and a fresh x64 package consumer at
`artifacts/package-consumer-render-corrections` pass. The current package has
SHA-256 `e3925e5319e063b89417207db4b555f00bebe5943cffadd287f09c9460ec832a`.
Both updated solution builds pass without warnings. The newly restored and
published x64 consumer passes actual tooltip keyboard input, native dimensions,
shadow/backdrop graphs and cleanup in Light/Dark at
`artifacts/package-consumer-line-tooltip`, for package SHA-256
`893bae053e9a45e4476d5a3889b361c650306ceeea91e3aea7984b33b08e789b`.
The complete local x64 catalog passes at `artifacts/smoke-line-tooltip-full`.
Native ARM64 tooltip verification remains required; the earlier ARM64 focused
chart evidence predates these tooltip changes.

The source interaction checkpoint passes the actual native 18-step sequence
in each theme at `artifacts/smoke-line-interaction-native`, alongside all 22
chart and 26 tooltip specimens and sampled effect pixels. Focus, Enter, blur,
refocus, pointer precedence/exit, boundary arrows and data shrink/empty actions
use normal WinUI focus and physical native input. This local x64 result uses
display/text scale 1, disabled animations and contrast off.
The fresh independent x64 consumer passes initial focus, physical arrows/Enter,
hide/reopen and retained selection across blur/refocus in both themes at
`artifacts/package-consumer-line-interaction`. Its package SHA-256 is
`302b2e59e1e7d60349a09fc2c83816c6d8b352780d2830ce3b125069d04385a8`.
This checkpoint has 38 passing regressions and clean x64/ARM64 solution builds.
The complete local x64 catalog passes Light/Dark at
`artifacts/smoke-line-interaction-all`; hosted ARM64 interaction remains required.

Remaining acceptance includes complete text/paint/legend and tooltip raster comparison,
source line/tooltip animation, plot clipping, duplicate
categories, date/object/boolean plotting and raw tooltip conversion, consumer
overrides, complete unload/reload and retemplating, measured collection performance,
contrast, Narrator, display/text scaling and native ARM64 checks. The inventory
stays in progress; this control is not an accepted complete chart component.

The catalog's retained interaction sequence now waits for actual routed key
release before changing focus or consumer data. Each injected key must deliver
exactly one down/up to the focused chart within the existing two-second
boundary. This also proves empty-data keys whose expected visual state was
already present. The original Line Chart then Error State sequence fails retry
pointer delivery at `artifacts/smoke-error-state-after-chart-handled`; with
routed key acknowledgement it passes both themes at
`artifacts/smoke-error-state-chart-key-release`. NativeInputSession records the
key counts and retained target in its desktop evidence. Product keyboard
semantics, cursor gates and pixel/timing tolerances are unchanged.
Orchard has no backend/metrics surface affected by this work. The public wiki
remote is unavailable; this in-repository contract is authoritative for the task.
