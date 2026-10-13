# BrandLockup reference and acceptance

The frozen source is
[`brand-lockup.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/brand-lockup.tsx),
SHA-256 `8b2319f72c31ffb0c5e775484ed3b59d2ceeb59a4e73a779c805053585d601e6`.
Its downloaded source matches the inventory hash. Implementation remains pending.

## Required native contract

- Preserve the consumer's exact href and custom link-rendering path. The
  renderer's item label is the supplied name when non-null, otherwise the
  logo's alternative text. The default link's accessible name derives from its
  image alternative text and visible name content. An empty name differs from
  a null name: it supplies the renderer's item label but does not
  render the name/separator group.
- Default alternative text is `gaulatti`. Use the pinned SVG from
  `src/assets/logo.ts`, with its 1000×1289.11 view box and six exact paths.
  A consumer-provided logo source remains authoritative.
- Small/large logo heights are 32/48; width preserves the source aspect ratio.
  Named lockups have a 16px gap around a 1px separator of the same height.
  Text uses Encode Sans Semibold at 20/30 with 28/36 line height and -0.02em
  tracking. Separator colors are sand at .55 opacity and dark white at .18.
- Logo opacity is .9 normally and 1 on link hover. Dark appearance inverts logo
  RGB, including custom sources, while preserving alpha. The frozen generated
  CSS resolves `duration-400` to **220ms**, rather than the class's numeric name.
- Preserve native link/command activation, keyboard focus and accessible names;
  supply Windows contrast adaptation without substituting brand colors.

The implementation must verify logo-only/named, both sizes, empty/null labels,
custom images and link templates, runtime updates, long/non-Latin content,
constrained width, native pointer/keyboard activation, theme changes, reduced
motion, package resources and an independent consumer. Full rendered source
comparison, x64/ARM64, contrast, scaling and Narrator remain required before
acceptance. No current preview is claimed.

The public wiki remote is unavailable; the contract is maintained here.
