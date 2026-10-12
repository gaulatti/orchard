# Input reference and acceptance

Reference: Bleecker `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/input.tsx` and `stories/components/Input.stories.tsx`.
`BleeckerInput` is an incomplete implementation; neither it nor the existing
`BleeckerTextBoxStyle` has full visual acceptance for this contract.

## Geometry and typography

| Size | Height | Horizontal text padding | Font size |
| --- | --- | --- | --- |
| Small | 36px | 12px | 13px |
| Medium (default) | 40px | 14px | 14px |
| Large | 44px | 16px | 15px |

The rendered Default story confirms Encode Sans regular, 14px / 20px line height,
zero vertical padding, a 1px border and 7px radius. Its 320px width belongs to the
story container; the component fills its consumer's width. The local three-size
audit fixture additionally measures Small at 13px/19.5px and Large at
15px/22.5px. All three render the listed heights/padding, 1px borders and 7px
radii. Full native raster and scaling equivalence remains unverified; the
100% baseline checkpoint below covers the measured Latin sample.

### Typography comparison at 100% display scale

The October 12 native/browser audit uses identical `Harbor update` text in all
three sizes. Dark captures are `artifacts/input-baseline-reference-dark.jpg`
and `artifacts/input-baseline-native-dark.jpg`. Foreground ink begins at
14/16/18px and ends at 95/104/112px from the left border in both renderers.
These bounds include glyph side bearings; they are not padding measurements.

Comparing integer vertical translations over the interior text pixels gives
the lowest squared error with no translation for Small/Large, but with native
Medium sampled one pixel upward. Equivalently, the native Medium text needs
to move down to align with the reference. A second `Agjpqy 0123456789` sample
confirms the same direction. The measurements are retained in
`artifacts/input-baseline-dark-alignment.json` and
`artifacts/input-baseline-medium-corpus-alignment.json`. This is diagnostic
alignment evidence, not exact raster acceptance: JPEG compression and the
renderers' antialiasing remain different.

Placeholder text is a separate issue. The identical-text filled/placeholder
captures from `artifacts/smoke-input-typography` show the native placeholder
one pixel below entered text at all three sizes. Against the browser placeholder,
Small/Large are one pixel low and Medium aligns without translation. See
`artifacts/input-placeholder-dark-alignment.json`. The element captures reproduce
the filled-text offsets from the actual-window captures, but do not establish
shadow or border compositing parity.

The focused Input smoke now emits `input-typography-{theme}.json`, plus filled
and placeholder captures for both themes. It records the native character-edge
rectangle, editor origin/height and placeholder origin/height/baseline using
the pinned WinUI APIs, and checks that the line fits its viewport. It restores
the fixture's original text and placeholders afterward. These checks expose
geometry and clipping; they deliberately do not assert that an uncorrected
baseline matches Bleecker. This initial audit identified work needed before
typography acceptance; the correction checkpoint below records subsequent work.

The diagnostics checkpoint passes the focused light/dark runtime suite at
`artifacts/smoke-input-typography-verified`, all 13 unit/coverage tests, x64
publish and ARM64 compilation. This change is confined to catalog verification
and documentation; it does not change the library or packaged control behavior.

### Baseline correction checkpoint

The current control centers a separately measured font line box and aligns its
baseline to a device pixel. It compensates for the different baseline rounding
observed in the pinned RichEdit editor and TextBlock placeholder. The invisible
font measurement remains available when the placeholder is collapsed. Corrections
apply only to the text presenters; borders, rings, icons and shadows retain their
geometry. A one-frame rendering callback runs after font, size, template or root
changes and detaches on unload. Other fonts, fallback scripts and scaling still
need verification before treating this rounding model as generally accepted.

The catalog embeds `Reference/InputTypography.json`: normalized text-row profiles
from the pinned browser's dark, 100% `Harbor update` captures, covering all three
sizes and both entered/placeholder text. The check renders the real fixture,
locates each input, normalizes foreground/background contrast and compares row
profiles at vertical offsets -2 through +2. It requires the best match at zero
offset and rejects blank text captures. This measures vertical alignment, not
complete glyph, color, shadow or component equivalence. The original native
captures fail this comparison for Medium entered text and Small/Large placeholders.

All 12 static size/state/theme comparisons and 12 live size-change comparisons
pass at 100% in `artifacts/smoke-input-baseline-final`. Entered and placeholder
text now align best with the browser without a vertical translation. The fixture
first lays out visible placeholders before reading their geometry; this corrects
the earlier diagnostic's zero/stale measurements for initially populated inputs.
At other display scales the retained browser profile is explicitly reported as
unverified, rather than being used as proof for an unmeasured scale. Actual-window
placeholder confirmation, other display/text scales, fallback scripts and full
glyph raster parity remain open.

