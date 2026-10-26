# Windows component preview: Status Badge caller metadata

Status Badge now respects caller automation Name and HelpText values, including
explicit empty strings, live bindings, inherited styles and ancestor implicit
styles. An empty caller label association stays authoritative. Clearing an
override restores the applicable default. The catalog demonstrates caller-owned
metadata, and this archive includes the accumulated Status Badge and Separator
hidden regression commands.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261026).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-status-badge-metadata-v1-local.nupkg` | `95f529dcce0ad8140b3c00ecc0580bde860e0a1c5f572e8bf6a18f2daad63736` |
| `Gaulatti.Orchard.Catalog.0.1.0-status-badge-metadata-v1-local-win-x64.zip` | `f48c708604b4ae3276b7f1f951e1c524ae9e9e15478798cb03acec470a44ca54` |

The source-tested library is
`87755ce30a712f4ce61957ec9b560bd3cdaee43aa8bf1c78408731b349107985`.
The preceding public library fails the caller-empty Name/HelpText regression
after nine visual/layout states. The repaired source, exact NuGet and archive
hosts each pass 18 Status Badge visual/layout states, 26 caller metadata and
lifecycle contracts, and 11 damaged-report checks. The nine baseline snapshots
and PNGs are unchanged. All three hosts also pass Separator's 32 rendered states,
eight caller/lifecycle contracts and ten damaged-report checks.
Source and independent consumers compile for x64 and ARM64. Source/NuGet state
and contract data agree; 16 of 18 Status Badge PNG pairs are byte-identical.
The two wide dark cases differ at one corner pixel each (maximum channel
difference five), retained as a failed zero-tolerance diagnostic. All 18
archive/source PNG pairs match. Complete visual acceptance remains open.

The catalog archive verifies all 550 entries. Its replay directory maps each
verified ZIP entry to a byte-identical independently frozen file; it is not a
second full physical extraction. Verification and source-input manifests retain
the exact build/runtime scope and original identities of unrelated older suites.
Both fictional Monitoring and Editorial component demos remain included.

Native runs are x64, hidden, and use no hardware input or OS setting changes.
Compilation for each architecture is recorded separately from native execution.
Actual text/display scaling, current native ARM64 execution, broader glyph/gamut
review and collection performance acceptance remain open. Narrator and OS
contrast campaigns are excluded. Actions are suppressed during delivery at the
user's request. The wiki is unavailable, so documentation is maintained in the
repository.

The inventory remains 112 entries started/in progress, zero pending and zero
accepted. Both required demos remain in progress. These counts do not estimate
completion.
