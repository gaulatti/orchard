# Native Windows runtime evidence

The catalog fixtures and independent NuGet consumer at commit `ebfb6ac` passed
on both x64 and native ARM64. That runtime baseline covers 20 in-progress
inventory entries; 92 entries remain pending. The earlier `5412997` checkpoint
covered 18 entries. Further implementation changes require their own native checks.
No component has full visual, input, accessibility and scaling acceptance.

Cursor evidence from earlier physical fixtures uses the UI thread's GetCursor
handle. That does not independently establish the global displayed cursor;
Microsoft documents GetCursorInfo for that observation. Current Checkbox and
Input transition diagnostics now retain both, including global visibility and
screen position. No visible-cursor acceptance follows from earlier thread-only
handle assertions. Rendered hover borders/opacity are independent observations.

At `706ee10`, ARM64's global cursor report proves the enabled Input displays
IBeam while its UI thread reports Arrow. It also proves the disabled Checkbox
still displays Arrow while its UI thread reports UniversalNo. Archive
`11780820812` was downloaded and verified, SHA-256
`6f75a65e7a63d3981ed16ad35774da3037a377d316998ae28764bb8f061337a8`.
The current candidate replaces the thread-only override with the root's
InputPointerSource cursor and strengthens both fixtures to require the visible
global handle. This is a new acceptance boundary, not a pass inferred from
earlier cursor properties. The prior `ae3e44f` run passes all three jobs under
the earlier gate; new runtime checks must prove the displayed override.

## Rendered Checkbox names and resting Input validation

At `9c5699b`, native ARM64 passes all 57 Checkbox disabled-pointer phases in
Light and Dark, then loses foreground to a background WSL console before its
next Dark click. Its archive `11779778593` has SHA-256
`dbc4ed9ab90e3a9d276044bbb3897e5a07e9ccc8509c34aca64ec67902b8a3fc`
and was downloaded and verified. x64 passes the 16 new rounded-corner samples
in Light, then reproduces the Input thread-cursor failure. Its verified archive
`11780455227` has SHA-256
`c887b2411136103e751435370f489443cb94b46d1799d9b700307276328344ff`.
The newer `f8c67a8` ARM64 catalog/consumer passes, but x64 still fails that
cursor check. The global cursor observation remains required for visible-cursor
acceptance; these runs do not establish full parity.

