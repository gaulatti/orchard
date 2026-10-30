# orchard

Gaulatti’s native **Windows** component library, built with C# and WinUI 3.
Orchard implements Bleecker’s visual language for Windows. Sabella owns Apple
platforms; Linux will have its own implementation.

The library and runnable Visual Studio catalog provide typography, generated
tokens, light/dark themes, high contrast resource mappings, button and icon-button
variants, connected button groups, toggle styles, a native switch preview and
initial input styles. See [Switch usage and acceptance](docs/switches.md) and
[Checkbox usage and acceptance](docs/checkboxes.md).
Full component parity and platform acceptance remain in progress.

Download the [2026-10-29 `.31` preview](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261029.31)
for the current x64 component milestone. Thirty-six of 112 entries are accepted:
Avatar, Brand Lockup, Brand Mark, Breadcrumb, Button, Button Group, Card, Context Menu, Dashboard Grid, Dashboard Section, Detail Layout, Dialog Close Button,
Empty, Error State, Feed Grid, Field Help Tooltip, Footer, Icon Badge, Icon Button, Kbd, Label, Loading Spinner, Metric,
Page Frame, Page Header, Panel Layout, Progress, Progress Stack, Section Header,
Separator, Skeleton, Sparkline, Stat Card, Status Badge, Switch and Tooltip. The [release note](docs/releases/2026-10-29-preview-31.md)
records the five checks and package hashes. Download the matching
[x64 catalog ZIP](https://github.com/gaulatti/orchard/releases/download/v0.1.0-preview.20261029.31/Orchard.Catalog.0.1.0-preview.20261029.31.win-x64.zip)
or the [ARM64 build](https://github.com/gaulatti/orchard/releases/download/v0.1.0-preview.20261029.31/Orchard.Catalog.0.1.0-preview.20261029.31.win-arm64.zip).
ARM64 runtime remains unverified.

The earlier [v5 preview](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261027.5)
has x64/ARM64 catalog ZIPs with the fictional Monitoring and Editorial demos.
The [v5 artifact record](docs/evidence/catalog-v5-archives/checkpoint.json)
lists their hashes and checks. Its extracted x64 ZIP passed hidden demo checks
and visible pointer and keyboard paths. The v5 NuGet consumer later exposed a
normal window-close crash
(`0xC000027B`), and routed hover over Theme Toggle's default tooltip also
crashed WinUI. Its earlier input probes missed the close exit code; see the
[corrected consumer record](docs/evidence/theme-toggle-package-accepted-input-v1/checkpoint.json).
Native ARM64 execution and final component acceptance remain open.

Nine display components preserve caller automation names and label associations;
Error State also preserves caller HelpText. See [metadata ownership](docs/automation-metadata.md).

Kbd preserves caller automation names and label associations, including empty
values. Its content fixture also verifies isolated native scaling. See [Kbd](docs/keyboard.md).

Progress rejects invalid ranges and exposes its displayed, bounded value to
UI Automation while retaining valid caller values and bindings. See [Progress](docs/progress.md).

Separator preserves fractional one-logical-pixel coverage. Its native scale
fixture uses an owned hidden XAML island. See [Separator](docs/separator.md).

Status Badge preserves caller automation names and help text, including explicit
empty values and inherited styles. See [Status Badge](docs/status-badge.md).

File Input honors ancestor implicit font/padding styles while preserving original
selected files across size changes. See [File Input](docs/file-input.md).

Avatars preserve caller dimensions and font-size bindings across size changes,
styles and templates. Empty silhouettes follow the actual caller dimensions.
See [Avatar usage and qualification](docs/avatar-reference.md).

Keyboard keycaps honor caller content alignment and live bindings, including
combination keys, templates and selectors. Their shadows follow caller corner
settings. See [Keyboard shortcuts](docs/keyboard.md).

`BleeckerSkeleton` now hosts original caller content, data templates and selectors.
Its content/opacity bindings, editor state, child actions and accessible child
peers survive template changes and remounts. See [Skeleton usage](docs/skeleton-reference.md).

The [Nav Menu preview](docs/nav-menu.md) presents caller-owned links in rows or
columns, with host routing and custom link templates.
The [Collection Filters preview](docs/collection-filters.md) composes native
Select and Date Picker controls with caller-owned filter/sort requests,
collapsible fields, active chips, responsive layout and source-generated
surface colors in both themes.
The [IconBadge preview](docs/icon-badge.md) supplies a passive, themed surface
with two sizes, three variants and retained consumer content or templates.
The [Empty preview](docs/empty-reference.md) supplies a passive empty state with
consumer title, description, icon and action content, including native templates.
The [Accordion preview](docs/accordion.md) supplies single-section expansion,
independent actions, rich content/templates and retained consumer input state.
The [Radio Group preview](docs/radio-group.md) supplies consumer-controlled
value-keyed options, descriptions, disabled states and wrapping horizontal or
vertical layout, with Windows keyboard navigation and selection automation.
The [Tabs preview](docs/tabs.md) supplies underline, segmented and enclosed strips,
three sizes, consumer-controlled selection and host-routed links with custom renderers.
The [Dialog Close Button](docs/dialog-close-button.md) supplies the source close
action with adjustable icon geometry, native commands and consumer-owned dismissal.
The [Field preview](docs/field.md) composes labels, controls, actions and messages
while preserving consumer values, validation and native metadata relationships.
The [Secure Field preview](docs/secure-field.md) supplies native password editing,
persistent reveal, caller-owned values and fictional catalog examples.
The [Segmented Control preview](docs/segmented-control.md) supplies typed value
selection with keyed options, Windows selection metadata and constrained layouts.
The [App Tab Bar preview](docs/app-tab-bar.md) supplies typed destination selection,
caller-owned Windows icons, capped badges and raw-count accessibility metadata.
The [OTP Input preview](docs/otp-input.md) supplies controlled digit requests,
native text/password slots and fictional caller-owned validation examples.
The [Context Menu draft](docs/context-menu.md) supplies native caller-owned
actions with a distinct context recipe, bindable modality and shared modal ownership. Input,
visual/motion and platform acceptance remain open.
The [Table preview](docs/table.md) supplies retained native cells, shared
columns, spans, scrolling and table metadata. Sortable headers and the basic
DataTable composition retain original cell/footer producers and nullable
controlled sort/selection. Source and fresh NuGet hosts verify 26 states and
72 action snapshots, including live bindings and notifications. Complete
component/platform acceptance is open; the separate TanStack adapter is pending.
The latest local preview also verifies Unicode/language header casing and
ordinal row keys/property names in source and fresh NuGet hosts. Cell styles now
forward font style/stretch/tracking and draw borders/corners, retaining bound
values and original content across template restoration. Column styles and live
alignment bindings now retain precedence; templated content resizes shared
columns when its bound text grows or shrinks. Retained rows now stay loaded
during sorting, with measured local performance and cold-mount limitations
recorded in the Table documentation.
Explicitly sized rich content skips unnecessary descendant sizing; live bound
width/minimum/maximum changes, including release to Auto, update shared columns
while retaining the original content and bindings.

The separate [Data Table](docs/data-table.md) records the pinned
TanStack composition: filtering, sort cycles, index-based selection and ten-row
pages. Its 42 source visual states and 166 interaction snapshots are frozen;
its native model, control and catalog fixture are local drafts. Native replay
covers all 208 states, including grouped headers and combined filter updates;
the actual catalog passes 24 interaction states. Complete visual, input and
platform acceptance remains open.
The [PageFrame preview](docs/page-frame.md) supplies centered reading/content/wide/full
boundaries with viewport-responsive spacing and retained consumer content.
The [DetailLayout preview](docs/detail-layout.md) composes retained main/rail
content with responsive columns, logical placement and bounded sticky positioning.
The [Slider draft](docs/slider.md) separates original displayed values from
sanitized range values and routes changes through consumer-controlled requests.
Guarded package tests match 182 source layouts and controlled ownership checks.
Its visible desktop verification is pending.

The [Theme Toggle draft](docs/theme-toggle.md) requests consumer-owned
Light/Dark/System changes with native icon-button variants and source labels.
Guarded hidden XAML checks verify its normal geometry and caller metadata
ownership. Its visible desktop verification is pending.

The [FeedGrid component](docs/feed-grid.md) auto-fills minimum-width columns,
reserves empty tracks and retains original caller controls across resizing.
The [FeedColumn preview](docs/feed-column.md) composes a fixed title/count/filter
header with an independent native scrolling body and caller-owned content.
The [FeedItem preview](docs/feed-item.md) presents author/time/relevance metadata,
wrapped content/categories and consumer-owned source-link requests.
The [Select and HeaderSelect previews](docs/select.md) provide three menu sizes,
compact header sizing and requests over original consumer options and bindings.
The [AppShell preview](docs/app-shell.md) composes original header/body/footer
controls with a viewport minimum and caller-controlled header offset.
The [Header prototype](docs/header.md) composes original brand/navigation and
desktop/mobile actions with responsive, fixed/sticky/static layout.
The [Popover prototype](docs/popover.md) anchors original caller content to an
original trigger, with controlled requests, internal history and source
placement. Hidden source-layout and ownership checks pass; full acceptance is
in progress.
The [HoverCard prototype](docs/hover-card.md) retains original trigger/content
in an inline hover surface with source placement and opacity motion. Hidden
layout, presentation-clock and ownership diagnostics pass; physical input and
complete visual/platform acceptance remain open.
The [ScrollArea prototype](docs/scroll-area.md) retains original content in a
native viewport with user orientation, programmatic offsets, reserved scrollbar
space and template support. Hidden source-layout, ownership and thumb-body
checks pass in the catalog and a fresh package consumer; full acceptance is open.
The [AdminShell draft](docs/admin-shell.md) adds a retained sidebar and a native
main scroller containing body and footer. Its independent native consumer matches
312 initial and 16 scrolled source states and passes 66 ownership checks,
including zero-minimum reset and live caller width constraints.

The [Monitoring and Editorial component demos](docs/workspace-demos.md) combine
the library controls into feed selection/review and page editing compositions.
Both use fictional in-memory data, with retained edits and explicit discard
decisions. Their visible interaction and platform acceptance remain in progress.

The [PanelLayout preview](docs/panel-layout.md) composes fixed and growing
panels horizontally and in vertical columns, retaining caller controls and
horizontal scrolling. The monitoring/editorial demonstrations remain required.

The [Panel preview](docs/panel.md) supplies an accented title/count header,
optional consumer slots and an independently scrolling body with retained state.
The [Date controls preview](docs/date-controls-reference.md) supplies a controlled
Calendar, Date Picker and Date Range Picker with local-date bounds, consumer
requests, presets and native popup focus. `BleeckerTextLayout.UseReferenceLineBox`
provides opt-in branded text alignment for an explicit `LineHeight`; date styles
apply it automatically, with tabular day numerals. Full visual/platform acceptance remains
in progress.
The [Skeleton preview](docs/skeleton-reference.md) supplies decorative shapes,
loading cards and tables with the pinned Bleecker dimensions and pulse recipe.
The [Avatar preview](docs/avatar-reference.md) supplies consumer images, initials,
silhouettes and overlapping groups with overflow counts in all five sizes.
The [Activity Item preview](docs/activity-item-reference.md) supplies exact
consumer-owned title/time strings, an Avatar and source hover styling.
The [Chart theme API and preview](docs/chart-theme-reference.md) supplies the
frozen role/palette resource keys and immutable chart style metadata. The
[Line Chart preview](docs/line-chart-reference.md) renders observable consumer
records/series with native monotone curves, axes, a legend and category
inspection; complete chart/platform acceptance remains in progress.
The [Currency Chart preview](docs/chart-reference.md) implements the fixed
`Chart` export with original `name`/`total` records, dollar ticks and a desert
monotone line. Its acceptance remains in progress.
The [Section Header preview](docs/section-header-reference.md) supplies a
level-two heading, optional eyebrow and consumer-owned summary.
The [Dashboard Section preview](docs/dashboard-section-reference.md) groups
consumer content beneath an optional responsive heading and action.
The [Dashboard Grid preview](docs/dashboard-grid-reference.md) supplies responsive
columns and spans, with fictional monitoring and editorial catalog compositions.
The [Sidebar preview](docs/sidebars.md) supplies keyed consumer destinations,
retained groups, independent scrolling and native link templates.
The [Data Table Toolbar preview](docs/data-table-toolbar-reference.md) connects
controlled search and reset to an application-owned table/filter adapter,
original content and a real filtered-row catalog view.
The [Data Table Pagination preview](docs/data-table-pagination-reference.md)
connects page navigation, size selection and filtered selection counts to the
consumer's original table and rows, with viewport-responsive controls.
The separate [Pagination draft](docs/pagination.md) requests caller-owned pages
through native buttons. Its catalog fixture is available from source; the
previously verified v20 NuGet preview does not include this new control.
The [Data Table Column Toggle preview](docs/data-table-column-toggle.md) offers
eligible original columns in a nonmodal checkbox popover. Visibility changes
reach the consumer's original columns and visible-cell projection.
The [Data Table Filters preview](docs/data-table-filters-reference.md) connects
a controlled Select to an original consumer column, with observable options,
modal keyboard/pointer input and a real filtered-row projection.
The [Sparkline preview](docs/sparkline-reference.md) supplies compact consumer
trends with monotone curves and native keyboard/pointer inspection.
The [Stat Card preview](docs/stat-card-reference.md) composes consumer values,
live trends, optional history, icons and footer actions.
The [Radar Chart preview](docs/radar-chart-reference.md) supplies polar subject
plots with observable consumer data/series, range boundaries, fill and native
keyboard/pointer inspection.

The [Radial Bar Chart preview](docs/radial-bar-chart-reference.md) supplies
consumer-owned radial bars, curved value labels, backgrounds and a sorted
legend, with live records and native keyboard/pointer inspection.
The [Funnel Chart preview](docs/funnel-chart-reference.md) supplies native
trapezoids, source name-only labels, live records and value inspection.
The [Sankey Chart preview](docs/sankey-chart-reference.md) supplies native nodes
and cubic flows from original consumer graphs, with live data and inspection.
The [Sunburst Chart preview](docs/sunburst-chart-reference.md) supplies recursive
sectors and outlined values from original consumer trees, with a named native
image and live hierarchy updates.

The catalog has a searchable component list. Selecting a component mounts its
fixture and unloads the previous one while preserving its local example state.
Appearance selection remains available across pages. See
[catalog navigation](docs/catalog-navigation.md) for verification and limits.

## Open in Visual Studio

1. On Windows, install **Visual Studio 2026** with the **WinUI application
   development** workload, following [Microsoft’s setup guide](https://learn.microsoft.com/en-us/windows/apps/get-started/start-here).
2. Install **.NET SDK 10.0.401**. `global.json` deliberately requires this exact
   version; a different installed feature band does not substitute for it.
3. Clone `https://github.com/gaulatti/orchard.git` and open **`Orchard.sln`**.
4. Select **Debug / x64** on an Intel/AMD machine, or **Debug / ARM64** on a
   Windows ARM machine.
5. Set **Orchard.Catalog** as the startup project and press **F5**.

The catalog is unpackaged and self contained. Building restores .NET and
Windows App SDK dependencies; launching the resulting output does not require
an MSIX registration or a separate Windows App SDK runtime installation. Keep
the complete output directory together.

For a low-disk release ZIP, `scripts/package-catalog-build.ps1` streams the
current catalog build output into an archive, excludes an older nested
`publish` directory, and checks every ZIP entry against its source file. The
script records an archive manifest but does not run the extracted archive;
runtime qualification is a separate check.

The solution targets the Windows SDK `10.0.26100.0`, with declared minimum
Windows API version `10.0.17763.0`. WinUI compilation and runtime verification
require Windows. Pure token contract tests and the generator can also run on
macOS; this does not make Orchard a cross-platform UI library.

## Terminal workflow

From the repository root, in Windows PowerShell:

```powershell
dotnet build Orchard.sln -c Release -p:Platform=x64
dotnet test tests/Orchard.Tests/Orchard.Tests.csproj
dotnet test tests/Orchard.Text.Tests/Orchard.Text.Tests.csproj -c Release
./scripts/run-catalog.ps1
```

Use `./scripts/run-catalog.ps1 -Platform ARM64` on Windows ARM. GitHub Actions
exercises the x64 catalog and independent package consumer on `windows-2025`,
and the ARM64 versions on the native `windows-11-arm` runner. Runtime reports
record process and OS architecture; the gates reject architecture mismatches.
A separate x64 job retains ARM64 cross-compilation coverage. The catalog and
independent consumer have passed on both architectures; see
[native runtime evidence](docs/runtime-evidence.md) for scope and remaining
platform acceptance.

## Repository layout

| Path | Purpose |
| --- | --- |
| `Orchard.sln` | Visual Studio solution |
| `src/Orchard` | Reusable WinUI library and Bleecker resources |
| `samples/Orchard.Catalog` | Development catalog and native runtime smoke checks |
| `tests/Orchard.Tests` | Token-to-C# and token-to-XAML contract tests |
| `tokens` | Pinned upstream Bleecker snapshot and provenance |
| `scripts` | Token generation and Windows launch/verification commands |

## Consume the library

For local build output, storage limits and cleanup, see [artifact storage](docs/artifacts.md).

Build a local NuGet package and verify it independently:

```powershell
dotnet pack src/Orchard/Orchard.csproj -c Release -p:Platform=x64 -o artifacts/packages
./scripts/verify-package.ps1
```

The verifier clears only its generated `Output/app` directory before each run,
so an ARM64 native DLL cannot survive into an x64 consumer. Reports in `Output`
remain available across runs.

Add `artifacts/packages` as a NuGet source in the consuming application and
reference `Gaulatti.Orchard` version `0.1.0`. The package supplies the library,
compiled XAML resources, fonts and their licenses. It needs no sibling checkout.
For library development, a project reference to `src/Orchard/Orchard.csproj` also
works. In the application’s resource dictionary, load WinUI resources first,
then Orchard:

```xml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <ResourceDictionary Source="ms-appx:///Orchard/Themes/Orchard.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Apply explicit styles such as `BleeckerPrimaryButtonStyle`,
`BleeckerSecondaryButtonStyle`, `BleeckerHeadingStyle`, `BleeckerBodyStyle`,
`BleeckerTextBoxStyle`, and `BleeckerComboBoxStyle`. Use `{ThemeResource ...}` for
colors so an open page reacts to theme changes.

Action component usage and remaining acceptance are documented separately:
[buttons](docs/buttons.md), [icon buttons](docs/icon-buttons.md),
[button groups](docs/button-group.md), [toggles](docs/toggles.md), and
[toggle groups](docs/toggle-groups.md).
Consumer-owned error summaries and optional retry commands are documented in
[Error State](docs/error-states.md).

See [architecture and verification](docs/architecture.md) for boundaries,
accepted scaffold behavior, and the remaining component work.
The [fixed parity inventory](docs/parity.md) records reference commits and
tracks the complete Windows component scope; pending entries are not implemented.

## Design tokens and fonts

`tokens/bleecker.source.ts` is an exact snapshot of Bleecker’s canonical token
source, pinned by commit and SHA-256 in `tokens/source-lock.json`. No sibling
checkout or network fetch is needed to generate or use Orchard.

With Node.js 24 or later:

```sh
node scripts/generate-tokens.mjs
node scripts/generate-tokens.mjs --check
```

The generated JSON, C# tokens, and XAML dictionaries are committed together.
To adopt an upstream change, review the source diff, copy the exact source at
the selected Bleecker commit, update its provenance/hash, regenerate, and run
the contract tests and catalog. Do not edit generated files directly.

Encode Sans and Libre Franklin are bundled under the SIL Open Font License.
Attribution and license texts are adjacent to the fonts. The vendored Bleecker
source retains its MIT license under `tokens/LICENSE-Bleecker.txt`.

The passive [ProgressStack preview](docs/progress-stack.md) adds consumer-owned segments and a wrapping legend; acceptance remains in progress.
