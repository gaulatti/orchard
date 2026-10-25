using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public sealed partial class BleeckerHeader
{
    // Encode the combined solid alpha once, at the native byte-color boundary.
    // Leaving fractional opacity on a brush makes WinUI truncate it separately
    // during each layer's premultiplication. Caller brushes remain authoritative.
    private sealed class HeaderPaintBinding(BleeckerHeader owner, DependencyProperty property, Action<Brush?> assign)
    {
        private readonly SolidColorBrush encoded = new();
        private SolidColorBrush? source;
        private long ownerToken, colorToken, opacityToken;
        private bool observing;

        internal void Start()
        {
            if (observing) return;
            observing = true;
            ownerToken = owner.RegisterPropertyChangedCallback(property, (_, _) => ObserveSource());
            ObserveSource();
        }

        private void ObserveSource()
        {
            DisconnectSource();
            var brush = (Brush?)owner.GetValue(property);
            if (brush is not SolidColorBrush solid) { assign(brush); return; }
            source = solid;
            colorToken = solid.RegisterPropertyChangedCallback(SolidColorBrush.ColorProperty, (_, _) => Paint());
            opacityToken = solid.RegisterPropertyChangedCallback(Brush.OpacityProperty, (_, _) => Paint());
            Paint();
        }

        private void Paint()
        {
            var color = source!.Color;
            color.A = checked((byte)Math.Round(color.A * source.Opacity, MidpointRounding.AwayFromZero));
            encoded.Color = color;
            assign(encoded);
        }

        private void DisconnectSource()
        {
            source?.UnregisterPropertyChangedCallback(SolidColorBrush.ColorProperty, colorToken);
            source?.UnregisterPropertyChangedCallback(Brush.OpacityProperty, opacityToken);
            source = null;
        }

        internal void Stop()
        {
            if (!observing) return;
            owner.UnregisterPropertyChangedCallback(property, ownerToken);
            DisconnectSource();
            observing = false;
        }
    }
}
