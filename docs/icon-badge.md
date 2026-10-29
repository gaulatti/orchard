# IconBadge

`BleeckerIconBadge` follows `src/components/icon-badge.tsx` at Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`. It is a passive content surface.
Its children retain their own commands and keyboard focus; the badge adds no
button behavior or Tab stop. Set `AutomationProperties.Name` when the marker
needs a named Windows group. Unnamed decorative containers stay out of the
UIA control/content views while their interactive descendants remain available.

```xml
<orchard:BleeckerIconBadge Size="Medium" Variant="Subtle"
                          AutomationProperties.Name="Editorial marker">
    <TextBlock Text="A" />
</orchard:BleeckerIconBadge>
```

| Source | Windows API | Default |
| --- | --- | --- |
| `md`, `lg` | `BleeckerIconBadgeSize.Medium`, `.Large` | Large |
| `primary`, `subtle`, `outlined` | `BleeckerIconBadgeVariant` | Primary |
| `children` | `Content`, `ContentTemplate`, `ContentTemplateSelector` | Empty |

Medium is 56 × 56, large is 64 × 64; the border is 1px and radius is 10px.
The generated IconBadge palettes preserve the source fractional brush opacity
in both themes. Windows contrast uses paired system window/window-text colors.
There is no component animation or extra disabled appearance.

Content is centered and may paint outside the border, including a fixed-size
100 × 80 child. The background border is a sibling of the content host, so
rounded corners do not clip child content. A constrained native layout can use
`MaxWidth`; a fixed native block can use a Canvas. These native layout choices
represent the source distinction between a shrinking flex item and a block
that keeps its declared width. `Width`, `Height`, padding, border, radius,
consumer styles and explicit colors remain consumer-owned when size/variant
changes. Size only supplies the default desired extent.

Strings and numbers are rendered as text with source whitespace collapse and
whole-word wrapping. Null and either Boolean render empty. Double negative
zero renders `0`; NaN, infinities and exponent boundaries share the existing
React number formatter. `BigInteger` represents React 19 BigInt content.
The original `Content` value is retained. Other models require a template or
selector; assign that template before the model. UIElements are retained
directly. The default template's `PART_ContentHost` Grid receives the content
presenter; replacement templates that preserve this part retain the same child.

The catalog registers all six size/variant combinations, empty/scalar/named
content and a consumer button. `tests/Reference/IconBadge.json` freezes 136
source frames and twelve retained-child updates, including light/dark and both
motion preferences. The collector pins Edge `155.0.4283.62`; older component
references retain their original browser pins. Run:

```powershell
./scripts/smoke-catalog.ps1 -Executable <catalog-exe> -Component icon-badge -ClientSize 320,240 -Output artifacts/icon-badge -ExpectedArchitecture X64
node scripts/verify-icon-badge.mjs --native artifacts/icon-badge --source tests/Reference/IconBadge --output artifacts/icon-badge/audit.json
```

The verifier separates contract checks from complete, unmasked PNG comparisons.
It records all raster differences with zero pixel tolerance and no resampling;
passing geometry and lifecycle checks does not establish raster acceptance.
Native child Button/TextBox appearance remains an explicit platform adaptation.

The x64 focused check passes all 136 frames, 52 child placements and 80 scalar
cases with zero geometry error, plus eight live/retemplate/detach/template/color
phases. The default size now respects consumer Width/Height and min/max bounds;
the source flex-width and explicit shorter-height boundaries are regression
checked. The flat overflow frames require every captured channel to match the
complete frozen source image; the ordinary blank-frame guard is unchanged.
All eleven deliberately corrupted geometry, child color, paint, semantics,
scalar and completeness reports are rejected by the audit. Four full PNGs
match exactly; the other 132 retain reported raster differences, including
rounded-edge antialiasing, translucent compositing and text/native child paint.
Geometry assertions cover the badge and explicit child boxes. Text ink/line
layout and native editor appearance remain part of the raster acceptance gap.

Version `0.1.0-b8aa2de` is built from
`b8aa2de56d11d7f23773196a4b722d0c2fd36df9`. Both catalog architectures build;
the independent x64 NuGet consumer passes both themes, including all six
variants/sizes, retained focus, names/colors and five scalar boundaries. Its
ARM64 consumer builds with runtime explicitly unverified. Both isolated
consumer workspaces and package caches are removed. The x64/ARM64 catalog
archives verify all 537/533 entries against their published files.

The current full x64 attempt stopped at the unchanged Activity Hand-cursor
gate after 44,195.8564ms. Windows reports no present mouse and no visible
global cursor, while WinUI selects Hand. This is not a complete full-suite
pass. The last complete passing source remains `48d8fd3`; the new candidate
still needs the unchanged full test with a connected mouse.

For the 2026-10-29 x64 [component acceptance contract](acceptance-contract.md),
Icon Badge passes four of five checks and remains unaccepted. The focused
catalog route verifies all six size/variant combinations plus empty, scalar,
named and interactive content in Light and Dark. The exact `.8` NuGet package
compiles in an independent x64 consumer and passes 14 native 150% bounds cases:
six combinations per theme and a visible overflow child per theme. The
component and template have no motion. The keyboard/pointer/focus check remains
failed because the current physical route timed out acquiring foreground input
before it could click or Tab through the child controls. Earlier passing input
evidence predates a control change and is not reused. The reports are in
`artifacts/current/context-menu-native-candidate/icon-badge-states.json`,
`icon-badge-scale-v8.json` and `icon-badge-milestone-failed-v8.json`.

The earlier exact-raster diagnostics above remain historical; raster parity is
not a milestone gate. ARM64 is build-only and runtime-unverified. Narrator and
OS contrast campaigns are excluded by the current library scope.

The current version `0.1.0-77f387a` passes the full 477-second x64 replay using a scoped Windows MouseKeys virtual mouse, plus a fresh x64 consumer in both themes. MouseKeys configuration is restored after each scope. This supersedes the earlier missing-mouse replay failure; see the [current checkpoint](native-validation-checkpoint.md).

The current local candidate refreshes unchanged plain text when `FontFamily`
changes. A consumer font clears Orchard's previously selected script faces;
restoring Encode Sans restores them. The catalog adds an Arial Japanese
caption, and the hidden package diagnostic covers live font transitions in
both themes. This new behavior still needs visible runtime/raster replay.
