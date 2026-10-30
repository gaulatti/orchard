# Toggles

For the current [x64 acceptance milestone](acceptance-contract.md), Toggle
passes all five checks: Light/Dark state paint for default and outline variants,
physical pointer/Space/focus in both themes, all six styles at native 150%,
stable selected paint under reduced motion, and independent `.38` package
consumer compilation with 12 standalone binding/action cases. ARM64 runtime
remains unverified. The historical full-parity notes below do not add milestone
gates.

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

The reference recipes use 10% light / 15% dark surface tints for hover and selection, with no additional pressed tint. Unselected outline borders use 30% light / 40% dark sand. Runtime checks assert those exact values for all eight states. Grouped items use an 8px radius; standalone toggles retain the 7px button token. Branded focus and motion, and disabled whole-control opacity compositing, remain part of the open visual audit.

The x64 published catalog's `TogglesFixture.VerifyAsync` toggles on/off through the native automation provider, verifies the consumer's visible result, and checks eight rendered states for both variants in light/dark. All six styles are registered with the catalog coverage gate. The smoke artifact includes `toggles-light.png`, `toggles-dark.png`, and the result report.

The shared hidden `ToggleGroupFixture` also exercises standalone native toggles
with all six styles in both themes. These 12 cases verify two-way selection in
both directions, exact selection-event counts, `CanExecuteChanged`, preservation
of caller-disabled state, live command/parameter replacement, old-command
detachment and retention of all four caller bindings. The existing background
runner and window guards are reused; these checks do not send hardware input
or change OS settings.

The [native Toggle automation method](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.peers.togglebuttonautomationpeer.toggle?view=windows-app-sdk-2.0)
cycles selection state. The current pinned-runtime tests additionally record
command execution through that native provider. The auditor requires exactly
two executions with the original command and two with its replacement in each
case; caller parameter identity is checked inside each execution. Calling the
consumer's command directly would not establish this control behavior.

The current catalog and independent x64 NuGet consumer each pass all 12
standalone cases, with two original-command and two replacement-command
executions per case. The surrounding Toggle Group regression passes 62 catalog
checks and 60 package checks; its 70 group observations are unchanged. Both
auditors reject 41 damaged reports, including missing command executions and
lost bindings, and retain the earlier actual retired-selection failure.
Evidence is under `artifacts/current/toggle-binding-catalog-native-v1` and
`toggle-binding-package-native-v1`. The shared fixture builds in independent
x64 and ARM64 consumers. Library implementation and delivery artifacts are
unchanged; native ARM64 execution is unverified.

In the Windows 11 desktop session, Tab/Shift+Tab showed native focus, Space selected Follow updates, pointer activation cleared it, and Tab moved to Large. Pointer activation also cleared the selected outline toggle. Both theme captures were inspected, including the wrapped Japanese, Portuguese and Arabic label at a constrained width.

The shared hidden XAML-island scale fixture now mounts all six selected native
Toggle styles at 100%, 125%, 150% and 200% rasterization scale in both themes.
All 48 style observations retain their 108-DIP row width, size-specific minimum
height, caller accessibility name, checked state and Tab eligibility, with
visible native paint and no selection events, focus or window activation. The
source catalog and the exact independent `0.1.0-menu-label-unicode-v1-local`
consumer each pass eight native frames; all 16 source/package PNG pairs from
the shared Separator/Switch/Toggle fixture match byte for byte. Both x64 and
ARM64 catalog and consumer builds pass, and each native audit rejects 18
corrupted reports. Source and consumer audits are retained at
`artifacts/current/toggle-isolated-scale-source-audit-v1.json` and
`artifacts/current/toggle-isolated-scale-package-audit-v1.json`; their 32 PNGs
and native reports are also in the 464-KB
`artifacts/current/toggle-isolated-scale-evidence-v1.tar`, which survives the
generated-directory cleanup. The compact result is indexed by
`artifacts/current/toggle-isolated-scale-checkpoint-v1.json`. This tests the registered
styles at an owned XAML-root scale; it does not change monitor DPI or Windows
text scaling. The first fixture pass used overlong string labels in fixed-width
cells and visibly clipped two examples; the current pass uses labels that fit
the source-sized examples. Consumers should use the documented wrapping
TextBlock for long labels.

The earlier full-parity review tracked monitor-DPI and increased text-scale
behavior, broader visual review and ARM64 runtime. These are not additional
x64 milestone gates. Narrator and OS contrast campaigns are excluded by the
[component-library scope](library-scope.md).
