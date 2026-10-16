using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private int packageStepInvocations;
    private BleeckerStep? lastPackageStep;
    private async Task VerifyStepperAsync()
    {
        PackageStepper.ApplyTemplate(); Root.UpdateLayout();
        var buttons = BrandDescendants<Button>(PackageStepper).ToArray();
        Require(buttons.Length == 2 && buttons.All(button => button.IsEnabled && button.ActualWidth == 32 && button.ActualHeight == 32), "Packaged Stepper lost native actions or circle sizing.");
        var first = PackageStepper.Steps[0];
        Require(AutomationProperties.GetName(buttons[0]) == first.Label && AutomationProperties.GetItemStatus(buttons[0]) == "Completed" &&
            AutomationProperties.GetItemStatus(buttons[1]) == "Active" && AutomationProperties.GetPositionInSet(buttons[1]) == 2, "Packaged Stepper lost derived status, names or ordinal metadata.");
        await AcquireConsumerInputAsync();
        Require(buttons[0].Focus(FocusState.Keyboard), "Could not focus the packaged step button.");
        var count = packageStepInvocations;
        await PaginationConsumerKeyAsync(0x20, buttons[0], buttons[0], () => Require(buttons[0].IsPressed && packageStepInvocations == count, "Packaged Stepper invoked before native Space was released."));
        Require(packageStepInvocations == count + 1 && ReferenceEquals(lastPackageStep, first), "Packaged Stepper did not invoke its original step after native Space release.");
        Require(PackageStepper.ActiveStep == "review" && first.Status is null, "Packaged Stepper advanced controlled state.");
        first.Label = "Updated consumer draft"; first.Status = BleeckerStepStatus.Error; Root.UpdateLayout();
        Require(ReferenceEquals(BrandDescendants<Button>(PackageStepper).First(), buttons[0]) && AutomationProperties.GetName(buttons[0]) == first.Label &&
            AutomationProperties.GetItemStatus(buttons[0]) == "Error", "Packaged Stepper did not apply live metadata to its retained native action.");
        first.IsDisabled = true; Root.UpdateLayout();
        Require(!buttons[0].IsEnabled && !FrameworkElementAutomationPeer.CreatePeerForElement(buttons[0]).IsEnabled(), "Packaged disabled step remained enabled in UIA.");
        first.Label = "Consumer draft"; first.Status = null; first.IsDisabled = false;
    }
}
