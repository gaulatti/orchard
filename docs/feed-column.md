# BleeckerFeedColumn

`BleeckerFeedColumn` is a reusable feed surface with a fixed header, optional
numeric count and filter slot, and an independent native scrolling body. The
default width and minimum width are 372 DIP. Its height stretches within a
bounded parent. The original three-stop 45-degree Oklab gradient, accent dot,
border, count pill and shadows belong to the component's presentation.

```xml
<orchard:BleeckerFeedColumn Title="Local coverage" Count="{Binding Count}"
                           Width="320" MinWidth="320" Height="480">
    <orchard:BleeckerFeedColumn.Filter>
        <orchard:BleeckerInput Text="{Binding Filter, Mode=TwoWay}" />
    </orchard:BleeckerFeedColumn.Filter>
    <ItemsControl ItemsSource="{Binding LocalItems}" />
</orchard:BleeckerFeedColumn>
```

`Title`, nullable numeric `Count`, solid-brush `Accent`, `Filter`, filter
templates/selectors and inherited content templates/selectors are bindable.
The original consumer objects, controls and bindings are retained. Updating
the header does not replace body/filter controls or reset their scroll state.
There are no services, filtering algorithms or model mutations in the control.
The Feed columns catalog fixture demonstrates composition with fictional local
data and consumer event handlers.

`Width` and `MinWidth` independently override the original width/minimum-width
pair; set both to change a column's recipe width. Native DIPs replace CSS unit
expressions. Inherited `Padding` controls the body's default 12-DIP inset.
`Background`, `BorderBrush`, `BorderThickness`, `CornerRadius`, RTL and native
templates remain caller-controlled. `Viewport` exposes the current native
`ScrollViewer` after template application for view changes, event subscription
and observation; reacquire it after replacing a template. The caller owns any
handlers attached to a former viewport.

The group peer uses the title by default and honors caller names, including
explicit empty local/bound/styled names and `LabeledBy`. Decorative dot parts
add no accessibility stop. A scrolling body with no focusable content exposes
the native viewport's keyboard stop. Focusable caller content retains normal
native traversal. Existing enabled/visibility behavior stays native.

Acceptance requires the pinned header/wrapping/count/filter/body geometry,
independent scrolling, live bindings, retained slot identity, templates,
metadata precedence, theme/accent updates, unload/remount, caller overrides,
visible input and raster behavior, scaling and platform/package integration.
The reference contains 154 original browser states and 58 complete PNGs.
Foreground verification remains pending while the desktop is in use; hidden
geometry and property checks do not prove visible rendering or input.

The current packaged x64 hidden run matches every rectangle and checked paint
property across all 154 states, with zero geometry error. Its 28 ownership and
lifecycle checks cover live header/accent/name bindings, edited original
controls, retained scroll, original template models and selectors, template
removal/reapply, and unload/remount. The run stayed invisible through 968
guards. Full raster, visible input, actual scaling, native ARM64 and final
integration acceptance remain open.

Both catalog architectures and independently restored package consumers compile
without warnings/errors. The x64 package also replays the existing hidden
Slider and Theme Toggle checks. Thirty-five corrupted FeedColumn receipts are
rejected, including altered title text, geometry, scroll, paint, metadata,
ownership results, architecture, source/assembly hashes and isolation claims.
