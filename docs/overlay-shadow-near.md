# Near-shadow precision investigation

The source-derived near-shadow experiment now matches 48 complete source images
with the source color or a white mask, on light, dark and transparent canvases.
The browser GPU, native Direct2D shaders and two hidden Composition paths agree
at zero tolerance. This is diagnostic evidence for the reusable overlay renderer.
The public component still uses its packaged Gaussian effect. The subsequent
[combined attachment preview](overlay-shadow-combined.md) coordinates both GPU
layers and their caches on the actual component. Complete source parity,
broader geometry/scaling, actual device loss and measured costs remain required
before public adoption.

## Source boundary

The producer retains 22 whole PNGs from Edge 155.0.4283.62, Playwright 1.62.1,
device scale one and blocked network requests. Eighteen preceding diagnostic
images and both original pinned masks recapture byte for byte. The cases cover
radii 7, 14 and 28, minimum cache geometry, larger rectangles, fractional sizes
and positions, and two cases crossing pixel-rounding boundaries. Ten images
remove blur to inspect the rounded input shape independently.

The source is `artifacts/current/overlay-shadow-near-precision-source-v4/precision-source.json`.
Its producer is `overlay-shadow-near-precision-capture-v4.mjs` in the same current
artifact directory. Its browser reports GaneshGL through ANGLE/D3D11. The
upstream browser Skia revision remains unresolved; the earlier auxiliary Skia
checkout supplies hypotheses, while frozen source pixels determine the result.

## Observed raster pipeline

For the tested sigma of seven, the cache's blur margin is 21 pixels. A radius
`r` produces a rounded caster of width `2r + 43` inside a square texture of
width `D = 2r + 85`. The input uses an analytic signed-distance coverage ramp.
All eight first unblurred geometry cases match that input exactly.

The texture is reduced to `S = floor(D * 4 / 7)` with one transparent pixel of
padding. The scaled sigma is `7 * S / D`. Adjacent Gaussian weights `wi, wj`
become one linear sample with weight `wi + wj` at offset
`i + wj / (wi + wj)`. The center remains a separate sample for the tested even
kernel radius. Horizontal and vertical passes write eight-bit intermediates,
followed by a linear expansion to `D`. Direct taps retain measurable errors;
paired linear taps reproduce the observed cache.

The final shader samples that cache through the nine-patch mapping and
multiplies by the inverse rounded-border coverage. The full-source gate includes
every RGBA pixel. Both single-channel and RGBA eight-bit textures match all
12 blurred source images: 24 comparisons, 6,953,600 compared pixels, zero error.
All 15 corresponding intermediate texture pairs match across the formats.

Pixel rounding has two stages. First round the original rectangle's left/top
and right/bottom edges to derive its dimensions. Then round the already-rounded
origin plus the shadow offset, retaining those dimensions for the blurred
caster. Rounding the combined authored edges gives incorrect results at the two
new phase-crossing cases. The unblurred path retains fractional offsets after
rounding the original box; the observed distinction is preserved in the data.

## Native proof

The isolated consumer uses the current `0.1.0-overlay-tint-v1-local` NuGet bytes
and the installed Win2D 1.4.0 API. Four HLSL programs provide the rounded input,
resampling, paired convolution and final mapping/clip. Their linked shader
binaries were compiled with the installed Windows SDK compiler. The native
experiment has no CPU bitmap inputs, and its diagnostic readbacks never become
renderer inputs.

Eight complete radius-14 images match the original and extended source exactly
offscreen and through a hidden Composition drawing surface. Five intermediate
native buffers match the browser GPU buffers byte for byte. Every Composition
image settles, contains the complete expected pixel buffer and detaches its
visual afterward. The final run releases all 59 owned resources. Its guards
observe no visible host, activation, focus, popup or hardware input.

Evidence:

- `artifacts/current/overlay-shadow-near-precision-source-gpu-audit-v1.json`
- `artifacts/current/overlay-shadow-near-precision-native-audit-v1.json`
- `artifacts/current/overlay-shadow-near-precision-composition-audit-v2.json`
- `artifacts/current/overlay-shadow-near-precision-negative-audit-v1.json`

The independent auditors reject 35 corruptions, including an empty Composition
capture with its valid empty hash, altered pixels with recomputed hashes, wrong
source images, raised tolerances and incorrect snapping. The first presentation
fixture produced empty captures; the auditor rejected them. The corrected host
uses a nested XAML visual and asserts the pixel-buffer dimensions before
declaring a capture settled. An initial shader compiler profile and a C#
initializer also failed during preparation; their attempts remain retained.

