# Header prototype

`BleeckerHeader` is a reusable WinUI component accepted for the five-check
x64 milestone in preview `.66`. Its current package and extracted catalog pass
Light/Dark states, physical pointer/keyboard/focus at mobile and desktop widths,
six independent native 150% layouts without clipping, static reduced-motion
menu presentation, and independent package-consumer compilation. The mobile
menu refreshes native command eligibility when toggled, so its visible links
accept Tab and direct focus. ARM64 is build-only and runtime-unverified.
Historical parity notes below do not add milestone gates.

It composes the
existing BrandLockup, NavMenu and IconButton with caller-owned desktop/mobile
actions. The caller owns routing. The [frozen source contract](header-reference.md)
records 72 Bleecker states and four original input histories. Both fictional-data
Monitoring and Editorial demonstrations remain required.

```xml
<orchard:BleeckerHeader ItemInvoked="Navigate" BrandInvoked="BrandRequested">
    <orchard:BleeckerHeader.Brand>
        <orchard:BleeckerHeaderBrand Href="#library" BrandName="Fictional library" />
    </orchard:BleeckerHeader.Brand>
    <orchard:BleeckerHeader.Actions>
        <TextBox Width="160" Text="Desktop caller input" />
    </orchard:BleeckerHeader.Actions>
    <orchard:BleeckerHeader.MobileActions>
        <TextBox Width="160" Text="Mobile caller input" />
    </orchard:BleeckerHeader.MobileActions>
</orchard:BleeckerHeader>
```

Set `Navigation` to original `IEnumerable<BleeckerNavItem>` entries; observable
collection/item changes update both directions. Derive items for caller metadata.
`ItemInvoked` returns the original navigation item. `BrandInvoked` returns the
original `BleeckerHeaderBrand`. An optional `Command` receives the explicit
`CommandParameter`. Requests are guarded by enabled/mounted/current state and
command availability. Command and collection notifications use the captured UI
dispatcher; old producers disconnect on replacement or unload.

`LinkTemplate` receives `BleeckerNavLinkContext` for brand and navigation. Bind
its prepared `Content`, `Style`, `Foreground`, `Href`, `Label`, original `Item`
and guarded `Command`. The brand item is constructed from the original brand
metadata, matching the source. `LinkStyle` replaces the source link style.
Custom templates must forward `Command` to invoke routing and close a mobile
navigation item. The component does not open destinations itself.

`Actions` and `MobileActions` accept original native controls or model values with
separate templates/selectors. Caller controls are retained through viewport and
navigation changes. The mobile action wrapper follows source conditional
truthiness: null, false, empty text, zero and NaN omit it; a truthy empty child
still creates its wrapper/gap. `IsMenuOpen` reports owned menu state, and
`ToggleNavigation()` activates the native equivalent of its named toggle.
Resizing to desktop hides the mobile panel while preserving state; returning to
narrow restores it. Closing clips mounted content without deleting caller input.

## Layout and themes

At viewport width 1024, the brand/toggle row becomes a desktop grid with centred
navigation. Media selection uses XamlRoot width even inside a narrow host.
`FullWidth` bypasses source container maxima while preserving horizontal padding.
A narrow source brand can overflow its intrinsic minimum. Image width, separator
shrink and inline baseline handling follow measured source layout, including
wrapped names; no ellipsis is substituted.

`Position` defaults to Fixed; Sticky and Static are also supported. Fixed paints
at viewport top and width while preserving its normal horizontal origin and
reserving zero flow height. Static and sticky reserve the row plus its bottom
rule; sticky stops at the containing parent's bottom. Header observes native
ScrollViewer/ScrollView and actual layout changes. Its owned template surface
overlaps without a new public host wrapper or moving caller content to a popup.

`HeaderHeight` consumes the shared `BleeckerHeaderHeight` resource, default 72,
separately from the one-pixel rule. AppShell's `OffsetHeader` uses the same row
token; a fixed Header contributes zero additional flow height. Its automation
peer delegates physical bounds/offscreen state to the rendered template surface;
normal flow size remains zero. Native automation clipping still applies when
part of the surface scrolls outside the viewport.

Light/dark resources preserve the 95% background and border alphas. Header uses
eight-pixel composition backdrop blur. The mobile surface uses both source
overlay shadows, a 384 maximum height and the 200-ms max-height/opacity
transition. Native motion follows the existing system preference. Component
theme and accessibility metadata support remain intact.

Header keeps caller-owned `Background` and `BorderBrush` values unchanged. Its
private solid paints combine color alpha and brush opacity, rounded once to
the nearest byte alpha (half upward), with native brush opacity one. Live color,
opacity and brush replacements update that private paint; retired or unloaded
surfaces disconnect their producers and reconnect on load. Gradients and other
non-solid brushes remain the original brush. This keeps the source's fractional
alpha contract public while avoiding WinUI's separate opacity quantization.

The default template requires a Grid named `PART_Header`. Template replacement
releases old presenters before attaching original caller controls. Missing
parts, invalid positions and invalid heights fail explicitly.

The shared [content presenter](content-presenters.md) installs original data
before selecting its template, while retaining unchanged presenters.

## Current Header paint prototype

Local package `0.1.0-header-paint-v1-local`, SHA-256
`ca03dd8b522e6346f1df7c19c140b814e958827d8e8c1fd01a243a3778d14e03`,
contains library
`901a04ec8995683e12dfccf905ebb07583903dde68f4b2d67ade8edc9526f083`.
The original background/border resources and frozen source PNGs are unchanged.
The private byte-alpha rendering fixes the dark bottom-rule probe: catalog and
fresh x64 consumer pass 23 native scenarios and the independent source paint
gate, with maximum channel error one against the unchanged one-level limit.
The added scenario covers original brush identity/values, live color/opacity,
replacement, gradients/null, retired templates and unload/remount behavior.

