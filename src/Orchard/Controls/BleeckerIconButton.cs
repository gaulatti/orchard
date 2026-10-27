using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerIconButtonVariant { Default, Subtle, Ghost }
public enum BleeckerIconButtonSize { Small, Medium, Large }

/// <summary>A native icon-only button. Consumers must supply an accessible name.</summary>
public class BleeckerIconButton : Button
{
    private ResourceDictionary? palette;
    private Control? foregroundContent;
    private Binding? foregroundBinding;
    private Control? observedContent;
    private long styleCallback;
    private readonly UISettings motionSettings = new();
    private CompositeTransform? pressTransform;
    private Storyboard? pressMotion;

    public BleeckerIconButton()
    {
        ApplyVariant();
        ApplySize();
        RegisterPropertyChangedCallback(ContentProperty, (_, _) => BindContentForeground());
        RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, (_, _) => UpdatePressMotion(true));
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdatePressMotion(false));
        Loaded += (_, _) => UpdatePressMotion(false);
        Unloaded += (_, _) => { pressMotion?.Stop(); pressMotion = null; };
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        BindContentForeground();
        pressMotion?.Stop(); pressMotion = null;
        var pressRoot = VisualTreeHelper.GetChildrenCount(this) > 0 ? VisualTreeHelper.GetChild(this, 0) as FrameworkElement : null;
        pressTransform = null;
        if (pressRoot is not null)
        {
            var existing = pressRoot.RenderTransform;
            var identity = existing is MatrixTransform matrix &&
                matrix.Matrix.M11 == 1 && matrix.Matrix.M22 == 1 &&
                matrix.Matrix.M12 == 0 && matrix.Matrix.M21 == 0 &&
                matrix.Matrix.OffsetX == 0 && matrix.Matrix.OffsetY == 0;
            if (existing is null || identity)
            {
                pressTransform = new CompositeTransform();
                pressRoot.RenderTransformOrigin = new Point(.5, .5);
                pressRoot.RenderTransform = pressTransform;
            }
        }
        UpdatePressMotion(false);
    }

    private void UpdatePressMotion(bool animate)
    {
        if (pressTransform is null) return;
        var fromScale = pressTransform.ScaleX;
        var fromY = pressTransform.TranslateY;
        var pressed = IsEnabled && IsPressed;
        var targetScale = pressed ? .97 : 1;
        var targetY = pressed ? 1 : 0;
        pressMotion?.Stop(); pressMotion = null;
        pressTransform.ScaleX = pressTransform.ScaleY = targetScale;
        pressTransform.TranslateY = targetY;
        if (!animate || !IsLoaded || !motionSettings.AnimationsEnabled ||
            (fromScale == targetScale && fromY == targetY)) return;
        var duration = TimeSpan.FromMilliseconds(BleeckerTokens.Durations["control"]);
        var storyboard = new Storyboard { Duration = duration };
        Tween("ScaleX", fromScale, targetScale);
        Tween("ScaleY", fromScale, targetScale);
        Tween("TranslateY", fromY, targetY);
        pressMotion = storyboard;
        storyboard.Completed += (_, _) => { if (ReferenceEquals(pressMotion, storyboard)) { storyboard.Stop(); pressMotion = null; } };
        storyboard.Begin();

        void Tween(string property, double from, double to)
        {
            if (from == to) return;
            var animation = new DoubleAnimationUsingKeyFrames { Duration = duration, EnableDependentAnimation = true };
            animation.KeyFrames.Add(new DiscreteDoubleKeyFrame { KeyTime = TimeSpan.Zero, Value = from });
            animation.KeyFrames.Add(new SplineDoubleKeyFrame
            {
                KeyTime = duration, Value = to,
                KeySpline = new KeySpline { ControlPoint1 = new Point(.22, 1), ControlPoint2 = new Point(.36, 1) }
            });
            Storyboard.SetTarget(animation, pressTransform);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
    }

    private void BindContentForeground()
    {
        if (observedContent is not null) observedContent.UnregisterPropertyChangedCallback(StyleProperty, styleCallback);
        observedContent = Content as Control;
        if (observedContent is not null)
            styleCallback = observedContent.RegisterPropertyChangedCallback(StyleProperty, (_, _) => BindContentForeground());
        if (foregroundContent is not null && foregroundContent.GetBindingExpression(ForegroundProperty)?.ParentBinding == foregroundBinding)
            foregroundContent.ClearValue(ForegroundProperty);
        foregroundContent = null;
        foregroundBinding = null;
        if (Content is not Control content || content.ReadLocalValue(ForegroundProperty) != DependencyProperty.UnsetValue) return;
        for (var style = content.Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == ForegroundProperty)) return;
        if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(this) == 0 ||
            Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(this, 0) is not ContentPresenter presenter) return;
        foregroundContent = content;
        foregroundBinding = new Binding { Source = presenter, Path = new PropertyPath("Foreground") };
        content.SetBinding(ForegroundProperty, foregroundBinding);
    }

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(BleeckerIconButtonVariant), typeof(BleeckerIconButton),
        new PropertyMetadata(BleeckerIconButtonVariant.Default, (owner, _) => ((BleeckerIconButton)owner).ApplyVariant()));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(BleeckerIconButtonSize), typeof(BleeckerIconButton),
        new PropertyMetadata(BleeckerIconButtonSize.Medium, (owner, _) => ((BleeckerIconButton)owner).ApplySize()));

    public BleeckerIconButtonVariant Variant { get => (BleeckerIconButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerIconButtonSize Size { get => (BleeckerIconButtonSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>The current palette recipe, for specialized icon-button defaults.</summary>
    protected Style VariantStyle => (Style)(palette ?? throw new InvalidOperationException("Icon Button palette is not initialized."))[typeof(Button)];

    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        if (palette is not null) Resources.MergedDictionaries.Remove(palette);
        palette = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/Buttons/Icon{Variant}.g.xaml") };
        Resources.MergedDictionaries.Add(palette);
        Style = (Style)palette[typeof(Button)];
    }

    private void ApplySize()
    {
        var dimension = Size switch
        {
            BleeckerIconButtonSize.Small => 32,
            BleeckerIconButtonSize.Medium => 40,
            BleeckerIconButtonSize.Large => 44,
            _ => throw new ArgumentOutOfRangeException(nameof(Size))
        };
        Width = MinWidth = dimension;
        Height = MinHeight = dimension;
        Padding = new Thickness(0);
    }
}
