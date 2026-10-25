# Drawer and Sheet source contracts

The original preparation checkpoint kept Drawer and Sheet pending. The
[native implementation and catalog fixture](drawer-sheet.md) now put both
entries **in progress**. Frozen browser evidence and internal models remain
separate from native and independent-package runtime validation; no complete
component acceptance or source raster parity is claimed.

The reference is Bleecker commit `a86059013cd59684ce452849278ea81e734f2066`.
`tests/Reference/EdgeDialog.json` records 194 states (66 Drawer, 128 Sheet),
20 accepted/refused close histories and ten synthetic touch histories.
The states cover both themes, widths 420/767/768/900, content overflow, empty
content, absent/wrapping headings, description-only headings, caller overrides
and retained content-height updates. The five local font faces are loaded before
measurement. The complete 194 PNGs and JSON recapture byte-for-byte independently.
The manifest identifies current frames; unrelated older PNGs are not evidence.

`EdgeDialogMotion.json` records ten timelines and 100 browser animation samples.
`EdgeDialogPresence.json` records eight traces and 104 presence observations,
including reduced motion, rapid reopen and closing before the enter delay.
Presence clocks are isolated inside the browser; they do not change OS time.
Synthetic browser touches establish the JavaScript threshold, not native touch
delivery or physical pointer acceptance.

## Geometry and content

Drawer is full width and bottom aligned, with a top border of one pixel and
14-pixel top corners. Its height follows content up to 90% of the viewport.
The custom reference uses 70% and square corners. Its 40-pixel handle row
contains a decorative 24-pixel Minus icon. There is no close button.
The optional header has horizontal padding 24, top padding 4 and bottom padding
16. Title typography is Encode Sans Medium 18/28; description is Libre Franklin
14/22.75 with an eight-pixel top margin, including description-only headers.
The body alone scrolls, with horizontal and bottom padding 24. The scrollbar
reduces body content width by 15; header width stays unchanged.

Sheet supports left, right, top and bottom. Vertical sides fill viewport height
and cap width at 384; a caller override in the reference raises that to 512.
Horizontal sides have natural content height, including height beyond the
viewport. They have no source viewport-height cap. The horizontal custom width
cap remains anchored at the left edge in the LTR reference.
All sides have a one-pixel border. Vertical sides have square corners; horizontal
sides round the inward corners by 14. Inner padding changes from 24 to 32 when
**viewport** width reaches 768. The header is always present, with a 36-pixel
close control, a 16-pixel text/control gap and a 20-pixel bottom margin.
Both headings retain the global h2 tracking of -0.02em. Sheet title typography
is Encode Sans Medium 24/32; description is Libre Franklin
14/22.75 with an eight-pixel top margin. Body scrolling does not narrow the
header. With `scrollContent=false`, the body clips or lets its original child
own scrolling. Native adaptation must preserve caller content, templates,
selectors, bindings and explicit sizing properties.

## Requests, timing and modality

Both components leave open state with the caller. Escape and backdrop clicks
request close; Sheet also has its close button. Refusing a request retains the
original edited input. Drawer requests close only when touch end Y minus start Y
is **greater than 60**, and clears the gesture afterward. Mouse dragging is not
a source dismissal path. The pinned Drawer accepts `snapPoints` but does not use
them; the snap reference exactly matches the fixed state. Do not invent snapping.

Presence waits 20 ms before entering and retains an inert, hidden closing surface
for 180 ms. Reopening cancels the previous removal. These presence boundaries
also apply with reduced motion. Opening panel and backdrop transitions are
240 ms with `cubic-bezier(.22,1,.36,1)`. Closing panels use 180 ms with
`cubic-bezier(.4,0,1,1)`. Sheet's closing backdrop uses that same exit curve;
Drawer's closing backdrop retains the premium opening curve. Tailwind's
individual `translate` transition is sampled separately from opacity.

Native controls must use the shared modal host, support nested/owned popup
branches, prevent background interaction, restore caller ownership and retain
the original content. Unnamed dialogs expose the source name `Dialog` and
optional title/description relationships. Actual focus, pointer/touch, complete
raster/motion parity, scaling and ARM64 runtime need separate native evidence.
Narrator and OS contrast campaigns are excluded by the current library scope.

## Verification and next acceptance boundary

`EdgeDialogReferenceTests` compares all 194 measured geometries, the ten touch
histories, 100 animation samples and 104 presence observations with the internal
models. This proves model agreement with frozen browser data, not a native UI.
`scripts/reference/verify-edge-dialog-source.mjs` checks coverage, source pins,
font producers, raw PNG hashes/CRC/dimensions, close/gesture contracts and both
timing references. Its full mode rehashes original bundle inputs, checks actual
transitive package versions against the pinned lock and compares independent
captures. Its frozen-only mode is usable in PR/manual validation without the
external checkout. `test-edge-dialog-source-audit.mjs` rejects damaged evidence.

Before either entry starts native acceptance, implement reusable controls and
fixtures that expose these behaviors through public APIs. Exercise both themes,
all four Sheet sides, retained content resizing, caller-refused close, nested
modal ownership, body-only scrolling and custom limits in the real catalog and
an independent package consumer. Source geometry tests and cross-builds alone
cannot satisfy that boundary. Both fictional Monitoring and Editorial component
demos remain required.
