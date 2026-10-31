# Textarea source reference

Textarea is accepted for the five-check `.58` x64 milestone. The extracted
catalog passed 172 Light/Dark states at 320 and 640 client widths, with four
physical pointer, keyboard, editing and focus sequences. The independent NuGet
consumer compiled and passed 48 native 150% Light/Dark size, rows, width and
error layouts without clipped editor, viewport or resize grip. Border paint
settled with animations disabled. ARM64 built and its consumer compiled;
runtime remains unverified. Older diagnostics below do not add milestone gates.

`BleeckerTextarea` is a
native `RichEditBox` with consumer-owned `Text`, `InputSize`, `Rows`, `IsError`
and nullable `IsInvalid`. The latter maps an explicit accessibility override to
`AutomationProperties.IsDataValidForForm`. The registered `TextareasFixture`
shows sizes, rows, empty/error, disabled/read-only, constrained and Arabic examples.

The native RichEdit automation peer also combines its attached validity with
`IsInvalid ?? IsError`. This keeps the Textarea invalid when a composing
`BleeckerField` clears its own error while the Textarea still has one, and
preserves an explicit `IsInvalid=false` override. The hidden x64 catalog check
at `artifacts/current/text-validity-v2` exercises both text editors and Field
composition without a visible or focused window. The later
`0.1.0-text-media-v3-local` NuGet package has the same Orchard DLL bytes and
passes fresh isolated x64 and ARM64 consumer builds. A fresh independent x64
package host also passes 17 hidden native-peer validity cases spanning Input,
Textarea and Field, including Textarea error, explicit override, Field precedence
and Field-error removal. Its report is in
`artifacts/current/text-media-v3-validity-x64-v2`; native ARM64 runtime remains
open. The separately built, extracted x64 catalog archive also passes the
hidden native-peer validity check; its Orchard DLL is not byte-identical to
the NuGet DLL.

## Native preview and boundaries

The x64 runs at `artifacts/textarea-live-readiness-runtime-320` and
`artifacts/textarea-live-readiness-runtime-640` pass all 172 source-sized native frames
and four editing sequences. They exercise physical Enter and selection
replacement, native undo, read-only selection and rejected editing, two-way
consumer binding, keyboard resize, template replacement, unload/remount and
trailing newlines. They also reject Ctrl+B/I/U in the preview-key phase, expose
plain editing actions through branded menus, paste text from a mixed text/HTML
clipboard without bold formatting, and restore the prior value with one undo.
There are 60 owned native key actions across the two widths. Each audit rejects
ten deliberate corruptions of frames, font/paint/line spacing, input ownership,
consumer binding, UIA text/read-only state and paste results.

The raw comparison covers all 172 whole-client frames: **1,001,348 differing
pixels**, absolute RGBA error 56,068,145, without masks or resampling. This is
not raster acceptance. All 125 unit tests, generated tokens and the existing
focused Input runtime pass. Eight external UIA snapshots cross the owned process boundary
and expose the native Edit role, name, document text, selection and read-only
state through TextPattern. RichEditBox does not expose ValuePattern here. The
legacy .NET client cannot query UIA validity property 30103; its report keeps
`valid=null`, and external validity acceptance stays open. The native attached
validity property is checked for each source state and explicit override.

Native edits publish LF line breaks. A bound WinUI multiline `TextBox` source
canonicalizes these to CR; the regression retains both raw strings and compares
their text with explicit newline normalization. Use
`Mode=TwoWay, UpdateSourceTrigger=PropertyChanged` when a consumer wants updates
on every edit. A single-line TextBox is an unsuitable multiline data model.

## User-input length and asynchronous clipboard ownership

`MaxLength` retains the inherited Windows contract: a positive value limits
user input and `0` is unlimited. This matches Orchard's other native editors;
it maps the browser's omitted `maxLength` attribute to the Windows default.
An HTML attribute of `maxLength=0` has different semantics. Programmatic `Text`
and consumer binding updates may exceed a positive limit and are preserved;
typing and paste must not add text beyond the current capacity. The pinned
WinUI `2.3.9` metadata (resolved by Windows App SDK `2.5.1`) defines this
zero-means-unlimited user-input property. Public Learn's available view is
older; the pinned package's XML and real runtime establish the versioned
contract used here.

