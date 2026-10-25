using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerMediaLibrary
{
    internal static TextBlock Text(string name, FontFamily family, double size, double line, bool semibold = false) => new() { Name = name, FontFamily = family, FontSize = size, FontWeight = semibold ? FontWeights.SemiBold : FontWeights.Normal, LineHeight = line, LineStackingStrategy = LineStackingStrategy.BlockLineHeight, TextWrapping = TextWrapping.Wrap, UseLayoutRounding = false };
    private sealed class MediaPanel : Panel, IDisposable
    {
        private readonly BleeckerMediaLibrary owner;
        internal readonly BleeckerSearchInput Search = new() { Name = "MediaSearch", UseLayoutRounding = false };
        private readonly TextBlock kicker, title, emptyText;
        private readonly ContentPresenter actions = new() { Name = "MediaActions", UseLayoutRounding = false }, filter = new() { Name = "MediaFilter", UseLayoutRounding = false };
        private readonly Border empty;
        internal readonly Style DefaultPreviewStyle;
        private Rect kickerBox, titleBox, actionBox, searchBox, filterBox, emptyBox;
        private readonly Dictionary<string, Rect> tileBoxes = [];
        private bool hasFilter, hasActions;
        internal MediaPanel(BleeckerMediaLibrary owner)
        {
            this.owner = owner; Name = "MediaLayout"; UseLayoutRounding = false; FlowDirection = FlowDirection.LeftToRight;
            kicker = Text("MediaKicker", (FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"], 10, 15, true); kicker.Text = "MEDIA COLLECTION"; kicker.CharacterSpacing = 110;
            title = Text("MediaTitle", (FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"], 32, 40, true); title.CharacterSpacing = -20; AutomationProperties.SetHeadingLevel(title, Microsoft.UI.Xaml.Automation.Peers.AutomationHeadingLevel.Level1);
            emptyText = Text("MediaEmptyText", (FontFamily)Application.Current.Resources["BleeckerSecondaryFontFamily"], 14, 20); emptyText.TextAlignment = TextAlignment.Center;
            empty = new() { Name = "MediaEmpty", Child = emptyText, BorderThickness = new(1), CornerRadius = new(12), UseLayoutRounding = false };
            foreach (var element in new UIElement[] { kicker, title, actions, Search, filter, empty }) Children.Add(element);
            AutomationProperties.SetName(Search, "Search media"); Search.Editor.TextChanged += SearchChanged; Search.ClearRequested += ClearSearch;
            Search.Editor.Loaded += EditorLoaded;
            var modalPalette = new ResourceDictionary { Source = new Uri("ms-appx:///Orchard/Themes/Modal.xaml") };
            DefaultPreviewStyle = new Style(typeof(BleeckerOverlaySurface)) { BasedOn = (Style)modalPalette["BleeckerModalSurfaceStyle"] }; DefaultPreviewStyle.Setters.Add(new Setter(MaxWidthProperty, 896d));
        }
        private void ClearSearch(object? sender, EventArgs args) => owner.RequestSearch("");
        private void SearchChanged(object sender, TextChangedEventArgs args) => owner.RequestSearch(Search.Editor.Text);
        private void EditorLoaded(object sender, RoutedEventArgs args) => Update();
        internal void Update()
        {
            var f = owner.RemScale; title.Text = owner.Title; title.FontSize = 32 * f; title.LineHeight = 40 * f; title.FontFamily = owner.HasCallerFontFamily ? owner.FontFamily : (FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"]; title.Foreground = owner.Foreground;
            kicker.Foreground = owner.Brush("Desert"); emptyText.Text = owner.EmptyMessage; emptyText.FontSize = 14 * f; emptyText.LineHeight = 20 * f; emptyText.Foreground = owner.Brush("Secondary");
            empty.Background = owner.Brush("EmptyBackground"); empty.BorderBrush = owner.Brush("EmptyBorder"); empty.Padding = new(32 * f, 56 * f, 32 * f, 56 * f); empty.Visibility = owner.items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (!ReferenceEquals(actions.Content, owner.Actions)) actions.Content = owner.Actions; actions.ContentTemplate = owner.ActionsTemplate;
            if (!ReferenceEquals(filter.Content, owner.Filter)) filter.Content = owner.Filter; filter.ContentTemplate = owner.FilterTemplate;
            hasActions = owner.Actions is not null || owner.ActionsTemplate is not null; hasFilter = owner.FilterTemplate is not null || owner.Filter is not (null or false or 0 or "");
            actions.Visibility = hasActions ? Visibility.Visible : Visibility.Collapsed; filter.Visibility = hasFilter ? Visibility.Visible : Visibility.Collapsed;
            Search.Editor.Height = Search.Height = 40 * f; Search.Editor.FontSize = 14 * f; Search.FontFamily = owner.FontFamily;
            foreach (var element in new FrameworkElement[] { title, kicker, emptyText, actions, filter, Search }) { element.FlowDirection = owner.FlowDirection; element.Language = owner.Language; }
            foreach (var text in new[] { title, kicker, emptyText }) text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled;
            UpdateInput(); InvalidateMeasure();
        }
        internal void UpdateInput() { Search.IsEnabled = owner.InputAllowed; actions.IsHitTestVisible = filter.IsHitTestVisible = owner.InputAllowed; }
        protected override Size MeasureOverride(Size available)
        {
            var f = owner.RemScale; var width = double.IsFinite(available.Width) ? available.Width : owner.XamlRoot?.Size.Width ?? 0; var viewport = owner.XamlRoot?.Size.Width ?? width; var wide = viewport >= 768; var columns = viewport >= 1024 ? 5 : wide ? 4 : 2; var px = (wide ? 32 : 20) * f; var py = (wide ? 36 : 24) * f; var inner = Math.Max(0, width - 2 * px); var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            void Set(ref Rect target, double x, double y, double w, double h) => target = new(rtl ? width - x - w : x, y, Math.Max(0, w), Math.Max(0, h));
            actions.Measure(new(inner, double.PositiveInfinity)); var actionWidth = hasActions ? Math.Min(inner, actions.DesiredSize.Width) : 0; var textWidth = wide && hasActions ? Math.Max(0, inner - actionWidth - 20 * f) : inner;
            kicker.Measure(new(textWidth, double.PositiveInfinity)); title.Measure(new(textWidth, double.PositiveInfinity)); var headingHeight = 15 + 8 * f + title.DesiredSize.Height;
            var headerHeight = wide ? Math.Max(headingHeight, hasActions ? actions.DesiredSize.Height : 0) : headingHeight + (hasActions ? 20 * f + actions.DesiredSize.Height : 0);
            Set(ref kickerBox, px, py, textWidth, 15); Set(ref titleBox, px, py + 15 + 8 * f, textWidth, title.DesiredSize.Height);
            Set(ref actionBox, wide ? width - px - actionWidth : px, wide ? py + headerHeight - actions.DesiredSize.Height : py + headingHeight + 20 * f, wide ? actionWidth : inner, actions.DesiredSize.Height);
            var toolsY = py + headerHeight + 32 * f; var filterWidth = wide ? Math.Min(256 * f, inner) : inner; filter.Measure(new(filterWidth, double.PositiveInfinity)); var searchWidth = Math.Max(0, inner - (wide && hasFilter ? filterWidth + 12 * f : 0)); Search.Measure(new(searchWidth, 40 * f));
            var toolsHeight = wide ? Math.Max(40 * f, hasFilter ? filter.DesiredSize.Height : 0) : 40 * f + (hasFilter ? 12 * f + filter.DesiredSize.Height : 0);
            Set(ref searchBox, px, toolsY, searchWidth, 40 * f); Set(ref filterBox, wide ? width - px - filterWidth : px, wide ? toolsY : toolsY + 52 * f, filterWidth, filter.DesiredSize.Height);
            var gridY = toolsY + toolsHeight + 28 * f; var tileSize = Math.Max(0, (inner - (columns - 1) * 16 * f) / columns); var rows = (owner.items.Count + columns - 1) / columns; tileBoxes.Clear();
            for (var i = 0; i < owner.items.Count; i++) { var item = owner.items[i]; var tile = owner.tiles[item.Id]; tile.Root.Measure(new(tileSize, tileSize)); var x = px + i % columns * (tileSize + 16 * f); tileBoxes[item.Id] = new(rtl ? width - x - tileSize : x, gridY + i / columns * (tileSize + 16 * f), tileSize, tileSize); }
            var gridHeight = rows * tileSize + Math.Max(0, rows - 1) * 16 * f; empty.Measure(new(inner, double.PositiveInfinity)); emptyBox = new(px, gridY, inner, empty.DesiredSize.Height);
            return new(width, gridY + gridHeight + (rows == 0 ? empty.DesiredSize.Height : 0) + py);
        }
        protected override Size ArrangeOverride(Size final)
        {
            kicker.Arrange(kickerBox); title.Arrange(titleBox); actions.Arrange(actionBox); Search.Arrange(searchBox); filter.Arrange(filterBox); empty.Arrange(emptyBox);
            foreach (var item in owner.items) if (tileBoxes.TryGetValue(item.Id, out var box)) owner.tiles[item.Id].Root.Arrange(box);
            return final;
        }
        public void Dispose() { Search.Editor.TextChanged -= SearchChanged; Search.ClearRequested -= ClearSearch; Search.Editor.Loaded -= EditorLoaded; actions.Content = filter.Content = null; Children.Clear(); }
    }
}
