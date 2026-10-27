# Fixed Windows parity inventory

Current scope is exclusively the component library, including the monitoring and
editorial component demos. The [user-confirmed scope](library-scope.md) supersedes
older Narrator and OS contrast acceptance requirements below; those test
campaigns are excluded. The user has resumed component-library work.

Use the [acceptance contract](acceptance-contract.md) to assess completion against
the original brief and subsequent native behavior/token corrections. The strict
image diagnostics below keep their zero thresholds and actual results. An exact
image result alone cannot establish component acceptance, and intentional user
corrections must be verified against their native expectations. All 112 entries
and both compositions remain in progress.

The 2026-10-25 inventory review corrects 30 stale exact-raster gate descriptions
across 30 entries to require visual and behavior review under that contract.
All recorded pixel counts, unresolved focus/route differences, motion/scaling
and runtime requirements, and failed strict diagnostics remain. No component or
composition becomes accepted through this wording correction.

The [v8 x64 catalog file set](evidence/components-v8-linked-catalog-v1/checkpoint.json)
is archive-identical in a separate low-disk runnable directory. Monitoring and
Editorial demos plus Icon Button, Tooltip and Modal focused physical paths pass
in Light and Dark. This verifies those specific native outcomes, not full
component acceptance, fresh ZIP extraction, clean-cache/self-contained NuGet
delivery or native ARM64 runtime.

The [exact v8 DLL also passes a focused x64 runtime smoke](evidence/components-v8-self-contained-overlay-v1/checkpoint.json)
inside a previously published independent self-contained consumer host. Its
unchanged package assets and host files are verified, but this overlay is not
a fresh v8 restore or publish. The v8 package remains unqualified.

The [matching v8 source also builds for ARM64](evidence/components-v8-linked-arm64-v1/checkpoint.json)
with zero warnings or errors. A separate manifest verifies all 660 files in a
versioned ARM64 catalog directory, including five new ARM64 binaries. It has
not run on native ARM64 hardware or been packaged as a standalone v8 ZIP.
An [independent consumer of the exact v8 NuGet package](evidence/components-v8-arm64-consumer-compile-v1/checkpoint.json)
also restores from the shared cache and cross-compiles for ARM64 with zero
warnings or errors. Its apphost is ARM64 and its Orchard DLL matches the
package. Clean-cache and self-contained ARM64 delivery and native runtime
remain unverified.

