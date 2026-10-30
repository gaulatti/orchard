using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class SegmentedControlScaleFixture
{
    internal static async Task RunAsync(Window window, string output)
    {
        var stage = "setup";
        try
        {
            var root = new Canvas { Width = 480, Height = 320 };
            using var island = new DesktopWindowXamlSource();
            island.Initialize(window.AppWindow.Id);
            island.Content = root;
            island.SiteBridge.OverrideScale = 1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(720, 480));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0, 0, 720, 480));
            window.Activate();
            var cases = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            foreach (var mode in new[] { "natural", "narrow", "large", "rtl" })
            foreach (var selected in new[] { "first", "second" })
            {
                stage = $"{theme}/{mode}/{selected}/150-percent";
                root.RequestedTheme = theme;
                var group = new BleeckerSegmentedControl
                {
                    Options = [new("first", "Daily"), new("second", "Weekly coverage"), new("third", "Disabled", true)],
                    SelectedValue = selected
                };
                if (mode == "narrow") group.Width = 200;
                if (mode == "large") group.FontSize = 26;
                if (mode == "rtl") group.FlowDirection = FlowDirection.RightToLeft;
                Canvas.SetLeft(group, 16);
                Canvas.SetTop(group, 16);
                root.Children.Add(group);
                await LayoutAsync(root);
                var buttons = Descendants<Button>(group).Where(button => button.DataContext is BleeckerSegmentOption).ToArray();
                var owner = group.TransformToVisual(root).TransformBounds(new Rect(0, 0, group.ActualWidth, group.ActualHeight));
                Require(Math.Abs(root.XamlRoot.RasterizationScale - 1.5) < .0001 && group.IsLoaded &&
                    group.ActualTheme == theme && buttons.Length == 3 && group.ActualWidth > 0 && group.ActualHeight >= 40 &&
                    owner.X >= 0 && owner.Y >= 0 && owner.Right <= root.Width && owner.Bottom <= root.Height,
                    $"Segmented Control frame clipped at native 150%: {owner}.");
                for (var index = 0; index < buttons.Length; index++)
                {
                    var button = buttons[index];
                    var label = Descendants<TextBlock>(button).Single(text => text.Name == "SegmentLabel");
                    var buttonBounds = button.TransformToVisual(group).TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
                    var labelBounds = label.TransformToVisual(group).TransformBounds(new Rect(0, 0, label.ActualWidth, label.ActualHeight));
                    Require(buttonBounds.X >= -.5 && buttonBounds.Y >= -.5 &&
                        buttonBounds.Right <= group.ActualWidth + .5 && buttonBounds.Bottom <= group.ActualHeight + .5 &&
                        labelBounds.X >= buttonBounds.X - .5 && labelBounds.Y >= buttonBounds.Y - .5 &&
                        labelBounds.Right <= buttonBounds.Right + .5 && labelBounds.Bottom <= buttonBounds.Bottom + .5 &&
                        label.ActualWidth > 0 && label.ActualHeight > 0 && button.IsEnabled == (index != 2),
                        $"Segmented Control option {index} clipped at native 150%: button={buttonBounds}, label={labelBounds}.");
                }
                cases.Add(new { theme = theme.ToString(), mode, selected, scale = root.XamlRoot.RasterizationScale,
                    width = group.ActualWidth, height = group.ActualHeight, passed = true });
                root.Children.Clear();
                await LayoutAsync(root);
            }
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, cases,
                librarySha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(typeof(BleeckerSegmentedControl).Assembly.Location))) }));
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

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T found) yield return found;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            foreach (var child in Descendants<T>(VisualTreeHelper.GetChild(parent, i))) yield return child;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
