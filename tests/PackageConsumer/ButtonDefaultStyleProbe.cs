using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

// The first custom control in this fresh process is constructed by C#;
// neither MainWindow nor another catalog fixture initializes its style.
internal static class ButtonDefaultStyleProbe
{
    private sealed class SpacingValues:INotifyPropertyChanged
    {
        private int spacing=48;
        public int Spacing{get=>spacing;set{spacing=value;PropertyChanged?.Invoke(this,new(nameof(Spacing)));}}
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject root)
    {
        if (root is FrameworkElement element) yield return element;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) foreach (var child in Elements(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var root = new Grid { Width = 640, Height = 400 }; window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(640, 400));
        var guards = 0; var activations = 0; var focus = 0; var samples = new List<object>();var spacingSamples=new List<object>();
        window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false;
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Button style probe acquired visibility, activation or focus.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Button style probe opened a popup.");
        }
        async Task Layout() { var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain button style initialization."); await idle.Task; await Task.Delay(20); root.UpdateLayout(); for (var i = 0; i < 6; i++) { root.Measure(new Size(640, 400)); root.Arrange(new Rect(0, 0, 640, 400)); root.UpdateLayout(); Guard(); await Task.Delay(15); } }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            root.RequestedTheme = Enum.Parse<ElementTheme>(scheme); root.Children.Clear();
            var original = new BleeckerButton { Content = "Fresh original C# action", Width = 220, Height = 40, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            var container=new ContentControl{CharacterSpacing=120,Content=original,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};root.Children.Add(container); await Layout();
            void Record(string stage)
            {
                var native = Elements(original).OfType<Button>().Single(e => e.Name == "PART_Action"); var ring = Elements(original).OfType<ProgressRing>().Single(e => e.Name == "PART_Progress");
                Require(original.Content is string content && content == "Fresh original C# action" && native.IsEnabled && ring.Visibility == Visibility.Collapsed, "Default style changed original content or idle behavior.");
                samples.Add(new { scheme, stage, nativeActionFound = true, progressPartFound = true, originalContentRetained = true, explicitStyle = !ReferenceEquals(original.ReadLocalValue(FrameworkElement.StyleProperty), DependencyProperty.UnsetValue), actualWidth = original.ActualWidth, actualHeight = original.ActualHeight });
            }
            Require(ReferenceEquals(original.ReadLocalValue(FrameworkElement.StyleProperty), DependencyProperty.UnsetValue), "Fresh default-style check accidentally used an explicit fixture style."); Record("fresh-csharp-construction");
            var custom = (ControlTemplate)XamlReader.Load("""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:o="using:Orchard" TargetType="o:BleeckerButton">
                  <Grid><Button x:Name="PART_Action" Content="{TemplateBinding Content}"/><ProgressRing x:Name="PART_Progress" Visibility="Collapsed" IsActive="False"/></Grid>
                </ControlTemplate>
                """);
            original.Template = custom; await Layout(); Require(ReferenceEquals(original.Template, custom), "Explicit template lost precedence."); Record("caller-template");
            original.ClearValue(Control.TemplateProperty); await Layout(); Require(!ReferenceEquals(original.Template, custom), "Clearing template did not restore the library default."); Record("default-template-restored");
            var style = new Style(typeof(BleeckerButton)); style.Setters.Add(new Setter(Control.TemplateProperty, custom)); original.Style = style; await Layout(); Require(ReferenceEquals(original.Style, style) && ReferenceEquals(original.Template, custom), "Caller style lost precedence."); Record("caller-style");
            original.ClearValue(FrameworkElement.StyleProperty); await Layout(); Require(ReferenceEquals(original.ReadLocalValue(FrameworkElement.StyleProperty), DependencyProperty.UnsetValue) && !ReferenceEquals(original.Template, custom), "Clearing caller style did not restore the library default."); Record("default-style-restored");
            var values=new SpacingValues();object? callerBinding=null;
            foreach(var step in new[]{"parent-spacing","local-spacing","local-cleared","bound-spacing","binding-notified","binding-cleared"})
            {
                if(step=="local-spacing")original.CharacterSpacing=72;
                if(step=="local-cleared"||step=="binding-cleared")original.ClearValue(Control.CharacterSpacingProperty);
                if(step=="bound-spacing"){original.SetBinding(Control.CharacterSpacingProperty,new Binding{Source=values,Path=new PropertyPath(nameof(SpacingValues.Spacing)),Mode=BindingMode.OneWay});callerBinding=original.ReadLocalValue(Control.CharacterSpacingProperty);}
                if(step=="binding-notified")values.Spacing=24;
                await Layout();var native=Elements(original).OfType<Button>().Single(e=>e.Name=="PART_Action");var expected=step=="local-spacing"?72:step=="bound-spacing"?48:step=="binding-notified"?24:0;
                Require(container.CharacterSpacing==120&&original.CharacterSpacing==expected&&native.CharacterSpacing==expected,"Button tracking inherited its container or lost caller precedence.");
                var retained=step is not ("bound-spacing" or "binding-notified")||ReferenceEquals(callerBinding,original.ReadLocalValue(Control.CharacterSpacingProperty));Require(retained,"Button tracking replaced the original caller binding.");
                spacingSamples.Add(new{scheme,stage=step,parentSpacing=container.CharacterSpacing,wrapperSpacing=original.CharacterSpacing,nativeSpacing=native.CharacterSpacing,callerBindingRetained=retained});
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); string Hash(string p) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { passed = true, scope = "fresh-csharp-button-default-resource-and-caller-template-style-precedence", samples,spacingSamples, guards, activations, focus, librarySha256 = Hash(typeof(BleeckerButton).Assembly.Location), hostSha256 = Hash(hostPath),appResourceSha256=Hash(Path.Combine(AppContext.BaseDirectory,"resources.pri")), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, nativePhysicalInputVerified = false, visibleStartupVerified = false }, new JsonSerializerOptions { WriteIndented = true }));
        window.Close(); Environment.Exit(0);
    }
}
