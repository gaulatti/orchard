using System.Numerics;
using Microsoft.UI.Xaml;

namespace Orchard;

// Internal combined preview. Public rendering stays on the original graph
// until complete geometry, scaling and original-image acceptance is verified.
internal sealed class BleeckerCombinedShadowLayer : IDisposable
{
    private readonly BleeckerOverlaySurface target;
    private readonly BleeckerCombinedShadowGraph graph = new();
    private readonly BleeckerNearShadowLayer near;
    private readonly BleeckerFarShadowLayer far;
    private bool disposed;

    internal BleeckerCombinedShadowLayer(BleeckerOverlaySurface target, Vector4 nearTint, Vector4 farTint)
    {
        this.target = target;
        var nearRecipe = BleeckerTokens.OverlayShadows[1];
        near = new(target, new Vector2(nearRecipe.X, nearRecipe.Y), nearTint, graph);
        try { far = new(target, farTint, graph); }
        catch { near.Dispose(); throw; }
        target.Unloaded += OnUnloaded;
    }

    private void OnUnloaded(object sender, RoutedEventArgs args) => Dispose();
    internal void InvalidateGeneratedResources() { near.InvalidateGeneratedResources(); far.InvalidateGeneratedResources(); }
    internal object Snapshot() => new
    {
        disposed, queued = near.RefreshQueued || far.RefreshQueued,
        graphResourceCount = near.GraphResourceCount + far.GraphResourceCount + graph.ResourceCount,
        near = near.Snapshot(), far = far.Snapshot(), graph = graph.Snapshot()
    };

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; target.Unloaded -= OnUnloaded;
        try { near.Dispose(); } finally { far.Dispose(); }
    }
}
