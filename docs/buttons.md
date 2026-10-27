# BleeckerButton

Hidden-window checks exercise native template parts and offscreen rendering.
Foreground catalog checks separately exercise visible pointer and keyboard
behavior. Neither is a complete variant/state or device-input matrix.

`BleeckerButton` wraps a native WinUI `Button` and preserves native commands, pointer/keyboard activation, focus visuals and automation. Import Orchard's resource dictionary before using it.

```xml
<orchard:BleeckerButton Content="Save draft"
    Variant="Primary" Size="Medium"
    Command="{Binding SaveCommand}" CommandParameter="draft"
    IsEnabled="{Binding CanEdit}" IsLoading="{Binding IsSaving}"
    AutomationProperties.Name="Save draft" />
```

`Variant` accepts `Primary`, `Secondary`, `Outline`, `Subtle`, `Ghost`, `Link` and `Destructive`. `Size` accepts `ExtraSmall`, `Small`, `Medium` and `Large`, with minimum heights 28/36/40/48 and pinned horizontal padding 10/14/18/24. Minimum heights permit larger text and wrapping content to grow. Link actions have no minimum height or horizontal padding. `HorizontalAlignment="Stretch"` produces a full-width action.

The consumer owns content, templates, commands and command parameters. `Click` exposes a normal routed-event handler with the Orchard control as sender. `IsLoading` shows a progress ring, exposes `LoadingText` (default `Loading`) through the native action's automation item status, and disables activation. It does not change the consumer's `IsEnabled` value or binding. The application must still enforce its own operation concurrency and cancellation rules.

Clearing `Template` retires the native parts, stops the owned spinner and removes
the library's event subscriptions and palette. Public state can change while
the template is absent; restoring a valid template applies that state to fresh
parts and preserves the caller's bindings. A non-null custom template must
provide `PART_Action` as a native `Button` and `PART_Progress` as a `ProgressRing`.
Missing required parts remain an explicit error. The library does not rewrite
commands or content bindings on caller-owned retired parts.

WinUI's `ContentControl` can synthesize a text-only template for non-UIElement
content after a caller clears `Template`, without raising a second template
property notification. Orchard remembers that explicit clear operation rather
than treating the native text template as a malformed button template. Setting
another template, or clearing the local template value to resume the default
style, resumes normal button-part initialization.

The `Link` variant is a command action with Bleecker's hover-only underline. Its text size is 16px and it has no size-dependent minimum height or padding, matching the source recipe that omits button size classes. Disabled and loading links do not show the hover underline. Application navigation belongs in its command. Use native `HyperlinkButton.NavigateUri` where Windows should own opening a URI.

The default template presents plain string links with the source's 24 DIP line
height, reference baseline and font-run advances. For example, `Reset` measures
42.578125 by 26 DIP including its border. Original content, content bindings and
accessible naming remain on the wrapper; custom UIElements and templates keep
their own presentation. Optional `PART_LinkTextHost` and `PART_LinkText` parts
provide this plain-string branch in the default template. The rendered native
foreground also drives this text and the underline through state/theme changes.

For custom icon content, supply an accessible name. For long text, use a wrapping `TextBlock` as content and constrain the control width. Font and corner radius come from canonical resources. Variant brushes are generated in component-scoped theme dictionaries; changing the variant replaces that dictionary while keeping the native input control. Light, dark and Windows high-contrast resources are provided. The inner button retains the pinned WinUI template and state machine.

`BleeckerButtonGroup` accepts this control alongside native `ButtonBase` controls. The group controls connected corner radii, borders, margins and stretch alignment; commands, loading and content remain on each action. Original local values, style values and appearance/alignment bindings resume after removal or group unload. The default button template fills its allocated wrapper, including the shared height of a wrapping group. See [connected button groups](button-group.md) for layout and restoration checks.

## Verification

