# BleeckerField

`BleeckerField` composes a label, one consumer-owned native `Control`, optional
header action, description and error. It implements the pinned Bleecker Field
recipe; it does not own a form, model, validation service or persistence.

```xml
<orchard:BleeckerField Label="Coverage title" Required="True"
    Description="Use a concise title." Error="{Binding TitleError}">
    <orchard:BleeckerInput Text="{Binding Title, Mode=TwoWay}" />
</orchard:BleeckerField>
```

The child retains its instance, text/value, bindings, commands, enabled state
and visual validation. Clicking the label focuses an enabled child. Interactive
elements inside a rich label keep their own input. The required star is
decorative: `Required` does not set the child's native required property. A
truthy `Error` temporarily sets `AutomationProperties.IsDataValidForForm` to
false without setting `BleeckerInput.IsError` or changing its border.
The Input and Textarea native automation peers combine that temporary Field
error with their own error/explicit-validity state; clearing Field's error
does not erase a child's remaining invalid state.

Existing child automation IDs, including an explicitly empty ID, are preserved.
Otherwise Field supplies a stable ID for that Field instance. An existing
`AutomationProperties.LabeledBy` wins; otherwise Field supplies its label.
Existing `DescribedBy` references remain first, followed by Field's description
and error nodes. Removing a message removes only its owned reference. Replacing
the child/template or unloading releases owned metadata. A temporarily
overridden validity binding is restored to its current producer value, without
writing into the producer. Consumer metadata changes are retained when they no
longer equal the owned temporary value; a same-value scalar assignment produces
no WinUI property-change notification.

`Label`, `Action`, `Description` and `Error` accept scalars or original UIElements;
models require the corresponding `LabelTemplate`, `ActionTemplate`,
`DescriptionTemplate` or `ErrorTemplate`. The child itself must be one `Control`.
The default control template requires a `Grid` named `PART_Root`. Setting the
template to null detaches presentation and owned metadata; restoring it retains
the generated identity. Arbitrary replacement templates must provide that part.

`Action` takes precedence over `Optional` when supplied, including `false`, an
empty string or zero. A false action renders nothing and hides Optional; zero
renders `0`. Description/error presence follows source truthiness: zero, false
and empty strings are absent; true supplies an empty node. Empty message nodes
keep their semantic relationships and the source's collapsed margin behavior.

The source spacing is 10 DIP vertically and 16 DIP between header items. The
label uses Encode Sans Medium at 13/19.5, Optional uses Libre Franklin at
11/16.5, description uses Libre Franklin at 12/20, and error uses Encode Sans at
12/20. Plain labels/messages use the shared Unicode shaping and wrapping path.
Original rich controls/templates retain their content and formatting. Direct,
unwrapped TextBlocks with an explicit line height and Orchard font assets also
use reference run advances and baseline placement. Other rich presentations
retain native intrinsic metrics. Exact parity for those presentations remains
an acceptance gap.

`IsTextScaleFactorEnabled` reaches Field's owned label/message text. The child
retains normal WinUI inheritance and explicit-value precedence; Field does not
write its text-scale property. The native check toggles the component preference
and verifies an explicit child value without changing system settings. Actual
larger display/text scale remains unverified.

The catalog's Fields page demonstrates required, optional, action, error and
narrow layouts using fictional local content. Its focused native check captures
44 source states and exercises real label focus, editing and action input in
both themes. A separate UIA3 COM client reads names, IDs, required/valid state
and ordered relationships from the owned process. It does not launch Narrator
or alter Windows settings. Binding restoration, template detach/replacement,
unload/remount and child replacement are also exercised. The independent NuGet
consumer loads Field through the packaged resources.

The frozen browser reference retains the original 44 PNGs and source, client,
collector, dependency and font hashes. Its rich specimen's actual browser font
receipt resolves `strong` to the pinned Encode Sans SemiBold face, despite the
computed bolder weight being 700. The native specimen uses that same actual
face. Full-frame raster comparisons use no masks, resampling or pixel tolerance.

The current preview passes all 44 measured geometry/typography/paint states with
zero geometry error, 52 external UIA snapshots and 19 negative audit cases.
Required-marker font boxes use the actual allocated layout slot and pinned face
ascent/descent; the raw TextBlock line boxes are retained separately. Link button
state/command/template regressions pass in both themes. The local NuGet package
passes a fresh x64 consumer runtime and an ARM64 consumer build. These checks
verify the measured/runtime contracts; full-frame pixels still differ.

Acceptance remains in progress: exact raster parity, broader rich templates and
consumer overrides, actual display/text scaling, and native ARM64 execution.
See [runtime evidence](runtime-evidence.md) for the current candidate's measured
results and [library scope](library-scope.md) for the governing scope.
