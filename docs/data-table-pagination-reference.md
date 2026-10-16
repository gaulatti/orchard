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
all constrained/RTL visual layouts, enabled-state input matrix, ARM64 runtime,
contrast/Narrator/scaling and collection performance remain unverified. The
new source consumer model regressions do not stand in for those UI checks.
Push-triggered Actions remain paused; no workflow dispatch was requested.

## Packaged preview checkpoint

The current artifact source is
`f750f0d28c2f6c0dbe24d3be7f3693caf1e1a61c` (preview implementation
`132576c100dacb9aad44bd984f96b257df1d6db4`, followed by package documentation).
`artifacts/packages-pagination/Gaulatti.Orchard.0.1.0.nupkg` has SHA-256
`3ed0bd4a178ef4dfc74a30bd7512a7ea17f4c0287bfec817de8a2fea95f8a8a5`.
The runnable catalog is at `artifacts/catalog-pagination`.

The full published x64 suite at `artifacts/smoke-pagination-published-full`
passes in 353.667325 seconds, within the unchanged 360-second wrapper.
It preserves all 1,028 previous PNG names and adds the two pagination renders.
All 174 protected Sunburst/Radial Bar glyph hashes are unchanged. The audit
also preserves 118 toolbar source cases, 28 tooltip gates and 16 final
presentation gates. The native input audit passes 1,732 owned pointer gates,
1,738 same-target key pairs and 62 clicks, with at least two stable frames
for each presentation gate. These counts describe the complete catalog suite;
the focused pagination pointer/Escape evidence is described above.

The independent consumer at `artifacts/package-consumer-pagination` restores
only the fresh package through an isolated feed/cache, then passes actual x64
runtime checks in both themes, including original rows, navigation boundaries,
size/top-row adjustment, selection summary and accessible roles/names. The
separate ARM64 consumer builds at `artifacts/package-consumer-pagination-arm64`;
its report explicitly records runtime unverified. Both generated caches are
removed. Package, catalog and both consumer Orchard assemblies have the same
SHA-256 `c5d8feda0bf2969d655333e406b07ad39f20a3e2dbb4026c168063096604c27b`
and product version `0.1.0+f750f0d28c2f6c0dbe24d3be7f3693caf1e1a61c`.
`artifacts/pagination-artifacts-report.json` records the correspondence.

The published catalog also passes all 132 source-state checks at
`artifacts/smoke-pagination-published-source`. Its exact pixel comparison is
recorded separately in `artifacts/pagination-published-pixels.json`.
All 132 pairs still differ: 513,050 differing pixels and the same 154 geometry
differences, with no missing pixels, masks or tolerance. The programmatic
source replay does not reproduce physical pointer positions or keyboard/focus
states, and the surface crops do not establish popup appearance parity.
The separate physical check at `artifacts/smoke-pagination-published-input`
passes the same 12 owned clicks and two exact popup-to-selector Escape pairs.

This checkpoint establishes packaged preview behavior, not completed parity.
No inventory entry is accepted by this evidence. The remaining visual,
keyboard, accessibility, scaling, motion, performance and ARM64 runtime
criteria remain open.
