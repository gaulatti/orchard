namespace Orchard;

// The native panel and background Win2D verification share the complete
// min-content -> item shrink -> legend wrap allocation path.
internal static class BleeckerProgressStackLayout
{
    internal readonly record struct TextWidths(double Maximum,double Minimum);
    internal readonly record struct InlineWidths(double Swatch,double Label,double Value);
    internal readonly record struct ItemBounds(double X,double Y,double Width,double Height);
    internal readonly record struct LegendBounds(double Height,IReadOnlyList<ItemBounds> Items);
    internal static double ItemWidth(double label,double value)=>22+label+value;

    internal static InlineWidths Allocate(double width,TextWidths label,TextWidths value)
    {
        var free=width-12;var basis=new[]{10d,label.Maximum,value.Maximum};
        var frozen=new bool[3];var min=new[]{0d,label.Minimum,value.Minimum};var sizes=basis.ToArray();
        for(var pass=0;pass<3;pass++)
        {
            var used=Enumerable.Range(0,3).Where(i=>frozen[i]).Sum(i=>sizes[i]);var total=Enumerable.Range(0,3).Where(i=>!frozen[i]).Sum(i=>basis[i]);var shrink=free-used-total;
            for(var i=0;i<3;i++)if(!frozen[i]){sizes[i]=basis[i]+(shrink<0&&total>0?shrink*basis[i]/total:0);if(sizes[i]<min[i]){sizes[i]=min[i];frozen[i]=true;}}
        }
        static double Unit(double size)=>Math.Floor(size*64+.5)/64;
        return new(Unit(sizes[0]),Unit(sizes[1]),Unit(sizes[2]));
    }

    internal static LegendBounds Place(double available,IReadOnlyList<(double Maximum,double Minimum)> widths,Func<int,double,double> measureHeight)
    {
        var positions=new List<ItemBounds>(widths.Count);var x=0d;var y=0d;var rowHeight=0d;
        for(var i=0;i<widths.Count;i++)
        {
            var item=widths[i];var width=Math.Min(item.Maximum,Math.Max(item.Minimum,available));
            if(x>0&&x+width>available){x=0;y+=rowHeight+12;rowHeight=0;}
            var height=measureHeight(i,width);positions.Add(new(x,y,width,height));x+=width+12;rowHeight=Math.Max(rowHeight,height);
        }
        return new(widths.Count==0?0:y+rowHeight,positions);
    }
    internal static double Center(double outer,double inner)=>Math.Truncate((outer-inner)*32)/64;
}
