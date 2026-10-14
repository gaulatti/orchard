# Dashboard Grid and Span

`BleeckerDashboardGrid` and `BleeckerDashboardSpan` implement the pinned Bleecker
[`DashboardGrid`/`DashboardSpan`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/dashboard-grid.tsx).
Source SHA-256: `69e25939e26e9fdc1973b6fdfff2924ff3ecd3ab909810a62cefed2fc628dd16`.

```xml
<orchard:BleeckerDashboardGrid Columns="3">
    <orchard:BleeckerDashboardSpan Span="Two">
        <orchard:BleeckerCard Content="Consumer-owned primary content" />
    </orchard:BleeckerDashboardSpan>
    <orchard:BleeckerDashboardSpan>
        <orchard:BleeckerButton Content="Open review" Command="{Binding ReviewCommand}" />
    </orchard:BleeckerDashboardSpan>
    <orchard:BleeckerDashboardSpan Span="Full">
        <TextBlock Text="Consumer-owned full-row content" />
    </orchard:BleeckerDashboardSpan>
</orchard:BleeckerDashboardGrid>
```

Columns defaults to 0 (automatic). The source's automatic recipe uses 1 column
below a 640px viewport, 2 from 640px, 3 from 1024px and 4 from 1280px. Explicit
Columns 1 always uses one column; 2 stops at two; 3 stops at three. Values 4–6
use 1/2/3 columns at the first three ranges and the requested value from 1280px.
The viewport is the window's logical XamlRoot width, independently of the grid's
own width. Unknown Columns values fail instead of selecting another recipe.

Gap defaults to the shared 24px group token. A finite, nonnegative Gap override
corresponds to a consumer spacing override. Span uses the typed One, Two, Three,
Four or Full values. Numeric spans grow through the same viewport ranges;
Full references the entire explicit row. Numeric spans can create implicit
columns when they exceed the grid's explicit count. Full continues to reference
the explicit grid in that case. Placement follows sparse row order, retaining
holes when a later span does not fit. Collapsed children do not occupy cells.

Explicit columns use equal fractional tracks, with captured 1/64px allocation
boundaries. Implicit tracks consume native child content widths. Rows use their
tallest child, and span content starts at the top of the row. The wrapper's
minimum width is zero. Fixed-size consumer elements retain normal native
alignment; set HorizontalAlignment="Left" when a fixed-width element should
start at its logical leading edge. Consumer data templates, bindings and child
identity remain owned by the application. Span surface properties and template
replacement remain native. Neither layout adds a keyboard stop; child controls
retain focus, commands and native automation. No loading, selection, error or
motion behavior is defined by this source component.

The Dashboard Grids catalog entry includes fictional monitoring and editorial
compositions, composed from Orchard cards, progress, text and actions. It makes
no service requests. The focused and full native fixtures consume
`tests/Reference/DashboardGridLayout.json`: 96 captured Light/Dark cases from
the exact source and pinned React 19.2.4 dependencies. Capture SHA-256:
`51ee4a93676059aa885c415c9fbabd7a5e2328e2d61471d28d61a7d9be72323c`.
The cases cover all column choices around every viewport breakpoint, every span,
empty content, fractional widths, constrained widths, implicit content tracks,
full rows and RTL. Additional runtime gates cover live bindings, span/gap changes,
child visibility/reordering, remount, invalid constraints, child keyboard input
and disabled span behavior. Both hosted architectures and the independent NuGet
consumer are registered. Push-triggered hosted execution remains paused.

Focused x64 runtime checks pass in both themes at `artifacts/smoke-dashboard-grid`.
All 96 source geometry records pass the unchanged 0.001px comparisons, including
fractional tracks, implicit columns and RTL. Live column bindings, span and gap
changes, collapse/reordering, remount, invalid-value rejection and physical
forward/reverse Tab and Space through a child action pass. The rendered child
fills are checked against their source colors. Native/browser RTL captures and
the monitoring/editorial catalog composition were visually inspected. All 42
regressions and the pinned token check pass. Local checks use display/text scale
1 and the host's disabled animation effects; broader acceptance remains open.
The full local Light/Dark catalog passes at `artifacts/smoke-dashboard-grid-full`
with the existing six-minute deadline unchanged. Both architecture solution
builds pass with zero warnings; local ARM64 build evidence is a cross-build,
not native runtime acceptance.

Complete intrinsic sizing for arbitrary wrapping content/implicit tracks,
consumer overrides and content templates, text/glyph raster, actual contrast
schemes, Narrator, display/text scaling and native ARM64 acceptance remain open.
The inventory remains in progress. This native layout has no backend,
configuration or telemetry impact. The public wiki remote is unavailable;
documentation is maintained here.
