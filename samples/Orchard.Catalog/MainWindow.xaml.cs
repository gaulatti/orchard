using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Text.Json;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Catalog;

public sealed partial class MainWindow : Window
{
    private int actionCount;
    private bool smokeStarted;

    public MainWindow()
    {
        InitializeComponent();
        InitializeNavigation();
        AppWindow.Resize(new SizeInt32(1100, 900));
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CatalogRoot is null) return; // SelectionChanged can fire during XAML initialization.
        CatalogRoot.RequestedTheme = ((ComboBox)sender).SelectedIndex switch
        {
            0 => ElementTheme.Default,
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => throw new InvalidOperationException("Unknown catalog appearance.")
        };
    }

    private void OnPrimaryAction(object sender, RoutedEventArgs e)
    {
        actionCount++;
        ActionResult.Text = $"Action invoked {actionCount} time{(actionCount == 1 ? "" : "s")}.";
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--smoke-output");
        if (index < 0 || smokeStarted) return;
        smokeStarted = true;
        if (index + 1 >= args.Length) { Environment.Exit(2); return; }
        var folder = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(folder);
        try
        {
            if (args.Contains("--smoke-tooltips")) await RunTooltipSmokeAsync(folder);
            else if (args.Contains("--smoke-input")) await RunInputSmokeAsync(folder);
            else await RunSmokeAsync(folder);
            Environment.Exit(0);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "result.json"),
                JsonSerializer.Serialize(new { passed = false, error = error.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
            Environment.Exit(1);
        }
    }

    // Catalog-only runtime check. The reusable library has no command-line or automation dependencies.
    private async Task RunTooltipSmokeAsync(string folder)
    {
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation);
            CatalogRoot.RequestedTheme = theme;
            await SelectFixtureAsync(Tooltips);
            await Tooltips.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "tooltips" }));
    }

    private async Task RunInputSmokeAsync(string folder)
    {
        await SelectFixtureAsync(Inputs);
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            CatalogRoot.RequestedTheme = theme;
            await Task.Delay(250);
            CatalogRoot.UpdateLayout();
            await Inputs.VerifyAsync(theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark, folder);
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(new { passed = true, component = "input" }));
    }

    private async Task RunSmokeAsync(string folder)
    {
        await VerifyNavigationAsync();
        var captures = new List<object>();
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            await SelectFixtureAsync(Foundation);
            ThemeSelector.SelectedIndex = theme == ElementTheme.Light ? 1 : 2;
            await Task.Delay(500);
            CatalogRoot.UpdateLayout();
            Require(CatalogRoot.ActualWidth > 0 && PrimaryAction.ActualWidth > 0, "Controls did not lay out.");
            var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            Require(PrimaryAction.Background is SolidColorBrush brush &&
                ColorHex(brush.Color) == BleeckerTokens.Colors(scheme)["primary"],
                $"Primary action does not use the Bleecker theme: requested={theme}, actual={CatalogRoot.ActualTheme}, background={DescribeBrush(PrimaryAction.Background)}, expected={BleeckerTokens.Colors(scheme)["primary"]}.");
            await VerifyButtonStatesAsync(PrimaryAction, scheme);
            await VerifyButtonStatesAsync(DisabledAction, scheme, disabled: true);
            VerifyButtonGroup();
            await SelectFixtureAsync(Toggles);
            await Toggles.VerifyAsync(scheme);
            await SelectFixtureAsync(Buttons);
            await Buttons.VerifyAsync(scheme);
            await SelectFixtureAsync(IconButtons);
            await IconButtons.VerifyAsync(scheme);
            await SelectFixtureAsync(ToggleGroups);
            await ToggleGroups.VerifyAsync();
            await SelectFixtureAsync(LoadingSpinners);
            await LoadingSpinners.VerifyAsync(scheme);
            await SelectFixtureAsync(Cards);
            await Cards.VerifyAsync(scheme);
            await SelectFixtureAsync(Separators);
            await Separators.VerifyAsync(scheme);
            await SelectFixtureAsync(StatusBadges);
            await StatusBadges.VerifyAsync(scheme);
            await SelectFixtureAsync(Menus);
            await Menus.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Notifications);
            await Notifications.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Tooltips);
            await Tooltips.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(FieldHelp);
            await FieldHelp.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Labels);
            await Labels.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Inputs);
            await Inputs.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(Keyboard);
            await Keyboard.VerifyAsync(scheme, folder);
            await SelectFixtureAsync(StatusBadges);
            StatusBadges.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
            await Task.Delay(350);
            await CaptureAsync(Path.Combine(folder, $"status-badges-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(Separators);
            Separators.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
            await Task.Delay(100);
            await CaptureAsync(Path.Combine(folder, $"separators-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(Cards);
            Cards.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
            await Task.Delay(100);
            await CaptureAsync(Path.Combine(folder, $"cards-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(LoadingSpinners);
            LoadingSpinners.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false });
            await Task.Delay(100);
            await CaptureAsync(Path.Combine(folder, $"loading-spinners-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(ToggleGroups);
            ToggleGroups.StartBringIntoView();
            await Task.Delay(350);
            await CaptureAsync(Path.Combine(folder, $"toggle-groups-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(IconButtons);
            IconButtons.StartBringIntoView();
            await Task.Delay(350);
            await CaptureAsync(Path.Combine(folder, $"icon-buttons-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(Buttons);
            Buttons.StartBringIntoView();
            await Task.Delay(350);
            await CaptureAsync(Path.Combine(folder, $"buttons-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(Toggles);
            Toggles.StartBringIntoView();
            await Task.Delay(350);
            await CaptureAsync(Path.Combine(folder, $"toggles-{theme.ToString().ToLowerInvariant()}.png"));
            await SelectFixtureAsync(Foundation);
            CatalogScroll.ChangeView(null, 0, null, disableAnimation: true);
            await Task.Delay(100);
            Require(HeadingSample.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf#Encode Sans", StringComparison.Ordinal), "Bundled headline font was not selected.");
            foreach (var font in new[] { "EncodeSans-Regular.ttf", "EncodeSans-Medium.ttf", "EncodeSans-SemiBold.ttf", "LibreFranklin-Regular.ttf", "LibreFranklin-SemiBold.ttf" })
                Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "Orchard", "Assets", "Fonts", font)), $"Missing font: {font}");
            var peer = new ButtonAutomationPeer(PrimaryAction);
            ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(actionCount == captures.Count + 1 && ActionResult.Text.StartsWith("Action invoked", StringComparison.Ordinal), "Button invocation did not reach the handler.");
            Require(!DisabledAction.IsEnabled, "Disabled action became enabled.");
            HeadlineInput.Text = "A Windows-native newsroom";
            Require(HeadlineInput.Text.Length > 0, "Text input rejected its value.");
            var size = await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            captures.Add(new { theme = theme.ToString(), width = size.Width, height = size.Height });
        }
        ThemeSelector.SelectedIndex = 0;
        Require(CatalogRoot.RequestedTheme == ElementTheme.Default, "System appearance did not reset.");
        await File.WriteAllTextAsync(Path.Combine(folder, "result.json"), JsonSerializer.Serialize(
            new { passed = true, actionCount, systemAppearance = true,
                buttonStates = new[] { "Normal", "PointerOver", "Pressed", "Disabled" },
                buttonSpinner = new { referenceVector = true, animationsEnabled = Buttons.SpinnerAnimationsEnabled,
                    rotationObserved = Buttons.SpinnerAnimationsEnabled, reducedMotionStaticObserved = !Buttons.SpinnerAnimationsEnabled, stopAndReset = true },
                buttonProperties = new[] { "Background", "Foreground", "BorderBrush", "Opacity" },
                loadingSpinner = new { sizes = new[] { 16, 32, 48 }, animationsEnabled = LoadingSpinners.AnimationsEnabled, hiddenAndUnloadedReset = true },
                toggles = new { automationOnOff = true, consumerStatePropagation = true, variants = new[] { "default", "outline" },
                    states = new[] { "Normal", "PointerOver", "Pressed", "Disabled", "Checked", "CheckedPointerOver", "CheckedPressed", "CheckedDisabled" } },
                captures }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private void VerifyButtonGroup()
    {
        var first = (Button)ActionGroup.Children[0];
        var middle = (Button)ActionGroup.Children[1];
        var last = (Button)ActionGroup.Children[2];
        var radius = BleeckerTokens.Radii["button"];
        Require(first.CornerRadius.TopLeft == radius && first.CornerRadius.TopRight == 0,
            "Horizontal group first corners are incorrect.");
        Require(middle.CornerRadius == new CornerRadius(0) && first.BorderThickness.Right == 0 && last.BorderThickness.Right == 1,
            "Group internal corners/borders are incorrect.");
        Require(FrameworkElementAutomationPeer.CreatePeerForElement(ActionGroup)?.GetAutomationControlType() == AutomationControlType.Group,
            "Button group has no accessible group role.");
        Require(!last.IsEnabled, "Group enabled a disabled action.");
        last.Visibility = Visibility.Collapsed;
        ActionGroup.UpdateLayout();
        Require(middle.CornerRadius.TopRight == radius && middle.BorderThickness.Right == 1,
            "Collapsed action left an internal edge at the end of the group.");
        last.Visibility = Visibility.Visible;
        ActionGroup.Orientation = Orientation.Vertical;
        ActionGroup.UpdateLayout();
        Require(first.CornerRadius.TopRight == radius && first.CornerRadius.BottomLeft == 0 && first.BorderThickness.Bottom == 0,
            "Vertical group did not update its edges.");
        ActionGroup.Orientation = Orientation.Horizontal;
        ActionGroup.Width = 280;
        ActionGroup.UpdateLayout();
        Require(first.ActualWidth + middle.ActualWidth + last.ActualWidth <= 281,
            "Constrained group overflowed its available width.");
        ActionGroup.ClearValue(FrameworkElement.WidthProperty);
        ActionGroup.UpdateLayout();
    }

    private static async Task VerifyButtonStatesAsync(Button button, BleeckerColorScheme scheme, bool disabled = false)
    {
        button.ApplyTemplate();
        var presenter = FindPresenter(button) ?? throw new InvalidOperationException("Button template has no content presenter.");
        foreach (var state in disabled ? new[] { "Disabled" } : new[] { "Normal", "PointerOver", "Pressed", "Normal" })
        {
            Require(VisualStateManager.GoToState(button, state, false), $"Missing native button state: {state}.");
            await Task.Delay(120);
            foreach (var (property, actual) in new[] { ("Background", presenter.Background), ("Foreground", presenter.Foreground), ("BorderBrush", presenter.BorderBrush) })
            {
                var token = property == "Foreground" ? "primaryForeground" : "primary";
                var expected = BleeckerTokens.Colors(scheme)[token];
                var opacity = state switch { "Disabled" => 0.4, "Pressed" => 0.8, "PointerOver" => 0.9, _ => 1.0 };
                Require(actual is SolidColorBrush solid && ColorHex(solid.Color) == expected && Math.Abs(solid.Opacity - opacity) < 0.001,
                    $"{scheme} {state} {property}: rendered={DescribeBrush(actual)}, expected={expected} at opacity {opacity}.");
            }
        }
    }

    private static ContentPresenter? FindPresenter(DependencyObject parent)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ContentPresenter presenter) return presenter;
            if (FindPresenter(child) is { } nested) return nested;
        }
        return null;
    }

    private static string ColorHex(Windows.UI.Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string DescribeBrush(Brush? brush) => brush is SolidColorBrush solid ? ColorHex(solid.Color) : brush?.GetType().FullName ?? "null";

    private Task<SizeInt32> CaptureAsync(string path) => CaptureElementAsync(CatalogRoot, path);
    internal static async Task<SizeInt32> CaptureElementAsync(UIElement visual, string path)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(visual);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "The catalog did not render.");
        var pixels = await bitmap.GetPixelsAsync();
        var bytes = new byte[checked((int)pixels.Length)];
        using (var reader = DataReader.FromBuffer(pixels)) reader.ReadBytes(bytes);
        Require(bytes.Where((_, offset) => offset % 4 != 3).Distinct().Count() > 8, "The render is blank.");
        await SavePngAsync(path, bytes, bitmap.PixelWidth, bitmap.PixelHeight);
        return new SizeInt32(bitmap.PixelWidth, bitmap.PixelHeight);
    }

    private static async Task SavePngAsync(string path, byte[] pixels, int width, int height)
    {
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied, (uint)width, (uint)height, 96, 96, pixels);
        await encoder.FlushAsync();
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync(checked((uint)stream.Size));
        var output = new byte[checked((int)stream.Size)];
        reader.ReadBytes(output);
        await File.WriteAllBytesAsync(path, output);
    }
}
