using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerStatCard
{
    private readonly List<StatTextMetrics> textMetrics = [];
    private readonly UISettings textSettings = new();
    private XamlRoot? typographyRoot;
    private bool typographyQueued;

    private void InitializeTypography()
    {
        LayoutUpdated += (_, _) => QueueTypography();
        Loaded += (_, _) =>
        {
            typographyRoot = XamlRoot;
            typographyRoot.Changed += TypographyRootChanged;
            QueueTypography();
        };
        Unloaded += (_, _) =>
        {
            if (typographyRoot is not null) typographyRoot.Changed -= TypographyRootChanged;
            typographyRoot = null;
            StopTypographyFrame();
        };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTypography(); });
    }

    private void AttachTypography()
    {
        foreach (var text in new[] { title, value, description, trendValue, trendLabel, iconText, footerText, iconScalar, footerScalar }.OfType<TextBlock>())
        {
            var item = new StatTextMetrics(text);
            textMetrics.Add(item);
            text.SizeChanged += TypographySizeChanged;
            foreach (var property in new[] { TextBlock.FontFamilyProperty, TextBlock.FontSizeProperty,
                TextBlock.FontWeightProperty, TextBlock.FontStyleProperty, TextBlock.FontStretchProperty,
                TextBlock.LineHeightProperty, TextBlock.IsTextScaleFactorEnabledProperty, TextBlock.TextProperty })
                item.Observers.Add((property, text.RegisterPropertyChangedCallback(property, (_, _) => QueueTypography())));
        }
        QueueTypography();
    }

    private void DetachTypography()
    {
        StopTypographyFrame();
        foreach (var item in textMetrics)
        {
            item.Text.SizeChanged -= TypographySizeChanged;
            foreach (var (property, token) in item.Observers) item.Text.UnregisterPropertyChangedCallback(property, token);
            if (ReferenceEquals(item.Text.RenderTransform, item.AppliedTransform))
            {
                if (item.OriginalTransform is null) item.Text.ClearValue(RenderTransformProperty);
                else item.Text.RenderTransform = item.OriginalTransform;
            }
        }
        textMetrics.Clear();
    }

    private void TypographyRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => QueueTypography();
    private void TypographySizeChanged(object sender, SizeChangedEventArgs args) => QueueTypography();
    private void StopTypographyFrame()
    {
        CompositionTarget.Rendering -= AlignTypography;
        typographyQueued = false;
    }
    private void QueueTypography()
    {
        if (!IsLoaded || typographyQueued) return;
        typographyQueued = true;
        CompositionTarget.Rendering += AlignTypography;
    }
    private void AlignTypography(object? sender, object args)
    {
        StopTypographyFrame();
        if (!IsLoaded) return;
        foreach (var item in textMetrics)
        {
            var text = item.Text;
            if (text.Visibility != Visibility.Visible || text.ActualHeight <= 0 || text.LineHeight <= 0)
            { item.Shift.Y = 0; continue; }
            var metrics = item.Metrics;
            metrics.FontFamily = text.FontFamily; metrics.FontSize = text.FontSize; metrics.FontWeight = text.FontWeight;
            metrics.FontStyle = text.FontStyle; metrics.FontStretch = text.FontStretch;
            metrics.IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled;
            metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            metrics.Arrange(new Rect(0, 0, metrics.DesiredSize.Width, metrics.DesiredSize.Height));
            // CSS rounds font ascent/descent before flooring half-leading.
            // WinUI scales the ascent to BlockLineHeight instead. Shift paint
            // to the source baseline without changing wrapping or allocation.
            var ascent = Math.Round(metrics.BaselineOffset, MidpointRounding.AwayFromZero);
            var descent = Math.Round(metrics.ActualHeight - metrics.BaselineOffset, MidpointRounding.AwayFromZero);
            var lineHeight = text.LineHeight * (text.IsTextScaleFactorEnabled ? textSettings.TextScaleFactor : 1);
            var baseline = ascent + Math.Floor((lineHeight - ascent - descent) / 2);
            // Read the arrange output rather than subtracting our last paint
            // shift from a float transform: that subtraction can change which
            // side of a half-pixel boundary a later update observes.
            var parent = (UIElement)VisualTreeHelper.GetParent(text);
            var offset = text.ActualOffset;
            var origin = parent.TransformToVisual(XamlRoot.Content).TransformPoint(new Point(offset.X, offset.Y)).Y;
            var scale = XamlRoot.RasterizationScale;
            // Snap the final baseline once, in device coordinates. Leaving a
            // half-pixel baseline to separate float operations can put identical
            // source positions on opposite sides of the rasterizer's boundary.
            var deviceBaseline = Math.Round((origin + baseline) * scale, MidpointRounding.AwayFromZero) / scale;
            item.Shift.Y = deviceBaseline - origin - text.BaselineOffset;
        }
    }

    private sealed class StatTextMetrics
    {
        internal TextBlock Text { get; }
        internal TextBlock Metrics { get; } = new() { Text = "Mg", UseLayoutRounding = false };
        internal TranslateTransform Shift { get; } = new();
        internal Transform? OriginalTransform { get; }
        internal Transform AppliedTransform { get; }
        internal List<(DependencyProperty Property, long Token)> Observers { get; } = [];
        internal StatTextMetrics(TextBlock text)
        {
            Text = text; OriginalTransform = text.RenderTransform;
            if (OriginalTransform is null) AppliedTransform = Shift;
            else
            {
                var group = new TransformGroup();
                group.Children.Add(OriginalTransform); group.Children.Add(Shift);
                AppliedTransform = group;
            }
            text.RenderTransform = AppliedTransform;
        }
    }
}
