# Command Spotlight source contract

Command Spotlight is a reusable component in the fixed Windows inventory,
within the [component-library scope](library-scope.md). Its pinned Bleecker source
is `src/components/command-spotlight.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`21ec854ef2a06e975e42ec1cbd058e2ed6f67a4e8c1b46bfebc2316b3bc4209e`.
Source preparation is verified and the native preview is described below;
component acceptance remains open. The frozen `tests/Reference/CommandSpotlight.json` SHA-256 is
`12b264b46566b389eff73d6b21e7f32feb465cdc52f2c86ab8379555a49a075e`.
It retains 100 canonical PNGs, 200 settled capture samples, 76 interaction
histories, 122 pinned CSS scan inputs, 1,698 actual bundle inputs, six original
fonts and all eleven producer dependencies. An independent recapture matches
all canonical PNGs and metadata/accessibility/histories without exclusions.
The auditor rejects 84 counterexamples, including actual source/CSS/bundle/PNG
corruption and a wrong-theme PNG with recomputed hashes. Eight source-backed
tests pass in the 450-test portable suite. Browser behavior and screenshots do
not establish native component or physical-input acceptance.

The caller owns ordered original actions, IDs, titles, descriptions, groups,
keywords, shortcut hints, optional icons, hrefs and callbacks. Open state is
controlled only when the supplied `open` value is Boolean; otherwise the
component owns its open state. Every open/close request reaches `onOpenChange`.
Search query and selected action ID are internal. Search trims and lowercases
the whole query, then finds that exact substring in the joined title,
description, original group, keywords and href. It does not tokenize or rank.
Visible groups use trimmed names or `Actions`, in first-occurrence order.
The displayed group heading uses the control's live `Language` for uppercase
casing (for example, Turkish `istanbul` displays as `İSTANBUL`), while its
original group value and automation name remain unchanged.

Disabled actions remain visible but cannot be selected or run. Arrow selection
wraps through enabled actions in original filtered order, which can differ from
the grouped visual order. An existing selectable ID survives live filtering;
otherwise the first enabled ID is selected. Hover selects an enabled row.
Arrow/Enter handling belongs to the search input. The shared modal layer owns
Escape, Tab confinement, background inertness, focus restoration and scrolling.

The default Ctrl/Meta+K hotkey toggles the spotlight. While closed, it ignores
input, textarea, select and contenteditable targets; while open it can close
from those targets. Closing through hotkey, backdrop, Escape or action clears
query even if a controlled caller rejects the close request. The source's
trigger label and keyboard hints have responsive behavior that must be measured.
The measured web trigger below 640px hides the label and each shortcut key but
leaves its `+` separator visible. The user explicitly corrected the native
contract: **hide the entire shortcut hint on narrow triggers**. Frozen source
images retain the stray separator; native fixtures must verify the corrected
hint visibility independently.

Running an enabled action first sends its exact original object to
`onActionSelect`, then runs the action's own callback if supplied. That callback
takes precedence over its href. Its synchronous exception or asynchronous
rejection is logged by the source; closing occurs immediately without awaiting
the result. Without an own callback, href uses internal navigation or a separate
external window. An outer `onActionSelect` exception is outside that catch block.
Native routing/callback adapters must preserve original values and ordering.
The component is UI infrastructure; fictional commands must not introduce
product services or authentication/publishing workflows.

Acceptance requires controlled/uncontrolled requests and query reset behavior,
original callbacks and navigation values, enabled selection and ordering,
grouping/search/empty states, retained live data, modal ownership, responsive
layout, typography/tokens, themes and motion to cross their runtime boundaries.
Source references retain actual PNGs, computed styles, accessibility snapshots
and interaction histories, with independent recapture and auditor
counterexamples. Browser navigation boundaries are explicitly intercepted and
reported. They are not native or physical-input evidence.
Native checks run in hidden windows without physical input. The pointer
heartbeat stays paused and pushes must not trigger GitHub Actions.

The reference harness waits for browser painting before recording settled
interaction metadata. Its separate exit-presence observation records the panel
35ms after dismissal, within the producer's 160ms exit lifetime. Earlier captures
with time/frame waits alone retained inconsistent inherited title/shortcut colors;
their failed comparison remains diagnostic evidence. No source styles, colors or
pixel thresholds were changed to make the histories settle.

Custom source classes still require the producer's CSS build to contain their
utilities. In the measured `pt-8` overlay-class case, class merging removes the
default arbitrary top padding while the source-only CSS scan has no `pt-8` rule;
the resulting top offset is zero. The frozen reference records that actual result.

## Native draft

`BleeckerCommandSpotlight` and its catalog fixture are under development. The
local preview is delivered as `0.1.0-command-spotlight-v2-local` NuGet and a
self-contained x64 catalog archive. Both solution architectures build without
warnings, and both fresh isolated package consumers build. The x64 consumer and
extracted archive replay pass 100 states and 20 behavior checks in both themes,
matching source state/contract data exactly. Eight state/dispatch regressions
bring the portable suite to 458 tests, with 10 Windows text tests passing.
The source auditor rejects 64 corruptions and the package auditor rejects 62.
Both fictional demos pass the shared host checks. The whole narrow hint is
hidden as explicitly requested.

Strict browser/native image equality fails at 29,631,022 pixels/max239, with
zero difference allowed. Source/package equality fails at 351/max5 and
source/archive at 345/max5. These failed gates are retained; no actual keyboard,
pointer, focus or native ARM64 acceptance is claimed. The current source captures
1,617 hidden-window guards and the package 1,593, with zero focus/activation or
hardware input. Source/archive each retain 102 actual PNGs; the package retains
100. The earlier v1 delivery remains historical: the expanded dispatch audit
found and corrected eager callback/href lookup before the outer callback.

The first hidden runtime attempt retained 23 states before its test harness
stopped at the native editor's managed automation-pattern boundary. Its failed
report is retained. The subsequent run follows the real TextChanged path and
records hidden-window guards with zero focus/activation or hardware input.

Declare the control inside the window's `BleeckerModalHost`. Supply ordered
`BleeckerCommandSpotlightAction` originals through `Actions`; an observable
`BleeckerCommandSpotlightActions` collection invalidates filtering/grouping when
its collection changes. Native row labels and metadata observe original object
mutations. Group membership and selectable IDs retain the source's array/query
memo boundary until the query or action collection changes.

`Open = null` uses internal state; Boolean `Open` is caller-controlled.
`OpenRequested` reports each request, including rejected ones. `Query` is internal
and readable; `SetQuery`, `SelectAction` and `InvokeAction` use the live component
input boundary. `ActionSelected` and optional `ActionCommand` receive the exact
original object before reading its current `OnSelectAsync` or `Href`. The outer
callback can add, replace or remove the own callback and change the original
href; dispatch then reads those updated values. An own callback wins over `Href`;
tasks do not delay close. Own failures reach `ActionFailed` and `LastActionError`.
Outer callback exceptions propagate before own callback/navigation/close.

`NavigationRequested` exposes the original action, href and external flag.
Applications handle internal navigation explicitly and set `Handled`; Orchard
does not invent a route or service. An unhandled absolute external URI uses the
Windows launcher. Hidden tests intercept navigation and never launch a browser.
`SurfaceStyle`, `TriggerStyle`, `OverlayStyle` and `OverlayTopOffset` provide native
customization. The default top offset is 12% of window height; the entire trigger
hint and label collapse below a 640px window width.

Native hotkeys use DOM key names such as `ArrowUp`, `Backspace`, `CapsLock` and
`NumLock`. Printable keys are translated using the UI thread's current Windows
keyboard layout and Shift/Caps state, with the UI Events modifier-selection
rules. They do not assume a US punctuation layout. AltGraph-generated characters
do not become Ctrl shortcuts; a Windows Meta modifier remains eligible. Dead
and unidentified keys retain their standard names. Translation uses the
read-only flag of [ToUnicodeEx](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-tounicodeex);
Orchard does not load/activate layouts or write keyboard state. The bridge follows
the [UI Events key-value contract](https://www.w3.org/TR/uievents-key/) and the
[Chromium Windows key-selection behavior](https://chromium.googlesource.com/chromium/src/+/lkgr/ui/events/keycodes/platform_key_map_win.cc).

Hotkey hints use full ECMAScript uppercase mappings (`ß` → `SS`, `ﬃ` → `FFI`).
Hotkey matching and search use the producer's full lowercase mappings and final
sigma context classes; they preserve the original caller values and UTF-16 code
units. `tests/Reference/CommandSpotlightHotkeys.json` freezes 168 logical source
scenarios, eight screenshots, every valid Unicode scalar's case mapping and
contextual classes from the canonical browser `155.0.4283.62`. Both independent
captures match exactly. This contract is separate from CSS language-tailored
typography casing used by other components. Hidden logical/native translation
checks do not establish physical keyboard routing, every installed/custom IME,
or full text rendering acceptance.

The hotkeys preview `0.1.0-command-spotlight-hotkeys-v1-local` passes 168 logical
hotkey scenarios, 38 Windows key-name checks, eight editor-to-original-action
Unicode searches and 26 read-only translation checks for the installed US layout
in source, a fresh NuGet consumer and the extracted catalog. It retains the
100 original states/20 behavior checks and 28 style states. The catalog's
`ß`/`ArrowUp`/`/` cycle and both fictional demos pass. All captured data match
across hosts; images still differ at 2,012/max9 for source/package and
1,758/max37 for source/archive. Browser/native differs at 29,631,020/max239 for
the original states and 20,596/max216 for eight new hint captures. Zero thresholds
remain unchanged. Auditors reject 117 source, 113 package and 21 new source-data
counterexamples; 484 portable and 10 Windows text tests pass, with both
solution/consumer architectures built. These checks do not claim physical
keyboard routing, broader layout/IME acceptance or native ARM64 runtime.

Surface maximum width and trigger height/padding/paint setters in `SurfaceStyle`
and `TriggerStyle`, including their `BasedOn` chains, take precedence over generated defaults. Changing, replacing or clearing
these styles while mounted updates the existing trigger and open surface.
Caller `MaxWidth`, trigger `Height`, `Padding`, `Background` and `BorderBrush`
setters keep their native values when the owner font size changes. Properties
without caller setters keep Orchard's scaled defaults; clearing the styles
restores those defaults. Use the exported styles as `BasedOn` to retain the
Orchard template. Binding either style property remains caller-owned through
refreshes, template replacement and unload/remount.

The catalog's caller-style example cycles compact, wide and default surfaces.
The caller-collection example switches 32/1,000 fictional commands while
retaining the original action objects. Group construction preserves first
occurrence order with ordinal keys. Native refreshes use membership sets, and
reordering uses WinUI's collection
[Move method](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.uielementcollection.move?view=windows-app-sdk-2.0)
to retain the existing visual elements. The hidden collection fixture measures
100/500/1,000 rows in single/unique groups across both themes, including set,
refresh, reverse, filter and clear, with caller icon bindings and lifecycle
counts. Model measurements cover 1,000–16,000 actions. Measurements describe the
tested machine and workload; they do not define a latency SLA or prove complete
collection performance acceptance.

The collection measurements compare the prior hotkey package with the collection
preview. For 16,000 distinct groups, the portable model's median setup time
changed from 552.2734ms to 21.9487ms across three samples; measured setup
allocations changed from 8,546,760 to 8,080,768 bytes. These are model costs,
separate from native rendering costs. The 12 native datasets produced 128 caller
icon load/unload events during reversal before the change and zero afterward,
while retaining rows, original actions, caller order and icon bindings.

Source-catalog synchronous medians for 1,000 native rows are recorded below.
Refresh uses three samples; reversal uses one. Times include the synchronous
control operation, with settled layout recorded separately in the evidence.

| Theme / grouping | Refresh before → after (ms) | Reverse before → after (ms) |
| --- | ---: | ---: |
| Light / single | 620.68 → 485.48 | 870.09 → 517.12 |
| Light / unique | 693.63 → 710.27 | 1,181.25 → 707.42 |
| Dark / single | 481.93 → 496.34 | 842.51 → 564.81 |
| Dark / unique | 770.82 → 703.11 | 1,178.30 → 816.79 |

Refresh timings remain mixed and native 1,000-row operations remain costly.
The measurements establish the retained-element repair and remove the repeated
group scan; they do not establish complete performance acceptance. Baseline,
source and fresh-package measurements retain their actual assembly hashes and
terminal process records. See
`artifacts/current/command-spotlight-collections-source-comparison-v1.json` and
`artifacts/current/command-spotlight-collections-package-comparison-v1.json`.

Hidden regressions record 14 style states in each theme, including live binding
replacement, default restoration, actual template paint/padding, retained
original actions, template replacement and remount. Closed-trigger captures
verify caller paint directly; open-surface captures verify paint above the modal
backdrop. These checks do not establish
physical hover/focus, arbitrary caller templates or complete scaling acceptance.
The implementation follows WinUI's
[dependency property precedence](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/dependency-properties-overview#dependency-property-value-precedence)
and [ClearValue contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.dependencyobject.clearvalue?view=windows-app-sdk-2.0).

The `0.1.0-command-spotlight-styles-v1-local` preview verifies these 28 style
states plus the original 100 states/20 behavior checks in the source catalog,
fresh independent NuGet consumer and extracted runnable catalog. Both catalog
hosts exercise the compact/wide/default demo cycle and both fictional demos.
All state and contract data match; source/package images still differ at 2,776
pixels and source/archive at 1,624, each maximum channel difference 10 with zero
allowed. Browser/native remains 29,631,022/max239. The style work does not resolve
these raster acceptance gaps. Source/package auditors reject 94/91 corrupted
cases; 474 portable and 10 Windows text tests pass. Both solution and isolated
consumer architectures build, with native ARM64 runtime unverified.

Use `scripts/package-catalog.ps1` with explicit `-AppDirectory`, `-Archive`,
`-ExpandedDirectory`, `-Output` and `-Version` values to create a fresh catalog
archive and byte-check every extracted file. Replay `scripts/verify-command-spotlight.ps1`
and `scripts/verify-public-modal-host.ps1` against that extracted directory before
claiming runtime or demo verification. Each probe requires a fresh output path.

The pinned SDK is Windows App SDK 2.5.1 (WinUI 2.3.9). Implementation consulted the
[2.5.1 release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0#version-251)
and [keyboard event documentation](https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-events).
The [TextBox peer documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.textboxautomationpeer?view=windows-app-sdk-2.0)
documents its native Text/Value pattern implementation. Hidden managed checks
change the real TextBox value and observe TextChanged; they do not claim an
external UI Automation client or physical editing session.

Remaining verification includes broader native interaction/lifecycle coverage,
native ARM64 runtime, complete source image equality, keyboard layout/hotkey
variants, full Unicode edge cases, actual focus/Tab/pointer routing, scaling,
motion, arbitrary caller templates/overlay layouts and collection performance. Background-only test
authorization and all exclusions above remain in force.

## Collection preview delivery

The `0.1.0-command-spotlight-collections-v1-local` preview packages the measured
collection repair. Source catalog, fresh independent x64 NuGet consumer and
extracted runnable catalog each pass 12 collection datasets/84 measurements,
with zero reorder icon lifecycle events. Each also passes 100 original states,
20 behavior checks, 28 style states, 168 logical hotkey states, 38 Windows key
checks, eight Unicode searches and 26 read-only US-layout probes. The catalog
32/1,000-action cycle passes in both themes. Both fictional demos pass the
shared-host checks in the source and extracted catalog. Portable tests pass
486/486 and Windows text tests 10/10; x64/ARM64 solution and isolated consumer
builds pass. Native ARM64 runtime remains unverified. Full source/package
auditors reject 117/113 corrupted cases; collection auditors reject 21 each.

All source/package/archive state and behavior data match. Strict source/package
image comparison fails at 2381 pixels/max8, and source/archive at 3070 pixels/max8;
zero difference remains allowed. The original browser/native comparison fails
at 29,631,021 pixels/max239 and the hint comparison at 20,596/max216. These
failed gates, physical input, broader layouts/IME, caller templates/overlays,
scaling/motion and remaining native collection costs keep acceptance open.
Evidence and prior checkpoints remain immutable in
`artifacts/current/command-spotlight-collections-checkpoint-v1.json`.

## Refresh rendering and initial key caps

Text and shortcut rendering remember their last applied values using weak
per-element state. Each update resolves the current font, brush, size, line
height, language, direction and text-scaling values; identical values avoid
repeating native dependency-property writes. Removed text and key-cap objects
are not kept alive by these tables. Action text, icon content/bindings,
selection and input checks still follow the existing refresh path.

Shortcut updates still walk the current template tree. Newly created caps get
their own dimensions, including after a shortcut changes in place. The hint's
Loaded handler applies the same dimensions after the template first creates its
caps. The previous package fails the new first-load regression. The implementation
uses the [WinUI Loaded contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.loaded?view=windows-app-sdk-2.0)
and [.NET 10 weak-key tables](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.conditionalweaktable-2?view=net-10.0).

The hidden refresh probe records four 1,000-row datasets, three full-refresh
samples each, 21 diagnostic measurements and 16 font/binding/key-cap cases.
Diagnostic measurements invoke private methods through reflection; full-refresh
measurements change the public trigger label. Their source medians are:

| Theme / grouping | Previous package (ms) | Current source (ms) |
| --- | ---: | ---: |
| Light / single | 486.00 | 122.59 |
| Light / unique | 672.65 | 267.00 |
| Dark / single | 491.75 | 114.14 |
| Dark / unique | 670.92 | 293.95 |

Diagnostic text-style updates changed from 272.24ms to 35.16ms and hint updates
from 117.20ms to 2.00ms. These measurements describe the tested machine and
workload. Unique-group refreshes remain costly; complete collection performance
acceptance and a latency SLA are not claimed. Source comparison evidence is
`artifacts/current/command-spotlight-refresh-source-comparison-v1.json`.
The source probe passes inherited/local fonts, live bindings, restoration,
language/direction/text scaling, retained original icons and key-cap dimensions.
The successful v1 baseline records font-only checks; the v2 success report also
requires the key-cap checks. Its font metadata is compared against the earlier
font-only baseline. Initial template-changing style checks preserve original
actions and icons; ordinary font-only updates also preserve the existing row.
The fresh package and extracted-catalog verification below now cover this
source change.

## Refresh preview delivery

The `0.1.0-command-spotlight-refresh-v1-local` preview passes the refresh probe
in source catalog, fresh x64 NuGet consumer and extracted catalog: four 1,000-row
datasets, 12 full-refresh measurements, 21 diagnostic measurements, 16 live
font/binding checks and 16 key-cap dimension checks per host. Current reports
require the v2 key-cap checks; only the explicit font-only baseline reader
accepts the earlier format. Each current auditor rejects 28 corruptions, including
a report downgraded to omit these checks. Each host also passes the full
100-state/20-behavior/28-style/168-hotkey/38-Windows-key/eight-Unicode-search/
26-read-only-layout suite and the 12-dataset/84-measurement collection suite.
The source/package full auditors reject 117/113 corruptions and collection
auditors 21 each. Reorder icon lifecycle events remain zero. Both fictional
demos and the 32/1,000-command catalog cycle pass. Portable tests pass 486/486,
Windows text tests 10/10, both solution and isolated consumer architectures
build, and all 1,360 extracted archive files match their recorded bytes.
Native ARM64 runtime and physical input remain unverified.

Source/package/archive state and behavior metadata match. Strict source/package
image equality fails at 1960 pixels/max8, and source/archive at 2426 pixels/max8.
Browser/native original states fail at 29629594 pixels/max239; hint images at
20596 pixels/max216. Zero difference remains allowed throughout.
The preview improves the measured refresh path and initial cap sizing while
full visual, interaction, customization, scaling/motion, performance and
platform acceptance remain open. Evidence is recorded in
`artifacts/current/command-spotlight-refresh-checkpoint-v1.json`.

## Isolated backdrop paint

The pinned browser renders the backdrop's `color-mix` fill in an isolated
compositing layer. A plain swatch can differ from that layer despite identical
computed CSS. With 45% black over white, the frozen component produces RGB 141,
while the prior native component produces RGB 140. Browser experiments reproduce
the isolated-layer result with blur or layer isolation. Native component
experiments show that removing blur or adding bitmap caching does not repair
the difference.

The backdrop renderer now encodes the source's single-precision mix with
transparent and its isolated layer's byte alpha into a private render brush.
The original theme/caller brush, its color, opacity, identity and binding remain
untouched. Color/opacity mutations and replacement of the semantic palette
brush update the rendered fill. Closing the presentation disconnects the
observers; reopening reads the current source again. This conversion belongs
to the backdrop fill, with the existing blur, transition and input behavior.

The diagnostic uses the same Chromium 155.0.4283.62 as the frozen reference:
150 browser swatches across six compositing contexts, 900 native swatches per
run, and 14 real-component context captures. The source implementation passes
14 live cases covering binding, color/opacity mutations, replacement, retired
sources, close and reopen. Its auditor rejects 25 counterexamples, including
source mutation, stale pixels and report downgrades. Zero pixel thresholds
remain unchanged. Diagnostic brush variants are not product options; the
broader color and raster gates remain open. The fresh full-state and package/archive
validation below now cover this source change.

## Backdrop paint preview delivery

The `0.1.0-command-spotlight-paint-v1-local` package and extracted catalog pass
the same 900-swatch, 14-context and 14-live-case paint audit as the source host.
Both source and package auditors reject 25 corruptions. The public semantic
brush retains its identity, binding and raw color/opacity; only its private
render projection changes. Closed presentations release their observers.

All three hosts also pass 100 full states, 20 behaviors, 28 caller styles,
168 logical hotkeys, 38 Windows key-name checks, eight Unicode searches,
26 read-only keyboard checks, 12 collection datasets/84 measurements and
four refresh datasets/12 measurements/21 diagnostic measurements with
16 font/binding and 16 key-cap checks. Both fictional demos and catalog
collection/style/hotkey cycles pass. Portable tests pass 486/486 and Windows
text tests 10/10. Root and independent consumer x64/ARM64 builds pass; all
1,360 extracted catalog files match the packaged bytes. Native ARM64 runtime
and physical input remain unverified.

At the unchanged zero threshold, browser/native original-state differences
decreased from 29,629,594 to 9873633 pixels (maximum channel delta 239).
Hotkey images still differ at 20598 pixels/max216. Source/package images differ
at 1005 pixels/max8 and source/archive at 453 pixels/max8.
State and behavior metadata match across hosts. These failed image gates,
caller templates/overlays, physical routing, scaling/motion, full collection
performance and native ARM64 runtime keep the component unaccepted. This
preview and the other 111 entries remain in progress. Evidence is recorded in
`artifacts/current/command-spotlight-paint-checkpoint-v1.json`.

## Selected-fill precision

The next source revision preserves the semantic selected-row brush and renders
its clipped Oklab color mix through a private one-pixel premultiplied raster.
Direct brush opacity gives RGB 234/238/243 in Light and 33/55/79 in Dark where
the pinned component produces 233/238/242 and 34/55/80. Merely rounding alpha
repairs those defaults but fails broader colors. Keeping the staged float
sRGB/XYZ/Oklab conversions, premultiplication and byte encoding reproduces the
broader samples without baking an assumed background into the fill.

The renderer observes color, source alpha, opacity and brush replacement while
retaining the semantic brush, binding and original action. Refresh retains its
image; closing releases that image and its observers. Reopening creates a fresh
renderer from the current source. The isolated backdrop retains its separate
paint conversion. Theme tokens and public component APIs are unchanged.

Acceptance for this change requires source-backed pixel encoding, live binding
and mutation/replacement checks, retained raster identity on refresh, observer
cleanup on close, and current source/package/archive regressions. The first
actual-library probe passes 378 source-backed swatches and 20 live cases in 19
PNGs. The full state/style/hotkey, backdrop, refresh/collection and current
package/archive verification below now cover this source revision. This result
does not accept the component or relax any zero-difference image gate.

`tests/Reference/CommandSpotlightSurface.json` retains the two raw source atlases
and 378 Oklab cases selected from 450 captured pixels. The primitive matrix also
covers zero opacity, transparent and partial-alpha endpoints and five
backgrounds. A prior diagnostic showed that serializing computed Oklab styles to
six digits changes a half-byte boundary; the palette atlas therefore uses the
frozen CSS classes and actual root theme variables. Both complete original
Light/Dark component baseline images were independently recaptured byte-exact.

Implementation study used the float operations in
[Chromium color conversions](https://github.com/chromium/chromium/blob/5ff8f5f29e40f76c25053ca909f3ef9abd42fe84/ui/gfx/color_conversions.cc),
[Chromium skcms helpers](https://github.com/chromium/chromium/blob/5ff8f5f29e40f76c25053ca909f3ef9abd42fe84/skia/ext/skcms_ext.cc)
and the corresponding
[Skia color-space definitions](https://github.com/google/skia/blob/c5f99538cbe23771df38b7c3e63a2beb76256681/include/core/SkColorSpace.h).
Those auxiliary snapshots do not establish the pinned browser revision;
acceptance uses its measured Chromium 155.0.4283.62 pixels. Chromium and Skia
notices are distributed with the NuGet payload.

## Selected-fill preview delivery

The `0.1.0-command-spotlight-surface-v1-local` preview passes all 378 swatches
and 20 live cases in source catalog, fresh x64 NuGet consumer and extracted
catalog. Each host retains 19 PNGs and validates binding, color/source-alpha/
opacity mutation, brush replacement, retired-source silence, raster identity
on refresh, image release/observer cleanup and fresh state on reopen. Source
and package auditors reject 28 corruptions each. The codec is covered by four
new portable tests, including opaque-color identity and invalid-opacity errors.

All three hosts pass the full 100-state/20-behavior/28-style/168-hotkey/
38-Windows-key/eight-Unicode-search/26-read-only-layout suite, the 900-swatch/
14-context/14-live-case backdrop suite, 12 collection datasets/84 measurements
and four refresh datasets/12 measurements/21 diagnostics with 16 font/binding
and 16 key-cap checks. Full source/package negatives are 117/113; backdrop
25/25, refresh 28/28 and collection 21/21. Reorder lifecycle events remain zero.
Both required fictional demos and catalog cycles pass. Portable tests pass
490/490, Windows text tests 10/10, root and independent-consumer x64/ARM64
builds pass, and all 1,360 extracted archive files match their packaged bytes.
Chromium/Skia notices and the package README match current source.

With zero differences still allowed, original browser/native pixels decreased
from 9,873,633 to 7832379 (maximum channel delta 239). Hint images still differ
at 20596 pixels/max216. Source/package differs at 716 pixels/max8;
source/archive at 399 pixels/max9. State/contract metadata match across
current hosts. Raster, physical routing, layouts/IME, full caller templates/
overlays, scaling/motion, collection performance and native ARM64 runtime keep
the component unaccepted. All 112 entries and both fictional demos remain in
progress. Evidence is recorded in
`artifacts/current/command-spotlight-surface-checkpoint-v1.json`.

## Shared outer-shadow preview

The `0.1.0-overlay-shadow-v1-local` preview makes the shared overlay shadow
independent of background alpha/kind and excludes its rounded interior.
Source, fresh NuGet and extracted catalog pass 16 shadow cases/32 PNGs and
26 lifecycle cases each, alongside the full Command Spotlight, fill, backdrop,
refresh and collection suites. Menubar, Navigation Menu, Sonner and both
fictional demos also pass. Portable tests pass 492/492 and Windows text tests
10/10; both solution and independent-consumer architectures build.

Strict browser/native pixels remain unequal (7,815,778 differing pixels,
maximum delta 239). Native source/package differs at 57 pixels;
source/archive at 32. No threshold changes or component acceptance
follow from this preview. See the [shared renderer contract](overlay-shadow.md)
and `artifacts/current/overlay-shadow-checkpoint-v1.json`.

## Source-premultiplied shadow tint preview

The local `0.1.0-overlay-tint-v1-local` preview corrects shared shadow paint
premultiplication while retaining the nominal tokens, geometry, blur, clipping
and graph lifecycle. Source, fresh NuGet and extracted catalog pass the complete
100-state/168-hotkey replay plus style, search, Unicode, fill/backdrop, refresh
and collection checks. Menubar, Navigation Menu, Sonner and both fictional
demos pass. Fresh root/consumer x64 and ARM64 builds, 495 portable tests and
10 Windows text tests pass; native ARM64 execution is unverified.

The independent source fixture verifies 12 paint captures and 6,572 covered
pixels; the native fixture adds two unblurred captures and verifies 1,398
covered native paint pixels per host. Its 34-PNG/26-lifecycle auditor rejects
39 corruptions. This paint check does not establish exact blurred-image parity.
Original browser/native Command Spotlight images still differ at
7,769,949 pixels/max239; hints at 20,596/max216; full source/package
at 77/max6, source/archive at 48/max6. These raster differences are diagnostic,
not a public-component delivery gate.
The isolated large light far-only shadow regression is recorded explicitly.
All 112 entries and both demos remain in progress and unaccepted. See the
[shared paint contract](overlay-shadow.md#source-paint-encoding) and
`artifacts/current/overlay-shadow-tint-checkpoint-v1.json`.

## Group heading language

The `0.1.0-group-language-v1-local` package uses the control's live language
for visible group headings. A hidden x64 source catalog and a freshly restored
independent NuGet consumer each pass 24 interaction contracts, including
Turkish and English heading changes in both themes; the caller's group value
and automation name remain unchanged. The shared casing tests pass 10/10 and
the ARM64 catalog builds. Native ARM64 execution and full component acceptance
remain open. Reports are under
`artifacts/current/command-spotlight-group-language-source-v2` and
`artifacts/current/command-spotlight-group-language-package-v1`.
