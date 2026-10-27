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

The same v4 NuGet package also passes a later focused hidden Footer public-slot
check: an independent consumer and source-built catalog each pass 12 contracts
and 205 isolation guards, including original editors at 640.5- and 768.5-DIP
host widths. Their auditors reject 28 and 24 damaged receipts. This test adds
no library change or visible-input claim; see the [Footer contract](../footer.md).
The shared fixture also compiles in the ARM64 catalog; ARM64 runtime was not run.
The [small receipts](../evidence/footer-fractional-v1/README.md) remain in Git
after disposable build output is cleared.

A separate general x64 package smoke initially failed at Data Table held-Reset
geometry. After the user renewed desktop-test authorization, the unchanged
package passed one fresh independent foreground consumer run and two direct
runtime repeats with a corrected
fixture. The fixture waits for two presented frames before measuring held and
released Reset, and permits up to half a physical pixel of native popup-origin
snap plus 1/32 DIP of measurement tolerance. The original failure, a separate
overstrict popup-alignment failure, the final pass and two direct repeats are
retained in the [consumer checkpoint](../evidence/data-table-pagination-pixel-snap-v1/checkpoint.json).
The fresh isolated restore workspace was removed. These changes qualify only
the general x64 consumer smoke; no Orchard binary or package bytes changed.

The same packaged Orchard DLL also passed two focused x64 catalog runs with a
real Windows File Input picker. Pointer and Space activation opened dialogs
owned by the catalog window; a fictional local file reached the original-file
callback, Tab moved focus between inputs, Escape canceled while retaining the
existing selection, and Clear emptied its automation value. The repeatable
[desktop checkpoint](../evidence/file-input-desktop-v1/checkpoint.json) records
the exact binary and the earlier script timing failures. This is a catalog-host
foreground check; it does not establish the full File Input input/visual matrix.

Native ARM64 runtime, broader foreground input, scaling, full component-by-component
visual review and Data Table performance remain open. The inventory remains
112 started and zero accepted; 100% describes entries started, not completion.
The push-triggered Actions workflow is paused at the user's request. The wiki
remote is unavailable, so this record is kept in the repository.
