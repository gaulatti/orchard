# Fixed Windows parity inventory

Current scope is exclusively the component library, including the monitoring and
editorial component demos. The [user-confirmed scope](library-scope.md) supersedes
older Narrator and OS contrast acceptance requirements below; those test
campaigns are excluded. The user has resumed component-library work.

Use the [2026-10-29 acceptance contract](acceptance-contract.md) for this
milestone: five x64 checks per component, with ARM64 build-only and runtime
unverified. Raster/pixel parity is not a gate. The machine-readable inventory
records pass/fail for all 112 entries. The [current `.89` release](releases/2026-10-31-preview-89.md)
lists all 112 x64-accepted components, including Table and Data Table.
Older row descriptions below retain historical diagnostics and do not add
milestone gates. Monitoring and Editorial remain required fictional demos.

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
The [accepted OTP Input](otp-input.md) preserves controlled caller values, ASCII
edit requests, native text/password slots and source deletion/paste semantics.
The exact `.45` package passed 60 states, 32 behavior cases, physical input in
both themes, native 150% no-clipping and reduced-motion focus-ring checks.
ARM64 runtime remains unverified.
The [accepted App Tab Bar](app-tab-bar.md) adds value-bound destinations, equal
tracks, caller-owned Windows icons and capped badges with raw-count metadata.
Its exact package passes 58 cases and eight Light/Dark layouts, while the native
150% consumer passes eight more cases. Focused physical pointer, keyboard and
focus evidence remains valid for unchanged component and fixture. ARM64 runtime
remains unverified.
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
| brand-lockup | `BleeckerBrandLockup`, typed consumer link context and live native image effects | Matching `.30` package DLL in extracted x64 catalog passes Light/Dark variants, live theme, physical pointer/Tab/Enter/focus, reduced-motion endpoints; independent consumer compiles and passes four native 150% small/large named cases | Accepted in `.30` for the five-check x64 milestone; ARM64 runtime unverified; see [contract](brand-lockup-reference.md) |
| brand-mark | `BleeckerBrandMark`, original template PNG and fixed square size | Current package passes 37 Light/Dark states, decorative input/focus, static motion, independent consumer compilation and 150% no-clipping at four sizes in both themes | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](brand-mark.md) |
| bauhaus-background | `BleeckerBauhausBackground`: four variants and live native image effects | Registered square/tall and missing-image fixtures; x64 source, geometry, motion-preference and decorative UIA checks; eight native 150% package-consumer cases | Accepted x64 milestone: [source contract and API](bauhaus-background-reference.md); ARM64 runtime unverified |
| activity-item | `BleeckerActivityItem`: exact strings, small Avatar, source typography and hover | `.34` package-matching x64 Light/Dark route and six native 150% consumer cases pass | Accepted on x64 under the [five-check contract](acceptance-contract.md); ARM64 runtime unverified; [source contract and API](activity-item-reference.md) |
| attention-surface | `BleeckerAttentionSurface`: five elements, densities, source color model and consumer content/actions | Exact `.65` x64 catalog passes 43 Light and 43 Dark states plus physical actions/focus and reduced-motion hover; independent consumer passes eight native 150% layouts and compiles/runs | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [reference](attention-surface-reference.md) |
| button | `BleeckerButton`: seven variants, four sizes, commands/content, loading | Registered fixtures; x64 native command/loading/binding, CanExecute/template replacement and 7×4 state checks in both themes; isolated NuGet consumer; ARM64 build | In progress: focus/input, scaling, motion, remaining ARM64 state checks; see [usage](buttons.md) |
| button-group | `BleeckerButtonGroup` | Horizontal/vertical catalog fixtures; published x64 light/dark layout and group-role checks | In progress: edge fixtures, child restoration, keyboard, scaling, remaining ARM64 state checks; see [usage](button-group.md) |
| card | `BleeckerCard`: five variants, four padding sizes, consumer content | Matching `.15` x64 catalog passes Light/Dark variants and physical consumer-child Tab/Space/pointer/focus; independent package consumer passes twenty native 150% cases, static reduced motion and compilation | Accepted for the x64 milestone; ARM64 runtime unverified. [Usage and evidence](cards.md) |
| chart | `BleeckerChart`: fixed currency plot over original consumer records | Accepted on x64: 47 Light and 47 Dark catalog cases with physical pointer/keyboard/focus; four native 150% no-clip cases and static reduced-motion geometry in the independent package consumer | ARM64 runtime unverified; historical diagnostics remain in the [component contract](chart-reference.md) |
| chart-theme | `BleeckerChartTheme` and `BleeckerChartSeries`: scoped resource keys, source palette selection, immutable style metadata and consumer-owned series | Registered role/palette/brand, series-paint and axis/grid/tooltip surface fixture; frozen metadata regressions | In progress: [contract and API](chart-theme-reference.md); actual chart consumers, tooltip effects and full platform acceptance |
| line-chart | `BleeckerLineChart`: consumer records/series, monotone curves, grid, axes, legend and category inspection | Accepted on x64: Light/Dark native states and physical pointer/keyboard/focus; four native 150% no-clip cases and static reduced-motion geometry in the independent package consumer | ARM64 runtime unverified; historical diagnostics remain in the [component contract](line-chart-reference.md) |
| sparkline | `BleeckerSparkline`: compact consumer records/key, monotone curves, optional paint and raw category inspection | Matching .18 x64 Light/Dark catalog, native pointer/keyboard/focus, 20 independent-consumer 150% cases, static reduced-motion behavior and package compilation | Accepted on x64: [source contract and evidence](sparkline-reference.md); ARM64 runtime unverified |
| stat-card | `BleeckerStatCard` and `BleeckerStatTrend`: consumer value/templates, trend/description precedence, icon, history and footer composition | Matching `.24` x64 catalog passes Light/Dark states and physical keyboard/pointer/focus; independent package consumer compiles and passes four native 150% cases; static reduced-motion behavior | Accepted on x64: [contract and evidence](stat-card-reference.md); ARM64 runtime unverified |
| bar-chart | `BleeckerBarChart`: consumer grouped bars, both layouts, axes, grid/legend and category inspection | Accepted on x64 in `.81`: Light/Dark states, physical focus/Right-arrow inspection, four native 150% no-clip cases, static reduced-motion geometry, independent package-consumer compile | ARM64 runtime unverified; historical diagnostics remain in the [component contract](bar-chart-reference.md) outside this milestone |
| area-chart | `BleeckerAreaChart`: overlapping/stacked monotone areas, ranges, axes and native inspection | Accepted on x64: 40 Light and 40 Dark catalog cases with physical pointer/keyboard/focus; four native 150% no-clip cases and static reduced-motion geometry in the independent package consumer | ARM64 runtime unverified; historical diagnostics remain in the [component contract](area-chart-reference.md) |
| pie-chart | `BleeckerPieChart` and consumer-owned `BleeckerPieChartDatum` | Accepted on x64: 22 Pie cases per theme with physical input/focus; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](pie-chart-reference.md) |
| donut-chart | `BleeckerDonutChart`: source ring and independent center text | Accepted on x64: 20 Donut cases per theme with physical input/focus; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](pie-chart-reference.md) |
| scatter-chart | `BleeckerScatterChart`, typed series and independent consumer collections | Accepted on x64: 32 cases per theme with physical input/focus; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](scatter-chart-reference.md) |
| radar-chart | `BleeckerRadarChart`, typed series, polar subjects and range boundaries | Accepted on x64: 36 cases per theme with physical input/focus; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](radar-chart-reference.md) |
| radial-bar-chart | `BleeckerRadialBarChart`, typed observable records, curved labels and consumer radii/angles | Accepted on x64: 40 cases per theme with physical input/focus; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](radial-bar-chart-reference.md) |
| funnel-chart | `BleeckerFunnelChart`, typed observable records, native trapezoids and wrapped source labels | Accepted on x64: 27 cases per theme with physical input/focus; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](funnel-chart-reference.md) |
| sankey-chart | `BleeckerSankeyChart`, original nodes/indexed flows and native cubic bands | Accepted on x64 in `.81`: Light/Dark states, physical focus/Right-arrow inspection, four native 150% no-clip cases, static reduced-motion geometry, independent package-consumer compile | ARM64 runtime unverified; historical diagnostics remain in the [component contract](sankey-chart-reference.md) outside this milestone |
| sunburst-chart | `BleeckerSunburstChart` and original recursive `BleeckerSunburstNode` records | Accepted on x64: 47 cases per theme and passive image semantics; four native 150% no-clip cases, static reduced motion and independent package compilation | ARM64 runtime unverified; [component contract](sunburst-chart-reference.md) |
| dashboard-grid | `BleeckerDashboardGrid` and `BleeckerDashboardSpan`: viewport columns, responsive/full spans and consumer children | Published `.14` x64 catalog passes 96 Light/Dark cases and physical keyboard/pointer/focus; matching package consumer passes eight native 150% cases, static reduced motion and compilation | Accepted for the x64 milestone; ARM64 runtime unverified. [Component record](dashboard-grid-reference.md) |
| dashboard-section | `BleeckerDashboardSection`: consumer content, optional title/description/action and viewport-responsive header | Published `.14` x64 catalog passes 50 Light/Dark cases and physical keyboard/pointer/focus; matching package consumer passes twelve native 150% cases including painted overflow, static reduced motion and compilation | Accepted for the x64 milestone; ARM64 runtime unverified. [Component record](dashboard-section-reference.md) |
| page-header | `BleeckerPageHeader`: consumer title, description, breadcrumbs, wrapping actions and Back commands/routes | Published `.13` x64 catalog passes sixteen Light/Dark cases with physical Back input; matching consumer passes 22 native 150% content cases, static motion and compilation | Accepted for the x64 milestone; ARM64 runtime unverified. Historical raster diagnostics remain in [the component record](page-header-reference.md) |
| sidebar | `BleeckerSidebar`, consumer-owned items and typed native link context | Exact `.57` x64 catalog: Light/Dark states and physical pointer/keyboard/focus; independent package consumer: 16 native 150% Light/Dark collapsed/expanded, RTL and constrained-width layouts, immediate reduced-motion width snap | Accepted on x64 for this milestone; ARM64 runtime unverified; [API and evidence](sidebars.md) |
| data-table-toolbar | `BleeckerDataTableToolbar`, original consumer table/column adapter and content | Accepted on x64 in `.79`: Light/Dark states, physical held-input/search/filter/Reset/action/focus, four native 150% no-clip cases, static reduced-motion state, independent package-consumer compile | ARM64 runtime unverified; historical source BODY/native focus differences remain in the [component contract](data-table-toolbar-reference.md) outside this milestone |
| data-table-filters | `BleeckerDataTableSelectFilter`, original consumer table/column and observable `BleeckerSelectOption` | Accepted on x64 in `.79`: Light/Dark native and live-update states, physical pointer/keyboard/focus, four native 150% open-popup cases, static reduced-motion state, independent package-consumer compile | ARM64 runtime unverified; historical source focus differences remain in the [component contract](data-table-filters-reference.md) outside this milestone |
| data-table-column-toggle | `BleeckerDataTableColumnToggle`: original top-level consumer columns and controlled visibility actions | Accepted on x64 in `.78`: Light/Dark states, physical pointer/keyboard/focus, four native 150% open-popup cases, static reduced-motion state, independent package-consumer compile | ARM64 runtime unverified; historical diagnostics remain in the [component contract](data-table-column-toggle.md) outside this milestone |
| data-table-pagination | BleeckerDataTablePagination: original consumer paging/count/action boundary | Accepted on x64 in `.77`: Light/Dark states, physical pointer/keyboard/focus, eight native 150% closed/open chooser cases, static reduced-motion state, independent package-consumer compile | ARM64 runtime unverified; historical diagnostics are retained in the [component contract](data-table-pagination-reference.md) and are outside this milestone |
| skeleton | `BleeckerSkeleton`, `BleeckerSkeletonCard`, `BleeckerSkeletonTable`: shapes, count-driven card/table compositions | Matching `.29` package DLL passes 36 Light/Dark/RTL states, 16 caller-content contracts, physical child pointer/Space/Enter/focus, reduced-motion opacity, independent consumer compilation and four native 150% viewport/theme cases | Accepted in `.29` for the five-check x64 milestone; ARM64 runtime unverified; see [contract and usage](skeleton-reference.md) |
| stepper | `BleeckerStepper` and consumer-owned `BleeckerStep`: both orientations, derived/explicit statuses and commands | Current x64 foreground fixture passes 44 Light/Dark cases with native pointer/keyboard/focus and zero baseline failures; independent package consumer passes eight native 150% layouts and static reduced-motion colors | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and evidence](stepper.md) |
| timeline | `BleeckerTimeline` and observable `BleeckerTimelineItem`: explicit/default statuses, original icons/templates and responsive timestamps | Current x64 catalog passes 72 Light/Dark states at widths 639, 640 and 1024, native input/focus and live breakpoint; independent package consumer passes eight native 150% layouts and static reduced-motion status colors | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and evidence](timeline.md) |
| data-list | `BleeckerDataList` and observable `BleeckerDataListItem`: consumer content/templates, stripes and viewport-responsive columns | `.34` package-matching x64 route passed 72 Light/Dark cases; independent consumer passed six native 150% layouts | Accepted on x64 under the [five-check contract](acceptance-contract.md); ARM64 runtime unverified; [source contract and evidence](data-list.md) |
| error-state | `BleeckerErrorState`: consumer-owned summaries, icon content and optional retry commands | Published `.10` x64 catalog passes seven Light/Dark specimens and physical pointer/Space retry; matching consumer passes fourteen native 150% scale cases, static reduced motion and package compilation | Accepted for the x64 milestone; ARM64 runtime unverified. Historical raster diagnostics remain in [the component record](error-states.md) |
| loading-overlay | `BleeckerLoadingOverlay`: caller-owned viewport portal, label and existing large spinner | Current x64 catalog passes 60 Light/Dark source states at three sizes and physical keyboard/pointer/focus; independent package consumer passes four native 150% layouts and static reduced-motion spinner | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and evidence](loading-overlay.md) |
| date-range-picker | `BleeckerDateRangePicker`, consumer-owned ranges/presets and controlled requests | Accepted on x64 in `.80`: Light/Dark states, physical pointer/keyboard/focus, four native 150% open-calendar cases, static reduced-motion popup, independent package-consumer compile | ARM64 runtime unverified; historical diagnostics remain in the [component contract](date-controls-reference.md) outside this milestone |
| filter-chip | `BleeckerFilterChip`, `BleeckerFilterGroup`, observable `BleeckerFilter`: caller-owned metadata and remove/clear commands | 40 x64 Light/Dark cases at 320/640 DIPs, four physical input/reduced-motion runs, six native 150% package-consumer cases | Accepted on x64 under the [five-check contract](acceptance-contract.md); ARM64 runtime unverified; [usage and evidence](filter-chip.md) |
| search-input | `BleeckerSearchInput`, retained native `BleeckerInput`, optional consumer-owned clear command/event | 40 source/native cases with zero geometry error, real editing/clipboard/binding/Tab/lifecycle checks, 24 external UIA snapshots and corruption negatives | In progress: [contract and acceptance](search-input.md); strict raster differences, full state/motion/customization/accessibility, scaling/native ARM64 |
| textarea | `BleeckerTextarea`: native multiline plain-text editor, rows and explicit validity override | Exact `.58` x64 catalog: 172 Light/Dark states and four physical pointer/keyboard/editing/focus sequences at 320/640 widths; independent package consumer: 48 native 150% Light/Dark size/rows/width/error layouts with stable reduced-motion border paint | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and usage](textarea-reference.md) |
| dropdown-menu | `BleeckerDropdownMenu`, native item styles and focus behavior | Registered root/selection/submenu fixture; light/dark x64 geometry, selection, invocation and popup captures | In progress: rich rows, shadow/motion, full physical input, scaling/remaining ARM64 state checks; see [menu gaps](menus.md) |
| metric | `BleeckerMetric`: source number/currency/percent/compact formatting and consumer inline slots | Matching .18 x64 Light/Dark catalog covers 680 format cases and rich-slot input/focus; 20 independent-consumer 150% cases, static reduced-motion behavior and package compilation | Accepted on x64: [source contract and evidence](metric-reference.md); ARM64 runtime unverified |
| notification-badge | `BleeckerNotificationBadge` and consumer-owned notification items | Registered populated/empty/count/long-content fixtures; x64 light/dark popup geometry, command/collection updates, live appearance and physical keyboard checks; independent package consumer | In progress: full state/input matrix, shadow/motion, scaling and remaining ARM64 state checks; see [evidence and gaps](notifications.md) |
| accordion | `BleeckerAccordion`, keyed consumer content and four template slots | Source/native layout, motion, continuity, scalar, physical input and package checks | In progress: exact raster, complete customization/lifecycle and platform acceptance; see [contract and evidence](accordion.md) |
| alert | `BleeckerAlert`, `BleeckerAlertContainer`: window-owned typed notifications and shared stream | Registered four-type fixture; x64 native position, Tab/Shift+Tab/Space, held/cancelled press, duration and lifecycle checks; clean package consumer and native 150% Light/Dark no-clipping | Accepted x64 milestone: [contract and usage](alert-reference.md); ARM64 runtime unverified |
| alert-dialog | `BleeckerAlertDialog`, caller-owned open state and requests | Accepted on x64 in preview .70: 56 Light/Dark states, physical pointer/keyboard/focus in both themes, four native 150% layouts, reduced motion and independent package consumer | [Public API and milestone](alert-dialog.md); ARM64 runtime unverified |
| avatar | `BleeckerAvatar`, `BleeckerAvatarGroup`, `BleeckerAvatarData`: five sizes, images/initials/silhouettes and consumer collections | Matching `.23` x64 package and catalog pass Light/Dark states, passive input/focus, native 150% bounds, static reduced-motion behavior and independent consumer compilation | Accepted on x64: [contract and usage](avatar-reference.md); ARM64 runtime unverified |
| breadcrumb | `BleeckerBreadcrumb` and consumer-owned `BleeckerBreadcrumbItem` | Matching `.29` x64 catalog passes Light/Dark states, physical ellipsis press/Enter/focus and reduced-motion endpoints; independent consumer compiles and passes eight native 150% collapsed/full path cases | Accepted in `.29` for the five-check x64 milestone; ARM64 runtime unverified; see [reference/API](breadcrumb-reference.md) |
| context-menu | `BleeckerContextMenu`: original native actions with bindable modal option and owner-aware focus | Current package DLL passes physical owned-modal click, Light/Dark states, 150% no-clipping, disabled open/close animation and package-consumer compilation | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](context-menu.md) |
| date-picker | `BleeckerCalendar`, `BleeckerDatePicker`: controlled local-date values and native popup requests | Accepted on x64 in `.80`: Light/Dark states, physical pointer/keyboard/focus, eight native 150% calendar/open-popup cases, static reduced-motion popup, independent package-consumer compile | ARM64 runtime unverified; historical diagnostics remain in the [component contract](date-controls-reference.md) outside this milestone |
| dialog-close-button | `BleeckerDialogCloseButton`, native commands, adjustable Lucide X and consumer-owned dismissal | Matching `.30` extracted x64 catalog passes 16 Light/Dark states, physical pointer/Space/Enter/focus, reduced-motion endpoints; independent consumer compiles and passes four native 150% icon/disabled cases | Accepted in `.30` for the five-check x64 milestone; ARM64 runtime unverified; see [contract](dialog-close-button.md) |
| drawer | `BleeckerDrawer`: retained caller content, controlled close requests and a scrolling bottom surface | Accepted on x64 in preview .72: shared 194 Light/Dark states and 42 lifecycle/content cases, physical pointer/Escape/focus in both themes, 20 family native 150% layouts, reduced motion and independent package consumer | [Contract and milestone](drawer-sheet.md); ARM64 runtime unverified |
| progress-stack | `BleeckerProgressStack`: consumer values/brushes and wrapping legend | 140 Light/Dark states, twelve lifecycle phases, passive keyboard/focus and physical segment pointer, sixteen 150% native scale cases | Accepted on x64 under the [five-check milestone contract](acceptance-contract.md); [component usage](progress-stack.md) |
| empty | `BleeckerEmpty`: passive title/description, default or consumer icon, and consumer action | 120 Light/Dark states, twelve lifecycle phases, consumer input, and twelve 150% native scale cases passed | Accepted on x64 under the [five-check milestone contract](acceptance-contract.md); [component usage](empty-reference.md) |
| eyebrow | `BleeckerEyebrow`: three tones, optional rule and language-aware plain-text casing | Current x64 package passes Light/Dark language-casing, rule layout, passive focus, static motion, 150% no-clipping and consumer compilation | In progress: rich-content uppercase inheritance differs from the pinned source; [usage and remaining check](eyebrows.md) |
| hover-card | `BleeckerHoverCard`: original trigger/content, inline hover placement and source opacity clock | Exact `.67` package/catalog pass 44 Light/Dark states and five contracts; physical hover, keyboard/focus and reduced-motion endpoints pass; independent consumer passes eight native 150% layouts | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](hover-card.md) |
| input | `BleeckerInput`: three sizes, adornments, validation and native editing | Exact `.57` x64 catalog: Light/Dark input, editing, pointer and focus states; independent package consumer: 48 native 150% Light/Dark size/width/error/adornment layouts with stable reduced-motion border paint | Accepted on x64 for this milestone; ARM64 runtime unverified; [reference](input-reference.md) and [usage](inputs.md) |
| kbd | `BleeckerKbd`: single content and observable key combinations | Current package DLL passes 64 Light/Dark content states, 32 corner states, 16 caller and 64 metadata contracts; native 150% single/combination captures stay inside bounds | Accepted on x64 for this milestone; static display has no action, tab stop or animation; ARM64 runtime unverified; [usage](keyboard.md) and [contract](kbd-reference.md) |
| label | `BleeckerLabel`, explicit typography and input association | Focused x64 catalog passes Light/Dark states; independent consumer passes physical nested pointer/keyboard/focus, eight native 150% content cases, static reduced motion and package compilation | Accepted for the x64 milestone; ARM64 runtime unverified. [Component record](labels.md) and [reference](label-reference.md) |
| menubar | `BleeckerMenubar`, original caller menu entries and controlled selection requests | Exact `.86` package-matching x64 catalog passes 30 Light/Dark states, 28 contracts, physical keyboard/pointer/focus in both themes; independent consumer passes four native 150% layouts and static reduced-motion popup | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](menubar.md) |
| navigation-menu | Original caller links/groups, guarded host routing and typed custom content | Exact `.85` package-matching x64 catalog passes 32 Light/Dark states, 22 contracts and physical keyboard/pointer/focus in both themes; independent consumer passes four native 150% layouts and static reduced-motion popup | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](navigation-menu.md) |
| otp-input | Native controlled text/password preview | Registered fictional catalog, 60 source states and 32 native behavior cases | Physical input/clipboard, scaling, full visual/motion and ARM64 runtime acceptance |
| popover | `BleeckerPopover`: original trigger/content, controlled requests, source placement and shared modal branches | Exact `.64` x64 consumer: 122 Light/Dark states, five contracts, six hosted cases, four native 150% layouts; extracted catalog: physical pointer/keyboard/focus, nested/modal and Escape in both themes | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [usage](popover.md) |
| progress | `BleeckerProgress`: three sizes, four variants and optional label | x64 Light/Dark matrix and passive range semantics pass; `.9` package consumer passes 24 native 150% cases and reduced-motion value changes settle immediately | Accepted for the x64 milestone; ARM64 runtime unverified. [Usage and evidence](progress.md) |
| radio-group | `BleeckerRadioGroup` and immutable `BleeckerRadioOption` | Registered native fixture; 56 source/native frames and 136 options have zero geometry error; physical keyboard, controlled requests, UIA and lifecycle checks in both themes | In progress: [contract and evidence](radio-group.md); full raster/motion, customization/error matrix and Windows platform acceptance |
| checkbox | `BleeckerCheckbox`: native selection with consumer label/description content | Registered fixture; v7 x64 catalog/consumer pass 72/70 rich-name, binding, style and native-toggle checks | In progress: [usage and gaps](checkboxes.md); complete rendering, physical input/cursor/focus, scaling, motion and ARM64 runtime |
| scroll-area | BleeckerScrollArea: original content/templates, native scrolling, source scrollbar recipe and public scroll pattern | Exact package DLL passes 94 Light/Dark layouts, 12 native histories and five contract groups; extracted x64 catalog passes physical pointer, wheel, Tab and keyboard scrolling; independent consumer passes ten native 150% layouts and static reduced-motion offsets | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and evidence](scroll-area.md) |
| separator | `BleeckerSeparator`: horizontal/vertical, decorative or semantic | Light/Dark states, passive input/focus semantics, 150% no-clipping, static reduced-motion behavior and current candidate package-consumer compilation pass | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](separator.md) |
| sheet | `BleeckerSheet`: retained caller content, controlled close requests and four physical sides | Accepted on x64 in preview .72: shared 194 Light/Dark states and 42 lifecycle/content cases, physical pointer/keyboard/focus in both themes, all four sides at native 150%, reduced motion and independent package consumer | [Contract and milestone](drawer-sheet.md); ARM64 runtime unverified |
| slider | `BleeckerSlider`: controlled raw/sanitized values and native range semantics | 182 exact hidden layouts, 14 retained-fill states, 36 ownership/lifecycle checks and 44 corruption negatives; multilingual font and styled-name regressions | In progress: [contract](slider.md); visible input/raster/motion, scaling, shared-consumer integration and platform acceptance |
| sonner | Native host/toast/action API and registered fictional fixture | [Source preparation](sonner-reference.md) and [native preview](sonner.md); source/fresh NuGet match 66 hidden states and 20 checks, 53/49 auditor rejections; 418 portable tests, both builds and isolated consumers pass | Physical input, full visual/motion/customization/modal and ARM64 runtime acceptance |
| switch | `BleeckerSwitch`: native binary toggle and optional label | x64 light/dark native layout, UIA/binding/lifecycle, live appearance, physical Space/Tab/track/label/RTL, Japanese fallback repair, isolated 100–200% display-scale rasterization and independent consumer passed; composed screenshots retained | In progress: [usage and gaps](switches.md); exact raster/motion/drag/cursor/focus, physical monitor and system text scaling, and remaining ARM64 desktop checks |
| toggle | Six native `ToggleButton` styles | Registered size/variant fixtures; x64 automation/state and binding/command checks; desktop pointer and Space/Tab; 48 hidden native theme/raster-scale style observations in source and exact NuGet consumer, with 16 matching PNG pairs | In progress: physical DPI/text scaling, reduced motion, complete visual review and ARM64 runtime; see [usage](toggles.md) |
| toggle-group | `BleeckerToggleGroup` and native toggle items; optional single/multiple selection | Registered binding, collection, disabled and constrained fixtures; x64 UIA and physical input; package consumer; ARM64 build | In progress: scaling, motion, broader RTL/error fixtures and remaining ARM64 state checks; see [usage](toggle-groups.md) |
| tooltip | `BleeckerTooltip` and service; shared modal plane with standalone constrained Popup | Matching `.27` x64 extracted catalog passes Light/Dark placement/content states, physical Top hover/leave, keyboard focus/Escape and reduced-motion dismissal; independent package consumer passes 16 native 150% narrow/wide placement cases and compiles | Accepted for the x64 milestone; ARM64 runtime unverified; see [usage](tooltips.md) |
| feed-item | `BleeckerFeedItem`: retained author/avatar/time/relevance/content/categories and consumer-owned source requests | Feed items fixture; 100 source layouts/64 PNGs, 21 localized timestamp cases and actual tooltip bodies | In progress: [contract](feed-item.md); native/visible/platform/date-locale acceptance open |
| field | `BleeckerField`: retained label/control/action/message composition | 44 measured states, real input, external UIA ownership and package checks | In progress: [contract and evidence](field.md); exact raster, broader customization/scaling and native ARM64 |
| header-select | `BleeckerHeaderSelect`: original options, caller value requests and compact recipe | Exact `.68` package/catalog pass 292 Light/Dark states, eight histories and 58 contracts; physical keyboard/pointer/focus and reduced-motion endpoints; independent consumer passes four native 150% open-menu layouts | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](select.md) |
| hero-carousel | `BleeckerHeroCarousel`, original items/context/images, owning-window arrows and inactive input, fictional catalog | Exact `.87` package-matching x64 catalog passes 82 Light/Dark states and 34 contracts; independent consumer passes four native 150% no-clipping and reduced-motion cases; focused physical x64 keyboard/pointer/focus observations are reused from v7 with unchanged Hero source | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](hero-carousel.md) |
| field-help-tooltip | `BleeckerFieldHelpTooltip`, native button with explicit vector template | Matching `.27` x64 catalog passes Light/Dark states, physical hover/click/Enter/Space/focus and reduced-motion behavior; independent package consumer compiles and passes four native 150% theme/width cases | Accepted on x64 under the five-check milestone; ARM64 runtime unverified; see [usage](field-help-tooltips.md) |
| icon-badge | `BleeckerIconBadge`: two sizes, three variants, passive rich/scalar content | Published `.11` catalog passes Light/Dark variants, physical child click/Tab/focus and lifecycle; matching consumer passes fourteen native 150% cases, static motion and compilation | Accepted for the x64 milestone; ARM64 runtime unverified. Historical raster diagnostics remain in [the component record](icon-badge.md) |
| icon-button | `BleeckerIconButton`: three variants and three sizes | All nine combinations render in Light/Dark; matching .26 extracted x64 catalog passes physical pointer/Space/Enter/focus and reduced-motion press; independent package consumer passes 18 native 150% cases and compiles; ARM64 build-only | Accepted on x64 under the five-check milestone; ARM64 runtime unverified; see [usage](icon-buttons.md) |
| loading-spinner | `BleeckerLoadingSpinner`: three sizes, themed three-quarter ring | Current x64 package DLL passes 150% Light/Dark bounds and independent consumer compilation; matching catalog passes sizes, theme brushes, constrained row and static reduced motion | Accepted on x64 for this milestone; ARM64 runtime unverified; [usage](loading-spinner.md) |
| media-library | `BleeckerMediaLibrary`: caller-owned items, controlled search, selected preview and delete requests | Exact .84 x64 package consumer passed 50 Light and 50 Dark states and 38 behavior checks; matching catalog passed physical pointer Delete/Open and keyboard Tab/Space/focus in both themes; independent consumer passed four native 150% no-clipping cases and static reduced-motion checks | Accepted on x64; [contract and evidence](media-library.md). ARM64 runtime unverified |
| modal | `BleeckerModal`, caller-owned content/open state | Accepted on x64 in preview .71: 40 Light/Dark layouts, 26 ownership cases, physical nested open/Escape/focus in both themes, four native 150% layouts, reduced motion and independent package consumer | [Public API and milestone](modal.md); ARM64 runtime unverified |
| nav-menu | `BleeckerNavMenu`: caller-owned row/column links and original-item custom contexts | Exact `.56` x64 catalog: 42 Light/Dark states and eight physical input histories; independent package consumer: 16 native 150% Light/Dark direction/width/RTL layouts, stable reduced-motion theme repaint | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](nav-menu.md) |
| pagination | `BleeckerPagination`: controlled page requests and producer-owned availability | Exact `.55` x64 catalog: 66 Light/Dark states, four physical input sequences and 64 reduced-motion cases; independent package consumer: 24 native 150% Light/Dark page/width/RTL layouts | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and evidence](pagination.md) |
| collection-filters | `BleeckerCollectionFilters`: caller-owned filter and sort requests | Exact `.56` x64 catalog: 50 Light/Dark states, four filter and four numeric-key physical input histories; independent package consumer: 16 native 150% Light/Dark expanded/width/RTL layouts with stable reduced-motion presentation | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract and evidence](collection-filters.md) |
| command-spotlight | `BleeckerCommandSpotlight`: caller actions, query/selection, modal surface and hotkey | Exact `.87` package-matching x64 catalog passes 100 Light/Dark states, 24 contracts, 168 hotkey cases and 38 Windows key checks; focused v7 physical Light/Dark keyboard/pointer/Tab/focus checks are reused with unchanged component source; independent consumer passes four native 150% no-clipping and reduced-motion cases | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](command-spotlight.md) |
| select | `BleeckerSelect`: original options/value bindings, shared portal and native option peers | Exact `.69` package/catalog pass shared 292 Light/Dark states, eight histories and 58 contracts; physical keyboard/pointer/focus and reduced-motion endpoints; independent consumer passes four native 150% open-menu layouts | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [API](select.md) |
| file-input | `BleeckerFileInput`, original native files, caller picker boundary and fictional catalog | [Native preview](file-input.md): source/package/archive 68 states/30 checks; 434 portable/10 text tests, both builds/consumers and demos pass | Strict browser/native and source/package images, native system picker, physical input, scaling/motion and ARM64 runtime remain open |
| section-header | `BleeckerSectionHeader`: title, optional eyebrow/description and native level-two heading | Published `.12` x64 catalog passes 24 Light/Dark specimens and physical Tab; matching consumer passes sixteen native 150% content cases, static motion and compilation | Accepted for the x64 milestone; ARM64 runtime unverified. Historical raster diagnostics remain in [the component record](section-header-reference.md) |
| panel | `BleeckerPanel`: title/count, consumer slots and an independent scroll body | 266 zero-error layouts, 58 hash-bound raw PNGs, 29 audit negatives, native input/binding/template/keyboard/scroll checks, five windowless font tests and fresh package consumers | In progress: [contract and evidence](panel.md); 661,516 raw pixel differences, normal motion, broader overrides/scaling, native ARM64 and final integration |
| status-badge | `BleeckerStatusBadge`: five variants, consumer label/description and constrained reflow | Current package consumer passes 18 Light/Dark states and 26 contracts; passive focus, static motion and 150% no-clipping checks pass | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](status-badge.md) |
| table | `BleeckerTable` primitives and caller-owned content | Registered fixtures; source and fresh consumer geometry, live/template and empty inline-action checks | In progress: [contract](table.md); complete visual, input and platform acceptance |
| data-table | `BleeckerDataTable`, original caller rows/columns and controlled state | 208 source states, 84 live states, 98 table-template states and filter/performance regressions | In progress: [contract](data-table.md); published v7 focused paging passes semantics but next-page median is 1,751.42 ms against the 1,000 ms local target; full performance matrix and component acceptance remain open |
| tabs | `BleeckerTabs`, keyed items, controlled selection and host-owned navigation | Exact `.55` x64 catalog: nine Light/Dark variant/size fixtures and physical keyboard/pointer/focus; independent package consumer: 36 native 150% Light/Dark layouts with stable reduced-motion selection paint | Accepted on x64 for this milestone; ARM64 runtime unverified; [API and acceptance](tabs.md) |
| theme-toggle | `BleeckerThemeToggle`: native icon button, consumer-owned mode requests and caller metadata precedence | Matching `.43` x64 catalog passes 134 Light/Dark states, physical pointer/Space/Enter/focus, disabled/rejected requests and immediate reduced-motion press endpoints; independent package consumer passes 108 native 150% cases | Accepted for the x64 five-check milestone; ARM64 runtime unverified. Historical diagnostics remain in [the component record](theme-toggle.md) |
| app-shell | `BleeckerAppShell`: retained header/body/footer, viewport minimum and caller-controlled offset | Registered fixture, 168 hidden source layouts, 44 ownership/metadata checks and independent package consumers | In progress: [contract and evidence](app-shell.md); visible raster/input, scaling and native ARM64 acceptance |
| admin-shell | `BleeckerAdminShell`: retained header/sidebar and scrolling body/footer | 312 initial/16 scrolled zero-error layouts, 72 ownership checks, 60 hidden/39 foreground audit negatives, 266 portable tests and visible keyboard/pointer/scroll evidence; separate 68-frame text probe with 38 rejection checks | In progress: [contract and remaining acceptance](admin-shell.md); source raster, broader customization, scaling and native ARM64 acceptance pending |
| auth-shell | `BleeckerAuthShell`: centered/split retained caller slots and responsive aside | Registered fictional catalog;104 native states/24 checks across source/NuGet/archive;94 source/package audit negatives each;469 portable/10 Windows text tests; both architectures/consumers/demos;104 source/archive PNGs exact | In progress: [contract and remaining visual/input/platform acceptance](auth-shell.md); browser and one package-image gate failed |
| detail-layout | `BleeckerDetailLayout`: responsive main/rail tracks and bounded sticky placement | Matching .20 x64 Light/Dark catalog passed 888 layouts and two native input/focus sequences; independent package consumer passed 16 native 150% states and static reduced-motion behavior | Accepted on x64: [contract and evidence](detail-layout.md); ARM64 runtime unverified |
| footer | `BleeckerFooter` with original brand/section models, bottom slots and a global guarded link context | 48 pinned source PNGs; catalog and fresh package hidden 144 layouts/8040 exact geometry checks/1434 brushes/7 lifetime and threading checks/1349 isolation guards; 40 catalog/44 package damaged reports rejected; fixture; 287 portable tests; clean x64/ARM64 builds and isolated consumers | In progress: [contract](footer.md); visible input/raster, broader props, motion/scaling, integration and native ARM64 acceptance |
| feed-column | `BleeckerFeedColumn`: fixed header, original filter/body slots and independent native viewport | Exact .46 package passes 154 source states and 29 checks; physical input is unchanged since the Light-host contrast fix; eight native 150% cases and static reduced-motion behavior pass | Accepted on x64: [contract](feed-column.md); ARM64 runtime unverified |
| feed-grid | `BleeckerFeedGrid`: auto-fill tracks, variable rows and original caller children | Matching .19 x64 catalog passes 144 Light/Dark layouts and native child input/focus; 16 independent-consumer 150% cases, static reduced-motion behavior and package compilation | Accepted on x64: [contract](feed-grid.md); ARM64 runtime unverified |
| header | `BleeckerHeader`: original brand/navigation, caller-owned actions and responsive positioning | Exact `.66` x64 package passes Light/Dark states and contracts; extracted catalog physical mobile/desktop pointer, keyboard and focus pass; independent consumer passes six native 150% layouts and static reduced-motion endpoint | Accepted for the five-check x64 milestone; ARM64 runtime unverified; [contract](header.md) |
| page-frame | `BleeckerPageFrame`: centered width/spacing recipes and semantic grouping | Matching .20 x64 Light/Dark catalog passed 792 layouts and two native input/focus sequences; independent package consumer passed 16 native 150% states and static reduced-motion behavior | Accepted on x64: [contract and evidence](page-frame.md); ARM64 runtime unverified |
| panel-layout | `BleeckerPanelLayout` / `BleeckerPanelColumn`: fixed/growing tracks, retained caller controls and horizontal scrolling | Matching .21 x64 Light/Dark catalog passed 86 layouts and two native input/focus sequences; independent package consumer passed 12 native 150% states and static reduced-motion behavior | Accepted on x64: [contract and evidence](panel-layout.md); ARM64 runtime unverified |

