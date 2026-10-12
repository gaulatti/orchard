using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerCardVariant { Surface, Outlined, Elevated, Subtle, Transparent }
public enum BleeckerCardPadding { None, Small, Medium, Large }

/// <summary>A Bleecker surface for consumer-owned content.</summary>
public sealed class BleeckerCard : ContentControl
{
    private ResourceDictionary? palette;
    private Grid? shadowHost;
    private Microsoft.UI.Xaml.Shapes.Path? nearMask;
    private Microsoft.UI.Xaml.Shapes.Path? farMask;
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings uiSettings = new();

    public BleeckerCard()
    {
        DefaultStyleKey = typeof(BleeckerCard);
        ApplyVariant();
        ApplyPadding();
        SizeChanged += (_, _) => UpdateShadow();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateShadow());
        Loaded += (_, _) => { uiSettings.ColorValuesChanged += OnColorsChanged; UpdateShadow(); };
        Unloaded += (_, _) => { uiSettings.ColorValuesChanged -= OnColorsChanged; ClearShadow(); };
    }

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(nameof(Variant), typeof(BleeckerCardVariant), typeof(BleeckerCard),
        new PropertyMetadata(BleeckerCardVariant.Surface, (owner, _) => ((BleeckerCard)owner).ApplyVariant()));
    public static readonly DependencyProperty PaddingSizeProperty = DependencyProperty.Register(nameof(PaddingSize), typeof(BleeckerCardPadding), typeof(BleeckerCard),
        new PropertyMetadata(BleeckerCardPadding.Medium, (owner, _) => ((BleeckerCard)owner).ApplyPadding()));
    public BleeckerCardVariant Variant { get => (BleeckerCardVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerCardPadding PaddingSize { get => (BleeckerCardPadding)GetValue(PaddingSizeProperty); set => SetValue(PaddingSizeProperty, value); }

    protected override void OnApplyTemplate()
    {
        ClearShadow();
        base.OnApplyTemplate();
        shadowHost = GetTemplateChild("PART_ShadowHost") as Grid;
        nearMask = GetTemplateChild("PART_NearMask") as Microsoft.UI.Xaml.Shapes.Path;
        farMask = GetTemplateChild("PART_FarMask") as Microsoft.UI.Xaml.Shapes.Path;
        UpdateShadow();
    }
    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        if (palette is not null) Resources.MergedDictionaries.Remove(palette);
        palette = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/Cards/{Variant}.g.xaml") };
        Resources.MergedDictionaries.Add(palette);
        Style = (Style)palette[typeof(BleeckerCard)];
        UpdateShadow();
    }
    private void ApplyPadding() => Padding = new Thickness(PaddingSize switch
    {
        BleeckerCardPadding.None => 0, BleeckerCardPadding.Small => 16,
        BleeckerCardPadding.Medium => 24, BleeckerCardPadding.Large => 32,
        _ => throw new ArgumentOutOfRangeException(nameof(PaddingSize))
    });
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateShadow);
    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
    }
    private void UpdateShadow()
    {
        if (shadowHost is null || nearMask is null || farMask is null) return;
        var visible = IsLoaded && Variant == BleeckerCardVariant.Elevated && !accessibility.HighContrast;
        shadowHost.Visibility = nearMask.Visibility = farMask.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        ClearShadow();
        if (!visible || ActualWidth <= 0 || ActualHeight <= 0) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var layers = compositor.CreateContainerVisual();
        // Bleecker --shadow-raised: two layers, including the negative spread.
        for (var i = 0; i < BleeckerTokens.RaisedShadows.Count; i++)
        {
            var layer = BleeckerTokens.RaisedShadows[i];
            var mask = i == 0 ? nearMask : farMask;
            var inset = -layer.Spread;
            mask.Width = Math.Max(0, ActualWidth - inset * 2);
            mask.Height = Math.Max(0, ActualHeight - inset * 2);
            mask.Data = RoundedMask(mask.Width, mask.Height, CornerRadius, inset);
            Canvas.SetLeft(mask, inset); Canvas.SetTop(mask, inset);
            var shadow = compositor.CreateDropShadow();
            shadow.BlurRadius = layer.Blur;
            shadow.Offset = new Vector3(layer.X, layer.Y, 0);
            shadow.Color = Windows.UI.Color.FromArgb(255, layer.R, layer.G, layer.B);
            shadow.Opacity = layer.Opacity;
            shadow.Mask = mask.GetAlphaMask();
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)mask.Width, (float)mask.Height);
            sprite.Offset = new Vector3(inset, inset, 0);
            sprite.Shadow = shadow;
            layers.Children.InsertAtTop(sprite);
        }
        ElementCompositionPreview.SetElementChildVisual(shadowHost, layers);
    }

    private static PathGeometry RoundedMask(double width, double height, CornerRadius corners, double inset)
    {
        var radii = new[] { corners.TopLeft, corners.TopRight, corners.BottomRight, corners.BottomLeft }
            .Select(radius => Math.Max(0, radius - inset)).ToArray();
        // Normalize overlapping corners together, preserving their relative sizes.
        var scale = 1d;
        foreach (var (length, total) in new[] { (width, radii[0] + radii[1]), (height, radii[1] + radii[2]), (width, radii[2] + radii[3]), (height, radii[3] + radii[0]) })
            if (total > 0) scale = Math.Min(scale, length / total);
        for (var i = 0; i < radii.Length; i++) radii[i] *= scale;
        var figure = new PathFigure { StartPoint = new Point(radii[0], 0), IsClosed = true, IsFilled = true };
        void Line(double x, double y) => figure.Segments.Add(new LineSegment { Point = new Point(x, y) });
        void Corner(double radius, double x, double y)
        {
            if (radius == 0) Line(x, y);
            else figure.Segments.Add(new ArcSegment { Point = new Point(x, y), Size = new Size(radius, radius), SweepDirection = SweepDirection.Clockwise });
        }
        Line(width - radii[1], 0); Corner(radii[1], width, radii[1]);
        Line(width, height - radii[2]); Corner(radii[2], width - radii[2], height);
        Line(radii[3], height); Corner(radii[3], 0, height - radii[3]);
        Line(0, radii[0]); Corner(radii[0], radii[0], 0);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        return geometry;
    }
}
