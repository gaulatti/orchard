# Collection Filters

This inventory entry follows frozen Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`,
`src/components/collection-filters.tsx` (SHA-256
`9b220e110dde6293e081ff555c6b69c9a0e34fee352e5b46888646b418774d69`).
It composes reusable controls and performs no data loading, sorting, persistence
or service calls. Monitoring and Editorial remain fictional-data component demos.

Acceptance requires these observable behaviors:

- The caller retains the original filter dictionary, option objects, sort state
  and bindings. Changes emit a fresh requested dictionary or sort value; accepting
  or declining a request remains the caller's choice. Programmatic updates emit
  no requests. Boolean false counts as an active filter and displays No.
- Select changes, boolean All/Yes/No, date selection/clearing, individual removal
  and Clear all preserve unrelated keys and original boolean/string types.
  Unknown keys and option values remain visible. Dates follow the source's local
  date parsing, rollover, serialization and locale display rules.
- The filter toggle owns expansion state. DefaultExpanded seeds that state once;
  later default changes do not reset it. With no filter or sort options the whole
  control occupies no space. Active chips remain available while collapsed.
- Sort field requests preserve direction, and direction requests preserve field.
  The descending glyph rotation and action name follow the current caller value.
  Empty sort options remove the sort controls.
- Header groups, active chips and the field grid follow the pinned viewport
  breakpoints, wrap behavior, tokens, fonts and Light/Dark palettes. Constrained
  widths, RTL, native metadata, keyboard/pointer/focus, removal, template changes
  and retained child controls require runtime evidence.
- A catalog fixture, source reference, regression coverage, both architecture
  builds and an independent package consumer must exercise the public control.
  Exact visual and platform acceptance require their own evidence.
- Owned surface and border brushes must preserve the pinned browser's RGBA8
  encoding of the original CSS tokens in both themes. Transparent DOM swatches
  bind that encoding separately from complete component captures. A palette
  correction must retain the 48 layouts and all controlled-input histories;
  full PNG comparisons keep zero tolerance, with no masks or resampling.
- Numeric field names follow the pinned source's object-key order: canonical
  array-index keys precede other names in ascending numeric order; other names
  retain their insertion order. Chips and emitted dictionaries use that order
  while preserving the caller's original dictionary and values. Boundary names
  such as `01`, `-0` and `4294967295` remain ordinary string keys. Real removal
  and re-selection must preserve this behavior for accepted and declined requests.
  The active count uses the source's tabular numerals, including two-digit counts.

The key-order boundary follows
[ECMAScript ordinary own-property ordering](https://tc39.es/ecma262/#sec-ordinaryownpropertykeys)
and is checked against the pinned component itself, including the last valid
array index and non-canonical numeric spellings.

`BleeckerCollectionFilters` exposes `FilterOptions`, `SortOptions`,
`CurrentFilters`, `CurrentSort` and `DefaultExpanded`. Filter values use
`BleeckerCollectionFilterValue`, preserving a boolean or string without coercion.
Each `BleeckerCollectionFilterOption` supplies a field, label, type and optional
original `BleeckerSelectOption` sequence. Sort options supply field/label pairs;
`BleeckerSortState` is an immutable caller-owned object.

```csharp
var filters = new BleeckerCollectionFilters
{
    DefaultExpanded = true,
    FilterOptions = [new("featured", "Featured", BleeckerCollectionFilterType.Boolean)],
    SortOptions = [new("title", "Title")],
    CurrentFilters = new Dictionary<string, BleeckerCollectionFilterValue>
    {
        ["featured"] = false
    },
    CurrentSort = new("title", BleeckerSortOrder.Ascending)
};
filters.FiltersRequested += (_, request) => viewModel.Filters = request.Filters;
filters.SortRequested += (_, request) => viewModel.Sort = request.Sort;
// Bind CurrentFilters/CurrentSort to those view-model properties to accept updates.
```

Requests carry new immutable dictionary wrappers or sort objects. The component
does not assign either caller property. Observable option sequences and option
metadata refresh their presentations; plain collection changes require an
observable notification or replacement property value. Private editors and chips
are retained by original option/field identity. Collapsing deactivates and removes
the field presentations, closing their popups; obsolete or hidden actions cannot
emit requests. Template replacement retains expansion and caller bindings.
An empty options pair removes spacing, navigation metadata and actionable peers.

The field grid follows the native root viewport at 768/1024 DIPs, independently
of the control's own width. Header padding and the Sort caption change at 640.
Tracks use cumulative 1/64-DIP boundaries. The composed Date Picker stretches
while retaining its source 200-DIP trigger minimum. Source date rollover, empty
and malformed values, boolean false, unknown keys/values and missing Select
options are covered. Windows formatter-added LRM separators are omitted from
the generated chip string; original caller strings remain intact.

The source reference contains 50 full PNGs, eight real browser interaction
sequences and 16 date probes. Eight source-bound regressions bring the portable
suite to 282 passing tests. The x64 native run matches all 3,408 geometry checks
and all 44 browser input/state samples, including accepted/declined filtering,
sorting and numeric-key removal/re-selection. Trusted native mouse presses and physical releases prove self-removing
chip clicks. Default changes, bindings, stale/template/hidden peers and disabled
actions are checked separately. Both catalog architectures build cleanly.
The owned palette is generated from 22 transparent DOM swatches of the original
CSS token declarations in the pinned browser. Its RGBA8 alpha lives in the brush
color, avoiding a second fractional-opacity conversion. The native audit checks
420 actual brushes. The palette correction reduced differing pixels from 3,403,374 to 630,700
(81.5% fewer) without changing layout, callbacks or the shared controls' palettes.
Regenerate with `scripts/generate-collection-filters-palette.mjs`; `--check`
verifies the source swatch hashes and generated file in CI.

The numeric regression first failed against the old model. Canonical indices
now sort before other field names in both chips and new request dictionaries;
the original dictionary retains its values and insertion order. The native
matrix also exposed proportional count digits; the count now uses the source's
tabular numerals, matching the two-digit toggle width exactly. A named editor
selector fixes the initial harness ambiguity with the hidden sort editor.

The `0.1.0-collection-filters-v4-local` NuGet package builds fresh independent
x64 and ARM64 consumers. Its x64 hidden runtime passes 50 source layouts,
1,944 geometry checks, 420 brush checks, 16 ownership checks and 558 isolation
guards without activation or focus. The foreground and hidden auditors reject
44 and 31 damaged receipts. Package DLL, six loose XAML files and compiled PRI are hash-bound to
the tested build. Temporary consumer source/feed caches are removed.

```powershell
./scripts/smoke-catalog.ps1 -Executable artifacts/catalog/Orchard.Catalog.exe -Component collection-filters -ExpectedArchitecture X64 -Output artifacts/collection-filters
node scripts/verify-collection-filters.mjs --native artifacts/collection-filters --app artifacts/catalog --source tests/Reference/CollectionFilters --output artifacts/collection-filters/audit.json
```

The native audit is `artifacts/current/collection-filters-order-native-v2/audit.json`.
Two empty PNGs match exactly; the other 48 retain 661,819 differing pixels.
The original 48 cases retain 630,648 differing pixels; the two new numeric-key
cases add 31,171. All original source PNGs remain byte-identical.
Images are complete client captures without masks, tolerance or resampling.
Exact paint/raster, expanded prop and date-input coverage, normal motion,
customization/scaling, final integration and native ARM64 runtime
remain open. This component is in progress and is not accepted. The public wiki
is unavailable; documentation remains in this repository. Narrator and OS
settings campaigns are outside this component-library task.
Broader prop coverage includes long labels, other locales,
mutable option sequences, date selection/clearing and hover/pressed states.

Worker-thread producer notifications are marshaled before any XAML state read.
See [the producer notification contract](producer-notifications.md) for the
shared catalog/package regression and its background-only verification limits.
