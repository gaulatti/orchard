using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public sealed partial class BleeckerCommandSpotlight
{
    // The source's color-mix tint is rasterized into an isolated backdrop layer.
    // Keep the caller's semantic brush intact and encode only that rendered fill.
    private sealed class BackdropPaint : IDisposable
    {
        private readonly TextBlock slot;
        private readonly Border target;
        private readonly SolidColorBrush rendered = new();
        private readonly long slotToken;
        private SolidColorBrush? source;
        private long colorToken, opacityToken;
        private bool disposed;

        internal BackdropPaint(TextBlock slot, Border target)
        {
            this.slot = slot; this.target = target; target.Background = rendered;
            slotToken = slot.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => Refresh());
            Refresh();
        }
        internal void Refresh()
        {
            if (disposed) return;
            var next = slot.Foreground as SolidColorBrush ?? throw new InvalidOperationException("Spotlight requires a solid semantic backdrop brush.");
            if (!ReferenceEquals(source, next))
            {
                Disconnect(); source = next;
                colorToken = source.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => Paint());
                opacityToken = source.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Media.Brush.OpacityProperty, (_, _) => Paint());
            }
            Paint();
        }
        private void Paint()
        {
            if (disposed || source is null) return;
            var color = source.Color;
            // Match the source's single-precision mix with transparent, then its
            // isolated layer's byte alpha. Neither step changes source properties.
            var alpha = 1f - (1f - (float)source.Opacity);
            color.A = (byte)Math.Floor(color.A * (double)alpha);
            if (rendered.Color != color) rendered.Color = color;
        }
        private void Disconnect()
        {
            if (source is null) return;
            source.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, colorToken);
            source.UnregisterPropertyChangedCallback(Microsoft.UI.Xaml.Media.Brush.OpacityProperty, opacityToken);
            source = null;
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            slot.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, slotToken);
            Disconnect(); target.Background = null;
        }
    }
}
