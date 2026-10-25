using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private void VerifyPackageField()
    {
        Require(ReferenceEquals(PackageField.Content, PackageFieldInput) && PackageFieldInput.Text == "Independent Field", "Packaged Field replaced its consumer control or value.");
        Require(AutomationProperties.GetLabeledBy(PackageFieldInput) is not null && AutomationProperties.GetDescribedBy(PackageFieldInput).Count == 2,
            "Packaged Field lost its label or description/error references.");
        Require(!AutomationProperties.GetIsDataValidForForm(PackageFieldInput) && !PackageFieldInput.IsError && !AutomationProperties.GetIsRequiredForForm(PackageFieldInput),
            "Packaged Field lost invalid semantics or changed the child's visual/native required state.");
        var id = AutomationProperties.GetAutomationId(PackageFieldInput);
        PackageField.Error = null; Root.UpdateLayout();
        Require(AutomationProperties.GetIsDataValidForForm(PackageFieldInput) && AutomationProperties.GetDescribedBy(PackageFieldInput).Count == 1,
            "Packaged Field did not restore validity/remove only its error reference.");
        PackageField.Description = null; Root.UpdateLayout();
        Require(AutomationProperties.GetDescribedBy(PackageFieldInput).Count == 0 && AutomationProperties.GetAutomationId(PackageFieldInput) == id,
            "Packaged Field lost child identity while messages changed.");
        PackageField.Description = "Consumer description"; PackageField.Error = "Consumer error"; Root.UpdateLayout();
    }
}
