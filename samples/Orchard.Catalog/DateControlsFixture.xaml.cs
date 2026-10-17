using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class DateControlsFixture : UserControl
{
    public DateControlsFixture()
    {
        InitializeComponent();
        CalendarSample.Value = BleeckerDates.Midnight(new DateTime(2026, 10, 17));
        CalendarSample.DateRequested += (_, args) => CalendarSample.Value = args.Value;
        PickerSample.DateRequested += (_, args) => PickerSample.Value = args.Value;
        RangeSample.Value = new("2026-10-10", "2026-10-20");
        RangeSample.Presets = new[] { new BleeckerDatePreset("Seven days", 7), new BleeckerDatePreset("Today", 0) };
        RangeSample.RangeRequested += (_, args) => RangeSample.Value = args.Value;
    }
    internal async Task VerifyAsync(string folder, BleeckerColorScheme scheme, FrameworkElement foreground)
    {
        using var input = new NativeInputSession(XamlRoot, folder, "dates-catalog-" + scheme);
        await input.AcquireAsync(foreground);
        var initialCalendar = CalendarSample.Value; var initialPicker = PickerSample.Value; var initialRange = RangeSample.Value; var passed = false;
        try
        {
            Require(BleeckerTextLayout.GetUseReferenceLineBox(LineBoxSample), "Catalog text line box was not enabled.");
            BleeckerTextLayout.SetUseReferenceLineBox(LineBoxSample, false); await MainWindow.AwaitPresentationAsync(LineBoxSample);
            Require(!(bool)LineBoxSample.GetValue(BleeckerTextLayout.UseReferenceLineBoxProperty), "Catalog text line box did not disable.");
            Require(LineBoxSample.Parent is StackPanel && LineBoxSample.RenderTransform is null &&
                LineBoxSample.HorizontalAlignment == HorizontalAlignment.Stretch && LineBoxSample.VerticalAlignment == VerticalAlignment.Stretch,
                "Disabling the catalog text line box did not restore its parent, alignment and transform.");
            BleeckerTextLayout.SetUseReferenceLineBox(LineBoxSample, true); await MainWindow.AwaitPresentationAsync(LineBoxSample);
            var sampleParent = (FrameworkElement)LineBoxSample.Parent;
            var sampleBaseline = LineBoxSample.TransformToVisual(sampleParent).TransformPoint(new Windows.Foundation.Point(0, LineBoxSample.BaselineOffset)).Y;
            Require(Math.Abs(sampleBaseline - 14) < .001, "Catalog text line box lost the measured heading baseline.");
            CalendarSample.Value = BleeckerDates.Parse("2026-10-17");
            await MainWindow.AwaitPresentationAsync(CalendarSample);
            await input.ClickAsync(CalendarSample.DayButtons.Single(x => x.Content.ToString() == "19"));
            Require(BleeckerDates.Serialize(CalendarSample.Value) == "2026-10-19", "Catalog calendar did not reach its consumer.");
            await input.ClickAsync(PickerSample.Trigger); await Until(() => PickerSample.Calendar?.IsLoaded == true);
            await input.ClickClosingSurfaceSourceAsync(PickerSample.Calendar!.DayButtons.Single(x => x.Content.ToString() == "19"), "selection");
            Require(!PickerSample.IsDropDownOpen && PickerSample.Value is not null, "Catalog picker did not update and close.");
            var preset = Children<BleeckerButton>(RangeSample).Single(x => x.Content as string == "Seven days");
            await input.ClickAsync(Children<Button>(preset).Single(x => x.Name == "PART_Action"));
            var today = DateTime.Now.Date;
            Require(RangeSample.Value == new BleeckerDateRange(today.AddDays(-7).ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd")), "Catalog range preset did not reach its consumer.");
            passed = true;
        }
        finally
        {
            CalendarSample.Value = initialCalendar; PickerSample.Value = initialPicker; RangeSample.Value = initialRange;
            await File.WriteAllTextAsync(Path.Combine(folder, $"dates-catalog-{scheme}.json"), JsonSerializer.Serialize(new { passed, consumerUpdates = 3, popupClosed = !PickerSample.IsDropDownOpen }));
        }
        await MainWindow.AwaitPresentationAsync(this);
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"dates-{scheme}.png"));
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        static async Task Until(Func<bool> value) { var clock = Stopwatch.StartNew(); while (!value() && clock.ElapsedMilliseconds < 2000) await Task.Delay(16); Require(value(), "Catalog date popup did not mount."); }
        static IEnumerable<T> Children<T>(DependencyObject root) where T : DependencyObject
        { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T typed) yield return typed; foreach (var nested in Children<T>(child)) yield return nested; } }
    }
}
