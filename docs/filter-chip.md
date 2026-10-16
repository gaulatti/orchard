# Filter chips

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
Both command and event run when both are registered. Callbacks never remove
models or clear the collection automatically; the consumer owns those changes.
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

`FilterChipsFixture` registers read-only, removable, multilingual and wrapping
examples. Its consumer handlers demonstrate explicit collection removal/reset.
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

Acceptance remains in progress. Exact source/native raster parity, enabled-motion
timelines and cancellation, complete typography/RTL/resource customization,
text/display scaling, contrast schemes, Narrator, external UI Automation and
native ARM64 runtime must be evidenced before acceptance. ARM64 cross-compilation
does not establish native runtime behavior.
