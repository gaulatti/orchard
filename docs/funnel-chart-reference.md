# Funnel Chart source contract and acceptance

The frozen [Funnel Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/funnel-chart.tsx)
has SHA-256 `eff2a2992f0e3fa08bf78beec46bd4291dc58013fa6cbe3c58cc46c5200384af`.
Its pinned runtime is React 19.2.4 and Recharts 3.8.1. Orchard pins .NET SDK
10.0.401, Windows App SDK 2.5.1 and Win2D 1.4.0. The exact
[Recharts source](https://github.com/recharts/recharts/blob/v3.8.1/src/cartesian/Funnel.tsx)
and [release](https://github.com/recharts/recharts/releases/tag/v3.8.1) were
consulted before implementation. The public repository wiki is unavailable;
this document retains the contract in the code repository.

Acceptance requires original consumer-owned name/value/fill records to drive
native trapezoid paths, inside labels, source palette and inspection. Height
defaults to 350 and ShowLabels to true. ValueFormatter applies at the producer's
inspection boundary. Capture actual source label text: the frozen label
formatter's optional second argument must not be assumed to contain a value.
Consumer brushes, including transparent fill, retain their identity.

Before implementation, capture the unchanged component with its exact frozen
dependency lock and bundled fonts loaded. Cover both themes, one/two/many/empty
items, zero/constant/increasing/negative/mixed values, decimals, missing/literal
fields, duplicated/long/non-Latin names, explicit paint, label/formatter modes,
and constrained/fractional sizes. Record paths, label text/baselines/paint,
tooltip payload/visibility/geometry and actual focus, arrows, Enter and hover.

Trace the native feature path through public dependency properties, original
collections, record notifications, geometry/text rendering, theme/template
resources, one named keyboard inspection stop, actual native pointer/keyboard
input, disabled traversal, stationary updates, detach/rapid remount, template
and collection replacement, resize and empty data. Register catalog navigation,
coverage and focused/full runtime checks. Validate a fresh independent NuGet
consumer, both architecture builds and the full published catalog. Preserve
all previous checks, coordinate tolerances and runtime deadlines.

Work is in progress and this inventory entry is not accepted. Complete raster,
motion/reversal, full data/override/RTL coverage, component theme/automation behavior,
display/text scaling, measured collection performance and native ARM64 runtime
remain required. This native-only presentation work has no backend/Prometheus
surface.

The unchanged source capture has 54 Light/Dark cases and zero browser page
errors. `tests/Reference/FunnelChartLayout.json` has SHA-256
`a02098e91de0cca4deda74d24ea03c1e13e3403acb68750746e70b47fef25ccf`.
Capture waits beyond the producer's 400ms animation delay plus 1500ms duration,
then requires unchanged SVG paths/text across three Rendering transitions.
All 46 portable regressions pass, including the full source path matrix at
the unchanged .001 coordinate limit. All 54 x64 native source cases and both
lifecycle suites pass at `artifacts/smoke-funnel-chart-lifecycle`. The complete
Funnel → Radial Bar → Radar → Scatter → Sparkline sequence also passes both
themes at `artifacts/smoke-funnel-chart-sequence`.

The producer's settled labels contain names followed by a colon and trailing
space, without values. They wrap using the mean of the upper/lower trapezoid
widths. Missing names produce no label. Focus inspects the first original value;
the producer's four arrow keys keep that selection, Enter hides inspection, and
native hover inspects the actual filled trapezoid. Inspection has no heading or
cursor. It preserves the producer's raw scalar/array text and applies
ValueFormatter only to numeric scalar values. A zero maximum paints no paths
or labels; its visible zero-value tooltip stays at the source wrapper's origin
because the browser rejects its nonfinite transform.

The fresh regular `0.1.0` package from clean source `648dd18`, SHA-256
`125605d46bbb85c5a1f100136519a95eb880aae061da5bf780f59a0c8998d7cd`,
passes actual x64 consumption in both themes at
`artifacts/package-consumer-funnel-chart-reset`. The independent test resets
its consumer-owned formatter before each theme at `efc5ba2`, then checks
original record identity, source geometry/labels, native input, notifications,
formatting, transparent fill, live names/visibility and empty cleanup. The same
package passes the isolated ARM64 build-only consumer at
`artifacts/package-consumer-funnel-chart-arm64`; ARM64 runtime is unverified.
Both temporary restore/build workspaces are removed.

The runnable x64 catalog at `artifacts/catalog-funnel-chart` is built from
`648dd18`. Its full Light/Dark run passes in 354.689 seconds at
`artifacts/smoke-funnel-chart-published-full`, within the unchanged 360-second
deadline. All 940 earlier PNGs remain, with twelve new source captures.
All 1,146 pointer gates pass their original ownership and two-unchanged-
transition checks. All 28 tooltip opening gates pass their full-opacity/scale
and stable geometry checks. Sixteen final capture layout reports settle.
Default Light/Dark Funnel captures were visually inspected. See
[the runtime record](runtime-evidence.md) for the unmasked font-capture
comparison and outstanding raster/platform acceptance gaps.
