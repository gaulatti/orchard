# Data Table Filters source contract

The fixed source is Bleecker `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/data-table-filters.tsx`, SHA-256
`83a7e5de0d5b0a63d42591e3d45b230243d0ee97d9797a2f9b6b87d4369334bb`.
It implements `DataTableSelectFilter` and exports the type-only
`DataTableFacetedFilterProps`; the frozen file has no faceted-filter renderer.
Do not invent one under this contract.

User-observable acceptance requires the following complete path:

- Resolve `table.getColumn(columnId)` on the original consumer. A missing
  column renders no wrapper/control and emits no request. A restored column
  uses its current original value, rather than a cached replacement.
- Read `column.getFilterValue() ?? ''`. Prepend the placeholder option with
  value `''` to the original ordered options, including their disabled flags.
  `placeholder` defaults to `All`; an explicitly empty string stays empty.
- Forward a changed string to that original column's `setFilterValue`; the
  empty option forwards `undefined`. Keep unrelated column filters, original
  rows, table identity and consumer filtering policy intact. Selecting the
  already selected option must not fabricate a change callback.
- Controlled external updates, column-ID changes, table replacement and old
  notifications must remain observable through normal consumer rows.
- Use the actual Select contract: a 40px default trigger, 14px text, 17px
  chevron, modal listbox, disabled options, real keyboard/typeahead/focus,
  source popup alignment/scrolling and both themes. Its modal outside-click
  behavior differs from the nonmodal Column Toggle.
- The wrapper fills available width below the 640px viewport breakpoint and
  is 176px wide at/above it, even in a narrower local host. RTL, top flips,
  long option lists, missing columns, empty/custom placeholders and unknown
  values require real layout/input evidence.
- Native acceptance additionally requires disabled and stale input rejection,
  live options and metadata, template replacement, unload/remount, original
  object ownership, accessible names/roles/selection, contrast/Narrator,
  display/text scaling, motion and collection performance.

The Select implementation converts an empty option to its internal literal
`__bleecker_empty_option__`. Consumer duplicates and literal collisions are
captured as actual source behavior, rather than silently repaired. Filter
values are not coerced to input strings: numeric, false, array and null
specimens must be checked against the actual Select/consumer runtime.

`scripts/reference/capture-data-table-filters.cjs` bundles the actual frozen
React 19.2.4, TanStack 8.21.3 and Radix Select 2.3.7 producers. It verifies the
original lock Git blob `0b6113dd13d74b1023208cbf96e080915f3b47a2`, every
bundled dependency version/integrity, existing compiled CSS, fonts and pinned
browser/tools. The consumer uses real `useReactTable`, original fictional
rows/columns, real filter callbacks and a rendered filtered-row projection.
Every capture requires three stable animation-frame transitions within two
seconds and records trusted native browser input plus the real accessibility
tree. No control or table API is mocked.

The first capture retained an outside-target collision; the second completed
the runtime but failed serialization because a document-element event trace
included the bundled script text. Those attempts remain separate. The
collector now records document target roles without copying unrelated script
content, and its presentation gate compares the actual focused element by
identity. No deadline, pointer activation, input trust or visual tolerance is
relaxed.

Implementation must reuse the existing original-column filtering boundary
(`IBleeckerDataTableFilterSource` / `IBleeckerDataTableFilterColumn`) and share
the Select behavior currently used by Pagination, preserving its existing
held/paired input and source-scroll regressions. A separately styled stock
ComboBox would not establish this source contract.

The native Filters prototype is being verified and is not accepted. The existing
Column Toggle package/catalog checkpoint remains authoritative. This work has
no backend or Prometheus surface. The public wiki remains unavailable
(`remote-not-found-or-inaccessible`), so documentation stays in the repository.
Push-triggered Actions remain paused; no workflow is dispatched.

## Verified source checkpoint

The committed reference contains **56 both-theme cases and 276 states** at
`tests/Reference/DataTableFilters.json`, SHA-256
`f85940d22b27c187d9c14ae7b2a6ce6fc788fb9e3a52971cf3aff6d2575790d6`.
Raw PNGs are in `artifacts/data-table-filters-browser/capture-v4`.
The independent source audit verifies all 276 image hashes/dimensions,
114 open listboxes, 706 trusted input events, 80 complete key pairs, source
dependency hashes and 48 bundled runtime packages. Its report is
`artifacts/data-table-filters-source-v4-audit.json`. Visual review of v3 caught
an overlapping outside fixture button and a collapsed body margin in dark
mode; v4 fixes those fixture-only layout issues and retains the same 276
producer/consumer states and input boundaries.

`DataTableFiltersReferenceTests` verifies the source pins and replays its
actual change events through the independent C# `SelectFilterTable` consumer.
All 93 regressions pass. The replay checks original columns/row objects,
callback counts, clearing versus external updates, unrelated filters, both
tables and the original visible-row projection at every one of the 276
states. Both solution architectures build without warnings/errors and the
token check passes. This verifies the source and consumer model; it is not
native Filters control evidence. No library behavior changed, so the existing
1,032-capture published Column Toggle runtime/package checkpoint remains the
authoritative native artifact evidence.

