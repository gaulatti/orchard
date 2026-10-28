# Label preview

`BleeckerLabel` presents consumer-owned text or rich content with Bleecker label
typography. Merge Orchard resources as described in the package setup guide.

```xml
<orchard:BleeckerLabel Content="Email Address" Target="{Binding ElementName=EmailInput}" />
<TextBox x:Name="EmailInput" Style="{StaticResource BleeckerTextBoxStyle}" />
```

The default template uses bundled Encode Sans Medium at 13px, a 13px text line
box, zero tracking and the primary text theme brush. Plain string content wraps
within the consumer's width. UIElement content and ContentTemplate/selector
content use a separate presenter; consumers own their rich-content layout and
should provide an explicit accessible name when needed.

The label adds no Tab stop. A plain-label tap requests pointer focus on its
enabled Target. Nested buttons and native input controls retain their own actions even when excluded from Tab navigation. Other tabbable controls also retain their own actions.
While loaded, the label supplies the target's AutomationProperties.LabeledBy
relationship. Detaching or changing Target restores the earlier relationship if
the consumer has not replaced it in the meantime. Explicit input automation
names remain consumer-owned. Use one owning label per Target.

The temporary association preserves the target's original local-value or style
precedence and original runtime binding object. A captured binding is detached
before assigning the temporary label, so a two-way source does not receive that
temporary value. Detach, retarget and unload reconnect the original binding to
its current source value. Originally unset values are cleared; an explicitly
local null stays local. Newer consumer bindings and local/style ownership are
preserved even when their current effective value is the temporary label.

IsEnabled=false is the explicit Windows disabled adaptation and renders label
content at .6 opacity. It is independent of Target.IsEnabled: the source's
disabled appearance comes from a preceding disabled CSS peer, not automatically
from its associated input. A disabled Target cannot receive label-requested
focus. Disabled pointer cursor behavior remains pending.

The label has no error enum. The catalog reproduces the pinned error story with
consumer styling: terracotta in light appearance and primary text in dark. See
[the reference audit](label-reference.md) for that CSS precedence detail.

This is an in-progress component. Exact glyph/baseline matching, empty and rich
content comparison, the full pointer and nested-action matrix,
live appearance, text/display scaling and ARM64 runtime remain unaccepted. Narrator
and OS contrast campaigns are excluded by [the library scope](library-scope.md). Catalog
coverage includes plain, disabled, empty, wrapped, non-Latin and rich labels;
the package consumer exercises the public typography and association contract.

Verification: artifacts/smoke-label-geometry passes both themes, including the 13px line box, disabled opacity within one-millionth numeric precision, wrapping, accessible name changes, association removal and explicit consumer-name precedence. Light/dark captures were inspected. Physical dark-theme label click focused the input and accepted typed text (artifacts/label-pointer-focus.jpg); one Tab moved directly to the nested Help button without stopping on intervening labels (artifacts/label-tab-order.jpg). The complete physical matrix remains open. All 13 tests and x64/ARM64 builds pass; ARM64 runtime remains unavailable.

The independent x64 NuGet consumer passes at artifacts/package-consumer-label-preview, including the public label font, size, accessible name and association.

Content and lifecycle regression passes in both themes at artifacts/smoke-label-lifecycle: DataTemplate updates, returning to plain text, template selection, UIElement replacement and visual detachment, Target transfer, preservation of a newer consumer association, unload/reload restoration, and the native input peer's labeled-by relationship. The dark template capture was inspected. These checks exercise the existing library; no product implementation change was required.

Live appearance checks pass in both directions at artifacts/smoke-label-theme-cursor: normal text follows primary text, the consumer error label changes between terracotta/light and primary/dark, and dimensions and association remain unchanged. A separate physical disabled-cursor probe on a fully visible maximized catalog still displayed an arrow despite assigning UniversalNo to ProtectedCursor (artifacts/label-disabled-cursor-full.jpg). That ineffective assignment was removed; disabled-cursor parity remains open.

Nested-action verification: artifacts/smoke-label-nested passes both themes, including UIA invocation of a nested non-tabbable button. Earlier physical dark-theme clicks incremented the consumer action count without redirecting to RichInput (artifacts/label-nested-pointer-focus.jpg). The current v7 foreground check below distinguishes the tabbable Help button, which receives pointer focus, from the non-tabbable Pointer action, which leaves focus on EmailInput. x64 publish and ARM64 build pass. The complete nested input/control matrix remains unverified.

The catalog's linked-label lifecycle example toggles a temporary label and
changes the original caption through fictional local state. The shared hidden
`LabelAssociationFixture` covers original local/null/unset/style values,
one-way/two-way bindings, newer same-value consumer bindings/styles, bound
Target transfer, unload/reload, rich content and disabled metadata. Run
`scripts/verify-label-association.ps1` against the catalog or a fresh package
consumer. It does not focus a control, invoke an enabled button, activate a
window or send hardware input. These checks do not establish physical label
activation, actual display/text scaling or native ARM64 runtime acceptance.

