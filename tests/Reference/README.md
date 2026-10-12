# Browser reference probes

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
