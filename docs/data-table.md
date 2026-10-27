# Data Table

The `0.1.0-text-media-v3-local` NuGet library checkpoint
(`13ab2e380421a2fb0850f1ffe2bcdcb157b1952d4d3175812859c6ed730f0f9e`)
passes a focused hidden x64 replay in its independently built package consumer:
20 semantic, original-content and unload operations at 16,384 source rows and
page size 100. The next-page mutation plus three layout passes took **1,727.45
ms** in this one process, exceeding the unchanged 1,000 ms local budget;
managed allocation was 19,854,616 bytes. The report is
`artifacts/current/text-media-v3-data-table-paging-package-x64-v1`, with raw
report SHA-256 `e7ddad2b9a27bdd5fa949eef6aa09f70833a41ba496f5a984a35da127703bf6f`.
This focused result is that package's functional evidence, not a three-process
median or full 120-profile performance acceptance. The Data Table remains open.

The newer `0.1.0-text-media-v4-local` package passed Media Library checks, but
its separate general consumer smoke test failed at the Data Table toolbar's
held-Reset geometry/early-invocation assertion. The fixture previously omitted
the measured bounds and event counts from its failure report; the next
authorized foreground run will include those diagnostics. This unresolved
failure does not establish a Data Table regression or a passing v4 general
consumer runtime. See the [v4 local preview record](releases/2026-10-26-text-media-v4-local.md).

A source-built x64 catalog library (`205c4521…`, source commit
`96516e7`) passes the focused hidden 16,384-row/page-100 replay: all 20 semantic
snapshots, bounded native rows, caller-owned content and unload checks pass.
Its next-page three-dispatcher-pass time is **1,675.29 ms**, above the unchanged
1,000 ms budget; managed allocation is 19,186,008 bytes. The prior source run
at `3939c80` measured 1,678.42 ms and 18,599,680 bytes. These are separate
single-process observations with different binaries, not an optimization
comparison or a three-process median. That report is
`artifacts/current/data-table-current-96516e7-paging-v1`; it does not qualify
the full 120-profile performance matrix. No library optimization or budget
change followed this observation. The rejected zero-width-measure and
row-container-reuse experiments below remain rejected.

An earlier published `0.1.0-menubar-peer-metadata-v1-local` NuGet library has a full
performance baseline under `artifacts/current/data-table-container-reuse-full-baseline-v1`
through `v3`, summarized by `data-table-container-reuse-full-baseline-budget-v1.json`.
Three independent sequential x64 processes pass all 120 functional profiles
apiece (360 measurements), with matching semantic snapshots and unload contracts.
The unchanged three-process-median budgets fail 25 timing profiles; all
allocation budgets pass. At 16,384 rows and page size 100, next-page processing
takes 1,622.3328 ms median (1,522.4139–1,753.6471), above the 1,000 ms limit.
This is current evidence for library SHA-256
`c0597e9f4cb9499cc09aafb59da2b321f6e302c7fe11eecf5d08a357a9110483`;
older results below retain their original binary scope. The different failure
count does not establish a controlled latency improvement.

An unadopted row-container reuse experiment, library SHA-256
`1db5bbb3129801f5a2cd356f6887750056a31cc33bc4e2359be97556775fc819`,
matches the baseline's 120 semantic/lifecycle snapshots. Its three-process
median for that paging case is 1,361.4763 ms, with 31.79% less managed allocation.
Twelve timing profiles still fail, including a newly failing 2,048-row,
10-row-page next-page case (376.0366 ms against 200 ms). These results do not
establish performance acceptance or authorize replacing the published package.
The raw comparison, budget failures and image differences remain in
`artifacts/current/data-table-container-reuse-full-results-v1.json`.

Three additional alternating pairs reproduced slow small-page results in both
binaries. Across all six processes per version, that case's median is 253.7109 ms
for the baseline and 299.6877 ms for the prototype; both exceed 200 ms.
Large-page medians are 1,613.9699 and 1,328.6983 ms, still above 1,000 ms.
These pooled observations supplement the two separate three-process budget
audits; they do not replace or erase either audit's failures. The prototype is
shelved and its production source edits are restored. See
`artifacts/current/data-table-container-reuse-confirmation-results-v1.json` and
`data-table-container-reuse-shelved-v1.json`.

The shared performance fixture retains five caller-content lifecycle checks:
default cells, a cell template, an implicit row style, local row data context and
local cell data context. Paging away and back must preserve original data,
editors and local edits, unload retired content, and restore the correct
presentation/context. Caller-controlled presentations must retain their existing
container lifecycle. Both the catalog and independent consumer compile this
fixture; the published baseline passes all five checks. The audit rejects
missing modes, lost edits, loaded retired content and recycled custom containers.
The independent x64 consumer of the exact published NuGet package also passes
all five cases and the 20-operation focused paging replay. Its evidence is under
`artifacts/current/data-table-caller-contracts-package-native-v1`. This verifies
caller-content behavior through the package boundary; it does not qualify the
full 120-profile performance matrix or establish passing timing budgets.

The current source's hidden reference fixture writes a small failure report
and exits with code 1 when capture, input or report generation fails. If the
failure report itself cannot be written, it still exits with code 1 and writes
the original error to stderr. The older October 25 catalog ZIP does not include
this test-host repair. Verify both failure paths with
`scripts/test-data-table-reference-failure.ps1 -AppDirectory <current-catalog> -Output <fresh-directory>`.
An incomplete report or a still-running process cannot count as a passing run.

A separate focused diagnostic trace is retained under
`artifacts/current/data-table-current-dae-profile-v1`. It again identifies shared
intrinsic measurement as an investigation area, without proving a safe repair.
It includes warmup/setup and sampled thread time, not CPU utilization or a
qualifying timing run. Plain-text caching is already present; prior language,
nowrap and zero-width shortcuts remain rejected. No library optimization or
budget relaxation was made for this baseline.

