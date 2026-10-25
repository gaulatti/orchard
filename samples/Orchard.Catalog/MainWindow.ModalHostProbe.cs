using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    internal bool ModalRootClosedRefreshVerified { get; private set; }
    // The actual catalog root, navigation and fictional demos in a hidden
    // unactivated window. This never calls the foreground smoke helpers.
    internal async Task<List<object>> ProbeModalRootAsync(Action guard, string outputFolder)
    {
        AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1100, 800));
        var rows = new List<object>();
        async Task Layout()
        {
            var idle = new TaskCompletionSource(); Require(DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain hidden catalog root."); await idle.Task; await Task.Delay(20);
            for (var i = 0; i < 4; i++) { ModalHost.Measure(new Size(1100, 800)); ModalHost.Arrange(new Rect(0, 0, 1100, 800)); ModalHost.UpdateLayout(); guard(); await Task.Delay(10); }
            Require(ModalHost.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && ModalHost.XamlRoot.Size == new Size(1100, 800), "Catalog probe lost its hidden unscaled viewport.");
            Require(Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(ModalHost.XamlRoot).Count == 0, "Catalog probe opened a native popup.");
            Require(ReferenceEquals(Content, ModalHost) && ReferenceEquals(ModalHost.Content, CatalogRoot) && ReferenceEquals(BleeckerModalHost.Require(CatalogRoot), ModalHost) && CatalogRoot.ActualWidth == 1100 && CatalogRoot.ActualHeight == 800, "Catalog root wrapper changed content or viewport.");
        }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            var captured = new HashSet<string>();
            ThemeSelector.SelectedIndex = scheme == "Light" ? 1 : 2;
            foreach (var fixture in new FrameworkElement[] { Foundation, ScrollAreas, Popovers, HoverCards, MonitoringWorkspace, EditorialWorkspace, Foundation })
            {
                ShowFixture(fixture); await Layout();
                Require(fixture.IsLoaded && ReferenceEquals(selectedFixture, fixture) && FixtureHost.Children.Count == 1 && ReferenceEquals(FixtureHost.Children[0], fixture), "Catalog root lost the original selected fixture.");
                Require(ModalHost.ActualTheme == CatalogRoot.ActualTheme && ModalHost.ActualTheme.ToString() == scheme && ModalHost.ActiveSurface is null && !ModalHost.BackgroundPointerBlocked, "Catalog root theme or idle isolation differs.");
                object? capture = null;
                if ((ReferenceEquals(fixture, Foundation) || ReferenceEquals(fixture, MonitoringWorkspace) || ReferenceEquals(fixture, EditorialWorkspace)) && captured.Add(fixture.Name))
                {
                    var file = "catalog-" + scheme.ToLowerInvariant() + "-" + fixture.Name + ".png"; var target = Path.Combine(outputFolder, file); var size = await CaptureElementAsync(ModalHost, target).WaitAsync(TimeSpan.FromSeconds(3)); guard();
                    Require(size.Width == 1100 && size.Height == 800, "Catalog diagnostic capture changed the viewport.");
                    capture = new { file, sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(target))), width = size.Width, height = size.Height };
                }
                rows.Add(new { scheme, fixture = fixture.Name, width = CatalogRoot.ActualWidth, height = CatalogRoot.ActualHeight, originalFixtureRetained = true, themeMatched = true, modalActive = false, capture });
            }
            var surface = new Grid { Width = 200, Height = 100 }; var overlay = new Grid(); overlay.Children.Add(surface); var registration = ModalHost.Attach(CatalogRoot, surface, overlay, () => { }); ModalHost.Activate(registration); await Layout();
            Require(ModalHost.BackgroundPointerBlocked && ReferenceEquals(ModalHost.ActiveSurface, surface), "Actual catalog root failed to own its diagnostic layer."); ModalHost.Deactivate(registration); registration.Dispose(); await Layout();
            rows.Add(new { scheme, fixture = "diagnostic-modal-layer", width = CatalogRoot.ActualWidth, height = CatalogRoot.ActualHeight, originalFixtureRetained = true, themeMatched = true, modalActive = false, capture = (object?)null });
        }
        BleeckerDisabledPointer.RefreshRoot(ModalHost); Close();
        BleeckerDisabledPointer.RefreshRoot(ModalHost);
        var closed = new TaskCompletionSource(); Require(DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => closed.SetResult()), "Could not drain closed catalog root."); await closed.Task; await Task.Delay(50);
        BleeckerDisabledPointer.RefreshRoot(ModalHost); guard(); ModalRootClosedRefreshVerified = true; return rows;
    }
}
