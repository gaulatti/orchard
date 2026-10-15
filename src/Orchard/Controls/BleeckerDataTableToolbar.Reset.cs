using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerDataTableToolbar
{
    private CompositeTransform? resetTransform;
    private long resetPressedCallback;
    private BleeckerDisabledPointer? resetPointer;
    private VisualStateGroup? resetStates;
    private Button? resetQueuedTarget;
    private bool applyingResetState;
    private uint? resetHeldPointer;
    private ClickMode? resetEnterClickMode;

    private void AttachResetPresentation(Button button)
    {
        resetTransform=new CompositeTransform();button.RenderTransform=resetTransform;button.RenderTransformOrigin=new Point(.5,.5);
        resetPointer=new(button,()=>button,_=>UpdateResetPresentation(),observeEnabled:true);
        resetPressedCallback=button.RegisterPropertyChangedCallback(Button.IsPressedProperty,(_,_)=>UpdateResetPresentation());
        button.IsEnabledChanged+=ResetEnabledChanged;button.Loaded+=ResetLoaded;button.Unloaded+=ResetUnloaded;
        button.PreviewKeyDown+=ResetPreviewKeyDown;button.LostFocus+=ResetLostFocus;
        button.AddHandler(UIElement.KeyUpEvent,new KeyEventHandler(ResetKeyUp),true);
        button.AddHandler(UIElement.PointerPressedEvent,new PointerEventHandler(ResetPointerPressed),true);
        button.AddHandler(UIElement.PointerReleasedEvent,new PointerEventHandler(ResetPointerEnded),true);
        button.PointerCanceled+=ResetPointerEnded;button.PointerCaptureLost+=ResetPointerEnded;
    }
    private void UpdateResetPresentation()
    {
        if(resetTransform is null||reset is null)return;
        // The source uses individual CSS scale/translate properties. They
        // change immediately even when its color transitions are enabled.
        // CSS :active survives a captured mouse drag outside the button;
        // native IsPressed deliberately clears there. Enter invokes on down
        // without the source's Space/mouse press transform.
        var held=reset.IsLoaded&&reset.IsEnabled&&(resetHeldPointer is not null||(reset.IsPressed&&resetEnterClickMode is null));
        resetTransform.ScaleX=resetTransform.ScaleY=held ? .99 : 1;resetTransform.TranslateY=held ? 1 : 0;
        if(!reset.IsLoaded||resetStates is null)return;
        // The source Ghost recipe has hover paint, but no active fill.
        var state=!reset.IsEnabled?"Disabled":resetPointer?.IsOver==true?"PointerOver":"Normal";
        if(resetStates.CurrentState?.Name==state)return;
        applyingResetState=true;try{VisualStateManager.GoToState(reset,state,false);}finally{applyingResetState=false;}
    }
    private void ObserveResetPointer()
    {
        if(reset is null)return;
        if(reset.IsLoaded&&reset.IsEnabled)resetPointer?.Update();else resetPointer?.Detach();
    }
    private void ReconcileReset()
    {
        if(reset is not Button target||ReferenceEquals(resetQueuedTarget,target))return;
        resetQueuedTarget=target;
        if(!DispatcherQueue.TryEnqueue(()=>
        {
            if(ReferenceEquals(resetQueuedTarget,target))resetQueuedTarget=null;
            if(!ReferenceEquals(reset,target))return;ObserveResetPointer();UpdateResetPresentation();
        }))resetQueuedTarget=null;
    }
    private void ResetStateChanged(object sender,VisualStateChangedEventArgs args){if(!applyingResetState)ReconcileReset();}
    private void ResetEnabledChanged(object sender,DependencyPropertyChangedEventArgs args)
    {if(reset is not null&&!reset.IsEnabled){resetHeldPointer=null;RestoreResetEnterMode(reset);}ObserveResetPointer();UpdateResetPresentation();}
    private void ResetPointerPressed(object sender,PointerRoutedEventArgs args)
    {
        var button=(Button)sender;
        if(args.IsGenerated||!button.IsLoaded||!button.IsEnabled||!args.GetCurrentPoint(button).Properties.IsLeftButtonPressed)return;
        resetHeldPointer=args.Pointer.PointerId;UpdateResetPresentation();
    }
    private void ResetPointerEnded(object sender,PointerRoutedEventArgs args)
    {if(resetHeldPointer!=args.Pointer.PointerId)return;resetHeldPointer=null;UpdateResetPresentation();}
    private void ResetPreviewKeyDown(object sender,KeyRoutedEventArgs args)
    {
        var button=(Button)sender;
        if(args.Key!=Windows.System.VirtualKey.Enter||!button.IsLoaded||!button.IsEnabled||resetEnterClickMode is not null)return;
        // Keep native Click/guards/command routing. Only Enter uses the source's
        // key-down activation; Space and pointer release retain Release mode.
        resetEnterClickMode=button.ClickMode;button.ClickMode=ClickMode.Press;UpdateResetPresentation();
    }
    private void ResetKeyUp(object sender,KeyRoutedEventArgs args)
    {if(args.Key==Windows.System.VirtualKey.Enter){RestoreResetEnterMode((Button)sender);UpdateResetPresentation();}}
    private void ResetLostFocus(object sender,RoutedEventArgs args)
    {RestoreResetEnterMode((Button)sender);UpdateResetPresentation();}
    private void RestoreResetEnterMode(Button button)
    {if(resetEnterClickMode is not ClickMode mode)return;resetEnterClickMode=null;button.ClickMode=mode;}
    private void ResetLoaded(object sender,RoutedEventArgs args)
    {
        var button=(Button)sender;button.ApplyTemplate();
        if(resetStates is not null)resetStates.CurrentStateChanged-=ResetStateChanged;
        resetStates=VisualTreeHelper.GetChildrenCount(button)>0&&VisualTreeHelper.GetChild(button,0) is FrameworkElement root?
            VisualStateManager.GetVisualStateGroups(root).SingleOrDefault(group=>group.Name=="CommonStates"):null;
        if(resetStates is not null)resetStates.CurrentStateChanged+=ResetStateChanged;
        ObserveResetPointer();UpdateResetPresentation();ReconcileReset();
    }
    private void ResetUnloaded(object sender,RoutedEventArgs args)
    {resetHeldPointer=null;RestoreResetEnterMode((Button)sender);resetPointer?.Detach();if(resetTransform is not null){resetTransform.ScaleX=resetTransform.ScaleY=1;resetTransform.TranslateY=0;}ReconcileReset();}
    private void DetachResetPresentation(Button button)
    {
        button.UnregisterPropertyChangedCallback(Button.IsPressedProperty,resetPressedCallback);
        button.IsEnabledChanged-=ResetEnabledChanged;button.Loaded-=ResetLoaded;button.Unloaded-=ResetUnloaded;
        button.PreviewKeyDown-=ResetPreviewKeyDown;button.LostFocus-=ResetLostFocus;
        button.RemoveHandler(UIElement.KeyUpEvent,new KeyEventHandler(ResetKeyUp));
        button.RemoveHandler(UIElement.PointerPressedEvent,new PointerEventHandler(ResetPointerPressed));
        button.RemoveHandler(UIElement.PointerReleasedEvent,new PointerEventHandler(ResetPointerEnded));
        button.PointerCanceled-=ResetPointerEnded;button.PointerCaptureLost-=ResetPointerEnded;
        resetHeldPointer=null;RestoreResetEnterMode(button);
        if(resetStates is not null)resetStates.CurrentStateChanged-=ResetStateChanged;resetStates=null;
        var pointer=resetPointer;resetPointer=null;pointer?.Detach();resetQueuedTarget=null;
        button.ClearValue(RenderTransformProperty);button.ClearValue(RenderTransformOriginProperty);resetTransform=null;
    }
}
