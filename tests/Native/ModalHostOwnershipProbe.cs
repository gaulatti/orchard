using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

// Native ownership predicates and transactional placement only. No focus,
// activation, native modality, popup opening or hardware input is requested.
internal static class ModalHostOwnershipProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static (TextBlock Block, Hyperlink Link) Link(string name)
    {
        var link = new Hyperlink(); link.Inlines.Add(new Run { Text = "Fictional " + name }); var span = new Span(); span.Inlines.Add(link); var text = new TextBlock(); text.Inlines.Add(span); return (text, link);
    }
    internal static async Task RunAsync(Window window, string output, string hostPath)
    {
        var root = new Grid { Width = 800, Height = 600 }; root.ColumnDefinitions.Add(new()); root.ColumnDefinitions.Add(new()); window.Content = root; window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(800, 600));
        var guards = 0; var activations = 0; var focus = 0; var cases = new List<object>(); var failures = new List<object>();
        window.Activated += (_, a) => { if (a.WindowActivationState != WindowActivationState.Deactivated) activations++; }; root.GotFocus += (_, _) => focus++;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground); var visible = false; EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var owner); visible |= owner == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && !window.Visible && !window.AppWindow.IsVisible && foreground != Environment.ProcessId && activations == 0 && focus == 0, "Ownership probe acquired visibility, activation or focus.");
            Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count == 0, "Ownership probe opened a popup.");
        }
        async Task Layout()
        {
            var idle = new TaskCompletionSource(); Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => idle.SetResult()), "Could not drain ownership layout."); await idle.Task; await Task.Delay(20); root.UpdateLayout();
            for (var i = 0; i < 4; i++) { root.Measure(new Size(800, 600)); root.Arrange(new Rect(0, 0, 800, 600)); root.UpdateLayout(); Guard(); await Task.Delay(10); }
            Require(root.XamlRoot is { IsHostVisible: false, RasterizationScale: 1 } && root.XamlRoot.Size == new Size(800, 600), "Ownership probe requires its actual hidden unscaled viewport.");
        }
        foreach (var scheme in new[] { "Light", "Dark" })
        {
            root.Children.Clear(); root.RequestedTheme = Enum.Parse<ElementTheme>(scheme);
            var background = Link("BackgroundLink"); var host = new BleeckerModalLayerHostCore { BackgroundContent = background.Block }; var other = new BleeckerModalLayerHostCore(); Grid.SetColumn(other, 1); root.Children.Add(host); root.Children.Add(other);
            BleeckerModalLayerHostCore.Layer Layer(BleeckerModalLayerHostCore owner, string name)
            {
                var surface = new Grid { Name = name, Width = 300, Height = 200 }; var overlay = new Grid(); overlay.Children.Add(surface); return owner.AttachLayer(surface, overlay);
            }
            var parent = Layer(host, "Parent"); var surfaceLink = Link("SurfaceLink"); ((Grid)parent.Surface).Children.Add(surfaceLink.Block);
            var richLink = new Hyperlink(); richLink.Inlines.Add(new Run { Text = "Fictional rich link" }); var paragraph = new Paragraph(); var nested = new Span(); nested.Inlines.Add(richLink); paragraph.Inlines.Add(nested); var rich = new RichTextBlock(); rich.Blocks.Add(paragraph); ((Grid)parent.Surface).Children.Add(rich);
            var branchLink = Link("BranchLink"); var branch = host.AttachBranch(parent, branchLink.Block, new(40, 80)); var foreign = Layer(other, "Foreign"); var foreignLink = Link("ForeignLink"); ((Grid)foreign.Surface).Children.Add(foreignLink.Block);
            host.Activate(parent, [], background.Link); other.Activate(foreign, [], null); await Layout();
            async Task Case(string name, Func<Task<object>> action)
            {
                try { var evidence = await action(); Guard(); cases.Add(new { scheme, name, passed = true, evidence }); }
                catch (Exception error) { failures.Add(new { scheme, name, error = error.ToString() }); cases.Add(new { scheme, name, passed = false }); }
            }
            await Case("foreign-host-position-rejected", () =>
            {
                var before = branch.Frame.Margin; var rejected = false; try { other.PositionBranch(branch, new(90, 120)); } catch (InvalidOperationException) { rejected = true; }
                var unchanged = branch.Frame.Margin == before; host.PositionBranch(branch, new(40, 80)); Require(rejected && unchanged, "A foreign host changed a branch it does not own."); return Task.FromResult<object>(new { rejected, unchanged });
            });
            await Case("invalid-position-preserves-frame", () =>
            {
                var before = branch.Frame.Margin; var rejected = 0; foreach (var point in new[] { new Point(double.NaN, 0), new Point(0, double.NaN), new Point(double.PositiveInfinity, 0), new Point(0, double.NegativeInfinity) }) { try { host.PositionBranch(branch, point); } catch (ArgumentOutOfRangeException) { rejected++; } }
                Require(rejected == 4 && branch.Frame.Margin == before, "Invalid branch placement changed its frame."); return Task.FromResult<object>(new { rejected, unchanged = true });
            });
            await Case("invalid-attach-leaves-root-reusable", async () =>
            {
                var candidate = new TextBlock { Text = "Reusable branch" }; var before = parent.Branches.Count; var rejected = false; try { host.AttachBranch(parent, candidate, new(double.NaN, 0)); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Require(rejected && parent.Branches.Count == before, "Invalid attachment changed registered branches."); var valid = host.AttachBranch(parent, candidate, new(0, 0)); await Layout(); Require(ReferenceEquals(valid.Root, candidate), "Invalid attachment replaced original branch content."); host.DetachBranch(valid); await Layout(); return new { rejected, rootReusable = true, originalReferenceRetained = true, registryUnchanged = parent.Branches.Count == before };
            });
            await Case("active-textblock-and-richtext-links", () =>
            {
                Require(ReferenceEquals(surfaceLink.Link.ElementStart.VisualParent, surfaceLink.Block) && ReferenceEquals(richLink.ElementStart.VisualParent, rich), "Native text pointers lost their actual owner.");
                var simple = host.AllowsFocusTarget(surfaceLink.Link); var richAllowed = host.AllowsFocusTarget(richLink); Require(simple && richAllowed, "An active surface's text-element target was not recognized."); return Task.FromResult<object>(new { simple, rich = richAllowed, actualTextPointerOwnersMatched = true });
            });
            await Case("active-registered-branch-link", () =>
            {
                var allowed = host.AllowsFocusTarget(branchLink.Link); Require(allowed, "A registered branch's inline link was not recognized."); return Task.FromResult<object>(new { allowed });
            });
            await Case("background-and-foreign-links-rejected", () =>
            {
                var backgroundRejected = !host.AllowsFocusTarget(background.Link); var foreignRejected = !host.AllowsFocusTarget(foreignLink.Link); Require(backgroundRejected && foreignRejected, "A text-element target escaped its active layer/host."); return Task.FromResult<object>(new { backgroundRejected, foreignRejected });
            });
            await Case("inactive-and-detached-links-rejected", async () =>
            {
                var child = Layer(host, "Child"); host.Activate(child, [parent.Surface], branchLink.Link); await Layout();
                bool inactiveRejected; try { inactiveRejected = !host.AllowsFocusTarget(surfaceLink.Link) && !host.AllowsFocusTarget(branchLink.Link); } finally { host.Deactivate(child); host.DetachLayer(child); }
                Require(inactiveRejected, "Inactive text-element targets remained allowed."); surfaceLink.Block.Inlines.Clear(); await Layout(); var detachedRejected = !host.AllowsFocusTarget(surfaceLink.Link); Require(detachedRejected, "A removed inline retained its active owner."); return new { inactiveRejected, detachedRejected };
            });
            await Case("ancestor-teardown-rewires-inline-target", async () =>
            {
                var child = Layer(host, "Successor"); host.Activate(child, [parent.Surface], branchLink.Link); await Layout(); var ancestor = host.Deactivate(parent); var removed = host.Deactivate(child); Require(!ancestor.WasActive && removed.WasActive && ReferenceEquals(removed.PreviousFocus, background.Link), "Ancestor teardown did not rewire the branch's inline restoration target."); host.DetachLayer(child); host.Activate(parent, [], background.Link); return new { inactiveAncestorDidNotRestore = true, originalBackgroundInlineTargetRestored = true, focusCalled = false };
            });
            await Case("detached-branch-position-rejected", () =>
            {
                var detached = host.AttachBranch(parent, new TextBlock { Text = "Detached" }, new(10, 10)); host.DetachBranch(detached); var before = detached.Frame.Margin; var rejected = false; try { host.PositionBranch(detached, new(100, 100)); } catch (InvalidOperationException) { rejected = true; }
                Require(rejected && detached.Frame.Margin == before, "Detached branch placement mutated an obsolete frame."); return Task.FromResult<object>(new { rejected, unchanged = true });
            });
            // Cleanup cannot depend on the deliberately failing ownership
            // predicates; each case's actual teardown result remains recorded.
            root.Children.Clear(); await Layout();
        }
        Directory.CreateDirectory(Path.GetDirectoryName(output)!); string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { format = "orchard-modal-host-ownership-v1", passed = failures.Count == 0, scope = "native-branch-ownership-transactionality-and-text-element-target-predicates", cases, failures, guards, activations, focus, librarySha256 = Hash(typeof(BleeckerButton).Assembly.Location), hostSha256 = Hash(hostPath), processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, nativeModalityVerified = false, actualFocusRoutingVerified = false }, new JsonSerializerOptions { WriteIndented = true })); window.Close(); Environment.Exit(failures.Count == 0 ? 0 : 1);
    }
}
