# Brand mark

Accepted for the 2026-10-29 x64 component milestone. The current published
`0.1.0-preview.20261029.6` package consumer passes 37 applicable Light/Dark
states, decorative input/focus semantics, static reduced-motion behavior and
compilation with zero warnings. The same package DLL passes native 150%
no-clipping checks at 16, 32, 64 and 96 DIPs in both themes, with eight frame
bounds checks and inspected captures in `artifacts/current/content-scale-brand-mark-v6.json`.
ARM64 catalog builds, but ARM64 runtime is unverified. Raster/pixel parity is
not a milestone gate.

`BleeckerBrandMark` implements the decorative view in pinned Sabella commit
`f5ac479bfceaa7eab23c1716ac47420ac53dae25`. It retains the original 1024-by-1024
grayscale/alpha PNG and its template-rendering intent. The default square is
64 DIPs, with uniform fit and 0.9 image opacity. Both themes use Orchard's
existing primary-text token, matching the pinned source palettes.

```xml
<orchard:BleeckerBrandMark Size="64"
    AutomationProperties.Name="Fictional studio logo" />
```

`Size` accepts finite, nonnegative values; zero gives an empty frame. Standard
native width constraints, padding, borders, background and opacity remain caller
properties. A fixed image frame does not grow when the control's font changes.
The default template requires a `SolidColorBrush` foreground, as documented by
the native [BitmapIcon renderer](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.bitmapicon?view=windows-app-sdk-2.0).
It observes brush color and opacity changes and updates its private renderer
without replacing the caller's brush or bindings. Observers detach on unload
and template replacement. A complete caller template owns its presentation;
the optional `PART_Mark` bitmap part opts into Orchard's tint handling.

This view has no action, selected state or default keyboard stop. Its automation
peer reports an image and preserves the caller's accessible name. Consumers
needing navigation use the library's link-bearing brand components. Focus and
selection variants are not fabricated for this decorative source contract.

The **Brand mark** catalog fixture demonstrates default, small, large, disabled
and live size/tint states. Source evidence is code plus the original template
image, not a claimed SwiftUI runtime screenshot. Acceptance requires a native
replay of frame sizes, theme tint and image alpha, live caller bindings, template
and unload/reload behavior, catalog registration, both architecture builds and
a fresh package consumer.

The current x64 source and fresh NuGet consumer each pass 37 states and 26,990
opaque-interior tint probes against the original alpha asset. Their state and
lifecycle reports and all 37 PNG pairs are identical. Two actual catalog theme
captures verify the registered fixture. Both architecture solutions and isolated
consumers build with the original PNG present and unchanged. The latest portable
run passes 413 tests after adding the missing public-control coverage entry;
32 corrupted reports and blank/wrong-tint frames are rejected.

The [published v7 fresh-consumer replay](evidence/brand-mark-v7-fresh-consumer-v1/checkpoint.json)
refreshes that focused x64 package boundary with the exact released Orchard DLL.
The hidden consumer exits zero and passes all 37 states, 26,990 opaque-interior
tint probes across 11,366,400 audited pixels, and the 32 corrupted-report plus
two bad-frame rejection controls. The original asset silhouette also matches
eight Light/Dark captures at 16, 32, 64 and 96 DIP: every source/native ink
pixel is within one device pixel of the other mask, with at least 97% overlap.
A stray edge pixel is rejected. The 64-DIP captures were visually reviewed in both
themes. This reuses the independently restored v7 consumer; it does not claim a
new package restore, catalog run, exact edge antialiasing, actual Windows
scaling or native ARM64 execution.

The [eight-size visual review](evidence/brand-mark-v7-visual-review-v1/checkpoint.json)
inspects the original asset beside all 16/32/64/96-DIP Light/Dark v7 consumer
captures. Its distinctive top and bottom arcs, central ring and right segment
remain visible, with no observed clipping or stray ink. Combined with the
existing tint and silhouette checks, this closes the applicable rendered asset
review. Native edge antialiasing need not be byte-identical to the source image;
actual Windows scaling and native ARM64 execution remain open.

The historical source and pixel reports above do not claim SwiftUI runtime or
native ARM64 execution. This decorative source has no action or selection
state; x64 milestone acceptance follows the five checks above.
