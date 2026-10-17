# Search Input

The current Input paint correction preserves fractional CSS brush alpha and
uses the shared premultiplied OKLab transition clock. All 40 native frames
retain zero geometry error and now record source RGB, effective brush alpha
and editor opacity explicitly. Eight corrupted reports are rejected, including
altered paint alpha and extra consumer clicks. Foreground acquisition may use
one separately named, receipt-verified surface click before the ten required
clear clicks per viewport. The strict comparison improves from 101,288 to
83,452 differing pixels; exact raster parity stays false. All 117 unit tests
pass, including 40 frozen browser paint timelines. See [Input paint](inputs.md).

`BleeckerSearchInput` composes the existing `BleeckerInput` native `TextBox` and
a native clear `Button`. Its source is Bleecker `search-input.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`0a9e5eafdbafdeffdb2e60859ecd5cd1eea4af3598eac712e03f5a6d01a2a038`.

Before implementation, the acceptance boundary was defined as: all three sizes,
empty/value/optional-clear/error/disabled/read-only/RTL/non-Latin variants in
both themes at 320 and 640 pixels; normal native text editing and two-way
consumer binding; independent clear activation through pointer, Space and Enter;
correct accessible editor/button names and roles; live command/label updates;
and safe template replacement and unload/remount. These are initial boundaries,
not the complete inventory acceptance matrix.

## Consumer contract

Bind `Text` in `TwoWay` mode when the consumer owns an observable value. Set
`PlaceholderText`, `InputSize`, `IsError`, `IsReadOnly` and `MaxLength` as needed.
`Editor` exposes the retained native `BleeckerInput` for selection, clipboard,
text events and the rest of the ordinary `TextBox` interface. Setting an outer
template to null unloads that editor; applying a replacement template retains
the same editor and bindings.

The clear button appears only for nonempty `Text` and a `ClearRequested` handler
or `ClearCommand`. Clear invokes the command with the original
`ClearCommandParameter`, then the event. The consumer decides whether to edit
the value or move focus. `CanExecute` disables the button. `ClearLabel` defaults
to `Clear search` and can be localized. Removing the last handler/command hides
the action. Invocation is guarded when disabled, unloaded or detached.

`IsEditorEnabled=false` models the source input's `disabled` attribute: the
separate clear action remains available. `IsReadOnly=true` also leaves clear
available. `IsEnabled=false` disables the whole compound control. These are
distinct Windows contracts. Escape empties an enabled, editable nonempty native
editor through its normal text-change/binding path, matching the source browser
search input default; it does not invoke the clear callback. A controlled React
consumer that ignores the change keeps its original value. A Windows consumer
can handle native editor text changes to apply its own value policy.

Use `AutomationProperties.Name` or `LabeledBy` on the outer control to name its
native editor. Clear keeps its separate action name. The wrapper is not a tab
stop; the native editor and visible enabled clear button participate in normal
Windows keyboard focus.

The start search glyph is 16 pixels. Clear uses a 14-pixel Lucide X, two-pixel
padding, four-pixel radius, physical right inset ten, and an immediate `.92`
pressed scale. The physical left/right positions remain the same in RTL while
the text editor follows `FlowDirection`. Text padding is always left 40/right
32, including size changes and template replacement. Hover uses the pinned
190 ms premium transition and sand opacity `.10` in Light / `.15` in Dark.
The shared native action implementation retains FilterChip's existing 12-pixel
glyph and `.9` pressed scale defaults. Both use bounded, detachable UI settings
and accessibility-name observers. High contrast theme resources and reduced
motion are implemented; their full acceptance evidence remains pending.

## Evidence and remaining acceptance

`scripts/reference/capture-search-input.cjs` bundles the actual frozen source
with the pinned React/Lucide/runtime dependencies and bundled fonts. Its
`tests/Reference/SearchInput.json` records 40 browser cases and four interaction
sequences. The capture checks source/lock/tool versions, verifies that disabled
and read-only editors permit clear, and records Escape's actual browser change
event. It does not infer browser default behavior from component source alone.

The catalog provides `SearchInputsFixture.xaml`, component smoke modes
`search-input` and `search-input-catalog`, and the full fixture navigation entry.
The focused native harness checks whole native editor/glyph/button geometry,
native input delivery, accessible names/roles, binding, command and lifecycle
behavior. `scripts/verify-search-input.mjs` uses the existing `.05` DIP geometry
bound, verifies actual input receipts and retains full PNG frames.

The initial x64 run passes all 40 native cases with **zero geometry error**,
56 exact native key pairs (including copy/paste and maximum-length editing),
four editor-to-clear Tab transfers, 20 clicks and 24 independent external UIA
snapshots. Those snapshots prove native Edit/Value/Text and Button/Invoke
patterns, names, values, read-only/disabled state, optional clear and remount.
Six corrupted reports are rejected. The real catalog consumer clears its own
text in both themes. All 116 unit tests pass. x64 and ARM64 cross-builds pass;
6,054 resolved methods and exposed interfaces match between architectures.
The shared FilterChip defaults retain their 20-case geometry and 28-event /
22-retained-invocation regression results.

The strict 40-frame source/native comparison reports **101,288 differing
pixels**, with zero masks and no resampling. Geometry success does not accept
those raster differences. Evidence is retained under `artifacts/search-input-*`
and `artifacts/catalog-search-input-final*`; these are local development paths,
not committed image assets. An earlier in-process pattern assertion, a missing
UIA fixture scope and the original mirrored-origin geometry measurement remain
as failed diagnostics. Physical bounds use `TransformBounds`, including RTL.

The first browser capture reused its DOM node and paused a transition at time
zero, retaining the previous variant's paint in some states. That 107,752-pixel
diagnostic is superseded by `artifacts/search-input-browser-fresh-mount` and
`artifacts/search-input-fresh-reference-pixels.json`. Each baseline now mounts
fresh source DOM; disabled opacity is explicitly checked as `.6`, and the
imported Input source SHA is verified. State inputs and measured geometry did
not change; the existing native frames and runtime receipts are re-audited
against the corrected reference. No native library behavior changed.

Checkpoint source is `1688886`. The independent
`Gaulatti.Orchard.0.1.0-search-input.1688886.nupkg` consumer passes x64 runtime
in both themes and ARM64 build-only validation. Both temporary isolated
consumer/cache directories are removed. The package's `Orchard.dll` exactly
matches the tested x64 catalog library. Versioned x64 and ARM64 catalog ZIPs
contain 536 and 532 files respectively, with every entry hash verified. The
package and archive hashes are recorded in [the current native checkpoint](native-validation-checkpoint.md).

Search Input remains **in progress**. Exact raster/typography/color parity,
complete hover/focus/pressed and enabled-motion timeline comparisons, broader
customization and clipboard/IME/selection/accessibility-event cases, actual
Windows display/text scaling, high contrast, Narrator and native ARM64 runtime
are not accepted. Cross-building ARM64 is not native runtime evidence. No
backend, search service, debounce, results list or submit policy is introduced.
