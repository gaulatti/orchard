# Chart theme source contract (in progress)

The current source-built x64 catalog and the package-reference consumer each
pass 170 hidden native checks across Line, Bar, Area, Scatter and Radar at
`artifacts/current/chart-theme-radar-source-v2/audit.json` and
`artifacts/current/chart-theme-radar-package-v1/audit.json`. The latter uses
the previously qualified `0.1.0-chart-theme-scatter-v1-local` NuGet, whose
cached archive hash matches the retained package; this run does not requalify
the whole package or claim an isolated restore.
Radar adds 22 checks for both themes and regular/scoped resources: real polygon
fill/stroke and legend paint, fill opacity, live key/name changes, transparent
caller brush identity and mutation, automatic restoration and scoped palette
updates. The existing registered Radar catalog page remains the visual fixture.
The independent audit rejects 20 damaged reports per host. Native ARM64,
physical input and full chart appearance acceptance remain open. Both current
fixture hosts compile for ARM64; that is build evidence only.

The preceding source catalog and fresh isolated x64 NuGet consumer pass 148
checks across Line, Bar, Area and Scatter at
`artifacts/current/chart-theme-scatter-source-v1/audit.json` and
`artifacts/current/chart-theme-scatter-package-native-v1/audit.json`.
Scatter adds two series, verifying sea/desert palette entries, scoped brush
changes, explicit transparent paint and live paint mutation in both themes.
Its grid, axis and tooltip surface defaults resolve through the same token
roles. Each host rejects 18 damaged variants and confirms no hardware input.
The independent consumer restores package
`0.1.0-chart-theme-scatter-v1-local` (SHA-256
`1b8ed813dda5352cb2d1def41aa3dcd5e87b93b6fb486b2392908602a68f390b`)
into a fresh Orchard cache; the ARM64 consumer also compiles with an ARM64
apphost. Neither native ARM64 execution nor complete chart visual, tooltip
interaction/effects, motion or scaling acceptance is claimed.

The earlier Line/Bar/Area consumer verification passes 126 background native checks
in both the catalog and independent x64 NuGet host. These checks use real plot
geometry and legends: literal dictionary keys beat conflicting nested paths,
live key/name changes update plots and labels, transparent and gradient brushes
retain caller identity, clearing explicit paint restores the automatic palette,
and local brand-brush mutations and light/dark changes reach both plots and
legends. Default palette expectations are checked against the committed token
JSON. The same fixture checks grid strokes and dashes, axis tick color/size,
and tooltip surface defaults against the frozen chart theme. It invokes the
production tooltip renderer at a controlled inspection index to verify actual
content, heading weight and text colors, including live scoped overrides.
This renderer check sends no input and does not establish pointer, keyboard,
focus routing or tooltip effects. Both hosts reject 16 altered reports.

Evidence is under `artifacts/current/chart-theme-defaults-catalog-native-v2`
and `chart-theme-defaults-package-native-v1`. Run
`scripts/verify-chart-theme-consumers.ps1` against a frozen host and its manifest
to reproduce the checks. The shared fixture builds in independent x64 and ARM64
consumers; native ARM64 execution is unverified. The library package and selected
delivery archives are unchanged. These targeted checks do not accept complete
chart visuals, tooltip effects, interaction, motion, scaling or chart families
beyond Scatter.

The frozen [Bleecker chart-theme source](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/chart-theme.ts)
has SHA-256 `94e006606feb77fc8163d05812e83668b04916dcdbb818a8aa8dc0d49e08e750`.
It exports `chartTheme`, `pickColor` and `ChartSeries`. It defines shared chart
configuration rather than a standalone rendered component. The Windows
inventory entry remains in progress; the public metadata API and palette
fixture do not constitute accepted chart rendering.

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
These are metadata/resource-boundary checks.

The immutable metadata is public as `BleeckerChartTheme.Colors`, `Palette`,
`Grid`, `Axis`, `Tooltip` and `PickColor(double)`. Grid/axis/tooltip values use
public readonly record structs. Resource keys are intended for real scoped
XAML `ThemeResource` expressions; they are not resolved brush snapshots.
The API returns null for the frozen source's undefined palette results.

`ChartThemesFixture` is registered after Attention Surfaces and the focused
`-Component chart-theme` mode uses ordinary catalog navigation. It renders all
12 roles and ten automatic colors, a second palette under a local sea-brush
override, and the native axis/grid/tooltip surface defaults. Runtime checks
switch the root appearance in place, retain the local brand scope, mutate the
owned brand brush and require exact composed screen pixels for every palette
swatch. Its tooltip surface specimen does not yet implement shadow or backdrop
blur and must not be treated as acceptance of those effects. Focused hosted
x64 and ARM64 checks run before the full catalog so earlier component failures
cannot hide this new fixture.

The series-color audit has verified the frozen SHA-256 for all twelve chart
modules under `artifacts/chart-theme-reference`. Line, Bar and Area consume
`ChartSeries.key`/`name` unchanged and select `s.color ?? palette[index % 10]`.
Radar and Scatter use their extended series models with the same color
precedence. Pie/Donut use each data item's explicit color; Sparkline uses its
explicit color or the sea role. The native series model preserves
explicit brush ownership and use a live scoped resource reference only when
that optional color is absent. An explicit value must not silently fall back
to a palette color.

