# Filter chips

Filter Chip and Filter Group are accepted on x64 under the
[five-check milestone](acceptance-contract.md). The source catalog passes 40
Light/Dark cases at 320 and 640 DIPs plus four physical keyboard, pointer and
focus runs. Those runs also verify immediate hover and exit paint with
animations already disabled. The independent `.34` package consumer compiles,
runs the native actions and passes six native 150% standalone, grouped and RTL
no-clipping cases. ARM64 runtime is unverified. The empty-group capture fixture
now checks its expected solid canvas instead of rejecting it as blank. Older
diagnostics below describe earlier work and do not add milestone gates.

The shared native action now invokes Enter on key-down through its Button
Invoke pattern, preserving Click/UIA guards and avoiding an extra key-up
invocation or pressed scale. Space and pointer activation still invoke on
release. Its unscaled transparent template surface retains the press target
while the visible remove artwork shrinks. The keyboard outline is drawn in
the same scaled visual with source color/radius and a Windows contrast brush.
These corrections are exercised by the Search Input state matrix because it
uses this same action implementation; FilterChip's own geometry, native actions
and retained external UIA patterns remain separate regression gates.

`BleeckerFilterChip`, `BleeckerFilterGroup` and observable `BleeckerFilter`
implement the frozen Bleecker FilterChip/FilterGroup source at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`e80e6fc2932baffa9c0471c5d1ff58d27593f6538070d7128a21ea6a84967525`.

```xml
<orchard:BleeckerFilterChip Label="Status" Value="Draft"
    RemoveCommand="{x:Bind RemoveFilter}" />
<orchard:BleeckerFilterGroup x:Name="ActiveFilters"
    ClearAllCommand="{x:Bind ClearFilters}" />
```

`Label` is required and defaults to the empty string; `Value` is optional.
An empty/null value adds no value span or gap. Rendering collapses CSS
whitespace without changing the model strings. The native remove button
exists only when `RemoveCommand` or `RemoveRequested` is supplied. It uses
the original command parameter and follows `CanExecute` and inherited
`IsEnabled`. Its default accessible name is `Remove {Label} filter`, using
the original label. Attached `AutomationProperties.Name` or `LabeledBy`
on a standalone chip supplies the consumer's accessible override.

`Filters` is a non-null `ObservableCollection<BleeckerFilter>`. Each model
exposes `Label`, `Value`, `RemoveCommand`, `RemoveCommandParameter` and
`RemoveRequested`. Metadata changes update the existing action. Reordering
retains model/action identity, visual order, tab order and focused actions.
The optional group action uses `ClearAllCommand`, `ClearAllCommandParameter`,
`ClearAllRequested` and localizable `ClearAllText` (default `Clear all`).
When both are registered, the command runs before the event. Group remove-event
dispatch requires active model membership; a command that removes its model
ends that model's action. Callbacks never remove models or clear the collection
automatically; the consumer owns those changes.
Empty groups have zero height and no native actions.

The source layout has a 28-DIP minimum chip height, 10/4-DIP horizontal/vertical
padding, one-DIP border, six-DIP radius and six-DIP internal gaps. Encode Sans
Medium labels and Semibold values use 12/18 typography. The 16-DIP remove
button contains the source's 12-DIP Lucide X and two-DIP padding. Groups wrap
with eight-DIP gaps and center the 12/16 Clear all text within each row.
Text shrinks toward minimum content widths; long unbreakable content may
overflow, following the source's intrinsic sizing. Theme brushes define the
light/dark border, text, fills and hover colors. Resolved palette brush changes
are observed until unload/template replacement. Hover colors use 190-ms premium
easing; the remove press scale is immediately 0.9, matching CSS individual scale.
Reduced motion and Windows contrast disable color animation. Contrast resources
use system colors; live contrast acceptance still requires runtime evidence.

