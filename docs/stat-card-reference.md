# Stat Card source contract and acceptance

For the 2026-10-29 x64 milestone, Stat Card passes all five required checks:
Light/Dark states, native keyboard/pointer/focus behavior, four native 150%
no-clipping cases, static behavior with reduced motion, and independent package
consumer compilation. The [inventory](parity-inventory.json) records the
matching `.24` package and catalog evidence. ARM64 remains build-only and
runtime-unverified. Exact raster comparisons are historical diagnostics, not
an acceptance gate.

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
contract and the control's live `Language`. For example, `istanbul` renders
as `İSTANBUL` under `tr-TR` and `ISTANBUL` under `en-US`; its caller-owned
`Title` is unchanged. The default group name follows the rendered title.
Value accepts scalar content, a native element or a ValueTemplate.
Icon/IconTemplate and Footer/FooterTemplate retain consumer content and native
child commands. BleeckerStatTrend owns observable Value, Direction and Label
dependency properties; Up, Down and Neutral select the source Lucide vector
and theme paint. Direction must be supplied before consumption; missing and
unknown directions fail explicitly. An explicit Trend suppresses Description, even if its text is
empty. Removing Trend restores a nonempty Description.

Raw Icon and Footer scalars follow the pinned conditional-rendering contract.
Null, false and an empty string create no slot. True creates an empty icon box
or footer wrapper. Numeric zero renders a plain `0` without the icon chrome or
footer border/padding. Other scalar content uses the normal wrapper. Text
collapses ASCII whitespace; a whitespace-only footer retains its empty wrapper.
Footer text uses 16/24px typography and wraps normally. Floating values use
canonical exponent formatting. An explicit IconTemplate or FooterTemplate owns
its data, including zero and false; that data is not coerced into raw slot text.

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
edge shapes, normal-motion timing, component theme/automation behavior,
display/text scaling and native ARM64 runtime remain open. The inventory entry must remain
in progress until its complete acceptance matrix is verified. This native
component has no backend, configuration or telemetry impact. The public wiki
remote is unavailable; documentation is maintained in this repository.

