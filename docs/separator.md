# Separator

`BleeckerSeparator` divides adjacent content without adding input behavior.

```xml
<orchard:BleeckerSeparator />
<orchard:BleeckerSeparator Orientation="Vertical" Height="64"
    Decorative="False" AutomationProperties.Name="Workspace columns" />
```

The default is horizontal and decorative. The line is one logical pixel thick, using literal black at 5% opacity in light appearance and literal white at 10% in dark appearance. These values match the pinned Bleecker separator recipe rather than the general border token. Windows contrast mode uses opaque system WindowText. Background remains consumer-overridable; the control introduces no Windows separator template, margins, radii, icons or animation.

The long axis fills its finite available space. Give a vertical separator an explicit Height or place it in a height-constrained container. An unbounded long axis has no intrinsic length, so a separator with no available length requires a consumer constraint. Width/Height can override the default thickness. Orientation changes remeasure without replacing dimension bindings. Surrounding layout owns spacing.

Decorative separators are omitted from UI Automation control/content views. Set Decorative to false for a semantic boundary; its peer reports Separator and the current orientation, retaining a consumer's localized accessible name. Neither mode is a keyboard stop. The reference has no selected, hovered, pressed, disabled, loading, empty, error or motion variant; disabling does not dim the line. It contains no text or user content.

## Evidence and remaining acceptance

The catalog covers both orientations, both semantic modes, constrained widths and live orientation/semantics changes. Native x64 assertions verify geometry, rendered brush bindings, exact color/opacity and real automation-peer properties in light and dark themes. The historical `artifacts/smoke-separators` directory is no longer present; current qualification uses the retained reports below. The pinned Storybook horizontal fixture measures 320 × 1; its vertical fixture measures 1 × 64 with an explicit height, both using white at 10% in dark mode. The clean package consumer includes a semantic horizontal separator.

The current [library scope](library-scope.md) excludes Narrator and OS contrast
campaigns. Separator has no keyboard or pointer action and is not a tab stop;
its applicable input contract is the absence of an unwanted focus stop and the
decorative/semantic automation distinction. It has no text, empty/error content
or animation states. Fractional display scaling, current native ARM64 execution
and complete visual/runtime qualification remain open.

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
it contains no repository project reference. The new probe command is available
from a source build and is not yet in the published catalog ZIP. Packaging it
is deferred to the next catalog milestone; no new NuGet version is needed for
this validation-only change.

Source catalog and independent consumer compilation also pass for x64 and ARM64.
This distinguishes architecture build coverage from the x64 native runs above;
current native ARM64 execution and actual fractional DPI remain unverified.
