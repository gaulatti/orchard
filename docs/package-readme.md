# Gaulatti.Orchard

Native Windows components and Bleecker theme resources for C# / WinUI 3.
For the 2026-10-29 milestone, acceptance is x64-only: Light/Dark states,
keyboard/pointer/focus, 150% scaling without clipping, reduced motion, and
package-consumer compilation. The release lists accepted components. ARM64 is
build-only and runtime-unverified; raster/pixel parity is not a gate. Older
component notes below describe historical checks and do not add release gates.

Accepted x64 components in this preview: Brand Mark, Card, Context Menu,
Dashboard Grid, Dashboard Section, Detail Layout, Empty, Error State, Feed Grid,
Icon Badge, Kbd, Label, Loading Spinner, Metric, Page Frame, Page Header,
Progress, Progress Stack, Section Header, Separator, Sparkline and Status Badge.
The fixed inventory has 112 entries started; 22 are accepted.

`BleeckerSkeleton` accepts caller-owned `Content`, `ContentTemplate` and
`ContentTemplateSelector`. Content and opacity bindings survive template changes
and remounts; child controls keep their own actions and accessible peers.
`Width`, `Height`, `Padding` and content alignment remain caller-owned. The
container stays decorative and follows native WinUI opacity composition.
Normal pulse timing and final visual/platform acceptance remain open.
Orchard's scope is the reusable component library and its fictional monitoring
and editorial demos. Narrator and Windows/OS contrast test campaigns are excluded;
existing component accessibility metadata and theme support remain in scope.
`BleeckerMediaLibrary` presents original `BleeckerMediaLibraryItem` objects,
controlled search, caller action/filter slots and a retained-item preview.
Set `SearchTerm` in `SearchTermChangeRequested` to accept an edit; the caller
supplies filtered `Items`. `DeleteRequested` receives the original ID, with
pointer Delete above the tile's Open target. The component never removes items.
`CopyRequested` receives the original filename or URL; otherwise `ClipboardWriter`
or the Windows clipboard handles the same string. `UrlCommand` optionally routes
the original URL, while the default native hyperlink uses an absolute URI.
Caller-owned `ThumbnailSource` and `ImageSource` provide native decoding; a failed
thumbnail never switches to the full image. Raw `Url`, `Alt` and `CreatedAt` stay
caller-owned. Use the window's `BleeckerModalHost`. Removing or replacing a row
does not replace its selected original; unloading clears preview selection.
Both light/dark catalog demos use fictional local data. This is a preview;
full visual review, physical input, complete scaling/motion/customization
and native ARM64 runtime acceptance remain open.

`BleeckerFileInput` provides native file selection with original `StorageFile`
objects, three sizes, error/disabled/required states and localizable labels.
Set `IsMultiple` and add explicit extensions such as `.png` to `FileTypeFilter`;
an empty collection permits all types. MIME filters are rejected without an
invented mapping. The built-in action associates the WinRT picker with its owner
window. `SelectionChanged` receives the same original read-only file list;
`SelectionCanceled` retains previous files. `SetFiles` and `ClearSelection`
update values silently. Await `PickFilesAsync` from a host action, or handle
`PickerFailed` when the built-in button needs host error presentation. Optional
`Picker` integration receives the configured native picker. `FontScale` scales
the source spacing/font recipe; caller font, padding, brushes and templates
remain available. `BleeckerFileInputStyle` is the explicit default style.
The preview's hidden geometry/brush/selection/lifecycle checks pass; strict
browser/native image equality, physical/system-picker interaction, complete
motion and native ARM64 runtime acceptance remain open. No file is uploaded or
read by the component.

`BleeckerTable.Sections` contains original table headers, bodies, footers and a
caption. Each section contains `BleeckerTableRow` controls with native
`BleeckerTableCell`/`BleeckerTableHead` content, templates and selectors. Shared
intrinsic columns support column/row spans, constrained horizontal scrolling
and RTL. `RowSpan=0` extends to the end of its section; `ColumnWidth` accepts
Auto or a preferred pixel width. Original actions, content and bindings remain
caller-owned. `BleeckerSortableTableHeader` requests caller-owned sorting.
`BleeckerSimpleDataTable` is the basic source DataTable composition, separate
from the TanStack-backed `BleeckerDataTable` draft. Supply `ItemsSource`, immutable
`BleeckerTableColumn` descriptions and `GetRowKey`; optional native cell/footer
producers receive original sorted rows and sorted indices. Nullable `Sort` and
`SelectedKeys` enable controlled behavior; requests always update internal
state, so releasing control exposes the last request. Collection/item
notifications refresh mounted rows; use `Refresh()` for non-observable data.
Both table compositions observe each distinct original object once, even when
separate objects compare equal. Source replacement and unload release those
listeners. Data Table reload rereads collection edits made while unloaded.
Headings use full Unicode uppercase mappings and the control's language for
contextual casing, retaining original labels and accessible names. String row
keys and default dictionary property names use ordinal matching, independently
of a caller collection's comparer. Caller collections and bindings are retained.
The preview includes Table/Grid metadata and theme resources. Physical input,
full raster/motion/scaling/shadow, collection performance and platform
acceptance remain open.

