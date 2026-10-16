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
source-derived selector/popup appearance, complete keyboard input timing,
all constrained/RTL visual layouts, enabled-state input matrix, ARM64 runtime,
contrast/Narrator/scaling and collection performance remain unverified. The
new source consumer model regressions do not stand in for those UI checks.
Push-triggered Actions remain paused; no workflow dispatch was requested.

## Initial packaged preview checkpoint

The initial pagination artifact source is
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

## Keyboard selection and traversal checkpoint

`capture-data-table-pagination-keyboard.cjs` uses the same pinned original
React/TanStack/Radix consumer and validates its baseline, client, frozen source,
dependency integrities, fonts and CSS. The final collection is
`artifacts/data-table-pagination-keyboard-browser/capture-v3`; the earlier
collections remain separate. `DataTablePaginationKeyboard.json` records 16
cases, 136 states and 120 actual trusted source key pairs. Full viewport PNGs
include opened popup states. It covers first/middle/last pages, custom long
options, Home/End, Enter/Space, Escape cancellation, popup Tab containment,
forward/reverse Tab, RTL and the narrow viewport's hidden navigation actions.

The baseline native probe found that closed ArrowDown changed page size
immediately. Pagination's private native ComboBox now opens on ArrowDown,
ArrowUp, Enter and Space without changing the consumer. A second probe found
that Tab closed the list and moved to Next; the private ComboBox/items now
contain Tab while open. Other native selection, pointer, peer and enabled-state
behavior continues through the existing ComboBox. The shared ComboBox style
and other consumers are not modified.

`artifacts/smoke-pagination-keyboard-held` passes all 136 state checks in both
themes. It uses physical keys and the normal original consumer callbacks,
checks original row objects and exactly one size commit, and observes each
explicit down/up owner. There are 118 held pairs plus two Shift+Tab transfers,
16 owned physical Before clicks and 138 native PNGs. Held keys wait for the
explicit focus destination and two actual rendering transitions before
release, within the existing two-second readiness/presentation gates. No
generated key events or forced focus changes stand in for these transitions.
The verifier checks reference and image hashes, PNG CRCs, capture dimensions,
all states, key owners/modifiers and pointer ownership/presentation.

Visual review found that the initial XAML-root bitmap omitted the native popup
island. `artifacts/smoke-pagination-keyboard-desktop` repeats and passes the
same 136 states with 136 guarded composed desktop-client captures plus two
final catalog renders. The current verifier requires this capture method;
it checks the owning foreground process, monitor bounds and occlusion guards.
The actual popup, selection highlight and scroll position are now visible in
the frames. This improves the visual evidence without establishing parity.

An earlier immediate press/release probe reached the correct middle-page
Enter result and final focus, but its keyup did not reach the assumed ComboBox
release target. The held-key suite does not establish zero-delay release
routing; that remains an explicit open check. Native key ownership is
checked against explicit native targets; the source's transient DOM keyup
targets are recorded separately and are not claimed to be identical.
All 87 source/consumer regressions pass. Pixel parity, the complete disabled
input matrix, accessibility/scaling/motion/performance and native ARM64
execution remain open. The backend/metrics contract is unaffected.

## Packaged keyboard checkpoint

The keyboard artifact source is
`cd3e7cf674167c625277578c43027e49e64e0b87`.
The package at
`artifacts/packages-pagination-keyboard/Gaulatti.Orchard.0.1.0.nupkg` has
SHA-256 `2e51de0a9e142163ef407c86aee9413dccc25c9751e0e9b1115b4193a718faa9`.
The runnable catalog is at `artifacts/catalog-pagination-keyboard`.

The current full x64 run at
`artifacts/smoke-pagination-keyboard-published-full` passes in 353.3632808
seconds under the unchanged 360-second wrapper. Its 1,030 PNGs preserve all
previous captures and all 174 protected glyph hashes. All 1,732 owned pointer
gates, 1,738 same-target key pairs and 62 clicks pass, along with 118 toolbar
source cases, 28 tooltip gates and 16 final presentation gates.
The focused published keyboard replay at
`artifacts/smoke-pagination-keyboard-published` separately passes 136 states,
120 exact key pairs (118 held plus two Shift+Tab), 16 owned pointer gates and
138 capture checks. Its audit continues to mark zero-delay keys and pixel
parity unverified.

