using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

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
    private bool pointerOver;
    private bool templateCleared;
    private readonly UISettings uiSettings = new();
    private Storyboard? spinnerAnimation;
    private RotateTransform? spinnerRotation;
    private Grid? linkTextHost;
    private TextBlock? linkText;
    private ContentPresenter? consumerContent;
    private readonly BleeckerReferenceText.Presentation linkPresentation = new();
    private readonly BleeckerReferenceText.IntrinsicWidth linkWidth = new();

    public BleeckerButton()
    {
        DefaultStyleKey = typeof(BleeckerButton);
        RegisterPropertyChangedCallback(ContentProperty, (_, _) => { UpdateAccessibleName(); UpdateLinkText(); });
        RegisterPropertyChangedCallback(ContentTemplateProperty, (_, _) => UpdateLinkText());
        RegisterPropertyChangedCallback(ContentTemplateSelectorProperty, (_, _) => UpdateLinkText());
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => UpdateAccessibleName());
        RegisterPropertyChangedCallback(AutomationProperties.LabeledByProperty, (_, _) => UpdateAccessibleName());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) =>
        {
            templateCleared = Template is null;
            if (templateCleared) DetachTemplate();
        });
        Loaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                uiSettings.AnimationsEnabledChanged += OnAnimationsChanged;
            UpdateSpinnerMotion();
        };
        Unloaded += (_, _) =>
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041))
                uiSettings.AnimationsEnabledChanged -= OnAnimationsChanged;
            StopSpinnerMotion();
        };
    }

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
        DetachTemplate();
        // ContentControl can synthesize a text-only template after an explicit
        // null without another Template-property notification. It has no action
        // parts; keep the caller's cleared state until a new template is set.
        if (templateCleared || Template is null) return;
        base.OnApplyTemplate();
        action = GetTemplateChild("PART_Action") as Button
            ?? throw new InvalidOperationException("BleeckerButton requires a native Button named PART_Action.");
        progress = GetTemplateChild("PART_Progress") as ProgressRing
            ?? throw new InvalidOperationException("BleeckerButton requires a ProgressRing named PART_Progress.");
        linkUnderline = GetTemplateChild("PART_LinkUnderline") as Border;
        linkTextHost = GetTemplateChild("PART_LinkTextHost") as Grid;
        linkText = GetTemplateChild("PART_LinkText") as TextBlock;
        consumerContent = GetTemplateChild("PART_Content") as ContentPresenter;
        if (linkText is not null && linkTextHost is not null) BleeckerTextLayout.SetUseReferenceLineBox(linkText, true);
        action.Click += OnClick;
        action.PointerEntered += OnPointerEntered;
        action.PointerExited += OnPointerExited;
        action.IsEnabledChanged += OnActionEnabledChanged;
        action.Loaded += OnActionLoaded;
        UpdateAccessibleName();
        ApplyVariant();
    }

    private void DetachTemplate()
    {
        StopSpinnerMotion();
        if (action is not null)
        {
            action.Click -= OnClick;
            action.PointerEntered -= OnPointerEntered;
            action.PointerExited -= OnPointerExited;
            action.IsEnabledChanged -= OnActionEnabledChanged;
            action.Loaded -= OnActionLoaded;
            if (palette is not null) action.Resources.MergedDictionaries.Remove(palette);
        }
        action = null;
        progress = null;
        palette = null;
        linkUnderline = null;
        linkTextHost = null;
        linkText = null;
        consumerContent = null;
        pointerOver = false;
    }

    private static void OnVariantChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerButton)owner).ApplyVariant();
    private static void OnPresentationChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerButton)owner).ApplyPresentation();

    private void UpdateAccessibleName()
    {
        if (action is null) return;
        var explicitName = AutomationProperties.GetName(this);
        var label = AutomationProperties.GetLabeledBy(this);
        AutomationProperties.SetLabeledBy(action, label);
        // The internal Grid is implementation detail, not the action's label.
        AutomationProperties.SetName(action, !string.IsNullOrEmpty(explicitName) ? explicitName
            : label is not null ? "" : Content as string ?? "");
    }

    private void ApplyVariant()
    {
        if (!Enum.IsDefined(Variant)) throw new ArgumentOutOfRangeException(nameof(Variant));
        if (action is null) return;
        if (palette is not null) action.Resources.MergedDictionaries.Remove(palette);
        palette = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/Buttons/{Variant}.g.xaml") };
        action.Resources.MergedDictionaries.Add(palette);
        // Reapply explicit setters in the new palette's resource scope.
        action.Style = (Style)palette[typeof(Button)];
        BindLinkUnderline();
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
        // The source link recipe deliberately omits the size classes and inherits 16px.
        action.FontSize = Variant == BleeckerButtonVariant.Link ? 16 : font;
        action.IsEnabled = !IsLoading;
        progress.Visibility = IsLoading ? Visibility.Visible : Visibility.Collapsed;
        progress.IsActive = IsLoading;
        UpdateSpinnerMotion();
        UpdateLinkUnderline();
        AutomationProperties.SetItemStatus(action, IsLoading ? LoadingText : "");
        UpdateLinkText();
    }

    private void UpdateLinkText()
    {
        if (action is null || linkTextHost is null || linkText is null || consumerContent is null) return;
        var plain = Variant == BleeckerButtonVariant.Link && Content is string && ContentTemplate is null && ContentTemplateSelector is null;
        linkTextHost.Visibility = plain ? Visibility.Visible : Visibility.Collapsed;
        consumerContent.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
        if (!plain) return;
        linkText.FontFamily = action.FontFamily; linkText.FontSize = action.FontSize; linkText.FontWeight = action.FontWeight;
        linkText.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; linkText.FlowDirection = FlowDirection; linkText.Language = Language;
        linkPresentation.Update(linkText, (string)Content);
        var width = Math.Ceiling(linkWidth.Measure(linkText, (string)Content) * 64) / 64;
        linkText.Width = width; linkTextHost.Width = width;
    }

    private void OnPointerEntered(object sender, PointerRoutedEventArgs args) { pointerOver = true; UpdateLinkUnderline(); }
    private void OnActionLoaded(object sender, RoutedEventArgs args) { BindLinkUnderline(); UpdateSpinnerMotion(); UpdateLinkText(); }
    private void OnAnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) =>
        DispatcherQueue.TryEnqueue(UpdateSpinnerMotion);
    private void StopSpinnerMotion()
    {
        spinnerAnimation?.Stop();
        spinnerAnimation = null;
        if (spinnerRotation is not null) spinnerRotation.Angle = 0;
        spinnerRotation = null;
    }
    private void UpdateSpinnerMotion()
    {
        if (!IsLoaded || !IsLoading || !uiSettings.AnimationsEnabled) { StopSpinnerMotion(); return; }
        if (spinnerAnimation is not null || progress is null) return;
        progress.ApplyTemplate();
        spinnerRotation = FindSpinnerPath(progress)?.RenderTransform as RotateTransform;
        // Consumer templates retain ownership of their own progress presentation.
        if (spinnerRotation is null) return;
        var rotation = new DoubleAnimation { From = 0, To = 360, Duration = TimeSpan.FromSeconds(1), RepeatBehavior = RepeatBehavior.Forever };
        Storyboard.SetTarget(rotation, spinnerRotation);
        Storyboard.SetTargetProperty(rotation, "Angle");
        spinnerAnimation = new Storyboard();
        spinnerAnimation.Children.Add(rotation);
        spinnerAnimation.Begin();
    }
    private static Microsoft.UI.Xaml.Shapes.Path? FindSpinnerPath(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Microsoft.UI.Xaml.Shapes.Path path && path.Name == "PART_SpinnerPath") return path;
            if (FindSpinnerPath(child) is { } nested) return nested;
        }
        return null;
    }
    private void BindLinkUnderline()
    {
        if (action is null) return;
        action.ApplyTemplate();
        if (FindPresenter(action) is { } presenter)
        {
            linkUnderline?.SetBinding(Border.BackgroundProperty, new Binding { Source = presenter, Path = new PropertyPath("Foreground") });
            progress?.SetBinding(Control.ForegroundProperty, new Binding { Source = presenter, Path = new PropertyPath("Foreground") });
            linkText?.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = presenter, Path = new PropertyPath("Foreground") });
        }
    }
    private static ContentPresenter? FindPresenter(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is ContentPresenter presenter) return presenter;
            if (FindPresenter(child) is { } nested) return nested;
        }
        return null;
    }
    private void OnPointerExited(object sender, PointerRoutedEventArgs args) { pointerOver = false; UpdateLinkUnderline(); }
    private void OnActionEnabledChanged(object sender, DependencyPropertyChangedEventArgs args) => UpdateLinkUnderline();
    private void UpdateLinkUnderline()
    {
        if (linkUnderline is not null)
            linkUnderline.Visibility = Variant == BleeckerButtonVariant.Link && pointerOver && action?.IsEnabled == true
                ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnClick(object sender, RoutedEventArgs args)
    {
        if (!IsLoading) Click?.Invoke(this, args);
    }
}
