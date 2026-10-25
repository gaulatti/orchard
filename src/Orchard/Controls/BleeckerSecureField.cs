using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A native password editor with Sabella's persistent reveal button.</summary>
public sealed class BleeckerSecureField : Control
{
    private PasswordBox? editor;
    private BleeckerIconButton? reveal;
    private FrameworkElement? eye;
    private FrameworkElement? eyeOff;
    private Grid? shadowHost;
    private Microsoft.UI.Xaml.Shapes.Path? shadowMask;
    private Border? shadowPalette;
    private readonly AccessibilitySettings accessibility = new();
    private readonly UISettings settings = new();

    public BleeckerSecureField()
    {
        DefaultStyleKey = typeof(BleeckerSecureField);
        IsTabStop = false;
        GotFocus += (_, _) =>
        {
            if (FocusState != FocusState.Unfocused && XamlRoot?.IsHostVisible == true && editor?.FocusState == FocusState.Unfocused)
                editor.Focus(FocusState);
        };
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => UpdateEditorName());
        RegisterPropertyChangedCallback(AutomationProperties.LabeledByProperty, (_, _) => UpdateEditorName());
        RegisterPropertyChangedCallback(IsRevealedProperty, (_, _) => UpdateReveal());
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => UpdateEnabledState());
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateShadow());
        SizeChanged += (_, _) => UpdateShadow();
        ActualThemeChanged += (_, _) => UpdateShadow();
        Loaded += (_, _) => { settings.ColorValuesChanged += OnColorsChanged; UpdateShadow(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= OnColorsChanged; ClearShadow(); };
    }

    public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(nameof(Password), typeof(string), typeof(BleeckerSecureField),
        new PropertyMetadata(string.Empty, (owner, _) => ((BleeckerSecureField)owner).PasswordChanged?.Invoke(owner, new RoutedEventArgs())));
    public static readonly DependencyProperty PlaceholderTextProperty = DependencyProperty.Register(nameof(PlaceholderText), typeof(string), typeof(BleeckerSecureField), new PropertyMetadata(string.Empty));
    public static readonly DependencyProperty IsRevealedProperty = DependencyProperty.Register(nameof(IsRevealed), typeof(bool), typeof(BleeckerSecureField), new PropertyMetadata(false));
    /// <summary>The caller's value. Bind with Mode=TwoWay and UpdateSourceTrigger=PropertyChanged.</summary>
    public string Password { get => (string)GetValue(PasswordProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(PasswordProperty, value); } }
    public string PlaceholderText { get => (string)GetValue(PlaceholderTextProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(PlaceholderTextProperty, value); } }
    /// <summary>False by default. The reveal button toggles this state persistently.</summary>
    public bool IsRevealed { get => (bool)GetValue(IsRevealedProperty); set => SetValue(IsRevealedProperty, value); }
    public event RoutedEventHandler? PasswordChanged;

    // Field descriptions/error metadata belong to this composite group. The
    // actual password/value pattern remains on its unmodified native editor.
    protected override AutomationPeer OnCreateAutomationPeer() => new MetadataPeer(this);
    private sealed class MetadataPeer(BleeckerSecureField owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(BleeckerSecureField);
    }

    protected override void OnApplyTemplate()
    {
        if (reveal is not null) reveal.Click -= OnReveal;
        if (editor is not null) editor.ClearValue(PasswordBox.PasswordProperty);
        ClearShadow();
        base.OnApplyTemplate();
        // Deliberately distinct from PasswordBox's native Peek RevealButton part.
        reveal = GetTemplateChild("SecureRevealButton") as BleeckerIconButton;
        editor = GetTemplateChild("SecurePasswordEditor") as PasswordBox;
        if (editor is not null) editor.SetBinding(PasswordBox.PasswordProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Password)), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
        eye = GetTemplateChild("SecureEye") as FrameworkElement;
        eyeOff = GetTemplateChild("SecureEyeOff") as FrameworkElement;
        shadowHost = GetTemplateChild("SecureShadowHost") as Grid;
        shadowMask = GetTemplateChild("SecureShadowMask") as Microsoft.UI.Xaml.Shapes.Path;
        shadowPalette = GetTemplateChild("SecureShadowPalette") as Border;
        if (reveal is not null) reveal.Click += OnReveal;
        UpdateReveal();
        UpdateEditorName();
        UpdateEnabledState();
        UpdateShadow();
    }

    private void UpdateEnabledState() => VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", false);

    private void UpdateEditorName()
    {
        if (editor is null) return;
        AutomationProperties.SetName(editor, AutomationProperties.GetName(this));
        AutomationProperties.SetLabeledBy(editor, AutomationProperties.GetLabeledBy(this));
    }

    private void OnReveal(object sender, RoutedEventArgs args)
    {
        if (!IsEnabled) return;
        IsRevealed = !IsRevealed;
    }

    private void UpdateReveal()
    {
        var visible = IsRevealed;
        if (editor is not null) editor.PasswordRevealMode = visible ? PasswordRevealMode.Visible : PasswordRevealMode.Hidden;
        if (reveal is not null) AutomationProperties.SetName(reveal, visible ? "Hide password" : "Show password");
        if (eye is not null) eye.Visibility = visible ? Visibility.Collapsed : Visibility.Visible;
        if (eyeOff is not null) eyeOff.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateShadow);
    private void ClearShadow()
    {
        if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null);
    }

    private void UpdateShadow()
    {
        ClearShadow();
        if (!IsLoaded || ActualTheme == ElementTheme.Dark || accessibility.HighContrast ||
            shadowHost is null || shadowMask is null || shadowPalette?.Background is not SolidColorBrush color ||
            shadowHost.ActualWidth <= 0 || shadowHost.ActualHeight <= 0) return;
        var width = shadowHost.ActualWidth;
        var height = shadowHost.ActualHeight;
        shadowMask.Width = width;
        shadowMask.Height = height;
        shadowMask.Data = BleeckerShadowGeometry.RoundedMask(width, height, CornerRadius, 0);
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var shadow = compositor.CreateDropShadow();
        shadow.Color = color.Color;
        shadow.Opacity = .055f;
        shadow.BlurRadius = 14;
        shadow.Offset = new Vector3(0, 5, 0);
        shadow.Mask = shadowMask.GetAlphaMask();
        var sprite = compositor.CreateSpriteVisual();
        sprite.Size = new Vector2((float)width, (float)height);
        sprite.Shadow = shadow;
        ElementCompositionPreview.SetElementChildVisual(shadowHost, sprite);
    }
}