The clean x64 consumer at `artifacts/package-consumer-pagination-keyboard`
passes both themes, including eight physical held-key pairs through its own
original rows and adapter: opening without mutation, popup Tab containment,
highlighting without a commit and exactly one Enter size commit. The ARM64
consumer at `artifacts/package-consumer-pagination-keyboard-arm64` builds;
runtime remains explicitly unverified. Both generated feed/cache workspaces
are removed. Package, catalog and both consumers have Orchard assembly SHA-256
`e23749f58cb51660c00254cea32adbce7e7c56d112c561baa633f909a27a4d1a`, with product
version `0.1.0+cd3e7cf674167c625277578c43027e49e64e0b87`.
`artifacts/pagination-keyboard-artifacts-report.json` verifies that correspondence
and the independent consumer's eight exact key owners.

The same published catalog passes the previous 132-state producer replay at
`artifacts/smoke-pagination-keyboard-published-source` and the 12 physical
clicks/two exact Escape transfers at
`artifacts/smoke-pagination-keyboard-published-input`. The current surface
comparison still fails all 132 image pairs: 510,420 differing pixels, no
missing pixels and 154 geometry differences, with zero masks and tolerance.
An independent Pillow decoder agrees with every pair. The source replay's
XAML-root crops continue to omit popup islands; the separate keyboard desktop
captures are the evidence for the actual composed popup states.

Both architecture builds are clean and 87 regressions pass. The fixed inventory
remains 51 in progress, 61 pending and zero accepted. Push-triggered Actions
remain paused. This is further preview evidence; it does not close the open
visual, zero-delay input, disabled input, platform or performance criteria.

## Popup placement work

The source Select requests bottom/start placement with a 4px gap, 8px collision
padding and a viewport height bounded by 18rem and available space. The native
default ComboBox instead overlays its selected row on the trigger. Placement
acceptance requires the popup to leave the trigger/value visible, retain its
80px width, preserve source counts/rows and keyboard/pointer selection, and
remain owned and reachable within the XAML root. Current work measures the
pinned WinUI 2.3.9 Popup placement API against the captured source before
accepting the layout. Collision edges, scrolling, full appearance and motion
remain separate unverified criteria.

The local placement prototype passes the existing 136 keyboard states at
`artifacts/smoke-pagination-popup-painted-bounds`. Its composed frames show
the controlled value while open. The measured painted borders have the 4px
gap and 80px width in all 34 open states; 26 heights match. The eight long-list
states have a 288px native viewport but still lack the source's scroll buttons.
Horizontal differences inherited from the bar's typography remain recorded;
the popup comparison does not claim full geometry or pixel parity.

The native split animation is coupled to close completion/focus restoration.
Removing its storyboard broke those boundaries. The prototype preserves the
completion storyboard object, replaces its split effect with a zero-duration
opacity transition, and targets the actual popup border. Native input and
completion checks pass with that approach on this animations-disabled host;
enabled motion remains unverified.

The earlier local lifecycle check at
`artifacts/smoke-pagination-popup-item-identity` failed after removing the
currently selected option while open: the popup remains open, the producer
keeps size 8 and receives zero extra requests, but the display retains `8`
instead of becoming empty. Reusing unchanged native items is implemented;
the failed output is preserved. The private chooser now presents an owned
label from the authoritative table/options instead of reusing WinUI's cached
selection-box text. This clears an unlisted size without changing the producer.

`capture-data-table-pagination-open-options.cjs` pins and reuses the original
React bundle, source keyboard reference, browser, CSS and fonts. Its eight
states in `artifacts/data-table-pagination-open-options-browser/capture-v2`
confirm that changing size while open, removing the selected option and
restoring it preserve the original table/rows and produce no pagination
callbacks. The unlisted value displays empty while the popup stays open.
`DataTablePaginationOpenOptionsTests` pins the evidence and checks the consumer
row boundary. All 88 regressions pass.

`artifacts/smoke-pagination-popup-verified` passes 136 keyboard states, including
the controlled display, with 138 composed/final captures. The popup layout
report still fails full equivalence: all 34 gaps and widths match and 26 heights
match, but typography positions and long-list scroll-button geometry differ.
The pointer/Escape replay at `artifacts/smoke-pagination-popup-final-input`
also passes. The original pixel tolerances and input deadlines are unchanged.

