using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class SonnersFixture : UserControl
{
    private string? latest;
    private readonly ICommand originalCommand;
    public SonnersFixture()
    {
        originalCommand = new ActionCommand(parameter => Result.Text = "Original fictional action: " + parameter + "; the notification remains mounted.");
        InitializeComponent();
    }
    private void PositionChanged(object sender, SelectionChangedEventArgs args)
    { if (Host is not null && sender is ComboBox { SelectedIndex: >= 0 } position) Host.Position = (BleeckerSonnerPosition)position.SelectedIndex; }
    private void PersistentChanged(object sender, RoutedEventArgs args)
    { if (Host is not null) Host.Duration = ((CheckBox)sender).IsChecked == true ? 0 : 4000; }
    private void ShowVariant(object sender, RoutedEventArgs args)
    {
        var variant = Enum.Parse<BleeckerSonnerVariant>((string)((FrameworkElement)sender).Tag);
        latest = Host.Show(new BleeckerSonnerToast("Fictional " + variant + " notification") { Variant = variant, Description = "Caller-owned local content." });
    }
    private void ShowAction(object sender, RoutedEventArgs args)
    {
        var variant = Enum.Parse<BleeckerButtonVariant>((string)((FrameworkElement)sender).Tag);
        var action = new BleeckerSonnerAction("Continue", originalCommand, "original " + variant + " action", variant);
        latest = Host.Show(new BleeckerSonnerToast("Ready to continue") { Description = "This action preserves its original command and parameter.", Duration = 0, Action = action });
    }
    private void ShowLong(object sender, RoutedEventArgs args) => latest = Host.Show(new BleeckerSonnerToast("A deliberately long fictional notification — Straße 東京 é العربية") { Description = "Fictional 日本語 descrição — שלום. This description wraps and retains its complete original content.", Duration = 0 });
    private void ShowStack(object sender, RoutedEventArgs args)
    { for (var index = 0; index < 12; index++) latest = Host.Show(new BleeckerSonnerToast("Original fictional item " + index) { Duration = 0 }); }
    private void DismissLatest(object sender, RoutedEventArgs args) { if (latest is not null) Host.Dismiss(latest); }
    private void ShowGlobal(object sender, RoutedEventArgs args)
    {
        var options = new BleeckerSonnerOptions() { Description = "Fictional imperative API examples.", Duration = 0 };
        BleeckerToast.Show(new BleeckerSonnerToast("Global default") { Duration = 0 }); BleeckerToast.Success("Global success", options); BleeckerToast.Error("Global error", options);
        BleeckerToast.Warning("Global warning", options); latest = BleeckerToast.Info("Global info", options); BleeckerToast.Dismiss("fictional-nonexistent-id");
    }
    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        static IEnumerable<T> FindAll<T>(DependencyObject root) where T : FrameworkElement
        {
            if (root is T match) yield return match;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
                foreach (var child in FindAll<T>(VisualTreeHelper.GetChild(root, index))) yield return child;
        }
        BleeckerOverlaySurface[] Surfaces() => FindAll<BleeckerOverlaySurface>((DependencyObject)XamlRoot.Content)
            .Concat(VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot)
                .Where(popup => popup.Child is not null).SelectMany(popup => FindAll<BleeckerOverlaySurface>(popup.Child)))
            .Where(surface => surface.IsLoaded && surface.Name == "SonnerToastSurface").Distinct().ToArray();
        static async Task AwaitAsync(Func<bool> condition, string message)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (condition()) return;
                await Task.Delay(20);
            }
            throw new InvalidOperationException(message);
        }
        var heading = (FrameworkElement)((StackPanel)Content).Children[0];
        using var input = new NativeInputSession(XamlRoot, folder, $"sonner-{scheme}");
        await input.AcquireAsync(heading);
        var modalHost = (BleeckerModalHost)XamlRoot.Content;
        await AwaitAsync(() =>
        {
            modalHost.UpdateLayout();
            return Math.Abs(modalHost.ActualWidth - XamlRoot.Size.Width) < .5 &&
                Math.Abs(modalHost.ActualHeight - XamlRoot.Size.Height) < .5;
        }, "Catalog modal host did not settle after foreground acquisition.");
        var first = Host.Show(new BleeckerSonnerToast("Fictional pointer dismissal") { Duration = 0 });
        await AwaitAsync(() => Surfaces().Length == 1, "Sonner did not present its pointer-dismiss target.");
        var reducedMotion = !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        if (!reducedMotion) throw new InvalidOperationException("Reduced-motion Sonner check requires the current animations-disabled desktop.");
        await AwaitAsync(() => Surfaces().Single().Opacity == 1 && Surfaces().Single().RenderTransform is TranslateTransform { Y: 0 },
            "Sonner did not reach its reduced-motion endpoint.");
        var close = FindAll<Button>(Surfaces().Single()).Single(button => button.Name == "SonnerDismiss");
        try { await input.ClickAsync(close); }
        catch (TimeoutException) when (Surfaces().Length == 0) { }
        await AwaitAsync(() => Surfaces().Length == 0, "Physical pointer did not dismiss Sonner toast.");
        var invoked = 0;
        var action = new BleeckerSonnerAction("Continue", new ActionCommand(_ => invoked++), null);
        var second = Host.Show(new BleeckerSonnerToast("Fictional action") { Duration = 0, Action = action });
        await AwaitAsync(() => Surfaces().Length == 1, "Sonner did not present its action toast.");
        var surface = Surfaces().Single();
        var actionButton = FindAll<Button>(surface).Single(button => button.Name != "SonnerDismiss");
        await input.ClickAsync(actionButton);
        await AwaitAsync(() => invoked == 1 && Surfaces().Length == 1, "Physical Sonner action did not preserve its toast.");
        if (!actionButton.Focus(FocusState.Keyboard) || !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), actionButton))
            throw new InvalidOperationException("Sonner action refused keyboard focus.");
        input.Key(0x20);
        await AwaitAsync(() => invoked == 2 && Surfaces().Length == 1, "Space did not invoke Sonner action.");
        close = FindAll<Button>(surface).Single(button => button.Name == "SonnerDismiss");
        if (!close.Focus(FocusState.Keyboard) || !ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), close))
            throw new InvalidOperationException("Sonner dismiss button refused keyboard focus.");
        input.Key(0x20);
        await AwaitAsync(() => Surfaces().Length == 0, "Space did not dismiss Sonner toast.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"sonner-input-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, theme = scheme.ToString(), pointerDismissed = first.Length > 0,
            pointerActionCount = 1, keyboardActionCount = 1, keyboardDismissed = second.Length > 0,
            focusVerified = true, reducedMotionEndpointVerified = reducedMotion, hardwareInputSent = true
        }));
    }
    private sealed class ActionCommand(Action<object?> execute) : ICommand
    { public event EventHandler? CanExecuteChanged { add { } remove { } } public bool CanExecute(object? parameter) => true; public void Execute(object? parameter) => execute(parameter); }
}
