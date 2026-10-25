using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;

namespace Orchard;

public sealed partial class BleeckerHeroCarousel
{
    private sealed class SlideSlot : IDisposable
    {
        internal readonly BleeckerHeroCarousel owner;
        private readonly Grid frame = new() { UseLayoutRounding = false };
        private readonly BleeckerHeroImage image = new();
        private readonly HeroBody body;
        private readonly LinkPanel linkPanel;
        private readonly BleeckerCssTransition<double> opacity = new((a,b,p)=>a+(b-a)*p, BleeckerCssEasing.Default);
        private FrameworkElement? element;
        private DataTemplate? template;
        private HyperlinkButton? areaHit, contentHit;
        private bool disposed, selected, initialized, hovered;
        private VisualStateGroup? states;
        internal int Index { get; private set; }
        internal BleeckerHeroCarouselLinkContext Context { get; private set; }
        internal ContentControl Root { get; } = new() { Name = "HeroSlide", HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch, Padding = new(0), MinWidth = 0, MinHeight = 0, IsTabStop = false, UseLayoutRounding = false, FlowDirection = FlowDirection.LeftToRight };
        internal SlideSlot(BleeckerHeroCarousel owner)
        {
            this.owner = owner; Context = new(owner); body = new(this); linkPanel = new(this);
            image.Name = "HeroImageLayers"; image.ImageFailed += (_, _) => { if (!disposed && Context.Item is { } item) owner.ImageFailed?.Invoke(owner, item); };
            frame.Children.Add(image); frame.Children.Add(linkPanel); Root.Content = frame;
            Root.Loaded += (_, _) => { if (!disposed) { Context.Mounted = true; Context.Notify(); } };
        }
        internal void Update(BleeckerHeroCarouselItem item, int index)
        {
            Index = index; if (!ReferenceEquals(template, owner.LinkTemplate)) ResetLink(); template = owner.LinkTemplate;
            Context.Item = item; Context.Content = body; Context.Style = owner.LinkStyle;
            Context.AccessibleName = (item.Kind == BleeckerHeroCarouselItemKind.Post ? "Read article: " : "View project: ") + item.Title;
            Root.Tag = item; AutomationProperties.SetName(Root, Context.AccessibleName);
            image.Source = item.ImageSource; image.DefaultBackground = owner.DefaultBackground;
            body.Refresh();
            if (element is null)
            {
                element = template is null ? new HyperlinkButton { Name = "HeroDefaultLink", Content = body, Command = Context.Command, NavigateUri = null, UseLayoutRounding = false } : template.LoadContent() as FrameworkElement ?? throw new InvalidOperationException("Hero Carousel LinkTemplate must create a FrameworkElement.");
                Context.Element = element; element.DataContext = Context; element.PointerEntered += LinkEntered; element.PointerExited += LinkExited; element.Loaded += LinkLoaded;
                linkPanel.Children.Add(element);
            }
            element.FlowDirection = owner.FlowDirection; element.Language = owner.Language;
            if (template is null) { ((HyperlinkButton)element).Style = owner.LinkStyle; ((HyperlinkButton)element).TabIndex = index; AutomationProperties.SetName(element, Context.AccessibleName); AutomationProperties.SetHelpText(element, item.Description); }
            ScaleLinkPadding(); Context.Mounted = owner.IsLoaded; Paint(); Root.InvalidateMeasure(); linkPanel.InvalidateMeasure();
            EnsureSourceHitTargets();
        }
        private void EnsureSourceHitTargets()
        {
            if (template is not null || owner.panel is null) return;
            if (areaHit is not null && contentHit is not null)
            {
                foreach(var hit in new[]{areaHit,contentHit})if(!ReferenceEquals(hit.Parent,owner.panel)){if(hit.Parent is Panel previous)previous.Children.Remove(hit);owner.panel.Children.Add(hit);}
                return;
            }
            // CSS lets the opaque slide's z-10 content escape its wrapper, while
            // opacity-zero sibling links still own their empty-area hit boxes.
            // Independent transparent native hit targets preserve that ordering
            // without regrouping the rendered opacity/effect layers or caller content.
            areaHit = HitTarget("HeroSourceLinkHit"); contentHit = HitTarget("HeroSourceContentHit");
            owner.panel.Children.Add(areaHit); owner.panel.Children.Add(contentHit);
            HyperlinkButton HitTarget(string name)
            {
                var hit = new HyperlinkButton { Name = name, Command = Context.Command, Tag = Context.Item, Opacity = 0, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent), Padding = new(0), BorderThickness = new(0), MinWidth = 0, MinHeight = 0, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, IsTabStop = false, AllowFocusOnInteraction = false, UseLayoutRounding = false };
                AutomationProperties.SetAccessibilityView(hit, AccessibilityView.Raw);
                hit.PointerEntered += LinkEntered; hit.PointerExited += LinkExited;
                hit.PointerPressed += (_, _) => { if (owner.InputAllowed && element is Control link) link.Focus(FocusState.Pointer); };
                return hit;
            }
        }
        internal void MeasureHitTargets()
        { areaHit?.Measure(new(linkPanel.Box.Width, linkPanel.Box.Height)); contentHit?.Measure(body.DesiredSize); }
        internal void ArrangeHitTargets()
        {
            if (areaHit is null || contentHit is null || owner.panel is null) return;
            areaHit.Tag = contentHit.Tag = Context.Item;
            Canvas.SetZIndex(areaHit, 10000 + Index); Canvas.SetZIndex(contentHit, 20000 + Index);
            areaHit.Arrange(linkPanel.Box); contentHit.Arrange(body.InputBounds(owner.panel));
        }
        private void LinkEntered(object sender, PointerRoutedEventArgs args) { hovered = true; Paint(); owner.QueueFrames(); }
        private void LinkExited(object sender, PointerRoutedEventArgs args) { hovered = false; Paint(); owner.QueueFrames(); }
        private void LinkLoaded(object sender,RoutedEventArgs args)
        {
            if(element is Control control)control.ApplyTemplate();if(states is not null)states.CurrentStateChanged-=LinkStateChanged;
            states=element is not null&&VisualTreeHelper.GetChildrenCount(element)>0&&VisualTreeHelper.GetChild(element,0)is FrameworkElement root?VisualStateManager.GetVisualStateGroups(root).FirstOrDefault(g=>g.Name=="CommonStates"):null;
            if(states is not null)states.CurrentStateChanged+=LinkStateChanged;ScaleLinkPadding();Context.Notify();
        }
        private void ScaleLinkPadding(){if(element is Control control&&ReferenceEquals(control.Style,Context.Style)){control.ClearValue(Control.PaddingProperty);var p=control.Padding;var f=owner.RemScale;control.Padding=new(p.Left*f,p.Top*f,p.Right*f,p.Bottom*f);}}
        private void LinkStateChanged(object sender,VisualStateChangedEventArgs args){hovered=states?.CurrentState?.Name is "PointerOver" or "Pressed";Paint();owner.QueueFrames();}
        internal void Select(bool active, bool animate)
        {
            Context.IsActive = active;
            if (!initialized || selected != active) { opacity.Retarget(active ? 1 : 0, owner.Now, 500, initialized && animate && owner.MotionEnabled); initialized = true; selected = active; }
            Present(owner.Now); UpdateInput();
        }
        internal void UpdateInput() { Root.IsEnabled = Root.IsHitTestVisible = owner.InputAllowed; foreach(var hit in new[]{areaHit,contentHit})if(hit is not null)hit.IsEnabled=hit.IsHitTestVisible=owner.InputAllowed; Context.Notify(); }
        internal void Paint()
        {
            if (disposed || owner.panel is null) return;
            image.SetPalette(owner.Brush("DeepSea"), owner.Brush("Dusk"), owner.Brush("AccentOxblood")); body.Paint(hovered && Context.IsActive);
        }
        internal bool IsAnimating(double now) => opacity.IsRunning(now) || body.IsAnimating(now);
        internal void Present(double now) { Root.Opacity = opacity.ValueAt(now); if(contentHit is not null)contentHit.Visibility=Root.Opacity==1?Visibility.Visible:Visibility.Collapsed; body.Present(now); }
        private void ResetLink()
        {
            foreach(var hit in new[]{areaHit,contentHit})if(hit is not null){hit.Command=null;hit.Tag=null;hit.PointerEntered-=LinkEntered;hit.PointerExited-=LinkExited;if(hit.Parent is Panel parent)parent.Children.Remove(hit);}areaHit=contentHit=null;
            Context.Mounted = false; Context.Notify(); Context.SetValue(BleeckerHeroCarouselLinkContext.ContentProperty, null);
            if (element is HyperlinkButton button && template is null) { button.Command = null; button.Content = null; }
            if(states is not null)states.CurrentStateChanged-=LinkStateChanged;states=null;
            if (element is not null) { element.PointerEntered -= LinkEntered; element.PointerExited -= LinkExited; element.Loaded-=LinkLoaded; element.DataContext = null; linkPanel.Children.Remove(element); }
            element = null; Context.Element = null; Context = new(owner) { Content = body }; hovered = false;
        }
        public void Dispose()
        {
            if (disposed) return; disposed = true; ResetLink(); Context.Mounted = false; image.Dispose(); body.Dispose(); Root.Content = null; frame.Children.Clear();
            if (Root.Parent is Panel parent) parent.Children.Remove(Root);
        }
        private sealed class LinkPanel(SlideSlot slot) : Panel
        {
            private Rect box;
            internal Rect Box => box;
            protected override Size MeasureOverride(Size available)
            {
                var f = slot.owner.RemScale; var width = double.IsFinite(available.Width) ? available.Width : 1000 * f; var viewport = slot.owner.XamlRoot?.Size.Width ?? width;
                var inner = Math.Min(width, 1280*f); var padding = (viewport < 640 ? 16 : viewport < 1024 ? 24 : 32)*f;
                box = new((width-inner)/2+padding,0,Math.Max(0,inner-2*padding),600*f); slot.element?.Measure(new(box.Width,box.Height)); return new(width,600*f);
            }
            protected override Size ArrangeOverride(Size final) { slot.element?.Arrange(box); return final; }
        }
    }

    private sealed class HeroBody : Panel, IDisposable
    {
        private readonly SlideSlot slot;
        private readonly BleeckerContentText kicker = new("HeroKicker", "BleeckerPrimaryFontFamily", 14, 20, "BleeckerHeroForegroundBrush");
        private readonly BleeckerContentText title = new("HeroTitle", "BleeckerPrimarySemiboldFontFamily", 36, 40, "BleeckerHeroForegroundBrush");
        private readonly BleeckerContentText description = new("HeroDescription", "BleeckerPrimaryFontFamily", 18, 28, "BleeckerHeroDescriptionBrush");
        private readonly BleeckerContentText ctaText = new("HeroCtaText", "BleeckerPrimaryMediumFontFamily", 14, 20, "BleeckerHeroCtaForegroundBrush", true);
        private readonly Border rule = new() { Name = "HeroKickerRule", Opacity = .75, IsHitTestVisible = false, UseLayoutRounding = false };
        private readonly Border cta;
        private readonly HeroChrome ctaSurface;
        private readonly List<(HeroChrome Surface,Border Box,BleeckerContentText Text)> tags = [];
        private readonly Dictionary<UIElement, Rect> boxes = [];
        private readonly SolidColorBrush titleInk = new(), descriptionInk = new();
        private readonly BleeckerCssTransition<BleeckerCssColor> titleMotion = new(BleeckerCssColor.Mix), descriptionMotion = new(BleeckerCssColor.Mix);
        private readonly List<HeroTextShadow> shadows = [];
        private readonly Dictionary<BleeckerContentText, FontFamily> defaultFamilies = [];
        private readonly BleeckerContentText strut = new("HeroInlineStrut","BleeckerPrimaryFontFamily",16,24,"BleeckerHeroForegroundBrush");
        private double measuredWidth;
        private bool appliedHover;
        private bool callerTitleBrush;
        internal HeroBody(SlideSlot slot)
        {
            this.slot = slot; Name = "HeroPreparedContent"; HorizontalAlignment=HorizontalAlignment.Stretch; FlowDirection = FlowDirection.LeftToRight; UseLayoutRounding = false;
            foreach (var text in new[] { kicker, title, description, ctaText, strut }) defaultFamilies.Add(text, text.Text.FontFamily);
            kicker.Opacity = .75; kicker.Text.FontWeight = new FontWeight { Weight = 300 }; title.Text.FontWeight = new FontWeight { Weight = 600 }; title.BalanceWrap = true;
            title.BlockMaxContent=description.BlockMaxContent=true;
            foreach (var text in new[] { kicker, title, description, ctaText }) { text.Text.TextAlignment = TextAlignment.Center; AutomationProperties.SetAccessibilityView(text, AccessibilityView.Raw); }
            ctaSurface=new("HeroCta",ctaText);cta=ctaSurface.Chrome;
            foreach (var text in new[] { kicker, title, description }) { var shadow = new HeroTextShadow(this, text); shadows.Add(shadow); Children.Add(shadow.Host); }
            Children.Add(kicker); Children.Add(rule); Children.Add(title); Children.Add(description); Children.Add(ctaSurface);
        }
        internal void Refresh()
        {
            var owner = slot.owner; var item = slot.Context.Item; var f = owner.RemScale; var post = item.Kind == BleeckerHeroCarouselItemKind.Post;
            kicker.Set(BleeckerTextCasing.Uppercase(post ? "From the Blog" : "Featured Project"), null); title.Set(item.Title,null); description.Set(item.Description,null); ctaText.Set(string.IsNullOrEmpty(item.CtaLabel) ? post ? "Read Article" : "View Project" : item.CtaLabel,null);
            foreach (var text in new[] { kicker,title,description,ctaText }) { text.FlowDirection=owner.FlowDirection; text.Language=owner.Language; text.Text.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled; }
            if(appliedHover){cta.ClearValue(Border.BackgroundProperty);appliedHover=false;} cta.Style=owner.CtaStyle; ScalePadding(cta,f);
            var values=item.Kind==BleeckerHeroCarouselItemKind.Project?item.Tags.ToArray():[];
            while(tags.Count>values.Length){var last=tags[^1];Children.Remove(last.Surface);last.Surface.Dispose();defaultFamilies.Remove(last.Text);tags.RemoveAt(tags.Count-1);}
            while(tags.Count<values.Length){var text=new BleeckerContentText("HeroTagText","BleeckerPrimaryMediumFontFamily",12,18,"BleeckerHeroForegroundBrush",true);defaultFamilies.Add(text,text.Text.FontFamily);var surface=new HeroChrome("HeroTag",text);tags.Add((surface,surface.Chrome,text));Children.Add(surface);}
            for(var i=0;i<tags.Count;i++){var tag=tags[i];tag.Box.Style=owner.TagStyle;ScalePadding(tag.Box,f);tag.Text.Set(values[i],null);tag.Text.FlowDirection=owner.FlowDirection;tag.Text.Language=owner.Language;tag.Text.Text.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;}
            foreach (var (text, family) in defaultFamilies) text.Text.FontFamily = owner.HasCallerFontFamily ? owner.FontFamily : family;
            Paint(false);InvalidateMeasure();
        }
        private static void ScalePadding(Border box,double factor)
        {box.ClearValue(Border.PaddingProperty);var padding=box.Padding;box.Padding=new(padding.Left*factor,padding.Top*factor,padding.Right*factor,padding.Bottom*factor);}
        internal void Paint(bool hovered)
        {
            var owner=slot.owner;if(owner.panel is null)return;var normal=owner.Foreground ?? owner.Brush("Foreground");callerTitleBrush=!ReferenceEquals(normal,owner.Brush("Foreground"));
            if(!callerTitleBrush)titleMotion.Retarget(Color(owner.Brush(hovered?"TitleHover":"Foreground")),owner.Now,220,owner.MotionEnabled);descriptionMotion.Retarget(Color(owner.Brush(hovered?"DescriptionHover":"Description")),owner.Now,220,owner.MotionEnabled);
            title.Text.Foreground=callerTitleBrush?normal:titleInk;description.Text.Foreground=descriptionInk;kicker.Text.Foreground=normal;ctaText.Text.Foreground=owner.Brush("CtaForeground");rule.Background=owner.Brush("Separator");foreach(var tag in tags)tag.Text.Text.Foreground=normal;
            if(appliedHover){cta.ClearValue(Border.BackgroundProperty);appliedHover=false;}
            if(hovered&&ReferenceEquals(cta.Background,owner.Brush("CtaBackground"))){cta.Background=owner.Brush("CtaHover");appliedHover=true;}
            Present(owner.Now);
        }
        private static BleeckerCssColor Color(Brush brush)
        {if(brush is not SolidColorBrush solid)throw new InvalidOperationException("Hero Carousel text colors require solid brushes.");var c=solid.Color;return BleeckerCssColor.FromRgba((c.R,c.G,c.B,c.A/255d*solid.Opacity),false);}
        internal bool IsAnimating(double now)=>(!callerTitleBrush&&titleMotion.IsRunning(now))||descriptionMotion.IsRunning(now);
        internal void Present(double now)
        {if(!callerTitleBrush)Paint(titleInk,titleMotion.ValueAt(now));Paint(descriptionInk,descriptionMotion.ValueAt(now));static void Paint(SolidColorBrush brush,BleeckerCssColor value){var c=value.Encode();brush.Color=Windows.UI.Color.FromArgb(255,c.R,c.G,c.B);brush.Opacity=c.Alpha;}}
        protected override Size MeasureOverride(Size available)
        {
            var owner=slot.owner;var f=owner.RemScale;var width=double.IsFinite(available.Width)?available.Width:1000*f;var viewport=owner.XamlRoot?.Size.Width??width;var side=(viewport<640?8:24)*f;var extra=32*f+2*side;
            kicker.Text.FontSize=14*f;kicker.Text.LineHeight=20*f;kicker.Text.CharacterSpacing=100;
            title.Text.FontSize=(viewport<640?30:viewport<1024?36:48)*f;title.Text.LineHeight=(viewport<640?36:viewport<1024?40:48)*f;title.Text.CharacterSpacing=-20;
            description.Text.FontSize=(viewport<640?16:18)*f;description.Text.LineHeight=(viewport<640?24:28)*f;ctaText.Text.FontSize=14*f;ctaText.Text.LineHeight=20*f;
            foreach(var text in new[]{kicker,title,description,ctaText})text.Measure(new(double.PositiveInfinity,double.PositiveInfinity));foreach(var tag in tags)tag.Surface.Measure(new(double.PositiveInfinity,double.PositiveInfinity));ctaSurface.Measure(new(double.PositiveInfinity,double.PositiveInfinity));
            var tagNatural=tags.Sum(t=>t.Surface.DesiredSize.Width)+Math.Max(0,tags.Count-1)*8*f;var natural=new[]{kicker.NaturalWidth,title.NaturalWidth,description.NaturalWidth,ctaSurface.DesiredSize.Width,tagNatural}.Max();
            var blockWidth=Math.Min(width,Math.Min(768*f,natural+extra));var contentWidth=Math.Max(0,blockWidth-extra);measuredWidth=width=blockWidth;var x=extra/2;
            foreach(var text in new[]{kicker,title,description})text.Measure(new(contentWidth,double.PositiveInfinity));ctaSurface.Measure(new(Math.Max(0,contentWidth),double.PositiveInfinity));
            var tagRows=new List<(int Start,int Count,double Width,double Height)>();var rowStart=0;var rowWidth=0d;var rowHeight=0d;
            for(var i=0;i<tags.Count;i++){var surface=tags[i].Surface;surface.Measure(new(Math.Max(0,contentWidth),double.PositiveInfinity));var needed=surface.DesiredSize.Width+(i>rowStart?8*f:0);if(i>rowStart&&rowWidth+needed>contentWidth){tagRows.Add((rowStart,i-rowStart,rowWidth,rowHeight));rowStart=i;rowWidth=0;rowHeight=0;needed=surface.DesiredSize.Width;}rowWidth+=needed;rowHeight=Math.Max(rowHeight,surface.DesiredSize.Height);}if(tags.Count>0)tagRows.Add((rowStart,tags.Count-rowStart,rowWidth,rowHeight));
            strut.Set("Mg",null);strut.Text.FontSize=16*f;strut.Text.LineHeight=24*f;strut.Text.IsTextScaleFactorEnabled=owner.IsTextScaleFactorEnabled;strut.Measure(new(double.PositiveInfinity,double.PositiveInfinity));
            double TagBaseline(int index){var tag=tags[index];return tag.Box.Margin.Top+tag.Box.BorderThickness.Top+tag.Box.Padding.Top+tag.Text.FirstBaseline;}
            double RowBaseline((int Start,int Count,double Width,double Height) row)=>Math.Max(strut.FirstBaseline,Enumerable.Range(row.Start,row.Count).Max(TagBaseline));
            double RowHeight((int Start,int Count,double Width,double Height) row)=>RowBaseline(row)+Math.Max(strut.NaturalHeight-strut.FirstBaseline,Enumerable.Range(row.Start,row.Count).Max(i=>tags[i].Surface.DesiredSize.Height-TagBaseline(i)));
            var tagsHeight=tagRows.Sum(RowHeight);var collapsed=description.DesiredSize.Height==0?16*f:0;var height=kicker.DesiredSize.Height+40*f+1+(tags.Count>0?tagsHeight+16*f:0)+title.DesiredSize.Height+16*f+description.DesiredSize.Height+24*f-collapsed+ctaSurface.DesiredSize.Height;
            static double InlineCenter(double value)=>Math.Floor(value*64)/64;
            double FlexCenter(double value)=>(owner.FlowDirection==FlowDirection.RightToLeft?Math.Ceiling(value*64):Math.Floor(value*64))/64;
            var kickerWidth=Math.Min(contentWidth,kicker.NaturalWidth);var y=0d;boxes.Clear();Place(kicker,FlexCenter((width-kickerWidth)/2),y,kickerWidth,kicker.DesiredSize.Height);y+=kicker.DesiredSize.Height+16*f;Place(rule,FlexCenter((width-48*f)/2),y,48*f,1);y+=1+24*f;
            foreach(var row in tagRows){var cursor=InlineCenter((width-row.Width)/2);for(var i=row.Start;i<row.Start+row.Count;i++){var surface=tags[i].Surface;var tx=owner.FlowDirection==FlowDirection.RightToLeft?width-cursor-surface.DesiredSize.Width:cursor;Place(surface,tx,y+RowBaseline(row)-TagBaseline(i),surface.DesiredSize.Width,surface.DesiredSize.Height);cursor+=surface.DesiredSize.Width+8*f;}y+=RowHeight(row);}
            if(tags.Count>0)y+=16*f;Place(title,x,y,contentWidth,title.DesiredSize.Height);y+=title.DesiredSize.Height+16*f;Place(description,x,y,contentWidth,description.DesiredSize.Height);y+=description.DesiredSize.Height+24*f-collapsed;Place(ctaSurface,InlineCenter((width-ctaSurface.DesiredSize.Width)/2),y,ctaSurface.DesiredSize.Width,ctaSurface.DesiredSize.Height);
            return new(width,height);
            void Place(UIElement element,double left,double top,double w,double h)=>boxes[element]=new(left,top,Math.Max(0,w),Math.Max(0,h));
        }
        protected override Size ArrangeOverride(Size final)
        {var raw=(final.Width-measuredWidth)/2;var offset=(slot.owner.FlowDirection==FlowDirection.RightToLeft?Math.Floor(raw*64):Math.Floor(raw*64+.5))/64;foreach(var (element,box)in boxes)element.Arrange(new Rect(box.X+offset,box.Y,box.Width,box.Height));foreach(var shadow in shadows){shadow.Host.Measure(final);shadow.Host.Arrange(new(0,0,final.Width,final.Height));shadow.Update();}return final;}
        internal Rect InputBounds(UIElement relativeTo)
        {var raw=(ActualWidth-measuredWidth)/2;var offset=(slot.owner.FlowDirection==FlowDirection.RightToLeft?Math.Floor(raw*64):Math.Floor(raw*64+.5))/64;var inset=16*slot.owner.RemScale;return TransformToVisual(relativeTo).TransformBounds(new Rect(offset+inset,0,Math.Max(0,measuredWidth-2*inset),ActualHeight));}
        public void Dispose(){foreach(var shadow in shadows)shadow.Dispose();ctaSurface.Dispose();foreach(var tag in tags)tag.Surface.Dispose();}
    }
}
