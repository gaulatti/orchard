# Section Header

`BleeckerSectionHeader` implements the pinned Bleecker
[`SectionHeader`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/section-header.tsx).
The source SHA-256 is
`df2e51474c906760bf20e369256590872f07a13a4e29dca60ee5235bbee638bf`.

```xml
<orchard:BleeckerSectionHeader Title="Editorial" Eyebrow="Workspace"
    Description="Review fictional stories before publication." />
```

Title, Eyebrow and Description are consumer-owned strings and dependency
properties. Empty/null optional strings collapse; whitespace remains content.
The title uses bundled Encode Sans Semibold at 28px/35px with −0.02em tracking.
It exposes a native level-two heading peer. The optional eyebrow reuses
`BleeckerEyebrow` and its existing uppercase/accent contract. The summary uses
Libre Franklin at 16px/28px, secondary text color and a 672px maximum width.
The normal eyebrow/title gap is 14px; title/summary spacing is 12px.

Empty titles retain the source's spacing behavior: eyebrow-only content is
30px high, an eyebrow with summary is 70px high at one summary line, and a
standalone summary has no internal leading gap. Entirely empty content has
zero intrinsic height. Surrounding layout supplies the width and placement.
The source does not define variants, commands, selection, disabled dimming,
loading or motion. The control adds no keyboard stop. RTL follows the native
reading direction; source strings are preserved.

The default template consumes Background, BorderBrush, BorderThickness,
CornerRadius, Padding and title Foreground. A consumer replacement template
owns its layout and semantics. Native system-aware text resources supply the
contrast mapping; actual contrast acceptance remains required. The heading
uses the pinned WinUI `AutomationProperties.HeadingLevel` contract documented
by [Microsoft](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties.headinglevel?view=windows-app-sdk-2.0).

The frozen browser data in `tests/Reference/SectionHeaderLayout.json` contains
24 Light/Dark cases rendered from that exact source with its pinned React
19.2.4 dependencies and Orchard's bundled fonts. Capture SHA-256:
`f56e112018ba0ea3bec63168dad8595c61623b5854a5f241cd1cd6445b11e4cb`.
The catalog fixture checks source geometry, palette, typography and real
heading peers, then exercises consumer binding updates, optional content
removal, disabled layout, remount, template replacement and physical
forward/reverse Tab traversal. It is registered in focused and full catalog
modes, both hosted architecture jobs and the independent package consumer.

Focused x64 runtime verification passes all 24 frozen layout cases at
`artifacts/smoke-section-header`, including source heights/spacing, colors,
real level-two heading peers, consumer binding changes, remount/template
replacement and physical forward/reverse Tab traversal. Summary allocation is
compared through its native layout slot; TextBlock's reported ActualWidth
retains the formatted text width inside that slot. The full x64 Light/Dark
catalog also passes at `artifacts/smoke-section-header-full`; all 42 regression
tests pass. ARM64 and independent package validation are recorded by their
respective build/runtime gates.
Both x64 and ARM64 solution builds pass with zero warnings. This local ARM64
result is a cross-build; native ARM64 runtime evidence comes from the hosted
job and is not inferred from compilation.
A fresh independent x64 consumer passes both themes with package 0.1.0,
SHA-256 `73de59638258cdd8da38a58f0a0a7f8702d14613d4ae2af4855e414d2fb9ae34`,
at `artifacts/package-consumer-section-header`. It verifies bundled fonts,
the real level-two heading peer, optional-content collapse and live title
updates, then removes its own isolated cache.

The fixture also records an invisible, non-interactive native font-metrics
probe and captures the actual composed client in each theme, with the header's
screen bounds. Focused checks pass at `artifacts/smoke-section-header-metrics`.
For the 16px summary in a 28px line box, the native baseline is 22.3168335px;
its natural line box is 19.3920002px with baseline 15.4560003px. Centering that
natural line box gives 19.7600002px, consistent with the observed roughly 3px
summary ink displacement. This is diagnostic evidence; no hardcoded offset or
baseline correction has been applied.

Exact glyph/baseline
raster comparison, CSS `text-balance`/`text-pretty`, the complete override and
multilingual constraint matrix, actual contrast schemes, Narrator and
display/text scaling remain open. The inventory entry remains in progress;
these gaps are not approved platform substitutions. This passive control has
no backend, configuration or metrics impact. The public repository wiki remote
is unavailable; the contract is maintained here.
