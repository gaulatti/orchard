# Component composition demos

The catalog contains **Monitoring demo** and **Editorial demo**. They compose
the existing Orchard controls over fictional, in-memory data. Use the catalog
navigation or the suite selector in either demo to switch between them. Both
fixture instances are retained while browsing the catalog, including selections
and unsaved edits. Restarting the catalog resets their data.

Monitoring combines AppShell, HeaderSelect, Sidebar, FeedColumn, FeedItem,
AttentionSurface, SearchInput, Checkbox, Panel, Field, Textarea and Button.
Each column has its own search. **Simulate local update** inserts a fictional
post without replacing existing posts or selections. **Details** shows the
original selected post. The attention section shows high-relevance examples.
Each post places its interactive controls and FeedItem on the themed card token
inside AttentionSurface's dark gradient, so their text remains readable in both
catalog themes.
**Review selected** locks a snapshot of the selected IDs; instructions can be
kept or explicitly discarded. **Record local review** adds an in-memory example
and clears that selection. The local reviews section displays those examples.
These controls do not send anything or connect to services.

Editorial combines AppShell, HeaderSelect, Sidebar, SearchInput, Select,
Checkbox, Button, Panel, Field, Input and Textarea. Filter by status/language,
search, sort by title, select rows, and open a page directly. The editor changes
title, body, local status, illustration, categories and authors. Selected
categories and authors have an explicit order; arrow buttons change that order.
The selected illustration is a fictional text description, requiring no image
service or downloaded asset. **Save local sample** validates a nonempty title
and updates the original in-memory page object. Dirty drafts require an inline
**Keep editing** or **Discard changes** decision before another page, internal
section or suite opens. Filtering and sorting preserve hidden row selections.

Rows are consumer compositions of existing controls. The review overlay is a
consumer composition of Panel and existing controls. These demos do not count
as implementations or acceptance evidence for the pending public Table,
DataTable, Modal or Sheet entries. Horizontal scrolling keeps the feed columns
and editorial list/editor usable in narrower catalog viewports.

## Reference and verification

