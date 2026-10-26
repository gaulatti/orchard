# Pinned Skeleton contract

Producer: `src/components/skeleton.tsx` at Bleecker
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`acc108ca2c34011adfaf1792d7f758c87e0dce9dfd65a3642ca55e7f7bd0837b`.
Its dependency is Tailwind CSS 4.2.2. The three native controls are previews;
Windows acceptance remains open.

`Skeleton` accepts consumer width, height, circle, style and div attributes.
Rectangle corners use the 7px UI radius. Circle uses a full radius: unequal
80×40 dimensions produce a capsule, rather than an ellipse. Background is sand
at 20% in Light and white at 10% in Dark. The normal pulse repeats every 2s
with cubic-bezier(.4,0,.6,1), moving opacity 1 → .5 → 1, with the low endpoint
at 1s. Bleecker's published global reduced-motion rule changes animation to
one 0.01ms iteration; the inspected reduced-motion reference rests at opacity 1.
[CSS normalizes overlapping corners](https://www.w3.org/TR/css-backgrounds-3/#corner-overlap)
by scaling all radii together. A 12px-high
shape paints 6px circular corners; an 8px-high shape paints 4px corners. The
native template normalizes its painted radius without changing the consumer's
`CornerRadius` value. The fixture checks both thin shapes and composed body rows.

`SkeletonCard` defaults to three body rows. It has a 12px radius, 24px padding,
opaque card background and an outer 1px ring: black at 8% in Light, white at
8% in Dark. Its Tailwind small shadow has two black layers: (0,1,3,0) at 10%
and (0,1,2,-1) at 10%, expressed as x/y/blur/spread. The ring does not add to
layout dimensions. The header has a 40×40 circle, a 12px horizontal gap and
a remaining-width text column. Its first shape is 16px high at one third of
the text column, its second is 12px high at half the text column, separated
by 8px. The header centers the 36px text column within its 40px height.
Body rows are 12px high, full width, with 12px vertical gaps between all main
sections. At 320px width, zero/three/six body rows measure 88/160/232px high;
the text shapes measure 73.328125/110px after browser layout quantization.

`SkeletonTable` defaults to five body rows and four equal-width columns.
Horizontal and vertical gaps are 8px. Header shapes are 32px high and body
shapes are 40px high. At 320px width the default is 272px high, with 74px
columns. Zero body rows retain the header: two columns measure 156px each
and the composition remains 32px high. Zero columns with five rows leaves
six empty row containers and preserves 40px of vertical gaps; it does not
collapse the entire composition to zero height.

The actual source was inspected in both appearances without component restyling.
The current reproducible contract is [Skeleton.json](../tests/Reference/Skeleton.json),
with the pinned [component source](../tests/Reference/Skeleton/source.tsx) and
[token CSS](../tests/Reference/Skeleton/tokens.generated.css). The original
`artifacts/skeleton-reference-*` images and font-audit harness are no longer
present at their documented paths. Normal pulse timing and full Windows
visual/scaling checks remain unverified. OS contrast and Narrator campaigns are
excluded by the [library scope](library-scope.md). The native runtime
checkpoint below covers lifecycle, consumer counts and package consumption.
The public wiki remote
is unavailable; the contract is recorded in this repository.

Use `BleeckerSkeleton` with standard WinUI `Width` and `Height`, and set `Circle`
for circles or capsules. `BleeckerSkeletonCard.Rows` defaults to 3;
`BleeckerSkeletonTable.Rows` and `.Columns` default to 5 and 4. Counts are
integers. Negative counts render zero items, as the producer's `Array.from`
does; the supplied dependency-property value and consumer binding are retained.
All three controls are decorative and absent from the UIA control/content views.
The Skeleton wrapper does not hide caller-owned child controls from those views.
Applications announce the associated loading operation through their own status
region. Skeleton shapes do not add focus stops or disabled dimming.

## Caller content

The pinned source spreads its div attributes, including React children, onto
the Skeleton element. `BleeckerSkeleton` therefore derives from `ContentControl`
and exposes `Content`, `ContentTemplate` and `ContentTemplateSelector`. Its
background and caller presentation are separate template siblings, so the
loading surface does not replace the original child subtree. Padding and
horizontal/vertical content alignment use standard native properties.

```xml
<orchard:BleeckerSkeleton Width="280" Height="140" Padding="12">
    <StackPanel Spacing="8">
        <TextBox Text="Fictional draft" />
        <Button Content="Invoke caller action" Command="{Binding CallerAction}" />
    </StackPanel>
</orchard:BleeckerSkeleton>
```

The catalog includes this caller-content example. The independent consumer also
compiles nested XAML content. The shared hidden fixture checks original content,
live bindings, editor state, native child actions/peer trees, template clearing
and restoration, original template/selector models, and opacity bindings across
unload/remount. It preserves the native content-loading behavior when a template
is cleared; it does not require an artificial unload event.

The old published `c0597e9f4cb9499cc09aafb59da2b321f6e302c7fe11eecf5d08a357a9110483`
library fails the new content-slot regression as expected. The
`0.1.0-skeleton-content-v1-local` candidate passes 36 default shape/card/table
states and 16 caller-content checks in both the x64 catalog and independent exact
NuGet consumer. Each host retains 40 images, verifies 236 source palette samples
and 51,200 empty-table pixels, and rejects 18 corrupted reports. The old library
and the first failing candidate reports remain retained.

Run `scripts/verify-skeleton-content.ps1 -AppDirectory <current-app> -Output <fresh-directory>`;
use `-HostName PackageConsumer` for the independent consumer. Add an immutable app
manifest with `-ImmutableManifest` when using a frozen qualification app. The
runner requires its recognized hidden command and never opens the default app,
shows/focuses a window, sends hardware input or changes OS settings. This new
command is absent from the earlier October 25 preview ZIP.

These checks qualify the currently reduced-motion environment. All 51 retained
older motion traces also have animations disabled, so they cannot prove the
normal 2-second curve. Current normal timing, live OS preference transitions and
native ARM64 execution are not claimed.

The caller-opacity images are consistent with native per-primitive opacity
multiplication; that explanation is inferred from their raw samples. They differ
from CSS isolated-group compositing by up to four channel values in the selected
sample. The library retains native WinUI opacity behavior. The disagreement is
preserved in `artifacts/current/skeleton-content-caller-opacity-review-v1.json`;
isolated-group raster parity and on-screen compositor behavior remain unverified.
The distinction between layer and multiply opacity is documented by
[DirectComposition](https://learn.microsoft.com/en-us/windows/win32/api/dcomptypes/ne-dcomptypes-dcomposition_opacity_mode).
Default geometry/brush checks retain their original tolerances. No pixel masks,
resampling or custom opacity renderer are used to hide this difference.

The default templates consume centrally generated theme brushes and canonical
7px/12px radii. High contrast uses an opaque system window-text shape and system
window surface/ring. Card shadows use the pinned two-layer small recipe and are
removed in high contrast. Each shape animates its own opacity temporarily,
preserving the underlying consumer value or binding. The omitted CSS 0%/100%
keyframes use that underlying opacity, while the explicit 50% keyframe is .5.
For example, a consumer opacity of .25 pulses between .25 and .5; multiplying
an internal pulse by .25 would produce a different result. Live base-value
changes rebuild the native pulse with the new consumer endpoint and retain its
playback time. Motion stops
on hide (including an ancestor), unload or retemplating and restores the consumer opacity; reduced
motion retains that value (1 by default). Native fixtures check live .75/.25
bindings, animated ranges, ancestor hide/show, restoration and updates while unloaded.
This follows the producer's
[implicit CSS endpoint rule](https://www.w3.org/TR/css-animations-1/#keyframes)
using WinUI's
[animation base value](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.dependencyobject.getanimationbasevalue?view=windows-app-sdk-2.0).

The catalog's Skeletons page registers every export and the nine reference
cases. Its native check exercises equal-column allocation, card proportions,
200px constrained widths, bound count updates, negative counts, capsule resize,
decorative peers, pulse activity/reset and detached template replacement.
It retains six element captures, four composited window captures with target
rectangles and `skeleton-measurements-{Light,Dark}.json`. The clean NuGet consumer
instantiates all three controls. The complete catalog and both independent
consumers pass at `291fee5`; each architecture/theme retains 29 branded center
pixel samples and verifies 12,800 opaque canvas pixels in the transparent table
region. The control-level pulse's live consumer endpoints and ancestor visibility
checks pass in both themes on x64 and ARM64 at `71a7fb8`, including normal-motion
.75/.25 bindings and restoration while hidden/unloaded. The earlier `61d13aa`
ARM64 run failed an Input cursor phase before reaching Skeleton; that issue did
not recur and its cause remains unresolved. See [checked runtime evidence](runtime-evidence.md).
The source audit and successful compilation do not establish visual acceptance.
Those historical architecture-labelled results do not establish current native
ARM64 execution. Full source-derived visual review, normal pulse timing and
display/text scaling remain open. Exact-raster diagnostics retain their original
scope under the [acceptance contract](acceptance-contract.md).

At `4de3160`, both native jobs reach the capture boundary after the Light
geometry, consumer-count, decorative-peer, motion and template-lifecycle checks.
Six element images are retained, then `RenderTargetBitmap` reports no rendered
pixels for the entirely transparent zero-column table. The fixture now preserves
that source behavior and captures its allocated 320×40 region in a composited
window image. The global nonblank-image gate remains unchanged for rendered
controls. Capture-stage failure reports retain the completed checks and images.
Dark and package runtime verification were not reached in that run.

At `e353488`, ARM64 completes Light and reaches the Dark table capture after all
geometry, binding, semantics and lifecycle checks. The sampled normal pulse
ranges from 0.5015 to 0.9994 opacity. The generic catalog image heuristic requires
more than eight distinct RGB byte values; a valid low-opacity white table can
have fewer values after premultiplication. The dedicated shapes/table capture
now checks every shape's actual center pixel against the branded brush, positive
alpha and normal/reduced pulse range. Independent alpha/color quantization allows
at most one byte of premultiplication rounding error. Cards retain the original
complex-image gate; the entirely transparent zero-column case retains its
composited capture and empty-layout assertions. Rendered sample values are
retained in the report's `paintSamples` list. Both architectures pass this
capture boundary at `291fee5`.
The empty-table capture also checks every pixel of its allocated region against
the pinned canvas color, requiring opaque composited pixels and no painted
content. The report records the number of verified empty pixels.

The aligned reference harness places all nine cases at integer coordinates on
the pinned `--surface-canvas` background, retaining metadata and images under
`artifacts/skeleton-aligned-reference-{Light,Dark}`. Browser screenshots are JPEG
encoded and retained with `.jpg` filenames. Their compression creates pixel
differences and these images support qualitative composition inspection only;
they do not establish exact raster parity. Native captures remain lossless PNG.

The implementation keeps Windows App SDK 2.5.1, documented under Microsoft's
[2.0 release family](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0).
