using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Rectangle = Microsoft.UI.Xaml.Shapes.Rectangle;
using Windows.Foundation;

namespace Orchard.Testing;

// Compare native scrolling capabilities under an unshown window. This is not
// the ScrollArea component and does not validate its scrollbar styling/input.
internal static class ScrollAreaPrimitiveProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    internal static async Task RunAsync(Window window, string output)
    {
        var root = new Canvas { Width = 800, Height = 700, Background = new SolidColorBrush(Microsoft.UI.Colors.White) }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 700));
        var guards = 0; var activations = 0; var focus = 0; var cases = new List<object>(); var failures = new List<object>();
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Scroll primitive acquired visibility, activation or focus.");
        }
        async Task Layout() { for (var i = 0; i < 8; i++) { root.Measure(new Size(800, 700)); root.Arrange(new Rect(0, 0, 800, 700)); root.UpdateLayout(); Guard(); await Task.Delay(20); } Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 700), "Scroll primitive requires its actual hidden unscaled viewport."); }
        object Box(FrameworkElement element) { var b = element.TransformToVisual(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight)); return new { x = b.X, y = b.Y, width = b.Width, height = b.Height }; }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        foreach (var name in new[] { "legacy-hidden-axis", "legacy-disabled-axis", "modern-vertical-content", "modern-both-content", "modern-reserved-vertical-ltr", "modern-reserved-vertical-rtl", "modern-reserved-horizontal-ltr", "modern-reserved-horizontal-rtl", "modern-reserved-both-ltr", "modern-reserved-both-rtl" })
        {
            try
            {
                root.Children.Clear(); var markers = new Canvas(); var vertical = new Rectangle { Width = 40, Height = 480, Fill = new SolidColorBrush(Microsoft.UI.Colors.Red) }; var horizontal = new Rectangle { Width = 520, Height = 40, Fill = new SolidColorBrush(Microsoft.UI.Colors.Green) }; markers.Children.Add(vertical); markers.Children.Add(horizontal);
                var original = new Border { Width = 520, Height = 480, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 25, 80, 120)), Child = markers };
                var modern = name.StartsWith("modern", StringComparison.Ordinal); var reserved = name.StartsWith("modern-reserved", StringComparison.Ordinal); var rtl = name.EndsWith("-rtl", StringComparison.Ordinal); FrameworkElement scroller; ScrollViewer? legacy = null; ScrollView? view = null; var completions = new List<object>();
                if (modern)
                {
                    view = new ScrollView { Width = 256, MaxHeight = 240, Content = original, HorizontalScrollMode = ScrollingScrollMode.Disabled, VerticalScrollMode = ScrollingScrollMode.Enabled, HorizontalScrollBarVisibility = ScrollingScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollingScrollBarVisibility.Auto, ContentOrientation = name == "modern-vertical-content" ? ScrollingContentOrientation.Vertical : ScrollingContentOrientation.Both, HorizontalContentAlignment = HorizontalAlignment.Stretch };
                    view.ScrollCompleted += (_, args) => completions.Add(new { args.CorrelationId }); scroller = view;
                    if (reserved)
                    {
                        view.Template = ReservedTemplate(); view.ContentOrientation = ScrollingContentOrientation.Vertical;
                        view.HorizontalScrollMode = name.Contains("-vertical-") ? ScrollingScrollMode.Disabled : ScrollingScrollMode.Enabled;
                        view.HorizontalScrollBarVisibility = name.Contains("-vertical-") ? ScrollingScrollBarVisibility.Hidden : ScrollingScrollBarVisibility.Auto;
                        view.VerticalScrollMode = name.Contains("-horizontal-") ? ScrollingScrollMode.Disabled : ScrollingScrollMode.Enabled;
                        view.VerticalScrollBarVisibility = name.Contains("-horizontal-") ? ScrollingScrollBarVisibility.Hidden : ScrollingScrollBarVisibility.Auto;
                        view.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    }
                }
                else
                {
                    legacy = new ScrollViewer { Width = 256, MaxHeight = 240, Content = original, HorizontalScrollMode = ScrollMode.Disabled, VerticalScrollMode = ScrollMode.Enabled, HorizontalScrollBarVisibility = name == "legacy-disabled-axis" ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, ZoomMode = ZoomMode.Disabled, HorizontalContentAlignment = HorizontalAlignment.Stretch }; scroller = legacy;
                }
                Canvas.SetLeft(scroller, 100); Canvas.SetTop(scroller, 100); root.Children.Add(scroller); await Layout();
                FrameworkElement? Find(DependencyObject parent, string part) { if (parent is FrameworkElement e && e.Name == part) return e; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) if (Find(VisualTreeHelper.GetChild(parent, i), part) is { } found) return found; return null; }
                object? Part(string part, string? within = null) { var search = within is null ? scroller : Find(scroller, within); return search is not null && Find(search, part) is { } element ? new { bounds = Box(element), visibility = element.Visibility.ToString() } : null; }
                object Snapshot(string stage) => new { stage, viewport = Box(scroller), content = Box(original), viewportWidth = modern ? view!.ViewportWidth : legacy!.ViewportWidth, viewportHeight = modern ? view!.ViewportHeight : legacy!.ViewportHeight, extentWidth = modern ? view!.ExtentWidth : legacy!.ExtentWidth, extentHeight = modern ? view!.ExtentHeight : legacy!.ExtentHeight, horizontalOffset = modern ? view!.HorizontalOffset : legacy!.HorizontalOffset, verticalOffset = modern ? view!.VerticalOffset : legacy!.VerticalOffset, originalRetained = ReferenceEquals(modern ? view!.Content : legacy!.Content, original), presenter = Part("PART_ScrollPresenter"), horizontalBar = Part("PART_HorizontalScrollBar"), verticalBar = Part("PART_VerticalScrollBar"), horizontalThumb = Part("HorizontalThumb", "PART_HorizontalScrollBar"), verticalThumb = Part("VerticalThumb", "PART_VerticalScrollBar") };
                var samples = new List<object> { Snapshot("initial") }; int? correlation = null; bool? changeAccepted = null;
                if (modern) correlation = view!.ScrollTo(80, 120, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore)); else changeAccepted = legacy!.ChangeView(80, 120, null, true);
                await Layout(); samples.Add(Snapshot("programmatic-hidden-axis")); var x = modern ? view!.HorizontalOffset : legacy!.HorizontalOffset; var y = modern ? view!.VerticalOffset : legacy!.VerticalOffset;
                var bitmap = new RenderTargetBitmap(); var operation = bitmap.RenderAsync(root); var task = operation.AsTask(); if (await Task.WhenAny(task, Task.Delay(2000)) != task) { operation.Cancel(); throw new TimeoutException("Scroll primitive rendering exceeded two seconds."); } await task; Guard(); Require(bitmap.PixelWidth == 800 && bitmap.PixelHeight == 700, "Scroll primitive frame was rescaled.");
                var buffer = await bitmap.GetPixelsAsync(); var pixels = new byte[buffer.Length]; using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer)) reader.ReadBytes(pixels); var at = (110 * 800 + 110) * 4; var rgba = new[] { (int)pixels[at + 2], pixels[at + 1], pixels[at], pixels[at + 3] };
                using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream(); var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, stream); encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8, Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied, 800, 700, 96, 96, pixels); await encoder.FlushAsync(); using var readerPng = new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0)); await readerPng.LoadAsync((uint)stream.Size); var png = new byte[stream.Size]; readerPng.ReadBytes(png); var file = name + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), png);
                original.Width = double.NaN; original.Child = new TextBlock { Text = "Fictional natural-width content wraps within its viewport without losing original caller ownership.", TextWrapping = TextWrapping.Wrap }; await Layout(); samples.Add(Snapshot("auto-width"));
                original.Height = 80; await Layout(); samples.Add(Snapshot("content-shrink"));
                cases.Add(new { name, samples, correlation, changeAccepted, completions, sourceHiddenAxisOffsetMatched = x == 80 && y == 120, ancestorPixel = rgba, file, pngSha256 = Convert.ToHexStringLower(SHA256.HashData(png)), nativeScrollViewerSubclassable = !typeof(ScrollViewer).IsSealed, nativeScrollViewSubclassable = !typeof(ScrollView).IsSealed });
            }
            catch (Exception error) { failures.Add(new { name, error = error.ToString() }); }
        }
        root.Children.Clear(); await Layout(); string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = failures.Count == 0, scope = "hidden-scroll-area-native-capability-probe", diagnosticOnly = true, sourceContractVerified = false, cases, failures, guards, activations, focus, librarySha256 = Hash(typeof(BleeckerHoverCard).Assembly.Location), hostSha256 = Hash(typeof(ScrollAreaPrimitiveProbe).Assembly.Location), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, nativeScrollAreaImplemented = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }

    // Diagnostic template only. It checks native reserved-gutter sizing and
    // original content; production tokens, input and thumb paint remain open.
    private static ControlTemplate ReservedTemplate() => (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("""
        <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" TargetType="ScrollView">
          <Grid x:Name="PART_Root" Background="Transparent">
            <Grid.Resources>
              <ControlTemplate x:Key="ThumbTemplate" TargetType="Thumb"><Border Background="{TemplateBinding Background}" CornerRadius="99" /></ControlTemplate>
              <ControlTemplate x:Key="RepeatTemplate" TargetType="RepeatButton"><Border Background="Transparent" /></ControlTemplate>
              <Style x:Key="Bar" TargetType="ScrollBar">
                <Setter Property="MinWidth" Value="0" /><Setter Property="MinHeight" Value="0" /><Setter Property="IsTabStop" Value="False" />
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ScrollBar"><Grid>
                  <Grid x:Name="HorizontalRoot" Background="Transparent"><Grid.ColumnDefinitions><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="Auto"/><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
                    <RepeatButton x:Name="HorizontalSmallDecrease" Visibility="Collapsed" IsTabStop="False"/>
                    <RepeatButton x:Name="HorizontalLargeDecrease" Grid.Column="1" Width="0" Template="{StaticResource RepeatTemplate}" IsTabStop="False"/>
                    <Thumb x:Name="HorizontalThumb" Grid.Column="2" MinWidth="17" Background="#FF336699" Template="{StaticResource ThumbTemplate}"/>
                    <RepeatButton x:Name="HorizontalLargeIncrease" Grid.Column="3" Template="{StaticResource RepeatTemplate}" IsTabStop="False"/>
                    <RepeatButton x:Name="HorizontalSmallIncrease" Grid.Column="4" Visibility="Collapsed" IsTabStop="False"/>
                  </Grid>
                  <Grid x:Name="VerticalRoot" Background="Transparent"><Grid.RowDefinitions><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="Auto"/><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
                    <RepeatButton x:Name="VerticalSmallDecrease" Visibility="Collapsed" IsTabStop="False"/>
                    <RepeatButton x:Name="VerticalLargeDecrease" Grid.Row="1" Height="0" Template="{StaticResource RepeatTemplate}" IsTabStop="False"/>
                    <Thumb x:Name="VerticalThumb" Grid.Row="2" MinHeight="17" Background="#FF336699" Template="{StaticResource ThumbTemplate}"/>
                    <RepeatButton x:Name="VerticalLargeIncrease" Grid.Row="3" Template="{StaticResource RepeatTemplate}" IsTabStop="False"/>
                    <RepeatButton x:Name="VerticalSmallIncrease" Grid.Row="4" Visibility="Collapsed" IsTabStop="False"/>
                  </Grid>
                </Grid></ControlTemplate></Setter.Value></Setter>
              </Style>
            </Grid.Resources>
            <Grid.RowDefinitions><RowDefinition Height="*"/><RowDefinition Height="Auto"/></Grid.RowDefinitions>
            <Grid.ColumnDefinitions><ColumnDefinition Width="*"/><ColumnDefinition Width="Auto"/></Grid.ColumnDefinitions>
            <ScrollPresenter x:Name="PART_ScrollPresenter" Content="{TemplateBinding Content}" ContentOrientation="{TemplateBinding ContentOrientation}" HorizontalScrollMode="{TemplateBinding HorizontalScrollMode}" VerticalScrollMode="{TemplateBinding VerticalScrollMode}" ZoomMode="Disabled" />
            <ScrollBar x:Name="PART_HorizontalScrollBar" Grid.Row="1" Height="15" Style="{StaticResource Bar}" Orientation="Horizontal" Visibility="{TemplateBinding ComputedHorizontalScrollBarVisibility}" />
            <ScrollBar x:Name="PART_VerticalScrollBar" Grid.Column="1" Width="6" Style="{StaticResource Bar}" Orientation="Vertical" Visibility="{TemplateBinding ComputedVerticalScrollBarVisibility}" />
          </Grid>
        </ControlTemplate>
        """);
}
