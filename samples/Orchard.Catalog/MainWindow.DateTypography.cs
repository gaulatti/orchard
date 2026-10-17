using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private static object[] DateTypography(BleeckerCalendar? calendar, IReadOnlyList<Button> triggers, BleeckerDateRangePicker? range, Button? clear, FrameworkElement viewport)
    {
        var result = new List<object>();
        void Add(string role, TextBlock text)
        {
            var metrics = new TextBlock { Text = "Mg", FontFamily = text.FontFamily, FontSize = text.FontSize, FontWeight = text.FontWeight,
                FontStyle = text.FontStyle, FontStretch = text.FontStretch, IsTextScaleFactorEnabled = text.IsTextScaleFactorEnabled, UseLayoutRounding = false };
            metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var transform = text.TransformToVisual(viewport);
            result.Add(new { role, text = text.Text, baseline = transform.TransformPoint(new Point(0, text.BaselineOffset)).Y,
                box = transform.TransformBounds(new Rect(0, 0, text.ActualWidth, text.ActualHeight)), fontSize = text.FontSize, fontWeight = text.FontWeight.Weight,
                lineHeight = text.LineHeight, letterSpacing = text.FontSize * text.CharacterSpacing / 1000, numerals = Typography.GetNumeralAlignment(text).ToString(),
                naturalBaseline = metrics.BaselineOffset, naturalHeight = metrics.DesiredSize.Height, blockBaseline = text.BaselineOffset,
                lineBox = (text.Parent as FrameworkElement)?.TransformToVisual(viewport).TransformBounds(new Rect(0, 0, ((FrameworkElement)text.Parent).ActualWidth, ((FrameworkElement)text.Parent).ActualHeight)) });
        }
        if (calendar is not null)
        {
            Add("heading", DateDescendants<TextBlock>(calendar).Single(item => item.Text == calendar.ViewMonth.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-US"))));
            var names = new[] { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };
            for (var index = 0; index < names.Length; index++) Add("weekday-" + index, DateDescendants<TextBlock>(calendar).Single(item => item.Text == names[index]));
            foreach (var day in calendar.DayButtons) Add("day-" + day.Content, DateDescendants<TextBlock>(day).Single(item => item.Name == "DayText"));
        }
        for (var index = 0; index < triggers.Count; index++) Add("trigger-" + index, DateDescendants<TextBlock>(triggers[index]).Single());
        if (range is not null)
        {
            Add("range-label-0", DateDescendants<TextBlock>(range).Single(item => item.Text == "FROM"));
            Add("range-label-1", DateDescendants<TextBlock>(range).Single(item => item.Text == "TO"));
            var actions = DateDescendants<BleeckerButton>(range).Where(item => item.Visibility == Visibility.Visible).ToArray();
            for (var index = 0; index < actions.Length; index++)
            {
                var label = actions[index].Content as string ?? "Clear";
                Add("action-" + index, DateDescendants<TextBlock>(actions[index]).Single(item => item.Text == label));
            }
        }
        if (clear is { Visibility: Visibility.Visible }) Add("clear", DateDescendants<TextBlock>(clear).Single());
        return result.ToArray();
    }
}
