using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System.Text.RegularExpressions;
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
        BindText(title,nameof(Title)); BindText(description,nameof(Description));
        header = GetTemplateChild("PART_Header") as Grid;
        layout = GetTemplateChild("PART_Layout") as StackPanel;
        lead = GetTemplateChild("PART_Lead") as StackPanel;
        action = GetTemplateChild("PART_Action") as ContentPresenter;
        if (header is not null && lead is not null && action is not null)
        {
            header.Children.Remove(lead); header.Children.Remove(action);
            row = new HeaderPanel(title,description); row.Children.Add(lead); row.Children.Add(action); header.Children.Add(row);
        }
        ObserveRoot(); UpdatePresentation();
    }
    private void BindText(TextBlock? text,string property)
    {
        text?.SetBinding(TextBlock.TextProperty,new Binding {Source=this,Path=new PropertyPath(property),Converter=WhitespaceConverter.Instance});
    }
    private sealed class WhitespaceConverter : IValueConverter
    {
        internal static readonly WhitespaceConverter Instance=new();
        private static readonly Regex Spaces=new("[ \\t\\r\\n]+",RegexOptions.CultureInvariant);
        public object Convert(object value,Type targetType,object parameter,string language)=>Spaces.Replace((string?)value??""," ").Trim(' ').Replace("\f","");
        public object ConvertBack(object value,Type targetType,object parameter,string language)=>throw new NotSupportedException();
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
        if(visible&&value!.All(c=>c is ' ' or '\t' or '\n' or '\r'))text.Height=0;
        else text.ClearValue(HeightProperty);
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SectionPeer(this);
    private sealed class SectionPeer(BleeckerDashboardSection owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(BleeckerDashboardSection);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }

    private sealed class HeaderPanel(TextBlock? title,TextBlock? description) : Panel
    {
        internal bool Wide;
        private double leadWidth, actionWidth;
        private readonly TextMinimum titleMinimum=new(),descriptionMinimum=new();
        protected override Size MeasureOverride(Size availableSize)
        {
            var lead = Children[0]; var action = Children[1]; var gap = action.Visibility == Visibility.Visible ? 8 : 0;
            lead.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            action.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            leadWidth = lead.DesiredSize.Width; actionWidth = action.DesiredSize.Width;
            if (Wide && double.IsFinite(availableSize.Width))
            {
                // The action's intrinsic content keeps its width. Resolve the
                // remaining deficit against text, freezing at its word minimum.
                // Do not wrap a word or overlap the action to hide overflow.
                if(leadWidth+actionWidth+gap>availableSize.Width)
                {
                    var minimum=Math.Max(titleMinimum.Get(title),descriptionMinimum.Get(description));
                    leadWidth=Math.Max(minimum,Math.Max(0,availableSize.Width-gap-actionWidth));
                }
            }
            else if (!Wide) { leadWidth = availableSize.Width; actionWidth = availableSize.Width; }
            lead.Measure(new Size(leadWidth, double.PositiveInfinity)); action.Measure(new Size(actionWidth, double.PositiveInfinity));
            return Wide ? new Size(Math.Min(availableSize.Width,leadWidth + actionWidth + gap), Math.Max(lead.DesiredSize.Height, action.DesiredSize.Height)) :
                new Size(Math.Max(lead.DesiredSize.Width, action.DesiredSize.Width), lead.DesiredSize.Height + gap + action.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var lead = Children[0]; var action = Children[1]; var gap = action.Visibility == Visibility.Visible ? 8 : 0;
            if (Wide)
            {
                lead.Arrange(new Rect(0, finalSize.Height - lead.DesiredSize.Height, leadWidth, lead.DesiredSize.Height));
                action.Arrange(new Rect(Math.Max(leadWidth+gap,finalSize.Width-actionWidth), finalSize.Height - action.DesiredSize.Height, actionWidth, action.DesiredSize.Height));
            }
            else
            {
                lead.Arrange(new Rect(0, 0, finalSize.Width, lead.DesiredSize.Height));
                action.Arrange(new Rect(0, lead.DesiredSize.Height + gap, finalSize.Width, action.DesiredSize.Height));
            }
            return finalSize;
        }
    }
    private sealed class TextMinimum
    {
        private object? key;
        private double width;
        private readonly Windows.UI.ViewManagement.UISettings settings=new();
        internal double Get(TextBlock? text)
        {
            if(text is null||text.Visibility!=Visibility.Visible||text.Text.Length==0)return 0;
            var scale=text.IsTextScaleFactorEnabled?settings.TextScaleFactor:1;
            var next=(text.Text,text.FontFamily.Source,text.FontSize,text.FontWeight.Weight,text.FontStyle,text.FontStretch,text.CharacterSpacing,scale,text.FlowDirection);
            if(Equals(key,next))return width;
            var family=text.FontFamily.Source;var separator=family.IndexOf('#');
            if(separator>=0&&Uri.TryCreate(family[..separator],UriKind.Absolute,out var uri))
            {
                // Win2D's appx font resolver requires package identity. Orchard's
                // build-transitive contract copies this same URI path beneath
                // the application directory for unpackaged and packaged output.
                if(uri.Scheme=="ms-appx"&&uri.Host.Length==0)
                {
                    var path=System.IO.Path.Combine(AppContext.BaseDirectory,Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/'));
                    uri=new Uri(System.IO.Path.GetFullPath(path));
                    family=uri.AbsoluteUri+family[separator..];
                }
                using var fonts=new CanvasFontSet(uri);
                if(fonts.Fonts.Count==0)throw new InvalidOperationException("Dashboard Section cannot measure the requested bundled font.");
            }
            using var format=new CanvasTextFormat {FontFamily=family,FontSize=(float)(text.FontSize*scale),FontWeight=text.FontWeight,
                FontStyle=text.FontStyle,FontStretch=text.FontStretch,WordWrapping=CanvasWordWrapping.WholeWord,
                Direction=text.FlowDirection==FlowDirection.RightToLeft?CanvasTextDirection.RightToLeftThenTopToBottom:CanvasTextDirection.LeftToRightThenTopToBottom};
            using var layout=new CanvasTextLayout(CanvasDevice.GetSharedDevice(),text.Text,format,float.MaxValue,float.MaxValue);
            layout.SetPairKerning(0,text.Text.Length,true);
            layout.SetCharacterSpacing(0,text.Text.Length,0,(float)(text.CharacterSpacing*text.FontSize*scale/1000),0);
            // Chromium allocates intrinsic word widths in 1/64px layout units.
            // Round outward so fractional advances cannot force an extra line.
            width=Math.Ceiling(layout.GetMinimumLineLength()*64)/64;key=next;return width;
        }
    }
}
