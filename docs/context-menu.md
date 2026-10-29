# Context Menu draft

The pinned `context-menu.tsx` has its own recipe: 160-DIP minimum width,
36-DIP minimum action height, Encode Sans Regular 13px/19.5px text, 10/6px
padding, five-DIP action corners and a 32-DIP inset option. Its focus background
is light sand in Light and white at 7% in Dark. The popup shares the canonical
border, six-DIP padding, seven-DIP corners and overlay shadow.

`BleeckerContextMenu` is a native MenuFlyout assigned to a target's
`ContextFlyout`. Native items retain their original commands, parameters,
Click handlers and accessible names. The context-specific presenter, plain,
inset and composed-content styles are registered in `ContextMenusFixture`.
Tokens and generated styles are owned by `generate-tokens.mjs` and
`context-menu-styles.mjs`; do not edit generated dictionaries.

Acceptance requires native context invocation, original actions and dismissal,
source geometry/paint, owner-aware modal placement and inactive input guards,
source selection cancellation, popup focus and lifecycle handling, runtime
catalog examples and independent x64/ARM64 NuGet consumers. This first draft
does not yet establish all those requirements. The public shared-plane adapter
is now implemented and verified through the actual control in hidden windows.
It retains the original native presenter as the items/commands and placement
owner, with a transparent size proxy in its template. The complete original
styled surface moves into its nearest modal branch. Fourteen public-control
cases verify automatic mounting, native item peer parentage, original commands
and bindings, live text/content/theme/style updates, RTL, viewport changes,
canceled dismissal, owner teardown and remount. A fresh NuGet consumer repeats
all fourteen cases. Final component acceptance remains open.

`IsModal` is a bindable root option and defaults to `true`. Set it to `false`
to keep outside controls available while the menu is open. The menu's own
surface remains hit-testable. Native pass-through handles outside input for an
unowned menu; the shared-plane adapter handles outside input for an owned menu.
A nonmodal menu still belongs to its nearest modal owner; an upper
modal shields that owner's menu without rewriting caller enabled bindings.
Menus without an active modal owner keep their original Windows presentation
in either mode, including targets inside an otherwise idle `BleeckerModalHost`.
Nonmodal mode passes outside pointer input to the root content through WinUI's
`OverlayInputPassThroughElement`; the menu's own surface still handles its
actions. Menus with an active modal owner use the shared-plane adapter.

