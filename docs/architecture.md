# Architecture and verification

Nav Menu keeps caller-owned destinations as opaque strings. Stable source keys
retain original items and custom template children across collection reordering;
retired contexts cannot invoke the host. A private flex panel applies intrinsic
minimum widths, source spacing and a single RTL arrangement. Source default link
styles and custom contexts share the prepared content and current foreground.
Color subscriptions and rendering clocks detach on unload or template retirement.
The effective 220 ms source transition uses the existing encoded-sRGB clock and
standard curve; it follows the current reduced-motion preference.
See [Nav Menu's API and acceptance](nav-menu.md).

Collection Filters owns only expansion and private presentations. Its original
filter dictionary, sort object and option objects remain caller-owned; actions
emit new requested values. Native Select and Date Picker compose inside retained
field slots. Viewport breakpoints control track count and header padding; RTL is
applied once to arranged tracks and separately to child content. Collapsing
deactivates/removes editors, and producer/root subscriptions stop on unload.
Action callbacks validate their actual sender's membership in the current
visible template, preventing stale peers from targeting replacement controls.
Its owned surface palette is generated from the frozen CSS utility tokens'
transparent DOM encodings. Alpha is encoded once in each brush's color; source
swatches, generated XAML and actual native brushes are checked independently.
Canonical numeric field keys sort before other keys in chips and newly emitted
dictionaries; the caller's dictionary retains its own order and values. The
active-count text uses tabular numerals in both its measurement and presentation.
See [the API and verification boundary](collection-filters.md).

Pagination keeps the caller's current page and availability booleans as its
only navigation state. Native buttons emit requests without assigning those
properties. Keyed page buttons remain mounted while their order, palette and
automation/Tab order update. A private row reproduces source flex shrink and
layout-grid centering; bundled-font text uses the shared measurement bridge.
Private native button backgrounds fill beneath their transparent borders, and
page labels follow the native presenter's state foreground. A retained private
button template exposes normal/hover palettes without stock transition timing.
Per-property production clocks paint colors and opacity with source interpolation
and 190ms premium easing; pressed scale stays immediate. System reduced motion
snaps these clocks. Brush, palette, preference and rendering subscriptions stop
when a button or owner is removed and reconnect on load. A catalog diagnostic
replays the same paint method at controlled times; it does not verify the
real-time normal-motion preference path. The catalog and fresh package consumer
exercise the same public control. See
[the contract and remaining acceptance](pagination.md).

The AdminShell draft uses a separate scrolling Main region with responsive
body/footer wrappers, preserving the outer header/sidebar controls. Its 312
initial and 16 scrolled source states match the independent native consumer.
Private background/border wrappers keep viewport width while separately
arranged content slots can expose explicitly wide native roots without clipping.
Auto-width roots retain native wrapping. Root width/minimum/maximum/margin
callbacks invalidate flow measurement even when the parent's clipped desired
size does not change; callbacks disconnect on unload and replacement. Both shell
controls use a private viewport minimum state that yields to caller values,
bindings and styles; explicit-zero removal and unload observation are covered
by native regressions. See [the current boundary](admin-shell.md).
The native scrolling viewport paints the main background through an initial
one-way binding; a caller replacement retains ownership. An outer main clip
aligns paint to device pixels after layout without rounding logical content
geometry. Loaded/layout subscriptions detach on unload and template release.
The footer likewise keeps its logical wrapper fractional while a separate
native template border rounds the decoration. Original caller footer content
remains in its own retained slot, with its own layout and binding values.

The catalog's [Monitoring and Editorial demos](workspace-demos.md) keep their
fictional state producers inside `Orchard.Catalog.Demos`, outside the public
library and NuGet API. Keyed views retain original posts, pages, selected controls
and editing fields. Suite selection emits a consumer request; the catalog mounts
the other cached fixture. Inline discard decisions guard dirty navigation.
There are no application services, network requests or persistence paths.
The hidden test launch branch runs before constructing or activating MainWindow.

AppShell keeps original caller slots in stable text/template presentations and
separates the passive Main landmark from the outer shell. Its flow layout uses
intrinsic header/body/footer measurements; inner wrappers absorb free height.
The default minimum follows the real XAML viewport, with caller local values,
bindings and inherited style setters taking precedence. Main padding uses the
source header truthiness/token rule and remains separate from actual header
height. Observers disconnect on unload. See [the API and acceptance](app-shell.md).
Stable slot presentations prevent redundant template/content assignments from
unloading unchanged controls. Minimum and outer-inset changes invalidate the
private flow's measurement dependency; arrange only places measured children.
Owned scalar text remains visible to native content peers while original
controls/templates retain their own semantics.

Select and HeaderSelect reuse the existing native table chooser's input,
typeahead, selection automation, scrolling and popup lifecycle. Their private
appearance supplies the source's size, icon, paint and compact header recipes;
legacy table appearances keep their existing templates and colors. A small
dependency-free model separates the original value prop from internal history
when an ordinary empty Select becomes uncontrolled. Controlled requests never
assign the caller's value. Option views are retained by original value and
occurrence while current original option objects remain event payloads. Header's
synthetic placeholder is component-owned. See [the API and acceptance](select.md).

FeedItem composes the existing Avatar and Tooltip, retaining both across live
metadata updates. Original category sequences and source URLs stay with the
consumer; display normalization is separate. A guarded native Hyperlink peer
routes the original source string through consumer commands/events. Internal
clock injection makes timestamp boundary tests deterministic without changing
OS time or adding a product timer. Owned text slots add opt-in nowrap and
emergency word wrapping; existing consumers keep their prior wrapping mode.
Collection, command, brush and host observers disconnect on unload. See
[the API and open acceptance](feed-item.md).
The nowrap RTL path keeps the measured bidi-fragment allocation. A Libre Franklin
timestamp around an LTR time is 1/64 DIP wider than its combined unrounded
advance; direct Win2D and packaged XAML checks bind that distinction to the
original browser. Ordinary wrapping keeps the existing whole-span measurement.

FeedColumn owns its header and native scrolling viewport while retaining the
original caller filter/body objects and template models. Header updates reuse
slot presentations and leave the viewport instance and offset intact. Its
45-degree gradient retains premultiplied Oklab through three source stops and
encodes only at the raster boundary, using the existing bitmap approach.
Legacy rgba stop alphas first use the source browser's 8-bit representation.
Brush observers and host subscriptions detach on unload; owned gradient and
shadow resources clear with native lifecycle. This control adds no feed service
or filtering state. See [the API and acceptance contract](feed-column.md).

HoverCard uses an inline panel whose desired size is its original trigger's
size. Its absolute content surface stays in the same native visual tree, so
ancestor clipping remains effective. Pointer handlers use the actual topmost
native hit within that scene, retaining the source's literal gap behavior.
The private surface's opacity follows the shared CSS transition clock while
placement and hit testing update immediately. It adds no focus or click policy.
The native control defaults to sibling layer 50; caller stacking values take
precedence, and native ancestor stacking scopes remain explicit. Original
trigger/content models and controls use the shared retained slot presenter.
See [the API and remaining runtime limits](hover-card.md).

Popover keeps original trigger and body slots separate from its native Popup.
A nullable controlled value remains independent of internal default history;
requests do not write caller state. The shared placement model applies source
collision shifting/flipping and device-pixel rounding. Intrinsic measurement
and presentation updates run outside the layout callback, with invalidation
when caller content changes. Native popup roots are registered by XAML root
for nested ownership and dismissal. Input subscriptions, presenters and owned
popup resources disconnect on template retirement or unload. See
[the public API and verification limits](popover.md).

Theme Toggle derives from the existing Icon Button, which is now extensible.
It retains native Button commands/input and a guarded Invoke peer while a
consumer-owned mode drives the source labels, tooltip and Lucide icon. Requests
never assign the mode DP or alter a root theme/storage. The demo accepts requests
on its own preview root. Source Tailwind 4 icon colors have dedicated shared
theme brushes; caller content/templates and metadata are retained.
The tooltip default lives in the native palette style, below local values,
bindings and caller setters. A per-control theme resource resolves the default
object independently when a style is shared. Native precedence restores it
after a null local value is cleared, including when no effective-value callback
fires. Empty styled tooltips remain absent under WinUI's setter rules. Default
action naming likewise respects an explicitly empty style/local name, even when
the caller supplies textual content. Clearing/removing overrides restores the
current mode's defaults. Guarded hidden-window diagnostics exercise the real
packaged XAML tree and leave foreground input and visible rendering unverified.

Slider keeps consumer props separate from the effective range input. Decimal
arithmetic aligns steps before requesting a value; requests leave the original
DP/binding intact. Its native Thumb, track input and automation peer share that
range model. The fill separately preserves the source's last valid inline
percentage, including invalid-value history. Owned labels use the shared font
measurement bridge; native foreground verification remains a separate gate.
Plain `BleeckerContentText` slots now use the combined inline advance already
used by ProgressStack. Summing separately rounded fallback/script runs added
1/64 DIP to a multilingual Slider label; whole-span measurement matches the
frozen browser and remains distinct from rich content's separate runs. Slider
naming also honors explicit empty local or inherited style setters. Guarded
package checks verify source geometry/paint properties, retained fill history,
controlled range requests and native resource/binding lifecycle without input.

PanelLayout and PanelColumn share an internal track allocator and child host.
Stable native slot panels retain each caller UI element across growth/size
updates. The horizontal host owns one native ScrollViewer; columns clip their
vertical overflow and leave body scrolling to each Panel. Growth observers
detach on unload and reconnect on remount. No application service or product
state is introduced by these composition controls.

Reuse project publish directories and the current consumer outputs as described
in [artifact storage](artifacts.md). Older evidence paths may have been removed
by the user-requested 2026-10-17 reset; consult the current checkpoint for
regenerated deliverables rather than assuming historical files still exist.

## Temporary Actions trigger pause

Automatic push runs are temporarily disabled at the user's request to conserve
Actions capacity during parity implementation. Pull-request validation and
manual `workflow_dispatch` remain available. Pushing to `main` does not provide
new hosted verification evidence while this pause is active. Continue local
validation and report native ARM64/hosted gaps explicitly. To restore automatic
push validation, add `push: { branches: [main] }` under the workflow's `on` map.

## Capture persistence

Catalog capture persistence swaps BGRA channels directly when every alpha byte
is 255. The channel swap checks and processes four pixels at a time with the
portable .NET 10 `Vector128` API when accelerated, and uses the same scalar
operation for remaining pixels. Any transparency retains the existing Windows
`SoftwareBitmap` conversion and its premultiplied-alpha rounding. The PNG encoder, metadata and
compression remain unchanged. File writes finish synchronously before the
capture task returns; input and presentation still use their explicit native
gates.

BGRA readback and RGBA conversion each retain at most one idle, exact-size byte
buffer. Scoped rentals keep overlapping captures independent and return storage
on errors. Readback and conversion fill every byte before validation or encoding;
no pixel data escapes the rental. Size changes replace the idle buffer instead
of accumulating a cache by dimension. The unchanged PNG encoder receives the
same complete arrays.

Bitmap and composed-screen blank checks share a bounded byte lookup. The check
still requires nine distinct non-alpha channel values, exactly matching the
previous LINQ predicate. Empty reference canvases retain their separate exact
opaque-color validation. Bitmap profiling records `bitmap.validation` separately
from rendering and persistence. Phase receipts also record process-wide garbage
collection counts and cumulative pause-time deltas; these diagnostics add no
dispatcher turn or input and do not change deadlines.

The `brush-alpha` diagnostic retains all four frozen PNG/RGBA cases and adds
36 opaque comparisons with the previous Windows conversion/write path, plus
36 alternating opaque/transparent comparisons with changing pixels at each
size to exercise reused storage. It checks complete files immediately after
return, exact PNG bytes, decoded RGBA
and write-error propagation. Run `scripts/verify-capture-persistence.mjs` against
its output. Paired persistence timings are diagnostic; the complete native
sequence must still pass its unchanged watchdog, input and protected-glyph
checks before a release.

The `48d8fd3` x64 replay passes all 156 phases in 471.9537397 seconds with every
prior PNG, input gate and protected glyph hash retained. Across the same
146-phase prefix as the preceding timeout, buffer reuse cuts generation-2
collections from 366 to 227 and GC pauses from 28.316 to 21.458 seconds. Both
architectures build and the isolated package consumer passes x64 runtime and
ARM64 build checks; native ARM64 execution remains unverified. See the current
validation checkpoint for immutable package/catalog hashes and remaining gaps.

Native cursor acceptance requires a Windows session with an available mouse.
On 2026-10-17, this host's `GetSystemMetrics(SM_MOUSEPRESENT)` and mouse-button
count both reported zero. Routed mouse input arrived and WinUI selected Hand,
but `GetCursorInfo` reports a hidden global cursor with a null handle. An
experimental balanced UI-thread display-count scope did not restore the native
cursor and was removed. After the user enabled the Bluetooth mouse, the
unchanged Activity Item cursor assertions passed both themes. Connect or enable
a physical/virtual mouse before cursor acceptance. Activity Item failures now retain
both the global observation and the actual WinUI pointer-source selection.

Reference text resolves and validates each of Orchard's five immutable bundled
font assets once per application process. The bounded cache stores validated
file locations; it does not cache shaping results or consumer-supplied fonts.
Font fragments, metric calculations and glyph allocation remain unchanged.
Other font URIs retain validation on each resolution.

The ProgressStack Unicode candidate separately opts into inline glyph-position
measurement and CSS line breaking. Shared font-run selection and the existing
glyph-width renderer are extracted into helpers; the previous per-run and
whitespace-joined width contracts remain available to their existing callers.
ProgressStack alone selects the captured Microsoft YaHei CJK punctuation face
and measures fragments against their shaping-run positions. The Windows-only
`Orchard.Text.Tests` runs the production helpers without creating a window or
changing desktop input. This verifies text measurements and line decisions;
visible native layout, complete raster comparison and package replay remain
separate gates. See [ProgressStack](progress-stack.md).

ProgressStack also keeps each TextBlock's accessible name tied to the logical
label/value. Its explicit line layout may insert newlines and display a taken
soft hyphen as U+2010; those paint details remain in the native TextPattern,
without replacing the source name. External UIA snapshots and retained-provider
Name events exercise this boundary in both themes. Narrator and text-range
navigation remain separate acceptance checks.

The ProgressStack panel and background boundary test share item shrink and
legend placement through `BleeckerProgressStackLayout`. The test starts with
consumer item specs, computes native text minima/maxima, allocates widths and
wraps lines, then compares all 212 source layouts without supplying browser
widths to the allocator. The panel still obtains actual heights from XAML
measurement; the background test uses the source's explicit 16px line height,
so it does not replace visible runtime verification.

Inline glyph measurements index sorted UTF-16 cluster starts across every
shaping run, with cumulative 16.16 advances and original run-local positions.
Run-transition counts distinguish a fragment in one run from a fragment that
crosses runs, even if characters share an offset. Binary searches select the
endpoints, preserving the existing rounded-position rule within one run and
the combined-advance rule across runs. A per-text offset table maps logical
break positions to painted text after interpreting zero-width controls. This
keeps long Unicode min-content and wrapping queries from repeatedly scanning
all glyphs or materializing text prefixes. The allocation regression runs
without a window; it is not a rendered collection-performance claim.

The independent consumer's `--verify-hidden-progress-stack` diagnostic creates
an unactivated WinUI window with the normal Orchard resources. Controls really
load and apply their templates, and XAML supplies their actual measurements.
It verifies that both WinUI and Win32 report the window hidden and that its
process never owns foreground input. The visible consumer path remains
unchanged. `progress-stack-contract.mjs` shares only the frame comparison;
the original visible verifier still requires its lifecycle and complete PNG
checks. The hidden host additionally captures complete 640 by 400
`RenderTargetBitmap` PNGs without activating the window. Its audit verifies
their hashes and compares every RGBA pixel against the frozen source, with
zero masks or tolerance. Raster differences remain open. Separate receipts
exercise reorder, remove, hide and clear on the same observable collection,
checking original collection/item identity and source geometry. Neither these
receipts nor offscreen pixels establish visible presentation, focus, input
or component acceptance.

The default ProgressStack track stores the source's exact `.2` alpha in its
color byte (`51/255`) with brush opacity one. This avoids the channel
truncation observed with an opaque color and fractional brush opacity. The
change lives in generated XAML/PRI resources; an unchanged assembly hash
does not establish unchanged visual resources. A separate ten-frame paint
probe compares full RGBA PNGs in the same 640 by 400 source/native context,
including transparent and contrasting backgrounds. Complete component
captures and smaller-context raster limitations remain separate evidence.

On a desktop without an attached mouse, Windows MouseKeys can provide the
virtual mouse before a new catalog process starts. The 2026-10-17 scoped
Activity replay passes both unchanged native cursor gates in this environment.
Validation snapshots/restores the configurable MouseKeys flags and speed
settings, while Windows owns the read-only button-selection/mode flags. The
library itself does not alter this OS setting. See Microsoft's
[MouseKeys contract](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-mousekeys).
## Platform ownership

The shared chart renderer reconciles lifecycle events with current native
focus. A quick reparent can route `GotFocus` before `Loaded`, and an old
`LostFocus` can arrive after focus was reacquired. `Loaded` clears the previous
connection's interaction state, then initializes the current focus owner before
rendering. `FocusManager.GetFocusedElement(XamlRoot)` supplies that owner,
matching the normal input-ownership boundary. Stale focus/blur notifications
cannot activate or deactivate a chart against the current keyboard target.
The existing late-`Unloaded` guard protects a newly mounted connection's
observers/effects. Data identity, real blur, remembered-key semantics, pointer
priority and genuine-unload resets remain governed by their existing contracts.

Focused `-Component chart-load-focus` checks Bar's category channel and Sankey's
node channel in both themes. It requests native focus during Loading and after
immediate reparent, records actual event order and verifies first-record UIA
inspection plus owned physical Right navigation. Genuine blur is checked too.
`scripts/verify-chart-load-focus.mjs --native <run> --output <audit.json>` audits
all 24 cases and input ownership. Optional `--before <old-run>` verifies the
captured 12 failing eager cases. This check uses the unchanged two-second
presentation/input deadlines. The workflow registers it for both runtime
architectures; push triggers remain paused.

Native input sessions restore the original cursor position through one absolute
virtual-desktop SendInput move, using the same coordinate calculation as fixture
movement. SetCursorPos moved the Win32 cursor without updating WinUI's cached
input position in the chart sequence; later layout-generated events incorrectly
activated a Sparkline at the previous button's location. Recorded native/XAML
coordinates and the passing sequence are documented in [Sparkline evidence](sparkline-reference.md).
This does not change product cursor publication, expected cursor handles,
pointer target bounds or settling deadlines.

The full catalog smoke process has an eight-minute overall watchdog for the
registered Light/Dark suite, including 240 additional required Pie/Donut and
Sparkline pointer actions. Focused modes retain their six-minute watchdog.
Per-state settling deadlines, real-input/presentation gates and architecture
checks remain unchanged. A deadline expiry terminates that owned test process
and fails verification; it does not count as runtime acceptance. The suite
watchdog grows with verification workload and is separate from the measured
collection-performance budgets. Report actual elapsed runtime; the increased
watchdog is not a speedup and does not reclassify earlier timed-out runs.
The [current native checkpoint](native-validation-checkpoint.md) records the
complete passing sequence and remaining acceptance gaps.

The contextual-help keyboard-entry fixture settles its Breadcrumb target
before physical pointer movement. It observes actual rendered geometry over
distinct rendering times and the ancestor viewports' intermediate/final
notifications, retaining the existing two-second deadline. Its diagnostic
records bounds and offsets. Native moves also record bounds after input and
require the actual pointer to remain inside both the selected and current
target bounds. A later layout shift now fails that boundary directly instead
of being mistaken for missing hover delivery.

The shared native-input session and Switch fixture invoke hosted welcome-window
preparation only when `CI=true`. Local runs acquire the catalog's own window
without invoking that CI-only operation. The preparation helper still rejects
non-CI callers; this keeps its foreign-window mutation restricted to isolated
hosted runners. All foreground, hit-target and actual-input checks remain active
locally.
The independent package consumer uses the same transient Alt press/release
when Windows rejects its initial foreground request. Its two-second acquisition
deadline includes that preparation. It records the actual foreground window
before and after, and still refuses component keyboard input until its own
visible window owns foreground input.
Composed captures likewise work locally, but require the foreground window to
belong to the running catalog process, remain entirely on its monitor and pass
the existing occlusion checks. They cannot capture another application's window.

Orchard is the Windows implementation of Gaulatti’s Bleecker design language.
It uses WinUI 3 and .NET, not an embedded browser. Sabella is the Apple library;
Linux has a separate future implementation. Shared branding and design tokens
do not imply shared platform control code.

The reusable library has no newsroom feature state, API clients, Cognito
configuration, credentials, SSE subscriptions, or application navigation.
Those belong to the future Windows newsroom application. The catalog is a
development executable and is not another user-facing newsroom product.

## Pinned foundation

| Dependency | Pin / owner |
| --- | --- |
| .NET SDK | `10.0.401`, `global.json` |
| Windows App SDK | `2.5.1`, `Directory.Packages.props` |
| Windows SDK build tools | `10.0.26100.7175`, `Directory.Packages.props` |
| MSTest | `4.5.1`, `Directory.Packages.props` |
| Bleecker tokens | Exact source commit/hash in `tokens/source-lock.json` |

The projects originated from Microsoft’s WinUI C# app/library templates
(`Microsoft.WindowsAppSDK.WinUI.CSharp.Templates` `0.0.7-alpha`). The template
package is a generation tool, not a runtime dependency; Orchard uses the
stable Windows App SDK pin above. The catalog replaces the template’s generic
page, assets, and packaging configuration.

## Token and control contracts

`scripts/generate-tokens.mjs` transforms the pinned platform-neutral source
into JSON, immutable C# dictionaries, and XAML resources. It verifies the
source hash and rejects unsupported color representations. Unknown color
schemes throw; no alternate palette is substituted.

Light maps to Bleecker’s light palette. WinUI’s `Default` theme dictionary
contains Bleecker’s dark palette. High contrast maps semantic resources to
Windows accessibility colors, including highlight and highlight text for
primary actions. Brand colors must not override the user’s high contrast
palette. Generated button styles and the foundation heading/body/button styles
disable WinUI's automatic text recoloring with `HighContrastAdjustment=None`
because their resources already supply system-aware pairs. This prevents a
primary-action label's mapped HighlightText foreground from being replaced by
white text on a dark backing. Resource completeness is tested; the live native
transition and paint verification is tracked in [contrast verification](contrast-verification.md).

Initial explicit styles retain WinUI’s native control templates, keyboard
behavior, focus handling, automation peers, and button state machines. Button
normal/hover/pressed/disabled brushes use the Bleecker palette; state opacity
is a Windows adaptation documented in the generator. Input styles provide
the baseline font, geometry, and surface. Full input/flyout state customization,
menus, tables, rich editors, notifications, navigation, and newsroom-specific
components are subsequent component work.

Primary and secondary button styles explicitly bind Background, Foreground and
BorderBrush to the generated theme resources. Inheriting these setters from the
SDK style can resolve the normal-state brushes in the SDK dictionary scope and
retain Windows accent colors. The native templates and their state machines remain
inherited; the published runtime regression checks the actual content presenter’s
normal, hover, pressed and disabled color/opacity in light and dark appearances.
These checks exercise visual states programmatically; physical input, Narrator and
Windows contrast verification remain separate acceptance requirements.

Use `ThemeResource` for color consumers and `StaticResource` for typography and
geometry. Load `XamlControlsResources` before Orchard resources. Font files
are copied to `Orchard/Assets/Fonts` in the consuming app output.

Disabled Input and Checkbox pointer behavior uses an internal observer shared
per XAML root. It queries the native pointer and performs at most one root hit
test per observation, then applies each control's own bounds, visibility, ancestor
clipping and occlusion. Checkbox supplies its rounded 20px box and uses the
browser's 1×1 pointer sample; Input currently supplies its control bounds.
Each control detaches on enable/unload, and Checkbox also detaches on
template replacement. One thread-local cursor owner prevents competing controls
from restoring stale overrides. The displayed override belongs to the root's
InputPointerSource, rather than the UI thread's Win32 cursor cache. Each root
retains cursor objects published by WinUI while excluding its own override
object, so release can restore the latest producer selection, including resource
cursors. Stationary enabling selects the control's explicit cursor. Routed
pointer observations coalesce one queued refresh after native cursor dispatch;
layout/render observations continue to handle stationary geometry changes.
Physical cursor gates require GetCursorInfo's visible global cursor handle.

The catalog keeps `EnableMsixTooling=true` and explicitly enables
`AppxGeneratePriEnabled` for the SDK’s application resource-index build
pipeline. `WindowsPackageType=None` still makes the app
unpackaged. Removing the tooling property can leave a compiling executable
without its application resources; CI explicitly requires the published
`resources.pri` before attempting a launch.

The catalog explicitly names its index `resources.pri`, omits the library-style
initial path prefix, and uses the SDK's default embedding of compiled XAML in
that index. Disabling XBF embedding creates references to loose files that the
unpackaged publish pipeline does not copy. These SDK properties keep
`ms-appx:///App.xaml` and the
catalog’s resource paths aligned with the unpackaged runtime lookup contract.

## Scaffold acceptance criteria

1. `Orchard.sln` contains a reusable library, a runnable catalog startup project,
   and tests, with Debug/Release x64 and ARM64 configurations.
2. A clean Windows restore/build compiles the solution without sibling
   repositories, credentials, or production services.
3. C# and XAML reproduce the pinned Bleecker palettes, geometry, and fonts;
   generated outputs cannot drift unnoticed.
4. The native catalog starts, resolves Orchard resource dictionaries and font
   assets, lays out controls, responds to button automation, accepts input,
   and changes between light, dark, and system appearance.
5. Runtime captures produce nonblank light/dark PNGs for visual inspection.
6. Setup, consumer resource paths, provenance, and verification limits are
   documented alongside the code.

## Verification

`samples/Orchard.Catalog/catalog-coverage.json` registers library styles and
public controls against catalog XAML fixtures. Contract tests discover styles
and public class declarations from library sources, compare the registration
sets, and require each registered fixture to actually reference/instantiate its
component. Registration proves fixture presence; it does not establish variant,
interaction or visual acceptance. Those remain tracked in the parity inventory.

The GitHub workflow builds on `windows-2025`, runs MSTest contracts, publishes
the complete self-contained x64 catalog, and launches it with
`--smoke-output`. The catalog checks theme colors, font selection and presence,
native button invocation, input, disabled state, layout, and theme reset. It
renders the actual XAML tree using `RenderTargetBitmap` and writes PNGs and a
structured result report, including initialization failures before the window
loads. A render is also captured before theme assertions, so an incorrect
control color leaves visual evidence for investigation. Failure exits nonzero; the workflow never treats a
build as runtime proof.

The `arm64-runtime` job uses GitHub's native `windows-11-arm` runner, publishes
the ARM64 catalog and runs the same fixture matrix plus the independent ARM64
package consumer. Runtime reports contain process architecture, OS architecture,
Windows/.NET versions and catalog display/text scale and animation preference.
`smoke-catalog.ps1 -ExpectedArchitecture Arm64` requires both process and OS to
be ARM64; the x64 job similarly requires X64. The package verifier requires the
selected process architecture and requires an ARM64 OS for ARM64 runtime work.
The separate `arm64-build` job retains cross-compilation coverage on x64.
GitHub documents the runner and free public-repository availability in its
[hosted-runner reference](https://docs.github.com/en/actions/reference/runners/github-hosted-runners).
The first native ARM64 runtime run and the repeat with fractional Progress
geometry passed the catalog and independent consumer. Reports prove native
process/OS architecture; see [runtime evidence](runtime-evidence.md) for the
retained captures, hashes and remaining platform acceptance.

Reproduce on Windows:

```powershell
dotnet publish samples/Orchard.Catalog/Orchard.Catalog.csproj -c Release -p:Platform=x64 -o artifacts/catalog
./scripts/smoke-catalog.ps1
```

The full catalog process has an eight-minute watchdog. Run motion sampling without
concurrent compilation; an overloaded UI thread can miss an entire short
transition. Field-help checks retain intermediate-frame assertions and write
per-frame color/scale evidence before requesting static captures.

The `orchard-windows-x64` workflow artifact contains the runnable output,
test reports, and native renders. The smoke check exercises automation
providers, not physical mouse/keyboard interaction. Font asset presence and
the selected family URI are checked; pixel-level typeface identity is not
asserted. This scaffold gate does not establish live-feed performance,
rich-document editing quality, screen-reader behavior,
or finished component parity.

## Independent package verification

`dotnet pack` includes the compiled library/resource index, XAML, one copy of each
bundled font, OFL licenses, Bleecker attribution and a package README.
The managed library targets AnyCPU while the applications select x64 or ARM64.
The package verifier inspects the PE header and rejects an architecture-specific
library in the common NuGet `lib` directory.
`buildTransitive/Gaulatti.Orchard.targets` copies font assets to the same
`Orchard/Assets/Fonts` URI paths for package consumers as project consumers.

`scripts/verify-package.ps1` checks the archive, creates a new consumer outside
the checkout, uses a fresh isolated NuGet cache, restores the exact package via
source mapping and verifies its SHA-256. The consumer has its own pinned SDK
configuration and package references. It publishes and runs a small native shell
with all six foundation styles, tests light/dark colors and button invocation,
checks bundled fonts/licenses, and captures native renders. CI retains the
package, runnable consumer and reports under `artifacts`. Each invocation removes
its own temporary source, feed, build workspace and isolated package cache in a
`finally` block, including restore/build failures and `-BuildOnly` returns.
`cleanup.json` records the generated workspace path and confirms its removal;
that path is diagnostic metadata, not a retained checkout. Cleanup refuses paths
outside the exact generated child of the temporary directory and reparse-point
roots. A cleanup failure fails verification rather than silently accumulating
another cache. Package isolation, source mapping, SHA-256, architecture and native
runtime checks remain required.
Successful `result.json` reports are written only after cleanup has completed;
a cleanup failure cannot leave a new successful verification report.
`scripts/verify-package-cleanup.ps1` also invokes the real verifier with a
forbidden project reference and a malformed project that fails `dotnet restore`.
Both must propagate their expected failure, retain a cleanup report, leave no
temporary consumer directory and produce no successful result. These regressions
run after package verification in all three Windows CI jobs.
Local failure regressions pass in `artifacts/package-cleanup-regression`; the
fresh ARM64 `-BuildOnly` consumer also publishes successfully and removes its
cache in `artifacts/package-consumer-cache-cleanup-buildonly`. The local native
x64 consumer restores and publishes, then fails its existing foreground guard
while Windows Terminal remains foreground. Its `cleanup.json` still confirms
removal, and the failed runtime report remains in
`artifacts/package-consumer-cache-cleanup`. No foreground assertion is relaxed.
CI also builds the solution and clean package consumer for ARM64 on its x64
runner. `-Platform ARM64 -BuildOnly` records `runtimeVerified: false`; it never
counts cross-compilation as ARM64 runtime acceptance. Run the same command
without `-BuildOnly` in a Windows ARM64 session to exercise that runtime.
The native ARM64 workflow runs this command without `-BuildOnly` and retains
the runnable catalog, consumer, package, captures and reports in the separate
`orchard-windows-arm64-runtime` artifact. These runtime checks cover the
implemented fixtures; they do not establish full visual or platform acceptance.

This verifies the existing foundation through a package boundary. The consumer
will need additional reusable components and representative compositions as the
inventory is implemented. It does not prove full parity or a published NuGet
feed release. The [component-library scope](library-scope.md) governs acceptance.

The public repository’s wiki was unavailable during initialization. Setup and
architecture documentation therefore live in this repository; no wiki remote
or wiki content was fabricated.

## PageFrame layout defaults

PageFrame's typed width and spacing recipes populate native `MaxWidth` and
`Padding` defaults. Caller local values, bindings and style setters take precedence;
recipe updates do not replace their producers. Clearing an override restores the
current recipe. `XamlRoot.Changed` updates viewport spacing independently of the
frame's own parent width; subscriptions detach on unload and resume on remount.
Content and native templates/selectors stay on the normal ContentControl path.
The source has no intrinsic loading, error, selection or motion behavior.

The reference fixture represents the browser's flat filler with a fractional
logical Grid and a separate native Border paint part snapped to physical pixels.
This matches the source CSS background while retaining its DOM box dimensions;
PageFrame never changes consumer rendering. Exact screen capture compares every
pixel to the original source PNG. A failed exact comparison saves the actual raw
capture for diagnosis and still throws; a saved image alone is not success.
The generic nonblank capture guard is unchanged. See [PageFrame](page-frame.md).

## DetailLayout content and scroll ownership

DetailLayout uses a passive main/rail panel and shared `BleeckerContentText`
presentation. `BleeckerReactContent` centralizes scalar presence and display
formatting previously owned by Field; Field's existing typography and layout
contract is preserved. Only DetailLayout requests stretch alignment of rich
content. Caller elements remain in main-then-rail tree order while track
coordinates implement logical rail placement. The pure model follows source
breakpoints, minimum track sizing and Chromium's float remainder allocation.
Explicit templates and selectors receive the original model object; source
scalar formatting applies only when no native template supplies presentation.

Sticky positioning owns a translation on the rail group, leaving child
transforms untouched. It observes the nearest ScrollViewer/ScrollView and the
XamlRoot. Unload, template replacement, absent rails and disabled sticky detach
observers; remount finds the current viewport. Rail translation is clamped by
the layout's measured container height. Native fixtures check both scroll
control types and owner replacement. See [DetailLayout](detail-layout.md).

## Panel header, slots and scrolling

Panel keeps a fixed header above one native ScrollViewer. Its pure header
model allocates the status dot, optional handle/count and an uppercase title
with the source's automatic minimum width. Native overflow clipping preserves
oversized header content while the body takes the remaining height. Six slot
presentations are retained by producer identity/template, so unrelated header
updates do not reattach caller controls or reset scroll position. The bounded
slot map is cleared with its owned template parts.

The viewport uses native focus traversal to detect focusable caller content.
Plain overflowing content gets a viewport Tab stop and native scrolling keys;
editable content retains its normal focus path. Brush observers and animation
callbacks detach on unload, and template replacement disposes owned cursor
resources. Theme paint is centralized in Panel.xaml. Source border timing is
200ms/default easing and handle foreground is 190ms/premium.

`BleeckerReferenceText` now keys intrinsic measurement by numeral alignment.
Its tabular Win2D typography retains explicit kerning; a windowless regression
matches every frozen Panel count, including infinity, against the browser.
Ordinary text keeps its prior shaping path. Raw composed images are SHA-256
bound to their native report; the audit compares every full RGBA frame and
records unresolved raster differences without masks or resampling. See
[Panel](panel.md).

## Field metadata ownership

Field retains the caller's native child Control. A short-lived property lease
owns only generated label/ID metadata and temporary error validity; it restores
local values or original bindings when released. Existing description references
remain ordered before owned message nodes. Template detach, unload and content
replacement release these relationships. The label's native peer excludes the
decorative required star. Runtime verification uses a separate UIA3 COM client
because the older managed UIA wrapper does not register IsDataValidForForm and
DescribedBy. Every queried control/reference is restricted to the test's owned
window/process. See [Field](field.md) for API boundaries and acceptance gaps.

## Shared CSS outer shadows and Tabs paint

`BleeckerCssShadow` renders rounded CSS outer shadows through a padded Gaussian
alpha mask and an exterior mask. Date controls retain their original recipes
through `BleeckerDateShadow`; Tabs supplies its own source recipes. Each control
owns and disposes its compositor graph. System theme/motion values are read
without changing Windows settings.

Tabs keeps source-state palette values separate from painted template brushes.
`BleeckerTabAnimation` drives per-property 190ms paint/shadow clocks using the
existing CSS transition model. It subscribes to rendering only while an enabled
transition runs and releases subscriptions on unload/template disposal. Custom
link renderers own their presentation; renderers using the supplied source style
participate in the same paint path.

Bundled text assets cache validated URI locations. Win2D validation can dispatch
nested XAML layout; nested requests use that same URI while the outer validation
finishes. Other threads wait, and a validation failure remains an explicit error.

Shared intrinsic text measurement applies CSS letter spacing outside connected
cursive runs. Win2D script properties identify these runs; whitespace retains
its spacing. This matches native XAML's cursive shaping and avoids allocating
Arabic links narrower than their actual text. Latin-only measurement keeps its
existing path. The Nav Menu text regression binds the measurement to original
browser widths for Latin, Japanese and Arabic labels. See [Nav Menu](nav-menu.md).

Footer, Nav Menu, Select, Collection Filters, DataTable Pagination, Column Toggle
and Select Filter capture their owning UI dispatcher during construction.
Producer notifications check queue access before reading `IsLoaded` or any
other UI state. Read-only motion callbacks enqueue their work directly; an
already-unloaded control does no further paint work. The catalog failure
reporter writes captured diagnostics without enumerating live UI resources,
so an off-thread failure cannot recursively fail while reporting itself.
This follows the [DispatcherQueue thread-affinity contract](https://learn.microsoft.com/en-us/windows/apps/develop/dispatcherqueue).

The shared native producer regression is compiled into the catalog and fresh
package consumer. It raises option metadata, observable collection and original
table notifications on real worker threads, then checks the rendered text,
original item ownership and prepared menu state on the UI thread. It also checks
table replacement and unloaded observer release. It uses an unactivated hidden
window, leaves popups closed and sends no input; it does not establish visible
rendering or component acceptance. See [producer notifications](producer-notifications.md).
