using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Orchard;

/// <summary>The pinned Sabella template logo in a fixed, aspect-fit square.</summary>
public sealed class BleeckerBrandMark : Control
{
    private BitmapIcon? mark;
    private SolidColorBrush? observedBrush;
    private long colorToken, opacityToken;

    public BleeckerBrandMark()
    {
        DefaultStyleKey = typeof(BleeckerBrandMark);
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => UpdateTint());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) { DisconnectBrush(); mark = null; } });
        Loaded += (_, _) => UpdateTint();
        Unloaded += (_, _) => DisconnectBrush();
        ActualThemeChanged += (_, _) => UpdateTint();
    }

    public BleeckerBrandMark(double size) : this() => Size = size;

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(BleeckerBrandMark), new PropertyMetadata(64d, (_, args) => ValidateSize((double)args.NewValue)));

    /// <summary>The image's square frame in DIPs; zero hides its ink.</summary>
    public double Size { get => (double)GetValue(SizeProperty); set { ValidateSize(value); SetValue(SizeProperty, value); } }

    private static void ValidateSize(double size)
    {
        if (!double.IsFinite(size) || size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Brand mark size must be finite and nonnegative.");
    }

    protected override void OnApplyTemplate()
    {
        DisconnectBrush(); mark = null;
        if (Template is null) return;
        base.OnApplyTemplate();
        var part = GetTemplateChild("PART_Mark");
        if (part is not null && part is not BitmapIcon) throw new InvalidOperationException("PART_Mark must be a BitmapIcon.");
        mark = part as BitmapIcon;
        UpdateTint();
    }

    private void DisconnectBrush()
    {
        if (observedBrush is null) return;
        observedBrush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, colorToken);
        observedBrush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacityToken);
        observedBrush = null;
    }

    private void UpdateTint()
    {
        if (mark is null) { DisconnectBrush(); return; }
        if (Foreground is not SolidColorBrush brush) throw new InvalidOperationException("The default brand mark requires a SolidColorBrush Foreground. A replacement template can own other brush presentations.");
        if (!ReferenceEquals(observedBrush, brush) || !IsLoaded)
        {
            DisconnectBrush();
            if (IsLoaded)
            {
                observedBrush = brush;
                colorToken = brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => UpdateTint());
                opacityToken = brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => UpdateTint());
            }
        }
        // BitmapIcon reads a color when Foreground is assigned, but does not
        // observe later Color mutations. Preserve the caller's original brush
        // and binding while giving the private renderer its current color.
        var color = brush.Color;
        mark.Foreground = new SolidColorBrush(Color.FromArgb(
            (byte)Math.Round(color.A * brush.Opacity, MidpointRounding.AwayFromZero), color.R, color.G, color.B));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MarkPeer(this);
    private sealed class MarkPeer(BleeckerBrandMark owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerBrandMark);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;
    }
}
