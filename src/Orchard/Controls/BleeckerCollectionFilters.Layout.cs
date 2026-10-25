using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerCollectionFilters
{
    private sealed partial class FieldSlot : Panel
    {
        internal BleeckerCollectionFilterOption Option { get; }
        private readonly BleeckerCollectionFilters owner;
        private readonly BleeckerContentText label;
        private readonly BleeckerSelectOption all = new("", "All");
        private readonly BleeckerSelectOption[] booleanOptions = [new("", "All"), new("true", "Yes"), new("false", "No")];
        private BleeckerSelect? select;
        private BleeckerDatePicker? date;
        internal FieldSlot(BleeckerCollectionFilters owner, BleeckerCollectionFilterOption option)
        {
            this.owner = owner; Option = option; UseLayoutRounding = false; Name = "CollectionField"; FlowDirection = FlowDirection.LeftToRight;
            label = Text("CollectionFieldLabel", option.Label, 12, 18, true); Children.Add(label);
            if (option.Type == BleeckerCollectionFilterType.Boolean || option.Type == BleeckerCollectionFilterType.Select && option.Options is not null)
            {
                select = new() { Name = "CollectionFieldSelect" }; AutomationProperties.SetName(select, option.Label); Children.Add(select);
                select.ValueRequested += (_, request) =>
                {
                    if (!CanAct()) return;
                    BleeckerCollectionFilterValue? value = option.Type == BleeckerCollectionFilterType.Boolean ? request.Value.Length == 0 ? null : new(request.Value == "true") : new(request.Value);
                    owner.RequestFilter(option.Field, value, this);
                };
            }
            else if (option.Type == BleeckerCollectionFilterType.Date)
            {
                date = new() { Name = "CollectionFieldDate", Width = double.NaN, HorizontalAlignment = HorizontalAlignment.Stretch, Placeholder = "Choose " + option.Label.ToLowerInvariant() }; AutomationProperties.SetName(date, option.Label); Children.Add(date);
                date.DateRequested += (_, request) => { if (CanAct()) owner.RequestFilter(option.Field, new(BleeckerCollectionFiltersModel.SerializeDate(request.Value)), this); };
            }
        }
        private bool CanAct() => owner.IsExpanded && owner.fieldSlots.Contains(this) && owner.FilterOptions.Any(option => ReferenceEquals(option, Option)) && owner.CanRequest(this);
        internal void Deactivate() { if (select is not null) select.IsEnabled = false; if (date is not null) date.IsEnabled = false; }
        internal void Update()
        {
            label.FlowDirection = owner.FlowDirection;
            var present = owner.CurrentFilters.TryGetValue(Option.Field, out var value);
            if (select is not null)
            {
                var options = Option.Type == BleeckerCollectionFilterType.Boolean ? booleanOptions : new[] { all }.Concat(Option.Options!).ToArray();
                if (!select.Options.SequenceEqual(options, ReferenceEqualityComparer.Instance)) select.Options = options;
                if (Option.Type == BleeckerCollectionFilterType.Boolean) select.Value = present ? value.Value is bool boolean ? boolean ? "true" : "false" : (string)value.Value : "";
                else if (!present || value.Value is false) select.Value = "";
                else if (value.Value is string text) select.Value = text;
                else
                {
                    // Preserve JS strict comparison for a boolean true supplied
                    // to a string Select: it matches no original string option.
                    var unmatched = "\0"; while (options.Any(option => option.Value == unmatched)) unmatched += "\0"; select.Value = unmatched;
                }
                select.FlowDirection = owner.FlowDirection; select.IsEnabled = owner.IsExpanded;
            }
            if (date is not null)
            {
                if (present && value.Value is not string) throw new ArgumentException("A date filter editor requires a string value.");
                date.Value = BleeckerCollectionFiltersModel.ParseDate(present ? (string)value.Value : ""); date.FlowDirection = owner.FlowDirection; date.IsEnabled = owner.IsExpanded;
            }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            label.Measure(new(available.Width, double.PositiveInfinity));
            if (Children.Count == 1) return new(Math.Min(available.Width, label.DesiredSize.Width), label.DesiredSize.Height);
            var input = (FrameworkElement)Children[1]; input.Measure(new(available.Width, double.PositiveInfinity));
            return new(double.IsFinite(available.Width) ? available.Width : Math.Max(label.DesiredSize.Width, input.DesiredSize.Width), label.DesiredSize.Height + 10 + input.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            label.Arrange(new Rect(0, 0, final.Width, label.DesiredSize.Height));
            if (Children.Count > 1) { var input = (FrameworkElement)Children[1]; input.Arrange(new Rect(0, label.DesiredSize.Height + 10, final.Width, input.DesiredSize.Height)); }
            return final;
        }
    }
    private sealed partial class FieldGrid(BleeckerCollectionFilters owner) : Panel
    {
        private readonly List<Rect> bounds = [];
        protected override Size MeasureOverride(Size available)
        {
            bounds.Clear(); var count = BleeckerCollectionFiltersModel.Columns(owner.XamlRoot?.Size.Width ?? 0); var width = available.Width;
            if (!double.IsFinite(width)) { foreach (var child in Children) child.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); width = Children.Count == 0 ? 0 : Children.OfType<FrameworkElement>().Max(child => child.DesiredSize.Width) * count + (count - 1) * 20; }
            var track = (double)(float)(Math.Max(0, width - (count - 1) * 20) / count); var top = 0d;
            for (var start = 0; start < Children.Count; start += count)
            {
                var row = Children.Skip(start).Take(count).Cast<FrameworkElement>().ToArray(); var height = 0d;
                for (var column = 0; column < row.Length; column++)
                {
                    var left = Math.Floor(track * 64 * column) / 64 + column * 20; var columnWidth = (Math.Floor(track * 64 * (column + 1)) - Math.Floor(track * 64 * column)) / 64;
                    row[column].Measure(new(columnWidth, double.PositiveInfinity)); height = Math.Max(height, row[column].DesiredSize.Height);
                    if (owner.FlowDirection == FlowDirection.RightToLeft) left = width - left - columnWidth;
                    bounds.Add(new(left, top, columnWidth, 0));
                }
                for (var index = bounds.Count - row.Length; index < bounds.Count; index++) { var rect = bounds[index]; rect.Height = height; bounds[index] = rect; }
                top += height + 20;
            }
            return new(width, Children.Count == 0 ? 0 : top - 20);
        }
        protected override Size ArrangeOverride(Size final) { for (var index = 0; index < Children.Count; index++) Children[index].Arrange(bounds[index]); return final; }
    }
    private sealed partial class FlowRow(BleeckerCollectionFilters owner, double gap, bool between = false) : Panel
    {
        private readonly List<(FrameworkElement Child, Rect Box)> bounds = [];
        protected override Size MeasureOverride(Size available)
        {
            bounds.Clear(); var left = 0d; var top = 0d; var rowHeight = 0d; var maximum = 0d; var rowStart = 0;
            var visible = Children.OfType<FrameworkElement>().Where(child => child.Visibility == Visibility.Visible).ToArray();
            void FinishRow()
            {
                var extra = between && bounds.Count - rowStart > 1 ? Math.Max(0, available.Width - left) / (bounds.Count - rowStart - 1) : 0;
                for (var index = rowStart; index < bounds.Count; index++)
                {
                    var (child, rect) = bounds[index]; rect.X += extra * (index - rowStart); rect.Y += (rowHeight - rect.Height) / 2;
                    if (owner.FlowDirection == FlowDirection.RightToLeft) rect.X = (double.IsFinite(available.Width) ? available.Width : left) - rect.X - rect.Width;
                    bounds[index] = (child, rect);
                }
                maximum = Math.Max(maximum, left); rowStart = bounds.Count;
            }
            foreach (var child in visible)
            {
                child.Measure(new(double.PositiveInfinity, double.PositiveInfinity)); var desired = child.DesiredSize;
                if (left > 0 && left + gap + desired.Width > available.Width) { FinishRow(); top += rowHeight + gap; left = rowHeight = 0; }
                if (desired.Width > available.Width) { child.Measure(new(available.Width, double.PositiveInfinity)); desired = child.DesiredSize; }
                if (left > 0) left += gap; bounds.Add((child, new Rect(left, top, desired.Width, desired.Height))); left += desired.Width; rowHeight = Math.Max(rowHeight, desired.Height);
            }
            FinishRow(); return new(between && double.IsFinite(available.Width) ? available.Width : Math.Min(available.Width, maximum), top + rowHeight);
        }
        protected override Size ArrangeOverride(Size final) { foreach (var (child, box) in bounds) child.Arrange(box); return final; }
    }
}
