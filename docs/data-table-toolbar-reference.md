# Data Table Toolbar source contract and native acceptance

The frozen Bleecker producer is `src/components/data-table-toolbar.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`a2beb795287fd027857d9f4d12df9d0e76406b0fa0703a9fcf20aaa23064a0ef`.
Its public contract is `DataTableToolbarProps<TData>` / `DataTableToolbar`:
the original table, optional search-column ID, placeholder (default `Search...`),
consumer filter renderers/actions and a consumer class name. The native
`BleeckerDataTableToolbar` preview implements the consumer boundary and the
118 normal-state source layouts. Complete Windows acceptance remains open.

## State, ownership and observable behavior

The toolbar reads `table.getState().columnFilters.length` and JavaScript
truthiness of `globalFilter`. It shows Reset when either is active. The search
input exists only for a truthy search-column ID; it reads that column's filter
value, substitutes an empty string for null/undefined, and writes the actual
input string through the same column's `setFilterValue`. It does not search the
global filter or infer a default column. A missing column leaves a visible,
controlled empty input whose attempted edits do not update table state.

Reset invokes `table.resetColumnFilters()` without an argument. In the pinned
TanStack table this restores the table's initial column filters. It does not
clear the global filter. A global-only filter therefore leaves Reset visible
after repeated pointer/keyboard activation. An initial nonempty column filter
can also keep Reset visible. Do not replace this behavior with a generic
clear-all-filters command.

The reference uses real TanStack `useReactTable`, controlled column/global
state, core/filtered row models, and three fictional original records. Changes
reach a rendered consumer row-result probe. It checks original table, column,
row and callback ownership. Replacing the table switches to a second real
table; restoring the first retains its own column/global state. Live column,
placeholder and slot changes are also captured. No table API is mocked.

JavaScript truthiness is deliberate: null, undefined, false, numeric zero,
negative zero, NaN and the empty string hide a global-only Reset. Whitespace,
the string `0`, nonzero numbers, true, Infinity, empty arrays and empty objects
show it. Merely having a column-filter entry shows Reset even when its value
is empty. Non-string filter values reach the browser input as `0`, `false`,
`42`, `Harbor,Reed`, `[object Object]` and `NaN`; null/undefined display empty.
Number/undefined descriptors preserve these values in JSON without losing
NaN or negative zero through ordinary JSON number serialization.

Consumer filter/action nodes pass through directly. Fragments produce sibling
flex items; the toolbar does not wrap each slot or disable consumer content.
Numeric zero renders an anonymous text item, while true, false, null and empty
string produce no node. Disabled consumer buttons do not disable Reset.
Callbacks retain the original table and data array.

## Measured layout and paint

The outer/left groups are columns with 12 px gaps below viewport width 640.
At 640 and above they become rows; the outer group centers and distributes
left/right groups, with the left growing and the right holding Reset/actions.
The breakpoint follows the viewport, not the toolbar's own width. The right
group uses an 8 px gap and starts at the leading edge in a column layout.

The search Input is 36 px high, with 14 px type, a 20 px line height, 14 px
horizontal padding, one-pixel border and 7 px radius. Its outer wrapper fills
the left group's remaining width. The inner input alone is capped at 320 px
above the breakpoint; this can leave space before the next consumer filter.
It must not be modeled as a 320 px wrapper. Its minimum border-box width is
30 px from padding/borders; narrower hosts retain overflow rather than
inventing wrapping or clipping.

With the 800 px default fixture, the left group is 684 px and its input wrapper
496 px. The input itself is 320 px, the 176 px consumer filter starts at x=508,
and the 104 px action starts at x=696. With Reset, the right group is
196.96875 px, including the 84.96875 px Reset and the 8 px internal gap.
At a 639 px viewport / 600 px host, the fixture is 132 px high: input at y=0,
filter at y=48 and actions at y=96. At viewport 640 the same host is 36 px high
with a 296 px input. A 180 px host at a wide viewport still has 334 px of
content; it does not switch to the narrow layout. RTL mirrors these groups.

Reset is the source ghost/small Button: 36 px high, 13 px medium type,
14 px horizontal padding, 8 px icon/text gap and a 14 px Lucide X. The actual
compiled stylesheet yields normal letter spacing. Source hover uses sand/12
in Light and white/6 in Dark; a held press translates one pixel and scales to
0.99. The Input/Button transitions retain their source properties and premium
easing. This capture uses reduced motion (computed 0.01 ms durations); complete
enabled-motion/reversal coverage remains required.

## Evidence and reproducibility

`tests/Reference/DataTableToolbarLayout.json` retains 118 cases (59 in each
theme), 376 interaction states, 494 fresh geometry/paint/value/state gates and
14 initial-state screenshots. All gates have at least three stable animation
frame transitions within two seconds; the maximum is 81.4 ms. The 1,246
distinct per-fixture browser input receipts are trusted (9,932 appearances in
cumulative state snapshots). No page errors or console warnings/errors occur.

The runtime is React/React DOM 19.2.4, TanStack React Table/Table Core 8.21.3,
Lucide React 0.563.0, scheduler 0.27.0, clsx 2.1.1 and tailwind-merge 3.5.0.
The actual bundle's eight package versions/integrities are checked against the
frozen producer lock, alongside all four bundled producer-file hashes, fonts,
stylesheet, fixture signature and checked-in capture/client code. Build tools
are esbuild 0.25.12 and Playwright 1.62.1, outside the producer runtime. The
recorded Edge version is 155.0.4283.45.

The raw capture is `artifacts/data-table-toolbar-browser/reference-v2.json`,
SHA-256 `3bf21063db6d63d3c274a7447de83ac8934b672ca74494913261b1d979013a4b`.
The derived reference SHA-256 is
`521d44096ee981c3581cfc022b1821ba8432dd275b182d5f6bd4f90085a8a00d`.
Style interning retains every field and reconstructs the complete raw capture
before writing. Thirteen negative derivation guards reject partial/duplicate
cases, changed props/runtime/source, untrusted input, lost ownership, wrong
visible rows/input values/reset behavior, missing paint gates and changed
screenshots. The DOM-only v1 diagnostic captured an unsettled hover paint;
it is not the reference. v2 resets and verifies a fresh paint gate after every
action. Seven source boundary regressions pass with all 73 local tests.

Run from the repository root with the frozen source checkout at
`artifacts/line-chart-browser/frozen-source`, its compiled `styles.css`, and
`artifacts/chart-theme-reference/bleecker-package-lock.json`:

```powershell
node scripts/reference/prepare-data-table-toolbar-runtime.cjs
npm ci --prefix artifacts/data-table-toolbar-browser --ignore-scripts --no-audit --no-fund
node scripts/reference/capture-data-table-toolbar.cjs --esbuild-module '<absolute esbuild package directory>' --playwright-module '<absolute playwright package directory>'
node scripts/derive-data-table-toolbar-reference.mjs artifacts/data-table-toolbar-browser/reference-v2.json
node scripts/reference/verify-data-table-toolbar-derivation.mjs artifacts/data-table-toolbar-browser/reference-v2.json
dotnet test tests/Orchard.Tests/Orchard.Tests.csproj -c Release
```

`--probe` captures eleven Light fixtures and cannot be derived as the full
reference. Full captures resume only when all provenance matches. Use a fresh
capture file after changing capture code, dependencies, fonts or CSS. Keep the
original native full-run deadline, motion windows, input receipts, stable
Rendering gates, all 1,004 existing PNGs and all prior assertions intact.

## Native preview and ownership

Set `Table` to an application adapter implementing
`IBleeckerDataTableFilterSource`. It exposes `ColumnFilterCount`, `GlobalFilter`,
`GetColumn(id)`, `ResetColumnFilters()` and a `Changed` event. Each original
column implements `GetFilterValue()` / `SetFilterValue(object?)`. The application
owns filtering, rows, initial state and column identity; the toolbar never
copies or replaces those objects. Raise `Changed` after external state changes.
Table notifications are dispatched to the toolbar's UI thread; the adapter
still owns synchronization of its data. Update content collections on the UI
thread, as with other native UI-bound observable collections.
The catalog's `ToolbarFilterTable` shows a real three-row consumer view.

```xml
<orchard:BleeckerDataTableToolbar Table="{x:Bind TableAdapter}" SearchColumn="title">
    <orchard:BleeckerDataTableToolbar.FilterRenderers>
        <Button Content="Status" Click="FilterStatus" />
    </orchard:BleeckerDataTableToolbar.FilterRenderers>
    <orchard:BleeckerDataTableToolbar.Actions>
        <Button Content="New record" Command="{x:Bind CreateRecordCommand}" />
    </orchard:BleeckerDataTableToolbar.Actions>