`BleeckerDataTable` owns client filtering, sorting, visibility, index selection
and ten-row pagination. Supply `ItemsSource`, `BleeckerDataTableColumn`
definitions in `Columns`, and an optional `SearchKey` column ID. Header and cell
producers receive original table/column/row contexts; `cell.Row.Original` is the
caller object. Consumers supply sort buttons, selection checkboxes and cell
actions themselves. `Model` also implements the existing pagination, filter
and column-visibility companion interfaces. Mutate a mounted model on its UI
dispatcher. Structural filter/page/column changes invalidate the shared native
table so restored original cell content reaches layout. Background validation
covers all 42 frozen static cases and 166 interaction snapshots, including
grouped headers, filter expansion and horizontal column geometry. Header
checkboxes follow the source block wrapper's logical start in RTL; explicit
caller header alignment styles keep precedence. Button tracking defaults to
zero independently of container tracking, matching the source button recipe;
caller local values and bindings keep normal WinUI precedence. Full raster,
scaling/motion, physical input, performance and ARM64 runtime acceptance remain
open. Dark surfaces use the user-selected Orchard tokens. TanStack Table algorithm attribution is included in
`licenses/LICENSE-TanStackTable.txt`.

`BleeckerNavigationMenu` accepts original direct links and descriptive groups,
with caller-owned raw destinations, external intent and routing. Default
grouped activation closes before `Command` / `ItemInvoked`; direct links leave
an open group unchanged. `LinkTemplate` receives the original item, prepared
content/style, guarded command and optional grouped `CloseCommand` (null for
direct links). Observable collections and dependency-property edits update
keyed live content. XAML uses `BleeckerNavigationMenuEntries` and
`BleeckerNavigationMenuLinks` collection wrappers. `TopLinkStyle`,
`GroupLinkStyle`, `TriggerStyle` and `SurfaceStyle` reach the native consumers.
The host owns navigation; native links do not set `NavigateUri`.

`BleeckerMenubar` accepts original `BleeckerMenubarMenu` descriptions with
action, controlled checkbox/radio, separator and recursive submenu entries.
Callbacks request caller changes once before dismissal; the control never
mutates their checked values. Shortcut strings only display hints. Observable
collections update automatically; call `Refresh()` for ordinary collection
edits. Trigger/item/root/submenu styles remain replaceable. The preview joins
an owning `BleeckerModalHost` or uses a root-constrained native popup, and retires
presentations on unload or template replacement. Normal motion, full keyboard/
focus, peer/group metadata, complete raster/scaling and ARM64 runtime acceptance
remain open.

`BleeckerContextMenu` is a native MenuFlyout assigned to a target's
`ContextFlyout`. Apply `BleeckerContextMenuItemStyle` or the inset/composed
variants from `Orchard.xaml` to original native items; caller commands,
parameters and Click handlers remain native. `Closing` cancellation can retain
an invoked menu. An active `BleeckerModalHost` owns the native presenter:
an upper modal temporarily hides it, then restores the same menu and items.
Original items may receive focus while their owner is active; Escape closes the
menu before the parent modal handles it. Owner teardown retires the popup even
after canceled closing. Live content, theme and presenter-style changes retain
native layout. Local x64 catalog checks pass physical keyboard opening,
Escape, right-click, original-item pointer selection, and owned Enter
activation. The `0.1.0-preview.20261029.5` x64 package consumer compiles
with zero warnings, and a physical owned-modal item click on the matching DLL
invokes the original handler. Light/Dark native 150% checks keep the item
within its presenter, and open/close animation stays disabled. Context Menu
passes all five x64 milestone checks; ARM64 runtime is unverified.

`IsModal` defaults to `true`. Bind it to `false` to retain outside input while
the original context menu surface is open, including outside a modal host.
Live changes retain original items and bindings. An unowned nonmodal menu uses
the native presenter and WinUI outside-input pass-through; its first outside
click activates the caller control and dismisses the menu on x64. Menus under
an active modal owner still obey that owner's scope. Normal closing cancellation
stays supported. Target removal retires an orphaned popup even after cancellation.

`BleeckerOtpInput` displays caller-owned `Value` in `Length` native slots (six by
default). Handle `ValueRequested` and assign the accepted value explicitly;
rejected or unhandled edits restore the caller value. Edits accept ASCII digits,
deletion compacts characters, and paste replaces the entire value. `IsSecure`
uses protected native PasswordBox editors. The component adds no authentication
or code-delivery service. Physical input, clipboard, scaling, full visual/motion
and ARM64 runtime acceptance remain open.

`BleeckerAppTabBar` binds `SelectedValue` to immutable `BleeckerAppTab` destinations
in an observable `Tabs` collection. Supply a native `FontIconSource`, label,
optional signed badge count and disabled state. Equal-width destinations preserve
typed values; badges cap visually at `99+` while retaining the raw accessible
count. The standalone compact component adds no app shell or navigation service.
Full physical-input, visual/scaling and ARM64 runtime acceptance remain open.

`BleeckerSegmentedControl` binds one `SelectedValue` to immutable, value-keyed
`BleeckerSegmentOption` entries in an observable `Options` collection. Values
retain their types, including enums and records; reselecting keeps the value.
It uses native activation and single-selection metadata, source typography and
theme tokens, and wrapped labels under constrained width. Full physical-input,
visual/scaling/customization and ARM64 runtime acceptance remain open.

`BleeckerSecureField` composes a native PasswordBox and persistent reveal button.
Bind `Password` two ways with `UpdateSourceTrigger=PropertyChanged`; use
`PlaceholderText` and an accessible name or `BleeckerField` label. It starts
concealed and preserves the value when revealed. It adds no authentication or
credential storage. `IsRevealed` exposes the persistent reveal state. Field
descriptions and invalid state belong to its composite Group peer; the inner
native editor retains the protected password/value peer. It adds no network
credential service. Native physical-input, complete visual and platform
acceptance remain open; see the repository's Secure Field documentation.

The current 0.1.0 foundation provides typography, light/dark/contrast palettes,
button and icon-button components, connected button groups, toggle styles and
groups, loading spinners, cards, transient alerts, brand lockups, attention
surfaces and initial input styles. Full component parity is in progress.

