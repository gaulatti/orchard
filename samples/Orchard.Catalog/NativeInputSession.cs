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
    private bool hostedForegroundRecoveryAttempted;
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
        var welcome = Environment.GetEnvironmentVariable("CI") == "true"
            ? NativeScreenCapture.MinimizeHostedWelcome(Window) : null;
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

    internal void ShiftKey(ushort key)
    {
        Require(OwnsForeground, "Shift keyboard input lost the catalog foreground window.");
        Send([new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = 0x10 } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key, Flags = 2 } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = 0x10, Flags = 2 } } }]);
    }

    internal async Task ClickAsync(FrameworkElement target, bool requireForeground = true)
    {
        await MoveAsync(target, requireForeground);
        PointerButton(true, requireForeground);
        PointerButton(false, requireForeground);
        await Task.Delay(100);
    }

    internal async Task MoveAsync(FrameworkElement target, bool requireForeground = true, Point? localPoint = null)
    {
        if (requireForeground) await RequireForegroundAsync();
        target.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false, VerticalAlignmentRatio = .5 });
        await Task.Delay(100);
        var bounds = ScreenBounds(target);
        Require(bounds.Width > 0 && bounds.Height > 0, "Physical pointer target has no bounds.");
        if (localPoint is Point local) Require(local.X >= 0 && local.Y >= 0 && local.X < target.ActualWidth && local.Y < target.ActualHeight,
            "Physical pointer offset exceeds its target bounds.");
        var scale = root.RasterizationScale;
        var x = (int)(bounds.X + (localPoint?.X * scale ?? bounds.Width / 2));
        var y = (int)(bounds.Y + (localPoint?.Y * scale ?? bounds.Height / 2));
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
        pointerMoves.Add(new { target = target.Name, bounds, localPoint, requested = new { x, y },
            positioned, actual = new { actual.X, actual.Y },
            hit = NativeScreenCapture.DescribePoint(Window, actual.X, actual.Y) });
        Require(positioned && bounds.Contains(new Point(actual.X, actual.Y)) &&
            NativeScreenCapture.OwnsScreenPoint(Window, actual.X, actual.Y), "Physical pointer movement missed its owned target.");
        if (localPoint is not null) Require(actual.X == x && actual.Y == y, "Physical pointer movement missed its exact boundary sample.");
    }

    internal bool OwnsForeground => GetForegroundWindow() == Window;
    internal bool OwnsCurrentPoint => GetCursorPos(out var point) && NativeScreenCapture.OwnsScreenPoint(Window, point.X, point.Y);
    internal bool HasForeignForeground
    {
        get
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            GetWindowThreadProcessId(foreground, out var processId);
            return processId != 0 && processId != Environment.ProcessId;
        }
    }

    internal object DescribeCurrentPoint()
    {
        Require(GetCursorPos(out var point), "Could not inspect the current native pointer point.");
        return NativeScreenCapture.DescribePoint(Window, point.X, point.Y);
    }

    internal async Task RequireForegroundAsync()
    {
        if (!OwnsForeground) await RestoreHostedForegroundAsync();
        Require(OwnsForeground, "Physical input lost the catalog foreground window.");
    }

    private async Task RestoreHostedForegroundAsync()
    {
        // A hosted runner can open a background setup console during a suite.
        // Recover only our still-visible surface, before any button is held.
        // An Orchard-owned popup/window remains an application test failure.
        if (Environment.GetEnvironmentVariable("CI") != "true" || hostedForegroundRecoveryAttempted || pointerButtonDown) return;
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero || foreground == Window) return;
        GetWindowThreadProcessId(foreground, out var processId);
        if (processId == 0 || processId == Environment.ProcessId || !GetCursorPos(out var point) ||
            !NativeScreenCapture.OwnsScreenPoint(Window, point.X, point.Y)) return;
        hostedForegroundRecoveryAttempted = true;
        var before = NativeScreenCapture.DescribePoint(Window, point.X, point.Y);
        var requested = SetForegroundWindow(Window);
        await Task.Delay(100);
        await File.WriteAllTextAsync(Path.Combine(folder, $"{prefix}-foreground-recovery.json"),
            JsonSerializer.Serialize(new { requested, restored = GetForegroundWindow() == Window, before,
                after = NativeScreenCapture.DescribePoint(Window, point.X, point.Y) },
                new JsonSerializerOptions { WriteIndented = true }));
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

    internal static IntPtr CurrentVisibleCursor()
    {
        var info = new CursorInformation { Size = (uint)Marshal.SizeOf<CursorInformation>() };
        Require(GetCursorInfo(ref info), "Could not read the global Windows cursor.");
        return (info.Flags & 1) != 0 ? info.Cursor : IntPtr.Zero;
    }

    internal static object DescribeCursor()
    {
        var info = new CursorInformation { Size = (uint)Marshal.SizeOf<CursorInformation>() };
        Require(GetCursorInfo(ref info), "Could not inspect the global Windows cursor.");
        var thread = GetCursor();
        return new { global = info.Cursor.ToInt64(), info.Flags, thread = thread.ToInt64(),
            position = new { info.Position.X, info.Position.Y },
            systemShapes = new[] { (Name: "Arrow", Id: 32512), (Name: "IBeam", Id: 32513),
                (Name: "Wait", Id: 32514), (Name: "UniversalNo", Id: 32648),
                (Name: "Hand", Id: 32649), (Name: "AppStarting", Id: 32650) }
                .Select(shape => new { shape.Name, handle = LoadCursor(IntPtr.Zero, new IntPtr(shape.Id)).ToInt64() }).ToArray() };
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

    [StructLayout(LayoutKind.Sequential)] private struct CursorInformation
    {
        public uint Size, Flags;
        public IntPtr Cursor;
        public CursorPoint Position;
    }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorInfo(ref CursorInformation info);
    [DllImport("user32.dll")] private static extern IntPtr GetCursor();
    [DllImport("user32.dll", EntryPoint = "LoadCursorW")] private static extern IntPtr LoadCursor(IntPtr instance, IntPtr name);

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
