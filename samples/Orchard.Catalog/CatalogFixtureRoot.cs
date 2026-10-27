using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

internal static class CatalogFixtureRoot
{
    internal static Grid From(XamlRoot root) => root.Content switch
    {
        Grid grid => grid,
        BleeckerModalHost { Content: Grid grid } => grid,
        _ => throw new InvalidOperationException("The catalog fixture requires its content Grid.")
    };

    internal static Border PopupBorder(XamlRoot root)
    {
        var legacy = VisualTreeHelper.GetOpenPopupsForXamlRoot(root)
            .Select(popup => popup.Child).OfType<Border>()
            .Where(border => border.Name == "PopupBorder");
        var hosted = Descendants<Border>(root.Content).Where(border => border.Name == "PopupBorder" && border.IsLoaded);
        return legacy.Concat(hosted).Single();
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T item) yield return item;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
