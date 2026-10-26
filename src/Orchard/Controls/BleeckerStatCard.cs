using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A consumer-owned statistic, trend and optional compact history.</summary>
public sealed partial class BleeckerStatCard : Control
{
    private TextBlock? title,value,description,trendValue,trendLabel,iconText,footerText,iconScalar,footerScalar;
    private ContentPresenter? richValue,richIcon,richFooter;
    private Grid? header,sparklineHost,trendHost;
    private Border? icon,footer;
    private Panel? trend;
    private Viewbox? trendIcon;
    private BleeckerStatTrend? observedTrend;
    private BleeckerSparkline? sparkline;
    private static readonly Regex Spaces=new("[ \\t\\r\\n]+",RegexOptions.CultureInvariant);
    public BleeckerStatCard()
    {
        DefaultStyleKey=typeof(BleeckerStatCard);
        InitializeTypography();
        Loaded+=(_,_)=>UpdatePresentation();
        Unloaded+=(_,_)=>{StopObserving();DispatcherQueue.TryEnqueue(()=>{if(IsLoaded)UpdatePresentation();});};
        ActualThemeChanged+=(_,_)=>UpdatePresentation();
    }
    private static DependencyProperty Property(string name,Type type)=>DependencyProperty.Register(name,type,typeof(BleeckerStatCard),new PropertyMetadata(null,(owner,_)=>((BleeckerStatCard)owner).UpdatePresentation()));
    public static readonly DependencyProperty TitleProperty=Property(nameof(Title),typeof(string));
    public static readonly DependencyProperty ValueProperty=Property(nameof(Value),typeof(object));
    public static readonly DependencyProperty ValueTemplateProperty=Property(nameof(ValueTemplate),typeof(DataTemplate));
    public static readonly DependencyProperty DescriptionProperty=Property(nameof(Description),typeof(string));
    public static readonly DependencyProperty TrendProperty=Property(nameof(Trend),typeof(BleeckerStatTrend));
    public static readonly DependencyProperty IconProperty=Property(nameof(Icon),typeof(object));
    public static readonly DependencyProperty IconTemplateProperty=Property(nameof(IconTemplate),typeof(DataTemplate));
    public static readonly DependencyProperty FooterProperty=Property(nameof(Footer),typeof(object));
    public static readonly DependencyProperty FooterTemplateProperty=Property(nameof(FooterTemplate),typeof(DataTemplate));
    public static readonly DependencyProperty SparklineDataProperty=Property(nameof(SparklineData),typeof(object));
    public static readonly DependencyProperty SparklineDataKeyProperty=Property(nameof(SparklineDataKey),typeof(string));
    public static readonly DependencyProperty SparklineColorProperty=Property(nameof(SparklineColor),typeof(SolidColorBrush));
    public string? Title {get=>(string?)GetValue(TitleProperty);set=>SetValue(TitleProperty,value);}
    public object? Value {get=>GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public DataTemplate? ValueTemplate {get=>(DataTemplate?)GetValue(ValueTemplateProperty);set=>SetValue(ValueTemplateProperty,value);}
    public string? Description {get=>(string?)GetValue(DescriptionProperty);set=>SetValue(DescriptionProperty,value);}
    public BleeckerStatTrend? Trend {get=>(BleeckerStatTrend?)GetValue(TrendProperty);set=>SetValue(TrendProperty,value);}
    public object? Icon {get=>GetValue(IconProperty);set=>SetValue(IconProperty,value);}
    public DataTemplate? IconTemplate {get=>(DataTemplate?)GetValue(IconTemplateProperty);set=>SetValue(IconTemplateProperty,value);}
    public object? Footer {get=>GetValue(FooterProperty);set=>SetValue(FooterProperty,value);}
    public DataTemplate? FooterTemplate {get=>(DataTemplate?)GetValue(FooterTemplateProperty);set=>SetValue(FooterTemplateProperty,value);}
    public ObservableCollection<IReadOnlyDictionary<string,object?>>? SparklineData {get=>(ObservableCollection<IReadOnlyDictionary<string,object?>>?)GetValue(SparklineDataProperty);set=>SetValue(SparklineDataProperty,value);}
    public string? SparklineDataKey {get=>(string?)GetValue(SparklineDataKeyProperty);set=>SetValue(SparklineDataKeyProperty,value);}
    public SolidColorBrush? SparklineColor {get=>(SolidColorBrush?)GetValue(SparklineColorProperty);set=>SetValue(SparklineColorProperty,value);}
    protected override void OnApplyTemplate()
    {
        DetachTypography();
        if(richValue is not null)richValue.Content=null;
        if(richIcon is not null)richIcon.Content=null;if(richFooter is not null)richFooter.Content=null;
        sparklineHost?.Children.Clear();trend=null;trendHost=null;
        base.OnApplyTemplate();
        title=GetTemplateChild("PART_Title") as TextBlock;value=GetTemplateChild("PART_Value") as TextBlock;richValue=GetTemplateChild("PART_RichValue") as ContentPresenter;
        iconText=GetTemplateChild("PART_IconText") as TextBlock;footerText=GetTemplateChild("PART_FooterText") as TextBlock;
        iconScalar=GetTemplateChild("PART_IconScalar") as TextBlock;footerScalar=GetTemplateChild("PART_FooterScalar") as TextBlock;
        if(GetTemplateChild("PART_IconScalarHost") is Grid scalarHost&&iconScalar is not null){scalarHost.Children.Clear();var panel=new IntrinsicTextPanel();panel.Children.Add(iconScalar);scalarHost.Children.Add(panel);}
        richIcon=GetTemplateChild("PART_RichIcon") as ContentPresenter;richFooter=GetTemplateChild("PART_RichFooter") as ContentPresenter;
        if(GetTemplateChild("PART_ValueHost") is Grid valueHost)
        {
            valueHost.Children.Clear();var panel=new ValuePanel();if(value is not null)panel.Children.Add(value);if(richValue is not null)panel.Children.Add(richValue);valueHost.Children.Add(panel);
        }
        description=GetTemplateChild("PART_Description") as TextBlock;header=GetTemplateChild("PART_Header") as Grid;
        foreach(var (name,text) in new[]{("PART_TitleHost",title),("PART_DescriptionHost",description)})
            if(GetTemplateChild(name) is Grid textHost&&text is not null){textHost.Children.Clear();var panel=new ValuePanel();panel.Children.Add(text);textHost.Children.Add(panel);}
        icon=GetTemplateChild("PART_Icon") as Border;footer=GetTemplateChild("PART_Footer") as Border;sparklineHost=GetTemplateChild("PART_SparklineHost") as Grid;sparkline=null;
        trendValue=GetTemplateChild("PART_TrendValue") as TextBlock;trendLabel=GetTemplateChild("PART_TrendLabel") as TextBlock;trendIcon=GetTemplateChild("PART_TrendIcon") as Viewbox;
        if(GetTemplateChild("PART_TrendHost") is Grid host)
        {
            trendHost=host;
            host.Children.Clear();trend=new TrendPanel();
            foreach(var child in new UIElement?[]{trendIcon,trendValue,trendLabel})if(child is not null)trend.Children.Add(child);
            host.Children.Add(trend);
        }
        AttachTypography();UpdatePresentation();
    }
    private void StopObserving(){if(observedTrend is not null)observedTrend.Changed-=TrendChanged;observedTrend=null;}
    private void TrendChanged(object? sender,EventArgs args)=>UpdatePresentation();
    private static string Text(string? text)=>Spaces.Replace(text??""," ").Trim(' ').Replace("\f","");
    private static bool Number(object? content)=>content is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
    private static bool FalsyNumber(object? content)=>Number(content)&&(Convert.ToDouble(content,CultureInfo.InvariantCulture) is double number&&(number==0||double.IsNaN(number)));
    private static bool Present(object? content)=>content is not null&&content is not false&&content is not string{Length:0}&&!FalsyNumber(content);
    private static string ScalarText(object? content)=>content switch{null or bool=>"",double number=>BleeckerChartScale.TickLabel(number),float number=>BleeckerChartScale.TickLabel(double.Parse(number.ToString("R",CultureInfo.InvariantCulture),CultureInfo.InvariantCulture)),decimal number when number==0=>"0",_=>Text(Convert.ToString(content,CultureInfo.InvariantCulture))};
    private static void SetText(TextBlock? target,string text){if(target is null)return;target.Text=text;if(text.Length==0)target.Height=0;else target.ClearValue(HeightProperty);}
    private void UpdatePresentation()
    {
        if(!ReferenceEquals(observedTrend,IsLoaded?Trend:null)){StopObserving();if(IsLoaded&&Trend is not null){observedTrend=Trend;observedTrend.Changed+=TrendChanged;}}
        SetText(title,BleeckerTextCasing.Uppercase(Text(Title)));
        var plain=ValueTemplate is null&&Value is not UIElement;
        SetText(value,plain?ScalarText(Value):"");
        if(value is not null)value.Visibility=plain?Visibility.Visible:Visibility.Collapsed;
        if(richValue is not null){richValue.Content=plain?null:Value;richValue.Visibility=plain?Visibility.Collapsed:Visibility.Visible;}
        var hasTrend=Trend is not null;var hasDescription=!hasTrend&&!string.IsNullOrEmpty(Description);
        SetText(description,Text(Description));if(description is not null)description.Visibility=hasDescription?Visibility.Visible:Visibility.Collapsed;
        if(GetTemplateChild("PART_DescriptionHost") is Grid descriptionHost)descriptionHost.Visibility=hasDescription?Visibility.Visible:Visibility.Collapsed;
        if(trend is not null)trend.Visibility=hasTrend?Visibility.Visible:Visibility.Collapsed;
        if(trendHost is not null)trendHost.Visibility=hasTrend?Visibility.Visible:Visibility.Collapsed;
        if(trendValue is not null)SetText(trendValue,Text(Trend?.Value));
        SetText(trendLabel,Text(Trend?.Label));if(trendLabel is not null)trendLabel.Visibility=string.IsNullOrEmpty(Trend?.Label)?Visibility.Collapsed:Visibility.Visible;
        if(trendIcon is not null&&Trend is not null)
        {
            var key=Trend.Direction switch{BleeckerStatTrendDirection.Up=>"BleeckerStatTrendUpBrush",BleeckerStatTrendDirection.Down=>"BleeckerTerracottaBrush",BleeckerStatTrendDirection.Neutral=>"BleeckerTextSecondaryBrush",_=>throw new ArgumentOutOfRangeException(nameof(Trend.Direction))};
            var data=Trend.Direction switch{BleeckerStatTrendDirection.Up=>"M16 7h6v6 M22 7l-8.5 8.5-5-5L2 17",BleeckerStatTrendDirection.Down=>"M16 17h6v-6 M22 17l-8.5-8.5-5 5L2 7",_=>"M5 12h14"};
            var vector=(Grid)XamlReader.Load($"<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Width=\"24\" Height=\"24\"><Path Data=\"{data}\" Stroke=\"{{ThemeResource {key}}}\" StrokeThickness=\"2\" StrokeStartLineCap=\"Round\" StrokeEndLineCap=\"Round\" StrokeLineJoin=\"Round\" /></Grid>");
            trendIcon.Child=vector;
            trendValue?.SetBinding(TextBlock.ForegroundProperty,new Microsoft.UI.Xaml.Data.Binding{Source=((Microsoft.UI.Xaml.Shapes.Path)vector.Children[0]),Path=new PropertyPath("Stroke")});
        }
        UpdateSlot(Icon,IconTemplate,icon,iconText,richIcon,iconScalar);
        UpdateSlot(Footer,FooterTemplate,footer,footerText,richFooter,footerScalar);
        if(header is not null)header.ColumnSpacing=IconTemplate is not null||Present(Icon)||FalsyNumber(Icon)?24:0;
        if(sparklineHost is not null)
        {
            var show=SparklineData is not null&&!string.IsNullOrEmpty(SparklineDataKey);
            if(show)
            {
                if(sparkline is null){sparkline=new BleeckerSparkline{Data=SparklineData!,DataKey=SparklineDataKey,Height=48,ShowArea=true};sparklineHost.Children.Add(sparkline);}
                else{sparkline.Data=SparklineData!;sparkline.DataKey=SparklineDataKey;}
                sparkline.Color=SparklineColor;
            }
            else{sparklineHost.Children.Clear();sparkline=null;}
            sparklineHost.Visibility=show?Visibility.Visible:Visibility.Collapsed;
        }
        FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private static void UpdateSlot(object? content,DataTemplate? template,Border? wrapper,TextBlock? text,ContentPresenter? rich,TextBlock? scalar)
    {
        var plain=template is null&&(content is null or string or bool||Number(content));
        var anonymous=template is null&&FalsyNumber(content);
        if(wrapper is not null)wrapper.Visibility=template is not null||Present(content)?Visibility.Visible:Visibility.Collapsed;
        SetText(text,plain?ScalarText(content):"");if(text is not null)text.Visibility=plain?Visibility.Visible:Visibility.Collapsed;
        if(rich is not null){rich.Content=plain?null:content;rich.Visibility=plain?Visibility.Collapsed:Visibility.Visible;}
        SetText(scalar,anonymous?ScalarText(content):"");if(scalar is not null)scalar.Visibility=anonymous?Visibility.Visible:Visibility.Collapsed;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new StatPeer(this);
    private sealed class StatPeer(BleeckerStatCard owner):FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()=>nameof(BleeckerStatCard);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
        protected override string GetNameCore()=>BleeckerAutomationMetadata.TryGetName(owner,this,out var name)?name:string.IsNullOrEmpty(base.GetNameCore())?BleeckerTextCasing.Uppercase(Text(owner.Title)):base.GetNameCore();
    }
    private sealed class TrendPanel:Panel
    {
        private double valueWidth,labelWidth;
        protected override Size MeasureOverride(Size available)
        {
            var visible=Children.Where(child=>child.Visibility==Visibility.Visible).ToArray();if(visible.Length==0)return new Size();
            foreach(var child in visible)child.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            // CSS flex text allocations round their intrinsic advances outward
            // to 1/64px. Keep that boundary before placing the following item.
            var text=(TextBlock)visible[1];valueWidth=Math.Ceiling(text.DesiredSize.Width*64)/64;labelWidth=visible.Length>2?Math.Ceiling(visible[2].DesiredSize.Width*64)/64:0;
            if(visible.Length>2&&double.IsFinite(available.Width))
            {
                var label=(TextBlock)visible[2];var room=Math.Max(0,available.Width-14-16);var total=valueWidth+labelWidth;
                if(total>room){var minValue=Minimum(text);var minLabel=Minimum(label);valueWidth=Math.Max(minValue,room*valueWidth/total);labelWidth=Math.Max(minLabel,room-valueWidth);valueWidth=Math.Max(minValue,Math.Min(valueWidth,room-labelWidth));}
                label.Measure(new Size(labelWidth,double.PositiveInfinity));
            }
            text.Measure(new Size(valueWidth,double.PositiveInfinity));
            // The flex container keeps its allocation even when its items reach
            // their word minima. Report that box so WinUI does not create a
            // layout clip around the deliberately overflowing children.
            return new Size(Math.Min(available.Width,14+8+valueWidth+(visible.Length>2?8+labelWidth:0)),Math.Max(14,visible.Skip(1).Max(child=>child.DesiredSize.Height)));
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            var x=0d;var visible=Children.Where(child=>child.Visibility==Visibility.Visible).ToArray();
            for(var i=0;i<visible.Length;i++){var child=visible[i];var width=i==0?14:i==1?valueWidth:labelWidth;child.Arrange(new Rect(x,(finalSize.Height-child.DesiredSize.Height)/2,width,child.DesiredSize.Height));x+=width+8;}
            return finalSize;
        }
        internal static double Minimum(TextBlock source)
        {
            var probe=new TextBlock{UseLayoutRounding=false,FontFamily=source.FontFamily,FontSize=source.FontSize,FontWeight=source.FontWeight,FontStyle=source.FontStyle,FontStretch=source.FontStretch,CharacterSpacing=source.CharacterSpacing,FlowDirection=source.FlowDirection,TextReadingOrder=source.TextReadingOrder,IsTextScaleFactorEnabled=source.IsTextScaleFactorEnabled};var width=0d;
            foreach(var word in source.Text.Split(' ',StringSplitOptions.RemoveEmptyEntries)){probe.Text=word;probe.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));width=Math.Max(width,probe.DesiredSize.Width);}
            return Math.Ceiling(width*64)/64;
        }
    }
    private sealed class ValuePanel:Panel
    {
        private readonly Dictionary<UIElement,double> minima=[];
        protected override Size MeasureOverride(Size available)
        {
            var height=0d;var width=0d;minima.Clear();
            foreach(var child in Children.Where(child=>child.Visibility==Visibility.Visible))
            {
                var minimum=child is TextBlock text?TrendPanel.Minimum(text):child is ContentPresenter{Content:FrameworkElement content}&&double.IsFinite(content.Width)?content.Width:0;
                minima[child]=minimum;child.Measure(new Size(available.Width,double.PositiveInfinity));height=Math.Max(height,child.DesiredSize.Height);width=Math.Max(width,child.DesiredSize.Width);
            }
            return new Size(Math.Min(available.Width,width),height);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach(var child in Children.Where(child=>child.Visibility==Visibility.Visible))child.Arrange(new Rect(0,0,Math.Max(finalSize.Width,Math.Max(child.DesiredSize.Width,minima.GetValueOrDefault(child))),child.DesiredSize.Height));
            return finalSize;
        }
    }
    private sealed class IntrinsicTextPanel:Panel
    {
        protected override Size MeasureOverride(Size available)
        {
            var child=Children[0];child.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
            return new Size(Math.Ceiling(child.DesiredSize.Width*64)/64,child.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size finalSize){Children[0].Arrange(new Rect(0,0,finalSize.Width,finalSize.Height));return finalSize;}
    }
}
