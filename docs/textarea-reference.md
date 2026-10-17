# Textarea source reference

Textarea is in progress in the fixed parity inventory. `BleeckerTextarea` is a
native `RichEditBox` with consumer-owned `Text`, `InputSize`, `Rows`, `IsError`
and nullable `IsInvalid`. The latter maps an explicit accessibility override to
`AutomationProperties.IsDataValidForForm`. The registered `TextareasFixture`
shows sizes, rows, empty/error, disabled/read-only, constrained and Arabic examples.
No component acceptance is claimed.

## Native preview and boundaries

The x64 runs at `artifacts/textarea-unicode-paste-runtime-320` and
`artifacts/textarea-unicode-paste-runtime-640` pass all 172 source-sized native frames
and four editing sequences. They exercise physical Enter and selection
replacement, native undo, read-only selection and rejected editing, two-way
consumer binding, keyboard resize, template replacement, unload/remount and
trailing newlines. They also reject Ctrl+B/I/U in the preview-key phase, expose
plain editing actions through branded menus, paste text from a mixed text/HTML
clipboard without bold formatting, and restore the prior value with one undo.
There are 60 owned native key actions across the two widths. Each audit rejects
ten deliberate corruptions of frames, font/paint/line spacing, input ownership,
consumer binding, UIA text/read-only state and paste results.

The raw comparison covers all 172 whole-client frames: **1,001,057 differing
pixels**, absolute RGBA error 56,028,527, without masks or resampling. This is
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

Exact native paragraph spacing uses the SDK's point units: 15pt for a 20px line,
18pt for 24px. Document initialization waits until the native view has loaded.
Keep XAML's existing private font-face binding when configuring character format;
replacing its asset URI with the bare family name discards the custom collection.
The five-control `textarea-font` diagnostic compares both document APIs, the
custom control, a system font and TextBox. It runs in Light only and is not
theme acceptance. The ineffective process-private GDI font experiment was removed.

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
Delayed clipboard-provider races/cancellation, paste caret positioning, menu
invocation and the fresh package-consumer fixture need further runtime evidence.
The direct native `ITextRange.Paste` experiment stowed-crashed at the clipboard
boundary and is retained as a failure. The implementation instead retrieves
plain text asynchronously and applies a selection edit after checking content,
selection, view, enabled and read-only ownership. Complete-catalog integration, exact whole-frame
raster equality, enabled motion, Windows scaling/contrast, Narrator and native
ARM64 remain open. Cross-builds do not establish native ARM64 runtime acceptance.

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
and reduced motion, contrast, Narrator, x64 and native ARM64. Source geometry or
a successful build alone does not satisfy these criteria.

The existing Input implementation already matches the measured color cascade,
fractional alpha, source shadow and transition endpoints. Native reuse must keep
the existing single-line Input contract and regression evidence intact; its
centered line layout cannot stand in for multiline top alignment and line height.
There is no backend or telemetry change. The public wiki remote remains
unavailable, so this contract is maintained in repository documentation.