`BleeckerScrollArea` retains original `Content` and template/selector models in
a naturally sized viewport. `Orientation` controls user axes while hidden axes
keep their programmatic range. Inherited `MaxHeight` constrains height; source
scrollbars reserve 6/15 DIPs and support both themes and RTL. `ScrollTo` uses
native positive leading-edge offsets; actual offsets, viewport/extent values
and `ViewChanged` remain public. It adds no data or virtualization policy.
This local prototype's full visual, input, scaling, performance and platform
acceptance remains open.

`BleeckerHoverCard` retains original trigger and content controls or models in
an inline surface. Both slots support templates/selectors. `Side` is Top or
Bottom; `SurfaceStyle` customizes the private surface's default 288-DIP width,
and inherited paint/typography properties remain public. Pointer hover drives
source opacity motion without taking focus or intercepting original actions.
The source adds no click toggle, focus loop, collision handling or portal and
leaves closed original content in the tab order. The native sibling layer
defaults to 50; caller `Canvas.ZIndex` values take precedence within native
ancestor stacking scopes. Full visual, physical-input, nested-host, scaling,
performance and ARM64 runtime acceptance remain open.

`BleeckerHeader` composes an original `BleeckerHeaderBrand`, navigation items and
independent desktop/mobile action content. It switches at the viewport's desktop
breakpoint, retains menu/caller state across resizing, and supports Fixed,
Sticky and Static positioning with the shared header-height token. Handle
`ItemInvoked`/`BrandInvoked` for host routing. A shared link template receives
`BleeckerNavLinkContext`; slot templates/selectors retain original caller data.
Header is an unaccepted prototype; full runtime/visual acceptance is in progress.
Header retains original background and border brushes. Solid paint combines
color alpha and brush opacity into its private byte-alpha brush; live color,
opacity and brush replacements update that rendering. Non-solid paint remains
the original brush.

`BleeckerPopover` hosts an original trigger and caller content in a nonmodal
anchored native popup. `Open` is nullable: null uses separate internal history
initialized from `DefaultOpen`; true/false is authoritative caller state.
Handle `OpenRequested` and update `Open` to accept controlled transitions;
`OpenChanging` can cancel a request. Both slots support original native controls,
models and templates/selectors. `Side`, `Align` and `SideOffset` control source
placement, including RTL, collision shifting/flipping and negative offsets.
`SurfaceStyle` customizes the private surface; inherited paint and typography
properties remain public. Popover is an unaccepted prototype. Full visual,
input/focus, nested modal, scaling and ARM64 runtime acceptance remain open.

`BleeckerBrandLockup.IsLogoLoaded` and `LogoOpened` expose the current native
logo decoder's readiness, independently of element loading; `LogoFailed`
continues to report native decode failures.

`BleeckerFooter` composes an original `BleeckerFooterBrand`, observable
`BleeckerFooterSection` entries and caller-owned bottom content/templates.
Viewport breakpoints choose one, two or four columns; `ShowBottomAccent`
controls the final rule. Handle `ItemInvoked` for original section items and
unchanged destinations. A global `LinkTemplate` receives `BleeckerNavLinkContext`
for brand, section and default source links. An omitted/null `BottomRight`
uses the first section item whose raw href contains `github.com`; empty text
and `false` suppress this default, while zero renders as `0`. The host owns
routing. Hidden layout, paint and worker-thread/lifetime checks pass; visible
input/full raster, expanded customization, motion/scaling and native ARM64
acceptance remain in progress.

`BleeckerNavMenu` presents caller-owned `BleeckerNavItem` links horizontally or
vertically. Handle `ItemInvoked` to route the original item and its unchanged
`Href`/`External` metadata. `LinkStyle` and `LinkTemplate` support host rendering;
custom templates receive `BleeckerNavLinkContext` with the original item,
prepared content, style, live foreground and guarded command. An optional menu
command receives its explicit `CommandParameter`. Observable collection/item
changes preserve stable keyed link content. Native x64 input, geometry, paint
and lifetime checks pass; full raster, motion, scaling and native ARM64
acceptance remain pending.

`BleeckerSelect` presents original `BleeckerSelectOption` entries in three sizes.
Handle `ValueRequested` and update the bound `Value` to accept controlled changes.
An empty value without an empty option retains the source's internal selection
history while leaving the original prop and binding unchanged. `BleeckerHeaderSelect`
adds a placeholder option when needed and uses a 144–208 DIP compact trigger.
Both retain original options, optional icon models/templates and caller metadata.
The catalog demonstrates accepted and declined requests with fictional local data.
Popup/input, exact raster, scaling and native ARM64 acceptance remain pending.

`BleeckerAppShell` composes original `Header`, `Content` and `Footer` objects,
templates and selectors. It uses the native window viewport as a default minimum
height while preserving caller minimum bindings and styles. `OffsetHeader`
defaults to true; a truthy header adds the shared 72-DIP `HeaderHeight` token to
main padding independently of actual header height. Set it false for a normal
in-flow header as demonstrated in the catalog. Explicit `ContentPadding`
overrides that recipe; `ContentBackground` styles the Main landmark separately.
Header and footer wrappers follow the original native or template root's live
`Canvas.ZIndex`, so Header and its mobile menu can overlap main content. Replaced
or unloaded roots stop publishing stacking changes; empty slots reset to zero.
The caller owns navigation, scrolling, values, commands and all application data.
Complete visual, input, scaling and platform acceptance remain pending.

`BleeckerAdminShell` adds an original `Sidebar` beside a native scrolling Main
region containing body and footer. Header/sidebar controls remain outside that
scroller. Body spacing follows viewport breakpoints at 640 and 1024 DIPs;
`ContentPadding` supplies an explicit override. All four slots support original
models, templates and selectors. `Viewport` exposes the current ScrollViewer.
Both shell controls keep their viewport minimum beneath local caller values,
so clearing zero reveals the recipe and equal-valued overrides retain ownership.
Their visual, input, scaling and platform acceptance remains in progress.