`--probe-hidden-button-lifecycle --verify-output <file>` runs the shared native
fixture in both the source catalog and independent package consumer. It covers
seven variants, both themes, and default/custom templates: 28 cases and 448
checks, with 84 offscreen images. It verifies retirement, unchanged retired
presentation/metadata, original caller bindings, loading, commands, remounts,
spinner reset and restoration of the default style after `ClearValue(Template)`.
The first retained baseline mounts and activates successfully, then throws for
missing `PART_Action` during the native text-template synthesis after clearing.
The repaired source, isolated package consumer and extracted catalog each pass
all 448 checks with retained readiness-render ownership. The audit also rejects
53 altered reports per host. Invoking a retained unloaded native
button still executes its native command on the pinned runtime; the retired part
does not forward Orchard's `Click`. That native behavior is observed rather than
changed by the fixture. These checks do not establish full component acceptance
or physical-input and normal-motion coverage.

The same fixture checks native focus eligibility in each case using
[`FocusManager.FindFirstFocusableElement`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.focusmanager.findfirstfocusableelement?view=windows-app-sdk-2.0):
the ready action must be discoverable, loading and caller-disabled actions must
be skipped, and the action must become discoverable again after loading ends.
These 112 assertions query the native focus search without calling `Focus`,
moving focus or sending input. A foreground check in the exact v5 x64
catalog observed Tab skip the loading action, Shift+Tab return to the loading
switch, and Tab reach the restored action after loading ended. Keyboard Space
then invoked the restored action. See
`docs/evidence/components-v5-button-foreground-v1/checkpoint.json`.

The catalog also demonstrates an action that enters loading on its own Click,
with an adjacent Finish action. A source-built x64 catalog passed the focused
`--smoke-buttons` fixture in both themes. In a visible desktop check, Space on
the focused Start action entered loading, disabled it, and moved the visible
focus ring to Finish; Space on Finish restored Start. The UI Automation
`focused_element` field continued to report the catalog search edit, so the
focus result is based on the visible ring and keyboard activation. See
`docs/evidence/components-v6-button-self-loading-foreground-v1/checkpoint.json`.
This source-build check does not qualify a new NuGet package or independent
consumer.

The current catalog validation host and independent x64 consumer of
`0.1.0-menu-toast-modal-v1-local` each pass all 448 lifecycle checks, including
those 112 focus-eligibility assertions, and retain 84 offscreen images. Each
auditor rejects 57 altered reports, including false ready/loading/restored/
disabled focus results. Evidence is under
`artifacts/current/button-focus-eligibility-catalog-native-v1` and
`button-focus-eligibility-package-native-v1`. Both hosts also build for ARM64;
native ARM64 execution remains unverified. This fixture-only change reuses the
qualified library package and does not replace the current delivery archives.

The shared `NativeRenderReadiness` fixture helper keeps its `RenderTargetBitmap`
alive until the native render completes. A separate hidden
`--probe-hidden-render-owner-lifetime` regression verifies seven retained frames
under forced collection and records three unretained-owner counterexamples on
the pinned x64 WinUI build. Forced collection is confined to that diagnostic;
normal component fixtures do not request it. See [runtime evidence](runtime-evidence.md)
for the orphaned original archive run and its completed corrected replays.

The built-in loading template renders Bleecker's Lucide `LoaderCircle` arc at 16px with rounded strokes and an 8px content gap. It rotates linearly once per second while loading and stops/reset when loading ends or the wrapper unloads. Its stroke follows the rendered action text color. Lucide's ISC/MIT notices ship in `licenses/LICENSE-Lucide.txt`.

Windows `UISettings.AnimationsEnabled` controls motion. With animation disabled, the arc stays visible at rest while the action retains its busy status. Windows 10 version 19041 and later notify the control of live setting changes. On the declared older minimum, the setting is sampled when loading/presentation changes; live changes during an uninterrupted loading operation remain an acceptance limitation. Consumer replacement templates own their progress visuals. The standalone Bleecker `LoadingSpinner` is a separate inventory entry and is not implemented by this button-specific arc.

String content supplies the native action's accessible name and stays synchronized with content bindings. `AutomationProperties.Name` overrides that default; `AutomationProperties.LabeledBy` can supply an external label. Set an explicit localized name or label for complex/icon content. These properties are forwarded to the inner action without replacing consumer bindings on the wrapper. Naming behavior is checked through the real UI Automation peer.

