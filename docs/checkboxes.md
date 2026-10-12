# Checkbox

`BleeckerCheckbox` is an in-progress implementation of Bleecker's
[`Checkbox`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/checkbox.tsx)
at the fixed reference commit. It inherits the pinned WinUI CheckBox rather
than rebuilding its state, keyboard activation or Toggle automation provider.

```xml
<orchard:BleeckerCheckbox Content="Enable notifications"
    Description="Send alerts when a device starts playback."
    IsChecked="{Binding NotificationsEnabled, Mode=TwoWay}" />
<orchard:BleeckerCheckbox IsChecked="True" AutomationProperties.Name="Select row" />
```

The source label maps to native Content/ContentTemplate/ContentTemplateSelector.
Description accepts consumer-owned text or UI content, with an optional
DescriptionTemplate. Consumers own IsChecked and receive normal native Checked,
Unchecked, Indeterminate and Click events. The normal two-state behavior remains
the native default; a consumer can explicitly use native IsThreeState. Null
selection renders the source's unchecked chrome rather than inventing a bar.
Use AutomationProperties.Name or LabeledBy to override the default accessible
name, which includes plain label and description text. A bare checkbox needs an
explicit name. Rich content retains its consumer-owned elements.

There is one source size and no variants. The box is 20×20px, with a 1px border,
4px radius and the reference white 14×14 SVG check. The SVG's 14×10 viewBox is
centered within an explicit square viewport, preserving its round 2-unit stroke.
The check remains left-to-right inside an RTL row; only row layout follows the
reading direction. Inheriting RTL into the vector would mirror its geometry.
Label and
description use Encode Sans at 14px/20px, medium and regular respectively.
Text starts 12px after the box. Description has a 2px top margin, including
when there is no label. Both strings wrap beside a top-aligned box; empty
content collapses the text column. Intrinsic measurement preserves the larger
unwrapped label/description width before constrained wrapping.

Light unchecked uses white and sand at 60% border opacity; checked uses sea.
Dark unchecked uses deep-sea and white at 20%; checked uses accent blue.
Unchecked hover uses sea at 50%. Keyboard focus uses a 2px sea/20 ring and a
2px white/deep-sea offset; pointer focus omits the ring. The complete native
template removes Windows default typography, minimum sizes, glyph and chrome.
Disabled changes only the input chrome to 50% opacity. The checkmark and text
retain full opacity, matching the source's separate SVG and text siblings.
Windows contrast uses paired Window/WindowText and Highlight/HighlightText
colors, with a visible system-colored focus ring.

Fill, border, glyph opacity, disabled opacity and focus each have an independent
190ms premium clock. Fill and border interpolate continuous premultiplied Oklab
and share the tested CSS reversal behavior with Switch. Palette changes do not
restart unchanged property clocks. Reduced motion settles immediately; motion
also snaps while Windows contrast is enabled. Rendering/settings/palette
subscriptions detach on unload or template replacement and restore on reload.

The pinned default Storybook render measures 291.71875×42px, with 20px label and
description lines and the 12/2px gaps. The light baseline and measurements are
retained at `artifacts/checkbox-reference-default-light.{jpg,json}`. The browser
focus audit confirms a 2px offset and a 4px outer shadow spread. Its current
reduced-motion preference reduces source transition duration to 0.01ms without
changing desktop settings. These observations do not establish native parity.

The registered Checkboxes fixture covers checked/unchecked, disabled off/on,
bare, description-only, constrained long text, Japanese/Arabic, RTL, rich
consumer content and two-way binding. Native verification checks first-layout
geometry, palette, opacity separation, UIA role/name/value, exactly-once events,
disabled rejection, content updates, keyboard/pointer focus modality, hover
states, template replacement, live appearance with retained state, intermediate
glyph motion and reversals. Reports and captures are retained as
`checkbox-measurements-{Light,Dark}.json` and `checkbox-{Light,Dark}.png`, including
on failure. The independent package consumer instantiates and toggles it.

The first x64/ARM64 run at `79f1053` stopped at its glyph geometry assertion.
The x64 capture also exposed inherited RTL mirroring. The follow-up keeps the
14×10 content inside an explicit 14×14 viewport and fixes the vector's reading
direction. Native default text measures 291.700043×42px against 291.71875×42px
in the browser. The failed fixture does not prove the subsequent behavior
checks; exact rendered comparison remains open.

Acceptance remains open: native runtime/captures and exact browser comparison,
dark/disabled/focus rendered references, full physical pointer/keyboard matrix
(including the disabled input's cursor), consumer rich-content edge cases,
non-Latin fallback glyphs, lifecycle/scaling, live system/contrast appearance,
Narrator, exact focus/color/motion raster timing and ARM64 state acceptance.
Error styling is consumer-owned: the pinned source defines no built-in invalid
variant. Local desktop text, display and animation preferences must remain
unchanged. The repository wiki remote is unavailable, so this contract is
documented here.
