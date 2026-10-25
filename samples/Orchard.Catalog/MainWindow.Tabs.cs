using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTabsReferenceAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Tabs.json")
            ?? throw new InvalidOperationException("Tabs reference is missing.");
        using var document = await JsonDocument.ParseAsync(stream);
        var previous = Content;
        var previousSize = AppWindow.ClientSize;
        var canvas = new Canvas { Width = 640, Height = 240, UseLayoutRounding = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var frames = new List<object>();
        object Box(FrameworkElement element)
        {
            var point = element.TransformToVisual(canvas).TransformPoint(new());
            return new { x = point.X, y = point.Y, width = element.ActualWidth, height = element.ActualHeight };
        }
        object Paint(Brush? value)
        {
            if (value is null) return new { r = 0, g = 0, b = 0, a = 0, opacity = 1d };
            if (value is not SolidColorBrush brush) throw new InvalidOperationException("Tabs reference requires a solid paint.");
            return new { r = (int)brush.Color.R, g = (int)brush.Color.G, b = (int)brush.Color.B, a = (int)brush.Color.A, opacity = brush.Opacity };
        }
        try
        {
            Content = canvas;
            if (AppWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter presenter) presenter.Restore(false);
            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(640, 240));
            await AwaitPresentationAsync(canvas);
            Require(AppWindow.ClientSize.Width == 640 && AppWindow.ClientSize.Height == 240, "Tabs reference requires a 640 by 240 client.");
            foreach (var row in document.RootElement.GetProperty("cases").EnumerateArray())
            {
                var theme = Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                canvas.RequestedTheme = theme;
                canvas.Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                var spec = row.GetProperty("spec");
                var control = new BleeckerTabs { Width = 480, UseLayoutRounding = false };
                if (spec.TryGetProperty("variant", out var variant)) control.Variant = Enum.Parse<BleeckerTabsVariant>(variant.GetString()!, true);
                if (spec.TryGetProperty("size", out var size)) control.Size = size.GetString() switch { "sm" => BleeckerControlSize.Small, "md" => BleeckerControlSize.Medium, "lg" => BleeckerControlSize.Large, _ => throw new InvalidOperationException("Unknown source Tabs size.") };
                if (spec.TryGetProperty("stretch", out var stretch)) control.Stretch = stretch.GetBoolean();
                if (spec.TryGetProperty("activeTab", out var active)) control.ActiveTab = active.GetString();
                if (spec.TryGetProperty("customLink", out var customLink) && customLink.GetBoolean()) control.LinkTemplate = (DataTemplate)Tabs.Resources["ConsumerTabLink"];
                control.Items = new ObservableCollection<BleeckerTabItem>(spec.GetProperty("tabs").EnumerateArray().Select(item => new BleeckerTabItem(
                    item.GetProperty("id").GetString()!, item.GetProperty("label").GetString()!,
                    item.TryGetProperty("href", out var href) ? href.GetString() : null,
                    item.TryGetProperty("external", out var external) && external.GetBoolean(),
                    item.TryGetProperty("panelId", out var panelId) ? panelId.GetString() : null)));
                AutomationProperties.SetName(control, "Component tabs");
                Canvas.SetLeft(control, 20); Canvas.SetTop(control, 20);
                canvas.Children.Clear(); canvas.Children.Add(control);
                await AwaitPresentationAsync(control);
                var frame = Desc<Border>(control).Single(border => border.Name == "TabsFrame");
                var buttons = Desc<Button>(control).ToArray();
                var items = buttons.Select(button =>
                {
                    var border = Desc<Border>(button).Single(value => value.Name == "TabBorder");
                    var label = Desc<TextBlock>(button).Single(value => value.Name == "TabLabel");
                    return new
                    {
                        box = Box(button), text = label.Text, foreground = Paint(label.Foreground), background = Paint(border.Background), border = Paint(border.BorderBrush),
                        fontSize = button.FontSize, lineHeight = label.LineHeight, radius = border.CornerRadius.TopLeft,
                        padding = button.Padding, borderThickness = border.BorderThickness, label = Box(label),
                        labelForeground = Paint(label.Foreground), label.FontFamily.Source, label.FontWeight.Weight
                    };
                }).ToArray();
                frames.Add(new { id = spec.GetProperty("id").GetString(), scheme = theme.ToString(), measurement = new { root = new { box = Box(frame), background = Paint(frame.Background), border = Paint(frame.BorderBrush), radius = frame.CornerRadius.TopLeft }, items } });
                if (row.TryGetProperty("file", out var png)) await NativeScreenCapture.CaptureClientAsync(Microsoft.UI.Win32Interop.GetWindowFromWindowId(AppWindow.Id), Path.Combine(folder, png.GetString()!));
            }
            await File.WriteAllTextAsync(Path.Combine(folder, "tabs-reference.json"), JsonSerializer.Serialize(new { passed = true, frames }, new JsonSerializerOptions { WriteIndented = true }));
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "tabs-reference", environment = RuntimeEnvironment() }));
        }
        finally { Content = previous; AppWindow.ResizeClient(previousSize); await AwaitPresentationAsync(CatalogRoot); }
    }
}
