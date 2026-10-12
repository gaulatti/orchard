# Tooltip reference and acceptance

This is a reference audit, not an implemented or accepted Windows component.
The fixed source is Bleecker `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/tooltip.tsx` and `stories/components/Tooltip.stories.tsx`.
Measurements below were taken from that checkout's rendered AllSides story on
2026-10-12 at a 1280×720 browser viewport, with reduced motion enabled.

## Rendered contract

| Property | Reference |
| --- | --- |
| Text | Encode Sans, normal 400, 12px, 19.5px line height |
| Body | Maximum width 224px, padding 12px horizontal / 8px vertical |
| Shape | Radius 6px, no border |
| Light | Body and arrow `#1A374D`; text `#FFFFFF` |
| Dark | Body and arrow `#1A2332`; text `#F0F4F8` |
| Arrow | 10×5px triangle; rotated footprint 5×10px on left/right |
| Placement | Top by default; top/bottom/left/right, centered on the trigger |
| Clearance | Arrow tip 7px from trigger; body edge approximately 12px away |
| Shadow | `0 24px 64px -20px rgba(26,55,77,.28)` plus `0 4px 14px rgba(26,55,77,.06)` |

Measured light body sizes were 81.265625×35.5px for “Top tooltip”,
82.40625×35.5px for “Left tooltip”, 90.03125×35.5px for “Right tooltip”,
and 101.046875×35.5px for “Bottom tooltip”. Dark “Top tooltip” had the
same size. Widths are browser font-layout evidence, not a tolerance waiver for
the Windows implementation. Browser placement rounds coordinates; measured
arrow-tip clearance ranged from 6.859375 to 7.25px in these fixtures.

The dark `sand` token is a dark blue surface, not the light theme's beige sand.
The arrow must follow the tooltip surface when appearance changes.

## Interaction and motion

Keyboard Tab opened each tooltip immediately (`data-state="instant-open"`),
left focus on its trigger, and closed the preceding tooltip. Escape dismissed
the bottom tooltip while focus remained on Bottom. Tooltip content had the
tooltip role and the trigger referenced it through `aria-describedby`.

The source sets hover delay to 400ms by default, accepts a consumer delay,
sets provider skip delay to 150ms, side offset to 7px and collision padding to
10px. Hover timing, pointer transit into content and collisions were not
measured in this audit.

Delayed entry uses opacity 0→1 and scale .98→1 over 220ms with
`cubic-bezier(.22,1,.36,1)`. Closed-state exit uses opacity 1→0 and scale
1→.98 over 150ms with ease-in. Instant keyboard entry has no delayed-entry
animation class. The source's reduced-motion rule sets animation and transition
durations to .01ms and iteration count to one. The browser reported reduced
motion in this audit; it does not establish normal-motion runtime acceptance.

## Windows implementation requirements

The pinned WinUI 2.3.9 default ToolTip template uses Windows font/color/radius
resources, 9/6/9/8 padding, a 320px maximum width and native fade animations.
Those defaults do not satisfy this contract. A native ToolTip can be templated,
but placement, collision response, timing, arrow orientation and animation
ownership must be proved in the actual runtime before selecting it as the
finished implementation. The inspected ToolTipService API does not expose
Bleecker's delay/skip-delay properties.

Acceptance must cover all four sides, keyboard and pointer entry, dismissal,
content transit, collision-driven side changes, modal ownership, consumer
content and live updates, long/multiline/non-Latin text, disabled triggers,
open-content appearance changes, text/display scaling, reduced and normal
motion, UI Automation/Narrator, and actual x64/ARM64 execution. Apply the
goal's explicit Windows contrast-color adaptation with paired readable colors.
Do not mark this inventory entry complete from a styled preview or a successful
build. The full component remains pending.
