using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunLoadingOverlayWindowsAsync(string folder)
    {
        var original = Content; var originalSize = AppWindow.ClientSize; var originalPosition = AppWindow.Position;
        var primary = new Page { RequestedTheme = ElementTheme.Light }; var secondary = new Page { RequestedTheme = ElementTheme.Dark };
        var primaryHost = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
        var secondaryHost = new Canvas { Background = new SolidColorBrush(Microsoft.UI.Colors.Black) };
        primary.Content = primaryHost; secondary.Content = secondaryHost;
        var primaryAction = Action("PrimaryAction"); var secondaryAction = Action("SecondaryAction");
        primaryHost.Children.Add(primaryAction); secondaryHost.Children.Add(secondaryAction);
        var primaryCount = 0; var secondaryCount = 0;
        primaryAction.Click += (_, _) => primaryCount++; secondaryAction.Click += (_, _) => secondaryCount++;
        var overlay = new BleeckerLoadingOverlay { Label = "Shared operation 日本語" }; AutomationProperties.SetName(overlay, "Shared operation");
        Canvas.SetLeft(overlay, 57.25); Canvas.SetTop(overlay, 101.5); primaryHost.Children.Add(overlay);
        Window? other = null; var otherClosed = false; NativeInputSession? firstInput = null, secondInput = null;
        XamlRoot? firstRoot = null, secondRoot = null; long? primaryWindow = null, secondaryWindow = null;
        var reports = new List<object>(); var phase = "mount"; var passed = false;
        try
        {
            Content = primary; await AwaitPresentationAsync(primary); firstRoot = primary.XamlRoot;
            firstInput = new NativeInputSession(firstRoot, folder, "loading-overlay-windows-primary"); await firstInput.AcquireAsync(primaryAction);
            primaryWindow = firstInput.Window.ToInt64();
            Require(primaryAction.Focus(FocusState.Keyboard), "Could not establish primary-window focus.");
            overlay.IsVisible = true; await AwaitPresentationAsync(primary); await CheckAsync("primary-open", primary, firstInput, primaryAction);
            await NativeScreenCapture.CaptureClientAsync(firstInput.Window, Path.Combine(folder, "catalog-light.png"));
            other = new Window { Title = "Orchard loading overlay transfer validation", Content = secondary };
            other.Closed += (_, _) => otherClosed = true; other.Activate(); await AwaitPresentationAsync(secondary); secondRoot = secondary.XamlRoot;
            secondInput = new NativeInputSession(secondRoot, folder, "loading-overlay-windows-secondary"); await secondInput.AcquireAsync(secondaryAction);
            secondaryWindow = secondInput.Window.ToInt64();
            other.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(320, 240)); await AwaitPresentationAsync(secondary);
            Require(!ReferenceEquals(firstRoot, secondRoot) && firstInput.Window != secondInput.Window, "Transfer probe did not create two native XAML windows.");
            Require(secondaryAction.Focus(FocusState.Keyboard), "Could not establish secondary-window focus.");
            phase = "detach-primary"; await SaveAsync(); primaryHost.Children.Remove(overlay); await AwaitPresentationAsync(primary);
            Require(Portals(firstRoot).Length == 0 && !overlay.IsLoaded, "Detaching the overlay retained its primary-window portal."); Record(phase, primary, null);
            phase = "attach-secondary"; await SaveAsync(); secondaryHost.Children.Add(overlay); await AwaitPresentationAsync(secondary);
            await CheckAsync(phase, secondary, secondInput, secondaryAction);
            await NativeScreenCapture.CaptureClientAsync(secondInput.Window, Path.Combine(folder, "catalog-dark.png"));
            phase = "old-primary-mutated"; primary.RequestedTheme = ElementTheme.Dark; AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(700, 500));
            await AwaitPresentationAsync(primary); await CheckAsync(phase, secondary, secondInput, secondaryAction);
            phase = "return-primary-immediate"; await firstInput.AcquireAsync(primaryAction); primary.RequestedTheme = ElementTheme.Light;
            Require(primaryAction.Focus(FocusState.Keyboard), "Could not restore primary focus before transfer."); await SaveAsync();
            secondaryHost.Children.Remove(overlay); primaryHost.Children.Add(overlay); await AwaitPresentationAsync(primary); await CheckAsync(phase, primary, firstInput, primaryAction);
            phase = "old-secondary-mutated"; secondary.RequestedTheme = ElementTheme.Light; other.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(400, 300));
            await AwaitPresentationAsync(secondary); await CheckAsync(phase, primary, firstInput, primaryAction);
            phase = "secondary-identical-layout"; await secondInput.AcquireAsync(secondaryAction); await AwaitPresentationAsync(secondary);
            Require(firstRoot.Size.Equals(secondRoot.Size) && firstRoot.RasterizationScale == secondRoot.RasterizationScale && primary.ActualTheme == secondary.ActualTheme, "Equal-layout transfer did not establish equal root inputs.");
            Require(secondaryAction.Focus(FocusState.Keyboard), "Could not focus the equal-layout target."); await SaveAsync();
            primaryHost.Children.Remove(overlay); secondaryHost.Children.Add(overlay); await AwaitPresentationAsync(secondary); await CheckAsync(phase, secondary, secondInput, secondaryAction);
            phase = "primary-identical-layout"; await firstInput.AcquireAsync(primaryAction); Require(primaryAction.Focus(FocusState.Keyboard), "Could not focus the equal-layout return target."); await SaveAsync();
            secondaryHost.Children.Remove(overlay); primaryHost.Children.Add(overlay); await AwaitPresentationAsync(primary); await CheckAsync(phase, primary, firstInput, primaryAction);
            phase = "secondary-immediate"; await secondInput.AcquireAsync(secondaryAction); other.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(320, 240));
            secondary.RequestedTheme = ElementTheme.Dark; Require(secondaryAction.Focus(FocusState.Keyboard), "Could not restore secondary focus before transfer."); await SaveAsync();
            primaryHost.Children.Remove(overlay); secondaryHost.Children.Add(overlay); await AwaitPresentationAsync(secondary); await CheckAsync(phase, secondary, secondInput, secondaryAction);
            phase = "detach-secondary"; await SaveAsync(); secondaryHost.Children.Remove(overlay); await AwaitPresentationAsync(secondary);
            Require(Portals(secondRoot).Length == 0 && !overlay.IsLoaded, "Detaching the overlay retained its secondary-window portal."); Record(phase, secondary, null);
            phase = "primary-remount"; await firstInput.AcquireAsync(primaryAction); Require(primaryAction.Focus(FocusState.Keyboard), "Could not focus primary remount target.");
            await SaveAsync(); primaryHost.Children.Add(overlay); await AwaitPresentationAsync(primary); await CheckAsync(phase, primary, firstInput, primaryAction);
            secondInput.Dispose(); secondInput = null; other.Close(); await AwaitPresentationAsync(primary);
            Require(otherClosed, "Owned secondary window did not close."); await CheckAsync("old-window-closed", primary, firstInput, primaryAction);
            overlay.IsVisible = false; await AwaitPresentationAsync(primary); Require(Portals(firstRoot).Length == 0, "Hiding the transferred overlay retained its final portal."); Record("hidden", primary, null);
            passed = true;
        }
        finally
        {
            overlay.IsVisible = false; secondInput?.Dispose(); firstInput?.Dispose(); if (other is not null && !otherClosed) other.Close();
            Content = original; AppWindow.ResizeClient(originalSize); AppWindow.Move(originalPosition); await AwaitPresentationAsync(CatalogRoot); await SaveAsync();
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "loading-overlay-windows", environment = RuntimeEnvironment() }));
        Task SaveAsync() => File.WriteAllTextAsync(Path.Combine(folder, "loading-overlay-windows.json"), JsonSerializer.Serialize(new { passed, phase, reports,
            primaryWindow, secondaryWindow,
            otherClosed, originalContentRestored = ReferenceEquals(Content, original) }, new JsonSerializerOptions { WriteIndented = true }));
        async Task CheckAsync(string id, Page owner, NativeInputSession input, Button action)
        {
            phase = id; var root = owner.XamlRoot; var popup = Portals(root).Single(); var portal = (FrameworkElement)popup.Child;
            var inactive = ReferenceEquals(owner, primary) ? secondRoot : firstRoot;
            Require(inactive is null || (ReferenceEquals(inactive, secondRoot) && otherClosed) || Portals(inactive).Length == 0, "Transfer retained a portal in the previous window.");
            Require(ReferenceEquals(overlay.XamlRoot, root) && ReferenceEquals(popup.XamlRoot, root) && ReferenceEquals(portal.XamlRoot, root), "Transfer retained a previous XamlRoot.");
            var at = portal.TransformToVisual(owner).TransformPoint(new Point());
            Require(Math.Abs(at.X) < .05 && Math.Abs(at.Y) < .05 && Math.Abs(portal.ActualWidth - root.Size.Width) < .05 && Math.Abs(portal.ActualHeight - root.Size.Height) < .05, "Transfer lost destination viewport coverage.");
            Require(ReferenceEquals(FocusManager.GetFocusedElement(root), action), "Transfer stole destination keyboard focus.");
            var card = Parts<BleeckerOverlaySurface>(portal).Single(); var spinner = Parts<BleeckerLoadingSpinner>(portal).Single();
            Require(card.ActualTheme == owner.ActualTheme && ColorHex(((SolidColorBrush)card.Background).Color) == (owner.ActualTheme == ElementTheme.Light ? "#FFFFFFFF" : "#FF182533"), "Transfer retained its previous palette.");
            Require(FrameworkElementAutomationPeer.CreatePeerForElement(spinner).GetName() == "Shared operation", "Transfer lost its caller accessibility name.");
            var before = ReferenceEquals(owner, primary) ? primaryCount : secondaryCount;
            await input.ClickAsync(portal, localPoint: new Point(90, 36)); Require((ReferenceEquals(owner, primary) ? primaryCount : secondaryCount) == before, "Transferred portal passed pointer input to its underlying action.");
            Require(action.Focus(FocusState.Keyboard), "Could not focus the transferred portal's underlying action."); await AwaitPresentationAsync(owner);
            Require(!owner.IsTabStop, "Transferred portal retained its temporary root tab stop.");
            await input.KeyAsync(0x20, action); await AwaitPresentationAsync(owner);
            Require((ReferenceEquals(owner, primary) ? primaryCount : secondaryCount) == before + 1, "Transferred portal blocked the original keyboard action.");
            Record(id, owner, portal);
        }
        void Record(string id, Page owner, FrameworkElement? portal)
        {
            phase = id;
            var at = portal?.TransformToVisual(owner).TransformPoint(new Point());
            var card = portal is null ? null : Parts<BleeckerOverlaySurface>(portal).Single();
            var spinner = portal is null ? null : Parts<BleeckerLoadingSpinner>(portal).Single();
            reports.Add(new { id, owner = ReferenceEquals(owner, primary) ? "primary" : "secondary", theme = owner.ActualTheme.ToString(), viewport = owner.XamlRoot.Size,
                primaryPortals = firstRoot is null ? (int?)null : Portals(firstRoot).Length, secondaryPortals = secondRoot is null || otherClosed ? (int?)null : Portals(secondRoot).Length,
                portal = portal is null ? null : new { x = at!.Value.X, y = at.Value.Y, width = portal.ActualWidth, height = portal.ActualHeight,
                    window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(portal.XamlRoot.ContentIslandEnvironment.AppWindowId).ToInt64(),
                    controlRootMatchesOwner = ReferenceEquals(overlay.XamlRoot, owner.XamlRoot), theme = card!.ActualTheme.ToString(),
                    cardColor = ColorHex(((SolidColorBrush)card.Background).Color), name = FrameworkElementAutomationPeer.CreatePeerForElement(spinner!).GetName() },
                focused = (FocusManager.GetFocusedElement(owner.XamlRoot) as FrameworkElement)?.Name, owner.IsTabStop, otherClosed });
        }
        static Popup[] Portals(XamlRoot root) => VisualTreeHelper.GetOpenPopupsForXamlRoot(root).Where(p => p.Child is FrameworkElement child && child.Name == "PART_Portal").ToArray();
        static Button Action(string name) { var button = new Button { Name = name, Content = name, Width = 140, Height = 32 }; Canvas.SetLeft(button, 20); Canvas.SetTop(button, 20); return button; }
        static IEnumerable<T> Parts<T>(DependencyObject parent) where T : DependencyObject
        {
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            { var child = VisualTreeHelper.GetChild(parent, index); if (child is T match) yield return match; foreach (var nested in Parts<T>(child)) yield return nested; }
        }
    }
}
