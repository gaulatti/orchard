# OTP Input

`BleeckerOtpInput` ports the controlled `OtpInput` at Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`. It is a reusable editor for fictional
local catalog values; it provides no authentication, delivery or validation service.

OTP Input is accepted for the 2026-10-29 x64 milestone. The exact published
`.45` package DLL passed Light and Dark source states and catalog examples,
physical pointer/keyboard/focus behavior in an independent consumer, ten
native 150% layouts with no clipped slots or editors, reduced-motion focus
ring endpoints while animations were disabled, and a fresh independent NuGet
consumer compile and runtime. The earlier Windows clipboard route also passed.
See `artifacts/current/context-menu-native-candidate/otp-input-*-v45.json`.
ARM64 compiles but its runtime remains unverified. Raster/pixel parity and
external UI Automation input are not gates for this milestone.

The original implementation contract requires caller values and source requests to cross native
editors correctly; ASCII-only edit filtering, deletion compaction and whole-value
paste; native protected password editors; dynamic length/mode/disabled state;
40×48 slots with 8-DIP spacing, theme paint and constrained overflow; registered
catalog examples; and fresh x64/ARM64 package consumers. Source browser evidence
does not establish Windows keyboard, clipboard, focus, scaling or raster parity.

```csharp
var input = new BleeckerOtpInput { Value = "482", Length = 6 };
input.ValueRequested += (_, request) => input.Value = request.Value;
```

`Value` remains caller-owned. A request never assigns it automatically, and an
absent or rejecting handler restores the displayed caller value. Initial values
are not sanitized: slots display the first `Length` UTF-16 characters, trimming
each character as JavaScript does. A native edit keeps its last ASCII digit and
joins the original slots with JavaScript whitespace removed. Deletion therefore
compacts later characters; editing an empty later slot does not invent padding.
Pasting filters ASCII digits and replaces the entire value from the first slot.
`Length` defaults to six and accepts nonnegative integers, including zero; negative
Windows values fail explicitly. `Spacing` defaults to eight and supports native
style customization corresponding to the source container's gap classes.

`IsSecure` uses actual PasswordBox editors, with hidden reveal controls and native
protected value metadata. Ordinary slots use native TextBox editors. The group
has the source name and each slot is named `Digit N`. Caller validation is composed
through `BleeckerField`, as demonstrated in `OtpInputsFixture.xaml`.
Secure mode retains the Windows mask glyph. Its native editing viewport is
centered using that glyph's measured font advance; no overlay renders passwords.
Rejected values restore after the native edit callback has finished, so the
native engine cannot overwrite the caller's retained value.

Source Left/Right moves to previous/next logical slot, including RTL. Backspace
clears a filled slot, or clears and focuses the previous slot when empty. Focus
selects the slot contents. Paste focuses the first remaining empty slot, or the
last slot when full. Background verification never moves native desktop focus.
The [published v7 x64 foreground checkpoint](evidence/otp-input-v7-physical-v1/checkpoint.json)
now verifies focused pointer digit entry, Backspace and Left Arrow movement,
caller rejection, secure masking and a real Windows Ctrl+C/Ctrl+V whole-value
paste that filters a non-digit. This focused route does not cover the complete
input matrix, independent NuGet consumer input or native ARM64 execution.
The background catalog driver edits the real native editor properties. WinUI's
text pattern implementation is native and requires an external UI Automation
client; this driver does not verify that external input path. See the
[TextBoxAutomationPeer contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.textboxautomationpeer?view=windows-app-sdk-2.0).

The pinned source has fixed slot widths and visible overflow under narrow
constraints. The native panel retains those slots instead of wrapping or shrinking
them. Native typography, secure glyphs/caret and complete visual/scaling/motion
acceptance remain under implementation. The source reference records 60 browser
states, 12 interaction histories and ten captures from the unchanged component.
The source and ten captures recapture byte for byte. The hidden Windows source
host passes all 60 states and 32 controlled/lifecycle cases. Independent pixel
checks cover 312 slot surfaces and 148 centered native glyphs. The capture gate
retains raw bitmaps and verifies its viewport crop byte for byte, including RTL
overflow. Earlier failed controlled edits and expanded viewport captures are
retained and rejected by the regression auditor.
The fresh `0.1.0-otp-input-v1-local` NuGet host passes the same 60 states and 32
cases, with the same surface and centered-glyph probes. x64/ARM64 catalog and
isolated consumer builds pass. The actual catalog passes both themes and eight
native editor changes across its seven examples. All 353 portable tests pass.
ARM64 runtime and the unverified native interaction routes above remain open.

The foreground checkpoint uses the extracted published catalog with the exact
released NuGet library DLL (`839f520e…`). The accepted fictional value visibly
changed `482` → `4827` → `482`, then a clipboard paste of `73a9` yielded `739`.
The rejecting example retained `482`; the secure example added a masked digit.
No new library implementation or fixture was needed. The catalog exited after
the test, but its exit code was not captured. Full physical interaction,
scaling, visual and ARM64 acceptance remain open.

The [fresh-cache v7 independent consumer checkpoint](evidence/otp-input-v7-fresh-consumer-v1/checkpoint.json)
reuses the existing hidden package fixture with the exact released Orchard DLL.
It passes 60 pinned source states, 32 controlled/lifecycle cases, 60 captures,
1,236 isolation guards, and 5.76 million audited pixels; the process exits
zero. This verifies current-package public API, resources, theme paint and
caller-owned edit behavior in a separate package-reference application. Its
hidden probe does not send hardware input or use the Windows clipboard, so the
catalog foreground result above and this consumer result retain separate scopes.

The [published v7 independent-consumer physical checkpoint](evidence/otp-input-v7-package-physical-v1/checkpoint.json)
now closes a focused package-input gap. A separate visible consumer, built with
the exact released NuGet DLL, passed physical fourth-slot entry, empty-slot
Backspace compaction, caller rejection/restoration and native protected secure
entry in both Light and Dark. Each foreground process exited zero; the normal
package-consumer runtime also passed after the fixture was added. The ARM64
consumer compiles with no warnings, but has not run on native ARM64 hardware.
The Light/Dark captures show the final fictional values without clipped slots;
they do not prove full visual parity. Other input routes, scaling, motion,
complete historical visual parity and native ARM64 execution remained open at
that earlier checkpoint; the current x64 milestone acceptance is recorded above.
