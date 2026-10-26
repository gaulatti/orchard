using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public enum BleeckerInputSize { Small, Medium, Large }

/// <summary>A single-line Bleecker text editor with decorative consumer-owned adornments.</summary>
public sealed partial class BleeckerInput : TextBox
{
    private bool pointerOver;
    private string? appearanceState;
    private bool applyingAppearance;
    private readonly TextBlock geometryDefaults = new();
    private Binding? defaultFontBinding, defaultPaddingBinding, defaultMinHeightBinding;
    private bool updatingDefaults;

    public BleeckerInput()
    {
        DefaultStyleKey = typeof(BleeckerInput);
        InitializeShadow();
        InitializeMotion();
        InitializePointer();
        InitializeTypography();
        RegisterPropertyChangedCallback(HeightProperty, (_, _) => UpdateGeometry());
        foreach (var property in new[] { FontSizeProperty, PaddingProperty, MinHeightProperty, StyleProperty })
            RegisterPropertyChangedCallback(property, (_, _) => EnsureDefaultBindings());
        GotFocus += (_, _) => UpdateAppearance();
        LostFocus += (_, _) => UpdateAppearance();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { UpdatePointerObservation(); UpdateAppearance(); });
        RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => UpdateAppearance());
        Loaded += (_, _) => { UpdateGeometry(); UpdateAppearance(false); };
    }

    public static readonly DependencyProperty InputSizeProperty = DependencyProperty.Register(nameof(InputSize), typeof(BleeckerInputSize), typeof(BleeckerInput),
        new PropertyMetadata(BleeckerInputSize.Medium, OnGeometryChanged));
    public BleeckerInputSize InputSize { get => (BleeckerInputSize)GetValue(InputSizeProperty); set => SetValue(InputSizeProperty, value); }
    public static readonly DependencyProperty StartIconProperty = DependencyProperty.Register(nameof(StartIcon), typeof(object), typeof(BleeckerInput), new PropertyMetadata(null, OnGeometryChanged));
    public object? StartIcon { get => GetValue(StartIconProperty); set => SetValue(StartIconProperty, value); }
    public static readonly DependencyProperty EndIconProperty = DependencyProperty.Register(nameof(EndIcon), typeof(object), typeof(BleeckerInput), new PropertyMetadata(null, OnGeometryChanged));
    public object? EndIcon { get => GetValue(EndIconProperty); set => SetValue(EndIconProperty, value); }
    public static readonly DependencyProperty IsErrorProperty = DependencyProperty.Register(nameof(IsError), typeof(bool), typeof(BleeckerInput),
        new PropertyMetadata(false, (owner, _) => ((BleeckerInput)owner).UpdateAppearance()));
    public bool IsError { get => (bool)GetValue(IsErrorProperty); set => SetValue(IsErrorProperty, value); }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        ApplyShadowTemplate();
        ApplyMotionTemplate();
        ApplyTypographyTemplate();
        appearanceState = null;
        UpdateGeometry();
        UpdateAppearance(false);
    }

    private static void OnGeometryChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerInput)owner).UpdateGeometry();
    private (double Height,double Inset,double Font) Geometry=>InputSize switch
    {
        BleeckerInputSize.Small => (36d, 12d, 13d),
        BleeckerInputSize.Medium => (40d, 14d, 14d),
        BleeckerInputSize.Large => (44d, 16d, 15d),
        _ => throw new ArgumentOutOfRangeException(nameof(InputSize))
    };
    private void UpdateGeometry()
    {
        var (height,inset,font)=Geometry;
        // A caller's explicit Height can replace the size recipe's height.
        geometryDefaults.MinHeight = double.IsNaN(Height) ? height : Math.Min(height, Height);
        geometryDefaults.FontSize = font;
        geometryDefaults.Padding = new Thickness(StartIcon is null ? inset : 40, 0, EndIcon is null ? inset : 40, 0);
        EnsureDefaultBindings();
    }
    private void EnsureDefaultBindings()
    {
        if (updatingDefaults) return;
        updatingDefaults = true;
        try
        {
            static bool Sets(Style? style, DependencyProperty property)
            {
                // The framework TextBox style is the baseline, not a caller
                // override. A derived Input style can override that baseline.
                for (; style is not null && style.TargetType != typeof(TextBox); style = style.BasedOn)
                    if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
                return false;
            }
            bool Styled(DependencyProperty property)
            {
                if (Style is { } explicitStyle) return Sets(explicitStyle, property);
                for (DependencyObject? scope = this; scope is not null; scope = VisualTreeHelper.GetParent(scope))
                    if (scope is FrameworkElement element && element.Resources.TryGetValue(typeof(BleeckerInput), out var resource) && resource is Style implicitStyle)
                        return Sets(implicitStyle, property);
                return Application.Current.Resources.TryGetValue(typeof(BleeckerInput), out var applicationStyle) && applicationStyle is Style style && Sets(style, property);
            }
            void Default(DependencyProperty property, ref Binding? binding, string path)
            {
                var owned = binding is not null && ReferenceEquals(GetBindingExpression(property)?.ParentBinding, binding);
                if (Styled(property)) { if (owned) ClearValue(property); return; }
                if (ReadLocalValue(property) == DependencyProperty.UnsetValue)
                {
                    binding = new Binding { Source = geometryDefaults, Path = new PropertyPath(path) };
                    SetBinding(property, binding);
                }
            }
            Default(FontSizeProperty, ref defaultFontBinding, nameof(TextBlock.FontSize));
            Default(PaddingProperty, ref defaultPaddingBinding, nameof(TextBlock.Padding));
            Default(MinHeightProperty, ref defaultMinHeightBinding, nameof(TextBlock.MinHeight));
        }
        finally { updatingDefaults = false; }
    }

    private void UpdateAppearance(bool animate = true)
    {
        var state = !IsEnabled ? pointerOver ? "InputDisabledHovered" : "InputDisabled" : FocusState != FocusState.Unfocused ? "InputFocused" : pointerOver ? "InputHovered" : "InputNormal";
        if (IsError) state += "Error";
        var changed = appearanceState != state;
        appearanceState = state;
        applyingAppearance = true;
        try { VisualStateManager.GoToState(this, state, false); }
        finally { applyingAppearance = false; }
        if (changed || !animate) RequestMotion(animate);
        UpdateShadow(animate);
    }
}
