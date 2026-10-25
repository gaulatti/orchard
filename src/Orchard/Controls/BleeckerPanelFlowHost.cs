using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

internal sealed class BleeckerPanelFlowHost
{
    private readonly Control owner;
    private readonly ObservableCollection<UIElement> children;
    private readonly bool horizontal;
    private readonly Dictionary<UIElement, Slot> slots = [];
    private readonly List<(DependencyObject Element, DependencyProperty Property, long Token)> observers = [];
    private Grid? root;
    private Flow? flow;
    private ScrollViewer? viewer;
    private Size constraint = new(double.PositiveInfinity, double.PositiveInfinity);

    internal BleeckerPanelFlowHost(Control owner, ObservableCollection<UIElement> children, bool horizontal)
    {
        this.owner = owner; this.children = children; this.horizontal = horizontal;
        children.CollectionChanged += CollectionChanged;
        owner.Loaded += (_, _) => { Reconcile(); Observe(); };
        owner.Unloaded += (_, _) => Disconnect();
        owner.SizeChanged += (_, _) => Clip();
        owner.RegisterPropertyChangedCallback(Control.TemplateProperty, (_, _) => { if (owner.Template is null) Detach(); });
        owner.RegisterPropertyChangedCallback(Control.PaddingProperty, (_, _) => { Constrain(constraint); flow?.InvalidateMeasure(); });
    }
    internal void Attach(Grid? grid)
    {
        root = grid ?? throw new InvalidOperationException("Panel layouts require a PART_Root Grid.");
        flow = new Flow(this) { Name = "PanelFlow", UseLayoutRounding = false };
        if (horizontal)
        {
            viewer = new ScrollViewer { Name = "PanelLayoutViewport", Content = flow, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollMode = ScrollMode.Enabled, VerticalScrollMode = ScrollMode.Disabled,
                ZoomMode = ZoomMode.Disabled, IsTabStop = false, HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch, UseLayoutRounding = false, Padding = new Thickness(0), BorderThickness = new Thickness(0) };
            viewer.LayoutUpdated += ViewportFocus; root.Children.Add(viewer);
        }
        else root.Children.Add(flow);
        Reconcile(); Observe(); Clip();
    }
    internal void Detach()
    {
        Disconnect();
        foreach (var slot in slots.Values) slot.Children.Clear();
        slots.Clear(); flow?.Children.Clear();
        if (viewer is not null) { viewer.LayoutUpdated -= ViewportFocus; viewer.Content = null; }
        if (root is not null) { if (viewer is not null) root.Children.Remove(viewer); else if (flow is not null) root.Children.Remove(flow); root.Clip = null; }
        viewer = null; flow = null; root = null;
    }
    internal void Constrain(Size size)
    {
        if (constraint == size) return;
        constraint = size; flow?.InvalidateMeasure();
    }
    private Size InnerConstraint => new(Subtract(constraint.Width, owner.Padding.Left + owner.Padding.Right), Subtract(constraint.Height, owner.Padding.Top + owner.Padding.Bottom));
    private static double Subtract(double size, double padding) => double.IsFinite(size) ? Math.Max(0, size - padding) : size;
    private void Clip()
    {
        if (root is not null) root.Clip = new RectangleGeometry { Rect = new Rect(0, 0, owner.ActualWidth, owner.ActualHeight) };
    }
    private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs args) { Reconcile(); Observe(); }
    private void Reconcile()
    {
        if (children.Any(child => child is null) || children.Distinct().Count() != children.Count)
            throw new ArgumentException("Panel layout children must be distinct non-null native UI elements.");
        if (flow is null) return;
        foreach (var removed in slots.Keys.Except(children).ToArray()) { flow.Children.Remove(slots[removed]); slots[removed].Children.Clear(); slots.Remove(removed); }
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            if (!slots.TryGetValue(child, out var slot)) { slot = new Slot(child, !horizontal) { UseLayoutRounding = false }; slots.Add(child, slot); }
            var previous = flow.Children.IndexOf(slot);
            if (previous == index) continue;
            if (previous >= 0) flow.Children.Move((uint)previous, (uint)index);
            else flow.Children.Insert(index, slot);
        }
        flow.InvalidateMeasure();
    }
    private void Disconnect() { foreach (var (element, property, token) in observers) element.UnregisterPropertyChangedCallback(property, token); observers.Clear(); }
    private void Observe()
    {
        Disconnect(); if (!owner.IsLoaded) return;
        foreach (var child in children)
        {
            var property = child is BleeckerPanel ? BleeckerPanel.GrowProperty : child is BleeckerPanelColumn ? BleeckerPanelColumn.GrowProperty : BleeckerPanelLayout.GrowProperty;
            observers.Add((child, property, child.RegisterPropertyChangedCallback(property, (_, _) => { flow?.InvalidateMeasure(); owner.InvalidateMeasure(); })));
        }
    }
    private void ViewportFocus(object? sender, object args)
    {
        if (viewer is not null && flow is not null) viewer.IsTabStop = viewer.ScrollableWidth > 0 && viewer.ViewportWidth > 0 && FocusManager.FindFirstFocusableElement(flow) is null;
    }
    private sealed partial class Flow(BleeckerPanelFlowHost host) : Panel
    {
        private double[] tracks = [];
        protected override Size MeasureOverride(Size available)
        {
            var horizontal = host.horizontal;
            var limit = host.InnerConstraint;
            var width = horizontal ? limit.Width : available.Width;
            var height = horizontal ? available.Height : limit.Height;
            foreach (Slot slot in Children) slot.Measure(new Size(horizontal ? double.PositiveInfinity : width, horizontal ? height : double.PositiveInfinity));
            var natural = Children.Select(child => horizontal ? child.DesiredSize.Width : child.DesiredSize.Height).ToArray();
            tracks = BleeckerPanelFlowModel.Tracks(horizontal ? width : height, natural, Children.Cast<Slot>().Select(slot => BleeckerPanelLayout.GetGrow(slot.Child)).ToArray());
            var cross = Children.Select(child => horizontal ? child.DesiredSize.Height : child.DesiredSize.Width).DefaultIfEmpty(0).Max();
            if (!double.IsFinite(width)) width = horizontal ? tracks.Sum() : cross;
            if (!double.IsFinite(height)) height = horizontal ? cross : tracks.Sum();
            for (var index = 0; index < Children.Count; index++) Children[index].Measure(horizontal ? new Size(tracks[index], height) : new Size(width, tracks[index]));
            return horizontal ? new Size(Math.Max(width, tracks.Sum()), height) : new Size(width, height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            double position = 0;
            for (var index = 0; index < Children.Count; index++)
            {
                Children[index].Arrange(host.horizontal ? new Rect(position, 0, tracks[index], final.Height) : new Rect(0, position, final.Width, tracks[index])); position += tracks[index];
            }
            return final;
        }
    }
    private sealed partial class Slot : Panel
    {
        private readonly bool clip;
        internal UIElement Child { get; }
        internal Slot(UIElement child, bool clip) { Child = child; this.clip = clip; Children.Add(child); }
        protected override Size MeasureOverride(Size available)
        {
            Child.Measure(available);
            return new Size(double.IsFinite(available.Width) ? available.Width : Child.DesiredSize.Width,
                double.IsFinite(available.Height) ? available.Height : Child.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var width = final.Width; var height = final.Height;
            if (Child is FrameworkElement element)
            {
                if (double.IsFinite(element.Width)) width = Math.Max(element.MinWidth, Math.Min(element.MaxWidth, element.Width)) + element.Margin.Left + element.Margin.Right;
                else width = Math.Max(element.MinWidth + element.Margin.Left + element.Margin.Right, Math.Min(width, element.MaxWidth + element.Margin.Left + element.Margin.Right));
                if (double.IsFinite(element.Height)) height = Math.Max(element.MinHeight, Math.Min(element.MaxHeight, element.Height)) + element.Margin.Top + element.Margin.Bottom;
                else height = Math.Max(element.MinHeight + element.Margin.Top + element.Margin.Bottom, Math.Min(height, element.MaxHeight + element.Margin.Top + element.Margin.Bottom));
            }
            Child.Arrange(new Rect(0, 0, width, height));
            Clip = clip ? new RectangleGeometry { Rect = new Rect(0, 0, final.Width, final.Height) } : null;
            return final;
        }
    }
}
