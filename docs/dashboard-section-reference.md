# Dashboard Section

`BleeckerDashboardSection` implements the pinned Bleecker
[`DashboardSection`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/dashboard-section.tsx).
Source SHA-256: `f8af75ecc48d211202e4aa0022e81195259fc934777c28a180ea7f325bce7fe6`.

```xml
<orchard:BleeckerDashboardSection Title="Monitoring" Description="Fictional queue summary">
    <orchard:BleeckerDashboardSection.Action>
        <orchard:BleeckerButton Content="Refresh" Command="{Binding RefreshCommand}" />
    </orchard:BleeckerDashboardSection.Action>
    <TextBlock Text="{Binding QueueSummary}" />
</orchard:BleeckerDashboardSection>
```

The title uses bundled Encode Sans Medium, 18px/28px with −0.02em tracking,
and a native level-two heading peer. The description uses Libre Franklin
14px/20px with a 4px top margin, including when there is no title. Optional
empty strings collapse; whitespace remains consumer content. Space/tab/CR/LF-only
text keeps its heading/paragraph peer and margins but allocates zero text height,
matching CSS whitespace collapse. The passive
section has no keyboard stop and preserves child focus and commands.
Display text collapses space, tab, CR and LF runs while the original Title and
Description properties remain unchanged. NBSP remains nonbreaking. A form feed
is omitted from the native display, matching the source's nonprinting glyph
instead of WinUI's hard line break. Heading names are checked against the
frozen browser accessibility tree, including the source's whitespace result.

The header stacks its text and optional action with an 8px gap below a 640px
window viewport. At 640px and above, the action occupies the trailing edge,
aligned with the bottom of the text. This breakpoint follows the window's
logical viewport, including when the section itself is narrower. Header-to-body
spacing is 20px; removing the entire header removes that gap. With an action
alone, the source retains its empty leading text group and the stacked 8px gap.
The source defines no loading, selection, error or motion behavior; those states
belong to consumer content. Disabling the control disables its child actions.

The intrinsic action retains its width in a constrained row. Text shrinks to its
native word minimum and then overflows, retaining the 8px gap instead of
overlapping the action or splitting an unbroken word. The minimum comes from
the pinned Win2D 1.4.0 `CanvasTextLayout.GetMinimumLineLength` with bundled font,
weight, style, stretch, tracking, direction and text scale. Intrinsic widths
round outward to 1/64px allocation units, verified against the captured browser
word boundaries. The existing output font path is resolved to a file URI for
Win2D's unpackaged font resolver; unsupported/missing font files fail rather
than silently measuring another font. Measurement resources are disposed and
each text slot caches only its current metric key and width. Wrappable action
content still requires the complete minimum-content and override matrix.

Inherited Content/ContentTemplate and Action/ActionTemplate accept consumer
content without copying it. Use a StackPanel for several actions and set its
8px spacing explicitly. HeaderStyle targets the header Grid, corresponding to
the source's header class override. Normal surface overrides consume Background,
BorderBrush, BorderThickness, CornerRadius and Padding; Foreground supplies the
title color. A replacement ControlTemplate owns its layout and semantics.
Theme resources retain the established system-aware contrast mappings.

`tests/Reference/DashboardSectionLayout.json` freezes 50 Light/Dark browser
cases using the exact source, pinned React 19.2.4 dependencies and bundled
fonts. Capture SHA-256:
`33ee53b0ec5077674c0ee8a3360e5c127752e28e7d9793f19b650ec15e07ed60`.
`scripts/derive-dashboard-section-reference.mjs` validates the pin and case set
before importing a capture. The registered DashboardSectionsFixture checks
viewport geometry, optional/empty content, RTL, multilingual text, typography,
real heading peers, live bindings, remount/template replacement and physical
Tab/Space traversal through consumer actions and content. It runs in focused
and full catalog modes, and is registered for both hosted architectures.
Hosted execution remains deferred while push Actions are paused.

Focused local x64 verification passes both themes at
`artifacts/smoke-dashboard-section`, including the original 30 captured geometry cases,
palette/font checks, bound title updates, content removal/restoration,
data templates, remount, template replacement, HeaderStyle/padding overrides,
disabled-child enforcement and physical forward/reverse Tab and Space.
The source/native layouts compare section/header/body/action boxes and text
line allocations. RTL compares the text group's trailing anchor independently
of font shaping; the reported native left-edge residual for the full RTL
specimen is 0.0205078125px. That residual remains an open shaping gap, not a
platform substitution. Pixel checks verify the real consumer content/action
fills. Native and browser RTL PNGs were inspected; complete glyph comparison
remains open. The host ran at display/text scale 1 with animation effects off.
The full local Light/Dark catalog also passes at
`artifacts/smoke-dashboard-section-full` within the existing six-minute deadline.
Both solution architecture builds pass with zero warnings; local ARM64 evidence
is a cross-build. All 42 regression tests and the pinned token check pass.

The expanded 50-case focused checks pass at
`artifacts/smoke-dashboard-section-constraints`. All original 30 geometry records
are unchanged; new records cover fixed actions in narrow sections, minimum-word
overflow, unbroken identifiers, NBSP and whitespace runs. The previous 280px
specimen misplaced its action by 10.61932373046875px; it now preserves the
source's 80px action at x=200. The frozen word minima and overflowing action
positions pass the same 0.001px geometry assertions. Live width/text changes
exercise the same path. Both themes' composed-window checks verify that the
action actually paints beyond a 160px section edge. Paint-width captures retain
this overflow; element-only captures do not by themselves prove composition.
The full local Light/Dark catalog also passes at
`artifacts/smoke-dashboard-section-constraints-full` with the six-minute deadline
unchanged. All 42 regressions and the token check pass for this correction.

Fresh package 0.1.0 from clean implementation commit
`d6d688dd7c3e1eff9a2daff7ddb0175a9f9a0b67` has SHA-256
`6dc5222d32c1de03906c39ca5db1788d5cf0852859a43f7ce87b624476802f3a`.
The isolated x64 consumer passes both themes at
`artifacts/package-consumer-dashboard-section`, checking bundled typography,
real level-two heading semantics, empty-header/body collapse and live content.
The isolated ARM64 consumer cross-build passes at
`artifacts/package-consumer-dashboard-section-arm64`, with native runtime
verification explicitly false. Both consumers remove their own temporary
caches. The separately published x64 catalog passes the focused runtime gate
at `artifacts/smoke-dashboard-section-published`.

The constrained-layout/whitespace correction is independently verified with a
fresh 0.1.0 package from clean commit
`ccb228b63d966d157f7a3f03520de91cc6ded44e`, SHA-256
`42d3eca0da8d827a7bccbafbded272825a71ddcc8885ca9ea029d7a963b62cde`.
The x64 consumer passes both themes at
`artifacts/package-consumer-dashboard-section-constraints`, including real font
measurement, display whitespace and the fixed 80px action at x=200 in a 280px
section. The ARM64 consumer cross-build passes at
`artifacts/package-consumer-dashboard-section-constraints-arm64` and explicitly
does not claim native runtime verification. Both isolated caches are removed.
The published x64 catalog passes all 50 focused cases at
`artifacts/smoke-dashboard-section-constraints-published`. Hosted/native ARM64
validation remains deferred while automatic push Actions are paused.

Complete glyph/baseline raster comparison, wrappable-action minimum-content constraints,
the complete consumer override matrix, component theme contrast,
display/text scaling and native ARM64 validation remain open. The inventory
entry remains in progress. This native passive composition has no backend,
configuration or telemetry impact. The public wiki remote is unavailable;
documentation is maintained here.
