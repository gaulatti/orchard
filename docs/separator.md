# Separator

`BleeckerSeparator` divides adjacent content without adding input behavior.

```xml
<orchard:BleeckerSeparator />
<orchard:BleeckerSeparator Orientation="Vertical" Height="64"
    Decorative="False" AutomationProperties.Name="Workspace columns" />
```

The default is horizontal and decorative. The line is one logical pixel thick, using literal black at 5% opacity in light appearance and literal white at 10% in dark appearance. These values match the pinned Bleecker separator recipe rather than the general border token. Windows contrast mode uses opaque system WindowText. Background remains consumer-overridable; the control introduces no Windows separator template, margins, radii, icons or animation.

The long axis fills its finite available space. Give a vertical separator an explicit Height or place it in a height-constrained container. An unbounded long axis has no intrinsic length, so a separator with no available length requires a consumer constraint. Width/Height can override the default thickness. Orientation changes remeasure without replacing dimension bindings. Surrounding layout owns spacing.

The default style disables `UseLayoutRounding` so fractional native rasterization
keeps the one-logical-pixel edge coverage instead of snapping away or filling
the last device pixel. A caller value or binding can set it to `True` to opt
into native pixel snapping. The 100% geometry and brush recipe are unchanged.

Decorative separators are omitted from UI Automation control/content views. Set Decorative to false for a semantic boundary; its peer reports Separator and the current orientation, retaining a consumer's localized accessible name. Neither mode is a keyboard stop. The reference has no selected, hovered, pressed, disabled, loading, empty, error or motion variant; disabling does not dim the line. It contains no text or user content.

## Evidence and remaining acceptance

The catalog covers both orientations, both semantic modes, constrained widths and live orientation/semantics changes. Native x64 assertions verify geometry, rendered brush bindings, exact color/opacity and real automation-peer properties in light and dark themes. The historical `artifacts/smoke-separators` directory is no longer present; current qualification uses the retained reports below. The pinned Storybook horizontal fixture measures 320 × 1; its vertical fixture measures 1 × 64 with an explicit height, both using white at 10% in dark mode. The clean package consumer includes a semantic horizontal separator.

The current [library scope](library-scope.md) excludes Narrator and OS contrast
campaigns. Separator has no keyboard or pointer action and is not a tab stop;
its applicable input contract is the absence of an unwanted focus stop and the
decorative/semantic automation distinction. It has no text, empty/error content
or animation states. The published v7 catalog and exact-package independent
consumer have focused runtime smokes. Native ARM64 execution of the
fractional-edge default remains open.

The shared `--probe-hidden-separator` fixture runs against the source catalog or
an independent NuGet consumer. It checks the source's two orientations and both
semantic modes in both themes, text directions and enabled states. Its capture
auditor checks every pixel against the source line recipe and the surrounding
canvas, including absence of disabled dimming or extraneous paint. Live resize,
orientation/semantic changes, original caller brush/dimension bindings,
retemplating and remounting are checked separately. The native process remains
hidden and sends no input or system-setting changes. These checks do not imply
native ARM64 execution or a changed desktop DPI.

```powershell
./scripts/verify-separator.ps1 -AppDirectory <frozen-catalog-app> -Output <new-output-directory>
```

The current x64 source and independent NuGet hosts each pass 32 rendered states
and eight caller-binding, template and remount contracts with library
`758d2398…`. Each independent audit
checks all 2,048,000 captured pixels, including 6,144 line pixels; source brush
RGB/opacity is checked independently of compositing. The pixel bounds account
only for the two adjacent UNORM8 alpha values, with exact canvas color required
outside the line. The audit rejects ten damaged-report cases. The registered
catalog's four separators are mounted in both source-host themes. All 377 source
and 369 package isolation guards pass with zero focus or activation. Reports
and captures are retained in `artifacts/current/separator-source-native-v2` and
`artifacts/current/separator-package-native-v1`; the first test attempt's strict
double-equality failure is retained separately, and the repaired assertion uses
the existing native fixture's numeric tolerance. The production separator and
published NuGet bytes are unchanged. The independent consumer restores exactly
`0.1.0-file-input-styles-v1-local` and verifies its package hash before running;
it contains no repository project reference. The Status Badge metadata preview
now packages this probe in the catalog ZIP. Source, exact NuGet and archive
hosts also rerun all 32 states, eight caller contracts and ten negative cases
with library `87755ce3…`. The Separator implementation itself is unchanged.

