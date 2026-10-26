# Shared native Select surface

The native Select implementation previously nested inside Pagination now
lives in `BleeckerSelectSurface.cs` and `BleeckerSelectSurface.Scroll.cs`.
It remains internal. Its explicit callbacks read the original consumer's
updating state, source identity, display text and checked paint. The shared
surface owns native input, focus, popup/scroll layout and accessibility, and
has no dependency on a table feature module or its rows/state.

When declared within `BleeckerModalHost`, all four recipes use its shared portal
plane. Their original popup border, options and scroll chrome move as one
surface. A private transparent canvas catches the first outside press, and the
newest active Select scope traps focus without disabling caller controls.
Modal-owned menus become inert when another modal is active. Deferred option
commits are rejected after that ownership change. The same logical popup scope
keeps a nested Select inside its parent Popover across either native or shared
portals. Standalone Select keeps its native Popup path.

The independent package consumer's broad smoke still searched for a native
`Popup` after opening the page-size and Select-filter menus inside its
`BleeckerModalHost`. That stale assertion failed at Pagination even though the
hosted package control worked. Both smoke checks now locate the original
`PopupBorder` and option buttons in the shared plane and reject a chooser
escaping to a native popup. A fresh isolated package consumer compiles, and
its hidden shared-plane fixture passes all eight public-API cases with 16
software option invokes at `artifacts/current/pagination-package-host-updated-v1`.
The broad keyboard smoke has not been rerun under the current background-only
desktop restriction; its original failure is retained at
`artifacts/current/input-geometry-package-v1/package-smoke-failure-v1.json`.

The native shared-plane fixture verifies 32 frozen open layouts (2,400 geometry
values), 40 ownership/live-producer/lifecycle cases across all four recipes, and
two actual nested catalog flows. Its independent gate rejects 66 damaged
reports, including data from the actual failed empty presenter. The fresh NuGet
consumer passes eight public-API recipe cases with 16 software native option
invokes; its gate rejects 30 damaged reports. The original 292 closed layouts,
58 ownership checks and eight request histories also pass unchanged.

Background checks use only the shared plane and never open a native Popup input
site or call a focus API. Physical input, focus routing, initial focused-option
paint and full open-state visual acceptance remain pending.

Pagination retains its original 80px/40px geometry, option objects/tags,
`Rows per page` name, `OrchardPageSizeChooser` automation class and selection
callback boundary. Existing resource and palette keys are deliberately kept.
The branded shapes, fonts, motion recipes and two-second presentation/input
gates are unchanged by extracting the code.

The trigger now captures its actual native mouse pointer for the duration of
the original press/release. A Filters prototype exposed a lost genuine
release on reopening at the same point after Escape; capture corrects that
route without moving the pointer, synthesizing XAML events or relaxing input
receipts. Disabled rows reject input and are skipped by typeahead/initial
focus. A top-flipped popup uses the same 4px gap with a negative vertical
offset. Width follows the actual trigger allocation when opened/resized.

These APIs were checked in the official WinUI 2.3.9 reference XML shipped with
the pinned Windows App SDK 2.5.1. Extraction-only x64 build and the existing
136-state/120-pair native replay pass; complete verification of the final
shared implementation is recorded at the subsequent artifact checkpoint.
The source/native key-release differences and full component acceptance
remain open.

The native Filters prototype is still undergoing its own source/runtime
verification and is not part of this shared-code release. Its known
modal-input observation and missing-column focus differences must not be
treated as accepted parity. See [the Filters contract](data-table-filters-reference.md).

There is no backend or metrics surface. The public wiki remains unavailable;
documentation stays in this repository. Push-triggered Actions stay paused.

## Verified artifact checkpoint

Clean source `f2ea231d0bce8b71e2dfb3bac5c9a36947d769af` builds x64 and ARM64
with zero warnings/errors and passes all 93 release regressions and the token
check. Its published x64 catalog's 532 files match the fresh build byte for
byte. NuGet 0.1.0 contains the same library assembly as the measured catalog.

- Package SHA-256:
  `8f23fb847857e3836eff02ae66758d161a72079a10d839dbcba571346f4de21c`.