The implementation uses the repository's pinned Windows App SDK
[2.5.1](https://github.com/microsoft/WindowsAppSDK/releases/tag/v2.5.1), the
existing Card surface recipe and existing Sparkline renderer. A consumer value
template receives its original data without invoking that object's ToString;
the native fixture supplies a value whose ToString throws to verify the boundary.

## Scalar content and intrinsic text follow-up

`tests/Reference/StatCardContent.json` adds 36 Light/Dark captures from the same
unchanged source and dependencies. SHA-256:
`4870542c84f54dbf389f6f2b788d1feb66ff8d31f62c8a5cc0f2bf145ce297a3`.
They cover long unbreakable/mixed title, description and value content, raw
zero/number/string/Boolean/whitespace slots, wrapping footer text and exponent
values. Import with `scripts/derive-stat-card-content-reference.mjs`.

The first native run reproduces ten mismatches: mixed-value height 134.5px
instead of 164.5px; scalar footer heights and wrapping; zero icon width; Boolean
icon text; and exponent casing. Measuring wrapped text at its original column
width before arranging its overflowing paint fixes the mixed-value reflow.
The same layout now lets long titles and descriptions paint beyond their column
without escaping the Card surface. New desktop pixel gates prove both the
visible overflow and absence of exterior leakage. The icon-zero allocation uses
the captured 1/64px intrinsic boundary without widening the comparison tolerance.
Focused checks pass in both themes at `artifacts/smoke-stat-content-desktop`:
80 source specimens plus template-owned zero/false, floating/decimal scalar and
existing input/lifecycle regressions. Desktop checks confirm visible long text
beyond the lead column and zero leaked text pixels outside the Card. Both
architecture solution builds pass with zero warnings. All 43 unit regressions
and the pinned token check pass at `artifacts/tests-stat-content`. The complete
Light/Dark catalog passes at `artifacts/smoke-stat-content-full` with the
six-minute deadline unchanged. Native ARM64 runtime remains deferred.

Fresh package 0.1.0 from clean fix commit
`e6e65bab01ceb79da184bc2ea6b1b591768c2e69` has SHA-256
`73e47e343bf334bc88b0ce6dbef18b82502fe60635280ee4aa3032de4a959f70`.
The isolated x64 consumer passes both themes at
`artifacts/package-consumer-stat-content`, including raw zero/Boolean footer
behavior, zero icon chrome and template-owned false data. The ARM64 consumer
cross-build passes at `artifacts/package-consumer-stat-content-arm64`, with
runtimeVerified false. Both temporary caches are removed. The separately
published x64 catalog passes all 80 source cases and desktop clipping gates at
`artifacts/smoke-stat-content-published`; runnable output is
`artifacts/catalog-stat-content`. Glyph/raster, full content/override/motion,
component theme/automation behavior, scaling and native ARM64 acceptance remain open.

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

## Text baseline follow-up

The bundled font investigation retains the canonical typographic family names.
Native Medium/Semibold samples retain the same glyphs at Normal weight; explicit
Segoe UI and missing-family samples differ. Legacy aliases change baseline
metrics without improving glyph selection. No shared font resources changed.
`-Component font-audit` captures the five original bundled faces with these diagnostic
comparisons. Its result explicitly reports `captureOnly: true` and
`parityVerified: false`; capture completion is not acceptance evidence.
The later App Tab Bar preview adds Encode Sans Bold as a sixth bundled face,
with dedicated badge and windowless font verification.

`tests/Reference/StatCardTypography.json` freezes 16 additional rendered source
specimens from the unchanged pinned component, React and Recharts versions.
SHA-256 of the committed LF-normalized JSON:
`24bef09cee9c00039e4020915c8f4ca7f3b4ed5e07f02a1aecb5f949618d8593`.
The capture records actual custom-font selection through browser CDP, font-file
hashes, first baselines and every occupied pixel row in each text rectangle.
Import with `scripts/derive-stat-card-typography-reference.mjs`. Source captures
and the capture script are retained at `artifacts/stat-card-typography`.

The regression first reproduces 27 text-placement failures in Light. Titles,
body, trend and scalar-slot text sit about 1.5–1.8px too low; large values sit
2.28px too high. WinUI's BlockLineHeight scales the ascent, whereas the browser
uses rounded natural font ascent/descent and floored half-leading. The default
template now shifts each built-in text part to that measured baseline, snapping
once in device coordinates to avoid inconsistent half-pixel raster rounding.
The origin comes from WinUI's arranged `ActualOffset`, rather than a transformed
position with the previous paint shift subtracted; repeated updates otherwise
cross the rounding boundary. The pinned SDK's API documentation defines that
property as arrange output; see the [UIElement reference](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.actualoffset?view=windows-app-sdk-2.0).
Line boxes, intrinsic measurements, wrapping and consumer templates retain their
existing behavior. Font/layout changes queue a single rendering update;
unload cancels it and template replacement detaches old observers/transforms.

The desktop regression compares 54 baseline/occupied-row results across both
themes, including multiline title, description, trend label and footer, raw
icon/footer text, and anonymous zero slots. It compares every pixel in the
captured text rectangles with their background, without a color threshold or
pixel mask. Matching occupied rows verifies placement, not complete glyph
shaping, horizontal advances, antialiasing or raster equality. Those historical
raster differences are outside the current x64 milestone. The separate native
150% fixture now passes in Light and Dark; ARM64 runtime is unverified. Narrator
and OS contrast campaigns are excluded by the current [library scope](library-scope.md).

The registered gallery adds a wrapped title/summary with raw icon and footer
text. Native checks pass at `artifacts/smoke-stat-typography-stable`, including
all 80 existing cases and 40 additional lifecycle baseline checks. The initial
correction exposes a repeated-update failure (76px instead of 77px for a value)
at `artifacts/smoke-stat-typography-lifecycle`; reading arrange output removes
that feedback. Source and native screenshots remain available beside the JSON
reports. All 43 unit tests and token generation checks pass at
`artifacts/tests-stat-typography`.
Both x64 and ARM64 solution builds pass with zero warnings. The full Light/Dark
catalog passes at `artifacts/smoke-stat-typography-full` with the six-minute
deadline unchanged. The final font diagnostic captures are at
`artifacts/smoke-font-audit-final`; their result remains capture-only.

Fresh package 0.1.0 from clean implementation commit
`50a936c5e351b1cd80312a7a007948d304280b9f` has SHA-256
`66e4478291e33e5a9102742032ef2965c9c22e94671d33b916136e3e89513464`.
The isolated x64 consumer passes both themes, including first-baseline checks
for title, value and trend, at `artifacts/package-consumer-stat-typography`.
The isolated ARM64 consumer cross-build passes at
`artifacts/package-consumer-stat-typography-arm64`; runtimeVerified is false.
Both GUID-scoped temporary caches are removed. The separately published x64
catalog passes all 80 layout/content cases, 54 composed typography results,
40 typography lifecycle checks and existing native input gates at
`artifacts/smoke-stat-typography-published`. The runnable output is
`artifacts/catalog-stat-typography`, and the NuGet file is
`artifacts/packages-stat-typography/Gaulatti.Orchard.0.1.0.nupkg`.
The package README remains accurate; no public API or font resource changed.
Push-triggered Actions remain paused at the user's request; these are local
verification results and do not establish native ARM64 or hosted acceptance.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).

The [constrained rich-content regression](rich-content-layout.md) checks retained
rich value/icon/footer elements through title, summary and child-size changes
at 72/140/320 DIPs in both themes/directions. It does not establish full visual
or source wrapping acceptance.
