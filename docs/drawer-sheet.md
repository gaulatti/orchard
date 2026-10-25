# Drawer and Sheet native preview

`BleeckerDrawer` and `BleeckerSheet` share the abstract `BleeckerEdgeDialog`
content and lifetime contract. This implementation is being validated against
the [pinned source evidence](drawer-sheet-reference.md). A successful build is
not native runtime or package acceptance; neither component is fully accepted.

Place declarations beneath one `BleeckerModalHost` for the window's XamlRoot.
The shared host handles modal ordering, nested ownership, background isolation
and Escape. The declaration stays in its original logical scope; its presentation
uses the host's private plane, without opening a native Popup.

```xml
<o:BleeckerModalHost>
 <Grid>
  <!-- The caller sets IsOpen and accepts or refuses CloseRequested. -->
  <o:BleeckerDrawer x:Name="Drawer" Title="Fictional local note"
      Description="The original editor stays with its caller."
      CloseRequested="CloseRequested">
   <TextBox Text="Original fictional value"/>
  </o:BleeckerDrawer>
  <o:BleeckerSheet x:Name="Sheet" Side="Right" Title="Fictional component"
      CloseRequested="CloseRequested">
   <TextBox Text="Another original local value"/>
  </o:BleeckerSheet>
 </Grid>
</o:BleeckerModalHost>
```

The handler may set `((BleeckerEdgeDialog)sender).IsOpen = false`. The component
only raises `CloseRequested` and then executes the captured `CloseCommand` with
`CloseCommandParameter`; it does not replace a binding or set `IsOpen` itself.
The close command's `CanExecute` state gates requests. An inactive modal cannot
close itself through `RequestClose()` or its automation Window close pattern.
Unloading releases the layer, timer, command subscription and content presenter.

`Title` and `Description` are optional. Empty values omit the associated heading.
The unnamed dialog exposes `Dialog`; title and description metadata update on
the existing surface. Content, templates, selectors, model identity and bindings
remain caller-owned. Ordinary content uses the same original slot as Modal.

Drawer is bottom aligned with a fixed decorative handle and header, a
content-following height up to 90% of the viewport, and a scrolling body. Touch
dismissal tracks touch pointers only, requests close after a downward distance
greater than 60, and resets on release/cancel. Physical touch delivery remains
unverified. The source's unused `snapPoints` does not create native snapping.

Sheet's `Side` is a `BleeckerSheetSide`: Left, Right, Top or Bottom. Vertical
sheets fill viewport height and default to a maximum width of 384. Horizontal
sheets follow natural content height, including content taller than the viewport.
Padding switches at viewport width 768. `ScrollContent` defaults to true; false
disables the outer scroller so the caller can provide a stretching ScrollView.
The body boundary clips overflow and preserves the caller's sizing properties.
Full consumer layout/customization acceptance remains separate from this preview.

`SurfaceStyle` targets `BleeckerOverlaySurface`. Base it on
`BleeckerDrawerSurfaceStyle` or the corresponding
`BleeckerSheetLeft/Right/Top/BottomSurfaceStyle` to retain the source defaults.
`BleeckerSheetSurfaceStyle` is the square, width-limited vertical base.
Styles can override corner radius and maximum dimensions. A Drawer `MaxHeight`
override is an absolute native value; use the caller's viewport sizing logic
when a relative custom cap is needed. Theme values remain central resources.

Presence uses a 20 ms entry delay and 180 ms inert exit retention. Panel and
backdrop transition independently using the source curves, including Drawer's
different backdrop exit curve. System animation preference is read without
changing it. Reduced motion shortens presentation transitions; it retains the
source's presence timing. Native animation sampling is still required.

The **Drawer and Sheet** catalog fixture follows Modal in navigation. It includes
all four Sheet sides, a nested Sheet inside Drawer, retained editors, body
growth/shrink actions, and a caller-owned scrolling example. Both fictional
Monitoring and Editorial component demos remain required and unchanged.

The background reference probe and fresh independent x64 NuGet consumer each
pass all 194 source states: 5,144 exact geometry values, 42 lifecycle/content/
scrolling scenarios, six software invokes and ten diagnostic PNGs. Original
models/selectors and templated editors survive live bound-height resizing.
The expanded actual catalog passes 16 cases, 36 software invokes and 14 diagnostic
PNGs covering both themes, all sides, nested composition, owned popup controls
and caller-owned scrolling.
Nine windowless font tests and all 334 portable tests pass. Composition-only
validation inherits the font result after verifying unchanged library inputs. Both catalog
architectures and clean independent x64/ARM64 consumers build; ARM64 runtime
has not been exercised.

Owned Popover, Tooltip and controlled Select are verified separately for Drawer
and all four Sheet sides in both themes: 30 cases and 180 behavior assertions
pass in the catalog host and a fresh independent x64 consumer of the unchanged
NuGet preview. All 30 measured popup bounds fit the real 900×760 XamlRoot.
A newer owner suspends the original branch; closing it resumes the original
editor/data. Closing an owner removes its popup presentation. Controlled Popover
open state and Select value/binding/options remain caller-owned. Tooltip reopens
only after a new request when its owner is remounted. Suspended Select actions
emit no requests; active actions return the original option without rewriting
the caller's value. These tests use public properties and software UIA invokes,
with 1,957 visibility/focus/activation/native-Popup guards per run. Physical input
and actual key routing remain separate acceptance boundaries.

The catalog now demonstrates an editable owned Popover in Drawer, a caller-
accepted Select and an attached Tooltip in Sheet. Its real handlers and original
controls are exercised by the expanded catalog probe. Independent auditors reject
21 damaged composition reports and 17 damaged catalog reports. The library DLL,
fonts and packaged sources remain unchanged; additional evidence does not imply
completion of the remaining visual, motion or platform work.

The first native audit rejected 22 states because native headings omitted the
global h2 tracking. The corrected renderer retains -0.02em tracking, and both
native gates reject the actual earlier run's 134 geometry differences as well
as 23 damaged reports. This is source layout and boundary evidence, with
complete component acceptance still open. Physical input/focus, complete source
raster and motion, advanced
customization/scaling/performance and ARM64 runtime remain open. Narrator and
OS contrast campaigns are excluded. GitHub Actions are not dispatched by this
work.

The [interruption evidence](drawer-sheet-motion.md) adds 390 original CSS samples
matching the C# clocks and live reduced-motion property checks in both native
hosts. The machine's disabled-animation preference is preserved; normal-mode
native timing and pixel motion remain open.
