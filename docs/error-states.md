# Error State

Error State is accepted for the 2026-10-29 x64 component milestone. The
published `.10` catalog passes seven specimens in Light and Dark, including
physical pointer and Space retry, focus, disabled command state, and live
content changes. The matching isolated package consumer compiles and runs, and
its native 150% XAML-scale fixture passes all fourteen specimen/theme cases
without visible content escaping its control. Error State has no animation;
the non-loading retry button has no active spinner, and the catalog passed with
Windows animations already disabled. See the five pass/fail records in
[the inventory](parity-inventory.json). ARM64 runtime remains unverified.

Historical parity investigations below remain useful diagnostics. Their raster,
OS contrast, and ARM64 runtime gaps are not gates for this x64 milestone.

Run [38547741448](https://github.com/gaulatti/orchard/actions/runs/38547741448)
at `55d9391` passes both standalone Error State suites, both complete catalogs,
both existing real-contrast suites and both independent consumers. Its new
Error State contrast checks fail before enabling contrast: the title's
reported native bounds begin at x281 while its centered painted text occupies
x483–653. The strict crop therefore contains no title glyphs. The verified x64
archive `11885827358` has SHA-256
`b373d683e9adc6f47f3c9aee762c2249e49aa16f06090586425698262f32c2b9`.

The template now centers the title element itself, aligning its native pose
with the painted text rather than a wider parent layout slot. Both normal
themes also verify the title's centered native bounds and establish title and
description ink with the unchanged strict composed-screen helper. The focused
x64 suite passes these checks and all fourteen frozen layout specimens at
`artifacts/smoke-error-state-title-bounds`. No crop offsets, glyph masks, pixel
tolerances or contrast assertions are changed. Actual contrast recovery still
requires a new hosted run.

The same run's ARM64 after-chart check fails earlier in Line Chart when a
foreign `wsl` ConsoleWindowClass window takes foreground; its pointer remains
on the owned catalog. The verified ARM64 archive `11886286177` has SHA-256
`6dcba94b70559f5494070f873aa314d72569353f0617664a02044a7c39bcd4dc`.
All chart keys now use the existing guarded hosted foreground recovery and
exact routed down/up acknowledgement. The helper still refuses application
popups, unowned pointer locations and recovery while a pointer button is held.

The updated x64/ARM64 builds and all 38 regressions pass at
`artifacts/tests-error-title-bounds`. The final local cross-fixture suite passes
both themes at `artifacts/smoke-error-title-chart-final`, with all fourteen
layout specimens, strict normal-mode title/description ink and 29 acknowledged
chart keys per theme. The fresh package is produced in
`artifacts/packages-error-title-bounds`, but its local consumer restore fails
when the drive fills during NuGet memory-mapped extraction. It is not a
passing consumer result. Its exact new cache and failure record are preserved;
the storage-only verifier wrapper changes directory compression, not package,
restore, licensing, runtime or architecture checks. Fresh hosted consumption
is still required for this candidate. The historical real-contrast run retains
its failure, but OS contrast campaigns are excluded from current acceptance.

The complete local run at `artifacts/smoke-error-title-full` passes the new
Error State checks in Light, then fails the unchanged Input reading-direction
boundary with the same 317px opaque span for a 320px Small input. The earlier
focused Input pass and hosted full-catalog passes do not close this local
sequence failure. It remains separate from the title-pose correction.

`BleeckerErrorState` implements the pinned Bleecker
[`ErrorState`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/error-state.tsx)
contract as a native Windows error summary. It supplies no data-loading,
endpoint selection, retry scheduling or inferred recovery policy.

```xml
<orchard:BleeckerErrorState Title="Archive unavailable"
    Description="The archive could not be loaded."
    RetryLabel="Reload archive" RetryCommand="{Binding ReloadArchive}"
    RetryCommandParameter="{Binding CurrentArchive}" />
```

Title defaults to “Something went wrong”, Description to “We couldn't load
this data. Please try again.” and RetryLabel to “Try again”. Empty/null strings
remain empty rather than being replaced with defaults. An empty description
is removed from layout. Icon accepts consumer-owned content; null selects the
default 28px Lucide circle-alert within the 48px icon box. The consumer retains
its icon object and normal native content semantics.

RetryCommand maps the source's optional callback to a normal Windows command.
The retry action exists only when that command is supplied. It uses the existing
Small/Secondary BleeckerButton and native Button activation, accessible name,
CommandParameter and CanExecute. The Retry event observes that activation;
subscribing does not add an action. Consumers own all work performed by the
command. IsEnabled disables the native action through normal inheritance.

The outer summary is a non-focusable UI Automation Group named from Title,
with Description as help text. Explicit AutomationProperties.Name, LabeledBy
and HelpText take precedence. The optional retry retains its own native button
and Invoke provider. The component does not announce an unsolicited live alert.

The recipe uses the existing terracotta and text theme resources, 12px card
radius, 32px horizontal/56px vertical padding, a 20px main gap and 4px text gap.
Title uses 16px semibold Encode Sans, 24px line height and -0.02em tracking;
description uses 14px Encode Sans, 20px line height and a 320px maximum width.
Light/Dark surface alpha is .035/.07; icon alpha is .07/.15; border alpha is
.25/.15. Contrast mode maps backgrounds to Window, foreground/borders to
WindowText, and removes the low alpha. Native button colors remain authoritative
for the retry label and glyph. No independent Error State animation is added.

The catalog registers default, retry, custom icon/empty description, constrained
long text, RTL, empty title and empty text specimens. The frozen browser record
`samples/Orchard.Catalog/Reference/ErrorStateLayout.json` captures all fourteen
theme/specimen combinations from the unchanged source and Tailwind 4.2.2,
React 19.2.4 and Lucide 0.563.0. The native fixture compares block geometry
directly with that record at display/text scale 1. Source capture and rendered
screenshots are retained in `artifacts/error-state-browser`; the source SHA-256
is `b1c0e51f0d36ef3e29443393b983f8bf05caaf47d1d666fcf6202092902e033d`.
`scripts/derive-error-state-reference.mjs` validates the frozen capture before
refreshing the tracked record.

Run `scripts/smoke-catalog.ps1 -Component error-state` for content, native pointer
and Space retry, exact command/event counts and parameter identity, live
CanExecute, disabled state, icon/text changes and template replacement. The
complete catalog also exercises this fixture. The independent package consumer
checks the public summary and command/content boundary after a fresh isolated
restore. Both hosted native jobs additionally run `-Component error-state-contrast`:
the existing guarded helper changes real Windows contrast only on isolated
GitHub-hosted Windows, compares painted background and established composed
title/description ink, then restores the original mode and palette. Local
invocations refuse before changing preferences.

Earlier parity work tracked glyph/icon raster and broader template edge cases.
Those are not gates for the current x64 milestone; native 150% no-clipping now
passes in both themes. Narrator and OS contrast campaigns are excluded by the
[current library scope](library-scope.md). There is no backend, metrics or production publishing surface.
The wiki remote remains unavailable; component documentation is kept here.

The focused native x64 suite passes both themes at
`artifacts/smoke-error-state-source-final`, including all fourteen source block
geometries, native pointer/Space command delivery, parameter identity, command
rejection/restoration, live icon/description changes and template replacement.
The old empty-title template fails the frozen 206px source block at 230px in
`artifacts/smoke-error-state-source-boundary`. The title-empty visual state
removes that phantom line. Live null-title diagnostics record Height=0, a 206px
summary and an empty accessible name; TextBlock.ActualHeight retains its old
24px metrics, so the regression checks actual summary layout and restoration.
The retry glyph and label bind to the mounted native content presenter's
foreground, including native state/theme updates, rather than a text getter
whose inherited paint does not represent the actual presentation brush.

The new actual-contrast entry point refuses locally at
`artifacts/smoke-error-state-contrast-local-refusal`, reporting contrast false.
No local Windows animation, scale or contrast preference is changed. Both
final solution builds are clean and all 38 regressions pass at
`artifacts/tests-error-state`. The fresh isolated x64 package consumer passes
both themes at `artifacts/package-consumer-error-state`, including native Invoke
delivery, exact parameter identity, live CanExecute, consumer icon ownership
and removal/restoration of optional content. Its package SHA-256 is
`03597a05acb331b1faeb449bfdb94b1616695b581ad285fee50fb852fc4ad331`.
Restore/build and native execution are separated; the isolated restored package
hash is rechecked before execution. Actual hosted contrast, native ARM64 and
the final complete-catalog verification remain pending for this candidate.

The initial complete catalog failed the first Light retry after Line Chart.
`-Component error-state-after-chart` reproduces that boundary independently at
`artifacts/smoke-error-state-after-chart-raw`. Command/event counts remain zero;
the native button is loaded/enabled and its command/parameter are unchanged.
The native point and foreground are owned, and XAML hit testing finds the
button's label/content/action. A held press reaches InputPointerSource at the
expected coordinates, but no routed XAML press/release reaches the new page
and the button never enters IsPressed. The GUI-thread trace records no capture,
menu loop, Alt or Enter key. The additional handled/moved trace shows native
Handled=true and routed movement over the retry label, with no managed input
exception. Counts, pressed-state assertions and deadlines are retained.

Mount-only and before-interaction diagnostic sequences pass both themes. The
failure requires the final Line Chart interaction sequence. Its empty-data
keys could satisfy an already-present expected state immediately after
SendInput, before routed key delivery completed. NativeInputSession.KeyAsync
now requires exactly one routed down/up on the focused chart before the next
operation. With that boundary, the complete Line Chart then Error State
sequence passes both themes at `artifacts/smoke-error-state-chart-key-release`.
Temporary diagnostic skip switches are removed. The focused cross-fixture
regression is also registered for both hosted architectures. This establishes
local integration recovery; final complete-catalog and hosted evidence remain
required.

`error-state-retry-pointer-{scheme}.json` records routed/raw events, command
counts, XAML/native point ownership, GUI-thread routing and child-window
geometry without window titles or content bodies. The catalog-only routing
observer uses [GetGUIThreadInfo](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getguithreadinfo)
and [InputPointerSource.PointerPressed](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.input.inputpointersource.pointerpressed?view=windows-app-sdk-2.0)
without changing input routing. Any held button is released by the existing
input-session cleanup on failure.

The final complete x64 catalog at
`artifacts/smoke-error-state-key-release-full` passes Line Chart and Error State
in Light, including thirteen acknowledged chart keys with exactly one routed
down/up each and retained focus. It then fails the existing Input
reading-direction raster boundary: a 317px opaque span for a 320px input.
The focused Input suite passes both themes at
`artifacts/smoke-input-after-key-release`. The full-suite raster failure remains
open; neither focused pass nor the earlier Error State result replaces it.

Final x64/ARM64 solution builds pass without warnings, all 38 regressions pass
at `artifacts/tests-error-state-key-release`, and token generation matches the
pinned source. The final binaries pass the complete Line Chart then Error
State sequence in both themes at `artifacts/smoke-error-state-key-release-final`.
Each theme records thirteen native keys, exactly one down/up per key, with no
lost target. The new component remains in progress; these checks do not
establish complete visual/accessibility acceptance or a green full suite.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).
