namespace Orchard;

// The original modal's geometry and endpoint CSS recipe. Text/button metrics
// are supplied by their renderer. Modal ownership is supplied by the shared
// native host; physical input/focus acceptance remains a separate boundary.
internal static class BleeckerAlertDialogModel
{
    internal readonly record struct Rect(double X, double Y, double Width, double Height);
    internal readonly record struct TextMetrics(double TitleHeight, double DescriptionHeight);
    internal readonly record struct Layout(Rect Surface, Rect Inner, Rect Header, Rect Icon, Rect IconSvg, Rect Title,
        Rect? Description, Rect Footer, Rect Cancel, Rect Confirm, double ViewportWidth, double ViewportHeight,
        double ExtentWidth, double ExtentHeight, double Padding, bool VerticalScrollbar);
    internal static Layout Measure(double viewportWidth, double viewportHeight, Func<double, TextMetrics> text,
        double cancelWidth, double confirmWidth, bool descriptionPresent, bool rtl, double maximumWidth = 448)
    {
        ArgumentNullException.ThrowIfNull(text);
        foreach (var value in new[] { viewportWidth, viewportHeight, cancelWidth, confirmWidth, maximumWidth })
            if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        var width = Math.Max(2, Math.Min(maximumWidth, Math.Max(0, viewportWidth - 32)));
        var maximumHeight = Math.Max(2, viewportHeight - 32); var padding = viewportWidth >= 768 ? 32d : 24;
        var scrollbar = false; Layout result = default;
        for (var pass = 0; pass < 8; pass++)
        {
            var clientWidth = Math.Max(0, width - 2 - (scrollbar ? 15 : 0)); var headerWidth = Math.Max(0, clientWidth - 2 * padding); var textWidth = Math.Max(0, headerWidth - 56); var metrics = text(textWidth);
            foreach (var value in new[] { metrics.TitleHeight, metrics.DescriptionHeight }) if (!double.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(text));
            var headerHeight = Math.Max(40, metrics.TitleHeight + (descriptionPresent ? 8 + metrics.DescriptionHeight : 0));
            var innerHeight = 2 * padding + headerHeight + 24 + 57; var height = Math.Min(innerHeight + 2, maximumHeight); var clientHeight = Math.Max(0, height - 2);
            var x = Math.Max(16, (viewportWidth - width) / 2); var y = Math.Max(16, (viewportHeight - height) / 2); var left = x + 1 + (rtl && scrollbar ? 15 : 0); var top = y + 1;
            var header = new Rect(left + padding, top + padding, headerWidth, headerHeight); var icon = new Rect(rtl ? header.X + header.Width - 40 : header.X, header.Y, 40, 40);
            var textX = rtl ? header.X : header.X + 56; var footer = new Rect(header.X, header.Y + headerHeight + 24, headerWidth, 57);
            var groupWidth = cancelWidth + 12 + confirmWidth; var groupX = rtl ? footer.X : footer.X + footer.Width - groupWidth; var buttonY = footer.Y + 21;
            result = new(new(x, y, width, height), new(left, top, clientWidth, innerHeight), header, icon, new(icon.X + 10, icon.Y + 10, 20, 20),
                new(textX, header.Y, textWidth, metrics.TitleHeight), descriptionPresent ? new Rect(textX, header.Y + metrics.TitleHeight + 8, textWidth, metrics.DescriptionHeight) : null,
                footer, new(rtl ? groupX + confirmWidth + 12 : groupX, buttonY, cancelWidth, 36), new(rtl ? groupX : groupX + cancelWidth + 12, buttonY, confirmWidth, 36),
                clientWidth, clientHeight, clientWidth, Math.Max(clientHeight, innerHeight), padding, scrollbar);
            var next = innerHeight > clientHeight;
            if (next == scrollbar) return result;
            scrollbar = next;
        }
        throw new InvalidOperationException("AlertDialog viewport geometry did not converge.");
    }
    internal readonly record struct Presentation(double Opacity, double BackdropOpacity, double Scale, double TranslateY);
    internal static Presentation SampleEndpointTransition(bool entering, double milliseconds, bool reduced = false)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        var progress = Math.Clamp(milliseconds / (reduced ? .01 : entering ? 190 : 150), 0, 1);
        var surface = SurfaceTiming(entering, progress); if (!entering) surface = 1 - surface;
        var backdrop = BackdropTiming(progress); if (!entering) backdrop = 1 - backdrop;
        return new(surface, backdrop, .992 + .008 * surface, 4 * (1 - surface));
    }
    internal static Rect Transform(Rect box, Presentation presentation) => new(box.X + box.Width * (1 - presentation.Scale) / 2,
        box.Y + box.Height * (1 - presentation.Scale) / 2 + presentation.TranslateY, box.Width * presentation.Scale, box.Height * presentation.Scale);
    internal static double SurfaceTiming(bool entering, double progress) => entering ? Cubic(progress, .22, 1, .36, 1) : Cubic(progress, .42, 0, 1, 1);
    internal static double BackdropTiming(double progress) => Cubic(progress, .4, 0, .2, 1);
    private static double Cubic(double progress, double x1, double y1, double x2, double y2)
    {
        if (progress <= 0) return 0; if (progress >= 1) return 1; double low = 0, high = 1;
        for (var i = 0; i < 32; i++) { var t = (low + high) / 2; var inverse = 1 - t; var x = 3 * inverse * inverse * t * x1 + 3 * inverse * t * t * x2 + t * t * t; if (x < progress) low = t; else high = t; }
        var at = (low + high) / 2; return 3 * (1 - at) * (1 - at) * at * y1 + 3 * (1 - at) * at * at * y2 + at * at * at;
    }
}

