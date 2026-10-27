# Menubar native preview

`BleeckerMenubar` is a reusable WinUI control for ordered command menus. It uses
Orchard's typography, theme brushes, card surface shadow and overlay shadow.
The [frozen source contract](menubar-reference.md) remains authoritative evidence
of the original Bleecker behavior. This preview is in progress; native test
results must be read from the current checkpoint before claiming verification.

The selected local preview `0.1.0-menubar-peer-metadata-v1-local` includes keyboard, radio
provider and peer metadata repairs. Source, independent NuGet and unpacked catalog
runtimes each pass 164 keyboard-controller cases, 40 radio-provider cases, eight
peer-tree observations and 12 multiple-group/lifecycle cases. The tree checks
verify 12 Menu peers and 18 radio selection-container associations. Negative
audits reject 94, 93 and 94 counterexamples respectively, including the actual
earlier duplicate-label tree. Source and independent consumer builds pass for
x64 and ARM64; every archive entry is byte verified. Both fictional demos pass
66 checks and 577 isolation guards on this build. Existing geometry, colors and
controlled motion checks retain their original tolerances and pass.

These are public in-process peer/provider queries and controlled keyboard
destinations. Routed keys, successful native focus movement, external client
traversal/event delivery, native ARM64 runtime and full component acceptance
remain unverified.

The working surface-motion change retains the source's 20ms entry delay and
150ms exit presence. An owned parent fades using the existing CSS transition
clock: 220ms premium entry and 150ms ease-in exit. The top-left geometry switches
between scale .992 / vertical offset -4 DIP and scale 1 / offset 0. The verified
compiled Tailwind CSS lists opacity and transform as transition properties;
its separate scale/translate properties switch immediately.
Interrupted reopening uses that clock's existing reversal behavior. Reduced
motion snaps to the destination while preserving the presence timers. Caller
surface opacity bindings and render transforms remain on the original surface.
The earlier `0.1.0-menubar-row-motion-v1-local` delivery's source, independent
NuGet and unpacked catalog hosts each pass 30
states, 28 contracts, 32 controlled root-surface samples and 264 row-color
presentations covering 528 rendered color properties. The source/archive
negative suites reject 72 counterexamples each; the consumer rejects 71. Both
consumer architecture builds pass. The archive's fictional Monitoring and
Editorial demos pass 66 checks and 577 isolation guards.

The row implementation uses 190ms premium background and foreground transitions
for triggers and all four item kinds. The source reference includes 48
headless histories / 288 computed-style samples; reduced native checks verify
the settled endpoint. Native checks preserve live caller brushes, original
foreground/background bindings, original gradient identity, caller templates
and unload/reload observer cleanup. Held 95ms trigger screenshots are retained.
These controlled checks do not prove physical hover routing or normal
real-time/nested motion, scaling, native ARM64 runtime or full acceptance.

The palette uses the established collapsed-presenter pattern. Earlier
zero-sized transparent-palette attempts failed during startup and are retained,
alongside resource-only diagnostics. The qualified package/catalog candidate
is `0.1.0-menubar-peer-metadata-v1-local`; delivery selection and exact hashes
are recorded by the latest checkpoint.

Set `Menus` to original `BleeckerMenubarMenu` objects. Each menu contains caller
entries: `BleeckerMenubarAction`, `BleeckerMenubarCheckbox`,
`BleeckerMenubarRadioGroup`, `BleeckerMenubarSeparator` or recursive
`BleeckerMenubarSubmenu`. Radio groups contain `BleeckerMenubarRadioItem` values.
The immutable entry descriptions retain their original callbacks, icons and
collection references. Rendered buttons receive the original entry as their
data context; the control does not mutate caller checked/radio state.

```csharp
bool guides = false;
void RenderCommands()
{
    commands.Menus = [new BleeckerMenubarMenu("View", [
        new BleeckerMenubarCheckbox("Show guides", guides, requested =>
        {
            guides = requested;
            RenderCommands();
        })
    ])];
}
```

The caller can refuse a request by leaving its value unchanged. The callback
runs once before the root closes. A throwing callback propagates and prevents
the later close, matching the source order. Shortcut strings are display-only;
they never install keyboard accelerators. An action's optional `Icon` can be
caller-owned presentation content, including an original `UIElement`.

`Menus` is a dependency property and supports ordinary WinUI binding.
Observable menu, submenu and radio-option collections update automatically.
Call `Refresh()` after mutating an ordinary collection without a notification.
The active root follows its numeric index, as in the source; keyed trigger
buttons survive replacement of a menu with the same trigger string. Duplicate
trigger strings, duplicate radio values within a group and recursive collection
cycles fail explicitly. A changed entry replaces presentation data without
replacing its row button. Root retirement releases its surface and timers.
Retired row automation providers reject later invokes instead of calling an
obsolete original action.

