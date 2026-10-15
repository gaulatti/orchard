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

Sparklines follows Pie and donut charts. Focused `-Component sparkline` checks 36 frozen
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
