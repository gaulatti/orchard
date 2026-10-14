using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunFontAuditAsync(string folder)
    {
        if (CatalogRoot.XamlRoot.RasterizationScale != 1 || new Windows.UI.ViewManagement.UISettings().TextScaleFactor != 1) throw new InvalidOperationException("Font audit requires display/text scale 1.");
        var specs = new[] {
            new { id = "primary", file = "EncodeSans-Regular.ttf", family = "Encode Sans", legacy = "Encode Sans", weight = 400, size = 14d, line = 20d, spacing = 0, text = "Consumer-owned summary." },
            new { id = "medium", file = "EncodeSans-Medium.ttf", family = "Encode Sans", legacy = "Encode Sans Md", weight = 500, size = 14d, line = 20d, spacing = 0, text = "+12%" },
            new { id = "semibold", file = "EncodeSans-SemiBold.ttf", family = "Encode Sans", legacy = "Encode Sans SmBold", weight = 600, size = 30d, line = 30d, spacing = -20, text = "1,248" },
            new { id = "secondary", file = "LibreFranklin-Regular.ttf", family = "Libre Franklin", legacy = "Libre Franklin", weight = 400, size = 16d, line = 24d, spacing = 0, text = "Consumer-owned summary." },
            new { id = "secondary-semibold", file = "LibreFranklin-SemiBold.ttf", family = "Libre Franklin", legacy = "Libre Franklin SemiBold", weight = 600, size = 16d, line = 24d, spacing = 0, text = "Consumer-owned summary." } };
        using var input = new NativeInputSession(CatalogRoot.XamlRoot, folder, "font-audit"); await input.AcquireAsync(ComponentSearch);
        var host = new Grid { Margin = new Thickness(20), Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)) }; Grid.SetRowSpan(host, CatalogRoot.RowDefinitions.Count); Grid.SetColumnSpan(host, CatalogRoot.ColumnDefinitions.Count); CatalogRoot.Children.Add(host);
        var reports = new List<object>();
        try
        {
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                CatalogRoot.RequestedTheme = theme;
                foreach (var spec in specs)
                    foreach (var variant in new[] { ("canonical", spec.family, spec.weight), ("legacy", spec.legacy, spec.weight), ("canonical-normal", spec.family, 400), ("legacy-normal", spec.legacy, 400), ("segoe", "Segoe UI", spec.weight), ("invalid", "Definitely Missing Typeface", spec.weight) })
                    {
                        var family = variant.Item1 == "segoe" ? variant.Item2 : $"ms-appx:///Orchard/Assets/Fonts/{spec.file}#{variant.Item2}";
                        var canvas = new Canvas { Width = 500, Height = 100, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false };
                        var text = new TextBlock
                        {
                            Text = spec.text, FontFamily = new FontFamily(family), FontSize = spec.size,
                            FontWeight = new Windows.UI.Text.FontWeight { Weight = (ushort)variant.Item3 },
                            CharacterSpacing = spec.spacing, LineHeight = spec.line, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                            UseLayoutRounding = false, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),
                            HighContrastAdjustment = ElementHighContrastAdjustment.None
                        };
                        var metrics = new TextBlock { Text = "Mg", FontFamily = text.FontFamily, FontSize = text.FontSize, FontWeight = text.FontWeight, UseLayoutRounding = false, Opacity = 0, IsHitTestVisible = false };
                        AutomationProperties.SetAccessibilityView(metrics, AccessibilityView.Raw);
                        Canvas.SetLeft(text, 20); Canvas.SetTop(text, 20);
                        canvas.Children.Add(text); canvas.Children.Add(metrics); host.Children.Clear(); host.Children.Add(canvas);
                        CatalogRoot.UpdateLayout(); await FontFrameAsync(); CatalogRoot.UpdateLayout(); await FontFrameAsync(); CatalogRoot.UpdateLayout();
                        var filename = $"font-audit-{spec.id}-{variant.Item1}-{theme}.png"; await CaptureElementAsync(canvas, Path.Combine(folder, filename));
                        if (variant.Item1 == "canonical") await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"font-audit-{spec.id}-composed-{theme}.png"));
                        var bounds = canvas.TransformToVisual(CatalogRoot).TransformBounds(new Rect(0, 0, canvas.ActualWidth, canvas.ActualHeight));
                        reports.Add(new
                        {
                            spec.id, theme = theme.ToString(), variant = variant.Item1, family, weight = variant.Item3,
                            spec.size, spec.spacing, spec.line, spec.text, width = text.ActualWidth, height = text.ActualHeight,
                            desiredWidth = text.DesiredSize.Width, baseline = text.BaselineOffset,
                            naturalHeight = metrics.ActualHeight, naturalBaseline = metrics.BaselineOffset,
                            canvas = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }, filename
                        });
                    }
                await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            }
        }
        finally { CatalogRoot.Children.Remove(host); }
        await File.WriteAllTextAsync(Path.Combine(folder, "font-audit.json"), JsonSerializer.Serialize(reports, new JsonSerializerOptions { WriteIndented = true }));
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, captureOnly = true, parityVerified = false, component = "font-audit", environment = RuntimeEnvironment() }));
    }
    private static async Task FontFrameAsync()
    {
        var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Rendered(object? sender, object args) => frame.TrySetResult();
        CompositionTarget.Rendering += Rendered;
        try { await frame.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendered; }
    }
}
