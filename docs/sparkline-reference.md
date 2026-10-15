# Sparkline source contract and acceptance

`BleeckerSparkline` implements the pinned Bleecker
[`Sparkline`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/sparkline.tsx).
Source SHA-256: `7e143cfb557045bfad591be820c28b3bcb29e77469261f51723317f4ffef0ed5`.
The capture uses the source's React 19.2.4 and Recharts 3.8.1 lock entries.

```xml
<orchard:BleeckerSparkline Data="{Binding History}" DataKey="value"
    Width="320" Height="40" AutomationProperties.Name="Consumer trend" />
```

Data is an observable collection of consumer-owned
`IReadOnlyDictionary<string, object?>` records. Supply DataKey before mounting;
an empty key is valid. Literal keys precede nested paths. Collection replacement,
record notifications and bound key changes use the same native chart renderer
as Line Chart. The collection and records are retained without copying.

Height defaults to 40px. The plot has 2px margins, a monotone 2px line, no axes
or legend, and no ordinary dots. Color accepts a SolidColorBrush; null selects
the source Sea paint, including in Dark, and explicit transparency is retained.
Null values split the curve, and a single point has no ordinary visible line.
The implicit numeric domain follows the pinned Recharts adaptive five-tick
algorithm, including all-negative, zero and constant data.

ShowArea retains the source option. The actual pinned source renders a Recharts
Line with `fill="none"` even when ShowArea is true; its gradient definition is
unused. Orchard preserves that observed output. It does not paint an area.

The inner chart provides one native keyboard stop and receives the consumer's
AutomationProperties.Name. Focus inspects the first point; Left/Right, Enter,
blur and pointer ownership use the shared Line Chart interaction contract.
Active dots have radius 3 and no outline. The cursor uses the source #ccc paint
in Light/Dark and the system window text color in contrast resources. Tooltip
content has no category heading and preserves the source separator and raw
value, for example ` : 30`. Missing/null values have no tooltip row. Boolean
coordinates coerce to numbers while the source domain excludes booleans; its
boolean tooltip content is empty. No backend, configuration or telemetry is
involved.

`tests/Reference/SparklineLayout.json` contains 36 rendered Light/Dark cases:
default, ShowArea, negative, zero, constant, gaps, single, empty, missing, nested,
numeric strings, explicit/transparent paint, short/narrow layouts, an empty key,
all-negative and nonnumeric data. Capture SHA-256:
`bcb1a8b4f58995000a4a415a3d907dd6d8d77474856af14759a65d8d80b86ac5`.
`scripts/derive-sparkline-reference.mjs` validates the source/version boundary
before importing the capture. Native geometry uses the existing 0.000501px
comparison against D3's three-decimal SVG output; the tolerance is unchanged.
The fixture additionally checks raw tooltip text, placement, active dots, native
Tab/Right/Shift-Tab, pointer entry/exit, consumer updates, bindings and remount.
Both architecture jobs and the isolated NuGet consumer register this component.
Push-triggered Actions remain paused at the user's request.

Focused x64 verification passes in both themes at
`artifacts/smoke-sparkline-final`, including all 36 source specimens and the
additional real-input/lifecycle checks. The normal Line Chart focused suite
also passes at `artifacts/smoke-line-chart-sparkline`. All 43 unit regressions
pass at `artifacts/tests-sparkline-final`, and the pinned token check passes.
Native/browser default curve captures were visually inspected. These local
checks use display/text scale 1 and the host's disabled animation effects.
The full Light/Dark catalog passes at
`artifacts/smoke-sparkline-full-diagnostic` within its unchanged six-minute
deadline. An earlier full run fails a stationary disabled-Input cursor check
in the Checkbox sequence. That intermittent failure remains open; the rerun
does not establish its cause or a fix. The fixture retains additional native
diagnostics for a recurrence. See [runtime evidence](runtime-evidence.md).
Final x64 and ARM64 solution builds pass with zero warnings. ARM64 compilation
on this Intel host is a cross-build and does not establish native runtime.