`BleeckerThemeToggle` composes the three native icon-button sizes/variants with
18 DIP Sun, Moon and Monitor vectors. `Mode` defaults to `System`. Handle
`ThemeRequested` and update the bound mode to accept Light → Dark → System
requests. The caller owns its root theme and persistence. The default accessible
name and tooltip describe the next mode; caller metadata takes precedence.
Local values/bindings retain empty or null tooltips. `Style`/`BasedOn` setters
retain empty names and absent tooltips under WinUI's setter rules.
Clearing a local override reveals the style; removing a style override restores
the mode's default name/tooltip. Mode updates preserve consumer content and
bindings. Hidden native diagnostics exercise these boundaries without showing
or activating a window, focusing a control or sending input.
Native interaction, raster, lifecycle and platform verification are pending.

`BleeckerTabs` provides `Underline`, `Segmented` and `Enclosed` variants in three
sizes, optional stretch layout and observable immutable `BleeckerTabItem` entries.
Handle `ActiveTabRequested` and update `ActiveTab` to accept selection. Host-owned
destinations retain their original metadata through `NavigationRequested` and
optional commands. `LinkTemplate` accepts a typed `BleeckerTabLinkContext` for
consumer routing/rendering. The library never launches a destination itself.
The native style includes the source's outer shadows and 190ms paint timing;
reduced motion snaps to the requested state. Pressing shifts the surface down
one pixel immediately. Exact raster and full platform acceptance remain open.

`BleeckerDialogCloseButton` is a 36 DIP native button with an adjustable Lucide X,
source hover/press styling and native commands. Set `AutomationProperties.Name`
to a localized action name. The caller owns dismissal and can decline a request;
the control never closes an ancestor window or overlay itself.

`BleeckerField` composes a label, optional action, description/error and one
consumer-owned `Control`. Set `Label`, `Required`, `Optional`, `Action`,
`Description` and `Error`; use the corresponding slot templates for models.
The child retains its value, commands, enabled state and visual validation.
The required star is decorative. Error supplies temporary invalid semantics;
ordered native label/description relationships and consumer bindings are
restored when messages, children or templates change. Exact raster, broader
customization/scaling and native ARM64 acceptance remain in progress.

`BleeckerPageFrame` supplies `Reading`, `Content`, `Wide` and `Full` width presets
and four `Gutter`/`VerticalSpacing` recipes. Spacing follows the window viewport;
the centered frame follows its parent width. Caller padding/maximum bindings and
style setters take precedence over recipes. Content, templates, values and events
remain owned by the consumer. `Element` supplies passive native grouping, a main
landmark or a named section region. Full visual/platform acceptance is in progress.

`BleeckerDetailLayout` composes main `Content` and optional `SideRail` with
`Primary`/`Balanced` ratios, `Start`/`End` placement and `Sticky` behavior. Columns
start at a 1024 DIP window viewport. Sticky positioning follows the nearest
native scroll viewport and stops at the layout bottom. Main remains first in
Tab order; `SideRailLabel` names the complementary group. Both slots support
consumer elements, templates and selectors. Full platform acceptance is in progress.
Explicit templates receive original model values, including numbers and Booleans.

`BleeckerSlider` is a controlled draft with `Min`, `Max`, `Step`, `Value`,
`Label` and `ShowValue`. `ValueRequested` leaves the caller's original value
and binding in place until accepted. Its thumb/native range use a step-aligned
value while the label retains the original number. Native desktop interaction,
raster and lifecycle checks remain unverified for this draft.
The visible label supplies a default native name; caller names from local values,
bindings and `Style`/`BasedOn` setters take precedence, including an empty name.
Owned plain-text slots measure a single combined inline advance across fallback
font/script runs, preserving the source's multilingual width and wrapping.

`BleeckerFeedGrid` auto-fills tracks with a default minimum width of 340 DIP,
12 DIP gaps and 16 DIP padding. Empty tracks remain reserved, row heights follow
the tallest child, and original native children retain their state and commands.
`MinColumnWidth`, `Gap` and `Padding` are bindable. Child alignment stays native;
set Stretch when a child should fill its track. Feed Grid passes the five x64
milestone checks; ARM64 runtime is unverified.

`BleeckerFeedColumn` supplies a fixed title/count/filter header and independently
scrolling body. `Title`, nullable numeric `Count`, solid `Accent`, `Filter` and
native content/filter templates retain consumer ownership. `Viewport` exposes
the current ScrollViewer after template application. Width and MinWidth default
to 372 DIP; set both for another recipe width. Body Padding defaults to 12 DIP.
Header updates retain controls and scroll state. Full acceptance remains open.
The default Feed Column keeps its dark gradient and requests dark theme tokens
for its content subtree, including composed Feed Items, in a Light host. Set
`RequestedTheme` explicitly when replacing that default surface recipe.

`BleeckerFeedItem` composes retained avatar/timestamp metadata, relevance,
wrapped content and language/category tags. Bind original `Categories` and
`SourceUrl`; formatting leaves their data intact. `LanguageCode` controls the
display tag. `SourceCommand`/`SourceRequested` route original source navigation
through the consumer. `PostedAt` accepts DateTimeOffset or timestamp strings;
legacy/extended date and broader locale acceptance remains open.
The source link stays on the readable sea accent when hovered on a dark feed
surface; its underline supplies the hover cue.

