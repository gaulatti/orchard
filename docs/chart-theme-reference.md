# Chart theme source contract (in progress)

The frozen [Bleecker chart-theme source](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/chart-theme.ts)
has SHA-256 `94e006606feb77fc8163d05812e83668b04916dcdbb818a8aa8dc0d49e08e750`.
It exports `chartTheme`, `pickColor` and `ChartSeries`. It defines shared chart
configuration rather than a standalone rendered component. The Windows
inventory entry remains in progress; the internal metadata port does not
constitute a public native API or accepted chart rendering.

Colors are references to the active Bleecker CSS variables. They must remain
live references to the equivalent scoped Windows theme resources, preserving
light/dark changes and consumer brand overrides. Resolving a global palette
once into fixed RGB values would lose that contract.

The named roles are sea, desert, terracotta, sunset, dusk, accent gold, accent
blue, primary/secondary text, background, card and border. The automatic palette
has exactly ten entries, in this order:

1. sea
2. desert
3. terracotta
4. sunset
5. dusk
6. accent gold
7. accent blue
8. accent oxblood
9. accent bronze
10. accent yellow

Grid strokes use the border role with a 3/3 dash pattern. Axis strokes use the
border role; tick text uses secondary text at size 12. Those values belong to
the chart theme and must be consumed by each applicable native chart.

The chart tooltip differs from Orchard's general tooltip: card background,
12px radius, a one-pixel border-role stroke, a black shadow at offset (0,8),
blur 32 and opacity .08, and backdrop blur 8. Item text uses primary text;
label text uses secondary text at weight 600. Reusing a general tooltip without
these chart-specific values would not satisfy the frozen contract.

`pickColor` indexes the palette using JavaScript remainder, not mathematical
modulo. Nonnegative integer indices repeat every ten entries. Negative multiples
of ten, including negative zero, select sea. Other negative indices, fractional
indices and nonfinite numbers return `undefined` at runtime. No floor, absolute
value or positive-wrap correction is present in the source. The native API must
make unsupported input behavior explicit instead of silently choosing another
color. The exact source behavior is retained in
`artifacts/chart-theme-reference/helper-cases.json`, evaluated from the verified
TypeScript module with the repository's source-capture TypeScript runtime.

`ChartSeries` requires consumer-owned key and name strings and permits an
optional color. Keys identify consumer data; names label a series. Explicit
series colors and palette assignment must be traced through the actual chart
consumers before choosing their native representation. This module supplies no
data fetching, persistence, application operations or independent animation.

Implementation must cover scoped resources, every role and palette entry,
exact default styling, live changes and overrides, series updates, and each
chart consumer. Native chart fixtures must provide the rendered, accessibility,
scaling, contrast, input and performance evidence appropriate to that chart.
The helper audit does not waive the chart group's remaining requirements.

The public wiki remote is unavailable, so the source contract is recorded here.
Orchard has no backend or metrics surface affected by this audit.

`src/Orchard/Charts/BleeckerChartTheme.cs` now retains every named role and palette
entry as a native theme-resource key, alongside the immutable grid/axis/tooltip
metadata. CSS shadow/backdrop blur values remain explicitly named CSS pixels;
the native renderer must perform its own verified conversion rather than
assuming SDK blur units have the same meaning. `PickColor(double)` preserves
the frozen remainder cases and returns null for the source's undefined results;
it never floors or positive-wraps an invalid index.

The source-generated fixture is committed at `tests/Reference/ChartTheme.json`.
Three regressions compare palette selection and all style defaults with the
verified source module, then require every resource reference to exist in each
of Light, Default and HighContrast. All 31 unit/resource tests pass at
`artifacts/tests-chart-theme`, and both architecture builds have zero warnings.
These are metadata/resource-boundary checks. Public ChartSeries/native theme
APIs, live scoped resource resolution and overrides, catalog rendering, native
chart consumers and their entire acceptance matrices remain unimplemented.
