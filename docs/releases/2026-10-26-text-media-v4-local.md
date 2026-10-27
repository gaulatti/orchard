# Local Windows component preview: Media Library RTL marker

`0.1.0-text-media-v4-local` adds the source-verified Media Library RTL failed-
thumbnail marker placement to the prior text and media preview. It remains a
local component-library preview, not an accepted inventory release. The packaged
component source is commit `e55379b410c3d755c45fc5f5342d49a97a3461d9`.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/current/packages/Gaulatti.Orchard.0.1.0-text-media-v4-local.nupkg` | `8e870673c0dcbefc2cdc9e60c0a9df6581c8de21a5b0998b5c48009b8a1c05eb` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-text-media-v4-local-win-x64.zip` | `c36327a0115edd84fe13a0c08f7a6c2bf885e1994e2480a59ee957cf3b69280c` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-text-media-v4-local-win-ARM64.zip` | `3d61c1c0199e9f4b5f8ade32752180f4271e1e5d504d3a57a8c8fb9a80677e36` |

The package contains one AnyCPU Orchard library, one PRI, all 157 tracked XAML
files with source-matching hashes, six fonts and two images. Fresh isolated x64
and ARM64 consumers restored and built the exact package; their temporary
restore workspaces were removed. The packaged x64 host passed 100 hidden Media
Library states, 38 behavior contracts and 100 PNG checks. The RTL failed-image
marker is at x=951 in the x=746..968 tile in both themes. Source and packaged
Orchard DLL hashes differ because they were separate builds; each runtime was
verified against its own bytes.

The x64 and ARM64 catalog archives contain 664 and 660 files respectively;
every extracted file matches its ZIP entry. The extracted x64 catalog passed
100 Media Library states, 38 contracts, 102 PNG checks, 66 Monitoring/Editorial
demo checks and 577 isolation guards. It also decoded all 653 embedded
reference resources against their pinned originals. The extracted x64 catalog
and package consumer have the same Orchard DLL SHA-256,
`1ac7e3d5ac2095e7a26db55ceb724dcc054ef2580dbda70173ebd88c90859f6b`.

The same extracted x64 catalog and independent package consumer subsequently
passed the hidden Skeleton caller-content contract: each host verified 36 default
states, 16 caller-content contracts, two themes and directions, 40 captures and
18 rejected damaged reports. This is current reduced-motion evidence for the
exact package bytes, recorded in the [Skeleton contract](../skeleton-reference.md).
Normal pulse timing, compositor shadow raster and display/text scaling remain open.

Toggle Group's hidden lifecycle contract also passes on the same packaged DLL:
the extracted x64 catalog passes 62 checks and the independent NuGet consumer
passes 60, with 70 observations per host. These cover selection bindings,
standalone styles and command gating, and rejected retired/disabled UI Automation
actions. Physical input, visual parity, scaling and ARM64 runtime remain open;
see the [Toggle Group contract](../toggle-groups.md).

Footer's pinned hidden layout/paint audit passes on the extracted x64 catalog
and independent package consumer. Each host checks 192 frames, 15,216 source
geometry values and 1,890 paint values with zero error, including the pinned
long/unbroken text cases. The package auditor rejects 50 damaged reports. See
the [Footer contract](../footer.md); visible interaction, full raster review,
scaling and native ARM64 runtime remain open.

A separate general x64 package smoke test **failed** at the Data Table toolbar
held-Reset geometry assertion. That fixture activates a foreground window and
sends native keyboard input; it was run outside the authorized desktop window
and will not be repeated without renewed desktop availability. The process
exited and its isolated temporary project was removed. This failed check is
unresolved; the successful Media Library package audit does not supersede it.

Native ARM64 runtime, foreground input, scaling, full component-by-component
visual review and Data Table performance remain open. The inventory remains
112 started and zero accepted; 100% describes entries started, not completion.
The push-triggered Actions workflow is paused at the user's request. The wiki
remote is unavailable, so this record is kept in the repository.
