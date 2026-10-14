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
empty strings collapse; whitespace remains consumer content. ASCII-whitespace-only
text keeps its heading/paragraph peer and margins but allocates zero text height,
matching CSS whitespace collapse. The passive
section has no keyboard stop and preserves child focus and commands.

The header stacks its text and optional action with an 8px gap below a 640px
window viewport. At 640px and above, the action occupies the trailing edge,
aligned with the bottom of the text. This breakpoint follows the window's
logical viewport, including when the section itself is narrower. Header-to-body
spacing is 20px; removing the entire header removes that gap. With an action
alone, the source retains its empty leading text group and the stacked 8px gap.
The source defines no loading, selection, error or motion behavior; those states
belong to consumer content. Disabling the control disables its child actions.

Inherited Content/ContentTemplate and Action/ActionTemplate accept consumer
content without copying it. Use a StackPanel for several actions and set its
8px spacing explicitly. HeaderStyle targets the header Grid, corresponding to
the source's header class override. Normal surface overrides consume Background,
BorderBrush, BorderThickness, CornerRadius and Padding; Foreground supplies the
title color. A replacement ControlTemplate owns its layout and semantics.
Theme resources retain the established system-aware contrast mappings.

`tests/Reference/DashboardSectionLayout.json` freezes 30 Light/Dark browser
cases using the exact source, pinned React 19.2.4 dependencies and bundled
fonts. Capture SHA-256:
`a4a413ba03475a2176d755662d82a88dce52b071e2ec0b52dd2193e3e255c166`.
`scripts/derive-dashboard-section-reference.mjs` validates the pin and case set
before importing a capture. The registered DashboardSectionsFixture checks
viewport geometry, optional/empty content, RTL, multilingual text, typography,
real heading peers, live bindings, remount/template replacement and physical
Tab/Space traversal through consumer actions and content. It runs in focused
and full catalog modes, and is registered for both hosted architectures.
Hosted execution remains deferred while push Actions are paused.

Focused local x64 verification passes both themes at
`artifacts/smoke-dashboard-section`, including all 30 captured geometry cases,
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

Complete glyph/baseline raster and whitespace-run comparison, flex minimum-content constraints,
the complete consumer override matrix, actual contrast schemes, Narrator,
display/text scaling and native ARM64 validation remain open. The inventory
entry remains in progress. This native passive composition has no backend,
configuration or telemetry impact. The public wiki remote is unavailable;
documentation is maintained here.
