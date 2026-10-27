# Toggle Group exclusive-selection receipts

The [checkpoint](checkpoint.json) pins SHA-256 hashes for these small,
byte-preserving copies. They record the original failure and the
repaired hidden native checks after disposable `artifacts/current` output is
cleared. `baseline-toggle-group.json` fails the new light and dark
`multiple-select-single-notification` cases. `source-toggle-group.json` and
`consumer-toggle-group.json` pass them; the corresponding `*-audit.json`
files record audited check counts and guards. `consumer-build.json` pins the
exact local NuGet package hash and confirms an isolated x64 consumer build.

The source catalog passes 64 checks, 70 observations and 1,777 guards. The
independent package consumer passes 62 checks, 70 observations and 1,749
guards. The two-case difference is the catalog's own visible-fixture checks;
`VerifyAsync` compiled but was not activated during background testing.

These receipts establish the hidden event contract. They do not establish
physical keyboard/pointer behavior, visual parity, display/text scaling,
native ARM64 runtime, or full component acceptance. The package and runnable
apps are disposable and are not stored in this archive. The ARM64 source
catalog also compiled with zero warnings and errors; it was not executed on
ARM64 hardware.
