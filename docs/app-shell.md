# AppShell

`BleeckerAppShell` composes original caller `Header`, `Content` and `Footer`
objects. Each slot supports templates and selectors for original models. It
uses the window viewport as its minimum height, allows long content to grow,
and places short-content footers at the bottom. The caller owns scrolling,
navigation, commands, editing state and all data. No application services are
introduced. The catalog's bounded shell example composes the existing suite
selector, input, FeedColumn and FeedItem over fictional local data; the two full
monitoring/editorial demos remain separate required compositions.

`OffsetHeader` defaults to true. A truthy header adds the 72-DIP header token to
the main region's top padding, independently of the supplied header's height.
Set it false for an ordinary header that already occupies the intended space.
`HeaderHeight` corresponds to the source `--bleecker-header-height` CSS token;
its style consumes the shared `BleeckerHeaderHeight` resource. Numeric zero is
rendered in a header but adds no offset; Boolean true adds the offset while
rendering no text, matching the source. Caller templates supply visible content.

Native `Padding`, `Height`, `MinHeight`, and `Style`/`BasedOn` overrides retain
their producers. A local, styled or bound `ContentPadding` replaces the main
padding recipe, including an explicit zero. `ContentBackground` styles the main
region separately from the shell's `Background`. The main region exposes a
native Main landmark while original child controls retain their own metadata.
Slot changes, template reapplication and window resizing must retain caller
objects, edits and bindings. Viewport observation disconnects on unload.

Header and footer slot wrappers preserve the original rendered root's
`Canvas.ZIndex`, including live changes and model-template roots. A
`BleeckerHeader` therefore keeps its default stacking above the opaque main
region; its mobile menu can overlap body content. Replacing a slot or unloading
it disconnects the old stacking observer. Clearing the slot resets its wrapper
to zero. Caller controls and templates remain the source of the stacking value.

Acceptance requires the complete 168-state frozen layout/paint reference and
56 original PNGs, both themes, all three original slots, scalar truthiness,
height/minimum/padding/token overrides, actual viewport changes, preserved
template models and bindings, real keyboard/pointer traversal, exact raster,
scaling, lifecycle and independent x64/ARM64 package/runtime verification. Source
interaction probes retain an edited input and its original action through slot
and width updates. Native verification is in progress; no visible acceptance is
claimed by that historical hidden checkpoint. The desktop is now reconnected;
initial visible AdminShell evidence is recorded separately.

The earlier independent native x64 consumer checkpoint matches all 168 layouts and checked
paint properties with zero geometry error. Its 44 ownership/lifecycle checks
include actual hidden client resizing and scalar text in the native content
tree; 41 damaged receipts are rejected. All 251 portable tests pass. Original
slot presentations are cached so unrelated updates do not remount edited
controls. Private layout measurements are invalidated when a minimum or outer
inset changes, allowing the shell to shrink after a viewport update. Visible
input/raster and full platform acceptance remain unverified.

The extended explicit-zero regression now passes in both themes on the
AdminShell v7 package: all 44 checks pass. Both shells share a private viewport
state that yields to local values, bindings and inherited Style minima. A loaded
explicit zero observes its otherwise unnotified removal on a dispatcher tick;
observation stops on unload or template removal. The AdminShell consumer also
checks equal-valued overrides and exact one-way/two-way binding preservation
during resize. Earlier failing receipts remain preserved. Broader visible and
platform acceptance remains open.
