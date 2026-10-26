using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

/// <summary>Displays consumer-owned keyboard keys; does not register shortcuts.</summary>
public sealed class BleeckerKbd : ContentControl
{
    private StackPanel? combination;
    private Grid? single;
    private TextBlock? separatorPalette;
    private INotifyCollectionChanged? observed;
    private string[] displayedKeys = [];

    public BleeckerKbd()
    {
        DefaultStyleKey = typeof(BleeckerKbd);
        Loaded += (_, _) => { ObserveKeys(); UpdateKeys(); };
        Unloaded += (_, _) => DetachKeys();
        SizeChanged += (_, _) => UpdateShadow();
        RegisterPropertyChangedCallback(CornerRadiusProperty, (_, _) => UpdateShadow());
    }

    public static readonly DependencyProperty KeysProperty = DependencyProperty.Register(nameof(Keys), typeof(object), typeof(BleeckerKbd),
        new PropertyMetadata(null, (owner, _) => { var control = (BleeckerKbd)owner; control.ObserveKeys(); control.UpdateKeys(); }));
    public IEnumerable<string>? Keys { get => (IEnumerable<string>?)GetValue(KeysProperty); set => SetValue(KeysProperty, value); }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        combination = GetTemplateChild("PART_Combination") as StackPanel;
        single = GetTemplateChild("PART_Single") as Grid;
        separatorPalette = GetTemplateChild("PART_SeparatorPalette") as TextBlock;
        UpdateKeys();
        UpdateShadow();
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private void DetachKeys()
    {
        if (observed is not null) observed.CollectionChanged -= OnKeysChanged;
        observed = null;
    }

    private void ObserveKeys()
    {
        DetachKeys();
        if (IsLoaded && Keys is INotifyCollectionChanged collection)
        {
            observed = collection;
            observed.CollectionChanged += OnKeysChanged;
        }
    }

    private void OnKeysChanged(object? sender, NotifyCollectionChangedEventArgs args) => UpdateKeys();

