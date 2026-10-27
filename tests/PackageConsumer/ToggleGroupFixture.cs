using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Orchard;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class ToggleGroupFixture
{
    private sealed class ToggleCommand(object parameter) : ICommand
    {
        internal int Executions;
        private bool allowed = true;
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? value) => allowed && ReferenceEquals(value, parameter);
        public void Execute(object? value) { if (!CanExecute(value)) throw new InvalidOperationException("Toggle command lost its caller parameter or eligibility."); Executions++; }
        internal void Allow(bool value) { allowed = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }
    internal static async Task RunAsync(Window window, string output, string hostAssembly, Func<FrameworkElement>? catalogFactory = null)
    {
        var checks = new List<object>(); var failures = new List<object>(); var observations = new List<object>(); var standaloneToggles = new List<object>();
        var guards = 0; var focus = 0; var activations = 0; var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var root = new Canvas { Width = 900, Height = 1100 }; window.Content = root;
        root.GotFocus += (_, _) => focus++; window.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard() { guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) && foreground != Environment.ProcessId && focus == 0 && activations == 0, "Toggle Group fixture acquired visibility, focus or activation."); }
        async Task Layout() { for (var i = 0; i < 4; i++) { root.Measure(new Size(900, 1100)); root.Arrange(new Rect(0, 0, 900, 1100)); root.UpdateLayout(); Guard(); await Task.Delay(10); } }
        async Task Case(string id, Func<Task> action) { try { Guard(); await action(); Guard(); checks.Add(new { id, passed = true }); } catch (Exception error) { checks.Add(new { id, passed = false }); failures.Add(new { id, error = error.ToString() }); } finally { root.Children.Clear(); await Layout(); } }
        static Binding Bind(object source, string name) => new() { Source = source, Path = new PropertyPath(name), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
        static ISelectionItemProvider Selection(BleeckerToggleGroupItem item) => (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.SelectionItem);
        static IToggleProvider Toggle(BleeckerToggleGroupItem item) => (IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.Toggle);
        static ISelectionProvider GroupSelection(BleeckerToggleGroup group) => (ISelectionProvider)FrameworkElementAutomationPeer.CreatePeerForElement(group).GetPattern(PatternInterface.Selection);
        static (BleeckerToggleGroup Group, BleeckerToggleGroupItem A, BleeckerToggleGroupItem B) Make(BleeckerSelectionMode mode = BleeckerSelectionMode.Single)
        { var a = new BleeckerToggleGroupItem { Name = "a", Value = "a", Content = "List" }; var b = new BleeckerToggleGroupItem { Name = "b", Value = "b", Content = "Grid" }; var group = new BleeckerToggleGroup { SelectionMode = mode, SelectedValue = "a" }; group.Items.Add(a); group.Items.Add(b); return (group, a, b); }
        async Task Mount(BleeckerToggleGroup group) { root.Children.Add(group); await Layout(); Require(group.IsLoaded && group.Items.Cast<BleeckerToggleGroupItem>().All(i => i.IsLoaded), "Real Toggle Group containers did not mount."); }
        void Snapshot(string id, BleeckerToggleGroup group, params BleeckerToggleGroupItem[] items)
        { observations.Add(new { id, selectedValue = group.SelectedValue, selectedValues = group.SelectedValues.ToArray(), groupLoaded = group.IsLoaded, canSelectMultiple = GroupSelection(group).CanSelectMultiple, selectionCount = GroupSelection(group).GetSelection().Length, items = items.Select(item => new { item.Value, selected = item.IsChecked, item.IsLoaded, item.IsEnabled }).ToArray() }); }
        async Task Reject(string id, BleeckerToggleGroup group, BleeckerToggleGroupItem item, Action action, string expected)
        {
            var value = group.SelectedValue; var values = group.SelectedValues.ToArray(); var selected = item.IsChecked; var events = 0; EventHandler listener = (_, _) => events++; group.SelectionChanged += listener;
            var expectedHResult = expected == nameof(ElementNotAvailableException) ? unchecked((int)0x80040201) : unchecked((int)0x80040200);
            string? exception = null; int? exceptionHResult = null; try { action(); } catch (Exception e) { exception = e.GetType().Name; exceptionHResult = e.HResult; } finally { group.SelectionChanged -= listener; }
            await Layout(); var unchanged = group.SelectedValue == value && group.SelectedValues.SequenceEqual(values) && item.IsChecked == selected;
            observations.Add(new { id, expectedException = expected, expectedHResult, exception, exceptionHResult, stateUnchanged = unchanged, events, groupLoaded = group.IsLoaded, itemLoaded = item.IsLoaded, itemEnabled = item.IsEnabled });
            Require((exception == expected || exceptionHResult == expectedHResult) && unchanged && events == 0, $"Rejected provider action failed: {id}; exception={exception ?? "none"}, hresult={exceptionHResult}, expected={expectedHResult}, unchanged={unchanged}, events={events}.");
        }
        try
        {
            await Layout();
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                await Case(theme + "/single-binding", async () =>
                {
                    var (group, a, b) = Make(); var source = new TextBlock { Text = "a" }; var binding = Bind(source, "Text"); group.SetBinding(BleeckerToggleGroup.SelectedValueProperty, binding); await Mount(group); Selection(b).Select(); await Layout(); Require(source.Text == "b" && b.IsChecked == true && a.IsChecked == false, "Single selection did not reach caller binding."); Toggle(b).Toggle(); await Layout(); Require(string.IsNullOrEmpty(source.Text) && GroupSelection(group).GetSelection().Length == 0, "Selected toggle did not clear optional selection."); source.Text = "missing"; await Layout(); Require(group.SelectedValue == "missing" && GroupSelection(group).GetSelection().Length == 0 && ReferenceEquals(group.GetBindingExpression(BleeckerToggleGroup.SelectedValueProperty)?.ParentBinding, binding), "Unknown caller selection or original binding was lost."); Snapshot(theme + "/single-binding", group, a, b);
                });
                await Case(theme + "/multiple-binding", async () =>
                {
                    var (group, a, b) = Make(BleeckerSelectionMode.Multiple); var original = new ObservableCollection<string> { "a" }; var producer = new Border { Tag = original }; var binding = Bind(producer, "Tag"); group.SetBinding(BleeckerToggleGroup.SelectedValuesProperty, binding); await Mount(group); Selection(b).AddToSelection(); Require(ReferenceEquals(group.SelectedValues, original) && original.SequenceEqual(new[] { "a", "b" }), "Multiple selection replaced caller collection."); Selection(a).RemoveFromSelection(); Require(original.SequenceEqual(new[] { "b" }), "Removing one value changed another."); Selection(a).Select(); Require(original.SequenceEqual(new[] { "a" }), "Select did not replace multiple selection."); var replacement = new ObservableCollection<string> { "b" }; producer.Tag = replacement; await Layout(); var events = 0; group.SelectionChanged += (_, _) => events++; original.Add("b"); Require(events == 0 && a.IsChecked == false && b.IsChecked == true, "Replaced collection remained observed."); replacement.Add("a"); Require(events == 1 && a.IsChecked == true && ReferenceEquals(group.GetBindingExpression(BleeckerToggleGroup.SelectedValuesProperty)?.ParentBinding, binding), "Current collection change did not reach the group."); Snapshot(theme + "/multiple-binding", group, a, b);
                });
                await Case(theme + "/multiple-select-single-notification", async () =>
                {
                    var (group, a, b) = Make(BleeckerSelectionMode.Multiple);
                    var original = group.SelectedValues;
                    original.Add("a"); original.Add("b");
                    await Mount(group);
                    var events = 0; group.SelectionChanged += (_, _) => events++;
                    Selection(b).Select(); await Layout();
                    Require(events == 1 && ReferenceEquals(group.SelectedValues, original) &&
                        original.SequenceEqual(new[] { "b" }) && a.IsChecked == false && b.IsChecked == true,
                        $"One exclusive selection raised {events} notifications or replaced caller state.");
                });
                await Case(theme + "/inactive-model", async () => { var (group, a, b) = Make(); group.SelectedValues.Add("b"); await Mount(group); group.SelectionMode = BleeckerSelectionMode.Multiple; Require(a.IsChecked == false && b.IsChecked == true && group.SelectedValue == "a", "Changing mode erased inactive single selection."); group.SelectionMode = BleeckerSelectionMode.Single; Require(a.IsChecked == true && b.IsChecked == false && group.SelectedValues.SequenceEqual(new[] { "b" }), "Changing mode erased inactive multiple selection."); Snapshot(theme + "/inactive-model", group, a, b); });
                foreach (var mode in new[] { BleeckerSelectionMode.Single, BleeckerSelectionMode.Multiple })
                foreach (var lifecycle in new[] { "removed", "unloaded" })
                foreach (var operation in new[] { "Select", "AddToSelection", "RemoveFromSelection", "Toggle" })
                {
                    var id = $"{theme}/{mode}/{lifecycle}/{operation}";
                    await Case(id, async () =>
                    {
                        var (group, a, b) = Make(mode); group.SelectedValues.Add("a"); await Mount(group); var selection = Selection(b); var toggle = Toggle(b); Action action = operation switch { "Select" => selection.Select, "AddToSelection" => selection.AddToSelection, "RemoveFromSelection" => selection.RemoveFromSelection, _ => toggle.Toggle };
                        if (lifecycle == "removed") group.Items.Remove(b); else root.Children.Remove(group); await Layout(); Require(!b.IsLoaded, "Retired toggle remained mounted."); await Reject(id, group, b, action, nameof(ElementNotAvailableException));
                    });
                }
                foreach (var kind in new[] { "item", "group" }) await Case(theme + "/disabled-" + kind, async () =>
                {
                    var (group, a, b) = Make(BleeckerSelectionMode.Multiple); group.SelectedValues.Add("a"); await Mount(group); if (kind == "item") b.IsEnabled = false; else group.IsEnabled = false; await Layout(); var selection = Selection(b); var toggle = Toggle(b);
                    foreach (var (name, action) in new (string, Action)[] { ("Select", selection.Select), ("AddToSelection", selection.AddToSelection), ("RemoveFromSelection", selection.RemoveFromSelection), ("Toggle", toggle.Toggle) }) await Reject(theme + "/disabled-" + kind + "/" + name, group, b, action, nameof(ElementNotEnabledException));
                });
                await Case(theme + "/reattach", async () =>
                {
                    var (group, a, b) = Make(BleeckerSelectionMode.Multiple); group.SelectedValues.Add("a"); await Mount(group); var selection = Selection(b); root.Children.Remove(group); await Layout(); group.SelectedValues.Clear(); group.SelectedValues.Add("b"); root.Children.Add(group); await Layout(); Require(b.IsChecked == true && a.IsChecked == false, "Remount did not reconcile caller state."); selection.Select(); Require(group.SelectedValues.SequenceEqual(new[] { "b" }), "Remounted selection handle no longer works."); group.Items.Remove(b); await Layout(); var next = new BleeckerToggleGroup { SelectedValue = "unknown" }; next.Items.Add(b); root.Children.Add(next); await Layout(); selection.Select(); Require(next.SelectedValue == "b" && group.SelectedValues.SequenceEqual(new[] { "b" }), "Reparented handle changed its old group."); Snapshot(theme + "/reattach", next, b);
                });
                await Case(theme + "/styles", async () =>
                {
                    var matrix = new StackPanel { Width = 600, Spacing = 12 }; root.Children.Add(matrix);
                    foreach (var size in Enum.GetValues<BleeckerControlSize>()) foreach (var variant in Enum.GetValues<BleeckerToggleVariant>()) { var (group, a, b) = Make(); group.Size = size; group.Variant = variant; matrix.Children.Add(group); await Layout(); Require(a.CornerRadius == new CornerRadius(8) && a.MinHeight == (size == BleeckerControlSize.Small ? 32 : size == BleeckerControlSize.Large ? 40 : 36) && a.BorderThickness == new Thickness(variant == BleeckerToggleVariant.Outline ? 1 : 0), "Group size, variant or radius was not applied."); observations.Add(new { id = $"{theme}/style/{size}/{variant}", size = size.ToString(), variant = variant.ToString(), height = a.ActualHeight, a.MinHeight, a.FontSize, radius = a.CornerRadius.TopLeft, border = a.BorderThickness.Left, font = a.FontFamily.Source, width = a.ActualWidth }); }
                });
                await Case(theme + "/empty", async () => { var group = new BleeckerToggleGroup(); await Mount(group); Require(GroupSelection(group).GetSelection().Length == 0 && !GroupSelection(group).IsSelectionRequired, "Empty group invented a selection."); Snapshot(theme + "/empty", group); });
                foreach (var key in new[] { "BleeckerSmallToggleStyle", "BleeckerToggleStyle", "BleeckerLargeToggleStyle", "BleeckerSmallOutlineToggleStyle", "BleeckerOutlineToggleStyle", "BleeckerLargeOutlineToggleStyle" })
                await Case(theme + "/standalone/" + key, async () =>
                {
                    var state = new Border { Tag = false }; var enabled = new Border { Tag = true };
                    var parameter = new object(); var command = new ToggleCommand(parameter);
                    var commandSource = new Border { Tag = command }; var parameterSource = new Border { Tag = parameter };
                    var toggle = new ToggleButton { Content = "Follow fictional updates", Style = (Style)Application.Current.Resources[key] };
                    var checkedBinding = Bind(state, "Tag");
                    var commandBinding = new Binding { Source = commandSource, Path = new PropertyPath("Tag") };
                    var parameterBinding = new Binding { Source = parameterSource, Path = new PropertyPath("Tag") };
                    var enabledBinding = new Binding { Source = enabled, Path = new PropertyPath("Tag") };
                    toggle.SetBinding(ToggleButton.IsCheckedProperty, checkedBinding); toggle.SetBinding(ButtonBase.CommandProperty, commandBinding);
                    toggle.SetBinding(ButtonBase.CommandParameterProperty, parameterBinding); toggle.SetBinding(Control.IsEnabledProperty, enabledBinding);
                    root.Children.Add(toggle); await Layout(); var events = 0; toggle.Checked += (_, _) => events++; toggle.Unchecked += (_, _) => events++;
                    var peer = FrameworkElementAutomationPeer.CreatePeerForElement(toggle); var provider = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);
                    provider.Toggle(); await Layout(); Require(toggle.IsChecked == true && Equals(state.Tag, true) && provider.ToggleState == ToggleState.On && events == 1, "Standalone Toggle did not update its caller's two-way binding.");
                    provider.Toggle(); await Layout(); Require(toggle.IsChecked == false && Equals(state.Tag, false) && events == 2, "Standalone Toggle did not clear its caller's two-way binding.");
                    state.Tag = true; await Layout(); Require(toggle.IsChecked == true && provider.ToggleState == ToggleState.On && events == 3, "Caller selection did not update the native Toggle.");
                    state.Tag = false; await Layout(); Require(toggle.IsChecked == false && events == 4, "Caller selection reset did not reach the native Toggle.");
                    command.Allow(false); await Layout(); var canExecuteDisabled = !toggle.IsEnabled && Equals(enabled.Tag, true); Require(canExecuteDisabled, "CanExecute did not disable the Toggle while preserving caller enabled state.");
                    int? disabledHResult = null; string? disabledError = null; try { provider.Toggle(); } catch (Exception error) { disabledHResult = error.HResult; disabledError = error.GetType().Name; }
                    Require((disabledHResult == unchecked((int)0x80040200) || disabledError == nameof(ElementNotEnabledException)) && toggle.IsChecked == false && Equals(state.Tag, false) && events == 4, "Disabled Toggle automation did not reject selection without changing caller state.");
                    command.Allow(true); await Layout(); Require(toggle.IsEnabled, "CanExecute recovery did not enable the Toggle.");
                    enabled.Tag = false; await Layout(); command.Allow(false); command.Allow(true); await Layout(); var callerDisablePreserved = !toggle.IsEnabled && Equals(enabled.Tag, false); Require(callerDisablePreserved, "Command changes overrode caller-disabled state.");
                    enabled.Tag = true; await Layout(); Require(toggle.IsEnabled, "Caller-enabled binding did not recover.");
                    var replacementParameter = new object(); var replacement = new ToggleCommand(replacementParameter);
                    commandSource.Tag = replacement; await Layout(); var replacementParameterValidated = !toggle.IsEnabled; Require(replacementParameterValidated, "Replacement command did not validate the existing parameter.");
                    parameterSource.Tag = replacementParameter; await Layout(); command.Allow(false); await Layout();
                    var oldCommandDetached = toggle.IsEnabled && ReferenceEquals(toggle.Command, replacement) && ReferenceEquals(toggle.CommandParameter, replacementParameter); Require(oldCommandDetached, "Live command/parameter bindings or old-command detachment failed.");
                    provider.Toggle(); await Layout(); provider.Toggle(); await Layout();
                    var checkedBindingRetained = ReferenceEquals(toggle.GetBindingExpression(ToggleButton.IsCheckedProperty)?.ParentBinding, checkedBinding);
                    var commandBindingRetained = ReferenceEquals(toggle.GetBindingExpression(ButtonBase.CommandProperty)?.ParentBinding, commandBinding);
                    var parameterBindingRetained = ReferenceEquals(toggle.GetBindingExpression(ButtonBase.CommandParameterProperty)?.ParentBinding, parameterBinding);
                    var enabledBindingRetained = ReferenceEquals(toggle.GetBindingExpression(Control.IsEnabledProperty)?.ParentBinding, enabledBinding);
                    Require(toggle.IsChecked == false && Equals(state.Tag, false) && events == 6 && checkedBindingRetained && commandBindingRetained && parameterBindingRetained && enabledBindingRetained, "Standalone Toggle replaced a caller binding or duplicated selection events.");
                    // Toggle promises state changes. Command execution is recorded
                    // separately and does not establish physical activation.
                    Require((command.Executions is 0 or 2) && (replacement.Executions is 0 or 2), "Native Toggle automation partially executed a command.");
                    standaloneToggles.Add(new { id = theme + "/standalone/" + key, checkedBindingRetained, commandBindingRetained, parameterBindingRetained, enabledBindingRetained, canExecuteDisabled, callerDisablePreserved, replacementParameterValidated, oldCommandDetached, selected = toggle.IsChecked, callerSelected = state.Tag, callerEnabled = enabled.Tag, toggle.IsEnabled, events, disabledHResult, disabledError, commandExecutions = command.Executions, replacementCommandExecutions = replacement.Executions });
                });
                if (catalogFactory is not null) await Case(theme + "/catalog", async () => { var fixture = catalogFactory(); root.Children.Add(fixture); await Layout(); var b = (BleeckerToggleGroupItem)fixture.FindName("GridItem"); Selection(b).Select(); await Layout(); Require(((TextBox)fixture.FindName("SelectionValueInput")).Text == "grid" && ((TextBlock)fixture.FindName("SelectionResult")).Text.Contains("grid", StringComparison.Ordinal), "Catalog did not display caller selection."); });
            }
        }
        catch (Exception error) { failures.Add(new { id = "fixture", error = error.ToString() }); }
        finally
        {
            root.Children.Clear(); await Layout(); Guard(); var passed = failures.Count == 0;
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-toggle-group-native-v1", passed, checks, failures, observations, standaloneToggles, guards, focus, activations, hostVisible = window.Visible || window.AppWindow.IsVisible || IsWindowVisible(handle), activateCalled = false, focusCalled = false, hardwareInputSent = false, enabledButtonInvoked = false, catalogFixtureVerified = catalogFactory is not null, sourceSha256 = "461435efe96948a3cbb453bcbf9674b3c4fd107a34e67613efe95912d15da755", librarySha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerToggleGroup).Assembly.Location))), hostSha256 = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostAssembly))), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(), physicalInputVerified = false, actualDisplayOrTextScalingVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close(); Environment.Exit(passed ? 0 : 1);
        }
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
}
