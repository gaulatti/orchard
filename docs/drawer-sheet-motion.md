# Drawer and Sheet interruption evidence

This extends the [native preview](drawer-sheet.md) without changing library
sources or the verified `0.1.0-edge-dialog-v1-local` NuGet package. It does not
complete native motion or component acceptance.

## Frozen original CSS

`tests/Reference/EdgeDialogInterruptions.json` records 30 histories, 150 retarget
stages and 390 samples from the actual pinned Drawer/Sheet components. The
matrix covers both themes and all five owner variants, with initial entry/exit
interruptions at 80/45, 160/75 and 240/60 ms, followed by two rapid reversals.
Independent captures are byte-identical. Collector, client, styles, fonts,
original bundle inputs and pinned component dependencies are hashed.

The driver pauses the original CSS `translate` and `opacity` transitions. Its
phase clock accumulates sampled animation time; a separate isolated browser
clock advances the React presence timers. That isolates CSS retarget behavior
and preserves the real component, original editor, inert state and modal
ownership. It does not establish real-time timer/CSS integration or native
renderer timing, and it does not change OS time.

Drawer's different exit curves require independent reversing-shortening factors.
After one interrupted exit, its panel reentry is approximately 58.39 ms while
the backdrop reentry is 202.23 ms. Later in that history, durations are 1.12 ms
and 106.81 ms. A shared duration would visibly change the presentation.
Sheet's equal panel/backdrop curves retain equal shortened durations.

`EdgeDialogReferenceTests` matches all 390 measured values and all 300 property
durations against the existing C# transition implementation. Pixel-coordinate
tolerance is 0.0002 logical units for browser float bounding rectangles;
opacity tolerance is 0.000002 for computed-style serialization, and duration
tolerance is 0.005 ms. No comparison is weakened to hide a failing transition.
All 335 portable tests pass. The source auditor rejects 16 damaged references.

## Live native property boundary

`EdgeMotionFixture` runs through public APIs in an unactivated hidden window.
It reads the actual WinUI translate transform and backdrop opacity under real
dispatcher timers. It never injects a model clock or changes system settings.
Property-change observers timestamp publications so optional normal-mode
continuity checks account for the age of the preceding published value.
Those bounds are property-level sanity checks, not pixel or frame-rate proof.

Windows currently reports animations disabled. The catalog host and a fresh
independent x64 consumer each pass ten reduced-motion cases and 240 property
observations. Entry/exit/reentry reach the correct endpoints, a closing layer
deactivates immediately, reentry retains the original surface and caller content,
and final close releases its presentation after the presence interval.
Both native reports explicitly record this preference and zero normal-mode
continuity pairs. Eighteen damaged native reports are rejected.

Normal-mode native timing, interrupted normal-mode property continuity, complete
pixel motion equivalence, reduced-motion preference changes while running,
physical input/focus and ARM64 runtime remain unverified. The existing OS
preference is preserved. These limitations do not authorize Narrator, OS
contrast/accessibility/time changes, or pointer-heartbeat activation.

The motion property gate and frozen-source/model gates are registered in the
existing PR/manual workflow. Push-triggered Actions remain disabled; no workflow
is dispatched by this verification.
