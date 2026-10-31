# Select and HeaderSelect

`BleeckerSelect` and `BleeckerHeaderSelect` are accepted for the five-check x64
milestone in preview `.69`: Light/Dark states, physical keyboard/pointer/focus
and caller requests, four native 150% open-menu layouts per recipe without
clipping, reduced-motion focus-ring endpoints, and independent package-consumer
compilation/runtime pass. ARM64 is build-only and runtime-unverified. Older
full-parity and platform-gap notes below are historical, not extra gates for
the current milestone.

`BleeckerSelect` is a reusable native choice menu over original
`BleeckerSelectOption` objects. It shares the existing table choosers' input,
scrolling, typeahead and selection surface. `BleeckerHeaderSelect` supplies the
compact header recipe: Small size, intrinsic width between 144 and 208 DIP,
at least 176 DIP of menu width, and an injected placeholder option when the
caller has no empty-valued option. Both are component-library APIs; no service
or application selection model is introduced.

```xml
<orchard:BleeckerSelect Options="{Binding Libraries}"
                       Value="{Binding Library, Mode=OneWay}"
                       ValueRequested="ChooseLibrary"
                       Placeholder="Choose a library" />
```

The consumer handles `ValueRequested` and updates its original bound model to
accept a controlled request. The event contains the requested value and option;
the original `Value` binding and original option objects are retained. Header's
injected empty choice is owned by the component. Caller options, their labels,
values and disabled flags remain unchanged. Observable collection and item
notifications update the existing view. A bound options sequence and value must
resolve before mounting.

The original source has a specific empty-value distinction. With an empty
option, an empty prop selects that option. Without an empty option, an empty
ordinary Select prop makes the underlying root uncontrolled: it retains the
last internally requested key while leaving the caller's prop empty. Temporary
nonempty controlled values do not erase that history. HeaderSelect's injected
empty option keeps its empty state controlled. Unknown nonempty values display
an empty label. Empty and literal `__bleecker_empty_option__` keys share the
source's internal sentinel, including its concatenated duplicate labels and
decoded empty requests. These behaviors are deliberate parity requirements.

The sizes use 36/40/44 DIP triggers, 13/14/15 DIP text, and 32/36/40 DIP option
minimums. The optional `StartIcon` and templates retain original caller content;
HeaderSelect exposes `Icon` aliases. Error and required state expose native form
metadata. Name, help, identity, label and description metadata come from the
caller-facing control. Requested autofocus applies at mount when its host is
visible, and does not activate a window. The native menu owns opening, Escape,
keyboard navigation, typeahead, scroll controls and selection automation.

Within `BleeckerModalHost`, Select and HeaderSelect use the shared portal plane
instead of a native Popup input site. The original popup border, option rows,
scroll chrome and producer callbacks stay intact. A private canvas catches the
first outside press; an active scope retains focus without overwriting caller
enabled state. The same surface supplies table-filter and pagination choosers.
Menus owned by an inactive modal retain their presentation and become inert;
queued stale selection requests are rejected. A Select inside a Popover keeps
its logical relationship with that parent across portal boundaries.

The background native fixture matches all 32 frozen open-menu positions and
rows (2,400 geometry values, zero errors), exercises 40 ownership/producer/live
update/lifecycle cases across the four recipes, and verifies two actual nested
catalog flows. The catalog's fictional choice panel demonstrates Select within
Popover and a newer confirmation. These checks invoke ten native option peers
in software and never call focus APIs, activate a window or open a native Popup.
Initial focused-option paint, physical pointer/keyboard/focus delivery, complete
open raster, scaling/motion and ARM64 runtime acceptance remain unverified.

A fresh x64 NuGet consumer independently verifies all four public recipes in
eight Light/Dark cases with 16 software native option invokes. It checks inert
menu rejection under a newer public AlertDialog, producer requests after
resumption, caller value/binding retention, preservation of the parent Popover,
and disable/unload cleanup. Both x64 and ARM64 consumer builds pass; this does
not establish normal visible startup or native ARM64 runtime behavior.

Acceptance requires the frozen 292 closed and 32 open source states, 92 complete
PNGs, original value/request history, retained option/icon identity, metadata,
all three sizes, default and header widths, themes, disabled/error states,
keyboard/pointer/focus, live collection updates, real popup placement/scroll,
motion, scaling and package/platform runtime. Background checks must never open
a native Popup input site or focus an invisible host. Shared-plane layout is
verified without either action. Four source autofocus states require later
visible focus/presentation checks. The new APIs remain previews while native
verification proceeds. Both required composition demos remain in scope.

