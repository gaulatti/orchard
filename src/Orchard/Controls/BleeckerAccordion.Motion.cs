namespace Orchard;

public sealed partial class BleeckerAccordion
{
    internal sealed record MotionFrame(string Key,double PaintedAt,double ExpansionStart,double ExpansionDuration,double From,double Target,double Fraction,double Height,double NaturalHeight,double ClipHeight,double Angle,double BackgroundStart,double BackgroundDuration,BleeckerCssColor BackgroundFrom,BleeckerCssColor BackgroundTarget,string Background,double BackgroundOpacity,bool Expanded,bool Animating);
    internal IReadOnlyList<MotionFrame> MotionFrames=>Items.Select(item=>entries[item.Id].ReadMotion()).ToArray();
    private sealed partial class Entry
    {
        private double paintedAt;
        internal MotionFrame ReadMotion()=>new(Key,paintedAt,expansion.Timing.Start,expansion.Timing.Duration,expansion.Endpoints.From,expansion.Endpoints.To,reveal.Fraction,reveal.ActualHeight,reveal.NaturalHeight,((Microsoft.UI.Xaml.Media.RectangleGeometry)reveal.Clip).Rect.Height,angle.Angle,background.Timing.Start,background.Timing.Duration,background.Endpoints.From,background.Endpoints.To,hover.Color.ToString(),hover.Opacity,expanded,rendering);
    }
}
