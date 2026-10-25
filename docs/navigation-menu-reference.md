# Navigation Menu source contract

Navigation Menu is a pending reusable Windows component. It is distinct from
the existing [Nav Menu](nav-menu.md), which displays a row or column of links,
and from [Menubar](menubar.md), which contains command and selection entries.
This reference prepares the native implementation; it does not claim a native
control, catalog fixture, NuGet implementation or final acceptance. The
[component-library scope](library-scope.md) applies, including both required
fictional-data Monitoring and Editorial demos.

The pinned source is `src/components/navigation-menu.tsx` at Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`937a1ff45c8a4058247968189c39a22bc444f1168c06b0b4b64200325e5a6d71`.
`tests/Reference/NavigationMenu.json` records the exact browser, modules, fonts,
capture code and bundle inputs. Eight original source dependencies, including
the imported Nav Menu default-link renderer, hooks, styles and story, are
preserved in `tests/Reference/NavigationMenu/Source` with pinned hashes.

## Original entries and rendering

The caller supplies ordered entries. A direct link has original `href`, `label`
and optional `external`/`description` values; a group has `trigger`, optional
`href` and an array of original links. The source identifies groups by the
presence of `items`. A group's `href` is unused: its trigger is a button that
toggles the dropdown, and clicking it does not navigate to that destination.
Description is rendered only for grouped links and only when truthy.

An optional generic `renderLink` receives the original item, prepared children,
the source class recipe and an optional close callback. Additional caller item
metadata remains on the same object. Direct links receive their label as
children and no close callback. Grouped links receive label/description spans
and a callback that sets the active index to null. The caller can invoke or
ignore that callback; the component does not override a custom renderer that
leaves its group open. A throwing custom callback before the supplied close
also leaves the group open.

The default renderer retains the literal destination string. An external link
sets `target="_blank"` and `rel="noreferrer noopener"`. The fixture prevents the
browser's default navigation after the original React handler, so these raw
attributes and callback effects can be observed without leaving the fixture,
opening another tab or sending network requests. Relative destinations and
escaped queries are preserved; no URL reconstruction or router is introduced.

The parent constructs grouped-link renderer props even while a group is closed.
The capture records the original Layout link and its close callback while the
Components group is open. This eager renderer invocation is distinct from
mounting DOM content. Caller-created input content retains its original DOM
node and local text across a parent rerender with the same link identity.
Native templates must explicitly preserve equivalent ownership and lifetime
semantics rather than cloning caller items or bypassing guarded activation.

## Interaction and presence

The active entry is an internal numeric index. Clicking its group toggles it;
another group's click switches it. Merely hovering another trigger does not
switch groups. Outside `pointerdown` closes the active group; pointer presses
inside the container or its popup remain inside. A direct link clicked while a
group is open leaves that group open, because it receives no close callback.
Default grouped-link activation invokes its close callback. After that subtree
unmounts, the observed browser focus is the document body; it is not restored to
the trigger.

The source adds no Escape, arrow-key, focus-trap or initial-focus behavior.
Escape and Arrow Down leave the group open and the trigger focused. Browser
Tab reaches the first grouped link, and Enter invokes it and closes the group.
These source observations must remain distinguishable from any documented
Windows keyboard/focus adaptation. The container has no explicit role, and
group triggers have `aria-expanded` without `aria-haspopup`.

Live label and description changes render while the group stays open.
Renaming its trigger keeps the numeric index active but replaces its keyed
wrapper. Prepending another group moves Home into the active index, yielding
no open popup. Removing the indexed group likewise yields no popup; the source
does not automatically reset the stored numeric index. Direct link and grouped
link keys use their literal destination; group wrappers use their trigger.

`usePresence` waits 20ms before making a mounted popup visible and retains a
closing subtree for 150ms. The same hidden node remains at the 50ms observation
and is absent by the 180ms observation. CSS defines 220ms surface entry,
320ms exit and 190ms controls; removal truncates the nominal exit. Captures use
reduced motion, shortening CSS transitions to .01ms while retaining those
JavaScript presence timers. This does not verify a full normal-motion timeline.

