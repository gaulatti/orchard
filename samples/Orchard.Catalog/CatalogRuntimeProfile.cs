using System.Diagnostics;
using System.Text.Json;

namespace Orchard.Catalog;

// Smoke diagnostics only. No extra await, dispatcher turn or input is added.
internal static class CatalogRuntimeProfile
{
    private static readonly object sync = new();
    private static readonly Dictionary<string, Measurement> measurements = [];
    private static string? output;
    private static long started, phaseStarted;
    private static string phase = "startup";
    private static string theme = "Default";

    internal static void Start(string folder)
    {
        if (output is not null) throw new InvalidOperationException("Catalog profiling already started.");
        output = Path.Combine(folder, "catalog-runtime-profile.jsonl");
        started = phaseStarted = Stopwatch.GetTimestamp();
        File.WriteAllText(output, JsonSerializer.Serialize(new { kind = "start", format = "orchard-catalog-runtime-profile-v1" }) + "\n");
    }

    internal static void EnterFixture(string name, string requestedTheme)
    {
        if (output is null) return;
        FlushPhase();
        phase = name;
        theme = requestedTheme;
        phaseStarted = Stopwatch.GetTimestamp();
        File.AppendAllText(output, JsonSerializer.Serialize(new { kind = "phase-start", phase, theme, elapsedMilliseconds = Elapsed(started) }) + "\n");
    }

    internal static void Finish(bool passed)
    {
        if (output is null) return;
        FlushPhase();
        File.AppendAllText(output, JsonSerializer.Serialize(new { kind = "end", passed, elapsedMilliseconds = Elapsed(started) }) + "\n");
        output = null;
    }

    internal static Scope Measure(string category) => new(output is null ? null : category);

    internal static void Count(string category) => Record(category, 0);

    private static void Record(string category, long ticks)
    {
        if (output is null) return;
        lock (sync)
        {
            measurements.TryGetValue(category, out var previous);
            measurements[category] = new(previous.Count + 1, previous.Ticks + ticks, Math.Max(previous.MaximumTicks, ticks));
        }
    }

    private static void FlushPhase()
    {
        object[] samples;
        lock (sync)
        {
            samples = measurements.OrderBy(item => item.Key).Select(item => (object)new
            {
                category = item.Key,
                count = item.Value.Count,
                milliseconds = item.Value.Ticks * 1000d / Stopwatch.Frequency,
                maximumMilliseconds = item.Value.MaximumTicks * 1000d / Stopwatch.Frequency
            }).ToArray();
            measurements.Clear();
        }
        File.AppendAllText(output!, JsonSerializer.Serialize(new
        {
            kind = "phase", phase, theme, milliseconds = Elapsed(phaseStarted),
            elapsedMilliseconds = Elapsed(started), measurements = samples
        }) + "\n");
    }

    private static double Elapsed(long timestamp) => Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds;
    private readonly record struct Measurement(int Count, long Ticks, long MaximumTicks);

    internal readonly struct Scope(string? category) : IDisposable
    {
        private readonly long timestamp = category is null ? 0 : Stopwatch.GetTimestamp();
        public void Dispose()
        {
            if (category is not null) Record(category, Stopwatch.GetTimestamp() - timestamp);
        }
    }
}
