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
Orchard DLL matches the package. The framework-dependent executable exited
during `MainWindow.InitializeComponent` with a WinUI `XamlParseException`.
That run does not qualify native runtime. The cause has not been established;
the previously qualified v7 consumer used a self-contained publish.

This package is **unqualified**. There is no v8 catalog archive, independent
self-contained x64 runtime result, or native ARM64 result. The v7 catalog still
has the Monitoring focus defect; v8 has not replaced it as a release. The
[checkpoint](../evidence/components-v8-package-preview-v1/checkpoint.json)
records hashes and failures. The inventory remains 112 entries started and
zero accepted, which is not a completion percentage.

The attempted cleanup of the new shared-cache consumer workspaces was rejected
by automatic approval review as “blocked by policy,” so those generated files
remain. C: had about 415 MB free after the attempts. The public wiki remote is
unavailable, so this release note is maintained in the repository.
