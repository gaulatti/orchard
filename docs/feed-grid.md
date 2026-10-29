# BleeckerFeedGrid

Feed Grid is accepted for the [x64 milestone](acceptance-contract.md) in preview
.19. The matching catalog passes 144 Light/Dark layouts and native child
keyboard, pointer and focus behavior. An independent package consumer compiles
and passes 16 native 150% scale cases across narrow/wide viewports, including
normal, wrapped, collapsed and empty states. The control has no animation or
transition, so its static behavior respects reduced motion. ARM64 is build-only
with runtime unverified. Raster parity is not a milestone gate.

`BleeckerFeedGrid` arranges original caller controls in an auto-filling grid.
Its default minimum column width is 340 DIP, gap is 12 DIP and padding is
16 DIP. The minimum is capped at the available inner width, so narrow grids
retain one fitting column. Empty column tracks remain reserved when fewer
children are present. Each row uses its tallest child; fixed-height children
keep their own height.

```xml
<orchard:BleeckerFeedGrid MinColumnWidth="240">
    <orchard:BleeckerCard><orchard:BleeckerInput Text="{Binding Value, Mode=TwoWay}" /></orchard:BleeckerCard>
    <orchard:BleeckerCard><orchard:BleeckerButton Content="Consumer action" Command="{Binding Action}" /></orchard:BleeckerCard>
</orchard:BleeckerFeedGrid>
```

The inherited native `Children` collection retains controls, input, bindings,
commands and focus. Native `Children.Move` reorders without removing their
visual lifetime. Collapsed children do not occupy a cell. `MinColumnWidth`,
`Gap` and `Padding` are bindable native properties and require finite,
nonnegative values. A zero minimum retains the source browser's one-pixel
floor for calculating the auto-repeat track count.

Native width, height, alignment, margin, background and RTL remain under caller
control. Set a child's `HorizontalAlignment="Stretch"` when it should fill its
track; existing component styles may choose another alignment. Minimum width
uses native DIPs; CSS unit expressions and arbitrary grid/class overrides are
not parsed. The container adds no action or Tab stop and keeps caller naming
metadata available through its native group peer. An unbounded native parent
uses the larger of the minimum width and the children's intrinsic width.

The Feed grids catalog fixture uses fictional local content. The pinned source
reference contains 144 layouts and 22 complete original PNGs, including empty
tracks, 723/724 DIP boundaries, fractional tracks, RTL, zero minimum, collapsed
children and explicit gap/padding overrides. Historical native and package
diagnostics are recorded in runtime-evidence.md.

The current x64 run has zero geometry error across all 144 layouts and zero
pixel differences across all 22 complete reference screenshots. Both themes
retain real editing, native pointer/Space actions, focus and child lifetime
across resizing, live minimum bindings and collection moves. These focused
results do not establish native ARM64 runtime. The current milestone's 150%
scaling checks are recorded above.

The native arrangement pass visits each child once. Twenty loaded layout
updates average 0.165/0.476/2.159 ms for 128/512/2,048 simple native Borders on
this x64 host. These timings measure width updates and native measure/arrange,
excluding rendering and application-specific child work. Both catalog builds
and fresh x64 runtime/ARM64 package-consumer compilation pass; temporary
consumer sources and package caches are removed.
