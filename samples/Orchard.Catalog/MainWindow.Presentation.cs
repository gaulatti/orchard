using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    // Nonanimated state checks need actual presentation, not a fixed sleep.
    internal static async Task AwaitPresentationAsync(FrameworkElement element)
    {
        element.UpdateLayout();
        var presented = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var frames = 0;
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        void Rendering(object? sender, object args) { if (++frames == 2) presented.TrySetResult(); }
        CompositionTarget.Rendering += Rendering;
        try { await presented.Task.WaitAsync(deadline.Token); }
        finally { CompositionTarget.Rendering -= Rendering; }
        await ElementCompositionPreview.GetElementVisual(element).Compositor.RequestCommitAsync().AsTask().WaitAsync(deadline.Token);
    }
}
