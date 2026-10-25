using System.Globalization;
using System.Numerics;

namespace Orchard;

internal static class BleeckerSliderModel
{
    internal readonly record struct Range(double Minimum, double Maximum, double Step, double Value);
    private readonly record struct DecimalValue(BigInteger Digits, int Scale)
    {
        internal static DecimalValue From(double value)
        {
            var text = BleeckerNumberText.Format(value); var exponentAt = text.IndexOf('e'); var exponent = 0;
            if (exponentAt >= 0) { exponent = int.Parse(text[(exponentAt + 1)..], CultureInfo.InvariantCulture); text = text[..exponentAt]; }
            var point = text.IndexOf('.'); var scale = (point >= 0 ? text.Length - point - 1 : 0) - exponent;
            var digits = BigInteger.Parse(text.Replace(".", ""), CultureInfo.InvariantCulture);
            return scale < 0 ? new(digits * BigInteger.Pow(10, -scale), 0) : new(digits, scale);
        }
        internal BigInteger At(int scale) => Digits * BigInteger.Pow(10, scale - Scale);
        internal double Number()
        {
            var negative = Digits.Sign < 0; var digits = BigInteger.Abs(Digits).ToString(CultureInfo.InvariantCulture);
            var text = Scale == 0 ? digits : digits.Length <= Scale ? "0." + new string('0', Scale - digits.Length) + digits : digits.Insert(digits.Length - Scale, ".");
            return double.Parse((negative ? "-" : "") + text, CultureInfo.InvariantCulture);
        }
    }
    internal static Range Normalize(double min, double max, double step, double value)
    {
        var minimum = double.IsFinite(min) ? min : 0; var maximum = Math.Max(minimum, double.IsFinite(max) ? max : 100);
        var frequency = double.IsFinite(step) && step > 0 ? step : 1;
        if (minimum == maximum) return new(minimum, maximum, frequency, minimum);
        var a = DecimalValue.From(minimum); var b = DecimalValue.From(maximum); var s = DecimalValue.From(frequency);
        DecimalValue requested;
        if (double.IsFinite(value)) requested = DecimalValue.From(Math.Clamp(value, minimum, maximum));
        else { var common = Math.Max(a.Scale, b.Scale); requested = new((a.At(common) + b.At(common)) * 5, common + 1); }
        var scale = new[] { a.Scale, b.Scale, s.Scale, requested.Scale }.Max();
        var low = a.At(scale); var high = b.At(scale); var increment = s.At(scale); var actual = BigInteger.Clamp(requested.At(scale), low, high);
        var index = BigInteger.DivRem(actual - low, increment, out var remainder);
        if (remainder * 2 >= increment) index++;
        index = BigInteger.Min(index, (high - low) / increment);
        return new(minimum, maximum, frequency, new DecimalValue(low + index * increment, scale).Number());
    }
    internal static double StepBy(Range range, int direction, bool page = false)
    {
        if (range.Minimum == range.Maximum) return range.Value;
        var a = DecimalValue.From(range.Minimum); var b = DecimalValue.From(range.Maximum); var s = DecimalValue.From(range.Step); var v = DecimalValue.From(range.Value);
        var scale = new[] { a.Scale, b.Scale, s.Scale, v.Scale }.Max(); var low = a.At(scale); var high = b.At(scale); var increment = s.At(scale); var current = v.At(scale);
        if (page)
        {
            var amount = BigInteger.Max(high - low, increment * 10);
            var requested = BigInteger.Clamp(current * 10 + direction * amount, low * 10, high * 10);
            return Normalize(range.Minimum, range.Maximum, range.Step, new DecimalValue(requested, scale + 1).Number()).Value;
        }
        var index = (current - low) / increment + direction; index = BigInteger.Clamp(index, 0, (high - low) / increment);
        return new DecimalValue(low + index * increment, scale).Number();
    }
    internal static double Key(Range range, string key, bool rtl = false) => key switch
    {
        "ArrowRight" => StepBy(range, rtl ? -1 : 1), "ArrowLeft" => StepBy(range, rtl ? 1 : -1),
        "ArrowUp" => StepBy(range, 1), "ArrowDown" => StepBy(range, -1), "PageUp" => StepBy(range, 1, true), "PageDown" => StepBy(range, -1, true),
        "Home" => range.Minimum, "End" => Normalize(range.Minimum, range.Maximum, range.Step, range.Maximum).Value,
        _ => throw new ArgumentException("Unknown range key.", nameof(key))
    };
    internal static double Fraction(Range range)
    {
        if (range.Minimum == range.Maximum) return 0;
        var a = DecimalValue.From(range.Minimum); var b = DecimalValue.From(range.Maximum); var v = DecimalValue.From(range.Value); var scale = Math.Max(a.Scale, Math.Max(b.Scale, v.Scale));
        return (double)((v.At(scale) - a.At(scale)) * 10000000000000000 / (b.At(scale) - a.At(scale))) / 10000000000000000;
    }
    internal static double ThumbLeft(double width, Range range, bool rtl = false)
    {
        var travel = Math.Max(0, width - 20); var position = Math.Floor((double)(float)(travel * Fraction(range)) * 64) / 64;
        return rtl ? width - 20 - position : position;
    }
    internal sealed class FillStyle
    {
        internal double? Percentage { get; private set; }
        internal void Apply(double min, double max, double value)
        {
            var percentage = (value - min) / (max - min) * 100;
            // A rejected CSS width assignment retains the previous inline
            // percentage. A fresh element with no assignment has auto width.
            if (double.IsFinite(percentage) && percentage >= 0) Percentage = percentage;
        }
        internal double Width(double width) => Percentage is null ? width : Math.Floor((double)(float)(width * Percentage.Value / 100) * 64) / 64;
    }
}
