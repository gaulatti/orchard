# Data Table Pagination contract and acceptance

The frozen Bleecker producer is `src/components/data-table-pagination.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`385a8ae19a8b052be43ae29e7c43b3662ac35ccf892f20c4ef1fc0b893d10897`.
It consumes the original TanStack table, optional page-size options (default
10, 20, 30, 50, 100) and a consumer class name.

Acceptance requires these observable paths, in both themes:

- The summary appears only for a positive filtered selected-row count and
  displays that count against the filtered row count, independently of paging.
- Page text displays the producer's index plus one and its actual page count.
  Empty data therefore displays `Page 1 of 0`; an unknown manual count remains
  `-1`. The view must not derive a different count or navigation policy.
- First invokes index 0; previous and next call their original table actions;
  last invokes the producer's page count minus one. Enablement comes from the
  producer's previous/next flags. No data or table replacement is implicit.
- Choosing a size forwards the selected integer to the original table. The
  consumer owns clamping and page-index adjustment. Current sizes absent from
  the options and empty/custom options remain visible states, with no invented
  first-option fallback.
- Selection survives paging, filtering and sorting through the consumer's
  original row objects. External page/selection/options updates and table
  replacement reach the view; stale table notifications cannot change it.
- The source switches between column/row layout at viewport width 640, and
  hides first/last below that breakpoint. A constrained host does not itself
  change the viewport breakpoint. Long numbers, RTL and overflow stay explicit.
- Native keyboard/pointer selection, focus, accessible names/roles/values,
  disabled controls, live theme changes, rendered popup states and independent
  package use must be exercised. Token assertions alone do not accept paint.

`IBleeckerDataTablePaginationSource` is the consumer-owned native boundary. It
exposes the producer's state/counts/flags and the four original actions; it
does not give the view ownership of rows, filtering, sorting or selection.

The reference consumer uses the frozen real TanStack 8.21.3 row models, React
19.2.4 and Radix Select 2.3.7 from the original lock file. It leaves the table's
methods intact and captures actual pagination callbacks, original table/data/
column/row identity and a rendered row projection. It includes empty/one-page,
filtered selection, custom/empty/unlisted/nonpositive size options, manual
known/unknown counts, responsive/RTL and original-table replacement cases.

The public wiki is unavailable, so documentation stays in the repository.
This native component has no backend or Prometheus surface. Implementation and
full Windows platform/visual acceptance remain in progress.

## Initial preview checkpoint

The corrected source collection is at
`artifacts/data-table-pagination-browser/capture-v2`: 52 cases and 132 states,
with original table/data/column/row identity and actual trusted input receipts.
Keyboard selection waits for the original Radix list's focused option before
sending the next key, within the original two-second readiness bound. Earlier
failed collection/prototype output remains separate. `DataTablePagination.json`
pins its scripts, dependency integrities, bundled fonts and source/CSS hashes.

`DataTablePaginationReferenceTests` verifies provenance, responsive source
rules and every consumer state against the native fictional paging model.
All 84 regressions pass. The current x64 and ARM64 solutions build without
warnings or errors; ARM64 runtime has not been exercised. The registered native
fixture passes both themes at
`artifacts/smoke-data-table-pagination-path-fixed`, including original row
identity, next/last boundaries, page-size/top-row adjustment, filtered
selection retention, sorting and the group automation role.

The native source replay at `artifacts/smoke-pagination-source-rounded`
checks all 132 states against the original producer: page/count/flags,
selection, visible original rows, labels, responsive button visibility and
table replacement. Keyboard-size phases in this replay select native items
programmatically; they do not establish physical keyboard parity.
`compare-data-table-pagination-pixels.mjs` verifies every source image hash,
the native reference hash and PNG chunk CRCs, and compares actual pixels.
All 132 image pairs differ: 510,418 differing pixels, no missing pixels,
154 geometry differences, zero masks and zero tolerance. An independent
Pillow decoder agrees with every pair. Intrinsic-width rounding removed an
unwanted native page-label wrap without changing the comparison bounds.

`artifacts/smoke-pagination-physical-root-popup` passes both themes with
12 actual owned pointer moves/clicks, each gated by at least two stable
presentation transitions, and two exact Escape key pairs. Escape travels
from the focused popup item back to the page-size ComboBox and closes the
popup. The first physical option-click attempt failed the owned-window gate
because the native ComboBox opened a separate popup HWND. The private
page-size chooser now constrains its native popup to its XAML root; the
original ownership checks and two-second deadline remain intact.

`artifacts/smoke-pagination-lifecycle-settled` passes both themes for live
options, table/options replacement, stale-source isolation, per-instance
defaults, disabled mutation suppression, template replacement and detached
handlers, popup closure on unload, reparenting, final observer removal and
native group/ComboBox roles plus action names. Final removal waits for the
actual Unloaded cleanup within the existing two-second readiness bound.

This is an initial preview, not accepted pagination parity. The page-size
chooser currently uses the existing native ComboBox foundation. Complete
source-derived selector/popup appearance, keyboard selection/tab traversal,
all constrained/RTL visual layouts, enabled-state input matrix,
independent consumer, complete current-source runtime, ARM64 runtime,
contrast/Narrator/scaling and collection performance remain unverified. The
new source consumer model regressions do not stand in for those UI checks.
The new library/catalog changes have not yet been pushed or packaged; the
last published held-input artifacts remain the current verified deliverable.
Push-triggered Actions remain paused; no workflow dispatch was requested.
