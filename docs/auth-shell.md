# AuthShell source contract

AuthShell is a reusable page layout in the fixed Windows component inventory,
within the [component-library scope](library-scope.md). It supplies caller-owned
brand, content, footer and optional aside slots. Fictional local forms may
demonstrate these slots; authentication services and workflows remain excluded.
The worktree contains a public `BleeckerAuthShell` implementation and registered
catalog fixture. Hidden source runtime verifies 104 states and 24 behavior
checks across both themes, including 5,134 geometry/font checks and 144 rendered
palette probes; 94 corrupted receipts/files/pixels are rejected. Both solution
architectures build without warnings or errors. The fresh local NuGet consumer
and extracted catalog independently pass the same 104 states and 24 checks.
All state and contract data agree across the three hosts. The 2026-10-29 x64
milestone accepts Auth Shell after the focused checks below.

The pinned Bleecker producer is `src/layout/auth-shell.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`b5bc05e24a2e42a054605aa51f6d8a9794f4d86ba72e1dd4baac7584a3c1f41c`.
The frozen `tests/Reference/AuthShell.json` SHA-256 is
`e40f5749a3a2abe09c77b2b0aaf3a080c896a857af359fec2f9845419aeec0e8`.
It retains 104 original PNGs, 208 stable capture samples, 20 behavior histories,
all six producer dependencies, 122 pinned CSS scan inputs, 15 actual bundle
inputs and six original fonts. Independent recapture matches every PNG and
metadata field without exclusions. The auditor rejects 106 counterexamples,
including damaged source/CSS/bundle/capture files and single-channel pixel
corruption. These are browser source observations, not Windows input evidence.

The default layout is centered with side end. Split layout requires a truthy
aside. Below the fixed 1024 CSS-pixel breakpoint, that aside stays mounted but
is hidden and excluded from Tab navigation. At 1024px it becomes visible with
five twelfths of the shell width, at least 24rem; the access column gets the
remaining seven twelfths. Start/end are logical positions and reverse in RTL.
The DOM always puts access before aside, preserving Tab order even when the
aside appears first. Optional brand/footer wrappers use producer truthiness:
zero, empty text, false and null omit them; an empty array retains an empty
wrapper. Children zero still renders a line containing zero.

The access section is at least the viewport height. Its horizontal padding is
1.25rem below 640px, 2rem at 640px, 3rem at 1024px and 4rem at 1280px. Vertical
padding is 1.5rem below 640px and 2rem thereafter. The inner column is centered
and limited to 28rem. Content fills the space between brand and footer with
2.5rem vertical padding below 640px and 3.5rem thereafter; short content centers
vertically, while tall content grows the page. Tall aside content also grows
the shared row. Root text scaling changes rem sizes without moving the fixed
viewport breakpoints. Footer typography is Libre Franklin, .75rem with 1.25rem
line height, using secondary text tokens.

The aside clips decoration, uses the theme's deep-sea color and white text,
and bottom-aligns its caller content with 3rem padding, increasing to 4rem at
1280px. Deep sea is `#1a374d` in light mode and `#182533` in dark mode. Two
physical top/right circles are 20rem and 13rem, offset by -6rem and -2.5rem,
with a one-pixel white/10 border. They ignore pointer input and accessibility
navigation. Caller content paints above them.

The source preserves access editors through layout, side, resize, text-scale
and theme changes. Responsive aside hiding preserves its editors. Switching
to centered or a falsy aside unmounts the aside; restoring it mounts new content.
The caller owns form actions; the diagnostic fixture prevents submission and
performs no network operation. Native slots, retained templates, public API,
catalog variants, runtime, scaling, input and delivery are covered by the
current x64 milestone. Historical pixel parity is outside its acceptance gate.

## Native API and local examples

`BleeckerAuthShell` is a `ContentControl`. `Layout` is `Centered` or `Split`;
`Side` is logical `Start` or `End`. Its inherited `Content`, `ContentTemplate`
and `ContentTemplateSelector` supply the main content. `Brand`, `Footer` and
`Aside` each have matching `Template` and `TemplateSelector` properties.
`AsideLabel` names the complementary region. The owner is a main landmark;
the optional footer is content information. Decorative circles stay outside
hit testing and content accessibility navigation.

Pass existing UI elements or scalar content directly. Supply a data template
or selector before presenting a model; the original model is passed unchanged.
Unrelated layout updates keep existing presenters, controls and bindings.
Removing a templated aside releases its presenter and restoring it builds a
new template root. Caller-owned UI elements retain their own identity and
values when the caller reuses them; the library does not clone caller controls.
A supplied template creates its wrapper even for null or scalar model values.
Untemplated optional zero/false/empty content follows source truthiness; main
content zero remains visible.

