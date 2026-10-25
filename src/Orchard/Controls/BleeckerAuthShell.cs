using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A responsive access-page layout with caller-owned brand, body, footer and complementary aside slots.</summary>
public sealed partial class BleeckerAuthShell : ContentControl
{
    private Grid? root;
    private ShellFlow? flow;
    private AccessFlow? access;
    private AsideRegion? aside;
    private BleeckerContentText? brand, body, footer, asideContent;
    private XamlRoot? observedRoot;
    private BleeckerViewportMinimum? minimum;
    private readonly UISettings settings = new();
    private readonly Dictionary<BleeckerContentText, (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)> slots = [];
    public BleeckerAuthShell()
    {
        DefaultStyleKey = typeof(BleeckerAuthShell); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => { ObserveRoot(); settings.TextScaleFactorChanged += TextScaleChanged; UpdateMinimum(); Render(); };
        Unloaded += (_, _) => { settings.TextScaleFactorChanged -= TextScaleChanged; DisconnectRoot(); minimum?.Suspend(); };
        ActualThemeChanged += (_, _) => Render();
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        foreach (var p in new[] { MinHeightProperty, StyleProperty, PaddingProperty, BorderThicknessProperty, HeightProperty }) RegisterPropertyChangedCallback(p, (_, _) => { UpdateMinimum(); Render(); });
        foreach (var p in new[] { ContentTemplateProperty, ContentTemplateSelectorProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty, FontStretchProperty, CharacterSpacingProperty, ForegroundProperty, BackgroundProperty, LanguageProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(p, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerAuthShell), new PropertyMetadata(value, (owner, _) => ((BleeckerAuthShell)owner).Render()));
    public static readonly DependencyProperty LayoutProperty = Property(nameof(Layout), typeof(BleeckerAuthShellLayout), BleeckerAuthShellLayout.Centered);
    public static readonly DependencyProperty SideProperty = Property(nameof(Side), typeof(BleeckerAuthShellSide), BleeckerAuthShellSide.End);
    public static readonly DependencyProperty BrandProperty = Property(nameof(Brand), typeof(object), null);
    public static readonly DependencyProperty BrandTemplateProperty = Property(nameof(BrandTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty BrandTemplateSelectorProperty = Property(nameof(BrandTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty FooterProperty = Property(nameof(Footer), typeof(object), null);
    public static readonly DependencyProperty FooterTemplateProperty = Property(nameof(FooterTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty FooterTemplateSelectorProperty = Property(nameof(FooterTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty AsideProperty = Property(nameof(Aside), typeof(object), null);
    public static readonly DependencyProperty AsideTemplateProperty = Property(nameof(AsideTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty AsideTemplateSelectorProperty = Property(nameof(AsideTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty AsideLabelProperty = Property(nameof(AsideLabel), typeof(string), null);
    public static readonly DependencyProperty AccessBackgroundProperty = Property(nameof(AccessBackground), typeof(Brush), null);
    public static readonly DependencyProperty AsideBackgroundProperty = Property(nameof(AsideBackground), typeof(Brush), null);
    public static readonly DependencyProperty AsideForegroundProperty = Property(nameof(AsideForeground), typeof(Brush), null);
    public static readonly DependencyProperty FooterForegroundProperty = Property(nameof(FooterForeground), typeof(Brush), null);
    public BleeckerAuthShellLayout Layout { get => (BleeckerAuthShellLayout)GetValue(LayoutProperty); set { Validate(value); SetValue(LayoutProperty, value); } }
    public BleeckerAuthShellSide Side { get => (BleeckerAuthShellSide)GetValue(SideProperty); set { Validate(value); SetValue(SideProperty, value); } }
    public object? Brand { get => GetValue(BrandProperty); set => SetValue(BrandProperty, value); }
    public DataTemplate? BrandTemplate { get => (DataTemplate?)GetValue(BrandTemplateProperty); set => SetValue(BrandTemplateProperty, value); }
    public DataTemplateSelector? BrandTemplateSelector { get => (DataTemplateSelector?)GetValue(BrandTemplateSelectorProperty); set => SetValue(BrandTemplateSelectorProperty, value); }
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
    public DataTemplate? FooterTemplate { get => (DataTemplate?)GetValue(FooterTemplateProperty); set => SetValue(FooterTemplateProperty, value); }
    public DataTemplateSelector? FooterTemplateSelector { get => (DataTemplateSelector?)GetValue(FooterTemplateSelectorProperty); set => SetValue(FooterTemplateSelectorProperty, value); }
    public object? Aside { get => GetValue(AsideProperty); set => SetValue(AsideProperty, value); }
    public DataTemplate? AsideTemplate { get => (DataTemplate?)GetValue(AsideTemplateProperty); set => SetValue(AsideTemplateProperty, value); }
    public DataTemplateSelector? AsideTemplateSelector { get => (DataTemplateSelector?)GetValue(AsideTemplateSelectorProperty); set => SetValue(AsideTemplateSelectorProperty, value); }
    public string? AsideLabel { get => (string?)GetValue(AsideLabelProperty); set => SetValue(AsideLabelProperty, value); }
    public Brush? AccessBackground { get => (Brush?)GetValue(AccessBackgroundProperty); set => SetValue(AccessBackgroundProperty, value); }
    public Brush? AsideBackground { get => (Brush?)GetValue(AsideBackgroundProperty); set => SetValue(AsideBackgroundProperty, value); }
    public Brush? AsideForeground { get => (Brush?)GetValue(AsideForegroundProperty); set => SetValue(AsideForegroundProperty, value); }
    public Brush? FooterForeground { get => (Brush?)GetValue(FooterForegroundProperty); set => SetValue(FooterForegroundProperty, value); }
    private static void Validate<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private double Rem => FontSize / 16 * (IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1);
    private double Viewport => XamlRoot?.Size.Width ?? 0;
    private double ViewportHeight => XamlRoot?.Size.Height ?? 0;
    private bool HasBrand => BrandTemplate is not null || BrandTemplateSelector is not null || BleeckerReactContent.Present(Brand);
    private bool HasFooter => FooterTemplate is not null || FooterTemplateSelector is not null || BleeckerReactContent.Present(Footer);
    private bool HasAside => Layout == BleeckerAuthShellLayout.Split && (AsideTemplate is not null || AsideTemplateSelector is not null || BleeckerReactContent.Present(Aside));
    private BleeckerAuthShellModel.Spacing Spacing => BleeckerAuthShellModel.Insets(Viewport, Rem);
    private void ObserveRoot() { if (ReferenceEquals(observedRoot, XamlRoot)) return; DisconnectRoot(); observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged; }
    private void DisconnectRoot() { if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { UpdateMinimum(); Render(); }
    private void TextScaleChanged(UISettings sender, object args) => DispatcherQueue.TryEnqueue(Render);
    private void UpdateMinimum() => minimum?.Update();
    protected override void OnContentChanged(object oldContent, object newContent) { if (body is not null) { body.Release(); slots.Remove(body); } base.OnContentChanged(oldContent, newContent); Render(); }
    protected override void OnApplyTemplate()
    {
        Detach(); if (Template is null) return; base.OnApplyTemplate(); root = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("AuthShell requires PART_Layout.");
        brand = Slot("AuthShellBrandBody"); body = Slot("AuthShellBody"); footer = new("AuthShellFooterBody", "BleeckerSecondaryFontFamily", 12, 20, "BleeckerTextSecondaryBrush", stretchContent: true);
        access = new(this, brand, body, footer) { Name = "AuthShellAccess", UseLayoutRounding = false };
        flow = new(this, access) { Name = "AuthShellFlow", UseLayoutRounding = false }; root.Children.Add(flow); minimum = new(this); UpdateMinimum(); Render();
    }
    private static BleeckerContentText Slot(string name) => new(name, "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
    private void Typography(BleeckerContentText slot, bool secondary = false, Brush? foreground = null)
    {
        slot.Text.FontFamily = secondary ? (FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"] : FontFamily; slot.Text.FontSize = FontSize * (secondary ? .75 : 1); slot.Text.FontWeight = FontWeight; slot.Text.FontStyle = FontStyle; slot.Text.FontStretch = FontStretch; slot.Text.CharacterSpacing = CharacterSpacing; slot.Text.LineHeight = FontSize * (secondary ? 1.25 : 1.5); slot.Text.Language = Language; slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; slot.SetForeground(foreground ?? Foreground);
    }
    private void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector)
    {
        if (slots.TryGetValue(slot, out var old) && (value is string or bool || BleeckerReactContent.Numeric(value) ? Equals(old.Value, value) : ReferenceEquals(old.Value, value)) && ReferenceEquals(old.Template, template) && ReferenceEquals(old.Selector, selector)) return;
        slot.Set(value, template, selector: selector); var scalar = template is null && selector is null ? BleeckerReactContent.Display(value) as string : null;
        AutomationProperties.SetAccessibilityView(slot.Text, scalar is not null && BleeckerInlineText.Normalize(scalar).Length > 0 ? AccessibilityView.Content : AccessibilityView.Raw); slots[slot] = (value, template, selector);
    }
    private void Render()
    {
        Validate(Layout); Validate(Side); if (flow is null || access is null || brand is null || body is null || footer is null) return;
        Typography(brand); Typography(body); Typography(footer, secondary: true, foreground: FooterForeground);
        SetSlot(brand, HasBrand ? Brand : null, HasBrand ? BrandTemplate : null, HasBrand ? BrandTemplateSelector : null); SetSlot(body, Content, ContentTemplate, ContentTemplateSelector); SetSlot(footer, HasFooter ? Footer : null, HasFooter ? FooterTemplate : null, HasFooter ? FooterTemplateSelector : null);
        if (HasAside)
        {
            if (aside is null) { asideContent = Slot("AuthShellAsideBody"); aside = new(this, asideContent) { Name = "AuthShellAside", UseLayoutRounding = false }; flow.Children.Add(aside); }
            Typography(asideContent!, foreground: AsideForeground); SetSlot(asideContent!, Aside, AsideTemplate, AsideTemplateSelector); aside.Visibility = Viewport >= 1024 ? Visibility.Visible : Visibility.Collapsed; AutomationProperties.SetName(aside, AsideLabel ?? ""); aside.Update();
        }
        else if (aside is not null) { asideContent!.Release(); slots.Remove(asideContent); flow.Children.Remove(aside); aside.Release(); aside = null; asideContent = null; }
        flow.Background = HasAside && !SuppliedBackground() ? AccessBackground : Background;
        access.Update(); flow.InvalidateMeasure(); InvalidateMeasure();
    }
    private bool SuppliedBackground()
    {
        if (ReadLocalValue(BackgroundProperty) != DependencyProperty.UnsetValue) return true;
        for (var style = Style; style is not null; style = style.BasedOn) if (style.Setters.OfType<Setter>().Any(s => s.Property == BackgroundProperty)) return true;
        return false;
    }
    private void Detach()
    {
        minimum?.Release(); minimum = null; foreach (var slot in slots.Keys) slot.Release(); slots.Clear(); aside?.Release(); access?.Release(); flow?.Children.Clear(); root?.Children.Clear(); root = null; flow = null; access = null; aside = null; brand = body = footer = asideContent = null;
    }
    protected override Size MeasureOverride(Size available) { if (IsLoaded) ObserveRoot(); UpdateMinimum(); var measured = base.MeasureOverride(available); return new(double.IsFinite(available.Width) ? available.Width : measured.Width, measured.Height); }
    protected override AutomationPeer OnCreateAutomationPeer() => new ShellPeer(this);
    private sealed class ShellPeer(BleeckerAuthShell owner) : FrameworkElementAutomationPeer(owner)
    { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; protected override AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.Main; protected override string GetClassNameCore() => nameof(BleeckerAuthShell); }
    private static void Bind(DependencyObject target, DependencyProperty property, object source, string name) => ((FrameworkElement)target).SetBinding(property, new Binding { Source = source, Path = new PropertyPath(name), Mode = BindingMode.OneWay });
    private sealed partial class SlotFrame : ContentControl
    {
        private readonly bool footer;
        internal SlotFrame(BleeckerAuthShell owner, string name, BleeckerContentText slot, bool footer = false)
        {
            this.footer = footer; foreach (var property in new[] { Control.FontFamilyProperty, Control.FontSizeProperty, Control.FontWeightProperty, Control.FontStyleProperty, Control.FontStretchProperty, Control.CharacterSpacingProperty, Control.IsTextScaleFactorEnabledProperty, FrameworkElement.LanguageProperty }) Bind(this, property, owner, property == FrameworkElement.LanguageProperty ? nameof(Language) : property == Control.FontFamilyProperty ? nameof(FontFamily) : property == Control.FontSizeProperty ? nameof(FontSize) : property == Control.FontWeightProperty ? nameof(FontWeight) : property == Control.FontStyleProperty ? nameof(FontStyle) : property == Control.FontStretchProperty ? nameof(FontStretch) : property == Control.CharacterSpacingProperty ? nameof(CharacterSpacing) : nameof(IsTextScaleFactorEnabled)); Name = name; Content = slot; IsTabStop = false; UseLayoutRounding = false; HorizontalContentAlignment = HorizontalAlignment.Stretch; VerticalContentAlignment = VerticalAlignment.Stretch;
            Template = (ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='ContentControl'><ContentPresenter Content='{TemplateBinding Content}' FontFamily='{TemplateBinding FontFamily}' FontSize='{TemplateBinding FontSize}' FontWeight='{TemplateBinding FontWeight}' FontStyle='{TemplateBinding FontStyle}' FontStretch='{TemplateBinding FontStretch}' CharacterSpacing='{TemplateBinding CharacterSpacing}' Foreground='{TemplateBinding Foreground}' IsTextScaleFactorEnabled='{TemplateBinding IsTextScaleFactorEnabled}' Language='{TemplateBinding Language}' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' UseLayoutRounding='False'/></ControlTemplate>");
            if (!footer) AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
        }
        protected override AutomationPeer OnCreateAutomationPeer() => new FramePeer(this, footer);
        private sealed class FramePeer(SlotFrame owner, bool footer) : FrameworkElementAutomationPeer(owner)
        { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; protected override AutomationLandmarkType GetLandmarkTypeCore() => footer ? AutomationLandmarkType.Custom : AutomationLandmarkType.None; protected override string GetLocalizedLandmarkTypeCore() => footer ? "contentinfo" : ""; }
    }
    private sealed partial class AccessFlow : Panel
    {
        private readonly BleeckerAuthShell owner;
        private readonly AccessInner inner;
        internal AccessFlow(BleeckerAuthShell owner, BleeckerContentText brand, BleeckerContentText body, BleeckerContentText footer)
        {
            this.owner = owner; inner = new(owner, brand, body, footer) { Name = "AuthShellInner", UseLayoutRounding = false }; Bind(this, BackgroundProperty, owner, nameof(AccessBackground)); Children.Add(inner);
        }
        internal void Update() { inner.Update(); InvalidateMeasure(); }
        internal void Release() { inner.Release(); Children.Clear(); }
        private double InnerWidth(double width) => Math.Min(owner.Spacing.MaximumContent, Math.Max(0, width - owner.Spacing.Horizontal * 2));
        protected override Size MeasureOverride(Size available)
        {
            var width = double.IsFinite(available.Width) ? available.Width : owner.Viewport; inner.Measure(new Size(InnerWidth(width), double.PositiveInfinity));
            return new(width, Math.Max(owner.ViewportHeight, inner.DesiredSize.Height + owner.Spacing.Vertical * 2));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var width = InnerWidth(final.Width); inner.Arrange(new Rect((final.Width - width) / 2, owner.Spacing.Vertical, width, Math.Max(0, final.Height - owner.Spacing.Vertical * 2))); return final;
        }
    }
    private sealed partial class AccessInner : Panel
    {
        private readonly BleeckerAuthShell owner;
        private readonly SlotFrame brand, footer;
        private readonly ContentRegion content;
        private double brandHeight, footerHeight;
        internal AccessInner(BleeckerAuthShell owner, BleeckerContentText brand, BleeckerContentText body, BleeckerContentText footer)
        {
            this.owner = owner; this.brand = new(owner, "AuthShellBrand", brand); content = new(owner, body) { Name = "AuthShellContent", UseLayoutRounding = false }; this.footer = new(owner, "AuthShellFooter", footer, true);
            Bind(this.footer, Control.ForegroundProperty, owner, nameof(FooterForeground)); this.footer.FontFamily = (FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"]; Children.Add(this.brand); Children.Add(content); Children.Add(this.footer);
        }
        internal void Update() { brand.Visibility = owner.HasBrand ? Visibility.Visible : Visibility.Collapsed; footer.Visibility = owner.HasFooter ? Visibility.Visible : Visibility.Collapsed; footer.FontSize = owner.FontSize * .75; InvalidateMeasure(); }
        internal void Release() { brand.Content = footer.Content = null; content.Release(); Children.Clear(); }
        protected override Size MeasureOverride(Size available)
        {
            var size = new Size(available.Width, double.PositiveInfinity); brand.Measure(size); content.Measure(size); footer.Measure(size); brandHeight = owner.HasBrand ? brand.DesiredSize.Height : 0; footerHeight = owner.HasFooter ? footer.DesiredSize.Height : 0;
            return new(available.Width, brandHeight + content.DesiredSize.Height + footerHeight);
        }
        protected override Size ArrangeOverride(Size final)
        {
            brand.Arrange(new Rect(0, 0, final.Width, brandHeight)); content.Arrange(new Rect(0, brandHeight, final.Width, Math.Max(content.DesiredSize.Height, final.Height - brandHeight - footerHeight))); footer.Arrange(new Rect(0, final.Height - footerHeight, final.Width, footerHeight)); return final;
        }
    }
    private sealed partial class ContentRegion(BleeckerAuthShell owner, BleeckerContentText body) : Panel
    {
        private SlotFrame? content;
        private double height;
        internal void Release() { if (content is not null) content.Content = null; Children.Clear(); }
        protected override Size MeasureOverride(Size available)
        {
            if (content is null) { content = new(owner, "AuthShellContentInner", body); Children.Add(content); }
            content.Measure(new Size(available.Width, double.PositiveInfinity)); height = content.DesiredSize.Height; return new(available.Width, height + owner.Spacing.ContentVertical * 2);
        }
        protected override Size ArrangeOverride(Size final) { content!.Arrange(new Rect(0, (final.Height - height) / 2, final.Width, height)); return final; }
    }
    private sealed partial class AsideRegion : Panel
    {
        private readonly BleeckerAuthShell owner;
        private readonly SlotFrame content;
        private readonly Canvas decoration;
        private readonly Microsoft.UI.Xaml.Shapes.Ellipse large, small;
        private readonly RectangleGeometry clip = new();
        private double bodyHeight;
        internal AsideRegion(BleeckerAuthShell owner, BleeckerContentText slot)
        {
            this.owner = owner; Clip = clip; Bind(this, BackgroundProperty, owner, nameof(AsideBackground));
            decoration = new Canvas { Name = "AuthShellCircles", IsHitTestVisible = false, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false }; AutomationProperties.SetAccessibilityView(decoration, AccessibilityView.Raw);
            Microsoft.UI.Xaml.Shapes.Ellipse Circle(string name) { var e = (Microsoft.UI.Xaml.Shapes.Ellipse)XamlReader.Load("<Ellipse xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Stroke='{ThemeResource BleeckerAuthShellCircleBrush}' StrokeThickness='1' UseLayoutRounding='False' IsHitTestVisible='False'/>"); e.Name = name; AutomationProperties.SetAccessibilityView(e, AccessibilityView.Raw); return e; }
            large = Circle("AuthShellLargeCircle"); small = Circle("AuthShellSmallCircle"); decoration.Children.Add(large); decoration.Children.Add(small); content = new(owner, "AuthShellAsideContent", slot); Bind(content, Control.ForegroundProperty, owner, nameof(AsideForeground)); Children.Add(decoration); Children.Add(content);
        }
        internal void Update() { content.ApplyTemplate(); if (Visibility == Visibility.Collapsed) content.Measure(new Size(Math.Max(0, 384 * owner.Rem - owner.Spacing.Aside * 2), double.PositiveInfinity)); InvalidateMeasure(); }
        internal void Release() { content.Content = null; Children.Clear(); decoration.Children.Clear(); }
        protected override Size MeasureOverride(Size available)
        {
            var padding = owner.Spacing.Aside; var width = double.IsFinite(available.Width) ? available.Width : 384 * owner.Rem; content.Measure(new Size(Math.Max(0, width - padding * 2), double.PositiveInfinity)); bodyHeight = content.DesiredSize.Height;
            large.Width = large.Height = 320 * owner.Rem; small.Width = small.Height = 208 * owner.Rem; decoration.Measure(new Size(width, owner.ViewportHeight));
            return new(width, Math.Max(owner.ViewportHeight, bodyHeight + padding * 2));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var p = owner.Spacing.Aside; clip.Rect = new Rect(0, 0, final.Width, final.Height); Canvas.SetLeft(large, final.Width - 224 * owner.Rem); Canvas.SetTop(large, -96 * owner.Rem); Canvas.SetLeft(small, final.Width - 168 * owner.Rem); Canvas.SetTop(small, -40 * owner.Rem); decoration.Arrange(new Rect(0, 0, final.Width, final.Height));
            content.Arrange(new Rect(p, final.Height - p - bodyHeight, Math.Max(0, final.Width - p * 2), bodyHeight)); return final;
        }
        protected override AutomationPeer OnCreateAutomationPeer() => new AsidePeer(this);
        private sealed class AsidePeer(AsideRegion owner) : FrameworkElementAutomationPeer(owner)
        { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; protected override AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.Custom; protected override string GetLocalizedLandmarkTypeCore() => "complementary"; protected override string GetClassNameCore() => "BleeckerAuthShellAside"; }
    }
    private sealed partial class ShellFlow(BleeckerAuthShell owner, AccessFlow access) : Panel
    {
        private BleeckerAuthShellModel.Tracks tracks;
        private bool initialized;
        protected override Size MeasureOverride(Size available)
        {
            if (!initialized) { Children.Insert(0, access); initialized = true; }
            var width = double.IsFinite(available.Width) ? available.Width : owner.Viewport; tracks = BleeckerAuthShellModel.Columns(width, owner.Viewport, owner.Rem, owner.HasAside, owner.Side);
            access.Measure(new Size(tracks.AccessWidth, double.PositiveInfinity)); if (owner.aside is not null) owner.aside.Measure(new Size(tracks.AsideWidth, double.PositiveInfinity));
            var outer = owner.Padding.Top + owner.Padding.Bottom + owner.BorderThickness.Top + owner.BorderThickness.Bottom;
            return new(width, Math.Max(Math.Max(0, owner.MinHeight - outer), Math.Max(access.DesiredSize.Height, tracks.AsideVisible ? owner.aside!.DesiredSize.Height : 0)));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var natural = Math.Max(access.DesiredSize.Height, tracks.AsideVisible ? owner.aside!.DesiredSize.Height : 0);
            var height = BleeckerAuthShellModel.RegionHeight(natural, final.Height, owner.HasAside);
            access.Arrange(new Rect(tracks.AccessX, 0, tracks.AccessWidth, height)); if (tracks.AsideVisible) owner.aside!.Arrange(new Rect(tracks.AsideX, 0, tracks.AsideWidth, height)); return final;
        }
    }
}
