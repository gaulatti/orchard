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

Scroll Area is accepted for the x64 milestone. The exact packaged DLL passed 94
Light/Dark native layouts, 12 scrolling histories and five contract groups in
the independent consumer. The extracted .47 x64 catalog, with unchanged
component source and fixture in .48, passed visible Light/Dark rendering,
pointer activation of the original caller action, wheel
scrolling, Tab traversal and Down-key scrolling. A Tab from the scroll command
followed by Down moved the native viewport to its end. The automation
focused-element field was stale; focus was assessed from visible outline and
observable keyboard response, without claiming an automation focus identity.

The independent package consumer passed ten native 150% Light/Dark layouts
covering vertical, horizontal, both, RTL and empty areas. The owner and viewport
stayed in bounds and original overflowing content was retained. Scroll Area
has no component animation path, and `ScrollTo` requests immediate native
scrolling. With Windows animations already disabled, offsets stayed static in
the ten package-consumer layouts; no OS setting was changed. The independent
x64 package consumer compiled and ran; ARM64 is build-only and runtime-unverified.
The [source contract](scroll-area-reference.md) remains available for usage
details. Raster/pixel parity and performance are not gates for this milestone.
