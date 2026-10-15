# Sidebar source contract and acceptance

The frozen Sidebar source at Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066` has SHA-256
`2c06422943eedda2232a04c087b8f8562f614829f2b77229ecc7941488894856`.
Its public contracts are SidebarItem, SidebarProps and Sidebar. The reference
uses React 19.2.4, Lucide React 0.563.0 and Radix Collapsible 1.1.14. Browser
captures must verify the actual bundled dependencies against the frozen lock.

The expanded surface is 256 units wide with 12-unit horizontal and 20-unit
vertical padding. Collapsed width is 64 with 8-unit horizontal padding. Header
and footer are consumer content; navigation fills the remaining height and
scrolls independently. Preserve borders, fonts, row spacing, 40-unit minimum
rows, 20-unit icon slots, badges, active rails, disabled-link paint, chevrons,
truncation, width/padding motion and the source's actual expansion behavior.
Measure these values in the rendered source before implementing them.

A group initializes expansion from immediate active children once. Its local
state follows the top-level item id across ordinary updates/reordering, without
automatically following later active changes. Collapse/expand and changing
between a leaf and a group must retain the mounted group's state as the source
does. Removing and readding an item establishes a new initial state. Render
only immediate child links with a truthy href; do not invent recursive rows.
Collapsed mode renders only top-level items with a truthy href, including
groups that also have links. Other top-level entries disappear. Group triggers
toggle expansion without dispatching the item-click callback.

Link activation passes the original item, including the exact href, to the
consumer callback. The custom renderLink path receives that same item,
prepared content, styling and activation callback. Preserve original records,
collections, icons, badge/header/footer content and template identities. Native
route handoff must follow the repository's existing opaque-href/consumer-owned
invocation convention. Do not reconstruct or launch routes in the library.

The source's disabled semantics need explicit browser evidence: link disabled
styling blocks pointer hit testing but does not set HTML disabled or remove the
anchor's keyboard focus. A non-leaf group's disabled field is not applied to
its trigger. Do not silently replace either with a different state model.
Document deliberate Windows keyboard/UIA adaptations, including accessible
names for collapsed links and expand/collapse state, separately from measured
source behavior. Consumer IsEnabled/command availability remains a native
control boundary.

Acceptance must cover expanded/collapsed/empty states, leaf/group/active and
disabled combinations, arbitrary slots, long/multilingual text, dimensions,
scrolling, real pointer and keyboard actions, callback identity, custom links,
retained expansion, live item/collection replacement, reorder/remove/readd,
theme/font changes, detach/rapid reparent and template replacement. Add native
catalog/navigation/coverage, independent package-consumer and boundary
regressions; verify both builds and the actual published runtime. Preserve all
source assertions and the current exact input, presentation and full-run
deadlines. No component is accepted without complete visual, motion, override,
RTL, contrast/Narrator/scaling, performance and native ARM64 evidence.

## Captured source evidence

`tests/Reference/SidebarLayout.json` retains 82 browser cases: 41 fixtures in
each of Light and Dark. Edge 155.0.4283.45 rendered the unchanged frozen source
with Encode Sans 400/500/600, React 19.2.4, React DOM 19.2.4, Lucide React
0.563.0 and Radix Collapsible 1.1.14. The compiler was esbuild 0.25.12 and the
browser driver was Playwright 1.62.1. The actual bundle's packages, versions
and integrity values were checked against the frozen Bleecker lockfile.
The exact installed Radix distribution was inspected; current online Radix
documentation targets a newer release and does not define this reference.

The fixtures cover surface constraints and fractional height, slots and empty
content, long/multilingual text, 35 scrolling destinations, custom renderers,
active/disabled combinations, collapsed routing, nonrecursive child filtering,
keyed reorder, same-id replacement, removal/readdition and transitions between
leaf and group shapes. Real Tab/Enter/Space and pointer actions retain trusted
DOM receipts. Item callbacks retain original record identity; custom links
also verify original icon/badge nodes and prepared label content. Header and
footer buttons receive their normal browser input. This is source evidence,
not a Windows runtime or accessibility result.

The disabled link remains in Tab order and invokes its original callback from
Enter. Its pointer click reaches the underlying navigation surface and invokes
no second callback. Disabled group triggers respond to pointer, Enter and
Space without invoking the item callback. Active-child mutation does not reset
expansion. Keyed reorder and leaf/group replacement retain it; removal and
readdition initialize it again. A grandchild's active flag does not initialize
the parent group. These behaviors are deliberate regression boundaries.

The default 256-by-480 source has a 231-unit navigation width, from x=12 to
x=243; the final unit is the surface's right border. With the fixture's
24-unit header/footer content, their outer heights are 45 and 41. Navigation
runs from y=85 to y=419. Child links begin at x=49 and have width 194: the
source uses 20 units of left margin, a one-unit border and 16 units of padding.
Top-level entries have four-unit spacing; immediate child links have none.
The 35-row fixture has scroll height 1536 while slots stay fixed. At 140 units
of surface height the navigation viewport reaches zero and the footer
overflows the surface; do not silently substitute a different minimum-height
policy. Collapsed width 64 leaves 47 navigation units after padding/border.

JavaScript content truthiness is visible. Numeric zero in header/footer renders
anonymous 24-unit text without slot chrome. Boolean true renders empty slot
chrome (21-unit header, 17-unit footer). False and empty strings render neither.
Numeric-zero icon and badge content appears as anonymous text. Preserve or
explicitly document the native mapping instead of treating every non-null
object as an ordinary slot.

Normal-motion cases measure the 200 ms width/padding transition with
`cubic-bezier(0.4, 0, 0.2, 1)`, 190 ms row transitions and 220 ms chevron
transitions with the premium curve. Actual animation frames retain width,
padding and Web Animations timings. Tailwind 4 uses individual `translate`
and `rotate` properties: a held link is translated one unit vertically and an
open chevron has `rotate: 90deg`, while `transform` itself can remain `none`.
The source's explicit row transition list names `transform`; do not infer an
animated individual translation solely from the 190 ms duration. Reduced-motion
cases retain the stylesheet's computed 0.00001-second transitions.

Source screenshots live in `artifacts/sidebar-browser`. The twelve
`*-initial.png` files correspond exactly to captured `normal` states;
`screenshots-report.json` verifies that match and hashes each PNG. Unsuffixed
screenshots record the end of their fixture's interaction sequence. Both
initial default theme screenshots were visually inspected, including the
initially open group. A closed group in an end-state screenshot must not be
used as the initial-state baseline.

The raw final capture is `artifacts/sidebar-browser/reference-v4.json`, SHA-256
`3ff6f391e1774a63921c33af90910f8a02d8b27967abeac60bdcc6ea94893cb3`.
The checked-in reference has SHA-256
`5acce68a119ac0f23f436b2943a415890e8fafdacb28ed0725c3751c7fba6210`.
Its `styleIndex` fields reference a table of 364 exact computed-style objects.
The derivation reconstructs and compares the entire raw capture before writing;
style interning loses no captured field. Document-level input targets retain
their target kind without copying inline script text into an event label.

`SidebarReferenceTests` protects these source boundaries. All 63 local tests
pass, including its six checks. The derivation additionally rejects nine
corruptions (missing case, wrong source/runtime/font, untrusted input, changed
record, cloned content, absent motion and style-table collision), and verifies
that every rejection leaves the valid reference unchanged. This does not
replace forthcoming native control tests.

## Reproduce the source capture

Run from the repository root with the frozen Bleecker source checkout at
`artifacts/line-chart-browser/frozen-source`, its existing compiled stylesheet
at `artifacts/line-chart-browser/styles.css`, and the original frozen lock at
`artifacts/chart-theme-reference/bleecker-package-lock.json`. The source and
actual bundled package identities are validated. Use esbuild 0.25.12,
Playwright 1.62.1 and the recorded Edge version for this baseline. Replace the
two quoted module placeholders below with their absolute installed package
directories; those build tools remain outside the producer's frozen runtime.

```powershell
node scripts/reference/prepare-sidebar-runtime.cjs
npm ci --prefix artifacts/sidebar-browser --ignore-scripts --no-audit --no-fund
node scripts/reference/capture-sidebar.cjs --esbuild-module '<absolute esbuild package directory>' --playwright-module '<absolute playwright package directory>'
node scripts/reference/capture-sidebar.cjs --screenshots-only --esbuild-module '<absolute esbuild package directory>' --playwright-module '<absolute playwright package directory>'
node scripts/derive-sidebar-reference.mjs artifacts/sidebar-browser/reference-v4.json
node scripts/reference/verify-sidebar-derivation.mjs artifacts/sidebar-browser/reference-v4.json
dotnet test tests/Orchard.Tests/Orchard.Tests.csproj -c Release
```

The runtime preparation selects the exact 20-package dependency/peer closure
from the frozen lock rather than resolving current versions. Captures resume
completed fixture/theme pairs; use a fresh capture file after changing capture
code, source, fonts, CSS or tooling. `--probe` runs seven Light fixtures only
and cannot be published by the full-capture derivation. Input/presentation
budgets and all existing native assertions remain unchanged.

## Native acceptance remains pending

No public native Sidebar API, catalog fixture, independent package consumer or
native runtime result is supplied by this source-contract change. Inventory
remains 48 in progress, 64 pending and zero accepted out of the fixed 112.
Implement the complete consumer/action/state path next, then verify native
geometry, glyphs, motion/reversal, lifecycle, override/template and data
updates, scrolling, RTL, commands, contrast, Narrator, scaling, performance,
x64 and native ARM64. Native accessible names for collapsed destinations and
ExpandCollapse UI Automation must be documented as Windows adaptations. The
existing catalog navigation continues using the current button/input previews.

The public wiki remains unavailable; documentation is retained in the
repository. This native presentation surface has no backend or Prometheus
concern. Library/catalog binaries and their previous runtime evidence are
unchanged. Push-triggered Actions remain paused at the user's request.
