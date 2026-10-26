# BleeckerPanel

`BleeckerPanel` is a titled surface with a fixed header and independently
scrolling caller content. Its optional filter, toolbar and handle slots retain
the caller's controls, input values, bindings and commands.

```xml
<orchard:BleeckerPanel Title="Local updates" Count="{Binding VisibleCount}"
    Width="360" Height="480" Accent="#407FA3">
    <orchard:BleeckerPanel.Filter>
        <orchard:BleeckerInput Text="{Binding Filter, Mode=TwoWay}" />
    </orchard:BleeckerPanel.Filter>
    <orchard:BleeckerPanel.Toolbar>
        <orchard:BleeckerButton Content="Consumer action" Command="{Binding Action}" />
    </orchard:BleeckerPanel.Toolbar>
    <StackPanel Spacing="16">
        <TextBlock Text="Consumer content" Style="{StaticResource BleeckerBodyStyle}" />
        <orchard:BleeckerInput Text="{Binding Value, Mode=TwoWay}" />
    </StackPanel>
</orchard:BleeckerPanel>
```

The source geometry is a physical 2 DIP left accent, 1 DIP right border, a
transparent body surface, and a square header with a 56 DIP minimum title row.
Titles render uppercase at 11/16.5 DIP, semibold with 0.1em tracking. The
plain title uses the control's live `Language` for casing; `istanbul` renders
as `İSTANBUL` under `tr-TR` and `ISTANBUL` under `en-US`. `Title` and the
default group name keep the original caller string. The
optional count badge uses tabular figures, 8/2 DIP padding, a 1 DIP border and a
5 DIP radius. `Count` is nullable: null omits the badge, while zero, fractions,
negative values, NaN and infinities retain the source number formatting.
Bindings can supply boxed numeric primitives through the object-backed DP.

The filter has 20 DIP side padding and 16 DIP bottom spacing. The toolbar is
centered below it, with 20/12 DIP padding and a top border. For multiple toolbar
controls, supply a horizontal panel with 8 DIP spacing. Untemplated false,
numeric zero, empty strings and null omit optional slots; explicit templates
and selectors can render those models and receive their original values.
`Content`, `Filter`, `Toolbar` and `DragHandle` support their corresponding
templates/selectors. Model objects require an explicit template or selector.

`Accent` accepts a solid native brush, including opacity; null uses the scoped
sea token. The dot and title update immediately. Border colors use the source
200ms default curve, while handle foreground uses 190ms premium timing.
Reduced motion snaps to the destination. Changes to the original brush are
observed without modifying it. Header updates retain unchanged slot presenters
and body scroll position. Brush observers and frame callbacks detach on unload;
replacing parts releases the owned cursor resources.

`DragHandle` supplies an integration slot; caller input and drag/drop behavior
remain with its content. Windows uses its four-way move cursor for the source
grab affordance; a child's own cursor still applies. `Grow` is consumed by
[PanelLayout/PanelColumn](panel-layout.md) and has no standalone visual effect.
The deprecated `IsDragging` and sole
`Variant="Monitor"` value retain their source behavior and introduce no mode
switch or disabled state. Native `Width`/`MinWidth` use DIPs; omit `Width` to
follow the parent's native layout.

The parent adds no Tab stop or command. Its group name follows `Title` unless
caller naming metadata supplies it. A plain overflowing body is keyboard
focusable and supports PageDown/Home; an editable body retains its own focus
path without an extra viewport stop. RTL reverses header and toolbar content
while preserving the physical left accent. Short panels clip their overflow
and leave the header slots intact.

The Panels catalog fixture uses fictional local values and actions. The frozen
reference contains 266 source states, 58 original full screenshots, two live
input sequences, two keyboard sequences and 14 border timeline samples.
Windowless native font tests also verify tabular counts and kerning against
those browser widths. Current runtime, raw pixel differences and remaining
acceptance are recorded in [runtime evidence](runtime-evidence.md). Full pixel,
normal native motion, broader overrides, actual scaling, native ARM64 and final
integration acceptance require current evidence before this preview is accepted.

The current preview has zero measured error across all 266 layouts and matching
paint values. Native editing, actions, binding/template/lifecycle, scroll
retention, focus and unbounded-width checks pass in both themes. Twenty-nine
corrupted reports/images are rejected. Full comparison retains 661,516
differing pixels across 58 raw screenshots. Both catalog builds pass without
warnings/errors. A fresh independent package passes x64 runtime in both themes
and ARM64 compilation, with temporary sources/caches removed. Field and
DetailLayout regressions pass with the new tabular-kerning bridge.