The independent package consumer exercises the public style types, source
palette selection and real scoped XAML brush references across both root
appearances. Fresh native reports are required before claiming those checks
passed. Actual chart renderer coverage is tracked below and in each chart's
acceptance record; a palette specimen does not establish complete rendering,
tooltip effects, scaling or native ARM64 acceptance. Narrator and OS contrast
campaigns are excluded by the [current library scope](library-scope.md).

`BleeckerChartSeries` supplies the native series model. `Key` and `Name` are
consumer-owned strings and must be assigned before consumption; reading an
unset value fails explicitly. Empty strings are retained. The optional `Color`
is a native `Brush`: null selects the automatic scoped palette in a chart
consumer, while any explicit brush—including transparent paint—takes
precedence. The model retains brush identity and exposes dependency properties
plus standard `INotifyPropertyChanged` notifications for ordinary live native
bindings. The fresh independent consumer exposed that dependency properties
alone read the initial managed object but did not update the mounted name
binding. The regression requires the real native binding to adopt subsequent
names and color changes, including transparent and null values.
The model does not interpret data keys, trim names,
resolve a global palette, clone brushes, fetch data or add application actions.

The registered Chart Theme fixture now instantiates automatic, explicit and
transparent series in XAML. Its native swatches consume keys/names and select
automatic versus explicit paint exclusively. Runtime checks cover exact
strings, live key/name/color changes, missing required values, explicit empty
strings, brush mutation, transparent precedence, clearing color back to the
live palette and retaining a nonsolid brush. Owned-screen captures require
exact pixels for explicit, mutated, transparent and automatic states. The
independent NuGet consumer also exercises the public model through real
mounted native bindings. Those earlier results remain series-model/specimen
checks; the current Line/Bar/Area boundary checks below exercise real plots and
legends. Other chart-specific requirements remain with their inventory entries.

The focused x64 series fixture passes both themes at
`artifacts/smoke-chart-series-native`, with exact composed pixels for explicit,
mutated, transparent and automatic paint. The first fresh consumer's mounted
name-binding regression fails against the dependency-property-only candidate
at `artifacts/package-consumer-chart-series`. After adding standard property
notifications, a second fresh isolated consumer passes both themes at
`artifacts/package-consumer-chart-series-binding`. Its package 0.1.0 SHA-256 is
`c22fe9259f80cf649b55fb3a7f6e85680a75dd59735aca19e3aae85e8afc85bd`.
Both architecture builds have zero warnings, all 32 regressions pass and token
generation matches the pin. Full catalog, native ARM64 series, component theme
and automation behavior, and scaling acceptance remain required.

The complete local x64 catalog passes both themes with the notification-enabled
model and all series paint regressions at
`artifacts/smoke-chart-series-binding-all`. This run retains disabled system
animations and display/text scale 1. The first model candidate's failure is
preserved; it is not counted as package acceptance. Fresh hosted native ARM64
series evidence and every actual chart consumer remain open requirements.

Hosted run `38509625229` at `6f5e41d` passes the focused series fixture, full
catalog and independent native consumer on both x64 and ARM64. The consumer
requires real mounted name/color bindings to update, including transparent and
null color transitions; the native fixture requires the composed paint samples.
These close the initial native series-model/runtime proof on both architectures.
Actual chart renderers, complete visual/accessibility/scaling/performance
acceptance remain open. The unrelated historical contrast gate retains its
original run result but is excluded from the current library scope.

The focused local x64 run passes both appearances at
`artifacts/smoke-chart-theme-native`, with Windows animations off and display/
text scale 1. Its three capture phases (initial, brand mutation, restored)
require exact RGBA for all twenty mounted regular/scoped palette swatches in
each appearance, including the consumer brush mutation's actual composed
pixels. Both solution architectures build with zero warnings, all 32
unit/resource/coverage tests pass, and token generation matches the pin.
This is initial native theme-consumer evidence, not chart visual acceptance.

The fresh isolated x64 NuGet consumer passes at
`artifacts/package-consumer-chart-theme-native`, including the new public
metadata types and mounted regular/scoped palette references in both themes.
Package 0.1.0 has SHA-256
`8e5f1d430e81d22ec3ff666bb6c3e2c4c49314961d92f75487b907d375f711ca`.
The verifier restores into a new independent cache and uses no source project
reference. Native ARM64 and enabled-animation evidence require the hosted run;
the unrelated disabled-cursor failure remains a required gate. The historical
contrast failure retains its result but is excluded from current acceptance.

The complete local x64 catalog passes both appearances with the registered
chart fixture and expanded contrast baseline diagnostics at
`artifacts/smoke-chart-theme-all`. This retains the same local settings and
does not establish a passing hosted contrast or ARM64 cursor gate.

Hosted run [38506082985](https://github.com/gaulatti/orchard/actions/runs/38506082985)
at `c7fdad4` passes the focused chart-theme step on both x64 and native ARM64;
ARM64 retains enabled Windows animations. Both steps use the real catalog
fixture and architecture-checked reports. Both independent native package
consumers also pass. Overall CI remains red: both contrast checks retain the
46/124 Attention failure, and the full ARM64 catalog stops at Breadcrumb's
custom-template `ConsumerLink` lookup. The full x64 catalog passes. These
focused successes do not establish complete chart or platform acceptance.
