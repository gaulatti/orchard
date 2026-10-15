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

## Native acceptance still required

- Extend the normal-state and representative input coverage to all 376 source
  interaction states, complete content/override/error/RTL and enabled/motion
  matrices, reverse/edge focus traversal and every original action/filter callback.
- Verify final matching-source package/catalog artifacts and native ARM64
  runtime. The provisional independent x64 consumer passes in both themes,
  including physical typing, original row/column identity and initial/global
  reset semantics, and removes its own GUID cache. Both solution architectures
  compile; those results do not establish native ARM64 runtime acceptance.
- Verify exact rendered parity, full content/override/error matrices, Windows
  contrast, Narrator, display/text scaling and measured collection performance.
  Include monitoring/editorial compositions and final matching-source artifacts.

The public wiki is unavailable, so documentation remains in the repository.
This native presentation work has no backend or Prometheus surface. Push
Actions remain paused by user request; no CI dispatch is authorized here.
