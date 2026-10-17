using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyDatesAsync()
    {
        var phases = new List<object>(); var requests = 0; var passed = false;
        DateTimeOffset Date(int day) => new(new DateTime(2026, 10, day), TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 10, day)));
        var layout = new StackPanel { Width = 360, Spacing = 12 };
        var calendar = new BleeckerCalendar { Width = 280, HorizontalAlignment = HorizontalAlignment.Left, Value = Date(17) };
        calendar.DateRequested += (_, args) => { requests++; calendar.Value = args.Value; };
        var picker = new BleeckerDatePicker { Value = Date(17) }; AutomationProperties.SetName(picker, "Packaged publication date");
        picker.DateRequested += (_, args) => { requests++; picker.Value = args.Value; };
        var range = new BleeckerDateRangePicker { Value = new("2026-10-10", "2026-10-20"), Presets = new[] { new BleeckerDatePreset("Package seven days", 7) } };
        range.RangeRequested += (_, args) => { requests++; range.Value = args.Value; };
        layout.Children.Add(calendar); layout.Children.Add(picker); layout.Children.Add(range);
        var host = new Border { Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = Root.Background, Child = layout };
        Root.Children.Add(host);
        try
        {
            await AwaitTextareaPresentationAsync(); await AcquireConsumerInputAsync();
            Require(calendar.ActualHeight == 252, "Packaged calendar lost its measured month height.");
            var day = DateChildren<ToggleButton>(calendar).Single(x => x.Content.ToString() == "19");
            await KeyAsync(0x20, day, day); Require(calendar.Value == Date(19), "Packaged calendar lost its controlled date request.");
            phases.Add(new { phase = "calendar", value = calendar.Value, height = calendar.ActualHeight });
            var trigger = DateChildren<Button>(picker).Single(x => x.Name == "PART_Trigger");
            await KeyAsync(0x20, trigger, null); await AwaitAsync(() => picker.IsDropDownOpen, "Packaged picker did not open through Space.");
            var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Single(x => DateChildren<BleeckerCalendar>(x.Child).Any());
            var chosen = DateChildren<ToggleButton>(popup.Child).Single(x => x.Content.ToString() == "19");
            await KeyAsync(0x0D, chosen, trigger); await AwaitAsync(() => !picker.IsDropDownOpen, "Packaged picker did not close after selection.");
            Require(picker.Value == Date(19) && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), trigger), "Packaged picker lost selection or focus restoration.");
            phases.Add(new { phase = "picker", value = picker.Value, focusRestored = true });
            var preset = DateChildren<BleeckerButton>(range).Single(x => x.Content as string == "Package seven days");
            var action = DateChildren<Button>(preset).Single(x => x.Name == "PART_Action");
            var today = DateTime.Now.Date; await KeyAsync(0x20, action, action);
            Require(range.Value == new BleeckerDateRange(today.AddDays(-7).ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd")), "Packaged range preset lost local dates.");
            phases.Add(new { phase = "range", value = range.Value }); Require(requests == 3, "Packaged date requests were missing or duplicated."); passed = true;
        }
        finally
        {
            Root.Children.Remove(host); await AwaitTextareaPresentationAsync();
            var args = Environment.GetCommandLineArgs(); var report = args[Array.IndexOf(args, "--verify-output") + 1];
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(report)!, $"consumer-dates-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed, requests, phases }));
        }

        async Task KeyAsync(ushort key, Control target, Control? releaseTarget)
        {
            await AcquireConsumerInputAsync(); Require(target.Focus(FocusState.Keyboard), "Packaged date target refused native focus.");
            var island = ContentIsland.FindAllForCurrentThread().Single(x => ReferenceEquals(x.Environment, Root.XamlRoot.ContentIslandEnvironment));
            var source = InputKeyboardSource.GetForIsland(island) ?? throw new InvalidOperationException("Packaged date input source unavailable.");
            var downs = 0; var ups = 0; var exact = true;
            void Observe(InputKeyboardSource sender, Microsoft.UI.Input.KeyEventArgs args, bool up)
            {
                if ((ushort)args.VirtualKey != key) return;
                exact &= ReferenceEquals(source, sender) && args.KeyStatus.RepeatCount == 1 && args.KeyStatus.IsKeyReleased == up && GetForegroundWindow() == WinRT.Interop.WindowNative.GetWindowHandle(this);
                if (up) ups++; else downs++;
            }
            var down = new TypedEventHandler<InputKeyboardSource, Microsoft.UI.Input.KeyEventArgs>((s, e) => Observe(s, e, false)); var up = new TypedEventHandler<InputKeyboardSource, Microsoft.UI.Input.KeyEventArgs>((s, e) => Observe(s, e, true));
            source.KeyDown += down; source.KeyUp += up;
            try
            {
                var inputs = new[] { new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = key } } }, new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = key, Flags = 2 } } } };
                Require(SendInput(2, inputs, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged date key was not injected.");
                await AwaitAsync(() => downs == 1 && ups == 1, "Packaged date key lost native press/release."); await AwaitTextareaPresentationAsync();
                Require(exact, "Packaged date key lost source or window ownership.");
                if (releaseTarget is not null) Require(ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), releaseTarget), "Packaged date key ended on the wrong focus owner.");
                phases.Add(new { phase = "native-key", key, downs, ups, exact });
            }
            finally { source.KeyDown -= down; source.KeyUp -= up; }
        }
    }
    private static IEnumerable<T> DateChildren<T>(DependencyObject root) where T : DependencyObject
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T typed) yield return typed; foreach (var nested in DateChildren<T>(child)) yield return nested; } }
}
