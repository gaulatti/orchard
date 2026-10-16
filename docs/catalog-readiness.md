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

The published candidate's full run stops at the Tooltip geometry assertion
after 323 captures (`artifacts/smoke-chart-readiness-validated-full`). A
primary diagnostic build passes all twelve Tooltip theme checks but times
out after 934 captures (`artifacts/smoke-tooltip-theme-diagnostic-full`).
The two-turn candidate therefore has no verified full-suite throughput
benefit. Both failures remain retained; no deadline or gate was changed.

## Runtime cost diagnostics

Smoke runs write `catalog-runtime-profile.jsonl` alongside their normal
evidence. Fixture boundaries flush completed phase durations and measured
bitmap rendering/readback, PNG conversion/encoding/writes, screen captures,
composition flushes and navigation mounting. Readiness counters identify
the caller and distinguish queued dispatcher turns from scheduled 16 ms
polls. They do not measure the actual duration of a poll.

The profile adds no await, dispatcher turn, input, capture or readiness
condition. It keeps the original PNG encoder and Windows alpha conversion.
The journal is flushed at fixture boundaries, so completed phases remain
available when the external 360-second wrapper terminates the process.
This catalog-only diagnostic is inactive during normal interactive use and
adds no reusable-library or package dependency.

```powershell
node scripts/summarize-catalog-profile.mjs --native <smoke-output> --output <summary-json>
```

The summarizer explicitly reports missing runtime results and an unfinished
phase for a timeout. A focused pass cannot become a full-runtime pass.
Scope durations include awaited work; nested categories overlap and must
not be added together. Profile journaling adds its own small observation
cost, so a diagnostic run cannot by itself prove an uninstrumented speedup.

The first focused profile at `artifacts/smoke-runtime-profile-sparkline`
passes the 36-case source/input audit and all 100 unit tests. Its 14 bitmap
captures spend 608.5 ms rendering, 63.9 ms reading back and 150.9 ms saving
PNGs, including 30.5 ms encoding. The complete focused run takes 7.835 s.
The independent comparison with the preceding published candidate finds
ten byte-identical PNGs, eight changed curve pixels across two chart PNGs,
and 9,330 changed shadow pixels across the two catalog captures. Their
cause is unestablished; the raw pixel report remains
`artifacts/runtime-profile-sparkline-pixel-differences.json`. This is not an
exact-raster pass. The PNG encoder source, Windows alpha conversion, source
references and all input/presentation assertions remain unchanged.