- Library SHA-256:
  `06ce66c9b67b0a398b020f1fdfeefca18f442d3e95b1a92baaa06cd80335e366`.

The actual published catalog passes the original 136-state held-key mode,
136-state rapid-pair mode, 36-state scroll/typeahead mode, 132 producer states,
12 physical clicks and two exact Escape transfers. Both keyboard modes keep
their original event-owner/order and current-point gates. All 34 popup widths,
heights and gaps match. Absolute layout and raster parity remain false:
all 132 surface comparisons differ, with the same 502,848 differing pixels and
154 layout differences measured before this refactor. Raw key-release route
differences also remain explicit and unaccepted.

The same published catalog passes the full sequence in **354.7255073s** under
the unchanged **360s** deadline. All **1,032** preceding capture names remain
present, with no additions/removals. All **174** protected Sunburst/Radial Bar
glyph hashes remain unchanged. The full audit retains 1,732 pointer
gates/moves, 1,738 keys, 62 clicks, 28 tooltip gates, 16 final presentation
gates and 118 Toolbar source cases, with no failed input/presentation gates.
Earlier unexplained Pie/Donut focus failure evidence remains retained; this
pass does not establish a causal fix for it.

Fresh isolated NuGet consumers pass x64 runtime in both themes and ARM64
compilation. ARM64 runtime is unverified. Both temporary consumer caches are
removed; the validation worktree is archived after preserving the published
artifacts. The native Filters prototype remains local and is excluded from
this package/catalog. It has 94 passing unit regressions but a failed native
suite: 274 captured states, two unverified modal outside inputs, and four
literal missing-column focus mismatches. Those failures remain failures.

Evidence/artifacts:

- `artifacts/catalog-shared-select-validated` and
  `artifacts/catalog-shared-select-validated-arm64` (ARM64 build only).
- `artifacts/packages-shared-select-validated/Gaulatti.Orchard.0.1.0.nupkg`.
- `artifacts/shared-select-catalog-archives.json` lists versioned catalog ZIPs.
- `artifacts/shared-select-published-correspondence.json` records source/bytes.
- `artifacts/shared-select-held-audit.json`,
  `artifacts/shared-select-paired-audit.json`,
  `artifacts/shared-select-scroll-audit.json`,
  `artifacts/shared-select-pointer-input-audit.json`,
  `artifacts/shared-select-popup-layout-audit.json` and
  `artifacts/shared-select-source-pixels.json` retain focused results/gaps.
- `artifacts/smoke-shared-select-validated-full`,
  `artifacts/shared-select-validated-full-audit.json`,
  `artifacts/shared-select-prior-captures-audit.json`,
  `artifacts/shared-select-validated-full-input-audit.json` and
  `artifacts/shared-select-validated-full-timing.json` retain the full run.

The inventory remains 52 in progress, 60 pending and zero accepted. This
checkpoint does not complete any component or the full goal. Push-triggered
Actions remain paused and no workflow was dispatched.

## Controlled option updates

The Filters live reference adds 140 states for keyed metadata/order changes,
focused removal/disable/value changes and initial long-list selection. The
shared surface preserves the current option key/occurrence and viewport
during controlled updates, and invalidates pending restoration when newer
updates, native input or dismissal supersede it. Retained keys keep their
native row and use the current original option object. Removed or changed
keys reject their old providers. Initial pointer-open positioning runs before
the initial-focus transaction ends, so a selected offscreen row scrolls into
view. The consumer adapter supplies its raw key mapping; the surface remains
independent of Filters and Pagination types.

The original 276 Filters states and shared Pagination source, input,
held/rapid-key and scroll modes remain required regression boundaries. See
[the live-update evidence](data-table-filters-reference.md#live-option-updates)
for the measured browser contract, native checks and remaining acceptance.

The popup now paints through the [shared overlay shadow](overlay-shadow.md)
inside its existing measured border. The rounded surface owns the original
background, outline and content; its Composition shadow adds no input region.
The shadow-only outside-click fixture verifies dismissal without a consumer
write or an invocation behind the modal surface.
