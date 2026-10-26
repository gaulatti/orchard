# Windows component preview: File Input caller styles

File Input's local default bindings overrode ancestor implicit styles. It now
respects implicit caller `FontSize`/`Padding` setters and their `BasedOn` chains,
while preserving the standard style's size-dependent defaults. Changing size or
font scale retains those caller settings and the original selected-file list and
objects. The catalog includes an implicitly styled attachment example.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261025.5).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-file-input-styles-v1-local.nupkg` | `58510aeff734cc855b1fe845e49be7e616a1f57217eee926842a0755d7f95de5` |
| `Gaulatti.Orchard.Catalog.0.1.0-file-input-styles-v1-local-win-x64.zip` | `7e8ee2975681853b92cd4d058397fa4ef3ea069c8afaef8ffb9f207003ac8704` |

The source-tested x64/package library has SHA-256
`758d23980168a5381c59500336d6db14042055af41b31ebeee83a35c6b1c6efb`.
The preceding published package fails the new implicit-style regression after
passing 34 existing states and 14 earlier contracts. The repaired source, exact
NuGet and archive hosts each pass 68 visual states, 34 behavior/style contracts
and 69 damaged-report/file checks. All 34 native states and images available from
the baseline are unchanged. Source brush, geometry, visible native glyphs and
original file/callback/lifecycle assertions retain their previous thresholds.
Source catalog and independent consumers also compile for x64 and ARM64 with
zero warnings and errors. Native execution was verified on x64.

Strict browser/native crop equality still fails at 411,684 differing pixels
(maximum channel difference 227, zero identical frames). The fresh NuGet host
also differs from the source host at seven pixels (maximum channel difference
14); their state and contract data agree. All 68 archive/source PNG pairs are
byte-identical. These diagnostics are
retained separately from the caller-style repair. The tests provide original
fictional native `StorageFile` values through the public picker boundary; no
system picker or physical input is used. They do not prove those interactions.

Every catalog ZIP entry is verified against the frozen app. Its replay directory
maps verified entries to identical independently frozen files. The verification
and source-input manifests record the exact scope. Earlier unrelated suites keep
their original binary/source identities. Both fictional Monitoring and Editorial
demos remain included. Hosted Actions remain suppressed at the user's request.

Native system-picker/physical input, native ARM64 execution, actual scaling and
complete visual acceptance remain open. Narrator and OS contrast campaigns are
excluded. The inventory remains 112 entries started/in progress, zero pending
and zero accepted, with both required demos in progress. These counts are not a
completion estimate.
