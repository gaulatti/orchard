using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerInput
{
    private TextBlock? fontMetrics;
    private TextBlock? typographyPlaceholder;
    private ScrollViewer? typographyEditor;
    private FrameworkElement? typographyHost;
    private XamlRoot? typographyRoot;
    private TranslateTransform? placeholderBaselineShift;
    private TranslateTransform? editorBaselineShift;
    private bool typographyQueued;

    private void InitializeTypography()
    {
        Loaded += (_, _) =>
        {
            typographyRoot = XamlRoot;
            typographyRoot.Changed += OnTypographyRootChanged;
            QueueTypography();
        };
        Unloaded += (_, _) =>
        {
            if (typographyRoot is not null) typographyRoot.Changed -= OnTypographyRootChanged;
            typographyRoot = null;
            CompositionTarget.Rendering -= OnTypographyRendering;
            typographyQueued = false;
        };
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, FontWeightProperty,
            FontStyleProperty, FontStretchProperty, IsTextScaleFactorEnabledProperty, UseLayoutRoundingProperty,
            PaddingProperty, BorderThicknessProperty })
            RegisterPropertyChangedCallback(property, (_, _) => QueueTypography());
    }

    private void ApplyTypographyTemplate()
    {
        foreach (var part in new FrameworkElement?[] { fontMetrics, typographyPlaceholder, typographyEditor, typographyHost })
            if (part is not null) part.SizeChanged -= OnTypographySizeChanged;
        fontMetrics = GetTemplateChild("InputFontMetrics") as TextBlock;
        typographyPlaceholder = GetTemplateChild("PlaceholderTextContentPresenter") as TextBlock;
        typographyEditor = GetTemplateChild("ContentElement") as ScrollViewer;
        typographyHost = GetTemplateChild("Editor") as FrameworkElement;
        placeholderBaselineShift = new TranslateTransform();
        editorBaselineShift = new TranslateTransform();
        if (typographyPlaceholder is not null) typographyPlaceholder.RenderTransform = placeholderBaselineShift;
        if (typographyEditor is not null) typographyEditor.RenderTransform = editorBaselineShift;
        foreach (var part in new FrameworkElement?[] { fontMetrics, typographyPlaceholder, typographyEditor, typographyHost })
            if (part is not null) part.SizeChanged += OnTypographySizeChanged;
        QueueTypography();
    }

    private void OnTypographyRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => QueueTypography();
    private void OnTypographySizeChanged(object sender, SizeChangedEventArgs args) => QueueTypography();

    private void QueueTypography()
    {
        if (!IsLoaded || typographyQueued) return;
        typographyQueued = true;
        CompositionTarget.Rendering += OnTypographyRendering;
    }

    private void OnTypographyRendering(object? sender, object args)
    {
        CompositionTarget.Rendering -= OnTypographyRendering;
        typographyQueued = false;
        if (!IsLoaded || fontMetrics is null || fontMetrics.ActualHeight <= 0 || typographyHost is null ||
            typographyEditor is null || typographyPlaceholder is null) return;
        var scale = XamlRoot.RasterizationScale;
        var hostY = typographyHost.TransformToVisual(this).TransformPoint(new Point()).Y;
        var baseline = fontMetrics.BaselineOffset;
        // Center the font's natural line box, then place its baseline on a device pixel.
        var target = Math.Round((hostY + typographyHost.ActualHeight / 2 + baseline - fontMetrics.ActualHeight / 2) * scale) / scale;
        if (typographyEditor.ActualHeight > 0)
        {
            var editorY = typographyEditor.TransformToVisual(this).TransformPoint(new Point()).Y - editorBaselineShift!.Y;
            // RichEdit quantizes font ascent separately from its arranged origin.
            var nativeBaseline = Math.Ceiling(editorY * scale + Math.Floor(baseline * scale)) / scale;
            editorBaselineShift.Y = target - nativeBaseline;
        }
        if (typographyPlaceholder.ActualHeight > 0)
        {
            var placeholderY = typographyPlaceholder.TransformToVisual(this).TransformPoint(new Point()).Y - placeholderBaselineShift!.Y;
            var nativeBaseline = Math.Ceiling((placeholderY + baseline) * scale) / scale;
            placeholderBaselineShift.Y = target - nativeBaseline;
        }
    }
}
