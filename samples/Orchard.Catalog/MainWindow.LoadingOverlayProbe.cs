using System.Numerics;
using System.Text.Json;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayProbeAsync(string folder)
    {
        using var input = new NativeInputSession(CatalogRoot.XamlRoot, folder, "loading-overlay-probe"); await input.AcquireAsync(ComponentSearch);
        var root = CatalogRoot.XamlRoot; var width = root.Size.Width; var height = root.Size.Height;
        var pattern = new Canvas { Width = width, Height = height, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        Grid.SetRowSpan(pattern, CatalogRoot.RowDefinitions.Count); Grid.SetColumnSpan(pattern, CatalogRoot.ColumnDefinitions.Count);
        for (var y = 0; y < height; y += 20) for (var x = 0; x < width; x += 20)
        {
            var level = (byte)((x / 20 + y / 20) % 2 == 0 ? 255 : 80);
            var tile = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, level, level, level)) };
            Canvas.SetLeft(tile, x); Canvas.SetTop(tile, y); pattern.Children.Add(tile);
        }
        var button = new Button { Content = "Underlying action", Width = 160, Height = 32 }; Canvas.SetLeft(button, 20); Canvas.SetTop(button, 20); pattern.Children.Add(button);
        CatalogRoot.Children.Add(pattern); await AwaitPresentationAsync(pattern); button.Focus(FocusState.Keyboard);
        var blurHost = new Grid(); var portal = new Grid { Width = width, Height = height, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) }; portal.Children.Add(blurHost);
        var popup = new Popup { XamlRoot = root, Child = portal, IsLightDismissEnabled = false, ShouldConstrainToRootBounds = true };
        var compositor = ElementCompositionPreview.GetElementVisual(blurHost).Compositor;
        using var backdrop = compositor.CreateBackdropBrush();
        using var blur = new GaussianBlurEffect { BlurAmount = 2, BorderMode = EffectBorderMode.Hard, Source = new CompositionEffectSourceParameter("backdrop") };
        using var factory = compositor.CreateEffectFactory(blur); using var brush = factory.CreateBrush(); brush.SetSourceParameter("backdrop", backdrop);
        using var sprite = compositor.CreateSpriteVisual(); sprite.Size = new Vector2((float)width, (float)height); sprite.Brush = brush;
        try
        {
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, "probe-before.png"));
            popup.IsOpen = true; await AwaitPresentationAsync(portal); ElementCompositionPreview.SetElementChildVisual(blurHost, sprite); await AwaitPresentationAsync(portal);
            var popupFocusRetained = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root), button);
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, "probe-popup-backdrop.png"));
            pattern.IsTabStop = true; var neutralRootFocus = pattern.Focus(FocusState.Pointer);
            var neutralRootOwner = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root), pattern);
            input.Key(0x09); await AwaitPresentationAsync(pattern);
            var tabFromRootEntersFirstChild = ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root), button);
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, diagnosticOnly = true, component = "loading-overlay-probe", popupFocusRetained, neutralRootFocus, neutralRootOwner, tabFromRootEntersFirstChild, width, height, environment = RuntimeEnvironment() }));
        }
        finally
        {
            popup.IsOpen = false; ElementCompositionPreview.SetElementChildVisual(blurHost, null); popup.Child = null;
            pattern.Children.Remove(blurHost); CatalogRoot.Children.Remove(pattern);
        }
    }
}
