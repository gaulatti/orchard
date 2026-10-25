# Native Windows runtime evidence

## Render-pass table alignment styles, 2026-10-24

The `2dabe1ac…` source candidate reuses equivalent generated alignment styles
among newly presented cells in one synchronous Data Table/Simple Data Table
refresh. Retained cells keep their presentation caches, caller styles use
reference identity, selectors still execute per cell, and no lookup survives
the pass. Both the original library and candidate pass the strengthened 84-state
live fixture. Detached-owner probes show all styles sealed before load and all
mutation attempts rejected, with caller bases and alignment unchanged.

Three normal processes retain every one of the 360 baseline semantic/lifecycle
operations. Preselected large-page paging allocation medians fall 9.4% at 2,048
rows and 2.6% at 16,384 rows; both allocation ranges are nonoverlapping. Timing
results remain mixed, with 26 fixed budget failures in both versions. Fifteen of
18 image pairs are exact; the other three retain one/four/five unexplained pixels.
All nine applicable native suites pass, including 208 reference states,
48,368 geometry values, 3,090 paint probes and 1,864 checkbox endpoints.
Both solution architectures and 495 portable plus 10 Windows text tests pass.

Local preview `0.1.0-table-style-v1-local` freshly restores into independent
x64 and ARM64 version caches and builds for both architectures, reusing existing
third-party dependencies. Its AnyCPU library and all package XAML, fonts, images,
PRI, README, transitive targets and licenses match current source. The dedicated
self-contained catalog publishes 550 verified files, reusing 546 unchanged
immutable SDK/resource files and extracting four changed files. It contains
one publish output, without a nested historical publish tree.

Fresh source, consumer and extracted catalog each pass 120 table semantic
operations, 84 live states and 72/70/72 Checkbox checks. Both fictional demos pass
66 checks per source/archive host. All raw shared states and lifecycle contracts
match. Of 274 native image pairs, 272 are exact: all live and Checkbox pairs,
plus ten of twelve delivery performance pairs; two retain tiny unexplained
differences. No thresholds or image masks change.

A later incremental catalog build reused temporary diagnostic object output.
A clean catalog rebuild produced the normal `aa68de58…` host; normal command
markers and all delivery replays were verified. The qualifying three-process
timing set retains its exact earlier `fa038fb1…` host and the same `2dabe1ac…`
library. Fresh delivery processes establish semantic correspondence, not a new
three-process timing median. Always rebuild and verify the normal catalog after
a temporary compile-item diagnostic override; restoring only bin files does not
qualify the intermediate compiler state.

See `artifacts/current/table-column-style-checkpoint-v1.json` for separate build,
package and catalog evidence. Full performance, visual/motion/scaling,
physical-input and native ARM64 acceptance remain open. All 112 entries and both
fictional-data demos remain in progress, zero accepted.

## Table queued-layout diagnostic and rejected language probe, 2026-10-24

A bounded current-library diagnostic passes 20 semantic/lifecycle operations
with exact original snapshots and cleanup contracts. It records 168 layout-pass
samples and identifies dispatcher wait as the dominant post-mutation stage.
A separate traced replay preserves those same contracts. The trace covers all
process phases; its nested sampled-thread-time values do not establish CPU
utilization or exclusive causes. Earlier full native-cost evidence remains
retained separately.

A narrow private-probe `Language` setter experiment then passes all 360 normal
semantic/lifecycle operations in three processes, but fails 28 fixed timing
budgets versus the prior 26. It has four nonoverlapping timing improvements and
three regressions; all allocation budgets pass. Seventeen of 18 image pairs are
exact, with five unexplained pixels in the remaining pair. The experiment is
preserved and reverted. No fresh package, native ARM64 runtime or full component
acceptance is claimed. Both production source hashes and eight normal source
host files are restored; a fresh 72-check Checkbox report matches the sealed
baseline byte for byte. The first output restore's PowerShell null-path argument
error and successful recovery remain recorded.

