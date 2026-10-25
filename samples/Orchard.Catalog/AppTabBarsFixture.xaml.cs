using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;

public sealed partial class AppTabBarsFixture : StackPanel
{
    private enum FictionalDesk { Overview, Queue }
    public AppTabBarsFixture()
    {
        InitializeComponent();
        Sample.Tabs = [Tab("overview", "Overview", Symbol.Home), Tab("queue", "Review queue", Symbol.Mail, 127), Tab("archive", "Archive", Symbol.Folder, isDisabled: true)]; Sample.SelectedValue = "overview";
        TypedSample.Tabs = [Tab(FictionalDesk.Overview, "Overview enum", Symbol.Home), Tab(FictionalDesk.Queue, "Queue enum", Symbol.Mail, 3)]; TypedSample.SelectedValue = FictionalDesk.Queue;
        NarrowSample.Tabs = [Tab(1, "Daily coverage", Symbol.Home), Tab(2, "Weekly coverage", Symbol.Calendar, 99)]; NarrowSample.SelectedValue = 1;
        DisabledSample.Tabs = [Tab("a", "Overview", Symbol.Home), Tab("b", "Queue", Symbol.Mail, 100)]; DisabledSample.SelectedValue = "b";
        LargeSample.Tabs = [Tab("a", "Overview", Symbol.Home), Tab("b", "Queue", Symbol.Mail, 7)]; LargeSample.SelectedValue = "a";
    }
    private static BleeckerAppTab Tab(object value, string label, Symbol icon, long? badge = null, bool isDisabled = false) => new(value, label, new FontIconSource { Glyph = char.ConvertFromUtf32((int)icon) }, badge, isDisabled: isDisabled);
    private void Changed(object? sender, EventArgs args) { if (Status is not null) Status.Text = $"Selected: {Sample.SelectedValue}"; }
}
