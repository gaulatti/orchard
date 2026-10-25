using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Orchard;

public sealed partial class BleeckerCommandSpotlight
{
    private sealed class SelectedFill : IDisposable
    {
        private readonly TextBlock slot;
        private readonly WriteableBitmap bitmap = new(1, 1);
        private readonly byte[] bytes = new byte[4];
        private readonly long slotToken;
        private SolidColorBrush? source;
        private long colorToken, opacityToken;
        private bool disposed, painted;
        internal ImageBrush Brush { get; }

        internal SelectedFill(TextBlock slot)
        {
            this.slot = slot; Brush = new() { ImageSource = bitmap, Stretch = Stretch.Fill };
            slotToken = slot.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty, (_, _) => Refresh()); Refresh();
        }
        internal void Refresh()
        {
            if (disposed) return;
            var next = slot.Foreground as SolidColorBrush ?? throw new InvalidOperationException("Spotlight requires a solid semantic selected-fill brush.");
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
            var color = source.Color; var value = BleeckerOklabFill.Encode(color.R, color.G, color.B, color.A, source.Opacity);
            if (painted && bytes[0] == value.B && bytes[1] == value.G && bytes[2] == value.R && bytes[3] == value.A) return;
            bytes[0] = value.B; bytes[1] = value.G; bytes[2] = value.R; bytes[3] = value.A;
            using (var stream = bitmap.PixelBuffer.AsStream()) stream.Write(bytes);
            bitmap.Invalidate(); painted = true;
        }
        private void Disconnect()
        {
            if (source is null) return;
            source.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, colorToken);
            source.UnregisterPropertyChangedCallback(Microsoft.UI.Xaml.Media.Brush.OpacityProperty, opacityToken); source = null;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true;
            slot.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty, slotToken); Disconnect(); Brush.ImageSource = null;
        }
    }
}
