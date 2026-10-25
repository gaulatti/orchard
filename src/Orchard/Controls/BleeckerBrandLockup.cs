using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerBrandLockupSize { Small, Large }

/// <summary>A consumer-owned link containing the pinned logo and optional brand name.</summary>
public sealed class BleeckerBrandLockup : Control
{
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly ResourceDictionary actions = new() { Source = new Uri("ms-appx:///Orchard/Themes/BrandActions.xaml") };
    private Grid? host, brand;
    private BleeckerBrandImage? logo;
    private Border? separator;
    private TextBlock? name, separatorPalette, contrastPalette;
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> paletteObservers = [];
    private bool? renderedContrast;
    private ElementTheme? renderedTheme;
    private HyperlinkButton? link;
    private BleeckerBrandLinkContext? context;
    private DataTemplate? renderedTemplate;
    private readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    private readonly BleeckerCssTransition<double> opacityMotion = new((from, to, progress) => from + (to - from) * progress, BleeckerCssEasing.Default);
    private bool opacityRendering;
    private ICommand? observedCommand;
    private bool hovering;
    private BleeckerContentText? inlineName;
    internal event EventHandler? IntrinsicMetricsChanged;
    internal bool InlineMetricsReady => logo is not null && name is not null;
    // A composed inline host needs the preferred width before applying its
    // shrink-to-fit constraint; measuring a wrapping star column alone gives
    // its minimum content width in that context.
    internal void PrepareInline(double available)
    {
        if (brand is null || logo is null || name is null) throw new InvalidOperationException("Inline brand metrics require an applied template.");
        logo.Width = double.NaN; logo.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var logoWidth = Math.Floor(logo.DesiredSize.Width * 64) / 64; logo.Width = logoWidth;
        if (inlineName is null)
        {
            inlineName = new("PART_InlineName", "BleeckerPrimarySemiboldFontFamily", 30, 36, "BleeckerTextPrimaryBrush");
            inlineName.VerticalAlignment = VerticalAlignment.Center; Grid.SetColumn(inlineName, 2); brand.Children.Remove(name); brand.Children.Add(inlineName);
        }
        inlineName.Text.FontFamily = name.FontFamily; inlineName.Text.FontWeight = name.FontWeight; inlineName.Text.FontSize = name.FontSize; inlineName.Text.LineHeight = name.LineHeight; inlineName.Text.CharacterSpacing = name.CharacterSpacing; inlineName.Text.Foreground = name.Foreground; inlineName.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
        inlineName.FlowDirection = FlowDirection; inlineName.Language = Language; inlineName.Set(BrandName, null); inlineName.Width = double.NaN; inlineName.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var named = !string.IsNullOrEmpty(BrandName); var gap = named ? 33 : 0; var preferred = logoWidth + gap + inlineName.NaturalWidth;
        // The source's auto-width image contributes zero to inline-flex's
        // intrinsic minimum, then keeps its actual width during flex layout.
        // Its empty one-pixel separator may shrink; the text stops at its
        // original min-content width, so a very narrow anchor can overflow.
        var width = Math.Max(named ? gap + inlineName.MinimumWidth : 0, Math.Min(available, preferred));
        var room = width - logoWidth - (named ? 32 : 0); var separatorWidth = named ? 1d : 0;
        if (named && room < 1 + inlineName.NaturalWidth)
        {
            separatorWidth = Math.Clamp(room / (1 + inlineName.NaturalWidth), 0, 1);
            if (room - separatorWidth < inlineName.MinimumWidth) separatorWidth = Math.Max(0, room - inlineName.MinimumWidth);
            separatorWidth = Math.Round(separatorWidth * 64, MidpointRounding.AwayFromZero) / 64;
        }
        separator!.Width = separatorWidth;
        var textWidth = Math.Max(inlineName.MinimumWidth, Math.Min(inlineName.NaturalWidth, Math.Max(0, room - separatorWidth)));
        inlineName.Width = textWidth; inlineName.Visibility = named ? Visibility.Visible : Visibility.Collapsed;
        if (Width != width) Width = width;
    }

