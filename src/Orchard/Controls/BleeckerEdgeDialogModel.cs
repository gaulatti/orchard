namespace Orchard;

internal enum BleeckerEdgeSide { Left, Right, Top, Bottom }

// Geometry of the pinned Drawer/Sheet, independent of XAML and input delivery.
internal static class BleeckerEdgeDialogModel
{
    internal readonly record struct Rect(double X,double Y,double Width,double Height);
    internal readonly record struct TextMetrics(double TitleHeight,double DescriptionHeight);
    internal readonly record struct Layout(Rect Surface,Rect Inner,Rect? Handle,Rect? HandleIcon,Rect? Header,Rect? Title,Rect? Description,Rect? Close,Rect Body,Rect Content,double ExtentHeight,bool VerticalScrollbar);
    private static void Size(double value,string name){if(!double.IsFinite(value)||value<0)throw new ArgumentOutOfRangeException(name);}
    private static void Limit(double value,string name){if(double.IsNaN(value)||value<0||double.IsNegativeInfinity(value))throw new ArgumentOutOfRangeException(name);}
    internal static Layout Drawer(double viewportWidth,double viewportHeight,Func<double,TextMetrics> text,Func<double,double> content,bool title,bool description,double? maximumHeight=null)
    {
        Size(viewportWidth,nameof(viewportWidth));Size(viewportHeight,nameof(viewportHeight));ArgumentNullException.ThrowIfNull(text);ArgumentNullException.ThrowIfNull(content);var maximum=maximumHeight??viewportHeight*.9;Limit(maximum,nameof(maximumHeight));
        var contentWidth=Math.Max(0,viewportWidth-48);var metrics=text(contentWidth);Size(metrics.TitleHeight,nameof(text));Size(metrics.DescriptionHeight,nameof(text));var th=title?metrics.TitleHeight:0;var dh=description?metrics.DescriptionHeight:0;var headerHeight=title||description?4+th+(description?8+dh:0)+16:0;var scrollbar=false;
        for(var pass=0;pass<8;pass++)
        {
            var cw=Math.Max(0,contentWidth-(scrollbar?15:0));var ch=content(cw);Size(ch,nameof(content));var natural=1+40+headerHeight+ch+24;var height=Math.Min(maximum,natural);var bodyHeight=Math.Max(0,height-1-40-headerHeight);var next=ch+24>bodyHeight;if(next!=scrollbar){scrollbar=next;continue;}
            var top=viewportHeight-height;var headerY=top+41;var bodyY=headerY+headerHeight;var header=title||description?new Rect(0,headerY,viewportWidth,headerHeight):(Rect?)null;
            return new(new(0,top,viewportWidth,height),new(0,top+1,viewportWidth,Math.Max(0,height-1)),new(0,top+1,viewportWidth,40),new((viewportWidth-24)/2,top+13,24,24),header,title?new Rect(24,headerY+4,contentWidth,th):null,description?new Rect(24,headerY+4+th+8,contentWidth,dh):null,null,new(0,bodyY,viewportWidth,bodyHeight),new(24,bodyY,cw,ch),Math.Max(bodyHeight,ch+24),scrollbar);
        }
        throw new InvalidOperationException("Drawer scrollbar geometry did not converge.");
    }
    internal static Layout Sheet(double viewportWidth,double viewportHeight,BleeckerEdgeSide side,Func<double,TextMetrics> text,Func<double,double> content,bool title,bool description,bool scrollContent=true,bool fill=false,double? maximumWidth=null)
    {
        Size(viewportWidth,nameof(viewportWidth));Size(viewportHeight,nameof(viewportHeight));if(!Enum.IsDefined(side))throw new ArgumentOutOfRangeException(nameof(side));ArgumentNullException.ThrowIfNull(text);ArgumentNullException.ThrowIfNull(content);var vertical=side is BleeckerEdgeSide.Left or BleeckerEdgeSide.Right;var maximum=maximumWidth??(vertical?384:double.PositiveInfinity);Limit(maximum,nameof(maximumWidth));var width=Math.Min(viewportWidth,maximum);var padding=viewportWidth>=768?32d:24;var innerWidth=Math.Max(0,width-2-2*padding);var closeWidth=Math.Clamp(innerWidth-16,20,36);var textWidth=Math.Max(0,innerWidth-16-closeWidth);var metrics=text(textWidth);Size(metrics.TitleHeight,nameof(text));Size(metrics.DescriptionHeight,nameof(text));var th=title?metrics.TitleHeight:0;var dh=description?metrics.DescriptionHeight:0;var headerHeight=Math.Max(36,th+(description?8+dh:0));var scrollbar=false;
        for(var pass=0;pass<8;pass++)
        {
            var cw=Math.Max(0,innerWidth-(scrollbar?15:0));var natural=content(cw);Size(natural,nameof(content));var height=vertical?viewportHeight:2+2*padding+headerHeight+20+natural;var bodyHeight=Math.Max(0,height-2-2*padding-headerHeight-20);var next=scrollContent&&natural>bodyHeight;if(next!=scrollbar){scrollbar=next;continue;}
            var x=side==BleeckerEdgeSide.Right?viewportWidth-width:0;var y=side==BleeckerEdgeSide.Bottom?viewportHeight-height:0;var hx=x+1+padding;var hy=y+1+padding;var bodyY=hy+headerHeight+20;var ch=scrollContent?natural:fill?bodyHeight:Math.Min(natural,bodyHeight);
            return new(new(x,y,width,height),new(x+1,y+1,Math.Max(0,width-2),Math.Max(0,height-2)),null,null,new(hx,hy,innerWidth,headerHeight),title?new Rect(hx,hy,textWidth,th):null,description?new Rect(hx,hy+th+8,textWidth,dh):null,new(hx+innerWidth-closeWidth,hy,closeWidth,36),new(hx,bodyY,innerWidth,bodyHeight),new(hx,bodyY,cw,ch),Math.Max(bodyHeight,ch),scrollbar);
        }
        throw new InvalidOperationException("Sheet scrollbar geometry did not converge.");
    }
}