The keyboard harness also exercises two unknown-but-already-positioned native
pointer clicks. Each has one exact, nongenerated press/release pair, an owned
target and two stable presentation transitions, with zero movement events.
It does not manufacture or reuse a movement receipt or move the pointer away
to provoke one. Ordinary movement verification remains unchanged.

The complete local prototype at `artifacts/smoke-pagination-popup-native-height`
also checks that every focused option is inside the painted popup viewport.
Visual review caught an offscreen End destination after capping only the inner
ScrollViewer; the native ComboBox's total drop-down height is now bounded as
well, and its focused item requests native viewport scrolling. End reaches and
visibly displays option 500. All 136 states pass with this additional check.
`artifacts/smoke-pagination-popup-final2-lifecycle` passes the external/unlisted
open-value, observer, template and unload checks in both themes. The updated
pointer/Escape regression is at `artifacts/smoke-pagination-popup-final2-input`.
This native component has no backend or metrics surface.

The first full published popup run at
`artifacts/smoke-pagination-popup-published-full` stopped after 87.8152707
seconds/226 captures on the light narrow Donut's Pie focus-payload gate. It
does not establish full-suite success. A focused Pie run with added
failure-only payload/focus/pointer diagnostics passes at
`artifacts/smoke-pagination-popup-pie-diagnostic`; the cause of the sequence
failure is not established. The original deadline and expected payload remain
unchanged. The broader runtime criterion remains open while this is examined.

The local package preview source is
`bd74a1bd1b6349469866eda198531d94833cf64d`, with the Pie failure diagnostics
committed separately at `c900b32ebb04263cfe61bcf92432bdc58242e9be`.
`artifacts/packages-pagination-popup/Gaulatti.Orchard.0.1.0.nupkg` has SHA-256
`55671d397554d466cfde6d48409f0efff8227a8b54d6cbb990747181d3eb8c1b`.
The runnable preview is at `artifacts/catalog-pagination-popup`.

The clean x64 package consumer passes in both themes, including eight exact
held-key pairs and measured 4px gap/80px width/visible current value checks.
The independent ARM64 consumer builds with runtime explicitly unverified;
both isolated caches are removed. Package, catalog and both consumers contain
Orchard assembly SHA-256
`34cc2a24560c35bfd9f248ae35d09a01e02cf95ceaf001661eb890e9e808467b`, product
version `0.1.0+bd74a1bd1b6349469866eda198531d94833cf64d`. The correspondence and
keyboard receipts are audited in `artifacts/pagination-popup-artifacts-report.json`.

At this preview checkpoint, these artifacts did not replace the last fully
checked catalog: the full-run Pie gate was unresolved and the popup commits
were unpushed. Remote main was `42ca0a92e809c21826c025b573543856daad55df`.
The subsequent full-run evidence is recorded below. Push-triggered Actions
remain paused. All 112 inventory entries retain their existing status
(51 in progress, 61 pending, zero accepted).

## Validated popup checkpoint

Clean source `27acbc47bbcc10d9c6510159edae815eb688807b` passes the complete
Light/Dark native catalog at `artifacts/smoke-pagination-popup-full-diagnostic`
in 355.1883746 seconds under the unchanged 360-second deadline. The added
Pie diagnostics do not change chart behavior or the original assertions.
The earlier narrow Donut focus failure remains unexplained; this later pass
does not establish that its cause was fixed.

The full audit retains every previous capture and both new Pagination captures
(1,030 PNGs), all 174 Sunburst/Radial Bar glyph hashes, 118 Toolbar source
cases, 28 tooltip presentation gates and 16 final presentation gates. The
native input audit verifies 1,732 pointer gates/moves, 1,738 key actions and
62 clicks with no failures and at least two stable Rendering transitions.
Reports are `artifacts/pagination-popup-full-diagnostic-audit.json` and
`artifacts/pagination-popup-full-diagnostic-input-audit.json`. Both architecture
solution builds have zero warnings/errors; all 88 regressions and the generated
token check pass.

