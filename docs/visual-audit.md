# Rendered Bleecker comparison

Reference: `gaulatti/bleecker` at `a86059013cd59684ce452849278ea81e734f2066`, local Storybook `components-button--complete-hierarchy`. Measurements below come from the rendered DOM, alongside source recipe inspection. They are reference evidence, not a claim that every Orchard state matches.

| Dark-theme reference | Observed result |
| --- | --- |
| Primary background / text | `#5ba3f5` / `#182533` |
| Secondary background / border | `#182533` / white at 18% |
| Subtle background | white at 7% |
| Button typography | Encode Sans, weight 500 |
| xs / sm / md / lg height | 28 / 36 / 40 / 48 px |
| xs / sm / md / lg text | 12 / 13 / 14 / 15 px |
| xs / sm / md / lg horizontal padding | 10 / 14 / 18 / 24 px |
| Link text / line height / height | 16 / 24 / 26 px |
| Link idle decoration | none |
| Link hover decoration / color | underline / `#0d1821` |
| Button corner radius | 7 px |

The reference browser currently reduces transition durations to 0.01ms. Normal-motion acceptance must use the source's 190ms control duration and premium cubic-bezier curve, then be exercised with reduced motion disabled. The observed reduced-motion render does not verify normal animation.

The dark link hover color has poor contrast against the reference surface. Orchard retains the requested reference color; this is an upstream design issue requiring an explicit reference change rather than an undocumented Windows substitution.

Corrections made: hover-only link underline, size-independent 16px link text, dark primary text, literal-white overlays instead of semantic text colors, secondary hover fill/border opacity, and destructive white text. Independent runtime assertions check secondary hover values as well as generated-resource resolution.

Control styles now explicitly select the bundled Encode Sans Medium face and weight 500. The font file's pinned hash and OS/2 weight metadata are tested; published catalog and independent package checks verify font resolution. Full shaping and baseline comparison remains open.

Buttons now specify a 2px outer focus ring and 2px reference-colored offset. Ring opacity is 35% light / 45% dark for text buttons and 30% / 40% for icon buttons. Toggles use an opaque 2px ring without an offset. Light uses sea; dark uses accent blue. Contrast resources use the system highlight/window colors. Native keyboard focus and navigation remain active.

Desktop Tab testing verifies the branded ring on the foundational secondary action in both themes and on the custom secondary-button wrapper in light. Captures are `artifacts/focus-dark.jpg`, `artifacts/focus-light.jpg`, and `artifacts/focus-wrapper-light.jpg`. Runtime assertions cover button variants and toggle geometry in both themes. Full focus acceptance still requires the remaining sizes, icon/toggle physical focus, edge clipping, actual contrast settings and scaling.

Focus implementation uses the pinned WinUI API's [outer/inner thickness](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.focusvisualprimarythickness?view=windows-app-sdk-2.0) and [negative outer margin](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.focusvisualmargin?view=windows-app-sdk-2.0) properties.

The button loading indicator now renders the pinned Lucide LoaderCircle path in a 24-unit viewbox scaled to 16px, with 2-unit round strokes. Runtime checks inspect the vector, observe rotation when Windows enables animations, and verify stop/reset after loading. The smoke report records which motion branch was actually exercised. This host reported animations disabled: the static reduced-motion branch passed; animated rotation remains unverified. Light/dark captures show the new arc. Live reduced-motion switching, unload/reload stress and older-Windows behavior remain open; this does not implement the standalone bordered-circle LoadingSpinner.

Icon-button reference `components-iconbutton--variants` renders 18×18 Lucide icons with viewbox `0 0 24 24`, two-unit round strokes and dark foreground `#f0f4f8`. The catalog now uses matching consumer-owned Lucide vectors instead of Windows SymbolIcon glyphs. Light/dark native captures were inspected. Runtime assertions verify vector dimensions and stroke properties, plus state foreground propagation across all three variants. This exposed and fixed nested Control content retaining its enabled color during the native disabled state. Consumer foreground values/styles and content replacement retain ownership. The independent package example uses a Lucide Save vector. Icons on other component surfaces remain part of their own pending acceptance.

The standalone `components-loadingspinner--default` browser reference renders a 32px ring with 2px border, transparent top border and dark accent blue `#5ba3f5`. Orchard's separate `BleeckerLoadingSpinner` renders the three 16/32/48px sizes with 2/2/3px flat-ended three-quarter arcs. Light/dark screenshots and native geometry/color assertions verify those static cases. Reduced motion, visibility and unload reset checks pass; normal-speed animation, live settings changes, contrast and scaling remain open. See [spinner evidence](loading-spinner.md).

Card reference `components-card--surface-hierarchy` confirms 24px default padding, 12px corners, dark deep-sea fill, white borders at .09/.15/.10 for Surface/Outlined/Elevated, and .035 white fill with .07 border for Subtle. The custom elevated shadow uses the two canonical raised-shadow layers; native layer counts and static surface recipes are checked. The catalog adds all five variants and four padding sizes with wrapping consumer content and a working child action. Exact blur rasterization, clipping and surface transitions remain open; see [card acceptance](cards.md).

Open comparison work includes shaping, line heights and text baselines, exact underline offset, the remaining focus matrix, shadows, disabled group opacity/compositing, remaining icons and loading/motion cases, and a complete rendered variant/size/theme/state matrix. Native-template behavior alone cannot close these gaps.
