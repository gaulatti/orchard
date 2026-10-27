# Orchard Windows preview — nested Popover and fictional review demo

This `0.1.0-components-v11-local` preview packages the nested Popover trigger
repair and the bounded, opaque Monitoring review demonstration. The release
tag targets source commit `e4720490ef6552c2f62f143dca2f9c24788f6e7d`.
Monitoring and Editorial remain fictional component-library demos.

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-components-v11-local.nupkg` | `a19736af4b0ba3faf5dc441776b2ed22120b26e6aa2b5e3bb7083180ed6d0b50` |
| `Gaulatti.Orchard.Catalog.0.1.0-components-v11-local-win-x64.zip` | `2a38db427827f25b9eecbe78f7ad2bd550591bab98d6e2c229c292e34ac6d7ec` |
| `Gaulatti.Orchard.Catalog.0.1.0-components-v11-local-win-arm64.zip` | `d2b158aecc95ae53e9d15e6b8d78abce5d5d54796845b151585087a7a11516e1` |

The exact package DLL runs in an independent x64 consumer and matches the
extracted x64 catalog DLL. The extracted x64 catalog passes Light/Dark physical
nested Popover pointer, focus and Escape checks, and both fictional demos pass
72 hidden checks with 589 isolation guards. The v11 package's hidden Popover
audit passes 122 reference states and rejects 20 damaged-evidence cases. All
664 x64 and 660 ARM64 catalog archive entries match their extracted files;
the ARM64 catalog and independent consumer compile, but have not run natively.

The original v11 automated nested screenshots were taken before the child
surface necessarily painted. Later source-only Light/Dark captures show the
settled surface, and a later source-only fixture passes physical confirmation
modal pointer and ordered Escape checks. These fixture changes do not change
the package or ZIP hashes. Complete visual, scaling, performance and ARM64
runtime acceptance remain open. All 112 inventory entries are started and zero
are accepted; this is not a completion percentage. GitHub Actions runs remain
paused at the user's request. The public wiki remote is unavailable, so the
evidence and usage documentation live in the repository.
