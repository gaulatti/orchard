# BleeckerFeedColumn

`BleeckerFeedColumn` is a reusable feed surface with a fixed header, optional
numeric count and filter slot, and an independent native scrolling body. The
default width and minimum width are 372 DIP. Its height stretches within a
bounded parent. The original three-stop 45-degree Oklab gradient, accent dot,
border, count pill and shadows belong to the component's presentation.
Feed Column is accepted for the 2026-10-29 x64 milestone. The exact preview
package passes Light/Dark states, native 150% no-clipping, static reduced-motion
behavior and independent consumer compilation. The catalog's physical pointer,
keyboard, focus and scroll checks remain valid because the component and fixture
are unchanged since that checkpoint. See `docs/parity-inventory.json` for the
five pass records. ARM64 compiles but its runtime remains unverified;
raster/pixel parity is not a milestone gate.
The gradient remains dark in either host theme. The default control requests
Orchard's dark theme for its header and content subtree so text, accents and
composed controls stay readable in a Light host. Callers can set
`RequestedTheme` explicitly when replacing the default surface recipe.

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
The plain `Title` display uppercases using the control's live `Language`
(for example, `istanbul` becomes `İSTANBUL` under `tr-TR`). The original
`Title` and default group name remain caller-owned.
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

The original implementation contract requires pinned header/wrapping/count/filter/body geometry,
independent scrolling, live bindings, retained slot identity, templates,
metadata precedence, theme/accent updates, unload/remount, caller overrides,
visible input and raster behavior, scaling and platform/package integration.
The reference contains 154 original browser states and 58 complete PNGs.
Foreground x64 catalog checks now cover physical pointer and keyboard input,
inner scrolling, header updates and retained caller-owned filter/body state.
Light and Dark host appearances were inspected. A Light-host contrast defect
was corrected by scoping dark tokens to the dark surface; the before/after
frames are in `docs/evidence/feed-light-contrast-v1/`. Those earlier source-catalog
checks alone did not establish packaged or ARM64 runtime acceptance.

The fresh `0.1.0-feed-contrast-v2-local` x64 package ran all 154 hidden source
states and passed 29 checks with no failures, including a Light host with a
dark Feed Column containing a Feed Item. Its 975 isolation guards stayed
invisible. The adjusted reference audit passed with zero geometry error and
no paint errors; it compares the Light-host default accent with the source's
dark accent token because the column has a dark surface. Thirty-five corrupted
receipts were rejected. Ownership and lifecycle checks cover live
header/accent/name bindings, edited original controls, retained scroll,
original template models and selectors, template
removal/reapply, and unload/remount. That earlier checkpoint predated native
150% and final package qualification; these are recorded for the current x64
milestone above. Native ARM64 runtime remains open.

Previous x64 and ARM64 catalog and package-consumer builds passed. Native ARM64
runtime remains unverified for this milestone.
