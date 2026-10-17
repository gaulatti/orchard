# Chart source contract and acceptance

The frozen [Chart source](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/chart.tsx)
has SHA-256 `eacc1a099bf0957734ddbffcae03f28a4a087f7c3ba676812ba42094d3d21ece`.
It is a fixed currency line plot: original consumer `data`, Height 300 by
default, `name` categories, `total` values, a desert-colored two-unit monotone
line, no ordinary dots/legend, and an active dot of radius six with card stroke.
The source margins are top/bottom five, right twenty and left zero before axes.
Y ticks prefix dollars; tooltip values prefix dollars and use the name Total.

Acceptance begins with unchanged source measurements under the frozen React
19.2.4/Recharts 3.8.1 runtime. Capture both themes, normal/empty/single/dense
data, zero/negative/decimal values, missing/null/coercible/malformed values,
duplicate/missing/Unicode/long names and constrained/fractional dimensions.
Record all line/control points, axis domains/ticks, grid, tick text/baselines,
paint, clipping, tooltip text/geometry, active dots and real focus/keyboard/
pointer behavior. Record invalid-input behavior without inventing fallback.

The native control must retain original records and observable collection
identity. Trace value/name notifications, append/remove/reorder/reset, root
collection replacement, font/theme changes, source-specific defaults, resize,
detach/rapid reparent, template replacement, disabled input and empty cleanup
through the shared renderer and accessibility consumers. Preserve exact literal
source keys and dollar-string behavior. Native keyboard conventions must be
identified explicitly where they extend the measured browser behavior.

The shared Line Chart path must preserve every other chart profile. Add native
catalog/navigation/coverage and independent package-consumer cases, source
boundary regressions, both architecture builds and complete published runtime
evidence. Keep all current source assertions, tolerances, native exact event
receipts, two-second presentation deadlines and the full 360-second deadline.

This entry is in progress and not accepted. Exact raster/motion, complete data/
override/RTL coverage, actual contrast/Narrator/display and text scaling,
measured collection performance and native ARM64 runtime remain required.
The public wiki remains unavailable, so documentation is retained here. This
native presentation component has no backend or Prometheus surface.

## Native usage and measured boundaries

`BleeckerChart.Data` retains an `ObservableCollection<object?>`. Dictionary
records expose the literal `name` and `total` fields; records implementing
`INotifyPropertyChanged` notify the renderer without replacement. Null and
primitive rows retain missing-field behavior. The control has no consumer
series, palette, legend or currency formatter option, matching the fixed export.

```csharp
var records = new ObservableCollection<object?>
{
    new Dictionary<string, object?> { ["name"] = "Harbor", ["total"] = 100d },
    new Dictionary<string, object?> { ["name"] = "Reed", ["total"] = "220.00" }
};
var chart = new BleeckerChart { Data = records, Width = 640 };
AutomationProperties.SetName(chart, "Fictional currency totals");
```

The source filters invalid category names before assigning line X coordinates,
unless duplicate valid names switch it to index categories. Axis text and
tooltip payload lookup have different missing-name behavior. In particular, a
missing name among duplicate names renders an `undefined` tick, and duplicate
tooltip labels select the first original matching record. Numeric strings keep
their original precision in dollar tooltips. Arrays retain JavaScript comma
joining; range values supply numeric domains without becoming line points.

The checked-in `tests/Reference/ChartLayout.json` holds 94 unchanged browser
cases under one React 19.2.4/Recharts 3.8.1 runtime. Its SHA-256 is
`e33771787ca89e33ff18da45c7cf5c1fdc791c67c3e42b6756672385175240ea`.
`scripts/derive-chart-reference.mjs` verifies the source hash, actual bundle
inputs and dependency lock integrities before deriving the golden. Pure
regressions compare domains, curve commands, category text, payload identity,
raw currency strings and active points without mutating original records.

The catalog's Currency chart fixture compares native axes, grids, curves,
paint, initial tooltip dimensions/placement and active dots to those captures.
It uses native keyboard and exact physical pointer input. Arrow captures include
the source's moving tooltip; only settled initial placement is compared so far.
WinUI shape bounds include the stroke, so the active ellipse has 14-unit outer
bounds for the source's six-unit path radius and two-unit stroke.

## Implementation checkpoint

Both x64 and ARM64 solution builds pass without warnings; 56 local tests and
token-generation checks pass. The native eleven-profile chart sequence passes
in Light and Dark in 165.08 seconds at display/text scale one with animations
off (`artifacts/smoke-currency-chart-sequence`). All 94 currency source cases
pass native geometry, tick/grid, paint and initial inspection checks. Real Tab,
reverse Tab, arrows, Enter, physical pointer, live record/collection changes,
shared records, resize/font, disabled focus, detach/rapid reparent and template
replacement checks pass. This host cannot execute native ARM64 binaries.

