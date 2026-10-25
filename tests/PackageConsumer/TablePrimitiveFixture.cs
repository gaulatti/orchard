using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class TablePrimitiveFixture
{
    private delegate bool Visitor(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var root = (Grid)XamlReader.Load("<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"900\" Height=\"760\" Background=\"{ThemeResource BleeckerBackgroundBrush}\"/>");
        window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900, 760));
        var cases = new List<object>(); var pngs = new List<object>(); var stage = "initial"; var guards = 0; var focus = 0; var activations = 0;
        root.GotFocus += (_, _) => focus++; window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((h, _) => { GetWindowThreadProcessId(h, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(h); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Table probe left its hidden boundary.");
        }
        async Task Layout()
        {
            for (var i = 0; i < 8; i++) { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain table layout."); await idle.Task; root.Measure(new Size(900, 760)); root.Arrange(new Rect(0, 0, 900, 760)); root.UpdateLayout(); Guard(); await Task.Delay(15); }
        }
        object Box(FrameworkElement element, UIElement relative)
        {
            var box = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            var target = (FrameworkElement)relative;
            var origin = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
            return new { x = box.X - origin.X, y = box.Y - origin.Y, width = box.Width, height = box.Height };
        }
        async Task Capture(string file)
        {
            Guard(); var bitmap = new RenderTargetBitmap(); await bitmap.RenderAsync(root); Guard();
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync()); using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0); using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes); await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes);
            pngs.Add(new { file, width = bitmap.PixelWidth, height = bitmap.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); Guard();
        }
        try
        {
            foreach (var scheme in new[] { "Light", "Dark" }) foreach (var variant in new[] { "plain", "selected", "disabled", "overflow", "rtl", "rowspan", "empty" })
            {
                stage = scheme + "-" + variant + "-setup"; root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
                var table = new BleeckerTable { Width = variant == "overflow" ? 320 : 660, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(20), FlowDirection = variant == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                var caption = new BleeckerTableCaption { Content = "Original fictional caption" }; table.Sections.Add(caption);
                var header = new BleeckerTableHeader(); var head = new BleeckerTableRow(); foreach (var text in new[] { "Original heading", "Count", "Original action" }) head.Cells.Add(new BleeckerTableHead { Content = text }); header.Children.Add(head);
                var body = new BleeckerTableBody(); var first = new BleeckerTableRow(); var second = new BleeckerTableRow(); var footer = new BleeckerTableFooter(); var foot = new BleeckerTableRow();
                var original = new Button { Content = "Inspect fictional item", MinWidth = 0, MinHeight = 0, Padding = new Thickness(), BorderThickness = new Thickness(), IsEnabled = variant != "disabled", Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TargetType=\"Button\"><TextBlock Text=\"{TemplateBinding Content}\" FontFamily=\"{TemplateBinding FontFamily}\" FontSize=\"{TemplateBinding FontSize}\" Foreground=\"{TemplateBinding Foreground}\" TextWrapping=\"WrapWholeWords\" LineHeight=\"20\" LineStackingStrategy=\"BlockLineHeight\"/></ControlTemplate>") };
                var clicks = 0; original.Click += (_, _) => clicks++;
                var source = new ToggleSwitch { IsOn = variant == "selected" }; BindingOperations.SetBinding(first, BleeckerTableRow.IsSelectedProperty, new Binding { Source = source, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)) });
                var originalText = variant == "overflow" ? (object)new TextBlock { Text = "Original retained content", Width = 520 } : "Original retained content";
                first.Cells.Add(new BleeckerTableCell { Content = originalText, RowSpan = variant == "rowspan" ? 2 : 1 }); first.Cells.Add(new BleeckerTableCell { Content = "12" }); first.Cells.Add(new BleeckerTableCell { Content = original });
                second.Cells.Add(new BleeckerTableCell { Content = "Two-column original cell", ColumnSpan = variant == "rowspan" ? 1 : 2 }); var checkbox = new BleeckerCheckbox { IsChecked = true }; AutomationProperties.SetName(checkbox, "Original cell checkbox"); second.Cells.Add(new BleeckerTableCell { Content = checkbox }); body.Children.Add(first); body.Children.Add(second);
                foot.Cells.Add(new BleeckerTableCell { Content = "Original fictional footer", ColumnSpan = 3 }); footer.Children.Add(foot);
                if (variant != "empty") { table.Sections.Add(header); table.Sections.Add(body); table.Sections.Add(footer); }
                root.Children.Add(table); stage = scheme + "-" + variant + "-mount"; await Layout();
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(table); Require(peer.GetAutomationControlType() == AutomationControlType.Table && peer.GetName() == "Original fictional caption", "Table role or original caption name changed.");
                var grid = (IGridProvider)peer.GetPattern(PatternInterface.Grid); var headers = (ITableProvider)peer.GetPattern(PatternInterface.Table);
                Require(grid.RowCount == (variant == "empty" ? 0 : 4) && grid.ColumnCount == (variant == "empty" ? 0 : 3) && headers.GetColumnHeaders().Length == (variant == "empty" ? 0 : 3), "Native table grid/header dimensions are wrong.");if(variant!="empty")Require(foot.FontWeight==Microsoft.UI.Text.FontWeights.Medium,"The footer did not supply its source medium weight.");
                var rows = variant == "empty" ? Array.Empty<BleeckerTableRow>() : new[] { head, first, second, foot };
                foreach (var row in rows) { var rowPeer = FrameworkElementAutomationPeer.CreatePeerForElement(row); Require(rowPeer.GetChildren().Count == row.Cells.Count, "Table row lost original cell peers."); foreach (var cell in row.Cells) { var cellPeer = FrameworkElementAutomationPeer.CreatePeerForElement(cell); if(cell.Content is string label)Require(cellPeer.GetName()==label,"Cell lost original accessible text.");if(ReferenceEquals(cell.Content,original))Require(cellPeer.GetName()=="Inspect fictional item","Original native action name was lost."); Require(ReferenceEquals(cellPeer.GetParent(), rowPeer), "Native cell lost its original row parent."); var item = (IGridItemProvider)cellPeer.GetPattern(PatternInterface.GridItem); Require(item.ColumnSpan == cell.ColumnSpan && item.RowSpan == (cell.RowSpan == 2 ? 2 : 1) && item.ContainingGrid is not null && grid.GetItem(item.Row, item.Column) is not null, "Native cell span/provider metadata is wrong."); } }
                if (variant != "empty") Require(original.IsLoaded && checkbox.IsLoaded && ReferenceEquals(first.Cells[2].Content, original) && first.IsSelected == source.IsOn && original.IsEnabled == (variant != "disabled"), "Table replaced content, a live binding, or caller enabled state.");
                if (variant == "rtl") { var a = first.Cells[0].TransformToVisual(root).TransformBounds(new Rect(0,0,first.Cells[0].ActualWidth,first.Cells[0].ActualHeight)); var b = first.Cells[2].TransformToVisual(root).TransformBounds(new Rect(0,0,first.Cells[2].ActualWidth,first.Cells[2].ActualHeight)); Require(a.X > b.X, "RTL table did not reverse physical column order."); }
                var snapshot = new { table = Box(table, root), caption = Box(caption, table), rows = rows.Select(r => new { box = Box(r, table), cells = r.Cells.Select(c => new { box = Box(c, table), content = c.Content as string, columnSpan = c.ColumnSpan, rowSpan = c.RowSpan, fontSize = c.FontSize, fontFamily = c.FontFamily.Source, lineHeight = c.LineHeight }).ToArray() }).ToArray() };
                await Capture($"table-native-{scheme}-{variant}.png");
                var liveContent = false; var invocation = false; var scrolling = false;
                if (variant == "plain")
                {
                    stage = scheme + "-live-original-content"; var binding = first.ReadLocalValue(BleeckerTableRow.IsSelectedProperty); source.IsOn = true; await Layout(); Require(first.IsSelected && ReferenceEquals(first.ReadLocalValue(BleeckerTableRow.IsSelectedProperty), binding), "Selected-row binding was replaced.");
                    ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(original).GetPattern(PatternInterface.Invoke)).Invoke(); await Layout(); Require(clicks == 1 && ReferenceEquals(first.Cells[2].Content, original), "Original native action did not run once."); invocation = true;
                    var replacement = new TextBlock { Text = "Original replacement content", Width = 260 }; first.Cells[2].Content = replacement; await Layout(); Require(replacement.IsLoaded && !original.IsLoaded, "Cell replacement did not release its original content."); first.Cells[2].Content = original; await Layout(); Require(original.IsLoaded && !replacement.IsLoaded, "Original native cell content did not survive restoration."); liveContent = true;
                    stage = scheme + "-template-retirement"; var originalTemplate = table.Template; table.Template = null; await Layout(); Require(!original.IsLoaded && grid.RowCount == 0 && peer.GetChildren().Count == 0, "Retired table template retained original content or grid metadata."); table.Template = originalTemplate; await Layout(); Require(original.IsLoaded && grid.RowCount == 4 && ReferenceEquals(first.Cells[2].Content, original) && first.IsSelected, "Original table content or bound state did not survive template restoration.");
                    var baselineButton=new Button{Content="Stock original content"};var stock=new ContentControl{Content=baselineButton,Width=60,Height=60,HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Top,Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" TargetType=\"ContentControl\"><ContentPresenter Content=\"{TemplateBinding Content}\"/></ControlTemplate>")};root.Children.Add(stock);await Layout();Require(baselineButton.IsLoaded,"Stock template baseline did not load.");stock.Template=null;await Layout();var stockKeepsContent=baselineButton.IsLoaded;
                    var originalCellTemplate = first.Cells[2].Template; first.Cells[2].Template = null; await Layout(); Require(original.IsLoaded==stockKeepsContent&&ReferenceEquals(first.Cells[2].Content,original), $"Cell template clearing disagrees with installed native ContentControl: stock={stockKeepsContent}; cell={original.IsLoaded}; templateNull={first.Cells[2].Template is null}.");root.Children.Remove(stock);await Layout(); first.Cells[2].Template = originalCellTemplate; await Layout(); Require(original.IsLoaded && ReferenceEquals(first.Cells[2].Content, original), "Original cell did not survive template restoration.");
                }
                if (variant == "overflow") { stage = scheme + "-native-scroll"; Require(table.ScrollableWidth > 100, "Intrinsic table width did not produce native horizontal overflow."); table.ScrollTo(50); await Layout(); Require(table.HorizontalOffset > 0 && ReferenceEquals(first.Cells[0].Content, originalText), "Native scrolling lost original cell content."); scrolling = true; }
                root.Children.Clear(); await Layout(); Require(!table.IsLoaded && !original.IsLoaded && !checkbox.IsLoaded, "Native table retained loaded original content after removal.");
                cases.Add(new { scheme, variant, passed = true, originalContentRetained = true, originalRowBindingRetained = true, nativeGridAndSpanMetadataVerified = true, originalItemPeerParentsVerified = true, nativeRtlOrderVerified = variant == "rtl", liveContentReplacementVerified = liveContent, originalNativeInvocationVerified = invocation, nativeScrollingVerified = scrolling, originalTemplateRestorationVerified = variant == "plain", originalContentUnloaded = true, snapshot });
            }
            string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-table-primitives-native-v1", passed = true, cases, pngs, guards, focus, activations, publicApisOnly = true, activateCalled = false, showWindowCalled = false, focusCalled = false, hardwareInputSent = false, physicalInputVerified = false, sourceGeometryVerified = false, sourceRasterParityVerified = false, dataTableCompositionImplemented = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerTable).Assembly.Location), hostSha256 = Hash(hostPath) }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(0);
        }
        catch (Exception error) { await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-table-primitives-native-v1", passed = false, stage, error = error.ToString(), cases, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(1); }
    }
}
