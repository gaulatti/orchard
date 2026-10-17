using System.ComponentModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Ellipse=Microsoft.UI.Xaml.Shapes.Ellipse;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyRadioGroupsAsync()
    {
        var state=new ConsumerRadioState();var requests=new List<string>();var keys=new List<object>();var keyedLifecycle=new List<object>();var accept=true;var passed=false;
        var group=new BleeckerRadioGroup { Width=280,Options=new(){new("daily","Daily","A morning digest."),new("weekly","Weekly","A weekly overview."),new("paused","Paused",Disabled:true)} };
        group.SetBinding(BleeckerRadioGroup.ValueProperty,new Binding{Source=state,Path=new PropertyPath(nameof(ConsumerRadioState.Value)),Mode=BindingMode.OneWay});
        group.ValueRequested+=(_,request)=>{requests.Add(request.Value);if(accept)state.Value=request.Value;};
        AutomationProperties.SetName(group,"Packaged delivery frequency");
        var before=new Button{Content="Before packaged radio"};var after=new Button{Content="After packaged radio"};
        var layout=new StackPanel{Width=280,Spacing=12};layout.Children.Add(before);layout.Children.Add(group);layout.Children.Add(after);
        var host=new Border{Margin=new Thickness(32),Background=Root.Background,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Child=layout};Root.Children.Add(host);
        try
        {
            Root.UpdateLayout();await AcquireConsumerInputAsync();
            var options=BrandDescendants<Button>(group).ToArray();Require(options.Length==3&&group.Value=="daily","Packaged radio lost its options or initial binding.");
            var selection=(ISelectionProvider)FrameworkElementAutomationPeer.CreatePeerForElement(group).GetPattern(PatternInterface.Selection);
            Require(selection.GetSelection().Length==1&&!selection.CanSelectMultiple&&!selection.IsSelectionRequired,"Packaged radio lost single-selection semantics.");
            var dailyPeer=FrameworkElementAutomationPeer.CreatePeerForElement(options[0]);var daily=(ISelectionItemProvider)dailyPeer.GetPattern(PatternInterface.SelectionItem);
            Require(dailyPeer.GetAutomationControlType()==AutomationControlType.RadioButton&&dailyPeer.GetName()=="Daily"&&dailyPeer.GetHelpText()=="A morning digest.","Packaged radio lost its role, name or help text.");
            var label=BrandDescendants<TextBlock>(options[0]).Single(text=>text.Name=="RadioLabel");var ring=BrandDescendants<Ellipse>(options[0]).Single(shape=>shape.Name=="RadioRing");
            Require(label.FontFamily.Source.Contains("EncodeSans-Medium.ttf")&&label.FontSize==14&&label.LineHeight==20&&ring.ActualWidth==18&&ring.StrokeThickness==2,"Packaged radio lost bundled typography or ring geometry.");
            Require(before.Focus(FocusState.Keyboard),"Packaged radio predecessor refused focus.");
            keys.Add(await PaginationConsumerKeyAsync(0x09,before,options[0],()=>Require(daily.IsSelected,"Packaged Tab entry changed selection.")));
            keys.Add(await PaginationConsumerKeyAsync(0x28,options[0],options[1],()=>Require(state.Value=="weekly"&&requests.Count==1,"Packaged arrow did not update the bound consumer.")));
            keys.Add(await PaginationConsumerKeyAsync(0x09,options[1],after,()=>Require(state.Value=="weekly","Packaged Tab exit changed selection.")));
            accept=false;Require(options[0].Focus(FocusState.Keyboard),"Packaged radio refused focus.");
            keys.Add(await PaginationConsumerKeyAsync(0x20,options[0],options[0],()=>Require(requests.Count==1,"Packaged radio requested before Space release.")));
            Require(state.Value=="weekly"&&group.Value=="weekly"&&requests.Count==2&&!daily.IsSelected,"Packaged declined Space changed bound state.");
            daily.Select();Require(requests.Count==3&&!daily.IsSelected,"Packaged declined automation changed bound state.");
            accept=true;daily.Select();Require(requests.Count==4&&daily.IsSelected&&state.Value=="daily","Packaged accepted automation did not reach bound state.");
            state.Value="weekly";Root.UpdateLayout();Require(!daily.IsSelected&&group.Value=="weekly","Packaged model changes did not reach selection.");
            var weekly=options[1];var loads=0;var unloads=0;weekly.Loaded+=(_,_)=>loads++;weekly.Unloaded+=(_,_)=>unloads++;
            async Task PresentRadioAsync()
            {
                var ready=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var frames=0;
                void Rendering(object? sender,object args){if(group.IsLoaded&&group.ActualWidth>0&&group.ActualHeight>0&&++frames>=2)ready.TrySetResult();}
                CompositionTarget.Rendering+=Rendering;try{await ready.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Rendering;}
            }
            async Task CheckLifecycle(string kind,Action mutation,int expectedLoads)
            {
                loads=0;unloads=0;mutation();Root.UpdateLayout();await PresentRadioAsync();var current=BrandDescendants<Button>(group).Single(button=>FrameworkElementAutomationPeer.CreatePeerForElement(button).GetName()=="Weekly");
                var selected=((ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(current).GetPattern(PatternInterface.SelectionItem)).IsSelected;
                Require(ReferenceEquals(current,weekly)&&loads==expectedLoads&&unloads==expectedLoads&&selected&&state.Value=="weekly"&&requests.Count==4,"Packaged keyed lifecycle differs for "+kind);
                keyedLifecycle.Add(new{kind,sameNode=ReferenceEquals(current,weekly),loads,unloads,selected});
            }
            await CheckLifecycle("move-last-to-first",()=>group.Options.Move(1,0),0);
            await CheckLifecycle("move-first-to-last",()=>group.Options.Move(0,1),1);
            await CheckLifecycle("replace-collection",()=>group.Options=new(group.Options.Select(option=>new BleeckerRadioOption(option.Value,option.Label,option.Description,option.Disabled))),0);
            await CheckLifecycle("orientation",()=>group.Orientation=Orientation.Horizontal,0);
            group.Options.Move(1,0);Root.UpdateLayout();Require(ReferenceEquals(BrandDescendants<Button>(group).First(),weekly),"Packaged reordering lost keyed option identity.");
            group.Options[0]=new("weekly","Updated weekly","Updated help");Root.UpdateLayout();Require(FrameworkElementAutomationPeer.CreatePeerForElement(weekly).GetName()=="Updated weekly","Packaged immutable option replacement lost live metadata.");
            var stale=(ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(weekly).GetPattern(PatternInterface.SelectionItem);group.Options.RemoveAt(0);Root.UpdateLayout();
            Require(state.Value=="weekly"&&selection.GetSelection().Length==0,"Packaged selected removal invented replacement state.");
            var rejected=false;try{stale.Select();}catch(ElementNotAvailableException){rejected=true;}Require(rejected&&requests.Count==4,"Packaged stale peer remained actionable.");
            group.IsEnabled=false;rejected=false;try{daily.Select();}catch(ElementNotEnabledException){rejected=true;}Require(rejected&&requests.Count==4,"Packaged disabled group requested a value.");
            passed=true;
        }
        finally
        {
            Root.Children.Remove(host);var args=Environment.GetCommandLineArgs();var report=args[Array.IndexOf(args,"--verify-output")+1];
            await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(report)!,"consumer-radio-"+Root.RequestedTheme+".json"),JsonSerializer.Serialize(new{passed,requests,keys,keyedLifecycle,binding=true,keyedIdentity=true,stalePeerRejected=true,disabledRejected=true}));
        }
    }
    private sealed class ConsumerRadioState:INotifyPropertyChanged
    {
        private string value="daily";
        public string Value{get=>value;set{if(this.value==value)return;this.value=value;PropertyChanged?.Invoke(this,new(nameof(Value)));}}
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
