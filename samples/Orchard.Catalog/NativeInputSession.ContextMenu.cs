using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    internal async Task OpenContextKeyAsync(Control target,MenuFlyout menu,string id,bool shiftF10,bool expectOpen=true,bool expectConstrained=true)
    {
        await RequireForegroundAsync();Require(ReferenceEquals(FocusManager.GetFocusedElement(root),target)&&!menu.IsOpen,"Context key requires its focused editor and closed menu.");
        var island=ContentIsland.FindAllForCurrentThread().Single(item=>ReferenceEquals(item.Environment,root.ContentIslandEnvironment));
        var source=InputKeyboardSource.GetForIsland(island)??throw new InvalidOperationException("Context keyboard source is unavailable.");
        var key=shiftF10?(ushort)0x79:(ushort)0x5D;var keys=shiftF10?new ushort[]{0x10,key}:new[]{key};var counts=keys.ToDictionary(k=>k,_=>new int[2]);var receipts=new List<object>();var errors=new List<string>();
        var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Observe(string kind,InputKeyboardSource sender,Microsoft.UI.Input.KeyEventArgs args)
        {
            var actual=(ushort)args.VirtualKey;if(!counts.TryGetValue(actual,out var count))return;var up=kind.EndsWith("up",StringComparison.Ordinal);
            var exact=ReferenceEquals(sender,source)&&OwnsForeground&&args.KeyStatus.RepeatCount==1&&args.KeyStatus.IsKeyReleased==up;
            count[up?1:0]++;receipts.Add(new{kind,key=actual,exactSource=ReferenceEquals(sender,source),args.KeyStatus.ScanCode,args.KeyStatus.RepeatCount,args.KeyStatus.WasKeyDown,args.KeyStatus.IsKeyReleased,exact});
            if(!exact)errors.Add("Context key lost its source, foreground or native key status.");if(up&&actual==(shiftF10?0x10:key))released.TrySetResult();
        }
        var down=new TypedEventHandler<InputKeyboardSource,Microsoft.UI.Input.KeyEventArgs>((sender,args)=>Observe("down",sender,args));var up=new TypedEventHandler<InputKeyboardSource,Microsoft.UI.Input.KeyEventArgs>((sender,args)=>Observe("up",sender,args));
        var systemDown=new TypedEventHandler<InputKeyboardSource,Microsoft.UI.Input.KeyEventArgs>((sender,args)=>Observe("system-down",sender,args));var systemUp=new TypedEventHandler<InputKeyboardSource,Microsoft.UI.Input.KeyEventArgs>((sender,args)=>Observe("system-up",sender,args));
        source.KeyDown+=down;source.KeyUp+=up;source.SystemKeyDown+=systemDown;source.SystemKeyUp+=systemUp;
        try
        {
            if(shiftF10)ShiftKey(key);else Key(key);await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(errors.Count==0&&counts.Values.All(c=>c[0]==1&&c[1]==1),"Context key did not retain one native down/up per key.");
            var clock=System.Diagnostics.Stopwatch.StartNew();while(expectOpen&&!menu.IsOpen&&clock.Elapsed<TimeSpan.FromSeconds(2))await MainWindow.AwaitPresentationAsync(target);
            await MainWindow.AwaitPresentationAsync(target);
            Require(menu.IsOpen==expectOpen,"The real context key produced the wrong menu visibility.");
            if(expectOpen&&expectConstrained)Require(menu.IsConstrainedToRootBounds,"The real context menu escaped its XAML root.");
        }
        catch(Exception error){errors.Add(error.ToString());throw;}
        finally
        {
            source.KeyDown-=down;source.KeyUp-=up;source.SystemKeyDown-=systemDown;source.SystemKeyUp-=systemUp;
            await File.WriteAllTextAsync(Path.Combine(folder,$"{prefix}-context-key-{id}.json"),JsonSerializer.Serialize(new{passed=errors.Count==0&&menu.IsOpen==expectOpen,expectOpen,expectConstrained,gesture=shiftF10?"Shift+F10":"Apps",layer="winui-input-keyboard-source",routedTargetRetentionClaimed=false,target=target.Name,counts,receipts,errors,handlersDetached=true}));
        }
    }
}
