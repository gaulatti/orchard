using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;
using Windows.Graphics;

namespace PackageConsumer;

// Visible input boundary for the exact restored NuGet DLL. No destination is launched.
public sealed class FooterForegroundWindow : Window
{
    private readonly ElementTheme theme;
    private readonly string output;
    private readonly Grid root;
    private readonly BleeckerFooter footer;
    private readonly BleeckerNavItem home = new("#home", "Home");
    private readonly TextBox callerInput = new() { Text = "Caller note", Width = 240 };
    private readonly List<BleeckerNavItem> requests = [];
    private bool running;
    private bool pointerSent;
    private bool keyboardSent;
    private bool pointerInvoked;
    private bool keyboardInvoked;

    public FooterForegroundWindow(ElementTheme theme, string output)
    {
        this.theme = theme;
        this.output = output;
        Title = "Orchard package Footer input";
        root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' />");
        root.RequestedTheme = theme;
        var stack = new StackPanel { Margin = new Thickness(24), Spacing = 16 };
        stack.Children.Add(new TextBlock { Text = $"Independent package Footer · {theme}", FontSize = 24 });
        stack.Children.Add(callerInput);
        footer = new BleeckerFooter
        {
            Width = 1000,
            Brand = new BleeckerFooterBrand { Href = "#brand", BrandName = "Fictional library", Description = "Local component demonstration." },
            Sections = new ObservableCollection<BleeckerFooterSection>
            {
                new() { Title = "Resources", Items = new ObservableCollection<BleeckerNavItem> { home, new("#other", "Other") } }
            }
        };
        footer.ItemInvoked += (_, item) => requests.Add(item);
        stack.Children.Add(footer);
        root.Children.Add(stack);
        root.Loaded += OnLoaded;
        Content = root;
        AppWindow.ResizeClient(new SizeInt32(1100, 760));
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        if (running) return;
        running = true;
        string stage = "mount";
        try
        {
            await Wait(() => footer.IsLoaded && footer.ActualWidth > 0, "Footer did not load.");
            var link = Links().Single(button => button.Name == "FooterSectionLink" && button.DataContext is BleeckerNavLinkContext context && ReferenceEquals(context.Item, home));
            stage = "foreground";
            await AcquireForeground();
            stage = "pointer";
            await Click(link);
            await Wait(() => requests.Count == 1, "Physical pointer did not invoke the Footer item.");
            Require(ReferenceEquals(requests[0], home), "Pointer replaced the original caller item.");
            pointerInvoked = true;

            stage = "custom-keyboard";
            footer.LinkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><HyperlinkButton Content='{Binding Content}' Command='{Binding Command}' Style='{Binding Style}' Tag='{Binding Item}' /></DataTemplate>");
            await Wait(() => Links().Any(button => ReferenceEquals(button.Tag, home) && button.IsLoaded && button.ActualWidth > 0), "Caller-rendered Footer link did not load.");
            root.UpdateLayout();
            var custom = Links().Single(button => ReferenceEquals(button.Tag, home));
            Require(custom.Focus(FocusState.Keyboard), $"Caller-rendered Footer link rejected keyboard focus: loaded={custom.IsLoaded}, width={custom.ActualWidth}, enabled={custom.IsEnabled}, tabStop={custom.IsTabStop}, visible={custom.Visibility}, foreground={GetForegroundWindow() == WinRT.Interop.WindowNative.GetWindowHandle(this)}.");
            Key(0x0D);
            await Wait(() => requests.Count == 2, "Physical Enter did not invoke the caller-rendered Footer link.");
            Require(ReferenceEquals(requests[1], home) && ReferenceEquals(FocusManager.GetFocusedElement(root.XamlRoot), custom),
                "Keyboard routing changed the original item or lost focus.");
            keyboardInvoked = true;
            Require(callerInput.Text == "Caller note", "Footer input changed caller-owned editor content.");
            var brand = Elements(footer).OfType<BleeckerBrandLockup>().Single();
            var expectedInk = theme == ElementTheme.Dark ? Windows.UI.Color.FromArgb(255, 240, 244, 248) : Windows.UI.Color.FromArgb(255, 45, 45, 45);
            Require(brand.Foreground is SolidColorBrush ink && ink.Color == expectedInk && ink.Opacity == 1,
                "Packaged Footer brand did not use its active theme text token.");
            Require(GetForegroundWindow() == WinRT.Interop.WindowNative.GetWindowHandle(this), "Footer test lost its owned foreground window.");
            await Write(true, stage, null);
            Close(); Environment.Exit(0);
        }
        catch (Exception error)
        {
            await Write(false, stage, error.ToString());
            Close(); Environment.Exit(1);
        }
    }

