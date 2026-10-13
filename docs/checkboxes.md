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
name, which includes rendered label and description text, including consumer
data templates. Collapsed child text is excluded; named consumer subtrees keep
their own accessible names. A bare checkbox needs an explicit name. Rich content
retains its consumer-owned elements, data and templates. Rich layout changes
invalidate the checkbox peer when its default name changes.

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
Negative margins give the 28/24px focus layers their full layout space around
the 20px input. Size assertions alone do not prove these layers paint outside
the box: the physical keyboard fixture checks composed pixels at both outer
edges and at the inner offset, against the theme brush composited over the
captured background. The report retains observed and expected colors.
Disabled changes only the input chrome to 50% opacity. The checkmark and text
retain full opacity, matching the source's separate SVG and text siblings.
Enabled associated-label hover selects the input's hover border across its
text. A disabled associated label does not propagate hover to the input:
only the 20px box selects the hover border and not-allowed cursor. Label and
description retain the normal arrow cursor and resting border. Checked hover
retains the checked border. Root pointer observation leaves native activation,
focus refusal, UIA disabled state and consumer-owned content unchanged.
Windows contrast uses paired Window/WindowText and Highlight/HighlightText
colors, with a visible system-colored focus ring.

Fill, border, glyph opacity, disabled opacity and focus each have an independent
190ms premium clock. Fill and border interpolate continuous premultiplied Oklab
and share the tested CSS reversal behavior with Switch. Palette changes do not
restart unchanged property clocks. Reduced motion settles immediately; motion
also snaps while Windows contrast is enabled. Rendering/settings/palette
subscriptions detach on unload or template replacement and restore on reload.
Use Visibility.Collapsed to hide a checkbox while retaining its template,
consumer state and content. A rendered replacement template must provide the
required PART_* elements. When replacing a template, rich label/description
elements detach from their old presenters before the replacement receives
them, preserving single visual ownership. Null assignment detaches Orchard's
observers/content immediately; restore a valid template before application or
layout. Null is not a persistent empty-presentation state: native ContentControl
creates default content visuals when it has content and no template. Orchard
does not rewrite a consumer's template value or binding to suppress that SDK
behavior. The style key remains fixed after construction. The lifecycle fixture
checks immediate detachment, retained value/content/name, branded restoration
and hide/show through normal layout with retained rich presenter ownership.

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
At `775c1b1`, the x64 snapshots confirm null assignment immediately removes all
visual children and original-template restoration rebuilds them. No managed
template exception is traced: the explicit null ApplyTemplate failure happens
before Orchard's override. Cleanup now observes null assignment itself.
At `ec16816`, normal layout exposes a template without PART_Box after null
assignment. Clearing the default style key was attempted as a correction.
At `2bf7205`, both architectures reject changing DefaultStyleKey after control
initialization, including on restoration. That approach has been removed.
At `8dccce0`, an explicit constructor DefaultStyleResourceUri builds cleanly
but x64 still fails at null ApplyTemplate. Its retained phase report shows a
new unnamed Grid and a nonnull effective template despite the preceding null
assignment. This matches the native
[ContentControl application path](https://github.com/microsoft/microsoft-ui-xaml/blob/821749494b17ad994e05af503516e4aaab7c062d/dxaml/xcp/core/core/elements/ContentControl.cpp#L418),
which creates default content visuals when no template child exists. The
earlier assumption that this was stock CheckBox style lookup was incorrect.
The unnecessary resource-URI override has been removed. The fixture now
verifies detachment/restoration before layout and a separate Collapsed/Visible
cycle through normal layout; it retains branded parts, consumer state/content,
native value/name updates and rich ownership checks. Applying a persistent
null template is not a supported way to hide a ContentControl-derived checkbox.

The Japanese DOM audit compares actual text layout, rather than canvas
measurement. The pinned component is 111.453125px wide for its Japanese label;
the explicit Yu Gothic UI probe is 99.546875px at weight 400 and 100.625px at
500. Yu Gothic is 111.453125px at all three tested weights. The browser report
and screenshot are retained as `artifacts/checkbox-reference-cjk-dom.{json,jpg}`.
Native fixture font probes compare the original stack with explicit Japanese
families and restore the consumer font. These are evidence probes; the library
font stack remains unchanged pending native glyph comparison.
At `ec16816`, the x64 original label is 99.538086px; the Yu Gothic probe is
111.439453px, within 0.014px of the rendered source. Yu Gothic UI and the
Meiryo probe retain 99.538086px. The native/source comparison is
retained at `artifacts/checkbox-cjk-comparison-Light.png`; the original stack
remains unchanged while broader script/weight/platform verification continues.
The native fixture also records Japanese, simplified/traditional Chinese,
Korean, Arabic and Latin consumer text at weights 400/500/600, with explicit
content languages. It compares the canonical font resources against the
Yu Gothic candidate and restores content, description and language afterward.
These reports (`checkbox-script-weights-{Light,Dark}.json`) and per-case native
captures are diagnostic evidence; they do not establish font parity or change
the library's font resources. The corresponding pinned browser measurements
and render are retained at `artifacts/checkbox-reference-script-weights-Light.{json,jpg}`.
At `7afa247`, all 36 probe dimensions agree across native x64 and ARM64.
The Yu Gothic candidate matches Japanese 400/500 within 0.014px, but Japanese
600 grows to 113.681641px against the browser's 111.453125px. Native Arabic
400/500 measures 93.846664px against 93.4375/93.46875px in the browser, and
its medium appearance remains lighter. A global Yu Gothic addition would not
resolve the complete contract. The eight-case comparison is retained at
`artifacts/checkbox-script-weights-comparison-Light.png`; these are open font
gaps, and the shared font resources remain unchanged.
Additional evidence cases compare direct Yu Gothic for Japanese and direct
Segoe UI, Arial and Tahoma for Arabic. They distinguish explicit family
selection from native fallback under the bundled primary face; these probes
do not choose or change a library fallback policy. The matrix now contains
48 cases per appearance, with the same consumer-state restoration.

The physical-input fixture uses a catalog-only NativeInputSession. It resolves
the catalog's HWND and verifies its process before acquiring foreground input;
every click verifies the screen point belongs to that window. Targets use
normal StartBringIntoView to remain reachable on hosted monitors. The fixture
checks Space, Tab including disabled skipping, box/label/description activation,
RTL box/label activation, exactly-once Click/value/command delivery, parameter
identity and CanExecute rejection/restoration. Keyboard and pointer captures,
desktop diagnostics and incremental steps are retained even on failure. The
session removes temporary topmost status and restores the pointer in cleanup.
These new physical checks still require hosted native runtime verification;
disabled cursor behavior remains a separate open acceptance item.
At `3dac874`, the complete registered x64 catalog and independent NuGet runtime
consumer pass; the light/dark checkbox reports include lifecycle, physical
input and command checks. ARM64 passes the light fixture with animations
enabled, including the same physical checks, then fails a forced dark hover
state. The retained report shows Off remained checked: the foreground fallback
click activated that probe in light mode. Acquisition now targets the static
fixture heading, and the physical fixture restores Off alongside other state.
Hover/press/exit acceptance now uses real pointer movement and held-button
input, including the source's hover border while pressed, with native captures.
The session releases any held pointer button during failure cleanup. The
shared x64/ARM64 baseline remains unchanged until the whole run passes.
At `81884bf`, both native runtime jobs fail at the first physical hover check,
before reaching the focus pixel gate. The retained x64 report shows Off remains
unchecked with the normal border and the catalog owns foreground input.
Pointer movement now uses SendInput with absolute virtual-desktop coordinates,
matching the physical button/keyboard input path. Every movement records the
requested and actual position and owned-window hit, and rejects a missed target.
This change requires a new native run; the rendered focus fix remains unverified.
At `3485f52`, the complete x64 catalog and independent NuGet consumer pass.
The native ARM64 Light checkbox also passes with animations enabled, including
hover/held press/exit, keyboard/pointer activation, commands and lifecycle.
The ARM64 run later fails Dark Input validation before its Dark checkbox or
independent consumer. This does not replace the shared full-runtime baseline.
Composed pixel checks verify the focus ring at both box edges and the inner
offset on x64 Light/Dark and ARM64 Light. All 48 Light font-probe dimensions
agree across architectures. Explicit Segoe UI retains the regular Arabic
appearance at 500, while the browser's medium text is heavier; selecting that
family alone does not resolve the gap. The comparison is retained at
`artifacts/checkbox-arabic-family-comparison-Light.png`.
The rich-content reports now inspect native default names with the fixture's
explicit override temporarily removed and restored. These diagnostic names
cover direct TextBlock content, a consumer data template and live template data;
they do not establish complete accessibility acceptance.
The next font evidence cases compare Yu Gothic Medium, Segoe UI Semibold and
an Arabic word/space run split. The browser remains heavier at Arabic 500 with
font-synthesis-weight disabled, and its explicit Segoe UI Semibold renders the
same width as its Segoe UI 500. Canonical font-table space advances at 14px are
3.423/3.451/3.486px for Encode Sans 400/500/600, versus 3.834961/3.848633px for
Segoe UI regular/semibold. These differences are consistent with the observed
whole-run width gap within native/browser layout quantization. The diagnostic preserves primary
spaces and selects the observed Arabic word face, retaining the full consumer
string and exposed font weight. There are now 57 cases per appearance. These
are evidence probes; shared library typography is unchanged.
At `0e35493`, both architecture jobs pass the complete Light/Dark catalog and
independent NuGet runtime consumer; the ARM64 build also passes. The retained
Input diagnostic is in InputNormalError with ReadOnlyInput focused, correct
palette, border and opacity. The prior Dark failure was not reproduced, so its
cause is still unproven. Rich TextBlock naming is correct, but a data template
announces `ConsumerLabel { Title = ... }` rather than the rendered label.
The checkbox peer now reads the rendered rich label/description tree, excludes
collapsed children and respects named subtrees. Native regressions require
direct/live text, label and description templates, live model updates, hidden
text exclusion and consumer Name/LabeledBy precedence, with ownership restored.
At `ebfb6ac`, eight strict rich-name regressions pass in both appearances on
x64 and native ARM64, alongside the full catalog and independent package
consumer. This does not establish Narrator acceptance.

## Disabled pointer boundary and shared cursor fixture

Pinned browser measurements distinguish disabled box hover from associated
label hover: the box uses sea/50, opacity .5 and `not-allowed`; hovering its
disabled label leaves the box at sand/60 and the label cursor at `default`.
Enabled label hover does propagate to the input. Source captures and computed
styles are retained at `artifacts/checkbox-reference-{disabled-box,disabled-label,enabled-label}-Light.{json,jpg}`.

The native Checkbox now supplies only `PART_Box` bounds to the shared disabled
pointer observer. Input retains its complete editor bounds. One observer per
XAML root shares native pointer queries, rendering/layout observation and hit
testing; one cursor owner coordinates both control types. Target and ancestor
visibility/clipping, scroll viewport bounds, native scroll chrome and sibling
occlusion govern hover. Enable/unload and Checkbox template replacement release
the registration and native override.

The existing physical Checkbox fixture adds disabled off/on box, label and
description cursor/border checks, enabled label hover, stationary enable/disable,
template detach/restore, occlusion, clipping, movement, hit testing, hide/show,
unload/reload, scrolling, RTL and movement between Input and Checkbox. It
asserts native cursor handles, theme stroke colors/opacity, input-only disabled
opacity and retained glyph/text opacity. Reports are
`checkbox-disabled-pointer-{Light,Dark}.json`; box/label hover captures are
`checkbox-{DisabledOff,DisabledOn}-{box,label}-hover-{Light,Dark}.png`.
Resting palette fixtures temporarily disable hit testing on disabled rows and
restore it before this physical matrix. Local compilation/tests pass; this new
pointer implementation requires its own hosted x64/ARM64 runtime evidence.

At `625262b`, the native ARM64 catalog and independent consumer pass; the x64
catalog passes disabled box/label/description and stationary enable/disable,
then fails to restore hover after reparenting into a fixture Grid. Its screen
box remains 20px at the same coordinates, with resting sand/60 and arrow rather
than expected sea/50 and not-allowed. The x64 artifact `11778231848` was
downloaded and hash-verified (SHA-256
`a92513ffc7111604d55247d571f2f33d8541fa377a6387e2904cc71e383d2be0`).
Failure reports now retain native hit elements, ancestor visibility/hit testing/
clipping, current geometry and tracker registration. Tracker fields are read
only as diagnostics; acceptance still uses physical cursor handles and actual
rendered output. At `b63424d`, the x64 report shows the control and relevant
ancestors loaded, visible and hit-testable, with Rows as the hit target, but no
pointer registration. The observer now coalesces a dispatcher reconciliation
after Loaded/Unloaded notifications, reattaching according to current IsLoaded,
IsEnabled, XAML root and template-part availability. Genuine unload and template
detachment still release immediately. The physical matrix adds three rapid
remove/add cycles for both Checkbox and Input, retaining a stationary pointer
and checking actual cursor/hover output. This correction needs a new native run.

At `bf910af`, native x64 and ARM64 both pass remount, all three Checkbox
rapid-remount cycles, clipping, visibility, scrolling and RTL. Both then fail
the strict I-beam check when moving from the disabled Checkbox to the enabled
Input. The catalog still owns foreground and the pointer is at the Input's
center; desktop ownership does not explain this failure. The fixture now saves
the actual native cursor, the Input's ProtectedCursor, the root input source's
cursor, current hit elements and observer state at that transition. Acceptance
still compares the physical cursor handle with the system I-beam handle.
The `8b8be3c` ARM64 capture shows the entered Input loaded/enabled, with
ProtectedCursor and InputPointerSource.Cursor both IBeam, but the UI thread's
GetCursor handle still Arrow. The observer is registered and no longer hovered.
The `f8c67a8` candidate restored immediately, then queued a reconciliation after
the current pointer dispatch. That reconciliation reads WinUI's current cursor,
requires no newer disabled owner and checks the root still owns the native
screen point. System cursor restoration no longer changes InputPointerSource's
cursor property; resource cursor restoration retains its native API path.
The strict physical I-beam transition remains the regression boundary.
The `f8c67a8` x64 run still fails that check. The fixture now also records
GetCursorInfo's global cursor handle, visibility flags and screen point for
every Checkbox boundary sample and Input transition. Microsoft's
[GetCursor documentation](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursor)
distinguishes the calling thread's cursor from
[global cursor information](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getcursorinfo).
Earlier handle checks use GetCursor and cannot by themselves prove the visible
desktop cursor. At `706ee10`, the global observation establishes that the enabled
Input displays IBeam correctly, but the disabled Checkbox displays Arrow while
the UI thread reports UniversalNo. The verified ARM64 archive `11780820812` has
SHA-256 `6f75a65e7a63d3981ed16ad35774da3037a377d316998ae28764bb8f061337a8`.
The shared override now sets InputPointerSource.Cursor for the root's native
input target, retaining the last cursor object actually published by WinUI and
updating it when WinUI publishes a new selection. Its own UniversalNo object
does not become a producer selection. Release restores that object through the
same API, preserving resource cursors without a hardcoded system-ID table.
The root also coalesces one refresh after pointer dispatch. Stationary enabling
selects the control's explicit Arrow/IBeam cursor. Both physical fixtures now
require GetCursorInfo's displayed global handle and showing flag. Thread cache
and source properties cannot satisfy the acceptance gate. New native evidence
is required; earlier thread-only cursor assertions do not prove visible parity.

The `b63424d` ARM64 run passes the remount/scroll checks, then loses the catalog
foreground before RTL input. Desktop diagnostics now capture the actual
foreground process/class/package alongside the owned screen point. Physical
input still fails if foreground ownership is lost; no other process is targeted.

The later `9c5699b` ARM64 reports pass all 57 disabled-pointer phases in both
appearances, including rounded corners. Its subsequent Dark click fails after
foreground moves to a `wsl` process / `ConsoleWindowClass`; the pointer still
hits Orchard's own topmost window. The hosted fixture now permits one
SetForegroundWindow call to its own catalog before a movement, only with CI
enabled, no held button, an external foreground process and an owned current
screen point. It records the attempted recovery and before/after window
descriptors. It does not reactivate over an Orchard popup, target the external
console, send recovery keys, or relax the foreground/window hit checks.

The pinned browser's 20×20 rounded box excludes its four extreme integer corner
pixels from the input hit target. `tests/Reference/CheckboxRoundedHit.json`
records all 400 integer points plus 16 edge/fractional samples for disabled
off/on in Light and Dark, measured with `document.elementFromPoint` against the
actual pinned Checkbox at an integer position. The component is not restyled.
The native observer now intersects the pointer sample with PART_Box's actual
corner radii. It uses a 1×1 DIP sample, matching the browser's
[rounded hit quad](https://raw.githubusercontent.com/chromium/chromium/b428a2ab5f47801213a6973f9911b7c858300306/third_party/blink/renderer/core/layout/hit_test_location.cc)
and [point rectangle](https://raw.githubusercontent.com/chromium/chromium/b428a2ab5f47801213a6973f9911b7c858300306/third_party/blink/renderer/core/layout/hit_test_location.h).
A dimensionless circle test would incorrectly exclude the adjacent corner
pixels that Bleecker includes. Root hit testing still checks clipping,
visibility and occlusion. Input's full control bounds are unchanged by this fix.

The regression compares all 1,600 captured integer points and 64 edge/fractional
samples with the library geometry. The physical catalog fixture adds all four
corner and four adjacent samples for each disabled state in both appearances,
requiring exact screen coordinates, actual border/opacity and native cursor
handles. These 16 extra phases bring the interaction matrix to 57 acceptance
phases per appearance (diagnostic snapshots are not acceptance phases).
Hosted native verification is required before accepting the rounded boundary.

Acceptance remains open: native runtime/captures and exact browser comparison,
dark/disabled/focus rendered references, full physical pointer/keyboard matrix
(including the disabled input's cursor), consumer rich-content edge cases,
non-Latin fallback glyphs, lifecycle/scaling, live system/contrast appearance,
Narrator, exact focus/color/motion raster timing and ARM64 state acceptance.
Error styling is consumer-owned: the pinned source defines no built-in invalid
variant. Local desktop text, display and animation preferences must remain
unchanged. The repository wiki remote is unavailable, so this contract is
documented here.
