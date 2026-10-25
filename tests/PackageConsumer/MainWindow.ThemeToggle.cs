using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Orchard;

namespace PackageConsumer;
public sealed partial class MainWindow
{
    private async Task VerifyPackageThemeToggleAsync()
    {
        Root.UpdateLayout(); var producer = new TextBlock { Tag = BleeckerThemeMode.System };
        PackageThemeToggle.SetBinding(BleeckerThemeToggle.ModeProperty, new Binding { Source = producer, Path = new PropertyPath("Tag"), Mode = BindingMode.OneWay });
        var requests = new List<BleeckerThemeMode>(); EventHandler<BleeckerThemeRequestedEventArgs> handler = (_, args) => requests.Add(args.Mode); PackageThemeToggle.ThemeRequested += handler;
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageThemeToggle); var invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
        Require(peer.GetName() == "Switch to light theme" && peer.GetAutomationControlType() == AutomationControlType.Button, "Packaged Theme Toggle lost native button metadata.");
        invoke.Invoke(); await Task.Delay(50); Root.UpdateLayout(); Require(requests.SequenceEqual(new[] { BleeckerThemeMode.Light }) && PackageThemeToggle.Mode == BleeckerThemeMode.System, "Packaged Theme Toggle mutated its consumer's rejected mode.");
        producer.Tag = BleeckerThemeMode.Dark; Root.UpdateLayout(); Require(PackageThemeToggle.Mode == BleeckerThemeMode.Dark && peer.GetName() == "Switch to system theme" && PackageThemeToggle.GetBindingExpression(BleeckerThemeToggle.ModeProperty) is not null, "Packaged Theme Toggle lost mode binding or live naming.");
        PackageThemeToggle.IsEnabled = false; var rejected = false; try { invoke.Invoke(); } catch (ElementNotEnabledException) { rejected = true; } Require(rejected && requests.Count == 1, "Packaged disabled Theme Toggle invoked."); PackageThemeToggle.IsEnabled = true;
        AutomationProperties.SetName(PackageThemeToggle, "Caller appearance"); ToolTipService.SetToolTip(PackageThemeToggle, "Caller tooltip"); producer.Tag = BleeckerThemeMode.Light; Root.UpdateLayout(); Require(peer.GetName() == "Caller appearance" && ToolTipService.GetToolTip(PackageThemeToggle) as string == "Caller tooltip", "Packaged Theme Toggle replaced caller metadata.");
        PackageThemeToggle.ClearValue(AutomationProperties.NameProperty); PackageThemeToggle.ClearValue(ToolTipService.ToolTipProperty); var originalStyle = PackageThemeToggle.Style;
        var callerBaseStyle = new Style(typeof(BleeckerThemeToggle)) { BasedOn = originalStyle }; callerBaseStyle.Setters.Add(new Setter(AutomationProperties.NameProperty, "")); callerBaseStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, "Styled tooltip")); PackageThemeToggle.Style = new Style(typeof(BleeckerThemeToggle)) { BasedOn = callerBaseStyle };
        producer.Tag = BleeckerThemeMode.Dark; Root.UpdateLayout(); Require(peer.GetName() == "" && ToolTipService.GetToolTip(PackageThemeToggle) as string == "Styled tooltip", "Packaged Theme Toggle replaced BasedOn metadata.");
        PackageThemeToggle.Style = originalStyle; Root.UpdateLayout(); Require(peer.GetName() == "Switch to system theme" && ((ToolTip)ToolTipService.GetToolTip(PackageThemeToggle)).Content as string == "Switch to system theme", "Packaged Theme Toggle did not restore defaults after style removal.");
        var template = PackageThemeToggle.Template; PackageThemeToggle.Template = null; Root.UpdateLayout(); PackageThemeToggle.Template = template; Root.UpdateLayout(); producer.Tag = BleeckerThemeMode.System; Root.UpdateLayout(); Require(PackageThemeToggle.Mode == BleeckerThemeMode.System && PackageThemeToggle.GetBindingExpression(BleeckerThemeToggle.ModeProperty) is not null, "Packaged Theme Toggle template replacement lost its binding.");
        PackageThemeToggle.ThemeRequested -= handler; PackageThemeToggle.ClearValue(BleeckerThemeToggle.ModeProperty); PackageThemeToggle.ClearValue(AutomationProperties.NameProperty); PackageThemeToggle.ClearValue(ToolTipService.ToolTipProperty); Root.UpdateLayout();
    }
}
