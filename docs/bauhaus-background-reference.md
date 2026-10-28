# BauhausBackground reference and acceptance

Live enabled-motion verification at `faab41e`, run `38578657500`, passes native
ARM64 focused Light/Dark and the full-suite Light boundary. The full-suite span
is 332.4672ms of frame time, predicting 5.9844096 degrees against 5.9843607
observed; maximum error across the complete trace is 0.000152741492 degrees.
Its verified archive SHA-256 is
`cd5210b2fe18ee1c289b8283e9216b782da2868df4b340a468941aaab350cd0b`.
Other full-suite failures prevent overall CI success. This verifies the clock
correction, while the broader motion/raster acceptance matrix remains open.

Run [38576042972](https://github.com/gaulatti/orchard/actions/runs/38576042972)
isolates the timing mismatch on native ARM64. Its full-suite Light observer
span is 324.7343ms while the rendered-frame span is 395.6032ms. Both transforms
advance 7.1208029 degrees, matching the frame interval's 7.1208576 degrees.
The maximum error across all 21 frames is 0.000104230066 degrees. The first
two callbacks arrive 3.3252ms apart but represent frames 89.1712ms apart: a
callback stopwatch does not identify the frames whose angles were captured.
The archive's verified SHA-256 is
`d4e77e3c390570184467bb7e158076b0195f9f1300f6fead7e1a71492ec4dabe`.

The gate now compares every actual outer/inner transform against the independent
`RenderingEventArgs.RenderingTime` interval at the same 18 degrees/second and
strict one-degree bound. `Storyboard.GetCurrentTime` remains diagnostic only.
Both callback and rendered-frame sampling spans must reach at least 320ms;
the existing two-second timeout and native reduced-motion requirement remain.
Timestamps must be finite and monotonic. The observer expectation is retained
as `expectedDelta`, with `expectedFrameDelta` and maximum per-frame error
reported separately. No frames are discarded, and no animation behavior changes.
`tests/Reference/BauhausFrameTiming.json` retains the real failed trace. Regression
coverage also rejects a bad intermediate rotation with correct endpoints,
non-static reduced motion, short windows and regressing frame timestamps.
Full motion/raster/platform acceptance still remains open.
The corrected fixture passes the focused native x64 Light/Dark suite at
`artifacts/smoke-bauhaus-rendering-time` with the actual animations-disabled
preference, zero angles/error, and both sampling spans above 320ms. All 42
unit/resource regressions pass, including the preserved enabled ARM64 trace.
Live enabled-motion verification of this correction still requires hosted CI.

Run [38563463296](https://github.com/gaulatti/orchard/actions/runs/38563463296)
still fails full-suite ARM64 motion: the observer measures 333.1919ms while the
storyboard advances 393.2726ms, producing 7.078908 degrees against 5.9974542
expected by the unchanged wall-clock gate. The verified runtime archive has
SHA-256 `2fd1646dff1e07a54ae32cbc7e729a6af893fca643d18b4626996eac12523604`.
The preceding diagnostic reports also retained each native
[`RenderingEventArgs.RenderingTime`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.renderingeventargs.renderingtime?view=windows-app-sdk-2.0),
available in the WinUI 2.3.9 metadata pinned by Windows App SDK 2.5.1. That added
the timestamp of the rendered frame alongside observer and storyboard time.
That diagnostic checkpoint changed no animation behavior, wall-clock expectation, sample duration,
native reduced-motion requirement or one-degree tolerance. Enabled ARM64
evidence is required before changing the timing diagnosis or acceptance claim.
Both x64/ARM64 solution builds and all 38 regressions pass at
`artifacts/tests-bauhaus-frame-clock`. The focused native x64 fixture passes
both appearances in `artifacts/smoke-bauhaus-frame-clock`; all 22 Light and 21
Dark samples contain native frame timestamps. The actual local preference is
animations disabled, so storyboard time is null and both angles remain zero.
This validates collection and reduced motion, not enabled-motion timing.

The frozen source is
[`bauhaus-background.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/bauhaus-background.tsx),
SHA-256 `648c0cdb37639b40ac6c67ecbad04b692d1400358b8b3bed9fad12e5baa1489c`.
The downloaded source matches the inventory. The native control is implemented
and remains in progress until the acceptance matrix below is verified.

The source contract is four variants: Nazca uses bronze/circles, Autostrada
oxblood/lines, Pompeii bronze/squares and Monitor oxblood/circles. Bronze is
`#cd7f32` and oxblood `#76323f` in both source appearances. The image fills the
container with object-cover. A transparent-to-30%-black vertical gradient is
multiplied over it, then the opaque variant/pattern layer uses soft-light blending.
Clipping, blending and transparent source pixels must be checked against the
actual parent backdrop; placing translucent normal-blend shapes over an image
does not implement this contract.

The two circles are rounded rectangles, not unconditional ellipses: the outer
is positioned at 25%/25%, sized 50%/50%, with an 8px white border; the inner at
one third/one third, sized one third/one third, with a 4px white border. Both use
full rounding. The outer rotates forward and inner backward, linearly over 20
seconds forever. The six-column lines use a 2px left border at white/85%; their
0–500ms transition delays have no animated property in this component. The nine
squares use a 3×3 grid with 16px padding/gaps and 2px white/85% borders.
At narrow widths, CSS border boxes retain minimum sizes (16/8px circles,
2px lines and 4px squares), while grid positions retain their smaller tracks.
The component clips the resulting overflow. Native geometry preserves this
behavior rather than shrinking the borders or expanding the grid gaps.

WinUI's `Border.CornerRadius` is a stroke-center radius. The
[pinned SDK geometry implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/ba3a8d59e6de4e2b86ca4f61c78bc26ccf129afd/src/dxaml/xcp/core/core/elements/geometry.cpp)
adds half the border thickness for the outer contour. The native adapter
subtracts that half-thickness to preserve CSS's outer rounded box. Rectangular
shape boxes retain fractional grid layout, while their painted border edges
snap to the device grid, matching the actual browser raster.

`imageUrl ?? defaultImageUrl` is authoritative: an empty supplied URL is not a
request for the default. The default is the application's `/hero/pompeii.avif`;
the frozen Bleecker tree contains no corresponding image asset. Do not fabricate
or silently replace it. A native adapter must document its application-resource
mapping, preserve custom sources, and expose unavailable images without choosing
alternate artwork. The source image has an empty alternative text and is
decorative. It contains no commands or user content.

Acceptance requires all four variants in square/wide/tall/constrained layouts,
opaque and RGBA custom images, explicit/default/missing/empty source behavior,
live changes, clipping/blend pixels, actual rotation/reduced motion, unload/reload,
themes/contrast, display/text scaling, decorative accessibility, x64/ARM64 and
independent NuGet consumption. Browser/native raster comparison remains required.
The public wiki remote is unavailable, so this source contract is recorded here.

## Native API and behavior

`BleeckerBauhausBackground.Type` is required before mounting and accepts the four
`BleeckerBauhausType` values. `ImageSource` and `DefaultImageSource` accept native
image sources; a null custom source selects the explicit default. Clearing both
clears the image. A supplied empty image object is retained and does not select
another image. `ImageFailed` exposes the native load/decode error to the consumer.

```xml
<orchard:BleeckerBauhausBackground Type="Pompeii" Width="288" Height="96"
    ImageSource="{Binding ConsumerImage}" />
```

The application-resource mapping for `/hero/pompeii.avif` is
`ms-appx:///hero/pompeii.avif`. The library does not bundle that application-owned
file or install image codecs. Applications must supply that resource and a
supported decoder, or provide their own `ImageSource`. Failure leaves the image
unavailable while the source's shade/pattern remain; no replacement art is used.

The visual graph captures the live native image and geometric layers, samples
the parent composition backdrop, and applies multiply then soft-light effects.
The effect's operand order is covered by native/browser pixel regression,
including opaque black and transparent image pixels. Private capture hosts
are translated outside the window so unfiltered source layers do not paint
over the effect. Retemplating and unloading dispose the composition resources;
remounting rebuilds them. No bitmap snapshot replaces the consumer's live source.

This background is decorative: it is absent from UIA's control/content trees,
has no tab stop and receives no pointer input. In Windows contrast mode it uses
a flat semantic Window surface and stops rotation, following Orchard's native
contrast contract. With animations disabled, both rotation angles remain zero.
Regular appearance changes preserve the source's bronze/oxblood colors.

## Current verification

The registered **Bauhaus backgrounds** fixture shows all four variants with an
RGBA source, square/tall layouts and an unavailable application default.
`scripts/smoke-catalog.ps1 -Component bauhaus` exercises both appearances;
the full catalog uses the same fixture. Checks include shape counts and
geometry, consumer source identity, null/default behavior, live type changes,
explicit empty images, retemplating, decorative UIA semantics and the native
animation preference. Updating the same WriteableBitmap and invalidating it
must change the actual composed pixels; retaining a stale snapshot fails.

`Reference/BauhausPixels.json` records Edge 155.0.4283.45 raster samples from
the frozen component structure with its CSS utilities expanded, at rotation
zero. It uses a 288×96 image with opaque, half-alpha and transparent stripes
over `#284c68`. This is a focused layer-blending reference, not complete
React/Tailwind component acceptance. The native regression compares 32 actual
composed pixels per appearance, with at most three RGB levels for raster
quantization. Local x64 checks pass at
`artifacts/smoke-bauhaus-blend-order` with display/text scale 1 and animations off.
The expanded fixture passes at `artifacts/smoke-bauhaus-live`, including bitmap
mutation and composition recreation after template replacement. For the raster
comparison, fixture-owned timelines align to zero and resume afterward;
rotation is independently measured beforehand under the real system preference.
`Reference/BauhausNarrow.json` records 51 browser shape boxes across nine
type/size combinations at 72×144, 40×32 and 6×8. The native fixture compares
positions and sizes within 0.03 DIP for CSS's 1/64px layout quantization.
Consumer `Background` is applied to the root surface and is included in
the live backdrop blend; the reference includes a `#506030` override.
The final focused x64 run passes at `artifacts/smoke-bauhaus-final`, including
all nine narrow-layout comparisons and the consumer background override in
both appearances. The earlier complete catalog passes at
`artifacts/smoke-bauhaus-all`; the later geometry/background refinements are
covered by the focused run. All 27 unit/resource tests and token generation
checks pass. Both final x64 and ARM64 builds have zero warnings.
The fresh isolated x64 consumer passes both appearances at
`artifacts/package-consumer-bauhaus`, using only the 0.1.0 NuGet package with
SHA-256 `0b28d4409c323f5a19cc742dcdd1d7838ec72bf126f1300bafbb92d049855ef3`.
Its Bauhaus check verifies packaged template geometry, consumer source identity
and decorative semantics. Keep the pinned Win2D 1.4.0 native runtime files in
the complete application output. No source project reference satisfies this test.

Run `38465239245` at `c8a830c` passes the actual ARM64 Windows contrast
transition and independent consumer. Its native capture samples both opaque
and transparent Bauhaus regions as exact Window `#FF202020` pixels after
394.8ms. Archive `11849310010` was downloaded and verified against SHA-256
`156d95cbc3979ce2dbe6e5579796e29a7643cb4e79bd0d9ecf8bcc5c8f69f088`;
selected evidence is at `artifacts/ci-c8a830c-arm64-evidence`.
The ordinary ARM64 suite fails before Bauhaus because a WSL setup console takes
foreground during Alert's Info capture. That keeps the overall run red and
leaves ordinary ARM64 geometry/pixel/motion acceptance unverified.

The same run passes the full x64 catalog, live contrast and independent
consumer. All six Bauhaus pixel reports match all 32 samples, including live
bitmap mutation and the consumer background override in both appearances.
The x64 runner has animations disabled: its 12/13 Dark/Light motion samples
remain at zero. This proves reduced motion, not enabled rotation.
Its actual contrast samples are exact Window pixels after 98.6ms.
Archive `11848508435` was verified against SHA-256
`6a504bb1b3b1952b08d2f9f5420cd0077f14a23d34962f61fe3ca8f9895d0cc3`;
selected reports/captures are at `artifacts/ci-c8a830c-x64-evidence`.
The independent x64 consumer's package SHA-256 is
`ad578f7b1f5905d805ce4a897581220448f71226f8c80a19e8df501b63cdbf70`.

CI now runs the focused Bauhaus fixture before the full catalog on each native
architecture. A focused failure still fails the job; the full catalog,
contrast and independent consumer checks continue and keep their own gates.

Run `38467123988` at `05a2109` passes the focused native ARM64 Bauhaus fixture
with animations enabled. Its Light/Dark counter-rotation changes are
5.9909°/5.6938° over approximately 306ms, within the fixture's 1° presentation
tolerance of the 20-second linear source cycle. Both directions advance equally
and oppositely. Geometry, RGBA, source mutation, consumer background and
zero-timeline pixel checks pass in both appearances. Actual contrast and the
independent ARM64 consumer also pass. The full ARM64 catalog gets past Alert
then fails the Small/valid disabled-input cursor at bottom-right `3,0`.
The global cursor is Arrow while the publisher retains UniversalNo.
Archive `11849412343` was verified against SHA-256
`01dca2a6e08f49cc8f1d59399545d444b934c68717f6a47392a4d17bac0e6e02`;
selected reports are at `artifacts/ci-05a2109-arm64-evidence`.
The x64 job passes the full catalog, contrast and independent consumer.
These hosted checks precede the border-radius and raster-edge corrections.

## Full raster regression

The actual frozen React component, `cn` helper, generated tokens and global
CSS are now rendered together, using the source lock's React 19.2.4,
Tailwind/CLI 4.2.2, clsx 2.1.1 and tailwind-merge 3.5.0. TypeScript 5.9.3
transpiles the unchanged component for server rendering. Edge 155.0.4283.45
renders the resulting markup and CSS with animation timelines paused at zero.
This replaces inferred utilities as the authority for the full-image comparison.

`Reference/BauhausRaster.json` records those versions, geometry and curve
regression pixels. The `BauhausAutostrada.png` and `BauhausPompeii.png`
references cover every pixel of each 288×96 specimen. The native fixture
compares actual composed screen pixels using the element's measured origin;
it does not shift the crop to find a passing alignment. Both full rectangular
rasters must differ by no more than three RGB levels. Higher display scales
explicitly fail this reference check until their raster coverage is added.

Before correction, rounded contours differ by up to 95 RGB levels and the
square borders by up to 53. The corrected square raster differs by at most
three levels across all 27,648 pixels, and Autostrada by at most two.
Curved contours now have the intended geometry; residual edge antialiasing
differences reach 15/16 levels and remain an open acceptance gap. Fixed curve
points detect the prior contour error without declaring full curved raster
acceptance. Comparison artifacts are at `artifacts/bauhaus-raster-comparison.*`.

The focused x64 run at `artifacts/smoke-bauhaus-raster-regression` passes both
appearances: all Autostrada/Pompeii raster channels stay within 2/3 levels,
and all four contour regression points within one. The complete catalog also
passes at `artifacts/smoke-bauhaus-raster-all`, with animations off and display/
text scale 1. All 27 unit/resource tests pass. The full-image check is exercised
again after retemplating, so a successful initial composition alone cannot pass.
Both final architecture builds have zero warnings. A fresh isolated x64
consumer passes both appearances at `artifacts/package-consumer-bauhaus-raster`,
using the 0.1.0 package with SHA-256
`79a15fbce81b6990b514a35830352d8980e02096abdb757af162a692f8909002`.

Full curved antialiasing, other sizes, additional image decoders/animated
sources, component theme-resource behavior and display/text scaling remain
open. The complete required catalog still needs a green native ARM64 run.
Narrator and OS contrast campaigns are excluded by the
[current library scope](library-scope.md).

Run `38475433445` at `764f5b2` passes focused ARM64 Bauhaus and Input, then the
full catalog loses foreground immediately before a composed Bauhaus capture.
The capture correctly refuses an unowned foreground. The fixture now requires
the shared session's foreground guard after its timeline-alignment await and
immediately before capture. This uses the existing bounded recovery for a
foreign process with an owned point and no held button; the capture ownership,
global cursor, raster tolerances and all motion assertions remain unchanged.

The guarded focused x64 fixture passes both appearances at
`artifacts/smoke-bauhaus-foreground-guard`, with animations disabled and scale 1.

At `5ad9d0b`, run `38480092499` passes focused native Bauhaus on both
architectures but fails Dark motion in the full ARM64 suite. Over a
300.9109ms wall-clock span, the sampled outer/inner counter-rotation is
6.8230476°, against 5.4163998° expected from the 20-second cycle. The next
report records `Storyboard.GetCurrentTime()` alongside each wall-clock sample
and actual transform angle. The API is present in the pinned WinUI 2.3.9
metadata shipped by Windows App SDK 2.5.1 and reports the animation clock.
This is diagnostic evidence only: the wall-clock assertion, native preference
check and existing one-degree limit remain unchanged.

The diagnostic fixture builds for x64 and ARM64 and passes focused x64 at
`artifacts/smoke-bauhaus-animation-clock`. With local animations disabled,
the report records a null animation clock and zero angles in every frame.
The enabled ARM64 clock comparison remains pending the next hosted run.

Native ARM64 at `dc7815c`, run `38491762987`, records a failed full-suite Light
span: wall time 318.7628ms versus 379.0937ms of storyboard time. The transforms
advance 6.8236866 degrees, matching the storyboard's 18 degrees/second but
exceeding the wall-clock expectation 5.7376602 by the existing one-degree bound.
The animation samples now come from CompositionTarget.Rendering, starting their
wall timer with the first actual render frame. The wall-clock speed and native
reduced-motion assertions remain required and unchanged. Focused local x64
passes both appearances at `artifacts/smoke-bauhaus-render-frame-sampling` with
animations off. Enabled ARM64 verification of the sampling change remains
pending; this observation does not close the real-time motion acceptance gap.

The fresh focused native ARM64 run `38494432792` passes both appearances with
animations enabled. Light measures 6.5227375 degrees against 5.8510152 expected,
and Dark 5.9910774 against 5.9880096, both within the unchanged one-degree bound.
The full ARM64 suite stops earlier at Notifications, so it does not exercise the
integrated Bauhaus path. Full curved raster, exact visible motion and the rest
of the acceptance matrix remain open.
