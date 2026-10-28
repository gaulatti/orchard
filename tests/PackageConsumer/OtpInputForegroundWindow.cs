using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Orchard;
using Windows.Graphics;

namespace PackageConsumer;

/// <summary>Visible, caller-owned OTP input for physical package-consumer checks.</summary>
public sealed class OtpInputForegroundWindow : Window
{
    public OtpInputForegroundWindow(ElementTheme theme)
    {
        if (theme is not (ElementTheme.Light or ElementTheme.Dark))
            throw new ArgumentOutOfRangeException(nameof(theme));

        Title = "Orchard package OTP input foreground";
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' />");
        root.RequestedTheme = theme;
        var stack = new StackPanel { Margin = new Thickness(32), Spacing = 12 };
        var body = (Style)Application.Current.Resources["BleeckerBodyStyle"];
        stack.Children.Add(new TextBlock { Text = $"Independent package OTP input · {theme}", FontSize = 24 });

        BleeckerOtpInput AddInput(string id, bool secure, Action<BleeckerOtpValueRequestedEventArgs> requested)
        {
            var input = new BleeckerOtpInput { Name = id, Value = "482", Length = 6, IsSecure = secure, Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
            AutomationProperties.SetAutomationId(input, id);
            input.ValueRequested += (_, args) => requested(args);
            stack.Children.Add(input);
            return input;
        }

        var acceptedStatus = new TextBlock { Name = "AcceptedStatus", Text = "Accepted: 482", Style = body };
        BleeckerOtpInput? accepted = null;
        accepted = AddInput("AcceptedOtp", false, request => { accepted!.Value = request.Value; acceptedStatus.Text = "Accepted: " + accepted.Value; });
        stack.Children.Add(acceptedStatus);

        var rejectedStatus = new TextBlock { Name = "RejectedStatus", Text = "Rejected: none; retained: 482", Style = body };
        AddInput("RejectedOtp", false, request => rejectedStatus.Text = "Rejected: " + request.Value + "; retained: 482");
        stack.Children.Add(rejectedStatus);

        var secureStatus = new TextBlock { Name = "SecureStatus", Text = "Secure accepted: 3 digits", Style = body };
        BleeckerOtpInput? secure = null;
        secure = AddInput("SecureOtp", true, request => { secure!.Value = request.Value; secureStatus.Text = $"Secure accepted: {secure.Value.Length} digits"; });
        stack.Children.Add(secureStatus);

        root.Children.Add(stack);
        Content = root;
        AppWindow.Resize(new SizeInt32(720, 500));
    }
}
