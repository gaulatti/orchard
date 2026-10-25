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
The manual-only workflow publishes the pinned catalog and retains bounded JSON
and cropped PNGs in logs, avoiding Actions artifact storage. Failed observations
remain failures. Both labels and architectures must pass before considering the
mechanism for the unchanged full contrast and exact OS/registry restoration suite.

The public repository wiki was unavailable at startup; this document records the
diagnostic's scope and limits in the repository.
