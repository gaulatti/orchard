using Microsoft.Graphics.Canvas.Text;

namespace Orchard;

// One authoritative font selection for a single chart render. Layouts remain
// per label; no font or consumer data is cached across renders.
internal sealed class BleeckerChartTextStyle : IDisposable
{
    private readonly CanvasFontSet? fonts;
    internal CanvasTextFormat Format { get; }
    internal BleeckerChartTextStyle(string family, float size, ushort weight)
    {
        CanvasTextFormat? format = null;
        try
        {
            var marker = family.IndexOf('#');
            if (marker >= 0 && Uri.TryCreate(family[..marker], UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == "ms-appx" && uri.Host.Length == 0)
                {
                    uri = new Uri(System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'))));
                    family = uri.AbsoluteUri + family[marker..];
                }
                fonts = new CanvasFontSet(uri);
                if (fonts.Fonts.Count == 0) throw new InvalidOperationException("Chart text cannot load its requested font.");
            }
            format = new CanvasTextFormat(); format.FontFamily = family; format.FontSize = size; format.FontWeight = new Windows.UI.Text.FontWeight { Weight = weight };
            Format = format;
        }
        catch { format?.Dispose(); fonts?.Dispose(); throw; }
    }
    public void Dispose() { Format.Dispose(); fonts?.Dispose(); }
}
