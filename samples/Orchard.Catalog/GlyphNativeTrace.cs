using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Orchard.Catalog;

// Diagnostic branch only. The existing highlighter assertions remain authoritative.
internal static class GlyphNativeTrace
{
    internal static bool Enabled => Environment.GetCommandLineArgs().Contains("--smoke-glyph-native-trace");

    internal static async Task BeginAsync(string folder)
    {
        if (!Enabled) return;
        if (Environment.GetEnvironmentVariable("CI") != "true" ||
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
            Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") != "github-hosted" || !IsDebuggerPresent())
            throw new InvalidOperationException("Native glyph tracing requires an owned hosted CI debugger.");
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, "--glyph-trace-token");
        if (index < 0 || index + 1 >= args.Length || !Guid.TryParseExact(args[index + 1], "N", out _))
            throw new InvalidOperationException("Missing isolated glyph trace token.");
        var token = args[index + 1];
        using var process = Process.GetCurrentProcess();
        var registration = Path.Combine(folder, "owned-process.json");
        await File.WriteAllTextAsync(registration + ".pending", JsonSerializer.Serialize(new
        {
            token, pid = Environment.ProcessId,
            startTimeUtcTicks = process.StartTime.ToUniversalTime().Ticks.ToString(), executable = Environment.ProcessPath
        }));
        File.Move(registration + ".pending", registration);
        var release = Path.Combine(folder, "identity-verified.release");
        var clock = Stopwatch.StartNew();
        while (!File.Exists(release) && clock.ElapsedMilliseconds < 120000) await Task.Delay(50);
        if (!File.Exists(release) || await File.ReadAllTextAsync(release) != token)
            throw new InvalidOperationException("The runner did not verify and release this exact native process.");
        Mark(0); // Only now enable the target-data breakpoints.
    }

    internal static void Mark(int phase)
    {
        if (Enabled)
        {
            if (phase is < 0 or > 12 || !IsDebuggerPresent())
                throw new InvalidOperationException("Invalid or detached native glyph trace phase.");
            OutputDebugStringW("OGT:" + (char)('A' + phase));
        }
    }

    [DllImport("kernel32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern void OutputDebugStringW(string message);
}
