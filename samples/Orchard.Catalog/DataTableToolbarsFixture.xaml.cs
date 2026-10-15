using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture:UserControl
{
    private readonly ToolbarFilterTable table=new();
    private ToolbarFilterTable activeTable;
    public DataTableToolbarsFixture(){InitializeComponent();activeTable=table;Preview.Table=table;table.Changed+=(_,_)=>{if(ReferenceEquals(activeTable,table))Results.Text=table.VisibleText;};Results.Text=table.VisibleText;}
    private void FilterClicked(object sender,RoutedEventArgs args)=>table.GetColumn("status")!.SetFilterValue(table.GetColumn("status")!.GetFilterValue() is null?"Draft":"");
    private void ActionClicked(object sender,RoutedEventArgs args)=>Results.Text="Consumer action invoked; "+table.VisibleText;
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        using var input=new NativeInputSession(XamlRoot,folder,$"data-table-toolbar-{scheme}");await input.AcquireAsync(Outside);
        await VerifySourceAsync(scheme,folder);
        Preview.ApplyTemplate();UpdateLayout();await MainWindow.AwaitPresentationAsync(Preview);
        var search=Descendants<BleeckerInput>(Preview).Single();
        Require(search.ActualHeight==36&&search.MinHeight==36&&search.FontSize==14&&search.Padding.Left==14,"Toolbar's medium Input height override did not reach native layout.");
        table.SetFilters([]);table.SetGlobal(null);Require(!Descendants<Button>(Preview).Any(button=>button.Name=="PART_Reset"),"Unfiltered table shows Reset.");
        await FocusAsync(search);await input.KeyAsync(0x48,search); // A real H key filters the original rows.
        await AwaitAsync(()=>search.Text.Equals("h",StringComparison.OrdinalIgnoreCase)&&table.VisibleText=="Harbor"&&Results.Text=="Harbor","Typed search did not reach the real consumer view.");
        var reset=Descendants<Button>(Preview).Single(button=>button.Name=="PART_Reset");await MainWindow.AwaitPresentationAsync(Preview);
        Require(table.Writes.Count>0&&ReferenceEquals(table.Writes[^1].Column,table.GetColumn("title"))&&ReferenceEquals(Preview.Table,table),"Search replaced the original table/column.");
        try{await input.ClickAsync(reset);}
        finally{await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-reset-{scheme}.json"),JsonSerializer.Serialize(new{table.Resets,reset.IsLoaded,reset.IsPressed,table.VisibleText,search.Text}));}
        await AwaitAsync(()=>search.Text==""&&table.VisibleRows.Count==3,"Reset did not restore the consumer's unfiltered view.");
        table.SetInitial(new[]{new KeyValuePair<string,object?>("title","Harbor")});table.GetColumn("title")!.SetFilterValue("Reed");table.SetGlobal("Harbor");
        await MainWindow.AwaitPresentationAsync(Preview);
        await input.ClickAsync(reset);await AwaitAsync(()=>search.Text=="Harbor"&&table.VisibleText=="Harbor","Reset did not restore initial column filters.");Require(Equals(table.GlobalFilter,"Harbor")&&reset.Visibility==Visibility.Visible,"Reset cleared global state or hid an active reset.");
        var before=table.Resets;await FocusAsync(reset);await input.KeyAsync(0x0D,reset);Require(table.Resets==before+1&&Equals(table.GlobalFilter,"Harbor"),"Keyboard reset did not use the consumer table.");
        Preview.SearchColumn="absent";await MainWindow.AwaitPresentationAsync(Preview);var writes=table.Writes.Count;await FocusAsync(search);await input.KeyAsync(0x52,search);await AwaitAsync(()=>search.Text==""&&table.Writes.Count==writes,"A missing column accepted a controlled edit.");
        Preview.SearchColumn="title";table.SetInitial([]);table.SetFilters([]);table.SetGlobal(null);await MainWindow.AwaitPresentationAsync(Preview);
        var peer=FrameworkElementAutomationPeer.CreatePeerForElement(Preview);Require(peer.GetClassName()==nameof(BleeckerDataTableToolbar),"Toolbar UI Automation identity differs.");
        await MainWindow.CaptureElementAsync(Preview,Path.Combine(folder,$"data-table-toolbar-{scheme}.png"));
        await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,search.ActualHeight,search.ActualWidth,table.Resets,writes=table.Writes.Count,table.VisibleText,originalRows=table.VisibleRows.All(row=>ToolbarFilterTable.OriginalRows.Any(original=>ReferenceEquals(original,row)))}));
        await VerifyInputHeightAsync();
        await VerifyLifecycleAsync(scheme,folder);
        await VerifyActionsAsync(input,scheme,folder);
    }
    private async Task VerifyInputHeightAsync()
    {
        var item=new BleeckerInput{Height=36,Width=200};Stage.Children.Add(item);
        try
        {
            await AwaitAsync(()=>item.IsLoaded,"Height override Input did not load.");item.ApplyTemplate();await MainWindow.AwaitPresentationAsync(item);
            Require(item.ActualHeight==36&&item.MinHeight==36&&item.FontSize==14&&item.Padding.Left==14,"Explicit Input height changed medium typography or failed layout.");
            item.ClearValue(HeightProperty);await MainWindow.AwaitPresentationAsync(item);Require(item.ActualHeight==40&&item.MinHeight==40,"Clearing Input height did not restore its medium recipe.");
            item.InputSize=BleeckerInputSize.Large;item.Height=36;await MainWindow.AwaitPresentationAsync(item);Require(item.ActualHeight==36&&item.FontSize==15&&item.Padding.Left==16,"Large Input did not preserve its typography with an explicit height.");
            item.ClearValue(HeightProperty);await MainWindow.AwaitPresentationAsync(item);Require(item.ActualHeight==44&&item.MinHeight==44,"Clearing Input height did not restore its large recipe.");
            item.FontSize=17;item.Padding=new Thickness(7,0,7,0);item.Height=40;await MainWindow.AwaitPresentationAsync(item);Require(item.ActualHeight==40&&item.FontSize==17&&item.Padding.Left==7,"Changing only Input height overwrote consumer typography.");
        }finally{Stage.Children.Remove(item);}
    }
    private static async Task FocusAsync(Control element)
    {
        if(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(element.XamlRoot),element))return;
        var focused=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);void Got(object sender,RoutedEventArgs args){if(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(element.XamlRoot),element))focused.TrySetResult();}
        element.GotFocus+=Got;try{Require(element.Focus(FocusState.Keyboard),"Toolbar target refused focus.");await focused.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{element.GotFocus-=Got;}
    }
    private async Task AwaitAsync(Func<bool> condition,string message){var watch=Stopwatch.StartNew();var readiness = new NativeReadiness(DispatcherQueue); while(!condition()&&watch.Elapsed<TimeSpan.FromSeconds(2)){UpdateLayout();if(condition())break;await readiness.WaitAsync();}Require(condition(),message);}
    private static IEnumerable<T> Descendants<T>(DependencyObject owner)where T:DependencyObject{for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){var child=VisualTreeHelper.GetChild(owner,i);if(child is T value)yield return value;foreach(var nested in Descendants<T>(child))yield return nested;}}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
