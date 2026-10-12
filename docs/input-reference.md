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
radii. Native text baseline/line-box equivalence remains unverified.

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
Orchard's dark disabled background is corrected; disabled hover and cursor
behavior remain open implementation gaps.

The x64 disabled-pointer investigation found that WinUI routes the pointer to
the enclosing ScrollViewer chrome, and omits disabled TextBox elements from
`FindElementsInHostCoordinates`, including its `includeAllElements` overload.
Setting `ProtectedCursor` to `UniversalNo` alone leaves the native arrow cursor.
An unpublished bounds/chrome observer and native cursor prototype produces the
dark disabled hover border while retaining disabled focus semantics and .6
opacity. Direct cursor-handle checks distinguish the actual system cursor from
the capture tool's pointer marker. Moving between disabled inputs exposed a
cursor ownership ordering bug; the revised prototype passes that transition.
Stationary-pointer scrolling/resizing, clipping/occlusion, enable/disable,
unload/reload, cursor restoration and light-theme regression remain required
before this work can be published. These findings do not close acceptance.

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