The actual published window confirms zero-offset entered-text alignment for
all three sizes in `artifacts/input-baseline-corrected-native-dark.jpg` and
`artifacts/input-baseline-corrected-native-alignment.json`. Physical typing into
Medium updates the native editor and its external UI Automation value. The full
reduced-motion catalog suite passes at `artifacts/smoke-input-baseline-integration`;
all 13 unit/coverage tests, x64 publish and ARM64 compilation pass.
The independent x64 package consumer passes at
`artifacts/package-consumer-input-baseline`, verifying resources, fonts, both
themes and a native action. Package SHA-256:
`777c053ca0111f4481457f4bb4775942673ce2595b2aa925ca42e7dafbb1bdb4`.

Start/end adornments are consumer-owned decorative content, centered vertically,
12px from their corresponding outer edge. Each occupied side gets a 40px text
inset. WithIcons renders 16px Search and User icons; that size belongs to the
story's supplied icons. The wrappers disable pointer events. They are not
additional input actions or keyboard stops.

## Rendered appearance

Default, WithIcons and Error were inspected in the pinned local Storybook.
These values distinguish observed medium-size states from source-only coverage.

| Surface | Light | Dark |
| --- | --- | --- |
| Background | white | `#182533` |
| Text | `#2D2D2D` | `#F0F4F8` |
| Placeholder | secondary `#595959` at 55% | `#B8C5D6` |
| Rest border | sand at 40% | white at 15% |
| Error rest border | `#A65D57` | `#D47B75` |
| Normal focus border | sea at 70% | accent blue |
| Error focus border | `#A65D57` | `#5BA3F5` |
| Normal focus ring | sea at 10%, 2px | accent blue at 12%, 2px |
| Error focus ring | terracotta at 15%, 2px | accent blue at 12%, 2px |

Dark focus rules override the error border/ring. Preserve the actual cascade;
do not assume error styling wins in every state. Focus after pointer click also
retains the hover shadow. The source rest shadow is `0 1px 2px rgba(26,55,77,.025)`;
hover is `0 3px 10px -8px rgba(26,55,77,.25)`. Hover borders are sand at 70% and
white at 25% respectively. Isolated rendered Error checks confirm that hover
overrides the terracotta border in both themes when the input is unfocused.

The source transitions background, border, shadow and opacity over the 190ms
control token with cubic-bezier(.22,1,.36,1). This host's reduced-motion setting
yielded a computed duration of 0.00001s. That observation does not verify the
enabled-motion curve. Disabled source styling applies light-sand/65 background,
opacity .6 and not-allowed cursor to the input itself, leaving sibling adornments
outside that opacity.

A local Storybook audit fixture imports the unchanged pinned Input and renders
`disabled`, then `disabled error`. Computed styles confirm light-sand at 65%
alpha in both appearances: `#F9F6F2` in light and `#0D1821` in dark, with .6
input opacity applied separately. The dark disabled rule wins over the ordinary
opaque deep-sea background. Disabled error retains terracotta at rest. Pointer
hover still changes both disabled cases to the ordinary hover border and shadow
in both themes, while focus remains false and the cursor is `not-allowed`.
Orchard's disabled background, hover border/shadow and system cursor now follow
this observed behavior. WinUI routes disabled TextBox pointer events to enclosing
ScrollViewer chrome and omits disabled editors from host-coordinate hit tests.
The implementation observes root events and the current physical pointer,
checks bounds, ancestor clipping, scroll viewports and sibling occlusion, and
refreshes stationary-pointer geometry on rendering. Native cursor restoration
uses WinUI's current selected system cursor. Observation detaches when the input
is enabled or unloaded.

The catalog pointer regression passes 52 transitions across plain/invalid and
light/dark appearances at scale 1. It verifies actual native cursor handles and
rendered template borders while the physical pointer stays stationary, including
enable/disable, opaque sibling occlusion, layout movement, rectangular clipping,
scrolling, unload and remount. Evidence:
`artifacts/input-pointer-scroll-result.json`. The regression caught stale cursor
restoration and clip removal that does not produce a layout event. A separate
physical keyboard restore/maximize check confirms hover clears and returns when
the window moves the editor away from and back beneath the pointer;
`artifacts/input-stationary-pointer-resize.jpg` retains that checkpoint.

The focused light/dark suite passes at `artifacts/smoke-input-pointer-render` and
the full reduced-motion catalog suite at
`artifacts/smoke-input-pointer-integration`. Custom resource cursors, other display
scales and observer performance remain unverified. Full Input acceptance is open.

## Behavior and native boundary

Input defaults to text and forwards native input props, refs and event handlers.
Error defaults false; an explicit `aria-invalid` overrides the error-derived
value. The Error story renders `aria-invalid=true`. Native search cancel chrome
is explicitly removed. Read-only, disabled, selection, editing, password/search
types, constraints and consumer accessibility names need explicit Windows
mapping and runtime verification, rather than an assumed type equivalence.

