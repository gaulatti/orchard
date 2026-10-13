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
The pinned SDK template also requires a Thumb named SwitchThumb for pointer
activation and dragging. Its transparent template spans the complete branded
row, including the label and gap, and adds no tab stop or accessible child.
Omitting this native input part allowed UIA and Space to work while physical
clicks did nothing. Template application now requires the input part, and the
fixture verifies that it covers the complete row before physical activation.

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
natural row width. Its preferred row width is rounded outward by one native
float step: subtracting the track and margin must not round the label's available
width below the glyph advance and move its final word to a second line.
The runtime fixture checks the initial single-line row
against its 173.296875px browser measurement with 0.05px text-shaping tolerance,
and the four-line 200px row retains a 152px label allocation. The allocation is
measured from the layout slot minus the margin; TextBlock ActualWidth reports
its longest rendered line, which can be narrower than the wrapping allocation.

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
Track fill, stroke and thumb position keep independent clocks. Palette changes
animate the track for 190ms and preserve the thumb's current path. Interrupted
colors retain continuous premultiplied Oklab values, avoiding an 8-bit round-trip
before retargeting. Reversals retain CSS's reversing-adjusted start and shortening
factor; a third destination starts a full transition from the current presentation.
Windows reduced motion and motion while contrast is enabled settle immediately. Motion and palette
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
An isolated CSS audit using the source color-mix expression observes repeated
reversal durations 182.662684ms and 153.358570ms, followed by a full 190ms dark
palette transition. Unit regressions check their rendered color samples and
that unchanged destinations preserve the thumb clock.
The probe pages and observed output are retained under
[tests/Reference](../tests/Reference/README.md).

The registered Switches catalog fixture exercises rendered geometry/palette,
disabled rejection, UIA activation/focus, consumer state in both directions,
two-way binding, live labels, explicit names, hover/press state retention,
template replacement, unload/reload and intermediate/reversed motion. Motion
and appearance samples are retained separately before physical input, so an
inactive hosted desktop does not erase animation evidence. The fixture emits
`switch-appearance-{Light,Dark}.json` even when appearance verification fails.
Endpoint checks compare against XAML's float opacity: the dark .15 resource is
reported as .15000000596046448; comparing it against an unrounded double with
1e-9 tolerance would falsely reject a settled palette. Every fixture row must
reach its own fill and border endpoints before the live appearance check
verifies presentation; the clocks of a single on/off pair cannot establish
settlement for disabled and edge-case rows. The appearance report retains all
rows at the alternate and restored endpoints, including when a check fails.
It also emits
`switch-measurements-{Light,Dark}.json` and corresponding native captures.
The independent NuGet consumer also instantiates and toggles the control.
The x64 catalog and isolated consumer at 7001e3e passed the registered light/dark
checks, including actual Space/Tab and track/label/RTL clicks. That host had
animations disabled, so it verified immediate reduced-motion settlement rather
than intermediate motion. Native ARM64 desktop activation remains unresolved.
See [runtime evidence](runtime-evidence.md) for reports, captures and exact scope.
The mounted fixture also switches appearance in place and verifies the off/on,
disabled and edge-case palette while retaining every consumer Boolean value.
Unload verification waits for the actual Unloaded event before checking that the
composition shadow has detached; IsLoaded can become false before event cleanup.
Hosted CI sends actual Space/Tab and pointer input to its own foreground catalog
window, verifying label/track activation, disabled rejection and RTL activation.
Ordinary local full-suite runs retain their existing omission of this Switch
physical phase. The focused `scripts/smoke-catalog.ps1 -Component switches`
explicitly includes it through the same owned-window path, without spoofing CI
or changing desktop preferences.

The physical phase now uses the catalog's shared `NativeInputSession` instead of
duplicating cursor movement, input injection, foreground acquisition and cleanup.
That session verifies the XAML-root HWND and process, temporarily maximizes and
makes only the catalog topmost, checks target ownership, and restores the pointer
and topmost state. Keyboard actions and captures require foreground immediately
beforehand. Pointer movement and held press/release use the shared guards.
On hosted CI only, the existing single recovery can re-acquire the still-visible
catalog after a foreign foreground change, with an owned current point and no
held pointer button. It refuses Orchard-owned popup/window failures. This does
not weaken activation or visible-state assertions.

Run `38470614305` at `289a8ba` passes ARM64's corrected Bauhaus fixture but later
fails during Dark Switch track activation because another HWND owns foreground.
Its retained desktop report confirms the mismatch. The old Switch helper lacked
the shared bounded recovery and checked only before its pointer delay; this
refactor puts it on the same path as the other catalog fixtures.
The focused local x64 check passes at `artifacts/smoke-switch-shared-input`
with animations off and display/text scale 1. Both appearances retain all
eight physical acceptance steps, including actual Space/Tab, label/track,
disabled and RTL activation. The composed captures retain the thumb shadows.
All 27 unit/resource tests pass; both architecture builds have zero warnings.
An occluded target also retains HWND class, executable basename, process/session,
topmost status, visibility and DWM
cloaking diagnostics, plus the owning application's AUMID, without other windows'
titles or content.
The native ARM64 diagnostic identifies its covering window as
`Microsoft.Windows.CloudExperienceHost_cw5n1h2txyewy!App`. Hosted preparation
requests normal minimization only when the foreground AUMID exactly matches
that welcome app and its session matches the catalog. It records the request
in `switch-{Light,Dark}-hosted-welcome.json`. The foreground and target-ownership
gates still prove whether that request exposed the catalog; the request itself
is not success. This code refuses to run outside CI. It makes no welcome
choices, changes no Windows preferences and does not terminate that process.
The next observed blocker is the same-session explorer window with class
`UserOOBEWindowClass`, exposed at the target after the welcome-app request.
Preparation also requests normal minimization of that exact class, only after
the CloudExperienceHost identity matched and after checking explorer/session
ownership. Its metadata and request are retained in the welcome report.
Window-station,
console-session and WTS connection state distinguish an inactive session from
a geometry error. The fixture emits isolated Japanese label captures for the
bundled font and explicit candidate font lists, restoring the original after
the probe; these are diagnostics, not accepted library fallback mappings.
The isolated browser font audit reproduces the canonical 125.296875px Latin
and 111.453125px Japanese labels with the source's Google Encode Sans 500 face
loaded. Segoe UI and Arial also produce the Japanese width through their own
fallback; matching the width does not identify the CJK glyph family.
Painted template parts have no automation peer, so
pointer targets transform their visual bounds to XamlRoot.Content, apply the
display scale, then add the HWND client origin obtained through ClientToScreen.
In-process peer bounds in this SDK projection were client-relative and did not
include the desktop offset; they cannot be used directly for pointer placement.
Partial physical steps are retained
even when a later check fails.
Hosted CI also emits `switch-window-{Light,Dark}.png` from the composed client
pixels, with screen bounds in a companion JSON file. The capture requires its
own foreground window, checks that the entire client fits its physical monitor
and is visible and unoccluded, and
flushes desktop composition before copying screen pixels. This includes the
composition thumb shadow omitted by XAML RenderTargetBitmap captures. These
screenshots support visual inspection; capturing alone does not establish parity.
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
