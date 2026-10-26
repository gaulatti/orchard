using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerFooter
{
    private FooterLink NewLink(string key, string role) { var link = new FooterLink(this, key, role); links.Add(link); return link; }
    private static void Order(Panel parent, IEnumerable<UIElement> values)
    {
        var next = values.ToArray(); foreach (var child in parent.Children.ToArray()) if (!next.Contains(child)) parent.Children.Remove(child);
        for (var index = 0; index < next.Length; index++) { var current = parent.Children.IndexOf(next[index]); if (current == index) continue; if (current >= 0) parent.Children.RemoveAt(current); parent.Children.Insert(index, next[index]); }
    }
    private sealed class ManualPanel : Panel
    {
        internal readonly Dictionary<UIElement, Rect> Bounds = [];
        internal Size Extent;
        protected override Size MeasureOverride(Size available)
        {
            foreach (var child in Children.ToArray()) child.Measure(Bounds.TryGetValue(child, out var box) ? new Size(box.Width, box.Height) : available);
            return Extent;
        }
        protected override Size ArrangeOverride(Size final) { foreach (var (child, bounds) in Bounds.ToArray()) if (Children.Contains(child)) child.Arrange(bounds); return final; }
        internal void Complete(Size size) { if (Extent != size) { Extent = size; InvalidateMeasure(); } else InvalidateArrange(); Measure(size); }
    }
    private sealed class SectionSlot : Panel
    {
        private readonly BleeckerFooter owner;
        internal string Key { get; }
        internal BleeckerFooterSection Model { get; private set; } = null!;
        internal readonly BleeckerContentText Heading = new("FooterSectionHeading", "BleeckerPrimaryMediumFontFamily", 14, 20, "BleeckerDesertBrush", true);
        private readonly List<FooterLink> rows = [];
        private FooterLink[] measured = [];
        private readonly List<Rect> bounds = [];
        internal SectionSlot(BleeckerFooter owner, string key)
        {
            this.owner = owner; Key = key; Name = "FooterSection"; FlowDirection = FlowDirection.LeftToRight; UseLayoutRounding = false; Children.Add(Heading);
            Heading.Text.CharacterSpacing = 50; Heading.ConstrainLayoutWidth = true; AutomationProperties.SetHeadingLevel(Heading, AutomationHeadingLevel.Level3);
        }
        internal void Update(BleeckerFooterSection model)
        {
            Model = model; Heading.FlowDirection = owner.FlowDirection; Heading.Language = owner.Language; Heading.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; Heading.Set(BleeckerTextCasing.Uppercase(model.Title, owner.Language), null); AutomationProperties.SetName(Heading, model.Title);
            var unused = rows.ToList(); var next = new List<FooterLink>();
            foreach (var item in model.Items)
            {
                if (item is null || item.Href is null || item.Label is null) throw new ArgumentException("Footer items require original destinations and labels.");
                var key = BleeckerNavMenuModel.Key(item.Href, item.Label); var row = unused.FirstOrDefault(candidate => candidate.Key == key && ReferenceEquals(candidate.Template, owner.LinkTemplate));
                if (row is null) row = owner.NewLink(key, "section"); else unused.Remove(row); row.Update(item); next.Add(row);
            }
            foreach (var row in unused) row.Retire(); if (!rows.SequenceEqual(next)) { rows.Clear(); rows.AddRange(next); }
            Order(this, new UIElement[] { Heading }.Concat(rows.Select(row => row.Root))); InvalidateMeasure();
        }
        internal void Retire() { foreach (var row in rows.ToArray()) row.Retire(); rows.Clear(); }
        protected override Size MeasureOverride(Size available)
        {
            measured = rows.ToArray(); bounds.Clear(); Heading.Measure(new(available.Width, double.PositiveInfinity)); var top = Heading.DesiredSize.Height + 32;
            foreach (var row in measured) { row.MeasureRow(available.Width); var height = row.BlockHeight; var width = row.Root.DesiredSize.Width; bounds.Add(new(owner.FlowDirection == FlowDirection.RightToLeft ? available.Width - width : 0, top + (height - row.Root.DesiredSize.Height) / 2, width, row.Root.DesiredSize.Height)); top += height + 16; }
            return new(available.Width, measured.Length == 0 ? Heading.DesiredSize.Height + 32 : top - 16);
        }
        protected override Size ArrangeOverride(Size final)
        {
            Heading.Arrange(new(0, 0, final.Width, Heading.DesiredSize.Height)); for (var index = 0; index < measured.Length; index++) measured[index].Root.Arrange(bounds[index]); return final;
        }
    }
    private sealed class FooterPanel : Panel
    {
        private readonly BleeckerFooter owner;
        private readonly Border rule, accent, bottomRule;
        private readonly ManualPanel container = new() { Name = "FooterContainer", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        private readonly ManualPanel grid = new() { Name = "FooterGrid", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        private readonly ManualPanel brandColumn = new() { Name = "FooterBrandColumn", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        private readonly ManualPanel bottom = new() { Name = "FooterBottom", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        private readonly BleeckerContentText description = new("FooterDescription", "BleeckerPrimaryFontFamily", 16, 26, "BleeckerTextSecondaryBrush");
        private readonly BleeckerContentText left = new("FooterBottomLeft", "BleeckerPrimaryFontFamily", 14, 20, "BleeckerTextSecondaryBrush");
        private readonly BleeckerContentText right = new("FooterBottomRight", "BleeckerPrimaryFontFamily", 14, 20, "FooterSourceTextBrush");
        private FooterLink? brandLink, sourceLink;
        private readonly ManualPanel sourceLine = new() { Name = "FooterSourceLine", FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        private readonly BleeckerNavItem brandItem = new();
        private Rect containerBox, accentBox;
        private SectionSlot[] measured = [];
        internal FooterPanel(BleeckerFooter owner)
        {
            this.owner = owner; Name = "FooterLayout";
            static Border Line(string name, string resource, double height) => (Border)XamlReader.Load($"<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Name='{name}' Background='{{ThemeResource {resource}}}' Height='{height}' UseLayoutRounding='False' AutomationProperties.AccessibilityView='Raw'/>");
            rule = Line("FooterTopRule", "FooterTopRuleBrush", 1); accent = Line("FooterAccent", "FooterAccentBrush", 2); bottomRule = Line("FooterBottomRule", "FooterBottomRuleBrush", 1);
            Children.Add(rule); Children.Add(container); Children.Add(accent); container.Children.Add(grid); container.Children.Add(bottom); grid.Children.Add(brandColumn);
            brandColumn.Children.Add(description); bottom.Children.Add(bottomRule); bottom.Children.Add(left); bottom.Children.Add(right);
            foreach (var text in new[] { description, left, right }) text.Text.CharacterSpacing = -20;
            left.DirectionalMaxContent = right.DirectionalMaxContent = true;
        }
        internal void Sync(BleeckerFooterSection[] values)
        {
            if (brandLink is null || !ReferenceEquals(brandLink.Template, owner.LinkTemplate)) { if (brandLink is not null) { brandColumn.Children.Remove(brandLink.Root); brandLink.Retire(); } brandLink = owner.NewLink("brand", "brand"); brandColumn.Children.Insert(0, brandLink.Root); }
            brandItem.Href = owner.Brand.Href; brandItem.Label = owner.Brand.BrandName ?? owner.Brand.LogoAlt; brandLink.Update(brandItem);
            description.Set(owner.Brand.Description, null);
            foreach (var text in new[] { description, left, right }) { text.FlowDirection = owner.FlowDirection; text.Language = owner.Language; text.Text.IsTextScaleFactorEnabled = owner.IsTextScaleFactorEnabled; }
            var unused = owner.sections.ToList(); var next = new List<SectionSlot>();
            foreach (var value in values) { var section = unused.FirstOrDefault(row => row.Key == value.Title); if (section is null) section = new(owner, value.Title); else unused.Remove(section); section.Update(value); next.Add(section); }
            foreach (var section in unused) section.Retire(); if (!owner.sections.SequenceEqual(next)) { owner.sections.Clear(); owner.sections.AddRange(next); }
            Order(grid, new UIElement[] { brandColumn }.Concat(next));
            left.Set(owner.BottomLeft, owner.BottomLeftTemplate, selector: owner.BottomLeftTemplateSelector);
            var source = BleeckerFooterModel.DefaultSource(owner.BottomRight) ? values.SelectMany(section => section.Items).FirstOrDefault(item => BleeckerFooterModel.IsSource(item.Href)) : null;
            if (source is not null)
            {
                if (sourceLink is null || !ReferenceEquals(sourceLink.Template, owner.LinkTemplate)) { right.Set(null, null); if (sourceLink is not null) sourceLine.Children.Remove(sourceLink.Root); sourceLink?.Retire(); sourceLink = owner.NewLink("source", "source"); sourceLine.Children.Add(sourceLink.Root); }
                sourceLink.Update(source); right.Set(sourceLine, null);
            }
            else { right.Set(owner.BottomRight, owner.BottomRightTemplate, selector: owner.BottomRightTemplateSelector); if (sourceLink is not null) sourceLine.Children.Remove(sourceLink.Root); sourceLink?.Retire(); sourceLink = null; }
            accent.Visibility = owner.ShowBottomAccent ? Visibility.Visible : Visibility.Collapsed; InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            var width = double.IsFinite(available.Width) ? available.Width : owner.XamlRoot?.Size.Width ?? 0;
            var viewport = owner.XamlRoot?.Size.Width ?? width; var containerWidth = BleeckerFooterModel.Container(viewport, width); var inner = Math.Max(0, containerWidth - 32);
            var columns = BleeckerFooterModel.Columns(viewport); var span = BleeckerFooterModel.BrandSpan(viewport, owner.sections.Count); var cell = Math.Max(0, (inner - (columns - 1) * 48) / columns);
            container.Bounds.Clear(); grid.Bounds.Clear(); brandColumn.Bounds.Clear(); bottom.Bounds.Clear(); measured = owner.sections.ToArray();
            var brandWidth = cell * span + (span - 1) * 48; brandLink!.MeasureBrand(brandWidth);
            var showDescription = !string.IsNullOrEmpty(owner.Brand.Description); description.Measure(new(Math.Min(448, brandWidth), double.PositiveInfinity));
            var inlineHeight = brandLink.Root.DesiredSize.Height + Math.Max(0, 6 - (brandLink.Root.DesiredSize.Height - 48) / 2);
            var brandHeight = inlineHeight + (showDescription ? 32 + description.DesiredSize.Height : 0);
            var brandLinkWidth = brandLink.Root.DesiredSize.Width; var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            brandColumn.Bounds[brandLink.Root] = new(rtl ? brandWidth - brandLinkWidth : 0, 0, brandLinkWidth, brandLink.Root.DesiredSize.Height);
            brandColumn.Bounds[description] = new(rtl ? brandWidth - Math.Min(448, brandWidth) : 0, inlineHeight + 32, Math.Min(448, brandWidth), showDescription ? description.DesiredSize.Height : 0);
            var placements = new List<(UIElement Child, int Column, int Row, double Width, double Height)> { (brandColumn, 0, 0, brandWidth, brandHeight) }; var cursor = span;
            foreach (var section in measured) { section.Measure(new(cell, double.PositiveInfinity)); placements.Add((section, cursor % columns, cursor / columns, cell, section.DesiredSize.Height)); cursor++; }
            var rowHeights = placements.GroupBy(place => place.Row).OrderBy(group => group.Key).Select(group => group.Max(place => place.Height)).ToArray(); var tops = new double[rowHeights.Length];
            for (var index = 1; index < tops.Length; index++) tops[index] = tops[index - 1] + rowHeights[index - 1] + 48;
            foreach (var place in placements) { var x = place.Column * (cell + 48); if (rtl) x = inner - x - place.Width; grid.Bounds[place.Child] = new(x, tops[place.Row], place.Width, rowHeights[place.Row]); }
            brandColumn.Complete(new(brandWidth, rowHeights[0]));
            var gridHeight = rowHeights.Sum() + Math.Max(0, rowHeights.Length - 1) * 48;
            grid.Complete(new(inner, gridHeight)); container.Bounds[grid] = new(16, 64, inner, gridHeight);
            if (sourceLink is not null)
            {
                sourceLink.MeasureRow(inner); var lineHeight = sourceLink.BlockHeight;
                sourceLine.Bounds.Clear(); sourceLine.Bounds[sourceLink.Root] = new(0, Math.Floor((lineHeight - sourceLink.Root.DesiredSize.Height) / 2), sourceLink.Root.DesiredSize.Width, sourceLink.Root.DesiredSize.Height); sourceLine.Complete(new(sourceLink.Root.DesiredSize.Width, lineHeight));
            }
            left.Measure(new(inner, double.PositiveInfinity)); right.Measure(new(inner, double.PositiveInfinity));
            var leftWidth = Math.Min(inner, left.DesiredSize.Width); var rightWidth = Math.Min(inner, right.DesiredSize.Width); var row = viewport >= 768;
            if (row && leftWidth + rightWidth + 16 > inner)
            {
                var scale = Math.Max(0, inner - 16) / Math.Max(1, leftWidth + rightWidth); leftWidth = Math.Round(leftWidth * scale * 64, MidpointRounding.AwayFromZero) / 64; rightWidth = Math.Max(0, inner - 16 - leftWidth); left.Measure(new(leftWidth, double.PositiveInfinity)); right.Measure(new(rightWidth, double.PositiveInfinity));
            }
            var leftHeight = left.DesiredSize.Height; var rightHeight = right.DesiredSize.Height;
            var contentHeight = row ? Math.Max(leftHeight, rightHeight) : leftHeight + 16 + rightHeight; var bottomHeight = 29 + contentHeight;
            bottom.Bounds[bottomRule] = new(0, 0, inner, 1); bottomRule.Measure(new(inner, 1));
            var leftX = row ? 0 : (inner - leftWidth) / 2; var rightX = row ? inner - rightWidth : (inner - rightWidth) / 2;
            if (row && rtl) { leftX = inner - leftWidth; rightX = 0; }
            bottom.Bounds[left] = new(leftX, 29 + (row ? (contentHeight - leftHeight) / 2 : 0), leftWidth, leftHeight);
            bottom.Bounds[right] = new(rightX, 29 + (row ? (contentHeight - rightHeight) / 2 : leftHeight + 16), rightWidth, rightHeight);
            bottom.Complete(new(inner, bottomHeight)); container.Bounds[bottom] = new(16, 64 + gridHeight + 64, inner, bottomHeight);
            var containerHeight = 128 + gridHeight + 64 + bottomHeight; container.Complete(new(containerWidth, containerHeight));
            rule.Measure(new(width, 1)); accent.Measure(new(width, owner.ShowBottomAccent ? 2 : 0)); containerBox = new((width - containerWidth) / 2, 1, containerWidth, containerHeight); accentBox = new(0, 1 + containerHeight, width, owner.ShowBottomAccent ? 2 : 0);
            return new(width, accentBox.Y + accentBox.Height);
        }
        protected override Size ArrangeOverride(Size final) { rule.Arrange(new(0, 0, final.Width, 1)); container.Arrange(containerBox); accent.Arrange(accentBox); return final; }
    }
}
