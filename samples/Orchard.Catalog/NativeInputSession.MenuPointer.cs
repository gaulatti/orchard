using System.Text.Json;
using Microsoft.UI.Content;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    // MenuFlyoutItem can close its popup before XAML routes PointerReleased.
    // Observe the actual island's press/release without changing routed input.
    internal Task ClickMenuItemSourceAsync(FrameworkElement target,string id)=>PointerSourceClickAsync(target,id,false,null);
    // Calendar selection and clear actions can also unmount during release.
    internal Task ClickClosingSurfaceSourceAsync(FrameworkElement target,string id)=>PointerSourceClickAsync(target,id,false,null);

    internal async Task OpenContextPointerAsync(Control target,MenuFlyout menu,string id,Point position)
    {
        Require(!menu.IsOpen,"Pointer context gesture requires a closed menu.");
        await PointerSourceClickAsync(target,"context-"+id,true,position);
        var clock=System.Diagnostics.Stopwatch.StartNew();while(!menu.IsOpen&&clock.Elapsed<TimeSpan.FromSeconds(2))await MainWindow.AwaitPresentationAsync(target);
        Require(menu.IsOpen&&menu.IsConstrainedToRootBounds,"Right click did not open a constrained editor menu.");
    }

    private async Task PointerSourceClickAsync(FrameworkElement target,string id,bool rightButton,Point? position)
    {
        await MoveAsync(target,true,position);var expected=ReadPointerPosition();
        var island=ContentIsland.FindAllForCurrentThread().Single(item=>ReferenceEquals(item.Environment,root.ContentIslandEnvironment));
        Require(island.Environment.AppWindowId.Value==root.ContentIslandEnvironment.AppWindowId.Value,"Menu input island belongs to another window.");
        var source=InputPointerSource.GetForIsland(island)??throw new InvalidOperationException("Menu input source is unavailable.");
        var receipts=new List<object>();var order=new List<string>();var downs=0;var ups=0;uint? pointer=null;uint frame=0;ulong timestamp=0;var errors=new List<string>();
        var pressed=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Observe(string kind,InputPointerSource sender,Microsoft.UI.Input.PointerEventArgs args)
        {
            var point=args.CurrentPoint;var client=NativeScreenCapture.ClientBounds(Window);var x=client.X+point.Position.X*root.RasterizationScale;var y=client.Y+point.Position.Y*root.RasterizationScale;
            var exact=ReferenceEquals(sender,source)&&point.PointerDeviceType.ToString()=="Mouse"&&Math.Abs(x-expected.X)<.001&&Math.Abs(y-expected.Y)<.001&&OwnsForeground&&OwnsCurrentPoint;
            exact&=pointer is null||pointer==point.PointerId;pointer??=point.PointerId;exact&=point.FrameId>frame&&point.Timestamp>=timestamp;frame=point.FrameId;timestamp=point.Timestamp;
            exact&=rightButton?point.Properties.IsRightButtonPressed==(kind=="down"):kind=="down"?point.Properties.IsLeftButtonPressed&&point.IsInContact:!point.Properties.IsLeftButtonPressed&&!point.IsInContact;
            receipts.Add(new{kind,exactSource=ReferenceEquals(sender,source),point.PointerId,point.FrameId,point.Timestamp,x,y,point.IsInContact,point.Properties.IsLeftButtonPressed,point.Properties.IsRightButtonPressed,exact});order.Add(kind);
            if(!exact)errors.Add("Menu input lost source, frame, pointer, button or owned point.");
            if(kind=="down"){downs++;pressed.TrySetResult();}else{ups++;released.TrySetResult();}
        }
        var down=new TypedEventHandler<InputPointerSource,Microsoft.UI.Input.PointerEventArgs>((sender,args)=>Observe("down",sender,args));
        var up=new TypedEventHandler<InputPointerSource,Microsoft.UI.Input.PointerEventArgs>((sender,args)=>Observe("up",sender,args));
        source.PointerPressed+=down;source.PointerReleased+=up;
        try
        {
            try{Button(true);await pressed.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{Button(false);}
            await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(downs==1&&ups==1&&errors.Count==0&&order.SequenceEqual(new[]{"down","up"}),"Menu click did not retain one native source press/release pair.");
        }
        catch(Exception error){errors.Add(error.ToString());throw;}
        finally
        {
            source.PointerPressed-=down;source.PointerReleased-=up;
            await File.WriteAllTextAsync(Path.Combine(folder,$"{prefix}-menu-pointer-{id}.json"),JsonSerializer.Serialize(new{passed=errors.Count==0&&downs==1&&ups==1,layer="winui-input-pointer-source",routedXamlReleaseClaimed=false,target=target.Name,rightButton,downs,ups,order,receipts,errors,handlersDetached=true}));
        }
        void Button(bool down)
        {
            if(!rightButton){PointerButton(down);return;}
            Require(OwnsForeground&&OwnsCurrentPoint,"Right-button input lost its owned window or point.");
            Send([new NativeInput{Type=0,Data=new InputData{Mouse=new MouseInput{Flags=down?0x0008u:0x0010u}}}]);
        }
    }
}
