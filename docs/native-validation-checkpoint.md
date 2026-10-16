# Current native validation checkpoint

## DataList release checkpoint

Current full-suite/package library and catalog source:
`a7dc3caf6b4fcc1fc5a50add2c87017046e971b5`.
The clean published x64 run passes in **437.5147483 seconds**, with **1,104
captures**, all 1,080 prior names and all **174 protected glyph hashes**.
Its independent audit verifies 1,982 pointer moves/presentation gates, 1,748
owned key pairs and 70 physical clicks. The maximum pointer gate is 169.8662 ms
under the unchanged two-second deadline. The eight-minute full watchdog is
unchanged; no collection-performance or speedup claim is made.

All 72 published DataList cases pass the existing 0.05-DIP geometry bound
across both themes and widths 639, 640 and 1024. Consumer actions, templates,
live binding, keyed lifecycle and real window resizing pass. Exact images
remain open: 66/72 differ without masks, tolerance or resampling, totaling
2,536,268 pixels and 114,743,118 absolute RGBA error. Six empty frames match.
See [the API and acceptance gaps](data-list.md). Inventory: **55 in progress,
57 pending and zero accepted**.

Sequential clean x64/ARM64 builds and publications pass without warnings;
108 unit tests and token generation pass. The initial concurrent build failed
because ARM64 restore replaced the shared x64 assets target; that failure is
retained in `artifacts/data-list-clean-x64-build.log`. All 324 Stepper baselines
remain within 0.0000153 DIP. Final visuals, contrast, Narrator, scaling, native
ARM64 runtime, remaining components and both compositions remain open.

The independent x64 consumer passes both themes at
`artifacts/package-consumer-data-list-x64-v3`. Its first two attempts fail the
existing Bar Chart initial-inspection assertion after DataList's own action
checks. The second failure records keyboard focus, intact records, empty help
text and no pointer events. Added lifecycle/focus observations change timing
and the third attempt passes; **no control fix is claimed**. Preserve both
failures and investigate this intermittent boundary before acceptance. The
ARM64 consumer builds only. All isolated restore caches are removed.

The library in the package, x64 catalog and passing/build-only consumers matches
byte for byte. `artifacts/data-list-release-binary-correspondence.json` records:

- Library SHA-256: `c62a181ae273d1059068970b533f872ed92b70ed8d77de39a535d04ebe751b1d`.
- Package SHA-256: `d2e6657b50ba39964890526582446f1b6839459f93cf2a675eb98ca4ac1c635d`.
- x64 catalog ZIP: 532 files, SHA-256 `92fc4e4afb044fb6bed9d33f148dcb8ce26b616536ac178ee31395cb30386742`.
- ARM64 catalog ZIP: 528 files, SHA-256 `ce1bf56ed414895cb574ba24864fa00560d8934ba3866163e7f8fbad1d300f4a`.

Both ZIPs match every published entry. The x64 publication has full-run
evidence; ARM64 has build evidence. Artifacts include
`artifacts/smoke-data-list-release-full`,
`artifacts/data-list-release-{full-audit,full-timing,archives,pixel-audit}.json`,
`artifacts/data-list-release-matrix/{1024,639,640}` and
`artifacts/data-list-release-stepper-typography-audit.json`. Push-triggered
Actions remain paused; no new hosted result is claimed. The public wiki
remains unavailable. The goal remains active.

## Previous Stepper baseline checkpoint

Full-suite and packaged library/catalog source: `b59cef474ad95c94841cbfccd77059e85dcc06ad`.
The clean published x64 full run passes in **406.1895514 seconds** with all
**1,080 captures**, every prior filename and all **174 protected glyph hashes**.
Its independent audits verify 1,980 pointer moves/presentation gates, 1,744
owned key pairs and 68 physical clicks, with zero failures. The maximum pointer
gate is 205.6012 ms under the unchanged two-second deadline. The full watchdog
remains eight minutes; no throughput or collection-performance claim is made.

