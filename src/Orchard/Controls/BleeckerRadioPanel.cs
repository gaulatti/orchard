using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

// CSS flex-wrap row layout, with source row/column gaps and stretched column rows.
internal sealed class BleeckerRadioPanel : Panel
{
    internal Orientation Orientation { get; set; }
    protected override Size MeasureOverride(Size available)
    {
        var x = 0d; var y = 0d; var lineHeight = 0d; var maximum = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(available.Width, double.PositiveInfinity)); var size = child.DesiredSize;
            if (Orientation == Orientation.Vertical) { y += size.Height + 12; maximum = Math.Max(maximum, size.Width); continue; }
            if (x > 0 && x + size.Width > available.Width) { y += lineHeight + 16; x = 0; lineHeight = 0; }
            x += size.Width + 16; lineHeight = Math.Max(lineHeight, size.Height); maximum = Math.Max(maximum, x - 16);
        }
        return Orientation == Orientation.Vertical ? new(double.IsFinite(available.Width) ? available.Width : maximum, Math.Max(0, y - (Children.Count > 0 ? 12 : 0))) : new(double.IsFinite(available.Width) ? available.Width : maximum, y + lineHeight);
    }
    protected override Size ArrangeOverride(Size final)
    {
        var x = 0d; var y = 0d; var lineHeight = 0d;var row=new List<(UIElement Child,double X,double Width)>();
        void Flush(){foreach(var item in row)item.Child.Arrange(new Rect(item.X,y,item.Width,lineHeight));row.Clear();}
        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            if (Orientation == Orientation.Vertical) { child.Arrange(new Rect(0,y,final.Width,size.Height)); y += size.Height + 12; continue; }
            if (x > 0 && x + size.Width > final.Width) { Flush();y += lineHeight + 16; x = 0; lineHeight = 0; }
            row.Add((child,x,size.Width));x += size.Width + 16; lineHeight = Math.Max(lineHeight,size.Height);
        }
        Flush();
        return final;
    }
}
