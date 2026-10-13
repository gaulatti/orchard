# BauhausBackground reference and acceptance

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

Enabled rotation timing, all geometric border/antialias pixels,
additional image decoders/animated sources, all contrast themes/states,
Narrator, display/text scaling and ordinary native ARM64 verification remain open.
