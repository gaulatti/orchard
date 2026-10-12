using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

public enum BleeckerInputSize { Small, Medium, Large }

/// <summary>A single-line Bleecker text editor with decorative consumer-owned adornments.</summary>
public sealed class BleeckerInput : TextBox
{
    private bool pointerOver;

    public BleeckerInput()
    {
        DefaultStyleKey = typeof(BleeckerInput);
        PointerEntered += (_, _) => { pointerOver = true; UpdateAppearance(); };
        PointerExited += (_, _) => { pointerOver = false; UpdateAppearance(); };
        GotFocus += (_, _) => UpdateAppearance();
        LostFocus += (_, _) => UpdateAppearance();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateAppearance());
        RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => UpdateAppearance());
        Loaded += (_, _) => { UpdateGeometry(); UpdateAppearance(); };
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
        UpdateGeometry();
        UpdateAppearance();
    }

    private static void OnGeometryChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerInput)owner).UpdateGeometry();
    private void UpdateGeometry()
    {
        var (height, inset, font) = InputSize switch
        {
            BleeckerInputSize.Small => (36d, 12d, 13d),
            BleeckerInputSize.Medium => (40d, 14d, 14d),
            BleeckerInputSize.Large => (44d, 16d, 15d),
            _ => throw new ArgumentOutOfRangeException(nameof(InputSize))
        };
        MinHeight = height;
        FontSize = font;
        Padding = new Thickness(StartIcon is null ? inset : 40, 0, EndIcon is null ? inset : 40, 0);
    }

    private void UpdateAppearance()
    {
        var state = !IsEnabled ? "InputDisabled" : FocusState != FocusState.Unfocused ? "InputFocused" : pointerOver ? "InputHovered" : "InputNormal";
        if (IsError && IsEnabled) state += "Error";
        VisualStateManager.GoToState(this, state, false);
    }
}
