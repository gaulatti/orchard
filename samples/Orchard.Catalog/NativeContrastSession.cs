using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.UI;

namespace Orchard.Catalog;

/// <summary>Real Windows contrast-mode transitions on isolated hosted runners.</summary>
internal sealed class NativeContrastSession : IDisposable
{
    internal sealed record State(uint Flags, string? Scheme)
    {
        public bool Enabled => (Flags & 1) != 0;
    }
    internal State Original { get; }
    private bool restored;
    private readonly string stages;

    internal NativeContrastSession(string folder)
    {
        stages = Path.Combine(folder, "contrast-native-stages.jsonl");
        if (Environment.GetEnvironmentVariable("CI") != "true" ||
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
            Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") != "github-hosted")
            throw new InvalidOperationException("Windows contrast mutation requires an isolated GitHub-hosted CI runner.");
        Trace("get-original-before");
        Original = Read();
        Trace("get-original-after");
        if (Original.Enabled || (Original.Flags & 2) == 0)
            throw new InvalidOperationException("Contrast transition verification requires contrast available and initially disabled.");
        for (var index = 0; index < 32; index++)
            if (Read() != Original) throw new InvalidOperationException("Windows contrast state changed during the repeated-getter regression.");
        Trace("repeated-getter-verified");
    }

    internal void Enable()
    {
        var requested = Original with { Flags = Original.Flags | 1 };
        Trace("enable-before", requested);
        Write(requested);
        Trace("enable-after", requested, Read());
    }
    internal void Restore()
    {
        if (restored) return;
        Trace("restore-before", Original);
        Write(Original);
        restored = true;
        Trace("restore-after", Original, Read());
    }
    public void Dispose() => Restore();

    internal static State Read()
    {
        var value = new HighContrast { Size = (uint)Marshal.SizeOf<HighContrast>() };
        if (!SystemParametersInfo(0x42, value.Size, ref value, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        // SPI_GETHIGHCONTRASTW returns a reused system buffer on the tested
        // Windows builds. Copy it immediately; freeing it corrupts user32's
        // retained allocation on the next query/theme operation.
        return new State(value.Flags, value.Scheme == IntPtr.Zero ? null : Marshal.PtrToStringUni(value.Scheme));
    }
    private static void Write(State state)
    {
        var pointer = state.Scheme is null ? IntPtr.Zero : Marshal.StringToHGlobalUni(state.Scheme);
        try
        {
            var value = new HighContrast { Size = (uint)Marshal.SizeOf<HighContrast>(), Flags = state.Flags & ~0x1000u, Scheme = pointer };
            // Broadcast the real mode change without requesting a profile write.
            if (!SystemParametersInfo(0x43, value.Size, ref value, 2)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        finally { if (pointer != IntPtr.Zero) Marshal.FreeHGlobal(pointer); }
    }
    internal static Color Color(int index)
    {
        var value = GetSysColor(index);
        return Windows.UI.Color.FromArgb(255, (byte)value, (byte)(value >> 8), (byte)(value >> 16));
    }
    [StructLayout(LayoutKind.Sequential)] private struct HighContrast { public uint Size, Flags; public IntPtr Scheme; }
    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SystemParametersInfo(uint action, uint size, ref HighContrast value, uint flags);
    private void Trace(string phase, State? requested = null, State? observed = null) => File.AppendAllText(stages,
        System.Text.Json.JsonSerializer.Serialize(new { phase, requested, observed,
            timestamp = System.Diagnostics.Stopwatch.GetTimestamp() }) + Environment.NewLine);
    [DllImport("user32.dll")] private static extern uint GetSysColor(int index);
}
