using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Testing;

internal static partial class CommandSpotlightFixture
{
    private static async Task VerifyCatalogStyleCycle(FrameworkElement fixture, BleeckerModalHost host,
        Func<int, Task> layout, Action<bool, string> require)
    {
        var cycle = Elements(fixture).OfType<Button>().Single(b => b.Name == "StyleCycle");
        var customized = Elements(fixture).OfType<BleeckerCommandSpotlight>().Single(c => c.Name == "Customized");
        var original = customized.Actions.First();
        foreach (var width in new[] { 480d, 800d, 0d })
        {
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(cycle)
                .GetPattern(Microsoft.UI.Xaml.Automation.Peers.PatternInterface.Invoke)).Invoke();
            await layout(100);
            if (width == 0) require(customized.SurfaceStyle is null && customized.TriggerStyle is null, "Catalog cycle restores default styles");
            else
            {
                require(customized.SurfaceStyle?.Setters.OfType<Setter>().Any(s => s.Property == FrameworkElement.MaxWidthProperty && (double)s.Value == width) == true, "Catalog cycle applies intended surface style");
                customized.Show(); await layout(350);
                require(Math.Abs(Elements(host).Single(e => e.Name == "SpotlightSurface").ActualWidth - width) < 1d / 64, "Catalog caller style reaches open surface");
                customized.Close(); await layout(220);
            }
            require(ReferenceEquals(customized.Actions.First(), original), "Catalog style cycle retains original action");
        }
    }
    private static async Task RunStyleContracts(Grid root, BleeckerModalHost host, ElementTheme theme,
        Func<bool?, Task<BleeckerCommandSpotlight>> create, Func<int, Task> layout,
        Func<string, Task<string>> capture, List<object> results, Action<bool, string> require)
    {
        var resources = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/CommandSpotlight.xaml") };
        var surfaceStandard = (Style)resources["BleeckerCommandSpotlightSurfaceStyle"];
        var triggerStandard = (Style)resources["BleeckerCommandSpotlightTriggerStyle"];
        var current = await create(true);
        var original = current.Actions.First();
        var firstTrigger = Elements(current).OfType<Button>().Single(b => b.Name == "SpotlightTrigger");
        var firstSurface = (Control)Elements(host).Single(e => e.Name == "SpotlightPaint");
        var triggerBindingSource = new ContentControl();
        var surfaceBindingSource = new ContentControl();
        // Use the declaration's live theme tokens. Opaque paint gives exact pixel probes.
        Brush Token(string name) => Elements(current).OfType<TextBlock>().Single(e => e.Name == "PART_" + name).Foreground;
        var selected = Token("SelectedForeground");
        var icon = Token("Primary");
        var border = Token("Border");
        Style Surface(double width, Brush background, Style? parent = null)
        {
            var style = new Style { TargetType = typeof(BleeckerOverlaySurface), BasedOn = parent ?? surfaceStandard };
            style.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty, width));
            style.Setters.Add(new Setter(Control.BackgroundProperty, background));
            style.Setters.Add(new Setter(Control.CornerRadiusProperty, new CornerRadius(0)));
            return style;
        }
        Style Trigger(double height, Thickness padding, Brush background)
        {
            var style = new Style { TargetType = typeof(Button), BasedOn = triggerStandard };
            style.Setters.Add(new Setter(FrameworkElement.HeightProperty, height));
            style.Setters.Add(new Setter(Control.PaddingProperty, padding));
            style.Setters.Add(new Setter(Control.BackgroundProperty, background));
            style.Setters.Add(new Setter(Control.BorderBrushProperty, border));
            return style;
        }
        var surfaceA = Surface(480, selected); var surfaceB = Surface(544, icon);
        var triggerA = Trigger(48, new(20, 9, 24, 11), selected);
        var triggerB = Trigger(52, new(6, 8, 10, 12), icon);
        var inheritedSurface = new Style { TargetType = typeof(BleeckerOverlaySurface), BasedOn = surfaceA };
        var inheritedTrigger = new Style { TargetType = typeof(Button), BasedOn = triggerA };
        inheritedTrigger.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(13, 7, 17, 5)));
        async Task Record(string id, double maxWidth, double triggerHeight, Thickness padding, Brush? background,
            bool styled, bool sameElements = true, bool bound = false)
        {
            await layout(350);
            var button = Elements(current).OfType<Button>().Single(b => b.Name == "SpotlightTrigger");
            var paint = (Control)Elements(host).Single(e => e.Name == "SpotlightPaint");
            var frame = Elements(host).Single(e => e.Name == "SpotlightSurface");
            var actualPadding = button.Padding;
            var buttonChrome = Elements(button).OfType<Border>().First();
            var surfaceWidth = Math.Min(maxWidth, host.Width - 32 * current.FontSize / 16);
            bool SameBrush(Brush? a, Brush? b) => a is SolidColorBrush x && b is SolidColorBrush y && x.Color == y.Color && x.Opacity == y.Opacity;
            bool Unset(DependencyObject target, DependencyProperty property) => target.ReadLocalValue(property) == DependencyProperty.UnsetValue;
            object? Color(Brush? brush) => brush is SolidColorBrush b ? new { a = b.Color.A, r = b.Color.R, g = b.Color.G, b = b.Color.B, opacity = b.Opacity } : null;
            object Insets(Thickness value) => new { left = value.Left, top = value.Top, right = value.Right, bottom = value.Bottom };
            object Box(FrameworkElement e) { var box = e.TransformToVisual(host).TransformBounds(new(0, 0, e.ActualWidth, e.ActualHeight)); return new { x = box.X, y = box.Y, width = box.Width, height = box.Height }; }
            var surfaceLocalUnset = Unset(paint, FrameworkElement.MaxWidthProperty);
            var triggerLocalsUnset = new[] { FrameworkElement.HeightProperty, Control.PaddingProperty, Control.BackgroundProperty, Control.BorderBrushProperty }.All(property => Unset(button, property));
            require(Math.Abs(paint.MaxWidth - maxWidth) < 1d / 64 && Math.Abs(frame.ActualWidth - surfaceWidth) < 1d / 64, $"Live surface style reaches layout: {id}; max={paint.MaxWidth}, expectedMax={maxWidth}, width={frame.ActualWidth}, expectedWidth={surfaceWidth}");
            require(button.Height == triggerHeight && button.ActualHeight == triggerHeight && actualPadding == padding && buttonChrome.Padding == padding, "Live trigger style reaches template: " + id);
            require(surfaceLocalUnset == styled && triggerLocalsUnset == styled, "Library local defaults yield to caller style: " + id);
            require(ReferenceEquals(current.Actions.First(), original) && ReferenceEquals(button, firstTrigger) == sameElements && ReferenceEquals(paint, firstSurface) == sameElements, "Style refresh retains original data and expected element lifetime: " + id);
            require(!styled || SameBrush(paint.Background, background) && SameBrush(button.Background, background) && SameBrush(buttonChrome.Background, background) && SameBrush(button.BorderBrush, border), "Caller style paint reaches rendered chrome: " + id);
            var bindingsIntact = current.GetBindingExpression(BleeckerCommandSpotlight.TriggerStyleProperty) is not null && current.GetBindingExpression(BleeckerCommandSpotlight.SurfaceStyleProperty) is not null;
            require(bindingsIntact == bound, "Caller style bindings preserved: " + id);
            results.Add(new { theme = theme.ToString(), id, passed = true, current.FontSize, originalActionRetained = true,
                sameElements, bindingsIntact, surfaceLocalUnset, triggerLocalsUnset,
                surface = new { paint.MaxWidth, box = Box(frame), background = Color(paint.Background) },
                trigger = new { button.Height, button.ActualHeight, padding = Insets(actualPadding), chromePadding = Insets(buttonChrome.Padding), background = Color(button.Background), chromeBackground = Color(buttonChrome.Background), border = Color(button.BorderBrush), box = Box(button) },
                file = await capture("command-spotlight-style-" + theme + "-" + id) });
        }
        await Record("default", 672, 36, new(12, 6, 12, 6), null, false);
        current.SurfaceStyle = surfaceA; current.TriggerStyle = triggerA;
        await Record("applied", 480, 48, new(20, 9, 24, 11), selected, true);
        current.TriggerLabel = "Live caller style"; current.SetQuery("beta");
        await Record("refreshed", 480, 48, new(20, 9, 24, 11), selected, true);
        current.SurfaceStyle = surfaceB; current.TriggerStyle = triggerB;
        await Record("replaced", 544, 52, new(6, 8, 10, 12), icon, true);
        current.SurfaceStyle = inheritedSurface; current.TriggerStyle = inheritedTrigger;
        await Record("inherited", 480, 48, new(13, 7, 17, 5), selected, true);
        current.FontSize = 32;
        await Record("scaled-inherited", 480, 48, new(13, 7, 17, 5), selected, true);
        current.SurfaceStyle = null; current.TriggerStyle = null;
        await Record("cleared", 1344, 72, new(24, 12, 24, 12), null, false);
        current.FontSize = 16; triggerBindingSource.Tag = triggerA; surfaceBindingSource.Tag = surfaceA;
        current.SetBinding(BleeckerCommandSpotlight.TriggerStyleProperty, new Binding { Source = triggerBindingSource, Path = new("Tag"), Mode = BindingMode.OneWay });
        current.SetBinding(BleeckerCommandSpotlight.SurfaceStyleProperty, new Binding { Source = surfaceBindingSource, Path = new("Tag"), Mode = BindingMode.OneWay });
        await Record("bound-first", 480, 48, new(20, 9, 24, 11), selected, true, bound: true);
        triggerBindingSource.Tag = triggerB; surfaceBindingSource.Tag = surfaceB;
        await Record("bound-second", 544, 52, new(6, 8, 10, 12), icon, true, bound: true);
        triggerBindingSource.Tag = null; surfaceBindingSource.Tag = null;
        await Record("bound-cleared", 672, 36, new(12, 6, 12, 6), null, false, bound: true);
        triggerBindingSource.Tag = triggerA; surfaceBindingSource.Tag = surfaceA; await layout(350);
        var template = current.Template; current.Template = null; await layout(220);
        require(!Elements(host).Any(e => e.Name == "SpotlightSurface"), "Template removal releases modal surface");
        current.Template = template; current.ApplyTemplate();
        await Record("retemplated", 480, 48, new(20, 9, 24, 11), selected, true, sameElements: false, bound: true);
        root.Children.Remove(current); await layout(220);
        require(!Elements(host).Any(e => e.Name == "SpotlightSurface"), "Unloaded style fixture releases modal surface");
        root.Children.Add(current);
        await Record("remounted", 480, 48, new(20, 9, 24, 11), selected, true, sameElements: false, bound: true);
        var closed = await create(false); var closedOriginal = closed.Actions.First();
        foreach (var second in new[] { false, true })
        {
            closed.TriggerStyle = second ? triggerB : triggerA; await layout(350);
            var button = Elements(closed).OfType<Button>().Single(b => b.Name == "SpotlightTrigger");
            var chrome = Elements(button).OfType<Border>().First();
            var id = second ? "closed-second" : "closed-first";
            var box = button.TransformToVisual(host).TransformBounds(new(0, 0, button.ActualWidth, button.ActualHeight));
            object? Color(Brush? brush) => brush is SolidColorBrush b ? new { a = b.Color.A, r = b.Color.R, g = b.Color.G, b = b.Color.B, opacity = b.Opacity } : null;
            object Insets(Thickness value) => new { left = value.Left, top = value.Top, right = value.Right, bottom = value.Bottom };
            require(!closed.IsOpen && !Elements(host).Any(e => e.Name == "SpotlightSurface") && ReferenceEquals(closed.Actions.First(), closedOriginal), "Closed styled trigger keeps original data without modal");
            require(button.Height == (second ? 52 : 48) && button.ActualHeight == button.Height && button.Padding == chrome.Padding, "Closed trigger setter reaches template");
            results.Add(new { theme = theme.ToString(), id, closed = true, passed = true, originalActionRetained = true,
                trigger = new { button.Height, button.ActualHeight, padding = Insets(button.Padding), chromePadding = Insets(chrome.Padding), background = Color(button.Background), chromeBackground = Color(chrome.Background), box = new { x = box.X, y = box.Y, width = box.Width, height = box.Height } },
                file = await capture("command-spotlight-style-" + theme + "-" + id) });
        }
    }
}
