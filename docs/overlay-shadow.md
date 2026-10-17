# Shared overlay shadow

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
complete three-sigma margin on every side. A SourceIn color effect and the
sprite's scalar opacity apply the token color without quantizing its opacity
to an eight-bit alpha. The project already pins Win2D 1.4.0; this adds no
dependency. See the [CSS blur definition](https://www.w3.org/TR/css-backgrounds-3/#shadow-blur)
and [Direct2D Gaussian blur](https://learn.microsoft.com/en-us/windows/win32/direct2d/gaussian-blur).

Composition children extend the paint beyond the layout box without adding
XAML hit regions. Resizing and corner changes update the live caster mask,
source extent and output placement while retaining the loaded effect graph.
Unchanged geometry skips that update. Template replacement and unloading
detach the visual and dispose the owned native graph;
XAML retains ownership of its alpha-mask brush. Existing Windows contrast
suppression remains in place. Live OS contrast switching, scaling and ARM64
runtime remain acceptance work.

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
53 in progress, 59 pending and zero accepted; the goal is active.
Push-triggered Actions remain paused. The public wiki is unavailable, so
this checkpoint is documented in the repository.
