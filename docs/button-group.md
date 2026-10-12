# Connected button groups

`BleeckerButtonGroup` joins consumer-owned native `ButtonBase` controls in a horizontal (default) or vertical strip. Set `AutomationProperties.Name` on the group to describe its purpose. Each child retains its own content, command, disabled state, automation peer and native keyboard focus. A group does not imply single or multiple selection; use selection controls for that purpose.

```xml
<orchard:BleeckerButtonGroup Orientation="Horizontal"
                            AutomationProperties.Name="Document actions">
    <Button Content="Review" Command="{Binding ReviewCommand}"
            Style="{StaticResource BleeckerSecondaryButtonStyle}" />
    <Button Content="Archive" Command="{Binding ArchiveCommand}"
            Style="{StaticResource BleeckerSecondaryButtonStyle}" />
</orchard:BleeckerButtonGroup>
```

The group owns child margins, corner radii and border thickness while attached. It uses the canonical button radius, removes internal double borders and excludes collapsed children from the strip. Previous local values and bindings are restored when children leave the group on the next layout pass, or when the group unloads. Other child styling remains consumer-owned. Only native `ButtonBase` children are accepted; invalid children fail during layout.

Horizontal groups constrain available widths proportionally when needed. Use wrapping `TextBlock` content for long labels; explicit child minimum/fixed widths remain the consumer's responsibility. Vertical groups stretch children across the group width. Normal Windows Tab navigation visits enabled children separately; arrow-key selection is not introduced for a group of independent actions.

The catalog registers horizontal and vertical fixtures. This is an implementation checkpoint: physical keyboard, screen-reader, high-contrast, scaling and complete parity acceptance are still pending.
