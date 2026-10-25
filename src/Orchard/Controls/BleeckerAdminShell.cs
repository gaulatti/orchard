using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A retained header/sidebar composition with responsive content padding and a native scrolling main region.</summary>
public sealed partial class BleeckerAdminShell : ContentControl
{
    private Grid? root;
    private ShellFlow? flow;
    private MainRegion? main;
    private BleeckerContentText? header, sidebar, body, footer;
    private readonly Dictionary<BleeckerContentText, (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)> slots = [];
    private XamlRoot? observedRoot;
    private BleeckerViewportMinimum? minimum;
    public BleeckerAdminShell()
    {
        DefaultStyleKey = typeof(BleeckerAdminShell); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => { ObserveRoot(); UpdateMinimum(); Render(); };
        Unloaded += (_, _) => { DisconnectRoot(); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
        RegisterPropertyChangedCallback(MinHeightProperty, (_, _) => { UpdateMinimum(); flow?.InvalidateMeasure(); });
        foreach (var property in new[] { PaddingProperty, BorderThicknessProperty, HeightProperty }) RegisterPropertyChangedCallback(property, (_, _) => flow?.InvalidateMeasure());
        RegisterPropertyChangedCallback(StyleProperty, (_, _) => { UpdateMinimum(); Render(); });
        foreach (var property in new[] { ContentTemplateProperty, ContentTemplateSelectorProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty, FontStretchProperty, CharacterSpacingProperty, ForegroundProperty, LanguageProperty, IsTextScaleFactorEnabledProperty }) RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerAdminShell), new PropertyMetadata(value, (owner, _) => ((BleeckerAdminShell)owner).Render()));
    public static readonly DependencyProperty HeaderProperty = Property(nameof(Header), typeof(object), null);
    public static readonly DependencyProperty HeaderTemplateProperty = Property(nameof(HeaderTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty HeaderTemplateSelectorProperty = Property(nameof(HeaderTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty SidebarProperty = Property(nameof(Sidebar), typeof(object), null);
    public static readonly DependencyProperty SidebarTemplateProperty = Property(nameof(SidebarTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty SidebarTemplateSelectorProperty = Property(nameof(SidebarTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty FooterProperty = Property(nameof(Footer), typeof(object), null);
    public static readonly DependencyProperty FooterTemplateProperty = Property(nameof(FooterTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty FooterTemplateSelectorProperty = Property(nameof(FooterTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty ContentPaddingProperty = Property(nameof(ContentPadding), typeof(Thickness), new Thickness(0));
    public static readonly DependencyProperty ContentBackgroundProperty = Property(nameof(ContentBackground), typeof(Brush), null);
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
    public DataTemplate? HeaderTemplate { get => (DataTemplate?)GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }
    public DataTemplateSelector? HeaderTemplateSelector { get => (DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty); set => SetValue(HeaderTemplateSelectorProperty, value); }
    public object? Sidebar { get => GetValue(SidebarProperty); set => SetValue(SidebarProperty, value); }
    public DataTemplate? SidebarTemplate { get => (DataTemplate?)GetValue(SidebarTemplateProperty); set => SetValue(SidebarTemplateProperty, value); }
    public DataTemplateSelector? SidebarTemplateSelector { get => (DataTemplateSelector?)GetValue(SidebarTemplateSelectorProperty); set => SetValue(SidebarTemplateSelectorProperty, value); }
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }
    public DataTemplate? FooterTemplate { get => (DataTemplate?)GetValue(FooterTemplateProperty); set => SetValue(FooterTemplateProperty, value); }
    public DataTemplateSelector? FooterTemplateSelector { get => (DataTemplateSelector?)GetValue(FooterTemplateSelectorProperty); set => SetValue(FooterTemplateSelectorProperty, value); }
    /// <summary>Explicit main-body padding; clearing it restores spacing based on the native viewport width.</summary>
    public Thickness ContentPadding { get => (Thickness)GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public Brush? ContentBackground { get => (Brush?)GetValue(ContentBackgroundProperty); set => SetValue(ContentBackgroundProperty, value); }
    /// <summary>The current native main scrolling viewport; null before template application and after removal.</summary>
    public ScrollViewer? Viewport => main?.Viewport;
    private static bool Styled(Control owner, DependencyProperty property) { for (var style = owner.Style; style is not null; style = style.BasedOn) if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true; return false; }
    private static bool Supplied(Control owner, DependencyProperty property) => owner.ReadLocalValue(property) != DependencyProperty.UnsetValue || Styled(owner, property);
    private BleeckerAdminShellModel.Spacing Spacing => BleeckerAdminShellModel.ContentSpacing(XamlRoot?.Size.Width ?? 0);
    private Thickness Insets => Supplied(this, ContentPaddingProperty) ? ContentPadding : new(Spacing.Horizontal, Spacing.Vertical, Spacing.Horizontal, Spacing.Vertical);
    private bool FooterWrapped => FooterTemplate is not null || FooterTemplateSelector is not null || BleeckerReactContent.Present(Footer);
    private void ObserveRoot() { if (ReferenceEquals(observedRoot, XamlRoot)) return; DisconnectRoot(); observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged; }
    private void DisconnectRoot() { minimum?.Suspend(); if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { UpdateMinimum(); Render(); }
    private void UpdateMinimum() => minimum?.Update();
    protected override void OnContentChanged(object oldContent, object newContent) { if (body is not null) { body.Release(); slots.Remove(body); } base.OnContentChanged(oldContent, newContent); Render(); }
    protected override void OnApplyTemplate()
    {
        Detach(); if (Template is null) return; base.OnApplyTemplate(); root = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("AdminShell requires PART_Layout.");
        BleeckerContentText Slot(string name) => new(name, "BleeckerPrimaryFontFamily", 16, 24, "BleeckerForegroundBrush", stretchContent: true);
        header = Slot("AdminShellHeader"); sidebar = Slot("AdminShellSidebar"); body = Slot("AdminShellBody"); footer = Slot("AdminShellFooterBody");
        main = new(this, body, footer) { Name = "AdminShellMain", UseLayoutRounding = false };
        flow = new(this, header, sidebar, main) { Name = "AdminShellFlow", UseLayoutRounding = false }; root.Children.Add(flow); minimum = new(this); UpdateMinimum(); Render();
    }
    private void Render()
    {
        if (flow is null || main is null || header is null || sidebar is null || body is null || footer is null) return;
        foreach (var slot in new[] { header, sidebar, body, footer })
        {
            slot.Text.FontFamily = FontFamily; slot.Text.FontSize = FontSize; slot.Text.FontWeight = FontWeight; slot.Text.FontStyle = FontStyle; slot.Text.FontStretch = FontStretch; slot.Text.CharacterSpacing = CharacterSpacing; slot.Text.Foreground = Foreground; slot.Text.LineHeight = FontSize * 1.5; slot.Text.Language = Language; slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled;
        }
        SetSlot(header, Header, HeaderTemplate, HeaderTemplateSelector); SetSlot(sidebar, Sidebar, SidebarTemplate, SidebarTemplateSelector); SetSlot(body, Content, ContentTemplate, ContentTemplateSelector); SetSlot(footer, Footer, FooterTemplate, FooterTemplateSelector);
        main.Update(); flow.InvalidateMeasure(); InvalidateMeasure();
    }
    private void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector)
    {
        if (slots.TryGetValue(slot, out var previous) && (value is string or bool || BleeckerReactContent.Numeric(value) ? Equals(previous.Value, value) : ReferenceEquals(previous.Value, value)) && ReferenceEquals(previous.Template, template) && ReferenceEquals(previous.Selector, selector)) return;
        slot.Set(value, template, selector: selector);
        var scalar = template is null && selector is null ? BleeckerReactContent.Display(value) as string : null;
        AutomationProperties.SetAccessibilityView(slot.Text, scalar is not null && BleeckerInlineText.Normalize(scalar).Length > 0 ? AccessibilityView.Content : AccessibilityView.Raw);
        slots[slot] = (value, template, selector);
    }
    private void Detach() { minimum?.Release(); minimum = null; foreach (var slot in slots.Keys) slot.Release(); slots.Clear(); main?.Release(); flow?.Children.Clear(); root?.Children.Clear(); root = null; flow = null; main = null; header = sidebar = body = footer = null; }
    protected override Size MeasureOverride(Size available) { if (IsLoaded) ObserveRoot(); UpdateMinimum(); var measured = base.MeasureOverride(available); return new(double.IsFinite(available.Width) ? available.Width : measured.Width, measured.Height); }
    protected override AutomationPeer OnCreateAutomationPeer() => new ShellPeer(this);
    private sealed class ShellPeer(BleeckerAdminShell owner) : FrameworkElementAutomationPeer(owner)
    { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; protected override string GetClassNameCore() => nameof(BleeckerAdminShell); }

    private sealed partial class ShellFlow : Panel
    {
        private readonly BleeckerAdminShell owner;
        private readonly FrameworkElement header;
        private readonly RowRegion row;
        internal ShellFlow(BleeckerAdminShell owner, FrameworkElement header, FrameworkElement sidebar, MainRegion main)
        { this.owner = owner; this.header = header; row = new(sidebar, main) { Name = "AdminShellRow", UseLayoutRounding = false }; Children.Add(header); Children.Add(row); }
        protected override Size MeasureOverride(Size available)
        {
            header.Measure(new Size(available.Width, double.PositiveInfinity)); row.Measure(new Size(available.Width, double.PositiveInfinity));
            var outer = owner.Padding.Top + owner.Padding.Bottom + owner.BorderThickness.Top + owner.BorderThickness.Bottom;
            var minimum = Math.Max(0, owner.MinHeight - outer);
            var height = BleeckerAdminShellModel.ShellHeight(minimum, header.DesiredSize.Height, row.DesiredSize.Height, 0, double.IsFinite(owner.Height) ? available.Height : null);
            row.Measure(new Size(available.Width, Math.Max(0, height - header.DesiredSize.Height)));
            return new(double.IsFinite(available.Width) ? available.Width : Math.Max(header.DesiredSize.Width, row.DesiredSize.Width), height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var rowHeight = Math.Max(0, final.Height - header.DesiredSize.Height);
            header.Arrange(new Rect(0, 0, final.Width, header.DesiredSize.Height)); row.Arrange(new Rect(0, header.DesiredSize.Height, final.Width, rowHeight)); return final;
        }
    }
    private sealed partial class RowRegion : Panel
    {
        private readonly FrameworkElement sidebar;
        private readonly MainRegion main;
        private double sidebarWidth;
        internal RowRegion(FrameworkElement sidebar, MainRegion main) { this.sidebar = sidebar; this.main = main; Children.Add(sidebar); Children.Add(main); }
        protected override Size MeasureOverride(Size available)
        {
            sidebar.Measure(new Size(available.Width, double.PositiveInfinity)); sidebarWidth = sidebar.DesiredSize.Width;
            main.Measure(new Size(Math.Max(0, available.Width - sidebarWidth), available.Height));
            return new(sidebarWidth + main.DesiredSize.Width, double.IsFinite(available.Height) ? available.Height : Math.Max(sidebar.DesiredSize.Height, main.DesiredSize.Height));
        }
        protected override Size ArrangeOverride(Size final)
        {
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, final.Width, final.Height) };
            sidebar.Arrange(new Rect(0, 0, sidebarWidth, sidebar.DesiredSize.Height)); main.Arrange(new Rect(sidebarWidth, 0, Math.Max(0, final.Width - sidebarWidth), final.Height)); return final;
        }
    }
    private sealed partial class MainRegion : Panel
    {
        private readonly MainFlow flow;
        private readonly Binding backgroundBinding;
        private readonly RectangleGeometry mainClip = new();
        internal ScrollViewer Viewport { get; }
        internal MainRegion(BleeckerAdminShell owner, FrameworkElement body, FrameworkElement footer)
        {
            flow = new(owner, body, footer) { Name = "AdminShellScrollContent", UseLayoutRounding = false };
            Viewport = (ScrollViewer)XamlReader.Load("<ScrollViewer xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' IsTabStop='False' UseLayoutRounding='False' CanContentRenderOutsideBounds='True' HorizontalScrollMode='Enabled' HorizontalScrollBarVisibility='Auto' VerticalScrollMode='Enabled' VerticalScrollBarVisibility='Auto' ZoomMode='Disabled' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch'/>");
            backgroundBinding = new Binding { Source = owner, Path = new PropertyPath(nameof(ContentBackground)), Mode = BindingMode.OneWay };
            Viewport.SetBinding(Control.BackgroundProperty, backgroundBinding);
            Viewport.Content = flow; Children.Add(Viewport);
            Viewport.ViewChanged += OnMainViewChanged;
            Clip = mainClip; Loaded += OnClipLoaded; Unloaded += OnClipUnloaded;
        }
        private void OnClipLoaded(object sender, RoutedEventArgs args) { LayoutUpdated += OnClipLayoutUpdated; UpdateClip(); }
        private void OnClipUnloaded(object sender, RoutedEventArgs args) => LayoutUpdated -= OnClipLayoutUpdated;
        private void OnClipLayoutUpdated(object? sender, object args) => UpdateClip();
        private void OnMainViewChanged(object? sender, ScrollViewerViewChangedEventArgs args) => UpdateClip();
        private void UpdateClip()
        {
            if (!IsLoaded || XamlRoot is null) return;
            var local = new Rect(0, 0, ActualWidth, ActualHeight);
            var transform = TransformToVisual(null);
            var origin = transform.TransformPoint(new Point());
            var horizontal = transform.TransformPoint(new Point(1, 0)); var vertical = transform.TransformPoint(new Point(0, 1));
            // Keep caller scaling/rotation in its own compositing space.
            // Device alignment applies to the ordinary layout axes, including
            // the native RTL mirror, and changes only the painted clip.
            if (Math.Abs(Math.Abs(horizontal.X - origin.X) - 1) < .000001 && Math.Abs(horizontal.Y - origin.Y) < .000001 && Math.Abs(vertical.X - origin.X) < .000001 && Math.Abs(vertical.Y - origin.Y - 1) < .000001 && transform.Inverse is { } inverse)
            {
                var bounds = transform.TransformBounds(local); var scale = XamlRoot.RasterizationScale;
                var left = Math.Floor(bounds.Left * scale + .5) / scale; var top = Math.Floor(bounds.Top * scale + .5) / scale;
                var right = Math.Floor(bounds.Right * scale + .5) / scale; var bottom = Math.Floor(bounds.Bottom * scale + .5) / scale;
                local = inverse.TransformBounds(new Rect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top)));
                flow.AlignFooterPaint(bounds, scale);
            }
            else flow.ResetFooterPaint();
            if (!mainClip.Rect.Equals(local)) mainClip.Rect = local;
        }
        internal void Update() { flow.Update(); InvalidateMeasure(); }
        internal void Release() { Loaded -= OnClipLoaded; Unloaded -= OnClipUnloaded; LayoutUpdated -= OnClipLayoutUpdated; Viewport.ViewChanged -= OnMainViewChanged; if (Viewport.GetBindingExpression(Control.BackgroundProperty)?.ParentBinding == backgroundBinding) Viewport.ClearValue(Control.BackgroundProperty); Viewport.Content = null; flow.Release(); Children.Clear(); }
        protected override Size MeasureOverride(Size available)
        {
            flow.Minimum = double.IsFinite(available.Height) ? available.Height : 0;
            flow.ViewportWidth = available.Width;
            Viewport.Measure(available); return new(Viewport.DesiredSize.Width, double.IsFinite(available.Height) ? available.Height : Viewport.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final) { Viewport.Arrange(new Rect(0, 0, final.Width, final.Height)); return final; }
        protected override AutomationPeer OnCreateAutomationPeer() => new MainPeer(this);
        private sealed class MainPeer(MainRegion owner) : FrameworkElementAutomationPeer(owner)
        { protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group; protected override AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.Main; protected override string GetClassNameCore() => "BleeckerAdminShellMain"; }
    }
    private sealed partial class MainFlow : Panel
    {
        private readonly BleeckerAdminShell owner;
        private readonly BleeckerContentText body, footer;
        private readonly Border contentHost;
        private readonly ContentControl footerHost;
        private readonly TranslateTransform footerPaintShift = new();
        private Border? footerDecoration;
        private double bodyWidth, footerWidth, footerHeight;
        private double minimum;
        private double viewportWidth;
        internal double ViewportWidth { get => viewportWidth; set { if (viewportWidth != value) { viewportWidth = value; InvalidateMeasure(); } } }
        internal double Minimum { get => minimum; set { if (minimum != value) { minimum = value; InvalidateMeasure(); } } }
        internal MainFlow(BleeckerAdminShell owner, FrameworkElement body, FrameworkElement footer)
        {
            this.owner = owner; this.body = (BleeckerContentText)body; this.footer = (BleeckerContentText)footer;
            this.body.FlowMinimumChanged += InvalidateMeasure; this.footer.FlowMinimumChanged += InvalidateMeasure;
            body.VerticalAlignment = VerticalAlignment.Top;
            contentHost = new Border { Name = "AdminShellContent", UseLayoutRounding = false };
            footerHost = (ContentControl)XamlReader.Load("<ContentControl xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' IsTabStop='False' IsHitTestVisible='False' UseLayoutRounding='False' BorderBrush='{ThemeResource AdminShellFooterBorderBrush}'><ContentControl.Template><ControlTemplate TargetType='ContentControl'><Border x:Name='AdminShellFooterDecoration' UseLayoutRounding='True' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}'/></ControlTemplate></ContentControl.Template></ContentControl>");
            footerHost.Name = "AdminShellFooter";
            AutomationProperties.SetAccessibilityView(footerHost, AccessibilityView.Raw);
            Children.Add(contentHost); Children.Add(body); Children.Add(footerHost); Children.Add(footer);
        }
        internal void Update() { contentHost.Padding = owner.Insets; var wrapped = owner.FooterWrapped; footerHost.Padding = wrapped ? new Thickness(owner.Spacing.Horizontal, 20, owner.Spacing.Horizontal, 20) : new Thickness(0); footerHost.BorderThickness = new Thickness(0, wrapped ? 1 : 0, 0, 0); InvalidateMeasure(); }
        internal void AlignFooterPaint(Rect mainBounds, double scale)
        {
            if (!footerHost.IsLoaded) return;
            if (footerDecoration is null)
            {
                footerHost.ApplyTemplate();
                footerDecoration = VisualTreeHelper.GetChild(footerHost, 0) as Border ?? throw new InvalidOperationException("AdminShell requires its native footer decoration.");
                footerDecoration.RenderTransform = footerPaintShift;
            }
            var transform = footerHost.TransformToVisual(null);
            var inverse = transform.Inverse ?? throw new InvalidOperationException("AdminShell footer paint requires an invertible layout transform.");
            var bounds = transform.TransformBounds(new Rect(0, 0, footerHost.ActualWidth, footerHost.ActualHeight));
            double Snap(double value) => Math.Floor(value * scale + .5) / scale;
            // The native scrolling parent snaps its origin separately. Align
            // only the decoration to the final device grid, leaving both its
            // logical wrapper and the original caller footer unchanged.
            var delta = new Point(Snap(bounds.Left) - Snap(mainBounds.Left) - (bounds.Left - mainBounds.Left), Snap(bounds.Top) - Snap(mainBounds.Top) - (bounds.Top - mainBounds.Top));
            var origin = inverse.TransformPoint(new Point()); var shifted = inverse.TransformPoint(delta);
            var x = shifted.X - origin.X; var y = shifted.Y - origin.Y;
            if (footerPaintShift.X != x) footerPaintShift.X = x;
            if (footerPaintShift.Y != y) footerPaintShift.Y = y;
        }
        internal void ResetFooterPaint() { footerPaintShift.X = 0; footerPaintShift.Y = 0; }
        internal void Release() { body.FlowMinimumChanged -= InvalidateMeasure; footer.FlowMinimumChanged -= InvalidateMeasure; Children.Clear(); }
        protected override Size MeasureOverride(Size available)
        {
            static double MeasureSlot(BleeckerContentText slot, double width)
            {
                slot.Measure(new Size(width, double.PositiveInfinity));
                var allocated = Math.Max(width, slot.FlowMinimumWidth);
                if (allocated != width) slot.Measure(new Size(allocated, double.PositiveInfinity));
                return allocated;
            }
            bodyWidth = MeasureSlot(body, Math.Max(0, viewportWidth - contentHost.Padding.Left - contentHost.Padding.Right));
            footerWidth = MeasureSlot(footer, Math.Max(0, viewportWidth - footerHost.Padding.Left - footerHost.Padding.Right));
            var bodyHeight = body.DesiredSize.Height + contentHost.Padding.Top + contentHost.Padding.Bottom;
            footerHeight = footer.DesiredSize.Height + footerHost.Padding.Top + footerHost.Padding.Bottom + footerHost.BorderThickness.Top;
            contentHost.Measure(new Size(viewportWidth, bodyHeight)); footerHost.Measure(new Size(viewportWidth, footerHeight));
            var height = BleeckerAdminShellModel.ContentHeight(bodyHeight, footerHeight, Minimum) + footerHeight;
            var contentStart = FlowDirection == FlowDirection.RightToLeft ? contentHost.Padding.Right : contentHost.Padding.Left;
            var footerStart = FlowDirection == FlowDirection.RightToLeft ? footerHost.Padding.Right : footerHost.Padding.Left;
            var extent = Math.Max(viewportWidth, Math.Max(contentStart + bodyWidth, footerStart + footerWidth));
            return new(extent, height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var natural = body.DesiredSize.Height + contentHost.Padding.Top + contentHost.Padding.Bottom;
            var contentHeight = BleeckerAdminShellModel.ContentHeight(natural, footerHeight, final.Height);
            var contentStart = FlowDirection == FlowDirection.RightToLeft ? contentHost.Padding.Right : contentHost.Padding.Left;
            var footerStart = FlowDirection == FlowDirection.RightToLeft ? footerHost.Padding.Right : footerHost.Padding.Left;
            contentHost.Arrange(new Rect(0, 0, viewportWidth, contentHeight));
            body.Arrange(new Rect(contentStart, contentHost.Padding.Top, bodyWidth, body.DesiredSize.Height));
            footerHost.Arrange(new Rect(0, contentHeight, viewportWidth, footerHeight));
            footer.Arrange(new Rect(footerStart, contentHeight + footerHost.BorderThickness.Top + footerHost.Padding.Top, footerWidth, footer.DesiredSize.Height));
            return final;
        }
    }
}
