using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;

namespace Orchard.Catalog;
public sealed partial class AccordionsFixture:UserControl
{
    internal static Border Body(string text)=>new(){Padding=new Thickness(16),Child=new TextBlock{Text=text,FontFamily=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"],FontSize=14,LineHeight=20,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,TextWrapping=TextWrapping.Wrap}};
    internal static ObservableCollection<BleeckerAccordionItem> Items()=>[new("first","Delivery schedule",Body("Daily updates arrive each morning.")){Subtitle="Choose when updates arrive."},new("second","Reader preferences",Body("Your choices stay with this view."))];
    public AccordionsFixture()
    {
        InitializeComponent();Example.Items=Items();var edit=new Button{Content="Edit",Style=(Style)Application.Current.Resources["BleeckerSecondaryButtonStyle"]};edit.Click+=(_,_)=>ActionStatus.Text="Edit invoked without changing expansion";Example.Items[0].Actions=edit;
        Disabled.Items=Items();
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        var root=(Grid)XamlRoot.Content;var host=new StackPanel{Width=320,Spacing=12,Margin=new Thickness(20),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Background=new SolidColorBrush(scheme==BleeckerColorScheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33))};Grid.SetRowSpan(host,root.RowDefinitions.Count);Grid.SetColumnSpan(host,root.ColumnDefinitions.Count);root.Children.Add(host);
        var before=new Button{Content="Before accordion"};var group=new BleeckerAccordion{Items=Items(),DefaultExpandedId="first"};var after=new Button{Content="After accordion"};var action=new Button{Content="Edit section"};var actions=0;action.Click+=(_,_)=>actions++;group.Items[0].Actions=action;var content=new TextBox{Text="Retained input",Margin=new Thickness(16)};group.Items[0].Content=content;
        host.Children.Add(before);host.Children.Add(group);host.Children.Add(after);var phases=new List<string>();object? templateEvidence=null;var passed=false;using var input=new NativeInputSession(XamlRoot,folder,"accordion-"+scheme);
        IExpandCollapseProvider Expand(Button button)=>(IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.ExpandCollapse);
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        try
        {
            await MainWindow.AwaitPresentationAsync(host);await input.AcquireAsync(before);var first=group.HeaderButtons[0];var second=group.HeaderButtons[1];var firstPeer=FrameworkElementAutomationPeer.CreatePeerForElement(first);
            Require(group.ExpandedId=="first"&&Expand(first).ExpandCollapseState==ExpandCollapseState.Expanded&&Expand(second).ExpandCollapseState==ExpandCollapseState.Collapsed,"Accordion initial expansion differs.");
            Require(firstPeer.GetName()=="Delivery schedule Choose when updates arrive.","Accordion accessible name lost actual title/subtitle.");phases.Add("initial-and-name");
            Require(second.Focus(FocusState.Keyboard),"Accordion header refused focus.");await input.KeyAsync(0x20,second);await MainWindow.AwaitPresentationAsync(group);Require(group.ExpandedId=="second"&&Expand(first).ExpandCollapseState==ExpandCollapseState.Collapsed,"Accordion Space failed single expansion.");phases.Add("space-switch");
            Expand(second).Collapse();Require(group.ExpandedId is null,"Accordion automation could not collapse.");Expand(first).Expand();await MainWindow.AwaitPresentationAsync(group);await input.ClickAsync(action);Require(actions==1&&group.ExpandedId=="first","Accordion action toggled expansion.");phases.Add("independent-action");
            Require(before.Focus(FocusState.Keyboard),"Accordion predecessor refused focus.");await input.HeldKeyAsync(0x09,before,()=>first,()=>MainWindow.AwaitPresentationAsync(group));Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),first),"Accordion Tab did not reach its first header.");await input.KeyAsync(0x0D,first);await MainWindow.AwaitPresentationAsync(group);Require(group.ExpandedId is null,"Accordion Enter did not collapse.");phases.Add("tab-enter");
            Expand(first).Expand();await MainWindow.AwaitPresentationAsync(group);Require(content.Focus(FocusState.Keyboard),"Accordion content refused focus.");Expand(first).Collapse();await MainWindow.AwaitPresentationAsync(group);Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),first),"Accordion collapse lost focus from its hidden content.");Expand(first).Expand();await MainWindow.AwaitPresentationAsync(group);Require(content.Text=="Retained input","Accordion recreated consumer content.");phases.Add("content-retention-and-focus");
            var item=group.Items[0];item.Title="Updated schedule";item.Subtitle="Updated details";group.Items=new(group.Items);await MainWindow.AwaitPresentationAsync(group);Require(ReferenceEquals(group.HeaderButtons[0],first)&&firstPeer.GetName()=="Updated schedule Updated details","Accordion replacement lost identity or live metadata.");group.DefaultExpandedId="second";Require(group.ExpandedId=="first","Accordion changed state when only its default changed.");phases.Add("live-model-and-default");
            group.Items.Move(0,1);await MainWindow.AwaitPresentationAsync(group);Require(ReferenceEquals(group.HeaderButtons[1],first),"Accordion keyed reorder recreated its header.");group.Items.RemoveAt(1);await MainWindow.AwaitPresentationAsync(group);Require(group.ExpandedId=="first"&&Expand(group.HeaderButtons[0]).ExpandCollapseState==ExpandCollapseState.Collapsed,"Accordion removal invented replacement expansion.");var rejected=false;try{Expand(first).Expand();}catch(ElementNotAvailableException){rejected=true;}Require(rejected,"Accordion stale peer remained actionable.");group.Items.Insert(0,item);await MainWindow.AwaitPresentationAsync(group);Require(group.ExpandedId=="first"&&Expand(group.HeaderButtons[0]).ExpandCollapseState==ExpandCollapseState.Expanded&&content.Text=="Retained input","Accordion reinsert failed retained ID/content.");phases.Add("keyed-remove-reinsert");
            var template=group.Template;group.Template=null;group.ApplyTemplate();group.Template=template;group.ApplyTemplate();await MainWindow.AwaitPresentationAsync(group);Require(content.Text=="Retained input"&&group.ExpandedId=="first","Accordion template replacement reset consumer content/state.");host.Children.Remove(group);await MainWindow.AwaitPresentationAsync(host);group.Items.Add(new("third","Third section",Body("Added while unmounted")));host.Children.Insert(1,group);await MainWindow.AwaitPresentationAsync(group);Require(group.HeaderButtons.Count==3&&group.ExpandedId=="first","Accordion remount missed unmounted data changes.");phases.Add("template-and-remount");
            group.IsEnabled=false;rejected=false;try{Expand(group.HeaderButtons[1]).Expand();}catch(ElementNotEnabledException){rejected=true;}Require(rejected&&group.ExpandedId=="first","Disabled accordion expanded.");group.IsEnabled=true;phases.Add("disabled-guard");
            var original=group.Items;rejected=false;try{group.Items=[new("same","A",null),new("same","B",null)];}catch(ArgumentException){rejected=true;}Require(rejected&&ReferenceEquals(original,group.Items),"Accordion duplicate IDs were accepted or changed consumer state.");phases.Add("duplicate-id-error");
            DataTemplate Template(string child)=>(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>"+child+"</DataTemplate>");
            var titleData=new BleeckerAccordionItem("title","Template title",null);var subtitleData=new BleeckerAccordionItem("subtitle","Template subtitle",null);var bodyData=new BleeckerAccordionItem("body","Bound content",null);var command=new AccordionCommand();var actionData=new BleeckerAccordionItem("action","Template action",command);
            var templated=new BleeckerAccordion{DefaultExpandedId="template",TitleTemplate=Template("<TextBlock Text='{Binding Title}'/>"),SubtitleTemplate=Template("<TextBlock Text='{Binding Title}'/>"),ContentTemplate=Template("<TextBox Text='{Binding Title, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}' Margin='16'/>"),ActionsTemplate=Template("<Button Name='TemplateAction' Content='{Binding Title}' Command='{Binding Content}'/>"),Items=[new("template",titleData,bodyData){Subtitle=subtitleData,Actions=actionData}]};host.Children.Add(templated);await MainWindow.AwaitPresentationAsync(templated);
            var templateInitialName=FrameworkElementAutomationPeer.CreatePeerForElement(templated.HeaderButtons[0]).GetName();Require(templateInitialName=="Template title Template subtitle","Accordion templates exposed model names instead of rendered text.");var templateAction=MainWindow.Desc<Button>(templated).Single(button=>button.Name=="TemplateAction");((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(templateAction).GetPattern(PatternInterface.Invoke)).Invoke();Require(command.Count==1&&templated.ExpandedId=="template","Accordion action template lost its consumer command or toggled state.");var editable=MainWindow.Desc<TextBox>(templated).Single();Require(editable.Text=="Bound content"&&editable.Focus(FocusState.Keyboard),"Accordion content template lost binding/focus.");editable.SelectAll();await input.KeyAsync(0x41,editable);Require(editable.Text!="Bound content"&&Equals(bodyData.Title,editable.Text),"Accordion content template did not write native input to the bound consumer.");titleData.Title="Live template title";await MainWindow.AwaitPresentationAsync(templated);var templateFinalName=FrameworkElementAutomationPeer.CreatePeerForElement(templated.HeaderButtons[0]).GetName();Require(templateFinalName=="Live template title Template subtitle","Accordion title template lost live consumer binding.");templateEvidence=new{beforeName=templateInitialName,afterName=templateFinalName,contentText=editable.Text,modelValue=bodyData.Title,commandInvocations=command.Count,expandedId=templated.ExpandedId};phases.Add("data-templates");await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,"accordion-runtime-"+scheme+".png"));passed=true;
        }
        finally{root.Children.Remove(host);await File.WriteAllTextAsync(Path.Combine(folder,"accordion-runtime-"+scheme+".json"),JsonSerializer.Serialize(new{passed,phases,actions,templateEvidence,defaultIsInitialOnly=true,contentRetained=true,stalePeerRejected=true,disabledRejected=true}));}
    }
    private sealed class AccordionCommand:System.Windows.Input.ICommand
    {
        internal int Count {get;private set;}
        public bool CanExecute(object? parameter)=>true;
        public void Execute(object? parameter)=>Count++;
        public event EventHandler? CanExecuteChanged {add{} remove{}}
    }
}
