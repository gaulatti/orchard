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
