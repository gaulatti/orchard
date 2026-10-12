using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

public enum BleeckerButtonVariant { Primary, Secondary, Outline, Subtle, Ghost, Link, Destructive }
public enum BleeckerButtonSize { ExtraSmall, Small, Medium, Large }

/// <summary>A Bleecker action with native Windows activation, commands and focus.</summary>
public sealed class BleeckerButton : ContentControl
{
    private Button? action;
    private ProgressRing? progress;
    private Border? linkUnderline;
    private ResourceDictionary? palette;

    public BleeckerButton() => DefaultStyleKey = typeof(BleeckerButton);

    public static readonly DependencyProperty VariantProperty = DependencyProperty.Register(
        nameof(Variant), typeof(BleeckerButtonVariant), typeof(BleeckerButton),
        new PropertyMetadata(BleeckerButtonVariant.Primary, OnVariantChanged));
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(BleeckerButtonSize), typeof(BleeckerButton),
        new PropertyMetadata(BleeckerButtonSize.Medium, OnPresentationChanged));
    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(BleeckerButton), new PropertyMetadata(false, OnPresentationChanged));
    public static readonly DependencyProperty LoadingTextProperty = DependencyProperty.Register(
        nameof(LoadingText), typeof(string), typeof(BleeckerButton), new PropertyMetadata("Loading", OnPresentationChanged));
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(BleeckerButton), new PropertyMetadata(null));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(BleeckerButton), new PropertyMetadata(null));

    public BleeckerButtonVariant Variant { get => (BleeckerButtonVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public BleeckerButtonSize Size { get => (BleeckerButtonSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public bool IsLoading { get => (bool)GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }
    public string LoadingText { get => (string)GetValue(LoadingTextProperty); set => SetValue(LoadingTextProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public event RoutedEventHandler? Click;

    protected override void OnApplyTemplate()
    {
        if (action is not null)
        {
            action.Click -= OnClick;
            if (palette is not null) action.Resources.MergedDictionaries.Remove(palette);
        }
        base.OnApplyTemplate();
        action = GetTemplateChild("PART_Action") as Button
            ?? throw new InvalidOperationException("BleeckerButton requires a native Button named PART_Action.");
        progress = GetTemplateChild("PART_Progress") as ProgressRing
            ?? throw new InvalidOperationException("BleeckerButton requires a ProgressRing named PART_Progress.");
        linkUnderline = GetTemplateChild("PART_LinkUnderline") as Border;
        action.Click += OnClick;
        ApplyVariant();
    }

    private static void OnVariantChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerButton)owner).ApplyVariant();
    private static void OnPresentationChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerButton)owner).ApplyPresentation();

    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        if (action is null) return;
        if (palette is not null) action.Resources.MergedDictionaries.Remove(palette);
        palette = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/Buttons/{Variant}.g.xaml") };
        action.Resources.MergedDictionaries.Add(palette);
        // Reapply explicit setters in the new palette's resource scope.
        action.Style = (Style)palette[typeof(Button)];
        ApplyPresentation();
    }

    private void ApplyPresentation()
    {
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (action is null || progress is null) return;
        var (height, padding, font) = Size switch
        {
            BleeckerButtonSize.ExtraSmall => (28, 10, 12),
            BleeckerButtonSize.Small => (36, 14, 13),
            BleeckerButtonSize.Medium => (40, 18, 14),
            BleeckerButtonSize.Large => (48, 24, 15),
            _ => throw new ArgumentOutOfRangeException(nameof(Size))
        };
        action.MinHeight = Variant == BleeckerButtonVariant.Link ? 0 : height;
        action.Padding = Variant == BleeckerButtonVariant.Link ? new Thickness(0) : new Thickness(padding, 4, padding, 4);
        action.FontSize = font;
        action.IsEnabled = !IsLoading;
        progress.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
        progress.IsActive = IsLoading;
        if (linkUnderline is not null)
            linkUnderline.Visibility = Variant == BleeckerButtonVariant.Link ? Visibility.Visible : Visibility.Collapsed;
        AutomationProperties.SetItemStatus(action, IsLoading ? LoadingText : "");
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (!IsLoading) Click?.Invoke(this, args);
    }
}
