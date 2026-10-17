# Radio group

`BleeckerRadioGroup` implements the pinned Bleecker RadioGroup option model:
string value/label, optional description and per-option disabled state, a
disabled group, vertical layout and a wrapping horizontal layout. Empty option
collections, empty-string values and unknown selected values are explicit states;
unknown values select no item. Values must be ordinal-unique and non-null.

The consumer owns `Value`. Handle `ValueRequested` and update your application
state or bound value to accept a request. Pointer, keyboard and UI Automation
selection use that same path. Declined requests retain the displayed value.
Selecting an already selected option produces no request; an item cannot clear
selection. Immutable `BleeckerRadioOption` objects are held in an observable
collection. Replace an entry to change text/disabled state. Value-keyed controls
survive reordering and same-value replacement. Removal does not invent a new
consumer value. Unmounted changes reconcile on remount, and removed/template
replaced automation peers cannot invoke stale actions.

```csharp
var group = new BleeckerRadioGroup {
    Value = "daily",
    Options = new() {
        new("daily", "Daily", "A morning digest."),
        new("weekly", "Weekly", "A weekly overview."),
        new("paused", "Paused", Disabled: true)
    }
};
group.ValueRequested += (_, request) => group.Value = request.Value;
```

There is one keyboard entry at the selected enabled option, or the first enabled
option when no enabled selection exists. Windows arrow navigation selects the
next enabled item and retains focus at group boundaries. Ctrl+arrow moves focus
without selecting; Home/End reach the first/last enabled item. Space activates
the focused option. Explicit programmatic keyboard focus retains the requested
enabled option; only Tab entry redirects to the current selection. The native group exposes Selection and each option exposes
SelectionItem with RadioButton role, label, help text and set position/size.
Unlike the browser's native-radio arrow wrap, group boundaries follow Windows
radio navigation. Native keyboard focus uses an explicit system focus visual;
the source's hidden input has no equivalent visible focus ring. DOM form names
have no Windows submission equivalent; use `AutomationProperties.Name` to name
the native group. Group selection remains local to its instance.

The source's 18px ring, 2px stroke, 8px dot, 12px label gap, 12px vertical gap
and 16px horizontal wrapping gaps use bundled Encode Sans and shared theme
resources. Whole-option disabled opacity is .5. Stroke and dot transitions use
the existing CSS transition clock and 190/220ms premium timing; OS reduced motion
and Windows contrast suppress animation. Source and actual native runtime
verification, full motion/raster matrices, real scaling/contrast, Narrator and
native ARM64 execution must all be established before component acceptance.

The reference collector imports the unmodified component from source commit
`a86059013cd59684ce452849278ea81e734f2066`, verifies source/CSS/lock bytes and
uses the existing locked compiler. `tests/Reference/RadioGroup.json` retains
56 captures and eight controlled consumer sequences. Catalog modes are
`radio-group` for native requests/lifecycle and `radio-reference` with a
320x640 or 640x640 client for the source matrix. The numeric verifier is
`node scripts/verify-radio-group.mjs --source <source-png-directory> --native-320 <run> --native-640 <run> --output <audit>`;
its geometry tolerance is 0.001px. The current 56-frame, 136-option audit has
zero geometry error and passes numeric typography, paint, selection and disabled
state checks. All four empty canvases match every source pixel. Across all
frames 147,819 pixels differ; exact raster parity remains unverified. The
comparison uses the entire client, zero masks, zero pixel tolerance and no
resampling. Normal motion is not verified by this reduced-motion run.

The intrinsic text measure retains DirectWrite shaping, font fallback and
consumer language. It converts nominal glyph advances to 16.16 fixed point,
rounds OpenType positioning separately and allocates each shaped run in 1/64px
units, matching the pinned Windows browser. Rounding the whole region or
already-kerned glyphs produces a one-unit RTL width error. The source collector
records DOM ranges, Canvas widths and platform font receipts; the native report
retains glyph advances and allocations for diagnosis. See Chromium's
[glyph conversion](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/skia/skia_text_metrics.h)
and [advance callbacks](https://raw.githubusercontent.com/chromium/chromium/main/third_party/blink/renderer/platform/fonts/skia/skia_text_metrics.cc).
Japanese/Chinese/Arabic mixed text uses the actual browser's reported Windows
faces. Bundled Latin text remains Encode Sans.

The x64 native fixture verifies accepted and declined pointer/UIA requests,
physical Tab entry/exit, non-first selected entry, reverse Tab, arrow boundaries,
Home/End, Ctrl+arrow without selection, Space on a focused item and repeated
Space on the selected item. Every injected key retains exact press/release
owners, including modifiers. It also exercises live resource-brush mutation,
keyed moves/replacement/removal, stale peers, template replacement and unmounted
collection changes. The independent consumer exposed an overly broad entry
redirect; the added regression explicitly focuses an unselected option from
outside the group, then declines its Space request. The expanded fixture has
eight requests per theme.
Live palette observers detach on unload/template changes and reattach on load.

The passing independent x64 package consumer has an application-owned notifying model bound
to `Value`; it exercises physical Tab/arrow/Space, declined/accepted requests,
UIA, keyed identity, metadata replacement, removal and disabled/stale peers.
Both consumer themes pass, and the independent ARM64 consumer builds. The full
x64 catalog passes 154 phases and 1,152 PNG captures in 473.6823716 seconds,
retaining all 1,150 prior captures and 174 protected chart glyph hashes. Both
catalog architectures build. Version `0.1.0-405cea2` and its independent consumers
use the same package; temporary consumer workspaces are removed. See the
[current native checkpoint](native-validation-checkpoint.md) for provenance.
`scripts/verify-radio-delivery.mjs --native <focused-or-full-run> --consumer
<independent-x64-output> --output <audit.json>` checks both theme reports,
16 native requests, eight packaged requests, 26 native key pairs, four modifier
pairs and eight packaged key pairs. With a full run it also checks fixture order
and the unchanged 480-second watchdog. `scripts/test-radio-delivery-audit.mjs`
uses the same `--native` and `--consumer` paths to prove that six altered focus,
request, key-owner, modifier or scale reports fail the audit. Its temporary
JSON-only workspace is removed after the test.
Remaining acceptance includes the complete customization/error/lifecycle matrix,
normal-motion and reversal raster comparisons, real contrast/text/display
scaling, Narrator, collection performance and native ARM64 execution.
