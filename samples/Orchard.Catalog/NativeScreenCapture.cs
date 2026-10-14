using System.Runtime.InteropServices;
using Windows.Foundation;

namespace Orchard.Catalog;

// Hosted desktop verification only. Screen pixels include composition child
// visuals that RenderTargetBitmap omits, including the switch thumb shadow.
internal static class NativeScreenCapture
{
    internal static object DescribeDesktop()
    {
        var input = OpenInputDesktop(0, false, 1); // DESKTOP_READOBJECTS
        var inputError = input == IntPtr.Zero ? Marshal.GetLastWin32Error() : 0;
        try { return new { station = Name(GetProcessWindowStation()), thread = Name(GetThreadDesktop(GetCurrentThreadId())),
            input = input == IntPtr.Zero ? null : Name(input), inputError,
            activeConsoleSession = WTSGetActiveConsoleSessionId(), connectionState = ConnectionState() }; }
        finally { if (input != IntPtr.Zero) CloseDesktop(input); }

        static string? Name(IntPtr desktop)
        {
            var name = new System.Text.StringBuilder(256);
            return GetUserObjectInformation(desktop, 2, name, 512, out _) ? name.ToString() : null;
        }
        static int? ConnectionState()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            if (!WTSQuerySessionInformation(IntPtr.Zero, process.SessionId, 8, out var data, out var length)) return null;
            try { return length >= 4 ? Marshal.ReadInt32(data) : null; }
            finally { WTSFreeMemory(data); }
        }
    }

    internal static Rect ClientBounds(IntPtr window)
    {
        var origin = new ScreenPoint(0, 0);
        if (!GetClientRect(window, out var client) || !ClientToScreen(window, ref origin))
            throw new InvalidOperationException("Could not locate the catalog client on screen.");
        return new Rect(origin.X, origin.Y, client.Right - client.Left, client.Bottom - client.Top);
    }

    internal static bool OwnsScreenPoint(IntPtr window, int x, int y) =>
        GetAncestor(WindowFromPoint(new ScreenPoint(x, y)), 2) == window;

    internal static object DescribePoint(IntPtr window, int x, int y)
    {
        var hit = WindowFromPoint(new ScreenPoint(x, y));
        var root = GetAncestor(hit, 2);
        var foreground = GetForegroundWindow();
        return new { x, y, own = Describe(window), hit = Describe(hit), root = Describe(root), foreground = Describe(foreground) };

        static object Describe(IntPtr target)
        {
            var name = new System.Text.StringBuilder(256);
            GetClassName(target, name, name.Capacity);
            var thread = GetWindowThreadProcessId(target, out var process);
            var session = process != 0 && ProcessIdToSessionId(process, out var sessionId) ? (uint?)sessionId : null;
            var status = DwmGetWindowAttribute(target, 14, out var cloaked, 4); // DWMWA_CLOAKED
            string? executable = null, processReadError = null;
            if (process != 0)
            {
                try { using var owner = System.Diagnostics.Process.GetProcessById((int)process); executable = owner.ProcessName; }
                catch (ArgumentException) { processReadError = "exited"; }
                catch (InvalidOperationException) { processReadError = "exited"; }
                catch (System.ComponentModel.Win32Exception error) { processReadError = $"win32-{error.NativeErrorCode}"; }
            }
            var application = ApplicationIdentity(process);
            return new { handle = target.ToInt64(), windowClass = name.ToString(), process, thread, executable, processReadError, application, session,
                topmost = (GetWindowLongPtr(target, -20).ToInt64() & 8) != 0,
                visible = IsWindowVisible(target), cloaked = status >= 0 ? (uint?)cloaked : null };
        }
    }

    internal static object? MinimizeHostedWelcome(IntPtr catalog)
    {
        if (Environment.GetEnvironmentVariable("CI") != "true")
            throw new InvalidOperationException("Welcome-window preparation is restricted to hosted CI.");
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero || window == catalog) return null;
        GetWindowThreadProcessId(window, out var process);
        var application = ApplicationIdentity(process);
        if (application.id != "Microsoft.Windows.CloudExperienceHost_cw5n1h2txyewy!App") return null;
        using var owner = System.Diagnostics.Process.GetProcessById((int)process);
        using var current = System.Diagnostics.Process.GetCurrentProcess();
        if (owner.SessionId != current.SessionId)
            throw new InvalidOperationException("Hosted welcome window is outside the catalog session.");
        // Minimize only the identified first-login window on the isolated runner.
        // Leave welcome choices, process lifetime and arbitrary apps alone.
        var requested = ShowWindowAsync(window, 6); // SW_MINIMIZE
        object? backdrop = null;
        var backdropWindow = FindWindow("UserOOBEWindowClass", null);
        if (backdropWindow != IntPtr.Zero)
        {
            GetWindowThreadProcessId(backdropWindow, out var backdropProcess);
            using var backdropOwner = System.Diagnostics.Process.GetProcessById((int)backdropProcess);
            if (!string.Equals(backdropOwner.ProcessName, "explorer", StringComparison.OrdinalIgnoreCase) || backdropOwner.SessionId != current.SessionId)
                throw new InvalidOperationException("Hosted welcome backdrop does not match its observed explorer/session boundary.");
            var backdropRequested = ShowWindowAsync(backdropWindow, 6);
            backdrop = new { window = backdropWindow.ToInt64(), process = backdropProcess,
                executable = backdropOwner.ProcessName, windowClass = "UserOOBEWindowClass", backdropOwner.SessionId, minimizeRequested = backdropRequested };
        }
        return new { window = window.ToInt64(), process, application, owner.SessionId, minimizeRequested = requested, backdrop };
    }

    private sealed record ApplicationInfo(string? id, int status);

    private static ApplicationInfo ApplicationIdentity(uint process)
    {
        var handle = OpenProcess(0x1000, false, process); // PROCESS_QUERY_LIMITED_INFORMATION
        if (handle == IntPtr.Zero) return new(null, Marshal.GetLastWin32Error());
        try
        {
            uint length = 0;
            var status = GetApplicationUserModelId(handle, ref length, null);
            if (status != 122) return new(null, status);
            var id = new System.Text.StringBuilder(checked((int)length));
            status = GetApplicationUserModelId(handle, ref length, id);
            return new(status == 0 ? id.ToString() : null, status);
        }
        finally { CloseHandle(handle); }
    }

    internal static async Task<Rect> CaptureClientAsync(IntPtr window, string path)
    {
        GetWindowThreadProcessId(window, out var process);
        if (process != Environment.ProcessId || GetForegroundWindow() != window)
            throw new InvalidOperationException("Composed capture requires the catalog's own foreground window.");
        var client = ClientBounds(window);
        var origin = new ScreenPoint((int)client.X, (int)client.Y);
        var width = (int)client.Width;
        var height = (int)client.Height;
        if (width <= 0 || height <= 0) throw new InvalidOperationException("Catalog client has no screen area.");
        var monitor = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(window, 2), ref monitor) || client.Left < monitor.Bounds.Left || client.Top < monitor.Bounds.Top ||
            client.Right > monitor.Bounds.Right || client.Bottom > monitor.Bounds.Bottom)
            throw new InvalidOperationException("Catalog client exceeds its physical monitor bounds.");
        foreach (var point in new[] { new ScreenPoint(origin.X + 1, origin.Y + 1),
            new ScreenPoint(origin.X + width - 2, origin.Y + height - 2), new ScreenPoint(origin.X + width / 2, origin.Y + height / 2) })
            if (GetAncestor(WindowFromPoint(point), 2) != window)
                throw new InvalidOperationException("Catalog client is occluded or outside the interactive screen.");
        if (DwmFlush() < 0) throw new InvalidOperationException("Desktop composition did not flush before capture.");

        var screen = GetDC(IntPtr.Zero);
        var memory = IntPtr.Zero;
        var bitmap = IntPtr.Zero;
        var previous = IntPtr.Zero;
        var pixels = new byte[checked(width * height * 4)];
        try
        {
            memory = CreateCompatibleDC(screen);
            bitmap = CreateCompatibleBitmap(screen, width, height);
            if (screen == IntPtr.Zero || memory == IntPtr.Zero || bitmap == IntPtr.Zero)
                throw new InvalidOperationException("Could not allocate the catalog screen capture.");
            previous = SelectObject(memory, bitmap);
            if (previous == IntPtr.Zero || previous == new IntPtr(-1))
                throw new InvalidOperationException("Could not select the catalog capture bitmap.");
            if (!BitBlt(memory, 0, 0, width, height, screen, origin.X, origin.Y, 0x40CC0020))
                throw new InvalidOperationException("Could not copy the composed catalog pixels.");
            SelectObject(memory, previous); previous = IntPtr.Zero;
            var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
            if (GetDIBits(memory, bitmap, 0, (uint)height, pixels, ref info, 0) != height)
                throw new InvalidOperationException("Could not read the composed catalog bitmap.");
        }
        finally
        {
            if (previous != IntPtr.Zero && previous != new IntPtr(-1)) SelectObject(memory, previous);
            if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
            if (memory != IntPtr.Zero) DeleteDC(memory);
            if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
        }
        // BI_RGB reserves the fourth byte; the copied screen is fully opaque.
        for (var offset = 3; offset < pixels.Length; offset += 4) pixels[offset] = 255;
        if (pixels.Where((_, offset) => offset % 4 != 3).Distinct().Count() <= 8)
            throw new InvalidOperationException("Composed catalog capture is blank.");
        await MainWindow.SavePngAsync(path, pixels, width, height);
        return new Rect(origin.X, origin.Y, width, height);
    }

    [StructLayout(LayoutKind.Sequential)] private readonly struct ScreenPoint(int x, int y) { public readonly int X = x, Y = y; }
    [StructLayout(LayoutKind.Sequential)] private struct ScreenRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public uint Size; public ScreenRect Bounds, Work; public uint Flags; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, SizeImage; public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant, Color;
    }
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindowAsync(IntPtr window, int command);
    [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern IntPtr FindWindow(string windowClass, string? title);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint process);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseHandle(IntPtr handle);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern int GetApplicationUserModelId(IntPtr process, ref uint length, System.Text.StringBuilder? id);
    [DllImport("kernel32.dll")] private static extern uint WTSGetActiveConsoleSessionId();
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ProcessIdToSessionId(uint process, out uint session);
    [DllImport("wtsapi32.dll", EntryPoint = "WTSQuerySessionInformationW", ExactSpelling = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool WTSQuerySessionInformation(IntPtr server, int session, int information, out IntPtr value, out uint length);
    [DllImport("wtsapi32.dll")] private static extern void WTSFreeMemory(IntPtr memory);
    [DllImport("user32.dll")] private static extern IntPtr GetProcessWindowStation();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern int GetClassName(IntPtr window, System.Text.StringBuilder name, int length);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", ExactSpelling = true)] private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);
    [DllImport("user32.dll")] private static extern IntPtr GetThreadDesktop(uint thread);
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr OpenInputDesktop(uint flags, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint access);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool CloseDesktop(IntPtr desktop);
    [DllImport("user32.dll", EntryPoint = "GetUserObjectInformationW", CharSet = CharSet.Unicode, ExactSpelling = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetUserObjectInformation(IntPtr obj, int index, System.Text.StringBuilder value, uint length, out uint needed);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetClientRect(IntPtr window, out ScreenRect rect);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ClientToScreen(IntPtr window, ref ScreenPoint point);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(ScreenPoint point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleBitmap(IntPtr dc, int width, int height);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool BitBlt(IntPtr destination, int x, int y, int width, int height, IntPtr source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] private static extern int GetDIBits(IntPtr dc, IntPtr bitmap, uint start, uint lines, [Out] byte[] pixels, ref BitmapInfo info, uint usage);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr window, uint attribute, out uint value, uint length);
}
