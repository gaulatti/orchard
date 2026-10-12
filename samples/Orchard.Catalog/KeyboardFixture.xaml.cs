using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class KeyboardFixture : UserControl
{
    private readonly ObservableCollection<string> keys = ["⌘", "K"];
    public KeyboardFixture()
    {
        InitializeComponent();
        Combo.Keys = keys;
        ThreeKeys.Keys = new[] { "Ctrl", "Shift", "P" };
    }

    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        UpdateLayout();
        await Task.Delay(100);
        Require(Single.ActualHeight == 20 && Single.ActualWidth >= 20 && Empty.ActualWidth == 20 && Single.Padding.Left == 6,
            "Keycap geometry differs from the pinned contract.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(Combo);
        Require(peer.GetAutomationControlType() == AutomationControlType.Group && peer.GetName() == "⌘+K" && !Combo.IsTabStop,
            "Shortcut group lost its accessible name, role or display-only contract.");
        var group = Find<StackPanel>(Combo, "PART_Combination");
        Require(group.Children.Count == 3 && group.Spacing == 2 && ((TextBlock)group.Children[1]).Text == "+", "Shortcut separators differ.");
        Require(Single.FontFamily.Source == "Consolas" && Single.FontSize == 11 && Single.FontWeight.Weight == 500, "Keycap typography differs.");
        var expected = BleeckerTokens.Colors(scheme)[scheme == BleeckerColorScheme.Dark ? "textPrimary" : "textSecondary"];
        Require(Hex(Single.Foreground) == expected, "Keycap foreground differs from the theme.");
        keys.Add("K");
        Require(group.Children.Count == 5 && peer.GetName() == "⌘+K+K", "Observable key insertion or duplicate keys were lost.");
        keys.RemoveAt(2);
        Combo.Keys = new[] { "Alt" };
        keys.Add("detached");
        Require(peer.GetName() == "Alt" && group.Children.Count == 1, "Replaced key collection retained an observer or lost one-key group semantics.");
        Combo.Keys = Array.Empty<string>();
        Require(peer.GetName() == "Esc" && peer.GetAutomationControlType() == AutomationControlType.Text && Find<Grid>(Combo, "PART_Single").Visibility == Visibility.Visible,
            "Empty keys did not restore consumer content.");
        AutomationProperties.SetName(Combo, "Explicit shortcut");
        Combo.Keys = keys;
        Require(peer.GetName() == "Explicit shortcut", "Keys replaced the consumer accessibility name.");
        Combo.ClearValue(AutomationProperties.NameProperty);
        keys.Remove("detached");
        UpdateLayout();
        await MainWindow.CaptureElementAsync(this, Path.Combine(folder, $"keyboard-{scheme}.png"));
    }

    private static string Hex(Brush brush)
    {
        var color = ((SolidColorBrush)brush).Color;
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
    private static T Find<T>(DependencyObject parent, string name) where T : FrameworkElement
    {
        if (parent is T match && match.Name == name) return match;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            try { return Find<T>(VisualTreeHelper.GetChild(parent, index), name); }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException($"Missing keyboard template part {name}.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
