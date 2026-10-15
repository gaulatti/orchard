using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace Orchard.Catalog;

public sealed partial class FieldHelpFixture : UserControl
{
    public FieldHelpFixture() => InitializeComponent();

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
        await MainWindow.AwaitPresentationAsync(this);
        using var input = new NativeInputSession(XamlRoot, folder, $"field-help-{scheme}");
        await input.AcquireAsync(Heading); await input.MoveAsync(Heading);
        Require(Help.ActualWidth == 16 && Help.ActualHeight == 16 && EmptyHelp.Visibility == Visibility.Collapsed, "Field help trigger or empty-text geometry differs.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Help);
        Require(peer.GetAutomationControlType() == AutomationControlType.Button && peer.GetName() == Help.Text, "Field help lost its button role or help-text name.");
        var root = (Grid)VisualTreeHelper.GetChild(Help, 0);
        var glyph = (Viewbox)root.FindName("InfoGlyph");
        var circle = (Ellipse)root.FindName("InfoCircle");
        Require(glyph.ActualWidth == 14 && glyph.ActualHeight == 14 && circle.Width == 22 && circle.StrokeThickness == 2, "Lucide info geometry differs.");
        var motionEvidence = new List<object>();
        var animationsEnabled = new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        foreach (var state in new[] { "Normal", "PointerOver", "Pressed", "Disabled" })
        {
            var samples = new List<(double Milliseconds, Windows.UI.Color Color, double Scale)>();
            var before = ((SolidColorBrush)circle.Stroke).Color;
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var started = false;
            void Sample(object? sender, object args)
            {
                if (!started) { ready.TrySetResult(); return; }
                samples.Add((elapsed.Elapsed.TotalMilliseconds, ((SolidColorBrush)circle.Stroke).Color, ((ScaleTransform)root.RenderTransform).ScaleX));
            }
            CompositionTarget.Rendering += Sample;
            try
            {
                await ready.Task.WaitAsync(TimeSpan.FromSeconds(2));
                elapsed.Restart();
                started = true;
                Require(VisualStateManager.GoToState(Help, state, true), $"Missing field help state {state}.");
                await Task.Delay(300);
            }
            finally { CompositionTarget.Rendering -= Sample; }
            var expected = BleeckerTokens.Colors(scheme)[state == "PointerOver" || (state == "Pressed" && Help.IsPointerOver) ? "textPrimary" : "textSecondary"];
            var color = ((SolidColorBrush)circle.Stroke).Color;
            Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == expected, "Field help state uses an incorrect icon color.");
            var scale = (ScaleTransform)root.RenderTransform;
            var expectedScale = state == "Pressed" ? .9 : 1;
            Require(Math.Abs(scale.ScaleX - expectedScale) < .000001 && Math.Abs(scale.ScaleY - expectedScale) < .000001,
                $"Field help scale differs: {state}/{scale.ScaleX}/{scale.ScaleY}.");
            if (animationsEnabled && state == "PointerOver")
                Require(samples.Any(sample => sample.Color != before && sample.Color != color), $"Enabled field help color transition produced no intermediate frame: before={before}, after={color}, frames={string.Join(';', samples.Select(sample => $"{sample.Milliseconds:F0}:{sample.Color}/{sample.Scale}"))}.");
            if (animationsEnabled && state == "Pressed")
                Require(samples.Any(sample => sample.Scale > .900001 && sample.Scale < .999999), $"Enabled field help scale transition produced no intermediate frame: {string.Join(';', samples.Select(sample => $"{sample.Milliseconds:F0}:{sample.Scale}"))}.");
            if (before == color)
                Require(samples.All(sample => sample.Color == color), "An unchanged field help color drifted during scale motion.");
            motionEvidence.Add(new { state, animationsEnabled, samples = samples.Select(sample => new { sample.Milliseconds, color = $"#{sample.Color.A:X2}{sample.Color.R:X2}{sample.Color.G:X2}{sample.Color.B:X2}", sample.Scale }) });
        }
        await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"field-help-motion-{scheme}.json"), System.Text.Json.JsonSerializer.Serialize(motionEvidence));
        // RenderTargetBitmap capture can stall the next compositor frame. Collect all
        // transition samples before requesting static state captures.
        foreach (var state in new[] { "Normal", "PointerOver", "Pressed", "Disabled" })
        {
            VisualStateManager.GoToState(Help, state, true);
            await Task.Delay(250);
            await MainWindow.CaptureElementAsync(Help, System.IO.Path.Combine(folder, $"field-help-{state}-{scheme}.png"));
        }
        VisualStateManager.GoToState(Help, "Normal", false);
        Require(Help.Focus(FocusState.Keyboard), "Field help did not accept keyboard focus.");
        await Task.Delay(180);
        var tooltip = BleeckerTooltipService.GetToolTip(Help)!;
        Require(tooltip.IsOpen && tooltip.Placement == PlacementMode.Bottom && Equals(tooltip.Content, Help.Text), "Field help did not open bottom help on keyboard focus.");
        Require(((Ellipse)root.FindName("HelpFocus")).Opacity == 1, "Field help keyboard focus ring is absent.");
        await MainWindow.CaptureElementAsync(tooltip, System.IO.Path.Combine(folder, $"field-help-content-{scheme}.png"));
        var activations = 0;
        RoutedEventHandler onClick = (_, _) => activations++;
        Help.Click += onClick;
        try
        {
            ((Microsoft.UI.Xaml.Automation.Provider.IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(50);
            Require(!tooltip.IsOpen && activations == 1 && ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot), Help),
                "Field help activation did not dismiss while preserving consumer activation and focus.");
        }
        finally { Help.Click -= onClick; }
        await VerifyLiveBrushAsync(circle, root, scheme, folder);
        tooltip.IsOpen = true;
        var original = Help.Text;
        Help.Text = "Updated help";
        Require(Equals(tooltip.Content, Help.Text) && peer.GetName() == Help.Text, "Changed help did not reach content and accessibility.");
        Help.Text = " \t";
        Require(Help.Visibility == Visibility.Collapsed && !tooltip.IsOpen, "Empty help left its trigger or popup active.");
        Help.Text = original;
        Require(Help.Visibility == Visibility.Visible, "Restored help did not restore its trigger.");
    }

    private async Task VerifyLiveBrushAsync(Ellipse circle, Grid root, BleeckerColorScheme scheme, string folder)
    {
        var normal = new SolidColorBrush(Microsoft.UI.Colors.DarkBlue);
        var hover = new SolidColorBrush(Microsoft.UI.Colors.DarkRed);
        var trace = new List<object>();
        var motionField = typeof(BleeckerFieldHelpTooltip).GetField("motion", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var sourceField = typeof(BleeckerFieldHelpTooltip).GetField("observedColor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var states = VisualStateManager.GetVisualStateGroups(root).Single(group => group.Name == "CommonStates");
        try
        {
            Help.Foreground = normal;
            Help.PointerOverForeground = hover;
            VisualStateManager.GoToState(Help, "Normal", false);
            await AwaitSettledAsync("Normal", normal);
            normal.Color = Microsoft.UI.Colors.DarkGreen;
            Record("normal-mutated");
            Require(((SolidColorBrush)circle.Stroke).Color == normal.Color, "Field help ignored an in-place normal brush update.");
            VisualStateManager.GoToState(Help, "PointerOver", false);
            await AwaitSettledAsync("PointerOver", hover);
            hover.Color = Microsoft.UI.Colors.DarkOrange;
            Record("hover-mutated");
            Require(((SolidColorBrush)circle.Stroke).Color == hover.Color, "Field help ignored an in-place hover brush update.");
            normal.Color = Microsoft.UI.Colors.Purple;
            Record("inactive-mutated");
            Require(((SolidColorBrush)circle.Stroke).Color == hover.Color, "An inactive field help brush changed the visible icon.");
            var replacement = new SolidColorBrush(Microsoft.UI.Colors.Teal);
            Help.PointerOverForeground = replacement;
            hover.Color = Microsoft.UI.Colors.Black;
            Record("hover-replaced");
            Require(((SolidColorBrush)circle.Stroke).Color == replacement.Color, "Field help retained a replaced brush subscription.");
            replacement.Color = Microsoft.UI.Colors.Navy;
            Record("replacement-mutated");
            Require(((SolidColorBrush)circle.Stroke).Color == replacement.Color, "Field help did not observe the replacement brush.");
        }
        finally
        {
            Record("cleanup");
            await System.IO.File.WriteAllTextAsync(System.IO.Path.Combine(folder, $"field-help-brush-{scheme}.json"), System.Text.Json.JsonSerializer.Serialize(trace));
            Help.ClearValue(BleeckerFieldHelpTooltip.ForegroundProperty);
            Help.ClearValue(BleeckerFieldHelpTooltip.PointerOverForegroundProperty);
            VisualStateManager.GoToState(Help, "Normal", false);
        }

        async Task AwaitSettledAsync(string state, SolidColorBrush expected)
        {
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            // An interpolated byte can equal its target before the storyboard
            // releases the effective Color value. Mutate a settled source only
            // after the actual clock and source subscription reach that state.
            bool Settled() => states.CurrentState?.Name == state && motionField.GetValue(Help) is null
                && ReferenceEquals(sourceField.GetValue(Help), expected) && ((SolidColorBrush)circle.Stroke).Color == expected.Color;
            Record($"await-{state}-start");
            while (!Settled() && elapsed.Elapsed < TimeSpan.FromSeconds(1))
                await Task.Delay(16);
            if (Settled())
            {
                var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                void OnFrame(object? sender, object args) => frame.TrySetResult();
                CompositionTarget.Rendering += OnFrame;
                try { await frame.Task.WaitAsync(TimeSpan.FromSeconds(1)); }
                finally { CompositionTarget.Rendering -= OnFrame; }
            }
            Record($"await-{state}-end");
            Require(Settled(), "Field help did not settle its requested palette state and animation clock.");
        }
        void Record(string phase)
        {
            var motion = motionField.GetValue(Help) as Microsoft.UI.Xaml.Media.Animation.Storyboard;
            var source = sourceField.GetValue(Help) as SolidColorBrush;
            trace.Add(new { phase, state = states.CurrentState?.Name, Help.IsPointerOver,
                rendered = ((SolidColorBrush)circle.Stroke).Color.ToString(), source = source?.Color.ToString(),
                observed = ReferenceEquals(source, normal) ? "normal" : ReferenceEquals(source, hover) ? "hover" : "other",
                clock = motion is null ? "none" : motion.GetCurrentState().ToString(),
                milliseconds = motion?.GetCurrentTime().TotalMilliseconds });
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
