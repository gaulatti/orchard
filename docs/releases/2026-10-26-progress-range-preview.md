# Windows component preview: Progress range handling

Progress now rejects nonfinite values and maxima that are nonfinite or at most
zero. CLR assignments fail before replacing a valid value; dependency-property
and binding paths also fail explicitly and recover when the caller supplies
valid data. The control never substitutes a range or replaces caller bindings.

Its read-only UI Automation value now agrees with the displayed bar: a caller
Value of −25 with Max 100 exposes 0, and Value 150 exposes 100. The raw finite
public Value stays unchanged. Maximum changes update the bounded provider value
and its notifications. The valid-value visual formula and theme tokens are
unchanged.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261026.2).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-progress-range-v1-local.nupkg` | `3a79b93f8d1d63d31de6792122c8afdb8ea8533e8d7a54c92d4635d12ee8515f` |
| `Gaulatti.Orchard.Catalog.0.1.0-progress-range-v1-local-win-x64.zip` | `cc2984de07f4a7ddf56944a6e47dd8840a7f7783aa52d842731f0be93bdd627a` |

Library SHA-256:
`78cbfcdeeaacd38cf3eb2c1ead5128437940f73bbdb1d973c94695d766d0fb04`.
Source catalog, exact independent NuGet consumer and catalog archive each pass
14 range states, 32 invalid CLR/dependency-property assignments, eight live
binding updates, four invalid-bound-data recovery contracts and ten
damaged-report checks. The baseline is rejected: it exposes four out-of-range
provider values, accepts all 32 invalid inputs and renders `NaN%` for zero/NaN
maxima. All 14 baseline valid-value fill widths, track widths and labels are
retained; this is layout/label evidence, not a pixel-equality claim.

Source and independent 159-file consumers compile for x64 and ARM64. Native
checks are hidden x64 runs without hardware input, activation or OS settings
changes. Native ARM64 execution of this repair, enabled/interrupted motion,
physical scaling and full visual acceptance remain open. Existing unrelated
evidence retains its original source and binary identities. Strict historical
raster failures remain recorded and do not gate this focused repair.

The catalog verifies all 550 ZIP entries. Replay maps them to byte-identical
independently frozen files instead of making another full extraction. Both
fictional Monitoring and Editorial component demos remain included. GitHub
Actions are suppressed during publication, then their prior settings restored.
The wiki is unavailable; documentation is maintained in the repository.

The inventory remains 112 entries started/in progress, zero pending and zero
accepted; both required demos remain in progress. These counts do not estimate
completion. Narrator and OS contrast campaigns are excluded.
