# ProgressStack native preview

`BleeckerProgressStack` displays consumer-owned `Items`, an optional `Max`,
and a wrapping legend (`ShowLegend`, true by default). Each observable
`BleeckerProgressStackItem` retains its `Label`, numeric `Value`, and optional
`Color` brush. Missing colors remain transparent. Replacing, reordering,
removing and editing items update the display without adding a command or
keyboard stop. An explicit automation name labels the passive group.
Collection and item updates use the control's owning UI thread.

```csharp
var stack = new BleeckerProgressStack { Max = 20 };
stack.Items.Add(new BleeckerProgressStackItem
{
    Label = "Published", Value = 6, Color = publicationBrush
});
stack.Items[0].Value = 8;
```

The same optional maximum can be set declaratively with `Max="20"`.
An item retains the original numeric bits, including negative zero, while
its visible label and tooltip use the source's Number text (`0`).

The pinned Bleecker source is `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/progress-stack.tsx`, SHA-256
`101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254`.
The original source defines a 12px track, 12px track/legend and wrapping gaps,
10px swatches, 6px item gaps and 12px/16px legend typography. Values use React
Number text, including zero, nonfinite numbers and exponent boundaries.

An omitted maximum sums the values in input order. A supplied maximum is
authoritative, including zero, negative and nonfinite values. Invalid or
negative CSS widths occupy no segment width; overflowing positive segments
shrink in proportion to their flex basis. A visible empty legend retains the
12px inter-block gap. A zero-width track still permits legend overflow.
The width behavior follows the [CSS flexible length algorithm](https://www.w3.org/TR/css-flexbox-1/#resolve-flexible-lengths)
and is verified against the pinned component's browser output.
Chromium's [length representation](https://github.com/chromium/chromium/blob/main/third_party/blink/renderer/platform/geometry/length.h)
stores percentages as floats. The frozen small-share case verifies that
rounding before allocation matters: the remaining share can become 100%.

Consumer bar customization uses `TrackHeight`, `CornerRadius` and `Background`.
The default track retains the source's exact 20% alpha as a color alpha byte
(`51/255`), with brush opacity at one. On this WinUI version, using an opaque
color with brush opacity `.2` truncates premultiplied channels and darkens the
light track. Consumer-supplied backgrounds retain their original brush.
The inherited font properties and foreground customize value typography;
labels retain the theme's secondary foreground. Brushes keep their identity.
There is no component-owned transition. Tooltips expose the original label
and Number text; independent native tooltip interaction is still unverified.

`ProgressStacksFixture` registers default, explicit maximum, hidden-legend
and wrapped cases, plus narrow Japanese and discretionary-hyphen examples.
`tests/Reference/ProgressStack.json` freezes 140 browser
frames and sixteen live update receipts from the original source, with exact
collector, fonts and complete PNG hashes. The native 140-state matrix and
twelve lifecycle phases pass with zero asserted geometry error. Complete
PNG comparisons still report raster differences, with no masks, tolerance
or resampling. Version `0.1.0-77f387a` passes a fresh independent x64 consumer in both themes and an ARM64 consumer build; native ARM64 runtime is unverified. No component is
accepted from a build or a geometry report alone. Complete raster parity,
arbitrary Unicode, remaining item/bar style overrides, external UIA/Narrator,
scaling, contrast, collection measurements and native ARM64 runtime remain
acceptance work.

All thirteen corrupted baseline audit reports are rejected, including
nonbreaking-space removal. Native and browser glyph receipts record the
57.53125px nonbreaking-space span width.

The Unicode candidate adds `tests/Reference/ProgressStackText.json`: 72 frozen
source frames covering mixed scripts and RTL, CJK punctuation, Hiragana,
Hangul, Arabic, Hebrew, combining marks, emoji graphemes, zero-width space,
word joiner, nonbreaking space, discretionary hyphens and Latin punctuation.
Each text field retains min/max-content widths, grapheme range rectangles and
platform-font receipts. The original source, font and collector hashes and
all PNG bytes are verified by the ordinary contract tests.

The Windows-only `Orchard.Text.Tests` suite creates no window and changes no
input state. It exercises the actual Win2D 1.4.0 shaping and break analysis
used by the control. Minimum/maximum widths and wrapped lines match all 72
source states. Line breaking respects grapheme boundaries, punctuation and
nonbreaking controls. It uses DirectWrite breakpoints with the pinned
browser's default CSS tailoring for Unicode CJ and solidus/letter boundaries;
see [CSS line breaking](https://www.w3.org/TR/css-text-3/#line-break-property)
and [Unicode 17 line-break properties](https://www.unicode.org/Public/17.0.0/ucd/LineBreak.txt).
Glyph advances round once for the inline's maximum width. A fragment within a
shaping run uses the difference between rounded glyph positions, preserving
kerning at punctuation boundaries. Existing consumers of per-run width keep
their measurement behavior. Zero-width break controls are interpreted by
layout, and a taken soft-hyphen break paints U+2010. Item labels and tooltip
strings retain the original Unicode code units.

Accessible text names retain the normalized logical label and value, including
nonbreaking controls and discretionary soft hyphens. Layout-inserted newlines
and visible U+2010 hyphens do not become part of those names. The existing native
TextBlock TextPattern continues to expose the painted text and its native
geometry. This follows the distinction between an accessible name and displayed
text in [Microsoft's TextBlock automation contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.textblockautomationpeer?view=windows-app-sdk-2.0).
The dependency pins remain Windows App SDK 2.5.1 and WinUI 2.3.9; the provider
behavior is checked on the actual installed runtime.

`ProgressStackAccessibility.json` freezes the original browser's 144 StaticText
names across all eighteen Unicode cases and both themes, with links to the
unchanged source PNG hashes. The separate native accessibility mode uses an
external UIA process, checks the passive group and all text nodes, retains a
label provider across a live name/value update, and observes the actual Name
property event. Width changes, reorder, hide/show, remount, retemplate and clear
are additional boundaries. Before the name fix, thirty of the 36 native cases
exposed painted text instead of source names. The corrected local candidate
passes all 144 text nodes and eighteen update states, including two external
Name-event transitions; all twelve corrupted audits are rejected. A source-name
match does not prove Narrator's spoken output, text-range navigation or complete
accessibility acceptance.

The logical-name candidate `0.1.0-a11y-c5495bca3e4b4` passes 190 portable tests,
four Windows text tests, x64/ARM64 solution builds and fresh independent consumer
builds. The x64 consumer runs in both themes, checking the original logical
names alongside visible CJK/soft-hyphen lines, tooltips and retained focus;
ARM64 remains build-only. Its focused native replay passes all 212 states and
26 lifecycle phases with zero asserted geometry error. The same fresh x64
package build passes the hidden layout/font/update/paint diagnostic without
activation or input.

The full native replay initially timed out after 162 completed phases. After
the idle MSBuild and compiler servers were shut down, a separate replay passed
all 168 phases and 1,160 captures in 476,267.6943ms, retaining all 174 protected
glyph hashes and the original input checks. The watchdog remains 480,000ms;
both attempts are retained, and timing margin remains a limitation. Mouse
settings and pointer position are restored in both attempts.

Compared with the preceding native paint candidate, 187 of the 212 complete
focused frames are byte-identical after RGBA decoding. The other 25 differ by
41 pixels total, with maximum channel difference one. These differences remain
open; their cause is not established, and no tolerance is applied. This native
comparison does not replace the larger unresolved browser/native raster
differences or confer component acceptance.

```powershell
./scripts/smoke-catalog.ps1 -Executable <catalog-exe> -Component progress-stack-accessibility -ClientSize 640,400 -Output artifacts/current/progress-stack-accessibility -ExpectedArchitecture X64
node scripts/verify-progress-stack-accessibility.mjs --native artifacts/current/progress-stack-accessibility --output artifacts/current/progress-stack-accessibility/audit.json
node scripts/test-progress-stack-accessibility-audit.mjs --native artifacts/current/progress-stack-accessibility --output artifacts/current/progress-stack-accessibility/negative.json
```

A second background boundary check feeds the item specs through production
number formatting, whitespace processing, native shaping, min-content sizing,
item shrink allocation and legend wrapping. All 212 original/Unicode states
match the source root, legend, item, swatch, label and value rectangles. The
test derives allocated widths from the input data; expected browser rectangles
are only assertions. RTL, zero-width overflow, hidden/empty legends, nonfinite
values and custom track height are included. The native panel consumes the
same `BleeckerProgressStackLayout` allocator and placement code. Text minima
and maxima are cached only for the current text/style/scale measurement;
item or style updates and effective text-scale changes replace that object.
These checks do not exercise XAML measurement, presentation or input. The
independent package consumer additionally checks narrow CJK and discretionary
hyphen lines, original tooltip text and retained focus in both themes; its
fresh x64 runtime replay passes those checks with the current candidate.

Glyph-run character positions and logical-to-painted UTF-16 offsets are
indexed once for the current text measurement. The index spans all fallback
font runs, retaining each cluster's original run and local position so that
single-run rounding remains distinct from combined advances. Fragment queries
use binary search and prefix differences; they do not scan every glyph or
font run or allocate a new text prefix for each break opportunity. The prior candidate
took a median 809.62ms for minimum width and 849.15ms for wrapping an
8,000-character CJK label, with 137,979,016 managed bytes allocated. On the
same host, the indexed candidate takes 3.97ms and 2.00ms, with 278,672 managed
bytes. The probe uses three trials at each of 500/1,000/2,000/4,000/8,000
characters; widths and complete line hashes are identical for all fifteen
paired samples. A separate fifteen-sample probe alternates Microsoft YaHei
and Yu Gothic on every character. Indexing across runs reduces the 8,000-character
min/wrap calculation from 1,413.62ms/1,480.94ms to 2.12ms/0.81ms, retaining
identical widths and line hashes. Its earlier per-run lookup still visited
every font run on each query. Timings are diagnostic and exclude shaping, presentation
and XAML measurement. A native regression instead gates query CPU below one
second and managed allocation
below 1 MiB at 8,000 characters and below sixfold growth from 2,000 characters.
It covers both single-font and alternating-font labels. Rendered collection
performance remains unverified.

```powershell
dotnet test tests/Orchard.Text.Tests/Orchard.Text.Tests.csproj -c Release
./scripts/smoke-catalog.ps1 -Executable <catalog-exe> -Component progress-stack-text -ClientSize 640,400 -Output artifacts/current/progress-stack-text -ExpectedArchitecture X64
node scripts/verify-progress-stack.mjs --text --native artifacts/current/progress-stack-text --source tests/Reference/ProgressStackText --output artifacts/current/progress-stack-text/audit.json
node scripts/test-progress-stack-audit.mjs --text --native artifacts/current/progress-stack-text --source tests/Reference/ProgressStackText --output artifacts/current/progress-stack-text-negative
```

The local candidate `0.1.0-paint-c5adc443e9786` passes the visible 140-state
baseline with twelve lifecycle phases and the 72-state Unicode matrix with
fourteen lifecycle phases, both with zero asserted geometry error. Each audit
rejects all thirteen corruption cases. A fresh independent x64 package consumer
passes in both themes, including retained focus. The full native catalog replay
completes 168 phases and 1,160 captures in 477,696.1136ms under the unchanged
480,000ms watchdog. Its 2,040 pointer moves/presentation gates, 1,778 key actions,
96 clicks and both Date closing pointer pairs pass; all 174 protected chart
glyph hashes are unchanged. Scoped mouse settings and pointer position are
restored. These are local uncommitted candidate results, separate from the
earlier `77f387a` release. Complete raster parity, arbitrary Unicode beyond the
captured cases and the other acceptance gaps listed above remain open.

The separate hidden-window package diagnostic now loads and templates the
real WinUI controls, then explicitly measures and arranges all 212 source
states. It compares geometry, theme paints, exact Unicode line strings,
original item labels/tooltips and passive automation metadata with the same
frame contract as visible verification. It sends no input and never calls
`Activate` or `Show`; visibility and foreground ownership are guarded during
every layout cycle. This is actual XAML layout evidence through the
independent package. Physical input, Narrator, visible presentation and native
ARM64 acceptance remain separate requirements.

```powershell
./scripts/verify-hidden-progress-stack.ps1 -Package <candidate-nupkg> -Platform x64 -Output artifacts/current/package-unicode-x64
node scripts/verify-hidden-progress-stack.mjs --input artifacts/current/package-unicode-x64/hidden-progress-stack.json --reference tests/Reference --output artifacts/current/package-unicode-x64/hidden-layout-audit.json
node scripts/test-hidden-progress-stack-audit.mjs --input artifacts/current/package-unicode-x64/hidden-progress-stack.json --reference tests/Reference --output artifacts/current/hidden-progress-stack-negative
```

The wrapper restores/builds the package consumer in a fresh isolated temporary
workspace and removes that source/cache before launching the hidden diagnostic.
`result.json` continues to say `runtimeVerified: false`; the hidden layout
report and audit have their own explicit scope. It requires a matching native
OS/process architecture, so ARM64 can be built on x64 but this wrapper refuses
to count an x64-hosted process as native ARM64 execution.

Live `FontFamily` changes also invalidate plain-caption presentation when its
characters stay the same. Moving from Orchard's Encode Sans to a consumer
family clears the old script-specific inline faces; restoring the Orchard
family reapplies its captured faces. This fixes stale CJK/Arabic font runs in
ProgressStack, Empty and IconBadge. The hidden consumer records all eighteen
original/custom/restored states across those three controls and both themes.
Registered catalog examples include consumer Arial captions as well.

The hidden diagnostic also captures all 212 complete 640 by 400 frames with
`RenderTargetBitmap`. Captures retain their original dimensions, PNG hashes
and full RGBA differences from the frozen browser PNGs. No masks, tolerance,
resampling or pixel substitution are applied. All 212 still have raster
differences; source raster parity remains false. These offscreen captures do not replace
the visible verifier's screen captures.

Sixteen update receipts additionally compare the same control's observable
collection after reorder, remove, hide and clear against the frozen browser
updates, across both themes and both frozen motion variants of the passive
source. This hidden diagnostic does not change Windows animation preferences.
The native mutations
retain the original collection and surviving item objects. Geometry, colors,
labels, values, tooltips and empty/hidden legend spacing use the shared source
contract. This diagnostic sends no focus or keyboard operations.

The visible checker retains its original complete-PNG requirement and thirteen
corruption checks. The hidden checker additionally rejects altered PNG hashes,
dimensions, filenames and bytes, missing collection updates, lost consumer
ownership, reversed display order and incorrect empty legend spacing. The
x64 run passes all layout, font, collection and paint checks with 1,928 visibility/
foreground guards, zero activation events and an unchanged foreground handle;
all thirty corruption cases are rejected, including a valid PNG/hash pair
containing the wrong paint pixels.
The x64 and native ARM64 CI jobs include the hidden package diagnostic; no
workflow was dispatched, and push-triggered Actions remain paused.

`ProgressStackPaint.json` isolates that default brush in ten 40 by 40
squares inside complete 640 by 400 captures: both theme colors over
transparent, white, black, dark and blue backgrounds. Browser and native use
the same full reference context. The collector uses the unchanged computed track CSS from the
frozen component reference and records its hash, collector hash and complete
PNG hashes. The package diagnostic paints each square with the real loaded
control's background brush. Its verifier requires every RGBA byte to match,
including alpha. This is a paint regression separate from the unchanged
212 complete component frames; it does not remove their rounded edges or text
from raster acceptance.
All ten full-context paint frames match every RGBA byte. In the complete
light empty component frame, differing pixels fall from 3,336 to 35; rounded
edges still prevent exact raster parity. The corrected candidate passes 189
portable tests, four Windows shaping/layout tests, both solution builds and
fresh package consumer builds on x64 and ARM64. The x64 hidden runtime passes;
visible x64 input and package replay also pass as described above. Native ARM64
runtime remains unverified.

A separate diagnostic found that Chromium's 40 by 40 viewport rounds two
background/color pairs differently from its 640 by 400 viewport. The unchanged
source component agrees with the larger context. That small-viewport
diagnostic is retained as an open context-dependent raster limitation; it is
not used to replace the full component's expected PNGs.

Rounded-edge diagnostics compare native Border, Rectangle, arc Path and Win2D
rounded fills against the unchanged complete browser frames. None matches both
themes exactly. Segment probes also identify lost edge coverage at the parent
rounded clip; moving segments outside that clip changes narrow-segment behavior.
These diagnostic variants are not applied to the control. Rounded edges remain
an open raster mismatch, with the original expected pixels and zero tolerance.
