using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture
{
    internal async Task VerifyScrollAsync(BleeckerColorScheme scheme, string folder)
    {
        var bytes = await ReadScrollReferenceAsync(); using var reference = JsonDocument.Parse(bytes);
        var table = new PaginationTable(50, pageSize: 10); var options = new ObservableCollection<int>([1,2,3,4,5,10,20,30,40,50,100,200,500]);
        var bar = new BleeckerDataTablePagination { Table = table, PageSizeOptions = options, Width = 800, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var root = (Grid)XamlRoot.Content; var host = new Grid { Background = root.Background }; Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); host.Children.Add(bar); root.Children.Add(host);
        using var input = new NativeInputSession(XamlRoot, folder, $"pagination-scroll-{scheme}"); var reports = new List<object>(); var failures = new List<string>();
        try
        {
            await ReadyAsync(() => bar.IsLoaded); bar.ApplyTemplate(); await input.AcquireAsync(bar);
            var combo = Chooser(bar); Require(combo.Focus(FocusState.Keyboard), "Could not focus the scrolling chooser.");
            Microsoft.UI.Xaml.Controls.Primitives.Popup Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(item => item.Child is not null && Descendants<Button>(item.Child).Any(option => combo.Items.Contains(option)));
            T Part<T>(string name) where T : FrameworkElement => Popup().Child is T rootPart && rootPart.Name == name ? rootPart : Descendants<T>(Popup().Child).Single(item => item.Name == name);
            string Describe(object? node) => node switch { Button item when item.Tag is int => "option:" + item.Content, _ when ReferenceEquals(node, combo) => "combobox", Control { Name: "PART_Listbox" } => "listbox", _ => node?.GetType().Name ?? "null" };
            foreach (var point in reference.RootElement.GetProperty("reports").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var phase = point.GetProperty("phase").GetString()!; var expected = point.GetProperty("state"); var errors = new List<string>(); object? receipt = null;
                switch (phase)
                {
                    case "opened": receipt = await PaginationKeyAsync(input, combo.Items.OfType<Button>().Single(item => (int)item.Tag == 10), "Space", Describe, errors); break;
                    case "end": receipt = await PaginationKeyAsync(input, combo.Items.OfType<Button>().Last(), "End", Describe, errors); break;
                    case "home":
                    case "home-after-type": receipt = await PaginationKeyAsync(input, combo.Items.OfType<Button>().First(), "Home", Describe, errors); break;
                    case "type-5": receipt = await PaginationKeyAsync(input,combo.Items.OfType<Button>().Single(item => (int)item.Tag == 5),"5",Describe,errors); break;
                    case "type-50": receipt = await PaginationKeyAsync(input,combo.Items.OfType<Button>().Single(item => (int)item.Tag == 50),"0",Describe,errors); break;
                    case "viewport-next": receipt = await PaginationKeyAsync(input,combo.Items.OfType<Button>().First(),"ArrowDown",Describe,errors); break;
                    case "hover-down-step":
                    case "hover-bottom":
                    case "hover-top":
                    case "hover-down-again":
                    case "hover-top-again":
                        var goingUp = phase.StartsWith("hover-top",StringComparison.Ordinal);
                        await input.MoveAsync(Part<Border>(goingUp ? "PART_ScrollUp" : "PART_ScrollDown"));
                        await ReadyAsync(() => { var scroll = Part<ScrollViewer>("ScrollViewer"); return goingUp ? scroll.VerticalOffset == 0 : phase == "hover-bottom" ? Math.Ceiling(scroll.VerticalOffset) >= scroll.ScrollableHeight : scroll.VerticalOffset > 0; });
                        break;
                    case "left-indicator":
                        // This owned modal popup blocks pointer delivery to the
                        // underlying bar. Leave its indicator through the painted
                        // top border, retaining a real owned pointer receipt.
                        var surface = Part<Border>("PopupBorder"); await input.MoveAsync(surface,localPoint:new Windows.Foundation.Point(surface.ActualWidth/2,.5)); break;
                    case "shrink-open": bar.PageSizeOptions = new[] { 10,20 }; break;
                    case "closed": receipt = await PaginationKeyAsync(input, combo, "Escape", Describe, errors); break;
                    case "restored-closed": bar.PageSizeOptions = options; break;
                    case "closed-type-5": receipt = await PaginationKeyAsync(input,combo,"5",Describe,errors); break;
                    case "closed-type-50": receipt = await PaginationKeyAsync(input,combo,"0",Describe,errors); break;
                    default: throw new InvalidOperationException("Unknown source scroll phase.");
                }
                var open = expected.GetProperty("popupOpen").GetBoolean(); await ReadyAsync(() => ChooserIsOpen(combo) == open);
                await MainWindow.AwaitPresentationAsync(open ? Part<Border>("PopupBorder") : combo);
                void Equal(bool value, string message) { if (!value) errors.Add(message); }
                void Near(double actual, double wanted, string message) => Equal(double.IsFinite(actual) && Math.Abs(actual - wanted) <= 1d/32, $"{message}: {actual:R}/{wanted:R}");
                Equal(Describe(FocusManager.GetFocusedElement(XamlRoot)) == expected.GetProperty("focused").GetString(), "Source focus differs.");
                var sourcePagination = expected.GetProperty("table").GetProperty("pagination");
                Equal(ReferenceEquals(bar.Table, table) && table.PageIndex == sourcePagination.GetProperty("pageIndex").GetInt32() && table.PageSize == sourcePagination.GetProperty("pageSize").GetInt32() && table.Requests.Count == expected.GetProperty("events").GetArrayLength() && table.VisibleText == expected.GetProperty("visibleRows").GetString() && table.VisibleRows.All(row => table.Rows.Any(original => ReferenceEquals(original,row))), "Original table/rows or callbacks differ.");
                var value = (IValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(combo).GetPattern(PatternInterface.Value); Equal(value.IsReadOnly && value.Value == expected.GetProperty("displayed").GetString(),"Accessible controlled value differs.");
                object? measured = null;
                if (open)
                {
                    var border = Part<Border>("PopupBorder"); var scroll = Part<ScrollViewer>("ScrollViewer"); var up = Part<Border>("PART_ScrollUp"); var down = Part<Border>("PART_ScrollDown");
                    Near(border.ActualWidth, expected.GetProperty("popup").GetProperty("width").GetDouble(), "Popup width"); Near(border.ActualHeight, expected.GetProperty("popup").GetProperty("height").GetDouble(), "Popup height");
                    var borderBox = Bounds(border,root); var chooserBox = Bounds(combo,root); Near(borderBox.Left,chooserBox.Left,"Popup start alignment"); Near(borderBox.Top-chooserBox.Bottom,4,"Popup trigger gap");
                    var row = Descendants<Button>(Popup().Child).First(); Near(row.ActualHeight,36,"Painted row height"); Near(row.ActualWidth,70,"Painted row width");
                    var offsetPixels = 4 + combo.Items.IndexOf(row)*row.ActualHeight-Bounds(row,scroll).Top;
                    var extentPixels = combo.Items.Count*row.ActualHeight+8;
                    var viewport = expected.GetProperty("viewport"); Near(scroll.ActualHeight, viewport.GetProperty("box").GetProperty("height").GetDouble(), "Viewport height"); Near(offsetPixels, viewport.GetProperty("scrollTop").GetDouble(), "Painted viewport offset"); Near(extentPixels, viewport.GetProperty("scrollHeight").GetDouble(), "Painted viewport extent");
                    foreach (var (indicator, field) in new[] { (up,"up"),(down,"down") }) { var visible = expected.GetProperty(field).ValueKind != JsonValueKind.Null; Equal((indicator.Visibility == Visibility.Visible) == visible, field + " availability differs."); if (visible) { Near(indicator.ActualHeight,28,field+" height"); Near(indicator.ActualWidth,78,field+" width"); } }
                    measured = new { popup=Bounds(border,root),viewport=Bounds(scroll,root),scroll.VerticalOffset,scroll.ExtentHeight,scroll.ViewportHeight,up=up.Visibility.ToString(),down=down.Visibility.ToString(),borderHeight=border.Height.ToString(),borderMinHeight=border.MinHeight,borderMaxHeight=border.MaxHeight.ToString(),borderDesired=border.DesiredSize,childDesired=border.Child.DesiredSize,childHeight=((FrameworkElement)border.Child).Height.ToString(),childMinHeight=((FrameworkElement)border.Child).MinHeight,childMaxHeight=((FrameworkElement)border.Child).MaxHeight.ToString(),scrollHeight=scroll.Height.ToString(),scrollDesired=scroll.DesiredSize };
                }
                var file = $"data-table-pagination-scroll-{phase}-{scheme}.png"; await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder,file));
                reports.Add(new { phase,errors,receipt,measured,focus=Describe(FocusManager.GetFocusedElement(XamlRoot)),callbacks=table.Requests.Count,file }); failures.AddRange(errors.Select(error => phase+": "+error));
                Require(errors.Count == 0,"Pagination scroll state differs; see its captured source/native report.");
            }
        }
        catch (Exception error) { failures.Add(error.ToString()); throw; }
        finally
        {
            root.Children.Remove(host);
            await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-pagination-scroll-{scheme}.json"),JsonSerializer.Serialize(new { passed=failures.Count==0,nativeComponentAccepted=false,referenceSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant(),reports,failures },new JsonSerializerOptions { WriteIndented=true }));
        }
    }
    private static async Task<byte[]> ReadScrollReferenceAsync()
    { using var stream = typeof(DataTablePaginationsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataTablePaginationScroll.json")!; using var bytes = new MemoryStream(); await stream.CopyToAsync(bytes); return bytes.ToArray(); }
}
