using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;
public sealed partial class MainWindow
{
    private async Task VerifyEmptyAsync()
    {
        var editor=new TextBox{Text="Retained package state",Width=140};var empty=new BleeckerEmpty{Width=360,Title="No items",Action=editor};
        var host=new Border{Margin=new(32),Background=Root.Background,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Child=empty};Root.Children.Add(host);var passed=false;var phases=new List<object>();
        async Task PresentAsync(){Root.UpdateLayout();var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var frames=0;void Rendering(object? sender,object args){if(empty.IsLoaded&&empty.ActualWidth>0&&++frames>=2)completion.TrySetResult();}CompositionTarget.Rendering+=Rendering;try{await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Rendering;}}
        try
        {
            await PresentAsync();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(empty);
            Require(!empty.IsTabStop&&peer.GetAutomationControlType()==AutomationControlType.Group&&peer.GetPattern(PatternInterface.Invoke)is null,"Packaged Empty gained a command or Tab stop.");
            await AcquireConsumerInputAsync();Require(editor.Focus(FocusState.Keyboard),"Packaged Empty input refused focus.");
            foreach(var description in new string?[]{"Description text."," ","",null})
            {
                empty.Description=description;empty.Title="Updated title";await PresentAsync();
                Require(ReferenceEquals(empty.Action,editor)&&editor.Text=="Retained package state"&&ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot),editor),"Packaged Empty update replaced consumer state or focus.");phases.Add(new{description,retained=true});
            }
            empty.Action=0d;empty.Title="No items";await PresentAsync();Require(empty.ActualHeight==230,"Packaged Empty zero action was not absent.");
            empty.Action=true;await PresentAsync();Require(empty.ActualHeight==254,"Packaged Empty true action lost its empty wrapper spacing.");
            empty.Action=null;empty.Title="";await PresentAsync();Require(empty.ActualHeight==206,"Packaged Empty title retained an empty line box.");
            empty.Title="No items";empty.Description=" ";await PresentAsync();Require(empty.ActualHeight==236,"Packaged Empty whitespace description lost its spacing.");
            empty.Icon=false;await PresentAsync();Require(Equals(empty.Icon,false),"Packaged Empty replaced its false icon with the default.");
            var red=new SolidColorBrush(Microsoft.UI.Colors.Red);var icon=new Border{Width=24,Height=24,Background=red};empty.Icon=icon;await PresentAsync();Require(ReferenceEquals(empty.Icon,icon)&&ReferenceEquals(icon.Background,red),"Packaged Empty replaced a consumer icon or color.");
            phases.Add(new{presenceBoundaries=true,customIcon=true});passed=true;
        }
        finally{Root.Children.Remove(host);var args=Environment.GetCommandLineArgs();var output=args[Array.IndexOf(args,"--verify-output")+1];await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!,"consumer-empty-"+Root.RequestedTheme+".json"),JsonSerializer.Serialize(new{passed,phases}));}
    }
}
