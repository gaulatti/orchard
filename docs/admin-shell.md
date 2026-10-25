# AdminShell preview

`BleeckerAdminShell` composes original caller-owned header, sidebar, body and
footer content. It is a reusable layout control with no application services or
administration/product behavior. The catalog's **Admin shells** fixture supplies
fictional local content and editable controls.

```xml
<orchard:BleeckerAdminShell Height="600" MinHeight="0">
 <orchard:BleeckerAdminShell.Header>
  <TextBlock Text="Caller header" Height="48"/>
 </orchard:BleeckerAdminShell.Header>
 <orchard:BleeckerAdminShell.Sidebar>
  <StackPanel Width="160"><TextBlock Text="Caller sidebar"/></StackPanel>
 </orchard:BleeckerAdminShell.Sidebar>
 <orchard:BleeckerInput Text="Original editable content"/>
 <orchard:BleeckerAdminShell.Footer>
  <TextBlock Text="Caller footer inside the main scroll region"/>
 </orchard:BleeckerAdminShell.Footer>
</orchard:BleeckerAdminShell>
```

The header spans the outer width. The sidebar occupies its own intrinsic width
beside a native scrolling Main region. The body fills free height above the
footer. The footer belongs to that same scrolling region; scrolling it does not
move the header or sidebar. Without an explicit height, the shell grows for its
content and has a viewport-height minimum. Use an explicit height and a suitable
`MinHeight` to constrain the main region. The default minimum follows the actual
XAML viewport; caller local values, bindings and inherited Style setters take
precedence. Clearing a caller minimum restores the viewport recipe.
The private viewport state is suppressed while a caller local value, binding or
inherited Style owns the minimum. WinUI does not notify when a local zero is
cleared to its zero metadata default, so a loaded zero override observes that
specific boundary on a 16-ms dispatcher tick. Observation stops on unload,
template removal, or when the override no longer needs it. Caller bindings and
their producer values are never rewritten.

Main-body padding uses **viewport width**, independent of shell width: 20×24 DIPs
below 640, 32×32 at 640–1023, and 40×40 from 1024. Footer padding uses the same
horizontal values and 20 DIPs vertically, with a one-DIP themed top border.
`AdminShellFooterBorderBrush` retains the source's exact 8% brush opacity.
The footer's logical wrapper keeps fractional bounds. A separate native border
inside its template rounds only the decoration; the original caller footer is
arranged separately and retains its own rounding values and bindings. Native
template bindings carry the themed border brush and thickness to that border.
Its paint translation accounts for the native scrolling parent's separately
rounded origin. Layout and scroll changes update only the decoration, keeping
the logical footer rectangle and original caller content intact. The scroll
observer disconnects on template release.
Setting `ContentPadding`, including an explicit zero or binding, overrides body
padding. Clearing it restores responsive spacing. `ContentBackground` changes
the main surface; ordinary Control properties style the outer shell.
The main surface is painted by the native scrolling viewport through an initial
one-way binding to `ContentBackground`. Caller replacements of
`Viewport.Background`, including bindings, are retained across owner updates.
Template release clears only the component-owned background binding.

An outer main-region clip bounds the native viewport's content. Its painted
edges align to device pixels after layout while logical wrapper/content bounds
remain fractional. This avoids the extra fringe produced by the native inner
clip at fractional coordinates. Native scrolling and original caller controls
remain in that same viewport. Clip observation stops on unload and template
release; caller scaling/rotation retains a local clip in its compositing space.

`Content`/`ContentTemplate`/`ContentTemplateSelector` hold the main body. Header,
Sidebar and Footer each have matching value, template and template-selector
properties. Original models reach those templates; original UI controls and
their input state survive property, width and viewport changes. Unsupported
model objects require a template instead of being silently converted to text.
`Viewport` exposes the current native main `ScrollViewer`, or null before
template application and after template removal.

The main region supports both scroll axes. Body and footer wrappers retain the
viewport width while explicitly wide native content or template roots expand
the scrollable extent. Auto-width roots remain constrained for native wrapping.
`Width`, `MinWidth`, `MaxWidth` and `Margin` changes on the original root update
that extent without rewriting caller properties or bindings. Clearing a width
can shrink the extent and clamp the scroll offset. Those root subscriptions
disconnect on unload, content/template replacement and template removal.
Native child panels continue to govern their own nested layout and clipping.
The catalog's **Toggle wide caller content** action exposes a 1,200-DIP root and
a far-edge action for keyboard/pointer checks.
On the current v20 package, that action remains fully visible after Tab scrolls
it into view and responds to Enter and pointer clicks. The original footer
action responds to Space and pointer clicks. Wheel scrolling moves only the
main region; edited input and action results survive width reset, Light/Dark
theme changes and maximize/restore. Ordinary wrapped content has no empty
horizontal range. Twelve window captures are recorded in
`artifacts/current/admin-shell-footer-visible-v20`.

The pinned source conditionally wraps a truthy footer in padding and a border.
Untemplated zero retains the source's visible `0` as an unwrapped text fragment;
empty strings and false/null render nothing. Native templates own their model
presentation, including Boolean and numeric values. Scalar text participates in
the native content tree; the Main landmark is passive and original interactive
controls retain their own semantics. Caller names and metadata are preserved.

The source is Bleecker `src/layout/admin-shell.tsx` at commit
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`535f760672696f04aa108eb79bf537055358641f121ae24dde0a0c98cb29c864`.
`tests/Reference/AdminShell.json` binds the frozen collector/client, exact source
dependencies, fonts, 312 initial states, 16 horizontally scrolled states, 88
complete source screenshots and two
real input/action continuity sequences. The hidden independent-package gate
compares actual native client viewports, geometry, paint, scroll extents,
semantics, bindings, templates and retained original controls in both themes.
It does not claim native pixels or physical input verification.

