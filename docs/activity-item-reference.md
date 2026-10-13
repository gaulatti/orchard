# ActivityItem reference and acceptance

The fixed source is
[`activity-item.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/activity-item.tsx),
SHA-256 `69fe700423f71b440d505428187b957df880d68e52ae6af634545744f7af74aa`.
The downloaded source matches the inventory. `BleeckerActivityItem` is an
in-progress native implementation, registered after Bauhaus in the catalog.

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

Acceptance requires normal/hover and both appearances, empty/long/multiline/
non-Latin/RTL strings, constrained widths, consumer updates, semantic UIA,
actual pointer and motion/reversal, native/browser rendering, all contrast
themes, display/text scaling, Narrator, x64/ARM64 and independent NuGet use.
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

Complete browser/native text and Avatar raster parity, constrained-width and
consumer-override matrix, precise motion timing and interrupted reversal,
all Windows contrast schemes, Narrator and display/text scaling remain open.
Successful layout and endpoint checks do not close those acceptance gaps.

The complete x64 catalog passes both appearances at
`artifacts/smoke-activity-all` with animations disabled and display/text scale 1.
Both architecture builds have zero warnings; all 27 unit/resource tests and
the token-generation drift check pass. The fresh independent x64 consumer
passes at `artifacts/package-consumer-activity`, using package 0.1.0 with SHA-256
`a4456e0e5a4b736f61f3bd87520fadd3e14ec7eae8caddc5cd9cdcfbeb8af770`.
This brings the registered inventory to 27 in-progress entries and 85 pending;
none has full acceptance.
