# Brand mark

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

These checks do not claim a SwiftUI runtime capture, complete source-edge raster
parity, physical input/focus behavior or native ARM64 runtime. The decorative
source has no built-in action or selection state. The entry remains in progress
until its applicable visual and platform acceptance is established.
