# Pagination

The `.55` x64 preview accepts Pagination under the five-check milestone. The
extracted catalog passed 66 Light/Dark states and four physical input sequences;
its 64 reduced-motion presentation cases passed with animations disabled. The
independent package consumer compiled and ran 24 native 150% cases across both
themes, two widths, three page positions and RTL/LTR without clipped actions.
ARM64 built and its consumer compiled; ARM64 runtime is unverified. Earlier
pixel and normal-motion diagnostics below are historical and are not gates for
this milestone.

The fixed inventory's Pagination entry follows Bleecker commit
`a86059013cd59684ce452849278ea81e734f2066`, `src/components/pagination.tsx`
(SHA-256 `9111a975e78d31dcd8e3c68b588110547b45ee31facad43b81f84bb9e7318834`).

The component must request page changes while retaining the caller's original
page value and binding. Previous/Next availability comes from the two supplied
booleans, even if those disagree with the page/count values. Requests may be
declined. Programmatic prop updates must not emit a request.

Counts of one or fewer render no navigation. Otherwise up to seven page buttons
appear: the first seven near the start, the last seven near the end, or a window
around the current page. Fractional numeric values keep the source arithmetic;
the component must not introduce page clamping or data loading. Each action uses
native Windows button keyboard, pointer and focus behavior. Both themes,
consumer names, constrained widths and lifecycle/binding retention require
runtime evidence before acceptance.

The public control is `BleeckerPagination`. Its `double` dependency properties
are `CurrentPage` (initially 1) and `TotalPages` (initially 0); `HasPrevPage` and
`HasNextPage` initially disable both arrows. Set all four properties from the
consumer's current state. `PageRequested` supplies `BleeckerPageRequestedEventArgs.Page`;
the control never assigns `CurrentPage` in response to input.

```xml
<orchard:BleeckerPagination CurrentPage="{Binding Page, Mode=OneWay}"
 TotalPages="{Binding PageCount}" HasPrevPage="{Binding CanPrevious}"
 HasNextPage="{Binding CanNext}" PageRequested="RequestPage"/>
```

The consumer's handler may accept the request by updating its own `Page`, or
decline it. Page buttons are retained by numeric value across window changes;
the native automation tree and Tab order follow the displayed order. Native
button peers reject disabled/detached actions. Empty navigation removes its
default top spacing. `AutomationProperties.Name`, `IsEnabled`, `FlowDirection`,
ordinary XAML sizing and a template containing `PART_Layout` remain native
customization boundaries. The default name is Pagination.

The registered `PaginationsFixture.xaml` demonstrates accepted/declined changes,
first/last pages, disabled and single-page states, RTL and a constrained width.
The collector records 66 original states and complete PNGs plus real click,
Space and Enter requests. Five source-bound portable tests cover page windows,
fractional/outside values, numeric edge cases and flex shrink/centering. Native
input checks cover binding retention, retained focused buttons, ignored
programmatic changes, empty space, disabled/detached peers, remount and template
replacement. The first native capture exposed a half-layout-unit centering
error on fractional labels; the corrected row snaps its initial allocation to
the source's 1/64-DIP layout grid.

```powershell
./scripts/smoke-catalog.ps1 -Component pagination -ClientSize 700,160 -Output artifacts/component-pagination -ExpectedArchitecture X64
node scripts/verify-pagination.mjs --native artifacts/component-pagination --app artifacts/catalog --source tests/Reference/Pagination --output artifacts/component-pagination/audit.json
node scripts/test-pagination-audit.mjs --native artifacts/component-pagination --app artifacts/catalog --source tests/Reference/Pagination --output artifacts/component-pagination-negative
```

The scripts retain complete physical client captures without masks, pixel
tolerance or resampling; source pixels are used only for comparison. Glyphs use
the bundled medium Encode Sans font and the existing shared text-layout bridge.
The source chevrons, colors, seven-pixel radius and pressed scale are preserved
through private native buttons and centrally defined palette resources.

The current native receipt is `artifacts/current/pagination-motion-native-v2/audit.json`:
66 zero-error layouts, four physical-input sequences and complete raw PNGs.
All 32 damaged foreground receipts are rejected, including an actual old PNG
whose hash and mismatch receipt are internally consistent. Six empty-state
images match exactly; the other 60 retain 79,424 differing pixels. On the
original 58-state matrix, differences fall from 91,442 to 67,532. Both catalog
builds and 274 portable tests pass.

The native button palettes use `OuterBorderEdge`, matching the source's fill
beneath transparent borders. The audit requires 260 actual composed border
pixels to match the source exactly. This fixes the background gap around Dark
inactive buttons and the backdrop under translucent arrow borders. Dark arrow
hover uses the source theme's `#1a2332` sand token, independently of its normal
white border. The added arrow hover/pressed fixtures verify those states.
Paint assertions read the actual text/path foreground and native presenter
background/border. Page text binds to that presenter so native hover states
update the visible glyph color.

The local `0.1.0-pagination-v4-local` NuGet package builds fresh independent x64
and ARM64 consumers. Its x64 consumer passes 52 hidden source layouts and 18
ownership/lifecycle checks, guarded 436 times against activation/foreground
ownership. Its 392 buttons and their template presenters retain outer-border
fill. All 23 damaged hidden receipts are rejected. These checks also cover
caller metadata and stale peers after template replacement; they do not claim
visible input or raster evidence. Both temporary source/package caches are
removed after publishing the consumer outputs.

`PaginationMotion.json` captures 64 source transitions and 384 timeline samples
in both themes and motion preferences. Colors and opacity use 190ms premium
easing; pressed scale changes immediately because the source transitions
`transform`, while Tailwind sets individual `scale`. Reduced motion uses the
source's 0.01ms duration. Run `node scripts/verify-pagination-motion-reference.mjs`
to check provenance and timing without changing OS settings.

The production control now owns separate color and opacity clocks. Its private
button template retains native input and focus while avoiding stock transition
timing. The clocks use the source's encoded-sRGB or premultiplied-Oklab mixing,
retarget from their presented value, and snap when Windows animations are
disabled. Pressed scale stays immediate. Paint callbacks and brush/palette
observers disconnect on removal, retirement, unload and template replacement.

Three motion regressions bring the portable suite to 274 tests. The focused
catalog fixture derives targets from actual native input and properties, then
replays the production paint method at controlled times. All 352 sampled native
presentations match source RGB, source alpha serialization, opacity and scale
across 64 transitions; 64 detached scopes release their paint subscriptions.
Four representative raw PNGs are retained. All 28 damaged motion receipts are
rejected. This controlled-time diagnostic does not establish real-time normal
animation: Windows reports animations disabled on this machine, and no OS setting
was changed.

```powershell
./scripts/smoke-catalog.ps1 -Component pagination-motion -ClientSize 700,160 -Output artifacts/pagination-motion -ExpectedArchitecture X64
node scripts/verify-pagination-motion.mjs --native artifacts/pagination-motion --app artifacts/catalog --output artifacts/pagination-motion/audit.json
node scripts/test-pagination-motion-audit.mjs --native artifacts/pagination-motion --app artifacts/catalog --output artifacts/pagination-motion-negative
```

The older `artifacts/current/pagination-motion-checkpoint.json` describes the
pre-milestone draft and is not the current acceptance record. The `.55` release
and machine-readable inventory record the five current x64 checks. The public
wiki is unavailable; this contract is maintained in the repository.
