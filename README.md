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
The [IconBadge preview](docs/icon-badge.md) supplies a passive, themed surface
with two sizes, three variants and retained consumer content or templates.
The [Empty preview](docs/empty-reference.md) supplies a passive empty state with
consumer title, description, icon and action content, including native templates.
The [Accordion preview](docs/accordion.md) supplies single-section expansion,
independent actions, rich content/templates and retained consumer input state.
The [Radio Group preview](docs/radio-group.md) supplies consumer-controlled
value-keyed options, descriptions, disabled states and wrapping horizontal or
vertical layout, with Windows keyboard navigation and selection automation.
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

The solution targets the Windows SDK `10.0.26100.0`, with declared minimum
Windows API version `10.0.17763.0`. WinUI compilation and runtime verification
require Windows. Pure token contract tests and the generator can also run on
macOS; this does not make Orchard a cross-platform UI library.

## Terminal workflow

From the repository root, in Windows PowerShell:

```powershell
dotnet build Orchard.sln -c Release -p:Platform=x64
dotnet test tests/Orchard.Tests/Orchard.Tests.csproj
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


### Native CI display requirements

The x64 and ARM64 runtime jobs require enough real desktop space for the frozen
1280-pixel client fixtures and the catalog window. `scripts/Set-CiDisplayMode.ps1`
runs only in GitHub-hosted Windows CI, enumerates the primary display's advertised
modes, prefers 1920×1080, tests support, and applies an adequate mode dynamically.
It verifies the observed physical dimensions and saves `artifacts/display-session.json`.
No registry, driver, system DPI, fixture viewport, capture containment check, or
pixel comparison is changed. A runner without an adequate supported mode fails
explicitly before the native tests.

Explicit client-size fixtures move the restored catalog window to the primary
screen origin and await presentation before physical input. Maximized fixtures
retain the operating system's placement. Runtime jobs have a bounded 60-minute
budget for the complete native, mutation-audit and package-consumer sequence;
individual fixture deadlines and all capture/pixel assertions remain unchanged.

The Brand contrast fixture samples the interior of its transparent source texel
rather than a rounded bilinear transition boundary. Exact system-color equality
remains required; the image interpolation and product palette are unchanged.

The chart load/focus fixture now holds focus on the preceding native button until
the late cases have actually loaded, then uses a physical Tab pair to enter the
chart. The eager cases still focus before Loaded. The report requires exactly
12 cases of each observed order, all 24 physical Right pairs and all first/next
inspection, blur and accessibility-help assertions.

ErrorState's native contrast mask is calibrated on the same live text with
opposite opaque paints at XAML animation precedence. Every captured byte and
original property/binding source must be restored after each paint. The mask is
fixed before changing Windows contrast mode; every selected pixel must still
match the real system foreground exactly. Failed calibration or restoration
fails the fixture, rather than establishing a contrast result.
Calibration uses the catalog's shared layout/compositor commit barrier before
baseline, opposite-paint, stability and restoration captures. Dashboard overflow
captures also verify foreground ownership at the capture boundary.

The Accordion scalar mutation audit copies its frozen corpus once, restores each
mutated receipt or image in `finally`, and still requires all 22 negative cases to
fail. Each verifier child has a 60-second deadline and emits its case, status and
elapsed time so a hung audit fails with evidence instead of consuming the job.

The native input verifier requires real routed pointer receipts per XAML root.
When a transferred window places a target at the cursor's existing screen
position, it first moves one owned pixel away and waits for that real receipt,
then returns and requires the target receipt. It never treats an unchanged
`SendInput` position as proof of delivery to the new root. The two-window
Loading Overlay lifecycle scenario exercises this boundary.

A failed exact glyph-raster restoration now records separate foreground-only
and backdrop-only calibration interventions. These run after the failure and
retain it; they neither retry the acceptance check nor replace its baseline.
They distinguish paint-induced raster changes from a product contrast-mode
failure before a further repair is chosen.

A rounded Input cursor mismatch retains its original native/SDK/publication
snapshot, then records physical visits to the existing neutral heading and
read-only Input. These diagnostic comparisons distinguish a global displayed
cursor anomaly from disabled-input ownership; the original failed assertion
and polling deadline remain unchanged.

Native fixture readiness: Loading Overlay contrast places a real pointer on the
open portal before the Windows transition. Accordion and Loading Overlay
captures invoke the existing guarded foreground check immediately before
capture; its recovery limit and strict owned-window checks are unchanged. The
independent package consumer waits for its original column checkbox to load
before invoking UIA and records the state before/after that action. Calibration
paint instability also collects failure-only foreground/backdrop interventions,
then retains the original failed assertion.

The hosted fixtures establish an owned neutral pointer target before Accordion
continuity input, and check guarded foreground recovery at Section Header capture
and Stepper Tab boundaries. Loading Overlay contrast restoration uses the same
bounded cover-window settling check as contrast entry. These changes retain
native target ownership, recovery limits, exact pixels and input assertions.
A packaged notification theme failure now records owner, trigger, presenter and
brush state after the original deadline, without changing the theme assertion.
