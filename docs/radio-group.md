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

The separate motion audit freezes 52 browser timelines and checks actual
native ring/dot properties with Windows animations enabled and reduced. It
verifies 190ms ring and 220ms dot timing, 156 frozen enabled-motion samples,
state/theme transitions, two mid-transition disable cases and palette changes
under both preferences. Disabling an option changes its whole-option opacity
immediately and retains unchanged stroke/dot clocks. Theme changes preserve
the ring transition; dot fill changes immediately, as in the source.

Selected theme endpoints are legacy RGB CSS colors and interpolate in encoded
sRGB. The source's translucent off/hover `color-mix(in oklab, ...)` endpoints
promote mixed transitions to premultiplied Oklab. `BleeckerCssColor` retains that
space through interruptions until the target is reached. Shared Oklab conversion
uses Chromium/Skia's ICC 16.16 sRGB-to-XYZ D50 matrix and Bradford adaptation,
rather than the direct-sRGB approximation. See the [color conversion notes](css-colors.md).

`scripts/smoke-radio-motion.ps1` exercises both real Windows animation preferences
and restores the original setting in `finally`, including on failure. It uses
the same extracted preference helper as the Stepper harness. The `radio-motion`
catalog mode records actual paint timestamps, transition clocks, ring brushes
and dot transforms; it does not override the renderer's clock.
`node scripts/verify-radio-motion.mjs --native <wrapper-output> --output <audit>`
checks the browser recipe, native trajectory, retained clocks and palette tests.
`scripts/test-radio-motion-audit.mjs --native <wrapper-output>` proves that
altered color/scale, missing frames, lost clocks, failed palette updates and
unrestored Windows preferences are rejected. Geometry/pixel tolerances are
unchanged. Numeric motion verification does not establish frame-by-frame raster
parity, full reversal/customization/lifecycle coverage or platform acceptance.

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
x64 catalog passes 154 phases and 1,152 PNG captures in 473.9255674 seconds,
retaining all 1,150 prior captures and 174 protected chart glyph hashes. Both
catalog architectures build. Version `0.1.0-9594a5a` and its independent consumers
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
