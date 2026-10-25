# Sonner reference and acceptance

The authoritative source is Bleecker `a86059013cd59684ce452849278ea81e734f2066`,
`src/components/sonner.tsx` (SHA-256
`9abd2f6c1f907708e499170c6368e20f8400afb0c0c5dc07f60fb4635289154d`).
This is a reusable notification component, separate from Notification Badge.
All examples use fictional local data; there is no notification service.

Before implementing the Windows control, freeze the actual source's rendered
states and retained-instance histories. Required observable behavior:

- Five variants (`default`, `success`, `error`, `warning`, `info`), six physical
  top/bottom and left/center/right positions, ordered unbounded stacking, and
  consumer-provided title, optional description and action.
- A mounted imperative API returns a new ID and routes subsequent calls to the
  source's current mounted host. Calls without a registered host return an empty
  ID. Capture multiple-host and unmount behavior rather than inventing routing.
- Default duration is 4000 ms. A toast's explicit duration overrides the host's
  default; changing the default affects later additions, not existing timers.
  Zero, negative and other source-supported persistent values must be recorded.
- Appearance starts on requestAnimationFrame. Timeout and dismiss-button paths
  hide the item and retain it for 300 ms before removal; imperative dismissal
  removes immediately. Action callbacks do not implicitly dismiss, and hover,
  focus and Escape do not add undocumented lifetime behavior.
- Link actions are the source's separate underlined text button. Other actions
  use the original small Button and all its variants; omitting the variant uses
  primary. Preserve callback ownership, exceptions and reentrant additions.
- The host has a polite `Notifications` live region and each item has alert
  semantics; its dismiss button is named `Dismiss`. Preserve component metadata
  without launching Narrator or changing OS accessibility settings.
- Match source geometry, typography, colors, Lucide paths, borders, 12px corners,
  overlay shadow and motion across Light/Dark, long/multilingual content, RTL,
  constrained viewports, text scaling, hover/pressed/focus and reduced motion.

Windows acceptance additionally requires a registered catalog fixture and both
fictional Monitoring/Editorial composition demos to remain intact; public API
regressions, native lifecycle and modal-isolation checks; current x64/ARM64
builds; an independent NuGet consumer; and actual runtime/raster/input evidence.
Compilation or source preparation does not establish native acceptance. Physical
desktop tests need renewed availability; background work must not activate a
window, move the system pointer, change OS settings or trigger GitHub Actions.

## Verified source preparation

`tests/Reference/Sonner.json` and `tests/Reference/Sonner/` retain 70 original
full-page PNGs and 38 retained-instance histories. Two independent headless Edge
captures produce identical receipts and PNGs. The source auditor decodes
51,984,000 pixels, checks 94 flat surface probes, and verifies nine pinned source
files and 1,694 actual bundled inputs. React/React DOM 19.2.4, Lucide 0.563.0 and
the lockfile's Tailwind 4.2.2 are recorded. The rejection audit passes 57 damaged
cases, including a real wrong-theme PNG with its hash recomputed.

The source confirms a late first animation frame can restore visibility after
an early timeout or button dismissal. The already-scheduled 300ms removal still
happens. Actions remain callable during that retained, transparent interval.
The internal `BleeckerSonnerLifetime` preserves those event boundaries. Five
new source-backed lifetime/reference tests pass; the complete portable suite
passes 418 tests with zero failures. Native scheduling and rendering have not
been exercised by those tests.

The link action is an inline text button: its parent line box adds 2px of
trailing descent at the captured default text size. Its 16px button bounds alone
do not describe the toast height. The 200% fixture records the ordinary button
action; full scaling across every action remains open.

For reproducibility, use `scripts/reference/capture-sonner.cjs` with
`--source-root`, `--tooling-root`, `--playwright-module` and a fresh `--output`.
Then run `verify-sonner-source.mjs --reference <Sonner.json> --frames <capture>
--recapture <independent-capture> --output <fresh-audit.json>` and
`test-sonner-source-audit.mjs` with the same `--reference`, `--frames` and a fresh
`--output`. The latter deliberately retains its damaged evidence beside that
output. Source files and PNGs are not transformed or repaired.

Clean CI uses the explicit `--frozen-only` mode on both audit commands. That
mode verifies the checked-in source files, capture scripts, bundle, CSS, PNGs,
measurements and histories, and reports zero local bundled inputs rehashed.
The default mode additionally verifies all 1,694 recorded local capture inputs;
it fails if that capture environment is absent. The independent recapture proof
remains part of the source-preparation checkpoint.

Sonner now has a [native preview](sonner.md) with public APIs, source lifetime
scheduling, native layout/theme and a registered fictional catalog fixture.
The x64 solution and all 418 portable tests pass. Both the source catalog and a
fresh isolated NuGet consumer pass 66 hidden states, 20 behavior checks, two
retained lifetimes and two standalone histories. Their corresponding snapshots
and 66 PNGs are identical. Both catalog themes exercise all four public styles.
The source auditor rejects 53 damaged native cases, including the two earlier
failed runs; the package auditor rejects 49 cases without catalog-only or
historical-local inputs. The shared host regression retains both fictional
demos. Actual physical input/focus, active modal isolation, complete raster,
normal motion/reversals, broader scaling/customization and native ARM64 runtime
acceptance remain open. Current package documentation describes the new Sonner
surface without claiming complete component acceptance.
Both x64/ARM64 solution builds and isolated package consumer builds pass. The
verified package payload includes the current portable DLL, all 151 loose theme
files, native PRI, six fonts, two images and README. ARM64 compilation does not
establish native ARM64 runtime.

Inventory is now 107 entries started, 5 pending and 0 accepted out of 112. That
is 95.5% entries started, not completion. Both fictional component demos remain
required. The public wiki and central standard could not be reached; the
checked-in managed snapshot and repository documentation were used.
