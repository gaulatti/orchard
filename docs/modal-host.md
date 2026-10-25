# Modal root host

`BleeckerModalHost` is the shared root for Orchard modal surfaces. Wrap the
entire content of a window in one host:

```xml
<orchard:BleeckerModalHost>
    <Grid>
        <!-- The application's original content and component declarations. -->
    </Grid>
</orchard:BleeckerModalHost>
```

The host retains `Content`, `ContentTemplate`, `ContentTemplateSelector` and
their caller bindings. Its private background wrapper handles modal pointer
isolation without disabling or rewriting the original controls. A second host
in the same XamlRoot is rejected. Opening outside the host, or with a host that
does not cover the full viewport, fails explicitly. Keep margins, constrained
sizes and transforms inside the content rather than around the host.

The default template has a `Grid` named `PART_Layout`. Replacement templates
must retain that part. The same private overlay plane survives template changes;
unloading releases registrations and the root registry. Native RTL coordinate
mirroring retains the same physical viewport and is supported by that check.
The catalog uses this host at its root and binds its requested theme to the
existing appearance selector. Header remains registered after Footer.

This preview exposes the root content host; modal layer/branch registration is
internal. The caller-controlled [AlertDialog preview](alert-dialog.md) uses it.
Popover now attaches both owned and background popup content to the same private
plane. Owned branches follow their nearest modal, including declarations in
another popup. Background branches retain source z-index 50 and are inert while
a modal is active. Select, Header Select, Data Table Filters and Data Table
Pagination now attach their original popup rows to the same plane, including
declarations nested inside Popover. Logical popup scopes retain the active
focus owner and reject inactive Select actions without rewriting caller values
or bindings. Tooltip and Field Help Tooltip also share this plane. Private
pointer wrappers and the host peer tree follow active popup scopes, including
logical branches declared inside Select. Active help consumes Escape before
its owner; unowned tooltips retain source z-index 180. Other required modal
surfaces and menus remain open integration work. Closing a root releases
background branches as well as modal layers.
The Popover branch fixture covers 32 native ownership/lifecycle cases, including
the actual catalog confirmation flow and transfers into and out of the host.
An independent NuGet consumer verifies the public popup/confirmation flow.

The Select branch gate covers 32 source open layouts, 40 recipe cases and two
actual catalog compositions. A fresh NuGet consumer covers eight public API
cases across all four recipes, including original option invocation and
suspension/resumption around a caller-controlled confirmation. Physical input,
focus and source-focused-option paint remain unverified.

Navigation Menu uses the same active-branch/owner eligibility boundary.
Background native providers and guarded context commands reject activation
while a modal is active; menus inside that modal remain eligible. Nested modal
close restores original link contexts, and owner close immediately revokes its
retained exit subtree. Declaration-lifetime scope subscriptions also restore
unopened links without changing caller enabled bindings. The component-specific
hidden regression covers these boundaries in both themes, including native
Invoke-provider rejection and original-item routing. See
[Navigation Menu](navigation-menu.md) for the current package/runtime evidence;
these software activation checks do not prove physical input or focus routing.

Menubar and Sonner also use that existing hosted branch/owner boundary. Their
component-specific public-provider regressions cover background blocking,
nested-owner restoration, original caller data/bindings and immediate revocation
when the owner closes. Sonner covers ordinary and Link actions while retaining
its normal transparent-exit action semantics. The combined local menu/toast
preview is verified through independent NuGet consumers, the extracted catalog
and both fictional demos; see [Menubar](menubar.md) and [Sonner](sonner.md).

The hidden native fixture checks 30 Light/Dark ownership, binding, sequential
target, metadata and lifecycle cases. It also loads 16 states of the actual
catalog root, including the fictional Monitoring and Editorial demos, and
records six diagnostic PNGs. A closed-root regression explicitly refreshes the
shared disabled-control pointer observer after window close; a detached content
island clears observation rather than dereferencing its missing environment.
The independent gate requires a clean process exit even when an in-process
report says it passed. A retained failed run demonstrates that boundary.

These checks send no native keys or pointer input, activate no window and call
no focus API. Keyboard target plans and peer metadata are inspected directly.
Actual focus cancellation, physical keyboard/pointer delivery, visible startup,
source raster parity, scaling/motion and native ARM64 runtime remain unverified.

`BleeckerModal` also uses this host for arbitrary original content. Its header
and body scroll together; close requests leave caller `IsOpen` bindings intact.
Nested Modal/AlertDialog declarations in the original content share the same
ownership plane. See [Modal](modal.md) for its content, styling and verification
contracts.
