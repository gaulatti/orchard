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

The registered catalog fixture renders all three sizes and a 120-DIP status row with a fixed 16-DIP ring beside an ellipsized label. The focused native x64 check `./scripts/smoke-catalog.ps1 -Component loading-spinner` passed in Light and Dark at display/text scale 1 with animations disabled; it checks the constrained row's bounds and captures both themes ([constrained-row checkpoint](evidence/loading-spinner-constrained-v1/checkpoint.json)). On the same loaded fixture, it also verifies live Light/Dark brush updates, an explicit consumer `Foreground` override, and theme-brush restoration after clearing that override ([foreground checkpoint](evidence/loading-spinner-foreground-v1/checkpoint.json)). Native x64 checks also cover dimensions, arc/border geometry, progress roles and names, runtime size changes, reduced motion, hide/show and unload/reload. The report records the actual animation setting. The earlier `artifacts/smoke-normal-motion/result.json` run on Intel Windows 11 verifies rotation with Windows Animation effects temporarily enabled, including stop/reset on hide and unload in both themes. The original disabled setting was restored afterward. The check samples an angle change over 180ms; it does not measure a complete one-second cycle or live preference switching. The clean NuGet consumer instantiates the small spinner and checks its dimensions and role.

The [isolated scale check](evidence/loading-spinner-isolated-scale-v1/checkpoint.json) mounts the large Spinner in the existing hidden x64 XAML island. In Light and Dark at 100%, 125%, 150% and 200% `XamlRoot.RasterizationScale`, its logical diameter remains 48 DIPs, the arc remains visible in the scaled capture, the stroke stays 3 DIPs, and its named ProgressBar peer remains outside Tab navigation. The check reuses the existing Separator/Status Badge/Switch scale fixture and rejects 29 damaged audit reports. It changes no Windows display, text-scale or accessibility setting and sends no hardware input. This proves isolated rasterization of the large size; it does not prove a physical monitor-DPI move, system text scaling of the consumer-owned status row, normal-speed motion at those scales, native ARM64 execution, or a fresh package consumer.

Remaining: precise animation timing and live preference switching, physical display and consumer-row text scaling beyond the isolated large-size check, ARM64 runtime and complete visual acceptance. Narrator and OS contrast campaigns are excluded by the [current library scope](library-scope.md). Source geometry and successful compilation do not close these acceptance gaps.
