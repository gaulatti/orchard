# Catalog navigation

The left component list filters titles case-insensitively as the user types.
Choose a result to show that fixture. Clearing the search restores the full list.
An unmatched query shows an explicit message and leaves the current fixture
visible. The selected entry is highlighted; selecting it again preserves its
scroll position and current keyboard focus.

Only the selected fixture is mounted in the content host. Switching fixtures
unloads the previous visual tree, resets the content scroll position and loads
the selected fixture. Fixture objects are retained, so local example text and
selection survive navigation. This is visual-tree isolation, not deferred object
construction or a claim of bounded large-data virtualization.

The appearance selector stays outside the content host. Both navigation and
content consume Orchard's fonts, colors and controls. The sidebar uses existing
Bleecker button previews and the Input preview; their outstanding component
parity gaps still apply.

The **Sidebar** fixture follows Dashboard Grids and exercises the separate
`BleeckerSidebar` library control. It covers the frozen source data/layout,
consumer templates, keyed expansion and actual native routing/input boundary;
see [usage and acceptance gaps](sidebars.md). It does not replace the catalog's
existing component list.

The Alert keyboard regression walks the actual mounted native tab stops. Its
finite traversal bound follows that tree, including the alert close action,
so adding a navigation entry does not silently exhaust a fixed key count.
The check still requires real forward entry, exit, full-cycle return, reverse
exit and forward return to the close button.

**Stepper** follows Sidebar. Choose a source case to inspect both orientations,
all statuses, disabled/static actions, empty content, long labels and RTL.
Focused `-Component stepper` replays the source cases and exercises native
activation and consumer-owned state; see [the contract and open acceptance](stepper.md).

**Data list** follows Stepper. The source-case picker includes normal/striped,
empty, raw scalar, long, narrow, RTL and embedded-action content. Focused
`-Component data-list` checks source geometry, consumer actions/templates,
keyed lifecycle and real window resizing at the 640-DIP breakpoint. See
[the API and remaining acceptance](data-list.md).

**Data Table Toolbars** follows Data list. It mounts the original consumer table,
column callbacks and filtered row projection, with controlled search, status
filter and consumer action. Focused `-Component data-table-toolbar` checks the
118 normal source cases, actual native search/reset and lifecycle ownership.
See [the API and remaining acceptance](data-table-toolbar-reference.md).

**Data table columns** follows Data table pagination. Its nonmodal picker
changes the original consumer's eligible column visibility and visible-cell
projection. Focused `-Component data-table-column-toggle` replays 132 frozen
states and tests original-column ownership, real input, popup geometry,
disabled rejection, table/template replacement, unload and remount. See
[the source contract and open parity gaps](data-table-column-toggle.md).

**Data table filters** follows Data table columns. The selector operates on
the original consumer status column and displays its actual filtered rows.
Focused `-Component data-table-filters-native` verifies 276 frozen states,
native keyboard/pointer/modal input and source/template/option lifecycle.
The separate `data-table-filters` mode preserves the strict browser comparison
and its four missing-column focus differences. Full catalog verification
includes both-theme lifecycle checks and the registered preview captures.
See [the API, evidence and remaining gaps](data-table-filters-reference.md).
The separate `-Component data-table-filters-live` mode replays 140 additional
source states for live option replacement, labels, ordering, disabled/value
changes, duplicate keys and long-list scroll preservation.

Section Headers follows Page Headers. It covers optional eyebrow/summary text,
empty titles, constrained widths, RTL and multilingual content. Focused
`-Component section-header` uses the same navigation path and verifies the
frozen source layouts, real heading peers, live bindings, remount/template
replacement and physical forward/reverse Tab traversal. Complete text raster,
contrast, scaling and Narrator acceptance remains open.

