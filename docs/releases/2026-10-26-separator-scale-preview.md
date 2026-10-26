# Windows component preview: Separator fractional coverage

Separator now defaults to `UseLayoutRounding=False`. At 125% and 150% native XAML
rasterization this preserves the source's partial edge pixel instead of dropping
it or painting it fully. Callers can still enable snapping through ordinary
values, styles or bindings. No new public API or renderer is introduced.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261026.1).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-separator-scale-v1-local.nupkg` | `517cd338e5040be5197f4b258edf4db1d56ed78806be8a53b7b9f78f8dd39e28` |
| `Gaulatti.Orchard.Catalog.0.1.0-separator-scale-v1-local-win-x64.zip` | `5c315ff9076027e548c08ae659e35d1fa0deafd34d7994dd7e055724277f2405` |

The source-tested library is
`01d48777a0c03cb8af5db22a4313b960c0cf6ac4a7b89c0e976db0679672487c`.
Source catalog, exact independent NuGet consumer and catalog archive hosts each
pass eight scale frames, covering both themes and orientations at 100%, 125%,
150% and 200%. They check the actual native `XamlRoot.RasterizationScale`, physical
PNG dimensions, default behavior and retained caller snapping bindings. The old
published library is rejected by the new regression check.

All three hosts also pass the existing 32 Separator states/eight contracts and
18 Status Badge states/26 contracts. Their damaged-report checks reject ten
scale, ten Separator and eleven Status Badge cases per host. The 32 original
100% Separator snapshots and PNGs remain byte-identical. Source and independent
consumer compilation cover x64 and ARM64; compilation is distinct from execution.

The source/native coverage masks match. Strict zero-tolerance color comparison
still fails at 4,128 pixels, with maximum channel difference one; this diagnostic
is retained separately from coverage and contract verification. It is not a
full-library visual acceptance claim.

All 550 ZIP entries are verified. Replay maps each entry to a byte-identical
independently frozen file rather than making another complete extraction. Both
fictional Monitoring and Editorial component demos remain included. Unaffected
older suites retain their original binary and source identities.

Native runs are hidden x64 tests without hardware input, activation or OS setting
changes. Real monitor-DPI transitions, Windows text scaling and native ARM64
execution of this repair remain open. Recovered historical ARM64 execution
covers the pre-repair baseline only. See [native rasterization scope](../native-rasterization.md).
Narrator and OS contrast campaigns remain excluded. Actions are suppressed during
delivery. The wiki is unavailable; documentation is maintained in the repository.

The inventory remains 112 entries started/in progress, zero pending and zero
accepted; both required demos remain in progress. These counts do not estimate
completion.
