using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;
public partial class App
{
    private async void RunHiddenFeedColumnsAsync(string referenceDirectory)
    {
        var arguments=Environment.GetCommandLineArgs();var index=Array.IndexOf(arguments,"--verify-output");if(index<0||index+1>=arguments.Length){Environment.Exit(2);return;}var output=arguments[index+1];
        var handle=nint.Zero;var frames=new List<object>();var checks=new List<object>();var failures=new List<string>();var completed=false;var guards=0;var activationEvents=0;string? failure=null;string? isolationFailure=null;string? referenceHash=null;
        var diagnostics=new List<string>();AppDomain.CurrentDomain.FirstChanceException+=(_,args)=>{if(diagnostics.Count<16&&args.Exception.StackTrace?.Contains("BleeckerFeedColumn",StringComparison.Ordinal)==true)diagnostics.Add(args.Exception.ToString());};DebugSettings.IsXamlResourceReferenceTracingEnabled=true;DebugSettings.XamlResourceReferenceFailed+=(_,args)=>diagnostics.Add(args.Message);
        void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);if(isolationFailure is not null||window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||foreground==Environment.ProcessId)throw new InvalidOperationException(isolationFailure??"Hidden feed test became visible or owned foreground input.");}
        static void Assert(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        void Check(string id,Action action){Guard();try{action();checks.Add(new{id,passed=true});}catch(Exception error){failures.Add(id+": "+error.Message);checks.Add(new{id,passed=false,error=error.Message});}Guard();}
        static bool Flag(JsonElement spec,string key)=>spec.TryGetProperty(key,out var value)&&value.GetBoolean();
        static double Number(JsonElement spec,string key,double otherwise)=>spec.TryGetProperty(key,out var value)?value.GetDouble():otherwise;
        static object? Field(BleeckerFeedColumn column,string name)=>typeof(BleeckerFeedColumn).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(column);
        static SolidColorBrush Brush(byte r,byte g,byte b,byte alpha=255)=>new(Windows.UI.Color.FromArgb(alpha,r,g,b));
        try
        {
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();
            window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;isolationFailure="Hidden feed window activated.";}};window.VisibilityChanged+=(_,args)=>{if(args.Visible)isolationFailure="Hidden feed window became visible.";};
            var root=new Canvas{Width=900,Height=680,UseLayoutRounding=false};window.Content=root;Guard();
            async Task Layout(FrameworkElement element){for(var i=0;i<3;i++){root.Measure(new Size(900,680));root.Arrange(new Rect(0,0,900,680));root.UpdateLayout();if(element is Control control)control.ApplyTemplate();Guard();await Task.Delay(10);}Assert(element.IsLoaded,"Feed did not enter the real XAML tree.");Guard();}
            FrameworkElement Part(BleeckerFeedColumn column,string name)=>HiddenDesc<FrameworkElement>(column).Single(element=>element.Name==name);
            object Box(FrameworkElement element){var b=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
            static object Paint(Brush? brush)=>brush is SolidColorBrush solid?new{color=$"#{solid.Color.A:x2}{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}",opacity=solid.Opacity}:throw new InvalidOperationException("Expected solid feed paint.");
            object Snapshot(BleeckerFeedColumn column,JsonElement spec)
            {
                var title=Part(column,"FeedColumnTitle");var count=(Border)Part(column,"FeedColumnCount");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(column);var surface=(Border)Part(column,"PART_Surface");var header=(Border)Part(column,"FeedColumnHeader");var dot=(Border)Part(column,"FeedColumnDot");var viewport=column.Viewport!;
                var content=HiddenDesc<FrameworkElement>(column).SingleOrDefault(element=>element.Name=="ReferenceBody");var filter=HiddenDesc<FrameworkElement>(column).SingleOrDefault(element=>element.Name=="ReferenceFilter");
                return new{column=Box(column),header=Box(header),row=Box(Part(column,"FeedColumnRow")),dot=Box(dot),title=new{box=Box(title),text=HiddenDesc<TextBlock>(title).Single(text=>text.Name=="FeedColumnTitleText").Text},count=column.Count.HasValue?new{box=Box(count),text=HiddenDesc<TextBlock>(count).Single(text=>text.Name=="FeedColumnCountTextText").Text}:null,filter=filter is null?null:Box(filter),feed=Box(viewport),content=content is null?null:Box(content),scroll=new{offset=viewport.VerticalOffset,viewportHeight=viewport.ViewportHeight,extentHeight=viewport.ExtentHeight},border=Paint(surface.BorderBrush),headerBorder=Paint(header.BorderBrush),dotPaint=Paint(dot.Background),countBackground=Paint(count.Background),countBorder=Paint(count.BorderBrush),radius=surface.CornerRadius.TopLeft,name=peer.GetName(),role=peer.GetAutomationControlType().ToString(),tabStop=column.IsTabStop,loaded=column.IsLoaded,hostVisible=column.XamlRoot.IsHostVisible};
            }
            var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,"FeedColumn.json"));referenceHash=Convert.ToHexStringLower(SHA256.HashData(bytes));using var reference=JsonDocument.Parse(bytes);Assert(reference.RootElement.GetProperty("sourceSha256").GetString()=="981a086c1bbf1ccd1f98399cef9bd097992e4e50f2dcd27beae354beac0ac2a3","Unexpected feed source.");
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);var spec=row.GetProperty("spec");var column=new BleeckerFeedColumn{Title=spec.TryGetProperty("title",out var title)?title.GetString()!:"Live coverage",Height=Number(spec,"height",320),FlowDirection=Flag(spec,"rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                if(spec.TryGetProperty("width",out var width)){column.Width=width.GetDouble();column.MinWidth=width.GetDouble();}if(Flag(spec,"override")){column.Width=240;column.MinWidth=180;column.CornerRadius=new CornerRadius(4);}
                if(spec.TryGetProperty("count",out var count))column.Count=count.GetDouble();if(spec.TryGetProperty("countKind",out var kind))column.Count=kind.GetString()=="nan"?double.NaN:double.PositiveInfinity;
                if(spec.TryGetProperty("accent",out var accent))column.Accent=accent.GetString()!.StartsWith("rgba",StringComparison.Ordinal)?Brush(24,90,130,102):Brush(64,127,163);
                if(Number(spec,"filterHeight",0)>0)column.Filter=new Border{Name="ReferenceFilter",Height=Number(spec,"filterHeight",0),UseLayoutRounding=false};else if(spec.TryGetProperty("filter",out var filter))column.Filter=filter.ValueKind switch{JsonValueKind.Number=>filter.GetDouble(),JsonValueKind.String=>filter.GetString(),JsonValueKind.True=>true,JsonValueKind.False=>false,_=>null};
                if(!Flag(spec,"empty"))column.Content=spec.TryGetProperty("scalar",out var scalar)?scalar.GetString():new Border{Name="ReferenceBody",Height=Number(spec,"bodyHeight",600),UseLayoutRounding=false};
                Canvas.SetLeft(column,row.GetProperty("measurement").GetProperty("column").GetProperty("x").GetDouble());Canvas.SetTop(column,20);root.Children.Clear();root.Children.Add(column);await Layout(column);
                if(spec.TryGetProperty("offset",out var offset)){column.Viewport!.ChangeView(null,offset.GetDouble(),null,true);await Layout(column);}
                frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),measurement=Snapshot(column,spec)});root.Children.Remove(column);root.UpdateLayout();Guard();
            }
            Assert(frames.Count==154,"Incomplete hidden feed matrix.");
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=scheme;root.Children.Clear();var prefix=scheme+"/";var titleProducer=new TextBlock{Tag="Bound title"};var countProducer=new TextBlock{Tag=12d};var accent=Brush(64,127,163);var accentProducer=new TextBlock{Tag=accent};var filter=new TextBox{Text="Consumer filter",Height=32};var input=new TextBox{Text="Consumer body"};var body=new StackPanel{Height=600};body.Children.Add(input);var filterUnloads=0;var bodyUnloads=0;filter.Unloaded+=(_,_)=>filterUnloads++;body.Unloaded+=(_,_)=>bodyUnloads++;
                var column=new BleeckerFeedColumn{Height=320,Filter=filter,Content=body};Canvas.SetLeft(column,20);Canvas.SetTop(column,20);
                foreach(var(property,producer)in new[]{(BleeckerFeedColumn.TitleProperty,titleProducer),(BleeckerFeedColumn.CountProperty,countProducer),(BleeckerFeedColumn.AccentProperty,accentProducer)})column.SetBinding(property,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});root.Children.Add(column);await Layout(column);
                var peer=FrameworkElementAutomationPeer.CreatePeerForElement(column);Check(prefix+"default-peer",()=>Assert(peer.GetName()=="Bound title"&&peer.GetAutomationControlType()==AutomationControlType.Group&&!column.IsTabStop,"Default feed metadata differs."));
                input.Text="Edited body";filter.Text="Edited filter";var viewport=column.Viewport!;viewport.ChangeView(null,120,null,true);await Layout(column);var before=viewport.VerticalOffset;
                titleProducer.Tag="Updated title";countProducer.Tag=0d;accent.Color=Windows.UI.Color.FromArgb(255,184,74,80);await Layout(column);
                Check(prefix+"retained-controls-and-scroll",()=>Assert(ReferenceEquals(column.Content,body)&&ReferenceEquals(column.Filter,filter)&&ReferenceEquals(column.Viewport,viewport)&&input.Text=="Edited body"&&filter.Text=="Edited filter"&&bodyUnloads==0&&filterUnloads==0&&before==120&&viewport.VerticalOffset==120,"Header update replaced controls, edits or scroll state."));
                Check(prefix+"bound-header-and-accent",()=>Assert(column.Title=="Updated title"&&column.Count==0&&peer.GetName()=="Updated title"&&((SolidColorBrush)((Border)Part(column,"FeedColumnDot")).Background).Color.R==184&&new[]{BleeckerFeedColumn.TitleProperty,BleeckerFeedColumn.CountProperty,BleeckerFeedColumn.AccentProperty}.All(property=>column.GetBindingExpression(property)is not null),"Live header or accent ownership failed."));
                var nameProducer=new TextBlock{Tag="Caller feed"};column.SetBinding(AutomationProperties.NameProperty,new Binding{Source=nameProducer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});await Layout(column);Check(prefix+"name-binding",()=>Assert(peer.GetName()=="Caller feed","Caller name binding was replaced."));nameProducer.Tag="";await Layout(column);Check(prefix+"empty-name-binding",()=>Assert(peer.GetName()==""&&column.GetBindingExpression(AutomationProperties.NameProperty)is not null,"Empty caller name was replaced."));column.ClearValue(AutomationProperties.NameProperty);
                var originalStyle=column.Style;var baseStyle=new Style(typeof(BleeckerFeedColumn)){BasedOn=originalStyle};baseStyle.Setters.Add(new Setter(AutomationProperties.NameProperty,""));column.Style=new Style(typeof(BleeckerFeedColumn)){BasedOn=baseStyle};await Layout(column);peer.InvalidatePeer();Check(prefix+"empty-style-name",()=>Assert(peer.GetName()=="","Empty styled name was replaced."));column.Style=originalStyle;
                var label=new TextBlock{Text="External feed label"};root.Children.Add(label);AutomationProperties.SetLabeledBy(column,label);await Layout(column);Check(prefix+"caller-label",()=>Assert(peer.GetName()=="External feed label","LabeledBy did not name the feed."));column.ClearValue(AutomationProperties.LabeledByProperty);
                var model=new FeedColumnConsumerModel{Value="Original model"};var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Value}'/></DataTemplate>");column.ContentTemplate=template;column.Content=model;column.FilterTemplate=template;column.Filter=model;await Layout(column);Check(prefix+"original-template-model",()=>Assert(ReferenceEquals(column.Content,model)&&ReferenceEquals(column.Filter,model)&&HiddenDesc<ContentPresenter>(column).Count(presenter=>ReferenceEquals(presenter.Content,model))==2&&HiddenDesc<TextBlock>(column).Count(text=>text.Text=="Original model")==2,"Template did not receive original models."));model.Value="Changed model";await Layout(column);Check(prefix+"live-template-model",()=>Assert(HiddenDesc<TextBlock>(column).Count(text=>text.Text=="Changed model")==2,"Template model binding was lost."));
                var selector=new FeedColumnConsumerSelector(template);column.ContentTemplateSelector=selector;column.FilterTemplateSelector=selector;column.ContentTemplate=null;column.FilterTemplate=null;await Layout(column);Check(prefix+"template-selector-model",()=>Assert(selector.Models.Count>=2&&selector.Models.All(value=>ReferenceEquals(value,model))&&HiddenDesc<TextBlock>(column).Count(text=>text.Text=="Changed model")==2,"Selectors did not receive the original models."));
                column.Content=null;column.ContentTemplate=null;column.ContentTemplateSelector=null;column.Filter=null;column.FilterTemplate=null;column.FilterTemplateSelector=null;column.Content=body;column.Filter=filter;await Layout(column);var templateBefore=column.Template;column.Template=null;await Layout(column);Check(prefix+"template-detach",()=>Assert(column.Viewport is null&&Field(column,"shadow")is null&&Field(column,"glow")is null&&((ICollection)Field(column,"observers")!).Count==0,"Removed template retained resources."));column.Template=templateBefore;await Layout(column);Check(prefix+"template-reapply",()=>Assert(column.Viewport is not null&&!ReferenceEquals(column.Viewport,viewport)&&ReferenceEquals(column.Content,body)&&ReferenceEquals(column.Filter,filter)&&column.GetBindingExpression(BleeckerFeedColumn.TitleProperty)is not null,"Template reapply lost original slots or bindings."));
                root.Children.Remove(column);root.UpdateLayout();await Task.Delay(20);Guard();Check(prefix+"unload-observers",()=>Assert(((ICollection)Field(column,"observers")!).Count==0,"Unloaded feed retained brush observers."));titleProducer.Tag="Remounted title";root.Children.Add(column);await Layout(column);Check(prefix+"remount-binding",()=>Assert(peer.GetName()=="Remounted title"&&ReferenceEquals(column.Content,body)&&input.Text=="Edited body","Remount lost caller state."));root.Children.Clear();Guard();
            }
            completed=failures.Count==0;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var visible=window?.Visible??false;var nativeVisible=handle!=nint.Zero&&IsWindowVisible(handle);GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var isolated=!visible&&!nativeVisible&&activationEvents==0&&isolationFailure is null&&foreground!=Environment.ProcessId;var assembly=typeof(BleeckerFeedColumn).Assembly.Location;var librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(assembly)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed&&isolated,scope="hidden-feed-column-contracts",hiddenWindow=true,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,visible,nativeVisible,activationEvents,guardChecks=guards,isolated,processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),referenceSha256=referenceHash,librarySha256,frames,checks,failures,failure,diagnostics,isolationFailure,visibleRuntimeVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window?.Close();Environment.Exit(completed&&isolated?0:1);
        }
    }
    [Bindable]
    private sealed class FeedColumnConsumerModel : System.ComponentModel.INotifyPropertyChanged
    {
        private string value="";
        public string Value { get=>value;set { this.value=value;PropertyChanged?.Invoke(this,new(nameof(Value))); } }
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class FeedColumnConsumerSelector(DataTemplate template) : DataTemplateSelector
    {
        internal List<object> Models { get; }=[];
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container){if(item is not null)Models.Add(item);return template;}
    }
}