Dashboard Sections follows Section Headers. Focused `-Component dashboard-section`
checks the frozen source's 639/640px viewport boundary, trailing/bottom action
placement, optional content, RTL/multilingual text, live updates, template
replacement and physical Tab/Space traversal through consumer children.
Dashboard Grids follows Dashboard Sections, with monitoring/editorial examples
and focused `-Component dashboard-grid` coverage of responsive tracks, spans,
implicit columns, live child changes and native keyboard traversal.

Breadcrumb is registered after Avatars, with source threshold boundaries,
independent wrapping, custom content, keyboard expansion and consumer updates.

Alerts is registered after Notifications. It exercises standalone and shared
window-owned notifications, four types, duration/message controls and close
callbacks. Navigating away unloads its owned popups and subscriptions.

Activity Item is registered after Bauhaus. Its normal, empty, multilingual and
RTL examples retain consumer strings and have no built-in activation action.
The focused `-Component activity` mode uses this same navigation path in both
appearances and checks physical hover, Tab behavior and lifecycle restoration.

Chart Theme is registered after Attention Surfaces. Its named roles, automatic
palette, scoped brand override and chart style specimens use live theme
references. The focused `-Component chart-theme` mode switches the root theme
in place and verifies composed palette pixels. The Line Charts fixture follows
Chart Theme and supplies observable consumer records, monotone curves and
keyboard inspection. Focused `-Component line-chart` verification compares
22 frozen light/dark specimens and tests live consumer changes. Complete chart
rendering, tooltip motion and full raster/platform acceptance remain inventory gaps.
Bar charts follows Line Charts and precedes Area charts. Focused `-Component
bar-chart` compares 68 Light/Dark source cases in both orientations, including
signed paths, palette, grids/ticks, category wrapping baselines, cursor bands and
physical inspection. It also checks live records/series/layout, collection
replacement, native Tab/Enter/pointer behavior, remount and template restoration.
See [the source contract and acceptance gaps](bar-chart-reference.md).

Area charts follows Bar charts. Focused `-Component area-chart` compares 80
Light/Dark source cases in overlapping and stacked modes, including curves,
ranges, single dots, signed stacks, grid/ticks, legend and native inspection.
See [the source contract and acceptance gaps](area-chart-reference.md).

Pie and donut charts follows Area charts. The shared `-Component pie-chart`
(or `donut-chart`) mode checks 84 frozen browser cases for both controls and
their native input/data/lifecycle path. See [the contract](pie-chart-reference.md).

Scatter charts follows Pie and donut charts. Focused `-Component scatter-chart`
compares numeric/bubble paths, axes, grid, legend and inspection with 64 pinned
browser cases, then exercises live root and independent series data, native
input, formatters, templates and observer lifecycles. See
[the source contract](scatter-chart-reference.md). `-Component
scatter-chart-sequence` runs that fixture followed by Sparkline in each theme
to retain the physical-input handoff regression.

The full smoke run's final per-page screenshots select each fixture and request
nonanimated bring-into-view, then observe two unchanged Rendering transitions
for viewport offsets, fixture origin/size and scroll extent. Each retains its
two-second presentation deadline and writes a `.png.layout.json` report. This
replaces fixed 100/350ms screenshot sleeps; fixture checks, screenshot order,
pixels and the complete run's 360-second limit remain unchanged.

Radar charts follows Scatter charts. Focused `-Component radar-chart` compares
72 frozen browser cases for polar paths, axes, labels, legend and inspection,
then exercises live data, native input and observer/template lifecycles.
`-Component radar-chart-sequence` runs Radar, Scatter and Sparkline in each
theme. See [the Radar contract](radar-chart-reference.md).

Radial Bar charts follows Radar charts. Focused `-Component radial-bar-chart`
compares 80 frozen Light/Dark cases for sectors, backgrounds, curved glyph
placement, legend and native inspection. The lifecycle checks exercise live
records, consumer paint/fonts, angle/radius changes, native Tab/Enter, remount,
template replacement, resize and empty data. `-Component
radial-bar-chart-sequence` runs Radial Bar, Radar, Scatter and Sparkline in each
theme. See [the Radial Bar contract](radial-bar-chart-reference.md).

