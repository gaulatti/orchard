# Timeline

`BleeckerTimeline` implements the frozen Bleecker `Timeline` component at
`a86059013cd59684ce452849278ea81e734f2066`, source SHA-256
`d070c4100c562241a3cc14585c7ddc30fa4be5bd0c96bb5b9ad7303734333f61`.
It displays consumer-owned history. It has no selection, navigation, automatic
status progression or generated actions. An embedded icon control keeps its
own focus, command, keyboard behavior and accessible name.

```xml
<orchard:BleeckerTimeline AutomationProperties.Name="Editorial record history">
 <orchard:BleeckerTimeline.Items>
  <orchard:BleeckerTimelineItem Id="created" Title="Record created"
      Timestamp="09:15" Description="Fictional local metadata" Status="Completed" />
  <orchard:BleeckerTimelineItem Id="review" Title="Editorial review" />
 </orchard:BleeckerTimeline.Items>
</orchard:BleeckerTimeline>
```

`Items` is an observable collection of `BleeckerTimelineItem`, with exact unique
IDs, required string titles, optional descriptions/timestamps, nullable
`BleeckerTimelineStatus` and consumer icon content. Metadata changes notify the
control. A null status means Active at index zero and Pending at later indices;
explicit Completed, Active, Pending or Error remains consumer-owned. Reordering
updates the default status and ordinal while preserving a retained key's row.
Invalid statuses, duplicate/null items and unsupported icon models fail explicitly.

A null icon uses the current one-based ordinal. Strings and numeric icons render
as text; booleans have no glyphs, and zero remains visible. A native `UIElement`
retains its original instance. Other view models require `IconTemplate`, whose
data context is the original model. Template replacement, removal and detachment
release old consumer content. Model strings stay unchanged; rendering collapses
CSS whitespace. Empty descriptions/timestamps are absent; whitespace strings
retain their source margins/gaps with zero-height text boxes.

