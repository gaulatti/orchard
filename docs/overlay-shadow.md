# Shared overlay shadow

The [combined near/far attachment preview](overlay-shadow-combined.md) coordinates
both internal GPU layers on the actual component. Source, NuGet-consumer and
catalog-archive replay verify its ownership and lifecycle. Its six original
combined images retain 16 differing pixels; the unchanged public default retains
90,433. It has not replaced the public renderer or established whole-component
parity.

Current scope is the reusable component library and its fictional monitoring
and editorial demos. [The user-confirmed scope](library-scope.md) supersedes
historical Narrator and OS contrast campaigns. Current verification uses
hidden hosts without hardware input or system setting changes. The older
release evidence below remains historical; the inventory currently has 112
entries started, zero pending and zero accepted.

The [near-shadow precision investigation](overlay-shadow-near.md) now proves
48 colored/mask images at zero tolerance through standalone browser, native
offscreen and encoded Composition paths. Those initial standalone results
preceded the internal library previews below; the default renderer description
below remains accurate.
The later [real-component attachment](overlay-shadow-near.md#attachment-to-the-actual-component)
also preserves exact near pixels during 12 retained-graph geometry changes.
Its six combined PNGs retain 145,582 differing pixels, so the full gate remains open.
The subsequent [internal ownership preview](overlay-shadow-near.md#internal-library-ownership-preview)
owns automatic updates and shared resource lifetime inside the library. It is
validated through focused source/NuGet/archive physical-pixel checks and leaves
the default renderer described below in place. The later
[active-radius and PNG follow-up](overlay-shadow-near.md#active-radius-and-diagnostic-png-follow-up)
passes radius eviction, five suspension/restoration transitions and original
near PNG comparisons in source, NuGet and catalog. Full/far raster, broader
geometry/scaling and actual device loss remain open.

`BleeckerOverlaySurface` renders the pinned two-layer Bleecker overlay recipe:
`(0,24,64,-20)` at .28 opacity and `(0,4,14,0)` at .06, both RGB
`(26,55,77)`. Select now uses this surface inside its existing popup border;
Menus, Tooltip, Alert, Notification and Column Toggle already consume it.
The shadow changes paint only. Popup measurement, placement, scrolling,
keyboard stops, modal ownership and consumer callbacks retain their contracts.

CSS defines Gaussian standard deviation as half the box-shadow blur value.
The renderer therefore uses Win2D `GaussianBlurEffect` at 32 and 7 pixels,
with soft transparent borders and quality optimization. The renderer
preserves CSS's front-to-back recipe order: the far (64px) layer is above the
near (14px) layer. Composition collections enumerate bottom to top, so the
native and independent consumer probes check `[14,64]` before sorting reports.
See [CSS shadow layering](https://www.w3.org/TR/css-backgrounds-3/#shadow-layers).
Each rounded caster
retains its token spread, and its Composition visual surface includes the
complete three-sigma margin on every side. A SourceIn color effect applies the
source's premultiplied paint bytes through `ColorSourceEffect.ColorHdr`;
the output sprite has opacity one because the tint already carries alpha.
The public opacity tokens remain .28 and .06. The project already pins Win2D 1.4.0; this adds no
dependency. See the [CSS blur definition](https://www.w3.org/TR/css-backgrounds-3/#shadow-blur)
and [Direct2D Gaussian blur](https://learn.microsoft.com/en-us/windows/win32/direct2d/gaussian-blur).

Composition children extend the paint beyond the layout box without adding
XAML hit regions. Resizing and corner changes update the live caster mask,
source extent and output placement while retaining the loaded effect graph.
Unchanged geometry skips that update. Collapsing the component or a XAML
ancestor, zero sizes, template replacement and unloading detach the visual
and dispose the owned native graph. While loaded, the surface observes its
own and its visual ancestors' `Visibility`; unloading removes those callbacks,
and loading after reparenting observes the new ancestry. Visibility restoration
recreates the graph without replacing caller bindings, brushes or opacity.
This tracks XAML visibility independently of the containing window's visibility;
hidden catalog hosts can still render offscreen. The callbacks use WinUI's
[registration tokens](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.dependencyobject.registerpropertychangedcallback?view=windows-app-sdk-2.0)
and are explicitly unregistered. The repository pins WindowsAppSDK 2.5.1;
the official API documentation currently exposes the 2.0 view, and the local
2.5.1 builds and runtime checks establish this repository's behavior.
Each Composition caster owns its opaque fill and rounded path. Existing
component contrast suppression remains in place. Scaling and ARM64 runtime
remain acceptance work; OS contrast test campaigns are excluded.

`scripts/verify-overlay-shadow-lifetime.ps1` runs the hidden catalog or
independent consumer fixture in `default`, `near` or `far` mode. It checks
three bound paints, self/parent/root collapse, zero dimensions, an empty
template, restoration, reparenting and final cache/subscription cleanup;
private modes also check a hidden template host. Its independent audit reads
the saved physical frames and diagnostic PNGs and requires identical restored
pixels. `test-overlay-shadow-lifetime-audit.mjs` rejects corrupted lifecycle
reports while retaining the original evidence. The pre-fix public baseline
retained 21 resources after collapse. Public/default, near and far modes now
pass in source, a fresh NuGet consumer and the extracted catalog: nine mode
runs, 60 suspension transitions, 27 bound paints and 17,846,400 restored pixels
compared exactly. All 846 prior isolated source capture files and 14
pre-collapse paint files remain byte-identical. The auditor rejects 89
corrupted reports. This scoped cleanup proof leaves the existing far, near
fractional and combined source-image gates open. The local preview is
`0.1.0-overlay-lifetime-v1-local`; it is not whole-component acceptance.

## Background-independent outer shadows

The shared surface casts its opaque border geometry regardless of the caller's
`Background`, including null, transparent, partial-alpha and gradient brushes.
An outside-border Composition clip excludes shadow paint from the rounded
interior. The caller's background object, opacity and binding remain untouched.
The old template mask parts retain their geometry but no longer paint an
additional copy of that background. This follows the
[CSS outer-shadow shape contract](https://www.w3.org/TR/css-backgrounds-3/#shadow-shape).
Layer colors, order, spread, blur, extents and public APIs remain unchanged.

The frozen Chromium 155 reference, `tests/Reference/OverlayShadow.json`, covers
eight background cases in each theme. Source capture proves that all outside
pixels are identical across those backgrounds. The hidden native fixture
captures each case with its shadow attached and detached, verifies the whole
rounded interior away from its antialiased edge, and retains both images.
It also checks binding/opacity mutation, graph identity through resize and
corner changes, and disposal on every unload. Both legacy catalog and independent-consumer shadow
inspectors are invoked directly by the hidden fixture; their geometry/type checks
now cover the opaque caster and outside clip. At the historical outer-shadow checkpoint, native x64 checks passed
16 cases, 32 PNGs and 26 lifecycle cases; its auditor rejected 28 corruptions.
The source/native zero-difference raster comparison still fails: 229,384
pixels differ across the 16 images, with absolute RGBA error 461,738. Native
background compositing and shadow rasterization remain visible in this result.
This focused result does not establish acceptance for all overlay consumers.

```powershell
./scripts/verify-overlay-shadow.ps1 -AppDirectory <hidden-catalog-or-consumer> `
  -Reference tests/Reference/OverlayShadow.json -Output <fresh-output>
node scripts/test-overlay-shadow-audit.mjs <fresh-output> `
  Orchard.Catalog.dll <fresh-negative-audit.json>
```

The `0.1.0-overlay-shadow-v1-local` preview passes the same shadow fixture in
source catalog, fresh NuGet consumer and extracted catalog. All three hosts
also pass Command Spotlight's full 100-state, style/hotkey/search, fill,
backdrop, refresh and collection suites; Menubar 30 states/26 contracts,
Navigation Menu 26 states/18 contracts, and Sonner 66 states/20 contracts.
Both fictional demos and catalog cycles pass. Portable tests pass 492/492,
Windows text tests 10/10, both solution and independent-consumer architectures
build cleanly, and all 1,360 extracted files match their archive bytes.

Original Command Spotlight browser/native differing pixels fall from
7,832,379 to 7,815,778; hints still differ at 20,596. Source/package differs
at 57 pixels and source/archive at 32. All thresholds remain zero.
Broader caller templates/overlays, exact raster, physical routing, motion,
scaling and native ARM64 execution keep full acceptance open. Evidence is
recorded in `artifacts/current/overlay-shadow-checkpoint-v1.json`.

## Source paint encoding

The internal native far-layer candidate and its verification boundary are
documented in the [far-shadow investigation](overlay-shadow-far.md). It has not
replaced the public renderer.

The later far-layer GPU investigation is retained separately in
`artifacts/current/overlay-shadow-far-gpu-integral-v1`. A shader generates the
512-entry mathematical integral table directly on the GPU; all table bytes
match the independent analytic table. Full float arithmetic matches every
physical pixel of the eight pinned far-layer cases, covering both the small
rectangle's general branch and the large rectangle's fast branch. Six original
PNG images also match; the two transparent canvas exports differ at 104 and
572 pixels despite identical physical bytes. Explicit half conversions worsen
the result and are rejected. The probe uploads no source images or readback
pixels as rendering inputs and makes no network requests. It has not changed
the library renderer, so the existing native far/combined gates remain open.

The tint candidate preserves nominal RGB `(26,55,77)` and the public opacity
recipe, while rounding to the source's premultiplied eight-bit paint before
constructing its straight float tint. The observed far paint is RGBA
`(7,15,21,71)` premultiplied; the near paint is `(2,3,5,15)`. A projected
`Color` getter reports straight `(25,54,75,71)` and `(34,51,85,15)` respectively.
The changed straight channel values follow from premultiplication at low alpha;
the design tokens themselves are unchanged. The blur, masks, source extents,
clip, layer order and loaded graph lifetime retain their existing paths. No CPU
bitmap generation or readback is part of this library correction.

`tests/Reference/OverlayShadowPaint.json` retains 12 new source captures and
independently observes 6,572 fully covered pixels across two geometries. All
eight retained layer captures match their earlier bytes. The native fixture
adds two unblurred transparent captures to check 1,398 fully covered paint
pixels against those independent source bytes. Its whole-image comparisons
still include all antialiased pixels at zero tolerance. Select and Date popup
snapshot auditors inspect actual projected tint bytes and sprite opacity;
their geometry checks remain intact. Day/trigger/card shadow recipes are
unaffected. Native report version two has 16 background cases, 34 PNGs, two
paint cases and 26 lifecycle checks.

The local `0.1.0-overlay-tint-v1-local` preview passes x64 and ARM64 solution
and fresh independent-consumer builds, 495 portable tests and 10 Windows text
tests. Source, NuGet consumer and extracted catalog each pass the 16-case,
34-PNG shadow fixture, two paint cases and 26 lifecycle checks; each auditor
rejects 39 corruptions. All 1,398 covered native paint pixels match the source
premultiplied bytes. The first source probe used a publish directory without
loose catalog XBF resources; the complete source replay and all demo checks use
the verified full build output. The archive has 1,360 byte-verified files.

All three hosts also pass the full Command Spotlight, fill/backdrop, refresh,
collection, Menubar, Navigation Menu and Sonner suites. Both fictional demos
pass in source and archive. Collection model costs were freshly recorded by
the passing portable test (15 datasets, 45 samples); the hidden native runs
retain 12 datasets and 84 measurements per host. Latency acceptance is false.

The source shadow image comparison still fails at 189,187 differing pixels and
absolute RGBA error 412,413 (previously 229,384 / 461,738). Original Command
Spotlight browser/native images differ at 7,769,949 pixels/max239; hint
images at 20,596/max216. Full source/package images differ at 77 pixels/max6
and source/archive at 48/max6. Shadow image comparisons across the hosts
record 68/68 byte-identical PNG pairs and 0 differing pixels; raw
paint bytes are compared separately. Every threshold stays zero. Physical input,
scaling/motion, broader templates/IME, full collection performance and native
ARM64 execution remain open, alongside all 112 inventory entries and both demos.
Evidence: `artifacts/current/overlay-shadow-tint-checkpoint-v1.json`.

The initial isolated
compositor experiment improves every tested combined/near light/dark case;
the large light far-only case increases absolute RGBA error from 39,545 to
40,437 while its existing blur remains inaccurate. This residual is retained
explicitly; exact raster and component acceptance remain open.

```powershell
node scripts/reference/capture-overlay-shadow-paint.mjs `
  --playwright-module <Playwright-1.62.1-package> `
  --layer-reference artifacts/current/overlay-shadow-layers-source-v1/layers-source.json `
  --output <fresh-source-output>
node scripts/reference/verify-overlay-shadow-paint-source.mjs `
  tests/Reference/OverlayShadowPaint.json tests/Reference/OverlayShadowPaint <fresh-audit.json>
```

The capture command requires the retained layer reference from the layer-study
checkpoint, pinned Edge 155 and the two original tracked source references.
It blocks network requests and rejects any change to the original source pins.
Catalog archives use the complete x64 solution build output. A no-build
`dotnet publish` output omits loose catalog XBF resources and is insufficient for
catalog delivery; the packager checks for those resources before creating a ZIP.

The initial two-dimensional near-mask hypothesis compares every pixel in both
retained white-mask geometries. Its best A8 rescale/nine-patch model differs
at 28 pixels per geometry, maximum alpha difference one. Direct and centered
paired Gaussian taps retain that result; reversing pass order worsens it.
The source browser revision remains unresolved, and this is diagnostic evidence,
not an adopted renderer or an accepted raster result. Evidence uses
`artifacts/current/overlay-shadow-near-two-dimensional-v{1,2,3}.json`.

The subsequent [near-shadow precision investigation](overlay-shadow-near.md)
resolves those mask differences through paired GPU samples and the observed
two-stage snapping order. All 12 extended source masks match in both browser
texture formats, and eight radius-14 masks match native offscreen and hidden
Composition presentation exactly. The subsequent colored study matches 48
images through offscreen and two encoded Composition paths, with all 15 native
intermediate buffers exact. This remains an unadopted renderer experiment;
shared-layer integration, lifetime, scaling and measured cache/resize costs remain open.

## Unpromoted layer investigation

The earlier October 23 layer study kept its library and preview package
unchanged; the separate source-paint correction above is now packaged. Its frozen browser capture separates far, near and combined shadows,
white coverage masks, and unblurred geometry at both the small fixture and the
actual fractional Command Spotlight dialog bounds. Both recaptured original
source images are byte-identical. Hidden package consumers retain raw
premultiplied BGRA bytes alongside their PNGs, which separates capture encoding
from shadow rendering. Their guards verify no visible window, activation,
focus, popup or hardware input.

The measurements rule out a tempting opacity change: nominal scalar opacity,
byte-packed scalar opacity, and byte-packed tint alpha produce identical PNG
and raw bytes in all 26 triplets. Pixel snapping alone, antialiased mask variants,
and an unconverted floating-point color surface each regress some colored cases.
They are retained as experiments and do not change the renderer.

An analytic rounded-rectangle coverage model matches all four source unblurred
alpha masks exactly. A separate integral far-mask prototype, with packed
premultiplied color converted for its floating-point surface, improves all four
opaque far-shadow comparisons. Absolute RGBA errors change from 8,045 to 196
and 3,458 to 18 in the small light/dark fixtures, and from 39,545 to 774 and
15,646 to 988 at the dialog bounds. Every residual channel error in those four
comparisons is one; the required allowed difference remains zero. Combined
shadows also improve, but their near-layer, geometry and compositing differences
remain. These are isolated fixture results, not a new library implementation or
a rerun of the full Command Spotlight raster suite.

The upstream browser revision could not be resolved. Current Skia sources were
used only as auxiliary algorithm documentation; they do not establish the
captured browser's implementation. A production change still requires general
corner/size/scaling behavior, resource and device lifetime, resize and collection
cost, and source/package/catalog regression checks. The 112 inventory entries
and both fictional component demos remain in progress and unaccepted.

The subsequent near-layer study retains 104 hidden observations with a
byte-identical far-only control. Floating-point output reduces the large light
combined fixture's absolute RGBA error from 16,932 to 7,370, but its readback
prototype takes about 185 ms to prepare the large near mask and remains
unpromoted. A 113-to-64-to-113 cached-blur model exactly matches all 40 sampled
source edge alpha bytes in both geometries. That sampled profile does not
establish full two-dimensional blur, scaling or performance parity.

The local `0.1.0-overlay-shadow-docs-v1-local` package refresh removes obsolete
Narrator/OS contrast acceptance wording from its README. All 181 library payload
entries are byte-identical to the preceding preview, including the library,
XAML, PRI, assets, notices and transitive build targets. Fresh independent x64
and ARM64 consumers build; the fresh x64 consumer passes the full 100-state
Command Spotlight suite and the 16-case/26-lifecycle shadow fixture. Its 32 shadow
PNGs match the source catalog byte for byte. Other source/catalog fixtures and
the 492 portable/10 Windows text results remain retained evidence for the same
library bytes; those suites were not rerun for this documentation-only package.

## Historical Select integration evidence

The `select-shadow` catalog mode replays a separately frozen browser capture
at the original shadow-only point `(200,186)`, outside the popup's
`(16,60,176,118)` border box. It requires visible shadow paint in both themes,
genuine native input receipts, dismissal and trigger focus, zero callbacks
to the underlying target, zero table mutations and cleared native layers.
Its independent audit compares actual source/native pixels without masks or
tolerance. Exact raster and raw source event-route parity remain false.

```powershell
./scripts/smoke-catalog.ps1 -Executable <catalog-exe> `
  -Component select-shadow -ClientSize 1000,745 -ExpectedArchitecture X64 `
  -Output <native-output>
node scripts/verify-select-shadow-pointer.mjs `
  --source artifacts/select-shadow-pointer-browser/capture-v2 `
  --native <native-output> --output <audit-json>
```

The original 276-state Filters replay independently inspects both live
Composition layers and cleared closures. `verify-select-shadow.mjs` compares
all client RGBA pixels against the unchanged browser capture and preceding
native artifact; closed-frame changes are retained in the report. The
140-state live-update, Pagination input/scroll, native root/nested menus and
independent NuGet consumer remain required regression boundaries. This
change does not establish complete Select or OverlaySurface acceptance.

The integrated x64 checkpoint passes 98 regressions and all 276 original
Filters states. Its full-client comparison verifies 228 native layers and
98 cleared closures. All 114 open frames improve: aggregate absolute RGBA
error falls from 150,235,406 to 116,792,257 (22.3%), and differing pixels
fall from 3,584,570 to 2,753,381. All 162 closed frames retain measurable
differences from the preceding native artifact, totaling 26,982 changed
pixels; these remain in the audit rather than being masked. Four existing
missing-column focus differences and raw event-route differences remain
explicit. Evidence: `artifacts/select-shadow-integrated-{source,rgba,pointer}-audit.json`.

The clean-checkout test caught a CRLF/LF script provenance mismatch in the
first new shadow reference. Capture v2 regenerates the same six source states
from Git's normalized LF script. The failed clean test and capture v1 remain
retained; the original Filters and live-update references are unchanged.

The first published full run timed out at the unchanged 360-second deadline
after 1,025 captures. Its failure remains in
`artifacts/select-shadow-validated-full-timing.json`. The renderer now reuses
its loaded effect factories and brushes through geometry changes. The menu
fixture resizes the live preview, changes its corners and restores both,
requiring the original native container/brush identities and updated kernel
bounds. Fresh focused, package and full verification remain required for
this optimization; the deadline and original input gates are unchanged.

## Local artifact checkpoint: full gate remains open

Source `c7ffe78` builds cleanly for x64 and ARM64, passes 98 regressions and
the token check, and passes nine published focused modes: Menus, shadow-only
outside input, original/live Filters, Pagination producer states, held/rapid
keys, scroll/typeahead and pointer input. The menu resize checks retain the
same native container and effect brushes through widths 224/256/224 and
corners 7/8/7. Both theme reports verify the updated mask/kernel bounds.

The 276-state Filters audit verifies 228 native shadow layers and 98 cleared
closures. Every one of its 114 open frames improves against the unchanged
browser capture: absolute RGBA error falls from 150,235,406 to 116,793,191
(22.3%), with differing pixels falling from 3,584,570 to 2,753,381. Its
162 closed frames retain 28,571 changed native pixels. These differences,
the four original missing-column focus differences, raw event-route gaps
and exact raster mismatch remain explicit; masks and tolerance are zero.
The 140-state live audit still matches settled focus and retained identities.

Fresh isolated NuGet consumers pass x64 runtime in both themes and ARM64
compilation. Both temporary caches are removed. All 532 published x64 files
match the clean build, and the published catalog and both consumers contain
the same package library. ARM64 runtime is unverified.

| Local artifact | SHA-256 |
| --- | --- |
| `packages-select-shadow-reuse-validated/Gaulatti.Orchard.0.1.0.nupkg` | `c9b826c0aef80858ec3895a8853042dab1b198ed48b9c65a98e152d95d1ede59` |
| `Orchard.dll` | `dda0631225ab7c1513bee8b91f44936e47004e99cef68b63ed137d3a76eff121` |
| `Orchard.Catalog.0.1.0-c7ffe78-x64.zip` | `f2907138a1f9a873c3e66a070d73fca52bc3b4292e9819981369850be9563bca` |
| `Orchard.Catalog.0.1.0-c7ffe78-ARM64.zip` | `2d90343d9d8023fbdc94034184076c1b92790cca3a70417191c508522b0d1978` |

All paths are below `artifacts/`. The ZIP audit verifies every archived file;
its x64 runtime scope is the nine focused modes, and full-runtime acceptance
is false. Evidence uses the `select-shadow-reuse-release-*-audit.json`
prefix, plus `select-shadow-reuse-{artifact-correspondence,package-consumer-audit,catalog-archives}.json`.

Two full runs of this optimized source fail the unchanged Sparkline area
focus assertion after 274 captures, at 140.3108473s and 139.1630071s.
Their folders are `smoke-select-shadow-reuse-validated-full` and its `-v2`
counterpart; matching `*-timing.json` files retain both failures. Native
focus and foreground are owned, while the stationary pointer is over the
chart and its tooltip shows value 20 instead of the keyboard-only reference's
10. A separate pinned browser probe also shows 20 after remount, focus and
Right-arrow under that pointer, including after a document reset with trusted
pointer-over/enter events. This identifies a concurrent-hover precondition
that the old keyboard-only fixture does not model; it does not repair that
fixture or establish a causal fix. See the [Sparkline evidence](sparkline-reference.md#concurrent-hover-precondition).

The full gate and throughput acceptance remain unresolved. No deadline,
input/presentation gate, original reference or comparison mask was relaxed.
The new commits remain local pending full validation. The inventory remains
53 in progress, 59 pending and zero accepted at that historical checkpoint;
current counts appear above.
Push-triggered Actions remain paused. The public wiki is unavailable, so
this checkpoint is documented in the repository.
