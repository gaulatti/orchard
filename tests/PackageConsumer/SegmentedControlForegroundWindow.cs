using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Orchard;
using Windows.Graphics;

namespace PackageConsumer;

/// <summary>A caller-owned RTL segment group for physical package-consumer input.</summary>
public sealed class SegmentedControlForegroundWindow : Window
{
    public SegmentedControlForegroundWindow(ElementTheme theme)
    {
        if (theme is not (ElementTheme.Light or ElementTheme.Dark))
            throw new ArgumentOutOfRangeException(nameof(theme));

        Title = "Orchard package segmented control foreground";
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' />");
        root.RequestedTheme = theme;
        var stack = new StackPanel { Margin = new Thickness(32), Spacing = 12 };
        stack.Children.Add(new TextBlock { Text = $"Independent package segmented control · RTL · {theme}", FontSize = 24 });
        var status = new TextBlock { Text = "Selection: east", Style = (Style)Application.Current.Resources["BleeckerBodyStyle"] };
        AutomationProperties.SetAutomationId(status, "SegmentedSelectionStatus");
        var group = new BleeckerSegmentedControl
        {
            FlowDirection = FlowDirection.RightToLeft,
            HorizontalAlignment = HorizontalAlignment.Left,
            Options = [new("east", "East"), new("west", "West"), new("closed", "Closed", true)],
            SelectedValue = "east"
        };
        AutomationProperties.SetAutomationId(group, "RtlSegmentedGroup");
        AutomationProperties.SetName(group, "Fictional RTL coverage mode");
        group.SelectionChanged += (_, _) => status.Text = "Selection: " + group.SelectedValue;
        stack.Children.Add(group);
        stack.Children.Add(status);
        root.Children.Add(stack);
        Content = root;
        AppWindow.Resize(new SizeInt32(720, 340));
    }
}
