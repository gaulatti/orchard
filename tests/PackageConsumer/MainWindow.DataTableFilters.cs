using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Orchard;
using Windows.Foundation;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifySelectFilterAsync()
    {
        var source = new ConsumerSelectFilterSource(); var originals = source.Rows.ToArray();
        var options = new ObservableCollection<BleeckerSelectOption> { new("Draft", "Draft"), new("Published", "Published") };
        var filter = new BleeckerDataTableSelectFilter { Table = source, ColumnId = "status", Options = options };
        var rows = new TextBlock { Text = source.VisibleText, Margin = new Thickness(0, 72, 0, 0), VerticalAlignment = VerticalAlignment.Top };
        EventHandler changed = (_, _) => rows.Text = source.VisibleText; source.Changed += changed;
        var host = new Grid { Width = 520, Margin = new Thickness(32), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Background = Root.Background };
        host.Children.Add(filter); host.Children.Add(rows); Root.Children.Add(host);
        ItemsControl Select() => BrandDescendants<ItemsControl>(filter).Single(item => item.Name == "PART_Select");
        Button Item(string value) => Select().Items.OfType<Button>().Single(item => item.Tag is BleeckerSelectOption option && option.Value == value);
        ISelectionItemProvider Choice(string value) => (ISelectionItemProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Item(value)).GetPattern(PatternInterface.SelectionItem);
        IExpandCollapseProvider Expand() => (IExpandCollapseProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Select()).GetPattern(PatternInterface.ExpandCollapse);
        string Value() => ((IValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(Select()).GetPattern(PatternInterface.Value)).Value;
        var arguments = Environment.GetCommandLineArgs(); var output = Path.GetDirectoryName(arguments[Array.IndexOf(arguments, "--verify-output") + 1])!;
        try
        {
            await AwaitAsync(() => filter.IsLoaded && source.Subscribers == 2, "Packaged Select filter did not observe its original consumer."); filter.ApplyTemplate(); Root.UpdateLayout();
            Require(Value() == "All" && rows.Text == "Harbor:Draft|Reed:Published", "Packaged Select changed its consumer's unrelated title filter.");
            Choice("Draft").Select(); await AwaitAsync(() => source.Requests == 1 && rows.Text == "Harbor:Draft" && Value() == "Draft", "Packaged Select did not filter original rows through its original column.");
            Require(ReferenceEquals(source.LastColumn, source.Status) && ReferenceEquals(filter.Table, source), "Packaged Select replaced its original table or column.");
            source.SetExternal("Published"); await AwaitAsync(() => rows.Text == "Reed:Published" && Value() == "Published", "External filter did not reach the packaged visible row projection."); Require(source.Requests == 1, "External filter created a consumer request.");
            options[1].Label = "Live Published"; await AwaitAsync(() => Value() == "Live Published", "Packaged Select ignored live option metadata.");
            Choice("").Select(); await AwaitAsync(() => source.Requests == 2 && source.Status.Value is null && rows.Text == "Harbor:Draft|Reed:Published", "Packaged All did not clear only the original status column.");
            Choice("").Select(); Require(source.Requests == 2 && source.TitleFilter == "r", "Packaged unchanged All emitted a write or cleared unrelated state.");

            var chooser = Select(); var peer = FrameworkElementAutomationPeer.CreatePeerForElement(chooser);
            Require(peer.GetName() == "All" && peer.GetAutomationControlType() == AutomationControlType.ComboBox && chooser.ActualWidth == 176 && chooser.ActualHeight == 40, "Packaged Select lost its source geometry or native accessible boundary.");
            var presentation = await AwaitSelectFilterPresentationAsync(chooser, rows);
            await AcquireConsumerInputAsync(); Require(chooser.Focus(FocusState.Keyboard), "Packaged Select refused native keyboard focus.");
            var empty = Item(""); var draft = Item("Draft"); var keys = new List<object>();
            keys.Add(await PaginationConsumerKeyAsync(0x28, chooser, empty, () => { Require(Expand().ExpandCollapseState == ExpandCollapseState.Expanded && source.Requests == 2, "Packaged Select opening changed original filter state."); var popup = VisualTreeHelper.GetOpenPopupsForXamlRoot(Root.XamlRoot).Single(item => item.Child is not null && BrandDescendants<Button>(item.Child).Any(option => chooser.Items.Contains(option))); VerifyPackagedSelectShadow((FrameworkElement)popup.Child); }));
            keys.Add(await PaginationConsumerKeyAsync(0x09, empty, empty, () => Require(source.Requests == 2, "Packaged modal Tab changed original filter state.")));
            keys.Add(await PaginationConsumerKeyAsync(0x28, empty, draft, () => Require(source.Requests == 2, "Packaged option highlight committed early.")));
            keys.Add(await PaginationConsumerKeyAsync(0x0D, draft, chooser, () => Require(source.Requests == 3 && rows.Text == "Harbor:Draft" && Expand().ExpandCollapseState == ExpandCollapseState.Collapsed, "Packaged Enter did not commit once through original visible rows.")));

            var staleChoice = Choice("Published");
            keys.Add(await PaginationConsumerKeyAsync(0x28, chooser, draft, () => Require(Expand().ExpandCollapseState == ExpandCollapseState.Expanded && source.Requests == 3, "Packaged Select reopening changed original filter state.")));
            var replacement = new ObservableCollection<BleeckerSelectOption> { new("Draft", "Replacement Draft"), new("Published", "Replacement Published", true) }; filter.Options = replacement;
            await AwaitAsync(() => Value() == "Replacement Draft" && ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), Item("Draft")), "Packaged open option replacement lost its controlled value or logical focus.");
            Require(ReferenceEquals(draft, Item("Draft")) && FrameworkElementAutomationPeer.CreatePeerForElement(draft).GetName() == "Replacement Draft", "Stable packaged option key lost its native identity or current metadata.");
            var retainedDisabled = false; try { staleChoice.Select(); } catch (ElementNotEnabledException) { retainedDisabled = true; } Require(retainedDisabled && source.Requests == 3, "Retained packaged provider did not use current disabled metadata.");
            var removedChoice = Choice("Published"); var removedOption = replacement[1]; removedOption.IsDisabled = false; replacement.RemoveAt(1);
            var stale = false; try { removedChoice.Select(); } catch (ElementNotAvailableException) { stale = true; } Require(stale && source.Requests == 3, "Removed packaged option key retained a mutation path.");
            removedOption.IsDisabled = true; replacement.Add(removedOption); await AwaitAsync(() => Select().Items.Count == 3, "Restored packaged option key did not remount.");
            replacement.Move(1, 0);
            await AwaitAsync(() => ReferenceEquals(FocusManager.GetFocusedElement(Root.XamlRoot), Item("Draft")) && Select().Items.IndexOf(Item("Draft")) == 2, "Packaged open option reordering lost its logical focus.");
            replacement[1].Label = "Live Draft Again"; filter.Placeholder = "Any status";
            await AwaitAsync(() => Value() == "Live Draft Again" && FrameworkElementAutomationPeer.CreatePeerForElement(Select()).GetName() == "Any status", "Packaged open option metadata or placeholder did not refresh.");
            var disabledOption = false; try { Choice("Published").Select(); } catch (ElementNotEnabledException) { disabledOption = true; } Require(disabledOption, "Disabled packaged option accepted a selection.");
            keys.Add(await PaginationConsumerKeyAsync(0x0D, Item("Draft"), chooser, () => Require(source.Requests == 3 && Expand().ExpandCollapseState == ExpandCollapseState.Collapsed && rows.Text == "Harbor:Draft", "Packaged updated option commit changed the current original filter or lost dismissal.")));
            filter.IsEnabled = false; var disabled = false; try { Expand().Expand(); } catch (ElementNotEnabledException) { disabled = true; } Require(disabled && source.Requests == 3, "Disabled packaged Select accepted expansion."); filter.IsEnabled = true;
            Require(source.Rows.Zip(originals).All(pair => ReferenceEquals(pair.First, pair.Second)) && source.TitleFilter == "r" && source.GlobalFilter is "consumer-owned", "Packaged Select replaced original rows or unrelated filter state.");
            Root.Children.Remove(host); await AwaitAsync(() => !filter.IsLoaded && source.Subscribers == 1, "Packaged Select did not disconnect on unmount.");
            source.SetExternal("Published"); Root.Children.Add(host); await AwaitAsync(() => filter.IsLoaded && source.Subscribers == 2 && Value() == "Replacement Published", "Packaged Select did not remount from authoritative consumer state.");
            await File.WriteAllTextAsync(Path.Combine(output, $"consumer-select-filter-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed = true, originalRows = true, originalColumn = true, preservedUnrelatedFilters = true, requests = source.Requests, externalRequests = 0, keyedNativeIdentityRetained = true, retainedProviderUsesCurrentMetadata = retainedDisabled, staleOptionRejected = stale, disabledOptionRejected = disabledOption, disabledFilterRejected = disabled, liveReplacement = true, liveReordering = true, liveMetadata = true, remounted = true, keys, presentation }));
        }
        finally
        {
            Root.Children.Remove(host); source.Changed -= changed;
            await AwaitAsync(() => !filter.IsLoaded && source.Subscribers == 0, "Packaged Select retained an original source observer.");
        }
        await File.WriteAllTextAsync(Path.Combine(output, $"consumer-select-filter-unload-{Root.RequestedTheme}.json"), JsonSerializer.Serialize(new { passed = true, finalObservers = source.Subscribers }));
    }

    private async Task<object> AwaitSelectFilterPresentationAsync(FrameworkElement chooser, FrameworkElement rows)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var elapsed = System.Diagnostics.Stopwatch.StartNew(); var stable = 0; var frames = 0; Rect? previous = null; Rect? previousRows = null;
        bool Visible(Rect box) => box.Width > 0 && box.Height > 0 && box.Left >= 0 && box.Top >= 0 && box.Right <= Root.ActualWidth && box.Bottom <= Root.ActualHeight;
        void Rendering(object? sender, object args)
        {
            frames++; var box = chooser.TransformToVisual(Root).TransformBounds(new Rect(0, 0, chooser.ActualWidth, chooser.ActualHeight)); var rowBox = rows.TransformToVisual(Root).TransformBounds(new Rect(0, 0, rows.ActualWidth, rows.ActualHeight));
            stable = chooser.IsLoaded && rows.IsLoaded && Visible(box) && Visible(rowBox) && previous == box && previousRows == rowBox ? stable + 1 : 0; previous = box; previousRows = rowBox; if (stable >= 2) completion.TrySetResult();
        }
        CompositionTarget.Rendering += Rendering;
        try { await completion.Task.WaitAsync(TimeSpan.FromSeconds(2)); } finally { CompositionTarget.Rendering -= Rendering; }
        return new { stable, frames, milliseconds = elapsed.Elapsed.TotalMilliseconds, chooser = previous, rows = previousRows };
    }

    private static void VerifyPackagedSelectShadow(FrameworkElement root)
    {
        var surface = BrandDescendants<BleeckerOverlaySurface>(root).Single(item => item.Name == "PopupSurface");
        var host = BrandDescendants<Grid>(surface).Single(item => item.Name == "PART_ShadowHost");
        var layers = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementChildVisual(host) as Microsoft.UI.Composition.ContainerVisual;
        Require(surface.IsLoaded && !surface.IsTabStop && layers?.Children.Count == 2, "Packaged Select lost its native overlay shadow layers.");
        var bottomToTop = new List<float>();
        foreach (var sprite in layers!.Children.Cast<Microsoft.UI.Composition.SpriteVisual>())
        {
            var effect = sprite.Brush as Microsoft.UI.Composition.CompositionEffectBrush ?? throw new InvalidOperationException("Packaged Select lost its Gaussian brush.");
            Require(effect.Properties.TryGetScalar("cssBlur.BlurAmount", out var sigma) == Microsoft.UI.Composition.CompositionGetValueStatus.Succeeded && sigma is 32 or 7, "Packaged Select lost its CSS Gaussian standard deviation.");
            bottomToTop.Add(sigma);
            Require(effect.Properties.TryGetColor("shadowTint.Color", out var color) == Microsoft.UI.Composition.CompositionGetValueStatus.Succeeded && color.A == 255 && color.R == 26 && color.G == 55 && color.B == 77 && Math.Abs(sprite.Opacity - (sigma == 32 ? .28 : .06)) < .000001, "Packaged Select lost its shadow color/opacity.");
            var input = (Microsoft.UI.Composition.CompositionSurfaceBrush)effect.GetSourceParameter("mask");
            var source = (Microsoft.UI.Composition.CompositionVisualSurface)input.Surface;
            var caster = (Microsoft.UI.Composition.SpriteVisual)source.SourceVisual; var extent = MathF.Ceiling(sigma * 3); var inset = sigma == 32 ? 20 : 0;
            Require(caster.Brush is not null && caster.Size.X == surface.ActualWidth - 2 * inset && caster.Size.Y == surface.ActualHeight - 2 * inset && source.SourceOffset.X == -extent && source.SourceOffset.Y == -extent && sprite.Size == caster.Size + new System.Numerics.Vector2(2 * extent) && sprite.Offset.X == inset - extent && sprite.Offset.Y == inset + (sigma == 32 ? 24 : 4) - extent, "Packaged Select clipped or misplaced its shadow mask.");
        }
        Require(bottomToTop.SequenceEqual(new[] { 7f, 32f }), "Packaged Select reversed the source's front-to-back shadow order.");
    }

    // Independent application model, using only the restored NuGet public API.
    private sealed class ConsumerSelectFilterSource : IBleeckerDataTableFilterSource
    {
        internal sealed record Row(string Title, string Status);
        internal sealed class Column(ConsumerSelectFilterSource owner) : IBleeckerDataTableFilterColumn
        {
            internal object? Value;
            public object? GetFilterValue() => Value;
            public void SetFilterValue(object? value) { Value = value; owner.Requests++; owner.LastColumn = this; owner.Publish(); }
        }
        private EventHandler? changed;
        internal readonly Row[] Rows = [new("Harbor", "Draft"), new("Reed", "Published"), new("Vale", "Draft")];
        internal readonly Column Status;
        internal Column? LastColumn;
        internal int Requests, Subscribers;
        internal string TitleFilter => "r";
        internal ConsumerSelectFilterSource() => Status = new(this);
        public event EventHandler? Changed { add { changed += value; Subscribers++; } remove { changed -= value; Subscribers--; } }
        public int ColumnFilterCount => Status.Value is null ? 1 : 2;
        public object? GlobalFilter => "consumer-owned";
        public IBleeckerDataTableFilterColumn? GetColumn(string id) => id == "status" ? Status : null;
        public void ResetColumnFilters() => throw new InvalidOperationException("Select must not reset unrelated consumer filters.");
        internal string VisibleText => string.Join('|', Rows.Where(row => row.Title.Contains(TitleFilter, StringComparison.OrdinalIgnoreCase) && (Status.Value is null || Equals(row.Status, Status.Value))).Select(row => row.Title + ":" + row.Status));
        internal void SetExternal(object? value) { Status.Value = value; Publish(); }
        private void Publish() => changed?.Invoke(this, EventArgs.Empty);
    }
}
