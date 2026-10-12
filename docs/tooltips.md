# Tooltip preview

`BleeckerTooltip` is a native WinUI `ToolTip` with an explicit Bleecker template.
Attach it through the standard service:

```xml
<Button Content="Settings" Style="{StaticResource BleeckerSecondaryButtonStyle}">
    <ToolTipService.ToolTip>
        <orchard:BleeckerTooltip Content="Workspace settings" Placement="Top" />
    </ToolTipService.ToolTip>
</Button>
```

Content and ContentTemplate remain consumer-owned. The template uses Encode
Sans 12px/19.5px, 12/8px padding, a 224px maximum body width, a 6px corner and
no border. Top/bottom arrows are 10×5px; left/right arrows occupy 5×10px.
The arrow matches the surface color. Light and dark colors follow the
[measured reference](tooltip-reference.md); Windows contrast uses paired
window/window-text colors. The shared overlay surface supplies the shadow
recipe. The tooltip is not a Tab stop and retains the native tooltip role.

The catalog includes all four directions and a wrapping mixed-script example.
Runtime checks cover body geometry, rendered font and colors, arrow dimensions,
native role, wrapping and live content remeasurement in both light and dark.
Physical keyboard testing is required independently of programmatic opening.

Escape dismissal is handled while the tooltip is open, with the root key
handler detached on close/unload. Physical x64 dark-theme testing verifies Tab
opens Bottom and Escape closes it while the trigger retains its focus ring.
The native default initially left the tooltip visible after Escape. Evidence
is retained at `artifacts/tooltip-keyboard-dismissed.jpg`; light/dark body
captures and content-remeasurement checks are at
`artifacts/smoke-tooltip-verified`. These captures do not prove popup clearance
or collision behavior: cross-popup visual transforms omit native window
placement offsets and must not be treated as screen-coordinate evidence.

The independent package consumer attaches the packaged tooltip to its icon
button and opens it in both themes, checking its loaded template, bundled font,
geometry and native role. Results are retained under
`artifacts/package-consumer-tooltip-final`. x64 runtime and ARM64 compilation
are separate checks; ARM64 runtime remains unavailable on this Intel host.

This is an in-progress component, not accepted visual parity. Native placement
and collision handling still own the popup position; the current arrow follows
the requested Placement and does not yet follow a collision-driven flip.
The native popup window can clip the overlay shadow. Reference 7px clearance,
10px collision padding, hover delay/skip delay, pointer transit, reference
entry/exit motion and modal ownership require further implementation and
verification. Native timing is not an approved substitute for Bleecker timing.
Live appearance, scaled/constrained layouts, actual Windows contrast, Narrator,
the full physical interaction matrix and actual ARM64 runtime also remain open.