| brand-mark (Sabella) | `BleeckerBrandMark`, original template PNG and caller-owned size/tint | Current package passes 37 Light/Dark states and eight native 150% size/theme checks; independent consumer compiles | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](brand-mark.md) |
| secure-field (Sabella) | `BleeckerSecureField` | Exact `.59` x64 catalog and consumer Light/Dark checks; unchanged-source visible pointer/keyboard/focus evidence; independent package consumer 32 native 150% layouts with static reduced motion | Accepted on x64 for this milestone; ARM64 runtime unverified; [contract](secure-field.md) |
| segmented-control (Sabella) | `BleeckerSegmentedControl` / `BleeckerSegmentOption` | Current package: 40 Light/Dark cases, eight layouts, physical pointer/keyboard/focus in both themes, 16 native 150% no-clipping cases, immediate reduced-motion behavior and independent consumer compilation | Accepted on x64: [contract](segmented-control.md); ARM64 runtime unverified |
| app-tab-bar (Sabella) | Native compact destinations with typed selection, equal tracks, icons and badges | Exact .47 package passes 58 cases and eight Light/Dark layouts; focused physical input is unchanged; eight native 150% layouts and static reduced-motion behavior pass | Accepted on x64: [contract](app-tab-bar.md); ARM64 runtime unverified |

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
- x64 runtime is the acceptance platform. ARM64 is build-only and labelled runtime-unverified for this milestone.
- Component accessibility requires keyboard/focus behavior and UI Automation names, roles and values. Narrator and OS contrast campaigns are excluded. Component performance checks use representative local workloads.
- Independent package consumer, versioned NuGet and runnable catalog, matching documentation, remote main and required CI are still required.

The GitHub wiki could not be retrieved. Documentation remains in this repository. Thompson is a supplementary contract reference; its touch-specific behavior does not override desktop Windows conventions. Sabella television/mobile-specific types are outside this Windows scope. Web providers and implementation utilities map to native infrastructure rather than separate visual controls. No networking, CMS schemas or product workflow logic enters Orchard.

