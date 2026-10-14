namespace Orchard;

// Adapted from d3-shape 3.2.0 curve/monotone.js (Mike Bostock).
// See licenses/LICENSE-D3-Shape.txt. Recharts 3.8.1 selects monotoneX for
// the frozen horizontal Line Chart and leaves connectNulls disabled.
internal static class BleeckerMonotoneCurve
{
    internal readonly record struct Point(double X,double Y);
    internal readonly record struct Command(string Kind,double[] Values);

    internal static IReadOnlyList<Command> Create(IEnumerable<Point> points)
    {
        var commands=new List<Command>();var segment=new List<Point>();
        foreach(var point in points)
        {
            if(!double.IsFinite(point.X)||!double.IsFinite(point.Y)){Flush();continue;}
            if(segment.Count==0||segment[^1]!=point)segment.Add(point);
        }
        Flush();return commands.AsReadOnly();

        void Flush()
        {
            if(segment.Count==0)return;
            var first=segment[0];commands.Add(new("M",[first.X,first.Y]));
            if(segment.Count==1)commands.Add(new("Z",[]));
            else if(segment.Count==2)commands.Add(new("L",[segment[1].X,segment[1].Y]));
            else
            {
                var tangents=new double[segment.Count];
                for(var index=1;index<segment.Count-1;index++)tangents[index]=Slope3(segment[index-1],segment[index],segment[index+1]);
                tangents[0]=Slope2(segment[0],segment[1],tangents[1]);
                tangents[^1]=Slope2(segment[^2],segment[^1],tangents[^2]);
                for(var index=0;index<segment.Count-1;index++)
                {
                    var a=segment[index];var b=segment[index+1];var dx=(b.X-a.X)/3;
                    commands.Add(new("C",[a.X+dx,a.Y+dx*tangents[index],b.X-dx,b.Y-dx*tangents[index+1],b.X,b.Y]));
                }
            }
            segment.Clear();
        }
    }
    private static double Slope3(Point a,Point b,Point c)
    {
        var h0=b.X-a.X;var h1=c.X-b.X;
        var s0=(b.Y-a.Y)/(h0!=0?h0:h1<0?-0d:0d);
        var s1=(c.Y-b.Y)/(h1!=0?h1:h0<0?-0d:0d);
        var p=(s0*h1+s1*h0)/(h0+h1);
        var result=(Sign(s0)+Sign(s1))*Math.Min(Math.Min(Math.Abs(s0),Math.Abs(s1)),.5*Math.Abs(p));
        return double.IsNaN(result)||result==0?0:result;
    }
    private static double Sign(double value)=>value<0?-1:1;
    private static double Slope2(Point a,Point b,double tangent)=>b.X!=a.X?(3*(b.Y-a.Y)/(b.X-a.X)-tangent)/2:tangent;
}
