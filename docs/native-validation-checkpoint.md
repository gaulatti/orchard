# Current native validation checkpoint

## Picker and range geometry and basic paint

The date presentation follow-up passes all remaining 56 Picker/Range source
states at 320/640px in Light/Dark. The independent audit checks 964 boxes,
620 popup days, 132 basic paints, 80 owned pointer gates and 20 Escape transfers.
Source measurements expose and verify intrinsic/action width rounding, equal
range tracks, pixel-aligned popup offsets, 20px caption line height and the full
254px Clear date action. All 142 unit tests pass. Adding the inline clear-action
measurement preserves every earlier source measurement and all 188 PNG hashes.

Reports are `artifacts/current/dates-presentation-audit.json`,
`dates-presentation-320`, `dates-presentation-640` and
`date-clear-source-audit.json`. Normal project publish directories now contain
this newer candidate; the versioned source48cca09 artifacts below retain their
own earlier validation scope. Exact raster/shadow/motion, remaining customized
consumer paths and Windows platform acceptance remain open. Inventory stays
63 in progress, 49 pending and zero accepted.

## Native date-control preview

The date controls have a [native preview and source contract](date-controls-reference.md)
with 188 standards-mode Light/Dark frames at 320/640px and 48 trusted browser
input sequences. These cover controlled selection, month retention/remount,
timestamp bounds, disabled predicates, picker focus/dismissal, range clearing,
presets and both DST boundaries. Reference/model/resource checks now total
141 passing tests; tokens pass. Every source PNG matches its frozen hash.
`BleeckerCalendar`, `BleeckerDatePicker` and `BleeckerDateRangePicker` implement
controlled requests, timestamp bounds, local date strings and presets, native
day Toggle/trigger ExpandCollapse automation, owned popup dismissal and focus.
The registered Dates fixture follows Textareas. Inventory is 63 in progress,
49 pending and zero accepted (56.3% started, not complete).

The focused native runner checks 132 Calendar state frames at 320/640px in
Light/Dark, plus controlled binding, four-row months, selection/clear, Escape,
outside dismissal, rejected requests, live month retention/remount, range bounds
and keyed preset replacement. Lifecycle checks cover disable, template replacement,
stale automation and unmount. `scripts/verify-date-controls.mjs` independently
audits geometry/state/names and real input receipts. Picker/Range's complete
source geometry and paint matrix, exact raster/shadow/motion, remaining custom
content/error paths, real contrast/Narrator/scaling and native ARM64 execution
remain unaccepted.

The normal project publish directories contain the current date candidate.
The versioned `3a0e8e1` archives below retain their original source identity;
they must not be confused with the newly built publish directories. Full catalog,
independent consumer and versioned date-preview validation is tracked separately.
Push-triggered Actions stay paused; no workflow is dispatched.

Source `48cca09c146e7bf4159d650f142700edc7957a77` publishes successfully for
x64 and ARM64. The focused date audit passes 132 frames, 4,060 day comparisons,
28 interaction/lifecycle phases, 16 closing-surface pointer pairs, 308 stable
pointer gates and 36 owned keyboard transfers. Its machine report is
`artifacts/current/dates-native-audit.json`.

Package `Gaulatti.Orchard.0.1.0-48cca09.nupkg` has SHA-256
`f59b9c673b12b8cb69a6be0122a2ab50a02f4d69933673e33a6dcf39f98f81e0`.
Its independent x64 consumer passes the complete Light/Dark runtime, including
eight date key pairs and six date requests. The ARM64 consumer builds; native
ARM64 runtime is unverified. Both consumer libraries match the packaged library
SHA-256 `a8d92169053b61a420450dd58b896f625cc5cc898d21c76a92ac50fee19b56b8`.
Both temporary source/feed/cache workspaces were removed. The first package
attempt failed because the new date check reused the unrelated off-screen
Textarea presentation predicate. The repaired check requires visible stable
bounds for the actual date target and preserves its report on failure.
`dates-package-audit.json` records the passing result.

The source-identified catalog archives contain 537 x64 and 533 ARM64 files.
Every archived file's bytes match the normal publish directories. Archive SHA-256
values are:

- x64: `24c2eda34fa6c736256b4aabcf1c27d4b57ae9d7181866b72002f6e9c10b02a6`.
- ARM64: `76944f1e419ae04f335acb3bf2e52618eb2ea9d7a8e9be47f456c6fdd378732c`.

`artifacts/current/dates-catalog-archives.json` retains paths and file counts.

The complete x64 catalog at this source passes 152 phases and 1,150 captures in
469.1742111 seconds under the unchanged 480-second watchdog. It retains all
1,148 prior capture names and all 174 protected chart glyph hashes. The audit
verifies 2,032 owned pointer moves/presentation gates, 1,764 owned keyboard
actions, 92 routed clicks, the two native Dates selection press/release pairs,
and all 20 existing live Textarea phases. The new Dates fixtures each verify
three consumer updates and restore their original sample values before capture.
Reports are `artifacts/current/native-full-dates-audit.json` and
`native-full-dates`; the retained `native-full` directory is the comparison
baseline. This is runtime/integration evidence, not full component acceptance.

## Regenerated deliverables after the 2026-10-17 reset

Source `3a0e8e16f5f6c2ad18c3ba2448f2c81e803827a5` includes the chart
early-key correction and live Textarea catalog/package regressions. Both x64
and ARM64 catalog publishes pass. All 130 unit tests and the pinned token
generation check pass. The independent x64 package consumer passes its complete
Light/Dark runtime, including six real context-key phases. The independent
ARM64 consumer builds from the same package; native ARM64 runtime remains
unverified. Both isolated temporary source/feed/cache workspaces are removed.
Both consumers' library bytes match the packaged library.

The regenerated complete x64 catalog passes all 150 phases in
467.3885367 seconds under the unchanged 480-second watchdog. Its 1,148 root
captures, 2,024 owned pointer moves/presentation gates, 1,764 owned key actions,
86 clicks and 174 valid chart glyph reports pass the current-state audit.
The 20 live Textarea phases and six native editing keys pass. The committed
Sparkline verifier passes all 36 source cases, 76 pointer gates and 38 owned
Right pairs, including physical Right before asynchronous `GotFocus` in both
themes. Reports are `artifacts/current/native-full-audit.json`,
`sparkline-pointer-audit.json` and `native-capture-manifest.json`; raw output is
in `native-full`. The original comparison baseline was deleted, so previous
capture-name and glyph-hash retention are explicitly not reverified by this
new audit. Current captures provide a new baseline for subsequent comparisons.

The current package and archives are below `artifacts/current`:

| Artifact | SHA-256 |
| --- | --- |
| `packages/Gaulatti.Orchard.0.1.0-3a0e8e1.nupkg` | `659e731326541f91203f78274792e4f2763fba59f5b2ba66c4abd2f8de8edb20` |
| `Orchard.Catalog.0.1.0-3a0e8e1-x64.zip` | `b557071aec1fc72d1ac6723cfdd83cf7c378f3748eafe48c929181d56f38f94b` |
| `Orchard.Catalog.0.1.0-3a0e8e1-ARM64.zip` | `fa31621b4dfad3d038c6f78512c54bc5869b75591698a521e81606d4da794f8e` |

Every ZIP entry matches its published file: 537 for x64 and 533 for ARM64.
PE headers confirm each native executable's architecture. `archives.json`
records these checks. Consumer reports are in `consumer-x64` and
`consumer-ARM64`; their runtime paths record the original staging location
before the verified output set was moved into `artifacts/current`.

The user reset removed the earlier local binaries, screenshots and reports.
Older sections below are historical verification records; their paths no longer
imply retained files. Source/reference JSON remains committed. See
[artifact storage](artifacts.md) for the bounded working set and reset behavior.
The inventory remains 61 in progress, 51 pending and zero accepted. Exact raster,
complete motion/customization, scaling, contrast/Narrator, collection performance,
both catalog compositions and current native ARM64 acceptance remain open.
Push-triggered Actions remain paused; no hosted verification was started.
The public wiki remains unavailable; documentation stays in this repository.

## Textarea live integration and early chart inspection

The x64 complete catalog at `artifacts/textarea-catalog-integrated-fixed-full`
passes all 150 phases in 469.1074913 seconds under the unchanged 480-second
watchdog. It retains all 1,146 prior PNG names and adds Light/Dark Textarea
fixtures, for 1,148 total. The integration audit verifies 2,024 owned pointer
moves/presentation gates, 1,764 owned key actions, 86 native clicks and all 174
protected Radial Bar/Sunburst glyph geometry hashes. This is glyph retention,
not raw raster acceptance.

The actual catalog sequence now exercises Textarea after Input and before
Keyboard shortcuts. Its 20 live phases cover size/rows, native paragraph
spacing, real Enter/character editing through two-way binding, read-only
consumer updates and rejected editing, owner theme changes, validity overrides
and restoration. The focused verifier rejects 12 deliberately corrupted
reports. Full-sequence integration and source-sized Textarea raster acceptance
remain distinct: the latter is still open.

The first full attempt exposed physical Right arriving after native Sparkline
focus but before asynchronous `GotFocus`. Ordinary chart inspection now
initializes from the actual focus owner before applying the key. The same
deterministic native harness fails with the old library (10) and passes with
the corrected library (30), with preview before `GotFocus` in both themes.
Its 36 pointer cases, 76 pointer moves/gates and 38 owned Right pairs pass.
The failed full run and focused before/after evidence are retained. All 130
unit tests pass. The later chart correction still needs a current ARM64 build
and current versioned artifacts; native ARM64 execution remains unverified.