`scripts/reference/capture-textarea-limits.cjs` records 56 fresh frozen-source
cases across both widths and themes, using the real clipboard, trusted keys,
consumer change records and one-step undo. It covers typed characters and Enter,
plain paste, CRLF normalization, UTF-16 surrogate boundaries, selection
replacement, full fields, programmatic overflow, replacing overflow, zero
insertion capacity with a selected suffix, live limits, unlimited and read-only.
The committed reference is `tests/Reference/TextareaLimits.json`. HTML constraint
validity and `aria-invalid` are distinct: exceeding a limit does not implicitly
set the forwarded accessibility attribute. Orchard keeps its explicit
`IsError` / `IsInvalid` consumer contract rather than inventing a form validator.

The original native paste path bypassed the editor's user-input limit because
it wrote directly through the document API. Its retained 640px baseline has
18 failures across both themes. The shared keyboard/menu paste path now
normalizes CRLF and CR to LF, computes capacity after the current selection,
truncates without splitting a surrogate pair and applies one native edit.
A native Select All range can include RichEdit's immutable final paragraph; it
adds no capacity because it is excluded from the public text. The native matrix
uses real Ctrl+A for full replacement. A full field with an empty selection
produces no edit. A selection can still
be deleted when capacity is zero, matching the browser. Read-only/disabled
guards remain active, and the caret follows the inserted text.

Clipboard text can arrive asynchronously. The insertion uses the current limit
after retrieval and rejects stale content, selection, template, disabled,
read-only or view ownership. Every load advances the view version, including
rapid remounts. Native delayed-provider checks hold the actual data request,
mutate the owning editor, release the data and await the exact operation started
by the real paste key. This distinguishes completed cancellation from an edit
that simply has not arrived yet. The final native runs pass all 56 length cases
and 36 delayed-provider cases across both widths/themes, with 188 owned key
actions including Select All and menu undo. Both audits reject twelve corruptions; all 129
unit tests and generated tokens pass. Both architectures build, with native
ARM64 execution still unverified.
The final original 172-state regression also passes, including four editing
sequences, eight external UIA/grip snapshots and 60 owned key actions.

Four physical menu-Paste invocations also pass limit, consumer binding, popup
closure and one-step undo checks. Read-only menu editing actions are disabled
and Copy stays enabled for a selection. The test opens the actual ContextFlyout,
then observes the menu click's exact native input-source press/release because
popup closure can remove its routed XAML release event. These are explicitly
native-source receipts. Context-menu opening gestures and the other menu
invocations, plus clipboard-provider failure reporting, still need separate
acceptance evidence. The registered fixture includes a 12-character editor.

Exact native paragraph spacing uses the SDK's point units: 15pt for a 20px line,
18pt for 24px. Document initialization waits until the native view has loaded.
Keep XAML's existing private font-face binding when configuring character format;
replacing its asset URI with the bare family name discards the custom collection.
The five-control `textarea-font` diagnostic compares both document APIs, the
custom control, a system font and TextBox. It runs in Light only and is not
theme acceptance. The ineffective process-private GDI font experiment was removed.

## Versioned preview artifacts

The current `0.1.0-3a0e8e1` package includes the subsequent shared-chart
early-focus correction and live catalog/package regressions. It is at
`artifacts/current/packages/Gaulatti.Orchard.0.1.0-3a0e8e1.nupkg`, SHA-256
`659e731326541f91203f78274792e4f2763fba59f5b2ba66c4abd2f8de8edb20`.
Both catalog architectures publish. A fresh independent x64 consumer passes
its complete Light/Dark runtime and six actual context-key phases; the same
package's ARM64 consumer builds, with native ARM64 execution unverified.
The regenerated full x64 catalog also passes all 150 phases in 467.3885367
seconds with 1,148 captures, including the 20 live Textarea phases and six
native editing keys. Current-state audits pass; the deleted earlier baseline's
capture-name and glyph-hash retention were not reverified.
The normal verifier uses fresh isolated source, feed and NuGet cache directories
and removes them after verification, including failures. Both consumer library
files match the packaged assembly. Current runnable ZIPs, hashes and scope are
in [the native checkpoint](native-validation-checkpoint.md).

