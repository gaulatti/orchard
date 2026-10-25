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
logs report private symbols and type layouts. Two displayed argument lists differ
from the previously retained source declarations, but that display alone does
not establish different calling conventions or source. The DLL resource's
embedded commit was not found through the public source repository's commit API;
it is not an established source binding.

The next metadata probe enumerates the loaded PDB's glyph, realization, render
parameter and matrix types, then disassembles only the first 0x40 instructions of
five named rendering functions. It still supplies no object address to a type
query, reads no arbitrary process data and runs no glyph case. Missing type/code
output remains a limitation even if the query transport completes. This evidence
must be inspected before constructing any targeted native data trace.

The metadata probe at 886b822 completed on both architectures: all 16 requested
types and five 64-instruction entries are present. The inspected entry code agrees
with the retained argument locations, correcting the initial signature-mismatch
inference. For example, x64 Rasterize reads tile offsets at entry RSP+0x30/+0x38
and the device context at +0x40; ARM64 retains the bounds in x3/x4, tile offsets in
s0/s1 and device context in x5. Exact loaded code and types remain the capture
authority. No complete source equivalence, glyph result or acceptance follows.

The targeted trace launches only this catalog under CDB, with credential-bearing
environment names removed and the debug heap disabled. Its fresh output directory
receives an atomic process registration. The runner verifies executable path,
creation time, architecture and the actual loaded XAML SHA/CodeView identity
before releasing startup and enabling target-data breakpoints. It never attaches
to or terminates an independently selected process.

The native binding compares all 45 UTF-16 characters of the unique fictional
Default description, then follows that renderer's drawing context. Managed phase
markers additionally require the actual named description. Snapshots retain at
most four runs, each at most 128 glyphs; native hook and capture counts are capped.
Stored glyph data, font-face identity, baseline, incoming matrix, tile matrix and
the six words at the observed final matrix call site are retained. The first
baseline, title-before-description order and every existing pixel/source/native
assertion remain. Marker failure cannot skip owned highlighter/backdrop cleanup.

This is an invasive diagnostic: breakpoint stops can change timing. A missing
initial target capture, command/memory error, bound violation, timeout or failure
to reproduce the original restoration difference makes the corresponding causal
comparison incomplete. A successful trace transport is not a passing glyph case.
The runner retains the original failing result and fails its step when that
result fails. No accepted calibration or Windows contrast transition is added.

The public repository wiki was unavailable at startup; this document records the
diagnostic's scope and limits in the repository.
