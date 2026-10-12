# Separator

`BleeckerSeparator` divides adjacent content without adding input behavior.

```xml
<orchard:BleeckerSeparator />
<orchard:BleeckerSeparator Orientation="Vertical" Height="64"
    Decorative="False" AutomationProperties.Name="Workspace columns" />
```

The default is horizontal and decorative. The line is one logical pixel thick, using literal black at 5% opacity in light appearance and literal white at 10% in dark appearance. These values match the pinned Bleecker separator recipe rather than the general border token. Windows contrast mode uses opaque system WindowText. Background remains consumer-overridable; the control introduces no Windows separator template, margins, radii, icons or animation.

The long axis fills its finite available space. Give a vertical separator an explicit Height or place it in a height-constrained container. An unbounded long axis has no intrinsic length, so a separator with no available length requires a consumer constraint. Width/Height can override the default thickness. Orientation changes remeasure without replacing dimension bindings. Surrounding layout owns spacing.

Decorative separators are omitted from UI Automation control/content views. Set Decorative to false for a semantic boundary; its peer reports Separator and the current orientation, retaining a consumer's localized accessible name. Neither mode is a keyboard stop. The reference has no selected, hovered, pressed, disabled, loading, empty, error or motion variant; disabling does not dim the line. It contains no text or user content.

## Evidence and remaining acceptance

The catalog covers both orientations, both semantic modes, constrained widths and live orientation/semantics changes. Native x64 assertions verify geometry, rendered brush bindings, exact color/opacity and real automation-peer properties in light and dark themes. Captures live in `artifacts/smoke-separators`. The pinned Storybook horizontal fixture measures 320 × 1; its vertical fixture measures 1 × 64 with an explicit height, both using white at 10% in dark mode. The clean package consumer includes a semantic horizontal separator.

Actual contrast settings, Narrator, fractional display scaling, physical keyboard traversal and ARM64 runtime remain unverified. This inventory entry stays in progress until those checks and complete rendered parity pass.
