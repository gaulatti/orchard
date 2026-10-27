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

This package is **unqualified**. There is no v8 catalog archive, independent
self-contained x64 runtime result, or native ARM64 result. The v7 catalog still
has the Monitoring focus defect; v8 has not replaced it as a release. The
[checkpoint](../evidence/components-v8-package-preview-v1/checkpoint.json)
records hashes, the repaired-host pass and remaining failures. The inventory remains 112 entries started and
zero accepted, which is not a completion percentage.

The attempted cleanup of the new shared-cache consumer workspaces was rejected
by automatic approval review as “blocked by policy,” so those generated files
remain. The later x64 consumer builds reduced free space to about 247 MB. The public wiki remote is
unavailable, so this release note is maintained in the repository.
