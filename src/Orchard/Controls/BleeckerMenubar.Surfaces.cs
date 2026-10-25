using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerMenubar
{
    private sealed class RootMenu : IDisposable
    {
        private readonly BleeckerMenubar owner;
        private BleeckerMenubarMenu? menu;
        private MenuLevel? level;
        internal RootMenu(BleeckerMenubar owner)
        {
            this.owner = owner;
            Trigger = new MenubarButton(owner) { Name = "MenubarTrigger", HasSubmenu = true, IsRootTrigger = true };
            Trigger.Request = () => owner.Toggle(this);
            Trigger.Expand = () => { owner.activeIndex = Index; owner.Render(); };
            Trigger.Collapse = owner.Close;
        }
        internal MenubarButton Trigger { get; }
        internal MenuLevel? Level => level;
        internal int Index { get; private set; }
        internal void Update(BleeckerMenubarMenu value, int index)
        {
            menu = value; Index = index; Trigger.DataContext = value; Trigger.Label = value.Trigger;
            Trigger.Style = owner.TriggerStyle; Trigger.Content = Text(value.Trigger);
            level?.Update(value.Items);
        }
        internal void SetOpen(bool open, bool immediate = false)
        {
            Trigger.IsExpanded = open;
            if (open && level is null)
            {
                owner.EnsurePresentation();
                if (owner.plane is null || menu is null) return;
                level = new(owner, Trigger, false); level.Update(menu.Items);
            }
            level?.SetOpen(open, immediate);
        }
        internal void Position(UIElement target) => level?.Position(target);
        internal void RetireSurface() { level?.Dispose(); level = null; Trigger.IsExpanded = false; }
        public void Dispose() { RetireSurface(); if (Trigger.Parent is Panel panel) panel.Children.Remove(Trigger); Trigger.Content = null; Trigger.Request = Trigger.Expand = Trigger.Collapse = null; }
    }

    private sealed partial class MenuLevel : IDisposable
    {
        private readonly BleeckerMenubar owner;
        private readonly MenubarButton anchor;
        private readonly bool nested;
        private readonly MenuItemsPanel items;
        private readonly Dictionary<int, RadioItemsPanel> radioPanels = [];
        private readonly Dictionary<string, Row> rows = [];
        private readonly DispatcherQueueTimer enter, exit;
        private bool disposed, open, mounted;
        private int? openSub;
        internal MenuLevel(BleeckerMenubar owner, MenubarButton anchor, bool nested)
        {
            this.owner = owner; this.anchor = anchor; this.nested = nested;
            items = new(this); anchor.ControlledMenu = this;
            Surface = new BleeckerOverlaySurface { Name = nested ? "MenubarSubmenuSurface" : "MenubarRootSurface", Content = items, FlowDirection = owner.FlowDirection, UseLayoutRounding = false, IsTabStop = false };
            InitializeMotion();
            Surface.PointerEntered += (_, _) => { if (nested) anchor.Expand?.Invoke(); };
            Surface.PointerExited += (_, _) => { if (nested) anchor.Collapse?.Invoke(); };
            enter = owner.DispatcherQueue.CreateTimer(); enter.Interval = TimeSpan.FromMilliseconds(20); enter.IsRepeating = false;
            enter.Tick += (_, _) => { if (!disposed && open) BeginSurfaceMotion(true); };
            exit = owner.DispatcherQueue.CreateTimer(); exit.Interval = TimeSpan.FromMilliseconds(150); exit.IsRepeating = false;
            exit.Tick += (_, _) => { if (!disposed && !open) Unmount(); };
        }
        internal BleeckerOverlaySurface Surface { get; }
        internal MenubarButton[] NavigationButtons => items.Children.SelectMany(element => element is RadioItemsPanel group ? group.Buttons : element is MenubarButton button ? new[] { button } : []).Where(NavigationEnabled).ToArray();
        internal bool IsOpen => open && mounted && !disposed;
        internal MenubarButton Anchor => anchor;
        internal MenuLevel? ChildOf(MenubarButton button) => rows.Values.Single(row => ReferenceEquals(row.Button, button)).Child;
        internal void Update(IReadOnlyList<BleeckerMenubarEntry> value)
        {
            Surface.Style = nested ? owner.SubmenuSurfaceStyle : owner.SurfaceStyle; Surface.FlowDirection = owner.FlowDirection;
            var ordered = new List<(string Key, BleeckerMenubarEntry Item, BleeckerMenubarRadioGroup? Group, int Index)>();
            for (var i = 0; i < value.Count; i++)
            {
                if (value[i] is BleeckerMenubarRadioGroup group)
                {
                    var keys = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var radio in group.Items) { if (!keys.Add(radio.Value)) throw new ArgumentException("Menubar radio values must be unique inside their group."); ordered.Add(($"{i}:radio:{radio.Value}", radio, group, i)); }
                }
                else ordered.Add((i.ToString(System.Globalization.CultureInfo.InvariantCulture), value[i], null, i));
            }
            var wanted = ordered.Select(row => row.Key).ToHashSet(StringComparer.Ordinal);
            foreach (var key in rows.Keys.Where(key => !wanted.Contains(key)).ToArray()) { rows[key].Dispose(); rows.Remove(key); }
            var presentation = new List<UIElement>();
            var grouped = new Dictionary<int, List<UIElement>>();
            for (var i = 0; i < ordered.Count; i++)
            {
                var entry = ordered[i];
                if (!rows.TryGetValue(entry.Key, out var row)) { row = new(this, owner); rows.Add(entry.Key, row); }
                row.Update(entry.Item, entry.Group, entry.Index);
                if (entry.Group is not null)
                {
                    if (!radioPanels.TryGetValue(entry.Index, out var panel)) { panel = new(this); radioPanels.Add(entry.Index, panel); }
                    if (!grouped.TryGetValue(entry.Index, out var children)) { children = []; grouped.Add(entry.Index, children); presentation.Add(panel); }
                    children.Add(row.Element); row.Button.SelectionGroup = panel;
                }
                else { row.Button.SelectionGroup = null; presentation.Add(row.Element); }
            }
            foreach (var entry in grouped) Place(radioPanels[entry.Key], entry.Value);
            foreach (var key in radioPanels.Keys.Where(key => !grouped.ContainsKey(key)).ToArray()) { radioPanels[key].Children.Clear(); radioPanels.Remove(key); }
            Place(items, presentation);
            foreach (var row in rows.Values) row.SetOpen(open && openSub == row.Index);
            NotifyMenu(AutomationEvents.StructureChanged);
            Surface.InvalidateMeasure();
        }
        internal void OpenSubmenu(int? index)
        {
            openSub = index;
            foreach (var row in rows.Values) row.SetOpen(open && openSub == row.Index);
            owner.UpdatePosition();
        }
        internal void SetOpen(bool value, bool immediate = false)
        {
            if (disposed) return;
            if (open == value) { if (!value && immediate) Unmount(); return; }
            open = value;
            if (value)
            {
                exit.Stop();
                if (!mounted)
                {
                    if (owner.plane is null) throw new InvalidOperationException("Menubar has no popup plane.");
                    owner.plane.Children.Add(motionRoot); mounted = true; Surface.IsHitTestVisible = true;
                    enter.Start();
                }
                else { Surface.IsHitTestVisible = true; enter.Start(); }
            }
            else
            {
                enter.Stop(); BeginSurfaceMotion(false);
                // Source exit is retained for 150ms. Keep its original row nodes
                // and nested state until removal; their caller values stay owned.
                if (immediate) Unmount(); else exit.Start();
            }
            foreach (var row in rows.Values) row.SetOpen(value && openSub == row.Index, immediate);
            NotifyMenu(value ? AutomationEvents.MenuOpened : AutomationEvents.MenuClosed);
        }
        private void Unmount()
        {
            enter.Stop(); exit.Stop();
            StopSurfaceMotion(); RetargetSurfaceMotion(false, motionClock.Elapsed.TotalMilliseconds, false);
            if (motionRoot.Parent is Panel parent) parent.Children.Remove(motionRoot);
            mounted = false; openSub = null;
            foreach (var row in rows.Values) row.SetOpen(false, true);
        }
        internal void Position(UIElement target)
        {
            if (!mounted || disposed) return;
            if (Surface.RequestedTheme != owner.ActualTheme) Surface.RequestedTheme = owner.ActualTheme;
            var bounds = anchor.TransformToVisual(target).TransformBounds(new Rect(0, 0, anchor.ActualWidth, anchor.ActualHeight));
            var left = nested ? bounds.Right + 4 : bounds.X; var top = nested ? bounds.Y : bounds.Bottom + 4;
            if (double.IsNaN(Canvas.GetLeft(motionRoot)) || Math.Abs(Canvas.GetLeft(motionRoot) - left) > .000001) Canvas.SetLeft(motionRoot, left);
            if (double.IsNaN(Canvas.GetTop(motionRoot)) || Math.Abs(Canvas.GetTop(motionRoot) - top) > .000001) Canvas.SetTop(motionRoot, top);
            // Canvas owns measure/arrange. Measuring its child with a second
            // constraint from LayoutUpdated can invalidate a native Popup's
            // autonomous layout indefinitely, even when the geometry is stable.
            foreach (var row in rows.Values) row.Position(target);
        }
        public void Dispose()
        {
            if (disposed) return; var wasOpen = open; disposed = true; open = false; Unmount();
            if (wasOpen) NotifyMenu(AutomationEvents.MenuClosed);
            if (ReferenceEquals(anchor.ControlledMenu, this)) anchor.ControlledMenu = null;
            foreach (var row in rows.Values) row.Dispose(); rows.Clear(); items.Children.Clear(); Surface.Content = null;
            radioPanels.Clear();
        }
    }
}
