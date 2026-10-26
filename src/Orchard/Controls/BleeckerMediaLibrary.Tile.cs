using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerMediaLibrary
{
    private sealed class Tile : IDisposable
    {
        private readonly BleeckerMediaLibrary owner;
        internal readonly Grid Root = new() { Name = "MediaTile", UseLayoutRounding = false };
        internal readonly Button Open = new() { Name = "MediaOpen", UseLayoutRounding = false };
        private readonly Border chrome = new() { Name = "MediaTileChrome", UseLayoutRounding = false };
        private readonly Grid content = new() { UseLayoutRounding = false };
        private readonly Image image = new() { Name = "MediaTileImage", Stretch = Stretch.UniformToFill, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false, FlowDirection = FlowDirection.LeftToRight };
        private readonly TextBlock missing, filename;
        private readonly RichTextBlock failure;
        private readonly Microsoft.UI.Xaml.Shapes.Path failureGlyph;
        private readonly Run failureLabel = new();
        private readonly Border placeholder = new() { Name = "MediaNoPreview", IsHitTestVisible = false };
        private readonly Grid overlay = new() { Name = "MediaTileOverlay", VerticalAlignment = VerticalAlignment.Bottom, UseLayoutRounding = false };
        private readonly Border overlayPaint = new() { Name = "MediaOverlayPaint", IsHitTestVisible = false };
        private readonly TileBackdrop backdrop = new();
        private readonly Border overlayContent;
        private readonly StackPanel overlayStack = new() { UseLayoutRounding = false };
        private readonly BleeckerIconButton delete = new() { Name = "MediaDelete", Variant = BleeckerIconButtonVariant.Subtle, HorizontalAlignment = HorizontalAlignment.Right, UseLayoutRounding = false };
        private readonly Border focusRing = new() { Name = "MediaTileFocusRing", BorderThickness = new(2), Margin = new(-2), CornerRadius = new(9), IsHitTestVisible = false, Visibility = Visibility.Collapsed };
        private readonly BleeckerCssTransition<double> reveal = new((a, b, p) => a + (b - a) * p), hover = new((a, b, p) => a + (b - a) * p);
        private readonly BleeckerCssTransition<BleeckerCssColor> border = new(BleeckerCssColor.Mix);
        private readonly SolidColorBrush animatedBorder = new();
        private readonly TranslateTransform shift = new();
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private readonly UISettings settings = new();
        private readonly List<BleeckerCssShadow> shadows = [];
        private readonly Style openStyle, defaultTileStyle;
        private VisualStateGroup? group, focusGroup;
        private bool over, focused, subscribed, disposed, failed;
        internal BleeckerMediaLibraryItem Item { get; private set; } = null!;
        internal Tile(BleeckerMediaLibrary owner)
        {
            this.owner = owner;
            var styles = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/MediaLibrary.xaml") }; openStyle = (Style)styles["BleeckerMediaLibraryOpenStyle"]; defaultTileStyle = (Style)styles["BleeckerMediaLibraryTileStyle"]; Open.Style = openStyle;
            var secondary = (FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"];
            missing = Text("MediaNoPreviewText", secondary, 12, 16); missing.Text = "No preview"; missing.HorizontalAlignment = HorizontalAlignment.Center; missing.VerticalAlignment = VerticalAlignment.Center; missing.IsHitTestVisible = false; placeholder.Child = missing;
            failure = new() { Name = "MediaFailedImageText", FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"], FontSize = 16, LineHeight = 24, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false, IsHitTestVisible = false, VerticalAlignment = VerticalAlignment.Top, Visibility = Visibility.Collapsed };
            // A failed browser image retains a visible marker before its original alt text.
            failureGlyph = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Name='MediaFailedImageGlyph' Width='16' Height='16' Stretch='None' StrokeThickness='1' Data='M1.5,1.5 H14.5 V14.5 H1.5 Z M3.5,11 L6,8 L8,10 L10,7 L13,11 M10,5 A1,1 0 1 0 8,5 A1,1 0 1 0 10,5'/>");
            failureGlyph.IsHitTestVisible = false; failureGlyph.RenderTransform = new TranslateTransform { Y = -4 }; AutomationProperties.SetAccessibilityView(failureGlyph, AccessibilityView.Raw);
            var failureParagraph = new Paragraph(); failureParagraph.Inlines.Add(new InlineUIContainer { Child = failureGlyph }); failureParagraph.Inlines.Add(failureLabel); failure.Blocks.Add(failureParagraph);
            filename = Text("MediaTileFilename", (FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"], 12, 18); filename.FontWeight = Microsoft.UI.Text.FontWeights.Medium; filename.TextWrapping = TextWrapping.NoWrap; filename.TextTrimming = TextTrimming.CharacterEllipsis; filename.IsHitTestVisible = false;
            overlayContent = new() { Child = overlayStack, UseLayoutRounding = false }; overlayStack.Children.Add(filename); overlayStack.Children.Add(delete); overlay.Children.Add(backdrop.Host); overlay.Children.Add(overlayPaint); overlay.Children.Add(overlayContent); overlay.RenderTransform = shift;
            delete.Content = Icon("M3,6 L21,6 M19,6 L18,20 L6,20 L5,6 M9,6 L9,3 L15,3 L15,6 M10,10 L10,16 M14,10 L14,16", 16, "Delete"); AutomationProperties.SetName(delete, "Delete media item"); ToolTipService.SetToolTip(delete, "Delete"); delete.Click += DeleteClicked;
            content.Children.Add(image); content.Children.Add(placeholder); content.Children.Add(failure); content.Children.Add(Open); content.Children.Add(overlay); Canvas.SetZIndex(Open, 10); Canvas.SetZIndex(overlay, 20); chrome.Child = content;
            for (var i = 0; i < 2; i++) { var host = new Grid { Name = "MediaShadow" + i, IsHitTestVisible = false }; var mask = new Microsoft.UI.Xaml.Shapes.Path { Fill = new SolidColorBrush(Microsoft.UI.Colors.White) }; var exterior = new Microsoft.UI.Xaml.Shapes.Path { Fill = new SolidColorBrush(Microsoft.UI.Colors.White) }; var masks = new Canvas { Opacity = 0, IsHitTestVisible = false }; masks.Children.Add(mask); masks.Children.Add(exterior); Root.Children.Add(masks); Root.Children.Add(host); shadows.Add(new(owner, chrome, host, mask, exterior)); }
            Root.Children.Add(chrome); Root.Children.Add(focusRing);
            Root.PointerEntered += (_, _) => SetOver(true); Root.PointerExited += (_, _) => SetOver(false); Root.GotFocus += (_, _) => SetFocused(true); Root.LostFocus += (_, _) => SetFocused(FocusWithin());
            Root.SizeChanged += (_, _) => Present(); Root.Unloaded += (_, _) => StopFrames();
            Open.Click += OpenClicked; Open.Loaded += OpenLoaded;
            image.ImageOpened += (_, _) => { failed = false; UpdateImage(); }; image.ImageFailed += (_, _) => { failed = true; UpdateImage(); owner.Failed(Item); };
        }
        private void OpenLoaded(object sender, RoutedEventArgs args)
        {
            Open.ApplyTemplate(); if (group is not null) group.CurrentStateChanged -= StateChanged; if (focusGroup is not null) focusGroup.CurrentStateChanged -= FocusStateChanged;
            group = VisualTreeHelper.GetChildrenCount(Open) > 0 && VisualTreeHelper.GetChild(Open, 0) is FrameworkElement root ? VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(g => g.Name == "CommonStates") : null;
            if (group is not null) group.CurrentStateChanged += StateChanged;
            focusGroup = VisualTreeHelper.GetChildrenCount(Open) > 0 && VisualTreeHelper.GetChild(Open, 0) is FrameworkElement focusRoot ? VisualStateManager.GetVisualStateGroups(focusRoot).FirstOrDefault(g => g.Name == "FocusStates") : null;
            if (focusGroup is not null) focusGroup.CurrentStateChanged += FocusStateChanged;
        }
        private void StateChanged(object sender, VisualStateChangedEventArgs args) => SetOver(group?.CurrentState?.Name is "PointerOver" or "Pressed");
        private void FocusStateChanged(object sender, VisualStateChangedEventArgs args) => SetFocused(focusGroup?.CurrentState?.Name is "Focused" or "PointerFocused");
        private bool FocusWithin() { for (DependencyObject? node = owner.XamlRoot is { } root ? Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root) as DependencyObject : null; node is not null; node = VisualTreeHelper.GetParent(node)) if (ReferenceEquals(node, Root)) return true; return false; }
        private void OpenClicked(object sender, RoutedEventArgs args) { if (!disposed && Open.IsLoaded && owner.InputAllowed && owner.items.Any(i => ReferenceEquals(i, Item))) owner.OpenPreview(Item); }
        private void DeleteClicked(object sender, RoutedEventArgs args) { if (!disposed) owner.RequestDelete(this); }
        internal void SetOver(bool value) { over = value; Paint(); }
        internal void SetFocused(bool value) { focused = value; Paint(); }
        internal void Update(BleeckerMediaLibraryItem item, int index)
        {
            Item = item; Root.Tag = item; Open.Tag = delete.Tag = item; Open.TabIndex = index * 2; delete.TabIndex = index * 2 + 1; chrome.Style = owner.TileStyle ?? defaultTileStyle;
            AutomationProperties.SetName(Open, "Open " + item.Filename); AutomationProperties.SetPositionInSet(Open, index + 1); AutomationProperties.SetSizeOfSet(Open, owner.items.Count);
            var next = item.ThumbnailSource ?? item.ImageSource; if (!ReferenceEquals(image.Source, next)) { failed = false; image.Source = next; }
            filename.Text = item.Filename; var f = owner.RemScale; filename.Margin = new(0, 0, 0, 8 * f); missing.FontSize = 12 * f; missing.LineHeight = 16 * f; overlayContent.Padding = new(12 * f); delete.Width = delete.Height = delete.MinWidth = delete.MinHeight = 40 * f;
            foreach (var text in new[] { missing, filename }) { text.FlowDirection = owner.FlowDirection; text.Language = owner.Language; text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; }
            failure.FlowDirection = owner.FlowDirection; failure.Language = owner.Language; failure.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            overlay.FlowDirection = owner.FlowDirection; UpdateImage(); UpdateInput(); Paint();
        }
        private void UpdateImage()
        {
            if (Item is null) return; var label = string.IsNullOrEmpty(Item.Alt) ? Item.Filename : Item.Alt; AutomationProperties.SetName(image, label); failureLabel.Text = label;
            placeholder.Visibility = image.Source is null ? Visibility.Visible : Visibility.Collapsed; image.Visibility = image.Source is not null && !failed ? Visibility.Visible : Visibility.Collapsed; failure.Visibility = failed ? Visibility.Visible : Visibility.Collapsed;
        }
        internal void UpdateInput() { Open.IsEnabled = owner.InputAllowed; delete.Visibility = owner.HasDelete ? Visibility.Visible : Visibility.Collapsed; delete.IsEnabled = owner.InputAllowed && owner.DeleteCommand?.CanExecute(Item.Id) != false; }
        private void Paint()
        {
            if (disposed || Item is null || owner.palette.Count == 0) return;
            var now = clock.Elapsed.TotalMilliseconds; var animate = Root.IsLoaded && settings.AnimationsEnabled;
            reveal.Retarget(over || focused ? 1 : 0, now, 220, animate); hover.Retarget(over ? 1 : 0, now, 220, animate);
            var brush = owner.Brush(over ? "TileHoverBorder" : "TileBorder"); var color = brush.Color; border.Retarget(BleeckerCssColor.FromRgba((color.R, color.G, color.B, color.A / 255d * brush.Opacity), false), now, 220, animate);
            if (owner.TileStyle is null) chrome.BorderBrush = animatedBorder;
            missing.Foreground = owner.Brush("Secondary"); failure.Foreground = owner.Foreground; failureGlyph.Stroke = owner.Brush("Secondary"); placeholder.Background = owner.Brush("Placeholder"); overlayPaint.Background = owner.Brush("Overlay"); filename.Foreground = owner.Brush("OverlayText"); delete.Foreground = owner.Brush("Delete"); focusRing.BorderBrush = owner.Brush("Focus"); focusRing.Visibility = focused ? Visibility.Visible : Visibility.Collapsed;
            Present(); if (!subscribed && (reveal.IsRunning(now) || hover.IsRunning(now) || border.IsRunning(now))) { CompositionTarget.Rendering += Rendering; subscribed = true; }
        }
        private void Rendering(object? sender, object args) { Present(); var now = clock.Elapsed.TotalMilliseconds; if (!reveal.IsRunning(now) && !hover.IsRunning(now) && !border.IsRunning(now)) StopFrames(); }
        private void Present()
        {
            if (disposed || Item is null) return; var now = clock.Elapsed.TotalMilliseconds; var shown = reveal.ValueAt(now); overlay.Opacity = shown; shift.Y = 4 * owner.RemScale * (1 - shown);
            var encoded = border.ValueAt(now).Encode(); animatedBorder.Color = Windows.UI.Color.FromArgb(255, encoded.R, encoded.G, encoded.B); animatedBorder.Opacity = encoded.Alpha;
            var h = hover.ValueAt(now); var f = (float)owner.RemScale; shadows[0].Set(new((float)(2 + 3 * h) * f, (float)(1 + h) * f, 0, (float)(.025 + .02 * h), 26, 55, 77)); shadows[1].Set(new((float)(30 + 12 * h) * f, (float)(10 + 8 * h) * f, (float)(-18 - 4 * h) * f, (float)(.18 + .06 * h), 26, 55, 77));
        }
        private void StopFrames() { if (subscribed) CompositionTarget.Rendering -= Rendering; subscribed = false; }
        public void Dispose() { if (disposed) return; disposed = true; StopFrames(); if (group is not null) group.CurrentStateChanged -= StateChanged; if (focusGroup is not null) focusGroup.CurrentStateChanged -= FocusStateChanged; group = focusGroup = null; Open.Click -= OpenClicked; Open.Loaded -= OpenLoaded; delete.Click -= DeleteClicked; Open.IsEnabled = delete.IsEnabled = false; backdrop.Dispose(); foreach (var shadow in shadows) shadow.Dispose(); image.Source = null; if (Root.Parent is Panel parent) parent.Children.Remove(Root); }
    }
    private static Viewbox Icon(string data, double size, string kind)
    {
        var icon = (Viewbox)XamlReader.Load($"<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='{size.ToString(System.Globalization.CultureInfo.InvariantCulture)}' Height='{size.ToString(System.Globalization.CultureInfo.InvariantCulture)}' AutomationProperties.AccessibilityView='Raw'><Canvas Width='24' Height='24'><Path Data='{data}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
        icon.Name = "Media" + kind + "Icon";
        icon.Loaded += (_, _) => { var path = (Microsoft.UI.Xaml.Shapes.Path)((Canvas)icon.Child).Children[0]; for (DependencyObject? node = VisualTreeHelper.GetParent(icon); node is not null; node = VisualTreeHelper.GetParent(node)) if (node is ContentPresenter presenter) { path.SetBinding(Shape.StrokeProperty, new Microsoft.UI.Xaml.Data.Binding { Source = presenter, Path = new PropertyPath("Foreground") }); break; } };
        return icon;
    }
}
