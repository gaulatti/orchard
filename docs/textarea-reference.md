# Textarea source reference

Textarea remains pending in the fixed parity inventory. This checkpoint establishes
the actual browser contract before native implementation; it does not accept a
Windows control or increase the completed component count.

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
