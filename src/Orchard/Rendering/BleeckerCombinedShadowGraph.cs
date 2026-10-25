using System.Numerics;
using Microsoft.UI.Composition;

namespace Orchard;

// Owns only the replacement transaction and its two translated clips. The
// component owns the original visuals/geometry; each layer owns its candidate.
internal sealed class BleeckerCombinedShadowGraph
{
    internal enum Slot { Near, Far }
    private readonly Dictionary<ContainerVisual, Entry> entries = [];

    internal Entry Acquire(ContainerVisual container, Slot slot)
    {
        if (!entries.TryGetValue(container, out var entry))
        {
            entry = new(this, container);
            entries.Add(container, entry);
        }
        entry.Reserve(slot);
        return entry;
    }

    internal int ResourceCount => entries.Count * 2;
    internal object Snapshot() => new { entryCount = entries.Count, leases = entries.Values.Sum(e => e.Leases), ownedClipCount = ResourceCount };

    internal sealed class Entry
    {
        private readonly BleeckerCombinedShadowGraph owner;
        private readonly ContainerVisual container;
        private readonly CompositionGeometricClip originalOutside;
        private readonly CompositionClip? originalNearClip;
        private readonly CompositionClip? originalFarClip;
        private readonly bool originalNearVisible;
        private readonly bool originalFarVisible;
        private readonly CompositionGeometricClip nearClip;
        private readonly CompositionGeometricClip farClip;
        private bool nearReserved;
        private bool farReserved;
        private SpriteVisual? nearCandidate;
        private SpriteVisual? farCandidate;
        internal SpriteVisual Near { get; }
        internal SpriteVisual Far { get; }
        internal CompositionGeometricClip Outside => originalOutside;
        internal int Leases => (nearReserved ? 1 : 0) + (farReserved ? 1 : 0);

        internal Entry(BleeckerCombinedShadowGraph owner, ContainerVisual container)
        {
            var layers = container.Children.Cast<SpriteVisual>().ToArray();
            if (layers.Length != 2 || container.Clip is not CompositionGeometricClip outside)
                throw new InvalidOperationException("Combined attachment requires the original two-layer overlay graph.");
            this.owner = owner; this.container = container; originalOutside = outside;
            Near = layers[0]; Far = layers[1]; originalNearClip = Near.Clip; originalFarClip = Far.Clip;
            originalNearVisible = Near.IsVisible; originalFarVisible = Far.IsVisible;
            nearClip = container.Compositor.CreateGeometricClip(outside.Geometry);
            try { farClip = container.Compositor.CreateGeometricClip(outside.Geometry); }
            catch { nearClip.Dispose(); throw; }
        }

        internal void Reserve(Slot slot)
        {
            if (slot == Slot.Near)
            {
                if (nearReserved) throw new InvalidOperationException("The combined near slot already has an owner.");
                nearReserved = true;
            }
            else
            {
                if (farReserved) throw new InvalidOperationException("The combined far slot already has an owner.");
                farReserved = true;
            }
        }

        internal void Attach(Slot slot, SpriteVisual candidate)
        {
            if (slot == Slot.Near)
            {
                if (!nearReserved || nearCandidate is not null) throw new InvalidOperationException("Invalid combined near attachment.");
                container.Children.InsertAbove(candidate, Near); nearCandidate = candidate;
            }
            else
            {
                if (!farReserved || farCandidate is not null) throw new InvalidOperationException("Invalid combined far attachment.");
                container.Children.InsertAbove(candidate, Far); farCandidate = candidate;
            }
            ApplyOriginals();
        }

        internal void UpdateClips()
        {
            nearClip.TransformMatrix = originalOutside.TransformMatrix * Matrix3x2.CreateTranslation(-Near.Offset.X, -Near.Offset.Y);
            farClip.TransformMatrix = originalOutside.TransformMatrix * Matrix3x2.CreateTranslation(-Far.Offset.X, -Far.Offset.Y);
        }

        private void ApplyOriginals()
        {
            Near.IsVisible = nearCandidate is null && originalNearVisible;
            Far.IsVisible = farCandidate is null && originalFarVisible;
            if (Leases == 0)
            {
                Near.Clip = originalNearClip; Far.Clip = originalFarClip; container.Clip = originalOutside;
            }
            else
            {
                UpdateClips(); Near.Clip = nearClip; Far.Clip = farClip; container.Clip = null;
            }
        }

        internal void Release(Slot slot, SpriteVisual? candidate, bool restore)
        {
            if (slot == Slot.Near)
            {
                if (!nearReserved || (nearCandidate is not null && !ReferenceEquals(candidate, nearCandidate))) throw new InvalidOperationException("Unknown combined near owner.");
                nearReserved = false; nearCandidate = null;
            }
            else
            {
                if (!farReserved || (farCandidate is not null && !ReferenceEquals(candidate, farCandidate))) throw new InvalidOperationException("Unknown combined far owner.");
                farReserved = false; farCandidate = null;
            }
            // A cleared template has already disposed the borrowed originals.
            // Never inspect them when releasing a superseded or unloaded graph.
            if (restore)
            {
                if (candidate is not null && ReferenceEquals(candidate.Parent, container)) container.Children.Remove(candidate);
                ApplyOriginals();
            }
            if (Leases == 0)
            {
                farClip.Dispose(); nearClip.Dispose(); owner.entries.Remove(container);
            }
        }
    }
}
