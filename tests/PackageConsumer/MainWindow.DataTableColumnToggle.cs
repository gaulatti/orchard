using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyColumnToggleAsync()
    {
        var source = new ConsumerColumnSource(); var originalRows = source.Rows.ToArray(); var originalColumns = source.Columns.ToArray(); var picker = new BleeckerDataTableColumnToggle { Table = source, HorizontalAlignment = HorizontalAlignment.Right };
        var cells = new TextBlock { Text = source.VisibleText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 56, 0, 0), VerticalAlignment = VerticalAlignment.Top };
        EventHandler changed = (_, _) => cells.Text = source.VisibleText; source.Changed += changed;
        var host = new Grid { Width = 520, Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = Root.Background }; host.Children.Add(picker); host.Children.Add(cells); Root.Children.Add(host);
        try
        {
            await AwaitAsync(() => picker.IsLoaded && source.Subscribers == 2, "Packaged column picker did not observe its original consumer."); picker.ApplyTemplate(); Root.UpdateLayout();
            var trigger = BrandDescendants<Button>(picker).Single(item => item.Name == "PART_Trigger"); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(trigger); var expand = (IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse);
            Require(peer.GetName() == "Columns", "Packaged column trigger lost its source label."); expand.Expand();
            Popup? Popup() => VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).SingleOrDefault(item => item.Child is FrameworkElement element && element.Name == "PART_Dialog");
            await AwaitAsync(() => Popup() is { } popup && BrandDescendants<BleeckerCheckbox>(popup.Child).Count() == 2, "Packaged original visibility columns did not render.");
            var checks = BrandDescendants<BleeckerCheckbox>(Popup()!.Child).ToArray(); var first = checks.Single(item => ReferenceEquals(item.Tag, source.Columns[0]));
            Require(checks.All(item => item.Tag is ConsumerColumnSource.Column column && column.HasAccessor && column.CanHide) && checks.All(item => item.IsChecked == true), "Packaged picker fabricated eligibility or checked state.");
            var toggle = (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(first).GetPattern(PatternInterface.Toggle); toggle.Toggle();
            await AwaitAsync(() => source.Requests == 1 && !source.Columns[0].IsVisible && cells.Text == "Draft|Ready", "Packaged column toggle did not change the original visible cells exactly once.");
            Require(ReferenceEquals(source.LastColumn, source.Columns[0]) && ReferenceEquals(picker.Table, source), "Packaged picker replaced its original table or column.");
            source.SetExternal(true); await AwaitAsync(() => first.IsChecked == true && cells.Text == "Harbor:Draft|Reed:Ready", "External visibility did not update packaged original rows."); Require(source.Requests == 1, "External visibility created a consumer request.");
            picker.Label = "Fields"; Root.UpdateLayout(); Require(peer.GetName() == "Fields", "Packaged trigger label did not update while open.");
            await AcquireConsumerInputAsync(); Require(first.Focus(FocusState.Keyboard), "Packaged column checkbox refused keyboard focus.");
            var key = await PaginationConsumerKeyAsync(0x20, first, first, () => Require(source.Requests == 1, "Packaged checkbox committed before its normal Space release."));
            await AwaitAsync(() => source.Requests == 2 && !source.Columns[0].IsVisible && cells.Text == "Draft|Ready", "Packaged native Space did not reach the original column/cells.");
            Require(source.Rows.Zip(originalRows).All(pair => ReferenceEquals(pair.First, pair.Second)) && source.Columns.Zip(originalColumns).All(pair => ReferenceEquals(pair.First, pair.Second)), "Packaged picker replaced original consumer rows or columns.");
            expand.Collapse(); await AwaitAsync(() => expand.ExpandCollapseState == ExpandCollapseState.Collapsed && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), trigger), "Packaged column dismissal lost trigger focus.");
            picker.IsEnabled = false; var disabled = false; try { expand.Expand(); } catch (ElementNotEnabledException) { disabled = true; } Require(disabled && source.Requests == 2, "Disabled packaged picker accepted a mutation path."); picker.IsEnabled = true;
            var args = Environment.GetCommandLineArgs(); var output = args[Array.IndexOf(args, "--verify-output") + 1];
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!, $"consumer-column-toggle-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed = true, originalColumns = true, originalRows = true, requests = source.Requests, externalRequests = 0, disabledRejected = disabled, key, sourceSubscribers = source.Subscribers }));
        }
        finally
        {
            Root.Children.Remove(host); source.Changed -= changed;
            await AwaitAsync(() => !picker.IsLoaded && source.Subscribers == 0, "Packaged column picker retained an original source observer.");
        }
        var arguments = Environment.GetCommandLineArgs(); var resultPath = arguments[Array.IndexOf(arguments, "--verify-output") + 1];
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(resultPath)!, $"consumer-column-toggle-unload-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed = true, pickerLoaded = picker.IsLoaded, finalObservers = source.Subscribers }));
    }
    private sealed class ConsumerColumnSource : IBleeckerDataTableColumnVisibilitySource
    {
        internal sealed record Row(string Title, string Status);
        internal sealed class Column(ConsumerColumnSource owner, string id, string header, bool accessor = true) : IBleeckerDataTableVisibilityColumn
        {
            public string Id => id;
            public object Header => header;
            public bool HasAccessor => accessor;
            public bool CanHide => true;
            public bool IsVisible => id != "title" || owner.titleVisible;
            public void ToggleVisibility(bool visible) { owner.Requests++; owner.LastColumn = this; if (id == "title") owner.titleVisible = visible; owner.changed?.Invoke(owner, EventArgs.Empty); }
        }
        private bool titleVisible = true;
        private EventHandler? changed;
        internal readonly Row[] Rows = [new("Harbor", "Draft"), new("Reed", "Ready")];
        internal readonly Column[] Columns;
        internal int Requests, Subscribers;
        internal Column? LastColumn;
        internal ConsumerColumnSource() => Columns = [new(this, "title", "Story"), new(this, "status", "Status"), new(this, "display", "Display", false)];
        public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
        public IReadOnlyList<IBleeckerDataTableVisibilityColumn> GetAllColumns() => Columns;
        internal string VisibleText => string.Join('|', Rows.Select(row => titleVisible ? row.Title + ":" + row.Status : row.Status));
        internal void SetExternal(bool value) { titleVisible = value; changed?.Invoke(this, EventArgs.Empty); }
    }
}
