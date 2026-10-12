using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Composition;
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
                var switchPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageSwitch);
                var switchToggle = (IToggleProvider)switchPeer.GetPattern(PatternInterface.Toggle);
                Require(PackageSwitch.FontSize == 14 && PackageSwitch.FontFamily.Source.Contains("EncodeSans-Medium.ttf") &&
                    switchPeer.GetName() == "Package notifications" && switchPeer.GetAutomationControlType() == AutomationControlType.CheckBox,
                    "Packaged switch lost its native semantics, label or bundled font.");
                switchToggle.Toggle();
                Require(PackageSwitch.IsOn && switchToggle.ToggleState == ToggleState.On, "Packaged native switch did not update consumer state.");
                PackageSwitch.IsOn = false;
                Require(switchToggle.ToggleState == ToggleState.Off, "Packaged consumer state did not reach the native switch.");
                var packagedRange = (IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(PackageProgress).GetPattern(PatternInterface.RangeValue);
                Require(packagedRange.IsReadOnly && packagedRange.Value == 65 && packagedRange.Maximum == 100 && PackageProgress.FontSize == 12,
                    "Packaged progress lost its resources or native range semantics.");
                Require(PackageEyebrow.ActualHeight == 16 && PackageEyebrow.FontSize == 11 && !PackageEyebrow.IsTabStop &&
                    PackageEyebrow.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf"), "Packaged eyebrow lost its resources or geometry.");
                Require(((Grid)VisualTreeHelper.GetChild(PackageEyebrow, 0)).Children.OfType<TextBlock>().Single().Text == "STRASSE" &&
                    (string)PackageEyebrow.Content == "Straße", "Packaged eyebrow lost the embedded Unicode mapping or changed consumer content.");
                Require(PackageKeyboard.ActualHeight == 20 && PackageKeyboard.FontSize == 11 && !PackageKeyboard.IsTabStop &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageKeyboard).GetName() == "Esc",
                    "Packaged keyboard display lost its template, typography or accessible content.");
                Require(Input.ActualHeight == 40 && Input.FontSize == 14 && Input.FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(Input).GetAutomationControlType() == AutomationControlType.Edit,
                    "Packaged input lost its geometry, font or native editor.");
                Require(PackageFieldLabel.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && PackageFieldLabel.FontSize == 13 &&
                    !PackageFieldLabel.IsTabStop && ReferenceEquals(Microsoft.UI.Xaml.Automation.AutomationProperties.GetLabeledBy(Input), PackageFieldLabel) &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageFieldLabel).GetName() == "Title", "Packaged field label lost typography or input association.");
                Require(PackageHelp.ActualWidth == 16 && PackageHelp.ActualHeight == 16 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageHelp).GetName() == PackageHelp.Text &&
                    BleeckerTooltipService.GetToolTip(PackageHelp)?.Placement == Microsoft.UI.Xaml.Controls.Primitives.PlacementMode.Bottom,
                    "Packaged field help lost its size, accessible name or bottom tooltip.");
                PackageMenu.ShowAt(MenuTrigger);
                await Task.Delay(200);
                var menuPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Select(popup => popup.Child).OfType<MenuFlyoutPresenter>().Single();
                Require(menuPresenter.Padding.Left == 6 && menuPresenter.CornerRadius.TopLeft == 7 &&
                    menuPresenter.FontFamily.Source.Contains("EncodeSans-Regular.ttf"), "Packaged menu lost its branded presenter.");
                var menuSurface = (BleeckerOverlaySurface)VisualTreeHelper.GetChild(menuPresenter, 0);
                var menuSurfaceRoot = (Grid)VisualTreeHelper.GetChild(menuSurface, 0);
                Require(ElementCompositionPreview.GetElementChildVisual((Grid)menuSurfaceRoot.Children[1]) is ContainerVisual menuShadows && menuShadows.Children.Count == 2,
                    "Packaged menu did not render its overlay shadow layers.");
                var labelPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageLabel);
                Require(PackageContentItem.ActualHeight == 32 && PackageContentText.FontSize == 14 &&
                    PackageContentText.FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageContentItem).GetName() == "Review locally",
                    "Packaged composed menu lost its default template, typography or action name.");
                Require(PackageInset.Padding == new Thickness(32, 6, 8, 6) && PackageInset.ActualHeight == 32,
                    "Packaged inset item lost its reference geometry.");
                var presentedLabel = FrameworkElementAutomationPeer.CreatePeerForElement(menuPresenter).GetChildren()
                    .SingleOrDefault(peer => peer.GetName() == "Account");
                Require(presentedLabel is not null && presentedLabel.GetAutomationControlType() == AutomationControlType.Text &&
                    presentedLabel.GetPattern(PatternInterface.Invoke) is null, "Packaged presenter omitted or made the label actionable.");
                Require(PackageLabel.ActualHeight == 27 && PackageLabel.FontFamily.Source.Contains("EncodeSans-SemiBold.ttf") &&
                    labelPeer.IsControlElement() && labelPeer.IsContentElement() && labelPeer.GetName() == "Account" &&
                    labelPeer.GetAutomationControlType() == AutomationControlType.Text,
                    $"Packaged menu label mismatch: height={PackageLabel.ActualHeight}, font={PackageLabel.FontFamily.Source}, control={labelPeer.IsControlElement()}, content={labelPeer.IsContentElement()}, name={labelPeer.GetName()}, type={labelPeer.GetAutomationControlType()}.");
                PackageMenu.Hide();
                await Task.Delay(150);
                var notificationTrigger = (BleeckerIconButton)PackageNotifications.FindName("Trigger");
                ((IInvokeProvider)new ButtonAutomationPeer(notificationTrigger).GetPattern(PatternInterface.Invoke)).Invoke();
                await Task.Delay(200);
                var notificationPresenter = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot)
                    .Select(popup => popup.Child).OfType<FlyoutPresenter>().Single();
                Require(notificationPresenter.ActualWidth == 320 && notificationTrigger.ActualHeight == 40 &&
                    ((TextBlock)PackageNotifications.FindName("CountText")).Text == "1",
                    "Packaged notification control lost its XAML resources or geometry.");
                Require(((ItemsControl)PackageNotifications.FindName("NotificationItems")).Items.Count == 1,
                    "Packaged notification collection did not reach the popup.");
                notificationTrigger.Flyout.Hide();
                await Task.Delay(150);
                PackageTooltip.IsOpen = true;
                await Task.Delay(150);
                var tooltipRoot = (Grid)VisualTreeHelper.GetChild(PackageTooltip, 0);
                var tooltipSurface = (BleeckerOverlaySurface)tooltipRoot.Children[0];
                Require(tooltipSurface.ActualWidth > 0 && tooltipSurface.ActualWidth <= 224 &&
                    tooltipSurface.Padding.Left == 12 && PackageTooltip.FontSize == 12 &&
                    PackageTooltip.FontFamily.Source.Contains("EncodeSans-Regular.ttf") &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageTooltip).GetAutomationControlType() == AutomationControlType.ToolTip,
                    "Packaged tooltip lost its template, font, geometry or native role.");
                var tooltipColor = ((SolidColorBrush)tooltipSurface.Background).Color;
                Root.RequestedTheme = theme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
                await Task.Delay(250);
                Require(PackageTooltip.IsOpen && PackageTooltip.ActualTheme == Root.ActualTheme && ((SolidColorBrush)tooltipSurface.Background).Color != tooltipColor,
                    "Packaged tooltip did not follow its owner's live appearance.");
                Root.RequestedTheme = theme;
                await Task.Delay(250);
                Require(((SolidColorBrush)tooltipSurface.Background).Color == tooltipColor, "Packaged tooltip did not restore its original appearance.");
                var tooltipOwnerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageTooltip.PlacementTarget!);
                ((IInvokeProvider)tooltipOwnerPeer.GetPattern(PatternInterface.Invoke)).Invoke();
                await Task.Delay(50);
                Require(!PackageTooltip.IsOpen, "Packaged tooltip did not dismiss after native activation.");
                PackageTooltip.IsOpen = false;
                await Task.Delay(80);
                Require(Action.ActualWidth > 0 && Heading.ActualHeight > 0, "Consumer controls did not lay out.");
                Require(PackageCard.Padding.Left == 16 && PackageCard.CornerRadius.TopLeft == 20 && PackageCard.ActualWidth > 0,
                    "Packaged card did not load its geometry and content template.");
                Require(PackageSeparator.ActualHeight == 1 && PackageSeparator.ActualWidth > 0 &&
                    FrameworkElementAutomationPeer.CreatePeerForElement(PackageSeparator).GetAutomationControlType() == AutomationControlType.Separator,
                    "Packaged separator lost its geometry or semantics.");
                Require(PackageStatus.ActualHeight == 24 && FrameworkElementAutomationPeer.CreatePeerForElement(PackageStatus).GetName() == "LOCAL",
                    "Packaged status badge lost its template or accessible name.");
                var cardRoot = (Grid)VisualTreeHelper.GetChild(PackageCard, 0);
                var cardMask = (Microsoft.UI.Xaml.Shapes.Path)((Canvas)cardRoot.Children[0]).Children[0];
                Require(((PathGeometry)cardMask.Data).Figures.Single().Segments.OfType<ArcSegment>().Select(arc => arc.Size.Width).SequenceEqual(new double[] { 8, 4, 16, 20 }),
                    "Packaged elevation did not retain consumer corner geometry.");
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
