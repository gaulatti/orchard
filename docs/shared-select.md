# Shared native Select surface

The native Select implementation previously nested inside Pagination now
lives in `BleeckerSelectSurface.cs` and `BleeckerSelectSurface.Scroll.cs`.
It remains internal. Its explicit callbacks read the original consumer's
updating state, source identity, display text and checked paint. The shared
surface owns native input, focus, popup/scroll layout and accessibility, and
has no dependency on a table feature module or its rows/state.

Pagination retains its original 80px/40px geometry, option objects/tags,
`Rows per page` name, `OrchardPageSizeChooser` automation class and selection
callback boundary. Existing resource and palette keys are deliberately kept.
The branded shapes, fonts, motion recipes and two-second presentation/input
gates are unchanged by extracting the code.

The trigger now captures its actual native mouse pointer for the duration of
the original press/release. A Filters prototype exposed a lost genuine
release on reopening at the same point after Escape; capture corrects that
route without moving the pointer, synthesizing XAML events or relaxing input
receipts. Disabled rows reject input and are skipped by typeahead/initial
focus. A top-flipped popup uses the same 4px gap with a negative vertical
offset. Width follows the actual trigger allocation when opened/resized.

These APIs were checked in the official WinUI 2.3.9 reference XML shipped with
the pinned Windows App SDK 2.5.1. Extraction-only x64 build and the existing
136-state/120-pair native replay pass; complete verification of the final
shared implementation is recorded at the subsequent artifact checkpoint.
The source/native key-release differences and full component acceptance
remain open.

The native Filters prototype is still undergoing its own source/runtime
verification and is not part of this shared-code release. Its known
modal-input observation and missing-column focus differences must not be
treated as accepted parity. See [the Filters contract](data-table-filters-reference.md).

There is no backend or metrics surface. The public wiki remains unavailable;
documentation stays in this repository. Push-triggered Actions stay paused.
