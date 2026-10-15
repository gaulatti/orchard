# Radial Bar Chart source contract and acceptance

The frozen [Radial Bar Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/radial-bar-chart.tsx)
has SHA-256 `609a43f3e779452bb9ed5777178592c9f86936132f6bcd817a50bd01239e213a`.
The pinned runtime is React 19.2.4 / Recharts 3.8.1; Orchard uses .NET SDK
10.0.401, Windows App SDK 2.5.1 and Win2D 1.4.0. The exact
[Recharts release](https://github.com/recharts/recharts/releases/tag/v3.8.1)
was checked before implementation. The public repository wiki is unavailable;
this document records the contract in the code repository.

Acceptance requires original consumer-owned observable records to drive each
radial bar, its background, curved inside-start value label, right-hand legend
and native inspection. Data items supply name/value and optional native fill.
Height defaults to 350, inner/outer radii to 10%/80%, ShowLegend to true, and
start/end angles to 90/-270. Numeric and percentage radius values must follow
the pinned producer. Explicit transparent paint must retain consumer ownership.
Backgrounds use the muted role, source corners use radius 8, and value labels
use primary text with semibold typography. Do not replace curved labels with
straight text or approximate their placement from a bounding box.

Before implementation, capture the unchanged source with its exact frozen
dependency lock and bundled fonts loaded. The matrix must cover both themes,
one/two/many/empty items, zero/constant/negative/mixed values, literal and missing
fields, duplicated/long/non-Latin names, explicit paint, numeric/percentage and
inverted/constrained radii, both angle directions, partial/full/zero spans,
fractional and constrained sizes, and legend modes. Record foreground and
background paths, curved label paths and glyph positions, legend allocation,
tooltip payload/visibility/geometry and real focus, arrows, Enter, hover/leave.

Trace and verify the complete native path: dependency properties and original
collections, record notifications, scales and bar layout, rounded paths,
curved glyph rendering, theme/template resources, one named automation stop,
native pointer/keyboard input, disabled traversal, stationary updates, real
detach/rapid reparent, template and collection replacement, resize and empty
data. Register catalog navigation/coverage and focused/full runtime checks.
An independent fresh NuGet consumer must exercise the packaged control,
resources, data, input, labels and paint. Verify both architecture builds and
the full published catalog without weakening existing checks or deadlines.

Work is in progress and this inventory entry is not accepted. Complete raster,
motion/reversal, full data/override/RTL coverage, actual contrast, Narrator,
display/text scaling, collection performance and native ARM64 acceptance remain
required. This native-only presentation work has no backend/Prometheus surface.

The unchanged-source matrix has 80 cases, SHA-256
`cd1a4c261b47ba500aab3e19fa5d584e5e9d8e0a046f220b5b54c33c69a223b0`,
in `tests/Reference/RadialBarChartLayout.json`. Browser page errors are zero.
The x64 native comparison passes all 80 Light/Dark cases, including every
sector/background path, curved glyph boundary and rotation, palette, legend
and actual keyboard/pointer inspection. Both native lifecycle suites pass in
`artifacts/smoke-radial-bar-chart-final`. The pure path regression checks all
550 source glyphs; all 45 portable regressions pass. These measurements retain
the .001 coordinate limit and do not establish complete glyph raster parity.

`BleeckerRadialBarChart.Data` retains the consumer's original observable
collection and record objects. `BleeckerRadialBarChartDatum` supplies Name,
finite Value and an optional native Fill brush with change notifications.
Dictionary records also follow the frozen producer's scalar/range/missing-value
semantics. Background sectors use muted paint, values follow the source numeric
domain, and the name-sorted legend retains each record's original palette index.
StartAngle/EndAngle and numeric or percentage InnerRadius/OuterRadius remain
live. Invalid radius strings use the producer's inner zero/outer 80% defaults.
Set an automation name on the wrapper; its inner chart is the single named
keyboard inspection stop. Native Tab, arrows and Enter pass through that stop.

SVG text uses measured conic path distances rather than analytic circle
lengths. The implementation follows Chromium 155's Skia at
[`29ed1e87`](https://github.com/google/skia/tree/29ed1e87a0a50f3d8347e988842d9d59e7573efa),
specifically its circular SVG arc construction and conic contour measurement.
The derived code's BSD license is included in source and NuGet. Source-derived
glyph-boundary regressions retain the .001px coordinate limit. Shape text with
DirectWrite before positioning its glyph outlines; keep each glyph's source
layout allocation and the path's finite length. The official Win2D
[glyph-run API](https://microsoft.github.io/Win2D/WinUI3/html/M_Microsoft_Graphics_Canvas_Geometry_CanvasGeometry_CreateGlyphRun.htm)
and [renderer contract](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Text_ICanvasTextRenderer.htm)
were consulted for the pinned native implementation.

The regular `0.1.0` NuGet package from clean source `43d200c`, SHA-256
`102fec25ab72d99f068f56992beb6c59b69f189adbdb4e8758356565a84edc99`,
passes the fresh isolated x64 consumer in both themes at
`artifacts/package-consumer-radial-bar-chart`. That consumer loads actual curved
font outlines, checks source geometry, original data identity, typed record
notifications, native Right inspection, transparent fill, radius/name changes
and empty cleanup. The same package passes the separate ARM64 build-only
consumer at `artifacts/package-consumer-radial-bar-chart-arm64`; ARM64 runtime
is unverified. Both isolated restore/build caches are removed.

The runnable x64 catalog at `artifacts/catalog-radial-bar-chart` is built from
`43d200c`. Its complete published Light/Dark run passes in 357.220 seconds at
`artifacts/smoke-radial-bar-chart-published-full`, within the unchanged
360-second deadline. All 928 earlier PNGs remain, with 12 new source captures.
All 992 native pointer gates pass their original ownership and two-unchanged-
transition checks, within the two-second presentation limit. Sixteen final
capture layout reports also settle. The focused Radial Bar → Radar → Scatter
→ Sparkline sequence passes both themes. Light/Dark default captures were
visually inspected; complete raster/platform acceptance remains open.
