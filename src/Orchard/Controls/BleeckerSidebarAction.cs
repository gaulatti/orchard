using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Lab = (double L,double A,double B,double Alpha);

namespace Orchard;

internal sealed class BleeckerSidebarAction
{
    internal static readonly DependencyProperty ContextProperty=DependencyProperty.RegisterAttached("Context",typeof(BleeckerSidebarLinkContext),typeof(BleeckerSidebarAction),new PropertyMetadata(null,(owner,args)=>
    {
        if(owner is not ButtonBase action)throw new ArgumentException("Sidebar source styles require a native ButtonBase.");
        if(action.GetValue(ControllerProperty) is BleeckerSidebarAction old)old.Dispose();
        action.SetValue(ControllerProperty,args.NewValue is BleeckerSidebarLinkContext context?new BleeckerSidebarAction(action,context):null);
    }));
    private static readonly DependencyProperty ControllerProperty=DependencyProperty.RegisterAttached("Controller",typeof(object),typeof(BleeckerSidebarAction),new PropertyMetadata(null));
    private readonly ButtonBase action;
    private readonly BleeckerSidebarLinkContext context;
    private readonly UISettings settings=new();
    private readonly AccessibilitySettings accessibility=new();
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly BleeckerCssTransition<Lab> background=new(BleeckerColorMix.MixPremultiplied),foreground=new(BleeckerColorMix.MixPremultiplied),rail=new(BleeckerColorMix.MixPremultiplied);
    private readonly BleeckerCssTransition<double> rotation=new((a,b,p)=>a+(b-a)*p);
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> observers=[];
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> brushObservers=[];
    private Border? normalPalette,activePalette,hoverPalette,railPalette,chrome,railChrome;
    private ContentPresenter? content;
    private TranslateTransform? translate;
    private bool hovering,rendering,loaded;
    private Lab? paintedBackground,paintedForeground,paintedRail;
    private BleeckerSidebarAction(ButtonBase action,BleeckerSidebarLinkContext context)
    {
        this.action=action;this.context=context;
        action.Loaded+=Loaded;action.Unloaded+=Unloaded;action.PointerEntered+=PointerEntered;action.PointerExited+=PointerExited;action.ActualThemeChanged+=ThemeChanged;
        if(action.IsLoaded)Load();
    }
    private void Loaded(object sender,RoutedEventArgs args)=>Load();
    private void Unloaded(object sender,RoutedEventArgs args)=>Unload();
    private void Load()
    {
        if(loaded)return;loaded=true;
        context.Changed+=ContextChanged;settings.ColorValuesChanged+=ColorsChanged;
        if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged+=MotionChanged;
        Watch(action,Control.TemplateProperty,()=>{ClearTemplate();ConnectTemplate();});
        Watch(action,ButtonBase.IsPressedProperty,()=>Refresh(true));Watch(action,Control.IsEnabledProperty,()=>Refresh(false));
        ConnectTemplate();
    }
    private void Unload()
    {
        if(!loaded)return;loaded=false;context.Changed-=ContextChanged;settings.ColorValuesChanged-=ColorsChanged;
        if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=MotionChanged;
        ClearTemplate();foreach(var sub in observers)sub.Owner.UnregisterPropertyChangedCallback(sub.Property,sub.Token);observers.Clear();hovering=false;
    }
    private void Dispose(){Unload();action.Loaded-=Loaded;action.Unloaded-=Unloaded;action.PointerEntered-=PointerEntered;action.PointerExited-=PointerExited;action.ActualThemeChanged-=ThemeChanged;}
    private void Watch(DependencyObject owner,DependencyProperty property,Action update)=>observers.Add((owner,property,owner.RegisterPropertyChangedCallback(property,(_,_)=>update())));
    private readonly List<(DependencyObject Owner,DependencyProperty Property,long Token)> paletteObservers=[];
    private void ConnectTemplate()
    {
        action.ApplyTemplate();if(!loaded||VisualTreeHelper.GetChildrenCount(action)==0)return;
        var root=(FrameworkElement)VisualTreeHelper.GetChild(action,0);
        normalPalette=root.FindName("PART_NormalPalette") as Border;activePalette=root.FindName("PART_ActivePalette") as Border;hoverPalette=root.FindName("PART_HoverPalette") as Border;railPalette=root.FindName("PART_RailPalette") as Border;
        chrome=root.FindName("PART_Chrome") as Border;railChrome=root.FindName("PART_Rail") as Border;content=root.FindName("PART_Content") as ContentPresenter;translate=root.FindName("PART_Move") as TranslateTransform;
        if(normalPalette is null||activePalette is null||hoverPalette is null||railPalette is null||chrome is null||railChrome is null||content is null||translate is null)throw new InvalidOperationException("Sidebar action template is missing its palette, chrome, content or movement parts.");
        foreach(var palette in new[]{normalPalette,activePalette,hoverPalette,railPalette})foreach(var property in new[]{Border.BackgroundProperty,Border.BorderBrushProperty})
            paletteObservers.Add((palette,property,palette.RegisterPropertyChangedCallback(property,(_,_)=>{ObserveBrushes();Refresh(false);})));
        ObserveBrushes();Refresh(false);
    }
    private void ClearTemplate()
    {
        StopRendering();foreach(var sub in paletteObservers)sub.Owner.UnregisterPropertyChangedCallback(sub.Property,sub.Token);paletteObservers.Clear();ClearBrushes();
        normalPalette=activePalette=hoverPalette=railPalette=chrome=railChrome=null;content=null;translate=null;paintedBackground=paintedForeground=paintedRail=null;
    }
    private void ObserveBrushes()
    {
        ClearBrushes();foreach(var brush in new[]{normalPalette,activePalette,hoverPalette,railPalette}.OfType<Border>().SelectMany(p=>new[]{p.Background,p.BorderBrush}).OfType<SolidColorBrush>().Distinct())
            foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})brushObservers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Refresh(false))));
    }
    private void ClearBrushes(){foreach(var sub in brushObservers)sub.Owner.UnregisterPropertyChangedCallback(sub.Property,sub.Token);brushObservers.Clear();}
    private void PointerEntered(object sender,Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args){hovering=true;Refresh(true);}
    private void PointerExited(object sender,Microsoft.UI.Xaml.Input.PointerRoutedEventArgs args){if(new Rect(0,0,action.ActualWidth,action.ActualHeight).Contains(args.GetCurrentPoint(action).Position))return;hovering=false;Refresh(true);}
    private void ContextChanged(object? sender,EventArgs args)=>Refresh(true);
    private void ThemeChanged(FrameworkElement sender,object args)=>Refresh(false);
    private void ColorsChanged(UISettings sender,object args)=>action.DispatcherQueue.TryEnqueue(()=>Refresh(false));
    private void MotionChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>action.DispatcherQueue.TryEnqueue(()=>Refresh(false));
    private void Refresh(bool animate)
    {
        if(normalPalette is null)return;
        action.IsHitTestVisible=!context.Disabled;action.Opacity=context.Disabled||!action.IsEnabled?.5:1;
        var target=hovering&&action.IsEnabled&&!context.Disabled?hoverPalette!:context.Active?activePalette!:normalPalette;
        var time=clock.Elapsed.TotalMilliseconds;var enabled=animate&&loaded&&settings.AnimationsEnabled&&!accessibility.HighContrast;
        background.Retarget(ColorOf(target.Background),time,BleeckerTokens.Durations["control"],enabled);
        foreground.Retarget(ColorOf(target.BorderBrush),time,BleeckerTokens.Durations["control"],enabled);
        rail.Retarget(context.Active&&!context.IsGroup?ColorOf(railPalette!.BorderBrush):default,time,BleeckerTokens.Durations["control"],enabled);
        rotation.Retarget(context.Expanded?90:0,time,BleeckerTokens.Durations["surface"],enabled);
        // The frozen arbitrary CSS transition list omits the individual
        // translate property. A held source press moves immediately.
        translate!.Y=action.IsPressed?1:0;
        Paint(time);
        if(Running(time)&&!rendering){rendering=true;CompositionTarget.Rendering+=Frame;}
    }
    private bool Running(double time)=>background.IsRunning(time)||foreground.IsRunning(time)||rail.IsRunning(time)||rotation.IsRunning(time);
    private void Frame(object? sender,object args){var time=clock.Elapsed.TotalMilliseconds;Paint(time);if(!Running(time))StopRendering();}
    private void StopRendering(){if(rendering)CompositionTarget.Rendering-=Frame;rendering=false;}
    private void Paint(double time)
    {
        var bg=background.ValueAt(time);var fg=foreground.ValueAt(time);var edge=rail.ValueAt(time);
        if(paintedBackground!=bg){chrome!.Background=BrushOf(bg);paintedBackground=bg;}
        if(paintedForeground!=fg){content!.Foreground=BrushOf(fg);context.Foreground=content.Foreground;if(context.ChevronStroke is not null)context.ChevronStroke.Stroke=content.Foreground;paintedForeground=fg;}
        if(paintedRail!=edge){railChrome!.BorderBrush=BrushOf(edge);paintedRail=edge;}
        if(context.Chevron is not null)context.Chevron.Angle=rotation.ValueAt(time);
    }
    private static Lab ColorOf(Brush? brush)
    {
        if(brush is null)return default;
        if(brush is not SolidColorBrush solid)throw new InvalidOperationException("Sidebar source palettes require solid colors.");
        return BleeckerColorMix.Premultiply((solid.Color.R,solid.Color.G,solid.Color.B,solid.Color.A/255d*solid.Opacity));
    }
    private static SolidColorBrush BrushOf(Lab lab){var color=BleeckerColorMix.EncodePremultiplied(lab);return new SolidColorBrush(Windows.UI.Color.FromArgb(255,color.R,color.G,color.B)){Opacity=color.Alpha};}
}