    private HyperlinkButton[] Links() => Elements(footer).OfType<HyperlinkButton>().ToArray();
    private static IEnumerable<DependencyObject> Elements(DependencyObject parent)
    {
        yield return parent;
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            foreach (var child in Elements(VisualTreeHelper.GetChild(parent, index))) yield return child;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static async Task Wait(Func<bool> condition, string message)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(3)) await Task.Delay(16);
        Require(condition(), message);
    }
    private async Task AcquireForeground()
    {
        var own = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Require(own != 0 && IsWindowVisible(own), "Footer consumer window is not visible.");
        Activate(); SetForegroundWindow(own);
        if (GetForegroundWindow() != own)
        {
            Key(0x12, requireForeground: false); // transient Alt permits the normal Windows foreground request
            await Task.Delay(16); SetForegroundWindow(own);
        }
        await Wait(() => GetForegroundWindow() == own, "Footer consumer did not acquire its own foreground window.");
    }
    private async Task Click(FrameworkElement target)
    {
        var own = WinRT.Interop.WindowNative.GetWindowHandle(this);
        Require(GetForegroundWindow() == own, "Footer pointer input lost the owned foreground window.");
        var box = target.TransformToVisual(root).TransformBounds(new Rect(0, 0, target.ActualWidth, target.ActualHeight));
        Require(box.Width > 0 && box.Height > 0, "Footer link has no pointer target.");
        var origin = new NativePoint();
        Require(ClientToScreen(own, ref origin), "Could not map Footer client coordinates to screen.");
        var scale = root.XamlRoot.RasterizationScale;
        Require(SetCursorPos(origin.X + (int)Math.Round((box.X + box.Width / 2) * scale),
            origin.Y + (int)Math.Round((box.Y + box.Height / 2) * scale)), "Could not position the native pointer.");
        await Task.Delay(80);
        var clicks = new[]
        {
            new NativeInput { Type = 0, Data = new NativeInputData { Mouse = new NativeMouse { Flags = 0x0002 } } },
            new NativeInput { Type = 0, Data = new NativeInputData { Mouse = new NativeMouse { Flags = 0x0004 } } }
        };
        Require(SendInput(2, clicks, Marshal.SizeOf<NativeInput>()) == 2, "Windows rejected Footer pointer input.");
        pointerSent = true;
    }
    private void Key(ushort key, bool requireForeground = true)
    {
        if (requireForeground) Require(GetForegroundWindow() == WinRT.Interop.WindowNative.GetWindowHandle(this), "Footer keyboard input lost foreground.");
        var keys = new[]
        {
            new NativeInput { Type = 1, Data = new NativeInputData { Keyboard = new NativeKeyboard { VirtualKey = key } } },
            new NativeInput { Type = 1, Data = new NativeInputData { Keyboard = new NativeKeyboard { VirtualKey = key, Flags = 2 } } }
        };
        Require(SendInput(2, keys, Marshal.SizeOf<NativeInput>()) == 2, "Windows rejected Footer keyboard input.");
        if (requireForeground) keyboardSent = true;
    }
    private Task Write(bool passed, string stage, string? error)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        GetWindowThreadProcessId(GetForegroundWindow(), out var foregroundOwnerPid);
        return File.WriteAllTextAsync(output, JsonSerializer.Serialize(new
        {
            passed, stage, error, theme = theme.ToString(), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            osArchitecture = RuntimeInformation.OSArchitecture.ToString(), foregroundOwnerPid,
            originalItemInvokedByPointer = pointerInvoked, originalItemInvokedByKeyboard = keyboardInvoked,
            customRendererUsed = keyboardInvoked, keyboardFocusRetained = keyboardInvoked,
            callerInputRetained = passed, themeBrandInkVerified = passed, pointerSent, keyboardSent,
            librarySha256 = Hash(typeof(BleeckerFooter).Assembly.Location), hostSha256 = Hash(typeof(FooterForegroundWindow).Assembly.Location),
            nativeComponentAccepted = false
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeInput { internal uint Type; internal NativeInputData Data; }
    [StructLayout(LayoutKind.Explicit)] private struct NativeInputData { [FieldOffset(0)] internal NativeMouse Mouse; [FieldOffset(0)] internal NativeKeyboard Keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeMouse { internal int X, Y; internal uint Data, Flags, Time; internal UIntPtr ExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeKeyboard { internal ushort VirtualKey, ScanCode; internal uint Flags, Time; internal UIntPtr ExtraInfo; }
    [DllImport("user32.dll")] private static extern uint SendInput(uint count, [In] NativeInput[] inputs, int size);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint window, ref NativePoint point);
}
