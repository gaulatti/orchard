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

    internal NativeContrastSession()
    {
        if (Environment.GetEnvironmentVariable("CI") != "true" ||
            Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
            Environment.GetEnvironmentVariable("RUNNER_ENVIRONMENT") != "github-hosted")
            throw new InvalidOperationException("Windows contrast mutation requires an isolated GitHub-hosted CI runner.");
        Original = Read();
        if (Original.Enabled || (Original.Flags & 2) == 0)
            throw new InvalidOperationException("Contrast transition verification requires contrast available and initially disabled.");
    }

    internal void Enable() => Write(Original with { Flags = Original.Flags | 1 });
    internal void Restore()
    {
        if (restored) return;
        Write(Original);
        restored = true;
    }
    public void Dispose() => Restore();

    internal static State Read()
    {
        var value = new HighContrast { Size = (uint)Marshal.SizeOf<HighContrast>() };
        if (!SystemParametersInfo(0x42, value.Size, ref value, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
        try { return new State(value.Flags, value.Scheme == IntPtr.Zero ? null : Marshal.PtrToStringUni(value.Scheme)); }
        finally { if (value.Scheme != IntPtr.Zero) LocalFree(value.Scheme); }
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
    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr value);
    [DllImport("user32.dll")] private static extern uint GetSysColor(int index);
}
