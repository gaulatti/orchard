# BleeckerSlider

`BleeckerSlider` is a consumer-controlled range input. `Min`, `Max`, `Step` and
`Value` default to 0, 100, 1 and 0. `Label` and `ShowValue` add the optional
heading row. Caller changes flow through `ValueRequested`; the component keeps
the original `Value` and its binding until the caller accepts a request.

```xml
<orchard:BleeckerSlider Label="Level" ShowValue="True"
    Min="0" Max="100" Step="5" Value="{Binding Level}"
    ValueRequested="RequestLevel" AutomationProperties.Name="Level" />
```

The handler updates the bound model, for example `ViewModel.Level = args.Value`.
Leaving the model unchanged rejects the request. Programmatic property updates
do not emit requests. The displayed number remains the original value, while
the native range peer and thumb use the source input's clamped, step-aligned
value. For example, Value 25.3 with Step 10 displays 25.3 and positions the thumb
at 30. Decimal step ties round upward, including 0.15 with a 0.1 step.

Nonfinite min/max/step attributes use the pinned HTML range behavior; a
nonfinite input value uses its valid-range midpoint before step alignment.
Reversed or equal bounds retain their source behavior. Invalid CSS fill widths
leave the previous valid percentage in place. A fresh control with no valid
fill assignment uses automatic width. The 182-case frozen browser reference
measures the actual browser thumb through DevTools, without reconstructing it
from the value alone; 18 real keyboard sequences and 14 retained-style updates
also record source behavior.

The horizontal track is 8 DIP high, the input region is 20 DIP high, and the
thumb is 20 DIP with a 2 DIP border. The optional heading uses 14/20 DIP medium
labels and regular tabular values, with an 8 DIP gap before the input. The fill
and two thumb-shadow layers use the control timing curve. Reduced motion snaps
to the destination. Disabled state dims the thumb while leaving the track and
heading unchanged. Theme colors come from the shared token resources.

The control uses a native Thumb for drag capture and native pointer/keyboard
events. Arrow keys change by a step; PageUp/PageDown change by the source's
10% range rule before step alignment. Home/End select valid endpoints. RTL
reverses horizontal arrows and thumb/fill placement. The native range peer
retains one keyboard entry, reports effective bounds/value/step, routes value
requests to the caller and rejects disabled or detached actions. Caller naming
metadata takes precedence; otherwise the visible label names the Windows
control. Parts, cursor handles, brush observers, clips and active timelines are
released or detached at their lifecycle boundaries.
An explicit empty name from a local value, binding or `Style`/`BasedOn` setter
stays empty. Removing the override restores the current label without replacing
the caller's style or value binding.

The Sliders catalog fixture accepts fictional local changes, rejects a fixed
value, and includes fractional, mismatched, disabled and RTL examples.

The packaged hidden contract must match all 182 source layouts on initial
presentation, including optional headers, raw text, sanitized thumb placement
and source paint properties. It must retain the fourteen source fill-history
states, route accepted/rejected range requests through the caller's model, keep
value/bounds/step/enabled/name/style bindings, respect empty local/style names
and caller labels, reject invalid/disabled/detached actions, and release owned
clips, shadows, observers and rendering callbacks at lifecycle boundaries.
An invisible host must not retain a rendering subscription. The diagnostic
does not show/activate a window, focus a control, send input or change OS
preferences. Visible raster, normal motion and physical input remain separate
acceptance checks.

The broad independent consumer's initial Slider check now accepts the observed
XAML-parsed raw value `25.299999237060547` for a `Value="25.3"` declaration
within one millionth, while still requiring the exact sanitized native range
value `30`, bounds, step and role. Its previous exact-double assertion rejected
this retained caller value. A fresh run passed that boundary but later stopped
in a separate Pagination physical-input check; it does not establish a complete
broad consumer pass. The focused hidden Sonner consumer check is recorded in
[Sonner](sonner.md).

All 234 portable tests and seven windowless native font tests pass. The
multilingual label uses the combined inline shaping advance, avoiding an extra
1/64 DIP from separately rounding fallback-font runs. The catalog includes that
multilingual example. A guarded independent x64 package consumer matches all
182 initial source layouts and paint properties with zero numeric error, all
fourteen fill-history states and 36 ownership/lifecycle checks. Four enabled
range requests exercise accepted/rejected caller models; no focus or physical
input is sent. All 44 damaged receipts are rejected. The same package also
replays Theme Toggle's 116 hidden layouts and 42 ownership checks.

Run the background contract with a fresh folder:

```powershell
./scripts/verify-hidden-slider-contracts.ps1 -Package <package.nupkg> -Platform x64 -Output <fresh-folder>
node scripts/verify-hidden-slider-contracts.mjs --native <fresh-folder> --output <fresh-folder>/audit.json
node scripts/test-hidden-slider-contracts-audit.mjs --native <fresh-folder> --output <fresh-regression-folder>
```

The exact v7 extracted x64 catalog now passes the focused visible native Slider
run. Its real pointer and keyboard route includes accepted and rejected thumb
drags in both Light and Dark, track/keyboard/range requests, disabled rejection,
template replacement, remount, and caller binding retention. The independent
consumer of the same v7 Orchard DLL passes its hidden 182-layout, 14-history,
36-ownership-check contract and 1,244 audit guards without sending input. The
[v7 Slider checkpoint](evidence/components-v7-slider-input-v1/checkpoint.json)
binds both reports and their auditors to the unchanged package. The catalog
auditor finds zero geometry error across 182 frames and verifies 18 keyboard
sequences, but none of the 44 source/native image pairs are byte-identical.
Two representative pairs were inspected; a full visual review remains open.
Normal/reduced motion, broader visible and independent-consumer input, actual
scaling, and native ARM64 runtime remain unverified. No Narrator or OS
preference changes were used.
