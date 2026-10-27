# Orchard Windows preview 2 — Footer brand foreground

The [published NuGet prerelease](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261027.2)
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

This release updates the NuGet package only. The earlier
[v11 catalog ZIPs](2026-10-27-components-v11-preview.md) predate the Footer
repair. Package-specific physical Footer input, complete visual and scaling
review, native ARM64 runtime, and the other component gaps remain open. All 112
inventory entries are started and zero are accepted; that is not a completion
percentage. Monitoring and Editorial remain fictional component demos. GitHub
Actions remain paused at the user's request. The public wiki remote is
unavailable, so these release notes live in the repository.
