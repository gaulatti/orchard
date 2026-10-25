# BleeckerPageFrame

`BleeckerPageFrame` is a centered, full-available-width boundary around original
consumer content. It implements the fixed Bleecker `PageFrame` layout recipe.

```xml
<orchard:BleeckerPageFrame Element="Article" FrameWidth="Reading"
    Gutter="Comfortable" VerticalSpacing="Comfortable">
    <StackPanel Spacing="16">
        <TextBlock Text="Consumer heading" Style="{StaticResource BleeckerHeadingStyle}" />
        <orchard:BleeckerInput Text="{Binding Title, Mode=TwoWay}" />
    </StackPanel>
</orchard:BleeckerPageFrame>
```

`FrameWidth` accepts `Reading` (768 DIP), `Content` (1152, default), `Wide`
(1440), or `Full` (no recipe maximum). It caps the complete frame, including
padding. The frame centers within its parent and fills the available width up
to that cap. An explicit native `Width` still respects the maximum. The width
recipe depends on the parent; spacing breakpoints depend on the window's logical
`XamlRoot` viewport, even when the frame sits in a narrow container.

| Recipe | Below 640 | From 640 | From 1024 |
| --- | ---: | ---: | ---: |
| Gutter None | 0 | 0 | 0 |
| Gutter Compact | 16 | 20 | 20 |
| Gutter Comfortable (default) | 20 | 32 | 40 |
| Gutter Spacious | 24 | 40 | 64 |
| VerticalSpacing None | 0 | 0 | 0 |
| VerticalSpacing Compact | 24 | 32 | 32 |
| VerticalSpacing Comfortable (default) | 40 | 48 | 64 |
| VerticalSpacing Spacious | 56 | 64 | 96 |

Horizontal spacing applies equally to left/right; vertical spacing applies to
top/bottom. These are default `Padding` and `MaxWidth` values. Caller local
values, bindings and style setters take precedence. Recipe changes keep those
producers intact. `ClearValue` restores the current recipe. A same-value scalar
write cannot be distinguished from the owned default because WinUI raises no
property-change event; use an explicit binding/style for a persistent override
that initially equals the current recipe.

`Element` maps `Div`, `Article`, `Main` and `Section` to native passive grouping.
Main exposes the native main landmark; a named Section exposes a custom region.
Article/Section/Main retain their semantic item type. Caller automation metadata
has normal precedence. The frame adds no Tab stop and owns no command, data,
selection, authentication or navigation. Content, data templates, template
selectors, input values and native events remain with the consumer. Unknown enum
values fail explicitly. Standard native background, border, alignment and
template properties remain available.

The Page frames catalog entry uses fictional local input and an independent
consumer action. The source reference retains 792 layout states and 40 original
PNGs from the pinned source, covering every width/gutter/vertical-spacing
combination around both viewport breakpoints, empty content, fractional and
narrow hosts, semantic elements and explicit padding/size overrides.

The current x64 runtime matches all 792 measured layouts with zero geometry
error and all 40 complete source screenshots with zero differing pixels. Twenty
corrupted reports/images are rejected. Both themes exercise real child editing,
pointer/Space actions, live viewport changes of an existing narrow frame,
binding/style/default restoration, templates/selectors, metadata precedence and
remounts. The flat reference filler has distinct logical and pixel-snapped paint
parts to represent the browser's fractional box and opaque background; PageFrame
leaves its caller's rendering intact. Raw exact-comparison failures are retained
for diagnosis without accepting them.

Both catalog architectures build without warnings/errors. The local package
passes a fresh independent x64 consumer in both themes and an ARM64 consumer
build, with both temporary source/package caches removed. The new dictionary
also passes the existing Field runtime and all 44 measured Field states.

Broader platform acceptance remains open. Broader caller override
boundaries, actual display/text scaling and native ARM64 execution must be
supported by current evidence before this component can be accepted. See
[runtime evidence](runtime-evidence.md) and the governing
[component-library scope](library-scope.md).
