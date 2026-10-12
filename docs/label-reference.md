# Label reference and acceptance

Reference: Bleecker `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/label.tsx`, `stories/components/Label.stories.tsx`, and installed
Radix Label 2.1.10. Windows implementation remains pending.

## Rendered contract

The Default and WithError stories were inspected in the pinned local Storybook.
Default uses Encode Sans, weight 500, size 13px, line height 13px, no additional
letter spacing (`tracking-ui` is 0em), zero padding/margin/border and no radius.
The label occupies the consumer grid's 384px width; this is a composition width,
not an intrinsic label width. Its measured single-line height is 13px.

| Composition | Light | Dark |
| --- | --- | --- |
| Default | `#2D2D2D` | `#F0F4F8` |
| WithError label | `#A65D57` | `#F0F4F8` |

The error story supplies `text-terracotta`; the component's retained
`dark:text-text-primary` wins in dark appearance. Do not invent a label-level
error variant or assume the consumer's light error color applies in both themes.
The associated input and error paragraph have their own contracts.

Clicking the Default label focuses the input with ID `email`. The input's
accessible name is `Email Address`; the label itself has tabIndex -1. It adds no
independent keyboard stop. Content and association are consumer-owned.

## Source behavior requiring further verification

The component forwards Radix label props, including children, association and
consumer styles. Radix uses a native HTML label and suppresses text selection on
repeated mouse-down except within nested button/input/select/textarea elements.
Nested interactive content must retain its own input behavior.

Disabled styling is conditional on a preceding disabled `.peer` sibling:
opacity .6 and a not-allowed pointer cursor. This is a CSS relationship, not a
label `disabled` prop and not automatically the state of any `htmlFor` target.
The two registered label stories do not demonstrate this relationship. A Windows
adaptation must document its explicit disabled-state API and verify focus,
association and opacity without silently equating unrelated controls.

There is no label-owned motion, padding, minimum width, validation message or
background. Long and rich content, empty content, constrained-width wrapping,
non-Latin text and increased text size still need rendered reference checks.

## Windows acceptance criteria

- Use the bundled Encode Sans Medium face, 13px text, 13px line box and zero
  tracking; verify actual glyph placement and clipping rather than just setters.
- Preserve consumer-owned content, style overrides and input association.
  Clicking a plain label must focus its associated enabled input through its
  normal focus path. Nested interactive children must keep their own actions.
- Expose the associated input's label to UI Automation and preserve explicit
  consumer accessibility names; avoid introducing a redundant Tab stop.
- Register default, error composition, explicit disabled adaptation, empty,
  long/multiline, non-Latin and rich-content fixtures. Match both reference
  themes and test live appearance changes.
- Use paired Windows contrast colors in actual contrast mode. Verify keyboard,
  pointer, Narrator, text/display scaling and x64/ARM64 runtime separately.
- Verify the public label API through the isolated NuGet consumer and document
  platform adaptations. No component acceptance follows from this reference
  audit alone.
