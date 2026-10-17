using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    // Inject the actual inspection key as soon as native Tab changes focus,
    // before the asynchronous GotFocus notification can initialize the chart.
    internal async Task EnterWithInspectionKeyAsync(Control preceding,Control target,ushort key)
    {
        await RequireForegroundAsync();Require(preceding.Focus(FocusState.Keyboard),"Preceding focus-entry action refused focus.");
        var downs=0;var ups=0;var targetRetained=true;var focusEvents=0;var injected=false;bool? focusEventAtPreview=null;
        var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Focused(object sender,RoutedEventArgs args)=>focusEvents++;
        var preview=new KeyEventHandler((_,args)=>{if((ushort)args.Key==key)focusEventAtPreview=focusEvents>0;});
        var down=new KeyEventHandler((_,args)=>{if((ushort)args.Key==key){downs++;targetRetained&=ReferenceEquals(args.OriginalSource,target)&&ReferenceEquals(FocusManager.GetFocusedElement(root),target);}});
        var up=new KeyEventHandler((_,args)=>{if((ushort)args.Key==key){ups++;targetRetained&=ReferenceEquals(args.OriginalSource,target)&&ReferenceEquals(FocusManager.GetFocusedElement(root),target);released.TrySetResult();}});
        target.GotFocus+=Focused;target.AddHandler(UIElement.PreviewKeyDownEvent,preview,true);root.Content.AddHandler(UIElement.KeyDownEvent,down,true);root.Content.AddHandler(UIElement.KeyUpEvent,up,true);
        var token=target.RegisterPropertyChangedCallback(Control.FocusStateProperty,(_,_)=>
        {
            if(injected||target.FocusState!=FocusState.Keyboard)return;
            injected=true;Key(key);
        });
        try
        {
            Key(0x09);await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(injected&&downs==1&&ups==1&&targetRetained&&focusEventAtPreview==false,"Native early inspection did not cross its deferred-focus boundary.");
        }
        finally
        {
            target.UnregisterPropertyChangedCallback(Control.FocusStateProperty,token);target.GotFocus-=Focused;target.RemoveHandler(UIElement.PreviewKeyDownEvent,preview);root.Content.RemoveHandler(UIElement.KeyDownEvent,down);root.Content.RemoveHandler(UIElement.KeyUpEvent,up);
            keyActions.Add(new{key,target=target.Name,downs,ups,targetRetained});
            await File.WriteAllTextAsync(Path.Combine(folder,$"{prefix}-focus-entry.json"),JsonSerializer.Serialize(new{injected,key,downs,ups,targetRetained,focusEventAtPreview,handlersDetached=true}));
        }
    }
}