</orchard:BleeckerDataTableToolbar>
```

Here `TableAdapter`, `FilterStatus` and `CreateRecordCommand` belong to the
application. The adapter wraps that application's actual table; the toolbar
does not supply a data store, table widget or record-creation workflow.

`SearchColumn` defaults to absent. `SearchPlaceholder` defaults to `Search...`.
The native search automation name follows that original placeholder, including
an explicitly empty value; no alternate label is invented for it.
The native Input renders a controlled string, including source number/boolean/
array/object coercion, without writing that string back merely on presentation.
Missing columns retain the source's visible empty/no-op behavior. Reset calls
the adapter's no-argument method and leaves global state under consumer control.
Conditional Reset removal is queued after the routed click so its real pointer
release reaches the original target.

`FilterRenderers` and `Actions` accept original UI elements, scalar content and
nested collections. Observable collections update while mounted; replacement
and unload detach prior subscriptions. `FilterTemplate` / `ActionTemplate`
render application models with their original data identity. Unsupported model
content requires a template; cycles, invalid spacing and a missing mounted
table fail explicitly. Use ordinary native `IsEnabled` and accessible names.

The 640px breakpoint reads the window viewport, independent of the toolbar's
width. The wide search wrapper grows while its Input caps at 320px; constrained
wide layouts retain the source's 30px input minimum and overflow. Narrow layouts
stack the left items and trailing row. `Spacing` controls the outer group gap
(default 12); source inner gaps remain 12 and 8. Standard padding, border, font
and template properties remain available. The medium Input's explicit 36px
height preserves its 14px type and 14px horizontal padding.

`-Component data-table-toolbar` exercises the registered fixture. At
`artifacts/smoke-data-table-toolbar-actions`, all 59 cases per theme
match source geometry within the unchanged 1/32px layout boundary, controlled
values and actual filtered row projections without presentation writeback.
Physical H/R/Enter input, exact Reset mouse down/up, initial/global state,
missing columns, UI Automation identity, live source/collection replacement,
unload/remount, template restore and original model/node identity pass. Input
height overrides, clearing the override and custom typography also pass.
Original status-filter and trailing-action clicks, eight exact forward Tab
press/release transfers, disabled control/peer inheritance and external updates
while disabled pass. All 75 local regression tests and token drift checks pass.
The final search-name follow-up passes all 118 source cases at
`artifacts/smoke-data-table-toolbar-accessible-names`, including the explicitly
empty placeholder. Both architecture solution builds pass with zero warnings
or errors after that change.

Exact raster acceptance fails for all 14 initial browser/native pairs:
1,216–2,456 pixels differ, with maximum channel delta 242. The comparison uses
the identical toolbar rectangle (native window origin 16,16 versus the producer
root screenshot), with no masks or tolerance. Complete window captures are
retained alongside the report at `artifacts/data-table-toolbar-raster-report.json`.

The full x64 run at `artifacts/smoke-data-table-toolbar-readiness-full` passes
in 350.1131651 seconds, retaining all 1,004 prior PNGs and adding 16 toolbar PNGs.
All 174 protected chart glyph hashes are unchanged. Native source, exact input,
tooltip and final-capture audits pass; see [runtime evidence](runtime-evidence.md).
Readiness gives queued UI work one turn bounded by the original 16ms poll.
Static toolbar source cases await both a commit and two real Rendering callbacks
with their complete props set before mounting. Live mutations and all pointer/
final-capture gates keep their existing ordering. All 14 native source toolbar
rectangles remain pixel-identical between the original and optimized full runs.

Final published evidence from source `2743907` is at
`artifacts/smoke-data-table-toolbar-published-full`: the complete x64 run passes
in 352.0595866 seconds with all 1,020 PNGs and protected input/glyph gates.
The published native-raster comparison has one pixel at channel delta one in
Light column-filter; that exact check stays failed without tolerance or masks.
All 14 browser/native comparisons also stay failed. See the complete hashes
and reports in [runtime evidence](runtime-evidence.md).

The versioned package is
`artifacts/packages-data-table-toolbar-native/Gaulatti.Orchard.0.1.0.nupkg` and
the matching runnable catalog is `artifacts/catalog-data-table-toolbar-native`.
Both independent consumers restore that exact package into fresh GUID caches,
then remove those caches. x64 runtime passes both themes; ARM64 compilation
passes with runtime explicitly unverified. All four Orchard DLLs are identical.

### Flat consumer chrome and caption baseline follow-up

The frozen consumer buttons specify a flat theme-sand border, transparent
background, regular 14px type and a 20px line box. The native source fixture
previously let Windows' default button chrome override those supplied values.
Its explicit consumer-owned template now preserves those CSS properties in
both themes; the toolbar still passes original UI elements through unchanged.
Every normal source case checks the actual native surface brush types/colors,
font size/weight, border thickness and radius against the frozen style records.

The fixture labels and library-owned Reset caption now use the existing CSS
line-box rule from Section Header/Stat Card: rounded natural ascent/descent
with floored half-leading. This preserves allocation, wrapping and the original
font; it does not add a fixed pixel offset. In the captured Light column-filter
case, Reset's occupied glyph rows move from 13–23 to the source's 11–21 while
the already-correct X icon stays unchanged. Original filter/action callbacks,
controlled values, row ownership, reset semantics, lifecycle and exact native
input/Tab checks still pass across both themes.

`tests/Reference/DataTableToolbarCaption.json` records the original browser
baseline at 22.25px in both themes. A zero-width inline baseline marker leaves
both initial PNGs byte-identical to the frozen producer screenshots, before
and after insertion. The reproducible pinned-browser probe is
`scripts/reference/capture-data-table-toolbar-caption.cjs --playwright-module
<absolute Playwright 1.62.1 package directory>`. Native source checks compare
the actual caption baseline with that independent measurement using the
existing 1/32px geometry boundary. They also inspect the visible slot label's
actual font and foreground, beyond the parent Button's properties. All 76
local tests pass, including the source-baseline provenance regression.

Focused evidence: `artifacts/smoke-data-table-toolbar-caption-baseline`.
The Light default difference falls from 1,900 to 1,163 pixels; column-filter
falls from 2,312 to 1,499. All 14 exact browser/native comparisons remain failed
(1,056–1,795 pixels, maximum channel delta 227), with no masks or tolerance.
Two earlier flat-template runs retain identical pixels across all 14 rectangles
at `artifacts/data-table-toolbar-flat-slots-repeat-report.json`.

The expanded x64 run at
`artifacts/smoke-data-table-toolbar-caption-diagnostic-full` passes in
348.6147173 seconds with all 1,020 PNGs, all 174 protected glyph hashes,
1,728 pointer gates (minimum two stable transitions), 1,738 same-target keys,
60 clicks, eight toolbar Tab transfers, 28 tooltip and 16 final-capture gates.
An earlier run failed the unchanged Dark Long Checkbox palette-settle check;
the report showed hover-level border opacity while the fixture expected rest.
Additional failure-only diagnostics now retain native/tracked hover, VSM and
cursor state without changing the predicate or its two-second limit. This
passing rerun does not establish the earlier failure's cause or resolve it.

Exact focused/full native comparison still fails for five pixels at channel
delta one in Dark small-viewport; the other 13 toolbar rectangles are identical.
No tolerance or mask is applied. Native ARM64 runtime remains unverified.

The matching published run from source `3df8889` passes at
`artifacts/smoke-data-table-toolbar-caption-published-full` in 349.6342151
seconds. All 1,020 PNGs, 174 protected glyph hashes and the input/source/capture
audits pass unchanged; maximum pointer readiness is 165.9788ms. The earlier
Checkbox failure does not recur in either final complete run, but its cause
remains unestablished and the extra failure diagnostics are retained.

The package is
`artifacts/packages-data-table-toolbar-caption/Gaulatti.Orchard.0.1.0.nupkg`,
SHA-256 `7ff7cd8d41e953376d397365d2ece02181b9c3534df4455f927d01418558dbc0`;
the matching catalog is `artifacts/catalog-data-table-toolbar-caption`.
All four Orchard DLLs (package, catalog, x64 consumer and ARM64 consumer) match
SHA-256 `9b26e31f81f2c466bc27fe3420daa2cd1e1a73406b35b7154393d4a3f0a54de8`,
version `0.1.0+3df888927db4bb7680314b484ec242f20a5d8f60`. Both solution builds
pass with zero warnings/errors. The fresh x64 consumer verifies the measured
22.25px caption baseline in both themes after the actual Reset text template
loads; the fresh ARM64 consumer compiles with runtime explicitly unverified.
All GUID caches are removed, including the first x64 attempt that queried the
new text subtree before its template loaded. The final exact browser/native
and focused/full raster checks remain failed without masks or tolerance.

### Reset press geometry follow-up

Reset previously entered WinUI's pressed state without changing its rendered
bounds. The focused regression at
`artifacts/smoke-data-table-toolbar-reset-states-initial` fails that boundary.
Reset now follows the original Button's centered 0.99 scale and 1px downward
translation through its actual `IsPressed` state. Native Click still owns the
consumer callback. The transform returns to rest on release, disabling or
unload; template replacement detaches the old presentation callbacks.

`tests/Reference/DataTableToolbarReset.json` captures the frozen producer with
trusted mouse down/up/click and Space down/up receipts, both themes and both
motion preferences. The individual CSS scale/translate properties already
have their final values during pointerdown and every held sample. They are
absent from the source transition-property list, even when its color transition
duration is 190ms. The native press geometry therefore changes immediately.
Reproduce with `scripts/reference/capture-data-table-toolbar-reset.cjs
--playwright-module <absolute Playwright 1.62.1 package directory>`.

Focused native evidence at
`artifacts/smoke-data-table-toolbar-reset-verified` passes actual hover, held
and release geometry, rendered Ghost background/foreground in both themes,
pointer focus, no callback on down, exactly one callback on release, cleared
column filters and retained global filtering. Hover follows a real search
click and a real move to Reset. Each release retains an exact, non-generated,
owned native mouse receipt. Six additional whole-window PNGs preserve those
states. The 118 initial source cases and existing lifecycle/action checks still
pass. All 77 regression tests, token checks and both solution architecture
builds pass; the builds have zero warnings/errors.

The independent package consumer includes a held native Space check for the
same geometry, exact original key targets, callback timing, focus and release.
Final fresh-consumer and published full-catalog results are recorded separately.
Native evidence here uses Windows animations off; enabled native color motion,
all source interaction states and exact raster acceptance remain open.

The matching published catalog from source `d2f1b8c` passes the complete x64
suite at `artifacts/smoke-data-table-toolbar-reset-published-repeat-full` in
350.7423591 seconds under the unchanged 360-second deadline. All 1,020 prior
PNG names remain, with six additional Reset captures (1,026 total). All 174
protected glyph hashes, 1,732 owned pointer gates (minimum two stable
transitions, maximum 208.4624ms), 1,738 same-target key actions, 62 click
releases, two exact Reset releases, eight toolbar Tab transfers, 28 tooltip
gates and 16 final capture gates pass. Reports are
`artifacts/data-table-toolbar-reset-full-audit.json` and
`artifacts/data-table-toolbar-reset-full-input.json`.

The first full run at
`artifacts/smoke-data-table-toolbar-reset-published-full` failed after
144.8268361 seconds with Windows `0x80004004` aborting the Light BottomTip
RenderTargetBitmap capture, after its original presentation gate passed.
The focused tooltip suite passes at
`artifacts/smoke-data-table-toolbar-reset-tooltip-diagnostic`; the complete
rerun also passes with the same published binary. No capture retry, predicate,
deadline, mask or tolerance was added. The original abort remains preserved
and its cause remains unestablished.

Package `artifacts/packages-data-table-toolbar-reset/Gaulatti.Orchard.0.1.0.nupkg`
has SHA-256 `ad769e48a8c0b59fd900394ad363528fea0c39039507bb4991ec790aab1d5562`.
The runnable catalog is `artifacts/catalog-data-table-toolbar-reset`.
Fresh x64 runtime verification passes both themes at
`artifacts/package-consumer-data-table-toolbar-reset`, including the actual
held Space check. ARM64 build-only verification passes at
`artifacts/package-consumer-data-table-toolbar-reset-arm64`; native ARM64
runtime remains unverified. Both generated restore caches are removed. All
four Orchard DLLs match SHA-256
`77579027cf3e4eeab3d9b0e47bf7bb191fe28f971b0bde002a0de7f99fba9888`,
ProductVersion `0.1.0+d2f1b8c0288d9db64ed15720351b6e9afcc9d7c7`.

Exact focused/full native comparison remains failed: 13 of 14 rectangles
match, while Dark multilingual has five pixels at channel delta one. All 14
browser/native comparisons still fail (1,056–1,795 pixels, maximum delta 227).
Reports retain zero masks and zero tolerance at
`artifacts/data-table-toolbar-reset-native-raster.json` and
`artifacts/data-table-toolbar-reset-browser-raster.json`. Inventory status
remains 50 in progress, 62 pending, none accepted out of the fixed 112.

### Complete consumer interaction replay

The catalog adds a dedicated `data-table-toolbar-interactions` smoke check for
all 376 recorded states across 52 original scenarios (188 states / 26 scenarios
per theme). It preserves the existing full-suite checks and 360-second limit.
Run the dedicated matrix, then independently audit its output:

```powershell
./scripts/smoke-catalog.ps1 -Executable <catalog-exe> -ClientSize 1024,745 -ExpectedArchitecture X64 -Component data-table-toolbar-interactions -Output <output-directory>
node scripts/verify-data-table-toolbar-interactions.mjs <output-directory>
```

The replay uses real native letter keys, Shift for capitals, Ctrl+A/Backspace
for replacement, actual mouse hover/down/up/click and routed Tab transfers.
Each key and modifier retains its original focused target and one down/up
receipt. Physical typing produces a native change per character; the producer's
Playwright `fill` produces a single change. Their resulting controlled state,
original column, filters and rendered consumer rows are compared at each
recorded boundary. Initial-filter restoration, global-only Reset, disabled
consumer slots, missing columns, live table/column replacement and replacement
slots follow the original frozen sequence.

Each boundary checks original table, column and row identities, raw filter
values, rendered row text, search/Reset/slot bounds and visibility, Reset
callback timing and the original slot callback role/index/table/data array/UI
element. Callback records are snapshots for that exact step; each capture also
retains the measured toolbar rectangle in the window for later raster comparison.
The standalone
audit verifies reference SHA-256, the complete ordered case/state/capture set,
all routed key/modifier/pointer receipts, 134 Tab transfers, held releases and
two final catalog captures. It explicitly reports component acceptance as false.

The fixture has visible Before/After controls and a visible Outside toolbar
host control, with native cyclic Tab navigation. That last control represents
the producer leaving its document for browser chrome; its actual native type,
name and host-boundary mapping remain in every focus record. Focus after
conditional Reset removal is separately recorded: the producer reports document
focus, while WinUI moves to the next action or After control. Those differences
remain open; the library does not force focus to a fixture-specific target.
The new consumer/geometry/input gate does not establish rendered paint, motion,
accessibility or complete focus parity.

The first correctly sized diagnostic at
`artifacts/smoke-data-table-toolbar-interactions-client-sized` passes the CLI
and its initial audit, before measured toolbar rectangles became required in
the current audit. All 376 state PNGs are exactly 1000×745; both final
catalog PNGs are 1024×745. There are 86 owned pointer gates/moves (minimum two
stable transitions, maximum 51.5934ms), 316 same-target key actions including
84 Shift/Ctrl combinations, 68 clicks, 18 exact held releases and 134 exact
Tab transfers. All 52 scenarios and 376 consumer/geometry/ownership checks
pass. The embedded source reference SHA-256 matches the frozen derived file.
The 26 conditional-removal focus differences (13 per theme) stay in the audit.

Two earlier diagnostics remain preserved. The first completed the replay but
the CLI rejected missing final catalog renders. The next completed replay
failed the independent image-size check: the new fixture had saved startup
window dimensions before input acquisition applied the requested client size.
It now saves dimensions after acquisition and restores those exact dimensions.
No size assertion or existing full-suite check was relaxed.

The current reproducible published run from source `29f42bd` is
`artifacts/smoke-data-table-toolbar-interactions-published`. Its CLI and current
standalone audit pass in 56.5281415 seconds, retaining the measured rectangles
for all 376 state captures and both final catalog renders. The counts remain
86 owned pointer gates/moves, 316 key actions (84 modified), 68 clicks,
18 held releases and 134 Tab transfers; maximum pointer readiness is 50.9236ms.
All 26 focus differences remain reported, with component acceptance false.

The complete published catalog also passes at
`artifacts/smoke-data-table-toolbar-interactions-published-full` in
350.9128699 seconds under the unchanged 360-second deadline. All 1,026 previous
PNG names remain, with no additions or omissions. All 174 protected glyph
hashes, 1,732 owned pointer gates/moves (minimum two stable transitions,
maximum 200.7276ms), 1,738 same-target keys, 62 clicks, two exact Reset releases,
eight toolbar Tab transfers, 28 tooltip gates and 16 final-capture gates pass.
See `artifacts/data-table-toolbar-interactions-retention-report.json`,
`artifacts/data-table-toolbar-interactions-full-audit.json` and
`artifacts/data-table-toolbar-interactions-full-input.json`.

All 14 initial native toolbar rectangles are pixel-identical to the previous
published full run, with zero masks/tolerance, at
`artifacts/data-table-toolbar-interactions-native-raster.json`. The 14 exact
browser/native comparisons still fail (1,056–1,795 pixels, maximum delta 227)
at `artifacts/data-table-toolbar-interactions-browser-raster.json`. The earlier
tooltip capture abort does not recur; its cause remains unestablished.

The matching package is
`artifacts/packages-data-table-toolbar-interactions/Gaulatti.Orchard.0.1.0.nupkg`,
SHA-256 `87c88a4942fe36b21820e82b128544fd48710f842160e368fb835e957f6f4723`;
the runnable catalog is `artifacts/catalog-data-table-toolbar-interactions`.
All four Orchard DLLs match SHA-256
`f8d45fb99c3fb9f4301633162768a65e88937e392d8880540e172ca814e6c4ce`,
ProductVersion `0.1.0+29f42bdcb7826f449cb24a8e56102cfb1a0c8c39`.
Fresh x64 consumer runtime and ARM64 build-only checks pass at
`artifacts/package-consumer-data-table-toolbar-interactions` and its
`-arm64` sibling; both temporary restore caches are removed. ARM64 runtime
remains unverified. All 77 local regressions and token checks pass; both
solution architectures build with zero warnings/errors. This checkpoint
changes catalog verification and documentation; there is no backend or
Prometheus impact. Inventory remains 50 in progress, 62 pending, none accepted.

### Complete browser state images and consumer focus paint

`tests/Reference/DataTableToolbarStatePixels.json` adds 390 image records:
all 376 interaction states and all 14 original initial captures, each with a
second live frame (780 PNGs). The source capture covers 62 scenarios and
retains all 1,246 distinct trusted receipts. Every captured DOM/layout/style,
table, callback, selection and accessibility snapshot matches the expanded
frozen v2 record; only receipt timestamps are excluded from that equality.
Before/after screenshot snapshot hashes also match. All 14 initial PNGs and
their repeats remain byte-identical to the originals.

Interaction screenshots use `caret: initial` and `animations: allow`, with
no masks or tolerance. Each state keeps three stable paint transitions within
the original two-second deadline. A repeated Light both-filters/Reset-hover
capture changes 17 pixels at the live text caret; both frames and the failed
repeat-stability result are preserved. The original initial screenshot step
is replayed only while unfocused: Playwright's original caret cleanup creates
an empty style attribute that the frozen later DOM snapshots contain. No DOM
attribute is silently dropped to make those snapshots agree.

Reproduce the source and compare against a verified native matrix:

```powershell
node scripts/reference/capture-data-table-toolbar-state-pixels.cjs --playwright-module <Playwright-1.62.1-directory> --output <source-output>
node scripts/compare-data-table-toolbar-state-pixels.mjs --source <source-output> --native <native-matrix-output> --output <comparison.json>
```

The source images are at `artifacts/data-table-toolbar-browser-states-live-caret`.
The comparator verifies source hashes/state checks, native input-audit status,
environment and the measured window rectangle, then compares every RGBA pixel
of each complete toolbar against both browser frames. Its built-in PNG reader
checks CRCs and decodes only the actual 8-bit RGB/RGBA, noninterlaced formats;
unsupported input fails explicitly. Pillow independently agrees with all
1,128 first/repeat/source-repeat comparisons in
`artifacts/data-table-toolbar-state-raster-pillow-audit.json`.

The original native matrix comparison at
`artifacts/data-table-toolbar-state-raster-report.json` fails all 376 pairs
against both source frames: 358–4,959 pixels differ in the first-frame pairs,
with maximum channel delta 255 and zero masks/tolerance. This broadens the
rendered acceptance evidence beyond the 14 initial images.

The measured browser consumer-button focus ring paints two dark inset pixels
despite its computed `auto 1px` outline. The native fixture previously used
Windows' offset black/white focus visual. `SourceSlotButtonStyle` now uses a
two-pixel primary visual, zero margin/secondary thickness and an opaque
`#101010` brush. A High Contrast resource selects the Windows highlight color;
actual Windows contrast verification remains open. The fixture checks these
normal-theme properties alongside its original consumer chrome. The toolbar
continues to pass the original consumer elements and callbacks through.

