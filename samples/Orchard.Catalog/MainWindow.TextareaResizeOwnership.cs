using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunTextareaResizeOwnershipAsync(string folder)
    {
        var original = Content; var canvas = new Canvas { Width = 640, Height = 600, Background = new SolidColorBrush(Microsoft.UI.Colors.White) };
        var page = new Page { Content = canvas }; NativeInputSession? input = null;
        var phases = new List<string>(); var phase = "mount"; var passed = false;
        try
        {
            Content = page; await AwaitPresentationAsync(page);
            input = new NativeInputSession(page.XamlRoot, folder, "textarea-resize-ownership"); await input.AcquireAsync(canvas);
            foreach (var scheme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                page.RequestedTheme = scheme; canvas.Children.Clear();
                var editor = new BleeckerTextarea { Width = 280, IsEnabled = false, Text = "Disabled notes" };
                Canvas.SetLeft(editor,20); Canvas.SetTop(editor,120); canvas.Children.Add(editor); await AwaitPresentationAsync(editor);
                var right = new Point(297, 120 + editor.ActualHeight - 3);
                phase = scheme + "-grip"; await input.MoveAsync(canvas,true,right); await CursorAsync(32645); phases.Add(phase);
                var overlay = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Microsoft.UI.Colors.Red) };
                Canvas.SetLeft(overlay,288); Canvas.SetTop(overlay,120 + editor.ActualHeight - 12); canvas.Children.Add(overlay);
                phase = scheme + "-occluded"; await AwaitPresentationAsync(page); await CursorAsync(32512); await RejectedDragAsync(right); phases.Add(phase);
                canvas.Children.Remove(overlay); await AwaitPresentationAsync(page); await input.MoveAsync(canvas,true,right); await CursorAsync(32645);
                editor.Clip = new RectangleGeometry { Rect = new Rect(0,0,280,editor.ActualHeight - 12) };
                phase = scheme + "-clipped"; await AwaitPresentationAsync(page); await CursorAsync(32512); await RejectedDragAsync(right); phases.Add(phase);
                editor.Clip = null; await AwaitPresentationAsync(page); await input.MoveAsync(canvas,true,right); await CursorAsync(32645);
                phase = scheme + "-unload"; canvas.Children.Remove(editor); await AwaitPresentationAsync(page); await CursorAsync(32512); phases.Add(phase);
                canvas.Children.Add(editor); await AwaitPresentationAsync(editor); await input.MoveAsync(canvas,true,right); await CursorAsync(32645);
                phase = scheme + "-live-direction"; editor.FlowDirection = FlowDirection.RightToLeft; await AwaitPresentationAsync(editor);
                var grip = ResizeDescendants<Control>(editor).Single(c=>c.Name=="TextareaResizeGrip");
                var bounds = grip.TransformToVisual(canvas).TransformBounds(new Rect(0,0,grip.ActualWidth,grip.ActualHeight));
                Require(bounds.X==20,phase+": RTL grip did not move to the physical left edge.");
                await input.MoveAsync(canvas,true,new Point(23,right.Y)); await CursorAsync(32645); phases.Add(phase);
                phase = scheme + "-outside"; await input.MoveAsync(canvas,true,new Point(2,2)); await CursorAsync(32512); phases.Add(phase);
                await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"catalog-{scheme.ToString().ToLowerInvariant()}.png"));
                async Task RejectedDragAsync(Point origin)
                {
                    var before=editor.ActualHeight; await input.MoveAsync(canvas,true,origin); await input.PressPointerAsync();
                    try { await input.MoveAsync(canvas,true,new Point(origin.X,origin.Y+20)); await AwaitPresentationAsync(page);
                        Require(page.PointerCaptures?.Count is null or 0,phase+": obscured grip captured the pointer.");
                        Require(editor.ActualHeight==before&&!editor.IsEnabled&&editor.Text=="Disabled notes",phase+": obscured grip resized or changed editor state."); }
                    finally { input.PointerButton(false); }
                    await input.MoveAsync(canvas,true,origin);
                }
                async Task CursorAsync(int shape)
                {
                    var watch=Stopwatch.StartNew();var stable=0;
                    while(stable<2&&watch.Elapsed<TimeSpan.FromSeconds(2)){await AwaitPresentationAsync(page);stable=NativeInputSession.CurrentVisibleCursor()==ResizeStockCursor(IntPtr.Zero,new IntPtr(shape))?stable+1:0;}
                    Require(stable==2,phase+": cursor ownership did not settle to "+shape+": "+JsonSerializer.Serialize(NativeInputSession.DescribeCursor()));
                }
            }
            passed=true;
        }
        finally { input?.Dispose();Content=original;await AwaitPresentationAsync(CatalogRoot);
            await File.WriteAllTextAsync(Path.Combine(folder,"textarea-resize-ownership.json"),JsonSerializer.Serialize(new{passed,phase,phases},new JsonSerializerOptions{WriteIndented=true})); }
        await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed,component="textarea-resize-ownership",environment=RuntimeEnvironment()}));
    }
}
