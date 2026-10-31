using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class MenubarsFixture
{
    internal async Task VerifyForegroundAsync(string folder, ElementTheme theme)
    {
        static IEnumerable<FrameworkElement> Elements(DependencyObject parent)
        {
            if (parent is FrameworkElement element) yield return element;
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                foreach (var child in Elements(VisualTreeHelper.GetChild(parent, i))) yield return child;
        }
        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
        async Task AwaitAsync(Func<bool> condition, string message)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(2))
            {
                Default.UpdateLayout();
                await Task.Delay(16);
            }
            Require(condition(), message);
        }
        Button? FindAction()
        {
            var roots = new List<DependencyObject> { XamlRoot.Content };
            roots.AddRange(VisualTreeHelper.GetOpenPopupsForXamlRoot(XamlRoot)
                .Where(popup => popup.Child is not null).Select(popup => (DependencyObject)popup.Child));
            return roots.SelectMany(Elements).OfType<Button>().FirstOrDefault(button =>
                button.IsLoaded && button.DataContext is BleeckerMenubarAction action &&
                action.Label == "New fictional draft");
        }

        await MainWindow.AwaitPresentationAsync(Default);
        using var input = new NativeInputSession(XamlRoot, folder, $"menubar-{theme}");
        await input.AcquireAsync(Result);
        var file = Elements(Default).OfType<Button>().Single(button =>
            button.DataContext is BleeckerMenubarMenu menu && menu.Trigger == "File");
        Require(file.Focus(FocusState.Keyboard), "Menubar File rejected keyboard focus.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), file),
            "Menubar File did not hold keyboard focus.");
        input.Key(0x28);
        await AwaitAsync(() => FindAction() is not null,
            "Keyboard Down did not open Menubar File.");
        await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot), FindAction()),
            "Keyboard Down opened Menubar but did not focus its first action.");
        input.Key(0x0D);
        await AwaitAsync(() => Result.Text == "New fictional draft selected",
            "Keyboard Enter did not invoke the original Menubar action.");

        Result.Text = "No fictional action selected";
        await input.ClickAsync(file);
        await AwaitAsync(() => FindAction() is not null,
            "Pointer click did not open Menubar File.");
        await input.ClickAsync(FindAction()!);
        await AwaitAsync(() => Result.Text == "New fictional draft selected",
            "Pointer click did not invoke the original Menubar action.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"menubar-foreground-{theme}.json"),
            JsonSerializer.Serialize(new { passed = true, theme, keyboardRootFocused = true,
                keyboardActionFocused = true, keyboardActionInvoked = true,
                pointerRootOpened = true, pointerActionInvoked = true }));
    }
}
