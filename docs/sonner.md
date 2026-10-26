# Sonner native preview

`BleeckerSonner` is the Windows host for the pinned Bleecker toast contract.
It is a reusable component with fictional local catalog examples, without a
notification service or application workflow. See the [source contract and
acceptance matrix](sonner-reference.md).

```xml
<orchard:BleeckerSonner x:Name="Notifications"
    Position="BottomRight" Duration="4000" />
```

```csharp
var originalAction = new BleeckerSonnerAction("Continue", command, callerParameter);
var originalToast = new BleeckerSonnerToast("Fictional local update")
{
    Description = "Your original content stays visible.",
    Action = originalAction,
    Duration = 0
};
var id = Notifications.Show(originalToast);
// The caller can explicitly remove it without an exit delay.
Notifications.Dismiss(id);
```

Call the APIs on the owning UI dispatcher after the host is loaded. The host's
instance API makes ownership explicit for Windows with multiple windows. The
`BleeckerToast` facade preserves the source's singleton API: `Show`, `Success`,
`Error`, `Warning`, `Info` and `Dismiss` route to the most recently registered
mounted host. Without a registration, additions return an empty string.
Source effect cleanup clears this registration even when an earlier host
unmounts while a later one remains; Orchard does not silently select another
host. Changing a loaded host's default duration registers that host again.

```csharp
BleeckerToast.Success("Fictional save", new BleeckerSonnerOptions
{
    Description = "A caller-owned example.",
    Duration = 0
});
```

Options preserve the difference between an omitted duration and an explicitly
supplied null. Omission captures the host default for that new item; null, NaN,
zero and negative values are persistent. Changing the host default does not
restart existing timers. Positive durations use the source browser timer's
signed 32-bit millisecond conversion, including fractional truncation and
overflow. A current source-built hidden x64 host and an isolated x64 consumer of
the existing `0.1.0-display-metadata-v1-local` package each pass 28 behavior
contracts, including a positive 0.5ms timeout, a 2³²+1000ms duration that wraps
to 1000ms, and an explicit NaN that remains mounted. Their negative auditors
reject 63 and 61 damaged reports respectively. The ARM64 solution compiles with
zero warnings; native ARM64 execution, other extreme values, physical input and
full package integration remain open. The broad consumer run reached a separate
Pagination physical-input check and did not pass, so it is not counted as full
package verification. Focused evidence is in
`artifacts/current/sonner-timer-source-native-v1` and
`artifacts/current/sonner-timer-package-native-v1`.

Actions retain their original `ICommand` and parameter. The action variant uses
`BleeckerButtonVariant`; omission selects Primary, and Link is a separate
underlined text button. `CanExecuteChanged` reaches the native button, including
worker-thread publications queued to the owning dispatcher. Invoking an action
does not implicitly dismiss its toast. Caller exceptions propagate; callers
retain responsibility for their command's error handling.

The dismiss button and lifetime timeout start the 300ms retained interval;
imperative `Dismiss` removes immediately. The first native rendering event
supplies the source's initial frame. If that first frame arrives after an early
timeout or dismiss-button invocation, it may restore visibility until the
already-scheduled removal. Retained actions remain callable while their item
is transparent, subject to ordinary enabled and modal ownership guards.

Six positions use physical left/center/right coordinates, ordered unbounded
stacking, a 22rem-equivalent maximum host width, 16px host insets/padding, and
8px row spacing. The source's mixed pixel/rem sizing is preserved by the draft
layout; `FontSize=16` is its base rem-equivalent size. Both the declaration's
theme and live native button foregrounds reach ported content. The new semantic
border and close-hover brushes are generated from the existing token source.
The shared overlay supplies branded shadows. The initial native motion uses
the source's 200ms opacity/vertical transition; full motion/reversal acceptance
is still open. Reading the OS animation preference does not change it.

The host uses the existing modal-host branch when available, or a root-scoped
non-light-dismiss native popup. Empty space has no hit-test background. Input
remains bound to the declaration's enabled/visible state and active modal scope;
unloading retires presentation, timers, commands and pending frame work. Native
alert groups expose original content and a `Dismiss` button; the Notifications
container has polite live-region metadata. No Narrator or OS setting campaign
is part of this work.