The probe at `artifacts/smoke-data-table-toolbar-slot-focus-probe` passes all
376 consumer/geometry/input checks and 134 Tab transfers. The complete raster
comparison at `artifacts/data-table-toolbar-slot-focus-raster-report.json`
improves 42 states, worsens none by differing-pixel count and leaves 334
unchanged. Total first-frame differing pixels fall from 760,441 to 733,921.
All 376 exact checks still fail; focus-removal differences and the live-caret
repeat difference remain recorded. No component is accepted by this change.

The first published raster check at
`artifacts/data-table-toolbar-state-pixels-published-raster-report.json` has
37 improved states, six worse by two differing pixels each and 333 unchanged
against the original matrix; the total is 731,635 differing first-frame pixels.
The six changes are in Dark column-filter input-focus/hover captures. Between
the focus probe and published run, 43 native pixels at the input's rounded right
edge differ by at most one channel level; the cause remains unestablished.

The Dark live-source hover probe also captured a missing Reset hover background.
The matrix now requires actual native `IsPointerOver`, no held press and the
rendered Ghost brush before capture, then checks that state again after the
existing presentation gate. The original two-second deadline and all physical
pointer rules remain. A stricter run at
`artifacts/smoke-data-table-toolbar-hover-ready` fails Dark live-source hover;
its desktop receipt shows the exact owned stationary point, backed by a prior
actual receipt, with no new move emitted. The cause remains unestablished.
Failure-only diagnostics retain hover/press/enabled/load/theme flags, actual
brush, visual state, pointer ownership/bounds and a whole-window PNG.
Two instrumented runs at `artifacts/smoke-data-table-toolbar-hover-diagnostic`
and its `-repeat` sibling pass all 376 states. These passes do not resolve the
earlier missing-hover failure. This remains an open interaction acceptance gap.

## Native acceptance still required

- Complete rendered paint/focus/motion acceptance for the 376 source interaction
  states, full content/override/error/RTL matrices and reverse/edge focus traversal.
  The dedicated replay checks consumer state, geometry, ownership and native input;
  source/native focus differences after conditional Reset removal remain open.
- Verify native ARM64 runtime; both solution architectures and fresh consumer
  builds pass, and the independent x64 consumer passes physical typing,
  original row/column identity and initial/global reset semantics in both themes.
- Verify exact rendered parity, full content/override/error matrices, Windows
  contrast, Narrator, display/text scaling and measured collection performance.
  Include monitoring/editorial compositions and final matching-source artifacts.

The public wiki is unavailable, so documentation remains in the repository.
This native presentation work has no backend or Prometheus surface. Push
Actions remain paused by user request; no CI dispatch is authorized here.
