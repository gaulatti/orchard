using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>A controlled calendar. Value changes are requested from its consumer.</summary>
public sealed class BleeckerCalendar : Control
{
    private Grid? host, dates, weekdays;
    private TextBlock? heading;
    private BleeckerIconButton? previous, next;
    private DateTime? month;
    private readonly List<Day> days = [];
    private readonly Func<DateTimeOffset> now;
    private readonly ResourceDictionary palette = BleeckerDateVisual.Palette();
    public BleeckerCalendar() : this(() => DateTimeOffset.Now) { }
    internal BleeckerCalendar(Func<DateTimeOffset> now)
    {
        this.now = now; DefaultStyleKey = typeof(BleeckerCalendar);
        SizeChanged += (_, _) => UpdateColumns();
        Loaded += (_, _) => { EnsureMonth(); RenderMonth(); };
        Unloaded += (_, _) => { if (!IsLoaded) month = null; };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) Detach(); });
    }
    private static DependencyProperty Property(string name) => DependencyProperty.Register(name, typeof(object), typeof(BleeckerCalendar), new PropertyMetadata(null, (owner, _) => ((BleeckerCalendar)owner).Refresh()));
    public static readonly DependencyProperty ValueProperty = Property(nameof(Value));
    public static readonly DependencyProperty MinDateProperty = Property(nameof(MinDate));
    public static readonly DependencyProperty MaxDateProperty = Property(nameof(MaxDate));
    public static readonly DependencyProperty DisabledDatesProperty = Property(nameof(DisabledDates));
    public DateTimeOffset? Value { get => (DateTimeOffset?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public DateTimeOffset? MinDate { get => (DateTimeOffset?)GetValue(MinDateProperty); set => SetValue(MinDateProperty, value); }
    public DateTimeOffset? MaxDate { get => (DateTimeOffset?)GetValue(MaxDateProperty); set => SetValue(MaxDateProperty, value); }
    public Func<DateTimeOffset, bool>? DisabledDates { get => (Func<DateTimeOffset, bool>?)GetValue(DisabledDatesProperty); set => SetValue(DisabledDatesProperty, value); }
    public event EventHandler<BleeckerDateRequestedEventArgs>? DateRequested;
    public DateTime ViewMonth { get { EnsureMonth(); return month!.Value; } }
    internal IReadOnlyList<ToggleButton> DayButtons => days;
    internal Button PreviousButton => previous ?? throw new InvalidOperationException("Calendar is not templated.");
    internal Button NextButton => next ?? throw new InvalidOperationException("Calendar is not templated.");
    private void EnsureMonth() { if (month is null) { var date = BleeckerDates.LocalDate(Value ?? now()); month = new(date.Year, date.Month, 1); } }
    protected override void OnApplyTemplate()
    {
        Detach(); base.OnApplyTemplate();
        host = GetTemplateChild("PART_Root") as Grid ?? throw new InvalidOperationException("Calendar requires PART_Root.");
        var layout = new StackPanel { Spacing = 0, UseLayoutRounding = false };
        var header = new Grid { Height = 32, Margin = new Thickness(0, 0, 0, 16), UseLayoutRounding = false };
        header.ColumnDefinitions.Add(new() { Width = GridLength.Auto }); header.ColumnDefinitions.Add(new()); header.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
        previous = Navigation("Previous month", "M15 18L9 12L15 6"); next = Navigation("Next month", "M9 18L15 12L9 6");
        previous.Click += Previous; next.Click += Next; header.Children.Add(previous); Grid.SetColumn(next, 2); header.Children.Add(next);
        heading = new TextBlock { Style = (Style)palette["BleeckerCalendarHeadingStyle"], HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(heading, 1); header.Children.Add(heading); layout.Children.Add(header);
        weekdays = new Grid { Height = 32, Margin = new Thickness(0, 0, 0, 4), UseLayoutRounding = false };
        var names = new[] { "SU", "MO", "TU", "WE", "TH", "FR", "SA" };
        for (var i = 0; i < 7; i++) { weekdays.ColumnDefinitions.Add(new()); var text = new TextBlock { Text = names[i], Style = (Style)palette["BleeckerCalendarWeekdayStyle"], HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }; AutomationProperties.SetAccessibilityView(text, AccessibilityView.Raw); Grid.SetColumn(text, i); weekdays.Children.Add(text); }
        layout.Children.Add(weekdays);
        dates = new Grid { RowSpacing = 2, UseLayoutRounding = false }; for (var i = 0; i < 7; i++) dates.ColumnDefinitions.Add(new());
        layout.Children.Add(dates); host.Children.Add(layout); EnsureMonth(); RenderMonth(); UpdateColumns();
    }
    private BleeckerIconButton Navigation(string name, string data)
    {
        var button = new BleeckerIconButton { Size = BleeckerIconButtonSize.Small, Variant = BleeckerIconButtonVariant.Ghost, UseLayoutRounding = false };
        AutomationProperties.SetName(button, name); button.Content = BleeckerDateVisual.Icon(data, 16, button, nameof(Foreground)); return button;
    }
    private void Previous(object sender, RoutedEventArgs args) => ChangeMonth(-1);
    private void Next(object sender, RoutedEventArgs args) => ChangeMonth(1);
    private void ChangeMonth(int delta) { if (!IsLoaded || !IsEnabled) return; month = ViewMonth.AddMonths(delta); RenderMonth(); }
    private void Detach()
    {
        if (previous is not null) previous.Click -= Previous;
        if (next is not null) next.Click -= Next;
        host?.Children.Clear(); days.Clear(); dates = null; weekdays = null; heading = null; previous = next = null; host = null;
    }
    private void UpdateColumns()
    {
        if (dates is null || weekdays is null || ActualWidth <= 0) return;
        var widths = BleeckerDates.Columns(ActualWidth);
        for (var i = 0; i < 7; i++) { dates.ColumnDefinitions[i].Width = new GridLength(widths[i]); weekdays.ColumnDefinitions[i].Width = new GridLength(widths[i]); }
    }
    private void RenderMonth()
    {
        if (dates is null || heading is null) return;
        var first = ViewMonth; heading.Text = first.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("en-US"));
        dates.Children.Clear(); dates.RowDefinitions.Clear(); days.Clear();
        var count = DateTime.DaysInMonth(first.Year, first.Month); var offset = (int)first.DayOfWeek;
        for (var row = 0; row < (offset + count + 6) / 7; row++) dates.RowDefinitions.Add(new() { Height = new GridLength(32) });
        for (var number = 1; number <= count; number++)
        {
            var day = new Day(this, BleeckerDates.Midnight(first.AddDays(number - 1))) { Name = "Day" + number, Content = number.ToString(CultureInfo.InvariantCulture), Style = (Style)palette["BleeckerCalendarDayStyle"], UseLayoutRounding = false };
            var index = offset + number - 1; Grid.SetRow(day, index / 7); Grid.SetColumn(day, index % 7); dates.Children.Add(day); days.Add(day);
        }
        previous!.IsEnabled = first.Year != 1 || first.Month != 1; next!.IsEnabled = first.Year != 9999 || first.Month != 12;
        Refresh();
    }
    private bool Blocked(DateTimeOffset date) => (MinDate is { } minimum && date < minimum) || (MaxDate is { } maximum && date > maximum) || DisabledDates?.Invoke(date) == true;
    private void Refresh()
    {
        foreach (var day in days)
        {
            day.IsEnabled = !Blocked(day.Date); day.IsChecked = Value is { } value && BleeckerDates.LocalDate(value) == day.Date.Date;
            day.Today = BleeckerDates.LocalDate(now()) == day.Date.Date; day.UpdateSelection();
        }
    }
    private void Request(Day day)
    {
        if (!IsLoaded || !IsEnabled || !day.IsEnabled || !days.Contains(day) || Blocked(day.Date)) return;
        DateRequested?.Invoke(this, new(day.Date)); Refresh();
    }
    private sealed class Day : ToggleButton
    {
        private readonly BleeckerCalendar owner;
        internal DateTimeOffset Date { get; }
        internal bool Today { get; set; }
        internal Day(BleeckerCalendar owner, DateTimeOffset date)
        {
            this.owner = owner; Date = date; RenderTransformOrigin = new Windows.Foundation.Point(.5, .5); RenderTransform = new ScaleTransform();
            RegisterPropertyChangedCallback(IsPressedProperty, (_, _) => { var scale = (ScaleTransform)RenderTransform; scale.ScaleX = scale.ScaleY = IsPressed ? .94 : 1; });
            AutomationProperties.SetName(this, date.Date.ToString("ddd MMM dd yyyy", CultureInfo.GetCultureInfo("en-US")));
        }
        protected override void OnToggle() => owner.Request(this);
        protected override void OnApplyTemplate() { base.OnApplyTemplate(); UpdateSelection(); }
        internal void UpdateSelection() => VisualStateManager.GoToState(this, IsChecked == true ? "DaySelected" : Today ? "DayToday" : "DayOrdinary", false);
    }
}

internal static class BleeckerDateVisual
{
    internal static ResourceDictionary Palette() => new() { Source = new Uri("ms-appx:///Orchard/Themes/DateControls.xaml") };
    internal static FrameworkElement Icon(string data, double size, DependencyObject source, string foreground)
    {
        var icon = (Viewbox)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' IsHitTestVisible='False'><Canvas Width='24' Height='24' FlowDirection='LeftToRight'><Path Data='" + data + "' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
        icon.Width = icon.Height = size;
        foreach (var shape in ((Canvas)icon.Child).Children.OfType<Microsoft.UI.Xaml.Shapes.Shape>()) shape.SetBinding(Microsoft.UI.Xaml.Shapes.Shape.StrokeProperty, new Binding { Source = source, Path = new PropertyPath(foreground) });
        AutomationProperties.SetAccessibilityView(icon, AccessibilityView.Raw); return icon;
    }
}