All twelve complete source/native chart viewport rasters still differ:
769–5,761 pixels, maximum channel difference 239, with no masks or pixel
tolerance (`artifacts/currency-chart-raster-comparison.json`). Full raster and
motion parity remain open. Fresh package-consumer and complete published
catalog validation are still pending at this source checkpoint. Inventory is
48 in progress, 64 pending, none accepted.

The first complete published run at `1b9bf1b` timed out at 360.223 seconds
during Dark Input after all chart checks passed. Its 884 PNGs and failure are
preserved in `artifacts/smoke-currency-chart-published-full`.

The currency matrix now replaces original data and dimensions on one mounted
consumer, awaiting real Loaded/GotFocus receipts and a Rendering boundary.
Keyboard resets use real native keys; all 94 cases and the same curve, axis,
grid, paint, tooltip and cursor assertions remain. Normal captures retain
their presentation gate, and physical pointer moves retain two stable Rendering
transitions, owned final bounds and exact nongenerated event receipts. All
two-second presentation deadlines and the full 360-second deadline remain.
Focused Brand, Bauhaus, Input and Currency checks pass both themes. Static
setup/unload waits now use presentation/Unloaded events, and Brand RTL checks
await presentation after direction changes. Measured motion windows remain
unchanged. The revised complete published run is still pending.

The revised `a43fc9b` run stopped at 67.103 seconds in Light Bar keyboard
inspection (`artifacts/smoke-currency-chart-events-published-full`). A separate
unchanged-browser probe confirms mouse-over-keyboard priority and exit
handover. The Bar fixture now verifies that real interaction explicitly and
awaits GotFocus before its original keyboard assertions. Both themes pass the
focused Bar checks; the suite now has 57 passing tests. Production chart
interaction behavior is unchanged. Complete published verification is pending.

The `1251148` candidate passed Bar and stopped at the equivalent Area keyboard
handover boundary after 70.630 seconds. Area now verifies the same source
hover/key/exit contract. The extended Bar → Area → Sparkline → Stat Card
sequence passes both themes (`artifacts/smoke-bar-area-source-handover`). No
source assertion, pointer receipt requirement or deadline changed.

## Validated published checkpoint

The clean `aa5e6d4731d8843fdaf2589abc2212284064801f` catalog passes the
complete run in **358.064 seconds**, under the unchanged 360-second deadline:
`artifacts/smoke-currency-chart-area-handover-published-full`. All 988 PNGs are
present, including all 976 from the preceding checkpoint. Both themes pass all
94 currency cases. The full audit records 1,710 successful pointer gates with
at least two stable Rendering transitions (maximum 233.734 ms), 1,710 owned
exact moves, 1,724 exact key actions, 46 click releases, 28 tooltip gates and
16 final capture gates, with no failures. Eighty-six stationary moves reuse
only an actual prior receipt at the same current point. All 174 existing
Sunburst/Radial Bar glyph hashes remain identical.

Both architecture builds have zero warnings, and 57 tests plus token checks
pass. The runnable catalog is `artifacts/catalog-currency-chart-area-handover`
with ProductVersion `0.1.0+aa5e6d4731d8843fdaf2589abc2212284064801f`.
Package 0.1.0 is
`artifacts/packages-currency-chart-area-handover/Gaulatti.Orchard.0.1.0.nupkg`,
SHA-256 `ade2395c1ec16f50961a1f9dc56c4b4c7be9877d8e47adcf9e2ff366164c9155`.
The fresh x64 consumer passes both themes at
`artifacts/package-consumer-currency-chart-final`; the ARM64 consumer build
passes at `artifacts/package-consumer-currency-chart-final-arm64`, with
`runtimeVerified:false`. Both isolated caches are removed. Catalog and x64
consumer load byte-identical Orchard assemblies.

Consumer menu verification now acquires its own foreground window, applies
the presenter/surface templates and awaits their loaded children and actual
shadow layers before inspecting them. This fixes a startup-check race without
changing the package or any assertion. The successful full catalog remains
the exact published `aa5e6d4` artifact.

All twelve complete currency rasters still differ by 769–5,761 pixels, maximum
channel difference 239, with zero masks/tolerance. Inventory remains 48 in
progress, 64 pending, none accepted; all documented platform, data, visual and
performance acceptance gaps remain. Push-triggered Actions remain paused.

The currency lifecycle fixture now observes both native focus departure and
the resulting hidden/empty inspection before issuing reverse Tab. FocusManager
can report the next native owner before routed LostFocus finishes; immediately
returning focus then suppresses that stale blur in the chart. The fixture also
requires Enter to restore the updated record's visible inspection before
reordering the collection. The existing reorder/removal, record subscription,
template, resize and cleanup assertions and two-second deadlines remain.
