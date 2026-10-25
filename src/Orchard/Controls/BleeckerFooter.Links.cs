using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerFooter
{
    private sealed class FooterLink
    {
        private readonly BleeckerFooter owner;
        internal string Key { get; }
        internal string Role { get; }
        internal DataTemplate? Template { get; }
        internal Grid Root { get; } = new() { UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        internal BleeckerNavLinkContext Context { get; }
        private readonly FrameworkElement element;
        private readonly BleeckerContentText? text;
        private readonly BleeckerBrandLockup? brand;
        private readonly ImageSource? defaultLogo;
        private readonly HyperlinkButton styleProbe = new() { Visibility = Visibility.Collapsed, IsTabStop = false };
        private readonly TextBlock normal, hover;
        private readonly SolidColorBrush ink = new();
        private readonly List<(SolidColorBrush Brush, long Color, long Opacity)> brushTokens = [];
        private readonly List<(DependencyProperty Property, long Token)> styleTokens = [];
        private readonly BleeckerCssTransition<(double R, double G, double B, double Alpha)> motion = new(BleeckerSrgbColorMix.MixPremultiplied);
        private bool hovering;
        private double recipeHeight;
        internal double BlockHeight { get; private set; }
        internal bool Running => motion.IsRunning(owner.clock.Elapsed.TotalMilliseconds);
        internal FooterLink(BleeckerFooter owner, string key, string role)
        {
            this.owner = owner; Key = key; Role = role; Template = owner.LinkTemplate; Context = new(owner.CanInvoke, owner.Invoke);
            var brush = role == "source" ? "FooterSourceTextBrush" : role == "brand" ? "BleeckerTextPrimaryBrush" : "BleeckerTextSecondaryBrush";
            TextBlock Probe(string resource) => (TextBlock)XamlReader.Load($"<TextBlock xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Foreground='{{ThemeResource {resource}}}' Visibility='Collapsed' AutomationProperties.AccessibilityView='Raw'/>");
            normal = Probe(brush); hover = Probe(role == "section" ? "BleeckerSunsetBrush" : "BleeckerSeaBrush");
            Root.Children.Add(normal); Root.Children.Add(hover); Root.Children.Add(styleProbe);
            if (role == "brand")
            {
                brand = new() { Name = "FooterBrandContent", Size = BleeckerBrandLockupSize.Large, LinkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><ContentPresenter Content='{Binding Content}'/></DataTemplate>") };
                defaultLogo = brand.LogoSource; AutomationProperties.SetAccessibilityView(brand, AccessibilityView.Raw); Context.Content = brand;
            }
            else { text = new(role == "source" ? "FooterSourceText" : "FooterLinkText", "BleeckerPrimaryFontFamily", role == "source" ? 14 : 16, role == "source" ? 20 : 24, brush); Context.Content = text; }
            if (Template is null)
            {
                var action = new HyperlinkButton { Name = role == "brand" ? "FooterBrandLink" : role == "source" ? "FooterSourceLink" : "FooterSectionLink", Content = Context.Content, Command = Context.Command };
                action.SetBinding(Control.ForegroundProperty, new Binding { Source = Context, Path = new PropertyPath(nameof(Context.Foreground)) }); element = action;
            }
            else element = Template.LoadContent() as FrameworkElement ?? throw new InvalidOperationException("Footer LinkTemplate must create a FrameworkElement.");
            Context.Element = element; element.DataContext = Context; Root.Children.Add(element);
            Root.PointerEntered += Entered; Root.PointerExited += Exited; Root.Loaded += Loaded;
            foreach (var property in new[] { Control.FontFamilyProperty, Control.FontSizeProperty, Control.FontWeightProperty, Control.CharacterSpacingProperty, Control.ForegroundProperty }) styleTokens.Add((property, styleProbe.RegisterPropertyChangedCallback(property, (_, _) => { Typography(); Paint(false); })));
        }
        internal void Update(BleeckerNavItem item)
        {
            Context.Item = item; Context.Label = item.Label; Context.Href = item.Href; Context.External = item.External;
            var style = owner.LinkStyle;
            if (style is null)
            {
                recipeHeight = Role == "source" ? 17 : 20;
                style = RecipeStyle(recipeHeight);
            }
            Context.Style = style; styleProbe.Style = style;
            if (Template is null) { ((HyperlinkButton)element).Style = style; AutomationProperties.SetName(element, Role == "brand" ? string.Join(" ", new[] { owner.Brand.LogoAlt, owner.Brand.BrandName }.Where(value => !string.IsNullOrEmpty(value))) : Role == "source" ? "View source on GitHub" : item.Label); }
            if (brand is not null)
            {
                brand.Href = item.Href; brand.BrandName = owner.Brand.BrandName; brand.LogoAlt = owner.Brand.LogoAlt;
                brand.LogoSource = owner.Brand.ReadLocalValue(BleeckerFooterBrand.LogoSourceProperty) == DependencyProperty.UnsetValue ? defaultLogo : owner.Brand.LogoSource;
            }
            if (text is not null) text.Set(Role == "source" ? "View source on GitHub" : item.Label, null);
            Root.FlowDirection = FlowDirection.LeftToRight; element.FlowDirection = owner.FlowDirection; element.Language = owner.Language;
            Context.Mounted = true; Typography(); Paint(false);
        }
        private Style RecipeStyle(double height)
        {
            var style = new Style(typeof(HyperlinkButton)) { BasedOn = (Style)owner.actions[typeof(HyperlinkButton)] };
            style.Setters.Add(new Setter(Control.FontSizeProperty, Role == "source" ? 14d : 16d));
            style.Setters.Add(new Setter(Control.CharacterSpacingProperty, Role == "source" ? -20 : 0));
            if (Role != "brand") style.Setters.Add(new Setter(HeightProperty, height));
            return style;
        }
        private void Typography()
        {
            if (text is null) return;
            text.FlowDirection = owner.FlowDirection; text.Language = owner.Language; text.Text.FontFamily = styleProbe.FontFamily; text.Text.FontWeight = styleProbe.FontWeight; text.Text.FontSize = styleProbe.FontSize; text.Text.CharacterSpacing = styleProbe.CharacterSpacing; text.Text.LineHeight = styleProbe.FontSize * (Role == "source" ? 20d / 14 : 1.5); text.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; text.InvalidateMeasure();
        }
        internal void MeasureBrand(double available)
        {
            if (brand is null) throw new InvalidOperationException("Only a brand link has brand intrinsic metrics.");
            brand.ApplyTemplate(); brand.Measure(new(double.PositiveInfinity, double.PositiveInfinity));
            if (brand.InlineMetricsReady) brand.PrepareInline(available);
            Root.Measure(new(available, double.PositiveInfinity));
        }
        internal void MeasureRow(double available)
        {
            var prepared = false;
            var blockHeight = 0d;
            if (text is not null && (Template is null || Descends(text, element)))
            {
                prepared = true;
                text.Measure(new(available, double.PositiveInfinity));
                blockHeight = text.DesiredSize.Height;
                Root.MinWidth = text.MinimumWidth;
                if (owner.LinkStyle is null)
                {
                    var height = blockHeight == 0 ? 0 : blockHeight - (Role == "source" ? 3 : 4);
                    if (height != recipeHeight)
                    {
                        recipeHeight = height; var style = RecipeStyle(height); Context.Style = style; styleProbe.Style = style;
                        if (Template is null) ((HyperlinkButton)element).Style = style;
                    }
                    var inlineAnchor = element is HyperlinkButton action && ReferenceEquals(action.Content, text) && action.Padding == new Thickness(0) && action.BorderThickness == new Thickness(0) && action.Margin == new Thickness(0);
                    Root.Width = inlineAnchor && element.ReadLocalValue(WidthProperty) == DependencyProperty.UnsetValue && double.IsNaN(element.Width) ? text.InlineContentWidth : double.NaN;
                }
                else Root.Width = double.NaN;
            }
            else { Root.Width = double.NaN; Root.MinWidth = 0; }
            Root.Measure(new(available, double.PositiveInfinity));
            // Parent measurement constrains DesiredSize to the inline hit area.
            // Retain the full CSS line-box height measured before that pass.
            BlockHeight = owner.LinkStyle is null && prepared ? Math.Max(blockHeight, Root.DesiredSize.Height) : Math.Max(24, Root.DesiredSize.Height);
        }
        private void Loaded(object sender, RoutedEventArgs args)
        {
            Typography(); Context.Notify(); Paint(false);
            if (brand is not null) owner.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { if (owner.IsLoaded && Context.Mounted) { Root.InvalidateMeasure(); owner.layout?.InvalidateMeasure(); owner.InvalidateMeasure(); } });
        }
        private void Entered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args) { hovering = true; Paint(true); }
        private void Exited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args) { if (new Rect(0, 0, Root.ActualWidth, Root.ActualHeight).Contains(args.GetCurrentPoint(Root).Position)) return; hovering = false; Paint(true); }
        internal void Paint(bool animate)
        {
            var normalBrush = owner.LinkStyle is null ? normal.Foreground : styleProbe.Foreground;
            var sources = new[] { normalBrush, hover.Foreground }.OfType<SolidColorBrush>().Distinct(ReferenceEqualityComparer.Instance).Cast<SolidColorBrush>().ToArray();
            if (owner.IsLoaded && Context.Mounted && !brushTokens.Select(row => row.Brush).SequenceEqual(sources, ReferenceEqualityComparer.Instance))
            {
                ClearBrushes(); foreach (var brush in sources) brushTokens.Add((brush, brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => Paint(true)), brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => Paint(true))));
            }
            if (normalBrush is not SolidColorBrush normalPaint || hover.Foreground is not SolidColorBrush hoverPaint) { Context.Foreground = normalBrush; if (text is not null) text.Text.Foreground = normalBrush; return; }
            var source = hovering && owner.IsEnabled && Role != "brand" ? hoverPaint : normalPaint; var color = source.Color;
            motion.Retarget(BleeckerSrgbColorMix.Premultiply((color.R, color.G, color.B, color.A / 255d * source.Opacity)), owner.clock.Elapsed.TotalMilliseconds, Role == "source" ? 190 : 220, animate && owner.IsLoaded && owner.settings.AnimationsEnabled);
            Context.Foreground = ink; if (text is not null) { text.Text.Foreground = ink; text.Text.TextDecorations = Role == "source" && hovering ? Windows.UI.Text.TextDecorations.Underline : Windows.UI.Text.TextDecorations.None; }
            Present(); if (Running) owner.EnsureMotion();
        }
        internal void Present() { var color = BleeckerSrgbColorMix.EncodePremultiplied(motion.ValueAt(owner.clock.Elapsed.TotalMilliseconds)); ink.Color = Windows.UI.Color.FromArgb(255, color.R, color.G, color.B); ink.Opacity = color.Alpha; }
        private void ClearBrushes() { foreach (var (brush, color, opacity) in brushTokens) { brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, color); brush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacity); } brushTokens.Clear(); }
        internal void Suspend() { hovering = false; ClearBrushes(); Paint(false); Context.Notify(); }
        internal void Retire()
        {
            Context.Mounted = false; Context.Notify(); hovering = false; ClearBrushes(); Root.PointerEntered -= Entered; Root.PointerExited -= Exited; Root.Loaded -= Loaded;
            foreach (var (property, token) in styleTokens) styleProbe.UnregisterPropertyChangedCallback(property, token); styleTokens.Clear(); owner.links.Remove(this);
        }
    }
}
