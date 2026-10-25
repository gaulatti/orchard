# BleeckerDetailLayout

`BleeckerDetailLayout` composes caller content with an optional complementary
rail. Both slots retain the original UIElements, input values and commands.

```xml
<orchard:BleeckerDetailLayout Ratio="Primary" Side="End"
    Sticky="True" SideRailLabel="Related details">
    <orchard:BleeckerDetailLayout.SideRail>
        <orchard:BleeckerInput Text="{Binding RelatedValue, Mode=TwoWay}" />
    </orchard:BleeckerDetailLayout.SideRail>
    <StackPanel Spacing="16">
        <TextBlock Text="Consumer heading" Style="{StaticResource BleeckerHeadingStyle}" />
        <orchard:BleeckerInput Text="{Binding Title, Mode=TwoWay}" />
    </StackPanel>
</orchard:BleeckerDetailLayout>
```

Below a 1024 DIP window viewport, main and rail stack with a 40 DIP gap.
From 1024 they form columns with a 48 DIP gap; from 1280 the gap is 64.
Breakpoints use the actual `XamlRoot` viewport, independently of container width.
`Primary` (default) allocates two shares to the main and one to the rail, with a
272 DIP minimum rail. A narrower container can therefore overflow horizontally.
`Balanced` allocates equal shares. Fractional tracks retain the source browser's
1/64 DIP layout allocation and float remainder order.

`Side="Start"` moves the rail visually ahead of main only in columns. Main
remains first in the native tree and Tab order. RTL mirrors the logical tracks.
Untemplated absent, false, numeric zero and empty-string rails omit the rail and its gap;
other source scalar content follows React truthiness and text formatting.
Models require `ContentTemplate`/`ContentTemplateSelector` or
`SideRailTemplate`/`SideRailTemplateSelector`. Consumer templates are retained
when an unrelated layout recipe changes.
Explicit templates/selectors receive original model values, including boxed
numbers and Booleans. An explicit rail template also supplies its presence for
false, zero or null models.

`Sticky` defaults true and applies only to columns. Its 32 DIP top inset is
relative to the nearest ancestor `ScrollViewer` or `ScrollView`, or the window
viewport when neither exists. The rail stops at its layout container's bottom.
Disabling sticky, removing the rail or unloading detaches scroll observers and
resets the owned translation. The component never changes the caller child's
render transform. No motion is introduced.

The layout is a passive native group. The rail is a complementary group named by
`SideRailLabel`; child controls retain their semantics and commands. Standard
native font, foreground, background, border and template properties remain
available. Unknown enum values and unsupported model content fail explicitly.

The Detail layouts catalog entry demonstrates fictional local input and actions.
`tests/Reference/DetailLayout.json` freezes 888 layouts, 48 original full-frame
PNGs and two browser input sequences from the pinned Bleecker source. It covers
breakpoints, narrow/fractional widths, ratios/sides, optional/scalar rails, RTL,
fixed heights, tall rails, scrolling and sticky bounds. Portable tests verify
every source geometry and reference hash. Native checks additionally exercise
input, Tab order, templates/selectors, remounts, live viewport changes and scroll
owner replacement. Current results and remaining acceptance gaps are recorded
in [runtime evidence](runtime-evidence.md).

The current x64 preview matches all 888 measured layouts with zero geometry
error and all 48 complete screenshots with zero differing pixels. Twenty-seven
damaged reports/images are rejected. Both themes pass the native interaction
and scroll-owner checks. Both catalog architectures build without warnings or
errors. A fresh independent NuGet consumer passes x64 runtime in both themes
and ARM64 compilation, including original numeric/Boolean template values;
both temporary source/package caches are removed. Current Field and PageFrame
regressions pass after the shared presentation/filler changes.

The screenshot fixture preserves the browser host's white 20 DIP top margin in
both themes. Opaque box specimens retain fractional logical bounds while their
separate paint parts snap to device pixels, including mirrored RTL coordinates.
These are fixture surfaces; no image pixels are ignored or edited for comparison.

Full caller-override, actual display/text scaling, native ARM64 and final
integration acceptance remain open. See the governing
[component-library scope](library-scope.md).