The runnable catalog is `artifacts/catalog-pagination-popup-validated`.
All 532 published files exactly match the measured build, audited in
`artifacts/pagination-popup-published-correspondence.json`. Package 0.1.0 at
`artifacts/packages-pagination-popup-validated/Gaulatti.Orchard.0.1.0.nupkg`
has SHA-256 `b823be3508fed228181676c040483dcc8936a3fa3992df6c2b6c13f8ed9bfb5d`.
Its source ProductVersion is `0.1.0+27acbc47bbcc10d9c6510159edae815eb688807b`.
The fresh x64 package consumer at
`artifacts/package-consumer-pagination-popup-validated` passes both themes,
including eight exact held-key actions, the 4px popup gap, 80px width, visible
controlled value and original rows. Native ARM64 runtime remains unverified.
The isolated ARM64 consumer build passes at
`artifacts/package-consumer-pagination-popup-validated-arm64`; both consumers'
temporary caches are removed. Package, catalog and both consumers contain
Orchard assembly SHA-256
`e1bc95454042ea8fa25895427ce73601a047f836b75c547237b1e779496df316`,
audited in `artifacts/pagination-popup-validated-artifacts-report.json`.

This checkpoint does not accept Pagination or any other inventory entry.
The exact browser/native pixel comparison still fails, including the source
scroll controls and long-popup height. Zero-delay keys, the complete disabled
input/motion/lifecycle matrix, contrast/Narrator/scaling, collection performance
and native ARM64 acceptance remain open. The fixed inventory stays at 51 in
progress, 61 pending and zero accepted. This is a native library with no backend
or metrics surface. The public wiki remains unavailable, so the contract and
evidence remain in-repository. Push-triggered Actions stay paused.

## Scroll-control source regression

`scripts/reference/capture-data-table-pagination-scroll.cjs` reuses the pinned
React/TanStack/Radix bundle, browser, CSS and font hashes. Its 18 actual browser
states are committed in `tests/Reference/DataTablePaginationScroll.json`, with
composed captures at `artifacts/data-table-pagination-scroll-browser/capture-v3`.
Both themes cover opening, End/Home, pointer hover in both directions, leaving
the indicator, replacing options while open and Escape. Pointer/scroll receipts
are trusted, the original table and rows remain unchanged and no pagination
callback is emitted. `DataTablePaginationScrollTests` pins the collector and
keyboard reference and checks those consumer-boundary invariants.

The long source list has a 288px viewport, 476px content extent and a 188px
maximum offset. At either endpoint one 28px scroll control is visible and the
popup is 318px tall. A downward hover from the top advances 36px and reveals
the upper control: the popup grows to 346px, moving the lower control away
from the stationary pointer and stopping that hover. A subsequent move to the
lower control reaches the bottom; hovering the upper control returns to zero.
After replacing the options with `[10,20]` at offset zero, Radix retains its
already-mounted lower control until a further scroll event. That observed
popup is 110px tall and keeps focus on the listbox. This is captured source
behavior, not an inferred availability policy.

The native implementation remains an uncommitted prototype. The latest local
run at `artifacts/smoke-pagination-scroll-visual-state` passes the first seven
Light states, including source heights, painted row offsets, control visibility,
focus, original rows and exact key owners. It fails the open options replacement:
the native popup is clipped to 72px instead of 110px. Dark and final Escape are
not reached in that run. These results do not replace the validated checkpoint
above, and no new native artifact or component acceptance is claimed.

The experiments establish two native constraints. CarouselPanel exposes row
scroll units, while StackPanel exposes pixels. With StackPanel, ComboBox's own
popup sizing rewrites the child height limits during layout and option changes.
Synchronous limit callbacks caused a retained layout-cycle failure at
`artifacts/smoke-pagination-scroll-hover-layout`; a held Opened-state limit also
failed after the native sizing rewrite. Those approaches are removed from the
prototype. The pinned WinUI 2.3.9 template and actual runtime reports remain
authoritative; current upstream ComboBox source was consulted only as diagnostic
context. The native popup sizing/ownership boundary must be resolved before
shipping the scroll controls or claiming the complete 18-state native replay.
The fixed inventory remains 51 in progress, 61 pending and zero accepted.

## Owned chooser and 36-state scroll replay

