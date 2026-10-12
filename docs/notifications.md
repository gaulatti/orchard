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

This is an implementation checkpoint, not complete visual acceptance. Remaining work includes exact shadow rasterization and placement, reference motion, the full pointer/keyboard/disabled and focus-restoration matrix, long/non-Latin content, constrained viewport and text/display scaling, live appearance changes, actual Windows contrast settings and row highlight foregrounds, Narrator and actual ARM64 runtime. The host is Intel x64 and no ARM64 device is available.
