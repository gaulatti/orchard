# Far-shadow GPU investigation

This work belongs to Orchard's reusable overlay component. The public default
renderer still uses its existing Gaussian effects. The internal
`BleeckerFarShadowLayer` candidate is verified independently and through the
[combined attachment preview](overlay-shadow-combined.md). Public integration
remains gated on complete original-image, geometry and scaling acceptance.

## Source and current boundary

`tests/Reference/OverlayFar.json` retains eight original far-layer images from
the pinned Edge 155.0.4283.62 source. They cover a 200 by 120 fixture and a
672 by 484.5 dialog, white masks, the source paint, and light, dark and transparent
canvases. Every retained PNG has its original bytes. The source browser revision
is `aa3a7406ba8c53a83536df539f07124d73c899bd`; the corresponding upstream Skia
revision remains unresolved.

The preceding browser GPU model independently generates a 512-entry integral
table and matches all eight physical images with full float arithmetic. Six
actual canvas PNG exports match; two transparent exports differ at 104 and 572
pixels. Explicit half conversions worsen the result. Those are diagnostic
browser results, not native library acceptance.

The native candidate reads the canonical far recipe from `BleeckerTokens`:
offset `(0,24)`, blur 64 (sigma 32), spread -20. It first snaps the original
border-box edges, then constructs the spread rectangle. The original rounded
border mask is inverted in the shader before tinting. Both fast and general
rectangle integral branches are implemented. No source image, CPU bitmap or
readback data becomes an input to the renderer.

The attachment requires the original two-layer component graph, scale one,
translation-only geometry and uniform corners from zero through 20. The source
extension also handles empty spread casters by clearing all three retained
alpha planes to transparent one-pixel surfaces. A resize back to positive area
reuses the graph, brush and plane objects. Other shapes fail explicitly.
The standalone constructors still
require exclusive attachment. `BleeckerCombinedShadowLayer` coordinates both
primitives on one component with separate clip and lifetime ownership. Rounded
spread casters, broader transforms and complete parity remain required work.
These boundaries do not establish completion of the overlay or library goal.

