# AlertDialog source contract and preparation

Pinned Bleecker source: `src/components/alert-dialog.tsx` at
`a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`5a7a05a0306b59817b11e7831ed7d446d1739e2c02362f40d491bfff1297fcaf`.
Read its Button, modal/presence hooks, global styles, tokens and the original
AlertDialog/mixed-modal stories together. The caller-controlled native control
and catalog fixture are now implemented as an unaccepted preview; the entry is
in progress. The original source preparation and historical host-only packages
below retain their narrower evidence scopes.

## Acceptance criteria before implementation

- Keep `IsOpen` controlled by the consumer. Confirm/cancel actions request the
  caller's original callback/command and do not independently close the dialog.
  Defaults are Cancel/Continue, default variant and loading false. Source title
  is required string content; description and label strings stay caller-owned.
- Loading disables both buttons, adds the original 16-pixel spinning Loader2
  beside the confirmation label, and rejects Escape/backdrop cancellation.
  Escape is consumed by the active modal even when loading rejects dismissal.
  Caller-driven closure remains authoritative during loading.
- Render a portaled alert-dialog role with modal semantics, title/optional
  description relationships and an explicitly focusable dialog surface. Only
  the active modal receives interaction; the existing background input state
  and body scroll setting are restored after the last modal closes.
- Follow the shared modal contract: focus the first enabled sequential control
  (Cancel normally, the surface when loading leaves none), cycle Tab/Shift+Tab,
  preserve logical ancestry across portals, allow registered portaled widget
  branches, and route dismissal to the active layer only. Restore original
  connected focus or the surviving parent; support simultaneous and out-of-order
  modal teardown. Do not assume a one-dialog-per-window policy without testing
  the original mixed-modal contract.
- Center the surface within 16-pixel viewport padding. Source maximum width is
  448 pixels and maximum height is viewport height minus 32; the surface scrolls
  when constrained. Inner padding is 24 pixels below the 768-pixel breakpoint
  and 32 pixels from that breakpoint. Header icon is 40 by 40 with a 20-pixel
  triangle, a 16-pixel text gap and a 24-pixel bottom margin. Footer uses a
  one-pixel top border, 20-pixel top padding and a 12-pixel button gap. Capture
  actual text wrapping, RTL, empty strings and long labels before fixing layout.
- Preserve the original theme paint: 14-pixel dialog radius, source borders,
  surface/shadow, variant icon palette, backdrop alpha and two-pixel backdrop
  blur. Resolve actual tokens, including dark sand, rather than assuming light
  values apply. Caller surface styling follows normal native override rules.
- Keep presence distinct from visibility and input ownership. Opening sets
  presence immediately and visibility after 20 ms. CSS opacity/transform enter
  over 190 ms with the premium curve; surface closing uses 150 ms and ease-in.
  Backdrop opacity uses its own standard cubic curve (.4,0,.2,1) in both
  directions, with 190/150-ms durations. The
  closed transform is translateY(4px) scale(.992), animated around the center.
  Closing becomes inert/aria-hidden immediately and stays mounted for 150 ms;
  this presence delay also applies when CSS reduced motion snaps. Reopening
  cancels pending removal. Capture interrupted transitions and disposal.

## Frozen evidence

`tests/Reference/AlertDialog.json` contains 56 original layouts/PNGs, 36 semantic
input/ownership histories, four presence timelines (40 samples) and four CSS
endpoint timelines (20 samples). Final independent captures match all 57
receipt/PNG files. The collector verifies all 122 TypeScript inputs scanned by
the source CSS compiler and all eight bundled local source files against Git
blobs, plus actual runtime dependencies, fonts and tools. Tailwind 4.2.2 matches
the pinned source lock; Playwright 1.62.1 drives headless Edge with scrollbar
hiding explicitly removed.

Input histories capture ownership/focus/presence rather than uncontrolled
closing-frame paint. CSS presentation is sampled separately through Web
Animations; presence uses isolated browser-realm timers in both motion modes.
Windows time and preferences are never changed. Earlier exploratory receipts
included an overly broad body focus label and uncontrolled transient paint;
those are superseded. A parallel pair also differed at 17 dark-shadow pixels
by one channel value; the final sequential captures match every byte.

The layout model matches all 56 layouts using measured text/button producers;
the presence model matches entry, exit, reopening and disposal, and the endpoint
motion recipe matches every controlled sample. Nested/portaled focus, simultaneous mount,
ancestor-first teardown and restoration of original/dynamic background state
are bound to original browser histories. DOM portal order differs from logical
modal ancestry and must not define the active layer.

`tests/Reference/AlertDialogMotion.json` adds 18 interrupted histories, 90
transitions and 234 presentation samples. Two independent captures match every
byte. A MutationObserver freezes transitions immediately after the original
component's state commit; otherwise a short reversed backdrop transition can
finish before a separate browser-tool call reads it. The fixture uses React's
`flushSync` commit boundary, retains the original component/hooks/CSS, verifies
all bundle inputs, and keeps presence timers in an isolated browser realm.
This measures paused CSS reversals, not real-time timer/CSS integration.

Surface opacity/transform switch between premium entry and ease-in exit;
backdrop opacity keeps the standard curve. Each property retains its own
reversing shortening factor. The old curve determines the interrupted value
and factor before the new transition adopts its new curve, as specified by
[CSS Transitions](https://drafts.csswg.org/css-transitions-1/#reversing).
`BleeckerCssTransition` accepts an optional curve per retarget; existing callers
retain their configured curve. `BleeckerAlertDialogMotion` matches the captured
durations and all 234 samples. The portable suite now passes 322 tests, including
the existing shared motion regressions. Reduced CSS duration stays separate
from the source's unchanged 20/150-ms presence timers.

## Native hosting constraints

The repository pins Windows App SDK 2.5.1 and WinUI 2.3.9. Its installed
`Microsoft.WinUI.xml` confirms `ContentDialog.ShowAsync(placement)`,
`ContentDialogPlacement.InPlace`, root-constrained Popup, and focus cancellation
APIs. InPlace determines visual-tree placement; it does not establish support
for the original logical modal stack. Microsoft's
[ContentDialog reference](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.contentdialog)
documents a single open ContentDialog restriction. One stock ContentDialog per
source layer therefore cannot be assumed to implement the measured nesting.

Native host evaluation must retain simultaneous descendants, caller-controlled
dismissal, active-only interaction, registered popup branches, original input
and focus state, and out-of-order teardown. A single shared framework host or a
library-owned overlay coordinator still requires validation on the pinned
runtime. No host choice or native modal-runtime success is claimed here.

`BleeckerModalStackModel` now matches the original sequential and child-first
simultaneous order, active layer and overlay depth in both themes. It preserves
the source's root-group/depth/registration ordering and reorders survivors when
an ancestor disappears. Teardown rewrites successor restoration targets owned
by the removed layer or its registered branches; removing an inactive ancestor
does not request focus restoration. Four regressions cover source dismissal
targets, ancestor-first teardown and independent root groups. This is identity
and target planning only: the native adapter still must supply actual logical
ancestry/branch containment, resolve connected and interactive targets, apply
focus, and preserve caller background properties. No native focus or inertness
is proved by these model tests.

`ShowAsync`, native focus and native modal opening have not been used during
this background-only preparation.

## Hidden native host prototype

`BleeckerModalLayerHostCore` is an internal Grid-based layout prototype. It owns
a background ContentPresenter and portal frames, preserving the original
background/control instances and their bindings. Active-only pointer shielding
changes the owned wrapper and frames, without disabling or restyling caller
controls. A host peer exposes the active surface and its registered branches;
closing layers stay available for paint until detached but stop receiving hit
queries. Restoration remains a target plan, without calling native focus APIs.

The catalog's `--probe-hidden-modal-host` fixture passes 12 cases across Light
and Dark: 68 snapshots, 592 exact geometry values, 32 peer scopes, full three-layer
source ordering, child-first activation, ancestor-first teardown, branch
ownership, reopening, dynamic/replaced background content and binding retention.
Fourteen full-client diagnostic PNGs are retained. All 960,000 pixels compared
before/after pointer shielding match. An independent auditor rejects 36 damaged
receipts, including a different real native PNG with an updated matching hash.
Its 518 guards record no activation, focus, visibility, popup opening or input.
The colored layer rectangles are diagnostic primitives, not AlertDialog paint.

Earlier fixture runs forced measurement before native theme initialization and
observed a temporary ContentControl template on a newly constructed button.
The fixture now drains native style initialization first. Both experimental
button resource changes were reverted byte-for-byte to the prior checkpoint;
the button implementation and styles are unchanged. A second early failure
used visual parents to validate an unattached overlay: the core now checks its
declared children before native visual materialization. Failed runs are retained.

The initial local `0.1.0-modal-host-core-v1-local` package contains the models
and internal host prototype. Fresh isolated x64/ARM64 consumers compile. A
fresh x64 consumer verifies ten original C# button construction and caller
template/style restoration states, without fixture style injection, activation,
focus or input. The portable suite remains 322 passing tests; both catalogs
compile. Future CI steps are registered without dispatching a hosted run.

The next ownership pass reproduced three branch failures in both themes:
another host could reposition a branch, a detached branch could still move,
and invalid attachment took the original content before rejecting its position,
preventing a later valid attachment. The core now validates placement before
taking content and checks the owning host and live registration before moving
a branch. The retained failed run has six failures; the corrected run passes
all 18 Light/Dark cases with 74 guards. The independent auditor rejects 34
damaged receipts, including the actual failed run with success/identity fields
rewritten. All original 12 layout/paint cases still pass.

That pass also verifies native inline Hyperlink ownership inside nested TextBlock
spans, RichTextBlock paragraphs and registered branches, rejection of background,
foreign, inactive and removed links, and restoration-target rewiring on ancestor
teardown. These predicates already worked and required no implementation change.
The pinned SDK's actual TextPointer visual owners are checked independently;
Hyperlink.Focus and native focus routing are not invoked.

The corrected `0.1.0-modal-host-core-v2-local` preview contains the branch fixes.
Both catalogs and fresh x64/ARM64 consumers compile, and the fresh x64 consumer
again passes all ten button template/style states. All 135 loose XAML files,
compiled PRI, fonts and README match their sources; the executed native host
and both clean consumers use the exact packaged library DLL. The old v1 preview
is retained as historical evidence. Neither package implements public AlertDialog.

The public root host is now a preview `BleeckerModalHost` content control. Put one
host around the entire content of a window; it preserves the original content,
DataTemplate and DataTemplateSelector through a private ContentPresenter. A
modal declaration outside that host, a second host in the same XamlRoot, or an
opening host that does not cover the viewport must fail explicitly. Its private
overlay plane retains the tested core rather than changing caller bindings.

The hidden public-host fixture verifies original content and bindings after open/close and
template changes; nearest logical ancestry through registered branches; active
layer pointer/peer isolation; correct sequential target plans including native
inline links; safe detach and reload; and no focus, activation or visible window
in a hidden-window probe: 30 Light/Dark cases, 16 actual catalog states, six
diagnostic PNGs and 286 guards. Its auditor rejects 55 damaged reports, including
the actual process-close failure with successful in-process fields. These
background checks cannot establish physical
keyboard routing or actual Windows focus cancellation. Native input acceptance
requires a separately authorized desktop session.

The caller-controlled AlertDialog API, native surface and catalog fixture are
implemented as an unaccepted preview. The source-backed renderer checks all 56
layouts, 2,664 geometry values, actual text lines and dialog relationships.
The controlled-action fixture checks 22 Light/Dark cases through eight software
native Button invokes, commands, live properties, close/reopen and teardown.
Neither fixture sends physical input. See [the public API and remaining work](alert-dialog.md).
Actual native focus cancellation and sequential keyboard routing
(including the now-verified text-element target ownership), all portaled widget
adapters, real-time presence/motion integration, blur/shadows and full platform
acceptance remain open. The current preview
does not prove Windows modality or physical input delivery.

The actual catalog root now uses the host. Its Header fixture was declared but
missing from the navigation registry; it is now registered after Footer, and a
portable regression checks every declared fixture against navigation. Window
close exposed a shared disabled-pointer observer dereferencing a missing content
island. The observer now clears that detached state; an explicit post-close
refresh and a clean process exit are required by the native gate. See the
[root-host contract](modal-host.md).

The native preview now includes the AlertDialog control and fixture. Its latest
source must be verified in a fresh package before any package replay is claimed;
older host-only packages contain no public dialog. The local preview work
and does not replace the separately retained release preview. Native modal
ownership, text/button rendering, real-time motion
integration, blur/shadows, actual focus/input, scaling/performance and native
ARM64 acceptance remain open. Modal focus and pointer
verification cannot be claimed from background layout or state injection.

Work is exclusively the reusable library and fictional local component demos.
Monitoring and Editorial remain required. Background source/build work is
authorized; no native activation/focus/input, Narrator, OS accessibility/contrast/
time setting changes, pointer heartbeat, GitHub Actions or pushes are performed.
