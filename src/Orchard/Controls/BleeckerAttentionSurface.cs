using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerAttentionElement { Article, Button, Div, ListItem, Section }
public enum BleeckerAttentionDensity { Compact, Comfortable }

/// <summary>A category-to-urgency surface for consumer-owned content and actions.</summary>
public sealed partial class BleeckerAttentionSurface : ContentControl
{
    private readonly BleeckerAttentionGradient gradient = new();
    private readonly SolidColorBrush sourceBorder = new(), borderPaint = new(), backgroundPaint = new();
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> borderMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<(double L, double A, double B, double Alpha)> backgroundMotion = new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<double> liftMotion = new((from,to,progress) => from + (to-from)*progress);
    private readonly BleeckerCssTransition<ShadowState> shadowMotion = new(ShadowState.Mix);
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observations = [];
    private Grid? host, motionRoot, shadowHost;
    private Border? surface, backdrop, focus;
    private Microsoft.UI.Xaml.Shapes.Path? shadowMask;
    private Button? action;
    private ContentPresenter? presenter;
    private TextBlock? text, metrics, contrastBackground, contrastBorder;
    private Grid? paragraph;
    private TranslateTransform? lift, baseline;
    private BleeckerAttentionColor? recipe;
    private XamlRoot? observedRoot;
    private bool pointerOver, updatingRecipe, running, lifecycleQueued;
    private bool? renderedContrast;
    private ElementTheme? renderedTheme;

