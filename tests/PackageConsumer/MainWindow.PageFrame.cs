using Microsoft.UI.Xaml;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private void VerifyPackagePageFrame()
    {
        Require(ReferenceEquals(PackagePageFrame.Content, PackagePageFrameInput) && PackagePageFrameInput.Text == "Independent page boundary" &&
            PackagePageFrame.MaxWidth == 768 && !PackagePageFrame.IsTabStop, "Packaged PageFrame lost its content, width recipe or passive behavior.");
        PackagePageFrame.FrameWidth = Orchard.BleeckerPageFrameWidth.Full;
        PackagePageFrame.Gutter = Orchard.BleeckerPageFrameGutter.None;
        PackagePageFrame.VerticalSpacing = Orchard.BleeckerPageFrameVerticalSpacing.None;
        Root.UpdateLayout();
        Require(double.IsPositiveInfinity(PackagePageFrame.MaxWidth) && PackagePageFrame.Padding == new Thickness(0) && ReferenceEquals(PackagePageFrame.Content, PackagePageFrameInput),
            "Packaged PageFrame did not apply live recipes or retained content.");
        PackagePageFrame.FrameWidth = Orchard.BleeckerPageFrameWidth.Reading;
        PackagePageFrame.Gutter = Orchard.BleeckerPageFrameGutter.Comfortable;
        PackagePageFrame.VerticalSpacing = Orchard.BleeckerPageFrameVerticalSpacing.Compact;
        Root.UpdateLayout();
    }
}
