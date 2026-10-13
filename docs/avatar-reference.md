# Pinned Avatar contract

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
Narrator and real contrast-theme behavior still require verification.

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
catalog and clean package-consumer checks for this addition are pending.
The first native run at `2571db7` fails startup on both architectures because
the fixture used an undeclared label style. The fixture now uses the established
body style, and the catalog resource-reference test rejects unknown Bleecker keys.
Subsequent native startup reports show the collection's strict type check rejecting
XAML assignment. The declared content-property syntax and binding replacement
check require a fresh native run before their behavior can be accepted.
Exact native/browser raster parity, contrast/Narrator, display/text scaling,
constrained widths and consumer style/geometry overrides remain acceptance gaps.
Neither a successful build nor nonblank screenshots establishes visual acceptance.
