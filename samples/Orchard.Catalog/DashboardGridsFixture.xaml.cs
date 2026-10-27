using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DashboardGridsFixture:UserControl
{
    public DashboardGridsFixture()=>InitializeComponent();
    private void OnReview(object sender,RoutedEventArgs args)=>ReviewStatus.Text="Fictional review opened";
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        Require(XamlRoot.RasterizationScale==1&&new Windows.UI.ViewManagement.UISettings().TextScaleFactor==1,"Frozen Dashboard Grid comparison requires display/text scale 1; scaling acceptance remains open.");
        using var input=new NativeInputSession(XamlRoot,folder,$"dashboard-grid-{scheme}");await input.AcquireAsync(Specimen);
        var window=AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);var original=window.ClientSize;
        var presenter=(OverlappedPresenter)window.Presenter;var maximized=presenter.State==OverlappedPresenterState.Maximized;
        var root=CatalogFixtureRoot.From(XamlRoot);var host=new Grid{Margin=new Thickness(20),Background=root.Background};
        Grid.SetRowSpan(host,root.RowDefinitions.Count);Grid.SetColumnSpan(host,root.ColumnDefinitions.Count);root.Children.Add(host);
        using var reference=JsonDocument.Parse(CatalogReferenceResources.Open(typeof(DashboardGridsFixture).Assembly, "Orchard.Catalog.Reference.DashboardGridLayout.json")!);
        var reports=new List<object>();
        try
        {
            presenter.Restore(false);
            foreach(var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(x=>x.GetProperty("scheme").GetString()==scheme.ToString()))
            {
                var viewport=item.GetProperty("viewport").GetInt32();window.ResizeClient(new SizeInt32(viewport,745));
                await AwaitAsync(()=>Math.Abs(XamlRoot.Size.Width-viewport)<.001,"Dashboard Grid viewport did not resize.");
                var props=item.GetProperty("props");var id=item.GetProperty("id").GetString()!;
                var grid=new BleeckerDashboardGrid{Width=item.GetProperty("width").GetDouble(),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,
                    Background=root.Background,Columns=props.TryGetProperty("columns",out var columns)?columns.GetInt32():0,
                    FlowDirection=item.TryGetProperty("rtl",out var rtl)&&rtl.GetBoolean()?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                var index=0;
                foreach(var specimen in props.GetProperty("items").EnumerateArray())
                {
                    var block=new Border{Height=specimen.GetProperty("height").GetDouble(),UseLayoutRounding=false,
                        Background=new SolidColorBrush(index%2==0?Windows.UI.Color.FromArgb(255,185,199,209):Windows.UI.Color.FromArgb(255,166,93,87))};
                    if(specimen.TryGetProperty("width",out var width)){block.Width=width.GetDouble();block.HorizontalAlignment=HorizontalAlignment.Left;}
                    grid.Children.Add(new BleeckerDashboardSpan{Span=ReadSpan(specimen.GetProperty("span")),Content=block});index++;
                }
                host.Children.Clear();host.Children.Add(grid);await AwaitAsync(()=>grid.IsLoaded,"Dashboard Grid did not mount.");UpdateLayout();await FrameAsync();UpdateLayout();
                var expected=item.GetProperty("layout");
                reports.Add(new{id,viewport,height=grid.ActualHeight,expectedHeight=expected.GetProperty("grid").GetProperty("height").GetDouble(),cells=grid.Children.Cast<FrameworkElement>().Select(c=>new{slot=Bounds(c,grid),content=Bounds((FrameworkElement)((BleeckerDashboardSpan)c).Content,grid)}).ToArray()});
                await File.WriteAllTextAsync(Path.Combine(folder,$"dashboard-grid-layout-{scheme}.json"),JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));
                Near(grid.ActualHeight,expected.GetProperty("grid").GetProperty("height").GetDouble(),$"{id} height");
                var expectedCells=expected.GetProperty("cells").EnumerateArray().ToArray();
                for(var i=0;i<grid.Children.Count;i++)
                {
                    var span=(BleeckerDashboardSpan)grid.Children[i];var block=(FrameworkElement)span.Content;
                    Compare(Bounds(span,grid),expectedCells[i].GetProperty("slot"),expected.GetProperty("grid"),$"{id}/{i} slot");
                    Compare(Bounds(block,grid),expectedCells[i].GetProperty("content"),expected.GetProperty("grid"),$"{id}/{i} content");
                    Require(!span.IsTabStop&&ReferenceEquals(span.Content,block),"Dashboard Span changed child identity or added a keyboard stop.");
                }
                Require(!grid.IsTabStop&&FrameworkElementAutomationPeer.CreatePeerForElement(grid).GetAutomationControlType()==AutomationControlType.Group,"Dashboard Grid lost passive native grouping.");
                if(id is "fractional" or "rtl" or "implicit-content" or "full-empty-row" or "constrained")await CaptureAsync(grid,Path.Combine(folder,$"dashboard-grid-{id}-{scheme}.png"));
            }
            await MutationsAndInputAsync(host,input);
        }
        finally
        {
            root.Children.Remove(host);window.ResizeClient(original);if(maximized)presenter.Maximize();
            await AwaitAsync(()=>maximized||Math.Abs(XamlRoot.Size.Width-original.Width)<.001,"Dashboard Grid viewport did not restore.");
        }
    }
    private async Task MutationsAndInputAsync(Grid host,NativeInputSession input)
    {
        var grid=new BleeckerDashboardGrid{Width=600,Columns=3,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
        var source=new TextBlock{Tag=3};grid.SetBinding(BleeckerDashboardGrid.ColumnsProperty,new Binding{Source=source,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
        for(var i=0;i<4;i++)grid.Children.Add(new BleeckerDashboardSpan{Content=new Border{Height=32,UseLayoutRounding=false}});
        host.Children.Clear();host.Children.Add(grid);await AwaitAsync(()=>grid.IsLoaded,"Dashboard mutation fixture did not mount.");UpdateLayout();
        source.Tag=2;UpdateLayout();Require(grid.Columns==2,"Dashboard Columns binding did not update.");Near(((FrameworkElement)grid.Children[0]).ActualWidth,288,"bound two-column width");
        var first=(BleeckerDashboardSpan)grid.Children[0];first.Span=BleeckerDashboardSpanSize.Full;UpdateLayout();Near(first.ActualWidth,600,"live Full span");
        first.Visibility=Visibility.Collapsed;UpdateLayout();Near(Bounds((FrameworkElement)grid.Children[1],grid).X,0,"collapsed item placement");
        first.Visibility=Visibility.Visible;first.Span=BleeckerDashboardSpanSize.One;grid.Gap=8;UpdateLayout();Near(Bounds((FrameworkElement)grid.Children[1],grid).X-((FrameworkElement)grid.Children[0]).ActualWidth,8,"gap override");
        grid.Children.Remove(first);grid.Children.Add(first);UpdateLayout();Require(ReferenceEquals(grid.Children[^1],first),"Dashboard Grid replaced a moved child.");
        host.Children.Remove(grid);host.Children.Add(grid);await AwaitAsync(()=>grid.IsLoaded,"Dashboard Grid did not remount.");UpdateLayout();Near(first.ActualWidth,296,"remounted width");
        Reject(()=>new BleeckerDashboardGrid{Columns=7});Reject(()=>new BleeckerDashboardGrid{Gap=double.NaN});Reject(()=>new BleeckerDashboardSpan{Span=(BleeckerDashboardSpanSize)99});
        var before=new BleeckerButton{Content="Before grid"};var action=new BleeckerButton{Content="Open fictional review"};var after=new BleeckerButton{Content="After grid"};var clicks=0;action.Click+=(_,_)=>clicks++;
        grid.Children.Clear();grid.Children.Add(new BleeckerDashboardSpan{Content=action});
        var stack=new StackPanel{VerticalAlignment=VerticalAlignment.Top};host.Children.Clear();stack.Children.Add(before);stack.Children.Add(grid);stack.Children.Add(after);host.Children.Add(stack);
        await AwaitAsync(()=>after.IsLoaded,"Dashboard keyboard fixture did not mount.");UpdateLayout();await input.AcquireAsync(before);
        var previous=Part<Button>(before,"PART_Action");var target=Part<Button>(action,"PART_Action");var next=Part<Button>(after,"PART_Action");
        Require(previous.Focus(FocusState.Keyboard),"Could not focus preceding dashboard action.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),target),"Tab skipped dashboard child action.");
        input.Key(0x20);await AwaitAsync(()=>clicks==1,"Dashboard child did not receive physical Space.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),next),"Tab did not leave dashboard child.");
        input.ShiftKey(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),target),"Reverse Tab skipped dashboard child.");
        ((BleeckerDashboardSpan)grid.Children[0]).IsEnabled=false;UpdateLayout();Require(!target.IsEnabled,"Disabled span did not disable its child action.");
    }
    private static void Reject(Func<object> action){try{_ = action();}catch(ArgumentOutOfRangeException){return;}throw new InvalidOperationException("Dashboard Grid accepted an invalid constraint.");}
    private static BleeckerDashboardSpanSize ReadSpan(JsonElement value)=>value.ValueKind==JsonValueKind.String?BleeckerDashboardSpanSize.Full:(BleeckerDashboardSpanSize)value.GetInt32();
    private static Rect Bounds(FrameworkElement element,FrameworkElement owner)
    {
        var root=owner.XamlRoot.Content;var bounds=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
        var origin=owner.TransformToVisual(root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));return new Rect(bounds.X-origin.X,bounds.Y-origin.Y,bounds.Width,bounds.Height);
    }
    private static void Compare(Rect actual,JsonElement expected,JsonElement origin,string label)
    {
        Near(actual.X,expected.GetProperty("x").GetDouble()-origin.GetProperty("x").GetDouble(),label+" x");Near(actual.Y,expected.GetProperty("y").GetDouble()-origin.GetProperty("y").GetDouble(),label+" y");
        Near(actual.Width,expected.GetProperty("width").GetDouble(),label+" width");Near(actual.Height,expected.GetProperty("height").GetDouble(),label+" height");
    }
    private static async Task CaptureAsync(BleeckerDashboardGrid grid,string path)
    {
        var bitmap=new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();await bitmap.RenderAsync(grid);Require(bitmap.PixelWidth>0&&bitmap.PixelHeight>0,"Dashboard Grid did not render.");
        var buffer=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)buffer.Length)];using(var reader=Windows.Storage.Streams.DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
        foreach(var span in grid.Children.Cast<BleeckerDashboardSpan>())
        {
            var block=(Border)span.Content;var bounds=Bounds(block,grid);if(bounds.Width<=0)continue;
            var x=(int)(bounds.X+bounds.Width/2);var y=(int)(bounds.Y+bounds.Height/2);var offset=(y*bitmap.PixelWidth+x)*4;var color=((SolidColorBrush)block.Background).Color;
            Require(bytes[offset]==color.B&&bytes[offset+1]==color.G&&bytes[offset+2]==color.R,"Dashboard Grid child fill did not render.");
        }
        await MainWindow.SavePngAsync(path,bytes,bitmap.PixelWidth,bitmap.PixelHeight);
    }
    private static T Part<T>(DependencyObject owner,string name)where T:FrameworkElement
    {
        if(owner is T match&&match.Name==name)return match;for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){try{return Part<T>(VisualTreeHelper.GetChild(owner,i),name);}catch(InvalidOperationException){}}
        throw new InvalidOperationException($"Missing dashboard child part {name}.");
    }
    private static async Task FrameAsync(){var frame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);void Presented(object? sender,object args)=>frame.TrySetResult();CompositionTarget.Rendering+=Presented;try{await frame.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Presented;}}
    private static async Task AwaitAsync(Func<bool> ready,string message){var watch=Stopwatch.StartNew();while(!ready()&&watch.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);Require(ready(),message);}
    private static void Near(double actual,double expected,string label)=>Require(Math.Abs(actual-expected)<.001,$"Dashboard Grid {label} differs: {actual:R}/{expected:R}.");
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
