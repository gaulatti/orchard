using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class RenderOwnerLifetimeFixture
{
    private sealed record Observation(int Attempt, bool OwnerAlive, string AsyncStatus, bool ManagedTaskCompleted, int SubsequentStrongFramesCompleted);
    private delegate bool Visitor(nint window, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<RenderTargetBitmap> weak) => weak.TryGetTarget(out _);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (IAsyncAction Action, Task Task, WeakReference<RenderTargetBitmap> Weak) StartUnretained(Grid root)
    {
        var bitmap = new RenderTargetBitmap(); var operation = bitmap.RenderAsync(root);
        return (operation, operation.AsTask(), new WeakReference<RenderTargetBitmap>(bitmap));
    }
    internal static async Task RunAsync(Window target, string output, string hostAssembly)
    {
        var root = new Grid { Width = 400, Height = 200, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 27, 30, 36)) };
        root.Children.Add(new TextBlock { Text = "Native capture lifetime control", Margin = new Thickness(20), Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) });
        target.Content = root; target.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(400, 200));
        var guards = 0; var activations = 0; var focus = 0; var frames = 0; var forcedCollections = 0; var observations = new List<Observation>();
        root.GotFocus += (_, _) => focus++;
        target.Activated += (_, e) => { if (e.WindowActivationState != WindowActivationState.Deactivated) activations++; };
        string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var pid); visible |= pid == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            if (visible || target.Visible || target.AppWindow.IsVisible || foreground == Environment.ProcessId || activations != 0 || focus != 0) throw new InvalidOperationException("Native lifetime fixture left hidden boundary.");
        }
        async Task StrongFrame(bool forceCollection)
        {
            Guard(); root.Measure(new Size(400, 200)); root.Arrange(new Rect(0, 0, 400, 200)); root.UpdateLayout();
            await NativeRenderReadiness.RenderAsync(root, forceCollection ? weak =>
            {
                forcedCollections++; GC.Collect(2, GCCollectionMode.Forced, true, true);
                if (!Alive(weak)) throw new InvalidOperationException("Readiness owner was collected before completion.");
            } : null);
            Guard(); if (!root.IsLoaded) throw new InvalidOperationException("Retained capture did not load the native root."); frames++;
        }
        try
        {
            var native = Path.Combine(AppContext.BaseDirectory, "Microsoft.UI.Xaml.dll");
            var uiHash = Hash(native);
            if (uiHash != "aad12524765e6fb63f0ae26a45a9ba3104f24fde66413d8a3036fbed74a1e990") throw new InvalidOperationException("Lifetime counterexample must use the pinned WinUI build.");
            await StrongFrame(true);
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var orphan = StartUnretained(root);
                forcedCollections++; GC.Collect(2, GCCollectionMode.Forced, true, true);
                await StrongFrame(true); await Task.Delay(50); await StrongFrame(false);
                observations.Add(new(attempt, Alive(orphan.Weak), orphan.Action.Status.ToString(), orphan.Task.IsCompleted, 2));
            }
            var exact = observations.All(row => !row.OwnerAlive && row.AsyncStatus == "Started" && !row.ManagedTaskCompleted);
            Guard();
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-render-owner-lifetime-native-v1", passed = exact, retainedFramesCompleted = frames, forcedCollections, observations, guards, focus, activations, winUiSha256 = uiHash, framework = Environment.Version.ToString(), sharedReadinessHelperVerified = true, forcedGcIsDiagnosticOnly = true, showCalled = false, activateCalled = false, focusCalled = false, hardwareInputSent = false, osSettingsChanged = false, nativeComponentAccepted = false, librarySha256 = Hash(typeof(BleeckerButton).Assembly.Location), hostSha256 = Hash(hostAssembly) }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            target.Close(); Environment.Exit(exact ? 0 : 1);
        }
        catch (Exception error)
        {
            await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-render-owner-lifetime-native-v1", passed = false, error = error.ToString(), observations, guards, focus, activations }, new JsonSerializerOptions { WriteIndented = true }));
            target.Close(); Environment.Exit(1);
        }
    }
}