Live option changes update the backdrop and focus-scope registration while
retaining the original native items, public binding and open/close lifecycle.
The browser source remounts its content on a mode change; that can cause focus
side effects and even dismissal. The native option preserves caller objects
instead of recreating them. Actual native focus transitions remain unverified.
The first physical x64 catalog check of the published v7 library (2026-10-28)
found a foreground failure that hidden `ShowAt` probes cannot cover. A real
right-click on the Context Menu target left no menu item visible; Shift+F10
likewise left no menu item in both modal and nonmodal modes. The exact library
DLL hash, input coordinates, clean process exits, UI Automation trees and
failure screenshots are recorded in
[`context-menu-v7-physical-v1`](evidence/context-menu-v7-physical-v1/checkpoint.json).
`verify-context-menu-desktop.ps1` is the focused repeatable check. The published
component remains unaccepted until native gesture opening, original pointer
selection and keyboard dismissal pass together. A local focus experiment was
reverted because it let the menu open but still failed pointer selection.
The later [native host-background trial](evidence/context-menu-background-native-trial-v1/checkpoint.json)
also failed the complete physical path. Leaving the default menu in its native
presenter allowed Shift+F10 and right-click opening plus Escape dismissal, but a
real click on the visible first item did not call the original catalog handler;
the menu remained open. The one-line placement candidate was reverted. The
original portal behavior and this trial's failure remain distinct evidence.
The [native default repair](evidence/context-menu-native-default-repair-v1/checkpoint.json)
retains that placement only for default modal menus without an active modal
owner and disables native root-bound constraining. On the same x64 library DLL,
Shift+F10, Escape, right-click and a single physical click on the first original
action passed. Root, modal-owner, catalog and root-option hidden checks also
passed, including live modality changes. Nonmodal and active-modal physical
selection still need repair; this does not accept the Context Menu component.
The [nonmodal physical follow-up](evidence/context-menu-nonmodal-physical-v1/checkpoint.json)
adds a fictional outside button to the catalog and a first-click regression
check. The committed portal path still closes before Shift+F10 exposes an item.
An unowned native nonmodal trial made item selection work, but its first outside
click only dismissed the menu instead of activating that button. A light-dismiss
and root-pointer trial also failed. Those library changes were reverted; the
catalog demo and focused test retain the required outside-input contract. The
later [native pass-through repair](evidence/context-menu-pass-through-v1/checkpoint.json)
uses the same native presenter for unowned nonmodal menus and routes outside
input through WinUI's flyout pass-through property. On one x64 DLL, physical
Shift+F10, Escape, right-click, original-item selection and the first outside
button click passed in the catalog. Active-modal-owner physical input remains
open, so this does not accept the full Context Menu component.
The catalog now includes an [active-modal-owner demo and physical regression](evidence/context-menu-owned-physical-v1/checkpoint.json).
In both modes, Shift+F10, Escape and right-click exposed and dismissed the
owned menu, but a physical click on its visible action dismissed the menu
without calling the original handler. A native-presenter trial made the
pointer action work but let Escape close the parent modal; a portal
pass-through trial did not fix selection. Both library trials were reverted.
The owned demo and focused test retain this acceptance boundary while the
unowned physical paths continue to pass.
The [window-hit follow-up](evidence/context-menu-window-hit-v1/checkpoint.json)
records the native top-level window at each original item's center during
the same physical test. Both the failing owned menu and passing unowned menu
report a `Microsoft.UI.Content.PopupWindowSiteBridge` at that point, so the
class alone does not identify the intercepting popup. Collapsing the owned
menu's native marker in a local trial left that window rectangle and the
failed physical action unchanged. A catalog-only transparent window-style
trial also left the original action unreachable. Both trials were reverted.
The owned click and complete focus/modality behavior remain open; the new
diagnostic does not accept this component.
Target removal retires the menu even if normal closing was canceled, including
the unprojected standalone path; normal cancellation still retains an attached
menu.

`ContextMenuOptions.json` freezes 24 browser histories, 56 snapshots and four
PNGs, reproduced byte for byte independently. The public native option fixture
covers 38 cases in both themes across standalone, hosted and modal-owned roots.
Its 12 WinUI hit-stack samples check the active-modal-owner menu subtree: WinUI may report
an internal PopupRoot Canvas outside a null-background popup, so an empty
global hit stack is not the input contract. Nonmodal samples retain the outside
target and exclude the menu plane; modal samples include that plane. These are
background input-surface checks, not physical pointer or focus-routing tests.
The actual catalog switch binds the option and demonstrates both modes.

The background root probe now verifies original command/parameter identity,
native Click → Command → Closed order, and reopening in both themes. A caller
can cancel the next native close through `Closing` and `args.Cancel = true`;
the action still fires, the original menu stays open, and a later inherited
`Hide()` closes it once. Native Click `Handled` does not implement this
cancellation contract. The root probe tests cancellation separately from
ordinary dismissal, without introducing another selection event.

Ten native layouts match the source structural recipe: three ordinary rows,
inset, edge-case content, Unicode and empty menus in both themes. The measured
inset width is 161 DIPs versus Chromium's 160.578125; the one-DIP advance-width
tolerance is explicit. Heights, row positions, padding, corner radii, native
names, disabled state and colors are checked separately. Native surface PNGs
have independent background-paint checks. This does not verify source popup
coordinates, hover/focus paint, full text rasterization, shadow parity or motion.

