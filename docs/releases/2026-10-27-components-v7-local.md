# Local Windows component preview: consolidated v7

`0.1.0-components-v7-local` packages the reusable Orchard component library at
source commit `aeb338c`. Since v6, Input's catalog theme owner was corrected,
Media Library, Select and Input gained physical catalog checks, and Popover now
retries initial content focus and restores Escape focus to a composite trigger's
native action. Monitoring and Editorial remain fictional-data component demos.
This is a local preview, not an accepted inventory release or external NuGet
publication.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v7-local.nupkg` | `32f9566b24115317438ae71a56d6e4b7d8ea358f0aea0a624cc0266e8a11cde4` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v7-local-win-x64.zip` | `308f1e70ecbd3e8d68f857826662bcd6138645644b6e236c2b66e4c523f73d51` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v7-local-win-ARM64.zip` | `2e6cfc08498cf7e34d75cccc3be22546ded98c05a205028d20bbe61684ac9863` |

An isolated x64 consumer restored this exact package and passed native resource,
font, light/dark theme, layout and action smoke. Its temporary restore workspace
was removed. Its Orchard DLL SHA-256
`ff3e83795ac7184297ace7bb2ccf69b16593384fe2eb569ed8f5b132adc81da9`
matches the extracted x64 catalog byte for byte. Both catalog archives were
extracted and every file matched the published directory: 664 x64 and 660 ARM64
files. The extracted x64 catalog then passed physical Popover, Media Library,
Select and Input pointer routes in Light and Dark. Its fictional Monitoring and
Editorial demos passed 66 hidden checks. These are focused routes, not complete
per-component acceptance.

The independent ARM64 consumer **did not publish**. Its fresh restore succeeded,
but the self-contained publish hit `MSB3021` while copying ARM64 runtime files
after C: reached zero free bytes. The verifier removed its temporary restore
workspace; a 450-file, 158 MB partial app output remains. The ARM64 catalog
archive is a verified build artifact, not native ARM64 runtime evidence. There
is no successful v7 ARM64 consumer result. Data Table's performance budget and
other inventory acceptance gaps remain open. The inventory is 112 entries
started and zero accepted; that is not a completion percentage. GitHub Actions
were not triggered, and the unavailable wiki remote leaves this record in the
repository.

The [delivery checkpoint](../evidence/components-v7-local/checkpoint.json)
binds the exact reports and hashes. The old x64 `publish/publish` output could
not be removed because automatic approval review blocked deletion, so v7 used
fresh named publish directories. The same review blocked deletion of the failed
ARM64 consumer's partial app output. Keep these generated directories out of
success claims and clean them only through an authorized cleanup path.
