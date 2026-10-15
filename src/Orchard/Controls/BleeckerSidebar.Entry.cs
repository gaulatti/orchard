using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Orchard;

public sealed partial class BleeckerSidebar
{
    private sealed class Entry(BleeckerSidebar owner,string key,bool initialOpen)
    {
        internal string Key {get;}=key;
        internal StackPanel Root {get;}=new(){UseLayoutRounding=false};
        internal bool Visible {get;private set;}
        private bool open=initialOpen;
        private BleeckerSidebarItem? item;
        private FrameworkElement? action;
        private BleeckerSidebarLinkContext? context;
        private Body? body;
        private bool group;
        private readonly List<Entry> children=[];
        private Border? childBorder;
        private StackPanel? childPanel;
        internal void Update(BleeckerSidebarItem value)
        {
            item=value;var nextGroup=!owner.Collapsed&&item.Items.Count>0;
            Visible=nextGroup||!string.IsNullOrEmpty(item.Href);
            if(!Visible){DisposeVisual();return;}
            if(action is null||group!=nextGroup)
            {
                DisposeVisual();group=nextGroup;
                body=new Body(owner,group);
                context=new(owner,group,group?()=>SetOpen(!open):null){Item=item,Content=body.Root,Chevron=body.Chevron,ChevronStroke=body.ChevronPath,Active=item.Active,Disabled=!group&&item.Disabled,Expanded=open,Label=item.Label,Href=item.Href??""};
                Typography();
                body.Connect(context);
                var style=new Style{TargetType=group?typeof(Button):typeof(HyperlinkButton),BasedOn=(Style)owner.actions[group?typeof(Button):typeof(HyperlinkButton)]};
                style.Setters.Add(new Setter(BleeckerSidebarAction.ContextProperty,context));context.Style=style;
                if(group)action=new GroupButton(context){Content=body.Root,Style=style,Command=context.Command};
                else if(owner.LinkTemplate is null)action=new HyperlinkButton{Content=body.Root,Style=style,Command=context.Command};
                else action=new ContentPresenter{Content=context,ContentTemplate=owner.LinkTemplate,UseLayoutRounding=false};
                Root.Children.Add(action);
            }
            context!.Item=item;context.Label=item.Label;context.Href=item.Href??"";context.Active=item.Active;context.Disabled=!group&&item.Disabled;
            context.Expanded=open;context.Mounted=true;Typography();body!.Update(item,owner.Collapsed);
            action.Tag=item;AutomationProperties.SetName(action,item.Label);AutomationProperties.SetHelpText(action,group?"":item.Href??"");
            AutomationProperties.SetItemStatus(action,!group&&item.Active?"Current page":"");
            if(group&&open)
            {
                if(childPanel is null)
                {
                    childPanel=new StackPanel{UseLayoutRounding=false};childBorder=SourceBorder("PART_Children");
                    childBorder.BorderThickness=new Thickness(1,0,0,0);childBorder.Padding=new Thickness(16,0,0,0);childBorder.Margin=new Thickness(20,4,0,0);childBorder.Child=childPanel;
                    Root.Children.Add(childBorder);
                }
                var unused=RetainEntries(children,item.Items.Where(child=>!string.IsNullOrEmpty(child.Href)).Select(child=>child.Id));var next=new List<Entry>();var visible=new List<UIElement>();
                foreach(var child in item.Items.Where(child=>!string.IsNullOrEmpty(child.Href)))
                {
                    var at=unused.FindIndex(entry=>entry.Key==child.Id);var entry=at>=0?unused[at]:new Entry(owner,child.Id,false);
                    if(at>=0)unused.RemoveAt(at);entry.UpdateLink(child);next.Add(entry);visible.Add(entry.Root);
                }
                foreach(var old in unused)old.DisposeVisual();children.Clear();children.AddRange(next);Reconcile(childPanel,visible);
            }
            else ClearChildren();
            context.Notify();
        }
        // Immediate children remain links even when their records contain Items.
        private void UpdateLink(BleeckerSidebarItem value)
        {
            item=value;Visible=true;
            if(action is null||group)
            {
                DisposeVisual();group=false;body=new Body(owner,false);
                context=new(owner,false,null){Item=value,Content=body.Root,Active=value.Active,Disabled=value.Disabled,Label=value.Label,Href=value.Href!};
                Typography();
                body.Connect(context);
                var style=new Style{TargetType=typeof(HyperlinkButton),BasedOn=(Style)owner.actions[typeof(HyperlinkButton)]};style.Setters.Add(new Setter(BleeckerSidebarAction.ContextProperty,context));context.Style=style;
                action=owner.LinkTemplate is null?new HyperlinkButton{Content=body.Root,Style=style,Command=context.Command}:new ContentPresenter{Content=context,ContentTemplate=owner.LinkTemplate,UseLayoutRounding=false};
                Root.Children.Add(action);
            }
            context!.Item=value;context.Label=value.Label;context.Href=value.Href!;context.Active=value.Active;context.Disabled=value.Disabled;context.Mounted=true;
            Typography();body!.Update(value,false);action.Tag=value;AutomationProperties.SetName(action,value.Label);AutomationProperties.SetHelpText(action,value.Href!);AutomationProperties.SetItemStatus(action,value.Active?"Current page":"");context.Notify();
        }
        private void Typography()
        {
            context!.FontSize=owner.FontSize;context.FontWeight=item!.Active?Microsoft.UI.Text.FontWeights.Medium:owner.FontWeight;context.CharacterSpacing=owner.CharacterSpacing;
            var family=owner.FontFamily;
            context.FontFamily=family.Source==((FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"]).Source&&context.FontWeight.Weight==500?(FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"]:family;
        }
        private void SetOpen(bool value)
        {
            if(open==value)return;var before=open;open=value;Update(item!);
            if(action is GroupButton button && FrameworkElementAutomationPeer.FromElement(button) is GroupPeer peer)peer.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,before?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed,value?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed);
        }
        internal void Notify(){context?.Notify();foreach(var child in children)child.Notify();}
        internal void ReplaceLinkTemplate(){if(!group)DisposeVisual();else foreach(var child in children)child.ReplaceLinkTemplate();}
        private void ClearChildren(){foreach(var child in children)child.DisposeVisual();children.Clear();if(childBorder is not null)Root.Children.Remove(childBorder);if(childBorder is not null)childBorder.Child=null;childPanel?.Children.Clear();childBorder=null;childPanel=null;}
        internal void DisposeVisual()
        {
            ClearChildren();if(context is not null){context.Mounted=false;context.Notify();context.Content=null;}
            if(action is ContentControl content)content.Content=null;
            if(action is ContentPresenter presenter)presenter.Content=null;
            body?.Release();Root.Children.Clear();action=null;context=null;body=null;
        }
    }
    private sealed class Body
    {
        internal Grid Root {get;}=new(){UseLayoutRounding=false,ColumnSpacing=12,VerticalAlignment=VerticalAlignment.Center};
        private readonly ContentPresenter icon=new(){UseLayoutRounding=false,VerticalAlignment=VerticalAlignment.Center,HorizontalContentAlignment=HorizontalAlignment.Center};
        private readonly ContentPresenter badge=new(){UseLayoutRounding=false,VerticalAlignment=VerticalAlignment.Center};
        private readonly TextBlock label=new(){Name="PART_Label",FontSize=14,FontWeight=Microsoft.UI.Text.FontWeights.Medium,LineHeight=20,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,TextWrapping=TextWrapping.NoWrap,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center,UseLayoutRounding=false};
        private readonly Viewbox? chevron;
        private readonly BleeckerSidebarContentForeground iconForeground=new(),badgeForeground=new();
        private BleeckerSidebarLinkContext? context;
        internal RotateTransform? Chevron {get;}
        internal Microsoft.UI.Xaml.Shapes.Path? ChevronPath {get;}
        internal Body(BleeckerSidebar owner,bool group)
        {
            label.SetBinding(TextBlock.FontFamilyProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(FontFamily)),Converter=new MediumFace()});
            label.SetBinding(TextBlock.CharacterSpacingProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(CharacterSpacing))});
            if(group)
            {
                chevron=(Viewbox)XamlReader.Load("<Viewbox xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='14' Height='14' VerticalAlignment='Center' IsHitTestVisible='False' RenderTransformOrigin='.5,.5'><Canvas Width='24' Height='24'><Path Data='M9,18 L15,12 L9,6' Stroke='{ThemeResource BleeckerTextSecondaryBrush}' StrokeThickness='2' StrokeStartLineCap='Round' StrokeEndLineCap='Round' StrokeLineJoin='Round'/></Canvas></Viewbox>");
                // The icon is prepared outside the action's template namescope.
                var path=(Microsoft.UI.Xaml.Shapes.Path)((Canvas)chevron.Child).Children[0];
                ChevronPath=path;
                Chevron=new RotateTransform();chevron.RenderTransform=Chevron;
                AutomationProperties.SetAccessibilityView(chevron,AccessibilityView.Raw);
            }
        }
        internal void Update(BleeckerSidebarItem item,bool collapsed)
        {
            ValidateContent(item.Icon,item.IconTemplate);if(!collapsed)ValidateContent(item.Badge,item.BadgeTemplate);
            iconForeground.Bind(item.IconTemplate is null?item.Icon:null,context!);badgeForeground.Bind(!collapsed&&item.BadgeTemplate is null?item.Badge:null,context!);
            var elements=new List<UIElement>();var widths=new List<GridLength>();
            var iconVisible=item.IconTemplate is not null||Truthy(item.Icon)||Numeric(item.Icon);
            if(iconVisible)
            {
                icon.Width=item.IconTemplate is not null||Truthy(item.Icon)?20:double.NaN;icon.Height=item.IconTemplate is not null||Truthy(item.Icon)?20:double.NaN;icon.ContentTemplate=item.IconTemplate;icon.Content=item.IconTemplate is null?DisplayValue(item.Icon):item.Icon;
                elements.Add(icon);widths.Add(GridLength.Auto);
            }
            else icon.Content=null;
            if(!collapsed)
            {
                label.Text=item.Label;elements.Add(label);widths.Add(new GridLength(1,GridUnitType.Star));
                if(item.BadgeTemplate is not null||Truthy(item.Badge)||Numeric(item.Badge)){badge.ContentTemplate=item.BadgeTemplate;badge.Content=item.BadgeTemplate is null?DisplayValue(item.Badge):item.Badge;elements.Add(badge);widths.Add(GridLength.Auto);}else badge.Content=null;
                if(chevron is not null){elements.Add(chevron);widths.Add(GridLength.Auto);}
            }
            else badge.Content=null;
            Root.ColumnDefinitions.Clear();foreach(var width in widths)Root.ColumnDefinitions.Add(new ColumnDefinition{Width=width});
            Reconcile(Root,elements);for(var index=0;index<elements.Count;index++)Grid.SetColumn((FrameworkElement)elements[index],index);
        }
        internal void Connect(BleeckerSidebarLinkContext value)
        {
            context=value;label.SetBinding(TextBlock.ForegroundProperty,new Binding{Source=value,Path=new PropertyPath(nameof(BleeckerSidebarLinkContext.Foreground))});
            icon.SetBinding(ContentPresenter.ForegroundProperty,new Binding{Source=value,Path=new PropertyPath(nameof(BleeckerSidebarLinkContext.Foreground))});
            badge.SetBinding(ContentPresenter.ForegroundProperty,new Binding{Source=value,Path=new PropertyPath(nameof(BleeckerSidebarLinkContext.Foreground))});
            foreach(var presenter in new[]{icon,badge})
                foreach(var(property,name)in new[]{(ContentPresenter.FontFamilyProperty,nameof(BleeckerSidebarLinkContext.FontFamily)),(ContentPresenter.FontSizeProperty,nameof(BleeckerSidebarLinkContext.FontSize)),(ContentPresenter.FontWeightProperty,nameof(BleeckerSidebarLinkContext.FontWeight)),(ContentPresenter.CharacterSpacingProperty,nameof(BleeckerSidebarLinkContext.CharacterSpacing))})
                    presenter.SetBinding(property,new Binding{Source=value,Path=new PropertyPath(name)});
        }
        internal void Release(){iconForeground.Release();badgeForeground.Release();icon.Content=null;badge.Content=null;Root.Children.Clear();}
        private sealed class MediumFace:IValueConverter
        {
            public object Convert(object value,Type targetType,object parameter,string language)=>value is FontFamily family&&family.Source==((FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"]).Source?Application.Current.Resources["BleeckerPrimaryMediumFontFamily"]:value;
            public object ConvertBack(object value,Type targetType,object parameter,string language)=>throw new NotSupportedException();
        }
    }
    private sealed class GroupButton(BleeckerSidebarLinkContext context):Button
    {protected override AutomationPeer OnCreateAutomationPeer()=>new GroupPeer(this,context);}
    private sealed class GroupPeer(GroupButton owner,BleeckerSidebarLinkContext context):ButtonAutomationPeer(owner),IExpandCollapseProvider
    {
        public ExpandCollapseState ExpandCollapseState=>context.Expanded?ExpandCollapseState.Expanded:ExpandCollapseState.Collapsed;
        public void Expand(){if(!IsEnabled())throw new ElementNotEnabledException();if(!context.Expanded)context.Toggle!();}
        public void Collapse(){if(!IsEnabled())throw new ElementNotEnabledException();if(context.Expanded)context.Toggle!();}
        protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.ExpandCollapse?this:base.GetPatternCore(pattern);
    }
}
