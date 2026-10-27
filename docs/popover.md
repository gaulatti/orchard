# Popover prototype

`BleeckerPopover` hosts an original trigger and nonmodal content. Inside a
`BleeckerModalHost` it uses the host's private portal plane; outside that host it
uses a native Popup. The [frozen contract](popover-reference.md) records its Bleecker source
and placement/input behavior. The local component and catalog fixture pass
hidden native checks and a fresh independent package replay. Complete native
input, visual and platform acceptance remain open.

```xml
<orchard:BleeckerPopover OpenRequested="PopoverRequested">
    <orchard:BleeckerPopover.Trigger>
        <Button Content="Edit fictional settings" />
    </orchard:BleeckerPopover.Trigger>
    <StackPanel Width="240" Spacing="12">
        <TextBlock Text="Component settings" />
        <TextBox Text="Original caller value" />
    </StackPanel>
</orchard:BleeckerPopover>
```

`Trigger` and inherited `Content` retain original native controls. Each supports
its own template/selector (`TriggerTemplate`/`TriggerTemplateSelector` and the
standard content properties). Selectors receive the original model. Unchanged
slots retain bindings, edits and selection. Native controls retain their own
commands, enabled state and metadata; Popover does not replace their peers.

## Open state and requests

`Open` is nullable: null uses internal history initialized from `DefaultOpen`
on the first load; true/false is authoritative controlled state. Later default
changes do not reset history. Releasing control restores that separate history.
`IsOpen` reports logical state, including while the native owner is unloaded or
disabled and has no presented popup.

`Toggle()` and `Close()` request transitions from a mounted, enabled component.
`OpenRequested` carries the requested Boolean. Uncontrolled requests update
history; controlled requests never assign `Open`. A host accepts a controlled
request by assigning its bound value. Input property changes emit no request.

`OpenChanging` supplies standard `CancelEventArgs` before a request is applied
or emitted. WinUI's button `Click` arguments do not provide a cancellation flag;
this event provides the native counterpart for preventing a trigger transition.
Trigger handling is queued after the original click and command finish. A
retired, disabled or replaced trigger cannot issue the queued request.
`Opened`/`Closed` report actual native presentation changes, independently of
controlled logical state. Removing the owner closes presentation without writing
the caller's state. Remounting preserves its internal history and controls.

## Placement and presentation

The shared root derives modal ownership from the declaration's actual surface
or registered popup branch. Nested Popovers retain the nearest modal owner.
Owned branches share that modal's z-order, sequential-target list and peer
scope. Activating a child modal makes its ancestor's branches inert immediately,
while keeping their paint and caller state. Background Popovers retain the
source's z-index 50 and become inert while any modal is active. The shared
private wrapper changes hit testing; caller `IsEnabled`, `Open`, content and
bindings are retained. Unloading declarations, replacing templates or removing
the root releases portal registrations. Remounting keeps original controls and
logical open state.

Moving the original declaration into or out of the shared root transfers its
presentation between the host plane and a native Popup. Original edits and
bindings stay intact. A delayed close notification from the retired native
Popup cannot close the new branch registration. Presentation reconciles the
actual XamlRoot when opening, including original nested controls that obtained
their native root after an earlier measurement/load callback.

The native fixture passes 32 Light/Dark ownership, placement, binding, nested
popup, lifecycle and transfer cases, plus four diagnostic captures. It invokes
the catalog's original confirmation button through its native peer in each
theme. A fresh NuGet consumer independently passes six public-API cases and two
captures for the popup/confirmation path. The independent gates reject 44
damaged native reports (including actual failed nested-root data) and 36 damaged
consumer reports. Both catalog and package retain the original 122-state
standalone geometry/paint regression. Actual physical input, focus delivery and
complete modal/Popover raster parity remain unverified.

`Side` defaults to Bottom, `Align` to Center and `SideOffset` to four. The enums
cover all four sides and Start/Center/End. Negative and fractional offsets remain
valid. Placement uses the actual trigger rectangle and XAML viewport, logical
RTL alignment, source collision shifting/flipping and physical pixel rounding.
`ActualSide`/`ActualAlign` expose the current computed placement.

The root-constrained popup stays within the application's rendering boundary;
oversized caller content keeps its intrinsic geometry. A separate content focus
host leaves the trigger's flow size intact. Background, border, corner radius,
padding, typography and content alignment use inherited component properties.
`SurfaceStyle` replaces the private surface's default layout style (minimum
width 224); use it for native surface sizing or template customization. The
component's explicit paint bindings continue to use its public properties.