The icon frame is 36×36 DIPs, radius nine, with a one-DIP translucent white
border, status fill and the source's 0/1/2-DIP deep-sea shadow at 8% opacity.
Pending fills and connectors use sand at 20%. Palette brushes are centralized
in `TimelinePalette.xaml`, including a deliberate Windows contrast adaptation.
Supply scoped resource overrides before template creation. Once resolved,
mutating the shadow brush's `Color` or `Opacity` repaints the composition shadow;
subscriptions are removed on detachment and template replacement. Theme resources
are evaluated at load and theme changes, following
[Microsoft's resource contract](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/xaml-theme-resources).
The source's `ring-*` classes specify color without a ring width; no extra
ring is drawn. Rows use a 16-DIP icon/content gap and 28-DIP bottom padding,
except the final row. Connectors start at y=32 and extend by the full row height.

At viewport widths below 640 DIPs, the header stacks title and timestamp with
a four-DIP gap. At exactly 640 and above, it places them on one line, centers
their line boxes vertically and distributes remaining horizontal space. This
follows `XamlRoot.Size.Width`, including a narrow Timeline in a wide window.
Title and timestamp flex-shrink to their minimum content widths. The body can
overflow a narrow control because the source does not set `min-width:0`.
RTL mirrors icon/content and header ordering. The connector intentionally stays
at physical left=17, matching the original absolute CSS coordinate.
Text uses the control's explicit reading direction, including mixed Latin,
Japanese and Arabic paragraphs.

Typography uses Encode Sans Medium 14/20 for titles, Libre Franklin 11/16.5
for timestamps and 14/24 for descriptions, and Encode Sans Semibold 12/16
for default ordinals. Scalar icon text inherits Encode Sans Regular 16/24.
Native font/display scaling uses the control's typography properties. The
automation hierarchy exposes a list and ordinal list items, with resolved
status and original consumer controls. Component automation/theme behavior and
complete scaling acceptance require native runtime evidence.

`TimelinesFixture` registers normal, explicit-status, single, empty, long,
narrow, RTL, raw whitespace, scalar-icon, consumer-action and timestamp cases.
`scripts/reference/capture-timeline.cjs` renders the unchanged frozen component
with pinned React, CSS, Edge, fonts, lockfile, esbuild and Playwright. The
72-case source matrix spans both themes and viewports 639, 640 and 1024;
six source action checks exercise pointer, Space and Enter. Its source geometry,
first baselines and unchanged PNGs are recorded in `tests/Reference/Timeline.json`.
The native boundary audits 534 library-owned first-line baselines, including
ordinals, scalar icons and text that overflows the icon frame. Consumer-button
typography is recorded separately. `scripts/verify-timeline.mjs` independently
checks source boxes, baselines, input receipts and viewport pixels; both native
CI jobs run it after each viewport matrix.
Native source captures rehost the original frame at integer screen coordinates,
capture the catalog's own foreground client, and crop the measured viewport
without scaling or color conversion. The original client images and coordinates
are retained; the image audit checks every cropped RGBA pixel against them and
verifies restoration of the original parent. Empty frames use the explicit
bitmap path. RenderTargetBitmap's overflow-expanded, fractionally shifted RTL
captures are retained as failed capture evidence and do not replace source PNGs.

Acceptance requires matching source geometry and text baselines, retaining
consumer icon actions/templates and live metadata through reorders/replacements,
unload/remount and theme changes, preserving normal disabled/automation behavior,
and exercising an actual 639→640→639 window resize. Run
`scripts/smoke-catalog.ps1 -Component timeline -ClientSize 1024,900`, then repeat
at widths 639 and 640. Exact image comparison, full-suite preservation,
independent package use, native ARM64, component theme/automation behavior, scaling and override
coverage remain required. The component remains in progress until that evidence
satisfies the fixed inventory.

Initial x64 evidence at `artifacts/timeline-matrix-v9/{1024,639,640}` passes
all 72 cases and 534 library-owned baselines. Maximum geometry error is
0.021729 DIP and maximum baseline error is 0.000021 DIP, within the original
0.05-DIP bound. Each process verifies original pointer/Space/Enter actions,
inherited disabled state, keyed replacement/reordering, ordinal defaults,
template replacement, detached updates, original template binding, signed zero,
scoped/mutated shadow brushes and a real 639→640→639 window resize.

The independent viewport audit verifies all 66 nonempty screen crops against
the original client pixels and checks restoration. Its four negative cases
reject fractional coordinates, an unrestored parent, a shifted baseline and
wrong client pixels. All 112 unit tests and token generation pass. The unchanged
zero-tolerance comparison still finds 66/72 differing images: 438,044 pixels and
34,943,600 absolute RGBA error. The six empty frames match. Exact font/shape,
shadow/alpha paint and remaining visual/platform matrices stay open.

Retained initial failures include `artifacts/smoke-timeline-v1` (rounding an
intrinsic text width down caused extra wrapping),
`artifacts/timeline-matrix-v4/1024` (a resource was supplied after template
evaluation) and `artifacts/timeline-matrix-v5` (overflow-expanded bitmap bounds).
Earlier mixed-direction captures at `artifacts/timeline-matrix-v6` are superseded
by explicit paragraph reading direction. The current source matrix adds long
and wrapping scalar icons. These checks establish the current implementation
boundary, while clean release/package/full-suite and final component acceptance
remain separate evidence. The public wiki remains unavailable; documentation
is maintained in this repository.

The clean `1f85bbc` published matrix at
`artifacts/timeline-release-matrix/{1024,639,640}` reproduces all 534 baselines,
66 viewport crops and the exact initial image totals. The full published x64
sequence passes in 447.8767607 seconds with 1,128 captures, preserving all 1,104
previous names and 174 protected glyph hashes. Its audit verifies 2,006 pointer
moves/presentation gates, 1,752 owned key pairs and 72 physical clicks. Maximum
gate: 231.9245 ms under the unchanged two-second deadline. All 324 Stepper and
132 wide DataList baselines remain within their original bounds. The full
watchdog stays eight minutes. Collection performance is a separate acceptance
requirement. Complete interactive-icon/focus, arbitrary-content/template,
live-theme/resource-override, scaling, component automation behavior and native ARM64
matrices remain open.

The fresh independent `artifacts/package-consumer-timeline-x64` verifies both
themes, original native icon activation, keyed updates and ordinal status.
The ARM64 package consumer has build-only evidence. Both temporary consumer
caches are removed. Versioned catalog ZIPs match every published file; the
[current checkpoint](native-validation-checkpoint.md) records separate package,
archive, architecture and runtime hashes/scope. Push-triggered Actions remain
paused and the component remains in progress.
