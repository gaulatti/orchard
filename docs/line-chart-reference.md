# Line Chart frozen contract and acceptance

The frozen [Line Chart source](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/line-chart.tsx)
has SHA-256 `673539e9a2c5a264fbfb6c35f09e7d956d2dbde9670f3ae6cf1888e752ec403a`.
Its package lock pins Recharts 3.8.1, victory-vendor 37.3.6, D3 Shape 3.2.0,
React/React DOM 19.2.4 and Tailwind 4.2.2. The upstream
[3.8.1 release](https://github.com/recharts/recharts/releases/tag/v3.8.1)
and installed version's source establish the version boundary; current Recharts
documentation describes later behavior and cannot replace it.

The consumer supplies record-shaped data, series and an X-axis data key. Height
defaults to 300. Grid and legend default to visible. The X/Y labels and numeric
tick/tooltip formatters are optional. Margins are top 10, right 20, bottom 25
when the X label is nonempty (otherwise 5), and left 20 when the Y label is
nonempty (otherwise 0). Recharts adds its 60px Y axis and 30px X axis inside
these margins. The legend's measured size also removes space from the plot.
At width 640 with the default two-series specimen, the plot runs X=60..620
and Y=10..225. Hiding the legend increases the plot bottom to 265. A narrow
240px chart wraps its legend and reduces the plot bottom to 201.

Each series uses its key/name unchanged, an explicit consumer color or its
index in the shared ten-color palette, a monotone curve, stroke width 2 and no
ordinary dots. Active dots have radius 5, stroke width 2 and the card-resource
stroke. Null/nonfinite coordinates divide the curve into separate segments;
connectNulls remains false. A single data row does not produce an ordinary
visible line or dot. The plot, legend, tooltip and active dot must preserve
explicit transparent paint rather than replacing it with a palette color.

Grid strokes use border/3-3 dashes and only horizontal lines. Axis tick text
uses secondary text at size 12; tick and axis lines are disabled. Numeric Y
domains/ticks use the pinned Recharts adaptive algorithm, including negative
and zero data. The observed default data maximum 310 produces 0/80/160/240/320
ticks. All-zero data produces 0/1/2/3/4. Category labels are measured and
filtered for spacing; long labels can remove earlier ticks. Native acceptance
must preserve the algorithm and text-measurement boundary rather than hardcode
these examples.

The default legend sorts entries by their displayed names. Its source line
icon is 14px with a 4px gap and 10px item margin. The wrapper adds 16px top
padding. Tooltip content uses the chart-specific theme, label and series names,
numeric formatter results when provided, and the source's empty/null behavior.
The chart is one keyboard stop with application semantics; Arrow navigation
updates the active category and tooltip. Native accessible names, values,
keyboard/pointer focus and ownership still require actual UIA/Narrator evidence.

## Evidence and implementation boundary

The unchanged React component is bundled with all 43 runtime dependency entries
checked against the frozen package lock. The complete frozen source tree—not
the earlier partial component tree—supplies the Tailwind stylesheet. That
distinction matters: a partial build omitted desert and hid the second series.
The corrected capture refuses missing default palette paint or axis ticks.
It uses local bundled fonts, Edge at raster scale 1, and reduced motion without
changing Windows preferences.

`artifacts/line-chart-browser/reference.json` retains 22 Light/Dark specimens:
default, grid/legend off, labeled/formatted axes, empty, single, negative, zero,
gaps, explicit/transparent colors, narrow and long-category data. The directory
also retains source snapshots, lock, capture/bundle scripts, DOM curves/ticks,
keyboard tooltip/active-dot markup and PNGs. The initial partial-styles captures
are superseded and do not establish parity.

`BleeckerMonotoneCurve` is an internal geometry foundation adapted from D3 Shape
3.2.0. It retains finite-coordinate segment boundaries, coincident-point
suppression, one-point closure, two-point lines and exact cubic control points.
`tests/Reference/LineCurve.json` contains 64 cases evaluated by the unmodified
pinned D3 context, including increasing/reversed/vertical/coincident inputs,
gaps, nonfinite values and varied curves. The regression compares native
commands and coordinates to that producer; all 33 regressions pass at
`artifacts/tests-line-curve`. The required D3 ISC notice is retained and packaged.
Both x64 and ARM64 solution builds pass with zero warnings. The local package
at `artifacts/packages-line-curve` contains the required D3 notice. This
checkpoint adds an unused internal numeric foundation and source evidence;
it does not change public control behavior or claim native chart rendering.

The native public Line Chart, complete data-key resolution, adaptive numeric
domain/ticks, text measurement/layout, live data/series/theme updates, legend,
tooltip effects, motion, keyboard/pointer/selection, accessibility, scaling,
performance, catalog and independent package/runtime checks remain required.
The geometry regression is not evidence that those layers are implemented or
accepted. The inventory stays in progress with every remaining boundary visible.
Orchard has no backend/metrics surface affected by this work. The public wiki
remote is unavailable; this in-repository contract is authoritative for the task.
