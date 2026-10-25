# Segmented Control native preview

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
without changing the value. Physical keyboard routing remains unverified.

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
the Windows API adds neither. Full sizing/customization acceptance remains open.

`SegmentedControlsFixture` demonstrates strings, typed enums, disabled options,
constrained width and enlarged text over fictional local values. The local
`0.1.0-segmented-control-v1-local` NuGet consumer and source host each pass
40 behavior cases, eight layouts/captures and eight selected-surface pixel probes.
Each auditor rejects 27 corrupted reports and the two actual wrong RTL captures.
The real catalog passes both themes, four native invocations and four selection
provider actions; its auditor rejects 16 corrupt reports. All 344 portable tests
pass. x64/ARM64 catalog and independent consumer builds pass; ARM64 runtime,
physical input/focus and full visual/scaling/customization acceptance remain open.
The source host passes 40 behavior cases and eight layout/capture cases. Each
selected surface is checked against physical root-space bounds and actual PNG
pixels. Two frozen captures from the earlier double-mirrored RTL candidate are
rejected even though its in-process report had claimed success.
Both fictional Monitoring and Editorial component demos remain required.
Narrator and OS contrast campaigns are excluded by [the library scope](library-scope.md).
