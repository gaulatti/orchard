# Loading overlay

`BleeckerLoadingOverlay` follows the frozen Bleecker `LoadingOverlay` at
`a86059013cd59684ce452849278ea81e734f2066`, source SHA-256
`9b4721d0cb08b99284bf0b59dc16ba2448cf8b065a926882f37b1099cea033da`.
The consumer owns `IsVisible` and `Label`. The default label is `Loading...`;
null and empty labels omit the paragraph, while whitespace retains its 12-DIP
gap and a zero-height paragraph.

```xml
<orchard:BleeckerLoadingOverlay IsVisible="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
    Label="Preparing the local preview" />
```

Mount the control in the application's XAML tree. Its popup covers the current
XamlRoot rather than the control's parent. The default card uses a 48-DIP large
spinner, 28/24-DIP padding, a one-DIP border, radius 12 and the existing canonical
overlay shadows. The backdrop applies a two-DIP Gaussian blur and the background
color at 72% opacity. The label uses Encode Sans Medium at 14/20 DIPs. Unbroken
labels keep their minimum content width and may extend beyond a narrow viewport.

The source is a pointer barrier with no dismissal action, automatic focus or
focus trap. Opening it retains existing keyboard focus; a focused underlying
action can still receive Space and Enter. Applications needing modal dialog
semantics should use a dialog contract. This component has no cancellation
command or automatic timeout. The catalog's three-second example demonstrates
caller-controlled visibility.

The spinner exposes an indeterminate `ProgressBar` automation peer without a
numeric range or keyboard stop. Its name follows Orchard's standard precedence:
`AutomationProperties.Name`, then `AutomationProperties.LabeledBy`, then `Label`.
Attached names and linked captions remain live while the portal is open and
survive template replacement. Supply a localized accessible name when omitting
the visible label; the control does not invent an operation name. This native
accessibility contract extends the frozen source, which has no ARIA name or role.

`-Component loading-overlay-accessibility -ClientSize 640,480` checks ten visible
states and hidden-peer removal in both themes, preserving the original action's
focus. A separate UI Automation client verifies the real out-of-process tree,
including localized bindings, live labels, linked captions and explicit-name
priority. The catalog compiles its bundled test client using Windows' .NET
Framework compiler and UI Automation assemblies; no execution-policy change is
required. These checks establish UI Automation exposure, not Narrator speech.

The template exposes `PART_Popup`, `PART_Portal`, `PART_Backdrop`, `PART_Tint`,
`PART_Card`, `PART_Body`, `PART_Spinner`, `PART_LabelHost` and `PART_Label`. Default brushes live in
`Themes/LoadingOverlay.xaml`. Windows contrast colors replace translucent tint
and border colors and suppress backdrop blur; actual contrast and Narrator
verification remains required. Spinner motion follows the existing loading
spinner's system animation preference.

Replacement templates preserve the named part types and their layout
relationships. `PART_Popup` is a non-dismissable `Popup`; `PART_Portal` is a
`Canvas` containing the `Grid` backdrop, `Border` tint and
`BleeckerOverlaySurface` card. The card contains the `PART_Body` Canvas, whose
children are the large `BleeckerLoadingSpinner` and `PART_LabelHost` Canvas;
the label host contains `PART_Label`, a `TextBlock`. The control positions these
parts in viewport coordinates. Additional decoration belongs to the caller.
Retain template bindings for brushes, padding, borders and corner radii.

Instance `Resources.ThemeDictionaries` can override `BleeckerCardBrush`,
`BleeckerTextPrimaryBrush`, `BleeckerLoadingOverlayBorderBrush` and
`BleeckerLoadingOverlayTintBrush`. Existing brush objects remain shared, so
color/opacity mutations reach open content. The dictionary-replacement fixture
switches themes after replacement: WinUI reevaluates `ThemeResource` references
on a theme change, as described by Microsoft's [resource contract](https://learn.microsoft.com/en-us/windows/apps/develop/platform/xaml/themeresource-markup-extension).
Local dependency-property values retain priority; clearing them restores the
active resource value.

`-Component loading-overlay-template -ClientSize 640,480` validates ten phases:
instance resources, decorated template replacement, Dark/Light changes, brush
mutation, asymmetric geometry, typography/RTL and live names, local-value
priority/restoration, dictionary replacement after a theme change and original
template restoration. Every phase exercises a real pointer barrier. Forty focus
observations distinguish a focus request from its presented result: the
temporary root tab stop is released after focus is presented on the original
action. Four negative audits reject stale replacement colors, missing custom
visuals, missing physical pointer receipts and a retained root tab stop. The
independent NuGet consumer also replaces its template and changes a caller label.
This fixture preserves the documented part layout; it does not establish every
possible caller template, cross-window transfer, display scaling or raster parity.

