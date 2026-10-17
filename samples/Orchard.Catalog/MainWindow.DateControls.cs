using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunDateControlsAsync(string folder)
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.DateControls.json") ?? throw new InvalidOperationException("Missing frozen dates reference.");
        using var reference = await JsonDocument.ParseAsync(resource);
        var original = Content; var canvas = new Canvas { Height = 640, Background = new SolidColorBrush(Microsoft.UI.Colors.White) }; var page = new Page { Content = canvas }; NativeInputSession? input = null;
        var outside = new Button { Name = "DateOutside", Content = "Outside", MinWidth = 0, MinHeight = 0, Padding = new Thickness(0), FontSize = 16, FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"], Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><TextBlock Text='{TemplateBinding Content}' FontFamily='{TemplateBinding FontFamily}' FontSize='16' LineHeight='24' LineStackingStrategy='BlockLineHeight' Foreground='{ThemeResource BleeckerTextPrimaryBrush}'/></ControlTemplate>") };
        var frames = new List<object>(); var phases = new List<object>(); var phase = "mount"; var passed = false;
        var width = 0;
        Func<DateTimeOffset> clock = () => BleeckerDates.Midnight(new DateTime(2026, 10, 17)).AddHours(12);
        try
        {
            Content = page; await AwaitPresentationAsync(page); input = new NativeInputSession(page.XamlRoot, folder, "date-controls"); await input.AcquireAsync(canvas);
            width = (int)page.XamlRoot.Size.Width; Require(width is 320 or 640, "Date references require a 320 or 640 client."); canvas.Width = width;
            foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                page.RequestedTheme = scheme; page.Background = canvas.Background = new SolidColorBrush(scheme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                var fixture = new DateControlsFixture(); await Mount(fixture); await fixture.VerifyTextLayoutAsync();
                foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray().Where(x => x.GetProperty("kind").GetString() == "calendar" && x.GetProperty("width").GetInt32() == width && x.GetProperty("scheme").GetString() == scheme.ToString()))
                {
                    phase = source.GetProperty("file").GetString()!; var id = source.GetProperty("id").GetString()!; var state = source.GetProperty("state").GetString()!;
                    var calendar = new BleeckerCalendar(clock) { Width = width - 40, Value = SourceDate(source.GetProperty("value")), FlowDirection = id == "rtl" ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                    if (id == "bounded") { calendar.MinDate = BleeckerDates.Parse("2026-10-15"); calendar.MaxDate = BleeckerDates.Parse("2026-10-20"); }
                    if (id == "min-noon") calendar.MinDate = BleeckerDates.Parse("2026-10-17")!.Value.AddHours(12);
                    if (id == "inverted") { calendar.MinDate = BleeckerDates.Parse("2026-10-20"); calendar.MaxDate = BleeckerDates.Parse("2026-10-10"); }
                    if (id == "weekends") calendar.DisabledDates = date => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                    await Mount(calendar); var active = calendar.DayButtons.FirstOrDefault(x => x.IsEnabled);
                    if (state == "hover") await input.MoveAsync(active!);
                    if (state == "focus") { calendar.NextButton.Focus(FocusState.Keyboard); await input.HeldKeyAsync(0x09, calendar.NextButton, () => active!, () => Until(() => ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), active), "Calendar Tab did not focus its first enabled day.")); }
                    if (state == "pressed") { await input.MoveAsync(active!); await input.PressPointerAsync(); }
                    try
                    {
                        await AwaitPresentationAsync(calendar);
                        Require(Math.Abs(calendar.ActualHeight - source.GetProperty("calendar").GetProperty("box").GetProperty("height").GetDouble()) < .001, phase + ": month height differs.");
                        var actualDays = new List<object>();
                        foreach (var expected in source.GetProperty("calendar").GetProperty("days").EnumerateArray())
                        {
                            var day = calendar.DayButtons.Single(x => x.Content.ToString() == expected.GetProperty("day").GetInt32().ToString());
                            var rectangle = day.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, day.ActualWidth, day.ActualHeight));
                            Require(day.IsEnabled != expected.GetProperty("disabled").GetBoolean() && day.IsChecked == expected.GetProperty("pressed").GetBoolean(), phase + ": day state differs.");
                            var expectedBox = expected.GetProperty("box");
                            Require(Math.Abs(rectangle.X - expectedBox.GetProperty("x").GetDouble()) < .001 && Math.Abs(rectangle.Y - expectedBox.GetProperty("y").GetDouble()) < .001 && Math.Abs(rectangle.Width - expectedBox.GetProperty("width").GetDouble()) < .001 && Math.Abs(rectangle.Height - expectedBox.GetProperty("height").GetDouble()) < .001, phase + ": day geometry differs: " + day.Content + " " + rectangle);
                            Require(AutomationProperties.GetName(day) == expected.GetProperty("label").GetString(), "Calendar day name differs.");
                            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(day); Require(peer.GetPattern(PatternInterface.Toggle) is IToggleProvider, "Calendar day lost native Toggle semantics.");
                            actualDays.Add(new { day = day.Content, rectangle, enabled = day.IsEnabled, selected = day.IsChecked, name = AutomationProperties.GetName(day) });
                        }
                        await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, phase));
                        frames.Add(new { id, state, scheme = scheme.ToString(), width, calendar.ActualWidth, calendar.ActualHeight, month = calendar.ViewMonth.ToString("yyyy-MM"), days = actualDays, typography = DateTypography(calendar, [], null, null, canvas) }); await Save();
                    }
                    finally { if (state == "pressed") input.PointerButton(false); }
                }
                phase = scheme + "-controlled";
                var bound = new BleeckerCalendar(clock) { Width = width - 40 }; var model = new DateModel { Value = BleeckerDates.Parse("2026-10-17") };
                bound.SetBinding(BleeckerCalendar.ValueProperty, new Binding { Source = model, Path = new PropertyPath(nameof(DateModel.Value)) });
                DateTimeOffset? requested = null; bound.DateRequested += (_, args) => requested = args.Value; await Mount(bound);
                await input.ClickAsync(Day(bound, 19)); Require(BleeckerDates.Serialize(requested) == "2026-10-19" && BleeckerDates.Serialize(bound.Value) == "2026-10-17", "Calendar replaced its consumer value.");
                model.Value = BleeckerDates.Parse("2024-02-29"); await AwaitPresentationAsync(bound); Require(bound.ViewMonth == new DateTime(2026, 10, 1) && bound.GetBindingExpression(BleeckerCalendar.ValueProperty) is not null, "External value lost month retention or binding."); phases.Add(new { phase, requested = BleeckerDates.Serialize(requested), monthRetained = true, bindingRetained = true });
                phase = scheme + "-four-rows"; var shortMonth = new BleeckerCalendar(clock) { Value = BleeckerDates.Parse("2026-02-15"), Width = width - 40 }; await Mount(shortMonth); Require(shortMonth.ActualHeight == 218 && shortMonth.DayButtons.Count == 28, "Four-row February differs."); phases.Add(new { phase, height = shortMonth.ActualHeight, days = shortMonth.DayButtons.Count });
                phase = scheme + "-picker";
                var picker = new BleeckerDatePicker(clock) { Value = BleeckerDates.Parse("2026-10-17") }; AutomationProperties.SetName(picker, "Publication date");
                picker.DateRequested += (_, args) => picker.Value = args.Value; await Mount(picker); await input.ClickAsync(picker.Trigger); await Until(() => picker.Calendar?.IsLoaded == true, "Picker did not open.");
                await Until(() => picker.Calendar is { IsLoaded: true } current && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), current.PreviousButton), "Picker initial focus differs.");
                await input.ClickClosingSurfaceSourceAsync(Day(picker.Calendar!, 19), "picker-select-" + scheme); await Until(() => !picker.IsDropDownOpen, "Selection did not close picker."); Require(BleeckerDates.Serialize(picker.Value) == "2026-10-19" && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), picker.Trigger), "Picker selection/focus differs.");
                await input.ClickAsync(picker.Trigger); await Until(() => picker.ClearButton?.IsLoaded == true, "Clear action not available."); await input.ClickClosingSurfaceSourceAsync(picker.ClearButton!, "picker-clear-" + scheme); await Until(() => !picker.IsDropDownOpen, "Clear did not close picker."); Require(picker.Value is null, "Clear did not request null."); phases.Add(new { phase, selected = "2026-10-19", cleared = true, focusReturned = ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), picker.Trigger) });
                phase = scheme + "-dismissal";
                await input.ClickAsync(picker.Trigger); await Until(() => picker.Calendar is { IsLoaded: true } current && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), current.PreviousButton), "Dismissal calendar did not focus.");
                await input.HeldKeyAsync(0x1B, picker.Calendar!.PreviousButton, () => picker.Trigger, () => Until(() => !picker.IsDropDownOpen, "Escape did not close date popup."));
                await input.ClickAsync(picker.Trigger); await Until(() => picker.Calendar?.IsLoaded == true, "Outside-dismissal calendar did not open."); await input.ClickAsync(outside); Require(!picker.IsDropDownOpen && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), outside), "Outside dismissal did not keep outside focus.");
                phases.Add(new { phase, escapeFocusRestored = true, outsideFocusRetained = true });
                phase = scheme + "-rejected-and-remounted";
                var retained = new BleeckerDatePicker(clock) { Value = BleeckerDates.Parse("2026-10-17") }; DateTimeOffset? rejectedRequest = null; retained.DateRequested += (_, args) => rejectedRequest = args.Value; await Mount(retained);
                await input.ClickAsync(retained.Trigger); await Until(() => retained.Calendar?.IsLoaded == true, "Controlled popup did not open."); await input.ClickClosingSurfaceSourceAsync(Day(retained.Calendar!, 19), "rejected-" + scheme); Require(retained.Value == BleeckerDates.Parse("2026-10-17") && BleeckerDates.Serialize(rejectedRequest) == "2026-10-19" && !retained.IsDropDownOpen, "Rejected consumer request changed the value or left popup open.");
                await input.ClickAsync(retained.Trigger); await Until(() => retained.Calendar?.IsLoaded == true, "Live-value popup did not open."); retained.Value = BleeckerDates.Parse("2024-02-29"); Require(retained.Calendar!.ViewMonth == new DateTime(2026, 10, 1), "Live popup value changed its viewed month.");
                await input.ClickAsync(outside); await input.ClickAsync(retained.Trigger); await Until(() => retained.Calendar?.IsLoaded == true, "Remounted popup did not open."); Require(retained.Calendar!.ViewMonth == new DateTime(2024, 2, 1), "Reopening did not remount from the current value."); await input.ClickAsync(outside);
                phases.Add(new { phase, rejectedValueRetained = true, liveMonthRetained = true, reopenedMonth = "2024-02" });
                phase = scheme + "-lifecycle";
                var lifecycle = new BleeckerDatePicker(clock) { Value = BleeckerDates.Parse("2026-10-17") }; await Mount(lifecycle);
                await input.ClickAsync(lifecycle.Trigger); await Until(() => lifecycle.Calendar?.IsLoaded == true, "Lifecycle popup did not open.");
                lifecycle.IsEnabled = false; Require(!lifecycle.IsDropDownOpen, "Disabling left a date popup open."); lifecycle.IsEnabled = true;
                var staleTrigger = lifecycle.Trigger; var staleProvider = (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(staleTrigger).GetPattern(PatternInterface.ExpandCollapse);
                await input.ClickAsync(lifecycle.Trigger); await Until(() => lifecycle.Calendar?.IsLoaded == true, "Template popup did not open.");
                lifecycle.Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' TargetType='Control'><Grid x:Name='PART_Root'/></ControlTemplate>");
                lifecycle.ApplyTemplate(); await AwaitPresentationAsync(lifecycle); Require(!lifecycle.IsDropDownOpen && !ReferenceEquals(staleTrigger, lifecycle.Trigger), "Template replacement retained its date popup or trigger.");
                var staleRejected = false; try { staleProvider.Expand(); } catch (ElementNotAvailableException) { staleRejected = true; } Require(staleRejected, "Stale date trigger still expanded.");
                await input.ClickAsync(lifecycle.Trigger); await Until(() => lifecycle.Calendar?.IsLoaded == true, "Unmount popup did not open."); await Mount(new BleeckerCalendar(clock) { Width = width - 40 });
                Require(!lifecycle.IsDropDownOpen && VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Count == 0, "Unmount retained a date popup.");
                phases.Add(new { phase, disableClosed = true, templateClosed = true, staleProviderRejected = true, unmountClosed = true });
                phase = scheme + "-range";
                var range = new BleeckerDateRangePicker(clock) { Width = width - 40, Value = new("2026-10-10", "2026-10-20"), Presets = new[] { new BleeckerDatePreset("Seven days", 7) } };
                range.RangeRequested += (_, args) => range.Value = args.Value; await Mount(range); await input.ClickAsync(range.FromPicker.Trigger); await Until(() => range.FromPicker.Calendar?.IsLoaded == true, "Range start did not open.");
                Require(!Day(range.FromPicker.Calendar!, 21).IsEnabled, "Range start lost its opposite bound."); await input.ClickClosingSurfaceSourceAsync(Day(range.FromPicker.Calendar!, 15), "range-from-" + scheme); await Until(() => !range.FromPicker.IsDropDownOpen, "Range start did not close."); Require(range.Value == new BleeckerDateRange("2026-10-15", "2026-10-20"), "Range update discarded its other endpoint.");
                var preset = DateDescendants<BleeckerButton>(range).Single(x => x.Content as string == "Seven days"); await input.ClickAsync(DateDescendants<Button>(preset).Single(x => x.Name == "PART_Action")); Require(range.Value == new BleeckerDateRange("2026-10-10", "2026-10-17"), "Preset did not subtract local calendar days.");
                var originalPreset = preset; var action = DateDescendants<Button>(preset).Single(x => x.Name == "PART_Action"); action.Focus(FocusState.Keyboard); range.Presets = new[] { new BleeckerDatePreset("Seven days", 14) }; await AwaitPresentationAsync(range);
                Require(ReferenceEquals(originalPreset, DateDescendants<BleeckerButton>(range).Single(x => x.Content as string == "Seven days")) && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), action), "Keyed preset update replaced its button or lost focus."); await input.ClickAsync(action); Require(range.Value.From == "2026-10-03", "Retained preset invoked stale days.");
                phases.Add(new { phase, boundVerified = true, endpointRetained = true, preset = range.Value });
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"catalog-{scheme.ToString().ToLowerInvariant()}.png")); await Save();
            }
            passed = true;
        }
        finally { input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot); await Save(); }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed, component = "date-controls", environment = RuntimeEnvironment() }));
        Task Save() => File.WriteAllTextAsync(Path.Combine(folder, "date-controls.json"), JsonSerializer.Serialize(new { passed, phase, frames, phases }, new JsonSerializerOptions { WriteIndented = true }));
        async Task Mount(FrameworkElement value) { canvas.Children.Clear(); Canvas.SetLeft(value, 20); Canvas.SetTop(value, 20); canvas.Children.Add(value); Canvas.SetLeft(outside, 20); Canvas.SetTop(outside, 606); canvas.Children.Add(outside); await input!.MoveAsync(canvas, true, new Point(width - 5, 600)); await AwaitPresentationAsync(value); }
        static ToggleButton Day(BleeckerCalendar calendar, int number) => calendar.DayButtons.Single(x => x.Content.ToString() == number.ToString());
        static DateTimeOffset? SourceDate(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : BleeckerDates.Parse(value.GetString());
        static async Task Until(Func<bool> ready, string message) { var watch = Stopwatch.StartNew(); while (!ready() && watch.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16); Require(ready(), message); }
    }
    private static IEnumerable<T> DateDescendants<T>(DependencyObject root) where T : DependencyObject
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T match) yield return match; foreach (var nested in DateDescendants<T>(child)) yield return nested; } }
    [Bindable]
    public sealed class DateModel : System.ComponentModel.INotifyPropertyChanged
    {
        private DateTimeOffset? value;
        public DateTimeOffset? Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
}