Local preview `0.1.0-label-ownership-v1-local` passes 28 source, 26 isolated
package and 28 extracted-catalog checks, with 222 recorded relationship/name
observations across those hosts. Each audit rejects 29 corrupted reports and
the actual earlier source-mutation/lost-binding report. Before the repair,
16 source checks failed; the two-way source received the temporary label.
The same audit also reproduced Button Group writing temporary geometry into
all five two-way producers. Both repairs pass in source and package, and the
two label catalog captures are byte-identical after archive extraction.
Both root/consumer build architectures and all 505 unit/text tests pass.
All inventory acceptance remains open under the broader platform contract.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).

The source-built catalog's 2026-10-27 combined presentation run passes the
Light Labels fixture after its live-theme check targets `CatalogRoot`, the
fixture's actual theme owner, instead of the outer modal host. That check
changes to Dark and restores Light while preserving geometry and input-label
association. The run later fails in Input shadow remount; a fresh Dark Labels
sequence and new package qualification remain unverified. See
`docs/evidence/catalog-presentation-theme-root-v1/checkpoint.json`.

The extracted v7 x64 catalog's association probe exposed a stale fixture
expectation in both themes. After explicitly setting the label's automation name
to empty, native Label and associated Input correctly report empty names while
retaining their relationship; the fixture had expected the visible `Plain caption`
instead. Only those two of 28 cases failed. The corrected fixture and independent
audit now require the empty names and reject fallback to the caption. A rebuilt
source catalog passes all 28 cases and the 30-case negative audit; the corrected
consumer compiles from the exact v7 NuGet package, but its framework-dependent
build does not provide runtime evidence: the thin host cannot resolve
`BleeckerTextBoxStyle` before Label checks, and a temporary corrected-host overlay
on the self-contained v7 consumer exited before writing a report. The original
consumer DLL was restored and hash-checked. The delivered v7 catalog's original
failed result remains failed. See the [checkpoint](evidence/label-explicit-empty-v7/checkpoint.json).

The corrected 26-case association fixture now passes in the full x64
PackageConsumer using the exact published `0.1.0-preview.20261027.7` NuGet DLL.
Both Light and Dark explicit-empty-name cases, caller binding identity and
precedence, two-way source preservation and all 566 isolation guards pass; the
hidden host exits zero. The [independent-consumer checkpoint](evidence/label-v7-independent-association-v1/checkpoint.json)
retains the report, audit, exit record and package/host hashes. This resolves
the earlier thin-host runtime gap without changing Label source or the package.
Broader physical input, scaling, full rendering and native ARM64 acceptance remain open.

The extracted v7 x64 catalog also passes a real foreground pointer/keyboard
route in both explicitly selected Light and Dark themes. `scripts/verify-label-desktop.ps1`
selects the theme through the catalog, opens Labels, clicks the plain label,
types into its target, Tabs past non-tabbable labels to Help, clicks rich-label
text and both nested buttons, then clicks the linked lifecycle label. The
plain/rich/linked labels focus their respective inputs. Both nested buttons
invoke the consumer's action; Help takes native pointer focus, while the
non-tabbable Pointer action leaves focus on EmailInput. The [physical-input
checkpoint](evidence/label-physical-v7/checkpoint.json) binds both reports to
the exact v7 Orchard DLL. It does not verify disabled-target activation, every
nested control, complete rendering, scaling, package-consumer physical input or
native ARM64 runtime. The initial probe's fixed action-count expectation and a
PowerShell variable-shadowing error were corrected in the test harness; their
failed reports remain separate from the two final passes.

The exact v7 NuGet package also publishes in a separate x64 consumer project
after moving its existing Label and input to the visible top of the consumer
card. A normal consumer smoke passes resources, fonts, Light/Dark presentation,
layout and native actions. In System theme-toggle mode, real Tab skips the
Label, a pointer click focuses the packaged input, and typed text reaches the
consumer-owned value; the input's automation relationship still points to the
public Label. The [consumer physical checkpoint](evidence/label-consumer-physical-v7/checkpoint.json)
binds the report, exact package hash and retained failures. This build reused
the machine's third-party NuGet cache after a private-cache restore ran out of
disk, so it is not fresh isolation of all dependencies. An attempted scroll
wrapper and live theme-toggle wiring crashed during runtime and were reverted.
One full consumer smoke run also timed out in Column Toggle; the next and final
runs passed. Stability of that unrelated smoke check remains open.

A later [exact-v7 consumer check](evidence/label-v7-consumer-physical-themes-v1/checkpoint.json)
passes real Label pointer, Tab and typing routes in separate System, Light and
Dark runs. The consumer physically accepts the Light and Dark theme requests
before Label input; its packaged Input keeps its `LabeledBy` relationship in
every mode. All three processes exit with code zero. The earlier v7 Theme Toggle
lifecycle evidence separately confirms visible Light/Dark backgrounds. This
closes the explicit-theme independent-consumer input gap. Disabled-target and
broader nested-control input, complete rendering, actual scaling and native
ARM64 runtime remain open.