### Historical packages removed in the local reset

The user-requested 2026-10-17 reset removed the older local packages and raw
evidence below. These paragraphs retain their original results and source
identities; see [artifact storage](artifacts.md) for availability limits.

The newer `0.1.0-textarea-commands` package contains library source
`751a3094ddef6f7b77961bb11610e5e28a2ae5c4`, including owned resize, paste limits
and constrained context commands. It is at
`artifacts/packages-textarea-commands/Gaulatti.Orchard.0.1.0-textarea-commands.nupkg`,
SHA-256 `5f9dd52a44cfa5910d92286636f744822ed1c76103a6d1af8b663675ef3782d1`.
The independent x64 consumer in `artifacts/package-consumer-textarea-commands-x64`
passes its full Light/Dark runtime and six real Apps-key context phases:
default, local-null disabled and ClearValue restored in each theme. The exact
published assembly matches the package; its PRI is merged into application
`resources.pri`, so the two resource-container hashes are recorded separately.
The consumer source is outside the repository and uses no ProjectReference.
Orchard starts uncached and its restored cache entry plus temporary source are
removed before launch. External SDK dependency caches are reused. The audit is
`artifacts/textarea-package-context-audit.json`. This package predates the
subsequent shared-chart early-focus correction; its source identity remains
explicit rather than claiming that later change.

Code source `c8ac17f91206f074a3da1337c2c11178fb9083f9` builds on x64 and ARM64.
All 6,286 resolved library methods, locals, handlers and exposed interfaces match
the runtime-tested x64 candidate in both committed builds. The library PRI is
byte-identical (`4fe2ad0f6dca01891750fae627e42f1d064fdf177b7124306484ce236e38e10a`).
The fresh x64 consumer runtime above predates this metadata-only rebuild; this
method/resource audit carries its evidence to the committed package rather than
claiming another runtime execution.

`artifacts/packages-textarea-readiness/Gaulatti.Orchard.0.1.0-textarea-readiness.nupkg`
contains the committed library and PRI, with repository commit checked explicitly.
Package SHA-256:
`f0bf46559c15a8b1a371857caf1a04990075e3789860d4d520888555b8105b01`.
A separate fresh ARM64 consumer builds against this final package; its cache is
removed. It is a cross-build, not native ARM64 runtime evidence.

The complete self-contained catalog directories are archived with every entry
hash checked: `artifacts/Orchard.Catalog.0.1.0-c8ac17f-x64.zip` (537 entries,
SHA-256 `5a4a212bb01521f14e7f1b933aa28c1d273e08606a008e7cf410006e626f8c1f`)
and `artifacts/Orchard.Catalog.0.1.0-c8ac17f-ARM64.zip` (533 entries,
SHA-256 `68869494d355ee8c5d2787242897792e51c6d0d24503a3359d6dc511d6a99bab`).
The private receipts are `artifacts/textarea-final-artifact-manifest.json` and
`artifacts/textarea-readiness-archives.json`. Push Actions remain disabled.

Readiness now comes from the loaded `ContentElement`, rather than a cached flag.
The independent consumer exposed a loaded control with a false cached flag after
reparenting, which skipped native writes. The regression now moves the original
packaged control into a visible host, waits for its own presentation, updates
read-only content and checks the exact native document and final newline. Its
fresh x64 build and full Light/Dark runtime pass after deleting the isolated cache
before launch (`artifacts/package-consumer-textarea-live-x64`). Initial stale
document and unrelated-pagination visibility failures remain retained.

The consumer's `IsReadOnly` dependency property stays stable while a synchronous
UI-thread transaction updates native text/formatting. The internal RichEdit guard
is restored before input can dispatch. Native read-only editing and external
provider behavior are exercised; transient UIA event behavior still needs coverage.
The source palette, fractional-alpha painter, premium transition, shadow clipping
and disabled pointer ownership follow Input's existing implementation. Input's
single-line source is unchanged.