`tests/Reference/Menu.json` freezes ten Context Menu source states among the
32 menu states. `ContextMenuModal.json` adds eight actual browser histories,
24 snapshots and six PNGs for ordinary selection, prevented selection, modal
ownership and owner unmount in both themes. Independent captures reproduce
the JSON and PNG bytes. The capture validates all 122 CSS-scanned source files
against the pinned commit and records its 1,762 actual bundled inputs.
Open/close animation stays disabled pending the source motion implementation.
The goal remains exclusively the component library and fictional demos.

The source makes an inactive branch inert without changing the enabled state
or opacity of its items. A programmatic DOM click still invokes selection;
native original programmatic invocation must likewise remain caller-owned.
Do not replace commands or rewrite enabled bindings to invent a different
contract. User pointer/focus shielding and direct programmatic invocation are
separate boundaries. Source owner unmount removes the portal even when the
last selection prevented dismissal.

The styled native investigation moves the complete original overlay surface,
including its real ScrollViewer and ItemsPresenter, into the nearest modal's
shared plane. Its temporary outer Grid is the only change to the generated
producer template. Before/after native surface pixels match byte for byte.
Original actions, composed content, commands, parameters, native cancellation,
inherited Hide and reopening survive; an upper modal blocks hit testing and
the inspected focus-ownership plan while caller enabled/opacity state stays
intact. The test explicitly retains programmatic invocation, as the source does.

Owner teardown detaches the shared branch but canceled closing can leave the
native marker open. Explicitly closing that marker removes it, and the original
actions survive owner remount. The public adapter now performs this cleanup
automatically; the earlier investigation performed it explicitly. Those eight
native cases are the earlier SDK-boundary prototype. They use
transient roots, fixed test coordinates and hidden windows with no focus or
activation. The subsequent public-control cases use no manual move or marker
closure. They verify a Menu peer whose original item peers identify that menu
as their parent; inactive ownership removes it from the active automation tree
without rewriting caller enabled state. A transparent native size proxy keeps
native placement authoritative while the original styled surface renders in
the shared plane. Its displayed box uses the native rounded size; content
observers invalidate natural measurement when a clipped label or composed
content changes. Actual native focus/key/pointer routing, nested native menu
extensions, full source modality/focus fidelity, complete source placement/
raster/motion and physical scaling still require acceptance.

Custom presenter styles may derive from `BleeckerContextMenuPresenterStyle`.
An overridden template must retain a Grid named `PART_ContextPortalSlot` and
its direct BleeckerOverlaySurface child named `PART_ContextSurface`. Missing
parts fail explicitly when a menu joins the shared host.

The actual catalog probe invokes all four enabled original actions in each
theme and modality, including both composed-content styles. Each invocation updates the
visible status and dismisses the native menu; both original content elements
remain attached to their caller-owned items. An isolated NuGet consumer repeats
all 38 option cases, the root layouts and command/cancellation checks, plus all
fourteen hosted adapter cases. x64 and ARM64 catalog and consumer builds pass;
ARM64 runtime is unverified. The local root-option preview package
contains all 144 loose theme dictionaries, the native PRI, six bundled fonts
and the current package README. This verifies the adapter's scoped behavior;
physical input/focus, source motion/placement and platform acceptance stay open.

Run `verify-context-menu-root.ps1` against the catalog output or a fresh package
consumer (`-HostName PackageConsumer`); run `verify-context-menu-catalog.ps1`
against the actual catalog output. `verify-context-menu-options.ps1` exercises
the root option in either host; its auditor also rejects the retained actual
standalone teardown counterexample. These wrappers reject an active Orchard
build or a host without their command before launching. The catalog also exits
with code 2 for unknown `--probe-hidden-*` commands. These guards repair an
invalidated build/probe overlap that opened an older regular catalog; that
process was stopped, and the failed run is excluded from successful evidence.
