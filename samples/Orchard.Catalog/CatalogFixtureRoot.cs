using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

internal static class CatalogFixtureRoot
{
    internal static Grid From(XamlRoot root) => root.Content switch
    {
        Grid grid => grid,
        BleeckerModalHost { Content: Grid grid } => grid,
        _ => throw new InvalidOperationException("The catalog fixture requires its content Grid.")
    };
}
