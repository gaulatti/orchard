using Microsoft.UI.Xaml;
using Windows.System;

namespace Orchard;

public sealed partial class BleeckerMenubar
{
    private static bool NavigationEnabled(MenubarButton button) => button.IsEnabled && button.IsTabStop && button.Visibility == Visibility.Visible;

    private bool OwnsNavigationTarget(MenubarButton button) => button.IsLoaded && NavigationEnabled(button) &&
        (button.Level is { } level ? level.IsOpen && level.NavigationButtons.Contains(button) : roots.Values.Any(root => ReferenceEquals(root.Trigger, button)));

    // Return the native focus destination separately so the controller can be
    // exercised in a hidden host without moving the user's keyboard focus.
    private (bool Handled, MenubarButton? Target) NavigateMenu(MenubarButton button, VirtualKey key)
    {
        if (!InputAllowed || !OwnsNavigationTarget(button)) return (false, null);
        var level = button.Level;
        var rootButton = button;
        for (var parent = level; parent is not null; parent = rootButton.Level) rootButton = parent.Anchor;
        var root = roots.Values.Single(candidate => ReferenceEquals(candidate.Trigger, rootButton));
        var ordered = roots.Values.OrderBy(candidate => candidate.Index).Where(candidate => NavigationEnabled(candidate.Trigger)).ToArray();
        MenubarButton? Edge(MenuLevel? menu, bool last) => last ? menu?.NavigationButtons.LastOrDefault() : menu?.NavigationButtons.FirstOrDefault();
        MenubarButton OpenRoot(RootMenu menu, bool last = false)
        {
            activeIndex = menu.Index; Render();
            return Edge(menu.Level, last) ?? menu.Trigger;
        }
        MenubarButton? ChangeRoot(int direction, bool edge = false)
        {
            if (ordered.Length == 0) return null;
            var index = edge ? (direction < 0 ? 0 : ordered.Length - 1) : (Array.IndexOf(ordered, root) + direction + ordered.Length) % ordered.Length;
            var next = ordered[index];
            if (activeIndex is not null) { var first = OpenRoot(next); return level is null ? next.Trigger : first; }
            return next.Trigger;
        }
        MenubarButton OpenChild()
        {
            button.Expand?.Invoke();
            return Edge(level?.ChildOf(button), false) ?? button;
        }
        switch (key)
        {
            case VirtualKey.Tab:
                if (activeIndex is null) return (false, null);
                Close(); return (false, rootButton);
            case VirtualKey.Escape:
                if (level is not null && !level.Anchor.IsRootTrigger) { level.Anchor.Collapse?.Invoke(); return (true, level.Anchor); }
                if (activeIndex is null) return (false, null);
                Close(); return (true, rootButton);
            case VirtualKey.Home:
            case VirtualKey.End:
                return (true, level is null ? ChangeRoot(key == VirtualKey.Home ? -1 : 1, true) : Edge(level, key == VirtualKey.End) ?? button);
            case VirtualKey.Up:
            case VirtualKey.Down:
                if (level is null) return (true, OpenRoot(root, key == VirtualKey.Up));
                var buttons = level.NavigationButtons;
                var offset = key == VirtualKey.Up ? -1 : 1;
                return (true, buttons[(Array.IndexOf(buttons, button) + offset + buttons.Length) % buttons.Length]);
            case VirtualKey.Left:
            case VirtualKey.Right:
                var forward = key == (FlowDirection == FlowDirection.RightToLeft ? VirtualKey.Left : VirtualKey.Right);
                if (level is not null && forward && button.HasSubmenu) return (true, OpenChild());
                if (level is not null && !forward && !level.Anchor.IsRootTrigger) { level.Anchor.Collapse?.Invoke(); return (true, level.Anchor); }
                return (true, ChangeRoot(forward ? 1 : -1));
            case VirtualKey.Enter:
            case VirtualKey.Space:
                if (level is null) return (true, OpenRoot(root));
                if (button.HasSubmenu) return (true, OpenChild());
                button.Request?.Invoke(); return (true, rootButton);
            default:
                return (false, null);
        }
    }

    private void FocusMenuTarget(MenubarButton target)
    {
        void Focus()
        {
            if (!InputAllowed) return;
            if (!target.IsLoaded)
            {
                RoutedEventHandler? loaded = null;
                loaded = (_, _) => { target.Loaded -= loaded; DispatcherQueue.TryEnqueue(Focus); };
                target.Loaded += loaded;
                return;
            }
            if (OwnsNavigationTarget(target)) target.Focus(FocusState.Keyboard);
        }
        DispatcherQueue.TryEnqueue(Focus);
    }
}
