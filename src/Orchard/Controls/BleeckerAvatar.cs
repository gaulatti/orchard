using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

public enum BleeckerAvatarSize { ExtraSmall, Small, Medium, Large, ExtraLarge }

/// <summary>A consumer-owned image, initials or the pinned decorative person silhouette.</summary>
public sealed class BleeckerAvatar : Control
{
    private Border? surface, photo, ring;
    private Image? loader;
    private TextBlock? text, groupRingProbe;
    private Viewbox? person;
    private Grid? shadowHost;
    private Grid? content;
    private Microsoft.UI.Xaml.Shapes.Path? nearMask, farMask;
    private bool imageFailed, grouped, overflow;
    private string? overflowText;
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly TextBlock sizeDefaults = new();
    private Binding? defaultWidthBinding, defaultHeightBinding, defaultFontBinding;
    private bool updatingDefaults;

    public BleeckerAvatar()
    {
        DefaultStyleKey = typeof(BleeckerAvatar);
        SizeChanged += (_, _) => UpdateSurface();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateSurface());
        RegisterPropertyChangedCallback(BorderThicknessProperty, (_, _) => UpdateSurface());
        foreach (var property in new[] { WidthProperty, HeightProperty, FontSizeProperty, StyleProperty })
            RegisterPropertyChangedCallback(property, (_, _) => { if (!updatingDefaults) EnsureDefaultBindings(); });
        Loaded += (_, _) => { settings.ColorValuesChanged += OnColorsChanged; UpdateSurface(); };
        Unloaded += (_, _) => { settings.ColorValuesChanged -= OnColorsChanged; ClearShadow(); };
        UpdateSize();
    }

    public static readonly DependencyProperty AvatarSizeProperty = DependencyProperty.Register(nameof(AvatarSize), typeof(BleeckerAvatarSize), typeof(BleeckerAvatar),
        new PropertyMetadata(BleeckerAvatarSize.Medium, (owner, _) => ((BleeckerAvatar)owner).UpdateSize()));
    public BleeckerAvatarSize AvatarSize { get => (BleeckerAvatarSize)GetValue(AvatarSizeProperty); set => SetValue(AvatarSizeProperty, value); }
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(nameof(Source), typeof(ImageSource), typeof(BleeckerAvatar),
        new PropertyMetadata(null, OnContentChanged));
    public ImageSource? Source { get => (ImageSource?)GetValue(SourceProperty); set => SetValue(SourceProperty, value); }
    public static readonly DependencyProperty FallbackProperty = DependencyProperty.Register(nameof(Fallback), typeof(string), typeof(BleeckerAvatar),
        new PropertyMetadata(null, OnContentChanged));
    public string? Fallback { get => (string?)GetValue(FallbackProperty); set => SetValue(FallbackProperty, value); }
    public static readonly DependencyProperty AltProperty = DependencyProperty.Register(nameof(Alt), typeof(string), typeof(BleeckerAvatar),
        new PropertyMetadata(null, OnContentChanged));
    public string? Alt { get => (string?)GetValue(AltProperty); set => SetValue(AltProperty, value); }
    private static void OnContentChanged(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerAvatar)owner).UpdateContent();

    internal bool ShowsImage => !overflow && Source is not null && !imageFailed;
    internal string Initials => overflow ? overflowText! : MakeInitials(Fallback);
    internal string AccessibleText => ShowsImage ? Alt ?? Fallback ?? "" : Initials;
    internal static (double Edge, double Font, double Line, double OverflowFont, double OverflowLine) Metrics(BleeckerAvatarSize size) => size switch
    {
        BleeckerAvatarSize.ExtraSmall => (24, 10, 15, 10, 15),
        BleeckerAvatarSize.Small => (32, 12, 16, 12, 16),
        BleeckerAvatarSize.Medium => (40, 14, 20, 12, 16),
        BleeckerAvatarSize.Large => (48, 16, 24, 14, 20),
        BleeckerAvatarSize.ExtraLarge => (64, 18, 28, 16, 24),
        _ => throw new ArgumentOutOfRangeException(nameof(size))
    };
    internal void SetGroupPresentation(bool isOverflow, long count = 0)
    {
        grouped = true; overflow = isOverflow;
        overflowText = isOverflow ? "+" + count.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        UpdateSize(); UpdateContent(); UpdateSurface();
    }
    private void UpdateSize()
    {
        var metrics = Metrics(AvatarSize);
        sizeDefaults.Width = sizeDefaults.Height = metrics.Edge;
        sizeDefaults.FontSize = overflow ? metrics.OverflowFont : metrics.Font;
        EnsureDefaultBindings();
        if (text is not null) text.LineHeight = overflow ? metrics.OverflowLine : metrics.Line;
    }
    private void EnsureDefaultBindings()
    {
        if (updatingDefaults) return;
        updatingDefaults = true;
        try
        {
            bool Styled(DependencyProperty property)
            {
                static bool HasSetter(Style? style, DependencyProperty property)
                {
                    for (; style is not null; style = style.BasedOn)
                        if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
                    return false;
                }
                if (Style is { } explicitStyle) return HasSetter(explicitStyle, property);
                // WinUI leaves Style null for implicit styles. Observe the same
                // nearest resource declaration without installing a different style.
                for (DependencyObject? scope = this; scope is not null; scope = VisualTreeHelper.GetParent(scope))
                    if (scope is FrameworkElement element && element.Resources.TryGetValue(typeof(BleeckerAvatar), out var resource) && resource is Style implicitStyle)
                        return HasSetter(implicitStyle, property);
                if (Application.Current.Resources.TryGetValue(typeof(BleeckerAvatar), out var applicationStyle) && applicationStyle is Style style)
                    return HasSetter(style, property);
                return false;
            }
            void Default(DependencyProperty property, ref Binding? binding, string path)
            {
                var owned = binding is not null && ReferenceEquals(GetBindingExpression(property)?.ParentBinding, binding);
                if (Styled(property)) { if (owned) ClearValue(property); return; }
                if (ReadLocalValue(property) == DependencyProperty.UnsetValue)
                {
                    // A binding expression distinguishes our defaults from a caller
                    // assigning even the same value, and survives size updates.
                    binding = new Binding { Source = sizeDefaults, Path = new PropertyPath(path) };
                    SetBinding(property, binding);
                }
            }
            Default(WidthProperty, ref defaultWidthBinding, nameof(TextBlock.Width));
            Default(HeightProperty, ref defaultHeightBinding, nameof(TextBlock.Height));
            Default(FontSizeProperty, ref defaultFontBinding, nameof(TextBlock.FontSize));
        }
        finally { updatingDefaults = false; }
    }
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
    {
        EnsureDefaultBindings();
        return base.MeasureOverride(availableSize);
    }
    protected override void OnApplyTemplate()
    {
        ClearShadow();
        if (loader is not null) loader.ImageFailed -= OnImageFailed;
        if (groupRingProbe is not null) groupRingProbe.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, groupRingToken);
        surface = photo = ring = null; loader = null; text = groupRingProbe = null;
        person = null; content = shadowHost = null; nearMask = farMask = null;
        base.OnApplyTemplate();
        if (Template is null) return;
        surface = Part<Border>("PART_Surface"); photo = Part<Border>("PART_Photo"); ring = Part<Border>("PART_Ring");
        loader = Part<Image>("PART_ImageLoader"); text = Part<TextBlock>("PART_Initials"); person = Part<Viewbox>("PART_Person");
        groupRingProbe = Part<TextBlock>("PART_GroupRing"); shadowHost = Part<Grid>("PART_ShadowHost");
        nearMask = Part<Microsoft.UI.Xaml.Shapes.Path>("PART_NearMask"); farMask = Part<Microsoft.UI.Xaml.Shapes.Path>("PART_FarMask");
        content = Part<Grid>("PART_Content");
        loader.ImageFailed += OnImageFailed;
        groupRingToken = groupRingProbe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => UpdateSurface());
        UpdateSize(); UpdateContent(); UpdateSurface();
    }
    private long groupRingToken;
    private T Part<T>(string name) where T : DependencyObject => GetTemplateChild(name) as T
        ?? throw new InvalidOperationException($"Avatar template requires {name}.");
    private void OnImageFailed(object sender, ExceptionRoutedEventArgs _) { imageFailed = true; UpdateContent(); }
    private void UpdateContent()
    {
        if (loader is null || photo is null || text is null || person is null) return;
        // Bleecker latches imgError for the lifetime of the component. Replacing
        // Source after failure does not retry or silently reset that state.
        loader.Source = ShowsImage ? Source : null;
        photo.Background = ShowsImage ? new ImageBrush { ImageSource = Source, Stretch = Stretch.UniformToFill } : null;
        photo.Visibility = ShowsImage ? Visibility.Visible : Visibility.Collapsed;
        text.Text = Initials;
        text.Visibility = !ShowsImage && Initials.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        person.Visibility = !ShowsImage && Initials.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private static string MakeInitials(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var initials = new List<string>(2);
        for (var index = 0; index < value.Length && initials.Count < 2; index++)
            if (!IsSpace(value[index]) && (index == 0 || IsSpace(value[index - 1])))
            {
                var unit = value[index].ToString();
                // The producer indexes word[0], a UTF-16 code unit, rather than
                // a grapheme or Unicode scalar. Preserve a leading surrogate.
                initials.Add(char.IsSurrogate(value[index]) ? unit : BleeckerTextCasing.Uppercase(unit));
            }
        return string.Concat(initials);
        static bool IsSpace(char value) => value is >= '\u0009' and <= '\u000D' or '\u0020' or '\u00A0' or '\u1680'
            or >= '\u2000' and <= '\u200A' or '\u2028' or '\u2029' or '\u202F' or '\u205F' or '\u3000' or '\uFEFF';
    }
    private void OnColorsChanged(UISettings sender, object _) => DispatcherQueue.TryEnqueue(UpdateSurface);
    private void ClearShadow() { if (shadowHost is not null) ElementCompositionPreview.SetElementChildVisual(shadowHost, null); }
    private void UpdateSurface()
    {
        if (surface is null || photo is null || ring is null || groupRingProbe is null || shadowHost is null || nearMask is null || farMask is null || person is null) return;
        person.Width = ActualWidth * .6;
        person.Height = ActualHeight * .6;
        var corners = CornerRadius;
        var factor = Math.Min(1, Math.Min(Math.Min(Ratio(ActualWidth, corners.TopLeft + corners.TopRight), Ratio(ActualWidth, corners.BottomLeft + corners.BottomRight)),
            Math.Min(Ratio(ActualHeight, corners.TopLeft + corners.BottomLeft), Ratio(ActualHeight, corners.TopRight + corners.BottomRight))));
        corners = new CornerRadius(corners.TopLeft * factor, corners.TopRight * factor, corners.BottomRight * factor, corners.BottomLeft * factor);
        surface.CornerRadius = photo.CornerRadius = corners;
        if (content is not null)
        {
            var visual = ElementCompositionPreview.GetElementVisual(content);
            visual.Clip = overflow ? null : visual.Compositor.CreateRectangleClip(0, 0, (float)ActualWidth, (float)ActualHeight,
                new Vector2((float)corners.TopLeft), new Vector2((float)corners.TopRight), new Vector2((float)corners.BottomRight), new Vector2((float)corners.BottomLeft));
        }
        ring.BorderBrush = grouped ? groupRingProbe.Foreground : BorderBrush;
        ring.BorderThickness = grouped ? new Thickness(2) : BorderThickness;
        // Base avatars retain ring-inset when AvatarGroup supplies ring-2.
        // The separate overflow span has an exterior ring and shadow-sm.
        ring.Margin = new Thickness(overflow ? -2 : 0);
        ring.CornerRadius = overflow ? new CornerRadius(corners.TopLeft + 2, corners.TopRight + 2, corners.BottomRight + 2, corners.BottomLeft + 2) : corners;
        ClearShadow();
        var visible = IsLoaded && overflow && !accessibility.HighContrast && ActualWidth > 0 && ActualHeight > 0;
        shadowHost.Visibility = nearMask.Visibility = farMask.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        if (!visible) return;
        var compositor = ElementCompositionPreview.GetElementVisual(shadowHost).Compositor;
        var layers = compositor.CreateContainerVisual();
        for (var index = 0; index < 2; index++)
        {
            var mask = index == 0 ? nearMask : farMask;
            mask.Width = Math.Max(0, ActualWidth - index * 2); mask.Height = Math.Max(0, ActualHeight - index * 2);
            mask.Data = BleeckerShadowGeometry.RoundedMask(mask.Width, mask.Height, corners, index);
            Canvas.SetLeft(mask, index); Canvas.SetTop(mask, index);
            var shadow = compositor.CreateDropShadow();
            shadow.BlurRadius = index == 0 ? 3 : 2; shadow.Offset = new Vector3(0, 1, 0);
            shadow.Color = Microsoft.UI.Colors.Black; shadow.Opacity = .1f; shadow.Mask = mask.GetAlphaMask();
            var sprite = compositor.CreateSpriteVisual();
            sprite.Size = new Vector2((float)mask.Width, (float)mask.Height); sprite.Offset = new Vector3(index, index, 0); sprite.Shadow = shadow;
            layers.Children.InsertAtTop(sprite);
        }
        ElementCompositionPreview.SetElementChildVisual(shadowHost, layers);
        static double Ratio(double size, double radii) => radii == 0 ? 1 : size / radii;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new BleeckerAvatarAutomationPeer(this);
}

internal sealed class BleeckerAvatarAutomationPeer(BleeckerAvatar owner) : FrameworkElementAutomationPeer(owner)
{
    protected override string GetClassNameCore() => nameof(BleeckerAvatar);
    protected override string GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? owner.AccessibleText : AutomationProperties.GetName(owner);
    protected override AutomationControlType GetAutomationControlTypeCore() => owner.ShowsImage ? AutomationControlType.Image : AutomationControlType.Text;
    protected override bool IsControlElementCore() => owner.ShowsImage && GetNameCore().Length > 0;
    protected override bool IsContentElementCore() => GetNameCore().Length > 0;
}