The resize grip adds Up/Down resizing for keyboard access, a deliberate Windows
accessibility extension to the source's vertical pointer grip. Pointer drag,
minimum clamping/cursor, disabled/read-only resize, live theme/customization,
remaining form constraints and complete menu invocation still need verification.
Immediate Enter, requested trailing newlines, read-only updates after remount and
the caret after paste are checked explicitly. Delayed clipboard-provider
races/cancellation and menu invocation need further runtime evidence.
The direct native `ITextRange.Paste` experiment stowed-crashed at the clipboard
boundary and is retained as a failure. The implementation instead retrieves
plain text asynchronously and applies a selection edit after checking content,
selection, view, enabled and read-only ownership. Complete-catalog integration,
full rendered review, enabled motion, Windows scaling, component theme/automation
behavior and native ARM64 remain open. Cross-builds do not establish native
ARM64 runtime acceptance.

## Frozen browser contract

The collector mounts the frozen Bleecker `Textarea` at commit
`a86059013cd59684ce452849278ea81e734f2066`, source SHA-256
`b523ba0962a02492a5e5954957eede4e1ad683ee7ce7ac21c04097325eb82082`.
It verifies installed dependency versions/integrities against the frozen lock,
uses the frozen stylesheet and Orchard's Encode Sans files, and records their
hashes. Edge `155.0.4283.45`, React `19.2.4`, esbuild `0.25.12` and Playwright
`1.62.1` are pinned. The committed `tests/Reference/Textarea.json` records 172
freshly mounted frames: both themes, 320/640px viewports, three sizes, rows 1/4/10,
empty, error, explicit `aria-invalid=false`, disabled, read-only, Arabic direction,
long overflow, and normal/hover/focus. Disabled controls have no focus case.
Transitions finish before endpoint measurement. These are endpoint references,
not enabled-motion timeline evidence. PNGs remain in `artifacts/textarea-browser`;
their hashes are retained in the committed reference.

Measured browser geometry, including rows and border-box sizing:

| Recipe | Default four-row height | Minimum | Font / line height | Horizontal / vertical padding |
| --- | ---: | ---: | --- | --- |
| Small | 98px | 96px | 14 / 20px | 12 / 8px |
| Medium | 112px | 112px | 14 / 20px | 16 / 12px |
| Large | 128px | 128px | 16 / 24px | 16 / 12px |

Medium ten rows is 226px; Large ten rows is 266px. A one-row Small is 96px.
Using only the minimum-height class would miss Small's default intrinsic height.
The dark `deep-sea` token resolves to `#182533`; its error focus border resolves
to accent blue `#5BA3F5` because the dark focus selector wins the cascade.
Error hover uses the hover border. An explicit `aria-invalid=false` overrides
the derived accessibility state while retaining the error visual. Disabled
opacity is 0.6 on the entire textarea, and its background alpha is 0.65.

Four real browser input sequences cover both widths and themes. They enter two
lines with Enter, replace selection and undo to restore both lines, select
read-only text while rejecting edits, and drag the actual resize grip. Vertical
drag increases height from 112 to 160px, preserves width, and shrinks only to
the 112px source minimum. Every recorded pointer, key and input event is trusted.

Native acceptance must carry this contract through the public control, dependency
properties, consumer binding, retained native editor, template, measurement,
scrolling, paint/motion/shadow, resize interaction, registered catalog fixture,
external UIA client and independent package consumer. Check normal editing,
selection, clipboard, undo/redo, wrapping and overflow, read-only versus disabled,
live size/row/error/theme updates, template replacement and unload/remount.
Preserve the explicit accessibility override. Verify source geometry and raw
raster comparisons without masks or resampling, actual Windows scaling, enabled
and reduced motion, component theme/automation behavior, x64 and native ARM64. Source geometry or
a successful build alone does not satisfy these criteria.

## Native context commands and consumer ownership

The default menu offers Undo, Redo, Cut, Copy, Paste and Select all. Apps,
Shift+F10 and right click open it through the real context-request path. Copy
and Cut export plain text only; Paste uses the same asynchronous ownership and
length checks as keyboard paste. Read-only editors allow Copy and Select all
while disabling the four editing commands. Commands restore focus to the editor.
Windows clipboard text may contain CRLF; reference comparisons retain the raw
clipboard and explicitly normalize line breaks to LF.

