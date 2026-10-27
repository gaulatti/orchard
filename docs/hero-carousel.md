# Hero Carousel native preview

On 2026-10-22 the user explicitly selected the source behavior: arrow keys throughout the owning window, plus Tab and click input on inactive slides. The `0.1.0-hero-carousel-input-v1-local` preview implements that choice. Source, fresh NuGet consumer and extracted catalog pass 82 states and 34 checks in each host, including owning-root subscriptions, direct outside-arrow processing, eleven native INPUT/TEXTAREA exclusions, contenteditable-equivalent RichEditBox, inactive link eligibility and original-item invocation, source empty/visible-content hit ordering and unload/remount cleanup. The later [v5 foreground input checkpoint](evidence/components-v5-hero-carousel-foreground-v1/checkpoint.json) verifies focused native paths in the extracted x64 catalog. No OS-global keyboard hook is used.

Both solution and isolated-consumer architectures build with no solution warnings; 434 portable and 10 Windows text tests pass. The source auditor rejects 88 corruptions and the package auditor rejects 85. Both fictional demos load in the source and extracted catalog. Native state/contract data match across all three hosts. The strict source/package image gate fails at 52 pixels, maximum channel difference 24, with 78/82 identical PNGs. Source/archive fails at 58 pixels, maximum 24, with 76/82 identical PNGs. The allowed difference remains zero. Full raster, broader physical input, scaling/motion/customization and native ARM64 acceptance remain open.

A [current independent NuGet-consumer foreground check](evidence/hero-carousel-current-package-foreground-v1/checkpoint.json)
now exercises the user-selected input behavior with the byte-matched current
package DLL. In explicit Light and Dark, physical Right/Left keys from a button
outside the carousel change its index, while Right in a native text editor does
not. Clicking the empty area over the visible first slide invokes the original
inactive third item; Tab and Enter also invoke the original second item while
the third is visible. The caller receives the original item's destination, and
the normal consumer preview still opens. This closes the focused independent
x64 package-input gap, not the complete focus/input matrix, full visual review,
scaling, motion, performance or native ARM64 runtime acceptance.

The earlier v5 effects preview adds the source's 2px CTA/tag/arrow backdrops and
inherited tag shadows. Native underlays keep the original styled border and
foreground separate; four-corner clipping follows the live border geometry.
The arrow template groups its underlay with pressed/disabled transforms, while
caller-authored replacement templates own their visuals. Tag and CTA glyph
shadows paint inside the content layer above the original background. Rendered
stripe/shadow on/off and cleanup checks pass in the hidden x64 source prototype:
82 states, 32 checks, 7,256 geometry values, 1,092 paint values and 222 exact
palette probes. Both themes produce visible changes at all six backdrop roles
(2,618 sampled pixels) and both active tag shadows (3,142 pixels). The auditor
rejects 72 corrupted source reports in the latest source run. The current
canonical Gaussian/black shadow path changes 1,698 tag pixels; the earlier
sealed experimental source checkpoint recorded 3,142. Live text/font changes,
RTL resizing, asymmetric corner clips, empty text and restored shadows also
pass rendered/lifecycle checks. The fresh `0.1.0-hero-carousel-v5-local` NuGet,
source catalog and extracted self-contained catalog pass the same 82 states/32
checks with identical library bytes. Both solution and isolated-consumer
architectures build; 426 portable and 10 Windows text tests pass. Both fictional
demos pass the shared-host checks in the source and extracted catalog.

The v5 zero-difference image gate still fails: 77/82 source/package PNG pairs
are identical, with 33 differing pixels and maximum channel difference 20.
The largest three differences lie on the CTA's right edge. Replaying the same
library/host bytes from the extracted catalog differs at 40 pixels, maximum 20,
with 75 identical pairs and equal state/contract data. These measurements do not
establish image equivalence or change the threshold. Complete component
acceptance remains false. The v4 results below remain historical.

