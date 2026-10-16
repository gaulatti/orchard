using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerStepper
{
    private sealed class StepperLayout(BleeckerStepper owner) : Panel
    {
        private double[] widths = [];
        protected override Size MeasureOverride(Size available)
        {
            UseLayoutRounding = false;
            var entries = owner.entries;
            if (entries.Count == 0) return new Size(0, 0);
            var width = double.IsFinite(available.Width) ? available.Width : entries.Sum(entry => Math.Max(32, Math.Min(128, entry.PreferredWidth)));
            widths = new double[entries.Count];
            if (owner.Orientation == Orientation.Horizontal)
            {
                // Equal flex bases, with min-content violations frozen before redistributing.
                var remaining = width; var flexible = Enumerable.Range(0, entries.Count).ToList();
                while (flexible.Count > 0)
                {
                    var share = remaining / flexible.Count;
                    var frozen = flexible.Where(i => entries[i].MinimumWidth > share).ToArray();
                    if (frozen.Length == 0) { foreach (var i in flexible) widths[i] = share; break; }
                    foreach (var i in frozen) { widths[i] = entries[i].MinimumWidth; remaining -= widths[i]; flexible.Remove(i); }
                }
                double edge = 0, roundedEdge = 0;
                for (var i = 0; i < widths.Length; i++)
                {
                    edge += widths[i]; var next = Math.Round(edge * 64) / 64;
                    widths[i] = next - roundedEdge; roundedEdge = next;
                }
            }
            else Array.Fill(widths, width);
            var height = 0d;
            for (var i = 0; i < entries.Count; i++)
            {
                entries[i].Measure(new Size(widths[i], double.PositiveInfinity));
                height = owner.Orientation == Orientation.Horizontal ? Math.Max(height, entries[i].DesiredSize.Height) : height + entries[i].DesiredSize.Height;
            }
            return new Size(width, height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            double x = 0, y = 0;
            for (var i = 0; i < owner.entries.Count; i++)
            {
                var entry = owner.entries[i]; var width = widths[i]; var height = entry.DesiredSize.Height;
                var horizontal = owner.Orientation == Orientation.Horizontal;
                var left = horizontal && owner.FlowDirection == FlowDirection.RightToLeft ? final.Width - x - width : x;
                entry.Arrange(new Rect(left, y, width, height));
                if (horizontal) x += width; else y += height;
            }
            return final;
        }
    }
}
