using System.Numerics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;
public sealed partial class MainWindow
{
    private async Task VerifyIconBadgeAsync()
    {
        var editor = new TextBox { Text = "Retained package child", Width = 120 };
        var badge = new BleeckerIconBadge { Content = editor };
        var host = new Border { Margin = new Thickness(32), Background = Root.Background, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Child = badge };
        Root.Children.Add(host); var passed = false; var phases = new List<object>();
        async Task PresentAsync()
        {
            Root.UpdateLayout(); var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var frames = 0;
            void Rendering(object? sender, object args) { if (badge.IsLoaded && badge.ActualWidth > 0 && ++frames >= 2) completion.TrySetResult(); }
            CompositionTarget.Rendering += Rendering; try { await completion.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendering; }
        }
        try
        {
            await PresentAsync();
            foreach (var size in Enum.GetValues<BleeckerIconBadgeSize>()) foreach (var variant in Enum.GetValues<BleeckerIconBadgeVariant>())
            {
                badge.Size = size; badge.Variant = variant; await PresentAsync();
                var extent = size == BleeckerIconBadgeSize.Medium ? 56 : 64;
                Require(badge.ActualWidth == extent && badge.ActualHeight == extent && !badge.IsTabStop && ReferenceEquals(badge.Content, editor) && editor.Text == "Retained package child", "Packaged IconBadge lost its geometry or consumer child.");
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(badge);
                Require(peer.GetAutomationControlType() == AutomationControlType.Group && peer.GetPattern(PatternInterface.Invoke) is null, "Packaged IconBadge gained button semantics.");
                phases.Add(new { size = size.ToString(), variant = variant.ToString(), extent });
            }
            badge.Width = 160; await PresentAsync(); await AcquireConsumerInputAsync();
            Require(editor.Focus(FocusState.Keyboard), "Packaged IconBadge child refused focus."); badge.Size = BleeckerIconBadgeSize.Medium; badge.Variant = BleeckerIconBadgeVariant.Outlined; await PresentAsync();
            Require(badge.ActualWidth == 160 && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), editor), "Packaged IconBadge reset consumer size or focus.");
            AutomationProperties.SetName(badge, "Independent marker"); Require(FrameworkElementAutomationPeer.CreatePeerForElement(badge).GetName() == "Independent marker", "Packaged IconBadge lost its name.");
            var red = new SolidColorBrush(Microsoft.UI.Colors.Red); badge.Foreground = red; badge.Variant = BleeckerIconBadgeVariant.Subtle;
            Require(ReferenceEquals(badge.Foreground, red), "Packaged IconBadge overwrote consumer foreground.");
            foreach (var (value, expected) in new (object Value, string Text)[] { (-0d, "0"), (1e21, "1e+21"), (double.NaN, "NaN"), (BigInteger.Parse("12345678901234567890123456789"), "12345678901234567890123456789"), (true, "") })
            {
                badge.Content = value; await PresentAsync();
                var text = string.Concat(BrandDescendants<TextBlock>(badge).Where(block => block.Visibility == Visibility.Visible).Select(block => block.Text));
                Require(text == expected && Equals(badge.Content, value), "Packaged IconBadge changed scalar content or ownership.");
                if (value is double original) Require(BitConverter.DoubleToInt64Bits((double)badge.Content) == BitConverter.DoubleToInt64Bits(original), "Packaged IconBadge changed the original number bits.");
                phases.Add(new { scalar = expected, retained = true });
            }
            passed = true;
        }
        finally
        {
            Root.Children.Remove(host); var args = Environment.GetCommandLineArgs(); var output = args[Array.IndexOf(args, "--verify-output") + 1];
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, "consumer-icon-badge-" + Root.RequestedTheme + ".json"), JsonSerializer.Serialize(new { passed, phases }));
        }
    }
}