Hosted action and dismiss eligibility follows the actual modal branch frame.
Background toasts are inert while a modal owns input; toasts declared inside
the active modal remain eligible. A nested modal temporarily blocks the owner's
actions, and closing it restores the original toast, action and parameter.
Closing the owner revokes its retained exit subtree immediately. Caller enabled
bindings remain live throughout. This ownership guard does not suppress the
ordinary 300ms retained-toast action behavior when no modal blocks it.

The current local preview, `0.1.0-menu-toast-modal-v1-local`, passes the
extended fixture in fresh x64 NuGet and extracted-catalog hosts: 66 states,
22 behavior contracts and 62/64 auditor rejections respectively, including the
actual failed modal baseline. Both ordinary and Link actions are covered.
Geometry remains within the existing 0.02 DIP tolerance (1,984 values; maximum
error 0.00006103515625 DIP); 430 palette values and 90 flat PNG probes pass.
Both source/consumer architecture builds and both fictional demos pass. Only the
library DLL changed in the package payload. Native ARM64 execution is unverified.
Evidence is retained under `artifacts/current/menu-toast-modal-sonner-package-v1`
and `menu-toast-modal-sonner-catalog-replay-v1`.
Native Button peers report disabled invocation as the SDK-defined
`UIA_E_ELEMENTNOTENABLED` HRESULT; the fixture accepts that exact result or the
managed equivalent and rejects other failures.

`SonnersFixture` registers all four public styles and exercises every variant,
six positions, ordinary/link actions, persistent/long/multilingual content,
twelve-item stacking, immediate dismissal and the global API. It remains a
component demonstration using fictional data. Both Monitoring and Editorial
composition demos remain required.

Earlier verification (the component remains in progress): the x64 build passed with zero
warnings/errors, and all 418 portable tests pass. The hidden source run passes
66 states, 20 behavior checks, two retained lifetimes, two standalone histories
and both actual catalog themes. It verifies 1,984 geometry values (maximum error
0.0000611px), 430 palette values and 90 flat PNG probes, with zero focus or
activation events. The auditor rejects 53 counterexamples, including both
earlier failed native runs and an actual wrong-theme PNG with a recomputed hash.
The earlier action-template and live-theme failures remain retained as evidence.

Rendered text comparison follows CSS normal whitespace because native text
inserts explicit presentation line breaks; original caller objects are checked
separately. Border opacity uses the native float32 representation of the source
alpha. Neither comparison establishes full glyph, icon or shadow raster parity.
The earlier `0.1.0-sonner-v1-local` NuGet passed the same 66 states and 20 checks
through an isolated x64 consumer; all corresponding snapshots and PNGs are
identical. Its auditor rejects 49 damaged cases. The shared host regression
passes 30 cases and 16 catalog root states, including both fictional demos.
The x64 and ARM64 solution builds and isolated package consumers pass. The
package contains the current DLL, 151 matching loose theme files, PRI, six fonts,
two images and README. Physical input,
full raster, motion/reversal, scaling/customization and
native ARM64 runtime remain open; the component is not accepted.

Reproduce the background checks with `scripts/verify-sonner.ps1` using a fresh
output directory, then `scripts/test-sonner-audit.mjs --native <output>
--host-assembly Orchard.Catalog.dll --output <fresh-negative.json>`. For a fresh
NuGet consumer use `PackageConsumer.dll`; historical failed reports are optional
explicit `--failed-baseline <path>` inputs. `scripts/compare-sonner-hosts.mjs`
binds the source/package reports and PNGs to the isolated build's exact package
hash. Forced hover/pressed template states do not prove physical input.

Implementation references for the repository's Windows App SDK 2.5.1 servicing
line: Microsoft's [release-channel mapping](https://learn.microsoft.com/windows/apps/windows-app-sdk/release-channels),
[DispatcherQueueTimer](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.dispatching.dispatcherqueuetimer?view=windows-app-sdk-2.0),
[Rendering event](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.compositiontarget.rendering?view=windows-app-sdk-2.0)
and [live-region property](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.automation.automationproperties.livesetting?view=windows-app-sdk-2.0).
Browser timer conversion follows the [HTML timer contract](https://html.spec.whatwg.org/multipage/timers-and-user-prompts.html#timers)
and [Web IDL integer conversion](https://webidl.spec.whatwg.org/#es-long).
