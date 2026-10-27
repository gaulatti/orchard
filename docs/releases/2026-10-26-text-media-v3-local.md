# Local Windows component preview: Media Library and text validity

`0.1.0-text-media-v3-local` packages the Media Library broken-full-preview
marker repair and the Input, Textarea and Field native validity behavior. These
are local preview artifacts; the component inventory is still in progress.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/current/packages/Gaulatti.Orchard.0.1.0-text-media-v3-local.nupkg` | `96f6000fb4d97fa6c7e362c6d3788781c4768f5d0825043ecd750c24b3ee8954` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-text-media-v3-local-win-x64.zip` | `47871b84cde5471b7fdb4eabb0c1e6336e44e42a7d401e6c4680309c8c198069` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-text-media-v3-local-win-ARM64.zip` | `4822bde19c7c21d48f0408861f732c5bbe6e5962def9b1a66c2766ab52985d22` |

The NuGet payload contains one AnyCPU library, one PRI, 157 source-matching
XAML files, six fonts and two images. Fresh isolated x64 and ARM64 consumers
restored and built it. The packaged x64 Media Library host passes 100 hidden
states, 38 contracts and 100 PNG audits; the independent x64 package host
passes 17 Input, Textarea and Field validity transitions. Temporary consumer
restore workspaces were removed after successful builds.

The same independently built x64 package consumer passes a focused hidden
Data Table replay with 20 semantic/ownership/unload operations at 16,384 rows
and page size 100. Its single-process next-page time is 1,727.45 ms against
the unchanged 1,000 ms local budget, so collection performance remains open.
This replay does not cover the full 120-profile matrix.

The catalog archives contain 664 x64 and 660 ARM64 files, each byte-verified
after extraction. The extracted x64 catalog passes 66 Monitoring/Editorial
hidden checks, 577 isolation guards, 100 Media Library states, 38 contracts,
two catalog themes, 102 PNG audits, and hidden Input/Textarea validity. It
decodes all 653 embedded reference resources with their original hashes.
Catalog and NuGet Orchard DLL hashes differ because they are separate builds;
each runtime was checked against its own artifact identity.

These checks used hidden x64 windows without physical input or OS setting
changes. Native ARM64 runtime, visible interaction, display/text scaling and
full source-derived visual review remain open. The strict image diagnostics
retain their failures; byte-identical raster output is not a public-component
delivery gate. No inventory entry is accepted yet: 112 started, zero accepted.
Push-triggered GitHub Actions remain paused at the user's request. The wiki
remote is unavailable, so this release note is maintained in the repository.
