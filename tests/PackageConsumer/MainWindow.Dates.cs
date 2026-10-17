using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Composition;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
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
            await PresentAsync(host); await AcquireConsumerInputAsync();
            Require(calendar.ActualHeight == 252, "Packaged calendar lost its measured month height.");
            var day = DateChildren<ToggleButton>(calendar).Single(x => x.Content.ToString() == "19");
            var previousDay = DateChildren<ToggleButton>(calendar).Single(x => x.Content.ToString() == "17");
            VerifySelectedShadow(previousDay);
            var dayText = DateChildren<TextBlock>(day).Single(x => x.Name == "DayText");
            var dayBaseline = ReferenceBaseline(dayText, 14);
            Require(Typography.GetNumeralAlignment(dayText) == FontNumeralAlignment.Tabular, "Packaged calendar lost tabular day numerals.");
            await KeyAsync(0x20, day, day); Require(calendar.Value == Date(19), "Packaged calendar lost its controlled date request.");
            Require(Shadow(previousDay) is null, "Packaged calendar retained its former selection shadow."); VerifySelectedShadow(day);
            phases.Add(new { phase = "calendar", value = calendar.Value, height = calendar.ActualHeight, dayBaseline, tabularNumerals = true, dayShadowTransferred = true });
            var trigger = DateChildren<Button>(picker).Single(x => x.Name == "PART_Trigger");
            var caption = DateChildren<TextBlock>(trigger).Single(); Require(caption.LineHeight == 20, "Packaged picker lost the source caption line height.");
            var captionBaseline = ReferenceBaseline(caption, 15);
            var triggerShadow = Shadow(trigger) ?? throw new InvalidOperationException("Packaged picker lost its native shadow.");
            Require(triggerShadow.R == 26 && triggerShadow.G == 55 && triggerShadow.B == 77 &&
                triggerShadow.A == 255 && ((triggerShadow.Sigma == 1 && triggerShadow.Opacity == .025f) || (triggerShadow.Sigma == 5 && triggerShadow.Opacity == .25f)), "Packaged picker lost its source shadow colour/alpha.");
            await KeyAsync(0x20, trigger, null); await AwaitAsync(() => picker.IsDropDownOpen, "Packaged picker did not open through Space.");
            var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Single(x => x.Child is not null && DateChildren<BleeckerCalendar>(x.Child).Any());
            var clearDate = DateChildren<Button>(popup.Child).Single(x => x.Content as string == "Clear date");
            await PresentAsync(clearDate); var clearWidth = clearDate.ActualWidth; var clearHeight = clearDate.ActualHeight;
            Require(clearWidth == 254 && clearHeight == 31, "Packaged picker lost its full-width clear action.");
            var chosen = DateChildren<ToggleButton>(popup.Child).Single(x => x.Content.ToString() == "19");
            await KeyAsync(0x0D, chosen, trigger); await AwaitAsync(() => !picker.IsDropDownOpen, "Packaged picker did not close after selection.");
            Require(picker.Value == Date(19) && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), trigger), "Packaged picker lost selection or focus restoration.");
            phases.Add(new { phase = "picker", value = picker.Value, focusRestored = true, captionLineHeight = caption.LineHeight, captionBaseline, clearWidth, clearHeight, triggerShadowVerified = true });
            var rangeFields = DateChildren<BleeckerDatePicker>(range).ToArray();
            var fromWidth = rangeFields.Single(x => AutomationProperties.GetName(x) == "Start date").ActualWidth;
            var toWidth = rangeFields.Single(x => AutomationProperties.GetName(x) == "End date").ActualWidth;
            Require(fromWidth == 130.421875 && toWidth == 132.125, "Packaged range lost source intrinsic field widths.");
            var preset = DateChildren<BleeckerButton>(range).Single(x => x.Content as string == "Package seven days");
            var presetBaseline = ReferenceBaseline(DateChildren<TextBlock>(preset).Single(), 12);
            var action = DateChildren<Button>(preset).Single(x => x.Name == "PART_Action");
            var today = DateTime.Now.Date; await KeyAsync(0x20, action, action);
            Require(range.Value == new BleeckerDateRange(today.AddDays(-7).ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd")), "Packaged range preset lost local dates.");
            phases.Add(new { phase = "range", value = range.Value, fromWidth, toWidth, presetBaseline }); Require(requests == 3, "Packaged date requests were missing or duplicated."); passed = true;
        }
        finally
        {
            Root.Children.Remove(host);
            var args = Environment.GetCommandLineArgs(); var report = args[Array.IndexOf(args, "--verify-output") + 1];
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(report)!, $"consumer-dates-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed, requests, phases }));
        }

        static (float Sigma, byte R, byte G, byte B, byte A, float Opacity)? Shadow(Control control)
        {
            var host = DateChildren<Grid>(control).Single(x => x.Name == "DateShadowHost");
            var visual = ElementCompositionPreview.GetElementChildVisual(host);
            if (visual is null) return null;
            Require(visual is SpriteVisual, "Packaged date shadow lost its sprite."); var sprite = (SpriteVisual)visual;
            Require(sprite.Brush is CompositionMaskBrush { Source: CompositionColorBrush, Mask: CompositionSurfaceBrush }, "Packaged date shadow lost its coloured output mask.");
            var outputMask = (CompositionMaskBrush)sprite.Brush;
            var color = ((CompositionColorBrush)outputMask.Source).Color;
            var filtered = (CompositionVisualSurface)((CompositionSurfaceBrush)outputMask.Mask).Surface;
            var brush = (CompositionEffectBrush)((SpriteVisual)filtered.SourceVisual).Brush;
            Require(brush.Properties.TryGetScalar("cssBlur.BlurAmount", out var sigma) == CompositionGetValueStatus.Succeeded && color.A == 255, "Packaged date shadow lost its blur/opaque colour.");
            return (sigma,color.R,color.G,color.B,color.A,sprite.Opacity);
        }
        static void VerifySelectedShadow(Control control)
        {
            var shadow = Shadow(control) ?? throw new InvalidOperationException("Packaged selection lost its native shadow.");
            Require(shadow == (1f,(byte)21,(byte)48,(byte)66,(byte)255,.12f), "Packaged selected-day shadow differs from the frozen source.");
        }
        static double ReferenceBaseline(TextBlock text, double expected)
        {
            Require(BleeckerTextLayout.GetUseReferenceLineBox(text), "Packaged date text lost reference line-box alignment.");
            var parent = (FrameworkElement)text.Parent;
            var baseline = text.TransformToVisual(parent).TransformPoint(new Point(0, text.BaselineOffset)).Y;
            Require(Math.Abs(baseline - expected) < .001, $"Packaged date baseline {baseline} differs from source {expected} for {text.Text}.");
            return baseline;
        }
        async Task KeyAsync(ushort key, Control target, Control? releaseTarget)
        {
            await PresentAsync(target); await AcquireConsumerInputAsync(); Require(target.Focus(FocusState.Keyboard), "Packaged date target refused native focus.");
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
                await AwaitAsync(() => downs == 1 && ups == 1, "Packaged date key lost native press/release."); await PresentAsync(host);
                Require(exact, "Packaged date key lost source or window ownership.");
                if (releaseTarget is not null) Require(ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), releaseTarget), "Packaged date key ended on the wrong focus owner.");
                phases.Add(new { phase = "native-key", key, downs, ups, exact });
            }
            finally { source.KeyDown -= down; source.KeyUp -= up; }
        }
        async Task PresentAsync(FrameworkElement target)
        {
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); Rect? previous = null; var stable = 0;
            void Rendering(object? sender, object args)
            {
                var box = target.TransformToVisual(Root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
                var visible = target.IsLoaded && box.Width > 0 && box.Height > 0 && box.Left >= 0 && box.Top >= 0 && box.Right <= Root.ActualWidth && box.Bottom <= Root.ActualHeight;
                stable = visible && previous == box ? stable + 1 : 0; previous = box; if (stable >= 2) ready.TrySetResult();
            }
            CompositionTarget.Rendering += Rendering;
            try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
            finally { CompositionTarget.Rendering -= Rendering; }
        }
    }
    private static IEnumerable<T> DateChildren<T>(DependencyObject root) where T : DependencyObject
    { for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) { var child = VisualTreeHelper.GetChild(root, i); if (child is T typed) yield return typed; foreach (var nested in DateChildren<T>(child)) yield return nested; } }
}
