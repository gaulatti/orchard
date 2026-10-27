# Modal

`BleeckerModal` is the reusable arbitrary-content dialog. Declare it under the
window's `BleeckerModalHost`, supply `Title` before opening, and keep `IsOpen`
owned by the caller. An empty title is allowed. The original `Content`,
`ContentTemplate` and `ContentTemplateSelector` remain caller-owned; live slot
updates are queued together before selecting the current model's presentation.

```xml
<orchard:BleeckerModalHost>
 <Grid>
  <orchard:BleeckerModal Title="Fictional component example"
      IsOpen="{x:Bind ExampleOpen, Mode=OneWay}"
      CloseRequested="OnCloseRequested">
   <TextBox Header="Fictional note" Text="Original local content" />
  </orchard:BleeckerModal>
 </Grid>
</orchard:BleeckerModalHost>
```

Close button, active backdrop, Escape and Window-pattern Close request closure.
`RequestClose()` uses the same boundary. The component raises `CloseRequested`,
then executes the captured optional `CloseCommand` with its original
`CloseCommandParameter`. It never changes `IsOpen`. A disabled component, rejected
command, closed/unloaded declaration or covered parent emits no request. The
caller accepts a request by changing its bound open value. Nested modals and
AlertDialogs use the same overlay ownership; popup components retain their
existing logical-owner contracts.

The pinned source uses a 576px maximum width, 16px viewport clearance, a 1px
border and 14px radius. Header padding is 24px horizontally and 20px vertically;
at viewport width 768px, horizontal padding becomes 28px. Body padding is 24px,
then 28px at that breakpoint. The title is Encode Sans Semibold 20/28 with
−0.02em tracking. The source flex row can shrink the 36px close button to its
20px icon-and-border minimum. The entire surface, including its header, scrolls
when content exceeds the viewport; the native ScrollView retains its providers.
`SurfaceStyle` targets `BleeckerOverlaySurface`; base it on
`BleeckerModalSurfaceStyle`. The catalog's wide square example uses maximum
width 672px and zero radius.

The source's modal presence and motion use the same contracts as AlertDialog:
mount immediately, show after 20ms, retain inert closing paint for 150ms, and
animate independent surface/backdrop properties with 190ms opening and 150ms
closing curves. Reduced motion shortens animation, while presence timers stay
unchanged. Unload, retemplate and root changes release timers, content presenters,
blur resources and registrations without rewriting caller bindings or content.

Live growth of the original content also remeasures the outer surface; it must
not remain at a previously cached compact height. Shrinking it clamps the scroll
offset and restores the compact surface without replacing the original editor.
The earlier native and independent-package runs retain the stale-height failure
as a regression receipt.

Automatic roots can remain stretched to an old actual height after their
children or bound text shrink. After native layout, Modal uses the content
slot's reduced desired height to invalidate the outer measure. It does not
rebuild the content renderer from inside measurement. The live-content source
fixture covers child resizing, child insertion/removal, bound multiline text,
bound root height and plain text. Its ten histories are independently captured
with original DOM nodes retained. Native composition checks exercise the same
size transitions plus Modal-owned Popover, Tooltip and controlled Select
suspension, resumption and teardown.

The source reference freezes 40 Light/Dark layouts across four viewport sizes,
including empty, wrapping-title, long-content and customized surfaces, plus 12
accepted/refused close histories and two scrolling histories retaining original
edits and moving the header with the body. Independent recapture
matches all 41 files byte-for-byte. Portable geometry checks consume real source
intrinsic title metrics. The current catalog and independent x64 package each
pass 40 native layouts, 928 exact geometry values and 26 content/ownership cases.
They verify original editors/models/selectors, caller open bindings and commands,
nested ownership, live rendered-root sizing, header scrolling and shrink/clamp
behavior. Each records six diagnostic PNGs and 753 isolation guards, with no
activation, focus or hardware input. Twenty damaged reports are rejected; the
actual earlier stale-height package and incomplete native receipt are retained.
Both architecture catalogs and isolated consumers compile. Package payload,
138 loose XAML files, native PRI, fonts and README match the tested inputs.

The component remains unaccepted. Remaining physical input/focus, raster, motion,
scaling/performance, broader customization and content reflow/integration cases,
and native ARM64 runtime are separate remaining gates. The current style cases
verify maximum width and radius overrides. Narrator and OS contrast campaigns
remain excluded by the library scope.

The [extracted-v7 x64 foreground probe](evidence/modal-foreground-v1/checkpoint.json)
passes in Light and Dark. Physical clicks open the Modal and nested AlertDialog;
the first Escape closes only the nested surface, the second closes the parent
through one caller request, and focus returns to the opener. The Modal, shared
host and catalog specimen are unchanged since that v7 archive. This is focused
input and ownership evidence; the remaining physical matrix, fresh v8 archive,
visual, scaling, motion and ARM64 runtime checks are still open.
The [archive-identical v8 catalog file set](evidence/components-v8-linked-catalog-v1/checkpoint.json)
also passes this nested physical ownership and focus-return probe in both themes
from a separate runnable directory. Fresh full ZIP extraction and the remaining
Modal matrix remain open.

`ModalsFixture` is registered directly after AlertDialogs and preserves both
fictional-data Monitoring and Editorial demos. It offers original editable
content, a nested confirmation and a long customized surface. No product service
or persistence is involved.

The editable example also expands and shrinks a fictional paragraph through its
real catalog button handlers. Its native fixture checks that the original note
and content controls remain intact when the surface returns to its compact size.

The live-content gate passes 16 cases in both the catalog and an independently
restored x64 package, including 90 exact comparisons with the ten source
histories. The previous package fails six shrink scenarios with the corrected
probe. The real catalog fixture separately passes eight native button invokes
across both themes and records four expanded/shrunk PNGs. These are hidden
software-invocation checks; the separate foreground probe above covers one
nested physical input/focus path. Full raster acceptance remains open.