internal sealed class BleeckerEdgeDialogPresence
{
    private double time,visibleAt,removeAt;private bool disposed;
    internal bool DesiredOpen{get;private set;}internal bool Present{get;private set;}internal bool Visible{get;private set;}
    internal void SetOpen(bool open,double milliseconds){Advance(milliseconds);if(disposed)throw new ObjectDisposedException(nameof(BleeckerEdgeDialogPresence));if(open==DesiredOpen)return;DesiredOpen=open;if(open){Present=true;visibleAt=milliseconds+20;}else{Visible=false;if(Present)removeAt=milliseconds+180;}}
    internal void Advance(double milliseconds){if(!double.IsFinite(milliseconds)||milliseconds<time)throw new ArgumentOutOfRangeException(nameof(milliseconds));time=milliseconds;if(disposed)return;if(DesiredOpen&&milliseconds>=visibleAt)Visible=true;else if(!DesiredOpen&&Present&&milliseconds>=removeAt)Present=false;}
    internal void Dispose(){disposed=true;DesiredOpen=Present=Visible=false;}
}

internal sealed class BleeckerEdgeDialogMotion
{
    private readonly BleeckerCssTransition<double> surface=new((a,b,t)=>a+(b-a)*t),backdrop=new((a,b,t)=>a+(b-a)*t);private double time;
    private readonly bool drawer;
    internal BleeckerEdgeDialogMotion(bool drawer){this.drawer=drawer;surface.Retarget(0,0,0,false);backdrop.Retarget(0,0,0,false);}
    internal (double Surface,double Backdrop) Durations=>(surface.Timing.Duration,backdrop.Timing.Duration);
    internal void SetVisible(bool visible,double milliseconds,bool reduced=false){Check(milliseconds);var duration=reduced?.01:visible?240:180;Func<double,double> easing=visible?BleeckerColorMix.Premium:Exit;surface.Retarget(visible?1:0,milliseconds,duration,true,easing);backdrop.Retarget(visible?1:0,milliseconds,duration,true,visible||drawer?BleeckerColorMix.Premium:Exit);}
    internal (double Surface,double Backdrop) Sample(double milliseconds){Check(milliseconds);return(surface.ValueAt(milliseconds),backdrop.ValueAt(milliseconds));}
    private void Check(double milliseconds){if(!double.IsFinite(milliseconds)||milliseconds<time)throw new ArgumentOutOfRangeException(nameof(milliseconds));time=milliseconds;}
    // Tailwind's pinned ease-in is (.4,0,1,1), distinct from CSS's keyword.
    private static double Exit(double progress)
    {
        if(progress<=0)return 0;if(progress>=1)return 1;double low=0,high=1;
        for(var i=0;i<32;i++){var t=(low+high)/2;var x=3*(1-t)*(1-t)*t*.4+3*(1-t)*t*t+t*t*t;if(x<progress)low=t;else high=t;}
        var at=(low+high)/2;return 3*(1-at)*at*at+at*at*at;
    }
}

internal sealed class BleeckerDrawerDismissGesture
{
    private double? start;
    internal void Start(double clientY){if(!double.IsFinite(clientY))throw new ArgumentOutOfRangeException(nameof(clientY));start=clientY;}
    internal bool End(double clientY){if(!double.IsFinite(clientY))throw new ArgumentOutOfRangeException(nameof(clientY));var initial=start;start=null;return initial is{} value&&clientY-value>60;}
    internal void Reset()=>start=null;
}