Within an Orchard modal host, background rows and triggers, including unopened
menus, reject activation while a modal owns input. Menus inside the active
modal remain eligible. A nested modal pauses its owner's menu without replacing
the original rows, and closing the owner immediately revokes its retained exit
subtree. Closing the modal restores original background rows, callbacks and
caller `Menus` bindings. These guards use the existing modal branch/owner
boundary; they add no menu keyboard behavior or routing policy.

The current local preview, `0.1.0-menu-toast-modal-v1-local`, passes the
extended fixture in fresh x64 NuGet and extracted-catalog hosts: 30 states,
28 behavior contracts and 60/61 auditor rejections respectively. Its 1,224
source geometry values remain within the existing 0.02 DIP tolerance (maximum
error 0.019012451171875 DIP). Both source/consumer architecture builds and both
fictional demos pass. Evidence is retained under
`artifacts/current/menu-toast-modal-menubar-package-v1` and
`menu-toast-modal-menubar-catalog-replay-v1`. Physical input, focus and native
ARM64 runtime remain separate unverified requirements.

The default template has optional `PART_Layout` (`Grid`) and `PART_Triggers`
(`StackPanel`) parts. A replacement template can own its appearance completely;
incorrect part types fail explicitly. Replacing a template or unloading the
control retires its popup presentation. Caller menu data and bindings remain
owned by the caller. Four style properties expose the original trigger, row,
root-surface and submenu-surface recipes: `TriggerStyle`, `ItemStyle`,
`SurfaceStyle` and `SubmenuSurfaceStyle`. The corresponding default keys are
`BleeckerMenubarTriggerStyle`, `BleeckerMenubarItemStyle`,
`BleeckerMenubarSurfaceStyle` and `BleeckerMenubarSubmenuSurfaceStyle`.
Root and nested styles currently supply 224px and 192px widths; consumer styles
can own these values. Both use the existing shared overlay surface renderer.

The native presentation joins an owning `BleeckerModalHost` branch when one is
present and otherwise uses a root-constrained WinUI popup. Logical popup scope
keeps menus inside their owning modal's input boundary. Outside pointer presses
close the active root; internal menu/trigger presses remain inside that scope.
Pointer and UI Automation requests preserve their existing focus behavior.
Canvas owns surface measurement and arrangement; placement updates only changed
coordinates. This avoids the native standalone popup layout cycle found when
the initial renderer remeasured its children from `LayoutUpdated`.
The [focused viewport repair](evidence/menubar-viewport-v1/checkpoint.json)
uses the realized menu-surface size: a bottom-edge root opens above its trigger,
a right-edge submenu flips left, and remaining coordinates stay inside the
native viewport where the surface fits. The source browser's overflowing
bottom-right position remains in the reference; this is an intentional Windows
adaptation. The source-built x64 catalog passes Light/Dark hidden placement
checks and visible dark/light RTL nested and deeper pointer actions. The
flipped root's shadow overlays the nearby trigger, so its exact trigger-pixel
probe is omitted; unobscured palette and row geometry checks remain. The
independent NuGet consumer, native ARM64 runtime and broader constrained-size
review remain open.
The [dark bottom-right capture](evidence/menubar-viewport-v1/bottom-right-dark.png)
shows the root opening above its trigger.

The keyboard handler uses the existing menu buttons and caller callbacks.
Left/Right move between top-level menus, with direction reversed in RTL.
Up/Down wrap among enabled items and skip separators; on a top-level trigger
they open its first/last enabled item. Home/End select the first/last destination
in the current level. Forward enters a submenu, backward leaves it, and Escape
closes one submenu level or the root. Enter/Space open menus or request the
original action/checkbox/radio change. A successful request targets the root
trigger; focus is applied only while that button remains current and enabled.
A throwing caller callback retains its existing error and
open-menu behavior. Tab closes the menu and leaves normal Tab traversal active;
the default trigger panel uses `TabFocusNavigation="Once"`.

Keyboard destinations respect disabled entries, caller `IsTabStop`/visibility,
unloaded/retired rows and the current modal input boundary. The custom navigation
controller does not consume Ctrl/Alt/Windows modified keys, except Tab dismissal. Displayed shortcuts
remain labels rather than registered accelerators. A custom Menubar template
owns its own trigger panel's Tab policy. Full routed-key, focus movement and
physical-input acceptance remain open; hidden controller checks cannot prove
these boundaries. The keyboard API signatures and `Once` semantics were checked
against the repository's pinned WinUI 2.3.9 metadata, used by Windows App SDK
2.5.1. See Microsoft's [keyboard interaction guidance](https://learn.microsoft.com/en-us/windows/apps/develop/input/keyboard-interactions).

