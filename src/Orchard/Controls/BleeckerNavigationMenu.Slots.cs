using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerNavigationMenu
{
    private static void ScalePadding(Control control, double scale)
    {
        control.ClearValue(Control.PaddingProperty);
        if (scale == 1) return;
        var value = control.Padding;
        control.Padding = new(value.Left * scale, value.Top * scale, value.Right * scale, value.Bottom * scale);
    }

    private static void ScaleLink(HyperlinkButton link, double scale)
    {
        link.ClearValue(Control.FontSizeProperty);
        var fontSize = link.FontSize;
        if (scale != 1) link.FontSize = fontSize * scale;
        ScalePadding(link, scale);
    }

    private static void ScaleSurface(BleeckerOverlaySurface surface, double scale)
    {
        surface.ClearValue(FrameworkElement.WidthProperty);
        surface.ClearValue(FrameworkElement.MinWidthProperty);
        var width = surface.Width;
        var minWidth = surface.MinWidth;
        if (scale != 1) { surface.Width = width * scale; surface.MinWidth = minWidth * scale; }
        ScalePadding(surface, scale);
    }

    private sealed partial class EntrySlot : IDisposable
    {
        private readonly BleeckerNavigationMenu owner;
        private IBleeckerNavigationMenuEntry? entry;
        private LinkSlot? direct;
        private TriggerButton? trigger;
        private BleeckerContentText? title;
        private FrameworkElement? chevron;
        private readonly Dictionary<string, LinkSlot> rows = [];
        private readonly StackPanel items = new() { UseLayoutRounding = false };
        private BleeckerOverlaySurface? surface;
        private readonly DispatcherQueueTimer enter, exit;
        private bool open, mounted, hovered, disposed;
        private long foregroundToken;
        internal EntrySlot(BleeckerNavigationMenu owner)
        {
            this.owner = owner;
            InitializeMotion();
            enter = owner.DispatcherQueue.CreateTimer(); enter.Interval = TimeSpan.FromMilliseconds(20); enter.IsRepeating = false;
            enter.Tick += (_, _) => { if (!disposed && open && surface is not null) BeginSurfaceMotion(true); };
            exit = owner.DispatcherQueue.CreateTimer(); exit.Interval = TimeSpan.FromMilliseconds(150); exit.IsRepeating = false;
            exit.Tick += (_, _) => { if (!disposed && !open) Unmount(); };
        }
        internal Grid Root { get; } = new() { UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        internal int Index { get; private set; }
        internal IEnumerable<LinkSlot> Links => direct is null ? rows.Values : [direct];
        internal double MinimumWidth => direct?.MinimumWidth ?? (title?.MinimumWidth ?? 0) + 14 + 4 * owner.RemScale + (trigger?.Padding.Left ?? 0) + (trigger?.Padding.Right ?? 0);
        internal void Update(IBleeckerNavigationMenuEntry value, int index)
        {
            entry = value; Index = index;
            if (value is BleeckerNavigationMenuLink link)
            {
                if (direct is null) { direct = new(owner, false); Root.Children.Add(direct.Root); }
                direct.Update(link); direct.Mount(); return;
            }
            var group = (BleeckerNavigationMenuGroup)value;
            if (trigger is null)
            {
                trigger = new(owner) { Name = "NavigationMenuTrigger", Request = () => owner.Toggle(this), ExpandRequest = () => owner.Expand(this), CollapseRequest = owner.Close };
                title = new("NavigationTriggerTitle", "BleeckerPrimaryMediumFontFamily", 13, 19.5, "BleeckerTextSecondaryBrush", true);
                chevron = (FrameworkElement)Microsoft.UI.Xaml.Markup.XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='14' Height='14' RenderTransformOrigin='.5,.5' AutomationProperties.AccessibilityView='Raw'><Viewbox.RenderTransform><RotateTransform/></Viewbox.RenderTransform><Canvas Width='24' Height='24'><Path Data='M6,9 L12,15 L18,9' Stroke='{ThemeResource BleeckerTextPrimaryBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
                var content = new Grid { ColumnSpacing = 4, UseLayoutRounding = false };
                content.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); content.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
                content.Children.Add(title); Grid.SetColumn(chevron, 1); content.Children.Add(chevron); trigger.Content = content; Root.Children.Add(trigger);
                trigger.PointerEntered += (_, _) => { hovered = true; Paint(); }; trigger.PointerExited += (_, _) => { hovered = false; Paint(); };
                trigger.Loaded += (_, _) => Paint();
                foregroundToken = trigger.RegisterPropertyChangedCallback(Control.ForegroundProperty, (_, _) => Paint());
            }
            trigger.DataContext = group; trigger.Label = group.Trigger; trigger.Style = owner.TriggerStyle; trigger.FlowDirection = owner.FlowDirection; trigger.Language = owner.Language;
            ScalePadding(trigger, owner.RemScale);
            ((Grid)trigger.Content).ColumnSpacing = 4 * owner.RemScale;
            title!.FlowDirection = owner.FlowDirection; title.Language = owner.Language; title.Set(group.Trigger, null); title.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            title.Text.FontFamily = trigger.FontFamily; title.Text.FontSize = trigger.FontSize; title.Text.FontWeight = trigger.FontWeight; title.Text.LineHeight = trigger.FontSize * 1.5;
            var values = group.Items.ToArray(); var wanted = new HashSet<string>(StringComparer.Ordinal);
            foreach (var child in values) { ArgumentNullException.ThrowIfNull(child); Validate(child); if (!wanted.Add(child.Href)) throw new ArgumentException("Navigation Menu group link href keys must be unique."); }
            foreach (var key in rows.Keys.Where(k => !wanted.Contains(k)).ToArray()) { rows[key].Dispose(); rows.Remove(key); }
            for (var i = 0; i < values.Length; i++)
            {
                var child = values[i]; if (!rows.TryGetValue(child.Href, out var row)) { row = new(owner, true); rows.Add(child.Href, row); }
                row.Update(child); if (mounted) row.Mount();
                var current = items.Children.IndexOf(row.Root); if (current != i) { if (current >= 0) items.Children.RemoveAt(current); items.Children.Insert(i, row.Root); }
            }
            if (surface is not null) { surface.Style = owner.SurfaceStyle; surface.FlowDirection = owner.FlowDirection; ScaleSurface(surface, owner.RemScale); }
            Paint();
        }
        internal void SetOpen(bool value)
        {
            value &= entry is BleeckerNavigationMenuGroup;
            if (disposed || value == open) return; open = value;
            if (trigger is not null) trigger.Expanded = value; Paint();
            if (value)
            {
                exit.Stop(); owner.EnsurePresentation();
                if (surface is null)
                {
                    surface = new BleeckerOverlaySurface { Name = "NavigationMenuSurface", Content = items, IsTabStop = false, UseLayoutRounding = false };
                    motionRoot.Children.Add(surface);
                }
                surface.Style = owner.SurfaceStyle; surface.FlowDirection = owner.FlowDirection; surface.RequestedTheme = owner.ActualTheme;
                ScaleSurface(surface, owner.RemScale);
                if (!mounted) { owner.plane!.Children.Add(motionRoot); mounted = true; foreach (var row in rows.Values) row.Mount(); }
                enter.Start();
            }
            else { enter.Stop(); if (surface is not null) BeginSurfaceMotion(false); exit.Start(); }
            Notify();
        }
        private void Unmount()
        {
            enter.Stop(); exit.Stop(); StopSurfaceMotion(); if (motionRoot.Parent is Panel parent) parent.Children.Remove(motionRoot); mounted = false;
            RetargetSurfaceMotion(false, motionClock.Elapsed.TotalMilliseconds, false);
            foreach (var row in rows.Values) row.Unmount();
        }
        internal void RetireSurface() { open = false; if (trigger is not null) trigger.Expanded = false; Unmount(); Paint(); }
        internal void Position(UIElement target)
        {
            if (!mounted || surface is null || disposed) return;
            surface.RequestedTheme = owner.ActualTheme;
            // The wrapper anchors the popup; the button's pressed translation never moves it.
            var box = Root.TransformToVisual(target).TransformBounds(new Rect(0, 0, Root.ActualWidth, Root.ActualHeight));
            if (double.IsNaN(Canvas.GetLeft(motionRoot)) || Math.Abs(Canvas.GetLeft(motionRoot) - box.X) > .000001) Canvas.SetLeft(motionRoot, box.X);
            var gap = 4 * owner.RemScale;
            if (double.IsNaN(Canvas.GetTop(motionRoot)) || Math.Abs(Canvas.GetTop(motionRoot) - box.Bottom - gap) > .000001) Canvas.SetTop(motionRoot, box.Bottom + gap);
            PaintSurfaceMotion(motionClock.Elapsed.TotalMilliseconds);
        }
        internal void Paint()
        {
            if (title is not null && trigger is not null)
            {
                var ink = (open || hovered) && owner.IsEnabled ? owner.primaryProbe?.Foreground ?? trigger.Foreground : trigger.Foreground;
                title.Text.Foreground = ink; foreach (var path in Descendants(chevron).OfType<Microsoft.UI.Xaml.Shapes.Path>()) path.Stroke = ink;
                if (chevron?.RenderTransform is RotateTransform rotate) rotate.Angle = open ? 180 : 0;
                trigger.SetOpenState(open);
            }
            foreach (var row in Links) row.Paint();
        }
        internal void Notify() { foreach (var row in Links) row.Context.Notify(); if (trigger is not null) FrameworkElementAutomationPeer.FromElement(trigger)?.InvalidatePeer(); }
        public void Dispose()
        {
            if (disposed) return; disposed = true; RetireSurface(); direct?.Dispose(); direct = null; foreach (var row in rows.Values) row.Dispose(); rows.Clear(); items.Children.Clear();
            if (surface is not null) surface.Content = null; motionRoot.Children.Clear(); surface = null;
            if (trigger is not null) { trigger.UnregisterPropertyChangedCallback(Control.ForegroundProperty, foregroundToken); trigger.Request = trigger.ExpandRequest = trigger.CollapseRequest = null; trigger.Content = null; } Root.Children.Clear(); if (Root.Parent is Panel parent) parent.Children.Remove(Root);
        }
    }
    private static void Validate(BleeckerNavigationMenuLink item) { if (item.Href is null || item.Label is null) throw new ArgumentException("Navigation Menu links require original non-null href and label values."); }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject? root)
    { if (root is null) yield break; yield return root; for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child; }

    private sealed class LinkSlot : IDisposable
    {
        private readonly BleeckerNavigationMenu owner;
        private readonly bool grouped;
        private readonly HyperlinkButton probe = new() { Visibility = Visibility.Collapsed, IsTabStop = false };
        private readonly BleeckerContentText title, description;
        private readonly StackPanel content = new() { Spacing = 2, UseLayoutRounding = false };
        private FrameworkElement? element;
        private DataTemplate? template;
        private bool hovered, disposed;
        private BleeckerNavigationMenuLink? item;
        private readonly long foregroundToken;
        internal LinkSlot(BleeckerNavigationMenu owner, bool grouped)
        {
            this.owner = owner; this.grouped = grouped; Context = new(owner) { IsGrouped = grouped };
            title = new("NavigationLinkTitle", "BleeckerPrimaryMediumFontFamily", 14, 20, grouped ? "BleeckerTextPrimaryBrush" : "BleeckerTextSecondaryBrush", true);
            description = new("NavigationLinkDescription", "BleeckerPrimaryFontFamily", 12, 19.5, "BleeckerTextSecondaryBrush");
            content.Children.Add(title); if (grouped) content.Children.Add(description);
            Context.Content = content; Root.Children.Add(probe); AutomationProperties.SetAccessibilityView(probe, AccessibilityView.Raw);
            Root.PointerEntered += (_, _) => { hovered = true; Paint(); }; Root.PointerExited += (_, _) => { hovered = false; Paint(); };
            Root.Loaded += (_, _) => { Paint(); Context.Notify(); };
            foregroundToken = probe.RegisterPropertyChangedCallback(Control.ForegroundProperty, (_, _) => Paint());
        }
        internal Grid Root { get; } = new() { UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        internal BleeckerNavigationLinkContext Context { get; private set; }
        internal double MinimumWidth => element is null ? 0 : double.IsFinite(element.Width) ? Math.Max(element.MinWidth, element.Width) : Math.Max(element.MinWidth, (template is null ? title.MinimumWidth + probe.Padding.Left + probe.Padding.Right : 0));
        internal void Update(BleeckerNavigationMenuLink value)
        {
            Validate(value); item = value;
            if (!ReferenceEquals(template, owner.LinkTemplate)) { ResetElement(); template = owner.LinkTemplate; }
            Context.Item = value; Context.Style = (grouped ? owner.GroupLinkStyle : owner.TopLinkStyle) ?? throw new InvalidOperationException("Navigation Menu requires its default or caller link style.");
            Context.Content = content; Context.IsGrouped = grouped; probe.Style = Context.Style; ScaleLink(probe, owner.RemScale);
            title.Set(value.Label, null); description.Set(value.Description, null); description.Visibility = string.IsNullOrEmpty(value.Description) ? Visibility.Collapsed : Visibility.Visible;
            foreach (var text in new[] { title, description }) { text.FlowDirection = owner.FlowDirection; text.Language = owner.Language; text.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; }
            content.Spacing = 2 * owner.RemScale;
            if (grouped)
            {
                title.Text.FontSize = 14 * owner.RemScale; title.Text.LineHeight = 20 * owner.RemScale;
                description.Text.FontSize = 12 * owner.RemScale; description.Text.LineHeight = 19.5 * owner.RemScale;
            }
            if (!grouped) { title.Text.FontFamily = probe.FontFamily; title.Text.FontSize = probe.FontSize; title.Text.FontWeight = probe.FontWeight; title.Text.LineHeight = probe.FontSize * (20d / 14); }
            if (element is not null) UpdateElement(); Paint(); Root.InvalidateMeasure();
        }
        internal void Mount()
        {
            if (disposed) return;
            if (element is null)
            {
                element = template is null ? new LinkButton(Context) { Name = grouped ? "NavigationGroupedLink" : "NavigationDirectLink", Content = content, Command = Context.Command } : template.LoadContent() as FrameworkElement ?? throw new InvalidOperationException("Navigation Menu LinkTemplate must create a FrameworkElement.");
                Context.Element = element; element.DataContext = Context;
                element.Loaded += ElementLoaded;
                if (template is null) ((HyperlinkButton)element).SetBinding(Control.ForegroundProperty, new Binding { Source = Context, Path = new PropertyPath(nameof(Context.Foreground)) });
                Root.Children.Add(element); UpdateElement();
            }
            Context.Mounted = true; Context.Notify(); Paint();
        }
        private void UpdateElement()
        {
            if (element is null) return; element.FlowDirection = owner.FlowDirection; element.Language = owner.Language;
            if (template is null) { var link = (HyperlinkButton)element; link.Style = Context.Style; ScaleLink(link, owner.RemScale); AutomationProperties.SetName(element, item!.Label); AutomationProperties.SetHelpText(element, grouped ? item.Description ?? "" : ""); }
        }
        private void ElementLoaded(object sender, RoutedEventArgs args) => Context.Notify();
        internal void Paint()
        {
            Context.Foreground = !grouped && hovered && owner.IsEnabled ? owner.primaryProbe?.Foreground ?? probe.Foreground : probe.Foreground;
            if (!grouped) title.Text.Foreground = Context.Foreground;
        }
        private void ResetElement()
        {
            Context.Mounted = false; Context.Notify(); Context.SetValue(BleeckerNavigationLinkContext.ContentProperty, null); if (element is HyperlinkButton button && template is null) { button.Command = null; button.Content = null; }
            if (element is not null) { element.Loaded -= ElementLoaded; Root.Children.Remove(element); element.DataContext = null; } element = null; Context.Element = null; hovered = false;
            Context = new(owner) { IsGrouped = grouped, Content = content }; if (item is not null) Context.Item = item;
        }
        internal void Unmount() { ResetElement(); if (item is not null) Update(item); }
        public void Dispose() { if (disposed) return; disposed = true; probe.UnregisterPropertyChangedCallback(Control.ForegroundProperty, foregroundToken); ResetElement(); Context.Mounted = false; Root.Children.Clear(); if (Root.Parent is Panel parent) parent.Children.Remove(Root); }
    }

    private sealed partial class EntryPanel(BleeckerNavigationMenu owner) : Panel
    {
        private readonly List<Rect> boxes = [];
        private EntrySlot[] measured = [];
        protected override Size MeasureOverride(Size available)
        {
            measured = owner.entries.Select(e => owner.slots[Key(e)]).ToArray(); boxes.Clear(); var natural = new List<double>(); var minimum = new List<double>();
            foreach (var slot in measured) { slot.Root.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); natural.Add(slot.Root.DesiredSize.Width); minimum.Add(slot.MinimumWidth); }
            var gap = 4 * owner.RemScale;
            var width = double.IsFinite(available.Width) ? available.Width : natural.Sum() + Math.Max(0, measured.Length - 1) * gap;
            // The existing flex solver subtracts a 32px gap. Supply equivalent room for this source's rem gap.
            var widths = BleeckerNavMenuModel.Widths(natural, minimum, width + Math.Max(0, measured.Length - 1) * (32 - gap));
            var left = 0d; var height = 0d;
            for (var i = 0; i < measured.Length; i++) { measured[i].Root.Measure(new(widths[i], double.PositiveInfinity)); var h = measured[i].Root.DesiredSize.Height; boxes.Add(new(left, 0, widths[i], h)); height = Math.Max(height, h); left += widths[i] + gap; }
            for (var i = 0; i < boxes.Count; i++) { var b = boxes[i]; b.Y = (height - b.Height) / 2; if (owner.FlowDirection == FlowDirection.RightToLeft) b.X = width - b.X - b.Width; boxes[i] = b; }
            return new(width, height);
        }
        protected override Size ArrangeOverride(Size final) { for (var i = 0; i < measured.Length; i++) measured[i].Root.Arrange(boxes[i]); return final; }
    }
    private sealed class LinkButton(BleeckerNavigationLinkContext context) : HyperlinkButton
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this, context);
        private sealed class Peer(LinkButton button, BleeckerNavigationLinkContext context) : FrameworkElementAutomationPeer(button), IInvokeProvider
        {
            protected override string GetClassNameCore() => "BleeckerNavigationLink";
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;
            protected override bool IsEnabledCore() => button.IsLoaded && button.IsEnabled && context.Command.CanExecute(null);
            protected override object? GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Invoke ? this : base.GetPatternCore(pattern);
            public void Invoke() { if (!IsEnabledCore()) throw new ElementNotEnabledException(); context.Command.Execute(null); }
        }
    }
    private sealed class TriggerButton(BleeckerNavigationMenu owner) : Button
    {
        internal Action? Request, ExpandRequest, CollapseRequest;
        internal string Label = "";
        internal bool Expanded;
        private bool wired;
        internal void SetOpenState(bool value) { if (!wired) { Click += (_, _) => Request?.Invoke(); Loaded += (_, _) => SetOpenState(Expanded); wired = true; } VisualStateManager.GoToState(this, value ? "MenuOpen" : "MenuClosed", false); }
        protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this, owner);
        private sealed class Peer(TriggerButton button, BleeckerNavigationMenu owner) : FrameworkElementAutomationPeer(button), IInvokeProvider, IExpandCollapseProvider
        {
            protected override string GetClassNameCore() => "BleeckerNavigationTrigger";
            protected override string GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(button)) ? button.Label : AutomationProperties.GetName(button);
            protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
            protected override bool IsEnabledCore() => button.IsLoaded && button.IsEnabled && owner.InputAllowed;
            protected override object? GetPatternCore(PatternInterface pattern) => pattern is PatternInterface.Invoke or PatternInterface.ExpandCollapse ? this : base.GetPatternCore(pattern);
            private void Require() { if (!IsEnabledCore()) throw new ElementNotEnabledException(); }
            public void Invoke() { Require(); button.Request?.Invoke(); }
            public ExpandCollapseState ExpandCollapseState => button.Expanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
            public void Expand() { Require(); button.ExpandRequest?.Invoke(); }
            public void Collapse() { Require(); button.CollapseRequest?.Invoke(); }
        }
    }
}
