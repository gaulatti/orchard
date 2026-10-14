# Notification badge (in progress)

`BleeckerNotificationBadge` supplies a 40px branded bell trigger, a 16px count badge (capped at `99+`), and a 320px notification popup. Its pinned reference is Bleecker `a86059013cd59684ce452849278ea81e734f2066`, `src/components/notification-badge.tsx`.

```xml
<orchard:BleeckerNotificationBadge Count="1">
    <orchard:BleeckerNotificationItem Id="review" Title="Review ready"
        Description="A fictional local update" Timestamp="2m ago" IsUnread="True" />
</orchard:BleeckerNotificationBadge>
```

Bind or replace `Items` with an `ObservableCollection<BleeckerNotificationItem>`. Item properties are dependency properties and update rendered content while open. Each item accepts `Command` and `CommandParameter`. Set `MarkAllReadCommand` to expose the header action when the count is positive or any item is unread. Commands retain normal `CanExecute` behavior. Consumers own count, unread state, data, timestamps and all application operations. Invoking a notification closes the popup; marking all read leaves it open. Neither operation automatically changes the data or count.

The popup uses a native `Flyout` with branded button rows, allowing the independent header button to participate in normal Tab navigation. Rows expose their title, description and timestamp as their accessible name. This desktop adaptation uses button semantics, rather than placing an interactive button inside a menu label. The surface, typography, borders, corners, focus tint and Lucide bell are explicitly styled. Windows presenter shadows and open/close animations are disabled; the shared Bleecker overlay supplies its two shadow layers. Focus and hover use a single composited tint. The count uses terracotta/white in light and dark, and the paired Windows highlight/highlight-text colors in contrast themes; it no longer hard-codes white against an independently mapped system color.

`NotificationsFixture` covers the reference's populated, empty and overflow-count examples. Published x64 runtime checks in both light and dark exercise trigger invocation, mark-all state updates, row command parameters, dismissal, collection replacement and collection clearing. They assert the 320×262 populated popup, 39px header and 72/72/56px rows against the rendered reference. The popup minimum width is explicit because WinUI otherwise shrank an end-aligned popup to 201px near the viewport edge. Captures are retained under `artifacts/smoke-notifications-final`. The package consumer loads the control and its item collection from the independent NuGet package.

The popup cycles Tab focus within its presenter and focuses its first available action on opening. The expanded runtime fixture checks initial focus, the branded keyboard focus highlight, and disabling a command through `CanExecuteChanged`; a disabled row must reject focus. Re-enabling the command restores the row. Focus captures are retained under `artifacts/smoke-notifications-keyboard`. These managed focus checks do not substitute for physical key input. WinUI 2.3.9 rejects Next/Previous with scoped FindNextElementOptions, so the fixture does not claim to emulate Tab traversal with that API. A regression also traces the count template's theme references to the paired Windows contrast resources.

Physical keyboard testing exposed two unintended stops: `MenuFlyoutSeparator` trapped focus outside a native menu presenter, and `ItemsControl` added a non-actionable stop. Both now explicitly reject Tab focus; the separator also refuses pointer focus. The catalog regression verifies the loaded separator cannot receive focus. The fixture's visible input trace reports native key delivery and focused action names without changing application behavior.

On x64 in dark appearance, physical input verifies one Tab from the header to the first row, Shift+Tab back to the header, reverse wrapping to the last row, and Enter invoking that row's command and dismissing the popup. Evidence is retained at `artifacts/notifications-tab-dark.jpg` and `artifacts/notifications-keyboard-selected.jpg`. The updated light/dark regression captures are under `artifacts/smoke-notifications-tab-final`.

The long-content fixture adds mixed Japanese, Portuguese and Arabic description text under a wrapping English title. The popup now sets its maximum width to 320px as well as its width/minimum: WinUI otherwise expanded it for this content. Runtime checks inspect visible text bounds, restore short and long text while open, and verify first-row focus when no header action is present. The appearance regression changes the catalog root between light and dark while the same popup remains open, checking surface, title/description, dot and count colors in both directions. Captures are retained under `artifacts/smoke-notifications-content-final`. These cases do not establish every script's line-breaking behavior or scaled/constrained-viewport acceptance.

This is an implementation checkpoint, not complete visual acceptance. Remaining work includes exact shadow rasterization and placement, reference motion, the full pointer/keyboard/disabled and focus-restoration matrix, long/non-Latin content, constrained viewport and text/display scaling, live appearance changes, actual Windows contrast settings and row highlight foregrounds, Narrator and the full ARM64 acceptance matrix. The local host is Intel x64; native ARM64 execution is available through the hosted Windows runner.