The pinned WinUI 2.3.9 `DefaultTextBoxStyle` uses a named `ContentElement`
ScrollViewer for native editing, a placeholder presenter, header/description
presenters, state-dependent Windows brushes/border thickness and a DeleteButton.
A branded template must retain native editing/automation while owning visible
chrome. Removing the Windows underline alone is insufficient.

Current style consumers include the main catalog Title and multiline Summary,
label fixtures, toggle-group consumer input and the isolated package consumer.
Do not apply single-line fixed geometry to the multiline Summary. Textarea has
its own inventory contract. Header/description composition must stay explicit.

## Acceptance criteria

### Reading direction checkpoint

The pinned Bleecker input forwards `dir` to the native HTML input. The local
reference Direction story confirms that Arabic entered text and placeholders
align left for `dir=ltr` and right for `dir=rtl`; the browser reports start
alignment. The retained capture is `artifacts/input-direction-reference-dark.jpg`.
Orchard previously inferred reading direction from Arabic content and aligned
default LTR entered text on the right. Its branded style now sets
`TextReadingOrder=UseFlowDirection` and `TextAlignment=Start`; the placeholder
binds both properties. Explicit consumer values remain dependency-property
overrides. This does not establish RTL adornment parity: Bleecker positions its
icons at physical left/right independently of the input's `dir`.

The fixture checks visible glyph centroids for entered text and placeholders
in both directions, all three sizes and both themes. All 24 cases pass at 100%
display scale in `artifacts/smoke-input-direction-final`. The bitmap check
locates each isolated opaque input box, since RTL overflow shifts the
RenderTargetBitmap origin; it asserts the expected box width before measuring
interior glyphs. Retained images permit inspection of that measurement.

Japanese, Arabic, Hindi, combining-accent and emoji samples additionally verify
full-value selection and line containment, with filled/placeholder captures.
Those checks do not prove glyph completeness, shaping, grapheme navigation or
fallback-font parity. Japanese remains visibly narrower in Orchard: the small
sample measures roughly 73px natively versus 91px in the Windows browser
reference. Candidate-font comparisons are diagnostic only; no inferred font
mapping has been added. Full Input acceptance remains open.

The subsequent equal-input comparison (`artifacts/input-japanese-equal-inputs.jpg`
and `input-japanese-equal-comparison.json`) corrects the earlier comparison of
inputs against plain spans. In identical 13px input boxes, the browser's Segoe UI
fallback path matches the reference with mean squared grayscale pixel error
0.051, versus approximately 962 for Microsoft YaHei UI and 971 for Yu Gothic.
Per-glyph inspection indicates different sources for kanji and kana; this is
rendering evidence, not an authoritative identification of the actual font faces.
The reference document language is `en` and the native input language is `en-US`.

`InputsFixture.FontFallback.cs` retains diagnostic candidate chains without
changing production typography. The focused runtime run at
`artifacts/smoke-input-fallback-probe` measures widths 73/79/84px for the default
and appended Segoe UI chains at small/medium/large. Appending Microsoft YaHei UI
and Yu Gothic yields 91/98/105px, but its glyph shapes differ from the reference.
Matching advance width therefore does not establish fallback parity. These
captures and measurements are observations, not acceptance gates. The fixture
restores the original font and text after the probe.
The finalized probe passes the focused Input runtime suite at
`artifacts/smoke-input-fallback-final`; x64 publish and ARM64 compilation pass.
No production component or package font mapping changed in this diagnostic
checkpoint. The preceding direction correction passed both GitHub CI jobs in
run `38254665170`.

Verification: focused Input and full catalog runtime suites pass at
`artifacts/smoke-input-direction-final` and
`artifacts/smoke-input-direction-integration`; all 13 tests, token drift check,
x64 publish and ARM64 compilation pass. The isolated x64 package consumer passes
at `artifacts/package-consumer-input-direction`, including resources, fonts,
themes and a native action. Package SHA-256:
`6d01abdcbd01246598b466033b38a1d65b484c75cf354f158434698ed5847055`.
ARM64 runtime remains unverified. This native-only change has no backend metrics
impact. The public wiki remote remains unavailable; documentation lives here.

### Remaining acceptance

- Register all three sizes, empty/populated, both adornments independently and
  together, valid/error, read-only/disabled, keyboard/pointer focus and hover.
- Match rendered geometry, glyph baseline, placeholder, alpha colors, border,
  ring, shadows and motion in both themes, including live theme transitions.
- Exercise native typing, selection, clipboard commands, focus traversal,
  validation/accessibility semantics and consumer data updates. Preserve normal
  editing behavior through the actual TextBox content host.
- Verify constrained width, long/non-Latin values, increased text/display size,
  reduced motion, actual Windows contrast colors and Narrator.
- Cover the independent package consumer and physical x64 runtime; retain the
  ARM64 runtime gap until hardware is available. Builds do not close that gap.
- Replace remaining preview consumers only when their applicable contracts are
  verified. No component acceptance follows from this reference audit alone.