Caller font family, size, weight, style, stretch, spacing, reading direction and
text-scale preference flow through the rendered text and intrinsic/baseline
measurements. A live style/stretch change recomputes group wrapping and Clear all
width while retaining existing native actions. The `filter-chip-typography` mode
exercises bundled and Segoe UI fonts, genuine italics, 20-DIP text, tracking, RTL,
constrained wrapping, multilingual content, restoration and unload/remount.
Segoe UI supplies a distinct italic face for detecting stale measurement caches.
These application typography overrides do not establish actual Windows text or
display scaling acceptance; those checks remain open.
The native regression records 20 live phases and 100 independently measured text
paragraphs. Its four corruption checks reject an incorrect style, stale width,
missing baseline or reversed RTL order. Before the fix, changing Segoe UI to
italic left Clear all measured at 90.1600 DIPs while its displayed face required
92.3267 DIPs. Rendered text, width probes and baseline probes now use the same
style/stretch values. Run with `-Component filter-chip-typography -ClientSize 640,480`
and inspect `scripts/verify-filter-chip-typography.mjs` for the evidence boundary.

`filter-chip-accessibility` verifies the published Control view through a separate
Microsoft UI Automation client restricted to the catalog's window/process and
fixture scope. Its 46 snapshots cover default/live/bound/linked names, explicit
name priority, native Button roles, Invoke, disabled commands/groups, localized
Clear all, focus-preserving reorder, template replacement, collection replacement,
empty groups and unload/remount. Read-only label/value text stays exposed without
remove actions. Native Tab/Shift+Tab traversal retains press/release ownership;
external Invoke reaches original callbacks/command parameters and rejects disabled
actions. The consumer still owns collection mutation. Five corrupted evidence
sets must be rejected by `scripts/test-filter-chip-accessibility-audit.mjs`.
Standard native before/after buttons provide reference roles and disabled Invoke
behavior. Fixture-only automation IDs identify targets inside the owned scope.
The probe source is shipped beside the catalog and compiled into the requested
smoke output using the Windows .NET Framework compiler, following the existing
Loading Overlay probe workflow. Normal catalog usage does not launch it.
`filter-chip-accessibility-session` keeps one external MTA client and its original
Invoke patterns alive across mutations. Name and effective enabled-state changes
raise native UIA property events. Names come from the standard button peer, so
default labels, explicit/bound names, linked text, linked automation metadata and
Clear all follow the same resolution as fresh queries. Linked-label layout and
metadata observers are removed on detach; ordinary explicit names need no layout
observer. Automation peers remain lazy for hidden/read-only controls.

The persistent client verifies event delivery before invoking retained live,
disabled, replaced, removed and unloaded actions. A native reference event checks
the client transport; component events originate in the library. It registers
and removes handlers on the same external MTA thread, following
[Microsoft's UIA threading guidance](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-threading-issues).
Broader component accessibility events and additional stale-client workflows
remain separate acceptance checks. Narrator testing is excluded by the
[current library scope](library-scope.md).

`FilterChipsFixture` registers read-only, removable, multilingual and wrapping
examples. Its consumer handlers demonstrate explicit collection removal/reset.
The demo queues collection changes after native event routing; the focused
regression separately verifies synchronous consumer removal through UI Automation.
The focused `filter-chip` smoke mode compares 40 cases across light/dark and
320/640-DIP clients, including empty labels/values, long wrapping and empty groups.
`scripts/reference/capture-filter-chip.cjs` renders the unchanged frozen source
with pinned React/CSS/fonts/Edge, Playwright and Lucide lock integrity checks.
`tests/Reference/FilterChip.json` records unchanged PNG hashes, boxes and baselines.
`scripts/verify-filter-chip.mjs` checks native geometry at the existing 0.05-DIP
limit, baseline positions, accessible names, lifecycle evidence and physical input
receipts. `scripts/test-filter-chip-audit.mjs` must reject corrupted geometry,
names, lifecycle records and input receipts.

```powershell
./scripts/smoke-catalog.ps1 -Component filter-chip -ClientSize 320,480 -Output artifacts/filter-chip-320
node scripts/verify-filter-chip.mjs --native artifacts/filter-chip-320 --width 320 --output artifacts/filter-chip-320-audit.json
```

`scripts/compare-filter-chip-pixels.mjs` compares all unmodified client pixels,
validates source PNG hashes and records differences without masks or resampling.
Its completed comparison is separate from acceptance; differing pixels keep
`sourcePixelParityVerified` false.

Acceptance remains in progress. Exact source/native raster parity, enabled-motion
timelines and cancellation, complete typography/RTL/resource customization,
text/display scaling, component theme resources, broader UI Automation and
native ARM64 runtime must be evidenced before acceptance. ARM64 cross-compilation
does not establish native runtime behavior.
