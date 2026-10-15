# Gaulatti.Orchard

Native Windows components and Bleecker theme resources for C# / WinUI 3.
The current 0.1.0 foundation provides typography, light/dark/contrast palettes,
button and icon-button components, connected button groups, toggle styles and
groups, loading spinners, cards, transient alerts, brand lockups, attention
surfaces and initial input styles. Full component parity is in progress.

Target .NET 10 and Windows SDK 10.0.26100.0. Use Windows App SDK 2.5.1.
Add the package from the directory containing the downloaded `.nupkg` or your
configured package feed:

```xml
<PackageReference Include="Gaulatti.Orchard" Version="0.1.0" />
```

Merge resources in `App.xaml` in this order:

```xml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <ResourceDictionary Source="ms-appx:///Orchard/Themes/Orchard.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Apply `BleeckerPrimaryButtonStyle`, `BleeckerSecondaryButtonStyle`,
`BleeckerHeadingStyle`, `BleeckerBodyStyle`, `BleeckerTextBoxStyle` or
`BleeckerComboBoxStyle`. Use `ThemeResource` for colors. The package copies fonts
and their SIL Open Font License texts to `Orchard/Assets/Fonts` in build and
publish output; no sibling source checkout or installed fonts are required.
Bleecker token attribution is included in `licenses/LICENSE-Bleecker.txt`.
D3 Shape attribution for native monotone curves is included in
`licenses/LICENSE-D3-Shape.txt`. The data-key parser's es-toolkit attribution
is included in `licenses/LICENSE-EsToolkit.txt`.
Recharts numeric domain and layout attribution is retained in
`licenses/LICENSE-Recharts.txt`.
`BleeckerLineChart` accepts consumer-owned observable records and series,
an X-axis key, optional labels/formatters, and grid/legend visibility.
Its category tooltip follows keyboard/pointer coordinates, preserves raw string
values, and owns a clipped native shadow and Gaussian backdrop effect.
Its full visual, motion and platform acceptance remains in progress.
`BleeckerSparkline` supplies a compact monotone line from observable Data and a
DataKey, with Height 40, optional Color and native inspection. ShowArea preserves
the pinned source's unfilled Line output. Complete parity remains in progress.
`BleeckerStatCard` composes Title, Value/ValueTemplate, an observable
`BleeckerStatTrend`, optional Icon/Footer content and consumer Sparkline history.
A Trend suppresses Description. Full visual/platform acceptance remains open.
Raw zero Icon/Footer content renders without chrome; Boolean content and
whitespace follow the pinned source. Custom templates retain their own data.
`BleeckerBarChart` consumes observable records and series in both source layouts,
with grouped rounded bars, optional grid/legend, formatters and native category
inspection. Set XAxisKey before mounting. Full raster, motion and platform
acceptance remains in progress.
The packaged brand SVG is copied to `Orchard/Assets/Images`. Brand filtering uses
the pinned transitive Win2D 1.4.0 dependency; keep its native runtime files with
the complete build/publish output.

Custom actions use the `using:Orchard` XAML namespace. `BleeckerButton` supports
variants, sizes, commands and loading. `BleeckerDashboardSection` groups consumer
`Content` beneath optional `Title`, `Description` and `Action` values; its header
stacks below a 640px window viewport and aligns its action to the bottom above
that breakpoint. Full visual/platform acceptance remains in progress.
`BleeckerDashboardGrid` accepts consumer children, Columns 1–6 or automatic (0),
and responsive `BleeckerDashboardSpan` wrappers (One–Four or Full). The 24px gap
uses the shared group token; window viewport breakpoints follow the source.
`BleeckerIconButton` supports three
variants and sizes and requires a consumer-supplied accessible name.
`BleeckerAlert` provides typed window-owned messages, duration and close callbacks;
`BleeckerAlertContainer.ShowAlert` publishes to containers in an explicit XAML root.
`BleeckerErrorState` provides consumer-owned title/description/icon content and
an optional native retry command, with parameter and CanExecute support.
Its complete visual and accessibility acceptance remains in progress.
`BleeckerPageHeader` supplies a consumer-owned title/description, breadcrumb
content/template and wrapping action content or observable collection. Its
640px breakpoint follows the window viewport. `BackHref` remains exact
application navigation metadata; `BackCommand`, parameter and `BackRequested`
deliver invocation without launching a URL. `BackLabel` supplies the accessible
name. Complete text raster, scaling, contrast and Narrator acceptance remains
in progress.
`BleeckerSectionHeader` supplies a level-two title, optional eyebrow and summary.
Its strings remain consumer-owned, optional empty content collapses, and it adds
no keyboard stop or action. Full text raster, contrast, scaling and Narrator
acceptance remains in progress.
`BleeckerBrandLockup` preserves consumer-owned routes and commands, supports both
sizes, optional names, custom image sources and typed consumer link templates.
`BleeckerBauhausBackground` provides four decorative image/pattern variants;
the consumer owns its image resources, including the default hero image.
`BleeckerButtonGroup` connects native or Orchard buttons. See the repository's
[component documentation](https://github.com/gaulatti/orchard/blob/main/docs/parity.md)
for examples and the acceptance status of each component.

The application owns data, commands, navigation and network access. Catalog and
test code are not part of the library. Keep the complete unpackaged output
directory together when distributing an application.

`BleeckerAreaChart` consumes observable records and series for overlapping or
stacked monotone areas, with optional numeric tick/tooltip formatters, a shared
palette and one native category inspection stop. See the Area Chart source
contract in the repository documentation for current acceptance limits.

`BleeckerPieChart` and `BleeckerDonutChart` consume original observable records
(or `BleeckerPieChartDatum` models), native brushes and one keyboard inspection
stop. Pie supports custom record keys and numeric/percentage inner radius; Donut
provides the source ring with independent center text. Full platform and visual
acceptance remains in progress; see the repository parity documentation.
