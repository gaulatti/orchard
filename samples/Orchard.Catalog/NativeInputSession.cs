using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Windows.Foundation;

namespace Orchard.Catalog;

/// <summary>Physical fixture input scoped to the catalog's own native window.</summary>
internal sealed partial class NativeInputSession : IDisposable
{
    private readonly XamlRoot root;
    private readonly CursorPoint originalPointer;
    private readonly string folder, prefix;
    private readonly List<object> pointerMoves = [];
    private readonly List<object> pointerPresentations = [];
    private readonly List<object> keyActions = [];
    private readonly List<object> pointerClicks = [];
    private bool pointerButtonDown;
    private bool hostedForegroundRecoveryAttempted;
    private CursorPoint? lastDeliveredPointer;
    internal IntPtr Window { get; }
    internal Point ReadPointerPosition()
    {
        Require(GetCursorPos(out var point), "Could not observe the native pointer position.");
        return new Point(point.X, point.Y);
    }

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
        var arguments = Environment.GetCommandLineArgs();
        var sizeIndex = Array.IndexOf(arguments, "--smoke-client-size");
        if (sizeIndex < 0) ShowWindow(Window, 3);
        else
        {
            Require(sizeIndex + 2 < arguments.Length && int.TryParse(arguments[sizeIndex + 1], out _) &&
                int.TryParse(arguments[sizeIndex + 2], out _), "Smoke client size requires explicit integer width and height.");
            var width = int.Parse(arguments[sizeIndex + 1]); var height = int.Parse(arguments[sizeIndex + 2]);
            Require(width > 0 && height > 0, "Smoke client width and height must be positive.");
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(root.ContentIslandEnvironment.AppWindowId);
            Require(appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter, "Smoke client sizing requires an overlapped catalog window.");
            if (appWindow.ClientSize.Width != width || appWindow.ClientSize.Height != height)
            {
                ((Microsoft.UI.Windowing.OverlappedPresenter)appWindow.Presenter).Restore(false);
                appWindow.ResizeClient(new Windows.Graphics.SizeInt32(width, height));
                await MainWindow.AwaitPresentationAsync(foregroundTarget);
            }
            Require(appWindow.ClientSize.Width == width && appWindow.ClientSize.Height == height,
                "Catalog did not adopt the explicitly requested client size.");
        }
        // Preserve OS placement for maximized fixtures. Explicit client sizes
        // restore the window, so apply the requested origin instead of retaining
        // a stale centered location that can put a 900px client off screen.
        var exposureFlags = sizeIndex < 0 ? 0x0043u : 0x0041u; // NOSIZE | SHOWWINDOW; NOMOVE only when maximized.
        Require(SetWindowPos(Window, new IntPtr(-1), 0, 0, 0, 0, exposureFlags), "Could not expose the catalog for physical input.");
        if (sizeIndex >= 0) await MainWindow.AwaitPresentationAsync(foregroundTarget);
        if (GetForegroundWindow() != Window) { SetForegroundWindow(Window); await Task.Delay(100); }
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

