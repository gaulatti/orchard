using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    private void AttachResetPresentation(Button button)
    {
        resetTransform=new CompositeTransform();button.RenderTransform=resetTransform;button.RenderTransformOrigin=new Point(.5,.5);
        resetPointer=new(button,()=>button,_=>UpdateResetPresentation(),observeEnabled:true);
        resetPressedCallback=button.RegisterPropertyChangedCallback(Button.IsPressedProperty,(_,_)=>UpdateResetPresentation());
        button.IsEnabledChanged+=ResetEnabledChanged;button.Loaded+=ResetLoaded;button.Unloaded+=ResetUnloaded;
    }
    private void UpdateResetPresentation()
    {
        if(resetTransform is null||reset is null)return;
        // The source uses individual CSS scale/translate properties. They
        // change immediately even when its color transitions are enabled.
        var held=reset.IsLoaded&&reset.IsEnabled&&reset.IsPressed;
        resetTransform.ScaleX=resetTransform.ScaleY=held ? .99 : 1;resetTransform.TranslateY=held ? 1 : 0;
        if(!reset.IsLoaded||resetStates is null)return;
        var state=!reset.IsEnabled?"Disabled":reset.IsPressed?"Pressed":resetPointer?.IsOver==true?"PointerOver":"Normal";
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
    private void ResetEnabledChanged(object sender,DependencyPropertyChangedEventArgs args){ObserveResetPointer();UpdateResetPresentation();}
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
    {resetPointer?.Detach();if(resetTransform is not null){resetTransform.ScaleX=resetTransform.ScaleY=1;resetTransform.TranslateY=0;}ReconcileReset();}
    private void DetachResetPresentation(Button button)
    {
        button.UnregisterPropertyChangedCallback(Button.IsPressedProperty,resetPressedCallback);
        button.IsEnabledChanged-=ResetEnabledChanged;button.Loaded-=ResetLoaded;button.Unloaded-=ResetUnloaded;
        if(resetStates is not null)resetStates.CurrentStateChanged-=ResetStateChanged;resetStates=null;
        var pointer=resetPointer;resetPointer=null;pointer?.Detach();resetQueuedTarget=null;
        button.ClearValue(RenderTransformProperty);button.ClearValue(RenderTransformOriginProperty);resetTransform=null;
    }
}
