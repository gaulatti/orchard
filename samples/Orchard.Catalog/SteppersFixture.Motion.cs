using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard.Catalog;

public sealed partial class SteppersFixture
{
    internal async Task VerifyMotionAsync(BleeckerColorScheme scheme, string folder)
    {
        using var reference = JsonDocument.Parse(CatalogReferenceResources.Open(typeof(SteppersFixture).Assembly, "Orchard.Catalog.Reference.StepperMotion.json")!);
        var enabled = new UISettings().AnimationsEnabled;
        var reports = new List<object>(); var failures = new List<string>();
        stepper.Steps = [new BleeckerStep("stage", "Draft") { Description = "Write a story" }];
        stepper.Width = 640; stepper.Orientation = Orientation.Horizontal; stepper.ActiveStep = null; stepper.FlowDirection = FlowDirection.LeftToRight;
        try
        {
            foreach (var spec in reference.RootElement.GetProperty("transitions").EnumerateArray().Where(item => item.GetProperty("scheme").GetString() == scheme.ToString() && item.GetProperty("reduced").GetBoolean() != enabled))
            {
                var theme = spec.GetProperty("kind").GetString() == "theme";
                var from = spec.GetProperty("from").GetString()!; var to = spec.GetProperty("to").GetString()!;
                stepper.RequestedTheme = scheme == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark;
                stepper.Steps[0].Status = Enum.Parse<BleeckerStepStatus>(theme ? spec.GetProperty("status").GetString()! : from, true);
                await SettleAsync();
                var button = Buttons().Single();
                var fill = (SolidColorBrush)Descendants<Border>(button).Single(item => item.Name == "CircleFill").Background;
                var foreground = (SolidColorBrush)Descendants<TextBlock>(button).Single().Foreground;
                var before = new MotionSample(0, Channels(fill), Channels(foreground));
                var samples = new List<MotionSample>(); var watch = Stopwatch.StartNew();
                if (theme) stepper.RequestedTheme = to == "Light" ? ElementTheme.Light : ElementTheme.Dark;
                else stepper.Steps[0].Status = Enum.Parse<BleeckerStepStatus>(to, true);
                var immediate = new MotionSample(watch.Elapsed.TotalMilliseconds, Channels(fill), Channels(foreground));
                void Frame(object? sender, object args) => samples.Add(new(watch.Elapsed.TotalMilliseconds, Channels(fill), Channels(foreground)));
                CompositionTarget.Rendering += Frame;
                try
                {
                    await Task.Delay(240); await MainWindow.AwaitPresentationAsync(stepper);
                }
                finally { CompositionTarget.Rendering -= Frame; }
                var after = new MotionSample(watch.Elapsed.TotalMilliseconds, Channels(fill), Channels(foreground));
                var errors = new List<string>();
                Check(before.Background, spec.GetProperty("before").GetProperty("background"), "initial background");
                Check(before.Foreground, spec.GetProperty("before").GetProperty("foreground"), "initial foreground");
                Check(after.Background, spec.GetProperty("after").GetProperty("background"), "settled background");
                Check(after.Foreground, spec.GetProperty("after").GetProperty("foreground"), "settled foreground");
                var backgroundChanged = !Same(before.Background, after.Background); var foregroundChanged = !Same(before.Foreground, after.Foreground);
                foreach (var sample in samples)
                {
                    CheckTrajectory(before.Background, after.Background, sample.Background, "background");
                    CheckTrajectory(before.Foreground, after.Foreground, sample.Foreground, "foreground");
                }
                if (enabled)
                {
                    if (backgroundChanged && !samples.Any(sample => !Same(sample.Background, before.Background) && !Same(sample.Background, after.Background))) errors.Add("background skipped intermediate native frames");
                    if (foregroundChanged && !samples.Any(sample => !Same(sample.Foreground, before.Foreground) && !Same(sample.Foreground, after.Foreground))) errors.Add("foreground skipped intermediate native frames");
                }
                else if (samples.Any(sample => !Same(sample.Background, after.Background) || !Same(sample.Foreground, after.Foreground))) errors.Add("reduced motion retained intermediate colors");
                reports.Add(new { kind = theme ? "theme" : "status", from, to, status = theme ? spec.GetProperty("status").GetString() : null, before, immediate, samples, after, backgroundChanged, foregroundChanged, errors });
                failures.AddRange(errors.Select(error => $"{(theme ? "theme" : "status")} {from}->{to}: {error}"));
                void Check(double[] actual, JsonElement expected, string label)
                {
                    if (actual.Where((value, channel) => Math.Abs(value - expected[channel].GetDouble()) > (channel == 3 ? 1 : 2)).Any()) errors.Add(label + " differs from the browser");
                }
                void CheckTrajectory(double[] first, double[] last, double[] current, string label)
                {
                    var axis = Enumerable.Range(0, 3).MaxBy(channel => Math.Abs(last[channel] - first[channel]));
                    var range = last[axis] - first[axis]; if (range == 0) return;
                    var progress = (current[axis] - first[axis]) / range;
                    // Two quantized channels each contribute at most half a byte.
                    if (Enumerable.Range(0, 3).Any(channel => Math.Abs(current[channel] - (first[channel] + (last[channel] - first[channel]) * progress)) > 1.5))
                        errors.Add(label + " departed from the source sRGB trajectory");
                }
            }
            var callbackRemovalPreservedMotion = true;
            if (enabled)
            {
                stepper.RequestedTheme = scheme == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark;
                stepper.Steps[0].Status = BleeckerStepStatus.Pending; await SettleAsync();
                var fill = (SolidColorBrush)Descendants<Border>(Buttons().Single()).Single(item => item.Name == "CircleFill").Background;
                stepper.Steps[0].Status = BleeckerStepStatus.Completed;
                await Task.Delay(40); await MainWindow.AwaitPresentationAsync(stepper);
                stepper.StepInvoked -= Invoked;
                try
                {
                    var during = fill.Color;
                    Require(!Buttons().Single().IsEnabled, "Callback removal retained input during a color transition.");
                    await SettleAsync();
                    callbackRemovalPreservedMotion = during != fill.Color;
                    Require(callbackRemovalPreservedMotion, "Callback removal snapped an unchanged running color transition.");
                }
                finally { stepper.StepInvoked += Invoked; }
            }
            stepper.RequestedTheme = scheme == BleeckerColorScheme.Light ? ElementTheme.Light : ElementTheme.Dark;
            stepper.Steps[0].Status = BleeckerStepStatus.Active; await SettleAsync();
            var target = Buttons().Single(); var circle = Descendants<Grid>(target).Single(item => item.Name == "Circle");
            var scale = (ScaleTransform)circle.RenderTransform;
            var presses = new List<object>(); var instantScale = true;
            var token = target.RegisterPropertyChangedCallback(ButtonBase.IsPressedProperty, (_, _) =>
            {
                var expected = target.IsPressed ? .94 : 1;
                instantScale &= Math.Abs(scale.ScaleX - expected) < .000001 && scale.ScaleX == scale.ScaleY;
                presses.Add(new { target.IsPressed, scale.ScaleX, scale.ScaleY, expected });
            });
            try
            {
                using var input = new NativeInputSession(XamlRoot, folder, $"stepper-motion-{scheme}");
                await input.AcquireAsync(target); target.Focus(FocusState.Keyboard); await MainWindow.AwaitPresentationAsync(target);
                await input.HeldKeyAsync(0x20, target, () => target, () => MainWindow.AwaitPresentationAsync(target));
                Require(instantScale && presses.Count == 2 && !target.IsPressed && scale.ScaleX == 1, "Native pressed scale did not change immediately at its IsPressed boundary.");
            }
            finally { target.UnregisterPropertyChangedCallback(ButtonBase.IsPressedProperty, token); }
            await File.WriteAllTextAsync(Path.Combine(folder, $"stepper-motion-{scheme}.json"), JsonSerializer.Serialize(new { passed = failures.Count == 0, enabled, scheme = scheme.ToString(), reports, presses, instantScale, callbackRemovalPreservedMotion, failures }, new JsonSerializerOptions { WriteIndented = true }));
            Require(failures.Count == 0, "Stepper motion differs from the source: " + string.Join("; ", failures.Take(8)));
        }
        finally { stepper.RequestedTheme = ElementTheme.Default; stepper.Steps = BasicSteps(); stepper.ActiveStep = "review"; }
    }
    private sealed record MotionSample(double Milliseconds, double[] Background, double[] Foreground);
    private static double[] Channels(SolidColorBrush brush) => [brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A * brush.Opacity];
    private static bool Same(double[] first, double[] second) => first.SequenceEqual(second);
}
