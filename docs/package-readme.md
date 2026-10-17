# Gaulatti.Orchard

Native Windows components and Bleecker theme resources for C# / WinUI 3.
The current 0.1.0 foundation provides typography, light/dark/contrast palettes,
button and icon-button components, connected button groups, toggle styles and
groups, loading spinners, cards, transient alerts, brand lockups, attention
surfaces and initial input styles. Full component parity is in progress.

`BleeckerTimeline` displays an observable `BleeckerTimelineItem` collection with
consumer-owned titles, descriptions, opaque timestamps, statuses and icons.
Null status follows ordinal position (first Active, later Pending). Null icons
use ordinals; original native elements and `IconTemplate` models retain their
own behavior. Headers stack below the 640-DIP window viewport and share a row
at or above it. Full visual/platform acceptance remains in progress.

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
The curved radial label's SVG path measurement includes Skia-derived math;
its BSD license is included in `licenses/LICENSE-Skia.txt`.
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
`BleeckerSidebar` retains consumer `ObservableCollection<BleeckerSidebarItem>`
records, keyed group expansion, header/footer slots and optional native link
templates. `ItemInvoked` and item commands hand opaque routes to the consumer.
Its CSS-style `Disabled` flag blocks link pointer input while preserving
keyboard activation; command availability and `IsEnabled` are native guards.
Complete visual and desktop acceptance remains in progress.
`BleeckerDataTableToolbar` connects controlled column search and Reset to an
application-owned `IBleeckerDataTableFilterSource`; the adapter retains its
original columns, rows, initial filters and global state. Original filter/action
content and optional templates follow the responsive 640px viewport layout.
The native preview's complete interaction, visual and platform acceptance is open.
`BleeckerDataTableColumnToggle` reads an application-owned
`IBleeckerDataTableColumnVisibilitySource`. Set `Table` before mounting and
raise `Changed` when original columns, headers, eligibility or visibility
change. `GetAllColumns()` must return the original top-level columns; the
picker offers only `HasAccessor && CanHide`, reads `IsVisible` and forwards
one `ToggleVisibility(bool)` action. String `Header` values are literal;
other headers use `Id`. The consumer owns all rows and visibility state.
`Label` defaults to `Columns`; the nonmodal popover uses native checkboxes.
Full visual, accessibility, scaling and platform acceptance is pending.

`BleeckerDataTablePagination` displays an application-owned
`IBleeckerDataTablePaginationSource`. Set `Table` before mounting and raise its
`Changed` event when page state, filtered counts or navigation flags change.
The view forwards first/previous/next/last and page-size requests to that same
adapter; the application retains rows, filtering, sorting, selection and paging
policy. `PageSizeOptions` defaults to 10, 20, 30, 50 and 100 and accepts a live
integer collection. Unknown manual page counts remain `-1`; an unlisted size
has an empty selector display. ArrowDown/ArrowUp/Enter/Space open the chooser
without changing the table; Tab stays inside the open list. A committed option
forwards its size, while Escape cancels. Complete visual, keyboard and platform acceptance
remains open.
The pagination chooser keeps its current value visible above a popup aligned
4px below the trigger and bounded to its 80px width. External table/options
updates preserve an open chooser; an unlisted current size displays empty
without selecting a replacement or changing the table. Full popup appearance,
collision edges, scroll buttons and enabled motion remain in progress.
`BleeckerIconButton` supports three
variants and sizes and requires a consumer-supplied accessible name.
`BleeckerAlert` provides typed window-owned messages, duration and close callbacks;
`BleeckerAlertContainer.ShowAlert` publishes to containers in an explicit XAML root.
`BleeckerErrorState` provides consumer-owned title/description/icon content and
an optional native retry command, with parameter and CanExecute support.
Its complete visual and accessibility acceptance remains in progress.
`BleeckerFilterChip` supplies a label/value and optional remove command/event.
`BleeckerFilterGroup.Filters` holds observable `BleeckerFilter` records and an
optional Clear all command/event. Each callback leaves collection changes to
the consumer. Live font style/stretch changes refresh label, value and Clear all
measurements. Native filter actions notify UIA clients when their resolved name
or effective enabled state changes. Exact raster/motion, contrast, scaling and Narrator acceptance
remain in progress; see the repository's `docs/filter-chip.md`.
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

`BleeckerScatterChart` consumes original root records and
`BleeckerScatterSeries` objects with optional per-series observable data.
The first series supplies the shared numeric X/Y/Z keys. It supports bubble
sizes, native inspection, axis labels and numeric formatters. Source/native
and platform acceptance are tracked in the repository's Scatter contract.

