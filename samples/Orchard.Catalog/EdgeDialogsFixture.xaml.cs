using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Text.Json;
namespace Orchard.Catalog;

public sealed partial class EdgeDialogsFixture:StackPanel
{
    private int requests;private bool drawerExpanded,sheetExpanded;
    public EdgeDialogsFixture(){InitializeComponent();SheetSelect.Options=[new("a","Fictional Alpha"),new("b","Fictional Beta")];Unloaded+=(_,_)=>{Nested.IsOpen=Drawer.IsOpen=Sheet.IsOpen=LeftSheet.IsOpen=TopSheet.IsOpen=BottomSheet.IsOpen=OwnScroll.IsOpen=false;};}
    private void OpenDrawer(object sender,RoutedEventArgs args)=>Drawer.IsOpen=true;
    private void OpenNested(object sender,RoutedEventArgs args)=>Nested.IsOpen=true;
    private void OpenSheet(object sender,RoutedEventArgs args){var side=Enum.Parse<BleeckerSheetSide>((string)((FrameworkElement)sender).Tag);(side switch{BleeckerSheetSide.Left=>LeftSheet,BleeckerSheetSide.Top=>TopSheet,BleeckerSheetSide.Bottom=>BottomSheet,_=>Sheet}).IsOpen=true;}
    private void OpenOwnScroll(object sender,RoutedEventArgs args)=>OwnScroll.IsOpen=true;
    private void ResizeDrawer(object sender,RoutedEventArgs args){drawerExpanded=!drawerExpanded;DrawerDetail.Text=drawerExpanded?string.Join('\n',Enumerable.Repeat("Fictional drawer component detail",60)):"Short fictional detail";}
    private void ResizeSheet(object sender,RoutedEventArgs args){sheetExpanded=!sheetExpanded;SheetDetail.Text=sheetExpanded?string.Join('\n',Enumerable.Repeat("Fictional sheet component detail",60)):"Short fictional detail";}
    private void Closed(object sender,RoutedEventArgs args){((BleeckerEdgeDialog)sender).IsOpen=false;Status.Text=$"Close requests: {++requests}";}
    private void ChoiceRequested(object? sender,BleeckerSelectRequestedEventArgs args){SheetSelect.Value=args.Value;Status.Text="Caller accepted fictional choice: "+args.Value;}

    internal async Task VerifyInputAsync(BleeckerColorScheme scheme, string folder)
    {
        static T? Find<T>(DependencyObject node, string name) where T : FrameworkElement
        {
            if (node is T element && element.Name == name && element.IsLoaded) return element;
            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
                if (Find<T>(VisualTreeHelper.GetChild(node, index), name) is { } found) return found;
            return null;
        }
        static async Task AwaitAsync(Func<bool> condition, string message)
        {
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (condition()) return;
                await Task.Delay(20);
            }
            throw new InvalidOperationException(message);
        }
        static bool Within(DependencyObject? node, DependencyObject owner)
        {
            for (; node is not null; node = VisualTreeHelper.GetParent(node))
                if (ReferenceEquals(node, owner)) return true;
            return false;
        }
        static async Task AwaitReducedAsync(Func<FrameworkElement?> surface, string name)
        {
            for (var attempt = 0; attempt < 15; attempt++)
            {
                if (surface()?.RenderTransform is TranslateTransform transform &&
                    Math.Abs(transform.X) < .01 && Math.Abs(transform.Y) < .01) return;
                await Task.Delay(10);
            }
            throw new InvalidOperationException($"Reduced-motion {name} did not reach its open endpoint.");
        }
        using var input = new NativeInputSession(XamlRoot, folder, $"edge-dialog-{scheme}");
        await input.AcquireAsync(OpenDrawerButton);
        var before = requests;
        await input.ClickAsync(OpenDrawerButton);
        await AwaitAsync(() => Drawer.IsOpen && Find<FrameworkElement>(XamlRoot.Content, "DrawerSurface") is not null,
            "Pointer did not open Drawer.");
        var reducedMotion = !new Windows.UI.ViewManagement.UISettings().AnimationsEnabled;
        if (reducedMotion)
            await AwaitReducedAsync(() => Find<FrameworkElement>(XamlRoot.Content, "DrawerSurface"), "Drawer");
        input.Key(0x1B);
        await AwaitAsync(() => !Drawer.IsOpen && requests == before + 1,
            "Escape did not request Drawer closure once.");
        await AwaitAsync(() => Within(FocusManager.GetFocusedElement(XamlRoot) as DependencyObject, OpenDrawerButton),
            "Drawer did not return focus to its opener.");

        await input.ClickAsync(OpenRightSheetButton);
        await AwaitAsync(() => Sheet.IsOpen && Find<FrameworkElement>(XamlRoot.Content, "SheetSurface") is not null,
            "Pointer did not open Sheet.");
        if (reducedMotion)
            await AwaitReducedAsync(() => Find<FrameworkElement>(XamlRoot.Content, "SheetSurface"), "Sheet");
        await AwaitAsync(() => Find<BleeckerDialogCloseButton>(XamlRoot.Content, "SheetClose") is not null,
            "Sheet close action did not mount.");
        var close = Find<BleeckerDialogCloseButton>(XamlRoot.Content, "SheetClose") ??
            throw new InvalidOperationException("Sheet close action did not mount.");
        await input.ClickAsync(close);
        await AwaitAsync(() => !Sheet.IsOpen && requests == before + 2,
            "Pointer close did not reach the Sheet caller once.");
        await AwaitAsync(() => Within(FocusManager.GetFocusedElement(XamlRoot) as DependencyObject, OpenRightSheetButton),
            "Sheet did not return focus to its opener.");
        input.Key(0x20);
        await AwaitAsync(() => Sheet.IsOpen, "Space did not reopen Sheet.");
        input.Key(0x1B);
        await AwaitAsync(() => !Sheet.IsOpen && requests == before + 3,
            "Escape did not request Sheet closure once.");
        await AwaitAsync(() => Within(FocusManager.GetFocusedElement(XamlRoot) as DependencyObject, OpenRightSheetButton),
            "Escape did not restore Sheet opener focus.");
        await File.WriteAllTextAsync(Path.Combine(folder, $"edge-dialog-input-{scheme}.json"), JsonSerializer.Serialize(new
        {
            passed = true, theme = scheme.ToString(), drawerEscapeRequests = 1,
            sheetPointerCloseRequests = 1, sheetEscapeRequests = 1,
            focusRestored = true, reducedMotion, hardwareInputSent = true
        }));
    }
}
