using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;
public partial class App
{
    private async void RunHiddenSelectsAsync(string referenceDirectory)
    {
        var arguments=Environment.GetCommandLineArgs();
        var output=arguments[Array.IndexOf(arguments,"--verify-output")+1];
        nint handle=0;
        var frames=new List<object>();var histories=new List<object>();var checks=new List<object>();
        var failures=new List<string>();var diagnostics=new List<string>();
        var completed=false;var guards=0;var activationEvents=0;
        string? failure=null,isolationFailure=null,referenceHash=null;
        DebugSettings.IsXamlResourceReferenceTracingEnabled=true;
        DebugSettings.XamlResourceReferenceFailed+=(_,args)=>diagnostics.Add(args.Message);
        void Guard()
        {
            guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);
            if(isolationFailure is not null||window!.Visible||window.AppWindow.IsVisible||IsWindowVisible(handle)||foreground==Environment.ProcessId)
                throw new InvalidOperationException(isolationFailure??"Hidden Select became visible or owned foreground input.");
        }
        static void Assert(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        void Check(string id,Action action)
        {
            Guard();try{action();checks.Add(new{id,passed=true});}
            catch(Exception error){failures.Add(id+": "+error.Message);checks.Add(new{id,passed=false,error=error.Message});}Guard();
        }
        static bool Flag(JsonElement spec,string key)=>spec.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.True;
        static string Text(JsonElement spec,string key,string otherwise)=>spec.TryGetProperty(key,out var value)?value.GetString()!:otherwise;
        static object? Field(object instance,string name)=>instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)?.GetValue(instance);
        static ItemsControl Surface(BleeckerSelect owner)=>(ItemsControl)typeof(BleeckerSelect).GetField("select",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner)!;
        static Button Item(BleeckerSelect owner,string value)=>Surface(owner).Items.OfType<Button>().Last(item=>((BleeckerSelectOption)item.Tag).Value==value);
        static ISelectionItemProvider Option(Button item)=>(ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(item).GetPattern(PatternInterface.SelectionItem);
        static AutomationPeer Peer(BleeckerSelect owner)=>FrameworkElementAutomationPeer.CreatePeerForElement(Surface(owner));
        static string Display(BleeckerSelect owner)=>((IValueProvider)Peer(owner).GetPattern(PatternInterface.Value)).Value;
        static Binding Bind(object producer,string path="Tag")=>new(){Source=producer,Path=new PropertyPath(path),Mode=BindingMode.OneWay};
        static ObservableCollection<BleeckerSelectOption> DefaultOptions()=>[new("a","Library Alpha"),new("b","Library Beta"),new("disabled","Disabled library",true)];
        try
        {
            window=new Window();handle=WinRT.Interop.WindowNative.GetWindowHandle(window);Guard();
            window.Activated+=(_,args)=>{if(args.WindowActivationState!=WindowActivationState.Deactivated){activationEvents++;isolationFailure="Hidden Select window activated.";}};
            window.VisibilityChanged+=(_,args)=>{if(args.Visible)isolationFailure="Hidden Select window became visible.";};
            var root=new Canvas{Width=900,Height=760,UseLayoutRounding=false};window.Content=root;Guard();
            async Task Layout(FrameworkElement owner)
            {
                for(var iteration=0;iteration<3;iteration++)
                {
                    root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();if(owner is Control control)control.ApplyTemplate();Guard();await Task.Delay(10);
                }
                Assert(owner.IsLoaded,"Select fixture did not enter the real XAML tree.");
                if(owner is BleeckerSelect select)Assert(Surface(select).IsLoaded&&Surface(select).FocusState==FocusState.Unfocused&&Field(Surface(select),"positionedPopup")is Popup{IsOpen:false},"Hidden Select focused or opened its popup.");Guard();
            }
            FrameworkElement Part(BleeckerSelect owner,string name)=>HiddenDesc<FrameworkElement>(Surface(owner)).Single(element=>element.Name==name);
            object Box(FrameworkElement element){var rect=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=rect.X,y=rect.Y,width=rect.Width,height=rect.Height};}
            static object Paint(Brush brush)=>brush is SolidColorBrush solid?new{color=$"#{solid.Color.A:x2}{solid.Color.R:x2}{solid.Color.G:x2}{solid.Color.B:x2}",opacity=solid.Opacity}:throw new InvalidOperationException("Expected solid Select paint.");
            object Snapshot(BleeckerSelect owner)
            {
                var surface=Surface(owner);var trigger=(Border)Part(owner,"Trigger");var peer=Peer(owner);var value=Part(owner,"SelectValue");var icon=HiddenDesc<FrameworkElement>(owner).SingleOrDefault(element=>element.Name=="ReferenceIcon");
                return new{trigger=Box(trigger),value=new{box=Box(value),text=Display(owner)},icon=icon is null?null:Box(icon),arrow=Box(Part(owner,"SelectArrow")),paint=new{background=Paint(trigger.Background),border=Paint(trigger.BorderBrush),color=Paint(surface.Foreground),opacity=Part(owner,"SelectRoot").Opacity,radius=trigger.CornerRadius.TopLeft},fontSize=surface.FontSize,lineHeight=HiddenDesc<TextBlock>(value).Single(text=>text.Name=="SelectValueText").LineHeight,padding=new[]{surface.Padding.Left,surface.Padding.Top,surface.Padding.Right,surface.Padding.Bottom},gap=((Grid)Part(owner,"TriggerLayout")).ColumnDefinitions[3].Width.Value,name=peer.GetName(),required=peer.IsRequiredForForm(),error=!peer.IsDataValidForForm(),disabled=!surface.IsEnabled,expanded=((IExpandCollapseProvider)peer.GetPattern(PatternInterface.ExpandCollapse)).ExpandCollapseState.ToString(),focused=surface.FocusState.ToString(),role=peer.GetAutomationControlType().ToString(),loaded=owner.IsLoaded,hostVisible=owner.XamlRoot.IsHostVisible,rawValue=owner.Value,optionCount=surface.Items.Count};
            }
            var bytes=await File.ReadAllBytesAsync(Path.Combine(referenceDirectory,"Select.json"));referenceHash=Convert.ToHexStringLower(SHA256.HashData(bytes));using var reference=JsonDocument.Parse(bytes);
            Assert(reference.RootElement.GetProperty("sourceSha256").GetString()=="874ad668945afb6f045d881447a275f73b9dc84ffbf56f4beed02a83f8d3fecd"&&reference.RootElement.GetProperty("headerSourceSha256").GetString()=="848685cb40ba19fdcf8991b0332f98f447b8bd2af6e7febcfc7c27e9517d8f2e","Unexpected Select source.");
            foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                root.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                var spec=row.GetProperty("spec");var header=Flag(spec,"header");BleeckerSelect owner=header?new BleeckerHeaderSelect():new BleeckerSelect();
                if(!header){owner.Width=spec.GetProperty("width").GetDouble();owner.Size=Text(spec,"size","md")switch{"sm"=>BleeckerControlSize.Small,"lg"=>BleeckerControlSize.Large,_=>BleeckerControlSize.Medium};owner.Error=Flag(spec,"error");}
                owner.Value=Text(spec,"value","");owner.IsEnabled=!Flag(spec,"disabled");owner.Required=Flag(spec,"required");owner.AutoFocus=Flag(spec,"autoFocus");owner.FlowDirection=Flag(spec,"rtl")?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
                if(spec.TryGetProperty("placeholder",out var placeholder))owner.Placeholder=placeholder.GetString()!;
                if(spec.TryGetProperty("name",out var name))AutomationProperties.SetName(owner,name.GetString()!);
                owner.Options=spec.TryGetProperty("options",out var options)?options.EnumerateArray().Select(option=>new BleeckerSelectOption(option.GetProperty("value").GetString()!,option.GetProperty("label").GetString()!,Flag(option,"disabled"))).ToArray():DefaultOptions();
                if(spec.TryGetProperty("icon",out var icon))owner.StartIcon=icon.ValueKind switch{JsonValueKind.True=>new Border{Name="ReferenceIcon",Width=16,Height=16,UseLayoutRounding=false},JsonValueKind.String=>icon.GetString(),JsonValueKind.Number=>icon.GetDouble(),_=>null};
                Canvas.SetLeft(owner,row.GetProperty("measurement").GetProperty("trigger").GetProperty("x").GetDouble());Canvas.SetTop(owner,20);root.Children.Clear();root.Children.Add(owner);await Layout(owner);
                frames.Add(new{id=row.GetProperty("id").GetString(),scheme=row.GetProperty("scheme").GetString(),measurement=Snapshot(owner)});root.Children.Remove(owner);root.UpdateLayout();Guard();
            }
            Assert(frames.Count==292,"Incomplete Select closed matrix.");
            foreach(var row in reference.RootElement.GetProperty("interactions").EnumerateArray())
            {
                var header=row.GetProperty("header").GetBoolean();var accept=row.GetProperty("accept").GetBoolean();root.RequestedTheme=Enum.Parse<ElementTheme>(row.GetProperty("scheme").GetString()!);
                BleeckerSelect owner=header?new BleeckerHeaderSelect():new BleeckerSelect{Width=240};var producer=new TextBlock{Tag=""};owner.Options=DefaultOptions();owner.SetBinding(BleeckerSelect.ValueProperty,Bind(producer));var requests=new List<string>();owner.ValueRequested+=(_,args)=>{requests.Add(args.Value);if(accept)producer.Tag=args.Value;};
                root.Children.Clear();root.Children.Add(owner);Canvas.SetLeft(owner,20);Canvas.SetTop(owner,20);await Layout(owner);Option(Item(owner,"b")).Select();await Layout(owner);var first=Display(owner);producer.Tag="a";await Layout(owner);var controlled=Display(owner);producer.Tag="";await Layout(owner);var released=Display(owner);
                histories.Add(new{scheme=row.GetProperty("scheme").GetString(),header,accept,first,controlled,released,requests,bindingRetained=owner.GetBindingExpression(BleeckerSelect.ValueProperty)is not null,rawValue=owner.Value,popupOpen=((Popup)Field(Surface(owner),"positionedPopup")!).IsOpen,focus=Surface(owner).FocusState.ToString()});root.Children.Clear();root.UpdateLayout();Guard();
            }
            foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=scheme;var prefix=scheme+"/";var options=DefaultOptions();var producer=new TextBlock{Tag="a"};var optionProducer=new TextBlock{Tag=options};var icon=new Border{Width=16,Height=16};var owner=new BleeckerSelect{Width=240,StartIcon=icon};owner.SetBinding(BleeckerSelect.ValueProperty,Bind(producer));owner.SetBinding(BleeckerSelect.OptionsProperty,Bind(optionProducer));var requests=new List<BleeckerSelectRequestedEventArgs>();var accept=false;owner.ValueRequested+=(_,args)=>{requests.Add(args);if(accept)producer.Tag=args.Value;};root.Children.Clear();root.Children.Add(owner);await Layout(owner);
                var originalSurface=Surface(owner);var originalB=Item(owner,"b");var peer=Peer(owner);var valueProvider=(IValueProvider)peer.GetPattern(PatternInterface.Value);
                Check(prefix+"default-peer",()=>Assert(peer.GetName()=="Select…"&&peer.GetAutomationControlType()==AutomationControlType.ComboBox&&valueProvider.IsReadOnly&&!owner.IsTabStop&&originalSurface.IsTabStop,"Default Select metadata differs."));
                Option(originalB).Select();await Layout(owner);
                Check(prefix+"controlled-reject",()=>Assert(requests.Count==1&&requests[0].Value=="b"&&ReferenceEquals(requests[0].Option,options[1])&&owner.Value=="a"&&Display(owner)=="Library Alpha"&&ReferenceEquals(Item(owner,"b"),originalB),"Rejected request changed caller state or original option identity."));
                accept=true;Option(originalB).Select();await Layout(owner);
                Check(prefix+"controlled-accept-binding",()=>Assert(requests.Count==2&&owner.Value=="b"&&Display(owner)=="Library Beta"&&owner.GetBindingExpression(BleeckerSelect.ValueProperty)is not null&&owner.GetBindingExpression(BleeckerSelect.OptionsProperty)is not null&&ReferenceEquals(owner.Options,options)&&ReferenceEquals(owner.StartIcon,icon),"Accepted request replaced caller bindings or objects."));
                Check(prefix+"readonly-value",()=>{try{valueProvider.SetValue("injected");throw new Exception("SetValue accepted arbitrary text.");}catch(InvalidOperationException){Assert(owner.Value=="b","Read-only provider changed raw value.");}});
                Check(prefix+"disabled-option",()=>{try{Option(Item(owner,"disabled")).Select();throw new Exception("Disabled option accepted a request.");}catch(ElementNotEnabledException){Assert(requests.Count==2,"Disabled option emitted a request.");}});
                owner.IsEnabled=false;await Layout(owner);
                Check(prefix+"disabled-control",()=>{try{Option(Item(owner,"a")).Select();throw new Exception("Disabled owner accepted a request.");}catch(ElementNotEnabledException){Assert(requests.Count==2,"Disabled owner emitted a request.");}});owner.IsEnabled=true;
                options[1].Label="Changed original Beta";await Layout(owner);
                Check(prefix+"live-option",()=>Assert(Display(owner)=="Changed original Beta"&&ReferenceEquals(Item(owner,"b"),originalB),"Original item notification did not update the existing view."));
                var replacement=new BleeckerSelectOption("b","Replacement Beta");options[1]=replacement;await Layout(owner);
                Check(prefix+"keyed-option-replacement",()=>Assert(ReferenceEquals(Item(owner,"b"),originalB)&&ReferenceEquals(originalB.Tag,replacement)&&Display(owner)=="Replacement Beta","Equal-key original replacement discarded its option view."));
                options.Remove(replacement);await Layout(owner);
                Check(prefix+"stale-option",()=>{try{Option(originalB).Select();throw new Exception("Removed option accepted a request.");}catch(ElementNotAvailableException){Assert(requests.Count==2&&Display(owner)=="","Removed option emitted a request or fabricated a label.");}});
                options.Add(replacement);await Layout(owner);
                var nameProducer=new TextBlock{Tag="Caller select"};owner.SetBinding(AutomationProperties.NameProperty,Bind(nameProducer));AutomationProperties.SetHelpText(owner,"Caller help");AutomationProperties.SetAutomationId(owner,"caller-select");owner.Required=true;owner.Error=true;await Layout(owner);peer.InvalidatePeer();
                Check(prefix+"caller-metadata",()=>Assert(peer.GetName()=="Caller select"&&peer.GetHelpText()=="Caller help"&&peer.GetAutomationId()=="caller-select"&&peer.IsRequiredForForm()&&!peer.IsDataValidForForm(),"Caller metadata did not reach the native input peer."));
                var description=new TextBlock{Text="Caller select description"};root.Children.Add(description);AutomationProperties.GetDescribedBy(owner).Add(description);await Layout(owner);
                Check(prefix+"caller-description",()=>Assert(((IEnumerable<AutomationPeer>)Peer(owner).GetType().GetMethod("GetDescribedByCore",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(Peer(owner),null)!).Single().GetName()=="Caller select description","Caller description did not reach the native input peer."));
                nameProducer.Tag="";await Layout(owner);peer.InvalidatePeer();Check(prefix+"empty-name-binding",()=>Assert(peer.GetName()==""&&owner.GetBindingExpression(AutomationProperties.NameProperty)is not null,"Empty bound name was replaced."));owner.ClearValue(AutomationProperties.NameProperty);
                var oldStyle=owner.Style;var baseStyle=new Style(typeof(BleeckerSelect)){BasedOn=oldStyle};baseStyle.Setters.Add(new Setter(AutomationProperties.NameProperty,""));owner.Style=new Style(typeof(BleeckerSelect)){BasedOn=baseStyle};await Layout(owner);peer.InvalidatePeer();Check(prefix+"empty-style-name",()=>Assert(peer.GetName()=="","Empty styled name was replaced."));owner.Style=oldStyle;
                var label=new TextBlock{Text="External select label"};root.Children.Add(label);AutomationProperties.SetLabeledBy(owner,label);await Layout(owner);peer.InvalidatePeer();Check(prefix+"caller-label",()=>Assert(peer.GetName()=="External select label"&&ReferenceEquals(peer.GetLabeledBy(),FrameworkElementAutomationPeer.CreatePeerForElement(label)),"Caller label was lost."));owner.ClearValue(AutomationProperties.LabeledByProperty);
                owner.Size=BleeckerControlSize.Large;owner.Padding=new Thickness(8,0,10,0);owner.FontSize=18;await Layout(owner);
                Check(prefix+"caller-geometry",()=>Assert(Surface(owner).FontSize==18&&Surface(owner).Padding.Left==8&&Surface(owner).Padding.Right==10&&owner.ActualHeight==44,$"Caller font/padding or size recipe was lost: {Surface(owner).FontSize}, {Surface(owner).Padding}, {owner.ActualHeight}."));
                var model=new SelectConsumerModel{Value="Icon model"};var template=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding Value}'/></DataTemplate>");owner.StartIconTemplate=template;owner.StartIcon=model;await Layout(owner);
                Check(prefix+"original-template-model",()=>Assert(ReferenceEquals(owner.StartIcon,model)&&HiddenDesc<ContentPresenter>(owner).Any(p=>ReferenceEquals(p.Content,model))&&HiddenDesc<TextBlock>(owner).Any(text=>text.Text=="Icon model"),"Icon template lost original model."));model.Value="Changed icon";await Layout(owner);Check(prefix+"live-template-model",()=>Assert(HiddenDesc<TextBlock>(owner).Any(text=>text.Text=="Changed icon"),"Icon template binding was lost."));
                var selector=new SelectConsumerSelector(template);owner.StartIconTemplateSelector=selector;owner.StartIconTemplate=null;await Layout(owner);Check(prefix+"template-selector-model",()=>Assert(selector.Models.Count>0&&selector.Models.All(item=>ReferenceEquals(item,model)),"Icon selector did not receive original model."));
                owner.StartIcon=icon;owner.StartIconTemplateSelector=null;await Layout(owner);var oldTemplate=owner.Template;owner.Template=null;await Task.Delay(20);root.UpdateLayout();Guard();
                Check(prefix+"template-detach",()=>Assert(typeof(BleeckerSelect).GetField("select",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner)is null,"Removed template retained its chooser."));owner.Template=oldTemplate;await Layout(owner);
                Check(prefix+"template-reapply",()=>Assert(!ReferenceEquals(Surface(owner),originalSurface)&&ReferenceEquals(owner.StartIcon,icon)&&ReferenceEquals(owner.Options,options)&&owner.GetBindingExpression(BleeckerSelect.ValueProperty)is not null&&Display(owner)=="Replacement Beta","Template reapply lost original state."));
                root.Children.Remove(owner);root.UpdateLayout();await Task.Delay(20);Guard();
                Check(prefix+"unload-observers",()=>Assert(((ICollection)typeof(BleeckerSelect).GetField("observedItems",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner)!).Count==0&&typeof(BleeckerSelect).GetField("observedOptions",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner)is null,"Unloaded Select retained original producers."));
                producer.Tag="a";root.Children.Add(owner);await Layout(owner);Check(prefix+"remount-binding",()=>Assert(Display(owner)=="Library Alpha"&&ReferenceEquals(owner.StartIcon,icon)&&ReferenceEquals(owner.Options,options),"Remount lost caller state."));
                owner.AutoFocus=true;await Layout(owner);Check(prefix+"late-autofocus",()=>Assert(Surface(owner).FocusState==FocusState.Unfocused,"Late autofocus changed native focus."));root.Children.Clear();root.UpdateLayout();Guard();
                var table=new SelectLegacyTable();var legacyOptions=DefaultOptions();var filter=new BleeckerDataTableSelectFilter{Width=240,Table=table,ColumnId="library",Options=legacyOptions};var pagination=new BleeckerDataTablePagination{Width=700,Table=table};root.Children.Add(filter);root.Children.Add(pagination);Canvas.SetTop(pagination,100);await Layout(filter);await Layout(pagination);
                var filterSurface=(ItemsControl)Field(filter,"select")!;var pageSurface=(ItemsControl)Field(pagination,"size")!;
                static IValueProvider LegacyValue(ItemsControl surface)=>(IValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(surface).GetPattern(PatternInterface.Value);
                Button FilterItem(string value)=>filterSurface.Items.OfType<Button>().Last(item=>((BleeckerSelectOption)item.Tag).Value==value);
                bool LegacyClosed(ItemsControl surface)=>surface.FocusState==FocusState.Unfocused&&Field(surface,"positionedPopup")is Popup{IsOpen:false}&&surface.GetType().GetProperty("Appearance",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(surface)is null&&surface.GetType().GetProperty("RequestSelection",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(surface)is null;
                Check(prefix+"legacy-filter-style",()=>Assert(LegacyClosed(filterSurface)&&filterSurface.ActualWidth==176&&filterSurface.ActualHeight==40&&filterSurface.FontSize==14&&filterSurface.Padding.Left==14&&LegacyValue(filterSurface).Value=="Library Alpha"&&FrameworkElementAutomationPeer.CreatePeerForElement(filterSurface).GetName()=="All","Existing filter appearance or metadata changed."));
                var legacyBeta=FilterItem("b");Option(legacyBeta).Select();await Layout(filter);Check(prefix+"legacy-filter-reject",()=>Assert(table.FilterRequests.SequenceEqual(new object?[]{"b"})&&Equals(table.FilterValue,"a")&&LegacyValue(filterSurface).Value=="Library Alpha"&&ReferenceEquals(FilterItem("b"),legacyBeta),"Existing controlled filter rejection changed."));
                table.Accept=true;Option(legacyBeta).Select();await Layout(filter);Check(prefix+"legacy-filter-accept",()=>Assert(Equals(table.FilterValue,"b")&&LegacyValue(filterSurface).Value=="Library Beta"&&ReferenceEquals(filter.Options,legacyOptions)&&ReferenceEquals(legacyBeta.Tag,legacyOptions[1])&&LegacyClosed(filterSurface),"Existing filter no longer retains original options or accepted state."));
                Option(FilterItem("")).Select();await Layout(filter);Check(prefix+"legacy-filter-empty",()=>Assert(table.FilterValue is null&&table.FilterRequests.Count==3&&table.FilterRequests[^1]is null&&LegacyValue(filterSurface).Value=="All","Existing empty filter boundary changed."));
                Check(prefix+"legacy-pagination-style",()=>Assert(LegacyClosed(pageSurface)&&pageSurface.ActualWidth==80&&pageSurface.ActualHeight==40&&pageSurface.FontSize==14&&LegacyValue(pageSurface).Value=="10"&&FrameworkElementAutomationPeer.CreatePeerForElement(pageSurface).GetName()=="Rows per page","Existing pagination appearance or metadata changed."));
                Option(pageSurface.Items.OfType<Button>().Single(item=>(int)item.Tag==50)).Select();await Layout(pagination);Check(prefix+"legacy-pagination-request",()=>Assert(table.PageSize==50&&table.PageRequests.SequenceEqual(new[]{50})&&LegacyValue(pageSurface).Value=="50"&&LegacyClosed(pageSurface),"Existing pagination no longer crosses its original producer boundary."));root.Children.Clear();root.UpdateLayout();Guard();
            }
            completed=failures.Count==0;
        }
        catch(Exception error){failure=error.ToString();}
        finally
        {
            var visible=window?.Visible??false;var nativeVisible=handle!=0&&IsWindowVisible(handle);GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var isolated=!visible&&!nativeVisible&&activationEvents==0&&isolationFailure is null&&foreground!=Environment.ProcessId;var assembly=typeof(BleeckerSelect).Assembly.Location;var librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(assembly)));
            await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=completed&&isolated,scope="hidden-select-contracts",hiddenWindow=true,activateCalled=false,showCalled=false,focusCalled=false,inputSent=false,visible,nativeVisible,activationEvents,guardChecks=guards,isolated,processArchitecture=RuntimeInformation.ProcessArchitecture.ToString(),osArchitecture=RuntimeInformation.OSArchitecture.ToString(),referenceSha256=referenceHash,librarySha256,frames,histories,checks,failures,failure,diagnostics,isolationFailure,visibleRuntimeVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false},new JsonSerializerOptions{WriteIndented=true}));window?.Close();Environment.Exit(completed&&isolated?0:1);
        }
    }
    [Bindable]
    private sealed class SelectConsumerModel:System.ComponentModel.INotifyPropertyChanged
    {
        private string value="";public string Value{get=>value;set{this.value=value;PropertyChanged?.Invoke(this,new(nameof(Value)));}}public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class SelectConsumerSelector(DataTemplate template):DataTemplateSelector
    {
        internal List<object> Models{get;}=[];
        protected override DataTemplate SelectTemplateCore(object item,DependencyObject container){if(item is not null)Models.Add(item);return template;}
    }
    private sealed class SelectLegacyTable:IBleeckerDataTableFilterSource,IBleeckerDataTableFilterColumn,IBleeckerDataTablePaginationSource
    {
        public bool Accept{get;set;}public object? FilterValue{get;private set;}="a";
        public List<object?> FilterRequests{get;}=[];public List<int> PageRequests{get;}=[];
        public event EventHandler? Changed;
        public int ColumnFilterCount=>FilterValue is null?0:1;public object? GlobalFilter=>null;
        public IBleeckerDataTableFilterColumn? GetColumn(string id)=>id=="library"?this:null;
        public object? GetFilterValue()=>FilterValue;
        public void SetFilterValue(object? value){FilterRequests.Add(value);if(Accept){FilterValue=value;Changed?.Invoke(this,EventArgs.Empty);}}
        public void ResetColumnFilters()=>SetFilterValue(null);
        public int PageIndex{get;private set;}public int PageSize{get;private set;}=10;
        public int PageCount=>10;public int FilteredRowCount=>100;public int FilteredSelectedRowCount=>0;
        public bool CanPreviousPage=>PageIndex>0;public bool CanNextPage=>PageIndex<PageCount-1;
        public void SetPageIndex(int value){PageIndex=value;Changed?.Invoke(this,EventArgs.Empty);}
        public void SetPageSize(int value){PageRequests.Add(value);PageSize=value;PageIndex=0;Changed?.Invoke(this,EventArgs.Empty);}
        public void PreviousPage()=>SetPageIndex(PageIndex-1);public void NextPage()=>SetPageIndex(PageIndex+1);
    }
}