// Independent CSS properties keep independent reversal durations. Presence
// timers are intentionally owned by BleeckerAlertDialogPresence instead.
internal sealed class BleeckerAlertDialogMotion
{
    private readonly BleeckerCssTransition<double> surface = new((a, b, t) => a + (b - a) * t);
    private readonly BleeckerCssTransition<double> backdrop = new((a, b, t) => a + (b - a) * t);
    private double time;
    internal BleeckerAlertDialogMotion()
    {
        surface.Retarget(0, 0, 0, false); backdrop.Retarget(0, 0, 0, false);
    }
    internal (double Surface, double Backdrop) Durations => (surface.Timing.Duration, backdrop.Timing.Duration);
    internal void SetVisible(bool visible, double milliseconds, bool animate = true, bool reduced = false)
    {
        CheckTime(milliseconds); var duration = reduced ? .01 : visible ? 190 : 150;
        surface.Retarget(visible ? 1 : 0, milliseconds, duration, animate, progress => BleeckerAlertDialogModel.SurfaceTiming(visible, progress));
        backdrop.Retarget(visible ? 1 : 0, milliseconds, duration, animate, BleeckerAlertDialogModel.BackdropTiming);
    }
    internal BleeckerAlertDialogModel.Presentation SampleAt(double milliseconds)
    {
        CheckTime(milliseconds); var value = surface.ValueAt(milliseconds);
        return new(value, backdrop.ValueAt(milliseconds), .992 + .008 * value, 4 * (1 - value));
    }
    private void CheckTime(double milliseconds)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < time) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        time = milliseconds;
    }
}

// Presence is independent of CSS reduced motion. Input/focus ownership follows
// DesiredOpen immediately; closing paint remains mounted for 150ms.
internal sealed class BleeckerAlertDialogPresence
{
    private double time, visibleAt, removeAt;
    private bool disposed;
    internal bool DesiredOpen { get; private set; }
    internal bool Present { get; private set; }
    internal bool Visible { get; private set; }
    internal void SetOpen(bool open, double milliseconds)
    {
        Advance(milliseconds); if (disposed) throw new ObjectDisposedException(nameof(BleeckerAlertDialogPresence)); if (open == DesiredOpen) return;
        DesiredOpen = open;
        if (open) { Present = true; visibleAt = milliseconds + 20; }
        else { Visible = false; if (Present) removeAt = milliseconds + 150; }
    }
    internal void Advance(double milliseconds)
    {
        if (!double.IsFinite(milliseconds) || milliseconds < time) throw new ArgumentOutOfRangeException(nameof(milliseconds)); time = milliseconds;
        if (disposed) return;
        if (DesiredOpen && milliseconds >= visibleAt) Visible = true;
        else if (!DesiredOpen && Present && milliseconds >= removeAt) Present = false;
    }
    internal bool CanRequestCancel(bool loading) => DesiredOpen && !loading;
    internal void Dispose() { disposed = true; DesiredOpen = Present = Visible = false; }
}
