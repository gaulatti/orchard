# Navigation Menu

The 2026-10-31 `.85` x64 milestone accepts Navigation Menu under the five-check
contract. Light and Dark hidden states, physical keyboard and pointer routing,
focus, four native 150% layout cases, static reduced-motion behavior and
compilation in an independent consumer of the exact NuGet package passed.
Grouped links refresh native command enablement when their popup elements load,
so a pointer click invokes the caller's link after opening the group. ARM64
builds and consumer compilation pass; ARM64 runtime remains unverified.
Historical prototype limitations below describe earlier investigations and do
not add acceptance gates to this milestone.

`BleeckerNavigationMenu` renders original caller links and groups. It is a
component, not a router or browser launcher. `BleeckerNavigationMenuLink`
retains its literal `Href`, `Label`, `Description` and `External` values;
`BleeckerNavigationMenuGroup.Href` is data and never an action. The control
executes the caller's `Command` with `CommandParameter`, then raises
`ItemInvoked` with the original link. Default grouped activation requests
dismissal before calling the host. Direct activation leaves an open group
open. `NavigateUri` is deliberately unset, following the
[WinUI HyperlinkButton contract](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.hyperlinkbutton).
The implementation targets the repository's WindowsAppSDK 2.5.1 / WinUI 2.3.9
packages; their installed XML metadata supplies the version-specific API
contract.

```csharp
navigation.Entries = new IBleeckerNavigationMenuEntry[] {
    new BleeckerNavigationMenuLink("/fictional/home", "Home"),
    new BleeckerNavigationMenuGroup("Components", new[] {
        new BleeckerNavigationMenuLink("/fictional/buttons", "Buttons", "Actions and choices")
    })
};
navigation.ItemInvoked += (_, original) => ShowFictionalDestination(original);
```

`Entries` and group `Items` accept ordinary enumerables or observable
collections. Original link/group dependency properties and collection changes
update the live view; call `Refresh()` after mutating an ordinary collection.
Sibling direct-link hrefs, group triggers and each group's link hrefs must be
unique within their respective keys. A retained href keeps its live custom
element through label/description changes. Open state follows the original
numeric entry index; inserting a preceding entry can therefore hide the open
group without clearing that index. Renaming a trigger creates a new wrapper.

`LinkTemplate` receives `BleeckerNavigationLinkContext`: original `Item`,
prepared `Content`, `Style`, `Foreground`, guarded default `Command`,
`IsGrouped` and optional `CloseCommand`. Direct links receive a null close
command. A custom renderer can own routing and choose whether to request
closure. Its callback failure before requesting closure leaves the group
open. The default command requests closure first. Custom renderers must honor
their context's command eligibility when routing independently; caller-owned
commands cannot be revoked by the library. Retired library commands and
providers reject activation.

Contexts/prepared content exist for closed group items, matching the source's
eager renderer-prop construction; actual native template elements mount only
with their group. A group retains the same subtree during its 150ms exit.
Reopening during that interval cancels removal. After removal, uncontrolled
template-local input resets on remount; data owned by the caller persists.
Default invocation checks current membership, load/visibility/enablement,
host `CanExecute` and active modal ownership.
An active modal blocks background link/trigger providers and guarded context
commands, including menus that have never opened a group. A menu inside the
active modal remains interactive. A nested modal suspends its owner's menu;
closing it restores the original link/context. Closing the owner revokes its
providers immediately, including the retained exit subtree. Scope subscriptions
follow the loaded declaration, so an unopened menu's native command enablement
also recovers after the modal closes. Caller enabled bindings remain intact.
Hiding the declaration or a visual ancestor revokes eligibility immediately
and retires its ported surface. Revealing it leaves the menu closed.

The default root has no surrounding card. Top links use 14/20 text and
12×8 padding; group triggers use 13/19.5 text, a 14px chevron and 4px gap.
The popup uses the existing overlay surface, 256px width, 6px padding and
1px border. Group rows use 12×10 padding, 14/20 titles and optional
12/19.5 descriptions separated by 2px. Popup position follows the physical
left of the group wrapper, four pixels below it, including RTL. The pressed
trigger's one-pixel translation does not move that anchor. A standalone
native Popup is root-constrained. The modal-host branch follows the root's
clipping policy; hidden capture fixtures explicitly clip their viewport.
Neither path adds focus requests, Escape/arrow switching or native menu
flyout behavior absent from the source.

The default `FontSize` is 16. Set it to 32 to reproduce the pinned 200% root
font case: rem-based link text, line height, padding, gaps and popup size
double. The trigger's 13px label and 14px chevron remain pixel-sized. Changes
to `FontSize` update the live menu. Caller `LinkTemplate` elements retain their
own layout; the prepared default content follows the menu's scale.

Override `TopLinkStyle`, `GroupLinkStyle`, `TriggerStyle`, `SurfaceStyle` or
the complete control template. `PART_Entries` is optional; when present it
must be a `Grid`. Replacing/removing the template retires popup ownership.
The registered `NavigationMenusFixture` includes default markup, original
items, all four public styles, a typed custom link template with retained
local input, RTL, disabled and empty states. Both existing fictional
Monitoring and Editorial demos remain in scope and unchanged.

