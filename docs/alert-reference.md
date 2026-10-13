# Alert reference and acceptance

Run `38465239245` at `c8a830c` fails its ARM64 Light/Info screen capture after
the runner's WSL console takes foreground. The retained desktop report shows
the pointer still over the catalog's own visible, topmost window. Capture now
uses the existing single hosted foreground guard immediately beforehand; it
does not recover an Orchard-owned popup/window or a held pointer action.
Diagnostic capture errors are recorded separately and preserve the original
failure. The hosted boundary still requires fresh acceptance.
The focused x64 Alert check passes both appearances at
`artifacts/smoke-alert-capture-guard` with animations off and scale 1.
Both architecture builds have zero warnings.

The frozen Bleecker alert is a transient notification overlay. Its source is
[`alert.tsx`](https://github.com/gaulatti/bleecker/blob/a86059013cd59684ce452849278ea81e734f2066/src/components/alert.tsx)
at `a86059013cd59684ce452849278ea81e734f2066`, SHA-256
`03f13d8e268535cd89c08ea44ee46d99177acfc0c6a372ad6cec0f9f4a1a336e`.
The downloaded source was verified against the inventory hash. `BleeckerAlert`
and `BleeckerAlertContainer` are implemented as an in-progress native preview;
full acceptance remains open.

## Native use

```xml
<orchard:BleeckerAlert x:Name="Notice" Message="Fictional coverage note saved."
                      Type="Success" Duration="0" IsOpen="False" />
<orchard:BleeckerAlertContainer x:Name="Notifications" />
```

Set `Notice.IsOpen = true` to open the standalone overlay. Its `Closed` event is
the consumer close callback. `Duration` accepts finite milliseconds, including
fractions; zero/negative values persist. The default is 5000ms. `MaxWidth` is
448 by default and can constrain the card. The host needs a loaded native
window; its popup uses client coordinates, independent of its declaration's
layout origin, and does not take focus when opening.

The nonvisual declaration participates in tab order only while open, redirecting
focus to the native close button through `GettingFocus`. Tab/Shift+Tab leave the
popup through its declaration's position in the owning tree. The declaration is
raw automation detail; the popup's message and close button remain accessible.
Opening does not invoke that focus path. Actual Tab entry/exit and Space closure
are required runtime checks; a programmatic focus call alone cannot accept them.

Call `BleeckerAlertContainer.ShowAlert(Notifications.XamlRoot, message, type)`
on that window's UI thread for the shared source contract. Type defaults to
Info and shared alerts use the reference lifetime. Each XAML root corresponds
to the source's document/window scope. As in the pinned subscription behavior,
mounting a container does not replay state; the next publication/removal
publishes the full snapshot, including messages published while unmounted.
Removing one message retains the other controls' identities.

Unloading stops timers/motion, closes the owned popup and unsubscribes the host.
Reloading a standalone alert whose `IsOpen` remains true reopens it. Replacing
its template during exit preserves the pending close callback. Repeated close
is idempotent. These are explicit native lifecycle contracts. No networking or
product notification data enters the library.

The registered Alerts page includes controls for all four types, message,
duration, standalone close and shared publication. Its focused runtime check is
`scripts/smoke-catalog.ps1 -Component alerts`.

## Observable contract

- Render success, error, warning and info notifications; info is the default.
  The consumer supplies the message and close callback.
- Start the visible entry on the next frame. The default lifetime is 5000ms;
  zero or negative duration disables automatic dismissal. A duration change
  replaces the lifetime timer. Closing fades/translates the surface out and
  calls the consumer after 220ms. Unmounting cancels the lifetime timer.
- Center each notification at 96px from the window's client top, with 16px
  horizontal clearance and a 448px maximum width. Multiple alerts occupy the
  same position in insertion order, as in the reference's separate portals.
- Use the shared card radius, overlay shadow, 220ms surface motion and premium
  easing. The hidden surface is translated 4px downward and transparent.
- Use 16px content padding, a 12px gap, a 20px Lucide status icon with 2px top
  margin, and wrapped 14px medium message text with 24px line height. The close
  action has a 16px Lucide X, 4px padding, 6px radius, -4px right/-2px top
  margins, the reference hover wash and 0.9 pressed scale.
- Use the pinned type-specific border/icon palettes in both appearances.
  Every Windows contrast pair must come from system semantic colors.
- Cover standalone notifications and the shared show-alert/container path,
  preserving message identity when another notification is removed and
  unsubscribing containers on unload. Native window ownership is explicit.

## Required verification

The implementation must register all four types, empty/long/multiline/non-Latin
messages, constrained widths, lifetime changes, persistent duration, repeated
close, collection updates, unload/reload and appearance changes. Verify the
actual popup geometry, paint, pointer/keyboard close and accessible message/name
through the running catalog and an independent package consumer. Opening an
alert must preserve the consumer's focus; reduced motion must stop transient
animation without losing the close callback. Native x64/ARM64, rendered reference
comparison, contrast, scaling and Narrator evidence are required before the
inventory can be accepted.

The composed capture exposed a shared `BleeckerOverlaySurface` defect: its
translucent border blended with the shadow because WinUI's Border background
stopped inside the outline. A separate rounded background now fills the border
box, preserving the reference's uniform tint. Position verification samples the
top/bottom border from the actual owned-window capture at 96px; it also exercises
the close action through physical input. Direct in-process peer bounds on this
runtime omit the client-to-screen offset and are retained as diagnostics.
The input harness uses the real native client origin and XAML-root transform.
Border sampling permits only pixel-grid placement and one-channel integer
quantization; it does not establish complete raster/shadow parity.

The close wash uses a per-control brush with the reference 190ms premium spline.
It interpolates alpha while keeping a fading dark-theme white wash white,
matching premultiplied color behavior. Live source color/opacity changes settle
the owned brush, and unloading removes those shared-brush observations.

Exact browser/native raster and shadow comparison, measured close-hover easing, full
enabled/reduced motion evidence, contrast, scaling, disabled states and Narrator
remain open. Shared overlay consumers must pass their existing runtime checks
after the background repair. No full parity claim is made from the preview.

The focused x64 check at `artifacts/smoke-alerts-keyboard-complete` passes both
themes with Windows animations disabled, display/text scale 1. It exercises the
actual 5000ms lifetime, fractional/changed/persistent durations, physical
Tab/Shift+Tab/Space, held/cancelled pointer press, repeated close, template exit
callback, unload/reload, live appearance, 280px width and shared-stream
identity/subscription behavior. Default close callbacks arrive after roughly
5.24 seconds, including the reference 220ms exit delay. This establishes the
registered endpoints/lifecycle, not enabled-motion or full accessibility parity.

The complete ordinary x64 catalog passes at `artifacts/smoke-alerts-all`, covering
the shared surface repair through existing menus, notifications and tooltips in
both appearances. The final premium-wash implementation passes its focused
check at `artifacts/smoke-alerts-premium`, including reference hover opacity/RGB,
held scale and cancelled-press reset. All 26 unit tests and generated-token
checks pass. Native ARM64 runtime and enabled-motion acceptance remain pending.

A fresh independent x64 consumer passes at
`artifacts/package-consumer-alerts-preview`; its 0.1.0 package SHA-256 is
`dd3da0dd8dd87abb006b6cd531a56aaaee46b829b79c00aed789ee6e4f7e4747`.
Both appearances load the packaged Alert template/fonts/resources, invoke the
native close action through UI Automation, deliver the callback and exercise
the shared publisher/container removal path. No source project reference is used.

The final premium implementation also passes a fresh independent consumer at
`artifacts/package-consumer-alerts-premium`, package SHA-256
`f16a7b49368c5428032382fa733ebe99d2b465c79ad64ce6ef83ef4df38f634c`.
The x64 publish and final ARM64 build have zero warnings.

Hosted run [38443310910](https://github.com/gaulatti/orchard/actions/runs/38443310910)
at `770546f` passes the entire x64 catalog, real Windows contrast transition
and independent consumer. Native ARM64 reaches and passes Alert's registered
geometry, palette, input and lifecycle checks in **both** appearances, with
animations enabled and display/text scale 1. The retained default lifetime
measurements are 5238.6942ms (Light) and 5224.3687ms (Dark). Its later Dark
Checkbox cursor check fails, so the overall ARM64 catalog is not green.
The separate ARM64 contrast and independent consumer steps pass; the consumer
package SHA-256 is
`b3e6d4a5df01896fb9c09b02244deac9958b6ce86b3691bc0031f58b27c46051`.
Runtime archive `11837402947` was downloaded and verified against SHA-256
`1f43addebc5ba7fc588b2a7fd7d19c35ed0d79d77b1c883997b5e5dbadc0fa79`;
selected Alert reports and captures are retained under
`artifacts/ci-770546f-arm64-evidence`. The composed Dark success capture was
inspected. Enabled endpoints and lifecycle do not establish measured motion
curves, complete raster parity, contrast/scaling or Narrator acceptance.

The public wiki remote is unavailable; this contract is maintained in the
repository.
