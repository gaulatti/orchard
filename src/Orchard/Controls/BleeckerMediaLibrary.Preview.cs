using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerMediaLibrary
{
    private sealed class Preview : StackPanel, IDisposable
    {
        private readonly BleeckerMediaLibrary owner;
        private readonly ImageFrame image;
        private readonly Border imageBorder, bottomBorder;
        private readonly StackPanel metadata = new() { Name = "MediaPreviewMetadata", UseLayoutRounding = false }, alt = new() { Name = "MediaPreviewAlt", UseLayoutRounding = false }, url = new() { Name = "MediaPreviewUrlSection", UseLayoutRounding = false }, created = new() { Name = "MediaPreviewCreated", UseLayoutRounding = false };
        private readonly TextBlock filename, altLabel, altValue, urlLabel, urlValue, createdLabel, createdValue;
        private readonly BleeckerIconButton copyFilename = new() { Name = "MediaCopyFilename", Variant = BleeckerIconButtonVariant.Ghost, UseLayoutRounding = false }, copyUrl = new() { Name = "MediaCopyUrl", Variant = BleeckerIconButtonVariant.Ghost, Size = BleeckerIconButtonSize.Small, UseLayoutRounding = false };
        private readonly HyperlinkButton link = new() { Name = "MediaUrlLink", Padding = new(0), MinWidth = 0, MinHeight = 0, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch, UseLayoutRounding = false };
        private readonly Grid nameRow = new() { UseLayoutRounding = false }, urlRow = new() { UseLayoutRounding = false }, bottom = new() { Name = "MediaPreviewBottom", UseLayoutRounding = false };
        internal Preview(BleeckerMediaLibrary owner)
        {
            this.owner = owner; Name = "MediaPreviewContent"; UseLayoutRounding = false; HorizontalAlignment = HorizontalAlignment.Stretch; Loaded += (_, _) => UpdateInput();
            var primary = (FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"]; var regular = (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"]; var secondary = (FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"];
            filename = Text("MediaPreviewFilename", primary, 20, 28, true); filename.CharacterSpacing = -20;
            altLabel = Text("MediaAltLabel", primary, 10, 15, true); altLabel.Text = "ALT TEXT"; altLabel.CharacterSpacing = 90; altValue = Text("MediaAltValue", secondary, 14, 24);
            urlLabel = Text("MediaUrlLabel", primary, 10, 15, true); urlLabel.Text = "URL"; urlLabel.CharacterSpacing = 90; urlValue = Text("MediaUrlValue", regular, 14, 20);
            createdLabel = Text("MediaCreatedLabel", primary, 10, 15, true); createdLabel.Text = "CREATED"; createdLabel.CharacterSpacing = 90; createdValue = Text("MediaCreatedValue", secondary, 14, 20);
            copyFilename.Content = Icon("M9,9 L21,9 L21,21 L9,21 Z M15,5 L15,3 L3,3 L3,15 L5,15", 18, "CopyFilename"); copyUrl.Content = Icon("M9,9 L21,9 L21,21 L9,21 Z M15,5 L15,3 L3,3 L3,15 L5,15", 14, "CopyUrl");
            AutomationProperties.SetName(copyFilename, "Copy filename"); AutomationProperties.SetName(copyUrl, "Copy URL"); ToolTipService.SetToolTip(copyFilename, "Copy filename"); ToolTipService.SetToolTip(copyUrl, "Copy URL");
            copyFilename.Click += CopyFilename; copyUrl.Click += CopyUrl; link.Click += RouteUrl;
            nameRow.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); nameRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); nameRow.Children.Add(filename); nameRow.Children.Add(copyFilename); Grid.SetColumn(copyFilename, 1); filename.VerticalAlignment = VerticalAlignment.Center;
            urlRow.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); urlRow.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); link.Content = urlValue; urlRow.Children.Add(link); urlRow.Children.Add(copyUrl); Grid.SetColumn(copyUrl, 1); link.VerticalAlignment = VerticalAlignment.Center;
            alt.Children.Add(altLabel); alt.Children.Add(altValue); url.Children.Add(urlLabel); url.Children.Add(urlRow); created.Children.Add(createdLabel); created.Children.Add(createdValue); bottom.Children.Add(url); bottom.Children.Add(created);
            bottomBorder = new() { Child = bottom, BorderThickness = new(0, 1, 0, 0), UseLayoutRounding = false };
            metadata.Children.Add(nameRow); metadata.Children.Add(alt); metadata.Children.Add(bottomBorder);
            image = new(owner); imageBorder = new() { Name = "MediaPreviewImageFrame", Child = image, BorderThickness = new(1), CornerRadius = new(7), UseLayoutRounding = false };
            Children.Add(imageBorder); Children.Add(metadata);
        }
        private void CopyFilename(object sender, RoutedEventArgs args) { if (owner.selected is { } item) owner.RequestCopy(item.Filename); }
        private void CopyUrl(object sender, RoutedEventArgs args) { if (owner.selected is { Url: { Length: > 0 } value }) owner.RequestCopy(value); }
        private void RouteUrl(object sender, RoutedEventArgs args) { if (owner.PreviewInputAllowed && owner.selected?.Url is { } value && owner.UrlCommand is { } command && command.CanExecute(value)) command.Execute(value); }
        internal void Update()
        {
            var item = owner.selected; Visibility = item is null ? Visibility.Collapsed : Visibility.Visible; var f = owner.RemScale; FlowDirection = owner.FlowDirection; Language = owner.Language; Spacing = 24 * f; metadata.Spacing = 20 * f;
            filename.Text = item?.Filename ?? ""; filename.FontSize = 20 * f; filename.LineHeight = 28 * f; filename.Foreground = owner.Foreground;
            copyFilename.Width = copyFilename.Height = copyFilename.MinWidth = copyFilename.MinHeight = 40 * f; copyFilename.Margin = new(8 * f, 0, 0, 0); copyUrl.Width = copyUrl.Height = copyUrl.MinWidth = copyUrl.MinHeight = 32 * f; copyUrl.Margin = new(8 * f, 0, 0, 0);
            alt.Visibility = string.IsNullOrEmpty(item?.Alt) ? Visibility.Collapsed : Visibility.Visible; altValue.Text = item?.Alt ?? ""; altValue.FontSize = 14 * f; altValue.LineHeight = 24 * f; altValue.Margin = new(0, 4 * f, 0, 0); altValue.Foreground = owner.Foreground;
            url.Visibility = string.IsNullOrEmpty(item?.Url) ? Visibility.Collapsed : Visibility.Visible; urlValue.Text = item?.Url ?? ""; urlValue.FontSize = 14 * f; urlValue.LineHeight = 20 * f; urlValue.Foreground = owner.Brush("Link"); urlLabel.Margin = new(0, 0, 0, 6 * f); AutomationProperties.SetName(link, item?.Url ?? "");
            link.NavigateUri = string.IsNullOrEmpty(item?.Url) || owner.UrlCommand is not null ? null : new Uri(item.Url, UriKind.Absolute);
            created.Visibility = string.IsNullOrEmpty(item?.CreatedAt) ? Visibility.Collapsed : Visibility.Visible; createdValue.Text = string.IsNullOrEmpty(item?.CreatedAt) ? "" : owner.FormatDate(item.CreatedAt); createdValue.FontSize = 14 * f; createdValue.LineHeight = 20 * f; createdValue.Foreground = owner.Foreground; createdLabel.Margin = new(0, 0, 0, 6 * f);
            foreach (var label in new[] { altLabel, urlLabel, createdLabel }) label.Foreground = owner.Brush("Secondary");
            foreach (var text in new[] { filename, altLabel, altValue, urlLabel, urlValue, createdLabel, createdValue }) { text.Language = owner.Language; text.FlowDirection = owner.FlowDirection; text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; }
            imageBorder.Background = owner.Brush("PreviewBackground"); imageBorder.BorderBrush = bottomBorder.BorderBrush = owner.Brush("PreviewBorder"); bottomBorder.Padding = new(0, 20 * f, 0, 0);
            var wide = (owner.XamlRoot?.Size.Width ?? 0) >= 768; bottom.ColumnDefinitions.Clear(); bottom.RowDefinitions.Clear();
            if (wide) { bottom.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); bottom.ColumnDefinitions.Add(new() { Width = new(24 * f) }); bottom.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); Grid.SetColumn(url, 0); Grid.SetColumn(created, 2); Grid.SetRow(url, 0); Grid.SetRow(created, 0); created.Margin = new(0); }
            else { bottom.ColumnDefinitions.Add(new() { Width = new(1, GridUnitType.Star) }); bottom.RowDefinitions.Add(new() { Height = GridLength.Auto }); bottom.RowDefinitions.Add(new() { Height = GridLength.Auto }); Grid.SetColumn(url, 0); Grid.SetColumn(created, 0); Grid.SetRow(url, 0); Grid.SetRow(created, 1); created.Margin = new(0, url.Visibility == Visibility.Visible ? 24 * f : 0, 0, 0); }
            image.Update(item); UpdateInput(); InvalidateMeasure();
        }
        internal void UpdateInput() { copyFilename.IsEnabled = owner.PreviewInputAllowed && owner.CopyCommand?.CanExecute(owner.selected?.Filename ?? "") != false; copyUrl.IsEnabled = owner.PreviewInputAllowed && owner.CopyCommand?.CanExecute(owner.selected?.Url ?? "") != false; link.IsEnabled = owner.PreviewInputAllowed && owner.UrlCommand?.CanExecute(owner.selected?.Url ?? "") != false; }
        public void Dispose() { copyFilename.Click -= CopyFilename; copyUrl.Click -= CopyUrl; link.Click -= RouteUrl; image.Dispose(); link.Content = null; Children.Clear(); }
        private sealed class ImageFrame : Panel, IDisposable
        {
            private readonly BleeckerMediaLibrary owner;
            private readonly Image image = new() { Name = "MediaPreviewImage", Stretch = Stretch.Uniform, IsHitTestVisible = false, FlowDirection = FlowDirection.LeftToRight };
            private readonly RichTextBlock failure;
            private readonly Run failureLabel = new();
            private readonly Microsoft.UI.Xaml.Shapes.Path failureGlyph;
            private BleeckerMediaLibraryItem? item;
            private bool failed;
            private Size size;
            internal ImageFrame(BleeckerMediaLibrary owner)
            {
                this.owner = owner; UseLayoutRounding = false;
                failure = new() { Name = "MediaPreviewFailedImage", FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"], FontSize = 16, LineHeight = 24, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false, IsHitTestVisible = false, Visibility = Visibility.Collapsed };
                failureGlyph = (Microsoft.UI.Xaml.Shapes.Path)XamlReader.Load("<Path xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Name='MediaPreviewFailedImageGlyph' Width='16' Height='16' Stretch='None' StrokeThickness='1' Data='M1.5,1.5 H14.5 V14.5 H1.5 Z M3.5,11 L6,8 L8,10 L10,7 L13,11 M10,5 A1,1 0 1 0 8,5 A1,1 0 1 0 10,5'/>");
                failureGlyph.IsHitTestVisible = false; failureGlyph.RenderTransform = new TranslateTransform { Y = -4 }; AutomationProperties.SetAccessibilityView(failureGlyph, AccessibilityView.Raw);
                var paragraph = new Paragraph(); paragraph.Inlines.Add(new InlineUIContainer { Child = failureGlyph }); paragraph.Inlines.Add(failureLabel); failure.Blocks.Add(paragraph);
                Children.Add(image); Children.Add(failure);
                image.ImageOpened += Opened; image.ImageFailed += Failed;
            }
            internal void Update(BleeckerMediaLibraryItem? next)
            {
                item = next; if (!ReferenceEquals(image.Source, next?.ImageSource)) { failed = false; image.Source = next?.ImageSource; }
                var label = string.IsNullOrEmpty(next?.Alt) ? next?.Filename ?? "" : next.Alt; AutomationProperties.SetName(image, label); failureLabel.Text = label; failure.Foreground = owner.Foreground; failureGlyph.Stroke = owner.Brush("Secondary"); failure.Language = owner.Language; failure.FlowDirection = owner.FlowDirection; image.Visibility = image.Source is not null && !failed ? Visibility.Visible : Visibility.Collapsed; failure.Visibility = failed ? Visibility.Visible : Visibility.Collapsed; InvalidateMeasure();
            }
            private void Opened(object sender, RoutedEventArgs args) { failed = false; Update(item); }
            private void Failed(object sender, ExceptionRoutedEventArgs args) { failed = true; Update(item); if (item is not null) owner.Failed(item); }
            protected override Size MeasureOverride(Size available)
            {
                var width = double.IsFinite(available.Width) ? available.Width : owner.XamlRoot?.Size.Width ?? 0;
                if (failed) { failure.Measure(new(width, double.PositiveInfinity)); size = failure.DesiredSize; return new(width, size.Height); }
                if (image.Source is null) { size = new(0, 0); return new(width, 0); }
                var maximumHeight = .6 * (owner.XamlRoot?.Size.Height ?? available.Height); double w, h;
                if (image.Source is BitmapSource bitmap && bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0) { w = bitmap.PixelWidth; h = bitmap.PixelHeight; }
                else if (image.Source is SvgImageSource svg && double.IsFinite(svg.RasterizePixelWidth) && svg.RasterizePixelWidth > 0 && double.IsFinite(svg.RasterizePixelHeight) && svg.RasterizePixelHeight > 0) { w = svg.RasterizePixelWidth; h = svg.RasterizePixelHeight; }
                else { image.Measure(new(width, maximumHeight)); w = image.DesiredSize.Width; h = image.DesiredSize.Height; }
                var scale = w > 0 && h > 0 ? Math.Min(1, Math.Min(width / w, maximumHeight / h)) : 0; size = new(w * scale, h * scale); image.Measure(size); return new(width, size.Height);
            }
            protected override Size ArrangeOverride(Size final) { var bounds = new Rect((final.Width - size.Width) / 2, 0, size.Width, size.Height); image.Arrange(bounds); failure.Arrange(bounds); return final; }
            public void Dispose() { image.ImageOpened -= Opened; image.ImageFailed -= Failed; image.Source = null; item = null; Children.Clear(); }
        }
    }
}
