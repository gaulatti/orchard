using System.Numerics;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed partial class BleeckerHeader
{
    private sealed class HeaderBackdrop : IDisposable
    {
        private readonly Grid host;
        private readonly DispatcherQueue uiQueue = DispatcherQueue.GetForCurrentThread() ?? throw new InvalidOperationException("Header backdrop requires a UI dispatcher.");
        private readonly AccessibilitySettings accessibility = new();
        private readonly UISettings settings = new();
        private CompositionBackdropBrush? source;
        private CompositionEffectFactory? factory;
        private CompositionEffectBrush? brush;
        private SpriteVisual? visual;
        private bool disposed;
        internal HeaderBackdrop(Grid host) { this.host = host; settings.ColorValuesChanged += Changed; }
        private void Changed(UISettings sender, object args) => uiQueue.TryEnqueue(() => { if (!disposed) Update(); });
        internal void Update()
        {
            if (disposed) return;
            if (!host.IsLoaded || accessibility.HighContrast || host.ActualWidth <= 0 || host.ActualHeight <= 0) { Clear(); return; }
            if (visual is null)
            {
                var compositor = ElementCompositionPreview.GetElementVisual(host).Compositor;
                source = compositor.CreateBackdropBrush();
                using var blur = new GaussianBlurEffect { BlurAmount = 8, BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("backdrop") };
                factory = compositor.CreateEffectFactory(blur); brush = factory.CreateBrush(); brush.SetSourceParameter("backdrop", source);
                visual = compositor.CreateSpriteVisual(); visual.Brush = brush; ElementCompositionPreview.SetElementChildVisual(host, visual);
            }
            visual.Size = new Vector2((float)host.ActualWidth, (float)host.ActualHeight);
        }
        private void Clear() { ElementCompositionPreview.SetElementChildVisual(host, null); visual?.Dispose(); brush?.Dispose(); factory?.Dispose(); source?.Dispose(); visual = null; brush = null; factory = null; source = null; }
        public void Dispose() { if (disposed) return; disposed = true; settings.ColorValuesChanged -= Changed; Clear(); }
    }
}
