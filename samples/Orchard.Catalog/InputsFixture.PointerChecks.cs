using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class InputsFixture
{
    private async void OnVerifyPointerLifecycle(object sender, RoutedEventArgs args)
    {
        PointerCheckButton.IsEnabled = false;
        var host = (FrameworkElement)XamlRoot.Content;
        var originalTheme = host.RequestedTheme;
        DependencyObject? ancestor = VisualTreeHelper.GetParent(this);
        while (ancestor is not null && ancestor is not ScrollViewer) ancestor = VisualTreeHelper.GetParent(ancestor);
        var viewport = (ScrollViewer)ancestor!;
        var originalHeight = viewport.Height;
        var originalAlignment = viewport.VerticalAlignment;
        var originalOffset = viewport.VerticalOffset;
        var scale = XamlRoot.RasterizationScale;
        var steps = new List<string>();
        var passed = false;
        string? error = null;
        try
        {
            PointerCheckStatus.Text = "Move the pointer over Unavailable invalid input, then leave it still.";
            var timer = Stopwatch.StartNew();
            while (IsLoaded && !PointerInside(DisabledErrorInput) && timer.Elapsed < TimeSpan.FromSeconds(60)) await Task.Delay(50);
            Require(IsLoaded, "Input fixture was unloaded during the pointer check.");
            Require(PointerInside(DisabledErrorInput), "No physical pointer entered the disabled invalid input within 60 seconds.");
            Require(ReadPointer(out var anchor), "Could not read the desktop pointer.");
            foreach (var invalid in new[] { false, true })
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                DisabledErrorInput.IsError = invalid;
                host.RequestedTheme = theme;
                var hover = theme == ElementTheme.Dark ? "#40FFFFFF" : "#B3E6D5B8";
                var rest = invalid ? theme == ElementTheme.Dark ? "#FFD47B75" : "#FFA65D57" : theme == ElementTheme.Dark ? "#26FFFFFF" : "#66E6D5B8";
                await Check("disabled hover", hover, 32648);
                Require(!DisabledErrorInput.Focus(FocusState.Keyboard), "Disabled hovered editor accepted keyboard focus.");
                DisabledErrorInput.IsEnabled = true;
                await Check("enabled beneath stationary pointer", hover, 32513);
                DisabledErrorInput.IsEnabled = false;
                await Check("disabled beneath stationary pointer", hover, 32648);
                PointerOccluder.Visibility = Visibility.Visible;
                await Check("occluded", rest, 32512);
                PointerOccluder.Visibility = Visibility.Collapsed;
                await Check("uncovered", hover, 32648);
                DisabledErrorInput.Margin = new Thickness(400, 0, -400, 0);
                await Check("moved away", rest, 32512);
                DisabledErrorInput.Margin = new Thickness(0);
                await Check("moved back", hover, 32648);
                DisabledPointerHost.Clip = new RectangleGeometry { Rect = new Rect(0, 0, 1, 1) };
                await Check("clipped", rest, 32512);
                DisabledPointerHost.Clip = null;
                await Check("unclipped", hover, 32648);
                viewport.Height = DisabledErrorInput.TransformToVisual(viewport).TransformPoint(new Point()).Y + DisabledErrorInput.ActualHeight + 16;
                viewport.VerticalAlignment = VerticalAlignment.Top;
                UpdateLayout();
                Require(viewport.ScrollableHeight >= 160, "Pointer fixture does not have enough scrollable content.");
                viewport.ChangeView(null, 160, null, true);
                await Check("scrolled away", rest, null);
                Require(Math.Abs(viewport.VerticalOffset - 160) < 1, "Scroll viewer did not reach the requested offset.");
                viewport.ChangeView(null, 0, null, true);
                await Check("scrolled back", hover, 32648);
                viewport.Height = originalHeight;
                viewport.VerticalAlignment = originalAlignment;
                UpdateLayout();
                DisabledPointerHost.Children.Remove(DisabledErrorInput);
                await Task.Delay(250);
                Require(!DisabledErrorInput.IsLoaded && CurrentCursor() != SystemCursor(IntPtr.Zero, new IntPtr(32648)), "Unloaded input retained its cursor.");
                steps.Add($"{theme}, invalid={invalid}: unloaded");
                DisabledPointerHost.Children.Insert(0, DisabledErrorInput);
                await Check("remounted", hover, 32648);
            }
            passed = true;

            async Task Check(string stage, string border, int? cursor)
            {
                UpdateLayout();
                bool CursorMatches() => cursor is int shape
                    ? CurrentCursor() == SystemCursor(IntPtr.Zero, new IntPtr(shape))
                    : CurrentCursor() != SystemCursor(IntPtr.Zero, new IntPtr(32648));
                var wait = Stopwatch.StartNew();
                while (wait.Elapsed < TimeSpan.FromSeconds(2))
                {
                    Require(IsLoaded, "Input fixture was unloaded during the pointer check.");
                    if (Hex(Find<Border>(DisabledErrorInput, "BorderElement").BorderBrush) == border && CursorMatches()) break;
                    await Task.Delay(16);
                }
                Require(ReadPointer(out var current) && current.X == anchor.X && current.Y == anchor.Y, $"Pointer moved during {stage}; rerun without moving it.");
                var actualBorder = Hex(Find<Border>(DisabledErrorInput, "BorderElement").BorderBrush);
                Require(actualBorder == border, $"{host.RequestedTheme}: {stage} border {actualBorder}, expected {border}.");
                var island = Microsoft.UI.Content.ContentIsland.FindAllForCurrentThread().Single(item => item.Environment.Equals(XamlRoot.ContentIslandEnvironment));
                var selected = Microsoft.UI.Input.InputPointerSource.GetForIsland(island).Cursor;
                var selectedName = selected is Microsoft.UI.Input.InputSystemCursor system ? system.CursorShape.ToString() : selected?.GetType().Name ?? "null";
                Require(CursorMatches(), $"{host.RequestedTheme}: {stage} cursor {CurrentCursor()}, expected {(cursor is int shape ? shape.ToString() : "not UniversalNo")}; WinUI selected {selectedName}.");
                Require(DisabledErrorInput.FocusState == FocusState.Unfocused, $"{stage} unexpectedly focused the editor.");
                steps.Add($"{host.RequestedTheme}, invalid={DisabledErrorInput.IsError}: {stage}");
            }
        }
        catch (Exception exception) { error = exception.ToString(); }
        finally
        {
            PointerOccluder.Visibility = Visibility.Collapsed;
            DisabledPointerHost.Clip = null;
            DisabledErrorInput.Margin = new Thickness(0);
            DisabledErrorInput.IsEnabled = false;
            DisabledErrorInput.IsError = true;
            if (!DisabledPointerHost.Children.Contains(DisabledErrorInput)) DisabledPointerHost.Children.Insert(0, DisabledErrorInput);
            host.RequestedTheme = originalTheme;
            viewport.Height = originalHeight;
            viewport.VerticalAlignment = originalAlignment;
            viewport.ChangeView(null, originalOffset, null, true);
            PointerCheckButton.IsEnabled = true;
        }
        var output = Path.Combine(Path.GetTempPath(), "orchard-input-pointer-result.json");
        File.WriteAllText(output, JsonSerializer.Serialize(new { passed, steps, error, scale }));
        PointerCheckStatus.Text = passed ? "Pointer lifecycle passed in light and dark." : $"Pointer check failed: {error}";
    }

    private static bool PointerInside(FrameworkElement element)
    {
        var bounds = FrameworkElementAutomationPeer.CreatePeerForElement(element).GetBoundingRectangle();
        return ReadPointer(out var point) && bounds.Contains(new Point(point.X, point.Y));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointerCheckPoint { public int X; public int Y; }
    [DllImport("user32.dll", EntryPoint = "GetCursorPos")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadPointer(out PointerCheckPoint point);
    [DllImport("user32.dll", EntryPoint = "GetCursor")]
    private static extern IntPtr CurrentCursor();
    [DllImport("user32.dll", EntryPoint = "LoadCursorW")]
    private static extern IntPtr SystemCursor(IntPtr instance, IntPtr name);
}
