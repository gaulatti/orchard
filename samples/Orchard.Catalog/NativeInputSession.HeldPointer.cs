using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Diagnostics;
using System.Text.Json;
using Windows.Foundation;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    private readonly List<object> heldPointerActions=[];
    internal async Task HeldPointerAsync(FrameworkElement target,Func<Task> whileHeld,Point? localPoint=null)
    {
        await MoveAsync(target,true,localPoint);var expected=ReadPointerPosition();var clock=Stopwatch.StartNew();var receipts=new List<object>();
        var down=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var up=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Receipt(PointerRoutedEventArgs args,bool pressed)
        {
            var point=args.GetCurrentPoint(root.Content);var client=NativeScreenCapture.ClientBounds(Window);var x=client.X+point.Position.X*root.RasterizationScale;var y=client.Y+point.Position.Y*root.RasterizationScale;
            var owned=false;for(var owner=args.OriginalSource as DependencyObject;owner is not null;owner=VisualTreeHelper.GetParent(owner))if(ReferenceEquals(owner,target)){owned=true;break;}
            var exact=owned&&!args.IsGenerated&&args.Pointer.PointerDeviceType==Microsoft.UI.Input.PointerDeviceType.Mouse&&point.Properties.IsLeftButtonPressed==pressed&&Math.Abs(x-expected.X)<.001&&Math.Abs(y-expected.Y)<.001;
            receipts.Add(new{pressed,owned,args.IsGenerated,x,y,exact,milliseconds=clock.Elapsed.TotalMilliseconds});if(exact)(pressed?down:up).TrySetResult();
        }
        var pressedHandler=new PointerEventHandler((_,args)=>Receipt(args,true));var releasedHandler=new PointerEventHandler((_,args)=>Receipt(args,false));
        root.Content.AddHandler(UIElement.PointerPressedEvent,pressedHandler,true);root.Content.AddHandler(UIElement.PointerReleasedEvent,releasedHandler,true);var sent=false;
        try{await PressPointerAsync();sent=true;await down.Task.WaitAsync(TimeSpan.FromSeconds(2));await whileHeld();}
        finally
        {
            try{if(sent){PointerButton(false);await up.Task.WaitAsync(TimeSpan.FromSeconds(2));}}
            finally{root.Content.RemoveHandler(UIElement.PointerPressedEvent,pressedHandler);root.Content.RemoveHandler(UIElement.PointerReleasedEvent,releasedHandler);heldPointerActions.Add(new{target=target.Name,expected,downDelivered=down.Task.IsCompletedSuccessfully,upDelivered=up.Task.IsCompletedSuccessfully,receipts});await File.WriteAllTextAsync(Path.Combine(folder,prefix+"-held-pointer.json"),JsonSerializer.Serialize(heldPointerActions,new JsonSerializerOptions{WriteIndented=true}));}
        }
    }
}
