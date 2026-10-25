# Isolated live highlight ink diagnostic

This branch tests a distinct calibration mechanism on the actual ErrorState
`PART_Title` and `PART_Description`, in that order, on hosted x64 and ARM64 Windows.
It does not change the original calibration, return an accepted `InkSample`, run
the Windows contrast transition, or qualify a component or the original PR.

The first untouched composed raster is captured before reading
`TextBlock.TextHighlighters`. That getter changes native rendering state; its
empty initialization must preserve the first raster, geometry and specified
foreground/backdrop, binding, animation-base, brush, shared-consumer and native
state. Two untouched observations must also match. No later baseline replaces it.

An exclusively owned full-range highlighter uses a nonnull transparent background
and an opaque foreground. It is built while detached, appended last, and removed
by identity. Existing collection contents and ranges remain unchanged; an active
selection is refused. This avoids replacing the foreground property or animating
its shared brush. It does not claim global mutation freedom: WinUI realizes its
default highlighter brushes, and lazy collection internal absence is not restored.

The original pair of stable black-on-white and white-on-black captures exercises
the actual live glyphs; no extra paint intervention precedes that pair. Every cleanup
must recover the first raster and all specified sources. The required mask is the
original calibration's nonempty exact three-way RGBA8 intersection. Equality of
every initial solid pixel is a diagnostic count, not a replacement contract.

`Probe-HighlightInkSources.ps1` keeps the existing 360-second process deadline.
The manual-only probe at commit `5be3e58f` passed both labels on x64. ARM64 passed
the title but changed 19 description pixels after black-on-white cleanup; all
specified source/native checks still matched. Independent decoding reproduced
the prior discrete-color probe's identical original and restored rasters. This
version is not qualified for the unchanged full contrast/OS-restoration suite.

The current manual workflow inventories the pinned rendering DLLs, PE CodeView
symbol identities and installed native debugging tools on both architectures.
It starts and closes only its own published catalog, without a smoke or contrast
command. It reads font preferences and rendering-module metadata; it performs no
repair and does not rerun the failed glyph case. Bounded JSON is retained in logs
without Actions artifact storage. A successful inventory is not a native pass.

The public repository wiki was unavailable at startup; this document records the
diagnostic's scope and limits in the repository.