`FontSize` scales the component's rem dimensions from the default 16; viewport
breakpoints use `XamlRoot.Size`, independently of the allocated control width.
The layout observes native text scaling when `IsTextScaleFactorEnabled` is true.
The shared viewport minimum helper preserves caller minimum-height overrides.
The access region still has its own viewport minimum, as in the producer.
`Background`, `Foreground`, `Padding`, borders and other inherited native
properties remain available. `AccessBackground`, `AsideBackground`,
`AsideForeground` and `FooterForeground` expose the region brushes. Colors come
from the central token generator and update with the component theme.

```xml
<orchard:BleeckerAuthShell Layout="Split" Side="Start"
    AsideLabel="Fictional collection note" Brand="Orchard Atelier"
    Footer="Fictional local component demo">
    <orchard:BleeckerAuthShell.Aside>
        <TextBlock Text="A place for fictional collections and their readers."
                   TextWrapping="Wrap" />
    </orchard:BleeckerAuthShell.Aside>
    <StackPanel Spacing="16">
        <TextBlock Text="Welcome back" />
        <orchard:BleeckerInput PlaceholderText="reader@example.test" />
        <Button Content="Try a caller-owned local action" />
    </StackPanel>
</orchard:BleeckerAuthShell>
```

`AuthShellsFixture.xaml` is registered under **Auth shells**. It includes live
layout/side/RTL/text-size controls, fictional member and recovery forms, empty
and scalar slots and a disabled example. Its buttons update local text only;
no authentication, product service or external request is implemented.

## Historical verification limits

The hidden fixture uses a native scrolling page host to match the source
document's tall-content behavior. It verifies logical tracks, padding, centered
content, tall access/aside content, source slot truthiness, caller typography,
decorations, original model identity, selector binding updates, template
replacement, native action/disabled semantics and unload/reload retention.
The host remains hidden with no focus, activation or hardware input. Both
fictional Monitoring and Editorial demos also pass the source host audit.
These observations do not establish actual keyboard, pointer or Tab routing.

Strict original-browser/native image equality still fails: 770,324 differing
pixels across 104 captures, maximum channel difference 250, zero byte-identical
PNG pairs, with the allowed channel difference fixed at zero. Native caller
controls and text rendering require further visual verification; no raster
threshold or source screenshot has been changed. Source/archive comparison
matches all 104 canonical PNGs byte for byte. Source/package comparison retains
76 differing pixels (maximum channel difference 85) in `Dark/split-end-1279`,
inside the caller content title at x253–290/y281–289; all other 103 PNGs match
byte for byte. This strict package-image gate remains failed. The diagnostic
does not establish a rendering cause.

The `0.1.0-auth-shell-v1-local` NuGet preview has an AnyCPU library, PRI, 156
themes, 157 loose XAML files, six fonts, two images and the current package
README. Both fresh isolated architecture consumer builds pass; the x64 consumer
is runtime verified. Its temporary build roots were removed while preserving
the shared dependency cache. The runnable x64 catalog ZIP's 1,360 extracted
files are byte verified and replayed with the component and both fictional
demos. All 469 portable and 10 Windows text tests pass. The source and package
auditors each reject 94 corruptions. These remain local preview artifacts.

At that historical preview, broader container widths independent of the
viewport, customization and lifetime cases, measured performance, actual
display/text scaling, physical input/focus/Tab and native ARM64 runtime were
open. The current milestone resolves the focused x64 checks below.

For the current x64 milestone, the published-package catalog passes the 104
Light/Dark source states and 24 contracts, plus the 132 container/root states
and 32 contracts. A focused foreground catalog check passes real Tab, Space,
pointer activation and caller-content retention in both themes. The independent
package consumer passes centered and split layouts in Light/Dark at native 150%
scale, with all visible text and regions inside the viewport. The component is
static and its rendered geometry stays settled with animations disabled. The
independent package consumer compiles. These five checks accept the component
on x64; ARM64 runtime remains unverified. The older image and broader platform
observations above remain historical, outside this milestone's gates.

The subsequent [container/root-size correction](auth-shell-layout.md) adds a
separate 132-state reference and closes padding-overflow, centered-height and
explicit-width alignment defects. It has its own delivery and regression
evidence; the earlier preview and its recorded failures remain preserved.