The reference is [the pinned source contract](navigation-menu-reference.md).
The earlier hidden source and fresh NuGet hosts each passed 26 states, 18 behavior
contracts, two presence histories and two standalone-popup histories. The
source host also loads the actual catalog fixture in both themes and checks
all four style identities on rendered native elements. Each host matches 752
source geometry values exactly and passes 40 flat palette probes. All 26
source/package PNG pairs and corresponding contract reports are byte-identical;
57 source and 56 package rejection cases pass. The regression run exposed and
fixed initial command/enablement coercion, missing public resource exports,
overlapping open/hover tint and hidden-declaration ownership boundaries.
Both x64/ARM64 solution builds and isolated package-consumer builds pass; the
portable regression suite passes all 413 tests. The full public-host regression
also loads both fictional Monitoring and Editorial demos. Native ARM64 runtime
has not been exercised.

The earlier `0.1.0-switch-japanese-v1-local` NuGet library also passed the
extended independent x64 consumer fixture: 30 states, 18 behavior contracts,
896 source geometry values with zero error, and 48 flat palette probes. The
four added snapshots cover `custom` and `custom-input` in both themes through
the public `LinkTemplate`, original context content, styles, foreground and
command. Each custom-input state contains five native text editors with the
original text, selection and Edit role. Their 90 by 18 DIP bounds reproduce
the caller fixture's requested layout; native editor paint is not a browser
raster claim. Existing retained-input, callback and remount checks still pass.
The auditor rejects 60 corrupt reports/images, including missing input, changed
text, wrong role and wrong bounds. Evidence is retained under
`artifacts/current/navigation-custom-content-native-v1`.

The earlier local preview, `0.1.0-navigation-modal-v1-local`, included both the
custom-template fixture extension and the modal-isolation repair. Fresh x64
NuGet and extracted-catalog hosts each pass 30 states, 20 behavior contracts,
896 source geometry values with zero error and 48 flat palette probes. The
catalog additionally loads its actual fixture in both themes. Package/catalog
auditors reject 74/75 corrupt reports and images respectively. Both x64/ARM64
library builds and fresh isolated consumer builds pass; both fictional demos
pass 66 checks and 577 guards from the extracted archive. Only the library DLL
changed in the package payload. Evidence is retained under
`artifacts/current/navigation-modal-package-v1`,
`navigation-modal-catalog-replay-v1` and `navigation-modal-demos-v1`.

The regression caught background commands remaining eligible under a public
`BleeckerModal` despite an intact caller enabled binding. The repair reuses the
existing Select Surface host-interaction guard and subscribes for the loaded
declaration's lifetime. An initial unselected candidate blocked input but left
an unopened link disabled after modal close; the current regression requires
that restoration as well as nested-owner, original-item, caller-binding and
immediate closing-state behavior. Historical failures remain recorded. These
software activation checks do not establish physical input or native ARM64
execution.

The current source catalog adds the `font200` snapshot in both themes and
checks a live 16→32 font-size change without replacing keyed link or trigger
elements. Its hidden x64 run passes 32 states, 20 behavior contracts, two
catalog states, 968 source geometry values with zero error and 52 flat palette
probes. The auditor rejects 77 altered reports and frames, including two
200% geometry mutations. The compact retained reports are in
[the font-size checkpoint](evidence/navigation-font200-v1/checkpoint.json). A fresh
`0.1.0-navigation-font200-v1-local` package restored and built in an isolated
x64 consumer. That host passed the same 32 states, 20 contracts and 968 exact
geometry values; its auditor rejected 76 mutations. The source and package
hosts report the same library DLL hash. Native ARM64 execution
remains unverified. The library and fresh isolated package consumer also build
for ARM64.

The popup now fades in over the surface token's 220ms and fades out over the
150ms exit token. Its source translate and scale endpoints switch with the open
state. The animation runs on a library-owned wrapper, preserving the caller's
surface opacity binding and render transform. Interrupted exits fade back in
without replacing the mounted surface; disabling animations settles directly
at the requested endpoint. The source-catalog x64 native run passes 32 states,
22 behavior contracts, 968 exact geometry values and 52 flat palette probes.
Its controlled-clock contract checks entry, exit, interruption, disabled-motion
endpoints and caller-owned properties; the auditor rejects 80 corrupted
reports and images. In the visible catalog, a pointer opened the group and
activated its Buttons link, and keyboard Return activated a focused Home link
while the open group remained visible. These observations are scoped to those
inputs and do not claim a complete focus-navigation matrix. A fresh
`0.1.0-navigation-motion-v1-local` package restored and built in an isolated
x64 consumer. Its native host passes the same 32 states, 22 contracts and 968
exact geometry values, and its auditor rejects 79 corruptions. Source and
consumer report the same Orchard DLL hash. The retained reports are in
[the motion checkpoint](evidence/navigation-motion-v1/checkpoint.json).
The updated library and its fresh isolated package consumer also build for
ARM64; native ARM64 execution remains unverified.

At the earlier prototype checkpoint, six source fixture kinds had no equivalent
native snapshot: individual trigger, direct and child hover, trigger/child focus
and pressed trigger. The empty popup also had browser/native corner
antialiasing differences. Those exact-raster comparisons are outside the .85
milestone gate. The .85 focused physical input, scaling and reduced-motion checks
are recorded above; ARM64 execution remains unverified. No Narrator, OS setting
changes, pointer heartbeat or Actions are part of this work.

The later [consolidated v5 package](releases/2026-10-26-components-v5-local.md)
passed the focused Navigation Menu check in its independent x64 consumer:
32 states, 22 contracts and 968 source geometry values with zero error. Its
[package regression checkpoint](evidence/components-v5-package-regressions-v1/checkpoint.json)
binds the audit to the exact v5 Orchard DLL. The remaining limits above apply.
