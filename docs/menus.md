# Menu foundation (in progress)

Use `BleeckerDropdownMenu` with native menu items styled using `BleeckerMenuItemStyle`, `BleeckerMenuCheckboxStyle`, `BleeckerMenuRadioStyle`, `BleeckerMenuSubmenuStyle` and `BleeckerMenuSeparatorStyle`. It supplies a 176px root minimum and a 144px submenu minimum, matching the reference's distinct popup sizes. Consumer code owns labels, checked values, radio groups, command handlers and shortcuts. `KeyboardAcceleratorTextOverride` only displays a hint; it does not register an accelerator.

The generated `Menus.g.xaml` dictionary is merged by Orchard's resource entry point. Existing native integrations can apply `BleeckerMenuPresenterStyle` directly to `MenuFlyout.MenuFlyoutPresenterStyle`; that single style gives both levels a 176px minimum. The wrapper supplies the distinct defaults through root and shared presenter styles, without setting a local minimum on the presenter. An explicitly assigned `MenuFlyoutPresenterStyle` owns both levels' appearance and dimensions. Returning to the wrapper's original style restores its distinct defaults. Open/close animation remains disabled until the reference motion is implemented.

This is an unfinished foundation, not accepted DropdownMenu parity. The pinned Bleecker recipe supplies Encode Sans Regular 14px/20px item typography, 8px horizontal and 6px vertical padding, 7px corners, 6px popup padding, a 1px border, the light/dark palette and Lucide selection/chevron paths. Windows' default presenter shadow is disabled. Native invocation, dismissal and selection behavior remain active.

The item styles enable `BleeckerMenuFocus.IsEnabled`, which forwards native keyboard focus to the template's `Focused`/`Unfocused` states. WinUI menu items do not drive these states automatically. The focused row uses the same Bleecker tint as hover, without adding the default Windows outline. Disabling the attached behavior detaches its event handlers and clears that template state.

The focus regression checks actual keyboard focus and removal of the previous row's highlight in both themes (`artifacts/smoke-menu-focus-complete`). Physical arrow-key testing also verifies the light highlight on Settings (`artifacts/menu-keyboard-light.jpg`). Dark focus intentionally retains the reference's subtle sand tint. Focus uses its own layer because native common-state updates otherwise clear the highlight. Broader input, accessibility and template-replacement checks remain open. The growing catalog suite has a two-minute timeout; separator resize checks explicitly finish layout before reading dimensions.

`MenusFixture` is registered in catalog coverage. Published x64 checks open real root and nested popups in both themes, inspect geometry and font selection, exercise checkbox and radio state, invoke a command and verify dismissal. Captures are retained as `menu-light/dark.png` and `submenu-light/dark.png`. Radio-group checks run while the items are loaded; native checkbox activation dismisses the popup asynchronously, so the harness waits before reopening it. Passing these checks does not establish the remaining visual and interaction contracts.

Remaining requirements:

- Reference overlay shadows, 220ms entry, exit animation and 190ms item transitions are not implemented. Catalog open/close animations are disabled rather than using Windows motion.
- Labels, inset rows, rich content/icons, indeterminate selection, empty/error and constrained-content fixtures remain unimplemented.
- Shortcut spacing, text baselines, overflow/scrolling, placement offset and all pointer/keyboard/focus states need rendered comparison.
- Verify radio selection persistence when values change while the popup is closed, theme changes while open, nested dismissal, focus restoration and modal ownership.
- Contrast hover foregrounds, actual contrast settings, Narrator, text/display scaling and ARM64 runtime still require implementation or verification. ARM64 hardware is unavailable on this Intel host.

The rendered Bleecker selection-controls fixture confirms 32px rows, 16px check vectors, 8px radio vectors and a separator reaching the popup's inner border. The radio uses a filled Lucide circle with radius 10 and stroke 2 in a 24-unit viewbox; its visible extent is 22/24 of the 8px icon box. Indicator alignment uses the reference 14px slot, including the check icon's 1px overhang.
