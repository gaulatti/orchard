# Browser reference probes

`InputPaintMotion.json` records 40 transitions from the actual frozen Input
component in Light/Dark with enabled/reduced motion. It retains source, tool,
CSS, lock and collector provenance, independent keyframes/timing definitions,
and five sampled points for background/border/editor opacity. The regression
compares computed CSS colors with the native premultiplied OKLab clock while
preserving fractional alpha. Canvas RGBA diagnostics remain in the capture;
they are not the authoritative color when low-alpha unpremultiplication adds
rounding. These samples verify paint interpolation, not native enabled-motion
presentation, focus-ring spread, shadows or complete raster parity.

`LoadingOverlay.json` records 60 viewport-portal cases at three client sizes
in Light/Dark and six physical browser input probes. The frozen source,
dependencies, CSS, fonts, collector and PNGs have recorded hashes. Capture
animations are paused at time zero. `scripts/verify-loading-overlay.mjs`
checks actual native viewport geometry, baselines, images and owned input;
successful geometry/input does not imply exact raster or motion parity.

`DataTableFilters.json` records the actual frozen Select Filter and real
TanStack consumer in 56 cases and 276 states. The collector/client live under
`scripts/reference`; `scripts/verify-data-table-filters-reference.mjs` checks
raw PNGs, pinned dependencies, trusted input and source-specific edge cases.
See [the source contract](../../docs/data-table-filters-reference.md). Native
Filters implementation and acceptance remain pending.

`DataTableColumnToggle.json` records the actual frozen React/TanStack/Radix
column visibility popover in 28 cases and 132 states. Its collector/client
under `scripts/reference` verify the original lock blob, dependency versions
and integrity, CSS/fonts, real trusted input, stable presentations, original
table/row/column/cell identity and accessibility. Regeneration requires those
frozen dependencies and the source commit listed in the fixture. See
[the source contract](../../docs/data-table-column-toggle.md) for native
comparison commands and open focus/visual/platform gaps.

These isolated pages measure properties of Bleecker's Switch contract at
`a86059013cd59684ce452849278ea81e734f2066`. They are diagnostic probes, not copies
of the component or evidence of full native visual acceptance.

Serve this directory with a local static server, open `SwitchTransitions.html`
or `SwitchFonts.html`, and use its measurement button. The matching JSON files
retain the observed browser output. The font probe uses the canonical Google
Encode Sans weights and requires network access. This is reference tooling;
Orchard's packaged fonts and consumer do not require a network connection.

`SwitchTransitions.html` uses the source's 25% sand color-mix expression,
sea/accent-blue colors, 190/220ms durations and premium cubic-bezier. It pauses
each CSS property at its stated elapsed time. The sequence switches on at 95ms,
reverses for 50ms, reverses again for 25ms, then retargets to dark for 95ms. The
thumb stays paused at 95ms to prove that changing the color leaves its own
animation clock intact. Canvas output measures the resulting sRGB raster.
Replacing the color-mix expression with legacy rgba changes interpolation and
is not an equivalent reference probe.

`SwitchFonts.html` records loaded font faces, canonical label widths and locally
available candidate faces. Segoe UI/Arial matching the Japanese width does not
identify the CJK fallback glyph family. Candidate output must not silently
become a library font mapping. Browser observed here: Windows x64 Chrome 155;
native x64/ARM64 probes are emitted by the registered Switches fixture.

FontFace `local()` resolves face names. Some families require the `Regular`
suffix; a failed bare-family lookup does not prove that the family is absent.
The probe also compares canvas rasters from its successfully loaded aliases.
Direct CSS family requests can silently choose another family, so zero
`differingChannels` from a direct request is insufficient proof of identity.
`namedDifferingChannels` uses the loaded face alias. Yu Gothic UI Regular gives
99.538086px in canvas, matching the native default's advance, while full Yu
Gothic faces give the reference's DOM width but different canvas glyphs. None
of the tested named CJK faces matches the canonical canvas raster exactly.

`CapturePersistence.json` freezes the existing Windows PNG persistence path,
separately from the Bleecker browser references. Its four deterministic
premultiplied BGRA inputs cover a transparent pixel, odd dimensions, every
alpha/channel pair in a 256 by 256 grid and a 1024 by 745 image. The baseline
was captured before changing `SavePngAsync`; it records exact input, PNG and
decoded RGBA hashes plus the unchanged helper/encoder and binary provenance.
The encoder source comparison normalizes CRLF to LF and preserves the
original raw hash as provenance. The existing `brush-alpha` diagnostic runs
the native regression, immediately reads every completed file and proves
that a write failure propagates. Independently verify those actual files
with `scripts/verify-capture-persistence.mjs --native <output> --output <audit>`.

`SearchInput.json` freezes 40 actual Bleecker browser cases at 320/640 in Light
and Dark, including all sizes, empty/value, optional clear, error, disabled,
read-only, RTL and non-Latin text. Capture uses the source component and pinned
runtime/lock/fonts, with source SHA, script SHA and PNG hashes. Four browser
interaction sequences establish that clear calls the consumer without editing
text, disabled/read-only editors leave clear active, and Escape emits an empty
change value that an ignoring controlled consumer does not accept. Native
verification uses actual editor input/binding and an independent external UIA
client; zero geometry error does not imply exact raster parity. See
`scripts/verify-search-input.mjs` and `docs/search-input.md`.
Each baseline mounts a fresh component so paused time-zero transitions cannot
retain another variant's paint. The imported Input SHA and disabled `.6`
opacity are checked explicitly. Original captures remain diagnostic evidence.
