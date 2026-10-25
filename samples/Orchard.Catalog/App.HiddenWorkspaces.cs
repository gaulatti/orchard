using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard.Catalog.Demos;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public partial class App
{
    private async void RunHiddenWorkspacesAsync(string folder)
    {
        Directory.CreateDirectory(folder);
        var checks = new List<object>(); var guards = 0; var activationEvents = 0; nint handle = 0;
        string? failure = null; string? isolationFailure = null; var completed = false;
        void Guard()
        {
            guards++; WorkspaceWindowThread(WorkspaceForeground(), out var foreground);
            if (isolationFailure is not null || _window!.Visible || _window.AppWindow.IsVisible || WorkspaceVisible(handle) || foreground == Environment.ProcessId)
                throw new InvalidOperationException(isolationFailure ?? "Hidden demo window became visible or owned foreground input.");
        }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Check(string id, bool condition) { Guard(); Require(condition, id); checks.Add(new { id, passed = true }); Guard(); }
        static T Named<T>(FrameworkElement owner, string name) where T : class => owner.FindName(name) as T ?? throw new InvalidOperationException("Missing demo control: " + name);
        static IEnumerable<DependencyObject> Descendants(DependencyObject parent)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++) { var child = VisualTreeHelper.GetChild(parent, i); yield return child; foreach (var item in Descendants(child)) yield return item; }
        }
        static IEnumerable<AutomationPeer> Peers(AutomationPeer parent)
        {
            foreach (var child in parent.GetChildren() ?? []) { yield return child; foreach (var item in Peers(child)) yield return item; }
        }
        try
        {
            _window = new Window(); handle = WinRT.Interop.WindowNative.GetWindowHandle(_window); Guard();
            _window.Activated += (_, args) => { if (args.WindowActivationState != WindowActivationState.Deactivated) { activationEvents++; isolationFailure = "Hidden demo activated."; } };
            _window.VisibilityChanged += (_, args) => { if (args.Visible) isolationFailure = "Hidden demo became visible."; };
            var root = new Canvas { Width = 1100, Height = 1000 }; _window.Content = root;
            async Task Layout()
            {
                for (var i = 0; i < 3; i++) { root.Measure(new Size(root.Width, root.Height)); root.Arrange(new Rect(0, 0, root.Width, root.Height)); root.UpdateLayout(); Guard(); await Task.Delay(10); }
                Require(root.XamlRoot is not null && !root.XamlRoot.IsHostVisible, "Hidden root is missing or visible.");
            }
            async Task Invoke(BleeckerButton button)
            {
                Guard(); Require(button.IsEnabled && (button.Command?.CanExecute(button.CommandParameter) ?? true), "Demo action is disabled."); button.ApplyTemplate();
                var action = Descendants(button).OfType<Button>().Single(item => item.Name == "PART_Action");
                ((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(action).GetPattern(PatternInterface.Invoke)).Invoke(); await Layout();
            }
            async Task Viewport(double width)
            {
                var scale = root.XamlRoot.RasterizationScale;
                _window.AppWindow.ResizeClient(new SizeInt32((int)Math.Round(width * scale), (int)Math.Round(root.Height * scale)));
                root.Width = width; await Layout();
                Require(Math.Abs(root.XamlRoot.Size.Width - width) < .001, "Actual native viewport resize differs.");
            }
            async Task Choose(BleeckerSelect select, string value)
            {
                Guard(); select.ApplyTemplate();
                var surface = (ItemsControl)typeof(BleeckerSelect).GetField("select", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(select)!;
                var option = surface.Items.OfType<Button>().Single(item => item.Tag is BleeckerSelectOption original && original.Value == value);
                ((ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(option).GetPattern(PatternInterface.SelectionItem)).Select(); await Layout();
                Require(surface.FocusState == FocusState.Unfocused, "Hidden chooser took focus.");
            }
            async Task Navigate(BleeckerSidebar sidebar, string name)
            {
                Guard(); sidebar.ApplyTemplate();
                var peer = Peers(FrameworkElementAutomationPeer.CreatePeerForElement(sidebar)).Single(item => item.GetName() == name && item.GetPattern(PatternInterface.Invoke) is IInvokeProvider);
                ((IInvokeProvider)peer.GetPattern(PatternInterface.Invoke)).Invoke(); await Layout();
            }
            await Layout();
            await Viewport(1100);
            foreach (var theme in new[] { ElementTheme.Light, ElementTheme.Dark })
            {
                root.RequestedTheme = theme;
                var monitoring = new MonitoringWorkspaceFixture { Width = 1060 }; var editorial = new EditorialWorkspaceFixture { Width = 1060 };
                string? requested = null; monitoring.SuiteRequested += value => requested = value; editorial.SuiteRequested += value => requested = value;
                root.Children.Add(monitoring); await Layout();
                var prefix = theme + "/monitoring/"; var originalPost = monitoring.Model.Post("harbor"); var originalCheckbox = monitoring.Selection("harbor");
                Check(prefix + "loaded", monitoring.IsLoaded && monitoring.ActualHeight >= 720 && Named<BleeckerAppShell>(monitoring, "Shell").Content is Grid);
                monitoring.Width = 760; await Viewport(800);
                Check(prefix + "narrow-viewport", monitoring.ActualWidth == 760 && Named<ScrollViewer>(monitoring, "FeedScroll").ScrollableWidth > 0 && monitoring.XamlRoot.Size.Width == 800);
                monitoring.Width = 1060; await Viewport(1100);
                originalCheckbox.IsChecked = true; await Layout();
                Check(prefix + "native-selection", monitoring.Model.SelectedIds.SetEquals(["harbor"]));
                var search = monitoring.Search("Local"); search.Editor.Text = "garden"; await Layout();
                Check(prefix + "native-search", monitoring.Model.Query("Local") == "garden" && monitoring.Model.Visible("Local").Single().Id == "garden" && monitoring.Model.SelectedIds.Contains("harbor"));
                await Invoke(Named<BleeckerButton>(monitoring, "Update")); search.Editor.Text = ""; await Layout();
                Check(prefix + "retained-update", monitoring.Model.UpdateCount == 1 && ReferenceEquals(originalPost, monitoring.Model.Post("harbor")) && ReferenceEquals(originalCheckbox, monitoring.Selection("harbor")) && originalCheckbox.IsChecked == true);
                await Invoke(monitoring.Details("harbor"));
                Check(prefix + "inspector", monitoring.Model.InspectorId == "harbor" && Named<TextBlock>(monitoring, "InspectorText").Text.Contains(originalPost.Content, StringComparison.Ordinal));
                await Navigate(Named<BleeckerSidebar>(monitoring, "Sidebar"), "Attention cards");
                Check(prefix + "sidebar-attention", monitoring.Model.Section == "attention" && monitoring.Model.SelectedIds.Contains("harbor"));
                await Navigate(Named<BleeckerSidebar>(monitoring, "Sidebar"), "Live columns");
                await Invoke(Named<BleeckerButton>(monitoring, "Review"));
                Check(prefix + "review-snapshot", monitoring.Model.Reviewing && monitoring.Model.ReviewIds.SequenceEqual(["harbor"]) && !Named<BleeckerSidebar>(monitoring, "Sidebar").IsEnabled && !Named<BleeckerHeaderSelect>(monitoring, "Suite").IsEnabled);
                var instructions = Named<BleeckerTextarea>(monitoring, "Instructions"); instructions.Text = "Keep this local draft."; await Layout();
                await Invoke(Named<BleeckerButton>(monitoring, "Back"));
                Check(prefix + "discard-decision", monitoring.Model.ConfirmCancel && Named<StackPanel>(monitoring, "DiscardReview").Visibility == Visibility.Visible && !instructions.IsEnabled);
                await Invoke(Named<BleeckerButton>(monitoring, "KeepReview"));
                Check(prefix + "keep-review", monitoring.Model.Reviewing && monitoring.Model.Instructions == "Keep this local draft." && instructions.Text == monitoring.Model.Instructions);
                await Invoke(Named<BleeckerButton>(monitoring, "RecordReview"));
                Check(prefix + "local-review", !monitoring.Model.Reviewing && monitoring.Model.Reviews.Single().PostIds.SequenceEqual(["harbor"]) && monitoring.Model.Reviews.Single().Instructions == "Keep this local draft." && monitoring.Model.SelectedIds.Count == 0);
                originalCheckbox.IsChecked = true; await Layout(); await Invoke(Named<BleeckerButton>(monitoring, "Review")); instructions.Text = "Discard this local instruction."; await Layout(); await Invoke(Named<BleeckerButton>(monitoring, "Back")); await Invoke(Named<BleeckerButton>(monitoring, "ConfirmDiscard"));
                Check(prefix + "explicit-review-discard", !monitoring.Model.Reviewing && monitoring.Model.SelectedIds.Contains("harbor") && monitoring.Model.Reviews.Count == 1);
                await Navigate(Named<BleeckerSidebar>(monitoring, "Sidebar"), "Local reviews");
                Check(prefix + "review-history", Named<StackPanel>(monitoring, "History").Children.OfType<TextBlock>().Single().Text.Contains("Keep this local draft.", StringComparison.Ordinal));
                await Choose(Named<BleeckerHeaderSelect>(monitoring, "Suite"), "editorial"); Check(prefix + "suite-request", requested == "editorial");
                root.Children.Clear(); root.Children.Add(editorial); await Layout();
                prefix = theme + "/editorial/"; var page = editorial.Model.Page("harbor-page"); var pageSelection = editorial.Selection(page.Id); var pageOpen = editorial.OpenPage(page.Id);
                Check(prefix + "loaded", editorial.IsLoaded && editorial.ActualHeight >= 760);
                editorial.Width = 760; await Viewport(800);
                Check(prefix + "narrow-viewport", editorial.ActualWidth == 760 && Descendants(editorial).OfType<ScrollViewer>().Any(scroll => scroll.ScrollableWidth > 0) && editorial.XamlRoot.Size.Width == 800);
                editorial.Width = 1060; await Viewport(1100);
                pageSelection.IsChecked = true; await Layout(); await Choose(Named<BleeckerSelect>(editorial, "StatusFilter"), "review"); await Choose(Named<BleeckerSelect>(editorial, "LanguageFilter"), "pt");
                Check(prefix + "native-filters", editorial.Model.Visible.Single().Id == "oficina-page" && editorial.Model.SelectedIds.Contains(page.Id));
                await Choose(Named<BleeckerSelect>(editorial, "StatusFilter"), ""); await Choose(Named<BleeckerSelect>(editorial, "LanguageFilter"), "");
                await Invoke(Named<BleeckerButton>(editorial, "Sort")); Check(prefix + "sort-retention", editorial.Model.Visible.First().Id == "oficina-page" && ReferenceEquals(pageSelection, editorial.Selection(page.Id)) && pageSelection.IsChecked == true);
                var pageSearch = Named<BleeckerSearchInput>(editorial, "Search"); pageSearch.Editor.Text = "no matches"; await Layout();
                Check(prefix + "search-empty", editorial.Model.Visible.Count() == 0 && Named<StackPanel>(editorial, "Rows").Children.Count == 0); pageSearch.Editor.Text = ""; await Layout();
                await Invoke(pageOpen); var draft = editorial.Model.Draft; var title = Named<BleeckerInput>(editorial, "PageTitle"); var body = Named<BleeckerTextarea>(editorial, "Body");
                title.Text = "Revised fictional harbor"; body.Text = "Edited local body"; await Layout();
                Check(prefix + "native-edit", editorial.Model.Draft!.Title == title.Text && editorial.Model.Draft.Body == body.Text && page.Title == "Harbor reading room" && editorial.Model.CanSave);
                await Choose(Named<BleeckerSelect>(editorial, "PageStatus"), "review"); await Choose(Named<BleeckerSelect>(editorial, "Media"), "garden");
                editorial.Choice("category", "culture").IsChecked = true; editorial.Choice("author", "mira").IsChecked = true; await Layout(); await Invoke(editorial.Move("author", "mira", -1));
                Check(prefix + "relations-order", editorial.Model.Draft!.MediaId == "garden" && editorial.Model.Draft.Status == "review" && editorial.Model.Draft.Categories.SequenceEqual(["local", "culture"]) && editorial.Model.Draft.Authors.SequenceEqual(["mira", "ada"]));
                Check(prefix + "visible-choice-order", ReferenceEquals(Named<StackPanel>(editorial, "Authors").Children[0], VisualTreeHelper.GetParent(editorial.Choice("author", "mira"))) && ReferenceEquals(Named<StackPanel>(editorial, "Authors").Children[1], VisualTreeHelper.GetParent(editorial.Choice("author", "ada"))));
                await Invoke(editorial.OpenPage("garden-page"));
                Check(prefix + "row-discard-decision", ReferenceEquals(draft, editorial.Model.Draft) && editorial.Model.Pending?.Id == "garden-page" && !Named<BleeckerPanel>(editorial, "Editor").IsEnabled);
                await Invoke(Named<BleeckerButton>(editorial, "KeepEditing"));
                Check(prefix + "keep-exact-draft", ReferenceEquals(draft, editorial.Model.Draft) && title.Text == "Revised fictional harbor" && body.Text == "Edited local body");
                root.Children.Clear(); await Layout(); root.Children.Add(editorial); await Layout();
                Check(prefix + "catalog-remount", ReferenceEquals(draft, editorial.Model.Draft) && ReferenceEquals(pageOpen, editorial.OpenPage(page.Id)) && title.Text == "Revised fictional harbor" && editorial.Model.SelectedIds.Contains(page.Id));
                await Invoke(Named<BleeckerButton>(editorial, "Save"));
                Check(prefix + "local-save-original", ReferenceEquals(page, editorial.Model.Page(page.Id)) && page.Title == "Revised fictional harbor" && page.Body == "Edited local body" && page.Status == "review" && page.MediaId == "garden" && page.Authors.SequenceEqual(["mira", "ada"]) && !editorial.Model.Dirty);
                title.Text = "Discarded row draft"; await Layout(); await Invoke(editorial.OpenPage("garden-page")); await Invoke(Named<BleeckerButton>(editorial, "Discard"));
                Check(prefix + "explicit-row-discard", editorial.Model.Draft!.Original.Id == "garden-page" && page.Title == "Revised fictional harbor"); await Invoke(pageOpen);
                title.Text = " "; await Layout();
                Check(prefix + "save-validation", !editorial.Model.CanSave && !Named<BleeckerButton>(editorial, "Save").Command!.CanExecute(null) && Named<BleeckerField>(editorial, "TitleField").Error is string { Length: > 0 });
                title.Text = "Unsaved again"; await Layout(); requested = null; await Choose(Named<BleeckerHeaderSelect>(editorial, "Suite"), "monitoring");
                Check(prefix + "suite-discard-decision", requested is null && editorial.Model.Pending?.Kind == "suite" && Named<BleeckerHeaderSelect>(editorial, "Suite").Value == "editorial");
                await Invoke(Named<BleeckerButton>(editorial, "KeepEditing"));
                await Navigate(Named<BleeckerSidebar>(editorial, "Sidebar"), "Media"); Check(prefix + "section-discard-decision", editorial.Model.Section == "pages" && editorial.Model.Pending?.Id == "media");
                await Invoke(Named<BleeckerButton>(editorial, "Discard")); Check(prefix + "section-discard", editorial.Model.Section == "media" && editorial.Model.Draft is null && page.Title == "Revised fictional harbor");
                await Navigate(Named<BleeckerSidebar>(editorial, "Sidebar"), "Pages"); await Invoke(pageOpen); title.Text = "Discarded suite draft"; await Layout(); await Choose(Named<BleeckerHeaderSelect>(editorial, "Suite"), "monitoring"); await Invoke(Named<BleeckerButton>(editorial, "Discard"));
                Check(prefix + "suite-discard", requested == "monitoring" && editorial.Model.Draft is null && page.Title == "Revised fictional harbor");
                root.Children.Clear(); root.Children.Add(monitoring); await Layout(); Check(prefix + "monitoring-remount", monitoring.Model.Reviews.Count == 1 && monitoring.Model.UpdateCount == 1 && ReferenceEquals(originalCheckbox, monitoring.Selection("harbor")));
                root.Children.Clear(); await Layout();
            }
            Require(ResourceErrors.Count == 0 && BindingErrors.Count == 0, "Demo resource or binding diagnostics were reported."); completed = true;
        }
        catch (Exception error) { failure = error.ToString(); }
        finally
        {
            var visible = _window?.Visible ?? false; var nativeVisible = handle != 0 && WorkspaceVisible(handle); WorkspaceWindowThread(WorkspaceForeground(), out var foreground);
            var isolated = !visible && !nativeVisible && activationEvents == 0 && isolationFailure is null && foreground != Environment.ProcessId;
            static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            await File.WriteAllTextAsync(Path.Combine(folder, "hidden-workspaces.json"), JsonSerializer.Serialize(new
            {
                passed = completed && isolated, scope = "hidden-workspace-demo-contracts", checks, failure, isolationFailure,
                hiddenWindow = true, activateCalled = false, showCalled = false, focusCalled = false, inputSent = false, visible, nativeVisible, activationEvents, isolated, guardChecks = guards,
                resourceErrors = ResourceErrors, bindingErrors = BindingErrors, processArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), osArchitecture = RuntimeInformation.OSArchitecture.ToString(),
                catalogSha256 = Hash(typeof(App).Assembly.Location), librarySha256 = Hash(typeof(BleeckerAppShell).Assembly.Location),
                visibleRuntimeVerified = false, sourceRasterParityVerified = false, nativeComponentAccepted = false
            }, new JsonSerializerOptions { WriteIndented = true }));
            _window?.Close(); Environment.Exit(completed && isolated ? 0 : 1);
        }
    }

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")] private static extern nint WorkspaceForeground();
    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")] private static extern uint WorkspaceWindowThread(nint window, out uint process);
    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool WorkspaceVisible(nint window);
}
