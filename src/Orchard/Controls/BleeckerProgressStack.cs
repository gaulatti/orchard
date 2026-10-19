using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A passive segmented display with a wrapping label/value legend.</summary>
public sealed class BleeckerProgressStack : Control
{
    private StackLayout? layout;
    private TextBlock? secondary;
    private long secondaryToken;
    private ObservableCollection<BleeckerProgressStackItem>? observed;
    private readonly HashSet<BleeckerProgressStackItem> observedItems = [];
    public BleeckerProgressStack()
    {
        DefaultStyleKey=typeof(BleeckerProgressStack); Items=[];
        Loaded+=(_,_)=>{ Observe(); Render(); }; Unloaded+=(_,_)=>StopObserving();
        foreach(var property in new[]{BackgroundProperty,ForegroundProperty,FontFamilyProperty,FontSizeProperty,FontWeightProperty,FontStyleProperty,FontStretchProperty,CharacterSpacingProperty,LanguageProperty,FlowDirectionProperty,IsTextScaleFactorEnabledProperty,CornerRadiusProperty})
            RegisterPropertyChangedCallback(property,(_,_)=>Render());
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)Detach();});
    }
    private static DependencyProperty Property(string name,Type type,object? initial=null)=>DependencyProperty.Register(name,type,typeof(BleeckerProgressStack),new PropertyMetadata(initial,(owner,_)=>{var control=(BleeckerProgressStack)owner;if(control.IsLoaded)control.Observe();control.Render();}));
    public static readonly DependencyProperty ItemsProperty=Property(nameof(Items),typeof(object));
    public static readonly DependencyProperty MaxProperty=Property(nameof(Max),typeof(object));
    public static readonly DependencyProperty ShowLegendProperty=Property(nameof(ShowLegend),typeof(bool),true);
    public static readonly DependencyProperty TrackHeightProperty=Property(nameof(TrackHeight),typeof(double),12d);
    public ObservableCollection<BleeckerProgressStackItem> Items { get=>GetValue(ItemsProperty) as ObservableCollection<BleeckerProgressStackItem>??throw new InvalidOperationException("ProgressStack requires an observable item collection.");set{ArgumentNullException.ThrowIfNull(value);SetValue(ItemsProperty,value);} }
    public double? Max { get=>(double?)GetValue(MaxProperty);set=>SetValue(MaxProperty,value); }
    public bool ShowLegend { get=>(bool)GetValue(ShowLegendProperty);set=>SetValue(ShowLegendProperty,value); }
    public double TrackHeight { get=>(double)GetValue(TrackHeightProperty);set{if(!double.IsFinite(value)||value<0)throw new ArgumentOutOfRangeException(nameof(value));SetValue(TrackHeightProperty,value);} }
    protected override void OnApplyTemplate()
    {
        Detach();base.OnApplyTemplate();if(Template is null)return;
        var host=GetTemplateChild("PART_LayoutHost") as Grid??throw new InvalidOperationException("ProgressStack requires PART_LayoutHost.");
        secondary=GetTemplateChild("PART_Secondary") as TextBlock??throw new InvalidOperationException("ProgressStack requires PART_Secondary.");
        secondaryToken=secondary.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>Render());
        layout=new(this);host.Children.Add(layout);Render();
    }
    private void Detach(){if(secondary is not null)secondary.UnregisterPropertyChangedCallback(TextBlock.ForegroundProperty,secondaryToken);secondary=null;if(layout?.Parent is Panel host)host.Children.Remove(layout);layout=null;}
    private void Observe()
    {
        StopObserving();observed=Items;observed.CollectionChanged+=CollectionChanged;
        foreach(var item in Items){if(item is null)throw new ArgumentException("ProgressStack items cannot be null.");if(observedItems.Add(item))item.PropertyChanged+=ItemChanged;}
    }
    private void StopObserving(){if(observed is not null)observed.CollectionChanged-=CollectionChanged;foreach(var item in observedItems)item.PropertyChanged-=ItemChanged;observed=null;observedItems.Clear();}
    private void CollectionChanged(object? sender,NotifyCollectionChangedEventArgs args){Observe();Render();}
    private void ItemChanged(object? sender,PropertyChangedEventArgs args)=>Render();
    private void Render(){layout?.Update();FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();}
    protected override AutomationPeer OnCreateAutomationPeer()=>new StackPeer(this);
    private sealed class StackPeer(BleeckerProgressStack owner):FrameworkElementAutomationPeer(owner)
    {
        protected override string GetClassNameCore()=>nameof(BleeckerProgressStack);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
        protected override string GetNameCore()=>AutomationProperties.GetName(owner);
    }
    private sealed class StackLayout:Panel
    {
        private readonly BleeckerProgressStack owner;
        private readonly Border track=new(){Name="ProgressStackTrack",UseLayoutRounding=false};
        private readonly SegmentPanel segments;
        private readonly LegendPanel legend;
        internal StackLayout(BleeckerProgressStack owner)
        {
            this.owner=owner;UseLayoutRounding=false;segments=new(owner);legend=new(owner);track.Child=segments;Children.Add(track);Children.Add(legend);
        }
        internal void Update()
        {
            track.Background=owner.Background;track.CornerRadius=owner.CornerRadius;track.Height=owner.TrackHeight;
            segments.Update();legend.Update();legend.Visibility=owner.ShowLegend?Visibility.Visible:Visibility.Collapsed;InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            var width=double.IsPositiveInfinity(available.Width)?legend.IntrinsicWidth:available.Width;
            track.Measure(new(width,owner.TrackHeight));legend.Measure(new(width,double.PositiveInfinity));
            return new(width,owner.TrackHeight+(owner.ShowLegend?12+legend.DesiredSize.Height:0));
        }
        protected override Size ArrangeOverride(Size final)
        {
            track.Arrange(new(0,0,final.Width,owner.TrackHeight));
            if(owner.ShowLegend)legend.Arrange(new(0,owner.TrackHeight+12,final.Width,legend.DesiredSize.Height));return final;
        }
    }
    private sealed class SegmentPanel(BleeckerProgressStack owner):Panel
    {
        private double[] widths=[];
        internal void Update()
        {
            UseLayoutRounding=false;while(Children.Count>owner.Items.Count)Children.RemoveAt(Children.Count-1);
            for(var i=0;i<owner.Items.Count;i++)
            {
                if(i==Children.Count)Children.Add(new Border{UseLayoutRounding=false,Name="ProgressStackSegment"});
                var item=owner.Items[i]??throw new ArgumentException("ProgressStack items cannot be null.");var segment=(Border)Children[i];segment.Background=item.Color;
                ToolTipService.SetToolTip(segment,item.Label+": "+BleeckerNumberText.Format(item.Value));
                AutomationProperties.SetAccessibilityView(segment,AccessibilityView.Raw);
            }
            InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            var total=owner.Max??owner.Items.Aggregate(0d,(sum,item)=>sum+item.Value);
            // CSS stores the parsed percentage as a float before allocating
            // 1/64px layout units; tiny remaining shares can round to 100%.
            var basis=owner.Items.Select(item=>total>0?(float)(item.Value/total*100)*available.Width/100:0).Select(value=>double.IsFinite(value)&&value>0?Math.Truncate(value*64)/64:0).ToArray();
            var sum=basis.Sum();widths=basis.Select(value=>sum>available.Width?Math.Floor(value/sum*available.Width*64+.5)/64:value).ToArray();
            for(var i=0;i<Children.Count;i++){var segment=(Border)Children[i];var radius=Children.Count==1?Math.Min(widths[i],owner.TrackHeight)/2:Math.Min(widths[i],owner.TrackHeight/2);segment.CornerRadius=new(i==0?radius:0,i==Children.Count-1?radius:0,i==Children.Count-1?radius:0,i==0?radius:0);segment.Measure(new(widths[i],owner.TrackHeight));}
            return new(available.Width,owner.TrackHeight);
        }
        protected override Size ArrangeOverride(Size final){var x=0d;for(var i=0;i<Children.Count;i++){Children[i].Arrange(new(x,0,widths[i],owner.TrackHeight));x+=widths[i];}return final;}
    }
    private sealed class LegendPanel(BleeckerProgressStack owner):Panel
    {
        private readonly List<Rect> positions=[];
        internal double IntrinsicWidth=>Children.Cast<LegendItem>().Sum(item=>item.MaximumWidth)+Math.Max(0,Children.Count-1)*12;
        internal void Update()
        {
            Name="ProgressStackLegend";UseLayoutRounding=false;
            while(Children.Count>owner.Items.Count)Children.RemoveAt(Children.Count-1);
            for(var i=0;i<owner.Items.Count;i++){if(i==Children.Count)Children.Add(new LegendItem(owner));((LegendItem)Children[i]).Update(owner.Items[i]);}InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            positions.Clear();var x=0d;var y=0d;var rowHeight=0d;
            foreach(var item in Children.Cast<LegendItem>())
            {
                var width=Math.Min(item.MaximumWidth,Math.Max(item.MinimumWidth,available.Width));
                if(x>0&&x+width>available.Width){x=0;y+=rowHeight+12;rowHeight=0;}
                item.Measure(new(width,double.PositiveInfinity));positions.Add(new(x,y,width,item.DesiredSize.Height));x+=width+12;rowHeight=Math.Max(rowHeight,item.DesiredSize.Height);
            }
            return new(available.Width,Children.Count==0?0:y+rowHeight);
        }
        protected override Size ArrangeOverride(Size final){for(var i=0;i<Children.Count;i++)Children[i].Arrange(positions[i]);return final;}
    }
    private sealed class LegendItem:Panel
    {
        private readonly BleeckerProgressStack owner;
        private readonly Border swatch=new(){Name="ProgressStackSwatch",UseLayoutRounding=false,CornerRadius=new(5)};
        private readonly TextSlot label=new("ProgressStackLabel"),value=new("ProgressStackValue");
        private double swatchWidth,labelWidth,valueWidth,height;
        internal LegendItem(BleeckerProgressStack owner){this.owner=owner;Name="ProgressStackItem";UseLayoutRounding=false;Children.Add(swatch);Children.Add(label);Children.Add(value);}
        internal double MaximumWidth=>22+label.MaximumWidth+value.MaximumWidth;
        internal double MinimumWidth=>22+label.MinimumWidth+value.MinimumWidth;
        internal void Update(BleeckerProgressStackItem item){swatch.Background=item.Color;label.Set(owner,item.Label,false);value.Set(owner,BleeckerNumberText.Format(item.Value),true);InvalidateMeasure();}
        protected override Size MeasureOverride(Size available)
        {
            var free=available.Width-12;var a=10d;var b=label.MaximumWidth;var c=value.MaximumWidth;
            var frozen=new bool[3];var basis=new[]{a,b,c};var min=new[]{0d,label.MinimumWidth,value.MinimumWidth};var sizes=basis.ToArray();
            for(var pass=0;pass<3;pass++)
            {
                var used=Enumerable.Range(0,3).Where(i=>frozen[i]).Sum(i=>sizes[i]);var total=Enumerable.Range(0,3).Where(i=>!frozen[i]).Sum(i=>basis[i]);var shrink=free-used-total;
                for(var i=0;i<3;i++)if(!frozen[i]){sizes[i]=basis[i]+(shrink<0&&total>0?shrink*basis[i]/total:0);if(sizes[i]<min[i]){sizes[i]=min[i];frozen[i]=true;}}
            }
            swatchWidth=Math.Floor(sizes[0]*64+.5)/64;labelWidth=Math.Floor(sizes[1]*64+.5)/64;valueWidth=Math.Floor(sizes[2]*64+.5)/64;
            swatch.Measure(new(swatchWidth,10));label.Measure(new(labelWidth,double.PositiveInfinity));value.Measure(new(valueWidth,double.PositiveInfinity));height=Math.Max(10,Math.Max(label.DesiredSize.Height,value.DesiredSize.Height));return new(available.Width,height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            static double Center(double outer,double inner)=>Math.Truncate((outer-inner)*32)/64;
            swatch.Arrange(new(0,Center(height,10),swatchWidth,10));label.Arrange(new(swatchWidth+6,Center(height,label.DesiredSize.Height),labelWidth,label.DesiredSize.Height));value.Arrange(new(swatchWidth+labelWidth+12,Center(height,value.DesiredSize.Height),valueWidth,value.DesiredSize.Height));return final;
        }
    }
    private sealed class TextSlot(string name):Panel
    {
        private readonly TextBlock text=new(){UseLayoutRounding=false,TextWrapping=TextWrapping.NoWrap,LineStackingStrategy=LineStackingStrategy.BlockLineHeight};
        private readonly TextBlock probe=new();
        private readonly BleeckerReferenceText.IntrinsicWidth intrinsic=new(joinWhitespace:true);
        private string logical="";
        private double paintWidth;
        internal double MaximumWidth=>WidthOf(logical);
        internal double MinimumWidth=>logical.Length==0?0:logical.Split(' ').Max(WidthOf);
        private double WidthOf(string content){BleeckerReferenceText.Set(probe,content);intrinsic.Measure(probe,content);return intrinsic.WhitespaceJoinedWidth;}
        internal void Set(BleeckerProgressStack owner,string content,bool semibold)
        {
            Name=name;UseLayoutRounding=false;if(Children.Count==0)Children.Add(text);
            var family=semibold&&owner.FontFamily.Source.Contains("/Orchard/Assets/Fonts/EncodeSans-Regular.ttf",StringComparison.Ordinal)?(FontFamily)Application.Current.Resources["BleeckerPrimarySemiboldFontFamily"]:owner.FontFamily;
            text.FontFamily=probe.FontFamily=family;text.FontSize=probe.FontSize=owner.FontSize;text.FontWeight=probe.FontWeight=semibold?Microsoft.UI.Text.FontWeights.SemiBold:owner.FontWeight;text.FontStyle=probe.FontStyle=owner.FontStyle;text.FontStretch=probe.FontStretch=owner.FontStretch;text.CharacterSpacing=probe.CharacterSpacing=owner.CharacterSpacing;text.IsTextScaleFactorEnabled=probe.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;text.FlowDirection=probe.FlowDirection=owner.FlowDirection;
            if(text.Language!=owner.Language)text.Language=owner.Language;if(probe.Language!=owner.Language)probe.Language=owner.Language;
            text.LineHeight=owner.FontSize*4/3;
            text.Foreground=semibold?owner.Foreground:owner.secondary!.Foreground;
            logical=Regex.Replace(content,"[ \\t\\r\\n\\f]+"," ").Trim(' ');text.Visibility=logical.Length==0?Visibility.Collapsed:Visibility.Visible;if(logical.Length==0)text.Text="";InvalidateMeasure();
        }
        protected override Size MeasureOverride(Size available)
        {
            if(logical.Length==0){paintWidth=0;return new(available.Width,0);}
            var lines=new List<string>();var current="";foreach(var word in logical.Split(' ')){var next=current.Length==0?word:current+" "+word;if(current.Length>0&&WidthOf(next)>available.Width){lines.Add(current);current=word;}else current=next;}lines.Add(current);
            paintWidth=Math.Max(available.Width,lines.Max(WidthOf));var rendered=string.Join('\n',lines);if(text.Text!=rendered)BleeckerReferenceText.Set(text,rendered);text.Measure(new(paintWidth,double.PositiveInfinity));return new(available.Width,text.DesiredSize.Height);
        }
        protected override Size ArrangeOverride(Size final){text.Arrange(new(0,0,paintWidth,final.Height));return final;}
    }
}