The native Stepper now matches all **324 first-line text baselines** across
44 pinned browser cases, including ordinals, both orientations, long/narrow
labels and RTL. Maximum baseline error is 0.0000153 DIP under the existing
0.05-DIP geometry bound. Label/description baselines move up one pixel from
the previous implementation; ordinal line height is explicitly 20px. The
source capture derives baselines from actual text ranges and measured font
bounding boxes without altering the DOM. Empty text and check glyphs have no
fabricated baseline. The prior nearest-pixel paint placement remains recorded
in `artifacts/smoke-stepper-typography-before`.

The unmasked zero-tolerance image audit improves from 165,563 to **143,878
differing pixels**, and from 27,086,663 to **11,026,803 absolute RGBA error**.
It still differs in 42/44 cases. Font/shape rasterization, alpha-paint details,
scaling, contrast, Narrator, full override/reversal matrices and native ARM64
runtime remain open. The inventory stays **54 in progress, 58 pending,
0 accepted**, and the goal remains active.

Clean x64 and ARM64 solution builds have zero warnings/errors. All **105 unit
tests** and token generation pass. Published enabled/reduced Stepper motion
passes 42 transitions, 776 sampled frames and four owned Space pairs, and
restores the original Windows preference. The maximum sRGB trajectory error
is 0.927 channel values under the unchanged 1.5 quantization bound.

Evidence:

- `artifacts/smoke-stepper-baseline-full`
- `artifacts/stepper-baseline-{full-audit,typography-audit,pixel-audit,full-timing}.json`
- `artifacts/smoke-stepper-baseline-motion` and `stepper-baseline-motion-audit.json`
- `artifacts/tests-stepper-baseline-release`
- `artifacts/catalog-stepper-baseline-{x64,arm64}`
- `artifacts/packages-stepper-baseline/Gaulatti.Orchard.0.1.0.nupkg`
- `artifacts/Orchard.Catalog.0.1.0-b59cef4-{x64,arm64}.zip`

Push-triggered Actions remain paused. No new hosted CI/runtime result is
claimed. The public wiki remains unavailable; this evidence is maintained
in-repository.

The fresh independent NuGet consumer passes its x64 native action, fonts,
resources and light/dark checks. The independent ARM64 consumer builds;
ARM64 runtime remains unverified. Both isolated restore caches are removed.
The package library and both consumers match the published x64 `Orchard.dll`
byte for byte. `artifacts/stepper-baseline-binary-correspondence.json` records
library SHA-256 `55ce036ea2d226d849cd8b808723723289e9d16281ba6459d44389af753d1a5a`
and package SHA-256 `3730495664c364cfc5a2f3e7418cf38b4c04ee285bc1655dbe131cee1260340f`.

Both versioned catalog ZIPs match every published entry byte for byte:

- x64: 532 files, SHA-256 `df119e829fcbdb8e45e2355e29181511d0102e510c1c4b7eabb5f49bdca0f33e`.
- ARM64: 528 files, SHA-256 `326d7a59e7c3542af0aa01ae90a69c3380a5ceac00da31fa0347fe46e427ab9d`.

The archive audit is `artifacts/stepper-baseline-archives.json`. The x64
archive contains the runtime-verified publication; the ARM64 archive has build
evidence only. These hashes identify artifacts and do not establish component
visual acceptance.

The subsequent capture-only Stepper raster diagnostic verifies all 44 cases
with **zero screen-versus-bitmap differences**. Every visible frame stays
identical before/after bitmap rendering, and the original catalog parent is
restored. It rules out the bitmap path as the cause of this host's observed
browser differences. The diagnostic's browser comparison still differs in
42 cases (143,883 pixels, 11,020,905 absolute RGBA error). The rehosted bitmap
differs from the prior full-run bitmap by 226 pixels across 26 cases; these
captures remain separate. No production control, reference image, comparison
tolerance or mask changes. Artifacts are `artifacts/smoke-stepper-raster-final`,
`artifacts/stepper-raster-final-audit.json` and
`artifacts/stepper-raster-negative-audit.json`. All four negative audit cases,
105 unit tests, token generation and x64/ARM64 diagnostic builds pass. The
complete suite/package evidence above remains at its stated source revision;
no new full-suite or ARM64 runtime result is claimed for the diagnostic.

