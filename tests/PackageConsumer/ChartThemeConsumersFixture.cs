using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard.Testing;

internal static class ChartThemeConsumersFixture
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(node); i++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(node, i))) yield return child;
    }
    private static string Hex(Brush paint) => paint is SolidColorBrush brush ? $"#{brush.Color.A:X2}{brush.Color.R:X2}{brush.Color.G:X2}{brush.Color.B:X2}" : paint.GetType().Name;
    private static Brush Paint(ShapePath path, string kind) => kind == "Bar" || path.Tag as string == "area-fill" ? path.Fill : path.Stroke;
    private static (double First, double Last) Ends(ShapePath[] paths, string kind)
    {
        if (kind == "Bar") return (((PathGeometry)paths[0].Data).Figures[0].StartPoint.Y, ((PathGeometry)paths[^1].Data).Figures[0].StartPoint.Y);
        var figure = ((PathGeometry)(kind == "Area" ? paths.Single(p => p.Tag as string == "area-stroke") : paths[0]).Data).Figures[0];
        return (figure.StartPoint.Y, ((BezierSegment)figure.Segments[^1]).Point3.Y);
    }
    internal static async Task RunAsync(Window window, string output, string hostAssembly)
    {
        var root = new Grid { Width = 800, Height = 500 }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 500));
        var checks = new List<object>(); var guards = 0; var focus = 0; var activations = 0;
        root.GotFocus += (_, _) => focus++; window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Chart consumers left the background boundary.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 3; i++) { root.Measure(new Size(800, 500)); root.Arrange(new Rect(0, 0, 800, 500)); root.UpdateLayout(); Guard(); await Task.Delay(16); }
            await NativeRenderReadiness.RenderAsync(root); Guard();
        }
        void Check(string id, bool passed, object observed) { Guard(); checks.Add(new { id, passed, observed }); Require(passed, id); }
        void Defaults(string id, BleeckerLineChart chart, Canvas plot, string borderColor, string tickColor, string cardColor)
        {
            var grid = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().Where(line => line.StrokeDashArray.Count > 0).ToArray();
            var labels = plot.Children.OfType<TextBlock>().ToArray(); var tooltip = Elements(chart).OfType<Border>().Single(e => e.Name == "PART_Tooltip");
            var gridDefaults = BleeckerChartTheme.Grid; var axis = BleeckerChartTheme.Axis; var surface = BleeckerChartTheme.Tooltip;
            var valid = grid.Length > 0 && labels.Length >= 3 && grid.All(line => Hex(line.Stroke) == borderColor && line.Stroke.Opacity == 1 && line.StrokeThickness == 1 && line.StrokeDashArray.SequenceEqual(gridDefaults.StrokeDashes))
                && labels.All(label => Hex(label.Foreground) == tickColor && label.Foreground.Opacity == 1 && label.FontSize == axis.TickFontSize)
                && Hex(tooltip.Background) == cardColor && tooltip.Background.Opacity == 1 && Hex(tooltip.BorderBrush) == borderColor && tooltip.BorderBrush.Opacity == 1
                && tooltip.BorderThickness == new Thickness(surface.BorderThickness) && tooltip.CornerRadius == new CornerRadius(surface.CornerRadius);
            Check(id, valid, new { gridStrokes = grid.Select(line => Hex(line.Stroke)).ToArray(), gridOpacities = grid.Select(line => line.Stroke.Opacity).ToArray(), gridWidths = grid.Select(line => line.StrokeThickness).ToArray(), gridDashes = grid.Select(line => line.StrokeDashArray.ToArray()).ToArray(), tickPaints = labels.Select(label => Hex(label.Foreground)).ToArray(), tickOpacities = labels.Select(label => label.Foreground.Opacity).ToArray(), tickSizes = labels.Select(label => label.FontSize).ToArray(), tooltipBackground = Hex(tooltip.Background), tooltipBackgroundOpacity = tooltip.Background.Opacity, tooltipBorder = Hex(tooltip.BorderBrush), tooltipBorderOpacity = tooltip.BorderBrush.Opacity, tooltipBorderThickness = new[] { tooltip.BorderThickness.Left, tooltip.BorderThickness.Top, tooltip.BorderThickness.Right, tooltip.BorderThickness.Bottom }, tooltipRadii = new[] { tooltip.CornerRadius.TopLeft, tooltip.CornerRadius.TopRight, tooltip.CornerRadius.BottomRight, tooltip.CornerRadius.BottomLeft }, tooltipVisibility = tooltip.Visibility.ToString(), tooltipInteractionVerified = false, tooltipEffectsVerified = false });
        }
        async Task Tooltip(string id, BleeckerLineChart chart, string kind, string name, int value, string borderColor, string headingColor, string itemColor, string cardColor)
        {
            // Exercise the production renderer with a controlled inspection index.
            // This does not invoke chart input handlers or move native focus.
            typeof(BleeckerLineChart).GetMethod("ShowActive", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(chart, new object?[] { 0, null }); await Layout();
            var tooltip = Elements(chart).OfType<Border>().Single(e => e.Name == "PART_Tooltip"); var items = Elements(chart).OfType<StackPanel>().Single(e => e.Name == "PART_TooltipItems").Children.OfType<TextBlock>().ToArray();
            var expectedItem = (kind == "Line" ? name : "") + " : " + value;
            Check(id, tooltip.Visibility == Visibility.Visible && tooltip.ActualWidth > 0 && tooltip.ActualHeight > 0 && items.Length == 2 && items[0].Text == "A" && items[1].Text == expectedItem && items[0].FontWeight.Weight == BleeckerChartTheme.Tooltip.LabelFontWeight && Hex(items[0].Foreground) == headingColor && Hex(items[1].Foreground) == itemColor && Hex(tooltip.Background) == cardColor && Hex(tooltip.BorderBrush) == borderColor,
                new { controlledRendererState = true, visibility = tooltip.Visibility.ToString(), width = tooltip.ActualWidth, height = tooltip.ActualHeight, heading = items.FirstOrDefault()?.Text, item = items.LastOrDefault()?.Text, headingWeight = items.FirstOrDefault()?.FontWeight.Weight, headingPaint = items.Length > 0 ? Hex(items[0].Foreground) : "", itemPaint = items.Length > 1 ? Hex(items[1].Foreground) : "", background = Hex(tooltip.Background), border = Hex(tooltip.BorderBrush), tooltipInteractionVerified = false, tooltipEffectsVerified = false });
            typeof(BleeckerLineChart).GetMethod("ClearActive", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(chart, null); await Layout();
        }
        try
        {
            await Layout();
            foreach (var kind in new[] { "Line", "Bar", "Area" }) foreach (var scoped in new[] { false, true })
            {
                var rows = new ObservableCollection<IReadOnlyDictionary<string, object?>>();
                for (var i = 0; i < 3; i++) rows.Add(new Dictionary<string, object?> { ["category"] = ((char)('A' + i)).ToString(), ["metric.a"] = 10d * (i + 1), ["metric.b"] = 30d - 10 * i, ["metric"] = new Dictionary<string, object?> { ["a"] = 30d - 10 * i, ["b"] = 10d * (i + 1) } });
                var series = new BleeckerChartSeries { Key = "metric.a", Name = "Fictional alpha" }; var collection = new ObservableCollection<BleeckerChartSeries> { series };
                Control owner = kind switch { "Line" => new BleeckerLineChart { Data = rows, Series = collection, XAxisKey = "category", ShowLegend = true }, "Bar" => new BleeckerBarChart { Data = rows, Series = collection, XAxisKey = "category", ShowLegend = true }, _ => new BleeckerAreaChart { Data = rows, Series = collection, XAxisKey = "category", ShowLegend = true } };
                owner.Width = 640; owner.Height = 300; owner.HorizontalAlignment = HorizontalAlignment.Left; owner.VerticalAlignment = VerticalAlignment.Top;
                var scope = new Grid(); var brand = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 115, 145));
                var border = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 128, 80, 32)); var secondary = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 112, 64, 144)); var card = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 32, 48, 64)); var primary = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 64, 48, 32));
                if (scoped) { scope.Resources["BleeckerSeaBrush"] = brand; scope.Resources["BleeckerBorderBrush"] = border; scope.Resources["BleeckerTextSecondaryBrush"] = secondary; scope.Resources["BleeckerCardBrush"] = card; scope.Resources["BleeckerTextPrimaryBrush"] = primary; }
                scope.Children.Add(owner); root.Children.Add(scope); await Layout();
                var chart = Elements(owner).OfType<BleeckerLineChart>().Single(); var plot = Elements(chart).OfType<Canvas>().Single(e => e.Name == "PART_Plot"); var legend = Elements(chart).OfType<Canvas>().Single(e => e.Name == "PART_Legend");
                ShapePath[] Shapes() => plot.Children.OfType<ShapePath>().ToArray();
                TextBlock Label() => Elements(legend).OfType<TextBlock>().Single(e => e.Text == series.Name);
                foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
                {
                    root.RequestedTheme = theme; series.Key = "metric.a"; series.Name = "Fictional alpha"; series.Color = null; brand.Color = Windows.UI.Color.FromArgb(255, 25, 115, 145);
                    border.Color = Windows.UI.Color.FromArgb(255, 128, 80, 32); secondary.Color = Windows.UI.Color.FromArgb(255, 112, 64, 144); card.Color = Windows.UI.Color.FromArgb(255, 32, 48, 64); primary.Color = Windows.UI.Color.FromArgb(255, 64, 48, 32); await Layout();
                    var id = kind + "/" + (scoped ? "scoped" : "regular") + "/" + theme; var shapes = Shapes(); var ends = Ends(shapes, kind);
                    var aliases = ReferenceEquals(chart.Data, rows) && ReferenceEquals(chart.Series, collection) && ReferenceEquals(chart.Series[0], series);
                    Check(id + "/source-key-name", shapes.Length == (kind == "Line" ? 1 : kind == "Bar" ? 3 : 2) && ends.First > ends.Last && Label().Text == series.Name && aliases, new { shapes = shapes.Length, firstY = ends.First, lastY = ends.Last, label = Label().Text, aliases });
                    var expected = scoped ? Hex(brand) : BleeckerTokens.Colors(theme == ElementTheme.Dark ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light)["sea"];
                    Check(id + "/automatic-palette", shapes.All(p => Hex(Paint(p, kind)) == expected) && Hex(Label().Foreground) == expected, new { expected, paints = shapes.Select(p => Hex(Paint(p, kind))).ToArray(), labelPaint = Hex(Label().Foreground) });
                    var colors = BleeckerTokens.Colors(theme == ElementTheme.Dark ? BleeckerColorScheme.Dark : BleeckerColorScheme.Light);
                    Defaults(id + "/grid-axis-tooltip-defaults", chart, plot, scoped ? Hex(border) : colors["border"], scoped ? Hex(secondary) : colors["textSecondary"], scoped ? Hex(card) : colors["card"]);
                    await Tooltip(id + "/rendered-tooltip-defaults", chart, kind, series.Name, 10, scoped ? Hex(border) : colors["border"], scoped ? Hex(secondary) : colors["textSecondary"], scoped ? Hex(primary) : colors["textPrimary"], scoped ? Hex(card) : colors["card"]);
                    series.Key = "metric.b"; series.Name = "Updated alpha"; await Layout(); ends = Ends(Shapes(), kind);
                    Check(id + "/live-key-name", ends.First < ends.Last && Label().Text == series.Name && !Elements(legend).OfType<TextBlock>().Any(e => e.Text == "Fictional alpha"), new { firstY = ends.First, lastY = ends.Last, label = Label().Text });
                    var explicitPaint = new SolidColorBrush(Microsoft.UI.Colors.Transparent); series.Color = explicitPaint; await Layout();
                    Check(id + "/explicit-transparent", Shapes().All(p => ReferenceEquals(Paint(p, kind), explicitPaint)) && ReferenceEquals(Label().Foreground, explicitPaint), new { originalBrushRetained = Shapes().All(p => ReferenceEquals(Paint(p, kind), explicitPaint)), legendBrushRetained = ReferenceEquals(Label().Foreground, explicitPaint), paint = Hex(explicitPaint) });
                    explicitPaint.Color = Microsoft.UI.Colors.OrangeRed; await Layout();
                    Check(id + "/live-explicit-paint", Shapes().All(p => ReferenceEquals(Paint(p, kind), explicitPaint) && Hex(Paint(p, kind)) == "#FFFF4500") && Hex(Label().Foreground) == "#FFFF4500", new { paints = Shapes().Select(p => Hex(Paint(p, kind))).ToArray(), labelPaint = Hex(Label().Foreground) });
                    var gradient = new LinearGradientBrush(); gradient.GradientStops.Add(new GradientStop { Color = Microsoft.UI.Colors.Red, Offset = 0 }); gradient.GradientStops.Add(new GradientStop { Color = Microsoft.UI.Colors.Blue, Offset = 1 }); series.Color = gradient; await Layout();
                    Check(id + "/gradient-ownership", Shapes().All(p => ReferenceEquals(Paint(p, kind), gradient)) && ReferenceEquals(Label().Foreground, gradient), new { originalBrushRetained = Shapes().All(p => ReferenceEquals(Paint(p, kind), gradient)), legendBrushRetained = ReferenceEquals(Label().Foreground, gradient) });
                    series.Color = null; await Layout();
                    Check(id + "/restore-automatic", Shapes().All(p => Hex(Paint(p, kind)) == expected) && Hex(Label().Foreground) == expected, new { expected, paints = Shapes().Select(p => Hex(Paint(p, kind))).ToArray(), labelPaint = Hex(Label().Foreground) });
                    if (scoped)
                    {
                        brand.Color = Windows.UI.Color.FromArgb(255, 205, 45, 95); await Layout(); var changed = Hex(brand);
                        Check(id + "/live-scoped-paint", Shapes().All(p => Hex(Paint(p, kind)) == changed) && Hex(Label().Foreground) == changed, new { expected = changed, paints = Shapes().Select(p => Hex(Paint(p, kind))).ToArray(), labelPaint = Hex(Label().Foreground) });
                        border.Color = Windows.UI.Color.FromArgb(255, 33, 115, 91); secondary.Color = Windows.UI.Color.FromArgb(255, 155, 78, 34); card.Color = Windows.UI.Color.FromArgb(255, 20, 40, 60); primary.Color = Windows.UI.Color.FromArgb(255, 96, 160, 192); await Layout();
                        Defaults(id + "/live-scoped-defaults", chart, plot, Hex(border), Hex(secondary), Hex(card));
                        await Tooltip(id + "/rendered-live-scoped-tooltip", chart, kind, series.Name, 30, Hex(border), Hex(secondary), Hex(primary), Hex(card));
                    }
                }
                root.Children.Clear(); await Layout();
            }
            Guard(); await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-chart-theme-consumers-v1", passed = true, checks, guards, focus, activations, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), showWindowCalled = false, focusCalled = false, hardwareInputSent = false, osSettingsChanged = false, sourceRasterParityVerified = false, nativeComponentAccepted = false, librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerLineChart).Assembly.Location))), hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))) }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-chart-theme-consumers-v1", passed = false, error = error.ToString(), checks, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1);
        }
    }
}
