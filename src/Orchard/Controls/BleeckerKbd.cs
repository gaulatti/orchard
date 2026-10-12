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
                    (CornerRadiusProperty, nameof(CornerRadius)), (IsTextScaleFactorEnabledProperty, nameof(IsTextScaleFactorEnabled)) })
                    cap.SetBinding(property, new Binding { Source = this, Path = new PropertyPath(name) });
                combination.Children.Add(cap);
            }
        }
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private void UpdateShadow()
    {
        if (GetTemplateChild("PART_Shadow") is not Microsoft.UI.Xaml.Shapes.Path shadow || ActualWidth < 8 || ActualHeight < 8) return;
        // Difference between the rounded keycap translated down 1px and the
        // original keycap: no shadow tint beneath its translucent background.
        var w = ActualWidth;
        var h = ActualHeight;
        var figure = new PathFigure { StartPoint = new(0, h - 4), IsClosed = true, IsFilled = true };
        figure.Segments.Add(new LineSegment { Point = new(0, h - 3) });
        Arc(4, h + 1, SweepDirection.Counterclockwise);
        figure.Segments.Add(new LineSegment { Point = new(w - 4, h + 1) });
        Arc(w, h - 3, SweepDirection.Counterclockwise);
        figure.Segments.Add(new LineSegment { Point = new(w, h - 4) });
        Arc(w - 4, h, SweepDirection.Clockwise);
        figure.Segments.Add(new LineSegment { Point = new(4, h) });
        Arc(0, h - 4, SweepDirection.Clockwise);
        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);
        shadow.Data = geometry;
        void Arc(double x, double y, SweepDirection direction) => figure.Segments.Add(new ArcSegment
        { Point = new(x, y), Size = new(4, 4), SweepDirection = direction });
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new KbdPeer(this);
    private sealed class KbdPeer(BleeckerKbd owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerKbd);
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.displayedKeys.Length > 0 ? AutomationControlType.Group : AutomationControlType.Text;
        protected override string GetNameCore()
        {
            var explicitName = Microsoft.UI.Xaml.Automation.AutomationProperties.GetName(owner);
            if (!string.IsNullOrEmpty(explicitName)) return explicitName;
            return owner.displayedKeys.Length > 0 ? string.Join("+", owner.displayedKeys) : owner.Content as string ?? base.GetNameCore();
        }
    }
}