Nonanimated button/icon state checks observe two Rendering frames and a
compositor commit within the existing two-second deadline. Input sessions
resize or request foreground only when the requested size/foreground differs.
All state assertions and pointer presentation/ownership gates remain in place;
the full smoke deadline remains 360 seconds.

Funnel charts follows Radial Bar charts. Focused `-Component funnel-chart`
compares 54 frozen Light/Dark cases for trapezoids, label text/wrapping/baselines,
palette and inspection. Native lifecycle checks cover original records, live
names/values/formatters/paint/fonts, physical Tab/Enter/arrows, stationary hover,
remount, template/data replacement, resize and empty data. `-Component
funnel-chart-sequence` runs Funnel, Radial Bar, Radar, Scatter and Sparkline in
each theme. See [the Funnel contract](funnel-chart-reference.md).

Sankey charts follows Funnel charts. Focused `-Component sankey-chart` compares
76 frozen Light/Dark graphs, node/cubic geometry, paint, hover opacity and
tooltip text/geometry through actual native pointer input. Its lifecycle suite
checks original nodes/links, live record/palette/options, stationary updates,
bounded node/flow keyboard inspection, disabled traversal, detach/rapid reparent,
template/root collection replacement, reordering, resize and empty data.
`-Component sankey-chart-sequence` runs Sankey, Funnel, Radial Bar, Radar,
Scatter and Sparkline in each theme. See [the Sankey contract](sankey-chart-reference.md).

Sunburst charts follows Sankey charts. Focused `-Component sunburst-chart`
compares 94 frozen Light/Dark cases for recursive sector paths, palette,
outlined values, advances and baselines. Its lifecycle suite exercises original
shared records, property/collection changes, literal keys, explicit root totals,
fonts, template replacement, detach/rapid reparent, reset and resize. Actual
native hover/click preserves the source's absence of visible inspection; Tab
skips the named image. The image's help text exposes ancestor paths and values.
`-Component sunburst-chart-sequence` runs Sunburst, Sankey, Funnel, Radial Bar,
Radar, Scatter and Sparkline in each theme. See [the Sunburst contract](sunburst-chart-reference.md).

Tooltip opening checks require full opacity/scale and unchanged size, placement
and origin across two Rendering transitions within two seconds. Dismissal
waits for the actual popup to leave the root; the separate motion/interruption
observations retain their original timing. Avatar, Skeleton and Page Header
setup uses the existing two-frame/compositor readiness check. Font evidence
captures complete the actual native render before reading measurements,
replacing fixed sleeps while preserving each PNG and assertion. These changes
leave pointer gates, source tolerances and the full 360-second deadline intact.

Nonanimated fixture binding, visual-state, template and font-probe checks use
the same two Rendering callbacks followed by an actual compositor commit.
Menu checks wait for their owned loaded presenter and its presentation; hiding
waits for the actual Closed event, presentation completion and removal from the
root before reopening. Notification command/collection checks
wait for the presented content, and closing waits for the actual popup removal.
The previous fixed 80–200 ms setup sleeps are removed. Timed motion samples,
all source/pixel/state assertions, pointer delivery gates and capture sets stay
unchanged. The complete catalog retains its 360-second deadline.
Focused `-Component presentation` runs every affected fixture in both themes,
including menu/notification lifecycle, source typography, normal input and cursor
checks, and unchanged toggle/button state assertions.

Absolute mouse input selects the requested pixel's interior in the 65,536-unit
virtual-desktop range. The prior endpoint interpolation could request `(61,84)`
and actually deliver `(60,84)`; exact native receipt checks caught that error.
The normalizer is exhaustively checked against the Windows mapping across
desktop sizes and negative origins. Out-of-range or unrepresentable pixels
fail explicitly. Actual owned positioning, nongenerated event receipts, exact
boundary samples and the two-second deadline remain required. See Microsoft's
[absolute input contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mouseinput)
and Chromium's [Windows input implementation](https://github.com/chromium/chromium/blob/main/ui/base/win/event_creation_utils.cc).

