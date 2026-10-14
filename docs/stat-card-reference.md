# Stat Card source contract and acceptance

`BleeckerStatCard` implements the pinned Bleecker
[`StatCard`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/stat-card.tsx).
Source SHA-256: `7085e7d88f1109a0d1ca93885fc0de01aa735fd7ca973de1bdb63cac333ffb06`.
The frozen capture uses React 19.2.4 and Recharts 3.8.1 with runtime dependencies
checked against the source package lock.

```xml
<orchard:BleeckerStatCard Title="Reviews" Value="1,248"
    SparklineData="{Binding History}" SparklineDataKey="value">
    <orchard:BleeckerStatCard.Trend>
        <orchard:BleeckerStatTrend Value="+12%" Direction="Up" Label="this week" />
    </orchard:BleeckerStatCard.Trend>
</orchard:BleeckerStatCard>
```

Title and Description retain consumer strings. Rendering collapses CSS ASCII
whitespace and uppercases Title using Orchard's existing Unicode casing
contract. Value accepts scalar content, a native element or a ValueTemplate.
Icon/IconTemplate and Footer/FooterTemplate retain consumer content and native
child commands. BleeckerStatTrend owns observable Value, Direction and Label
dependency properties; Up, Down and Neutral select the source Lucide vector
and theme paint. Direction must be supplied before consumption; missing and
unknown directions fail explicitly. An explicit Trend suppresses Description, even if its text is
empty. Removing Trend restores a nonempty Description.

The surface reuses the default Card recipe: 24px padding, 1px border and 12px
corners, with 20px between the main header, optional history and footer. Header
content has an 8px vertical gap. Title uses Encode Sans Semibold 11/16.5px with
0.09em tracking; the default scalar value uses Semibold 30/30px and -0.02em
tracking. Trend and description use Encode Sans 14/20px. The optional icon is
40px with 9px corners and a 24px header gap. Trend icons are 14px and retain the
pinned Lucide paths. Up uses the source Emerald 600/400 paints, Down uses
Terracotta and Neutral uses secondary text. Contrast resources use system
window/text colors; actual Windows contrast acceptance remains separate.

SparklineData is a nullable observable collection of consumer-owned records.
A nonnull collection and nonempty SparklineDataKey mount a 48px Sparkline with
4px negative horizontal margins. An empty collection still reserves this area;
an empty key hides it. Color is optional and explicit transparency is retained.
The source ShowArea option follows the [Sparkline contract](sparkline-reference.md).
The footer has a 1px top border and 16px top padding. The outer card adds no
keyboard stop; interactive value/footer children and history keep native input.

`tests/Reference/StatCardLayout.json` contains 44 rendered Light/Dark cases:
default, description, all trend directions, optional trend label, icon, footer,
complete composition, history/data/key conditions, explicit history paint,
empty/zero values, whitespace, multilingual/RTL, constrained widths and custom
value content. Capture SHA-256:
`297e5ed6a665e4be492204b733bd342a9f1ecb99727081bc4f9eb2e5add8abc3`.
`scripts/derive-stat-card-reference.mjs` validates the pinned source and versions
before importing the capture. The catalog fixture compares native geometry,
text and paint, and exercises live bindings, trend changes, normal child input,
disabled inheritance, remount and template replacement. Focused x64 checks pass
in both themes at `artifacts/smoke-stat-card-contract`. All 44 source specimens
pass their defined geometry/text/color comparisons, including live consumer
bindings, native Tab/Right/Space/reverse Tab and custom value/control templates.
The local environment uses display/text scale 1 and disabled animation effects.
Both solution architecture builds pass with zero warnings. All 43 unit
regressions and the pinned token check pass at `artifacts/tests-stat-card-contract`.
The ARM64 result is a cross-build on the Intel host, not native runtime evidence.
The complete local Light/Dark catalog passes at
`artifacts/smoke-stat-card-full-settled` within the existing six-minute deadline.
The initial full run exposed the recurring stationary Input cursor observation
race; its stricter rendered-state regression and evidence are described in
[runtime evidence](runtime-evidence.md). No product cursor code changed.

The narrow source composition intentionally paints an unbreakable value and
trend-label text beyond its 66px header text column. The first native render
clipped them there. Native value/trend panels now retain the column allocation
while arranging the overflowing children without an intermediate layout clip.
Composed desktop pixel gates in both themes prove actual visible text beyond
that column. Captures and pixel counts are retained as
`stat-card-narrow-composed-{Light,Dark}.png` and
`stat-card-overflow-{Light,Dark}.json` in the focused output. Native/browser full
and narrow captures were visually inspected; complete raster acceptance remains
open. Keyboard-only setup moves the physical pointer to the preceding action so
the documented chart pointer channel cannot override keyboard inspection.
The initial published focused run fails history inspection. The fixture now
waits for the source's first focused value before Right and records exactly one
native key press/release delivered to the actual chart. Those stronger checks
pass in both themes at `artifacts/smoke-stat-card-key-ack`. A future failure
retains focus, records, series and interaction-channel diagnostics. The initial
failure lacks that state, so its exact cause is unproven.

The first RTL native comparison records a 0.015625px trend-value allocation
residual. RTL checks retain the source's trailing anchor and native 8px sibling
gap; the reports retain the actual positions. Exact text advances and glyph
raster remain an acceptance gap. Geometry tolerance remains 0.001px for the
compared anchors, dimensions and gaps.

Full glyph/curve/surface raster, all consumer templates/overrides and ReactNode
edge shapes, normal-motion timing, actual contrast, Narrator, display/text
scaling and native ARM64 runtime remain open. The inventory entry must remain
in progress until its complete acceptance matrix is verified. This native
component has no backend, configuration or telemetry impact. The public wiki
remote is unavailable; documentation is maintained in this repository.

The implementation uses the repository's pinned Windows App SDK
[2.5.1](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1), the
existing Card surface recipe and existing Sparkline renderer. A consumer value
template receives its original data without invoking that object's ToString;
the native fixture supplies a value whose ToString throws to verify the boundary.

Fresh NuGet 0.1.0 from clean implementation commit
`b3de3487e3cdfa9cffb0e2fc06bce5acbf767b27` has SHA-256
`4ab803d6cda7defabe4c243b9bc3572893d713467ea7613be4da47888c450f61`.
The isolated x64 consumer passes both themes at
`artifacts/package-consumer-stat-card`, verifying independent resources,
typography/layout, live trend changes, restored description and original
history collection identity. The isolated ARM64 consumer cross-build passes
at `artifacts/package-consumer-stat-card-arm64`; runtimeVerified remains false.
Both GUID-scoped temporary caches are removed. The separately published x64
catalog passes the 44 focused specimens, desktop overflow gates and acknowledged
native history input at `artifacts/smoke-stat-card-published-key-ack`. Its Right
records retain exactly one down/up at the actual chart in each theme. Native
ARM64/hosted validation remains deferred while push-triggered Actions are paused.
The same published output also passes the complete Light/Dark catalog at
`artifacts/smoke-stat-card-published-full`, with the six-minute deadline
unchanged. This includes the first-focus/key-delivery gates alongside all
existing catalog fixtures. The runnable output is
`artifacts/catalog-stat-card-key-ack` and the NuGet file is
`artifacts/packages-stat-card/Gaulatti.Orchard.0.1.0.nupkg`.
