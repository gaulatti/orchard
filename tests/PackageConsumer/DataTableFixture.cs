using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

// The source catalog and isolated NuGet consumer compile this same public-API fixture.
internal static partial class DataTableFixture
{
    private delegate bool Visitor(nint handle,nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement element)yield return element;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static object Box(FrameworkElement e,UIElement root){var b=e.TransformToVisual(root).TransformBounds(new Rect(0,0,e.ActualWidth,e.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
    private static string Hash(string path)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Invoke(FrameworkElement button){var action=button as Button??Elements(button).OfType<Button>().Single();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(action);Require(peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider,"Native button has no Invoke provider.");((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke();}
    private static void Toggle(BleeckerCheckbox checkbox)=>((IToggleProvider)FrameworkElementAutomationPeer.CreatePeerForElement(checkbox).GetPattern(PatternInterface.Toggle)).Toggle();
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='1000' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,1000));var snapshots=new List<object>();var contracts=new List<object>();var pngs=new List<object>();var guards=0;var focus=0;var activations=0;var stage="initial";
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Data Table left its hidden background boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout.");await idle.Task;root.Measure(new Size(900,1000));root.Arrange(new Rect(0,0,900,1000));root.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var context=new Context();var data=context.Data;root.Children.Add(data);await Layout();var primitive=context.Table;var original=context.Items[0].Action;
                async Task Record(string label){stage=scheme+"-"+label;await Layout();snapshots.Add(new{scheme,stage=label,snapshot=context.Snapshot(root)});await Capture("data-table-"+scheme+"-"+label+".png");}
                await Record("initial");Require(data.Model.PageRows.Count==10&&data.Model.PageCount==3,"Default page contract differs.");
                Toggle(context.Checks["0"]);await Record("selected-first");Require(data.Model.FilteredSelectedRowCount==1,"Row checkbox did not reach model.");
                Invoke(context.Next);await Record("page-two");Require(data.Model.PageRows[0].Id=="10","Next page did not render index ten.");
                Toggle(context.Checks["10"]);await Record("selected-second-page");Require(data.Model.FilteredSelectedRowCount==2,"Selection lost another page.");
                context.Search.Text="Cedar";await Record("filtered");Require(data.Model.PageIndex==0&&data.Model.FilteredRowCount==8&&data.Model.FilteredSelectedRowCount==1,"Native search did not filter/reset/count selection.");
                context.Search.Text="no fictional match";await Record("empty");Require(data.Model.FilteredRowCount==0,"Native empty search did not filter.");
                context.Search.Text="";await Record("restored");Require(data.Model.FilteredSelectedRowCount==2,"Restored filter lost selection.");
                Invoke(context.Headings["Count"]);await Record("count-descending");Require(ReferenceEquals(data.Model.PageRows[0].Original,context.Items[4]),"Original numeric descending sort differs.");
                Invoke(context.Headings["Count"]);await Record("count-ascending");Require(ReferenceEquals(data.Model.PageRows[0].Original,context.Items[0])&&ReferenceEquals(original,context.Status[context.Items[0]]),"Sort replaced original row/content.");
                Invoke(original);await Record("original-action");Require(context.Invoked.SequenceEqual(new[]{"fictional-00"}),"Original action lost row data.");
                context.Data.Model.GetColumn("Status")!.ToggleVisibility(false);await Record("hidden-status");Require(context.Table.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().All(r=>r.Cells.Count==3),"Hidden column remains rendered.");
                context.Data.Model.GetColumn("Status")!.ToggleVisibility(true);await Record("restored-status");Require(original.IsLoaded,"Restored original action is not loaded.");
                var template=data.Template;data.Template=null;await Layout();Require(!original.IsLoaded,"Cleared template retained loaded original content.");data.Template=template;await Record("restored-template");Require(original.IsLoaded&&ReferenceEquals(primitive,context.Table),"Restored template replaced primitive/content.");
                data.Columns.Clear();await Record("no-columns");Require(context.Table.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().All(r=>r.Cells.Count==0&&r.ActualHeight==0),"Empty source columns require zero-height rows.");
                data.ItemsSource=Array.Empty<Item>();await Record("no-columns-no-data");context.SetColumns();await Record("columns-restored-empty");
                data.ItemsSource=context.Items;await Record("data-restored");Require(data.Model.FilteredSelectedRowCount==2,"Data restoration lost retained index selection.");
                data.SearchKey="Missing";context.Search.Text="Cedar";await Record("missing-search");Require(context.Search.Text==""&&data.Model.FilteredRowCount==23,"Missing search key did not retain empty controlled input.");
                root.Children.Clear();await Layout();Require(!original.IsLoaded,"Removed table retained loaded original content.");root.Children.Add(data);await Record("reloaded");Require(original.IsLoaded,"Reload failed to restore original content.");root.Children.Clear();await Layout();
                contracts.Add(new{scheme,passed=true,originalContentRetained=true,templateRetirementAndRestoration=true,unloadAndReload=true});
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-native-v1",passed=true,snapshots,contracts,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,completeSourceGeometryVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-native-v1",passed=false,stage,error=error.ToString(),snapshots,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
    private sealed record Item(string Id,string Name,int Count,string Status,BleeckerButton Action);
    private sealed class Context
    {
        internal readonly BleeckerDataTable Data=new(){Width=660,Margin=new Thickness(20),SearchKey="Name",HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
        internal readonly Dictionary<string,BleeckerButton> Headings=new(StringComparer.Ordinal);
        internal readonly Dictionary<string,BleeckerCheckbox> Checks=new(StringComparer.Ordinal);
        internal readonly Dictionary<Item,BleeckerButton> Status=[];
        internal readonly List<string> Invoked=[];
        internal readonly Item[] Items;
        private BleeckerCheckbox? all;
        internal BleeckerTable Table=>Elements(Data).OfType<BleeckerTable>().Single();
        internal BleeckerInput Search=>Elements(Data).OfType<BleeckerInput>().Single(e=>e.Name=="PART_Search");
        internal Button Next=>Elements(Data).OfType<Button>().Single(e=>e.Name=="PART_Next");
        internal Context(){Items=Enumerable.Range(0,23).Select(i=>{var id="fictional-"+i.ToString("D2");var action=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content=i%2==0?"Queued":"Ready"};action.Click+=(_,_)=>Invoked.Add(id);var item=new Item(id,new[]{"Cedar","Birch","Ash"}[i%3]+" "+(i+1),(i*7)%29,i%2==0?"Queued":"Ready",action);Status.Add(item,action);return item;}).ToArray();SetColumns();Data.ItemsSource=Items;}
        internal void SetColumns(){Data.Columns.Clear();Data.Columns.Add(new(id:"select",headerContentSelector:HeaderSelection,cellContentSelector:Selection));Data.Columns.Add(new(accessorKey:"Name",header:"Fictional item",headerContentSelector:Heading));Data.Columns.Add(new(accessorKey:"Count",header:"Count",headerContentSelector:Heading));Data.Columns.Add(new(accessorKey:"Status",header:"Status",cellContentSelector:cell=>((Item)cell.Row.Original).Action));}
        private object Heading(BleeckerDataTableHeader context){if(!Headings.TryGetValue(context.Column.Id,out var button)){var id=context.Column.Id;button=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content=context.Column.Header};button.Click+=(_,_)=>Data.Model.GetColumn(id)!.ToggleSorting();Headings.Add(id,button);}return button;}
        private object HeaderSelection(BleeckerDataTableHeader context){if(all is null){all=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};AutomationProperties.SetName(all,"Select page");void Change(object sender,RoutedEventArgs args){if(all.IsChecked!=Data.Model.AllPageRowsSelected)Data.Model.ToggleAllPageRowsSelected(all.IsChecked==true);}all.Checked+=Change;all.Unchecked+=Change;}all.IsChecked=context.Table.AllPageRowsSelected;return all;}
        private object Selection(BleeckerDataTableCell context){var id=context.Row.Id;if(!Checks.TryGetValue(id,out var check)){check=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};AutomationProperties.SetName(check,"Select row "+id);void Change(object sender,RoutedEventArgs args){var row=Data.Model.CoreRows.FirstOrDefault(r=>r.Id==id);if(row is not null&&check.IsChecked!=row.IsSelected)row.ToggleSelected(check.IsChecked==true);}check.Checked+=Change;check.Unchecked+=Change;Checks.Add(id,check);}check.IsChecked=context.Row.IsSelected;return check;}
        internal object Snapshot(UIElement root)
        {
            var model=Data.Model;var table=Table;var header=table.Sections.OfType<BleeckerTableHeader>().Single();var body=table.Sections.OfType<BleeckerTableBody>().Single();var rows=body.Children.OfType<BleeckerTableRow>().ToArray();
            Require(rows.Where(r=>r.Tag is BleeckerDataTableRow).Select(r=>((BleeckerDataTableRow)r.Tag).Id).SequenceEqual(model.PageRows.Select(r=>r.Id)),"Rendered body order differs from original contexts.");
            foreach(var row in rows)if(row.Tag is BleeckerDataTableRow context){Require(ReferenceEquals(context.Original,Items[context.Index]),"Native row lost original object.");Require(row.Cells.Count==context.GetVisibleCells().Count,"Native visible cells differ.");foreach(var cell in row.Cells){var original=(BleeckerDataTableCell)cell.Tag;Require(ReferenceEquals(original.Row,context),"Native cell lost row context.");if(original.Column.Id=="Status")Require(ReferenceEquals(cell.Content,((Item)context.Original).Action)&&((BleeckerButton)cell.Content).IsLoaded,"Original button was replaced or unloaded.");else if(original.Column.Id=="select")Require(cell.Content is BleeckerCheckbox check&&check.IsChecked==context.IsSelected,"Native checkbox state differs.");else Require(Equals(cell.Content,original.RenderContent()),"Rendered producer value differs.");}}
            else Require(rows.Length==1&&row.Cells.Single().Content?.ToString()=="No results."&&row.ActualHeight==96&&row.Cells[0].ColumnSpan==Math.Max(1,Data.Columns.Count),"Empty-result contract differs.");
            var previous=Elements(Data).OfType<Button>().Single(b=>b.Name=="PART_Previous");var summary=Elements(Data).OfType<TextBlock>().Single(e=>e.Name=="PART_Summary");Require(previous.IsEnabled==model.CanPreviousPage&&Next.IsEnabled==model.CanNextPage,"Native pagination limits differ.");Require(previous.ActualWidth==32&&previous.ActualHeight==32&&Next.ActualWidth==32&&Next.ActualHeight==32,"Native pagination dimensions differ.");Require(summary.Text==(model.FilteredSelectedRowCount>0?model.FilteredSelectedRowCount+" of "+model.FilteredRowCount+" row(s) selected.":""),"Rendered summary differs.");Require(Search.ActualHeight==40&&Math.Abs(Search.ActualWidth-384)<.01,"Native search dimensions differ.");
            return new{fixture=Box(Data,root),table=Box(table,root),pageIndex=model.PageIndex,pageCount=model.PageCount,filtered=model.FilteredRowCount,selected=model.SelectedRowIds.OrderBy(id=>id,StringComparer.Ordinal).ToArray(),filteredSelected=model.FilteredSelectedRowCount,sorting=model.Sorting.Select(s=>new{id=s.Id,desc=s.Descending}).ToArray(),rows=rows.Select(r=>new{id=(r.Tag as BleeckerDataTableRow)?.Id,box=Box(r,root),cells=r.Cells.Select(c=>new{column=(c.Tag as BleeckerDataTableCell)?.Column.Id,colSpan=c.ColumnSpan,content=c.Content is BleeckerButton b?b.Content:c.Content is BleeckerCheckbox check?(object?)(check.IsChecked==true):c.Content,box=Box(c,root)}).ToArray()}).ToArray(),header=header.Children.OfType<BleeckerTableRow>().SelectMany(r=>r.Cells).Select(c=>new{colSpan=c.ColumnSpan,box=Box(c,root)}).ToArray(),search=new{text=Search.Text,box=Box(Search,root)},summary=summary.Text,canPrevious=previous.IsEnabled,canNext=Next.IsEnabled,invoked=Invoked.ToArray(),originalNativeContentVerified=true};
        }
    }
}
