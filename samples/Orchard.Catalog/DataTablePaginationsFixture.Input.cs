using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class DataTablePaginationsFixture
{
    internal async Task VerifyPhysicalAsync(BleeckerColorScheme scheme, string folder)
    {
        using var input = new NativeInputSession(XamlRoot, folder, $"pagination-input-{scheme}"); await input.AcquireAsync(Outside);
        table.SetFilter(false); table.SetSort(false); table.SetPagination(0, 10); table.SetSelection([]); await MainWindow.AwaitPresentationAsync(Preview);
        var buttons = Descendants<Button>(Preview).Where(item => item.Name.StartsWith("PART_", StringComparison.Ordinal)).ToDictionary(item => item.Name); var combo = Chooser(Preview);
        await input.ClickAsync(buttons["PART_Next"]); await ReadyAsync(() => table.PageIndex == 1 && table.VisibleRows[0] == table.Rows[10]);
        await input.ClickAsync(buttons["PART_Last"]); await ReadyAsync(() => table.PageIndex == 4 && !buttons["PART_Last"].IsEnabled);
        await input.ClickAsync(buttons["PART_First"]); await ReadyAsync(() => table.PageIndex == 0);
        table.SetPagination(2, 10); await MainWindow.AwaitPresentationAsync(Preview); await input.ClickAsync(combo); await ReadyAsync(() => ChooserIsOpen(combo));
        Button? option = null;
        await ReadyAsync(() => { option = combo.Items.OfType<Button>().SingleOrDefault(item => item.Tag is int value && value == 20); return option is { IsLoaded: true }; });
        await input.ClickAsync(option!); await ReadyAsync(() => table.PageSize == 20 && table.PageIndex == 1 && !ChooserIsOpen(combo));
        Require(ReferenceEquals(table.VisibleRows[0], table.Rows[20]) && Rows.Text == table.VisibleText, "Physical page-size selection lost original consumer rows.");
        await input.ClickAsync(combo); await ReadyAsync(() => ChooserIsOpen(combo)); await MainWindow.AwaitPresentationAsync(Preview);
        var focused = FocusManager.GetFocusedElement(XamlRoot) as Control ?? throw new InvalidOperationException("Open native selector has no focused control.");
        var popupRoot = (UIElement)focused;
        var downs = 0; var ups = 0; var downOwned = true; var upOwned = true; var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var down = new KeyEventHandler((_, args) => { if (args.Key != Windows.System.VirtualKey.Escape) return; downs++; downOwned &= ReferenceEquals(args.OriginalSource, focused); });
        var up = new KeyEventHandler((_, args) => { if (args.Key != Windows.System.VirtualKey.Escape) return; ups++; upOwned &= ReferenceEquals(args.OriginalSource, combo); released.TrySetResult(); });
        popupRoot.AddHandler(UIElement.KeyDownEvent, down, true); XamlRoot.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            input.Key(0x1B); await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(downs == 1 && ups == 1 && downOwned && upOwned && !ChooserIsOpen(combo) && ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), combo), "Native selector Escape lost its exact focus transfer.");
        }
        finally
        {
            popupRoot.RemoveHandler(UIElement.KeyDownEvent, down); XamlRoot.Content.RemoveHandler(UIElement.KeyUpEvent, up);
            await File.WriteAllTextAsync(Path.Combine(folder, $"pagination-escape-{scheme}.json"), JsonSerializer.Serialize(new { downs, ups, downOwned, upOwned, from = focused.Name, to = combo.Name, closed = !ChooserIsOpen(combo) }));
        }
        await File.WriteAllTextAsync(Path.Combine(folder, $"pagination-input-{scheme}.json"), JsonSerializer.Serialize(new { passed = true, table.PageIndex, table.PageSize, table.PageCount, originalRows = table.VisibleRows.All(row => table.Rows.Any(original => ReferenceEquals(original, row))) }));
    }
}
