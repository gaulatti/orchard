# BleeckerButton

Before the runtime variant/state matrix, the catalog acquires its own foreground
window and moves the actual pointer to the fixture heading. This establishes the
non-hover precondition for the initial link-underline assertion even when the
restored pointer overlaps the action after a fixture/theme change. Native input
ownership remains checked and the prior pointer position is restored when the
matrix finishes. Actual hovered states remain separate acceptance checks.

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

The `Link` variant is a command action with Bleecker's hover-only underline. Its text size is 16px and it has no size-dependent minimum height or padding, matching the source recipe that omits button size classes. Disabled and loading links do not show the hover underline. Application navigation belongs in its command. Use native `HyperlinkButton.NavigateUri` where Windows should own opening a URI.

For custom icon content, supply an accessible name. For long text, use a wrapping `TextBlock` as content and constrain the control width. Font and corner radius come from canonical resources. Variant brushes are generated in component-scoped theme dictionaries; changing the variant replaces that dictionary while keeping the native input control. Light, dark and Windows high-contrast resources are provided. The inner button retains the pinned WinUI template and state machine.

`BleeckerButtonGroup` accepts this control alongside native `ButtonBase` controls. The group controls connected corner radii and borders; commands, loading and content remain on each action.

## Verification

The built-in loading template renders Bleecker's Lucide `LoaderCircle` arc at 16px with rounded strokes and an 8px content gap. It rotates linearly once per second while loading and stops/reset when loading ends or the wrapper unloads. Its stroke follows the rendered action text color. Lucide's ISC/MIT notices ship in `licenses/LICENSE-Lucide.txt`.

Windows `UISettings.AnimationsEnabled` controls motion. With animation disabled, the arc stays visible at rest while the action retains its busy status. Windows 10 version 19041 and later notify the control of live setting changes. On the declared older minimum, the setting is sampled when loading/presentation changes; live changes during an uninterrupted loading operation remain an acceptance limitation. Consumer replacement templates own their progress visuals. The standalone Bleecker `LoadingSpinner` is a separate inventory entry and is not implemented by this button-specific arc.

String content supplies the native action's accessible name and stays synchronized with content bindings. `AutomationProperties.Name` overrides that default; `AutomationProperties.LabeledBy` can supply an external label. Set an explicit localized name or label for complex/icon content. These properties are forwarded to the inner action without replacing consumer bindings on the wrapper. Naming behavior is checked through the real UI Automation peer; Narrator acceptance remains separate.

Button text selects `BleeckerPrimaryMediumFontFamily` and weight 500 explicitly. The package includes the real Encode Sans Medium face; no locally installed font is required. Explicit font properties on consumer-owned content retain their normal WinUI precedence.

The catalog registers all variants/sizes, disabled/loading controls, full-width and multilingual wrapping fixtures. Runtime checks cover native command invocation and parameters, loading rejection, restoration of consumer-enabled state without replacing the binding, per-variant rendered colors in both themes, and connected-group integration. `CanExecuteChanged` is verified in both directions, including loading and size changes while execution is forbidden. A replacement template is instantiated and its native action is invoked to verify exactly one command and Click event, then the original template is restored. The independent package consumer instantiates the custom control, loads its template and per-variant resources, and invokes its Click event.

Consumer templates must provide `PART_Action` (`Button`) and `PART_Progress` (`ProgressRing`). They may provide `PART_LinkUnderline` (`Border`) for the built-in link treatment. Keep the native action's command, parameter, content and automation bindings when replacing the template. Missing required parts fail explicitly.

Acceptance remains in progress: the complete keyboard/pointer matrix, Narrator, actual contrast settings, scaling, reduced motion, loading focus behavior and ARM64 runtime. Desktop pointer testing confirms the link's hover-only underline; runtime checks verify its text color binding through each visual state. The [rendered visual audit](visual-audit.md) records Bleecker measurements and outstanding appearance work. Published reports and captures prove only the checks they exercise.

Light Ghost hover/pressed sand uses byte alpha 31 at brush opacity 1, based on the [actual source/native alpha diagnostic](brush-alpha-reference.md). Dark, disabled, contrast and other variant recipes retain their existing representations.
