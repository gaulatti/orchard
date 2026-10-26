# Windows component preview: Kbd caller names and scaling

Kbd now respects caller automation names, including empty values, live bindings,
BasedOn styles and ancestor implicit styles. An associated label supplies its
name when no caller Name overrides it; an empty associated label stays empty.
Clearing an override restores the generated key/group name. Content and Keys
bindings remain consumer-owned through changes, template replacement and remount.
The catalog includes an explicit empty-name example.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261026.3).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-kbd-metadata-scale-v1-local.nupkg` | `a920900fde229aa982b7819dc8b4bc173bd51aca6e277b4174325cd7ce1e72c8` |
| `Gaulatti.Orchard.Catalog.0.1.0-kbd-metadata-scale-v1-local-win-x64.zip` | `81a42e53171eab84cd9145ac18250ed169fdc2ebd64eb7f80b0d3e163718e93c` |

Library SHA-256:
`83dc48dd8df9ab75dfa529d1d29f661a25b39e5ebfee197f7bc4ba25174bf29c`.
Source, exact independent NuGet and archive hosts each pass 64 alignment states,
16 content/lifecycle contracts, 64 caller-name contracts and eight actual native
scale frames. Source and archive also verify the registered catalog example in
both themes. The scale frames cover 100%, 125%, 150% and 200% in both themes,
checking actual XamlRoot scale, physical PNG dimensions, default keycap/group
names and geometry, and caller content against native Border layout.

The baseline is rejected because a caller-empty name returns `Esc`. The first
repair exposed another real failure: base name resolution replaced an empty
associated label with content. The final repair reads the associated label peer
directly. All 64 original alignment states, 16 content contracts and eight scale
observations are retained; all 72 baseline PNGs are byte-identical. This scoped
result does not establish full browser/native glyph or fallback parity.

Source and independent 159-file consumers compile for x64 and ARM64. Native runs
are hidden x64 checks with no hardware input, activation or OS settings changes.
Actual monitor transitions, Windows text scaling, native ARM64 execution of the
name repair and remaining typography/lifecycle acceptance remain open. Earlier
unaffected suites retain their original binary and source identities. Historical
strict raster failures remain recorded; they do not gate this focused repair.

The catalog verifies all 550 ZIP entries. Replay maps each entry to a byte-identical
independently frozen file rather than duplicating the full extraction. Both
fictional Monitoring and Editorial component demos remain included. Actions are
suppressed for publication and their prior settings restored. The wiki is
unavailable; documentation is maintained in the repository.

The inventory remains 112 entries started/in progress, zero pending and zero
accepted; both required demos remain in progress. These are not completion
estimates. Narrator and OS contrast campaigns remain excluded.
