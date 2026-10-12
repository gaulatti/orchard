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
        Require(SetCursorPos(x, y), "Could not position the fixture pointer.");
        await Task.Delay(30);
        if (requireForeground) Require(GetForegroundWindow() == Window, "Catalog lost foreground before its pointer click.");
        Send([new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput { Flags = 0x0002 } } },
              new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput { Flags = 0x0004 } } }]);
        await Task.Delay(100);
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
            File.WriteAllText(Path.Combine(folder, $"{prefix}-desktop.json"),
                JsonSerializer.Serialize(new { window = Window.ToInt64(), foregroundWindow = GetForegroundWindow().ToInt64(),
                    sessionId = Process.GetCurrentProcess().SessionId, desktop = NativeScreenCapture.DescribeDesktop() },
                    new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            SetWindowPos(Window, new IntPtr(-2), 0, 0, 0, 0, 0x0013);
            SetCursorPos(originalPointer.X, originalPointer.Y);
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
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out CursorPoint point);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetCursorPos(int x, int y);
}
