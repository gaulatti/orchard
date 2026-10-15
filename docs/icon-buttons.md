# Icon buttons

`BleeckerIconButton` derives from the native WinUI `Button`. It accepts consumer-owned icon content, commands, command parameters and Click handlers. Disabled state, native focus visuals and the UI Automation Invoke pattern are retained.

```xml
<orchard:BleeckerIconButton Variant="Subtle" Size="Medium"
    Command="{Binding EditCommand}" CommandParameter="{Binding SelectedStory}"
    AutomationProperties.Name="Edit story" ToolTipService.ToolTip="Edit story">
    <ContentControl Style="{StaticResource LucideEditIcon}" />
</orchard:BleeckerIconButton>
```

Always supply a localized accessible name that describes the action. An icon glyph is not an accessible name. A native tooltip gives sighted users the same description; it remains consumer-owned so applications can localize it or include keyboard hints.

`LucideEditIcon` in this example is a consumer resource, illustrated by [the catalog's vector styles](../samples/Orchard.Catalog/ReferenceIcons.xaml). It is not a library resource. The catalog uses the pinned Bleecker dependency, Lucide 0.563.0, rendered at 18×18 with a 24-unit canvas and round two-unit strokes. The Lucide license ships with the catalog and library package. Windows `SymbolIcon` glyphs are not visual substitutes for those vectors.

Nested `Control` content without an explicit foreground follows the native presenter's rendered foreground, including disabled state. A local value or binding, or a foreground setter in an explicit style, retains consumer ownership. Replacing that style re-evaluates ownership; removing the content removes only Orchard's own binding. Vector templates should bind their strokes to their own foreground, as shown in the catalog. Other consumer content retains native WinUI behavior.

Variants are `Default`, `Subtle` and `Ghost`. Sizes are `Small` (32×32), `Medium` (40×40, default) and `Large` (44×44), matching the pinned Bleecker component. The size property owns width, height and zero padding. Keep custom icon content within that square. A constrained toolbar must provide enough room or scrolling for its fixed-size hit targets. Use `BleeckerButton` for text actions and loading actions.

Palettes derive from canonical tokens in generated, per-variant theme dictionaries. Native normal/hover/pressed/disabled states resolve within the selected palette. Windows high-contrast resources are defined, and the native template remains responsible for keyboard, pointer and focus behavior. `BleeckerButtonGroup` accepts icon buttons through the native `ButtonBase` contract.

## Verification and remaining acceptance

The catalog registers all nine size/variant combinations plus a disabled action. Published x64 checks assert square dimensions, 18px vector size and round strokes, accessible names/button roles, native command and parameter delivery, and rendered background/foreground/border and icon stroke values across normal, hover, pressed and disabled states for all three variants in both themes. Consumer color/style ownership and content replacement are checked. The light/dark `icon-buttons-*.png` captures are visually inspected. The independent package consumer includes its own Lucide Save vector and checks layout, disabled foreground propagation and invocation.

Physical desktop testing in the dark theme verifies the Edit fixture: pointer click increments its command result, Tab/Shift+Tab displays its branded focus ring, Space increments the result again, and its Edit story tooltip appears. Evidence: `artifacts/icon-keyboard-dark.jpg`.

Still open: the remaining physical keyboard/pointer and tooltip matrix, Narrator, actual Windows contrast settings, display/text scaling, reduced motion and ARM64 runtime. ARM64 compilation is separate evidence and does not establish runtime support.

Light Ghost hover/pressed backgrounds share the [measured byte-alpha sand recipe](brush-alpha-reference.md) with text Ghost buttons. Foreground, border, disabled and contrast resources retain their existing recipes.