The foreground `admin-shell` capture records all 312 initial states, 16 scrolled
states and 88 complete native client PNGs. It captures the actual catalog window
and compares every pixel without masks, tolerance or resampling. Scrolled images
wait three seconds for the pinned WinUI template's native two-second indicator
contract and fade. Immediate captures remain preserved separately. The audit
binds the reference, library and catalog hashes and reports exact raster parity
separately from geometry success; a valid receipt is not component acceptance.

```powershell
./scripts/smoke-catalog.ps1 -Component admin-shell -ClientSize 900,640 -Output artifacts/admin-shell -ExpectedArchitecture X64
node scripts/verify-admin-shell.mjs --native artifacts/admin-shell --source tests/Reference/AdminShell --app artifacts/catalog --output artifacts/admin-shell/audit.json
node scripts/test-admin-shell-audit.mjs --native artifacts/admin-shell --source tests/Reference/AdminShell --app artifacts/catalog --output artifacts/admin-shell-audit-regression
```

Use `Arm64` on a native ARM64 host. The capture requires the existing 100%
display/text scale and does not change OS settings. Its negative receipt
checks include corrupted binary identities, missing states, geometry, scrolled
positions, raw PNG hashes and false raster or acceptance claims.

The current foreground receipt is
`artifacts/current/admin-shell-footer-native-v20/audit.json`: all 328 layouts
and their device-aligned clip bounds match. Both fractional surface frames now
match exactly, and the gate requires that result. In total, 74 of 88 complete
PNGs match exactly. The other 14 scalar-text frames retain 4,067 differing pixels,
reduced from 6,071 by the footer correction. Text antialiasing remains an open
acceptance gap. Earlier full receipts remain preserved separately.

The `admin-shell-diagnostic` mode uses fresh shells for sixteen focused probes in
each theme and saves 32 complete PNGs, actual layout/clip bounds, brush values
and composition traces. Its receipt is explicitly diagnostic, cannot establish
component acceptance, and is rejected by the full-matrix audit. It does not
change OS settings or paint reference pixels.

The separate `admin-shell-text-diagnostic` fixture compares the original header
text with seven standard Win2D profiles, 24 per-draw gamma/contrast profiles and
two font-coverage probes in each theme. All 68 complete client PNGs use the
actual bundled font and an explicitly opaque native test scene, preserving the
original component geometry. A catalog-only DirectWrite bridge creates the
custom parameters and reads them back from the actual drawing session; it does
not change OS settings. The coverage probes quantize freshly rendered glyph
coverage to RGB565 or BGR565 before blending the fixture's colors. Reference
pixels are used only for comparison. The diagnostic requires a clean native
process exit and verifies binary, font and PNG hashes. It does not replace
library text or establish support for arbitrary consumer backgrounds.

```powershell
./scripts/smoke-catalog.ps1 -Component admin-shell-text-diagnostic -ClientSize 900,640 -Output artifacts/admin-shell-text -ExpectedArchitecture X64
node scripts/verify-admin-shell-text-diagnostic.mjs --native artifacts/admin-shell-text --source tests/Reference/AdminShell --app artifacts/catalog --output artifacts/admin-shell-text/audit.json
node scripts/test-admin-shell-text-diagnostic-audit.mjs --native artifacts/admin-shell-text --source tests/Reference/AdminShell --app artifacts/catalog --output artifacts/admin-shell-text-negative
```

The current probe preserves the v20 library binary. None of the 68 frames
matches the source exactly. The RGB565 probe reduces the Light header's differing
pixels from 575 to 533, but its Dark result is worse than native text. All 38
damaged-receipt cases are rejected, including changed requested/applied rendering
parameters and coverage order. The current receipt is
`artifacts/current/admin-shell-text-coverage-native-v1/audit.json`, recorded in
`artifacts/current/admin-shell-text-parameters-checkpoint.json`. A production
text replacement has not been selected.

This is an in-progress preview. The current visible catalog replay verifies body
editing, Tab revealing offscreen content, original handlers through Enter,
Space and pointer clicks, wheel scrolling with a stationary header and
sidebar, and retained edits across theme changes and maximize/restore. Full focus
and interaction coverage, source-raster comparison, broader nested-content and
custom-renderer coverage, actual display/text scaling, and native ARM64 runtime
acceptance remain open. No OS accessibility settings or Narrator campaign is
part of this work. The GitHub wiki remote is unavailable, so usage and evidence
are maintained in the repository.

The fresh independent x64 package consumer passes all 312 initial and 16 scrolled
states with zero rectangle/clip error, 72 ownership checks and 2,680 isolation guards. These
include explicit-zero and equal-valued minimum resets, unchanged one-way and
two-way bindings/producers during resize, and stopped observation after unload.
The checks include wrapped content without empty horizontal scroll space, live
width bindings, minimum/maximum constraints, wide model templates and observer
cleanup, live main-background bindings, caller viewport-background overrides and
the caller's original footer rounding binding.
All 60 damaged hidden receipts and 39 damaged foreground receipts are rejected,
including scrolled geometry, offsets, main clipping and paint, and footer
color/opacity damage.
The 266 portable tests pass and both catalog architectures compile without
warnings. The rebuilt local v20 package contains the main and footer paint corrections.
Its current hidden evidence is under
`artifacts/current/admin-shell-footer-consumer-v20` and
`admin-shell-footer-hidden-negative-v20`. Six related package regressions and
both component-demo fixtures pass on those rebuilt binaries. Earlier failed reports
remain preserved; this passing preview does not imply complete component acceptance.
