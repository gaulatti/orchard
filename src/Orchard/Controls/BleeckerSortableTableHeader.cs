using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A native table heading that requests caller-owned sort changes.</summary>
public sealed class BleeckerSortableTableHeader : BleeckerTableHead
{
    private readonly SortButton button;
    public BleeckerSortableTableHeader()
    {
        DefaultStyleKey = typeof(BleeckerSortableTableHeader);
        button = new(this);
        button.Click += (_, _) => SortRequested?.Invoke(this, new(Field,
            CurrentSort.Field == Field && CurrentSort.Order == BleeckerTableSortOrder.Ascending
                ? BleeckerTableSortOrder.Descending : BleeckerTableSortOrder.Ascending));
        foreach (var property in new[] { FontFamilyProperty, FontSizeProperty, FontWeightProperty, FontStyleProperty, FontStretchProperty, CharacterSpacingProperty, LineHeightProperty, LanguageProperty, IsTextScaleFactorEnabledProperty })
            RegisterPropertyChangedCallback(property, (_, _) => Update());
        Update();
    }
    public static readonly DependencyProperty FieldProperty = DependencyProperty.Register(nameof(Field), typeof(string), typeof(BleeckerSortableTableHeader), new PropertyMetadata("", Changed));
    public string Field { get => (string)GetValue(FieldProperty); set => SetValue(FieldProperty, value); }
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(BleeckerSortableTableHeader), new PropertyMetadata("", Changed));
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public static readonly DependencyProperty CurrentSortProperty = DependencyProperty.Register(nameof(CurrentSort), typeof(object), typeof(BleeckerSortableTableHeader), new PropertyMetadata(null, Changed));
    private static readonly BleeckerTableSort Unsorted = new("");
    public BleeckerTableSort CurrentSort { get => (BleeckerTableSort?)GetValue(CurrentSortProperty) ?? Unsorted; set { ArgumentNullException.ThrowIfNull(value); SetValue(CurrentSortProperty, value); } }
    public static readonly DependencyProperty IsSortableProperty = DependencyProperty.Register(nameof(IsSortable), typeof(bool), typeof(BleeckerSortableTableHeader), new PropertyMetadata(true, Changed));
    public bool IsSortable { get => (bool)GetValue(IsSortableProperty); set => SetValue(IsSortableProperty, value); }
    public static readonly DependencyProperty SortButtonStyleProperty = DependencyProperty.Register(nameof(SortButtonStyle), typeof(Style), typeof(BleeckerSortableTableHeader), new PropertyMetadata(null, Changed));
    public Style? SortButtonStyle { get => (Style?)GetValue(SortButtonStyleProperty); set => SetValue(SortButtonStyleProperty, value); }
    public event EventHandler<BleeckerTableSort>? SortRequested;
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs _) => ((BleeckerSortableTableHeader)owner).Update();
    private void Update()
    {
        if (button is null) return;
        button.Style = SortButtonStyle; button.FontFamily = FontFamily; button.FontSize = FontSize; button.FontWeight = FontWeight;
        button.FontStyle = FontStyle; button.FontStretch = FontStretch; button.CharacterSpacing = CharacterSpacing;
        button.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; button.Language = Language;
        button.Content = BleeckerTextCasing.Uppercase(Label, Language);
        AutomationProperties.SetName(button, Label);
        var content = IsSortable ? (object)button : Label;
        if (!ReferenceEquals(Content, content) && !Equals(Content, content)) Content = content;
        button.Update(); InvalidateMeasure(); Table?.InvalidateTable();
    }
    internal override (double Minimum, double Preferred) Intrinsic()
    {
        if (!IsSortable) return base.Intrinsic();
        ApplyTemplate(); Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var preferred = DesiredSize.Width; var minimum = 0d;
        var probe = new TextBlock { UseLayoutRounding = false, FontFamily = FontFamily, FontSize = FontSize, FontWeight = FontWeight, FontStyle = FontStyle, FontStretch = FontStretch, CharacterSpacing = CharacterSpacing, Language = Language, IsTextScaleFactorEnabled = IsTextScaleFactorEnabled };
        foreach (var word in System.Text.RegularExpressions.Regex.Split((string)button.Content, @"\s+|(?<=-)"))
        {
            probe.Text = word; probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); minimum = Math.Max(minimum, probe.DesiredSize.Width);
        }
        return (Math.Max(MinWidth, minimum + (Label.Length > 0 ? FontSize * CharacterSpacing / 1000 + 18 : 12) + Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right) + Margin.Left + Margin.Right, Math.Max(MinWidth + Margin.Left + Margin.Right, preferred));
    }
    private sealed class SortButton : Button
    {
        private readonly BleeckerSortableTableHeader owner;
        private TextBlock? label;
        private FrameworkElement? up, down, both;
        private TextBlock[] colors = [];
        private readonly TextBlock metrics = new() { Text = "A", UseLayoutRounding = false, IsTextScaleFactorEnabled = false };
        private readonly Windows.UI.ViewManagement.UISettings settings = new();
        private bool hovered;
        internal SortButton(BleeckerSortableTableHeader owner)
        {
            this.owner = owner; UseLayoutRounding = false;
            PointerEntered += (_, _) => { hovered = true; Update(); };
            PointerExited += (_, _) => { hovered = false; Update(); };
            RegisterPropertyChangedCallback(FocusStateProperty, (_, _) => Update());
            RegisterPropertyChangedCallback(FlowDirectionProperty, (_, _) => Update());
            ActualThemeChanged += (_, _) => Update();
            Loaded += (_, _) => Update();
            Unloaded += (_, _) => { hovered = false; Update(); };
            RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if (Template is null) { label = null; colors = []; up = down = both = null; } });
        }
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            label = Part<TextBlock>("PART_Label"); up = Part<FrameworkElement>("PART_Up"); down = Part<FrameworkElement>("PART_Down"); both = Part<FrameworkElement>("PART_Unsorted");
            colors = new[] { "PART_Normal", "PART_Hover", "PART_Active" }.Select(Part<TextBlock>).ToArray(); Update();
        }
        private T Part<T>(string name) where T : FrameworkElement => GetTemplateChild(name) as T ?? throw new InvalidOperationException($"A table sort button requires {name}.");
        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateLabelLayout();
            // An inline-flex button participates in its th's 14px/20px
            // baseline strut. Vertical centering only the button box loses
            // that strut, especially once the anonymous label wraps.
            var scale = IsTextScaleFactorEnabled ? settings.TextScaleFactor : 1;
            var line = Math.Floor(owner.LineHeight * scale * 64) / 64;
            if (label is not null) label.LineHeight = line / scale;
            var desired = base.MeasureOverride(availableSize);
            double Baseline(double size, double height, Windows.UI.Text.FontWeight weight)
            {
                metrics.FontFamily = FontFamily; metrics.FontSize = size; metrics.FontWeight = weight; metrics.FontStyle = FontStyle; metrics.FontStretch = FontStretch; metrics.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var ascent = Math.Round(metrics.BaselineOffset, MidpointRounding.AwayFromZero);
                var descent = Math.Round(metrics.DesiredSize.Height - metrics.BaselineOffset, MidpointRounding.AwayFromZero);
                return ascent + Math.Floor((height - ascent - descent) / 2);
            }
            var strutHeight = owner.LineHeight * (14d / 11) * scale;
            var parentBaseline = Baseline(FontSize * (14d / 11) * scale, strutHeight, Microsoft.UI.Text.FontWeights.Bold);
            var childBaseline = owner.Label.Length == 0 ? desired.Height : Baseline(FontSize * scale, line, FontWeight);
            var above = Math.Max(parentBaseline, childBaseline) - childBaseline;
            var below = Math.Max(strutHeight - parentBaseline, desired.Height - childBaseline) - (desired.Height - childBaseline);
            // The collapsed one-DIP header rule occupies half a DIP inside
            // the cell's content box. These margins represent the source
            // line box, rather than an empirical glyph translation.
            var margin = new Thickness(0, above, 0, below + .5);
            if (Margin != margin) Margin = margin;
            return desired;
        }
        internal void Update()
        {
            if (label is null) return;
            var sorted = owner.CurrentSort.Field == owner.Field;
            Foreground = colors[sorted ? 2 : hovered ? 1 : 0].Foreground;
            UpdateLabelLayout();
            label.TextDecorations = FocusState == FocusState.Keyboard ? Windows.UI.Text.TextDecorations.Underline : Windows.UI.Text.TextDecorations.None;
            up!.Visibility = sorted && owner.CurrentSort.Order == BleeckerTableSortOrder.Ascending ? Visibility.Visible : Visibility.Collapsed;
            down!.Visibility = sorted && owner.CurrentSort.Order == BleeckerTableSortOrder.Descending ? Visibility.Visible : Visibility.Collapsed;
            both!.Visibility = sorted ? Visibility.Collapsed : Visibility.Visible;
        }
        private void UpdateLabelLayout()
        {
            if (label is null) return;
            var trailing = owner.Label.Length > 0 ? FontSize * CharacterSpacing / 1000 : 0;
            var previous = label.Margin;
            var margin = FlowDirection == FlowDirection.RightToLeft ? new Thickness(trailing, 0, 0, 0) : new Thickness(0, 0, trailing, 0);
            label.Margin = margin;
            label.Visibility = owner.Label.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
            if (VisualTreeHelper.GetParent(label) is Grid layout)
            {
                var spacing = owner.Label.Length == 0 ? 0 : 6;
                if (layout.ColumnSpacing != spacing) layout.ColumnSpacing = spacing;
                // Switching sides preserves desired width. The native grid
                // must still arrange the label with its current tracking inset.
                if ((previous.Left == 0) != (margin.Left == 0) || (previous.Right == 0) != (margin.Right == 0)) layout.InvalidateArrange();
            }
        }
    }
}
