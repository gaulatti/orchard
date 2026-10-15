using System.Globalization;

namespace Orchard;

// Recharts 3.8.1's 32-pass layout and the frozen wrapper's custom band path.
// Layout state is separate from the consumer's original records.
internal static class BleeckerSankeyGeometry
{
    internal sealed class Node(int index)
    {
        internal readonly int Index = index;
        internal readonly List<int> Incoming = [], Outgoing = [];
        internal int Depth;
        internal double X = double.NaN, Width = double.NaN, Y, Height, Value;
        internal double CenterY => Y + Height / 2;
        internal bool Paintable => new[] { X, Y, Width, Height }.All(double.IsFinite) && Width >= 0 && Height >= 0;
    }
    internal sealed class Link(int index, int source, int target, double value)
    {
        internal readonly int Index = index, Source = source, Target = target;
        internal readonly double Value = value;
        internal double Height, SourceOffset, TargetOffset;
        internal double SourceX, TargetX, SourceY, TargetY, SourceControlX, TargetControlX;
        internal double[] Coordinates => [SourceX, SourceY + SourceOffset, SourceControlX, SourceY + SourceOffset, TargetControlX, TargetY + TargetOffset, TargetX, TargetY + TargetOffset,
            TargetX, TargetY + TargetOffset + Height, TargetControlX, TargetY + TargetOffset + Height, SourceControlX, SourceY + SourceOffset + Height, SourceX, SourceY + SourceOffset + Height];
        internal bool Valid;
        internal string? Path => !Valid || !Coordinates.All(double.IsFinite) ? null : $"M{Pair(0)}C{Pair(2)} {Pair(4)} {Pair(6)}L{Pair(8)}C{Pair(10)} {Pair(12)} {Pair(14)}Z";
        private string Pair(int at) { var v = Coordinates; return v[at].ToString("G17", CultureInfo.InvariantCulture) + "," + v[at + 1].ToString("G17", CultureInfo.InvariantCulture); }
    }
    internal sealed record Layout(Node[] Nodes, Link[] Links);
    private static double? Scalar(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = BleeckerChartData.Read(row, key);
        if (value is null) return null;
        if (value is double number) return number;
        if (value is float single) return single;
        if (value is byte or sbyte or short or ushort or int or uint or long or ulong or decimal) return Convert.ToDouble(value, CultureInfo.InvariantCulture);
        throw new InvalidOperationException($"Sankey {key} requires a numeric record value.");
    }
    private static double Number(IReadOnlyDictionary<string, object?> row, string key) { var value = Scalar(row, key); return value is null || double.IsNaN(value.Value) ? 0 : value.Value; }
    private static int Index(IReadOnlyDictionary<string, object?> row, string key)
    {
        var value = Scalar(row, key);
        return value is double n && double.IsFinite(n) && n == Math.Truncate(n) && n >= 0 && n <= int.MaxValue ? (int)n : -1;
    }
    internal static Layout Compute(IReadOnlyList<IReadOnlyDictionary<string, object?>> records, IReadOnlyList<IReadOnlyDictionary<string, object?>> flows, double width, double height, double nodeWidth = 12, double curvature = .5)
    {
        var nodes = records.Select((_, i) => new Node(i)).ToArray();
        var links = flows.Select((row, i) => new Link(i, Index(row, "source"), Index(row, "target"), Number(row, "value"))).ToArray();
        bool Exists(int index) => index >= 0 && index < nodes.Length;
        foreach (var link in links)
        {
            if (Exists(link.Source)) nodes[link.Source].Outgoing.Add(link.Index);
            if (Exists(link.Target)) nodes[link.Target].Incoming.Add(link.Index);
        }
        foreach (var node in nodes) node.Value = Math.Max(node.Incoming.Sum(i => links[i].Value), node.Outgoing.Sum(i => links[i].Value));
        // Only rooted traversal recurses in the producer. A rootless cycle has
        // no horizontal coordinates; it must not be silently changed into a DAG.
        foreach (var root in nodes.Where(node => node.Incoming.Count == 0))
        {
            var path = new HashSet<int> { root.Index }; var pending = new Stack<(Node Node, int Next)>(); pending.Push((root, 0));
            while (pending.TryPop(out var frame))
            {
                if (frame.Next == frame.Node.Outgoing.Count) { path.Remove(frame.Node.Index); continue; }
                pending.Push((frame.Node, frame.Next + 1)); var target = links[frame.Node.Outgoing[frame.Next]].Target;
                if (!Exists(target)) continue;
                if (!path.Add(target)) throw new InvalidOperationException("Sankey rooted graph contains a cycle; the source layout cannot terminate.");
                nodes[target].Depth = Math.Max(frame.Node.Depth + 1, nodes[target].Depth); pending.Push((nodes[target], 0));
            }
        }
        var maxDepth = nodes.Length == 0 ? 0 : nodes.Max(node => node.Depth);
        if (maxDepth >= 1)
            foreach (var node in nodes)
            {
                if (node.Outgoing.Count == 0) node.Depth = maxDepth;
                node.X = node.Depth * ((width - 40 - nodeWidth) / maxDepth); node.Width = nodeWidth;
            }
        var columns = Enumerable.Range(0, maxDepth + 1).Select(depth => nodes.Where(node => node.Depth == depth).ToList()).ToArray();
        var contentHeight = height - 20;
        var ratio = columns.Where(column => column.Count > 0).Select(column => (contentHeight - (column.Count - 1) * 16) / column.Sum(node => node.Value)).DefaultIfEmpty(double.PositiveInfinity).Min();
        foreach (var column in columns) for (var i = 0; i < column.Count; i++) { column[i].Y = i; column[i].Height = column[i].Value * ratio; }
        foreach (var link in links) link.Height = link.Value * ratio;
        void Collisions()
        {
            foreach (var column in columns)
            {
                StableSort(column, node => node.Y); var y = 0d;
                foreach (var node in column) { var delta = y - node.Y; if (delta > 0) node.Y += delta; y = node.Y + node.Height + 16; }
                y = contentHeight + 16;
                for (var i = column.Count - 1; i >= 0; i--) { var node = column[i]; var delta = node.Y + node.Height + 16 - y; if (delta <= 0 || double.IsNaN(delta)) break; node.Y -= delta; y = node.Y; }
            }
        }
        void Relax(bool reverse, double alpha)
        {
            foreach (var column in reverse ? columns.Reverse() : columns)
                foreach (var node in column)
                {
                    var ids = reverse ? node.Outgoing : node.Incoming;
                    if (ids.Count == 0) continue;
                    var weighted = 0d;
                    foreach (var id in ids) { var link = links[id]; var index = reverse ? link.Target : link.Source; if (Exists(index)) weighted += nodes[index].CenterY * link.Value; }
                    node.Y += (weighted / ids.Sum(i => links[i].Value) - node.CenterY) * alpha;
                }
        }
        Collisions(); var alpha = 1d;
        for (var i = 0; i < 32; i++) { alpha *= .99; Relax(true, alpha); Collisions(); Relax(false, alpha); Collisions(); }
        foreach (var node in nodes)
        {
            StableSort(node.Outgoing, i => Exists(links[i].Target) ? nodes[links[i].Target].Y : double.NaN);
            StableSort(node.Incoming, i => Exists(links[i].Source) ? nodes[links[i].Source].Y : double.NaN);
            var offset = 0d; foreach (var i in node.Outgoing) { links[i].SourceOffset = offset; offset += links[i].Height; }
            offset = 0; foreach (var i in node.Incoming) { links[i].TargetOffset = offset; offset += links[i].Height; }
        }
        foreach (var link in links)
        {
            if (!Exists(link.Source) || !Exists(link.Target)) continue;
            var source = nodes[link.Source]; var target = nodes[link.Target];
            link.SourceX = source.X + source.Width + 20; link.TargetX = target.X + 20;
            link.SourceY = source.Y + link.SourceOffset + link.Height / 2 + 10; link.TargetY = target.Y + link.TargetOffset + link.Height / 2 + 10;
            link.SourceControlX = link.SourceX + (link.TargetX - link.SourceX) * curvature;
            link.TargetControlX = link.SourceX + (link.TargetX - link.SourceX) * (1 - curvature); link.Valid = true;
        }
        foreach (var node in nodes) { node.X += 20; node.Y += 10; }
        return new(nodes, links);
    }
    // JavaScript's stable comparator treats NaN as equality. Do not move
    // nonfinite entries to the front as the default .NET double comparer does.
    private static void StableSort<T>(List<T> list, Func<T, double> key)
    {
        for (var i = 1; i < list.Count; i++) { var item = list[i]; var at = i; while (at > 0 && key(list[at - 1]) - key(item) > 0) { list[at] = list[at - 1]; at--; } list[at] = item; }
    }
}
