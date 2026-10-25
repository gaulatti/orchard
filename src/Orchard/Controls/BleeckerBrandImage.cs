using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Orchard;

// A live XAML image remains the authoritative decoder/renderer, including SVG,
// animated images and consumer-owned ImageSource instances. Effects operate on
// its pixels, rather than replacing custom artwork with a monochrome logo.
internal sealed class BleeckerBrandImage : Grid
{
    internal event EventHandler? IntrinsicOpened;
    internal bool IsSourceOpened { get; private set; }
    private readonly Image image = new() { Stretch = Stretch.Uniform, IsHitTestVisible = false };
    private readonly Grid sourceHost = new() { IsHitTestVisible = false };
    private readonly Border effectHost = new() { IsHitTestVisible = false };
    private CompositionVisualSurface? surface;
    private CompositionSurfaceBrush? sourceBrush;
    private CompositionEffectFactory? invertFactory, maskFactory;
    private CompositionEffectBrush? invertBrush, maskBrush;
    private SpriteVisual? sprite;
    private XamlRoot? observedRoot;
    private bool invert, contrast;
    private Color maskColor;
    internal Image Image => image;
    internal event ExceptionRoutedEventHandler? ImageFailed;

    internal BleeckerBrandImage()
    {
        FlowDirection = FlowDirection.LeftToRight;
        HighContrastAdjustment = ElementHighContrastAdjustment.None;
        sourceHost.Children.Add(image); Children.Add(sourceHost); Children.Add(effectHost);
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        AutomationProperties.SetAccessibilityView(image, AccessibilityView.Raw);
        ElementCompositionPreview.SetIsTranslationEnabled(sourceHost, true);
        Loaded += (_, _) => { observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += OnRootChanged; UpdateSurface(); };
        Unloaded += (_, _) => { if (observedRoot is not null) observedRoot.Changed -= OnRootChanged; observedRoot = null; Disconnect(); };
        image.ImageOpened += (_, _) => { IsSourceOpened = true; InvalidateMeasure(); UpdateSurface(); IntrinsicOpened?.Invoke(this, EventArgs.Empty); };
        image.ImageFailed += (sender, args) => { IsSourceOpened = false; ImageFailed?.Invoke(sender, args); };
        SizeChanged += (_, _) => UpdateSurface();
        image.SizeChanged += (_, _) => UpdateSurface();
    }

    internal ImageSource? Source { get => image.Source; set { if (ReferenceEquals(image.Source, value)) return; IsSourceOpened = false; image.Source = value; } }
    internal void SetAppearance(bool dark, bool highContrast, Color foreground)
    {
        invert = dark; contrast = highContrast; maskColor = foreground; UpdateSurface();
    }
    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateSurface();
    private void UpdateSurface()
    {
        if (!IsLoaded || XamlRoot is null || image.ActualWidth <= 0 || image.ActualHeight <= 0) return;
        if (surface is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(effectHost).Compositor;
            surface = compositor.CreateVisualSurface();
            surface.SourceVisual = ElementCompositionPreview.GetElementVisual(image);
            sourceBrush = compositor.CreateSurfaceBrush(surface);
            using var inverted = new InvertEffect { Source = new CompositionEffectSourceParameter("image") };
            invertFactory = compositor.CreateEffectFactory(inverted);
            invertBrush = invertFactory.CreateBrush(); invertBrush.SetSourceParameter("image", sourceBrush);
            using var color = new ColorSourceEffect { Name = "maskColor", Color = maskColor };
            using var masked = new CompositeEffect { Mode = CanvasComposite.SourceIn };
            masked.Sources.Add(new CompositionEffectSourceParameter("image")); masked.Sources.Add(color);
            maskFactory = compositor.CreateEffectFactory(masked, ["maskColor.Color"]);
            maskBrush = maskFactory.CreateBrush(); maskBrush.SetSourceParameter("image", sourceBrush);
            sprite = compositor.CreateSpriteVisual();
            ElementCompositionPreview.SetElementChildVisual(effectHost, sprite);
        }
        var size = new Vector2((float)image.ActualWidth, (float)image.ActualHeight);
        surface.SourceSize = size; sprite!.Size = size;
        maskBrush!.Properties.InsertColor("maskColor.Color", maskColor);
        var filtered = invert || contrast;
        sprite.Brush = contrast ? maskBrush : invertBrush;
        sprite.IsVisible = filtered;
        // Opacity=0 on a XAML ancestor suppresses the pixels captured by the
        // visual surface. Translate only the private source host beyond this
        // native window, preserving its live source and alpha without painting
        // the unfiltered image beneath the filtered output. Translation survives
        // layout changes; caller geometry and hit testing remain XAML-owned.
        // Keep the private decoder on physical pixels. A fractional off-screen
        // translation otherwise introduces fractional ancestor render bounds and
        // partially transparent viewport edges, although caller layout is unchanged.
        var scale = XamlRoot.RasterizationScale;
        var translation = filtered ? -(float)(Math.Ceiling((XamlRoot.Size.Width + ActualWidth + 1) * scale) / scale) : 0;
        ElementCompositionPreview.GetElementVisual(sourceHost).Properties.InsertVector3("Translation", new Vector3(translation, 0, 0));
    }
    private void Disconnect()
    {
        ElementCompositionPreview.SetElementChildVisual(effectHost, null);
        if (surface is not null) surface.SourceVisual = null;
        sprite?.Dispose(); maskBrush?.Dispose(); invertBrush?.Dispose(); maskFactory?.Dispose(); invertFactory?.Dispose();
        sourceBrush?.Dispose(); surface?.Dispose();
        sprite = null; maskBrush = invertBrush = null; maskFactory = invertFactory = null; sourceBrush = null; surface = null;
    }
}
