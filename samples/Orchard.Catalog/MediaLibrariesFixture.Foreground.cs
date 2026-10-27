using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MediaLibrariesFixture
{
    internal async Task VerifyForegroundAsync(string folder, BleeckerColorScheme scheme)
    {
        removed.Clear(); Primary.SearchTerm = ""; Rebuild();
        await MainWindow.AwaitPresentationAsync(Primary);
        using var input = new NativeInputSession(XamlRoot, folder, $"media-library-{scheme}");
        await input.AcquireAsync(Result);

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        static IEnumerable<FrameworkElement> Elements(DependencyObject parent)
        {
            if (parent is FrameworkElement element) yield return element;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                foreach (var child in Elements(VisualTreeHelper.GetChild(parent, i))) yield return child;
        }
        async Task AwaitAsync(Func<bool> condition, string message)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2))
            {
                Primary.UpdateLayout();
                await Task.Delay(16);
            }
            Require(condition(), message);
        }
        (Grid Tile, Button Open, BleeckerIconButton Delete, Grid Overlay) Find(string id)
        {
            var tile = Elements(Primary).OfType<Grid>().Single(e => e.Name == "MediaTile" && e.Tag is BleeckerMediaLibraryItem item && item.Id == id);
            var children = Elements(tile).ToArray();
            return (tile, children.OfType<Button>().Single(e => e.Name == "MediaOpen"),
                children.OfType<BleeckerIconButton>().Single(e => e.Name == "MediaDelete"),
                children.OfType<Grid>().Single(e => e.Name == "MediaTileOverlay"));
        }

        var coast = Find("coast");
        await input.MoveAsync(coast.Open);
        await AwaitAsync(() => coast.Overlay.Opacity > .95 && coast.Delete.IsLoaded && coast.Delete.IsEnabled,
            "Pointer hover did not reveal the Media Library Delete control.");
        // The caller removes this tile in DeleteRequested, so its routed
        // PointerReleased can disappear with the target before ClickAsync
        // observes it. The callback and preview state are the delivery proof.
        await input.MoveAsync(coast.Delete);
        input.PointerButton(true);
        input.PointerButton(false);
        await AwaitAsync(() => Result.Text == "Caller removed original ID: coast" && !Primary.Items.Any(item => item.Id == "coast"),
            "Pointer Delete did not reach the caller with the original ID.");
        Require(!Primary.IsPreviewOpen && Primary.SelectedItem is null, "Pointer Delete opened a preview.");

        var portrait = Find("portrait");
        Require(portrait.Open.Focus(FocusState.Keyboard), "The Media Library Open control rejected keyboard focus.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), portrait.Open),
            "Keyboard focus did not reach the tile Open control.");
        input.Key(0x09);
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), portrait.Delete),
            "Tab did not reach the tile Delete control after Open.");
        // Space removes the focused tile before KeyUp can bubble to the root.
        // The focused target plus the original-ID callback prove activation.
        input.Key(0x20);
        await AwaitAsync(() => Result.Text == "Caller removed original ID: portrait" && !Primary.Items.Any(item => item.Id == "portrait"),
            "Keyboard Delete did not reach the caller with the original ID.");
        Require(!Primary.IsPreviewOpen && Primary.SelectedItem is null, "Keyboard Delete opened a preview.");

        var ordinary = Find("none");
        await input.ClickAsync(ordinary.Open);
        await AwaitAsync(() => Primary.IsPreviewOpen && ReferenceEquals(Primary.SelectedItem, originals.Single(item => item.Id == "none")),
            "Pointer Open did not retain the original tile item in the preview.");
        await AwaitAsync(() => Primary.PreviewInputAllowed, "Media Library preview did not become ready for input.");
        Primary.ClosePreview();
        await AwaitAsync(() => !Primary.IsPreviewOpen, "Media Library did not close its preview after the foreground check.");

        await File.WriteAllTextAsync(Path.Combine(folder, $"media-library-foreground-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, scheme, pointerDeleteOriginalId = "coast", pointerDeleteDidNotOpenPreview = true,
            tabFocusedDelete = true, keyboardDeleteOriginalId = "portrait", keyboardDeleteDidNotOpenPreview = true,
            pointerOpenOriginalObjectRetained = true
        }));
        removed.Clear(); Rebuild();
    }
}
