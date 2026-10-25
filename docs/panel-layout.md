# BleeckerPanelLayout and BleeckerPanelColumn

`BleeckerPanelLayout` places caller controls in one horizontal row. Fixed
children keep their natural or explicit width. Growing children share the
remaining width equally, with no gaps or wrapping. Excess fixed width scrolls
horizontally; vertical overflow is clipped.

`BleeckerPanelColumn` stacks children vertically. Fixed children keep their
natural or explicit height; growing children share the remaining height. A
column clips overflow rather than adding another scroll viewport. Each
`BleeckerPanel` retains its independently scrolling body.

```xml
<orchard:BleeckerPanelLayout Height="480">
    <orchard:BleeckerPanel Title="Fixed column" Width="240">
        <orchard:BleeckerInput Text="{Binding Filter, Mode=TwoWay}" />
    </orchard:BleeckerPanel>
    <orchard:BleeckerPanelColumn Grow="True">
        <orchard:BleeckerPanel Title="Summary" Height="160" />
        <orchard:BleeckerPanel Title="Remaining height" Grow="True">
            <orchard:BleeckerInput Text="{Binding Value, Mode=TwoWay}" />
        </orchard:BleeckerPanel>
    </orchard:BleeckerPanelColumn>
</orchard:BleeckerPanelLayout>
```

Both controls expose a read-only `ObservableCollection<UIElement>` named
`Children`, also their XAML content property. Add, remove or move original
native elements in that collection. Growth and size changes retain their
controls, values, bindings and commands. Native templates must provide a
`PART_Root` Grid. Unload detaches growth observers; remount reconnects them.

`BleeckerPanel.Grow` and `BleeckerPanelColumn.Grow` are consumed by their parent
layout. For another native element, use the attached
`BleeckerPanelLayout.Grow="True"` property. Its static setters/getters also
read and write the corresponding Panel/Column property. Growth has no effect
outside these containers. Explicit child dimensions still apply: clear `Width`
or `Height` when the original fixed dimension should become flexible.

`Padding` uses native DIPs and defaults to zero. Native dimensions, margins,
alignment and RTL follow the caller's WinUI layout. The source's arbitrary
CSS class strings are represented by native layout properties, without a CSS
class parser. Text/model content must be supplied as a native element such as
TextBlock or ContentPresenter. The containers add no action or unconditional
Tab stop; original child focus and naming metadata remain available. A plain
overflowing horizontal viewport can receive keyboard focus when no caller
child is focusable.

The Panel layouts fixture demonstrates both controls with fictional local
values. The frozen Bleecker source contains 86 reference layouts across both
themes, including fractional tracks, RTL, empty/hidden children, nested
columns, padding and constrained overflow, with 34 complete original PNGs.
Current native and package evidence is recorded in runtime-evidence.md. This
preview remains subject to actual scaling, native ARM64 and final integration
acceptance.

The current focused native run matches all 86 layouts exactly. Both themes
retain edited text, focus and child lifetime across growth/collection moves,
as well as commands, templates and scroll position. Plain overflow responds to
native Right Arrow input. Twenty-three corrupted reports/images are rejected.
Eighteen full reference screenshots match exactly; sixteen retain 12,082
differing pixels. These raw differences remain open and are not masked.

Both catalog architectures build without warnings/errors. A fresh x64 NuGet
consumer passes both themes, including compiled XAML attached growth and live
attached bindings; the independent ARM64 consumer compiles the same API. Its
temporary source and package caches are removed after validation. Native ARM64
execution and a complete catalog replay remain unverified for this package.
