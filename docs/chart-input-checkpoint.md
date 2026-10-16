# Chart input and overlay validation checkpoint

This is the historical `ab2af53`/`ad40db5` checkpoint. Its six-minute
watchdog instructions below describe that earlier workload. The current
coverage-scaled watchdog is documented in [architecture](architecture.md)
and [runtime diagnostics](catalog-readiness.md).

The changes remain local. Push-triggered Actions remain paused; no workflow
was dispatched and no commit was pushed for this checkpoint. Remote main is
`6bb1a3756bd1a136429637d2a2de2566ac925e1f`, with zero runs recorded for that
head. The public wiki remains inaccessible, so documentation stays here.

## Published local candidate

The runnable candidate and package use source
`ab2af53b226277705379b7b5539ae210f6521de1`. All eleven focused modes pass:
menus, Select shadow input, Filters source and live updates, Pagination
producer, held keys, paired keys, scroll and physical input, Pie/Donut, and
Sparkline. Their independent audits use
`artifacts/chart-readiness-release-*-audit.json`.

The chart audits retain 84 circular cases, 36 Sparkline cases, every original
keyboard expectation, 256 pointer presentation gates and 306 owned key
pairs. All 240 newly added hover/exit moves have fresh genuine receipts.
Two stationary circular mutation moves reuse their exact preceding receipt;
the audit reports them separately. Exact source pixels and raw event routes
remain unaccepted.

The Filters shadow audit verifies 228 layers and 98 closures. All 114 open
frames improve over the preceding native implementation: absolute RGBA error
falls from 150,235,406 to 116,792,256. The 162 closed frames retain 26,982
changed pixels. Comparisons cover the entire client, with zero masks and
zero tolerance; exact raster parity remains false. Pagination retains 132
producer states, 136 held states, 136 paired states, 36 scroll states,
twelve physical clicks and two exactly owned Escape transfers. Its producer
report still records 154 layout differences.

The independent x64 consumer passes both themes. Its Sparkline records prove
cleared empty-data inspection, restored original data and one owned Right
down/up pair. The fixture's output-path compile error was repaired in
`83a18b3`; both the failed invocation and the successful isolated rerun remain
recorded. ARM64 consumer compilation passes; ARM64 runtime is unverified.
Both generated consumer caches were removed.

The later diagnostic source `ad40db5` also builds cleanly for x64 and ARM64,
passes all 100 unit tests and token generation, and passes all twelve exact
Tooltip theme checks from its clean validation checkout. These later builds
were not repackaged or published; the artifacts below remain source
`ab2af53`.

All 532 published x64 files match the clean build. The published catalog and
both independent consumers contain the same packaged library. Byte audits
and archives explicitly report focused runtime scope and false full-runtime
acceptance.

| Artifact below `artifacts/` | SHA-256 |
| --- | --- |
| `packages-chart-readiness-validated/Gaulatti.Orchard.0.1.0.nupkg` | `e64c4aaa5206d666abf732342d021784eee464db98d3db699740d4951f4c05cd` |
| Packaged `Orchard.dll` | `85c81d082f869ae0f8f6a432b38f7a2b93b01a37b9c960f148c5a4e2bff3cbeb` |
| `Orchard.Catalog.0.1.0-ab2af53-x64.zip` | `7b4105e1ddb82b7724824dac9b5de24f3fa01f39a16497935190e6cd1b4d9e10` |
| `Orchard.Catalog.0.1.0-ab2af53-ARM64.zip` | `10d97f121280c9e7b255b90cb5012794fbb4c3f365b17eb4837420e89dcbeef3` |

## Full gate remains open

The preceding source `96e8299` times out after 936 captures. The two-turn
readiness candidate stops at the original LongTip live-theme geometry
assertion after 323 captures. The primary diagnostic build then passes all
twelve Tooltip theme checks but times out after 934 captures. All failures
remain retained; none establishes a causal timing repair. See
[Tooltip diagnostics](tooltips.md) and [readiness scheduling](catalog-readiness.md).

`artifacts/chart-readiness-partial-preservation.json` verifies all 174
protected Sunburst/Radial glyph hashes against the last passing 1,034-capture
baseline. The incomplete run is still missing 100 baseline captures.
`artifacts/chart-readiness-full-profile.json` records a 17.2734-second
increase across common capture intervals. Those intervals include work
between image writes and cannot identify a causal component cost by
themselves. Sankey's existing pointer gates account for about 23.8 seconds
across both themes; their original assertions remain required.

The next throughput investigation needs measured phase/capture/readiness
costs before another optimization. Keep the 360-second wrapper, original
two-second input deadlines, stable Rendering transitions, real input
ownership, original image names and references unchanged. Required full
verification must finish before pushing these changes. The inventory
remains 53 in progress, 59 pending and zero accepted out of 112; 47.3% is
started, not completed.
