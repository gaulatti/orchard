using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Orchard;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace PackageConsumer;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Resize(new SizeInt32(1100, 760));
    }
    private void OnSave(object sender, RoutedEventArgs e) => Result.Text = $"Saved: {Input.Text}";

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!Environment.GetCommandLineArgs().Contains("--verify-output")) return;
        try
        {
            foreach (var font in new[] { "EncodeSans-Regular.ttf", "EncodeSans-Medium.ttf", "EncodeSans-SemiBold.ttf", "LibreFranklin-Regular.ttf", "LibreFranklin-SemiBold.ttf", "OFL-EncodeSans.txt", "OFL-LibreFranklin.txt" })
                Require(File.Exists(Path.Combine(AppContext.BaseDirectory, "Orchard", "Assets", "Fonts", font)), $"Missing package asset: {font}");
            Require(Heading.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf#Encode Sans"), "Package font URI not selected.");
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                Root.RequestedTheme = theme;
                await Task.Delay(250);
                Root.UpdateLayout();
                Require(Action.ActualWidth > 0 && Heading.ActualHeight > 0, "Consumer controls did not lay out.");
                Require(PackageSpinner.ActualWidth == 16 && PackageSpinner.ActualHeight == 16 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageSpinner).GetAutomationControlType() == AutomationControlType.ProgressBar,
                    "Packaged loading spinner lost its geometry or progress role.");
                var scheme = theme == ElementTheme.Light ? BleeckerColorScheme.Light : BleeckerColorScheme.Dark;
                var color = ((SolidColorBrush)Action.Background).Color;
                Require($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}" == BleeckerTokens.Colors(scheme)["primary"], $"Package theme mismatch: {theme}");
                PackageAction.ApplyTemplate();
                var native = VisualTreeHelper.GetChild(PackageAction, 0) as Button;
                Require(native is not null && native.ActualWidth > 0, "Packaged component template did not load.");
                Require(new ButtonAutomationPeer(native!).GetName() == "Package component", "Packaged text action lost its accessible name.");
                Require(native!.FontWeight.Weight == 500 && native.FontFamily.Source.Contains("EncodeSans-Medium.ttf#Encode Sans"),
                    "Packaged component did not resolve the bundled medium font.");
                Require(PackageIcon.ActualWidth == 40 && PackageIcon.ActualHeight == 40, "Packaged icon control did not lay out.");
                PackageIcon.IsEnabled = false;
                await Task.Delay(100);
                var iconPresenter = (ContentPresenter)VisualTreeHelper.GetChild(PackageIcon, 0);
                Require(SaveVector.Foreground is SolidColorBrush iconBrush && iconPresenter.Foreground is SolidColorBrush iconText && iconBrush.Color == iconText.Color && Math.Abs(iconBrush.Opacity - iconText.Opacity) < 0.001,
                    "Consumer-owned vector did not follow the packaged disabled foreground.");
                PackageIcon.IsEnabled = true;
                Require(PackageGroup.ActualWidth > 0 && PackageGridItem.ActualHeight >= 36, "Packaged toggle group template did not lay out.");
                var componentColor = ((SolidColorBrush)native!.Background).Color;
                Require($"#{componentColor.A:X2}{componentColor.R:X2}{componentColor.G:X2}{componentColor.B:X2}" == BleeckerTokens.Colors(scheme)["primary"], "Packaged per-variant resources did not load.");
                PackageAction.IsLoading = true;
                await Task.Delay(100);
                Require(HasReferenceSpinner(PackageAction), "Packaged loading action lost its reference vector template.");
                await CaptureAsync(theme);
                PackageAction.IsLoading = false;
            }
            Input.Text = "Package interaction verified";
            ((IInvokeProvider)new ButtonAutomationPeer(Action).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(Result.Text == "Saved: Package interaction verified", "Package button invocation failed.");
            Result.Text = "Ready";
            ((IInvokeProvider)new ButtonAutomationPeer((Button)VisualTreeHelper.GetChild(PackageAction, 0)).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(Result.Text == "Saved: Package interaction verified", "Packaged component Click event failed.");
            Result.Text = "Ready";
            ((IInvokeProvider)new ButtonAutomationPeer(PackageIcon).GetPattern(PatternInterface.Invoke)).Invoke();
            await Task.Delay(100);
            Require(Result.Text == "Saved: Package interaction verified", "Packaged icon activation failed.");
            var selection = (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(PackageGridItem).GetPattern(PatternInterface.SelectionItem);
            selection.Select();
            Require(PackageGroup.SelectedValue == "grid" && PackageGridItem.IsChecked == true, "Packaged group selection failed.");
            selection.RemoveFromSelection();
            Require(string.IsNullOrEmpty(PackageGroup.SelectedValue), "Packaged group optional selection failed.");
            App.Report(true, "Package resources, fonts, light/dark themes, layout and native action verified.");
        }
        catch (Exception error) { App.Report(false, error.ToString()); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static bool HasReferenceSpinner(DependencyObject owner)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(owner); i++)
        {
            var child = VisualTreeHelper.GetChild(owner, i);
            if (child is Microsoft.UI.Xaml.Shapes.Path { Name: "PART_SpinnerPath" }) return true;
            if (HasReferenceSpinner(child)) return true;
        }
        return false;
    }

    private async Task CaptureAsync(ElementTheme theme)
    {
        var arguments = Environment.GetCommandLineArgs();
        var folder = Path.GetDirectoryName(arguments[Array.IndexOf(arguments, "--verify-output") + 1])!;
        var bitmap = new RenderTargetBitmap();
        await bitmap.RenderAsync(Root);
        Require(bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0, "Consumer rendered an empty frame.");
        var pixels = await bitmap.GetPixelsAsync();
        var bytes = new byte[checked((int)pixels.Length)];
        using (var reader = DataReader.FromBuffer(pixels)) reader.ReadBytes(bytes);
        using var stream = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied,
            (uint)bitmap.PixelWidth, (uint)bitmap.PixelHeight, 96, 96, bytes);
        await encoder.FlushAsync();
        using var outputReader = new DataReader(stream.GetInputStreamAt(0));
        await outputReader.LoadAsync(checked((uint)stream.Size));
        var output = new byte[checked((int)stream.Size)];
        outputReader.ReadBytes(output);
        await File.WriteAllBytesAsync(Path.Combine(folder, $"consumer-{theme.ToString().ToLowerInvariant()}.png"), output);
    }
}
