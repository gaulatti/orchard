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
  object ownership, accessible names/roles/selection, display/text scaling,
  motion and collection performance. Narrator and OS contrast campaigns are
  excluded by the [current library scope](library-scope.md).

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
is false. The desktop replay does not establish component acceptance. The
separate live-update checkpoint below covers keyed option replacement,
reordering and focused disabled/value changes. Full collection/content,
visual/motion/override/RTL matrices, text and display scaling, performance
and native ARM64 runtime remain required. Existing automation metadata and
theme resources remain in scope; the [current library scope](library-scope.md)
excludes Narrator and OS contrast campaigns.
The clean release checkpoint below verifies the isolated consumer and the
integrated full run while retaining those acceptance gaps.

## Verified artifact checkpoint

Product source `877625ea4c975744ae8e5ac94ca2bec99c57396f` builds x64 and ARM64
in the fresh managed `filters-validation` worktree without warnings/errors.
All **94** regressions and generated-token checks pass. The x64 published
catalog's **532 files** match the clean build byte for byte, and its library
matches the NuGet library byte for byte. Product version is
`0.1.0+877625ea4c975744ae8e5ac94ca2bec99c57396f`.

The actual published `artifacts/catalog-filters-validated` passes the focused
desktop replay at `artifacts/smoke-filters-validated-native`. Its independent
`filters-validated-native-audit.json` verifies 276 states, 278 captures, 80
key pairs, 118 clicks, 120 presentation gates and two genuine modal outside
clicks. All **114** compared popup regions differ, totaling **172,947 pixels**
with zero tolerance and no masks. The four literal focus differences and raw
event-route differences remain explicit. Inspection also shows missing native
popup shadow and selected-text/glyph paint differences; these remain work.

The full published sequence at `artifacts/smoke-filters-validated-full`
passes **1,034 captures in 357.0138124 seconds** under the unchanged 360-second
wrapper. It preserves all **1,032** preceding names and all **174** protected
Sunburst/Radial glyph hashes. Full audits verify 1,732 input presentation gates,
1,738 keys, 62 clicks, 28 tooltip gates, 18 final presentation gates, 118 Toolbar
source cases and both-theme Filters/Columns/Pagination lifecycles with no
failed gate or remaining observer. Evidence:
`filters-validated-full-{audit,input-audit,timing}.json`.

The isolated x64 consumer at `artifacts/package-consumer-filters-validated`
passes both themes using only the restored NuGet public API. It verifies the
original column and rows, preserves unrelated filters, makes three intended
requests, produces no external/unchanged-selection request, and passes four
physical key pairs per theme. Live labels, replacement/stale providers,
disabled option/control rejection and unload/remount pass; final observer
counts are zero. The separate ARM64 consumer builds only. Both generated
temporary consumer directories are removed.

Artifacts and SHA-256 values:

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/packages-filters-validated/Gaulatti.Orchard.0.1.0.nupkg` | `925553f870147c573a4247bb735fc7dbfa5efabfd73759d24881195d005a4860` |
| Published/package `Orchard.dll` | `fce34659c392b1ee71fd02e4b7c5159e714496e320eb7cda935905f04957a196` |
| `artifacts/Orchard.Catalog.0.1.0-877625e-x64.zip` | `e1e078fd476c06738183cf613b9b7a2c312201c3559638c6de24c4affa3551cc` |
| `artifacts/Orchard.Catalog.0.1.0-877625e-ARM64.zip` | `362adbbb35687e5f74bcb23c979796944b0d1b0e3c9ecb83b48c44ef37b9384d` |

ARM64 runtime remains unverified. The fixed inventory is **53 in progress,
59 pending, zero accepted**. The public wiki remains unavailable; these
in-repository documents carry the contract. Push-triggered Actions remain
paused at the user's request; local verification does not claim new CI or
deployment evidence.

## Live option updates

The additional reference `tests/Reference/DataTableFiltersLive.json` preserves
the original 276-state fixture unchanged. It freezes **32 cases / 140 states**
from the same actual producer, CSS, four source files, 48 bundled dependencies,
browser and fonts. It includes **32 updates**, **76 open states**, **44 key
pairs** and **272 trusted events**. Reference SHA-256:
`9d63fbc158c57cd970dbe3d7c72a1fc654580ae9db7fc6c89f72b8f40c530ef3`.

Source behavior established by real browser rendering/input:

- Same-value new option objects retain the existing keyed node, focus and
  current selection. Labels, order, disabled metadata and placeholder update
  without a table write. Duplicate empty keys and sentinel collisions retain
  the measured focused occurrence.
- Removing, disabling or changing the key of the focused option moves focus
  to the listbox. Escape and the next normal navigation/commit remain usable.
- A long list initially selected at Status 17 opens at scroll offset 400.
  Reordering preserves that offset and the focused node, even when the node's
  new allocation lies outside the current viewport. The next genuine native
  navigation/pointer action resumes normal scrolling behavior.

The pre-fix native run at `artifacts/smoke-filters-live-baseline` fails these
boundaries: same-value replacement loses focus, subsequent Enter cannot
commit, and an initial pointer-opened long selection remains at offset zero.
Those failures are retained with their original two-second limits.

The control now keys native rows by raw option Value and duplicate occurrence,
retags retained rows with the current original consumer option, and remembers
the last applied key for observable Value changes. Existing providers for a
retained key expose current metadata; removing or changing the key invalidates
its old provider. The earlier same-value replacement rejection assumption was
too strict: the rendered source proves that the keyed node survives. Native
focus restoration preserves that node and the existing viewport; pending
restorations are invalidated by later updates, native input, dismissal or
unload. Initial pointer-open positioning now runs within the initial-focus
transaction, so its selected row scrolls correctly.

`artifacts/smoke-filters-live-keyed` and its independent
`artifacts/filters-live-keyed-audit.json` pass all 140 states, 44 key pairs,
32 mutations, 32 clicks and 142 captures. All 32 update focus identities and
all 108 post-initial settled focus roles match the source. No table callback
occurs during updates. The 32 initially unfocused states are excluded from
focus-role parity, as in the original native fixture. Source raw event-route
and exact pixel parity remain unverified/false; component acceptance is false.

Lifecycle checks additionally exercise the original option's live Label,
Value and IsDisabled notifications while open. Stable keys keep native
identity, changed keys reject old providers, focused disabling moves to the
listbox, and template/table/unload boundaries retain zero final observers.
The isolated package consumer includes open replacement, reordering, live
metadata, retained-provider metadata and removed-key rejection checks.

Reproduce the added source and native audits with:

```powershell
node scripts/reference/capture-data-table-filters-live.cjs `
  --output <source-output> --esbuild-module <esbuild> `
  --playwright-module <playwright>
