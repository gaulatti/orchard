# Catalog condition scheduling

`NativeReadiness` gives pending binding/callback chains two low-priority
dispatcher turns before using the existing 16 ms poll. Each queued turn is
still bounded by that poll. Callers retain their exact conditions, layout
checks and original two-second deadlines.

This helper does not establish presentation. Pointer and final-capture gates
continue to require their genuine stable `CompositionTarget.Rendering`
transitions; input delivery and ownership checks are unchanged.

The expanded chart-input full run preserves all source-state checks but
times out after 936 captures at 360 seconds. The new chart pointer gates
account for about 12 seconds in that run. The capture/input profile is
`artifacts/chart-input-full-profile.json`.

The two-turn scheduling candidate passes the full focused Pie/Donut input
audit in 19.7744603 seconds versus the preceding published run's 20.6130906.
This single comparison does not establish a causal or whole-suite speedup.
Both runs retain 84 cases, 180 pointer presentation gates, 268 owned key pairs
and the same source-state expectations. Fresh published full verification
under the unchanged 360-second limit remains required.