The scroll regression now contains 36 actual browser states (18 per theme),
collected in `artifacts/data-table-pagination-scroll-browser/capture-v5`.
The reference SHA-256 is
`cdca36b24ac2a8ef1304e368b9bd231f6c9d91c0be934ca365d10c3859875307`.
The collector still pins the original keyboard reference, bundle, browser,
CSS and fonts. Added paths cover open and closed typeahead, keyboard navigation
after viewport hover, repeated hover, and restoring options after dismissal.
Typing `5` from the closed value `10` selects `50` through the original table
exactly once; the following `0` retains that value without another callback.
This follows the captured Radix wrap/search ordering.

The private page-size chooser now owns a WinUI Popup, ScrollViewer and option
Buttons through an ItemsControl template. It replaces the earlier private
ComboBox workaround, leaving the public pagination/table/options contract
unchanged. This removes the native ComboBox height rewrite that clipped the
short popup. No copied SDK ComboBox template is shipped. Option identities are
reused for matching values; external table/options updates stay authoritative,
including an empty display for an unlisted current size.

The popup aligns to the painted trigger with a 4px gap. Its source-sized
viewport, padding, 36px rows and 28px hover controls produce 318px endpoint,
346px two-control and 110px short-list popup heights. Actual scrolling updates
control availability; extent-only options changes preserve the mounted flags
observed in Radix. Hover uses the native 50ms timer and selected row height.
The timer stops when the indicator leaves the last genuine pointer position,
on exit, disablement, dismissal or unload. First/last keyboard focus includes
viewport padding; typeahead centers an out-of-view target as captured in the
browser. Keyboard selection/dismissal finishes after the original routed event
so the original down/up observers retain their node.

The chooser exposes native UI Automation ComboBox/ExpandCollapse/Selection and
read-only Value patterns, its viewport exposes List/Scroll, and its options
expose ListItem/SelectionItem. Native disabled and detached selection requests
are rejected. Lifecycle checks retain the original observer, template,
unlisted-value, unload and reparent boundaries. These providers and tests do
not establish Narrator acceptance.

The clean targeted native outputs are:

- `artifacts/smoke-pagination-owned-final-pagination-scroll`: all 36 scroll
  states and 38 composed/final PNGs pass. The source/native audit at
  `artifacts/pagination-owned-final-scroll-audit.json` checks every PNG hash,
  source pin, focus/callback state, viewport measurement, 20 exact held-key
  pairs and 12 owned, nongenerated pointer moves with stable presentation.
- `artifacts/smoke-pagination-owned-final-pagination-keyboard`: the original
  136 states and 120 key actions pass, including both Shift+Tab transfers and
  two current-position clicks with no movement. All 34 popup widths, heights
  and gaps match; absolute positions still differ with bar typography. Audits
  are `artifacts/pagination-owned-final-keyboard-audit.json` and
  `artifacts/pagination-owned-final-popup-layout.json`.
- `artifacts/smoke-pagination-owned-final-pagination-input`,
  `artifacts/smoke-pagination-owned-final-pagination` and
  `artifacts/smoke-pagination-owned-final-pagination-source`: the original 12
  clicks, two Escape transfers, both-theme lifecycle and 132 producer states
  pass. Input audit: `artifacts/pagination-owned-final-input-audit.json`.

The browser's leave-indicator gesture goes outside the popup. The native modal
popup blocks delivery to the underlying bar, so its replay genuinely moves to
the painted popup border to leave the indicator. This verifies indicator exit;
outside-popup pointer behavior remains unverified. Readiness/input deadlines,
held-key ownership, rendering stability and geometric tolerance are unchanged.
The new verifier is `scripts/verify-data-table-pagination-scroll.mjs`.

Visual review confirms the short popup and both long-list controls are painted.
Exact pixel parity still fails: typography, source shadow and other paint/state
details remain open. The current 132 surface pairs differ by 502,848 pixels and
154 layout measurements. The independent Pillow audit agrees, with no masks
or tolerance (`artifacts/pagination-owned-final-pixels.json` and
`artifacts/pagination-owned-final-pillow.json`). Both architecture builds have
zero warnings/errors; all 89 regressions and generated-token checks pass.
Zero-delay key release, full disabled/motion coverage,
contrast/Narrator/scaling, measured collection performance and native ARM64
runtime are also open. These targeted outputs do not replace the full catalog
and package checkpoint until that separate validation is recorded. No inventory
entry is accepted; the fixed count remains 51 in progress, 61 pending and zero
accepted. No backend/metrics surface changed. The public wiki remains unavailable
and push-triggered Actions remain paused.
