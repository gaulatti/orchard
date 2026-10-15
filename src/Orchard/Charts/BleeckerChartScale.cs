namespace Orchard;

// Recharts 3.8.1's default adaptive five-tick numeric axis. Empty data has no
// ticks; the [0,auto] lower bound expands for negative data while the upper
// bound follows the data maximum, including an entirely negative series.
internal static class BleeckerChartScale
{
    internal static double[] Ticks(IEnumerable<double> values, bool allowDecimals = true)
    {
        var finite = values.Where(double.IsFinite).ToArray();
        if (finite.Length == 0) return [];
        var min = Math.Min(0, finite.Min()); var max = finite.Max();
        if (min == max)
        {
            if(min==0)return [0,1,2,3,4];
            if(!allowDecimals)return Enumerable.Range(0,5).Select(i=>Math.Floor(min)+i-2).ToArray();
            var exponent=0;var middle=min;
            if(min!=Math.Truncate(min))
            {
                if(Math.Abs(min)<1){exponent=(int)Math.Floor(Math.Log10(Math.Abs(min)));middle=Math.Floor(min/Math.Pow(10,exponent));}
                else middle=Math.Floor(min);
            }
            return Enumerable.Range(0,5).Select(i=>DecimalPower(middle+i-2,exponent)).ToArray();
        }
        var rough = (max - min) / 4;
        if (!double.IsFinite(rough)) throw new ArgumentOutOfRangeException(nameof(values), "Chart domain exceeds the finite numeric range.");
        var digits = Math.Floor(Math.Log10(Math.Abs(rough))) + 1; var magnitude = Math.Pow(10, digits);
        var ratioScale = digits == 1 ? .1 : .05; var initial = Math.Ceiling(rough / magnitude / ratioScale);
        for (var correction = 0; ; correction++)
        {
            var step = (initial + correction) * ratioScale * magnitude;
            if(!allowDecimals)step=Math.Max(1,Math.Ceiling(DecimalPower((initial+correction)*(digits==1?1:5),(int)digits-(digits==1?1:2))));
            var below = (int)Math.Ceiling(-min / step); var above = (int)Math.Ceiling(max / step);
            var count = below + above + 1;
            if (count > 5) continue;
            if (count < 5) { if (max > 0) above += 5 - count; else below += 5 - count; }
            if(!allowDecimals)return Enumerable.Range(-below,below+above+1).Select(index=>index*step).ToArray();
            // The producer multiplies decimal tick steps before converting to
            // IEEE doubles. Avoid accumulating a binary step's rounding error.
            var coefficient=(initial+correction)*(digits==1?1:5);var exponent=(int)digits-(digits==1?1:2);
            return Enumerable.Range(-below, below + above + 1).Select(index => DecimalPower(index*coefficient,exponent)).ToArray();
        }
    }
    private static double DecimalPower(double integer,int exponent)=>double.Parse(integer.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+"e"+exponent.ToString(System.Globalization.CultureInfo.InvariantCulture),System.Globalization.CultureInfo.InvariantCulture);
    internal static string TickLabel(double value)
    {
        if(value==0)return "0";
        var text=value.ToString("R",System.Globalization.CultureInfo.InvariantCulture);var marker=text.IndexOf('E');if(marker<0)return text;
        var exponent=int.Parse(text[(marker+1)..],System.Globalization.CultureInfo.InvariantCulture);var mantissa=text[..marker];
        if(exponent is < -6 or >=21)return mantissa+"e"+(exponent>=0?"+":"")+exponent.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var sign=mantissa.StartsWith('-')?"-":"";var digits=mantissa.TrimStart('-').Replace(".","");var point=1+exponent;
        return sign+(point<=0?"0."+new string('0',-point)+digits:point>=digits.Length?digits+new string('0',point-digits.Length):digits[..point]+"."+digits[point..]);
    }
    internal static double Position(double value, double min, double max, double start, double end) => start + (value - min) / (max - min) * (end - start);
}
