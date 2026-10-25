using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;

namespace Orchard;

// The public Select recipe shares input/selection/scroll behavior with the
// existing chooser, keeping the historical table recipes separate.
internal sealed class BleeckerSelectAppearance:IDisposable
{
    internal BleeckerSelect Owner {get;}
    private readonly BleeckerSelectSurface surface;
    private readonly ResourceDictionary resources=new(){Source=new Uri("ms-appx:///Orchard/Themes/SelectSurface.xaml")};
    private readonly ResourceDictionary palette=new(){Source=new Uri("ms-appx:///Orchard/Themes/SelectPalette.xaml")};
    private readonly UISettings settings=new();
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly SolidColorBrush background=new(),border=new(),ringPaint=new();
    private readonly BleeckerCssTransition<BleeckerCssColor> backgroundMotion=new(BleeckerCssColor.Mix),borderMotion=new(BleeckerCssColor.Mix),ringMotion=new(BleeckerCssColor.Mix);
    private readonly BleeckerCssTransition<double> opacityMotion=new((a,b,p)=>a+(b-a)*p),focusMotion=new((a,b,p)=>a+(b-a)*p),shadowMotion=new((a,b,p)=>a+(b-a)*p);
    private readonly Dictionary<BleeckerSelectItem,BleeckerContentText> labels=[];
    private Grid? root,layout;
    private Border? trigger,ring;
    private ContentPresenter? iconHost;
    private BleeckerCssShadow? shadow;
    private XamlRoot? observedRoot;
    private object? renderedIcon;
    private DataTemplate? renderedTemplate;
    private DataTemplateSelector? renderedSelector;
    private bool iconSet,over,focused,rendering,disposed;
    internal BleeckerContentText? Label {get;private set;}
    internal BleeckerContentText? Icon {get;private set;}
    internal double PopupWidth=>Math.Max(Owner.HeaderRecipe?176:0,surface.ActualWidth);
    internal ControlTemplate Template=>(ControlTemplate)resources["SelectSurfaceTemplate"];
    internal ControlTemplate OptionTemplate=>(ControlTemplate)resources["SelectOptionTemplate"];
    internal BleeckerSelectAppearance(BleeckerSelect owner,BleeckerSelectSurface surface)
    {
        Owner=owner;this.surface=surface;surface.Loaded+=Loaded;surface.Unloaded+=Unloaded;surface.ActualThemeChanged+=ThemeChanged;
    }
    internal void Attach(Border border,Border ring,ContentPresenter display)
    {
        shadow?.Dispose();trigger=border;this.ring=ring;root=surface.AppearancePart<Grid>("SelectRoot");layout=surface.AppearancePart<Grid>("TriggerLayout");iconHost=surface.AppearancePart<ContentPresenter>("StartIcon");
        shadow=new(surface,trigger,surface.AppearancePart<Grid>("ShadowHost"),surface.AppearancePart<Microsoft.UI.Xaml.Shapes.Path>("ShadowMask"),surface.AppearancePart<Microsoft.UI.Xaml.Shapes.Path>("ShadowExterior"));Update();Paint(false,false,false);
    }
    internal void Update()
    {
        var metrics=Owner.Metrics;surface.FontSize=BleeckerSelect.Supplied(Owner,Control.FontSizeProperty)?Owner.FontSize:metrics.Font;surface.FontWeight=Owner.FontWeight;surface.Foreground=Owner.Foreground;surface.Padding=BleeckerSelect.Supplied(Owner,Control.PaddingProperty)?Owner.Padding:new Thickness(metrics.Padding,0,metrics.Padding,0);surface.BorderThickness=Owner.BorderThickness;surface.CornerRadius=Owner.CornerRadius;
        if(Label is not null)TextStyle(Label,metrics.Line,Owner.Foreground);
        if(trigger is null||layout is null||iconHost is null)return;
        trigger.CornerRadius=Owner.CornerRadius;var radius=Owner.CornerRadius;ring!.CornerRadius=new CornerRadius(radius.TopLeft+2,radius.TopRight+2,radius.BottomRight+2,radius.BottomLeft+2);
        Icon??=new BleeckerContentText("SelectStartIcon","BleeckerPrimaryFontFamily",metrics.Font,metrics.Line,"BleeckerSeaBrush"){NoWrap=true};AutomationProperties.SetAccessibilityView(Icon,AccessibilityView.Raw);iconHost.Content=Icon;
        var present=Owner.StartIconTemplate is not null||Owner.StartIconTemplateSelector is not null||BleeckerReactContent.Present(Owner.StartIcon);
        if(!iconSet||!ReferenceEquals(renderedIcon,Owner.StartIcon)||!ReferenceEquals(renderedTemplate,Owner.StartIconTemplate)||!ReferenceEquals(renderedSelector,Owner.StartIconTemplateSelector)){Icon.Set(Owner.StartIcon,Owner.StartIconTemplate,selector:Owner.StartIconTemplateSelector);renderedIcon=Owner.StartIcon;renderedTemplate=Owner.StartIconTemplate;renderedSelector=Owner.StartIconTemplateSelector;iconSet=true;}
        TextStyle(Icon,metrics.Line,surface.Brush(surface.tokens,"BleeckerSeaBrush"));iconHost.Visibility=present?Visibility.Visible:Visibility.Collapsed;layout.ColumnDefinitions[1].Width=new GridLength(present?metrics.Gap:0);layout.ColumnDefinitions[3].Width=new GridLength(metrics.Gap);
        foreach(var old in labels.Keys.Where(item=>!surface.Items.Contains(item)).ToArray()){labels[old].Release();labels.Remove(old);}
        Paint(over,focused,false);
    }
    private void TextStyle(BleeckerContentText text,double line,Brush brush){text.Text.FontFamily=Owner.FontFamily;text.Text.FontSize=surface.FontSize;text.Text.FontWeight=Owner.FontWeight;text.Text.LineHeight=line;text.Text.Foreground=brush;text.Text.Language=Owner.Language;text.Text.IsTextScaleFactorEnabled=Owner.IsTextScaleFactorEnabled;}
    internal void SetDisplay(ContentPresenter presenter,string value)
    {
        Label??=new BleeckerContentText("SelectValue","BleeckerPrimaryFontFamily",Owner.Metrics.Font,Owner.Metrics.Line,"BleeckerTextPrimaryBrush"){NoWrap=true};Label.Text.TextTrimming=TextTrimming.CharacterEllipsis;AutomationProperties.SetAccessibilityView(Label,AccessibilityView.Raw);TextStyle(Label,Owner.Metrics.Line,Owner.Foreground);Label.Set(value,null);presenter.Content=Label;
    }
    internal void RenderItem(BleeckerSelectItem item)
    {
        var metrics=Owner.Metrics;var selected=surface.SelectionMatches(item);item.MinHeight=metrics.OptionHeight;item.Padding=new Thickness(12,metrics.OptionPadding,36,metrics.OptionPadding);item.FontSize=surface.FontSize;item.FontWeight=selected?Microsoft.UI.Text.FontWeights.Medium:Owner.FontWeight;var regular=(FontFamily)Application.Current.Resources["BleeckerPrimaryFontFamily"];item.FontFamily=selected&&Owner.FontFamily.Source==regular.Source?(FontFamily)Application.Current.Resources["BleeckerPrimaryMediumFontFamily"]:Owner.FontFamily;
        item.Foreground=surface.Brush(surface.tokens,selected?"BleeckerSeaBrush":"BleeckerTextPrimaryBrush");item.Background=Brush(item.FocusState!=FocusState.Unfocused?"SelectOptionHighlight":"SelectOptionBackground");item.Opacity=item.IsEnabled?1:.4;
        if(item.AppearancePart<ContentPresenter>("OptionContent")is{} presenter)
        {
            if(!labels.TryGetValue(item,out var label)){label=new("SelectOptionText","BleeckerPrimaryFontFamily",metrics.Font,metrics.Line,"BleeckerTextPrimaryBrush");labels.Add(item,label);}
            label.Text.FontFamily=item.FontFamily;label.Text.FontSize=item.FontSize;label.Text.FontWeight=item.FontWeight;label.Text.Foreground=item.Foreground;label.Text.LineHeight=metrics.Line;label.Text.IsTextScaleFactorEnabled=Owner.IsTextScaleFactorEnabled;label.Text.Language=Owner.Language;label.Set(item.Content?.ToString()??"",null);presenter.Content=label;
        }
        if(item.AppearancePart<FrameworkElement>("Checkmark")is{} mark)mark.Visibility=selected?Visibility.Visible:Visibility.Collapsed;
    }
    private Brush Brush(string key)=>surface.Brush(palette,key);
    private static BleeckerCssColor Color(Brush? brush,bool legacy=false){if(brush is null)return BleeckerCssColor.FromRgba((0,0,0,0),legacy);if(brush is not SolidColorBrush solid)throw new ArgumentException("Select animated colors require solid brushes.");return BleeckerCssColor.FromRgba((solid.Color.R,solid.Color.G,solid.Color.B,solid.Color.A/255d*solid.Opacity),legacy);}
    internal void Paint(bool pointer,bool hasFocus,bool animate=true)
    {
        over=pointer;focused=hasFocus;if(disposed||trigger is null)return;var time=clock.Elapsed.TotalMilliseconds;var motion=animate&&surface.IsLoaded&&surface.Visibility==Visibility.Visible&&surface.XamlRoot?.IsHostVisible==true&&settings.AnimationsEnabled;
        var normal=Owner.BorderBrush??Brush(Owner.HeaderRecipe?"SelectHeaderBorder":"SelectBorder");var outline=focused?Brush(Owner.Error?"SelectError":"SelectFocus"):over?Brush("SelectHover"):Owner.Error?Brush("SelectError"):normal;
        backgroundMotion.Retarget(Color(!surface.IsEnabled?Brush("SelectDisabled"):Owner.Background??Brush("SelectBackground")),time,190,motion);borderMotion.Retarget(Color(outline),time,190,motion);ringMotion.Retarget(Color(Brush(Owner.Error?"SelectErrorRing":"SelectRing")),time,190,motion);opacityMotion.Retarget(surface.IsEnabled?1:.6,time,190,motion);focusMotion.Retarget(focused?1:0,time,190,motion);shadowMotion.Retarget(over?1:0,time,190,motion);Draw(time);
        if(Running(time)){if(!rendering){CompositionTarget.Rendering+=Frame;rendering=true;}}else Stop();
    }
    private bool Running(double time)=>backgroundMotion.IsRunning(time)||borderMotion.IsRunning(time)||ringMotion.IsRunning(time)||opacityMotion.IsRunning(time)||focusMotion.IsRunning(time)||shadowMotion.IsRunning(time);
    private static void Set(SolidColorBrush brush,BleeckerCssColor color){var value=color.Encode();brush.Color=Windows.UI.Color.FromArgb(255,value.R,value.G,value.B);brush.Opacity=value.Alpha;}
    private void Draw(double time)
    {
        if(trigger is null||ring is null||root is null)return;Set(background,backgroundMotion.ValueAt(time));Set(border,borderMotion.ValueAt(time));Set(ringPaint,ringMotion.ValueAt(time));trigger.Background=background;trigger.BorderBrush=border;ring.BorderBrush=ringPaint;ring.Opacity=focusMotion.ValueAt(time);ring.Visibility=ring.Opacity>0?Visibility.Visible:Visibility.Collapsed;root.Opacity=opacityMotion.ValueAt(time);var hover=shadowMotion.ValueAt(time);shadow?.Set(new((float)(2+8*hover),(float)(1+2*hover),(float)(-8*hover),(float)((6+58*hover)/255d),26,55,77));
    }
    private void Frame(object? sender,object args){if(surface.XamlRoot?.IsHostVisible!=true||!settings.AnimationsEnabled){Paint(over,focused,false);return;}var time=clock.Elapsed.TotalMilliseconds;Draw(time);if(!Running(time))Stop();}
    private void Stop(){if(rendering){CompositionTarget.Rendering-=Frame;rendering=false;}}
    private void Loaded(object sender,RoutedEventArgs args){if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged+=AnimationsChanged;observedRoot=surface.XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;Paint(over,focused,false);}
    private void Unloaded(object sender,RoutedEventArgs args){Disconnect();Stop();}
    private void Disconnect(){if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=AnimationsChanged;if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void AnimationsChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>surface.DispatcherQueue.TryEnqueue(()=>Paint(over,focused,false));
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args){if(!sender.IsHostVisible)Paint(over,focused,false);}
    private void ThemeChanged(FrameworkElement sender,object args)=>Paint(over,focused,false);
    public void Dispose(){if(disposed)return;disposed=true;Disconnect();Stop();shadow?.Dispose();shadow=null;surface.Loaded-=Loaded;surface.Unloaded-=Unloaded;surface.ActualThemeChanged-=ThemeChanged;Icon?.Release();Label?.Release();foreach(var label in labels.Values)label.Release();labels.Clear();if(iconHost is not null)iconHost.Content=null;}
}
