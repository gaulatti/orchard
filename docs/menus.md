# Dropdown Menu

The [current component-library scope](library-scope.md) governs this work.
Narrator and Windows/OS contrast campaigns are excluded. Historical captures
below describe earlier runs; they do not authorize foreground tests or changes
to system settings. Existing native metadata and theme resources remain intact.

Use `BleeckerDropdownMenu` with native menu items styled using `BleeckerMenuItemStyle`, `BleeckerMenuCheckboxStyle`, `BleeckerMenuRadioStyle`, `BleeckerMenuSubmenuStyle` and `BleeckerMenuSeparatorStyle`. It supplies a 176px root minimum and a 144px submenu minimum, matching the reference's distinct popup sizes. Consumer code owns labels, checked values, radio groups, command handlers and shortcuts. `KeyboardAcceleratorTextOverride` only displays a hint; it does not register an accelerator.

The generated `Menus.g.xaml` dictionary is merged by Orchard's resource entry point. Existing native integrations can apply `BleeckerMenuPresenterStyle` directly to `MenuFlyout.MenuFlyoutPresenterStyle`; that single style gives both levels a 176px minimum. The wrapper supplies the distinct defaults through root and shared presenter styles, without setting a local minimum on the presenter. An explicitly assigned `MenuFlyoutPresenterStyle` owns both levels' appearance and dimensions. Returning to the wrapper's original style restores its distinct defaults. Open/close animation remains disabled until the reference motion is implemented.

Dropdown Menu is accepted for the 2026-10-29 x64 milestone. The pinned Bleecker recipe supplies Encode Sans Regular 14px/20px item typography, 8px horizontal and 6px vertical padding, 7px corners, 6px popup padding, a 1px border, the light/dark palette and Lucide selection/chevron paths. Windows' default presenter shadow is disabled. Native invocation, dismissal and selection behavior remain active.

The exact .73 package assembly and extracted catalog passed Light/Dark state fixtures and physical trigger, action, Space, Escape and focus-return checks in both themes. An independent package consumer compiled and ran. Its native 150% island passed wrapped item and label bounds in both themes; open/close animation is disabled. The native flyout can close during pointer-up before a popup-level `PointerReleased` observer fires; the physical action test verifies one callback and dismissal after the exact pointer move. ARM64 runtime remains unverified.

The presenter uses `BleeckerOverlaySurface`, a consumer-content surface with the canonical two-layer overlay shadow: `(0,24,64,-20)` at .28 opacity and `(0,4,14,0)` at .06, both RGB `(26,55,77)`. These values are generated from the pinned tokens. It uses the shared rounded-mask geometry also used by cards, updates on resizing/corner changes, clears composition visuals on unload, and suppresses the shadow in Windows contrast mode. Its default font is Encode Sans Regular, and it is not a keyboard stop.

The [shared renderer](overlay-shadow.md) now maps CSS blur to explicit Gaussian
standard deviation with a padded native effect graph. Root, nested and
standalone preview checks inspect the actual graph's blur, color, opacity,
spread and offsets. Exact shadow pixels are outside this milestone's gates.

The surface background fills the rounded border box beneath its outline. This
matches CSS background clipping and prevents the overlay shadow from tinting a
translucent border differently on its upper and lower edges. Content padding,
border geometry, shadow layers and native input remain unchanged.

`BleeckerDropdownMenu` defaults to `ShouldConstrainToRootBounds=true`, keeping menus within the application viewport like the browser reference. This also lets the custom blur render beyond the menu edge; a separate native popup window can clip it. A desktop light-theme capture is retained at `artifacts/menu-shadow-root-light.jpg`. Consumers that opt back into windowed popup behavior must verify clipping for their host.

The item styles enable `BleeckerMenuFocus.IsEnabled`, which forwards native keyboard focus to the template's `Focused`/`Unfocused` states. WinUI menu items do not drive these states automatically. The focused row uses the same Bleecker tint as hover, without adding the default Windows outline. Disabling the attached behavior detaches its event handlers and clears that template state.

The focus regression checks actual keyboard focus and removal of the previous row's highlight in both themes (`artifacts/smoke-menu-focus-complete`). Physical arrow-key testing also verifies the light highlight on Settings (`artifacts/menu-keyboard-light.jpg`). Dark focus intentionally retains the reference's subtle sand tint. Focus uses its own layer because native common-state updates otherwise clear the highlight. Broader input, accessibility and template-replacement checks remain open. The growing catalog suite has a three-minute process timeout; separator resize checks explicitly finish layout before reading dimensions.

