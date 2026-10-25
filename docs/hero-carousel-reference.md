# Hero Carousel source contract

Hero Carousel is a reusable component in progress, within the
[component-library scope](library-scope.md). Its pinned Bleecker source is
`src/components/hero-carousel.tsx` at commit
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`2338fc09fd0f22f36fe9f2ead221b18b7596b5555581c2c97eba03e5c3191012`.
The sealed source preparation does not establish native acceptance. A newer
[public native preview and catalog fixture](hero-carousel.md) are implemented;
fresh NuGet and Windows acceptance remain open. Both fictional Monitoring
and Editorial component demos remain required.

The caller owns ordered items, original titles/descriptions/destinations,
optional IDs, image URLs, kind, tags, external intent and CTA label. Default
links retain the supplied literal destination; external links use a new target
and noreferrer/noopener. A custom renderer receives the original item, prepared
content, class recipe and accessible label. It may supply its own link/content
and callback. No product router, image service or URL reconstruction belongs
in the Windows component.

Kind defaults to project for accessible labels and kicker/CTA text. Tags are
shown only for an explicitly project-kind item, which is a separate source
condition. Empty CTA text uses the kind default. An omitted or empty image URL
uses the source gradient; an image decoding error does not select that gradient.
The image path layers cover cropping, dark overlays, two directional gradients,
soft-light radial tint, overlay grain and a final radial vignette. Those visual
layers must be compared rather than replaced with an unrelated stock treatment.

The component starts at numeric index zero. Previous/next and indicator buttons
change this index; interval rotation defaults to 5,000ms. The interval exists
only with multiple items and no hover pause. Item count, interval or pause
changes recreate it; same-count data replacement and manual navigation do not
restart its clock. Hover pauses, mouse leave restarts a full interval. Focus
alone adds no pause. Keyboard listeners are document-wide with multiple items;
Arrow Left/Right move except when the active element's tag is INPUT or TEXTAREA.
The source adds no per-carousel focus requirement or event cancellation.

Data updates retain the numeric index instead of clamping it. Shrinking below
that index can leave every slide hidden; a single remaining item has no timer
or navigation controls to recover it. Empty content has a separate 600px/rem
equivalent surface with caller content or the default empty message. Restoring
items does not itself reset the stored index. Keyed slide/input lifetime depends
on the explicit ID, otherwise title plus destination. Capture these boundaries
before implementing the Windows lifecycle.

Multiple slides remain mounted with opacity and aria-hidden selection. The
source has no explicit carousel role, live-region, aria-current indicator or
focus suppression on inactive links. Navigation arrows are opacity-zero until
hover at widths of at least 768px; below that breakpoint they stay visible.
Their native browser focusability/hit testing remains present. The measured
Tab sequence includes all three links, including the two inactive links, before
reaching the arrows. Clicking visible title content reaches the active item;
clicking empty hero space reaches the last, invisible item. The source's nested
stacking contexts therefore matter. These hazards are recorded evidence,
not Windows acceptance; the native input/focus adaptation still needs an
explicit contract and runtime verification.

Visual acceptance covers both themes, no image and local image content,
empty/custom empty, one/multiple items, kinds/tags/CTA/custom link, constrained
width, responsive title sizes, 200% text, long/multilingual/RTL content,
hover/focus/pressed controls and indicators, and interrupted normal motion.
Controls require multiple items and ShowControls; indicators additionally
require ShowIndicators. Hidden indicator/control options must keep interval
and keyboard behavior separate. The 500ms slide opacity transition and token
control/surface transitions require their own motion evidence; reduced-motion
endpoints do not establish them.

Before native acceptance, verify original caller data/content/commands, exact
image ownership and error state, live updates, keyed retention and cleanup,
timer/hover/keyboard boundaries, appropriate Windows input/focus/UIA metadata,
registered catalog variants, both architectures, source and isolated fresh
NuGet runtime, and full rendered visual comparison. Background preparation
does not authorize foreground Windows input, Narrator, OS settings, pointer
heartbeat or GitHub Actions.