The Checkbox v3 hidden fixture now asserts native readiness after rendering;
all shared source/consumer/archive raw checks agree, including `IsLoaded`.
This corrects the earlier harness timing, with no library change. The existing
NuGet is retained and the catalog fixture is refreshed; see
[runtime evidence](runtime-evidence.md#hidden-checkbox-readiness-correction-2026-10-24).
This does not accept an inventory entry or close the existing performance,
visual, scaling, motion or native ARM64 gaps.

The 2026-10-24 follow-up corrects the collection benchmark's ownership of retired
native rows and cells and adds explicit three-process budgets. Three fresh v3
runs pass all 360 semantic/lifecycle operations, but the unchanged library fails
26 of 120 median profile budgets. Three measurement optimizations were preserved
and reverted after mixed timings. All 566 library source hashes remain unchanged;
see [the performance evidence](data-table.md#current-performance-acceptance-budgets).

The shared overlay tint correction is now packaged as local preview
`0.1.0-overlay-tint-v1-local`. Its independent paint fixture retains 12 captures
and 6,572 covered source pixels; all three hidden hosts match the 1,398 covered
native paint pixels and pass 16 shadow cases, 34 PNG checks and 26 lifecycle
checks, with 39 rejected corruptions each. Root and fresh-consumer x64/ARM64
builds pass, alongside 495 portable and 10 Windows text tests. Source, package
and archive repeat the full Command Spotlight, fill/backdrop, refresh/collection
and shared menu/toast suites; both fictional demos pass in source and archive.
Original browser/native images still differ at 7,769,949 pixels/max239;
source/package at 77/max6, source/archive at 48/max6. Every image
threshold remains zero. The shadow image comparison still fails, including a
small isolated far-layer regression. All inventory acceptance remains open.
See [source paint encoding](overlay-shadow.md#source-paint-encoding) and
`artifacts/current/overlay-shadow-tint-checkpoint-v1.json`. Older preview results
below are historical checkpoints.

The newer [near-shadow shader investigation](overlay-shadow-near.md) matches
48 complete colored/mask source images in browser GPU rendering and in native
offscreen and two hidden Composition paths, across radii 7, 14 and 28.
All 15 native intermediate buffers match the browser GPU bytes. Those initial
results used the delivered tint library unchanged. The later library previews
below address integration and lifetime; no inventory entry is accepted by these
focused results.

The subsequent real-component attachment retains exact near pixels across 48
cases and 12 live geometry updates, with three bound paints and all 15
intermediate buffers checked. Six far-layer clip relocations are byte-identical.
Its combined PNG gate still fails at 145,582 pixels. This is an externally driven
prototype; library event/cache ownership, recovery, scaling and full raster
acceptance remain open. See the [component evidence](overlay-shadow-near.md#attachment-to-the-actual-component).

The newer [internal ownership preview](overlay-shadow-near.md#internal-library-ownership-preview)
puts automatic refresh and shared lifetime in the library and passes the same
physical source cases in source, NuGet and extracted-catalog runs. Concurrent
owners, template replacement, detach and five rejected-geometry rollbacks pass;
63 corrupted-evidence cases are rejected. Fresh 495 portable/10 Windows text,
both architecture builds and the package's 100-state/168-hotkey default contract
pass. The original near PNG gate retains 6,006 differences. Default adoption,
active-radius eviction, suppression, geometry/scaling and real device loss remain open.

The subsequent local `0.1.0-overlay-near-leases-v1-local` preview passes active-radius
eviction, concurrent different-radius ownership and five suspension/restoration
transitions. Its diagnostic PNG exporter uses the pinned, injective browser codec;
both physical bytes and original near PNG RGBA now match at zero tolerance in
source, NuGet and extracted catalog (19,672,000 compared pixels per host). All 99
retained native raw frames are unchanged from the preceding preview. The auditor
rejects 80 corruptions. Fresh 495 portable/10 Windows text tests, both solution
and consumer architectures, the three 100-state/168-hotkey default replays and
both fictional-demo replays pass. The default renderer stays unchanged. Full/far
raster, broader geometry/scaling, actual device loss, measured costs and native
ARM64 runtime remain open. See the [follow-up evidence](overlay-shadow-near.md#active-radius-and-diagnostic-png-follow-up).

The [broader geometry sweep](overlay-shadow-near.md#broader-geometry-regression-sweep)
adds 103 repeatable source images, 31 exact native supported cases and 40 explicit
rollback cases for the internal primitive, with 46 rejected corruptions. Broader
fractional/small/unequal/transformed rendering remains research; the library and
NuGet renderer bytes are unchanged by this regression work.

The subsequent internal fractional preview corrects final cache sampling and
shadow-offset handling. Browser GPU models match all 43 eligible geometry and
56 positive-radius overlap cases. The native overlap fixture retains its graph
and shared ceiling-radius kernel across all 56 live radius roundtrips, but two
one-level alpha pixels still fail in one transparent case (54/56 images exact).
Those gates remain false at zero tolerance; the public default renderer and all
inventory acceptance remain unchanged. See the [current investigation](overlay-shadow-near.md#fractional-radius-implementation-under-validation).
Source, fresh NuGet and extracted catalog reproduce these same results. Both
architecture builds and consumers, 495 portable/10 text tests, the three default
100-state/168-hotkey replays and both fictional-demo replays pass. The remaining
far/combined raster gates still fail at 75,924/76,743 pixels per host.

The source commits below freeze this goal. Upstream updates require explicit scope review.

Textarea's [source contract and native preview](textarea-reference.md) cover 172
browser and native state frames, multiline editing, consumer binding, pointer
resizing, paste limits and real context commands. Full catalog integration
passes; remaining visual/platform acceptance evidence keeps it in progress.
Counts are 112 in progress, zero pending and zero fully accepted: 100% of the
fixed inventory has entries started. This is not a completion percentage.
The [shared overlay-shadow correction](overlay-shadow.md) now passes a hidden
x64 source fixture across 16 background/theme cases, 32 on/off PNGs and 26
lifecycle cases. Transparent/null backgrounds retain an outer shadow without
interior paint; original bindings and the loaded graph survive updates. Its
strict browser/native image gate still fails. Fresh package and extracted
catalog repeat this fixture and pass full Command Spotlight, Menubar,
Navigation Menu, Sonner and both fictional-demo regressions. Portable tests
pass 492/492; Windows text tests 10/10 and both architecture/consumer builds
pass. All 112 entries remain in progress. No inventory entry is accepted by
this focused evidence.
AuthShell's [source contract](auth-shell.md) now retains 104 original PNGs and
20 histories, an exact independent recapture with zero metadata exclusions,
and 106 rejected audit corruptions. Eight new source tests pass in the full
466-test portable preparation suite. Its public native layout and catalog are
now verified as a local preview:104 states/24 checks in source, fresh NuGet
and extracted catalog, 94 source/package audit negatives each, 469 portable and
10 Windows text tests, both architecture/consumer builds and both fictional
demos. All state/contract data match. Source/archive matches all104 PNGs exactly;
source/package retains76 differing pixels in one dark content-text image. The
strict original-browser/native gate also fails; no threshold is relaxed.
The subsequent [container/root sizing correction](auth-shell-layout.md) adds
132 source states/8 histories, closes padding overflow, centered-height and
explicit-width positioning defects, and passes132 native states/32checks in
source/freshNuGet/extractedcatalog. All132 native PNGs match exactly across
those hosts;474portable/10WindowsTexttests and both architectures/consumers/demos
pass. Browser/native raster and broader acceptance remain open.
Command Spotlight's [source preparation](command-spotlight.md) freezes 100
original screenshots and 76 interaction histories. The independent recapture
matches every canonical PNG byte for byte and all metadata, accessibility and
histories, with zero exclusions. Its auditor rejects 84 counterexamples. Eight
new source-backed tests brought the preparation suite to 450 passing tests. The
native component and catalog now have a hidden preview: 100 source-backed
states, 20 behavior checks and 28 native style states in source, fresh NuGet and
extracted catalog. Live caller maximum width, trigger height/padding/paint,
BasedOn inheritance, clearing, bindings and remounts pass. State/contract data
match across all three hosts; 94 source and 91 package corruptions are rejected.
Both solution/consumer architectures and both fictional demos pass, with 474
portable and 10 Windows text tests passing. The whole narrow
shortcut hint is hidden per the user's correction. Strict browser/native image
equality fails at 29,631,022 pixels/max239; source/package across the 128 native
states at 2,776/max10 and source/archive at 1,624/max10 (zero allowed).
Complete runtime/visual/input/
customization acceptance stays open. The frozen source retains its stray `+`.
The newer `0.1.0-command-spotlight-hotkeys-v1-local` preview additionally verifies
168 logical hotkey scenarios, 38 Windows key-name checks, eight native Unicode
search checks and 26 read-only US-layout translation checks in all three hosts.
Full producer casing covers every Unicode scalar and final-sigma context class;
independent captures match exactly. Baseline failures demonstrated four hint,
eleven key-name and four native search defects, plus 55 scalar/seven context or
UTF-16 model differences. The source/package auditors reject 117/113 cases and
the new source-data auditor rejects 21. Both architectures/consumers/demos,
484 portable and 10 Windows text tests pass. Strict original browser/native
images fail at 29,631,020/max239 and eight new hint images at 20,596/max216;
136 native image pairs fail at 2,012/max9 for source/package and 1,758/max37 for
source/archive. Thresholds remain zero; physical routing, broader layouts/IME,
rendering and full component acceptance remain open.
The newer `0.1.0-command-spotlight-surface-v1-local` preview adds 378 clipped-fill
swatches and 20 live brush/binding/alpha/opacity/replacement/refresh/disposal
cases per host, with 28 source/package audit negatives each. All full-state,
backdrop, hotkey, refresh, collection and fictional-demo regressions pass in
source, fresh NuGet and extracted catalog. Portable tests pass 490/490; Windows
text tests 10/10 and both architectures/consumers pass. Original browser/native
differing pixels fall from 9,873,633 to 7832379 with zero thresholds unchanged.
Full raster and broader component acceptance stay open.

Media Library's [source preparation](media-library.md) freezes 100 original
screenshots and 50 interaction histories. A fresh independent capture matches
all canonical PNGs, settled metadata/accessibility and histories exactly, with
no semantic exclusions. Its auditor rejects 80 corruptions; eight new
source-backed regressions bring the portable suite to 442 passing tests. The
native source, fresh NuGet and extracted catalog pass 100 states/38 checks;
83 source and 83 package corruptions are rejected. Both solution and fresh
consumer architectures build, and both fictional demos pass. State/contract data
match; strict browser/native image equality fails at 13,349,376pixels/max242,
source/package at 9,171/max102 and source/archive at 10,366/max102 (zero allowed).
Windows implements
the user's correction: pointer Delete requests deletion without opening the
preview; the caller retains ownership of the collection.
File Input's [source preparation](file-input.md) freezes 68 original source
screenshots and 22 selection/event histories. A second fresh capture matches all
68 settled PNGs byte for byte and the semantic selection histories. The auditor
rejects 83 counterexamples; five new source/reference regressions bring the
portable suite at preparation to 431 passing tests. The public native control
and catalog are now in progress; 434 portable tests pass. Hidden source checks
cover 68 states and 30 behavior checks in source, fresh NuGet and extracted
catalog. Both solution and isolated-consumer architectures build; both fictional
demos load. Native system-picker interaction remains pending. Strict browser/native component image equality
fails; no native entry is accepted. No source
capture is presented as native system-dialog or physical-input acceptance.
Sonner's [source preparation](sonner-reference.md) freezes 70 source states and
38 retained-instance histories with independent matching captures, 57 auditor
rejections and five source-backed lifetime/reference tests (418 portable tests
total). Its public native control and catalog fixture pass 66 hidden states,
20 behavior checks and 53 auditor rejection cases. The isolated x64 NuGet
consumer matches all 66 snapshots and PNGs; both fictional demos pass the shared
host regression. Both solution builds and isolated package consumers pass;
full acceptance remains open.
Hero Carousel's current [input preview](hero-carousel.md) implements the user's
owning-window arrows and inactive-slide Tab/click choice. Source, fresh NuGet
and extracted catalog pass 82 states/34 checks with identical state/contract
data; 434 portable tests, 10 Windows text tests, both architecture builds and
isolated consumers, and both fictional demos pass. Strict source/package
image equality fails at 52 pixels/max24; source/archive fails at 58 pixels/max24.
Physical routing, Tab traversal, full visual/motion/scaling and native ARM64
acceptance remain open. The prior effects evidence below remains historical.
Hero Carousel's [source contract](hero-carousel-reference.md) freezes 86 original
frames and 54 histories with independent matching captures and 90 auditor
rejections. Seven new source-backed event-state tests bring the portable suite
to 425 passing preparation tests. A new frozen-gradient regression brings the
portable suite at that stage to 426 passing tests; 10 Windows text tests also pass.
Its [public native preview](hero-carousel.md) and fictional catalog fixture
pass 82 hidden states and 30 behavior checks, with matching captured source
geometry and exact sampled palette values. The x64 build has no warnings.
The fresh Hero Carousel NuGet and both solution/isolated-consumer architectures
are verified for this preview. Caller fonts and original mutable brushes update
live; remount restores working navigation. The extracted self-contained catalog
archive also passes the native matrix and both fictional demos. Full-image
equality still fails; the same-binary archive replay also shows low-level rendering variation. The
[preview evidence and remaining work](hero-carousel.md) keep the component in
progress, with full acceptance and native ARM64 runtime unverified.
The newer v5 effects source, fresh NuGet consumer and extracted catalog pass
82 states/32 checks, live glyph/font, RTL resize, asymmetric corners and
empty/restore lifecycles. The auditors reject 72 source and 69 package
counterexamples. Pixel checks cover tag/CTA/arrow/navigation backdrops and
inherited tag shadows. Both full solution and isolated-consumer architectures,
426 portable tests, 10 Windows text tests and both fictional demos pass. The v5
strict source/package image gate remains failed: 33 differing pixels, maximum
20, with 77/82 identical PNG pairs; identical-binary archive replay differs at
40 pixels. Full component acceptance remains open.
The [OTP Input preview](otp-input.md) preserves controlled caller values, ASCII
edit requests, native text/password slots and source deletion/paste semantics.
Its source host passes 60 states and 32 behavior cases; physical input, clipboard,
scaling, complete visual/motion and ARM64 runtime acceptance remain open.
The [App Tab Bar preview](app-tab-bar.md) adds value-bound destinations, equal
tracks, caller-owned Windows icons and capped badges with raw-count metadata.
Its source host passes 58 cases and eight rendered layout checks; physical input,
actual scaling, full visual/customization and ARM64 runtime remain open.
The [Segmented Control preview](segmented-control.md) implements the pinned
value-bound choice contract. Its source host passes 40 behavior cases and eight
layouts, with rendered selected-surface checks that reject the actual earlier
RTL capture. Physical input and full visual/platform acceptance remain open.
The [Secure Field preview](secure-field.md) contains a native password editor,
persistent reveal, two-way value binding and registered fictional catalog states.
Its background source host passes 30 contract/metadata/paint/lifecycle cases.
Physical input/focus, complete visual/scaling and ARM64 runtime remain open.
Drawer and Sheet have [frozen source contracts and internal models](drawer-sheet-reference.md)
and a [native preview](drawer-sheet.md) with registered catalog examples.
The catalog and independent NuGet consumer each pass 194 measured layouts and
42 behavior cases. Full native visual/input/platform acceptance remains open.
Owned Popover/Tooltip/Select compositions also pass 30 cases across Drawer and
all Sheet sides, including retained data and teardown, in both native hosts.
Popover's [source contract](popover-reference.md) binds 122 source states and
16 histories. Its reusable [native prototype](popover.md) and catalog fixture
are implemented. Hidden native geometry, paint-value and ownership/lifecycle
checks pass; complete visual, input/focus and platform acceptance remain open.
HoverCard's [source contract](hover-card-reference.md) binds 44 states,
four histories and 16 motion timelines. Its [native prototype](hover-card.md)
and fixture pass those layouts, 132 brush checks, 96 presentation samples and
five ownership/query groups. Native diagnostics inject model/time inputs;
physical input, full visual and platform acceptance remain open.
Both required [component demos](workspace-demos.md) now have catalog fixtures,
fictional local state and interaction regression coverage. Their visible and
platform acceptance remains open independently of the 112 component entries.

The [Field preview](field.md) adds consumer-owned label/control/action/message
composition with a frozen 44-state browser reference and native UIA3 ownership
and lifecycle checks. Monitoring and editorial remain component demonstrations.
The [PageFrame preview](page-frame.md) adds centered page boundaries, typed width
and spacing recipes, native semantic grouping and preserved caller overrides.
The [DetailLayout preview](detail-layout.md) adds responsive main/rail tracks,
container-bounded sticky placement and main-before-rail keyboard order, with an
888-state reference and 48 exact full-frame native comparisons.

The date controls now have a [native preview and pinned browser contract](date-controls-reference.md):
Calendar, Date Picker and Date Range Picker share 188 measured states and 48
trusted browser interaction sequences. Native geometry covers all 188 states;
the 56 Picker/Range states add 964 box comparisons, 620 popup days and 132 basic
paint checks. Typography compares 6,076 text entries, including transformed
baselines, line heights, tracking and 4,680 tabular day entries. Interaction checks cover controlled requests, bounds,
focus/dismissal, remounts and live presets. Exact raster/shadow/motion, remaining
customization boundaries and Windows platform acceptance remain open.

Visual acceptance requires every component to match its Bleecker reference across every variant, size, theme and applicable interaction state. Compare typography, spacing, dimensions, colors, borders, radii, icons and motion against the rendered reference. Windows control defaults must not override the branded appearance, including nested popups and disabled/focus states. A mismatch is an open acceptance gap, never an approved platform substitution.

| Reference | Commit |
| --- | --- |
| gaulatti/bleecker | a86059013cd59684ce452849278ea81e734f2066 |
| gaulatti/sabella | f5ac479bfceaa7eab23c1716ac47420ac53dae25 |
| gaulatti/thompson | 30a76c849e2486ba26b15b0c0c569fbde8c0606c |
| gaulatti/newsroom | 82cca98c4d3dfd1d65509f8d8c46e8fece0ead70 |

Machine-readable contracts, public symbols, literal variants, states, evidence paths and remaining gaps are recorded in [parity-inventory.json](parity-inventory.json). No component is complete until its evidence is populated and verified. Existing foundation styles cover only part of Button, Input, Select and typography.

Every interactive entry additionally requires hover, pressed, focus, selection and loading where applicable, keyboard activation/dismissal/focus restoration, long/multiline/non-Latin content, theme changes and reduced motion. Overlays must verify open-content theme changes. Collections must preserve selection across updates and sorting. Dialogs must explicitly protect unsaved edits.

Visual acceptance requires comparison with the rendered pinned Bleecker reference across every variant, size, theme and interaction state. Match typography, spacing, dimensions, colors, borders, radii, icons and motion. Native Windows semantics may be retained, but Windows defaults must not override the branded appearance. Token assertions and successful native-control interaction alone do not establish visual parity. Existing components remain in progress until this comparison is verified, including focus, loading and animated transitions.

The available development host is Intel x64. ARM64 compilation is verified separately. Earlier native `windows-11-arm` runs passed baseline fixtures and the independent package consumer with process/OS checks; those historical runs do not verify later components or corrections. See [runtime evidence](runtime-evidence.md). The latest recorded native ARM64 sequence retains an unresolved Input exterior-cursor failure. Push-triggered Actions are paused at the user's request, so new native ARM64 runtime evidence remains pending. Full component visual, input, accessibility and scaling acceptance remains open; no local ARM64 machine is available. Narrator and OS contrast campaigns are excluded.

The visual audit tracks unresolved Windows defaults in current previews. Icon-button fixtures now use pinned Lucide vectors instead of SymbolIcon glyphs; other preview surfaces still require audit. The button loading indicator now uses the reference Lucide arc. Focus rings use Bleecker brushes and geometry, and the link uses the reference hover-only underline. The remaining focus matrix, state motion, shadows and typography still require full rendered comparison. These are tracked acceptance gaps, not approved visual substitutions. See [rendered comparison evidence](visual-audit.md).

| Component contract | Windows implementation | Catalog / regression / runtime evidence | Remaining gap |
| --- | --- | --- | --- |
| brand-lockup | `BleeckerBrandLockup`, typed consumer link context and live native image effects | Registered size/name, custom RGBA/link, disabled/command and non-Latin fixtures; focused x64 initial checks | In progress: [source contract and API](brand-lockup-reference.md); rendered parity, live/lifecycle/RTL, motion/reversal, error/override, package, scaling and ARM64 acceptance |
| brand-mark | `BleeckerBrandMark`, original template PNG and fixed square size | Registered fixture; 37 source/fresh NuGet states and identical PNGs, live brush/size bindings, image peer, both architecture builds | In progress: [source contract and API](brand-mark.md); complete visual and platform acceptance |
| bauhaus-background | `BleeckerBauhausBackground`: four variants and live native image effects | Registered RGBA, square/tall and missing-image fixtures; x64 blend-pixel, source, geometry, motion-preference and UIA checks | In progress: [source contract and API](bauhaus-background-reference.md); full raster/motion, mutations, scaling, packaging and ARM64 acceptance |
| activity-item | `BleeckerActivityItem`: exact strings, small Avatar, source typography and hover | Registered normal/empty/multilingual/RTL fixtures; frozen browser layout; x64/ARM64 pointer/Tab/UIA/lifecycle, motion-preference and independent-consumer checks pass at `64a2b27` | In progress: [source contract and API](activity-item-reference.md); baseline/raster, complete motion, constrained/override matrix/scaling |
| attention-surface | `BleeckerAttentionSurface`: five elements, densities, source color model and consumer content/actions | 143 frozen-helper/browser numeric cases; 86 initial x64 source-layout cases and native action/style/hover checks | In progress: [source contract and API](attention-surface-reference.md); full raster/motion, constraints/lifecycle/overrides, scaling, package and ARM64 acceptance |
| button | `BleeckerButton`: seven variants, four sizes, commands/content, loading | Registered fixtures; x64 native command/loading/binding, CanExecute/template replacement and 7×4 state checks in both themes; isolated NuGet consumer; ARM64 build | In progress: focus/input, scaling, motion, remaining ARM64 state checks; see [usage](buttons.md) |
| button-group | `BleeckerButtonGroup` | Horizontal/vertical catalog fixtures; published x64 light/dark layout and group-role checks | In progress: edge fixtures, child restoration, keyboard, scaling, remaining ARM64 state checks; see [usage](button-group.md) |
| card | `BleeckerCard`: five variants, four padding sizes, consumer content | Registered fixtures; native surface, wrapping, child-action and shadow-layer checks; independent consumer | In progress: exact shadows/clipping, surface motion, overrides, accessibility/scaling, remaining ARM64 state checks; see [usage](cards.md) |
| chart | `BleeckerChart`: fixed currency plot over original consumer records | Registered 94-case source fixture, real pointer/keyboard handovers and independent consumer; retained in the current published catalog | In progress: [source contract and evidence](chart-reference.md); exact raster/motion, full data/override/RTL and platform acceptance |
| chart-theme | `BleeckerChartTheme` and `BleeckerChartSeries`: scoped resource keys, source palette selection, immutable style metadata and consumer-owned series | Registered role/palette/brand, series-paint and axis/grid/tooltip surface fixture; frozen metadata regressions | In progress: [contract and API](chart-theme-reference.md); actual chart consumers, tooltip effects and full platform acceptance |
| line-chart | `BleeckerLineChart`: consumer records/series, monotone curves, grid, axes, legend and category inspection | Registered fixture; frozen curve/domain/data cases, 60 placement cases, 22 chart and 26 tooltip specimens, composed blur/shadow profiles; independent consumer | In progress: [API and evidence](line-chart-reference.md); complete visual/motion/lifecycle, tooltip raster and platform acceptance |
| sparkline | `BleeckerSparkline`: compact consumer records/key, monotone curves, optional paint and raw category inspection | Registered 36-case frozen fixture, negative-domain axes, physical input/binding/remount gates and independent consumer | In progress: [source contract and evidence](sparkline-reference.md); full raster/motion/data matrix and platform acceptance |
| stat-card | `BleeckerStatCard` and `BleeckerStatTrend`: consumer value/templates, trend/description precedence, icon, history and footer composition | Registered 80-case layout/content and 16-case typography references, composed clipping/occupied-row and native binding/input/lifecycle/template gates; independent consumer | In progress: [contract and evidence](stat-card-reference.md); exact glyph/raster/motion, full content/override and platform acceptance |
| bar-chart | `BleeckerBarChart`: consumer grouped bars, both layouts, axes, grid/legend and category inspection | Registered 68-case frozen geometry/palette/tick/cursor/tooltip fixture, source wrapping baselines and native live-data/input/lifecycle checks | In progress: [source contract and evidence](bar-chart-reference.md); full raster/motion/content/override/RTL and platform acceptance |
| area-chart | `BleeckerAreaChart`: overlapping/stacked monotone areas, ranges, axes and native inspection | Registered 80-case source fixture and live data/input/lifecycle checks | In progress: [source contract and acceptance gaps](area-chart-reference.md) |
| pie-chart | `BleeckerPieChart` and consumer-owned `BleeckerPieChartDatum` | Registered shared 84-case Pie/Donut source fixture and native lifecycle checks | In progress: [source contract and acceptance gaps](pie-chart-reference.md) |
| donut-chart | `BleeckerDonutChart`: source ring and independent center text | Shared Pie/Donut fixture | In progress: [source contract and acceptance gaps](pie-chart-reference.md) |
| scatter-chart | `BleeckerScatterChart`, typed series and independent consumer collections | Registered 64-case source fixture plus native input/data/lifecycle checks | In progress: [source contract and acceptance gaps](scatter-chart-reference.md) |
| radar-chart | `BleeckerRadarChart`, typed series, polar subjects and range boundaries | Registered 72-case source fixture and native input/data/lifecycle checks | In progress: [source contract and acceptance gaps](radar-chart-reference.md) |
| radial-bar-chart | `BleeckerRadialBarChart`, typed observable records, curved labels and consumer radii/angles | Registered 80-case source fixture and native input/data/lifecycle checks | In progress: [source contract and acceptance gaps](radial-bar-chart-reference.md) |
| funnel-chart | `BleeckerFunnelChart`, typed observable records, native trapezoids and wrapped source labels | Registered 54-case source fixture and native input/data/lifecycle checks | In progress: [source contract and acceptance gaps](funnel-chart-reference.md) |
| sankey-chart | `BleeckerSankeyChart`, original nodes/indexed flows and native cubic bands | Registered 76-case source fixture and graph-error/input/lifecycle checks | In progress: [source contract and acceptance gaps](sankey-chart-reference.md) |
| sunburst-chart | `BleeckerSunburstChart` and original recursive `BleeckerSunburstNode` records | Registered 94-case source fixture, invalid-tree regressions and native input/lifecycle checks | In progress: [source contract and acceptance gaps](sunburst-chart-reference.md) |
| dashboard-grid | `BleeckerDashboardGrid` and `BleeckerDashboardSpan`: viewport columns, responsive/full spans and consumer children | Registered 96-case frozen layout, mutation/keyboard fixture and independent consumer; monitoring/editorial compositions | In progress: [source contract and evidence](dashboard-grid-reference.md); full intrinsic/override matrix, raster, scaling and platform acceptance |
| dashboard-section | `BleeckerDashboardSection`: consumer content, optional title/description/action and viewport-responsive header | Registered 50-case frozen layout, binding/lifecycle/keyboard fixture and independent consumer | In progress: [source contract and evidence](dashboard-section-reference.md); exact raster, minimum-content/override matrix, scaling and full platform acceptance |
| page-header | `BleeckerPageHeader`: consumer title, description, breadcrumbs, wrapping actions and Back commands/routes | Registered sixteen-case viewport/RTL and content/lifecycle/keyboard fixture; independent package consumer | In progress: [source contract and API](page-header-reference.md); exact text balancing/raster, minimum-content constraints, scaling and full platform acceptance |
| sidebar | `BleeckerSidebar`, consumer-owned items and typed native link context | Registered 82-case geometry/color fixture, original callbacks, physical input, keyed state, scrolling, lifecycle and independent package consumer | In progress: [API and evidence](sidebars.md); exact raster, full state/motion/override/RTL and platform acceptance |
| data-table-toolbar | `BleeckerDataTableToolbar`, original consumer table/column adapter and content | Registered 118-case normal source layout fixture, actual native filtering/reset, original rows and lifecycle checks | In progress: [API and evidence](data-table-toolbar-reference.md); complete interaction, raster, overrides and platform acceptance remain open |
| data-table-filters | `BleeckerDataTableSelectFilter`, original consumer table/column and observable `BleeckerSelectOption` | Original 276 states plus 140 live-update states, keyed focus/identity/scrolling, native input and lifecycle checks; clean full x64 catalog and isolated consumer pass, ARM64 builds | In progress: [contract and evidence](data-table-filters-reference.md); four original raw focus differences, raster/routes, full collection/content/motion matrix and platform acceptance remain open |
| data-table-column-toggle | `BleeckerDataTableColumnToggle`: original top-level consumer columns and controlled visibility actions | Registered 28-case/132-state source reference; x64 native original-column/cell, input, geometry and lifecycle checks; isolated consumer fixture | In progress: [contract and evidence](data-table-column-toggle.md); source body/native dialog focus difference, raster, complete state/motion matrix and platform acceptance remain open |
| data-table-pagination | BleeckerDataTablePagination: original consumer paging/count/action boundary | 132 producer states, 136 keyboard states, 36 source scroll/typeahead states, 12 physical clicks, two Escape transfers and lifecycle checks pass; 89 regressions; all 34 popup widths/heights/gaps match; packaged checkpoint passes full 1,030-capture x64 suite and a fresh visible x64 consumer | In progress: all 132 surface pixel pairs differ; zero-delay key release, outside-popup pointer behavior, selector/popup paint, disabled input matrix, accessibility/scaling/performance and ARM64 runtime remain open; see [contract](data-table-pagination-reference.md#owned-chooser-artifact-checkpoint) |
| skeleton | `BleeckerSkeleton`, `BleeckerSkeletonCard`, `BleeckerSkeletonTable`: shapes, count-driven card/table compositions | Registered nine-case fixture; x64/ARM64 geometry, count/opacity bindings, rendered/empty pixels, ancestor visibility/lifecycle and clean consumers pass at `71a7fb8` | In progress: native/browser raster parity, exact pulse timing, scaling; see [contract and usage](skeleton-reference.md) |
| stepper | `BleeckerStepper` and consumer-owned `BleeckerStep`: both orientations, derived/explicit statuses and commands | Registered source layout, native input/lifecycle, motion, typography and strict raster fixtures; x64/ARM64 builds; exact v7 consumer verifies focused Space, metadata and disabled UIA in Light/Dark | In progress: [contract and evidence](stepper.md); source-derived visual review, complete reversal/override motion, scaling, native ARM64 runtime and final consumer acceptance; strict raster failures remain diagnostic |
| timeline | `BleeckerTimeline` and observable `BleeckerTimelineItem`: explicit/default statuses, original icons/templates and responsive timestamps | Registered 72-case frozen source matrix at widths 639, 640 and 1024; native action, keyed lifecycle, typography and real resize checks | In progress: [source contract and evidence](timeline.md); exact raster/shadow, full override/scaling, ARM64 runtime |
| data-list | `BleeckerDataList` and observable `BleeckerDataListItem`: consumer content/templates, stripes and viewport-responsive columns | Registered 72-case frozen matrix at widths 639, 640 and 1024; 396 scalar first-line baselines, native action, binding, keyed lifecycle and real resize checks | In progress: [source contract and evidence](data-list.md); exact raster/wrapped-line detail, full content/override matrix, scaling and ARM64 runtime |
| error-state | `BleeckerErrorState`: consumer-owned summaries, icon content and optional retry commands | Registered normal/empty/long/RTL and native retry fixtures; x64/ARM64 runtime, contrast and independent consumers pass at `64f4f25` | In progress: complete glyph/icon raster comparison, scaling, lifecycle/template matrix; see [contract and evidence](error-states.md) |
| loading-overlay | `BleeckerLoadingOverlay`: caller-owned viewport portal, label and existing large spinner | 60 source/native cases, 42 baselines and real input; live root-binding restoration; 22 external UIA trees; ten template/resource phases; thirteen real two-window phases; fresh x64 NuGet consumer; full 1,130-capture x64 sequence | In progress: [contract and acceptance](loading-overlay.md); exact raster/motion, broader lifecycle/theme, scaling and native ARM64 |
| date-range-picker | `BleeckerDateRangePicker`, consumer-owned ranges/presets and controlled requests | Registered Date Controls fixture; frozen source/native geometry, typography, popup, bounds, focus and independent consumer regressions | In progress: [contract and evidence](date-controls-reference.md); exact raster/motion, remaining customization and full platform acceptance |
| filter-chip | `BleeckerFilterChip`, `BleeckerFilterGroup`, observable `BleeckerFilter`: caller-owned metadata and remove/clear commands | 40 frozen/native cases, 72 baselines, 20 typography phases, 46 UIA trees, 28 Tab transfers, 28 property-event checks, 22 retained-pattern invocations, corruption negatives and fresh x64 NuGet consumer; earlier full 1,132-capture sequence | In progress: [contract and acceptance](filter-chip.md); exact raster/motion, broader lifecycle/customization/accessibility, scaling and native ARM64 |
| search-input | `BleeckerSearchInput`, retained native `BleeckerInput`, optional consumer-owned clear command/event | 40 source/native cases with zero geometry error, real editing/clipboard/binding/Tab/lifecycle checks, 24 external UIA snapshots and corruption negatives | In progress: [contract and acceptance](search-input.md); strict raster differences, full state/motion/customization/accessibility, scaling/native ARM64 |
| textarea | `BleeckerTextarea`: native multiline plain-text editor, rows and explicit validity override | 172 source/native states; 72 pointer resize cases and twelve ownership phases; 56 length cases, 36 delayed-provider cases and four real menu paste invocations; 36 real context command states and 28 ownership/reset phases; editing/undo/binding, external UIA and fresh visible preview consumer | In progress: [contract and acceptance](textarea-reference.md); 1,001,652 differing preview pixels, provider errors/remaining customization, full integration/platform/accessibility |
| dropdown-menu | `BleeckerDropdownMenu`, native item styles and focus behavior | Registered root/selection/submenu fixture; light/dark x64 geometry, selection, invocation and popup captures | In progress: rich rows, shadow/motion, full physical input, scaling/remaining ARM64 state checks; see [menu gaps](menus.md) |
| metric | `BleeckerMetric`: source number/currency/percent/compact formatting and consumer inline slots | 680 frozen browser cases across four locales/two themes; Windows ICU regression; registered native fixture; exact v7 independent consumer verifies formatting, metadata, live binding and retained rich-child focus in Light/Dark | In progress: [contract and open acceptance](metric-reference.md); source-derived raster/baselines, full customization/lifecycle, scaling/native ARM64 |
| notification-badge | `BleeckerNotificationBadge` and consumer-owned notification items | Registered populated/empty/count/long-content fixtures; x64 light/dark popup geometry, command/collection updates, live appearance and physical keyboard checks; independent package consumer | In progress: full state/input matrix, shadow/motion, scaling and remaining ARM64 state checks; see [evidence and gaps](notifications.md) |
| accordion | `BleeckerAccordion`, keyed consumer content and four template slots | Source/native layout, motion, continuity, scalar, physical input and package checks | In progress: exact raster, complete customization/lifecycle and platform acceptance; see [contract and evidence](accordion.md) |
| alert | `BleeckerAlert`, `BleeckerAlertContainer`: window-owned typed notifications and shared stream | Registered four-type fixture; x64 native paint/position, Tab/Shift+Tab/Space, held/cancelled press, duration and lifecycle checks; clean package consumer | In progress: [contract and usage](alert-reference.md); rendered reference, premium hover easing, full motion, disabled/scaling and ARM64 |
| alert-dialog | Controlled native preview and catalog fixture | 56 source/native layouts, 2,664 geometry values, 22 action/lifecycle cases; source motion models and shared host regressions | In progress: [public API](alert-dialog.md), [source contract](alert-dialog-reference.md); scrollbar edge paint, popup adapters, complete motion/visual/input and platform acceptance remain open |
| avatar | `BleeckerAvatar`, `BleeckerAvatarGroup`, `BleeckerAvatarData`: five sizes, images/initials/silhouettes and consumer collections | Registered Avatars fixture; source Light/Dark audit; x64/ARM64 geometry, rendered pixels, collection/lifecycle and independent package checks pass at `2f265e3` | In progress: native/browser raster parity, scaling, constrained widths and overrides; see [contract and usage](avatar-reference.md) |
| breadcrumb | `BleeckerBreadcrumb` and consumer-owned `BleeckerBreadcrumbItem` | x64/ARM64 wrapping, threshold, template/collection, held/cancelled press, keyboard expansion/focus and unload/reload checks; independent consumers pass at `7c4c272` | In progress: complete visuals/motion, remaining focus, scaling; see [reference/API](breadcrumb-reference.md) |
| context-menu | `BleeckerContextMenu`: native actions with original commands and a bindable modal option | Registered catalog and fresh NuGet consumer; 38 option cases, 14 hosted-adapter cases and source layout checks | In progress: [contract and evidence](context-menu.md); physical input/focus, nested behavior, complete placement/raster/motion and native ARM64 runtime |
| date-picker | `BleeckerDatePicker`: controlled local-date values and native popup requests | Registered Date Controls fixture; frozen source/native geometry, typography, popup, bounds, focus and independent consumer regressions | In progress: [contract and evidence](date-controls-reference.md); exact raster/motion, remaining customization and full platform acceptance |
| dialog-close-button | `BleeckerDialogCloseButton`, native commands, adjustable Lucide X and consumer-owned dismissal | 16 native/source states; 110 motion samples; real input, live themes and shadow disposal; eleven corruption checks | In progress: [contract and evidence](dialog-close-button.md); exact raster, normal native motion, broader overrides/scaling and native ARM64 |
| drawer | `BleeckerDrawer`: retained caller content, controlled close requests and a scrolling bottom surface | Registered edge-dialog catalog; shared Drawer/Sheet source and fresh x64 NuGet checks cover 194 states and 42 lifecycle/content/scrolling scenarios | In progress: [contract and evidence](drawer-sheet.md); physical touch/input, complete visual/motion/scaling and native ARM64 runtime |
| progress-stack | `BleeckerProgressStack`: consumer values/brushes and wrapping legend | 140 source/native frames and twelve lifecycle phases; zero asserted geometry error | In progress: [contract and remaining acceptance](progress-stack.md) |
| empty | `BleeckerEmpty`: passive title/description, default or consumer icon, and consumer action | 120 source/native states and twelve live, detach, minimum/style ownership and template/model phases; zero asserted geometry error | In progress: [source/native contract](empty-reference.md); full raster/typography/shadows, remaining customization, platform/a11y and integration acceptance |
| eyebrow | `BleeckerEyebrow`: three tones, optional rule and language-aware plain-text casing | Registered fixture and independent consumer; hidden x64 source run verifies live Turkish/English display and original names in both themes | In progress: [usage and gaps](eyebrows.md); rich/contextual casing, exact rendering, scaling and current native ARM64 runtime |
| hover-card | `BleeckerHoverCard`: original trigger/content, inline hover placement and source opacity clock | 44 native layouts, 132 brush checks, 96 controlled presentation samples and five ownership/query groups; full client captures | In progress: [API and gaps](hover-card.md); physical input, nested/ancestor stacking, full raster, scaling, performance and native ARM64 |
| input | `BleeckerInput`: three sizes, adornments, validation and native editing | Registered light/dark fixtures; native editing, motion, typography, 24 reading-direction and 72 physical-adornment cases; extracted v7 x64 disabled-pointer lifecycle passes 52 stages with animations off and the NuGet-matching DLL | In progress: [reference comparison](input-reference.md), fallback typography, scaling (225% Japanese viewport failure), enabled-motion reversal, full motion parity and remaining ARM64 state checks; see [usage](inputs.md) |
| kbd | `BleeckerKbd`: single content and observable key combinations | Registered fixture; v7 extracted x64 catalog and independent consumer pass 64 alignment, 16 caller, eight isolated-scale and 64 metadata checks each | In progress: [usage and gaps](keyboard.md), [pinned contract](kbd-reference.md); complete rendering, physical scaling and ARM64 runtime |
| label | `BleeckerLabel`, explicit typography and input association | Catalog variants; corrected source host passes 28 association cases and negative audit rejects empty-name fallback; extracted v7 x64 catalog passes real pointer/keyboard routes in Light and Dark; independent exact-package x64 consumer passes visible Label input in System mode and full smoke with a retained intermittent Column Toggle failure | In progress: [preview](labels.md), corrected association replay in the package consumer, explicit-theme consumer input, full rendering, scaling and ARM64; [reference](label-reference.md) |
| menubar | `BleeckerMenubar`, original caller menu entries and controlled selection requests | Registered catalog; source/fresh NuGet 30 states, 26 contracts, presence/standalone lifecycles, 1,224 geometry checks per host and 30 identical PNG pairs; [native preview](menubar.md) | In progress: full native motion, input/focus, visual/scaling and platform acceptance |
| navigation-menu | Original caller links/groups, guarded host routing and typed custom content | [Native contract](navigation-menu.md): source/fresh NuGet26states/18contracts each, both catalog themes,752exact geometry values and26identical PNG pairs | Full input/motion/metadata/scaling/raster, modal isolation and native ARM64 acceptance |
| otp-input | Native controlled text/password preview | Registered fictional catalog, 60 source states and 32 native behavior cases | Physical input/clipboard, scaling, full visual/motion and ARM64 runtime acceptance |
| popover | `BleeckerPopover`: original trigger/content, controlled requests, source placement and shared modal branches | 122 standalone source states in catalog/fresh consumer; 32 native ownership/lifecycle/transfer cases, six public NuGet popup/confirmation cases, and extracted v7 x64 Light/Dark physical opening/focus/Escape/controlled/outside dismissal with NuGet-matching DLL | In progress: [API and remaining acceptance](popover.md); broader nested/modal input, full raster, scaling, performance and native ARM64 runtime |
| progress | `BleeckerProgress`: three sizes, four variants and optional label | Registered size/variant matrix; v7 x64 catalog and independent consumer pass 14 range states, 32 invalid-input cases and live binding/recovery checks each | In progress: [usage and gaps](progress.md); full rendering, motion, lifecycle, scaling and ARM64 runtime |
| radio-group | `BleeckerRadioGroup` and immutable `BleeckerRadioOption` | Registered native fixture; 56 source/native frames and 136 options have zero geometry error; physical keyboard, controlled requests, UIA and lifecycle checks in both themes | In progress: [contract and evidence](radio-group.md); full raster/motion, customization/error matrix and Windows platform acceptance |
| checkbox | `BleeckerCheckbox`: native selection with consumer label/description content | Registered fixture; v7 x64 catalog/consumer pass 72/70 rich-name, binding, style and native-toggle checks | In progress: [usage and gaps](checkboxes.md); complete rendering, physical input/cursor/focus, scaling, motion and ARM64 runtime |
| scroll-area | BleeckerScrollArea: original content/templates, native scrolling, source scrollbar recipe and public scroll pattern | ScrollAreasFixture; 94 layouts, 12 native histories, five contract groups, 17,952 thumb-body pixels and fresh consumer | In progress: [contract and evidence](scroll-area.md); physical input, full visual/scaling/performance and platform acceptance open |
| separator | `BleeckerSeparator`: horizontal/vertical, decorative or semantic | Registered geometry, theme, live orientation and automation fixture; native 100–200% isolated scale and no-tab-stop checks pass; v7 catalog foreground and exact-package independent consumer smokes pass | In progress: [contract and evidence](separator.md); native ARM64 fractional-default acceptance; strict one-channel raster difference remains diagnostic |
| sheet | `BleeckerSheet`: retained caller content, controlled close requests and four physical sides | Registered edge-dialog catalog; shared 194-state source/NuGet checks and 16 actual catalog cases cover all sides and nested composition | In progress: [contract and evidence](drawer-sheet.md); physical input, complete visual/motion/scaling and native ARM64 runtime |
| slider | `BleeckerSlider`: controlled raw/sanitized values and native range semantics | 182 exact hidden layouts, 14 retained-fill states, 36 ownership/lifecycle checks and 44 corruption negatives; multilingual font and styled-name regressions | In progress: [contract](slider.md); visible input/raster/motion, scaling, shared-consumer integration and platform acceptance |
| sonner | Native host/toast/action API and registered fictional fixture | [Source preparation](sonner-reference.md) and [native preview](sonner.md); source/fresh NuGet match 66 hidden states and 20 checks, 53/49 auditor rejections; 418 portable tests, both builds and isolated consumers pass | Physical input, full visual/motion/customization/modal and ARM64 runtime acceptance |
| switch | `BleeckerSwitch`: native binary toggle and optional label | x64 light/dark native layout, UIA/binding/lifecycle, live appearance, physical Space/Tab/track/label/RTL, Japanese fallback repair, isolated 100–200% display-scale rasterization and independent consumer passed; composed screenshots retained | In progress: [usage and gaps](switches.md); exact raster/motion/drag/cursor/focus, physical monitor and system text scaling, and remaining ARM64 desktop checks |
| toggle | Six native `ToggleButton` styles | Registered size/variant fixtures; x64 automation/state and binding/command checks; desktop pointer and Space/Tab; 48 hidden native theme/raster-scale style observations in source and exact NuGet consumer, with 16 matching PNG pairs | In progress: physical DPI/text scaling, reduced motion, complete visual review and ARM64 runtime; see [usage](toggles.md) |
| toggle-group | `BleeckerToggleGroup` and native toggle items; optional single/multiple selection | Registered binding, collection, disabled and constrained fixtures; x64 UIA and physical input; package consumer; ARM64 build | In progress: scaling, motion, broader RTL/error fixtures and remaining ARM64 state checks; see [usage](toggle-groups.md) |
| tooltip | `BleeckerTooltip` and service; shared modal plane with standalone constrained Popup | Frozen 40-state source matrix; 72 native layouts, active popup/peer scopes, Field Help cleanup, catalog Tooltip/Popover composition and v7/v8 archive-identical x64 Top hover/leave/keyboard/Escape in both themes; [reference](tooltip-reference.md) | In progress: remaining physical input/focus matrix, full raster, timing/motion, corner/scaling/customization and native ARM64 acceptance; see [gaps](tooltips.md) |
| feed-item | `BleeckerFeedItem`: retained author/avatar/time/relevance/content/categories and consumer-owned source requests | Feed items fixture; 100 source layouts/64 PNGs, 21 localized timestamp cases and actual tooltip bodies | In progress: [contract](feed-item.md); native/visible/platform/date-locale acceptance open |
| field | `BleeckerField`: retained label/control/action/message composition | 44 measured states, real input, external UIA ownership and package checks | In progress: [contract and evidence](field.md); exact raster, broader customization/scaling and native ARM64 |
| header-select | `BleeckerHeaderSelect`: original options, caller value requests and compact recipe | Shared 292 closed/32 open layout states; modal ownership/live-update cases and independent public NuGet recipe checks; source x64 keyboard selection/Escape in both themes | In progress: [API and acceptance](select.md); remaining input matrix, source-focused paint comparison, full raster, scaling/motion/performance and ARM64 runtime |
| hero-carousel | `BleeckerHeroCarousel`, original items/context/images, owning-window arrows and inactive input, fictional catalog | [Source preparation](hero-carousel-reference.md): 86 frames/54 histories; current source/package/archive 82 states/34 checks, 434 portable/10 text tests, both builds/consumers and demos pass; exact v7 independent consumer outside arrows/editor exclusion pass in Light/Dark | [Native preview](hero-carousel.md): strict image diagnostics, broader published physical input/focus, complete visual/motion/scaling/customization and native ARM64 acceptance remain open |
| field-help-tooltip | `BleeckerFieldHelpTooltip`, native button with explicit vector template | Inline label and empty text, branded state values, keyboard help, accessible name and live text | In progress: exact motion comparison, physical input, rendered parity, scaling and ARM64; see [usage](field-help-tooltips.md) |
| icon-badge | `BleeckerIconBadge`: two sizes, three variants, passive rich/scalar content | 136 source/native frames, 52 child placements, 80 scalars and eight retained-child lifecycle phases; zero geometry error | In progress: [contract and gaps](icon-badge.md); exact raster, remaining customization/input/a11y, scaling/native ARM64 acceptance |
| icon-button | `BleeckerIconButton`: three variants and three sizes | Nine registered combinations; x64 dimensions, names/roles, command and state checks; light/dark captures; package fixture; ARM64 build; extracted v7 and archive-identical v8 x64 physical Edit pointer/Space/Enter and tooltip in both themes | In progress: wider physical input, fresh full v8 ZIP extraction, scaling, motion and remaining ARM64 state checks; see [usage](icon-buttons.md) |
| loading-spinner | `BleeckerLoadingSpinner`: three sizes, themed three-quarter ring | Registered fixture; x64 geometry, theme, UIA, reduced-motion and lifecycle checks; independent package consumer | In progress: normal/live motion, scaling/composition, remaining ARM64 state checks; see [usage](loading-spinner.md) |
| media-library | `BleeckerMediaLibrary`: caller-owned items, controlled search, selected preview and delete requests | Fictional catalog, fresh NuGet and extracted catalog; 100 hidden x64 states and 38 behavior checks include the corrected pointer Delete hit target; extracted v7 x64 pointer Delete and keyboard Tab/Space pass in both themes | In progress: [contract and evidence](media-library.md); broader physical input/focus, complete visual/motion/scaling and native ARM64 runtime |
| modal | `BleeckerModal` native draft, caller-owned content/open state | `ModalsFixture`; extracted v7 and archive-identical v8 x64 physical nested open/Escape/parent focus restoration in Light and Dark | In progress: [source, runtime and remaining acceptance](modal.md) |
| nav-menu | `BleeckerNavMenu`: caller-owned row/column links and original-item custom contexts | 42 source PNGs; 888 exact native geometry and 138 paint checks; 8 native histories/24 samples; 26 lifetime checks; 40/42 corrupt foreground/hidden receipts rejected; source 220 ms midpoint; catalog fixture; 285 portable and 9 Windows text tests; clean x64/ARM64 builds and fresh package consumers; x64 hidden 42 layouts/16 ownership checks | In progress: [contract](nav-menu.md); ARM64 runtime, full raster, native motion, scaling and integration |
| pagination | `BleeckerPagination`: controlled page requests and producer-owned availability | 66 zero-error layouts and actual brush checks; 260 exact border pixels; real input/binding/focus; fresh consumers with 52 hidden layouts/18 ownership checks; production motion clocks match 352 controlled-time native presentations across 64 source transitions; 32/23/28 rejection checks | In progress: [contract and current evidence](pagination.md); exact raster, real-time normal motion, broader input/customization/scaling, full integration and native ARM64 acceptance |
| collection-filters | `BleeckerCollectionFilters`: caller-owned filter and sort requests | 50 source/native layouts, 3,408 zero-error geometry checks, 44 input/state samples; numeric-key order and tabular count digits; 420 actual-brush checks per runtime; fresh consumers with 50 hidden layouts/16 ownership checks; 44/31 rejection checks; both architectures build | In progress: [contract and evidence](collection-filters.md); 661,819 differing pixels, broader prop/date input, normal motion, customization/scaling, final integration and native ARM64 acceptance |
| command-spotlight | `BleeckerCommandSpotlight`: caller actions, query/selection, modal surface and hotkey | Registered source, caller-style, Unicode-hotkey and 32/1,000-action fixtures; native state/style/hotkey and measured collection/refresh checks; live font/binding/key-cap, isolated backdrop and selected-fill/observer regressions; independent package consumer; published v7 focused physical input passes in system Dark | In progress: [contract and measured gaps](command-spotlight.md); strict raster diagnostics, broader physical routing/layouts/IME, caller templates/scaling/motion, large native collections and ARM64 runtime remain open |
| select | `BleeckerSelect`: original options/value bindings, shared portal and native option peers | 32 open layouts/2,400 exact geometry values; 40 recipe cases, two catalog flows and eight public NuGet cases; 292 closed layouts retained; extracted v7 x64 pointer and Header Select keyboard paths pass in both themes | In progress: [API and acceptance](select.md); remaining input matrix, source-focused paint comparison, full raster, scaling/motion/performance and ARM64 runtime |
| file-input | `BleeckerFileInput`, original native files, caller picker boundary and fictional catalog | [Native preview](file-input.md): source/package/archive 68 states/30 checks; 434 portable/10 text tests, both builds/consumers and demos pass | Strict browser/native and source/package images, native system picker, physical input, scaling/motion and ARM64 runtime remain open |
| section-header | `BleeckerSectionHeader`: title, optional eyebrow/description and native level-two heading | Registered 24-case frozen layout and live binding/lifecycle/keyboard fixture; independent consumer | In progress: [source contract and evidence](section-header-reference.md); exact text raster/balancing, complete overrides, scaling and full platform acceptance |
| panel | `BleeckerPanel`: title/count, consumer slots and an independent scroll body | 266 zero-error layouts, 58 hash-bound raw PNGs, 29 audit negatives, native input/binding/template/keyboard/scroll checks, five windowless font tests and fresh package consumers | In progress: [contract and evidence](panel.md); 661,516 raw pixel differences, normal motion, broader overrides/scaling, native ARM64 and final integration |
| status-badge | `BleeckerStatusBadge`: five variants, consumer label/description and constrained reflow | Registered palette/layout/automation and live binding fixtures; independent consumer | In progress: [contract and evidence](status-badge.md); complete shaping/gamut/raster, scaling and ARM64 acceptance |
| table | `BleeckerTable` primitives and caller-owned content | Registered fixtures; source and fresh consumer geometry, live/template and empty inline-action checks | In progress: [contract](table.md); complete visual, input and platform acceptance |
| data-table | `BleeckerDataTable`, original caller rows/columns and controlled state | 208 source states, 84 live states, 98 table-template states and filter/performance regressions | In progress: [contract](data-table.md); large-page performance remains mixed and full acceptance is open |
| tabs | `BleeckerTabs`, keyed items, controlled selection and host-owned navigation | Nine variant/size fixtures; 44 native/source geometry and paint cases with zero geometry error; 528 motion samples; real input and boundary regressions | In progress: [API and acceptance](tabs.md); exact raster, native normal-motion presentation, broader overrides/scaling and native ARM64 runtime |
| theme-toggle | `BleeckerThemeToggle`: native icon button, consumer-owned mode requests and caller metadata precedence | 134 original browser states/PNGs; 116 exact hidden native layouts, 42 ownership/lifecycle checks and 34 corruption negatives; catalog fixture and prepared foreground/package assertions | In progress: [contract](theme-toggle.md); visible input/raster, inherited motion, scaling, full integration and platform acceptance |
| app-shell | `BleeckerAppShell`: retained header/body/footer, viewport minimum and caller-controlled offset | Registered fixture, 168 hidden source layouts, 44 ownership/metadata checks and independent package consumers | In progress: [contract and evidence](app-shell.md); visible raster/input, scaling and native ARM64 acceptance |
| admin-shell | `BleeckerAdminShell`: retained header/sidebar and scrolling body/footer | 312 initial/16 scrolled zero-error layouts, 72 ownership checks, 60 hidden/39 foreground audit negatives, 266 portable tests and visible keyboard/pointer/scroll evidence; separate 68-frame text probe with 38 rejection checks | In progress: [contract and remaining acceptance](admin-shell.md); source raster, broader customization, scaling and native ARM64 acceptance pending |
| auth-shell | `BleeckerAuthShell`: centered/split retained caller slots and responsive aside | Registered fictional catalog;104 native states/24 checks across source/NuGet/archive;94 source/package audit negatives each;469 portable/10 Windows text tests; both architectures/consumers/demos;104 source/archive PNGs exact | In progress: [contract and remaining visual/input/platform acceptance](auth-shell.md); browser and one package-image gate failed |
| detail-layout | `BleeckerDetailLayout`: responsive main/rail tracks and bounded sticky placement | 888 zero-error states, 48 exact full PNGs, 27 audit negatives, real input/Tab order, original template models and nearest ScrollViewer/ScrollView retargeting | In progress: [contract and evidence](detail-layout.md); broader caller overrides/scaling, native ARM64 and final integration |
| footer | `BleeckerFooter` with original brand/section models, bottom slots and a global guarded link context | 48 pinned source PNGs; catalog and fresh package hidden 144 layouts/8040 exact geometry checks/1434 brushes/7 lifetime and threading checks/1349 isolation guards; 40 catalog/44 package damaged reports rejected; fixture; 287 portable tests; clean x64/ARM64 builds and isolated consumers | In progress: [contract](footer.md); visible input/raster, broader props, motion/scaling, integration and native ARM64 acceptance |
| feed-column | `BleeckerFeedColumn`: fixed header, original filter/body slots and independent native viewport | Feed columns fixture; 154 pinned source states and 58 original full PNGs | In progress: [contract](feed-column.md); native/visible/platform acceptance remains open |
| feed-grid | BleeckerFeedGrid | FeedGridsFixture | Preview: auto-fill tracks, variable rows and retained native children; visual/platform acceptance open |
| header | `BleeckerHeader`: original brand/navigation, caller-owned actions and responsive positioning | Catalog and fresh x64 consumer; 72 source layouts, 23 native scenarios and both architecture builds | In progress: [contract and evidence](header.md); desktop input/focus, complete visual/motion/scaling, integration and native ARM64 runtime |
| page-frame | `BleeckerPageFrame`: centered width/spacing recipes and semantic grouping | 792 measured states, 40 exact full PNGs, 20 audit negatives, native input, viewport/binding/style/template/selector checks and fresh package consumer | In progress: [contract and evidence](page-frame.md); broader content/override/scaling and native ARM64 |
| panel-layout | BleeckerPanelLayout / BleeckerPanelColumn | PanelLayoutsFixture | Preview: fixed/growing tracks, retained child controls, horizontal scrolling; visual/platform acceptance open |

| brand-mark (Sabella) | `BleeckerBrandMark`, original template PNG and caller-owned size/tint | 37 source/fresh NuGet states per host; original-alpha tint probes and both architecture builds | In progress: [contract](brand-mark.md); complete visual and platform acceptance |
| secure-field (Sabella) | `BleeckerSecureField` | SecureFieldsFixture; native and fresh consumer 30 cases; disabled visual regression | In progress: [contract](secure-field.md); physical input, full visual/scaling/customization and ARM64 runtime |
| segmented-control (Sabella) | `BleeckerSegmentedControl` / `BleeckerSegmentOption` | SegmentedControlsFixture; both hosts 40 cases/8 layouts/8 rendered probes; wrong RTL captures rejected | In progress: [contract](segmented-control.md); full input/visual/platform acceptance |
| app-tab-bar (Sabella) | Native compact preview with typed selection, equal tracks, icons and badges | Registered fictional catalog, source/NuGet native cases and rendered checks | Physical input, actual scaling, full visual/customization and ARM64 runtime acceptance |

The focused title-language repair renders `istanbul` as `İSTANBUL` under
`tr-TR` and `ISTANBUL` under `en-US` in Eyebrow, Feed Column, Panel, Stat Card
and Footer section headings. A shared hidden Catalog/consumer fixture verifies
live language changes across both themes, preserving the original caller titles
and existing automation names. All 16 new title cases and four Eyebrow cases pass
in source and the exact x64 NuGet consumer. Both catalog architectures and
isolated package consumers build; native ARM64 runtime and full component
acceptance remain open.

## Compositions

Monitoring: suite-selector slot, sidebar, live columns, attention cards, search, stable selection and processing review. Editorial: filters, sortable/selectable rows, page editing, media/category/author selection and inline confirmation. Both use fictional local data, without product APIs or authentication.

## Verification and release acceptance

- Foundation: published and Visual Studio startup, fonts/resources, light/dark/system themes, open flyouts, token drift, text/display scaling and reduced motion.
- Every public visual control/style must have registered catalog coverage and appropriate boundary regression checks.
- x64 and ARM64 builds and runtime evidence are separate requirements. The registered fixture/runtime baseline passes on both architectures; full platform acceptance remains open as described in [runtime evidence](runtime-evidence.md).
- Component accessibility requires keyboard/focus behavior and UI Automation names, roles and values. Narrator and OS contrast campaigns are excluded. Component performance checks use representative local workloads.
- Independent package consumer, versioned NuGet and runnable catalog, matching documentation, remote main and required CI are still required.

The GitHub wiki could not be retrieved. Documentation remains in this repository. Thompson is a supplementary contract reference; its touch-specific behavior does not override desktop Windows conventions. Sabella television/mobile-specific types are outside this Windows scope. Web providers and implementation utilities map to native infrastructure rather than separate visual controls. No networking, CMS schemas or product workflow logic enters Orchard.

