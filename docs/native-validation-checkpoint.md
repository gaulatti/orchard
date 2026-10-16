# Current native validation checkpoint

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