The earlier independent x64 package baseline matches all 292 closed layouts
with zero geometry error, 288 normal paint cases and eight request histories.
Its 58 checks include original producer ownership, metadata, templates/lifecycle,
and the existing table-filter and pagination recipes. All 48 damaged receipts
are rejected. That historical checkpoint used 248 portable and eight windowless
font tests. The shared-plane fixture above now verifies geometry for all 32
open-source states. Focused presentation/input/raster comparison remains open.
The user has since authorized unattended desktop component tests. A focused
[source x64 foreground checkpoint](evidence/select-foreground-source-v1/checkpoint.json)
now exercises Select and Header Select through physical Windows input in both
themes. Pointer opening of Select focuses its selected original option; a
pointer choice requests `b` from the fictional caller, which accepts the value,
closes the menu and restores trigger focus. Keyboard Enter opens Header Select
with the placeholder focused; Down focuses the first caller-owned option, Enter
requests and accepts `a`, and Escape dismisses a reopened menu without changing
the value. Four settled open-menu captures were visually checked for the focused
row, checkmark, and legible light/dark surface. This is focused x64 source-host
evidence, not complete source-raster, input-matrix, scaling, motion, package or
native ARM64 acceptance. Current package/build evidence is recorded in the
latest component checkpoint rather than inferred from this older baseline.

The later [v7 local preview](releases/2026-10-27-components-v7-local.md) repeats
this focused Light/Dark route from the extracted x64 catalog with the same
Orchard DLL as the independently restored NuGet package. It does not close the
broader input, scaling, motion or native ARM64 runtime gaps.

The [expanded x64 foreground check](evidence/select-foreground-expanded-v1/checkpoint.json)
also passes native Space, Home, Tab, End, Up, Enter and Escape on ordinary Select,
and pointer opening and choice on Header Select, in both Light and Dark. The
original caller accepts each choice and focus returns to the trigger. Four
settled open-menu captures show readable focused rows and disabled options.
This check uses the current source-built catalog at 100% display/text scale
with animations disabled. A subsequent [typeahead and outside-dismissal
check](evidence/select-foreground-typeahead-v1/checkpoint.json) passes physical
prefix cycling in closed and open Select, caller value requests, focus-only
navigation, and outside-click dismissal in both themes. The first attempt used
`B` for a label beginning `Local`; that fixture input was corrected to `L`.
No library behavior changed in that check.

The [focused Header Select input check](evidence/select-header-typeahead-v1/checkpoint.json)
now passes physical closed and open typeahead plus outside-click dismissal in
the source-built x64 catalog, in Light and Dark. Repeated `L` first requests
the injected `Local suite` placeholder after Beta, then cycles to the caller's
Alpha option; open-menu `L` focuses Beta without changing Alpha. The two
focused-row captures are readable, but source-paint parity, independent
package input, scaling, motion and native ARM64 runtime remain unverified.

A fresh [extracted v7 catalog foreground pass](evidence/select-package-foreground-v1/checkpoint.json)
uses the Orchard DLL byte-matched to that preview's independently restored
NuGet consumer. In explicit Light and Dark appearance, native pointer opening,
prefix cycling while open and closed, Enter delivery to the fictional caller,
and outside dismissal preserve the observed value and status. The Select
implementation and its theme files are unchanged since v7. This establishes
those physical routes for that package-matched catalog, but not foreground
input in the separate consumer or the latest package build.

The [latest independent NuGet-consumer foreground check](evidence/select-latest-package-consumer-foreground-v1/checkpoint.json)
closes that specific gap for x64. The consumer restores the current local
package and runs its byte-matched Orchard DLL. In explicit Light and Dark,
physical pointer choice reaches each caller-owned `ValueRequested` handler
for Select and Header Select. Closed Select prefix input cycles the accepted
value; open prefix input only focuses the next option, and an outside click
dismisses without changing the caller's value. The normal consumer preview
also opens. Complete input, focused-option source paint, full open raster,
scaling, motion and native ARM64 runtime checks remain open.

The [published v7 package Header Select input check](evidence/select-v7-package-header-input-v1/checkpoint.json)
also passes physical pointer choice, closed placeholder-to-Alpha typeahead,
open focus-only typeahead, and outside dismissal in the independently restored
x64 consumer, in both themes. The value and caller status agree after each
choice. The focused Beta row is visible in the inspected captures, but its
exact UI Automation focus identity was not established; complete input, source
paint parity, scaling, motion and native ARM64 runtime remain open.

Worker-thread producer notifications are marshaled before any XAML state read.
See [the producer notification contract](producer-notifications.md) for the
shared catalog/package regression and its background-only verification limits.
