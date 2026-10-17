using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Orchard.Catalog;

public sealed partial class RadioGroupsFixture:UserControl
{
    internal static ObservableCollection<BleeckerRadioOption> Options()=>[new("daily","Daily","A morning digest."),new("weekly","Weekly","A weekly overview."),new("paused","Paused",Disabled:true)];
    public RadioGroupsFixture()
    {
        InitializeComponent();
        foreach(var group in new[]{Vertical,Horizontal,Disabled,Declined})group.Options=Options();
        Vertical.ValueRequested+=(_,args)=>{Vertical.Value=args.Value;SelectionStatus.Text="Selected: "+args.Value;};
        Horizontal.ValueRequested+=(_,args)=>Horizontal.Value=args.Value;
    }
    internal async Task VerifyAsync(BleeckerColorScheme scheme,string folder)
    {
        var root=(Grid)XamlRoot.Content;var host=new StackPanel{Margin=new Thickness(20),Spacing=12,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Width=280};Grid.SetRowSpan(host,root.RowDefinitions.Count);Grid.SetColumnSpan(host,root.ColumnDefinitions.Count);root.Children.Add(host);
        var before=new Button{Content="Before radio"};var group=new BleeckerRadioGroup{Options=Options(),Value="daily"};var after=new Button{Content="After radio"};host.Children.Add(before);host.Children.Add(group);host.Children.Add(after);
        using var input=new NativeInputSession(XamlRoot,folder,"radio-"+scheme);var requests=new List<string>();var accept=true;
        group.ValueRequested+=(_,args)=>{requests.Add(args.Value);if(accept)group.Value=args.Value;};
        try
        {
            await MainWindow.AwaitPresentationAsync(host);await input.AcquireAsync(before);
            // Establish real receipts on two distinct owned targets. A previous
            // process may have left the cursor at this fixture's first target.
            await input.MoveAsync(after);await input.MoveAsync(before);
            Require(before.Focus(FocusState.Keyboard),"Radio predecessor refused focus.");await input.HeldKeyAsync(0x09,before,()=>group.OptionButtons[0],()=>MainWindow.AwaitPresentationAsync(group));
            Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),group.OptionButtons[0]),"Tab did not enter selected radio.");
            await input.HeldKeyAsync(0x28,group.OptionButtons[0],()=>group.OptionButtons[1],()=>MainWindow.AwaitPresentationAsync(group));Require(group.Value=="weekly"&&requests.SequenceEqual(new[]{"weekly"}),"Arrow selection did not reach the consumer.");
            await input.KeyAsync(0x28,group.OptionButtons[1]);Require(requests.Count==1&&ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),group.OptionButtons[1]),"Radio boundary wrapped or reached a disabled option.");
            await input.HeldKeyAsync(0x09,group.OptionButtons[1],()=>after,()=>MainWindow.AwaitPresentationAsync(group));Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),after),"Tab did not leave radio group.");
            accept=false;await input.ClickAsync(group.OptionButtons[0]);Require(group.Value=="weekly"&&requests.Count==2&&requests[^1]=="daily","Declined pointer request changed the selected value.");
            var peer=FrameworkElementAutomationPeer.CreatePeerForElement(group);var selection=(ISelectionProvider)peer.GetPattern(PatternInterface.Selection);
            Require(!selection.CanSelectMultiple&&!selection.IsSelectionRequired&&selection.GetSelection().Length==1,"Radio selection automation differs.");
            var itemPeer=FrameworkElementAutomationPeer.CreatePeerForElement(group.OptionButtons[0]);var item=(ISelectionItemProvider)itemPeer.GetPattern(PatternInterface.SelectionItem);
            Require(itemPeer.GetAutomationControlType()==AutomationControlType.RadioButton&&itemPeer.GetName()=="Daily"&&itemPeer.GetHelpText()=="A morning digest.","Radio automation lost role, label or description.");
            item.Select();Require(!item.IsSelected&&group.Value=="weekly"&&requests.Count==3,"Declined automation request changed selection.");
            accept=true;item.Select();Require(item.IsSelected&&group.Value=="daily"&&requests.Count==4,"Accepted automation request did not update selection.");
            var disabled=(ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(group.OptionButtons[2]).GetPattern(PatternInterface.SelectionItem);var rejected=false;try{disabled.Select();}catch(ElementNotEnabledException){rejected=true;}Require(rejected&&requests.Count==4,"Disabled automation produced a request.");
            var retained=group.OptionButtons[0];group.Options.Move(0,1);await MainWindow.AwaitPresentationAsync(group);Require(ReferenceEquals(retained,group.OptionButtons[1])&&group.Value=="daily","Reordering replaced keyed radio identity or value.");
            group.Options[1]=new("daily","Updated daily","A morning digest.");Require(FrameworkElementAutomationPeer.CreatePeerForElement(retained).GetName()=="Updated daily","Option replacement did not update automation.");
            group.Options.RemoveAt(1);await MainWindow.AwaitPresentationAsync(group);Require(group.Value=="daily"&&selection.GetSelection().Length==0,"Removing a selected option invented a replacement value.");
            rejected=false;try{item.Select();}catch(ElementNotAvailableException){rejected=true;}Require(rejected&&requests.Count==4,"Removed radio peer remained actionable.");
            var template=group.Template;group.Template=null;group.ApplyTemplate();group.Template=template;group.ApplyTemplate();await MainWindow.AwaitPresentationAsync(group);Require(group.Options.Count==2&&group.Value=="daily","Template replacement changed consumer data.");
            host.Children.Remove(group);await MainWindow.AwaitPresentationAsync(host);group.Options.Add(new("new","New option"));host.Children.Insert(1,group);await MainWindow.AwaitPresentationAsync(group);Require(group.OptionButtons.Count==3,"Remount failed to reconcile unmounted data changes.");
            group.Value="new";group.Orientation=Orientation.Horizontal;await MainWindow.AwaitPresentationAsync(group);
            Require(before.Focus(FocusState.Keyboard),"Radio predecessor refused focus after remount.");
            await input.HeldKeyAsync(0x09,before,()=>group.OptionButtons[2],()=>MainWindow.AwaitPresentationAsync(group));
            Require(ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),group.OptionButtons[2]),"Tab did not enter the non-first selected radio.");
            await input.HeldKeyAsync(0x24,group.OptionButtons[2],()=>group.OptionButtons[0],()=>MainWindow.AwaitPresentationAsync(group));
            Require(group.Value=="weekly"&&requests.Count==5,"Home did not select the first enabled radio.");
            await input.HeldKeyAsync(0x23,group.OptionButtons[0],()=>group.OptionButtons[2],()=>MainWindow.AwaitPresentationAsync(group));
            Require(group.Value=="new"&&requests.Count==6,"End did not select the last enabled radio.");
            await input.HeldKeyAsync(0x25,group.OptionButtons[2],()=>group.OptionButtons[0],()=>MainWindow.AwaitPresentationAsync(group),0x11);
            Require(group.Value=="new"&&requests.Count==6,"Ctrl+arrow changed consumer selection.");
            await input.KeyAsync(0x20,group.OptionButtons[0]);Require(group.Value=="weekly"&&requests.Count==7,"Space did not select the focused radio.");
            await input.KeyAsync(0x20,group.OptionButtons[0]);Require(requests.Count==7,"Space requested an already selected value.");
            await input.HeldKeyAsync(0x09,group.OptionButtons[0],()=>after,()=>MainWindow.AwaitPresentationAsync(group));
            await input.HeldKeyAsync(0x09,after,()=>group.OptionButtons[0],()=>MainWindow.AwaitPresentationAsync(group),0x10);
            Require(group.Value=="weekly"&&requests.Count==7,"Reverse Tab changed selection or missed the selected radio.");
            var selected=MainWindow.Desc<Microsoft.UI.Xaml.Controls.TextBlock>(group.OptionButtons[0]).Single(text=>text.Name=="OnProbe");var palette=(Microsoft.UI.Xaml.Media.SolidColorBrush)selected.Foreground;var saved=palette.Color;
            try{palette.Color=Microsoft.UI.Colors.Magenta;await MainWindow.AwaitPresentationAsync(group);var stroke=(Microsoft.UI.Xaml.Media.SolidColorBrush)MainWindow.Desc<Microsoft.UI.Xaml.Shapes.Ellipse>(group.OptionButtons[0]).Single(shape=>shape.Name=="RadioRing").Stroke;Require(stroke.Color==palette.Color,"Live palette mutation did not reach the radio stroke.");}
            finally{palette.Color=saved;}
            await MainWindow.AwaitPresentationAsync(group);await NativeScreenCapture.CaptureClientAsync(input.Window,System.IO.Path.Combine(folder,"radio-runtime-"+scheme+".png"));
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"radio-runtime-"+scheme+".json"),JsonSerializer.Serialize(new{passed=true,requests,consumerControlled=true,disabledRejected=true,stalePeerRejected=true,keyedReorder=true,remount=true,templateReplacement=true,homeEnd=true,controlArrow=true,space=true,reverseTab=true,nonFirstSelectionEntry=true,livePalette=true}));
        }
        finally{await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"radio-final-state-"+scheme+".json"),JsonSerializer.Serialize(new{group.Value,requests,focused=FocusManager.GetFocusedElement(XamlRoot)?.GetType().FullName,buttons=group.OptionButtons.Select(button=>new{button.IsEnabled,button.IsTabStop,button.FocusState}).ToArray()}));root.Children.Remove(host);}
    }
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
