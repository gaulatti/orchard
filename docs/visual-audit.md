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

Open comparison work includes shaping, line heights and text baselines, exact underline offset, branded focus rings, shadows, disabled group opacity/compositing, icons, loading indicator, normal and reduced motion, and a complete rendered variant/size/theme/state matrix. Native-template behavior alone cannot close these gaps.
