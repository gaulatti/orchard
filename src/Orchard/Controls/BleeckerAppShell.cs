using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

/// <summary>A viewport-height page composition over the caller's original header, body and footer.</summary>
public sealed partial class BleeckerAppShell:ContentControl
{
    private Grid? root;
    private ShellPanel? panel;
    private BleeckerContentText? header,body,footer;
    private MainRegion? main;
    private readonly Dictionary<BleeckerContentText,(object? Value,DataTemplate? Template,DataTemplateSelector? Selector)> slots=[];
    private XamlRoot? observedRoot;
    private BleeckerViewportMinimum? minimum;
    public BleeckerAppShell()
    {
        DefaultStyleKey=typeof(BleeckerAppShell);IsTabStop=false;UseLayoutRounding=false;
        Loaded+=(_,_)=>{ObserveRoot();UpdateMinimum();Render();};
        Unloaded+=(_,_)=>{DisconnectRoot();};
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)Detach();});
        RegisterPropertyChangedCallback(MinHeightProperty,(_,_)=>{UpdateMinimum();panel?.InvalidateMeasure();});
        foreach(var property in new[]{PaddingProperty,BorderThicknessProperty})RegisterPropertyChangedCallback(property,(_,_)=>panel?.InvalidateMeasure());
        RegisterPropertyChangedCallback(StyleProperty,(_,_)=>{UpdateMinimum();Render();});
        foreach(var property in new[]{ContentTemplateProperty,ContentTemplateSelectorProperty,FontFamilyProperty,FontSizeProperty,FontWeightProperty,FontStyleProperty,FontStretchProperty,CharacterSpacingProperty,ForegroundProperty,LanguageProperty,IsTextScaleFactorEnabledProperty})RegisterPropertyChangedCallback(property,(_,_)=>Render());
    }
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerAppShell),new PropertyMetadata(value,(owner,_)=>((BleeckerAppShell)owner).Render()));
    public static readonly DependencyProperty HeaderProperty=Property(nameof(Header),typeof(object),null);
    public static readonly DependencyProperty HeaderTemplateProperty=Property(nameof(HeaderTemplate),typeof(DataTemplate),null);
    public static readonly DependencyProperty HeaderTemplateSelectorProperty=Property(nameof(HeaderTemplateSelector),typeof(DataTemplateSelector),null);
    public static readonly DependencyProperty FooterProperty=Property(nameof(Footer),typeof(object),null);
    public static readonly DependencyProperty FooterTemplateProperty=Property(nameof(FooterTemplate),typeof(DataTemplate),null);
    public static readonly DependencyProperty FooterTemplateSelectorProperty=Property(nameof(FooterTemplateSelector),typeof(DataTemplateSelector),null);
    public static readonly DependencyProperty OffsetHeaderProperty=Property(nameof(OffsetHeader),typeof(bool),true);
    public static readonly DependencyProperty HeaderHeightProperty=Property(nameof(HeaderHeight),typeof(double),72d);
    public static readonly DependencyProperty ContentPaddingProperty=Property(nameof(ContentPadding),typeof(Thickness),new Thickness(0));
    public static readonly DependencyProperty ContentBackgroundProperty=Property(nameof(ContentBackground),typeof(Brush),null);
    public object? Header{get=>GetValue(HeaderProperty);set=>SetValue(HeaderProperty,value);}
    public DataTemplate? HeaderTemplate{get=>(DataTemplate?)GetValue(HeaderTemplateProperty);set=>SetValue(HeaderTemplateProperty,value);}
    public DataTemplateSelector? HeaderTemplateSelector{get=>(DataTemplateSelector?)GetValue(HeaderTemplateSelectorProperty);set=>SetValue(HeaderTemplateSelectorProperty,value);}
    public object? Footer{get=>GetValue(FooterProperty);set=>SetValue(FooterProperty,value);}
    public DataTemplate? FooterTemplate{get=>(DataTemplate?)GetValue(FooterTemplateProperty);set=>SetValue(FooterTemplateProperty,value);}
    public DataTemplateSelector? FooterTemplateSelector{get=>(DataTemplateSelector?)GetValue(FooterTemplateSelectorProperty);set=>SetValue(FooterTemplateSelectorProperty,value);}
    public bool OffsetHeader{get=>(bool)GetValue(OffsetHeaderProperty);set=>SetValue(OffsetHeaderProperty,value);}
    /// <summary>The source --bleecker-header-height token, independent of the supplied header's actual height.</summary>
    public double HeaderHeight{get=>(double)GetValue(HeaderHeightProperty);set{if(!double.IsFinite(value)||value<0)throw new ArgumentOutOfRangeException(nameof(value));SetValue(HeaderHeightProperty,value);}}
    public Thickness ContentPadding{get=>(Thickness)GetValue(ContentPaddingProperty);set=>SetValue(ContentPaddingProperty,value);}
    public Brush? ContentBackground{get=>(Brush?)GetValue(ContentBackgroundProperty);set=>SetValue(ContentBackgroundProperty,value);}
    private static bool Styled(Control owner,DependencyProperty property){for(var style=owner.Style;style is not null;style=style.BasedOn)if(style.Setters.OfType<Setter>().Any(setter=>setter.Property==property))return true;return false;}
    private static bool Supplied(Control owner,DependencyProperty property)=>owner.ReadLocalValue(property)!=DependencyProperty.UnsetValue||Styled(owner,property);
    private Thickness Insets=>Supplied(this,ContentPaddingProperty)?ContentPadding:new Thickness(0,OffsetHeader&&(HeaderTemplate is not null||HeaderTemplateSelector is not null||BleeckerReactContent.Present(Header))?HeaderHeight:0,0,0);
    private void ObserveRoot(){if(ReferenceEquals(observedRoot,XamlRoot))return;DisconnectRoot();observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;}
    private void DisconnectRoot(){minimum?.Suspend();if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>UpdateMinimum();
    private void UpdateMinimum()=>minimum?.Update();
    protected override void OnContentChanged(object oldContent,object newContent){if(body is not null){body.Release();slots.Remove(body);}base.OnContentChanged(oldContent,newContent);Render();}
    protected override void OnApplyTemplate()
    {
        Detach();if(Template is null)return;base.OnApplyTemplate();root=GetTemplateChild("PART_Layout")as Grid??throw new InvalidOperationException("AppShell requires PART_Layout.");
        header=new("AppShellHeader","BleeckerPrimaryFontFamily",16,24,"BleeckerForegroundBrush",stretchContent:true);
        body=new("AppShellBody","BleeckerPrimaryFontFamily",16,24,"BleeckerForegroundBrush",stretchContent:true);
        footer=new("AppShellFooter","BleeckerPrimaryFontFamily",16,24,"BleeckerForegroundBrush",stretchContent:true);
        main=new(this,body){Name="AppShellMain",UseLayoutRounding=false};panel=new(this,header,main,footer){Name="AppShellFlow",UseLayoutRounding=false};root.Children.Add(panel);minimum=new(this);UpdateMinimum();Render();
    }
    private void Render()
    {
        if(!double.IsFinite(HeaderHeight)||HeaderHeight<0)throw new ArgumentOutOfRangeException(nameof(HeaderHeight));
        if(panel is null||header is null||body is null||footer is null||main is null)return;
        foreach(var slot in new[]{header,body,footer})
        {
            slot.Text.FontFamily=FontFamily;slot.Text.FontSize=FontSize;slot.Text.FontWeight=FontWeight;slot.Text.FontStyle=FontStyle;slot.Text.FontStretch=FontStretch;slot.Text.CharacterSpacing=CharacterSpacing;slot.Text.Foreground=Foreground;slot.Text.LineHeight=FontSize*1.5;slot.Text.Language=Language;slot.Text.IsTextScaleFactorEnabled=IsTextScaleFactorEnabled;
        }
        SetSlot(header,Header,HeaderTemplate,HeaderTemplateSelector);SetSlot(body,Content,ContentTemplate,ContentTemplateSelector);SetSlot(footer,Footer,FooterTemplate,FooterTemplateSelector);
        main.Background=ContentBackground;main.InvalidateMeasure();panel.InvalidateMeasure();InvalidateMeasure();
    }
    private void SetSlot(BleeckerContentText slot,object? value,DataTemplate? template,DataTemplateSelector? selector)
    {
        if(slots.TryGetValue(slot,out var previous)&&(value is string or bool||BleeckerReactContent.Numeric(value)?Equals(previous.Value,value):ReferenceEquals(previous.Value,value))&&ReferenceEquals(previous.Template,template)&&ReferenceEquals(previous.Selector,selector))return;
        slot.Set(value,template,selector:selector);
        var scalar=template is null&&selector is null?BleeckerReactContent.Display(value)as string:null;
        AutomationProperties.SetAccessibilityView(slot.Text,scalar is not null&&BleeckerInlineText.Normalize(scalar).Length>0?AccessibilityView.Content:AccessibilityView.Raw);
        slots[slot]=(value,template,selector);
    }
    private void Detach(){minimum?.Release();minimum=null;panel?.Release();foreach(var slot in slots.Keys)slot.Release();slots.Clear();main?.Children.Clear();panel?.Children.Clear();root?.Children.Clear();root=null;panel=null;main=null;header=body=footer=null;}
    protected override Size MeasureOverride(Size available){if(IsLoaded)ObserveRoot();UpdateMinimum();var measured=base.MeasureOverride(available);return new(double.IsFinite(available.Width)?available.Width:measured.Width,measured.Height);}
    protected override AutomationPeer OnCreateAutomationPeer()=>new ShellPeer(this);
    private sealed class ShellPeer(BleeckerAppShell owner):FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
        protected override string GetClassNameCore()=>nameof(BleeckerAppShell);
    }
    private sealed partial class ShellPanel:Panel
    {
        private readonly BleeckerAppShell owner;private readonly BleeckerContentText header,footer;private readonly MainRegion main;
        internal ShellPanel(BleeckerAppShell owner,BleeckerContentText header,MainRegion main,BleeckerContentText footer){this.owner=owner;this.header=header;this.main=main;this.footer=footer;Children.Add(header);Children.Add(main);Children.Add(footer);header.StackingOrderChanged+=UpdateStacking;footer.StackingOrderChanged+=UpdateStacking;}
        // Source header/footer nodes are direct flex children. Their native
        // presentation wrappers must preserve the original root's stacking.
        private void UpdateStacking(){if(Canvas.GetZIndex(header)!=header.ContentZIndex)Canvas.SetZIndex(header,header.ContentZIndex);if(Canvas.GetZIndex(footer)!=footer.ContentZIndex)Canvas.SetZIndex(footer,footer.ContentZIndex);}
        internal void Release(){header.StackingOrderChanged-=UpdateStacking;footer.StackingOrderChanged-=UpdateStacking;}
        protected override Size MeasureOverride(Size available)
        {
            foreach(var child in new FrameworkElement[]{header,main,footer})child.Measure(new Size(available.Width,double.PositiveInfinity));
            var outer=owner.Padding.Top+owner.Padding.Bottom+owner.BorderThickness.Top+owner.BorderThickness.Bottom;
            var minimum=Math.Max(0,owner.MinHeight-outer);
            var natural=header.DesiredSize.Height+main.DesiredSize.Height+footer.DesiredSize.Height;
            var height=double.IsFinite(owner.Height)?Math.Max(minimum,available.Height):Math.Max(minimum,natural);
            var mainHeight=BleeckerAppShellModel.Arrange(height,header.DesiredSize.Height,footer.DesiredSize.Height,owner.Insets.Top,owner.Insets.Bottom).MainHeight;
            main.Measure(new Size(available.Width,mainHeight));
            return new(double.IsFinite(available.Width)?available.Width:Math.Max(header.DesiredSize.Width,Math.Max(main.DesiredSize.Width,footer.DesiredSize.Width)),height);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var tracks=BleeckerAppShellModel.Arrange(final.Height,header.DesiredSize.Height,footer.DesiredSize.Height,owner.Insets.Top,owner.Insets.Bottom);
            header.Arrange(new Rect(0,0,final.Width,header.DesiredSize.Height));main.Arrange(new Rect(0,tracks.MainY,final.Width,tracks.MainHeight));footer.Arrange(new Rect(0,tracks.FooterY,final.Width,footer.DesiredSize.Height));return final;
        }
    }
    private sealed partial class MainRegion:Panel
    {
        private readonly BleeckerAppShell owner;private readonly InnerHost inner;
        internal MainRegion(BleeckerAppShell owner,FrameworkElement body){this.owner=owner;inner=new(body){Name="AppShellContent",UseLayoutRounding=false};Children.Add(inner);}
        protected override Size MeasureOverride(Size available)
        {
            var padding=owner.Insets;inner.Measure(new Size(Math.Max(0,available.Width-padding.Left-padding.Right),Math.Max(0,available.Height-padding.Top-padding.Bottom)));
            return new(inner.DesiredSize.Width+padding.Left+padding.Right,Math.Min(available.Height,inner.DesiredSize.Height+padding.Top+padding.Bottom));
        }
        protected override Size ArrangeOverride(Size final){var padding=owner.Insets;inner.Arrange(new Rect(padding.Left,padding.Top,Math.Max(0,final.Width-padding.Left-padding.Right),Math.Max(0,final.Height-padding.Top-padding.Bottom)));return final;}
        protected override AutomationPeer OnCreateAutomationPeer()=>new MainPeer(this);
        private sealed class MainPeer(MainRegion owner):FrameworkElementAutomationPeer(owner)
        {
            protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Group;
            protected override AutomationLandmarkType GetLandmarkTypeCore()=>AutomationLandmarkType.Main;
            protected override string GetClassNameCore()=>"BleeckerAppShellMain";
        }
    }
    private sealed partial class InnerHost:Panel
    {
        private readonly FrameworkElement body;
        internal InnerHost(FrameworkElement body){this.body=body;Children.Add(body);}
        protected override Size MeasureOverride(Size available){body.Measure(new Size(available.Width,double.PositiveInfinity));return new(body.DesiredSize.Width,Math.Min(available.Height,body.DesiredSize.Height));}
        protected override Size ArrangeOverride(Size final){body.Arrange(new Rect(0,0,final.Width,body.DesiredSize.Height));return final;}
    }
}