UI Automation exposes a menu bar and named menu items, with expand/collapse,
toggle and radio selection request patterns as appropriate. Expand/collapse
provides an accessible way to open recursive menus. Disabled native items
reject those requests. Unlike the source's disabled-hover quirk, a disabled
native submenu is blocked by its entry's disabled flag; the source observation
is preserved unchanged in the reference. Menu/Group roles, controlled-peer discovery, selection containers and group
queries are verified in the scoped hosts; external tree/event delivery remains
part of final acceptance.

The metadata implementation gives menu items native Button peers with an
explicit leaf subtree, so labels and decorative icons do not appear as duplicate
controls. Menu content exposes a `Menu` peer; root and submenu triggers identify
their open menu through `GetControlledPeers` (`ControllerFor`). The presentation
keeps its existing popup/modal branch rather than creating another visual menu.

Each original radio group has a zero-padding panel with a `Group` peer and the
single-selection provider pattern. Its items return that group as their selection
container. Selection queries read the rendered caller-owned values; an unmatched
caller value leaves the selection empty. Reorder retains original row objects,
and selection in one group leaves other groups unchanged. Closing, blocked and
removed groups stop exposing active children/selections. Closed menu item
providers reject Invoke, selection and focus requests before the visual fade
finishes. Successful focus movement and external event delivery remain unverified.
The same scoped metadata checks pass in the source, NuGet and catalog archive
hosts. External event delivery remains a separate acceptance boundary.

The radio selection provider keeps its programmatic selection operations
distinct from ordinary invocation. Selecting or adding the current option does
not request another callback. Removing an unselected option does nothing;
removing the current choice is rejected because this API has no clear-value
request. Adding another option while the original group already has a selected
value is rejected. If the caller's value matches no option, Add can request the
chosen value. Select can request a replacement; the caller still accepts or
refuses it. Disabled and unloaded providers reject every operation before these
checks. Ordinary Invoke, including invocation of the selected option, preserves
the original callback and dismissal behavior. This follows the existing Orchard
Radio Group convention and the [SelectionItem provider contract](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/implementing-the-ui-automation-selectionitem-control-pattern).

Presence retains closing surfaces for 150ms and uses the source's 20ms entry
visibility delay. The first native preview changes opacity at these boundaries;
the initial implementation had no surface fade or geometry state change.
The surface and row changes above use the verified 220/150ms and 190ms tokens.
Do not interpret controlled presentation or presence checks as normal-motion
parity. Source font scaling, complete wrapping/placement/raster, custom style
and template lifetime acceptance, physical input/focus and native ARM64 runtime
also remain open.

Earlier preview evidence covered 30 light/dark states, 26 caller/lifecycle
contracts, two presence histories and two standalone-popup histories in both
the source host and fresh `0.1.0-menubar-v2-local` NuGet consumer. Each host
checks 1,224 geometry values against the original source, with maximum error
0.019013 DIP at a 0.02 DIP tolerance, and 32 rendered palette probes. The
source host also opens the actual catalog fixture in both themes and verifies
that all four original styles reach their rendered elements. Source and
consumer state, contract and presence reports match, and all 30 PNG pairs are
byte-identical. These compare the two native hosts; full browser/native raster
parity remains unproven. The source's `font200` frames remain unverified natively.

That earlier source auditor rejected 51 corrupt/incorrect cases; its consumer rejected 50
(catalog style checks belong to the source host). Unmodified earlier native
frames preserve two real counterexamples: an overflowing viewport capture and
an incorrectly blue expanded submenu. Expanded submenu rows now retain their
ordinary item palette; only an open top-level trigger receives the selected
blue/white recipe. The broader public modal-host probe passes 30 cases and
16 catalog-root states, including both fictional demos. The portable suite
passes all 413 tests. These are scoped preview checks, not final acceptance.
Solution and fresh isolated-consumer builds pass for x64 and ARM64. ARM64
runtime remains unverified.

`MenubarsFixture` demonstrates original callbacks, accepted checkbox/radio
requests, nested actions, display hints, empty/disabled bars and RTL with
fictional local content. It is registered in catalog navigation and coverage.
Both the Monitoring and Editorial component demos remain required and intact.
No application service, authentication or product workflow belongs to this
control. Narrator, OS-setting changes and the pointer heartbeat are excluded.
