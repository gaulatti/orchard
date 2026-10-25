# Navigation Menu

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

The current `0.1.0-switch-japanese-v1-local` NuGet library also passes the
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

The current local preview, `0.1.0-navigation-modal-v1-local`, includes both the
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

These results cover scoped component behavior. Seven source fixture kinds have
no equivalent native snapshot yet: individual trigger, direct and child hover,
trigger/child focus, font200 and pressed trigger.
Setting a native visual state for an open/hover combination does not prove
physical pointer input. The empty 14px popup has browser/native corner
antialiasing differences; palette checks use flat interior padding and do not
claim edge parity. Normal CSS motion, physical pointer/keyboard/focus, full
metadata/customization, Windows scaling,
complete visual review and native ARM64 execution remain open. Hidden public API/UIA
tests do not claim those results. No Narrator, OS setting changes, pointer
heartbeat or Actions are part of this work.
