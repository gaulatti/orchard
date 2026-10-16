# Data list

`BleeckerDataList` implements the frozen Bleecker `DataList` contract from
`a86059013cd59684ce452849278ea81e734f2066`, source SHA-256
`23da195177b59a57fb1031ba4c8fdd900f7484febc4d73d13b96770cc55bde5a`.
It presents consumer-owned terms and values without inventing selection,
sorting, loading, error messages or row actions. Embedded controls retain their
own commands, focus, keyboard behavior and accessible names.

```xml
<orchard:BleeckerDataList IsStriped="True" AutomationProperties.Name="Metadata">
 <orchard:BleeckerDataList.Items>
  <orchard:BleeckerDataListItem Id="title" Label="Title" Value="Harbor dispatch"
      Description="A fictional editorial record" />
 </orchard:BleeckerDataList.Items>
</orchard:BleeckerDataList>
```

`Items` is an observable collection of `BleeckerDataListItem`. Each item has an
exact, unique `Id` and nullable `Label`, `Value` and `Description` objects, with
property-change notification. Strings, numeric scalars, booleans and native
`UIElement` content are supported directly. Other view models require the
control's `LabelTemplate`, `ValueTemplate` or `DescriptionTemplate`; unsupported
objects fail explicitly. The model keeps original consumer values. Only text
presentation collapses CSS whitespace. Null and boolean text have no glyphs;
zero remains visible. A numeric zero/NaN description follows the source's
conditional expression and appears as anonymous value text, without a summary
paragraph. A whitespace summary keeps its four-pixel margin and empty line box.

At a window content width of at least 640 DIPs, the label/value columns divide
the remaining width in a 0.8:1.2 ratio with a 24-DIP gap. This breakpoint follows
the viewport, including exactly 640, rather than the list's allocated width.
Below it, terms and values stack with a six-DIP gap. RTL mirrors the columns;
wide values remain physically right-aligned, as in the source. Rows have
14-DIP vertical padding and one bottom divider except on the last row.
`IsStriped` adds a six-DIP radius and 12-DIP horizontal padding to even-indexed
rows, using light-sand/40 in Light and white/3 in Dark. Typography is Encode Sans
Medium 13/19.5 for terms and 14/20 for values, with Libre Franklin 12/20 for
summaries. The stripe palette has an explicit Windows contrast adaptation.

Scalar text uses the selected font's measured natural ascent and descent,
quantized at the current display scale, to place its baseline inside the CSS
line box. Compact rows retain the half-DIP origins produced by the term's
19.5-DIP line height. Font, font-size and text-scale changes refresh the
measurement. Consumer `UIElement` and template content keeps its own layout.

The native automation hierarchy exposes a list and ordinal list items, keeping
embedded consumer controls reachable. Changes, reorder, removal, template
replacement, unloading/remounting and theme changes preserve the relevant
consumer content. Native rendering supplies text/display scaling. Final
Narrator, contrast, scaling and ARM64 runtime verification remain required.

The registered `DataListsFixture` includes a source-case picker and frozen
normal, striped, raw scalar, multilingual, narrow, RTL, empty and embedded-action
examples. `scripts/reference/capture-data-list.cjs` captures the actual frozen
React component with pinned React, esbuild, Playwright, Edge, CSS, font bytes
and lockfile provenance. `tests/Reference/DataList.json` records 72 cases across
both themes and viewports 639, 640 and 1024, plus six consumer-action checks.
These browser cases establish the source contract; native visual acceptance
requires comparison with their unchanged screenshots.

Run native checks with `scripts/smoke-catalog.ps1 -Component data-list`, and
repeat with `-ClientSize 639,1000` and `-ClientSize 640,1000` for the breakpoint
matrix. The full catalog must retain all existing captures and input checks.
The component remains in progress until runtime, raster, content/template,
accessibility, scaling and platform evidence satisfies the fixed inventory.

Initial x64 evidence in `artifacts/data-list-matrix/{1024,639,640}` passes all
72 source geometry cases at the original 0.05-DIP bound. Each process verifies
both themes, pointer/Space/Enter activation of the original embedded action,
inherited disabled state, live metadata, keyed reorder/removal, template
replacement and unload/remount. Three content templates retain their original
data contexts and live bindings. A real 639→640→639 window resize changes the
layout without remounting the row and restores the original window size.

The unmasked image audit at `artifacts/data-list-pixel-audit.json` compares all
72 source/native images with zero tolerance and no resampling. Six empty
frames match; the other 66 differ, totaling 2,536,268 differing pixels and
114,743,118 absolute RGBA error. Text baselines, font/shape rasterization and
alpha paint remain open. Both architecture builds, 108 unit tests and token
generation pass. This initial matrix does not establish ARM64 runtime,
Narrator, Windows contrast or complete scaling/override acceptance.

The initial embedded-action runs are retained in `artifacts/smoke-data-list-v1`
and `v2`. A managed stack in `artifacts/data-list-action-stall-stack.txt` locates
the stall in the rich slot's native measure loop. Measuring its collapsed
scalar child resolves the pending-layout loop; the same embedded-action
boundary passes in the complete matrix. No deadline was increased.

The clean published `a7dc3ca` replay at
`artifacts/data-list-release-matrix/{1024,639,640}` passes the same 72 cases.
Its pixel audit reproduces the initial totals exactly. The full published
catalog passes 1,104 captures in 437.5147483 seconds, preserving all 1,080 prior
names and 174 protected glyph hashes. The independent package consumer passes
its DataList checks and an instrumented complete attempt; the two earlier Bar
Chart focus failures were later repaired and verified separately. See
[the current checkpoint](native-validation-checkpoint.md)
for separate package failure/success evidence, artifact hashes and open gaps.

`scripts/reference/capture-data-list-typography.cjs` measures original DOM text
ranges and Canvas font bounds without inserting baseline markers or changing
the frozen component. `tests/Reference/DataListTypography.json` records 402
first-line baselines: 396 scalar baselines and six consumer-button baselines.
The original native implementation fails all 396 scalar checks, by up to
2.068001 DIPs. The corrected x64 matrix at
`artifacts/data-list-baseline-local/{1024,639,640}` passes all 396 at the original
0.05-DIP geometry bound; maximum error is 0.0000124 DIP. The native fixture
asserts scalar baselines and line heights, and the independent
`scripts/verify-data-list-typography.mjs` audit checks the same emitted values.
Consumer-button measurements remain diagnostic and are not shifted by the
library. Both native CI jobs register the audit; push runs remain paused.

The unchanged zero-tolerance image comparison at
`artifacts/data-list-baseline-local-pixel-audit.json` still finds 66/72 differing
cases: 2,496,401 pixels and 76,288,483 absolute RGBA error. That is 39,867 fewer
differing pixels and 38,454,635 less error than the initial matrix. Exact font,
shape and alpha rasterization, wrapped-line detail, consumer/override coverage,
scaling, contrast/Narrator and ARM64 runtime remain open. These baseline checks
do not accept the component or its remaining platform requirements.
