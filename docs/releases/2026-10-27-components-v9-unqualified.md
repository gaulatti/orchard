# Components v9 local preview — unqualified

`0.1.0-components-v9-local` packages the current reusable WinUI 3 library and
catalog after the Monitoring demo's themed post-card fix and the inherited Icon
Button pressed-scale/shift fix. Both fictional Monitoring and Editorial demos
remain in the catalog. This is a local preview, not an accepted release.

The NuGet file is
`artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v9-local.nupkg`
(SHA-256 `b4b6eecfd9a5bf072e060833f35f2d00b3f3eb085fae2b14481e9a424df935a4`).
An independent consumer restored it with a clean package cache, published
self-contained for x64 and ran its native resource, font, theme, layout and
action checks. The same package restored and published self-contained for
ARM64 with an ARM64 apphost; native ARM64 execution was not available. Both
temporary consumer caches were removed by the package verifier.

The runnable catalog archives are
`artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v9-local-win-x64.zip`
(SHA-256 `2e0560bb13361b1aa366f2e6f47e5cd3fe84dc85e50ece85a5baf0c2c83bd1e9`)
and
`artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v9-local-win-arm64.zip`
(SHA-256 `a837ad65bff5f9629425ea07ada6d018794c06d47be478e5227ed2ed0fb29677`).
Fresh extraction matched every one of the 664 x64 and 660 ARM64 files by
SHA-256. Each contains 114 compiled XAML fixtures, both fictional demos, and
local .NET and WinUI runtime files. The extracted x64 catalog passed physical
Theme Toggle pointer/Space/Enter and rejected-Invoke checks in both themes;
its 134 source/native geometry states passed. Its hidden native Monitoring and
Editorial checks passed 68 cases with 581 isolation guards. The x64 catalog
Orchard DLL matches the independently consumed NuGet DLL byte for byte.

The [delivery checkpoint](../evidence/components-v9-delivery-v1/checkpoint.json)
and [x64](../evidence/components-v9-delivery-v1/catalog-x64-manifest.json) /
[ARM64](../evidence/components-v9-delivery-v1/catalog-arm64-manifest.json)
manifests retain exact hashes and distinguish runtime from archive evidence.
The [pressed-state checkpoint](../evidence/theme-toggle-press-v1/checkpoint.json)
retains source and native screenshots. Full raster appearance, normal motion,
actual scaling, broader component interactions and native ARM64 runtime remain
open. The inventory is 112 entries started, zero accepted; entries started is
not a completion percentage. Commits use `[skip ci]` and no workflow was
manually dispatched, as requested; required hosted CI remains pending. The public wiki
remote is unavailable, so this documentation is kept in the repository.