Measured details that must survive native implementation:

- Closed/default text is `All`; a successful Draft commit invokes one original
  callback, clearing invokes a second, and selecting All again emits none.
- An outside physical click dismisses Select and restores trigger focus while
  emitting **zero** outside-button callbacks. The open Select hides outside
  controls from the browser accessibility tree.
- Tab and Shift/Tab stay on the currently focused option in the modal list.
  The source release routes are retained in the audit for future comparison.
- Width is 300px in the 639px/300px specimen and 176px at the 640px breakpoint.
  A 120px local host at a wide viewport still has a 176px filter.
- False and numeric zero render placeholder text without a selected option.
  Truthy numeric/array values render empty text. Null selects the empty/All
  option, but a literal initial null filter still filters out all fixture rows;
  displaying All must not silently clear that original state.
- Duplicate empty options render `AllExplicit empty`; a literal sentinel
  collision renders `AllLiteral sentinel`. Both matching options have checked
  paint, and initial focus goes to the later matching item.

Reproduce the source audit with:

```powershell
node scripts/verify-data-table-filters-reference.mjs `
  --source artifacts/data-table-filters-browser/capture-v4 `
  --output artifacts/data-table-filters-source-v4-audit.json
```

The native checkpoint moves Filters to in progress: **53 in progress, 59
pending, zero accepted**. The earlier source-only checkpoint left it pending;
passing the desktop replay does not establish full acceptance.

## Native implementation in progress

`BleeckerDataTableSelectFilter` consumes the existing original-column filter
interfaces. `Table`, required `ColumnId`, required `Options` and `Placeholder`
map the source boundary. `BleeckerSelectOption` retains original consumer
identity and observable `Value`, `Label` and `IsDisabled` metadata. Empty
selection clears only the original column through a null/undefined request;
refresh never invents a table write. The strict value projection is covered
against all 276 actual producer states, including duplicate/sentinel paint.

The Pagination-owned chooser and scroll implementation have moved into
`BleeckerSelectSurface` and `BleeckerSelectSurface.Scroll`. It receives explicit
consumer read/identity callbacks and has no dependency on Pagination or
Filters. Existing palette/resource keys and Pagination automation names are
retained. The extraction alone builds without warnings and passes the
existing 136-state/120-pair native rapid replay and its independent audit;
the reported source/native release-owner differences remain unaccepted.

`DataTableFiltersFixture` registers both public types and replays the real
source cases with native pointer/key input, original rows, option ownership,
accessible values, scrolling and measured geometry. The failed v1–v9 runs
are retained. The shared Select trigger now captures the genuine press so
opening and reopening at the unchanged pointer location receives its release.

The strict `data-table-filters` mode retains four literal focus failures:
removing a focused column moves Windows focus to the next existing control;
restoring it retains that focus. The browser focuses body in both states.
Native Windows semantics are permitted by the fixed parity contract. The
separate `data-table-filters-native` mode verifies this desktop behavior,
including the distinction from an initially unfocused missing column. It
still records the original browser role and `sourceFocusParityVerified=false`;
it never renames the outside button to body or claims strict source parity.

`artifacts/smoke-data-table-filters-desktop-v2` passes all **276 states**, **80
key pairs** (including two Shift/Tab pairs), **118 clicks**, **120 presentation
gates** and two genuine modal outside clicks. Twelve unchanged-position moves
reuse an earlier exact receipt in the same native input session, as required
by the original stationary-pointer contract. The modal input observer uses
`InputPointerSource.GetForIsland` on the exact existing XAML content island:
one genuine move/down/up sequence, exact publisher/window/position/pointer,
increasing frame IDs, ordered timestamps and normal button/contact states.
It does not modify `Handled`. The real dismissal restores trigger focus and
emits no outside-button callback. Reports explicitly claim no routed-XAML
receipt or raw DOM-route equivalence. Earlier failed observers remain failed.

Both themes also pass original-column/row mutation, external state, live
labels, placeholder, option replacement, stale-provider rejection, disabled
option/control rejection, template detachment/replacement, table replacement
and unload/remount checks. Final source-observer counts are zero.

Reproduce the native audit after the focused catalog mode with:

```powershell
./scripts/smoke-catalog.ps1 -Executable <catalog.exe> `
  -Component data-table-filters-native -ClientSize 1000,745 `
  -ExpectedArchitecture X64 -Output <native-output>
node scripts/verify-data-table-filters-native.mjs `
  --source artifacts/data-table-filters-browser/capture-v4 `
  --native <native-output> --output <audit.json>
```

The independent audit hashes all 276 original/native images and compares all
114 open-popup regions with zero tolerance and no masks. Exact pixel parity
is false. The desktop replay does not establish component acceptance. Open
option-collection replacement/reordering, complete live disabled/value
mutation, full visual/motion/override/RTL matrices, Narrator, contrast, text
and display scaling, performance and native ARM64 runtime remain required.
The isolated consumer and integrated full-run additions require clean
release validation before they can be reported as verified artifacts.
