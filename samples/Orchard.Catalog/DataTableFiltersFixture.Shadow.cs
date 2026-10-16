using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class DataTableFiltersFixture
{
    internal async Task VerifyShadowPointerAsync(BleeckerColorScheme scheme, string folder)
    {
        using var stream = typeof(DataTableFiltersFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.SelectShadowPointer.json")!;
        using var buffer = new MemoryStream(); await stream.CopyToAsync(buffer); var bytes = buffer.ToArray(); using var document = JsonDocument.Parse(bytes);
        var sample = document.RootElement.GetProperty("cases").EnumerateArray().Single(item => item.GetProperty("scheme").GetString() == scheme.ToString());
        var root = (Grid)XamlRoot.Content; var table = new SelectFilterTable();
        var view = new BleeckerDataTableSelectFilter { Table = table, ColumnId = "status", Options = new[] { new BleeckerSelectOption("Draft", "Draft"), new BleeckerSelectOption("Published", "Published") }, Width = 520, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(16) };
        var outsideClicks = 0;
        var outside = new Button { Name = "ShadowOutside", Width = 16, Height = 16, MinWidth = 0, MinHeight = 0, Padding = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(192,178,0,0), Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><Border Background='Transparent'/></ControlTemplate>") };
        AutomationProperties.SetName(outside, "Shadow outside target"); outside.Click += (_, _) => outsideClicks++;
        var rows = new TextBlock { Text = table.VisibleText, Margin = new Thickness(16,88,16,0), VerticalAlignment = VerticalAlignment.Top };
        var host = new Grid { Background = root.Background }; host.Children.Add(view); host.Children.Add(outside); host.Children.Add(rows); Grid.SetRowSpan(host, Math.Max(1, root.RowDefinitions.Count)); Grid.SetColumnSpan(host, Math.Max(1, root.ColumnDefinitions.Count)); root.Children.Add(host);
        using var input = new NativeInputSession(XamlRoot, folder, $"select-shadow-pointer-{scheme}"); var reports = new List<object>(); string? failure = null; BleeckerOverlaySurface? surface = null;
        try
        {
            await ReadyAsync(() => view.IsLoaded && table.Subscribers == 1); view.ApplyTemplate(); await MainWindow.AwaitPresentationAsync(view); await input.AcquireAsync(outside); outsideClicks = 0;
            var select = Select(view);
            Popup Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Single(item => item.Child is Border { Name: "PopupBorder" });
            foreach (var point in sample.GetProperty("snapshots").EnumerateArray())
            {
                var phase = point.GetProperty("phase").GetString()!; var expected = point.GetProperty("state"); object? receipt = null;
                if (phase == "open") await input.ClickAsync(select);
                if (phase == "outside") receipt = await input.DismissModalPointerSourceOutsideAsync(select, Popup(), outside);
                var open = expected.GetProperty("popupOpen").GetBoolean(); await ReadyAsync(() => (Provider(select).ExpandCollapseState == ExpandCollapseState.Expanded) == open); await MainWindow.AwaitPresentationAsync(view);
                var focused = FocusManager.GetFocusedElement(XamlRoot); var role = ReferenceEquals(focused, select) ? "trigger" : focused is Button button && select.Items.Contains(button) ? "option:" + select.Items.IndexOf(button) : focused?.GetType().Name ?? "BODY";
                if (phase != "closed") Require(role == expected.GetProperty("focused").GetString(), "Shadow outside click lost the original focus role.");
                Require(table.Requests.Count == 0 && outsideClicks == 0 && rows.Text == expected.GetProperty("visibleRows").GetString() && ReferenceEquals(view.Table, table), "Shadow outside click leaked a consumer callback or replaced its original table/rows.");
                object? shadow = null; bool? shadowClosed = null;
                if (open)
                {
                    await MainWindow.AwaitPresentationAsync((FrameworkElement)Popup().Child); var box = Bounds((FrameworkElement)Popup().Child, root);
                    Require(box.X == 16 && box.Y == 60 && box.Width == 176 && box.Height == 118 && !box.Contains(new Windows.Foundation.Point(200,186)), "Shadow input changed the source popup box or outside point.");
                    surface = Descendants<BleeckerOverlaySurface>(Popup().Child).Single(item => item.Name == "PopupSurface"); shadow = DescribeShadow((FrameworkElement)Popup().Child);
                }
                else if (surface is not null) { shadowClosed = ShadowIsCleared(surface); Require(shadowClosed.Value, "Shadow input dismissal retained its Composition layers."); }
                var target = Bounds(outside, root); Require(target.X == 192 && target.Y == 178 && target.Width == 16 && target.Height == 16, "Shadow input lost its actual source target rectangle.");
                var file = $"select-shadow-pointer-{phase}-{scheme}.png"; await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder,file)); reports.Add(new { phase, file, open, focus = role, outsideClicks, requests = table.Requests.Count, rows = rows.Text, target, shadow, shadowClosed, receipt });
            }
        }
        catch (Exception error) { failure = error.ToString(); throw; }
        finally
        {
            root.Children.Remove(host); await ReadyAsync(() => !view.IsLoaded && table.Subscribers == 0);
            await File.WriteAllTextAsync(Path.Combine(folder,$"select-shadow-pointer-{scheme}.json"),JsonSerializer.Serialize(new { passed = failure is null && reports.Count == 3, failure, referenceSha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant(), client = NativeScreenCapture.ClientBounds(input.Window), finalObservers = table.Subscribers, reports }));
        }
    }

    private static bool ShadowIsCleared(BleeckerOverlaySurface surface)
    {
        var host = Descendants<Grid>(surface).Single(item => item.Name == "PART_ShadowHost");
        return !surface.IsLoaded && ElementCompositionPreview.GetElementChildVisual(host) is null;
    }

    private static object DescribeShadow(FrameworkElement popup)
    {
        var surface = Descendants<BleeckerOverlaySurface>(popup).Single(item => item.Name == "PopupSurface");
        return OverlayShadowProbe.Read(surface);
    }
}
