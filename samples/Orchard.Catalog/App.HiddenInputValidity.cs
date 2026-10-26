using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Orchard;
using Windows.Foundation;

namespace Orchard.Catalog;

public partial class App
{
    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")] private static extern nint GetForegroundWindowInputValidity();
    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")] private static extern bool IsWindowVisibleInputValidity(nint handle);
    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")] private static extern uint GetWindowThreadProcessIdInputValidity(nint handle, out uint process);

    private async void RunHiddenInputValidityAsync(string output)
    {
        var stage = "startup";
        var guards = 0;
        var focusEvents = 0;
        var activationEvents = 0;
        var passed = false;
        string? failure = null;
        nint handle = 0;
        void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        void Guard()
        {
            guards++;
            GetWindowThreadProcessIdInputValidity(GetForegroundWindowInputValidity(), out var foreground);
            Require(!_window!.Visible && !_window.AppWindow.IsVisible && !IsWindowVisibleInputValidity(handle) && foreground != Environment.ProcessId && focusEvents == 0 && activationEvents == 0,
                "Input validity probe became visible or owned foreground input.");
        }
        try
        {
            handle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            Guard();
            var root = new Canvas { Width = 500, Height = 650 };
            root.GotFocus += (_, _) => focusEvents++;
            _window!.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activationEvents++; };
            var fixture = new InputsFixture();
            _window!.Content = root;
            root.Children.Add(fixture);
            for (var i = 0; i < 3; i++)
            {
                root.Measure(new Size(500, 650));
                root.Arrange(new Rect(0, 0, 500, 650));
                root.UpdateLayout();
                Guard();
                await Task.Delay(16);
            }
            Require(fixture.IsLoaded, "Catalog Input fixture was not loaded in the hidden XAML tree.");
            var normal = (BleeckerInput)fixture.FindName("MediumInput");
            var error = (BleeckerInput)fixture.FindName("ErrorInput");
            var normalPeer = FrameworkElementAutomationPeer.CreatePeerForElement(normal);
            var errorPeer = FrameworkElementAutomationPeer.CreatePeerForElement(error);
            void Verify(string id, BleeckerInput input, AutomationPeer peer, bool valid)
            {
                stage = id;
                Guard();
                Require(peer.IsDataValidForForm() == valid, $"{id}: native peer validity differs from {valid}.");
            }
            Verify("normal", normal, normalPeer, true);
            Verify("error", error, errorPeer, false);
            Require(AutomationProperties.GetIsDataValidForForm(error), "Input replaced caller-owned attached validity metadata.");
            Require(normalPeer.GetAutomationControlType() == AutomationControlType.Edit && normalPeer.GetName() == "Search channels",
                "Input lost native edit role or catalog name.");
            normal.Text = "Fictional value";
            normal.Select(0, 9);
            Require(normal.SelectedText == "Fictional", "Input lost native text selection.");
            AutomationProperties.SetIsDataValidForForm(normal, false); Verify("caller-invalid", normal, normalPeer, false);
            normal.ClearValue(AutomationProperties.IsDataValidForFormProperty); Verify("caller-cleared", normal, normalPeer, true);
            error.IsError = false; Verify("error-cleared", error, errorPeer, true);
            error.IsInvalid = true; Verify("explicit-invalid", error, errorPeer, false);
            error.IsError = true; error.IsInvalid = false; Verify("explicit-valid-over-error", error, errorPeer, true);
            error.IsInvalid = null; Verify("error-restored", error, errorPeer, false);
            error.IsError = false; Verify("all-clear", error, errorPeer, true);
            stage = "field-composition";
            root.Children.Clear();
            var fieldInput = new BleeckerInput { Text = "Fictional summary" };
            var field = new BleeckerField { Label = "Summary", Error = "Review the summary.", Content = fieldInput };
            root.Children.Add(field);
            for (var i = 0; i < 3; i++)
            {
                root.Measure(new Size(500, 650)); root.Arrange(new Rect(0, 0, 500, 650)); root.UpdateLayout();
                Guard(); await Task.Delay(16);
            }
            var fieldPeer = FrameworkElementAutomationPeer.CreatePeerForElement(fieldInput);
            Verify("composed-error", fieldInput, fieldPeer, false);
            Require(!AutomationProperties.GetIsDataValidForForm(fieldInput), "Field error did not reach attached validity metadata.");
            fieldInput.IsError = true; Verify("composed-double-error", fieldInput, fieldPeer, false);
            field.Error = null; Verify("child-error-after-composed-clear", fieldInput, fieldPeer, false);
            Require(AutomationProperties.GetIsDataValidForForm(fieldInput), "Field failed to restore the caller-owned validity metadata.");
            fieldInput.IsError = false; Verify("composed-all-clear", fieldInput, fieldPeer, true);
            Guard(); passed = true;
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed, scope = "hidden-input-validity", stage, guards,
                hiddenWindow = true, visible = _window?.Visible, nativeVisible = IsWindowVisibleInputValidity(handle),
                focusEvents, activationEvents, inputSent = false, failure }, new JsonSerializerOptions { WriteIndented = true }));
            _window?.Close();
            Environment.Exit(passed ? 0 : 1);
        }
    }
}