At `6382185`, run `38441309158`, the hosted x64 focus assertion reports the row
Unfocused before the visual-state check. The fixture now acquires its own native
foreground and parks the pointer on its heading before opening the menu. It
requires the focus call to succeed, then awaits actual Keyboard focus and the
authoritative focused element within two seconds. Failure retains the menu's
open state and foreground/focus diagnostics; a fixed delay is not focus evidence.
The highlight/tint requirements are unchanged. This correction remains pending
native verification; no library focus behavior was changed.

The corrected full local x64 suite passes in
`artifacts/smoke-alerts-menu-focus-all`, including both menu focus/tint checks,
open notification appearance transitions and the final Alert preview. Hosted
ARM64 at `6382185` separately fails an existing notification surface theme
assertion; its retained diagnostic now includes requested/actual theme and
actual/expected color to isolate that boundary before changing library behavior.

`MenusFixture` is registered in catalog coverage. Published x64 checks open real root and nested popups in both themes, inspect geometry and font selection, exercise checkbox and radio state, invoke a command and verify dismissal. Captures are retained as `menu-light/dark.png` and `submenu-light/dark.png`. Radio-group checks run while the items are loaded; native checkbox activation dismisses the popup asynchronously, so the harness waits before reopening it. Passing these checks does not establish the remaining visual and interaction contracts.

`BleeckerMenuLabel Text="Visible fields"` supplies a non-actionable section heading: Encode Sans Semibold 10px/15px, 0.09em tracking, uppercase display, secondary foreground and 8/6px padding (27px single-line height). `Inset="True"` adds 24px before the text, producing the reference's 32px left inset without replacing consumer padding or its style. Casing uses Orchard's full Unicode uppercase mapping, follows `Language`, updates live and preserves the original `Text` as the accessible name. The catalog includes `Straße · ﬃ`, which displays as `STRASSE · FFI`; hidden x64 catalog and NuGet-consumer checks also cover Turkish `istanbul` in both themes. Explicit automation names remain consumer-owned. The control uses a native menu-item host with hit testing and keyboard focus disabled. Its custom peer exposes text with no invoke/toggle pattern. This avoids WinUI filtering separator-derived items out of the presenter child tree. Both the catalog and isolated package consumer verify the label through the presenter, and external Windows UI Automation inspection confirms the menu section labels appear as text. The catalog also checks nested-label tree inclusion, live casing and consumer padding. Physical label clicks leave the menu open and execute no action. Final light/dark root and nested captures are retained in `artifacts/smoke-menu-label-tree-nested`. Component accessibility metadata still requires final acceptance. Narrator and OS contrast campaigns are excluded by the current library scope.

Use `BleeckerMenuInsetItemStyle` or `BleeckerMenuInsetSubmenuStyle` for the reference inset variants. They inherit the complete regular item/submenu templates and set padding to `32,6,8,6`, aligning text with selection-row labels while retaining 32px single-line height. Catalog checks cover root actions, submenu triggers and nested actions in light/dark, including normal invocation and dismissal. Consumer styles can derive from these keys and own their padding.

`BleeckerMenuContentItem` accepts a consumer-composed `UIElement` through `Content` (also its XAML content property). This mirrors Bleecker's arbitrary row children without imposing a Windows icon glyph or fixed icon slot. Set inherited `Text` to the action's accessible name, and use native `Click`, `Command` and `CommandParameter` for invocation. The default template supplies Encode Sans Regular 14px and the standard row padding, colors, focus and disabled states. Compose presentation elements such as vectors and text; do not place independent buttons or other interactive controls inside a menu action. Consumer content owns its internal layout, icon dimensions and text line heights. For a regular single-line row, use a 20px text line height. The fixture uses the existing pinned Lucide Edit geometry at 16px with an explicit 8px content gap; these are consumer composition values, not a new universal icon policy.

