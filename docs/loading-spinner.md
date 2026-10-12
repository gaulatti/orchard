# Loading spinner

`BleeckerLoadingSpinner` is the standalone Bleecker indeterminate loading indicator. It is distinct from the Lucide arc inside a loading button.

```xml
<orchard:BleeckerLoadingSpinner Size="Medium"
    AutomationProperties.Name="Syncing changes" />
```

Supply a localized accessible name describing the operation. The control exposes a progress-bar automation role, has no numeric value, and is not a keyboard stop. It owns no operation, networking, completion state or error handling. Show/hide it through normal consumer bindings; hide or remove it when work finishes. It has no selection, input, empty or error variant.

| Size | Diameter | Border |
| --- | --- | --- |
| Small | 16 | 2 |
| Medium (default) | 32 | 2 |
| Large | 48 | 3 |

The ring leaves its top quarter transparent, matching the reference's CSS border geometry. It uses the primary theme brush (sea in light appearance, accent blue in dark appearance, system highlight in Windows contrast themes). Consumers can set Foreground explicitly. Keep the intrinsic ring size rather than stretching it to fill a container; use surrounding layout for alignment.

When Windows enables animations, it rotates linearly once per second. Reduced motion retains a static visible ring. Collapsing or unloading stops and resets rotation; showing or reloading resumes it. Template replacement releases the previous animation. Windows 10 version 19041 and later provides live animation-preference notifications. On the older declared minimum, the preference is read on load, visibility and size changes; uninterrupted live preference changes remain an acceptance limitation. Consumer replacement templates own their visuals.

## Evidence and remaining acceptance

The registered catalog fixture renders all three sizes. Native x64 checks cover dimensions, arc/border geometry, light/dark colors, progress roles and names, runtime size changes, reduced motion, hide/show and unload/reload. The report records the actual animation setting. On this Intel host animations are disabled, so normal-speed rotation is not yet verified. The clean NuGet consumer instantiates the small spinner and checks its dimensions and role.

Remaining: normal animation and live preference switching, actual Windows contrast settings, Narrator, display scaling, constrained composition and ARM64 runtime. Source geometry and successful compilation do not close these acceptance gaps.
