# Toggles

Orchard styles the native WinUI `ToggleButton`. Import `Themes/Orchard.xaml` as documented in the package setup, then choose a style:

| Size | Default variant | Outline variant |
| --- | --- | --- |
| Small | `BleeckerSmallToggleStyle` | `BleeckerSmallOutlineToggleStyle` |
| Medium | `BleeckerToggleStyle` | `BleeckerOutlineToggleStyle` |
| Large | `BleeckerLargeToggleStyle` | `BleeckerLargeOutlineToggleStyle` |

```xml
<ToggleButton Content="Follow updates"
              IsChecked="{Binding FollowUpdates, Mode=TwoWay}"
              Style="{StaticResource BleeckerToggleStyle}" />
```

The consumer owns `IsChecked`, `Command`, `CommandParameter`, content and `IsEnabled`. Use `Checked`/`Unchecked` events or a two-way binding to observe selection. This is an independent on/off action; selection groups are a separate contract. Leave `IsThreeState` false for Bleecker's binary contract. The native optional indeterminate state also receives theme resources.

Tab navigation, Space activation, focus visuals and the UI Automation Toggle pattern remain native. Supply `AutomationProperties.Name` for icon-only or non-text content. Long labels can use a wrapping `TextBlock` as content. The styles use minimum heights (32/36/40), not fixed heights, so labels and enlarged text can grow vertically. These are the pinned component dimensions; typography and radius use the canonical Orchard font and radius resources.

Generated resources supply normal, hover, pressed, disabled, checked and indeterminate colors, including combinations. Checked surfaces use a primary tint. Default toggles have no visible border; outline toggles use one logical pixel. High contrast maps to Windows system colors instead of brand colors. Native template motion is retained; reduced-motion behavior still needs session verification.

## Evidence and remaining acceptance

The x64 published catalog's `TogglesFixture.VerifyAsync` toggles on/off through the native automation provider, verifies the consumer's visible result, and checks eight rendered states for both variants in light/dark. All six styles are registered with the catalog coverage gate. The smoke artifact includes `toggles-light.png`, `toggles-dark.png`, and the result report.

In the Windows 11 desktop session, Tab/Shift+Tab showed native focus, Space selected Follow updates, pointer activation cleared it, and Tab moved to Large. Pointer activation also cleared the selected outline toggle. Both theme captures were inspected, including the wrapped Japanese, Portuguese and Arabic label at a constrained width.

Remaining acceptance: Narrator, actual Windows contrast settings, increased text/display scale, reduced motion, command and two-way binding regression coverage, and ARM64 runtime. These styles are implemented; the parity entry remains in progress until all required evidence exists.
