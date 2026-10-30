# Stepper

`BleeckerStepper` is accepted for the x64 milestone against frozen Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, `src/components/stepper.tsx`
(SHA-256 `a0cea6e7cc8fcc541d0089682d64ed8589fc4983a482095785477cb01d646c29`).

The current foreground catalog passes all 44 Light/Dark source cases, both
orientations and all four statuses. Native pointer, Tab/Space/Enter, focus,
disabled rejection and lifecycle checks pass in both themes. A fractional
catalog-window position previously displaced every text baseline by 0.64 DIP;
rounding now uses Stepper-local coordinates. The independent package consumer
passes eight native 150% Light/Dark horizontal, narrow, vertical and RTL layouts
without clipping. With Windows animations already disabled, status colors
settle immediately and remain static. The x64 package consumer compiles and
runs; ARM64 is build-only and runtime-unverified. The historical parity
diagnostics below do not add gates beyond the [five-check milestone](acceptance-contract.md).

The source contract includes both orientations and all four statuses
in light and dark themes, including empty content, constrained widths and RTL.
The active ID derives statuses only where a step has no explicit status.
An unknown or empty active ID leaves those steps pending. Activation must return
the original consumer step without changing the active ID or any step metadata.
Missing activation handlers disable input without dimming; an explicitly
disabled step also dims its circle. Native Tab, Space, Enter, pointer and UIA
activation must respect the same ownership and disabled checks.

Live collection/model/command changes, keyed replacement, reorder, removal,
template replacement and unload/reload must update the actual UI without stale
activation or retained subscriptions. The current milestone verifies native
150% scaling, reduced motion, accessible names/status/position, an independent
package consumer and the existing catalog regression sequence. Broader
historical contrast and platform parity are outside this milestone. The
[current library scope](library-scope.md) excludes Narrator and OS contrast
campaigns.

The source uses physical left offsets for connectors even in RTL. Its vertical
circles can shrink below 32 pixels under severe width constraints. These are
source contracts, not opportunities for a different responsive layout.

Use `BleeckerStepper.Steps`, an `ObservableCollection<BleeckerStep>`, with stable
IDs. Each original step has `Id`, `Label`, nullable `Description`, nullable
`Status` and `IsDisabled`. Set `ActiveStep` to derive unspecified statuses and
`Orientation` to `Horizontal` or `Vertical`. Subscribe to `StepInvoked` or supply
`StepCommand`; the command receives the original step as its parameter and its
`CanExecute` controls availability. Update consumer state in the callback when
navigation should advance. Supplying both invokes the command and then the event.
Neither mechanism changes `ActiveStep` automatically.

Windows buttons announce the consumer label, description, status and ordinal,
including completed steps whose source glyph has no textual button name. This
is an intentional accessible-name adaptation for native assistive technology.
The control preserves a consumer-provided group automation name.

`scripts/reference/capture-stepper.cjs` renders the actual frozen React component
with pinned dependencies. Its output records complete unmasked screenshots,
DOM geometry, computed styles and trusted pointer/keyboard events. Browser
evidence alone does not establish native parity.

Current runtime regression uses a 0.05-DIP geometry bound against the captured
DOM. Browser canvas conversion of CSS OKLab can differ by up to two channel
values; brush checks allow that conversion difference and one alpha value.
These checks are not a claim of exact screenshot equality. Source-derived visual
review of the differing states, complete reversal/override motion matrices,
contrast-theme appearance, text/display scaling, native ARM64 runtime and final
visual acceptance remain open. The strict zero-tolerance pixel failures remain
diagnostics under the [acceptance contract](acceptance-contract.md), not a
byte-identical public-component delivery gate.

The x64 focused replay currently passes all 44 normal cases and both themes'
physical pointer, Space/Enter/Tab, held-Space scale, UIA invocation, command
replacement, model replacement/reorder/removal, template replacement and
unload/remount checks. x64 and ARM64 solution builds pass. The zero-tolerance
unmasked screenshot audit currently differs in 42 of 44 cases; the two empty
frames match exactly. At that historical stage the component was **in progress**. Local evidence is in
`artifacts/smoke-stepper-release-full` and
`artifacts/stepper-release-full-pixel-audit.json`. The full catalog run retains
all 1,034 earlier captures plus 46 Stepper captures; see
[the current checkpoint](native-validation-checkpoint.md).

The [exact published v7 NuGet-consumer check](evidence/theme-toggle-v7-lifecycle-v1/checkpoint.json)
also runs Stepper in its [full Light/Dark consumer result](evidence/theme-toggle-v7-lifecycle-v1/full-consumer-result.json).
It verifies two 32-DIP native actions, derived status/name/ordinal metadata,
Space-release invocation of the original step without changing controlled
state, live status/name updates and disabled UIA state. This closes a focused
current-package input path, while the full independent-consumer visual/override
matrix and native ARM64 runtime remain open.

## Press and color motion

The frozen browser capture in `tests/Reference/StepperMotion.json` distinguishes
the individual CSS `scale` property from `transform`. Pointer/Space press sets
scale to 0.94 immediately, and release restores 1 immediately, in both motion
modes. The source transitions `background-color` and `color` for 190 ms using
`cubic-bezier(0.22,1,0.36,1)`. Its legacy RGB endpoints interpolate in encoded
sRGB. Stepper uses a separate sRGB interpolator; controls with explicit OKLab
color-mix rules retain their existing interpolator.

