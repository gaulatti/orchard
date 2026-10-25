using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerNavMenu
{
    private sealed class LinkSlot
    {
        private readonly BleeckerNavMenu owner;
        internal string Key { get; }
        internal DataTemplate? Template { get; }
        internal Grid Root { get; } = new() { UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        internal BleeckerNavLinkContext Context { get; }
        internal BleeckerContentText Text { get; }
        private readonly HyperlinkButton probe = new() { Visibility = Visibility.Collapsed, IsTabStop = false };
        private readonly FrameworkElement element;
        private readonly SolidColorBrush ink = new();
        private readonly BleeckerCssTransition<(double R, double G, double B, double Alpha)> motion = new(BleeckerSrgbColorMix.MixPremultiplied, BleeckerCssEasing.Default);
        private readonly List<(DependencyProperty Property, long Token)> tokens = [];
        private readonly List<(SolidColorBrush Brush, long Color, long Opacity)> paintTokens = [];
        internal bool Hovering { get; set; }
        internal bool Running => motion.IsRunning(owner.clock.Elapsed.TotalMilliseconds);
        internal LinkSlot(BleeckerNavMenu owner, string key, DataTemplate? template)
        {
            this.owner = owner; Key = key; Template = template; Context = new(owner);
            Text = new("NavLinkText", "BleeckerPrimaryMediumFontFamily", 16, 24, "BleeckerTextPrimaryBrush", true);
            Context.Content = Text; AutomationProperties.SetAccessibilityView(probe, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw); Root.Children.Add(probe);
            if (template is null)
            {
                var link = new HyperlinkButton { Name = "NavLink", Content = Text, Command = Context.Command, Tag = Context };
                link.SetBinding(Control.ForegroundProperty, new Binding { Source = Context, Path = new PropertyPath(nameof(Context.Foreground)) }); element = link;
            }
            else element = template.LoadContent() as FrameworkElement ?? throw new InvalidOperationException("Nav Menu LinkTemplate must create a FrameworkElement.");
            Context.Element = element; element.DataContext = Context; Root.Children.Add(element);
            Root.PointerEntered += Entered; Root.PointerExited += Exited;
            Root.Loaded += (_, _) => { Context.Notify(); UpdateText(); Paint(false); };
            foreach (var property in new[] { Control.FontFamilyProperty, Control.FontSizeProperty, Control.FontWeightProperty, Control.CharacterSpacingProperty, Control.ForegroundProperty }) tokens.Add((property, probe.RegisterPropertyChangedCallback(property, (_, _) => { UpdateText(); Paint(true); })));
        }
        internal void Update(BleeckerNavItem item, Style style)
        {
            Context.Item = item; Context.Label = item.Label; Context.Href = item.Href; Context.External = item.External; Context.Style = style;
            probe.Style = style;
            if (Template is null)
            {
                ((HyperlinkButton)element).Style = style; AutomationProperties.SetName(element, item.Label);
            }
            element.FlowDirection = Text.FlowDirection = owner.FlowDirection; element.Language = Text.Language = owner.Language;
            Text.Set(item.Label, null); UpdateText(); Paint(false); Root.InvalidateMeasure();
        }
        private void UpdateText()
        {
            Text.Text.FontFamily = probe.FontFamily; Text.Text.FontSize = probe.FontSize; Text.Text.FontWeight = probe.FontWeight; Text.Text.CharacterSpacing = probe.CharacterSpacing;
            Text.Text.LineHeight = probe.FontSize * (owner.Direction == BleeckerNavDirection.Horizontal ? 1.5 : 28d / 18);
            Text.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; Text.InvalidateMeasure();
        }
        private void Entered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args) { Hovering = true; Paint(true); }
        private void Exited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args)
        {
            if (new Rect(0, 0, Root.ActualWidth, Root.ActualHeight).Contains(args.GetCurrentPoint(Root).Position)) return;
            Hovering = false; Paint(true);
        }
        internal void Paint(bool animate)
        {
            var sources = new[] { probe.Foreground, owner.hoverProbe?.Foreground }.OfType<SolidColorBrush>().Distinct(ReferenceEqualityComparer.Instance).Cast<SolidColorBrush>().ToArray();
            if (owner.IsLoaded && Context.Mounted && !paintTokens.Select(row => row.Brush).SequenceEqual(sources, ReferenceEqualityComparer.Instance))
            {
                StopObservingPaint();
                foreach (var brush in sources) paintTokens.Add((brush, brush.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => Paint(true)), brush.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => Paint(true))));
            }
            if (probe.Foreground is not SolidColorBrush normal || owner.hoverProbe?.Foreground is not SolidColorBrush hover)
            {
                // An explicit consumer brush remains authoritative; only source
                // solid colors participate in the CSS color transition.
                Context.Foreground = Text.Text.Foreground = probe.Foreground; return;
            }
            var source = Hovering && owner.IsEnabled ? hover : normal; var color = source.Color;
            motion.Retarget(BleeckerSrgbColorMix.Premultiply((color.R, color.G, color.B, color.A / 255d * source.Opacity)), owner.clock.Elapsed.TotalMilliseconds, 220, animate && owner.IsLoaded && owner.settings.AnimationsEnabled);
            Context.Foreground = Text.Text.Foreground = ink; Present(); if (Running) owner.EnsureMotion();
        }
        internal void Present()
        {
            var color = BleeckerSrgbColorMix.EncodePremultiplied(motion.ValueAt(owner.clock.Elapsed.TotalMilliseconds));
            ink.Color = Windows.UI.Color.FromArgb((byte)Math.Round(color.Alpha * 255), color.R, color.G, color.B);
        }
        internal double MinimumWidth
        {
            get
            {
                if (double.IsFinite(element.Width)) return Math.Max(element.MinWidth, element.Width);
                return Math.Max(element.MinWidth, Template is null || DescendsFrom(Text, element) ? Text.MinimumWidth : 0);
            }
        }
        internal void Retire()
        {
            Context.Mounted = false; Context.Notify(); Hovering = false;
            StopObservingPaint();
            Root.PointerEntered -= Entered; Root.PointerExited -= Exited;
            foreach (var (property, token) in tokens) probe.UnregisterPropertyChangedCallback(property, token); tokens.Clear();
        }
        internal void StopObservingPaint() { foreach (var (brush, color, opacity) in paintTokens) { brush.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, color); brush.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacity); } paintTokens.Clear(); }
    }
    private sealed partial class LinkPanel(BleeckerNavMenu owner) : Panel
    {
        private readonly List<Rect> bounds = [];
        private LinkSlot[] measured = [];
        protected override Size MeasureOverride(Size available)
        {
            bounds.Clear(); measured = owner.slots.ToArray(); var natural = new List<double>(); var minimum = new List<double>();
            foreach (var slot in measured) { slot.Root.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); natural.Add(slot.Root.DesiredSize.Width); minimum.Add(slot.MinimumWidth); }
            var horizontal = owner.Direction == BleeckerNavDirection.Horizontal;
            var width = double.IsFinite(available.Width) ? available.Width : horizontal ? natural.Sum() + Math.Max(0, natural.Count - 1) * 32 : natural.DefaultIfEmpty(0).Max();
            var widths = horizontal ? BleeckerNavMenuModel.Widths(natural, minimum, width) : Enumerable.Repeat(width, natural.Count).ToArray();
            var maximum = 0d; var top = 0d; var left = 0d;
            for (var index = 0; index < measured.Length; index++)
            {
                var slot = measured[index]; slot.Root.Measure(new(widths[index], double.PositiveInfinity)); var height = slot.Root.DesiredSize.Height;
                bounds.Add(new(left, top, widths[index], height)); maximum = Math.Max(maximum, height);
                if (horizontal) left += widths[index] + 32; else top += height + 16;
            }
            if (horizontal) for (var index = 0; index < bounds.Count; index++) { var box = bounds[index]; box.Y = (maximum - box.Height) / 2; if (owner.FlowDirection == FlowDirection.RightToLeft) box.X = width - box.X - box.Width; bounds[index] = box; }
            return new(width, horizontal ? maximum : measured.Length == 0 ? 0 : top - 16);
        }
        protected override Size ArrangeOverride(Size final) { for (var index = 0; index < measured.Length; index++) measured[index].Root.Arrange(bounds[index]); return final; }
    }
}
