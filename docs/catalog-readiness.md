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

The full diagnostic run `artifacts/smoke-runtime-profile-full` times out
after 936 captures at the original deadline. Its journal retains 114
completed phases and identifies Dark Input as the unfinished phase.
The summarizer reports no terminal profile/result and false full-runtime
acceptance. The external wrapper records the terminal timeout separately
in `artifacts/runtime-profile-full-timing.json`. All 174 protected glyph
hashes match the last passing baseline; 98 baseline PNGs remain missing.

Across those completed phases, 545 bitmap snapshots spend 24.077 seconds
rendering and 0.971 seconds reading pixels. Saving 858 PNGs takes 11.326
seconds: 4.773 converting, 1.888 encoding and 4.200 writing. These are
nested wall-time spans, not additive CPU costs. The slowest complete
phases include Light Input at 16.206 seconds, each Sankey theme at 15.556,
each Alert theme at 11.952 and Light Checkbox at 10.519. Required Alert
lifetimes and Skeleton motion observation windows remain intact.

A resource-reuse experiment keeps one RenderTargetBitmap but calls
RenderAsync for every snapshot. Its Sparkline render total is 632.9 ms
versus 608.5 ms before; Input renders 140 snapshots in 5.869 seconds.
Both focused runtime checks pass, but neither comparison establishes a
rendering-cost improvement. The experiment is removed. Its decision,
raw profile and exact patch remain at
`artifacts/bitmap-reuse-candidate-decision.json`,
`artifacts/bitmap-reuse-*-summary.json` and
`artifacts/bitmap-reuse-rejected.patch`.

Further save-pipeline work must first trace all consumers that open a
just-written image. Every required reader must still wait for completed
bytes; every snapshot must still be captured from the actual visual; all
image writes must finish successfully before a runtime pass is emitted.
No image cache, omitted capture, relaxed comparison or input-gate change
is authorized by this profiling work.

The retained profiler source `86fb9c6` builds cleanly for x64 and ARM64,
passes all 100 unit tests and token generation, and passes the complete
focused Input runtime from its clean checkout. That run records 140 bitmap
snapshots, 164 PNG writes and a valid terminal profile in 32.178 seconds at
`artifacts/smoke-runtime-profile-clean-input`. Its bitmap render total is
5.511 seconds; this supports removing the resource-reuse experiment rather
than retaining it as an optimization. The rejected primary build is renamed
to `win-x64-rejected-bitmap-reuse-prototype` beside the usual primary Release
output. Use the clean checkout's executable until the primary output is
rebuilt. The exact generated-output location and preserved assembly hash
are recorded in `artifacts/bitmap-reuse-prototype-location.json`.
