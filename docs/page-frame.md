# BleeckerPageFrame

`BleeckerPageFrame` is a centered, full-available-width boundary around original
consumer content. It implements the fixed Bleecker `PageFrame` layout recipe.

```xml
<orchard:BleeckerPageFrame Element="Article" FrameWidth="Reading"
    Gutter="Comfortable" VerticalSpacing="Comfortable">
    <StackPanel Spacing="16">
        <TextBlock Text="Consumer heading" Style="{StaticResource BleeckerHeadingStyle}" />
        <orchard:BleeckerInput Text="{Binding Title, Mode=TwoWay}" />
    </StackPanel>
</orchard:BleeckerPageFrame>
```

`FrameWidth` accepts `Reading` (768 DIP), `Content` (1152, default), `Wide`
(1440), or `Full` (no recipe maximum). It caps the complete frame, including
padding. The frame centers within its parent and fills the available width up
to that cap. An explicit native `Width` still respects the maximum. The width
recipe depends on the parent; spacing breakpoints depend on the window's logical
`XamlRoot` viewport, even when the frame sits in a narrow container.

| Recipe | Below 640 | From 640 | From 1024 |
| --- | ---: | ---: | ---: |
| Gutter None | 0 | 0 | 0 |
| Gutter Compact | 16 | 20 | 20 |
| Gutter Comfortable (default) | 20 | 32 | 40 |
| Gutter Spacious | 24 | 40 | 64 |
| VerticalSpacing None | 0 | 0 | 0 |
| VerticalSpacing Compact | 24 | 32 | 32 |
| VerticalSpacing Comfortable (default) | 40 | 48 | 64 |
| VerticalSpacing Spacious | 56 | 64 | 96 |

Horizontal spacing applies equally to left/right; vertical spacing applies to
top/bottom. These are default `Padding` and `MaxWidth` values. Caller local
values, bindings and style setters take precedence. Recipe changes keep those
producers intact. `ClearValue` restores the current recipe. A same-value scalar
write cannot be distinguished from the owned default because WinUI raises no
property-change event; use an explicit binding/style for a persistent override
that initially equals the current recipe.

`Element` maps `Div`, `Article`, `Main` and `Section` to native passive grouping.
Main exposes the native main landmark; a named Section exposes a custom region.
Article/Section/Main retain their semantic item type. Caller automation metadata
has normal precedence. The frame adds no Tab stop and owns no command, data,
selection, authentication or navigation. Content, data templates, template
selectors, input values and native events remain with the consumer. Unknown enum
values fail explicitly. Standard native background, border, alignment and
template properties remain available.

Caller font properties retain native inheritance. A `FontSize` binding on the
frame reaches an original child TextBlock without replacing that child. Larger
fonts remeasure wrapped content and grow the frame vertically; they do not scale
the DIP-based gutter/width recipes. A local font value on the child takes
precedence, and clearing it restores inheritance. Explicit oversized child
dimensions remain caller-owned while the frame retains its width cap. Native
horizontal alignment positions a narrower frame within its parent.

The shared `PageFrameOverridesFixture` checks these boundaries at 16, 24 and
32 DIP fonts, two host widths, both themes and both flow directions. It also
checks an 11 DIP child override, restoration to a live 40 DIP parent font,
1,200 DIP caller content, left/center/right frame alignment and passive metadata.
The fixture's 128 checks run through
`--probe-hidden-page-frame-overrides --verify-output <report.json>` in either
the catalog or independent package consumer built from this source checkout.
This test entry point is newer than the October 25 preview catalog ZIP; do not
pass it to that older executable. The current fixture never shows or focuses its
window. Audit the report with `scripts/verify-page-frame-overrides.mjs`, passing
`--report`, `--app` and `--output`; add `--host-assembly PackageConsumer.dll` for
the independent consumer. `scripts/test-page-frame-overrides-audit.mjs` verifies
that incorrect fonts, clipping, replacement, sizing, alignment, missing cases,
foreground input and stale binaries are rejected. Version 2 adds 48 checks for
caller style precedence, restoration of live recipes after clearing a style,
replacement control templates, and style/local/bound automation metadata.
The auditor still supports historical version 1 reports with their original
80-check scope.

A caller style's padding, maximum width and inherited font survive a live
`FrameWidth` change. Clearing that style restores the current recipe. A complete
replacement control template receives the original content; restoring the
original template preserves that same loaded object. Explicit landmark type,
localized landmark description, item type and name take precedence over
`Element` defaults. A live name binding continues to follow its producer;
clearing local metadata reveals the style, then clearing the style restores the
appropriate component defaults. The localized landmark string follows the
[native metadata precedence contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties.localizedlandmarktype?view=windows-app-sdk-2.0).

These are component font overrides, not Windows text/display scaling. System
scaling, physical input and native ARM64 runtime remain separate requirements.
The added checks pass in the x64 catalog and an independent x64 consumer of the
published `0.1.0-menubar-peer-metadata-v1-local` package: 128 checks per host and
15 incorrect reports rejected per host. The consumer also builds for ARM64;
that is compile evidence only. The runs retain the published library's
`c0597e9f4cb9499cc09aafb59da2b321f6e302c7fe11eecf5d08a357a9110483`
SHA-256 and report 801 background-isolation guards per host, with zero focus or
activation events. Evidence is under
`artifacts/current/page-frame-additional-overrides-catalog-native-v1` and
`page-frame-additional-overrides-package-native-v1`. No production component or
theme code changes are needed for these passing boundaries.

The Page frames catalog entry uses fictional local input and an independent
consumer action. The source reference retains 792 layout states and 40 original
PNGs from the pinned source, covering every width/gutter/vertical-spacing
combination around both viewport breakpoints, empty content, fractional and
narrow hosts, semantic elements and explicit padding/size overrides.

Earlier x64 runtime evidence matches all 792 measured layouts with zero geometry
error and all 40 complete source screenshots with zero differing pixels. Twenty
corrupted reports/images are rejected. Both themes exercise real child editing,
pointer/Space actions, live viewport changes of an existing narrow frame,
binding/style/default restoration, templates/selectors, metadata precedence and
remounts. The flat reference filler has distinct logical and pixel-snapped paint
parts to represent the browser's fractional box and opaque background; PageFrame
leaves its caller's rendering intact. Raw exact-comparison failures are retained
for diagnosis without accepting them.

For that earlier preview, both catalog architectures built without warnings/errors. Its local package
passed a fresh independent x64 consumer in both themes and an ARM64 consumer
build, with both temporary source/package caches removed. The new dictionary
also passes the existing Field runtime and all 44 measured Field states.

The full 792-layout and 40-image suite was not rerun for the focused font/size
checks above. Its earlier results retain their original scope.

Broader platform acceptance remains open. Actual Windows display/text scaling,
native ARM64 execution and final visual/integration acceptance still need
current evidence. The caller override cases above do not establish those
platform outcomes. See
[runtime evidence](runtime-evidence.md) and the governing
[component-library scope](library-scope.md).