`tests/Reference/LoadingOverlay.json` records 60 unmodified browser cases across
1024×900, 640×480 and 320×240 in both themes, plus six real keyboard/pointer
interaction probes. `scripts/reference/capture-loading-overlay.cjs` reproduces
them from the pinned React component with recorded font/tool hashes. Snapshot
animations are paused at time zero; they do not establish motion parity.

The catalog's `--smoke-loading-overlay` mode measures the matching viewport
cases against the source with the existing 0.05-DIP geometry/baseline bound and
captures the actual owned native client. It records partial measurements before
capture/input failures. The separate backdrop diagnostic proved that the pinned
WinUI Popup samples the application behind it and retains existing focus.
All 60 native cases pass geometry and 42 first-line baselines: maximum errors
are 0.018189 and 0.000031 DIPs, respectively. Real Space/Enter, pointer blocking,
Escape, root focus clearing and subsequent Tab/hide behavior pass in both
themes at all three sizes. Four negative audits reject shifted baselines,
shifted viewports, retained pointer focus and missing physical pointer receipts.

Pointer focus clearing temporarily makes the XAML root focusable. Its next real
focus move restores the original `IsTabStop` value or binding. Opening and hiding
the overlay do not themselves take focus. The catalog source harness hides its
navigation while measuring the isolated source viewport, then restores it.

A `Page` used as `Window.Content` can start with an effective non-tab-stop value
that `ClearValue` does not recover: the API can expose Control's `true` default
instead. Pointer focus clearing captures that original effective value and
restores it explicitly when needed. Existing bindings are reattached; ordinary
unset Grid values remain unset. The native Page regression reproduces the
original failure after a real click, hide and Tab, then verifies restoration.

Focused `-Component loading-overlay-lifecycle -ClientSize 640,480` validates
14 phases under a Page root: open Light/Dark/Light changes, original mutable
brushes and asymmetric geometry, real window resizing, immediate reparenting,
template removal/restoration, unload/remount, independent stacked callers and
pointer/root focus restoration. It restores the original window content and
records real viewport bounds, palette values, focus notifications and native
pointer receipts. `scripts/verify-loading-overlay-lifecycle.mjs` audits that
evidence. This does not establish arbitrary custom-template or cross-window
root-transfer behavior, complete resource replacement, scaling or pixel parity.
Three negative audits reject a retained root tab stop, shifted resized portal
and stale Dark palette. The published frozen-source matrix also retains all
original Grid-root value and binding checks.

The portal tracks presentation while visible as well as layout changes. An
ancestor's render transform can move without a layout notification; changed
popup anchors are laid out before that frame is drawn. Hiding, detachment and
template replacement release the rendering subscription. Focused
`-Component loading-overlay-scroll -ClientSize 640,480` records every observed
frame while the parent scrolls and changes its render transform. Its viewport
bound remains 0.05 DIP, including the first frame after a change.

Exact unmasked pixel comparison remains required. The native fixture uses real
native underlying buttons, whose own rendering also differs from the browser
fixture. The checkerboard phase matches the source. No masks, resampling or
pixel tolerances are applied.
The published 60-case comparison retains 3,041,291 differing pixels and
38,220,303 total RGBA channel error across the complete source/native frames.
Every frame differs. These totals include underlying fixture differences.

Clean release source is `2e72e334c5b321f795a248ba3aef1dc3563233da`.
Both architecture builds and all 113 unit tests pass. The full x64 catalog
passes 1,130 captures in 456.908882 seconds, retaining every prior capture
and 174 protected glyph hashes. The independent x64 NuGet consumer passes
both themes, viewport coverage, its original keyboard action, consumer label,
spinner automation, live bound/linked names and omitted-label layout. The ARM64 consumer builds only;
both isolated caches are removed. See [release evidence](native-validation-checkpoint.md).
The published 14-phase lifecycle, 24-frame scroll/transform and 22-tree external
UI Automation audits pass. Three accessibility negative audits reject stale
external names, lost focus and retained hidden peers. The earlier full run's
Avatar presentation timeout remains retained; its cause is not established.
The original release DLL is rejected by the same Page-root regression: the
actual button receives Tab focus while the root incorrectly remains a tab stop.

Acceptance remains open: exact raster parity, broader resource and
custom-template/cross-window lifecycle, scaling,
contrast, Narrator, motion and native ARM64
runtime. A successful build or the presence of the catalog example is not
component acceptance.
