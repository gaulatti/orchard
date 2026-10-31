# Header source contract

Header is [accepted for the five-check x64 milestone](header.md) in preview
`.66`. These fixtures establish the
source contract for a reusable layout component; they do not add application
navigation, services or production routes. Monitoring and Editorial remain
required fictional-data component demonstrations.

`tests/Reference/HeaderComposition.json` adds six source Header/AppShell states:
desktop, narrow closed and narrow open, each in light and dark themes. Two
independent captures match all receipt and PNG bytes. They bind the same pinned
Header dependencies plus the original AppShell source. The captures establish
the topmost Header hit, unchanged 72-pixel body offset and footer flow, and the
original body's edited text and selection across viewport/menu changes.

## Frozen source and evidence

`tests/Reference/Header.json` records Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, file `src/layout/header.tsx`, SHA-256
`e6d0c2969a7f042e13cf3261043c74dc8ae9c7caf9c7225db9271ea3d28bd009`.
It binds the original BrandLockup, NavMenu and IconButton dependencies, source
styles, story, lockfile, installed module versions, four bundled font assets,
capture code, 72 full client PNGs and four interaction histories (24 samples).
Two independent captures match every JSON field and PNG byte. The capture moves
its simulated pointer away before sampling a closed menu, avoiding residual
hover colour in the closing link. Source components are unchanged.

The 36 cases in each theme cover viewport breakpoints, constrained hosts,
full-width mode, three positions, scrolling and the sticky parent boundary,
empty/long/large brands, asymmetric or missing actions, empty navigation,
caller link rendering, a fractional header-height token, RTL, expanded menus,
16-item clipping and caller-owned input. Five portable regression tests bind
the capture and images and assert the measured flow, breakpoint, paint and
interaction contracts. All 293 portable tests pass at this checkpoint.

## User-observable native acceptance criteria

- Compose the existing reusable BrandLockup, NavMenu and IconButton contracts.
  Preserve the caller's original item metadata, render templates, command
  parameters, data bindings and native action controls.
- Default to fixed positioning. Its header paints at viewport top with the
  viewport width but reserves zero height in normal flow. A constrained host
  preserves its horizontal static origin: a 400-DIP host at x=80 with a
  1280-DIP viewport produces a header at x=80, width 1280. Static and sticky
  positions reserve the full row plus the one-pixel bottom rule.
- Sticky position stops at the containing parent's lower edge. It must continue
  to behave correctly inside the normal native scrolling path, with no extra
  caller offset or reconstruction of source geometry.
- Consume the shared header-height token (72 by default); keep its one-pixel
  bottom border separate. The captured fractional token is 96.5. The mobile
  menu starts at the row height and does not increase header flow height.
- At viewport width 1024, switch from the narrow brand/toggle row to the
  three-column desktop grid with centred navigation. The breakpoint uses the
  viewport even when the host is narrow. Preserve container maxima, padding,
  gaps, RTL and caller sizing.
- Keep independent desktop and mobile action slots mounted across ordinary
  state changes. Preserve caller input value and selection across desktop and
  narrow resizing, item reordering and closing a mobile item. The source does
  not restore focus automatically when returning to the narrow viewport.
- Preserve menu-open state across the desktop breakpoint. On desktop, the
  mobile panel is hidden while its open state survives. Below the breakpoint,
  closing it uses max-height/opacity clipping, not removal of its children.
  The closed border box measures one pixel; an expanded panel caps at 384.
  A native keyboard/tab policy must be checked against the actual source,
  rather than inferred from visual hiding.
- Forward a custom link renderer's invocation through the original item and
  close the mobile menu on a mobile navigation invocation. Keep retired or
  disabled actions inert. Preserve native accessibility metadata.
- Match light/dark brushes, the 95% header background, eight-pixel backdrop
  blur, border alphas and both overlay shadows. Verify the 200-ms max-height
  and opacity transition and component reduced-motion behavior separately
  from the reduced-motion source snapshots.
- Register a fictional-data catalog fixture and independent NuGet consumer.
  Compare native layout and paint against every frozen state, then verify
  visible rendering, pointer/keyboard behavior, scaling and ARM64 runtime when
  those execution surfaces are available. A compile or source screenshot does
  not establish native parity.

## Native layout investigation

At the source-preparation checkpoint the public package remained
`0.1.0-footer-expanded-v1-local`; no Header API or new library binary was
claimed by those fixtures. A hidden diagnostic in
`tests/PackageConsumer/HeaderPlacementProbe.cs` investigates the zero-flow
layout boundary before choosing the control's native positioning design.
Its first panel-only capture proved physical 800-by-73 layout with zero desired
height and a following sibling at the same y position. That capture rendered
the panel itself, so it did not establish ancestor clipping, stacking or hit
testing. The expanded diagnostic rejected that design: 12 ancestor paint and
hit-test checks fail across a panel, templated control and translated control,
despite apparently correct measured rectangles. The initial expanded harness
also omitted actual hidden client sizing; those early attempts remain recorded
and are not used as valid raster evidence.

The corrected native approach uses negative margins on the control's owned
template surface to remove its flow contribution while retaining normal
physical layout. A new isolated x64 consumer passes both unshifted and
top-translated scenarios. The complete ancestor image and hit-testing APIs see
both the header and the overflowing menu beyond a 400-DIP parent. The following
body retains its normal position. The same binary reproduces the rejected
zero-flow implementation's 12 failures across three scenarios.

The independent audit checks all 5,760,000 pixels across the five images,
including full rectangles, draw order and unclipped background/body areas.
It verifies original DLL/package identities, actual hidden 1280-by-900 client
size at scale one, 45 visibility/foreground/focus guards, zero activation/focus
events and removal of the fresh consumer cache. Its rejection test rejects
39 damaged receipts/images. No desktop input, showing or activation is used.
The report is
`artifacts/current/header-placement-probe-consumer-v6/header-placement-audit.json`;
the Header preparation checkpoint is
`artifacts/current/header-preparation-checkpoint.json`.

Reproduce the investigation with a fresh isolated consumer of the current
Footer-expanded package, using `scripts/verify-package.ps1 -BuildOnly`, then
run `scripts/verify-hidden-header-placement.ps1` with its app directory, exact
package and consumer output directory. The wrapper runs the candidate and
explicit rejected-design modes and checks their expected exits. The audit is
`scripts/verify-hidden-header-placement.mjs`; its receipt regression is
`scripts/test-hidden-header-placement-audit.mjs`.

This preparation established a layout technique. The [native prototype](header.md)
now implements dynamic positions, caller APIs/slots and a catalog fixture.
Its current geometry, public-contract, package and remaining acceptance evidence
are tracked separately from the historical placement diagnostic.

The implementation targets the repository's pinned App SDK 2.5.1 / WinUI
2.3.9. Relevant primary API contracts are Microsoft's
[ArrangeOverride documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.frameworkelement.arrangeoverride?view=windows-app-sdk-2.0),
[RenderTargetBitmap documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.imaging.rendertargetbitmap.renderasync?view=windows-app-sdk-2.0)
and [visual-tree hit testing](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.visualtreehelper.findelementsinhostcoordinates?view=windows-app-sdk-2.0).
Offscreen diagnostic pixels do not establish a visible presentation, backdrop
composition or complete source-raster parity.

The current desktop reservation has expired. This investigation uses hidden,
unactivated windows with visibility, foreground and focus guards; it sends no
desktop input. Narrator, OS accessibility/contrast/settings changes, pointer
heartbeat and GitHub Actions remain excluded. The public wiki remote is
unavailable, so the source contract is documented here.
