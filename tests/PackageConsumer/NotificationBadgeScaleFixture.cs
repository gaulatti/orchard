using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class NotificationBadgeScaleFixture
{
    internal static async Task RunAsync(Window window, string output, string hostAssembly)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 600, Height = 420 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 630));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 900, 630));
            window.Activate();
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                stage = theme + "/150-percent";
                root.RequestedTheme = theme;
                var badge = new BleeckerNotificationBadge { Count = 1 };
                badge.Items.Add(new BleeckerNotificationItem
                {
                    Id = "local", Title = "Regional coverage update ready for editorial review",
                    Description = "東京 · São Paulo · القاهرة — a fictional local update with additional detail for the newsroom.",
                    Timestamp = "Updated just now", IsUnread = true
                });
                Canvas.SetLeft(badge, 400); Canvas.SetTop(badge, 40); root.Children.Add(badge);
                await LayoutAsync(root);
                var trigger = (BleeckerIconButton)badge.FindName("Trigger");
                var flyout = (Flyout)trigger.Flyout;
                flyout.ShowAt(trigger);
                var presenter = await PresenterAsync(root, flyout);
                await LayoutAsync(root);
                var visibleText = Descendants<TextBlock>(presenter)
                    .Where(block => block.Visibility == Visibility.Visible && block.ActualWidth > 0 && block.ActualHeight > 0).ToArray();
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 &&
                    trigger.ActualWidth == 40 && trigger.ActualHeight == 40 &&
                    presenter.ActualWidth == 320 && presenter.ActualHeight > 72 &&
                    presenter.ActualHeight <= root.Height && visibleText.Length >= 3,
                    $"Notification Badge flyout did not fit the native 150% host: {theme}, {presenter.ActualWidth}x{presenter.ActualHeight}.");
                foreach (var block in visibleText)
                {
                    var bounds = block.TransformToVisual(presenter).TransformBounds(new Rect(0, 0, block.ActualWidth, block.ActualHeight));
                    Require(!block.IsTextTrimmed && bounds.X >= -.5 && bounds.Y >= -.5 &&
                        bounds.Right <= presenter.ActualWidth + .5 && bounds.Bottom <= presenter.ActualHeight + .5,
                        $"Notification Badge text clipped the native 150% flyout: {theme}, {block.Text}, {bounds}.");
                }
                Require(!flyout.AreOpenCloseAnimationsEnabled,
                    "Notification Badge retained open/close motion under reduced motion.");
                cases.Add(new { theme = theme.ToString(), scale = root.XamlRoot.RasterizationScale,
                    width = presenter.ActualWidth, height = presenter.ActualHeight, textBlocks = visibleText.Length, passed = true });
                flyout.Hide();
                await UntilAsync(() => !flyout.IsOpen);
                root.Children.Clear();
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                librarySha256 = Hash(typeof(BleeckerNotificationBadge).Assembly.Location), hostSha256 = Hash(hostAssembly) }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = false, stage, error = error.ToString() }));
            window.Close(); Environment.Exit(1);
        }
    }

    private static async Task LayoutAsync(Canvas root)
    {
        for (var i = 0; i < 3; i++)
        {
            root.Measure(new Size(root.Width, root.Height));
            root.Arrange(new Rect(0, 0, root.Width, root.Height));
            root.UpdateLayout();
            await Task.Delay(16);
        }
    }

    private static async Task<FlyoutPresenter> PresenterAsync(Canvas root, Flyout flyout)
    {
        FlyoutPresenter? presenter = null;
        await UntilAsync(() =>
        {
            presenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot)
                .Select(popup => popup.Child).OfType<FlyoutPresenter>().FirstOrDefault();
            return flyout.IsOpen && presenter is { IsLoaded: true, ActualWidth: > 0, ActualHeight: > 0 };
        });
        return presenter!;
    }

    private static async Task UntilAsync(Func<bool> ready)
    {
        for (var i = 0; i < 300; i++)
        {
            if (ready()) return;
            await Task.Delay(16);
        }
        throw new TimeoutException("Notification Badge flyout did not reach its expected native state.");
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

    private static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
