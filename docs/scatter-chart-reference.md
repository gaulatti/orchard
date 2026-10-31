# Scatter Chart source contract and acceptance

Scatter Chart is accepted for the 2026-10-29 x64 milestone. Its exact
Light/Dark catalog states and physical pointer/keyboard/focus pass, as do four
native 150% no-clip cases, static reduced-motion behavior, and independent
package-consumer compilation. ARM64 runtime remains unverified.
Earlier checkpoint status statements below are historical and do not add gates to this milestone.

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
data/key/formatter/override/RTL coverage, component theme/automation behavior, display/text
scaling, collection performance and native ARM64 acceptance remain open. This
native presentation change has no backend or metrics surface. The public wiki
is unavailable, so the contract is maintained in repository documentation.

The next complete run at
`artifacts/smoke-scatter-chart-published-viewport-full` fails the Light Sparkline
mutation before the final screenshot gate runs. Source input diagnostics and
its focused passing sequence are recorded in [the Sparkline contract](sparkline-reference.md).
That full run is not accepted as a pass. `-Component scatter-chart-sequence`
exercises Scatter followed by Sparkline in both themes.

The later full run at `artifacts/smoke-scatter-chart-published-input-full`
passes Light, including the Sparkline key gate and final screenshots, but fails
the Dark Scatter record mutation: the independent point moves after Y=50
changes to 60 and its pointer tooltip disappears. This run is not a pass.
An unchanged-source browser probe keeps `x : 40y : 60` visible at the same
physical pointer location after that update. The browser replaces its SVG
element, so element identity is not an acceptance requirement.

The first correction retained point visuals during data changes. The regression
requires updated geometry and tooltip text, then checks the tooltip again after
two rendering events with the native pointer unchanged. It also checks
transparent paint restoration, repeated-series insertion/removal, and the
existing detach/template paths.
The initial retained-visual attempts failed startup (XAML namespace placement,
then sharing an already-parented geometry); those reports remain preserved.
Final package and complete catalog evidence follow after verification.

The strengthened focused sequence passes both themes at
`artifacts/smoke-scatter-chart-settled-hover`, including stationary hover after
rendering, color reset, repeated-series points, all 64 source cases and the
following Sparkline input gate. Release x64 and ARM64 solution builds have zero
warnings; all 44 unit/resource checks and generated tokens pass.

The intermediate regular 0.1.0 package contains that first correction from clean commit
`a6e23bd22a4b8cd6289a71e021c783675cbdce87`. Its SHA-256 is
`6c56d4797a37039bdf0a8872d210b60efb4012fa1f54e4701dbc1bb0c829313f`;
nuspec repository metadata matches that source. The isolated x64 consumer
passes actual Light/Dark runtime at
`artifacts/package-consumer-scatter-chart-hover`. The ARM64 consumer builds at
`artifacts/package-consumer-scatter-chart-hover-arm64`, with runtime verification
explicitly false. Both temporary restore/build workspaces are removed. The
runnable x64 catalog is `artifacts/catalog-scatter-chart-hover`.

That complete published catalog fails the strengthened Dark stationary-hover
check at 291.150 seconds, after the full Light sequence passes. The subsequent
ten-mutation control at `artifacts/smoke-scatter-chart-hover-events-control`
reproduces the failure immediately in Light. Its trace records native pointer
(946,412), chart-local (300,35), timestamp 276593981928 and owned focus unchanged.
The real Path hover has `IsGenerated=false`; the later Line hit has
`IsGenerated=true` and clears the pointer index. Retaining the visual does not
prevent this layout-generated re-hit-test.

Scatter now preserves interaction on platform-generated empty hits and exits.
Live point hits still select their point. The visual cache is removed;
normal rendering still updates geometry, palette and data. Microsoft's
[IsGenerated contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.input.pointerroutedeventargs.isgenerated?view=windows-app-sdk-2.0)
distinguishes these UI changes from user interaction. The regression retains all
event flags, coordinates, timestamps and interaction state, alternates Y=50/60
eleven times with the physical mouse stationary, and checks a real move into blank
plot space and a real exit. The intermediate package is superseded by the next
verified package; its passing consumer is not full catalog acceptance.

