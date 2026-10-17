# Frozen Accordion source images

These 60 full-client PNGs are generated from the unmodified Bleecker Accordion
at `a86059013cd59684ce452849278ea81e734f2066`. `../Accordion.json` records every
image SHA-256, source and harness hashes, the browser version, geometry, paint
and rich-text platform-font receipts. The unit suite verifies the image bytes.

Regenerate with `scripts/reference/capture-accordion.cjs` using the pinned source,
tooling and Playwright directories described in `docs/accordion.md`. Review new
reference bytes and provenance together; replacing a golden is not evidence of
native parity. Native comparisons use the whole client, zero pixel tolerance,
zero masks and no resampling. Source raster parity remains unproven.
