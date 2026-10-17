using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using ShapePath=Microsoft.UI.Xaml.Shapes.Path;

namespace Orchard;
public sealed partial class BleeckerAccordion
{
    private sealed class Entry:StackPanel
    {
        private readonly BleeckerAccordion owner;
        internal string Key {get;}
        internal BleeckerAccordionItem Item {get;private set;}
        internal HeaderButton Header {get;}
        internal Border Divider {get;}=new(){Height=1,IsHitTestVisible=false};
        private readonly Grid headerRow=new(){UseLayoutRounding=false,ColumnDefinitions={new(){Width=new GridLength(1,GridUnitType.Star)},new(){Width=GridLength.Auto}}};
        private readonly HeaderLayout lead=new();
        private readonly Slot title=new("AccordionTitle",14,20),subtitle=new("AccordionSubtitle",12,16,true),actions=new("AccordionActions",16,24),body=new("AccordionContent",16,24);
        private readonly Border content=new(){UseLayoutRounding=false,BorderThickness=new Thickness(0,1,0,0)};
        private readonly RevealPanel reveal=new(){UseLayoutRounding=false,HorizontalContentAlignment=HorizontalAlignment.Stretch};
        private readonly SolidColorBrush hover=new();
        private readonly RotateTransform angle=new(){CenterX=8,CenterY=8};
        private readonly Stopwatch clock=Stopwatch.StartNew();
        private readonly BleeckerCssTransition<double> expansion=new((a,b,t)=>a+(b-a)*t,Expo);
        private readonly BleeckerCssTransition<BleeckerCssColor> background=new(BleeckerCssColor.Mix);
        private bool expanded,hovered,rendering;
        internal Entry(BleeckerAccordion owner,BleeckerAccordionItem item)
        {
            this.owner=owner;Item=item;Key=item.Id;UseLayoutRounding=false;Name="AccordionRow";
            var arrow=new ShapePath{Name="AccordionChevron",Width=16,Height=16,Stretch=Stretch.None,StrokeThickness=4d/3,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Data=new PathGeometry{Figures={new PathFigure{StartPoint=new(4,6),Segments={new PolyLineSegment{Points={new(8,10),new(12,6)}}}}}},RenderTransform=angle};
            AutomationProperties.SetAccessibilityView(arrow,AccessibilityView.Raw);lead.Arrow=arrow;lead.Title=title;lead.Subtitle=subtitle;lead.Children.Add(arrow);lead.Children.Add(title);lead.Children.Add(subtitle);
            Header=new(this){Name="AccordionHeader",Content=lead,Style=(Style)new ResourceDictionary{Source=new Uri("ms-appx:///Orchard/Themes/Accordion.xaml")}["BleeckerAccordionHeaderStyle"]};
            Header.Click+=(_,_)=>{if(owner.Owns(this)&&owner.IsEnabled&&Header.IsEnabled)owner.SetExpanded(this,!expanded);};headerRow.Children.Add(Header);Grid.SetColumn(actions,1);headerRow.Children.Add(actions);headerRow.Background=hover;
            headerRow.PointerEntered+=(_,_)=>{hovered=true;PaintBackground();};headerRow.PointerExited+=(_,_)=>{hovered=false;PaintBackground();};
            content.Child=body;reveal.Content=content;Children.Add(headerRow);Children.Add(reveal);Children.Add(Divider);
            Loaded+=(_,_)=>{hovered=false;Update(Item);UpdateExpansion(false);};Unloaded+=(_,_)=>{if(!IsLoaded)Stop();};
        }
        internal void Update(BleeckerAccordionItem item)
        {
            Item=item;var primary=owner.Foreground;var secondary=owner.Paint("Secondary");
            title.Set(owner,item.Title,owner.TitleTemplate,primary);subtitle.Set(owner,item.Subtitle,owner.SubtitleTemplate,secondary);
            lead.HasSubtitle=owner.SubtitleTemplate is not null||Present(item.Subtitle);lead.Padding=owner.Compact?16:20;
            subtitle.Visibility=lead.HasSubtitle?Visibility.Visible:Visibility.Collapsed;
            actions.Set(owner,item.Actions,owner.ActionsTemplate,primary);actions.Visibility=owner.ActionsTemplate is not null||Present(item.Actions)?Visibility.Visible:Visibility.Collapsed;actions.Margin=new(0,0,owner.Compact?16:20,0);actions.VerticalAlignment=VerticalAlignment.Center;
            body.Set(owner,item.Content,owner.ContentTemplate,primary);content.Background=owner.Paint("Body");content.BorderBrush=owner.Paint("BodyBorder");Divider.Background=owner.Paint("Divider");lead.Arrow.Stroke=secondary;
            Header.InvalidateMeasure();FrameworkElementAutomationPeer.FromElement(Header)?.InvalidatePeer();PaintBackground();UpdateExpansion(IsLoaded);
        }
        internal void UpdateExpansion(bool animate)
        {
            var next=owner.ExpandedId==Key;var before=expanded;expanded=next;
            if(!next&&before&&owner.XamlRoot is not null){var focused=Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(owner.XamlRoot) as DependencyObject;for(var current=focused;current is not null;current=VisualTreeHelper.GetParent(current))if(ReferenceEquals(current,reveal)){Header.Focus(FocusState.Keyboard);break;}}
            expansion.Retarget(next?1:0,clock.Elapsed.TotalMilliseconds,200,animate&&IsLoaded&&owner.settings.AnimationsEnabled&&!owner.accessibility.HighContrast);
            if(next)content.Visibility=Visibility.Visible;Paint();
            if(before!=next)FrameworkElementAutomationPeer.FromElement(Header)?.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,before?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed,next?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed);
        }
        private void PaintBackground()
        {
            var paint=(SolidColorBrush)owner.Paint("Hover");var color=paint.Color;var value=hovered?BleeckerCssColor.FromRgba((color.R,color.G,color.B,color.A/255d*paint.Opacity),false):BleeckerCssColor.FromRgba((color.R,color.G,color.B,0),false);
            background.Retarget(value,clock.Elapsed.TotalMilliseconds,220,IsLoaded&&owner.settings.AnimationsEnabled&&!owner.accessibility.HighContrast);Paint();
        }
        private void Paint()
        {
            var time=clock.Elapsed.TotalMilliseconds;var fraction=expansion.ValueAt(time);reveal.Fraction=fraction;angle.Angle=-90*(1-fraction);if(!expanded&&!expansion.IsRunning(time))content.Visibility=Visibility.Collapsed;
            var color=background.ValueAt(time).Encode();hover.Color=Windows.UI.Color.FromArgb(255,color.R,color.G,color.B);hover.Opacity=color.Alpha;
            if(expansion.IsRunning(time)||background.IsRunning(time)){if(!rendering){CompositionTarget.Rendering+=Frame;rendering=true;}}else Stop();
        }
        private void Frame(object? sender,object args)=>Paint();
        internal void Stop(){if(rendering)CompositionTarget.Rendering-=Frame;rendering=false;}
        internal void Release(){Stop();title.Release();subtitle.Release();actions.Release();body.Release();}
        internal string AccessibleName=>string.Join(" ",new[]{title.AccessibleName,lead.HasSubtitle?subtitle.AccessibleName:""}.Where(text=>text.Length!=0));
        private static double Expo(double x){if(x<=0)return 0;if(x>=1)return 1;double low=0,high=1;for(var i=0;i<28;i++){var t=(low+high)/2;var sample=3*(1-t)*(1-t)*t*.16+3*(1-t)*t*t*.3+t*t*t;if(sample<x)low=t;else high=t;}return 1-Math.Pow(1-(low+high)/2,3);}
        internal sealed class HeaderButton(Entry entry):Button
        {
            protected override AutomationPeer OnCreateAutomationPeer()=>new Peer(this,entry);
            private sealed class Peer(HeaderButton button,Entry entry):ButtonAutomationPeer(button),IExpandCollapseProvider,IInvokeProvider
            {
                public ExpandCollapseState ExpandCollapseState=>entry.expanded?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed;
                public void Expand()=>entry.owner.SetExpanded(entry,true);
                public void Collapse()=>entry.owner.SetExpanded(entry,false);
                void IInvokeProvider.Invoke()=>entry.owner.SetExpanded(entry,!entry.expanded);
                protected override string GetNameCore(){var specified=AutomationProperties.GetName(button);return specified.Length>0?specified:entry.AccessibleName;}
                protected override object GetPatternCore(PatternInterface pattern)=>pattern is PatternInterface.ExpandCollapse or PatternInterface.Invoke?this:base.GetPatternCore(pattern);
            }
        }
    }
    private sealed class HeaderLayout:Panel
    {
        internal ShapePath Arrow=null!;
        internal Slot Title=null!,Subtitle=null!;
        internal bool HasSubtitle;
        internal double Padding;
        private double textWidth,titleHeight,subtitleHeight;
        protected override Size MeasureOverride(Size available)
        {
            var inner=Math.Max(0,available.Width-Padding*2-28);Title.Measure(new(double.PositiveInfinity,double.PositiveInfinity));Subtitle.Measure(new(double.PositiveInfinity,double.PositiveInfinity));textWidth=Math.Min(inner,Math.Max(Title.DesiredSize.Width,HasSubtitle?Subtitle.DesiredSize.Width:0));
            Title.Measure(new(textWidth,double.PositiveInfinity));Subtitle.Measure(new(textWidth,double.PositiveInfinity));Arrow.Measure(new(16,16));titleHeight=Title.DesiredSize.Height;subtitleHeight=HasSubtitle?Subtitle.DesiredSize.Height:0;
            return new(double.IsInfinity(available.Width)?Padding*2+28+textWidth:available.Width,32+Math.Max(16,titleHeight+(HasSubtitle?4+subtitleHeight:0)));
        }
        protected override Size ArrangeOverride(Size final)
        {
            var textHeight=titleHeight+(HasSubtitle?4+subtitleHeight:0);Arrow.Arrange(new(Padding,(final.Height-16)/2,16,16));Title.Arrange(new(Padding+28,(final.Height-textHeight)/2,textWidth,titleHeight));Subtitle.Arrange(new(Padding+28,(final.Height-textHeight)/2+titleHeight+4,textWidth,subtitleHeight));return final;
        }
    }
    private sealed class RevealPanel:ContentControl
    {
        private double fraction,fullHeight;
        internal double Fraction {get=>fraction;set{if(fraction==value)return;fraction=value;InvalidateMeasure();}}
        protected override Size MeasureOverride(Size available){var size=base.MeasureOverride(new(available.Width,double.PositiveInfinity));fullHeight=size.Height;return new(size.Width,size.Height*fraction);}
        protected override Size ArrangeOverride(Size final){base.ArrangeOverride(new(final.Width,fullHeight));Clip=new RectangleGeometry{Rect=new(0,0,final.Width,final.Height)};return final;}
    }
}
