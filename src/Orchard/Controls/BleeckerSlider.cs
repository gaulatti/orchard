using System.Diagnostics;
using System.Numerics;
using Microsoft.UI.Input;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ViewManagement;

namespace Orchard;

public sealed record BleeckerSliderValueRequestedEventArgs(double Value);

/// <summary>A consumer-controlled range input with original displayed values and native Windows input/automation.</summary>
public sealed partial class BleeckerSlider : Control
{
    private Grid? root;
    private SliderPanel? panel;
    private HeaderPanel? header;
    private InputPanel? input;
    private BleeckerContentText? labelText, valueText;
    private Border? track, fill, focusRing, focusOffset, thumbBorder;
    private Thumb? thumb;
    private Canvas? fillCanvas, thumbCanvas;
    private readonly List<BleeckerCssShadow> shadows = [];
    private readonly ResourceDictionary templates = new(){Source=new Uri("ms-appx:///Orchard/Themes/Slider.xaml")};
    private CompositionRoundedRectangleGeometry? clipGeometry;
    private CompositionGeometricClip? fillClip;
    private readonly List<(DependencyObject Owner, DependencyProperty Property, long Token)> observers = [];
    private readonly SolidColorBrush trackPaint = new();
    private readonly UISettings settings = new();
    private readonly AccessibilitySettings accessibility = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly BleeckerCssTransition<double> fillMotion = new((a,b,p) => a+(b-a)*p);
    private readonly record struct ShadowShape(double Blur,double Y,double Spread)
    {
        internal static ShadowShape Mix(ShadowShape a,ShadowShape b,double p)=>new(a.Blur+(b.Blur-a.Blur)*p,a.Y+(b.Y-a.Y)*p,a.Spread+(b.Spread-a.Spread)*p);
    }
    private readonly BleeckerCssTransition<ShadowShape> firstShadow=new(ShadowShape.Mix),secondShadow=new(ShadowShape.Mix);
    private BleeckerSliderModel.FillStyle fillStyle = new();
    private BleeckerSliderModel.Range previous = BleeckerSliderModel.Normalize(0,100,1,0);
    private double fillTarget = double.NaN, dragOrigin, dragDistance;
    private bool rendering, dragging, trackCaptured, thumbHovered, firstPaint=true;
    private bool CanAnimate=>IsLoaded&&Visibility==Visibility.Visible&&settings.AnimationsEnabled;
    public BleeckerSlider()
    {
        DefaultStyleKey = typeof(BleeckerSlider); UseLayoutRounding = false;
        Loaded += (_, _) => { if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged+=MotionChanged;Observe(); Render(); };
        Unloaded += (_, _) => { if(OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=MotionChanged;Stop(); Disconnect(); CancelTrack(); dragging = false; thumbHovered = false; input?.ClearCursor(); };
        RegisterPropertyChangedCallback(VisibilityProperty,(_,_)=>{if(Visibility!=Visibility.Visible)SnapMotion();});
        IsEnabledChanged += (_, _) => { if(!IsEnabled) { CancelTrack(); dragging=false; } Render(); };
        RegisterPropertyChangedCallback(TemplateProperty, (_, _) => { if(Template is null) Detach(); });
        foreach(var property in new[]{FlowDirectionProperty,IsTextScaleFactorEnabledProperty,LanguageProperty,ForegroundProperty,BorderBrushProperty,BorderThicknessProperty})
            RegisterPropertyChangedCallback(property, (_, _) => Render());
        foreach(var property in new[]{AutomationProperties.NameProperty,AutomationProperties.LabeledByProperty})
            RegisterPropertyChangedCallback(property, (_, _) => FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer());
        RegisterPropertyChangedCallback(StyleProperty,(_,_)=>FrameworkElementAutomationPeer.FromElement(this)?.InvalidatePeer());
        ActualThemeChanged += (_, _) => DispatcherQueue.TryEnqueue(() => { Observe(); Render(); });
    }
    private static DependencyProperty Property(string name,Type type,object value) => DependencyProperty.Register(name,type,typeof(BleeckerSlider),new PropertyMetadata(value,(owner,_)=>((BleeckerSlider)owner).Render()));
    public static readonly DependencyProperty MinProperty=Property(nameof(Min),typeof(double),0d);
    public static readonly DependencyProperty MaxProperty=Property(nameof(Max),typeof(double),100d);
    public static readonly DependencyProperty StepProperty=Property(nameof(Step),typeof(double),1d);
    public static readonly DependencyProperty ValueProperty=Property(nameof(Value),typeof(double),0d);
    public static readonly DependencyProperty LabelProperty=Property(nameof(Label),typeof(string),"");
    public static readonly DependencyProperty ShowValueProperty=Property(nameof(ShowValue),typeof(bool),false);
    public double Min{get=>(double)GetValue(MinProperty);set=>SetValue(MinProperty,value);}
    public double Max{get=>(double)GetValue(MaxProperty);set=>SetValue(MaxProperty,value);}
    public double Step{get=>(double)GetValue(StepProperty);set=>SetValue(StepProperty,value);}
    public double Value{get=>(double)GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public string Label{get=>(string)GetValue(LabelProperty);set=>SetValue(LabelProperty,value);}
    public bool ShowValue{get=>(bool)GetValue(ShowValueProperty);set=>SetValue(ShowValueProperty,value);}
    public event EventHandler<BleeckerSliderValueRequestedEventArgs>? ValueRequested;
    private BleeckerSliderModel.Range Range=>BleeckerSliderModel.Normalize(Min,Max,Step,Value);
    private bool HasHeader=>!string.IsNullOrEmpty(Label)||ShowValue;
    protected override void OnApplyTemplate()
    {
        Detach();base.OnApplyTemplate();root=GetTemplateChild("PART_Root") as Grid;
        if(root is null){if(Template is null)return;throw new InvalidOperationException("Slider requires PART_Root.");}
        labelText=new("SliderLabel","BleeckerPrimaryMediumFontFamily",14,20,"BleeckerTextPrimaryBrush",medium:true);
        valueText=new("SliderValue","BleeckerPrimaryFontFamily",14,20,"BleeckerTextSecondaryBrush");Typography.SetNumeralAlignment(valueText.Text,FontNumeralAlignment.Tabular);
        header=new HeaderPanel(labelText,valueText){Name="SliderHeader",UseLayoutRounding=false};
        input=new InputPanel{Height=20,Name="SliderInput",FlowDirection=FlowDirection.LeftToRight,UseLayoutRounding=false};
        focusRing=new Border{Name="SliderFocusRing",BorderThickness=new Thickness(2),CornerRadius=new CornerRadius(8),Height=16,IsHitTestVisible=false,UseLayoutRounding=false};
        focusOffset=new Border{Name="SliderFocusOffset",BorderThickness=new Thickness(2),CornerRadius=new CornerRadius(6),Height=12,IsHitTestVisible=false,UseLayoutRounding=false};
        track=new Border{Name="SliderTrack",Height=8,CornerRadius=new CornerRadius(4),VerticalAlignment=VerticalAlignment.Center,UseLayoutRounding=false};
        fillCanvas=new Canvas{Height=8,UseLayoutRounding=false};fill=new Border{Name="SliderFill",Height=8,CornerRadius=new CornerRadius(4),UseLayoutRounding=false};fillCanvas.Children.Add(fill);track.Child=fillCanvas;
        thumbCanvas=new Canvas{Height=20,UseLayoutRounding=false};thumb=new Thumb{Name="SliderThumb",Width=20,Height=20,IsTabStop=false,UseLayoutRounding=false,Template=(ControlTemplate)templates["SliderThumbTemplate"]};
        AutomationProperties.SetAccessibilityView(thumb,AccessibilityView.Raw);thumbCanvas.Children.Add(thumb);input.Children.Add(focusRing);input.Children.Add(focusOffset);input.Children.Add(track);input.Children.Add(thumbCanvas);
        panel=new SliderPanel(this,header,input){UseLayoutRounding=false};root.Children.Add(panel);
        track.PointerPressed+=TrackPressed;input.PointerMoved+=TrackMoved;input.PointerReleased+=TrackReleased;input.PointerCaptureLost+=TrackCaptureLost;
        thumb.DragStarted+=DragStarted;thumb.DragDelta+=DragDelta;thumb.DragCompleted+=DragCompleted;
        thumb.PointerEntered+=ThumbEntered;thumb.PointerExited+=ThumbExited;thumb.ApplyTemplate();
        var thumbRoot=(Grid)VisualTreeHelper.GetChild(thumb,0);thumbBorder=(Border)thumbRoot.FindName("SliderThumbBorder");
        for(var index=0;index<2;index++)shadows.Add(new(this,thumbBorder,(Grid)thumbRoot.FindName("SliderThumbShadow"+index),(Microsoft.UI.Xaml.Shapes.Path)thumbRoot.FindName("SliderThumbMask"+index),(Microsoft.UI.Xaml.Shapes.Path)thumbRoot.FindName("SliderThumbExterior"+index)));
        input.SizeChanged+=InputSized;input.Loaded+=InputLoaded;Observe();Render();
    }
    private void Render()
    {
        var range=Range;var old=previous;previous=range;
        if(FrameworkElementAutomationPeer.FromElement(this) is {} peer){if(old.Value!=range.Value)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,old.Value,range.Value);if(old.Minimum!=range.Minimum)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty,old.Minimum,range.Minimum);if(old.Maximum!=range.Maximum)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty,old.Maximum,range.Maximum);if(old.Step!=range.Step)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.SmallChangeProperty,old.Step,range.Step);var oldLarge=Math.Max(old.Step,old.Maximum/10-old.Minimum/10);var nextLarge=Math.Max(range.Step,range.Maximum/10-range.Minimum/10);if(oldLarge!=nextLarge)peer.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.LargeChangeProperty,oldLarge,nextLarge);peer.InvalidatePeer();}
        fillStyle.Apply(Min,Max,Value);
        if(panel is null||header is null||input is null||labelText is null||valueText is null||thumb is null||thumbBorder is null||root is null)return;
        labelText.Set(Label,null);valueText.Set(BleeckerNumberText.Format(Value),null);header.Visibility=HasHeader?Visibility.Visible:Visibility.Collapsed;valueText.Visibility=ShowValue?Visibility.Visible:Visibility.Collapsed;
        header.FlowDirection=FlowDirection;foreach(var slot in new[]{labelText,valueText}){slot.Text.IsTextScaleFactorEnabled=IsTextScaleFactorEnabled;slot.Text.Language=Language;}
        labelText.Text.Foreground=Foreground;valueText.Text.Foreground=Palette(ActualTheme==ElementTheme.Dark?"PART_ValueDarkSource":"PART_ValueLightSource");
        var sand=Palette("PART_SandSource") as SolidColorBrush??throw new InvalidOperationException("Slider's sand token requires a solid color brush.");trackPaint.Color=sand.Color;trackPaint.Opacity=sand.Opacity*(accessibility.HighContrast?1:ActualTheme==ElementTheme.Dark?.3:.2);track!.Background=trackPaint;
        fill!.Background=Palette("PART_AccentSource");thumbBorder.Background=Palette("PART_ThumbSource");thumbBorder.BorderBrush=BorderBrush;thumbBorder.BorderThickness=BorderThickness;
        focusRing!.BorderBrush=Palette("PART_AccentSource");focusOffset!.BorderBrush=Palette("PART_OffsetSource");thumb.Opacity=IsEnabled?1:.5;
        input.UpdateCursor(IsEnabled);UpdateFocus();PaintShadow();panel.InvalidateMeasure();LayoutInput(true);
    }
    private Brush Palette(string name)=>((TextBlock)root!.FindName(name)).Foreground;
    private void InputLoaded(object sender,RoutedEventArgs args){LayoutInput(false);firstPaint=false;}
    private void MotionChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>DispatcherQueue.TryEnqueue(SnapMotion);
    private void SnapMotion(){if(double.IsFinite(fillTarget))fillMotion.Retarget(fillTarget,clock.Elapsed.TotalMilliseconds,0,false);PaintShadow(false);LayoutInput(false);}
    private void InputSized(object sender,SizeChangedEventArgs args)=>LayoutInput(true);
    private void LayoutInput(bool animate)
    {
        if(input is null||fill is null||thumb is null||track is null||focusRing is null||focusOffset is null)return;
        var width=input.ActualWidth;if(!double.IsFinite(width))return;var target=fillStyle.Width(width);
        if(target!=fillTarget){fillTarget=target;fillMotion.Retarget(target,clock.Elapsed.TotalMilliseconds,BleeckerTokens.Durations["control"],animate&&!firstPaint&&CanAnimate);}
        fill.Width=Math.Max(0,fillMotion.ValueAt(clock.Elapsed.TotalMilliseconds));Canvas.SetLeft(fill,FlowDirection==FlowDirection.RightToLeft?width-fill.Width:0);
        Canvas.SetLeft(thumb,BleeckerSliderModel.ThumbLeft(width,Range,FlowDirection==FlowDirection.RightToLeft));
        focusRing.Width=width+8;focusOffset.Width=width+4;input.InvalidateArrange();
        if(clipGeometry is null){var compositor=ElementCompositionPreview.GetElementVisual(track).Compositor;clipGeometry=compositor.CreateRoundedRectangleGeometry();clipGeometry.CornerRadius=new Vector2(4);fillClip=compositor.CreateGeometricClip(clipGeometry);ElementCompositionPreview.GetElementVisual(fillCanvas!).Clip=fillClip;}clipGeometry.Size=new Vector2((float)width,8);
        UpdateClock();
    }
    private void Frame(object? sender,object args){LayoutInput(false);ApplyShadowFrame();UpdateClock();}
    private void UpdateClock(){if(!IsLoaded||Visibility!=Visibility.Visible){Stop();return;}var now=clock.Elapsed.TotalMilliseconds;if(fillMotion.IsRunning(now)||firstShadow.IsRunning(now)||secondShadow.IsRunning(now)){if(!rendering){CompositionTarget.Rendering+=Frame;rendering=true;}}else Stop();}
    private void Stop(){if(rendering)CompositionTarget.Rendering-=Frame;rendering=false;}
    private void Observe()
    {
        Disconnect();if(!IsLoaded||root is null)return;
        foreach(var name in new[]{"PART_ValueLightSource","PART_ValueDarkSource","PART_SandSource","PART_AccentSource","PART_ThumbSource","PART_OffsetSource"})
        {var text=(TextBlock)root.FindName(name);observers.Add((text,TextBlock.ForegroundProperty,text.RegisterPropertyChangedCallback(TextBlock.ForegroundProperty,(_,_)=>{Observe();Render();})));if(text.Foreground is SolidColorBrush brush)foreach(var property in new[]{SolidColorBrush.ColorProperty,Brush.OpacityProperty})observers.Add((brush,property,brush.RegisterPropertyChangedCallback(property,(_,_)=>Render())));}
    }
    private void Disconnect(){foreach(var(owner,property,token)in observers)owner.UnregisterPropertyChangedCallback(property,token);observers.Clear();}
    private void PaintShadow(bool animate=true)
    {
        if(shadows.Count!=2)return;
        var now=clock.Elapsed.TotalMilliseconds;var duration=BleeckerTokens.Durations["control"];firstShadow.Retarget(thumbHovered?new(6,4,-1):new(3,1,0),now,duration,animate&&!firstPaint&&CanAnimate);secondShadow.Retarget(thumbHovered?new(4,2,-2):new(2,1,-1),now,duration,animate&&!firstPaint&&CanAnimate);ApplyShadowFrame();UpdateClock();
    }
    private void ApplyShadowFrame(){if(shadows.Count!=2)return;var a=firstShadow.ValueAt(clock.Elapsed.TotalMilliseconds);var b=secondShadow.ValueAt(clock.Elapsed.TotalMilliseconds);shadows[0].Set(new((float)a.Blur,(float)a.Y,(float)a.Spread,.1f,0,0,0));shadows[1].Set(new((float)b.Blur,(float)b.Y,(float)b.Spread,.1f,0,0,0));}
    private void ThumbEntered(object sender,PointerRoutedEventArgs args){thumbHovered=true;PaintShadow();}
    private void ThumbExited(object sender,PointerRoutedEventArgs args){thumbHovered=false;PaintShadow();}
    private void Request(double value){if(IsLoaded&&IsEnabled&&value!=Range.Value)ValueRequested?.Invoke(this,new(value));}
    private double At(double x)
    {
        var travel=Math.Max(0,(input?.ActualWidth??0)-20);var fraction=travel==0?0:Math.Clamp((x-10)/travel,0,1);if(FlowDirection==FlowDirection.RightToLeft)fraction=1-fraction;
        var range=Range;return BleeckerSliderModel.Normalize(Min,Max,Step,range.Minimum*(1-fraction)+range.Maximum*fraction).Value;
    }
    private void TrackPressed(object sender,PointerRoutedEventArgs args)
    {
        if(!IsEnabled||input is null||(args.Pointer.PointerDeviceType==PointerDeviceType.Mouse&&!args.GetCurrentPoint(input).Properties.IsLeftButtonPressed))return;
        Focus(FocusState.Pointer);trackCaptured=input.CapturePointer(args.Pointer);if(trackCaptured)Request(At(args.GetCurrentPoint(input).Position.X));args.Handled=true;
    }
    private void TrackMoved(object sender,PointerRoutedEventArgs args){if(trackCaptured&&input is not null){Request(At(args.GetCurrentPoint(input).Position.X));args.Handled=true;}}
    private void TrackReleased(object sender,PointerRoutedEventArgs args){if(trackCaptured){input?.ReleasePointerCapture(args.Pointer);trackCaptured=false;args.Handled=true;}}
    private void TrackCaptureLost(object sender,PointerRoutedEventArgs args)=>trackCaptured=false;
    private void CancelTrack(){if(trackCaptured)input?.ReleasePointerCaptures();trackCaptured=false;}
    private void DragStarted(object sender,DragStartedEventArgs args){if(!IsEnabled)return;Focus(FocusState.Pointer);dragging=true;dragOrigin=BleeckerSliderModel.ThumbLeft(input?.ActualWidth??0,Range,FlowDirection==FlowDirection.RightToLeft)+10;dragDistance=0;}
    private void DragDelta(object sender,DragDeltaEventArgs args){if(!dragging||!IsEnabled)return;dragDistance+=args.HorizontalChange;Request(At(dragOrigin+dragDistance));}
    private void DragCompleted(object sender,DragCompletedEventArgs args)=>dragging=false;
    protected override void OnKeyDown(KeyRoutedEventArgs args)
    {
        base.OnKeyDown(args);if(args.Handled||!IsEnabled)return;
        var key=args.Key switch{VirtualKey.Left=>"ArrowLeft",VirtualKey.Right=>"ArrowRight",VirtualKey.Up=>"ArrowUp",VirtualKey.Down=>"ArrowDown",VirtualKey.PageUp=>"PageUp",VirtualKey.PageDown=>"PageDown",VirtualKey.Home=>"Home",VirtualKey.End=>"End",_=>null};
        if(key is null)return;Request(BleeckerSliderModel.Key(Range,key,FlowDirection==FlowDirection.RightToLeft));args.Handled=true;
    }
    protected override void OnGotFocus(RoutedEventArgs args){base.OnGotFocus(args);UpdateFocus();}
    protected override void OnLostFocus(RoutedEventArgs args){base.OnLostFocus(args);UpdateFocus();}
    private void UpdateFocus(){if(focusRing is null||focusOffset is null)return;focusRing.Visibility=focusOffset.Visibility=IsEnabled&&FocusState==FocusState.Keyboard?Visibility.Visible:Visibility.Collapsed;}
    private void Detach()
    {
        Stop();Disconnect();CancelTrack();dragging=false;thumbHovered=false;foreach(var shadow in shadows)shadow.Dispose();shadows.Clear();
        if(thumb is not null){thumb.DragStarted-=DragStarted;thumb.DragDelta-=DragDelta;thumb.DragCompleted-=DragCompleted;thumb.PointerEntered-=ThumbEntered;thumb.PointerExited-=ThumbExited;}
        if(track is not null)track.PointerPressed-=TrackPressed;
        if(input is not null){input.PointerMoved-=TrackMoved;input.PointerReleased-=TrackReleased;input.PointerCaptureLost-=TrackCaptureLost;input.SizeChanged-=InputSized;input.Loaded-=InputLoaded;input.Dispose();}
        if(fillCanvas is not null)ElementCompositionPreview.GetElementVisual(fillCanvas).Clip=null;fillClip?.Dispose();clipGeometry?.Dispose();fillClip=null;clipGeometry=null;
        labelText?.Release();valueText?.Release();if(root is not null&&panel is not null)root.Children.Remove(panel);root=null;panel=null;header=null;input=null;labelText=valueText=null;thumb=null;thumbBorder=null;track=fill=focusRing=focusOffset=null;fillCanvas=thumbCanvas=null;fillStyle=new();fillTarget=double.NaN;firstPaint=true;
    }
    protected override AutomationPeer OnCreateAutomationPeer()=>new SliderPeer(this);
    private bool HasNameOverride()
    {
        if(ReadLocalValue(AutomationProperties.NameProperty)!=DependencyProperty.UnsetValue)return true;
        for(var style=Style;style is not null;style=style.BasedOn)
            if(style.Setters.OfType<Setter>().Any(setter=>setter.Property==AutomationProperties.NameProperty))return true;
        return false;
    }
    private sealed class SliderPeer(BleeckerSlider owner):FrameworkElementAutomationPeer(owner),IRangeValueProvider
    {
        protected override string GetClassNameCore()=>nameof(BleeckerSlider);
        protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Slider;
        protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.RangeValue?this:base.GetPatternCore(pattern);
        protected override IList<AutomationPeer>? GetChildrenCore()=>null;
        protected override Point GetClickablePointCore()=>owner.thumb is not null?FrameworkElementAutomationPeer.CreatePeerForElement(owner.thumb).GetClickablePoint():base.GetClickablePointCore();
        protected override string GetNameCore(){if(owner.HasNameOverride())return AutomationProperties.GetName(owner);var name=base.GetNameCore();return name.Length>0||AutomationProperties.GetLabeledBy(owner)is not null?name:owner.Label??"";}
        public bool IsReadOnly=>false;public double Minimum=>owner.Range.Minimum;public double Maximum=>owner.Range.Maximum;public double Value=>owner.Range.Value;
        public double SmallChange=>owner.Range.Step;public double LargeChange=>Math.Max(SmallChange,Maximum/10-Minimum/10);
        public void SetValue(double value){if(!owner.IsLoaded)throw new ElementNotAvailableException();if(!owner.IsEnabled)throw new ElementNotEnabledException();if(!double.IsFinite(value)||value<Minimum||value>Maximum)throw new ArgumentOutOfRangeException(nameof(value));owner.Request(BleeckerSliderModel.Normalize(owner.Min,owner.Max,owner.Step,value).Value);}
    }
    private sealed partial class InputPanel:Grid,IDisposable
    {
        private readonly InputSystemCursor hand=InputSystemCursor.Create(InputSystemCursorShape.Hand),prohibited=InputSystemCursor.Create(InputSystemCursorShape.UniversalNo);
        internal void UpdateCursor(bool enabled)=>ProtectedCursor=enabled?hand:prohibited;
        internal void ClearCursor()=>ProtectedCursor=null;
        public void Dispose(){ProtectedCursor=null;hand.Dispose();prohibited.Dispose();}
        protected override Size MeasureOverride(Size available){var width=double.IsFinite(available.Width)?available.Width:129;foreach(var child in Children)child.Measure(new Size(width,20));return new(width,20);}
        protected override Size ArrangeOverride(Size final)
        {
            foreach(var child in Children){if(child is Border border&&border.Name is "SliderFocusRing" or "SliderFocusOffset")child.Arrange(new Rect((final.Width-border.Width)/2,(20-border.Height)/2,border.Width,border.Height));else child.Arrange(new Rect(0,child is Border?6:0,final.Width,child is Border?8:20));}return final;
        }
    }
    private sealed partial class HeaderPanel(BleeckerContentText left,BleeckerContentText right):Panel
    {
        private double leftWidth,rightWidth;
        private void Ensure(){if(Children.Count==0){Children.Add(left);Children.Add(right);}}
        protected override Size MeasureOverride(Size available)
        {
            Ensure();left.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));right.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));leftWidth=left.NaturalWidth;rightWidth=right.Visibility==Visibility.Visible?right.NaturalWidth:0;
            var width=double.IsFinite(available.Width)?available.Width:leftWidth+rightWidth;var overflow=leftWidth+rightWidth-width;
            if(overflow>0){var total=leftWidth+rightWidth;leftWidth=Math.Max(left.MinimumWidth,leftWidth-overflow*leftWidth/total);rightWidth=Math.Max(right.Visibility==Visibility.Visible?right.MinimumWidth:0,rightWidth-overflow*rightWidth/total);if(leftWidth+rightWidth>width){if(leftWidth==left.MinimumWidth)rightWidth=Math.Max(right.Visibility==Visibility.Visible?right.MinimumWidth:0,width-leftWidth);else leftWidth=Math.Max(left.MinimumWidth,width-rightWidth);}}
            left.Measure(new Size(leftWidth,double.PositiveInfinity));right.Measure(new Size(rightWidth,double.PositiveInfinity));return new(width,Math.Max(left.DesiredSize.Height,right.Visibility==Visibility.Visible?right.DesiredSize.Height:0));
        }
        protected override Size ArrangeOverride(Size final){left.Arrange(new Rect(0,(final.Height-left.DesiredSize.Height)/2,leftWidth,left.DesiredSize.Height));right.Arrange(new Rect(Math.Max(leftWidth,final.Width-rightWidth),(final.Height-right.DesiredSize.Height)/2,rightWidth,right.DesiredSize.Height));return final;}
    }
    private sealed partial class SliderPanel(BleeckerSlider owner,HeaderPanel header,InputPanel input):Panel
    {
        private void Ensure(){if(Children.Count==0){Children.Add(header);Children.Add(input);}}
        protected override Size MeasureOverride(Size available)
        {
            Ensure();header.Measure(new Size(available.Width,double.PositiveInfinity));var width=double.IsFinite(available.Width)?available.Width:Math.Max(129,header.DesiredSize.Width);header.Measure(new Size(width,double.PositiveInfinity));input.Measure(new Size(width,20));return new(width,20+(owner.HasHeader?header.DesiredSize.Height+8:0));
        }
        protected override Size ArrangeOverride(Size final){var height=owner.HasHeader?header.DesiredSize.Height:0;header.Arrange(new Rect(0,0,final.Width,height));input.Arrange(new Rect(0,owner.HasHeader?height+8:0,final.Width,20));return final;}
    }
}
