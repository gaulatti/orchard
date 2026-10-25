# Table implementation work

The pinned `src/components/table.tsx` exports the table primitives,
`SortableTableHeader`, and its own `DataTable` composition. That composition is
distinct from the TanStack adapter in `src/components/data-table.tsx`; both are
required inventory entries. This work does not implement application data
services or a product table workflow.

The initial Table primitive checkpoint implemented the primitives and their first
catalog fixture locally. At that point, all 368 portable tests and ten windowless font tests passed. Fourteen hidden native cases verify
original content, bindings, native invocation, spans, scrolling, RTL and
template restoration. Twelve layouts match the source within a one-DIP font
tolerance (current maximum difference 0.27988 DIPs); 36 source/native header, selected-row
and footer paint samples match within one channel value. These checks are not
physical input or full raster acceptance. A fresh isolated x64 NuGet consumer
repeats all fourteen cases, twelve layouts and 36 paint samples. x64 and ARM64
catalog and consumer builds pass; ARM64 runtime remains unverified. The local
primitive preview contains 146 loose dictionaries, six fonts, the matching PRI,
current DLL and package README. Earlier Context Menu packages do not contain
these controls.

The current `0.1.0-data-table-live-v1-local` preview adds identity-based original
item observation and structural basic-table layout repairs. An additional
84-state source/fresh-NuGet suite verifies both compositions' live collection
updates, reload/template behavior and caller style bindings; all 84 capture
pairs are byte identical. See [Data Table validation](data-table.md) for the
current 406-test suite, evidence and remaining acceptance limits.

`tests/Reference/Table.json` freezes 38 source states and 24 interaction
histories containing 84 snapshots. An independent run reproduces its JSON and
38 PNGs byte for byte. The audit decodes 25,992,000 pixels, checks a header-paint
sample in every frame, validates all 122 CSS-scanned source inputs against the
pinned commit, and records 1,694 actual bundled inputs. States include ordinary
and selected rows, disabled original content, row/column spans, RTL, constrained
scrolling, captions, footers, default/custom empty text, caller-owned error text
and mixed cell values.

The source contract established by those observations is:

- The Table wrapper scrolls horizontally once its cells reach their intrinsic
  minimum widths. Headers, body and footer share columns; captions appear below
  the table with a 16px gap. Ordinary headers are 44px high; body cells use
  16px horizontal and 12px vertical padding, 14px text and a 20px line box.
- Header text is uppercase 11px SemiBold with 0.08em tracking. Sort buttons use
  the original label, a 12px directional icon and a 6px gap. An unsorted field
  requests ascending order; an ascending current field requests descending.
- Controlled sort/selection requests also update internal state. Rejection
  leaves the caller's visible state intact; releasing control exposes the last
  internal request. Cell and footer producers receive the actual sorted rows.
- Numeric pairs compare numerically; other values use string comparison.
  Null cells display empty text, booleans retain their names, arrays join with
  commas and ordinary objects display `[object Object]`. Comparisons are stable.
- Row keys are produced after sorting, with the sorted index. Index-based keys
  therefore move visible selection to the row at that position. Selecting one
  row retains unrelated selected keys; selecting all replaces the key set with
  the visible sorted keys. Empty data never reports all selected.
- The default source Checkbox has no explicit `role` attribute. The table's
  `[role=checkbox]` padding/translation selector does not match it: default
  checkbox cells retain 16px padding and a 52px intrinsic minimum width.
- Header fill is translucent. In Dark, its observed composite is RGB 22/32/41
  over the primitive's parent surface and RGB 32/45/58 over the composed card.
  A single opaque fill would break one of those contexts.

`BleeckerTableModel.cs` introduces column/sort descriptions and the internal
state/projection model. Portable tests replay all ten sort/selection histories
in both themes, including exact callback key order, original row identity,
controlled rejection/release, collection replacement, custom comparison and
post-sort index keying. These tests establish model behavior, not rendered UI.

