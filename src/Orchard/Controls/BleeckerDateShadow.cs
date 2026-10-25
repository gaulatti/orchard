using Microsoft.UI.Xaml.Controls;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;

// Preserve the date controls' existing recipes and API while sharing the renderer.
internal sealed class BleeckerDateShadow : IDisposable
{
    internal sealed record Recipe(float Blur, float Y, float Spread, float Opacity, byte R, byte G, byte B);
    internal static readonly Recipe Selected = new(2, 1, 0, .12f, 21, 48, 66);
    internal static readonly Recipe Rest = new(2, 1, 0, .025f, 26, 55, 77);
    internal static readonly Recipe Hover = new(10, 3, -8, .25f, 26, 55, 77);
    private readonly BleeckerCssShadow shadow;
    internal BleeckerDateShadow(Control owner, Grid host, ShapePath mask, ShapePath exterior) => shadow = new(owner, owner, host, mask, exterior);
    internal void Set(Recipe? recipe) => shadow.Set(recipe is null ? null : new(recipe.Blur, recipe.Y, recipe.Spread, recipe.Opacity, recipe.R, recipe.G, recipe.B));
    public void Dispose() => shadow.Dispose();
}