The newer `bf910af` pointer candidate in
[run 38322267056](https://github.com/gaulatti/orchard/actions/runs/38322267056)
builds and passes 23 tests on both architectures. Both native catalogs pass
Checkbox remount/rapid-remount, clipping, scrolling and RTL, then fail the
enabled Input I-beam transition. Neither reaches package-consumer verification.
The archived x64 evidence `11779656182` has SHA-256
`b86793c623784cf134e9a301b7566d5e287a173a97cca0302438eabf8265cb55`;
ARM64 `11779716166` has SHA-256
`3ae86f7f376834f8486b1fd80496b06955492acc607a38fc24b23e371acff86c`.
Both archives were downloaded and verified. This partial run does not replace
the complete `ebfb6ac` baseline above.

The diagnostic `8b8be3c` run
[38323217756](https://github.com/gaulatti/orchard/actions/runs/38323217756)
passes the complete x64 catalog and package consumer. ARM64 passes the remount
and geometry phases, then reproduces the enabled Input cursor failure: WinUI's
cursor properties select IBeam while the actual native handle remains Arrow.
The ARM64 archive `11780000790` has SHA-256
`05b5ecb34d8f565c9db107ce6a8e6593c6b28c00735ad569e9120848ec34e325`
and was downloaded and verified. The release correction retains the physical
I-beam acceptance check; it needs new native evidence on both architectures.

Commit `ebfb6ac` passes all three jobs in
[run 38319267360](https://github.com/gaulatti/orchard/actions/runs/38319267360).
Both native architectures pass the complete Light/Dark catalog, 23 tests and
independent NuGet consumer. Eight strict rich-name cases pass in each appearance
on each architecture. Dark Input's diagnostic explicitly selects
`InputNormalError`, with hit testing temporarily disabled, `#FFD47B75` rendered
and palette border, and full enabled editor opacity. This verifies the resting
fixture correction; physical hover remains a separate contract.

The x64 archive `11778171441` has SHA-256
`c171a20fc9f0a60527c4769c5248f2fee3fcc7329ccd3d92c38784b5da44c152`;
ARM64 archive `11779000272` has SHA-256
`39c640f5ca815ae4e8c16967e2f44ffdd0b052f8f4a6760ef44491c5cdaec705`.
Both were downloaded and verified under `artifacts/ci-checkbox-ebfb6ac-{x64,arm64}`.
Package SHA-256 is `c02bb6b15080c6e585f9ba5e60416bda1411f97e2a152bcd1d7d7ff916525bd7`
on x64 and `1cb0a84836b518eb5ef956a2d424ede44ff491d865e5b8bae13e043e2718f0b5`
on ARM64. The 57 font probes remain diagnostic; full font, Narrator, scaling,
contrast, motion and component parity acceptance remains open.

## Checkbox and Switch native runtime checkpoint

Commit `0e354939d430a06ead949c0fc98cb198b4f46e42` passes all three jobs in
[run 38316642519](https://github.com/gaulatti/orchard/actions/runs/38316642519).
Both runtime jobs build/test/publish the catalog and pack/run an isolated NuGet
consumer. Their 23 unit/resource tests pass. Native reports confirm:

| Evidence | x64 | ARM64 |
|---|---|---|
| Process / OS architecture | X64 / X64 | Arm64 / Arm64 |
| Windows build | 26100 | 26200 |
| .NET runtime | 10.0.12 | 10.0.12 |
| Display / text scale | 1 / 1 | 1 / 1 |
| Windows animations | Disabled | Enabled |
| Light / Dark catalog | Passed / passed | Passed / passed |
| Independent package consumer | Passed | Passed |

The Checkbox reports include real hover/held press/release/exit, physical
Space/Tab, disabled skipping/rejection, box/label/description/RTL clicks,
exactly-once command/value events, consumer binding and template/visibility
lifecycle. Composed pixel samples verify the focus ring beyond both box edges
and its offset. The Switch also passes both appearances and native physical
input. All 48 Checkbox font-probe dimensions agree across architectures, but
the probes identify remaining source font mismatches rather than accepting them.

Package 0.1.0 SHA-256 is
`9d584e2bb3e9135b8ce33813645230305e235a1c9a6e98403ad9c20f9a52b8fe` on x64 and
`b16df6883c1b513ff2846890071832c657280095057cb4f301747d695a15d8b5` on ARM64.
The x64 artifact `11777456732` has SHA-256
`8cad4468b21f813414b8b5f72b6a67d73b27be49699eb91b0217aa816acb03b5`;
the ARM64 runtime artifact `11777611609` has SHA-256
`2e90b8f3d321d114fdcd8225bc7c52261295442a4a331d3a8108929aa0d54d11`.
Both archives were downloaded and hash-verified. Selected reports/captures
are retained under `artifacts/ci-checkbox-0e35493-{x64,arm64}`.

The prior `3485f52` ARM64 run passed Light, then failed Dark Input validation.
The new diagnostic reports show correct InputNormalError, focus, palette and
rendered settlement in this run; the previous failure's cause remains unproven.
The Checkbox rich-name diagnostics also expose model string representations
for data templates. These gaps and the full acceptance matrix remain open.

At `9c6a8ce`, [run 38318155884](https://github.com/gaulatti/orchard/actions/runs/38318155884)
passes x64 catalog/consumer and ARM64 compilation. The Checkbox's rendered
rich-content names pass eight regression cases in both x64 appearances and
ARM64 Light, including live text, updated label/description data templates,
collapsed content, explicit subtree names and owner name/label precedence.
ARM64 then fails Dark Input's resting validation assertion: diagnostics show
`InputHoveredError`, matching palette/rendered `#40FFFFFF` and opacity 1. This
identifies pointer interference in that run; it does not retroactively prove
the cause of the undiagnosed `3485f52` failure. ARM64 Dark Checkbox and its
independent package consumer were not reached.

The x64 artifact `11777688804` has SHA-256
`835d436b6027e12b8966a12e5acbf2052816d52e5c27b8e77f91f431360f72c7`;
ARM64 artifact `11777753571` has SHA-256
`3699facdbd33c56922586c58cd4130ef5f8cf07bda136c6680322ed315f8bb5d`.
Both were downloaded and verified under `artifacts/ci-checkbox-9c6a8ce-{x64,arm64}`.
The resting Input fixture correction passes at the later `ebfb6ac` checkpoint
above. No full component acceptance is claimed by these partial results.

## Switch x64 runtime

Commit `7001e3e46627263c909e3b324b139681e5f2434a` passed the x64 job in
[run 38283953920](https://github.com/gaulatti/orchard/actions/runs/38283953920).
The catalog report records X64 process and OS, Windows 10.0.26100, .NET 10.0.12,
display/text scale 1 and animations disabled. In both light and dark, the Switch
fixture passed geometry/palette, first-display wrapping, UIA state/name/focus,
disabled rejection, consumer binding, template/unload/reload, live appearance,
reduced-motion settlement, physical Space/Tab, track/label clicks and RTL
activation. Native input required the pinned SDK's transparent SwitchThumb part.
This run does not verify intermediate animation frames.

The x64 artifact is `11763240141`, SHA-256
`c4163aae82022a2897c76bc2d0f8589c82183166708dbd72881f01a9fa0d2256`.
The downloaded archive was hash-verified at
`artifacts/ci-switch-7001e3e-x64.zip`. Selected reports and composed client
screenshots are under `artifacts/ci-switch-7001e3e-x64/`. The client captures fit
the physical monitor and include the composition thumb shadow.

The independent consumer reports runtime success for package 0.1.0, SHA-256
`601082af3516cf0eab9f2f28c09da57ead2968009b54d9e1e46aa9cf199055de`.
Its process and OS are X64. The ARM64 cross-build/isolated-consumer build also
passed. The native ARM64 job reached the Switch desktop check but could not
acquire foreground input; the independent ARM64 runtime consumer was skipped.
The next diagnostic uses the XamlRoot AppWindowId as the authoritative HWND and
retains the foreground/session/input-desktop state. This is not an ARM64 pass.

Full Switch acceptance remains open. The natural Latin row measures
173.272018px against the browser's 173.296875px, while the 200px constrained row
retains four lines and a 152px text allocation. Japanese fallback glyphs measure
99.538086px against 111.453125px in the reference, a visible width mismatch that
must be resolved. The browser captures are JPEG despite their .png filenames;
they do not support a lossless pixel-equality claim. Exact glyph/pill/shadow
raster comparison, animated state/appearance transitions, drag/cursor/focus,
contrast/Narrator and isolated scaling remain unaccepted.

## Switch motion and hosted desktop follow-up

At `f725ecea147cf88b0d6c1e57092f57e6fd198b96`,
[run 38291211362](https://github.com/gaulatti/orchard/actions/runs/38291211362)
passed x64 catalog/runtime and the independent consumer. Package 0.1.0 reports
SHA-256 `de7c3c134dc77180bc117b27fd1e6db2e0ab2a00bdd6fd27d88d61c697b34487`.
The x64 artifact `11766796078`, SHA-256
`1ce00b21cae104a7c2db1cbb3a5e7d1733f3e1399a7d8316b15b2e93a6dd0f16`,
was downloaded and hash-verified under `artifacts/ci-switch-f725ece-x64`.

The native ARM64 job passed the Switch's Light appearance and motion checks
before its physical-input failure. Animations were enabled. Separate reports
retain intermediate track colors, off alpha, thumb movement, reversal and a
palette change during a toggle. The appearance report marks `passed: true`;
motion verification returned before physical input began. This verifies these
Light-started checks, not the unexecuted Dark-started or full input matrix.
The ARM64 artifact `11766885762`, SHA-256
`bc060aefe622c54bdcb241e0067857413aa477659a6079b735b0b942588140b1`,
was downloaded and hash-verified under `artifacts/ci-switch-f725ece-arm64`.

The ARM64 session is active console session 2, WinSta0/Default, WTS state 0.
The catalog is visible, uncloaked and topmost. The intended point is instead
covered by a topmost `WWAHost` CoreWindow in the same session, and the Alt retry
still cannot activate the catalog. Additional diagnostics query that process's
application identity without its title or content. The physical check continues
to reject an unowned target; the ARM64 consumer runtime remains skipped.

Font probes at `fc93aa2` returned identical x64/ARM64 widths: 99.538086px for the
default bundled family, and the same result with Segoe UI/Arial appended.
Microsoft YaHei UI or MS Gothic produces 112px, which still differs from the
111.453125px browser reference. These are diagnostic candidates, not accepted
font mappings. Motion raster parity, fallback glyphs and the other acceptance
gaps remain open.

The Switch follow-up at `19c1cb25a3ec00a828f77659cd7357773574eded`
([run 38293032359](https://github.com/gaulatti/orchard/actions/runs/38293032359))
passed x64 runtime and independent consumption, and ARM64 cross-build. The
native ARM64 check instead stopped at live appearance: the on/off pair had
settled while DisabledOff had not yet met its palette assertion. It did not
reach the application-identity diagnostic or physical input. Artifact
`11766798546`, SHA-256
`2e997121a289bfda2b3f1df4ca34a63296080a38a46492935d7f806d38c2e007`,
was downloaded and hash-verified under `artifacts/ci-switch-19c1cb2-arm64`.
The next fixture waits for every row's fill and border endpoints and retains
all row values on failure. This run does not replace the shared baseline below.

At `4b91993af77a7a56d6aa774e26e15180e9250749`,
[run 38294745658](https://github.com/gaulatti/orchard/actions/runs/38294745658)
passes all nine rows' alternate/restored palette endpoints on native ARM64,
with the Light-started motion checks returning before physical input. The
target-ownership diagnostic identifies the covering welcome app as
`Microsoft.Windows.CloudExperienceHost_cw5n1h2txyewy!App`, in the catalog's
active session. Artifact `11768680948`, SHA-256
`61c451ca0f2c65174fa5f749739bf234c13c9dd065fbd07abd595da11b042882`,
was downloaded and hash-verified under `artifacts/ci-switch-4b91993-arm64`.
Physical input, Dark-started checks and the ARM64 consumer remain unexecuted.
The next hosted check requests normal minimization of that exact foreground
application and keeps all foreground, occlusion and ownership gates active.
It does not change the developer's desktop or accept welcome preferences.

At `3a26b98caceabf12e0c07e459f49025a6b225bd4`,
[run 38295643362](https://github.com/gaulatti/orchard/actions/runs/38295643362)
passed x64 runtime/consumer and ARM64 cross-build. The native ARM64 welcome
report records a successful minimization request for the identified app, but
the pointer target belongs to a topmost, visible, uncloaked explorer window
with class `UserOOBEWindowClass` in session 2. The physical gate correctly fails;
the request does not prove that foreground was acquired. Artifact `11768162672`,
SHA-256 `c3bc0e94baaaf4ea5b909180b251ed80008bab8242f70e353586b2dcb21b326e`,
was downloaded and hash-verified under `artifacts/ci-switch-3a26b98-arm64`.
The next preparation also requests normal minimization of that exact welcome
backdrop, gated by the app identity, explorer process name and matching session.
Dark-started checks, physical input and ARM64 consumption remain unverified.

The first Checkbox runtime at `79f10535647973b89da1e29cd2853f0d0f24d10b`
([run 38297199784](https://github.com/gaulatti/orchard/actions/runs/38297199784))
failed its initial glyph geometry check on x64 and ARM64; ARM64 cross-build
passed. The x64 report/capture was downloaded and hash-verified from artifact
`11768994195`, SHA-256
`b655bea08cd5cff8d33c53f5fc4cc01d56eaa7cf5715dbd569ea13fce1f8b49c`,
under `artifacts/ci-checkbox-79f1053-x64`. Default text measures
291.700043×42px, and the image exposes RTL checkmark mirroring. The follow-up
adds an explicit square SVG viewport and preserves its left-to-right geometry.
Remaining behavior checks were not reached. Both local architecture builds
and 23 unit tests passed. A locally packed isolated x64 consumer build passed
without runtime launch; package SHA-256 is
`09fd119f4665472b3a9349ed44b5cad46a272da17b903e6f5a90f4f870ee21f6`.

At `a4720b2b25f669141835a8c2e46c4cf7453f9aa4`,
[run 38299428078](https://github.com/gaulatti/orchard/actions/runs/38299428078)
passed both architecture builds and all 23 unit tests. Both native jobs
passed Checkbox's initial geometry, UIA role/name/value and exactly-once events,
disabled rejection, binding in both directions, content updates, focus modality
and unchecked visual states. Both then failed while clearing Template, before
motion, live appearance, Switch or independent runtime consumption. The x64
report retains the original XAML parsing exception and seven verified steps.
Artifact `11770053223`, SHA-256
`8274634f4728b7e884e8f2dd02904c64e1181c9bc36aa381f8f2f54ea20d33d2`,
was downloaded and hash-verified under `artifacts/ci-checkbox-a4720b2-x64`.
Its initial capture confirms the corrected RTL checkmark and exposes the
37px rich-content row. The actual pinned browser matrix measures that row
165.109375×42px and confirms dark deep-sea #182533, dark sea #5BA3F5 for
hover/focus, and the dark focus offset. Those reference reports/captures are
retained under `artifacts/checkbox-reference-matrix-{Light,Dark}` and
`artifacts/checkbox-reference-focus-Dark`. The follow-up fixes those contracts
and adds native boundary checks; it does not establish their runtime acceptance
until the hosted checks return. The shared baseline remains unchanged.

At `48386d4`, [run 38300873774](https://github.com/gaulatti/orchard/actions/runs/38300873774)
again passed builds and all 23 tests. The new native rich-content boundary check
failed on both architectures at 165.083999×37px. The owned TextBlocks retain
LineHeight 0 / MaxHeight despite the presenters' LineHeight 20. The follow-up
supplies scoped TextBlock style defaults, preserving explicit consumer
formatting. That run did not reach template replacement, motion, appearance,
Switch or independent runtime consumption.

At `d0a66dd`, [run 38301875168](https://github.com/gaulatti/orchard/actions/runs/38301875168)
passed builds and unit checks, then failed the rich row at 39.5px on both
architectures. The description resolved the presenter-scoped 20px style;
the native Content-owned label retained its 17.5px automatic line. The next
candidate scopes the default style to the checkbox's private dictionary and
adds consumer style/local-property/data-template regression checks.

At `84e6ef2`, [run 38302708416](https://github.com/gaulatti/orchard/actions/runs/38302708416)
passed builds and all 23 tests. Both native jobs reached the consumer data
template assertion after the default rich row and explicit style/local line
overrides passed. The next fixture waits for binding propagation and retains
template text/layout diagnostics before restoring the original rich content.
Template clearing, motion, appearance, Switch and independent runtime
consumption remain unreached. A separate local isolated x64 consumer build
passed with runtime disabled; its package SHA-256 is
`644e34e16dc42e5ac0466692c0bdc2e9e6c9654eb85a6d254b0edf8007114131`.
The x64 captures/report were downloaded and hash-verified from artifact
`11770703516`, SHA-256
`f4714cf3c154b40a7a9f8ae0495b85980ed3d77c4d162a42c2248e2a6c29e7aa`,
under `artifacts/ci-checkbox-84e6ef2-x64`.

At `d382242`, [run 38304146283](https://github.com/gaulatti/orchard/actions/runs/38304146283)
shows that consumer template binding succeeds on x64 and ARM64. Both reports identify
correct strings and the expected ConsumerLabel data context, but each template
line remains LineHeight 0 / MaxHeight at 17.5px, yielding 57px rather than 62px.
The next candidate supplies the source line defaults to template-generated
text only when no local value, binding or style setter supplies them. It keeps
the consumer template/styles intact and removes owned defaults on detachment.

At `7a0def1`, [run 38305820220](https://github.com/gaulatti/orchard/actions/runs/38305820220)
passes builds/unit checks and the rich-content native boundary checks on both
architectures, then fails at ApplyTemplate after assigning null. The x64 report
and captures were hash-verified from artifact `11771893712`, SHA-256
`1b3cff8a4a96e64d04e36b57803855a01ce911b350e794efeb379de74507f11f`,
under `artifacts/ci-checkbox-7a0def1-x64`. Template phase diagnostics and the
original managed exception trace will distinguish clearing from native style
resolution; no lifecycle assertion has been relaxed.

At `775c1b1`, [run 38306688055](https://github.com/gaulatti/orchard/actions/runs/38306688055)
records zero visual children immediately after null assignment and a rebuilt
Grid after original-template restoration. The x64 original failure remains the
explicit native ApplyTemplate(null) invocation; no managed override error is
traced. Null-property cleanup now detaches Orchard observers and owned content.
The fixture verifies empty-tree layout and native value/content/name updates before restoring
and applying the nonempty template. Explicit native ApplyTemplate(null) remains
a documented SDK limitation, rather than a verified library capability.
The snapshots were hash-verified from artifact `11772384171`, SHA-256
`e3ff9a0c9c1476ce416b231fdadac218461e3c944ddc26b71e537290dd155510`,
under `artifacts/ci-checkbox-775c1b1-x64`.

At `ec16816`, [run 38307865801](https://github.com/gaulatti/orchard/actions/runs/38307865801)
fails during normal null-template layout with the original managed error,
`BleeckerCheckbox requires PART_Box`. Thus the failure is not limited to an
explicit application call: native theme lookup selects non-Orchard chrome.
The next correction clears the default style key while empty and restores it
with a nonempty template. Verification retains explicit ApplyTemplate(null),
normal empty layout, consumer state/content/name updates and restoration.
The x64 font probes and captures were downloaded and hash-verified from
artifact `11773291932`, SHA-256
`da33f36824120a31e03325164059c68414fdfa2026f0cde9278240516f0a81c1`,
under `artifacts/ci-checkbox-ec16816-x64`. Its explicit Yu Gothic probe measures
111.439453px against the source's 111.453125px; the default is 99.538086px.
This is retained probe evidence, not a change to the shared font stack.

At `2bf7205`, [run 38309805067](https://github.com/gaulatti/orchard/actions/runs/38309805067)
passes builds/unit checks and rejects post-initialization DefaultStyleKey
changes with COMException 0x80004005 on both architectures. The mutation has
been removed. DefaultStyleResourceUri is now set during construction to
Orchard's Generic.xaml, using the pinned SDK property documented by
[Microsoft](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.control.defaultstyleresourceuri?view=windows-app-sdk-2.0).
The same null application/layout/state/content/restoration gates remain active.
At `8dccce0`, [run 38311418818](https://github.com/gaulatti/orchard/actions/runs/38311418818)
again fails null-template application on x64 and ARM64; ARM64 build/independent
consumer build passes. The x64 phase report is retained in the digest-verified
`artifacts/ci-checkbox-8dccce0-x64` archive (SHA256
`2942b4e88ab1b8e0f40871e2c08ff9293f281aff3aadbe9868a6f48624903b67`).
It proves application creates an unnamed Grid and replaces the null effective
template. Microsoft's ContentControl source explains this native content
presentation path; it is not evidence of stock CheckBox style lookup. The
constructor resource-URI experiment is removed. Lifecycle acceptance now
checks immediate detach/valid restore and normal-layout hide/show using
Visibility, including native state, content/name and rich ownership. A
persistent null-template empty state was an incorrect fixture assumption;
the consumer Template property and bindings remain untouched by Orchard.
At `7062317`, [run 38312230166](https://github.com/gaulatti/orchard/actions/runs/38312230166)
passes template detachment/restoration, then rejects the hide/show fixture on
both architectures because a fresh Toggle pattern is unavailable while the
control is collapsed. The fixture now retains the native provider while the
control is visible before checking consumer state updates during collapse.
No library behavior is changed to manufacture an accessible hidden control.
The checkbox fixture now captures an additional six-script, three-weight
font comparison with explicit language tags, before its template lifecycle
checks. The original and Yu Gothic candidates use actual bundled font
resources. Reports and captures restore the consumer content and language;
the shared library font stack is unchanged pending comparison.
At `7afa247`, [run 38311616439](https://github.com/gaulatti/orchard/actions/runs/38311616439)
completes the 36 native script/font probes on x64 and ARM64 before the known
null-template failure. Both architectures produce identical probe dimensions.
Digest-verified archives are retained at `artifacts/ci-checkbox-7afa247-x64`
(SHA256 `11e66feef27e090747da2b7e8cf0df379ab6ca395f846aabe46192cfbfc01a7f`)
and `artifacts/ci-checkbox-7afa247-arm64`
(SHA256 `c2cbcd41ed53eaab0038ddae3ff336f052bd7baf72188376c5548d5d99013c6e`).
The retained comparison exposes semibold Japanese and Arabic font differences;
native architecture agreement does not establish browser parity.

The checkbox fixture now adds physical keyboard/pointer and inherited-command
checks with a catalog-only NativeInputSession. Foreground input and pointer
hit points must belong to the catalog's own native window. Captures include
the composed keyboard/pointer window; reports retain incremental checks and
desktop diagnostics. Runtime verification of these new gates is pending.
At `3dac874`, [run 38312883131](https://github.com/gaulatti/orchard/actions/runs/38312883131)
passes x64 build, 23 unit tests, full catalog runtime and the independent
NuGet consumer (package SHA256
`ed41e1ded3ed586d6d02261229aded4b9c3f2436905a469f898fd0b16b03553b`).
ARM64 passes its build/independent consumer build and light checkbox lifecycle,
motion, physical input and command checks with animations enabled. The dark
forced hover check fails because the foreground-acquisition fallback toggled
Off during light testing and left its state selected. Acquisition now clicks
the static heading, state cleanup includes Off, and hover/held press/exit are
tested through actual input. Native captures and reports are retained in
`artifacts/ci-checkbox-3dac874-x64` (archive SHA256
`358ed4ee233eb6168cfdb39b2c5b262a5ff2e9f2f8558cb0f41157b6339d7b12`)
and `artifacts/ci-checkbox-3dac874-arm64` (archive SHA256
`bf2854db389331e44572a40214013322efde299d6297b8549cab03fe760d37ab`).
This is x64 acceptance of the current registered runtime gates, not full visual
acceptance or a new shared architecture baseline.
Inspection of the ARM64 composed keyboard capture also reveals a missing
visible focus ring despite its reported 28px size. The focus layers now use
negative layout margins, matching their outer extents without growing the
20px input. A native pixel regression checks both outer edges and the inner
offset in the captured window, after a rendering opportunity, and retains
`checkbox-focus-pixels-{Light,Dark}.json`. This correction needs fresh runtime
and rendered verification; the earlier dimension/opacity check alone was
insufficient evidence of visible focus chrome.

## Shared x64 and ARM64 baseline

The first native ARM64 run was [38267617259](https://github.com/gaulatti/orchard/actions/runs/38267617259)
at commit `403d8783416eae79086a6b59c714131a88a44121`. All three jobs passed:
x64 build/runtime/consumer, ARM64 cross-build/consumer build, and native ARM64
build/runtime/consumer. The repeat run [38268516314](https://github.com/gaulatti/orchard/actions/runs/38268516314)
at `5412997a3dfd15c76a06367b51480ab6a91b8e7c` also passed all three jobs,
including the tighter fractional Progress width regression.

The native reports from the repeat run record:

| Evidence | Observed value |
| --- | --- |
| Catalog process architecture | `Arm64` |
| Catalog OS architecture | `Arm64` |
| Consumer process / OS architecture | `Arm64` / `Arm64` |
| Windows version | `10.0.26200` |
| .NET runtime | `10.0.12` |
| Display / text scale | `1` / `1` |
| Windows animations | Enabled |
| Catalog / independent consumer | Passed / passed |
| Package version | `0.1.0` |
| ARM64 consumer package SHA-256 | `3b22c5e4b0039119c19519e2b8c300d9e205eeccc58702a66bb2074b2e987641` |

The `orchard-windows-arm64-runtime` artifact retains the runnable catalog,
independent consumer, package, 19-test report, captures and per-fixture JSON.
Artifact `11755888347` has SHA-256
`2fcbf474c4c660e45cad7cca1ab6a5caa58e066b3aa807b0a9e90b577dcbd209`.
Selected reports and captures are retained locally under
`artifacts/ci-arm64-5412997`; the earlier artifact under
`artifacts/ci-arm64-403d878` was also downloaded and its archive hash verified.

The architecture gates inspect reports produced by the running applications.
ARM64 smoke requires an ARM64 process and OS; the package consumer requires the
selected process architecture and an ARM64 OS for ARM64 verification. A build,
runner label or emulated x64 process cannot satisfy these checks. Each runtime
job currently packs and verifies its own candidate from the same source.

For Progress, the earlier x64 `4c76d59` and ARM64 `403d878` light/dark captures
are pixel-identical: zero differing pixels in each 756×502 image. The library
sources are unchanged between those commits. The comparison is retained at
`artifacts/progress-native-architecture-comparison.json`. The repeat ARM64 run
reports a 174.199996948px fill for a 268px track at 65%, preserving the
mathematical 174.2px width rather than rounding it to 174px. The pinned browser
reports 174.1875px after CSS layout-unit quantization. See [Progress](progress.md)
for the pill rendering checks and remaining reference-comparison work.

Full visual comparison across the inventory, actual contrast settings,
Narrator, display/text scaling, all motion/input states, collection performance,
the two substantial catalog compositions and the remaining components still
require acceptance evidence. The local development host remains Intel x64;
these checks ran on GitHub's ARM64 host without changing the developer's desktop
settings.
