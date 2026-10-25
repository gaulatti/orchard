using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Orchard.Catalog;

public sealed partial class MediaLibrariesFixture : UserControl
{
    private readonly BleeckerMediaLibraryItem[] originals;
    private readonly HashSet<string> removed = new(StringComparer.Ordinal);
    private bool rejectSearch;
    public MediaLibrariesFixture()
    {
        InitializeComponent();
        SvgImageSource Image(int index, int width, int height) => new(new Uri($"ms-appx:///Assets/MediaLibrary/media-asset-{index}.svg")) { RasterizePixelWidth = width, RasterizePixelHeight = height };
        originals = [
            new BleeckerMediaLibraryItem("coast", "fictional-coast.svg") { Alt = "Original fictional coastal illustration", ThumbnailSource = Image(0,320,200), ImageSource = Image(1,640,400), Url = "https://fixtures.orchard.test/coast.svg?label=Original%20value#view", CreatedAt = "2026-03-10T10:00:00.000Z" },
            new BleeckerMediaLibraryItem("portrait", "fictional-portrait.svg") { Alt = "Original fictional portrait illustration", ImageSource = Image(2,240,480), Url = "https://fixtures.orchard.test/portrait.svg", CreatedAt = "2026-03-11T00:30:00.000Z" },
            new BleeckerMediaLibraryItem("thumbnail", "fictional-thumbnail-only.svg") { ThumbnailSource = Image(3,240,240) },
            new BleeckerMediaLibraryItem("none", "fictional-no-preview.bin"),
            new BleeckerMediaLibraryItem("broken", "fictional-broken-thumb.svg") { Alt = "Original broken thumbnail", ThumbnailSource = Image(5,320,200), ImageSource = Image(1,640,400), Url = "https://fixtures.orchard.test/coast.svg?label=Original%20value#view" },
            new BleeckerMediaLibraryItem("unicode", "Straße 東京 é العربية.svg") { Alt = " ", ImageSource = Image(4,480,320), Url = "https://fixtures.orchard.test/unicode.svg", CreatedAt = "2026-10-21T00:30:00.000Z" }
        ];
        Primary.SearchTermChangeRequested += (_, value) => { Result.Text = "Search requested: " + value; if (!rejectSearch) { Primary.SearchTerm = value; Rebuild(); } };
        Primary.CopyRequested += (_, value) => Result.Text = "Copy requested, original string: " + value;
        Primary.DeleteRequested += (_, id) => { removed.Add(id); Rebuild(); Result.Text = "Caller removed original ID: " + id; };
        Primary.ImageFailed += (_, item) => Result.Text = "Image failed for original ID: " + item.Id;
        Disabled.Items = originals.Take(2).ToArray(); Constrained.Items = originals.Take(2).ToArray(); Scaled.Items = originals.Take(1).ToArray();
        Rebuild();
    }
    private void Rebuild() => Primary.Items = originals.Where(i => !removed.Contains(i.Id) && (i.Filename + " " + i.Alt).Contains(Primary.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToArray();
    private void Restore(object sender, RoutedEventArgs args) { removed.Clear(); Primary.SearchTerm = ""; Rebuild(); Result.Text = "Restored original fictional objects"; }
    private void RejectSearch(object sender, RoutedEventArgs args) => rejectSearch = ((CheckBox)sender).IsChecked == true;
    private void Disable(object sender, RoutedEventArgs args) => Primary.IsEnabled = ((CheckBox)sender).IsChecked != true;
    private void Direction(object sender, RoutedEventArgs args) => Primary.FlowDirection = ((CheckBox)sender).IsChecked == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
    private void Rename(object sender, RoutedEventArgs args) => originals[0].Filename = "Updated original fictional coast.svg";
    private void RemoveFirst(object sender, RoutedEventArgs args) { removed.Add(originals[0].Id); Rebuild(); }
}
