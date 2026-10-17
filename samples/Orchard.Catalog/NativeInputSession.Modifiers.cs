using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Orchard.Catalog;

internal sealed partial class NativeInputSession
{
    internal Task ModifiedKeyAsync(ushort modifier,ushort key,FrameworkElement target)=>ModifiedKeyCoreAsync(modifier,key,target,false);
    internal Task ModifiedPreviewKeyAsync(ushort modifier,ushort key,FrameworkElement target)=>ModifiedKeyCoreAsync(modifier,key,target,true);
    private async Task ModifiedKeyCoreAsync(ushort modifier,ushort key,FrameworkElement target,bool preview)
    {
        await RequireForegroundAsync();
        Require(ReferenceEquals(FocusManager.GetFocusedElement(root),target),"Modified keyboard target does not own XAML focus.");
        var released=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var downs=0;var ups=0;var modifierDowns=0;var modifierUps=0;var targetRetained=true;
        var down=new KeyEventHandler((_,args)=>
        {
            if((ushort)args.Key!=key&&(ushort)args.Key!=modifier)return;
            targetRetained&=ReferenceEquals(args.OriginalSource,target);if((ushort)args.Key==key)downs++;else modifierDowns++;
        });
        var up=new KeyEventHandler((_,args)=>
        {
            if((ushort)args.Key!=key&&(ushort)args.Key!=modifier)return;
            targetRetained&=ReferenceEquals(args.OriginalSource,target);if((ushort)args.Key==key)ups++;else{modifierUps++;released.TrySetResult();}
        });
        var downEvent=preview?UIElement.PreviewKeyDownEvent:UIElement.KeyDownEvent;
        root.Content.AddHandler(downEvent,down,true);root.Content.AddHandler(UIElement.KeyUpEvent,up,true);
        try
        {
            Send([new NativeInput{Type=1,Data=new InputData{Keyboard=new KeyboardInput{Key=modifier}}},
                new NativeInput{Type=1,Data=new InputData{Keyboard=new KeyboardInput{Key=key}}},
                new NativeInput{Type=1,Data=new InputData{Keyboard=new KeyboardInput{Key=key,Flags=2}}},
                new NativeInput{Type=1,Data=new InputData{Keyboard=new KeyboardInput{Key=modifier,Flags=2}}}]);
            await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(downs==1&&ups==1&&modifierDowns==1&&modifierUps==1&&targetRetained,"Modified keyboard action did not retain one exact press/release for each original key target.");
        }
        finally
        {
            root.Content.RemoveHandler(downEvent,down);root.Content.RemoveHandler(UIElement.KeyUpEvent,up);
            keyActions.Add(new{key,target=target.Name,downs,ups,targetRetained,modifier,modifierDowns,modifierUps,preview});
        }
    }
}
