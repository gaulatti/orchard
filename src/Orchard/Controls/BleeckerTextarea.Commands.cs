using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

namespace Orchard;

public sealed partial class BleeckerTextarea
{
    private void InitializePlainTextCommands()
    {
        PreviewKeyDown += OnPlainTextPreviewKeyDown;
        var menu = new BleeckerDropdownMenu();
        var items = new List<(BleeckerMenuContentItem Item, Func<bool> Enabled)>();
        Add("Undo", () => TextDocument.Undo(), () => !IsReadOnly && TextDocument.CanUndo());
        Add("Redo", () => TextDocument.Redo(), () => !IsReadOnly && TextDocument.CanRedo());
        Add("Cut", () => TextDocument.Selection.Cut(), () => !IsReadOnly && TextDocument.Selection.Length > 0);
        Add("Copy", () => TextDocument.Selection.Copy(), () => TextDocument.Selection.Length > 0);
        Add("Paste", async () => await StartPlainTextPasteAsync(), () => !IsReadOnly && Clipboard.GetContent().Contains(StandardDataFormats.Text));
        Add("Select all", () => TextDocument.Selection.SetRange(0, Text.Length), () => Text.Length > 0);
        menu.Opening += (_, _) => { foreach (var (item, enabled) in items) item.IsEnabled = IsEnabled && enabled(); };
        ContextFlyout = SelectionFlyout = menu;

        void Add(string text, Action action, Func<bool> enabled)
        {
            var item = new BleeckerMenuContentItem { Text = text, Content = new TextBlock { Text = text } };
            item.Click += (_, _) => { if (IsEnabled && enabled()) action(); };
            menu.Items.Add(item); items.Add((item, enabled));
        }
    }

    private void OnPlainTextPreviewKeyDown(object sender, KeyRoutedEventArgs args)
    {
        var control = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) != 0;
        var alt = (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu) & CoreVirtualKeyStates.Down) != 0;
        if (control && !alt && args.Key is VirtualKey.B or VirtualKey.I or VirtualKey.U or VirtualKey.E or VirtualKey.J or VirtualKey.L or VirtualKey.R or VirtualKey.M or VirtualKey.T or VirtualKey.Number1 or VirtualKey.Number2 or VirtualKey.Number5 or (VirtualKey)0xBB or (VirtualKey)0xBC or (VirtualKey)0xBE)
        {
            args.Handled = true;
        }
    }
}
