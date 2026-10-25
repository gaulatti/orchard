using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static partial class DataTableFixture
{
    // Caller equality is deliberately independent of original object identity.
    private sealed class LiveItem(string id,string name,int count):INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? changed;
        private int count=count;
        public string Id{get;}=id;
        public string Name{get;}=name;
        public int Count{get=>count;set{count=value;changed?.Invoke(this,new(nameof(Count)));}}
        public int Subscribers=>changed?.GetInvocationList().Length??0;
        public event PropertyChangedEventHandler? PropertyChanged{add=>changed+=value;remove=>changed-=value;}
        public override bool Equals(object? other)=>other is LiveItem;
        public override int GetHashCode()=>7;
    }
    private sealed class LiveCollection:ObservableCollection<LiveItem>,INotifyCollectionChanged
    {
        public int Subscribers{get;private set;}
        event NotifyCollectionChangedEventHandler? INotifyCollectionChanged.CollectionChanged
        {add{Subscribers++;CollectionChanged+=value;}remove{Subscribers--;CollectionChanged-=value;}}
    }
    private sealed class FontValues:INotifyPropertyChanged
    {
        private double size=24;
        public double Size{get=>size;set{size=value;PropertyChanged?.Invoke(this,new(nameof(Size)));}}
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class LiveContext
    {
        internal readonly string Kind;
        internal readonly Control Control;
        internal readonly List<LiveItem> Known=[];
        internal readonly List<LiveCollection> Collections=[];
        internal readonly List<string> Invoked=[];
        internal readonly Dictionary<string,BleeckerButton> Actions=new(StringComparer.Ordinal);
        internal LiveCollection Current=null!;
        internal LiveContext(string kind)
        {
            Kind=kind;
            if(kind=="data-table")
            {
                var data=new BleeckerDataTable();Control=data;
                data.Columns.Add(new(accessorKey:"Name",header:"Fictional item"));
                data.Columns.Add(new(accessorKey:"Count",header:"Count"));
                data.Columns.Add(new(id:"Action",header:"Original action",cellContentSelector:c=>Action(c.Row.Id,(LiveItem)c.Row.Original)));
            }
            else
            {
                var data=new BleeckerSimpleDataTable{GetRowKey=(_,index)=>index.ToString(CultureInfo.InvariantCulture)};Control=data;
                data.Columns.Add(new("Name","Fictional item",valueSelector:r=>((LiveItem)r).Name));
                data.Columns.Add(new("Count","Count",valueSelector:r=>((LiveItem)r).Count));
                data.Columns.Add(new("Action","Original action",isSortable:false,cellContentSelector:(r,index)=>Action(index.ToString(CultureInfo.InvariantCulture),(LiveItem)r)));
            }
            Control.Width=660;Control.Margin=new Thickness(20);Control.HorizontalAlignment=HorizontalAlignment.Left;Control.VerticalAlignment=VerticalAlignment.Top;
        }
        internal BleeckerTable Table=>Elements(Control).OfType<BleeckerTable>().Single();
        internal LiveItem Item(string id,string name,int count){var item=new LiveItem(id,name,count);Known.Add(item);return item;}
        internal LiveCollection Collection(params LiveItem[] items){var collection=new LiveCollection();foreach(var item in items)collection.Add(item);Collections.Add(collection);return collection;}
        internal void Source(LiveCollection collection){Current=collection;if(Control is BleeckerDataTable data)data.ItemsSource=collection;else((BleeckerSimpleDataTable)Control).ItemsSource=collection;}
        private BleeckerButton Action(string slot,LiveItem item)
        {
            if(!Actions.TryGetValue(slot,out var action)){action=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content="Open"};action.Click+=(_,_)=>Invoked.Add(((LiveItem)action.Tag).Id);Actions.Add(slot,action);}action.Tag=item;return action;
        }
        internal void Styles(Style? body,Style? heading)
        {
            var bodyCalls=0;var headingCalls=0;
            if(Control is BleeckerDataTable data){data.CellStyleSelector=c=>{bodyCalls++;return c.Id=="Name"?body:null;};data.HeaderStyleSelector=_=>{headingCalls++;return heading;};bodyCalls=headingCalls=0;data.Refresh();}
            else{var simple=(BleeckerSimpleDataTable)Control;simple.CellStyleSelector=c=>{bodyCalls++;return c.Key=="Name"?body:null;};simple.HeaderStyleSelector=_=>{headingCalls++;return heading;};bodyCalls=headingCalls=0;simple.Refresh();}
            Require(bodyCalls==Rows().Where(row=>row.Tag is not null).Sum(row=>row.Cells.Count),"Explicit refresh skipped or repeated a caller cell-style selector.");
            Require(headingCalls==Table.Sections.OfType<BleeckerTableHeader>().Single().Children.OfType<BleeckerTableRow>().Sum(row=>row.Cells.Count),"Explicit refresh skipped or repeated a caller header-style selector.");
        }
        internal BleeckerTableRow[] Rows()=>Table.Sections.OfType<BleeckerTableBody>().Single().Children.OfType<BleeckerTableRow>().ToArray();
        internal object Snapshot(UIElement root,string label)
        {
            var rows=Rows();var expected=Current.ToArray();var actual=rows.Where(r=>r.Tag is not null).ToArray();
            var result=new{stage=label,expectedRows=expected.Select(i=>new{id=i.Id,name=i.Name,count=i.Count}).ToArray(),rows=actual.Select((r,index)=>new{id=(r.Tag is BleeckerDataTableRow model?(LiveItem)model.Original:(LiveItem)r.Tag).Id,originalRetained=index<expected.Length&&ReferenceEquals(r.Tag is BleeckerDataTableRow context?context.Original:r.Tag,expected[index]),name=r.Cells[0].Content?.ToString(),count=r.Cells[1].Content?.ToString(),nameFont=r.Cells[0].FontSize,renderedNameFont=Elements(r.Cells[0]).OfType<TextBlock>().SingleOrDefault(t=>t.Name=="PART_Text")?.FontSize,headerFont=Table.Sections.OfType<BleeckerTableHeader>().Single().Children.OfType<BleeckerTableRow>().First().Cells[0].FontSize,alignment=r.Cells[0].HorizontalContentAlignment.ToString(),originalActionRetained=ReferenceEquals(r.Cells[2].Content,Actions[index.ToString(CultureInfo.InvariantCulture)])&&ReferenceEquals(((BleeckerButton)r.Cells[2].Content).Tag,expected[index])&&((BleeckerButton)r.Cells[2].Content).IsLoaded,box=Box(r,root)}).ToArray(),emptyText=expected.Length==0?rows.Single().Cells.Single().Content?.ToString():null,subscriptions=Known.Select(i=>new{id=i.Id,count=i.Subscribers,expected=Control.IsLoaded&&expected.Any(e=>ReferenceEquals(e,i))?1:0}).ToArray(),collectionSubscriptions=Collections.Select(c=>new{count=c.Subscribers,expected=Control.IsLoaded&&ReferenceEquals(c,Current)?1:0}).ToArray(),invoked=Invoked.ToArray(),loaded=Control.IsLoaded,box=Box(Control,root)};
            return JsonSerializer.SerializeToElement(result);
        }
    }
    internal static async Task RunLiveAsync(Window window,string output,string hostAssembly)
    {
        var args=Environment.GetCommandLineArgs();var position=Array.IndexOf(args,"--live-kind");var kind=position<0?"all":args[position+1];Require(kind is "all" or "data-table" or "simple","Unknown live table fixture kind.");var kinds=kind=="all"?new[]{"data-table","simple"}:new[]{kind};
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='900' Height='1000' Background='{ThemeResource BleeckerBackgroundBrush}'/>");window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,1000));var states=new List<object>();var contracts=new List<object>();var pngs=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Live table probe left its hidden boundary.");}
        async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused live table layout.");await idle.Task;root.Measure(new Size(900,1000));root.Arrange(new Rect(0,0,900,1000));root.UpdateLayout();Guard();await Task.Delay(12);}}
        async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes))});Guard();}
        try
        {
            foreach(var scheme in new[]{"Light","Dark"})foreach(var controlKind in kinds)
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var context=new LiveContext(controlKind);var a=context.Item("a","Twin",1);var b=context.Item("b","Twin",1);var source=context.Collection(a,b);context.Source(source);root.Children.Add(context.Control);await Layout();
                Require(!ReferenceEquals(a,b)&&a.Equals(b),"Equality counterexample must use distinct equal objects.");
                async Task Record(string label)
                {
                    stage=scheme+"-"+controlKind+"-"+label;await Layout();var file="data-table-live-"+stage+".png";await Capture(file);var snapshot=(JsonElement)context.Snapshot(root,label);states.Add(new{scheme,kind=controlKind,snapshot,file});
                    var expected=snapshot.GetProperty("expectedRows");var rows=snapshot.GetProperty("rows");Require(expected.GetArrayLength()==rows.GetArrayLength(),"Live original row count differs.");for(var i=0;i<expected.GetArrayLength();i++){Require(rows[i].GetProperty("originalRetained").GetBoolean()&&rows[i].GetProperty("originalActionRetained").GetBoolean(),"Live original object/action was replaced or unloaded.");Require(rows[i].GetProperty("name").GetString()==expected[i].GetProperty("name").GetString()&&rows[i].GetProperty("count").GetString()==expected[i].GetProperty("count").GetInt32().ToString(CultureInfo.InvariantCulture),"Live original notification did not reach rendered text.");}
                    if(label!="initial")foreach(var subscriber in snapshot.GetProperty("subscriptions").EnumerateArray().Concat(snapshot.GetProperty("collectionSubscriptions").EnumerateArray()))Require(subscriber.GetProperty("count").GetInt32()==subscriber.GetProperty("expected").GetInt32(),"Original producer subscription count differs.");
                }
                await Record("initial");b.Count=42;await Record("equal-object-notified");
                var c=context.Item("c","Cedar",3);source.Add(c);await Record("collection-add");source.Move(2,0);await Record("collection-move");var d=context.Item("d","Birch",4);source[1]=d;await Record("collection-replace");a.Count=99;await Record("retired-item-notified");source.RemoveAt(2);await Record("collection-remove");source.Clear();await Record("collection-clear");source.Add(a);source.Add(b);await Record("collection-restored");
                var u=context.Item("u","Orchard",5);var v=context.Item("v","Grove",6);var replacement=context.Collection(u,v);context.Source(replacement);await Record("source-replaced");Require(source.Subscribers==0&&a.Subscribers==0&&b.Subscribers==0,"Replaced producer remains subscribed.");
                root.Children.Clear();await Layout();Require(context.Known.All(i=>i.Subscribers==0)&&context.Collections.All(c=>c.Subscribers==0),"Unloaded table kept original producer subscriptions.");var w=context.Item("w","Meadow",7);v.Count=77;replacement.Add(w);root.Children.Add(context.Control);await Record("reloaded");
                var template=context.Control.Template;context.Control.Template=null;await Layout();Require(context.Actions.Values.All(button=>!button.IsLoaded),"Cleared template retained original actions.");v.Count=88;var x=context.Item("x","Willow",8);replacement.Add(x);context.Control.Template=template;await Record("template-restored");Invoke(context.Actions["0"]);await Record("original-action");Require(context.Invoked.SequenceEqual(new[]{"u"}),"Live original callback lost its original item.");
                var body=(Style)XamlReader.Load("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:o='using:Orchard' TargetType='o:BleeckerTableCell'><Setter Property='FontSize' Value='20'/><Setter Property='HorizontalContentAlignment' Value='Right'/></Style>");
                var heading=(Style)XamlReader.Load("<Style xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:o='using:Orchard' TargetType='o:BleeckerTableHead'><Setter Property='FontSize' Value='18'/></Style>");
                context.Styles(body,heading);await Record("column-styles");var first=context.Rows()[0].Cells[0];var values=new FontValues();first.SetBinding(Control.FontSizeProperty,new Binding{Source=values,Path=new PropertyPath(nameof(FontValues.Size)),Mode=BindingMode.OneWay});var binding=first.GetBindingExpression(Control.FontSizeProperty);await Record("local-binding");values.Size=28;await Record("notified-binding");context.Styles(null,null);await Record("column-styles-cleared");Require(ReferenceEquals(first,context.Rows()[0].Cells[0])&&ReferenceEquals(binding,first.GetBindingExpression(Control.FontSizeProperty))&&first.FontSize==28,"Column selector refresh replaced original cell/binding.");first.ClearValue(Control.FontSizeProperty);await Record("local-binding-cleared");
                replacement.Add(v);await Record("duplicate-reference");v.Count=111;await Record("duplicate-reference-notified");Require(v.Subscribers==1,"A repeated original reference must be observed once.");
                if(controlKind=="data-table"){await Task.Run(()=>v.Count=112);await Record("worker-item-notified");var y=context.Item("y","Field",9);await Task.Run(()=>replacement.Add(y));await Record("worker-collection-add");}
                root.Children.Clear();await Layout();Require(context.Known.All(i=>i.Subscribers==0)&&context.Collections.All(c=>c.Subscribers==0),"Final unload retained producers.");contracts.Add(new{scheme,kind=controlKind,distinctEqualOriginalsVerified=true,repeatedOriginalObservedOnce=true,replacedProducersReleased=true,unloadedProducersReleased=true,templateContentRetired=true,originalCellAndBindingRetained=true,workerNotificationsVerified=controlKind=="data-table"});
            }
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-live-native-v1",passed=true,kinds,states,contracts,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-data-table-live-native-v1",passed=false,stage,error=error.ToString(),states,contracts,pngs,guards,focus,activations,librarySha256=Hash(typeof(BleeckerDataTable).Assembly.Location)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
    }
}