`BleeckerTable.Sections` retains original `BleeckerTableHeader`,
`BleeckerTableBody`, `BleeckerTableFooter` and `BleeckerTableCaption` instances.
Sections contain original `BleeckerTableRow` controls, whose `Cells` collection
contains `BleeckerTableCell` and `BleeckerTableHead` controls. Cells retain native
`Content`, templates and selectors. `ColumnSpan` and `RowSpan` describe their
logical positions; zero row span extends to the end of the current section.
`ColumnWidth` supplies a preferred width while intrinsic minimums still apply.
Cell styles and local/bound values also control FontStyle, FontStretch,
CharacterSpacing, BorderBrush, BorderThickness and CornerRadius. The default
header style supplies 80 units of tracking; callers can override it. Plain text,
the rich-content presenter and sortable labels forward typography. Intrinsic
word measurement uses the effective font style/stretch/tracking and includes
cell padding, borders and margins. A consumer-owned rich TextBlock retains its
own typography and content.

The default cell presentation contains PART_Text and PART_Rich; a template
that uses either part must provide both. A complete replacement containing
neither delegates presentation to the supplied native ContentControl template.
This includes the stock presentation WinUI can apply when Template is cleared
under a derived style. Orchard does not select an alternate template. Restoring
the default template restores its text/rich presentation using the original
content and caller bindings.

The renderer orders caption content below its shared column grid and exposes
native Table/Grid cell and header metadata. `ScrollTo` controls the actual
horizontal ScrollViewer. The catalog's original button toggles a selected-row
example and updates fictional local status. The actual catalog probe invokes
that original button twice in each theme, verifies both selected states and
visible status, and retains the original cells and action. Template removal
retires the Table's active grid metadata; cell template clearing follows the
installed native ContentControl behavior, checked against a stock control.
Restoration retains the same original content and row binding.

The palette is generated from `tokens/bleecker.source.ts` by
`generate-table-palette.mjs`; run its `--check` mode with Node 24. The source
pixel audit caught and rejects an earlier selected-row Sea/Deep Sea mix-up and
a dark footer that incorrectly used light Sand. The original failed overflow
report is also retained and rejected by the runtime auditor.

The current preview adds `BleeckerSortableTableHeader` and
`BleeckerSimpleDataTable`, verified in both the source host and an isolated
NuGet consumer using `0.1.0-table-composition-v1-local`. The separate TanStack
adapter remains pending. The new catalog entry is **Sortable tables**, backed
by `SimpleDataTablesFixture.xaml`; the original **Tables** fixture remains.

The sorting header takes `Field`, `Label`, `CurrentSort` and `IsSortable`, and
raises `SortRequested` without changing `CurrentSort`. Its native button keeps
the original accessible label. `SortButtonStyle` permits a caller template;
its required parts are `PART_Label`, `PART_Up`, `PART_Down`, `PART_Unsorted` and
the `PART_Normal`, `PART_Hover`, `PART_Active` palette text probes.

The composition takes an `ItemsSource`, its observable `Columns` collection,
and a required `GetRowKey` callback for nonempty data. Column descriptions use
the immutable `BleeckerTableColumn` constructor: `key`, `header`, and optional
`isSortable`, `alignment`, `valueSelector`, `cellContentSelector`, `comparison`.
Replace a column description to change it. This also avoids WinUI's generated
metadata attempting to assign C# `init` properties. `CellStyleSelector` and
`HeaderStyleSelector` provide per-column native styles. Column alignment is a
style-level default. A selector style's direct or inherited alignment setter
takes precedence, followed by any local value or live binding on the generated
cell. Reconciliation never writes a local alignment value. When a selector does
not specify alignment, an internal style based on that exact caller style
supplies the column default; the caller's style and setters are not modified.
Removing selectors and releasing bindings reveals the current column default.
Equivalent generated alignment styles may be shared by newly presented cells
within one synchronous reconciliation. Retained cells keep their existing
styles; selectors still execute for each cell. The temporary lookup ends with
that pass and does not cross control instances. Caller-style reference identity,
target type and alignment distinguish entries. Generated styles are already
sealed before owner load, so this sharing adds no mutable style window. The
native live and template fixtures cover selector calls, alignment precedence,
style replacement and retained caller bindings for both table compositions.
The catalog intentionally centers Count through selectors despite its declared
right alignment, and renders names with a caller DataTemplate. The cell producer
receives an original item and sorted index; returning the same native control
retains that control while rows move. Stable unique row and column keys are
required. `FooterRowsSelector` receives the sorted original objects and returns
original `BleeckerTableRow` instances.

