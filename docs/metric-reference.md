# Metric

`BleeckerMetric` implements frozen Bleecker `Metric` at
`a86059013cd59684ce452849278ea81e734f2066` (`metric.tsx` SHA-256
`2aeb702dc6a115a9b24ff62842d564a9fcd92f1069925b3e07929532daca47c7`).
The application owns its nullable numeric value, inline prefix/suffix and
culture. It has no requests, timers, commands or data collection of its own.

The observable acceptance contract is all four number formats with source
rounding/precision, empty missing/NaN values, signed zero/infinity, live updates,
retained consumer content, noninteractive text accessibility, source tabular
numerals and refined tracking, wrapping and theme-aware text. Full acceptance
also requires native raster, scaling, contrast, Narrator and ARM64 runtime
evidence; these remain open until separately verified.

```xml
<orchard:BleeckerMetric Value="1248" Language="en-US" Suffix=" reviews" />
<orchard:BleeckerMetric Value="0.125" Format="Percent" Decimals="2" />
<orchard:BleeckerMetric Value="1234.56" Format="Currency" Currency="EUR" />
<orchard:BleeckerMetric Value="1234567" Format="Compact" />
```

`Value` is `double?`. `Format` defaults to `Number`, `Currency` to `USD` and
`Decimals` to null (the format's source default). Explicit decimals fix both
minimum and maximum fractional digits from 0 through 100. Percent values are
fractions: `.125` becomes `12.5%` by default. Number defaults to at most three
fractional digits; currency uses the selected currency's minor units. Compact
notation follows the locale. Null and NaN contribute empty formatted text while
retaining prefix/suffix. Invalid precision and malformed three-letter currency
codes fail explicitly for nonempty values.

`Language` supplies the Windows culture. The formatter uses the Windows
`icuin.dll` C API, with explicit source half-up rounding. The actual host ICU
data can differ between Windows releases; the frozen source matrix is a
regression contract, not proof for every Windows/locale combination. See
[Microsoft's Windows ICU documentation](https://learn.microsoft.com/en-us/windows/win32/intl/international-components-for-unicode--icu-)
and the [ICU 72.1 number-format API](https://github.com/unicode-org/icu/blob/release-72-1/icu4c/source/i18n/unicode/unum.h).

`Prefix` and `Suffix` accept strings, numbers or original consumer UI elements.
Boolean/null slots are empty, as in React. `PrefixTemplate` and `SuffixTemplate`
render application data without converting it into a display string. Nested
controls retain their own input and focus behavior. The metric itself is a
non-tabbable text role; its default name combines scalar slots with the
formatted value. An explicit `AutomationProperties.Name` takes precedence.
Rich slot descendants retain their native accessibility; give rich-only
semantics an explicit consumer name when needed.

The template uses the shared Encode Sans font and theme text brush, tabular
numerals, `CharacterSpacing=-20`, and wrapping. `LineHeight=0` uses the source
1.5 ratio at the current font size; a positive value overrides it. Native
text-scaling support follows `IsTextScaleFactorEnabled`. The source has no
disabled paint variant, motion or error adornment; consumers own such content.

The registered **Metrics** fixture follows Stat cards and includes all formats,
missing values, disabled, constrained text and rich/template content. Focused
verification is `scripts/smoke-catalog.ps1 -Component metric`. The source
collector is `scripts/reference/capture-metric.cjs`; `tests/Reference/Metric.json`
contains 680 actual browser cases (85 inputs × four locales × two themes).

The x64 native matrix passes all 680 rendered text and retained accessibility
name comparisons. It also verifies source font/tracking/tabular digits/theme
paint, live value/precision bindings including bound null precision, explicit
consumer names, original rich content, real nested Space/pointer actions,
template replacement and unload/remount. Value and precision retain nullable
CLR types with boxed dependency-property storage: nullable-typed custom DPs
rejected primitive XAML bindings in the initial runtime regression.

Value updates retain the original prefix/suffix inline hosts, including a
consumer button's keyboard focus. Slot replacement affects only that slot.

All 121 unit tests and token checks pass. Seven corrupted native reports fail
the audit. Twelve raw whole-frame comparisons still show 12,286 differing
pixels with zero masks/resampling. Exact baseline/glyph/wrapping parity is open;
the browser canvas uses the pinned source's actual background variables.
Earlier rich-slot startup, small-glyph capture and nullable-binding failures
remain under `artifacts/metric-*` as diagnostics. No component is fully accepted.
