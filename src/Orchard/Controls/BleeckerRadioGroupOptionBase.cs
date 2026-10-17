using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.ViewManagement;

namespace Orchard;

internal abstract class BleeckerRadioGroupOptionBase:Button
{
    private readonly UISettings settings=new();
    private readonly AccessibilitySettings accessibility=new();
    private readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    private readonly BleeckerCssTransition<(double L,double A,double B,double Alpha)> strokeMotion=new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<double> dotMotion=new((a,b,t)=>a+(b-a)*t);
    private readonly SolidColorBrush stroke=new();
    private readonly BleeckerDisabledPointer pointer;
    private readonly BleeckerReferenceText.IntrinsicWidth labelWidth=new(),descriptionWidth=new();
    private Grid? root;
    private Ellipse? ring,dot;
    private TextBlock? label,description;
    private TextBlock[] probes=[];
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> paletteObservers=[];
    private string title="";
    private string? details;
    private bool selected,over,rendering;
    internal object TextMeasurements=>new{label= new{labelWidth.RawWidth,labelWidth.RegionWidths,labelWidth.GlyphRuns},description=new{descriptionWidth.RawWidth,descriptionWidth.RegionWidths,descriptionWidth.GlyphRuns}};
    internal BleeckerRadioGroupOptionBase()
    {
        pointer=new(this,()=>root,SetOver,observeEnabled:true);
        Loaded+=(_,_)=>{settings.ColorValuesChanged-=Colors;settings.ColorValuesChanged+=Colors; if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)){settings.AnimationsEnabledChanged-=Animations;settings.AnimationsEnabledChanged+=Animations;} pointer.Update(); pointer.ReconcileLifecycle(); ObservePalette(); Paint(false);};
        Unloaded+=(_,_)=>{if(IsLoaded)return; settings.ColorValuesChanged-=Colors;if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=Animations;pointer.Detach();StopObservingPalette();Stop();};
        ActualThemeChanged+=(_,_)=>Paint(false);
        RegisterPropertyChangedCallback(IsEnabledProperty,(_,_)=>{pointer.Update();Paint(false);});
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null){pointer.Detach();StopObservingPalette();Stop();root=null;ring=dot=null;label=description=null;probes=[];}});
    }
    protected override void OnApplyTemplate()
    {
        StopObservingPalette();Stop();base.OnApplyTemplate();
        root=Part<Grid>("OptionRoot");ring=Part<Ellipse>("RadioRing");dot=Part<Ellipse>("RadioDot");label=Part<TextBlock>("RadioLabel");description=Part<TextBlock>("RadioDescription");
        probes=[Part<TextBlock>("OffProbe"),Part<TextBlock>("HoverProbe"),Part<TextBlock>("OnProbe")];ring.Stroke=stroke;
        SetText(title,details);pointer.Update();if(IsLoaded)ObservePalette();Paint(false);
    }
    private T Part<T>(string name)where T:FrameworkElement=>GetTemplateChild(name)as T??throw new InvalidOperationException("Radio option requires "+name);
    internal void SetText(string value,string? detail)
    {
        title=value;details=detail;
        if(label is not null&&description is not null){BleeckerReferenceText.Set(label,value);BleeckerReferenceText.Set(description,detail??"");description.Visibility=string.IsNullOrEmpty(detail)?Visibility.Collapsed:Visibility.Visible;InvalidateMeasure();}
    }
    internal void Select(bool value)
    {
        if(selected==value)return;var old=selected;selected=value;Paint(true);
        if(FrameworkElementAutomationPeer.FromElement(this)is{}peer)
        {
            peer.RaisePropertyChangedEvent(Microsoft.UI.Xaml.Automation.SelectionItemPatternIdentifiers.IsSelectedProperty,old,value);
            if(value)peer.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
        }
    }
    protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size available)
    {
        if(label is null||description is null)return base.MeasureOverride(available);
        var textWidth=Math.Max(labelWidth.Measure(label,title),descriptionWidth.Measure(description,details??""));
        // A CSS block label and description share the flex item's widest text
        // column. Browser intrinsic inline widths round up to a 1/64px unit.
        textWidth=Math.Min(Math.Max(0,available.Width-30),Math.Ceiling(textWidth*64)/64);
        label.Width=description.Width=textWidth;root!.ColumnDefinitions[1].Width=new GridLength(textWidth);
        var width=30+textWidth;var result=base.MeasureOverride(new(width,available.Height));return new(width,result.Height);
    }
    private void SetOver(bool value){if(over==value)return;over=value;Paint(true);}
    private void Colors(UISettings sender,object args)=>DispatcherQueue.TryEnqueue(()=>Paint(false));
    private void Animations(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>DispatcherQueue.TryEnqueue(()=>Paint(false));
    private void Stop(){CompositionTarget.Rendering-=Frame;rendering=false;}
    private void ObservePalette()
    {
        StopObservingPalette();
        foreach(var probe in probes)
        {
            paletteObservers.Add((probe,TextBlock.ForegroundProperty,probe.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>{ObservePalette();Paint(true);})));
            if(probe.Foreground is SolidColorBrush brush)
                foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})
                    paletteObservers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Paint(true))));
        }
    }
    private void StopObservingPalette(){foreach(var observer in paletteObservers)observer.Owner.UnregisterPropertyChangedCallback(observer.Property,observer.Token);paletteObservers.Clear();}
    private void Paint(bool animate)
    {
        if(ring is null||dot is null||root is null||probes.Length!=3)return;
        var brush=(SolidColorBrush)probes[selected?2:over?1:0].Foreground;
        var time=clock.Elapsed.TotalMilliseconds;var moving=animate&&IsLoaded&&settings.AnimationsEnabled&&!accessibility.HighContrast;
        strokeMotion.Retarget(BleeckerColorMix.Premultiply((brush.Color.R,brush.Color.G,brush.Color.B,brush.Color.A/255d*brush.Opacity)),time,BleeckerTokens.Durations["control"],moving);
        dotMotion.Retarget(selected?1:0,time,BleeckerTokens.Durations["surface"],moving);root.Opacity=IsEnabled?1:.5;Draw(time);
        if(strokeMotion.IsRunning(time)||dotMotion.IsRunning(time)){if(!rendering){CompositionTarget.Rendering+=Frame;rendering=true;}}else Stop();
    }
    private void Draw(double time)
    {
        var color=BleeckerColorMix.EncodePremultiplied(strokeMotion.ValueAt(time));stroke.Color=Windows.UI.Color.FromArgb(255,color.R,color.G,color.B);stroke.Opacity=color.Alpha;
        if(dot is not null)dot.RenderTransform=new ScaleTransform{ScaleX=dotMotion.ValueAt(time),ScaleY=dotMotion.ValueAt(time)};
    }
    private void Frame(object? sender,object args){var time=clock.Elapsed.TotalMilliseconds;Draw(time);if(!strokeMotion.IsRunning(time)&&!dotMotion.IsRunning(time))Stop();}
}
