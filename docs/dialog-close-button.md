# Dialog Close Button

`BleeckerDialogCloseButton` is accepted in current source for the
[five-check x64 milestone](acceptance-contract.md). The published `.29` preview
predates this inventory update. Its matching package DLL passed the focused
extracted catalog's 16 Light/Dark states and physical pointer, Space, Enter and
focus path with animations disabled. The independent x64 consumer compiled and
ran; four native 150% default/large-icon and enabled/disabled cases kept the
root, border and icon inside the owner. ARM64 remains build-only and
runtime-unverified. Exact raster and normal-motion parity remain outside this
milestone's gates.

`BleeckerDialogCloseButton` derives from WinUI `Button`. Its default surface is
36 by 36 DIPs with a 7 DIP radius, a one DIP border and a centered Lucide X.
`IconSize` defaults to 18; zero and other finite non-negative values are valid.
The consumer supplies a localized accessible name and owns the action:

```xml
<orchard:BleeckerDialogCloseButton
    AutomationProperties.Name="Close preview"
    Command="{Binding RequestCloseCommand}"
    CommandParameter="{Binding Preview}" />
```

The button does not find or dismiss a parent window, dialog or overlay. A caller
can decline a close request, preserve unsaved edits, or ask for confirmation.
Native `Click`, `Command`, `CommandParameter`, `CanExecute`, keyboard activation
and the UI Automation Button/Invoke contract remain available. Supply a tooltip
separately when the application needs one. The catalog keeps its fictional
preview open to demonstrate consumer ownership.

The pinned Bleecker source is `src/components/dialog-close-button.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`a1614f004198702d1325af4dac2e2ab18fe6cf227574df2b1caf22a2e341c7e8`.
`DialogCloseButton.json` retains 16 actual browser states, all original PNGs,
20 motion sequences and two activation sequences. Source disabled buttons keep
full opacity. Pressing applies the individual CSS translate/scale immediately:
one DIP down and 0.97 scale. A held Space key applies that transform while
retaining resting paint when the pointer is elsewhere. Hover paint and outer
shadow use the source's 190ms premium curve; reduced motion snaps to the
requested paint. The dark hover
background/border use the dark `sand` token (`#1A2332`).

The implementation reuses the existing CSS color clocks and outer-shadow
renderer. Only running enabled transitions subscribe to native rendering.
Unload/template replacement releases subscriptions and compositor resources.
A replacement template without the source named parts owns its presentation.
The source paint path currently requires solid brushes; broader brush/template
customization remains part of acceptance work.

`-Component dialog-close -ClientSize 200,120` captures actual composed pixels
and exercises pointer release, Space/Enter, command parameter identity, disabled
invocation, template replacement and remounts. `verify-dialog-close-button.mjs`
compares the 16 source/native geometry and paint states, icon bounds, native
shadow graph, focus and input receipts. Numeric and raster evidence are separate;
exact pixels, normal native motion and broader overrides remain open in the
historical parity review; they do not alter the five-check milestone result.

The focused x64 runtime and all 16 numeric reference states pass in both themes.
Maximum geometry error is 0.000003814697265625 DIP; measured colors, alpha,
radius, border and icon stroke match. Real input produces exactly three Click
and command invocations (pointer, Space, Enter) per theme, retaining the original
parameter and mounted consumer content. The live-theme audit compares the
current hover/rest paint with its matching source case. Template replacement
and unload clear the actual previous compositor graph. Eleven damaged reports
are rejected. Six held-key pairs, four held-pointer pairs, four activation-key
pairs and 28 pointer presentation gates retain exact native receipts.
Full-frame comparison still finds 9,621 differing pixels across the
16 frames, with zero masks, resampling or tolerance; exact raster is unaccepted.
Both catalog architectures build without warnings or errors. Package validation
uses the local `0.1.0-dialog-close-input-local` candidate and fresh isolated consumers.
That package passes native x64 consumption in both themes, including its current
theme resource, icon updates, command/parameter identity and CanExecute boundary.
The separate ARM64 consumer builds; ARM64 execution remains unverified. Both
helpers remove their temporary sources and isolated package caches. The versioned
local evidence is `artifacts/current/dialog-close-checkpoint.json`.

The repository pins Windows App SDK 2.5.1, reviewed against its
[stable release notes](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/release-notes/windows-app-sdk-2-0#version-251)
and [native Button contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.button?view=windows-app-sdk-2.0).
The [component-library scope](library-scope.md) governs this work.
