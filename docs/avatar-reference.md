# Pinned Avatar contract

For the 2026-10-29 x64 milestone, Avatar and AvatarGroup pass the five checks in
[the acceptance contract](acceptance-contract.md). The packaged x64 hidden
fixture passed 280 Light/Dark sizing and content states plus 32 group/lifecycle
contracts, with zero focus or activation. The catalog presentation route and
independent x64 package consumer passed. Four native 150% Light/Dark narrow/wide
cases kept initials, empty silhouettes and five group circles within bounds.
These controls are passive and define no animation. ARM64 remains build-only and
runtime-unverified; raster/pixel parity is outside this milestone. Historical
preview language below does not add acceptance gates.

Producer: `src/components/avatar.tsx` at Bleecker
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`e5cd2428aeddba94d2fca122ab21edf6e8e6238223c529cd2529266248fa2eb7`.
Its dependency is Tailwind CSS 4.2.2. Avatar and AvatarGroup remain previews;
their full Windows acceptance is open.

## Appearance and source behavior

| Size | Edge | Initials font / line height | Overflow font / line height |
| --- | --- | --- | --- |
| xs | 24 | 10 / 15 | 10 / 15 |
| sm | 32 | 12 / 16 | 12 / 16 |
| md | 40 | 14 / 20 | 12 / 16 |
| lg | 48 | 16 / 24 | 14 / 20 |
| xl | 64 | 18 / 28 | 16 / 24 |

Dimensions are CSS pixels, mapped to WinUI logical pixels. Encode Sans Medium
(500) centers the initials in a circular surface. Light uses lightSand
`#F9F6F2` and textSecondary `#595959`; Dark uses the rendered deepSea
`#182533` and textPrimary `#F0F4F8`. The base ring is inset 1px, black at 10%
in Light and white at 10% in Dark. The empty fallback uses the exact filled
person SVG at 60% of both dimensions. Neither avatar nor overflow adds motion,
interaction styling, focus stops or disabled dimming.

