# Orchard Windows preview 2 — Footer brand foreground

The [published prerelease](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261027.2)
contains `Gaulatti.Orchard.0.1.0-preview.20261027.2.nupkg` (SHA-256
`62e3d1f9f4d78700520ceaa5dcf0d664877484e359bfc9f9f9bbb8119ad0b1df`).
Its tag targets source commit `9349b598b803754bbeb45d95f2d23ec9baeac0d3`.
GitHub reports the same digest for the uploaded asset. This preview includes the
Footer fix that colors its nested BrandLockup with the active caller link or
theme foreground, and the catalog selector that retains all three Footer demos.

The package has the same 182 non-metadata payload entries as the locally tested
Footer package, including the exact Orchard DLL. A package-reference x64
consumer restored the new version, built with zero warnings or errors, and
passed its normal Light/Dark resource, theme, layout and native-action runtime
check. The package's hidden Footer public-contract probe passed 12 scenarios
and 205 isolation guards. Its strict isolated-consumer auditor did not pass:
that auditor requires a fresh-checkout/cache cleanup receipt, which this
in-repository consumer does not produce. See the
[checkpoint](../evidence/footer-brand-preview-v2/checkpoint.json) and retained
runtime reports.

Subsequent [package-consumer foreground evidence](../evidence/footer-package-physical-input-v1/checkpoint.json)
passes Light/Dark physical pointer and Enter input through the original Footer
item, a caller-rendered link, retained focus and caller input.

The same release now includes complete catalog ZIPs for Windows x64
(`Gaulatti.Orchard.Catalog.0.1.0-preview.20261027.2-win-x64.zip`, SHA-256
`fac0190b32d174595eddcef504a47f5097fdd934219dbf310cc2d333ef90710e`)
and Windows ARM64 (`Gaulatti.Orchard.Catalog.0.1.0-preview.20261027.2-win-arm64.zip`,
SHA-256 `ee3f8ed7a9f8688548f048c6cdb4e4bbcac63316f10c0433d5578581a7adfa6c`).
Every ZIP entry was read back and matched to the current build output. The x64
catalog contains the exact Orchard DLL in the NuGet package; its current build
output passed Light/Dark physical pointer and keyboard checks for Monitoring,
Editorial Search and Editorial Edit. The ARM64 catalog built without warnings
or errors. See the [catalog checkpoint](../evidence/catalog-preview-v2/checkpoint.json)
and archived manifests/reports. Extracted-ZIP execution, native ARM64 runtime,
complete visual and scaling review, and the other component gaps remain open.
All 112
inventory entries are started and zero are accepted; that is not a completion
percentage. Monitoring and Editorial remain fictional component demos. GitHub
Actions remain paused at the user's request. The public wiki remote is
unavailable, so these release notes live in the repository.
