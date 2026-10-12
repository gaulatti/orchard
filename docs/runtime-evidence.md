# Native Windows runtime evidence

The catalog fixtures and independent NuGet consumer at commit `5412997` passed
on both x64 and native ARM64. That baseline covers 18 in-progress inventory
entries. The added Switch preview raises the current inventory to 19 in-progress
and 93 pending entries; its additional native checks are still being verified.
No component has full visual, input, accessibility and scaling acceptance.

The first native ARM64 run was [38267617259](https://github.com/gaulatti/orchard/actions/runs/38267617259)
at commit `403d8783416eae79086a6b59c714131a88a44121`. All three jobs passed:
x64 build/runtime/consumer, ARM64 cross-build/consumer build, and native ARM64
build/runtime/consumer. The repeat run [38268516314](https://github.com/gaulatti/orchard/actions/runs/38268516314)
at `5412997a3dfd15c76a06367b51480ab6a91b8e7c` also passed all three jobs,
including the tighter fractional Progress width regression.

The native reports from the repeat run record:

| Evidence | Observed value |
| --- | --- |
| Catalog process architecture | `Arm64` |
| Catalog OS architecture | `Arm64` |
| Consumer process / OS architecture | `Arm64` / `Arm64` |
| Windows version | `10.0.26200` |
| .NET runtime | `10.0.12` |
| Display / text scale | `1` / `1` |
| Windows animations | Enabled |
| Catalog / independent consumer | Passed / passed |
| Package version | `0.1.0` |
| ARM64 consumer package SHA-256 | `3b22c5e4b0039119c19519e2b8c300d9e205eeccc58702a66bb2074b2e987641` |

The `orchard-windows-arm64-runtime` artifact retains the runnable catalog,
independent consumer, package, 19-test report, captures and per-fixture JSON.
Artifact `11755888347` has SHA-256
`2fcbf474c4c660e45cad7cca1ab6a5caa58e066b3aa807b0a9e90b577dcbd209`.
Selected reports and captures are retained locally under
`artifacts/ci-arm64-5412997`; the earlier artifact under
`artifacts/ci-arm64-403d878` was also downloaded and its archive hash verified.

The architecture gates inspect reports produced by the running applications.
ARM64 smoke requires an ARM64 process and OS; the package consumer requires the
selected process architecture and an ARM64 OS for ARM64 verification. A build,
runner label or emulated x64 process cannot satisfy these checks. Each runtime
job currently packs and verifies its own candidate from the same source.

For Progress, the earlier x64 `4c76d59` and ARM64 `403d878` light/dark captures
are pixel-identical: zero differing pixels in each 756×502 image. The library
sources are unchanged between those commits. The comparison is retained at
`artifacts/progress-native-architecture-comparison.json`. The repeat ARM64 run
reports a 174.199996948px fill for a 268px track at 65%, preserving the
mathematical 174.2px width rather than rounding it to 174px. The pinned browser
reports 174.1875px after CSS layout-unit quantization. See [Progress](progress.md)
for the pill rendering checks and remaining reference-comparison work.

Full visual comparison across the inventory, actual contrast settings,
Narrator, display/text scaling, all motion/input states, collection performance,
the two substantial catalog compositions and the remaining components still
require acceptance evidence. The local development host remains Intel x64;
these checks ran on GitHub's ARM64 host without changing the developer's desktop
settings.