## Measured layout and visual states

The container uses a horizontal flex row with a 4px gap. The normal fixture is
680×36px; an empty container has zero height. Direct links use Encode Sans
Medium 14px/20px and 12/8px padding. Group triggers use the same family at
13px/19.5px, a 14px chevron, a 4px internal gap and 12/8px padding. Their height
is 35.5px, centered at y=20.25 beside the 36px links in this fixture. The source
`tracking-ui` class produces normal letter spacing in the pinned CSS.

The chevron's individual CSS `rotate` is 0° closed and 180° open. A pressed
trigger translates down by one pixel while its wrapper and popup anchor remain
stationary. The capture records individual rotate/translate/scale properties;
the older combined `transform` property alone would miss these Tailwind states.
Trigger hover and open states use primary text with the sand tint. Direct-link
hover also becomes primary. Default grouped links keep primary titles and
secondary descriptions on hover, with a tinted row background.

The popup is aligned to its wrapper's physical left, 4px below it. Its minimum
width is 256px, padding 6px, border 1px and corners 7px. The normal Components
popup is at `(85.765625,59.75)` and measures 256×198.5px. Each described row has
12/10px padding, a 14px/20px Medium title, a 2px description gap and a
12px/19.5px Regular description, for 61.5px total height. Without descriptions,
rows are 40px and the popup is 134px. The empty popup is 14px. A wrapping long
description produces a 100.5px row and a 237.5px popup.

The light surface is white and the dark surface is deep sea `#182533`.
The default top-item intrinsic widths are
`[61.765625,115.921875,83.09375,121.96875]`; each is also its min-content width.
They overflow the 200px constrained container instead of silently shrinking.
RTL keeps physical-left popup placement, at x=518.3125 in this fixture. The
bottom-right popup extends outside the viewport; the source does not flip or
constrain it. Doubling the root font scales rem-based links, padding and popup
width, but the literal 13px group-trigger font stays 13px. The resulting row is
72px high, triggers 51.5px and the popup 512×395px. These facts must be treated
separately from Windows text/display scaling conventions.

## Evidence and native acceptance criteria

Two independent captures contain 44 light/dark PNG states, 42 retained-instance
histories and two presence histories. Their JSON, source dependencies and PNGs
are byte-identical. The verifier checks pinned inputs, original-item/callback
semantics, geometry, typography, raw PNG CRC/hash/dimensions and 30 limited
surface palette probes. Fifty-seven rejection cases cover changed source,
missing/corrupt frames, wrong callbacks/identity, speculative keyboard behavior,
lost caller input, incorrect placement/scaling and invented native success.

```powershell
node scripts/reference/verify-navigation-menu-source.mjs --reference tests/Reference/NavigationMenu.json --frames tests/Reference/NavigationMenu --output artifacts/navigation-menu-source-audit.json
node scripts/reference/test-navigation-menu-source-audit.mjs --reference tests/Reference/NavigationMenu.json --frames tests/Reference/NavigationMenu --output artifacts/navigation-menu-source-negative.json
```

Before native acceptance, prove that original link objects and unchanged raw
destinations reach default and custom content, guarded native activation and
host-owned routing requests. Preserve optional close behavior, callback order,
custom state, keyed live updates, bindings, template/unload retirement and
inactive-modal input guards. Group destinations must remain unused. Register
fictional catalog fixtures and independently verify source and fresh NuGet
hosts, both architectures, actual geometry/paint, normal/reduced motion,
native keyboard/pointer/focus, metadata, scaling and overrides. The source has
no disabled/error link flag: Windows control disabling and invalid input must
be verified without inventing a source product error workflow.

Reuse the existing original-item/link-template conventions where applicable,
but do not substitute the flat Nav Menu or command Menubar for this contract.
Native implementation and a registered fictional catalog fixture are now in
progress; see [the Windows contract](navigation-menu.md). The 44 frozen source
frames remain source evidence. Native results and gaps are recorded separately.
Narrator, OS-setting changes, the pointer heartbeat and GitHub Actions are
excluded from this work.
