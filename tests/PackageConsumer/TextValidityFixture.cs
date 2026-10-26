using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

internal static class TextValidityFixture
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));

    internal static async Task RunAsync(Window window, string output, string hostAssembly)
    {
        var cases = new List<string>();
        var stage = "startup";
        var guards = 0;
        var focusEvents = 0;
        var activationEvents = 0;
        var passed = false;
        string? failure = null;
        var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var root = new Canvas { Width = 500, Height = 650 };
        root.GotFocus += (_, _) => focusEvents++;
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activationEvents++; };
        void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        void Guard()
        {
            guards++;
            GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            Require(!window.Visible && !window.AppWindow.IsVisible && !IsWindowVisible(handle) &&
                foreground != Environment.ProcessId && focusEvents == 0 && activationEvents == 0,
                "Package text-validity probe became visible or owned foreground input.");
        }
        async Task Mount(FrameworkElement control)
        {
            root.Children.Clear();
            root.Children.Add(control);
            for (var i = 0; i < 3; i++)
            {
                root.Measure(new Size(500, 650));
                root.Arrange(new Rect(0, 0, 500, 650));
                root.UpdateLayout();
                Guard();
                await Task.Delay(16);
            }
            Require(control.IsLoaded, "Packaged text control did not enter the hidden XAML tree.");
        }
        void Verify(string id, FrameworkElement control, bool valid)
        {
            stage = id;
            Guard();
            var peer = FrameworkElementAutomationPeer.CreatePeerForElement(control);
            Require(peer is not null && peer.GetAutomationControlType() == AutomationControlType.Edit &&
                peer.IsDataValidForForm() == valid, $"{id}: packaged native edit validity differs.");
            cases.Add(id);
        }
        try
        {
            Guard();
            window.Content = root;
            var input = new BleeckerInput { Text = "Fictional local value" };
            await Mount(input);
            Verify("input-default", input, true);
            input.IsError = true;
            Verify("input-error", input, false);
            input.IsInvalid = false;
            Verify("input-override-valid", input, true);
            input.IsInvalid = null;
            Verify("input-override-cleared", input, false);
            input.IsError = false;
            AutomationProperties.SetIsDataValidForForm(input, false);
            Verify("input-caller-invalid", input, false);
            input.ClearValue(AutomationProperties.IsDataValidForFormProperty);
            Verify("input-caller-cleared", input, true);

            var fieldInput = new BleeckerInput { Text = "Fictional summary" };
            var field = new BleeckerField { Label = "Summary", Error = "Review summary", Content = fieldInput };
            await Mount(field);
            Verify("field-input-error", fieldInput, false);
            fieldInput.IsInvalid = false;
            Verify("field-input-error-precedes-override", fieldInput, false);
            field.Error = null;
            Verify("field-input-restored", fieldInput, true);

            var notes = new BleeckerTextarea { Text = "Fictional local notes" };
            await Mount(notes);
            Verify("textarea-default", notes, true);
            notes.IsError = true;
            Verify("textarea-error", notes, false);
            notes.IsInvalid = false;
            Verify("textarea-override-valid", notes, true);
            notes.IsInvalid = null;
            Verify("textarea-override-cleared", notes, false);
            notes.IsError = false;
            root.Children.Clear();
            var notesField = new BleeckerField { Label = "Notes", Error = "Review notes", Content = notes };
            await Mount(notesField);
            Verify("field-textarea-error", notes, false);
            notes.IsInvalid = false;
            Verify("field-textarea-error-precedes-override", notes, false);
            notes.IsInvalid = null;
            notes.IsError = true;
            notesField.Error = null;
            Verify("textarea-error-after-field-clear", notes, false);
            notes.IsInvalid = false;
            Verify("textarea-override-after-field-clear", notes, true);
            Guard();
            passed = true;
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
            {
                format = "orchard-package-text-validity-v1", passed, stage, cases, guards,
                hiddenWindow = true, visible = window.Visible, nativeVisible = IsWindowVisible(handle),
                focusEvents, activationEvents, inputSent = false, failure,
                librarySha256 = Hash(typeof(BleeckerInput).Assembly.Location), hostSha256 = Hash(hostAssembly)
            }, new JsonSerializerOptions { WriteIndented = true }));
            window.Close();
            Environment.Exit(passed ? 0 : 1);
        }
    }
}
