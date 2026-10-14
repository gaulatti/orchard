# Native Windows runtime evidence

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
