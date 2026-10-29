# Progress display

`BleeckerProgress` is a preview of the pinned Bleecker `src/components/progress.tsx`
at commit `a86059013cd59684ce452849278ea81e734f2066`.

Progress is accepted for the 2026-10-29 x64 component milestone. The focused
catalog run passed all 12 size/variant combinations in Light and Dark, passive
read-only keyboard/pointer/focus semantics, and immediate value changes while
Windows animations were already disabled. The independent
`0.1.0-preview.20261029.9` package consumer compiled without warnings, passed
its range checks and passed 24 native 150% no-clipping cases across both themes.
The reports are `artifacts/current/context-menu-native-candidate/progress-milestone.json`,
`progress-range-v9.json` and `progress-content-scale-v9.json` in that directory.
ARM64 runtime remains unverified for this milestone.

```xml
<orchard:BleeckerProgress Value="65" />
<orchard:BleeckerProgress Value="145" Max="200" ProgressSize="Large"
    Variant="Warning" ShowLabel="True" AutomationProperties.Name="Upload" />
```

Value defaults to 0 and Max to 100. Value must be finite; Max must be finite and
greater than zero. Invalid CLR assignments throw `ArgumentOutOfRangeException`
before replacing the current value. Dependency-property assignments are checked
by the same update path and fail explicitly; callers must correct an invalid
assigned value before using the control again. Validation never replaces a
caller binding or substitutes an invented range.

The displayed fraction is `Value / Max`,
clamped to 0–100%, while the consumer values remain unchanged. The optional
percentage label rounds halves upward as in the reference. The control exposes
a ProgressBar role and read-only RangeValue pattern with its displayed value
clamped between 0 and the consumer maximum. The raw public Value remains
unchanged. Changing Max also updates the exposed bounded value. This follows
the [Windows RangeValue convention](https://learn.microsoft.com/en-us/windows/win32/winauto/uiauto-implementingrangevalue).
It is not a tab stop and does not add disabled
dimming. Supply an accessible name for the operation being represented.

Small, Medium and Large tracks are 6, 8 and 12px high. Default and Success use
sea in light mode and accent-blue in dark mode; Warning uses sunset/accent-gold,
and Destructive uses terracotta. The sand track is 15% opaque in light mode and
20% in dark mode. Both track and fill have pill ends. The label has bundled
Encode Sans medium at 12px, a 16px line box, tabular numerals, a minimum 40px
width and a 12px gap. Theme resources switch the palette; contrast mode uses
system colors.

WinUI does not normalize a `999` radius like CSS `rounded-full`: the first CI
captures at `artifacts/ci-progress-09c9bf6` show long elliptical caps. Track radii
now use half the track height, and fill radii use half the smaller rendered
dimension. A rendering observer follows that radius only while width is animated
and detaches on completion, interruption, unload and template replacement.
The native pixel regression samples the top/bottom straight body at quarter
width, where the earlier elliptical fill had transparent pixels. This fix is
verified in x64 CI run `38267087734` at commit `4c76d59`; all 12 combinations
passed in light and dark themes. Corrected captures and measurements are retained
under `artifacts/ci-progress-4c76d59/smoke`.

Value changes request the 240ms overlay transition with the premium cubic
curve. Reduced motion settles immediately. Resize and template replacement
recompute the destination; unload stops the storyboard and detaches preference
observation. The component displays determinate progress only, matching the
source's default value path.

The catalog registers all 12 size/variant combinations, optional labels, disabled
and RTL examples. Native regression checks cover track/fill geometry, palette,
consumer values across negative/over-maximum inputs, a custom maximum, label
rounding, a named read-only UIA range and rejected UIA writes. The independent
NuGet consumer also creates the control.

The rendered pinned browser matrix covers all 12 combinations in both themes.
At 320px row width and 65% value, each row is 16px high, the track is 268px
wide, the label is 40px wide at x=280px, and the browser fill is 174.1875px
wide. Reference measurements and captures are retained at
`artifacts/progress-reference-light.json` / `progress-reference-dark.json` and
the corresponding PNG paths. Native measurements are emitted per theme as
`progress-measurements-Light.json` / `progress-measurements-Dark.json` in the
smoke output. Native geometry assertions cover row allocation; exact raster
edges and glyphs remain separate acceptance checks.

The first allocation check incorrectly compared `TextBlock.ActualWidth` with the
CSS label box. In x64 CI the native row was 320×16, track 268 and label x=280,
but the TextBlock reported 21.366px for its formatted `65%` text. The regression
now checks the label's layout slot minus margins against the required 40px
allocation, and records both the allocated width and the reported text width.

The layout regression resizes a labeled progress display to 200px and 360px,
checks proportional fill and consumer range retention, replaces its template
visuals, and verifies that fill and label are restored. These checks and the
independent package consumer passed in that x64 CI run.

The captures also exposed Windows layout rounding: a mathematically 174.2px
fill was reported as 174px, while the rendered browser measured 174.1875px
(CSS layout-unit quantization). The default style now disables layout rounding
within Progress, preserving fractional fill dimensions. The regression requires
the rendered width to agree with the calculated percentage within 0.001px.
This fractional-width correction passed x64 and native ARM64 catalog/consumer
CI at `5412997`, run `38268516314`. The ARM64 fill measures
174.199996948px, and the captured pill bodies pass in all 12 combinations and
both themes. See [runtime evidence](runtime-evidence.md).

The historical full-parity review still has rendered native/browser comparison,
exact pill edges and typography, enabled/interrupted motion, and ARM64 runtime
checks open. These are not gates for the current x64 milestone. Narrator and OS
contrast campaigns are excluded by the current
[library scope](library-scope.md). Do not change
the developer's desktop-wide settings to perform scaling checks.

## Focused range regression

`--probe-hidden-progress-range` checks 14 valid/out-of-range states in both
themes, 32 invalid CLR/dependency-property assignments, eight live binding
updates and four invalid-bound-data recovery contracts. It checks the actual
read-only automation provider, rendered fill geometry, percentage labels,
retained raw finite values and binding expressions. The baseline library exposes
four out-of-range provider values and accepts all 32 invalid inputs, including
`NaN%` for zero or NaN maxima. Native results and delivery identities are listed
in the [range preview notes](releases/2026-10-26-progress-range-preview.md).

```powershell
./scripts/verify-progress-range.ps1 -AppDirectory <frozen-app> -Output <fresh-output>
```

These are hidden x64 checks without hardware input or OS changes. The focused
range results do not establish enabled-motion, physical scaling, native ARM64
execution or complete visual acceptance.

The consolidated v7 extracted x64 catalog and its independent NuGet consumer
both passed this same focused probe with identical Orchard library hash
`ff3e8379…`. Each host passed 14 range states, 32 invalid-input cases,
eight live binding contracts, four invalid-bound-data recovery contracts and
93 audit guards. The retained reports are under
`artifacts/current/components-v7-progress-range-catalog-x64` and
`artifacts/current/components-v7-progress-range-consumer-x64`. These results
verify the shipped x64 bytes for this range contract. The newer `.8` package
consumer and focused catalog checks above establish the current x64 milestone;
the earlier v7 result is retained as historical range evidence.