The native API follows the installed nongeneric
[`PixelShaderEffect` contract](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_PixelShaderEffect.htm)
and its [linked shader compiler procedure](https://microsoft.github.io/Win2D/WinUI3/html/M_Microsoft_Graphics_Canvas_Effects_PixelShaderEffect__ctor.htm).
Custom shaders draw into a Composition drawing surface through Win2D; direct
use in a Composition effect factory is unsupported by that API.

## Colored-layer precision

The additional producer freezes four color/background combinations for each of
the 12 blurred geometries above. All 12 white masks and six original colored
images recapture unchanged. The 48 browser GPU images, encoded by the browser's
own PNG encoder, match every source RGBA pixel. Browser readback uses binary
transport; an earlier boxed-array attempt exhausted Node's heap and is retained
as a failed attempt.

The packed near paint is premultiplied RGBA `(2,3,5,15)`, rather than the nominal
floating-point CSS color. A colored eight-bit intermediate loses precision
before background blending. A half-float linear-color surface reduces that
error but retains 1,854 one-byte differences on the two original dark canvases.
Applying color later to an eight-bit white mask reduces the difference to four
antialiased light-canvas pixels in each original geometry.

The exact paths instead encode the clipped floating-point mask into an opaque
eight-bit surface. With `x = coverage * 255`, two channels store `floor(x)/255`
and `x-floor(x)`. A third-channel control stores another fractional digit.
A Composition color matrix reconstructs coverage with coefficients `1`,
`1/255` and, for the control, `1/65025`, then applies the packed premultiplied
paint before final background blending. This retains clip precision without
baking in the caller's background. No empirical color bias is applied.

Native shader coefficients are supplied at runtime for radii 7, 14 and 28.
All 48 full-image cases match the independently verified source premultiplied
bytes in offscreen rendering and both encoded Composition paths. The 15 native
intermediate buffers match the browser GPU buffers byte for byte. The hidden
run disposes 895 owned resources and observes no focus, activation, visible
window, popup or hardware input.

The independent color auditors reject 37 corruptions, including empty captures
with valid empty hashes, a valid premultiplied pixel changed with a recomputed
hash, changed intermediate pixels, incorrect snapping, raised tolerances and
source/producer mismatches. Large immutable counterexample inputs are hard-linked;
mutations replace their links first, preserving the canonical evidence.

The physical comparison includes all four premultiplied channels. Recovery
from each source PNG is checked against its actual browser raw/encoded pair;
the browser-encoded images themselves match the source's straight RGBA pixels.
Native WIC's straight-alpha PNG conversion is not used to claim that equivalence.
The original whole-component PNG gates remain unchanged.

The installed Win2D 1.4.0 header and the actual Composition result establish
the alpha mapping used here. The
[WinUI 3 ColorMatrix reference](https://microsoft.github.io/Win2D/WinUI3/html/P_Microsoft_Graphics_Canvas_Effects_ColorMatrixEffect_AlphaMode.htm),
[Direct2D alpha modes](https://learn.microsoft.com/en-us/windows/win32/direct2d/color-matrix#alpha-modes)
and [effect precision guidance](https://microsoft.github.io/Win2D/WinUI3/html/EffectPrecision.htm)
describe the relevant interfaces; the Win2D and Direct2D mode names must not be
assumed interchangeable. The prototype uses the verified `Straight` setting
with an opaque encoded input and a premultiplied matrix output.

Evidence:

- `artifacts/current/overlay-shadow-near-color-source-v1/color-source.json`
- `artifacts/current/overlay-shadow-near-color-gpu-v4/gpu.json`
- `artifacts/current/overlay-shadow-near-color-native-audit-v5.json`
- `artifacts/current/overlay-shadow-near-color-negative-audit-v1.json`

## Attachment to the actual component

The next prototype attaches the encoded near layer to the real, packaged
`BleeckerOverlaySurface` and its existing `PART_ShadowHost`. It hides the original
near output for isolated comparisons. The shader already excludes the original
border interior, so the common outside clip moves onto the retained far sprite.
Six full-image before/after comparisons prove that this relocation leaves every
far-layer byte unchanged. The retained far sprite remains above the near layer.

All 48 near-layer cases remain exact inside the actual component template.
The first attachment used a viewport-sized texture. A fixed padding attempt
failed 40 cases because the texture did not follow the shifted caster; that
failure and its 1,014,398 differing pixels remain retained. The corrected texture
covers the snapped caster plus its 21-pixel margin on each side. Its sprite
offset cancels the component's fractional root position. The largest tested
surface contains 1,505,112 nominal pixel bytes; this is a dimension calculation,
not a measurement of driver memory.

One loaded component then traverses all 12 light-canvas geometries, including
three radii, fractional sizes and positions, and both rounding-boundary cases.
Every update matches the source physical bytes, totaling 3,476,800 compared
pixels. The prototype retains its container, near effect brush and drawing
surface while resizing and redrawing the surface against three shared kernel
entries. The driver invokes this refresh explicitly; the production library's
automatic event and ownership wiring has not been replaced or verified by this
experiment.

Three bound paints—partial alpha, gradient and transparent—retain their brush
references, background binding and component opacity. Shadow-on/off snapshots
agree at all 45,900 checked interior pixels. All 48 component instances clear
their original graph on unload. The probe releases 480 explicitly owned
per-case resources before the next case, peaks at 40 such live resources and
releases the 30 remaining kernel resources at the end. These counters exclude
objects owned by the framework and the existing library.

The component/lifecycle auditor rejects 60 corruptions. These include altered
primary, update, interior and far-layer pixels with recomputed hashes; lost
bindings or graph identity; incorrect texture bounds; changed resource counters;
empty captures; and raised tolerances. Fresh isolated x64 and ARM64 consumers
build from the unchanged tint NuGet. The corrected x64 fixture runs successfully;
ARM64 runtime remains unverified. Their temporary source/feed/package caches are
removed after successful builds.

In the retained x64 run, CPU wall time for resize/draw/session-end submission is
0.1940–0.7973 ms, with a 0.2543 ms median across 12 updates. Diagnostic snapshots
are excluded from that timing. GPU latency, concurrent ownership and actual
driver memory remain unmeasured. Device-loss recovery is also unverified.

The six combined-shadow PNGs still fail the unchanged source RGBA gate:
145,582 differing pixels, absolute RGBA error 916,416 and maximum channel
difference 128. Their far-only controls retain 145,475 differing pixels. Near
parity and unchanged far clipping do not establish full-component parity.

Evidence uses the `artifacts/current/overlay-shadow-near-component-` prefix:

- `audit-v5.json` verifies the component, retained updates and all 15 intermediate buffers.
- `combined-v4.json` retains both failed full-image gates.
- `native-v2` and `audit-v2.json` retain the failed fixed-padding prototype.
- `native-v4` contains the corrected bounded-surface and lifetime captures.

## Internal library ownership preview

The local `0.1.0-overlay-near-owner-v1-local` preview moves the renderer into
internal `BleeckerNearShadowLayer` and `BleeckerNearShadowDevice` classes. Four
linked shader binaries are embedded in the library. Normal builds consume those
binaries; `scripts/compile-overlay-near-shaders.ps1` independently rebuilds them
with the documented Windows SDK FXC procedure and verifies every byte.

The layer owns size, corner and layout/position callbacks, its drawing surface
and effect brush, and template/unload cleanup. Device resources are shared per
compositor/UI thread. Two simultaneous components share the same kernel bitmap;
unloading one preserves the other's brush and source-exact pixels. The final
owner releases the renderer's cache and graphics wrapper without disposing
Win2D's shared Canvas device. Controlled generated-resource invalidation redraws
both owners through library callbacks. An actual device-loss event is unverified.

Fresh source, independent NuGet and extracted-catalog runs each pass 48 physical
near-image cases, 12 automatic geometry states, three bound paints and 45,900
interior-pixel checks. Template replacement rebuilds the owned graph and retains
the same source pixels. Loaded disposal restores the original default graph
byte for byte. Five unsupported inputs—nonuniform corners, zero radius, small
rectangles, fractional radius and transformed scale—fail explicitly inside the
internal primitive and leave the original public renderer unchanged. The test
driver changes properties and offsets; it issues no near-renderer draw calls.

The PNG round-trip audit found an export defect: implicit WIC unpremultiplication
converted native BGRA `(129,129,129,129)` to straight RGBA `(254,254,254,129)`.
That image cannot recover the native bytes. The new fixture supplies explicit
straight bytes using nearest-even conversion; every exported PNG now recovers
its original premultiplied buffer. The auditor rejects 63 corruptions, including
this retained real defect and altered raw/PNG pairs with recomputed hashes.

Physical source comparisons cover 18,070,400 pixels per host at zero tolerance.
The original straight-RGBA PNG gate remains false at 6,006 differing pixels;
encoder rounding is reported separately rather than waived. The original
combined/far and full-component raster gates remain open. Both source/package
sets contain 99 raw and 99 PNG files; all 198 corresponding files are identical.

A follow-up codec diagnostic explains why a small floating-point fit is
insufficient: three expressions match all 313 observed channel/alpha pairs but
fail the pinned browser's exhaustive palettes. A reference mapping derived from
all 32,896 legal eight-bit channel/alpha pairs matches 296,064 cross-palette
channel checks and all 13,907,200 source pixels. Every mapped value recovers its
original premultiplied byte, so the mapping is injective for each alpha and
cannot hide a physical pixel change. This is diagnostic PNG-encoder evidence;
neither the native exporter nor renderer adopts it in this preview. The 6,006
original-PNG differences remain recorded until a separately verified export
change uses that mapping. See
`artifacts/current/overlay-shadow-near-owner-codec-palette-audit-v1.json`.

Root x64 and ARM64 builds and clean NuGet consumer builds pass. The package
contains one AnyCPU IL library and the unchanged public API, six font files and
the current README. Fresh portable and Windows text suites pass 495 and 10 tests.
The package's default Command Spotlight checks pass 100 states and 168 hotkey
states. Source and extracted-catalog replays also load both required fictional
demos through 30 checks and 16 catalog states. ARM64 runtime remains unverified.

The internal renderer is not the default overlay path and has no public opt-in
switch. Its cache currently retains generated radii while an ownership group
remains loaded; active-radius eviction, zero-size/suppression transitions and
measured collection/driver-memory costs remain required. The default path stays
on the existing renderer while these and broader geometry/scaling are resolved.
The new source and its embedded binaries are retained in
`artifacts/current/overlay-shadow-near-owner-implementation-v1` for reproducibility.

```powershell
./scripts/verify-overlay-near-owner.ps1 -AppDirectory <catalog-or-consumer> `
  -HostName Orchard.Catalog -Reference tests/Reference/OverlayNearOwner.json `
  -ReferenceImages tests/Reference/OverlayNearOwner `
  -ShaderManifest tests/Reference/OverlayNearOwnerShaders.json -Output <fresh-output>
```

Use `PackageConsumer` for an isolated NuGet consumer. The wrapper requires a
hidden, noninteractive run and invokes the independent audit. The permanent
reference preserves the original capture JSON bytes and 48 PNGs. Paths inside
that capture JSON describe historical producer provenance; they are not runtime
dependencies of this fixture.

## Active-radius and diagnostic PNG follow-up

The next internal preview replaces historical-radius retention with leases for
currently drawable owners. A radius change retains another owner's kernel and
evicts the previous radius when its last lease ends. Suppression, a missing
template and zero dimensions release the owned graph and kernel while retaining
the loaded owner. The component notifies internal observers when its shadow
graph changes; the layer also retains its layout, size and corner callbacks.
No OS settings are changed by the fixture's component-level suppression checks.

The hidden fixture now embeds the independently captured browser PNG encoder
mapping from `tests/Reference/OverlayNearPngCodec.json`. It verifies the mapping's
hash and all 32,896 legal channel/alpha round trips before exporting. The
independent auditor checks each PNG channel against that mapping and also
recovers every original native byte. Original RGBA reference comparisons retain
zero tolerance. This table is confined to fixture serialization; it is absent
from the Orchard library, its GPU shaders and renderer inputs.

The local `0.1.0-overlay-near-leases-v1-local` preview passes fresh source,
independent NuGet and extracted-catalog runs. Each covers 48 primary cases, 12
automatic geometry states, two concurrent-radius frames and five restored
states: 19,672,000 source pixels with zero physical or original-PNG differences.
All five suspended states paint no shadow and retain neither graph resources
nor a kernel lease. All 99 retained raw frames are byte-identical to the preceding
ownership preview; the encoder change leaves their physical pixels unchanged.
All 111 raw and 111 PNG files match across the three hosts. The auditor rejects
80 corruptions, including an alternate PNG rounding map that recovers the same
physical bytes but differs from the pinned encoder.

Fresh x64/ARM64 solution and isolated-consumer builds pass, alongside 495 portable
and 10 Windows text tests. All three hosts repeat the default Command Spotlight
100-state/168-hotkey contracts. Source and archive also repeat both fictional
demos through 30 public-host cases and 16 catalog states. These checks preserve
their stated scope; they do not establish full component raster or physical input.
The package contains the internal shaders and active-radius renderer; the reference
codec is absent from the library and confined to the hidden fixture hosts.

## Broader geometry regression sweep

`tests/Reference/OverlayNearGeometry.json` and its 103 PNGs freeze additional
integer and fractional corners, small and clamped shapes, unequal corners and
uniform transforms. Edge 155.0.4283.62 with revision
`aa3a7406ba8c53a83536df539f07124d73c899bd` recaptures all 103 images byte for byte
in a fresh repeat and in reverse order. Both original mask baselines remain exact.
The set includes 32 unblurred coverage diagnostics.

The hidden native sweep passes 31 currently supported geometries, covering 15
distinct integer radii between one and 40 and 7,824,000 source pixels. Physical
bytes and original PNG RGBA both match at zero tolerance. Another 40 blurred
inputs exercise the internal primitive's explicit rejection boundary; each leaves
the existing public renderer's bytes and caller paint intact, with no retained
cache owner. Automatic unload releases every supported case. The 32 unblurred
diagnostics are excluded because the native primitive uses the fixed near sigma
of seven. This does not add a public rejection rule or broaden the primitive.

The independent auditor rejects 46 corruptions, including raw/PNG changes with
recomputed hashes and a rollback image reshaped without changing its byte count.
Source, fresh NuGet and extracted catalog each pass that sweep. All 222 raw/PNG
files match across the three hosts. Fresh x64/ARM64 solution and consumer builds
and 495 portable tests pass; the 10 Windows text results are retained for
unchanged library/text inputs. The three default 100-state/168-hotkey replays and
both fictional-demo replays also pass. The library and NuGet bytes remain the
verified lease preview; only reference data, documentation and fixture hosts
change here. The package README remains accurate for those unchanged APIs and
renderer limits.

Broader fractional shader research remains unadopted. The 17-cache GPU model
matches 41 of 43 selected source cases; quarter radii 6.25 and 13.25 still fail.
Nearest-radius and separate caster-radius variants do not resolve them. Another
57 overlapping/phase/zero-radius captures test interior exclusion; the two clip
variants match only 39 of 114 comparisons. These are diagnostic counts, not
component acceptance. The unblurred source coverage matches the fractional
signed-distance shape at all nine examined radii. Cache construction and the
remaining general rendering paths still need work before native adoption.

## Fractional-radius implementation under validation

The next diagnostic establishes the missing sampling rule: the canonical cache
uses the ceiling of the radius, while the final nine-patch uses the original
fractional radius and nearest-neighbor sampling. This matches all 43 eligible
frozen geometry cases at zero physical and original-PNG error. Retaining the
actual shadow offset after snapping the original border also matches all 56
positive-radius overlap/position cases. Browser paint-command snapshots confirm
that fractional radii and shadow offsets reach drawing unchanged; original
border bounds are snapped first. These are browser-model results pending native
verification, rather than full component acceptance.

The internal layer now uses that rule. Its geometry fixture accepts the original
103-case reference and `tests/Reference/OverlayNearFractional.json`, preserving
the latter's 57 original PNGs. The expected boundaries are 43 supported / 28
rejected / 32 unblurred diagnostics, and 56 supported / one rejected zero-radius
case respectively. Rejection still leaves the public renderer unchanged. The
ownership fixture replaces its former fractional-radius rejection with a
fractional shape too small for the canonical kernel. Run the geometry wrapper
above with either reference and its matching image directory. The public default
renderer, small/zero/unequal-corner paths and transforms remain unchanged.

The first native overlap run found three one-level pixel differences. Using a
third fractional encoding digit resolves one; the remaining two alpha pixels
belong to `overlap-r13.25-Transparent` (native seven, source six). The 56 loaded
fractional radius roundtrips retain the same graph, drawing surface, brush and
ceiling-radius kernel, then restore identical physical pixels. Their independent
source-image gate remains false until those two differences are resolved. A
separate browser float/UNORM diagnostic shows the GPU's normalized-byte conversion
rounding the examined float outputs differently from ideal scalar rounding;
this is diagnostic evidence, not a renderer workaround or relaxed tolerance.

The local `0.1.0-overlay-near-fractional-v1-local` NuGet and catalog preview
repeat these results in source, an independently restored NuGet consumer and
the extracted catalog. Each passes the 43-case geometry gate (including 12
fractional roundtrips), 28 rejection rollbacks and the existing 48-primary-case
ownership suite with 12 automatic states, two concurrent-radius frames and five
restored states. Each reproduces the same two failed alpha pixels in the
56-case overlap sweep. The auditor rejects 52 corruptions, including missing
fractional callbacks and replaced graphs/kernels. The historical `encode2`
resource now retains three fractional encoding digits.

Both solution and fresh-consumer architectures build without errors; the
solution builds have zero warnings. Final portable tests pass 495/495; the
unchanged Windows text inputs pass 10/10. All three default Command Spotlight
replays pass 100 states and 168 logical hotkey states. Source and archive pass
both fictional demos through 30 public-host cases and 16 catalog states. These
checks establish neither physical input nor full demo raster acceptance. Fresh
far/combined original-PNG comparisons still fail at 75,924 / 76,743 pixels in all
three hosts. The preview remains internal and unaccepted, with the default
renderer unchanged.

```powershell
./scripts/verify-overlay-near-geometry.ps1 -AppDirectory <catalog-or-consumer> `
  -HostName Orchard.Catalog -Reference tests/Reference/OverlayNearGeometry.json `
  -ReferenceImages tests/Reference/OverlayNearGeometry -Output <fresh-output>
```

Use `PackageConsumer` for an independent NuGet host. The renderer receives only
geometry and paint parameters; reference pixels stay in the independent auditor.

## Float-intermediate diagnostics

The hidden geometry fixture also exports byte and float32 GPU targets for six
overlap controls. It records the canonical kernel, the encoded target, color
reconstruction, explicit float-to-byte copying and forced effect caching. Its
diagnostic readbacks never become renderer inputs. Each trace compares the
component frame before and after and verifies that its graph and kernel remain
owned by the library.

The two transparent alpha pixels already differ in direct Win2D byte output,
before Composition. Calculating residuals from the normalized coarse digits
improves float reconstruction but leaves that image gate false. Forcing a float
intermediate in the standalone Win2D control yields the source alpha of six;
direct matrix evaluation yields seven. Setting `CacheOutput` on the effect
passed to Composition does not change the actual component result, so that
ineffective setting is removed. A one-pixel float32 Composition surface is
rejected during allocation on this runtime with `E_INVALIDARG` (`0x80070057`).
The probe borrows the graphics device without disposing it. Residual arithmetic,
reordered digit channels and a two-stage decoder all retain the same two failing
pixels; none is adopted. The three renderer files are restored byte for byte
from the preceding sealed implementation. A forced rebuild produces a different
DLL identity and receives its own hidden source validation; that identity is
not substituted for the earlier package's verified binary.

`scripts/verify-overlay-near-precision.mjs` validates all 54 trace targets,
finite float values, preserved component frames/ownership and the original
strict raster result. Its data-integrity success does not make a failed raster
gate pass.

The restored-source run is retained in
`artifacts/current/overlay-shadow-quantization-native-source-v9`. It reproduces
55/56 exact fractional images, preserves all 56 live roundtrips and verifies the
54 diagnostic targets. The descriptor auditor also validates the actual color
matrix and capability result; twelve corruptions are rejected. These fixture
changes remain unpackaged. The earlier fractional preview remains the last
sealed package, with its two-pixel failure explicitly open.

## Coordinate and sampling trace

The catalog and independent consumer now include `OverlayShadowSamplingTrace`
as test infrastructure. Its five float32 data targets record input coordinates,
three equivalent kernel-sampling expressions, exterior clipping, warp positions
and scene coordinates. Six cases provide 30 targets. These are numeric data,
not color images; values above one must remain intact. Source images and readback
bytes never become renderer inputs, and the actual component frame must remain
unchanged before and after tracing.

The hidden source run found no sample differences among the three UV formulas.
The left scene center can differ from its nominal value by approximately
`-1.91e-6`, while remaining in the correct pixel cell. Reconstructing pixel centers
in the actual shader retained the same two failed alpha pixels, so that trial
was rejected and the library sources were restored byte for byte. Alternative
paint/clipping orders also failed to improve the original source gate. None of
these experiments changes image tolerance or establishes component acceptance.

`scripts/verify-overlay-shadow-sampling.mjs` validates shader provenance, finite
data, the actual scene pixel cell, numeric ranges and retained component bytes.
`scripts/test-overlay-shadow-sampling-audit.mjs` rejects fourteen corruptions,
including rehashed NaN data, wrong scene cells and clamped numeric data. The diagnostic preview `0.1.0-overlay-precision-v1-local` verifies these targets
in the source catalog, fresh isolated NuGet consumer and extracted catalog
archive. Fourteen near and eighteen far auditor corruptions are rejected. Root
and independent-consumer x64/ARM64 builds, 495 portable tests and ten Windows
text tests pass. Native runtime evidence is hidden x64 only; ARM64 runtime and
physical input remain unverified.

## Geometry research after the combined preview

The sealed combined preview remains the last package delivery. Current geometry
sources are an unpackaged internal prototype; they do not change the public
renderer or establish acceptance of any inventory entry.

The rectangle prototype embeds six near shaders. The independent rebuild helper
reads the shader manifest, rejects missing or extra source/binary files and hash
drift, and verifies all six rebuilt binaries byte for byte. Its overall result
fails when any embedded binary differs. The completed rebuild is retained at
`artifacts/current/overlay-shadow-generic-filter-shader-rebuild-v4/compile.json`.

The zero-radius extension generates a 128-texel Gaussian integral profile on
the GPU and shares it across rectangle owners. The headless GPU model matches
both frozen zero-radius images, including overlap with the original surface.
The native implementation matches all 44 supplemental rectangle cases in
`tests/Reference/OverlayNearRectangles.json`: tiny sizes, the 41/42-pixel branch
boundary, short axes, overlaps, white masks, and light/dark/transparent tinted
canvases. The two shared baselines retain their original source PNG bytes.
These fixtures extend coverage; they do not replace any of the original 103
entries.

The complete original geometry sweep now renders 57 cases, with 55 exact,
14 explicitly rejected blurred cases and all 32 unblurred diagnostics retained.
The capsule still differs at 544 pixels and the oval at 3,098; zero tolerance
remains unchanged. The updated fractional sweep renders all 57 cases with 56
exact; its original two alpha differences remain. All 48 canonical owner cases
remain exact, and 495 portable plus ten Windows text tests pass.

The loaded owner traverses actual, canonical, rectangle and actual geometry
without replacing its graph, drawing surface or caller binding. Rectangles of
different dimensions share one integral profile (512 nominal pixel bytes, not
a driver-memory measurement), retain two independent leases, and preserve the
survivor when the other owner returns to a circle. Generated-resource
invalidation, suspension/resume, unload and empty snapped bounds retain their
checks. The auditor rejects 71 full-geometry corruptions and 40 applicable
rectangle-report corruptions; absent rollback/roundtrip metadata is explicitly
reported as inapplicable in the supplemental-only suite.

Evidence uses `artifacts/current/overlay-shadow-generic-filter-`: `native-v2`,
`rectangles-native-v1`, `fractional-v1`, `owner-v1`, `negative-v1.json` and
`rectangles-negative-v1.json`. This implementation is unpackaged and unaccepted;
it has not changed the public default renderer or either required fictional demo.

The six hidden source lifetime modes (default, near, far, combined,
near-rectangle and combined-rectangle) pass 41 suspension transitions, reparenting,
former-ancestor isolation, bound paint preservation and final-owner cleanup.
The near-rectangle mode additionally replaces its generated profile while
retaining graph, brush and binding identity, then restores the exact image.
This controlled invalidation does not simulate actual device loss. The two
rectangle lifetime auditors reject 47 and 40 corrupted reports, respectively;
together with the geometry auditors, 198 corruptions are rejected. All 468
preceding case capture files compared remain byte-identical. The latest x64
build and ARM64 cross-build pass with no warnings or errors, and root restore
assets are x64. Native ARM64 runtime remains unverified.

The combined rectangle lifetime case uses a 200-by-120 surface whose far-shadow
spread caster has positive area. The current far owner rejects dimensions at
or below 40 pixels after snapping because its canonical spread is -20.
Consequently, the exact tiny near-shadow cases did not establish combined
tiny-surface support. The next source-only extension clears the retained far
alpha planes to transparent one-pixel surfaces when either snapped caster axis
is empty, then resumes normal rendering when both axes become positive. It does
not replace the loaded graph, brushes or planes during these resizes.

The [CSS shadow contract](https://www.w3.org/TR/css-backgrounds-3/#shadow-shape)
floors contracted width and height at zero. The pinned Edge source probe freezes
156 images across 13 geometries, four canvases and far/near/combined layers in
`tests/Reference/OverlayEmptyCaster.json`. Its integer empty cases produce only
the canvas for the far layer and byte-identical near/combined images. Fractional
40.49 and 40.51 CSS widths bracket the snapped 40/41-pixel boundary. The hidden
native source sweep is 95/104 exact, including all 36 empty combined zero-radius
rectangle cases. The four combined rounded 40-by-120 cases still differ in
their near mask (718/93/15/49 physical pixels for white/light/dark/transparent).
These added failures preserve the full geometry requirement. The far-only empty
cases all paint only their canvas. This remains source evidence without public
adoption, component acceptance or a new package delivery.

The actual-shape GPU path caches by normalized radius and snapped dimensions.
Its hidden x64 source run preserves all 43 previously supported images and adds
exact physical/PNG matches for the 69- and 70-pixel rounded squares. The capsule
still differs at 544 pixels (maximum channel difference 1), and the oval differs
at 3,098 pixels (maximum 2) after trimming the input to the blur reach. The
latest hidden x64 sweep renders 56 frozen cases with 54 exact physical/PNG
matches, explicitly rejects 15 blurred cases, and retains all 32 independent
unblurred diagnostics. All nine circular cases are exact in WinUI, including the
one-pixel circle that failed in the browser model. Its strict image gate still
fails on the capsule and oval. See
`artifacts/current/overlay-shadow-geometry-expansion-native-v3/geometry-audit.json`.

A separate mathematical circle model creates the radial profile on the GPU from
Gaussian coefficients. Eight of nine frozen circular cases, including colored
light/dark/transparent cases, match every physical and PNG pixel. The one-pixel
circle differs at 53 pixels, each by one channel value. No source images or
readbacks become renderer inputs. These are headless browser model results;
the WinUI implementation subsequently matches all nine circular cases. Shape-key
sharing, transitions to the canonical path, generated-resource invalidation,
suspension, surviving owners and unload pass their separate native checks. The
fixture preserves caller bindings and compares the roundtrip, regeneration and
resume images byte for byte. See
`artifacts/current/overlay-shadow-geometry-expansion-native-v5/geometry-audit.json`.
Two additional native checks reject width/height bounds that snap to zero,
retain public bytes and release every temporary cache owner. All 217 capture
files from the preceding run remain byte-identical. The updated source suite
retains 55/56 fractional images exact (two alpha pixels still fail), all 48
canonical owner cases exact, and 495 portable plus ten Windows text tests.
The x64 build and ARM64 cross-build pass with no warnings or errors. The final
auditor rejects 64 corrupted reports, including false success claims for the
empty-bounds checks. The strict raster gate remains failed. Native ARM64 runtime
is unverified; the root restore state is x64.
These results do not establish a new NuGet or catalog delivery.
The model reports are under
`artifacts/current/overlay-shadow-geometry-expansion-circle-gpu-v3-*`.

The model follows the radial-profile and mask-allocation contracts inspected in
[Skia's primary source](https://skia.googlesource.com/skia/+/6ff22cb04a14e373fc9780bc955775fbe85b4ca0/src/gpu/ganesh/GrBlurUtils.cpp).
That commit has not been confirmed as the pinned Edge binary's exact Skia
revision, so its applicability is an inference tested against frozen images.
Current work also accounts for source blur reach when trimming rectangular
backings. Circular radii beyond the tested range (including the source half-plane
approximation) also need coverage. The full small-shape, unequal-corner,
transform and scaling requirements
remain open; exact results for selected cases do not replace those requirements.

## Remaining acceptance

The ownership preview has not replaced the default library renderer. Arbitrary
radii, small/nonuniform rounded rectangles, scaling and native ARM64 remain open.
Default integration must retain public backgrounds,
bindings, geometry updates, loaded graph identity, unload behavior and device
recovery. It must measure cache creation, repeated resize, collection use and
memory with diagnostic readbacks excluded from the rendering path.

The current internal-renderer preview changes library/package bytes; its public
default renderer and original component raster gates remain unchanged. All 112
entries and both fictional demos remain in progress and unaccepted. The fresh
495 portable/10 Windows text and focused runtime results above belong to the
ownership preview; older delivered-preview suites retain their historical scope.
Wiki access was unavailable, so the evidence is documented here.
