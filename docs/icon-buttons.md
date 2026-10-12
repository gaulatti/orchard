# Icon buttons

`BleeckerIconButton` derives from the native WinUI `Button`. It accepts consumer-owned icon content, commands, command parameters and Click handlers. Disabled state, native focus visuals and the UI Automation Invoke pattern are retained.

```xml
<orchard:BleeckerIconButton Variant="Subtle" Size="Medium"
    Command="{Binding EditCommand}" CommandParameter="{Binding SelectedStory}"
    AutomationProperties.Name="Edit story" ToolTipService.ToolTip="Edit story">
    <SymbolIcon Symbol="Edit" />
</orchard:BleeckerIconButton>
```

Always supply a localized accessible name that describes the action. An icon glyph is not an accessible name. A native tooltip gives sighted users the same description; it remains consumer-owned so applications can localize it or include keyboard hints.

Variants are `Default`, `Subtle` and `Ghost`. Sizes are `Small` (32×32), `Medium` (40×40, default) and `Large` (44×44), matching the pinned Bleecker component. The size property owns width, height and zero padding. Keep custom icon content within that square. A constrained toolbar must provide enough room or scrolling for its fixed-size hit targets. Use `BleeckerButton` for text actions and loading actions.

Palettes derive from canonical tokens in generated, per-variant theme dictionaries. Native normal/hover/pressed/disabled states resolve within the selected palette. Windows high-contrast resources are defined, and the native template remains responsible for keyboard, pointer and focus behavior. `BleeckerButtonGroup` accepts icon buttons through the native `ButtonBase` contract.

## Verification and remaining acceptance

The catalog registers all nine size/variant combinations plus a disabled action. Published x64 checks assert square dimensions, accessible names/button roles, native command and parameter delivery, and rendered background/foreground/border values across normal, hover, pressed and disabled states for all three variants in both themes. The light/dark `icon-buttons-*.png` captures are visually inspected. The independent package consumer includes an icon action and checks layout and invocation.

Still open: physical keyboard/pointer and tooltip interaction, Narrator, actual Windows contrast settings, display/text scaling, reduced motion and ARM64 runtime. ARM64 compilation is separate evidence and does not establish runtime support.
