using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

namespace Orchard;

public sealed partial class BleeckerMediaLibrary
{
    private sealed class TileBackdrop : IDisposable
    {
        internal Border Host { get; } = new() { Name = "MediaTileBackdrop", IsHitTestVisible = false, UseLayoutRounding = false };
        private CompositionBackdropBrush? source;
        private CompositionEffectFactory? factory;
        private CompositionEffectBrush? brush;
        private SpriteVisual? visual;
        internal TileBackdrop() { Host.Loaded += Loaded; Host.Unloaded += Unloaded; Host.SizeChanged += SizeChanged; }
        private void Loaded(object sender, RoutedEventArgs args) => Update();
        private void Unloaded(object sender, RoutedEventArgs args) => Clear();
        private void SizeChanged(object sender, SizeChangedEventArgs args) => Update();
        private void Update()
        {
            if (!Host.IsLoaded || Host.ActualWidth <= 0 || Host.ActualHeight <= 0) return;
            if (visual is null) { var compositor = ElementCompositionPreview.GetElementVisual(Host).Compositor; source = compositor.CreateBackdropBrush(); using var blur = new GaussianBlurEffect { BlurAmount = 2, BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("backdrop") }; factory = compositor.CreateEffectFactory(blur); brush = factory.CreateBrush(); brush.SetSourceParameter("backdrop", source); visual = compositor.CreateSpriteVisual(); visual.Comment = "OrchardMediaBackdropBlur2"; visual.Brush = brush; ElementCompositionPreview.SetElementChildVisual(Host, visual); }
            visual.Size = new((float)Host.ActualWidth, (float)Host.ActualHeight);
        }
        private void Clear() { ElementCompositionPreview.SetElementChildVisual(Host, null); visual?.Dispose(); brush?.Dispose(); factory?.Dispose(); source?.Dispose(); visual = null; brush = null; factory = null; source = null; }
        public void Dispose() { Host.Loaded -= Loaded; Host.Unloaded -= Unloaded; Host.SizeChanged -= SizeChanged; Clear(); }
    }
}
