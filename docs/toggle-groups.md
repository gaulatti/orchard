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

In `Multiple` mode, an item's UI Automation `SelectionItem.Select` replaces the
active selection in the original consumer collection. The group raises one
`SelectionChanged` notification after the final selection is synchronized. An
already exclusive selection is a no-op. Consumers that subscribe to their own
`ObservableCollection` still receive its ordinary clear/add collection events.

Item values must be unique and nonblank. Selected collection values must also be unique and nonblank. Invalid configuration fails explicitly. Unknown selected values are retained so temporarily absent items regain selection when reinserted. Removal and reordering do not discard consumer state. Items or `ItemsSource` must contain `BleeckerToggleGroupItem` instances; bind each item's content to application data as needed. The group owns `IsChecked` and two-state behavior; bind the group's selection properties rather than item `IsChecked`.

Group variants are `Default` and `Outline`; sizes are `Small`, `Medium` (default), and `Large`. An explicit item style overrides the inherited group style. `IsEnabled="False"` on a group disables its items; individual items may also be disabled. The horizontal strip scrolls at constrained widths. Content, accessible group/item names and localization belong to the consumer.

Tab uses native focus navigation. Left/Right move focus between enabled visible items, wrap at the ends, and respect right-to-left layout. Home/End focus the first/last enabled item. Focus movement does not change selection; Space activates the focused native toggle. UI Automation exposes group Selection and item SelectionItem/Toggle patterns.

SelectionItem and Toggle actions apply only while the item is mounted in its
current group. Retained handles reject actions after item removal or group unload
with the Windows element-not-available error, before changing selection or raising
selection events. A mounted disabled item or group reports element-not-enabled.
After remounting or moving the same item to another group, its handle uses the
current mounted group. This lifecycle contract does not require a screen reader
or changes to Windows accessibility settings.

## Verification and remaining acceptance

The earlier lifecycle fixture runs in a hidden native XAML host: 50 source,
48 independent-package and 50 archive checks pass, with 70 observations per
host. It verifies optional single selection and its two-way binding, original
multiple-selection collection identity, replacement subscription cleanup,
inactive-model preservation, removal/unload rejection, disabled actions,
remount/reparent behavior, all six styles in both themes, and the actual catalog
fixture's displayed selection. All 70 observations match across the three hosts.
These checks send no hardware input and do not establish keyboard, pointer,
full visual, scaling or ARM64 runtime acceptance. See the
[current runtime evidence](runtime-evidence.md#toggle-group-retained-provider-lifecycle-2026-10-24).

The later `0.1.0-text-media-v4-local` package passes the same hidden lifecycle
contract on its current Orchard DLL. The extracted x64 catalog passes 62 checks
and 1,749 visibility/focus guards; its independent NuGet consumer passes 60
checks and 1,721 guards. Each host records 70 observations, 12 standalone
style/command cases, 32 rejected retired-provider actions and 16 rejected
disabled actions. The [v4 checkpoint](../artifacts/current/text-media-v4-toggle-group-checkpoint-v1.json)
pins both audited reports and the package bytes. These runs do not exercise
physical input, source raster parity, display scaling or native ARM64 execution.

The `0.1.0-toggle-notification-v1-local` preview fixes the multiple-selection
`SelectionItem.Select` event count. Before the fix, the new hidden boundary
case failed in both light and dark themes because one action raised two group
notifications. The repaired source catalog passes 64 checks and 1,777 guards;
an independent consumer of the new NuGet package passes 62 checks and 1,749
guards. Both record 70 observations and retain the prior lifecycle and disabled
action checks. The [preview record](releases/2026-10-26-toggle-notification-v1-local.md)
and [small native receipts](evidence/toggle-notification-v1/README.md) survive
cleanup of disposable build output. Package validation was hidden/background;
the catalog's visible `VerifyAsync` assertion was compiled but not activated.
The updated source catalog compiles for ARM64; native ARM64 runtime remains open.

Catalog fixtures cover optional single selection, multiple selection, two-way consumer binding, disabled items/groups, both variants, all sizes and constrained multilingual content. Published x64 runtime checks exercise UI Automation selection, collection mutation, removal/reinsertion, disabled inheritance and light/dark rendering. Physical pointer and keyboard checks verify multiple selection, disabled-item skipping, wrapping, Space selection and clearing. The independent NuGet consumer exercises group templates and selection without a project reference. ARM64 compilation is checked separately.

Remaining acceptance: rendered Bleecker comparison across every size, variant,
theme and interaction state; branded focus and motion; display/text scaling;
reduced motion; broader keyboard/RTL and error-state fixtures; and ARM64 runtime.
Narrator and OS contrast campaigns are excluded by the
[component-library scope](library-scope.md). The earlier published runtime
statements describe their original checks; the v4 evidence above is separate.
Token and interaction checks do not establish visual parity. This component
remains in progress in the parity inventory.
