# Input editing preview

At `17b0e1b`, run `38597371241` passes the entire x64 job and ARM64 cross-build.
Native ARM64 passes Section Header, contrast and the independent consumer, but
focused Input fails Small/invalid/top-left 0,0 outside the rounded shape:
Windows displays AppStarting 65561 instead of Arrow 65539. Orchard's hover and
ownership are false, its publisher is inactive, and its retained restoration
shape is Arrow. This differs from a pending UniversalNo publication. The full
ARM64 catalog separately loses ownership between the restored-owner check and
the fixture's next forced transaction. Archive `11910643289` is verified with
SHA-256 `9945d1d0e4085c061eead3b062dbe1e19586727689f3a0c49c5015ac64f0a7f8`.
AppStarting is not accepted as Arrow, and both failures remain open pending
their respective runtime corrections. Automatic push validation is temporarily
disabled at the user's request; later pushes do not imply hosted acceptance.

The publisher separates SDK publication into rendering phases. After the
queued owner/epoch check, it retains the exact producer selection for two
rendering callbacks, then publishes UniversalNo and waits for an actual visible
native acknowledgement. This tests whether the prior same-dispatch transition
was coalesced; the underlying SDK cause is not established by a passing run.
Owner release cancels the producer phase, invalidates its epoch and detaches
the frame observer. A native regression releases the real owner in that phase
and requires one cancellation, zero No deliveries and actual Arrow restoration.
No pointer coordinates, expected handles, tolerances or state deadlines change.

Run `38593563145` at `9303b06` passes the entire native ARM64 job with actual
animations enabled. The verified full-catalog reports contain both producer
phase cancellations (one cancellation, zero deliveries, real Arrow) and both
forced settlements (one delivery, one acknowledgement, two producer callbacks,
visible UniversalNo and no pending publication). Its fresh ARM64 consumer passes
and removes its own cache. Archive `11910030536` SHA-256:
`a68c8aed669b393f1948f0e16bb2eba839acb48961e31cd5623627078d9ad00e`.
The x64 job passes focused checks, contrast and its consumer but fails the full
catalog's contextual-help/Breadcrumb pointer check. Its verified diagnostics
show that the target moved 42px after the native move selected its coordinates.
Overall CI is still red; this run alone does not establish complete acceptance.

Local x64 focused Input and the full Light/Dark catalog pass at
`artifacts/smoke-cursor-producer-frame-input` and
`artifacts/smoke-cursor-producer-frame-full`. In each theme the new producer-phase
cancellation records exactly one cancellation, zero deliveries, no pending
observer and actual Arrow. The following forced transaction records exactly
one delivery, one acknowledgement, two producer callbacks and visible
UniversalNo with no pending publication. Both architecture solution builds and
all 42 regression tests pass. A fresh independent x64 package consumer passes
with package SHA-256
`80055ea6ef93f79d80bce5a3a75736283fa4843e22c6824b8f41125683c15b32`;
its own GUID cache was removed after verification. The later hosted ARM64
verification is recorded above.

At `38426be`, run `38588481432` passes the full x64 job and both focused Input
matrices. Full ARM64 Dark still fails Checkbox hit-testing restoration round 6:
the SDK retains UniversalNo, the visual/ownership state matches, but Windows
displays Arrow for 97 callbacks. The publisher leaves that transaction pending
and does not falsely acknowledge it. Its verified archive SHA-256 is
`acce72fba3a6e9a45e25f5dd2644a9803eb2eb4617671a2d7c014bfea2036cfb`.
The remaining case requires further SDK publication investigation; accepting
Arrow, changing pointer coordinates or relaxing its deadline is not verified.

Native ARM64 at `d68a732`, run `38584983610`, passes both complete Input
matrices, both native acknowledgement snapshots, queued cancellation, full
catalog lifecycle checks and the independent consumer. Its actual preference
is animations enabled. The verified archive SHA-256 is
`77cf99ad89ee36a8db75b97406ac6893c247f773a288617333cf8782ac5d2b5b`.
Each forced transaction has one delivery/acknowledgement, visible handle 65559
and no pending observer; the Dark trace handles two SDK producer replacements.
This is runtime evidence for the cursor correction, not acceptance of the
remaining Input visual, accessibility and scaling matrix.

The publisher now acknowledges a deferred selection only after two or more
rendering callbacks and a successful `GetCursorInfo` read showing a visible
UniversalNo cursor. Every settlement observation revalidates the current owner
and publication epoch first. Until Windows displays that cursor, the same
publication stays pending instead of being reset to its retained producer.
If the SDK replaces its selection during that pending transaction, the
publisher retains the new producer value and reasserts its owned override.
Waiting alone failed a local Small invalid top-right 3,0 case after 62 frames;
the SDK had already selected Arrow while the transaction was pending.
The native snapshot at acknowledgement is recorded and required by the
stationary Checkbox regression. Source ownership, exact producer restoration,
queued cancellation and the existing displayed-cursor deadlines are preserved.
This is a candidate correction for the ARM64 Arrow failure at `faab41e`;
hosted evidence is still required.
The revised local focused matrix passes all 192 Light/Dark samples at
`artifacts/smoke-cursor-live-producer-input`; its Dark trace records one handled
producer replacement. The earlier waiting-only candidate remains rejected at
`artifacts/smoke-cursor-acknowledgement-input`. No displayed-cursor expectation
or per-state deadline was changed.
The complete local Light/Dark catalog also passes at
`artifacts/smoke-runtime-settlement-full`, including both actual acknowledgement
snapshots, one completed/acknowledged forced transaction per theme, no pending
observer, and the unchanged queued cancellation, scrolling and lifecycle checks.
Its shared publisher records handled producer replacements in both themes.

