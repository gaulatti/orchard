using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class SegmentedControlsFixture : StackPanel
{
    private enum FictionalMode { Brief, Detail }
    public SegmentedControlsFixture()
    {
        InitializeComponent();
        Sample.Options = [new("brief", "Brief"), new("detail", "Detail"), new("archive", "Archive", true)]; Sample.SelectedValue = "brief";
        TypedSample.Options = [new(FictionalMode.Brief, "Brief enum"), new(FictionalMode.Detail, "Detail enum")]; TypedSample.SelectedValue = FictionalMode.Detail;
        NarrowSample.Options = [new(1, "Daily coverage"), new(2, "Weekly coverage")]; NarrowSample.SelectedValue = 1;
        DisabledSample.Options = [new("a", "Brief"), new("b", "Detail")]; DisabledSample.SelectedValue = "b";
        LargeSample.Options = [new("a", "Brief"), new("b", "Detail")]; LargeSample.SelectedValue = "a";
    }
    private void Changed(object? sender, EventArgs args) { if (Status is not null) Status.Text = $"Selection: {Sample.SelectedValue}"; }
}
