using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Orchard;

// The frozen wrapper's leaf total and Recharts 3.8.1 drawArcs. This is not a
// normalized partition: missing branch values and explicit root totals retain
// their measured source behavior.
internal static class BleeckerSunburstGeometry
{
    internal sealed class Node(IReadOnlyDictionary<string, object?> record, int[] path, int sibling)
    {
        internal readonly IReadOnlyDictionary<string, object?> Record = record;
        internal readonly int[] Path = path;
        internal readonly int Sibling = sibling;
        internal readonly List<Node> Children = [];
        internal object? ChildSource, Value;
        internal int Depth = 1;
        internal double LeafTotal;
    }
    internal sealed record Entry(Node Node, double Inner, double Outer, double Start, double End, double LabelX, double LabelY, string Label)
    {
        internal bool HasLabel => double.IsFinite(LabelX) && double.IsFinite(LabelY);
        internal string? Path(double cx, double cy) => new[] { Inner, Outer, Start, End, cx, cy }.All(double.IsFinite) ? BleeckerPieGeometry.Path(cx, cy, Inner, Outer, new(Start, End, 0)) : null;
    }
    internal sealed record Layout(Node Root, IReadOnlyList<Node> Nodes, Entry[] Entries, double RootValue, double Thickness, double CenterX, double CenterY);
    internal static object? Read(IReadOnlyDictionary<string, object?> record, string key) => record.TryGetValue(key, out var value) ? value : null;
    private static bool Numeric(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    private static double LiteralNumber(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);
    internal static string String(object? value) => value switch
    {
        null => "",
        bool flag => flag ? "true" : "false",
        IList list => string.Join(",", list.Cast<object?>().Select(String)),
        _ => Numeric(value) ? BleeckerChartScale.TickLabel(LiteralNumber(value)) : value is string text ? text : "[object Object]"
    };
    private static double Coerce(object? value) => value switch { null => double.NaN, bool flag => flag ? 1 : 0, IList list => BleeckerChartData.Number(String(list)) ?? double.NaN, _ => Numeric(value) ? LiteralNumber(value) : BleeckerChartData.Number(value) ?? double.NaN };
    private static double Scale(object? value, double total)
    {
        var number = Coerce(value); if (double.IsNaN(number) || double.IsNaN(total)) return double.NaN;
        var t = total == 0 ? .5 : total < 0 ? (number - total) / -total : number / total;
        return total < 0 ? 360 * (1 - t) + 0 * t : 0 * (1 - t) + 360 * t;
    }
    private static IReadOnlyDictionary<string, object?>[] Children(object? value)
    {
        if (value is null) return [];
        if (value is not IEnumerable sequence || value is string) throw new InvalidOperationException("Sunburst children requires a collection of node records.");
        return sequence.Cast<object?>().Select(child => child as IReadOnlyDictionary<string, object?> ?? throw new InvalidOperationException("Sunburst children requires non-null node records.")).ToArray();
    }
    internal static Layout Compute(IReadOnlyDictionary<string, object?> data, string dataKey, double width, double height)
    {
        ArgumentNullException.ThrowIfNull(data); ArgumentNullException.ThrowIfNull(dataKey);
        var nodes = new List<Node>();
        Node Create(IReadOnlyDictionary<string, object?> row, int[] path, int sibling) { var node = new Node(row, path, sibling) { ChildSource = Read(row, "children"), Value = Read(row, dataKey) }; nodes.Add(node); return node; }
        var root = Create(data, [], 0); var active = new HashSet<IReadOnlyDictionary<string, object?>>(ReferenceEqualityComparer.Instance) { data };
        var build = new Stack<(Node Node, IReadOnlyDictionary<string, object?>[] Children, int Next)>(); build.Push((root, Children(root.ChildSource), 0));
        while (build.TryPop(out var frame))
        {
            if (frame.Next == frame.Children.Length)
            {
                frame.Node.Depth = frame.Node.Children.Count == 0 ? 1 : 1 + frame.Node.Children.Max(child => child.Depth);
                if (frame.Node.Children.Count == 0) frame.Node.LeafTotal = Numeric(frame.Node.Value) ? LiteralNumber(frame.Node.Value!) : 0;
                else foreach (var child in frame.Node.Children) frame.Node.LeafTotal += child.LeafTotal;
                active.Remove(frame.Node.Record); continue;
            }
            build.Push((frame.Node, frame.Children, frame.Next + 1)); var record = frame.Children[frame.Next];
            if (!active.Add(record)) throw new InvalidOperationException("Sunburst tree contains a cycle; the source traversal cannot terminate.");
            var childNode = Create(record, [.. frame.Node.Path, frame.Next], frame.Next); frame.Node.Children.Add(childNode); build.Push((childNode, Children(childNode.ChildSource), 0));
        }
        var total = Numeric(root.Value) ? LiteralNumber(root.Value!) : root.LeafTotal > 0 ? root.LeafTotal : double.NaN;
        var cx = width / 2; var cy = height / 2; var thickness = (Math.Min(width, height) / 2 - 50) / root.Depth;
        var entries = new List<Entry>(); var draw = new Stack<(IReadOnlyList<Node> Children, int Next, double Angle, double Inner)>(); draw.Push((root.Children, 0, 0, 50));
        while (draw.TryPop(out var frame))
        {
            if (frame.Next == frame.Children.Count) continue;
            var node = frame.Children[frame.Next]; var arc = Scale(node.Value, total); var end = frame.Angle + arc; var middle = frame.Angle + arc - arc / 2;
            var point = BleeckerPieGeometry.Polar(cx, cy, frame.Inner + thickness / 2, middle);
            entries.Add(new(node, frame.Inner, frame.Inner + thickness, frame.Angle, end, point.X, point.Y, Regex.Replace(String(node.Value), @"[ \f\n\r\t\v\u2028\u2029]+", " ")));
            draw.Push((frame.Children, frame.Next + 1, end, frame.Inner)); draw.Push((node.Children, 0, frame.Angle, frame.Inner + thickness + 2));
        }
        return new(root, nodes, entries.ToArray(), total, thickness, cx, cy);
    }
}
