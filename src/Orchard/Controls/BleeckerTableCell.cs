using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>An original native cell with table spans, content and templates.</summary>
public class BleeckerTableCell : ContentControl
{
    private TextBlock? text;
    private ContentPresenter? rich;
    private Grid? contentBox;
    private TextBlock? strutProbe;
    private BleeckerButton? inlineButton;
    private long[] inlineButtonTokens = [];
    private static readonly DependencyPropertyChangedCallback ContentChanged = (owner, _) => ((BleeckerTableCell)owner).UpdateContent();
    private static readonly DependencyProperty[] ContentProperties = [ContentProperty, ContentTemplateProperty, ContentTemplateSelectorProperty, FontSizeProperty, FontFamilyProperty, FontWeightProperty, FontStyleProperty, FontStretchProperty, CharacterSpacingProperty, IsTextScaleFactorEnabledProperty, PaddingProperty, BorderThicknessProperty, MarginProperty, WidthProperty, MinWidthProperty, MaxWidthProperty, UseLayoutRoundingProperty, LanguageProperty, HorizontalContentAlignmentProperty, FlowDirectionProperty];
    private static readonly DependencyPropertyChangedCallback TemplateChanged = (owner, _) =>
    {
        var cell = (BleeckerTableCell)owner;
        if (cell.Template is null) { if (cell.rich is not null) cell.rich.Content = null; cell.rich = null; cell.text = null; cell.contentBox = null; cell.ClearInlineButton(); }
    };
    private static readonly RoutedEventHandler CellLoaded = (sender, _) => ((BleeckerTableCell)sender).ObserveInlineButton();
    private static readonly RoutedEventHandler CellUnloaded = (sender, _) =>
    {
        var cell = (BleeckerTableCell)sender; cell.contentMinimum?.Clear(); cell.ClearInlineButton();
    };
    private static readonly DependencyProperty[] InlineButtonProperties = [ContentProperty, ContentTemplateProperty, ContentTemplateSelectorProperty, BleeckerButton.IsLoadingProperty, BleeckerButton.VariantProperty, BorderThicknessProperty];
    private BleeckerTableContentMinimum? contentMinimum;
    private TextBlock? wordProbe;
    private (double Minimum, double Preferred)? plainIntrinsic;
    private double plainTextScale;
    private double? plainRasterScale;
    [ThreadStatic] private static Windows.UI.ViewManagement.UISettings? textSettings;
    private Style? columnInputStyle, columnPresentationStyle;
    private HorizontalAlignment columnAlignment;
    internal void ApplyColumnStyle(Style? style, HorizontalAlignment alignment, BleeckerTableColumnStyles presentations)
    {
        var specifiesAlignment = false; for (var current = style; current is not null; current = current.BasedOn)
            specifiesAlignment |= current.Setters.OfType<Setter>().Any(setter => setter.Property == HorizontalContentAlignmentProperty);
        if (specifiesAlignment) { Style = style; columnInputStyle = null; columnPresentationStyle = null; return; }
        if (columnPresentationStyle is null || !ReferenceEquals(columnInputStyle, style) || columnAlignment != alignment)
        {
            columnInputStyle = style; columnAlignment = alignment; columnPresentationStyle = presentations.Get(GetType(), style, alignment);
        }
        Style = columnPresentationStyle;
    }
    internal BleeckerTable? Table;
    internal int LogicalRow, LogicalColumn, EffectiveRowSpan = 1;
    public BleeckerTableCell()
    {
        DefaultStyleKey = typeof(BleeckerTableCell); IsTabStop = false; UseLayoutRounding = false;
        foreach (var property in ContentProperties)
            RegisterPropertyChangedCallback(property, ContentChanged);
        RegisterPropertyChangedCallback(TemplateProperty, TemplateChanged);
        Loaded += CellLoaded;
        Unloaded += CellUnloaded;
    }
    public static readonly DependencyProperty ColumnSpanProperty = DependencyProperty.Register(nameof(ColumnSpan), typeof(int), typeof(BleeckerTableCell), new PropertyMetadata(1, LayoutChanged));
    public int ColumnSpan { get => (int)GetValue(ColumnSpanProperty); set { if (value < 1) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(ColumnSpanProperty, value); } }
    public static readonly DependencyProperty RowSpanProperty = DependencyProperty.Register(nameof(RowSpan), typeof(int), typeof(BleeckerTableCell), new PropertyMetadata(1, LayoutChanged));
    /// <summary>Zero extends to the end of the current section.</summary>
    public int RowSpan { get => (int)GetValue(RowSpanProperty); set { if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(RowSpanProperty, value); } }
    public static readonly DependencyProperty ColumnWidthProperty = DependencyProperty.Register(nameof(ColumnWidth), typeof(GridLength), typeof(BleeckerTableCell), new PropertyMetadata(GridLength.Auto, LayoutChanged));
    /// <summary>A preferred column width; intrinsic cell minimums still apply.</summary>
    public GridLength ColumnWidth { get => (GridLength)GetValue(ColumnWidthProperty); set => SetValue(ColumnWidthProperty, value); }
    public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(BleeckerTableCell), new PropertyMetadata(20d, (o, _) => ((BleeckerTableCell)o).UpdateContent()));
    public double LineHeight { get => (double)GetValue(LineHeightProperty); set { if (!double.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); SetValue(LineHeightProperty, value); } }
    private static void LayoutChanged(DependencyObject owner, DependencyPropertyChangedEventArgs args) { var cell = (BleeckerTableCell)owner; if (cell.ColumnWidth.IsStar) throw new ArgumentException("Table ColumnWidth supports Auto or pixel widths."); cell.InvalidateMeasure(); cell.Table?.InvalidateTable(); }
    protected override void OnApplyTemplate()
    {
        plainIntrinsic = null;
        contentMinimum?.Clear();
        ClearInlineButton();
        if (rich is not null) rich.Content = null;
        contentBox = null;
        if (Template is null) { rich = null; text = null; return; }
        base.OnApplyTemplate(); text = GetTemplateChild("PART_Text") as TextBlock;
        rich = GetTemplateChild("PART_Rich") as ContentPresenter;
        contentBox = GetTemplateChild("PART_CellContent") as Grid;
        // A complete replacement delegates presentation to the consumer's
        // native ContentControl template. WinUI also does this when clearing
        // a template under a derived Style. No replacement template is chosen
        // here; an incomplete Orchard presentation is still an explicit error.
        if (text is null && rich is null) return;
        if (text is null || rich is null) throw new InvalidOperationException("A table cell presentation requires both PART_Text and PART_Rich.");
        UpdateContent();
    }
    protected virtual string DisplayText(object? content) => BleeckerTableState.CellText(content);
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        if (UpdateInlineBaseline()) desired = base.MeasureOverride(availableSize);
        // A row panel reports shared column widths. Its own desired size can
        // stay unchanged when a bound template shrinks, so relay the cell's
        // native measure invalidation to the shared layout explicitly. The
        // table suppresses this while it is measuring its own columns.
        Table?.InvalidateTable();
        return desired;
    }
    private bool UpdateInlineBaseline()
    {
        // The default template opts into the source table's inline line box.
        // An empty inline-flex action synthesizes its baseline at its content
        // box's bottom edge, inside the source recipe's bottom border. The
        // parent font strut still contributes its descent below that baseline.
        // Caller templates without this part own their native presentation.
        if(contentBox is null)return false;
        var descent=0d;
        if(ContentTemplate is null&&ContentTemplateSelector is null&&Content is BleeckerButton{IsLoading:false,ContentTemplate:null,ContentTemplateSelector:null} button&&button.Variant!=BleeckerButtonVariant.Link&&(button.Content is null||button.Content is string{Length:0}))
        {
            var scale=IsTextScaleFactorEnabled?(textSettings??=new()).TextScaleFactor:1;
            var probe=strutProbe??=new TextBlock{Text="Mg",UseLayoutRounding=false,IsTextScaleFactorEnabled=false};probe.FontFamily=FontFamily;probe.FontSize=FontSize*scale;probe.FontWeight=FontWeight;probe.FontStyle=FontStyle;probe.FontStretch=FontStretch;probe.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            var ascent=Math.Round(probe.BaselineOffset,MidpointRounding.AwayFromZero);var below=Math.Round(probe.DesiredSize.Height-probe.BaselineOffset,MidpointRounding.AwayFromZero);var height=LineHeight*scale;var baseline=ascent+Math.Floor((height-ascent-below)/2);descent=Math.Max(0,height-baseline-button.BorderThickness.Bottom);
        }
        var margin=new Thickness(0,0,0,descent);if(contentBox.Margin==margin)return false;contentBox.Margin=margin;return true;
    }
    private void ClearInlineButton()
    {
        if (inlineButton is not null) for (var i = 0; i < inlineButtonTokens.Length; i++) inlineButton.UnregisterPropertyChangedCallback(InlineButtonProperties[i], inlineButtonTokens[i]);
        inlineButton = null; inlineButtonTokens = [];
    }
    private void ObserveInlineButton()
    {
        var next = Content is BleeckerButton button && IsLoaded && contentBox is not null && ContentTemplate is null && ContentTemplateSelector is null ? button : null;
        if (ReferenceEquals(next, inlineButton)) return;
        ClearInlineButton(); inlineButton = next;
        if (next is not null)
        {
            DependencyPropertyChangedCallback changed = (_, _) => { InvalidateMeasure(); Table?.InvalidateTable(); };
            inlineButtonTokens = InlineButtonProperties.Select(property => next.RegisterPropertyChangedCallback(property, changed)).ToArray();
        }
    }
    private void UpdateContent()
    {
        plainIntrinsic = null;
        contentMinimum?.Clear();
        ObserveInlineButton();
        if (text is null || rich is null) return;
        var alignment = HorizontalContentAlignment;
        // Source headers use physical text-left/right, and body text-right is
        // explicit; ordinary body cells retain the inherited start alignment.
        if (FlowDirection == FlowDirection.RightToLeft && (this is BleeckerTableHead || alignment == HorizontalAlignment.Right))
            alignment = alignment switch { HorizontalAlignment.Left => HorizontalAlignment.Right, HorizontalAlignment.Right => HorizontalAlignment.Left, _ => alignment };
        rich.HorizontalContentAlignment = alignment;
        var plain = ContentTemplate is null && ContentTemplateSelector is null && (Content is null or string || Content.GetType().IsValueType);
        if (plain)
        {
            rich.Content = null; text.Text = DisplayText(Content); text.Visibility = Visibility.Visible; rich.Visibility = Visibility.Collapsed;
            text.LineHeight = LineHeight; text.CharacterSpacing = CharacterSpacing;
            var trailing = text.Text.Length > 0 ? FontSize * text.CharacterSpacing / 1000 : 0;
            text.Margin = FlowDirection == FlowDirection.RightToLeft ? new Thickness(trailing, 0, 0, 0) : new Thickness(0, 0, trailing, 0);
            text.TextAlignment = alignment switch { HorizontalAlignment.Right => TextAlignment.Right, HorizontalAlignment.Center => TextAlignment.Center, _ => TextAlignment.Left };
        }
        else
        {
            text.Text = ""; text.Visibility = Visibility.Collapsed;
            rich.ContentTemplate = ContentTemplate; rich.ContentTemplateSelector = ContentTemplateSelector;
            if (!ReferenceEquals(rich.Content, Content)) rich.Content = Content; rich.Visibility = Visibility.Visible;
        }
        InvalidateMeasure(); Table?.InvalidateTable(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    internal virtual (double Minimum, double Preferred) Intrinsic()
    {
        ApplyTemplate();
        var scale = IsTextScaleFactorEnabled ? (textSettings ??= new()).TextScaleFactor : 1;
        var rasterScale = XamlRoot?.RasterizationScale;
        if (text is { Visibility: Visibility.Visible } && plainIntrinsic is { } cached && plainTextScale == scale && plainRasterScale == rasterScale) return cached;
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); var preferred = DesiredSize.Width;
        if (text is { Visibility: Visibility.Visible })
        {
            var probe = wordProbe ??= new TextBlock { UseLayoutRounding = false, TextWrapping = TextWrapping.NoWrap };
            probe.FontFamily = FontFamily; probe.FontSize = FontSize; probe.FontWeight = FontWeight; probe.FontStyle = FontStyle; probe.FontStretch = FontStretch;
            probe.CharacterSpacing = text.CharacterSpacing; probe.IsTextScaleFactorEnabled = IsTextScaleFactorEnabled; probe.Language = Language;
            var minimum = 0d;
            foreach (var word in System.Text.RegularExpressions.Regex.Split(text.Text, @"\s+|(?<=-)")) { probe.Text = word; probe.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity)); minimum = Math.Max(minimum, probe.DesiredSize.Width); }
            var trailing = text.Text.Length > 0 ? FontSize * text.CharacterSpacing / 1000 : 0;
            var result = (Math.Max(MinWidth, minimum + trailing + Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right) + Margin.Left + Margin.Right, Math.Max(MinWidth + Margin.Left + Margin.Right, preferred));
            plainTextScale = scale; plainRasterScale = rasterScale; plainIntrinsic = result; return result;
        }
        UIElement? rendered = rich is { Visibility: Visibility.Visible } ? rich : Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(this) > 0 ? Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(this, 0) as UIElement : null;
        if (rendered is not null) (contentMinimum ??= new(() => { InvalidateMeasure(); Table?.InvalidateTable(); })).Capture(rendered);
        Measure(new Size(0, double.PositiveInfinity));
        var minimumContent = rendered is null ? DesiredSize.Width : contentMinimum!.Measure(rendered);
        var insets = ReferenceEquals(rendered, rich) ? Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right : 0;
        return (Math.Max(MinWidth, minimumContent + insets) + Margin.Left + Margin.Right, Math.Max(MinWidth + Margin.Left + Margin.Right, preferred));
    }

    internal void ReleaseTable() { plainIntrinsic = null; contentMinimum?.Clear(); Table = null; LogicalRow = LogicalColumn = 0; }
    protected override AutomationPeer OnCreateAutomationPeer() => new TableCellPeer(this);
    private sealed class TableCellPeer(BleeckerTableCell owner) : FrameworkElementAutomationPeer(owner), Microsoft.UI.Xaml.Automation.Provider.IGridItemProvider, Microsoft.UI.Xaml.Automation.Provider.ITableItemProvider
    {
        protected override string GetClassNameCore() => owner.GetType().Name;
        protected override AutomationControlType GetAutomationControlTypeCore() => owner is BleeckerTableCaption ? AutomationControlType.Text : owner is BleeckerTableHead ? AutomationControlType.HeaderItem : AutomationControlType.DataItem;
        protected override string GetNameCore() => AutomationProperties.GetName(owner) is { Length: > 0 } name ? name : owner.Content is FrameworkElement element ? CreatePeerForElement(element)?.GetName() ?? base.GetNameCore() : owner.ContentTemplate is null && owner.ContentTemplateSelector is null ? owner.DisplayTextForName() : base.GetNameCore();
        protected override object GetPatternCore(PatternInterface pattern) => owner.Table is not null && pattern is PatternInterface.GridItem or PatternInterface.TableItem ? this : base.GetPatternCore(pattern);
        public int Row => owner.LogicalRow;
        public int Column => owner.LogicalColumn;
        public int RowSpan => owner.EffectiveRowSpan;
        public int ColumnSpan => owner.ColumnSpan;
        public Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple ContainingGrid => ProviderFromPeer(CreatePeerForElement(owner.Table ?? throw new InvalidOperationException("Cell is not in a table.")));
        public Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple[] GetRowHeaderItems() => [];
        public Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple[] GetColumnHeaderItems() => owner.Table?.HeaderCells(owner.LogicalColumn, owner.ColumnSpan).Select(c => ProviderFromPeer(CreatePeerForElement(c))).ToArray() ?? [];
    }
    private string DisplayTextForName() => BleeckerTableState.CellText(Content);
}

/// <summary>A table heading retaining its original content and accessible name.</summary>
public class BleeckerTableHead : BleeckerTableCell
{
    public BleeckerTableHead() => DefaultStyleKey = typeof(BleeckerTableHead);
    protected override string DisplayText(object? content) => BleeckerTextCasing.Uppercase(base.DisplayText(content), Language);
}

/// <summary>The original caption, placed below the table grid.</summary>
public sealed class BleeckerTableCaption : BleeckerTableCell
{
    public BleeckerTableCaption() => DefaultStyleKey = typeof(BleeckerTableCaption);
}
