using System.Numerics;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Input;
using Windows.Foundation;

namespace Orchard;

public abstract partial class BleeckerEdgeDialog
{
    private OverlayPanel? overlay;
    private Grid? backdrop,handle;
    private Border? blurHost;
    private FocusSurface? focusSurface;
    private BleeckerOverlaySurface? paint;
    private ScrollView? viewport;
    private InnerPanel? inner;
    private HeaderPanel? header;
    private BodyPanel? body;
    private BleeckerContentText? titleText,descriptionText,contentSlot;
    private BleeckerDialogCloseButton? closeButton;
    private ResourceDictionary? palette;
    private readonly TranslateTransform surfaceTransform=new();
    private readonly List<IDisposable> effects=[];
    private readonly BleeckerDrawerDismissGesture touch=new();
    private uint? touchId;
    private SpriteVisual? blurVisual;
    private bool measureQueued;
    private FrameworkElement? contentRoot;
    private readonly List<(DependencyProperty Property,long Token)> contentRootTokens=[];
    private BleeckerEdgeDialogModel.Layout layout;
    private string Prefix=>drawer?"Drawer":"Sheet";
    private bool HasTitle=>!string.IsNullOrEmpty(Title);
    private bool HasDescription=>!string.IsNullOrEmpty(Description);
    private void QueueRemeasure()
    {
        if(measureQueued||overlay is not{} mounted)return;measureQueued=true;
        if(!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>{measureQueued=false;if(ReferenceEquals(overlay,mounted))mounted.InvalidateMeasure();}))throw new InvalidOperationException("Drawer/Sheet could not queue content measurement.");
    }
    private void ObserveContentRoot()
    {
        var presenter=contentSlot?.Children.OfType<ContentPresenter>().Single();
        var next=presenter is not null&&presenter.Visibility==Visibility.Visible&&VisualTreeHelper.GetChildrenCount(presenter)>0?VisualTreeHelper.GetChild(presenter,0)as FrameworkElement:null;
        if(ReferenceEquals(next,contentRoot))return;DisconnectContentRoot();contentRoot=next;
        if(next is null)return;next.SizeChanged+=ContentSizeChanged;
        foreach(var p in new[]{WidthProperty,HeightProperty,MinWidthProperty,MaxWidthProperty,MinHeightProperty,MaxHeightProperty,MarginProperty,VisibilityProperty})contentRootTokens.Add((p,next.RegisterPropertyChangedCallback(p,(_,_)=>QueueRemeasure())));
        QueueRemeasure();
    }
    private void ContentSizeChanged(object sender,SizeChangedEventArgs args)=>QueueRemeasure();
    private void DisconnectContentRoot(){if(contentRoot is not null){contentRoot.SizeChanged-=ContentSizeChanged;foreach(var(p,token)in contentRootTokens)contentRoot.UnregisterPropertyChangedCallback(p,token);}contentRootTokens.Clear();contentRoot=null;}
    private void MountPresentation()
    {
        if(registration is{IsAttached:true}){if(!registration.Layer.Active)registration.Host.Activate(registration);return;}
        ClearPresentation();palette=new(){Source=new Uri("ms-appx:///Orchard/Themes/EdgeDialog.xaml")};
        titleText=new(Prefix+"Title","BleeckerPrimaryMediumFontFamily",drawer?18:24,drawer?28:32,"BleeckerTextPrimaryBrush",medium:true){ConstrainLayoutWidth=true};
        // The pinned global h2 rule retains -.02em tracking after text-lg/2xl.
        titleText.Text.CharacterSpacing=-20;
        descriptionText=new(Prefix+"Description","BleeckerSecondaryFontFamily",14,22.75,"BleeckerTextSecondaryBrush"){ConstrainLayoutWidth=true};
        contentSlot=new(Prefix+"Content","BleeckerPrimaryFontFamily",16,24,"BleeckerTextPrimaryBrush",stretchContent:true);
        contentSlot.SetBinding(DataContextProperty,new Binding{Source=this,Path=new PropertyPath(nameof(DataContext))});
        body=new(this){Name=Prefix+"BodyContent",UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};body.Children.Add(contentSlot);
        viewport=new(){Name=Prefix+"Body",IsTabStop=false,UseLayoutRounding=false,Content=body,ContentOrientation=ScrollingContentOrientation.Vertical,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Top,HorizontalScrollMode=ScrollingScrollMode.Disabled,HorizontalScrollBarVisibility=ScrollingScrollBarVisibility.Hidden,VerticalScrollMode=ScrollingScrollMode.Enabled,VerticalScrollBarVisibility=ScrollingScrollBarVisibility.Auto,ZoomMode=ScrollingZoomMode.Disabled,Style=BleeckerModalViewport.CreateStyle()};
        header=new(this){Name=Prefix+"Header",UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};header.Children.Add(titleText);header.Children.Add(descriptionText);
        inner=new(this){Name=Prefix+"Inner",UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};inner.Children.Add(header);inner.Children.Add(viewport);
        if(drawer)
        {
            handle=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' UseLayoutRounding='False' IsHitTestVisible='False' AutomationProperties.AccessibilityView='Raw'><Canvas Width='24' Height='24' VerticalAlignment='Top' Margin='0,12,0,4'><Line X1='6' Y1='12' X2='18' Y2='12' Stroke='{ThemeResource DrawerHandleBrush}' StrokeThickness='3' StrokeStartLineCap='Round' StrokeEndLineCap='Round'/></Canvas></Grid>");handle.Name="DrawerHandle";inner.Children.Add(handle);
        }
        else
        {
            closeButton=new(){Name="SheetClose",UseLayoutRounding=false,MinWidth=0};AutomationProperties.SetName(closeButton,"Close");closeButton.Click+=CloseClick;header.Children.Add(closeButton);
        }
        paint=new(){Name=Prefix+"PaintSurface",Content=inner,UseLayoutRounding=false};
        focusSurface=new(this){Name=Prefix+"Surface",Content=paint,IsTabStop=true,UseSystemFocusVisuals=false,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch,UseLayoutRounding=false,RenderTransform=surfaceTransform,TabFocusNavigation=KeyboardNavigationMode.Cycle};
        AutomationProperties.SetIsDialog(focusSurface,true);
        if(drawer){focusSurface.AddHandler(PointerPressedEvent,new PointerEventHandler(TouchPressed),true);focusSurface.AddHandler(PointerReleasedEvent,new PointerEventHandler(TouchReleased),true);focusSurface.AddHandler(PointerCanceledEvent,new PointerEventHandler(TouchCanceled),true);}
        backdrop=new(){Name=Prefix+"Backdrop",Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent),UseLayoutRounding=false};blurHost=new();backdrop.Children.Add(blurHost);backdrop.Children.Add((Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource AlertDialogBackdropBrush}'/>"));backdrop.Tapped+=BackdropTapped;
        overlay=new(this){Name=Prefix+"Overlay",UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};overlay.Children.Add(backdrop);overlay.Children.Add(focusSurface);
        RenderContent();var host=BleeckerModalHost.Require(this);registration=host.Attach(this,focusSurface,overlay,RequestClose);host.Activate(registration);
    }
    private void TouchPressed(object sender,PointerRoutedEventArgs args){if(args.Pointer.PointerDeviceType!=PointerDeviceType.Touch||touchId is not null||!IsOpen)return;touchId=args.Pointer.PointerId;touch.Start(args.GetCurrentPoint(overlay).Position.Y);}
    private void TouchReleased(object sender,PointerRoutedEventArgs args){if(args.Pointer.PointerDeviceType!=PointerDeviceType.Touch||touchId!=args.Pointer.PointerId)return;touchId=null;if(touch.End(args.GetCurrentPoint(overlay).Position.Y))RequestClose();}
    private void TouchCanceled(object sender,PointerRoutedEventArgs args){if(touchId==args.Pointer.PointerId){touchId=null;touch.Reset();}}
    private void CloseClick(object sender,RoutedEventArgs args)=>RequestClose();
    private void BackdropTapped(object sender,TappedRoutedEventArgs args){RequestClose();args.Handled=true;}
    private void RenderContent()
    {
        if(paint is null||focusSurface is null||titleText is null||descriptionText is null||contentSlot is null||viewport is null||palette is null)return;
        var style=SurfaceStyle??(Style)palette[drawer?"BleeckerDrawerSurfaceStyle":"BleeckerSheet"+Side+"SurfaceStyle"];if(!ReferenceEquals(paint.Style,style))paint.Style=style;
        titleText.Set(Title,null);descriptionText.Set(Description,null);contentSlot.Set(Content,ContentTemplate,selector:ContentTemplateSelector);
        titleText.Visibility=HasTitle?Visibility.Visible:Visibility.Collapsed;descriptionText.Visibility=HasDescription?Visibility.Visible:Visibility.Collapsed;
        if(header is not null)header.Visibility=drawer&&!HasTitle&&!HasDescription?Visibility.Collapsed:Visibility.Visible;
        foreach(var slot in new[]{titleText,descriptionText,contentSlot}){slot.FlowDirection=slot.Text.FlowDirection=FlowDirection;slot.Text.Language=Language;slot.Text.IsTextScaleFactorEnabled=IsTextScaleFactorEnabled;}
        focusSurface.RequestedTheme=ActualTheme;focusSurface.IsEnabled=IsEnabled;viewport.FlowDirection=paint.FlowDirection=FlowDirection;
        viewport.VerticalScrollMode=ScrollContent?ScrollingScrollMode.Enabled:ScrollingScrollMode.Disabled;viewport.VerticalScrollBarVisibility=ScrollContent?ScrollingScrollBarVisibility.Auto:ScrollingScrollBarVisibility.Hidden;
        if(closeButton is not null)closeButton.IsEnabled=IsEnabled&&CloseCommand?.CanExecute(CloseCommandParameter)!=false;
        AutomationProperties.SetName(focusSurface,HasTitle?Title!:"Dialog");AutomationProperties.SetLabeledBy(focusSurface,HasTitle?titleText.Text:null);AutomationProperties.SetHelpText(focusSurface,HasDescription?Description!:"");
        FrameworkElementAutomationPeer.FromElement(focusSurface)?.InvalidatePeer();overlay?.InvalidateMeasure();
    }
    private Size MeasurePresentation(Size available)
    {
        if(titleText is null||descriptionText is null||contentSlot is null||paint is null||focusSurface is null||inner is null)return available;
        BleeckerEdgeDialogModel.TextMetrics Text(double width){titleText.Measure(new Size(width,double.PositiveInfinity));descriptionText.Measure(new Size(width,double.PositiveInfinity));return new(HasTitle?titleText.NaturalHeight:0,HasDescription?descriptionText.NaturalHeight:0);}
        double Content(double width){contentSlot.Measure(new Size(width,double.PositiveInfinity));return contentSlot.DesiredSize.Height;}
        var next=drawer?BleeckerEdgeDialogModel.Drawer(available.Width,available.Height,Text,Content,HasTitle,HasDescription,double.IsPositiveInfinity(paint.MaxHeight)?null:paint.MaxHeight):BleeckerEdgeDialogModel.Sheet(available.Width,available.Height,Side,Text,Content,HasTitle,HasDescription,ScrollContent,!ScrollContent,paint.MaxWidth);
        if(layout!=next){layout=next;inner.InvalidateMeasure();header?.InvalidateMeasure();body?.InvalidateMeasure();}
        focusSurface.Measure(new Size(layout.Surface.Width,layout.Surface.Height));UpdateBlur(available);return available;
    }
    private void ApplyMotion((double Surface,double Backdrop) value)
    {
        if(focusSurface is null||backdrop is null)return;var vertical=Side is BleeckerEdgeSide.Left or BleeckerEdgeSide.Right;var offset=(1-value.Surface)*(vertical?layout.Surface.Width:layout.Surface.Height)*(Side is BleeckerEdgeSide.Left or BleeckerEdgeSide.Top?-1:1);surfaceTransform.X=vertical?offset:0;surfaceTransform.Y=vertical?0:offset;backdrop.Opacity=value.Backdrop;
    }
    private void UpdateBlur(Size size)
    {
        if(blurHost is null)return;if(blurVisual is null){var compositor=ElementCompositionPreview.GetElementVisual(blurHost).Compositor;var source=compositor.CreateBackdropBrush();using var blur=new GaussianBlurEffect{BlurAmount=2,BorderMode=EffectBorderMode.Hard,Optimization=EffectOptimization.Quality,Source=new CompositionEffectSourceParameter("backdrop")};var factory=compositor.CreateEffectFactory(blur);var brush=factory.CreateBrush();brush.SetSourceParameter("backdrop",source);blurVisual=compositor.CreateSpriteVisual();blurVisual.Brush=brush;effects.Add(source);effects.Add(factory);effects.Add(brush);effects.Add(blurVisual);ElementCompositionPreview.SetElementChildVisual(blurHost,blurVisual);}blurVisual.Size=new Vector2((float)size.Width,(float)size.Height);
    }
    private void ClearPresentation()
    {
        DisconnectContentRoot();touchId=null;touch.Reset();if(backdrop is not null)backdrop.Tapped-=BackdropTapped;if(closeButton is not null)closeButton.Click-=CloseClick;if(blurHost is not null)ElementCompositionPreview.SetElementChildVisual(blurHost,null);
        if(drawer&&focusSurface is not null){focusSurface.RemoveHandler(PointerPressedEvent,new PointerEventHandler(TouchPressed));focusSurface.RemoveHandler(PointerReleasedEvent,new PointerEventHandler(TouchReleased));focusSurface.RemoveHandler(PointerCanceledEvent,new PointerEventHandler(TouchCanceled));}
        for(var i=effects.Count-1;i>=0;i--)effects[i].Dispose();effects.Clear();blurVisual=null;contentSlot?.Release();titleText?.Release();descriptionText?.Release();
        if(viewport is not null)viewport.Content=null;if(paint is not null)paint.Content=null;if(focusSurface is not null)focusSurface.Content=null;overlay?.Children.Clear();inner?.Children.Clear();header?.Children.Clear();body?.Children.Clear();
        overlay=null;backdrop=null;blurHost=null;handle=null;focusSurface=null;paint=null;viewport=null;inner=null;header=null;body=null;titleText=null;descriptionText=null;contentSlot=null;closeButton=null;palette=null;
    }
    private static void Place(FrameworkElement? element,BleeckerEdgeDialogModel.Rect? box,double x,double y){if(element is null||box is not{} b)return;element.Measure(new Size(b.Width,b.Height));element.Arrange(new Rect(b.X-x,b.Y-y,b.Width,b.Height));}
    private sealed partial class OverlayPanel(BleeckerEdgeDialog owner):Panel
    {
        protected override Size MeasureOverride(Size size)=>owner.MeasurePresentation(size);
        protected override Size ArrangeOverride(Size size){owner.backdrop?.Arrange(new Rect(0,0,size.Width,size.Height));Place(owner.focusSurface,owner.layout.Surface,0,0);owner.ApplyMotion(owner.motion?.Sample(owner.clock.Elapsed.TotalMilliseconds)??(0,0));return size;}
    }
    private sealed partial class InnerPanel(BleeckerEdgeDialog owner):Panel
    {
        protected override Size MeasureOverride(Size size){var l=owner.layout;owner.header?.Measure(new Size(l.Header?.Width??0,l.Header?.Height??0));owner.handle?.Measure(new Size(l.Handle?.Width??0,l.Handle?.Height??0));owner.viewport?.Measure(new Size(l.Body.Width,l.Body.Height));return new(l.Inner.Width,l.Inner.Height);}
        protected override Size ArrangeOverride(Size size){var l=owner.layout;Place(owner.header,l.Header,l.Inner.X,l.Inner.Y);Place(owner.handle,l.Handle,l.Inner.X,l.Inner.Y);Place(owner.viewport,l.Body,l.Inner.X,l.Inner.Y);if(!owner.drawer)Clip=new RectangleGeometry{Rect=new Rect(0,0,size.Width,size.Height)};return size;}
    }
    private sealed partial class HeaderPanel(BleeckerEdgeDialog owner):Panel
    {
        protected override Size MeasureOverride(Size size){var l=owner.layout;owner.titleText?.Measure(new Size(l.Title?.Width??0,l.Title?.Height??0));owner.descriptionText?.Measure(new Size(l.Description?.Width??0,l.Description?.Height??0));if(owner.closeButton is not null){owner.closeButton.Width=l.Close?.Width??36;owner.closeButton.Measure(new Size(l.Close?.Width??36,36));}return new(l.Header?.Width??0,l.Header?.Height??0);}
        protected override Size ArrangeOverride(Size size){var l=owner.layout;Place(owner.titleText,l.Title,l.Header?.X??0,l.Header?.Y??0);Place(owner.descriptionText,l.Description,l.Header?.X??0,l.Header?.Y??0);Place(owner.closeButton,l.Close,l.Header?.X??0,l.Header?.Y??0);return size;}
    }
    private sealed partial class BodyPanel(BleeckerEdgeDialog owner):Panel
    {
        protected override Size MeasureOverride(Size size){var l=owner.layout;owner.contentSlot?.Measure(new Size(l.Content.Width,l.Content.Height));return new(Math.Max(0,l.Body.Width-(l.VerticalScrollbar?15:0)),l.ExtentHeight);}
        protected override Size ArrangeOverride(Size size){var l=owner.layout;Place(owner.contentSlot,l.Content,l.Body.X,l.Body.Y);return size;}
    }
    private sealed class FocusSurface(BleeckerEdgeDialog owner):ContentControl{protected override AutomationPeer OnCreateAutomationPeer()=>new DialogPeer(this,owner);}
    private sealed class DialogPeer(FocusSurface surface,BleeckerEdgeDialog owner):FrameworkElementAutomationPeer(surface),IWindowProvider
    {
        protected override string GetClassNameCore()=>owner.drawer?nameof(BleeckerDrawer):nameof(BleeckerSheet);protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Window;protected override bool IsDialogCore()=>true;protected override string GetNameCore()=>owner.HasTitle?owner.Title!:"Dialog";protected override string GetHelpTextCore()=>owner.Description??"";
        protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.Window?this:base.GetPatternCore(pattern);
        public bool IsModal=>true;public bool IsTopmost=>ReferenceEquals(owner.registration?.Host.ActiveSurface,surface);public bool Maximizable=>false;public bool Minimizable=>false;public WindowInteractionState InteractionState=>owner.IsOpen?WindowInteractionState.ReadyForUserInteraction:WindowInteractionState.Closing;public WindowVisualState VisualState=>WindowVisualState.Normal;
        public void Close()=>owner.RequestClose();public void SetVisualState(WindowVisualState state){if(state!=WindowVisualState.Normal)throw new InvalidOperationException("Drawer/Sheet cannot be minimized or maximized.");}public bool WaitForInputIdle(int milliseconds){if(milliseconds<0)throw new ArgumentOutOfRangeException(nameof(milliseconds));return owner.IsOpen;}
    }
}
