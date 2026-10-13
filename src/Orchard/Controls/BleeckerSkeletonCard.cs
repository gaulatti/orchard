using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>The pinned Bleecker loading-card composition.</summary>
public sealed class BleeckerSkeletonCard : Control
{
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings settings = new();
    private StackPanel? body;
    private Grid? textColumn;
    private BleeckerSkeleton? headline;
    private BleeckerSkeleton? subtitle;
    private Border? ring;
    private Grid? shadowHost;
    private Microsoft.UI.Xaml.Shapes.Path? nearMask;
    private Microsoft.UI.Xaml.Shapes.Path? farMask;

    public BleeckerSkeletonCard()
    {
        DefaultStyleKey = typeof(BleeckerSkeletonCard);
        SizeChanged += (_, _) => UpdateSurface();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateSurface());
        RegisterPropertyChangedCallback(BorderThicknessProperty, (_, _) => UpdateSurface());
        Loaded += (_, _) => { settings.ColorValuesChanged += OnColorsChanged; UpdateSurface(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= OnColorsChanged; ClearShadow(); };
    }
    public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(BleeckerSkeletonCard),
        new PropertyMetadata(3, (owner, _) => ((BleeckerSkeletonCard)owner).RebuildRows()));
    public int Rows { get => (int)GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    protected override void OnApplyTemplate()
    {
        ClearShadow();
        body?.Children.Clear();
        if (textColumn is not null) textColumn.SizeChanged -= OnTextSizeChanged;
        body = null; textColumn = null; headline = subtitle = null;
        ring = null; shadowHost = null; nearMask = farMask = null;
        base.OnApplyTemplate();
        if (Template is null) return;
        body = GetTemplateChild("PART_Body") as StackPanel ?? throw new InvalidOperationException("SkeletonCard template requires PART_Body.");
        textColumn = GetTemplateChild("PART_TextColumn") as Grid ?? throw new InvalidOperationException("SkeletonCard template requires PART_TextColumn.");
        headline = GetTemplateChild("PART_Headline") as BleeckerSkeleton ?? throw new InvalidOperationException("SkeletonCard template requires PART_Headline.");
        subtitle = GetTemplateChild("PART_Subtitle") as BleeckerSkeleton ?? throw new InvalidOperationException("SkeletonCard template requires PART_Subtitle.");
        ring = GetTemplateChild("PART_Ring") as Border ?? throw new InvalidOperationException("SkeletonCard template requires PART_Ring.");
        shadowHost = GetTemplateChild("PART_ShadowHost") as Grid ?? throw new InvalidOperationException("SkeletonCard template requires PART_ShadowHost.");
        nearMask = GetTemplateChild("PART_NearMask") as Microsoft.UI.Xaml.Shapes.Path ?? throw new InvalidOperationException("SkeletonCard template requires PART_NearMask.");
        farMask = GetTemplateChild("PART_FarMask") as Microsoft.UI.Xaml.Shapes.Path ?? throw new InvalidOperationException("SkeletonCard template requires PART_FarMask.");
        textColumn.SizeChanged += OnTextSizeChanged;
        RebuildRows();
        UpdateTextWidths();
        UpdateSurface();
    }
    private void RebuildRows()
    {
        if (body is null) return;
        body.Children.Clear();
        for (var row = 0; row < Math.Max(0, Rows); row++) body.Children.Add(new BleeckerSkeleton { Height = 12 });
        body.Margin = new Thickness(0, Rows > 0 ? 12 : 0, 0, 0);
    }
    private void OnTextSizeChanged(object sender, SizeChangedEventArgs args) => UpdateTextWidths();
    private void UpdateTextWidths()
    {
        if (textColumn is null || headline is null || subtitle is null) return;
        headline.Width = textColumn.ActualWidth / 3;
        subtitle.Width = textColumn.ActualWidth / 2;
    }
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateSurface);
    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
    }
    private void UpdateSurface()
    {
        if (ring is not null)
        {
            var border = BorderThickness;
            ring.Margin = new Thickness(-border.Left, -border.Top, -border.Right, -border.Bottom);
            ring.CornerRadius = new CornerRadius(CornerRadius.TopLeft + Math.Max(border.Left, border.Top),
                CornerRadius.TopRight + Math.Max(border.Right, border.Top), CornerRadius.BottomRight + Math.Max(border.Right, border.Bottom),
                CornerRadius.BottomLeft + Math.Max(border.Left, border.Bottom));
        }
        if (shadowHost is null || nearMask is null || farMask is null) return;
        ClearShadow();
        var visible = IsLoaded && !accessibility.HighContrast;
        shadowHost.Visibility = nearMask.Visibility = farMask.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible || ActualWidth <= 0 || ActualHeight <= 0) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var layers = compositor.CreateContainerVisual();
        // Tailwind shadow-sm: 0 1px 3px 0 black/10, 0 1px 2px -1px black/10.
        for (var index = 0; index < 2; index++)
        {
            var inset = index;
            var mask = index == 0 ? nearMask : farMask;
            mask.Width = Math.Max(0, ActualWidth - inset * 2);
            mask.Height = Math.Max(0, ActualHeight - inset * 2);
            mask.Data = BleeckerShadowGeometry.RoundedMask(mask.Width, mask.Height, CornerRadius, inset);
            Canvas.SetLeft(mask, inset); Canvas.SetTop(mask, inset);
            var shadow = compositor.CreateDropShadow();
            shadow.BlurRadius = index == 0 ? 3 : 2;
            shadow.Offset = new Vector3(0, 1, 0);
            shadow.Color = Microsoft.UI.Colors.Black;
            shadow.Opacity = .1f;
            shadow.Mask = mask.GetAlphaMask();
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)mask.Width, (float)mask.Height);
            sprite.Offset = new Vector3(inset, inset, 0);
            sprite.Shadow = shadow;
            layers.Children.InsertAtTop(sprite);
        }
        ElementCompositionPreview.SetElementChildVisual(shadowHost, layers);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BleeckerSkeletonAutomationPeer(this);
}
