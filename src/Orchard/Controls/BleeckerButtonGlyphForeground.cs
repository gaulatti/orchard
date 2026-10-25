using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Shape = Microsoft.UI.Xaml.Shapes.Shape;

namespace Orchard;

// Native button states change the presenter's foreground, rather than the
// button's base value. Owned vector content must follow that rendered producer.
internal static class BleeckerButtonGlyphForeground
{
    internal static void Attach(Shape glyph)
    {
        Binding? owned = null;
        var queued = false;
        void Reconcile()
        {
            if(queued)return;
            queued=true;
            if(!glyph.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>
            {
                queued=false;
                if(glyph.IsLoaded)Loaded(glyph,new RoutedEventArgs());
            }))
            {
                queued=false;
                if(glyph.IsLoaded)throw new InvalidOperationException("Button foreground dispatcher rejected reconciliation.");
            }
        }
        void Loaded(object sender,RoutedEventArgs args)
        {
            if(!ReferenceEquals(glyph.ReadLocalValue(Shape.StrokeProperty),DependencyProperty.UnsetValue)
                &&(owned is null||glyph.GetBindingExpression(Shape.StrokeProperty)?.ParentBinding!=owned))return;
            for(var parent=VisualTreeHelper.GetParent(glyph);parent is not null;parent=VisualTreeHelper.GetParent(parent))
            {
                if(parent is ContentPresenter presenter)
                {
                    owned=new Binding{Source=presenter,Path=new PropertyPath(nameof(ContentPresenter.Foreground))};
                    glyph.SetBinding(Shape.StrokeProperty,owned);return;
                }
                if(parent is Button button)
                {
                    // Existing caller templates can host content directly.
                    // Without a presenter state layer, the button remains
                    // their original, authoritative foreground producer.
                    owned=new Binding{Source=button,Path=new PropertyPath(nameof(Control.Foreground))};
                    glyph.SetBinding(Shape.StrokeProperty,owned);return;
                }
            }
            throw new InvalidOperationException("Rendered navigation glyphs require their native button.");
        }
        void Unloaded(object sender,RoutedEventArgs args)
        {
            if(owned is not null&&glyph.GetBindingExpression(Shape.StrokeProperty)?.ParentBinding==owned)
                glyph.ClearValue(Shape.StrokeProperty);
            owned=null;
            // Reparenting can deliver an old unload after the new load.
            // Release immediately, then follow the current tree once settled.
            Reconcile();
        }
        glyph.Loaded+=(sender,args)=>{Loaded(sender,args);Reconcile();};glyph.Unloaded+=Unloaded;
        if(glyph.IsLoaded){Loaded(glyph,new RoutedEventArgs());Reconcile();}
    }
}
