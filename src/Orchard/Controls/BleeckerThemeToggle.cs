using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public sealed record BleeckerThemeRequestedEventArgs(BleeckerThemeMode Mode);

/// <summary>A native icon button that requests the next consumer-owned theme mode.</summary>
public sealed class BleeckerThemeToggle : BleeckerIconButton
{
    private ToolTip? defaultTip;
    private ResourceDictionary? tooltipDefaults;
    private readonly ContentControl icon = new() { Name = "ThemeIcon", IsHitTestVisible = false, FlowDirection = FlowDirection.LeftToRight };
    private bool applyingDefaultStyle;

    public BleeckerThemeToggle()
    {
        icon.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/ThemeToggle.xaml") });
        icon.Template = (ControlTemplate)icon.Resources["ThemeToggleIconTemplate"];
        icon.IsTabStop = false;
        icon.Padding = new Thickness(0);
        AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw);
        Content = icon;
        RegisterPropertyChangedCallback(StyleProperty, (_, _) =>
        {
            ApplyPaletteDefaults();
            FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
        });
        ApplyPaletteDefaults();
        IsEnabledChanged += (_, _) => RefreshEnabled();
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => RefreshEnabled());
        Loaded += (_, _) => RefreshEnabled();
        Click += (_, _) => { if (IsLoaded && IsEnabled) ThemeRequested?.Invoke(this, new(BleeckerThemeToggleModel.Next(Mode))); };
        UpdateMode();
    }

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(nameof(Mode), typeof(BleeckerThemeMode), typeof(BleeckerThemeToggle),
        new PropertyMetadata(BleeckerThemeMode.System, (owner, args) => ((BleeckerThemeToggle)owner).UpdateMode((BleeckerThemeMode)args.OldValue)));

    public BleeckerThemeMode Mode
    {
        get => (BleeckerThemeMode)GetValue(ModeProperty);
        set { _ = BleeckerThemeToggleModel.Next(value); SetValue(ModeProperty, value); }
    }
    public event EventHandler<BleeckerThemeRequestedEventArgs>? ThemeRequested;

    private void RefreshEnabled() => icon.Opacity = IsEnabled ? 1 : .45;

    private bool Styled(DependencyProperty property)
    {
        for (var style = Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
        return false;
    }

    private bool UsesDefaultName => ReadLocalValue(AutomationProperties.NameProperty) == DependencyProperty.UnsetValue &&
        !Styled(AutomationProperties.NameProperty) && AutomationProperties.GetName(this).Length == 0 && AutomationProperties.GetLabeledBy(this) is null;

    private void ApplyPaletteDefaults()
    {
        if (applyingDefaultStyle || !ReferenceEquals(Style, VariantStyle) || GetBindingExpression(StyleProperty) is not null) return;
        // Keep the default below native local/style overrides. The theme resource
        // resolves against each target's resources when a caller shares its style.
        var defaults = (ResourceDictionary)XamlReader.Load("<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><ToolTip x:Key='BleeckerThemeToggleDefaultToolTip'/><Style x:Key='BleeckerThemeToggleTooltipRecipe' TargetType='Button'><Setter Property='ToolTipService.ToolTip' Value='{ThemeResource BleeckerThemeToggleDefaultToolTip}'/></Style></ResourceDictionary>");
        defaultTip = (ToolTip)defaults["BleeckerThemeToggleDefaultToolTip"];
        defaultTip.Content = BleeckerThemeToggleModel.Label(Mode);
        if (tooltipDefaults is not null) Resources.MergedDictionaries.Remove(tooltipDefaults);
        tooltipDefaults = defaults;
        Resources.MergedDictionaries.Add(defaults);
        var style = (Style)defaults["BleeckerThemeToggleTooltipRecipe"];
        style.BasedOn = VariantStyle;
        applyingDefaultStyle = true;
        try
        {
            Style = style;
        }
        finally { applyingDefaultStyle = false; }
    }

    private void UpdateMode(BleeckerThemeMode? previousMode = null)
    {
        var label = BleeckerThemeToggleModel.Label(Mode);
        var peer = FrameworkElementAutomationPeer.FromElement(this);
        var oldName = UsesDefaultName && previousMode is {} oldMode ? BleeckerThemeToggleModel.Label(oldMode) : peer?.GetName();
        var paint = " Stroke='{ThemeResource BleeckerThemeToggleIconBrush}' StrokeThickness='1.5' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/>";
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='24' Height='24' FlowDirection='LeftToRight'>" + BleeckerThemeToggleModel.Glyph(Mode).Replace("/>", paint) + "</Canvas>");
        icon.Content = new Viewbox { Width = 18, Height = 18, Child = canvas, UseLayoutRounding = false, IsHitTestVisible = false };
        if (defaultTip is not null) defaultTip.Content = label;
        if (peer is not null)
        {
            peer.InvalidatePeer();
            var newName = peer.GetName();
            if (oldName != newName) peer.RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldName, newName);
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ThemePeer(this);
    private sealed class ThemePeer(BleeckerThemeToggle owner) : ButtonAutomationPeer(owner), IInvokeProvider
    {
        protected override string GetClassNameCore() => nameof(BleeckerThemeToggle);
        protected override string GetNameCore()
        {
            if (owner.ReadLocalValue(AutomationProperties.NameProperty) != DependencyProperty.UnsetValue || owner.Styled(AutomationProperties.NameProperty))
                return AutomationProperties.GetName(owner);
            return owner.UsesDefaultName ? BleeckerThemeToggleModel.Label(owner.Mode) : base.GetNameCore();
        }
        protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Invoke ? this : base.GetPatternCore(pattern);
        void IInvokeProvider.Invoke()
        {
            if (!owner.IsLoaded || owner.XamlRoot is null) throw new ElementNotAvailableException();
            if (!owner.IsEnabled) throw new ElementNotEnabledException();
            base.Invoke();
        }
    }
}
