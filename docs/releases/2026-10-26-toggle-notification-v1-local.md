# Local Orchard Toggle Group notification preview

`0.1.0-toggle-notification-v1-local` packages the reusable WinUI component
library with a focused Toggle Group fix: one `SelectionItem.Select` action in
multiple mode now raises one group `SelectionChanged` event after the caller's
original collection reaches its final exclusive selection. A previously
exclusive value produces no new event. The original hidden boundary case
failed in both themes; the repaired source and independent NuGet consumer pass.

| Artifact under `artifacts/current` | SHA-256 |
| --- | --- |
| `packages/Gaulatti.Orchard.0.1.0-toggle-notification-v1-local.nupkg` | `8caaebd48f7817fe050f49ff35218e6c572417e6f7cc67b80f59d6b6505a8811` |

The source-built x64 catalog passes 64 hidden Toggle Group checks, 70
observations and 1,777 isolation guards. A fresh x64 consumer restored and
built the exact package, then passed 62 hidden checks, 70 observations and
1,749 guards. The two extra catalog checks verify its own demo; the visible
catalog `VerifyAsync` routine was compiled but not run. Both hosts verified 12
standalone styles and rejected 32 retired-provider and 16 disabled-provider
actions. The package builder removed its isolated temporary restore project.
The source ARM64 catalog compiled with zero warnings and errors; native ARM64
runtime was not available on this x64 host.
The [small receipts](../evidence/toggle-notification-v1/README.md) remain in
Git after `artifacts/current` is cleared.

This is a local preview. Physical input, visual parity, display/text scaling
and native ARM64 execution remain open. The inventory has 112 entries started
and zero accepted; entries started is not a completion percentage. The wiki
remote was unavailable, so this record is kept in the repository. Push-triggered
GitHub Actions remain disabled at the user's request.
