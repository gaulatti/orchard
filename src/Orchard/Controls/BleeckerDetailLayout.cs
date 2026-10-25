using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>Responsive content and optional complementary rail with container-bounded sticky positioning.</summary>
public sealed partial class BleeckerDetailLayout : ContentControl
{
    private Grid? root;
    private LayoutPanel? panel;
    private BleeckerContentText? main, railText;
    private RailGroup? rail;
    private readonly TranslateTransform railShift = new();
    private XamlRoot? observedRoot;
    private DependencyObject? scrollOwner;
    private FrameworkElement? stickyViewport;
    public BleeckerDetailLayout()
    {
        DefaultStyleKey = typeof(BleeckerDetailLayout); IsTabStop = false; UseLayoutRounding = false;
        Loaded += (_, _) => { ObserveRoot(); LayoutUpdated += LayoutChanged; Render(); };
        Unloaded += (_, _) => { LayoutUpdated -= LayoutChanged; DisconnectRoot(); DisconnectScroll(); railShift.Y = 0; };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) DetachParts(); });
        foreach (var property in new[] { ContentTemplateProperty, ContentTemplateSelectorProperty, FontFamilyProperty, FontSizeProperty, FontWeightProperty,
            FontStyleProperty, FontStretchProperty, CharacterSpacingProperty, ForegroundProperty, IsTextScaleFactorEnabledProperty, LanguageProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Render());
    }
    private static DependencyProperty Property(string name, Type type, object? value) => DependencyProperty.Register(name, type, typeof(BleeckerDetailLayout),
        new PropertyMetadata(value, (owner, _) => ((BleeckerDetailLayout)owner).Render()));
    public static readonly DependencyProperty RatioProperty = Property(nameof(Ratio), typeof(BleeckerDetailLayoutRatio), BleeckerDetailLayoutRatio.Primary);
    public static readonly DependencyProperty SideProperty = Property(nameof(Side), typeof(BleeckerDetailLayoutSide), BleeckerDetailLayoutSide.End);
    public static readonly DependencyProperty SideRailProperty = Property(nameof(SideRail), typeof(object), null);
    public static readonly DependencyProperty SideRailTemplateProperty = Property(nameof(SideRailTemplate), typeof(DataTemplate), null);
    public static readonly DependencyProperty SideRailTemplateSelectorProperty = Property(nameof(SideRailTemplateSelector), typeof(DataTemplateSelector), null);
    public static readonly DependencyProperty SideRailLabelProperty = Property(nameof(SideRailLabel), typeof(string), null);
    public static readonly DependencyProperty StickyProperty = Property(nameof(Sticky), typeof(bool), true);
    public BleeckerDetailLayoutRatio Ratio { get => (BleeckerDetailLayoutRatio)GetValue(RatioProperty); set { Validate(value); SetValue(RatioProperty, value); } }
    public BleeckerDetailLayoutSide Side { get => (BleeckerDetailLayoutSide)GetValue(SideProperty); set { Validate(value); SetValue(SideProperty, value); } }
    public object? SideRail { get => GetValue(SideRailProperty); set => SetValue(SideRailProperty, value); }
    public DataTemplate? SideRailTemplate { get => (DataTemplate?)GetValue(SideRailTemplateProperty); set => SetValue(SideRailTemplateProperty, value); }
    public DataTemplateSelector? SideRailTemplateSelector { get => (DataTemplateSelector?)GetValue(SideRailTemplateSelectorProperty); set => SetValue(SideRailTemplateSelectorProperty, value); }
    public string? SideRailLabel { get => (string?)GetValue(SideRailLabelProperty); set => SetValue(SideRailLabelProperty, value); }
    public bool Sticky { get => (bool)GetValue(StickyProperty); set => SetValue(StickyProperty, value); }
    private static void Validate<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private bool HasRail => SideRailTemplate is not null || SideRailTemplateSelector is not null || BleeckerReactContent.Present(SideRail);
    private double Viewport => XamlRoot?.Size.Width ?? 0;
    protected override void OnContentChanged(object oldContent, object newContent) { main?.Release(); base.OnContentChanged(oldContent, newContent); Render(); }
    protected override void OnApplyTemplate()
    {
        DetachParts(); base.OnApplyTemplate(); root = GetTemplateChild("PART_Root") as Grid;
        if (root is null) { if (ReadLocalValue(TemplateProperty) is null) return; throw new InvalidOperationException("DetailLayout requires a PART_Root Grid."); }
        main = new("DetailContent", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        railText = new("DetailRailText", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerTextPrimaryBrush", stretchContent: true);
        rail = new RailGroup { Name = "DetailSideRail", Content = railText, IsTabStop = false, Padding = new Thickness(0), BorderThickness = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Top, UseLayoutRounding = false, RenderTransform = railShift };
        panel = new(this, main, rail) { Name = "DetailColumns", UseLayoutRounding = false }; root.Children.Add(panel); Render();
    }
    private void Render()
    {
        Validate(Ratio); Validate(Side);
        if (panel is null || main is null || rail is null || railText is null) return;
        foreach (var slot in new[] { main, railText })
        {
            slot.Text.FontFamily = FontFamily; slot.Text.FontSize = FontSize; slot.Text.FontWeight = FontWeight; slot.Text.FontStyle = FontStyle;
            slot.Text.FontStretch = FontStretch; slot.Text.CharacterSpacing = CharacterSpacing; slot.Text.Foreground = Foreground;
            slot.Text.LineHeight = FontSize * 1.5; slot.Text.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; slot.Text.Language = Language;
        }
        main.Set(Content, ContentTemplate, selector: ContentTemplateSelector); railText.Set(SideRail, SideRailTemplate, selector: SideRailTemplateSelector);
        rail.Visibility = HasRail ? Visibility.Visible : Visibility.Collapsed; AutomationProperties.SetName(rail, SideRailLabel ?? "");
        panel.InvalidateMeasure(); InvalidateMeasure(); UpdateSticky();
    }
    private void DetachParts()
    {
        DisconnectScroll(); railShift.Y = 0; main?.Release(); railText?.Release(); if (rail is not null) rail.Content = null;
        panel?.Children.Clear(); if (root is not null && panel is not null) root.Children.Remove(panel);
        root = null; panel = null; main = railText = null; rail = null;
    }
    private void ObserveRoot()
    {
        if (ReferenceEquals(observedRoot, XamlRoot)) return;
        DisconnectRoot(); observedRoot = XamlRoot; if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void DisconnectRoot() { if (observedRoot is not null) observedRoot.Changed -= RootChanged; observedRoot = null; }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) { panel?.InvalidateMeasure(); InvalidateMeasure(); UpdateSticky(); }
    private void LayoutChanged(object? sender, object args) => UpdateSticky();
    private void ViewerChanged(object? sender, ScrollViewerViewChangedEventArgs args) => UpdateSticky();
    private void ViewChanged(ScrollView sender, object args) => UpdateSticky();
    private void DisconnectScroll()
    {
        if (scrollOwner is ScrollViewer viewer) viewer.ViewChanged -= ViewerChanged;
        if (scrollOwner is ScrollView view) view.ViewChanged -= ViewChanged;
        scrollOwner = null; stickyViewport = null;
    }
    private static ScrollContentPresenter? FindPresenter(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is ScrollContentPresenter presenter) return presenter;
            if (child is ScrollViewer or ScrollView) continue;
            var nested = FindPresenter(child); if (nested is not null) return nested;
        }
        return null;
    }
    private void ObserveScroll()
    {
        DependencyObject? next = null;
        for (var ancestor = VisualTreeHelper.GetParent(this); ancestor is not null; ancestor = VisualTreeHelper.GetParent(ancestor))
            if (ancestor is ScrollViewer or ScrollView) { next = ancestor; break; }
        if (ReferenceEquals(next, scrollOwner) && stickyViewport is not null) return;
        DisconnectScroll(); scrollOwner = next;
        if (next is ScrollView view)
        {
            view.ApplyTemplate(); stickyViewport = view.ScrollPresenter ?? throw new InvalidOperationException("Sticky DetailLayout requires the ScrollView's loaded ScrollPresenter.");
            view.ViewChanged += ViewChanged;
        }
        else if (next is ScrollViewer viewer)
        {
            viewer.ApplyTemplate(); stickyViewport = FindPresenter(viewer) ?? throw new InvalidOperationException("Sticky DetailLayout requires its ScrollViewer's ScrollContentPresenter.");
            viewer.ViewChanged += ViewerChanged;
        }
        else stickyViewport = XamlRoot?.Content as FrameworkElement;
    }
    private void UpdateSticky()
    {
        if (!IsLoaded || panel is not { IsLoaded: true } || rail is null) return;
        if (!HasRail || !Sticky || Viewport < 1024) { DisconnectScroll(); railShift.Y = 0; return; }
        ObserveScroll();
        if (stickyViewport is null) throw new InvalidOperationException("DetailLayout cannot locate its sticky viewport.");
        var transform = panel.TransformToVisual(stickyViewport); var origin = transform.TransformPoint(new Point());
        var scale = transform.TransformPoint(new Point(0, 1)).Y - origin.Y;
        if (!(scale > 0) || !double.IsFinite(scale)) throw new InvalidOperationException("DetailLayout sticky positioning requires a positive vertical scale.");
        var normalTop = (origin.Y / scale) + panel.RailY;
        var offset = BleeckerDetailLayoutModel.StickyOffset(normalTop, panel.ActualHeight, rail.ActualHeight, true);
        if (Math.Abs(railShift.Y - offset) >= .000001) railShift.Y = offset;
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsLoaded) ObserveRoot();
        var measured = base.MeasureOverride(availableSize);
        return new(double.IsFinite(availableSize.Width) ? availableSize.Width : measured.Width, measured.Height);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(BleeckerDetailLayout owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(BleeckerDetailLayout);
    }
    private sealed class RailGroup : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
        private sealed class Peer(RailGroup owner) : FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
            protected override AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.Custom;
            protected override string GetLocalizedLandmarkTypeCore() => "complementary";
            protected override string GetItemTypeCore() => "complementary";
        }
    }
    private sealed partial class LayoutPanel(BleeckerDetailLayout owner, BleeckerContentText main, RailGroup rail) : Panel
    {
        private BleeckerDetailLayoutModel.Tracks tracks;
        private double mainHeight, railHeight;
        internal double RailY { get; private set; }
        private void EnsureChildren() { if (Children.Count == 0) { Children.Add(main); Children.Add(rail); } }
        protected override Size MeasureOverride(Size available)
        {
            EnsureChildren(); var width = available.Width;
            if (!double.IsFinite(width))
            {
                main.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); rail.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                if (!owner.HasRail) width = main.DesiredSize.Width;
                else if (owner.Viewport < 1024) width = Math.Max(main.DesiredSize.Width, rail.DesiredSize.Width);
                else if (owner.Ratio == BleeckerDetailLayoutRatio.Balanced) width = 2 * Math.Max(main.DesiredSize.Width, rail.DesiredSize.Width) + (owner.Viewport < 1280 ? 48 : 64);
                else width = 3 * Math.Max(main.DesiredSize.Width / 2, Math.Max(272, rail.DesiredSize.Width)) + (owner.Viewport < 1280 ? 48 : 64);
            }
            tracks = BleeckerDetailLayoutModel.Columns(width, owner.Viewport, owner.HasRail, owner.Ratio, owner.Side);
            main.Measure(new Size(tracks.MainWidth, double.PositiveInfinity)); rail.Measure(new Size(tracks.RailWidth, double.PositiveInfinity));
            mainHeight = main.DesiredSize.Height; railHeight = owner.HasRail ? rail.DesiredSize.Height : 0;
            RailY = !owner.HasRail || tracks.Columns ? 0 : mainHeight + tracks.Gap;
            return new(width, !owner.HasRail ? mainHeight : tracks.Columns ? Math.Max(mainHeight, railHeight) : RailY + railHeight);
        }
        protected override Size ArrangeOverride(Size final)
        {
            main.Arrange(new Rect(tracks.MainX, 0, tracks.MainWidth, mainHeight));
            if (owner.HasRail) rail.Arrange(new Rect(tracks.RailX, RailY, tracks.RailWidth, railHeight));
            owner.UpdateSticky(); return final;
        }
    }
}
