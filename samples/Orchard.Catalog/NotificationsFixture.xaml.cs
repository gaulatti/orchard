using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class NotificationsFixture : UserControl
{
    private int selected;
    private int marked;
    public NotificationsFixture()
    {
        InitializeComponent();
        var popupContent = (UIElement)((Flyout)((BleeckerIconButton)Badge.FindName("Trigger")).Flyout).Content;
        popupContent.GotFocus += (_, _) => UpdateInputTrace("focus");
        popupContent.AddHandler(UIElement.KeyDownEvent, new KeyEventHandler((_, args) => UpdateInputTrace(args.Key.ToString())), true);
        Badge.MarkAllReadCommand = new ActionCommand(_ =>
        {
            marked++;
            Badge.Count = 0;
            foreach (var item in Badge.Items) item.IsUnread = false;
            Result.Text = $"Marked read: {marked}";
        });
        Empty.MarkAllReadCommand = Badge.MarkAllReadCommand;
        foreach (var item in Badge.Items)
        {
            item.CommandParameter = item.Id;
            item.Command = new ActionCommand(parameter => { selected++; Result.Text = $"Selected notification {parameter}: {selected}"; });
            Overflow.Items.Add(item);
        }
    }

    private void UpdateInputTrace(string action)
    {
        var focused = FocusManager.GetFocusedElement(XamlRoot) as FrameworkElement;
        var name = focused is null ? "none" : FrameworkElementAutomationPeer.CreatePeerForElement(focused)?.GetName();
        InputTrace.Text = $"Keyboard evidence: {action}; focus: {name ?? focused?.GetType().Name ?? "none"}";
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Badge.Count = 3;
        foreach (var item in Badge.Items) item.IsUnread = item.Id != "3";
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await Task.Delay(100);
        var trigger = Descendants<BleeckerIconButton>(Badge).Single();
        var counter = (Border)Badge.FindName("CountBadge");
        Require(trigger.ActualWidth == 40 && trigger.ActualHeight == 40 && counter.ActualHeight == 16 && counter.ActualWidth == 16,
            "Notification trigger/count geometry differs from the reference.");
        Require(((TextBlock)Overflow.FindName("CountText")).Text == "99+" && ((Border)Empty.FindName("CountBadge")).Visibility == Visibility.Collapsed,
            "Notification count cap or empty visibility failed.");
        var popup = (Flyout)trigger.Flyout;
        await OpenAsync(trigger, popup);
        var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(item => item.Child).OfType<FlyoutPresenter>().Single();
        var stack = (StackPanel)popup.Content;
        var header = (Grid)stack.Children[0];
        var mark = (Button)header.Children[1];
        var items = (ItemsControl)stack.Children[3];
        Require(!items.IsTabStop, "Notification list container must not add an empty Tab stop.");
        var rows = Descendants<Button>(items).ToArray();
        var separator = (MenuFlyoutSeparator)stack.Children[1];
        Require(!separator.IsTabStop && !separator.AllowFocusOnInteraction && !separator.Focus(FocusState.Keyboard),
            "Notification separator must not trap keyboard focus outside a native menu presenter.");
        Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), mark),
            "Notification popup did not initially focus the header action.");
        Require(presenter.ActualWidth == 320 && presenter.ActualHeight == 262 && header.ActualHeight == 39 && rows.Length == 3,
            $"Notification popup/header/list geometry mismatch: {presenter.ActualWidth}/{header.ActualHeight}/{rows.Length}.");
        Require(rows.Select(row => row.ActualHeight).SequenceEqual(new double[] { 72, 72, 56 }),
            $"Notification row heights differ: {string.Join(',', rows.Select(row => row.ActualHeight))}.");
        Require(mark.Visibility == Visibility.Visible && ((TextBlock)stack.Children[2]).Visibility == Visibility.Collapsed,
            "Notification populated/header state mismatch.");
        Require(new ButtonAutomationPeer(rows[0]).GetName() == "New order Order #1234 received 2m ago",
            "Notification action omitted its description or timestamp from accessibility.");
        await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"notifications-{scheme.ToString().ToLowerInvariant()}.png"));
        var root = (FrameworkElement)XamlRoot.Content;
        var originalTheme = root.RequestedTheme;
        var changedScheme = scheme == BleeckerColorScheme.Light ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light;
        try
        {
            root.RequestedTheme = changedScheme == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            await Task.Delay(150);
            VerifyPalette(presenter, rows[0], counter, changedScheme);
            Require(popup.IsOpen && presenter.ActualWidth == 320, "Live theme change dismissed or resized notifications.");
            await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"notifications-live-{changedScheme.ToString().ToLowerInvariant()}.png"));
        }
        finally { root.RequestedTheme = originalTheme; }
        await Task.Delay(150);
        VerifyPalette(presenter, rows[0], counter, scheme);
        Require(mark.Focus(FocusState.Keyboard), "Notification header did not accept keyboard focus.");
        Require(rows[0].Focus(FocusState.Keyboard), "Notification row did not accept keyboard focus.");
        Require(Descendants<Border>(rows[0]).Single(border => border.Name == "FocusHighlight").Opacity == 1,
            "Keyboard-focused notification lost its branded highlight.");
        var firstCommand = (ActionCommand)Badge.Items[0].Command!;
        firstCommand.Enabled = false;
        await Task.Delay(100);
        Require(!rows[0].IsEnabled, "Notification ignored consumer CanExecuteChanged.");
        Require(!rows[0].Focus(FocusState.Keyboard), "A disabled notification accepted keyboard focus.");
        firstCommand.Enabled = true;
        await Task.Delay(100);
        Require(rows[0].IsEnabled, "Notification did not restore consumer command availability.");
        rows[0].Focus(FocusState.Keyboard);
        await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"notifications-focus-{scheme.ToString().ToLowerInvariant()}.png"));
        var beforeMarked = marked;
        ((IInvokeProvider)new ButtonAutomationPeer(mark).GetPattern(PatternInterface.Invoke)).Invoke();
        await Task.Delay(100);
        Require(marked == beforeMarked + 1 && popup.IsOpen && Badge.Count == 0 && Badge.Items.All(item => !item.IsUnread) && mark.Visibility == Visibility.Collapsed,
            "Mark-all command did not update consumer state while retaining the popup.");
        Require(Descendants<Microsoft.UI.Xaml.Shapes.Ellipse>(items).All(dot => dot.Visibility == Visibility.Collapsed),
            "Live unread state did not reach rendered notification dots.");
        var beforeSelected = selected;
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnClosed(object? sender, object args) => closed.TrySetResult();
        popup.Closed += OnClosed;
        try
        {
            ((IInvokeProvider)new ButtonAutomationPeer(rows[0]).GetPattern(PatternInterface.Invoke)).Invoke();
            await closed.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Require(selected == beforeSelected + 1 && !popup.IsOpen && Result.Text.StartsWith("Selected notification 1:", StringComparison.Ordinal),
                "Notification selection did not invoke its command/parameter and dismiss.");
        }
        finally { popup.Closed -= OnClosed; }
        var emptyTrigger = Descendants<BleeckerIconButton>(Empty).Single();
        var emptyPopup = (Flyout)emptyTrigger.Flyout;
        await OpenAsync(emptyTrigger, emptyPopup);
        var emptyStack = (StackPanel)emptyPopup.Content;
        Require(((TextBlock)emptyStack.Children[2]).Visibility == Visibility.Visible &&
            ((Button)((Grid)emptyStack.Children[0]).Children[1]).Visibility == Visibility.Collapsed, "Empty notification state is incorrect.");
        presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(item => item.Child).OfType<FlyoutPresenter>().Single();
        await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"notifications-empty-{scheme.ToString().ToLowerInvariant()}.png"));
        Empty.Items = [new BleeckerNotificationItem { Id = "live", Title = "Live collection update", IsUnread = true }];
        await Task.Delay(100);
        Require(((ItemsControl)emptyStack.Children[3]).Items.Count == 1 && ((TextBlock)emptyStack.Children[2]).Visibility == Visibility.Collapsed &&
            ((Button)((Grid)emptyStack.Children[0]).Children[1]).Visibility == Visibility.Visible,
            "Replacing the consumer collection did not update the open notification list.");
        Empty.Items.Clear();
        Require(((TextBlock)emptyStack.Children[2]).Visibility == Visibility.Visible && ((Border)Empty.FindName("CountBadge")).Visibility == Visibility.Collapsed,
            "Clearing items changed the independent count or failed to restore the empty state.");
        emptyPopup.Hide();
        await Task.Delay(150);
        await VerifyLongContentAsync(scheme, folder);
    }

    private static void VerifyPalette(FlyoutPresenter presenter, Button row, Border counter, BleeckerColorScheme scheme)
    {
        var colors = BleeckerTokens.Colors(scheme);
        var surface = Descendants<BleeckerOverlaySurface>(presenter).Single();
        Require(BrushColor(surface.Background) == colors[scheme == BleeckerColorScheme.Dark ? "deepSea" : "card"],
            "Open notification surface retained its previous theme.");
        var text = Descendants<TextBlock>(row).Where(block => block.Visibility == Visibility.Visible && block.ActualHeight > 0).ToArray();
        Require(BrushColor(text[0].Foreground) == colors["textPrimary"] && BrushColor(text[1].Foreground) == colors["textSecondary"],
            $"Open notification text palette mismatch: {BrushColor(text[0].Foreground)}/{BrushColor(text[1].Foreground)} for {scheme}.");
        Require(BrushColor(Descendants<Microsoft.UI.Xaml.Shapes.Ellipse>(row).Single().Fill) == colors["sea"] &&
            BrushColor(counter.Background) == colors["terracotta"], "Notification dot or counter retained its previous theme.");
    }

    private static string BrushColor(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private async Task VerifyLongContentAsync(BleeckerColorScheme scheme, string folder)
    {
        var trigger = (BleeckerIconButton)LongContent.FindName("Trigger");
        var popup = (Flyout)trigger.Flyout;
        await OpenAsync(trigger, popup);
        var presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Select(item => item.Child).OfType<FlyoutPresenter>().Single();
        var row = Descendants<Button>((ItemsControl)((StackPanel)popup.Content).Children[3]).Single();
        Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), row), "A notification without a header action did not focus its first row.");
        var tallHeight = row.ActualHeight;
        await MainWindow.CaptureElementAsync(presenter, Path.Combine(folder, $"notifications-long-{scheme.ToString().ToLowerInvariant()}.png"));
        Require(presenter.ActualWidth == 320 && tallHeight > 72,
            $"Long notification did not wrap inside the reference popup width: popup={presenter.ActualWidth}, row={row.ActualWidth}x{tallHeight}.");
        foreach (var text in Descendants<TextBlock>(row).Where(text => text.ActualHeight > 0))
        {
            var origin = text.TransformToVisual(row).TransformPoint(new Windows.Foundation.Point());
            Require(!text.IsTextTrimmed && origin.X >= 0 && origin.Y >= 0 &&
                origin.X + text.ActualWidth <= row.ActualWidth + 0.1 && origin.Y + text.ActualHeight <= row.ActualHeight + 0.1,
                "Long/non-Latin notification text escaped its row bounds.");
        }
        var item = LongContent.Items[0];
        var originalTitle = item.Title;
        var originalDescription = item.Description;
        item.Title = "Short update";
        item.Description = "Ready";
        await Task.Delay(100);
        Require(row.ActualHeight == 72, "Live notification text update did not reduce the row height.");
        item.Title = originalTitle;
        item.Description = originalDescription;
        await Task.Delay(100);
        Require(row.ActualHeight == tallHeight, "Restoring long text did not restore wrapped layout.");
        popup.Hide();
        await Task.Delay(150);
    }
    private static async Task OpenAsync(Button trigger, Flyout popup)
    {
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnOpened(object? sender, object args) => opened.TrySetResult();
        popup.Opened += OnOpened;
        try
        {
            ((IInvokeProvider)new ButtonAutomationPeer(trigger).GetPattern(PatternInterface.Invoke)).Invoke();
            await opened.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await Task.Delay(100);
        }
        finally { popup.Opened -= OnOpened; }
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class ActionCommand(Action<object?> action) : ICommand
    {
        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set { enabled = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
        }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => enabled;
        public void Execute(object? parameter) => action(parameter);
    }
}
