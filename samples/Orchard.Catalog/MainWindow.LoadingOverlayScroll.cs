using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayScrollAsync(string folder)
    {
        var original = Content; var page = new Page(); var root = new Canvas(); page.Content = root;
        var button = new Button { Content = "Original focus", Width = 140, Height = 32 }; Canvas.SetLeft(button, 20); Canvas.SetTop(button, 20); root.Children.Add(button);
        var content = new Canvas { Width = 400, Height = 2400 };
        var anchor = new Grid { Width = 100, Height = 40 }; Canvas.SetLeft(anchor, 71.25); Canvas.SetTop(anchor, 900.5); content.Children.Add(anchor);
        var overlay = new BleeckerLoadingOverlay(); anchor.Children.Add(overlay);
        var scroll = new ScrollViewer { Width = 300, Height = 200, Content = content, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden };
        Canvas.SetLeft(scroll, 30); Canvas.SetTop(scroll, 100); root.Children.Add(scroll);
        var reports = new List<object>(); var failures = new List<string>(); var phase = "initial";
        NativeInputSession? input = null;
        void Observe(object? sender, object args)
        {
            var portals = VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Where(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").ToArray();
            if (portals.Length != 1) { failures.Add(phase + ": portal count " + portals.Length); return; }
            var popup = portals[0]; var portal = (FrameworkElement)popup.Child;
            var at = portal.TransformToVisual(page).TransformPoint(new Point()); var owner = overlay.TransformToVisual(page).TransformPoint(new Point());
            var valid = Math.Abs(at.X) < .05 && Math.Abs(at.Y) < .05 && Math.Abs(portal.ActualWidth - page.XamlRoot.Size.Width) < .05 && Math.Abs(portal.ActualHeight - page.XamlRoot.Size.Height) < .05;
            if (!valid) failures.Add(phase + $": portal {at.X},{at.Y} while owner {owner.X},{owner.Y}");
            reports.Add(new { phase, at, owner, portal.ActualWidth, portal.ActualHeight, viewport = page.XamlRoot.Size, scrollOffset = scroll.VerticalOffset, popup.HorizontalOffset, popup.VerticalOffset, valid });
        }
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "loading-overlay-scroll"); await input.AcquireAsync(button); button.Focus(FocusState.Keyboard);
            overlay.IsVisible = true; await AwaitPresentationAsync(page); Observe(null, new object());
            CompositionTarget.Rendering += Observe;
            foreach (var offset in new[] { 300d, 1000d, 0d, 1600d })
            {
                phase = "scroll-" + offset; Require(scroll.ChangeView(null, offset, null, disableAnimation: true), "Scroll probe did not accept its requested offset."); await AwaitPresentationAsync(page); Observe(null, new object());
            }
            phase = "render-transform"; scroll.RenderTransform = new TranslateTransform { X = 24.75, Y = 17.25 }; await AwaitPresentationAsync(page); Observe(null, new object());
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, "scroll-portal.png"));
        }
        finally
        {
            CompositionTarget.Rendering -= Observe; overlay.IsVisible = false; input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot);
            await File.WriteAllTextAsync(Path.Combine(folder, "loading-overlay-scroll.json"), JsonSerializer.Serialize(new { reports, failures }, new JsonSerializerOptions { WriteIndented = true }));
        }
        Require(failures.Count == 0, "Scrolling moved the viewport portal: " + string.Join("; ", failures.Take(5)));
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "loading-overlay-scroll", environment = RuntimeEnvironment() }));
    }
}
