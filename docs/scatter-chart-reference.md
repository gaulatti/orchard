# Scatter Chart source contract and acceptance

The frozen [Scatter Chart](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/scatter-chart.tsx)
has SHA-256 `84972e61d3849694bb5d69367e296b806b886747b1e24af1f252601dc86f7e00`.
React 19.2.4 and Recharts 3.8.1 are pinned by the existing source lock. The
installed Scatter, Symbols, numeric-axis selectors and keyboard middleware
establish the implementation contract. Native SDK remains Windows App SDK 2.5.1.

The pinned [Recharts release](https://github.com/recharts/recharts/releases/tag/v3.8.1)
and [native SDK release](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1)
were checked before implementation. Browser captures use the unchanged source
and verify the installed runtime lock entries against the source lock.

Acceptance requires original observable root and per-series data to update the
numeric axes, points, circle legend and raw/ formatted tooltip rows. The first
series owns the shared X/Y/Z axis keys, including when another series declares
different keys. Optional per-series data replaces the root collection for that
series. Missing Z uses the source's 64-area circle; configured Z uses a 60–400
area scale and omits a falsy raw Z from its tooltip. Points retain explicit
transparent paint. The native control must expose one named inspection stop,
traverse normal physical keyboard/pointer input, and preserve observations
across live collections, template replacement, real detach and rapid remount.
Independent package consumption and actual native runtime are required.

`BleeckerScatterChart` and `BleeckerScatterSeries` are being implemented with
consumer-owned records, axis/tooltip formatters, optional labels, grid and legend.
The shared chart renderer supplies tooltip effects and interaction lifecycle;
the Scatter profile owns its numeric axes, points and per-series observers.
Source and native fixtures must establish the behavior before acceptance.

`tests/Reference/ScatterChartLayout.json` contains 64 browser cases, including
both themes, numeric and bubble plots, negative/zero/empty/single data,
independent/empty series, first-series keys, nested/literal keys, explicit paint,
12 series, labels, formatting and constrained/fractional dimensions. SHA-256 is
`c48e3e894ec55959b3796d9ee4fa76258f3845a3deea73fc8e03041a1835a041`.
Import using `scripts/derive-scatter-chart-reference.mjs`.

Initial captures discovered an invalid measurement precondition: the bundled
font was still unloaded when Recharts first measured numeric ticks. Its cache
retained fallback widths even after the final glyphs loaded. The large-number
tick originally shifted to X=601.1875. A separate unchanged-source probe reports
`document.fonts.check` false before explicit loading, then X=596.1328125 when
Encode Sans is loaded before mounting; native 16px measurement gives that same
position. All 64 final cases were recaptured with explicit font loading before
mount. Initial JSON, ruler probes and final captures remain under
`artifacts/scatter-chart-browser`; expected coordinates were not edited by hand.
The ruler allocates on a 1/64px grid at 16px, while the tick glyphs are 12px.
Only labels shift inward; the numeric point scale and grid retain their bounds.

Focused x64 verification passes all 64 final cases in both themes at
`artifacts/smoke-scatter-chart-crosshair`, including circle arcs/positions,
numeric ticks and baselines, grid, axis labels, legend baselines, tooltip
payload/height/visibility and exact crosshair paths. Live checks pass original
root and independent collections, record notifications, shared first-series
keys, bubble updates, native Tab/arrows/hover, transparent brushes, formatters,
legend names, disabled inheritance, real detach, rapid remount, templates and
empty data. Default Light and bubble Dark source/native screenshots were
inspected; full raster acceptance remains open.

The keyboard selector uses concatenated series data for its index bounds but
reads the default tooltip item's payload. When that item has no row at the
selected index, the tooltip can hide while a crosshair remains at the numeric
X and `(plotTop + chartHeight) / 2`. The 12-series source case proves this
behavior, and the native profile retains it. Keyboard inspection chooses the
first nonempty source item; pointer inspection can select any series.

Release x64 and ARM64 solution builds pass without warnings; all 44 existing
unit/resource regressions and generated-token validation pass. Independent
package and complete published catalog verification are recorded after they run.

The regular 0.1.0 package is built from clean source commit
`d6502425f138e5d046765c944644134d7f5e300d`; SHA-256 is
`4f83502c585ea3cd08585d876659dc9a3ec7c288463812cad47c2c7875e7655c`.
Repository metadata matches that commit. The first x64 independent-consumer
geometry assertion reports X=120, Y=113.33333333333334 and radius
6.794000148773193. The native path parser stores the source's rounded 6.794
coordinate in single precision. The consumer now compares the exact stored
float value rather than a double literal; coordinate bounds are unchanged.
Initial reports remain at `artifacts/package-consumer-scatter-chart` and
`artifacts/package-consumer-scatter-chart-geometry`. This correction changes
the consumer assertion only; the package and library remain unchanged.

The same package passes the fresh independent x64 consumer's actual Light/Dark
runtime at `artifacts/package-consumer-scatter-chart-float`: source numeric and
bubble geometry, original root/independent data, one named inspection stop,
physical Right, live record replacements, transparent brush ownership and
empty data. The corrected independent ARM64 consumer builds at
`artifacts/package-consumer-scatter-chart-float-arm64`, with runtime verification
explicitly false. Both generated temporary restore/build workspaces are removed.

The first complete published run at `artifacts/smoke-scatter-chart-published-full`
passes the component fixtures through Dark contrast entry but reaches the
360-second limit during final Dark screenshots, so it is not a complete pass.
The final screenshot navigation now waits for stable viewport geometry rather
than fixed 100/350ms delays. It preserves all fixture checks and screenshot
order, uses a two-second presentation deadline, and retains per-capture layout
evidence. The global deadline remains unchanged. This changes only catalog
verification; package/library inputs remain the independently verified commit.
The focused run at `artifacts/smoke-scatter-chart-viewport` passes all source and
live checks with that final capture gate in both themes. Its layout reports
record three frames/two unchanged transitions, offsets zero, and 32.6826ms and
35.0733ms settling times. All 44 regressions still pass. Complete catalog
verification remains a separate required run.

Full shape/glyph/legend/cursor/tooltip raster and effects, motion, complete
data/key/formatter/override/RTL coverage, actual contrast, Narrator, display/text
scaling, collection performance and native ARM64 acceptance remain open. This
native presentation change has no backend or metrics surface. The public wiki
is unavailable, so the contract is maintained in repository documentation.

The next complete run at
`artifacts/smoke-scatter-chart-published-viewport-full` fails the Light Sparkline
mutation before the final screenshot gate runs. Source input diagnostics and
its focused passing sequence are recorded in [the Sparkline contract](sparkline-reference.md).
That full run is not accepted as a pass. `-Component scatter-chart-sequence`
exercises Scatter followed by Sparkline in both themes.