Catalog readiness checks for live palette changes and long/short text remeasurement now retry the same exact assertions every 16ms for at most one second. This replaces fixed 100/150ms sleeps that intermittently inspected an earlier frame; assertion failures still surface at the deadline. The application behavior and expected colors/heights are unchanged. The combined catalog run at artifacts/smoke-tooltip-activation-ready passes these checks.

Native ARM64 run `38494432792` at `384b54b` fails the open popup's Dark-to-Light
palette change: requested Light, actual/presenter Dark, explicit Default, and
surface `#FF182533` instead of `#FFFFFFFF`. The exact readiness assertion remains
required; a passing local or earlier hosted run does not close this recurrence.
The verified archive and shared checkpoint are recorded in
[runtime evidence](runtime-evidence.md).

The owned native FlyoutPresenter now follows the notification trigger's
ActualTheme on opening and while the popup renders. The presenter is located through
this popup's own content ancestry; Orchard does not select or recolor unrelated
open popups. Queued theme events and an open-only render observer read the
trigger's current value, avoiding an obsolete event value during rapid changes.
Only a changed theme value is assigned. The event-only candidate reproduced the
stale presenter locally even with the owner/trigger/root already Light, so an
event notification alone is not treated as proof of synchronized rendering.
Closing/unloading removes the observer and clears the presenter reference;
reopening resolves the current owner again.

Acceptance for this correction requires the same open popup to update surface,
title/description, unread dot and count colors through inherited theme changes
and a local owner override, preserving width, focus and consumer root/sibling
themes. The catalog repeats six inherited transitions in each appearance and
captures the local override. The independent package consumer exercises
inherited/local/inherited transitions and checks the actual loaded surface and
title. The focused route is `./scripts/smoke-catalog.ps1 -Component notifications`;
the ordinary full catalog still includes the fixture. Fresh native x64/ARM64
runtime and package verification remain required before accepting the fix.

Focused x64 verification passes both appearances at
`artifacts/smoke-notifications-rendered-owner-theme`. The complete local catalog
also passes at `artifacts/smoke-notifications-theme-composed-ink-all`, with
inherited/local palette and focus/geometry checks and retained owner-theme
captures. System animations are disabled and display/text scale is 1 in those
runs. All 28 unit/resource tests and token generation pass.

The independent x64 consumer passes the new open-popup theme assertions at
`artifacts/package-consumer-notifications-owner-theme`, using package 0.1.0
SHA-256 `75345cd7cf57af8d166f96ce8bc5935b6188c033ac9865fe5da572913cd07b08`.
Its new cache was populated for this exact package. After correcting a test's
Color/string comparison, the same isolated source project was republished and
the restored package hash was rechecked before native execution. It contains
no repository project reference or parent repository configuration. Native ARM64
verification of the fix remains pending; CI now runs Notifications directly on
both architectures before the full catalog. No backend or metrics surface is
affected by this native theme synchronization.

Fresh hosted run `38498921210` at `3f4c0e7` passes the focused notification
regression on x64 and native ARM64, including repeated inherited changes, local
owner overrides and focus/width preservation. The independent consumers pass
the new surface/text assertions on both architectures. ARM64 has animations
enabled. This verifies the scoped live-theme correction; the component's full
visual, accessibility, scaling and interaction matrix remains in progress.
The overall required CI run is still failed on cursor and contrast glyph gates,
as recorded in [runtime evidence](runtime-evidence.md).

The catalog opening check now resolves each notification's FlyoutPresenter
through that popup's own content ancestry, matching the control's ownership
boundary. It no longer selects a presenter from an aggregate of all open
popups after a fixed 100ms sleep. Opened and owned-presenter readiness share
the existing five-second deadline; early dismissal fails explicitly. Geometry,
focus, count, commands, palette, text bounds and empty-state assertions retain
their expected values.

The fixture acquires its own foreground window and checks foreground ownership
before invoking actions. `NotificationPopupTrace` records opening/opened/closing/
closed and owner loaded/unloaded events, current count/unread state, focus type,
content ancestry and foreground ownership. It observes events without cancelling
close or reopening a dismissed popup. Evidence is saved on both successful and
failed runs as `notifications-{scheme}-lifecycle.json`.

The focused local x64 check passes both themes at
`artifacts/smoke-notifications-owned-presenter`. Both solution builds are clean
and all 38 regressions pass at `artifacts/tests-notification-presenter`.
This checkpoint changes catalog verification only; the NuGet library and
documented consumer behavior are unchanged. No backend or metrics surface is
affected. Hosted ARM64 recurrence and full component acceptance remain open.
The complete local x64 catalog also passes both themes at
`artifacts/smoke-notifications-owned-all`, including all three owned popup
open/close cycles and the other mounted controls. Local animations are disabled,
display/text scale is 1 and contrast is off. The public wiki remains unavailable;
this contract and the runtime evidence are maintained in the repository.