`Sort` and `SelectedKeys` are nullable controlled properties. Their request
events are `SortRequested` and `SelectionRequested`; null releases control to
the last internal request. `EffectiveSort` and `EffectiveSelectedKeys` report
the visible state. Collection and item notifications refresh mounted data;
`Refresh()` handles non-observable data and a mutated caller-owned key set.
Each distinct original object receives one listener, including separate objects
that compare equal. Repeated references share one listener. Source replacement
and unload release the retired collection and item listeners.
Structural body/footer edits invalidate the shared table layout, including
collection additions whose new original cells have not yet been measured.
Notifications must arrive on the control's UI thread. The composition template
requires `PART_TableHost` and the `PART_Secondary` palette text probe. Template
restoration reuses the original internal Table and native cell contents.

`verify-simple-data-table.ps1` and its independent auditor replay the real
source histories through native button/checkbox providers and compare all 26
composed source layouts, including header buttons and their fonts, directions,
opacity and foreground colors. The auditor also compares the 20 interaction
histories' retained-instance layouts and requests. Runtime, source geometry and
fresh package validation of this preview pass. Physical input/focus,
complete motion/scaling/shadow raster, collection performance and ARM64 runtime
remain acceptance work.

RTL measurements use physical bounds in the root coordinate space. WinUI
already mirrors inherited RTL coordinates; manually reversing columns again
had put the first logical column on the wrong physical side. Headers retain
the source's physical left/right alignment, explicit right-aligned body cells
remain on the right, and ordinary body cells use the inherited start alignment.
CSS includes a final tracking advance that native `CharacterSpacing` omits;
the native heading renderer includes that advance in visual and intrinsic
measurement. Sort buttons also participate in the header's 14px/20px baseline
strut, including the half-DIP collapsed header rule. The renderer computes the
line box from measured font ascent/descent and the wrapped label height. The
independent source baseline probe verifies that adding its diagnostic zero-size
marker does not change the original button bounds. Wrapped original TextBlocks retain their word minimums instead
of being measured as a zero-width rich presenter. Row `MinHeight` also applies
to shared layout, including the 96-DIP empty row.

