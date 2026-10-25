using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunDatePresentationAsync(string folder)
    {
        using var resource = CatalogReferenceResources.Open(Assembly.GetExecutingAssembly(), "Orchard.Catalog.Reference.DateControls.json") ?? throw new InvalidOperationException("Missing dates reference.");
        using var reference = await JsonDocument.ParseAsync(resource);
        var original = Content;
        var canvas = new Canvas { Width = 640, Height = 640, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
        var bodyBackground = new Border { IsHitTestVisible = false };
        var scroll = new ScrollViewer { Content = canvas, HorizontalScrollMode = ScrollMode.Enabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden, VerticalScrollMode = ScrollMode.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, ZoomMode = ZoomMode.Disabled };
        var page = new Page { Content = scroll, Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
        scroll.ViewChanged += (_, args) =>
        {
            if (args.IsIntermediate || page.XamlRoot is null) return;
            var scale = page.XamlRoot.RasterizationScale; var offset = Math.Round(scroll.HorizontalOffset * scale, MidpointRounding.AwayFromZero) / scale;
            if (Math.Abs(offset - scroll.HorizontalOffset) > .0001) scroll.ChangeView(offset, null, null, true);
        };
        var outside = new Button { Content = "Outside", MinHeight = 0, MinWidth = 0, Padding = new Thickness(0), FontSize = 16, FontFamily = (FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"], Template = (ControlTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><TextBlock Text='{TemplateBinding Content}' FontFamily='{TemplateBinding FontFamily}' FontSize='16' LineHeight='24' LineStackingStrategy='BlockLineHeight' Foreground='{ThemeResource BleeckerTextPrimaryBrush}'/></ControlTemplate>") };
        var frames = new List<object>(); var errors = new List<object>(); var phase = "mount"; NativeInputSession? input = null;
        Func<DateTimeOffset> clock = () => BleeckerDates.Midnight(new DateTime(2026, 10, 17)).AddHours(12);
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "date-presentation"); await input.AcquireAsync(canvas);
            var width = (int)page.XamlRoot.Size.Width; Require(width is 320 or 640, "Date presentation requires a 320 or 640 client.");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                page.RequestedTheme = theme;
                bodyBackground.Background = new SolidColorBrush(theme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 13, 24, 33));
                foreach (var source in reference.RootElement.GetProperty("cases").EnumerateArray().Where(row => row.GetProperty("kind").GetString() != "calendar" && row.GetProperty("width").GetInt32() == width && row.GetProperty("scheme").GetString() == theme.ToString()))
                {
                    phase = source.GetProperty("file").GetString()!;
                    var id = source.GetProperty("id").GetString()!; var kind = source.GetProperty("kind").GetString()!; var state = source.GetProperty("state").GetString()!;
                    BleeckerDatePicker active; FrameworkElement control; Border? card = null; BleeckerDateRangePicker? rangeControl = null;
                    var triggers = new List<Button>(); var buttons = new List<FrameworkElement>();
                    if (kind == "picker")
                    {
                        var picker = new BleeckerDatePicker(clock) { Value = Date(source.GetProperty("value")), IsEnabled = id != "disabled" };
                        if (id == "long-placeholder") picker.Placeholder = "Choose the editorial publication date for this collection";
                        control = picker; await Mount(control); active = picker; triggers.Add(picker.Trigger); buttons.Add(picker.Trigger);
                    }
                    else
                    {
                        var value = source.GetProperty("value");
                        var range = new BleeckerDateRangePicker(clock) { Width = width - 40, Value = new(Field(value, "from"), Field(value, "to")) };
                        if (id == "presets") range.Presets = new[] { new BleeckerDatePreset("Seven days", 7), new BleeckerDatePreset("Today", 0) };
                        rangeControl = range; control = range; await Mount(control);
                        card = DateDescendants<Border>(range).Single(item => item.MinWidth == 360);
                        canvas.Width = Math.Max(width, 40 + Math.Max(range.ActualWidth, card.ActualWidth)); await AwaitPresentationAsync(range);
                        active = id == "to-limit" ? range.ToPicker : range.FromPicker;
                        triggers.Add(range.FromPicker.Trigger); triggers.Add(range.ToPicker.Trigger); buttons.AddRange(triggers);
                        buttons.AddRange(DateDescendants<BleeckerButton>(range).Where(item => item.Visibility == Visibility.Visible));
                    }
                    if (state == "open")
                    {
                        await input.ClickAsync(active.Trigger);
                        await Until(() => active.Calendar is { IsLoaded: true } current && ReferenceEquals(FocusManager.GetFocusedElement(page.XamlRoot), current.PreviousButton), "Presentation popup did not focus.");
                    }
                    else if (id == "disabled") { await input.ClickAsync(active.Trigger); Require(!active.IsDropDownOpen, "Disabled presentation opened."); }
                    await AwaitPresentationAsync(control);
                    var startErrors = errors.Count;
                    CompareBox("root", control, source.GetProperty("root").GetProperty("box"));
                    CompareNumber("viewport", "scrollX", scroll.HorizontalOffset, source.GetProperty("viewport").GetProperty("scrollX").GetDouble());
                    var expectedTriggers = source.GetProperty("triggers").EnumerateArray().ToArray();
                    Require(triggers.Count == expectedTriggers.Length, "Date trigger count differs.");
                    for (var index = 0; index < triggers.Count; index++)
                    {
                        CompareBox("trigger-" + index, triggers[index], expectedTriggers[index].GetProperty("box"));
                        Require(triggers[index].IsEnabled != expectedTriggers[index].GetProperty("disabled").GetBoolean(), "Date trigger enabled state differs.");
                        var text = DateDescendants<TextBlock>(triggers[index]).Single();
                        var paint = reference.RootElement.GetProperty("paints").GetProperty(expectedTriggers[index].GetProperty("paintRef").GetString()!);
                        CompareNumber("trigger-" + index, "fontSize", text.FontSize, CssNumber(paint.GetProperty("fontSize")));
                        CompareNumber("trigger-" + index, "lineHeight", text.LineHeight, CssNumber(paint.GetProperty("lineHeight")));
                        CompareNumber("trigger-" + index, "fontWeight", text.FontWeight.Weight, CssNumber(paint.GetProperty("fontWeight")));
                    }
                    var expectedButtons = source.GetProperty("buttons").EnumerateArray().ToArray();
                    Require(buttons.Count == expectedButtons.Length, "Date action count differs.");
                    for (var index = 0; index < buttons.Count; index++) CompareBox("button-" + index, buttons[index], expectedButtons[index].GetProperty("box"));
                    if (card is not null) CompareBox("range-card", card, source.GetProperty("rangeCard").GetProperty("box"));
                    FrameworkElement? surface = null;
                    if (state == "open")
                    {
                        var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(page.XamlRoot).Single();
                        surface = DateDescendants<BleeckerOverlaySurface>(popup.Child).Single();
                        CompareBox("popup", surface, source.GetProperty("popup").GetProperty("box"));
                        var calendar = active.Calendar!; CompareBox("calendar", calendar, source.GetProperty("calendar").GetProperty("box"));
                        foreach (var expected in source.GetProperty("calendar").GetProperty("days").EnumerateArray())
                        {
                            var day = calendar.DayButtons.Single(item => item.Content.ToString() == expected.GetProperty("day").GetInt32().ToString());
                            CompareBox("day-" + day.Content, day, expected.GetProperty("box"));
                            Require(day.IsEnabled != expected.GetProperty("disabled").GetBoolean() && day.IsChecked == expected.GetProperty("pressed").GetBoolean(), "Popup day state differs.");
                        }
                        if (source.TryGetProperty("clearAction", out var expectedClear))
                        {
                            var action = active.ClearButton ?? throw new InvalidOperationException("Missing selected popup clear action.");
                            CompareBox("clear-action", action, expectedClear.GetProperty("box"));
                            var parent = (FrameworkElement)action.Parent; CompareBox("clear-parent", parent, expectedClear.GetProperty("parent"));
                            CompareNumber("clear-action", "bottomGap", Box(parent).Bottom - Box(action).Bottom, expectedClear.GetProperty("bottomGap").GetDouble());
                        }
                    }
                    var actual = new { id, kind, state, width, scheme = theme.ToString(), root = Box(control), scrollX = scroll.HorizontalOffset, rangeCard = card is null ? (Rect?)null : Box(card), popup = surface is null ? (Rect?)null : Box(surface),
                        triggers = triggers.Select(item => new { box = Box(item), item.IsEnabled, item.FontWeight.Weight, chrome = Chrome(item), shadow = DateShadowProbe.Read(item), texts = DateDescendants<TextBlock>(item).Select(text => new { text.Text, text.FontFamily.Source, text.FontSize, weight = text.FontWeight.Weight, text.LineHeight, foreground = Brush(text.Foreground) }).ToArray() }).ToArray(),
                        rangePaint = card is null ? null : new { background = Brush(card.Background), border = Brush(card.BorderBrush), radius = card.CornerRadius.TopLeft, borderWidth = card.BorderThickness.Left },
                        popupPaint = surface is null ? null : new { background = Brush(((BleeckerOverlaySurface)surface).Background), border = Brush(((BleeckerOverlaySurface)surface).BorderBrush), radius = ((BleeckerOverlaySurface)surface).CornerRadius.TopLeft, borderWidth = ((BleeckerOverlaySurface)surface).BorderThickness.Left },
                        popupShadow = surface is null ? null : OverlayShadowProbe.Read((BleeckerOverlaySurface)surface),
                        calendar = active.Calendar is not { } popupCalendar ? null : new { box = Box(popupCalendar), month = popupCalendar.ViewMonth.ToString("MMMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
                            days = popupCalendar.DayButtons.Select(day => new { day = int.Parse(day.Content.ToString()!, System.Globalization.CultureInfo.InvariantCulture), box = Box(day), enabled = day.IsEnabled, selected = day.IsChecked, name = AutomationProperties.GetName(day), shadow = DateShadowProbe.Read(day) }).ToArray() },
                        clearAction = active.ClearButton is not { Visibility: Visibility.Visible } clearAction ? null : new { box = Box(clearAction), parent = Box((FrameworkElement)clearAction.Parent), bottomGap = Box((FrameworkElement)clearAction.Parent).Bottom - Box(clearAction).Bottom },
                        buttons = buttons.Select(item => new { box = Box(item), name = AutomationProperties.GetName(item) }).ToArray(), errors = errors.Count - startErrors,
                        typography = DateTypography(active.Calendar, triggers, rangeControl, active.ClearButton, page) };
                    frames.Add(actual); await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, phase)); await Save();
                    if (active.IsDropDownOpen) await input.HeldKeyAsync(0x1B, active.Calendar!.PreviousButton, () => active.Trigger, () => Until(() => !active.IsDropDownOpen, "Presentation popup did not close."));
                }
                await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            }
            Require(frames.Count == 28, "Date presentation omitted frozen reference frames.");
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = errors.Count == 0, component = "date-presentation", errors = errors.Count, environment = RuntimeEnvironment() }));
        }
        finally { input?.Dispose(); Content = original; await AwaitPresentationAsync(CatalogRoot); await Save(); }

        Task Save() => File.WriteAllTextAsync(Path.Combine(folder, "date-presentation.json"), JsonSerializer.Serialize(new { passed = errors.Count == 0 && frames.Count == 28, phase, frames, errors }, new JsonSerializerOptions { WriteIndented = true }));
        Rect Box(FrameworkElement item) => item.TransformToVisual(page).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
        void CompareBox(string name, FrameworkElement item, JsonElement expected)
        { var actual = Box(item); CompareNumber(name, "x", actual.X, expected.GetProperty("x").GetDouble()); CompareNumber(name, "y", actual.Y, expected.GetProperty("y").GetDouble()); CompareNumber(name, "width", actual.Width, expected.GetProperty("width").GetDouble()); CompareNumber(name, "height", actual.Height, expected.GetProperty("height").GetDouble()); }
        void CompareNumber(string name, string property, double actual, double expected) { if (Math.Abs(actual - expected) >= .001) errors.Add(new { phase, element = name, property, actual, expected }); }
        async Task Mount(FrameworkElement item)
        {
            canvas.Children.Clear(); canvas.Width = (int)page.XamlRoot.Size.Width;
            // Match the browser body's viewport-wide paint while its overflowing
            // document scrolls. The underlying browser canvas remains white.
            bodyBackground.Width = page.XamlRoot.Size.Width; bodyBackground.Height = page.XamlRoot.Size.Height; canvas.Children.Add(bodyBackground);
            Canvas.SetLeft(item, 20); Canvas.SetTop(item, 20); canvas.Children.Add(item);
            Canvas.SetLeft(outside, 20); Canvas.SetTop(outside, 606); canvas.Children.Add(outside); scroll.ChangeView(0, 0, null, true); await AwaitPresentationAsync(item);
            canvas.Width = Math.Max(canvas.Width, item.ActualWidth + 40); await AwaitPresentationAsync(item); await input!.MoveAsync(outside);
        }
        static object? Brush(Brush brush) => brush is SolidColorBrush color ? new { color = color.Color.ToString(), opacity = color.Opacity } : null;
        static object Chrome(Button button)
        {
            var border = DateDescendants<Border>(button).Single(item => item.Name == "Chrome");
            var root = DateDescendants<Grid>(button).Single(item => item.Name == "TriggerRoot");
            return new { background = Brush(border.Background), border = Brush(border.BorderBrush), opacity = root.Opacity * button.Opacity, radius = border.CornerRadius.TopLeft, borderWidth = border.BorderThickness.Left };
        }
        static double CssNumber(JsonElement value) => double.Parse(value.GetString()!.Replace("px", ""), System.Globalization.CultureInfo.InvariantCulture);
        static string? Field(JsonElement value, string name) => value.TryGetProperty(name, out var field) ? field.GetString() : null;
        static DateTimeOffset? Date(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : BleeckerDates.Parse(value.GetString());
        static async Task Until(Func<bool> ready, string message) { var timer = Stopwatch.StartNew(); while (!ready() && timer.ElapsedMilliseconds < 2000) await Task.Delay(16); Require(ready(), message); }
    }
}
