namespace Orchard;

// Source flex allocation includes the close button's 2px non-shrinking border.
internal static class BleeckerModalModel
{
    internal readonly record struct Rect(double X, double Y, double Width, double Height);
    internal readonly record struct Layout(Rect Surface, Rect Inner, Rect Header, Rect Title, Rect Close, Rect Body, Rect Content,
        double ViewportWidth, double ViewportHeight, double ExtentHeight, bool VerticalScrollbar);
    internal static Layout Measure(double viewportWidth, double viewportHeight, double titleMaximum, double titleMinimum,
        Func<double,double> titleHeight, Func<double,double> contentHeight, bool rtl = false, double maximumWidth = 576)
    {
        foreach(var value in new[]{viewportWidth,viewportHeight,titleMaximum,titleMinimum,maximumWidth}) if(!double.IsFinite(value)||value<0)throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        ArgumentNullException.ThrowIfNull(titleHeight); ArgumentNullException.ThrowIfNull(contentHeight);
        var width=Math.Max(2,Math.Min(maximumWidth,Math.Max(0,viewportWidth-32)));var maxHeight=Math.Max(2,viewportHeight-32);var padding=viewportWidth>=768?28d:24;var scrollbar=false;
        for(var pass=0;pass<8;pass++)
        {
            var clientWidth=Math.Max(0,width-2-(scrollbar?15:0));var available=Math.Max(0,clientWidth-2*padding-16);var titleWidth=titleMaximum;var closeWidth=36d;
            var deficit=titleMaximum+36-available;
            if(deficit>0){var total=titleMaximum+34;titleWidth=Math.Max(titleMinimum,titleMaximum-deficit*(total>0?titleMaximum/total:0));closeWidth=Math.Max(20,36-deficit*(total>0?34/total:0));if(closeWidth==20)titleWidth=Math.Max(titleMinimum,available-20);else if(titleWidth==titleMinimum)closeWidth=Math.Max(20,available-titleMinimum);}
            static double Unit(double value)=>Math.Floor(value*64+.5)/64;
            titleWidth=Unit(titleWidth);closeWidth=Unit(closeWidth);var th=titleHeight(titleWidth);var contentWidth=Math.Max(0,clientWidth-2*padding);var ch=contentHeight(contentWidth);
            if(!double.IsFinite(th)||th<0||!double.IsFinite(ch)||ch<0)throw new ArgumentOutOfRangeException(nameof(titleHeight));
            var rowHeight=Math.Max(th,36);var headerHeight=41+rowHeight;var bodyHeight=2*padding+ch;var extent=headerHeight+bodyHeight;var height=Math.Min(maxHeight,extent+2);var clientHeight=Math.Max(0,height-2);var next=extent>clientHeight;
            if(next!=scrollbar){scrollbar=next;continue;}
            var x=Math.Max(16,(viewportWidth-width)/2);var y=Math.Max(16,(viewportHeight-height)/2);var left=x+1+(rtl&&scrollbar?15:0);var top=y+1;
            var titleX=rtl?left+clientWidth-padding-titleWidth:left+padding;var closeX=rtl?left+padding:left+clientWidth-padding-closeWidth;
            return new(new(x,y,width,height),new(left,top,clientWidth,extent),new(left,top,clientWidth,headerHeight),new(titleX,top+20+(rowHeight-th)/2,titleWidth,th),new(closeX,top+20+(rowHeight-36)/2,closeWidth,36),new(left,top+headerHeight,clientWidth,bodyHeight),new(left+padding,top+headerHeight+padding,contentWidth,ch),clientWidth,clientHeight,extent,scrollbar);
        }
        throw new InvalidOperationException("Modal viewport geometry did not converge.");
    }
}
