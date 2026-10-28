# Page Header source contract

The native implementation is `BleeckerPageHeader`. This entry remains in
progress; the full visual and platform matrix is not accepted. The frozen browser capture is
`tests/Reference/PageHeaderLayout.json`, SHA-256
`90a3c3f98b9e582cdb39ca5b23c9b15ee4356640a07a797539919b0230ce9f76`.
It renders the actual [pinned PageHeader source](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/page-header.tsx),
SHA-256 `7f15ebbd49fe21db59a95c21be64a69c1bd28a2a92b8216ca01befe4ac9013d0`,
with its Button dependency, React 19.2.4 and Lucide 0.563.0. Dependency versions
and integrity values are checked against the pinned lockfile. Fonts use the
repository's Encode Sans and Libre Franklin files.

Sixteen Light/Dark captures cover title-only, full composition, narrow viewport,
narrow container within a wide viewport, long wrapping text, empty content,
consumer routes and RTL. The callback case executes the real Back action once;
the route case retains the supplied href exactly. Browser evidence alone does
not establish native or package acceptance.

The source supplies a 32px semibold Encode Sans title, 36.8px line height and
-0.02em tracking. Its description uses Libre Franklin 14px/24px, a 10px top
margin and a 672px maximum width. Breadcrumb content has 14px/20px secondary
typography and a 20px gap before the main row. The optional Back action uses
the Small Ghost button contract with a 32px square override, 2px top margin,
18px Lucide chevron and a 16px gap before the text. Actions wrap with 12px gaps.

The 640px responsive breakpoint follows the viewport. At a 600px viewport,
actions sit 28px below the title group. A 360px container in a 900px viewport
still uses the horizontal row: its title/description group shrinks and the
actions wrap to two rows. An adapter must preserve this distinction rather
than infer the breakpoint from its own control width.

The native title uses the browser's resolved 36.796875px line height. Retaining
the unquantized 36.8 value accumulated a 0.0125px discrepancy across four lines;
the implementation follows the frozen rendering rather than widening the gate.

`Title` and `Description` retain the supplied strings. Null/empty strings
collapse. `Breadcrumbs` accepts consumer content with `BreadcrumbsTemplate`;
unconfigured rich text receives 20px line height, while local values and styles
remain authoritative. `Actions` accepts a single content object or an enumerable
of content objects, with `ActionsTemplate` applied per item. Native elements
remain the same instances. Observable collections update while mounted and
refresh on remount; unchanged action elements retain focus. Actions wrap with
12px horizontal and vertical gaps, centering mixed heights within each row.
A supplied empty action collection retains its slot, matching the source's
truthy empty React content; null/empty string removes the slot. Consumer
background/border/padding/corner properties apply through the template's border.
An immediate remove/reinsert can deliver WinUI `Unloaded` after `Loaded`.
The unload callback disconnects observers and queues a guarded check of the
current mount, restoring subscriptions only when that same control is loaded.
The native regression verifies subsequent collection edits after this boundary.

`BackHref` is exact application-owned navigation metadata. It never becomes a
`NavigateUri` or launches a browser. `BackCommand`, `BackCommandParameter` and
`BackRequested` deliver native invocation to the consumer. A href exposes the
Hyperlink UIA role; a command-only action exposes Button. Both use native button
keyboard/CanExecute semantics. The consumer may handle `BackRequested` to route
using the current `BackHref`. `BackLabel` defaults to `Back` and supplies the
accessible name. The title exposes heading level 1, and the group defaults to
the title as its accessible name while respecting explicit name/LabeledBy.
The chevron keeps the source's leftward shape under RTL while its position and
the row order follow the consumer's reading direction.

The viewport breakpoint observes `XamlRoot.Size.Width` and disconnects on
unload. The main row stacks below 640px and remains horizontal for a narrow
container inside a wide window. Its proportional shrink preserves the frozen
360px container case; complete CSS minimum-content constraints still require
additional specimens.

The catalog registers `PageHeadersFixture`, including the sixteen frozen
Light/Dark cases, physical Back/Space activation, CanExecute, route/role checks,
live action/focus retention, unload/remount, content templates, disabled
ancestors and template replacement. Run the focused gate with
`scripts/smoke-catalog.ps1 -Component page-header`. An independent
`tests/PackageConsumer` fixture consumes the packaged API and default template.

Remaining acceptance: exact glyph baselines/widths, CSS `text-wrap: balance` and
`pretty`, the full minimum-content/override matrix, component theme resources,
scaling, automation behavior and complete x64/ARM64 visual evidence. The title-only native width is
119.7433px versus 119.1094px in the browser; passing block heights does not
accept that width difference. Do not treat this component as fully accepted.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).
