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

Runtime inventory at `a9e66d1c` completed on both architectures and found matching
CDB/symchk plus the loaded rendering DLLs' exact PE CodeView identities. The
package number and DLL product version do not alone establish source equivalence.

The current manual workflow queries symbols on its own published catalog, without
a smoke/contrast command. CDB attaches noninvasively without suspending the target,
uses the explicit Microsoft public symbol server, and queries module/function/type
metadata. It detaches before the owned processes are closed. It makes no dumps,
runs no glyph case and performs no repair or SDK replacement. The complete native
log is retained in bounded hash-bound JSON parts, avoiding Actions artifact quota
and long-line truncation. Query transport completion does not prove symbol
availability or native acceptance; the actual query output must be inspected.
The loaded XAML image SHA and CodeView identity must match the captured inventory
before querying. That binding does not claim source-tag or original failing-run
binary equivalence.

The initial symbol probe at 9c3855f completed on both architectures. The actual
logs report private symbols and type layouts, but two rendering signatures differ
from the previously retained source tag. That tag cannot supply register/stack
capture positions. The DLL resource's embedded commit was not found through the
public source repository's commit API; it is not an established source binding.

The next metadata probe enumerates the loaded PDB's glyph, realization, render
parameter and matrix types, then disassembles only the first 0x40 instructions of
five named rendering functions. It still supplies no object address to a type
query, reads no arbitrary process data and runs no glyph case. Missing type/code
output remains a limitation even if the query transport completes. This evidence
must be inspected before constructing any targeted native data trace.

The public repository wiki was unavailable at startup; this document records the
diagnostic's scope and limits in the repository.