Live status, theme and palette changes animate when Windows enables animation.
Changing callback/command availability preserves an unchanged running color
transition. Native reduced motion snaps synchronously. The source's 0.01 ms
reduced-motion floor is complete before a displayed frame; the regression
compares reduced-mode presented samples after that floor, while retaining the
browser's time-zero record.

The historical `scripts/smoke-stepper-motion.ps1` campaign scoped the real
Windows client-area animation preference, ran `stepper-motion` in enabled and
reduced modes, and restored the original preference in its outer `finally`.
The current goal does not rerun an OS accessibility-setting campaign. The script
uses the documented
[SystemParametersInfo client-area animation flags](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa).
The fixture checks 42 source status/theme transitions, intermediate native
frames, the sRGB trajectory, callback removal and immediate press/release scale
at the native `IsPressed` boundary. `scripts/verify-stepper-motion.mjs` separately
audits those reports, all four owned Space down/up pairs and preference
restoration. This motion coverage does not establish exact screenshot parity,
all reversal/override combinations or final desktop acceptance.

The initial x64 motion run passes all 42 transitions and 767 sampled frames,
with four owned Space pairs and verified preference restoration. The independent
trajectory audit's maximum error is 0.893 channel values under the fixed 1.5
quantization bound. The prior OKLab implementation fails the same audit. Source
timeline regressions now pass 104 unit tests. Evidence is retained under
`artifacts/smoke-stepper-motion-v2` and `artifacts/stepper-motion-v2-audit.json`.

The final published replay at `05c5b2d` passes 768 native frames with maximum
trajectory error 0.936. The complete 1,080-capture catalog regression passes in
404.2454108 seconds. Current evidence is `artifacts/smoke-stepper-motion-final`,
`artifacts/smoke-stepper-motion-final-full` and
`artifacts/stepper-motion-final-audit.json`; artifact hashes and scope are in
the current checkpoint linked above.

## Text baselines

Stepper floors its measured line baseline onto the display grid. The previous
nearest-pixel rounding placed the pinned label and description glyphs one pixel
below the browser text. Ordinals now use an explicit 20px line box and align
their measured semibold baseline in the 32px circle. This changes text painting;
the source row, label, connector and circle geometry stays the same.

`scripts/reference/capture-stepper-typography.cjs` measures the first glyph's DOM
range and actual Canvas font bounding-box ascent. It verifies that the two font
line-box measurements agree, uses the original text nodes and leaves the DOM
unchanged. The frozen `StepperTypography.json` covers all 44 source cases, with
324 nonempty label, description and ordinal baselines. Empty text and completed
glyphs have no fabricated baseline. The native fixture and
`scripts/verify-stepper-typography.mjs` independently compare the baseline and
line height at the original 0.05-DIP geometry bound.

The focused x64 replay passes all 324 baselines with a maximum error of
0.000016 DIP, and 105 unit tests pass. The unmasked zero-tolerance image audit
improves from 165,563 to 143,875 differing pixels and from 27,086,663 to
11,020,196 absolute RGBA error. It still differs in 42/44 cases. Font/shape
rasterization, the remaining alpha-paint differences and broader desktop
acceptance remain open; no pixel tolerance or comparison mask was added.

The clean published full run at `b59cef4` passes all 1,080 captures in
406.1895514 seconds, preserving every prior filename and 174 protected glyph
hashes. The same 324 baseline assertions pass in the full sequence. Its
zero-tolerance image audit records 143,878 differing pixels and 11,026,803
absolute RGBA error; this is a separate capture from the focused result above.
Published enabled/reduced motion remains verified. Current evidence is under
`artifacts/smoke-stepper-baseline-full`, with the independent baseline audit at
`artifacts/stepper-baseline-typography-audit.json`.

## Screen and bitmap diagnostic

`scripts/smoke-catalog.ps1 -Component stepper-raster` temporarily hosts the
existing 1024×745 reference frame fully on screen at integer device coordinates.
It captures the catalog client before and after `RenderTargetBitmap` renders
that same frame, for all 44 source cases. Display and text scale must both be 1.
The original control, font, brushes, reference geometry and source images are
unchanged, and the frame returns to its catalog parent in `finally`.

`scripts/compare-stepper-raster.mjs --native <diagnostic> --source <browser>
--baseline <prior-native> --output <audit.json>` requires every on-screen frame
to remain identical across the bitmap capture. It compares all frame pixels
between the screen, bitmap, original browser and prior native captures, with
zero tolerance, no masks and no resampling. Extracting the exact frame bounds
from the client is the only crop. This separate diagnostic reports capture
evidence and does not grant visual or component acceptance.

The final x64 diagnostic at `artifacts/smoke-stepper-raster-final` verifies all
44 screen/bitmap pairs with **zero differing pixels**, and both screen captures
of each case remain identical. The catalog parent and child index are restored.
This rules out the bitmap capture path as the cause of the observed source
differences on this host. Browser comparison still differs in 42 cases, with
143,883 differing pixels and 11,020,905 absolute RGBA error. The newly hosted
bitmap differs from the prior full-run bitmap by 226 pixels across 26 cases;
the two runs remain separate evidence, with neither result substituted for
the other. See `artifacts/stepper-raster-final-audit.json`.

`scripts/test-stepper-raster-audit.mjs` uses the same arguments to test rejection
of a changed on-screen frame, fractional capture coordinates, duplicate cases
and an unrestored catalog parent. All four negative cases are rejected at
`artifacts/stepper-raster-negative-audit.json`; temporary test metadata is removed.
The public library is unchanged by this diagnostic. Both architecture builds,
all 105 unit tests and token generation pass; native ARM64 runtime remains open.