    internal async Task KeyAsync(ushort key, FrameworkElement target)
    {
        await RequireForegroundAsync();
        Require(ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root), target),
            "Physical keyboard target does not own XAML focus.");
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var downs = 0; var ups = 0; var targetRetained = true;
        var down = new Microsoft.UI.Xaml.Input.KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key != key) return;
            downs++; targetRetained &= ReferenceEquals(args.OriginalSource, target);
        });
        var up = new Microsoft.UI.Xaml.Input.KeyEventHandler((_, args) =>
        {
            if ((ushort)args.Key != key) return;
            ups++; targetRetained &= ReferenceEquals(args.OriginalSource, target); released.TrySetResult();
        });
        root.Content.AddHandler(UIElement.KeyDownEvent, down, true);
        root.Content.AddHandler(UIElement.KeyUpEvent, up, true);
        try
        {
            Key(key);
            await released.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Require(downs == 1 && ups == 1 && targetRetained,
                "Physical keyboard action did not deliver exactly one press/release to its focused target.");
        }
        finally
        {
            root.Content.RemoveHandler(UIElement.KeyDownEvent, down);
            root.Content.RemoveHandler(UIElement.KeyUpEvent, up);
            keyActions.Add(new { key, target = target.Name, downs, ups, targetRetained });
        }
    }

    internal void ShiftKey(ushort key)
    {
        Require(OwnsForeground, "Shift keyboard input lost the catalog foreground window.");
        Send([new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = 0x10 } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = key, Flags = 2 } } },
            new NativeInput { Type = 1, Data = new InputData { Keyboard = new KeyboardInput { Key = 0x10, Flags = 2 } } }]);
    }

    internal async Task ClickAsync(FrameworkElement target, bool requireForeground = true, Point? localPoint = null)
    {
        await MoveAsync(target, requireForeground, localPoint);
        var expected = ReadPointerPosition(); var watch = Stopwatch.StartNew(); var receipts = new List<object>();
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var roots = new HashSet<UIElement> { root.Content, target };
        foreach (var popup in Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)) if (popup.Child is UIElement child) roots.Add(child);
        var released = new Microsoft.UI.Xaml.Input.PointerEventHandler((_, args) =>
        {
            var point = args.GetCurrentPoint(root.Content).Position; var client = NativeScreenCapture.ClientBounds(Window);
            var x = client.X + point.X * root.RasterizationScale; var y = client.Y + point.Y * root.RasterizationScale;
            var exact = !args.IsGenerated && args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse && !args.GetCurrentPoint(root.Content).Properties.IsLeftButtonPressed && Math.Abs(x - expected.X) < .001 && Math.Abs(y - expected.Y) < .001;
            receipts.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, args.IsGenerated, x, y, exact });
            if (exact) delivered.TrySetResult();
        });
        foreach (var element in roots) element.AddHandler(UIElement.PointerReleasedEvent, released, true);
        try
        {
            if (requireForeground) await PressPointerAsync(); else PointerButton(true, false);
            PointerButton(false, requireForeground); await delivered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            foreach (var element in roots) element.RemoveHandler(UIElement.PointerReleasedEvent, released);
            pointerClicks.Add(new { target = target.Name, expected, delivered = delivered.Task.IsCompletedSuccessfully, receipts });
        }
    }

    internal async Task PressPointerAsync()
    {
        await RequireForegroundAsync();
        PointerButton(true);
    }

    internal async Task MoveAsync(FrameworkElement target, bool requireForeground = true, Point? localPoint = null)
    {
        if (requireForeground) await RequireForegroundAsync();
        var initialViewports = JsonSerializer.Serialize(DescribeViewports(target));
        target.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = false, VerticalAlignmentRatio = .5 });
        var bounds = await AwaitPointerPresentationAsync(target, localPoint, initialViewports);
        Require(bounds.Width > 0 && bounds.Height > 0, "Physical pointer target has no bounds.");
        if (localPoint is Point local) Require(local.X >= 0 && local.Y >= 0 && local.X < target.ActualWidth && local.Y < target.ActualHeight,
            "Physical pointer offset exceeds its target bounds.");
        var scale = root.RasterizationScale;
        var x = (int)(bounds.X + (localPoint?.X * scale ?? bounds.Width / 2));
        var y = (int)(bounds.Y + (localPoint?.Y * scale ?? bounds.Height / 2));
        if (!NativeScreenCapture.OwnsScreenPoint(Window, x, y))
        {
            await File.WriteAllTextAsync(Path.Combine(folder, $"{prefix}-hit-target.json"),
                JsonSerializer.Serialize(new { target.Name, target.IsLoaded, bounds, localPoint, point = NativeScreenCapture.DescribePoint(Window, x, y),
                    viewports = DescribeViewports(target) }, new JsonSerializerOptions { WriteIndented = true }));
            throw new InvalidOperationException($"Physical target {target.Name} at {x},{y} does not belong to the catalog.");
        }
        var receiptWatch = Stopwatch.StartNew(); var receipts = new List<object>();
        var delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var positionKnown = GetCursorPos(out var before);
        var stationary = positionKnown && before.X == x && before.Y == y && lastDeliveredPointer is CursorPoint known && known.X == x && known.Y == y;
        var receiptX = x; var receiptY = y; var receiptStage = "target";
        var receiptRoots = new HashSet<UIElement> { root.Content, target };
        foreach (var popup in Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(root)) if (popup.Child is UIElement child) receiptRoots.Add(child);
        var receipt = new Microsoft.UI.Xaml.Input.PointerEventHandler((_, args) =>
        {
            var position = args.GetCurrentPoint(root.Content).Position;
            var client = NativeScreenCapture.ClientBounds(Window);
            var screenX = client.X + position.X * scale; var screenY = client.Y + position.Y * scale;
            var exact = !args.IsGenerated && args.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Mouse && Math.Abs(screenX - receiptX) < .001 && Math.Abs(screenY - receiptY) < .001;
            receipts.Add(new { milliseconds = receiptWatch.Elapsed.TotalMilliseconds, stage = receiptStage, args.IsGenerated, position, screenX, screenY, exact });
            if (exact) { lastDeliveredPointer = new CursorPoint { X = receiptX, Y = receiptY }; delivered.TrySetResult(); }
        });
        foreach (var element in receiptRoots) element.AddHandler(UIElement.PointerMovedEvent, receipt, true);
        try
        {
            if (!stationary && positionKnown && before.X == x && before.Y == y)
            {
                // A new XamlRoot can appear under the existing screen point.
                // Establish a real move in this root before returning to the
                // target; Windows need not route an unchanged absolute move.
                var departure = new[] { (X: x + 1, Y: y), (X: x - 1, Y: y), (X: x, Y: y + 1), (X: x, Y: y - 1) }
                    .Where(point => bounds.Contains(new Point(point.X, point.Y)) && NativeScreenCapture.OwnsScreenPoint(Window, point.X, point.Y))
                    .Take(1).ToArray();
                Require(departure.Length == 1, "Pointer target has no owned departure pixel for a new root receipt.");
                receiptX = departure[0].X; receiptY = departure[0].Y; receiptStage = "departure";
                SendPointerMovement(receiptX, receiptY);
                await delivered.Task.WaitAsync(TimeSpan.FromSeconds(2));
                delivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                receiptX = x; receiptY = y; receiptStage = "target";
            }
            SendPointerMovement(x, y);
            // Windows need not emit PointerMoved for an unchanged position.
            // Reuse only an actual prior receipt at that exact current point;
            // every changed position requires its own real routed mouse event.
            if (!stationary) await delivered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            foreach (var element in receiptRoots) element.RemoveHandler(UIElement.PointerMovedEvent, receipt);
            if (!stationary && !delivered.Task.IsCompletedSuccessfully) pointerMoves.Add(new { target = target.Name, requested = new { x, y }, deliveryFailed = true, receipts });
        }
        var positioned = GetCursorPos(out var actual);
        var finalBounds = ScreenBounds(target);
        pointerMoves.Add(new { target = target.Name, bounds, finalBounds, localPoint, requested = new { x, y },
            positioned, actual = new { actual.X, actual.Y },
            hit = NativeScreenCapture.DescribePoint(Window, actual.X, actual.Y), stationary, delivered = delivered.Task.IsCompletedSuccessfully, deliveryMilliseconds = receiptWatch.Elapsed.TotalMilliseconds, receipts });
        Require(positioned && bounds.Contains(new Point(actual.X, actual.Y)) && finalBounds.Contains(new Point(actual.X, actual.Y)) &&
            NativeScreenCapture.OwnsScreenPoint(Window, actual.X, actual.Y), "Physical pointer movement missed its owned target.");
        if (localPoint is not null) Require(actual.X == x && actual.Y == y, "Physical pointer movement missed its exact boundary sample.");
    }

    private async Task<Rect> AwaitPointerPresentationAsync(FrameworkElement target, Point? localPoint, string initialViewports)
    {
        var ready = new TaskCompletionSource<Rect>(TaskCreationOptions.RunContinuationsAsynchronously);
        var watch = Stopwatch.StartNew(); var observations = new List<object>(); var commits = new List<object>();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2)); var committing = false;
        (Rect Bounds, double Width, double Height, string Viewports, bool Owned)? previous = null;
        var stable = 0; var frames = 0;
        async void Presented(object? sender, object args)
        {
            try
            {
                frames++; if (committing) return;
                if (!target.IsLoaded || target.XamlRoot is null) { previous = null; stable = 0; observations.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, target.IsLoaded }); return; }
                var bounds = ScreenBounds(target); var viewports = DescribeViewports(target);
                var scale = root.RasterizationScale;
                var x = (int)(bounds.X + (localPoint?.X * scale ?? bounds.Width / 2));
                var y = (int)(bounds.Y + (localPoint?.Y * scale ?? bounds.Height / 2));
                var client = NativeScreenCapture.ClientBounds(Window);
                var owned = bounds.Width > 0 && bounds.Height > 0 && NativeScreenCapture.OwnsScreenPoint(Window, x, y);
                var state = (Bounds: bounds, Width: target.ActualWidth, Height: target.ActualHeight, Viewports: JsonSerializer.Serialize(viewports), Owned: owned);
                stable = owned && previous == state ? stable + 1 : 0; previous = state;
                observations.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, bounds, target.ActualWidth, target.ActualHeight, stable, viewports, requested = new { x, y }, client, owned });
                if (stable >= 2)
                {
                    if (state.Viewports != initialViewports)
                    {
                        // Scroll metrics can reach their destination before
                        // TransformToVisual reflects the compositor's scroll.
                        committing = true;
                        try
                        {
                            await Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(target).Compositor.RequestCommitAsync().AsTask().WaitAsync(deadline.Token);
                            var after = (ScreenBounds(target), target.ActualWidth, target.ActualHeight, JsonSerializer.Serialize(DescribeViewports(target)), NativeScreenCapture.OwnsScreenPoint(Window, x, y));
                            var unchanged = state == after; commits.Add(new { milliseconds = watch.Elapsed.TotalMilliseconds, before = bounds, after = after.Item1, unchanged });
                            if (!unchanged) { previous = null; stable = 0; return; }
                        }
                        finally { committing = false; }
                    }
                    ready.TrySetResult(bounds);
                }
            }
            catch (Exception error) { ready.TrySetException(error); }
        }
        Microsoft.UI.Xaml.Media.CompositionTarget.Rendering += Presented;
        try { return await ready.Task.WaitAsync(deadline.Token); }
        finally
        {
            Microsoft.UI.Xaml.Media.CompositionTarget.Rendering -= Presented;
            pointerPresentations.Add(new { target = target.Name, milliseconds = watch.Elapsed.TotalMilliseconds, frames, stable, passed = ready.Task.IsCompletedSuccessfully, observations, commits });
        }
    }

    private static void SendPointerMovement(int x, int y)
    {
        var virtualX = GetSystemMetrics(76); var virtualY = GetSystemMetrics(77);
        var virtualWidth = GetSystemMetrics(78); var virtualHeight = GetSystemMetrics(79);
        Require(virtualWidth > 1 && virtualHeight > 1 && x >= virtualX && y >= virtualY &&
            x < virtualX + virtualWidth && y < virtualY + virtualHeight, "Physical target exceeds the virtual desktop.");
        // Keep Win32 and WinUI's input position on the same native event path,
        // including the one restoration to the session's original location.
        Send([new NativeInput { Type = 0, Data = new InputData { Mouse = new MouseInput
        {
            X = NativeAbsolutePointer.Normalize(x, virtualX, virtualWidth),
            Y = NativeAbsolutePointer.Normalize(y, virtualY, virtualHeight),
            Flags = 0x0001 | 0x8000 | 0x4000 // MOVE | ABSOLUTE | VIRTUALDESK
        } } }]);
    }

    private static object[] DescribeViewports(FrameworkElement target)
    {
        var viewports = new List<object>();
        for (DependencyObject? parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(target); parent is not null; parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(parent))
            if (parent is Microsoft.UI.Xaml.Controls.ScrollViewer viewport)
                viewports.Add(new { viewport.Name, viewport.VerticalOffset, viewport.HorizontalOffset, viewport.ScrollableHeight, viewport.ScrollableWidth,
                    viewport.ViewportHeight, viewport.ViewportWidth, viewport.ExtentHeight, viewport.ExtentWidth });
        return viewports.ToArray();
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

    internal object DescribeInputRouting()
    {
        var thread=GetWindowThreadProcessId(Window,out _);
        var info=new GuiThreadInformation { Size=(uint)Marshal.SizeOf<GuiThreadInformation>() };
        var read=GetGUIThreadInfo(thread,ref info);
        return new { read,info.Flags,active=info.Active.ToInt64(),focus=info.Focus.ToInt64(),capture=info.Capture.ToInt64(),
            menuOwner=info.MenuOwner.ToInt64(),moveSize=info.MoveSize.ToInt64(),caret=info.Caret.ToInt64(),children=DescribeChildren(),
            leftButton=(GetAsyncKeyState(1)&0x8000)!=0,alt=(GetAsyncKeyState(0x12)&0x8000)!=0,enter=(GetAsyncKeyState(0x0D)&0x8000)!=0 };
    }
    private object[] DescribeChildren()
    {
        var children=new List<object>();
        EnumChildWindows(Window,(child,_)=>
        {
            var className=new System.Text.StringBuilder(256);GetClassName(child,className,className.Capacity);GetWindowRect(child,out var rect);
            children.Add(new {handle=child.ToInt64(),windowClass=className.ToString(),visible=IsWindowVisible(child),enabled=IsWindowEnabled(child),rect.Left,rect.Top,rect.Right,rect.Bottom});return true;
        },IntPtr.Zero);
        return children.ToArray();
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
                    virtualDesktop = new { x = GetSystemMetrics(76), y = GetSystemMetrics(77), width = GetSystemMetrics(78), height = GetSystemMetrics(79) },
                    sessionId = Process.GetCurrentProcess().SessionId, desktop = NativeScreenCapture.DescribeDesktop(),
                    currentPoint = positioned ? NativeScreenCapture.DescribePoint(Window, point.X, point.Y) : null, pointerMoves, pointerPresentations, keyActions, pointerClicks },
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
                SendPointerMovement(originalPointer.X, originalPointer.Y);
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
    [StructLayout(LayoutKind.Sequential)] private struct GuiThreadInformation
    {
        public uint Size,Flags; public IntPtr Active,Focus,Capture,MenuOwner,MoveSize,Caret; public NativeRectangle CaretBounds;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRectangle { public int Left,Top,Right,Bottom; }
    [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetGUIThreadInfo(uint thread,ref GuiThreadInformation info);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    private delegate bool ChildWindowCallback(IntPtr child,IntPtr data);
    [DllImport("user32.dll")]private static extern bool EnumChildWindows(IntPtr parent,ChildWindowCallback callback,IntPtr data);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]private static extern int GetClassName(IntPtr window,System.Text.StringBuilder name,int capacity);
    [DllImport("user32.dll")]private static extern bool GetWindowRect(IntPtr window,out NativeRectangle rect);
    [DllImport("user32.dll")]private static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")]private static extern bool IsWindowEnabled(IntPtr window);
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
