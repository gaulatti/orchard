# Components v8 local package preview — unqualified

`0.1.0-components-v8-local` is a 1.73 MB NuGet preview at
`artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v8-local.nupkg`
(SHA-256 `a37aba0cf848effda102e1479cfc2a8965295cb68996b95ed184ecb7fc121121`).
It was packed without rebuilding from the source-built x64 library that passed
the [Monitoring visible focus check](../evidence/monitoring-focus-source-v1/checkpoint.json).
The package's single AnyCPU IL `Orchard.dll` has SHA-256
`0dc1d2b6f0683a36d757751bc64be4e7998db3d644a02f72742297b2150104cb`,
matching that source-built catalog. Package structure and bundled assets passed
the repository verifier's checks before its restore step.

The clean-cache independent consumer could not restore: C: ran out of space
while copying a Windows SDK reference assembly. Its temporary restore workspace
was removed. A separate consumer source copy outside the repository restored
the exact v8 package from the local feed using the machine's cached third-party
dependencies, then compiled for x64 with zero warnings or errors. Its copied
Orchard DLL matches the package. The first experimental executable exited during
`MainWindow.InitializeComponent` with a WinUI `XamlParseException`.

The [host diagnostic](../evidence/components-v8-package-preview-v1/host-diagnostic.json)
now identifies a malformed resource index in that experimental build. The build
used a custom absolute `OutputPath`; its PRI keys end in
`…appApp.xbf` and `…appMainWindow.xbf` instead of the required `App.xbf`
and `MainWindow.xbf`; the qualified v7 consumer has the latter keys. The
prefixed names point to the custom output-path invocation as the likely cause.
Replacing only the Orchard DLL with v7 bytes reproduced the same parse failure, then the
v8 DLL was restored by hash. Enabling Windows App SDK self-contained output in
the same custom-path build also retained the malformed keys and failure. This
invalid host cannot attribute a defect to the v8 library. The repository's
normal [package verifier](../../scripts/verify-package.ps1) uses
`dotnet publish -o`, avoiding that experimental output-path command.

A default-path Rebuild replaced the stale `obj/embed` names. Its PRI now indexes
`App.xbf` and `MainWindow.xbf`. The independently restored consumer loads the
exact v8 Orchard DLL and [passes its full x64 native check](../evidence/components-v8-package-preview-v1/v8-x64-consumer-runtime.json)
with exit code 0: package resources, fonts, Light/Dark themes, layout and
native action. A separate normal launch shows a populated
[consumer window](../evidence/components-v8-package-preview-v1/v8-x64-consumer-visible.png).
This is a framework-dependent .NET/Windows App SDK run with the installed 2.5.1
runtime and shared third-party dependency cache. It is valid x64 independent
NuGet-consumer runtime evidence; it does not prove clean-cache restore or
self-contained delivery. See Microsoft's [deployment modes](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps)
for the separate Windows App SDK and .NET self-contained settings.

A [self-contained consumer overlay](../evidence/components-v8-self-contained-overlay-v1/checkpoint.json)
now runs the exact v8 package DLL in the independently built v7 x64 consumer
host. The v7 and v8 packages have the same 186 entries; only the version
metadata and Orchard DLL differ. The v7 host was published with .NET and
Windows App SDK self-contained settings, and the separate v8 directory retains
its 530 unchanged host files as verified hardlinks. Its local `coreclr.dll`,
`hostfxr.dll` and WinUI runtime are present. The executable exits 0 after
checking package resources, fonts, Light/Dark themes, layout and a native
action with the exact v8 DLL. This directly verifies that self-contained host
runtime path. It does not prove a fresh v8 restore, compilation or publish;
those clean delivery steps remain open while C: lacks space.

The local v8 x64 catalog preview ZIP is now available at
`artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v8-local-win-x64.zip`
(SHA-256 `40da12696757f38414de8115bc5d1d81a50cc36add1a48a25153405989f74b52`).
Its [manifest](../evidence/components-v8-package-preview-v1/v8-x64-catalog-stream-manifest.json)
records all 664 entries and their SHA-256 hashes. Every ZIP entry was streamed
back and matched to the source-built catalog. All 114 expected XAML fixtures are
present. Only the catalog executable, catalog DLL/PDB and Orchard DLL/PDB differ
from the v7 archive. The ZIP retains both fictional component demos and the
runtime files from the verified v7 deployment file set.

An [archive-equivalent overlay replay](../evidence/components-v8-package-preview-v1/checkpoint.json)
replaced those five files in the hash-verified v7 extraction with the v8 ZIP
bytes. The Monitoring review passed its physical pointer/keyboard focus checks
in Light and Dark; the v7 extraction was then restored by hash. Both screenshots
were inspected.

A separate [low-disk v8 catalog materialization](../evidence/components-v8-linked-catalog-v1/checkpoint.json)
now verifies all 664 destination files against that archive manifest. It hardlinks
659 byte-identical v7 files and writes the five changed binaries from the v8 ZIP
into `artifacts/current/components-v8-catalog-linked-x64-v1`, without modifying
the v7 deployment. This adds 21,502,936 bytes of changed payload. The new
directory runs directly: both fictional Editorial and Monitoring demos pass
physical input checks in Light and Dark, and their four screenshots were
inspected. Icon Button, Tooltip and nested Modal also pass focused physical
checks in both themes against the same exact v8 Orchard DLL. The first
one-step Icon Button hover diagnostic failed; its report and screenshot remain
recorded, while the swept-pointer run passed. These checks establish native
behavior for the archive-identical file set. They do not establish full visual
or component acceptance. The v8 ZIP has not been unpacked independently into
a fresh directory; C: still lacks room for that and clean-cache qualification.

This package and catalog remain **unqualified**. A fresh v8 self-contained x64
restore/publish, a fresh ZIP extraction and replay, an ARM64 catalog archive and
native ARM64 runtime remain open. The v7 catalog still has the Monitoring focus
defect; v8 has not replaced it as a release. The
[checkpoint](../evidence/components-v8-package-preview-v1/checkpoint.json)
records hashes, verified checks and remaining failures. The inventory remains
112 entries started and zero accepted; that is not a completion percentage.

The attempted cleanup of generated artifacts was rejected by automatic approval
review as “blocked by policy,” so those files remain. C: still has less than
150 MB free after the low-disk runtime checks.
The public wiki remote is unavailable, so this release note is maintained in
the repository.