## Previous source-motion checkpoint

Current library/catalog source: `05c5b2db48c8b02af2ddc5fb548cde73a8e65a55`.
The published full x64 run passes in **404.2454108 seconds**, with all
**1,080 captures**, all prior filenames and **174 protected glyph hashes**. Independent audits verify
1,980 pointer moves and stable presentation gates, 1,744 owned key pairs and
68 physical clicks, with zero failed gates. The maximum pointer gate is
197.5917 ms under the unchanged two-second deadline. The full-suite watchdog
remains eight minutes. No throughput or collection-performance claim is made.

Clean x64 and ARM64 builds have zero warnings/errors; **104 unit tests** and
token generation pass. The published Stepper motion check exercises the real
Windows client-area animation setting in enabled/reduced modes and restores
the original preference. All 42 status/theme transitions, 768 sampled native
frames and four owned Space down/up pairs pass. Scale changes immediately;
colors follow the source's 190 ms premium sRGB transition. The trajectory
audit's maximum error is 0.936 channel values under its fixed 1.5 quantization
bound. Callback removal preserves running color motion. The source capture,
native fixture and independent verifier retain separate evidence.

The failed full run at `ba1434b` remains in
`artifacts/smoke-stepper-motion-full`. Its trace records BottomTip's delayed
hover timer taking ownership while the programmatic check inspected LongTip.
The fixture now establishes a genuine neutral pointer receipt before those
checks. The final run passes all 28 Tooltip gates with the original deadline
and stability requirement; production Tooltip behavior is unchanged.

The zero-tolerance Stepper image audit still differs in **42/44 cases**
(165,563 differing pixels). Exact raster, complete reversal/override matrices,
scaling, contrast, Narrator and native ARM64 runtime remain open. The inventory
stays **54 in progress, 58 pending, 0 accepted**; the goal remains active.

Current local evidence:

- `artifacts/smoke-stepper-motion-final` and `stepper-motion-final-audit.json`
- `artifacts/smoke-stepper-motion-final-full`
- `artifacts/stepper-motion-final-{full-audit,pixel-audit,full-timing}.json`
- `artifacts/tests-stepper-motion-release`
- `artifacts/catalog-stepper-motion-final-{x64,arm64}`
- `artifacts/packages-stepper-motion-final/Gaulatti.Orchard.0.1.0.nupkg`
- `artifacts/Orchard.Catalog.0.1.0-05c5b2d-{x64,arm64}.zip`

The fresh independent x64 NuGet consumer passes both themes and native input.
Its library DLL matches the validated catalog and package. ZIP audits compare
every published entry byte-for-byte. The independent ARM64 consumer compiles
and contains the same packaged DLL; its runtime remains unverified. Both
isolated consumer caches are removed. Consumer evidence is in
`artifacts/package-consumer-stepper-motion-{x64,arm64}` and byte correspondence
is in `artifacts/stepper-motion-final-binary-correspondence.json`.
Current artifact SHA-256 values:

| Artifact | SHA-256 |
| --- | --- |
| NuGet `0.1.0` | `5c258447279360969a7b26587a011c5b23e413fa3712ee29ad69185d868a413f` |
| Packaged `Orchard.dll` | `8f701635a67093e586b62805887ea4043efecc71f5564c9ea912ab1429981dd8` |
| x64 catalog ZIP | `630d5ee9c44439a790503072a1dd9e4b2a5f71c49819493dcc6d34c8bc9cc19e` |
| ARM64 catalog ZIP (build only) | `49e9fb3aaaa8f72bb53650a68ed0bd5f5219507f5aa0ae2ba34b7960d20f7404` |

Push-triggered Actions remain paused. The motion gate is registered for future
manual/pull-request runs; no hosted runtime result is claimed. The public wiki
is unavailable, so documentation remains in-repository.

## Previous initial Stepper checkpoint