Run [38567209211](https://github.com/gaulatti/orchard/actions/runs/38567209211)
at `64f4f25` passes all required jobs, both complete native catalogs, both real
contrast suites and all independent consumers. The verified native ARM64
archive has SHA-256
`f8ba882068d43a9d56740c71a3c0cf3998565ddb643abd3b7cdd08e0108fec51`.
Both themes retain 96 passing Input samples. Each controlled Checkbox
publication completes and acknowledges exactly once, records two rendering
ticks, and displays UniversalNo with no pending observer. Each ownership
cancellation increments only its cancellation count and displays Arrow.
This establishes hosted evidence for the frame-settlement correction; it does
not accept the remaining Input accessibility, scaling, motion and visual matrix.

Cursor diagnostics now retain bounded native snapshots immediately after the
initial blocked assignment, intermediate producer assignment, deferred blocked
assignment and producer restoration. Each records whether `GetCursorInfo`
succeeded, its displayed handle/flags/point, the caller's `GetCursor` handle,
native thread ID and monotonic timestamp. Window-point diagnostics also record
the owning window threads. These observations distinguish an immediately
ineffective SDK assignment from a later replacement; they do not supply an
expected handle or alter publication, ownership, cancellation or acceptance.

The local 1024x745 x64 matrix passes all 192 Light/Dark samples in
`artifacts/smoke-cursor-publication-observation-input-sized`. In 39 samples,
Windows still displays Arrow immediately after the deferred blocked assignment
and displays UniversalNo later at the same point. At `64f4f25`, the publisher kept that
deferred transaction pending for two rendering ticks, allowing a complete frame
before another refresh can start recovery through the intermediate producer.
Restore detaches the pending frame observer and invalidates its epoch. This is
a candidate timing correction; the displayed-cursor deadline and expected
handles remain unchanged. The Checkbox fixture additionally forces one real
publication at a stationary owned point and requires exactly one completion,
one frame acknowledgement, no cancellation and actual UniversalNo display.
Its existing owner-release cancellation and repeated lifecycle checks remain.
Both solution builds are clean and all 38 regressions pass in
`artifacts/tests-cursor-publication-settle`. The complete local x64 catalog
passes both themes at a 1024x745 client size in
`artifacts/smoke-cursor-publication-settle-full`, including all 192 Input samples,
the original cancellation checks and exactly one completed/acknowledged forced
publication per theme. Each completion records two rendering ticks, no pending
observer and actual UniversalNo. The earlier maximized run stopped at its
unchanged composed-client occlusion guard before exercising publication; its
failure remains in `artifacts/smoke-cursor-publication-observation-input`.
Hosted ARM64 verification is still required for the frame-settlement candidate.
The fresh independent ARM64 consumer cross-build also passes in
`artifacts/package-consumer-publication-settle-buildonly`, restoring package
SHA-256 `ed1de471d9f642c68530fb1eec056730ccdab6e6a8ce1aadb1a2e4561650bba8`.
Its cache is removed successfully; `runtimeVerified` remains false on this
x64 host. Native consumption remains a required hosted gate.

The pinned [WinUI 2.5.1 input source](https://github.com/microsoft/microsoft-ui-xaml/blob/ba3a8d59e6de4e2b86ca4f61c78bc26ccf129afd/src/dxaml/xcp/core/input/InputServices.cpp#L1057)
walks the captured/entered element ancestry and publishes through the same
`InputPointerSource.Cursor` API. Its interaction with Orchard's publisher is
still under investigation. The strict native cursor checks remain required.

Run [38556522194](https://github.com/gaulatti/orchard/actions/runs/38556522194)
at `7d11f1b` does not establish ARM64 cursor recovery. The failing Dark
Medium/top-right sample reports Arrow while its publisher has queued 86
recoveries, completed 85, canceled none and has one recovery pending. Source
and root still report the same accessible dispatcher; paint, opacity and
ownership are correct. The deferred assignments therefore execute but do not
resolve this observed native mismatch. The current scheduling explanation
remains unproven, and the unchanged displayed-cursor gate remains required.

The same run fails the new Error State real-contrast check on both architectures:
only 242 of 243 established glyph pixels adopt WindowText. The existing broader
contrast suite and fresh independent consumers pass on both architectures.
The full x64 catalog passes; the full ARM64 catalog instead fails native menu
radio exclusivity. These are separate retained failures, not evidence that the
component inventory or the cursor feature is complete. No glyph masks, pixel
counts, timing bounds or radio-selection assertions are relaxed.

Local C: reaches zero available bytes during the following audit. Scoped
lossless compression preserves evidence but recovers no usable space. Local
builds, repairs and fresh restores require storage recovery; hosted evidence
can still be inspected. The local checkout remains at `7d11f1b` until it can
fetch this documentation commit. The goal remains active.

The next cursor candidate separates the stale-value recovery assignments into
two dispatcher callbacks. The current same-callback producer/UniversalNo
transition still fails on ARM64 despite continued publication, correct paint,
owned foreground/point and matching source/root dispatcher identity. Combining
the assignments inside the SDK is a hypothesis, not an established cause.

On a detected owned mismatch, the publisher assigns its exact retained
producer selection and queues UniversalNo at Low priority. Rendering/layout
refreshes preserve the intermediate selection while that operation is pending.
The callback checks its version, current root/source and active ownership.
Release invalidates queued work before restoring the latest native producer;
resource/null selections remain authoritative. Queue rejection fails explicitly.
Normal publication and the existing restore path are otherwise unchanged.
There is no SetCursor call, invented restore shape, widened deadline or
substitution for GetCursorInfo's displayed handle and showing flag. The queue
ordering follows [DispatcherQueue.TryEnqueue](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueue.tryenqueue?view=windows-app-sdk-2.0).

The focused local x64 suite passes both themes at
`artifacts/smoke-cursor-dispatch-input`, including all 192 rounded samples and
final cursor exit. Its counters record twenty completed deferred publications
in each theme (cumulative twenty/forty), so the candidate path was exercised.
Catalog diagnostics record queued/completed/canceled counts and pending state.
The Checkbox regression deliberately schedules this real publication for its
actual owner, disables native hit testing before delivery, then requires one
canceled callback, no completion, an actual Arrow and unchanged pointer
position before restoring the owner. This controlled scheduling is test-only;
it does not replace native cursor assertions or expose a production API.
Native ARM64 recovery and the complete lifecycle suite remain required.

The first complete candidate run at `artifacts/smoke-cursor-dispatch-full`
passes Light cancellation, then fails the same strict check in Dark. Disabling
IsHitTestVisible does not necessarily refresh the observer synchronously before
a Low-priority callback. That exposes stale ownership at delivery. The callback
now re-reads the actual native point and current XAML hit before evaluating its
version/source/ownership guards. The test records counters and the real cursor
on both success and failure, and keeps its exact cancellation/no-completion
and displayed-Arrow requirements. Recovery remains a candidate pending the
updated complete run and hosted ARM64 verification.

The corrected complete local suite passes both themes at
`artifacts/smoke-cursor-dispatch-delivery-hit`, including Input rounded samples,
stationary-pointer hit testing, template removal/restoration, re-enable,
remount, clipping/occlusion, scrolling and shared Input/Checkbox ownership.
The cancellation record preserves counters and the actual global cursor:
Light changes canceled 2 to 3 with completed fixed at 38; Dark changes canceled
6 to 7 with completed fixed at 80. Each owner restoration again requires the
actual UniversalNo cursor. The previous Dark failure is therefore a regression
for the delivery-time hit check, with no relaxed cancellation or cursor gate.
This is native-only and has no backend/metrics impact. Hosted ARM64 recovery,
real contrast and independent package consumption remain pending.

Both final solution builds pass without warnings and all 38 unit/resource
regressions pass at `artifacts/tests-cursor-dispatch`. Local disk capacity does
not support another fresh independent restore; the hosted consumer remains a
required gate. No complete cursor or component acceptance is claimed before
native ARM64 and real-contrast results establish this candidate's behavior.

The pinned browser Input paints its background through the complete border
box. Its source SHA-256 is
`01bce586edd4c1ad4aed30415b5aa9cbc6e620a874e4751cf7299bfa54ccc192`.
The frozen React 19.2.4/Tailwind 4.2.2 runtime confirms computed
`background-clip: border-box` for normal, invalid, disabled and disabled-invalid
states in both themes at `artifacts/input-border-box-browser/reference.json`.
This also matches the [CSS background painting contract](https://www.w3.org/TR/css-backgrounds-3/#background-clip).

The old native BorderElement used WinUI's InnerBorderEdge default, leaving its
translucent border without the Input background underneath. In the failed
complete local capture at `artifacts/smoke-error-title-full`, pixel x327 on the
Small input's middle row has alpha 254 instead of the focused capture's 255;
the unchanged strict opaque-span check consequently measures 317px for the
320px control. Both saved and directly analyzed frames reproduce that edge.

The Input style now defaults the existing native BackgroundSizing property to
OuterBorderEdge and passes it through TemplateBinding to BorderElement.
This paints the provided background beneath the source border without changing
brush values, border width, rounded geometry, alpha thresholds, glyph masks or
timing tolerances. Consumers can select InnerBorderEdge through the native
property, then clear it to restore the source default. The native fixture checks
all eight catalog Inputs and the live override/restoration; the independent
package consumer checks the same public boundary. See the
[WinUI background-sizing contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.control.backgroundsizing?view=windows-app-sdk-2.0).

This native-only change has no backend or metrics impact. The wiki remains
unavailable, so the contract is maintained in this repository.

The focused native x64 suite passes both themes at
`artifacts/smoke-input-border-box`. The full local x64 catalog then passes both
themes at `artifacts/smoke-input-border-box-full` with every existing assertion
retained. Its formerly failing Small input now has an exact 320px opaque span
from x9 through x328 on the middle row; both x327 and x328 have alpha 255.
Native captures include entered/placeholder Arabic in both reading directions,
all sizes, disabled/error styling, pointer lifecycle, motion and theme checks.
Both solution builds pass without warnings, all 38 regressions pass at
`artifacts/tests-input-border-box`, and token generation matches the source.
The updated fresh package consumer and native ARM64 paint/contrast checks
remain required in hosted CI; local fresh restore remains constrained by disk.

Run [38551291475](https://github.com/gaulatti/orchard/actions/runs/38551291475)
at `c6f0c75` closes the preceding Error State title/contrast and after-chart
checks on both architectures, including fresh independent consumption. Its
x64 job and ARM64 build pass. The native ARM64 job still fails rounded disabled
Input cursor publication with Arrow displayed despite UniversalNo selected.
Its diagnostic confirms source/root thread access and the same dispatcher on
ARM64 too. The cursor defect therefore remains open; it is not explained by a
simple dispatcher mismatch, and no cursor correction is claimed here.

Run `38545127689` at `a236ab7` still fails the actual ARM64 cursor boundary.
The verified runtime archive `11884776670` has SHA-256
`2319baa17e66cce4ba77f22a8c3969b330af939d48554af349fdb614d75ece5f`.
Its rounded matrix passes all 96 Light samples, then exit reports an owned
point on Heading with hover/ownership cleared and the source selecting Arrow,
while GetCursorInfo displays AppStarting. The Checkbox repeat-restoration
failure in the same archive also shows continued republishing with Arrow
displayed and UniversalNo selected. The existing same-callback producer
transition therefore does not establish recovery.

The catalog observer additionally records source/root DispatcherQueue thread
access and queue identity. The local focused suite passes both themes at
`artifacts/smoke-input-cursor-queue`; its active source and root both report
thread access and the same queue. This rules out a simple queue mismatch for
that x64 observation; it does not establish ARM64 queue identity or repair the
displayed-cursor defect. No cursor writes, native assertions or deadlines are
changed by these diagnostics.

The rounded-pointer runtime matrix translates each specimen by less than one
physical pixel to an integer screen origin before issuing the pinned one-pixel
corner samples. This addresses the local catalog's fractional centered column:
at x=1672.5, truncating the requested local (0,0) to x=1672 misses the input
before a cursor assertion can run. Reports retain original/aligned bounds and
the translation, require unchanged dimensions, and restore the original local
transform value (or clear it when originally unset) on success or failure.
Cursor, border, opacity and focus expectations remain unchanged. Fractional
layout, display scaling and full native/browser raster acceptance remain open;
this specimen setup is not evidence for those requirements.

`BleeckerInput` derives from WinUI TextBox and supplies a branded template with
consumer-owned text and decorative StartIcon/EndIcon content. It is an incomplete
preview against the [pinned reference](input-reference.md), not accepted parity.

The font-settings fixture checks that italic style, condensed stretch and a
disabled text-scale preference reach the editor, placeholder and baseline metrics
element, then restores consumer values. The existing template passes those
inheritance checks at 100% text size. Typography diagnostics now record Windows
text scale separately from display scale; the retained browser pixel profile is
accepted only when both are 100%.

Scaling remains an open gap: `artifacts/smoke-input-text-225` fails the Japanese
Small input viewport-height assertion at 225% text size, and the catalog sidebar
clips enlarged labels. Windows text size was restored to 100%. Further scaling
verification must use an isolated test environment rather than changing the
developer's desktop-wide settings.

Text and placeholders use `FlowDirection` with start alignment by default,
matching Bleecker's LTR/RTL input alignment rather than inferring alignment from
the entered script. The Input fixture verifies 24 rendered alignment cases
across sizes and themes, and captures Japanese, Arabic, Hindi and mixed Unicode
values. Adornments and their padding stay on physical left/right sides in both
directions, with a further 72 runtime fixture cases covering placement and text
insets. See the reference audit for remaining fallback-font gaps; these checks
do not establish complete multilingual parity.

```xml
<orchard:BleeckerLabel Content="Title" Target="{Binding ElementName=TitleInput}" />
<orchard:BleeckerInput x:Name="TitleInput" Text="Harbor update"
    InputSize="Medium" PlaceholderText="Title" />
```

InputSize supports Small, Medium and Large. The preview selects 36/40/44px
minimum heights, 13/14/15px Encode Sans text and 12/14/16px horizontal padding.
Each supplied adornment reserves 40px on its side. Adornment presenters are
decorative and do not intercept pointer input. Consumers supply their icon
artwork and accessible input name through the normal WinUI automation contract.

Native TextBox text, selection, read-only state, bindings and value automation
remain available. IsError currently controls appearance only; it does not
validate consumer data or yet expose a complete validation accessibility API.
Use a separate label and description composition. Password masking and native
HTML input-type adaptations are not provided by this text-editor preview.

The template owns background, border, focus ring, shadow and placeholder colors; it
omits the Windows underline and clear button. The focus palette includes the
pinned dark error input's blue border/ring. Windows contrast resources provide
paired system colors, but actual contrast acceptance remains outstanding.
Existing BleeckerTextBoxStyle consumers, especially multiline Summary, remain
on their previous preview until the applicable input/textarea contracts pass.

Remaining work includes exact glyph/baseline and scaling comparison, custom
cursor restoration, error semantics, shadow raster and motion comparison,
live brush updates, broader editing/clipboard
regressions, physical keyboard/pointer, Narrator and ARM64 runtime verification.
The catalog fixture covers all sizes and representative empty, populated,
adorned, error, disabled, read-only and constrained multilingual examples.
The focused smoke also captures every size with identical entered/placeholder
text and records native line geometry. The [typography audit](input-reference.md#typography-comparison-at-100-display-scale)
records the original Medium entered-text and Small/Large placeholder offsets.
The current baseline correction passes the retained browser text-row profiles
for all three sizes, entered/placeholder text, both themes and live size changes
at 100% display scale. Other scales, fallback scripts and complete raster parity
remain unverified.

## Verification checkpoint

`artifacts/smoke-input-preview` passes both themes, including all three heights,
native selection/replacement, edit role/name, disabled focus refusal and the
error-focus palette. The isolated package consumer passes at
`artifacts/package-consumer-input-preview`. x64 publish, ARM64 build and the
13 unit/coverage tests pass.

Physical dark-theme pointer focus and typing changed the real editor value;
`artifacts/input-physical-edit.jpg` retains that evidence. Clicking the error
input changes its terracotta rest border to blue, captured in
`artifacts/input-dark-error-focus.jpg`. These checks do not establish the full
keyboard, clipboard, validation or accessibility contract. Native TextBox value
patterns require an external UI Automation client; the managed peer pattern
cast is not an equivalent test.

The disabled/error combination retains its terracotta border while the editor
is dimmed to .6 opacity. Decorative adornments remain outside the dimmed editor.
Both themes use light-sand at 65% alpha as the disabled background, separately
from the editor's .6 opacity. Dark therefore uses `#A60D1821`, not the ordinary
opaque deep-sea fill. The rendered pinned reference confirms this cascade.
The registered fixture includes this combination. Runtime regression coverage
also changes each size with adornments removed and restored, and checks that
presentation changes retain native text selection.
These checks pass in both themes at `artifacts/smoke-input-states`; the retained
captures include the disabled invalid example. x64 publish and ARM64 build pass.

The shadow preview uses the pinned rest `0 1px 2px rgba(26,55,77,.025)` and
hover `0 3px 10px -8px rgba(26,55,77,.25)` values. A Composition
DropShadow uses a rounded alpha mask; hover contracts that mask by 8px per side.
The mask source does not paint an extra background layer. The shadow follows
editor geometry, rebuilds on corner-radius changes and clears on unload.
Windows contrast mode suppresses decorative shadows. Reference blur/raster
equivalence, contrast switching and the 190ms shadow transition remain
unaccepted; exact parameter values alone do not establish visual parity.

`artifacts/smoke-input-shadow` passes rest shadow parameters, editor resizing
and unload/reload cleanup in both themes. x64 publish, ARM64 build and 13
unit/coverage tests pass. The live light-theme hover capture is retained at
`artifacts/input-light-hover-shadow.jpg`; it is a rendering checkpoint, not
an exact blur comparison or motion acceptance.

Live-theme regression switches between light and dark with an invalid editor
focused and text selected. It checks normal background/border/text/placeholder,
error focus border/ring, disabled invalid background/border and decorative icon
foreground together. Restoring the original theme must preserve consumer text,
selection, focus and geometry. This check covers ordinary appearance changes;
Windows contrast switching and live mutation of consumer brushes remain open.
Both directions pass in the full catalog suite at
`artifacts/smoke-input-live-theme`. This checkpoint changes verification only.

## Color and opacity motion preview

Border, background, focus-ring color and focus/disabled opacity use the 190ms
control duration and cubic-bezier(.22,1,.36,1). Each editor owns its animated
brushes. New state changes begin from the currently rendered values, while
unchanged colors skip interpolation. Source brush color/opacity changes update
the presentation without modifying shared resources. Non-solid consumer brushes
remain supported and are applied immediately rather than color-interpolated.

Reduced motion settles immediately. Live theme changes, motion-preference
changes and unload stop active transitions and apply their destination values.
The shadow transition is covered by the later shadow checkpoint below.
Neither checkpoint establishes complete Input motion parity.

For focused diagnostics, run `scripts/smoke-catalog.ps1 -Component input` with
the usual executable/output arguments. This mounts the same Input fixture alone
and retains the same assertions. The default remains the full catalog suite.
The full enabled-motion run on this host missed entire 190ms transitions:
`artifacts/smoke-input-motion-enabled-idle/input-motion-Light.json` records a
gap from 1ms to 228ms. Catalog frame responsiveness therefore remains an open
performance issue; isolated motion evidence does not close that requirement.

The enabled focused run passes at `artifacts/smoke-input-motion-focused-ready`.
Its JSON samples include intermediate colors and opacity values in both themes
and assert that another input's brush remains unchanged. The same run also
checks live theme restoration across the animated state palette. Windows
animation effects were restored to their original Off setting after this run.
The full reduced-motion suite passes at
`artifacts/smoke-input-motion-reduced-final`, including immediate color/opacity
settling. The final x64 publish and ARM64 build pass. Neither the isolated
enabled run nor these builds establish full-catalog enabled-motion performance,
exact browser/native interpolation equivalence, or ARM64 runtime acceptance.
The isolated package consumer passes at
`artifacts/package-consumer-input-motion`; all 13 unit/coverage tests pass.

The searchable catalog now passes its full suite with animations enabled at
`artifacts/smoke-navigation-motion`, including intermediate Input frames in both
themes. This supersedes the earlier full-catalog sampling failure as the latest
result; repeatable performance budgets and exact motion equivalence remain open.

Disabled-background correction passes the focused light/dark runtime suite at
`artifacts/smoke-input-disabled-background`, including plain/invalid disabled
states, re-enabling/disabling and live theme restoration. The captured dark
fixture was visually inspected. All 13 tests and x64 publish pass. The pointer
checkpoint below extends this background correction; the complete disabled-state
contract remains subject to the outstanding visual and accessibility checks.

## Disabled pointer preview and regression fixture

Plain and invalid disabled inputs use the reference hover border and shadow,
retain .6 editor opacity, refuse focus, and select the not-allowed system cursor.
The control observes the root pointer because WinUI routes disabled TextBox
input to enclosing scroll chrome. A rendering observer refreshes stationary
pointer geometry, including clipping, scrolling and remounts. Its registration
detaches on enable or unload. Input and Checkbox share one observer per XAML
root and one native cursor override owner. The observer stops when its last
disabled control detaches; pointer queries and hit testing are shared across
registered controls. A coalesced dispatcher callback reconciles the control's
current loaded state after rapid reparenting notifications; true unload releases
immediately. Native cursor restoration uses WinUI's current selected system
cursor rather than a cursor saved before entering the input.

Open Input in the catalog and choose **Verify disabled pointer lifecycle**.
Move the pointer over **Unavailable invalid input** and leave it still. The
fixture runs plain and invalid states in both appearances, checking native
cursor handles, actual template borders, disabled focus refusal, enabling,
occlusion, movement, clipping, scrolling and remounts. It restores the original appearance
and fixture state after success or failure. The result is written to
`%TEMP%/orchard-input-pointer-result.json`; it is a separate physical-pointer
check, not part of the unattended smoke command.

All 52 transitions pass at 100% display scale in
`artifacts/input-pointer-scroll-result.json`. The focused Input suite passes
at `artifacts/smoke-input-pointer-render`, and the full reduced-motion catalog
suite passes at `artifacts/smoke-input-pointer-integration`. Custom resource
cursor restoration, scaling and observer performance remain open. These results
do not establish full Input parity or ARM64 runtime support.

The independent x64 package consumer passes at
`artifacts/package-consumer-input-pointer`, verifying packaged resources, fonts,
both themes and a native action. Its package SHA-256 is
`8a5547a015bc4e3c21647e4ce575d1127d24c1b4d4f82fa03265dfd8a7408c39`.

## Shadow motion and exterior clipping checkpoint

The shadow retains one source visual and interpolates blur, offset, negative
spread and opacity together over the 190ms premium curve. Interrupted returns
sample the current transition and shorten its duration according to the
[CSS reversal rules](https://www.w3.org/TR/css-transitions-1/#reversing).
For these two endpoints and the non-overshooting curve, the shortening factor
is the distance to the new endpoint. Reduced motion applies the destination
immediately. Zero-width geometry hides the shadow; restoring width restores it.

A CompositionVisualSurface renders the shadow through an even-odd alpha mask
that excludes the rounded border box. This prevents shadow tint from leaking
through translucent disabled backgrounds. The mask follows size and corner
radius and uses the existing composition APIs without a graphics dependency.
The before/after captures are `artifacts/input-shadow-interior-leak.jpg` and
`artifacts/input-shadow-exterior-light.jpg`. An amplified red diagnostic shadow
in `artifacts/input-shadow-amplified-clip.jpg` confirms that exterior output
remains visible while the interior is excluded. Those diagnostic color/blur
values were restored before the final build; that capture is not brand parity
evidence. JPEG pixel samples are diagnostic, not exact raster acceptance.

The physical-pointer fixture checks source identity, coordinated interpolation,
intermediate frames, destination parameters, shortened returns and repeated
reversal direction. It measures reversal timing after the native cursor confirms
that hover was applied, allowing 50ms for frame presentation and sampling.
At least one timing budget must be below 180ms to distinguish shortening from
a full 190ms restart. All 60 enabled-motion stages pass at 100% display scale in
`artifacts/input-shadow-final-pointer-result.json`; three of four timing samples
meet that discriminating budget, with observed returns of 92–122ms. All four
cases pass repeated reversals. The earlier clipped implementation also passes
52 reduced-motion pointer stages at
`artifacts/input-shadow-exterior-pointer-result.json` and the focused suite at
`artifacts/smoke-input-shadow-exterior`.

Exact native/browser blur and antialias comparison, scaling, Windows contrast,
Narrator, observer performance and ARM64 runtime remain unverified. This
checkpoint does not establish complete Input parity.

The final full enabled-motion catalog suite passes at
`artifacts/smoke-input-shadow-final-full`; the focused reduced-motion Input
suite passes at `artifacts/smoke-input-shadow-final-reduced`. Windows Animation
effects were restored to their original Off setting. All 13 unit/coverage tests,
the token drift check using Node 24, x64 publish and ARM64 compilation pass.
The independent x64 consumer passes at
`artifacts/package-consumer-input-shadow-final`, including packaged resources,
fonts, both themes and a native action. Package SHA-256:
`e68523aedf7a509cc0a31efa38c781fd4ed9169d980e094eedbcf6db59543b7e`.

At `3485f52`, the x64 catalog and independent consumer pass. Native ARM64
passes Light Input and the later Light Checkbox/Switch checks, then fails
Dark Input's disabled-to-enabled validation check. The assertion still requires
the enabled error border and full editor opacity. The fixture now retains
`input-disabled-validation-{Light,Dark}.json` and element captures, including
the requested/current visual states, focus owner, palette and rendered
brushes/opacity at each transition and on failure. These diagnostics distinguish
an unexpected hover/focus state from unsettled motion without changing or
weakening the acceptance criterion. No product styling changes accompany them.

The `9c6a8ce` ARM64 diagnostics identify `InputHoveredError` as the cause of
that run's resting-border assertion failure: the stationary desktop pointer
selected the correct reference hover border (`#40FFFFFF` in Dark), with full
editor opacity and matching rendered/palette brushes. The resting validation
fixture now temporarily disables hit testing on its target, waits for the
resting error border, and asserts `InputNormalError` after enabling. It restores
the original hit-testing setting in `finally`. The physical disabled-pointer
lifecycle fixture continues to verify the hover/error cascade separately.
This fixture correction does not change product styling or shorten its waits.

### Cursor handoff from disabled controls

The Checkbox physical fixture also exercises a shared Input: disabled Input to
disabled Checkbox, enabling the Input while the pointer remains on Checkbox,
then moving into the enabled editor. The earlier `8b8be3c` assertion compares
the UI thread's GetCursor handle with IBeam and fails, despite both WinUI cursor
properties selecting IBeam. At `706ee10`, GetCursorInfo confirms the displayed
cursor is IBeam; that earlier transition failure is a thread-cache observation.
The same global report exposes a real disabled-cursor defect: the displayed
Checkbox cursor stays Arrow despite the UI thread reporting UniversalNo.

The shared override now sets the root InputPointerSource cursor. It retains
cursor objects published by WinUI during the override and excludes its own
UniversalNo object, then restores the latest producer selection on release.
Stationary enabling explicitly selects the enabled control's cursor. The
observer coalesces a refresh after routed pointer dispatch as well as observing
layout/render changes. Both Input and Checkbox physical checks now require the
visible global cursor from GetCursorInfo; hidden/suppressed cursors cannot pass.
Property values and the UI-thread cache remain diagnostics only. Native x64
and ARM64 runtime verification is required for this correction.

### Rounded disabled pointer boundary

The pinned browser Input uses a 7px rounded border as its hit region. Its three
sizes (36/40/44px high), valid/invalid states and Light/Dark appearances exclude
the outer three pixels at either end of the top/bottom edge. Those corner pixels
retain the surrounding cursor and resting border; the next pixel enters
disabled hover and selects the not-allowed cursor. Orchard's disabled observer
now intersects the pointer sample with the control's actual CornerRadius, rather
than treating the full editor as a rectangle. Clipping, occlusion, viewport and
cursor ownership checks still apply.

`tests/Reference/InputRoundedHit.json` records the actual unmodified Input from
Bleecker commit `a86059013cd59684ce452849278ea81e734f2066`: 96,000 integer
points and 240 fractional/edge observations over both appearances. The regression
compares the shared native geometry with every recorded hit result. The registered
Inputs fixture now physically samples each corner and its included neighbor for
all three sizes and both validation states, requiring the global visible cursor,
rendered border, disabled opacity and absence of focus. It retains
`input-rounded-pointer-{Light,Dark}.json`, rest/hover element captures for every
size/validation combination, plus native desktop/point diagnostics.
Each rest/hover case also retains an owned, composited window capture and its
client/target rectangles. These captures show the actual background composition;
transparent element PNGs can magnify low-alpha channel quantization when viewed
in isolation. Window occlusion checks remain active during capture.
Native x64/ARM64 verification and display/text scaling remain required before
accepting this boundary.

At `130d653`, x64 reaches the first rounded sample's expected visible cursor and
border, then fails the new fixture's double-literal opacity assertion. Its
retained validation report records native 60% opacity as
`0.6000000238418579`. The fixture now awaits the exact float-represented 60%
destination with cursor, border and unfocused state; it records actual opacity
and focus on each accepted sample and in failure details. The checked archive
`11782215973` has SHA-256
`af529700868802695814960ac4ea383c8afc5b90c4ae6a014f5619b105e44203`.
This fixture correction requires new native evidence; the product styling and
per-state acceptance values are unchanged.

At `76c09db`, both native architectures pass all 96 rounded-pointer samples in
Light: three sizes, both validation states, all corners and included neighbors,
visible global cursors, rendered borders, exact native 60% opacity and no focus.
The native rest/hover images are retained for all six size/validation cases.
Both suites later fail Checkbox's immediate template-detachment cursor check,
before reaching Dark Input or their independent consumers. This is Light
boundary evidence, not complete Input or suite acceptance. Checked archives:
x64 `11781357506`, SHA-256
`ed2feee247a7266232743aa0ef96ca2a91a032c98e7b7bc454eaaf028dc356d8`;
ARM64 `11781267570`, SHA-256
`acaa7cda4f49d7e1e79a095d617716e114e0c17f0cc32c8350c985aeebb42bb7`.

At `7024b9e`, the complete hosted catalog and independent consumer pass on x64
and native ARM64. Both themes pass all 96 rounded-pointer samples, with twelve
composited window captures and target/client bounds per theme. The twelve paired
valid-rest/invalid-hover crops across all sizes and themes have identical RGB
bytes between architectures at 100% display/text scale. These composited pixels
do not show the apparent blue shadow-channel artifact from isolated low-alpha
element PNG previews. The comparison is retained in
`artifacts/input-composited-7024b9e-comparison.json`; archive/package digests are
recorded in [runtime evidence](runtime-evidence.md). Browser/native parity,
enabled corner interaction, contrast/Narrator and scaling remain open.

The local `artifacts/smoke-brand-motion-all` run stops at the reading-direction
opaque-span assertion. Its isolated element preview alone cannot identify the
second raster's origin or span. The fixture now saves that exact raster and
reports control name/size/enabled/focus, transformed bounds, row, scale,
bitmap dimensions and opaque first/last/count on failure. The unchanged
assertion passes in the subsequent full Input check
`artifacts/smoke-motion-input-diagnostics` and repeated complete catalog
`artifacts/smoke-brand-motion-all-diagnostics`, both appearances. This is an
unresolved intermittent observation, not evidence of a repaired product defect.

Hosted run `38458612519` at `883b0c9` passes the complete native ARM64 catalog
and independent consumer. Its x64 Input corner check fails with a visible
AppStarting cursor while the producer reports Arrow and the border/opacity,
foreground and hit target are correct. The exact displayed-cursor gate remains
required; the failure is not accepted as normal Arrow behavior or silently
ignored. Both architecture contrast checks and independent consumers pass.

Run `38461347470` at `77eb85a` passes the complete x64 catalog but fails the
ARM64 Large/invalid corner check at bottom-right `3,0`. Windows displays Arrow
while the island publisher reports UniversalNo; foreground and the screen point
remain owned by the catalog, and hover/border/opacity are correct. This is an
unresolved publication boundary, distinct from the earlier AppStarting result.
Both live contrast checks and independent consumers pass. The displayed-cursor
assertion stays unchanged.

Run `38467123988` at `05a2109` passes the x64 full catalog but fails the
ARM64 Small/valid bottom-right `3,0` corner. It reproduces the same displayed
Arrow versus published UniversalNo condition with the correct hover, border,
opacity, owned native point and foreground. The full ARM64 suite has already
passed Alert and Bauhaus before this failure; the focused Bauhaus fixture,
live contrast and independent consumer also pass. This remains a cursor
publication defect, without an accepted substitute or widened timeout.

The rounded-pointer failure trace now also records shared observer refresh,
block, republish and restore counts; whether the last publisher read was
Orchard's override; its last observed native handle; and the retained/restored
SDK cursor type and system shape. These bounded private counters observe the
existing path without changing cursor selection. The catalog reads the state
for the control's own XAML root, including after override release, so it can
distinguish a missing refresh from repeated unsuccessful publication. The
visible global cursor and owned foreground/point still determine acceptance.

The focused local x64 Input run passes both appearances at
`artifacts/smoke-input-cursor-diagnostics`. In its final Light hover sample,
the observer has 1889 refreshes, 556 block calls, 17 republications and 23
restores; the retained producer is Arrow and the displayed cursor is the
expected UniversalNo. This verifies the diagnostics' native execution and
shows that republication also occurs in a passing local run. It does not
establish the cause of the hosted failure. Both builds have zero warnings and
all 27 unit/resource tests pass.
The complete catalog also passes both appearances at
`artifacts/smoke-cursor-diagnostics-all`, including the shared Checkbox/Input
owner, hover, release and reattachment paths. This remains diagnostic evidence,
not a claim that the hosted publication failure is fixed.
A fresh isolated x64 consumer passes at
`artifacts/package-consumer-cursor-diagnostics`, package SHA-256
`49cc71e969011340fc89dc6cca64fcbeae9718545cdba208a228d3a394044184`.

At `289a8ba`, run `38470614305` passes x64's focused Bauhaus full-raster and
live contrast checks but fails Input's Large/valid bottom-left `2,0` corner.
The displayed cursor is AppStarting while the publisher reports Arrow and
the hover, border, opacity, owned point and foreground match the reference.
Fresh diagnostics are still required for that hosted observation.

CI runs the focused Input fixture after Bauhaus and before the complete catalog
on both native architectures. Its exact displayed-cursor and publication trace
can therefore run independently of later overlay/lifecycle failures. A failure
still fails the job, and the complete catalog, contrast and independent
consumer continue with their existing gates. No assertion or timeout changed.

Run `38473604856` at `c8018ba` exposes a separate focused-fixture setup issue:
the Light live-theme check measured sand/70% (`#B3E6D5B8`), the correct hovered
border, while requiring the resting sand/40% border. The full catalog, contrast
and independent x64 consumer passed. The live-theme fixture now acquires the
shared native input session and moves the real pointer to its heading before
focusing the invalid editor and changing appearances. It retains every color,
focus, selection and geometry assertion. `input-theme-{Light,Dark}-desktop.json`
records ownership and the physical movement; production Input behavior is
unchanged.

The focused x64 fixture passes both appearances at
`artifacts/smoke-input-theme-heading` with display/text scale 1 and animations
disabled, preserving the exact palette and native cursor checks.

Run `38475433445` at `764f5b2` still encounters the hovered border in x64's
focused palette check. The verified archive `11853753529` has SHA-256
`61d52cf12a60c91a81d213399243c6c80381e994768e9b40146abba03c27bb6c`.
Its recorded initial movement reaches Heading at `(416,158)` with owned
foreground, but it precedes invalid-editor focus and does not record the final
control bounds. Focus may queue bring-into-view layout after changing focus
state. The fixture now moves to Heading again after focus; palette failure
details include the current Heading/Medium bounds and global pointer/cursor
snapshot. All original assertions remain required. This hypothesis still
requires hosted verification; the initial correction is not a demonstrated fix.

The focused x64 check with post-focus placement passes both appearances at
`artifacts/smoke-input-theme-focused-point`; its hosted result remains pending.

At `64a2b27`, run `38477265194` disproves the queued-scroll explanation for
the x64 palette failure: the final pointer `(416,158)` is on Heading's
`(248,138,336.11206,40)` rectangle and outside Medium's
`(257,238,320,40)` rectangle, while Medium still paints the hovered border.
The next diagnostic adds the control's pointer flag, requested appearance,
palette border and actual VSM groups to distinguish stale hover state from
palette/motion state. The resting color assertion remains unchanged; moving
the pointer again is not considered a fix for this observed product defect.

The verified ARM64 archive for `64a2b27` is `11856081210`, SHA-256
`bc422f0ce5dcbd80257ad70115b9fc34f234c17dd29a8035674f98e7ed220aea`.
Its focused Small/invalid top-right `(3,0)` failure shows 47 refresh/block
calls and 46 republish attempts between 267.451ms and 1027.7992ms, while the
global cursor remains Arrow and the SDK retains an owned UniversalNo object.
The source's retained producer is Arrow and no restore occurs during the wait.
This rules out missing refreshes for that sample and demonstrates that the
object-identity republish is ineffective there.

The publisher now sends its exact retained producer selection before sending
the blocked cursor when it observes this owned visible-cursor mismatch. Two
UniversalNo objects describe the same native system cursor; the intermediate
producer selection supplies an actual SDK value transition. It preserves the
producer's resource cursor or null selection and adds no inferred restore
shape. The cache explanation is an inference; successful global output and
all native restore/lifecycle checks remain the required verification.

Both architecture builds and all 27 unit/resource tests pass for this candidate.
The full x64 catalog passes both appearances at
`artifacts/smoke-cursor-producer-transition-all`; a new isolated consumer passes
at `artifacts/package-consumer-cursor-producer-transition`, with package 0.1.0
SHA-256 `ef22610d347f5eaee0f34d3b33c82708a4434ecd7cd0b40a9c96a035a7fba4ba`.
The separate enabled-hover/palette defect remains open.

The smaller-window reproduction uses the explicit verifier option
`-ClientSize 1024,697`, which sets this test window's client area in physical
pixels. With the existing implementation it reproduces the hosted defect:
the pointer is on Heading and outside Medium, WinUI's CommonStates is Normal,
but Orchard's pointer flag is true and InputStates is InputHovered.
The failing report is at `artifacts/smoke-input-client-1024`.

Input now observes enabled as well as disabled geometry through the shared
root observer. Its hover source is the current owned point, rounded editor
bounds, visibility, clipping and actual hit ancestry. Enabled descendants,
including TextBox's nested ScrollViewer, belong to the editor; a sibling
overlay occludes it. The observer blocks the cursor only for a disabled owner.
The independent PointerEntered/PointerExited flag writers are removed from
Input, so layout/remount changes cannot leave those writers out of sync.
Checkbox retains its existing disabled-only registration.

The live-theme regression now exercises actual enabled hover entry/exit and
stationary sibling-overlay show/hide, verifies that the pointer does not move,
and then runs the original focus/selection/theme palette assertions. The
focused 1024×697 check passes both appearances at
`artifacts/smoke-input-geometric-overlay-1024`. Larger-window integration and
hosted verification remain required for this candidate.

The full maximized x64 catalog passes both appearances at
`artifacts/smoke-input-geometric-all`, including shared Checkbox/Input ownership,
rounded boundaries, stationary hit testing, scrolling and reattachment. Both
architecture builds have zero warnings; all 27 unit/resource tests pass.
The default maximized and explicit 1024×697 tests use the same runtime path.

A new isolated x64 consumer passes both appearances at
`artifacts/package-consumer-input-geometric`, using package 0.1.0 with SHA-256
`be56f853db8d8107b54981c1e6eaa78543e219e9148bb1c7ad19919bb70febb3`.

Rapid remount verification also runs through the shared Checkbox/Input pointer
ownership fixture. Use `scripts/smoke-catalog.ps1 -Component checkboxes` for that
fixture in both themes. The mode uses the existing Foundation/theme/fixture
selection preparation and records the normal catalog captures.

`artifacts/smoke-area-chart-observer-full` passed its chart checks but rejected
Dark Input rapid remount round 1's exact disabled hover border/cursor condition.
That original assertion did not retain its individual reads, so its failure
cannot be classified as a producer defect or an observation race. The focused
diagnostic's original condition passed both themes; its first command wrapper
then rejected missing catalog captures, which the focused route now supplies.

The remount gate now requires two rendered, settled observations, matching the
preceding disabled-under-pointer gate. It retains the exact UniversalNo handle,
theme border, stationary coordinates and two-second deadline, and additionally
checks owned foreground/cursor with no queued publication. Per-frame success
and failure evidence is retained. This changes verification timing rather than
Input or cursor production; it does not establish the cause of the original
unrecorded failure. The complete published catalog passes both themes at
`artifacts/smoke-area-chart-published-full`, under the unchanged six-minute
deadline. All six rapid Input remounts record two ready rendered observations
at approximately 114ms. The focused published mode passes both themes at
`artifacts/smoke-checkbox-remount-published`, including the required captures.