The central standard and public wiki could not be reached. Checked-in managed
rules and repository documentation govern this preparation.

## Verified preparation

`tests/Reference/HeroCarousel.json` freezes 86 original PNGs, 54 histories, nine
pinned source files and two fictional local SVG test images. The source and an
independent headless Edge recapture match the full receipt and every PNG byte.
No source component or screenshot is transformed. React/React DOM 19.2.4,
Lucide 0.563.0, Tailwind 4.2.2 and 1,694 actual bundle inputs are recorded.
The initial two capture-development attempts remain retained; the frozen
reference uses the expanded third capture with explicit non-hover placement,
767/768px boundaries and both background/class slots.

The independent audit checks 4,526 geometry values, 3,372 paint values, 234 exact
source palette probes and decodes 64,175,920 pixels. Centering follows Chromium's
1/64px layout units, including RTL allocation. The two narrow-width gradient
midpoint samples retain their measured raster quantization; they are not
recolored to the CSS stop value or hidden by a broad channel tolerance. Both
independent captures establish these same exact samples. Ninety damaged cases
are rejected, including a real wrong-theme PNG with a recomputed hash, changed
source/image bytes, incorrect callback identity and silently repaired source
interaction quirks.

Seven source-backed portable regressions join the previous 418 tests: all 425
pass. The internal `BleeckerHeroCarouselState` records numeric selection,
source-key conditions and interval-generation cleanup. It retains invalid
indices across shrinking/single/empty lists, keeps a timer generation across
same-count changes and manual moves, and retires that generation on hover,
interval/count changes or removal. It consumes actual tick events; it does not
simulate Windows scheduling or invent missed-tick catch-up. That source checkpoint
predates the public Windows host, native timer/input adapter and fictional
catalog fixture. The [current native preview](hero-carousel.md) now covers
source/fresh NuGet runtime, caller font/brush updates, remount navigation,
both architecture builds and the extracted catalog archive. This source
preparation remains immutable historical evidence rather than proof of those
newer native results.

Reproduce with `scripts/reference/capture-hero-carousel.cjs --source-root ...
--tooling-root ... --playwright-module ... --output <fresh-directory>`, followed
by `verify-hero-carousel-source.mjs --reference <HeroCarousel.json> --frames ...
--recapture <independent-directory> --output <fresh-audit.json>` and
`test-hero-carousel-source-audit.mjs --reference ... --frames ... --output ...`.
Both auditors support explicit `--frozen-only` mode without the original local
tool cache; that mode reports zero bundle inputs rehashed. The default mode
verifies all original bundle files and fails if they are absent.

The [React effect contract](https://react.dev/reference/react/useEffect) and
[keyed lifetime documentation](https://react.dev/learn/preserving-and-resetting-state)
describe the general mechanisms; the pinned 19.2.4 runtime captures above
establish this component's actual behavior. The exact 19.2.4
[effect implementation](https://github.com/react/react/blob/v19.2.4/packages/react-reconciler/src/ReactFiberHooks.js)
and [keyed reconciliation implementation](https://github.com/react/react/blob/v19.2.4/packages/react-reconciler/src/ReactChildFiber.js)
are the version-specific references. [HTML timers](https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html#timers)
do not guarantee exact real-time scheduling. Source clock histories establish
event boundaries, not native timer precision or normal CSS/native tween motion.

Hero Carousel is in progress in the fixed inventory. Current counts are 108 in
progress, four pending and zero accepted: 96.4% entries started, never completion.
The sealed preparation retains its historical 107/5/0 counts. Physical Windows input/focus,
complete native visual/image/motion/scaling/customization and native ARM64
runtime acceptance remain unverified. Both fictional component demos remain
required. No Narrator, OS settings, pointer heartbeat, foreground input or
GitHub Actions were invoked.

The [native preview](hero-carousel.md) now includes live tag/CTA/arrow/indicator
underlays and glyph shadows. Native source checks cover visible on/off pixels,
original glyph surfaces after font/text changes, four-corner clips, RTL resizing
and empty/restore cleanup. These additions preserve the frozen source inputs;
they do not establish complete source/native raster equivalence.