Library/catalog source: `14bc3ef42549af8c7d61bf407cf0f9c8b2c55b1f`.
Independent consumer harness: `5a72f66` (test assertions only).

The published x64 full sequence passes in **404.7333773 seconds** with
**1,080 captures**. All prior 1,034 names remain present; the added 46 are
44 Stepper source cases and two native held-Space captures. All **174 protected
Sunburst/Radial glyph hashes remain unchanged**. Independent input audits verify
1,978 pointer moves and stable presentation gates, 1,744 owned key pairs and
68 physical clicks, with zero failures. The maximum pointer gate is 179.9942 ms
under the unchanged two-second deadline. The eight-minute full-suite and
six-minute focused watchdogs are unchanged. This is functional verification,
with no throughput improvement or collection-performance acceptance claim.

Clean x64 and ARM64 builds pass with zero warnings/errors. All **102 unit tests**
and token generation pass. The independent x64 NuGet consumer passes both
themes, including native Space press/release, original Stepper data ownership,
live metadata and disabled UIA state. The package DLL matches the validated
x64 catalog and consumer bytes. Both catalog ZIPs match every published file.
The independent ARM64 consumer compiles; both isolated consumer caches are
removed. Native ARM64 execution remains unverified on this x64 machine.

The new navigation entry exposed the Alert test's fixed 64-key traversal bound.
The corrected check derives its finite bound from mounted native tab stops.
Both themes actually require 65 keys to return to the close action, within the
measured bound of 68. Forward entry, exit, full-cycle return, reverse exit and
forward return assertions remain intact. The earlier failed full and focused
runs are retained at `artifacts/smoke-stepper-full` and
`artifacts/smoke-stepper-alerts`.

Stepper remains **in progress**. All 44 normal source cases pass geometry,
brush, disabled-state and accessible metadata checks, and both themes pass
native input, command, keyed-model, template and unload/remount checks. The
unmasked zero-tolerance screenshot audit still differs in **42 of 44 cases**
(165,569 differing pixels); both empty frames match. Exact pixels, enabled
motion, contrast, scaling, Narrator and native ARM64 acceptance remain open.
The inventory is **54 in progress, 58 pending, 0 accepted**.

Local evidence:

- `artifacts/smoke-stepper-release-full`
- `artifacts/stepper-release-{full-audit,full-pixel-audit,full-timing}.json`
- `artifacts/stepper-release-{binary-correspondence,archives}.json`
- `artifacts/tests-stepper-release`
- `artifacts/package-consumer-stepper-release-x64-v2`
- `artifacts/package-consumer-stepper-release-arm64` (build only)
- `artifacts/packages-stepper-release/Gaulatti.Orchard.0.1.0.nupkg`
- `artifacts/Orchard.Catalog.0.1.0-14bc3ef-{x64,arm64}.zip`

NuGet SHA-256:
`3b5ccd8e634818b2b3c9be023cd495fb242e82c723f129bcedccb654c0fd74ec`.
x64 ZIP: `46dd162111f54a6698ee6f9007550da6a95b206ddd053cce75cf24542bdb0660`.
ARM64 ZIP: `39457da82b9a04412d35dba5051581e0bf74a0759ba7d1158fc41c6396c6b6a2`.

Actions remain paused for pushes. The Stepper gates are registered for future
manual or pull-request validation; no new hosted runtime evidence is implied.
The public wiki remains unavailable; this checkpoint is documented in-repository.

## Previous capture-persistence checkpoint

Validated source: `3591548efd593215cc54cd9c32272bd8491cb14e`.
The full published x64 sequence passes in **374.8848365 seconds** with all
**1,034 captures**. The full-suite watchdog is eight minutes; focused modes
retain six minutes. The additional 240 required chart pointer actions and
their presentation gates account for a larger workload. This is functional
verification, with no throughput-improvement claim. The original collection
performance requirements remain open and separate.

## Runtime evidence

Independent audits verify:

