# Input editing preview

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
