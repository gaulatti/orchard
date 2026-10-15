using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class TooltipsFixture
{
    private readonly List<object> tooltipPresentations = [];
    private async Task AwaitTooltipPresentationAsync(BleeckerTooltip tip, BleeckerColorScheme scheme, string folder)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var watch = Stopwatch.StartNew(); var stable = 0; var frames = 0;
        (PlacementMode Placement, double Width, double Height, Windows.Foundation.Point Origin)? previous = null;
        var observations = new List<object>();
        void Presented(object? sender, object args)
        {
            try
            {
                frames++;
                var motion = Descendants<FrameworkElement>(tip).SingleOrDefault(element => element.Name == "PART_MotionRoot");
                var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).SingleOrDefault(item => ReferenceEquals(item.Child, tip));
                var settled = tip.IsOpen && tip.IsLoaded && popup?.IsOpen == true && motion?.Opacity == 1 &&
                    motion.RenderTransform is ScaleTransform { ScaleX: 1, ScaleY: 1 } && tip.ActualWidth > 0 && tip.ActualHeight > 0;
                if (!settled) { previous = null; stable = 0; observations.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, settled }); return; }
                var origin = tip.TransformToVisual(tip.PlacementTarget).TransformPoint(new Windows.Foundation.Point());
                var state = (tip.ActualPlacement, tip.ActualWidth, tip.ActualHeight, origin);
                stable = previous == state ? stable + 1 : 0; previous = state;
                observations.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, settled, stable, placement = tip.ActualPlacement.ToString(), tip.ActualWidth, tip.ActualHeight, origin });
                if (stable >= 2) ready.TrySetResult();
            }
            catch (Exception error) { ready.TrySetException(error); }
        }
        CompositionTarget.Rendering += Presented;
        try { await ready.Task.WaitAsync(TimeSpan.FromSeconds(2)); }
        finally
        {
            CompositionTarget.Rendering -= Presented;
            tooltipPresentations.Add(new { tip.Name, milliseconds = watch.Elapsed.TotalMilliseconds, frames, stable, passed = ready.Task.IsCompletedSuccessfully, observations });
            await File.WriteAllTextAsync(Path.Combine(folder, $"tooltip-presentation-{scheme}.json"), JsonSerializer.Serialize(tooltipPresentations, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    private Task AwaitTooltipClosedAsync(BleeckerTooltip tip) => AwaitVerificationAsync(() => Require(!tip.IsOpen &&
        !VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot).Any(item => ReferenceEquals(item.Child, tip)), "Completed tooltip dismissal retained its popup."));
}