The UI behavior reference is pinned to newsroom commit
`82cca98c4d3dfd1d65509f8d8c46e8fece0ead70`:
[monitoring composition](https://github.com/gaulatti/newsroom/blob/82cca98c4d3dfd1d65509f8d8c46e8fece0ead70/Sources/Newsroom/ContentView.swift)
and [editorial interface](https://github.com/gaulatti/newsroom/blob/82cca98c4d3dfd1d65509f8d8c46e8fece0ead70/docs/ariston.md).
`tests/Reference/WorkspaceDemos.json` records verified Git blob and SHA-256
identities, acceptance scenarios and the local-data boundary. Production API,
authentication, streaming and publishing behavior in those sources is excluded.

`WorkspaceDemoTests` exercises the catalog's original state producers through
filtering, selection, updates, review decisions, ordered relations, dirty
navigation, validation and saves. `--verify-hidden-workspaces <folder>` runs
the actual catalog fixtures in an unactivated native window in both themes,
using native button/sidebar invocation, native selection providers and actual
input/checkbox properties. It checks remount continuity and resource/binding
diagnostics. It sends no physical input and cannot prove visible raster,
keyboard/pointer navigation, focus restoration, scaling or ARM64 runtime
acceptance. Those remain explicit acceptance work.

Run the hidden gate against a freshly built native catalog:

```powershell
./scripts/verify-hidden-workspaces.ps1 -AppDirectory samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 -Output artifacts/workspace-demos
node scripts/verify-hidden-workspaces.mjs --report artifacts/workspace-demos/hidden-workspaces.json --app samples/Orchard.Catalog/bin/x64/Release/net10.0-windows10.0.26100.0/win-x64 --output artifacts/workspace-demos/audit.json
node scripts/test-hidden-workspaces-audit.mjs --report artifacts/workspace-demos/hidden-workspaces.json --output artifacts/workspace-demos/negative-audit.json
```

Use a fresh output directory to preserve failed evidence. On native ARM64, build
with `-p:Platform=ARM64`, use the ARM64/win-arm64 directory, and pass
`-Platform ARM64`. The workflow has matching gates for both native hosts; push
triggers remain paused during implementation. The local checkpoint passes 66
native checks and 25 negative receipt cases. The same fixture gate was replayed
on the AdminShell v7 library with all 66 checks and 577 isolation guards passing.
The current portable suite has 266 passing tests; both catalog architectures
compile without warnings. Visible demo interaction acceptance remains open.
The gate also passes on the AdminShell v10 library with all 66 checks and 577
isolation guards. Its audit rejects all 25 damaged receipts.

The `0.1.0-text-media-v3-local` component-library build also passes the current
x64 hidden demo gate: 66 checks and 577 isolation guards. Its Orchard DLL SHA-256
(`13ab2e380421a2fb0850f1ffe2bcdcb157b1952d4d3175812859c6ed730f0f9e`)
matches the independently consumed NuGet package. The report and audit are in
`artifacts/current/text-media-v3-workspace-demos`. This verifies the current
catalog compositions in a hidden x64 host; visible interaction, scaling,
native ARM64 runtime and a current catalog archive remained open at that check.

The versioned `0.1.0-text-media-v3-local` x64 and ARM64 catalog ZIPs now include
both compiled demo fixtures, with every extracted file byte-verified. The
extracted x64 archive passes the same 66 hidden demo checks and 577 isolation
guards. It also decodes all 653 embedded reference resources against their
original hashes. Its compiled Orchard DLL differs from the NuGet build and was
qualified independently. Visible input, scaling and native ARM64 runtime remain
open; the demos are not yet accepted.

No backend, environment variables or metrics are involved. GitHub's wiki remote
is unavailable; this document is the in-repository usage and verification source.

The extracted `0.1.0-text-media-v4-local` x64 catalog archive also passes the
66 hidden Monitoring and Editorial checks with 577 isolation guards. Both v4
catalog architectures compile and their ZIP entries match the extracted files;
the ARM64 archive has no native runtime result on this x64 host. See the
[v4 local preview record](releases/2026-10-26-text-media-v4-local.md).

The older `0.1.0-components-v7-local` x64 catalog exposed a visible Monitoring
focus defect: after
selecting a fictional post and opening Processing review with Enter, keyboard
focus stayed on the background Review selected button. Escape therefore did not
reach the review. The source fixture now measures the visible overlay before
focusing Local instructions. A source-built x64 catalog passes physical
navigation, post selection, review Enter/Escape, focus return and an update that
retains selection in Light and Dark. The focused [checkpoint and screenshots](evidence/monitoring-focus-source-v1/checkpoint.json)
retain the v7 failure and source-build hashes. Both source screenshots show the
review editor's focus outline. That older local v7 package still contains the
defect. The [archive-identical v8 catalog file set](evidence/components-v8-linked-catalog-v1/checkpoint.json)
now passes this same physical review/focus/Escape/update path in Light and Dark
from a separate runnable directory; its two screenshots were inspected. Broader
visible workflow and visual review, scaling and native ARM64 runtime remain open.
The current hidden Monitoring/Editorial gate
passes 66 checks and 577 isolation guards; its negative auditor rejects 25
altered receipts.

The later published `0.1.0-preview.20261027.7` extracted x64 catalog now
passes this same physical Monitoring review/focus/Escape/update route in Light
and Dark. Its Orchard DLL matches the released NuGet package byte for byte;
both processes exited, and both review screenshots visibly show the editor's
focus outline. The retained [reports and captures](evidence/monitoring-v7-published-focus-v1/checkpoint.json)
close the older package-version gap for this focused demo flow. Process exit
codes were not captured. Broader visual/workflow review, scaling and native
ARM64 runtime remain open.

The source-built and extracted v7 x64 catalogs pass a focused visible Editorial
search check in Light and Dark. Physical pointer input selects a fictional page
and focuses the search editor; typing `garden` filters four rows to one while keeping the hidden
Harbor selection. Clearing the query restores all four rows with Harbor still
selected. The [Editorial checkpoint and screenshots](evidence/editorial-search-source-v1/checkpoint.json)
identify both catalog library identities and the probe bytes. Run the check with
`scripts/verify-editorial-search-desktop.ps1` and a fresh output directory.
The same two catalogs also pass a [visible edit-and-decision check](evidence/editorial-edit-v1/checkpoint.json)
in both themes at a 1600 × 1000 window size. Physical pointer and keyboard
input opens a page, changes its title and saves the fictional local row. A later
unsaved title change triggers the inline navigation decision; Keep editing
preserves the draft, while Discard changes opens the requested page without
changing the saved row. The checkpoint retains all four runtime reports and
decision screenshots. Run `scripts/verify-editorial-edit-desktop.ps1` with a
fresh output directory. At the time of that v7 run, status/media/category/author
editing, full visual comparison, scaling and native ARM64 runtime remained open.
The older local v7 package passes these Editorial flows but fails the Monitoring review
focus check described above; the later published `.7` catalog passes that
focused Monitoring route. The current source-built x64 catalog now has a
[visible relation-editing checkpoint](evidence/editorial-relations-source-v1/checkpoint.json):
pointer input selected a local status and media illustration, selected and
reordered categories and authors, saved the fictional sample, navigated away,
and reopened the page with those values retained. The Dark run covered all four
fields; the Light run added another category and author, reordered an author,
and verified the saved values in the same running local sample. This closes the
previously open status/media/category/author editing observation for source x64;
fresh-package runtime, broader keyboard/focus paths, visual comparison, scaling
and native ARM64 runtime remain open. The archive-identical v8 catalog file set also
passes the physical edit/save/dirty-decision path in Light and Dark from the
separate runnable directory; both screenshots were inspected. Its
[NuGet preview and catalog ZIP](releases/2026-10-27-components-v8-unqualified.md)
have a passing clean-cache, self-contained independent x64 consumer, fresh
x64 catalog extraction and runtime, and ARM64 catalog extraction and consumer
publish. Native ARM64 runtime and full component acceptance remain open.

The subsequent source fixture fixes a Light-theme Monitoring contrast defect
seen in the v8 catalog: dark FeedItem text previously sat directly on the
AttentionSurface gradient. The [source checkpoint](evidence/monitoring-card-contrast-v1/checkpoint.json)
records the themed-card composition, the updated 68-check hidden gate with 581
isolation guards, and visible Light/Dark inspection. The v8 ZIP and NuGet
package predate this demo fix. The [v9 local preview](releases/2026-10-27-components-v9-unqualified.md)
includes it: the freshly extracted x64 catalog passes the same 68 hidden
Monitoring/Editorial checks and 581 isolation guards. The ARM64 archive is
hash-verified; its native demo runtime remains open.

The source-built Monitoring review now uses a bounded, opaque themed card surface.
Previously, the review panel stretched with an empty outlined area and its
transparent body exposed post cards behind the overlay. The panel stays within
336 effective pixels and uses `BleeckerCardBrush` in Light and Dark while keeping
the review action visible. The [review surface checkpoint](evidence/monitoring-review-surface-source-v1/checkpoint.json)
records a fresh x64 hidden gate (72 checks, 589 isolation guards, 25 rejected
damaged receipts), warning-free x64 and ARM64 builds, and visible Light/Dark
inspection of the source catalog. In Dark, a pointer click on Record local review
changed the fictional summary from zero to one local review. The v9 archive
predates this fixture change; native ARM64 runtime and broader composition
acceptance remain open.

The now-published `0.1.0-preview.20261027.5` package matches the source-built
x64 catalog's Orchard DLL. Its Monitoring and Editorial hidden fixture passes
72 checks and 589 isolation guards; the audit rejects 25 damaged reports.
Visible desktop checks pass Monitoring review/focus/Escape/update, Editorial
search/selection, and Editorial edit/save/dirty-decision in Light and Dark with
physical pointer and keyboard input. All six screenshots were inspected and
retained with the reports in the [v5 demo checkpoint](evidence/catalog-v5-demos/checkpoint.json).
An initial Monitoring Light run could not activate the catalog while a stale
“Open With” dialog held foreground; the unchanged retry passed. This is
source-built catalog evidence. The versioned v5 x64 and ARM64 catalog ZIPs are
now [stream-verified](evidence/catalog-v5-archives/checkpoint.json), and every
extracted x64 file matches its archive manifest. The extracted x64 catalog
passes the 72 hidden demo checks and 589 isolation guards, visible Monitoring
Light review/focus/Escape/update, and Editorial Dark edit/save/dirty-decision
with physical pointer and keyboard input. An initial extracted Monitoring run
also stopped before input on a stale foreground “Open With” dialog; the
unchanged retry passed. The v5 ARM64 catalog [builds with both demo XBF files](evidence/preview-v5-arm64-build/checkpoint.json)
but has not run on ARM64 hardware. Broader visual/scaling and package catalog
qualification remain open.

The published `0.1.0-preview.20261027.7` x64 catalog archive now passes the
same 72 hidden Monitoring/Editorial checks and 589 isolation guards in Light
and Dark, with no resource or binding diagnostics. The suite covers ordered
Editorial category/author relations, local save, validation and discard
decisions; its auditor rejects 25 damaged receipts. The
[v7 workspace checkpoint](evidence/catalog-v7-workspaces-hidden-v1/checkpoint.json)
binds those results to the exact NuGet-matching DLL and verified release ZIP.
The earlier [visible relationship walkthrough](evidence/editorial-relations-source-v1/checkpoint.json)
used demo source unchanged at the v7 release tag. That visible source evidence
and the published hidden replay cover different boundaries; neither proves a
complete published-archive input matrix, visual comparison, scaling or native
ARM64 execution.
