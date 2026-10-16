using System.Diagnostics;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Orchard;

internal sealed class BleeckerFilterAction : Button
{
    private readonly Control owner;
    private readonly bool remove;
    private readonly Action invoke;
    private readonly Func<bool> canInvoke;
    private readonly Func<Brush> normal,hover,hoverBackground;
    private readonly Func<string>? textValue;
    private readonly Border chrome=new();
    private readonly Grid visual=new(){RenderTransformOrigin=new Point(.5,.5),UseLayoutRounding=false};
    private readonly ScaleTransform scale=new();
    private readonly BleeckerFilterText? text;
    private readonly SolidColorBrush foreground=new(),background=new();
    private readonly UISettings settings=new();
    private readonly AccessibilitySettings accessibility=new();
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly BleeckerCssTransition<(double R,double G,double B,double Alpha)> foregroundMotion=new(BleeckerSrgbColorMix.MixPremultiplied),backgroundMotion=new(BleeckerSrgbColorMix.MixPremultiplied);
    private readonly BleeckerDisabledPointer disabledPointer;
    private bool pointer,attached,rendering;
    internal BleeckerFilterAction(Control owner,bool remove,Action invoke,Func<bool> canInvoke,Func<Brush> normal,Func<Brush> hover,Func<Brush> hoverBackground,Func<string>? textValue=null)
    {
        this.owner=owner;this.remove=remove;this.invoke=invoke;this.canInvoke=canInvoke;this.normal=normal;this.hover=hover;this.hoverBackground=hoverBackground;this.textValue=textValue;
        var resources=new ResourceDictionary{Source=new Uri("ms-appx:///Orchard/Themes/FilterActions.xaml")};Style=(Style)resources[typeof(Button)];
        ProtectedCursor=InputSystemCursor.Create(InputSystemCursorShape.Hand);visual.RenderTransform=scale;
        chrome.Background=background;chrome.CornerRadius=new CornerRadius(remove?4:0);chrome.Padding=new Thickness(remove?2:0);visual.Children.Add(chrome);
        if(remove)
        {
            var icon=new Canvas{Width=12,Height=12};
            foreach(var line in new[]{new Line{X1=9,Y1=3,X2=3,Y2=9},new Line{X1=3,Y1=3,X2=9,Y2=9}})
            {line.Stroke=foreground;line.StrokeThickness=1;line.StrokeStartLineCap=line.StrokeEndLineCap=PenLineCap.Round;icon.Children.Add(line);}
            chrome.Child=icon;
        }
        else{text=new BleeckerFilterText{Name="ClearFilterText"};text.Text.Foreground=foreground;chrome.Child=text;}
        Content=visual;
        PointerEntered+=(_,_)=>{if(!attached)return;pointer=true;Update();};PointerExited+=(_,_)=>{if(!attached)return;pointer=false;Update();};IsEnabledChanged+=(_,_)=>{UpdateScale();disabledPointer?.Update();};
        RegisterPropertyChangedCallback(IsPressedProperty,(_,_)=>UpdateScale());Loaded+=(_,_)=>Attach();Unloaded+=(_,_)=>{if(!IsLoaded)Detach();};
        disabledPointer=new BleeckerDisabledPointer(this,()=>IsEnabled?null:this,_=>{});
    }
    internal void Attach()
    {
        if(attached)return;attached=true;Click+=Clicked;settings.ColorValuesChanged+=ColorsChanged;
        if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged+=MotionChanged;
        Update(false);
    }
    internal void Detach()
    {
        if(attached){Click-=Clicked;settings.ColorValuesChanged-=ColorsChanged;if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=MotionChanged;}
        attached=false;pointer=false;StopRendering();IsEnabled=false;disabledPointer.Detach();
    }
    private void Clicked(object sender,RoutedEventArgs args){if(attached&&IsLoaded&&canInvoke())invoke();}
    private void ColorsChanged(UISettings sender,object args)=>DispatcherQueue.TryEnqueue(()=>{if(attached&&IsLoaded)Update(false);});
    private void MotionChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>DispatcherQueue.TryEnqueue(()=>{if(attached&&IsLoaded)Update(false);});
    internal void Update(bool animate=true)
    {
        IsEnabled=canInvoke();var active=pointer&&IsEnabled;
        if(text is not null){text.Set(owner,textValue!(),false,16);text.Text.TextDecorations=active?Windows.UI.Text.TextDecorations.Underline:Windows.UI.Text.TextDecorations.None;}
        var now=clock.Elapsed.TotalMilliseconds;var moving=animate&&attached&&settings.AnimationsEnabled&&!accessibility.HighContrast;
        foregroundMotion.Retarget(Color(active?hover():normal()),now,190,moving);
        backgroundMotion.Retarget(active?Color(hoverBackground()):(0,0,0,0),now,190,moving);
        Paint(now);UpdateScale();disabledPointer.Update();
        if(foregroundMotion.IsRunning(now)||backgroundMotion.IsRunning(now)){if(!rendering){rendering=true;CompositionTarget.Rendering+=Frame;}}
        else StopRendering();
    }
    private void UpdateScale()=>scale.ScaleX=scale.ScaleY=remove&&IsPressed&&IsEnabled ? .9 : 1;
    private void Frame(object? sender,object args){var now=clock.Elapsed.TotalMilliseconds;Paint(now);if(!foregroundMotion.IsRunning(now)&&!backgroundMotion.IsRunning(now))StopRendering();}
    private void StopRendering(){if(rendering)CompositionTarget.Rendering-=Frame;rendering=false;}
    private void Paint(double now){Set(foreground,foregroundMotion.ValueAt(now));Set(background,backgroundMotion.ValueAt(now));}
    private static (double R,double G,double B,double Alpha) Color(Brush brush)
    {if(brush is not SolidColorBrush solid)throw new InvalidOperationException("Filter action palette entries must be solid brushes.");var c=solid.Color;return BleeckerSrgbColorMix.Premultiply((c.R,c.G,c.B,c.A/255d*solid.Opacity));}
    private static void Set(SolidColorBrush brush,(double R,double G,double B,double Alpha) color)
    {var c=BleeckerSrgbColorMix.EncodePremultiplied(color);brush.Color=Windows.UI.Color.FromArgb(255,c.R,c.G,c.B);brush.Opacity=c.Alpha;}
}