    public BleeckerAttentionSurface()
    {
        Resources["BleeckerAttentionBackgroundBrush"] = gradient.Brush;
        Resources["BleeckerAttentionBorderBrush"] = sourceBorder;
        DefaultStyleKey = typeof(BleeckerAttentionSurface);
        foreach (var property in new[] { ContentProperty, ContentTemplateProperty, ContentTemplateSelectorProperty, FontSizeProperty, FontFamilyProperty,
            FontWeightProperty, CharacterSpacingProperty, PaddingProperty, CornerRadiusProperty, BorderThicknessProperty, StyleProperty })
            RegisterPropertyChangedCallback(property, (_, _) => { UpdateContent(); UpdateGeometry(); });
        foreach (var property in new[] { BackgroundProperty, BorderBrushProperty })
            RegisterPropertyChangedCallback(property, (_, _) => { ObserveBrushes(); UpdateAppearance(true); });
        foreach (var property in new[] { AutomationProperties.NameProperty, AutomationProperties.LabeledByProperty })
            RegisterPropertyChangedCallback(property, (_, _) => UpdateAccessibleName());
        Loaded += OnLoaded; Unloaded += OnUnloaded;
        SizeChanged += (_, _) => UpdateGeometry();
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => UpdateAppearance(false));
        GotFocus += (_, _) => UpdateFocus(); LostFocus += (_, _) => UpdateFocus();
        Tapped += (_, args) =>
        {
            if (Element == BleeckerAttentionElement.Button || !IsEnabled) return;
            if (Command is not null && !Command.CanExecute(CommandParameter)) return;
            Command?.Execute(CommandParameter); Click?.Invoke(this, args);
        };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(nameof(Hue), typeof(object), typeof(BleeckerAttentionSurface), new PropertyMetadata(null, OnColorChanged));
    public static readonly DependencyProperty IntensityProperty = DependencyProperty.Register(nameof(Intensity), typeof(object), typeof(BleeckerAttentionSurface), new PropertyMetadata(null, OnColorChanged));
    public double? Hue { get => (double?)GetValue(HueProperty); set => SetValue(HueProperty, value); }
    public double? Intensity { get => (double?)GetValue(IntensityProperty); set => SetValue(IntensityProperty, value); }
    public static readonly DependencyProperty ElementProperty = DependencyProperty.Register(nameof(Element), typeof(BleeckerAttentionElement), typeof(BleeckerAttentionSurface), new PropertyMetadata(BleeckerAttentionElement.Article, OnStructureChanged));
    public static readonly DependencyProperty DensityProperty = DependencyProperty.Register(nameof(Density), typeof(BleeckerAttentionDensity), typeof(BleeckerAttentionSurface), new PropertyMetadata(BleeckerAttentionDensity.Comfortable, OnStructureChanged));
    public static readonly DependencyProperty InteractiveProperty = DependencyProperty.Register(nameof(Interactive), typeof(object), typeof(BleeckerAttentionSurface), new PropertyMetadata(null, OnStructureChanged));
    public BleeckerAttentionElement Element { get => (BleeckerAttentionElement)GetValue(ElementProperty); set => SetValue(ElementProperty, value); }
    public BleeckerAttentionDensity Density { get => (BleeckerAttentionDensity)GetValue(DensityProperty); set => SetValue(DensityProperty, value); }
    public bool? Interactive { get => (bool?)GetValue(InteractiveProperty); set => SetValue(InteractiveProperty, value); }
    public bool IsInteractive => Interactive ?? Element == BleeckerAttentionElement.Button;
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BleeckerAttentionSurface), new PropertyMetadata(null, OnCommandChanged));
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BleeckerAttentionSurface), new PropertyMetadata(null, OnCommandChanged));
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event RoutedEventHandler? Click;
    internal FrameworkElement? PointerSurface => motionRoot;
    internal void SetPointerOver(bool value) { if (pointerOver == value) return; pointerOver = value; UpdateAppearance(true); }

    private static void OnColorChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerAttentionSurface)owner).UpdateRecipe();
    private static void OnStructureChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args)
    {
        var control = (BleeckerAttentionSurface)owner;
        if (!Enum.IsDefined(control.Element) || !Enum.IsDefined(control.Density)) throw new ArgumentOutOfRangeException(nameof(Element));
        control.UpdateStructure(); control.UpdateGeometry(); control.UpdateAppearance(true);
        FrameworkElementAutomationPeer.FromElement(control)?.InvalidatePeer();
    }
    private static void OnCommandChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerAttentionSurface)owner).UpdateCommand();
    private void Validate() { if (Hue is null || Intensity is null) throw new InvalidOperationException("Attention Surface Hue and Intensity must resolve while mounted."); }
    private void OnLoaded(object sender, RoutedEventArgs args)
    {
        Validate();QueueLifecycle();
    }
    private void OnUnloaded(object sender, RoutedEventArgs args) => QueueLifecycle();
    private void QueueLifecycle()
    {
        if(lifecycleQueued)return;
        lifecycleQueued=true;
        if(!DispatcherQueue.TryEnqueue(() =>
        {
            lifecycleQueued=false;
            // WinUI can deliver the previous Unloaded after a rapid remount.
            // Reconcile the final owner/root instead of clearing a live renderer.
            if(!IsLoaded){StopRuntime();return;}
            Validate();
            if(!ReferenceEquals(observedRoot,XamlRoot)){StopRuntime();StartRuntime();}
            ObserveBrushes();UpdateRecipe();UpdateStructure();UpdateContent();UpdateGeometry();UpdateAppearance(false);
        })) { lifecycleQueued=false;if(!IsLoaded)StopRuntime(); }
    }
    private void StartRuntime()
    {
        Validate(); observedRoot = XamlRoot; observedRoot.Changed += OnRootChanged;
        settings.ColorValuesChanged += OnColorsChanged;
        if (OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)) settings.AnimationsEnabledChanged += OnAnimationsChanged;
        BleeckerAttentionPointer.Add(this); CompositionTarget.Rendering += OnFrame;
        ObserveBrushes(); UpdateRecipe(); UpdateStructure(); UpdateContent(); UpdateGeometry(); UpdateAppearance(false);
    }
    private void StopRuntime()
    {
        BleeckerAttentionPointer.Remove(this); CompositionTarget.Rendering -= OnFrame;
        if (observedRoot is not null) observedRoot.Changed -= OnRootChanged; observedRoot = null;
        settings.ColorValuesChanged -= OnColorsChanged;
        if (OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)) settings.AnimationsEnabledChanged -= OnAnimationsChanged;
        StopObserving(); gradient.Clear(); ClearShadow(); UpdateAppearance(false);
    }
    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        host = Part<Grid>("PART_Host"); motionRoot = Part<Grid>("PART_Motion"); action = Part<Button>("PART_Action");
        surface = Part<Border>("PART_Surface"); backdrop = Part<Border>("PART_Background"); focus = Part<Border>("PART_Focus");
        shadowHost = Part<Grid>("PART_ShadowHost"); shadowMask = Part<Microsoft.UI.Xaml.Shapes.Path>("PART_ShadowMask");
        presenter = Part<ContentPresenter>("PART_Content"); text = Part<TextBlock>("PART_String"); metrics = Part<TextBlock>("PART_StringMetrics"); paragraph = Part<Grid>("PART_StringParagraph");
        contrastBackground = Part<TextBlock>("PART_ContrastBackground"); contrastBorder = Part<TextBlock>("PART_ContrastBorder");
        motionRoot.RenderTransform = lift = new TranslateTransform(); text.RenderTransform = baseline = new TranslateTransform();
        backdrop.Background = backgroundPaint; surface.BorderBrush = borderPaint; action.Click += OnClick;
        ObserveBrushes(); UpdateRecipe(); UpdateStructure(); UpdateContent(); UpdateGeometry(); UpdateAppearance(false);
    }
    private T Part<T>(string name) where T : FrameworkElement => GetTemplateChild(name) as T ?? throw new InvalidOperationException($"Attention Surface requires {name}.");
    private void OnClick(object sender, RoutedEventArgs args) => Click?.Invoke(this,args);
    private void UpdateStructure()
    {
        if (host is null || action is null || surface is null) return;
        if (Element == BleeckerAttentionElement.Button)
        {
            if (host.Children.Contains(surface)) host.Children.Remove(surface);
            action.Content = surface; action.Visibility = Visibility.Visible;
        }
        else
        {
            action.Content = null; action.Visibility = Visibility.Collapsed;
            if (!host.Children.Contains(surface)) host.Children.Add(surface);
        }
        UpdateCommand(); UpdateAccessibleName(); UpdateFocus();
    }
    private void UpdateCommand() { if (action is null) return; action.Command = Command; action.CommandParameter = CommandParameter; }
    private void UpdateAccessibleName()
    {
        if (action is null) return;
        AutomationProperties.SetLabeledBy(action, AutomationProperties.GetLabeledBy(this));
        AutomationProperties.SetName(action, AutomationProperties.GetName(this) is {Length:>0} name ? name : Content as string ?? "");
    }
    private void UpdateContent()
    {
        if (text is null || presenter is null || metrics is null) return;
        var plain = Content is string && ContentTemplate is null && ContentTemplateSelector is null;
        text.Text = plain ? (string)Content : ""; text.Visibility = plain && text.Text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        presenter.Visibility = plain ? Visibility.Collapsed : Visibility.Visible;
        text.LineHeight = FontSize * 1.5; UpdateAccessibleName();
    }
    private void UpdateRecipe()
    {
        if (IsLoaded) Validate(); if (Hue is not double hue || Intensity is not double intensity) return;
        recipe = BleeckerAttentionColor.Create(hue,intensity);
        var paint = BleeckerColorMix.EncodePremultiplied(recipe.Border);
        updatingRecipe = true;
        try { sourceBorder.Color = Windows.UI.Color.FromArgb(255,paint.R,paint.G,paint.B); sourceBorder.Opacity = paint.Alpha; }
        finally { updatingRecipe = false; }
        UpdateGeometry(); UpdateAppearance(true);
    }
    private bool HasPaddingOverride()
    {
        if (ReadLocalValue(PaddingProperty) != DependencyProperty.UnsetValue) return true;
        for (var style = Style; style is not null; style = style.BasedOn)
            if (style.Setters.OfType<Setter>().Any(setter => setter.Property == PaddingProperty)) return true;
        return false;
    }
    private void DetachTemplate()
    {
        StopObserving(); ClearShadow(); if (action is not null) { action.Click -= OnClick; action.Content = null; }
        if (presenter is not null) presenter.Content = null;
        host = motionRoot = shadowHost = paragraph = null; surface = backdrop = focus = null; shadowMask = null; action = null; presenter = null;
        text = metrics = contrastBackground = contrastBorder = null; lift = baseline = null; gradient.Clear(); running = false;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SurfacePeer(this);
    private sealed class SurfacePeer(BleeckerAttentionSurface owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerAttentionSurface);
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.Element == BleeckerAttentionElement.ListItem ? AutomationControlType.ListItem : AutomationControlType.Group;
        protected override List<AutomationPeer> GetChildrenCore() => (base.GetChildrenCore() ?? []).Where(peer => !ReferenceEquals(peer, owner.metrics is null ? null : FromElement(owner.metrics))).ToList();
    }
}
