# Empty source contract

`BleeckerEmpty` is accepted on x64 under the current
[five-check milestone contract](acceptance-contract.md). The matching catalog
passed 120 Light/Dark state frames and twelve lifecycle phases; the independent
NuGet consumer compiled and ran, including twelve native 150% no-clipping
cases. The control has no animation or transition, and reduced-motion states
passed without changing OS settings. ARM64 builds but its runtime is unverified.

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
The native check verifies 120 reference states and twelve lifecycle phases:
live consumer input/focus, detach/commands, explicit minimum/style ownership,
and original models through templates and selectors. The focused catalog route
also exercised physical pointer activation in both themes. The independent
package consumer passed twelve native 150% no-clipping cases across Light/Dark
and 400/900 logical-pixel viewports. Older exact-PNG reports remain historical
research, not component acceptance gates.

`tests/Reference/Empty.json` freezes Bleecker
`a86059013cd59684ce452849278ea81e734f2066`, component hash
`d11e150b9241e508fa81b769e645469db54838bb96530f640ee2cea978f85209`.
The unmodified source renders 120 frames: thirty content/layout cases in both
themes and both motion preferences, plus twelve retained consumer-input updates.
The collector pins React 19.2.4, esbuild 0.25.12, Playwright 1.62.1 and Edge
155.0.4283.62, with the exact regular and semibold font bytes.

The source contract implemented by the native control includes:

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
- Exercise the registered catalog fixture, consumer-owned action, native x64
  scaling and package-consumer compilation under the current milestone contract.

The frozen source tests verify these distinctions and every PNG/collector hash.
They establish the intended behavior for implementation, not native parity.

The control refreshes title/description text when `FontFamily` changes,
clearing old Orchard script faces for a consumer font and restoring them when
Encode Sans returns. The registered custom example uses Arial with mixed
CJK/Arabic text.