Shortcut templates follow native visibility states and have no fixed left margin or cross-row minimum width. Empty hints consume zero layout width. Setting `KeyboardAcceleratorTextOverride` remains display-only. The catalog's fictional rich action combines its Lucide icon, composed label and `Ctrl+E` hint, and checks the hint's text and visibility alongside the accessible action name and native invocation/dismissal. Composed content can grow vertically and be replaced while open; the catalog verifies replacement and inherited fonts. The full notification and rich-row reference variants still need acceptance.

Remaining requirements:

The frozen browser reference in `tests/Reference/Menu.json` and
`tests/Reference/Menu/` contains 32 light/dark open states and 12 action
histories from Bleecker commit `a86059013cd59684ce452849278ea81e734f2066`.
Two independent headless captures produced identical JSON and PNG bytes. The
histories reopen the same React instance and verify caller acceptance/refusal
of checkbox and radio changes, submenu invocation, dismissal and trigger focus.
These are source observations; they do not establish native interaction parity.

ContextMenu has its own source recipe: 160px minimum width, 36px action rows,
13px text with 19.5px line height, 10/6px padding and 5px action corners. Its
disabled opacity is .4; DropdownMenu uses 176px minimum root width, 32px rows,
14px/20px text, 8/6px padding, 7px action corners and .5 disabled opacity.
The source ContextMenu surface is white in light mode and deep sea in dark
mode. The [Context Menu native foundation](context-menu.md) now supplies this
distinct recipe. Its public shared modal adapter preserves original native
actions and uses native layout for placement. Its bindable `IsModal` option
retains caller objects and owner shielding in both modes; see its scoped runtime
evidence and remaining physical input/focus requirements.

Run the portable frozen-reference checks with:

```powershell
node scripts/reference/verify-menu-source.mjs --reference tests/Reference/Menu.json --frames tests/Reference/Menu --output artifacts/menu-source-audit.json
node scripts/reference/test-menu-source-audit.mjs --reference tests/Reference/Menu.json --frames tests/Reference/Menu --output artifacts/menu-source-negative-audit.json
```

DropdownMenu shared modal placement remains unresolved. SDK probes investigated native
presenter ownership using software invokes and background isolation guards. Moving an
already-open presenter caused extra lifecycle events and unloaded its rows.
Returning a stable empty native marker and retaining the original presenter
preserved one open event; assigning its original items restored loaded rows,
but native invocation failed to dismiss the menu. The investigation receipts
explicitly state `menuAdapterImplemented=false`. Their `passed` flag describes
probe execution and desktop isolation, not adapter acceptance.

A subsequent primitive retained the original native presenter and moved only
its template's `ItemsPresenter` into the shared plane. The original row remained
loaded and native invocation dismissed the inherited flyout with exactly one
open and one close event. The expanded submenu trial failed the background
isolation guard and exited; that receipt is a failure, not native acceptance.
Submenu runtime checks are deferred until foreground desktop testing is
authorized again. The root-only follow-up covers original command parameter
identity and Click/Command/Closed order, checkbox dismissal and retained state,
reopening, and inherited `FlyoutBase.Hide`. These use a minimal SDK test template;
the Bleecker visual template, source placement, inactive-modal input guards,
focus and submenu behavior still require implementation and verification.
No such adapter has been installed in `BleeckerDropdownMenu`; its existing
native ownership and public `MenuFlyout` contract remain unchanged.

- Exact overlay shadow rasterization, 220ms entry, exit animation and 190ms item transitions remain open. Catalog open/close animations are disabled rather than using Windows motion.
- Complete notification/rich-row compositions, indeterminate selection, empty/error and constrained-content fixtures remain unimplemented. Label wrapping, text scaling and RTL still need full acceptance.
- Shortcut spacing, text baselines, overflow/scrolling, placement offset and all pointer/keyboard/focus states need rendered comparison.
- Verify radio selection persistence when values change while the popup is closed, theme changes while open, nested dismissal, focus restoration and modal ownership.
- Component theme foregrounds, text/display scaling and ARM64 runtime remain unverified. Existing contrast-theme resources stay supported without an OS contrast campaign. ARM64 hardware is unavailable on this Intel host.

The rendered Bleecker selection-controls fixture confirms 32px rows, 16px check vectors, 8px radio vectors and a separator reaching the popup's inner border. The radio uses a filled Lucide circle with radius 10 and stroke 2 in a 24-unit viewbox; its visible extent is 22/24 of the 8px icon box. Indicator alignment uses the reference 14px slot, including the check icon's 1px overhang.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).