The shadow-off capture sets the owned shadow sprites' opacity to zero and
verifies that it remains zero through layout. Detaching the container left the
captured images byte-identical in this runtime, while opacity suppression with
the same library bytes exposed the shadow pixels. Resource retirement remains
a separate assertion on removed tags, replacement templates and unload.
Glyph masks refresh when text, font metrics, layout size or DPI changes. Empty
or collapsed text releases its graph, and restoring text builds a fresh graph.
The original XAML alpha brush and surface remain XAML-owned; Orchard only
disposes its wrappers and effects.

`BleeckerHeroCarousel` is a reusable WinUI component with original caller items,
link rendering, image ownership and fictional catalog examples. The
[pinned source contract](hero-carousel-reference.md) remains the visual and
behavior reference. Both fictional Monitoring and Editorial demos remain in
scope. This preview does not add a router, image service or product workflow.

```xml
<orchard:BleeckerHeroCarousel x:Name="Featured" AutoAdvanceMilliseconds="5000" />
```

```csharp
var original = new BleeckerHeroCarouselItem(
    "/fictional/original?value=Original%20text",
    "Original fictional title", "Original caller description")
{
    Id = "original-key",
    Kind = BleeckerHeroCarouselItemKind.Project,
    Tags = new ObservableCollection<string> { "Original", "Local" },
    ImageSource = callerImageSource
};
Featured.Items = new BleeckerHeroCarouselItems { original };
Featured.ItemInvoked += (_, item) => callerRoute(item.Href, item.External);
```

Use the owning UI dispatcher. Observable item/tag collections and dependency
properties update automatically; `Refresh()` rereads ordinary mutable
collections. Original objects, literal destinations, external intent and
`ImageSource` instances remain intact. Default native links do not set
`NavigateUri`; `Command`/`CommandParameter` and `ItemInvoked` let the caller own
routing. They are guarded by loading, enabled/visible ancestors, the actual
retained element and current modal scope. Inactive slides keep input. A caller
exception propagates.

An omitted `Kind` uses project labels and CTA text without displaying tags;
tags require explicit Project kind. Empty CTA text uses the kind default.
Sibling keys must be unique: explicit ID, otherwise original title plus href.
Same-key edits/reorders keep the same native template/editor instance. Removed
keys, template replacement and unloading revoke old contexts and commands.
Remount starts at numeric index zero and restores working previous/next and
indicator controls. Slide cleanup removes only the owned slide containers;
arrow buttons share the native ContentControl base class and stay independent.

`LinkTemplate` receives `BleeckerHeroCarouselLinkContext`: `Item`, prepared
`Content`, `Style`, `AccessibleName`, `IsActive` and guarded `Command`. Bind
mutable context properties OneWay. The prepared native content can have one
parent, or the template can render its original item itself. `EmptyState`,
`EmptyStateTemplate` and `EmptyStateTemplateSelector` preserve caller content.
The default empty message is used only for null content without a caller
template. Source false/zero/empty content is not replaced with invented text.

The source event model preserves the numeric index through same-count reorder,
shrink, single and empty lists. A temporarily out-of-range index can leave all
slides hidden. `CurrentIndex`, `CurrentIndexChanged`, `MoveNext()`,
`MovePrevious()` and `SelectSlide(index)` expose native observation/navigation
while loaded and enabled. Previous/next wrap; a supplied index must be valid.
Manual navigation does not restart the interval. Hover pauses and mouse leave
starts a fresh interval. Item count, interval and hover changes retire the old
timer generation. Unloading stops timers and remount starts at zero.

Each loaded carousel subscribes to KeyDown on its owning XamlRoot content,
including handled events, and leaves the event unconsumed. All eligible mounted
carousels in that root receive arrows, even when focus is outside them. Native
text/password, checkbox/radio, slider/switch, file and OTP inputs retain their
keys, corresponding to the source INPUT/TEXTAREA exclusion. RichEditBox is the
contenteditable counterpart and does not suppress carousel arrows. Unloading
removes the root listener. Disabled, hidden, detached and modal-blocked owners
still reject input.

