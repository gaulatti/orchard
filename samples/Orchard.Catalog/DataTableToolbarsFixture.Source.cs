using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    private async Task VerifySourceAsync(BleeckerColorScheme scheme,string folder)
    {
        using var reference=JsonDocument.Parse(typeof(DataTableToolbarsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataTableToolbarLayout.json")!);
        using var captionReference=JsonDocument.Parse(typeof(DataTableToolbarsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DataTableToolbarCaption.json")!);
        var root=(Grid)XamlRoot.Content;var host=new Grid{Margin=new Thickness(16),Background=root.Background,RequestedTheme=scheme==BleeckerColorScheme.Light?ElementTheme.Light:ElementTheme.Dark};Grid.SetRowSpan(host,root.RowDefinitions.Count);Grid.SetColumnSpan(host,root.ColumnDefinitions.Count);
        var app=AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);var original=app.ClientSize;var reports=new List<object>();root.Children.Add(host);
        try
        {
            foreach(var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("scheme").GetString()==scheme.ToString()))
            {
                var id=sample.GetProperty("id").GetString()!;var props=sample.GetProperty("props");var viewport=sample.GetProperty("viewportWidth").GetInt32();if(app.ClientSize.Width!=viewport)app.ResizeClient(new SizeInt32(viewport,original.Height));
                var model=new ToolbarFilterTable();model.SetInitial(Filters(props,"initialColumnFilters"));model.SetFilters(Filters(props,"columnFilters"));if(props.TryGetProperty("globalFilter",out var global))model.SetGlobal(Value(global));
                var bar=new BleeckerDataTableToolbar{Table=model,Width=sample.GetProperty("hostWidth").GetDouble(),VerticalAlignment=VerticalAlignment.Top,HorizontalAlignment=HorizontalAlignment.Left,FlowDirection=sample.GetProperty("direction").GetString()=="rtl"?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                if(props.TryGetProperty("searchColumn",out var column))bar.SearchColumn=column.GetString();if(props.TryGetProperty("searchPlaceholder",out var placeholder))bar.SearchPlaceholder=placeholder.GetString()!;
                bar.FilterRenderers=props.TryGetProperty("filters",out var filters)?SourceContent(filters,"filters",bar,model):null;bar.Actions=props.TryGetProperty("actions",out var actions)?SourceContent(actions,"actions",bar,model):null;
                if(id=="custom-layout"){bar.Spacing=20;bar.Padding=new Thickness(8);bar.BorderThickness=new Thickness(2);bar.BorderBrush=(Brush)Application.Current.Resources["BleeckerSeaBrush"];}
                var probe=new TextBlock{Text=model.VisibleText,Margin=new Thickness(0,180,0,0),VerticalAlignment=VerticalAlignment.Top};model.Changed+=(_,_)=>probe.Text=model.VisibleText;
                host.Children.Clear();host.Children.Add(bar);host.Children.Add(probe);await AwaitAsync(()=>bar.IsLoaded,"Source toolbar did not load.");bar.ApplyTemplate();UpdateLayout();await AwaitSourcePresentationAsync(bar);
                var expected=sample.GetProperty("normal");var errors=new List<string>();
                void Check(string name,Action verify){try{verify();}catch(InvalidOperationException error){errors.Add(name+": "+error.Message);}}
                Check("surface",()=>Compare(new Rect(0,0,bar.ActualWidth,bar.ActualHeight),expected.GetProperty("surface").GetProperty("box")));
                var sourceNodes=Nodes(expected.GetProperty("surface")).ToArray();var nativeSearch=Descendants<BleeckerInput>(bar).SingleOrDefault();var sourceSearch=sourceNodes.Where(n=>n.GetProperty("tag").GetString()=="INPUT").ToArray();
                Check("search",()=>{Require((nativeSearch is not null)==(sourceSearch.Length==1),"Search visibility differs.");if(sourceSearch.Length==1){Compare(Bounds(nativeSearch!,bar),sourceSearch[0].GetProperty("box"));Require(nativeSearch!.Text==sourceSearch[0].GetProperty("value").GetString()&&nativeSearch.PlaceholderText==sourceSearch[0].GetProperty("placeholder").GetString(),"Controlled text/placeholder differs.");Require(Microsoft.UI.Xaml.Automation.Peers.FrameworkElementAutomationPeer.CreatePeerForElement(nativeSearch).GetName()==nativeSearch.PlaceholderText,"Search automation name differs from its original placeholder.");}});
                var nativeReset=Descendants<Button>(bar).Where(button=>button.Name=="PART_Reset").ToArray();var sourceReset=sourceNodes.Where(n=>n.GetProperty("tag").GetString()=="BUTTON"&&n.GetProperty("text").GetString()=="Reset").ToArray();
                Check("reset",()=>{Require(nativeReset.Length==sourceReset.Length,"Reset visibility differs.");if(nativeReset.Length>0)Compare(Bounds(nativeReset[0],bar),sourceReset[0].GetProperty("box"));});
                Check("reset-caption-baseline",()=>{if(id!="column-filter")return;var text=Descendants<TextBlock>(nativeReset.Single()).Single(text=>text.Text=="Reset");var sourceBaseline=captionReference.RootElement.GetProperty("results").EnumerateArray().Single(item=>item.GetProperty("scheme").GetString()==scheme.ToString()).GetProperty("baselineY").GetDouble();Require(Math.Abs(Bounds(text,bar).Y+text.BaselineOffset-sourceBaseline)<=1d/32,"Reset caption baseline differs from the original source's measured baseline.");});
                foreach(var role in new[]{"filters","actions"})
                {
                    var actual=Descendants<FrameworkElement>(bar).Where(element=>element.Tag is string tag&&tag.StartsWith(role+":",StringComparison.Ordinal)).OrderBy(element=>element.Tag).ToArray();
                    var source=sourceNodes.Where(n=>n.TryGetProperty("attributes",out var attributes)&&attributes.TryGetProperty("data-slot",out var slot)&&slot.GetString()==role).ToArray();
                    Check(role,()=>{Require(actual.Length==source.Length,"Slot element count differs.");for(var index=0;index<actual.Length;index++){Compare(Bounds(actual[index],bar),source[index].GetProperty("box"));if(actual[index] is Button button)VerifySlotPaint(button,reference.RootElement.GetProperty("styles")[source[index].GetProperty("styleIndex").GetInt32()]);}});
                }
                Check("consumer",()=>Require(model.VisibleText==expected.GetProperty("visibleRows").GetString()&&probe.Text==model.VisibleText&&probe.IsLoaded&&model.Writes.Count==0,"Source display mutated a filter or row projection differs."));
                reports.Add(new{id,viewport,width=bar.ActualWidth,height=bar.ActualHeight,input=nativeSearch is null?(Rect?)null:Bounds(nativeSearch,bar),model.VisibleText,writes=model.Writes.Count,errors});
                if(new[]{"default","column-filter","small-viewport","at-breakpoint","constrained-host","multilingual","rtl"}.Contains(id))await MainWindow.CaptureElementAsync(root,Path.Combine(folder,$"data-table-toolbar-source-{id}-{scheme}.png"));
            }
        }
        finally{root.Children.Remove(host);if(app.ClientSize!=original)app.ResizeClient(original);await MainWindow.AwaitPresentationAsync(this);await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-source-{scheme}.json"),JsonSerializer.Serialize(reports,new JsonSerializerOptions{WriteIndented=true}));}
        var failures=reports.Select(report=>JsonSerializer.SerializeToElement(report)).Where(report=>report.GetProperty("errors").GetArrayLength()>0).ToArray();Require(failures.Length==0,"Toolbar source mismatches: "+string.Join(" | ",failures.Select(f=>f.GetProperty("id").GetString()+": "+string.Join(';',f.GetProperty("errors").EnumerateArray().Select(e=>e.GetString())))));
    }
    private static IEnumerable<JsonElement> Nodes(JsonElement node){yield return node;if(node.TryGetProperty("children",out var children))foreach(var child in children.EnumerateArray())foreach(var value in Nodes(child))yield return value;}
    private static IEnumerable<KeyValuePair<string,object?>> Filters(JsonElement props,string name)=>props.TryGetProperty(name,out var values)?values.EnumerateArray().Select(filter=>new KeyValuePair<string,object?>(filter.GetProperty("id").GetString()!,Value(filter.GetProperty("value")))).ToArray():[];
    private static object? Value(JsonElement value)=>value.ValueKind switch
    {
        JsonValueKind.Null=>null,JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.String=>value.GetString(),JsonValueKind.Number=>value.GetDouble(),JsonValueKind.Array=>value.EnumerateArray().Select(Value).ToArray(),
        JsonValueKind.Object when value.TryGetProperty("$undefined",out _)=>null,
        JsonValueKind.Object when value.TryGetProperty("$number",out var number)=>number.GetString() switch{"NaN"=>double.NaN,"Infinity"=>double.PositiveInfinity,"-Infinity"=>double.NegativeInfinity,"-0"=>-0d,_=>throw new ArgumentException("Unknown source number.")},
        JsonValueKind.Object=>value.EnumerateObject().ToDictionary(p=>p.Name,p=>Value(p.Value)),_=>throw new ArgumentException("Unknown source value.")
    };
    private object? SourceContent(JsonElement value,string role,BleeckerDataTableToolbar owner,ToolbarFilterTable table)
    {
        if(value.ValueKind!=JsonValueKind.Object)return Value(value);
        if(value.GetProperty("kind").GetString()=="text")return new TextBlock{Tag=role+":0",Text=value.GetProperty("text").GetString(),FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"],FontSize=value.TryGetProperty("size",out var size)?size.GetDouble():16,LineHeight=24,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,UseLayoutRounding=false};
        var elements=new ObservableCollection<UIElement>();foreach(var label in value.GetProperty("labels").EnumerateArray())
        {
            var button=new Button{Style=(Style)Resources["SourceSlotButtonStyle"],FontSize=value.TryGetProperty("fontSize",out var buttonFont)?buttonFont.GetDouble():14,Tag=role+":"+elements.Count,Content=new SourceLineBox(new TextBlock{Text=label.GetString(),FontSize=value.TryGetProperty("fontSize",out var font)?font.GetDouble():14,LineHeight=20,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,TextWrapping=value.TryGetProperty("wrap",out var wrap)&&wrap.GetBoolean()?TextWrapping.Wrap:TextWrapping.NoWrap}),FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"],Width=value.TryGetProperty("width",out var width)?width.GetDouble():role=="filters"?176:104,Height=value.TryGetProperty("height",out var height)?height.GetDouble():36,MinWidth=0,MinHeight=0,Padding=new Thickness(12,0,12,0),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),UseLayoutRounding=false,IsEnabled=!value.TryGetProperty("disabled",out var disabled)||!disabled.GetBoolean()};
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button,label.GetString());
            if(role=="filters")button.Click+=(_,_)=>table.GetColumn("status")!.SetFilterValue(table.GetColumn("status")!.GetFilterValue() is null?"Draft":"");elements.Add(button);
        }return elements;
    }
    private static Rect Bounds(FrameworkElement element,UIElement parent)
    {
        var owner=(FrameworkElement)parent;var root=owner.XamlRoot.Content;var bounds=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));var origin=owner.TransformToVisual(root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));return new Rect(bounds.X-origin.X,bounds.Y-origin.Y,bounds.Width,bounds.Height);
    }
    private static void Compare(Rect actual,JsonElement expected){foreach(var(key,value)in new[]{("x",actual.X),("y",actual.Y),("width",actual.Width),("height",actual.Height)})Require(Math.Abs(value-expected.GetProperty(key).GetDouble())<=1d/32,$"{key} {value:R}/{expected.GetProperty(key).GetDouble():R}");}
    private static void VerifySlotPaint(Button button,JsonElement expected)
    {
        Require(button.UseSystemFocusVisuals&&button.FocusVisualPrimaryThickness==new Thickness(2)&&button.FocusVisualSecondaryThickness==new Thickness(0)&&button.FocusVisualMargin==new Thickness(0),"Consumer keyboard focus geometry differs from the measured browser outline.");
        Require(button.FocusVisualPrimaryBrush is SolidColorBrush focus&&focus.Color.A==255&&focus.Color.R==16&&focus.Color.G==16&&focus.Color.B==16&&focus.Opacity==1,"Consumer keyboard focus paint differs from the original browser outline.");
        Require(VisualTreeHelper.GetChild(button,0) is Border{Name:"PART_SourceSlotSurface"},"Consumer slot retained Windows default chrome.");
        var surface=(Border)VisualTreeHelper.GetChild(button,0);
        void Color(Brush brush,string css)
        {
            var parts=System.Text.RegularExpressions.Regex.Matches(css,@"[\d.]+").Select(match=>double.Parse(match.Value,System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            Require(brush is SolidColorBrush,"Source consumer paint must remain a flat brush.");var actual=((SolidColorBrush)brush).Color;
            Require(actual.R==parts[0]&&actual.G==parts[1]&&actual.B==parts[2]&&actual.A/255d*brush.Opacity==(parts.Length==4?parts[3]:1),"Original consumer CSS color differs.");
        }
        Color(surface.BorderBrush,expected.GetProperty("borderColor").GetString()!);Color(surface.Background,expected.GetProperty("backgroundColor").GetString()!);Color(button.Foreground,expected.GetProperty("color").GetString()!);
        var label=Descendants<TextBlock>(button).First(text=>text.Opacity>0);
        Require(label.FontFamily.Source==button.FontFamily.Source,$"Consumer label family differs: {label.FontFamily.Source}; expected {button.FontFamily.Source}.");
        Require(label.FontWeight.Weight==button.FontWeight.Weight&&label.FontSize==button.FontSize,"Consumer label did not retain its actual source typography.");
        Color(label.Foreground,expected.GetProperty("color").GetString()!);
        Require(button.FontWeight.Weight==ushort.Parse(expected.GetProperty("fontWeight").GetString()!)&&button.FontSize==double.Parse(expected.GetProperty("fontSize").GetString()![..^2],System.Globalization.CultureInfo.InvariantCulture)&&surface.BorderThickness==new Thickness(1)&&surface.CornerRadius==new CornerRadius(7),"Original consumer CSS typography or border differs.");
    }
    private static async Task AwaitSourcePresentationAsync(FrameworkElement bar)
    {
        // These source specimens have their complete direction/props before
        // mounting. Require both a committed layout and two actual Rendering
        // callbacks. Keep live mutations, pointer gates and final captures on
        // their established ordering and stability checks.
        bar.UpdateLayout();
        var frames=0;var presented=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(2));
        void Rendering(object? sender,object args){if(++frames==2)presented.TrySetResult();}
        CompositionTarget.Rendering+=Rendering;
        try
        {
            var committed=Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(bar).Compositor.RequestCommitAsync().AsTask();
            await Task.WhenAll(presented.Task,committed).WaitAsync(deadline.Token);
        }
        finally{CompositionTarget.Rendering-=Rendering;}
    }
}
