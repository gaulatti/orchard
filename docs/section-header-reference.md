# Section Header

Section Header is accepted for the 2026-10-29 x64 component milestone. The
published `.12` catalog passes all 24 Light/Dark specimens and physical
forward/reverse Tab traversal around this passive header. The matching
independent package consumer compiles and runs; sixteen native 150% scale cases
keep visible text inside the control. The component has no animation, and the
catalog passed with Windows animations already disabled. The five checks are
recorded in [the inventory](parity-inventory.json). ARM64 runtime remains
unverified. Historical raster and OS contrast investigations below are not
release gates.

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
contrast mapping; component theme-resource verification remains required, while
OS contrast switching is excluded by the [current library scope](library-scope.md). The heading
uses the pinned WinUI `AutomationProperties.HeadingLevel` contract documented
by [Microsoft](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties.headinglevel?view=windows-app-sdk-2.0).

The frozen browser data in `tests/Reference/SectionHeaderLayout.json` contains
24 Light/Dark cases rendered from that exact source with its pinned React
19.2.4 dependencies and Orchard's bundled fonts. Capture SHA-256:
`fede6a8f4d06ce46822f05bd3bc74bd67af249ec7b9d3e2816b40cb74c0631ab`.
The capture now includes measured first-line summary baselines and the full
specimen's painted summary rows. A zero-size inline baseline marker is removed
before capture, and all 24 original geometry records remain unchanged.
`scripts/derive-section-header-reference.mjs` validates the source pin and
required measurements before copying a supplied frozen capture into the tests.
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

The summary retains its native 28px line boxes and uses a private measured-font
probe to position its first baseline. Rounding native ascent/descent to
integral layout values and flooring the first half-leading reproduces the
frozen summary baseline, 19px from the paragraph top. This is a model verified
against the captured cases, rather than a claim about browser internals.
Unadjusted WinUI places the baseline at
22.3168335px. The control computes the source position from live font metrics,
then arranges the text inside its unchanged paragraph allocation. It does not
apply a fixed three-pixel translation. The probe follows native font bindings,
has no input behavior and is omitted from the accessible content tree.

Focused x64 checks pass at `artifacts/smoke-section-header-baseline`. Both
composed Light/Dark captures now paint the full summary at rows 84–98, matching
the frozen browser; the previous native rows were 87–101. The runtime gate
reads the real window PNG and compares the first/last rows with the frozen
capture. Baselines are also checked for every nonempty summary, including
wrapped, RTL, multilingual and empty-title cases. The full local Light/Dark
catalog passes at `artifacts/smoke-section-header-baseline-full`; all 42
regression tests and the pinned token check pass. Native ARM64 validation of
this correction remains open while automatic Actions push runs are paused.

The fresh 0.1.0 package built from clean source commit
`c1b0dff8365bcf4e6c9b04bab80f33afcb465bd8` has SHA-256
`12099e23714392ebc52f760008489d214f49fd6c8c4ffd392b6730141e70d628`.
Its isolated x64 consumer passes both themes, including the corrected summary
baseline, at `artifacts/package-consumer-section-header-baseline`. The isolated
ARM64 consumer builds at `artifacts/package-consumer-section-header-baseline-arm64`;
this is a cross-build and records runtime verification as false. Both consumers
remove their own temporary caches. The separately published x64 catalog also
passes the focused runtime checks at
`artifacts/smoke-section-header-baseline-published` with display/text scale 1
and the host's animation effects disabled.

Earlier parity investigations tracked glyph/baseline raster details,
CSS `text-balance`/`text-pretty` and broader overrides. These are not gates
for the current x64 milestone; native 150% no-clipping now passes in both themes.
Narrator and OS contrast campaigns are excluded by the
[current library scope](library-scope.md). This passive control has
no backend, configuration or metrics impact. The public repository wiki remote
is unavailable; the contract is maintained here.