`BleeckerPanelLayout` lays out fixed-width children and equally growing children
in one horizontally scrolling row. `BleeckerPanelColumn` stacks fixed-height
children and shares remaining height. Both expose native `Children` collections
and preserve caller input, bindings and commands. Use `BleeckerPanel.Grow`,
`BleeckerPanelColumn.Grow`, or attached `BleeckerPanelLayout.Grow` for other
native elements. Padding defaults to zero. These previews retain open raster,
scaling, ARM64 runtime and final integration acceptance.

`BleeckerPanel` provides a title, nullable count badge, optional `Filter`,
`Toolbar` and `DragHandle` slots, and independently scrolling `Content`. Each
slot supports templates/selectors and original consumer controls. Header
updates preserve unchanged slots and scrolling. A plain overflow viewport is
keyboard-focusable; editable children retain their own focus path. `Accent`
accepts a solid brush, including opacity. Full pixel/platform acceptance remains
in progress.

`BleeckerIconBadge` is a passive content surface with `Medium`/`Large` sizes
(56/64px), `Primary`/`Subtle`/`Outlined` variants and a 10px corner radius.
It retains consumer UIElements, templates, explicit colors, styles and sizes.
It adds no Tab stop or Invoke behavior; use its children for commands. Set an
automation name for a named Windows group. Full visual/platform acceptance
remains in progress.

`BleeckerEmpty` supplies nullable title/description strings, a default or
consumer-owned icon, and optional consumer action content. Icons/actions accept
UIElements, scalar content, templates and selectors. The parent adds no command
or Tab stop; its title exposes heading level 3. Explicit minimum values and
styles keep ownership when padding changes. Full visual/platform acceptance
remains in progress.

`BleeckerAccordion` displays an observable `BleeckerAccordionItem` collection with
one internally expanded ID. `DefaultExpandedId` initializes state once; changing
it after mount does not change expansion. Title, subtitle, content and independent
action slots accept UI elements or data templates and retain consumer bindings,
commands and input state across collapse. Header buttons expose Invoke and
ExpandCollapse automation and ordinary Tab/Enter/Space navigation. Full
visual/platform acceptance remains in progress.

`BleeckerRadioGroup` exposes observable immutable `BleeckerRadioOption` entries,
nullable string `Value` and horizontal/vertical `Orientation`. Handle
`ValueRequested` and update your bound model to accept selection. Declining a
request retains selection. Value-keyed option controls survive moves and
same-value metadata replacement. The group supports Windows keyboard navigation
and Selection/SelectionItem automation. Full visual/platform acceptance remains
in progress.

`BleeckerCalendar` and `BleeckerDatePicker` expose controlled `Value`, `MinDate`,
`MaxDate` and `DisabledDates`. Handle `DateRequested` and update the consumer
value to accept selection or clearing. `BleeckerDateRangePicker` exposes a
`BleeckerDateRange` with local `From`/`To` strings, `Presets` and `RangeRequested`.
Handle its request to accept a new record. The picker popup supports native
keyboard input, Escape, outside dismissal and trigger focus restoration.
Date styles align branded text to the source line boxes and use tabular calendar
numerals. For an authored `TextBlock` under a `Panel` or `Border`, opt in with
`BleeckerTextLayout.UseReferenceLineBox="True"` and an explicit positive
`LineHeight`. Disabling the property restores the original element alignment
and render transform. This behavior is intended for single-line branded text.
Complete source visual and Windows accessibility/platform acceptance remains open.

`BleeckerTextarea` previews native multiline plain-text editing with `Text`,
`InputSize`, `Rows`, `IsReadOnly`, `IsError` and nullable `IsInvalid`. It retains
the bundled font asset, native selection/undo and a vertically resizable layout.
Use `Text="{Binding Notes, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"`
for a consumer model that updates on each edit. Pointer resize, remaining form
constraints, full visual/platform and accessibility acceptance are in progress.

`BleeckerMetric` displays nullable consumer values using Number, Currency,
Percent or Compact formats. `Language`, `Currency` and nullable `Decimals`
control culture and precision. Original prefix/suffix elements and templates
remain consumer-owned; the metric is non-tabbable accessible text. Null/NaN
leave its numeric text empty. Source-format regression covers 680 browser
cases. Metric passes the five x64 milestone checks; ARM64 runtime is unverified.

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
the pinned source's unfilled Line output. Sparkline passes the five x64
milestone checks; ARM64 runtime is unverified.
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
or effective enabled state changes. Exact raster/motion, scaling and keyboard/focus acceptance
remain in progress; see the repository's `docs/filter-chip.md`.
`BleeckerPageHeader` supplies a consumer-owned title/description, breadcrumb
content/template and wrapping action content or observable collection. Its
640px breakpoint follows the window viewport. `BackHref` remains exact
application navigation metadata; `BackCommand`, parameter and `BackRequested`
deliver invocation without launching a URL. `BackLabel` supplies the accessible
name. Complete text raster, scaling and keyboard/focus acceptance remains
in progress.
`BleeckerSectionHeader` supplies a level-two title, optional eyebrow and summary.
Its strings remain consumer-owned, optional empty content collapses, and it adds
no keyboard stop or action. Full text raster and scaling acceptance remains in progress.
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

Select, Collection Filters and the DataTable pagination, column toggle and
select filter views marshal caller-owned producer notifications to their UI
dispatcher before reading XAML state. This applies to model metadata,
observable collection events and original table `Changed` events raised on
worker threads. Serialize producer mutations; concurrent collection mutation
and enumeration is not supported. Set dependency properties on the UI thread.
Unloading releases observers and replacement tables retain their own state.

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

The shared `BleeckerOverlaySurface` casts an opaque rounded border shape even
when `Background` is null or transparent. Its outer shadows exclude the
rounded interior. Solid/gradient brush identity, opacity and background
bindings remain caller-owned; changing them does not rebuild the shadow graph.
Its tint preserves the source's premultiplied eight-bit paint through a floating
color input. Nominal RGB/opacity tokens stay unchanged; alpha is carried by the
tint, and the output sprite uses opacity one. This retains the existing GPU
blur/geometry path without CPU bitmap generation or readback.