The versioned `0.1.0-textarea-commands` package is explicitly library source
`751a309`, predating that later chart correction. An independent x64 consumer
passes full Light/Dark runtime and six real context-key phases after removing
its temporary source and restored Orchard cache entry. External SDK dependency
caches are reused. See [Textarea artifact identities](textarea-reference.md#versioned-preview-artifacts).
Push Actions remain disabled; no workflow was dispatched.

## Textarea length limits and delayed paste

The new frozen browser and x64 native matrix passes all 56 cases at 320/640px in
Light/Dark: typing, Enter, paste, CRLF, surrogate boundaries, selection capacity,
programmatic overflow, replacing overflow, zero-capacity selection deletion,
live limits, unlimited and read-only. Native `MaxLength=0` retains the Windows
unlimited convention. The former direct document paste exceeded the limit in
18 baseline cases; that failure is retained. Paste now applies the current limit
after normalizing LF and preserves a complete surrogate pair at the boundary.
One undo restores the original consumer value, including blocked paste cases.

Thirty-six delayed-provider cases hold the actual clipboard request and verify
current limits plus content, selection, read-only, disabled, template and
normal/rapid-remount ownership. Checks await the exact task started by real
Ctrl+V before accepting cancellation. Each load advances the view version.
Four real menu clicks prove that branded Paste uses the same limit and binding,
closes its popup and remains one undo edit; read-only editing items are disabled
while Copy remains available. Menu press/release is observed at the exact native
input source because closing a popup can suppress routed XAML release; no routed
release claim is made. All native key actions retain their original target.
Native Ctrl+A is used for full replacement; its immutable final paragraph is
excluded when computing user capacity. Together the new runs contain 188 owned
key actions. Both audits reject twelve corruptions. All 129 unit tests and
generated tokens pass. Both architectures
build; native ARM64 runtime is unverified.
The final original Textarea regression also passes all 172 state frames, four
editing sequences, eight external UIA/grip snapshots and 60 owned keys. Its
existing geometry, native font, border paint and consumer-binding audit passes.

Evidence: `artifacts/textarea-limits-final-runtime-{320,640}`,
`artifacts/textarea-limits-final-{320,640}-audit.json`, corresponding negative
audits, and `artifacts/textarea-limits-final-tests`. Browser reference, source hashes
and trusted clipboard/key events are committed in
`tests/Reference/TextareaLimits.json`. The `TextareasFixture` adds a 12-character
editor. A first-click receipt failure at an unchanged restored pointer position
is retained; the fixture now establishes a real owned move first. Existing input
deadlines and receipt checks are unchanged.
Final editing evidence is in `artifacts/textarea-limits-final-editing-{320,640}`
and its corresponding audit files.

This remains an in-progress component. Provider-error handling, other menu
invocation/opening paths, remaining customization, full catalog integration,
exact raster/motion, scaling, contrast/Narrator and native ARM64 remain open.
Previous versioned preview packages predate this change. Inventory is still
**61 in progress, 51 pending, zero accepted**. Push Actions stay disabled; no
hosted validation is claimed. There is no backend or metrics impact, and the
unavailable public wiki remains documented in-repository.

## Textarea pointer resizing

The separate frozen browser reference and native x64 matrix both pass all 72
cases: 320/640px, Light/Dark, three sizes, editable/read-only/disabled and LTR/RTL.
Real mouse drags preserve text, width, disabled/read-only state and editor focus;
they match the 48px enlargement, recipe minimum and overshoot/reversal anchor.
The actual Windows cursor is checked before and during each drag. Root capture
is released on pointer release, capture loss, template replacement and unload.
Twelve additional Light/Dark ownership phases verify occlusion, clipping,
stationary unload, live direction changes and cursor restoration. The audits
reject ten corruptions per width. All 126 unit tests, token generation and the
existing Input runtime pass. Both catalog architectures build; native ARM64
runtime remains unverified.

The final explicit resize-grip automation peer is checked from a separate UIA
process in both widths/themes and editable/read-only modes: eight named,
focusable `ControlType.Thumb` snapshots. The strengthened original Textarea
suite passes all 172 states, four editing sequences and 60 owned keys; its audits
reject eleven corruptions per width, including a lost resize provider. The final
peer build also passes all twelve pointer ownership phases.

Evidence is in `artifacts/textarea-resize-direction-runtime-{320,640}`, the
corresponding `textarea-resize-{320,640}-audit.json` and negative audits,
`artifacts/textarea-resize-ownership-runtime`, and
`artifacts/textarea-resize-input-regression`. Final peer evidence is in
`artifacts/textarea-resize-peer-runtime-{320,640}` and
`artifacts/textarea-resize-peer-ownership-runtime`. The earlier RTL cursor failure is
retained: the editor's mirrored local coordinates concealed a physically right
grip in its LTR chrome. Native hit testing now transforms to the rendered grip;
live RTL moves that grip to the physical left edge.

This closes the measured pointer resize boundary, not complete Textarea
acceptance. Exact raster/motion, remaining constraints and clipboard/menu paths,
full catalog integration, scaling, contrast/Narrator and native ARM64 remain
open. The previous preview packages below predate this resize change. Counts
remain **61 in progress, 51 pending, zero fully accepted**. Push-triggered
Actions remain disabled and no hosted validation is claimed. The public wiki
remote is unavailable; documentation remains in this repository. There is no
backend or telemetry impact.

## Textarea native multiline preview

The x64 Textarea preview passes both 320/640px viewport suites: 172 source-sized
native frames, four editing/lifecycle sequences, eight external UIA snapshots
and 60 owned key actions. The audits reject ten corruptions per viewport. This
covers rows sizing, native paragraph spacing, font asset binding, error validity
override, editing/undo/read-only selection, two-way consumer data, plain-text
shortcut/paste behavior, keyboard resize, template replacement and remounting.
All 125 tests, token generation and the existing Input runtime pass.

The strengthened 172-frame comparison has 1,001,348 differing pixels, without masks or
resampling. Pointer resizing/cursor, clipboard-provider races/caret behavior,
menu invocation, remaining constraints/customization, full catalog integration
and architecture/runtime acceptance are still open. A fresh x64 package consumer
passes its full Light/Dark suite with the original Textarea visibly reparented,
exact read-only document updates, rows and native bound editing; its isolated
cache was removed before launch. This exposed and fixed a stale readiness flag.
Readiness now follows the loaded native template part. Enter, trailing newlines
and the paste caret also have direct assertions. Failed consumer runs are retained.
The legacy external UIA client cannot query validity; no success is inferred from
its null field. Native/direct-paste and initial font/layout failures are retained.
Counts are **61 in progress, 51 pending, zero fully accepted** (54.5% started).
Push-triggered Actions remain disabled; no hosted acceptance is claimed.

Committed code `c8ac17f` builds for x64 and ARM64. Both libraries match all 6,286
resolved methods from the runtime-tested candidate; x64 library PRI bytes also
match. The versioned `0.1.0-textarea-readiness` package contains the committed
library and is cross-built by a separate fresh ARM64 consumer. Both catalog ZIPs
have every entry hash checked. See the [artifact identities](textarea-reference.md#versioned-preview-artifacts).
The fresh x64 runtime predates this metadata-only rebuild and is linked through
method/resource identity; no additional runtime execution is implied.

## Textarea source baseline before native implementation

The pending Textarea now has a committed [source contract](textarea-reference.md),
172 fresh browser state frames, and four trusted editing/undo/read-only/resize
sequences containing 568 recorded input events. All 172 PNG hashes were verified.
The reference preserves intrinsic rows sizing, the actual themed CSS cascade,
the explicit accessibility override and the vertical resize minimum. All 125
unit tests and token generation checks pass. No library, catalog or package
behavior changed in this checkpoint, so the native evidence below remains at
its stated source commits. Textarea is still pending; counts remain 60 in
progress, 52 pending and zero fully accepted. Push Actions remain disabled.

## Complete catalog sequence after Input alpha and Metric

Catalog source: `1abca74`. The x64 full sequence passes in **463.2165656 seconds**
under the unchanged 480-second watchdog. Both appearances complete all 148
profiled phases. All 1,132 prior PNG names remain, plus twelve Metric frames
and two Search Input fixture frames: **1,146 PNGs** total. All 174 protected
Radial Bar/Sunburst **glyph geometry hashes** match; this is not raster parity.
The audit verifies 2,016 pointer presentation gates, 1,758 owned key actions and
82 retained native clicks. Maximum presentation gate duration is 229.7422 ms;
the original two-second per-state deadlines remain unchanged.

The run exposed two stale integration assertions. Mixed-case `iNpUt` must
return both Input and Search inputs, and all future matching entries, in the
original registry order; activation now targets the actual Input fixture.
The disabled Checkbox/Input cursor handover must inspect opaque border RGB
and fractional opacity after Input's alpha migration. All six stationary
remounts pass in Light/Dark, with maximum settled duration 122.3765 ms.
Native No-cursor ownership and two stable rendered observations remain required.
The original startup and Light remount failures are retained separately.

Metric's 680 native cases, source interaction contract, slot placement/lifecycle
checks and eight corruption negatives also pass inside the complete sequence.
All 122 unit tests pass. The library's 6,137 resolved methods/exposed interfaces
and PRI resource bytes are unchanged from the independently validated `8bc623c`
package. These corrections affect catalog verification only; that package's
fresh x64 runtime and ARM64 cross-build evidence remain applicable.

The corrected catalog also cross-builds for ARM64; all 6,137 resolved methods
and exposed interfaces match its x64 build. Every archive entry is verified.
Native ARM64 runtime was not run.

| Catalog artifact | SHA-256 |
| --- | --- |
| `Orchard.Catalog.0.1.0-1abca74-x64.zip` | `a3e29d66a684b81197132984da526254460ef76cb84fc60a9046b022942d3dd4` |
| `Orchard.Catalog.0.1.0-1abca74-ARM64.zip` | `5308a8b914fa1d03a08eb43284972d67f7e88bb23366dab4d87805944c5cc10b` |

Evidence: `artifacts/input-alpha-remount-full`,
`input-alpha-remount-full-audit.json`, `input-alpha-remount-full-profile.json`,
`metric-in-full-audit.json`, `checkbox-input-alpha-remount-audit.json` and
`full-sequence-library-method-audit.json`, `full-sequence-architecture-audit.json`
and `full-sequence-archives.json`. Earlier failed runs are
`metric-published-full-x64` and `catalog-navigation-matches-full`.
Full-suite success does not establish exact raster, actual scaling/contrast,
Narrator, collection performance or native ARM64 acceptance. Inventory remains
60 in progress, 52 pending and zero accepted. Actions remain paused on push;
the goal stays active. The wiki remains unavailable.

## Metric formatting and stable consumer slots

Code source: `8bc623c`. Metric now supplies source Number/Currency/Percent/Compact
formatting, nullable and numeric data bindings, culture/precision updates and
original consumer prefix/suffix content. All 680 frozen/native text and name
cases pass across four locales and both themes; all 122 unit tests and token
checks pass. Twenty source browser interaction states establish stable button
identity and zero focus/lifecycle events across updates and outside focus.

Rich controls remain mounted in a sibling slot plane while noninteractive
inline spacers follow their actual arranged sizes. This avoids WinUI's native
inline-child unload during text mutation, without restoring focus or adding
pending input handlers. Real Space/pointer input, 27 batched values, format and
prefix updates, original content, template replacement and remounts pass.
Eight constrained/RTL placement cases and six native-template/fractional-font
cases pass with zero geometry error under the unchanged .05-DIP bound. Eight
corruption negatives are rejected. The twelve raw frame comparisons still
record 12,286 differing pixels; there are no masks or resampling.

Both architectures build and all 6,137 resolved methods/exposed interfaces
match. The fresh x64 package consumer passes its complete suite in both themes,
including rich slot presentation and zero update focus/lifecycle events. The
ARM64 consumer passes cross-build validation only. Both isolated caches are
removed. Package, tested x64 catalog and fresh consumer library bytes match;
every entry in the 536-file x64 and 532-file ARM64 archives is hash-verified.

| Artifact | SHA-256 |
| --- | --- |
| `packages-metric-rounded/Gaulatti.Orchard.0.1.0-metric-rounded.nupkg` | `467fa34cb0bef318eb6387c33b2968174aac981e25cbcff3af569102d01ed96b` |
| Package / x64 catalog `Orchard.dll` | `5d94fb35210c7b650a562b7094e9282892a43611026ed8c8093746129bf3f99f` |
| ARM64 catalog `Orchard.dll` | `b0dcbe78537dda8378dea594b745052eb1ee13fbf8490ba516fd5c589d50f129` |
| `Orchard.Catalog.0.1.0-8bc623c-x64.zip` | `b9760039351a1fc56c235f34a9b15135fb3a1f208f45ac183d474bc16a8c3b49` |
| `Orchard.Catalog.0.1.0-8bc623c-ARM64.zip` | `0939eebb26ec55711d9ca6d952e8ecd28d0e2e3f958f8f8a4c0b5ae6a4155e30` |

Evidence is under `artifacts/metric-rounded-*`, `metric-native-rounded-x64`,
`package-consumer-metric-rounded-*`, `metric-rich-browser` and `metric-archives.json`.
Earlier focus-restoration prototypes and the first fresh-consumer timeout are
retained; neither is presented as a passing result. The corrected consumer
passes the original timeout. No Windows setting changed and no workflow was
dispatched. Push-triggered Actions remain paused. The full catalog sequence was
not rerun. Exact raster, full rich/customization/lifecycle/accessibility matrices,
actual scaling/contrast, Narrator and native ARM64 runtime remain open.
Inventory remains 60 in progress, 52 pending and zero accepted of 112. The goal
stays active. The wiki is unavailable; see [Metric](metric-reference.md).

The previous cleanup attempt was rejected by automatic approval review with
“blocked by policy.” Its ten intermediate build paths were not retried.

## Search Input activation, focus and theme states

Source: `7df5542`. The frozen browser state matrix exposed Enter-on-release,
lost pointer release at the pressed clear edge, an oversized/unscaled focus
outline and a fixed Light sand hover color in Dark. Shared inline actions now
invoke Enter on key-down through native Button Invoke, keep Space/pointer
activation on release, retain the original transparent hit surface and render
the keyboard cue inside the scaled visual. Search hover consumes the shared
theme sand brush with the correct alpha and live brush/opacity observation.
The catalog explicitly imports and renders its palette style.

All 48 source/native state cases pass at 320/640 in Light/Dark. Geometry error
is at most `.00006103515625` DIP under the unchanged `.05` bound. Source RGB,
fractional alpha, focus and pressed artwork/outline bounds, key-down/release
counts and consumer-owned text are verified. Twenty held key pairs and eight
held pointer pairs retain real native delivery and exact owners. Four live
palette checks cover source mutation, replacement, alpha changes, restoration
and a theme round trip on the same control. Sixty-eight paint readiness gates
require two matching rendered frames within two seconds; maximum observed
duration is 37.044 ms on this reduced-motion host. Eight corruptions are
rejected, including Enter-on-release, missing edge ownership and unscaled focus.

The original 40-case Search Input matrix remains at zero geometry error and
83,452 differing pixels. The new, separate 48-state whole-frame matrix records
142,074 differing pixels. Both comparisons have zero masks and no resampling;
neither establishes source raster parity. The real catalog fixture, all 118
unit tests and token checks pass. FilterChip's 20-case geometry regression and
28 property-event / 22 retained-invocation checks pass. Both architecture
builds pass and all 6,062 resolved methods/exposed interfaces match.

The fresh x64 package consumer passes both themes, including held Enter's
key-down-only invocation and unpressed appearance. ARM64 consumer validation
remains build-only. Both isolated temporary caches are removed. Package and
tested x64 catalog library bytes match. Every entry in the 536-file x64 and
532-file ARM64 catalog archives is verified.

| Artifact | SHA-256 |
| --- | --- |
| `packages-search-input-states/Gaulatti.Orchard.0.1.0-search-states.7df5542.nupkg` | `e15c13072f06f0087f36b319f516f9dfc8dd2129f7fcb760c1dd052f87059faf` |
| Package / x64 catalog `Orchard.dll` | `4f400d31ccf01731a50f0f6471558a615cdaf458390ba0209b4ce3b80811127a` |
| ARM64 catalog `Orchard.dll` | `986148035e7a1b1b7dcd2767cde2c4326f0f8fc3be58603225fad763d199888e` |
| `Orchard.Catalog.0.1.0-7df5542-x64.zip` | `be4b51a385412b73c4f9c881c729829bd926cdab8d95a629d9b991776aa4005d` |
| `Orchard.Catalog.0.1.0-7df5542-ARM64.zip` | `caff8f6c2c1e4523d7ed678445542e70ee0eee171ff6e27cfdbebd8cf93c1e58` |

Evidence is under `artifacts/search-input-states-*`,
`search-input-state-core-*`, `search-input-state-filter-*` and
`package-consumer-search-states-*`. Earlier lost-release, default focus, wrong
Dark hover, style registration and resource-scope failures are retained as
diagnostics. Exact raster, broader customization/pointer states, actual native
enabled-motion timelines, Windows scaling/contrast, Narrator, collection
performance and current native ARM64 runtime remain open. No Windows setting
changed, no workflow was dispatched, and push-triggered Actions remain paused.
The full catalog sequence was not rerun. Inventory remains 59 in progress,
53 pending, zero accepted. The goal stays active. The wiki remains unavailable;
see [Search Input](search-input.md) and [Filter chips](filter-chip.md).

## Input fractional alpha and OKLab paint transitions

Source: `1aaf782`. Input retains CSS alpha in `Brush.Opacity`, with opaque RGB,
instead of flattening it to an 8-bit color channel. Its background/border/ring
paint now uses the existing premium CSS clock and premultiplied OKLab color
interpolation. Disabled editor opacity remains a separate presentation value.
Rendering/brush subscriptions stop on unload or template replacement.

Forty independently captured browser transitions in both themes and both motion
preferences verify 360 background/border samples plus 180 opacity samples.
Computed CSS is authoritative for these color samples; canvas readback adds
another rounding step at low alpha. All 117 unit tests and token checks pass.
The focused Input runtime suite passes both themes, including theme and brush
isolation, editing/selection, typography, adornments, pointer ownership, rounded
boundaries, shadows and remounts. The toolbar consumer passes. Both Search Input
viewports pass their native input/UIA/lifecycle suite with zero geometry error,
and now record source RGB, fractional brush alpha and editor opacity directly.
Eight corruptions are rejected, including changed alpha and extra clear clicks.
An optional foreground acquisition click must be on the named capture surface,
have real native receipts and precede the ten required clear clicks.

The 40 whole-frame comparison improves from 101,288 to **83,452 differing
pixels** (17.6% fewer), without masking or resampling. This is partial visual
progress. Source pixel parity remains false; native compositing, glyphs,
focus-ring spread and shadow differences remain open. The full catalog sequence
was not rerun. Enabled-motion numeric samples are not native presentation proof.
The host remained at reduced motion, 100% text/display scale, normal contrast.
No Windows setting changed. Actual enabled-motion presentation, scaling,
contrast, Narrator, collection performance and current native ARM64 runtime
still require evidence. The inventory stays 59 in progress, 53 pending, zero
accepted of 112. Push-triggered Actions remain paused; nothing was dispatched.

Both architecture builds pass, with all 6,057 resolved methods and exposed
interfaces matching. The fresh isolated x64 NuGet consumer passes both themes,
including normal-border and disabled-background fractional-alpha checks. ARM64
consumer validation is build-only. Both temporary consumer caches are removed.
The package contains the same library bytes as the tested x64 catalog. Catalog
ZIPs contain 536 x64 / 532 ARM64 files, all verified entry by entry.

| Artifact | SHA-256 |
| --- | --- |
| `packages-input-paint/Gaulatti.Orchard.0.1.0-input-paint.1aaf782.nupkg` | `a19431c42f6a708bf0cda54a7db16df7499fb6ab14180a0a74ea36798d22ba06` |
| Package / x64 catalog `Orchard.dll` | `bf529286a350a0124114a571ef35ddd99d04c7c64d82262bddba3a11d6d17d66` |
| ARM64 catalog `Orchard.dll` | `4407c6af49e12d32bf590954d7f06df8de32c4f81586923c4d2996e52551f854` |
| `Orchard.Catalog.0.1.0-1aaf782-x64.zip` | `6a6a569e3f4a2ae18e125fd047513268e7c01a5c79862ccc6ef8057b7f8eb67a` |
| `Orchard.Catalog.0.1.0-1aaf782-ARM64.zip` | `ee87b8c92f12811515cb5b1550fbd5e8e3c5a3469c6b1288c68dd902d4e81161` |

Local evidence: `input-alpha-measured-*`, `input-alpha-architecture-audit.json`,
`input-alpha-deliverable-audit.json`, `input-alpha-archives.json`,
`input-alpha-measured-regression`, `input-alpha-toolbar-regression` and
`package-consumer-input-paint-*` under `artifacts`. The original canvas-color
comparison, byte-format hover rejection and unclassified foreground-click
rejection remain diagnostic evidence. The public wiki remains unavailable;
the contract is maintained in [Input](inputs.md) and [Search Input](search-input.md).

## Search Input native editing and consumer-owned clear

Source: `1688886`. `BleeckerSearchInput` composes the retained `BleeckerInput`
editor and a separate native clear action. The frozen browser establishes
consumer ownership, editor-only disabled/read-only behavior and Escape's native
search change event. Internal Windows text binding uses PropertyChanged so
edits reach the consumer immediately. The clear command/event never edits text
or transfers focus automatically. RTL uses physical glyph/button insets and
the editor's requested reading direction.

All 40 source/native cases pass with zero geometry error at 320/640 in Light
and Dark. Runtime evidence includes 56 native key pairs, four Tab transfers,
20 clicks, live two-way binding, selection replacement, Backspace, Escape,
copy/paste, maximum length, command parameters/CanExecute, independent editor
disablement, parent disablement, label updates, template replacement and
unload/remount. Twenty-four external MTA UIA snapshots prove the native
Edit/Value/Text and Button/Invoke patterns, names, values and state. Six
corrupted reports are rejected. The real catalog consumer passes both themes.
All 116 unit tests and token generation checks pass. The shared FilterChip
action's 20-case geometry, 28 property events and 22 retained invocations pass.
Both architecture builds pass; 6,054 resolved methods and exposed interfaces
match. The full catalog sequence was not rerun for this checkpoint.

The fresh x64 NuGet consumer passes both themes. ARM64 consumer verification
is build-only. Both isolated temporary caches are removed. Package and tested
x64 catalog libraries have identical bytes. Versioned archives have 536 x64 /
532 ARM64 entries, all verified individually.

| Artifact | SHA-256 |
| --- | --- |
| `packages-search-input/Gaulatti.Orchard.0.1.0-search-input.1688886.nupkg` | `ee447bfcd7366b4b297bf997bc60adcd0793ab45e9f2808d26a5b6f516a9ef5e` |
| Package / x64 catalog `Orchard.dll` | `50bba0edafbb8ef75c15dcfc43e847b66f8100d49a6d7fdb323df1af519d100e` |
| ARM64 catalog `Orchard.dll` | `98eb7871c2a4cb707aefe54cec6a91ffa0da9582206289b78085fb921e296dcc` |
| `Orchard.Catalog.0.1.0-1688886-x64.zip` | `d460ebe2194cfdd0129229772111a322a1f63c7a3d09666dd22629077825aa97` |
| `Orchard.Catalog.0.1.0-1688886-ARM64.zip` | `4bae7cf3c7890c361e1308a4e603cf4d63ba650515c19ebfed7a1781113f1b5e` |

The corrected strict 40 whole-frame comparison retains 101,288 differing pixels, without
masks or resampling; disabled editor backgrounds account for the largest
differences. Source pixel parity is false. Full state/motion/customization,
broader accessibility/IME/selection, actual Windows scaling/contrast, Narrator,
collection performance and native ARM64 runtime remain open. No Windows
setting changed. Inventory is **59 in progress, 53 pending, zero accepted** of
112. The goal remains active. Push-triggered Actions remain paused and no
workflow was dispatched. The public wiki is still unavailable; documentation
is maintained here. See [Search Input contract](search-input.md).

Browser reference correction: each case now mounts fresh DOM. Reusing a prior
case and pausing CSS transitions at time zero had retained its enabled opacity
and other paint. The original 107,752-pixel comparison remains as a superseded
diagnostic. The imported Input hash and disabled `.6` opacity are now checked.
Existing native frames and input/UIA receipts are re-audited against the new
reference; all geometry/state inputs and library behavior remain unchanged.

## FilterChip property events and retained clients

Source: `b833a05`. A long-lived external UIA client reproduced missing name and
effective enabled-state notifications even though fresh queries returned updated
values. A deliberately raised event on a standard native reference button proved
the client transport before component checks. Native filter actions now notify
clients when the standard button peer's resolved name or enabled state changes.
Peer creation remains lazy for hidden controls. Linked-caption metadata/layout
subscriptions are released on detach, and ordinary explicit names avoid a layout
observer. The first eager-peer startup failure is retained alongside the original
missing-name and missing-enabled event evidence.

Two persistent external clients pass 28 ordered property-event checks (26 from
components and two reference health checks), with maximum delivery 66.6675 ms
under the unchanged two-second deadline. Cases cover default, explicit, bound,
linked-text and linked-metadata names, name priority, Clear all, command state
and inherited group state. Twenty-two invocations use the original retained
Invoke patterns: eight current actions execute exactly once; fourteen disabled,
replaced, removed, empty-group or unloaded actions reject invocation without
consumer writes. New template/remounted actions remain usable. Five corruption
checks reject missing/reused events, switched clients/patterns and stale writes.

The existing 46-tree UIA/28-Tab matrix, 20 typography phases/100 paragraph
measurements, 40 default source cases/72 baselines, all 115 unit tests and token
checks pass. Both architecture publications pass, with 5,977 resolved method
bodies and exposed interfaces equivalent across architectures. A fresh x64 NuGet
consumer passes both themes; its library bytes equal the current x64 catalog.
The ARM64 consumer build passes with runtime unverified. Both temporary package
caches are removed. Every entry in both catalog ZIPs matches its publication.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-filter-chip-events/Gaulatti.Orchard.0.1.0-filter-events.b833a05.nupkg` | `53244793c4e1adb2a7773c5fcf4875d854688f5b00162dd56220a9b23d5b2f66` |
| `Orchard.Catalog.0.1.0-b833a05-x64.zip` | `cb1ebaa8de8721e0aa257938a334f413aee0b8220b3344f1bea8a45386f72d19` |
| `Orchard.Catalog.0.1.0-b833a05-ARM64.zip` | `47dfaecc34818ca1740ceb438b1e22da48497ba8554a0ec00c5d6b54d4c9a502` |

No Windows preference changed. Actual Narrator speech, broader accessibility,
exact raster/motion, scaling/contrast, collection performance and native ARM64
acceptance remain open. Inventory stays **58 in progress, 54 pending, zero
accepted** (51.8% started). Push-triggered Actions remain paused; no hosted run
was dispatched. The public wiki remains unavailable.

## FilterChip external UI Automation and focus

Catalog/test source: `fd3c46b`. The public implementation remains `8fa91ab`.
The new `filter-chip-accessibility` mode queries the public Control view through
a separate Microsoft UI Automation client restricted to the catalog process,
window and fixture ScrollViewer. Standard native before/after buttons provide
reference behavior; target automation IDs belong to the fixture.

Both themes pass 46 external snapshots covering default/live/bound/linked names,
name priority, native Button roles, Invoke availability, command/group disabled
states, localized Clear all, reorder focus/order, template replacement, collection
replacement, empty output and unload/remount. Read-only label/value content stays
exposed without remove buttons. All 28 physical Tab transfers retain exactly one
owned press/release, including four Shift+Tab transfers. Six external invocations
reach original event/command consumers; six disabled invocations are rejected.
No callback automatically removes or clears caller records. Five corruptions are
rejected: stale names, enabled disabled-actions, accepted disabled Invoke, missing
read-only text and lost Tab release ownership.

All 115 unit tests, both architecture publications and the token check pass.
The current public library has the same 5,967 resolved method bodies and exposed
interfaces as the prior typography candidate, including equivalent generated
WinRT provider aliases. Current x64/ARM64 library implementations also match.
The earlier `8fa91ab` NuGet package and independent consumer evidence remain
scoped to that unchanged public implementation. Current ARM64 evidence is a
cross-build; no native ARM64 runtime claim is made.

Light/dark populated-scene captures and post-lifecycle captures are retained.
The early transparent-background fixture is preserved; final captures use
explicit theme backgrounds so the linked caption is visible. Every entry in
both versioned catalog ZIPs matches its publication directory.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `Orchard.Catalog.0.1.0-fd3c46b-x64.zip` | `e6255fc11e7ec52dda3bbec0a59dffb03c6ea59c3fa9684dca7d4ea159b8342c` |
| `Orchard.Catalog.0.1.0-fd3c46b-ARM64.zip` | `f72f831f59b2e27c60f1e44aa78f18ceee20aaa1c403ed259f5ee3f3fcdabdfd` |

This closes the basic external query/invocation and keyboard-order matrix.
Narrator speech, accessibility-event delivery, stale-client invocation,
actual Windows scaling/contrast, broader lifecycle/customization, exact raster,
enabled motion and native ARM64 acceptance remain open. Inventory stays **58
in progress, 54 pending, zero accepted** (51.8% started). Push-triggered Actions
remain paused; no hosted run was dispatched. The public wiki is unavailable.

## FilterChip live font measurements

Source: `8fa91ab`. Switching a mounted Segoe UI filter group to italic reproduced
stale paragraph widths in labels, values and Clear all. Clear all retained
90.1600 DIPs while its rendered italic face required 92.3267 DIPs. Rendered text
inherited the style, but detached width/baseline probes retained the normal face.
The fix observes style/stretch changes and propagates them through chip/group
rendering, intrinsic probes and baseline probes.

The new focused `filter-chip-typography` mode passes 20 phases in light/dark,
including true italics, family/size/tracking changes, RTL, constrained wrapping,
multilingual text, restoration and remounting. All 100 paragraph widths match
independent native measurements exactly. Two physical Space pairs reach the
original consumer actions. Four corruption checks reject wrong styles, stale
widths, missing baselines and incorrect RTL order. The 40 default frozen cases,
72 baselines, source colors, native actions and all 115 unit tests still pass.
The source token check passes. The previous full 1,132-capture result below
belongs to the preceding candidate; current verification targets the affected
typography and default component paths.

The resolved method audit confines implementation changes to four FilterChip
methods; the other 5,963 methods are unchanged or use equivalent generated
WinRT providers. Both architecture libraries have matching managed resources
and all 5,967 resolved method bodies/interfaces match across architectures.
A new isolated x64 NuGet consumer verifies the italic measurements and native
actions in both themes; its library bytes match the x64 catalog. The ARM64
consumer build passes with runtime unverified. Both temporary caches are removed.
Every ZIP entry matches the published directory.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-filter-chip-typography/Gaulatti.Orchard.0.1.0-filter-typography.8fa91ab.nupkg` | `2ce8061e4ba3be27d9c89e4bd58fbbad0ad966193b37ac4bd339f9d0b871697b` |
| `Orchard.Catalog.0.1.0-8fa91ab-x64.zip` | `7dd9e59ef4e0afa75ef63787959f2558ce120489c6b4b198e9f484412d70afe9` |
| `Orchard.Catalog.0.1.0-8fa91ab-ARM64.zip` | `ff1a3179965edd0f10c53c3eb8b33ecab2f95cdbb1c65d011ba97a9cfdedff4d` |

Strict before/after default-frame equality does not pass: 291 pixels differ,
with total absolute RGBA error 1,381 and maximum channel difference three.
Those differences are retained without masks or resampling. Pixel parity,
enabled motion, broader customization, actual Windows text/display scaling,
contrast/Narrator, external UI Automation and native ARM64 acceptance remain
open. No Windows preference changed. Inventory stays **58 in progress,
54 pending, zero accepted** (51.8% started). Push Actions remain paused and no
hosted run was dispatched. The public wiki remains unavailable.

## FilterChip and FilterGroup foundation

Public implementation source: `e0cae0d`. Catalog/input evidence corrections:
`9ffb912`. Push-triggered Actions remain paused; no hosted run was dispatched.

The frozen and native matrices cover 40 cases in light/dark at 320/640 DIPs.
All 72 first-line baselines, chip/text/action/group boxes and recorded normal
text/fill/border colors pass. Maximum geometry error is 0.04425048828125 DIP,
within the existing 0.05-DIP limit; baseline error is below 0.000001 DIP.
The focused native runs retain eight owned Space/Enter pairs and twelve physical
click receipts. Commands, original parameters, caller-owned collections, live
metadata, inherited disabled state, reorder identity/focus, palette mutations,
template replacement, unload/remount, empty groups and synchronous removal
through native UI Automation pass. Five corruption audits reject shifted
baselines, wrong names/opacity, missing lifecycle flags and missing click receipts.

All 115 unit/contract tests and generated-token checks pass. A clean NuGet
consumer passes x64 runtime in both themes with real native keyboard actions;
its ARM64 build passes with runtime explicitly unverified. Both isolated
temporary package caches were removed. The first full catalog run stopped at
the new demo's synchronous removal and missing routed pointer-release receipt.
The demo now queues its consumer mutation after native routing; its focused
catalog mode passes. That failed evidence is retained. The revised complete
catalog sequence passes with 1,132 PNGs, all 1,130 previous capture names retained
and 174 protected glyph hashes unchanged. It verifies 2,010 pointer movements
and presentation gates, 1,756 owned key actions and 76 physical clicks; maximum
pointer readiness is 207.3863 ms under the unchanged two-second limit.

The two architecture libraries have matching managed resources. Resolving all
5,967 method bodies finds 5,965 identical methods and two unchanged Select
constructors whose raw XAML strings differ only by checkout CRLF/LF endings;
the parsed XML is identical. The strict byte/operand comparison remains recorded
as failed. The package contains the same library bytes as the ARM64 catalog;
the x64 catalog uses the equivalent XML-line-ending variant.

All entries in the versioned x64/ARM64 catalog ZIPs are verified against their
published directories. ARM64 artifacts represent cross-compilation only.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-filter-chip/Gaulatti.Orchard.0.1.0-filter-chip.e0cae0d.nupkg` | `9c8f754dc25088aaa056e1656b84a7165bdc7a63d91bca1d61fa35c7bb9cf012` |
| `Orchard.Catalog.0.1.0-9ffb912-x64.zip` | `592bfad382be3c9f0cdf9e180f1d6fc308f95c793d04b5285e1c1b884ad7b56e` |
| `Orchard.Catalog.0.1.0-9ffb912-ARM64.zip` | `5803ff5bb7cef8a52293981be917f0dcfbe9c24fc87a504facd82617da609025` |

The unmodified source/native client comparison still finds 42,338 differing
pixels and total absolute RGBA error 2,987,351. No masks or resampling are used;
source pixel parity remains false. Exact raster, enabled motion, full resource
and typography/RTL customization, text/display scaling, contrast, Narrator,
external UI Automation and native ARM64 acceptance remain open. Inventory is
**58 in progress, 54 pending, zero accepted** of 112: **51.8% started, 0%
accepted**. The public wiki remains unavailable. See [API and acceptance](filter-chip.md).

## Loading Overlay guarded contrast preparation

Catalog/test source: `9b4d7ba84157ef1f6e48014784b931e10b16559e`.
The public library and hosted-only mutation helper are unchanged.

The clean x64 `loading-overlay-contrast-entry` mode passes both ordinary themes,
retaining default palette, blur/shadows and original keyboard focus. Each actual
native label capture contains 217 solid glyph pixels. Three negative audits
reject missing baseline shadows, an incorrect ordinary text color and an
attempt to pass entry-only evidence as actual contrast verification.

The live mode refuses local execution with the existing isolated-runner error
before creating native mutation stages. Win32 state before/after refusal is
identical: flags 126, configured scheme `High Contrast Black`, enabled false.
The request for a temporary local-settings exception is unanswered; the guard
has not been weakened and CI metadata has not been spoofed. The new live gate
is compiled and registered for future native CI runs, but **actual contrast
transition and restoration remain unverified**. Actions are still paused for
pushes, with zero runs for `9b4d7ba`.

Both clean architecture publications, all 113 unit tests and token checks pass.
Across architectures all 5,777 method definitions/IL records and managed
resources match. A resolved-operand comparison with the prior package release
also matches implementations and all 24 exposed-type mappings. No new package
is needed for this catalog-only preparation; independent consumer evidence
remains at the prior two-window checkpoint. The new catalog library hashes are
`8b3068443e933691da5ce690fdc71247da96b3b388682eacc9f3fb6a208f78dc` (x64) and
`d9e1036a3f1498eba5d0d58527ea4ad20c4695636a397bf1476ad55ad176a4e6` (ARM64).

Both versioned catalog archives match every published file. The x64 archive
has entry-only runtime evidence; ARM64 has build-only evidence. Neither archive
claims an actual contrast transition:

| Artifact below `artifacts/` | Entries | SHA-256 |
| --- | --- | --- |
| `Orchard.Catalog.0.1.0-9b4d7ba-x64.zip` | 533 | `3a66407515d25df43c22cfdb4f8936274e47628a49228ef05793cf124af904df` |
| `Orchard.Catalog.0.1.0-9b4d7ba-arm64.zip` | 529 | `96dfc4851cf01acecb0b1f15582d4afbb0edd0bd67b3b10f7cc1bd33f36278c7` |

Evidence is in `artifacts/loading-overlay-contrast-validated-entry`,
`artifacts/loading-overlay-contrast-validated-negative-audit.json`,
`artifacts/loading-overlay-contrast-local-refusal`,
`artifacts/loading-overlay-contrast-local-state-audit.json` and
`artifacts/loading-overlay-contrast-{architecture-il,resolved-method}-audit.json`.
`artifacts/loading-overlay-contrast-validated-archives.json` records artifact scope.
Current catalog runtime evidence is entry-only. Actual contrast, Narrator,
scaling, motion, exact raster and native ARM64 runtime remain open. Inventory
stays 57 in progress, 55 pending, zero accepted. The public wiki remains
unavailable. The next independent inventory entry is FilterChip/FilterGroup;
its frozen source hash is verified, with implementation still pending.

## Previous Loading Overlay two-window checkpoint

Catalog/test source: `c27b2a4b504f3fb807a22348050fe8fc89806a6b`.
The public library source remains unchanged from `2e72e33`.

The clean x64 catalog passes thirteen phases across two real native windows
and distinct XamlRoots. Ten visible phases retain destination viewport coverage,
palette, accessible name and original keyboard focus. Every visible phase
blocks a physical pointer click and permits one owned Space press/release to
activate the underlying action. Transfers cover detach/remount and immediate
reparenting, including identical size/theme/scale inputs. Mutating the old
root's size/theme leaves the destination portal unchanged. Closing the old
secondary window preserves the returned portal; hiding removes the final one.
Light and Dark captures use actual 640×480 and 320×240 clients.

Four negative audits reject stale source portals, wrong destination window
IDs, stale target palettes and missing keyboard releases. The existing library
passes these checks without a product correction. The same NuGet consumer
instance transfers its custom template to a second window and back in both
themes. The fresh x64 consumer passes runtime; ARM64 builds only. Both isolated
caches are removed. The unchanged package SHA-256 is
`7074796d9b7f34f3daab4d7ee1cfea10987c140cafb1c0b00bb44d24de9ceea8`.

Both clean catalog architectures, all 113 unit tests and token checks pass.
Across architectures all 5,777 method definitions/IL records and managed
resources match. Relative to the prior package release, a resolved-operand
comparison verifies the method implementations and all 24 exposed-type
mappings; fourteen generated provider targets use equivalent representative
names. The x64 and ARM64 catalog library hashes are
`058504b2dccfcc7088a899d9fd834bfb3b9ea73c325178102941cd831475cbf4` and
`cb2236f4ddcc5815f1bb4342f826f0ce112b1f13d39c8f7e85f2cb6fe11924a1`.

The versioned archives match every published file: 533 entries for x64 and
529 for ARM64. The x64 archive has the focused two-window runtime evidence;
the ARM64 archive has build-only scope.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `Orchard.Catalog.0.1.0-c27b2a4-x64.zip` | `ec88eacb9bb129e92c70923f8c9c2d7f6de6b66c70b504ab15f55764df616726` |
| `Orchard.Catalog.0.1.0-c27b2a4-arm64.zip` | `197b5ae0e5b0766d910b83ab1799240ca1eb37dfb8c29c817c4c60ba2e40717d` |

Evidence is in `artifacts/loading-overlay-windows-validated-windows`,
`artifacts/loading-overlay-windows-validated-negative-audit.json`,
`artifacts/loading-overlay-windows-validated-archives.json`,
`artifacts/loading-overlay-windows-{architecture-il,resolved-method}-audit.json`
and `artifacts/package-consumer-loading-overlay-windows-{x64,arm64}`.
Current catalog runtime evidence covers the new two-window mode. Earlier full
catalog evidence remains at `2e72e33`; the earlier template checkpoint below
retains its four focused modes. Display/text scale is 1, animations and high
contrast disabled. Same-thread transfer coverage does not establish the
remaining scaling, contrast, Narrator, motion, exact raster or broader lifecycle
matrices. Native ARM64 execution remains unverified. Inventory stays 57 in
progress, 55 pending and zero accepted. Push-triggered Actions remain paused,
with zero runs for `c27b2a4`. The public wiki remains unavailable.

## Previous Loading Overlay caller-template checkpoint

Catalog/test source: `a660a365c435ae30ec0807c3c32582b77385d250`.
The public library source is unchanged from `2e72e33`; this checkpoint adds
caller-template/resource coverage and documentation.

Both clean catalog architectures build. The published x64 catalog passes
four focused modes: ten custom-template/resource phases with ten real pointer
barriers and forty focus observations; 22 external UIA trees; fourteen lifecycle
phases; and 22 observed scroll/transform frames. The custom-template fixture
verifies decoration, instance theme dictionaries, live brush identity, local
property priority/clearing, dictionary replacement after a theme transition,
typography/RTL, names and original-template restoration. Four negative audits
reject stale palette data, missing decoration, missing physical pointer receipts
and retained root tab stops. Both theme captures are retained.

The first stronger fixture read `IsTabStop` immediately after requesting focus
and failed. Its retained trace now distinguishes request from presentation:
the original action receives focus immediately, while the root lease is
released by presentation under the unchanged two-second boundary. The product
code required no correction. See [the template contract](loading-overlay.md).

All 113 unit tests and token checks pass. Fresh independent consumers use the
unchanged `2e72e33` NuGet package: x64 runtime passes both themes, replacing the
template and updating a caller label; ARM64 builds only. Both caches are removed.
The package SHA-256 remains
`7074796d9b7f34f3daab4d7ee1cfea10987c140cafb1c0b00bb44d24de9ceea8`.

Across the two new architectures, all 5,777 method definitions/IL records and
managed resources match. The earlier-release comparison has ten differing
raw records, including two renamed generated provider types. A resolved-operand audit
checks method implementations and all 24 exposed-type mappings; fourteen
provider targets have new representative names but identical interface
implementations. Raw differences remain recorded, rather than reported as
identical bytes. The new x64/ARM64 library hashes are
`e30cf501c063afc2fce9a7fa231c94b918ffc50e7c51ead6bb59cc2accb495b4` and
`52206e612998dad1a49622e5a2ae6ea872ec61b2d69a4546293801c864bc30a1`.

The new catalog archives match every published file. The x64 archive has
533 entries and the four focused runtime checks above; ARM64 has 529 entries
and build-only scope:

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `Orchard.Catalog.0.1.0-a660a36-x64.zip` | `e85bd479aec229d5ecf3ee8d60d24f0ac935c4bc79712d78e468b4048b85ec6c` |
| `Orchard.Catalog.0.1.0-a660a36-arm64.zip` | `63caa7585d8cd28bbade02d2e42cae1b017a81b73382ab4e8255751326706896` |

Evidence is in `artifacts/loading-overlay-template-validated-{template,accessibility,lifecycle,scroll}`,
`artifacts/loading-overlay-template-{architecture-il,resolved-method,raw-token}-audit.json`,
`artifacts/loading-overlay-template-validated-{negative-audit,archives}.json` and
`artifacts/package-consumer-loading-overlay-template-{x64,arm64}`.
The synchronous fixture failure remains in `artifacts/loading-overlay-template-v2`.
Full-catalog evidence remains at `2e72e33` below; current catalog validation
covers the four named modes. Display/text scale remains 1, animations and high
contrast disabled. Broader templates, cross-window transfer, exact raster,
motion, scaling, contrast, Narrator and native ARM64 runtime remain open.
Actions remain paused for pushes, with zero runs for `a660a36`. Inventory stays
57 in progress, 55 pending, zero accepted. The public wiki remains unavailable.

## Previous Loading Overlay accessible-name checkpoint

Current library/catalog source: `2e72e334c5b321f795a248ba3aef1dc3563233da`.
The spinner now honors caller `AutomationProperties.Name` and `LabeledBy`
before the visible label, following the existing Button contract. The retained
pre-fix regression fails at the explicit name. Live localized bindings, linked
captions, explicit-name priority, omitted labels and template replacement pass
in both themes. Twenty visible states preserve the original action's focus;
22 separate-process UI Automation trees verify named, nonfocusable,
indeterminate progress and hidden-peer removal. Three negative audits reject
stale external names, lost focus and retained hidden peers. This verifies
automation exposure; Narrator speech remains unverified.

The clean full x64 run passes **1,130 captures in 456.908882 seconds**, preserving
all prior captures and **174 protected glyph hashes**. It retains 2,006 pointer
moves/presentation gates, 1,754 owned key pairs and 72 physical clicks; maximum
gate time is 218.8598 ms under the unchanged two-second deadline. The first
full run stopped on an Avatar presentation timeout after 43.7977438 seconds.
Its evidence is preserved; the subsequent pass does not establish its cause.

All **113 unit tests**, token checks and clean x64/ARM64 publications pass.
The 60-case overlay matrix retains 42 baselines at the original 0.05-DIP bound;
14 lifecycle phases and 24 observed scroll/transform frames pass. Exact raster
comparison remains open: all 60 frames differ, with 3,041,291 differing pixels
and 38,220,303 total RGBA channel error, without masks, resampling or tolerance.
The independent x64 NuGet consumer passes both themes, including live bound
and linked names with an omitted visible label. ARM64 consumption is build-only;
both isolated caches are removed. All 5,777 managed method definitions,
including 30 overlay methods, and managed resources match across architectures.

The package, x64 catalog and both consumers contain identical portable library
bytes. The ARM64 catalog matches its separate architecture build; its library
SHA-256 is `cff23d1b4aa0094b1c4e1804b804ec6d8af16fbfd50a373d1b899d8b45da6026`.
Both versioned archives match every published file:

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-loading-overlay-accessibility-release/Gaulatti.Orchard.0.1.0.nupkg` | `7074796d9b7f34f3daab4d7ee1cfea10987c140cafb1c0b00bb44d24de9ceea8` |
| Portable `Orchard.dll` | `48592cbecb2c324cc0874d880b97068989b65919021a93e745f0d25af2251d98` |
| `Orchard.Catalog.0.1.0-2e72e33-x64.zip` (533 entries) | `9b996c6f77beeec5c0dc42b8e5a488fb2682c2b17c53298f2583b19db1bc6c17` |
| `Orchard.Catalog.0.1.0-2e72e33-arm64.zip` (529 entries, build-only) | `afeb7525f1a21ab2c24bc8fe87d9c07c8d2af385c24c0a79e30b459a69f9116b` |

Evidence is in `artifacts/loading-overlay-accessibility-release-{matrix,lifecycle,scroll,accessibility}`,
`artifacts/smoke-loading-overlay-accessibility-release-full-v2`,
`artifacts/loading-overlay-accessibility-release-{full-audit,full-v2-timing,pixel-audit,negative-audit,binary-correspondence,archives}.json`,
and `artifacts/loading-overlay-accessibility-v1` (pre-fix failure).
The earlier failed full run is `artifacts/smoke-loading-overlay-accessibility-release-full`.
Validation used x64 Windows at display/text scale 1, animations disabled and
high contrast disabled. Native ARM64 execution, Narrator, scaling, contrast,
complete motion/raster/lifecycle acceptance and remaining inventory stay open.
Push-triggered Actions remain paused; GitHub reports zero runs for `2e72e33`.
Inventory stays **57 in progress, 55 pending, zero accepted**. The public wiki
remains unavailable; documentation is maintained in-repository.

## Previous Loading Overlay root and viewport checkpoint

Current library/catalog source: `5550af051b3a0e93d0eca804a0774f6d76838f10`.
The Page-root correction preserves the original effective `IsTabStop` value
when `ClearValue` exposes Control's true default. The same native regression
rejects the original `d4067ea` release DLL and passes with the corrected library.
The native focus trace retains the original/after-Tab property values.

Fourteen published lifecycle phases verify open Light/Dark/Light changes,
original mutable brushes, asymmetric geometry, actual resizing, reparenting,
template replacement, detach/remount, stacked callers and root focus restoration.
The scroll/transform probe verifies **22 observed frames**, including the first
frame after ancestor render motion. The portal follows presentation while
visible and commits changed anchors before rendering. Three lifecycle negative
audits and four source negative audits reject invalid evidence. Complete custom
templates, cross-window transfers and remaining acceptance still require work.

The clean full x64 suite passes **1,130 captures in 453.355999 seconds**, preserving
every prior capture and **174 protected glyph hashes**. Its audit verifies 2,006
pointer moves/presentation gates, 1,754 owned key pairs and 72 actual clicks.
Maximum gate time is 222.9579 ms under the same two-second deadline. Timeline,
Stepper and DataList baseline gates retain their original bounds.

All **113 unit tests**, token checks and clean x64/ARM64 publications pass. The
published source matrix retains **60 cases and 42 baselines**. The independent
x64 NuGet consumer passes both themes and native actions; ARM64 consumption
is build-only. Both isolated caches are removed. Exact source/native frames
still differ in all 60 cases: **3,041,325 pixels and 38,288,089 RGBA error**,
including underlying native/browser button differences, without masks,
resampling or pixel tolerances. Component acceptance remains open.

Evidence is in `artifacts/loading-overlay-motion-release-matrix`,
`artifacts/loading-overlay-motion-release-{lifecycle,scroll}`,
`artifacts/smoke-loading-overlay-motion-release-full`,
`artifacts/loading-overlay-motion-release-{full-audit,full-timing,pixel-audit,archives,binary-correspondence}.json`
and `artifacts/loading-overlay-old-library-negative-audit.json`.

- Portable library SHA-256: `d62d9ca4256cc2e25d7e8dbdd8f43de2d0da21bf1788665eff934313d62d912c`.
- NuGet SHA-256: `5e2f63a39f0504c5f3eb7b4af611968cc2f6e9c7247d663e356b8b40e201fcb0`.
- x64 ZIP: 532 entries, SHA-256 `a6dc2185ce4bdf432661d2961a3031a6cbc55ac672b0fe1b08ab43191932c5ba`.
- ARM64 ZIP: 528 entries, SHA-256 `2c9f5106c946ecb02e7083c8d334f81d6beafe4bbf21aa3941ef724829dd1dad`.

The package, x64 catalog and both consumers contain identical library bytes.
The separately built ARM64 catalog library has SHA-256
`3d6011bdd22a268dd0f5704e2b2f9a275f5e3f3266e620963c5c0e3e65244c44` and
matches its clean architecture build. All 5,774 managed method signatures/IL
records, including 27 overlay methods, and resources match across architectures.
Both ZIPs match every published file. Native ARM64 execution remains unverified.

The earlier full run at `104203e` failed the Dark catalog viewport check after
346.405805 seconds; its evidence is preserved. Separate motion probes reproduce
persistent and first-frame offsets; the published correction passes all frames
and the full sequence. No bounds or deadlines were relaxed.
Push-triggered Actions remain paused; GitHub reports **zero runs for `5550af0`**.
Inventory remains **57 in progress, 55 pending, zero accepted**. Full visuals,
motion, accessibility/scaling, the remaining components and compositions remain
open. The public wiki remains unavailable; the goal stays active.

## Previous Loading Overlay initial contract checkpoint

Current library/catalog source: `d4067eaedf0ab6c9c0889c07c8c9c2389df6f44f`.
The clean published full x64 sequence passes in **463.9042732 seconds** with
**1,130 captures**. It retains all **1,128 previous capture names** and
**174 protected glyph hashes**, adding two full-window overlay previews.
The audit verifies 2,006 real pointer moves/presentation gates, 1,754 owned
key pairs and 72 physical clicks. Maximum gate time is 188.2566 ms under
the unchanged two-second deadline; the full watchdog remains eight minutes.

The published overlay matrix passes **60 cases** and **42 first-line baselines**
at 1024×900, 640×480 and 320×240 in both themes. Maximum geometry error is
0.018189 DIP and baseline error is 0.000031 DIP at the existing 0.05-DIP bound.
Original keyboard focus, real Space/Enter, pointer blocking, Escape, pointer
focus clearing, subsequent Tab/hide behavior and restoration of the original
root value/live binding pass. Four negative audits reject invalid geometry,
focus and physical input evidence. See [the contract](loading-overlay.md).

Both clean architecture builds, **113 unit tests** and token generation pass.
The fresh independent x64 package consumer passes both themes and the loading
portal's public API and original native action. Its build and runtime reports
are retained separately. ARM64 consumption is build-only; both isolated caches
are removed. Existing Timeline, Stepper and DataList baseline gates still pass.
Exact unmasked overlay frames remain open: **60/60 differ**, with **3,041,308
pixels** and **38,254,621 total RGBA error**. The native underlying buttons also
differ from the browser fixture. No masks, tolerances or resampling are used.

Evidence is in `artifacts/loading-overlay-release-matrix/{1024,640,320}`,
`artifacts/smoke-loading-overlay-release-full`,
`artifacts/loading-overlay-release-{full-audit,full-timing,pixel-audit,negative-audit,archives,binary-correspondence}.json`
and `artifacts/package-consumer-loading-overlay-{x64,arm64}`.
Package, x64 catalog and both consumers contain the same portable library:

- Library SHA-256: `3956a5ed62b21585301360f48b431276e1524824ea83ab6e9f0c341ee53fb797`.
- Package SHA-256: `c2400030ddf0c08706fc6010083eff0913df6c75f9cba1ee7772fabb1aea6791`.
- x64 ZIP: 532 entries, SHA-256 `85dfc8e7efb4b412c4b2789e0b05733276c708202550be5524485f810ff3f98c`.
- ARM64 ZIP: 528 entries, SHA-256 `87798b8f1c45bad67c518d9faad06aa3045b551a89473631aa59b93b857155af`.

Both archives match every published file. The separate ARM64 catalog library
matches its architecture build, with SHA-256
`22ba5fa72d26c98033e715b79d7d8809d06c10e2698b3765ab9b4bfd91f9c2b1`.
Its PE bytes differ from the portable package; all 5,773 method signatures and
IL records, including 26 overlay methods, and managed resources match.

Full visual/motion, live/lifecycle/override, scaling, contrast, Narrator and
native ARM64 acceptance remain open. Inventory is **57 in progress, 55 pending,
zero accepted**: 50.9% started, 0% accepted. Push-triggered Actions remain paused;
the remote workflow permits only PR/manual runs. The GitHub API run count could
not be checked because of rate limiting. No hosted run was dispatched or claimed.
The public wiki remains unavailable. The goal stays active.

## Previous Timeline initial contract checkpoint

Current library/catalog source: `1f85bbc263c112bd26a5ddbbba080eb46be01a7a`.
The clean published full x64 sequence passes in **447.8767607 seconds**, retaining
all **1,104 previous capture names**, **174 protected glyph hashes** and adding
**24 Timeline captures**, for **1,128** total. Its audit verifies 2,006 pointer
moves/presentation gates, 1,752 owned key pairs and 72 physical clicks. Maximum
gate: 231.9245 ms under the unchanged two-second deadline. The full watchdog
remains eight minutes. Collection performance remains a separate requirement.

The published Timeline matrix passes **72 cases** and **534 library-owned
first-line baselines** across both themes and widths 639, 640 and 1024.
Maximum geometry error is 0.021729 DIP and baseline error is 0.000021 DIP at
the original 0.05-DIP bound. All 66 nonempty screen crops match their retained
client pixels exactly. Four negative audits reject fractional coordinates,
an unrestored parent, a shifted baseline and wrong client pixels. Native
pointer/Space/Enter actions, keyed updates, default/explicit status, original
icon content/templates, signed zero, mutable shadow brushes, template/lifecycle
and actual breakpoint resizing pass. See [the source contract](timeline.md).

Both clean architecture builds/publications, **112 unit tests** and token
generation pass. A fresh independent x64 package consumer passes both themes
and native Timeline actions. The ARM64 consumer builds only. Both isolated
caches are removed. All 324 Stepper and 132 wide DataList baselines retain
their existing bounds. Exact Timeline images remain open: **66/72 differ**,
with **438,044 pixels** and **34,943,600 absolute RGBA error**, using zero
tolerance, no masks and no resampling. The published replay reproduces the
prototype totals exactly. Complete focus/content/override/lifecycle matrices,
scaling, contrast, Narrator, native ARM64, the remaining components and both
compositions remain open. Inventory: **56 in progress, 56 pending, zero accepted**.

Evidence is in `artifacts/timeline-release-matrix/{1024,639,640}`,
`artifacts/smoke-timeline-release-full`,
`artifacts/timeline-release-{1024,639,640}-audit.json`,
`artifacts/timeline-release-{full-audit,full-source-audit,full-timing,pixel-audit,negative-audit,archives}.json`
and `artifacts/package-consumer-timeline-{x64,arm64}`.
`artifacts/timeline-release-binary-correspondence.json` verifies identical
library bytes in the package, x64 catalog and both package consumers:

- Library SHA-256: `ee9a23888ae6c350bfb5143891f7248a38c1fe5c990492f8cfe354d1e25d3e75`.
- Package SHA-256: `0408790175fb80d6b77d9951342a161439de40a08fdee8e8d5cf0b32424f6505`.
- x64 ZIP: 532 entries, SHA-256 `e1aceb990e62f0fc632767958c841c2912b7f01eddddb082d6f132717d0faf21`.
- ARM64 ZIP: 528 entries, SHA-256 `f0b34a96be9a82d37fea469cffe7adc01d71781f98d49c68733d1e8fe4c77433`.

Both ZIPs match every published entry. The separately built ARM64 library
matches its clean architecture build, with SHA-256
`af8c50e335635328a2a3a3789ec15799c6ef0a495cc040c37a00f49426d74616`.
Its PE bytes differ from the portable package. The architecture audit verifies
identical signatures and IL instruction records for all 5,739 methods, including
93 Timeline methods, plus identical managed resources. Native ARM64 runtime
remains unverified. Push-triggered Actions remain paused; no hosted result
is claimed. The public wiki remains unavailable. The goal stays active.

## Previous DataList first-line baseline checkpoint

Current library/catalog source: `82d7d34137d2c76080a33c870e7e2a8a5b30c69d`.
All **396 scalar first-line baselines** pass across **72 cases**, both themes
and viewport widths 639, 640 and 1024. Maximum error is 0.0000124 DIP at the
unchanged 0.05-DIP geometry bound. The original native implementation fails
all 396 checks, by up to 2.068001 DIPs. Source ranges and Canvas font metrics
are measured without changing the frozen component. The correction rounds
font ascent/descent before placing the line baseline, preserving compact
rows' half-DIP origins. Six consumer-button baselines remain diagnostic;
the library preserves consumer typography and layout.

The clean published full x64 sequence passes in **435.2680508 seconds**,
retaining all **1,104 capture names** and **174 protected glyph hashes**.
Its audit verifies 1,982 pointer moves/presentation gates, 1,748 owned key
pairs and 70 physical clicks. Maximum gate: 210.8032 ms under the unchanged
two-second deadline. All 324 Stepper baselines remain within 0.0000153 DIP.
The full watchdog remains eight minutes; no collection-performance claim
is made. The first postprocessing attempt incorrectly required a component
field in the full-suite report. The audit now requires explicit
`--full-suite` mode and validates that report's existing schema; five negative
checks retain the original baseline failures and reject mismatched modes.
The existing native evidence passes corrected postprocessing at the same bounds.

Both clean architecture builds/publications, **109 unit tests** and token
generation pass. A fresh independent x64 NuGet consumer passes both themes
and native actions. The ARM64 consumer builds only. Both isolated caches
are removed. Exact images remain open: **66/72 differ**, with 2,496,401
differing pixels and 76,288,484 absolute RGBA error, using zero tolerance,
no masks and no resampling. The clean replay differs from the prototype
by one unit of total RGBA error. See [the control contract](data-list.md)
for the remaining raster, override, scaling, accessibility and platform gaps.
Inventory remains **55 in progress, 57 pending, zero accepted**.

Evidence is in `artifacts/data-list-baseline-release-matrix/{1024,639,640}`,
`artifacts/smoke-data-list-baseline-release-full`,
`artifacts/data-list-baseline-release-{1024,639,640}-audit.json`,
`artifacts/data-list-baseline-release-{full-audit,full-timing,pixel-audit,archives}.json`,
`artifacts/data-list-baseline-negative-audit.json` and
`artifacts/package-consumer-data-list-baseline-{x64,arm64}`.
`artifacts/data-list-baseline-release-binary-correspondence.json` verifies
identical library bytes in the package, x64 catalog and both package consumers:

- Library SHA-256: `a4742298ae522f5791a52c4df09a37e55e4fea6b02c064e4b5469f41830229c4`.
- Package SHA-256: `d2c12fbea8d9c70c748dfca528e1bb2c70e5c2cdc309c19546e12210a039f765`.
- x64 ZIP: 532 entries, SHA-256 `7b67a24771df8d0af0c7a0f09630cd525faf1b243501cdd343f154dc65066113`.
- ARM64 ZIP: 528 entries, SHA-256 `88166f99cd35bdc7c83985675ad7d92613c61fee50dfa6f31faa39c465a7898b`.

The separately built ARM64 catalog library has SHA-256
`372a911a86b5c710e21b1d0c75a20e10232d15dd3edee7399d495089e4e8c034`
and matches its clean architecture build. Its PE bytes differ from the
portable package. `artifacts/data-list-baseline-architecture-il-audit.json`
verifies identical method signatures and IL instruction records for all
5,616 methods, including 84 DataList methods, plus identical managed resources.
This is build evidence; native ARM64 runtime remains unverified. Both ZIPs
match every published entry. Push-triggered Actions remain paused; no hosted
result is claimed. The public wiki remains unavailable. The goal stays active.

## Previous chart focus ownership repair checkpoint

Current library/catalog source: `4d14c0e6be01d12cbd2b48ae565c7924f5f37631`.
The clean published full x64 sequence passes in **434.4379521 seconds**, retaining
all **1,104 capture names** and **174 protected glyph hashes**. Its independent
audit verifies 1,982 pointer moves/presentation gates, 1,748 owned key pairs and
70 physical clicks. Maximum gate: 152.1393 ms under the unchanged two-second
deadline. The eight-minute full watchdog is unchanged; no collection-performance
or speedup claim is made.

The shared renderer reconciles lifecycle/focus notifications with the current
`FocusManager.GetFocusedElement(XamlRoot)` owner. The published native probe
passes 24 Bar/Sankey cases across both themes, both event orders, genuine blur,
stale blur and 24 owned Right pairs. It preserves the original 12 eager cases'
24 first/next-record failures. The property-based intermediate at `d6808e1`
still failed a fresh consumer and one of three cold runs. Holding consumer IL
unchanged and replacing only the library passes three cold prototype runs.
Final fresh-package verification passes **two separate x64 restores/builds/native
runs in both themes**, without temporary per-event focus instrumentation.
The targeted x64 regression is resolved; all old failures remain preserved.

Both clean architecture builds/publications, 108 unit tests and token generation
pass. ARM64 package validation is build-only. All isolated consumer caches are
removed. All 324 Stepper baselines remain within 0.0000153 DIP. Source data,
geometry, navigation rules and deadlines are unchanged. Native ARM64 runtime,
visuals, scaling, contrast, Narrator, remaining components and compositions stay
open. Inventory: **55 in progress, 57 pending, zero accepted**.

Evidence is in `artifacts/smoke-chart-owner-release`,
`artifacts/smoke-chart-owner-release-full`,
`artifacts/chart-owner-release-{audit,full-audit,full-timing,archives}.json`,
`artifacts/package-consumer-chart-owner-x64-{1,2}` and
`artifacts/package-consumer-chart-owner-arm64`.
`artifacts/chart-owner-release-binary-correspondence.json` verifies the same
library in the package, x64 catalog and all three consumers:

- Library SHA-256: `690098b411e9f0f297dcc17a727615ddf3156f96bf08a9c6a28ee3d9ac90f0de`.
- Package SHA-256: `b2a9d52b2ffbc9e988694c0f4155ef59890fa8012b17a0329a99433ef9bb8efd`.
- x64 ZIP: 532 entries, SHA-256 `1d8719b95dcc558b95df2ad1248b8aa8ac8df466263bd16a8eecdfe5a0be715d`.
- ARM64 ZIP: 528 entries, SHA-256 `6257a44d7c8184b88d42ddc365d1d2c76c6893fd350d11da8cdcc202f257f71b`.

Both ZIPs match every published entry. x64 has full runtime evidence; ARM64 has
build evidence. Push-triggered Actions remain paused, with no hosted result
claimed. The public wiki remains unavailable. The goal remains active.

## Previous DataList release checkpoint

Current full-suite/package library and catalog source:
`a7dc3caf6b4fcc1fc5a50add2c87017046e971b5`.
The clean published x64 run passes in **437.5147483 seconds**, with **1,104
captures**, all 1,080 prior names and all **174 protected glyph hashes**.
Its independent audit verifies 1,982 pointer moves/presentation gates, 1,748
owned key pairs and 70 physical clicks. The maximum pointer gate is 169.8662 ms
under the unchanged two-second deadline. The eight-minute full watchdog is
unchanged; no collection-performance or speedup claim is made.

All 72 published DataList cases pass the existing 0.05-DIP geometry bound
across both themes and widths 639, 640 and 1024. Consumer actions, templates,
live binding, keyed lifecycle and real window resizing pass. Exact images
remain open: 66/72 differ without masks, tolerance or resampling, totaling
2,536,268 pixels and 114,743,118 absolute RGBA error. Six empty frames match.
See [the API and acceptance gaps](data-list.md). Inventory: **55 in progress,
57 pending and zero accepted**.

Sequential clean x64/ARM64 builds and publications pass without warnings;
108 unit tests and token generation pass. The initial concurrent build failed
because ARM64 restore replaced the shared x64 assets target; that failure is
retained in `artifacts/data-list-clean-x64-build.log`. All 324 Stepper baselines
remain within 0.0000153 DIP. Final visuals, contrast, Narrator, scaling, native
ARM64 runtime, remaining components and both compositions remain open.

The independent x64 consumer passes both themes at
`artifacts/package-consumer-data-list-x64-v3`. Its first two attempts fail the
existing Bar Chart initial-inspection assertion after DataList's own action
checks. The second failure records keyboard focus, intact records, empty help
text and no pointer events. Added lifecycle/focus observations change timing
and the third attempt passes; **no control fix is claimed**. Preserve both
failures and investigate this intermittent boundary before acceptance. The
ARM64 consumer builds only. All isolated restore caches are removed.

The library in the package, x64 catalog and passing/build-only consumers matches
byte for byte. `artifacts/data-list-release-binary-correspondence.json` records:

- Library SHA-256: `c62a181ae273d1059068970b533f872ed92b70ed8d77de39a535d04ebe751b1d`.
- Package SHA-256: `d2e6657b50ba39964890526582446f1b6839459f93cf2a675eb98ca4ac1c635d`.
- x64 catalog ZIP: 532 files, SHA-256 `92fc4e4afb044fb6bed9d33f148dcb8ce26b616536ac178ee31395cb30386742`.
- ARM64 catalog ZIP: 528 files, SHA-256 `ce1bf56ed414895cb574ba24864fa00560d8934ba3866163e7f8fbad1d300f4a`.

Both ZIPs match every published entry. The x64 publication has full-run
evidence; ARM64 has build evidence. Artifacts include
`artifacts/smoke-data-list-release-full`,
`artifacts/data-list-release-{full-audit,full-timing,archives,pixel-audit}.json`,
`artifacts/data-list-release-matrix/{1024,639,640}` and
`artifacts/data-list-release-stepper-typography-audit.json`. Push-triggered
Actions remain paused; no new hosted result is claimed. The public wiki
remains unavailable. The goal remains active.

## Previous Stepper baseline checkpoint

Full-suite and packaged library/catalog source: `b59cef474ad95c94841cbfccd77059e85dcc06ad`.
The clean published x64 full run passes in **406.1895514 seconds** with all
**1,080 captures**, every prior filename and all **174 protected glyph hashes**.
Its independent audits verify 1,980 pointer moves/presentation gates, 1,744
owned key pairs and 68 physical clicks, with zero failures. The maximum pointer
gate is 205.6012 ms under the unchanged two-second deadline. The full watchdog
remains eight minutes; no throughput or collection-performance claim is made.

The native Stepper now matches all **324 first-line text baselines** across
44 pinned browser cases, including ordinals, both orientations, long/narrow
labels and RTL. Maximum baseline error is 0.0000153 DIP under the existing
0.05-DIP geometry bound. Label/description baselines move up one pixel from
the previous implementation; ordinal line height is explicitly 20px. The
source capture derives baselines from actual text ranges and measured font
bounding boxes without altering the DOM. Empty text and check glyphs have no
fabricated baseline. The prior nearest-pixel paint placement remains recorded
in `artifacts/smoke-stepper-typography-before`.

The unmasked zero-tolerance image audit improves from 165,563 to **143,878
differing pixels**, and from 27,086,663 to **11,026,803 absolute RGBA error**.
It still differs in 42/44 cases. Font/shape rasterization, alpha-paint details,
scaling, contrast, Narrator, full override/reversal matrices and native ARM64
runtime remain open. The inventory stays **54 in progress, 58 pending,
0 accepted**, and the goal remains active.

Clean x64 and ARM64 solution builds have zero warnings/errors. All **105 unit
tests** and token generation pass. Published enabled/reduced Stepper motion
passes 42 transitions, 776 sampled frames and four owned Space pairs, and
restores the original Windows preference. The maximum sRGB trajectory error
is 0.927 channel values under the unchanged 1.5 quantization bound.

Evidence:

- `artifacts/smoke-stepper-baseline-full`
- `artifacts/stepper-baseline-{full-audit,typography-audit,pixel-audit,full-timing}.json`
- `artifacts/smoke-stepper-baseline-motion` and `stepper-baseline-motion-audit.json`
- `artifacts/tests-stepper-baseline-release`
- `artifacts/catalog-stepper-baseline-{x64,arm64}`
- `artifacts/packages-stepper-baseline/Gaulatti.Orchard.0.1.0.nupkg`
- `artifacts/Orchard.Catalog.0.1.0-b59cef4-{x64,arm64}.zip`

Push-triggered Actions remain paused. No new hosted CI/runtime result is
claimed. The public wiki remains unavailable; this evidence is maintained
in-repository.

The fresh independent NuGet consumer passes its x64 native action, fonts,
resources and light/dark checks. The independent ARM64 consumer builds;
ARM64 runtime remains unverified. Both isolated restore caches are removed.
The package library and both consumers match the published x64 `Orchard.dll`
byte for byte. `artifacts/stepper-baseline-binary-correspondence.json` records
library SHA-256 `55ce036ea2d226d849cd8b808723723289e9d16281ba6459d44389af753d1a5a`
and package SHA-256 `3730495664c364cfc5a2f3e7418cf38b4c04ee285bc1655dbe131cee1260340f`.

Both versioned catalog ZIPs match every published entry byte for byte:

- x64: 532 files, SHA-256 `df119e829fcbdb8e45e2355e29181511d0102e510c1c4b7eabb5f49bdca0f33e`.
- ARM64: 528 files, SHA-256 `326d7a59e7c3542af0aa01ae90a69c3380a5ceac00da31fa0347fe46e427ab9d`.

The archive audit is `artifacts/stepper-baseline-archives.json`. The x64
archive contains the runtime-verified publication; the ARM64 archive has build
evidence only. These hashes identify artifacts and do not establish component
visual acceptance.

The subsequent capture-only Stepper raster diagnostic verifies all 44 cases
with **zero screen-versus-bitmap differences**. Every visible frame stays
identical before/after bitmap rendering, and the original catalog parent is
restored. It rules out the bitmap path as the cause of this host's observed
browser differences. The diagnostic's browser comparison still differs in
42 cases (143,883 pixels, 11,020,905 absolute RGBA error). The rehosted bitmap
differs from the prior full-run bitmap by 226 pixels across 26 cases; these
captures remain separate. No production control, reference image, comparison
tolerance or mask changes. Artifacts are `artifacts/smoke-stepper-raster-final`,
`artifacts/stepper-raster-final-audit.json` and
`artifacts/stepper-raster-negative-audit.json`. All four negative audit cases,
105 unit tests, token generation and x64/ARM64 diagnostic builds pass. The
complete suite/package evidence above remains at its stated source revision;
no new full-suite or ARM64 runtime result is claimed for the diagnostic.

## Previous source-motion checkpoint

Current library/catalog source: `05c5b2db48c8b02af2ddc5fb548cde73a8e65a55`.
The published full x64 run passes in **404.2454108 seconds**, with all
**1,080 captures**, all prior filenames and **174 protected glyph hashes**. Independent audits verify
1,980 pointer moves and stable presentation gates, 1,744 owned key pairs and
68 physical clicks, with zero failed gates. The maximum pointer gate is
197.5917 ms under the unchanged two-second deadline. The full-suite watchdog
remains eight minutes. No throughput or collection-performance claim is made.

Clean x64 and ARM64 builds have zero warnings/errors; **104 unit tests** and
token generation pass. The published Stepper motion check exercises the real
Windows client-area animation setting in enabled/reduced modes and restores
the original preference. All 42 status/theme transitions, 768 sampled native
frames and four owned Space down/up pairs pass. Scale changes immediately;
colors follow the source's 190 ms premium sRGB transition. The trajectory
audit's maximum error is 0.936 channel values under its fixed 1.5 quantization
bound. Callback removal preserves running color motion. The source capture,
native fixture and independent verifier retain separate evidence.

The failed full run at `ba1434b` remains in
`artifacts/smoke-stepper-motion-full`. Its trace records BottomTip's delayed
hover timer taking ownership while the programmatic check inspected LongTip.
The fixture now establishes a genuine neutral pointer receipt before those
checks. The final run passes all 28 Tooltip gates with the original deadline
and stability requirement; production Tooltip behavior is unchanged.

The zero-tolerance Stepper image audit still differs in **42/44 cases**
(165,563 differing pixels). Exact raster, complete reversal/override matrices,
scaling, contrast, Narrator and native ARM64 runtime remain open. The inventory
stays **54 in progress, 58 pending, 0 accepted**; the goal remains active.

Current local evidence:

- `artifacts/smoke-stepper-motion-final` and `stepper-motion-final-audit.json`
- `artifacts/smoke-stepper-motion-final-full`
- `artifacts/stepper-motion-final-{full-audit,pixel-audit,full-timing}.json`
- `artifacts/tests-stepper-motion-release`
- `artifacts/catalog-stepper-motion-final-{x64,arm64}`
- `artifacts/packages-stepper-motion-final/Gaulatti.Orchard.0.1.0.nupkg`
- `artifacts/Orchard.Catalog.0.1.0-05c5b2d-{x64,arm64}.zip`

The fresh independent x64 NuGet consumer passes both themes and native input.
Its library DLL matches the validated catalog and package. ZIP audits compare
every published entry byte-for-byte. The independent ARM64 consumer compiles
and contains the same packaged DLL; its runtime remains unverified. Both
isolated consumer caches are removed. Consumer evidence is in
`artifacts/package-consumer-stepper-motion-{x64,arm64}` and byte correspondence
is in `artifacts/stepper-motion-final-binary-correspondence.json`.
Current artifact SHA-256 values:

| Artifact | SHA-256 |
| --- | --- |
| NuGet `0.1.0` | `5c258447279360969a7b26587a011c5b23e413fa3712ee29ad69185d868a413f` |
| Packaged `Orchard.dll` | `8f701635a67093e586b62805887ea4043efecc71f5564c9ea912ab1429981dd8` |
| x64 catalog ZIP | `630d5ee9c44439a790503072a1dd9e4b2a5f71c49819493dcc6d34c8bc9cc19e` |
| ARM64 catalog ZIP (build only) | `49e9fb3aaaa8f72bb53650a68ed0bd5f5219507f5aa0ae2ba34b7960d20f7404` |

Push-triggered Actions remain paused. The motion gate is registered for future
manual/pull-request runs; no hosted runtime result is claimed. The public wiki
is unavailable, so documentation remains in-repository.

## Previous initial Stepper checkpoint

Library/catalog source: `14bc3ef42549af8c7d61bf407cf0f9c8b2c55b1f`.
Independent consumer harness: `5a72f66` (test assertions only).

The published x64 full sequence passes in **404.7333773 seconds** with
**1,080 captures**. All prior 1,034 names remain present; the added 46 are
44 Stepper source cases and two native held-Space captures. All **174 protected
Sunburst/Radial glyph hashes remain unchanged**. Independent input audits verify
1,978 pointer moves and stable presentation gates, 1,744 owned key pairs and
68 physical clicks, with zero failures. The maximum pointer gate is 179.9942 ms
under the unchanged two-second deadline. The eight-minute full-suite and
six-minute focused watchdogs are unchanged. This is functional verification,
with no throughput improvement or collection-performance acceptance claim.

Clean x64 and ARM64 builds pass with zero warnings/errors. All **102 unit tests**
and token generation pass. The independent x64 NuGet consumer passes both
themes, including native Space press/release, original Stepper data ownership,
live metadata and disabled UIA state. The package DLL matches the validated
x64 catalog and consumer bytes. Both catalog ZIPs match every published file.
The independent ARM64 consumer compiles; both isolated consumer caches are
removed. Native ARM64 execution remains unverified on this x64 machine.

The new navigation entry exposed the Alert test's fixed 64-key traversal bound.
The corrected check derives its finite bound from mounted native tab stops.
Both themes actually require 65 keys to return to the close action, within the
measured bound of 68. Forward entry, exit, full-cycle return, reverse exit and
forward return assertions remain intact. The earlier failed full and focused
runs are retained at `artifacts/smoke-stepper-full` and
`artifacts/smoke-stepper-alerts`.

Stepper remains **in progress**. All 44 normal source cases pass geometry,
brush, disabled-state and accessible metadata checks, and both themes pass
native input, command, keyed-model, template and unload/remount checks. The
unmasked zero-tolerance screenshot audit still differs in **42 of 44 cases**
(165,569 differing pixels); both empty frames match. Exact pixels, enabled
motion, contrast, scaling, Narrator and native ARM64 acceptance remain open.
The inventory is **54 in progress, 58 pending, 0 accepted**.

Local evidence:

- `artifacts/smoke-stepper-release-full`
- `artifacts/stepper-release-{full-audit,full-pixel-audit,full-timing}.json`
- `artifacts/stepper-release-{binary-correspondence,archives}.json`
- `artifacts/tests-stepper-release`
- `artifacts/package-consumer-stepper-release-x64-v2`
- `artifacts/package-consumer-stepper-release-arm64` (build only)
- `artifacts/packages-stepper-release/Gaulatti.Orchard.0.1.0.nupkg`
- `artifacts/Orchard.Catalog.0.1.0-14bc3ef-{x64,arm64}.zip`

NuGet SHA-256:
`3b5ccd8e634818b2b3c9be023cd495fb242e82c723f129bcedccb654c0fd74ec`.
x64 ZIP: `46dd162111f54a6698ee6f9007550da6a95b206ddd053cce75cf24542bdb0660`.
ARM64 ZIP: `39457da82b9a04412d35dba5051581e0bf74a0759ba7d1158fc41c6396c6b6a2`.

Actions remain paused for pushes. The Stepper gates are registered for future
manual or pull-request validation; no new hosted runtime evidence is implied.
The public wiki remains unavailable; this checkpoint is documented in-repository.

## Previous capture-persistence checkpoint

Validated source: `3591548efd593215cc54cd9c32272bd8491cb14e`.
The full published x64 sequence passes in **374.8848365 seconds** with all
**1,034 captures**. The full-suite watchdog is eight minutes; focused modes
retain six minutes. The additional 240 required chart pointer actions and
their presentation gates account for a larger workload. This is functional
verification, with no throughput-improvement claim. The original collection
performance requirements remain open and separate.

## Runtime evidence

Independent audits verify:

- All prior 1,034 capture names, with none missing.
- All 174 protected Sunburst/Radial glyph hashes unchanged.
- 1,972 pointer moves and presentation gates, zero failures and at least two
  genuine stable Rendering transitions. Maximum gate time is 181.8119 ms
  under the original two-second deadline. Eighty-eight stationary moves
  retain a preceding exact receipt; all 240 newly added chart moves require
  their own fresh genuine receipt.
- 1,738 owned key pairs and 62 physical clicks, with zero failures.
- All eighteen final presentation gates, twenty-eight Tooltip gates and
  118 Toolbar source cases.
- Both themes' Filter, Column Toggle and Pagination lifecycle checks,
  including controlled ownership, stale/disabled rejection and zero final
  observers. The keyed live-options checks remain verified.
- The separately audited 84 circular and 36 Sparkline cases retain every
  original keyboard expectation and their added source-defined hover/exit
  states.

Evidence is below `artifacts/`: `smoke-capture-boundary-validated-full`,
`capture-boundary-{prior-1034,full-preservation,full-native-input,full-aggregate,full-pie,full-sparkline}-audit.json`
and `capture-boundary-validated-full-timing.json`. The timing journal and its
summary retain the complete process rather than inferring a pass from image
count alone.

The earlier six-minute timeouts and intermittent Tooltip/Input failures
remain retained. The current full run passes those same assertions; it does
not establish the causes of the earlier intermittent failures. Input now
writes failure-only brush/state diagnostics. No expected color, native
cursor, source reference, comparison mask, per-state deadline or input
assertion was relaxed.

## Builds, consumer and image persistence

Clean x64 and ARM64 solution builds have zero warnings/errors; all 100 unit
tests and token generation pass. The fresh independent x64 NuGet consumer
passes both themes. Its Sparkline evidence verifies empty-data inspection
cleanup, restored original data and one owned Right down/up pair. The fresh
ARM64 consumer compiles, with runtime explicitly unverified. Both generated
consumer caches are removed, and the cleanup regression also proves cleanup
after invalid-project and restore failures.

The published `brush-alpha` mode verifies four frozen persistence cases:
transparent, odd dimensions, every alpha/channel pair and catalog size.
An independent decoder confirms exact PNG bytes and RGBA pixels, immediate
file completion and propagated write failure. That regression is registered
in both native CI jobs. Actions remain paused for pushes; no new hosted
runtime evidence is implied.

The bitmap-reuse, worker-encoding and direct-DIB experiments showed no
established performance benefit and were removed. Their measurements and
patches remain available; see [runtime diagnostics](catalog-readiness.md).
The public library and dependency source is unchanged from candidate
`ab2af53`, whose eleven focused modes and independent audits remain prior
evidence. The current full sequence, package consumer and persistence checks
above use the new source and artifacts.

## Deliverable bytes

All 532 published x64 files match the clean build. The catalog and both
consumers contain the same packaged library. ZIP audits verify every entry;
the ARM64 ZIP records build-only scope.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-capture-boundary-validated/Gaulatti.Orchard.0.1.0.nupkg` | `47f43908a40954bfcc78ac64e1a66f7e6d291be9c71ffcfa2933a19d1118c70e` |
| Packaged `Orchard.dll` | `a2f077e4f3e3aaeff0cc4930105585619dd7da9f014d835c06d36fbeaf23037d` |
| `Orchard.Catalog.0.1.0-3591548-x64.zip` | `228167c5eb4878cfaaf7104fa910c165922e7a8b7f6217f68fde2ac693d68f0f` |
| `Orchard.Catalog.0.1.0-3591548-ARM64.zip` | `8bf35058181fa7c7eebfa9a6bb0056af3b470bfbba2f44805b9b32488ba19a20` |

## Remaining acceptance

This previous checkpoint had 53 in progress, 59 pending and zero accepted out
of 112. Exact source raster/routes, complete motion and override matrices,
scaling, Windows contrast/Narrator, collection performance and current native
ARM64 runtime remain separate required work. The goal stays active. The
public wiki remains unavailable, so this checkpoint is documented here.