`BleeckerRadarChart` consumes original observable records and
`BleeckerRadarSeries` objects. Supply SubjectKey and each series Key/Name;
ShowLegend defaults to true and Height to 350. Series Fill enables 0.35 opacity,
Color preserves consumer brushes, and TickFormatter formats numeric radius
ticks and inspection values. Native inspection uses one named keyboard stop.
See the repository's Radar contract for visual and platform acceptance gaps.

`BleeckerRadialBarChart` consumes original observable name/value/fill records,
including `BleeckerRadialBarChartDatum` with live property notifications.
Height defaults to 350, ShowLegend to true, InnerRadius/OuterRadius to
"10%"/"80%" and StartAngle/EndAngle to 90/-270. Radii accept native numbers
or source percentage strings. Consumer Fill brushes retain their identity,
including transparent paint. Curved values and the sorted legend use the
bundled typography; native inspection has one consumer-named keyboard stop.
See the repository's Radial Bar contract for remaining acceptance gaps.

`BleeckerFunnelChart` consumes original observable name/value/fill records,
including `BleeckerFunnelChartDatum` with live property notifications. Height
defaults to 350 and ShowLabels to true. ValueFormatter formats numeric
inspection values. The pinned source's inside labels contain the name followed
by a colon; they omit the value. Native focus inspects the first record, arrow
keys retain that selection, Enter toggles inspection and pointer hover inspects
the actual trapezoid. Transparent Fill brushes retain their identity. Set the
wrapper's automation name for its single named inspection stop. See the
repository's Funnel contract for visual and platform acceptance gaps.

`BleeckerSankeyChart` consumes a `BleeckerSankeyChartData` object containing the
original observable Nodes and Links collections. `BleeckerSankeyNode` supplies
Name/Fill and `BleeckerSankeyLink` supplies Source/Target indexes and Value, all
with live notifications. Indexes address the original Nodes order. Defaults:
Height 400, NodeWidth 12, LinkCurvature 0.5, LinkOpacity 0.35 and NodeRadius 4.
Optional NodeColors holds original native brushes; per-node Fill takes
precedence and remains shared by its outgoing bands. The frozen source's bands
use palette index zero unless an original dictionary node supplies `index`.
Empty palettes paint black, as measured in SVG. Set the wrapper automation name
for one named group stop. Native Left/Right traverses nodes then flows; Enter
toggles inspection and Tab leaves normally. This Windows keyboard convention
extends a source chart that has no keyboard stop. Source geometry quirks and
remaining visual/platform gaps are documented in the Sankey contract.

`BleeckerDataTableSelectFilter` consumes the original
`IBleeckerDataTableFilterSource` through required `Table`, `ColumnId` and
`Options`. `BleeckerSelectOption` has observable `Value`, `Label` and
`IsDisabled` properties and supports XAML construction. `Placeholder` defaults
to `All`. Selecting All passes null to the original column; it preserves
unrelated filters and never resets the table. Missing columns hide the
selector. A wide viewport uses 176×40 geometry; below 640 the selector fills
its host. The child ComboBox exposes its placeholder name and controlled
value, modal list/scroll roles, Arrow/Home/End/typeahead navigation and Escape
focus restoration. Native Windows focus advances when a focused column is
removed. Full rendered and platform acceptance remains in progress; see the
repository's Data Table Filters contract.

Use an observable Options collection for live add/remove/reorder notifications.
Rows retain native identity for unchanged raw Value keys and expose current
original option metadata. Label/IsDisabled changes update in place; changing
Value changes the row key. Removed or changed keys invalidate old providers.
Open controlled updates preserve logical focus and the existing viewport;
removing or disabling the focused option transfers focus to the listbox.

The Select popup uses the shared Bleecker overlay surface. Its two native
Gaussian shadow layers paint outside the existing measured border without
adding an input region. The library's pinned Win2D dependency supplies the
effect graph; consumers need no separate renderer. Closing or unloading the
surface detaches and releases its owned Composition resources. Exact source
raster and full platform acceptance remain in progress.

`BleeckerSunburstChart.Data` is a required original recursive record implementing
`IReadOnlyDictionary<string, object?>`. `BleeckerSunburstNode` provides Name,
optional Value/Fill and an original observable Children collection. Shared
records remain shared at every tree path; record notifications and collection
changes update sectors, outlined values and accessible hierarchy text. Defaults
are Height 350, DataKey `value` and NameKey `name`; keys are literal properties,
including empty or dotted keys. Root totals follow the frozen source: infer a
positive leaf sum only when the root lacks a numeric selected value, preserve
explicit root totals, and never infer branch values. Palette indexes restart
among each node's siblings; explicit native Fill brushes retain identity.
Set the chart's automation name for its noninteractive Image role. Tab skips it;
hover/click adds no visible inspection. Help text supplies ancestor paths and
values. Cycles and malformed child collections fail explicitly. See the
Sunburst contract for source quirks and outstanding visual/platform acceptance.