Inactive slides retain their enabled, hit-testable links and Tab eligibility;
the default link TabIndex follows the original item order. IsActive describes
presentation and must not be used as an input guard in a source-equivalent
LinkTemplate. Caller command denial still applies to every slide. The group's
automation children retain the source aria-hidden distinction, so only active
slide content is exposed there. Keyboard focus reveals navigation arrows.
The v5 foreground checkpoint verifies a pointer click on an inactive slide and
Tab/Enter activation of another inactive slide in the extracted x64 catalog.
Broader focus and visual acceptance still require runtime evidence. The original
browser histories and focus screenshots remain retained in the frozen source
reference.

The default renderer keeps transparent native hit targets separate from its
rendered opacity/effect layers. This preserves the source stacking rule:
settled visible content sits above inactive links, while empty link space hits
the last original sibling. These targets invoke the original context command,
stay out of Tab order and the group's automation children, and send pointer
focus to the original link. Custom LinkTemplate content keeps its own native
pointer targets and original controls. Hidden tests inspect hit ordering and
native Invoke payloads; the focused foreground checkpoint adds actual pointer
and Tab/Enter observations without proving every input state.
WinUI documents that [zero opacity retains hit testing](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.uielement.opacity?view=windows-app-sdk-2.0)
and that [the hit-test stack is returned in descending z-order](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.visualtreehelper.findelementsinhostcoordinates?view=windows-app-sdk-2.0).

Layout keeps the source's 600px/rem-equivalent height, 1280px/rem container,
responsive gutters, balanced headings, original complete text, kinds/tags and
CTA spacing. `FontSize=16` is the base rem equivalent; source pixel-only tag
font/radii and mixed pixel/rem values remain separate. Window viewport width
selects responsive breakpoints. Five public styles customize link, arrow,
indicator, tag and CTA decoration; `DefaultBackground` is the caller brush for
the no-image and empty paths. A replaced control/link template owns its visuals.

`FontFamily` supplied locally, by binding, or through the control's `Style`
reaches all prepared slide text and the plain empty message. Changing it live
retains the text elements; clearing it restores the separate regular, medium,
and semibold source faces. Role sizes and weights stay source-defined. Caller
templates and rich empty content continue to own their text presentation.
Style changes follow WinUI's effective `Template` identity: an actual template
replacement revokes old contexts. Retention through a font-only style change
requires keeping that template unchanged; assigning the existing `Template`
locally before applying/removing the style makes this boundary explicit.
`Foreground` retains the original caller brush for title, kicker, and tags,
including gradient brushes and live mutations. Hover does not replace that
brush. Clearing it restores the semantic theme colors and transitions;
description and CTA keep their separate semantic colors. The catalog exposes
secondary-font and token-gradient examples. Background regression coverage
checks binding replacement, style removal, face restoration, original brush
identity, and visible title-pixel changes in both themes.

A native `Image` retains the caller's live SVG/animated/mutable image source
with cover cropping. Composition applies black overlay, vertical/horizontal
gradients, soft-light tint, overlay grain and radial vignette. Image failures
raise `ImageFailed` with the original item and keep the image path; they do not
select alternate artwork or the no-image gradient. Static layer textures update
on size/DPI changes; live source pixels remain compositor-owned. Text shadows
and the indicator backdrop use native composition. The no-image gradient uses
the source GPU's ordered 8x8 dither before RGBA8 storage, including narrow-width
rounding boundaries. Full layer/glyph raster equivalence,
precision, performance, motion interruptions and background capture coverage
are still unverified.

