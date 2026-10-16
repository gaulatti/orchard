using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    // An isolated paint diagnostic. It neither changes the product palette nor
    // enters the measured full catalog run.
    private async Task RunBrushAlphaProbeAsync(string folder)
    {
        var reports = new List<object>();
        var host = new Canvas { Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) };
        Grid.SetRowSpan(host, CatalogRoot.RowDefinitions.Count);
        Grid.SetColumnSpan(host, CatalogRoot.ColumnDefinitions.Count);
        CatalogRoot.Children.Add(host);
        try
        {
            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1024, 745));
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                CatalogRoot.RequestedTheme = theme;
                host.Children.Clear();
                var palette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Buttons/Ghost.g.xaml") };
                var themed = (ResourceDictionary)palette.ThemeDictionaries[theme == ElementTheme.Light ? "Light" : "Default"];
                var original = (SolidColorBrush)themed["ButtonBackgroundPointerOver"];
                // The legacy source recipe is deliberately independent of the
                // production brush's representation, so the negative survives
                // a palette correction. The browser collector checks its alpha.
                var legacy = new SolidColorBrush(Color.FromArgb(255, original.Color.R, original.Color.G, original.Color.B)) { Opacity = theme == ElementTheme.Light ? .12 : .06 };
                var alpha = legacy.Opacity;
                var rounded = (byte)Math.Round(alpha * 255, MidpointRounding.AwayFromZero);
                var floor = (byte)Math.Floor(alpha * 255);
                var color = legacy.Color;
                var background = theme == ElementTheme.Light ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 13, 24, 33);
                var backgrounds = new[] { background, Color.FromArgb(255, 230, 213, 184), Color.FromArgb(255, 0, 0, 0) };
                var cells = new List<object>();
                for (var row = 0; row < backgrounds.Length; row++)
                {
                    var label = new TextBlock { Text = $"{theme}: {ColorHex(backgrounds[row])}", FontSize = 16, Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0)) };
                    Canvas.SetLeft(label, 16); Canvas.SetTop(label, 16 + row * 80); host.Children.Add(label);
                    var brushes = new[]
                    {
                        new SolidColorBrush(color) { Opacity = alpha },
                        new SolidColorBrush(Color.FromArgb(rounded, color.R, color.G, color.B)),
                        new SolidColorBrush(Color.FromArgb(floor, color.R, color.G, color.B)),
                        new SolidColorBrush(color)
                    };
                    for (var column = 0; column < brushes.Length; column++)
                    {
                        var x = 16 + column * 64; var y = 40 + row * 80;
                        var cell = new Border { Width = 32, Height = 32, Background = new SolidColorBrush(backgrounds[row]), Child = new Border { Background = brushes[column] } };
                        Canvas.SetLeft(cell, x); Canvas.SetTop(cell, y); host.Children.Add(cell);
                        cells.Add(new { row, column, x, y, width = 32, height = 32, background = ColorHex(backgrounds[row]), color = ColorHex(brushes[column].Color), opacity = brushes[column].Opacity });
                    }
                }
                await AwaitPresentationAsync(host);
                var file = $"catalog-{theme.ToString().ToLowerInvariant()}.png";
                var size = await CaptureElementAsync(host, Path.Combine(folder, file));
                reports.Add(new { scheme = theme.ToString(), alpha, rounded, floor, color = ColorHex(color), productionColor = ColorHex(original.Color), productionOpacity = original.Opacity, file, width = size.Width, height = size.Height, cells });
            }
            var buttons = new List<object>();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                buttons.Add(await CaptureResetAlphaAsync(host, theme, folder));
            await CapturePersistenceProbe.VerifyAsync(folder);
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "brush-alpha", environment = RuntimeEnvironment(), reports, buttons }, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally { CatalogRoot.Children.Remove(host); }
    }

    private async Task<object> CaptureResetAlphaAsync(Canvas host, ElementTheme theme, string folder)
    {
        CatalogRoot.RequestedTheme = theme;
        host.Children.Clear(); host.Background = CatalogRoot.Background;
        var model = new ToolbarFilterTable(); model.SetGlobal("Harbor");
        var bar = new BleeckerDataTableToolbar { Table = model, Width = 800 };
        Canvas.SetLeft(bar, 16); Canvas.SetTop(bar, 40); host.Children.Add(bar);
        await AwaitPresentationAsync(bar); bar.ApplyTemplate();
        static IEnumerable<T> Children<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                var child = VisualTreeHelper.GetChild(parent, index);
                if (child is T match) yield return match;
                foreach (var descendant in Children<T>(child)) yield return descendant;
            }
        }
        var button = Children<Button>(bar).Single(item => item.Name == "PART_Reset");
        using var input = new NativeInputSession(host.XamlRoot, folder, $"brush-alpha-{theme}");
        await input.AcquireAsync(bar); await input.MoveAsync(button); await AwaitPresentationAsync(button);
        Require(button.IsPointerOver && !button.IsPressed, "Alpha diagnostic did not receive actual Reset hover.");
        var surface = Children<ContentPresenter>(button).Single(item => item.Name == "ContentPresenter");
        var brush = surface.Background as SolidColorBrush ?? throw new InvalidOperationException("Reset hover is not a solid brush.");
        var originalColor = brush.Color; var originalOpacity = brush.Opacity;
        var legacyOpacity = theme == ElementTheme.Light ? .12 : .06;
        var rounded = (byte)Math.Round(legacyOpacity * 255, MidpointRounding.AwayFromZero);
        var floor = (byte)Math.Floor(legacyOpacity * 255);
        var variants = new[] { (Name: "production", Alpha: originalColor.A, Opacity: originalOpacity), (Name: "legacy-opacity", Alpha: (byte)255, Opacity: legacyOpacity), (Name: "rounded", Alpha: rounded, Opacity: 1d), (Name: "floor", Alpha: floor, Opacity: 1d) };
        var captures = new List<object>();
        try
        {
            foreach (var variant in variants)
            {
                // Only the isolated specimen's actual rendered brush changes.
                // Native input/state remains in control; no state is synthesized.
                brush.Color = Color.FromArgb(variant.Alpha, originalColor.R, originalColor.G, originalColor.B); brush.Opacity = variant.Opacity;
                await AwaitPresentationAsync(button);
                Require(ReferenceEquals(surface.Background, brush) && button.IsPointerOver && !button.IsPressed, "Reset alpha specimen lost its native hover/brush.");
                var file = $"reset-{theme.ToString().ToLowerInvariant()}-{variant.Name}.png";
                await CaptureElementAsync(host, Path.Combine(folder, file));
                var bounds = button.TransformToVisual(host).TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
                captures.Add(new { variant = variant.Name, file, color = ColorHex(brush.Color), opacity = brush.Opacity, bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }, button.IsPointerOver, button.IsPressed });
            }
        }
        finally { brush.Color = originalColor; brush.Opacity = originalOpacity; }
        return new { scheme = theme.ToString(), originalColor = ColorHex(originalColor), originalOpacity, rounded, floor, captures };
    }
}
