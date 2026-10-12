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
        var rows = Descendants<Button>(items).ToArray();
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
