using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Orchard;

/// <summary>A connected strip of native buttons. Each button retains its own command and focus.</summary>
public sealed class BleeckerButtonGroup : Panel
{
    private readonly Dictionary<Control, ChildAppearance> appearances = new();
    private Control[] visible = [];
    private double[] widths = [];

    public BleeckerButtonGroup()
    {
        Loaded += (_, _) => InvalidateMeasure();
        Unloaded += (_, _) => RestoreChildren();
    }

    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(BleeckerButtonGroup),
        new PropertyMetadata(Orientation.Horizontal, (owner, _) => ((BleeckerButtonGroup)owner).InvalidateMeasure()));

    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new GroupPeer(this);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Orientation is not (Orientation.Horizontal or Orientation.Vertical))
            throw new InvalidOperationException("Button group orientation must be Horizontal or Vertical.");
        if (Children.Any(child => child is not ButtonBase and not BleeckerButton))
            throw new InvalidOperationException("BleeckerButtonGroup accepts ButtonBase or BleeckerButton children only.");

        var children = Children.Cast<Control>().ToArray();
        foreach (var removed in appearances.Keys.Except(children).ToArray())
        {
            appearances[removed].Restore(removed);
            appearances.Remove(removed);
        }
        visible = children.Where(child => child.Visibility == Visibility.Visible).ToArray();
        var radius = BleeckerTokens.Radii["button"];
        for (var i = 0; i < visible.Length; i++)
        {
            var child = visible[i];
            if (!appearances.ContainsKey(child)) appearances.Add(child, new ChildAppearance(child));
            var first = i == 0;
            var last = i == visible.Length - 1;
            child.Margin = new Thickness(0);
            child.CornerRadius = Orientation == Orientation.Horizontal
                ? new CornerRadius(first ? radius : 0, last ? radius : 0, last ? radius : 0, first ? radius : 0)
                : new CornerRadius(first ? radius : 0, first ? radius : 0, last ? radius : 0, last ? radius : 0);
            child.BorderThickness = Orientation == Orientation.Horizontal
                ? new Thickness(1, 1, last ? 1 : 0, 1)
                : new Thickness(1, 1, 1, last ? 1 : 0);
            child.Measure(new Size(Orientation == Orientation.Horizontal ? double.PositiveInfinity : availableSize.Width,
                double.PositiveInfinity));
        }

        widths = visible.Select(child => child.DesiredSize.Width).ToArray();
        var total = widths.Sum();
        if (Orientation == Orientation.Horizontal && total > availableSize.Width && total > 0)
        {
            for (var i = 0; i < visible.Length; i++)
            {
                widths[i] *= availableSize.Width / total;
                visible[i].Measure(new Size(widths[i], double.PositiveInfinity));
            }
        }
        return Orientation == Orientation.Horizontal
            ? new Size(widths.Sum(), visible.Select(child => child.DesiredSize.Height).DefaultIfEmpty().Max())
            : new Size(widths.DefaultIfEmpty().Max(), visible.Sum(child => child.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        double offset = 0;
        for (var i = 0; i < visible.Length; i++)
        {
            if (Orientation == Orientation.Horizontal)
            {
                visible[i].Arrange(new Rect(offset, 0, widths[i], finalSize.Height));
                offset += widths[i];
            }
            else
            {
                var height = visible[i].DesiredSize.Height;
                visible[i].Arrange(new Rect(0, offset, finalSize.Width, height));
                offset += height;
            }
        }
        return finalSize;
    }

    private void RestoreChildren()
    {
        foreach (var (child, appearance) in appearances) appearance.Restore(child);
        appearances.Clear();
    }

    private sealed class ChildAppearance(Control child)
    {
        private readonly LocalValue radius = new(child, Control.CornerRadiusProperty);
        private readonly LocalValue border = new(child, Control.BorderThicknessProperty);
        private readonly LocalValue margin = new(child, MarginProperty);
        public void Restore(Control target)
        {
            radius.Restore(target, Control.CornerRadiusProperty);
            border.Restore(target, Control.BorderThicknessProperty);
            margin.Restore(target, MarginProperty);
        }
    }

    private sealed class LocalValue(FrameworkElement element, DependencyProperty property)
    {
        private readonly object value = element.ReadLocalValue(property);
        private readonly Binding? binding = element.GetBindingExpression(property)?.ParentBinding;
        public void Restore(FrameworkElement target, DependencyProperty property)
        {
            if (binding is not null) target.SetBinding(property, binding);
            else if (value == DependencyProperty.UnsetValue) target.ClearValue(property);
            else target.SetValue(property, value);
        }
    }

    private sealed class GroupPeer(BleeckerButtonGroup owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerButtonGroup);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}
