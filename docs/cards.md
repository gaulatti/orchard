# Cards

`BleeckerCard` is a content surface. It owns no navigation, selection or action behavior; child controls retain their normal commands and accessibility.

```xml
<orchard:BleeckerCard Variant="Surface" PaddingSize="Medium">
    <StackPanel Spacing="16">
        <TextBlock Text="Local coverage" TextWrapping="Wrap" />
        <orchard:BleeckerButton Content="Open workspace" Command="{Binding OpenCommand}" />
    </StackPanel>
</orchard:BleeckerCard>
```

Variants are `Surface` (default), `Outlined`, `Elevated`, `Subtle` and `Transparent`. The pinned Bleecker recipes supply the per-theme fill and border alpha, 12px corners and a 1px border. Padding sizes are `None` (0), `Small` (16), `Medium` (24, default) and `Large` (32). Changing PaddingSize sets Padding; applications can instead supply an explicit Thickness for custom composition. Content and ContentTemplate remain consumer-owned. Local Background and BorderBrush overrides retain normal WinUI precedence.

Consumer Style values and bindings also retain normal precedence. Changing Variant updates instance-owned light/dark palette brushes without replacing Style. A style can override corners or border thickness while retaining the variant's fill and border colors. The generic default style supplies the Bleecker template and typography; the generated variant dictionaries contain only palette resources. Windows contrast resources remain system-owned.

Elevated uses two custom composition shadow layers from Bleecker's raised-shadow recipe: `(0,2,5,0)` at .045 alpha and `(0,18,42,-22)` at .24, in RGB(26,55,77). Negative spread contracts the second mask. Windows ThemeShadow is not used. Shadows are removed on unload and rebuilt on resize or corner changes; all four corner values are honored, with proportional normalization when radii overlap. Windows contrast mode disables shadows. Composition shadow rasterization still requires comparison against the browser blur, including small cards and overridden corners. Framework bitmap captures do not prove composition-shadow rendering; inspect a desktop capture as well.

## Acceptance

The registered fixture covers every variant and padding size, wrapping international text, a constrained-width composition, live variant changes and child action invocation. Native checks compare actual brushes, dimensions, padding and composition layer counts with the source recipe. The independent consumer wraps its controls in a card.

The light-theme desktop capture `artifacts/cards-desktop-light.jpg` confirms that the elevated shadow renders in the published application. A physical pointer click on the card's Open workspace action updates the consumer result to Workspace opened: 1. Light/dark fixture bitmap captures and the independent package runtime also pass. These observations verify visible rendering and child input, not exact browser blur equivalence.

Additional native regressions retain a consumer background binding across variant changes and update it from its source. Pixel assertions in both themes verify that oversized child content is present inside the card but absent beyond its right edge and rounded corner. The visible overflow fixture and asymmetric-corner fixture retain these cases for visual inspection. A corner-change regression first reproduced a stale elevation mask and now verifies each of the four updated arc radii.

The consumer-style fixture binds Style, cycles through all five variants, checks retained corner/border setters and variant brush opacity, then updates the binding source to a different style. These native checks pass in light and dark themes; captures are retained in `artifacts/smoke-card-styles-final`. The independent package fixture supplies its asymmetric corners through a consumer style.

This component remains in progress. Outstanding checks include exact shadow rasterization, broader clipping cases with transforms/scaling, 220ms reference surface transitions and reduced motion, consumer template/style lifecycle, actual contrast settings, Narrator and scaling, and ARM64 runtime. The static resource checks do not establish full visual parity.
