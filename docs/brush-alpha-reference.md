# Ghost hover alpha diagnostic

The Light Ghost and IconGhost hover/pressed backgrounds encode the source's
12% sand as `Color="#1FE6D5B8" Opacity="1"`. Normal/disabled states, foregrounds,
borders, Dark palettes, contrast resources, consumer overrides and other button
variants retain their existing recipes. Page Header's back action and Data
Table Toolbar's Reset import these same generated palettes.

The correction follows actual native Reset paint, rather than a preblended
background color. In the isolated native specimen, physical pointer hover is
retained while its actual rendered brush cycles through the production,
legacy opaque-color/brush-opacity, rounded byte-alpha and floor byte-alpha
forms. Input still goes through the owned catalog window and the normal native
button; the diagnostic does not synthesize visual states or pointer events.
The original brush is restored afterward. The diagnostic is separate from the
measured full catalog run.

The frozen source remains Bleecker `a86059013cd59684ce452849278ea81e734f2066`.
The browser collector checks all toolbar dependency hashes, the compiled CSS,
bundle, bundled font hashes and pinned Playwright/Edge versions. It performs a
real Reset hover and checks the source's frozen computed background, trusted
pointer receipt and actual hit ownership. It also records `matches(':hover')`;
that query reports false in this Edge capture despite the retained computed
hover color and owned trusted hit, so it is not claimed as a hover assertion.
Source paint is captured from the original button as well as flat diagnostic
cells; neither copy is substituted for the other.

The Light original source button interior is RGBA `(252,250,246,255)` over
white. Legacy native brush opacity `.12` and explicit alpha 30 both give
`(252,250,247,255)`; alpha 31 gives the exact source channels. The source's
original Dark button and all three native forms give `(27,38,46,255)` over
`(13,24,33,255)`. This is evidence for the Light correction, not a universal
change to every translucent brush.

Flat cells deliberately cover the theme background, sand and black. Source
Light paint matches native alpha 31 in all three. A significant counterexample
is preserved: the source's flat Dark cell paints `(28,38,46,255)` over the
theme background, while its original rounded button paints `(27,38,46,255)`.
This occurs even when the swatch uses the original stylesheet's Oklab
`color-mix` declaration and inherited variables. Its cause remains unresolved.
Copying serialized computed Oklab values is also insufficient evidence; the
collector uses the original declaration and saves the original button PNG.

Reproduce with the pinned runtime paths documented by the workspace, after
building the x64 catalog:

```powershell
node scripts/reference/capture-brush-alpha.mjs --playwright-module <pinned-playwright-directory> --output artifacts/brush-alpha-browser
./scripts/smoke-catalog.ps1 -Executable <catalog-executable> -Component brush-alpha -ClientSize 1024,745 -ExpectedArchitecture X64 -Output artifacts/brush-alpha-native
node scripts/compare-brush-alpha.mjs --source artifacts/brush-alpha-browser --native artifacts/brush-alpha-native --output artifacts/brush-alpha-comparison.json
```

The comparator preserves raw channels, PNG hashes, all flat-cell differences,
and 4x3 interior patches from the actual source/native Reset captures, with
zero masks and tolerance. These patches prove only interior paint. Full
control edges, text, focus and motion still require the complete interaction
image comparisons. `buttonPaintPassed` describes the actual production Reset
patches in both themes. `passed` describes the stricter flat/original source
agreement; it remains false and the command exits 1 for the Dark counterexample.
`supportsProductionChange` remains false because the diagnostic alone cannot
establish complete component acceptance. The full native suite and interaction
matrix remain required for palette changes.

The public wiki is unavailable, so this evidence is documented in the
repository. There is no backend or Prometheus surface.

## Published checkpoint

The final binary source is `b28fac0c3bf72cbe8960c9550e7081795a2bd656`.
`artifacts/catalog-ghost-byte-alpha-diagnostic` passes the complete native suite
in 351.1146312 seconds at
`artifacts/smoke-ghost-byte-alpha-diagnostic-published-full`. All 1,028 PNG names
are preserved, all 174 protected Sunburst/Radial Bar glyph hashes are unchanged,
and all 1,732 pointer presentation gates, 1,738 same-target key actions, 62
clicks, 118 source toolbar cases, 28 tooltip gates and 16 final presentation
gates pass. The previous failed full run remains preserved and unexplained.

The matching 376-state interaction replay passes its checked boundaries in
55.809279 seconds at
`artifacts/smoke-ghost-byte-alpha-diagnostic-published-matrix`. Compared with the
previous published stationary-hover source images, 49 states improve, none
worsen and 327 remain identical in differing-pixel count. Total first-frame
differing pixels decrease from 741,517 to 622,366. Every complete source/native
pair still differs (358–4,750 pixels), all 26 known focus-removal differences
remain, and the source's one changing caret repeat remains recorded. The
14 initial native toolbar rectangles are unchanged from the previous catalog;
all 14 still differ from the browser. No mask or tolerance is introduced.
The actual production Reset interior patches match both source themes; the
Light legacy/floor negative controls still fail and the source Dark flat-cell
counterexample is retained in
`artifacts/ghost-byte-alpha-diagnostic-probe-comparison.json`.

All 79 regressions, token checks and x64/ARM64 solution builds pass. NuGet 0.1.0
at `artifacts/packages-ghost-byte-alpha-diagnostic` has SHA-256
`7ced03a87b55ded799ef6a0377ab681aa7130c3f29659b203f00e3df00269ab6`.
Fresh x64 runtime and ARM64 build-only consumers pass under
`artifacts/package-consumer-ghost-byte-alpha-diagnostic` and its `-arm64`
sibling; both temporary restore caches are removed. Package, catalog and both
consumers have identical Orchard DLL SHA-256
`e0fa47a03ba224630a7d707940c427bf7f3df841cb48ccf2657d50952dc71e50`,
ProductVersion `0.1.0+b28fac0c3bf72cbe8960c9550e7081795a2bd656`.
Native ARM64 execution, full raster/motion, contrast/Narrator/scaling and the
remaining acceptance matrices stay open. Inventory remains 50 in progress,
62 pending and none accepted out of 112. Push Actions remain paused.
