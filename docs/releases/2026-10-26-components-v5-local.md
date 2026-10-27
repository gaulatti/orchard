# Local Windows component preview: consolidated v5

`0.1.0-components-v5-local` packages the current reusable Orchard library at
commit `c945e38`. Since the v4 package, source changes cover Navigation Menu
motion and text scaling, Toggle Group selection notification, and File Input's
long-filename display. This is a local preview, not an accepted inventory release
or a published GitHub release. The previously published preview remains linked
from the root README.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v5-local.nupkg` | `6498f772034dd9f66ba9be3270d8b129289fb50657709d6bd724e3a772261a8f` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v5-local-win-x64.zip` | `65cb51a5f3b5e877f1f4ff15c31033b9b3db789a3e9e2dd4bc004e6095ec17bb` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v5-local-win-ARM64.zip` | `a9d82744ceb2d80eda5d4558240b4d85be84f17c0ee6d2b8145bd7b9fe649512` |

The package payload audit passed: one AnyCPU library, one PRI, 156 themes and
157 loose XAML files matching current sources, six fonts, two images and the
current README. A fresh isolated x64 NuGet consumer restored, published and
passed its native light/dark resource, layout and action smoke test. A separate
isolated ARM64 consumer restored and built the same package; its runtime was not
executed on this x64 host. Both temporary restore workspaces were removed.
The x64 and ARM64 catalog archives contain 664 and 660 files respectively; all
extracted bytes match their ZIP entries. The x64 catalog and independent
consumer use the same packaged Orchard DLL SHA-256,
`9c50496b46f29fd36b0de029c484013257c5ef880013671d33dc9458081c2ee6`.
The source-built ARM64 catalog has its own DLL SHA-256,
`2b4a030fc173fd7866942701e9670547ee51b0a6d6054d28c95ca56ed2d7866a`.

The extracted x64 catalog passed File Input's 68 hidden visual states and 34
selection/lifecycle contracts in both themes. The exact NuGet consumer passed
the same 68 states and 34 contracts. Both show the long filename suffix while
retaining the full original value. Their state and contract data agree; the
strict host image comparison remains **failed** at seven pixels (maximum channel
difference 14, 66/68 pairs byte identical). The browser/native strict image
comparison also remains failed. Neither diagnostic is relabeled as passing.
The extracted x64 catalog separately passed 66 hidden Monitoring and Editorial
demo checks with fictional local data. These are hidden runtime checks, not a
claim of complete visible interaction or visual acceptance.

Native ARM64 catalog/consumer runtime, fresh real-picker interaction for these
v5 bytes, remaining component visual and behavior checks, measured collection
performance, and final package acceptance remain open. The library inventory
remains 112 entries started, zero accepted. The push-triggered Actions workflow
remains disabled at the user's request; local checks above do not claim CI.
The wiki remote was unavailable, so this release record is in the repository.
