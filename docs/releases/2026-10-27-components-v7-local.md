# Local Windows component preview: consolidated v7

`0.1.0-components-v7-local` packages the reusable Orchard component library at
source commit `aeb338c`. Since v6, Input's catalog theme owner was corrected,
Media Library, Select and Input gained physical catalog checks, and Popover now
retries initial content focus and restores Escape focus to a composite trigger's
native action. Monitoring and Editorial remain fictional-data component demos.
This is a local preview, not an accepted inventory release or external NuGet
publication.

| Artifact | SHA-256 |
| --- | --- |
| `artifacts/current/packages/Gaulatti.Orchard.0.1.0-components-v7-local.nupkg` | `32f9566b24115317438ae71a56d6e4b7d8ea358f0aea0a624cc0266e8a11cde4` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v7-local-win-x64.zip` | `308f1e70ecbd3e8d68f857826662bcd6138645644b6e236c2b66e4c523f73d51` |
| `artifacts/current/Gaulatti.Orchard.Catalog.0.1.0-components-v7-local-win-ARM64.zip` | `2e6cfc08498cf7e34d75cccc3be22546ded98c05a205028d20bbe61684ac9863` |

An isolated x64 consumer restored this exact package and passed native resource,
font, light/dark theme, layout and action smoke. Its temporary restore workspace
was removed. Its Orchard DLL SHA-256
`ff3e83795ac7184297ace7bb2ccf69b16593384fe2eb569ed8f5b132adc81da9`
matches the extracted x64 catalog byte for byte. Both catalog archives were
extracted and every file matched the published directory: 664 x64 and 660 ARM64
files. The extracted x64 catalog then passed physical Popover, Media Library,
Select and Input pointer routes in Light and Dark. Its fictional Monitoring and
Editorial demos passed 66 hidden checks. These are focused routes, not complete
per-component acceptance.

An additional focused Progress range probe passed against both extracted x64
hosts with the same `ff3e8379…` Orchard DLL: 14 value/range states, 32 rejected
invalid inputs, eight live bindings, four invalid-binding recovery contracts and
93 audit guards per host. The retained audit SHA-256 values are
`935a31d6f4869c425867c6b2aab6952412084bd5f3d959999f875721ddc8582c`
for `artifacts/current/components-v7-progress-range-catalog-x64/audit.json`
and `e510157125d0ef123cf492cbd4e9f4766fc8769e865cc830cec9d8e26939d90f`
for `artifacts/current/components-v7-progress-range-consumer-x64/audit.json`.
These hidden checks sent no physical input or OS-setting changes and do not
establish Progress motion, complete visual, scaling or ARM64 runtime acceptance.

Keyboard shortcuts also passed the existing content probe on both extracted x64
hosts: 64 alignment states, 16 caller contracts, eight isolated-scale captures
and 64 metadata contracts per host, with zero focus/activation. The catalog and
consumer audit SHA-256 values are
`47231c66a5381ecceb9c40f66089e0205031c150e336e7515e1f96d46067425b`
and `3b9c52e033086e99a7d45de43fe62c3e127b5f321e112572bf31a85ebebc32a6`.
Reports are under `artifacts/current/components-v7-kbd-content-catalog-x64`
and `artifacts/current/components-v7-kbd-content-consumer-x64`. Isolated XAML
scale is verified here; physical monitor/text scaling, full visual parity and
native ARM64 runtime remain open.

Checkbox rich-content checks passed on the same x64 catalog and independent
consumer: 72 and 70 Light/Dark checks respectively, covering names, native
toggle events, caller bindings and implicit styles. Audit SHA-256 values are
`b55d0a4e2542a83cbea7e6d7bd946ecfab3c44b48beefb98f0f9d6b5270ab6b2`
for `artifacts/current/components-v7-checkbox-content-catalog-x64/audit.json`
and `304feb75ee6d3db04998f3a681fc805b51f20df0ca24e04664465e2d1b1f79ba`
for `artifacts/current/components-v7-checkbox-content-consumer-x64/audit.json`.
Full visual, physical-input, motion, scaling and ARM64 runtime checks remain open.

A subsequent Label association probe against the extracted v7 x64 catalog
failed two of 28 cases because its test host expected the visible caption after
the caller set an explicit empty automation name. The native label and associated
input correctly retained empty names in Light and Dark. The corrected source
catalog passes 28 cases, and a test consumer compiled from this exact NuGet
package, but the corrected consumer has no native runtime result. The original
v7 Label failure is retained in the [focused checkpoint](../evidence/label-explicit-empty-v7/checkpoint.json);
the framework-dependent and temporary host-overlay runtime attempts also failed
before a valid Label result. The original self-contained consumer host was
restored byte for byte. No Label acceptance is claimed.

The extracted v7 x64 catalog subsequently passed focused real foreground
Label pointer and keyboard routes in explicitly selected Light and Dark themes:
plain/rich/linked label focus, typing, Tab skip and both nested-button actions.
The native Help button took pointer focus; the non-tabbable Pointer action left
focus on the input. The [physical-input checkpoint](../evidence/label-physical-v7/checkpoint.json)
binds both reports to this package's Orchard DLL. The complete Label physical,
visual, scaling, consumer and ARM64 matrices remain open.

A separate consumer project restored this exact NuGet package with a shared
third-party cache, published x64 and passed its final full runtime smoke after
moving the existing Label and input into view. Real Tab, pointer and typing
reach that packaged input in System theme-toggle mode; the native input retains
its label association. The [consumer physical checkpoint](../evidence/label-consumer-physical-v7/checkpoint.json)
preserves the exact hashes, an intermittent Column Toggle smoke failure, and
reverted scroll/theme experiments that crashed at runtime. Private-cache
restore ran out of disk, so this is not a new fully isolated dependency restore.
Explicit Light/Dark consumer physical input remains unverified.

The same extracted v7 x64 catalog later passed real Button pointer activation
in Light and Dark: consumer command delivery, loading rejection/restoration and
the self-loading Start/Finish actions. The [Button pointer checkpoint](../evidence/components-v7-button-pointer-v1/checkpoint.json)
retains both passing reports and the earlier probe timing failures. This adds
focused runtime evidence to the unchanged package; the full Button matrix and
independent-consumer pointer route remain open.

The independent ARM64 consumer **did not publish**. Its fresh restore succeeded,
but the self-contained publish hit `MSB3021` while copying ARM64 runtime files
after C: reached zero free bytes. The verifier removed its temporary restore
workspace; a 450-file, 158 MB partial app output remains. The ARM64 catalog
archive is a verified build artifact, not native ARM64 runtime evidence. A
separate fresh, framework-dependent `-CompileOnly` ARM64 consumer now **builds**
from the exact v7 package with zero warnings/errors. It verifies package SHA-256
`32f9566b…`, byte-identical Orchard DLL `ff3e8379…` and an Arm64 PE apphost;
its temporary restore workspace was removed. The 59-file, 52.67 MiB output is at
`artifacts/current/components-v7-consumer-arm64-compile-verified`. The result
and cleanup SHA-256 values are `9418b55d7a54618aef7e9b7c190d6735b20236c0d1f2f9e7902a37797edc9f9a`
and `005eead21facbcbe06cd8c14518a02736e81fcc807c15bdf12dee41929334cfd`.
This does not repair the failed self-contained publish or verify native ARM64
runtime. Data Table's performance budget and
other inventory acceptance gaps remain open. The inventory is 112 entries
started and zero accepted; that is not a completion percentage. GitHub Actions
were not triggered, and the unavailable wiki remote leaves this record in the
repository.

The [delivery checkpoint](../evidence/components-v7-local/checkpoint.json)
binds the exact reports and hashes. The old x64 `publish/publish` output could
not be removed because automatic approval review blocked deletion, so v7 used
fresh named publish directories. The same review blocked deletion of the failed
ARM64 consumer's partial app output. Keep these generated directories out of
success claims and clean them only through an authorized cleanup path.
