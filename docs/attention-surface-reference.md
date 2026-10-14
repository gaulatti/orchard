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
