# ScrollArea

`BleeckerScrollArea` is a reusable content boundary with a constrained viewport
and source scrollbar recipe. It owns scrolling presentation and retains the
caller's original content, bindings, editing and selection. It adds no data
loading, virtualization or selection policy.

```xml
<orchard:BleeckerScrollArea Width="360" MaxHeight="240"
                            Orientation="Both" IsTabStop="True"
                            AutomationProperties.Name="Fictional local notes">
    <StackPanel Width="620" Spacing="16">
        <TextBox Text="Original caller editor" />
        <Button Content="Original caller action" />
    </StackPanel>
</orchard:BleeckerScrollArea>
```

Use the inherited `MaxHeight`/`Height` properties for native layout constraints.
An unconstrained area grows naturally. `Orientation` is `Vertical` (default),
`Horizontal`, or `Both`; it controls user axes and scrollbar visibility.
Hidden user axes retain a programmatic range. Auto-width content wraps within
the remaining viewport. Explicitly wider content retains its extent. Scrollbars
reserve 6 DIPs vertically and 15 DIPs horizontally when needed, with the vertical
gutter on the left in RTL. Padding scrolls with content; borders stay outside
the viewport. Caller styles, typography, background, border, padding and corner
radius use normal WinUI properties.

`Content`, `ContentTemplate` and `ContentTemplateSelector` accept original native
controls or original models with a template. Untemplated scalar text follows
the library's React-content formatting contract. An arbitrary model requires a
template or selector. Changes to orientation, maximum size and paint preserve
unchanged original controls and their bindings.
Clear or replace model content before removing its required template/selector;
an untemplated arbitrary model is rejected immediately.

`ScrollTo(horizontalOffset, verticalOffset)` requests immediate native scrolling
and returns its native correlation identifier (`-1` if no operation starts).
Offsets clamp to the current range and must be finite. Horizontal offsets are
positive distances from the leading edge, including from the right in RTL;
this follows native conventions. Source browser `scrollLeft` is negative in
RTL. Read `HorizontalOffset`, `VerticalOffset`, `ViewportWidth`, `ViewportHeight`,
`ExtentWidth` and `ExtentHeight` after `ViewChanged` for actual layout/position.
Extents are at least the corresponding viewport dimension. A mounted template
is required before calling `ScrollTo`.

Set `IsTabStop` and `AutomationProperties.Name` when the area itself should be
focusable/named. The owner exposes pane semantics and a scroll pattern backed by
the actual native range, with user-axis permissions, percentages and increments.
Disabled user requests are rejected; offsets reclamp if native scrollbar
visibility changes the viewport.
Original descendant controls keep their own actions and metadata. Native touch
and pen manipulation remain with `ScrollView`; wheel and keyboard requests
follow the component's enabled axes and immediate scrolling policy. The source
uses a 40-DIP arrow step and vertical Home/End/Page keys. Scrollbar hover uses
the theme's sand recipe; ordinary scrolling has no component animation.

`ScrollAreasFixture` is registered beside HoverCard. Its fictional local content
demonstrates all three orientations, live maximum/direction changes, retained
editing/actions, programmatic scrolling, wrapping and empty content. Monitoring
and Editorial remain separate required fictional-data component demos.

Local `0.1.0-scroll-area-v2-local` and the final x64 catalog each pass 94 source
layouts, 12 controlled scrolling histories, 3,076 zero-error geometry values and
five contract groups. Fourteen full native client PNGs verify 12 source paint
samples, 17,952 straight thumb-body pixels (maximum channel error one) and 960
zero-height clipping pixels. The auditor rejects 28 damaged catalog receipts
and 32 damaged package receipts, including an actual earlier elliptical-thumb
PNG with a consistent hash. The production radii are 3 DIPs vertically and
7.5 DIPs horizontally; WinUI's oversized radius produced an ellipse rather than
the source capsule. Both architecture catalogs and clean consumers compile;
309 portable and nine windowless font tests pass. The fictional-data fixture
and both theme styles are registered in `catalog-coverage.json` and navigation.

These are hidden diagnostics using production scroll methods and native visual
states. They record zero visibility, activation, focus and physical input.
An example [raw native capture](../artifacts/current/scroll-area-native-v6/Light-both-hover.png)
shows the diagnostic viewport and thumbs, rather than normal catalog startup.

This is an unaccepted local prototype. The [source contract](scroll-area-reference.md)
and native/package evidence remain distinct from full acceptance. Physical
input/focus delivery, complete source raster and normal motion behavior,
customization, scaling, representative performance, visible startup and native
ARM64 acceptance remain open. Background runs never authorize desktop input,
Narrator, OS settings, pointer heartbeat, Actions or pushes.