    public BleeckerBrandLockup()
    {
        DefaultStyleKey = typeof(BleeckerBrandLockup);
        LogoSource = new SvgImageSource(new Uri("ms-appx:///Orchard/Assets/Images/gaulatti-logo.svg"));
        Loaded += (_, _) =>
        {
            settings.ColorValuesChanged += OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnMotionChanged;
            ObserveCommand(); ObservePalette(); Render();
            CompositionTarget.Rendering += OnThemeFrame;
        };
        Unloaded += (_, _) =>
        {
            settings.ColorValuesChanged -= OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnMotionChanged;
            StopObservingCommand(); StopObservingPalette(); hovering = false; SetLogoOpacity(false);
            CompositionTarget.Rendering -= OnThemeFrame;
        };
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            if (IsLoaded) UpdateAppearance();
        });
        PointerEntered += (_, _) => { hovering = true; SetLogoOpacity(true); };
        PointerExited += (_, args) =>
        {
            var point = args.GetCurrentPoint(this).Position;
            if (new Rect(0, 0, ActualWidth, ActualHeight).Contains(point)) return;
            hovering = false; SetLogoOpacity(true);
        };
        RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => { context?.NotifyCanExecuteChanged(); SetLogoOpacity(false); });
        RegisterPropertyChangedCallback(ForegroundProperty, (_, _) => UpdateAppearance());
        RegisterPropertyChangedCallback(AutomationProperties.NameProperty, (_, _) => UpdateAccessibleName());
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }

    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerBrandLockup), new PropertyMetadata(null, OnChanged));
    public string Href { get => (string?)GetValue(HrefProperty) ?? throw new InvalidOperationException("BrandLockup Href must resolve before mounting."); set => SetValue(HrefProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty LogoAltProperty = DependencyProperty.Register(nameof(LogoAlt), typeof(string), typeof(BleeckerBrandLockup), new PropertyMetadata("gaulatti", OnChanged));
    public string LogoAlt { get => (string)GetValue(LogoAltProperty); set => SetValue(LogoAltProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public static readonly DependencyProperty LogoSourceProperty = DependencyProperty.Register(nameof(LogoSource), typeof(ImageSource), typeof(BleeckerBrandLockup), new PropertyMetadata(null, OnChanged));
    public ImageSource? LogoSource { get => (ImageSource?)GetValue(LogoSourceProperty); set => SetValue(LogoSourceProperty, value); }
    public static readonly DependencyProperty BrandNameProperty = DependencyProperty.Register(nameof(BrandName), typeof(string), typeof(BleeckerBrandLockup), new PropertyMetadata(null, OnChanged));
    public string? BrandName { get => (string?)GetValue(BrandNameProperty); set => SetValue(BrandNameProperty, value); }
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(nameof(Size), typeof(BleeckerBrandLockupSize), typeof(BleeckerBrandLockup), new PropertyMetadata(BleeckerBrandLockupSize.Small, OnChanged));
    public BleeckerBrandLockupSize Size { get => (BleeckerBrandLockupSize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public static readonly DependencyProperty LinkTemplateProperty = DependencyProperty.Register(nameof(LinkTemplate), typeof(DataTemplate), typeof(BleeckerBrandLockup), new PropertyMetadata(null, OnChanged));
    public DataTemplate? LinkTemplate { get => (DataTemplate?)GetValue(LinkTemplateProperty); set => SetValue(LinkTemplateProperty, value); }
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(BleeckerBrandLockup), new PropertyMetadata(null, OnChanged));
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BleeckerBrandLockup), new PropertyMetadata(null, OnChanged));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }
    public event EventHandler<string>? Invoked;
    public event ExceptionRoutedEventHandler? LogoFailed;
    public event EventHandler? LogoOpened;
    /// <summary>Whether the current non-null logo has completed its native decoder's ImageOpened event.</summary>
    public bool IsLogoLoaded => logo?.IsSourceOpened == true;
    private static void OnChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) { var control = (BleeckerBrandLockup)owner; control.ObserveCommand(); control.Render(); }

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate();
        if (Template is null) return;
        host = GetTemplateChild("PART_LinkHost") as Grid ?? throw new InvalidOperationException("BrandLockup requires PART_LinkHost.");
        separatorPalette = GetTemplateChild("PART_SeparatorPalette") as TextBlock ?? throw new InvalidOperationException("BrandLockup requires PART_SeparatorPalette.");
        contrastPalette = GetTemplateChild("PART_ContrastPalette") as TextBlock ?? throw new InvalidOperationException("BrandLockup requires PART_ContrastPalette.");
        ObservePalette();
        Render();
    }
    private void DetachTemplate()
    {
        opacityRendering = false;
        StopObservingPalette(); separatorPalette = contrastPalette = null;
        if (context is not null) context.Content = null;
        if (link is not null) link.Content = null;
        host?.Children.Clear(); host = brand = null; logo = null; separator = null; name = null; inlineName = null; link = null; context = null; renderedTemplate = null;
    }
    private void Render()
    {
        if (host is null) return;
        if (GetValue(HrefProperty) is not string) { if (IsLoaded) _ = Href; return; }
        if (!Enum.IsDefined(Size)) throw new ArgumentOutOfRangeException(nameof(Size));
        if (brand is null || !ReferenceEquals(renderedTemplate, LinkTemplate))
        {
            if (context is not null) context.Content = null;
            if (link is not null) link.Content = null;
            host.Children.Clear(); link = null;
            brand = new Grid { ColumnSpacing = 16, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false };
            brand.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); brand.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
            brand.ColumnDefinitions.Add(new() { Width = new GridLength(1, GridUnitType.Star) });
            logo = new BleeckerBrandImage { Name = "PART_Logo", VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false };
            logo.IntrinsicOpened += (_, _) => { InvalidateMeasure(); IntrinsicMetricsChanged?.Invoke(this, EventArgs.Empty); LogoOpened?.Invoke(this, EventArgs.Empty); };
            logo.ImageFailed += (sender, args) => LogoFailed?.Invoke(this, args);
            separator = new Border { Name = "PART_Separator", Width = 1, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
            name = new TextBlock { Name = "PART_Name", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                CharacterSpacing = -20, LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, UseLayoutRounding = false };
            foreach (var child in new UIElement[] { separator, name }) AutomationProperties.SetAccessibilityView(child, AccessibilityView.Raw);
            name.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = this, Path = new PropertyPath(nameof(FontFamily)) });
            name.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = this, Path = new PropertyPath(nameof(Foreground)) });
            Grid.SetColumn(separator, 1); Grid.SetColumn(name, 2); brand.Children.Add(logo); brand.Children.Add(separator); brand.Children.Add(name);
            context = new BleeckerBrandLinkContext(this) { Content = brand };
            if (LinkTemplate is null)
            {
                link = new HyperlinkButton { Style = (Style)actions[typeof(HyperlinkButton)], Content = brand, Command = context.Command };
                host.Children.Add(link);
            }
            else host.Children.Add(new ContentPresenter { Content = context, ContentTemplate = LinkTemplate });
            renderedTemplate = LinkTemplate;
        }
        var large = Size == BleeckerBrandLockupSize.Large;
        logo!.Source = LogoSource; logo.Height = large ? 48 : 32;
        separator!.Height = logo.Height;
        var named = !string.IsNullOrEmpty(BrandName);
        brand.ColumnSpacing = named ? 16 : 0; separator.Visibility = name!.Visibility = named ? Visibility.Visible : Visibility.Collapsed;
        name.Text = BrandName ?? ""; name.FontSize = large ? 30 : 20; name.LineHeight = large ? 36 : 28;
        context!.Href = Href; context.Label = BrandName ?? LogoAlt; context.CommandParameter = CommandParameter; context.NotifyCanExecuteChanged();
        if (link is not null) { link.CommandParameter = CommandParameter; AutomationProperties.SetHelpText(link, Href); }
        UpdateAccessibleName(); UpdateAppearance(); SetLogoOpacity(false);
    }
    private void UpdateAccessibleName()
    {
        if (link is null) return;
        var explicitName = AutomationProperties.GetName(this);
        AutomationProperties.SetName(link, !string.IsNullOrEmpty(explicitName) ? explicitName :
            string.Join(" ", new[] { LogoAlt, BrandName }.Where(value => !string.IsNullOrEmpty(value))));
    }
    private void UpdateAppearance()
    {
        if (logo is null || separator is null) return;
        separator.Background = separatorPalette!.Foreground;
        if (contrastPalette!.Foreground is not SolidColorBrush foreground)
            throw new InvalidOperationException("BrandLockup contrast palette requires a solid semantic foreground brush.");
        var color = foreground.Color;
        color.A = (byte)Math.Round(color.A * foreground.Opacity);
        var highContrast = accessibility.HighContrast;
        renderedTheme = ActualTheme;
        logo.SetAppearance(renderedTheme == ElementTheme.Dark, highContrast, color);
        if (renderedContrast != highContrast) { renderedContrast = highContrast; SetLogoOpacity(false); }
    }
    private void OnThemeFrame(object? sender, object args)
    {
        // Inherited theme changes can expose the new ActualTheme after the
        // notification callback has run. Reconcile its authoritative value at
        // presentation; stable frames do not rebuild or republish the effects.
        if (renderedTheme != ActualTheme) UpdateAppearance();
        if (opacityRendering && logo is not null)
        {
            var time = clock.Elapsed.TotalMilliseconds;
            logo.Opacity = opacityMotion.ValueAt(time);
            opacityRendering = opacityMotion.IsRunning(time);
        }
    }
    private void ObservePalette()
    {
        StopObservingPalette();
        if (!IsLoaded) return;
        foreach (var probe in new[] { separatorPalette, contrastPalette }.OfType<TextBlock>())
        {
            paletteObservers.Add((probe, TextBlock.ForegroundProperty, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => { ObservePalette(); UpdateAppearance(); })));
            if (probe.Foreground is SolidColorBrush brush)
                foreach (var property in new[] { SolidColorBrush.ColorProperty, Brush.OpacityProperty })
                    paletteObservers.Add((brush, property, brush.RegisterPropertyChangedCallback(property, (_, _) => UpdateAppearance())));
        }
    }
    private void StopObservingPalette()
    {
        foreach (var observer in paletteObservers) observer.Owner.UnregisterPropertyChangedCallback(observer.Property, observer.Token);
        paletteObservers.Clear();
    }
    private void SetLogoOpacity(bool animate)
    {
        if (logo is null) return;
        var target = accessibility.HighContrast || hovering && CanInvoke() ? 1 : .9;
        var time = clock.Elapsed.TotalMilliseconds;
        opacityMotion.Retarget(target, time, BleeckerTokens.Durations["surface"],
            animate && IsLoaded && settings.AnimationsEnabled && !accessibility.HighContrast);
        logo.Opacity = opacityMotion.ValueAt(time);
        opacityRendering = opacityMotion.IsRunning(time);
    }
    internal bool CanInvoke() => IsEnabled && (Command?.CanExecute(CommandParameter) ?? true);
    internal void Invoke()
    {
        if (!CanInvoke()) return;
        var href = Href; Command?.Execute(CommandParameter); Invoked?.Invoke(this, href);
    }
    private void ObserveCommand()
    {
        if (ReferenceEquals(Command, observedCommand)) return;
        StopObservingCommand();
        if (IsLoaded && Command is ICommand command) { observedCommand = command; command.CanExecuteChanged += OnCanExecuteChanged; }
        context?.NotifyCanExecuteChanged();
    }
    private void StopObservingCommand() { if (observedCommand is not null) observedCommand.CanExecuteChanged -= OnCanExecuteChanged; observedCommand = null; }
    private void OnCanExecuteChanged(object? sender, EventArgs args)
    {
        void Update() { context?.NotifyCanExecuteChanged(); SetLogoOpacity(false); }
        if (DispatcherQueue.HasThreadAccess) Update(); else DispatcherQueue.TryEnqueue(Update);
    }
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateAppearance);
    private void OnMotionChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(() => SetLogoOpacity(false));
    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}

