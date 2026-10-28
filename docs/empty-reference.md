# Empty source contract

`BleeckerEmpty` is an in-progress native preview. Its source/native layout and
lifecycle checks pass; full visual/platform acceptance remains unverified.

```xml
<orchard:BleeckerEmpty Title="No items" Description="Create an item to get started.">
    <orchard:BleeckerEmpty.Action>
        <Button Content="Create item" Command="{Binding CreateCommand}" />
    </orchard:BleeckerEmpty.Action>
</orchard:BleeckerEmpty>
```

`Title` and `Description` are nullable strings. `Icon` and `Action` retain their
original UIElements, scalar values or template models; `IconTemplate`,
`ActionTemplate` and their selectors provide native model presentation. Assign
a template before its model. The default icon applies only when `Icon` is null.
`Spacing` defaults to 20 and maps the source outer column gap. A custom control
template must provide the `PART_LayoutHost` Grid.

The default padding is 32px horizontally and 64px vertically. Its border-box
minimum is therefore 66 × 130px. Native defaults track padding/border changes
before WinUI clips its desired size; explicit consumer minimum values and style
setters keep ownership. Source-constrained parent cases use a native block
panel that offers the parent width and arranges a larger child minimum without
clipping. A native zero Width on the child represents a different constraint.

The title exposes heading level 3. The passive parent adds no Tab stop or Invoke
provider, and consumer actions retain their own keyboard and command behavior.
The generated palette uses source light/dark colors and paired system colors
in Windows contrast. Both surface-shadow layers come from the frozen token
recipe and are suppressed in contrast mode.

The title consumes the source refined tracking (-0.02em). Centering divides
signed 1/64px layout units toward zero, including overflowing content. RTL
receipts measure transformed bounds rather than the mirrored local origin.
The native check verifies all 120 reference states and twelve lifecycle phases
with zero asserted geometry error: live consumer input/focus, detach/commands,
explicit minimum/style ownership, and original models through templates and
selectors. Complete PNG comparisons retain zero tolerance, no masks and no
resampling; they still report raster differences rather than accepting them.
All twelve corrupted layout, heading, content-presence, paint and completeness
reports are rejected. Version `0.1.0-4d820a9` passes both catalog builds and a
fresh independent x64 NuGet consumer in both themes; the ARM64 consumer builds
with runtime explicitly unverified. Both generated consumer workspaces and
package caches are removed. Both catalog ZIPs verify every published entry
(537 x64 files and 533 ARM64 files).

Remaining acceptance includes complete raster/typography/shadows, arbitrary
Unicode and remaining template/override behavior, isolated scaling, component
theme resources, external UIA, collection measurements and native ARM64 runtime. The
current complete catalog run also needs a connected mouse for its unchanged
global cursor checks. Push-triggered Actions remain paused.

`tests/Reference/Empty.json` freezes Bleecker
`a86059013cd59684ce452849278ea81e734f2066`, component hash
`d11e150b9241e508fa81b769e645469db54838bb96530f640ee2cea978f85209`.
The unmodified source renders 120 frames: thirty content/layout cases in both
themes and both motion preferences, plus twelve retained consumer-input updates.
The collector pins React 19.2.4, esbuild 0.25.12, Playwright 1.62.1 and Edge
155.0.4283.62, with the exact regular and semibold font bytes.

Acceptance criteria for the native implementation:

- Preserve the passive outer container and consumer icon/action objects,
  bindings, templates, commands, keyboard order and accessible descendants.
- Match the 12px outer radius, 1px border, 32px horizontal/64px vertical padding,
  20px column gaps, theme colors and visible overflow.
- Render the default 56px icon surface with its 10px radius, 32px source SVG,
  foregrounds and both measured surface-shadow layers.
- Match the 16px/24px semibold title and 14px/22.75px description, centered
  shrink-to-fit body, 320px description maximum and whole-word wrapping.
- Preserve the source presence boundaries. A null/undefined icon selects the
  default graphic; either Boolean icon renders empty; numeric zero renders `0`.
  Zero, NaN and false actions are absent, while true creates an empty action
  wrapper and retains its 4px margin plus the 20px column gap.
- Preserve zero-height text boxes and their surrounding spacing. An empty
  title removes its line box; a whitespace description retains the 6px body
  spacing despite its own zero height.
- Match constrained parents rather than clipping or shrinking intrinsic
  content. The zero-width source parent produces a 66px outer border box and
  an overflowing body wider than that box.
- Verify live title/description/icon changes without replacing the original
  input in a still-present action; verify action activation and Tab navigation.
- Add registered native catalog, boundary regressions, independent package and
  CI coverage, then exercise x64 and native ARM64, raster, typography, component
  theme resources, scaling, motion preferences, UIA and lifecycle/override acceptance.

The frozen source tests verify these distinctions and every PNG/collector hash.
They establish the intended behavior for implementation, not native parity.

The current version `0.1.0-77f387a` also passes the full 477-second x64 replay and a fresh x64 consumer in both themes. Native ARM64 runtime and complete component acceptance remain open; see the [current checkpoint](native-validation-checkpoint.md).

The current local candidate refreshes unchanged title/description text when
`FontFamily` changes, clearing old Orchard script faces for a consumer font
and restoring them when Encode Sans returns. The registered custom example
uses Arial with mixed CJK/Arabic text. Live transitions run in the hidden
package diagnostic; visible runtime/raster replay remains required.
