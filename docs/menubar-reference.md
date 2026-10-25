# Menubar source contract

Menubar has an [in-progress native preview](menubar.md). This page records the
pinned Bleecker behavior captured before native implementation; source evidence
does not establish native or package acceptance. The
[component-library scope](library-scope.md) applies. Monitoring and Editorial
remain required fictional-data component demos.

The source is `src/components/menubar.tsx` at Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`372c40d54f5cf711426d2042b3a1f00d71221f157ab16643d8f42f8883cc2122`.
`tests/Reference/Menubar.json` binds the browser, React/Lucide versions, original
source dependencies, actual bundled inputs, Encode Sans fonts and capture code.
`tests/Reference/Menubar/Source` preserves all six pinned dependencies.

## Caller ownership and interaction

The caller supplies an ordered array of menus, each with a trigger string and
an array of action, checkbox, radio-group, separator or recursive submenu items.
Actions carry their original callback and an optional React icon. Checkbox
values and radio-group values are controlled by the caller. Clicking requests
the opposite checked value or the selected radio value, invokes the supplied
callback once, then closes the root. A caller can accept or refuse the request;
reopening the same React instance displays the caller's resulting value.
Radio entries live inside a group and have individual disabled flags. Optional
shortcut strings are display hints; the component registers no accelerators.

Top-level clicks toggle their own root or switch to another root. Merely
hovering a different trigger does not switch roots. Outside `pointerdown`
closes the active menu; pointer events inside the bar or its nested surfaces
do not. Clicking a normal action also closes the root. Disabled actions and
radio entries do not invoke their callbacks. A throwing action callback leaves
the root open because its later close call is never reached.

Submenus open on pointer enter and close on pointer leave. Their surface can
reopen the same submenu on pointer enter during the retained exit interval.
The source also opens a *disabled* submenu on hover: the disabled button still
has a pointer-enter handler. This observation is preserved in the reference;
no Windows policy or native parity claim is inferred from it. Clicking the
submenu trigger through its DOM click method alone does not open it.

The source has no Escape or arrow-key handlers, focus trap, initial item focus,
or trigger-focus restoration. Captured Escape and Arrow Down leave File open
and focused. Browser Tab reaches the first enabled action, and Enter invokes
it. Once an invoked item unmounts, focus is the document body. These are source
observations. The eventual Windows control must explicitly document and verify
any adaptation to native keyboard/focus conventions.

The active root is a numeric index. Live caller selection and item-label
changes render while open. Replacing a trigger string leaves the index active;
removing File can make Edit occupy that active index, and prepending a menu
makes the prepended menu active. Removing all menus produces an empty bar.
The original React instance remains mounted throughout these histories.

## Geometry, themes and presence

At a 16px root font, the bar has 4px padding, a 1px border, 2px trigger gaps and
7px corners. Triggers use Encode Sans Medium 14px/20px and 12/6px padding,
making a single line 32px high and the populated bar 42px high. An empty bar is
10px high. Its light surface is white; its dark surface is deep sea
`#182533`. The active trigger is sea `#2C5784` in light mode and accent blue
`#5BA3F5` in dark mode, with white text.

The root surface starts 4px below its trigger, with a 224px minimum width,
6px padding, a 1px border and 7px corners. Its inner item container adds 4px
vertical padding, so an empty root is 22px high. Rows use Encode Sans Regular
14px/20px and 12/6px padding. Single-line rows are 32px high; wrapping remains
significant. The captured checkbox with a shortcut wraps to 52px, Unicode
raises the root to 336px, and the long action label raises it to 376px.
The basic root is 224×316px. Disabled opacity is .5.

Subsurfaces use a 192px minimum and a 4px horizontal gap from their wrapper.
The captured submenu and deeper submenu are 192×126px and 192×54px. Source
subsurfaces have no explicit `role=menu`; the root does. Root position uses
physical left alignment even in RTL. The source does not constrain the menu
to its bar width or viewport: the narrow fixture retains a 224px root, and the
bottom-right fixture overflows below the viewport. Doubling the root font to
32px produces an 82px bar and a 448×628px basic root.

`usePresence` delays visibility by 20ms and removes a closing subtree after
150ms. The CSS tokens specify 220ms entry, 150ms exit and 190ms control
transitions, matching the exit removal timer. The reduced-motion
stylesheet shortens CSS transitions to .01ms; it does not remove the JavaScript
presence delay. Browser observations retain the original hidden menu node at
50ms and remove it by the 180ms observation. These samples verify presence,
not a complete normal-motion timeline or native animation.

The compiled CSS retained by the same reference hash uses individual `scale`
and `translate` properties for these utilities. Its transition property list
is `opacity,transform`, which leaves those individual geometry properties
without interpolation. The Windows surface switches their endpoint states
while fading opacity. The earlier 320ms exit note confused the generic duration
token with the actual `--motion-exit: 150ms` token; the latter governs Menubar.

## Verification and remaining work

`tests/Reference/MenubarRowMotion.json` records 48 additional headless color
transition histories and 288 computed-style samples from the unchanged pinned
CSS and JavaScript bundle. It covers trigger/item hover and leave, trigger
open/close, all four item kinds, both themes and reduced motion. Normal color
transitions use 190ms and the premium cubic curve; reduced motion uses .01ms.
Hover backgrounds preserve Oklab interpolation while ordinary trigger text and
selected colors use legacy RGB interpolation. The capture records its installed
Edge 156.0.4314.42 separately from the original static capture's Edge
155.0.4283.62. It does not claim the original browser version or native success.

Reproduce with `scripts/reference/capture-menubar-row-motion.cjs`, supplying the
verified source artifact, installed Playwright module and a fresh output file.
The native source, independent NuGet and unpacked catalog hosts match 264
controlled presentations / 528 rendered color properties across these
histories. Native reduced-motion checks exclude the zero-time sample. Earlier
startup failures and the resource-only comparisons remain retained under
`artifacts/current/menubar-row-motion-*`; the adopted palette is collapsed,
matching the established Pagination pattern. Normal real-time, nested and
physical-input acceptance remain open.

Two independent headless Edge captures cover 32 light/dark PNG states, 50
retained-instance interaction histories and two presence histories. The auditor
checks exact frozen source hashes, state/history contracts, PNG CRC/hash/size,
and 52 direct palette probes at unobscured pixels. It does not equate those
probes with full raster parity. Forty-six negative cases reject changed source,
missing/corrupt evidence, caller-ownership and keyboard/focus mistakes, premature
presence removal, invented native success, blank active frames and wrong themes.

```powershell
node scripts/reference/verify-menubar-source.mjs --reference tests/Reference/Menubar.json --frames tests/Reference/Menubar --output artifacts/menubar-source-audit.json
node scripts/reference/test-menubar-source-audit.mjs --reference tests/Reference/Menubar.json --frames tests/Reference/Menubar --output artifacts/menubar-source-negative.json
```

Recapture with `scripts/reference/capture-menubar.cjs`, passing the established
`--source-root`, `--tooling-root`, `--playwright-module` and a fresh `--output`.
Pass the second directory to the verifier's `--recapture` option to compare
all JSON, pinned dependencies and PNG bytes.

The native preview supplies caller-owned menu/item contracts, themed rendering
and registered fictional catalog fixtures. Its scoped lifecycle and fresh NuGet
results are recorded in the native preview documentation. Full normal motion, input,
focus, metadata, template ownership, scaling, constrained placement and ARM64
runtime acceptance remain open. No foreground input, Narrator, system setting
change, pointer heartbeat or GitHub Actions run is part of this preparation.