Button text selects `BleeckerPrimaryMediumFontFamily` and weight 500 explicitly. The package includes the real Encode Sans Medium face; no locally installed font is required. Explicit font properties on consumer-owned content retain their normal WinUI precedence.

The wrapper's default `CharacterSpacing` is zero, matching the pinned button
recipe independently of a containing heading's tracking. Caller local spacing
and bindings retain normal WinUI precedence. Data Table's full replay checks
the resulting native font/weight/tracking and shared column geometry; the fresh
C# default-style probe also covers inherited parent tracking, local spacing,
binding updates and clearing values back to the library default.

The catalog registers all variants/sizes, disabled/loading controls, full-width and multilingual wrapping fixtures. Runtime checks cover native command invocation and parameters, loading rejection, restoration of consumer-enabled state without replacing the binding, per-variant rendered colors in both themes, and connected-group integration. `CanExecuteChanged` is verified in both directions, including loading and size changes while execution is forbidden. A replacement template is instantiated and its native action is invoked to verify exactly one command and Click event, then the original template is restored. The independent package consumer instantiates the custom control, loads its template and per-variant resources, and invokes its Click event.

Consumer templates must provide `PART_Action` (`Button`) and `PART_Progress` (`ProgressRing`). They may provide `PART_LinkUnderline` (`Border`) for the built-in link treatment. Keep the native action's command, parameter, content and automation bindings when replacing the template. Missing required parts fail explicitly.

The hidden default-style fixture constructs a C# button before any other
component fixture, checks its required native parts, and verifies caller
`Style`/`Template` precedence and restoration. Native theme initialization must
finish before the fixture manually forces root measurement; otherwise it can
observe a temporary base ContentControl template. The original library button
style is retained; an explicit resource URI and a template extraction were
unsuccessful diagnostic candidates and were reverted.
The fresh `0.1.0-modal-host-core-v1-local` x64 consumer passes all ten Light/Dark
states, with no explicit style in the construction/restoration states, no
activation/focus/input and 60 hidden-window guards. This does not establish
normal visible catalog startup or physical button activation.

Acceptance remains in progress: the complete keyboard/pointer matrix, scaling,
reduced motion and ARM64 runtime. The [current library scope](library-scope.md)
excludes Narrator and OS contrast campaigns. Desktop pointer testing confirms
the link's hover-only underline; runtime checks verify its text color binding
through each visual state. The [rendered visual audit](visual-audit.md) records
Bleecker measurements and outstanding appearance work. Published reports and
captures prove only the checks they exercise.

The exact v7 extracted x64 catalog now passes a focused real-pointer route in
both Light and Dark. A physical click on Run preview invokes the consumer
command once; loading disables the native action and rejects a second click;
turning loading off restores the command. The Start/Finish pair also enters and
leaves self-loading through pointer clicks without accepting a second busy
activation. The [v7 Button pointer checkpoint](evidence/components-v7-button-pointer-v1/checkpoint.json)
binds both reports to the packaged Orchard DLL and preserves two failed probe
runs: a zero-dwell synthetic click left the loading switch On. The final probe
uses a short pointer settle and press interval. This route does not cover the
remaining variants, independent-consumer pointer input, scaling, motion, or
native ARM64 runtime.

The same extracted v7 x64 catalog also passes focused physical Space and Enter
activation in both themes. Each key reaches the consumer's Run preview command
once after the pointer/loading route, with the command count moving from two to
three and then four. The [v7 Button keyboard checkpoint](evidence/components-v7-button-keyboard-v1/checkpoint.json)
binds these reports to the unchanged package. Focused Space on Start also enters
self-loading and disables Start; focused Enter on Finish restores it in both
themes. The remaining Button variants,
independent-consumer physical input, scaling, motion and ARM64 runtime remain
open.

Light Ghost hover/pressed sand uses byte alpha 31 at brush opacity 1, based on the [actual source/native alpha diagnostic](brush-alpha-reference.md). Dark, disabled, contrast and other variant recipes retain their existing representations.
