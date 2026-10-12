# Toggle groups

`BleeckerToggleGroup` groups consumer-owned `BleeckerToggleGroupItem` controls by stable string values. Single selection is optional: activating the selected item clears it. Multiple selection toggles membership in an observable collection.

```xml
<orchard:BleeckerToggleGroup SelectionMode="Single"
    SelectedValue="{Binding View, Mode=TwoWay}"
    AutomationProperties.Name="View">
    <orchard:BleeckerToggleGroupItem Value="list" Content="List" />
    <orchard:BleeckerToggleGroupItem Value="grid" Content="Grid" />
</orchard:BleeckerToggleGroup>
```

In `Single` mode, `SelectedValue` is authoritative; null or empty means no selection. In `Multiple` mode, bind `SelectedValues` to a non-null `ObservableCollection<string>`. Collection changes update the controls, and activation updates that same collection. `SelectionChanged` reports changes to the active model. Changing modes does not translate or erase the inactive model.

Item values must be unique and nonblank. Selected collection values must also be unique and nonblank. Invalid configuration fails explicitly. Unknown selected values are retained so temporarily absent items regain selection when reinserted. Removal and reordering do not discard consumer state. Items or `ItemsSource` must contain `BleeckerToggleGroupItem` instances; bind each item's content to application data as needed. The group owns `IsChecked` and two-state behavior; bind the group's selection properties rather than item `IsChecked`.

Group variants are `Default` and `Outline`; sizes are `Small`, `Medium` (default), and `Large`. An explicit item style overrides the inherited group style. `IsEnabled="False"` on a group disables its items; individual items may also be disabled. The horizontal strip scrolls at constrained widths. Content, accessible group/item names and localization belong to the consumer.

Tab uses native focus navigation. Left/Right move focus between enabled visible items, wrap at the ends, and respect right-to-left layout. Home/End focus the first/last enabled item. Focus movement does not change selection; Space activates the focused native toggle. UI Automation exposes group Selection and item SelectionItem/Toggle patterns.

## Verification and remaining acceptance

Catalog fixtures cover optional single selection, multiple selection, two-way consumer binding, disabled items/groups, both variants, all sizes and constrained multilingual content. Published x64 runtime checks exercise UI Automation selection, collection mutation, removal/reinsertion, disabled inheritance and light/dark rendering. Physical pointer and keyboard checks verify multiple selection, disabled-item skipping, wrapping, Space selection and clearing. The independent NuGet consumer exercises group templates and selection without a project reference. ARM64 compilation is checked separately.

Remaining acceptance: rendered Bleecker comparison across every size, variant, theme and interaction state; branded focus and motion; Narrator announcements; actual Windows contrast settings; display/text scaling; reduced motion; broader keyboard/RTL and error-state fixtures; and ARM64 runtime. Token and interaction checks do not establish visual parity. This component remains in progress in the parity inventory.
