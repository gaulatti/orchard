# Loading overlay

`BleeckerLoadingOverlay` follows the frozen Bleecker `LoadingOverlay` at
`a86059013cd59684ce452849278ea81e734f2066`, source SHA-256
`9b4721d0cb08b99284bf0b59dc16ba2448cf8b065a926882f37b1099cea033da`.
The consumer owns `IsVisible` and `Label`. The default label is `Loading...`;
null and empty labels omit the paragraph, while whitespace retains its 12-DIP
gap and a zero-height paragraph.

```xml
<orchard:BleeckerLoadingOverlay IsVisible="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
    Label="Preparing the local preview" />
```

Mount the control in the application's XAML tree. Its popup covers the current
XamlRoot rather than the control's parent. The default card uses a 48-DIP large
spinner, 28/24-DIP padding, a one-DIP border, radius 12 and the existing canonical
overlay shadows. The backdrop applies a two-DIP Gaussian blur and the background
color at 72% opacity. The label uses Encode Sans Medium at 14/20 DIPs. Unbroken
labels keep their minimum content width and may extend beyond a narrow viewport.

The source is a pointer barrier with no dismissal action, automatic focus or
focus trap. Opening it retains existing keyboard focus; a focused underlying
action can still receive Space and Enter. Applications needing modal dialog
semantics should use a dialog contract. This component has no cancellation
command or automatic timeout. The catalog's three-second example demonstrates
caller-controlled visibility.

The template exposes `PART_Popup`, `PART_Portal`, `PART_Backdrop`, `PART_Tint`,
`PART_Card`, `PART_Body`, `PART_Spinner`, `PART_LabelHost` and `PART_Label`. Default brushes live in
`Themes/LoadingOverlay.xaml`. Windows contrast colors replace translucent tint
and border colors and suppress backdrop blur; actual contrast and Narrator
verification remains required. Spinner motion follows the existing loading
spinner's system animation preference.

`tests/Reference/LoadingOverlay.json` records 60 unmodified browser cases across
1024×900, 640×480 and 320×240 in both themes, plus six real keyboard/pointer
interaction probes. `scripts/reference/capture-loading-overlay.cjs` reproduces
them from the pinned React component with recorded font/tool hashes. Snapshot
animations are paused at time zero; they do not establish motion parity.

The catalog's `--smoke-loading-overlay` mode measures the matching viewport
cases against the source with the existing 0.05-DIP geometry/baseline bound and
captures the actual owned native client. It records partial measurements before
capture/input failures. The separate backdrop diagnostic proved that the pinned
WinUI Popup samples the application behind it and retains existing focus.
All 60 native cases pass geometry and 42 first-line baselines: maximum errors
are 0.018189 and 0.000031 DIPs, respectively. Real Space/Enter, pointer blocking,
Escape, root focus clearing and subsequent Tab/hide behavior pass in both
themes at all three sizes. Four negative audits reject shifted baselines,
shifted viewports, retained pointer focus and missing physical pointer receipts.

Pointer focus clearing temporarily makes the XAML root focusable. Its next real
focus move restores the original `IsTabStop` value or binding. Opening and hiding
the overlay do not themselves take focus. The catalog source harness hides its
navigation while measuring the isolated source viewport, then restores it.

Exact unmasked pixel comparison remains required. The native fixture uses real
native underlying buttons, whose own rendering also differs from the browser
fixture. The checkerboard phase matches the source. No masks, resampling or
pixel tolerances are applied.
The published 60-case comparison retains 3,041,308 differing pixels and
38,254,621 total RGBA channel error across the complete source/native frames.
Every frame differs. These totals include underlying fixture differences.

Clean release source is `d4067eaedf0ab6c9c0889c07c8c9c2389df6f44f`.
Both architecture builds and all 113 unit tests pass. The full x64 catalog
passes 1,130 captures in 463.9042732 seconds, retaining every prior capture
and 174 protected glyph hashes. The independent x64 NuGet consumer passes
both themes, viewport coverage, its original keyboard action, consumer label,
spinner automation and omitted-label layout. The ARM64 consumer builds only;
both isolated caches are removed. See [release evidence](native-validation-checkpoint.md).

Acceptance remains open: exact raster parity, live theme/resource
updates, resize/reparent/template lifecycle, original popup stacking, scaling,
contrast, Narrator, motion and native ARM64
runtime. A successful build or the presence of the catalog example is not
component acceptance.
