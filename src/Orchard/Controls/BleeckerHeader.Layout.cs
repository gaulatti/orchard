using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerHeader
{
    private sealed class ManualPanel : Panel
    {
        internal readonly Dictionary<UIElement, Rect> Bounds = [];
        internal Size Extent;
        protected override Size MeasureOverride(Size available) { foreach (var child in Children.ToArray()) child.Measure(Bounds.TryGetValue(child, out var box) ? new Size(box.Width, box.Height) : available); return Extent; }
        protected override Size ArrangeOverride(Size final) { foreach (var (child, bounds) in Bounds.ToArray()) if (Children.Contains(child)) child.Arrange(bounds); return final; }
        internal void Complete(Size value) { if (Extent != value) { Extent = value; InvalidateMeasure(); } else InvalidateArrange(); Measure(value); }
    }
    private sealed class HeaderCommand(HeaderSurface surface) : ICommand, IDisposable
    {
        private ICommand? observed;
        private bool disposed;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => !disposed && surface.Current && (surface.owner.Command?.CanExecute(parameter) ?? true);
        public void Execute(object? parameter) { if (CanExecute(parameter)) surface.owner.Command?.Execute(parameter); }
        internal void Observe() { if (observed is not null) observed.CanExecuteChanged -= Changed; observed = !disposed && surface.IsLoaded ? surface.owner.Command : null; if (observed is not null) observed.CanExecuteChanged += Changed; Notify(); }
        private void Changed(object? sender, EventArgs args) { if (surface.owner.uiQueue.HasThreadAccess) Notify(); else surface.owner.uiQueue.TryEnqueue(() => { if (!disposed) Notify(); }); }
        internal void Notify() { surface.NotifyBrand(); CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
        public void Dispose() { disposed = true; if (observed is not null) observed.CanExecuteChanged -= Changed; observed = null; Notify(); }
    }
    private sealed class HeaderSurface : Panel
    {
        internal readonly BleeckerHeader owner;
        private readonly ManualPanel container = new() { Name = "HeaderContainer", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        private readonly ManualPanel row = new() { Name = "HeaderRow", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        private readonly ManualPanel brand = new() { Name = "HeaderBrand", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        private readonly BleeckerBrandLockup brandContent;
        private readonly ImageSource? defaultLogo;
        private readonly BleeckerNavItem brandItem = new();
        private readonly BleeckerNavLinkContext brandContext;
        private FrameworkElement? brandElement;
        private DataTemplate? brandTemplate;
        private readonly ResourceDictionary brandLinks = new() { Source = new Uri("ms-appx:///Orchard/Themes/BrandActions.xaml") };
        private readonly BleeckerContentText actions = new("HeaderActions", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerForegroundBrush");
        private readonly BleeckerContentText mobileActions = new("HeaderMobileActions", "BleeckerPrimaryFontFamily", 16, 24, "BleeckerForegroundBrush");
        private readonly BleeckerNavMenu navigation = new() { Name = "HeaderDesktopNavigation", UseLayoutRounding = false };
        internal readonly BleeckerNavMenu MobileNavigation = new() { Name = "HeaderMobileNavigation", Direction = BleeckerNavDirection.Vertical, UseLayoutRounding = false };
        private readonly BleeckerIconButton toggle = new() { Name = "HeaderToggle", Variant = BleeckerIconButtonVariant.Ghost, UseLayoutRounding = false };
        private readonly ContentControl icon = new() { Width = 20, Height = 20, IsTabStop = false, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
        private readonly Grid backdrop = new() { Name = "HeaderBackdrop", IsHitTestVisible = false };
        private readonly Border background = new() { Name = "HeaderBackground" }, rule;
        private readonly HeaderPaintBinding backgroundPaint, rulePaint;
        private readonly MobileSurface mobile;
        private readonly HeaderCommand command;
        private HeaderBackdrop? effect;
        private (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? actionValue, mobileValue;
        private Rect containerBox;
        internal bool Current => owner.IsLoaded && IsLoaded && ReferenceEquals(owner.surface, this) && owner.IsEnabled;
        internal HeaderSurface(BleeckerHeader owner)
        {
            this.owner = owner; command = new(this);
            brandContent = new() { Name = "HeaderBrandContent", LinkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ContentPresenter Content='{Binding Content}'/></DataTemplate>") }; defaultLogo = brandContent.LogoSource;
            brandContent.IntrinsicMetricsChanged += BrandMetricsChanged;
            AutomationProperties.SetAccessibilityView(brandContent, AccessibilityView.Raw);
            brandContext = new(CanInvokeBrand, context => { var original = owner.Brand; owner.Command?.Execute(owner.CommandParameter); owner.BrandInvoked?.Invoke(owner, original); });
            brandContext.Content = brandContent; brandContext.Item = brandItem;
            rule = new() { Name = "HeaderBottomRule", IsHitTestVisible = false };
            backgroundPaint = new(owner, Control.BackgroundProperty, value => background.Background = value);
            rulePaint = new(owner, Control.BorderBrushProperty, value => rule.Background = value);
            backgroundPaint.Start(); rulePaint.Start();
            mobile = new(this);
            Children.Add(backdrop); Children.Add(background); Children.Add(container); Children.Add(rule); Children.Add(mobile);
            container.Children.Add(row); row.Children.Add(brand); row.Children.Add(navigation); row.Children.Add(actions); row.Children.Add(toggle);
            toggle.Content = icon; toggle.Click += Toggle; AutomationProperties.SetName(toggle, "Toggle navigation menu");
            navigation.ItemInvoked += owner.NavigationInvoked; MobileNavigation.ItemInvoked += owner.NavigationInvoked;
            Loaded += LoadedSurface; Unloaded += UnloadedSurface; UpdateIcon();
        }
        private void LoadedSurface(object sender, RoutedEventArgs args) { backgroundPaint.Start(); rulePaint.Start(); command.Observe(); brandContext.Notify(); effect ??= new(backdrop); effect.Update(); }
        private void UnloadedSurface(object sender, RoutedEventArgs args) { backgroundPaint.Stop(); rulePaint.Stop(); effect?.Dispose(); effect = null; command.Observe(); brandContext.Notify(); }
        private void Toggle(object sender, RoutedEventArgs args) => owner.ToggleNavigation();
        private void BrandMetricsChanged(object? sender, EventArgs args) => InvalidateMeasure();
        internal void NotifyBrand() => brandContext.Notify();
        internal void InvalidateMenu() { mobile.InvalidateMeasure(); InvalidateMeasure(); }
        private bool CanInvokeBrand(BleeckerNavLinkContext context)
        {
            if (!Current || !context.Mounted || context.Element?.IsLoaded != true || !(owner.Command?.CanExecute(owner.CommandParameter) ?? true)) return false;
            var mounted = false;
            for (DependencyObject? node = context.Element; node is not null; node = VisualTreeHelper.GetParent(node)) { if (node is FrameworkElement { Visibility: Visibility.Collapsed }) return false; mounted |= ReferenceEquals(node, this); }
            return mounted;
        }
        internal void UpdateIcon()
        {
            var canvas = new Canvas { Width = 24, Height = 24, IsHitTestVisible = false };
            var segments = owner.menuOpen ? new[] { (18d, 6d, 6d, 18d), (6d, 6d, 18d, 18d) } : new[] { (4d, 5d, 20d, 5d), (4d, 12d, 20d, 12d), (4d, 19d, 20d, 19d) };
            foreach (var (x1, y1, x2, y2) in segments) { var line = new Microsoft.UI.Xaml.Shapes.Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, StrokeThickness = 2, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round }; line.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty, new Binding { Source = icon, Path = new PropertyPath(nameof(Foreground)) }); canvas.Children.Add(line); }
            icon.Content = new Viewbox { Width = 20, Height = 20, Child = canvas, IsHitTestVisible = false };
        }
        internal void Sync()
        {
            var model = owner.Brand;
            if (!Enum.IsDefined(model.Size)) throw new ArgumentOutOfRangeException(nameof(model.Size));
            brandContent.Href = model.Href; brandContent.BrandName = model.BrandName; brandContent.LogoAlt = model.LogoAlt; brandContent.Size = model.Size;
            brandContent.LogoSource = model.ReadLocalValue(BleeckerHeaderBrand.LogoSourceProperty) == DependencyProperty.UnsetValue ? defaultLogo : model.LogoSource;
            brandContent.FlowDirection = owner.FlowDirection; brandContent.Language = owner.Language; brandContent.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            var style = owner.LinkStyle ?? (Style)brandLinks[typeof(HyperlinkButton)];
            if (brandElement is null || !ReferenceEquals(brandTemplate, owner.LinkTemplate))
            {
                brandContext.Mounted = false; brandContext.Notify(); brand.Children.Clear();
                if (brandElement is HyperlinkButton old && brandTemplate is null) old.Content = null;
                brandElement = owner.LinkTemplate is null ? new HyperlinkButton { Content = brandContent, Command = brandContext.Command, Tag = brandContext } : owner.LinkTemplate.LoadContent() as FrameworkElement ?? throw new InvalidOperationException("Header LinkTemplate must create a FrameworkElement.");
                brandTemplate = owner.LinkTemplate; brandElement.DataContext = brandContext; brandContext.Element = brandElement; brand.Children.Add(brandElement);
            }
            brandItem.Href = model.Href; brandItem.Label = model.BrandName ?? model.LogoAlt;
            brandContext.Href = brandItem.Href; brandContext.Label = brandItem.Label; brandContext.Style = style; brandContext.Foreground = owner.Foreground; brandContext.Mounted = true;
            if (brandTemplate is null) { ((HyperlinkButton)brandElement).Style = style; ((HyperlinkButton)brandElement).Foreground = owner.Foreground; AutomationProperties.SetName(brandElement, string.Join(" ", new[] { model.LogoAlt, model.BrandName }.Where(value => !string.IsNullOrEmpty(value)))); }
            brandElement.FlowDirection = owner.FlowDirection; brandElement.Language = owner.Language; brandContext.Notify();
            foreach (var menu in new[] { navigation, MobileNavigation }) { if (!ReferenceEquals(menu.Items, owner.Navigation)) menu.Items = owner.Navigation; menu.LinkTemplate = owner.LinkTemplate; menu.LinkStyle = owner.LinkStyle; menu.Command = command; menu.CommandParameter = owner.CommandParameter; menu.FlowDirection = owner.FlowDirection; menu.Language = owner.Language; menu.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; menu.ApplyTemplate(); }
            foreach (var slot in new[] { actions, mobileActions }) { slot.FlowDirection = owner.FlowDirection; slot.Language = owner.Language; slot.Text.FontFamily = owner.FontFamily; slot.Text.FontSize = owner.FontSize; slot.Text.LineHeight = owner.FontSize * 1.5; slot.Text.Foreground = owner.Foreground; slot.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; }
            SetSlot(actions, owner.Actions, owner.ActionsTemplate, owner.ActionsTemplateSelector, ref actionValue); SetSlot(mobileActions, owner.MobileActions, owner.MobileActionsTemplate, owner.MobileActionsTemplateSelector, ref mobileValue); command.Observe();
        }
        private static void SetSlot(BleeckerContentText slot, object? value, DataTemplate? template, DataTemplateSelector? selector, ref (object? Value, DataTemplate? Template, DataTemplateSelector? Selector)? current)
        {
            if (current is { } prior && (value is string or bool || BleeckerReactContent.Numeric(value) ? Equals(prior.Value, value) : ReferenceEquals(prior.Value, value)) && ReferenceEquals(prior.Template, template) && ReferenceEquals(prior.Selector, selector)) return;
            slot.Set(value, template, selector: selector); current = (value, template, selector);
        }
        internal bool HasMobileActions => owner.MobileActionsTemplate is not null || owner.MobileActionsTemplateSelector is not null || BleeckerReactContent.Present(owner.MobileActions);
        protected override Size MeasureOverride(Size available)
        {
            var width = available.Width; var viewport = owner.Viewport; var height = owner.HeaderHeight; var desktop = viewport >= 1024; var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            var containerWidth = BleeckerHeaderModel.Container(viewport, width, owner.FullWidth); var inner = Math.Max(0, containerWidth - 32);
            navigation.Visibility = actions.Visibility = desktop ? Visibility.Visible : Visibility.Collapsed; toggle.Visibility = desktop ? Visibility.Collapsed : Visibility.Visible; mobile.Visibility = desktop ? Visibility.Collapsed : Visibility.Visible;
            navigation.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); actions.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
            var navWidth = desktop ? navigation.DesiredSize.Width : 0; var navHeight = desktop ? navigation.DesiredSize.Height : 0; var actionWidth = desktop ? actions.DesiredSize.Width : 0; var actionHeight = desktop ? actions.DesiredSize.Height : 0;
            var side = Math.Max(0, Math.Floor((inner - navWidth - 64) * 32) / 64); var brandAvailable = desktop ? side : Math.Max(0, inner - 72);
            brandContent.ApplyTemplate(); if (brandContent.InlineMetricsReady) brandContent.PrepareInline(brandAvailable); brandElement?.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
            var brandWidth = brandElement?.DesiredSize.Width ?? 0; var brandHeight = brandElement?.DesiredSize.Height ?? 0; var wrapperHeight = Math.Max(brandHeight, (brandHeight + (owner.Brand.Size == BleeckerBrandLockupSize.Large ? 48 : 32)) / 2 + 6);
            if (brandElement is not null) brand.Bounds[brandElement] = new(0, 0, brandWidth, brandHeight); brand.Complete(new(brandWidth, wrapperHeight));
            var brandX = rtl ? inner - brandWidth : 0; var navX = side + 32; var actionX = Math.Max(inner, navWidth + 64) - actionWidth;
            if (rtl) { navX = inner - navX - navWidth; actionX = 0; }
            row.Bounds[brand] = new(brandX, (height - wrapperHeight) / 2, brandWidth, wrapperHeight);
            row.Bounds[navigation] = new(navX, (height - navHeight) / 2, navWidth, navHeight); row.Bounds[actions] = new(actionX, (height - actionHeight) / 2, actionWidth, actionHeight);
            toggle.Measure(new(40, 40)); row.Bounds[toggle] = new(rtl ? 0 : inner - 40, (height - 40) / 2, 40, 40); row.Complete(new(inner, height));
            container.Bounds[row] = new(16, 0, inner, height); container.Complete(new(containerWidth, height)); containerBox = new((width - containerWidth) / 2, 0, containerWidth, height);
            mobile.Measure(new(width, double.PositiveInfinity)); backdrop.Measure(new(width, height + 1)); background.Measure(new(width, height + 1)); rule.Measure(new(width, 1)); return new(width, height + 1);
        }
        protected override Size ArrangeOverride(Size final)
        {
            backdrop.Arrange(new(0, 0, final.Width, final.Height)); background.Arrange(new(0, 0, final.Width, final.Height)); container.Arrange(containerBox); rule.Arrange(new(0, owner.HeaderHeight, final.Width, 1)); mobile.Arrange(new(0, owner.HeaderHeight, final.Width, mobile.DesiredSize.Height)); effect?.Update(); return final;
        }
        internal void Release() { backgroundPaint.Stop(); rulePaint.Stop(); effect?.Dispose(); effect = null; command.Dispose(); brandContext.Mounted = false; brandContext.Notify(); brandContent.IntrinsicMetricsChanged -= BrandMetricsChanged; if (brandElement is HyperlinkButton link && brandTemplate is null) link.Content = null; brand.Children.Clear(); actions.Release(); mobileActions.Release(); mobile.Release(); navigation.ItemInvoked -= owner.NavigationInvoked; MobileNavigation.ItemInvoked -= owner.NavigationInvoked; toggle.Click -= Toggle; Loaded -= LoadedSurface; Unloaded -= UnloadedSurface; Children.Clear(); }

        private sealed class MobileSurface : Panel
        {
            private readonly HeaderSurface parent;
            private readonly ManualPanel viewport = new() { Name = "HeaderMobileViewport", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
            private readonly ManualPanel inner = new() { Name = "HeaderMobileContent", UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
            private readonly Border background, topRule;
            private readonly Grid[] shadowHosts = [new() { IsHitTestVisible = false }, new() { IsHitTestVisible = false }];
            private readonly List<BleeckerCssShadow> shadows = [];
            internal MobileSurface(HeaderSurface parent)
            {
                this.parent = parent; Name = "HeaderMobilePanel"; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight;
                static Border Palette(string key) => (Border)XamlReader.Load($"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{{ThemeResource {key}}}' UseLayoutRounding='False'/>");
                background = Palette("HeaderMobileBackgroundBrush"); background.Name = "HeaderMobileBackground"; topRule = Palette("HeaderMobileRuleBrush"); topRule.Name = "HeaderMobileTopRule";
                foreach (var host in shadowHosts) { var mask = new Microsoft.UI.Xaml.Shapes.Path { Fill = new SolidColorBrush(Microsoft.UI.Colors.White), Visibility = Visibility.Collapsed }; var exterior = new Microsoft.UI.Xaml.Shapes.Path { Fill = new SolidColorBrush(Microsoft.UI.Colors.White), Visibility = Visibility.Collapsed }; AutomationProperties.SetAccessibilityView(mask, AccessibilityView.Raw); AutomationProperties.SetAccessibilityView(exterior, AccessibilityView.Raw); host.Children.Add(mask); host.Children.Add(exterior); Children.Add(host); shadows.Add(new(parent.owner, background, host, mask, exterior)); }
                shadows[0].Set(new(64, 24, -20, .28f, 26, 55, 77)); shadows[1].Set(new(14, 4, 0, .06f, 26, 55, 77));
                Children.Add(viewport); viewport.Children.Add(background); viewport.Children.Add(topRule); viewport.Children.Add(inner); inner.Children.Add(parent.mobileActions); inner.Children.Add(parent.MobileNavigation);
            }
            protected override Size MeasureOverride(Size available)
            {
                var width = available.Width; var owner = parent.owner; var container = BleeckerHeaderModel.Container(owner.Viewport, width, owner.FullWidth); var contentWidth = Math.Max(0, container - 32); var hasActions = parent.HasMobileActions;
                parent.mobileActions.Visibility = hasActions ? Visibility.Visible : Visibility.Collapsed; parent.mobileActions.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); parent.MobileNavigation.Measure(new(contentWidth, double.PositiveInfinity));
                var actionWidth = hasActions ? parent.mobileActions.DesiredSize.Width : 0; var actionHeight = hasActions ? parent.mobileActions.DesiredSize.Height : 0;
                var navY = 24 + (hasActions ? actionHeight + 20 : 0); var natural = navY + parent.MobileNavigation.DesiredSize.Height + 24;
                inner.Bounds[parent.mobileActions] = new(owner.FlowDirection == FlowDirection.RightToLeft ? container - 16 - actionWidth : 16, 24, actionWidth, actionHeight);
                inner.Bounds[parent.MobileNavigation] = new(16, navY, contentWidth, parent.MobileNavigation.DesiredSize.Height); inner.Complete(new(container, natural));
                var now = owner.clock.Elapsed.TotalMilliseconds; var height = BleeckerHeaderModel.MenuHeight(natural + 1, owner.maximum.ValueAt(now)); Opacity = owner.opacity.ValueAt(now);
                viewport.Bounds[background] = new(0, 0, width, height); viewport.Bounds[topRule] = new(0, 0, width, 1); viewport.Bounds[inner] = new((width - container) / 2, 1, container, natural); viewport.Complete(new(width, height)); viewport.Clip = new RectangleGeometry { Rect = new(0, 0, width, height) };
                foreach (var host in shadowHosts) host.Measure(new(width, height)); return new(width, height);
            }
            protected override Size ArrangeOverride(Size final) { foreach (var host in shadowHosts) host.Arrange(new(0, 0, final.Width, final.Height)); viewport.Arrange(new(0, 0, final.Width, final.Height)); return final; }
            internal void Release() { foreach (var shadow in shadows) shadow.Dispose(); shadows.Clear(); inner.Children.Clear(); Children.Clear(); }
        }
    }
}