Collapsing the surface or a XAML ancestor releases its owned shadow graph;
restoring visibility recreates it while retaining caller paint and bindings.
Zero dimensions, missing template parts and unloading also release the graph.
The surface removes visibility subscriptions on unload and follows the new
ancestry when reloaded after reparenting. Hidden windows can render offscreen;
this behavior follows XAML component visibility without changing OS settings.

The local near-renderer preview also contains an internal renderer with embedded
GPU shaders, automatic geometry/position refresh and per-compositor shared
kernel ownership. Kernels are leased by active radii; suspended and zero-sized
owners release their generated resources and restore them when drawable again.
It remains under validation and is not the default overlay
renderer. Its current primitive accepts cacheable rectangles with uniform
positive finite corners at scale one, including fractional radii. The canonical
kernel rounds its radius upward; final sampling retains the original radius
and offset. Fractional overlap quantization still has an open source-image gate.
Small, unequal-corner and transformed geometry fails explicitly inside
that primitive. Existing public overlay APIs keep the renderer described above.
Hidden fixture PNGs use a pinned, injective browser encoder mapping for diagnostic
serialization. The library renderer receives neither that mapping nor readback pixels.
Device recovery, broader geometry/scaling and the combined source image gate
remain acceptance work. No public opt-in switch is added.

The local shadow preview also includes an internal far-layer investigation with
GPU-generated mask data and shared resource ownership. Four of eight pinned
images are exact; eighteen one-level physical pixel differences keep its source
image gate open. Its public default remains the renderer described above.
Combined attachment, broader geometry, device recovery and measured memory and
collection costs remain required before adoption. Diagnostic screenshot export
data is confined to the catalog and test consumers.

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


`BleeckerSearchInput` composes Orchard's native editor with an optional clear
action. Bind `Text` in TwoWay mode. `ClearRequested` / `ClearCommand` keep
text changes consumer-owned; `ClearLabel` localizes the action. `Editor`
exposes native selection, clipboard and text events. `IsEditorEnabled=false`
disables only editing, preserving the sibling clear button; `IsEnabled=false`
disables both. Escape edits the native value without invoking clear. Sizes,
themes, RTL geometry, editing/binding and external UIA are exercised in the
catalog. Exact raster, complete state/motion and platform acceptance remain in
progress; see `docs/search-input.md` in the repository.

Input palette brushes retain exact CSS alpha through `Brush.Opacity` alongside
opaque RGB colors. Combine `Color.A / 255` and `Opacity` when inspecting a
brush. Background/border paint transitions use the existing premium CSS clock
and premultiplied OKLab interpolation; disabled editor opacity remains separate.

Inline clear/remove actions invoke Enter on key-down through the native Button
Invoke path. Space and pointer clicks invoke on release. Their original hit
surface remains available while the artwork shrinks, and the keyboard outline
scales with the artwork. Search clear hover uses the shared theme sand brush,
including live color/opacity changes. Full source raster and platform acceptance
remain in progress.

Footer section links wrap using full source line-box spacing while their
interactive inline boxes retain the source text bounds. Empty links add no
artificial row height. Narrow grid headings retain their column width while
unbreakable text can overflow. Plain RTL bottom content uses directional
intrinsic widths, and shrinking bottom slots retain CSS layout-unit precision.
Custom link renderer containers keep their own sizing and padding; the default
inline recipe does not resize arbitrary caller wrappers. Native input/raster,
motion/scaling and platform acceptance remain in progress.

`BleeckerProgressStack` provides observable consumer-owned `Items`, optional `Max` and `ShowLegend`. Each `BleeckerProgressStackItem` retains its label, numeric value and optional brush. This preview has remaining visual and platform acceptance work.

`BleeckerModalHost` is the preview root host for Orchard modal surfaces. Place
one around the entire window content, without an outer margin, transform or
size constraint. It retains the original content, content template and template
selector. The host's overlay registration is internal. `BleeckerAlertDialog`
uses that plane with caller-owned `IsOpen`, title/description, labels, loading
and variant properties. Cancel/Confirm request events and optional commands do
not close the dialog; the caller decides whether to change its bound open value.
`SurfaceStyle` customizes the native overlay surface. The catalog demonstrates
both variants, temporary loading and a wide square surface. Hidden-window ownership and lifecycle checks
do not establish physical keyboard routing, focus cancellation or native modality.

`BleeckerPopover` shares this root's portal plane when declared inside the host.
Nested popups keep the nearest modal owner; inactive modal and background
popups retain caller content/state while their private wrappers block input.
The Popover catalog fixture includes a fictional confirmation opened from its
original settings popup. Standalone windows without the shared host retain the
native Popup path. Complete input, visual and platform acceptance remains open.

Select, HeaderSelect, table filter Select and pagination choosers also share the
modal root. Their original option rows and producer callbacks remain intact.
The shared plane supplies outside-pointer shielding, logical containment with
parent Popovers, and active focus scopes. Newer modals inert an open menu without
overwriting its caller values or bindings. Physical input/focus and complete
open-menu visual acceptance remain unverified.

Tooltip and Field Help Tooltip use the same plane when their original placement
owner is inside `BleeckerModalHost`. They retain consumer content,
`ContentTemplate`, `ContentTemplateSelector`, description links and explicit
theme overrides. Private wrappers suspend input under a newer modal or Select
scope; the peer tree follows the active logical scope. Active help consumes
Escape before its owner. Standalone use retains the constrained Popup path.
The catalog includes a fictional Tooltip/Popover composition. Hidden checks
verify geometry, ownership and cleanup; physical keyboard/focus, complete
raster/motion/scaling and native ARM64 acceptance remain open.

