using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed record BleeckerPageRequestedEventArgs(double Page);

/// <summary>Requests navigation while preserving the consumer's page and availability values.</summary>
public sealed partial class BleeckerPagination : Control
{
    private Grid? host;
    private PageRow? row;
    private ActionButton? previous, next;
    private readonly List<ActionButton> pages = [];

    public BleeckerPagination()
    {
        DefaultStyleKey = typeof(BleeckerPagination);
        Loaded += OnPaginationLoaded;
        Unloaded += OnPaginationUnloaded;
    }

    public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(nameof(CurrentPage), typeof(double), typeof(BleeckerPagination), new PropertyMetadata(1d, Changed));
    public static readonly DependencyProperty TotalPagesProperty = DependencyProperty.Register(nameof(TotalPages), typeof(double), typeof(BleeckerPagination), new PropertyMetadata(0d, Changed));
    public static readonly DependencyProperty HasPrevPageProperty = DependencyProperty.Register(nameof(HasPrevPage), typeof(bool), typeof(BleeckerPagination), new PropertyMetadata(false, Changed));
    public static readonly DependencyProperty HasNextPageProperty = DependencyProperty.Register(nameof(HasNextPage), typeof(bool), typeof(BleeckerPagination), new PropertyMetadata(false, Changed));
    public double CurrentPage { get => (double)GetValue(CurrentPageProperty); set => SetValue(CurrentPageProperty, value); }
    public double TotalPages { get => (double)GetValue(TotalPagesProperty); set => SetValue(TotalPagesProperty, value); }
    public bool HasPrevPage { get => (bool)GetValue(HasPrevPageProperty); set => SetValue(HasPrevPageProperty, value); }
    public bool HasNextPage { get => (bool)GetValue(HasNextPageProperty); set => SetValue(HasNextPageProperty, value); }
    public event EventHandler<BleeckerPageRequestedEventArgs>? PageRequested;
    private static void Changed(DependencyObject owner, DependencyPropertyChangedEventArgs args) => ((BleeckerPagination)owner).Refresh();

    protected override void OnApplyTemplate()
    {
        foreach (var button in Ordered()) button.Active = false;
        host?.Children.Clear(); pages.Clear();
        base.OnApplyTemplate();
        host = GetTemplateChild("PART_Layout") as Grid ?? throw new InvalidOperationException("Pagination requires PART_Layout.");
        row = new PageRow(this) { Name = "PaginationNavigation", Margin = new Thickness(0, 32, 0, 0), UseLayoutRounding = false };
        previous = Arrow("PaginationPrevious", "Previous page", "M15 18L9 12L15 6", -1);
        next = Arrow("PaginationNext", "Next page", "M9 18L15 12L9 6", 1);
        row.Children.Add(previous); row.Children.Add(next); host.Children.Add(row); Refresh();
    }

    private ActionButton Arrow(string name, string label, string figures, int direction)
    {
        var button = new ActionButton(this, direction) { Name = name };
        button.Recipe("Arrow"); AutomationProperties.SetName(button, label); ToolTipService.SetToolTip(button, label);
        var glyph = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='24' Height='24' FlowDirection='LeftToRight'><Path Data='" + figures + "' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas>");
        ((Microsoft.UI.Xaml.Shapes.Path)glyph.Children[0]).SetBinding(Microsoft.UI.Xaml.Shapes.Path.StrokeProperty, new Binding { Source = button, Path = new PropertyPath(nameof(Foreground)) });
        button.Content = new Viewbox { Width = 20, Height = 20, Child = glyph, IsHitTestVisible = false, FlowDirection = FlowDirection.LeftToRight, UseLayoutRounding = false };
        return button;
    }

    private IEnumerable<ActionButton> Ordered()
    {
        if (previous is null || next is null) yield break;
        yield return previous; foreach (var button in pages) yield return button; yield return next;
    }

