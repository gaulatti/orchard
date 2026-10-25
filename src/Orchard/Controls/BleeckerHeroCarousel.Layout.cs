using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerHeroCarousel
{
    private sealed class HeroPanel : Panel, IDisposable
    {
        private readonly BleeckerHeroCarousel owner;
        private readonly BleeckerHeroImage emptyBackground = new() { Name="HeroEmptyBackground" };
        private readonly BleeckerContentText empty = new("HeroEmptyContent","BleeckerPrimaryFontFamily",20,28,"BleeckerHeroForegroundBrush");
        private readonly FontFamily defaultEmptyFamily;
        private readonly Dictionary<string,DotSlot> dots=new(StringComparer.Ordinal);
        private readonly BleeckerCssTransition<double> arrows=new((a,b,p)=>a+(b-a)*p,EaseInOut);
        private Border? navigation;
        private DotPanel? dotPanel;
        private BleeckerIconButton? previous,next;
        private readonly HeroBackdrop navigationBlur=new("HeroNavigationBackdrop");
        private Rect previousBox,nextBox,navigationBox,emptyBox;
        private readonly Dictionary<BleeckerIconButton,HeroBackdrop> arrowBlurs=[];
        internal HeroPanel(BleeckerHeroCarousel owner)
        {
            this.owner=owner;defaultEmptyFamily=empty.Text.FontFamily;empty.Text.TextAlignment=TextAlignment.Center;Children.Add(emptyBackground);Children.Add(empty);Children.Add(navigationBlur.Host);
            Canvas.SetZIndex(emptyBackground,-2);Canvas.SetZIndex(empty,-1);Canvas.SetZIndex(navigationBlur.Host,999999);
        }
        internal IEnumerable<FrameworkElement> AutomationControls=>navigation is null?[]:new FrameworkElement[]{previous!}.Concat(owner.items.Where(i=>dots.ContainsKey(Key(i))).Select(i=>(FrameworkElement)dots[Key(i)].Button)).Append(next!);
        internal IEnumerable<FrameworkElement> AutomationEmptyContent=>owner.items.Count==0?[empty]:[];
        internal void AddSlide(UIElement element)=>Children.Add(element);
        internal void ClearSlides(){foreach(var element in Children.OfType<ContentControl>().Where(e=>e.Name=="HeroSlide").ToArray())Children.Remove(element);}
        internal void Refresh()
        {
            var f=owner.RemScale;var isEmpty=owner.items.Count==0;emptyBackground.Visibility=empty.Visibility=isEmpty?Visibility.Visible:Visibility.Collapsed;
            empty.Text.FontFamily=owner.HasCallerFontFamily?owner.FontFamily:defaultEmptyFamily;
            emptyBackground.DefaultBackground=owner.DefaultBackground;emptyBackground.SetPalette(owner.Brush("DeepSea"),owner.Brush("Dusk"),owner.Brush("AccentOxblood"));
            var emptyValue=owner.EmptyState;if(emptyValue is null&&owner.EmptyStateTemplate is null&&owner.EmptyStateTemplateSelector is null)emptyValue="No featured content available";empty.Set(emptyValue,owner.EmptyStateTemplate,false,owner.EmptyStateTemplateSelector);empty.Text.FontSize=20*f;empty.Text.LineHeight=28*f;empty.Text.Foreground=owner.Brush("EmptyForeground");empty.FlowDirection=owner.FlowDirection;empty.Language=owner.Language;empty.Text.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;
            var show=owner.items.Count>1&&owner.ShowControls;
            if(!show)RetireNavigation();
            else
            {
                if(navigation is null){dotPanel=new(this);navigation=new Border{Name="HeroIndicators",Child=dotPanel,Padding=new Thickness(4*f),BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(7),UseLayoutRounding=false};navigationBlur.Host.SetBinding(Border.CornerRadiusProperty,new Binding{Source=navigation,Path=new PropertyPath("CornerRadius")});previous=Arrow(false);next=Arrow(true);Children.Add(navigation);Children.Add(previous);Children.Add(next);foreach(var e in new UIElement[]{navigation,previous,next})Canvas.SetZIndex(e,1000000);}
                foreach(var arrow in new[]{previous!,next!}){arrow.Style=owner.ArrowStyle;arrow.Width=arrow.Height=arrow.MinWidth=arrow.MinHeight=40*f;if(arrow.Content is Viewbox icon)icon.Width=icon.Height=20;RefreshArrowBackdrop(arrow);}
                navigation.Padding=new Thickness(4*f);navigation.Visibility=owner.ShowIndicators?Visibility.Visible:Visibility.Collapsed;navigationBlur.Host.Visibility=navigation.Visibility;
                var keys=owner.ShowIndicators?owner.items.Select(Key).ToHashSet(StringComparer.Ordinal):[];
                foreach(var key in dots.Keys.Where(k=>!keys.Contains(k)).ToArray()){dots[key].Dispose();dots.Remove(key);}
                if(owner.ShowIndicators)for(var index=0;index<owner.items.Count;index++){var key=Key(owner.items[index]);if(!dots.TryGetValue(key,out var dot)){dot=new(this);dots.Add(key,dot);dotPanel!.Children.Add(dot.Button);}dot.Update(owner.items[index],index);}
            }
            Paint();UpdateControls();InvalidateMeasure();
        }
        private BleeckerIconButton Arrow(bool forward)
        {
            var arrow=new BleeckerIconButton{Name=forward?"HeroNext":"HeroPrevious",UseLayoutRounding=false};AutomationProperties.SetName(arrow,forward?"Next slide":"Previous slide");
            arrow.Content=XamlReader.Load($"<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='20' Height='20' AutomationProperties.AccessibilityView='Raw'><Canvas Width='24' Height='24'><Path Data='{(forward?"M9,6 L15,12 L9,18":"M15,6 L9,12 L15,18")}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
            arrow.Click+=forward?NextClicked:PreviousClicked;
            arrow.Loaded+=(_,_)=>{if(!arrow.IsLoaded)return;arrow.ApplyTemplate();var presenter=Descendants(arrow).OfType<ContentPresenter>().FirstOrDefault();var path=Descendants(arrow).OfType<Microsoft.UI.Xaml.Shapes.Path>().FirstOrDefault();if(presenter is not null&&path is not null)path.SetBinding(Shape.StrokeProperty,new Binding{Source=presenter,Path=new PropertyPath("Foreground")});RefreshArrowBackdrop(arrow);};
            return arrow;
        }
        private void RefreshArrowBackdrop(BleeckerIconButton arrow)
        {
            if(!arrow.IsLoaded||!(ReferenceEquals(arrow,previous)||ReferenceEquals(arrow,next)))return;arrow.ApplyTemplate();var host=Descendants(arrow).OfType<Border>().SingleOrDefault(b=>b.Name=="HeroArrowBackdrop");
            if(arrowBlurs.TryGetValue(arrow,out var old)){if(ReferenceEquals(old.Host,host)){old.Update();return;}old.Dispose();arrowBlurs.Remove(arrow);}
            if(host is not null){var blur=new HeroBackdrop("HeroArrowBackdrop",host);arrowBlurs.Add(arrow,blur);blur.Update();}
        }
        private void PreviousClicked(object sender,RoutedEventArgs args){if(ReferenceEquals(sender,previous)&&previous?.IsLoaded==true)owner.MovePrevious();}
        private void NextClicked(object sender,RoutedEventArgs args){if(ReferenceEquals(sender,next)&&next?.IsLoaded==true)owner.MoveNext();}
        private void RetireNavigation()
        {
            foreach(var dot in dots.Values)dot.Dispose();dots.Clear();
            if(previous is not null){previous.IsEnabled=false;previous.Click-=PreviousClicked;Children.Remove(previous);}if(next is not null){next.IsEnabled=false;next.Click-=NextClicked;Children.Remove(next);}if(navigation is not null)Children.Remove(navigation);
            previous=next=null;navigation=null;dotPanel=null;navigationBlur.Clear();navigationBlur.Host.ClearValue(Border.CornerRadiusProperty);navigationBlur.Host.Visibility=Visibility.Collapsed;foreach(var blur in arrowBlurs.Values)blur.Dispose();arrowBlurs.Clear();
        }
        internal void UpdateControls()
        {
            if(previous is null||next is null)return;previous.IsEnabled=next.IsEnabled=owner.InputAllowed&&owner.items.Count>1;
            var viewport=owner.XamlRoot?.Size.Width??ActualWidth;arrows.Retarget(viewport<768||owner.hovered||owner.keyboardFocused?1:0,owner.Now,200,owner.MotionEnabled);
            foreach(var dot in dots.Values)dot.Paint();Present(owner.Now);owner.QueueFrames();
        }
        internal void Paint()
        {
            emptyBackground.SetPalette(owner.Brush("DeepSea"),owner.Brush("Dusk"),owner.Brush("AccentOxblood"));empty.SetForeground(owner.Brush("EmptyForeground"));
            if(navigation is not null){navigation.Background=owner.Brush("NavigationBackground");navigation.BorderBrush=owner.Brush("NavigationBorder");navigationBlur.Update();}
            foreach(var dot in dots.Values)dot.Paint();
        }
        internal bool IsAnimating(double now)=>arrows.IsRunning(now)||dots.Values.Any(d=>d.IsAnimating(now));
        internal void Present(double now){if(previous is not null)previous.Opacity=arrows.ValueAt(now);if(next is not null)next.Opacity=arrows.ValueAt(now);foreach(var dot in dots.Values)dot.Present(now);}
        protected override Size MeasureOverride(Size available)
        {
            var f=owner.RemScale;var width=double.IsFinite(available.Width)?available.Width:1000*f;var height=600*f;
            foreach(var slot in owner.slots.Values){slot.Root.Measure(new(width,height));slot.MeasureHitTargets();}emptyBackground.Measure(new(width,height));empty.Measure(new(Math.Max(0,width),double.PositiveInfinity));emptyBox=new((width-empty.DesiredSize.Width)/2,32*f+(height-empty.DesiredSize.Height)/2,empty.DesiredSize.Width,empty.DesiredSize.Height);
            if(previous is not null&&next is not null&&navigation is not null)
            {
                navigation.Measure(new(double.PositiveInfinity,double.PositiveInfinity));var navHeight=owner.ShowIndicators?navigation.DesiredSize.Height:40*f;var y=height-32*f-navHeight+(navHeight-40*f)/2;
                previousBox=new(owner.FlowDirection==FlowDirection.RightToLeft?width-56*f:16*f,y,40*f,40*f);nextBox=new(owner.FlowDirection==FlowDirection.RightToLeft?16*f:width-56*f,y,40*f,40*f);
                previous.Measure(new(40*f,40*f));next.Measure(new(40*f,40*f));navigationBox=new((width-navigation.DesiredSize.Width)/2,height-32*f-navigation.DesiredSize.Height,navigation.DesiredSize.Width,navigation.DesiredSize.Height);
            }
            return new(width,height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var height=600*owner.RemScale;Clip=new RectangleGeometry{Rect=new(0,0,final.Width,Math.Min(final.Height,height))};foreach(var slot in owner.slots.Values){slot.Root.Arrange(new(0,0,final.Width,height));slot.ArrangeHitTargets();}emptyBackground.Arrange(new(0,0,final.Width,height));empty.Arrange(emptyBox);
            if(previous is not null&&next is not null&&navigation is not null){previous.Arrange(previousBox);next.Arrange(nextBox);navigation.Arrange(navigationBox);navigationBlur.Host.Arrange(navigationBox);navigationBlur.Update();foreach(var blur in arrowBlurs.Values)blur.Update();}
            return final;
        }
        public void Dispose(){RetireNavigation();navigationBlur.Dispose();emptyBackground.Dispose();empty.Set(null,null);Children.Clear();}
        private static double EaseInOut(double value){if(value<=0)return 0;if(value>=1)return 1;var low=0d;var high=1d;for(var i=0;i<24;i++){var t=(low+high)/2;var x=3*(1-t)*(1-t)*t*.42+3*(1-t)*t*t*.58+t*t*t;if(x<value)low=t;else high=t;}var p=(low+high)/2;return 3*(1-p)*p*p+p*p*p;}
        private sealed class DotSlot : IDisposable
        {
            private readonly HeroPanel panel;
            private readonly Ellipse indicator=new(){Name="HeroDot",IsHitTestVisible=false,UseLayoutRounding=false};
            private readonly ScaleTransform scale=new();
            private readonly SolidColorBrush fill=new();
            private readonly BleeckerCssTransition<BleeckerCssColor> color=new(BleeckerCssColor.Mix);
            private readonly BleeckerCssTransition<double> size=new((a,b,p)=>a+(b-a)*p);
            private VisualStateGroup? group;
            private bool disposed,hovered;
            private int index;
            internal Button Button{get;}=new(){Name="HeroIndicator",UseLayoutRounding=false};
            internal DotSlot(HeroPanel panel){this.panel=panel;indicator.Fill=fill;indicator.RenderTransform=scale;indicator.RenderTransformOrigin=new(.5,.5);Button.Content=indicator;Button.Click+=Clicked;Button.Loaded+=Loaded;}
            private void Loaded(object sender,RoutedEventArgs args){Button.ApplyTemplate();if(group is not null)group.CurrentStateChanged-=StateChanged;group=VisualTreeHelper.GetChildrenCount(Button)>0&&VisualTreeHelper.GetChild(Button,0)is FrameworkElement root?VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(g=>g.Name=="CommonStates"):null;if(group is not null)group.CurrentStateChanged+=StateChanged;hovered=group?.CurrentState?.Name is "PointerOver" or "Pressed";Paint();}
            private void StateChanged(object sender,VisualStateChangedEventArgs args){hovered=group?.CurrentState?.Name is "PointerOver" or "Pressed";Paint();panel.owner.QueueFrames();}
            private void Clicked(object sender,RoutedEventArgs args){if(!disposed&&Button.IsLoaded&&Button.IsEnabled&&panel.owner.InputAllowed)panel.owner.SelectSlide(index);}
            internal void Update(BleeckerHeroCarouselItem item,int value){index=value;Button.Tag=item;Button.Style=panel.owner.IndicatorStyle;Button.Width=Button.Height=40*panel.owner.RemScale;indicator.Width=indicator.Height=8*panel.owner.RemScale;Button.TabIndex=value+1;AutomationProperties.SetName(Button,"Go to slide "+(value+1));AutomationProperties.SetPositionInSet(Button,value+1);AutomationProperties.SetSizeOfSet(Button,panel.owner.items.Count);Paint();}
            internal void Paint(){if(disposed)return;var owner=panel.owner;var active=owner.state?.Index==index;Button.IsEnabled=owner.InputAllowed;AutomationProperties.SetItemStatus(Button,active?"Current slide":"");var brush=owner.Brush(active?"IndicatorActive":hovered?"IndicatorHover":"Indicator");var c=brush.Color;color.Retarget(BleeckerCssColor.FromRgba((c.R,c.G,c.B,c.A/255d*brush.Opacity),false),owner.Now,190,owner.MotionEnabled);size.Retarget(hovered?1.5:1,owner.Now,190,owner.MotionEnabled);Present(owner.Now);}
            internal bool IsAnimating(double now)=>color.IsRunning(now)||size.IsRunning(now);
            internal void Present(double now){var c=color.ValueAt(now).Encode();fill.Color=Windows.UI.Color.FromArgb(255,c.R,c.G,c.B);fill.Opacity=c.Alpha;scale.ScaleX=scale.ScaleY=size.ValueAt(now);}
            public void Dispose(){if(disposed)return;disposed=true;Button.IsEnabled=false;Button.Click-=Clicked;Button.Loaded-=Loaded;if(group is not null)group.CurrentStateChanged-=StateChanged;group=null;if(Button.Parent is Panel parent)parent.Children.Remove(Button);Button.Content=null;}
        }
        private sealed class DotPanel(HeroPanel panel):Panel
        {
            protected override Size MeasureOverride(Size available){var f=panel.owner.RemScale;foreach(var dot in panel.dots.Values)dot.Button.Measure(new(40*f,40*f));return new(panel.dots.Count*40*f+Math.Max(0,panel.dots.Count-1)*4*f,40*f);}
            protected override Size ArrangeOverride(Size final){var f=panel.owner.RemScale;for(var i=0;i<panel.owner.items.Count;i++)if(panel.dots.TryGetValue(Key(panel.owner.items[i]),out var dot)){var x=i*44*f;if(panel.owner.FlowDirection==FlowDirection.RightToLeft)x=final.Width-x-40*f;dot.Button.Arrange(new(x,0,40*f,40*f));}return final;}
        }
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);yield return child;foreach(var descendant in Descendants(child))yield return descendant;}}
}
