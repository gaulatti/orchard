# Windows component preview: keyboard keycap content

Caller content stayed centered when a keycap requested a different alignment.
`BleeckerKbd` now honors horizontal/vertical content alignment and live bindings,
including generated combination keys. Center remains the default. Caller
content, original template/selector models and bindings survive template changes
and remounts. The catalog includes aligned keycaps alongside the corner examples.

Download the [preview release](https://github.com/gaulatti/orchard/releases/tag/v0.1.0-preview.20261025.3).

| Artifact | SHA-256 |
| --- | --- |
| `Gaulatti.Orchard.0.1.0-kbd-content-v1-local.nupkg` | `867bdc19e0b11053879cac9dcdcfc9610d6d9e818d08ba4862dbd8a456dea026` |
| `Gaulatti.Orchard.Catalog.0.1.0-kbd-content-v1-local-win-x64.zip` | `80f376661d467bc8687e7bec23312009569e1f2e59e639fd0a3c79439ab66e24` |

The source-tested x64/package library has SHA-256
`fc945b795643e5ec37a4902933a622c1d4e5e72f94d314aeddf9dea34376e930`.
The previous published library fails the caller-alignment regression: top-aligned
content remains centered at y=21 instead of the native reference's y=5.

Source, exact NuGet consumer and catalog archive hosts each pass 64 alignment
states and 16 caller-content checks. These cover every horizontal/vertical
alignment combination in both themes/directions, live bindings on retained
combination children, original template/selector model context, and retained
content through template replacement/remounting. The source host also passes the
existing 32-state/four-context corner regression. All four default keycap images
remain unchanged. Every hidden host records zero focus or activation.
Source and independent NuGet consumer builds pass for x64 and ARM64. These
builds establish compilation, not native ARM64 execution.

Every catalog ZIP entry is verified against the frozen app. The replay directory
maps verified ZIP entries to identical independently frozen files, avoiding a
second full runtime copy. Source-input and verification manifests record the
exact qualification scope. Unchanged component/demo evidence retains its prior
source and binary identities; earlier portable/demo suites are not reported as
fresh runs of this package. Both fictional Monitoring and Editorial demos remain
included in the catalog. Hosted Actions remain suppressed at the user's request.

Native ARM64 execution, actual display/text scaling, complete browser/native
visual review and final component acceptance remain open. The focused tests use
no physical input or OS setting changes. Narrator and OS contrast campaigns are
excluded. The inventory remains 112 entries started/in progress, zero pending
and zero accepted, with both required demos in progress. These are inventory
counts, not a completion estimate.
