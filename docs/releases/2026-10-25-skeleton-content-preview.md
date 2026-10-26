# Windows component preview: Skeleton caller content

The pinned Skeleton accepts children, but the previous Windows control exposed
no content slot. This preview makes `BleeckerSkeleton` a `ContentControl` and
presents the original caller subtree alongside its loading surface. It supports
content bindings, templates/selectors, padding and content alignment. Caller
edit state, actions, accessible child peers and original model context survive
template changes and remounts. The catalog includes an interactive content example.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261025.1):

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-skeleton-content-v1-local.nupkg` | `2e22664d22d46d18825610ebd668ae113d28ee6564b1f666930c3cc1741b0d34` |
| `Gaulatti.Orchard.Catalog.0.1.0-skeleton-content-v1-local-win-x64.zip` | `b4306f92a1798b9ee18d2a15a41008f6e9460f489f4f4e1e6140c09fa7e3522f` |

The source-tested x64/package library has SHA-256
`c0856b7251fc8e42bf2906855fe50779c5bc827537e937364b0bd683d081c939`.
Source ARM64 compilation produces a separate architecture build; independent
ARM64 consumption compiles against the exact packaged library.

Qualification covers 36 default states and 16 caller-content checks per x64
source and independent NuGet host, both themes and directions. Each host retains
40 images and rejects 18 damaged reports. The previous published library fails
the missing-content-slot regression. Both source and independent consumer compile
for x64/ARM64. All 496 portable tests pass, with no failures or skips. The two
fictional Monitoring/Editorial demos pass 66 background checks and 577 isolation
guards; 25 incorrect demo reports are rejected. Hosted Actions remain suppressed
at the user's request; local checks are reported separately from CI.

The catalog archive verifies every entry against the frozen app. For runtime
qualification, verified archive entries are mapped to identical immutable files
in a separate app directory, avoiding another full runtime copy. The release's
verification and source-input manifests describe the exact evidence scope.
Binaries were built from verified working inputs before the source commit/tag;
the source-input manifest records those file hashes.

Normal pulse timing is unverified: current Windows settings and all 51 retained
Skeleton traces use reduced motion. OS settings were not changed. Caller-opacity
RTB samples differ from CSS isolated-group compositing; the native behavior and
raw disagreement are retained. On-screen compositor/shadow raster, physical
input, full scaling, native ARM64 execution and final component acceptance remain
open. Exact-raster research is not a blanket delivery gate.

The inventory remains 112 entries started/in progress, zero pending and zero
accepted. Both required demos remain in progress. These counts are not a
completion estimate. This is a component-library development preview.