The earlier source catalog and independent consumer compilation passed for x64
and ARM64. Those builds cover the pre-repair identity; the isolated fractional
native checks below establish a separate scope.

The [current preview recheck](evidence/separator-preview-v2/checkpoint.json)
uses the exact Orchard DLL published in `0.1.0-preview.20261027.2`. Both its x64
catalog and independent NuGet consumer pass the existing 32 native states,
eight caller contracts, full-line pixel audit and isolation checks. Both pass
the isolated 100%, 125%, 150% and 200% rasterization audit in Light and Dark.
The strict source-image diagnostic still differs at 4,128 pixels by at most one
channel value; coverage masks match. This refreshes the source/package evidence
without proving a physical monitor-DPI change or native ARM64 execution.
Separator has no text, so system text scaling does not apply. Its component-scale
contract is satisfied by the tested native `XamlRoot` scales and physical PNG
dimensions across both orientations and themes. A physical monitor move remains
untested, but adds no Separator-owned transition path: the component has no DPI
listener or scale-dependent state. The Separator implementation, style and
brush tokens used by this test remain unchanged in the published v7 source.
This does not establish native ARM64 execution, so Separator remains in progress.

The [published v7 catalog foreground check](evidence/separator-v7-catalog-foreground-v1/checkpoint.json)
opens the extracted x64 catalog with the exact released library DLL. In system
Dark and explicit Light, four divider lines render in the registered fixture.
UI Automation exposes the named semantic horizontal and vertical separators,
while the decorative lines are omitted from its control tree. The catalog
window closes and its process ends; no exit code was captured. This is a
focused visible smoke, not a replacement for native ARM64 checks.

The [exact v7 NuGet-consumer qualification](evidence/theme-toggle-v7-lifecycle-v1/checkpoint.json)
also includes Separator. Its [full consumer result](evidence/theme-toggle-v7-lifecycle-v1/full-consumer-result.json)
passes with the released `Orchard.dll` hash in a separate package-reference x64
application. The Light/Dark loop asserts that the packaged semantic horizontal
Separator is one logical pixel tall, has positive width and exposes the native
Separator automation role. This is a focused current-package smoke; the earlier
32-state and eight-contract v2 source/package reports remain the broader
Separator-specific evidence. Neither x64 result establishes native ARM64 runtime.

## Isolated native rasterization

`--probe-hidden-content-scale` uses an owned `DesktopWindowXamlSource` and its
`SiteBridge.OverrideScale`. It checks actual `XamlRoot.RasterizationScale` values
and physical PNG dimensions at 100%, 125%, 150% and 200% in both themes. It covers
both orientations and caller snapping bindings; it changes no monitor, Windows
text-scale or accessibility setting and never shows or activates its window.
The pinned browser reference records the matching stateless Separator CSS recipe
at those device scales.

The published `87755ce3…` baseline rounds the last device pixel at fractional
scale. Its actual 125% default misses the source's partial edge coverage. A
caller `UseLayoutRounding=False` restores that coverage, establishing the repair
through the existing WinUI property rather than a custom renderer. Existing
zero-tolerance color diagnostics retain their results independently of source
coverage and native behavior checks.

```powershell
./scripts/verify-content-scale.ps1 -AppDirectory <frozen-catalog-app> -Output <new-output-directory>
```

Recovered historical ARM64 reports prove a genuine Arm64 process/OS and the same
WinUI/CoreLib binaries for the unchanged pre-repair Separator baseline. The new
fractional default is outside that historical scope. Native ARM64 execution of
this repair and physical monitor-DPI transitions remain unverified. The latter
is a broader integration observation, not a component-specific gate for this
stateless non-text control. The entry remains in progress. See the
[native rasterization scope](native-rasterization.md).
