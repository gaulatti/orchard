using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

public sealed partial class FooterFixture
{
    internal async Task VerifyForegroundAsync(string folder, BleeckerColorScheme scheme)
    {
        Status.Text = "Choose a destination";
        var home = sections.SelectMany(section => section.Items).Single(item => item.Href == "#home");
        var defaultLink = MainWindow.Desc<HyperlinkButton>(Default).Single(link =>
            link.Name == "FooterSectionLink" && link.DataContext is BleeckerNavLinkContext context && ReferenceEquals(context.Item, home));
        var requests = new List<BleeckerNavItem>();
        void Invoked(object? sender, BleeckerNavItem item) => requests.Add(item);
        Default.ItemInvoked += Invoked;
        Custom.ItemInvoked += Invoked;
        try
        {
            await MainWindow.AwaitPresentationAsync(Default);
            using var input = new NativeInputSession(XamlRoot, folder, $"footer-foreground-{scheme}");
            await input.AcquireAsync(Status);
            await input.MoveAsync(defaultLink);
            await MainWindow.AwaitPresentationAsync(defaultLink);
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"footer-foreground-hover-{scheme}.png"));
            await input.ClickAsync(defaultLink);
            await AwaitAsync(() => requests.Count == 1 && ReferenceEquals(requests[0], home) && Status.Text == "Host request: Home → #home",
                "Physical pointer did not invoke the original Footer link through its host.");

            ShowVariant(1);
            await MainWindow.AwaitPresentationAsync(Custom);
            var customLink = MainWindow.Desc<HyperlinkButton>(Custom).Single(link => ReferenceEquals(link.Tag, home));
            if (!customLink.Focus(FocusState.Keyboard)) throw new InvalidOperationException("Custom Footer link rejected keyboard focus.");
            input.Key(0x0D);
            await AwaitAsync(() => requests.Count == 2 && ReferenceEquals(requests[1], home) &&
                ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), customLink),
                "Physical Enter did not invoke the custom Footer link and retain focus.");
            await MainWindow.AwaitPresentationAsync(customLink);
            await NativeScreenCapture.CaptureClientAsync(input.Window, Path.Combine(folder, $"footer-foreground-focus-{scheme}.png"));
            if (CallerInput.Text != "Caller note") throw new InvalidOperationException("Footer navigation changed the original caller input.");
            ShowVariant(2);
            await MainWindow.AwaitPresentationAsync(Minimal);
            if (Minimal.Sections.Any() || Minimal.ShowBottomAccent) throw new InvalidOperationException("Minimal Footer variant lost its empty-section or accent setting.");
            ShowVariant(0);
            await File.WriteAllTextAsync(Path.Combine(folder, $"footer-foreground-{scheme}.json"), JsonSerializer.Serialize(new
            {
                passed = true, scheme, pointerInvokedOriginal = true, keyboardInvokedOriginal = true,
                customTemplateRouted = true, keyboardFocusRetained = true, callerInputRetained = true
            }));
        }
        finally
        {
            Default.ItemInvoked -= Invoked;
            Custom.ItemInvoked -= Invoked;
        }
    }

    private static async Task AwaitAsync(Func<bool> condition, string message)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2)) await Task.Delay(16);
        if (!condition()) throw new InvalidOperationException(message);
    }
}
