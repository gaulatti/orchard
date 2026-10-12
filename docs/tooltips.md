# Tooltip preview

`BleeckerTooltip` owns a WinUI `Popup` constrained to the application's XAML root. Attach it with Orchard's service, which supplies the input/placement owner and its accessible description relationship:

```xml
<Button Content="Settings" Style="{StaticResource BleeckerSecondaryButtonStyle}">
    <orchard:BleeckerTooltipService.ToolTip>
        <orchard:BleeckerTooltip Content="Workspace settings" Placement="Top" />
    </orchard:BleeckerTooltipService.ToolTip>
</Button>
```

This replaces the earlier preview's native `ToolTipService.ToolTip` API. Migrate that attached property to `orchard:BleeckerTooltipService.ToolTip`. The component now derives from ContentControl, not ToolTip. Do not wrap it in the native service: that creates another independently positioned Windows tooltip. A programmatic consumer can set PlacementTarget directly before opening IsOpen; the target must be loaded. One tooltip has one owner.

Content and ContentTemplate remain consumer-owned. The template uses Encode Sans 12px/19.5px, 12/8px padding, a 224px maximum body width, a 6px corner and no border. Top/bottom arrows are 10×5px; left/right arrows occupy 5×10px. The arrow matches the surface. Light and dark colors follow the [measured reference](tooltip-reference.md); Windows contrast uses paired window/window-text colors. The shared overlay supplies the shadow recipe without the earlier separate tooltip window clipping its blur.

Placement accepts Top, Bottom, Left or Right. ActualPlacement reports the resolved side. The calculation uses a 7px arrow-tip clearance and 10px viewport padding, flips to the opposite side when it has more space and the preferred side cannot fit, and shifts the body within the viewport. The arrow tracks its owner's center and hides if the shifted body cannot point at it. Geometry changes trigger remeasurement; unchanged geometry does not repeatedly invalidate layout.

Keyboard focus opens immediately. Escape, focus loss and pointer press dismiss without moving focus. Hover uses DelayDuration (400ms by default) and a 150ms same-tooltip skip window. Only one tooltip is open per XAML root. The control exposes the tooltip automation role and open/close events, and the service links it through the owner's DescribedBy relationship. Removing the service detaches its owner handlers and description relationship.

The catalog checks all four directions in the viewport center and all four opposite-side collision flips in both themes. Checks measure 7px clearance, assert the popup is constrained, and verify body geometry, fonts, colors, arrows, role, wrapping, live content remeasurement, single-open behavior and the description link. Physical dark-theme Tab entry and Escape dismissal preserve trigger focus; the desktop capture is `artifacts/tooltip-owned-keyboard.jpg`.

The final full catalog run is `artifacts/smoke-tooltip-owned-verified`; the isolated package consumer is `artifacts/package-consumer-tooltip-owned`. Both pass. An earlier full run stopped at the notification fixture's short-text remeasurement check; the final run passes that same check without changing the notification implementation. ARM64 compilation also passes.

Pointer transit uses the pinned Radix 1.2.16 convex grace polygon, including its 5px exit padding. Moving between the trigger and content keeps the tooltip open; subsequent movement outside that polygon dismisses it. The geometry is tested in both directions on all four sides. Actual Windows pointer input verifies top placement in dark appearance through the gap, into content, back to the trigger, and out of the polygon. A discrete jump creates the polygon at the exit sample; the next movement outside dismisses it. The content capture is `artifacts/tooltip-transit-content.jpg`. The adapted geometry's MIT notice is included in the NuGet package as `licenses/LICENSE-Radix.txt`.

The transit checkpoint passes 13 unit tests, x64 published catalog smoke (`artifacts/smoke-tooltip-transit`), ARM64 compilation, and the isolated x64 package consumer (`artifacts/package-consumer-tooltip-transit`).

Motion now uses the surface and motionExit duration tokens: delayed hover entry fades from 0 to 1 and scales .98 to 1 over 220ms with cubic-bezier(.22,1,.36,1); exit fades to 0 and scales to .98 over 150ms with CSS ease-in. Keyboard/programmatic/skip-window entry remains immediate. The template owns the transform, leaving the consumer's control transform untouched. IsOpen and Closed describe logical dismissal; the popup is removed after exit. Reopening cancels the old exit. Detaching or unloading the owner removes it immediately. Windows disabled animations bypass both animations; disabling animations during motion settles an open tooltip or removes a closing one.

This is still in progress. The complete physical transit matrix, rendered normal-motion timing/curve comparison, measured hover/skip timing, modal ownership, collision corners and very small viewports, live appearance, scaled layouts, actual Windows contrast and Narrator require further work and verification. Exact shadow/font rasterization remains subject to rendered comparison. ARM64 compilation does not establish ARM64 runtime on this Intel host.

Motion checkpoint evidence: reduced-motion smoke at artifacts/smoke-tooltip-motion; final enabled-motion smoke at artifacts/smoke-tooltip-motion-duration; final isolated package consumer at artifacts/package-consumer-tooltip-motion. The catalog checks immediate entry, different reduced/normal dismissal lifetimes, interrupted exit/reopening and eventual popup removal. All pass; 13 unit tests and the final ARM64 build also pass. These checks do not yet measure the physical delayed-hover animation frame by frame.
