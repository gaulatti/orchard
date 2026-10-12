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
live appearance, contrast,
Narrator, text/display scaling and ARM64 runtime remain unaccepted. Catalog
coverage includes plain, disabled, empty, wrapped, non-Latin and rich labels;
the package consumer exercises the public typography and association contract.

Verification: artifacts/smoke-label-geometry passes both themes, including the 13px line box, disabled opacity within one-millionth numeric precision, wrapping, accessible name changes, association removal and explicit consumer-name precedence. Light/dark captures were inspected. Physical dark-theme label click focused the input and accepted typed text (artifacts/label-pointer-focus.jpg); one Tab moved directly to the nested Help button without stopping on intervening labels (artifacts/label-tab-order.jpg). The complete physical matrix remains open. All 13 tests and x64/ARM64 builds pass; ARM64 runtime remains unavailable.

The independent x64 NuGet consumer passes at artifacts/package-consumer-label-preview, including the public label font, size, accessible name and association.

Content and lifecycle regression passes in both themes at artifacts/smoke-label-lifecycle: DataTemplate updates, returning to plain text, template selection, UIElement replacement and visual detachment, Target transfer, preservation of a newer consumer association, unload/reload restoration, and the native input peer's labeled-by relationship. The dark template capture was inspected. These checks exercise the existing library; no product implementation change was required.

Live appearance checks pass in both directions at artifacts/smoke-label-theme-cursor: normal text follows primary text, the consumer error label changes between terracotta/light and primary/dark, and dimensions and association remain unchanged. A separate physical disabled-cursor probe on a fully visible maximized catalog still displayed an arrow despite assigning UniversalNo to ProtectedCursor (artifacts/label-disabled-cursor-full.jpg). That ineffective assignment was removed; disabled-cursor parity remains open.

Nested-action verification: artifacts/smoke-label-nested passes both themes, including UIA invocation of a nested non-tabbable button. Physical dark-theme clicks increment its consumer action count and preserve focus in the previously focused EmailInput rather than redirecting it to RichInput (artifacts/label-nested-pointer-focus.jpg). x64 publish and ARM64 build pass. The complete nested input/control matrix remains unverified.
