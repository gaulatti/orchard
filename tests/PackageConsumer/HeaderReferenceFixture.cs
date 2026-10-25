using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Orchard;

namespace Orchard.Testing;

internal static partial class HeaderReferenceFixture
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private sealed class Item : BleeckerNavItem { internal string? Marker; }
    private sealed class Host(BleeckerHeader header, FrameworkElement following) : Panel
    {
        internal void Mount() { Children.Add(header); Children.Add(following); }
        protected override Size MeasureOverride(Size available) { header.Measure(new(Width, double.PositiveInfinity)); following.Measure(new(Width, 1600)); return new(Width, Height); }
        protected override Size ArrangeOverride(Size final) { header.Arrange(new(0, 0, final.Width, header.DesiredSize.Height)); following.Arrange(new(0, header.DesiredSize.Height, final.Width, 1600)); return final; }
    }
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++) foreach (var nested in Elements(VisualTreeHelper.GetChild(node, index))) yield return nested;
    }
    private static double Value(JsonElement spec, string key, double otherwise) => spec.TryGetProperty(key, out var value) ? value.GetDouble() : otherwise;
    private static bool Flag(JsonElement spec, string key) => spec.TryGetProperty(key, out var value) && value.GetBoolean();
    private static object? Brush(Brush? value) => value is SolidColorBrush solid ? new { color = $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}", opacity = solid.Opacity } : null;
    internal static async Task RunAsync(Window window, string referencePath, string reportPath, string hostPath)
    {
        var referenceBytes = await File.ReadAllBytesAsync(referencePath); using var reference = JsonDocument.Parse(referenceBytes); var root = new Canvas { Width = 1280, Height = 900, UseLayoutRounding = false }; window.Content = root;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window); var guards = 0; var activations = 0; var focus = 0; var frames = new List<object>(); string? failure = null;
        window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1280, 900));
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); if (window.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle) || foreground == Environment.ProcessId || activations != 0 || focus != 0) throw new InvalidOperationException("Hidden Header acquired visibility, activation or focus."); }
        async Task Layout() { for (var index = 0; index < 5; index++) { root.Measure(new(root.Width, 900)); root.Arrange(new(0, 0, root.Width, 900)); root.UpdateLayout(); Guard(); await Task.Delay(15); } if (root.XamlRoot is not { IsHostVisible: false, RasterizationScale: 1 } || root.XamlRoot.Size.Width != root.Width || root.XamlRoot.Size.Height != 900) throw new InvalidOperationException("Header requires the actual hidden unscaled source viewport."); }
        object Box(FrameworkElement element)
        {
            for (DependencyObject? node = element; node is not null; node = VisualTreeHelper.GetParent(node)) if (node is FrameworkElement { Visibility: Visibility.Collapsed }) return new { x = 0d, y = 0d, width = 0d, height = 0d };
            var bounds = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height };
        }
        try
        {
            foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.Children.Clear(); await Layout();
                var spec = source.GetProperty("spec"); root.Width = Value(spec, "viewportWidth", 1280); window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)root.Width, 900)); root.RequestedTheme = Enum.Parse<ElementTheme>(source.GetProperty("scheme").GetString()!);
                var brandSpec = spec.GetProperty("brand"); var brand = new BleeckerHeaderBrand { Href = brandSpec.GetProperty("href").GetString()!, BrandName = brandSpec.TryGetProperty("name", out var name) ? name.GetString() : null, LogoAlt = brandSpec.GetProperty("logoAlt").GetString()!, Size = brandSpec.TryGetProperty("size", out var size) && size.GetString() == "lg" ? BleeckerBrandLockupSize.Large : BleeckerBrandLockupSize.Small };
                var navigation = new ObservableCollection<BleeckerNavItem>(spec.GetProperty("navigation").EnumerateArray().Select(item => new Item { Href = item.GetProperty("href").GetString()!, Label = item.GetProperty("label").GetString()!, Marker = item.GetProperty("marker").GetString() }));
                object? Slot(bool mobile)
                {
                    var key = mobile ? "mobileActionWidth" : "actionWidth"; if (!spec.TryGetProperty(key, out var width) || width.ValueKind is JsonValueKind.Null or JsonValueKind.False) return null;
                    if (Flag(spec, "input")) return new TextBox { Width = width.GetDouble(), Height = 32, MinHeight = 0, Text = "Original", Padding = new Thickness(0), UseLayoutRounding = false };
                    return new Border { Width = width.GetDouble(), Height = 32, UseLayoutRounding = false, Child = new TextBlock { Text = mobile ? "Mobile action" : "Caller action", FontSize = 16, LineHeight = 24, LineStackingStrategy = LineStackingStrategy.BlockLineHeight } };
                }
                var header = new BleeckerHeader { Brand = brand, Navigation = navigation, Actions = Slot(false), MobileActions = Slot(true), FullWidth = Flag(spec, "fullWidth"), Position = spec.TryGetProperty("position", out var position) ? Enum.Parse<BleeckerHeaderPosition>(position.GetString()!, true) : BleeckerHeaderPosition.Fixed, HeaderHeight = Value(spec, "headerHeight", 72), FlowDirection = Flag(spec, "rtl") ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                if (Flag(spec, "custom")) header.LinkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><HyperlinkButton Content='{Binding Content}' Command='{Binding Command}' Style='{Binding Style}' Foreground='{Binding Foreground}' Tag='{Binding Item}' AutomationProperties.Name='{Binding Label}'/></DataTemplate>");
                var following = new Border { Height = 1600 }; var host = new Host(header, following) { Width = Value(spec, "width", root.Width), Height = Value(spec, "hostHeight", 2000), UseLayoutRounding = false }; host.Mount(); Canvas.SetLeft(host, Value(spec, "offsetX", 0)); Canvas.SetTop(host, Value(spec, "offsetY", 0) - Value(spec, "scrollY", 0)); root.Children.Add(host); await Layout();
                var brandRenderer = Elements(header).OfType<BleeckerBrandLockup>().Single(); var decodeDeadline = System.Diagnostics.Stopwatch.StartNew();
                while (brandRenderer.LogoSource is not null && !brandRenderer.IsLogoLoaded) { if (decodeDeadline.ElapsedMilliseconds > 5000) throw new TimeoutException("The actual Header logo decoder did not become ready."); await Layout(); }
                await Layout();
                if (Flag(spec, "open")) { header.ToggleNavigation(); await Task.Delay(230); await Layout(); }
                var elements = Elements(header).ToArray(); FrameworkElement Part(string part) => elements.FirstOrDefault(element => element.Name == part) ?? throw new InvalidOperationException("Missing Header part " + part);
                var measurement = new Dictionary<string, object?> { ["host"] = Box(host), ["following"] = Box(following), ["header"] = Box(Part("HeaderSurface")), ["container"] = Box(Part("HeaderContainer")), ["row"] = Box(Part("HeaderRow")), ["brand"] = Box(Part("HeaderBrand")), ["desktopNavigation"] = Box(Part("HeaderDesktopNavigation")), ["actions"] = Box(Part("HeaderActions")), ["toggle"] = Box(Part("HeaderToggle")), ["panel"] = Box(Part("HeaderMobilePanel")), ["mobileContent"] = Box(Part("HeaderMobileContent")), ["mobileActions"] = header.MobileActions is FrameworkElement original ? Box(original) : null };
                var links = elements.OfType<HyperlinkButton>().Where(link => link.DataContext is BleeckerNavLinkContext).Select(link => { var context = (BleeckerNavLinkContext)link.DataContext; return new { href = context.Item.Href, label = context.Item.Label, marker = (context.Item as Item)?.Marker, box = Box(link), paint = Brush(link.Foreground) }; }).ToArray();
                var paint = new { header = Brush(header.Background), bottomRule = Brush(header.BorderBrush), mobile = Brush(((Border)Part("HeaderMobileBackground")).Background), mobileRule = Brush(((Border)Part("HeaderMobileTopRule")).Background) };
                var renderPaint = new { header = Brush(((Border)Part("HeaderBackground")).Background), bottomRule = Brush(((Border)Part("HeaderBottomRule")).Background) };
                var iconCanvas = (((Part("HeaderToggle") as ContentControl)?.Content as ContentControl)?.Content as Viewbox)?.Child as Canvas;
                frames.Add(new { id = source.GetProperty("id").GetString(), scheme = source.GetProperty("scheme").GetString(), actualTheme = header.ActualTheme.ToString(), menuOpen = header.IsMenuOpen, flowHeight = header.DesiredSize.Height, measurement, links, paint, renderPaint, icon = iconCanvas?.Children.OfType<Microsoft.UI.Xaml.Shapes.Line>().Select(line => new[] { line.X1, line.Y1, line.X2, line.Y2 }).ToArray() }); Guard();
            }
        }
        catch (Exception error) { failure = error.ToString(); }
        string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!); await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new { passed = failure is null, diagnosticOnly = true, scope = "hidden-header-source-layout", failure, frames, guardChecks = guards, activations, focus, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), referenceSha256 = Convert.ToHexStringLower(SHA256.HashData(referenceBytes)), hostSha256 = Hash(hostPath), librarySha256 = Hash(typeof(BleeckerHeader).Assembly.Location), actualNativeScrollVerified = false, visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failure is null ? 0 : 1);
    }
}