    private void Refresh()
    {
        if (row is null || host is null || previous is null || next is null) return;
        var visible = BleeckerPaginationModel.IsVisible(TotalPages); host.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        previous.IsEnabled = HasPrevPage; next.IsEnabled = HasNextPage;
        var available = pages.ToList(); var retained = new List<ActionButton>();
        foreach (var page in BleeckerPaginationModel.Pages(CurrentPage, TotalPages))
        {
            var button = available.FirstOrDefault(button => button.Page.Equals(page));
            if (button is null)
            {
                button = new ActionButton(this, 0) { Page = page };
                var text = new BleeckerContentText("PaginationPageText", "BleeckerPrimaryMediumFontFamily", 13, 19.5, "BleeckerTextPrimaryBrush", medium: true) { IsHitTestVisible = false };
                Typography.SetNumeralAlignment(text.Text, FontNumeralAlignment.Tabular);
                text.Text.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = button, Path = new PropertyPath(nameof(Foreground)) });
                text.Set(BleeckerNumberText.Format(page), null); button.Content = text;
                AutomationProperties.SetName(button, BleeckerNumberText.Format(page)); row.Children.Add(button);
            }
            else available.Remove(button);
            button.Recipe(page == CurrentPage ? "Selected" : "Page");
            AutomationProperties.SetItemStatus(button, page == CurrentPage ? "Current page" : "");
            retained.Add(button);
        }
        foreach (var removed in available) { removed.Active = false; row.Children.Remove(removed); }
        pages.Clear(); pages.AddRange(retained);
        var index = 0;
        foreach (var button in Ordered()) { button.Active = visible; button.TabIndex = index++; }
        row.InvalidateMeasure(); FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }

    private bool CanInvoke(ActionButton button) => IsLoaded && XamlRoot is not null && BleeckerPaginationModel.IsVisible(TotalPages) && button.Active && button.IsLoaded;
    private void Request(ActionButton button)
    {
        if (CanInvoke(button) && IsEnabled && button.IsEnabled)
            PageRequested?.Invoke(this, new(button.Direction == 0 ? button.Page : CurrentPage + button.Direction));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NavigationPeer(this);
    private sealed class NavigationPeer(BleeckerPagination owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerPagination);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override bool IsControlElementCore() => BleeckerPaginationModel.IsVisible(owner.TotalPages);
        protected override bool IsContentElementCore() => BleeckerPaginationModel.IsVisible(owner.TotalPages);
        protected override IList<AutomationPeer> GetChildrenCore() => BleeckerPaginationModel.IsVisible(owner.TotalPages)
            ? owner.Ordered().Select(button => CreatePeerForElement(button)).ToList() : [];
    }

    private sealed partial class ActionButton : Button
    {
        private readonly BleeckerPagination owner;
        private string recipe = "";
        private ResourceDictionary? palette;
        private readonly ScaleTransform pressScale = new();
        private bool active;
        internal bool Active
        {
            get => active;
            set
            {
                if (active == value) return;
                active = value;
                if (value) RequestPaint(false);
                else { pointerOver = false; StopPaint(); }
            }
        }
        internal double Page { get; init; }
        internal int Direction { get; }
        internal ActionButton(BleeckerPagination owner, int direction)
        {
            this.owner = owner; Direction = direction;
            Template = owner.ActionTemplate;
            InitializePaint();
            RenderTransform = pressScale;
            SizeChanged += (_, _) => { pressScale.CenterX = ActualWidth / 2; pressScale.CenterY = ActualHeight / 2; };
            void Press(bool pressed) => pressScale.ScaleX = pressScale.ScaleY = pressed && IsEnabled ? .96 : 1;
            AddHandler(PointerPressedEvent, new PointerEventHandler((_, _) => Press(true)), true);
            AddHandler(PointerReleasedEvent, new PointerEventHandler((_, _) => Press(false)), true);
            AddHandler(PointerCanceledEvent, new PointerEventHandler((_, _) => Press(false)), true);
            AddHandler(PointerCaptureLostEvent, new PointerEventHandler((_, _) => Press(false)), true);
            AddHandler(KeyDownEvent, new KeyEventHandler((_, args) => { if (args.Key is Windows.System.VirtualKey.Space or Windows.System.VirtualKey.Enter) Press(true); }), true);
            AddHandler(KeyUpEvent, new KeyEventHandler((_, args) => { if (args.Key is Windows.System.VirtualKey.Space or Windows.System.VirtualKey.Enter) Press(false); }), true);
            LostFocus += (_, _) => Press(false);
            Unloaded += (_, _) => Press(false);
            Click += (_, _) => owner.Request(this);
            IsEnabledChanged += (_, _) => { RequestPaint(true); if (!IsEnabled) Press(false); };
        }
        internal void Recipe(string value)
        {
            if (value == recipe) return;
            recipe = value;
            if (palette is not null) Resources.MergedDictionaries.Remove(palette);
            palette = new ResourceDictionary { Source = new Uri($"ms-appx:///Orchard/Themes/Pagination/{value}.g.xaml") };
            Resources.MergedDictionaries.Add(palette); Style = (Style)palette[typeof(Button)];
            RequestPaint(true);
        }
        protected override AutomationPeer OnCreateAutomationPeer() => new ActionPeer(this);
        protected override void OnApplyTemplate()
        {
            ReleasePaintTemplate();
            base.OnApplyTemplate();
            ApplyPaintTemplate();
            var presenter = GetTemplateChild("ContentPresenter") as ContentPresenter
                ?? throw new InvalidOperationException("Pagination actions require ContentPresenter.");
            if (Content is BleeckerContentText text)
                text.Text.SetBinding(TextBlock.ForegroundProperty, new Binding { Source = presenter, Path = new PropertyPath(nameof(Foreground)) });
            else if (Content is Viewbox { Child: Canvas glyph })
                foreach (var path in glyph.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>())
                    path.SetBinding(Microsoft.UI.Xaml.Shapes.Path.StrokeProperty, new Binding { Source = presenter, Path = new PropertyPath(nameof(Foreground)) });
        }
        private sealed class ActionPeer(ActionButton button) : ButtonAutomationPeer(button), IInvokeProvider
        {
            protected override object GetPatternCore(PatternInterface pattern) => pattern == PatternInterface.Invoke ? this : base.GetPatternCore(pattern);
            void IInvokeProvider.Invoke()
            {
                if (!button.owner.CanInvoke(button)) throw new ElementNotAvailableException();
                if (!button.owner.IsEnabled || !button.IsEnabled) throw new ElementNotEnabledException();
                base.Invoke();
            }
        }
    }

    private sealed class PageRow(BleeckerPagination owner) : Panel
    {
        private double[] widths = [];
        private double arrowWidth;
        protected override Size MeasureOverride(Size available)
        {
            widths = owner.pages.Select(button => { button.Measure(new Size(double.PositiveInfinity, 36)); return button.DesiredSize.Width; }).ToArray();
            var natural = widths.Sum() + Math.Max(0, widths.Length - 1) * 4 + 84;
            var width = double.IsFinite(available.Width) ? available.Width : natural;
            arrowWidth = BleeckerPaginationModel.ArrowWidth(width, widths);
            owner.previous!.Measure(new Size(arrowWidth, 36)); owner.next!.Measure(new Size(arrowWidth, 36));
            return new Size(width, 36);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var x = BleeckerPaginationModel.Start(finalSize.Width, widths);
            owner.previous!.Arrange(new Rect(x, 0, arrowWidth, 36)); x += arrowWidth + 6;
            for (var index = 0; index < widths.Length; index++) { owner.pages[index].Arrange(new Rect(x, 0, widths[index], 36)); x += widths[index] + (index == widths.Length - 1 ? 0 : 4); }
            x += 6; owner.next!.Arrange(new Rect(x, 0, arrowWidth, 36)); return finalSize;
        }
    }
}