Sparklines follows Sunburst charts. Focused `-Component sparkline` checks 36 frozen
Light/Dark cases, raw tooltips, physical keyboard/pointer input, live bindings
and remount. It also verifies four all-negative normal Line Chart axes against
actual rendered source tick labels in each theme.
Stat cards follows Sparklines. Focused `-Component stat-card` compares 80 frozen
Light/Dark compositions, then checks live bindings/trends, normal keyboard input
through history and footer actions, disabled inheritance, remount and consumer
value/control template replacement. Scalar slots, word overflow/wrapping and
composed clipping are included. Sixteen further source specimens check 54
baseline/occupied-row results in composed desktop captures, plus typography
after content changes, fractional-origin moves, remount and template replacement.
RTL glyph advance and complete glyph raster accuracy remain open.

Focused `-Component font-audit` is a capture-only diagnostic for the five bundled
font faces, canonical/legacy names, weight selection and explicit system/missing
family comparisons. `result.json` marks `captureOnly` true and `parityVerified`
false. It is separate from the full acceptance suite.

Focused `-Component stepper-raster` captures the existing Stepper reference
frame fully on screen and as a bitmap for all 44 source cases. Its independent
audit checks screen stability and compares every frame pixel; the diagnostic
restores the frame's original catalog parent. Like the font audit, it is
capture-only and grants no parity acceptance. See [Stepper](stepper.md).

The full runtime suite uses the same fixture selection path before exercising
each registered component and capturing its result. Navigation checks exercise
search, native button activation, no-result messaging, clearing search, retained
consumer text and unloading of the previous fixture. The focused Input mode
selects Input through the same catalog path.

The focused tooltip mode (`-Component tooltips`) also navigates away and back
between appearances, exercising theme propagation after owner reattachment.

Verification on the Intel x64 host passes the full light/dark runtime suite with
Windows Animation effects off (`artifacts/smoke-navigation-fixed`) and on
(`artifacts/smoke-navigation-motion`). Both Input themes capture intermediate
color and opacity frames with animations enabled. This verifies the existing
motion checks; it does not establish a performance budget or exact browser
animation parity. The original Windows setting was restored.

Actual keyboard input verifies mixed-case search followed by Tab/Enter
activation (`artifacts/navigation-keyboard-input.jpg`). An unmatched query keeps
Input visible and shows the explicit message (`artifacts/navigation-no-results.jpg`).
Clearing search restores the list, and pointer activation returns to Foundation.
The baseline unit/coverage suite passes all 19 tests, and x64 publish and ARM64
compilation pass. The full navigation/catalog fixture checks now also pass on
native ARM64; see [runtime evidence](runtime-evidence.md) for architecture
reports and the remaining platform acceptance.

Text-size/density controls, the remaining unimplemented inventory and full
keyboard/Narrator/responsive acceptance are still required. Catalog navigation
does not make any component's visual or interaction acceptance complete.

For an explicit constrained-window runtime check, pass `-ClientSize width,height`
to `scripts/smoke-catalog.ps1`, for example `-Component input -ClientSize 1024,697`.
Both dimensions must be positive integers. The verifier restores and resizes
only its own overlapped catalog window and requires the resulting client area
to match the supplied physical-pixel dimensions. Omitting this option retains
the existing maximized verifier. It does not change Windows display or text
scale. Capture ownership and monitor-bound checks still apply.

The **Windows contrast** page is a registered cross-component verification
fixture. Normal navigation displays the semantic-color specimens; only its
guarded CI smoke entry point changes the isolated runner's actual mode. See
[contrast verification](contrast-verification.md) for scope and remaining gaps.

The Switches page adds off/on, both disabled states, a named unlabeled switch,
long/Japanese/RTL labels and a consumer-owned two-way Boolean binding. Its
registered runtime checks and remaining acceptance are documented in
[switches](switches.md).
The focused `scripts/smoke-catalog.ps1 -Component switches` route explicitly
exercises real keyboard/pointer input through the shared owned-window session
in both appearances, without requiring the CI environment flag.

