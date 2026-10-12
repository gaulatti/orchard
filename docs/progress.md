# Progress display

`BleeckerProgress` is a preview of the pinned Bleecker `src/components/progress.tsx`
at commit `a86059013cd59684ce452849278ea81e734f2066`.

```xml
<orchard:BleeckerProgress Value="65" />
<orchard:BleeckerProgress Value="145" Max="200" ProgressSize="Large"
    Variant="Warning" ShowLabel="True" AutomationProperties.Name="Upload" />
```

Value defaults to 0 and Max to 100. The displayed fraction is `Value / Max`,
clamped to 0–100%, while the consumer values remain unchanged. The optional
percentage label rounds halves upward as in the reference. The control exposes
a ProgressBar role and read-only RangeValue pattern with the raw consumer value,
minimum 0 and consumer maximum. It is not a tab stop and does not add disabled
dimming. Supply an accessible name for the operation being represented.

Small, Medium and Large tracks are 6, 8 and 12px high. Default and Success use
sea in light mode and accent-blue in dark mode; Warning uses sunset/accent-gold,
and Destructive uses terracotta. The sand track is 15% opaque in light mode and
20% in dark mode. Both track and fill have pill ends. The label has bundled
Encode Sans medium at 12px, a 16px line box, tabular numerals, a minimum 40px
width and a 12px gap. Theme resources switch the palette; contrast mode uses
system colors.

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

The layout regression resizes a labeled progress display to 200px and 360px,
checks proportional fill and consumer range retention, replaces its template
visuals, and verifies that fill and label are restored. These checks are pending
runtime verification at this checkpoint.

Acceptance remains open: rendered native/browser comparison, exact pill edges
and typography, enabled/interrupted motion, live appearance and unload/reload,
invalid or zero maxima/nonfinite values, out-of-range native UIA
conformance, contrast/Narrator, isolated scaling and ARM64 runtime. Do not change
the developer's desktop-wide settings to perform scaling checks.