Fresh package 0.1.0 from clean implementation commit
`c6c9c52dac019889f8deae5b9e88e53634173e3d` has SHA-256
`defc7ab89f4223a771370a2377b4bff958c89ae31111dc467dc81d3a3f2513fa`.
The isolated x64 consumer passes both themes at
`artifacts/package-consumer-sparkline-corrected`, including compact geometry,
data identity, naming, physical Right navigation and collection replacement.
Each theme starts an actual fresh lifecycle: refocus alone intentionally leaves
retained inspection hidden. The first consumer fixture incorrectly assumed
otherwise; correcting its lifecycle/input setup required no library change.
The same corrected consumer cross-builds for ARM64 at
`artifacts/package-consumer-sparkline-arm64-corrected`; runtimeVerified remains
false. Both consumers remove their GUID-scoped temporary directories, including
the initial failing x64 run. The separately published x64 catalog passes all 36
focused cases at `artifacts/smoke-sparkline-published`. Native ARM64/hosted
validation is deferred while push-triggered Actions remain paused.

This work also exposed a shared all-negative domain bug. The earlier reference
generator incorrectly clamped the maximum to zero. Four independently rendered
normal Line Chart axes in `tests/Reference/NegativeChartDomains.json` establish
the corrected upper bound and singleton behavior. Native axes and regression
tests compare their numbers and visible tick strings to those DOM captures.
Decimal tick construction avoids binary multiplication tails in axis labels.

Full text/curve/effect raster, normal-motion timing, RTL and consumer overrides,
extreme numeric/data shapes, disabled/error contracts, actual contrast settings,
Narrator, display/text scaling and native ARM64 acceptance remain open. This
entry remains in progress. The public wiki remote is unavailable, so this
contract is maintained in the repository.

The Bar Chart integration's first full x64 sequence stops at Sparkline's negative
specimen after its Right key (`artifacts/smoke-bar-chart-full`). That run records
no per-key delivery information, so the exact cause remains unproven. The fixture
now requires exactly one native press/release at the actual focused chart and
retains data, loaded/focus state, tooltip items, private interaction channels and
profile state on failure. It sends no additional key and changes no expected
output or two-second settling deadline. Focused Light/Dark checks pass at
`artifacts/smoke-sparkline-bar-key-ack`. The sequence investigation and final
confirmation follow below.

The shorter Bar Chart → Sparkline → Stat Card sequence reproduces a pointer
channel override before any Sparkline arrow. At
`artifacts/smoke-bar-chart-sequence-pointer`, Win32 reports (910,843), outside
the Sparkline's (262,285)–(582,325) bounds, while XAML delivers local (55,20).
That maps to (317,305), the preceding Bar Chart button's last physical position.
The chart is loaded/enabled, owns keyboard focus, and uses the Sparkline profile;
pointer index 1 overrides keyboard index 0. The session's SetCursorPos restoration
leaves this stale WinUI input position behind.

The existing single cursor restoration now uses the same absolute virtual-desktop
SendInput movement path as normal fixture moves. The complete chart sequence
passes both themes at `artifacts/smoke-bar-chart-sequence-restored`, retaining
all source outputs, normal native delivery and unchanged deadlines. This is a
catalog input-session correction; chart interaction behavior is unchanged.
The complete Light/Dark catalog subsequently passes at
`artifacts/smoke-bar-chart-full-restored`, under the same six-minute deadline,
including all pre-existing pointer/cursor and keyboard gates.

The Scatter full catalog at
`artifacts/smoke-scatter-chart-published-viewport-full` fails the Light mutation's
physical Right value check. That path did not retain key delivery or private
interaction state, so its cause is unclassified. It now uses the existing
native press/release verifier on the actual focused chart and records focus,
keys, pointer, modes, data/observer identity and interaction fields on both
success and failure. Expected value and the two-second deadline are unchanged;
no Sparkline production code changes. The focused `-Component
scatter-chart-sequence` passes both themes at
`artifacts/smoke-scatter-sparkline-input`, with one Right down/up, matching
focus, observers connected and the expected ` : 30` value. Complete catalog
validation remains separate; this does not prove the original failure's cause.