See `artifacts/current/table-probe-language-checkpoint-v1.json` and
[the performance account](data-table.md#shared-notification-callbacks).
The verified library/NuGet/catalog remain the preceding Checkbox-ready preview.
All 112 entries and both fictional-data demos remain in progress, zero accepted.

## Hidden Checkbox readiness correction, 2026-10-24

The earlier rich-content checkpoint below retained 13 source/consumer and four
source/archive `IsLoaded` differences. A separate plain-WinUI/Orchard diagnostic
records 224 observations over 24 cases. Three cases report loaded after layout
waits alone; all 24 report loaded after a hidden native render and unloaded after
removal. Late subscription after actual readiness was not tested.

The v3 shared fixture now renders its hidden root and asserts readiness before
mounted checks. Source passes 72 checks twice with identical reports; the
independent consumer passes 70 and extracted catalog passes 72. All shared raw
checks, including loaded state, match exactly. Each gate rejects 34 corruptions,
16 original wrong-name states, three wrong-style states and eight retained
unready observations. Both fictional demos pass 66 checks per source/archive.
All ten prior/source/consumer/archive native image pairs are byte identical.
Root and independent consumer fixture builds pass for x64 and ARM64; ARM64
runtime remains unverified. No foreground activation or hardware input is used.

This changes only validation and documentation. Library `22f84eba…` and NuGet
`0.1.0-checkbox-rich-v1-local` (`2ebf07f3…`) remain unchanged. Consumers reuse the
previously verified package cache. The refreshed self-contained catalog is
`0.1.0-checkbox-ready-v1-local` (`3035504b…`). The 505 regression tests and table
timings below are inherited from the preceding checkpoint, not rerun against
the new capture host `c193e65a…`; the 26 timing-budget failures remain open.
See `artifacts/current/checkbox-loaded-checkpoint-v1.json`. Full visual, motion,
scaling, physical-input and native ARM64 acceptance remain open. All 112 entries
and both required fictional-data demos remain in progress, zero accepted.

## Checkbox rich-content and combined table preview, 2026-10-24

Preview `0.1.0-checkbox-rich-v1-local` preserves direct rich-content names when
Checkbox presentation is absent and keeps consumer implicit text styles from
being overwritten by local defaults. The preceding library reproduced 16 empty
rich-name states. The name-only repair exposed three remaining style failures;
both original reports and sources remain retained. Two initial catalog name
assertions were harness errors because the demo supplies an explicit name.

Current source/package/extracted catalog pass 72/70/72 checks, including merged
styles, inherited setters, `RichTextBlock`, binding ownership, explicit metadata,
rendered template names and native toggle events. Each negative gate rejects
30 corruptions plus the 16 actual wrong-name and three wrong-style states.
Both fictional-data component demos pass 66 checks per source/archive host.
Root x64/ARM64 builds and all 505 regression tests pass. Independent consumers
restore the same fresh NuGet bytes and build for both architectures; their x64
runtime is verified separately from ARM64 compilation. The package retains its
AnyCPU library, 157 loose XAML files, one PRI, six fonts, two images and current
README/notices. The full standalone x64 ZIP verifies 1,360 files.

The combined preview retains the table callback allocation draft below. Three
fresh source processes and one independent-consumer process pass 480 semantic
operations. The current library fails 26 of 120 source timing profiles; all
allocation budgets pass. Large-page paging allocation medians are about 18%
and 15% below the fresh Switch-package baseline. Fifteen of 18 table image pairs
are byte identical; the remaining three have four/five unexplained pixels.
All ten Checkbox image pairs are byte identical. Its raw hidden-host `IsLoaded`
observations vary in four source/archive and 13 source/consumer checks, so full
lifecycle equivalence is not claimed. Final source/delivery fingerprints are
recorded in `artifacts/current/checkbox-content-checkpoint-v1.json`. Full
visual/motion/scaling, physical-input, collection-performance and native ARM64
acceptance remain open. All 112 entries and both required demos remain in
progress, with zero accepted.

## Table callback allocation draft, 2026-10-24

Current source `32b869ec…` shares equivalent table property callbacks while
retaining every property notification, cleanup token and original consumer
object. Three independent baseline processes of the verified Switch package
and three candidate source processes preserve all 360 semantic/lifecycle
snapshots. Median large-page paging allocations fall about 27% at 2,048 rows
and 15% at 16,384 rows. Timings remain mixed: the fixed budget audit fails
26 baseline and 27 candidate profiles. All allocation budgets pass; performance
acceptance remains open. Four of 18 native image pairs have one to five changed
pixels with unverified causes; 14 pairs are byte identical.

Eight native suites pass for customization, templates, text, table primitives,
Data Table composition, live data, supplemental filtering and reference history.
The reference replay covers 208 states, 7,560 rendered cells and 48,368 geometry
values; the paint gate checks 3,090 probes and 1,864 checkbox endpoints. Both
root architecture builds and all 505 regression tests pass. These are hidden
x64 runtime checks, with no foreground activation or hardware input. ARM64
runtime, full visual/motion/scaling and performance acceptance remain open.

The source-only evidence is retained in
`artifacts/current/table-callback-checkpoint-v2.json`. It is not a new package
delivery: the Switch checkpoint below remains the newest verified NuGet/catalog
preview. Fresh package/consumer verification is required for the next batched
delivery. Inventory remains 112 started and in progress, zero accepted, with
both fictional-data component demos required and in progress.

## Switch public state and native-input lifecycle, 2026-10-24

Preview `0.1.0-switch-lifecycle-v1-local` makes Toggle state available from
caller-owned `IsOn` before templating, rejects public activation while the owner
or native input is unmounted, ignores obsolete native input events, and
synchronizes the live input from public state on remount. The preceding library
reproduced 24 failed checks and 16 caller-state mutations, across both Boolean
states and themes. The original reports and production source remain retained.

Current source/package/archive pass 46/44/46 checks with 84/82/84 observations.
Each host verifies four untemplated states, rejects 12 unavailable and four
disabled actions, isolates eight retired native inputs, and records 48 bound
state observations. Event counts are asserted where measured; other state
observations explicitly record an unmeasured count as null. Name precedence,
single accessibility representation, template replacement, constrained LTR/RTL
labels and actual catalog caller output remain passing. Thirty corrupted reports
per host and the actual before-repair caller-mutation report are rejected.

All 84 source/archive observations and 82 common source/package observations
match. Six before/after captures and 12 source/package/archive image pairs are
byte-identical, with the zero-difference diagnostic unchanged. These comparisons
show native correspondence; they do not establish browser/native visual parity,
physical input, scaling or motion acceptance. No foreground input or OS settings
changes were used.

The 495 portable and ten Windows text tests pass. Both root and fresh isolated
NuGet-consumer architectures build cleanly. The package's AnyCPU assembly,
157 loose XAML files, 156 themes, PRI, fonts, images and current README pass
payload checks. Root binary identities are recorded separately; the current
lifecycle methods have matching compiled sizes in x64/ARM64, which does not
establish ARM64 runtime behavior. The initial identity-inspection helper's
PowerShell extension-method error is retained; the corrected helper passes.

The standalone catalog ZIP contains all 1,360 files. Its local validation copy
reuses 1,356 byte-verified files from the preceding immutable archive and freshly
extracts four changed files. It has no hard links to mutable build output.
Every ZIP/source/expanded file hash matches. Both fictional Monitoring and
Editorial demos pass 66 hidden checks per current source/archive host.

All 112 entries and both required demos remain in progress and unaccepted.
Complete visual/motion/scaling, physical-input and native ARM64 acceptance remain
open. This preview has no qualified collection-performance baseline or fresh
Data Table image comparison; earlier failures retain their original scope.
Evidence uses `artifacts/current/switch-lifecycle-*`; see the
[Switch contract](switches.md).

## Toggle Group retained-provider lifecycle, 2026-10-24

Preview `0.1.0-toggle-lifecycle-v1-local` prevents retained SelectionItem and
native Toggle handles from changing state after item removal or group unload.
The component checks current mounted ownership before selection actions and
native Toggle, and ignores events from retired children. Remounting and moving
an item to another group reconnect its handle to the current group. Active
native Toggle behavior and caller selection models remain intact.

The unchanged preceding library reproduced 32 failed lifecycle checks and 14
state-mutation observations. Four additional initial disabled-Toggle failures
were test error-mapping mistakes: the native UIA COM HRESULT differs from the
managed WinRT exception's HRESULT. The corrected check accepts the exact managed
exception type or the native UIA error code, and still requires unchanged state
and no selection events. Raw reports and an independent baseline analysis remain
retained; those four mapping failures are not counted as component defects.

Current source/package/archive pass 50/48/50 checks with 70 observations per
host. Each host rejects 32 retired-handle and 16 disabled actions and verifies
all six size/variant styles in both themes. Twenty-eight corrupt reports per
host and the actual before-repair mutation report are rejected. All 70
observations match across the three hosts; the source/archive reports are
byte-identical. These are behavior/style observations, not browser-image parity.
The actual catalog fixture displays the bound caller selection correctly.

Root and fresh independent-consumer x64/ARM64 builds pass. The package's AnyCPU
assembly, 157 loose XAML files, 156 themes, PRI, fonts, images and current README
pass payload checks. All 1,360 catalog archive files match the source bytes.
The 495 portable and ten Windows text tests pass. Both fictional Monitoring and
Editorial demos pass 66 hidden checks per current source/archive host.

Both root-build libraries are AnyCPU IL and contain the current lifecycle guard
methods. Their binary hashes differ and are recorded independently; the cause
of that difference is unverified. Both independent consumer architectures restore
the same verified package bytes. No ARM64 native runtime equivalence is claimed.

Full visual/motion/scaling acceptance, physical input and native ARM64 runtime
remain open. This preview has no qualified collection-performance baseline or
fresh Data Table image comparison. The preceding library's six differing table
pixels and the last qualified baseline's 26 failed budgets remain historical
evidence with their original scope. All 112 entries and both demos remain
unaccepted. Evidence uses `artifacts/current/toggle-group-*`; see the
[Toggle Group contract](toggle-groups.md).

## Temporary binding ownership repair, 2026-10-24

Preview `0.1.0-label-ownership-v1-local` repairs Label losing original runtime
bindings/style precedence and writing its temporary relationship into two-way
sources. It preserves newer bindings and style ownership even when they resolve
to the same label. Button Group now disconnects captured bindings before
assigning temporary geometry, preserving caller two-way values. Original
binding objects reconnect to current source values on release. See
[Label ownership](labels.md) and [Button Group ownership](button-group.md).

Source/package/archive pass 28/26/28 Label checks with 222 relationship/name
observations, and 22/20/22 group checks with the existing 80 layout states.
Twenty-nine corrupt reports are rejected per component/host. The actual
before-repair Label source mutation and previous native group overlap remain
rejected. All 18 group image comparisons and both source/archive Label captures
are byte-identical. Both root and independent-consumer architectures build;
495 portable and ten Windows text tests pass. The standalone Button's ten
style/template and twelve spacing-binding states remain passing. Both fictional
demos pass 66 hidden checks in each current source/archive host.

All 120 packaged Data Table semantic/lifecycle operations match the preceding
preview. Four of six captures are byte-identical; six pixels differ across two
images, maximum channel difference 20, cause unverified. The zero image gate
still fails. No current three-process performance baseline is claimed; the last
qualified baseline had 26 failing budgets. Native ARM64 runtime, physical input,
actual scaling and full visual/motion acceptance remain open. All 112 entries
and both demos remain in progress and unaccepted. Evidence uses
`artifacts/current/label-association-*`; previous results retain their scope.

## Connected Button Group repair, 2026-10-24

Preview `0.1.0-button-group-v2-local` repairs wrapping groups that left gaps,
overlapped by a pixel, or rendered actions narrower/shorter than their allocated
slots. The group restores caller local values, style values and original
appearance/alignment bindings after removal or unload. Its shared rounded
edges use the mounted root's rasterization scale. The default Button native
action fills its wrapper. See [the contract and native cases](button-group.md).

Source, independent NuGet consumer and extracted catalog pass respectively
20/18/20 hidden checks and 28/24/28 layout states. Each verifies 14 appearance
and eight alignment restorations. All twelve common native image comparisons
are byte-identical; the 22 captures also verify painted action coverage.
Twenty-six corrupt reports and the retained actual one-pixel overlap are
rejected per host. Both x64/ARM64 root and independent-consumer builds pass;
495 portable and ten Windows text tests pass. The current package also passes
the standalone Button's ten style/template states, twelve spacing-binding
states and all 120 Data Table semantic/lifecycle operations. Both fictional
demos pass 66 hidden checks in each current source/archive host.

The current table snapshots and lifecycle contracts match the preceding
preview. Five of six table captures are byte-identical; one pixel differs at
maximum channel difference 20, with the cause unverified. Its zero image gate
still fails. The last qualified three-process performance baseline had 26
failing budgets; no new performance acceptance is claimed for this package.
Native ARM64 runtime, physical input, actual scaling and full inventory
acceptance remain open. All 112 entries and both demos remain unaccepted.
Evidence uses `artifacts/current/button-group-contract-*`; older preview and
strict image results below retain their original scope and identities.

## Current package boundary, 2026-10-24

Local preview `0.1.0-component-validation-v1-local` is built from the current
library sources. Its AnyCPU DLL matches the fresh x64 source build and catalog
(`3afe4362853fef3c2b270e015769a7c57bbc0bac8e278cbe91ad6880906b9b9b`).
The payload audit verifies all 157 loose XAML files against source, 156 themes,
the PRI, six fonts, two images, current README and required notices. The fresh
x64 root build has zero warnings/errors; 495 portable and ten Windows text
tests pass. Independent x64 and ARM64 consumers restore the exact package into
fresh Orchard caches without repository project references. Installed third
party dependency packages are reused.

The first ARM64 publish ran out of disk space. Its failed output and logs are
retained. After hash-preserving NTFS compression of 64 finished diagnostic host
copies recovered about 2.7 GB, the same isolated restore publishes successfully
to a fresh output using the pinned SDK's build/publish hardlink options. This
is an ARM64 build result, not native ARM64 runtime evidence.

The independent x64 consumer passes 116 hidden Theme Toggle states and 42
ownership/metadata checks, with zero geometry error. The catalog ZIP has 1,360
files, each verified after extraction. Source and extracted catalog separately
pass all 66 hidden Monitoring/Editorial demo checks in light and dark themes,
including original data, selection, filtering, editing and remount contracts.
These checks guard against activation, visibility, focus and hardware input.
They do not establish foreground interaction or complete visual acceptance.

The packaged Data Table passes all 120 native semantic/lifecycle operations,
matching the qualified source baseline's snapshots and contracts. The verifier
rejects 74 corrupt reports and the retained late-observation failure. Five of
six original captures are byte-identical; the 2,048-row append capture differs
at five pixels, maximum channel difference 20, with the cause unverified. The
comparison retains its zero threshold and reports exact-image failure.

Reports use the `artifacts/current/component-delivery-*` prefix. The prior
combined-renderer delivery and strict image diagnostics retain their own
identities and actual results. The current preview does not inherit those
runtime results as fresh package evidence. All 112 inventory entries and both
fictional demos remain in progress and unaccepted. The qualified three-process
Data Table baseline still has 26 failing performance budgets; a new package
smoke run cannot replace that baseline. Follow the
[acceptance contract](acceptance-contract.md) for remaining visual, behavior,
motion, scaling, platform and delivery requirements.

## AlertDialog source/model preparation, 2026-10-19

The latest continuation is `artifacts/current/alert-dialog-preparation-checkpoint.json`.
The frozen source contains 56 layouts/PNGs, 36 input histories, 40 presence
samples and 20 endpoint CSS samples. Final independent captures match all 57
files. The source layout/presence/endpoint model matches these contracts, and
all 315 portable tests pass. The source verifier decodes every PNG and checks
surface fill, reserved scrollbars, relationships, loading and focus metadata.

These are original headless browser interactions, not native modal tests.
Browser-realm clocks and Web Animations control timer/presentation samples;
no Windows preferences or time settings change. The native control, fixture,
modal host/integration, interrupted motion and package remain pending. The
current model is newer than the verified ScrollArea package. Inventory stays
92 in progress/20 pending/zero accepted of 112 — 82.1% entries started — with
both fictional-data demos required/in progress and unaccepted. See
[the measured contract and acceptance gaps](alert-dialog-reference.md).

## ScrollArea native prototype, 2026-10-19

The implementation checkpoint is `artifacts/current/scroll-area-implementation-checkpoint.json`.
Catalog and fresh `0.1.0-scroll-area-v2-local` consumer each pass 94 layouts,
12 controlled scrolling histories, 3,076 zero-error geometry values and five
contract groups. Fourteen raw native client PNGs bind 12 paint samples,
17,952 straight thumb-body pixels (maximum channel error one) and 960 zero-height
clipping pixels. The source scrollbar capsule uses native 3/7.5-DIP radii;
an earlier elliptical native PNG is retained as a rejection fixture. The auditor
rejects 28 damaged catalog and 32 damaged package receipts. All runs record zero
visibility, activation, focus and physical input. Native scroll methods and
visual states are injected; physical event routing remains unverified.

Both catalog architectures and clean package consumers compile. All 309 portable
and nine windowless font tests pass. The control/fixture and both keyed style
examples are registered in navigation and `catalog-coverage.json`. Package
payload bytes and both catalog archives are checked separately. See
[the public contract and acceptance gaps](scroll-area.md).

The same package passes HoverCard 44 layouts/five contract groups, Popover
122 layouts/five groups, Header 72 layouts/23 scenarios with the existing
one-channel paint gate, Footer 192 frames/11 contracts and AppShell 168 layouts/
44 ownership checks. These are fresh regressions on the ScrollArea package.

Inventory: 92 in progress, 20 pending, zero accepted of 112 — 82.1% entries
started. Both fictional-data demos remain required/in progress and unaccepted.
Complete raster/rounded edges, physical input/focus, broader integration,
scaling/performance, visible startup and native ARM64 acceptance remain open.
Background only; no Narrator, OS settings, pointer heartbeat, Actions or pushes.

## ScrollArea source/model preparation, 2026-10-19

The preparation checkpoint is `artifacts/current/scroll-area-preparation-checkpoint.json`.
Independent source captures match 107 receipt/PNG files: 94 layouts, 12 scrollbar
paint states and 16 input histories. The collector explicitly removes
Playwright's headless `--hide-scrollbars` switch. Five new portable regressions
bring the passing suite to 309. The model compiles for x64 and ARM64.

Ten hidden native capability scenarios establish a suitable `ScrollView`
configuration and reserved-gutter template. Ninety geometry comparisons include
eight original source thumb bounds; ten full native client PNGs and 18 fill
samples pass. There are 338 guards with zero visibility, activation, focus or
input. The auditor rejects 27 damaged receipts. This diagnostic uses the old
HoverCard package as its host and does not implement or package ScrollArea.
The current model is newer than that package. See [the contract](scroll-area-reference.md).

Inventory remains 91 in progress, 21 pending, zero accepted of 112 — 81.3%
entries started. ScrollArea remains pending; its native control, fixture and
package remain open. Both fictional-data demos remain required/in progress.
All broader component acceptance remains open. Background only; no Narrator,
OS settings, pointer heartbeat, Actions or pushes.

## HoverCard native prototype, 2026-10-19

Local `0.1.0-hover-card-v1-local` implements the reusable inline control and
catalog fixture. Catalog and fresh x64 package consumer each pass 44 source
states, 1,020 zero-error geometry/layout values, 132 brush comparisons, 96
native presentation samples and five contract groups. Actual native point
queries cover direct content, the literal gap, disabled originals and caller
stacking. Private model/time injection does not prove physical input delivery.
Eight complete native client PNGs bind dimensions, hashes and fill samples;
complete source raster/shadows remain unverified. All runs record zero
visibility, activation, focus and input. The auditor rejects 21 damaged catalog
and 25 damaged package receipts.

The same package passes Popover 122 states/five contracts, Header 72 layouts/
23 scenarios with source paint error at most one, Footer 192 layouts/11
contracts and AppShell 168 layouts/44 ownership checks. All 304 portable tests
pass; the nine windowless font tests are inherited only after their unchanged
inputs are rehashed. Both architecture catalogs and clean package consumers
compile. Versioned archives and package payload bytes are verified. Native
ARM64, physical input/focus, nested/ancestor integration, full visual/motion,
scaling, performance and normal visible startup remain open.

The continuation is `artifacts/current/hover-card-implementation-checkpoint.json`.
Inventory: 91 in progress, 21 pending, zero accepted of 112 — 81.3% entries
started. Both fictional-data component demos remain required/in progress and
unaccepted. No Narrator, OS settings, pointer heartbeat, Actions or pushes.

## HoverCard source preparation, 2026-10-19

`tests/Reference/HoverCard.json` records 44 source states, four input histories
and 16 motion timelines. Independent final captures match all 45 receipt/PNG
files. Five new portable tests bring the suite to 304 passing tests and match
the production placement/motion model against 44 layouts and 96 timeline samples.
The source is an inline CSS hover group: no portal/collision handling, no
keyboard opening or Escape dismissal, and snapping translation with animated
opacity. See [its complete contract](hover-card-reference.md).

`artifacts/current/hover-card-preparation-checkpoint.json` seals this preparation.
Native inline overflow/hit testing, the mounted control/fixture and a fresh
HoverCard package remain unverified. The Popover package below remains the last
verified implementation package; it does not contain the new HoverCard model.
Inventory stays 90 in progress, 22 pending and zero accepted: 80.4% entries
started. Both fictional-data demos remain required/in progress/unaccepted.

## Popover native prototype, 2026-10-19

Local `0.1.0-popover-v1-local` implements the reusable control and catalog
fixture. Catalog and fresh x64 package consumer both pass 122 source states,
1,920 zero-error geometry/layout values, 360 brush comparisons and five
ownership/lifecycle groups. Native child PNG dimensions/background samples
pass; complete source raster and shadows remain unverified. Runs record zero
visibility, activation, focus and input. Fourteen damaged catalog receipts and
17 damaged package receipts are rejected.

The same package passes Header 72 layouts/23 scenarios (source paint error at
most one), Footer 192 layouts/11 contracts and AppShell 168 layouts/44 ownership
checks. All 299 portable and nine windowless font tests pass. Both architecture
catalogs and isolated package consumers compile; ARM64 runtime remains open.
Versioned catalog archives match all built files, and package DLL, XAML, PRI,
README and Floating UI license bytes are verified. Earlier native layout-cycle,
initial-sizing and presentation failures remain recorded and are addressed by
the passing final catalog/package runs. See [the API and remaining gaps](popover.md).

The continuation is `artifacts/current/popover-implementation-checkpoint.json`.
Inventory: 90 in progress, 22 pending, zero accepted of 112, or 80.4% entries
started. Both fictional-data demos remain required/in progress/unaccepted.
Native input/focus, nested/modal integration, full raster, scaling, performance,
visible startup and complete ARM64 acceptance remain open. Background work only;
no Narrator, OS settings, pointer heartbeat, Actions or pushes.

## Popover source and native primitive preparation, 2026-10-19

`tests/Reference/Popover.json` binds 122 source states and 16 histories, with
byte-identical independent captures and actual nested-module provenance. The
production state/placement model matches all 120 open source rectangles and
side/alignment choices. Five new tests bring the portable suite to 299 passing
tests. The source normal stylesheet reports no popup animation despite its
220/150-ms tokens; the implementation must follow the effective styles.

A new isolated x64 consumer harness opens a real, root-constrained native Popup
under a hidden window. Its anchor-relative bounds and child pixel sample match,
with 13 isolation checks and zero visibility, activation, focus or input. That
probe uses the existing Header paint package only as a baseline dependency;
it does not validate a Popover component or package. The reusable Popover control
and fixture remain pending. See [the full source contract](popover-reference.md).
The continuation is sealed by `artifacts/current/popover-preparation-checkpoint.json`.

## Header solid paint repair, 2026-10-19

Local `0.1.0-header-paint-v1-local` preserves public source brush values while
encoding combined solid alpha once into private native paint. Catalog and fresh
x64 consumer pass 23 native scenarios, including paint ownership/lifecycle;
their independent source paint gates pass at maximum error one. The gate's
one-level limit and frozen PNGs are unchanged. The package passes 72 layouts,
6,096 zero-error geometry checks, 952 source brushes and 144 exact private alpha
checks. Both architecture builds and fresh consumer compilations pass, as do
294 portable tests. Full raster, input/focus, normal motion, scaling/performance,
broader integration, visible startup and native ARM64 acceptance remain open.
Work remains in the background; both fictional-data demos remain required.
The exact package also passes Footer192/11 and AppShell168/44 regressions;
44 layout and 68 contract corruptions are rejected against fully passing valid
baselines. Versioned catalog archives match their builds byte for byte. See
`artifacts/current/header-paint-checkpoint.json` for source and evidence hashes.

## Earlier Header/AppShell repair draft with an open visual gate, 2026-10-19

`artifacts/current/header-integration-checkpoint.json` records the local
`0.1.0-header-integration-v3-local` prototype. Header and footer slot wrappers
follow original root stacking, fixing Header painting/hit order above AppShell's
opaque main. Twenty-two native scenario assertions pass; actual ScrollViewer,
ScrollView, reparenting and live position paths are exercised. Six original
composition states match 120 geometry checks. Raw native frames and lossless
viewport extraction are preserved, including dark logo decoder bounds.

The independent structural audit passes. The source paint probe gate remains red:
dark closed bottom-rule pixels differ by two channel levels, above the existing
one-level gate. Overall audit `passed` is false, and component acceptance is
unproven. Both builds and fresh consumer compilations pass; 294 portable and
nine windowless font tests pass. Native ARM64 and normal visible/input, motion,
scaling, performance and complete raster checks remain open. Work is background
only; both required fictional-data demos remain preserved and unaccepted.

## Earlier Header native prototype and original-model selection, 2026-10-19

The local Header v2 package and catalog pass 72 source states each, with 6,096
zero-error geometry checks and 952 brushes. Fifteen public contracts pass,
including real ScrollViewer presentation, original slot/model/selector ownership,
worker updates, retirement and template replacement. The fresh x64 package
rejects 42 damaged layout receipts and 37 contract receipts/images. Both builds
and isolated consumer compilations pass; ARM64 execution remains open. The
exact package also passes Footer192/15,216zero-error/1,890brushes and eleven
public contracts, plus AppShell168layouts/44ownership/zeroerror.293portable and
nine windowless font tests pass. No native input or full raster parity is claimed.
Inventory is 89 started,23pending,0accepted; both fictional demos remain required.
See [Header](header.md) and [shared content presenters](content-presenters.md).

## Header source and native placement investigation, 2026-10-19

Header remains pending. Its 72 source states and four interaction histories
match independent captures; five new reference tests pass, bringing the portable
suite to 293. A hidden, fresh x64 package consumer exposed a clipping defect in
the initially considered zero-flow layout despite correct measured rectangles.
An owned template surface with negative margins passes both candidate placement
scenarios; the same binary reproduces 12 rejected-design failures across three
scenarios. The independent auditor checks all 5,760,000 diagnostic pixels and
the hit-test tree, with 45 guards and no activation, focus or input. It rejects
39 damaged receipts/images. The existing Footer package/library and catalog
archives remain the verified local component artifacts. No native Header parity
or component acceptance is claimed. See [Header](header-reference.md).

## Expanded Footer source layout, 2026-10-19

Sixty-four source states now cover wrapping, empty values, long bottom slots,
multirow sections, narrow RTL/CJK text and live mutations. Independent recaptures
match and all original 48 images/measurements remain unchanged. The stronger
baseline exposed 318 geometry failures. The corrected final catalog and fresh
x64 package each pass all 192 layouts and 15,216 geometry checks with zero error,
1,890 brushes, seven lifetime checks and 1,877 guards. Their auditors reject
46/50 damaged receipts. Eleven public contracts pass, alongside both builds,
fresh consumers, 288 portable tests and nine windowless font tests. The fresh
package's Nav Menu and threading regressions pass too. Every check is background
only: no activation, focus, input, OS setting change or hosted workflow. Visible
rendering/input, broader properties and native ARM64 remain unverified. See
[Footer](footer.md) for package hashes and the checkpoint.

## Footer public-API contracts, 2026-10-19

Ten slot/link/lifecycle scenarios pass in both the catalog and a fresh isolated
x64 NuGet application, with 179 isolation guards and no activation, focus or
native input. Original editor selection, bound model/template/selector handling,
original link metadata, explicit command parameters and action retirement are
covered. Auditors reject 24 catalog and 28 package corruptions. Both architecture
builds and fresh consumer compilations pass alongside 287 portable tests. The
library DLL/package remain unchanged; no new library behavior is claimed.
See [Footer](footer.md) for the exact scenario list and remaining acceptance work.

## Producer notification regression, 2026-10-19

The catalog and independent x64 NuGet consumer pass actual worker-thread
notifications through Select, Header Select, Collection Filters and the three
DataTable views. Five scenarios cover 23 publications, original metadata/items,
source replacement and unload, with 191 isolation guards and no activation,
focus or input. The saved prior package fails all five dispatcher boundaries
with `0x8001010E`. The new catalog/package rejection audits reject 27/31 damaged
receipts. Footer catalog and package layout/paint regressions pass against the
same new library DLL. Both architecture builds and fresh consumer compilations
pass; ARM64 runtime remains open. See [producer notifications](producer-notifications.md).
Inventory is unchanged at 88 in progress, 24 pending and zero accepted.

## Footer prototype, 2026-10-19

Footer has a registered fictional-data fixture and initial public metadata,
section/link and bottom-content APIs. Its 48 source layouts are independently
reproducible at a 900-pixel client height; all geometry matches the prior
1600-pixel-height reference. The production breakpoint/nullish-content models
and reference provenance are covered by the 287 passing portable tests.

The initial visible attempts are failed/incomplete runtime evidence. The first sequence retained
one PNG before a WinUI exception. Subsequent sequences completed 41 and 39 state
records before stack overflows; all partial reports and raw PNGs remain intact.
Background reproduction found a motion callback accessing `IsLoaded` off-thread,
followed by recursive failure while the error reporter queried UI resources.
Callbacks now dispatch before reading UI state; reporting avoids that UI query.
The current hidden run completes 144 layouts (three full source cycles), 8,040
exact geometry checks, 1,434 actual brush checks and seven lifetime/threading
checks, with 1,349 isolation guards and zero activation/focus events. It sends
no input. All 40 damaged reports are rejected. Exact tested DLLs are preserved.
Brand wrapping uses the existing text bridge and unbreakable link widths retain
their source overflow. Both current architecture builds and 287 portable tests
pass. The fresh local Footer v1 package builds independent x64/ARM64 consumers;
its x64 hidden runtime repeats the same 144 layouts, 8,040 exact geometry and
1,434 brush checks, seven lifetime/threading checks and 1,349 isolation guards.
All 44 damaged package receipts are rejected, and the generated restore/source
caches are removed. DLL, four XAML files, PRI and README match the package.
This is an unaccepted local package; full visible runtime acceptance is not
claimed. The earlier verified Nav Menu package remains preserved;
a current-binary Nav Menu source audit is still required after shared changes.

Visible native tests are no longer authorized after October 18 midnight EDT.
Continue background builds, hidden-window diagnostics without activation/focus
or input, and source analysis. Inventory is 88 in progress, 24 pending and zero
accepted (78.6% entries started). Both required fictional-data demos remain.

## Nav Menu draft, 2026-10-18

The source-bound x64 run passes 42 layouts, 888 exact geometry checks, 138
actual paint checks, eight native mouse/keyboard histories (24 samples) and
26 lifetime checks. Original item identities, raw destinations, custom renderers,
retained input/selection/focus after reorder, live brush opacity and retired
actions are covered. Forty damaged foreground reports are rejected.

Both catalog architectures and fresh isolated consumers of
`0.1.0-nav-menu-v2-local` build cleanly. Its x64 hidden runtime passes 42 layouts,
888 exact geometry checks, 78 normal-state paint checks, 16 ownership checks
and 336 isolation guards without activation/focus. Forty-two damaged hidden
reports are rejected; temporary restore directories are removed. The package's
DLL, four loose XAML files, compiled PRI and README match the verified inputs.

All 285 portable and nine Windows text tests pass. The latter bind cursive
tracking to source widths while preserving Japanese/Latin measurements.
Four raw PNGs are exact; 88,562 pixels still differ over 42 full images. No
masks, tolerance, resampling or scene painting are used. Full raster, native
motion, scaling, further customization/integration and native ARM64 runtime
remain open. This draft is unaccepted. Inventory: 87 in progress, 25 pending,
zero accepted; 77.7% entries started. Monitoring and Editorial demos remain
required. No OS settings, Narrator, pointer heartbeat or Actions were used.
See [Nav Menu](nav-menu.md) for the API and evidence boundary.

## Collection Filters draft, 2026-10-18

The source-bound x64 run passes 50 layouts, 3,408 exact geometry checks and all
44 input/state samples from eight browser sequences. The control never assigns
its caller's filter dictionary or sort object. Both themes verify accepted and
declined filtering/sorting, boolean false, removal, Clear all, default expansion
updates and binding retention. Template replacement, inactive/hidden peers and
disabled actions cannot emit requests. The initial stale-peer test exposed a
callback closing over the replacement action; callbacks now validate their
actual sender against the current visible template.

The initial source comparison also exposed duplicate RTL mirroring, a composed
Date Picker retaining its standalone width, and Windows formatter-added LRM
markers in generated chip strings. Those are corrected. The native input harness
waits for the popup's original option focus before sending keyboard input.
Self-removing chip clicks are proven by trusted native mouse presses, physical
releases and the resulting consumer request, without requiring the retired node
to receive a later routed release event.

All 50 complete physical client PNGs are retained. Two empty states match
exactly; 48 retain 661,819 differing pixels. The owned palette is generated from
22 unchanged CSS-token DOM swatches, with alpha encoded once in each brush color.
Both foreground and hidden package runs pass 420 actual-brush checks. The
original 48 cases retain 630,648 differing pixels, down 81.5% from 3,403,374;
the two new numeric cases add 31,171. No masks, resampling or pixel tolerance are
applied. All 44 damaged foreground receipts are rejected. Both catalog
architectures build cleanly, and 282 portable tests pass.

The fresh `0.1.0-collection-filters-v4-local` package (SHA-256
`471e70cb179af43e7ee05c3669ee867ebffdd27d81947838bd8e986852b5c263`)
builds independent x64 and ARM64 consumers with their own feed/cache and no
source-project reference. Both builds are warning-free. The x64 hidden runtime
passes 50 layouts, 1,944 geometry checks, 16
ownership checks and 558 isolation guards with zero activation/focus events.
Temporary source and package caches are removed after publishing the runnable
consumer. All 31 corrupted hidden receipts are rejected. The packaged DLL,
six loose XAML files and compiled PRI match the tested build. This hidden
evidence is separate from foreground input and raster.

Numeric-key coverage includes zero, the last valid array index and non-canonical
spellings such as `01`, `-0` and `4294967295`. The old model fails the browser-bound
regression; chips and emitted dictionaries now follow source order without
changing the original dictionary. Actual mouse removal and keyboard re-selection
match accepted and declined source histories. The 12-filter case exposed the
count's missing tabular numerals; the corrected toggle matches source width.

See `artifacts/current/collection-filters-order-native-v2/audit.json` and
`docs/collection-filters.md`. Exact paint/raster, normal motion, broader prop/date
input, customization/scaling, final integration checks and native ARM64
runtime remain open. The v20 preview and earlier checkpoints remain preserved;
no push or workflow dispatch was performed. The public wiki remains unavailable,
so documentation is maintained here.

## Pagination draft, 2026-10-18

The controlled Pagination control passes all 66 native/source geometry
states with zero error. It uses the actual bundled medium font, source page
window arithmetic and native buttons; availability comes from the caller's
booleans. The initial capture exposed a 1/128-DIP centering error on fractional
labels. Snapping the initial allocation to the source's layout grid resolves
it, with the failed receipt retained separately.

Both themes pass physical click/Space/Enter requests, declined requests without
page/binding mutation, retained focused buttons, programmatic updates without
requests, empty layout, disabled/detached peer rejection, template replacement
and remount. Complete raw client PNGs remain unmodified. Six empty states match
exactly; the remaining 60 have 79,424 differing pixels. On the retained original
58 states, differences fall from 91,442 to 67,532. All 32 damaged foreground
receipts are rejected, including a real old PNG with a self-consistent hash and
mismatch receipt. This is not complete visual or motion acceptance.

`OuterBorderEdge` paints beneath transparent borders, resolving the inactive
Dark page-button gap. The audit requires 260 actual composed border pixels to
match exactly. Dark arrow hover uses the pinned dark sand token; page labels
follow the native presenter's hover foreground. Actual text/path foreground and
presenter background/border values match the source across all 66 states.

Both catalog builds and all 274 portable tests pass. The fresh
`0.1.0-pagination-v4-local` package (SHA-256
`6987db7df672f368d05ea5a7df2c9d72a331688ab9675f6703b039a77f166f76`)
builds independent x64 and ARM64 consumers. The x64 package also passes 52
hidden source layouts, 18 ownership/lifecycle checks and 436 isolation guards;
all 23 damaged hidden receipts are rejected. The 392 native package buttons and
their template presenters retain outer-border fill. Native ARM64 runtime remains
unverified. Neither isolated consumer has a source-project reference; its
temporary source/package cache is removed after publishing the runnable output.

The motion collector verifies 64 source transitions and 384 samples in both
themes and motion preferences. The production per-property clocks now implement
190ms premium easing and source color-space interpolation; pressed scale stays
immediate. A focused catalog replay derives targets from actual native input and
properties, then presents the production paint method at controlled times. All
352 native samples match source RGB, source alpha serialization, opacity and
scale, and all 64 detached scopes release paint subscriptions. Four raw native
PNGs are retained; 28 damaged motion receipts are rejected. Windows reports
animations disabled. No OS setting was changed, and this controlled-time
diagnostic does not establish normal real-time preference-path playback.

See `artifacts/current/pagination-motion-native-v2/audit.json`,
`artifacts/current/pagination-motion-presentation-v2/audit.json`,
`artifacts/current/pagination-motion-consumer-x64-v1/pagination-audit.json` and
`artifacts/current/pagination-motion-checkpoint.json`. The v20 preview package remains
preserved separately. No workflow was dispatched or push performed; automatic
push triggers remain paused. The public wiki is unavailable, so documentation
stays in this repository.

## AdminShell text rendering diagnostic, 2026-10-18

The latest probe expands to 68 complete raw client PNGs: native text, seven
standard Win2D profiles, 24 custom gamma/contrast profiles and two coverage
profiles per theme. All retain zero source geometry error and the pinned font.
The catalog-only COM bridge uses the installed Win2D 1.4.0 native interop
interfaces with DirectWrite's
[CreateCustomRenderingParams](https://learn.microsoft.com/en-us/windows/win32/api/dwrite/nf-dwrite-idwritefactory-createcustomrenderingparams).
Readback from each actual drawing session verifies gamma, enhanced contrast,
ClearType level, pixel geometry and rendering mode. OS settings are unchanged.

The source header's channel values largely fit five/six/five-bit coverage
quantization; that observation alone does not identify the source renderer.
Two private probes quantize coverage freshly generated from the font and blend
the fixture's foreground/background colors. They never paint source pixels or
modify comparison images. RGB565 reduces Light's differing pixels to 533 from
native's 575, but Dark retains 589 versus native's 575. The best Light aggregate
RGBA error is 23,134 with gamma 1, contrast 0 and NaturalSymmetric mode; the
default profile remains better for Dark. None matches exactly.

Both current catalog builds pass with zero warnings/errors and native x64 exits
with code zero. All 38 damaged-receipt checks reject false metadata, including
requested/applied parameter changes, missing readback and coverage-order damage.
See `artifacts/current/admin-shell-text-coverage-native-v1/audit.json`,
`artifacts/current/admin-shell-text-coverage-negative-v1/summary.json` and
`artifacts/current/admin-shell-text-parameters-checkpoint.json`. The production
v20 package and library are unchanged; its 4,067 differing text pixels remain
open. Arbitrary backgrounds, native ARM64 runtime and full acceptance remain
unverified.

The preceding standard-profile checkpoint is preserved below.

The isolated text fixture retains the verified v20 library binary and compares
the native header with seven Win2D profiles in both themes. All 16 complete raw
client PNGs retain zero source geometry error and the exact pinned regular font.
The revised offscreen-target fixture exits cleanly; x64 and ARM64 catalog builds
have zero warnings/errors. All 28 damaged diagnostic receipts are rejected,
including a successful payload paired with a failed native process exit.

For the Light header, native grayscale has 575 differing pixels and 54,575 total
RGBA-channel error. Default ClearType has 596 differing pixels and 29,736 total
error. Dark has 575/56,454 and 598/28,292 respectively. ClearType brings the color
closer, but it does not establish exact parity. No tested standard profile is a
production replacement; the library preview remains v20 with 4,067 text pixels
open across its full source matrix. Arbitrary-background rendering is unverified.

The first live-surface probe wrote all frames but ended with native exception
`0xc0000374` during shutdown. It remains unaccepted. The revised fixture uses a
disposed offscreen render target and a decoded native image; its exit code is
zero. Complete images are compared without masks, resampling or source-pixel
painting. See `artifacts/current/admin-shell-text-native-v2/audit.json` and
`artifacts/current/admin-shell-text-negative-v2/summary.json`.

The pinned dependency is Win2D 1.4.0. Its rendering mode/grid-fit constructor
was checked against the installed API and
[Microsoft's rendering parameter documentation](https://microsoft.github.io/Win2D/WinUI3/html/M_Microsoft_Graphics_Canvas_Text_CanvasTextRenderingParameters__ctor.htm).
The opaque-target constraint follows Microsoft's
[text antialiasing guidance](https://microsoft.github.io/Win2D/WinUI2/html/T_Microsoft_Graphics_Canvas_Text_CanvasTextAntialiasing.htm).
The expanded probe above rules out those tested per-draw parameter combinations.
No production library, Actions trigger, commit or push
changed; the public wiki remains unavailable.

## AdminShell fractional footer paint, 2026-10-18

The v20 candidate keeps the logical footer wrapper fractional and paints its
border through a separate native template. Its component-owned translation
accounts for the scrolling parent's separately rounded origin. Layout and view
changes update only that decoration. Original caller controls, metadata and
bindings remain intact; the scroll subscription disconnects on release.
Production code does not override framework composition-node flags.

`artifacts/current/admin-shell-footer-native-v20/audit.json` passes all 312
initial and 16 scrolled geometry/clip checks with zero error. Both complete
fractional surface PNGs now match the frozen source exactly. Of all 88 PNGs,
74 match exactly; the remaining 14 scalar-text images retain 4,067 differing
pixels. The preceding v16 preview had 72 exact frames and 6,071 differing pixels.
The audit now requires exact fractional surface pixels in both themes.
All 39 damaged foreground receipts are rejected, including two complete-image
substitutions whose hashes and mismatch receipts are internally consistent.
No masks, tolerance, resampling or source-pixel painting are used.

The fresh v20 independent package passes 72 ownership checks and 2,680 isolation
guards, including a caller-owned footer rounding binding. Both catalog and
consumer architectures build, and all 9,434 library method bodies, normalized
metadata, resources and static data match across x64/ARM64. Native ARM64
runtime, full interaction/customization, scaling and complete raster acceptance
remain open. The 112-entry inventory remains 84 started, 28 pending and zero
accepted. Six related package runtime/source replays and both required demos
pass on these exact binaries; the demos retain 66 checks and 577 isolation
guards, and all 25 damaged demo receipts are rejected. Twelve visible captures
verify original keyboard/pointer handlers, wheel scrolling, width reset and
retained edits across theme and maximize/restore. The recorded preview is
`artifacts/current/admin-shell-footer-checkpoint.json`; full component acceptance
remains false.

The constructor and loaded visual-flag attempts are retained as rejected
v17/v18 diagnostics. WinUI overwrote those framework-owned flags. The v19
template-only attempt retained geometry but did not improve the raster count.
These attempts are evidence, not accepted previews. No OS settings, Narrator,
product commit, push or workflow dispatch was used. The wiki remains unavailable.
The current 32-probe focused fixture also passes on v20, with both fresh baseline
images matching exactly. A preceding post-input capture contains the computer-use
pointer halo outside the component at the bottom-right client corner. Resetting
the computer-use JavaScript kernel removed it. The clean receipt is under
`artifacts/current/admin-shell-footer-focused-v20-clean`; the contaminated raw
images and their explicit contamination receipt remain preserved without masks.

## AdminShell device-aligned main clip, 2026-10-18

The v16 component fixes an extra main-region fringe at fractional coordinates.
Its native viewport paints the background through a one-way binding; the outer
main region owns a device-aligned clip after layout. Logical caller geometry,
original controls and bindings remain unchanged. Caller viewport-background
overrides retain ownership, and all added observers detach on unload/release.
No production code modifies internal composition nodes.

`artifacts/current/admin-shell-clip-native-v16/audit.json` passes all 312 initial
and 16 scrolled geometry/clip checks. Of 88 complete raw native PNGs, 72 match
the frozen browser exactly; 6,071 pixels differ in the other 16, reduced from
7,170. No masks, tolerance or resampling are used. Fresh two-theme diagnostic
instances independently confirm the main fringe is removed; fractional footer
border and shared text antialiasing differences remain open.

The local `0.1.0-admin-shell-clip-v16-local` package passes 70 hidden ownership
checks and 2,676 isolation guards, six related native runtime replays and the
66-check/577-guard Monitoring and Editorial demo replay. All 59 damaged hidden,
36 foreground, 41 AppShell and 25 demo receipts are rejected. All 266 portable
tests pass. Both catalog architectures build with zero warnings/errors and
match all 9,430 library method bodies, normalized metadata, resources and static
data. Fresh x64/ARM64 consumers build; native ARM64 runtime remains unverified.

Twelve visible captures in `artifacts/current/admin-shell-clip-visible-v16`
bind these exact library/catalog binaries. Tab reveals the far-edge original
button; Enter and pointer clicks reach its handler. Space and pointer clicks
invoke the original footer action. Wheel scrolling leaves header/sidebar fixed.
Edited input and action results survive width reset, theme change and
maximize/restore. Full interaction/customization, raster, scaling and ARM64
acceptance remain open. The checkpoint is
`artifacts/current/admin-shell-clip-checkpoint.json`; the inventory remains 84
started, 28 pending and zero accepted of 112. No OS settings, Narrator, push,
product commit or workflow dispatch was used. The public wiki remains unavailable.

## AdminShell complete native PNG receipt, 2026-10-18

`artifacts/current/admin-shell-raster-native-v6/audit.json` verifies all 312
initial and 16 horizontally scrolled native layouts with zero geometry error.
The foreground run saves all 88 complete client PNGs and binds the actual
catalog/library hashes to the frozen source. It compares every pixel without
masks, resampling or tolerance: 72 frames match exactly and 7,170 pixels differ
across the remaining 16. Text antialiasing and fractional border/clip edges
remain unresolved; full source raster parity is explicitly false.

Scrolled captures wait three seconds for the existing native scroll indicator
to finish its resting transition. The immediate captures are retained in
`admin-shell-raster-native-v1`. All 35 corrupted foreground receipts are rejected.
The capture uses the unchanged 100% display/text scale and changes no OS settings.
The x64 and ARM64 CI jobs register the capture, audit and negative receipt checks;
push-triggered Actions stay paused, and no workflow was dispatched.

The library and independent-consumer sources match the preceding stable
overflow checkpoint. Experimental decorative footer renderers did not improve
the raw pixels and were removed. The rebuilt library has a new generated WinRT
binary identity, so package and demo evidence is bound to the rebuilt candidate
rather than treating previous binaries as interchangeable.

The local `0.1.0-admin-shell-raster-v14-local` package passes fresh x64/ARM64
consumer builds, all 66 hidden AdminShell ownership checks, and the six related
AppShell, Select, FeedItem, FeedColumn, Slider and ThemeToggle runtime replays.
Both required demo fixtures pass 66 checks and 577 isolation guards on the new
catalog. The 266 portable tests pass; both catalog builds have zero warnings and
the library architectures match all 9,426 method bodies, normalized metadata,
resources and static data. Native ARM64 runtime remains unverified. The new
checkpoint is `artifacts/current/admin-shell-raster-checkpoint.json`; the
112-entry inventory remains 84 started, 28 pending and zero accepted.

## AdminShell horizontal overflow checkpoint, 2026-10-18

The current v10 independent package consumer matches all 312 initial and 16
horizontally scrolled source states with zero geometry error. All 66 ownership
checks and 2,644 isolation guards pass, and all 57 damaged receipts are rejected.
The reference binds 88 complete source PNGs. Caller width bindings, minimum and
maximum constraints, wide native template roots, observer cleanup and remounts
are included. Root callbacks invalidate the private flow when a caller width
changes without changing the clipped desired size reported to its parent.

Actual visible checks in Dark and Light confirm that Tab reveals a fully drawn
far-edge action, Enter and pointer clicks reach its original handler, and header
and sidebar stay in place. Resetting the original width removes the horizontal
range and retains the action result. Ordinary wrapped content has no empty
horizontal range. Six original window captures and matching observations are
saved in `artifacts/current/admin-shell-overflow-visible-v10`.

The earlier v8 prototype passed rectangle/extent checks but failed visible
clipping and introduced empty horizontal space. V9 repaired those visible
failures but reproduced a stale extent of 1,232 while the original bound width
had reached 1,400. Those failed receipts and images remain preserved. The final
flow keeps viewport-sized background/border wrappers separate from wider
content slots and observes original root constraints without replacing bindings.

All six related package runtime replays and the 66-check Monitoring/Editorial
demo replay pass on the same library. Both catalog architectures and fresh
consumers compile cleanly; the library builds match all 9,426 method bodies,
normalized metadata, resources and static data. All 266 portable tests pass.
ARM64 consumer verification is build-only. Full source-raster, interaction,
customization, scaling and native ARM64 runtime acceptance remains open. The
inventory is unchanged at 84 started, 28 pending and zero accepted of 112.
No OS settings, Narrator or Actions runs were used. The wiki remote remains
unavailable; documentation is maintained here.

## AdminShell hidden and initial visible verification, 2026-10-18

The fresh v7 x64 package consumer passes the full source audit: 296 states have
zero measured geometry error and matching checked paint/scroll values. All 52
ownership checks and 2,402 isolation guards pass, and all 49 damaged receipts are
rejected. The footer uses a themed brush with the exact source 8% opacity.
The extended AppShell explicit-zero regression also passes all 44 checks on
that package. Both catalog architectures compile without warnings; all 266
portable tests pass.

The reconnected desktop permits visible tests again. The actual catalog retains
keyboard-edited body text across native maximize/restore and Light/Dark changes.
Tab reveals and focuses the offscreen footer action; Enter and pointer clicks
reach its original handler. Shift+Tab reveals the retained input. Pointer-wheel
scrolling moves the main body/footer while the original header/sidebar stay in
place. Raw window captures and accessibility observations are preserved in
`artifacts/current/admin-shell-visible-v1`; the input attempt before explicitly
focusing search is not counted. This is initial visible evidence, not complete
interaction, source-raster, scaling or native ARM64 acceptance.

The shared viewport-minimum helper suppresses its private visual state whenever
caller locals, bindings or inherited Style setters own the property. WinUI
does not notify when a local zero is cleared to metadata zero. Only loaded
explicit zero overrides observe that boundary on a 16-ms dispatcher tick;
the consumer proves observation stops on unload and resumes on remount.
One-way/two-way BindingExpression identities and zero producer values survive
resize. No caller values, bindings or styles are rewritten.

### Earlier failed diagnostics retained

The frozen AdminShell collector records 296 source states, 56 complete PNGs and
two real source input/action continuity sequences. A fresh x64 NuGet consumer
matches all measured rectangles with zero geometry error. This is a diagnostic:
the full native run fails two of 36 ownership checks, reproducing a caller's
zero-minimum reset in both themes. Thirty-four other ownership, template,
scrolling, metadata and lifecycle checks pass; 2,280 isolation guards pass.
An extended AppShell replay on that same package reproduces the same boundary
in two of 44 checks, with 168 frames captured and 1,641 isolation guards. The
old passing AppShell checkpoint did not cover clearing an explicit zero.

The x64 catalog and fresh package consumer compile; 266 portable tests pass.
The current candidate is unfinished and unaccepted. A LayoutUpdated observer
does not fix the reset. Isolated native probes have ruled out changing a string
DefaultStyleKey and shown that replacing a numeric ThemeResource does not
update an already applied minimum. These are historical failures preserved
alongside the repaired candidate. The final helper uses the narrowly scoped UI
dispatcher observation described above. No OS configuration changes were made.

## Component demo hidden checkpoint, 2026-10-18

The catalog's actual Monitoring and Editorial fixtures pass 66 native checks
across Light and Dark, including real hidden client resizing to an 800-DIP
viewport. Native button/sidebar invocation, chooser selection providers and
actual editing/checkbox properties exercise retained selection, per-column
search, updates, review snapshots/history, keep/discard decisions, filters,
sorting, empty rows, original-page saves, ordered relation rows, validation and
catalog remounts. All 577 isolation guards pass, with no resource or binding
diagnostics. The audit rejects 25 damaged receipts. Eleven new model regression
tests bring the portable suite to 262 passing tests.

Both catalog architectures compile without warnings. Library implementation is
unchanged from the AppShell package checkpoint; its exact x64 assembly identity
is retained, and both architecture builds have the same 9,279 method bodies,
normalized metadata, resources and static data. The existing independent NuGet
consumer evidence applies to that unchanged library; this demo checkpoint does
not claim a new consumer runtime run.

See [demo usage and boundaries](workspace-demos.md). The execution user session
is disconnected, so visible keyboard/pointer, focus behavior, raster comparison,
scaling and native ARM64 runtime acceptance remain open. No window was activated,
shown or focused, and no physical input or OS configuration changes were made.

## AppShell hidden checkpoint, 2026-10-18

The independent NuGet consumer matches all 168 original AppShell rectangles and
checked paint properties with zero geometry error. It resizes its real hidden
XAML client viewport rather than substituting a test viewport value. Forty-four
checks preserve original controls, edits, slot/value/template bindings, inherited
minimum setters, a Main landmark and readable scalar text metadata. They cover
header offset/token changes, zero padding overrides, smaller/larger viewport
minimums, template cleanup and remounts. Isolation passes 1,625 guards, and all
41 damaged receipts are rejected. The frozen reference retains 56 complete PNGs
and actual edited-input/action continuity in both browser themes.

Runtime checks found and corrected a layout cycle from measuring during arrange,
unchanged-slot remounts and a cached private minimum that prevented shrinking
after viewport changes. Scalar slot text is exposed as native content text;
supplied controls and templates retain their own peers. All 251 portable tests
pass. The registered App shells fixture composes the suite selector, input and
feed components using fictional local data. Both full monitoring/editorial demos
remain required and pending.

Exact native raster, real pointer/keyboard/focus, scaling, native ARM64 runtime,
broader positioning/customization and complete runtime integration remain open.
The Windows execution session is still disconnected, so visible tests are not
claimed. No OS settings or pointer heartbeat were changed, and Actions push
triggers remain paused. Inventory is 83/112 started (74.1%), 29 pending and zero
fully accepted. Two-hour status emails remain active; the wiki is unavailable.

## Select and HeaderSelect hidden checkpoint, 2026-10-18

The independent x64 NuGet consumer matches all 292 closed source layouts with
zero geometry error. Checked normal paint matches in 288 cases; four source
autofocus cases have geometry checks while focused paint/input remain deferred.
Eight genuine source request histories match the native selection provider.
Fifty-eight checks retain caller bindings, original option/icon/template models,
metadata (including descriptions), controlled rejection/acceptance, observable
updates and lifecycle state. They also exercise the shared legacy table filter
and pagination recipes without opening a popup. Isolation passes 1,938 guards;
all 48 damaged evidence receipts are rejected. Ninety-two complete original
browser PNGs and 32 open source states are retained for later visible comparison.

Native testing found and fixed long HeaderSelect labels exceeding the 208-DIP
maximum: the private surface now measures against the actual available width.
All 248 portable and eight windowless font tests pass. Both catalog builds and
fresh package consumer builds pass; 9,171 complete method bodies, metadata,
resources and static data match between architectures. These compilation and
hidden checks do not establish native ARM64 runtime, real input/focus, popup
placement/scroll, motion, scaling or exact raster acceptance.

The user authorized desktop tests for October 18. The first desktop launch
attempt failed with `GetCursorPos failed: Access is denied (0x80070005)`;
Windows reports the execution user's session 1 as disconnected. Reconnection
is pending. No visible test is claimed, and no OS settings were changed.
Inventory is 82/112 started (73.2%), 30 pending and zero fully accepted; both
required component demos remain pending. The requested two-hour status email
automation is active. Actions push triggers remain paused. The wiki is still
unavailable, so the API, fixtures and evidence are documented in this repository.

## FeedItem hidden checkpoint, 2026-10-18

The current x64 package matches 100 source layouts and checked paint properties
with zero geometry error. Thirty ownership/lifecycle checks retain original
producer data/bindings, collections, Avatar and source-link instances, request
URLs and command parameters. They cover image failure, rejected/disabled/stale
actions, template cleanup, unload/remount and an unbounded native parent. Four
enabled source requests use the component's guarded peer without focus or input.
The run stayed invisible through 708 guards. All 43 damaged receipts are rejected.

The browser reference binds 64 complete PNGs, 21 timestamp cases across three
locales and actual timestamp tooltip bodies. Native checks inject the reference
clock internally; Windows time/settings are unchanged. An RTL baseline exposed
the physical auto-margin relevance placement, physical-left source link and
1/64-DIP nowrap timestamp allocation. These are corrected; 243 portable tests
and eight windowless native font tests pass. Ordinary shared text consumers
retain their previous measurement mode. The registered Feed items fixture
demonstrates composition with FeedColumn and fictional local metadata.

Both catalog architectures compile without warnings/errors. Exact visible
raster, actual pointer/keyboard/focus/tooltip and motion behavior, real scaling,
native ARM64, full runtime integration, and broader locale/legacy/extended-date
semantics remain open. Inventory is 80/112 started (71.4%), 32 pending and zero
fully accepted; both required component demos remain pending. Actions push
triggers and pointer heartbeat stay paused. The wiki remains unavailable, so
the API and evidence are documented in this repository.

## FeedColumn hidden checkpoint, 2026-10-18

FeedColumn's independent x64 package consumer matches all 154 source rectangles
with zero geometry error and the checked border/dot/count paint properties.
Twenty-eight ownership checks retain original controls, edits, viewport/scroll,
header/accent/name bindings, original template/selector models, and lifecycle
state. The run never showed or activated its window, focused a control, sent
input or changed OS settings; 968 guards verified isolation. Native testing
found and fixed premature template application and vertically centered short
body content. The frozen reference retains 58 original complete PNGs.

All 238 portable tests pass. The Feed columns fixture is registered alongside
the existing composition controls. Both required monitoring/editorial demos
remain pending; this work adds no product services. Visible raster/input,
rounded clipping/shadows/gradient rendering, actual scaling, native ARM64 and
full integration remain unverified. The goal remains active with 79/112 entries
started (70.5%), 33 pending, and zero fully accepted. Push Actions stay paused.

Both x64/ARM64 catalog builds have zero warnings/errors. Fresh x64 and ARM64
consumers restore the same local package; isolated sources/caches are removed.
The compiled library's 8,772 complete method bodies, metadata, resources and
referenced static data match across architectures after separating MVID and
physical RVA differences. The same x64 package replays Slider's 182 layouts,
14 retained-fill states and 36 ownership checks, plus Theme Toggle's 116 layouts
and 42 checks. All 35 deliberately damaged FeedColumn receipts are rejected.
Native ARM64 execution and full catalog/package runtime remain separate gates.

## Current Slider ownership checkpoint, 2026-10-18

The packaged x64 hidden check now matches all 182 initial source layouts and
paint properties with zero numeric error, fourteen retained fill-history states
and 36 ownership/lifecycle checks. Four enabled native range requests verify
accepted/rejected consumer models without focus or physical input. Value,
bounds, step, enabled, name and style bindings survive updates and remounts;
invalid, disabled and detached actions are rejected. Template detachment releases
owned clips, shadows, input and rendering resources. All 44 damaged receipts
are rejected, including a later frame that tries to conceal an initial mismatch.

The baseline reproduced an empty styled-name override being replaced by the
label and a multilingual label measuring 1/64 DIP too wide. Native names now
respect local and inherited-style empty values. Shared plain-text slots use the
combined inline advance already used by ProgressStack, with a new direct Win2D
regression. All 234 portable and seven windowless native font tests pass. The
source reference, original thumb measurements and 44 full PNGs are unchanged.
The catalog adds the multilingual example; its prepared foreground harness also
handles absent header parts and checks style naming boundaries.

The same independently built consumer replays Theme Toggle's 116 exact hidden
layouts and 42 ownership checks. The Slider run stayed invisible through 1,244
guards; neither replay showed/activated a window, focused a control, sent input
or changed OS preferences. These results do not establish visible raster,
physical pointer/keyboard, normal motion, actual scaling, full shared-consumer
integration or native ARM64. Further desktop availability remains pending.
Current receipts are in `artifacts/current/slider-ownership-checkpoint.json`;
FeedGrid remains the last verified foreground checkpoint.

CI reuses the already compiled independent consumer for these hidden checks.
No hosted workflow was dispatched; push runs and the pointer heartbeat remain
paused. Inventory stays at 78 in progress and 34 pending out of 112 (69.6%
started), zero fully accepted, and both required composition demos pending.
The wiki remains unavailable, so repository documentation was updated.

## Previous Theme Toggle ownership checkpoint, 2026-10-18

The native preview now respects caller local/style/`BasedOn` names and tooltips,
including empty names, absent styled tooltips and local null values. Defaults
live in the native palette style and resolve through per-control resources;
clearing local metadata restores style/default values, and sharing a style
keeps each control's mode distinct. Initial disabled state now dims the icon
before interaction. The catalog adds a style-owned metadata example.

A fresh independent x64 NuGet consumer ran a real loaded XAML tree in an
invisible window. All 116 source normal-state layouts match with zero geometry
error. All 42 ownership/lifecycle checks pass in both themes, including binding
and content preservation, shared styles, label/name precedence, empty and null
values, default restoration, template replacement, remounts and disabled/detached
rejection. All 34 corrupted receipts are rejected. The source reference and 134
original PNGs are unchanged; all 234 portable tests pass.

The check remained invisible through 830 guards. It never activated/showed a
window, focused a control, invoked an enabled button, sent input or changed OS
preferences. These results do not prove visible rendering, physical input,
normal motion, scaling, full package/library integration or native ARM64. Those
acceptance gaps remain open; further desktop availability is still pending.
Native foreground Slider and Theme Toggle checks have not run. FeedGrid remains
the last verified foreground checkpoint. Current receipts are in
`artifacts/current/theme-toggle-ownership-checkpoint.json`.

CI uses the already built independent consumer for this hidden check. No
workflow was dispatched, and push-triggered Actions and the pointer heartbeat
remain paused. Inventory stays at 78 in progress and 34 pending out of 112
(69.6% started), zero fully accepted, with both required composition demos
pending. The wiki remains unavailable; repository documentation was updated.

## Previous Theme Toggle background draft, 2026-10-18

The uncommitted draft adds `BleeckerThemeToggle`: Light/Dark/System requests,
native Icon Button sizes/variants, original Lucide geometry, and consumer-owned
mode, metadata and root appearance. The Theme Toggles fixture changes only its
own fictional preview. The original component/provider/Icon Button generate 134
frozen states/full PNGs and twelve real browser click/Space/Enter sequences.
The reference includes current Tailwind 4 gray paint rather than older palette
assumptions. All 234 portable tests pass, including source labels, source cycle,
glyph paths, icon paint and all reference image hashes.

`--smoke-theme-toggle`, a complete raw raster/numeric audit and isolated package
runtime assertions are prepared. These native checks have not run. Source
observations and compilation do not establish native interaction, appearance,
motion, lifecycle, metadata, scaling or ARM64 runtime. The desktop availability
question remains pending; all work continues in the background. Current build
and package compilation receipts are in
`artifacts/current/theme-toggle-draft-checkpoint.json`. Slider is also an
unverified native draft; FeedGrid remains the last verified native checkpoint.
Inventory coverage is 78 in progress and 34 pending out of 112 (69.6% started),
with zero fully accepted entries and both required composition demos pending.
Actions push runs and the pointer heartbeat stay paused. No Narrator or OS
preference changes are used. The public repository wiki remains unavailable;
in-repository usage, architecture and runtime documentation govern this draft.

## Previous Slider background draft, 2026-10-18

The uncommitted library draft adds `BleeckerSlider`, its controlled request API,
native Thumb input, range automation peer, styling and fictional catalog fixture.
All 231 portable tests and six windowless native font tests pass. The new source
reference contains 182 states, 44 unchanged full PNGs, measured browser shadow
thumb boxes, 18 real keyboard sequences and 14 retained-style updates. Decimal
step ties, RTL, PageUp/PageDown, raw versus sanitized values, frozen requests and
invalid fill assignment history match those source observations.

Native foreground checks have not run. The user's desktop window ended at
08:00; further availability was requested while background work continued.
`--smoke-slider`, the source/native audit, corruption checks and independent
package runtime assertions are prepared, with no claimed runtime success.
Native geometry, raster, pointer dragging, keyboard/focus, automation, lifecycle,
normal motion, scaling and ARM64 runtime remain unverified. The current x64
catalog compilation passes without warnings/errors; background architecture
and isolated package compilation receipts are tracked in the draft checkpoint.
The package `0.1.0-slider-local` is a build-only draft. The last verified native
checkpoint remains FeedGrid. Implementation coverage is 77 in progress and
35 pending, with zero fully accepted entries and both composition demos pending.
No Narrator, system preference changes, Actions run or pointer heartbeat was used.

## Current FeedGrid preview, 2026-10-18

The local component-library candidate adds `BleeckerFeedGrid`, preserving empty
auto-fill tracks and original native children. All 227 portable tests pass.
The x64 run matches all 144 frozen source layouts with zero geometry error,
and all 22 complete raw PNGs match exactly: zero differing pixels, masks,
resampling or tolerance. Both themes pass real editing, pointer/Space actions,
723/724 DIP boundary changes, live minimum-width bindings, native collection
moves with zero child unloads and retained focus, visibility, remounts, caller
naming metadata and invalid-value rejection.

The final linear arrangement pass retains the same exact geometry/raster.
Twenty loaded native layout updates average 0.165/0.476/2.159 ms for
128/512/2,048 simple Borders; these exclude rendering and application-specific
child work. Twenty-two corrupted reports/images are rejected. Both catalog
builds and fresh x64 runtime/ARM64 consumer compilation pass, with temporary
sources/caches removed. The focused native and x64 consumer UI finished before
the user's 08:00 desktop window ended; subsequent work is background-only.

The reference covers default/custom minimum width, zero minimum, empty tracks,
variable row heights, fractional widths, RTL, hidden children and explicit
gap/padding overrides. The Feed grids catalog fixture uses fictional local
content. Current package/build receipts are recorded in
`artifacts/current/feed-grid-checkpoint.json`; actual scaling, native ARM64,
broader overrides and complete integration/release acceptance remain open.
Inventory coverage is 76 in progress and 36 pending out of 112; both required
composition demos remain pending. Actions push runs and the pointer heartbeat
remain paused. No Narrator or system preference changes were used.

## Current PanelLayout / PanelColumn preview, 2026-10-18

The local component-library candidate adds horizontal fixed/growing tracks and
vertical fixed/growing columns. All 224 portable and five windowless native
font tests pass. The focused x64 run measures all 86 original source layouts
with zero geometry error, covering nested columns, RTL, fractional sizes,
padding, hidden/empty children and constrained overflow. Both themes pass
native editing, pointer/Space actions, live grow bindings, retained keyboard
focus and zero child unloads across collection moves, template replacement,
remounts, horizontal scroll retention, native Right Arrow scrolling and
unbounded-parent measurement. The catalog registers a fictional composition
fixture, with two additional complete native demo screenshots.

Twenty-three corrupted reports/images are rejected. All 34 original full
reference PNGs and their raw native counterparts are hash-bound and compared
without masks, resampling or pixel tolerance. Eighteen match exactly; sixteen
retain 12,082 differing pixels. Fractional RTL/viewport painting therefore
remains an open visual acceptance item. Failed exact captures are retained as
raw evidence; completing the remaining geometry/input checks does not turn
those failures into visual acceptance.

Current build, package-consumer and Panel integration results are recorded in
`artifacts/current/panel-layout-checkpoint.json`. The package is
`0.1.0-panel-layout-local`. Both catalog builds have zero warnings/errors;
fresh x64 runtime and ARM64 consumer compilation pass, including XAML attached
growth and live binding. Temporary consumer sources/caches are removed. The
current Panel regression again matches all 266 layouts with zero geometry
error; its raw differences remain unchanged. Actual scaling, native ARM64 execution and final
complete catalog/release acceptance remain open. Inventory coverage is 75 in
progress and 37 pending out of 112, with zero fully accepted components and
both required monitoring/editorial demonstrations pending. Actions push runs
and the pointer heartbeat remain paused. No Narrator or system preference
changes were used.

## Current Panel preview, 2026-10-18

The uncommitted component-library candidate adds `BleeckerPanel`: a title/count
header, optional consumer filter/toolbar/handle slots and an independently
scrolling body. All 221 portable and five windowless native font tests pass.
The x64 runtime matches all 266 source layouts with zero geometry error and
matching measured theme/alpha paint. Twenty-nine corrupted reports/images are
rejected. All 58 full raw PNGs are hash-bound to the report and compared without
masks, resampling or pixel tolerance. There are 661,516 differing pixels, so
complete raster parity remains false. Native text and translucent/composed
surfaces still need their final visual acceptance.

Both themes pass real editing, native pointer/Space actions, count bindings,
original template models/selectors, metadata precedence, template replacement,
remounts, retained scrolling across header updates, keyboard PageDown/Home and
unbounded horizontal-parent measurement. A plain overflow viewport is
focusable; editable content retains its own path without an extra viewport Tab
stop. The source border timeline supplies 14 normal-motion samples; actual
native normal-motion verification remains pending on this reduced-motion host.
The tabular glyph bridge preserves kerning, including infinity count widths.
Unchanged slot presenters are retained so header updates keep caller state.

Both catalog architectures build without warnings/errors. Local package
`0.1.0-panel-local`, SHA-256
`277d40f1bf88b9decab7eee41aa94adb43a2d6451bb600a385b7048ad7339b1c`,
passes fresh independent x64 runtime in both themes and ARM64 consumer
compilation after the border-background correction. Both temporary source/
package caches are removed. Field passes its 44-state, zero-error measured
regression and 52 external UIA snapshots. DetailLayout passes all 888 layouts
and 48 exact full PNGs again with the current typography helper. The checkpoint
is `artifacts/current/panel-checkpoint.json`; older checkpoints remain retained.
Complete-catalog replay, hosted CI and native ARM64 runtime are not current for
this package.

Inventory is 74 in progress, 38 pending and zero fully accepted: 66.1% started.
Both monitoring/editorial demonstrations remain required and pending. Full
raster, normal native motion, broader overrides, actual display/text scaling,
native ARM64 execution, PanelLayout growth consumption and final integration/
release acceptance remain open. The goal is active. Push-triggered Actions and
the pointer heartbeat remain paused. No product commit/push, hosted dispatch or
system preference change occurred. The wiki remains unavailable; documentation
is maintained in this repository.

## Earlier DetailLayout preview, 2026-10-18

The uncommitted component-library candidate adds `BleeckerDetailLayout`: retained
main/rail content, Primary/Balanced ratios, logical Start/End placement and
container-bounded sticky positioning. All 217 portable tests pass. The source
matrix contains 888 layouts and 48 original full-frame PNGs; the measured native
matrix has zero geometry error and zero differing RGBA pixels. No masks,
resampling or pixel tolerance are used. The browser host's white top margin and
the opaque box specimen's fractional logical and snapped paint parts are
reproduced in the fixture. The library leaves caller rendering untouched.

Both themes pass real input and pointer/Space actions, main-before-rail physical
Tab order, live viewport changes, retained templates/selectors and original
numeric/Boolean model types, template replacement and remounts. Nested
ScrollViewer and ScrollView checks verify nearest-owner selection, sticky
toggles, owner replacement, detached-owner isolation and container clamping.
All 27 corrupted reports/images are rejected. The physical Tab harness expects
key-down on main and key-up on the newly focused rail; the generic same-focus
key helper remains unchanged. Current Field checks pass all 44 measured states
with zero geometry error and 52 external UIA snapshots. PageFrame passes its
792-state, 40-exact-PNG and both-theme ownership regression with this library.

Both catalog architectures build without warnings/errors. Local package
`0.1.0-detail-layout-local`, SHA-256
`1b3e9c2d43a3cb3600a52d21ca924aa6264da85887d1083a2ef80aaa4bd9aae1`,
passes fresh independent x64 runtime in both themes and ARM64 consumer
compilation. Both temporary source/package caches are removed. The consumers
include retained main/rail inputs, template replacement, named complementary
semantics, live recipes and original numeric/Boolean template values. Native
ARM64 execution remains pending. The checkpoint is
`artifacts/current/detail-layout-checkpoint.json`; older checkpoints are retained.
Historical complete-catalog runs do not verify this package.

Inventory is 73 in progress, 39 pending and zero fully accepted: 65.2% started.
Both monitoring/editorial component demonstrations remain required and pending. Broader overrides,
actual display/text scaling, native ARM64, remaining components and final
integration/release acceptance stay open. The goal remains active. No product
commit/push, hosted workflow dispatch or system preference changes occurred.
Push-triggered Actions and the pointer heartbeat remain paused. The wiki is
unavailable; documentation stays here.

## Earlier PageFrame preview, 2026-10-18

The current uncommitted component-library candidate adds `BleeckerPageFrame`,
four width presets, four horizontal/vertical spacing recipes and native semantic
grouping. All 213 portable tests pass. The focused x64 runtime matches all 792
source layouts with zero measured geometry error. All 40 complete original
source PNGs match every RGBA pixel, including the fractional-host case. No masks,
resampling or pixel tolerance are used. The caller-owned flat filler separates
its fractional logical box and device-snapped paint part to match the browser's
opaque CSS rectangle; the library component leaves caller rendering unchanged.

Both themes pass actual viewport changes of an existing narrow frame, native
editing and pointer/Space actions, binding/style precedence, ClearValue recipe
restoration, template/selector retention, remount, semantic metadata precedence
and invalid-enum rejection. Twenty damaged reports/images are rejected, including
a swapped full screenshot. Failed exact captures retain their actual raw pixels
and still fail; the shared generic nonblank guard remains unchanged.

Both catalog architectures build without warnings/errors. Package
`0.1.0-page-frame-local`, SHA-256
`b1ae323397b7922da0d11dc7db83ed94b540b062fd6a9bed81e060f7651a968a`,
passes fresh independent x64 runtime in both themes and ARM64 consumer compilation.
Both temporary source/package caches are removed. Field runtime and all 44
zero-error measured states pass again with the current library. Its raster
acceptance stays open. The checkpoint is
`artifacts/current/page-frame-checkpoint.json`; prior checkpoints are retained.
Historical complete-catalog results do not verify this package.

Inventory is 72 in progress, 40 pending and zero fully accepted: 64.3% started.
Both monitoring/editorial component demonstrations remain. Broader consumer
overrides, actual scaling, native ARM64 runtime, remaining library components and
final integration/release acceptance remain open. The goal stays active. No
system settings, product commit/push, hosted workflow dispatch or pointer
automation changes occurred. Push-triggered Actions remain paused. The wiki is
unavailable; documentation remains in this repository.

## Earlier Field preview, 2026-10-18

The current uncommitted component-library candidate adds `BleeckerField`:
consumer-owned label/control/action/message composition with retained editor
identity, value, validation and commands. All 210 portable tests and four native
Unicode shaping tests pass. Forty-four source/native states have zero measured
geometry error and matching measured typography/paint, including optional and
action text, required-marker allocation, narrow wrapping and a rich label using
the browser's actual resolved pinned SemiBold face. Raw native marker line boxes,
layout allocations and face metrics remain in the report. Full-frame comparison
retains 137,761 differing pixels; no masks, resampling or pixel tolerance are used.

Both themes pass real label focus, editing and action input. Fifty-two external
UIA3 snapshots verify names, IDs, required/valid state and ordered descriptions.
The native test also verifies binding restoration, template detach/replacement,
unload/remount, child replacement and normal explicit-child text-scale precedence.
Nineteen corrupted reports are rejected. The existing button variant/state,
command, naming, template and theme checks pass with the updated Link text branch.

Both catalog builds pass without warnings or errors. Local package
`0.1.0-field-local`, SHA-256
`700bf7f25c5a493ca2a38050d8586fa1aee15cfc0f2245ce6aa9c25e8a56da47`,
passes a fresh independent x64 consumer in both themes and a separate ARM64
consumer build. The date preset test now selects its actual caption explicitly
because the button template also contains a collapsed Link text branch. Original
date geometry/baseline assertions and native input remain active. Both temporary
consumer sources/caches are removed. The checkpoint is
`artifacts/current/field-checkpoint.json`; current consumer directories are reused.
Earlier full-catalog and Tabs reference results describe their older candidates.

Inventory is 71 in progress, 41 pending and zero fully accepted: 63.4% started.
Both required component demonstration screens remain. Exact raster, broader rich
template/consumer override behavior, actual scaling, native ARM64 execution and
final integration/release acceptance remain open. The goal stays active. System
settings are unchanged; push-triggered Actions and the pointer heartbeat stay
paused. No product commit, push or hosted workflow dispatch occurred. The public
wiki remains unavailable; documentation stays in this repository.

## Earlier Dialog Close Button preview, 2026-10-18

The current uncommitted component-library candidate adds
`BleeckerDialogCloseButton`, with native commands, an adjustable Lucide X,
source hover/press behavior and consumer-owned dismissal. All 205 portable tests
pass, including 110 production motion samples against 20 actual browser
sequences. Sixteen source/native states match measured paint, geometry and icon
properties, with maximum geometry error 0.000003814697265625 DIP. Both themes
pass real pointer release, Space/Enter, exact command parameter identity,
disabled rejection, live theme paint, template replacement and remounts. Actual
old shadow graphs are cleared on template replacement and unload. Eleven corrupted
reports are rejected. Full-frame comparison retains 9,621 differing pixels;
no masks, resampling or pixel tolerance are used.

Both catalog builds pass with zero warnings/errors. The current local package is
`0.1.0-dialog-close-input-local`; its package hash is recorded in the current
checkpoint. The earlier 14-state package/checkpoint is retained separately.
Fresh independent consumers pass native x64 and cross-build ARM64 separately;
both temporary sources/caches are removed. ARM64 execution remains unverified.
The current checkpoint is `artifacts/current/dialog-close-checkpoint.json`.
Tabs runtime and all 44 zero-error geometry/paint cases are rechecked against
the current library; older full-catalog reports remain historical evidence.

Inventory is 70 in progress, 42 pending and zero fully accepted: 62.5% started.
Both required component demonstration screens remain. Exact raster, normal
native motion, broader overrides/scaling, native ARM64 and final release
acceptance remain open. System preferences are unchanged. Push-triggered
Actions and the pointer heartbeat remain paused; no hosted workflow is dispatched.
The public wiki remains unavailable, so documentation stays in this repository.

## Earlier Tabs preview, 2026-10-18

The preceding uncommitted component-library candidate adds `BleeckerTabs` with
three variants, three sizes, consumer-controlled selection and host-routed links.
Its pinned browser reference has 44 rendered cases and four selection sequences.
All 201 portable tests pass. The focused x64 native check passes Light and Dark:
source-rounded stretch tracks, accepted/declined requests, keyed updates,
template replacement, disabled/stale-peer rejection, original command parameters
and CanExecute checks, custom links, real keyboard navigation including Home/End,
RTL, pointer selection and immediate held-pointer displacement. Both x64 and
ARM64 catalog builds pass without warnings or errors. A cold-start font-validation
re-entry failure was fixed without changing asset resolution or hiding errors.

All 44 native/reference cases have zero geometry error and matching measured
paint, padding, border thickness, weight, line height and radius. The verifier
rejects nine deliberately damaged reports. Thirty-six complete source PNGs are
retained and hash-checked. Full-frame pixels still differ with zero masks,
resampling or pixel tolerance; no exact raster acceptance is claimed.

The production Tabs paint clocks match 528 samples from 96 actual browser motion
sequences and 192 interpolation-space mappings. Source colors and shadows use
190ms premium timing; the one-pixel press displacement is immediate. This host
reports animations disabled, so normal native motion presentation remains open.
The extracted CSS shadow renderer preserves the preceding live compositor graphs
across 188 date frames, 4,680 days, 84 triggers and 20 popups. Existing date
geometry, paint, shadow, input and disposal audits pass independently.

Package `0.1.0-tabs-motion-local`, SHA-256
`3db5a80b8a6b1a2523e0a582c3eb6b4506cc8e1f6ecd1788711b4ae51b94e441`,
passes a fresh independent x64 consumer in both themes and a separate ARM64
consumer build. Both helpers confirm cleanup of their temporary sources and
caches. Outputs reuse the current consumer directories. The current checkpoint
is `artifacts/current/tabs-motion-checkpoint.json`; older full-catalog reports describe
earlier package bytes and do not verify the new candidate.

Inventory remains 69 in progress, 43 pending and zero formally accepted, with
both component demonstration screens retained. Exact raster, native normal motion,
broader customization/scaling/lifecycle, native ARM64 runtime and final release
acceptance remain open. No push, hosted workflow or publication occurred.
The [component-library scope](library-scope.md) excludes Narrator and OS contrast
campaigns; their older mentions below are historical. Push-triggered Actions and
the pointer heartbeat remain paused.

## Earlier checkpoints

Source `48d8fd3` passes 177 tests, both catalog builds and the complete x64
sequence: 156 phases and all 1,154 PNGs in 471.9537397 seconds, with 2,040 pointer
gates, 1,778 key actions, 96 clicks and 174 unchanged protected glyph hashes.
Fresh layout/lifecycle/motion/continuity/scalar checks and all negative audits
pass. The four frozen PNG/RGBA cases, 36 opaque and 36 alternating-alpha Windows
comparisons preserve exact bytes. Bounded buffer reuse reduces full GC
collections from 366 to 227 across the same 146-phase prefix. Package
`0.1.0-48d8fd3` passes the isolated x64 consumer in both themes and the ARM64
build; temporary consumers are removed. Every entry in both versioned catalog
ZIPs is byte-verified. Native ARM64 execution and full raster/platform acceptance
remain open. Push-triggered Actions remain paused. See the current checkpoint
for hashes and detailed counts.

Source `5fc9c4e` passes 170 tests and native exact-byte persistence checks. Its
first full replay stopped with a native WinUI stowed exception in dark Sunburst.
Focused Sunburst and the broader chart sequence both passed afterward. One full
repeat passed that transition, then expired at the unchanged 480-second
watchdog after 144 phases. Animation preferences were restored after both runs.
The later bounded blank-frame scan and GC phase diagnostics retain this failure
as historical evidence. This source has no completed release archive.

Source `e5fdbdc` passes all 168 tests, both catalog builds, 292 scalar cases and
the existing layout/lifecycle/motion/continuity audits and corruption checks.
The isolated `0.1.0-e5fdbdc` package passes both x64 themes and the ARM64 build;
temporary consumers are removed. The mouse is available again and the unchanged
Activity Item cursor checks pass both themes. The full replay timed out at its
unchanged 480-second watchdog after 142 phases; animation preferences were
restored. Full native integration and versioned archives remain pending while
capture timing is investigated. Push-triggered Actions remain paused and full
platform/visual acceptance remains open. See the current checkpoint.

Source `36d1c44` passes 168 tests, both catalog builds, all 292 scalar cases and
the existing layout/lifecycle/motion/continuity audits. A native persistence
probe retains four frozen PNG/RGBA cases and 36 byte-for-byte opaque comparisons
with the previous Windows path. Four corruptions are rejected. Fresh package
`0.1.0-36d1c44` passes the isolated x64 consumer in both themes and the ARM64
consumer build; temporary sources, feeds and caches are removed. The full
replay stops at Activity Item's actual Hand-cursor assertion: Windows reports
no mouse and a hidden/null cursor while WinUI selects Hand. Focused replay
reproduces this, with exact owned pointer receipts. The ineffective UI-thread
visibility experiment was removed. Full native integration and archives remain pending. Exact raster and full platform
acceptance remain open, and push-triggered Actions stay paused. See the current
checkpoint for hashes and retained failures.

Source `1eaebd9` passes all 292 scalar/painted-overflow cases with zero geometry
error, including visible last-word ink, preserved mixed-word lines and native
pointer activation beyond the logical header. All 166 tests and all existing
Accordion layout/lifecycle/motion/continuity audits pass. Both catalog
architectures build. The isolated `0.1.0-1eaebd9` package passes the full x64
consumer in both themes, numeric values/names, original negative-zero retention,
whole-word geometry and existing Date regressions; the ARM64 consumer build
passes. Temporary consumers are removed. Two full-suite attempts expired at the
unchanged 480-second watchdog; this source has no completed release archive.
The later capture optimization above retains the fix. Exact raster and full platform acceptance remain open.
Push-triggered Actions stay paused. See the current checkpoint.

Diagnostic source `f2ceffd` passes 32 Accordion collection/content continuity
cases in both themes and real motion preferences using the exact library DLL
from package `0.1.0-ab030df`. Its 244 native samples verify ten retained clocks,
six resets, body growth, actual labels/UIA and keyed lifecycle; height and
float-angle errors are zero. All 20 damaged continuity receipts are rejected.
All 164 unit tests pass, including 224 frozen source samples. Both diagnostic
catalog architectures build; ARM64 execution is unverified. Library source and
public API are unchanged. This extends focused evidence for the existing
package; the full catalog and versioned delivery below remain at `ab030df`.
That iteration's publish directories contained the diagnostic build, while the
retained archives remained immutable. Push-triggered Actions stay paused. See the current
checkpoint for exact scope and provenance.

Source `ab030df` uses 1/64-pixel Accordion reveal heights. The focused x64 capture
passes 48 timelines and six reversal steps under both real Windows motion
preferences, with zero height/float-angle error. Its 1,160 property samples
include 169 intermediate background paints; all 768 frozen browser background
samples and 23 negative audit mutations pass. All 162 unit tests pass.
Both catalog architectures, the isolated x64 package runtime and the ARM64
consumer build pass; temporary source/feed/cache cleanup is verified. The full
x64 replay passes all 156 phases, 1,154 prior captures and 174 protected glyph
hashes in 474.0696258 seconds under the unchanged 480-second watchdog. All input
and delivery/integration audits pass; every entry in both versioned ZIPs matches
publish output. The 60-frame layout audit retains zero geometry error and the
same 951,926 differing source pixels. The first hover-readiness fixture failure is
preserved and corrected by requiring delivered hover plus completed animation.
The versioned delivery below is the previous verified source. Exact motion/static
raster and complete platform acceptance remain open. See the current checkpoint.

Source `5a115e5` passes 60 source/native Accordion layout frames and 108
rows with zero geometry error, plus 44 native lifecycle phases under both themes
and Windows motion preferences. All 159 unit tests pass. Fourteen damaged audit
receipts are rejected; template commands, bound typing and accessible names are
verified. Both catalog architectures and the independent ARM64 consumer build
pass. The complete isolated x64 package consumer passes Light/Dark, including
held/released activation checks and the existing Date regressions. The full x64
catalog passes 156 phases, 1,154 PNGs and all 174 protected chart glyph hashes in
477.986485 seconds under the unchanged 480-second limit. Every entry in both
versioned catalog ZIPs matches publish output. Exact raster, source motion
trajectories and full platform acceptance remain open. Inventory is 65 in
progress, 47 pending and zero accepted. See the current checkpoint.

Source `e73e626` passes 28 source-derived Radio lifecycle cases
and six live reversal steps with 116 native property samples in Light/Dark.
It corrects the four move-direction clock mismatches in the preserved pre-fix
capture, retains visible labels and UIA selection, and restores Windows motion
preferences. All 155 unit tests pass, including 36 frozen reversal samples;
17 damaged continuity receipts are rejected. Its 52 existing motion timelines
also pass with 418 native samples. Both catalog architectures and the independent
ARM64 consumer build pass. The isolated x64 package consumer passes both themes,
including public keyed lifecycle checks and the existing Date regressions.
The full x64 catalog passes all 154 phases, 1,152 prior PNGs and 174 protected
chart glyph hashes in 473.0093278 seconds under the unchanged 480-second limit.
All entries in both versioned catalog ZIPs match publish output. Native ARM64
runtime, exact raster and full platform acceptance remain open. Push-triggered
Actions remain paused. See the current checkpoint for hashes and raw evidence.

Source `9594a5a` corrects Radio Group's interrupted and theme motion, CSS
color-space selection and shared Chromium/Skia color conversion. All 152 unit
tests pass. The enabled/reduced Windows harness passes 52 timelines, 417 native
samples and 206 distinct intermediate ring paints, with exact RGB comparisons,
two retained-clock disable cases and restored preferences. The independent
x64 consumer and ARM64 builds pass. The full x64 replay passes 154 phases,
1,152 PNGs and all 174 protected chart glyph hashes in 473.9255674 seconds under
the unchanged 480-second watchdog. Its first failed pointer-boundary attempt
is preserved; the focused 27-case Funnel replay passes in both themes with
unchanged binaries/gates. See [the current checkpoint](native-validation-checkpoint.md)
and [color conversion notes](css-colors.md). Motion raster, reversal/customization,
native ARM64 runtime and full accessibility/scaling/performance acceptance
remain open. Push-triggered Actions remain paused.

Source `405cea2` adds the controlled Radio Group preview. Both catalog
architectures build and all 149 unit regressions pass. All 56 source/native
frames and 136 options have zero measured geometry error; exact raster and
normal motion remain open. Physical navigation, explicit focus, controlled
requests, UIA, keyed identity, lifecycle and live palette checks pass in both
themes. The complete x64 catalog passes 154 phases and 1,152 PNGs in
473.6823716 seconds under the unchanged 480-second limit, retaining all 1,150
prior captures and 174 protected chart glyph hashes. Package `0.1.0-405cea2`
passes the complete isolated x64 Light/Dark consumer; ARM64 consumer build
passes and temporary caches are removed. Every file in both catalog ZIPs
matches publish output. The [current checkpoint](native-validation-checkpoint.md)
records hashes and exact scope. Inventory is 64 in progress, 48 pending and
zero accepted; two compositions and full Windows platform acceptance remain
open. Push-triggered Actions stay paused by user request.

Source `2c315c3` builds both catalog architectures and passes 144 regressions,
tokens and all 188 focused date frames. The live shadow graph and complete-client
comparison pass with 144 improved shadow frames, eight equal and none regressing.
The audit preserves every source PNG and uses zero masks/resampling/tolerance.
Its 40 popup layers also verify CSS paint order before report sorting. Geometry,
basic paint and all 6,076 typography entries remain verified. Validation source
`c64f5ec` passes all 152 full-catalog phases, 1,150 captures and 174 protected
chart glyph hashes in 471.6649986 seconds under the unchanged 480-second limit.
The Currency Chart fixture observes blur before refocusing; all original
collection, input and lifecycle checks remain. Package `0.1.0-c64f5ec` passes the
complete independent x64 Light/Dark runtime, including date selection/trigger
shadows and popup layer order, and the ARM64 consumer build. Temporary consumers
are removed; package and full-runtime library bytes match. Both catalog ZIPs
are verified file by file; their hashes are in the checkpoint. Exact raster,
motion and platform acceptance remain open. See the current checkpoint.

The date typography fixes at `0758f03` pass both focused native runners at
320/640px in Light/Dark and matches all 6,076 source text entries in 188 frames.
It also retains the existing geometry, basic paint, consumer requests and native
input checks. All 143 unit tests and generated-token checks pass. Source
`0758f03` passes the full x64 catalog: 152 phases and 1,150 captures in
469.5629669 seconds, retaining all prior names and 174 protected chart glyph
hashes. The enable/disable/re-enable regression and native consumer requests
pass in both themes. Both catalog architectures publish, and package
`0.1.0-0758f03` passes the complete independent x64 Light/Dark runtime and an
independent ARM64 build. Packaged typography, eight native date key pairs and
six consumer updates pass. Both consumers' library bytes match the package and
the full-catalog library; both temporary workspaces are removed. Native ARM64
runtime, exact raster/shadows/motion and full Windows platform acceptance remain
open. Current archive/package hashes are in the checkpoint. Historical results
below retain their original source and verification scope.

The date presentation fixes at `c687e27` publish for x64 and ARM64. Native
geometry covers all 188 reference frames: 132 Calendar states and 56 Picker/Range
states with 964 box comparisons, 620 popup days and 132 basic paint checks.
The independent package consumer passes its complete x64 Light/Dark runtime,
including corrected packaged geometry and eight date keyboard pairs; ARM64
builds from the same package. All 142 unit tests pass.
Both consumers' library bytes match the package and their temporary workspaces
are removed. Native ARM64 execution and full visual/platform acceptance remain
open. See [the current checkpoint](native-validation-checkpoint.md) for full
catalog integration, source/package correspondence, artifact hashes and limits.
Historical results below retain their original source and verification scope.

The complete x64 sequence at that source passes 152 phases, 1,150 captures,
2,032 stable pointer gates, 1,764 owned keyboard actions and 92 routed clicks
in 467.1893479 seconds under the unchanged 480-second watchdog. Every previous
capture name and all 174 protected chart glyph hashes are retained. Both Dates
fixtures and their native closing-surface input pairs pass.

## Chart focus ownership repair

Source `4d14c0e` passes the 24-case native Bar/Sankey load/focus regression,
including real event ordering, genuine/stale blur and owned Right input.
The original 12 eager cases' 24 failures and the property-based intermediate's
package failure remain recorded. The current-owner repair passes three cold
runs with consumer IL unchanged, then two fresh x64 NuGet consumer runs in both
themes without temporary per-event focus instrumentation. The complete published
x64 sequence passes 1,104 captures in 434.4379521 seconds, preserving every
prior filename and 174 protected glyph hashes. Both architecture builds, 108
unit tests, token generation and the ARM64 consumer build pass; native ARM64
runtime is unverified. See [the checkpoint](native-validation-checkpoint.md)
for separate failure/success evidence, input audits and artifact hashes.
Visual/platform acceptance remains open; no component is fully accepted.

## Toolbar chrome and caption follow-up

Source `3df888927db4bb7680314b484ec242f20a5d8f60` corrects the toolbar Reset
caption's CSS baseline and the source fixture's original consumer button
chrome/line boxes. An independent frozen-producer probe measures 22.25px in
both themes while preserving the original PNG hashes before/after insertion
of a zero-width baseline marker. The native fixture and isolated package
consumer verify that actual baseline. All 76 local tests, token checks and
x64/ARM64 builds pass; actual source slot label fonts/colors are also checked.

The final published x64 run passes in 349.6342151 seconds at
`artifacts/smoke-data-table-toolbar-caption-published-full`. All 1,020 PNGs,
174 protected glyph hashes, 118 toolbar cases, 1,728 pointer gates (minimum two
stable transitions; maximum 165.9788ms), 1,738 same-target keys, 60 clicks,
eight toolbar Tab transfers, 28 tooltip and 16 final-capture gates pass.
An earlier run failed the unchanged Dark Long Checkbox settle predicate;
additional failure-only diagnostics were added. The later complete runs pass,
but the earlier failure's cause remains unestablished.

Fresh x64 consumer runtime passes both themes, including caption baseline;
fresh ARM64 consumer compilation passes with runtime unverified. Their own
GUID caches are removed. Matching package/catalog hashes and paths are in the
[toolbar reference](data-table-toolbar-reference.md). All 14 exact browser/
native raster pairs still fail (1,056–1,795 pixels, maximum channel delta 227).
Five Dark small-viewport pixels differ at channel delta one between focused
and full native captures. No masks or tolerance are applied, and zero of the
fixed 112 contracts is accepted. Push Actions remain paused by user request.

## Native Data Table Toolbar work

The toolbar preview brings the working inventory to 50 in progress, 62 pending
and zero accepted out of the fixed 112 contracts. All 118 normal source layouts
and actual consumer filtering/reset, ownership, lifecycle, original actions,
disabled inheritance and eight exact native Tab transfers pass in both themes.
All 75 regression tests and token generation checks pass. Exact raster still
fails all 14 compared initial toolbar rectangles; see the
[source contract and remaining acceptance](data-table-toolbar-reference.md).

The first expanded full run reached 973 PNGs before the unchanged 360-second
deadline. Moving PNG conversion/encoding to a worker did not improve timing
and was reverted. A concurrent generic presentation barrier caused a Dark RTL
Brand failure and was also reverted; established live-mutation presentation
ordering is retained. Readiness polling now gives pending UI callbacks one
dispatcher turn bounded by the existing 16ms poll, with identical conditions,
layout checks and deadlines. Only new toolbar source specimens, whose complete
direction/props precede mounting, await the compositor commit alongside two
actual Rendering callbacks within two seconds. Native pointer gates,
final-capture stable transitions, all assertions, captures, motion windows and
full-run deadline remain unchanged.

The expanded full x64 run passes at
`artifacts/smoke-data-table-toolbar-readiness-full` in 350.1131651 seconds.
All 1,004 prior PNGs remain and 16 toolbar PNGs are added (1,020 total).
All 174 protected Sunburst/Radial Bar glyph hashes remain identical. The audit
verifies 1,728 owned pointer gates (minimum two stable transitions, maximum
191.4257ms), 1,738 same-target key press/releases, 60 clicks, 28 tooltip gates,
16 final-capture stability gates and eight exact toolbar Tab transfers without
failures. All 118 normal toolbar source cases have zero geometry/value/row or
presentation-writeback errors. All 75 local tests pass after the final build.

All 14 toolbar rectangles from this full run are pixel-identical to their
corresponding original sequential-wait full-run captures. The earlier focused
captures differ from both full runs by six pixels at channel delta one across
four Light cases; those diagnostics are retained without masks or tolerance.
This comparison establishes full-run raster retention for the scheduling change.
Browser/native exact parity remains failed and no component is accepted.

The final published catalog from source
`274390749b5c8a25685808784074a466b073cb7c` passes the complete x64 run at
`artifacts/smoke-data-table-toolbar-published-full` in 352.0595866 seconds.
The same 1,020 PNGs, all 174 protected glyph hashes, 118 normal toolbar cases,
1,738 same-target keys, 60 clicks, eight toolbar Tab transfers, 28 tooltip gates
and 16 final-capture gates pass. All 1,728 pointer gates have at least two
actual stable transitions; maximum readiness is 265.9897ms, with no failures.
This final run also verifies that search automation names follow the original
placeholder, including an explicitly empty placeholder.

The final toolbar native-raster retention comparison passes 13 of 14 rectangles;
the Light column-filter rectangle has one pixel at channel delta one relative
to the original sequential-wait full capture. Its exact comparison remains
failed, with zero masks/tolerance. Browser/native comparison still fails all
14 pairs (1,216–2,456 pixels, maximum channel delta 242). Neither the scheduling
change nor the published artifacts close visual acceptance.

The final package is
`artifacts/packages-data-table-toolbar-native/Gaulatti.Orchard.0.1.0.nupkg`,
SHA-256 `1d02b8d530ce4b52a9ef0075b0b6ce5de37ce4c224c9dc874dee1e655e251d15`.
Catalog: `artifacts/catalog-data-table-toolbar-native`. The package, catalog,
fresh x64 consumer and fresh ARM64 consumer contain the same Orchard DLL,
SHA-256 `6cadca81414eb6e0c3b79047182197bee8bbaa08bab2c0e4bf9b430a16694247`,
version `0.1.0+274390749b5c8a25685808784074a466b073cb7c`. Fresh x64 consumer
runtime passes both themes; fresh ARM64 consumer compilation passes with
runtime explicitly unverified. Both GUID restore caches are removed. Push
Actions remain paused by user request; no manual dispatch is authorized.

## Previous Sidebar checkpoint

The native Sidebar remains in progress: 49 of the fixed 112 contracts are in
progress, 63 are pending and none is accepted. Its focused native fixture
passes all 82 frozen source cases across Light/Dark plus navigation, original
callbacks, expansion retention, scrolling, templates and lifecycle checks.
See [Sidebar evidence and remaining acceptance](sidebar-reference.md).

Catalog readiness polling now rechecks its condition immediately after
`UpdateLayout`, avoiding a polling sleep when layout already satisfied the
condition. Required Rendering transitions, input receipts, motion sampling
windows and the complete-run 360-second deadline are unchanged. An experiment
using Loaded-event waits exposed a timing-sensitive Pie focus failure in the
full sequence; it was reverted and the existing mount sequence is retained.
Independent Sidebar reference cases reuse a mounted consumer while replacing
its collection through the public API to discard keyed state. A change in
initial collapse mode creates a fresh owner, preserving initial-state behavior
when animations are enabled. Static Field Help captures require the expected
color and scale for three Rendering frames (two stable transitions) within two
seconds. The four 300 ms motion-sampling windows remain unchanged.

Capture encoding uses Windows `SoftwareBitmap` to convert premultiplied BGRA
to straight RGBA, preserving Windows' alpha rounding, then writes a lossless
PNG with faster Deflate compression. Resolution, sRGB and gamma metadata match
the prior Windows encoder. For the same premultiplied inputs, all valid
channel/alpha combinations and all 988 prior full-run images decode identically
through the old and new paths. An independent decoder verifies all 988 PNG
structures and checksums. Compression takes about 2.08 seconds versus 6.04
seconds across that benchmark, with larger encoded files (31.30 MB versus
19.71 MB). No pixels, masks or tolerances change. Boundary regression coverage
includes RGBA scanlines/Zlib, metadata and invalid dimensions/buffer lengths;
all 66 local tests pass. These are verification-harness changes and have no
backend or Prometheus impact.

The complete working-tree x64 run passes in 356.4662745 seconds at
`artifacts/smoke-sidebar-state-gates-full`, under the unchanged 360-second
deadline. All 988 prior PNG names remain, for 1,004 total. All 174 Sunburst and
Radial glyph hashes are unchanged. All 1,720 owned pointer gates have at least
two stable Rendering transitions (maximum 219.4854 ms); 1,732 key actions,
52 click receipts, 28 tooltip gates and 16 settled final captures pass without
failure. Sidebar's two Tab handovers and two cancelled releases retain exact
owned receipts. Audits are `artifacts/sidebar-state-gates-input-report.json`
and `artifacts/sidebar-state-gates-evidence.json`.

The published catalog from `5fcdd1af521779af6b15945926957263d15358ab` also
passes the full run in 356.4127496 seconds at
`artifacts/smoke-sidebar-native-published-full`. It retains the same 1,004 PNGs,
174 unchanged glyph hashes and all input/tooltip/final-capture coverage; its
maximum pointer gate is 214.1027 ms. Audits are
`artifacts/sidebar-native-published-input-report.json` and
`artifacts/sidebar-native-published-evidence.json`. An independent decoder
verifies all 1,004 PNG structures/checksums.

Versioned artifacts are `artifacts/catalog-sidebar-native` and
`artifacts/packages-sidebar-native/Gaulatti.Orchard.0.1.0.nupkg` (package SHA-256
`1ccad0a78427ba92812b14a8bc2a5e6bb8bad97464cbd1e80eb37e7d636eccf3`).
Both contain identical Orchard assemblies, SHA-256
`ca97be7a79ddcbbb3dff22b2bb4b0b82bba4381e5d165c2d07c5ff50f40c6a28`,
and informational version `0.1.0+5fcdd1af521779af6b15945926957263d15358ab`.
The fresh x64 consumer at `artifacts/package-consumer-sidebar-native` passes
both themes; `artifacts/package-consumer-sidebar-native-arm64` passes a fresh
ARM64 build only. Both isolated caches are removed. ARM64 runtime is unverified.
Provenance is `artifacts/sidebar-native-artifact-provenance.json`.

Push-triggered Actions remain paused by user request. Native ARM64, exact
raster, Narrator/contrast, scaling and measured collection acceptance remain
open. This is a verified implementation checkpoint, not full contract acceptance.

## Earlier published checkpoints

The complete published x64 catalog from `a34bb35` passes Light/Dark in
359.572 seconds, under the unchanged 360-second limit, at
`artifacts/smoke-sankey-chart-published-full`. It adds all 76 frozen Sankey
cases and native lifecycle/collection/keyboard checks. All 952 prior PNGs are
retained, for 964 total. All 1,676 pointer presentation gates pass with at least
two stable Rendering transitions and owned final points; actual native event
receipts replace fixed post-input waits. Scroll compositor checks detect five
stale bounds before movement. All 44 click release gates and 28 tooltip opening
gates pass, with no failed deliveries. Sixteen settled final captures remain.

Final x64 and ARM64 builds have zero warnings; all fifty regressions and
generated tokens pass. The fresh regular `0.1.0` package from `a34bb35`, SHA-256
`d48769634ee5e4133ba489c14f5ea802d465b0f8ae0a74268d18465d0f205ce1`,
passes the isolated x64 runtime consumer and ARM64 build-only consumer; both
temporary caches are removed. Native ARM64 remains unverified. See
[Sankey evidence and limits](sankey-chart-reference.md). Whole chart viewport
comparisons retain raster differences without excluded pixels. Inventory is
46 in progress, 66 pending and zero accepted of 112. Push-triggered Actions
remain paused by user request. The full run has only 0.428 seconds of timing
headroom; full raster/motion, contrast, Narrator, scaling and measured collection
performance remain open.

The preceding Funnel checkpoint follows for comparison.

The local published x64 catalog from `648dd18` passes the complete Light/Dark
run in 354.689 seconds at `artifacts/smoke-funnel-chart-published-full`, under
the unchanged 360-second limit. It adds 54 frozen Funnel cases and native
data/input/lifecycle checks while preserving every previous fixture check and
all 940 previous PNGs. There are 952 PNGs, sixteen settled final capture reports
and 1,146 owned pointer gates, with zero failed gates and at least two unchanged
Rendering transitions. The slowest pointer gate is 144.164ms. All 28 tooltip
opening gates also settle within their two-second limit, at most 87.843ms.
Final x64/ARM64 builds have zero warnings; 46 regressions and generated tokens
pass. A fresh regular `0.1.0` NuGet package from `648dd18`, SHA-256
`125605d46bbb85c5a1f100136519a95eb880aae061da5bf780f59a0c8998d7cd`,
passes the isolated x64 runtime consumer and ARM64 build-only consumer; both
temporary caches are removed. Consumer test setup at `efc5ba2` resets its
formatter between themes. See [Funnel evidence](funnel-chart-reference.md).
Push-triggered Actions remain paused, and these local results do not replace
the unresolved hosted native ARM64 checks or full platform/visual acceptance.
Inventory remains 45 in progress, 67 pending and zero accepted of 112.

The readiness comparison retains whole PNGs and does not mask differences.
All 122 Checkbox font PNGs match the previous decoded pixels exactly; all
twelve compared font/reading-direction metric reports are unchanged. Across
166 compared PNGs, 127 are identical and 39 Input/Switch captures have raster
differences, including fixture-button glyphs and a one-pixel Switch alpha
difference. Full raster acceptance remains open. Details are recorded in
`artifacts/funnel-published-readiness-comparison.json`.

Run [38597371241](https://github.com/gaulatti/orchard/actions/runs/38597371241)
at `17b0e1b` passes the complete x64 job, ARM64 cross-build, both focused native
Section Header checks, contrast and both fresh consumers. Native ARM64 fails
focused Input at a rounded exterior point (actual AppStarting, expected Arrow,
publisher inactive) and the full catalog's forced Checkbox transaction setup
(owner inactive). The two failures are distinct from a pending blocked cursor.
Verified ARM64 archive `11910643289` SHA-256:
`9945d1d0e4085c061eead3b062dbe1e19586727689f3a0c49c5015ac64f0a7f8`.
That run remains red. Push-triggered Actions are now temporarily disabled at
the user's request; local checks and unverified hosted gaps must be reported
separately during the pause.

Run [38593563145](https://github.com/gaulatti/orchard/actions/runs/38593563145)
at `9303b06` passes the entire native ARM64 job, with an ARM64 process/OS,
display/text scale 1 and actual animations enabled. Both new producer-phase
cancellations record one cancellation and zero No deliveries, with visible
Arrow and detached observers. Both forced transactions record one delivery,
one acknowledgement, two producer callbacks, actual UniversalNo/visible flag
and no pending publication. The fresh native ARM64 consumer passes with package
SHA-256 `f66a3d478979dcad4ee25c31cabb6f56be9fec13c9d9b2892271b7b5bfed38e4`,
and its own cache cleanup succeeds. Verified archive `11910030536` SHA-256:
`a68c8aed669b393f1948f0e16bb2eba839acb48961e31cd5623627078d9ad00e`.

The same run's x64 job passes focused checks, contrast and the fresh consumer
but fails full Light contextual-help/Breadcrumb hover. The physical move chose
the expansion button's center at y=646, then its real bounds moved from
y=634.5 to y=592.5. The final pointer remains at y=646, below that target. The
popup does not occlude it and the catalog owns foreground. This is evidence of
stale target geometry, not evidence that pointer delivery reached the button.
Verified x64 archive `11908797521` SHA-256:
`124253a22d5a204ad27dbf68ecb2b22143d5324a3b2f204c7d0b45a26c1f98e5`.
Overall CI remains red; no inventory entry is accepted.

Run [38588481432](https://github.com/gaulatti/orchard/actions/runs/38588481432)
at `38426be` passes the complete x64 job and ARM64 cross-build. The Attention
geometry wait passes the full hosted x64 sequence. Native ARM64 passes every
focused check, contrast and the fresh package consumer, but fails the full
Dark Checkbox phase `hit-testing-repeat-restored-6`. Border, alpha and ownership
are correct; Windows displays Arrow 65539 while the SDK source retains
UniversalNo. The pending transaction spans 97 rendering callbacks and correctly
records no new acknowledgement. This differs from a replaced SDK selection;
the remaining native materialization failure is still open.
Verified ARM64 archive SHA-256:
`acce72fba3a6e9a45e25f5dd2644a9803eb2eb4617671a2d7c014bfea2036cfb`.
Overall CI remains red and no inventory entry is accepted.

Run [38584983610](https://github.com/gaulatti/orchard/actions/runs/38584983610)
at `d68a732` passes the entire native ARM64 job with animations enabled. Both
focused and full Input matrices retain 96 passing samples per theme. Each
forced cursor transaction records one delivery, one acknowledgement, actual
visible UniversalNo 65559, and no pending observer. Cancellation records one
cancellation and zero deliveries. The Dark shared publisher records two SDK
producer replacements. The native package consumer and cleanup pass.
Verified ARM64 archive SHA-256:
`77cf99ad89ee36a8db75b97406ac6893c247f773a288617333cf8782ac5d2b5b`.

x64 passes its focused checks, contrast and package consumption, but its full
Dark Attention sequence sees heading geometry still translated by -2706.75px
after the viewport offset reports zero. The verified x64 archive SHA-256 is
`4c4006c456123962da018b90f7b4b240a5a5c550cbcac473f3769a8f3f3cda49`.
The geometry wait now uses the existing Attention settling bound before input;
this follow-up still requires hosted validation. Overall CI is not green yet.
The follow-up passes the focused Attention fixture, both solution builds, all
42 regressions and the complete local x64 Light/Dark sequence at
`artifacts/smoke-attention-geometry-full`. It changes catalog preparation only;
the library and previously verified package remain unchanged.

The next local runtime candidate passes both solution builds, all 42 regressions,
the focused 192-sample Input matrix, focused Attention viewport returns, and
the complete x64 Light/Dark catalog at `artifacts/smoke-runtime-settlement-full`.
Deferred cursor acknowledgement now records actual visible UniversalNo after
owner revalidation, while handling SDK producer changes within the same pending
transaction. Both forced transactions complete/acknowledge exactly once; their
native snapshots show handle 65559 and flag 1 with no pending frame observer.
The Attention fixture explicitly resets its registered viewport before returning
to its first-row heading. Hosted verification remains required.
The fresh x64 NuGet consumer passes both themes at
`artifacts/package-consumer-runtime-settlement` and confirms isolated workspace
cleanup. Package 0.1.0 SHA-256 is
`e8cc7b0a238e027920418bd13138b0e5d52a2ab005cb8d8042f30314765e54cc`.
The fresh ARM64 cross-build consumes that same package and confirms cleanup at
`artifacts/package-consumer-runtime-settlement-arm64`. It explicitly reports
`runtimeVerified: false` on this x64 machine.

Run [38578657500](https://github.com/gaulatti/orchard/actions/runs/38578657500)
at `faab41e` passes both builds, all 42 regressions, focused Page Header checks,
Windows contrast and both fresh package consumers. Enabled ARM64 Bauhaus passes
focused Light/Dark and full-suite Light with maximum per-frame errors below
0.000153 degrees. The verified runtime archive SHA-256 is
`cd5210b2fe18ee1c289b8283e9216b782da2868df4b340a468941aaab350cd0b`;
its consumer cleanup reports `removed: true`.

Required CI remains red. ARM64's focused Input fails the Large invalid
bottom-right 0,3 boundary: border/opacity match, but Windows displays Arrow
65539 instead of UniversalNo 65559 after 50 completed publications. Its full
suite later fails Line Chart's `explicit-transparent` category-indicator check.
x64's full suite rejects an off-screen Attention heading at 350,-1950 after a
bring-into-view request. These are separate unresolved runtime boundaries;
the verified clock correction does not close them or accept any inventory entry.

Run [38576042972](https://github.com/gaulatti/orchard/actions/runs/38576042972)
at `f72666b` passes the complete x64 job, the ARM64 cross-build and both fresh
native package consumers. Both focused Page Header suites and Windows contrast
checks pass. ARM64's complete catalog fails the Bauhaus delivery-clock timing
comparison. Its verified frame timestamps identify a correct 18 degrees/second
rotation across 395.6032ms of frame time versus 324.7343ms of callback time.
See [the clock correction and unchanged limits](bauhaus-background-reference.md).
Main is not fully green at this checkpoint; no inventory entry is accepted.

The Page Header checkpoint passes both local solution builds, all 38
unit/resource tests, its sixteen-case focused native gate, and the complete
x64 Light/Dark catalog at `artifacts/smoke-page-header-full`. This host uses
display/text scale 1 and the actual reduced-motion preference. Its fresh
independent x64 NuGet consumer passes in both themes at
`artifacts/package-consumer-page-header`, and its isolated workspace cleanup
reports `removed: true`. Package SHA-256 is
`d5b5f552e399505d598fd9c1407619ef174fd6f0400e7821e770e3d1d4a1ce61`.
The foreground trace confirms a real owned-window transition from Terminal
after transient Alt preparation in 39.5ms. The initial rejected acquisition
was retained during investigation; no ownership assertion was removed.
The fresh ARM64 consumer cross-build also passes with the identical package
hash and confirms cleanup. It explicitly reports `runtimeVerified: false` on
this x64 host; native ARM64 verification belongs to the hosted runtime job.
Broader Page Header raster, text balancing, contrast/Narrator/scaling and hosted
platform acceptance remain open, as documented in its source contract.

The subsequent diagnostic run
[38568587791](https://github.com/gaulatti/orchard/actions/runs/38568587791) passes
the x64 job but fails ARM64's complete catalog because its physical Attention
child-action target no longer belongs to the catalog at the sampled screen
point. Run [38569920476](https://github.com/gaulatti/orchard/actions/runs/38569920476)
passes both ARM64 jobs, but x64 fails Error State contrast at 242/243 established
glyph pixels and a rapid disabled Input remount cursor check. Package consumers
pass in both runs. These failures remain open; main is not fully green at this
checkpoint. No cursor, pixel-mask or deadline assertion has been relaxed.

Run [38567209211](https://github.com/gaulatti/orchard/actions/runs/38567209211)
at `64f4f25` passes every required job: both complete catalogs, both live Windows
contrast suites and all independent consumers. Native ARM64 retains enabled
animations and verifies the new cursor settlement/cancellation boundary in
both themes. Its verified archive SHA-256 is
`f8ba882068d43a9d56740c71a3c0cf3998565ddb643abd3b7cdd08e0108fec51`.
The previous intermittent glyph and motion failures remain unexplained; this
passing checkpoint does not close their full acceptance matrix. The subsequent
frame-timestamp diagnostics have separate hosted evidence above. Inventory
completion is still unproven: 32 entries are in progress and 80 are pending.

Run [38558580437](https://github.com/gaulatti/orchard/actions/runs/38558580437)
at `7920215` passes the full x64 job, ARM64 build and both independent package
consumers. Both focused Input suites and Error State contrast checks pass.
The complete ARM64 catalog still fails disabled cursor publication on the Large
invalid Input at bottom-right 3,0: Windows displays Arrow after 136 completed
deferred publications, with three cancellations and one publication pending.
Ownership, hover, border paint and opacity remain correct. This is a failed
required runtime gate; the existing displayed-cursor assertion remains intact.

The passing x64 contrast archive has SHA-256
`7940371dfde28b562ddf187193b49d2cc73ef562ccb38ac1c7a20b39f209b342`.
It uses the same 243-pixel description mask and geometry as failed run
`38556522194`: pixel 218,9 is now white rather than RGB 246,246,246. This confirms
the earlier contrast failure is intermittent, without establishing its cause
or accepting a changed mask, crop or tolerance. No inventory entry is accepted.

Run [38545127689](https://github.com/gaulatti/orchard/actions/runs/38545127689)
at `a236ab7` verifies the Switch metadata correction with a passing x64 job and
ARM64 build. The native ARM64 job fails Input cursor restoration and a Checkbox
disabled-pointer phase, while its real Windows contrast transition and fresh
package consumer pass. Main is therefore not fully green at this checkpoint.
These failures remain open and are not evidence that the cursor issue is fixed.

Run [38533153948](https://github.com/gaulatti/orchard/actions/runs/38533153948)
at `45c011d` passes all required jobs, including both complete native catalogs,
both real Windows contrast transitions and both independent consumers. ARM64
retains enabled animations; x64 reports animations disabled. The tooltip
placement correction now has hosted runtime evidence. No intermittent cursor
cause is established by this passing run, and no complete component acceptance
is claimed. Error State implementation and the Switch metadata correction
described in their component docs are subsequent changes.

The contrast fixture now places its open contextual help to the right so
small viewports do not flip it over Breadcrumb's pointer target. The original
fixture reproduces the hosted failure locally at a 1024x745 client size in
`artifacts/smoke-contrast-entry-occlusion-before`: the tooltip covers the target
center and CommonStates stays Normal. The corrected focused native entry
passes Light/Dark in `artifacts/smoke-contrast-entry-unobscured`, recording no
occlusion and PointerOver, followed by physical Escape and retained keyboard
focus. The actual contrast check keeps its exact semantic colors, popup
retention and restoration requirements, and now explicitly checks occlusion.
Hosted mode-changing verification remains pending. This changes catalog
verification only; the library's tooltip default and backend/metrics behavior
are unchanged.
Both solution builds are clean, all 38 regressions pass at
`artifacts/tests-contrast-entry`, token generation matches its pinned source,
and the complete local x64 catalog passes both themes at
`artifacts/smoke-contrast-entry-full`. The unchanged package retains its
previous independently restored consumer evidence; no new library packaging
contract is introduced by this catalog-only correction.

Run [38530280838](https://github.com/gaulatti/orchard/actions/runs/38530280838)
at `645189c` passes both builds, 38 regressions, the complete x64 catalog and
both independent package consumers. ARM64's focused Input and full catalog
still fail real global cursor checks, and both contrast jobs still fail
Breadcrumb hover. The consumer foreground correction has passed its hosted
runtime gate; required CI and inventory acceptance remain incomplete.

The independent consumer's physical chart input now performs one explicit
activation of its own visible process-owned window before keyboard focus.
It calls Window.Activate and SetForegroundWindow once, then requires the
actual foreground HWND to match within two seconds. Each subsequent native
key retains its immediate foreground guard; no later loss is recovered or
ignored. `consumer-input-{theme}.json` records before/after HWNDs, process IDs,
window classes, visibility, activation result and elapsed time without window
titles or content. Microsoft's [windowing guidance](https://learn.microsoft.com/en-us/windows/apps/develop/ui/windowing-overview)
states that Window.Activate does not force a background app into foreground;
[SetForegroundWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
is subject to Windows foreground restrictions, which this fixture preserves.

The fresh isolated x64 consumer passes both themes at
`artifacts/package-consumer-foreground-owned`, including real keys and owned
activation records. The fresh independent ARM64 consumer compiles/publishes
at `artifacts/package-consumer-foreground-arm64`; its result explicitly leaves
native runtime unverified on this x64 host. Both use unchanged package SHA-256
`becbbfa4b759f068a7c582d0f7521473ba6227ca1fcf1dd0344442ce39bebc22`.
All 38 regressions pass at `artifacts/tests-consumer-foreground`. This changes
consumer verification only; no library or backend/metrics behavior changes.
Run `38530280838` verifies hosted native ARM64 acquisition and consumer runtime.
Its retained archive `artifacts/ci-645189c-arm64.zip` matches published SHA-256
`9119834e2f9b8376fb76787763e5399f8081cfbe4e694354976468d34ea32e9e`.
Selected evidence in `artifacts/ci-645189c-arm64-evidence` records the consumer's
own visible window before/after activation in both themes, `acquired=true`,
and passing native ARM64 runtime. This successful run began with the window
already in foreground; it does not prove activation after foreign ownership.

Run [38527036318](https://github.com/gaulatti/orchard/actions/runs/38527036318)
at `00734d0` passes both builds, 38 regressions, both focused notification
presenter/lifecycle suites, both focused Attention suites, the full x64 catalog
and the x64 package consumer. Both actual contrast jobs retain the subsequent
Breadcrumb Highlight/HighlightText hover failure. ARM64's full catalog fails
Checkbox's disabled-pointer `moved-back` phase. Its independent consumer again
loses foreground during actual Line Chart keyboard input. These remain open
required gates; neither overall CI nor the component inventory is complete.

The next Breadcrumb correction observes in-place Color/Opacity changes on the
actual palette brush producers. The old renderer fails the real hovered
ellipsis mutation at `artifacts/smoke-breadcrumb-mutable-before`; the correction
passes both themes at `artifacts/smoke-breadcrumb-mutable-fixed`, including
opaque, half-alpha and restored source colors. Both builds are clean and all
38 regressions pass at `artifacts/tests-breadcrumb-mutable`. Hosted contrast
correction remains unverified. New contrast diagnostics record palette values,
rendered values, visual states and native pointer ownership without changing
the semantic-color assertion or deadline.
The new package's fresh isolated x64 consumer passes both themes at
`artifacts/package-consumer-breadcrumb-mutable`, including public foreground
brush mutation and disabled-state preservation, for package SHA-256
`becbbfa4b759f068a7c582d0f7521473ba6227ca1fcf1dd0344442ce39bebc22`.
Build/restore and native execution were separated to avoid measuring UI during
lossless compression of earlier task-owned artifacts. The restored package
hash was rechecked before native launch.
The complete local x64 catalog passes both themes at
`artifacts/smoke-breadcrumb-mutable-all`. Local scale is 1, animations are
disabled and contrast is off. This verifies the mounted mutation correction
and existing interactions, not the hosted semantic hover-color gate.

Run [38525069103](https://github.com/gaulatti/orchard/actions/runs/38525069103)
at `90fdad6` passes both builds, 38 regressions, both focused Attention and
notification suites and the complete x64 catalog. Both actual contrast jobs
now pass Attention's established glyph mask and fail later at Breadcrumb's
hover Highlight/HighlightText pairing. The retained native ARM64 artifact at
`artifacts/ci-90fdad6-arm64.zip` has verified SHA-256
`b64aa2440069e72df248511b24bb1dbfd9b925331d70852d3039f62564b92f56`.
Selected evidence at `artifacts/ci-90fdad6-arm64-evidence` records 124/124
Attention glyph pixels, zero unmatched pixels, and whole origins Y=429/392.
ARM64's complete catalog still fails Input's Small/valid bottom-right `(3,0)`
global Arrow/UniversalNo mismatch. Its package consumer fails foreground
ownership during actual Line Chart input; the new Attention scrolling check
has already passed. The x64 package consumer passes. These are separate open
gates, and no component or overall CI is accepted as complete.

The catalog now resolves notification presenters through the owned popup
content, shares Opened/readiness within the existing five-second deadline,
and records lifecycle/foreground events without cancelling or reopening.
The focused native x64 suite passes both themes at
`artifacts/smoke-notifications-owned-presenter`; the complete local x64 catalog
passes both themes at `artifacts/smoke-notifications-owned-all`. Both solution
builds are clean and all 38 regressions pass at
`artifacts/tests-notification-presenter`. This checkpoint changes catalog
verification only; hosted recurrence remains unverified until the new run.

The Attention viewport regression reproduces the hosted 46/124 glyph mismatch
locally when scrolling by 36.5px, with Windows contrast off. Quantizing native
ascent before the baseline margin calculation passes the unchanged mask across
seven transitions in both themes at `artifacts/smoke-attention-scroll-native-ascent`.
Evidence for the original failed probe is at `artifacts/smoke-attention-scroll-before`;
the ineffective visual-snapping probe is at `artifacts/smoke-attention-scroll-snapped`.
Both solution builds are clean and all 38 regressions pass at
`artifacts/tests-attention-scroll`. Hosted contrast correction is still unverified.
The independent x64 package consumer passes both themes at
`artifacts/package-consumer-attention-scroll`, including its new consumer-owned
ScrollViewer check, for package SHA-256
`c95c07878b02225b91711050b08068743083d81e5900c49c9e3a0a58e58f757f`.
Its repaired Loaded-boundary fixture was republished in the originally fresh
isolated consumer while retaining the hash-verified package/cache.
The complete local x64 catalog passes both themes at
`artifacts/smoke-attention-scroll-all`, including all seven Attention glyph
transitions. Local scale is 1, animations are disabled and contrast is off;
this is not evidence of passing the hosted mode-changing gate.

Run [38522487971](https://github.com/gaulatti/orchard/actions/runs/38522487971)
at `69cdc87` passes both builds, 38 regressions, the focused Line Chart interaction
on both native architectures, the full x64 catalog and both independent package
consumers. The ARM64 focused chart now passes the zero-data tooltip edge check
as well as the effects and source interaction sequence. Both contrast jobs
retain the 46/124 Attention failure. ARM64 also fails Input's Medium/valid
top-right `(3,0)` and the full catalog's Large/invalid top-left `(3,0)` cursor
publication with global Arrow rather than UniversalNo. Its focused notification
fixture throws `Sequence contains no elements` while inspecting the popup.
These remain open required gates; no inventory component becomes accepted.

The retained Line Chart interaction checkpoint passes all 38 regressions,
both solution builds without warnings, and focused native x64 inspection in
Light/Dark at `artifacts/smoke-line-interaction-native`. The fixture exercises
36 source-captured focus, arrow, Enter, blur/refocus, pointer and data-shrink
outcomes, alongside 22 chart and 26 tooltip specimens and unchanged effect
pixel checks. The fresh independent x64 NuGet consumer passes actual keyboard
hide/reopen and retained selection across blur/refocus in both themes at
`artifacts/package-consumer-line-interaction`. Package SHA-256 is
`302b2e59e1e7d60349a09fc2c83816c6d8b352780d2830ce3b125069d04385a8`.
Local scale is 1, animations are disabled and contrast is off. Native ARM64
interaction, actual contrast, complete chart acceptance and required CI remain
open.
The complete local x64 catalog also passes both themes at
`artifacts/smoke-line-interaction-all`, using the final shared two-second
presentation deadline and the existing geometry/pixel tolerances.

Run [38519659390](https://github.com/gaulatti/orchard/actions/runs/38519659390)
at `81a352b` passes both solution builds and independent package consumers.
The x64 focused chart and full catalog pass. The ARM64 chart passes the unchanged
shadow pixel profile, then fails the zero-data tooltip's left edge placement.
ARM64 also fails Input's Large/invalid top-left `(3,0)` cursor publication and
loses foreground during the full catalog's Alert pointer action. Both contrast
jobs still fail Attention at 46/124 established glyph pixels: moving baseline
correction into layout did not resolve the live contrast transition.
The ARM64 artifact is retained at `artifacts/ci-81a352b-arm64.zip`, verified
against SHA-256 `ee2a75832d753b670add881107acc56ccce7aeadf92604395e2d691c2a51b0f9`.
Selected evidence is at `artifacts/ci-81a352b-arm64-evidence`. Required CI remains
failing and no inventory component becomes accepted.

The next renderer candidates place Attention's baseline in layout and give the
chart shadow a Composition-owned vector mask. Both solution builds, 37
regressions, focused Attention glyph transitions, focused chart/effect pixel
checks, the complete local x64 catalog and a fresh independent x64 package
consumer pass. Evidence is retained at `artifacts/tests-render-corrections`,
`artifacts/smoke-attention-layout-glyphs`, `artifacts/smoke-line-tooltip-vector-mask`,
`artifacts/smoke-render-corrections-all` and
`artifacts/package-consumer-render-corrections`. Local scale is 1, animations
are disabled and contrast is off. The hosted contrast and unpainted-shadow
corrections remain unverified; no required gate or inventory entry is accepted.

Run [38517156846](https://github.com/gaulatti/orchard/actions/runs/38517156846)
at `e0db53c` passes both builds and package consumers but fails the focused
tooltip shadow profile on both hosted architectures. The native ARM64 artifact
is retained with verified SHA-256
`918ad22f258bdfe3a3dfa1c587711efdfa747ad49f35e664ca0735a10156422b` at
`artifacts/ci-e0db53c-arm64.zip`; its focused and full chart shadow samples all
remain background-colored. Both contrast jobs still fail Attention at 46/124.
ARM64 also fails Input's Large/valid bottom-right `(3,0)` with global Arrow
instead of UniversalNo, and a notification mark-all popup check. These remain
open required gates.

The tooltip checkpoint passes both updated solution builds without warnings,
37 regressions, 22 chart and 26 tooltip source specimens, sampled real
blur/shadow profiles in both themes, a fresh independent x64 package consumer
with actual keyboard input, and the complete local x64 catalog. Evidence is at
`artifacts/tests-line-tooltip-final`, `artifacts/smoke-line-tooltip-mask`,
`artifacts/package-consumer-line-tooltip` and `artifacts/smoke-line-tooltip-full`.
Display/text scale is 1, animations are disabled and contrast is off. Native
ARM64 tooltip execution, complete raster/motion/lifecycle and accessibility
acceptance remain open; the inventory stays in progress.

Run [38513468894](https://github.com/gaulatti/orchard/actions/runs/38513468894)
at `9e74f40` passes both builds and the focused Line Chart source cases on x64
and native ARM64. The full ARM64 catalog times out while invoking/closing a
Menu item, before reaching Line Chart. Both contrast checks retain the 46/124
Attention glyph failure. Focused chart success does not establish passing
overall CI or complete chart/inventory acceptance.

The public Line Chart checkpoint passes both solution builds without warnings,
36 unit regressions, all 22 source-backed native chart specimens including
series/axis paint, the complete local x64 catalog and an independently
restored/published x64 package consumer in both themes. Evidence is retained in
`artifacts/tests-line-chart-final`, `artifacts/smoke-line-chart-paint`,
`artifacts/smoke-line-chart-full` and `artifacts/package-consumer-line-chart`.
Local display/text scale is 1, animations are disabled and contrast is off.
Native ARM64 chart execution and the remaining visual/motion/accessibility
boundaries are unverified; no inventory entry becomes accepted from this run.

Run [38510904185](https://github.com/gaulatti/orchard/actions/runs/38510904185)
at `c63e7c2` passes both builds and the frozen-curve unit checkpoint. The full
x64 catalog passes; the full native ARM64 catalog fails Checkbox's
`hit-testing-repeat-restored-7` disabled-pointer phase. Both contrast checks
still fail Attention at 46/124 established glyph positions. The cursor and
contrast gates remain open; this run does not establish passing overall CI.

Run [38509625229](https://github.com/gaulatti/orchard/actions/runs/38509625229)
at `6f5e41d` passes both builds, all 32 regressions, both complete native
catalogs and both independent native package consumers. The focused chart-theme
steps exercise the new automatic/explicit/transparent series and real bindings
on x64 and native ARM64; ARM64 retains enabled animations. The current run does
not reproduce the prior cursor failure, which remains an intermittent open
boundary. Both required contrast checks still fail Attention at 46/124 exact
foreground glyph positions. Passing catalogs and package consumers therefore
do not establish passing overall CI or complete inventory acceptance.

Run [38507683990](https://github.com/gaulatti/orchard/actions/runs/38507683990)
at `cf94a93` passes both builds, all 32 regressions, focused Attention with the
new painted/action bounds and far-edge activation on both native architectures,
and both native independent package consumers. The full x64 catalog passes.
Its focused Input step fails a Small/valid top-left `(0,0)` restoration:
GetCursorInfo reports AppStarting while Arrow is required. Full ARM64 reaches
and passes the updated Breadcrumb checks, then fails Input's Medium/invalid
bottom-left `(3,0)` with matching paint and opacity but global Arrow instead of
UniversalNo. Both contrast jobs retain 46/124 Attention glyph positions.
The native sizing correction and template-readiness change do not resolve
those separate cursor/contrast gates. No required check is weakened or accepted
by this checkpoint.

Run [38506082985](https://github.com/gaulatti/orchard/actions/runs/38506082985)
at `c7fdad4` passes both builds, all 32 regressions, the new focused chart-theme
fixture on x64 and native ARM64, and both independent native package consumers.
The full x64 catalog passes. The full ARM64 catalog stops at Breadcrumb's
custom-template `ConsumerLink` lookup before its named child materializes.
Both contrast jobs retain the 46/124 Attention glyph failure. Verified ARM64
snapshots show unchanged natural text metrics and UseLayoutRounding=false
throughout the owned ancestor tree; see [contrast diagnostics](contrast-verification.md).
The native archive SHA-256 is
`fda55ecdcb6e9003581ba9bcf765975a967ff7f98ec0d9ebbbf48fd860545c34`.
This remains a failing required CI checkpoint, with no inventory acceptance.

Run [38502493745](https://github.com/gaulatti/orchard/actions/runs/38502493745)
at `4fde530` passes both builds, all 31 unit/resource regressions, the focused
Bauhaus/Input/Activity/Attention/Notifications checks on both native
architectures, and the full x64 catalog. The full ARM64 catalog fails the
disabled-checkbox `hit-testing-repeat-restored-6` pointer phase: native paint
and SDK publication match, but GetCursorInfo still reports Arrow after 147
republish calls. Both live contrast checks fail Attention's established glyph
mask at 46/124. Stable capture poses rule out movement during PNG encoding;
the one-pixel positioning defect remains open. These failures prevent a
passing-current-CI claim or component acceptance.

Run [38498921210](https://github.com/gaulatti/orchard/actions/runs/38498921210)
at `3f4c0e7` passes both builds, the full x64 catalog and both independent native
package consumers. Focused Notifications passes on x64 and ARM64, including the
new inherited/local owner-theme assertions; ARM64 uses enabled animations.
Focused Bauhaus/Activity/Attention also pass on both architectures.

The required checkpoint remains red. ARM64 Input's Large/invalid top-left
`(3,0)` has matching hover paint/opacity and an owned UniversalNo publisher, but
the visible cursor is Arrow after 77 republish calls. Its full suite fails the
disabled Checkbox `shown` phase. Both live contrast jobs fail the composed
Attention glyph check at 46/124 exact foreground positions.

The native composed Attention image confirms a one-pixel vertical displacement:
all 124 established positions match exact Windows foreground at offset (0,1).
This offset is diagnostic only; the pre-transition mask is not shifted to make
the gate pass. Font, dimensions and scale are unchanged, and the label's local
transform changes by half a pixel. The display/crop/baseline positioning path
must be traced and verified before the contrast gate can be accepted.

ARM64 archive `11865550209` is verified against SHA-256
`18efbd5173ef21f8a465833a13a3cac64d6d01d92db5179df0e05db86e86811e`;
selected reports, notification captures and contrast images are at
`artifacts/ci-3f4c0e7-arm64-evidence`. Its passing native consumer uses package
0.1.0 SHA-256
`e2504fb85d0b0e56e624224a63f0453bbb87eba74fdace783121c8bdf449954d`.

Run [38496131766](https://github.com/gaulatti/orchard/actions/runs/38496131766)
at `0b60f37` passes both builds, 28 unit/resource tests, the full x64 catalog,
both native package consumers, and focused Bauhaus/Input/Activity/Attention on
both native architectures. Attention's focused ARM64 run uses enabled animations
and verifies its initial source-layout/action/command/lifecycle/interior-pixel
checks; it does not close full raster, motion, contrast, scaling or Narrator.
Native ARM64's full suite fails Checkbox's `hit-testing-repeat-restored-5`, and
both contrast jobs retain the 38/46 Attention isolated-ink failure.

The verified ARM64 archive `11864352182` has SHA-256
`81af82c674c5fd5c7f10a9269ca0e7e3cef6dae6fb4328d6e41e1bb1948d677c`;
selected reports and ink images are at `artifacts/ci-0b60f37-arm64-evidence`.
Its native package consumer passes with 0.1.0 SHA-256
`10efe2dfb780bf61fcd14721b8fbc22000fc44ec82c1070917dbb848b95063ba`.
The new ink diagnostics establish a changing local text transform with stable
displayed baseline phase; the next fixture compares composed screen glyphs,
retaining the pre-transition mask and exact system foreground requirement.
The full required checkpoint remains red.

## Attention verification checkpoint

Run [38494432792](https://github.com/gaulatti/orchard/actions/runs/38494432792)
at `384b54b` passes both architecture builds, all 28 unit/resource tests,
the x64 full catalog and both independent native package consumers. The ARM64
cross-build consumer also passes. Focused Bauhaus passes on native ARM64 with
animations enabled: Light measures 6.5227375 degrees against 5.8510152 expected,
and Dark 5.9910774 against 5.9880096. Both retain the existing one-degree bound;
this does not close the full visual/motion matrix.

Native ARM64 remains failed. Focused Input's Small/valid bottom-right `(0,0)`
release has correct resting paint, no hover/owner, and Arrow in the SDK producer,
but GetCursorInfo displays AppStarting while the catalog owns foreground and
the screen point. The full suite fails an open notification popup's Dark-to-Light
change: the requested theme is Light while the presenter and surface remain
Dark. These are required failures, not accepted platform substitutions.

The new Attention contrast check fails identically on x64 and ARM64: 38 of 46
pre-transition established glyph pixels become exact Windows foreground pixels.
The native surface/background, border/text brushes, gradient removal and
lift/shadow suppression pass before that glyph assertion. The original mode and
producer image/border/foreground restore. The next diagnostic fixture retains
exact before/after label rasters, point-level mismatches and layout/font/scale
metadata without changing the ink mask, expected color or timeout. Attention
also receives a focused native CI step so earlier full-suite failures cannot
prevent its architecture-specific layout/action/lifecycle checks from running.

ARM64 evidence archive `11862903921` is verified against SHA-256
`2ed438477f376e55549396f84ca42bbd2603a4b442f57cd269418b6780b9ef61`;
selected reports are at `artifacts/ci-384b54b-arm64-evidence`. Its passing native
consumer uses package 0.1.0 SHA-256
`574bf839d955dbd3d390e35088e727c86f4b4b6ce207cdc4d0473205cd5b66bd`.
The complete required CI checkpoint remains red.

## Recent native input checkpoint

Run [38486835832](https://github.com/gaulatti/orchard/actions/runs/38486835832)
at `387e6f4` passes x64's full job and the ARM64 cross-build. Native ARM64's
first attempt passes focused Bauhaus/Input/Activity, live contrast and the
independent consumer. The full suite stops in Dark Alert's cancelled held
press: a runner-owned `wsl` ConsoleWindowClass takes foreground while the
catalog still owns the screen point. Its strict guard refuses the release;
no input gate is relaxed. Light Field Help's settled brush mutation checks pass
with animations enabled. The archive was verified against SHA-256
`b7c30941946fbfb928ada21b5821d7b8470ad586ac748fddd7151b2711127350`,
with selected reports at `artifacts/ci-387e6f4-arm64-evidence`.

The same failed ARM64 job was retried on a fresh host with unchanged source.
Attempt 2 reproduces the actual cursor defect: focused Small/valid bottom-right
`(3,0)` owns its target and UniversalNo publisher but globally displays Arrow,
with 55 republish calls, 164 block calls and 3 restores at the final snapshot.
The full suite also fails Dark Checkbox's `hit-testing-repeat-restored-6`.
Activity, Bauhaus, contrast and native package consumption pass. These failures
remain required acceptance gaps; the retry does not establish a passing shared
checkpoint or complete enabled-motion Field Help acceptance in both themes.

Run [38485003280](https://github.com/gaulatti/orchard/actions/runs/38485003280)
at `1cf7c76` passes x64's complete job and the ARM64 cross-build. Native ARM64
passes focused Bauhaus, Input, Activity Item, live contrast and independent
package consumption. Activity's native title/time ink extents match the frozen
browser in both appearances at display/text scale 1 with animations enabled.
The ordinary ARM64 catalog fails Field Help's in-place hover-brush assertion;
this candidate does not establish a passing shared checkpoint or resolve the
intermittent cursor/motion gaps. The independent ARM64 package has SHA-256
`6fca1df2ea6fc2efef266ef735b0cacdaf2bc9edeada8912582a2a3edc9a8f1d`.

ARM64 artifact `11859541782` was verified against SHA-256
`d7b9d79de708622f3fcb4b98e2ae3ac8e59a2d69664223788d20efdf01b75e9e`.
Selected reports are retained at `artifacts/ci-1cf7c76-arm64-evidence`.
Field Help's fixture now awaits the actual owned animation clock's release and
active source subscription before asserting a settled in-place brush mutation.
It records clock/source/state diagnostics and owns foreground input throughout;
fresh enabled-motion ARM64 verification of that correction is required.

Run [38472918618](https://github.com/gaulatti/orchard/actions/runs/38472918618)
at `6a48813` passes all three required jobs, the complete x64/ARM64 catalog,
live contrast and independent consumers. The registered inventory still has
26 in-progress components and no fully accepted component at that checkpoint.

Run [38473604856](https://github.com/gaulatti/orchard/actions/runs/38473604856)
at `c8018ba` adds a focused Input run ahead of the full catalog. ARM64's focused
Input and Bauhaus checks pass with animations enabled. x64's focused Input
live-theme check encounters the correct hovered sand/70% border while requiring
the resting border; the full x64 catalog, contrast and consumer pass. The
fixture correction at `764f5b2` positions the physical pointer on the heading
before this palette assertion; production behavior is unchanged.

The same candidate's full ARM64 catalog fails Checkbox's Dark stationary
`hit-testing-repeat-restored-5` phase. Foreground and the screen point are owned,
hover/border/opacity match, and the SDK cursor is UniversalNo, but the global
visible cursor remains Arrow throughout the two-second trace. The final
publisher sample records 8,481 refreshes, 2,940 block calls, 285 republish calls
and 129 restores, retained producer Arrow and an active blocked owner. These
cumulative counts do not establish the root cause or prove when republishing
occurred during that failure. The exact visible-cursor gate remains required.
ARM64 contrast and the independent consumer still pass.

The ARM64 archive `11852889880` was verified against SHA-256
`403f6760f4226dfcc9a76d288a2a62628ed0b2b792c43512ef76c8618d3507eb`.
Selected reports are retained at `artifacts/ci-c8018ba-arm64-evidence`.
This failed candidate does not replace the passing checkpoint.

Run `38475433445` at `764f5b2` passes both architectures' focused Bauhaus,
ARM64 focused Input, live contrast and independent consumers. x64's focused
palette check still fails with the hovered border after moving to Heading
before invalid-editor focus. The recorded initial point is owned, but final
control bounds are absent, so this does not prove a resting point after the
focus-induced layout. The next fixture checks move again after focus and add
those diagnostics. Full x64 passes; full ARM64 loses foreground before a
Bauhaus composed capture, whose strict guard refuses it. The next capture
requires the shared foreground session immediately beforehand.

Checkbox cursor mismatch polls now record the bounded publisher diagnostics
beside each global cursor sample. A failure can show changes in refresh,
block, republish and restore counts during its own wait, rather than relying
on a final cumulative total. This is catalog-only observation; it changes no
cursor publication behavior, ownership condition, expected handle or timeout.

Run `38477265194` at `64a2b27` passes both ordinary full catalogs, focused
Activity Item and independent consumers, plus ARM64 cross-build. Activity Item
records disabled animations on x64 and enabled animations on native ARM64.
Both jobs fail the new contrast selector's duplicate `PART_Surface` name
(the row and its nested Avatar); its correction selects the row's direct root.
x64 focused Input still paints hover with a demonstrably external pointer;
ARM64 focused Input owns its Small/invalid top-right `(3,0)` target and
publishes UniversalNo but displays Arrow. These required gates remain red.
The passing ordinary suite does not replace the failed focused boundaries.

Run `38480092499` at `5ad9d0b` passes both live contrast checks, including
Activity Item's semantic hover/restoration, and both independent consumers.
x64's ordinary full catalog passes. Both focused Input jobs expose the stale
enabled hover flag with native CommonStates Normal and an external pointer.
The local explicit 1024×697 viewport reproduces it, allowing a geometry-based
Input correction and stationary-overlay regression to be exercised before CI.
The ARM64 ordinary suite also fails its Dark Bauhaus motion check: measured
counter-rotation 6.8230476° versus 5.4163998° from a 300.9109ms wall-clock span.
This mismatch remains open; its existing tolerance is not widened.
The ARM64 artifact `11855619263` was checked against SHA-256
`2a191a2b648bd2b6721bebe73abea7f63f4e67f3a6f44b0e1fa9648269c895a0`,
with selected reports at `artifacts/ci-5ad9d0b-arm64-evidence`.

Run `38482502505` at `28f3efe` passes the x64 job, including focused Input,
the complete catalog, contrast and package consumption. The geometry-based
Input hover correction gets through ARM64's live-theme assertions too, but
ARM64's focused Large/valid bottom-right `(0,3)` target still globally displays
Arrow despite an owned UniversalNo publisher and matching hover/opacity.
Its ordinary catalog fails Checkbox's `hit-testing-repeat-restored-6` boundary.
The retained-producer value transition is therefore not a demonstrated remedy
for the ARM64 cursor defect. Both contrast checks, ARM64 consumer and
cross-build pass. The strong displayed-cursor assertions remain required.

## Live Windows contrast checkpoint

Run [38434880055](https://github.com/gaulatti/orchard/actions/runs/38434880055)
at `b6ca38d` passes all three required jobs, both ordinary native catalogs and
both independent package consumers. Real contrast switching/restoration passes
on x64 and ARM64. Rendered primary-action background and all 154 established
label ink pixels match Windows Highlight/HighlightText. The x64 system transition
cover causes ordinary window deactivation; the fixture waits for its owned
target to be released, then verifies real keyboard re-entry into the same help
control. See [contrast evidence and limits](contrast-verification.md).

This is a representative live transition regression. Full four-scheme contrast,
every component/state, Narrator and scaling remain unaccepted.

## Breadcrumb press and lifecycle checkpoint

Run [38414003860](https://github.com/gaulatti/orchard/actions/runs/38414003860)
at `7c4c272` passes all three jobs: x64 build/catalog/package consumer, ARM64
cross-build/consumer build, and native ARM64 catalog/package consumer. All 26
unit tests pass. The registered baseline now covers 23 in-progress inventory
entries; 89 remain pending and none has full acceptance.

The downloaded reports verify X64/X64 and Arm64/Arm64 process/OS architectures.
Both use .NET 10.0.12 and display/text scale 1. x64 runs Windows 10.0.26100 with
animations disabled; ARM64 runs Windows 10.0.26200 with animations enabled.
Breadcrumb's actual held press, cancelled release, keyboard expansion/focus and
expanded unload/reload checks pass in both themes. The ARM64 pressed/reloaded
captures were inspected. This proves their registered endpoint/lifecycle checks,
not full raster, animation-curve, contrast, Narrator or scaling acceptance.

| Candidate evidence | SHA-256 |
| --- | --- |
| x64 runtime archive `11823338685` | `dfc9ead7a667020eede0d7b24afb1c2660303f5fbb0dbdfcd1dd675ba7995265` |
| ARM64 runtime archive `11824046064` | `4f5f863205d7c37cb01e0e7aacb757cd94d19e5599f7336ea59c89b368ab4965` |
| x64 consumer package 0.1.0 | `92188c41bc1f92b2d87296ee43bc7b7c684525deff6a0eb7405423233b2e3016` |
| ARM64 consumer package 0.1.0 | `ed55dedc49dbc737b5084fc5859f974699d039835b37bdf189d22ea10e2df907` |

Archive digests were verified before extraction; selected reports/captures are
retained at `artifacts/ci-checkbox-7c4c272-{x64,arm64}`. Subsequent fixture changes
need their own native evidence.

## Local physical-input preconditions

The subsequent local catalog setup fixes fractional Input sample origins, gives
Checkbox's stationary scroll test a 500px maximum viewport, and places the
pointer on the Button fixture heading before asserting its non-hover state.
The complete x64 catalog passes both themes and system appearance under
`artifacts/smoke-all-local-preconditions`, with a 3440×1369 client, display/text
scale 1 and animations disabled. Each theme passes all 96 Input rounded-pointer
samples and Checkbox's stationary scroll-away/back checks. Local x64/ARM64 builds
have zero warnings; all 26 unit tests pass. These setup changes require their own
fresh CI runtime/consumer results before replacing the checkpoint above.

## Earlier Avatar baseline

The catalog fixtures and independent NuGet consumer at commit `2f265e3` passed
on both x64 and native ARM64, including Avatar's consumer images, collection
bindings, lifecycle and retained overflow elevation. The same run replays
Skeleton's rendered pixels, live consumer opacity and ancestor visibility, and
the stronger global visible-cursor gate. This runtime baseline covers 22
in-progress inventory entries; 90 remain pending.
The earlier `7024b9e` and `5412997` checkpoints covered 20 and 18 entries.
The Input cursor failure observed at `61d13aa` did not recur, but its cause
remains unresolved. Further implementation changes require their own native checks.
No component has full visual, input, accessibility and scaling acceptance.

## Avatar startup boundary

Run `38364251212` at `c7df5e5` reproduces the ARM64 cursor mismatch while
Orchard owns foreground: Light Small disabled invalid Input, bottom-left (3,0),
publishes UniversalNo and owns the rounded hit but globally displays Arrow.
The foreground guard therefore does not resolve the product publication problem.
The publisher now writes its owned cursor on each shared observation rather than
skipping the SDK setter when the getter retains the same object. No Win32 cursor
write or test-side cursor override is added. A passing native run is still required.

Run `38359503487` at `d280c79` passes the x64 catalog and independent consumer,
but fails ARM64's Light Medium disabled Input at included corner (3,0).
Its checked artifact `11794259493` has SHA-256
`d2fa7519acfbe29e311c528f7eeccfe6fa516c4f540d58c31a0b221d7d0f7b8c`.
The final physical move records Orchard in foreground. During the one-second
cursor wait, a same-session WSL ConsoleWindowClass takes foreground while the
pointer remains over Orchard's DesktopChildSiteBridge. The 41 mismatch samples
show global Arrow, UI-thread Wait, and published UniversalNo; all three values
are now independently identified. Handle 65561 is AppStarting on this runner.
This proves foreground ownership was lost during the wait; it does not prove
a product cursor defect or establish that every earlier failure has this cause.

Rounded Input cursor waits now check foreground ownership before and after their
conditions and reuse NativeInputSession's existing single recovery for foreign
runner windows over the still-owned target. Recovery only reactivates Orchard;
it does not move the pointer, set a cursor, retry the fixture or change any
expected value. A second foreground loss or unsuccessful recovery still fails.
The global displayed-cursor, border, opacity and focus criteria remain required.
This harness correction requires a fresh passing native run on each architecture.

The subsequent docs-only run `38349885199` at `54a370e` passes the ARM64
catalog and independent consumer, but x64 fails Small disabled invalid Input's
excluded (0,0) corner in Light with visible handle 65561 and published Arrow.
The recurrence now spans both themes. Rounded-pointer reports additionally retain
the active sample's one-second mismatch timeline, thread/global cursor handles,
showing flags, system-shape handle mappings and native hit/foreground windows.
These diagnostics distinguish a foreign cursor, a thread selection and a displayed
cursor; acceptance still requires the actual global cursor without retries or
fixture-driven cursor writes. No product cursor correction is claimed yet.

### Passing Avatar checkpoint

Run `38344790556`, commit `2f265e355f5eb34f7b317fcc794959688463ab2b`, passes
all three jobs, both complete catalogs and both independent package consumers.
Both runtime archives were downloaded and their SHA-256 digests verified:

| Architecture | Artifact | Archive SHA-256 | Independent consumer package SHA-256 |
| --- | --- | --- | --- |
| x64 | `11787634602` | `9b974e4d235ee5ae2c76ceefe54656338cf5478533ecdbe7e690e9cefda101c6` | `4421c3c3f3f5370122180b19607c053c475d429ce4f2f34d5c176e0801094c29` |
| ARM64 | `11787644833` | `a8c4a5a1d49d63dc0b0c502d798d45038e1e3eff5a5f737af63b9b2a73405d05` | `979d6c9598c12e958a464fc77e75999409778754cfdab3bf8ded1b4fd0a7baed` |

Each architecture/theme retains ten element images, two composited window images,
ten size/ink samples and 40 checked image/corner pixels. Consumer image cropping,
sticky failure, Unicode code units, shared collection replacement, item changes,
index identity, negative Max and detached template replacement pass. The retained
overflow circle keeps both shadow layers after collection additions. The clean
package consumer loads the independent Avatar/Group/Data templates, fonts and
Unicode mappings in both appearances. x64 runs Windows 26100; ARM64 runs 26200.

The inspected source/native composition images show the same tested geometry,
but remain qualitative JPEG/PNG comparisons. Typography/rasterization, exact
shadow output, contrast/Narrator and display/text scaling still require complete
acceptance; no inventory entry is marked complete.

### Earlier failures and fixes

The measurement-only run `38347993940` at `953e847` passes Avatar in both
themes on x64 and ARM64, then fails the previously tracked Input cursor gate in
Dark. x64 Small disabled Input's excluded (0,0) corner publishes Arrow while
the visible cursor is 65561 instead of 65539. ARM64's included (3,0) corner
publishes UniversalNo while the visible cursor remains Arrow (65539 instead of
65559). Styling and tracker ownership match their expected states. This is a
recurrence of the unresolved global cursor problem, not an accepted cursor fix.
The complete passing baseline remains `2f265e3`; this run does not reach native
package-consumer verification.

Both failed-run archives were downloaded and SHA-256 verified: x64 artifact
`11789768781`, digest `dad25c3b257bbc8fc6fb69e1cf204f45f6b438bddd57f21f1e952bb423ca1178`;
ARM64 artifact `11788814143`, digest
`1e9eacd90a8a0b3b83782521b73ed6a02e24a90b8b70903529c8fdc6896d702b`.
Avatar's retained text boxes have source/native width differences of
-.001875/-.011624/-.005750/-.015499/-.009624 logical pixels from xs through xl,
identical on both architectures and in both themes. All line-box heights match
15/16/20/24/28. These measurements establish advance and line-box agreement
within browser layout quantization; glyph baselines and rasterization remain open.

Run `38338501970`, commit `2571db7a43363f72999fa2f8f99f492fea6ce7f2`, passes
both solution builds, 25 tests and the independent ARM64 consumer build, but
both native catalogs fail XAML startup: AvatarsFixture references undeclared
`BleeckerLabelStyle`. Neither reaches Avatar runtime verification or the native
package consumer. The fixture now uses the existing body style and the new
resource-reference regression test checks catalog Bleecker keys against declared
library/catalog resources. The corrected local build and all 26 tests pass;
native verification of the correction is pending.

Run `38339035639` at `4ae9b66` passes both builds, all 26 tests and the ARM64
consumer build. Native startup gets past resource lookup but fails assigning
`BleeckerAvatarGroup.Avatars` on both architectures. Avatar still has no native
runtime acceptance. Smoke startup reports now retain bounded managed Avatar
setter/metadata exceptions beneath generic XAML parse errors to identify the
collection assignment boundary.

Run `38339805040` at `401a63d` confirms the original managed error in both
startup reports: the AvatarGroup dependency-property callback rejects the value
XAML assigns as an observable collection. The catalog still does not launch.
The group now declares Avatars as its XAML content property and the catalog and
consumer populate it with direct AvatarData children, using the existing
notification collection pattern. A new native check replaces the consumer
collection and requires every bound group to receive the same instance. Fallback
checks also require actual initials/silhouette ink. The corrected local build,
26 tests and token drift check pass; these new native checks remain pending.

Run `38340953532` at `059fe94` identifies the rejected startup value as null,
with the catalog still failing before mounting fixtures. The control now permits
an unresolved null binding only while unmounted and validates the required
collection at load; direct null assignment remains an immediate error. No default
collection replaces a missing binding. The native regression also requires a
rejected direct null assignment to leave consumer data intact. A fresh native
run is required to verify this binding-initialization boundary.

Run `38341800804` at `b842608` gets past XAML parsing, then fails required-data
validation when an AvatarGroup loads with an unresolved collection. No empty
data replaces the missing source. Smoke diagnostics now retain the binding's
element/path/source type and bounded WinUI binding errors so the next run can
identify the unresolved collection boundary.

Run `38342620751` at `3a55430` confirms an unnamed group's ElementName source
is still null at its Loaded callback (`DefaultGroup`, path `Avatars`). Both native
catalogs fail required-data validation; compilation and consumer builds pass.
The mount validation now runs on the next dispatcher turn after Loaded so WinUI
can complete binding evaluation. It still requires actual consumer data and does
not substitute a collection. Fresh native verification remains required.

Run `38343424439` at `0eda50a` passes startup on both architectures and reaches
Avatar's own fixture. Deferred mount validation resolves the initialization
boundary in that run. The first ring check then fails; the assertion now uses a
one-millionth opacity tolerance and reports actual size, ring thickness, margin,
color and opacity. This preserves the branded ring gate and distinguishes
numeric representation from an appearance mismatch. Native verification is pending.

Run `38344071421` at `96e6599` passes the ring checks on both architectures and
reaches live AvatarGroup collection updates. Both then fail the retained overflow
shadow check after adding an item. Synchronize was detaching/reinserting overflow
on every update. It now keeps that child in place while overflow stays positive,
inserts newly visible avatars before it, and removes it only when overflow ends.
The regression requires retained overflow identity, loaded state and both shadow
layers after the count update. The corrected local build and 26 tests pass;
native verification of the lifecycle fix remains pending.

## Skeleton runtime checkpoint

Run `38334259867`, commit `71a7fb8c0757df34c7dd563b776378a95933f0fe`, passes
all three jobs, both complete catalogs and both clean package consumers.
The checked runtime archives and consumer packages are:

| Architecture | Artifact | Archive SHA-256 | Independent consumer package SHA-256 |
| --- | --- | --- | --- |
| x64 | `11784128612` | `7f8228abbfa72e3ee9388ded32a994c06ccdcce08db1b1f9e1b10de74063d858` | `0e94e67bf4ddc8d54d5a728fbbd0aaa787c270ad56de8aa5e60fea7c1b9e298b` |
| ARM64 | `11784223774` | `75d06f960e5a12f31253db57205a0c3f85f9b0bff4b6a9d59d2db64363a786b5` | `eeb69dd424e6defaf8dddea89f61db1d5481a8a68392ea41c6a22ac68f984949` |

Both Skeleton themes preserve the consumer opacity binding and pass ancestor
hide/show, unloading, updates while unloaded and detached template replacement.
With normal motion on ARM64, the .75 endpoint ranges approximately .5002–.7496
in Light and .5004–.7497 in Dark. The .25 endpoint ranges .2503–.4997 and
.2515–.4987. Reduced-motion x64 remains at the exact supplied endpoint. Both
retain six element/four window captures, 29 branded center-pixel checks and
12,800 verified empty canvas pixels per theme. This proves the tested endpoint
ranges and lifecycle, not exact normal pulse timing or full visual acceptance.

ARM64 now displays Arrow at Medium Input's excluded (2,0) corner in both valid
and invalid states. The island publishes Arrow, the disabled tracker has no
cursor ownership/hover, and the hit path is the surrounding Grid/Border/scroll
container. This passed observation does not identify or fix the earlier
intermittent cursor failure. The stronger gate and failure diagnostics remain.

### Earlier checkpoints

Run `38332404342`, commit `291fee51ea52290c04bcfd058f7c4ac1a2e350cb`, passes
all three jobs, the complete catalog and both independent package consumers.
For each architecture and theme, Skeleton retains six element images and four
composited window images, verifies 29 actual branded shape-center pixels, and
checks all 12,800 pixels of the transparent zero-column table's allocated region
against the pinned canvas. Geometry, count bindings, negative count values,
decorative peers, constrained widths and lifecycle checks pass. x64 has reduced
motion; ARM64 has normal motion. Both use 100% display/text scale.

Both runtime archives were downloaded and SHA-256 verified:

| Architecture | Artifact | Archive SHA-256 | Independent consumer package SHA-256 |
| --- | --- | --- | --- |
| x64 | `11783359207` | `0eb3658ccfbe6fdfdaf85961835bc9dac6bfe67c3bdd02462dd3def76b9e47da` | `8e3b31ecf84552dd504766f1d61f99171819a4e9b522f8c44e9b0aab4c8d3535` |
| ARM64 | `11784125403` | `34dbc80b93f4b097d61d9717ce164aa339b925c622a51037db336b71d590ed4d` | `1d6d98c7c555483c48f1f9bb0d9cdb82440fb188d5ff61f2b5736b6a764a01e3` |

This checkpoint predates the control-level pulse and consumer opacity endpoints
in `61d13aa`. Its run `38333238719` passes x64 catalog/consumer and ARM64 build.
The checked x64 archive `11783653162` has SHA-256
`9b2f7af325ad04c7a089e07f0d8053af321ba6b5f9a10d3d905b6d1b2f89f10f`;
the consumer package has SHA-256
`1722e369589a962a98b188c57ec9a96dddcd92df883582352786eef1733a487c`.
Both x64 themes retain live .75/.25 bindings and static reduced-motion endpoints,
and pass ancestor hide/show, unload, binding updates and template replacement.
That run did not reach normal-motion consumer endpoint verification on ARM64;
the later checkpoint above supplies it.

ARM64's checked failure archive `11784001632` has SHA-256
`b68d6c25cc3215925f82f4b8e84aa8b98ba8865d1dc38136c8c7d07a43ad4731`.
Its Medium Input at excluded top-left point (2,0) displays cursor handle 65561
instead of expected Arrow 65539. The rendered border, 60% opacity, unfocused
state and exact owned pointer position match. It fails before Skeleton and the
package consumer. Rounded Input reports now retain the island's selected cursor,
tracker ownership/hover and visual hit path to distinguish a publisher error
from an override error. Those diagnostics do not replace the displayed-cursor
acceptance gate. Exact browser raster parity, full pulse timing, Narrator,
Windows contrast and scaling remain open.

Cursor evidence from earlier physical fixtures uses the UI thread's GetCursor
handle. That does not independently establish the global displayed cursor;
Microsoft documents GetCursorInfo for that observation. Current Checkbox and
Input transition diagnostics now retain both, including global visibility and
screen position. No visible-cursor acceptance follows from earlier thread-only
handle assertions. Rendered hover borders/opacity are independent observations.

At `706ee10`, ARM64's global cursor report proves the enabled Input displays
IBeam while its UI thread reports Arrow. It also proves the disabled Checkbox
still displays Arrow while its UI thread reports UniversalNo. Archive
`11780820812` was downloaded and verified, SHA-256
`6f75a65e7a63d3981ed16ad35774da3037a377d316998ae28764bb8f061337a8`.
The implementation replaces the thread-only override with the root's
InputPointerSource cursor and strengthens both fixtures to require the visible
global handle. This is a new acceptance boundary, not a pass inferred from
earlier cursor properties. The prior `ae3e44f` run passes all three jobs under
the earlier gate; the `7024b9e` run proves the displayed override in both themes.

Run `38329932509`, commit `7024b9e95fefd2e4c74fd8901fb44c02e8938db8`, passes
all three jobs. Both native suites pass 96 Input rounded-pointer phases per
theme and retain twelve composited Input captures per theme. Checkbox reports
contain 60 observations per theme, including immediate detachment diagnostics
and native acceptance phases for rounded bounds, shared cursor ownership,
stationary changes, visibility, clipping, scrolling and remounting. The immediate
null-template diagnostic still sees UniversalNo; the unmounted observation
then sees Arrow, and the restored branded template sees UniversalNo again.

Both downloaded runtime archives were SHA-256 checked:

| Architecture | Artifact | Archive SHA-256 | Independent consumer package SHA-256 |
| --- | --- | --- | --- |
| x64 | `11781299898` | `f427496140adafd1afcb028e1c90ebbb089531c51c593c7e1dfd8d163bf79c2b` | `b50ea8b2ee3a91d1cdb6ab7350fa2924a6cd93ac990a7e51f71cd8e6ffbd2157` |
| ARM64 | `11781594621` | `fc8d50703f9161ee36252efbcc5010bb469a370abd7f942a390e99ec99a9d275` | `61333557bfef216dd8e422739736ab99da56e85aa1624568c64d0d0ec1ce97c1` |

x64 records OS build 26100 with animations disabled; ARM64 records build 26200
with animations enabled. Both have 100% display/text scale. All 25 unit/resource
checks pass. Twelve paired composited Input crops (each size, both themes,
valid rest and invalid hover) have identical RGB bytes across architectures;
the comparison is retained in `artifacts/input-composited-7024b9e-comparison.json`.
This establishes that sampled architecture boundary, while native/browser
parity and the remaining visual/accessibility/scaling matrix stay open.

## Rendered Checkbox names and resting Input validation

At `76c09db`, both x64 and native ARM64 pass all 96 Light Input corner samples
and retain all twelve rest/hover captures. The reports assert the displayed
cursor, rendered border, exact native 60% opacity and unfocused state. Both later
fail Checkbox's immediate cursor observation after template clearing, before
Dark Input or independent consumer verification. Checked archives: x64
`11781357506`, SHA-256
`ed2feee247a7266232743aa0ef96ca2a91a032c98e7b7bc454eaaf028dc356d8`;
ARM64 `11781267570`, SHA-256
`acaa7cda4f49d7e1e79a095d617716e114e0c17f0cc32c8350c985aeebb42bb7`.
This adds Light Input boundary evidence without replacing the full-suite baseline.

The `1113935` x64 report is the first evidence here that disabled Checkbox
UniversalNo actually displays: GetCursorInfo succeeds with showing flag 1 and
the expected global handle for off/on, rounded corners and stationary
enable/disable. The run then fails on template removal during layout. Its
verified archive `11780458372` has SHA-256
`31bf5c107dfdd0502c7b56cc5d9f609339bd07e060053df2307807f30003b85c`.
This proves those Light transitions, not the remaining lifecycle/themes or a
complete native runtime baseline. At `019f28c`, both architectures still fail
the fixture's unsupported null-template layout. Its verified x64 archive
`11781885568` has SHA-256
`adb0862bd31fc9da759cfc7b00c1c84c5080d1c653e900fc1bbbfabb47b5a0bb`.
The ineffective local-null guard is removed; the fixture now verifies immediate
visible-cursor release and restores the branded template before normal layout,
as required by the existing ContentControl contract. A further native run is
required.

At `9c5699b`, native ARM64 passes all 57 Checkbox disabled-pointer phases in
Light and Dark, then loses foreground to a background WSL console before its
next Dark click. Its archive `11779778593` has SHA-256
`dbc4ed9ab90e3a9d276044bbb3897e5a07e9ccc8509c34aca64ec67902b8a3fc`
and was downloaded and verified. x64 passes the 16 new rounded-corner samples
in Light, then reproduces the Input thread-cursor failure. Its verified archive
`11780455227` has SHA-256
`c887b2411136103e751435370f489443cb94b46d1799d9b700307276328344ff`.
The newer `f8c67a8` ARM64 catalog/consumer passes, but x64 still fails that
cursor check. The global cursor observation remains required for visible-cursor
acceptance; these runs do not establish full parity.

The newer `bf910af` pointer candidate in
[run 38322267056](https://github.com/gaulatti/orchard/actions/runs/38322267056)
builds and passes 23 tests on both architectures. Both native catalogs pass
Checkbox remount/rapid-remount, clipping, scrolling and RTL, then fail the
enabled Input I-beam transition. Neither reaches package-consumer verification.
The archived x64 evidence `11779656182` has SHA-256
`b86793c623784cf134e9a301b7566d5e287a173a97cca0302438eabf8265cb55`;
ARM64 `11779716166` has SHA-256
`3ae86f7f376834f8486b1fd80496b06955492acc607a38fc24b23e371acff86c`.
Both archives were downloaded and verified. This partial run does not replace
the complete `ebfb6ac` baseline above.

The diagnostic `8b8be3c` run
[38323217756](https://github.com/gaulatti/orchard/actions/runs/38323217756)
passes the complete x64 catalog and package consumer. ARM64 passes the remount
and geometry phases, then reproduces the enabled Input cursor failure: WinUI's
cursor properties select IBeam while the actual native handle remains Arrow.
The ARM64 archive `11780000790` has SHA-256
`05b5ecb34d8f565c9db107ce6a8e6593c6b28c00735ad569e9120848ec34e325`
and was downloaded and verified. The release correction retains the physical
I-beam acceptance check; it needs new native evidence on both architectures.

Commit `ebfb6ac` passes all three jobs in
[run 38319267360](https://github.com/gaulatti/orchard/actions/runs/38319267360).
Both native architectures pass the complete Light/Dark catalog, 23 tests and
independent NuGet consumer. Eight strict rich-name cases pass in each appearance
on each architecture. Dark Input's diagnostic explicitly selects
`InputNormalError`, with hit testing temporarily disabled, `#FFD47B75` rendered
and palette border, and full enabled editor opacity. This verifies the resting
fixture correction; physical hover remains a separate contract.

The x64 archive `11778171441` has SHA-256
`c171a20fc9f0a60527c4769c5248f2fee3fcc7329ccd3d92c38784b5da44c152`;
ARM64 archive `11779000272` has SHA-256
`39c640f5ca815ae4e8c16967e2f44ffdd0b052f8f4a6760ef44491c5cdaec705`.
Both were downloaded and verified under `artifacts/ci-checkbox-ebfb6ac-{x64,arm64}`.
Package SHA-256 is `c02bb6b15080c6e585f9ba5e60416bda1411f97e2a152bcd1d7d7ff916525bd7`
on x64 and `1cb0a84836b518eb5ef956a2d424ede44ff491d865e5b8bae13e043e2718f0b5`
on ARM64. The 57 font probes remain diagnostic; full font, Narrator, scaling,
contrast, motion and component parity acceptance remains open.

## Checkbox and Switch native runtime checkpoint

Commit `0e354939d430a06ead949c0fc98cb198b4f46e42` passes all three jobs in
[run 38316642519](https://github.com/gaulatti/orchard/actions/runs/38316642519).
Both runtime jobs build/test/publish the catalog and pack/run an isolated NuGet
consumer. Their 23 unit/resource tests pass. Native reports confirm:

| Evidence | x64 | ARM64 |
|---|---|---|
| Process / OS architecture | X64 / X64 | Arm64 / Arm64 |
| Windows build | 26100 | 26200 |
| .NET runtime | 10.0.12 | 10.0.12 |
| Display / text scale | 1 / 1 | 1 / 1 |
| Windows animations | Disabled | Enabled |
| Light / Dark catalog | Passed / passed | Passed / passed |
| Independent package consumer | Passed | Passed |

The Checkbox reports include real hover/held press/release/exit, physical
Space/Tab, disabled skipping/rejection, box/label/description/RTL clicks,
exactly-once command/value events, consumer binding and template/visibility
lifecycle. Composed pixel samples verify the focus ring beyond both box edges
and its offset. The Switch also passes both appearances and native physical
input. All 48 Checkbox font-probe dimensions agree across architectures, but
the probes identify remaining source font mismatches rather than accepting them.

Package 0.1.0 SHA-256 is
`9d584e2bb3e9135b8ce33813645230305e235a1c9a6e98403ad9c20f9a52b8fe` on x64 and
`b16df6883c1b513ff2846890071832c657280095057cb4f301747d695a15d8b5` on ARM64.
The x64 artifact `11777456732` has SHA-256
`8cad4468b21f813414b8b5f72b6a67d73b27be49699eb91b0217aa816acb03b5`;
the ARM64 runtime artifact `11777611609` has SHA-256
`2e90b8f3d321d114fdcd8225bc7c52261295442a4a331d3a8108929aa0d54d11`.
Both archives were downloaded and hash-verified. Selected reports/captures
are retained under `artifacts/ci-checkbox-0e35493-{x64,arm64}`.

The prior `3485f52` ARM64 run passed Light, then failed Dark Input validation.
The new diagnostic reports show correct InputNormalError, focus, palette and
rendered settlement in this run; the previous failure's cause remains unproven.
The Checkbox rich-name diagnostics also expose model string representations
for data templates. These gaps and the full acceptance matrix remain open.

At `9c6a8ce`, [run 38318155884](https://github.com/gaulatti/orchard/actions/runs/38318155884)
passes x64 catalog/consumer and ARM64 compilation. The Checkbox's rendered
rich-content names pass eight regression cases in both x64 appearances and
ARM64 Light, including live text, updated label/description data templates,
collapsed content, explicit subtree names and owner name/label precedence.
ARM64 then fails Dark Input's resting validation assertion: diagnostics show
`InputHoveredError`, matching palette/rendered `#40FFFFFF` and opacity 1. This
identifies pointer interference in that run; it does not retroactively prove
the cause of the undiagnosed `3485f52` failure. ARM64 Dark Checkbox and its
independent package consumer were not reached.

The x64 artifact `11777688804` has SHA-256
`835d436b6027e12b8966a12e5acbf2052816d52e5c27b8e77f91f431360f72c7`;
ARM64 artifact `11777753571` has SHA-256
`3699facdbd33c56922586c58cd4130ef5f8cf07bda136c6680322ed315f8bb5d`.
Both were downloaded and verified under `artifacts/ci-checkbox-9c6a8ce-{x64,arm64}`.
The resting Input fixture correction passes at the later `ebfb6ac` checkpoint
above. No full component acceptance is claimed by these partial results.

## Switch x64 runtime

Commit `7001e3e46627263c909e3b324b139681e5f2434a` passed the x64 job in
[run 38283953920](https://github.com/gaulatti/orchard/actions/runs/38283953920).
The catalog report records X64 process and OS, Windows 10.0.26100, .NET 10.0.12,
display/text scale 1 and animations disabled. In both light and dark, the Switch
fixture passed geometry/palette, first-display wrapping, UIA state/name/focus,
disabled rejection, consumer binding, template/unload/reload, live appearance,
reduced-motion settlement, physical Space/Tab, track/label clicks and RTL
activation. Native input required the pinned SDK's transparent SwitchThumb part.
This run does not verify intermediate animation frames.

The x64 artifact is `11763240141`, SHA-256
`c4163aae82022a2897c76bc2d0f8589c82183166708dbd72881f01a9fa0d2256`.
The downloaded archive was hash-verified at
`artifacts/ci-switch-7001e3e-x64.zip`. Selected reports and composed client
screenshots are under `artifacts/ci-switch-7001e3e-x64/`. The client captures fit
the physical monitor and include the composition thumb shadow.

The independent consumer reports runtime success for package 0.1.0, SHA-256
`601082af3516cf0eab9f2f28c09da57ead2968009b54d9e1e46aa9cf199055de`.
Its process and OS are X64. The ARM64 cross-build/isolated-consumer build also
passed. The native ARM64 job reached the Switch desktop check but could not
acquire foreground input; the independent ARM64 runtime consumer was skipped.
The next diagnostic uses the XamlRoot AppWindowId as the authoritative HWND and
retains the foreground/session/input-desktop state. This is not an ARM64 pass.

Full Switch acceptance remains open. The natural Latin row measures
173.272018px against the browser's 173.296875px, while the 200px constrained row
retains four lines and a 152px text allocation. Japanese fallback glyphs measure
99.538086px against 111.453125px in the reference, a visible width mismatch that
must be resolved. The browser captures are JPEG despite their .png filenames;
they do not support a lossless pixel-equality claim. Exact glyph/pill/shadow
raster comparison, animated state/appearance transitions, drag/cursor/focus,
contrast/Narrator and isolated scaling remain unaccepted.

## Switch motion and hosted desktop follow-up

At `f725ecea147cf88b0d6c1e57092f57e6fd198b96`,
[run 38291211362](https://github.com/gaulatti/orchard/actions/runs/38291211362)
passed x64 catalog/runtime and the independent consumer. Package 0.1.0 reports
SHA-256 `de7c3c134dc77180bc117b27fd1e6db2e0ab2a00bdd6fd27d88d61c697b34487`.
The x64 artifact `11766796078`, SHA-256
`1ce00b21cae104a7c2db1cbb3a5e7d1733f3e1399a7d8316b15b2e93a6dd0f16`,
was downloaded and hash-verified under `artifacts/ci-switch-f725ece-x64`.

The native ARM64 job passed the Switch's Light appearance and motion checks
before its physical-input failure. Animations were enabled. Separate reports
retain intermediate track colors, off alpha, thumb movement, reversal and a
palette change during a toggle. The appearance report marks `passed: true`;
motion verification returned before physical input began. This verifies these
Light-started checks, not the unexecuted Dark-started or full input matrix.
The ARM64 artifact `11766885762`, SHA-256
`bc060aefe622c54bdcb241e0067857413aa477659a6079b735b0b942588140b1`,
was downloaded and hash-verified under `artifacts/ci-switch-f725ece-arm64`.

The ARM64 session is active console session 2, WinSta0/Default, WTS state 0.
The catalog is visible, uncloaked and topmost. The intended point is instead
covered by a topmost `WWAHost` CoreWindow in the same session, and the Alt retry
still cannot activate the catalog. Additional diagnostics query that process's
application identity without its title or content. The physical check continues
to reject an unowned target; the ARM64 consumer runtime remains skipped.

Font probes at `fc93aa2` returned identical x64/ARM64 widths: 99.538086px for the
default bundled family, and the same result with Segoe UI/Arial appended.
Microsoft YaHei UI or MS Gothic produces 112px, which still differs from the
111.453125px browser reference. These are diagnostic candidates, not accepted
font mappings. Motion raster parity, fallback glyphs and the other acceptance
gaps remain open.

The Switch follow-up at `19c1cb25a3ec00a828f77659cd7357773574eded`
([run 38293032359](https://github.com/gaulatti/orchard/actions/runs/38293032359))
passed x64 runtime and independent consumption, and ARM64 cross-build. The
native ARM64 check instead stopped at live appearance: the on/off pair had
settled while DisabledOff had not yet met its palette assertion. It did not
reach the application-identity diagnostic or physical input. Artifact
`11766798546`, SHA-256
`2e997121a289bfda2b3f1df4ca34a63296080a38a46492935d7f806d38c2e007`,
was downloaded and hash-verified under `artifacts/ci-switch-19c1cb2-arm64`.
The next fixture waits for every row's fill and border endpoints and retains
all row values on failure. This run does not replace the shared baseline below.

At `4b91993af77a7a56d6aa774e26e15180e9250749`,
[run 38294745658](https://github.com/gaulatti/orchard/actions/runs/38294745658)
passes all nine rows' alternate/restored palette endpoints on native ARM64,
with the Light-started motion checks returning before physical input. The
target-ownership diagnostic identifies the covering welcome app as
`Microsoft.Windows.CloudExperienceHost_cw5n1h2txyewy!App`, in the catalog's
active session. Artifact `11768680948`, SHA-256
`61c451ca0f2c65174fa5f749739bf234c13c9dd065fbd07abd595da11b042882`,
was downloaded and hash-verified under `artifacts/ci-switch-4b91993-arm64`.
Physical input, Dark-started checks and the ARM64 consumer remain unexecuted.
The next hosted check requests normal minimization of that exact foreground
application and keeps all foreground, occlusion and ownership gates active.
It does not change the developer's desktop or accept welcome preferences.

At `3a26b98caceabf12e0c07e459f49025a6b225bd4`,
[run 38295643362](https://github.com/gaulatti/orchard/actions/runs/38295643362)
passed x64 runtime/consumer and ARM64 cross-build. The native ARM64 welcome
report records a successful minimization request for the identified app, but
the pointer target belongs to a topmost, visible, uncloaked explorer window
with class `UserOOBEWindowClass` in session 2. The physical gate correctly fails;
the request does not prove that foreground was acquired. Artifact `11768162672`,
SHA-256 `c3bc0e94baaaf4ea5b909180b251ed80008bab8242f70e353586b2dcb21b326e`,
was downloaded and hash-verified under `artifacts/ci-switch-3a26b98-arm64`.
The next preparation also requests normal minimization of that exact welcome
backdrop, gated by the app identity, explorer process name and matching session.
Dark-started checks, physical input and ARM64 consumption remain unverified.

The first Checkbox runtime at `79f10535647973b89da1e29cd2853f0d0f24d10b`
([run 38297199784](https://github.com/gaulatti/orchard/actions/runs/38297199784))
failed its initial glyph geometry check on x64 and ARM64; ARM64 cross-build
passed. The x64 report/capture was downloaded and hash-verified from artifact
`11768994195`, SHA-256
`b655bea08cd5cff8d33c53f5fc4cc01d56eaa7cf5715dbd569ea13fce1f8b49c`,
under `artifacts/ci-checkbox-79f1053-x64`. Default text measures
291.700043×42px, and the image exposes RTL checkmark mirroring. The follow-up
adds an explicit square SVG viewport and preserves its left-to-right geometry.
Remaining behavior checks were not reached. Both local architecture builds
and 23 unit tests passed. A locally packed isolated x64 consumer build passed
without runtime launch; package SHA-256 is
`09fd119f4665472b3a9349ed44b5cad46a272da17b903e6f5a90f4f870ee21f6`.

At `a4720b2b25f669141835a8c2e46c4cf7453f9aa4`,
[run 38299428078](https://github.com/gaulatti/orchard/actions/runs/38299428078)
passed both architecture builds and all 23 unit tests. Both native jobs
passed Checkbox's initial geometry, UIA role/name/value and exactly-once events,
disabled rejection, binding in both directions, content updates, focus modality
and unchecked visual states. Both then failed while clearing Template, before
motion, live appearance, Switch or independent runtime consumption. The x64
report retains the original XAML parsing exception and seven verified steps.
Artifact `11770053223`, SHA-256
`8274634f4728b7e884e8f2dd02904c64e1181c9bc36aa381f8f2f54ea20d33d2`,
was downloaded and hash-verified under `artifacts/ci-checkbox-a4720b2-x64`.
Its initial capture confirms the corrected RTL checkmark and exposes the
37px rich-content row. The actual pinned browser matrix measures that row
165.109375×42px and confirms dark deep-sea #182533, dark sea #5BA3F5 for
hover/focus, and the dark focus offset. Those reference reports/captures are
retained under `artifacts/checkbox-reference-matrix-{Light,Dark}` and
`artifacts/checkbox-reference-focus-Dark`. The follow-up fixes those contracts
and adds native boundary checks; it does not establish their runtime acceptance
until the hosted checks return. The shared baseline remains unchanged.

At `48386d4`, [run 38300873774](https://github.com/gaulatti/orchard/actions/runs/38300873774)
again passed builds and all 23 tests. The new native rich-content boundary check
failed on both architectures at 165.083999×37px. The owned TextBlocks retain
LineHeight 0 / MaxHeight despite the presenters' LineHeight 20. The follow-up
supplies scoped TextBlock style defaults, preserving explicit consumer
formatting. That run did not reach template replacement, motion, appearance,
Switch or independent runtime consumption.

At `d0a66dd`, [run 38301875168](https://github.com/gaulatti/orchard/actions/runs/38301875168)
passed builds and unit checks, then failed the rich row at 39.5px on both
architectures. The description resolved the presenter-scoped 20px style;
the native Content-owned label retained its 17.5px automatic line. The next
candidate scopes the default style to the checkbox's private dictionary and
adds consumer style/local-property/data-template regression checks.

At `84e6ef2`, [run 38302708416](https://github.com/gaulatti/orchard/actions/runs/38302708416)
passed builds and all 23 tests. Both native jobs reached the consumer data
template assertion after the default rich row and explicit style/local line
overrides passed. The next fixture waits for binding propagation and retains
template text/layout diagnostics before restoring the original rich content.
Template clearing, motion, appearance, Switch and independent runtime
consumption remain unreached. A separate local isolated x64 consumer build
passed with runtime disabled; its package SHA-256 is
`644e34e16dc42e5ac0466692c0bdc2e9e6c9654eb85a6d254b0edf8007114131`.
The x64 captures/report were downloaded and hash-verified from artifact
`11770703516`, SHA-256
`f4714cf3c154b40a7a9f8ae0495b85980ed3d77c4d162a42c2248e2a6c29e7aa`,
under `artifacts/ci-checkbox-84e6ef2-x64`.

At `d382242`, [run 38304146283](https://github.com/gaulatti/orchard/actions/runs/38304146283)
shows that consumer template binding succeeds on x64 and ARM64. Both reports identify
correct strings and the expected ConsumerLabel data context, but each template
line remains LineHeight 0 / MaxHeight at 17.5px, yielding 57px rather than 62px.
The next candidate supplies the source line defaults to template-generated
text only when no local value, binding or style setter supplies them. It keeps
the consumer template/styles intact and removes owned defaults on detachment.

At `7a0def1`, [run 38305820220](https://github.com/gaulatti/orchard/actions/runs/38305820220)
passes builds/unit checks and the rich-content native boundary checks on both
architectures, then fails at ApplyTemplate after assigning null. The x64 report
and captures were hash-verified from artifact `11771893712`, SHA-256
`1b3cff8a4a96e64d04e36b57803855a01ce911b350e794efeb379de74507f11f`,
under `artifacts/ci-checkbox-7a0def1-x64`. Template phase diagnostics and the
original managed exception trace will distinguish clearing from native style
resolution; no lifecycle assertion has been relaxed.

At `775c1b1`, [run 38306688055](https://github.com/gaulatti/orchard/actions/runs/38306688055)
records zero visual children immediately after null assignment and a rebuilt
Grid after original-template restoration. The x64 original failure remains the
explicit native ApplyTemplate(null) invocation; no managed override error is
traced. Null-property cleanup now detaches Orchard observers and owned content.
The fixture verifies empty-tree layout and native value/content/name updates before restoring
and applying the nonempty template. Explicit native ApplyTemplate(null) remains
a documented SDK limitation, rather than a verified library capability.
The snapshots were hash-verified from artifact `11772384171`, SHA-256
`e3ff9a0c9c1476ce416b231fdadac218461e3c944ddc26b71e537290dd155510`,
under `artifacts/ci-checkbox-775c1b1-x64`.

At `ec16816`, [run 38307865801](https://github.com/gaulatti/orchard/actions/runs/38307865801)
fails during normal null-template layout with the original managed error,
`BleeckerCheckbox requires PART_Box`. Thus the failure is not limited to an
explicit application call: native theme lookup selects non-Orchard chrome.
The next correction clears the default style key while empty and restores it
with a nonempty template. Verification retains explicit ApplyTemplate(null),
normal empty layout, consumer state/content/name updates and restoration.
The x64 font probes and captures were downloaded and hash-verified from
artifact `11773291932`, SHA-256
`da33f36824120a31e03325164059c68414fdfa2026f0cde9278240516f0a81c1`,
under `artifacts/ci-checkbox-ec16816-x64`. Its explicit Yu Gothic probe measures
111.439453px against the source's 111.453125px; the default is 99.538086px.
This is retained probe evidence, not a change to the shared font stack.

At `2bf7205`, [run 38309805067](https://github.com/gaulatti/orchard/actions/runs/38309805067)
passes builds/unit checks and rejects post-initialization DefaultStyleKey
changes with COMException 0x80004005 on both architectures. The mutation has
been removed. DefaultStyleResourceUri is now set during construction to
Orchard's Generic.xaml, using the pinned SDK property documented by
[Microsoft](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.control.defaultstyleresourceuri?view=windows-app-sdk-2.0).
The same null application/layout/state/content/restoration gates remain active.
At `8dccce0`, [run 38311418818](https://github.com/gaulatti/orchard/actions/runs/38311418818)
again fails null-template application on x64 and ARM64; ARM64 build/independent
consumer build passes. The x64 phase report is retained in the digest-verified
`artifacts/ci-checkbox-8dccce0-x64` archive (SHA256
`2942b4e88ab1b8e0f40871e2c08ff9293f281aff3aadbe9868a6f48624903b67`).
It proves application creates an unnamed Grid and replaces the null effective
template. Microsoft's ContentControl source explains this native content
presentation path; it is not evidence of stock CheckBox style lookup. The
constructor resource-URI experiment is removed. Lifecycle acceptance now
checks immediate detach/valid restore and normal-layout hide/show using
Visibility, including native state, content/name and rich ownership. A
persistent null-template empty state was an incorrect fixture assumption;
the consumer Template property and bindings remain untouched by Orchard.
At `7062317`, [run 38312230166](https://github.com/gaulatti/orchard/actions/runs/38312230166)
passes template detachment/restoration, then rejects the hide/show fixture on
both architectures because a fresh Toggle pattern is unavailable while the
control is collapsed. The fixture now retains the native provider while the
control is visible before checking consumer state updates during collapse.
No library behavior is changed to manufacture an accessible hidden control.
The checkbox fixture now captures an additional six-script, three-weight
font comparison with explicit language tags, before its template lifecycle
checks. The original and Yu Gothic candidates use actual bundled font
resources. Reports and captures restore the consumer content and language;
the shared library font stack is unchanged pending comparison.
At `7afa247`, [run 38311616439](https://github.com/gaulatti/orchard/actions/runs/38311616439)
completes the 36 native script/font probes on x64 and ARM64 before the known
null-template failure. Both architectures produce identical probe dimensions.
Digest-verified archives are retained at `artifacts/ci-checkbox-7afa247-x64`
(SHA256 `11e66feef27e090747da2b7e8cf0df379ab6ca395f846aabe46192cfbfc01a7f`)
and `artifacts/ci-checkbox-7afa247-arm64`
(SHA256 `c2cbcd41ed53eaab0038ddae3ff336f052bd7baf72188376c5548d5d99013c6e`).
The retained comparison exposes semibold Japanese and Arabic font differences;
native architecture agreement does not establish browser parity.

The checkbox fixture now adds physical keyboard/pointer and inherited-command
checks with a catalog-only NativeInputSession. Foreground input and pointer
hit points must belong to the catalog's own native window. Captures include
the composed keyboard/pointer window; reports retain incremental checks and
desktop diagnostics. Runtime verification of these new gates is pending.
At `3dac874`, [run 38312883131](https://github.com/gaulatti/orchard/actions/runs/38312883131)
passes x64 build, 23 unit tests, full catalog runtime and the independent
NuGet consumer (package SHA256
`ed41e1ded3ed586d6d02261229aded4b9c3f2436905a469f898fd0b16b03553b`).
ARM64 passes its build/independent consumer build and light checkbox lifecycle,
motion, physical input and command checks with animations enabled. The dark
forced hover check fails because the foreground-acquisition fallback toggled
Off during light testing and left its state selected. Acquisition now clicks
the static heading, state cleanup includes Off, and hover/held press/exit are
tested through actual input. Native captures and reports are retained in
`artifacts/ci-checkbox-3dac874-x64` (archive SHA256
`358ed4ee233eb6168cfdb39b2c5b262a5ff2e9f2f8558cb0f41157b6339d7b12`)
and `artifacts/ci-checkbox-3dac874-arm64` (archive SHA256
`bf2854db389331e44572a40214013322efde299d6297b8549cab03fe760d37ab`).
This is x64 acceptance of the current registered runtime gates, not full visual
acceptance or a new shared architecture baseline.
Inspection of the ARM64 composed keyboard capture also reveals a missing
visible focus ring despite its reported 28px size. The focus layers now use
negative layout margins, matching their outer extents without growing the
20px input. A native pixel regression checks both outer edges and the inner
offset in the captured window, after a rendering opportunity, and retains
`checkbox-focus-pixels-{Light,Dark}.json`. This correction needs fresh runtime
and rendered verification; the earlier dimension/opacity check alone was
insufficient evidence of visible focus chrome.

## Shared x64 and ARM64 baseline

The first native ARM64 run was [38267617259](https://github.com/gaulatti/orchard/actions/runs/38267617259)
at commit `403d8783416eae79086a6b59c714131a88a44121`. All three jobs passed:
x64 build/runtime/consumer, ARM64 cross-build/consumer build, and native ARM64
build/runtime/consumer. The repeat run [38268516314](https://github.com/gaulatti/orchard/actions/runs/38268516314)
at `5412997a3dfd15c76a06367b51480ab6a91b8e7c` also passed all three jobs,
including the tighter fractional Progress width regression.

The native reports from the repeat run record:

| Evidence | Observed value |
| --- | --- |
| Catalog process architecture | `Arm64` |
| Catalog OS architecture | `Arm64` |
| Consumer process / OS architecture | `Arm64` / `Arm64` |
| Windows version | `10.0.26200` |
| .NET runtime | `10.0.12` |
| Display / text scale | `1` / `1` |
| Windows animations | Enabled |
| Catalog / independent consumer | Passed / passed |
| Package version | `0.1.0` |
| ARM64 consumer package SHA-256 | `3b22c5e4b0039119c19519e2b8c300d9e205eeccc58702a66bb2074b2e987641` |

The `orchard-windows-arm64-runtime` artifact retains the runnable catalog,
independent consumer, package, 19-test report, captures and per-fixture JSON.
Artifact `11755888347` has SHA-256
`2fcbf474c4c660e45cad7cca1ab6a5caa58e066b3aa807b0a9e90b577dcbd209`.
Selected reports and captures are retained locally under
`artifacts/ci-arm64-5412997`; the earlier artifact under
`artifacts/ci-arm64-403d878` was also downloaded and its archive hash verified.

The architecture gates inspect reports produced by the running applications.
ARM64 smoke requires an ARM64 process and OS; the package consumer requires the
selected process architecture and an ARM64 OS for ARM64 verification. A build,
runner label or emulated x64 process cannot satisfy these checks. Each runtime
job currently packs and verifies its own candidate from the same source.

For Progress, the earlier x64 `4c76d59` and ARM64 `403d878` light/dark captures
are pixel-identical: zero differing pixels in each 756×502 image. The library
sources are unchanged between those commits. The comparison is retained at
`artifacts/progress-native-architecture-comparison.json`. The repeat ARM64 run
reports a 174.199996948px fill for a 268px track at 65%, preserving the
mathematical 174.2px width rather than rounding it to 174px. The pinned browser
reports 174.1875px after CSS layout-unit quantization. See [Progress](progress.md)
for the pill rendering checks and remaining reference-comparison work.

Full visual comparison across the inventory, actual contrast settings,
Narrator, display/text scaling, all motion/input states, collection performance,
the two substantial catalog compositions and the remaining components still
require acceptance evidence. The local development host remains Intel x64;
these checks ran on GitHub's ARM64 host without changing the developer's desktop
settings.

## Sparkline checkpoint: stationary Input cursor investigation

The local x64 full run at `artifacts/smoke-sparkline-full` passes the chart
fixtures, then fails the Light Checkbox sequence while disabling its shared
Input under a stationary pointer. The expected UniversalNo cursor is not
observed within the unchanged deadline. The preceding recording observes the
enabled editor's actual I-beam. This is a separate open runtime failure; focused
Sparkline and normal Line Chart checks pass. The fixture now records foreground,
hit testing, native cursor and publisher state at this transition on failure,
without changing expected handles, input routing or deadlines. Push-triggered
Actions remain paused at the user's request.

The next complete Light/Dark run at
`artifacts/smoke-sparkline-full-diagnostic` passes with the unchanged six-minute
deadline. The stationary transition passes in both themes. No cursor product
change was made, so the first failure remains an open intermittent issue.

The later Stat Card full run at `artifacts/smoke-stat-card-full-contract`
reproduces that transition and retains the new diagnostic trace. Its two failed
poll reads occur at 0.1985ms and 0.8934ms, with the actual cursor changing from
I-beam to UniversalNo and foreground/ownership intact. The generic wait exits
after its first matching read, then immediately asserts a second read; it can
therefore fail during this transition without using the remaining two-second
deadline. The stationary-disable gate now requires two consecutive Rendering
observations of actual UniversalNo, the owned foreground, the correct cursor
owner and no pending publication. Its deadline remains two seconds. It retains
all observations on success and failure. No product cursor code, handles,
publication phases or input paths change. The complete Light/Dark catalog now
passes at `artifacts/smoke-stat-card-full-settled` with the six-minute deadline
unchanged. Light records settled UniversalNo at 12.5538ms and 28.6331ms; Dark at
35.7968ms and 52.153ms, each with owned foreground and no pending publication.
The reports retain all four Dark samples and both Light samples. This verifies
the corrected local observation boundary; native ARM64 retesting and the
separate exterior Input/AppStarting failure remain deferred during the Actions
pause.

## Pie and Donut implementation checkpoint

Commit `d95e454` adds both controls, the typed consumer datum and the shared
registered fixture. All 84 pinned browser cases pass native x64 checks in both
themes at `artifacts/smoke-pie-chart-final`, including path geometry, colors,
source text baselines and center allocation, raw keyboard payloads and tooltip
visibility. Live native input, typed notifications, consumer collection/brush
identity, radius and center changes, font overrides, real detach, rapid
reparent and template restoration pass. The font and source observation details
are recorded in [the Pie/Donut contract](pie-chart-reference.md).

Both solution architectures build without warnings, all 44 regressions pass,
and token generation is current. The regular 0.1.0 package has SHA-256
`d26aa70c763c39921f99a0fa1badd3aecd87f47e27a93cb983892135be5b9aae`
and repository metadata for the clean source commit
`d95e454d28afb91c2c23c33d8300122d01e472c2`. Its isolated x64 consumer passes
actual Light/Dark runtime, including both new controls and live typed data;
the isolated ARM64 consumer builds with runtime verification explicitly false.
Reports are under `artifacts/package-consumer-pie-chart` and
`artifacts/package-consumer-pie-chart-arm64`. Both temporary workspaces are
removed. The runnable catalog is `artifacts/catalog-pie-chart`.

These controls remain in progress. The inventory contains 41 in-progress and
71 pending entries, with no entries accepted. Full visual/motion/data/override/
RTL, actual contrast, Narrator, display/text scaling, collection performance
and native ARM64 acceptance remain required. The public wiki is unavailable;
repository documentation records this evidence. Push-triggered Actions remain
paused at the user's request.

The final published catalog from `220a8ec` passes the complete Light/Dark
sequence in 352.436 seconds with the 360-second deadline unchanged, at
`artifacts/smoke-pie-chart-published-settled-full`. It saves 904 PNG captures,
passes both Pie/Donut source and lifecycle reports, and retains both exact
124-pixel Attention masks. The two preceding runs remain recorded as failures:
one reaches the final Dark captures but times out; the next fails Attention's
fractional-scroll return. Both screenshot blank checks now short circuit the
same nine-distinct-byte predicate, and the Attention fixture waits for stable
geometry instead of two arbitrary frames. Frame traces demonstrate text moving
between those first two events. The final runnable output is
`artifacts/catalog-pie-chart-settled`; no library/package input changed after
the independently verified `d95e454` package.

## Scatter Chart implementation checkpoint

Commit `d650242` adds numeric Scatter/bubble plots, typed series with original
optional data, shared first-series axis keys and a registered catalog page.
All 64 source cases and live native input/data/lifecycle checks pass in both
themes at `artifacts/smoke-scatter-chart-crosshair`. The reference loads the
bundled font before mounting the unchanged source; its initial fallback-font
measurement and recapture evidence are documented in
[the Scatter contract](scatter-chart-reference.md). Exact circle paths, numeric
axes, labels/grid, legend baselines, tooltip payload/height/visibility and
crosshair paths are checked. Source/native Light and Dark screenshots were
inspected. Full raster and platform acceptance remain open.

Both solution architectures build without warnings, all 44 unit/resource
regressions pass, and token generation is current. A clean-source 0.1.0 package
from `d6502425f138e5d046765c944644134d7f5e300d` has SHA-256
`4f83502c585ea3cd08585d876659dc9a3ec7c288463812cad47c2c7875e7655c`.
Its isolated x64 consumer passes actual Light/Dark runtime at
`artifacts/package-consumer-scatter-chart-float`; its independent ARM64 consumer
build passes at `artifacts/package-consumer-scatter-chart-float-arm64`, with
runtime verification explicitly false. Both temporary workspaces are removed.
The consumer's initial radius assertion was corrected to the exact native
single-precision stored value, with library/package inputs unchanged. The
initial diagnostic failures remain recorded.

The inventory contains 42 in-progress and 70 pending entries, with none fully
accepted. Full shape/glyph/effects raster, motion, data/key/formatter/override/
RTL coverage, actual contrast, Narrator, scaling, collection performance and
native ARM64 acceptance remain required. Push-triggered Actions remain paused
at the user's request; the public wiki is still unavailable.

Full catalog verification subsequently exposes a Dark hovered-record mutation
failure. An intermediate correction retains point visuals during data changes;
the unchanged browser source confirms that updated hover text remains visible
while the physical pointer stays stationary. The strengthened native sequence
passes at `artifacts/smoke-scatter-chart-settled-hover`, including two rendering
events after the mutation, consumer color reset and repeated-series points.
The catalog's final screenshot navigation now waits for stable viewports,
and Sparkline's mutation records actual key press/release delivery and state.
Those verification changes preserve the existing bounds and deadlines. Earlier
timeouts and failed runs remain documented in the component contracts.

The fresh package from clean commit
`a6e23bd22a4b8cd6289a71e021c783675cbdce87` has SHA-256
`6c56d4797a37039bdf0a8872d210b60efb4012fa1f54e4701dbc1bb0c829313f`.
It passes the isolated x64 consumer in both themes at
`artifacts/package-consumer-scatter-chart-hover`; the isolated ARM64 consumer
build passes at `artifacts/package-consumer-scatter-chart-hover-arm64`, with
runtime verification false. Both temporary workspaces are removed. Both
solution architectures build without warnings and all 44 checks plus tokens
pass. The intermediate runnable x64 catalog is `artifacts/catalog-scatter-chart-hover`.

That full published run fails the strengthened Dark stationary-hover check at
291.150 seconds, so it is not accepted. A repeated-mutation control identifies a
platform-generated `PointerMoved` event with identical native/local coordinates
and timestamp, but a new Line hit target, clearing pointer selection. The final
correction ignores generated empty hits/exits for Scatter and removes the visual
cache. Its geometry-keyed hover identity distinguishes leaving an unchanged
point from retained inspection after point replacement; a new point hit restores
normal leave behavior. Event/state traces and superseding verification are recorded in
[the Scatter contract](scatter-chart-reference.md).

The corrected source commit `ad2814e36140af64d9c035321c959f4d7860d57c`
passes the strengthened 64-case Light/Dark sequence at
`artifacts/smoke-scatter-chart-hover-identity`, including eleven stationary
mutations and both unchanged/replaced point leave paths. Its fresh regular
0.1.0 package has SHA-256
`5e14ae3752ba6c91f840a7a2a876df4868576f867f88ceca6e0ded3f606205fb`.
The isolated x64 consumer passes actual Light/Dark runtime at
`artifacts/package-consumer-scatter-chart-interaction`; the isolated ARM64
consumer builds at `artifacts/package-consumer-scatter-chart-interaction-arm64`,
with runtime verification false. Both temporary workspaces are removed. The
runnable x64 catalog is `artifacts/catalog-scatter-chart-interaction`.

The complete run from that catalog passes all component fixtures, including
Dark stationary hover, but reaches the unchanged 360-second limit during final
Dark screenshots. `artifacts/smoke-scatter-chart-published-interaction-full` is
not a complete pass. Pointer moves now wait for two unchanged Rendering
transitions in native screen bounds, dimensions and ancestor viewport metrics
instead of a fixed 100ms pause. Every observation is retained; existing native
foreground, ownership, exact boundary and final-hit assertions remain active.
The focused Scatter/Sparkline sequence and complete Input fixture pass both
themes at `artifacts/smoke-scatter-chart-stable-pointer` and
`artifacts/smoke-input-stable-pointer`. No library or package input changes.

The initial complete stable-pointer run fails Dark Attention's distant child
action while its stable bounds are still off-screen. Readiness now requires the
requested point to pass the existing native owned-window hit check across the
stable frames. Focused Attention passes Light/Dark at
`artifacts/smoke-attention-visible-pointer`, preserving four scroll/return paths
and exact 124-pixel masks. All fifty target gates pass; nine wait for an initial
unowned point, and maximum settling is 96.6803ms. Scatter/Sparkline also pass at
`artifacts/smoke-scatter-chart-visible-pointer`. The failed full run remains at
`artifacts/smoke-scatter-chart-published-stable-pointer-full`.

The final complete published catalog from `20820c6` passes in 325.934 seconds,
with the 360-second limit unchanged, at
`artifacts/smoke-scatter-chart-published-visible-pointer-full`. It retains 916
PNG captures, sixteen settled final-capture reports, exact 124-pixel Attention
masks in both themes and eleven stationary Scatter mutations per theme. All
544 pointer gates pass, with two stable transitions and owned final points;
twenty-one wait for initially unowned targets, and maximum settling is
167.0193ms. The runnable catalog is `artifacts/catalog-scatter-chart-visible-pointer`.
Library inputs remain the independently verified `ad2814e` NuGet package.
The inventory remains 42 in progress, 70 pending and none accepted. Native
ARM64, full raster/motion, contrast, Narrator, scaling and collection-performance
acceptance remain open. Push-triggered Actions remain paused by user request.

## Sunburst and scoped chart font setup

Clean source `3b3c836e07c7c988f74606d431d34ccd51e6d98d` passes the complete
published x64 catalog in 354.531 seconds at
`artifacts/smoke-chart-font-style-published-full`. The 360-second deadline is
unchanged. All 964 previous PNGs remain, with 976 total. All 1,684 native pointer
gates pass their existing stable-transition, ownership and exact-receipt
checks; 46 click releases, 28 tooltip gates and 16 final capture reports pass.
Absolute input now addresses the requested pixel's interior correctly; no
coordinate tolerance, cursor handle, pointer parking or deadline is changed.

Sunburst adds 94 frozen source cases, four invalid-tree probes and actual native
input/tree/font/lifecycle checks. Shared font setup is selected once per render
for Sunburst and Radial Bar; all 174 native glyph hashes match the originals.
Both architecture builds have zero warnings, all 55 regressions and generated
token checks pass. The package at `artifacts/packages-chart-font-style` has
SHA-256 `57832285ebc784d125460656faeb55679cff2ba2e7977c6b6352132b3ff51b2d`;
fresh x64 consumption passes both themes, while ARM64 consumption is build-only.
Both isolated caches are removed. The runnable catalog is
`artifacts/catalog-chart-font-style`, with its source commit in ProductVersion.

Inventory: 47 in progress, 65 pending, none accepted. Twelve complete Sunburst
viewports still differ from the browser with zero excluded pixels. Exact
raster/motion, complete overrides/RTL, actual contrast/Narrator/scaling,
collection-performance acceptance and native ARM64 runtime remain open. There
is no backend/Prometheus surface. Wiki access remains unavailable, and these
in-repository documents retain the contract. Push-triggered Actions stay paused.

## Currency Chart published checkpoint

The fixed currency Chart adds 94 source cases and native coverage while
retaining the 112-entry inventory. Source `aa5e6d4` passes the complete native
catalog in 358.064 seconds under the unchanged 360-second deadline at
`artifacts/smoke-currency-chart-area-handover-published-full`. All 988 PNGs are
present and every previous 976 PNG is retained. Both solution architectures
build without warnings; 57 tests and token checks pass.

The audit records 1,710 pointer gates, minimum two stable Rendering
transitions, maximum 233.734 ms; 1,710 exact owned moves, including 86
stationary moves backed by prior actual receipts; 1,724 exact key actions;
46 click releases; 28 tooltip presentation gates; and 16 final captures.
There are no failed gates, moves, keys or releases. Eighty-two compositor
commits include seven bounds changes caught before input. All 174 preexisting
Sunburst/Radial Bar glyph hashes match. Evidence is retained in
`artifacts/currency-chart-final-full-input-report.json` and
`artifacts/currency-chart-final-evidence.json`.

The runnable catalog is `artifacts/catalog-currency-chart-area-handover`,
ProductVersion `0.1.0+aa5e6d4731d8843fdaf2589abc2212284064801f`. Package 0.1.0
at `artifacts/packages-currency-chart-area-handover` has SHA-256
`ade2395c1ec16f50961a1f9dc56c4b4c7be9877d8e47adcf9e2ff366164c9155`.
Fresh x64 runtime verification passes both themes at
`artifacts/package-consumer-currency-chart-final`; ARM64 build-only
verification passes at `artifacts/package-consumer-currency-chart-final-arm64`.
Both private caches are removed. Their Orchard assembly matches the catalog.
Consumer menu checks now await actual foreground/template/shadow readiness;
all existing assertions remain. No catalog or library code changed after the
successful published run.

Inventory: 48 in progress, 64 pending, none accepted. Currency's twelve whole
viewports still differ from the browser by 769–5,761 pixels, maximum channel
difference 239, with no excluded pixels or tolerance. Full data/override/RTL,
raster/motion, contrast/Narrator/scaling, collection performance and native
ARM64 runtime remain open. There is no backend/Prometheus surface. The public
wiki remains unavailable; documentation stays in the repository. Push-triggered
Actions stay paused at the user's request.

## Pagination popup validated checkpoint

The full x64 catalog from clean source `27acbc4` passes in 355.1883746 seconds
with all 1,030 expected PNGs, unchanged 174 protected glyph hashes and no failed
native input/presentation gates. Both architecture builds, 88 regressions and
the token check pass. The published catalog's 532 files match the measured
build exactly, and a freshly restored x64 NuGet consumer passes both themes.
The preceding Pie focus failure remains unexplained; the later passing run
does not establish a causal fix. Component acceptance and native ARM64 runtime
remain open. Artifact paths, hashes, audit counts and limitations are in the
[Pagination checkpoint](data-table-pagination-reference.md#validated-popup-checkpoint).
Push-triggered Actions remain paused at the user's request.

## Pagination owned chooser checkpoint

Clean source `3a06914` builds both architectures without warnings/errors and
passes 89 regressions and token checks. Its published x64 catalog passes all
1,030 captures in 356.4500522 seconds under the unchanged 360-second limit,
with all 174 protected glyph hashes unchanged and no failed input/presentation
gates. The published 36-state scroll replay also passes.

A fresh NuGet consumer passes both themes after its original pagination and
row controls are made visible and their actual observer/rendering readiness is
checked. Earlier offscreen placement, focus and observer failures remain
retained. The library and catalog did not change after the full run. Package
hashes, source correspondence, failure paths, consumer evidence and remaining
acceptance gaps are recorded in the
[owned chooser checkpoint](data-table-pagination-reference.md#owned-chooser-artifact-checkpoint).
Push-triggered Actions remain paused; native ARM64 runtime is unverified and no
inventory entry is accepted.

## Pagination rapid key-pair evidence

The same frozen keyboard reference now passes a separate native two-event
batch replay: 136 states, 120 rapid down/up pairs and 138 captures. The original
held-key mode also passes all 136 states and its exact owner gates. Both
architecture builds, 89 regressions and token checks pass. Raw release routes
still differ from the browser; this evidence establishes the functional rapid
path while leaving route equivalence and component acceptance open. Paths,
timing observations and source/native owner records are documented in the
[rapid key-pair replay](data-table-pagination-reference.md#rapid-native-key-pair-replay).
The validated library/package checkpoint is unchanged. Push-triggered Actions
remain paused; the new regression is registered for future PR/manual runs.

## Column Toggle artifact checkpoint

Clean source `32e3200` builds both architectures without warnings/errors and
passes 91 regressions and the token check. Its published x64 catalog passes
all 1,032 captures in 356.0196733 seconds under the unchanged 360-second limit,
preserving all 1,030 previous names and all 174 protected glyph hashes. Input
and presentation audits report no failed gates. The actual published focused
replay passes all 132 states, 44 key pairs and 48 pointer boundaries; raw
source/native focus and pixel differences remain open.

A fresh isolated x64 NuGet consumer passes both themes through original
visibility actions and visible rows, with disabled rejection and no observers
after unload. ARM64 builds only; its current runtime is unverified. Both
consumer caches are removed and the fresh build worktree is archived. Package
hashes, source correspondence, retained failures and artifacts are recorded in
the [Column Toggle checkpoint](data-table-column-toggle.md#validated-artifact-checkpoint).
The inventory remains 52 in progress, 60 pending and zero accepted. The public
wiki remains unavailable. Push-triggered Actions remain paused.

## Shared Select artifact checkpoint

Clean source `f2ea231` extracts Pagination's native selector into a reusable
internal surface and preserves the real trigger press/release across popup
opening. Both architecture builds, 93 release regressions and token checks
pass. The published catalog passes held/rapid keyboard, source scroll, all
132 producer states, physical input and the full 1,032-capture sequence in
354.7255073 seconds. All preceding capture names and 174 protected glyph
hashes are preserved; native input/presentation audits have no failed gates.

The fresh NuGet consumer passes x64 runtime in both themes and ARM64 builds.
Package/source correspondence, hashes, artifact paths and outstanding exact
layout/raster/release-route acceptance are recorded in the
[shared Select checkpoint](shared-select.md#verified-artifact-checkpoint).
The Filters prototype remains local with failed modal-input and missing-column
focus verification, and is excluded from this released checkpoint. Inventory
acceptance remains zero. The public wiki is unavailable and push-triggered
Actions remain paused.

## Data Table Filters artifact checkpoint

Clean product source `877625e` builds x64 and ARM64 without warnings/errors
and passes 94 regressions plus token checks. The actual published x64 catalog
passes the 276-state desktop filter replay and all 1,034 full-sequence captures
in 357.0138124 seconds. All 1,032 prior names and 174 protected glyph hashes
are preserved; input/presentation audits have no failed gates. Both themes
pass original consumer ownership, disabled/stale rejection and lifecycle
cleanup. Exact source raster, four focus differences and raw event-route
parity remain open.

The fresh isolated x64 NuGet consumer passes both themes, including native
Select keyboard commits and visible original filtered rows. ARM64 consumer
builds only. Published/package bytes, hashes, artifacts, audit paths and the
remaining acceptance are recorded in the
[Filters checkpoint](data-table-filters-reference.md#verified-artifact-checkpoint).
The inventory is 53 in progress, 59 pending and zero accepted. The public wiki
remains unavailable, and push-triggered Actions remain paused.

## Keyed Select live-update checkpoint

Clean source `65b1dcb` passes x64/ARM64 builds, 97 regressions, token checks,
all seven focused published Select/Pagination modes and fresh isolated package
consumers (ARM64 build only). The additional 140-state/32-update source replay
matches both settled focus and retained-node identity with no refresh write;
original Label/Value/disabled notifications and removed-key providers pass.
The unchanged 276-state Filters and Pagination boundaries still pass.

The actual published full run passes 1,034 captures in 358.3080684 seconds,
preserving all 1,032 preceding names and 174 protected glyph hashes. Native
input/presentation audits have no failed gates, and strengthened full checks
verify both-theme keyed/live lifecycle behavior. Current artifact hashes,
consumer evidence, image/route gaps and source correspondence are in the
[live-update artifact checkpoint](data-table-filters-reference.md#validated-live-update-artifacts).
No component is fully accepted; the inventory remains 53 in progress and
59 pending. The public wiki is unavailable and push-triggered Actions remain
paused.

## Local overlay shadow checkpoint

Source `c7ffe78` passes clean x64/ARM64 builds, 98 regressions, token checks,
nine published focused modes and fresh isolated NuGet consumers (ARM64 build
only). The original 276 Filters states verify both live Gaussian layers and
98 cleared closures; all 114 open frames reduce full-client source RGBA error.
Native menu resize/corner checks retain the loaded graph and brush identities.

Full validation remains unsuccessful: the preceding source timed out after
1,025 captures, and two optimized-source runs stop at the Sparkline area
focus assertion after 274 captures. Matching browser probes reproduce the
hovered source value while the keyboard-only fixture expects another value.
The [overlay checkpoint](overlay-shadow.md#local-artifact-checkpoint-full-gate-remains-open)
records hashes, failures, source observations and remaining work. This is
local progress, with no successful full-runtime or throughput claim. The
new commits remain unpushed; Actions remain paused and the public wiki is
unavailable. No component is fully accepted.
## Stepper continuation checkpoint

The latest full x64 run at library/catalog source `14bc3ef` passes 1,080
captures in 404.7333773 seconds. All previous 1,034 names and 174 protected
glyph hashes are preserved. Clean x64/ARM64 builds, 102 unit tests, token
generation and the fresh x64 NuGet consumer pass; the ARM64 consumer compiles
without a runtime claim. Stepper stays in progress: 42 of 44 unmasked source
images differ at zero tolerance. The fixed inventory now has 54 in progress,
58 pending and zero accepted. See the
[current checkpoint](native-validation-checkpoint.md) for artifact hashes,
input audits and remaining acceptance. Earlier entries retain historical scope.

## Stepper source motion checkpoint

Source `05c5b2d` corrects immediate press scale, legacy sRGB interpolation and
live theme/callback color-transition continuity. The pinned browser capture has
42 status/theme cases, four trusted Space interactions and two callback-removal
probes. The published native enabled/reduced matrix passes 768 frames, all four
owned key pairs and restoration of the original Windows animation preference.
All 104 unit tests, token generation, x64/ARM64 builds and the 1,080-capture
x64 full sequence pass; full time is 404.2454108 seconds. All prior names and
174 protected glyph hashes remain. Exact Stepper images still differ in 42/44
cases, and the component remains in progress. The
[current native checkpoint](native-validation-checkpoint.md) records the
Tooltip competing-hover diagnosis, unchanged deadlines, package evidence and
remaining desktop acceptance. Push-triggered Actions remain paused.

## Stepper text baseline checkpoint

At `b59cef4`, all 324 nonempty paragraph/ordinal baselines match the captured
browser across 44 cases, within 0.000016 DIP. Native nearest-pixel rounding had
placed labels/descriptions one pixel low; floor placement and explicit ordinal
line boxes correct this. The full published x64 sequence passes 1,080 captures
in 406.1895514 seconds and preserves all previous names and 174 glyph hashes.
All 105 unit tests, token generation and x64/ARM64 builds pass. Published motion
passes both Windows preference modes and restores the original setting. The
image comparison retains zero tolerance and no masks; 42/44 cases still differ,
with 143,878 differing pixels and 11,026,803 absolute RGBA error. The component
and overall goal remain in progress; see the
[current checkpoint](native-validation-checkpoint.md) for artifacts and gaps.

## DataList implementation checkpoint

Source `a7dc3ca` adds consumer-owned DataList content/templates, stripes,
viewport-responsive columns and native list/item semantics. All 72 published
source geometry cases pass across both themes and widths 639, 640 and 1024,
including native actions, bindings, lifecycle and real breakpoint changes.
Exact images remain open: 66/72 differ without masks or tolerance. The full
published x64 sequence passes 1,104 captures in 437.5147483 seconds, preserving
all previous names and 174 protected glyph hashes. Both architecture builds,
108 unit tests and token generation pass. The ARM64 consumer has build-only
evidence. At that checkpoint, the x64 consumer passes an instrumented attempt
after two unresolved Bar Chart focus failures. The subsequent focus-owner repair
has separate native/package evidence in the
[current checkpoint](native-validation-checkpoint.md).
Inventory: 55 in progress, 57 pending, zero accepted. Push-triggered Actions
remain paused and the goal remains active.

## DataList first-line baseline checkpoint

Source `82d7d34` corrects scalar text placement with measured font metrics and
line-relative quantization, retaining compact rows' half-DIP origins. All 396
scalar first-line baselines pass across the 72-case published x64 matrix,
within 0.0000124 DIP. The original renderer fails all 396 checks. Consumer
controls keep their own layout. The full published x64 sequence passes 1,104
captures in 435.2680508 seconds, retaining every prior name and 174 protected
glyph hashes. All 324 Stepper baselines remain within 0.0000153 DIP.

Both clean architecture builds, 109 unit tests and token generation pass.
The fresh x64 package consumer passes both themes and native actions; ARM64
has build-only evidence. Consumer caches are removed. Exact images still
differ in 66/72 cases: 2,496,401 pixels and 76,288,484 absolute RGBA error,
with zero masks/tolerance and no resampling. The first postprocessing run
rejected the full report's schema; explicit full-suite audit mode now passes
and rejects mismatched report modes. See the
[current checkpoint](native-validation-checkpoint.md) for separate binary,
archive and platform evidence. Inventory remains 55 in progress, 57 pending,
zero accepted. Push-triggered Actions remain paused; the goal stays active.

## Timeline implementation checkpoint

Source `1f85bbc` adds the frozen Timeline contract, original icon content and
templates, default/explicit statuses, responsive timestamps and keyed metadata.
The published x64 matrix passes 72 cases and 534 scalar/ordinal baselines at
the original 0.05-DIP bound. It verifies 66 exact crops of retained foreground
client images, restoration and four rejected invalid-input cases. All 112 unit
tests, token generation and clean x64/ARM64 builds pass. ARM64 package/runtime
scope remains build-only. The fresh independent x64 package consumer passes
both themes and original native Timeline actions; both consumer caches are removed.

The complete published x64 sequence passes 1,128 captures in 447.8767607 seconds,
retaining all 1,104 previous names and 174 protected glyph hashes. The audit
verifies 2,006 pointer moves/presentation gates, 1,752 owned key pairs and
72 physical clicks, plus the unchanged Stepper/DataList baseline checks. Exact
Timeline images still differ in 66/72 cases: 438,044 pixels and 34,943,600 RGBA
error with zero tolerance/masks and no resampling. Interactive-icon/focus,
arbitrary-content/template, complete override/scaling, contrast/Narrator and
native ARM64 matrices remain open. Inventory is now 56 in progress, 56 pending,
zero accepted. See [the current checkpoint](native-validation-checkpoint.md)
for package and release evidence. Push-triggered Actions stay paused.

## Loading Overlay implementation checkpoint

Source `d4067ea` adds the caller-owned viewport portal, large spinner, label,
two-DIP backdrop blur and canonical overlay shadows. The published x64 matrix
passes 60 cases and 42 first-line baselines across three viewports and both
themes. Native opening focus, Space/Enter, pointer blocking, Escape, pointer
focus clearing, subsequent Tab/hide and root value/live-binding restoration pass.
Four negative audits reject invalid geometry, focus and physical pointer evidence.
All 113 unit tests, token generation and clean x64/ARM64 builds pass. The fresh
independent x64 NuGet consumer passes both themes and native actions; ARM64
consumption is build-only. Both temporary consumer caches are removed.

The complete published x64 sequence passes 1,130 captures in 463.9042732 seconds,
retaining all 1,128 earlier names and 174 protected glyph hashes. It verifies
2,006 pointer moves/presentation gates, 1,754 owned key pairs and 72 real clicks,
plus the existing Timeline, Stepper and DataList baseline gates. Exact overlay
frames differ in all 60 cases: 3,041,308 pixels and 38,254,621 RGBA error, with
zero masks/tolerance and no resampling; native/browser underlying buttons also
differ. Complete visual/motion, lifecycle/theme/overrides, scaling, contrast,
Narrator and native ARM64 acceptance remain open. Inventory is 57 in progress,
55 pending, zero accepted. See [release evidence](native-validation-checkpoint.md).
Push-triggered Actions remain paused; no hosted job was dispatched.

## Loading Overlay root and presentation correction

Source `104203e` fixes Page-root tab-stop restoration; `5550af0` pins the portal
during render-only ancestor motion. The new native regression rejects the
original release DLL on the exact Page-default failure, while 14 lifecycle
phases and a 22-frame scroll/transform check pass with the published correction.
The clean x64 full suite passes 1,130 captures in 453.355999 seconds, preserving
all prior names and 174 glyph hashes. It verifies 2,006 pointer moves/gates,
1,754 owned key pairs and 72 clicks. Existing Timeline, Stepper and DataList
baseline checks remain green. All 113 unit tests, the 60-case source matrix,
clean architecture builds and fresh x64 package consumer pass. ARM64 consumers
build only; both temporary caches are removed. Exact visual/motion, complete
custom/cross-window/resource behavior, scaling, contrast, Narrator and native
ARM64 acceptance remain open. See [release evidence](native-validation-checkpoint.md).
The previous 346-second full viewport failure remains retained; the corrected
run passes without changing bounds or deadlines. Actions stay paused, and the
code push has zero hosted runs. Inventory remains 57 started, 55 pending,
zero accepted.

## FilterChip foundation and native input evidence (2026-10-16)

Implementation `e0cae0d` adds caller-owned FilterChip/FilterGroup controls;
`9ffb912` stabilizes catalog removal routing and focused input evidence. Forty
source/native cases at 320/640 DIPs and both themes pass geometry, 72 baselines,
normal colors, optional actions, empty output and wrapping. Live commands,
parameters, disablement, collection reorder/focus, palette changes, template
replacement, remounting and synchronous UIA removal pass. Five deliberately
corrupted evidence sets are rejected. All 115 unit tests and token checks pass.

The first full run's missing release receipt is retained. The corrected full
sequence passes with 1,132 PNGs, no missing previous captures and 174 protected
glyph hashes unchanged. Its 2,010 pointer gates, 1,756 owned key actions and 76
clicks pass; maximum readiness is 207.3863 ms. A fresh x64 NuGet consumer passes
both themes and real keyboard actions. ARM64 publication and an isolated package
consumer build pass; native ARM64 runtime remains unverified. Both temporary
package caches are removed. Every x64/ARM64 catalog ZIP entry is checked.

The unmodified 40-case client pixel comparison finds 42,338 differing pixels,
so raster parity and component acceptance remain false. Complete motion,
customization, scaling, contrast/Narrator and native ARM64 acceptance remain
open. Inventory is 58 started (51.8%), 54 pending and zero accepted of 112.
Push-triggered Actions remain paused; no hosted run was dispatched. The public
wiki is unavailable. See [checkpoint and artifact hashes](native-validation-checkpoint.md)
and [FilterChip contract](filter-chip.md).

## FilterChip live font-style measurements (2026-10-16)

At `8fa91ab`, live Segoe UI italic/style/stretch changes refresh every label,
value, Clear all width and baseline probe. The regression first reproduced a
2.1667-DIP Clear all width discrepancy. Twenty native phases and 100 independent
text-width measurements now pass exactly; four deliberate corruptions are
rejected. Both appearances retain native actions, RTL ordering, constrained
wrapping, multilingual content, restoration and remounting. The 40 default
reference cases and all 115 unit tests pass. Only four library method bodies
change; the other 5,963 are unchanged or exposed through equivalent WinRT
providers. Both architecture publications pass and 5,967 resolved method bodies
match. A fresh x64 NuGet consumer passes the italic-width and native action
checks; ARM64 is build-only. Both isolated caches are removed.

Strict equality with earlier default frames remains false: 291 pixels differ,
with maximum channel difference three. No masks, resampling or Windows preference
changes were used. Actual Windows scaling, complete raster/motion/customization,
contrast/Narrator and native ARM64 acceptance remain open. The earlier full
1,132-capture result is scoped to the previous candidate. Inventory remains
58 started, 54 pending, zero accepted. Push-triggered Actions remain paused.
See [current checkpoint and artifact hashes](native-validation-checkpoint.md).

## FilterChip external UIA and keyboard order (2026-10-16)

Catalog/test source `fd3c46b` adds a separate process/window-scoped Microsoft UI
Automation client. Forty-six external Control-view snapshots pass across both
themes: native names/roles, live bindings and labels, explicit-name priority,
Invoke, disabled commands/groups, localized Clear all, reorder focus/order,
template and collection replacement, empty output and unload/remount. Read-only
content remains exposed without remove actions. Twenty-eight physical Tab
transfers retain owned press/release pairs, including four Shift+Tab transfers.
Six external invocations reach original consumers; six disabled invocations are
rejected. Five deliberately corrupted evidence sets fail verification.

Both architecture builds, all 115 unit tests and token checks pass. All 5,967
resolved public-library methods/interfaces match the previous typography
candidate and across architectures, allowing equivalent generated WinRT provider
aliases. The public implementation remains `8fa91ab`; its existing versioned
NuGet and independent consumer evidence remain applicable. ARM64 runtime remains
unverified. Current populated and post-lifecycle screenshots are retained, and
every versioned catalog ZIP entry is verified.

Narrator, accessibility events, stale-client invocation, actual Windows scaling
and contrast, full raster/motion/customization and native ARM64 acceptance stay
open. Inventory remains 58 started, 54 pending and zero accepted. Push Actions
remain paused. See [checkpoint and hashes](native-validation-checkpoint.md).

## FilterChip property notifications and retained UIA patterns (2026-10-16)

Source `b833a05` fixes missing resolved-name and effective enabled-state property
notifications. Fresh UIA queries had returned the new values, but persistent
clients received no event. The standard native button peer remains authoritative
for names and states. Hidden controls keep lazy peers; linked-label observation
ends on detach, and ordinary names require no layout observer.

Two long-lived MTA clients pass 28 ordered event checks, including two reference
transport checks. Maximum delivery is 66.6675 ms under the two-second deadline.
Twenty-two original retained-pattern invocations pass: eight current invocations
reach consumers once, while fourteen inactive invocations are rejected without
writes. Tests span command/parent disablement, template replacement, model
removal, empty groups, unload and remount. Five corruptions are rejected. The
46-tree UIA, 28-Tab, 20-phase typography and 40-case source regressions pass,
as do all 115 unit tests and token checks. Both architecture builds pass and
5,977 resolved methods/interfaces match. A fresh x64 NuGet consumer passes;
ARM64 remains build-only. Both isolated package caches are removed and both
catalog ZIPs are verified entry by entry.

Narrator speech, broader accessibility, full raster/motion/customization,
Windows scaling/contrast, collection performance and native ARM64 acceptance
stay open. No Windows setting changed. Inventory is 58 started, 54 pending,
zero accepted. Actions remain paused for pushes. See [checkpoint and hashes](native-validation-checkpoint.md).


## Search Input initial native checkpoint

Source `1688886` composes the retained editor and independent consumer-owned
clear button. Forty source/native cases pass at 320/640 in Light/Dark with zero
geometry error. Actual input covers 56 key pairs, four Tab transfers and 20
clicks, including native clipboard, selection, maximum length, Escape and
immediate two-way binding. Twenty-four independent external UIA snapshots
verify Edit/Value/Text and Button/Invoke contracts. Six corruptions are rejected.
The catalog consumer passes both themes; 116 unit tests and token checks pass.
Both architecture builds pass and all 6,054 resolved methods/interfaces match.
The shared FilterChip geometry and persistent UIA event regressions also pass.

Fresh x64 NuGet runtime passes both themes; ARM64 remains build-only. Temporary
isolated caches are removed. Versioned package and both runnable catalog ZIPs
are verified, with hashes in [the checkpoint](native-validation-checkpoint.md).
Corrected strict full-frame comparison retains 101,288 differing pixels, so exact raster
parity remains false. Full state/motion/customization, broader accessibility,
actual scaling/contrast, Narrator, collection performance and native ARM64 stay
open. No full catalog rerun or workflow dispatch occurred. The fixed inventory
is 59 in progress, 53 pending, zero accepted. Push-triggered Actions remain
paused. See [Search Input](search-input.md).


## Input fractional alpha and source paint timing

Source `1aaf782` preserves fractional CSS brush alpha and uses the shared
premultiplied OKLab transition clock. Forty frozen browser transitions supply
360 background/border and 180 opacity samples. All 117 unit tests and token
checks pass. Native Input, Search Input at both widths, and the toolbar consumer
pass. The new Search Input audit requires exact source RGB and fractional
alpha/opacity values within native float precision, retaining zero geometry
error, all input/UIA/lifecycle evidence and eight corruption checks. Strict
40-frame raster error improves from 101,288 to 83,452 differing pixels with
zero masks/resampling. Exact raster remains false.

Both architecture builds pass; all 6,057 resolved methods/interfaces match.
Fresh x64 NuGet runtime passes both themes and ARM64 consumer build passes;
both temporary isolated caches are removed. Package and tested x64 library
bytes match, and every catalog archive entry is verified. See [checkpoint
hashes](native-validation-checkpoint.md). No full catalog rerun or hosted
workflow occurred. Native enabled-motion presentation, complete raster/state
matrices, actual scaling/contrast, Narrator, collection performance and current
native ARM64 acceptance remain open. The goal stays active at 59 in progress,
53 pending and zero accepted. Push-triggered Actions remain paused.


## Search Input activation and visual states

Source `7df5542` fixes Enter timing/appearance, pressed-edge release ownership,
scaled keyboard focus and Dark hover's shared sand color. Forty-eight frozen
browser/native cases pass at both widths and themes, with maximum geometry
error .00006103515625 DIP, exact source RGB and fractional alpha, 20 held key
pairs, eight held pointer pairs, four live palette/theme checks and 68 rendered
paint gates. Eight corrupted reports are rejected. The original 40 cases remain
zero-error in geometry. The catalog, 118 unit tests, tokens, FilterChip geometry
and its 28-event / 22-retained-invocation regression pass. Both architecture
builds match across 6,062 resolved methods/interfaces. Fresh x64 NuGet runtime
and ARM64 build-only consumers pass, and their temporary caches are removed.

The new 48-state raster comparison retains 142,074 differing pixels; the
separate original matrix retains 83,452. No masks/resampling are used. Full
raster, native enabled-motion timelines, broader pointer/customization cases,
actual scaling/contrast, Narrator, performance and current native ARM64 remain
open. No full catalog rerun, Windows preference change or Actions dispatch
occurred. Push-triggered Actions remain paused. Inventory is unchanged at 59
in progress, 53 pending and zero accepted. Versioned hashes and evidence are
in [the checkpoint](native-validation-checkpoint.md).

## Button template retirement — October 24, 2026

The current local candidate is library `c6508753` and NuGet
`0.1.0-button-lifecycle-v1-local` (package SHA-256
`be9ede5e58f2c95cb7e4eaf400bb2d0ba41590c6317bbfe1cb537db066a0f03c`).
Clearing a BleeckerButton template previously let ContentControl synthesize a
text-only template without another property notification; the wrapper then
threw because that native template has no PART_Action. The repair remembers the
explicit clear operation, releases its old parts/palette/event handlers and
spinner, and resumes normal part initialization after a caller restores a
template or clears the local template value. Caller bindings and native command
behavior remain intact.

The source catalog and fresh isolated x64 package consumer each pass 448 hidden
native checks across seven variants, both themes and default/custom templates:
896 checks, 56 cases and 168 images. Each host rejects 52 damaged reports. The
first retained original-library run mounts and activates successfully before its
actual null-template exception. An early repaired fixture incorrectly expected
unloaded native Invoke to suppress its command; its 28 expectation failures are
retained. Native commands remain unchanged, and retired parts do not forward
Orchard Click events. Default-template restoration, loading retirement and owned
spinner reset are checked. All 84 source/package button image pairs and the one
original-library initial button image have identical RGBA pixels.

All 495 portable and 10 Windows text tests pass; root and fresh-version package
consumer builds pass for x64 and ARM64. The fresh C# package probe passes ten
resource/style/template samples and twelve spacing-binding samples. Source and
package ButtonGroup checks pass (22 and 20); their six common images match.
Current live tables pass 84 states per host with identical raw states/contracts.
Four of their 84 source/package image pairs each differ by one pixel and one
channel level; this remains unexplained. Source fictional Monitoring and
Editorial demos pass 66 checks. These runs do not qualify current table timing
performance: the last qualified `2dabe1ac` table library has 26 failing timing
profiles and earlier unresolved image differences.

The new self-contained catalog archive has 550 verified files: 548 link only to
verified immutable evidence and two are freshly extracted. Its native replay
started at 17:45:11 UTC in PID 33576 and passed 128 checks before making no further
layout/capture progress in Light/Ghost/default. No assertion failure is recorded,
and the process was nonterminal at that observation. A three-second sampled-thread-time trace
and read-only session checks are retained. Both owned hidden windows respond;
one repaint and one one-pixel resize-and-restore of the main test window did not
advance the run. Bounds, hidden state and foreground ownership remained unchanged.
No process was killed or restarted, hardware input sent, or OS setting changed.
At that working-checkpoint observation, archive lifecycle, integration and demo
replays remained pending. The subsequent lifetime diagnosis and completed
corrected replays are recorded below; that older observation does not describe
the current process state. A timeout alone never justified restarting it.

The explicit-null behavior was checked against installed WinUI 2.3.9 under
WindowsAppSDK 2.5.1 and the actual native transitions. The retained upstream main
ContentControl source explains its text-template synthesis but is not asserted
to be the exact pinned source revision. The working checkpoint and framework
reference distinguish these evidence scopes. Native ARM64 execution, full
browser/native raster parity, normal motion, physical input and scaling remain
open. All 112 entries and both fictional-data demos remain in progress; none is
accepted. The goal remains exclusively the reusable component library and demos.

## Retained native capture ownership and completed button delivery — October 24

The archive stall was a fixture ownership defect, not evidence of a Button
failure. Two ClrMD snapshots of the same owned PID found its Layout state waiting
on the RenderAsync task while the UI thread sat in WinUI's message loop. No
RenderTargetBitmap object remained in the managed heap, and the generated await
state did not retain one. An isolated hidden probe on the exact pinned WinUI DLL
reproduced three unretained owners whose native actions stayed Started after two
subsequent retained frames completed. All seven retained captures completed.
The native probe used .NET 10.0.12 and Microsoft.UI.Xaml SHA-256 aad12524….
Upstream main explains the non-owning manager list, but is not claimed to be the
exact pinned native source revision.

After establishing this orphaned-operation diagnosis, the owned hidden main
window of PID 33576 was closed gracefully at 18:37:32 UTC. Its original driver
recorded exit 0 without a final test report; that aborted run is not accepted.
This was not a restart caused solely by an observation timeout. The snapshots,
probe, 128 completed checks and shutdown rationale remain retained.

Only two readiness-only calls had this lifetime gap. Shared
NativeRenderReadiness now keeps its bitmap alive through the native await;
normal component captures that read pixels afterward already retain theirs.
The helper adds no forced collection to ordinary runs. A maintained lifetime
fixture forces collection only for its diagnostic cases and fails before
awaiting if the required owner was lost. An initial extra nonzero-dimension
assertion rejected the intentionally empty layout root; that assertion was
removed, and its failed report is preserved.

Corrected source, independent NuGet consumer and extracted archive each pass
448 Button lifecycle checks and seven retained-frame/three unretained-owner
lifetime controls. Their 1,344 Button checks and 252 images cover all seven
variants, both themes and default/custom templates; 159 altered Button reports
and 54 altered lifetime reports are rejected. Checkbox v4 passes 72/70/72 checks
with 105 altered reports, 48 historical wrong-name states, nine historical style
failures and 24 historical unready observations rejected across the three hosts.
ButtonGroup passes 22/20/22 checks; live tables pass 84 states per host; both
fictional demos pass 66 source and 66 archive checks. The fresh C# package probe
passes ten default-style samples and twelve spacing samples.

Both root architectures and both isolated package-consumer architectures build;
495 portable and ten Windows text tests pass. The rebuilt consumers reuse the
already verified package version cache and third-party dependencies; they are
not claimed to perform another fresh-version restore. Library c6508753 and NuGet
0.1.0-button-lifecycle-v1-local remain byte unchanged. The new runnable catalog
0.1.0-button-lifecycle-render-v2-local contains 550 byte-verified files, reusing
549 immutable files and freshly extracting one PDB. No mutable output links into
immutable evidence. Current source/package/archive comparison verifies 360 of
361 image pairs exactly; one live-table source/package pair differs by one pixel
and one channel level. All button, Checkbox and connected-group pairs match.
The unexplained table difference retains its failing exact-pixel classification.

Byte-preserving hardlink coalescing of closed, manifest-verified evidence
recovered about 2.7 GB of observed free space while preserving original paths
and hashes. Automatic approval review blocked deleting the obsolete fully
archived staging folder; it remains untouched. No prior blocked deletion was
retried. The library's public implementation did not change during this capture
repair, and test assets were not externalized or given a fallback path.

Current table timing remains unqualified: the previous qualified 2dabe1ac
library had 26 failing timing profiles. Full browser/native raster, normal
motion, physical input/scaling and native ARM64 runtime acceptance remain open.
All 112 components and both required fictional-data demos remain in progress,
zero accepted. Work stays within the reusable component library and its demos;
no Narrator, OS settings, pointer heartbeat, GitHub Actions or pushes were used.
