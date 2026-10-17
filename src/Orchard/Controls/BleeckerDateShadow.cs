using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

// The pinned date-picker.tsx recipes are shared by Calendar and picker triggers.
internal sealed class BleeckerDateShadow : IDisposable
{
    internal sealed record Recipe(float Blur, float Y, float Spread, float Opacity, byte R, byte G, byte B);
    internal static readonly Recipe Selected = new(2, 1, 0, .12f, 21, 48, 66);
    internal static readonly Recipe Rest = new(2, 1, 0, .025f, 26, 55, 77);
    internal static readonly Recipe Hover = new(10, 3, -8, .25f, 26, 55, 77);
    private readonly Control owner;
    private readonly Grid host;
    private readonly Microsoft.UI.Xaml.Shapes.Path mask, exterior;
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly List<IDisposable> resources = [];
    private readonly long radiusToken;
    private Recipe? recipe;
    private SpriteVisual? caster, filtered, output;
    private CompositionVisualSurface? source, filteredSource;
    private CompositionColorBrush? color;
    private CompositionEffectBrush? effect;
    private CompositionMaskBrush? outputMask;
    private bool disposed;
    private (double Width, double Height, CornerRadius Radius, Recipe Recipe)? geometry;

    internal BleeckerDateShadow(Control owner, Grid host, Microsoft.UI.Xaml.Shapes.Path mask, Microsoft.UI.Xaml.Shapes.Path exterior)
    {
        ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(host); ArgumentNullException.ThrowIfNull(mask); ArgumentNullException.ThrowIfNull(exterior);
        this.owner = owner; this.host = host; this.mask = mask; this.exterior = exterior;
        owner.Loaded += Loaded; owner.Unloaded += Unloaded; host.SizeChanged += SizeChanged;
        radiusToken = owner.RegisterPropertyChangedCallback(Control.CornerRadiusProperty, (_, _) => Render());
        if (owner.IsLoaded) settings.ColorValuesChanged += ColorsChanged;
    }
    internal void Set(Recipe? value) { recipe = value; Render(); }
    private void Loaded(object sender, RoutedEventArgs args) { settings.ColorValuesChanged -= ColorsChanged; settings.ColorValuesChanged += ColorsChanged; Render(); }
    private void Unloaded(object sender, RoutedEventArgs args) { settings.ColorValuesChanged -= ColorsChanged; Clear(); }
    private void ColorsChanged(UISettings sender, object args) => owner.DispatcherQueue.TryEnqueue(Render);
    private void SizeChanged(object sender, SizeChangedEventArgs args) => Render();
    private void Clear()
    {
        ElementCompositionPreview.SetElementChildVisual(host, null);
        for (var index = resources.Count - 1; index >= 0; index--) resources[index].Dispose();
        resources.Clear(); caster = filtered = output = null; source = filteredSource = null; color = null; effect = null; outputMask = null; geometry = null;
    }
    private void Render()
    {
        if (disposed) return;
        if (!owner.IsLoaded || accessibility.HighContrast || recipe is null || host.ActualWidth <= 0 || host.ActualHeight <= 0) { Clear(); return; }
        var next = (host.ActualWidth, host.ActualHeight, owner.CornerRadius, recipe);
        if (geometry == next) return;
        var inset = -recipe.Spread; var width = Math.Max(0, host.ActualWidth - inset * 2); var height = Math.Max(0, host.ActualHeight - inset * 2);
        if (width == 0 || height == 0) { Clear(); return; }
        mask.Width = width; mask.Height = height; mask.Data = BleeckerShadowGeometry.RoundedMask(width, height, owner.CornerRadius, inset);
        var sigma = recipe.Blur / 2; var extent = MathF.Ceiling(sigma * 3);
        if (output is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
            color = compositor.CreateColorBrush();
            caster = compositor.CreateSpriteVisual(); source = compositor.CreateVisualSurface(); source.SourceVisual = caster;
            var input = compositor.CreateSurfaceBrush(source);
            using var blur = new GaussianBlurEffect { Name = "cssBlur", BlurAmount = sigma, BorderMode = EffectBorderMode.Soft, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("mask") };
            using var clipped = new CompositeEffect { Mode = CanvasComposite.SourceIn, Sources = { new CompositionEffectSourceParameter("exterior"), blur } };
            var factory = compositor.CreateEffectFactory(clipped, ["cssBlur.BlurAmount"]);
            effect = factory.CreateBrush(); effect.SetSourceParameter("mask", input);
            filtered = compositor.CreateSpriteVisual(); filtered.Brush = effect;
            filteredSource = compositor.CreateVisualSurface(); filteredSource.SourceVisual = filtered;
            var filteredMask = compositor.CreateSurfaceBrush(filteredSource);
            outputMask = compositor.CreateMaskBrush(); outputMask.Source = color; outputMask.Mask = filteredMask;
            output = compositor.CreateSpriteVisual(); output.Brush = outputMask;
            resources.Add(color); resources.Add(caster); resources.Add(source); resources.Add(input); resources.Add(factory); resources.Add(effect); resources.Add(filtered); resources.Add(filteredSource); resources.Add(filteredMask); resources.Add(outputMask); resources.Add(output);
            ElementCompositionPreview.SetElementChildVisual(host, output);
        }
        effect!.Properties.InsertScalar("cssBlur.BlurAmount", sigma);
        color!.Color = Windows.UI.Color.FromArgb(255, recipe.R, recipe.G, recipe.B);
        caster!.Size = new Vector2((float)width, (float)height); caster.Brush = mask.GetAlphaMask();
        source!.SourceOffset = new Vector2(-extent); source.SourceSize = new Vector2((float)width + extent * 2, (float)height + extent * 2);
        filtered!.Size = source.SourceSize; filteredSource!.SourceSize = source.SourceSize;
        output.Size = source.SourceSize; output.Offset = new Vector3(inset - extent, inset + recipe.Y - extent, 0); output.Opacity = recipe.Opacity;
        // CSS outer shadows exclude the complete rounded border box, including
        // translucent range fields. Apply this mask after blurring and offsetting.
        var hole = BleeckerShadowGeometry.RoundedMask(host.ActualWidth, host.ActualHeight, owner.CornerRadius, 0); hole.FillRule = FillRule.EvenOdd;
        var bounds = new PathFigure { StartPoint = new Point(output.Offset.X, output.Offset.Y), IsClosed = true, IsFilled = true };
        bounds.Segments.Add(new LineSegment { Point = new Point(output.Offset.X + output.Size.X, output.Offset.Y) });
        bounds.Segments.Add(new LineSegment { Point = new Point(output.Offset.X + output.Size.X, output.Offset.Y + output.Size.Y) });
        bounds.Segments.Add(new LineSegment { Point = new Point(output.Offset.X, output.Offset.Y + output.Size.Y) });
        hole.Figures.Add(bounds); hole.Transform = new TranslateTransform { X = -output.Offset.X, Y = -output.Offset.Y };
        exterior.Width = output.Size.X; exterior.Height = output.Size.Y; exterior.Data = hole; effect.SetSourceParameter("exterior", exterior.GetAlphaMask());
        geometry = next;
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; owner.Loaded -= Loaded; owner.Unloaded -= Unloaded; host.SizeChanged -= SizeChanged;
        owner.UnregisterPropertyChangedCallback(Control.CornerRadiusProperty, radiusToken); settings.ColorValuesChanged -= ColorsChanged; Clear();
    }
}
