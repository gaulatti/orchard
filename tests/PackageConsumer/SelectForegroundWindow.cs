using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Orchard;
using Windows.Graphics;

namespace PackageConsumer;

/// <summary>A visible, caller-owned Select fixture for physical NuGet-consumer input.</summary>
public sealed class SelectForegroundWindow : Window
{
    private readonly TextBlock status;

    public SelectForegroundWindow(ElementTheme theme)
    {
        if (theme is not (ElementTheme.Light or ElementTheme.Dark))
            throw new ArgumentOutOfRangeException(nameof(theme));

        Title = "Orchard package Select foreground";
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' />");
        root.RequestedTheme = theme;
        var stack = new StackPanel { Margin = new Thickness(40), Spacing = 18 };
        stack.Children.Add(new TextBlock { Text = $"Independent package Select · {theme}", FontSize = 24 });
        status = new TextBlock { Name = "Status", Text = "No value requested." };
        stack.Children.Add(status);

        BleeckerSelectOption[] options =
        [
            new("a", "Local library Alpha"),
            new("b", "Local library Beta"),
            new("disabled", "Unavailable local library", true)
        ];
        var select = new BleeckerSelect { Name = "PackageSelect", Width = 260, HorizontalAlignment = HorizontalAlignment.Left, Value = "a", Options = options };
        AutomationProperties.SetName(select, "Independent package choice");
        select.ValueRequested += Requested;
        stack.Children.Add(select);

        var header = new BleeckerHeaderSelect { Name = "PackageHeaderSelect", Placeholder = "Local suite", Options = options };
        AutomationProperties.SetName(header, "Independent package suite");
        header.ValueRequested += Requested;
        stack.Children.Add(header);

        root.Children.Add(stack);
        Content = root;
        AppWindow.Resize(new SizeInt32(640, 420));
    }

    private void Requested(object? sender, BleeckerSelectRequestedEventArgs args)
    {
        var owner = (BleeckerSelect)sender!;
        owner.Value = args.Value;
        status.Text = $"{owner.Name} accepted: {args.Value}";
    }
}