The package passes 72 source layouts, 6,096 zero-error geometry checks, 952
original brush checks and 144 exact private byte-alpha checks. Both architecture
builds and fresh consumer compilations pass; all 294 portable tests pass.
The same package passes Footer's 192 layouts and 11 contracts, and AppShell's
168 layouts and 44 ownership checks. Rejection tests reject 44 malformed layout
receipts and 68 malformed contract receipts/images, with both valid baselines
fully passing. Versioned x64/ARM64 catalog archives match every build file.
Evidence is sealed in `artifacts/current/header-paint-checkpoint.json`.
These checks do not establish full source raster or component acceptance.
Desktop input/focus, normal motion, scaling, performance, broader integration,
visible startup and native ARM64 acceptance remain open. Inventory remains
89 in progress, 23 pending and zero accepted: **79.5% entries started**.
Both required fictional-data demos remain in scope and unaccepted.

## Earlier Header/AppShell repair prototype

The current local package is `0.1.0-header-integration-v3-local`, SHA-256
`40a0e269215350deeca128231f0a2de3a0388211eed28a54978558c9fc45f658`.
Its library is
`39bc6e1236bd52044a9db52a0e833c0ecb291e1e3c03e225ea8e80ef18ee79ce`.
This prototype remains unaccepted.

AppShell now follows the original header/footer root's live `Canvas.ZIndex`.
The earlier wrapper allowed the opaque main region to paint above a fixed
Header and receive its topmost hit. The regression reproduces that failure in
the previous package. Current scenarios cover raising/lowering roots, original
and templated root replacement, clearing, restoration and retired producers.
The Header catalog fixture adds a bounded, fictional-data AppShell composition.

Twenty-two native scenario assertions pass, including both native scroll hosts,
eight completed ScrollView operations, live position changes and reparenting.
Six source composition states match 120 geometry values with zero error. The
independent audit verifies topmost hits and every viewport pixel against the
preserved raw frame. Dark logo filtering retains an off-screen decoder; its
private translation now aligns to physical pixels. Extraction uses measured
native bounds and does not rescale or repaint the capture.

The source paint probe gate is still **red**: the dark closed bottom rule differs
by two RGB channel levels from the source, exceeding the existing one-level
limit. The structural contract audit passes, while `sourcePaintProbeGatePassed`
and overall `passed` remain false. This distinction does not establish full
raster or component acceptance. The gap and raw failures remain recorded in
`artifacts/current/header-integration-checkpoint.json`.

Both architecture builds and fresh consumer compilations pass. The current
portable suite has 294 passing tests, and nine windowless font tests pass.
The exact v3 package also passes all 72 Header layouts (6,096 geometry values
at zero error and 952 brushes), Footer's 192 layouts and 11 contracts, and
AppShell's 168 layouts and 44 ownership checks. Rejection audits reject 42
damaged layout receipts and 68 malformed structural contract receipts. The
contract baseline's paint gate remains failed throughout those structural
rejection tests.
Native ARM64, desktop input/focus, normal motion, scaling, performance, broader
customization and normal visible catalog startup remain open. Inventory remains
89 in progress, 23 pending and zero accepted of 112: **79.5% entries started**.
Both required fictional-data demos remain in scope and unaccepted.

## Earlier Header v2 checkpoint

Local package: `0.1.0-header-v2-local`, SHA-256
`066b61cdcd7d6488aaca00169ae49e2670c9ac6df1c6e57c265b8174ee546063`.
The actual packaged library is
`b843b48085ecdad0ff4bec18c0ac25f69f6c2b4935d9badd2683415b040fbfb9`.

Catalog and fresh isolated x64 consumer each pass all 72 frozen states:
6,096 geometry checks with zero error and 952 brush checks. Both architecture
builds and fresh x64/ARM64 consumer compilations pass; their temporary caches
are removed. The x64 package passes 15 public scenarios with 468 isolation
guards, covering original caller histories, model templates/selectors, live
bindings, explicit command parameters, worker notifications, source replacement,
retired actions, template replacement, null logos, mobile truthiness and actual
ScrollViewer positions/parent limits. Four raw native PNGs and hit-test checks
verify scrolled ancestor presentation. Banner bounds follow native clipping.

The initial catalog had 172 measured layout errors. A first package replay also
exposed an asynchronous first-logo sample: its image had not finished decoding.
Captures now wait on the actual decoder's `IsLogoLoaded`, matching source
`img.decode()`; failures are preserved. Rejection audits reject 42 damaged
package layout receipts and 37 contract receipts/images.

That earlier package also passes Footer's 192 layouts, 15,216 zero-error
geometry checks, 1,890 brushes and 11 public contracts. AppShell passes 168
source layouts and 44 ownership checks with zero geometry error. All 293
portable and nine windowless font tests pass. Shared content-selector ordering
is covered at the native original-model boundary. Package DLL, five loose XAML
files, compiled PRI and README match the verified build/source bytes.

No activation, showing, focus, desktop input, OS settings or hosted workflows are
used by these checks. Full source raster, native input/focus, normal motion,
scaling, broader customization, ScrollView route, performance, normal visible
catalog startup and ARM64 runtime acceptance remain open. Header is started,
not accepted. Inventory is 89 in progress, 23 pending and zero accepted out of
112: **79.5% entries started**, not completion. Both fictional-data demos remain
required and preserved.

`samples/Orchard.Catalog/HeaderFixture.xaml` registers default and custom
fictional-data examples, separate original editors and host request output.
That earlier evidence is sealed by `artifacts/current/header-checkpoint.json`.
The prior preview/package remain preserved. The public wiki remote is
unavailable; documentation is maintained here.
