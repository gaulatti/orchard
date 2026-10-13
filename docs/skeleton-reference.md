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
Local reference evidence is retained in `artifacts/skeleton-reference-contract.json` and
`artifacts/skeleton-reference-{Light,Dark}.jpg`; the source harness is
`artifacts/font-audit/skeleton.html`. These captures use the existing local
reduced-motion preference. Normal pulse raster timing, Windows contrast,
native lifecycle, consumer count changes, package consumption and all Windows
visual/accessibility/scaling checks remain unverified. The public wiki remote
is unavailable; the contract is recorded in this repository.

Use `BleeckerSkeleton` with standard WinUI `Width` and `Height`, and set `Circle`
for circles or capsules. `BleeckerSkeletonCard.Rows` defaults to 3;
`BleeckerSkeletonTable.Rows` and `.Columns` default to 5 and 4. Counts are
integers. Negative counts render zero items, as the producer's `Array.from`
does; the supplied dependency-property value and consumer binding are retained.
All three controls are decorative and absent from the UIA control/content views.
Applications announce the associated loading operation through their own status
region. Skeleton shapes do not add focus stops or disabled dimming.

The default templates consume centrally generated theme brushes and canonical
7px/12px radii. High contrast uses an opaque system window-text shape and system
window surface/ring. Card shadows use the pinned two-layer small recipe and are
removed in high contrast. Each shape owns its pulse on the template surface,
preserving consumer opacity. Motion stops and resets to opacity 1 when hidden,
unloaded or retemplated; Windows reduced motion rests at opacity 1.

The catalog's Skeletons page registers every export and the nine reference
cases. Its native check exercises equal-column allocation, card proportions,
200px constrained widths, bound count updates, negative counts, capsule resize,
decorative peers, pulse activity/reset and detached template replacement.
It retains six element captures, four composited window captures with target
rectangles and `skeleton-measurements-{Light,Dark}.json`. The clean NuGet consumer
instantiates all three controls. Hosted x64/ARM64 execution remains required;
the source audit and successful compilation do not establish visual acceptance.
Exact native/browser raster parity, full pulse timing, contrast/Narrator and
display/text scaling remain acceptance gaps.

At `4de3160`, both native jobs reach the capture boundary after the Light
geometry, consumer-count, decorative-peer, motion and template-lifecycle checks.
Six element images are retained, then `RenderTargetBitmap` reports no rendered
pixels for the entirely transparent zero-column table. The fixture now preserves
that source behavior and captures its allocated 320×40 region in a composited
window image. The global nonblank-image gate remains unchanged for rendered
controls. Capture-stage failure reports retain the completed checks and images.
Dark and package runtime verification were not reached in that run.

The implementation keeps Windows App SDK 2.5.1, documented under Microsoft's
[2.0 release family](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0).
