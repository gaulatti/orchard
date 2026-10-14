# Attention Surface reference and acceptance

The fixed component is
[`attention-surface.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/attention-surface.tsx),
SHA-256 `59fb7ff2bb5459facc410a6fd204d5e977639ce093113b23d8fff8d86e7a8321`.
Its helper is
[`attention-color.ts`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/utils/attention-color.ts),
SHA-256 `967280e82680e07604a5ac271c1dd21388b006c3e82ecb537e20ea6c5dd01fab`.
The public wiki remote remains unavailable; this contract is maintained here.

The consumer owns content and button attributes/actions. `hue` and `intensity`
are required numeric inputs. Hue is rounded with ECMAScript Math.round and
wrapped into 0–359; a non-finite hue explicitly uses 210 in the frozen source.
Intensity explicitly uses zero for non-finite input and clamps to 0–10.
Red mix is Math.round(intensity×10) percent. Coverage is 24+intensity×7.2 percent.
Color strength, gradient stops and border strength use the helper's exact
rounding and one-decimal `toFixed` rules. These are source-defined behaviors,
not new fallback policies.

The accent mixes HSL category 66%/52% with danger hue 6, saturation 78%,
lightness 56%, in premultiplied Oklab. A 45° gradient uses two strengths over
rgba(17,30,41,.94), ending at the source coverage; the border mixes the same
accent into white/.06. The frozen browser parses legacy rgba alpha as byte
values before Oklab conversion. Native rendering must preserve the continuous
mix, angle, stops, alpha and consumer overrides in both appearances.

The component defaults to an article and comfortable 20px padding. Compact
uses 16px. It has an 11px radius and 1px border. Its normal shadow is
`0 16px 38px -30px rgba(0,0,0,.92)`. Interactivity defaults to true only for
the button element; an explicit value overrides that visual choice. Interactive
hover lifts the surface 1px and uses `0 24px 48px -28px rgba(0,0,0,.9)`.
Focus uses a 2px sea/30 ring. The surface transition lasts 220ms with premium
easing. Supported source elements are article, button, div, li and section.
A visual interactive override on a non-button does not itself invent keyboard
or button semantics. Native adapters must retain applicable consumer-owned
actions, disabled behavior and real native button focus/keyboard semantics.

## Current foundation

`BleeckerAttentionColor` is an internal numeric foundation. The native surface
and catalog now have a prototype with initial x64 runtime evidence. The color model preserves double precision until pixel
encoding, exact source normalization, premultiplied Oklab mixtures and the
binary-value decimal rounding used by `toFixed(1)`.

`tests/Reference/AttentionColors.json` contains 143 cases evaluated by the
unchanged frozen helper under source-locked TypeScript 5.9.3, then resolved by
Edge 155.0.4283.45. Cases cover negative/wrapped/fractional hues, clamped and
fractional urgency, non-finite inputs and decimal rounding boundaries. The
regression compares source metadata and stop positions exactly, alpha within
CSS serialization precision, and Oklab components within 0.00005. The latter
records small browser-versus-CSS-conversion-matrix differences; it does not
establish pixel parity or permit a different rendered treatment. Native/browser
gradient pixels must still be compared at the actual rendering boundary.
The capture script and source are retained at
`artifacts/bauhaus-browser/capture-attention-color.cjs`.

## Native prototype and verification boundaries

`BleeckerAttentionSurface` exposes `Element` (Article, Button, Div, ListItem,
Section), `Density` (Comfortable/Compact), nullable `Interactive`, required
nullable numeric `Hue`/`Intensity`, ordinary Content/ContentTemplate, Command,
CommandParameter and Click. Null Interactive follows the selected element;
false affects visual interactivity while retaining native button activation.
Non-button Click/Command uses consumer-authorized pointer activation without
adding a keyboard stop or Invoke provider. Arbitrary child actions remain on
the normal routed-event path. Missing numeric inputs fail while mounted;
explicit non-finite numbers retain the frozen helper's defined normalization.

The default instance owns its gradient and border resources. Consumer
Background, BorderBrush, CornerRadius and Padding values/bindings/styles take
precedence. Density supplies padding only when no consumer Padding override
exists, including setters in a Style's BasedOn chain. Solid border colors use
premultiplied Oklab transitions; unsupported non-solid border brushes fail
explicitly. Background supports native brushes. High contrast maps default
paint to system resources and suppresses lift/shadow motion. Actual contrast
verification for this prototype remains pending.

`BleeckerAttentionGradient` renders the frozen 45-degree Oklab treatment into
a per-instance BGRA bitmap at the actual XamlRoot raster scale. The CSS
positioning area is the padding box; the gradient repeats into the border box.
The cache changes only with recipe, dimensions, border or scale and releases
its bitmap when unloaded. Native gradient, border, radius and shadow raster
comparison remains required before acceptance.

The unchanged frozen React component and exact pinned styles were rendered
under source-locked dependencies into 43 cases in each appearance, including
all five elements, both densities, three interactivity settings, disabled,
empty, long, override and wide/tall/square gradients. Styles are verified
against the frozen upstream bytes. Source screenshots and capture script are
retained at `artifacts/bauhaus-browser/capture-attention.cjs`; the catalog embeds
`Reference/AttentionLayout.json`. The source gradient is pixel-identical to an
explicit Oklab gradient; sRGB variants differ. A numeric composition probe
differs by at most two channel values across 162 source pixel samples. This
probe does not establish native-rendered parity or authorize that difference.

`AttentionSurfacesFixture` registers these variants, consumer styles/slots and
actual command, keyboard and hover paths. After the first hover-ownership
correction, x64 passes all 43 cases in each appearance, instance gradient
ownership, native commands/Space, non-button Invoke exclusion, child actions,
consumer style authority and disabled hover. Evidence is retained at
`artifacts/smoke-attention-native-hover-ancestor`. Windows animations are off,
display/text scale is 1, and no preference was changed.

The composed-window pixel capture at `artifacts/smoke-attention-native-pixels`
initially records at most 15 channel values of difference in Light and 2 in
Dark across 81 interior gradient samples each. The large Light error exposes
the native drop shadow under the translucent fill; CSS cuts outer shadows out
of the border box. The correction uses a geometric composition clip for the
visible viewport minus the normalized rounded border contour. Fresh x64 at
`artifacts/smoke-attention-shadow-cutout` passes both appearances; each set of
81 interior samples differs by at most one channel value from the source.
This does not establish full gradient, border, radius, shadow-edge or glyph
raster parity. The component remains in progress.

The interior-sample regression now requires at most one channel value of
BGRA encoding precision against the captured source and writes its report
before asserting that bound. This is limited to those measured interior cells;
it does not relax or replace the full raster acceptance matrix. A rapid
12-cycle remove/re-add regression initially cleared a live bitmap through a
late Unloaded event. Runtime subscriptions now reconcile the final loaded
owner/root after queued lifecycle notifications. Pointer registration retains
its actual observer independently of XamlRoot availability so cleanup can
remove it after detach. The fresh focused x64 run at
`artifacts/smoke-attention-lifecycle-reconciled` passes both appearances,
retains the same interior bound, and verifies template replacement preserves
the consumer child and releases its old visual parent.

Acceptance still requires the complete native surface, all element/density and
interactive/disabled/content/command cases, browser/native gradient and shadow
comparison, actual motion/reversal, contrast, scaling, Narrator, x64/ARM64,
independent package consumption and composition/performance evidence. The
inventory remains in progress until every required acceptance boundary is verified.

The complete x64 catalog passes both appearances at
`artifacts/smoke-attention-full-catalog`. x64 and ARM64 solution builds have
zero warnings, and all 28 unit/resource tests pass. The fresh x64 independent
NuGet consumer passes light/dark resources, gradient geometry, non-button
semantics, actual native button/Click activation and live recipe changes at
`artifacts/package-consumer-attention`. Package version 0.1.0 has SHA-256
`f9ae063786b8b14b4368c0f4002062ba584a4c0017ca25021b3bcfb550d0ade2`.
Native ARM64 runtime of Attention, exact motion/reversal, full raster,
contrast/Narrator/scaling and the remaining contract boundaries stay open.

The native command-availability regression now changes the consumer command's
CanExecute result and raises its real CanExecuteChanged event. Both actual inner
buttons must become disabled, refuse unavailable Invoke without executing,
and become enabled again when the command becomes available. The focused x64
check passes both appearances at `artifacts/smoke-attention-command-availability`.
The cross-component live contrast fixture also includes Attention's system
paint, unchanged pre-transition glyph mask, lift/shadow suppression and producer
image restoration; fresh hosted verification is pending. See
[contrast verification](contrast-verification.md).

The combined native x64 catalog also passes Light and Dark with these checks at
`artifacts/smoke-attention-contrast-breadcrumb-all`. That run uses display/text
scale 1 and disabled system animations; it does not supply enabled-motion,
contrast-transition, ARM64 or Narrator acceptance evidence.

The capture report now retains each gradient specimen's exact client rectangle
and native surface radius, allowing whole-image comparisons without inferring
coordinates from colors. At `artifacts/smoke-attention-complete-raster`, all 18
native/browser images have identical dimensions. Full interior comparisons are
within one RGB level except Light intensity 10, which reaches two; straight
border samples reach two. Rounded-corner differences reach 62 in Light and 44
in Dark. The comparisons are retained at `artifacts/attention-raster-comparison`.
These are diagnostic differences, not accepted raster tolerances.

A temporary fixture-only radius-10 probe is retained at
`artifacts/smoke-attention-radius-diagnostic` and
`artifacts/attention-radius-diagnostic-comparison`. It reduces some Light corner
differences but leaves large differences in both themes and slightly worsens
Dark intensity 10. The probe was removed; the library's public radius and native
stroke-centerline conversion are unchanged. Matching the full curved contour
remains an open acceptance gate.

Run `38494432792` passes the full x64 catalog in both themes and the independent
native ARM64 consumer's initial Attention contract. Its full ARM64 catalog stops
at Notifications before Attention, and the live contrast glyph check fails on
both architectures. CI now includes a focused Attention step on each native
architecture before the full catalog, preserving the full catalog and contrast
requirements. See [runtime evidence](runtime-evidence.md) for the exact failures
and package hash; no inventory entry is promoted to accepted by this checkpoint.

The diagnostic checkpoint builds for x64 and ARM64 with zero warnings and passes
the focused local x64 fixture in both themes at
`artifacts/smoke-attention-hosted-diagnostics`. All 28 unit/resource tests and the
pinned token-generation check pass. The library has no behavior change in this
checkpoint; the new contrast diagnostics and focused native ARM64 CI step still
require their fresh hosted run. This native library has no backend metrics
surface, and its unavailable public wiki is documented in-repository.

The catalog now records controlled fractional-position diagnostics after its
ordinary Attention assertions. It preserves/restores the consumer margin,
foreground, background, density and content, waits for two real render frames,
and captures paragraph origin, text baseline/transform, natural metrics and
physical glyph bounds. It changes only this owned UI, not Windows preferences.

At `artifacts/smoke-attention-fractional-baselines`, whole/half-pixel margin
changes retain all 215 established white glyph positions in both themes. The
compact `Contrast surface` specimen, with the same #F0F4F8-to-white foreground
change as the hosted transition, retains all 124 positions at
`artifacts/smoke-attention-compact-color-baselines`. Adding a consumer
gradient-to-solid background change also retains all 124 positions at
`artifacts/smoke-attention-background-baselines`. None needs a mask offset.
These diagnostics rule out those changes alone as the hosted one-pixel cause;
they do not replace actual Windows contrast verification or establish a fix.

The next candidate places the same measured baseline through balanced top/bottom
text margins instead of a TextBlock render translation. The margins sum to zero,
preserving the source paragraph height and owner sizing while putting the text
position into layout before glyph rasterization. Its local Attention matrix
passes at `artifacts/smoke-attention-layout-baseline` in both themes, including
physical actions, sizing and the existing fractional-position captures. This is
not a verified hosted contrast correction: run `38519659390` at `81a352b`
still adopts only 46/124 established glyph pixels on both architectures.
Moving the baseline into layout did not resolve this defect. The exact established glyph
mask remains unchanged; hosted diagnostics also record margins and each owned
ancestor's composition pixel-snapping setting.
The existing five-state capture is now a regression assertion: it establishes
the first label's solid glyph mask and requires every corresponding position to
adopt each subsequent foreground. `artifacts/smoke-attention-layout-glyphs`
passes all 124 positions across all five states in both themes. It does not
offset the mask, broaden pixel tolerances or replace the hosted contrast gate.
Both updated solution builds pass without warnings. The independently restored
and published x64 consumer passes in both themes at
`artifacts/package-consumer-render-corrections` for package SHA-256
`e3925e5319e063b89417207db4b555f00bebe5943cffadd287f09c9460ec832a`.
The complete local x64 catalog passes at `artifacts/smoke-render-corrections-all`,
including all 124 glyph positions in each of the five controlled transitions
in both themes. Local contrast is off, so actual hosted contrast remains open.

The fractional viewport boundary now reproduces the hosted defect locally.
The original five-state margin/color/background matrix and public visual
inspection pass, but adding a 36.5px ScrollViewer offset fails the established
glyph mask at `artifacts/smoke-attention-scroll-before`. A visual pixel-snapping
candidate also fails at exactly 46/124, matching the hosted result.

The current correction quantizes the native TextBlock ascent on the device grid
before calculating its layout margin. The source-centered target baseline is
unchanged; text origins now land on whole device pixels across fractional
viewport translations. The focused x64 run at
`artifacts/smoke-attention-scroll-native-ascent` passes all 124 established glyph
pixels across seven transitions in each theme, including the 36.5px scroll and
its reversal. It retains the original glyph mask, crop dimensions, exact color
comparison and deadlines. The consumer's margin, content, density, foreground,
background and scroll position are restored. Actual hosted contrast remains a
required separate gate.

The [WinUI glyph-realization implementation](https://github.com/microsoft/microsoft-ui-xaml/blob/main/dxaml/xcp/core/native/text/Common/D2DTextDrawingContext.cpp)
rounds the run baseline during realization. This source explains the diagnostic
hypothesis; the native regression against the repository's pinned SDK is the
evidence for this correction. The failed visual-snapping candidate is removed.
The independently restored/published x64 consumer passes in both themes at
`artifacts/package-consumer-attention-scroll` for package SHA-256
`c95c07878b02225b91711050b08068743083d81e5900c49c9e3a0a58e58f757f`.
Its new fixture waits for its ScrollViewer child to load before applying the
template, then checks consumer paint and whole-device-pixel text origins at
offsets 0.5/37/0.5. The initial fixture called ApplyTemplate before that child
was attached and failed; the repaired fixture was republished in the originally
fresh isolated project, retaining the same hash-verified package and cache.
This consumer check verifies the package's public rendering path; the catalog
glyph mask and real hosted contrast transition remain the physical pixel gates.
The complete local x64 catalog passes both themes at
`artifacts/smoke-attention-scroll-all`, including all seven-transition masks,
the frozen Attention layout matrix and other mounted controls. Both solution
builds have zero warnings and all 38 regressions pass. Windows animations remain
off, display/text scale remains 1 and contrast remains off on the local host.
This renderer change has no backend or metrics surface; actual hosted contrast,
native ARM64 raster and the remaining component acceptance gaps stay open.
Run `38525069103` at `90fdad6` now passes the actual contrast glyph check on
both native architectures. The verified ARM64 artifact records 124/124 matching
positions, zero unmatched pixels, and whole-pixel before/after text origins
Y=429/392. Both focused Attention suites pass, including seven-transition
scroll masks; the ARM64 host has animations enabled. The complete contrast
jobs fail later at Breadcrumb's paired hover colors. This closes the measured
Attention glyph defect, not the full component's acceptance gaps or required CI.

The expanded native ancestor diagnostics exposed a separate width defect:
the compact contrast specimen's owner is 320px wide, while its inner native
Button and painted border are only 148.735992px wide. The initial source-layout
regression measured the owner alone. WinUI's Button has a Left alignment
default ([alignment contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.horizontalalignment?view=windows-app-sdk-2.0));
the template must explicitly stretch its owned action to the consumer-sized
surface. Regression coverage now requires the painted surface and actual
native action to fill the wrapper and verifies physical activation at the far
edge, beyond the old content-sized action. This is a distinct geometry/input
boundary; the contrast glyph-mask and complete raster gates remain required.

The new regression fails against the old native template at
`artifacts/smoke-attention-sizing-before-fix`: the comfortable button paints
225.232025px inside its 320px wrapper. The template now explicitly sets
HorizontalAlignment and VerticalAlignment to Stretch on its owned native
Button. The corrected focused x64 run passes both appearances at
`artifacts/smoke-attention-sizing-fixed`, including all 86 frozen layout cases,
native action and border dimensions, live widths 416/230/320 and a physical
far-edge click with the original consumer command/parameter. x64 and ARM64
builds have zero warnings, and all 32 unit/resource regressions pass. Windows
animations remain off and display/text scale remains 1 on the local host.
This fixes the measured sizing/action defect; it does not establish complete
Attention visual or platform acceptance.

The fresh independent x64 NuGet consumer passes the corrected native action
and painted bounds in both appearances at
`artifacts/package-consumer-attention-sizing-fixed`. Package 0.1.0 SHA-256 is
`c268545330db09d4771a87c3299313a00114e919fa015c29290483bdb57cd5f8`.
The complete local x64 catalog passes Light and Dark with the sizing correction
at `artifacts/smoke-attention-sizing-all`. Fresh native ARM64 and hosted contrast
checks remain required; the correction does not relax either gate.
