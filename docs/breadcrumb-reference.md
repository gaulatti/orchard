# Breadcrumb reference and Windows contract

The fixed Bleecker commit is `a86059013cd59684ce452849278ea81e734f2066`.
`src/components/breadcrumb.tsx` has SHA-256
`46f67eb20c2038b3d9920555b528b39c02b3fa6e8ec271e1f99fcd1b91ee55c0`;
its default link renderer comes from `nav-menu.tsx`, SHA-256
`1a9b3d9c7c7d43c52d23080c37af477211d37ed457f1c6a8eb2a18269b2f8cc5`.

Breadcrumb is accepted in current source for the [five-check x64 milestone](acceptance-contract.md).
The `.29` preview lists it. The matching `.29` package DLL passed the focused Light/Dark
catalog route, including native ellipsis hover, held/cancelled press, Enter
expansion, focus restoration and custom links. With animations disabled, the
pressed/restored endpoints settled and the control started no storyboard.
The independent x64 package consumer compiled and ran; eight native 150%
Light/Dark collapsed/full path cases at 320- and 600-DIP viewports stayed within
their bounds. ARM64 is build-only and runtime-unverified for this milestone.
Older source-derived criteria and reports below retain their historical scope;
exact raster/motion parity and OS contrast are not acceptance gates.

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
  belong to the broader historical parity review. Contrast uses Windows semantic
  colors. The current milestone uses the five x64 checks above; ARM64 runtime,
  exact raster/motion parity and OS contrast campaigns are outside that gate.

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
system color changes repaint its live palette. OS contrast switching is excluded
from current acceptance by the [library scope](library-scope.md).
Default links and the expansion button use the existing branded Windows focus
ring. Keyboard expansion restores focus to the first focusable revealed link,
including a consumer link template's native focusable descendant. Disabled or
non-focusable renderers do not receive focus, and the current-page text is not a
target. Native [focus discovery](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.focusmanager.findfirstfocusableelement?view=windows-app-sdk-2.0)
preserves the consumer's tab order and focusability. The custom renderer fixture
expands with real Enter, verifies the original item/focused Button and continues
with real Tab to the next revealed consumer link. Broader historical focus-visual
review remains open; native 150% bounds now pass under the current milestone.
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

The `46a7ae2` hosted ARM64 run loses foreground after its hover observation and
before pointer down. The input fixture now revalidates foreground immediately
before starting a press, using the existing single hosted recovery guard only
when no button is held and the current point belongs to Orchard. Breadcrumb,
Checkbox, Alert and normal clicks share that entry path. Held-button release,
target ownership and foreground assertions remain unchanged. Fresh native
verification is pending; a rejected pointer action is not a component pass.
The complete local x64 catalog passes both appearances at
`artifacts/smoke-brand-all`, including the held/cancelled Breadcrumb and Alert
presses and Checkbox physical input. This does not substitute for ARM64 runtime.

For focused local verification, publish the catalog and run
`./scripts/smoke-catalog.ps1 -Executable <published>/Orchard.Catalog.exe -Component breadcrumb -Output artifacts/smoke-breadcrumb`.
This runs both themes and retains pressed, reloaded and composed captures. The
default `all` command still runs every fixture; a focused pass does not establish
full-suite acceptance.

The default native links use HyperlinkButton Click/Command rather than
NavigateUri so application routes and relative hrefs stay consumer-owned, as
described in Microsoft's [HyperlinkButton guidance](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.hyperlinkbutton?view=windows-app-sdk-2.0).
No browser, networking or application navigation is launched by Orchard.

At `dc7815c`, x64 run `38491762987` reaches the expanded unavailable custom
path before its ContentPresenters have materialized their ConsumerLink buttons.
The fixture now waits for all four actual loaded template buttons before reading
their availability and keyboard focus. Expansion count, exact consumer item,
CanExecute(false) and restored-focus assertions remain unchanged; no product
behavior or timeout is relaxed. Focused local verification passes both themes at
`artifacts/smoke-breadcrumb-consumer-materialization`.
Fresh hosted run `38494432792` passes the full x64 catalog in both themes with
the readiness change. Native ARM64 passes Light Breadcrumb checks before its
later notification failure; that run does not reach Dark Breadcrumbs.
The repository wiki remote is unavailable; this in-repository document is the
current contract. Orchard has no backend or metrics surface affected by this change.

Run `38506082985` at `c7fdad4` exposes the same materialization boundary in the
enabled custom-link expansion branch on native ARM64: the nine presenter/separator
children exist before the named `ConsumerLink` visual can be found. That branch
now combines actual loaded-template readiness with keyboard-focus restoration
inside its existing two-second deadline, using the existing nonthrowing tree
lookup. The four original consumer hosts, item identity and subsequent physical
Tab assertions remain required. The reusable Breadcrumb implementation and
focus behavior are unchanged; fresh focused and hosted checks are required.

The focused local x64 check passes both themes at
`artifacts/smoke-breadcrumb-enabled-template-focus`, retaining keyboard
expansion, original consumer items and physical Tab traversal. Both architecture
builds have zero warnings; all 32 regressions and token generation pass. The
expanded template-readiness predicate still requires fresh native ARM64 CI.

The cloned Breadcrumb paint now observes Color and Opacity on its currently
resolved SolidColorBrush producers, as well as palette brush replacement.
Replacing a palette brush rebinds those observers; template cleanup and unload
remove them. The existing source color/scale transitions and reduced-motion
behavior are retained.

`VerifyMutablePaletteAsync` first proves a local failure against the old
renderer at `artifacts/smoke-breadcrumb-mutable-before`. While a real pointer
hovers the ellipsis, the same foreground/background brush objects change color
and opacity without replacement. Both the rendered chrome and icon must adopt
each value, then return to the original source colors after restoration. The
corrected focused x64 run passes both themes at
`artifacts/smoke-breadcrumb-mutable-fixed`, alongside physical press cancellation,
keyboard expansion, focus restoration and consumer template checks. The exact
color assertions and two-second readiness deadline are unchanged. Both builds
are clean and all 38 regressions pass at `artifacts/tests-breadcrumb-mutable`.

The hosted contrast check additionally records palette values/opacities,
rendered chrome/icon colors, native CommonStates, and current pointer/window
ownership in `contrast-breadcrumb-hover.json`, on both success and failure.
The same Windows Highlight/HighlightText assertion remains required; the local
producer-mutation proof does not replace the actual mode-changing gate.
The fresh independent x64 consumer passes both themes at
`artifacts/package-consumer-breadcrumb-mutable` for package SHA-256
`becbbfa4b759f068a7c582d0f7521473ba6227ca1fcf1dd0344442ce39bebc22`.
Its public HyperlinkButton Foreground producer changes Color and Opacity on
the same brush; the mounted text must adopt the values while the disabled
consumer link remains unavailable. Foreground and availability are restored.
Restore/publish ran separately from native execution while older task artifacts
were losslessly compressed; the restored package hash was rechecked before
launching that originally fresh consumer. No repository project reference or
parent configuration participates. No backend or metrics surface is affected
by this native brush-observation correction.
The complete local x64 catalog passes both themes at
`artifacts/smoke-breadcrumb-mutable-all`, including color/opacity restoration,
physical Breadcrumb interactions, other mounted controls and the seven-state
Attention glyph regression. Local animations are disabled, display/text scale
is 1 and contrast is off. Actual hosted contrast remains unverified; the full
Breadcrumb acceptance gaps remain open.
