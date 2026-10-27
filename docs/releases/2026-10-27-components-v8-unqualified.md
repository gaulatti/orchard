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
were inspected. This establishes native behavior for the exact v8 archive file
set without a second full extraction. The v8 ZIP itself has not been extracted
and run from a fresh directory because C: has only about 143 MB free.

This package and catalog remain **unqualified**. Independent self-contained x64
runtime, a fresh ZIP extraction and replay, an ARM64 catalog archive and native
ARM64 runtime remain open. The v7 catalog still has the Monitoring focus defect;
v8 has not replaced it as a release. The
[checkpoint](../evidence/components-v8-package-preview-v1/checkpoint.json)
records hashes, verified checks and remaining failures. The inventory remains
112 entries started and zero accepted; that is not a completion percentage.

The attempted cleanup of the new shared-cache consumer workspaces was rejected
by automatic approval review as “blocked by policy,” so those generated files
remain. After the x64 consumer builds and preview ZIP, C: has about 143 MB free.
The public wiki remote is unavailable, so this release note is maintained in
the repository.