The **Brand lockups** page adds logo-only and named lockups in both sizes,
consumer-owned RGBA artwork, a typed link template, command availability,
disabled, constrained international text and a failed source. Its focused
runtime path is `scripts/smoke-catalog.ps1 -Component brand`; it also participates
in the full catalog. See [the contract and current evidence](brand-lockup-reference.md).

The **Bauhaus backgrounds** page follows Brand lockups. It shows four variants,
RGBA image blending, square/tall layouts and the missing application-owned hero
image. Its focused runtime path is `scripts/smoke-catalog.ps1 -Component bauhaus`;
see [the contract and current evidence](bauhaus-background-reference.md).

The **Attention surfaces** page follows Activity items. It registers all source
elements and densities, independent visual interactivity, disabled/empty/long
content, consumer styles and child actions, and wide/tall/square gradients.
`scripts/smoke-catalog.ps1 -Component attention` exercises its initial native
boundaries and records composed gradient samples. Those samples report observed
differences; they do not establish complete visual acceptance. See
[the source contract and current evidence](attention-surface-reference.md).

Focused `-Component checkboxes` runs the Checkbox page's normal fixture in
both themes, including shared disabled Checkbox/Input ownership and rapid
stationary Input remounts. It retains native border/cursor expectations and
records the standard catalog captures. See [Input verification](inputs.md).

Physical pointer preparation requires the requested point to pass the existing
native owned-window hit check, then waits for native target bounds, actual dimensions
and ancestor viewport metrics to remain unchanged across two Rendering
transitions. It uses the catalog's two-second presentation policy and retains
the normal foreground, native ownership, exact boundary-position and final-hit
checks. Each input session records `pointerPresentations`, including every
observation and failure. This replaces the unconditional 100ms pre-move delay;
the complete runtime check still has its 360-second limit.

The post-move gate waits for a real, non-generated mouse `PointerMoved` routed
through the catalog root, target or current popup child. Its root-relative
coordinates must map to the exact requested native screen pixel, within the
same two-second policy. This replaces a fixed 30ms delay. A move to unchanged
coordinates can reuse only a previous actual receipt at that exact point,
together with a fresh matching native cursor read; Windows need not emit a new
event for that move. The normal SendInput operation still runs. Every movement
records receipts, elapsed delivery time and whether this stationary condition
applied. Final bounds, ownership and exact boundary assertions are preserved.
Baseline diagnostics retained the old delay across 874 moves in Sankey, Input,
Checkbox, Tooltip, Alert and Attention fixtures: all 854 changed positions had
actual matching receipts, and twenty eventless moves repeated a preceding
confirmed coordinate. See `artifacts/pointer-receipt-baseline.json`. Full runtime
verification of the event-based gate is recorded separately.

The first complete working-tree attempt at
`artifacts/smoke-sankey-chart-source-full` stopped after 230.298 seconds during
Dark Attention gradient checks. A final pointer assertion detected a 222-pixel
target shift: scroll metrics had changed while the reported transform remained
at its previous position for two Rendering transitions. When a bring-into-view
operation changes viewport metrics, preparation now waits for a compositor
commit and rechecks the complete bounds/dimensions/viewport/ownership state.
Any change resets the stable count. The same two-second deadline and final
native pointer assertions remain active.

Click delivery likewise requires an actual non-generated mouse release at the
requested screen point, with the left button released, within two seconds.
This replaces the fixed 100ms post-click wait. Baseline runs retained that delay
while recording all 56 click releases in Attention, Switch, Checkbox, Alert,
Brand, Activity and Page Header fixtures; every release matched, with a maximum
of 36.706ms (`artifacts/pointer-release-baseline.json`). Existing invocation,
toggle, disabled, focus and timed-motion assertions remain separate.
