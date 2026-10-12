using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Foundation;

namespace Orchard.Catalog;

/// <summary>Physical fixture input scoped to the catalog's own native window.</summary>
internal sealed class NativeInputSession : IDisposable
{
    private readonly XamlRoot root;
    private readonly CursorPoint originalPointer;
    private readonly string folder, prefix;
    private readonly List<object> pointerMoves = [];
    private bool pointerButtonDown;
    internal IntPtr Window { get; }

    internal NativeInputSession(XamlRoot root, string folder, string prefix)
    {
        this.root = root; this.folder = folder; this.prefix = prefix;
        Window = Microsoft.UI.Win32Interop.GetWindowFromWindowId(root.ContentIslandEnvironment.AppWindowId);
        GetWindowThreadProcessId(Window, out var processId);
        Require(Window != IntPtr.Zero && processId == Environment.ProcessId, "Physical input did not resolve the catalog's own window.");
        Require(GetCursorPos(out originalPointer), "Physical input could not read the original pointer position.");
    }

    internal async Task AcquireAsync(FrameworkElement foregroundTarget)
    {
        var welcome = NativeScreenCapture.MinimizeHostedWelcome(Window);
        if (welcome is not null)
            await File.WriteAllTextAsync(Path.Combine(folder, $"{prefix}-hosted-welcome.json"),
                JsonSerializer.Serialize(welcome, new JsonSerializerOptions { WriteIndented = true }));
        ShowWindow(Window, 3);
        Require(SetWindowPos(Window, new IntPtr(-1), 0, 0, 0, 0, 0x0043), "Could not expose the catalog for physical input.");
        SetForegroundWindow(Window); await Task.Delay(100);
        if (GetForegroundWindow() != Window)
        {
            SendKeyCore(0x12); // Transient Alt clears the hosted desktop's foreground lock.
            SetForegroundWindow(Window); await Task.Delay(100);
        }
        if (GetForegroundWindow() != Window) await ClickAsync(foregroundTarget, requireForeground: false);
        Require(GetForegroundWindow() == Window, "Catalog did not acquire foreground input.");
    }

    internal void Key(ushort key)
    {
        Require(GetForegroundWindow() == Window, "Physical keyboard input lost the catalog foreground window.");
        SendKeyCore(key);
    }

    internal async Task ClickAsync(FrameworkElement target, bool requireForeground = true)
    {
        await MoveAsync(target, requireForeground);
        PointerButton(true, requireForeground);
        PointerButton(false, requireForeground);
        await Task.Delay(100);
    }

    internal async Task MoveAsync(FrameworkElement target, bool requireForeground = true)
    {
        if (requireForeground) Require(GetForegroundWindow() == Window, "Physical pointer input lost the catalog foreground window.");
        target.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false, VerticalAlignmentRatio = .5 });
        await Task.Delay(100);
        var bounds = ScreenBounds(target);
        Require(bounds.Width > 0 && bounds.Height > 0, "Physical pointer target has no bounds.");
        var x = (int)(bounds.X + bounds.Width / 2); var y = (int)(bounds.Y + bounds.Height / 2);
        if (!NativeScreenCapture.OwnsScreenPoint(Window, x, y))
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"{prefix}-hit-target.json"),
                JsonSerializer.Serialize(NativeScreenCapture.DescribePoint(Window, x, y), new JsonSerializerOptions { WriteIndented = true }));
            throw new InvalidOperationException($"Physical target {target.Name} at {x},{y} does not belong to the catalog.");
        }
        var virtualX = GetSystemMetrics(76); var virtualY = GetSystemMetrics(77);
        var virtualWidth = GetSystemMetrics(78); var virtualHeight = GetSystemMetrics(79);
        Require(virtualWidth > 1 && virtualHeight > 1 && x >= virtualX && y >= virtualY &&
            x < virtualX + virtualWidth && y < virtualY + virtualHeight, "Physical target exceeds the virtual desktop.");
        // Send a mouse movement event through the same input path as button and
        // keyboard actions. Absolute virtual-desktop coordinates avoid pointer
        // acceleration and preserve the user's mouse settings.
        Send([new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput
        {
            X = (int)Math.Round((x - virtualX) * 65535d / (virtualWidth - 1)),
            Y = (int)Math.Round((y - virtualY) * 65535d / (virtualHeight - 1)),
            Flags = 0x0001 | 0x8000 | 0x4000 // MOVE | ABSOLUTE | VIRTUALDESK
        } } }]);
        await Task.Delay(30);
        var positioned = GetCursorPos(out var actual);
        pointerMoves.Add(new { target = target.Name, bounds, requested = new { x, y },
            positioned, actual = new { actual.X, actual.Y },
            hit = NativeScreenCapture.DescribePoint(Window, actual.X, actual.Y) });
        Require(positioned && bounds.Contains(new Point(actual.X, actual.Y)) &&
            NativeScreenCapture.OwnsScreenPoint(Window, actual.X, actual.Y), "Physical pointer movement missed its owned target.");
    }

    internal void PointerButton(bool down, bool requireForeground = true)
    {
        if (requireForeground) Require(GetForegroundWindow() == Window, "Catalog lost foreground before its pointer action.");
        Require(GetCursorPos(out var point) && NativeScreenCapture.OwnsScreenPoint(Window, point.X, point.Y),
            "Physical pointer action lost its owned screen point.");
        Send([new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput { Flags = down ? 0x0002u : 0x0004u } } }]);
        pointerButtonDown = down;
    }

    internal Rect ScreenBounds(FrameworkElement target)
    {
        var client = NativeScreenCapture.ClientBounds(Window);
        var local = target.TransformToVisual(root.Content).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
        var scale = root.RasterizationScale;
        return new Rect(client.X + local.X * scale, client.Y + local.Y * scale, local.Width * scale, local.Height * scale);
    }

    public void Dispose()
    {
        try
        {
            var positioned = GetCursorPos(out var point);
            File.WriteAllText(Path.Combine(folder, $"{prefix}-desktop.json"),
                JsonSerializer.Serialize(new { window = Window.ToInt64(), foregroundWindow = GetForegroundWindow().ToInt64(),
                    sessionId = Process.GetCurrentProcess().SessionId, desktop = NativeScreenCapture.DescribeDesktop(),
                    currentPoint = positioned ? NativeScreenCapture.DescribePoint(Window, point.X, point.Y) : null, pointerMoves },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            try
            {
                if (pointerButtonDown)
                {
                    Send([new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput { Flags = 0x0004 } } }]);
                    pointerButtonDown = false;
                }
            }
            finally
            {
                SetWindowPos(Window, new IntPtr(-2), 0, 0, 0, 0, 0x0013);
                SetCursorPos(originalPointer.X, originalPointer.Y);
            }
        }
    }

    private static void SendKeyCore(ushort key) => Send(
        [new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key } } },
         new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key, Flags = 2 } } }]);
    private static void Send(NativeInput[] inputs) => Require(SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<NativeInput>()) == inputs.Length,
        "Windows rejected physical fixture input.");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    [StructLayout(LayoutKind.Sequential)] private struct NativeInput { public uint Type; public InputData Data; }
    [StructLayout(LayoutKind.Explicit)] private struct InputData { [FieldOffset(0)] public MouseInput Mouse; [FieldOffset(0)] public KeyboardInput Keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct MouseInput { public int X, Y; public uint MouseData, Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] private struct CursorPoint { public int X, Y; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, NativeInput[] inputs, int size);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern int GetSystemMetrics(int index);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out CursorPoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetCursorPos(int x, int y);
}
