# Field help tooltip preview

Use `BleeckerFieldHelpTooltip` alongside a field label. Text is consumer-owned and supplies the tooltip content and default accessible button name.

```xml
<orchard:BleeckerFieldHelpTooltip Text="Use a concise title for this coverage item." />
```

The fixed reference is Bleecker `a86059013cd59684ce452849278ea81e734f2066`, `src/components/field-help-tooltip.tsx`. Its default Storybook fixture was inspected in dark appearance: a transparent 16×16 button, 14px Lucide Info geometry with a 2-unit stroke in its 24-unit coordinate system, secondary text color `#B8C5D6`, a 2px `#5BA3F5` focus ring, and bottom-positioned tooltip. The browser's focused trigger exposed the entire help string as its accessible name. The source specifies primary text on hover, .9 scale while pressed, and a 190ms premium color/transform transition. Its empty/whitespace text branch renders nothing.

The Windows preview uses a native Button with an explicit template, no Windows button chrome, a 14px vector icon, branded state colors, a circular focus ring, and the shared BleeckerTooltip. Its normal and pressed scale values are 1 and .9. Empty/whitespace text collapses the control and detaches the tooltip, removing any open popup immediately. Restoring text restores the trigger. Text changes update open content and the automation name. The Lucide notice is already included with the package.

The registered FieldHelpFixture covers an inline label, empty help, normal/hover/pressed/disabled state values, keyboard focus, bottom help, updated text and empty/restored transitions in both themes. A clean package fixture checks the public component's template, dimensions, accessible name and bottom placement.

This component is in progress. The 190ms color/scale transitions, physical keyboard/pointer matrix, rendered light/dark reference comparison, actual Windows contrast, Narrator, scaling, and ARM64 runtime remain unaccepted. It also inherits the shared Tooltip's remaining acceptance gaps. Disabled is a native Windows adaptation; the reference does not expose a disabled prop or custom disabled palette. No complete parity claim is made from the preview.

The x64 published catalog run at artifacts/smoke-field-help-verified passes both themes, including all four state values, keyboard-triggered help, naming, live text, collapse and restoration. State captures and dark tooltip content were inspected. Scale assertions allow only one-millionth numeric precision on each axis around the exact reference targets. All 13 unit tests and x64/ARM64 builds pass; ARM64 runtime remains unavailable.

The independent x64 package consumer passes at artifacts/package-consumer-field-help. Keyboard activation dismissal and the distinction between keyboard-pressed and pointer-pressed icon colors still require an explicit reference/runtime check.
