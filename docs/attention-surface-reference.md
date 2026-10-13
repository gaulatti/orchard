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

`BleeckerAttentionColor` is an internal numeric foundation; the native surface
and catalog entry are still pending. It preserves double precision until pixel
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

Acceptance still requires the actual native surface, all element/density and
interactive/disabled/content/command cases, browser/native gradient and shadow
comparison, actual motion/reversal, contrast, scaling, Narrator, x64/ARM64,
independent package consumption and composition/performance evidence. The
inventory remains pending until its native implementation and fixtures exist.
