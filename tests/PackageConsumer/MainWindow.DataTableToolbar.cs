using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Orchard;
using System.Runtime.InteropServices;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private readonly ConsumerFilterSource toolbarSource = new();

    private void InitializeToolbar()
    {
        PackageToolbar.Table = toolbarSource;
        toolbarSource.Changed += (_, _) => PackageToolbarRows.Text = toolbarSource.VisibleText;
        PackageToolbarRows.Text = toolbarSource.VisibleText;
    }

    private async Task VerifyToolbarAsync()
    {
        toolbarSource.Initial = "";
        toolbarSource.Global = null;
        toolbarSource.Column.SetFilterValue("");
        PackageToolbar.ApplyTemplate();
        Root.UpdateLayout();
        var search = BrandDescendants<BleeckerInput>(PackageToolbar).Single();
        Require(search.ActualHeight == 36 && search.FontSize == 14 && search.Padding.Left == 14,
            "Packaged toolbar lost its medium Input override.");
        await AcquireConsumerInputAsync();
        search.StartBringIntoView();
        Require(search.Focus(FocusState.Keyboard), "Packaged toolbar search refused focus.");
        var keys = new[]
        {
            new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x48 } } },
            new ConsumerInput { Type = 1, Data = new ConsumerInputData { Keyboard = new ConsumerKeyboard { VirtualKey = 0x48, Flags = 2 } } }
        };
        Require(SendInput(2, keys, Marshal.SizeOf<ConsumerInput>()) == 2, "Packaged toolbar H did not enter native input.");
        await AwaitAsync(() => PackageToolbarRows.Text == "Harbor" && search.Text.Equals("h", StringComparison.OrdinalIgnoreCase),
            "Packaged toolbar did not filter its real consumer rows.");
        Require(ReferenceEquals(PackageToolbar.Table, toolbarSource) && ReferenceEquals(toolbarSource.LastColumn, toolbarSource.Column) &&
            ReferenceEquals(toolbarSource.VisibleRows.Single(), toolbarSource.Rows[0]), "Packaged toolbar replaced consumer objects.");
        toolbarSource.Initial = "Reed";
        toolbarSource.Global = "Reed";
        var reset = BrandDescendants<Button>(PackageToolbar).Single(button => button.Name == "PART_Reset");
        var caption = BrandDescendants<TextBlock>(reset).Single(text => text.Text == "Reset");
        var baseline = caption.TransformToVisual(reset).TransformPoint(new Windows.Foundation.Point()).Y + caption.BaselineOffset;
        Require(Math.Abs(baseline - 22.25) <= 1d / 32, "Packaged Reset caption differs from the measured source baseline.");
        ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(reset).GetPattern(PatternInterface.Invoke)).Invoke();
        await AwaitAsync(() => search.Text == "Reed" && PackageToolbarRows.Text == "Reed", "Packaged toolbar reset did not restore initial filters.");
        Require(Equals(toolbarSource.GlobalFilter, "Reed") && reset.IsLoaded && ReferenceEquals(PackageToolbarFilter.Parent, search.Parent),
            "Packaged toolbar lost global state or original filter content.");
        PackageToolbar.SearchColumn = "missing";
        Root.UpdateLayout();
        await AwaitAsync(() => search.Text == "", "Packaged missing column did not render controlled empty text.");
        PackageToolbar.SearchColumn = "title";
        toolbarSource.Initial = "";
        toolbarSource.Global = null;
        toolbarSource.Column.SetFilterValue("");
        await AwaitAsync(() => search.Text == "" && PackageToolbarRows.Text == "Harbor|Reed|Vale", "Packaged toolbar did not restore rows.");
    }

    // Independent application model: no source-project reference or catalog helper.
    private sealed class ConsumerFilterSource : IBleeckerDataTableFilterSource
    {
        internal sealed record Row(string Title);
        internal sealed class FilterColumn(ConsumerFilterSource owner) : IBleeckerDataTableFilterColumn
        {
            internal string Value = "";
            public object? GetFilterValue() => Value;
            public void SetFilterValue(object? value)
            {
                Value = (string?)value ?? "";
                owner.LastColumn = this;
                owner.Publish();
            }
        }
        internal readonly Row[] Rows = [new("Harbor"), new("Reed"), new("Vale")];
        internal readonly FilterColumn Column;
        internal FilterColumn? LastColumn;
        internal string Initial = "";
        internal string? Global;
        internal ConsumerFilterSource() => Column = new(this);
        public event EventHandler? Changed;
        public int ColumnFilterCount => Column.Value.Length > 0 ? 1 : 0;
        public object? GlobalFilter => Global;
        public IBleeckerDataTableFilterColumn? GetColumn(string id) => id == "title" ? Column : null;
        public void ResetColumnFilters() => Column.SetFilterValue(Initial);
        private void Publish() => Changed?.Invoke(this, EventArgs.Empty);
        internal Row[] VisibleRows => Rows.Where(row => row.Title.Contains(Column.Value, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(Global) || row.Title.Contains(Global, StringComparison.OrdinalIgnoreCase))).ToArray();
        internal string VisibleText => string.Join('|', VisibleRows.Select(row => row.Title));
    }
}
