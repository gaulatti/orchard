using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A consumer-owned error summary with an optional native retry command.</summary>
public sealed class BleeckerErrorState : Control
{
    private BleeckerButton? retry;
    private TextBlock? description;
    private FrameworkElement? defaultIcon, customIcon;
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings settings = new();

    public BleeckerErrorState()
    {
        DefaultStyleKey = typeof(BleeckerErrorState);
        Loaded += (_, _) => { settings.ColorValuesChanged += OnColorsChanged; UpdatePresentation(); };
        Unloaded += (_, _) => settings.ColorValuesChanged -= OnColorsChanged;
        ActualThemeChanged += (_, _) => UpdatePresentation();
    }

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(BleeckerErrorState),
        new PropertyMetadata("Something went wrong", OnPresentationChanged));
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(nameof(Description), typeof(string), typeof(BleeckerErrorState),
        new PropertyMetadata("We couldn't load this data. Please try again.", OnPresentationChanged));
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(object), typeof(BleeckerErrorState),
        new PropertyMetadata(null, OnPresentationChanged));
    public static readonly DependencyProperty RetryLabelProperty = DependencyProperty.Register(nameof(RetryLabel), typeof(string), typeof(BleeckerErrorState),
        new PropertyMetadata("Try again", OnPresentationChanged));
    public static readonly DependencyProperty RetryCommandProperty = DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(BleeckerErrorState),
        new PropertyMetadata(null, OnPresentationChanged));
    public static readonly DependencyProperty RetryCommandParameterProperty = DependencyProperty.Register(nameof(RetryCommandParameter), typeof(object), typeof(BleeckerErrorState),
        new PropertyMetadata(null));

    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string? RetryLabel { get => (string?)GetValue(RetryLabelProperty); set => SetValue(RetryLabelProperty, value); }
    public ICommand? RetryCommand { get => (ICommand?)GetValue(RetryCommandProperty); set => SetValue(RetryCommandProperty, value); }
    public object? RetryCommandParameter { get => GetValue(RetryCommandParameterProperty); set => SetValue(RetryCommandParameterProperty, value); }
    /// <summary>Observes native retry activation; RetryCommand controls whether the action is present.</summary>
    public event RoutedEventHandler? Retry;

    protected override void OnApplyTemplate()
    {
        if (retry is not null) retry.Click -= OnRetry;
        base.OnApplyTemplate();
        retry = GetTemplateChild("PART_Retry") as BleeckerButton ?? throw new InvalidOperationException("BleeckerErrorState requires PART_Retry.");
        description = GetTemplateChild("PART_Description") as TextBlock ?? throw new InvalidOperationException("BleeckerErrorState requires PART_Description.");
        defaultIcon = GetTemplateChild("PART_DefaultIcon") as FrameworkElement ?? throw new InvalidOperationException("BleeckerErrorState requires PART_DefaultIcon.");
        customIcon = GetTemplateChild("PART_Icon") as FrameworkElement ?? throw new InvalidOperationException("BleeckerErrorState requires PART_Icon.");
        var retryIcon = GetTemplateChild("PART_RetryIcon") as Microsoft.UI.Xaml.Shapes.Path ?? throw new InvalidOperationException("BleeckerErrorState requires PART_RetryIcon.");
        var retryLabel = GetTemplateChild("PART_RetryLabel") as TextBlock ?? throw new InvalidOperationException("BleeckerErrorState requires PART_RetryLabel.");
        retry.ApplyTemplate();
        var nativeAction = ((FrameworkElement)VisualTreeHelper.GetChild(retry,0)).FindName("PART_Action") as Button
            ?? throw new InvalidOperationException("Error State retry requires the Bleecker native action.");
        nativeAction.ApplyTemplate();
        var nativeContent = ((FrameworkElement)VisualTreeHelper.GetChild(nativeAction,0)).FindName("ContentPresenter") as ContentPresenter
            ?? throw new InvalidOperationException("Error State retry requires the native content foreground producer.");
        retryLabel.SetBinding(TextBlock.ForegroundProperty,new Binding { Source=nativeContent,Path=new PropertyPath("Foreground") });
        retryIcon.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty,new Binding { Source=nativeContent,Path=new PropertyPath("Foreground") });
        retry.Click += OnRetry;
        UpdatePresentation();
    }

    private static void OnPresentationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var owner = (BleeckerErrorState)sender;
        owner.UpdatePresentation();
        FrameworkElementAutomationPeer.FromElement(owner)?.InvalidatePeer();
    }
    private void OnRetry(object sender, RoutedEventArgs args) => Retry?.Invoke(this, args);
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdatePresentation);
    private void UpdatePresentation()
    {
        if (retry is null) return;
        retry.Visibility = RetryCommand is null ? Visibility.Collapsed : Visibility.Visible;
        AutomationProperties.SetName(retry, RetryLabel ?? string.Empty);
        description!.Visibility = string.IsNullOrEmpty(Description) ? Visibility.Collapsed : Visibility.Visible;
        defaultIcon!.Visibility = Icon is null ? Visibility.Visible : Visibility.Collapsed;
        customIcon!.Visibility = Icon is null ? Visibility.Collapsed : Visibility.Visible;
        VisualStateManager.GoToState(this, string.IsNullOrEmpty(Title) ? "TitleEmpty" : "TitlePresent", false);
        VisualStateManager.GoToState(this, accessibility.HighContrast ? "ErrorContrast" : ActualTheme == ElementTheme.Dark ? "ErrorDark" : "ErrorLight", false);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new ErrorStatePeer(this);
    private sealed class ErrorStatePeer(BleeckerErrorState owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerErrorState);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override bool IsKeyboardFocusableCore() => false;
        protected override string GetNameCore()
        {
            if (BleeckerAutomationMetadata.TryGetName(owner, this, out var callerName)) return callerName;
            var explicitName = AutomationProperties.GetName(owner);
            if (!string.IsNullOrEmpty(explicitName) || AutomationProperties.GetLabeledBy(owner) is not null) return base.GetNameCore();
            return owner.Title ?? string.Empty;
        }
        protected override string GetHelpTextCore() => BleeckerAutomationMetadata.HasSuppliedValue(owner, AutomationProperties.HelpTextProperty)
            ? AutomationProperties.GetHelpText(owner) : string.IsNullOrEmpty(AutomationProperties.GetHelpText(owner))
            ? owner.Description ?? string.Empty : base.GetHelpTextCore();
    }
}
