# BleeckerButton

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

The `Link` variant is an underlined command action, not an automatic URL launcher. Its persistent underline supplies a non-color affordance. Application navigation belongs in its command. Use native `HyperlinkButton.NavigateUri` where Windows should own opening a URI.

For custom icon content, supply an accessible name. For long text, use a wrapping `TextBlock` as content and constrain the control width. Font and corner radius come from canonical resources. Variant brushes are generated in component-scoped theme dictionaries; changing the variant replaces that dictionary while keeping the native input control. Light, dark and Windows high-contrast resources are provided. The inner button retains the pinned WinUI template and state machine.

`BleeckerButtonGroup` accepts this control alongside native `ButtonBase` controls. The group controls connected corner radii and borders; commands, loading and content remain on each action.

## Verification

The catalog registers all variants/sizes, disabled/loading controls, full-width and multilingual wrapping fixtures. Runtime checks cover native command invocation and parameters, loading rejection, restoration of consumer-enabled state without replacing the binding, per-variant rendered colors in both themes, and connected-group integration. `CanExecuteChanged` is verified in both directions, including loading and size changes while execution is forbidden. A replacement template is instantiated and its native action is invoked to verify exactly one command and Click event, then the original template is restored. The independent package consumer instantiates the custom control, loads its template and per-variant resources, and invokes its Click event.

Consumer templates must provide `PART_Action` (`Button`) and `PART_Progress` (`ProgressRing`). They may provide `PART_LinkUnderline` (`Border`) for the built-in link treatment. Keep the native action's command, parameter, content and automation bindings when replacing the template. Missing required parts fail explicitly.

Acceptance remains in progress: desktop keyboard/pointer for the wrapper, Narrator, actual contrast settings, scaling, reduced motion, loading focus behavior and ARM64 runtime. The desktop-control tool's window-opened handler failed to initialize during this checkpoint, so no new physical-input acceptance is claimed. Published reports and captures are evidence for the checks they exercise; they do not close those remaining requirements.
