using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

public readonly record struct BleeckerFeedItemNavigationEventArgs(string SourceUrl);

/// <summary>A feed row presenting original caller metadata and requesting caller-owned source navigation.</summary>
public sealed partial class BleeckerFeedItem : Control
{
    private Grid? root;
    private Border? surface;
    private BleeckerAvatar? avatar;
    private BleeckerContentText? authorText,timeText,relevanceText,contentText,linkText;
    private FeedLink? link;
    private BleeckerTooltip? timestampTip;
    private TagWrap? tags;
    private TextBlock? hoverSource,linkSource,linkHoverSource;
    private readonly SolidColorBrush backgroundPaint=new(),linkPaint=new();
    private readonly BleeckerCssTransition<BleeckerCssColor> backgroundMotion=new(BleeckerCssColor.Mix),linkMotion=new(BleeckerCssColor.Mix);
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly UISettings settings=new();
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> observers=[];
    private INotifyCollectionChanged? observedCategories;
    private ICommand? observedCommand;
    private XamlRoot? observedHost;
    private string[] renderedCategories=[];
    private bool over,linkOver,rendering;
    internal TimeProvider TimeSource { get; set; }=TimeProvider.System;

    public BleeckerFeedItem()
    {
        DefaultStyleKey=typeof(BleeckerFeedItem);IsTabStop=false;UseLayoutRounding=false;Categories=new ObservableCollection<string>();
        Loaded+=(_,_)=>{Subscribe();Render();Paint(false);};Unloaded+=(_,_)=>{Unsubscribe();Stop();};
        ActualThemeChanged+=(_,_)=>DispatcherQueue.TryEnqueue(()=>{ObserveBrushes();Paint(false);});
        PointerEntered+=(_,_)=>{over=true;Paint(true);};PointerExited+=(_,_)=>{over=false;Paint(true);};
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)Detach();});
        foreach(var property in new[]{IsEnabledProperty,FlowDirectionProperty,IsTextScaleFactorEnabledProperty,LanguageProperty,ForegroundProperty})RegisterPropertyChangedCallback(property,(_,_)=>Render());
        RegisterPropertyChangedCallback(BackgroundProperty,(_,_)=>{ObserveBrushes();Paint(true);});
        RegisterPropertyChangedCallback(VisibilityProperty,(_,_)=>{if(Visibility!=Visibility.Visible){over=linkOver=false;Paint(false);}});
        RegisterPropertyChangedCallback(StyleProperty,(_,_)=>FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer());
    }
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerFeedItem),new PropertyMetadata(value,(owner,_)=>((BleeckerFeedItem)owner).Render()));
    public static readonly DependencyProperty AuthorProperty=Property(nameof(Author),typeof(string),"");
    public static readonly DependencyProperty ContentProperty=Property(nameof(Content),typeof(string),"");
    public static readonly DependencyProperty InitProperty=Property(nameof(Init),typeof(string),"");
    public static readonly DependencyProperty LanguageCodeProperty=Property(nameof(LanguageCode),typeof(string),"");
    public static readonly DependencyProperty PostedAtProperty=Property(nameof(PostedAt),typeof(object),"");
    public static readonly DependencyProperty RelevanceProperty=Property(nameof(Relevance),typeof(double),0d);
    public static readonly DependencyProperty SourceUrlProperty=Property(nameof(SourceUrl),typeof(string),"");
    public static readonly DependencyProperty AvatarSourceProperty=Property(nameof(AvatarSource),typeof(ImageSource),null);
    public static readonly DependencyProperty CategoriesProperty=DependencyProperty.Register(nameof(Categories),typeof(object),typeof(BleeckerFeedItem),new PropertyMetadata(null,(owner,args)=>{var item=(BleeckerFeedItem)owner;item.DisconnectCategories();if(args.NewValue is null&&!item.IsLoaded&&item.GetBindingExpression(CategoriesProperty)is not null)return;if(args.NewValue is not IEnumerable<string>)throw new ArgumentException("FeedItem Categories requires a non-null sequence of strings.");if(item.IsLoaded)item.ConnectCategories();item.Render();}));
    public static readonly DependencyProperty SourceCommandProperty=DependencyProperty.Register(nameof(SourceCommand),typeof(ICommand),typeof(BleeckerFeedItem),new PropertyMetadata(null,(owner,_)=>{var item=(BleeckerFeedItem)owner;item.ConnectCommand();item.Render();}));
    public static readonly DependencyProperty SourceCommandParameterProperty=Property(nameof(SourceCommandParameter),typeof(object),null);
    public string Author {get=>(string)GetValue(AuthorProperty);set=>SetValue(AuthorProperty,value);}
    public string Content {get=>(string)GetValue(ContentProperty);set=>SetValue(ContentProperty,value);}
    public string Init {get=>(string)GetValue(InitProperty);set=>SetValue(InitProperty,value);}
    public string LanguageCode {get=>(string)GetValue(LanguageCodeProperty);set=>SetValue(LanguageCodeProperty,value);}
    public object PostedAt {get=>GetValue(PostedAtProperty);set=>SetValue(PostedAtProperty,value);}
    public double Relevance {get=>(double)GetValue(RelevanceProperty);set=>SetValue(RelevanceProperty,value);}
    public string SourceUrl {get=>(string)GetValue(SourceUrlProperty);set=>SetValue(SourceUrlProperty,value);}
    public ImageSource? AvatarSource {get=>(ImageSource?)GetValue(AvatarSourceProperty);set=>SetValue(AvatarSourceProperty,value);}
    public IEnumerable<string> Categories {get=>GetValue(CategoriesProperty) as IEnumerable<string>??throw new InvalidOperationException("FeedItem Categories binding must resolve before mounting.");set=>SetValue(CategoriesProperty,value??throw new ArgumentNullException(nameof(value)));}
    public ICommand? SourceCommand {get=>(ICommand?)GetValue(SourceCommandProperty);set=>SetValue(SourceCommandProperty,value);}
    public object? SourceCommandParameter {get=>GetValue(SourceCommandParameterProperty);set=>SetValue(SourceCommandParameterProperty,value);}
    public event EventHandler<BleeckerFeedItemNavigationEventArgs>? SourceRequested;

    protected override void OnApplyTemplate()
    {
        Detach();if(Template is null)return;base.OnApplyTemplate();
        T Part<T>(string name)where T:class=>GetTemplateChild(name)as T??throw new InvalidOperationException($"FeedItem requires {name}.");
        root=Part<Grid>("PART_Root");surface=Part<Border>("PART_Surface");hoverSource=Part<TextBlock>("PART_HoverSource");linkSource=Part<TextBlock>("PART_LinkSource");linkHoverSource=Part<TextBlock>("PART_LinkHoverSource");
        authorText=new("FeedItemAuthor","BleeckerPrimarySemiboldFontFamily",13,19.5,"BleeckerTextPrimaryBrush"){NoWrap=true};authorText.Text.FontWeight=Microsoft.UI.Text.FontWeights.SemiBold;authorText.Text.TextTrimming=TextTrimming.CharacterEllipsis;
        timeText=new("FeedItemTime","BleeckerSecondaryFontFamily",11,16.5,"BleeckerTextSecondaryBrush"){NoWrap=true};
        relevanceText=new("FeedItemRelevanceText","BleeckerPrimarySemiboldFontFamily",11,16.5,"BleeckerSeaBrush");relevanceText.Text.FontWeight=Microsoft.UI.Text.FontWeights.SemiBold;Typography.SetNumeralAlignment(relevanceText.Text,FontNumeralAlignment.Tabular);
        var relevance=Create<Border>("<Border x:Name='FeedItemRelevance' CornerRadius='5' Padding='8,2' BorderThickness='1' BackgroundSizing='OuterBorderEdge' Background='{ThemeResource FeedItemRelevanceBackground}' BorderBrush='{ThemeResource FeedItemRelevanceBorder}'/>");relevance.Child=relevanceText;
        contentText=new("FeedItemContent","BleeckerPrimaryFontFamily",13,20,"BleeckerTextPrimaryBrush"){BreakWords=true};contentText.Text.TextAlignment=TextAlignment.Left;
        var header=new HeaderRow(authorText,timeText,relevance){Name="FeedItemHeader",UseLayoutRounding=false};
        timestampTip=new BleeckerTooltip();BleeckerTooltipService.SetToolTip(timeText,timestampTip);
        tags=new TagWrap{Name="FeedItemTags",UseLayoutRounding=false};
        linkText=new("FeedItemSourceText","BleeckerPrimaryMediumFontFamily",12,16,"BleeckerSeaBrush",medium:true);
        var icon=Create<Viewbox>("<Viewbox x:Name='FeedItemSourceIcon' Width='12' Height='12' FlowDirection='LeftToRight' IsHitTestVisible='False'><Canvas Width='24' Height='24'><Path Stroke='{ThemeResource BleeckerSeaBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round' Data='M15 3H21V9 M10 14L21 3 M18 13V19A2 2 0 0 1 16 21H5A2 2 0 0 1 3 19V8A2 2 0 0 1 5 6H11'/></Canvas></Viewbox>");
        ((Microsoft.UI.Xaml.Shapes.Path)((Canvas)icon.Child).Children[0]).Stroke=linkPaint;
        var inline=new LinkInline(linkText,icon){UseLayoutRounding=false};var styles=new ResourceDictionary{Source=new Uri("ms-appx:///Orchard/Themes/FeedItemLink.xaml")};link=new FeedLink(this){Name="FeedItemSourceLink",Style=(Style)styles[typeof(HyperlinkButton)],Content=inline,UseLayoutRounding=false};
        link.Click+=(_,_)=>RequestSource();link.PointerEntered+=(_,_)=>{linkOver=true;linkText.Text.TextDecorations=Windows.UI.Text.TextDecorations.Underline;Paint(true);};link.PointerExited+=(_,_)=>{linkOver=false;linkText.Text.TextDecorations=Windows.UI.Text.TextDecorations.None;Paint(true);};
        var body=new BodyStack(header,contentText,tags,link,linkText){Name="FeedItemBody",UseLayoutRounding=false};
        avatar=Create<BleeckerAvatar>("<local:BleeckerAvatar xmlns:local='using:Orchard' x:Name='FeedItemAvatar' AvatarSize='Small' BorderBrush='{ThemeResource FeedItemAvatarRing}'/>");
        root.Children.Add(new FeedRow(avatar,body){UseLayoutRounding=false});if(IsLoaded){ObserveBrushes();}Render();Paint(false);
    }
    private static T Create<T>(string markup)=>(T)Microsoft.UI.Xaml.Markup.XamlReader.Load(markup.Insert(markup.IndexOf(' ')," xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' UseLayoutRounding='False'"));
    private void Render()
    {
        if(root is null)return;
        var timestamp=BleeckerFeedItemModel.FormatTimestamp(PostedAt,TimeSource.GetUtcNow(),CultureInfo.CurrentCulture,TimeSource.LocalTimeZone);
        authorText!.Set(Author,null);timeText!.Set("· "+timestamp.Label,null);timestampTip!.Content=timestamp.Iso;relevanceText!.Set(BleeckerFeedItemModel.Relevance(Relevance),null);contentText!.Set(Content,null);linkText!.Set(BleeckerFeedItemModel.SourceLabel(SourceUrl),null);
        authorText.Text.Foreground=contentText.Text.Foreground=Foreground;linkText.Text.Foreground=linkPaint;
        avatar!.Fallback=BleeckerFeedItemModel.Fallback(Author,Init);avatar.Source=AvatarSource;
        var normalized=Categories.Select(value=>BleeckerFeedItemModel.Category(value??throw new ArgumentException("FeedItem category strings must not be null."))).Where(value=>value.Length>0).Prepend(BleeckerTextCasing.Uppercase(string.IsNullOrEmpty(LanguageCode)?"n/a":LanguageCode)).ToArray();
        if(!normalized.SequenceEqual(renderedCategories)||tags!.Children.Count==0)
        {
            renderedCategories=normalized;tags!.Children.Clear();for(var i=0;i<normalized.Length;i++){var label=new BleeckerContentText("FeedItemTagText"+i,"BleeckerPrimaryMediumFontFamily",11,16.5,"BleeckerTextSecondaryBrush",medium:true);label.Set(normalized[i],null);var tag=i==0?Create<Border>("<Border CornerRadius='5' Padding='8,2' Background='{ThemeResource FeedItemLanguageBackground}'/>"):Create<Border>("<Border CornerRadius='5' Padding='8,2' BorderThickness='1' BackgroundSizing='OuterBorderEdge' Background='{ThemeResource FeedItemCategoryBackground}' BorderBrush='{ThemeResource FeedItemCategoryBorder}'/>");tag.Name="FeedItemTag"+i;tag.Child=label;tags.Children.Add(tag);}
        }
        foreach(var text in new[]{authorText.Text,timeText.Text,relevanceText.Text,contentText.Text,linkText.Text}.Concat(tags!.Children.OfType<Border>().Select(tag=>((BleeckerContentText)tag.Child).Text))){text.Language=Language;text.IsTextScaleFactorEnabled=IsTextScaleFactorEnabled;}
        link!.IsEnabled=IsEnabled&&(SourceCommand?.CanExecute(SourceCommandParameter)??true);AutomationProperties.SetName(link,BleeckerFeedItemModel.SourceLabel(SourceUrl));AutomationProperties.SetHelpText(link,SourceUrl);
        root.FlowDirection=FlowDirection;InvalidateMeasure();FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer();
    }
    private void RequestSource()
    {
        if(!IsLoaded||link?.IsLoaded!=true)throw new ElementNotAvailableException();if(!IsEnabled||link.IsEnabled==false)throw new ElementNotEnabledException();if(SourceCommand is not null&&!SourceCommand.CanExecute(SourceCommandParameter))return;
        SourceCommand?.Execute(SourceCommandParameter);SourceRequested?.Invoke(this,new(SourceUrl));
    }
    private void ConnectCategories(){DisconnectCategories();observedCategories=Categories as INotifyCollectionChanged;if(observedCategories is not null)observedCategories.CollectionChanged+=CategoriesChanged;}
    private void DisconnectCategories(){if(observedCategories is not null)observedCategories.CollectionChanged-=CategoriesChanged;observedCategories=null;}
    private void CategoriesChanged(object? sender,NotifyCollectionChangedEventArgs args)=>Render();
    private void ConnectCommand(){if(observedCommand is not null)observedCommand.CanExecuteChanged-=CommandChanged;observedCommand=IsLoaded?SourceCommand:null;if(observedCommand is not null)observedCommand.CanExecuteChanged+=CommandChanged;}
    private void CommandChanged(object? sender,EventArgs args)=>Render();
    private void Subscribe(){ConnectCategories();ConnectCommand();ObserveBrushes();if(observedHost is not null)observedHost.Changed-=HostChanged;observedHost=XamlRoot;if(observedHost is not null)observedHost.Changed+=HostChanged;}
    private void Unsubscribe(){DisconnectCategories();if(observedCommand is not null)observedCommand.CanExecuteChanged-=CommandChanged;observedCommand=null;DisconnectBrushes();if(observedHost is not null)observedHost.Changed-=HostChanged;observedHost=null;over=linkOver=false;}
    private void HostChanged(XamlRoot sender,XamlRootChangedEventArgs args){if(!sender.IsHostVisible){over=linkOver=false;Paint(false);}}
    private void DisconnectBrushes(){foreach(var(owner,property,token)in observers)owner.UnregisterPropertyChangedCallback(property,token);observers.Clear();}
    private void ObserveBrushes()
    {
        DisconnectBrushes();if(!IsLoaded)return;foreach(var source in new[]{hoverSource,linkSource,linkHoverSource}.OfType<TextBlock>())observers.Add((source,TextBlock.ForegroundProperty,source.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>{ObserveBrushes();Paint(false);})));foreach(var brush in new[]{Background,hoverSource?.Foreground,linkSource?.Foreground,linkHoverSource?.Foreground}.OfType<SolidColorBrush>().Distinct())foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})observers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Paint(false))));
    }
    private static BleeckerCssColor Color(Brush? value,bool legacy){if(value is null)return BleeckerCssColor.FromRgba((0,0,0,0),legacy);if(value is not SolidColorBrush solid)throw new ArgumentException("FeedItem animated brushes require solid colors.");return BleeckerCssColor.FromRgba((solid.Color.R,solid.Color.G,solid.Color.B,solid.Color.A/255d*solid.Opacity),legacy);}
    private void Paint(bool animate)
    {
        if(surface is null||hoverSource is null||linkSource is null||linkHoverSource is null)return;var time=clock.Elapsed.TotalMilliseconds;var allowed=animate&&IsLoaded&&Visibility==Visibility.Visible&&XamlRoot?.IsHostVisible==true&&settings.AnimationsEnabled;
        backgroundMotion.Retarget(Color(over?hoverSource.Foreground:Background,false),time,220,allowed);linkMotion.Retarget(Color(linkOver?linkHoverSource.Foreground:linkSource.Foreground,true),time,190,allowed);Draw(time);
        if(backgroundMotion.IsRunning(time)||linkMotion.IsRunning(time)){if(!rendering){CompositionTarget.Rendering+=Frame;rendering=true;}}else Stop();
    }
    private void Draw(double time){static void Set(SolidColorBrush brush,BleeckerCssColor color){var rgba=color.Encode();brush.Color=Windows.UI.Color.FromArgb(255,rgba.R,rgba.G,rgba.B);brush.Opacity=rgba.Alpha;}Set(backgroundPaint,backgroundMotion.ValueAt(time));Set(linkPaint,linkMotion.ValueAt(time));if(surface is not null)surface.Background=backgroundPaint;}
    private void Frame(object? sender,object args){var time=clock.Elapsed.TotalMilliseconds;Draw(time);if(!backgroundMotion.IsRunning(time)&&!linkMotion.IsRunning(time))Stop();}
    private void Stop(){if(rendering){CompositionTarget.Rendering-=Frame;rendering=false;}}
    private void Detach(){DisconnectBrushes();Stop();if(timeText is not null)BleeckerTooltipService.SetToolTip(timeText,null);if(link is not null){link.Owner=null;link.Content=null;}root?.Children.Clear();root=null;surface=null;avatar=null;authorText=timeText=relevanceText=contentText=linkText=null;link=null;tags=null;timestampTip=null;hoverSource=linkSource=linkHoverSource=null;renderedCategories=[];}
    protected override AutomationPeer OnCreateAutomationPeer()=>new ItemPeer(this);
    private sealed class ItemPeer(BleeckerFeedItem owner):FrameworkElementAutomationPeer(owner){protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;protected override string GetClassNameCore()=>nameof(BleeckerFeedItem);}
    private sealed class FeedLink(BleeckerFeedItem owner):HyperlinkButton
    {
        internal BleeckerFeedItem? Owner=owner;
        protected override AutomationPeer OnCreateAutomationPeer()=>new LinkPeer(this);
        private sealed class LinkPeer(FeedLink owner):FrameworkElementAutomationPeer(owner),IInvokeProvider
        {
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Hyperlink;
            protected override string GetClassNameCore()=>nameof(HyperlinkButton);
            protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.Invoke?this:base.GetPatternCore(pattern);
            public void Invoke(){if(owner.Owner is not { } item||!owner.IsLoaded)throw new ElementNotAvailableException();if(!owner.IsEnabled)throw new ElementNotEnabledException();item.RequestSource();}
        }
    }
    private sealed partial class FeedRow(FrameworkElement avatar,FrameworkElement body):Panel
    {
        protected override Size MeasureOverride(Size available){if(Children.Count==0){Children.Add(avatar);Children.Add(body);}avatar.Measure(new Size(32,32));body.Measure(new Size(Math.Max(0,available.Width-46),double.PositiveInfinity));return new(double.IsFinite(available.Width)?available.Width:46+body.DesiredSize.Width,Math.Max(32,body.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){avatar.Arrange(new Rect(0,0,32,32));body.Arrange(new Rect(46,0,Math.Max(0,final.Width-46),body.DesiredSize.Height));return final;}
    }
    private sealed partial class HeaderRow(BleeckerContentText author,BleeckerContentText time,FrameworkElement relevance):Panel
    {
        private double authorWidth,identityWidth,identityHeight;
        protected override Size MeasureOverride(Size available){if(Children.Count==0){Children.Add(author);Children.Add(time);Children.Add(relevance);}author.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));time.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));relevance.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));var width=double.IsFinite(available.Width)?available.Width:author.NaturalWidth+16+time.NaturalWidth+relevance.DesiredSize.Width;identityWidth=Math.Min(author.NaturalWidth+8+time.NaturalWidth,Math.Max(0,width-relevance.DesiredSize.Width-8));authorWidth=Math.Max(0,identityWidth-8-time.NaturalWidth);author.Measure(new Size(authorWidth,double.PositiveInfinity));identityHeight=Math.Max(author.DesiredSize.Height,time.DesiredSize.Height);return new(width,Math.Max(identityHeight,relevance.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){author.Arrange(new Rect(0,(final.Height-author.DesiredSize.Height)/2,authorWidth,author.DesiredSize.Height));time.Arrange(new Rect(authorWidth+8,(final.Height-time.DesiredSize.Height)/2,time.NaturalWidth,time.DesiredSize.Height));relevance.Arrange(new Rect(FlowDirection==FlowDirection.RightToLeft?identityWidth+8:final.Width-relevance.DesiredSize.Width,(final.Height-relevance.DesiredSize.Height)/2,relevance.DesiredSize.Width,relevance.DesiredSize.Height));return final;}
    }
    private sealed partial class TagWrap:Panel
    {
        private readonly List<(UIElement Child,double X,double Y,double Width,double Height)> boxes=[];
        protected override Size MeasureOverride(Size available){boxes.Clear();var line=new List<(UIElement Child,double X,double Width,double Height)>();double x=0,y=0,height=0,maximum=0;void Flush(){maximum=Math.Max(maximum,line.Select(box=>box.X+box.Width).DefaultIfEmpty(0).Max());foreach(var box in line)boxes.Add((box.Child,box.X,y+(height-box.Height)/2,box.Width,box.Height));if(line.Count>0)y+=height+6;line.Clear();x=height=0;}foreach(var child in Children){child.Measure(new Size(available.Width,double.PositiveInfinity));var size=child.DesiredSize;if(line.Count>0&&x+size.Width>available.Width)Flush();line.Add((child,x,size.Width,size.Height));x+=size.Width+6;height=Math.Max(height,size.Height);}Flush();return new(double.IsFinite(available.Width)?available.Width:maximum,Math.Max(0,y-6));}
        protected override Size ArrangeOverride(Size final){foreach(var box in boxes)box.Child.Arrange(new Rect(box.X,box.Y,box.Width,box.Height));return final;}
    }
    private sealed partial class LinkInline(BleeckerContentText text,FrameworkElement icon):Panel
    {
        protected override Size MeasureOverride(Size available){if(Children.Count==0){Children.Add(text);Children.Add(icon);}text.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));icon.Measure(new Size(12,12));return new(text.DesiredSize.Width+16,Math.Max(text.DesiredSize.Height,12));}
        protected override Size ArrangeOverride(Size final){text.Arrange(new Rect(0,(final.Height-text.DesiredSize.Height)/2,text.DesiredSize.Width,text.DesiredSize.Height));icon.Arrange(new Rect(text.DesiredSize.Width+4,(final.Height-12)/2,12,12));return final;}
    }
    private sealed partial class BodyStack(FrameworkElement header,FrameworkElement content,FrameworkElement tags,FrameworkElement link,BleeckerContentText linkText):Panel
    {
        private double tagsY,linkY;
        protected override Size MeasureOverride(Size available){if(Children.Count==0){Children.Add(header);Children.Add(content);Children.Add(tags);Children.Add(link);}foreach(var child in Children)child.Measure(new Size(available.Width,double.PositiveInfinity));var width=double.IsFinite(available.Width)?available.Width:Children.Max(child=>child.DesiredSize.Width);if(!double.IsFinite(available.Width))foreach(var child in Children)child.Measure(new Size(width,double.PositiveInfinity));tagsY=header.DesiredSize.Height+(content.DesiredSize.Height>0?6+content.DesiredSize.Height+8:8);linkY=tagsY+tags.DesiredSize.Height+10;return new(width,linkY+24);}
        protected override Size ArrangeOverride(Size final){header.Arrange(new Rect(0,0,final.Width,header.DesiredSize.Height));content.Arrange(new Rect(0,header.DesiredSize.Height+6,final.Width,content.DesiredSize.Height));tags.Arrange(new Rect(0,tagsY,final.Width,tags.DesiredSize.Height));link.Arrange(new Rect(FlowDirection==FlowDirection.RightToLeft?final.Width-link.DesiredSize.Width:0,linkY+(linkText.DesiredSize.Height>0?6:12),link.DesiredSize.Width,link.DesiredSize.Height));return final;}
    }
}