`BleeckerModal` provides arbitrary original content, templates and selectors
inside `BleeckerModalHost`. Its `IsOpen` remains caller-owned. Close button,
backdrop, Escape, Window-pattern Close and `RequestClose()` raise
`CloseRequested` and execute an optional `CloseCommand`; the caller decides when
to close. `SurfaceStyle` derives from `BleeckerModalSurfaceStyle`. The 576px
surface uses responsive padding and scrolls its header with long content.
The catalog demonstrates editable content, nested confirmation and a wide
square surface with fictional local data. Overall component acceptance remains
incomplete.

Modal also follows live content reflow. Automatic child insertion/removal and
bound template text can grow and shrink the same surface without replacing
caller controls or data. The catalog's expand/shrink example keeps its original
editable note. Popover, Tooltip and controlled Select branches inside Modal
retain their logical owners and suspend under a newer modal.

`BleeckerDrawer` and `BleeckerSheet` provide caller-controlled edge dialogs beneath
the same `BleeckerModalHost`. Optional `Title` and `Description` accompany the
original content/template/selector. `CloseRequested`, `CloseCommand` and
`CloseCommandParameter` leave `IsOpen` with the caller. Drawer has a fixed handle
and heading with a scrolling body; Sheet supports four `BleeckerSheetSide` values
and optional caller-owned scrolling through `ScrollContent=false`. Side-specific
surface styles preserve the central theme tokens. The catalog demonstrates all
four sides, retained editors, body resizing and a nested Sheet. Full native
input/focus, visual/motion and platform acceptance remain incomplete.

Table cells accept caller typography (FontStyle, FontStretch and
CharacterSpacing), padding, borders and corners. The default renderer forwards
these values to text and rich content; sortable labels use the same typography.
A fully replaced native cell template owns its presentation. Templates using
Orchard's PART_Text/PART_Rich contract must provide both parts. Restoring the
default presentation retains original content and property bindings.

Simple DataTable column alignment is a style-level default. Caller selector
styles (including BasedOn setters), local values and live bindings retain
native precedence through refresh and sorting. Templated cells measure their
realized text, border, grid and stack content; bound text can grow or shrink
shared columns without replacing the original content or template.

Table reordering moves retained native rows/cells without unloading them.
Reference-identity membership and reusable text probes reduce repeated work;
plain intrinsic sizes are reused only while their layout inputs and scales
remain unchanged. Large initial mounts remain expensive; see the repository's
Table documentation for measured local workloads and limitations.

Rich table content with an explicit width skips descendant intrinsic-size work.
Live width, minimum, maximum and margin changes invalidate shared columns,
including releasing a bound width back to Auto. Original content and bindings
remain intact; constraint observers are released with content, templates and
unloading. Full visual, scaling, motion and platform acceptance remains open.

Data Table has a dedicated light surface palette and retains Orchard's dark
tokens. Its default surface forwards caller background, border and corner
values and uses both canonical surface-shadow layers. Caller templates can
own decoration while retaining the original seven functional template parts;
see the repository's Data Table contract for the optional shadow-part group.

Data Table global queries leave ordinary column-filter results intact when no
accessor is globally eligible; hidden eligible accessors still participate.
Numeric filter bounds and global eligibility resolve once for each fresh pass.
The default table cell preserves the line-box descent of empty actions while
retaining original caller content, margins, bindings and replacement templates.

`BleeckerSonner` is a mounted toast host with six positions, five variants and
caller-owned `BleeckerSonnerToast`, `BleeckerSonnerOptions` and
`BleeckerSonnerAction` data. `Show` returns an opaque ID; `Dismiss(id)` removes
immediately. Timeout and dismiss-button paths retain the item for 300ms. Actions
retain their original `ICommand` and parameter and do not implicitly dismiss.
Omitted duration captures the host default; explicit null, NaN, zero and negative
values persist. Call these APIs on the owning UI dispatcher after loading.

`BleeckerToast.Show`, `Success`, `Error`, `Warning`, `Info` and `Dismiss` provide
the pinned global routing contract; instance APIs provide explicit window
ownership. `BleeckerSonnerSurfaceStyle`, `BleeckerSonnerDismissButtonStyle`,
`BleeckerSonnerLinkActionStyle` and `BleeckerSonnerButtonActionStyle` expose the
four style boundaries. The catalog includes fictional local examples. Sonner
remains a preview: hidden geometry, palette and lifecycle checks do not establish
physical input/focus, complete raster/motion/customization or platform acceptance.

`BleeckerCommandSpotlight` presents caller-owned
`BleeckerCommandSpotlightAction` objects inside a `BleeckerModalHost`. Its
`Actions` collection preserves original IDs, labels, groups, keywords, shortcuts,
icons, hrefs and callbacks. `Open = null` uses internal state; Boolean `Open` is
controlled through `OpenRequested`. Search uses a whole lowercased substring,
and input arrows wrap through enabled originals in caller order. The entire
trigger shortcut hint hides below a 640px window width.
Hotkeys use browser key names and the current Windows layout's printable keys.
Hints use full uppercase mappings such as `ß` → `SS`; search and matching retain
the producer's Unicode lowercase/context behavior and original caller values.
Keyboard translation reads state without loading layouts or sending input.
Caller collection order and original action objects remain authoritative.
Group construction and retained-child membership use ordinal/indexed lookups;
native reordering moves retained visual elements. The catalog includes a
32/1,000-command collection example with fictional data.
Text and shortcut rendering avoid repeating unchanged native property writes;
weak per-element state does not retain removed visual elements. Shortcut dimensions
also apply when templates first load their nested key caps, and refresh with
live font/scaling and shortcut changes.
The backdrop's isolated-layer paint preserves the semantic source brush and
binding, observes live color/opacity and brush replacement, and disconnects
those observers when the presentation closes.

