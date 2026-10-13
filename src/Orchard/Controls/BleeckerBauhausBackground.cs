using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerBauhausType { Nazca, Autostrada, Pompeii, Monitor }

/// <summary>Decorative image, multiply shade and soft-light geometric pattern.</summary>
public sealed class BleeckerBauhausBackground : Control
{
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private Grid? root, imageHost, shadeHost, patternHost, shadeSurface, patternSurface;
    private Image? image;
    private Canvas? pattern;
    private Border? output;
    private TextBlock? bronze, oxblood, contrastBackground;
    private readonly List<CompositionVisualSurface> surfaces = [];
    private readonly List<CompositionSurfaceBrush> sources = [];
    private CompositionBackdropBrush? backdrop;
    private CompositionEffectFactory? factory;
    private CompositionEffectBrush? effect;
    private SpriteVisual? sprite;
    private Storyboard? rotation;
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observations = [];
    private XamlRoot? observedRoot;
    private bool? renderedContrast;
    private ElementTheme? renderedTheme;

    public BleeckerBauhausBackground()
    {
        DefaultStyleKey = typeof(BleeckerBauhausBackground);
        DefaultImageSource = new BitmapImage(new Uri("ms-appx:///hero/pompeii.avif"));
        Loaded += (_, _) =>
        {
            if (!Enum.IsDefined(Type)) throw new InvalidOperationException("BauhausBackground Type must resolve before mounting.");
            observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += OnRootChanged;
            settings.ColorValuesChanged += OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged += OnAnimationsChanged;
            CompositionTarget.Rendering += OnFrame;
            UpdateSource(); UpdateLayoutState(); UpdateAppearance(); StartRotation();
        };
        Unloaded += (_, _) =>
        {
            if (observedRoot is not null) observedRoot.Changed -= OnRootChanged; observedRoot = null;
            settings.ColorValuesChanged -= OnColorsChanged;
            if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)) settings.AnimationsEnabledChanged -= OnAnimationsChanged;
            CompositionTarget.Rendering -= OnFrame; StopRotation(); Disconnect();
        };
        SizeChanged += (_, _) => UpdateLayoutState();
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => { if (IsLoaded) UpdateAppearance(); });
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachTemplate(); });
    }

    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(nameof(Type), typeof(BleeckerBauhausType), typeof(BleeckerBauhausBackground),
        new PropertyMetadata((BleeckerBauhausType)(-1), (owner, _) => ((BleeckerBauhausBackground)owner).UpdatePattern()));
    public BleeckerBauhausType Type { get => (BleeckerBauhausType)GetValue(TypeProperty); set { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(TypeProperty, value); } }
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(BleeckerBauhausBackground), new PropertyMetadata(null, OnSourceChanged));
    public ImageSource? ImageSource { get => (ImageSource?)GetValue(ImageSourceProperty); set => SetValue(ImageSourceProperty, value); }
    public static readonly DependencyProperty DefaultImageSourceProperty = DependencyProperty.Register(nameof(DefaultImageSource), typeof(ImageSource), typeof(BleeckerBauhausBackground), new PropertyMetadata(null, OnSourceChanged));
    public ImageSource? DefaultImageSource { get => (ImageSource?)GetValue(DefaultImageSourceProperty); set => SetValue(DefaultImageSourceProperty, value); }
    public event ExceptionRoutedEventHandler? ImageFailed;
    private static void OnSourceChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerBauhausBackground)owner).UpdateSource();

    protected override void OnApplyTemplate()
    {
        DetachTemplate(); base.OnApplyTemplate(); if (Template is null) return;
        root = Part<Grid>("PART_Root"); imageHost = Part<Grid>("PART_ImageHost"); shadeHost = Part<Grid>("PART_ShadeHost"); patternHost = Part<Grid>("PART_PatternHost");
        shadeSurface = Part<Grid>("PART_ShadeSurface"); patternSurface = Part<Grid>("PART_PatternSurface");
        image = Part<Image>("PART_Image"); pattern = Part<Canvas>("PART_Pattern"); output = Part<Border>("PART_Output");
        bronze = Part<TextBlock>("PART_Bronze"); oxblood = Part<TextBlock>("PART_Oxblood"); contrastBackground = Part<TextBlock>("PART_ContrastBackground");
        foreach (var probe in new[] { bronze, oxblood, contrastBackground })
            observations.Add((probe, TextBlock.ForegroundProperty, probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => UpdateAppearance())));
        foreach (var host in new[] { imageHost, shadeHost, patternHost }) ElementCompositionPreview.SetIsTranslationEnabled(host, true);
        image.ImageFailed += OnImageFailed; image.ImageOpened += OnImageOpened;
        UpdateSource(); UpdatePattern(); UpdateLayoutState(); UpdateAppearance();
    }
    private T Part<T>(string name) where T : FrameworkElement => GetTemplateChild(name) as T ?? throw new InvalidOperationException($"BauhausBackground requires {name}.");
    private void OnImageFailed(object sender, ExceptionRoutedEventArgs args) => ImageFailed?.Invoke(this, args);
    private void OnImageOpened(object sender, RoutedEventArgs args) => UpdateLayoutState();
    private void UpdateSource() { if (image is not null) image.Source = ImageSource ?? DefaultImageSource; }
    private void UpdatePattern()
    {
        if (pattern is null || !Enum.IsDefined(Type)) return;
        StopRotation(); pattern.Children.Clear();
        if (Type is BleeckerBauhausType.Nazca or BleeckerBauhausType.Monitor)
        {
            for (var index = 0; index < 2; index++) pattern.Children.Add(new Border
            {
                Name = index == 0 ? "PART_OuterCircle" : "PART_InnerCircle",
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White),
                BorderThickness = new Thickness(index == 0 ? 8 : 4),
                RenderTransform = new RotateTransform(),
                RenderTransformOrigin = new Point(.5, .5)
            });
        }
        else for (var index = 0; index < (Type == BleeckerBauhausType.Autostrada ? 6 : 9); index++)
        {
            var paint = new Canvas();
            paint.Children.Add(new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.White) { Opacity = .85 },
                BorderThickness = Type == BleeckerBauhausType.Autostrada ? new Thickness(2, 0, 0, 0) : new Thickness(2)
            });
            pattern.Children.Add(new Border { Name = $"PART_Shape{index}", Child = paint });
        }
        UpdateLayoutState(); UpdateAppearance(); StartRotation();
    }
    private void UpdateLayoutState()
    {
        if (root is null || pattern is null || ActualWidth <= 0 || ActualHeight <= 0) return;
        root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
        var width = ActualWidth; var height = ActualHeight;
        for (var index = 0; index < pattern.Children.Count; index++)
        {
            var shape = (Border)pattern.Children[index]; double x, y, w, h;
            if (Type is BleeckerBauhausType.Nazca or BleeckerBauhausType.Monitor)
            {
                var factor = index == 0 ? .5 : 1d / 3; var minimum = index == 0 ? 16 : 8;
                w = Math.Max(minimum, width * factor); h = Math.Max(minimum, height * factor); x = index == 0 ? width / 4 : width / 3; y = index == 0 ? height / 4 : height / 3;
                // WinUI's Border radius describes the stroke centerline. Its
                // outer radius adds half the border; CSS rounds the outer box.
                shape.CornerRadius = new CornerRadius(Math.Max(0, Math.Min(w, h) / 2 - shape.BorderThickness.Left / 2));
            }
            else if (Type == BleeckerBauhausType.Autostrada) { w = Math.Max(2, width / 6); h = height; x = index * width / 6; y = 0; }
            else
            {
                var column = Math.Max(0, (width - 64) / 3); var row = Math.Max(0, (height - 64) / 3);
                // CSS border-box dimensions cannot shrink below the border.
                // Grid positions still use the smaller minmax(0,1fr) tracks.
                w = Math.Max(4, column); h = Math.Max(4, row); x = 16 + (index % 3) * (column + 16); y = 16 + (index / 3) * (row + 16);
            }
            shape.Width = w; shape.Height = h; Canvas.SetLeft(shape, x); Canvas.SetTop(shape, y);
            if(shape.Child is Canvas paint && XamlRoot is XamlRoot xamlRoot)
            {
                // CSS paints rectangular border edges at device-pixel bounds
                // while retaining fractional grid boxes for layout.
                var scale=xamlRoot.RasterizationScale;
                double Snap(double edge)=>Math.Round(edge*scale,MidpointRounding.AwayFromZero)/scale;
                var border=(Border)paint.Children[0];border.Width=Snap(x+w)-Snap(x);border.Height=Snap(y+h)-Snap(y);
                Canvas.SetLeft(border,Snap(x)-x);Canvas.SetTop(border,Snap(y)-y);
            }
        }
        UpdateEffect();
    }
    private void UpdateAppearance()
    {
        if (patternSurface is null || output is null || bronze is null || oxblood is null || contrastBackground is null) return;
        renderedTheme = ActualTheme; var highContrast = accessibility.HighContrast;
        patternSurface.Background = Type is BleeckerBauhausType.Autostrada or BleeckerBauhausType.Monitor ? oxblood.Foreground : bronze.Foreground;
        output.Background = highContrast ? contrastBackground.Foreground : null;
        if (sprite is not null) sprite.IsVisible = !highContrast;
        if (renderedContrast != highContrast) { renderedContrast = highContrast; StartRotation(); }
    }
    private void UpdateEffect()
    {
        if (!IsLoaded || XamlRoot is null || root is null || output is null || image is null || imageHost is null || shadeHost is null || patternHost is null || shadeSurface is null || patternSurface is null) return;
        if (factory is null)
        {
            var compositor = ElementCompositionPreview.GetElementVisual(output).Compositor;
            foreach (var visualRoot in new FrameworkElement[] { image, shadeSurface, patternSurface })
            {
                var surface = compositor.CreateVisualSurface(); surface.SourceVisual = ElementCompositionPreview.GetElementVisual(visualRoot); surfaces.Add(surface);
                sources.Add(compositor.CreateSurfaceBrush(surface));
            }
            backdrop = compositor.CreateBackdropBrush();
            using var imageOverBackdrop = new CompositeEffect { Mode = CanvasComposite.SourceOver };
            imageOverBackdrop.Sources.Add(new CompositionEffectSourceParameter("backdrop")); imageOverBackdrop.Sources.Add(new CompositionEffectSourceParameter("image"));
            using var shaded = new BlendEffect { Mode = BlendEffectMode.Multiply, Background = imageOverBackdrop, Foreground = new CompositionEffectSourceParameter("shade") };
            // Composition's soft-light operand order is established by the
            // browser/native RGBA regression, including opaque black. Swapping
            // these inputs leaves a colored pattern over black instead of black.
            using var blended = new BlendEffect { Mode = BlendEffectMode.SoftLight, Background = new CompositionEffectSourceParameter("pattern"), Foreground = shaded };
            factory = compositor.CreateEffectFactory(blended); effect = factory.CreateBrush();
            effect.SetSourceParameter("backdrop", backdrop); effect.SetSourceParameter("image", sources[0]); effect.SetSourceParameter("shade", sources[1]); effect.SetSourceParameter("pattern", sources[2]);
            sprite = compositor.CreateSpriteVisual(); sprite.Brush = effect; ElementCompositionPreview.SetElementChildVisual(output, sprite);
        }
        var size = new Vector2((float)ActualWidth, (float)ActualHeight); foreach (var surface in surfaces) surface.SourceSize = size; sprite!.Size = size;
        var translation = new Vector3(-(float)(XamlRoot.Size.Width + ActualWidth + 1), 0, 0);
        // Capture the shade/pattern roots inside their private hosts so translating
        // the host does not translate the effect's source coordinates.
        ElementCompositionPreview.GetElementVisual(imageHost).Properties.InsertVector3("Translation", translation);
        ElementCompositionPreview.GetElementVisual(shadeHost).Properties.InsertVector3("Translation", translation);
        ElementCompositionPreview.GetElementVisual(patternHost).Properties.InsertVector3("Translation", translation);
        UpdateAppearance();
    }
    private void StartRotation()
    {
        StopRotation();
        if (!IsLoaded || pattern is null || !settings.AnimationsEnabled || accessibility.HighContrast || Type is not (BleeckerBauhausType.Nazca or BleeckerBauhausType.Monitor)) return;
        rotation = new Storyboard();
        for (var index = 0; index < pattern.Children.Count; index++)
        {
            var transform = (RotateTransform)((Border)pattern.Children[index]).RenderTransform;
            var animation = new DoubleAnimation { From = 0, To = index == 0 ? 360 : -360, Duration = TimeSpan.FromSeconds(20), RepeatBehavior = RepeatBehavior.Forever, EnableDependentAnimation = true };
            Storyboard.SetTarget(animation, transform); Storyboard.SetTargetProperty(animation, "Angle"); rotation.Children.Add(animation);
        }
        rotation.Begin();
    }
    private void StopRotation() { rotation?.Stop(); rotation = null; if (pattern is not null) foreach (var shape in pattern.Children.OfType<Border>()) if (shape.RenderTransform is RotateTransform transform) transform.Angle = 0; }
    // AccessibilitySettings.HighContrastChanged cannot subscribe in this
    // unpackaged runtime (HRESULT 0x80070490). Observe the public state while
    // loaded, as well as palette/theme notifications, without masking errors.
    private void OnFrame(object? sender, object args) { if (renderedTheme != ActualTheme || renderedContrast != accessibility.HighContrast) UpdateAppearance(); }
    private void OnColorsChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(UpdateAppearance);
    private void OnAnimationsChanged(UISettings sender, UISettingsAnimationsEnabledChangedEventArgs args) => DispatcherQueue.TryEnqueue(StartRotation);
    private void OnRootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdateLayoutState();
    private void Disconnect()
    {
        if (output is not null) ElementCompositionPreview.SetElementChildVisual(output, null);
        foreach (var surface in surfaces) surface.SourceVisual = null;
        sprite?.Dispose(); effect?.Dispose(); factory?.Dispose(); backdrop?.Dispose(); foreach (var source in sources) source.Dispose(); foreach (var surface in surfaces) surface.Dispose();
        sprite = null; effect = null; factory = null; backdrop = null; sources.Clear(); surfaces.Clear();
    }
    private void DetachTemplate()
    {
        StopRotation(); Disconnect(); if (image is not null) { image.ImageFailed -= OnImageFailed; image.ImageOpened -= OnImageOpened; }
        foreach (var observation in observations) observation.Owner.UnregisterPropertyChangedCallback(observation.Property, observation.Token); observations.Clear();
        root = imageHost = shadeHost = patternHost = shadeSurface = patternSurface = null; image = null; pattern = null; output = null; bronze = oxblood = contrastBackground = null;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new DecorativePeer(this);
    private sealed class DecorativePeer(FrameworkElement owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override bool IsControlElementCore() => false; protected override bool IsContentElementCore() => false;
        protected override List<AutomationPeer>? GetChildrenCore() => null;
    }
}