    private void UpdateKeys()
    {
        displayedKeys = Keys?.ToArray() ?? [];
        if (displayedKeys.Any(key => key is null)) throw new ArgumentException("Keyboard keys cannot contain null entries.", nameof(Keys));
        if (combination is not null && single is not null && separatorPalette is not null)
        {
            combination.Children.Clear();
            combination.Visibility = displayedKeys.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            single.Visibility = displayedKeys.Length > 0 ? Visibility.Collapsed : Visibility.Visible;
            foreach (var key in displayedKeys)
            {
                if (combination.Children.Count > 0)
                {
                    var separator = new TextBlock { Text = "+", FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
                    separator.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = separatorPalette, Path = new PropertyPath("Foreground") });
                    separator.SetBinding(TextBlock.FontFamilyProperty, new Binding { Source = separatorPalette, Path = new PropertyPath("FontFamily") });
                    Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessibilityView(separator, Microsoft.UI.Xaml.Automation.Peers.AccessibilityView.Raw);
                    combination.Children.Add(separator);
                }
                var cap = new BleeckerKbd { Content = key };
                foreach (var (property, name) in new[] {
                    (FontFamilyProperty, nameof(FontFamily)), (FontSizeProperty, nameof(FontSize)),
                    (FontWeightProperty, nameof(FontWeight)), (ForegroundProperty, nameof(Foreground)),
                    (BackgroundProperty, nameof(Background)), (BorderBrushProperty, nameof(BorderBrush)),
                    (BorderThicknessProperty, nameof(BorderThickness)), (PaddingProperty, nameof(Padding)),
                    (HorizontalContentAlignmentProperty, nameof(HorizontalContentAlignment)), (VerticalContentAlignmentProperty, nameof(VerticalContentAlignment)),
                    (CornerRadiusProperty, nameof(CornerRadius)), (IsTextScaleFactorEnabledProperty, nameof(IsTextScaleFactorEnabled)) })
                    cap.SetBinding(property, new Binding { Source = this, Path = new PropertyPath(name) });
                combination.Children.Add(cap);
            }
        }
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private void UpdateShadow()
    {
        if (GetTemplateChild("PART_Shadow") is not Microsoft.UI.Xaml.Shapes.Path shadow) return;
        // Difference between the rounded keycap translated down 1px and the
        // original keycap: no shadow tint beneath its translucent background.
        var w = ActualWidth;
        var h = ActualHeight;
        if (w <= 0 || h <= 0) { shadow.Data = null; return; }
        var corners = CornerRadius;
        var scale = Math.Min(1, Math.Min(Math.Min(Ratio(w, corners.TopLeft + corners.TopRight), Ratio(w, corners.BottomLeft + corners.BottomRight)),
            Math.Min(Ratio(h, corners.TopLeft + corners.BottomLeft), Ratio(h, corners.TopRight + corners.BottomRight))));
        var left = corners.BottomLeft * scale;
        var right = corners.BottomRight * scale;
        var figure = new PathFigure { StartPoint = new(0, h - left), IsClosed = true, IsFilled = true };
        figure.Segments.Add(new LineSegment { Point = new(0, h + 1 - left) });
        Arc(left, h + 1, left, SweepDirection.Counterclockwise);
        figure.Segments.Add(new LineSegment { Point = new(w - right, h + 1) });
        Arc(w, h + 1 - right, right, SweepDirection.Counterclockwise);
        figure.Segments.Add(new LineSegment { Point = new(w, h - right) });
        Arc(w - right, h, right, SweepDirection.Clockwise);
        figure.Segments.Add(new LineSegment { Point = new(left, h) });
        Arc(0, h - left, left, SweepDirection.Clockwise);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        shadow.Data = geometry;
        void Arc(double x, double y, double radius, SweepDirection direction)
        {
            if (radius == 0) figure.Segments.Add(new LineSegment { Point = new(x, y) });
            else figure.Segments.Add(new ArcSegment { Point = new(x, y), Size = new(radius, radius), SweepDirection = direction });
        }
        static double Ratio(double size, double radii) => radii == 0 ? 1 : size / radii;
    }

    private bool HasSuppliedName()
    {
        var property = Microsoft.UI.Xaml.Automation.AutomationProperties.NameProperty;
        if (ReadLocalValue(property) != DependencyProperty.UnsetValue) return true;
        static bool Sets(Style? style, DependencyProperty property)
        {
            for (; style is not null; style = style.BasedOn)
                if (style.Setters.OfType<Setter>().Any(setter => setter.Property == property)) return true;
            return false;
        }
        if (Style is { } explicitStyle) return Sets(explicitStyle, property);
        for (DependencyObject? scope = this; scope is not null; scope = VisualTreeHelper.GetParent(scope))
            if (scope is FrameworkElement element && element.Resources.TryGetValue(typeof(BleeckerKbd), out var resource) && resource is Style implicitStyle)
                return Sets(implicitStyle, property);
        return Application.Current.Resources.TryGetValue(typeof(BleeckerKbd), out var applicationStyle) && applicationStyle is Style style && Sets(style, property);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new KbdPeer(this);
    private sealed class KbdPeer(BleeckerKbd owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerKbd);
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.displayedKeys.Length > 0 ? AutomationControlType.Group : AutomationControlType.Text;
        protected override string GetNameCore()
        {
            var explicitName = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(owner);
            if (owner.HasSuppliedName() || !string.IsNullOrEmpty(explicitName)) return explicitName;
            if (Microsoft.UI.Xaml.Automation.AutomationProperties.GetLabeledBy(owner) is not null) return GetLabeledBy()?.GetName() ?? "";
            return owner.displayedKeys.Length > 0 ? string.Join("+", owner.displayedKeys) : owner.Content as string ?? base.GetNameCore();
        }
    }
}
