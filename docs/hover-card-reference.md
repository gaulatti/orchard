# HoverCard source contract and native preparation

Hover Card is accepted for the five-check x64 milestone in preview `.67`;
ARM64 runtime remains unverified. Older parity plans below are historical.
The component has a native control and catalog fixture. The reference is Bleecker
commit `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/hover-card.tsx`, SHA-256
`f8af0dfd5c71926359b269f15d8782bd97fbd3fd18579346f59b28a9bfd0fcb6`.
It is a CSS hover group with original trigger/content, an inline absolute
surface and a top/bottom side. It has no portal, controlled-open value,
open/close delay, click toggle, focus scope or Escape dismissal.

## Observable acceptance contract

- The wrapper contributes only the trigger's flow size. Its 288-wide surface is
  physically centered over that wrapper, including in RTL. Bottom starts eight
  pixels below the trigger; top ends eight pixels above it. It does not shift
  or flip near viewport edges. Ancestor clipping remains effective. Caller
  classes can override surface dimensions, padding and radius.
- Default surface padding is 16, border one and card radius 12. Light uses white
  with sand at 30%; dark uses deep sea with white at 12%. Typography inherits
  Encode Sans 16/24 and the existing source overlay shadow. The surface uses
  tooltip semantics and stacking level 50. The source does not add trigger
  expanded/described-by attributes.
- Hover over the wrapper or an already interactive surface enables hit testing
  and targets opacity one. Leaving targets opacity zero and disables hit testing
  immediately. A direct pointer jump into the visible surface keeps the group
  hovered; crossing the literal eight-pixel gap closes it. There is no hidden
  gap bridge or timer. A disabled original button still allows group hover.
- Keyboard activation runs the original trigger action without opening the
  surface. Focus does not open it, hover does not take focus, and Escape does
  not close a still-hovered surface. The source's opacity/pointer-events hiding
  leaves original content in the keyboard tab order while visually closed.
  Preserve original controls, commands, bindings, input and selection through
  side/content changes and show/hide. Do not add modal focus behavior.
- Normal motion transitions opacity for 220 ms with premium easing
  `cubic-bezier(.22,1,.36,1)`. The actual Tailwind stylesheet uses individual
  `translate`, while the transition list contains `opacity, transform`.
  Consequently the four-pixel closed vertical offset snaps on hover change;
  only opacity animates. The source reduced-motion rule makes transitions
  0.01 ms. Native reduced motion must respect the existing preference without
  changing Windows settings.

These details are deliberate source-fidelity requirements, including the
unusual closed-content tab order and immediate gap dismissal. A native popup
with collision handling, focus trapping or conventional delayed opening would
change this component's observable behavior.

## Verification and remaining work

`tests/Reference/HoverCard.json` freezes 44 light/dark source states, four input
histories and 16 controlled animation timelines. Two independent captures match
every receipt and PNG byte. Five portable tests bring the suite to 304 passing
tests and compare the production placement/motion model with those source
observations, including all 96 timeline samples. This is source preparation;
it does not accept the native component or validate a new package.

The collector under `scripts/reference` compiles the original component and
styles, verifies pinned Git blobs and resolves the actual bundled dependency
files. It captures light/dark, top/bottom, RTL, fractional geometry, edge
overflow, narrow viewports, empty/tall/oversized content, disabled triggers,
caller class overrides and clipping. Histories exercise real browser keyboard
and pointer input, retained edits and the gap. Controlled browser animation
timelines distinguish opacity from the snapping translation. Its isolated
browser animation clock pauses before trusted hover input, retaining even the
0.01-ms reduced transition for explicit timeline sampling. Earlier captures
without that clock could miss a completed reduced transition and are retained
as superseded evidence.

The hidden inline primitive passes ancestor pixels, clipping and hit testing
without a popup. The mounted control/fixture now pass all 44 native layouts,
132 brush checks, 96 opacity/presentation samples and five ownership/query
groups. These diagnostics inject the production model's hover/time inputs and
query actual native hit testing; they do not deliver physical input. See
[the native API and fresh independent package evidence](hover-card.md). Native input,
complete raster/shadows, scaling, performance and x64/ARM64 evidence. Both
Monitoring and Editorial fictional-data component demos remain required.
Foreground desktop component tests are authorized. Narrator, OS settings,
pointer heartbeat and Actions remain excluded; pushes use `[skip ci]`.
See the current [library scope](library-scope.md) and
[v10 foreground receipt](evidence/hover-card-v10-foreground-v1/checkpoint.json)
for the active restrictions and observed pointer behavior.