The [CSS contract](https://www.w3.org/TR/css-backgrounds-3/#shadow-shape) floors
the contracted dimensions at zero. `OverlayEmptyCaster.json` freezes 156 pinned
Edge source images across 13 geometries, four canvases and far/near/combined
layers. A hidden source run verifies 104 far/combined cases, including 104
geometry roundtrips and public-default restorations, two controlled resource
resets and retained caller bindings. All 40 far-only empty cases paint only
their canvas; all 36 empty combined zero-radius rectangle cases match exactly.
The full sweep is 95/104 exact. Four empty combined rounded cases retain a
near-mask mismatch, and five positive-caster cases retain pixel differences.
Those nine failures remain visible under zero tolerance. The independent auditor
rejects 45 corrupted reports, including a valid nonempty image substituted into
the empty far result with matching capture hashes. All eight source lifetime
modes pass 55 suspension transitions, bound paint preservation, reparenting,
former-ancestor isolation and final-owner cleanup. The two empty modes check
2,700 interior pixels per mode across three caller paints. This source
implementation is unpackaged and
unaccepted; the last delivered preview below retains its original scope.

## Resources and verification

`BleeckerFarShadowDevice` shares one 512 by 1 GPU table between owners on the
same compositor thread. The table generator writes an eight-bit texture from
the analytic integral formula. `OverlayFarIntegral.json` is a diagnostic
expectation used only by the independent auditor. It is not loaded by the
library. The native shader is drawn to a Composition surface through Win2D;
the custom shader is not passed to Composition as an effect graph.

The device wrapper borrows Win2D's shared `CanvasDevice` and never disposes it.
The final owner releases the generated table, device-loss subscription and
Composition wrapper. Generated-resource invalidation queues owner refreshes;
that controlled path is distinct from an actual device-loss event.

The hidden `OverlayFarFixture` exercises each source image, retained-graph
geometry roundtrips, disposal restoring the public default, reattachment,
unload, concurrent owners, controlled resource invalidation and rejected shapes.
Its captures check that the window remains hidden with no focus, activation,
hardware input or popups. `scripts/verify-overlay-far.mjs` verifies all table
bytes, full physical and original PNG channels, ownership metadata and PNG
codec integrity. Structural success never changes a failed raster result.

The current candidate stores three binary mask parts in float16 alpha surfaces.
RGB values stay zero in those surfaces; Composition combines the alpha parts
before applying paint. This avoids treating encoded data as float16 color.
Each loaded owner has thirteen graph resources and one shared 2 KiB GPU table.
The three surfaces are retained across geometry changes. Their memory and
collection costs remain an explicit performance gate before public adoption.

The source replay in `artifacts/current/overlay-shadow-far-native-source-v7`
passes eight geometry roundtrips, eight default restorations, eight automatic
unloads, concurrent table sharing/reset, and four rejected-shape rollbacks.
All eight GPU tables match the independent analytic bytes. Four of the eight
primary images match physically and in original PNG RGBA. The remaining
eighteen physical pixels differ by one level; the PNG gate remains false at
those same pixels. Removing propagated HLSL precision qualifiers did not change
that result. That shader-only iteration preceded the full root, consumer and
archive replays below. It is not component acceptance.

The direct byte-surface baseline quantizes paint before backdrop blending and
fails the opaque images. The RGB-encoded float16 variant worsens the result and
is not adopted. Both failed implementations and their runtime failures are
retained. An initial offscreen concurrent-peer fixture changed capture bounds;
the corrected fixture uses two owners at the same bounds, hides only the peer's
candidate sprite during capture, and retains exact reset/restoration assertions.

## Diagnostic PNG export

The source images were produced by Playwright page screenshots. The earlier
near fixture codec was measured through `canvas.toDataURL`; the two export
paths differ at 148 channel/alpha mappings. A separate page-screenshot palette
measures all 32,896 legal premultiplied channel/alpha pairs in three independent
color permutations (296,064 comparisons). The resulting mapping is injective
and recovers every input byte. Source images are checked only after generation
and do not supply table entries.

`ScreenshotPngCodec` uses that diagnostic mapping for the far fixture. It
eliminates the 104 and 572 transparent export-only differences while preserving
the exact physical and original PNG gates. The library contains neither codec
bytes nor reference-image inputs. Existing sealed canvas-export evidence keeps
its original encoder and scope. Evidence is retained under
`overlay-shadow-far-screenshot-palette-v1` and its independent audit.

The local `0.1.0-overlay-far-rect-v1-local` preview passes full x64/ARM64 builds,
fresh independent consumers in both architectures, 495 portable tests and ten
Windows text tests. Source, NuGet and extracted catalog reproduce the same
passing ownership checks and failing image gates. All three retain 43 exact
near geometry cases, 28 rejection rollbacks, twelve geometry roundtrips,
48 primary ownership cases and all 56 fractional-overlap roundtrips. Two near
alpha pixels remain different. Their default 100-state and 168-logical-hotkey
replays pass; both fictional demos pass 30 host cases and sixteen catalog states
in source and archive. Native ARM64 runtime remains unverified.

Across the four isolated component suites, 846 raw/PNG image pairs are byte
identical in source, consumer and archive. Forty-eight auditor corruptions are
rejected. The package's six embedded shader payloads match compiled source;
its 157 existing XAML files, six fonts and attribution notices are preserved.
Both diagnostic codecs are absent from the library. The catalog ZIP retains
1,360 byte-verified files. Duplicate expanded consumer/catalog files are retained
in verified ZIPs before retirement; captured identity and image copies retain
every byte through checked hard links and NTFS compression.

The unchanged public Gaussian renderer also receives a fresh comparison against
six original combined-shadow PNGs: it differs at 90,433 pixels per host. This
broader gate is independent of the internal far candidate's eighteen-pixel gate.
Neither comparison is relaxed. At this checkpoint, caller
bindings and template/suspension coverage were still open, as were combined
attachment, broader geometry, actual device-loss events, scaling, native ARM64
and measured costs.

## Component visibility and caller lifetime follow-up

The shared public surface now releases its graph when it or a XAML ancestor is
collapsed, instead of retaining 21 native resources. Loaded ancestry visibility
callbacks are removed on unload and replaced after reparenting. The containing
window may remain hidden for offscreen rendering; no OS visibility or settings
are changed. See [the shared lifecycle contract](overlay-shadow.md).

The hidden `OverlayShadowLifetimeFixture` now passes in public/default,
internal near and internal far modes in the source catalog, fresh independent
NuGet consumer and extracted catalog. Three bound paints
per mode retain brush identity, binding and caller opacity. Self/parent/root
collapse, zero dimensions and an empty template release owned graph resources;
private modes also cover a hidden template host. All resumed and reparented
frames match their own visible baseline exactly. Near suspension releases kernel
leases; far suspension releases all 13 graph resources and three float16 planes,
retaining only the shared 2 KiB integral table while its owner is alive. Final
unload releases the registry and visibility subscriptions.

The independent auditor rejects 89 corrupted reports across those three modes.
All 846 prior isolated source capture files and 14 pre-collapse binding/paint
files remain byte-identical. This establishes the scoped lifetime contract;
it does not close the eighteen-pixel far image gate or broader geometry,
combined attachment, actual device-loss, scaling, native ARM64 or cost gates.
The local `0.1.0-overlay-lifetime-v1-local` preview verifies nine mode runs and
60 suspension transitions across those hosts, with 413,100 interior and
17,846,400 restored physical pixels compared exactly. Native ARM64 runtime,
physical input and whole-component acceptance remain open.

```powershell
./scripts/verify-overlay-far.ps1 `
  -AppDirectory <complete-build-or-consumer-directory> `
  -HostName Orchard.Catalog `
  -Output <fresh-evidence-directory>
```

Implementation uses the repository's pinned Win2D 1.4.0 and WindowsAppSDK 2.5.1.
The installed headers and the official [PixelShaderEffect API](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_PixelShaderEffect.htm)
and [CanvasComposition API](https://microsoft.github.io/Win2D/WinUI3/html/Methods_T_Microsoft_Graphics_Canvas_UI_Composition_CanvasComposition.htm)
establish the native drawing boundary. Runtime checks establish support on this
machine. The central standard and public wiki were unavailable; checked-in rules
and this repository documentation are used.

## Precision stage diagnostics

`OverlayFarPrecisionTrace` is catalog/consumer test infrastructure, excluded from
the library. It clones the actual mathematical shader inputs and decoder into
offscreen GPU targets: raw float32 coverage, three float16 alpha planes,
reconstructed float32 coverage, float32 paint, and direct/copied byte outputs
with the fixture backdrop. A diagnostic coverage shader shares the exact
library source prefix before its binary alpha packing. No source image or
readback bytes feed the renderer. The actual component is captured before and
after tracing and must remain byte-identical, with its graph and shader
properties retained.

Source, fresh NuGet consumer and extracted catalog each verify 96 targets over
eight cases. CPU reconstruction
of the half planes exactly matches the offscreen GPU decoder; its maximum
coverage difference from the raw shader is about `1.02e-10`. Materializing paint
in float32 and then copying it to the backdrop matches all eight original source
images, with both nearest and linear final bitmap sampling. The actual
Composition output still retains eighteen differing pixels. Direct effect-to-
byte diagnostic rendering has additional differences and must not be treated as
the actual component result. Different source-build/package DLL identities and
their identical six shader payloads are recorded separately.

Pixel-center reconstruction, direct normalized integral lookup, equivalent
SourceIn tinting and linear alpha-brush sampling did not improve the eighteen-
pixel component gate; their implementations and failures are retained and
rejected. A post-paint affine stage failed at native graph construction because
Composition requires its input to be a source parameter. A nested effect brush
is also excluded by Microsoft's documented brush-combination contract. The
trial moving tint multiplication before three-plane GPU packing worsened the
native result to 441,843 differing primary pixels. Its implementation is retained
and rejected; all 560 library source files match the sealed lifetime baseline.
Output-stage precision
remains under investigation. These diagnostic results establish no new package
or whole-component acceptance. `scripts/verify-overlay-far-precision.mjs`
independently checks original images, target bytes, shader provenance and the
actual before/after component captures.

The brush restriction is documented in Microsoft's
[Composition brush combinations](https://learn.microsoft.com/en-us/windows/apps/develop/composition/composition-brushes#combining-compositionbrushes).
The official [effect precision guidance](https://learn.microsoft.com/en-us/windows/apps/develop/win2d/effect-precision-and-clamping)
explains that graph linking and intermediate placement can change numerical
results. This motivates investigation; it does not establish the cause of the
remaining component pixels.

## Delivered precision diagnostic preview

The local `0.1.0-overlay-precision-v1-local` package contains the unchanged 560
library source files and six original shader payloads. Both new diagnostic
helpers and their shaders stay in the catalog/consumer fixtures. Eighteen far
auditor corruptions are rejected, including rehashed invalid floating-point
data, wrong snapped geometry, changed tint and incorrect reconstruction.

Source, NuGet and extracted-catalog replay verify 30 near-sampling and 96 far
precision targets per host, retain all 940 earlier image files byte for byte,
and pass all nine lifetime modes / 60 suspension transitions. Each host passes
100 default states and 168 logical hotkey cases. Both fictional demos pass
30 host cases and 16 catalog states in source and archive. Independent x64 and
ARM64 consumers build, and 495 portable / ten Windows text tests pass.

All 2,272,672 original reference pixels outside the diagnostic shadow textures
are exactly the known fixture backdrops. This independently establishes full
reference coverage for the copied float32 diagnostics. It does not change the
actual component gate: eighteen far pixels, two near alpha pixels and 90,433
public combined pixels per host still differ. No numerical trial is adopted,
and all 112 inventory entries and both demos remain unaccepted. Native ARM64,
physical input, broader geometry/scaling, actual device loss and measured
collection costs remain open.
