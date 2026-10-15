# Toolbar held-input source contract

The original Bleecker toolbar and real TanStack consumer are pinned by
`tests/Reference/DataTableToolbarLayout.json`. The additive collector
`scripts/reference/capture-data-table-toolbar-press.cjs` captures Space with
the pointer outside/on Reset, Enter with the pointer outside, dragging out,
dragging out and back, and cancelling held Space with Shift+Tab. It begins
with an actual search click and two Tab transfers; it does not park the
pointer or synthesize DOM events. Global filtering keeps Reset mounted so
callback timing can be observed without conflating conditional removal.

`tests/Reference/DataTableToolbarPress.json` records 24 source cases and 88
states across both themes and both motion preferences. Each phase has at
least three stable actual animation frames within two seconds, a live-caret
PNG/hash, exact before/after state equality, trusted input receipts, consumer
table/column/row identity, filter state, row projection, focus, transform,
paint and click counts. Original source/CSS/bundle/font and capture-script
hashes are checked. Raw `matches(':active')`/`matches(':hover')` queries are
retained but, as in the earlier source collector, report false even during
the measured CSS states; they are not substituted for rendered styles and
actual input/hit evidence. No original reference or capture is removed.

Observed source behavior:

- Space applies centered scale `.99` and translate Y `1` until release.
  Background continues to follow hover. Without hover it stays transparent.
- Enter invokes the original Reset callback on key-down and has no press
  transform. Key-up must not invoke it again.
- Dragging outside keeps the press transform but removes hover paint. An
  outside release cancels; returning before release invokes Reset once.
- Shift+Tab while Space is held restores the normal transform and transfers
  focus to the original filter control. Releasing Space there does not reset.
- Reset preserves global filtering and the original consumer-owned objects
  throughout these paths.

The separate native command is `scripts/smoke-catalog.ps1 -Component
data-table-toolbar-press -ClientSize 1024,745` against a built/published
catalog. It replays the reduced-motion source cases through the owned native
window, actual pointer/key input, exact forward and reverse Tab receipts and
an explicit Space/Enter release target. Key-up is sent in a finally path even
when the held action fails. It records 44 native states plus two final catalog
images and retains every source mismatch. This additional matrix does not
replace the original full suite or 376-state interaction replay.

The baseline at `artifacts/smoke-data-table-toolbar-press-baseline` fails in
both themes: non-hovered Space paints the pressed Ghost background, Enter
uses the native release timing/press transform, and a held pointer leaving
Reset drops its transform. Native `0.99` is stored as
`0.9900000095367432`; the fixture uses the existing `1e-6` numeric transform
bound and the existing `1/32` layout bound. Raster comparison retains zero
masking and tolerance. Raw baseline reports remain preserved, including the
first diagnostic's unnecessarily exact floating-point assertions.

The public wiki remains unavailable, so documentation stays here. This native
input/presentation work has no backend or Prometheus surface. Full platform
and component acceptance remain open.
