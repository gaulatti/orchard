# Connected button groups

`BleeckerButtonGroup` joins consumer-owned native `ButtonBase` controls or Orchard `BleeckerButton` controls in a horizontal (default) or vertical strip. Set `AutomationProperties.Name` on the group to describe its purpose. Each child retains its own content, command, disabled state, automation peer and native keyboard focus. A group does not imply single or multiple selection; use selection controls for that purpose.

```xml
<orchard:BleeckerButtonGroup Orientation="Horizontal"
                            AutomationProperties.Name="Document actions">
    <Button Content="Review" Command="{Binding ReviewCommand}"
            Style="{StaticResource BleeckerSecondaryButtonStyle}" />
    <Button Content="Archive" Command="{Binding ArchiveCommand}"
            Style="{StaticResource BleeckerSecondaryButtonStyle}" />
</orchard:BleeckerButtonGroup>
```

The group owns child margins, corner radii, border thickness and horizontal/vertical stretch alignment while attached. It uses the canonical button radius, removes internal double borders and excludes collapsed children from the strip. Stretch alignment keeps wrapped labels connected and gives vertical actions the same width. The default `BleeckerButton` template stretches its native action to fill the allocated wrapper. Previous local values, style values and original bindings are restored when children leave the group on the next layout pass, or when the group unloads. Restored bindings read their producer's current values. Other child styling remains consumer-owned. Only `ButtonBase` or `BleeckerButton` children are accepted; invalid children and orientations fail during layout. Set enabled state on individual actions; the group is a `Panel` and has no `IsEnabled` property.

Horizontal groups constrain available widths proportionally when needed. With layout rounding enabled, shared edges are rounded using the mounted XAML root's rasterization scale; adjacent positions and widths use the same boundaries. Use wrapping `TextBlock` content for long labels; explicit child minimum/fixed widths remain the consumer's responsibility. Vertical groups stretch children across the group width. Normal Windows Tab navigation visits enabled children separately; arrow-key selection is not introduced for a group of independent actions.

Original runtime bindings are captured and detached before the group assigns
its temporary geometry. Two-way sources therefore retain caller values while
grouped. Removal or unload reconnects the same binding objects and reads their
current source values, including corner radius, border, margin and both
alignments. The native-v2 fixture checks all five two-way properties; older v1
reports retain their narrower one-way/local/style coverage.

The `0.1.0-label-ownership-v1-local` ownership follow-up passes 22 source,
20 independent-package and 22 extracted-catalog checks. The new two-way cases
first reproduced caller values being replaced with radius 7, border 1, margin
0 and Stretch alignments. They now verify all five source values remain intact
while grouped and the original binding objects reconnect on removal. All 18
compared group captures, including the prior layout preview, are byte-identical;
the previous gap/overlap repair remains intact. Evidence uses the
`artifacts/current/label-association-*` prefix.

The catalog registers horizontal and vertical fixtures in the main overview and in `ButtonsFixture.xaml`, including wrapping English, Japanese and Arabic labels, RTL, disabled actions and caller commands. The shared `ButtonGroupFixture` exercises native lifecycle and layout through the source catalog and an independent NuGet consumer. Run `scripts/verify-button-group.ps1` against a built host; its independent audit checks original appearance/alignment bindings and current producer values as well as contiguous native action bounds. Those automated probes remain hidden, without activation, focus or hardware input. Native ARM64 runtime, actual display/text scaling and full visual acceptance remain open. Narrator and OS contrast campaigns are excluded by the [library scope](library-scope.md).

The [v5 foreground Button Group checkpoint](evidence/components-v5-button-group-foreground-v1/checkpoint.json) records SendInput pointer and keyboard observations in the extracted x64 catalog, whose Orchard DLL matches the v5 NuGet consumer. A pointer click and Enter each invoked the horizontal Review command once; Tab visibly focused the two following horizontal actions and the RTL vertical action, whose Enter invoked the caller's Save command. The next Tab skipped its disabled sibling. Turning off the caller's enabled switch disabled Review, and clicking it left the command count at three. UI Automation's focused-element field stayed on the search edit despite the visible focus outlines and successful keyboard activations, so the checkpoint relies on the visible focus and command result, not that field. This focused catalog check does not establish independent-consumer foreground input, other visual/scaling states, hardware-device input or ARM64 runtime.

The 2026-10-24 local preview `0.1.0-button-group-v2-local` passes 20 source,
18 independent-consumer and 20 extracted-catalog checks. The hosts record 80
layout states and 22 captures in total, with 14 appearance restorations and
eight alignment restorations per host. All 12 common source/package/archive
image comparisons are byte-identical. Each host's audit rejects 26 corrupted
reports and the actual earlier one-pixel native overlap. The original source
also reproduced five/two-pixel gaps, a 39-pixel narrow vertical action and
nine-pixel unpainted top/bottom bands. Those reports and the intermediate
overlap remain retained under `artifacts/current/button-group-contract-*`.
Both solution architectures build, both clean package consumers build, and all
505 unit/text tests pass. The standalone Button probe retains its ten
style/template states and twelve spacing-binding states. These scoped results
do not accept the whole component or replace platform/scaling/input checks.
