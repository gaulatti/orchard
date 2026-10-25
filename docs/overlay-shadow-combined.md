# Combined overlay shadow attachment

`BleeckerCombinedShadowLayer` is an internal component-library preview. It
coordinates the existing near and far GPU primitives on one loaded
`BleeckerOverlaySurface`. The public component still uses its original renderer.
The remaining geometry, scaling and original-image gates continue to govern
promotion; a successful combined attachment does not establish full parity.

The coordinator saves the original two sprites, their visibility, their clips
and the container clip. Each candidate is inserted immediately above its own
original sprite. Composition collections enumerate from bottom to top, so the
near replacement stays behind the far replacement. The original sprite becomes
hidden while its candidate owns that slot. Standalone attachments retain their
exclusive original-graph requirement.

Two owned geometric clips share the component's original geometry. Each has a
separate translation for its original sprite's offset. This lets a remaining
default layer stay clipped correctly when the other candidate is disposed.
The last release restores the original clip and visibility references. When
the component clears or unloads its graph, release avoids accessing disposed
original visuals. The coordinator never disposes the borrowed geometry or
Win2D's shared device.

The source preview inherits both primitives' explicit limitations: scale one,
uniform nonnegative corners erased by the far layer's spread, no transformed
geometry and the near primitive's remaining small rounded-mask limitations.
The zero-radius near path also supports tiny rectangles. Empty far casters
contribute transparent pixels while retaining ownership for later resizes.
It retains caller backgrounds,
bindings, content and opacity. Unsupported input fails explicitly.

`OverlayCombinedFixture` is registered in the catalog and independent package
consumer. Its six original combined images are copied byte for byte from the
pinned source manifest, including both geometries and light, dark and transparent
backdrops. The fixture checks stacking, geometry round trips, partial disposal,
constructor rollback, duplicate ownership, resource regeneration, default
restoration, reattachment and automatic unload. The independent
`verify-overlay-combined.mjs` auditor checks capture provenance, pixel bytes,
interior exclusion, ownership and original PNG comparisons with zero channel
tolerance.

The subsequent unpackaged empty-caster source extension verifies all 36 empty
combined zero-radius rectangle images exactly, including one-pixel surfaces,
one empty axis and the fractional snapping boundary. The combined rounded
40-by-120 cases expose the remaining near-mask differences and are not accepted.
These cases supplement the six original combined image gates below.

The current source replay retains the original 16 differing pixels and all six
geometry roundtrips, partial disposals, resets, default restorations and unloads.
One earlier run failed a transparent geometry roundtrip at 1,992 pixels; a fresh
unchanged replay did not reproduce it. The combined capture fixture now awaits
the pinned compositor's `RequestCommitAsync` after layout. Two subsequent full
runs pass every lifetime/roundtrip check, each recording 156 commit requests;
their original 16-pixel image gate still fails. A third attempt ran out of disk
space during the identity copy before launch and supplies no native test result.
The earlier failure remains retained and universal capture stability is not
established. The versioned auditor rejects 46 corruptions, including a missing
commit barrier claim. The pinned package projection confirms this API; its
[official documentation](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.compositor.requestcommitasync)
describes initiating a commit cycle without promising universal capture
stability. These source results do not replace the delivered preview below.

## Verified local preview

`0.1.0-overlay-combined-v1-local` is verified through the source catalog, a fresh
independent NuGet consumer and the extracted catalog archive. Each host passes
six combined cases, five constructor/duplicate rejections, six resource resets,
six geometry round trips, both partial-disposal orders, restoration and unload.
The combined auditor verifies 48 captures per host and rejects 45 corrupted
reports. The lifetime auditor rejects a further 37 corruptions.

All four lifetime modes pass in all three hosts: 81 suspension transitions,
bound solid/gradient/transparent paints, template changes, reparenting and cache
cleanup. Each host passes 100 default component states and 168 logical hotkey
cases. Source and archive preserve both fictional demos with 30 host cases and
16 catalog states each. Root and independent consumers build for x64 and ARM64;
495 portable and ten Windows text tests pass. Native ARM64 runtime, physical
input, actual device-loss events and measured collection costs remain open.

The preview differs from the six original combined PNGs at 16 pixels per host
(four fixture-light, ten dialog-light and two dialog-transparent pixels). The
public default is unchanged and still differs at 90,433 pixels per host.
Standalone far images retain eighteen differing pixels; fractional near images
are 55/56 exact, with two alpha differences in one image. The earlier 54/56
status count was incorrect; the underlying images have not changed.

All 1,082 retained standalone/default capture files are byte-identical to the
preceding delivery. All 1,210 selected captures per host are byte-identical
across source, NuGet and archive; duplicate captures share storage without
changing bytes. Consumer apps are retained in verified ZIPs, and the tested
catalog ZIP retains all 1,360 files after retiring its expanded copy.

The preview's zero-tolerance image gate remains failing. Broader geometry,
scaling, customization and public renderer integration remain required. All
112 inventory entries and both fictional demos remain in progress and
unaccepted. No completion percentage is inferred from entries started.

Microsoft documents the ordering and sibling insertion semantics in
[VisualCollection.InsertAbove](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.visualcollection.insertabove?view=windows-app-sdk-2.0).
The repository pins Windows App SDK 2.5.1; its version-specific documentation
view is unavailable, so compilation and native fixtures must also verify the
installed API behavior. No framework or package version is changed.
