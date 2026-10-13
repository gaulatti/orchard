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
Default links and the expansion button use the existing branded Windows focus
ring. Keyboard expansion restores focus to the first revealed default link; this
keeps Windows keyboard navigation within the newly exposed path. Complete focus
visual comparison and custom-template focus restoration remain open checks.

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

The default native links use HyperlinkButton Click/Command rather than
NavigateUri so application routes and relative hrefs stay consumer-owned, as
described in Microsoft's [HyperlinkButton guidance](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.hyperlinkbutton?view=windows-app-sdk-2.0).
No browser, networking or application navigation is launched by Orchard.
The repository wiki remote is unavailable; this in-repository document is the
current contract. Orchard has no backend or metrics surface affected by this change.
