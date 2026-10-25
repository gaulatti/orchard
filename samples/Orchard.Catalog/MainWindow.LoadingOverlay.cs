using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayAsync(string folder)
    {
        using var input = new NativeInputSession(CatalogRoot.XamlRoot, folder, "loading-overlay"); await input.AcquireAsync(ComponentSearch);
        var root = CatalogRoot.XamlRoot; var size = root.Size;
        Require(root.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1, "Loading overlay reference requires display/text scale 1.");
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.LoadingOverlay.json") ?? throw new InvalidOperationException("Loading overlay reference is missing.");
        using var reference = JsonDocument.Parse(stream);
        var host = new Canvas { Width = size.Width, Height = size.Height, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false };
        Grid.SetRowSpan(host, CatalogRoot.RowDefinitions.Count); Grid.SetColumnSpan(host, CatalogRoot.ColumnDefinitions.Count);
        var pattern = new Canvas { Width = size.Width, Height = size.Height }; host.Children.Add(pattern);
        var actions = 0;
        var underlying = new Button { Name = "UnderlyingAction", Content = "Underlying action", Width = 140, Height = 32 };
        underlying.Click += (_, _) => actions++; Canvas.SetLeft(underlying, 20); Canvas.SetTop(underlying, 20); host.Children.Add(underlying);
        var next = new Button { Name = "NextAction", Content = "Next action" }; Canvas.SetLeft(next, 20); Canvas.SetTop(next, 70); host.Children.Add(next);
        var anchor = new Grid { Width = 100, Height = 40, UseLayoutRounding = false, RenderTransform = new TranslateTransform { X = 31.25, Y = 27.5 }, Clip = new RectangleGeometry { Rect = new Rect(0, 0, 100, 40) } };
        Canvas.SetLeft(anchor, 70); Canvas.SetTop(anchor, 120); host.Children.Add(anchor);
        var overlay = new BleeckerLoadingOverlay(); var second = new BleeckerLoadingOverlay { Label = "Second" }; anchor.Children.Add(overlay); anchor.Children.Add(second);
        var originalVisibility = CatalogRoot.Children.OfType<UIElement>().Select(child => (Child: child, Visibility: child.Visibility)).ToArray();
        var originalTabStop = CatalogRoot.ReadLocalValue(UIElement.IsTabStopProperty);
        var originalTabBinding = CatalogRoot.GetBindingExpression(UIElement.IsTabStopProperty)?.ParentBinding;
        var tabModel = new ToggleSwitch { IsOn = false };
        var tabBinding = new Binding { Source = tabModel, Path = new PropertyPath(nameof(ToggleSwitch.IsOn)), Mode = BindingMode.OneWay };
        foreach (var item in originalVisibility) item.Child.Visibility = Visibility.Collapsed;
        CatalogRoot.Children.Add(host);
        var cases = new List<object>(); var failures = new List<string>(); var interactions = new List<object>();
        try
        {
            foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                CatalogRoot.RequestedTheme = scheme; pattern.Children.Clear();
                if (scheme == ElementTheme.Dark) CatalogRoot.SetBinding(UIElement.IsTabStopProperty, tabBinding);
                var colors = scheme == ElementTheme.Light ? new[] { "#FFFFFF", "#E0E0E0" } : new[] { "#132331", "#071019" };
                for (var y = 0; y < size.Height; y += 20) for (var x = 0; x < size.Width; x += 20)
                {
                    var color = colors[1 - (x / 20 + y / 20) % 2];
                    var tile = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, Convert.ToByte(color.Substring(1, 2), 16), Convert.ToByte(color.Substring(3, 2), 16), Convert.ToByte(color.Substring(5, 2), 16))) };
                    Canvas.SetLeft(tile, x); Canvas.SetTop(tile, y); pattern.Children.Add(tile);
                }
                foreach (var spec in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("width").GetInt32() == (int)size.Width && c.GetProperty("height").GetInt32() == (int)size.Height && c.GetProperty("scheme").GetString() == scheme.ToString()))
                {
                    var id = spec.GetProperty("id").GetString()!; var props = spec.GetProperty("props");
                    overlay.IsVisible = second.IsVisible = false; await AwaitPresentationAsync(host);
                    overlay.FlowDirection = spec.GetProperty("direction").GetString() == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
                    overlay.Label = props.TryGetProperty("label", out var value) ? value.GetString() : "Loading...";
                    overlay.IsVisible = props.GetProperty("visible").GetBoolean();
                    second.IsVisible = props.TryGetProperty("second", out var stacked) && stacked.GetBoolean();
                    await AwaitPresentationAsync(host);
                    var portals = VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Where(p => p.Child is FrameworkElement element && element.Name == "PART_Portal").ToArray();
                    var expected = spec.GetProperty("normal").GetProperty("overlays"); var errors = new List<string>(); var measurements = new List<object>();
                    if (portals.Length != expected.GetArrayLength()) errors.Add($"Portal count {portals.Length} != {expected.GetArrayLength()}.");
                    // Match by source label rather than relying on WinUI's popup enumeration order.
                    foreach (var target in expected.EnumerateArray())
                    {
                        var text = target.GetProperty("label"); var wanted = text.ValueKind == JsonValueKind.Null ? "" : text.GetProperty("text").GetString()!.Trim();
                        var portal = portals.Select(p => (FrameworkElement)p.Child).Single(p => Part<TextBlock>(p, "PART_Label").Text == wanted);
                        var card = Part<BleeckerOverlaySurface>(portal, "PART_Card"); var spinner = Part<BleeckerLoadingSpinner>(portal, "PART_Spinner"); var label = Part<TextBlock>(portal, "PART_Label");
                        var p = Box(portal); var c = Box(card); var s = Box(spinner); var l = Box(Part<Canvas>(portal, "PART_LabelHost"));
                        Check(p, target.GetProperty("box"), "portal"); Check(c, target.GetProperty("card").GetProperty("box"), "card"); Check(s, target.GetProperty("spinner").GetProperty("box"), "spinner");
                        double? baseline = label.Text.Length == 0 ? null : label.TransformToVisual(root.Content).TransformPoint(new Point(0, label.BaselineOffset)).Y;
                        if (text.ValueKind != JsonValueKind.Null)
                        {
                            Check(l, text.GetProperty("box"), "label");
                            if (text.GetProperty("firstBaseline").ValueKind != JsonValueKind.Null) Near(baseline ?? double.NaN, text.GetProperty("firstBaseline").GetDouble(), "baseline");
                        }
                        measurements.Add(new { portal = p, card = c, spinner = s, label = text.ValueKind == JsonValueKind.Null ? null : (object)new { box = l, text = label.Text, baseline } });
                    }
                    var file = spec.GetProperty("file").GetString()!;
                    await File.AppendAllTextAsync(Path.Combine(folder, "loading-overlay-measurements.jsonl"), JsonSerializer.Serialize(new { id, scheme = scheme.ToString(), measurements, errors }) + "\n");
                    await input.MoveAsync(portals.LastOrDefault()?.Child as FrameworkElement ?? host, localPoint: new Point(size.Width - 4, size.Height - 4));
                    await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, file));
                    if (id == "clipped") File.Copy(Path.Combine(folder, file), Path.Combine(folder, $"catalog-{scheme.ToString().ToLowerInvariant()}.png"), true);
                    cases.Add(new { id, width = size.Width, height = size.Height, scheme = scheme.ToString(), errors, measurements, file });
                    failures.AddRange(errors.Select(error => $"{scheme}/{id}: {error}"));
                    await SaveReportAsync();
                    void Near(double actual, double source, string name) { if (!double.IsFinite(actual) || Math.Abs(actual - source) > .05) errors.Add($"{name}: {actual} != {source}"); }
                    void Check(Rect actual, JsonElement source, string name) { Near(actual.X, source.GetProperty("x").GetDouble(), name + ".x"); Near(actual.Y, source.GetProperty("y").GetDouble(), name + ".y"); Near(actual.Width, source.GetProperty("width").GetDouble(), name + ".width"); Near(actual.Height, source.GetProperty("height").GetDouble(), name + ".height"); }
                }
                overlay.IsVisible = second.IsVisible = false; await AwaitPresentationAsync(host); actions = 0;
                underlying.Focus(FocusState.Keyboard); overlay.Label = "Loading..."; overlay.IsVisible = true; await AwaitPresentationAsync(host);
                var focusOnOpen = ReferenceEquals(FocusManager.GetFocusedElement(root), underlying);
                await input.KeyAsync(0x20, underlying); await input.KeyAsync(0x0D, underlying); var keyboardActivations = actions;
                var activePortal = (FrameworkElement)VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Single(p => p.Child is FrameworkElement element && element.Name == "PART_Portal").Child;
                await input.ClickAsync(activePortal, localPoint: new Point(90, 36)); var pointerBlocked = actions == keyboardActivations;
                var focusAfterPointer = (FocusManager.GetFocusedElement(root) as FrameworkElement)?.Name;
                var neutralRootFocused = ReferenceEquals(FocusManager.GetFocusedElement(root), root.Content);
                if (neutralRootFocused) await input.KeyAsync(0x20, CatalogRoot);
                var pointerClearedActivation = actions == keyboardActivations;
                if (neutralRootFocused) await input.KeyAsync(0x1B, CatalogRoot); else input.Key(0x1B);
                await AwaitPresentationAsync(host);
                var escapeDoesNotDismiss = VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Any(p => p.Child is FrameworkElement element && element.Name == "PART_Portal");
                input.Key(0x09); await AwaitPresentationAsync(host); var focusAfterTab = (FocusManager.GetFocusedElement(root) as FrameworkElement)?.Name;
                var rootTabStopRestored = !CatalogRoot.IsTabStop;
                var rootBindingRestored = scheme == ElementTheme.Light ? ReferenceEquals(CatalogRoot.ReadLocalValue(UIElement.IsTabStopProperty), originalTabStop) : ReferenceEquals(CatalogRoot.GetBindingExpression(UIElement.IsTabStopProperty)?.ParentBinding, tabBinding);
                if (scheme == ElementTheme.Dark) { tabModel.IsOn = true; rootBindingRestored &= CatalogRoot.IsTabStop; tabModel.IsOn = false; rootBindingRestored &= !CatalogRoot.IsTabStop; }
                overlay.IsVisible = false; await AwaitPresentationAsync(host); var focusAfterHide = (FocusManager.GetFocusedElement(root) as FrameworkElement)?.Name;
                interactions.Add(new { scheme = scheme.ToString(), focusOnOpen, keyboardActivations, pointerBlocked, focusAfterPointer, neutralRootFocused, pointerClearedActivation, escapeDoesNotDismiss, focusAfterTab, focusAfterHide, rootTabStopRestored, rootBindingRestored });
                if (!focusOnOpen || keyboardActivations != 2 || !pointerBlocked || !neutralRootFocused || !pointerClearedActivation || !escapeDoesNotDismiss || focusAfterTab != "UnderlyingAction" || focusAfterHide != "UnderlyingAction" || !rootTabStopRestored || !rootBindingRestored) failures.Add(scheme + ": source pointer/keyboard focus semantics differ.");
            }
        }
        finally
        {
            overlay.IsVisible = second.IsVisible = false; CatalogRoot.Children.Remove(host);
            foreach (var item in originalVisibility) item.Child.Visibility = item.Visibility;
            if (originalTabBinding is not null) CatalogRoot.SetBinding(UIElement.IsTabStopProperty, originalTabBinding);
            else if (ReferenceEquals(originalTabStop, DependencyProperty.UnsetValue)) CatalogRoot.ClearValue(UIElement.IsTabStopProperty);
            else CatalogRoot.SetValue(UIElement.IsTabStopProperty, originalTabStop);
            await SaveReportAsync();
        }
        Require(cases.Count == 20 && failures.Count == 0, "Loading overlay geometry differs from source: " + string.Join("; ", failures.Take(5)));
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "loading-overlay", environment = RuntimeEnvironment() }));
        Task SaveReportAsync() => File.WriteAllTextAsync(Path.Combine(folder, "loading-overlay-source.json"), JsonSerializer.Serialize(new { cases, failures, interactions }, new JsonSerializerOptions { WriteIndented = true }));
        Rect Box(FrameworkElement element) { var at = element.TransformToVisual(root.Content).TransformPoint(new Point()); return new Rect(at.X, at.Y, element.ActualWidth, element.ActualHeight); }
        static T Part<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match && match.Name == name) return match; try { return Part<T>(child, name); } catch (KeyNotFoundException) { } }
            throw new KeyNotFoundException(name);
        }
    }
}