node scripts/verify-data-table-filters-reference.mjs --live `
  --source <source-output> --output <source-audit.json>
./scripts/smoke-catalog.ps1 -Executable <catalog.exe> `
  -Component data-table-filters-live -ClientSize 1000,745 `
  -ExpectedArchitecture X64 -Output <native-output>
node scripts/verify-data-table-filters-live-native.mjs `
  --source <source-output> --native <native-output> --output <audit.json>
```

## Validated live-update artifacts

Clean product source `65b1dcbd32206282e3e1e0ec3f3af09579f618ea` builds x64 and
ARM64 without warnings/errors, passes **97 regressions** and the token check,
and produces the current validated artifacts. The 532 published x64 files
match the clean build; the package and published library match byte for byte.
Product version: `0.1.0+65b1dcbd32206282e3e1e0ec3f3af09579f618ea`.

All seven focused modes pass on the actual published catalog:

- Original Filters: 276 states, 80 key pairs, 118 clicks and two modal outside
  clicks. Its four browser-body/native-next-control focus differences remain
  explicitly recorded by the existing desktop contract.
- Live Filters: 140 states, 44 key pairs, 32 updates, 32 clicks and 142 captures.
  All update focus identities and all post-initial settled roles match; no
  refresh writes occur. Both themes pass live original-object notifications
  and source/template/unload lifecycle checks.
- Pagination: 132 producer states; 136 held-key states and 136 rapid-pair states
  with 120 pairs each; 36 scroll states with 20 pairs and 12 pointer boundaries;
  12 physical clicks and two exact Escape focus transfers. Its 154 recorded
  layout differences and raw release-owner differences remain open.

The focused outputs are listed in
`artifacts/filters-live-release-focused-results.json`. Independent source,
mutation, held, paired, scroll, pointer-input and Pagination-source audits use
the `artifacts/filters-live-release-*-audit.json` prefix. The live image audit
compares all 76 open regions with zero tolerance and no masks: all differ,
totaling **137,845 pixels**. Focus success does not establish visual acceptance.

The full `artifacts/smoke-filters-live-validated-full` run passes **1,034
captures in 358.3080684 seconds** under the unchanged 360-second limit. It
preserves all 1,032 earlier capture names and 174 protected glyph hashes.
The strengthened `--filters-live` audit verifies the new keyed/live lifecycle
gates in both themes. All 1,732 input presentation gates, 1,738 keys, 62 clicks,
28 tooltip gates, 18 final presentation gates and 118 Toolbar source cases
pass. Evidence: `artifacts/filters-live-validated-full-{audit,input-audit,timing}.json`.

`artifacts/package-consumer-filters-live-validated` passes x64 runtime in both
themes through the restored package alone. It verifies six physical key pairs
per theme, three intended requests, no external/refresh/unchanged-selection
write, retained native identities, current metadata through retained providers,
removed-key rejection and zero observers after unload. The ARM64 consumer
builds only. Both temporary package caches are removed.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/packages-filters-live-validated/Gaulatti.Orchard.0.1.0.nupkg` | `03ebd600b2b26aec9fe3016bce3fba73ad62c5310161996862436247202b3127` |
| Published/package `Orchard.dll` | `362378b946c07cfa78f31d7da5f28ee0ee5995e885c1e23caa084d6ca0ca437c` |
| `artifacts/Orchard.Catalog.0.1.0-65b1dcb-x64.zip` | `87e6d4e8ab2245557a2ad17841bef1945a93598359f710d79b29c83f828eb3c6` |
| `artifacts/Orchard.Catalog.0.1.0-65b1dcb-ARM64.zip` | `53c1ac16675ae6f9ab74e6992e3d4d6b8aa6bb8664b80ca379d3d8b6513f2e1e` |

Both archives' files are verified against their published directories in
`artifacts/filters-live-catalog-archives.json`. ARM64 runtime, full visual/motion,
content/override/RTL, accessibility/scaling and performance acceptance remain
open. The inventory stays **53 in progress, 59 pending, zero accepted**.
Push-triggered Actions remain paused and the public wiki remains unavailable.

## Popup shadow boundary

The shared Select now uses `BleeckerOverlaySurface` for its original rounded
background, border and content. The [overlay contract](overlay-shadow.md)
describes its CSS Gaussian mapping, native resource cleanup and separate
shadow-only outside-click reference. The original browser JSON and all
276-state input/geometry/ownership gates remain unchanged. Full-client RGBA
comparisons retain remaining source paint differences without masks or
tolerance; complete component acceptance remains open.

Worker-thread producer notifications are marshaled before any XAML state read.
See [the producer notification contract](producer-notifications.md) for the
shared catalog/package regression and its background-only verification limits.
