# Breadcrumb reference and Windows contract

The fixed Bleecker commit is `a86059013cd59684ce452849278ea81e734f2066`.
`src/components/breadcrumb.tsx` has SHA-256
`46f67eb20c2038b3d9920555b528b39c02b3fa6e8ec271e1f99fcd1b91ee55c0`;
its default link renderer comes from `nav-menu.tsx`, SHA-256
`1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5`.

Before implementation, the actual source was rendered with empty/single paths,
five items, collapse thresholds 1/2/-1, the exact threshold, custom links and
separators, a 140px width, Japanese/Latin text, and path replacement after
expansion. Light/dark measurements and screenshots are retained locally as
`artifacts/breadcrumb-reference-{Light,Dark}.{json,jpg}`. The source loaded the
Encode Sans font. These source observations establish acceptance inputs;
they do not establish native acceptance.

## Observable acceptance criteria

- Default collapse threshold is 0. Collapse only when the positive threshold
  is exceeded by more than one item; collapsed output always shows the first
  item, ellipsis, and last item, including threshold 2. Expanding remains
  latched through collection replacement, item updates and template changes.
- Every preceding item is a link with the original consumer item; the final
  item is noninteractive current-page text. Consumer href/external metadata
  stays intact. Native commands/events leave routing to the application.
- Text uses Encode Sans 14px/20px, weight 400 for links and 500 for current text.
  Link/chevron ink is #595959 in Light and #B8C5D6 in Dark. Current/hover text
  is #2D2D2D/#F0F4F8. Link hover has no background or underline.
- Each link, separator, ellipsis and current item wraps independently, with
  6px horizontal and vertical gaps and center alignment within each row.
  A five-item 140px path is 72px tall at source scale 1. Empty content is 0 tall.
- Breadcrumb disables WinUI layout rounding within its own subtree. The initial
  native link widths 37/44/43/38 accumulated a 1.5px current-item displacement;
  source widths are 36.984375/43.453125/42.171875/37.890625. The native fixture now
  requires rounding to remain disabled and advances to agree within 1/32px.
  At `6742e1d`, both architectures produce 37.86300277709961 for Drafts versus
  browser 37.890625: a .02762222290039px shaping/layout difference exceeds one
  browser quantum but stays below two. The 1/32px bound retains sensitivity to
  the earlier .546875/.828125px rounding errors. This small difference remains
  recorded; the advance check does not claim exact glyph/raster acceptance.
- Chevron and ellipsis are the source Lucide vectors at 14px. The ellipsis
  button is 24px square with a 6px radius; hover uses sand at 10%/20% opacity,
  pressed scale is .9, and control motion uses the premium .22,1,.36,1 curve.
  Reduced motion settles immediately. Theme changes update live content.
- Custom link and separator templates receive consumer content and suppress
  the default presentation. The current item remains current-page text.
- Native keyboard and pointer activation, current-page accessibility, focus
  after expansion, binding/collection changes, reuse and template lifecycle
  must pass on x64/ARM64. Contrast uses Windows semantic colors. Narrator,
  scaling, focus visuals and full source/native raster/motion acceptance remain
  required before this inventory entry can be complete.

## Windows API

`BleeckerBreadcrumb.Items` is a consumer-owned
`ObservableCollection<BleeckerBreadcrumbItem>`. Each item carries `Label`,
`Href`, `External`, `Command`, and `CommandParameter`. `ItemInvoked` exposes the
original item on a default-link activation. `LinkTemplate` replaces preceding
links and receives that item as its data context; its routing and activation
belong to the consumer. `SeparatorTemplate` creates an independent separator at
each position. `CollapsedAfter` follows the source threshold and lifetime latch.
Separator accessibility excludes its entire subtree, matching source aria-hidden.
Internal action styles are loaded separately and assigned only to default
links/ellipsis, so their native templates do not override consumer template controls.
Contrast-mode ellipsis hover pairs Windows Highlight with HighlightText, and
system color changes repaint its live palette. Manual contrast-theme acceptance
is still pending.
Default links and the expansion button use the existing branded Windows focus
ring. Keyboard expansion restores focus to the first focusable revealed link,
including a consumer link template's native focusable descendant. Disabled or
non-focusable renderers do not receive focus, and the current-page text is not a
target. Native [focus discovery](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.focusmanager.findfirstfocusableelement?view=windows-app-sdk-2.0)
preserves the consumer's tab order and focusability. The custom renderer fixture
expands with real Enter, verifies the original item/focused Button and continues
with real Tab to the next revealed consumer link. Full focus visual, Narrator and
scaling acceptance remains open; these new checks require fresh native runs.
Both default and custom renderer regressions also disable the first revealed
item through consumer `CanExecute(false)` and require focus on the next available
link. No command is invoked merely to restore focus.

