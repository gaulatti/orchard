using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Orchard;

/// <summary>Consumer content grouped beneath an optional responsive dashboard heading.</summary>
public sealed class BleeckerDashboardSection : ContentControl
{
    private TextBlock? title, description;
    private Grid? header;
    private StackPanel? layout, lead;
    private ContentPresenter? action;
    private HeaderPanel? row;
    private XamlRoot? observedRoot;

    public BleeckerDashboardSection()
    {
        DefaultStyleKey = typeof(BleeckerDashboardSection);
        Loaded += (_, _) => { ObserveRoot(); UpdatePresentation(); };
        Unloaded += (_, _) =>
        {
            DisconnectRoot();
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
                () => { if (IsLoaded) { ObserveRoot(); UpdatePresentation(); } });
        };
        RegisterPropertyChangedCallback(ContentProperty, (_, _) => UpdatePresentation());
    }

    private static DependencyProperty Property(string name, Type type) => DependencyProperty.Register(
        name, type, typeof(BleeckerDashboardSection), new PropertyMetadata(null,
            (owner, _) => ((BleeckerDashboardSection)owner).UpdatePresentation()));
    public static readonly DependencyProperty TitleProperty = Property(nameof(Title), typeof(string));
    public static readonly DependencyProperty DescriptionProperty = Property(nameof(Description), typeof(string));
    public static readonly DependencyProperty ActionProperty = Property(nameof(Action), typeof(object));
    public static readonly DependencyProperty ActionTemplateProperty = Property(nameof(ActionTemplate), typeof(DataTemplate));
    public static readonly DependencyProperty HeaderStyleProperty = Property(nameof(HeaderStyle), typeof(Style));
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    public object? Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }
    public DataTemplate? ActionTemplate { get => (DataTemplate?)GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    public Style? HeaderStyle { get => (Style?)GetValue(HeaderStyleProperty); set => SetValue(HeaderStyleProperty, value); }

    protected override void OnApplyTemplate()
    {
        row?.Children.Clear(); row = null;
        base.OnApplyTemplate();
        title = GetTemplateChild("PART_Title") as TextBlock;
        description = GetTemplateChild("PART_Description") as TextBlock;
        header = GetTemplateChild("PART_Header") as Grid;
        layout = GetTemplateChild("PART_Layout") as StackPanel;
        lead = GetTemplateChild("PART_Lead") as StackPanel;
        action = GetTemplateChild("PART_Action") as ContentPresenter;
        if (header is not null && lead is not null && action is not null)
        {
            header.Children.Remove(lead); header.Children.Remove(action);
            row = new HeaderPanel(); row.Children.Add(lead); row.Children.Add(action); header.Children.Add(row);
        }
        ObserveRoot(); UpdatePresentation();
    }
    private void ObserveRoot()
    {
        if (ReferenceEquals(observedRoot, XamlRoot)) return;
        DisconnectRoot(); observedRoot = XamlRoot;
        if (observedRoot is not null) observedRoot.Changed += RootChanged;
    }
    private void DisconnectRoot()
    {
        if (observedRoot is not null) observedRoot.Changed -= RootChanged;
        observedRoot = null;
    }
    private void RootChanged(XamlRoot sender, XamlRootChangedEventArgs args) => UpdatePresentation();
    private void UpdatePresentation()
    {
        var hasTitle = !string.IsNullOrEmpty(Title); var hasDescription = !string.IsNullOrEmpty(Description);
        var hasAction = Action is not null && (Action is not string value || value.Length > 0);
        var hasHeader = hasTitle || hasDescription || hasAction;
        SetTextPresentation(title,Title,hasTitle);
        SetTextPresentation(description,Description,hasDescription);
        if (action is not null) action.Visibility = hasAction ? Visibility.Visible : Visibility.Collapsed;
        if (header is not null) header.Visibility = hasHeader ? Visibility.Visible : Visibility.Collapsed;
        if (layout is not null) layout.Spacing = hasHeader && Content is not null ? 20 : 0;
        if (row is not null)
        {
            var wide = XamlRoot is not null && XamlRoot.Size.Width >= 640;
            if (row.Wide != wide) { row.Wide = wide; row.InvalidateMeasure(); }
        }
    }
    private static void SetTextPresentation(TextBlock? text,string? value,bool visible)
    {
        if(text is null)return;
        text.Visibility=visible?Visibility.Visible:Visibility.Collapsed;
        // CSS collapses ASCII whitespace without removing the heading/paragraph
        // node or its margin. Keep the consumer string and native peer intact.
        if(visible&&value!.All(c=>c is ' ' or '\t' or '\n' or '\r' or '\f'))text.Height=0;
        else text.ClearValue(HeightProperty);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SectionPeer(this);
    private sealed class SectionPeer(BleeckerDashboardSection owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerDashboardSection);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    private sealed class HeaderPanel : Panel
    {
        internal bool Wide;
        private double leadWidth, actionWidth;
        protected override Size MeasureOverride(Size availableSize)
        {
            var lead = Children[0]; var action = Children[1]; var gap = action.Visibility == Visibility.Visible ? 8 : 0;
            lead.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            action.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            leadWidth = lead.DesiredSize.Width; actionWidth = action.DesiredSize.Width;
            if (Wide && double.IsFinite(availableSize.Width))
            {
                var total = leadWidth + actionWidth;
                if (total + gap > availableSize.Width && total > 0)
                {
                    var capacity = Math.Max(0, availableSize.Width - gap);
                    leadWidth = capacity * leadWidth / total; actionWidth = capacity - leadWidth;
                }
            }
            else if (!Wide) { leadWidth = availableSize.Width; actionWidth = availableSize.Width; }
            lead.Measure(new Size(leadWidth, double.PositiveInfinity)); action.Measure(new Size(actionWidth, double.PositiveInfinity));
            return Wide ? new Size(lead.DesiredSize.Width + action.DesiredSize.Width + gap, Math.Max(lead.DesiredSize.Height, action.DesiredSize.Height)) :
                new Size(Math.Max(lead.DesiredSize.Width, action.DesiredSize.Width), lead.DesiredSize.Height + gap + action.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var lead = Children[0]; var action = Children[1]; var gap = action.Visibility == Visibility.Visible ? 8 : 0;
            if (Wide)
            {
                lead.Arrange(new Rect(0, finalSize.Height - lead.DesiredSize.Height, leadWidth, lead.DesiredSize.Height));
                action.Arrange(new Rect(finalSize.Width - action.DesiredSize.Width, finalSize.Height - action.DesiredSize.Height, action.DesiredSize.Width, action.DesiredSize.Height));
            }
            else
            {
                lead.Arrange(new Rect(0, 0, finalSize.Width, lead.DesiredSize.Height));
                action.Arrange(new Rect(0, lead.DesiredSize.Height + gap, finalSize.Width, action.DesiredSize.Height));
            }
            return finalSize;
        }
    }
}
