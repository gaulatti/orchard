# Pie and Donut source contracts and evidence

The frozen [Pie Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/pie-chart.tsx)
has SHA-256 `985309bd4131c154f88fcca7f03839519da66192e49bd940b92e3307a9ddf0ac`.
The [Donut wrapper](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/donut-chart.tsx)
has SHA-256 `dc5a9151b7501563651ba025c82da3b0b976e12fcc5fef0a50720aa6f00874f8`.
React 19.2.4 and [Recharts 3.8.1](https://github.com/recharts/recharts/releases/tag/v3.8.1)
are pinned by the source lock. Its installed `Pie`, `Sector`, keyboard middleware
and tooltip code establish the geometry and inspection behavior. Native
dependencies retain [Windows App SDK 2.5.1](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1).

`BleeckerPieChart` accepts an original observable collection of read-only record
interfaces, `NameKey` (default `name`), `DataKey` (default `value`), `InnerRadius`
(a finite number or source numeric/percentage string, default 0), `ShowLegend`
(true), and the normal native `Height` (300). `BleeckerPieChartDatum` supplies
the default name/value/color record keys with live native dependency properties
and `INotifyPropertyChanged`. Its name and finite value are required; native
`Brush` colors, including transparency, remain consumer-owned. Generic records
support custom, nested and literal-priority keys. Null and missing values are
distinguished where the source supplies a default.

```csharp
var chart = new BleeckerPieChart();
chart.Data.Add(new BleeckerPieChartDatum { Name = "Harbor", Value = 100 });
chart.Data.Add(new BleeckerPieChartDatum { Name = "Reed", Value = 220 });
```

The source's responsive internal viewport rounds dimensions to whole pixels.
Chart margins are 5 on every edge. A shown legend reserves 36px even when empty;
its rectangular icons use the shared palette and its entries sort by name.
The radius scale is half the smaller available plot dimension. Outer radius is
80% of that scale. Multiple records reserve a 2-degree gap per nonzero record;
zero values retain labels and keyboard payloads but produce no slice path.
A single record disables padding and uses the source's 359.999-degree arc.
The sum and percentages preserve signed values; a nonpositive sum produces no
slices or labels. Numeric strings and Booleans do not contribute to the sum,
but their raw tooltip payloads follow the source's formatter. Sector coordinates
round to four decimal places. Slice labels are 16px in the slice's color, at
outer radius plus 20, with the source's percentage and anchor.

`BleeckerDonutChart` uses the same original records and Pie renderer with an
inner radius of **60% of the radius scale**, rather than 60% of outer radius.
It exposes `Height`, `ShowLegend`, `CenterValue` and `CenterLabel`. Center text
is independent of the legend and uses the entire unrounded outer allocation
with 24px bottom padding. Value text is 24/32, semibold with -0.02em tracking;
label text is 12/16. The string `0` is visible. Center text collapses CSS ASCII
whitespace and passes through the existing primary font and theme resources.
The value uses the existing semibold font asset rather than a synthetic weight.
Browser measurements establish a 1/64px inline allocation, including trailing
tracking. A platform-font audit identifies Microsoft YaHei for Han, Yu Gothic
for kana and Segoe UI for Arabic when Encode Sans lacks those glyphs. Native
font runs preserve these faces and separately rounded advances, including
spaces at a font boundary; consumer font overrides remain authoritative.
Legend labels use the measured 18px baseline in each 24px row, with the 14px
icon allocation starting at 6.65625px. These are measured source positions,
not relaxed native comparison bounds.

Both controls expose one native category inspection stop with the consumer's
name. Left/Right, Enter, Tab and pointer inspection use normal native input.
The visual tooltip has one raw value row, an empty name, no category heading,
and no added cursor or active marker. Existing scoped tooltip effects and
observer/lifecycle handling are shared with the cartesian charts. Source slice
borders are white in both themes; a separate native theme resource supplies
the Windows contrast adaptation.

`tests/Reference/PieChartLayout.json` contains 84 actual browser cases: Pie and
Donut, both themes, empty/zero/single, mixed signs, duplicate names, explicit
brushes/transparency, narrow/fractional sizes, long text, 12 palette entries,
numeric and percentage radii, custom/nested keys, numeric strings/Booleans and
center text variants. Canonical SHA-256 is
`ddefde419f8215d401a8c5f15a054b88682ca18cddf8ab19bad6b40a53a65ce7`.
Import with `scripts/derive-pie-chart-reference.mjs`; browser files are under
`artifacts/pie-chart-browser`. The first 20ms key captures recorded seven empty
Enter tooltip wrappers as still visible. Eight targeted browser recaptures at
100ms retained identical slice paths and confirmed the settled hidden state;
their actual keyboard states replace those captures. Initial and probe JSON
remain available. The capture script now uses the longer observation interval.

The catalog registers a shared Pie/Donut page after Area Charts. Focused modes
`-Component pie-chart` and `-Component donut-chart` run both controls' source
comparisons and live native input/data/lifecycle checks. Initial native Light
and Dark runs pass all 84 source cases at
`artifacts/smoke-pie-chart-final`, including exact center horizontal
allocation, baselines, slice paths, palette, legend text, keyboard payload and
visibility. Separate lifecycle reports verify typed notifications, original
collections and brushes, Tab/arrows/Enter, real hover, live radius/center,
disable, real detach, rapid reparent, templates, empty data and center font
overrides. Browser/native Light center, multilingual Pie and Dark ring
screenshots were inspected; complete raster acceptance remains
open. The initial reference timing issue is retained at
`artifacts/smoke-pie-chart-initial`. Further build and package evidence is
recorded after verification.

The first full published run, `artifacts/smoke-pie-chart-published-full`, reaches
the final Dark screenshot sequence but exceeds the unchanged 360-second limit;
it is not a passing run. File timestamps attribute about 11.2 seconds to the
new fixture. The screenshot blank check also needlessly scans every color byte
after proving its required variation. It now stops after nine distinct
non-alpha byte values: the same `> 8` predicate with an early exit. An actual
1024x745 capture and blank/eight-value/varied inputs retain equivalent results;
20 benchmark repetitions take 1059.104ms before and 19.530ms after. Capture
pixels, required checks and deadlines remain unchanged.
The same early exit applies to composed screen capture's duplicate blank
predicate. The next run fails a pre-existing Attention fractional-scroll mask;
its retained pixels and the stronger presentation gate are documented in
[the Attention contract](attention-surface-reference.md). That run is also
not accepted as a complete catalog pass.

Release x64 and ARM64 solution builds pass without warnings; all 44 regression
tests and generated-token validation pass. A regular `Gaulatti.Orchard.0.1.0`
package was produced from clean commit
`d95e454d28afb91c2c23c33d8300122d01e472c2`, with SHA-256
`d26aa70c763c39921f99a0fa1badd3aecd87f47e27a93cb983892135be5b9aae`.
Its repository metadata is checked against that source commit. The isolated
ARM64 consumer builds at `artifacts/package-consumer-pie-chart-arm64` with
`runtimeVerified: false`; its generated temporary workspace is removed.
The same package passes the independent x64 consumer's actual Light/Dark
runtime at `artifacts/package-consumer-pie-chart`. Both new public controls and
the typed model are consumed from the NuGet assembly, with original collection
identity, source sector/ring geometry, native focus/Right, live datum changes,
transparent brush identity, live radius/center and collection replacement
verified. That consumer's temporary restore/build workspace is also removed.
The public wiki remote remains unavailable, so this contract and verification
evidence are maintained in the repository. Push-triggered Actions remain
paused at the user's request.

The final published catalog from `220a8ec` passes the complete Light/Dark
sequence at `artifacts/smoke-pie-chart-published-settled-full` in 352.436 seconds
under the unchanged 360-second limit, saving all 904 PNG captures. Both
42-case theme reports and both live Pie/Donut lifecycle reports pass. Attention
also retains its exact 124-pixel masks in each theme. The runnable final catalog
is `artifacts/catalog-pie-chart-settled`. Library sources and all package inputs
remain unchanged from the independently verified `d95e454` package; later
commits change catalog capture/observation and documentation only.

Both components remain **in progress**. Full sector/glyph/legend/center/tooltip
raster and effects, normal/interrupted motion, the complete data/key/radius/
formatter/override and RTL matrix, actual contrast, Narrator, display/text
scaling, collection performance and native ARM64 acceptance remain required.
This native presentation change has no backend or metrics surface.
