# Orchard components v11 local preview

Version `0.1.0-components-v11-local` packages the nested Popover trigger repair
and the bounded, opaque Monitoring review demo. The [Popover checkpoint](../evidence/popover-nested-trigger-v1/checkpoint.json)
and [Monitoring checkpoint](../evidence/monitoring-review-surface-source-v1/checkpoint.json)
record the observed defects and tests.

The local artifacts are `artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v11-local.nupkg`,
`artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v11-local-win-x64.zip`,
and `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v11-local-win-arm64.zip`.
The package SHA-256 is `a19736af4b0ba3faf5dc441776b2ed22120b26e6aa2b5e3bb7083180ed6d0b50`;
the x64 and ARM64 catalog ZIP hashes are
`2a38db427827f25b9eecbe78f7ad2bd550591bab98d6e2c229c292e34ac6d7ec`
and `d2b158aecc95ae53e9d15e6b8d78abce5d5d54796845b151585087a7a11516e1`.
All 664 x64 and 660 ARM64 extracted catalog files match their archives.

A clean independent x64 NuGet consumer publishes and runs successfully. A clean
ARM64 consumer compiles with an Arm64 apphost; native ARM64 execution is not
verified on this x64 machine. The x64 catalog DLL exactly matches the NuGet
DLL. Its extracted archive passes the Light/Dark physical nested Popover route
and the 72-check fictional Monitoring/Editorial hidden gate with 589 isolation
guards. No inventory entry is accepted yet: 112 entries are started, zero
accepted. Broader component visual, scaling, input and native ARM64 checks
remain open. GitHub Actions were not triggered for this local build.