Local x64 checks pass in both themes under
`artifacts/smoke-breadcrumb-custom-disabled`; the composed Dark capture shows
the consumer's visible focus ring. The complete ordinary catalog passes at
`artifacts/smoke-focus-contrast-all`. The independent NuGet consumer also passes
both themes and custom-template restoration, using package SHA-256
`4a7f261e03a1e0675123ff646ee97885798ccb73bda1813269f038f498c9ce0f`, with evidence
at `artifacts/package-consumer-custom-focus-ready`. Both architecture builds have
zero warnings and all 26 unit tests pass. Fresh ARM64 runtime acceptance remains
required for this change.

```xml
<orchard:BleeckerBreadcrumb CollapsedAfter="2">
    <orchard:BleeckerBreadcrumbItem Label="Home" Href="/home" />
    <orchard:BleeckerBreadcrumbItem Label="Library" Href="/library" />
    <orchard:BleeckerBreadcrumbItem Label="Stories" Href="/stories" />
    <orchard:BleeckerBreadcrumbItem Label="Harbor" Href="/harbor" />
</orchard:BleeckerBreadcrumb>
```

Handle `ItemInvoked` or supply item commands to navigate. The catalog includes
ten paths and per-theme physical hover/keyboard expansion, collection replacement,
item updates, unchanged-key reuse and template-restoration regressions. The clean
package consumer includes a four-item collapsed path.

The initial x64 run `38362755594` at `2d6ff12` passes the complete Light
Breadcrumb phase, then fails its Dark hover-palette assertion. The checked archive
`11795722319`, SHA-256 `120aa97534d35d848fe2b2776c6cb07fbd131a66fc8f95d6b8c970d1c9174666`,
contains the passing Light report/window capture and all pre-hover Dark images.
The fixture expected Light sand in Dark. The actual pinned source's computed
`--color-sand` is #1A2332 in Dark, with the explicit dark:hover:bg-sand/20 rule;
its expected 20% brush is #331A2332. Light remains #1AE6D5B8. The product's
generated brushes already consume the correct theme tokens. The fixture now
asserts those exact source values, and retains target/painted palettes, native
common state, bounds, foreground ownership and preceding measurements. A full
native Breadcrumb pass on both architectures is recorded below.

Run [38370125528](https://github.com/gaulatti/orchard/actions/runs/38370125528)
at `2d7fdc9` passes the x64 and native ARM64 catalogs and independent package
consumers, including physical ellipsis hover, keyboard expansion/focus,
fractional path geometry and the shared global visible-cursor gates. This
establishes the registered runtime checks, not full visual acceptance.

The fixture now additionally holds a real pointer press until the ellipsis
settles at scale .9, captures it, then releases outside the action and requires
scale 1, transparent chrome and the unchanged collapse latch. It also removes
and remounts an expanded path, mutates consumer data while detached and verifies
rehydration, restored item observation and retained expansion. These additional
checks pass locally on x64 in both themes with Windows animations disabled,
retained under `artifacts/smoke-breadcrumb-focused`. The reloaded and pressed
captures were inspected. Run `38414003860` at `7c4c272` additionally passes the
full native catalogs and independent consumers on both architectures. ARM64's
report verifies animations enabled; x64 verifies reduced motion. The pressed and
reloaded ARM64 captures were inspected. Full curve/raster motion parity remains
open. Local x64 and ARM64 builds have zero warnings and all 26 unit tests pass.

The full local suite stopped later at Input's rounded-pointer movement check
(`artifacts/smoke-breadcrumb-resume-focus/result.json`), after Breadcrumb's Light
phase passed. The focused result does not clear that separate full-suite failure.

For focused local verification, publish the catalog and run
`./scripts/smoke-catalog.ps1 -Executable <published>/Orchard.Catalog.exe -Component breadcrumb -Output artifacts/smoke-breadcrumb`.
This runs both themes and retains pressed, reloaded and composed captures. The
default `all` command still runs every fixture; a focused pass does not establish
full-suite acceptance.

The default native links use HyperlinkButton Click/Command rather than
NavigateUri so application routes and relative hrefs stay consumer-owned, as
described in Microsoft's [HyperlinkButton guidance](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.hyperlinkbutton?view=windows-app-sdk-2.0).
No browser, networking or application navigation is launched by Orchard.
The repository wiki remote is unavailable; this in-repository document is the
current contract. Orchard has no backend or metrics surface affected by this change.
