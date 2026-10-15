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
