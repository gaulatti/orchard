# ProgressStack native preview

`BleeckerProgressStack` displays consumer-owned `Items`, an optional `Max`,
and a wrapping legend (`ShowLegend`, true by default). Each observable
`BleeckerProgressStackItem` retains its `Label`, numeric `Value`, and optional
`Color` brush. Missing colors remain transparent. Replacing, reordering,
removing and editing items update the display without adding a command or
keyboard stop. An explicit automation name labels the passive group.
Collection and item updates use the control's owning UI thread.

```csharp
var stack = new BleeckerProgressStack { Max = 20 };
stack.Items.Add(new BleeckerProgressStackItem
{
    Label = "Published", Value = 6, Color = publicationBrush
});
stack.Items[0].Value = 8;
```

The same optional maximum can be set declaratively with `Max="20"`.
An item retains the original numeric bits, including negative zero, while
its visible label and tooltip use the source's Number text (`0`).

The pinned Bleecker source is `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/progress-stack.tsx`, SHA-256
`101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254`.
The original source defines a 12px track, 12px track/legend and wrapping gaps,
10px swatches, 6px item gaps and 12px/16px legend typography. Values use React
Number text, including zero, nonfinite numbers and exponent boundaries.

An omitted maximum sums the values in input order. A supplied maximum is
authoritative, including zero, negative and nonfinite values. Invalid or
negative CSS widths occupy no segment width; overflowing positive segments
shrink in proportion to their flex basis. A visible empty legend retains the
12px inter-block gap. A zero-width track still permits legend overflow.
The width behavior follows the [CSS flexible length algorithm](https://www.w3.org/TR/css-flexbox-1/#resolve-flexible-lengths)
and is verified against the pinned component's browser output.
Chromium's [length representation](https://github.com/chromium/chromium/blob/main/third_party/blink/renderer/platform/geometry/length.h)
stores percentages as floats. The frozen small-share case verifies that
rounding before allocation matters: the remaining share can become 100%.

Consumer bar customization uses `TrackHeight`, `CornerRadius` and `Background`.
The inherited font properties and foreground customize value typography;
labels retain the theme's secondary foreground. Brushes keep their identity.
There is no component-owned transition. Tooltips expose the original label
and Number text; independent native tooltip interaction is still unverified.

`ProgressStacksFixture` registers default, explicit maximum, hidden-legend
and wrapped cases. `tests/Reference/ProgressStack.json` freezes 140 browser
frames and sixteen live update receipts from the original source, with exact
collector, fonts and complete PNG hashes. The native 140-state matrix and
twelve lifecycle phases pass with zero asserted geometry error. Complete
PNG comparisons still report raster differences, with no masks, tolerance
or resampling. Version `0.1.0-77f387a` passes a fresh independent x64 consumer in both themes and an ARM64 consumer build; native ARM64 runtime is unverified. No component is
accepted from a build or a geometry report alone. Complete raster parity,
arbitrary Unicode, remaining item/bar style overrides, external UIA/Narrator,
scaling, contrast, collection measurements and native ARM64 runtime remain
acceptance work.

All thirteen corrupted audit reports are rejected, including nonbreaking-space removal. The separate whitespace-aware measurement joins compatible DirectWrite fragments before 1/64px allocation; the existing per-run measurement is preserved. Native and browser glyph receipts record the 57.53125px span width.
