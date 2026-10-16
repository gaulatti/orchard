# Data Table Column Toggle contract

The frozen source is `src/components/data-table-column-toggle.tsx` at Bleecker
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`9082ca90691d40083e9bb4b94f687daa56a8e3a064e356ab5dd9afa67ed89df7`.
Its original package lock is Git blob `0b6113dd13d74b1023208cbf96e080915f3b47a2`,
verified against the pinned repository tree. The browser collector executes
the actual React 19.2.4, TanStack 8.21.3 and Radix Popover 1.1.23 producers.
Every bundled dependency is checked against the frozen version/integrity,
including Floating UI; a first installation's newer Floating UI packages were
rejected and replaced by the frozen versions. The existing compiled CSS and
bundled font hashes are retained.

Acceptance covers these observable boundaries:

- Read the original top-level `getAllColumns()` sequence. Offer a column only
  when its accessor exists and its original hiding permission is true. Do not
  flatten groups, offer display-only columns, or replace original columns.
- Read the original visible state and forward exactly one requested boolean
  through that column's original visibility action. The consumer retains its
  table, rows, visible-cell projection and state policy.
- Display a string header literally, including an empty string. A non-string
  header displays the original column ID. The trigger defaults to `Columns`
  and respects a custom or empty label.
- Preserve controlled external visibility/permission updates, table
  replacement, stale-source isolation, template replacement and unload.
  Refreshing checkbox values must not emit consumer visibility requests.
- Use a nonmodal popover, not menu selection. Toggling a checkbox keeps it open.
  An outside click closes it and activates the original outside control once.
  Escape closes and restores trigger focus. Initial focus goes to the first
  checkbox, or to the empty dialog; Tab/Shift+Tab loop inside its focus scope.
- Match the source sizing, alignment, scrolling, both themes, source shadow and
  the actual native pointer/keyboard/accessibility boundaries. Native disabled,
  contrast, text/display scaling, motion and independent package use require
  their own runtime evidence.

The source's `w-52` is constrained by Popover's `min-w-56`: its actual width is
224px, not 208px. It has a 1px border, 6px padding, 31px heading allocation,
36px rows with 20px checkboxes and a 240px maximum viewport. Empty, one-row,
four-row and long popovers measure 45, 81, 189 and 285px high. The normal gap is
4px with logical-end alignment and 12px collision padding. The collector also
exercises RTL, a top flip and a constrained-width collision.

Two source focus behaviors remain explicit: removing all eligible columns
while open leaves body focus, which remains after eligibility is restored;
reverse Tab in the long list focuses its last checkbox without scrolling the
viewport from zero. These are observed states, not invented first-item or
scrolling fallbacks. Raw focus information is retained when comparing native
container focus with browser body focus.

`scripts/reference/capture-data-table-column-toggle.cjs` and its actual
consumer in `data-table-column-toggle-client.tsx` collect 28 both-theme cases
and 132 states. The committed reference is
`tests/Reference/DataTableColumnToggle.json`, with composed captures at
`artifacts/data-table-column-toggle-browser/capture-v3`. It records original
table/column/row/cell identities, callbacks, physical input trust, geometry,
styles and the real browser accessibility tree. Earlier failed dependency
collection and the initial 110-state run remain separate.

`BleeckerDataTableColumnToggle` consumes
`IBleeckerDataTableColumnVisibilitySource` and the original
`IBleeckerDataTableVisibilityColumn` objects. `GetAllColumns`, `HasAccessor`,
`CanHide`, `IsVisible`, `Header` and `ToggleVisibility` map the original
producer boundary; the control stores no row or visibility state. A consumer
raises `Changed` for external visibility, eligibility or metadata updates.
Native label/checkbox requests retain original object identity and reject
stale or disabled paths. The registered catalog fixture is
`DataTableColumnTogglesFixture.xaml`; its independent fictional model is also
used to check the frozen consumer boundary in regression tests.

The native implementation uses the existing overlay shadow/palette and
Bleecker Checkbox, a nonmodal Popup, a native expandable trigger and a dialog
pane. Popup/scroll/focus APIs were checked in the exact Microsoft WinUI 2.3.9
reference documentation shipped with pinned Windows App SDK 2.5.1. The x64
build passes without warnings. Native runs v8 through v10 complete all 132 producer
states in both themes, retaining actual input owners, original callbacks,
visible cells, accessible names, 224px popup width, source heights, 4px gap,
logical-end alignment, checkbox dimensions and scroll offsets. Run v9 also
checks native disabled rejection, table replacement, stale notification
isolation, template replacement, unload and remount. It records every Shift
down/up and main-key down/up in the actual native route.

Four eligibility states explicitly retain a focus difference: the source
body is focused, while Windows retains the existing dialog. Functional
container handling passes; `sourceFocusParityVerified` remains false. Exact
release-owner parity has not been established either. Earlier readiness,
popup-window ownership, fractional alignment and close-focus failures remain
in their original run directories. Run v9's independent image audit exposed a
harness resize ordering error in constrained cases. Run v10 resizes after
foreground acquisition; all 132 raw PNG dimensions now match the source,
including narrow/collision specimens. Its independent audit passes 44 key
pairs, four complete Shift/Tab modifier sequences and 48 pointer
presentation/move/click boundaries. Exact popup pixel comparisons still
differ across all 74 open-popup comparisons (292,724 differing pixels).
Full raster, theme/override/motion, Narrator,
contrast, scaling, performance, current package/full-catalog and ARM64 runtime
acceptance remain open.

Reproduce the focused native replay with:

```powershell
./scripts/smoke-catalog.ps1 -Component data-table-column-toggle `
  -ClientSize 1000,745 -ExpectedArchitecture X64 -Output artifacts/column-toggle
node scripts/verify-data-table-column-toggle.mjs `
  --source artifacts/data-table-column-toggle-browser/capture-v3 `
  --native artifacts/column-toggle --output artifacts/column-toggle-audit.json
```

The independent audit verifies raw PNG hashes/dimensions, source pins, keyboard
modifier order, real pointer gates and exact popup pixels without masks,
resampling or nonzero pixel tolerance. A functional pass never accepts visual
or platform parity.

The public wiki is unavailable, so this contract stays in the repository.
There is no backend or Prometheus surface. Push-triggered Actions remain paused.