Selected rows preserve the original theme or caller brush while projecting its
clipped Oklab fill into a private premultiplied raster. Color, alpha, opacity and
binding replacements update live; unchanged refreshes retain the raster. Closing
releases its image and observers, and reopening reads the current source again.
The isolated backdrop keeps its own compositing path. Chromium and Skia
attribution notices are included in the package.

`ActionSelected` and optional `ActionCommand` receive the exact original before
its `OnSelectAsync`; an own callback takes precedence over href and does not
delay closing. Own failures are exposed through `ActionFailed` and
`LastActionError`. Handle `NavigationRequested` and set `Handled` to route an
internal href; the library does not invent application routes. Unhandled
absolute external hrefs use the Windows launcher. `SurfaceStyle`, `TriggerStyle`,
`OverlayStyle` and `OverlayTopOffset` expose native presentation boundaries.
Live caller style setters, including `BasedOn` setters, override generated
surface width and trigger height/padding/paint defaults. Clearing styles restores
the scaled defaults; style bindings remain intact across refresh and remount.
This component is a local preview: full raster/input/focus/Tab, motion,
customization and performance acceptance remains open.

`BleeckerHeroCarousel` retains caller-owned `BleeckerHeroCarouselItem` values,
literal destinations, external intent, tags and native `ImageSource` instances.
Use `BleeckerHeroCarouselItems` for observable ordering and `ItemInvoked` or the
host `Command`/`CommandParameter` for caller navigation. Native links do not
launch a router or reconstruct destinations. `LinkTemplate` receives the
original `Item`, prepared `Content`, `Style`, `AccessibleName`, `IsActive` and
guarded `Command` through `BleeckerHeroCarouselLinkContext`.

Previous/next and indicator controls change numeric selection; a 5,000ms
interval rotates multiple items. Hover pauses it; unloading retires it and
old link commands. Remount restores working navigation at index zero. Inactive
slides retain Tab/click input, while the group's UIA children expose the active
slide. Arrow keys throughout the owning XamlRoot advance each eligible mounted
carousel without consuming the event. Native INPUT/TEXTAREA equivalents keep
their arrows; RichEditBox follows the source contenteditable behavior and also
advances the carousel. Do not gate a source-equivalent LinkTemplate with
IsActive. Five public styles expose link, arrow, indicator,
tag and CTA decoration. `DefaultBackground` applies to no-image/empty content;
`EmptyState`, its template and selector retain caller content. Failed images
raise `ImageFailed` with the original item and retain the original image path.

Set or bind `FontFamily` to use a caller face across prepared slide text and the
plain empty message; clearing the value restores the source faces and weights.
A control style can supply the same override. `Foreground` retains a caller
brush on title, kicker and tags, including gradients and live brush mutations;
hover preserves the override and clearing it restores semantic theme colors.
Description/CTA colors and caller-authored rich templates remain independent.

Default tag, CTA, arrow and indicator chrome uses a live 2px backdrop blur.
Clipping follows the styled corners and current layout; the default arrow
template groups its underlay with pressed and disabled presentation. Prepared
text uses glyph shadows above the original chrome background. Masks update
with text, font, layout-size and DPI changes; empty text, removed tags, replaced
templates and unload release owned graphics resources. Caller replacement
templates continue to own their visuals.

Hero Carousel remains a preview. Hidden layout, sampled palette and ownership
checks do not establish physical input/focus, full raster/motion/customization
or native ARM64 acceptance. The fictional catalog fixture demonstrates the
component without product services.

### AuthShell layout

`BleeckerAuthShell` supplies a main content slot with optional `Brand`, `Footer`
and complementary `Aside`. Set `Layout` to `Centered` or `Split` and `Side` to
logical `Start` or `End`; `AsideLabel` names the complementary region. Every
slot accepts existing UI elements, scalar content, a data template or a template
selector, keeping the original caller model. Supply templates before presenting
model content. Layout, side, theme and responsive changes retain access controls
and their bindings. A responsive hidden aside retains its content; removing a
templated aside releases that presenter and restores a new root when re-added.
Existing caller-owned UI elements remain the caller's original objects.

The access section fills at least the native viewport height and grows for tall
content. It centers a maximum 28rem inner column between optional brand/footer
slots. The aside appears from the 1024px viewport breakpoint with a 24rem minimum
and a five-twelfths track. Padding follows the source 640/1024/1280px boundaries;
`FontSize` scales rem dimensions from 16 without moving viewport breakpoints.
`AccessBackground`, `AsideBackground`, `AsideForeground` and `FooterForeground`
expose themed region brushes. Inherited native styling remains available.

The aside and access padding minimums remain in force inside a narrower
container, including their visible overflow and logical RTL positions. The
component's default left/top alignment preserves block positioning when an
explicit width is supplied; caller alignment overrides remain available.
A taller centered shell keeps the access section at its natural viewport
height, leaving main background below it. A split shell stretches its grid
row. An explicitly short main with a cleared minimum still allows the access
section's own viewport minimum to overflow. Root padding and borders inset
the child layout without changing viewport-based breakpoints.

The registered **Auth shells** catalog uses fictional local member/recovery
forms, live layout/side/RTL/text-size controls and disabled/scalar examples.
Buttons update local display text only. The component provides layout and
content composition; authentication services and product workflows are outside
the library. Hidden source runtime covers 104 states and 24 behavior checks in
both themes, including actual layout, original model binding and native
action/disabled semantics. Fresh delivery validation is in progress. Full
visual, physical input/focus, scaling, lifetime, performance and native ARM64
acceptance remain open; the strict browser/native image gate still fails.