Sparse sections keep their original cells and visual layout. Metadata-only
empty cells supply the remaining positions in the rectangular grid, retain
their containing table and row peer, and do not enter the caller's `Cells`
collection. This follows the [UI Automation grid contract](https://learn.microsoft.com/en-us/windows/win32/api/uiautomationcore/nf-uiautomationcore-igridprovider-getitem).
These are component metadata checks, independent of the excluded OS testing
campaigns.

Shared columns also invalidate retained row panels' arrangement after measuring.
A footer can keep the same outer row size while its spanned cell width changes;
the source histories caught an earlier stale footer after sorting. All 26 static
layouts and 72 action snapshots now match the source over 8,792 geometry values
within one DIP (observed maximum 0.05451 DIPs). Seventy-six background paint
samples match within one channel value. Real earlier RTL, constrained-text and
footer-reflow counterexamples remain frozen in
`tests/Reference/SimpleDataTableCounterexamples.json` and are rejected.

Two additional native contracts verify original actions/cells, live item
notifications through sorting and footer production, rejected requests with
unchanged caller binding expressions, live caller updates, release to internal
requests, template restoration and removal of item observers on unload.
The actual catalog drives ten actions across Light and Dark, including a
standalone header, controlled rejection/release, row selection and original
cell buttons. The fresh NuGet consumer repeats all 26 static states, 20 retained
histories/72 snapshots, 8,792 geometry values, 76 paint samples and both original
content/binding contracts under the identical DLL. Both architecture consumers
build from isolated feeds/caches, which are removed after verification. The
archive's DLL, PRI, all 146 loose dictionaries, six fonts and README match the
tested/current files. ARM64 runtime remains unverified.

Before final acceptance, expand customization,
scaling, complete border/text/corner/shadow
raster, state motion, physical input/focus and collection performance. The
primitive auditor's v1 raw report contains an old constant composition field;
its canonical audit now names that scope `dataTableCompositionVerifiedByThisProbe`.
The separate composition audit supplies the current implementation evidence.

The `0.1.0-table-text-v1-local` preview corrects Unicode/language casing and
string-key membership, verified in both the source host and a fresh isolated
NuGet consumer. `TableText.json` freezes 18 language/header
states and six histories containing 18 snapshots; an independent capture
reproduces its JSON and all 18 PNGs byte for byte.

Plain, sortable and nonsortable headings use full uppercase mappings. The
original `Content`/`Label` and accessible names remain unchanged. Unicode 17
simple mappings are generated from the official `UnicodeData.txt` into the
small embedded `Uppercase-17.0.0.txt` resource; unconditional multi-scalar
mappings continue to use `SpecialCasing-17.0.0.txt`. This avoids relying on an
older OS/runtime Unicode version for newly cased characters. Contextual locale
mapping uses Windows' supported ICU common C API, available below Orchard's
Windows 10 version 1809 minimum, on WinUI's initialized UI thread. Failures are
explicit. No OS language or accessibility setting is changed. See
[Windows ICU](https://learn.microsoft.com/en-us/windows/win32/intl/international-components-for-unicode--icu-)
and the [CSS text-transform contract](https://www.w3.org/TR/css-text-3/#text-transform-property).

The native fixture checks 72 heading presentations across the static and live
language cases, including German/ligature expansion, Turkish/Azerbaijani dotted
i, Lithuanian dot removal, Greek context and supplementary characters. Display
comparison permits canonical Unicode equivalence, since the installed ICU can
represent preserved Greek accents differently from the source browser. Original
labels and accessible names are still compared exactly. This is a rendered-text
contract check, not full glyph-raster acceptance.

Row-key membership and default dictionary property lookup are ordinal. A CLR
set or dictionary configured to ignore case cannot make `cedar` and `Cedar`, or
`name` and `NAME`, interchangeable. Composed/decomposed key strings also remain
distinct. The control consumes the caller's actual values without changing the
caller-owned collection or binding. Controlled rejection/release, unknown keys,
select-all replacement and explicit `ValueSelector` customization retain their
existing semantics. The actual old runtime's five incorrect language displays
and two key/property histories are frozen as regression counterexamples.
The fresh package repeats all 72 heading presentations and six histories/18
snapshots, the existing 26-state/72-snapshot composition suite and fourteen
primitive cases. x64/ARM64 catalogs and isolated consumers build; ARM64 runtime
remains unverified. Both fictional component demos pass startup under this DLL.

Run the frozen source gates with:

```powershell
node scripts/reference/verify-table-source.mjs --reference tests/Reference/Table.json --frames tests/Reference/Table --output artifacts/table-source-audit.json
node scripts/reference/test-table-source-audit.mjs --reference tests/Reference/Table.json --frames tests/Reference/Table --output artifacts/table-source-negative.json
```

Run `verify-table-primitives.ps1` against the catalog build or a fresh consumer
(`-HostName PackageConsumer`), and `verify-table-catalog.ps1` against the catalog.
The wrappers reject an active build or an unrecognized hidden command before
launching. The native auditor compares 504 geometry values and the original
source-context paint samples, validates the actual binaries and PNG bytes, and
requires a successful process exit. Both required fictional component demos
remain intact and pass startup under the current library.

The native customization regression covers plain, heading, sortable,
nonsortable, caption, original rich TextBlock and consumer DataTemplate content.
Five retained-instance stages in each theme exercise derived styles, seven live
bindings, property notifications, template clearing/restoration and release back
to style values. Text measurement uses the same unrounded native font metrics as
the renderer. The earlier customization DataTemplate example supplies an explicit MinWidth.
The additional template suite verifies unbreakable content without that override. Full CSS customization and raster acceptance remain
open. Run verify-table-customization.ps1 and test-table-customization-audit.mjs
against the source host or a fresh package consumer.

The template minimum calculator uses the actual realized native template tree.
Text, styled inline runs, borders, content presenters, auto/star grid columns and
vertical stacks contribute their intrinsic widths. Horizontal native stacks
measure children without a horizontal constraint, so their minimum includes
those actual unbounded desired widths. Explicit Width/MinWidth/MaxWidth values
and fixed grid columns retain the consumer's native clipping constraints;
nested ScrollViewers keep their own viewport contract. Opaque leaves/custom
panels retain their native unconstrained desired size. No content or template
is reconstructed for measurement. Captured tree references are cleared when
presentation/content retires.

The shared minimum-width regression exercises 24 native states across both
themes: original text/actions, fixed-size checkbox content, scrolling viewports,
live Width/MinWidth changes, caller measure overrides with growing/shrinking
minimums, and complete caller template replacement. It verifies the actual cell
width, outer overflow, original content/template identity and loaded state.
Twenty corrupted reports are rejected. Run the source catalog or the independent
package consumer with:

```powershell
./scripts/verify-table-minimum.ps1 -AppDirectory PATH -Output artifacts/table-minimum
node scripts/test-table-minimum-audit.mjs artifacts/table-minimum artifacts/table-minimum/negative-audit.json
```

Use `-HostName PackageConsumer` for the consumer. These checks keep windows hidden
and send no hardware input. They establish the native minimum-width contract,
not full raster parity or physical-input acceptance.

The updated catalog and unchanged `0.1.0-table-scalar-text-v1-local` package
repeat all 24 states in source, package and extracted archive hosts (72 total).
All 48 source-to-delivery image pairs are byte identical, and each host's auditor
rejects 20 corrupt reports. Both consumer architectures build with the new
fixture using the previously verified package-version cache; this is not another
fresh-version restore. Both fictional component demos remain in the catalog.

Cells relay native measure invalidations to the shared table layout, because a
shared row panel can keep the same desired size when bound text shrinks. The
retained-instance regression checks that shorter text retires overflow and
restored text grows the same column, preserving original controls, objects and
templates. This uses native layout invalidation, with no per-frame tree polling.

Collection measurement uses the actual public `BleeckerSimpleDataTable` with 32, 128 and 512
fictional rows, three columns and checkbox selection. A 16-row warmup precedes
each process. Ten operations cover mount, ascending/descending sort, select all,
clear selection, item notification, source reversal, releasing sort, removal and
reinserting an original item. Mutation time and time through three dispatcher
idle/layout passes are recorded separately. Screenshots, verification, data
preparation and report persistence are outside those timers. Managed allocation,
process CPU, GC counts/pauses and private-memory deltas are diagnostics. GC is
not forced. This is a local x64 workload, not an established performance budget
or a comparison between UI frameworks.
The three-pass interval does not wait for checkbox state animations to finish.

Three baseline and three candidate processes retain identical semantic outcomes
and 18 complete PNG files. At 512 rows, median settled ascending sort improves
from 2276.30ms to 1028.32ms; descending sort from 2416.77ms to 1237.55ms, and
releasing sort from 2282.68ms to 1090.14ms. Retained-row unloads fall from 511
per reorder to zero. The benchmark rejects the real prior unload cases. Initial
mount is still slow and regresses in this sample: 5249.35ms to 5489.25ms.
Selection timings have overlapping ranges. These limitations remain explicit
acceptance work; do not describe the entire component as fast or accepted.

Reconciliation uses native UIElementCollection.Move for retained rows, cells and
sections, preserving actual child order and parent continuity. Reference-identity
sets replace repeated membership scans. Plain cells create rich-content helpers
only when needed and reuse their word probe. Unchanged plain intrinsic sizes
are cached; content/template, typography, width constraints, padding/borders,
margins, language, direction and rounding changes invalidate them. Text scale
and XamlRoot rasterization scale are checked before reuse. Rich templates keep
their live native measurement path.

Table cells share their private dependency-property identifier list and
owner-dispatched template/load/unload delegates. Each cell still registers its
own native property listeners; event callbacks obtain the affected cell from
the native owner or event sender. The content, inline-action subscription and
minimum-width cleanup paths retain their existing per-cell state. This removes
construction allocations without caching native cells or caller content.

For explicitly sized rich subtrees, the minimum-width result depends on Width,
MinWidth, MaxWidth and margins. Measurement now stops at that boundary rather
than traversing its descendants. Native layout still measures the original
content normally. This preserves the existing [WinUI width constraint
priority](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.width?view=windows-app-sdk-2.0).

Live rich-content constraint changes also invalidate the shared columns through
dependency-property observers. Native desired size can remain unchanged when
releasing a bound width, so relying solely on the outer cell's native measure
invalidation left a real prior case at 302 DIPs instead of 548.80 DIPs. Fourteen
additional light/dark snapshots cover fixed width, changed text, maximum width,
minimum-over-maximum, releasing to Auto, restoring the explicit width and changing margins. They
retain the original border, text and all four binding expressions. The earlier
two failed Auto snapshots are frozen as regression counterexamples. The full
template suite now records 70 PNGs, alongside its existing 24 alignment
snapshots and 32 template states. Observers are released when content or
templates change, cells leave their table, or the cell unloads; no visual-tree
polling is introduced.

The default cell's `PART_CellContent` grid also accounts for the font descent
below an empty inline action's synthesized baseline. The source synthesizes this
baseline from the button's content box, inside its bottom border, as described
by the [Flexbox baseline rules](https://www.w3.org/TR/css-flexbox-1/#flex-baselines).
Changing the button label, loading state, content template, border or cell
typography updates that measurement. Button property callbacks invalidate the
shared table even when the native button's fixed height stays unchanged; they
detach when content/templates change or the cell unloads. The inset belongs to
the default grid; the original button's margin and bindings remain caller-owned.
A replacement cell template without that optional grid owns its presentation.
Twenty-eight additional light/dark states cover these transitions and restoration,
bringing the template suite to 98 original native PNGs.

The follow-up three-run comparison against the retained-row version preserves
all 18 benchmark PNGs. At 512 rows, ascending sort's median three-pass time
falls from 1028.32ms to 760.49ms, with managed allocation falling from
10,413,312 to 5,407,592 bytes. Mount changes from 5489.25ms to 5418.01ms with
overlapping ranges, so a cold-mount speed improvement is not established.
Results remain mixed: deselect-all increases from 171.34ms to 303.27ms with
non-overlapping ranges. The full timing/allocation ranges are retained in
`artifacts/current/table-cold-load-comparison-v1.json`. Initial loading,
selection timings, broader workloads and a performance acceptance budget
remain open; this change does not establish overall performance acceptance.

Run verify-table-performance.ps1 and test-table-performance-audit.mjs for current
correctness/lifecycle checks. compare-table-performance.mjs accepts three
baseline and three candidate folders and reports medians and complete ranges;
the recorded comparison is artifacts/current/table-performance-comparison-v1.json.

The 2026-10-24 follow-up evaluated a per-measurement text-width memo, shared
private probes and lazy per-cell probe allocation. Each prototype remains
preserved as experimental evidence and has been reverted because timings were
mixed. The library's prior measurement implementation is restored. The lazy
prototype passed the native template, multilingual-text, customization and
live-data contracts, but that does not establish a performance improvement.
The corrected benchmark avoids strongly retaining retired native rows and
evaluates explicit three-process budgets. See the current
[Data Table performance evidence](data-table.md#current-performance-acceptance-budgets).

The separate native phase profiler now distinguishes queued intrinsic layout
from mutation, verification and capture. Its generated source reverses exactly
to the qualifying fixture and its diagnostic format is rejected by normal
performance gates. A subsequent attempt to avoid unused visual-child queries
preserved all 360 behavior/lifecycle snapshots but missed its allocation
criterion; it is preserved and reverted. The prior implementation remains in
place. See the same performance evidence for the complete measured results.



## Header label raster diagnostic

The source catalog and independent consumer recognize
`--probe-hidden-table-label-raster --verify-output FILE`. This is a developer
fixture for the reusable sortable header. It retains the original 98 template
captures and records 24 alignment states in both themes and directions. Each
state has its original frame, an immediate repeated render, and a render after
another layout cycle: 72 label observations and 48 additional raw PNGs.

Reports include the label's font, tracking, current generated-theme palette,
baseline, native bounds, ancestor transforms and grid widths before and after
each render. Report v2 also records native margins. The normal template fixture
and collection timing fixture retain their existing expectations and budgets.
The diagnostic keeps its original frames and never aligns, resamples or masks
images to make comparisons pass.

Run `scripts/verify-table-label-raster.ps1` with an application directory,
`Orchard.Catalog` or `PackageConsumer` host name, and a fresh output directory.
The accompanying JavaScript audit verifies all 146 PNGs, the native hidden-window
boundary, inherited typography and generated token colors. The v2 audit also
reports tracking margins that disagree with the current direction. Capture
failures and observation timeouts do not authorize a restart or foreground input.

The initial source/NuGet observation has 96 exact same-host comparisons and
69 exact cross-host comparisons out of 72. The three differing captures share
one RTL alignment state: its label moves by 0.8800048828125 DIP, while its
parent, font, color, baseline and size agree. This equals the header's 11px font
size times 80/1000 tracking. The v2 probe below established the stale margin
behind this mismatch. See
`artifacts/current/table-label-raster-comparison-v1.json`.

### Tracking-margin repair

The v2 native probe established a stale derived-property failure: an RTL label
retained a right margin of 0.8799999952316284 DIP with zero left margin, while
its current font and tracking require the inset on the left. Six before/after
snapshots of the Light/RTL/bound state reject that placement; the ordinary
98-frame template audit still passes. The failed report remains at
`artifacts/current/table-label-raster-margin-source-v1`.

The repair synchronizes the existing label layout during native load
and measure, as well as its existing property-update path. When tracking moves
between sides, it invalidates the template grid's arrangement because the total
desired width stays unchanged. Caller styles, native focus/hover behavior, sort
requests and the tracking formula remain intact. The source, independent NuGet
consumer and extracted catalog each pass all 72 label observations, nine table
suites and 24 minimum-width states. Their audits reject 63 corrupted label
reports and 60 corrupted minimum-width reports in total. All 144 cross-host
label pairs have identical raw pixels and before/after layout metadata. The
normal diagnostic host is unchanged between the frozen baseline and candidate;
only the library DLL changes.

The local delivery version is `0.1.0-table-tracking-v1-local`. Both x64 and ARM64
builds and independent consumer builds pass, alongside 496 portable and ten
Windows text tests. ARM64 execution remains unverified. Both fictional
monitoring/editorial demos pass 66 hidden checks in the source and archived
catalog. These checks do not establish foreground input or full component
acceptance.

Across the broader delivery replay, 893 of 1,100 PNG pairs are byte identical;
207 differ, with at most 79 pixels and a maximum channel difference of 98.
Those remaining differences are unexplained. All 48 minimum-width image pairs
are identical. The original captures, masks and tolerances remain unchanged.
The unchanged collection budgets still have 27 timing misses; all allocation
budgets pass. See `artifacts/current/table-label-tracking-label-comparison-v1.json`,
`table-label-tracking-delivery-images-v1.json` and
`table-label-tracking-performance-v1.json` for the separate results.

Earlier package runs remain recorded as failures: one could not write its final
label report because C: was full; another exited with a stack overflow during
disk pressure. The stack overflow's exact cause is unverified. Fresh label and
live-data replays pass; the failed runs are retained and are not counted as
successful verification.

The diagnostic wrapper accepts `-ImmutableManifest` for a frozen runtime under
`artifacts/current`. It verifies each identity hash before linking evidence to
that immutable runtime. Without this argument it copies identity files; the
catalog host DLL is large, so prefer a verified immutable manifest when space
is limited. Never link evidence to mutable SDK output directories.
