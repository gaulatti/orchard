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
            await RunSmokeAsync(folder);
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
    private async Task RunSmokeAsync(string folder)
    {
        var captures = new List<object>();
        foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
        {
            ThemeSelector.SelectedIndex = theme == ElementTheme.Light ? 1 : 2;
            await Task.Delay(500);
            CatalogRoot.UpdateLayout();
            Require(CatalogRoot.ActualWidth > 0 && PrimaryAction.ActualWidth > 0, "Controls did not lay out.");
            var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
            await CaptureAsync(Path.Combine(folder, $"catalog-{theme.ToString().ToLowerInvariant()}.png"));
            Require(PrimaryAction.Background is SolidColorBrush brush &&
                ColorHex(brush.Color) == BleeckerTokens.Colors(scheme)["primary"],
                $"Primary action does not use the Bleecker theme: requested={theme}, actual={CatalogRoot.ActualTheme}, background={DescribeBrush(PrimaryAction.Background)}, expected={BleeckerTokens.Colors(scheme)["primary"]}.");
            Require(HeadingSample.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf#Encode Sans", StringComparison.Ordinal), "Bundled headline font was not selected.");
            foreach (var font in new[] { "EncodeSans-Regular.ttf", "EncodeSans-SemiBold.ttf", "LibreFranklin-Regular.ttf", "LibreFranklin-SemiBold.ttf" })
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
            new { passed = true, actionCount, systemAppearance = true, captures }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static string ColorHex(Windows.UI.Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string DescribeBrush(Brush? brush) => brush is SolidColorBrush solid ? ColorHex(solid.Color) : brush?.GetType().FullName ?? "null";

    private async Task<SizeInt32> CaptureAsync(string path)
    {
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(CatalogRoot);
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
