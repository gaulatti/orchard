using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Windowing;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    internal async Task<object> VerifyInteractionsAsync(BleeckerColorScheme scheme,string folder)
    {
        using var referenceStream=CatalogReferenceResources.Open(typeof(DataTableToolbarsFixture).Assembly, "Orchard.Catalog.Reference.DataTableToolbarLayout.json")!;
        using var referenceBuffer=new MemoryStream();await referenceStream.CopyToAsync(referenceBuffer);var referenceBytes=referenceBuffer.ToArray();
        var referenceSha256=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(referenceBytes)).ToLowerInvariant();using var reference=JsonDocument.Parse(referenceBytes);
        var root=(Grid)XamlRoot.Content;var app=AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var reports=new List<object>();var transfers=new List<object>();var focusDifferences=new List<object>();
        using var input=new NativeInputSession(XamlRoot,folder,$"data-table-toolbar-matrix-{scheme}");await input.AcquireAsync(Outside);
        var original=app.ClientSize;
        var stateCount=0;var caseCount=0;
        try
        {
            foreach(var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item=>item.GetProperty("scheme").GetString()==scheme.ToString()&&item.GetProperty("states").GetArrayLength()>0))
            {
                var id=sample.GetProperty("id").GetString()!;var viewport=sample.GetProperty("viewportWidth").GetInt32();if(app.ClientSize.Width!=viewport)app.ResizeClient(new SizeInt32(viewport,original.Height));
                var scenario=new ToolbarScenario(this,sample,scheme);root.Children.Add(scenario.Host);caseCount++;
                try
                {
                    await AwaitAsync(()=>scenario.Bar.IsLoaded,"Interaction source toolbar did not load.");scenario.Bar.ApplyTemplate();UpdateLayout();await MainWindow.AwaitPresentationAsync(scenario.Bar);scenario.RegisterSlots();
                    var step=0;var tabStarted=false;var held=false;
                    try
                    {
                        foreach(var sourceStep in sample.GetProperty("states").EnumerateArray())
                        {
                            var action=sourceStep.GetProperty("action").GetString()!;var expected=sourceStep.GetProperty("state");step++;
                            var resetBefore=scenario.A.Resets+scenario.B.Resets;
                            switch(action)
                            {
                                case "search-focus":await FocusAsync(scenario.Search);break;
                                case "search-fill":await ReplaceToolbarTextAsync(input,scenario.Search,"Harbor");break;
                                case "search-clear":await ReplaceToolbarTextAsync(input,scenario.Search,"");break;
                                case "search-keyboard":await FocusAsync(scenario.Search);await TypeToolbarTextAsync(input,scenario.Search,"Reed");break;
                                case "reset-hover":
                                    var hoveredReset=scenario.Reset;await input.MoveAsync(hoveredReset);
                                    try{await AwaitAsync(()=>ResetHoverPresented(hoveredReset,scheme,input),"Interaction Reset did not present its actual native hover palette.");}
                                    catch(InvalidOperationException)
                                    {
                                        var child=Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(hoveredReset,0) as FrameworkElement;
                                        var surface=Descendants<ContentPresenter>(hoveredReset).SingleOrDefault(element=>element.Name=="ContentPresenter");
                                        await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-hover-failure-{id}-{scheme}.json"),JsonSerializer.Serialize(new{id,step,hoveredReset.IsPointerOver,hoveredReset.IsPressed,hoveredReset.IsEnabled,hoveredReset.IsLoaded,theme=hoveredReset.ActualTheme.ToString(),pointer=input.ReadPointerPosition(),owned=input.OwnsCurrentPoint,bounds=input.ScreenBounds(hoveredReset),background=DescribeBrush(surface?.Background),states=child is null?[]:VisualStateManager.GetVisualStateGroups(child).Select(group=>new{group.Name,current=group.CurrentState?.Name}).ToArray()},new JsonSerializerOptions{WriteIndented=true}));
                                        await MainWindow.CaptureElementAsync(root,Path.Combine(folder,$"data-table-toolbar-hover-failure-{id}-{scheme}.png"));throw;
                                    }
                                    break;
                                case "reset-held":await input.PressPointerAsync();held=true;await AwaitAsync(()=>scenario.Reset.IsPressed,"Interaction Reset did not enter an actual held press.");break;
                                case "reset-release":await ReleaseToolbarPointerAsync(input,$"{id}-{step}-{scheme}",folder);held=false;break;
                                case "reset-click":await input.ClickAsync(scenario.Reset);break;
                                case "reset-keyboard":case "reset-Enter":await FocusAsync(scenario.Reset);await input.KeyAsync(0x0D,scenario.Reset);break;
                                case "filter-slot":await input.ClickAsync(scenario.Slot("filters"));break;
                                case "action-slot":await input.ClickAsync(scenario.Slot("actions"));break;
                                case "Tab":
                                    if(!tabStarted){await FocusAsync(scenario.Before);tabStarted=true;}
                                    await TransferToolbarTabAsync(input,scenario,transfers,id,step,expected.GetProperty("focused").GetString()!);break;
                                case "external-column-filter":scenario.Active.SetFilters(new[]{new KeyValuePair<string,object?>("title","Vale")});break;
                                case "external-global-filter":scenario.Active.SetGlobal("Vale");break;
                                case "search-column-change":scenario.Bar.SearchColumn="status";scenario.Bar.SearchPlaceholder="Status search";break;
                                case "search-column-hidden":scenario.Bar.SearchColumn="";break;
                                case "replace-table":scenario.SelectTable(true);scenario.Bar.SearchColumn="category";break;
                                case "second-table-search":await ReplaceToolbarTextAsync(input,scenario.Search,"Reed");break;
                                case "restore-table":scenario.SelectTable(false);scenario.Bar.SearchColumn="title";break;
                                case "replace-slots":scenario.ReplaceSlots();break;
                                default:throw new InvalidOperationException("Unknown frozen interaction: "+action);
                            }
                            await MainWindow.AwaitPresentationAsync(scenario.Bar);scenario.RegisterSlots();
                            var errors=new List<string>();void Check(string name,Action verify){try{verify();}catch(InvalidOperationException error){errors.Add(name+": "+error.Message);}}
                            Check("consumer",()=>scenario.VerifyConsumer(expected));Check("geometry",()=>VerifyToolbarInteractionGeometry(scenario.Bar,expected));
                            if(action=="reset-hover")Check("rendered-hover",()=>Require(ResetHoverPresented(scenario.Reset,scheme,input),"Reset lost its native hover presentation before capture."));
                            Check("callbacks",()=>
                            {
                                var increment=action is "reset-release" or "reset-click" or "reset-keyboard" or "reset-Enter"?1:0;
                                Require(scenario.A.Resets+scenario.B.Resets==resetBefore+increment,"Reset callback timing/count differs from its native action boundary.");
                                var expectedCalls=expected.GetProperty("events").EnumerateArray().Where(item=>item.GetProperty("kind").GetString()=="slot").ToArray();
                                Require(scenario.Calls.Count==expectedCalls.Length&&scenario.Calls.All(call=>call.OriginalTable&&call.OriginalRows&&call.OriginalNode)&&scenario.Calls.Zip(expectedCalls).All(pair=>pair.First.Role==pair.Second.GetProperty("slot").GetString()&&pair.First.Index==pair.Second.GetProperty("index").GetInt32()&&pair.First.Table==pair.Second.GetProperty("table").GetString()),"Original slot callback role/index/table/ownership/count differs.");
                            });
                            var sourceFocus=expected.GetProperty("focused").GetString()!;var nativeFocus=scenario.DescribeFocus();
                            if(sourceFocus!=nativeFocus)focusDifferences.Add(new{id,step,action,sourceFocus,nativeFocus});
                            var capture=$"data-table-toolbar-matrix-{id}-{step:D2}-{action}-{scheme}.png";
                            await MainWindow.CaptureElementAsync(root,Path.Combine(folder,capture));
                            var hover=action=="reset-hover"?new{frameworkIsPointerOver=scenario.Reset.IsPointerOver,physicalHitAndPalette=ResetHoverPresented(scenario.Reset,scheme,input)}:null;
                            reports.Add(new{id,step,action,sourceFocus,nativeFocus,focus=scenario.FocusEvidence(),sourceFocusMatched=sourceFocus==nativeFocus,table=scenario.ActiveName,scenario.Active.VisibleText,filters=scenario.Active.CurrentFilters.Count,resets=scenario.Active.Resets,slotCallbacks=scenario.Calls.ToArray(),surface=Bounds(scenario.Bar,root),hover,capture,errors});stateCount++;
                            Require(errors.Count==0,$"Toolbar interaction {id}/{step}/{action}: "+string.Join(" | ",errors));
                        }
                    }
                    finally{if(held)await ReleaseToolbarPointerAsync(input,$"{id}-cleanup-{scheme}",folder);}
                }
                finally{root.Children.Remove(scenario.Host);scenario.Dispose();}
            }
        }
        finally
        {
            if(app.ClientSize!=original)app.ResizeClient(original);await MainWindow.AwaitPresentationAsync(this);
            await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-matrix-{scheme}.json"),JsonSerializer.Serialize(new{format="orchard-data-table-toolbar-native-interactions-v1",scheme=scheme.ToString(),referenceSha256,caseCount,stateCount,reports,transfers,focusDifferences},new JsonSerializerOptions{WriteIndented=true}));
        }
        Require(caseCount==26&&stateCount==188,"Frozen interaction matrix is incomplete.");
        return new{caseCount,stateCount,consumerGeometryAndInputPassed=true,sourceFocusMatched=focusDifferences.Count==0,focusDifferences};
    }

    private static async Task TypeToolbarTextAsync(NativeInputSession input,BleeckerInput search,string text)
    {
        foreach(var character in text)
        {
            Require(char.IsAsciiLetter(character),"Frozen toolbar text requires an explicit native keyboard mapping.");
            var key=(ushort)char.ToUpperInvariant(character);if(char.IsUpper(character))await input.ModifiedKeyAsync(0x10,key,search);else await input.KeyAsync(key,search);
        }
    }
    private static async Task ReplaceToolbarTextAsync(NativeInputSession input,BleeckerInput search,string text)
    {await FocusAsync(search);await input.ModifiedKeyAsync(0x11,0x41,search);await input.KeyAsync(0x08,search);await TypeToolbarTextAsync(input,search,text);}

    private static bool ResetHoverPresented(Button reset,BleeckerColorScheme scheme,NativeInputSession input)
    {
        if(!input.OwnsCurrentPoint||reset.IsPressed)return false;
        var screen=input.ReadPointerPosition();var client=NativeScreenCapture.ClientBounds(input.Window);var root=reset.XamlRoot.Content;
        var logical=new Windows.Foundation.Point((screen.X-client.X)/reset.XamlRoot.RasterizationScale,(screen.Y-client.Y)/reset.XamlRoot.RasterizationScale);
        var hit=Microsoft.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(logical,root).FirstOrDefault();var owned=false;
        for(var node=hit as DependencyObject;node is not null;node=Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(node))if(ReferenceEquals(node,reset)){owned=true;break;}
        var surface=Descendants<ContentPresenter>(reset).SingleOrDefault(element=>element.Name=="ContentPresenter");
        return owned&&surface?.Background is Microsoft.UI.Xaml.Media.SolidColorBrush background&&background.Color.A==(scheme==BleeckerColorScheme.Light?31:255)&&
            background.Color.R==(scheme==BleeckerColorScheme.Light?230:255)&&background.Color.G==(scheme==BleeckerColorScheme.Light?213:255)&&background.Color.B==(scheme==BleeckerColorScheme.Light?184:255)&&
            Math.Abs(background.Opacity-(scheme==BleeckerColorScheme.Light?1:.06))<.000001;
    }

    private void VerifyToolbarInteractionGeometry(BleeckerDataTableToolbar bar,JsonElement expected)
    {
        Compare(new Windows.Foundation.Rect(0,0,bar.ActualWidth,bar.ActualHeight),expected.GetProperty("surface").GetProperty("box"));
        var nodes=Nodes(expected.GetProperty("surface")).ToArray();var inputs=nodes.Where(node=>node.GetProperty("tag").GetString()=="INPUT").ToArray();var search=Descendants<BleeckerInput>(bar).SingleOrDefault();
        Require((search is null)==(inputs.Length==0),"Search visibility differs.");if(search is not null){Compare(Bounds(search,bar),inputs.Single().GetProperty("box"));Require(search.Text==inputs[0].GetProperty("value").GetString()&&search.PlaceholderText==inputs[0].GetProperty("placeholder").GetString(),"Controlled search text or placeholder differs.");}
        var sourceReset=nodes.Where(node=>node.GetProperty("tag").GetString()=="BUTTON"&&node.GetProperty("text").GetString()=="Reset").ToArray();var reset=Descendants<Button>(bar).SingleOrDefault(button=>button.Name=="PART_Reset");
        Require((reset is null)==(sourceReset.Length==0),"Reset visibility differs.");if(reset is not null)Compare(Bounds(reset,bar),sourceReset.Single().GetProperty("box"));
        foreach(var role in new[]{"filters","actions"})
        {
            var originals=Descendants<FrameworkElement>(bar).Where(element=>element.Tag is string tag&&tag.StartsWith(role+":",StringComparison.Ordinal)).OrderBy(element=>element.Tag).ToArray();
            var source=nodes.Where(node=>node.TryGetProperty("attributes",out var attributes)&&attributes.TryGetProperty("data-slot",out var slot)&&slot.GetString()==role).ToArray();
            Require(originals.Length==source.Length,"Original slot element count differs.");for(var index=0;index<originals.Length;index++)Compare(Bounds(originals[index],bar),source[index].GetProperty("box"));
        }
    }
}
