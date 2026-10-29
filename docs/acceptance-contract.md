# Component-library acceptance contract

The user set this milestone's acceptance criteria on 2026-10-29. Orchard remains
exclusively a reusable C# / WinUI 3 component library with fictional Monitoring
and Editorial catalog demos. The fixed 112-entry inventory remains the scope.

A component is accepted only when all five checks pass on x64:

1. Its applicable states render in Light and Dark.
2. Applicable keyboard, pointer, and focus behavior works.
3. It does not clip at 150% scaling.
4. It respects reduced motion; for a component without motion, the check passes
   when its static behavior is verified.
5. An independent package consumer compiles against the candidate package.

Record pass/fail for every check on every component in
[parity-inventory.json](parity-inventory.json). A missing or stale observation is
fail until verified. The release must list the accepted components by name.
ARM64 is build-only for this milestone and must be labelled runtime-unverified.
Raster/pixel parity is not a gate and must not appear as a remaining inventory
gap. No new evidence folders are required. Keep diagnostics confined to the
five checks and the observed component defect being repaired.

The current Context Menu defect is a physical click on an item owned by an
active modal. Its native flyout must invoke the caller's action without the
modal swallowing the click. Verify that behavior through the x64 package
consumer. Preserve the existing component APIs, themes, fixtures, and tests.
Narrator and OS accessibility/contrast campaigns remain excluded. Do not
launch Narrator or change system accessibility, contrast, or time settings.
The paused pointer heartbeat stays paused, and release delivery must not
trigger GitHub Actions.