The sealed v4 preview records zero-warning x64 and ARM64 solution builds,
426 passing portable tests and 10 passing Windows text tests. Its hidden
native matrix passes 82 states and 30
behavior checks across both themes, plus both actual catalog themes: 7,256
captured geometry values, 1,092 paint values and 222 exact PNG palette probes.
The tests preserve original items, commands, parameters, keyed editors, image
sources and failure payloads; they check inactive source input and group UIA exposure,
timer pause/unload cleanup, restored working remount navigation, empty content,
live themes, font binding/style restoration, retained editor content and live
original gradient/solid brush updates. The independent auditors reject 49
source and 46 package counterexamples. Six additional PNGs per host capture
caller font and gradient cases; gradient mutation changes 4,346 title pixels
by at least 64 channel levels in each theme. Forced visual states
are not physical input. The first failed image graph, z-index and source
comparison attempts remain retained.

Run `scripts/verify-hero-carousel.ps1 -AppDirectory <built-app> -Output <fresh-dir>`
and `node scripts/test-hero-carousel-audit.mjs --native <fresh-dir>
--host-assembly Orchard.Catalog.dll --output <fresh-negative-report>` for the
background native checks. They never show/activate/focus the native window.
The report hashes the actual executed library and host; no old package can
establish a result for newer source.

Still open: complete rendered image/glyph/gradient/shadow equality, normal
motion interruptions and timing extremes, CTA/tag/arrow backdrop coverage,
tag shadows, broader caller font/style/brush customization, active-modal
isolation, physical keyboard/pointer/focus and native ARM64 runtime. The
gradient's exact sampled colors do not establish all-pixel equivalence.
Both solution architectures and isolated NuGet consumers build. The fresh
`0.1.0-hero-carousel-v4-local` package passes the same 82 states/30 behavior
checks with the same v4 library bytes. Source and package reports match their
geometry, palette, data and lifecycle results. The source/package full-image
comparison still fails its zero-difference gate: 3,008 pixels differ by at most
two RGB levels; 36 of 82 default PNG pairs are identical. Replaying the same
v4 library/host from the extracted catalog archive differs at 1,349 pixels
by at most one level, with 47 identical pairs and matching state/contract data. All captures require
two consecutive identical rendered frames before saving. These observations
do not establish image equivalence or relax the gate. Windows text tests pass
10 checks; the v4 public-host regression retains both fictional demos.
The versioned self-contained x64 catalog archive has 1,346 independently
byte-verified files. Its extracted copy passes the same 82 states/30 checks,
both catalog themes and the shared-host regression with both fictional demos.
Complete component acceptance is false. The sealed source preparation and
previous Sonner package remain historical evidence. These v4 results do not
validate the newer effects draft described above.

Implementation references for the pinned Windows App SDK 2.5.1 servicing line:
[release mapping](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-channels),
[dispatcher timer](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueuetimer?view=windows-app-sdk-2.0),
[visual surface](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.compositionvisualsurface?view=windows-app-sdk-2.0),
[XAML alpha masks](https://learn.microsoft.com/en-us/windows/apps/develop/composition/using-the-visual-layer-with-xaml),
[chained effect brushes](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.compositioneffectbrush?view=windows-app-sdk-2.0),
[Win2D blend effects](https://microsoft.github.io/Win2D/WinUI3/html/T_Microsoft_Graphics_Canvas_Effects_BlendEffect.htm)
and [bitmap capture limits](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.imaging.rendertargetbitmap?view=windows-app-sdk-2.0).
The gradient follows [CSS color conversion](https://www.w3.org/TR/2026/CRD-css-color-4-20261020/#color-conversion-code)
and [Skia GPU dither](https://github.com/google/skia/blob/main/src/gpu/DitherUtils.cpp);
the frozen original PNGs remain the measured reference.
No Narrator, OS setting changes, pointer heartbeat, foreground input or GitHub
Actions is part of this work. Central standard/wiki access was unavailable;
checked-in managed rules and repository documentation were used.