The unchanged source hides inspection when an unchanged point is left for blank
space. A point replaced by a geometry update instead retains its last tooltip
over blank space; chart exit hides it. Returning the point under the mouse after
ten alternating updates restores normal leave behavior. The source keys symbols
by series key, X/Y, area and row. Native inspection retains that identity from
the last live point hit and distinguishes leaving the same item from leaving
blank space after its replacement. The regression checks normal blank-space
leave, stationary updates, retained inspection after replacement, a new point
hit and its normal leave, then actual chart exit. Separate unchanged-source
browser probes record these boundaries under `artifacts/scatter-chart-browser`.

The corrected native sequence passes Light/Dark at
`artifacts/smoke-scatter-chart-hover-identity`: all 64 source cases, eleven
stationary mutations, unchanged/replaced point leave behavior, re-hover, actual
exit and the following Sparkline checks. Both event traces contain eleven
settled mutations and a generated move with inspection preserved. The unchanged
browser eleven-update probe retains `x : 40y : 60` over blank space, its
ten-update probe returns to the original geometry and hides on point leave, and
both hide on chart exit. All 44 unit/resource checks and tokens pass.

The corrected regular 0.1.0 package is built from clean source commit
`ad2814e36140af64d9c035321c959f4d7860d57c`, matching its nuspec metadata.
SHA-256 is `5e14ae3752ba6c91f840a7a2a876df4868576f867f88ceca6e0ded3f606205fb`.
The fresh isolated x64 consumer passes Light/Dark runtime at
`artifacts/package-consumer-scatter-chart-interaction`; the fresh ARM64 consumer
build passes at `artifacts/package-consumer-scatter-chart-interaction-arm64`,
with runtime verification false. Both temporary restore/build workspaces are
removed. Both solution builds have zero warnings. The corrected runnable x64
catalog is `artifacts/catalog-scatter-chart-interaction`.

That full run passes every component check, including Dark hover and Sparkline,
but reaches 360 seconds during final Dark screenshots, before completing
Toggles/Foundation. It is not a complete pass. The PNG encoder experiment at
`artifacts/png-encoding-benchmark` preserves exact decoded pixels across nine
samples but offers only a small saving; PNG encoding remains unchanged.

Pointer preparation instead observes stable target screen bounds, dimensions
and ancestor viewport metrics over two Rendering transitions, replacing its
unconditional 100ms delay. Foreground, ownership, exact coordinates, final-hit
checks and the 360-second global deadline remain unchanged. The strengthened
Scatter/Sparkline sequence passes at
`artifacts/smoke-scatter-chart-stable-pointer`; its sixteen Scatter target gates
average about 42ms, with two stable transitions each. The complete focused Input
fixture also passes both themes at `artifacts/smoke-input-stable-pointer`.

The first complete stable-pointer run fails Dark Attention at an off-screen
ChildAction before queued scrolling completes. Stable off-screen geometry is
not readiness. The gate now requires its requested point to pass the same
native owned-window check during the two stable transitions; the final hit check
still runs. The focused Attention fixture passes both themes at
`artifacts/smoke-attention-visible-pointer`, including all four distant-child
scroll/return paths and exact 124-pixel masks. Fifty target gates pass; nine
wait for an initially unowned point to become visible. Maximum settling is
96.6803ms. Scatter/Sparkline also pass both themes at
`artifacts/smoke-scatter-chart-visible-pointer`. No library/package input changes.

The complete published catalog from `20820c6` passes Light/Dark in 325.934
seconds at `artifacts/smoke-scatter-chart-published-visible-pointer-full`, under
the unchanged 360-second limit. It retains 916 PNG captures, sixteen settled
final-capture reports, both exact 124-pixel Attention masks and eleven stationary
Scatter mutations per theme. All 544 pointer gates pass with at least two stable
transitions and owned final points; twenty-one wait for initially unowned
targets. Maximum settling is 167.0193ms. The final runnable catalog is
`artifacts/catalog-scatter-chart-visible-pointer`; the independently verified
library package remains the clean `ad2814e` package recorded above. Full raster,
motion, accessibility, scaling, collection performance and native ARM64
acceptance remain open; the inventory entry remains in progress.
