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
The checkbox's private resource dictionary supplies a default TextBlock style
with the same 20px block line height. It covers the native Content ownership
path and the description/template visual tree; explicit consumer text
properties/styles take precedence.
Template-generated TextBlock/RichTextBlock elements whose declaration scope
does not resolve that style receive the same defaults during layout. Local
values, bindings and style setters remain authoritative; owned defaults and
style observers detach on content/template replacement, generated subtree
removal or unload. Changing an explicit style on generated text releases the
owned values before that style takes effect.
Text starts 12px after the box. Description has a 2px top margin, including
when there is no label. Both strings wrap beside a top-aligned box; empty
content collapses the text column. Intrinsic measurement preserves the larger
unwrapped label/description width before constrained wrapping.

Light unchecked uses white and sand at 60% border opacity; checked uses sea.
Dark unchecked uses the dark deep-sea token (#182533) and white at 20%; checked
uses accent blue. All brush values are generated from the locked token source.
Unchecked hover uses the active theme's sea at 50% (dark #5BA3F5).
Keyboard focus uses a 2px active-sea/20 ring and a
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
Temporarily clearing Template removes presentation without losing consumer
state. Rich label/description elements detach from their old presenters before
the replacement template receives them, preserving single visual ownership.

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
If a template is unavailable on failure, the report explicitly marks that row's
measurement error and preserves the original verification exception.
The same rich row also exercises an explicit consumer text style (28px lines),
local text properties (30px lines), and a two-line consumer data template.
It verifies their resulting 50/52/62px row heights, data updates, template/content
identity and restoration of the original 42px row. Individual captures are
retained as `checkbox-rich-{style,local,template}-{Light,Dark}.png`.
The template row also applies and removes a consumer style after layout,
requiring 70px and then 62px without replacing the style or template.

The first x64/ARM64 run at `79f1053` stopped at its glyph geometry assertion.
The x64 capture also exposed inherited RTL mirroring. The follow-up keeps the
14×10 content inside an explicit 14×14 viewport and fixes the vector's reading
direction. Native default text measures 291.700043×42px against 291.71875×42px
in the browser. The failed fixture does not prove the subsequent behavior
checks; exact rendered comparison remains open.

At `a4720b2`, both architectures passed initial geometry, native UIA
role/name/value and exactly-once events, disabled rejection, two-way binding,
content updates, focus modality and unchecked visual states. Clearing Template
still failed in the native base application path. An empty template now clears
Orchard presentation before invoking any base template work; nonempty templates
retain the normal native application path. Per-control checkpoints distinguish
clearing from restoration. The same run exposed a 37px rich-content row, while
the actual pinned browser component measures 165.109375×42px. Rich presenters
now provide the source's line height, with that geometry checked at runtime.
The light/dark source matrix is retained under
`artifacts/checkbox-reference-matrix-{Light,Dark}.{jpg,json}`. These follow-up
changes still require native verification. That matrix also confirms that dark
deep-sea, hover and focus must use dark token values, rather than the light
brand constants. Runtime checks now verify the source's dark fill and focus
palette as well as geometry.

Run [38300873774](https://github.com/gaulatti/orchard/actions/runs/38300873774)
at `48386d4` passed builds and unit checks, then correctly rejected the rich row
at 37px on both architectures: ContentPresenter's own LineHeight does not
propagate into consumer-owned TextBlocks. The follow-up scopes a TextBlock
style to each rich presenter. It retains the 42px runtime acceptance check;
template clearing and the subsequent checks were not reached in that run.
At `d0a66dd`, the description acquired the presenter-scoped style but the
native Content-owned label did not, producing 39.5px on both architectures.
The style now lives in the checkbox's private dictionary so both ownership
paths can resolve it. Verification continues to require the full 42px row.
At `84e6ef2`, the default 42px row and the explicit style/local overrides pass.
The consumer data-template check then fails before template replacement.
The next fixture waits for normal binding propagation and records template
text, data-context type and geometry before restoration; the exact 62px
acceptance remains unchanged.
At `d382242`, binding succeeds and the template receives the expected data,
but both template lines retain 17.5px automatic heights (57px total). Default
line formatting now also reaches those generated elements, without changing
the consumer's template or style identity. Native verification remains open.
At `7a0def1`, x64 and ARM64 pass the rich-content checks, including the 62px
template row and live consumer style changes. Both then fail when applying a
cleared template. The smoke report now distinguishes assignment, application
and restoration, and native template exceptions retain their managed
diagnostic through Trace. The lifecycle gate remains active.

Acceptance remains open: native runtime/captures and exact browser comparison,
dark/disabled/focus rendered references, full physical pointer/keyboard matrix
(including the disabled input's cursor), consumer rich-content edge cases,
non-Latin fallback glyphs, lifecycle/scaling, live system/contrast appearance,
Narrator, exact focus/color/motion raster timing and ARM64 state acceptance.
Error styling is consumer-owned: the pinned source defines no built-in invalid
variant. Local desktop text, display and animation preferences must remain
unchanged. The repository wiki remote is unavailable, so this contract is
documented here.
