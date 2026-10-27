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

The exact v7 x64 catalog exposed a visible Monitoring focus defect: after
selecting a fictional post and opening Processing review with Enter, keyboard
focus stayed on the background Review selected button. Escape therefore did not
reach the review. The source fixture now measures the visible overlay before
focusing Local instructions. A source-built x64 catalog passes physical
navigation, post selection, review Enter/Escape, focus return and an update that
retains selection in Light and Dark. The focused [checkpoint and screenshots](evidence/monitoring-focus-source-v1/checkpoint.json)
retain the v7 failure and source-build hashes. Both source screenshots show the
review editor's focus outline. The unchanged v7 package still contains the
defect. The [archive-identical v8 catalog file set](evidence/components-v8-linked-catalog-v1/checkpoint.json)
now passes this same physical review/focus/Escape/update path in Light and Dark
from a separate runnable directory; its two screenshots were inspected. Broader
visible workflow and visual review, scaling and native ARM64 runtime remain open.
The current hidden Monitoring/Editorial gate
passes 66 checks and 577 isolation guards; its negative auditor rejects 25
altered receipts.

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
fresh output directory. Status/media/category/author editing, full visual
comparison, scaling and native ARM64 runtime remain open. The v7 package
passes these Editorial flows but still fails the Monitoring review
focus check described above. The archive-identical v8 catalog file set also
passes the physical edit/save/dirty-decision path in Light and Dark from the
separate runnable directory; both screenshots were inspected. Its
[NuGet preview and catalog ZIP](releases/2026-10-27-components-v8-unqualified.md)
have a passing framework-dependent independent x64 consumer and exact-file-set
catalog runtime. Clean-cache restore, independent fresh ZIP extraction,
self-contained consumer delivery and native ARM64 runtime remain open.
