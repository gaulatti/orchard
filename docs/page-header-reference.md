# Page Header source contract

This entry is pending native implementation. The frozen browser capture is
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
the route case retains the supplied href exactly. No native control, package
consumer or accepted inventory entry is implied by this browser reference.

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

Native implementation must preserve consumer title, description, breadcrumbs,
actions and routing metadata; follow the existing Breadcrumb contract for
application-owned navigation; provide native keyboard/focus/command semantics
and heading accessibility; and register catalog and independent package
fixtures. Text balancing, all wrap/layout cases, dynamic content/templates,
themes/contrast, scaling, RTL, lifecycle and x64/ARM64 runtime require evidence
before this entry can be accepted.
