# Stepper

Implementation is in progress against frozen Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, `src/components/stepper.tsx`
(SHA-256 `a0cea6e7cc8fcc541d0089682d64ed8589fc4983a482095785477cb01d646c29`).

Acceptance requires both orientations and all four statuses to match the source
in light and dark themes, including empty content, constrained widths and RTL.
The active ID derives statuses only where a step has no explicit status.
An unknown or empty active ID leaves those steps pending. Activation must return
the original consumer step without changing the active ID or any step metadata.
Missing activation handlers disable input without dimming; an explicitly
disabled step also dims its circle. Native Tab, Space, Enter, pointer and UIA
activation must respect the same ownership and disabled checks.

Live collection/model/command changes, keyed replacement, reorder, removal,
template replacement and unload/reload must update the actual UI without stale
activation or retained subscriptions. Verification must cover text scaling,
Windows contrast, reduced motion, accessible names/status/position, an independent
package consumer and the existing catalog regression sequence. Native ARM64 and
Narrator acceptance still require suitable runtime evidence.

The source uses physical left offsets for connectors even in RTL. Its vertical
circles can shrink below 32 pixels under severe width constraints. These are
source contracts, not opportunities for a different responsive layout.

Use `BleeckerStepper.Steps`, an `ObservableCollection<BleeckerStep>`, with stable
IDs. Each original step has `Id`, `Label`, nullable `Description`, nullable
`Status` and `IsDisabled`. Set `ActiveStep` to derive unspecified statuses and
`Orientation` to `Horizontal` or `Vertical`. Subscribe to `StepInvoked` or supply
`StepCommand`; the command receives the original step as its parameter and its
`CanExecute` controls availability. Update consumer state in the callback when
navigation should advance. Supplying both invokes the command and then the event.
Neither mechanism changes `ActiveStep` automatically.

Windows buttons announce the consumer label, description, status and ordinal,
including completed steps whose source glyph has no textual button name. This
is an intentional accessible-name adaptation for native assistive technology.
The control preserves a consumer-provided group automation name.

`scripts/reference/capture-stepper.cjs` renders the actual frozen React component
with pinned dependencies. Its output records complete unmasked screenshots,
DOM geometry, computed styles and trusted pointer/keyboard events. Browser
evidence alone does not establish native parity.

Current runtime regression uses a 0.05-DIP geometry bound against the captured
DOM. Browser canvas conversion of CSS OKLab can differ by up to two channel
values; brush checks allow that conversion difference and one alpha value.
These checks are not a claim of exact screenshot equality. Exact pixel parity,
enabled-motion timelines, Windows contrast, text/display scaling, Narrator,
native ARM64 runtime and final visual acceptance remain open.

The x64 focused replay currently passes all 44 normal cases and both themes'
physical pointer, Space/Enter/Tab, held-Space scale, UIA invocation, command
replacement, model replacement/reorder/removal, template replacement and
unload/remount checks. x64 and ARM64 solution builds pass. The zero-tolerance
unmasked screenshot audit currently differs in 42 of 44 cases; the two empty
frames match exactly. Keep the component **in progress**. Local evidence is in
`artifacts/smoke-stepper-v5` and `artifacts/stepper-v5-pixel-audit.json`.
