# Radar Chart source contract and acceptance

The frozen [Radar Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/radar-chart.tsx)
has SHA-256 `62d3df8715257cc90e3230b196de68744252b7aaa381240531d186444faaf969`.
It uses the pinned React 19.2.4 and Recharts 3.8.1 runtime. Orchard pins
Windows App SDK 2.5.1, Win2D 1.4.0 and .NET SDK 10.0.401. The exact
[Recharts release](https://github.com/recharts/recharts/releases/tag/v3.8.1) and
[native SDK release](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1)
were checked before implementation. The public wiki is unavailable.

Acceptance requires original consumer-owned observable data and series to drive
the polar axes, polygons, legend and inspection. SubjectKey and each series Key
and Name are required. Height defaults to 350 and ShowLegend to true. Optional
series paint overrides the live palette, including transparent brushes. Fill
enables the source's 0.35 fill opacity; its default is false. TickFormatter
formats numeric radius ticks and numeric tooltip values.

```xml
<orchard:BleeckerRadarChart x:Name="Scores" SubjectKey="subject"
                          AutomationProperties.Name="Consumer scores">
    <orchard:BleeckerRadarChart.Series>
        <orchard:BleeckerRadarSeries Key="value" Name="Current" Fill="True" />
    </orchard:BleeckerRadarChart.Series>
</orchard:BleeckerRadarChart>
```

Add records such as `new Dictionary<string, object?> { ["subject"] = "A",
["value"] = 10d }` to `Scores.Data`. Both collections retain their original
identity. Collection changes and records implementing `INotifyPropertyChanged`
update the mounted chart; replace an ordinary dictionary record to notify a
value change. Keys use the shared literal-first dotted/bracketed lookup.
Left/Right inspect unique subjects without wrapping; Enter toggles keyboard
inspection. Angular pointer inspection preserves the separate keyboard index.
Supply an accessible name for the single inner inspection stop; disabled state
inherits normally from the owner.

The reference matrix must exercise both themes, positive/negative/zero and
empty data, empty series, one/two/many subjects and series, duplicated and
missing subjects, numeric strings, missing/null values, nested/literal keys,
range arrays, formatting, explicit paint, fill modes, wrapped legend names,
constrained and fractional sizes. Load bundled fonts before mounting the
unchanged source. Capture exact paths, axes/grid, text positions and baselines,
legend allocation, tooltip payload/visibility/position and active points.

Native regression must follow the full consumer path: dependency properties,
original collections, collection and record notifications, shared renderer,
template resources, theme/font/size changes, pointer and keyboard inspection,
one named automation boundary, Tab traversal, disabled inheritance, real
detach, rapid reparent, template replacement and empty/replacement collections.
Register the fixture in navigation, coverage and focused/full runtime checks.
The independent NuGet consumer must exercise the packaged control and live data.

`tests/Reference/RadarChartLayout.json` contains 72 unchanged-source captures
(36 configurations in both themes), SHA-256
`681f5f6a0589683074c38fc4dd19f8f11b6c6d0a0f4a6479e7ea07a4f6575a9e`.
The capture validates the frozen lockfile's installed dependency versions and
integrities, loads the bundled fonts before mounting, and records no page
errors. `scripts/derive-radar-chart-reference.mjs` imports the capture without
changing source geometry, payloads or visibility.

The radius axis uses integer steps and five adaptive ticks. Series polygons
repeat their first point; grid rings close directly. Unique valid subjects
determine angles and keyboard inspection; duplicate subjects inspect the first
matching record. A missing subject uses the producer's angle-band fallback.
Range domains read the first two numeric entries and geometry uses the last
entry as its outer value. Empty series and null-only rows retain hidden tooltip
text, matching the browser. Series names sort legend and tooltip entries, while
palette indices follow original series order. Source radius labels rotate 90
degrees. Tooltip headings use the bundled semibold face while respecting a
consumer FontFamily override. Tooltip rows preserve raw accessible values while
collapsing HTML whitespace for presentation; intrinsic widths and legend
centering follow the browser's 1/64-pixel layout allocation. Arrays retain
contextual kerning across their React text nodes, then allocate each node's
width separately using Win2D's
[character regions](https://microsoft.github.io/Win2D/WinUI3/html/M_Microsoft_Graphics_Canvas_Text_CanvasTextLayout_GetCharacterRegions.htm).
The pinned Win2D-WinUI 1.4.0 entry in the
[Win2D changelog](https://github.com/microsoft/Win2D/blob/winappsdk/main/CHANGELOG.md)
and its font-file URI contract were checked before using this API.

The focused native comparison preserves the .001-coordinate tolerance, real
input and the two-second state deadlines. Run `scripts/smoke-catalog.ps1` with
`-Component radar-chart` or `radar-chart-sequence`; full smoke also includes it.
`tests/PackageConsumer/MainWindow.RadarChart.cs` verifies package resources,
source geometry, original data, native Right, replacement records, fill,
transparent paint, formatters and empty data in both themes.

The focused x64 run at `artifacts/smoke-radar-chart-context` passes all 72 cases
and both lifecycle suites at display/text scale 1 with OS animations disabled.
The normal filled chart was visually inspected beside the browser capture.
The Radar → Scatter → Sparkline sequence also passes in both themes at
`artifacts/smoke-radar-chart-sequence`. The fixture starts outside the first
chart before normal capture; each verified source outside action establishes
the next mount's pointer setup, avoiding a redundant setup move. All source
actions, comparisons and deadlines remain active.

The clean regular `0.1.0` package from `2fdb1c9` is
`artifacts/packages-radar-chart/Gaulatti.Orchard.0.1.0.nupkg`, SHA-256
`7d38ea5ccab8ca98e2cf0011557c5f7d24712579bf2c4122ca221586a15b2070`.
The isolated x64 consumer at `artifacts/package-consumer-radar-chart` passes
actual Light/Dark runtime, including live range geometry and the source's
64.90625px contextual text width through the packaged font path. The same
package builds in the isolated ARM64 consumer at
`artifacts/package-consumer-radar-chart-arm64`; native ARM64 runtime is unverified.
Both temporary restore/build caches were removed. Final x64/ARM64 solution
builds have zero warnings; all 44 regressions and generated tokens pass.

The complete published x64 catalog from `d1ea925` passes both themes in
355.519 seconds under the unchanged 360-second limit at
`artifacts/smoke-radar-chart-published-full`. It retains 928 PNG captures,
sixteen settled final-capture reports, both Radar source/lifecycle suites and
the existing components' checks. All 768 native pointer presentation gates pass,
with owned final points and at least two unchanged transitions; twenty wait for
an initially unowned point. Maximum settling is 184.4789ms. The runnable catalog
is `artifacts/catalog-radar-chart`. Push-triggered Actions remain paused.

No Radar inventory entry is accepted. Full raster/effects, normal/interrupted motion, complete data and
override/RTL coverage, actual contrast, Narrator, display/text scaling,
collection performance and native ARM64 acceptance remain required. This native
presentation work has no backend or Prometheus surface.
