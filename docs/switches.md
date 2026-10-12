# Switch

`BleeckerSwitch` is a preview of the fixed Bleecker `src/components/switch.tsx`
at commit `a86059013cd59684ce452849278ea81e734f2066`.

```xml
<orchard:BleeckerSwitch Label="Enable notifications"
    IsOn="{Binding NotificationsEnabled, Mode=TwoWay}" Toggled="OnNotificationsChanged" />
<orchard:BleeckerSwitch IsOn="True" IsEnabled="False" Label="On (disabled)" />
<orchard:BleeckerSwitch AutomationProperties.Name="Toggle feature" />
```

IsOn defaults to false. Label is optional. Consumers own the Boolean state and
handle Toggled or use a two-way binding. A programmatic IsOn change also raises
Toggled once. Supply an accessible name when there is no visible label;
AutomationProperties.Name and LabeledBy take precedence over Label.

The pinned WinUI SDK seals ToggleSwitch, so the public control wraps one native
ToggleSwitch. That input owns activation, dragging, focus and its Toggle
automation provider. The wrapper delegates its UIA Toggle pattern and focus to
the native control; it preserves the native role, localized control type and
accessible control/content visibility, and suppresses duplicate
accessible children. Its visible template renders the Bleecker label and omits
the Windows On/Off captions. This preserves the single source label.
The Switch theme dictionary is available to the library template and is also
attached directly to the native input before its template is applied. This
supports resource lookup before the nested input joins the owner's visual tree.

The track is 36×20px, with a 1px border and 10px radius. The white thumb is
16×16px, inset 2px, and moves 16px when on. The label uses bundled Encode Sans
medium, 14px/20px with a 12px gap. It wraps beside the vertically centered track.
There is one source size and no visual variant API. Disabled content has 50%
opacity; hover and press retain the same colors and geometry.

The control measures an unwrapped copy of the label with the visible text's
font/scaling properties to determine its preferred width, then constrains the row
to available space before the final wrapped measure. This follows source
inline-flex sizing and prevents the star column from shrinking an ordinary
label to its longest word and clipping it during first arrangement. The native
input and visible label stretch to their allocated widths. The unwrapped
measurement disables layout rounding too, avoiding whole-pixel inflation of the
natural row width. The runtime fixture checks the initial single-line row
against its 173.296875px browser measurement with 0.05px text-shaping tolerance,
and the four-line 200px row retains a 152px label allocation.

The source's hidden checkbox has no visible focus treatment, so the template does not add a
Windows stock focus ring. Keyboard focus visibility remains an acceptance gap.

Light off uses sand at 25% fill and 35% border opacity; light on uses sea. Dark
off uses white at 15% fill and 10% border opacity; dark on uses accent-blue.
Windows contrast maps off/on to system Window/Highlight and paired text colors,
and removes the thumb shadow. The ordinary thumb shadow has the source deep-sea
color at 18% opacity, a 1px vertical offset and a 2px composition blur. CSS and
composition shadow rasterization still require rendered comparison.
The visible thumb ellipse also supplies the shadow alpha mask. An additional
painted black mask underneath the thumb would darken its antialiased edges.
The track uses OuterBorderEdge background sizing. WinUI's inner-edge default
left a transparent 1px inset on the checked track, reducing its visible pill to
34×18px. CSS draws the background underneath the transparent border. Native
raster regression now samples both side midpoints and the top midpoint to
require the background at the outer pill bounds.

Track color transitions use 190ms; thumb translation uses 220ms. Both follow
the source cubic-bezier(.22,1,.36,1). Color interpolation uses premultiplied
Oklab, following [CSS color interpolation](https://www.w3.org/TR/css-color-4/#interpolation-alpha).
Interrupted transitions sample their current presentation and shorten
the reverse path. Windows reduced motion settles immediately. Motion and palette
observers detach on unload/template replacement; reload restores presentation.

The reference audit retains geometry and palette under
`artifacts/switch-reference-light.json` and `switch-reference-dark.json`, with
corresponding captures. The source cases include off/on, disabled off/on, no
label, long labels at 200px, Japanese and RTL Arabic. The browser long label
occupies four 20px lines. The ordinary reference observes this host's reduced
motion preference. A separate moving audit case explicitly restores the pinned
190/220ms durations within that case only and freezes CSS transitions at 95ms;
it does not change desktop settings or reference component source. The track
alpha at that point is .971037 and the thumb displacement is 14.9522px. A unit
regression compares the native easing calculation with these observed values.
The source color sampled into a 1px sRGB canvas is RGBA 46,88,133,248;
a second regression checks premultiplied color interpolation against that raster,
allowing one channel step for the canvas alpha round-trip.

The registered Switches catalog fixture exercises rendered geometry/palette,
disabled rejection, UIA activation/focus, consumer state in both directions,
two-way binding, live labels, explicit names, hover/press state retention,
template replacement, unload/reload and intermediate/reversed motion. It emits
`switch-measurements-{Light,Dark}.json` and corresponding native captures.
The independent NuGet consumer also instantiates and toggles the control.
Runtime evidence must be collected before these checks are claimed as passed.
The mounted fixture also switches appearance in place and verifies the off/on,
disabled and edge-case palette while retaining every consumer Boolean value.
Unload verification waits for the actual Unloaded event before checking that the
composition shadow has detached; IsLoaded can become false before event cleanup.
The dedicated hosted CI desktop additionally sends actual Space/Tab and pointer
input to its own foreground catalog window, verifying label/track activation,
disabled rejection and RTL activation. Local smoke checks omit physical input
and explicitly record that omission, preserving the developer's foreground app.
The hosted check restores its own initially hidden window and temporarily makes
it topmost. If Windows refuses a foreground request, a real click activates that
window before keyboard checks begin. Cleanup removes the temporary topmost state
and restores the pointer. Painted template parts have no automation peer, so
pointer targets use their visual bounds relative to the native input's screen
rectangle and the XamlRoot display scale. Partial physical steps are retained
even when a later check fails.
The disabled-activation regression accepts the native UIA ElementNotEnabled
HRESULT `0x80040200`, which this SDK projection exposes as COMException, while
also requiring the consumer Boolean to remain unchanged.

Acceptance remains open: complete native/browser raster comparison (glyphs,
baselines, pill edges, shadows and fallback fonts), actual keyboard/pointer/drag
matrix, focus visibility, live system appearance/contrast and Narrator,
constrained widths and isolated text/display scaling, exact interrupted-motion
timing/color rasterization, and remaining x64/ARM64 state acceptance. Desktop-wide
display, text size and animation settings must remain unchanged during local
verification. The repository wiki remote is unavailable; this contract is
documented in the code repository.
