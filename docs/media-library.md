# Media Library source contract

Media Library is a reusable component in the fixed Windows inventory, within
the [component-library scope](library-scope.md). The pinned Bleecker source is
`src/components/media-library.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`eac6b2e0474d8970539f6f23fff22cc515df39fb496dd7bed8beeb646e420232`.
Source preparation is verified. The public Windows component and its fictional
catalog fixture now have a verified hidden preview. The x64 solution builds
without warnings, as does ARM64. Fresh NuGet consumers for both architectures
build successfully; the x64 consumer and extracted runnable catalog pass the
hidden preview. No full native acceptance is claimed.

The caller owns ordered items and their original IDs, filenames, optional alt
text, thumbnail/full URLs and creation-date values. The component owns only
the selected preview item. Its search term is controlled: editing and clearing
request a new value, and the caller decides whether to accept it or filter the
items. Actions and filters are arbitrary caller content slots. The component
does not supply paging, upload, storage, deletion or application services.

Selecting a tile retains that exact original item for its modal. Removing or
replacing a list entry does not replace or dismiss the selected object. A copy
callback receives the original filename or URL string. Without that callback,
the source calls the clipboard boundary. A delete callback receives the original
ID and does not mutate items or select a tile. In the original browser source,
the tile's Open button covers the Delete target: a pointer click opens the
preview, while keyboard activation requests deletion. The frozen reference
retains this measured source behavior.

The user explicitly corrected the Windows contract: **pointer Delete must
request deletion without opening the preview**. Keyboard Delete activation must
make the same request. Neither route mutates the caller's collection. Native
hit testing and callback/preview state must verify this correction independently
of the original browser pointer behavior.

The native API is `BleeckerMediaLibrary`, with ordered
`BleeckerMediaLibraryItem` objects and an optional observable
`BleeckerMediaLibraryItems` collection. IDs must be unique among siblings; empty
IDs and filenames retain their original values. `SearchTermChangeRequested`
and `SearchCommand` receive the requested string. The caller must set
`SearchTerm` to accept it and supplies any filtered `Items`. `DeleteRequested`
and `DeleteCommand` receive the original ID. They never mutate `Items`.
`CopyRequested` and `CopyCommand` receive the original filename or full URL;
without either, `ClipboardWriter` or the native Windows clipboard handles the
same string. An optional `UrlCommand` routes the original URL without opening a
browser. Otherwise the native hyperlink requires a supplied absolute URI.

`ThumbnailSource` and `ImageSource` are caller-owned WinUI `ImageSource` objects,
the native counterparts of the web image values. `Url` independently retains
the original full URL for display, copy and routing. The component never infers
an image path from the filename or URL and never substitutes a different decoder
after failure. Tiles prefer `ThumbnailSource`; preview uses only `ImageSource`.
For SVGs the caller owns rasterization dimensions, as described in the
[Windows SVG sizing contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.imaging.svgimagesource?view=windows-app-sdk-2.0).
The implementation was checked against the installed WinUI 2.3.9 metadata in
Windows App SDK 2.5.1. Raw creation-date strings remain caller-owned;
`CreatedAtFormatter` can supply application formatting. The default uses the
current process culture and local time zone, displaying `Invalid Date` when
parsing fails. No OS culture or time-zone setting is changed.

Place the library beneath the window's `BleeckerModalHost`. Actions and filters
use retained `Actions`/`Filter` content and optional data templates.
`TileStyle` targets `Border`; `PreviewSurfaceStyle` targets
`BleeckerOverlaySurface`. `FontSize=16` is the rem baseline; the source's fixed
10px kicker/metadata labels and 12px overlay filenames stay fixed when changing
that baseline. `OpenPreview` accepts an original current item, `SelectedItem`
exposes the retained object, and `ClosePreview` dismisses it. Unload clears
selection. Programmatic visual-state checks are distinct from physical input.

The tile uses a nonempty thumbnail URL before a nonempty full URL, with alt text
falling back to the filename. With neither image value it displays `No preview`.
An image decoding failure does not select another source. The preview uses only
the full URL, so a thumbnail-only item has an empty image frame in the modal.
When the full preview image fails, its original alt text appears with the same
small, non-interactive failure marker used by a failed tile thumbnail.
For a failed tile thumbnail, the original alt text remains visible with a small
image-failure marker at its start, matching the source's broken-image affordance.
At narrow widths, later lines of the alt text wrap back to the tile edge instead
of staying indented beneath the marker.
The marker uses Orchard's secondary text brush in each theme and adds no pointer
or accessibility stop. This is an intentional Windows rendering of the browser's
default broken-image icon rather than a replacement image or URL fallback.
Alt text, URL and creation-date sections follow the original truthiness checks;
an empty filename selects the modal heading `Media Preview`. Created dates use
the browser's local date formatting, including its visible invalid-date result.

The source layout uses two columns below 768px, four from 768px and five from
1024px of viewport width. Tiles are square, retain complete filenames in their
accessible open labels, and truncate the displayed hover filename. The hover
overlay, delete affordance, focus ring, preview sizing, responsive padding,
typography, colors, borders, shadows and motion come from the pinned tokens and
source styles. A narrow host in a wide viewport does not change that viewport
breakpoint contract.

Acceptance requires the original item and string values to reach the visible
grid, preview, copy/delete requests and caller slots; controlled search must
reach the caller and render the value that the caller supplies. Retained slots,
keyed tile updates, selection across collection changes, preview dismissal,
image replacement/failure, theme and scale changes, disabled native owners and
unload/remount must be verified at their runtime boundaries. The catalog must
use fictional local data and retain both required workspace demonstrations.

The frozen [reference](../tests/Reference/MediaLibrary.json), SHA-256
`11cbfaa6488702b925f04e498647c59dccce1a3229366c1ed530cd34f56257c5`,
retains 100 original browser PNGs, settled computed styles, hit-test order,
accessibility snapshots and 50 interaction histories in both themes. A fresh
independent capture matches every canonical PNG byte for byte, all settled
metadata/accessibility snapshots and all histories, with no semantic fields
excluded. The source auditor checks 12 pinned dependencies, 122 CSS scan inputs,
1,699 actual bundle inputs, six original fonts, seven local fictional image
assets, 6,510 geometry values, 7,310 paint values and 400 actual PNG probes.
All 202 raw capture samples are retained; the final two samples of each case
are identical. Its negative suite rejects 80 deliberate corruptions. Eight
source-backed regressions bring the portable suite to 442 passing tests.

The accepted capture is `artifacts/current/media-library-source-v6`, independently
repeated in `media-library-source-v7`. Earlier attempts are diagnostics. In
particular, v4/v5 RTL captures exposed an offscreen harness-sentinel overflow
that produced blank screenshots despite correct DOM bounds. The pixel auditor
rejected them; removing those unused harness sentinels fixed capture without
altering the source component or CSS. No diagnostic image was promoted.

Browser image requests use explicit local fixture fulfillment, with no external
network requests. Clipboard calls are explicitly intercepted; the system
clipboard is unchanged. These are source references, not native runtime or
physical-input evidence. Complete native/visual acceptance remains required.
Desktop work, Narrator,
OS settings, pointer heartbeat and
GitHub Actions remain unauthorized.

The current native source preview is
`artifacts/current/media-library-native-source-v6`: 100 states and 38 behavior
checks pass in the hidden x64 host, with both actual catalog themes. Its
independent audit checks 4,750 geometry values, 1,268 paint values, 6,580
original-value assertions and 102 retained PNGs. It also checks centered cover
geometry and actual SVG-circle landmarks, catching the earlier top-left crop.
The negative suite rejects 83 corruptions, including zero-width Open targets,
incorrect crop placement, covered Delete targets, collection mutation and
rejected-search persistence. The source preview DLL is SHA-256
`9ab629cd12c8402390771b6fa492d34a56e29ca95f54fc6f931905aba952cd87`.

Native source checks verify controlled search rejection/acceptance/clear,
pointer Delete hit ordering followed by software invocation, original deletion
IDs and copy strings, an intercepted clipboard boundary, original URL-command
routing, selection across removal/replacement/mutation, thumbnail-only empty
previews, close-layer removal, retained caller editors and keyed tiles, command
enablement and unload/remount. There is no physical click or key injection.
Focus, activation and visible-window guards remain zero; the OS clipboard and
external browser are unchanged. The 442 portable and 10 Windows text tests pass.
Both required fictional demos pass the current shared-host regression: 30 cases
and 16 catalog-root states.

Strict browser/native image equality **fails**: 13,349,376 differing pixels,
maximum channel difference 242, zero allowed and zero byte-identical PNG pairs
out of 100. The earlier top-left crop result differed at 15,247,257 pixels and
was superseded by the centered source. Full raster, modal/text/display scaling,
motion/customization/performance, physical input/focus/Tab and native ARM64
runtime acceptance remain open. No inventory entry is accepted by these checks.

Delivery preview `0.1.0-media-library-v1-local` is independently verified:

- NuGet `artifacts/current/packages/Gaulatti.Orchard.0.1.0-media-library-v1-local.nupkg`,
  SHA-256 `9c3b896adfd76b4bcbfc28fe373f71cb3cc8a9dde6c31da22417064db5e371ac`.
  The AnyCPU library, PRI, 154 theme files, six fonts, two images and current
  README match their checked sources.
- Both full solution builds and fresh isolated NuGet-consumer builds pass.
  x64 runtime repeats all 100 states/38 checks and rejects 83 corruptions.
  ARM64 is build evidence only. Temporary consumer roots were removed, retaining
  the shared dependency cache.
- The self-contained catalog ZIP is created and extracted into
  `artifacts/current/media-library-catalog-expanded-v1`; all 1,356 files are
  byte-verified. The extracted archive repeats the 100-state/38-check matrix and
  the shared-host regression for both required fictional demos.

State and contract data are identical across source, fresh package and archive.
Strict image equality remains failed: source/package differs at 9,171 pixels
(maximum channel 102; zero identical PNG pairs), source/archive at 10,366 pixels
(maximum 102; 11 identical pairs). Both permit zero difference. These previews
verify delivery and behavior without claiming complete visual/input acceptance.

The subsequent failed-thumbnail repair uses the existing fictional broken-image
catalog case. `artifacts/current/media-library-broken-image-v2` passes 100 hidden
x64 states, 38 behavior checks and 102 rendered PNG audits; its light and dark
default captures were inspected. The audit checks the 16px marker beside the
original alt text, top-edge placement, non-hit-testable presentation and visible
marker ink. An initial capture with the marker centered vertically is retained
as a failed visual diagnostic; the corrected capture places it beside the text.
The negative audit rejects 85 altered reports and images, including a hidden or
erased marker. Its successful scratch copy is removed after the audit. Both x64
and ARM64 catalog builds pass without warnings. This is source-host evidence;
fresh NuGet delivery, physical input, full visual comparison and ARM64 runtime
remain open, so the component is not accepted.

The following narrow-width repair places the marker and alt text in one rich
paragraph so wrapped lines return to the tile edge. The 319px light and dark
captures are checked for text ink at that edge, in addition to marker placement.
`artifacts/current/media-library-inline-failure-v4` passes 100 hidden x64
states, 38 behavior checks and 102 rendered PNGs. The 319px light capture was
compared with the pinned source capture. This evidence is for the source host;
the package boundary is qualified separately below.

Local delivery preview `0.1.0-media-library-v2-local` contains both failed-image
repairs (NuGet SHA-256
`1daf2dcf4ee6a8b6139aa96b06ee56100aa89ca84957607adc381a64c646dac0`).
Fresh isolated x64 and ARM64 consumers restored that exact package and built;
both temporary restore workspaces were removed on success. The packaged x64
host passes 100 hidden Media Library states, 38 behavior checks and 100 actual
PNG audits, including narrow failed-alt wrapping. Its state snapshots and
behavior results match the source host. ARM64 has build evidence only; native
ARM64 runtime, physical pointer/keyboard input and the remaining full visual
review are still open. Media Library is not yet an accepted inventory entry.

The later source-only full-preview repair adds that marker to the broken-full
case in both themes. `artifacts/current/media-library-preview-failure-v1` passes
100 hidden x64 states, 38 behavior checks and 102 rendered PNG audits. Its light
and dark broken-full captures were compared with the pinned source, and the
negative audit rejects 87 corruptions, including a hidden or erased preview
marker. The x64 and ARM64 catalog builds pass without warnings; 498 portable
tests pass. At that checkpoint the v2 NuGet predates this repair, so fresh
package and catalog delivery, physical input, full visual review and ARM64
native runtime remain open. The component is still not accepted.

Local package `0.1.0-text-media-v3-local` (SHA-256
`96f6000fb4d97fa6c7e362c6d3788781c4768f5d0825043ecd750c24b3ee8954`)
contains the full-preview marker repair and the later Input/Textarea validity
changes. Its payload has one AnyCPU library, one PRI, all 157 tracked XAML
files (156 themes), six fonts and two images; every packaged XAML file matches
its source hash. Fresh isolated x64 and ARM64 consumers restored and built that
exact package, then removed their temporary restore workspaces. The packaged
x64 Media Library host passes 100 hidden states, 38 behavior contracts and 100
PNG audits. The audit checks the broken-full marker's placement and rendered
ink in both themes. The packaged Orchard DLL SHA-256
`13ab2e380421a2fb0850f1ffe2bcdcb157b1952d4d3175812859c6ed730f0f9e`
matches the source host tested for text-editor validity. This is current NuGet
delivery evidence; at that checkpoint the catalog archive and native ARM64
runtime remained open.

Versioned local catalog archives now contain the same source repair for x64
(`47871b84cde5471b7fdb4eabb0c1e6336e44e42a7d401e6c4680309c8c198069`)
and ARM64 (`4822bde19c7c21d48f0408861f732c5bbe6e5962def9b1a66c2766ab52985d22`).
All 664 x64 and 660 ARM64 files match their extracted ZIP entries. The extracted
x64 catalog passes 100 Media Library states, 38 contracts, both catalog themes
and 102 PNG audits, including the broken-full marker. Its Orchard DLL has a
different build hash from the NuGet DLL, so these are separate runtime results.
Physical input, full visual acceptance and native ARM64 runtime remain open.
