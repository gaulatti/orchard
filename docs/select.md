# Select and HeaderSelect

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

Worker-thread producer notifications are marshaled before any XAML state read.
See [the producer notification contract](producer-notifications.md) for the
shared catalog/package regression and its background-only verification limits.
