# Isolated native rasterization

The hidden Content Scale fixture uses `DesktopWindowXamlSource.Initialize` with
its owned window and `SiteBridge.OverrideScale`. These APIs are present in the
pinned WinUI 2.3.9 and InteractiveExperiences 2.1.9 packages. See Microsoft's
[OverrideScale documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.content.desktopsitebridge.overridescale?view=windows-app-sdk-2.0).

The fixture checks actual `XamlRoot.RasterizationScale`, loaded component state
and physical PNG dimensions at 100%, 125%, 150% and 200% in both themes. It checks
horizontal and vertical Separators, the default fractional edge coverage, and
retained caller bindings that enable layout rounding. It also checks Switch
track, thumb and wrapped-label presentation and all six native Toggle styles.
The Toggle cases retain selected state, caller names, size-specific minimum
heights and visible paint across eight theme/scale frames. It uses no hardware
input, window activation or OS setting changes.

This establishes isolated native XAML scaling for the tested states. The physical
display remains at 100%; real monitor-DPI transitions and Windows text scaling
remain unverified.
Recovered historical ARM64 execution covers the pre-repair baseline only, not
the new fractional default.

The pinned browser Separator recipe supplies the source geometry and coverage
mask. The repaired native mask matches it. Flat color compositing differs by at
most one UNORM channel quantum; the separate strict comparison keeps its zero
tolerance and reports that difference as a failure. Neither result establishes
full-library visual acceptance.

Run the existing focused fixture with:

```powershell
./scripts/verify-content-scale.ps1 -AppDirectory <frozen-app> -Output <fresh-output>
```