The default `ContextFlyout` property is unset. Orchard handles an unhandled
`ContextRequested` event and opens its branded menu at the pointer or selection
edge, within the owner's XAML root. Set a local `ContextFlyout=null` to disable
this default context menu, or supply/bind a custom flyout to use normal WinUI
dispatch. `ClearValue(UIElement.ContextFlyoutProperty)` restores Orchard's
default behavior. Consumer bindings remain attached. `SelectionFlyout` exposes
the default six-command menu; it can also be customized independently.

The standalone Textarea style deliberately omits the stock RichEditBox context
flyout. In pinned WinUI `2.3.9`, its automatic MenuFlyout dispatch and both
positioned `ShowAt` overloads create a separate popup HWND even when root
constraint is requested. Orchard uses a layout anchor with the unpositioned
overload. The keyboard anchor follows the selection's trailing edge in LTR and
leading edge in RTL. Template replacement and unload close the owned menu.
An explicit null style setter was rejected: clearing a local override throws
`E_FAIL`, reproduced in a plain RichEditBox with the same setter. Leaving the
property unset avoids that failure and preserves consumer reset semantics.
The pinned package's `generic.xaml` establishes the inherited stock flyout;
[WinUI's MenuFlyout implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/dxaml/lib/MenuFlyout_Partial.cpp)
and [FlyoutBase implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/dxaml/lib/FlyoutBase_partial.cpp)
explain the overload distinction. Current upstream code is supporting context;
the actual pinned runtime is the verification boundary.

`scripts/reference/capture-textarea-commands.cjs` records 36 frozen-source
states in `tests/Reference/TextareaCommands.json`, across both widths/themes:
Select all, Copy, Cut, Undo, Redo, Paste and read-only Copy/Cut/Paste. The
collector seeds its own clipboard, uses trusted key events and records clipboard
formats and screenshot hashes. Native checks use actual Apps/Shift+F10/right
click input and native source press/release receipts, followed by actual menu
item presses. A popup can close before XAML routes release; these receipts
explicitly claim the WinUI input-source boundary, not retained routed release.
Run `scripts/smoke-catalog.ps1 -Component textarea-commands` at 320x600 and
640x600, then `scripts/verify-textarea-commands.mjs` and its negative audit.
The final runs at `artifacts/textarea-commands-final-runtime-320` and
`artifacts/textarea-commands-final-runtime-640` pass all 36 command states and
28 ownership/reset phases. Their audits reject 15 deliberate corruptions per
width, including missing input receipts and missing ownership verification.
The affected regression reruns retain all 172 source-sized frames, four editing
sequences, eight external UIA snapshots and 60 native key actions. The separate
length suite retains 56 source cases, 36 delayed-provider cases, four real menu
paste checks and 188 native key actions. All 130 unit tests, generated tokens,
and x64/ARM64 builds pass. Native ARM64 execution remains pending. The latest
raw comparison (`artifacts/textarea-commands-regression-pixels.json`) reports
1,001,652 differing pixels and absolute RGBA error 56,022,878 across 172 complete
frames, with no masks or resampling; visual parity is still open.
Menu screenshots are evidence of the native result, not raw source raster
acceptance. Clipboard-provider errors, remaining customization, enabled motion,
scaling, component theme/automation behavior and native ARM64 still require verification.

## Live catalog integration

`TextareasFixture.VerifyAsync` keeps the original XAML editor mounted while
changing Small/Medium/Large recipes and rows 1/4/10. Its measured heights are
96, 98, 266, 226 and 112px, with native paragraph spacing updated to 15 or 18pt.
Real Enter and character keys cross the original two-way binding. A subsequent
read-only consumer update reaches the native document, preserves the trailing
newline and native read-only guard, and rejects a physical edit.

The same editor switches its owner between Dark and Light, checking native
brush color and opacity separately against the frozen reference. WinUI stores
brush opacity at single precision despite projecting a double property; the
runtime check uses the exact native representations of 0.15 and 0.4. Explicit
`IsInvalid=false` overrides a live error's accessibility state; clearing it
restores error-derived invalidity. Text, properties and owner theme are restored
after each run.

Run `scripts/smoke-catalog.ps1 -Component textarea-catalog`. The standalone
`artifacts/textarea-catalog-live-verified-runtime` passes 20 live phases and six
owned native keys across Light/Dark. `scripts/verify-textarea-catalog.mjs`
checks source geometry/paint and the native input reports; its negative audit
rejects 12 corrupted results. The complete catalog sequence now invokes this
fixture after Input and before Keyboard shortcuts. The separate integration
audit additionally checks prior capture retention, native input ownership,
chart glyph hashes and the full-sequence deadline. Standalone success alone
does not establish that integration boundary.

The complete x64 run at `artifacts/textarea-catalog-integrated-fixed-full`
passes in 469.1074913 seconds under the unchanged 480-second watchdog, with
150 completed phases. All 1,146 previous PNG names are retained, plus the two
Textarea fixtures (1,148 PNGs). The audit verifies 2,024 owned pointer moves and
presentation gates, 1,764 owned key actions, 86 native clicks and 174 retained
Radial Bar/Sunburst glyph geometry hashes. The 20 live Textarea phases and six
native editing keys pass within that sequence. These checks establish catalog
integration; exact raster and platform/accessibility acceptance remain open.

## Pointer resize contract

`scripts/reference/capture-textarea-resize.cjs` collects a separate 72-case
trusted-input browser reference in `tests/Reference/TextareaResize.json`:
320/640px, Light/Dark, Small/Medium/Large, editable/read-only/disabled and LTR/RTL.
Each case is freshly mounted. The browser's disabled and read-only fields retain
their resize affordance. RTL places the grip at the bottom left. A 48px vertical
drag increases height by exactly 48px without changing width, text, disabled
state, read-only state or textarea focus. A subsequent 240px upward drag clamps
to the recipe minimum. Reversing 20px while still held retains that minimum:
clamping must not replace the original pointer anchor.

The native control uses a keyboard-accessible grip and an enabled XAML-root
pointer capture so that the editor itself stays disabled throughout a disabled
resize. The grip consumes its leaf press before RichEdit can focus or select
text. Pointer observation preserves the existing rounded hit, clipping, native
window ownership and occlusion checks; only the grip region is exempt from the
disabled body cursor. The vertical resize cursor follows Chromium's
`EResize::kVertical` / `NorthSouthResizeCursor` implementation in
[`EventHandler`](https://github.com/chromium/chromium/blob/main/third_party/blink/renderer/core/input/event_handler.cc).
The browser reference's computed CSS `cursor` is the text-area style and does
not expose that UA resize-corner cursor.

Run the dedicated native check with `scripts/smoke-catalog.ps1 -Component
textarea-resize -ClientSize 640,600` (and 320,600), using an absolute executable
and output path. The test translates the field to y=280 during drag so the
upward overshoot stays within the owned HWND. Horizontal movement points inward
for LTR and rightward for RTL; the browser's positive LTR movement leaves its
viewport. Both signs must leave width unchanged. Final captures restore y=20.
`scripts/verify-textarea-resize.mjs` checks every measured case against the
frozen source. This does not establish raster, motion, accessibility or complete
Textarea acceptance.

The 72 native cases pass with real routed pointer receipts, root capture and
actual Windows cursor checks before and during drag. Ten corrupted reports per
width are rejected by `scripts/test-textarea-resize-audit.mjs`. The separate
`textarea-resize-ownership` mode passes twelve Light/Dark phases: overlay
occlusion, clipping, stationary unload, live RTL changes and cursor restoration.
Disabled Large RTL cases additionally verify cancellation on capture loss,
template replacement and unload. Existing Input runtime and all 126 unit tests
pass; both architectures cross-build. Native ARM64 runtime is still pending.
The explicit grip peer retains the resize name, focusability and thumb role in
eight independent UIA snapshots across widths, themes and editable/read-only
states. The final 172-state editing regression passes with 60 owned key actions;
its audits reject eleven corruptions per width, including a lost resize provider.

The existing Input implementation already matches the measured color cascade,
fractional alpha, source shadow and transition endpoints. Native reuse must keep
the existing single-line Input contract and regression evidence intact; its
centered line layout cannot stand in for multiline top alignment and line height.
There is no backend or telemetry change. The public wiki remote remains
unavailable, so this contract is maintained in repository documentation.
