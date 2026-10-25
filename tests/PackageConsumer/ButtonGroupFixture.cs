using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Orchard;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class ButtonGroupFixture
{
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFixture = null)
    {
        var checks = new List<object>(); var frames = new List<object>(); var pngs = new List<object>(); var failures = new List<object>(); var restorations = new List<object>(); var alignmentRestorations = new List<object>(); var twoWayRestorations = new List<object>();
        var guards = 0; var activations = 0; var focus = 0; var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var root = new Canvas { Width = 900, Height = 800 }; window.Content = root;
        window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Fixture became visible, activated or focused."); }
        async Task Layout() { for (var i = 0; i < 4; i++) { root.Measure(new Size(900, 800)); root.Arrange(new Rect(0, 0, 900, 800)); root.UpdateLayout(); Guard(); await Task.Delay(10); } }
        async Task Mount(BleeckerButtonGroup g) { Canvas.SetLeft(g, 24); Canvas.SetTop(g, 24); root.Children.Add(g); foreach (var c in g.Children.OfType<Control>()) c.ApplyTemplate(); await Layout(); Require(g.IsLoaded, "Group did not mount in the real XAML tree."); }
        async Task Check(string id, Func<Task> action) { try { Guard(); await action(); Guard(); checks.Add(new { id, passed = true }); } catch (Exception error) { checks.Add(new { id, passed = false }); failures.Add(new { id, error = error.ToString() }); } finally { root.Children.Clear(); await Layout(); } }
        static double[] Corners(CornerRadius r) => [r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft];
        static double[] Sides(Thickness t) => [t.Left, t.Top, t.Right, t.Bottom];
        void Edges(BleeckerButtonGroup g)
        {
            var children = g.Children.OfType<Control>().Where(c => c.Visibility == Visibility.Visible).ToArray(); var radius = BleeckerTokens.Radii["button"];
            for (var i = 0; i < children.Length; i++)
            {
                var first = i == 0; var last = i == children.Length - 1;
                var corner = g.Orientation == Orientation.Horizontal ? new CornerRadius(first ? radius : 0, last ? radius : 0, last ? radius : 0, first ? radius : 0) : new CornerRadius(first ? radius : 0, first ? radius : 0, last ? radius : 0, last ? radius : 0);
                var border = g.Orientation == Orientation.Horizontal ? new Thickness(1, 1, last ? 1 : 0, 1) : new Thickness(1, 1, 1, last ? 1 : 0);
                Require(children[i].Margin == new Thickness(0) && children[i].CornerRadius == corner && children[i].BorderThickness == border, "Connected edges do not match visible child order.");
            }
        }
        void Snapshot(string id, BleeckerButtonGroup g)
        {
            frames.Add(new { id, orientation = g.Orientation.ToString(), direction = g.FlowDirection.ToString(), groupWidth = g.ActualWidth, groupHeight = g.ActualHeight, children = g.Children.OfType<Control>().Select(c => { var b = c.TransformToVisual(root).TransformBounds(new Rect(0, 0, c.ActualWidth, c.ActualHeight)); var action = c is BleeckerButton ? Descendants(c).OfType<Button>().Single() : c; var a = action.TransformToVisual(root).TransformBounds(new Rect(0, 0, action.ActualWidth, action.ActualHeight)); return new { id = c.Tag as string, type = c.GetType().Name, loaded = c.IsLoaded, visible = c.Visibility == Visibility.Visible, enabled = c.IsEnabled, margin = Sides(c.Margin), radius = Corners(c.CornerRadius), border = Sides(c.BorderThickness), horizontalAlignment = c.HorizontalAlignment.ToString(), verticalAlignment = c.VerticalAlignment.ToString(), x = b.X, y = b.Y, width = b.Width, height = b.Height, actionX = a.X, actionY = a.Y, actionWidth = a.Width, actionHeight = a.Height }; }).ToArray() });
        }
        async Task Capture(string id, BleeckerButtonGroup g)
        {
            Guard(); await Task.Delay(180); var image = new RenderTargetBitmap(); await image.RenderAsync(g); Guard();
            var pixels = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await image.GetPixelsAsync()); Require(image.PixelWidth > 0 && image.PixelHeight > 0 && pixels.Any(p => p != 0), "Group capture is empty.");
            using var stream = new InMemoryRandomAccessStream(); var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)image.PixelWidth, (uint)image.PixelHeight, 96, 96, pixels); await encoder.FlushAsync(); stream.Seek(0);
            using var reader = new DataReader(stream.GetInputStreamAt(0)); await reader.LoadAsync((uint)stream.Size); var bytes = new byte[(int)stream.Size]; reader.ReadBytes(bytes);
            var file = id.Replace('/', '-') + ".png"; await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, file), bytes); pngs.Add(new { file, width = image.PixelWidth, height = image.PixelHeight, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }); Guard();
        }
        Button Native(string id) => new() { Tag = id, Content = id, Style = (Style)Application.Current.Resources["BleeckerSecondaryButtonStyle"] };
        try
        {
            Guard(); await Layout();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                await Check(theme + "/empty", async () => { var g = new BleeckerButtonGroup(); await Mount(g); Require(g.DesiredSize.Width == 0 && g.DesiredSize.Height == 0 && g.Children.Count == 0, "Empty group has nonzero desired size."); Snapshot(theme + "/empty", g); });
                await Check(theme + "/order-visibility-orientation", async () =>
                {
                    var command = new ProbeCommand(); var parameter = new object(); var producer = new TextBlock { Text = "Original bound label" }; var first = Native("first"); first.Command = command; first.CommandParameter = parameter;
                    var binding = new Binding { Source = producer, Path = new PropertyPath("Text") }; first.SetBinding(ContentControl.ContentProperty, binding);
                    var middle = new ToggleButton { Tag = "middle", Content = "Middle", IsChecked = true };
                    var last = new BleeckerButton { Tag = "last", Content = "Last", Variant = BleeckerButtonVariant.Secondary, IsEnabled = false };
                    var g = new BleeckerButtonGroup(); AutomationProperties.SetName(g, "Caller group"); g.Children.Add(first); g.Children.Add(middle); g.Children.Add(last); await Mount(g); Edges(g);
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(g); Require(peer.GetAutomationControlType() == AutomationControlType.Group && peer.GetName() == "Caller group", "Group metadata changed.");
                    Require(new ButtonAutomationPeer(first).GetAutomationControlType() == AutomationControlType.Button && first.IsTabStop && middle.IsTabStop && middle.IsChecked == true && !last.IsEnabled, "Child semantics changed.");
                    Snapshot(theme + "/horizontal", g); g.Children.Remove(last); g.Children.Insert(0, last); await Layout(); Edges(g); Require(ReferenceEquals(g.Children[0], last), "Reorder replaced original child."); Snapshot(theme + "/reorder", g);
                    first.Visibility = Visibility.Collapsed; await Layout(); Edges(g); Snapshot(theme + "/collapsed-middle", g);
                    last.Visibility = Visibility.Collapsed; middle.Visibility = Visibility.Collapsed; await Layout(); Require(g.DesiredSize.Width == 0 && g.DesiredSize.Height == 0, "Collapsed group is not empty."); Snapshot(theme + "/all-collapsed", g);
                    foreach (var c in g.Children) c.Visibility = Visibility.Visible; g.Orientation = Orientation.Vertical; await Layout(); Edges(g); Snapshot(theme + "/vertical", g);
                    g.Orientation = Orientation.Horizontal; producer.Text = "Updated bound label"; await Layout(); Edges(g);
                    Require(ReferenceEquals(first.GetBindingExpression(ContentControl.ContentProperty)?.ParentBinding, binding) && Equals(first.Content, producer.Text) && ReferenceEquals(first.Command, command) && ReferenceEquals(first.CommandParameter, parameter) && command.Calls == 0, "Geometry changed caller content, command or parameter."); Snapshot(theme + "/horizontal-restored", g);
                });
                await Check(theme + "/local-removal", async () =>
                {
                    var child = Native("local"); var radius = new CornerRadius(2, 3, 4, 5); var border = new Thickness(2, 3, 4, 5); var margin = new Thickness(6, 7, 8, 9); child.CornerRadius = radius; child.BorderThickness = border; child.Margin = margin;
                    var g = new BleeckerButtonGroup(); g.Children.Add(child); g.Children.Add(Native("remaining")); await Mount(g); Edges(g); g.Children.Remove(child); await Layout();
                    Require(child.CornerRadius == radius && child.BorderThickness == border && child.Margin == margin && child.Parent is null, "Removed child did not recover local values."); Edges(g); Snapshot(theme + "/local-removed", g);
                    restorations.Add(new { id = theme + "/local-removed", radius = Corners(child.CornerRadius), border = Sides(child.BorderThickness), margin = Sides(child.Margin), loaded = child.IsLoaded });
                });
                await Check(theme + "/binding-removal-and-unload", async () =>
                {
                    var producer = new Border { CornerRadius = new CornerRadius(11), BorderThickness = new Thickness(2), Margin = new Thickness(3) }; var child = Native("bound");
                    var properties = new[] { Control.CornerRadiusProperty, Control.BorderThicknessProperty, FrameworkElement.MarginProperty };
                    var bindings = new[] { "CornerRadius", "BorderThickness", "Margin" }.Select(name => new Binding { Source = producer, Path = new PropertyPath(name) }).ToArray(); for (var i = 0; i < properties.Length; i++) child.SetBinding(properties[i], bindings[i]);
                    var g = new BleeckerButtonGroup(); g.Children.Add(child); await Mount(g); Edges(g); producer.CornerRadius = new CornerRadius(12, 13, 14, 15); producer.BorderThickness = new Thickness(4, 5, 6, 7); producer.Margin = new Thickness(8, 9, 10, 11); await Layout(); Edges(g);
                    void Restored(string phase) { var restored = properties.Select((p, i) => ReferenceEquals(child.GetBindingExpression(p)?.ParentBinding, bindings[i])).ToArray(); Require(restored.All(v => v), "Original appearance binding was not restored."); Require(child.CornerRadius == producer.CornerRadius && child.BorderThickness == producer.BorderThickness && child.Margin == producer.Margin, "Restored binding did not read current producer values."); restorations.Add(new { id = theme + "/" + phase, radius = Corners(child.CornerRadius), border = Sides(child.BorderThickness), margin = Sides(child.Margin), originalBindingsRestored = restored, loaded = child.IsLoaded }); }
                    g.Children.Remove(child); await Layout(); Restored("bound-removed"); producer.Margin = new Thickness(17); await Layout(); Restored("removed-producer-update"); g.Children.Add(child); await Layout(); Edges(g);
                    root.Children.Remove(g); await Layout(); Require(!g.IsLoaded && !child.IsLoaded, "Unmount did not unload originals."); Restored("group-unloaded"); producer.CornerRadius = new CornerRadius(19); await Layout(); Restored("unloaded-producer-update"); await Mount(g); Edges(g);
                    g.Children.Clear(); await Layout(); Restored("children-cleared"); Require(g.DesiredSize.Width == 0 && g.DesiredSize.Height == 0, "Cleared group retained size."); Snapshot(theme + "/binding-cleared", g);
                });
                await Check(theme + "/style-removal", async () =>
                {
                    var child = Native("styled"); var style = new Style { TargetType = typeof(Button), BasedOn = child.Style }; style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(21))); style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(4))); style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(5))); child.Style = style;
                    var g = new BleeckerButtonGroup(); g.Children.Add(child); await Mount(g); Edges(g); g.Children.Clear(); await Layout();
                    Require(child.ReadLocalValue(Control.CornerRadiusProperty) == DependencyProperty.UnsetValue && child.ReadLocalValue(Control.BorderThicknessProperty) == DependencyProperty.UnsetValue && child.ReadLocalValue(FrameworkElement.MarginProperty) == DependencyProperty.UnsetValue && ReferenceEquals(child.Style, style), "Removal replaced caller style or left local group values.");
                    Require(child.CornerRadius == new CornerRadius(21) && child.BorderThickness == new Thickness(4) && child.Margin == new Thickness(5), "Style values did not resume.");
                    restorations.Add(new { id = theme + "/style-removed", radius = Corners(child.CornerRadius), border = Sides(child.BorderThickness), margin = Sides(child.Margin), localValuesUnset = new[] { Control.CornerRadiusProperty, Control.BorderThicknessProperty, FrameworkElement.MarginProperty }.Select(p => child.ReadLocalValue(p) == DependencyProperty.UnsetValue).ToArray(), loaded = child.IsLoaded });
                });
                await Check(theme + "/alignment-restoration", async () =>
                {
                    foreach (var kind in new[] { "local", "style", "binding" })
                    {
                        var child = Native(kind); var producer = new Border { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
                        var horizontal = new Binding { Source = producer, Path = new PropertyPath("HorizontalAlignment") }; var vertical = new Binding { Source = producer, Path = new PropertyPath("VerticalAlignment") };
                        if (kind == "local") { child.HorizontalAlignment = HorizontalAlignment.Center; child.VerticalAlignment = VerticalAlignment.Bottom; }
                        if (kind == "style") { var style = new Style { TargetType = typeof(Button), BasedOn = child.Style }; style.Setters.Add(new Setter(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center)); style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Bottom)); child.Style = style; }
                        if (kind == "binding") { child.SetBinding(FrameworkElement.HorizontalAlignmentProperty, horizontal); child.SetBinding(FrameworkElement.VerticalAlignmentProperty, vertical); }
                        var g = new BleeckerButtonGroup(); g.Children.Add(child); await Mount(g); Require(child.HorizontalAlignment == HorizontalAlignment.Stretch && child.VerticalAlignment == VerticalAlignment.Stretch, "Group did not own stretch alignment.");
                        producer.HorizontalAlignment = HorizontalAlignment.Center; producer.VerticalAlignment = VerticalAlignment.Bottom; await Layout();
                        void Restored(string phase) { var originalBindings = kind != "binding" || (ReferenceEquals(child.GetBindingExpression(FrameworkElement.HorizontalAlignmentProperty)?.ParentBinding, horizontal) && ReferenceEquals(child.GetBindingExpression(FrameworkElement.VerticalAlignmentProperty)?.ParentBinding, vertical)); var unset = kind != "style" || (child.ReadLocalValue(FrameworkElement.HorizontalAlignmentProperty) == DependencyProperty.UnsetValue && child.ReadLocalValue(FrameworkElement.VerticalAlignmentProperty) == DependencyProperty.UnsetValue); Require(child.HorizontalAlignment == HorizontalAlignment.Center && child.VerticalAlignment == VerticalAlignment.Bottom && originalBindings && unset, "Caller alignment did not resume."); alignmentRestorations.Add(new { id = theme + "/" + phase, horizontal = child.HorizontalAlignment.ToString(), vertical = child.VerticalAlignment.ToString(), originalBindingsRestored = originalBindings, styleLocalValuesUnset = unset, loaded = child.IsLoaded }); }
                        g.Children.Remove(child); await Layout(); Restored(kind + "-removed");
                        if (kind == "binding") { g.Children.Add(child); await Layout(); root.Children.Remove(g); await Layout(); Restored("binding-unloaded"); }
                        root.Children.Clear(); await Layout();
                    }
                });
                await Check(theme + "/two-way-binding-restoration", async () =>
                {
                    var producer = new Border { CornerRadius = new CornerRadius(11), BorderThickness = new Thickness(2), Margin = new Thickness(3), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top }; var child = Native("two-way");
                    var properties = new[] { Control.CornerRadiusProperty, Control.BorderThicknessProperty, FrameworkElement.MarginProperty, FrameworkElement.HorizontalAlignmentProperty, FrameworkElement.VerticalAlignmentProperty };
                    var names = new[] { "CornerRadius", "BorderThickness", "Margin", "HorizontalAlignment", "VerticalAlignment" }; var bindings = names.Select(name => new Binding { Source = producer, Path = new PropertyPath(name), Mode = BindingMode.TwoWay }).ToArray();
                    for (var i = 0; i < properties.Length; i++) child.SetBinding(properties[i], bindings[i]); var g = new BleeckerButtonGroup(); g.Children.Add(child); await Mount(g); Edges(g);
                    var sourcePreserved = producer.CornerRadius == new CornerRadius(11) && producer.BorderThickness == new Thickness(2) && producer.Margin == new Thickness(3) && producer.HorizontalAlignment == HorizontalAlignment.Right && producer.VerticalAlignment == VerticalAlignment.Top;
                    twoWayRestorations.Add(new { id = theme + "/mounted", sourcePreserved, radius = Corners(producer.CornerRadius), border = Sides(producer.BorderThickness), margin = Sides(producer.Margin), horizontal = producer.HorizontalAlignment.ToString(), vertical = producer.VerticalAlignment.ToString() });
                    Require(sourcePreserved, "Grouping wrote temporary appearance values into a two-way binding source.");
                    producer.CornerRadius = new CornerRadius(13); producer.Margin = new Thickness(17); await Layout(); g.Children.Remove(child); await Layout(); var restored = properties.Select((p, i) => ReferenceEquals(child.GetBindingExpression(p)?.ParentBinding, bindings[i])).ToArray();
                    twoWayRestorations.Add(new { id = theme + "/released", originalBindingsRestored = restored, radius = Corners(child.CornerRadius), border = Sides(child.BorderThickness), margin = Sides(child.Margin), horizontal = child.HorizontalAlignment.ToString(), vertical = child.VerticalAlignment.ToString() });
                    Require(restored.All(v => v) && child.CornerRadius == new CornerRadius(13) && child.Margin == new Thickness(17) && producer.HorizontalAlignment == HorizontalAlignment.Right && producer.VerticalAlignment == VerticalAlignment.Top, "Two-way group bindings or current producer values were not restored.");
                });
                await Check(theme + "/long-labels", async () =>
                {
                    var labels = new[] { "Review international coverage and retain the original action", "東京の編集内容を確認して保存", "مراجعة التغطية الدولية وحفظ التغييرات" }; var g = new BleeckerButtonGroup { Width = 360 };
                    var originals = labels.Select((label, i) => new BleeckerButton { Tag = "long-" + i, Variant = BleeckerButtonVariant.Secondary, Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap } }).ToArray(); foreach (var c in originals) g.Children.Add(c); await Mount(g); Edges(g);
                    foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft }) { g.FlowDirection = direction; await Layout(); Edges(g); Require(g.ActualWidth == 360 && originals.All(c => c.ActualWidth > 0 && c.ActualHeight > 0) && originals.Select(c => ((TextBlock)c.Content).Text).SequenceEqual(labels), "Constrained labels lost content or size."); Snapshot(theme + "/long-" + direction, g); await Capture(theme + "/long-" + direction, g); }
                    g.Orientation = Orientation.Vertical; g.Width = 260; await Layout(); Edges(g); Snapshot(theme + "/long-vertical", g); await Capture(theme + "/long-vertical", g); Require(g.Children.Cast<UIElement>().SequenceEqual(originals), "Layout replaced original controls.");
                });
                await Check(theme + "/invalid-child", async () => { var g = new BleeckerButtonGroup(); g.Children.Add(new TextBlock { Text = "Not an action" }); var rejected = false; try { g.Measure(new Size(300, 200)); } catch (Exception e) { rejected = e.Message.Contains("accepts", StringComparison.Ordinal); } Require(rejected, "Invalid child did not fail explicitly."); await Task.CompletedTask; });
                await Check(theme + "/invalid-orientation", async () => { var g = new BleeckerButtonGroup { Orientation = (Orientation)42 }; var rejected = false; try { g.Measure(new Size(300, 200)); } catch (Exception e) { rejected = e.Message.Contains("orientation", StringComparison.OrdinalIgnoreCase); } Require(rejected, "Invalid orientation did not fail explicitly."); await Task.CompletedTask; });
                if (catalogFixture is not null) await Check(theme + "/catalog-fixture", async () =>
                {
                    var fixture = catalogFixture(); root.Children.Add(fixture); await Layout();
                    var horizontal = (BleeckerButtonGroup)fixture.FindName("LongGroupedActions"); var vertical = (BleeckerButtonGroup)fixture.FindName("VerticalGroupedActions");
                    Require(horizontal.IsLoaded && vertical.IsLoaded && horizontal.Width == 360 && vertical.Width == 260 && vertical.Orientation == Orientation.Vertical && vertical.FlowDirection == FlowDirection.RightToLeft, "Catalog long-label groups did not mount with their declared layout.");
                    Edges(horizontal); Edges(vertical); Require(horizontal.Children.Count == 3 && vertical.Children.Count == 2 && !((Control)vertical.Children[1]).IsEnabled, "Catalog group children/disabled state changed.");
                    var action = (BleeckerButton)fixture.FindName("InternationalReview"); var save = (Button)fixture.FindName("InternationalSave");
                    Require(action.Command is not null && ReferenceEquals(action.Command, save.Command) && Equals(action.CommandParameter, "international-review") && Equals(save.CommandParameter, "international-save"), "Catalog grouped actions lost caller commands/parameters.");
                    Snapshot(theme + "/catalog-horizontal", horizontal); Snapshot(theme + "/catalog-vertical", vertical); await Capture(theme + "/catalog-horizontal", horizontal);
                });
            }
        }
        catch (Exception error) { failures.Add(new { id = "fixture", error = error.ToString() }); }
        finally
        {
            root.Children.Clear(); await Layout(); var library = typeof(BleeckerButtonGroup).Assembly.Location; var passed = failures.Count == 0; Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-button-group-native-v2", passed, checks, frames, pngs, restorations, alignmentRestorations, twoWayRestorations, failures, guards, activations, focus, hostVisible = window.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle), hardwareInputSent = false, activateCalled = false, focusCalled = false, enabledButtonInvoked = false,
                catalogFixtureVerified = catalogFixture is not null, sourceSha256 = "f4a21c77bf545ddc0096dc7e7dc5d6fcdb892b381d651668501cbbab412d5306", librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(library))), hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), nativeArm64RuntimeVerified = false, actualDisplayOrTextScalingVerified = false, physicalInputVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(passed ? 0 : 1);
        }
    }
    private sealed class ProbeCommand : ICommand { internal int Calls { get; private set; } public bool CanExecute(object? parameter) => true; public void Execute(object? parameter) => Calls++; public event EventHandler? CanExecuteChanged { add { } remove { } } }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject parent) { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var next in Descendants(child)) yield return next; } }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
}
