using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
using Windows.Foundation;
using Windows.Graphics.Imaging;

namespace Orchard.Catalog;

public sealed partial class LineChartsFixture : UserControl
{
    public LineChartsFixture()
    {
        InitializeComponent();
        Chart.Series.Add(new BleeckerChartSeries { Key = "total", Name = "Consumer total" });
        Chart.Series.Add(new BleeckerChartSeries { Key = "selected", Name = "Consumer selected" });
        foreach (var record in new[] { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) }) Chart.Data.Add(record);
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"line-chart-{scheme}"); await input.AcquireAsync(Chart); await input.MoveAsync(Before);
        await RemountChartAsync();
        Chart.ApplyTemplate(); UpdateLayout(); var root = (FrameworkElement)VisualTreeHelper.GetChild(Chart, 0);
        var plot = (Canvas)root.FindName("PART_Plot"); var legend = (Canvas)root.FindName("PART_Legend");
        await AwaitAsync(() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count() == 2, "Line chart did not render both consumer series.");
        Require(Chart.ActualWidth == 640 && Chart.ActualHeight == 300, "Line chart lost its source default dimensions.");
        var paths = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
        var first = (PathGeometry)paths[0].Data;
        Require(first.Figures[0].Segments.Count == 3 && first.Figures[0].Segments.All(segment => segment is BezierSegment), "Native chart did not consume monotone cubic geometry.");
        Require(Math.Abs(first.Figures[0].StartPoint.X - 60) < .001 && Math.Abs(first.Figures[0].StartPoint.Y - 157.8125) < .001, "Native line did not cross the source scale/layout boundary.");
        Require(plot.Children.OfType<Line>().Count() == 5 && legend.Children.Count == 2, "Native chart lost source grid or legend defaults.");
        Require(plot.Children.OfType<TextBlock>().Any(label => label.Text == "Harbor") && plot.Children.OfType<TextBlock>().Any(label => label.Text == "North"),
            "Category tick visibility incorrectly clipped labels to the plot instead of the full chart view box.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(Chart).GetName() == "Fictional coverage totals" && Chart.IsTabStop, "Native chart lost its consumer name or single keyboard entry.");
        var popup = (Border)root.FindName("PART_Tooltip"); var items = (StackPanel)root.FindName("PART_TooltipItems");
        Require(Chart.Focus(FocusState.Keyboard), "Native chart refused keyboard focus.");
        await AwaitAsync(() => popup.Visibility == Visibility.Visible && items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "Harbor", "Initial keyboard focus did not inspect the first consumer category.");
        await input.KeyAsync(0x27,Chart);
        await AwaitAsync(() => popup.Visibility == Visibility.Visible && items.Children.OfType<TextBlock>().First().Text == "Reed", "Arrow navigation did not inspect the second consumer category.");
        var container = (Grid)root.FindName("PART_TooltipContainer");
        await AwaitAsync(() => Math.Abs(popup.ActualHeight - 110) < .001, "Tooltip item padding and line height did not cross the source layout boundary.");
        Require(Math.Abs(container.Margin.Top - 35) < .001 && Math.Abs(container.Margin.Left - 256.6666667) < .001, "Keyboard tooltip lost the frozen chart-height coordinate or edge placement.");
        VerifyEffects(root);
        await AwaitPresentationAsync(root);
        Require(((Canvas)root.FindName("PART_Active")).Children.OfType<Ellipse>().Count() == 2, "Active chart category did not show both source dots.");
        await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"line-chart-{scheme}-keyboard.png"));
        await VerifyShadowPixelsAsync(scheme, folder);
        await input.MoveAsync(Chart, localPoint: new Point(246, 50));
        var bounds = input.ScreenBounds(Chart); var actualPointerY = ((int)(bounds.Y + 50 * XamlRoot.RasterizationScale) - bounds.Y) / XamlRoot.RasterizationScale;
        await AwaitAsync(() => Math.Abs(container.Margin.Top - (actualPointerY + 10)) < .001, "Pointer tooltip did not track the physical pointer coordinate.");
        await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"line-chart-{scheme}-pointer.png"));
        await input.MoveAsync(Before); Chart.Focus(FocusState.Keyboard); await input.KeyAsync(0x0D,Chart);
        await AwaitAsync(() => popup.Visibility == Visibility.Visible && items.Children.OfType<TextBlock>().First().Text == "Reed", "Keyboard inspection did not restore after pointer exit.");
        Chart.TooltipFormatter = value => $"amount {value}"; Chart.YAxisTickFormatter = value => $"${value}";
        await AwaitAsync(() => items.Children.OfType<TextBlock>().Any(item => item.Text.Contains("amount 220")), "Consumer tooltip formatter did not reach the native overlay.");
        Chart.Series[0].Name = "Updated consumer total";
        await AwaitAsync(() => items.Children.OfType<TextBlock>().Any(item => item.Text.StartsWith("Updated consumer total")), "Series mutation did not update the native chart consumer.");
        var explicitBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent); Chart.Series[0].Color = explicitBrush;
        await AwaitAsync(() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Any(path => ReferenceEquals(path.Stroke, explicitBrush)), "Explicit transparent paint lost native precedence.");
        Chart.Data[1] = Record("Reed changed", 80, 40);
        await AwaitAsync(() => items.Children.OfType<TextBlock>().First().Text == "Reed changed", "Consumer record replacement did not update active chart content.");
        Chart.Data[1] = new Dictionary<string, object?> { { "label", "Raw consumer strings" }, { "total", "220" }, { "selected", "pending" } };
        await AwaitAsync(() => items.Children.OfType<TextBlock>().Any(item => item.Text.EndsWith(" : pending")) && items.Children.OfType<TextBlock>().Any(item => item.Text.EndsWith(" : 220")), "Raw string tooltip values were filtered or passed through the numeric formatter.");
        Chart.Data[1] = new Dictionary<string, object?> { { "label", "Gap" }, { "total", null }, { "selected", null } };
        await AwaitAsync(() => popup.Visibility == Visibility.Collapsed && ((Canvas)root.FindName("PART_Active")).Children.OfType<Line>().Count() == 1, "Null tooltip payload retained a visible surface or removed the category cursor.");
        Chart.ShowLegend = false; Chart.ShowGrid = false;
        await AwaitAsync(() => legend.Children.Count == 0 && plot.Children.OfType<Line>().Count() == 0, "Grid/legend visibility did not cross the native producer boundary.");
        Chart.Data.Clear(); await AwaitAsync(() => !plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Any() && popup.Visibility == Visibility.Collapsed, "Empty consumer data retained live series or tooltip artifacts.");
        Require(ElementCompositionPreview.GetElementChildVisual((Grid)root.FindName("PART_TooltipShadow")) is null && ElementCompositionPreview.GetElementChildVisual((Grid)root.FindName("PART_TooltipBackdrop")) is null, "Closed tooltip retained its composition effects.");
        foreach (var record in new[] { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) }) Chart.Data.Add(record);
        Chart.Series[0].Name = "Consumer total"; Chart.Series[0].Color = null; Chart.TooltipFormatter = null; Chart.YAxisTickFormatter = null; Chart.ShowLegend = true; Chart.ShowGrid = true;
        await AwaitAsync(() => plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().Count() == 2 && legend.Children.Count == 2, "Native chart did not restore consumer data and defaults.");
        Require(Before.Focus(FocusState.Keyboard), "Normal chart capture could not leave keyboard inspection.");
        await input.MoveAsync(Before); await MainWindow.CaptureElementAsync(Chart, System.IO.Path.Combine(folder, $"line-chart-{scheme}.png"));
        var sourceCases = await VerifySourceCasesAsync(scheme, folder, plot, legend);
        var tooltipCases = await VerifyTooltipCasesAsync(scheme, folder, input, root);
        await VerifyBlurProbeAsync(scheme, folder, input);
        await VerifyInteractionAsync(scheme,folder,input);
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"line-chart-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, sourceCases, tooltipCases, curves = 2, keyboard = true, liveData = true, explicitTransparent = true }));
    }
    private async Task RemountChartAsync()
    {
        Before.Focus(FocusState.Keyboard);
        var unloaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loaded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnUnloaded(object sender, RoutedEventArgs args) => unloaded.TrySetResult();
        void OnLoaded(object sender, RoutedEventArgs args) => loaded.TrySetResult();
        Chart.Unloaded += OnUnloaded; Chart.Loaded += OnLoaded;
        try
        {
            ChartStage.Children.Remove(Chart); await unloaded.Task.WaitAsync(TimeSpan.FromSeconds(2));
            ChartStage.Children.Add(Chart); await loaded.Task.WaitAsync(TimeSpan.FromSeconds(2)); UpdateLayout();
        }
        finally { Chart.Unloaded -= OnUnloaded; Chart.Loaded -= OnLoaded; }
    }
    private async Task VerifyInteractionAsync(BleeckerColorScheme scheme,string folder,NativeInputSession input)
    {
        await input.MoveAsync(Before);Chart.Series=new ObservableCollection<BleeckerChartSeries>{new(){Key="total",Name="Consumer total"}};
        Chart.Data=new ObservableCollection<IReadOnlyDictionary<string,object?>>{Record("Harbor",100,0),Record("Reed",220,0),Record("Vale",140,0),Record("North",310,0)};
        await RemountChartAsync();var root=(FrameworkElement)VisualTreeHelper.GetChild(Chart,0);var popup=(Border)root.FindName("PART_Tooltip");var active=(Canvas)root.FindName("PART_Active");var items=(StackPanel)root.FindName("PART_TooltipItems");
        using var stream=typeof(LineChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.LineInteraction.json")??throw new InvalidOperationException("Missing frozen interaction sequence.");using var reference=JsonDocument.Parse(stream);
        var sequence=reference.RootElement.GetProperty("results").EnumerateArray().Single(item=>item.GetProperty("scheme").GetString()==scheme.ToString());var evidence=new List<object>();
        foreach(var expected in sequence.GetProperty("steps").EnumerateArray())
        {
            var action=expected.GetProperty("action").GetString()!;
            switch(action)
            {
                case "focus":case "refocus":Require(Chart.Focus(FocusState.Keyboard),"Chart refused keyboard entry.");break;
                case "blur":Require(After.Focus(FocusState.Keyboard),"Chart refused normal focus exit.");break;
                case "ArrowRight":await input.KeyAsync(0x27,Chart);break;
                case "ArrowLeft":await input.KeyAsync(0x25,Chart);break;
                case "Enter":await input.KeyAsync(0x0D,Chart);break;
                case "pointer-midpoint":await input.MoveAsync(Chart,localPoint:new Point(340,100));break;
                case "pointer-exit":await input.MoveAsync(Before);break;
                case "single-right":Chart.Data=new ObservableCollection<IReadOnlyDictionary<string,object?>>{Record("Harbor",100,0)};await Task.Delay(32);await input.KeyAsync(0x27,Chart);break;
                case "single-enter":await input.KeyAsync(0x0D,Chart);break;
                case "empty-left":Chart.Data.Clear();await Task.Delay(32);await input.KeyAsync(0x25,Chart);break;
                case "empty-enter":await input.KeyAsync(0x0D,Chart);break;
                default:throw new InvalidOperationException("Unknown frozen keyboard action.");
            }
            bool Matches()=>popup.Visibility==(expected.GetProperty("visible").GetBoolean()?Visibility.Visible:Visibility.Collapsed)&&active.Children.OfType<Line>().Any()==expected.GetProperty("cursor").GetBoolean()&&(popup.Visibility!=Visibility.Visible||items.Children.OfType<TextBlock>().First().Text==expected.GetProperty("label").GetString())&&(Chart.FocusState==FocusState.Keyboard)==expected.GetProperty("focused").GetBoolean();
            await AwaitAsync(Matches,$"{action}: native focus/keyboard/pointer state differs from the frozen source.");
            evidence.Add(new{action,visible=popup.Visibility==Visibility.Visible,label=popup.Visibility==Visibility.Visible?items.Children.OfType<TextBlock>().First().Text:"",cursor=active.Children.OfType<Line>().Any(),focused=Chart.FocusState==FocusState.Keyboard});
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"line-interaction-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,steps=evidence}));
        Before.Focus(FocusState.Keyboard);Chart.Series=new ObservableCollection<BleeckerChartSeries>{new(){Key="total",Name="Consumer total"},new(){Key="selected",Name="Consumer selected"}};Chart.Data=new ObservableCollection<IReadOnlyDictionary<string,object?>>{Record("Harbor",100,50),Record("Reed",220,180),Record("Vale",140,90),Record("North",310,240)};
    }
    private static void VerifyEffects(FrameworkElement root)
    {
        var shadow = (ContainerVisual)ElementCompositionPreview.GetElementChildVisual((Grid)root.FindName("PART_TooltipShadow"));
        var visual = shadow.Children.Single() as SpriteVisual ?? throw new InvalidOperationException("Tooltip shadow lacks its native sprite.");
        Require(visual.Shadow is DropShadow effect && effect.BlurRadius == 32 && effect.Offset.Y == 8 && Math.Abs(effect.Opacity - .08) < 1e-6 && effect.Mask is not null && shadow.Clip is CompositionGeometricClip, "Tooltip shadow did not consume its source metadata or exclude the border box.");
        var backdrop = ElementCompositionPreview.GetElementChildVisual((Grid)root.FindName("PART_TooltipBackdrop")) as SpriteVisual;
        Require(backdrop?.Brush is CompositionEffectBrush brush && brush.Properties.TryGetScalar("chartBackdrop.BlurAmount", out var amount) == CompositionGetValueStatus.Succeeded && amount == 8 && backdrop.Clip is CompositionGeometricClip, "Tooltip backdrop did not consume its scoped Gaussian blur or rounded clipping.");
    }
    private static async Task AwaitPresentationAsync(FrameworkElement root)
    {
        var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var frames=0;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        void Frame(object? sender,object args){if(++frames==2)completion.TrySetResult();}
        CompositionTarget.Rendering+=Frame;
        try{await completion.Task.WaitAsync(deadline.Token);}finally{CompositionTarget.Rendering-=Frame;}
        await ElementCompositionPreview.GetElementVisual(root).Compositor.RequestCommitAsync().AsTask().WaitAsync(deadline.Token);
    }
    private async Task VerifyShadowPixelsAsync(BleeckerColorScheme scheme, string folder)
    {
        using var referenceStream = typeof(LineChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.LineShadow.json") ?? throw new InvalidOperationException("Missing source shadow pixels.");
        using var reference = JsonDocument.Parse(referenceStream); var expected = reference.RootElement.GetProperty("results").EnumerateArray().Single(item => item.GetProperty("scheme").GetString() == scheme.ToString());
        using var file = File.OpenRead(System.IO.Path.Combine(folder, $"line-chart-{scheme}-keyboard.png")); using var random = file.AsRandomAccessStream(); var decoder = await BitmapDecoder.CreateAsync(random);
        var pixels = (await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var evidence = new List<object>(); var matches = true;
        foreach (var sample in expected.GetProperty("samples").EnumerateArray())
        {
            var origin = Chart.TransformToVisual(XamlRoot.Content).TransformPoint(new Point(sample.GetProperty("x").GetDouble(), sample.GetProperty("y").GetDouble()));
            var x = (int)Math.Floor(origin.X * XamlRoot.RasterizationScale); var y = (int)Math.Floor(origin.Y * XamlRoot.RasterizationScale); var index = (y * (int)decoder.PixelWidth + x) * 4;
            var red = pixels[index + 2]; var green = pixels[index + 1]; var blue = pixels[index]; evidence.Add(new { sample = sample.Clone(), native = new { x, y, red, green, blue } });
            matches &= Math.Abs(red - sample.GetProperty("r").GetInt32()) <= 2 && Math.Abs(green - sample.GetProperty("g").GetInt32()) <= 2 && Math.Abs(blue - sample.GetProperty("b").GetInt32()) <= 2;
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"line-tooltip-{scheme}-shadow.json"), JsonSerializer.Serialize(new { passed = matches, evidence })); Require(matches, "Composed tooltip shadow did not match the source Gaussian profile.");
    }
    private async Task VerifyBlurProbeAsync(BleeckerColorScheme scheme, string folder, NativeInputSession input)
    {
        using var referenceStream = typeof(LineChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.LineBlur.json") ?? throw new InvalidOperationException("Missing source blur probe.");
        using var reference = JsonDocument.Parse(referenceStream); var expected = reference.RootElement.GetProperty("results").EnumerateArray().Single(item => item.GetProperty("scheme").GetString() == scheme.ToString());
        var probe = new BleeckerLineChart { Width = 640, XAxisKey = "label", HorizontalAlignment = HorizontalAlignment.Left };
        probe.Resources["BleeckerCardBrush"] = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = .25 };
        probe.Series.Add(new BleeckerChartSeries { Key = "total", Name = "Consumer total" }); probe.Series.Add(new BleeckerChartSeries { Key = "selected", Name = "Consumer selected" });
        foreach (var record in new[] { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) }) probe.Data.Add(record);
        Before.Focus(FocusState.Keyboard); await input.MoveAsync(Before); Chart.Visibility = Visibility.Collapsed; BlurProbe.Visibility = Visibility.Visible;
        for (var column = 0; column < 80; column++) { var stripe = new Rectangle { Width = 8, Height = 300, Fill = new SolidColorBrush(column % 2 == 0 ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White) }; Canvas.SetLeft(stripe, column * 8); BlurProbe.Children.Add(stripe); }
        ChartStage.Children.Add(probe);
        try
        {
            UpdateLayout(); await Task.Delay(32); Require(probe.Focus(FocusState.Keyboard), "Blur probe refused actual keyboard entry.");
            var root = (FrameworkElement)VisualTreeHelper.GetChild(probe, 0); var popup = (Border)root.FindName("PART_Tooltip");
            var items = (StackPanel)root.FindName("PART_TooltipItems");
            await AwaitAsync(() => popup.Visibility == Visibility.Visible && items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "Harbor", "Blur probe initial focus did not select its first source category.");
            await input.KeyAsync(0x27,probe);
            await AwaitAsync(() => popup.Visibility == Visibility.Visible && popup.ActualHeight == 110 && items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == "Reed", "Blur probe did not produce its source tooltip dimensions and category."); VerifyEffects(root);
            await AwaitPresentationAsync(root); var capturePath = System.IO.Path.Combine(folder, $"line-tooltip-{scheme}-blur.png"); await NativeScreenCapture.CaptureClientAsync(input.Window, capturePath);
            using var file = File.OpenRead(capturePath); using var random = file.AsRandomAccessStream(); var decoder = await BitmapDecoder.CreateAsync(random);
            var pixels = (await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, new BitmapTransform(), ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage)).DetachPixelData();
            var actual = new List<object>();
            foreach (var sample in expected.GetProperty("samples").EnumerateArray())
            {
                var origin = probe.TransformToVisual(XamlRoot.Content).TransformPoint(new Point(sample.GetProperty("x").GetDouble(), sample.GetProperty("y").GetDouble()));
                var x = (int)Math.Floor(origin.X * XamlRoot.RasterizationScale); var y = (int)Math.Floor(origin.Y * XamlRoot.RasterizationScale); var index = (y * (int)decoder.PixelWidth + x) * 4;
                var red = pixels[index + 2]; var green = pixels[index + 1]; var blue = pixels[index]; actual.Add(new { x, y, red, green, blue });
                Require(Math.Abs(red - sample.GetProperty("r").GetInt32()) <= 2 && Math.Abs(green - sample.GetProperty("g").GetInt32()) <= 2 && Math.Abs(blue - sample.GetProperty("b").GetInt32()) <= 2, "Composed tooltip backdrop did not match the source's blurred eight-pixel stripes.");
            }
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"line-tooltip-{scheme}-blur.json"), JsonSerializer.Serialize(new { passed = true, samples = actual }));
        }
        finally { ChartStage.Children.Remove(probe); BlurProbe.Children.Clear(); BlurProbe.Visibility = Visibility.Collapsed; Chart.Visibility = Visibility.Visible; }
    }
    private async Task<int> VerifyTooltipCasesAsync(BleeckerColorScheme scheme, string folder, NativeInputSession input, FrameworkElement root)
    {
        using var stream = typeof(LineChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.LineTooltip.json") ?? throw new InvalidOperationException("Missing frozen tooltip capture.");
        using var reference = JsonDocument.Parse(stream); Require(reference.RootElement.GetProperty("recharts").GetString() == "3.8.1", "Tooltip renderer version changed.");
        var popup = (Border)root.FindName("PART_Tooltip"); var container = (Grid)root.FindName("PART_TooltipContainer"); var items = (StackPanel)root.FindName("PART_TooltipItems"); var active = (Canvas)root.FindName("PART_Active");
        var evidence = new List<object>(); var checkedCases = 0;
        foreach (var specimen in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
        {
            Before.Focus(FocusState.Keyboard); await input.MoveAsync(Before); var props = specimen.GetProperty("props"); var id = specimen.GetProperty("id").GetString()!;
            Chart.Width = specimen.GetProperty("width").GetDouble(); Chart.Height = specimen.GetProperty("height").GetDouble(); Chart.XAxisKey = props.GetProperty("xAxisKey").GetString();
            Chart.XAxisLabel = props.TryGetProperty("xAxisLabel", out var x) ? x.GetString() : null; Chart.YAxisLabel = props.TryGetProperty("yAxisLabel", out var y) ? y.GetString() : null;
            Chart.ShowGrid = !props.TryGetProperty("showGrid", out var grid) || grid.GetBoolean(); Chart.ShowLegend = !props.TryGetProperty("showLegend", out var legend) || legend.GetBoolean();
            var format = props.TryGetProperty("format", out var formatting) && formatting.GetBoolean(); Chart.YAxisTickFormatter = format ? value => "$" + value.ToString(CultureInfo.InvariantCulture) : null; Chart.TooltipFormatter = format ? value => "amount " + value.ToString(CultureInfo.InvariantCulture) : null;
            Chart.Series = new ObservableCollection<BleeckerChartSeries>(props.GetProperty("series").EnumerateArray().Select(item => new BleeckerChartSeries { Key = item.GetProperty("key").GetString()!, Name = item.GetProperty("name").GetString()!, Color = item.TryGetProperty("color", out var color) ? new SolidColorBrush(color.GetString() == "transparent" ? Microsoft.UI.Colors.Transparent : Windows.UI.Color.FromArgb(255, 25, 115, 145)) : null }));
            Chart.Data = new ObservableCollection<IReadOnlyDictionary<string, object?>>(props.GetProperty("data").EnumerateArray().Select(item => (IReadOnlyDictionary<string, object?>)item.EnumerateObject().ToDictionary(property => property.Name, property => Value(property.Value))));
            await RemountChartAsync();
            root = (FrameworkElement)VisualTreeHelper.GetChild(Chart, 0);
            popup = (Border)root.FindName("PART_Tooltip"); container = (Grid)root.FindName("PART_TooltipContainer");
            items = (StackPanel)root.FindName("PART_TooltipItems"); active = (Canvas)root.FindName("PART_Active");
            UpdateLayout(); Require(Chart.Focus(FocusState.Keyboard), $"{id}: tooltip keyboard entry failed.");
            await AwaitAsync(() => Chart.FocusState == FocusState.Keyboard && (Chart.Data.Count == 0 || items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == Convert.ToString(Chart.Data[0][Chart.XAxisKey!], CultureInfo.InvariantCulture)), $"{id}: initial focus did not cross the native entry boundary.");
            await input.KeyAsync(0x27,Chart);
            var expected = specimen.GetProperty("keyboard"); var visible = expected.GetProperty("visible").GetBoolean();
            var cursorBeforeCapture = active.Children.OfType<Line>().Count();
            await AwaitAsync(() => popup.Visibility == (visible ? Visibility.Visible : Visibility.Collapsed) && (Chart.Data.Count == 0 || items.Children.OfType<TextBlock>().FirstOrDefault()?.Text == expected.GetProperty("label").GetString()) && active.Children.OfType<Line>().Any() == expected.GetProperty("cursor").GetBoolean(), $"{id}: source/native tooltip visibility, category or cursor differs.");
            if (visible)
            {
                Require(items.Children.OfType<TextBlock>().Skip(1).Select(item => item.Text).SequenceEqual(expected.GetProperty("items").EnumerateArray().Select(item => item.GetString()!)), $"{id}: source/native tooltip payload differs.");
                var box = expected.GetProperty("box"); await AwaitAsync(() => Math.Abs(popup.ActualHeight - box.GetProperty("height").GetDouble()) < .001, $"{id}: source/native tooltip height differs.");
                Require(Math.Abs(container.Margin.Top - box.GetProperty("y").GetDouble()) < .001, $"{id}: source/native keyboard tooltip vertical placement differs.");
                var sourceX = box.GetProperty("x").GetDouble(); var sourceWidth = box.GetProperty("width").GetDouble();
                var anchor = Chart.Data.Count == 1 ? (60 + (string.IsNullOrEmpty(Chart.YAxisLabel) ? 0 : 20) + Chart.ActualWidth - 20) / 2 : 60 + (string.IsNullOrEmpty(Chart.YAxisLabel) ? 0 : 20) + (Chart.ActualWidth - 20 - 60 - (string.IsNullOrEmpty(Chart.YAxisLabel) ? 0 : 20)) / (Chart.Data.Count - 1);
                if (Math.Abs(sourceX - anchor - 10) < .001) Require(Math.Abs(container.Margin.Left - sourceX) < .001, $"{id}: right tooltip offset differs.");
                else if (Math.Abs(sourceX - 60) < .001) Require(Math.Abs(container.Margin.Left - sourceX) < .001, $"{id}: oversized tooltip lost source clamping.");
                else await AwaitAsync(() => Math.Abs(container.Margin.Left + popup.ActualWidth - sourceX - sourceWidth) < .001, $"{id}: left tooltip edge placement differs.");
                VerifyEffects(root); evidence.Add(new { id, source = box.Clone(), native = new { x = container.Margin.Left, y = container.Margin.Top, width = popup.ActualWidth, height = popup.ActualHeight } });
                await NativeScreenCapture.CaptureClientAsync(input.Window, System.IO.Path.Combine(folder, $"line-tooltip-{scheme}-{id}.png"));
            }
            try { Require(active.Children.OfType<Line>().Any() == expected.GetProperty("cursor").GetBoolean(), $"{id}: source/native category cursor differs."); }
            catch
            {
                var currentRoot = (FrameworkElement)VisualTreeHelper.GetChild(Chart, 0);
                var currentActive = (Canvas)currentRoot.FindName("PART_Active");
                var state = typeof(BleeckerLineChart).GetField("interaction", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(Chart)!;
                var stateFields = state.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .ToDictionary(field => field.Name, field => field.GetValue(state));
                await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"line-tooltip-cursor-{scheme}-{id}.json"), JsonSerializer.Serialize(new
                {
                    id, expected = expected.Clone(), cursorBeforeCapture, afterCount = active.Children.OfType<Line>().Count(),
                    sameRoot = ReferenceEquals(root, currentRoot), sameActive = ReferenceEquals(active, currentActive),
                    active.IsLoaded, active.Visibility, currentCount = currentActive.Children.OfType<Line>().Count(),
                    children = active.Children.Select(child => child.GetType().Name), Chart.FocusState,
                    popup = popup.Visibility, labels = items.Children.OfType<TextBlock>().Select(text => text.Text), stateFields,
                    point = input.DescribeCurrentPoint(), routing = input.DescribeInputRouting()
                }));
                throw;
            }
            checkedCases++;
        }
        Before.Focus(FocusState.Keyboard); Chart.Width = 640; Chart.XAxisLabel = null; Chart.YAxisLabel = null; Chart.YAxisTickFormatter = null; Chart.TooltipFormatter = null; Chart.ShowGrid = true; Chart.ShowLegend = true;
        Chart.Series = new ObservableCollection<BleeckerChartSeries> { new() { Key = "total", Name = "Consumer total" }, new() { Key = "selected", Name = "Consumer selected" } };
        Chart.Data = new ObservableCollection<IReadOnlyDictionary<string, object?>> { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) };
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"line-tooltip-{scheme}-measurements.json"), JsonSerializer.Serialize(evidence)); Require(checkedCases == 13, "Tooltip specimen count changed."); return checkedCases;
    }
    private async Task<int> VerifySourceCasesAsync(BleeckerColorScheme scheme, string folder, Canvas plot, Canvas legend)
    {
        using var stream = typeof(LineChartsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.LineLayout.json") ?? throw new InvalidOperationException("Missing frozen Line Chart layout evidence.");
        using var reference = JsonDocument.Parse(stream);
        Require(reference.RootElement.GetProperty("sourceSha256").GetString() == "673539e9a2c5a264fbfb6c35f09e7d956d2dbde9670f3ae6cf1888e752ec403a" && reference.RootElement.GetProperty("recharts").GetString() == "3.8.1", "Line Chart reference changed its source boundary.");
        var checkedCases = 0;
        foreach (var specimen in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
        {
            var id = specimen.GetProperty("id").GetString()!; var props = specimen.GetProperty("props");
            Chart.Width = specimen.GetProperty("width").GetDouble(); Chart.Height = specimen.GetProperty("height").GetDouble();
            Chart.XAxisKey = props.GetProperty("xAxisKey").GetString();
            Chart.XAxisLabel = props.TryGetProperty("xAxisLabel", out var xLabel) ? xLabel.GetString() : null;
            Chart.YAxisLabel = props.TryGetProperty("yAxisLabel", out var yLabel) ? yLabel.GetString() : null;
            Chart.ShowGrid = !props.TryGetProperty("showGrid", out var showGrid) || showGrid.GetBoolean();
            Chart.ShowLegend = !props.TryGetProperty("showLegend", out var showLegend) || showLegend.GetBoolean();
            Chart.YAxisTickFormatter = props.TryGetProperty("format", out var format) && format.GetBoolean() ? value => $"${value}" : null;
            Chart.Series = new ObservableCollection<BleeckerChartSeries>(props.GetProperty("series").EnumerateArray().Select(item => new BleeckerChartSeries { Key = item.GetProperty("key").GetString()!, Name = item.GetProperty("name").GetString()!, Color = item.TryGetProperty("color", out var color) ? new SolidColorBrush(color.GetString() == "transparent" ? Microsoft.UI.Colors.Transparent : Windows.UI.Color.FromArgb(255, 25, 115, 145)) : null }));
            Chart.Data = new ObservableCollection<IReadOnlyDictionary<string, object?>>(props.GetProperty("data").EnumerateArray().Select(item => (IReadOnlyDictionary<string, object?>)item.EnumerateObject().ToDictionary(property => property.Name, property => Value(property.Value))));
            UpdateLayout(); await Task.Delay(32); UpdateLayout();
            var nativeCurves = plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray(); var sourceCurves = specimen.GetProperty("curves").EnumerateArray().ToArray();
            Require(nativeCurves.Length == sourceCurves.Length, $"{id}: native/source curve count differs.");
            for (var index = 0; index < sourceCurves.Length; index++)
            {
                CompareCurve((PathGeometry)nativeCurves[index].Data, sourceCurves[index].GetProperty("d").GetString()!, id);
                Require(nativeCurves[index].StrokeThickness == 2, $"{id}: native/source line width differs.");
                ComparePaint(nativeCurves[index].Stroke, sourceCurves[index].GetProperty("stroke").GetString()!, id);
            }
            var nativeGrid = plot.Children.OfType<Line>().ToArray(); var sourceGrid = specimen.GetProperty("grid").EnumerateArray().ToArray();
            Require(nativeGrid.Length == sourceGrid.Length, $"{id}: native/source grid count differs.");
            for (var index = 0; index < sourceGrid.Length; index++)
            {
                var expected = sourceGrid[index]; var line = nativeGrid[index];
                Require(Math.Abs(line.X1 - Number(expected.GetProperty("x1"))) < .001 && Math.Abs(line.X2 - Number(expected.GetProperty("x2"))) < .001 && Math.Abs(line.Y1 - Number(expected.GetProperty("y1"))) < .001 && Math.Abs(line.Y2 - Number(expected.GetProperty("y2"))) < .001, $"{id}: native/source grid coordinates differ.");
            }
            var ticks = plot.Children.OfType<TextBlock>().Where(label => label.Text != Chart.XAxisLabel && label.Text != Chart.YAxisLabel).ToArray();
            var expectedTicks = specimen.GetProperty("xTicks").EnumerateArray().Concat(specimen.GetProperty("yTicks").EnumerateArray()).Select(item => item.GetProperty("text").GetString()!).Order(StringComparer.Ordinal).ToArray();
            Require(ticks.Select(label => label.Text).Order(StringComparer.Ordinal).SequenceEqual(expectedTicks), $"{id}: native/source visible tick labels differ: {string.Join('|', ticks.Select(label => label.Text))}.");
            foreach (var expected in specimen.GetProperty("xTicks").EnumerateArray().Concat(specimen.GetProperty("yTicks").EnumerateArray()))
                ComparePaint(ticks.First(label => label.Text == expected.GetProperty("text").GetString()).Foreground, expected.GetProperty("fill").GetString()!, id);
            var nativeLegend = legend.Children.OfType<StackPanel>().Select(group => group.Children.OfType<TextBlock>().Single().Text).ToArray();
            Require(nativeLegend.SequenceEqual(specimen.GetProperty("legend").EnumerateArray().Select(item => item.GetString()!)), $"{id}: native/source legend order differs.");
            await MainWindow.CaptureElementAsync(Chart, System.IO.Path.Combine(folder, $"line-chart-{scheme}-{id}.png")); checkedCases++;
        }
        Chart.Width = 640; Chart.XAxisLabel = null; Chart.YAxisLabel = null; Chart.YAxisTickFormatter = null;
        Chart.Series = new ObservableCollection<BleeckerChartSeries> { new() { Key = "total", Name = "Consumer total" }, new() { Key = "selected", Name = "Consumer selected" } };
        Chart.Data = new ObservableCollection<IReadOnlyDictionary<string, object?>> { Record("Harbor", 100, 50), Record("Reed", 220, 180), Record("Vale", 140, 90), Record("North", 310, 240) };
        Require(checkedCases == 11, "Frozen Line Chart specimen count changed."); return checkedCases;
    }
    private static object? Value(JsonElement value) => value.ValueKind switch { JsonValueKind.String => value.GetString(), JsonValueKind.Number => value.GetDouble(), JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unexpected frozen chart data type.") };
    private static double Number(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.Parse(value.GetString()!, CultureInfo.InvariantCulture);
    private static void ComparePaint(Brush actual, string expected, string id)
    {
        Require(actual is SolidColorBrush, $"{id}: source paint did not resolve to a native solid brush.");
        var brush = (SolidColorBrush)actual; var channels = Regex.Matches(expected, @"[0-9]+(?:\.[0-9]+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        Require(channels.Length is 3 or 4, $"{id}: unexpected source paint syntax."); var alpha = channels.Length == 4 ? channels[3] : 1;
        Require(Math.Abs(brush.Color.A / 255d * brush.Opacity - alpha) < 1e-9 && (alpha == 0 || brush.Color.R == channels[0] && brush.Color.G == channels[1] && brush.Color.B == channels[2]), $"{id}: native/source chart paint differs.");
    }
    internal static void CompareCurve(PathGeometry actual, string expected, string id)
    {
        var commands = new List<(string Kind, double[] Values)>();
        foreach (var figure in actual.Figures)
        {
            commands.Add(("M", [figure.StartPoint.X, figure.StartPoint.Y]));
            foreach (var segment in figure.Segments)
                if (segment is BezierSegment cubic) commands.Add(("C", [cubic.Point1.X, cubic.Point1.Y, cubic.Point2.X, cubic.Point2.Y, cubic.Point3.X, cubic.Point3.Y]));
                else if (segment is LineSegment line) commands.Add(("L", [line.Point.X, line.Point.Y]));
                else throw new InvalidOperationException("Unexpected chart path segment.");
            if (figure.IsClosed) commands.Add(("Z", []));
        }
        var source = Regex.Matches(expected, "([MLCZ])([^MLCZ]*)").ToArray(); Require(source.Length == commands.Count, $"{id}: source/native command count differs.");
        for (var index = 0; index < source.Length; index++)
        {
            var values = source[index].Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray(); var command = commands[index];
            Require(source[index].Groups[1].Value == command.Kind && values.Length == command.Values.Length, $"{id}: source/native command kind differs.");
            // D3's frozen SVG producer rounds coordinates to three decimal places.
            for (var coordinate = 0; coordinate < values.Length; coordinate++) Require(Math.Abs(values[coordinate] - command.Values[coordinate]) <= .000501, $"{id}: source/native curve coordinate differs.");
        }
    }
    private static IReadOnlyDictionary<string, object?> Record(string label, double total, double selected) => new Dictionary<string, object?> { { "label", label }, { "total", total }, { "selected", selected } };
    private async Task AwaitAsync(Func<bool> condition, string message)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew(); while (!condition() && clock.Elapsed < TimeSpan.FromSeconds(2)) { UpdateLayout(); await Task.Delay(16); }
        Require(condition(), message);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
