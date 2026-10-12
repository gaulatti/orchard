# Input editing preview

`BleeckerInput` derives from WinUI TextBox and supplies a branded template with
consumer-owned text and decorative StartIcon/EndIcon content. It is an incomplete
preview against the [pinned reference](input-reference.md), not accepted parity.

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

The template owns background, border, focus ring and placeholder colors; it
omits the Windows underline and clear button. The focus palette includes the
pinned dark error input's blue border/ring. Windows contrast resources provide
paired system colors, but actual contrast acceptance remains outstanding.
Existing BleeckerTextBoxStyle consumers, especially multiline Summary, remain
on their previous preview until the applicable input/textarea contracts pass.

Remaining work includes exact glyph/baseline and scaling comparison, reference
hover/disabled precedence, error semantics, shadows, motion/reduced motion,
live theme and brush updates, broader editing/clipboard
regressions, physical keyboard/pointer, Narrator and ARM64 runtime verification.
The catalog fixture covers all sizes and representative empty, populated,
adorned, error, disabled, read-only and constrained multilingual examples.

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
The registered fixture includes this combination. Runtime regression coverage
also changes each size with adornments removed and restored, and checks that
presentation changes retain native text selection.
These checks pass in both themes at `artifacts/smoke-input-states`; the retained
captures include the disabled invalid example. x64 publish and ARM64 build pass.