The navigation-arrow repair first shipped in `0.1.0-table-navigation-v1-local`. It fixes disabled
pagination arrows in the Data Table and its pagination companion, with fresh
independent package builds and focused package/catalog runtime verification.
See [pagination arrow foreground](#pagination-arrow-foreground) for the evidence
and remaining limits. The current `0.1.0-table-content-foreground-v2-local` preview
also repairs Reset/Columns caption and vector colors and deferred glyph
reconciliation after reparenting. See [the toolbar evidence](data-table-toolbar-reference.md).
Earlier preview results below retain their original scope.

The preceding cell-metadata local preview shares immutable property
identifiers and owner-dispatched lifecycle delegates. Its normal benchmark
fixture and native measurement behavior are unchanged. Three alternating
baseline/candidate process pairs preserve all 720 semantic/lifecycle outcomes.
Both versions fail 24 of the fixed 120 timing budgets; all allocation budgets
pass. For 16,384 rows/page 100, median next-page allocation changes from
19,569,424 bytes (17,818,584–20,431,056) to 17,767,560 bytes
(17,700,984–18,206,064). The ranges overlap, so this is an observed allocation
reduction rather than a reliable latency improvement. Fourteen of 18 paired
PNGs are byte identical; four have one to four unexplained differing pixels
(maximum channel difference 20). No image masks or thresholds change. Minimum
width, template, typography, customization and live-data native suites pass.
See `artifacts/current/table-cell-metadata-decision-v1.json`.

The `0.1.0-table-cell-metadata-v1-local` NuGet and catalog preview passes x64
and ARM64 source/independent-consumer builds, 496 portable plus ten Windows text
tests, and all nine table runtime suites in source, package and extracted
catalog hosts. The three hosts pass 72 minimum-width states, 60 corrupted-report
rejections and 252 live-data states. Source and archive pass 132 monitoring and
editorial demo checks using fictional data. The selected library supplies 600
normal collection-operation observations across its three source processes and
single package/archive delivery processes; only the three source processes
establish medians. Native ARM64 runtime remains unverified.

The complete delivery comparison has 1,013 byte/RGBA-identical pairs out of
1,100. All 48 minimum-width pairs are exact. The other 87 pairs retain up to
614 differing pixels and a maximum channel difference of 188. The two largest
differences are confined to header-label pixels. A same-source template repeat
produces 98 identical PNGs and does not establish the cause of the delivery
differences. No masks, tolerances or acceptance budgets change. See
`artifacts/current/table-cell-metadata-delivery-images-v1.json` and
`table-cell-metadata-template-repeatability-v1.json`. This is a local preview;
full visual, latency and component acceptance remain open.

The separate `src/components/data-table.tsx` component at Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066` uses TanStack Table 8.21.3. Orchard now
has a local native model/control draft and a reachable Data Table catalog fixture.
The `0.1.0-data-table-paint-v1-local` preview adds the dedicated surface palette,
canonical shadow integration and physical shadow-mask coordinates for RTL.
Its 208-state reference/paint and 84-state live suites pass in source and fresh
NuGet hosts; 12 fresh-C# spacing cases also verify container isolation and caller
local/binding precedence.
It is not accepted as complete.
The existing [basic table composition](table.md) implements `table.tsx` and has a
different state and rendering contract.

`BleeckerDataTable` accepts original objects through `ItemsSource`, immutable
`BleeckerDataTableColumn` definitions through `Columns`, and an optional
`SearchKey` matching the column ID. For nested accessor keys, dots become
underscores in the derived ID. Caller-defined IDs remain unchanged.

```csharp
var data = new BleeckerDataTable { SearchKey = "Name" };
data.Columns.Add(new BleeckerDataTableColumn(
    accessorKey: "Name", header: "Fictional item"));
data.Columns.Add(new BleeckerDataTableColumn(
    accessorKey: "Status", header: "Status",
    cellContentSelector: cell => ((FictionalItem)cell.Row.Original).Action));
data.ItemsSource = fictionalItems;
```

The original header and cell producers receive `BleeckerDataTableHeader` and
`BleeckerDataTableCell`. Their `Table`, `Column`, `Row`, `GetValue()` and
`Row.Original` members retain the caller's data/context boundary. Return native
elements to retain interactive content, or ordinary values for text cells.
The composition adds no automatic sort buttons or selection column: consumers
supply them through these producers, as the catalog fixture does. A returned
native element must have only one current native parent.

`Model` exposes sorting, filters, global search, index selection, visibility,
filtered selection counts and pagination. Column contexts provide
`ToggleSorting`, `SetFilterValue` and `ToggleVisibility`; row contexts provide
`ToggleSelected`. The same model implements the existing filter, visibility and
pagination companion interfaces. Model mutations attached to a native control
belong on its UI dispatcher. Loaded controls observe collection/item changes;
unloaded controls release those producer subscriptions. Observation uses original
object identity: distinct objects that compare equal each retain one listener,
and repeated references to one object share one listener. Reload rereads the
current `ItemsSource`, including collection edits made while unloaded. Original
item and collection notifications are dispatched to the control's UI thread.

`CellStyleSelector` and `HeaderStyleSelector` apply per-column native styles.
Caller local values and bindings retain the primitive table's precedence.
Selectors still run for each rendered cell. Equivalent generated alignment
styles can be shared by newly presented cells within one synchronous refresh;
retained cells keep their existing presentation styles. The temporary lookup
uses caller-style reference identity and does not survive the refresh or cross
control instances. Explicit alignment setters remain authoritative. Generated
styles are already sealed when assigned, including before the owner loads.
The default control template requires `PART_TableHost`, `PART_SearchHost`,
`PART_Search`, `PART_Summary`, `PART_Secondary`, `PART_Previous` and `PART_Next`.
Clearing the template retires its native visual tree; restoring it reuses the
owned primitive and original content. Colors and the card radius consume the
existing Orchard tokens. Dark mode intentionally uses Orchard's established
dark surface; the original source's white dark-mode body remains recorded below.

The portable suite passes 406 tests, including every frozen static row model
and interaction history described below. The full background native replay
covers all 42 frozen static cases and 34 histories with 166 snapshots in both
themes: 208 PNGs, 7,560 rendered cells, 48,368 geometry values within 1 DIP,
and 416 pagination-arrow paint probes. Geometry checks include header/body/row
column bounds, checkbox positions and horizontal scroll extents; the current
maximum difference is 0.273804 DIP. Actual displayed text and native button
font/weight/tracking are checked separately from original cell content.
Its negative gate rejects 56 corrupted reports, the actual pre-repair stale
filtered-row capture, and three real geometry/tracking counterexamples from
two retained native scenes. Full source raster parity remains unverified.

Native header buttons previously inherited the heading's tracking of 80 while
the pinned source button resets tracking. This inflated intrinsic column
minimums despite the realized button bounds looking correct. The button's own
default tracking is now zero. Plain-text header geometry was already correct.
The expanded audit also found that the RTL header checkbox was physically
left-aligned; the source Checkbox's block wrapper follows logical start.
Data Table now chooses that default for checkbox header content while preserving
explicit caller header styles. The unchanged pinned browser bundle reproduces
the original header geometry and button typography in ten diagnostic scenes.

That replay exposed a real reconciliation bug: clearing one filter while
retaining another reinserted original rows into the logical grid without
invalidating the shared table layout. Original action content remained unloaded
and the screen retained the smaller old result set. Structural row/header
changes now invalidate the table even when retained cell values are unchanged.
The combined-filter history and retained failed image cover this boundary.

The actual `DataTablesFixture` passes 24 interaction states, including original
caller actions, search, count-sort cycles, selection across pages and empty
results. Its 24 PNGs are captured directly in a padded 900-by-1300 native viewport;
no image resampling is used. The catalog negative gate rejects 29 corrupted
reports. The original 38-state fixture separately covers template/unload
restoration and the existing clipped-arrow counterexamples.

Both solution architectures and isolated x64/ARM64 package builds pass. Source
and x64 package reports have identical state and geometry for all 208 snapshots.
Of their 208 capture pairs, 159 are byte identical; 49 differ in 522 pixels
total, with a maximum of 154 pixels in one capture. The cause is unverified and
recorded in `artifacts/current/data-table-geometry-package-comparison-v1.json`.
Native keyboard/focus, full raster/motion/scaling,
collection performance and ARM64 runtime remain open.

`tests/Reference/DataTable.json` freezes 42 visual states and 34 interaction
histories containing 166 snapshots. A second browser run reproduces its JSON
and 42 PNGs byte for byte. Both runs use the unchanged pinned component and its
actual React 19.2.4 / TanStack 8.21.3 dependencies. The audit decodes 37,800,000
pixels and verifies 84 header/body paint samples. All examples use fictional
local rows and consumer-supplied header/cell content.

The component accepts columns, original data and an optional search key. It
owns sort, column-filter and row-selection state. Header/cell renderers receive
the actual table, column and row contexts. Those original contexts drive the
sort buttons, selection checkboxes and cell actions in the reference fixture.

- Rows receive array-index string IDs before filtering, sorting and pagination.
  Selection can remain attached to an index when its original data object is
  replaced. Removing all data retains the internal selection map while the
  filtered selection count becomes zero.
- Pages contain ten rows by default. The fixture's 23 rows produce pages of
  ten, ten and three. Filter, sort and data changes reset the current page to
  zero. The two 32-DIP navigation buttons use the actual previous/next limits.
- String-column search is case insensitive. A missing search column leaves the
  controlled input empty and produces no filter. Empty results display the
  original `No results.` cell at a height of 96 DIPs.
- The automatic filter is chosen from the column's first core-row value.
  Numeric search text `25` is interpreted by the pinned range filter as bounds
  2 and 5; `5` becomes a lower bound. Boolean search text `true` matches no
  boolean rows because the original comparison is strict. Typed consumer
  range filters and combined column/global filtering have separate histories.
- String and numeric sort cycles start in different directions and include
  removal. Multi-sort preserves the earlier field and breaks equal-name ties
  with the numeric field. Custom comparers retain original index order for
  ties. The pinned automatic sorting implementation behaves differently for
  short data arrays; the three-row history includes missing and null values.
- Selection counts include all filtered selected rows across pages. Selecting
  or clearing one page preserves selection on another page. A partial page
  selection leaves the fixture's header checkbox unchecked; that consumer
  renderer supplies no indeterminate value.
- Grouped and three-level headers render placeholders. The component does not
  apply TanStack's header colSpan to its DOM cells: each rendered header cell
  has colSpan 1. The grouped empty-result cell uses the top-level column count
  of three, even though the ordinary body has four leaf cells.
- Replacing columns with an empty array preserves ten model rows with empty,
  zero-height rendered rows. Empty data then produces a single 96-DIP result
  cell whose DOM colSpan is clamped to one. Restoring columns/data restores the
  original layout through the same React component instance.
- Consumer-provided original buttons retain their row data and action behavior.
  The fixture also covers disabled original content, caller-rendered error
  text, display-only columns, nested accessor keys, RTL and horizontal overflow.

The pinned component places `data-state=selected` on its rows while the shared
row primitive styles `data-selected`. An unhovered selected row therefore
retains the ordinary row fill. Selection metadata and checkbox state must be
distinguished from hover paint when verifying a native port.

The source's Dark body is white with pale theme text. The header/body pixel
evidence preserves that actual source appearance. Native presentation uses
Orchard's established dark tokens, as explicitly selected by the user on
2026-10-20; source evidence preserves the original appearance.

The Data Table has its own generated palette. Light presentation uses the
source's sand border at 25% opacity and light-sand header at 50%. Dark
presentation keeps Orchard's deep-sea card surface, 10% white border and 3.5%
white header. `Background`, `BorderBrush`, `BorderThickness` and `CornerRadius`
flow into the default surface. Both canonical surface-shadow recipes use the
existing CSS-shadow renderer and follow radius/size changes, unload/reload and
template replacement.

Caller templates still require the original seven parts: `PART_TableHost`
and `PART_SearchHost` grids, `PART_Search` input, `PART_Summary` and
`PART_Secondary` text blocks, and `PART_Previous`/`PART_Next` buttons. A caller
template can supply its own decoration. A `PART_Surface` border opts into the
library shadow group and then requires `PART_ShadowNear`/`PART_ShadowFar` grids
and `PART_MaskNear`/`PART_ExteriorNear`/`PART_MaskFar`/`PART_ExteriorFar` paths.

The paint fixture clips to its declared 900×1000 viewport before rendering,
without resizing captured images. Normal captures allow the canonical 190 ms
control animation to finish before checking checkbox glyph opacity and actual
fill/stroke brushes. `-CaptureEarlyDiagnostic` deliberately omits that wait;
its output cannot pass the settled-paint gate. Native surface/header/body
pixels and composition graph metadata are separate checks. RenderTargetBitmap
does not establish composition-shadow raster parity; that needs a future
desktop capture within an authorized desktop-testing window.
The shadow hosts and invisible masks keep physical left-to-right coordinates
inside RTL tables, matching their geometry recipes without changing content flow.

The current source and fresh NuGet consumer each pass 208 frozen states with
3,090 surface/header/body pixel probes (maximum channel difference one), 1,864
checkbox visual-tree endpoints and 414 composition graphs. Both themes verify
surface overrides, updated corner masks, graph disposal/restoration and caller
templates retaining the seven functional parts. Seventy-eight contexts release
their graphs when unloaded. The negative gate rejects 40 corrupted reports and
two retained actual palette/RTL-overflow PNGs. All 208 state reports match across
hosts; 48 full PNGs are byte-identical and 160 differ in 1,660 pixels in total.
The cause of those differences is unverified, including the earlier differences
before the settling wait. All 84 live producer-state reports and PNG pairs match.

Native producer notification/style-selector boundaries beyond the frozen
histories now have an additional shared source/package fixture. Its 84 states
cover both table compositions and themes, with 274 rendered rows and 626 exact
subscription counts: distinct equal objects, repeated references, collection
add/move/replace/remove/clear, retired sources/items, edits while unloaded,
reload, template restoration, original actions, style selectors and retained
caller bindings. Data Table also exercises item/collection notifications from
a worker thread; the basic composition retains its documented UI-thread rule.
The negative gate rejects 43 corrupted reports and four retained actual failed
library states, with their original PNGs preserved.
All 84 source/package live state reports and PNG pairs are identical; this
establishes repeatability between these two native hosts for those cases.

Those probes found three defects: value-equality deduplication omitted distinct
original item listeners in both compositions; Data Table reload refreshed only
its old core rows; and a basic table collection addition could leave the new
original row unloaded until an unrelated layout invalidation. Observation now
uses reference identity, reload rereads `ItemsSource`, and structural basic-table
body/footer edits invalidate shared layout.

Keyboard/focus, full raster/motion/scaling, collection
performance and ARM64 runtime still require their own evidence.

The separate `DataTablePerformanceFixture` measures this paginated composition
through its public APIs. Its six profiles use 128, 2,048 and 16,384 fictional
source rows with page sizes of 10 and 100, following a 16-row warmup. Twenty
operations cover binding data, sort button actions, paging, page/all selection,
visible/hidden item notifications, numeric ranges, the actual search input,
global search, source reversal, clearing sort, removal and reinsertion. The
existing `TablePerformanceFixture` measures `BleeckerSimpleDataTable`; its
results must not be presented as measurements of this component.
This workload uses Light presentation; it is not a theme or motion benchmark.

Mutation and three native dispatcher/layout passes are timed separately.
Control construction, caller replacement-data preparation, correctness checks,
captures and report persistence are excluded. Three passes do not establish
completed animation timing. The fixture records allocation, process CPU,
private-memory changes and GC diagnostics without forcing GC. Page-size-10
profiles retain six complete 1000×1000 PNGs; page-size-100 profiles verify the
entire realized native row/cell tree, including content below the capture viewport.
Independent C# expectations and a JavaScript replay check original objects,
full filtered/sorted order digests, actual rendered names/counts, native action
ownership, index selection, retained native rows/cells and producer subscriptions.
An absolute performance budget and overall performance acceptance remain open.

The loading diagnostic retains normal, early/late `Loaded` listener and late
`Unloaded` listener cases. Late `Loaded` subscription changed the observed
`IsLoaded` state in an earlier native run. Benchmark row tracking therefore
observes only unloads, outside timing, and still requires every measured native
row to report loaded. The earlier rejected reports remain diagnostic evidence.

Run the workload and its negative gate with:

```powershell
./scripts/verify-data-table-performance.ps1 -AppDirectory samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 -Output artifacts/data-table-performance
node scripts/test-data-table-performance-audit.mjs --native artifacts/data-table-performance --output artifacts/data-table-performance/negative-audit.json
```

Use a fresh output folder for each process and run timed processes sequentially.
The launcher polls the same recorded PID until it exits. Tool observation
timeouts do not establish that it stopped: inspect `process.json` and continue
observing that process before auditing it or starting another timing process.
The same unchanged fixture compiles into the isolated consumer; pass its app
directory and `-HostName PackageConsumer`. `summarize-data-table-performance.mjs`
accepts three comma-separated folders through `--runs`, verifies each report and
original PNG, and reports medians with all three values and complete ranges.
It establishes a local baseline without claiming a speed improvement or budget.
For consumer reports, also pass `--host-assembly PackageConsumer.dll` to that
summary, `verify-data-table-performance-budgets.mjs` and its budget regression
test. Cross-host comparisons use `--baseline-host-assembly` and
`--candidate-host-assembly` separately. Each option selects the actual retained
host DLL for SHA-256 verification; it does not rename or substitute an assembly.

For a bounded large-page investigation, `-PagingOnly` runs the same 20 operations
at 16,384 rows/page 100 after the existing 16-row warmup. It preserves the normal
mutation/three-layout-pass timing and all original-data, native-identity and
subscription checks. The audit records `focused-paging` and explicitly leaves
`fullPerformanceMatrixVerified` false. Such a report is rejected by the full
120-operation audit and cannot establish overall performance acceptance or a
three-process median. A host built before this option is rejected before launch.
Pass `-ImmutableManifest` with a frozen runtime under `artifacts/current` to
link verified identity files instead of copying them into every report folder.
Direct native invocation uses `--probe-hidden-data-table-performance --paging-only`;
direct auditing uses `verify-data-table-performance.mjs --scope paging` with its
normal native/host/output arguments. The full workload remains the default.

Three current source processes pass 360 measurements with identical semantic
snapshots and lifecycle contracts. A fresh isolated NuGet consumer repeats all
120 operations, including 6,005 rendered-row observations and 3,048 retained-row
observations per process. Both negative gates reject 69 corrupted reports and
the retained late-listener observation. Source run v4 is retained diagnostically
and excluded from the three-process summary because a JavaScript audit overlapped
its timed work. The summary uses runs v3, v5 and v6.

At 16,384 source rows and page size 10, median three-pass global-search time is
256.42 ms (255.16–333.51 ms), allocating about 12.7 MB managed memory. Binding
data is 313.94 ms (234.40–444.82 ms). Page size 100 is substantially more costly:
binding data is 2,963.35 ms (1,885.68–3,119.02 ms), and paging is 2,307.16 ms
(2,153.49–2,416.14 ms). All 120 distributions and GC diagnostics are retained in
`artifacts/current/data-table-performance-baseline-summary-v1.json`. These are
local observations, not evidence of a speed improvement or overall performance
acceptance. Those baseline measurements used the unchanged paint-phase library
and preview package, before the filtering repair below.

Supplemental pinned-source evidence covers empty global-filter eligibility,
boolean-only accessors, preserving an active boolean column filter, and hidden
eligible columns in both themes (eight cases/24 original PNGs). The source
ignores a global query when no eligible columns exist, retaining the result of
ordinary column filters. Hidden eligible columns still participate. Filtering
now resolves eligibility and numeric range bounds once per recomputation rather
than once per original row. It reevaluates them after data, column or query
changes. Caller predicates still run for every row in source order, including
predicates following an earlier rejection; mutable caller values retain their
existing conversion and iteration behavior. No filter plan persists across passes.

The six original wrong native filter states and their unchanged PNGs are retained
in `tests/Reference/DataTableFilterPlanCounterexamples.json`. A separate pair
preserves the empty-action line-box discrepancy exposed by this replay. Run
`verify-data-table-filter-plan.ps1` and its negative auditor against a fresh host;
the gate compares the actual original rows, rendered text and one-DIP row heights
with the pinned supplemental source, and rejects all eight retained failures.

Three final source processes preserve all 120 semantic snapshots and six lifecycle
contracts against that baseline. At 16,384 source rows and page size 10, numeric
range filtering changes from a median 227.06 ms (224.97–260.47 ms) to 133.89 ms
(131.48–135.84 ms). Median allocation falls from 4,518,264 to 2,774,584 bytes.
Global-filter median allocation falls from 12,720,552 to 5,611,264 bytes, while
its timing ranges overlap. Ordinary binding and paging at page size 10 also have
overlapping timing ranges.

Large-page results remain mixed. At page size 100, paging's median changes from
2,307.16 ms (2,153.49–2,416.14 ms) to 3,052.55 ms (2,176.30–3,317.48 ms).
The ranges overlap, but the higher candidate median remains recorded. The final
candidate removes an unnecessary table invalidation on every cell load: it
measures original content before resolving an empty-action inset and measures
again only if that inset changes. Its predecessor's three valid measurements
and the comparison remain available; they were not discarded for being slower.

All distributions are in
`artifacts/current/data-table-filtering-performance-comparison-v2.json`; the
intermediate layout comparison is
`artifacts/current/data-table-filtering-performance-layout-comparison-v2.json`.
Sixteen of 18 baseline/final PNG pairs are byte identical; the two remaining
differences are retained with their pixel counts and unverified causes.
These are local three-process observations, without an absolute performance
budget, complete motion timing or overall component acceptance.

## Current performance acceptance budgets

The 2026-10-25 triage reused the retained layout and native-cost diagnostics.
They locate expensive work in queued native layout and overlapping cell
measurement/construction paths, but do not establish an exclusive cause in the
current preview. The profiled older binaries cannot prove its current timings.
No new performance implementation, benchmark campaign or budget change follows
from that review. The failed normal-run budgets below remain open; another
optimization needs a distinct, evidence-backed cause rather than a repeat of
the rejected text-probe experiments.

The 2026-10-24 follow-up establishes budgets before changing the implementation.
It uses the existing 120-operation hidden native fixture in three independent,
sequential processes, after a 16-row warmup. Source counts are 128, 2,048 and
16,384 fictional rows, with page sizes 10 and 100. The host is an Intel Core
i7-8700 (six cores/twelve logical processors), about 64 GiB RAM, Windows 11 Pro
build 26200, x64 .NET 10.0.12 and the repository's pinned WinUI packages.

For every operation/profile, the median mutation-plus-three-layout-pass budget
is 200 ms at page size 10 and 1,000 ms at page size 100. Median managed allocation
budgets are 16 MiB and 64 MiB, respectively. These are local fixture budgets;
the timings exclude construction, replacement-data preparation, screenshots,
verification and motion settling, and do not establish physical input latency.
All three samples and ranges must remain visible, including failures.

Semantic acceptance retains the exact original rows/actions, source selection
and filter/sort results, page-bounded native row counts, loaded identity for
retained rows and complete unload cleanup. Descendant content, typography,
constraints, templates, language, direction and scaling must still remeasure
correctly. The proposed investigation concerns repeated rich-text minimum-width
probing inside a single synchronous shared-table measurement transaction.
Results must not persist across layout passes. The initial plan and unchanged
baseline sources are retained under
`artifacts/current/component-acceptance-performance-plan-v1.json` and
`component-acceptance-performance-baseline-implementation-v1`.
The initial three-process baseline and transaction-scoped text-measurement
cache experiment both failed 36 of 120 median operation/profile budgets. The
cache produced mixed timing changes and is preserved as an unadopted prototype;
the prior production implementation has been restored. All 360 operations in
each set passed the row, action, selection and unload contracts.

Those runs used a benchmark dictionary that strongly retained retired native
rows until each 20-operation profile ended. They remain recorded, but cannot
establish normal collection-lifetime performance. Native report v2 uses a
`ConditionalWeakTable` for unload counters so instrumentation does not retain
retired rows. Per-operation maps still retain the immediately preceding rows
for identity checks, and caller-owned action/checkbox caches remain unchanged.
The verifier preserves explicit v1 support for historical evidence, requires
the weak-tracker contract in v2 and rejects mixed-version three-process sets.
Fresh v2 baseline measurements use the same budgets; a harness correction is
not attributed to a library optimization. Performance acceptance remains open.

The three corrected v2 baselines pass all 360 semantic/lifecycle operations,
with identical snapshots and contracts. They still exceed 28 of 120 median
profile budgets. At 16,384 source rows and page size 100, next-page layout takes
1,804.5211 ms median (1,778.4321–1,878.8043 ms), exceeding the 1,000 ms limit.
All allocation budgets pass. These runs hold the original `b8808410…` library
DLL constant while using the freshly built weak-tracker host. All 566 library
source hashes match that baseline, but a new build produced different DLL
bytes; both binaries and the explicit pinning record are retained. This is a
controlled instrumentation comparison, not a reproducible-build claim.

A second experiment shared private text probes per UI thread and cleared
measured text after each use. It passed all 360 semantic/lifecycle operations
and all 18 paired native PNGs were byte identical. Timings remained mixed:
six improvements and six regressions had nonoverlapping ranges. It failed
27 profile budgets, with large-page paging increasing to 2,299.3641 ms median.
It is preserved as an unadopted experiment and has been reverted.

The narrower follow-up allocated the original per-cell probe lazily. It passed
all 360 semantic/lifecycle operations, the template, multilingual text,
customization, live-data and complete Data Table reference regressions, and
495 portable plus 10 Windows text tests. Fifteen of 18 paired native PNGs were
byte identical; the other three retained one to four differing pixels, with
their cause unverified. Timings were still mixed: ten improvements and sixteen
regressions had nonoverlapping ranges, with 26 failed budgets and large-page
paging at 1,824.3945 ms median. This prototype is also preserved and reverted.
All 566 library source hashes match the pre-experiment baseline. The weak-row
benchmark correction and explicit budget checks remain; the historical v2
baseline fails 28 profile budgets. No library performance improvement or component
acceptance is claimed from these experiments.

A subsequent instrumentation audit found that v2's per-operation identity maps
still held the outgoing native page strongly during its timed replacement.
Native report v3 uses weak row and cell references in those maps and stores
unload snapshots by row ID. Retained objects must resolve to the same live
native instances; missing or replaced retained targets still fail the check.
The profile-long unload counters remain in a `ConditionalWeakTable`. Consumer
action/checkbox ownership is unchanged. This removes the benchmark's ownership
of retired UI objects without forcing GC or changing the workload or budgets.
Three fresh v3 runs pass all 360 operations and preserve every v2 semantic and
lifecycle snapshot. Each report's auditor rejects 74 corruptions; the budget
checker passes 16 boundary/counterexample checks. The unchanged library still
fails 26 of 120 median profile budgets. Large-page paging has a 1,830.6038 ms
median (1,739.5928–1,946.3998 ms) against its 1,000 ms budget. Allocation budgets
pass. All three runs use the same frozen `b8808410…` library DLL and freshly
built v3 host, isolating instrumentation changes. The 28-failure result above
belongs to v2; neither result establishes a library speedup or acceptance.

### Shared notification callbacks

A later bounded diagnostic of the combined `22f84eba…` preview replays the
2,048-row/page-100 profile without changing the qualifying v3 fixture. All
20 semantic snapshots and unload contracts match exactly, with 168 recorded
layout-pass samples including setup, warmup and cleanup. In all 20 operations,
the dispatcher-wait stage dominates explicit measure/arrange/update calls.
This includes queued native work; it does not establish an exclusive cause.
A matching EventPipe replay preserves the same contracts. Its sampled thread
stacks place substantial inclusive time in cell intrinsic measurement, with
overlapping nested values. These are sampled thread-time observations, not
CPU utilization or a new qualifying budget result.

The resulting narrow experiment skipped a private measurement probe's native
`Language` setter when its current language already matched the requested value.
Three normal processes preserved all 360 semantic/lifecycle snapshots, and
17 of 18 native image pairs were exact. The remaining pair has five unexplained
pixels (maximum channel difference 20). Timings remained mixed: four improvements
and three regressions had nonoverlapping ranges, and the fixed budget gate
failed 28 profiles versus the baseline's 26. All allocation budgets passed.
The prototype is preserved under
`artifacts/current/table-probe-language-unadopted-v1` and has been reverted.
Both production sources and the eight verified normal host outputs were restored
by hash; the restored Checkbox report is byte identical to its sealed baseline.
No library optimization or performance acceptance is claimed from this trial.
See `artifacts/current/table-probe-language-checkpoint-v1.json` for the complete
diagnostic and rejected-candidate evidence. The existing allocation repair below
and the verified NuGet/catalog preview remain unchanged.

The next source candidate shares callbacks that already perform the same action.
Cell content-property notifications use one static delegate, receiving the
original cell as sender. Each intrinsic-content helper shares its constraint
callback across descendant registrations, and each inline action shares its
cell-invalidation callback across properties. Every property registration and
cleanup token remains independent. Measurement, content ownership, templates,
bindings and the timing workload are unchanged.

Three fresh processes of the verified Switch package (`fc99c35b…`) establish the
comparison baseline. Three source processes of the callback candidate
(`32b869ec…`) preserve all 360 semantic/lifecycle snapshots. At page size 100,
paging median managed allocations change from 28,804,360 to 21,022,112 bytes
for 2,048 rows and from 21,607,480 to 18,360,184 bytes for 16,384 rows. These
are measured allocation reductions; timing results remain mixed. Paging medians
change from 1,939.5459 to 1,893.6046 ms and from 1,719.6779 to 1,756.8891 ms,
respectively, with overlapping ranges. The fixed budget gate fails 26 baseline
and 27 candidate profiles; all allocation budgets pass. Preserve every sample
and range in `artifacts/current/table-callback-performance-comparison-v1.json`
and both budget reports. No timing improvement or performance acceptance follows.

Four of the 18 original native image pairs differ by one to five pixels, with
maximum channel differences of 13 or 20 and unverified causes. The remaining
14 pairs are byte identical. No image masks or thresholds change. The candidate
passes the existing native customization, template, text, primitive-table,
Data Table, live-data, filtering and 208-state reference-history suites, including
descendant width/margin changes, binding notifications and template restoration.
The separate paint audit verifies 3,090 probes and 1,864 checkbox endpoints;
full composition-shadow raster parity remains open. That source-only draft
predates the combined preview below; its original samples remain retained.

The combined `0.1.0-checkbox-rich-v1-local` preview uses library `22f84eba…`.
Three fresh source processes preserve all 360 baseline semantic/lifecycle
operations, and its independent NuGet consumer repeats all 120 operations with
identical snapshots and cleanup contracts. Against the same fresh Switch-package
baseline, paging allocation medians are 23,568,360 bytes at 2,048 rows and
18,395,816 bytes at 16,384 rows: about 18% and 15% lower, respectively. Timing
medians are 1,820.2414 and 1,763.5463 ms, with ranges overlapping the baseline.
The current fixed budget gate fails 26 of 120 profiles; all allocation budgets
pass. Fifteen of 18 original image pairs are byte identical; three differ by
four or five pixels, maximum channel differences 13 or 20, with unverified
causes. No masks or thresholds change. Current samples and ranges are retained
in `artifacts/current/checkbox-content-table-performance-comparison-v1.json`.
The consumer's snapshots match current source, but its single process does not
establish independent timing medians. Full performance and visual acceptance
remain open.

### Render-pass alignment styles

The later render-pass style candidate (`2dabe1ac…`) shares equivalent generated
alignment `Style`/`Setter` objects among newly presented cells in Data Table and
Simple Data Table. The per-cell cache for retained presentations is unchanged.
An explicit-refresh regression verifies one selector call per rendered body and
header cell. Both the original library and candidate pass the same 84 live
states with exact state and cleanup contracts. A detached-owner probe verifies
that generated styles are sealed before owner load and reject mutation in both
versions; four equivalent new-cell styles become one while caller bases remain
intact. This does not introduce a mutable shared-style window.

Three normal candidate processes preserve all 360 semantic/lifecycle operations.
At page size 100, paging median allocations change from 23,568,360 to 21,344,160
bytes for 2,048 rows (9.4% lower) and from 18,395,816 to 17,923,208 bytes for
16,384 rows (2.6% lower), with nonoverlapping allocation ranges in both cases.
Paging timing medians change from 1,820.2414 to 1,635.9316 ms and from 1,763.5463
to 1,689.5584 ms. The former ranges do not overlap; the latter do. Timings remain
mixed across the workload: 12 improvements and four regressions have
nonoverlapping ranges. Both versions fail 26 fixed timing budgets; all allocation
budgets pass. Fifteen of 18 image pairs are exact, with one/four/five unexplained
pixels in the remaining pairs and unchanged zero thresholds.

Native template, text, customization, primitive, Simple Data Table, Data Table,
live-data, filtering and 208-state reference suites pass. The paint gate verifies
3,090 probes and 1,864 checkbox endpoints. These source results establish the
bounded allocation improvement, not full performance, visual, motion, scaling
or native ARM64 acceptance. Source, package and catalog qualifications are
recorded separately in `artifacts/current/table-column-style-checkpoint-v1.json`.
The resulting local preview is `0.1.0-table-style-v1-local`. Independent x64 and
ARM64 consumers freshly restore that version using existing third-party caches;
x64 source, consumer and catalog each repeat all 120 table semantic operations
and 84 live states with exact raw states and cleanup contracts. The catalog
contains a dedicated 550-file self-contained publish output. Delivery timings
remain separate from the qualified three-process `fa038fb1…` benchmark host.

### Separate native-cost diagnostics

`create-data-table-cost-fixture.mjs` generates a phase-instrumented copy of the
unchanged native v3 fixture. Its reversible source transformation preserves the
workload and marks setup, mutation, queued layout, explicit layout, verification,
capture and cleanup. A temporary `CustomAfterMicrosoftCommonTargets` file replaces
only the catalog fixture; it is excluded from the library's compilation. Preserve
the normal host outputs before this diagnostic build and restore them by hash
after collection. The normal fixture source is not edited.

The hidden host waits for the `Orchard-DataTable-NativeCost` EventSource provider,
then runs the original 120 operations. Collect it with the pinned `dotnet-trace`
tool using `dotnet-sampled-thread-time`, the custom provider, and verbose GC
allocation events (`Microsoft-Windows-DotNETRuntime:0x1:5`). Avoid the broader
handle/exception event sets for this investigation. The tool uses
[EventPipe](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventpipe);
its [sampled-thread-time profile](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)
does not establish kernel or native CPU time. Diagnostic reports have a separate
format and are rejected by the normal benchmark and budget gates.

The first trace matches all 3,716 markers and every v3 semantic/lifecycle
snapshot. In this instrumented run, timed mutation/layout accounts for about
64.3 seconds and 763.9 MB of approximate process-wide allocations; verification
accounts for about 8.1 seconds and 342.9 MB. Most queued-layout samples include
intrinsic measurement, and WinRT object references appear prominently in sampled
allocations. These are diagnostics, not normal benchmark results. Phase intervals
are nested; summing them double-counts work. Allocation ticks are estimates, and
a GC pause's timing does not identify which earlier allocations caused it.

`analyze-data-table-native-cost.mjs` checks the raw diagnostic report, images,
baseline snapshots and complete phase nesting, then attributes recorded managed
caller stacks by phase. `test-data-table-cost-fixture.mjs` verifies source
reversibility and rejection at the diagnostic/normal evidence boundary. The
normal eight output files were restored after the trace. A further normal
candidate avoided unused visual-child enumeration in intrinsic measurement.
It preserved all 360 semantic/lifecycle snapshots, but failed its predeclared
allocation criterion: the 2,048-row/page-100 next-page median increased from
25,670,440 to 25,913,464 bytes. It failed 28 budgets versus the baseline's 26.
Four of 18 paired PNGs retain one to five differing pixels at the unchanged
zero threshold. The candidate is preserved and reverted; all 566 library source
hashes and eight normal output files match the baseline again. It establishes
no library performance improvement.

An additional plain-cell experiment reused the realized text width when the
string had no word-break opportunity, avoiding a second text measurement for
those cells. Three alternating baseline/candidate x64 focused-paging pairs
passed all 20 operations per run, with matching semantic snapshots and caller
lifecycle contracts. At 16,384 rows/page 100, next-page medians were 1,686.8
and 1,655.9 ms; their ranges overlap and both exceed the 1,000 ms budget.
Other timings moved in both directions, including a slower clear-global median.
The change was reverted; a rebuilt normal x64 host passed the same 20 focused
operations with matching semantic snapshots and caller lifecycle contracts.
This focused result neither qualifies the full
120-profile matrix nor establishes a reliable speedup. Raw reports and the
decision are under `artifacts/current/table-no-break-*-v[1-3]` and
`artifacts/current/table-no-break-decision-v1.json`.

Run the diagnostic workflow from the repository root after a normal x64 build
and a matching qualifying benchmark run. Use fresh output directories. Install
the pinned tool once with `dotnet tool install dotnet-trace --tool-path
artifacts/diagnostics/dotnet-trace-v10 --version 10.0.750501`. Then:

```powershell
$costApp = 'samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64'
dotnet build scripts/diagnostics/NativeCostTraceReader/TraceReader.csproj -c Release
node scripts/create-data-table-cost-fixture.mjs --source tests/PackageConsumer/DataTablePerformanceFixture.cs --output artifacts/cost-generated --normal-host $costApp
$costTargets = (Resolve-Path 'artifacts/cost-generated/NativeCost.targets').Path
dotnet build samples/Orchard.Catalog/Orchard.Catalog.csproj -c Release -p:Platform=x64 -p:UseSharedCompilation=false -p:CustomAfterMicrosoftCommonTargets=$costTargets --no-restore
./scripts/profile-data-table-native-cost.ps1 -AppDirectory $costApp -Output artifacts/cost-profile -NormalBackup artifacts/cost-generated/normal-backup -Baseline artifacts/qualifying-normal-run
```

The collector rejects a normal host before launching it. It starts the diagnostic
host hidden and restores all eight normal output files by hash in `finally`
before decoding or converting the trace. It then verifies semantic and phase
evidence. Output directories use NTFS compression, and collection/conversion check
available capacity. The reader uses
the exact TraceEvent assemblies installed with the pinned tool. Collection is a
diagnostic workflow and remains separate from the three-run budget check.

The broader second trace hit disk exhaustion in the converter's temporary index;
its initial restoration also failed. The raw trace and failure logs are retained,
and all eight normal outputs were subsequently restored and verified after
compressing completed generated files. A later conversion-space check exposed a
PowerShell 32-bit overflow and was corrected to use 64-bit sizes. The fourth run,
using GC events without handle/exception events, completes collection, restoration,
decoding, conversion and analysis. It matches all 3,716 markers and all baseline
behavior/lifecycle snapshots. The qualifying 26 budget failures remain unchanged.

The committed local thresholds are in
`tests/Reference/DataTablePerformanceBudgets.json`. Audit three fresh native v3
runs with the command below. It validates the run identities, semantics and
frames before evaluating each operation's three-process median, writes all
samples and failures, and returns a failing exit code for any exceeded budget.
It does not establish component acceptance or physical-input latency.

```powershell
node scripts/verify-data-table-performance-budgets.mjs --runs artifacts/run1,artifacts/run2,artifacts/run3 --budgets tests/Reference/DataTablePerformanceBudgets.json --output artifacts/data-table-budget-audit.json
node scripts/test-data-table-performance-budgets.mjs --native artifacts/run1 --budgets tests/Reference/DataTablePerformanceBudgets.json --output artifacts/data-table-budget-negative.json
```

The `0.1.0-data-table-filtering-v2-local` preview is verified in fresh isolated
x64 and ARM64 consumer builds. Its x64 runtime repeats all 120 collection
operations, 24 supplemental filtering states, 208 reference states, 84 live states
and the shared table's 98 template captures. Source and consumer state reports
match; all 24 supplemental and 98 template PNG pairs are byte identical. The
148 loose XAML files, native PRI, six bundled fonts and package README match
current source. ARM64 runtime and full component acceptance remain unverified.

Run the full native and actual catalog checks against a fresh x64 build with:

```powershell
./scripts/verify-data-table-reference.ps1 -AppDirectory samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 -Output artifacts/data-table-reference
node scripts/test-data-table-reference-audit.mjs --native artifacts/data-table-reference --output artifacts/data-table-reference/negative-audit.json
node scripts/verify-data-table-paint.mjs --native artifacts/data-table-reference --output artifacts/data-table-reference/paint-audit.json
node scripts/test-data-table-paint-audit.mjs --native artifacts/data-table-reference --output artifacts/data-table-reference/paint-negative-audit.json
./scripts/verify-data-table-catalog.ps1 -AppDirectory samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 -Output artifacts/data-table-catalog
node scripts/test-data-table-catalog-audit.mjs --native artifacts/data-table-catalog --output artifacts/data-table-catalog/negative-audit.json
./scripts/verify-data-table-live.ps1 -AppDirectory samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 -Output artifacts/data-table-live
node scripts/test-data-table-live-audit.mjs --native artifacts/data-table-live --output artifacts/data-table-live/negative-audit.json
```

The reference fixture is compiled unchanged into the isolated package consumer;
pass `-HostName PackageConsumer` and that consumer's app directory to its wrapper.
These checks keep windows hidden and send no physical input or focus requests.

Run the source checks with:

```powershell
node scripts/reference/verify-data-table-source.mjs --reference tests/Reference/DataTable.json --frames tests/Reference/DataTable --output artifacts/data-table-source-audit.json
node scripts/reference/test-data-table-source-audit.mjs --reference tests/Reference/DataTable.json --frames tests/Reference/DataTable --output artifacts/data-table-source-negative.json
```

Capture commands require the pinned Bleecker source/cache, its lockfile-matched
dependencies, the repository's reference tooling and headless Edge. The capture
script verifies every CSS-scanned source file against the pin and records all
bundled input hashes. The upstream [filtering guide](https://tanstack.com/table/v8/docs/guide/column-filtering)
and [pagination guide](https://tanstack.com/table/v8/docs/guide/pagination)
provide context; the exact installed version and recorded runtime determine
the contracts above.

### Scalar text allocation

Cell text, search inputs and toolbar values share the same source-compatible
conversion. Its reference-identity cycle path is now allocated only when the
value enters an enumerable array. Plain text, numbers, booleans, null and object
labels do not create an unused set. Nested arrays keep their original iteration
and coercion behavior; repeated noncyclic branches remain valid and cyclic
arrays still throw. The path remains private to each conversion call.

Three fresh processes for each version preserve all 360 semantic and unload
results. At 16,384 source rows and page size 100, median search allocation falls
from 21,893,432 to 18,031,760 bytes (17.6%); global-filter allocation falls from
15,143,032 to 13,075,520 bytes (13.7%). The 2,048-row counterparts fall 5.9% and
2.9%. All allocation budgets pass. Timings remain mixed: 12 improvements and
seven regressions have nonoverlapping ranges, and the candidate misses 26 timing
budgets versus the fresh baseline's 25. Large-page paging takes 2,028.0002 ms
median versus 1,679.1731 ms, with overlapping ranges. This does not establish a
latency improvement or performance acceptance.

Twelve of 18 paired native captures are byte identical. The other six differ
by one to five pixels, with maximum channel differences of 13 or 20; their cause
is unverified. No pixel masks or tolerance changes are applied. See
`artifacts/current/table-scalar-text-comparison-v1.json` and
`table-scalar-text-budget-v1.json` for the complete measurements.

The `0.1.0-table-scalar-text-v1-local` preview packages the measured
`de9697d8…` library. Independent x64 and ARM64 consumers freshly restore and
build that version. Source, package and extracted catalog pass 208 reference
states, 84 live-update states and 24 supplemental filtering states each. The
source's three qualifying collection runs plus one package and one archive
delivery replay pass 600 current-library operations. The latter two runs verify
delivery behavior and are not additional three-process timing qualifications.
Both fictional-data demos pass 66 checks in the source host and 66 in the
extracted catalog. ARM64 runtime and full component acceptance remain open.

Across 644 source/package/archive image pairs, 434 are byte identical and 210
differ. Of the differing pairs, 181 have a maximum channel difference of one.
The reference suite's largest pair differs at 100 pixels with a maximum channel
difference of 168, including checked-glyph edges. Two collection-workload pairs
differ at one or four pixels with maximum channel differences of 20 or 13.
Their causes remain unverified. Each host separately passes the unchanged paint
audit's 3,090 probes and 1,864 checkbox endpoints; those checks do not establish
full-frame equality. See `table-scalar-text-delivery-images-v1.json`.

### Rejected constrained-measure experiment

A later experiment conditionally omitted the rich cell's zero-width native
measure for recognized presentation trees. Its 24 minimum-width boundary states,
506 source tests and 360 collection outcomes passed. The initial paging median
fell from 2,028.0002 to 1,756.4405 ms, but the sample ranges overlapped. This was
not sufficient evidence of a reliable improvement.

Three alternating baseline/candidate pairs using frozen, verified application
closures did not reproduce the reduction. Baseline paging measured 1,737.7755 ms
median (1,722.8281–1,793.7078); candidate paging measured 1,781.3508 ms
(1,668.3922–1,835.7750), a 2.5% higher median. The candidate missed 27 timing
budgets versus the controlled baseline's 25. All allocation budgets passed and
all 720 controlled semantic outcomes matched. The fixed timing budgets were not
relaxed. The prototype is preserved under `table-zero-width-unadopted-v1`, and
all three library source files were restored byte for byte. The new boundary
fixture remains; the optimization is not adopted.

The first incremental restore build reused the prototype DLL because restored
files retained older write times. Its tests are explicitly not counted as
restored-library verification. Advancing those three source times caused a fresh
build with no prototype symbols, but different DLL bytes. That rebuild is kept
separately. Delivery continues to select the unchanged qualified `de9697d8…`
library and the normally compiled pre-experiment catalog host, using verified
immutable runtime files; the existing NuGet package is unchanged. The fresh
SDK rebuild and the selected delivery binary are not described as byte identical.

### Header tracking repair

A separate source/package diagnostic records sortable-label fonts, palette,
geometry, native margins and repeated raw renders. It established a stale
0.88-DIP tracking inset on the wrong side of an RTL label. The header now
synchronizes its derived layout during native load and measure and invalidates
arrangement when the inset changes sides. All 144 source/package/archive label
pairs now have identical raw pixels and layout metadata. The local package
version is `0.1.0-table-tracking-v1-local`. See
[the header diagnostic](table.md#header-label-raster-diagnostic).

All nine table suites pass in the source, independent consumer and extracted
catalog, including 252 live-data states across those hosts. Five collection
runs verify 600 operation outcomes. The three normal source runs still miss
27 fixed timing budgets, while every allocation budget passes. Paging 16,384
rows with a page size of 100 takes a median 1,630.7153 ms across those runs
(1,583.2063–2,460.1246 ms), against the unchanged 1,000 ms budget. These samples
do not establish a latency improvement. Broader image parity remains incomplete:
207 of 1,100 delivery pairs differ, even though all header-label pairs agree.
See `artifacts/current/table-label-tracking-performance-v1.json` and
`table-label-tracking-delivery-images-v1.json`.

### Pagination arrow foreground

The disabled-arrow regression found that the native button's base `Foreground`
stays fully opaque while its active content presenter applies the pinned
45% disabled foreground. Binding the arrow to the base value therefore left it
at full contrast. The same producer mismatch affected the separate
`BleeckerDataTablePagination` companion.

The working repair binds owned navigation paths to the current native
`ContentPresenter.Foreground` when their glyphs load. It releases its own
bindings on unload and reconnects to replacement presenters. Caller button
foreground bindings and caller presenter colors remain intact. Existing caller
templates that host content directly retain the button's own foreground as
their producer. Presenter names can differ from the default. No opacity constant, token,
public API, input behavior or glyph geometry changes.

`--probe-hidden-table-navigation-foreground` is registered in the source catalog
and independent consumer. Its shared fixture covers 96 states and 88 raw frames:
first/middle/last and single pages, inherited disabling, live themes, unload and
reload, template removal/restoration, caller templates and bound foreground
updates, including direct-hosted caller content. The controlled baseline has
56 foreground/ink failures and eight retained-binding failures. The final repair
passes all 264 button observations with zero foreground, ink or binding-release
failures, including direct-content caller templates. Eighteen corrupt reports,
blank glyphs and actual pre-repair opaque glyph images are rejected. The image
audit requires visible glyph ink; metadata equality alone cannot pass it.
x64 and ARM64 library builds pass. Nine broader table suites passed the earlier
presenter repair; the final compatibility branch has the focused 96-state
runtime proof. The `0.1.0-table-navigation-v1-local` package restores and builds
in independent x64 and ARM64 consumers. The x64 package and extracted catalog
each pass the same 96 states and 264 button observations. All 550 catalog files
match the clean publish output and archive; its fictional Monitoring and
Editorial demos pass 66 checks. Native ARM64 execution and complete component
acceptance remain open. Current delivery evidence is in
`artifacts/current/table-navigation-foreground-checkpoint-v1.json`.

The basic Data Table's former dark-channel visibility cutoff assumed the old
fully opaque arrow and rejected a correctly dimmed endpoint. Its replacement
uses the pinned button source and footer foreground colors recorded in
`tests/Reference/DataTableNavigationPaint.json`. It retains the same 13-by-13
sample positions and minimum eight ink pixels, and additionally checks that
glyph coverage stays between the sampled background and the source foreground
at its enabled or disabled opacity. Actual pre-repair opaque arrows and blank
glyphs are rejected in both themes. This is a foreground/visibility check;
full-image parity keeps all original pixels and remains a separate requirement.
The original failed audit log and its captures remain retained.
