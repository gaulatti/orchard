# Shared overlay shadow

`BleeckerOverlaySurface` renders the pinned two-layer Bleecker overlay recipe:
`(0,24,64,-20)` at .28 opacity and `(0,4,14,0)` at .06, both RGB
`(26,55,77)`. Select now uses this surface inside its existing popup border;
Menus, Tooltip, Alert, Notification and Column Toggle already consume it.
The shadow changes paint only. Popup measurement, placement, scrolling,
keyboard stops, modal ownership and consumer callbacks retain their contracts.

CSS defines Gaussian standard deviation as half the box-shadow blur value.
The renderer therefore uses Win2D `GaussianBlurEffect` at 32 and 7 pixels,
with soft transparent borders and quality optimization. Each rounded caster
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
