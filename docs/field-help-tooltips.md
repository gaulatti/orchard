# Field help tooltip preview

Use `BleeckerFieldHelpTooltip` alongside a field label. Text is consumer-owned and supplies the tooltip content and default accessible button name.

```xml
<orchard:BleeckerFieldHelpTooltip Text="Use a concise title for this coverage item." />
```

The fixed reference is Bleecker `a86059013cd59684ce452849278ea81e734f2066`, `src/components/field-help-tooltip.tsx`. Its default Storybook fixture was inspected in dark appearance: a transparent 16×16 button, 14px Lucide Info geometry with a 2-unit stroke in its 24-unit coordinate system, secondary text color `#B8C5D6`, a 2px `#5BA3F5` focus ring, and bottom-positioned tooltip. The browser's focused trigger exposed the entire help string as its accessible name. The source specifies primary text on hover, .9 scale while pressed, and a 190ms premium color/transform transition. Its empty/whitespace text branch renders nothing.

The Windows preview uses a native Button with an explicit template, no Windows button chrome, a 14px vector icon, branded state colors, a circular focus ring, and the shared BleeckerTooltip. Its normal and pressed scale values are 1 and .9. Color and scale transitions use the control duration token (190ms) and cubic-bezier(.22,1,.36,1). Keyboard press retains secondary color; pointer press uses primary color while the pointer remains over the trigger. Each trigger animates its own brush, leaving shared theme brushes untouched. Foreground and PointerOverForeground use solid brushes; the default style supplies theme-aware secondary/primary colors. Windows disabled animations settle directly to the destination; preference changes and unloading stop active motion. Empty/whitespace text collapses the control and detaches the tooltip, removing any open popup immediately. Restoring text restores the trigger. Text changes update open content and the automation name. The Lucide notice is already included with the package.

The registered FieldHelpFixture covers an inline label, empty help, normal/hover/pressed/disabled state values, keyboard focus, bottom help, updated text and empty/restored transitions in both themes. A clean package fixture checks the public component's template, dimensions, accessible name and bottom placement.

This component is in progress. Exact rendered motion-curve comparison, the full physical keyboard/pointer matrix, rendered light/dark reference comparison, actual Windows contrast, Narrator, scaling, and ARM64 runtime remain unaccepted. It also inherits the shared Tooltip's remaining acceptance gaps. Disabled is a native Windows adaptation; the reference does not expose a disabled prop or custom disabled palette. No complete parity claim is made from the preview.

The x64 published catalog run at artifacts/smoke-field-help-verified passes both themes, including all four state values, keyboard-triggered help, naming, live text, collapse and restoration. State captures and dark tooltip content were inspected. Scale assertions allow only one-millionth numeric precision on each axis around the exact reference targets. All 13 unit tests and x64/ARM64 builds pass; ARM64 runtime remains unavailable.

The independent x64 package consumer passes at artifacts/package-consumer-field-help. Physical dark-theme Tab entry followed by Enter or Space now dismisses help and retains the focus ring, matching the pinned reference's Enter behavior. See artifacts/field-help-enter-dismissed.jpg. The state fixture now checks keyboard-pressed secondary color and hover primary color; held physical pointer/keyboard comparison remains pending.

Enabled-motion verification passes in artifacts/smoke-field-help-motion-idle for both themes, recording intermediate color and scale values before exact endpoints. Frame samples are collected before static RenderTargetBitmap captures to avoid capture work interfering with the measured transition. Unchanged colors are not animated: a regression guards against WinUI interpolation rounding that briefly changed identical color endpoints. The isolated package consumer passes at artifacts/package-consumer-field-help-stable-color; x64 publish, ARM64 build and 13 unit tests pass. ARM64 runtime remains unavailable.

Tooltip layout and exit-cleanup checks await exact conditions with a bounded one-second deadline; these checks do not establish precise animation timing. The growing full catalog suite has a three-minute process limit. Earlier runs recorded missed frames under load and one exceeded the previous two-minute limit; the successful enabled-motion run was performed without concurrent compilation. Exact rendered curve comparison, live contrast-color refresh and rapid reversal behavior remain unverified.

Reduced-motion verification passes at artifacts/smoke-field-help-motion-reduced-final in both themes. Windows Animation effects is restored to its original Off setting. The wrapped-button activation fixture explicitly completes layout and awaits its template before inspecting the native trigger.

The animated icon observes Color changes on its active source brush, including in-place consumer palette updates. Changing state or replacing a brush updates that subscription; unloading removes it. The runtime fixture checks normal and hover updates, inactive-brush isolation and replacement. Actual Windows contrast switching remains a separate acceptance requirement.

The live-brush regression passes in both themes at artifacts/smoke-field-help-brush-ready, with Windows animations disabled. It waits for each requested state before mutating the active brush. This proves in-place source-color propagation; it does not replace an actual Windows contrast-mode or animated reversal check.