Images fill the circle with centered cover cropping.
The inset ring paints beneath child content, following
[CSS shadow layering](https://www.w3.org/TR/css-backgrounds-3/#shadow-layers);
an opaque image covers that ring instead of acquiring a tinted outline.
Image alt text is
`alt ?? fallback ?? ''`; explicitly empty alt remains decorative. Failed images
show the initials or silhouette. The producer latches image failure for the
component's lifetime: replacing the source does not retry, while remounting
a new component with a valid source loads it. Consumer values remain unchanged.

Initials use the first UTF-16 unit of the first two whitespace-delimited words,
then Unicode full uppercase. The native implementation shares Orchard's
documented Unicode casing data. Examples include `Jane Doe` → `JD`,
`ßeta ßeta` → `SSSS`, and `東京 港` → `東港`. A word beginning with an emoji
contributes its leading surrogate, as JavaScript `word[0]` does; the fixture
checks the code units as well as retaining the rendered example. Empty and
whitespace-only values show the silhouette.

AvatarGroup defaults to Medium and `max=4`. Each adjacent child overlaps 8px.
The group forces its size onto each avatar and replaces the base ring with
an opaque inset 2px white/DeepSea ring. The separate overflow circle retains
an exterior 2px ring and Tailwind's small black 10% shadows: (0,1,3,0) and
(0,1,2,-1), expressed as x/y/blur/spread. Overflow is `avatars.length - max`;
visible items use `avatars.slice(0,max)`, including negative maxima. Six items
with Max 4/0/-2/9 produce 4 avatars plus `+2`, only `+6`, 4 avatars plus `+8`,
and all 6 avatars respectively. An empty group has zero content height.
React's index keys retain each component's identity and failure latch at its
position when the consumer changes or reorders the collection.
The retained overflow circle also updates in place while overflow remains
positive. Routine data/count changes do not detach it or clear its elevation.

## Native API and integration

```xml
<orchard:BleeckerAvatar AvatarSize="Medium" Fallback="Jane Doe" />
<orchard:BleeckerAvatarGroup Max="4" AvatarSize="Small">
    <orchard:BleeckerAvatarData Fallback="Alice Harbor" />
    <orchard:BleeckerAvatarData Fallback="Bob Reed" />
</orchard:BleeckerAvatarGroup>
```

Use `xmlns:orchard="using:Orchard"`. `BleeckerAvatarSize` maps xs/sm/md/lg/xl
to ExtraSmall/Small/Medium/Large/ExtraLarge. `Source` accepts a consumer-owned
WinUI `ImageSource`; Orchard has no network client or image URL policy. `Fallback`
and `Alt` are nullable strings. Group `Avatars` is a non-null consumer-owned
`ObservableCollection<BleeckerAvatarData>`; each data item exposes the same
Source/Fallback/Alt properties. Collection, item and binding changes update the
visible composition. Max preserves its supplied integer value. The group's
default collection is independently owned by that instance.
AvatarGroup declares Avatars as its XAML content property, following the existing
notification collection pattern. Populate it with direct AvatarData children or
bind a consumer-owned observable collection.
An ElementName binding may be unresolved while XAML builds its namescope. The
control permits that null placeholder only for an unmounted bound target;
the next dispatcher turn after load still requires a non-null collection, after
WinUI completes binding evaluation. A programmatic null assignment
is rejected before replacing consumer data. No empty collection substitutes for
an unresolved or invalid binding.

The default templates consume centrally generated theme brushes, bundled fonts
and the pinned SVG geometry. Rounded content clipping uses the Windows App SDK
2.0 family's [RectangleClip](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.composition.rectangleclip?view=windows-app-sdk-2.0).
High contrast maps surfaces to system Window, text/rings to WindowText, and
removes overflow shadows. Named images use the UIA Image role; initials and
overflow expose text content; silhouettes and explicit empty-alt images are
decorative. Consumers can supply an explicit AutomationProperties.Name.
Component automation metadata and theme resources remain in scope. Narrator and
OS contrast campaigns are excluded by the [current library scope](library-scope.md).

## Verification and remaining acceptance

The actual pinned source was rendered in both appearances without component
restyling. Local reference evidence is retained in
`artifacts/avatar-reference-{Light,Dark}.json` and `.jpg`, plus
`artifacts/avatar-source-error-contract.json`. The source harness is
`artifacts/font-audit/avatar.html`; its image and names are fictional.
The public wiki remote is unavailable, so this contract lives in the repository.

The registered Avatars catalog page covers every export, all five sizes,
silhouettes, consumer images, image failure, Unicode initials, all group sizes,
overflow, zero/negative Max and empty collections. Native checks exercise live
size/item/collection changes, index identity, detached template replacement,
UIA names/roles, exact geometry and branded brushes, actual fallback ink, circular clipping, both
overflow shadow layers, centered image pixels and transparent corners. It retains
ten element images, two composited window images and
`avatar-measurements-{Light,Dark}.json`. The clean package consumer instantiates
Avatar, AvatarGroup and AvatarData, including the expanding-uppercase example.

The initial local x64 solution and isolated package-consumer builds pass with
no warnings; all 26 contract tests and token drift checks pass. Native x64/ARM64
catalog and clean package-consumer checks pass at `2f265e3` in both themes.
Each architecture/theme records ten size/ink samples, 40 verified image/corner
pixels, ten element images and two actual window composites. Collection replacement,
item/reorder/count changes, retained overflow elevation and detached retemplating
pass. See [checked runtime evidence](runtime-evidence.md).
The fixture also records initials' text-box width/height, line height and origin.
At `953e847`, source/native JD advances differ by at most .015499 logical pixels
in all five sizes; line-box heights match exactly. Both architectures and themes
agree. That run passes Avatar, then fails the earlier Input cursor phase and
does not reach the independent consumer; the complete baseline remains `2f265e3`.
The catalog resource-reference test rejects unknown Bleecker keys. Native checks
also verify collection resolution after load, rejected direct null input, shared
binding updates and retained overflow identity/elevation. Earlier startup and
update failures are recorded in the runtime evidence page.
Complete native/browser visual review, display/text scaling, constrained widths
and remaining image/customization acceptance stay open. Caller sizing and style
precedence qualification is recorded below.
Neither a successful build nor nonblank screenshots establishes visual acceptance.

## Caller sizing

`Width`, `Height` and `FontSize` remain caller-owned. Local values and bindings
survive template application, `AvatarSize` changes and remounting, including a
caller value equal to the current size default. Explicit styles, `BasedOn`
setters and the nearest implicit avatar style take precedence over the size
recipe. Clearing an override restores the applicable style or current size
default. The empty silhouette's viewport uses 60% of each actual dimension;
custom non-square avatars retain native uniform SVG scaling within that viewport.

The catalog includes bound caller dimensions/font size and a non-square empty
avatar. `--probe-hidden-avatar-sizing` qualifies five default sizes, bound/local
and styled overrides, updates and clearing, templates/remounts, and retained
default/styled group positions. It runs without desktop input. OS accessibility
campaigns follow the exclusions in [library scope](library-scope.md).

The current source, exact NuGet consumer and catalog archive hosts each pass
280 sizing states and 32 caller/group lifecycle checks in Light/Dark and LTR/RTL.
Each run records 1,757 isolation guards with zero focus or activation. The ten
Light/LTR default initials/silhouette images across all five sizes are unchanged.
These checks do not establish complete image, visual or platform acceptance.

Caller automation-name overrides, including empty values and styles, follow
[the shared metadata contract](automation-metadata.md).
