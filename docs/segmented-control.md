# Segmented Control native preview

Segmented Control is accepted for the 2026-10-29 x64 milestone in preview
`.44`. The current package passed 40 Light/Dark bound-selection cases and
eight rendered layouts, physical pointer/keyboard/focus and RTL selection in
both themes, and 16 native 150% no-clipping cases covering natural, narrow,
large-text and RTL layouts. Selection changes use immediate VisualState setters
with transitions disabled, so reduced motion needs no alternate animation.
The independent NuGet consumer compiled and ran. ARM64 builds, but its runtime
remains unverified. The focused reports are under the existing
`artifacts/current/context-menu-native-candidate/` directory.

`BleeckerSegmentedControl` represents one caller-bound value. It changes a value,
without hosting panels or navigation. The [pinned Sabella source](https://github.com/gaulatti/sabella/blob/f5ac479bfceaa7eab23c1716ac47420ac53dae25/Sources/Sabella/Buttons.swift)
uses generic Hashable values, option-value identity and an unconditional button
assignment. Selecting the selected option preserves the selection. Empty options,
an unmatched value and a selected disabled option do not change the caller's value.

Assign an `ObservableCollection<BleeckerSegmentOption>` to `Options`. Options are
immutable: replace an entry to update its label or disabled state. `Value` may be
a string, number, enum, value-equal record or null. Keep equality and hash codes
stable while a value is in use, and use unique values. Duplicate values fail
explicitly. Original option/value objects are retained; values are not converted
to strings or reconstructed from labels.

Bind `SelectedValue` using `Mode=TwoWay, UpdateSourceTrigger=PropertyChanged`.
`SelectionChanged` reports a dependency-property change. Unlike the existing
Radio Group's request event, activation updates the bound value directly, as the
source Binding does. `AutomationProperties.Name` labels the group; labels name
individual native actions. Windows exposes a single-selection Group and radio
SelectionItem semantics, with native button activation. Keyboard entry prefers
the enabled selection, then the first enabled option. Left/Right and Home/End
move among enabled options; direction respects RTL. Control+Arrow moves focus
without changing the value. The focused x64 catalog input checks below verify
the left-to-right paths; RTL routing still needs foreground verification.

The explicit source dimensions are medium Encode Sans 13, minimum button height
32, horizontal button padding 12, gap 2, frame inset 4 and radii 7/9. Theme tokens
supply sea/secondary text, card/transparent option backgrounds, muted frame at
.65 opacity and the border. Native disabled content dims to .55; the source
does not specify that platform opacity. Windows focus cues use the sea token.
The source's medium font file matches the bundled font byte for byte.

The Windows layout measures natural labels first. Under a smaller proposal,
shorter labels keep their width and longer labels wrap into the remaining space;
the 24-DIP horizontal padding remains intact. The frame clips overflow and grows
vertically for wrapped or enlarged text. This is a native text-layout mapping.
Apple HStack compression and raster geometry were not measured, and no Apple
pixel parity is claimed. The source has no scrolling or equal-width-fill option;
the Windows API adds neither. Broader sizing/customization review is outside
the current five-check milestone.

`SegmentedControlsFixture` demonstrates strings, typed enums, disabled options,
constrained width and enlarged text over fictional local values. The local
`0.1.0-segmented-control-v1-local` NuGet consumer and source host each pass
40 behavior cases, eight layouts/captures and eight selected-surface pixel probes.
Each auditor rejects 27 corrupted reports and the two actual wrong RTL captures.
The real catalog passes both themes, four native invocations and four selection
provider actions; its auditor rejects 16 corrupt reports. All 344 portable tests
pass. Those earlier x64/ARM64 catalog and independent consumer builds predate
the current package. Their broader input, visual and scaling questions were
reassessed against the five-check x64 milestone above; ARM64 runtime remains
unverified.
The source host passes 40 behavior cases and eight layout/capture cases. Each
selected surface is checked against physical root-space bounds and actual PNG
pixels. Two frozen captures from the earlier double-mirrored RTL candidate are
rejected even though its in-process report had claimed success.
Both fictional Monitoring and Editorial component demos remain required.
Narrator and OS contrast campaigns are excluded by [the library scope](library-scope.md).

The [v5 foreground Segmented Control checkpoint](evidence/components-v5-segmented-foreground-v1/checkpoint.json)
records SendInput pointer and keyboard observations in the extracted x64 catalog.
Clicking Detail selected it; clicking it again retained the value. Control+Left
moved the visible focus outline to Brief while Detail stayed selected, and Space
then selected Brief. Right selected Detail; another Right at the disabled Archive
end left Detail selected. Home selected Brief, End selected Detail, and Tab moved
to the selected Detail enum in the next group. This check does not establish RTL,
independent-consumer foreground input, display/text scaling, hardware-device
input or ARM64 runtime.

The [published v7 independent-consumer RTL checkpoint](evidence/segmented-control-v7-consumer-rtl-physical-v1/checkpoint.json)
closes that focused x64 input gap. A visible caller-owned group uses the unchanged
v7 NuGet library, with original fictional values and a disabled option. In both
Light and Dark, physical Left/Right followed RTL order, Home/End skipped the
disabled option, Control+Left moved focus without changing the selected value,
Space selected the focused option, and a pointer click restored the caller's
East value. Both independent processes exited with code zero. The retained
screenshots were inspected for visual order and readable theme paint. Run
`scripts/verify-segmented-consumer-desktop.ps1` against a fresh independent
consumer build of the pinned package to repeat this check. The library source
did not change. The earlier full pixel review and native ARM64 runtime remain
outside this x64 milestone.