/// <summary>Data supplied to a consumer link template, including the ready-to-render brand content.</summary>
public sealed class BleeckerBrandLinkContext : DependencyObject
{
    private readonly Activation activation;
    internal BleeckerBrandLinkContext(BleeckerBrandLockup owner) => activation = new(owner);
    public static readonly DependencyProperty HrefProperty = DependencyProperty.Register(nameof(Href), typeof(string), typeof(BleeckerBrandLinkContext), new PropertyMetadata(""));
    public string Href { get => (string)GetValue(HrefProperty); internal set => SetValue(HrefProperty, value); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerBrandLinkContext), new PropertyMetadata(""));
    public string Label { get => (string)GetValue(LabelProperty); internal set => SetValue(LabelProperty, value); }
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(object), typeof(BleeckerBrandLinkContext), new PropertyMetadata(null));
    public UIElement? Content { get => (UIElement?)GetValue(ContentProperty); internal set => SetValue(ContentProperty, value); }
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(BleeckerBrandLinkContext), new PropertyMetadata(null));
    public object? CommandParameter { get => GetValue(CommandParameterProperty); internal set => SetValue(CommandParameterProperty, value); }
    public ICommand Command => activation;
    internal void NotifyCanExecuteChanged() => activation.Notify();
    private sealed class Activation(BleeckerBrandLockup owner) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => owner.CanInvoke();
        public void Execute(object? parameter) => owner.Invoke();
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
