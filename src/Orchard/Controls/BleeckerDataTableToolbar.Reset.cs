using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerDataTableToolbar
{
    private CompositeTransform? resetTransform;
    private long resetPressedCallback;

    private void AttachResetPresentation(Button button)
    {
        resetTransform=new CompositeTransform();button.RenderTransform=resetTransform;button.RenderTransformOrigin=new Point(.5,.5);
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
    }
    private void ResetEnabledChanged(object sender,DependencyPropertyChangedEventArgs args)=>UpdateResetPresentation();
    private void ResetLoaded(object sender,RoutedEventArgs args)=>UpdateResetPresentation();
    private void ResetUnloaded(object sender,RoutedEventArgs args)
    {if(resetTransform is not null){resetTransform.ScaleX=resetTransform.ScaleY=1;resetTransform.TranslateY=0;}}
    private void DetachResetPresentation(Button button)
    {
        button.UnregisterPropertyChangedCallback(Button.IsPressedProperty,resetPressedCallback);
        button.IsEnabledChanged-=ResetEnabledChanged;button.Loaded-=ResetLoaded;button.Unloaded-=ResetUnloaded;
        button.ClearValue(RenderTransformProperty);button.ClearValue(RenderTransformOriginProperty);resetTransform=null;
    }
}
