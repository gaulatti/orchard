using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Windows.Foundation;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.UI.Core;

namespace Orchard;

public sealed partial class BleeckerTextarea
{
    private BleeckerDropdownMenu? plainTextMenu;
    private Canvas? contextAnchorHost;
    private Border? contextAnchor;

    private void InitializePlainTextCommands()
    {
        PreviewKeyDown += OnPlainTextPreviewKeyDown;
        var menu = new BleeckerDropdownMenu();
        plainTextMenu = menu;
        Loaded += (_, _) => menu.Hide();
        Unloaded += (_, _) => { if (!IsLoaded) menu.Hide(); };
        var items = new List<(BleeckerMenuContentItem Item, Func<bool> Enabled)>();
        Add("Undo", () => TextDocument.Undo(), () => !IsReadOnly && TextDocument.CanUndo());
        Add("Redo", () => TextDocument.Redo(), () => !IsReadOnly && TextDocument.CanRedo());
        Add("Cut", () => TextDocument.Selection.Cut(), () => !IsReadOnly && TextDocument.Selection.Length > 0);
        Add("Copy", () => TextDocument.Selection.Copy(), () => TextDocument.Selection.Length > 0);
        Add("Paste", async () => await StartPlainTextPasteAsync(), () => !IsReadOnly && Clipboard.GetContent().Contains(StandardDataFormats.Text));
        Add("Select all", () => TextDocument.Selection.SetRange(0, Text.Length), () => Text.Length > 0);
        menu.Opening += (_, _) => { foreach (var (item, enabled) in items) item.IsEnabled = IsEnabled && enabled(); };
        SelectionFlyout = menu;
        ContextRequested += (_, args) =>
        {
            if (args.Handled || !OwnsPlainTextContextMenu) return;
            args.Handled = true;
            ShowPlainTextContextMenu(args.TryGetPosition(this, out var position) ? position : null);
        };

        void Add(string text, Action action, Func<bool> enabled)
        {
            var item = new BleeckerMenuContentItem { Text = text, Content = new TextBlock { Text = text } };
            item.Click += (_, _) => { if (IsEnabled && enabled()) action(); };
            menu.Items.Add(item); items.Add((item, enabled));
        }
    }

    // The standalone style leaves ContextFlyout unset so WinUI does not
    // automatically open a windowed text flyout before ContextRequested.
    // A local null disables our menu; a supplied flyout keeps normal WinUI ownership.
    private bool OwnsPlainTextContextMenu => plainTextMenu is not null && ContextFlyout is null &&
        ReadLocalValue(ContextFlyoutProperty) == DependencyProperty.UnsetValue;

    private void ShowPlainTextContextMenu(Point? position = null)
    {
        if (plainTextMenu is null || plainTextMenu.IsOpen || !IsEnabled) return;
        if (position is null)
        {
            // WinUI anchors keyboard context menus at the trailing selection
            // edge in LTR and the leading edge in RTL, in text-view coordinates.
            var rtl = FlowDirection == FlowDirection.RightToLeft;
            var offset = rtl ? TextDocument.Selection.StartPosition : TextDocument.Selection.EndPosition;
            TextDocument.GetRange(offset, offset).GetRect(PointOptions.ClientCoordinates, out var caret, out _);
            var viewport = GetTemplateChild("ContentElement") as FrameworkElement ?? throw new InvalidOperationException("Textarea context menus require ContentElement.");
            position = viewport.TransformToVisual(this).TransformPoint(new Point(rtl ? caret.Left : caret.Right, caret.Bottom));
        }
        if (resizeHost is null) throw new InvalidOperationException("Textarea context menus require Editor.");
        if (contextAnchorHost is null || !ReferenceEquals(VisualTreeHelper.GetParent(contextAnchorHost), resizeHost))
        {
            contextAnchorHost = new Canvas { IsHitTestVisible = false, Opacity = 0 };
            contextAnchor = new Border { Width = 1, Height = 1 };
            AutomationProperties.SetAccessibilityView(contextAnchorHost, AccessibilityView.Raw);
            contextAnchorHost.Children.Add(contextAnchor);
            resizeHost.Children.Add(contextAnchorHost);
        }
        var point = TransformToVisual(resizeHost).TransformPoint(position.Value);
        Canvas.SetLeft(contextAnchor, point.X);
        Canvas.SetTop(contextAnchor, point.Y - 1);
        contextAnchorHost.UpdateLayout();
        // In pinned WinUI 2.3.9, positioned overloads create an external HWND.
        // A layout anchor lets
        // the constrained overload retain the exact request/caret position.
        plainTextMenu.Placement = FlyoutPlacementMode.BottomEdgeAlignedLeft;
        plainTextMenu.ShowAt(contextAnchor);
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
