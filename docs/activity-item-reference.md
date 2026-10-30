# ActivityItem reference and acceptance

The fixed source is
[`activity-item.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/activity-item.tsx),
SHA-256 `69fe700423f71b440d505428187b957df880d68e52ae6af634545744f7af74aa`.
The downloaded source matches the inventory. `BleeckerActivityItem` is
registered after Bauhaus in the catalog. For the current x64 milestone it passes
all five checks in the [acceptance contract](acceptance-contract.md): applicable
Light/Dark states, physical keyboard/pointer/focus, native 150% no-clipping,
reduced motion, and independent package-consumer compilation. The package-matching
`.34` catalog passed its Light/Dark route; the independent `.34` consumer passed
six 150% cases covering long, empty and RTL content. The source has no disabled
or error variant. ARM64 runtime remains unverified for this milestone. The
broader parity investigations below are historical and add no current gate.

The consumer owns three required strings: `init`, `title` and `time`. The source
does not parse or format time, truncate title, expose an image URL, provide a
selection state or define an activation callback. Empty strings are valid;
there is no fabricated default person, title or timestamp. Avatar's existing
empty-fallback silhouette and initials behavior still apply.

The row uses 16px horizontal/vertical padding, a 14px gap, a small 32px Avatar,
and a flexible zero-minimum-width text column. Title is 13px Encode Sans Medium
with a 20px line height. Time is 11px Libre Franklin with the source's inherited
1.5 line height (16.5px); the paragraph gap is 6px. Both paragraphs wrap.
The bottom border is 1px sand/20% in Light and white/7% in Dark. Pointer hover
changes only the row background to light-sand/35% or white/3%, over the
220ms surface duration and premium `cubic-bezier(0.22,1,0.36,1)` easing.
The cursor is a hand. The source has no pressed visual state or disabled
variant. A native adapter must not invent a navigation route or activation
command merely because the source uses a hand cursor.

The native contract must preserve consumer strings exactly, reuse Avatar,
support wrapping and consumer layout, avoid keyboard focus without an action,
and provide a coherent accessible container with title/time content. Consumer
list/selection or ancestor activation remains outside this component.
Windows contrast must pair semantic surfaces and text. Reduced motion must
use immediate endpoints; live theme, unload/reload and template replacement
must not retain stale hover or subscriptions.

The earlier parity program required normal/hover and both appearances, empty/long/multiline/
non-Latin/RTL strings, constrained widths, consumer updates, semantic UIA,
actual pointer and motion/reversal, meaningful native/browser appearance,
display/text scaling, x64/ARM64 and independent NuGet use. Narrator and OS
contrast campaigns are excluded from the [current library scope](library-scope.md).
The source supplies no error/disabled state; the inventory's generic labels
must be classified from this actual contract rather than creating new styling.
The public wiki remote is unavailable; the contract is maintained here.

## Native usage

```xml
<orchard:BleeckerActivityItem Init="Alice Harbor"
    Title="Consumer-owned activity title" Time="5 minutes ago" Width="400" />
```

`Init`, `Title` and `Time` must all resolve while mounted; missing values fail
explicitly at load or a live property update. An empty paragraph is collapsed,
with its dedicated 6-DIP sibling gap retained. The Avatar remains 32×32.
The native Group peer exposes title/time children and has no Invoke pattern or
keyboard stop. An ancestor can handle normal routed `Tapped` events without
the component intercepting them. `Padding`, `BorderThickness`, `BorderBrush`,
`Foreground`, `Background`, width and `Template` remain consumer-owned WinUI
properties. Animated `Background` supports a solid color brush. The default
implementation rejects other brush types explicitly; a consumer
composition can place additional visuals behind its transparent surface.
No string-to-date, route or selection policy is introduced.

## Reference comparison and verification

`samples/Orchard.Catalog/Reference/ActivityLayout.json` comes from the unchanged
frozen React component and Avatar, the pinned global/generated CSS and `cn`
helper, compiled using the source lock's Tailwind/CLI 4.2.2, React/DOM 19.2.4,
TypeScript 5.9.3, clsx 2.1.1 and tailwind-merge 3.5.0. Edge
155.0.4283.45 uses Orchard's bundled Encode Sans/Libre Franklin font files.
The isolated browser evidence and script are retained under
`artifacts/bauhaus-browser/capture-activity.cjs`; original source SHA is checked
before rendering. Light and Dark browser captures are retained beside it.

At 400px width and display/text scale 1, normal and RTL rows measure 75.5px,
the empty row 65px and the long multilingual row 115.5px. Native regressions
compare row/paragraph heights and visible paragraph offsets against these
browser measurements, including empty-title/time updates. The comparison
currently requires scale 1 explicitly.

The focused native check (`-Component activity`) exercises actual pointer
hover/exit, movement across child elements, global Hand cursor, ancestor tap,
physical Tab skipping actionless rows, exact content updates, template
replacement, unload/reload and live theme changes. It records actual rendered
hover frames using the user's existing animation preference. Full catalog
verification runs the same check; hosted contrast verification tests the live
Windows Highlight/HighlightText pair and restoration to WindowText. The
independent package consumer loads the published template/fonts, verifies
exact strings and UIA, and exercises an empty-title update in both themes.

Constrained-width and consumer-override matrix, meaningful text and Avatar
appearance parity, precise motion timing and interrupted reversal, and
display/text scaling remain open.
Successful layout and endpoint checks do not close those acceptance gaps.

The complete x64 catalog passes both appearances at
`artifacts/smoke-activity-all` with animations disabled and display/text scale 1.
Both architecture builds have zero warnings; all 27 unit/resource tests and
the token-generation drift check pass. The fresh independent x64 consumer
passes at `artifacts/package-consumer-activity`, using package 0.1.0 with SHA-256
`a4456e0e5a4b736f61f3bd87520fadd3e14ec7eae8caddc5cd9cdcfbeb8af770`.
This brings the registered inventory to 27 in-progress entries and 85 pending;
none has full acceptance.

The next focused capture at `artifacts/smoke-activity-capture-origins` records
the actual client origin, row/text screen rectangles, native baseline offsets
and colors in `activity-capture-{Light,Dark}.json`. At scale 1, the title/time
line boxes still begin at row offsets 16/42, while their native baseline
offsets are 16.4799995/13.1509905. Their row-relative baselines are therefore
32.4799995/55.1509905. Inline zero-size baseline probes in the unchanged frozen
browser component measure 31/54. The measured-origin Latin ink comparison
finds native title rows 21–34 and time rows 47–56, against browser 20–33 and
46–55. Colors match. This is an open text-placement defect, not an accepted
platform substitution; a correction must preserve line-box layout and prove
the actual rendered baseline, including wrapping and scaling.

The first hosted x64 candidate at `64a2b27` passes focused Activity Item,
the full catalog and independent consumption. Contrast verification initially
fails because both Activity Item and its nested Avatar name a border
`PART_Surface`; selecting all descendants with `Single` is ambiguous. The
fixture now selects Activity Item's direct template root and requires its
expected name before checking colors. The historical contrast check remains
unverified for its original binary; OS contrast campaigns are excluded from
current component acceptance.

The ARM64 candidate in that same run also passes focused Activity Item with
animations enabled, the complete ordinary catalog and independent package
consumption. Its contrast fixture reaches the same ambiguous selector failure.
This verifies the registered runtime, endpoint/motion-preference and consumer
checks on both native architectures; it does not close the baseline, complete
motion or scaling gaps.

The baseline correction now centers the natural font line box in the declared
line height and aligns the resulting baseline to the current display's pixel
grid, using live WinUI font metrics. It introduces no fixed vertical offset.
Separate paragraph hosts preserve the source 20/16.5-DIP line boxes and 6-DIP
gap while the text drawing transform corrects the baseline. Zero-size,
transparent metric probes contribute no layout or pointer target; the Group
peer explicitly excludes their peers while retaining the real Avatar/title/time
children. Template replacement clears both the probes and drawing transforms.

The regenerated frozen browser reference includes measured baselines and
normal-row ink extents. Focused x64 at
`artifacts/smoke-activity-baseline-regression` passes both appearances: native
title/time baselines are 31/54 row-relative DIPs, title ink rows are 20–33 and
time ink rows 46–55, matching the actual browser capture. The regression parses
the composed native PNG using its measured client/row origin and compares ink
extents using the same browser-reference threshold and region. This checks
vertical placement, not complete antialiasing or whole-glyph raster identity.
Wrapping/paragraph geometry and actual pointer/Tab/lifecycle/UIA checks remain
required and pass in this focused candidate. Native ARM64, scaling and the
remaining complete visual/interaction matrix still require verification.

Custom templates must retain the named `PART_TitleParagraph` and
`PART_TimeParagraph` Grid hosts plus the `PART_TitleMetrics` and
`PART_TimeMetrics` TextBlocks, alongside the existing content/palette parts.
The metrics use the corresponding text font/size and a natural line height;
they must remain measured while contributing no visible layout or accessible
content. Missing parts fail template application explicitly. The independent
consumer checks the nested paragraph structure and exact title/time strings.

The complete x64 catalog passes both appearances at
`artifacts/smoke-activity-baseline-all`; the final stable-origin focused check
passes at `artifacts/smoke-activity-stable-baseline`. Both architecture builds
have zero warnings and all 27 unit/resource tests pass.

The fresh independent consumer passes both appearances at
`artifacts/package-consumer-activity-baseline`, using package 0.1.0 with SHA-256
`a24c2d4579c97977a21c5e8f271b005c929b6095f5b24181d57680de88c258c4`.

Native ARM64 at `1cf7c76`, run `38485003280`, passes the focused row fixture
in both appearances with animations enabled and display/text scale 1. Its
composed-image ink checks match title rows 20–33 and time rows 46–55, and its
actual pointer/Tab, lifecycle, UIA and motion endpoint/intermediate checks pass.
The same run passes live contrast and independent native package consumption.
The ordinary ARM64 suite fails an unrelated Field Help brush assertion. These
results verify the row's registered ARM64 checks, leaving complete glyph raster,
motion/reversal, overrides, scaling and full acceptance open.

On 2026-10-27, a source-built x64 catalog failed the focused Activity fixture
because its row began at a fractional screen Y. The title baseline landed on
the correct device pixel, but the fixture compared its row-relative coordinate
to the browser's integer-origin value. The composed-ink sampler likewise
truncated the screen origin and reported both glyph spans one row low. The
fixture now compares baselines after pixel alignment and rounds the capture
origin; the existing browser ink extents still have to match exactly. It also
changes the catalog's actual theme root during its live-theme check. The
focused light/dark smoke passes, with title ink rows 20–33 and time rows 46–55
in both appearances. The combined presentation smoke advances to Tooltip and
fails there. See `docs/evidence/activity-catalog-pixel-origin-v1/checkpoint.json`.
This corrects catalog verification, without changing the library control or
qualifying a new package.
