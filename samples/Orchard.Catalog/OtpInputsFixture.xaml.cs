using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class OtpInputsFixture : StackPanel
{
    public OtpInputsFixture() => InitializeComponent();
    private void Accept(object? sender, BleeckerOtpValueRequestedEventArgs args)
    {
        var input = (BleeckerOtpInput)sender!; input.Value = args.Value;
        if (ReferenceEquals(input, Sample) && Status is not null) Status.Text = "Accepted fictional value: " + input.Value;
    }
}