The default component template requires a Grid named `PART_Layout`. Replacement
releases old presenters and input subscriptions before mounting originals in the
new template. The source's effective stylesheet supplies no popup animation;
the prototype does not add stock open/close motion or change OS preferences.
Theme and existing accessibility metadata support remain intact.

Visible-host opening focuses the first focusable content or the content host.
Tab navigation cycles inside it. Escape requests close on the topmost popover;
outside pointer/focus requests preserve outside focus. Controlled requests may
be ignored. Nested popovers register their content as owned branches. The group
peer provides ExpandCollapse, and the content uses a native Pane peer. The x64
foreground check verifies focus on the original TextBox and restoration to the
native trigger action. Nested modal/flyout interoperability and complete
accessibility behavior still require runtime acceptance evidence.

`samples/Orchard.Catalog/PopoversFixture.xaml` registers fictional default,
controlled, side/alignment and nested examples. Both required Monitoring and
Editorial demos remain preserved. Broader native input, complete raster,
scaling, performance and ARM64 runtime acceptance remain open. The focused
foreground fixture uses the desktop for component pointer and keyboard checks.

## Verified prototype checkpoint

Local package `0.1.0-popover-v1-local` passes the independent x64 consumer's
122-state source audit: 1,920 geometry/layout values have zero error and 360
brush comparisons pass. Five contract groups verify original native command
ordering and cancellation, retired queued triggers, controlled/internal history,
original editor state, template replacement/removal/restoration, unload/remount,
disabled presentation, template selectors and live model sizing. Two native
popup-child PNGs verify dimensions and background pixels; they do not compare
the complete source raster or overlay shadows. All native runs remain hidden
with zero activation, focus and input.

The same package passes Header's 72 layouts/23 scenarios, Footer's 192 layouts/
11 contracts and AppShell's 168 layouts/44 ownership checks. Header's source
paint probe remains within its unchanged one-level channel-error limit.
The portable suite passes 299 tests and the windowless font suite passes nine.
Catalog and clean package consumers compile for x64 and ARM64; ARM64 execution
has not been performed. Independent audit tests reject 14 damaged catalog
receipts and 17 damaged package receipts. Versioned catalog archives match every
built file. No Actions were dispatched and nothing was pushed.

The sealed evidence is `artifacts/current/popover-implementation-checkpoint.json`.
It preserves earlier failed layout/sizing runs and the preparation checkpoint.
This historical prototype checkpoint predates the focused foreground follow-up
below. Neither fictional-data demo is accepted yet.

## Focused foreground follow-up (2026-10-27)

`docs/evidence/popover-foreground-source-v1/checkpoint.json` records a fresh
source-built x64 catalog run in Light and Dark. Physical pointer opening mounts
and focuses the caller's original TextBox; Escape closes the surface and restores
focus to the trigger's native action. Physical Enter opens the controlled example,
Space activates its original close button, and an outside pointer click dismisses
the default example without changing the caller's text. Both settled open-surface
captures were visually reviewed. Five portable Popover reference tests, the x64
catalog build and the ARM64 library compile passed.

This run found and fixed two focus defects: an early unsuccessful initial-focus
attempt was never retried, and Escape tried to focus the outer, non-tab-stop
`BleeckerButton` instead of its native action. The new fixture exercises those
behaviors at the physical input and visible UI boundary. Broader nested/modal
input, complete raster and scaling, performance, packaged foreground input, and
native ARM64 runtime remain open. No new package was qualified by this source
run.

The later [v7 local preview](releases/2026-10-27-components-v7-local.md) repeats
this focused physical route in both themes from the extracted x64 catalog. Its
Orchard DLL matches the independently restored NuGet package. Broader
nested/modal input, full visual review and native ARM64 runtime remain open.

The [nested-trigger repair](evidence/popover-nested-trigger-v1/checkpoint.json)
closes the catalog's physical nested-button path. The child button received a
native Click while its Popover had no registered trigger buttons: the trigger's
visual child appeared after the owner's earlier layout reconciliation. Popover
now reconciles buttons when its trigger slot lays out and removes that listener
when the template retires. The existing foreground fixture opens parent and
child with real pointer input, checks focus on the original child editor, then
uses Escape twice to close child and parent in order with focus restored. The
Light/Dark source and extracted v11 x64 runs pass. The v11 NuGet DLL is byte
identical to the extracted x64 catalog DLL and passes an independent x64
consumer runtime; ARM64 consumer compilation and catalog archive validation
pass without claiming native ARM64 execution. Hidden reference and modal-branch
contracts still pass. Full visual, scaling and broader nested/modal input
acceptance remain open.
