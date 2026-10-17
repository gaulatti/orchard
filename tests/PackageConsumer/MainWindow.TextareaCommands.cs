using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyTextareaContextAsync()
    {
        Require(PackageTextarea.ContextFlyout is null&&PackageTextarea.ReadLocalValue(UIElement.ContextFlyoutProperty)==DependencyProperty.UnsetValue,"Packaged Textarea inherited the stock rich-text context flyout.");
        var menu=PackageTextarea.SelectionFlyout as BleeckerDropdownMenu??throw new InvalidOperationException("Packaged Textarea lost its branded commands.");
        Require(menu.Items.Count==6,"Packaged Textarea command set differs.");
        var phases=new List<object>();
        await OpenAsync("default",true);menu.Hide();await AwaitTextareaPresentationAsync();
        PackageTextarea.ContextFlyout=null;await OpenAsync("local-null",false);
        Require(!VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Any(),"Packaged local null opened a native context popup.");
        PackageTextarea.ClearValue(UIElement.ContextFlyoutProperty);
        Require(PackageTextarea.ContextFlyout is null&&PackageTextarea.ReadLocalValue(UIElement.ContextFlyoutProperty)==DependencyProperty.UnsetValue,"Packaged context reset did not clear the local override.");
        await OpenAsync("restored",true);menu.Hide();await AwaitTextareaPresentationAsync();
        var arguments=Environment.GetCommandLineArgs();var report=arguments[Array.IndexOf(arguments,"--verify-output")+1];
        await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(report)!,$"consumer-textarea-context-{Root.RequestedTheme}.json"),JsonSerializer.Serialize(new{passed=true,phases}));

        async Task OpenAsync(string phase,bool expected)
        {
            await AcquireConsumerInputAsync();Require(PackageTextarea.Focus(FocusState.Keyboard),"Packaged Textarea context owner refused focus.");
            var root=Root.XamlRoot;var island=ContentIsland.FindAllForCurrentThread().Single(item=>ReferenceEquals(item.Environment,root.ContentIslandEnvironment));
            var source=InputKeyboardSource.GetForIsland(island)??throw new InvalidOperationException("Packaged context source unavailable.");
            var downs=0;var ups=0;var exact=true;
            void Observe(InputKeyboardSource sender,Microsoft.UI.Input.KeyEventArgs args,bool released)
            {
                if((ushort)args.VirtualKey!=0x5D)return;
                exact&=ReferenceEquals(sender,source)&&args.KeyStatus.RepeatCount==1&&args.KeyStatus.IsKeyReleased==released&&GetForegroundWindow()==WinRT.Interop.WindowNative.GetWindowHandle(this);
                if(released)ups++;else downs++;
            }
            var down=new TypedEventHandler<InputKeyboardSource,Microsoft.UI.Input.KeyEventArgs>((sender,args)=>Observe(sender,args,false));var up=new TypedEventHandler<InputKeyboardSource,Microsoft.UI.Input.KeyEventArgs>((sender,args)=>Observe(sender,args,true));
            source.KeyDown+=down;source.KeyUp+=up;
            try
            {
                var keys=new[]{new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x5D}}},new ConsumerInput{Type=1,Data=new ConsumerInputData{Keyboard=new ConsumerKeyboard{VirtualKey=0x5D,Flags=2}}}};
                Require(SendInput(2,keys,Marshal.SizeOf<ConsumerInput>())==2,"Packaged context key did not enter native input.");
                await AwaitAsync(()=>downs==1&&ups==1,"Packaged context key lost its native source press/release.");await AwaitTextareaPresentationAsync();
                Require(exact&&menu.IsOpen==expected&&(!expected||menu.IsConstrainedToRootBounds),"Packaged context key lost ownership, visibility or root constraint.");
            }
            finally{source.KeyDown-=down;source.KeyUp-=up;phases.Add(new{phase,expected,downs,ups,exact,isOpen=menu.IsOpen,constrained=menu.IsConstrainedToRootBounds,handlersDetached=true});menu.Hide();}
        }
    }
}
