# HoverCard prototype

`BleeckerHoverCard` composes an original trigger and content in an inline hover
surface. Its [frozen source contract](hover-card-reference.md) governs placement,
pointer behavior, keyboard behavior and motion. The native control and fixture
pass the hidden native source matrix, ownership diagnostics and fresh package
replay. Complete component acceptance remains open.

```xml
<orchard:BleeckerHoverCard Side="Bottom">
    <orchard:BleeckerHoverCard.Trigger>
        <Button Content="Fictional contributor" />
    </orchard:BleeckerHoverCard.Trigger>
    <StackPanel Spacing="8">
        <TextBlock Text="Mira Quinn" />
        <TextBox Text="Original caller note" />
    </StackPanel>
</orchard:BleeckerHoverCard>
```

`Trigger` and inherited `Content` retain original native controls or models.
Use `TriggerTemplate`/`TriggerTemplateSelector` and the standard content
template/selector properties for original model presentation. Unchanged slots
preserve their controls, commands, bindings, edits and selection.

`Side` supports Top and Bottom, defaulting to Bottom. The surface is centered
over the trigger and separated by eight DIPs, with no collision shift/flip or
portal. Ancestor clipping remains effective. `SurfaceStyle` replaces its private
surface layout style, whose default width is 288. Inherited background, border,
padding, corner radius, typography and content alignment style the surface.
Default padding is 16, border one and shared card radius 12. The native control
defaults to `Canvas.ZIndex=50` so its surface can overlap normal sibling content;
explicit caller values take precedence. Native ancestors retain their own
stacking scopes, so broader ancestor/overlay compositions still need verification.
Native templates
require a Grid named `PART_Layout`.

Pointer hover targets opacity one and enables hit testing immediately. Leaving
targets opacity zero and disables hit testing immediately; the four-pixel
closed offset snaps while opacity follows the source 220-ms premium transition.
Reduced motion follows the existing preference without changing Windows.
`IsHovered` reports the current pointer state. HoverCard does not intercept
original trigger actions, open on focus, capture focus, dismiss on Escape or
add a focus loop. The source leaves original closed content in the tab order.
The content has a native ToolTip peer; original trigger/control peers remain
caller-owned. Template retirement releases old presenters and motion resources;
unload disconnects the preference observer and rendering clock.

`HoverCardsFixture.xaml` registers fictional default, top-side and disabled
trigger examples with caller-owned editable content. Monitoring and Editorial
remain required component demonstrations. Native pointer/focus, nested host
integration, full raster/shadows, customization, scaling, performance, visible
startup and ARM64 runtime acceptance remain open.

## Verified prototype checkpoint

Local package `0.1.0-hover-card-v1-local` and the catalog each pass 44 source
states: 1,020 geometry/layout values have zero error, 132 brush comparisons
pass, and 96 native presentation samples match the frozen opacity/position
timelines (maximum opacity error below 0.0000005). Five contract groups cover
original actions, disabled triggers, actual native point queries and sibling
stacking, retained editor/template state, original model selectors/live sizing,
and presentation clocks. Eight full native client PNGs retain original pixels
and are checked for dimensions, hashes and selected surface-fill samples.

These diagnostics inject the private production model/time inputs and query
actual native hit testing. They do not deliver physical pointer/keyboard input,
verify focus behavior or compare the complete source raster/shadows. All runs
record zero visibility, activation, focus and input. The separate inline
primitive verifies four source rectangles, 12 actual pixels/hit samples and
closed-editor focus traversal under an unshown window.

The same package passes Popover's 122 states/five contracts, Header's 72 layouts/
23 scenarios (unchanged source paint limit one), Footer's 192 layouts/11
contracts and AppShell's 168 layouts/44 ownership checks. All 304 portable tests
pass. Catalog and isolated package consumers compile for x64/ARM64; native
ARM64 execution remains open. Twenty-one damaged catalog receipts and 25
damaged package receipts are rejected. Versioned catalog archives match every
built file, and the package DLL/XAML/PRI/README/license payload is verified.

The sealed evidence is `artifacts/current/hover-card-implementation-checkpoint.json`.
Physical input/focus, source raster/shadows, broader ancestor stacking and
nested popup/modal integration, live normal motion, customization, scaling,
performance and visible startup remain unaccepted. Both fictional-data demos
remain required and unaccepted. No Actions were dispatched and nothing was pushed.
