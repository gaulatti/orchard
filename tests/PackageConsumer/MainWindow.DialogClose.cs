using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyPackageCloseAsync()
    {
        Require(PackageClose.ActualWidth == 36 && PackageClose.ActualHeight == 36 && PackageClose.IconSize == 18, "Packaged Close lost its dimensions or icon size.");
        var expectedBackground = PackageClose.ActualTheme == ElementTheme.Light ? Microsoft.UI.Colors.White : Windows.UI.Color.FromArgb(255, 24, 37, 51);
        Require(PackageClose.Background is SolidColorBrush paint && paint.Color == expectedBackground, "Packaged Close did not resolve its current theme resource.");
        var peer = FrameworkElementAutomationPeer.CreatePeerForElement(PackageClose);
        Require(peer.GetAutomationControlType() == AutomationControlType.Button && peer.GetName() == "Close package preview", "Packaged Close lost native name/role.");
        var parameter = new object(); var command = new PackageCloseCommand(); PackageClose.Command = command; PackageClose.CommandParameter = parameter;
        ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();
        await AwaitAsync(() => command.Executions == 1, "Packaged Close did not invoke the consumer command.");
        Require(ReferenceEquals(command.Parameter, parameter) && PackageClose.IsLoaded, "Packaged Close changed the command parameter or dismissed the consumer.");
        PackageClose.IconSize = 24; Root.UpdateLayout();
        Require(PackageClose.Content is Viewbox { ActualWidth: 24, ActualHeight: 24 }, "Packaged Close did not update icon geometry.");
        command.Enabled = false; command.Notify(); Root.UpdateLayout();
        Require(!PackageClose.IsEnabled, "Packaged Close ignored CanExecute.");
        command.Enabled = true; command.Notify(); PackageClose.Command = null; PackageClose.CommandParameter = null; PackageClose.IconSize = 18;
    }
    private sealed class PackageCloseCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        internal bool Enabled { get; set; } = true;
        internal int Executions { get; private set; }
        internal object? Parameter { get; private set; }
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) { Require(Enabled, "Packaged disabled Close executed."); Executions++; Parameter = parameter; }
        internal void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
