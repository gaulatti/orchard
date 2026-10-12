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

The template owns background, border, focus ring, shadow and placeholder colors; it
omits the Windows underline and clear button. The focus palette includes the
pinned dark error input's blue border/ring. Windows contrast resources provide
paired system colors, but actual contrast acceptance remains outstanding.
Existing BleeckerTextBoxStyle consumers, especially multiline Summary, remain
on their previous preview until the applicable input/textarea contracts pass.

Remaining work includes exact glyph/baseline and scaling comparison, disabled
hover/cursor behavior, error semantics, shadow raster comparison and motion,
live brush updates, broader editing/clipboard
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
Both themes use light-sand at 65% alpha as the disabled background, separately
from the editor's .6 opacity. Dark therefore uses `#A60D1821`, not the ordinary
opaque deep-sea fill. The rendered pinned reference confirms this cascade.
The registered fixture includes this combination. Runtime regression coverage
also changes each size with adornments removed and restored, and checks that
presentation changes retain native text selection.
These checks pass in both themes at `artifacts/smoke-input-states`; the retained
captures include the disabled invalid example. x64 publish and ARM64 build pass.

The shadow preview uses the pinned rest `0 1px 2px rgba(26,55,77,.025)` and
enabled hover `0 3px 10px -8px rgba(26,55,77,.25)` values. A Composition
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
The shadow transition remains separate unfinished work; this implementation
does not establish complete Input motion parity.

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
fixture was visually inspected. All 13 tests and x64 publish pass. Disabled
hover still needs its reference border/shadow behavior and not-allowed cursor;
the background correction does not close the full disabled-state contract.