- All prior 1,034 capture names, with none missing.
- All 174 protected Sunburst/Radial glyph hashes unchanged.
- 1,972 pointer moves and presentation gates, zero failures and at least two
  genuine stable Rendering transitions. Maximum gate time is 181.8119 ms
  under the original two-second deadline. Eighty-eight stationary moves
  retain a preceding exact receipt; all 240 newly added chart moves require
  their own fresh genuine receipt.
- 1,738 owned key pairs and 62 physical clicks, with zero failures.
- All eighteen final presentation gates, twenty-eight Tooltip gates and
  118 Toolbar source cases.
- Both themes' Filter, Column Toggle and Pagination lifecycle checks,
  including controlled ownership, stale/disabled rejection and zero final
  observers. The keyed live-options checks remain verified.
- The separately audited 84 circular and 36 Sparkline cases retain every
  original keyboard expectation and their added source-defined hover/exit
  states.

Evidence is below `artifacts/`: `smoke-capture-boundary-validated-full`,
`capture-boundary-{prior-1034,full-preservation,full-native-input,full-aggregate,full-pie,full-sparkline}-audit.json`
and `capture-boundary-validated-full-timing.json`. The timing journal and its
summary retain the complete process rather than inferring a pass from image
count alone.

The earlier six-minute timeouts and intermittent Tooltip/Input failures
remain retained. The current full run passes those same assertions; it does
not establish the causes of the earlier intermittent failures. Input now
writes failure-only brush/state diagnostics. No expected color, native
cursor, source reference, comparison mask, per-state deadline or input
assertion was relaxed.

## Builds, consumer and image persistence

Clean x64 and ARM64 solution builds have zero warnings/errors; all 100 unit
tests and token generation pass. The fresh independent x64 NuGet consumer
passes both themes. Its Sparkline evidence verifies empty-data inspection
cleanup, restored original data and one owned Right down/up pair. The fresh
ARM64 consumer compiles, with runtime explicitly unverified. Both generated
consumer caches are removed, and the cleanup regression also proves cleanup
after invalid-project and restore failures.

The published `brush-alpha` mode verifies four frozen persistence cases:
transparent, odd dimensions, every alpha/channel pair and catalog size.
An independent decoder confirms exact PNG bytes and RGBA pixels, immediate
file completion and propagated write failure. That regression is registered
in both native CI jobs. Actions remain paused for pushes; no new hosted
runtime evidence is implied.

The bitmap-reuse, worker-encoding and direct-DIB experiments showed no
established performance benefit and were removed. Their measurements and
patches remain available; see [runtime diagnostics](catalog-readiness.md).
The public library and dependency source is unchanged from candidate
`ab2af53`, whose eleven focused modes and independent audits remain prior
evidence. The current full sequence, package consumer and persistence checks
above use the new source and artifacts.

## Deliverable bytes

All 532 published x64 files match the clean build. The catalog and both
consumers contain the same packaged library. ZIP audits verify every entry;
the ARM64 ZIP records build-only scope.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-capture-boundary-validated/Gaulatti.Orchard.0.1.0.nupkg` | `47f43908a40954bfcc78ac64e1a66f7e6d291be9c71ffcfa2933a19d1118c70e` |
| Packaged `Orchard.dll` | `a2f077e4f3e3aaeff0cc4930105585619dd7da9f014d835c06d36fbeaf23037d` |
| `Orchard.Catalog.0.1.0-3591548-x64.zip` | `228167c5eb4878cfaaf7104fa910c165922e7a8b7f6217f68fde2ac693d68f0f` |
| `Orchard.Catalog.0.1.0-3591548-ARM64.zip` | `8bf35058181fa7c7eebfa9a6bb0056af3b470bfbba2f44805b9b32488ba19a20` |

## Remaining acceptance

This previous checkpoint had 53 in progress, 59 pending and zero accepted out
of 112. Exact source raster/routes, complete motion and override matrices,
scaling, Windows contrast/Narrator, collection performance and current native
ARM64 runtime remain separate required work. The goal stays active. The
public wiki remains unavailable, so this checkpoint is documented here.
