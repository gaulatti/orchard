# Local Windows component preview: consolidated v6

`0.1.0-components-v6-local` packages the reusable Orchard component library at
source commit `d761853`. Since v5, the library restores an Input's composition
shadow after same-size remount. The catalog also corrects theme-root and native
presentation assertions for Notifications, Activity Item, Tooltip, Labels,
Inputs and Switches, and covers Button's focused self-loading transition.
Monitoring and Editorial remain fictional-data component demonstrations.
This is a local preview, not an accepted inventory release or external
publication.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v6-local.nupkg` | `8d3e62fce3db487ba9c67883ee7bd055564b0b855f61c79d6b03f549507ee2df` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v6-local-win-x64.zip` | `c53e6bce65f711201452975e96a379060128b566819d6fdfbd2f585de5db8d00` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v6-local-win-ARM64.zip` | `1784cd8265db2aab777a24160eda2eac5c9f96cca0660382006b2d53748782e0` |

An isolated x64 consumer freshly restored this exact NuGet package, published
and passed native resources, fonts, light/dark themes, layout and action smoke.
An isolated ARM64 consumer restored and published the same package; runtime
was not executed on this x64 host. Both generated restore workspaces were
removed by the verification script. The x64 and ARM64 catalog archives contain
664 and 660 files respectively, and every extracted file matches its archive
entry. The x64 consumer and extracted catalog share Orchard DLL SHA-256
`633efea340501c116ebc06e9a5f358c2192ecc7462579e759592c0e474c6b3e0`.
The ARM64 catalog's DLL is
`5f3f9fb8d6838ec5dc62cf5900f9e5c10122e246d8e95b9837f81f76facb81dc`.

The extracted x64 catalog passed focused Input checks in both themes, including
two consecutive shadow detach/restore cycles per theme. Before packaging, the
same source passed two complete light/dark presentation runs with 256 captures
each, 37 Input-related unit tests and an ARM64 library build. The extracted x64
catalog also passed 66 hidden Monitoring and Editorial checks with fictional
local data. The [Input checkpoint](../evidence/input-shadow-remount-v1/checkpoint.json)
retains the prior intermittent failure and its verification limits.

The [delivery checkpoint](../evidence/components-v6-local/checkpoint.json)
identifies the reports and hashes. Native ARM64 execution, Data Table's failing
performance budget, remaining component behavior and visual reviews, and final
inventory acceptance remain open. The inventory is 112 entries started and
zero accepted; this is not a completion percentage. GitHub Actions were not
triggered. The wiki remote was unavailable, so this record is in the repository.
