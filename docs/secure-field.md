# Secure Field native preview

`BleeckerSecureField` is a reusable password input, with a fictional local
catalog fixture. It provides no authentication, credential storage or network
service. Its contract comes from Sabella commit
[`f5ac479`](https://github.com/gaulatti/sabella/blob/f5ac479bfceaa7eab23c1716ac47420ac53dae25/Sources/Sabella/Controls.swift).
`tests/Reference/SecureField.json` freezes the explicit values and the complete
Controls/Tokens source hashes. `capture-secure-field.cjs` verifies those source
expressions before recording them. No Apple-rendered reference is claimed.

Bind `Password` using `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`, or
handle `PasswordChanged`. The caller owns the value. `PlaceholderText` appears
while empty. Reveal begins concealed and persists until toggled; toggling
preserves the value and does not raise a password-change event. `IsRevealed` is
the observable Boolean state; setting it also updates the native editor and
action name. The native Peek mode is not part of this contract. Set a meaningful
`AutomationProperties.Name`, or associate a `BleeckerField` label. The composite
forwards name and label to its native editor.
The composite exposes a Group peer for Field descriptions and validation state;
the native editor owns its protected password/value peer. Clearing Field help
or errors must remove the corresponding relationships and invalid state.

WinUI 2.3.9 seals `PasswordBox`, so the component contains a native PasswordBox
and an ordinary keyboard-accessible `BleeckerIconButton`. There is one password
editor; concealment uses its native mode. The component does not copy plaintext
into a TextBox or simulate bullets. Normal keyboard, IME, selection and protected
automation behavior remain owned by the native editor. The reveal action is
named “Show password” or “Hide password.” Disabled controls disable both parts.
Sabella swaps SecureField for TextField when revealed. Windows retains the
PasswordBox in Visible mode to preserve editor identity and its native password,
clipboard and automation contract. Full editing/selection parity across that
platform mapping still needs desktop verification.

The explicit source dimensions are Encode Sans 14, horizontal inset 14, minimum
height 40 and radius 7. Shared theme resources supply the source card, primary
text, secondary text and border colors. The light surface has the source's
deep-sea shadow, opacity .055, radius 14 and vertical offset 5; dark has no shadow.
Windows rounded corners and composition blur are native mappings, without an
Apple raster-equivalence claim. The source leaves HStack spacing, SF Symbol
bounds and button target size implicit. Windows uses the established 32-DIP
small icon button, a 16-DIP eye glyph and 8-DIP spacing. A native editor focus
underline and the existing icon-button focus cue identify keyboard focus.

The background regression harness exercises empty/concealed/revealed/disabled
states, binding in both directions, clearing, repeated reveal, template
replacement, unload/reload, RTL and explicit 28-DIP enlarged text. It uses three
widths and both themes. Hidden UIA checks must refuse password value reads.
Reports contain checks and lengths, never password values; captured revealed
states use only fixed fictional samples. `scripts/verify-secure-field.ps1` runs
the same public-API fixture in the catalog host and the isolated NuGet consumer,
with guards against window visibility, activation, focus, input and native Popups.
Use the same command with `-Catalog` for the real `SecureFieldsFixture`. Its
native value event must update the length counter, reveal must preserve the
counter, and the actual Clear button must empty both native value and counter
without resetting reveal state. The fixture also renders disabled, constrained
and enlarged-font examples. All values are fictional and remain local.

The catalog host and fresh `0.1.0-secure-field-v2-local` NuGet consumer each pass
30 hidden cases and six component captures, with 28 corrupted reports rejected
per audit. Nine windowless font tests and all 335 portable tests pass. These are
background component checks, with no claim of physical typing or Apple parity.
The real catalog adds two theme cases, six native action invocations and four
captures; its auditor rejects 15 corrupt reports. The catalog root also starts
with both required fictional Monitoring and Editorial demos.

The first library build failed the disabled visual regression: its native
children were disabled, but the intended .55 content opacity stayed at 1.
The component now applies its Normal/Disabled visual state on template creation
and enabled-state changes. Reenabling must restore opacity 1. The failing
library identity and report are retained alongside the corrected evidence.

Physical typing, pointer and keyboard traversal, focus transitions, actual OS
text scaling, complete visual acceptance and ARM64 runtime remain unverified.
The component remains a preview. Narrator and OS contrast campaigns are excluded
by [the component-library scope](library-scope.md). Both required fictional-data
Monitoring and Editorial demos remain in scope.
