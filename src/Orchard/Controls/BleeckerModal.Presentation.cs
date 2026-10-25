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
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerModal
{
    private OverlayPanel? overlay;
    private Grid? backdrop;
    private Border? blurHost,header;
    private FocusSurface? focusSurface;
    private BleeckerOverlaySurface? paint;
    private ScrollView? viewport;
    private InnerPanel? inner;
    private BleeckerContentText? titleText,contentSlot;
    private BleeckerDialogCloseButton? closeButton;
    private ResourceDictionary? palette;
    private Style? defaultSurfaceStyle;
    private readonly CompositeTransform surfaceTransform=new();
    private readonly List<IDisposable> effects=[];
    private SpriteVisual? blurVisual;
    private bool measureQueued;
    private FrameworkElement? contentRoot;
    private readonly List<(DependencyProperty Property,long Token)> contentRootTokens=[];
    private BleeckerModalModel.Layout layout;
    private void QueueRemeasure()
    {
        if(measureQueued||overlay is not{} mounted)return;measureQueued=true;
        if(!DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>{measureQueued=false;if(ReferenceEquals(overlay,mounted))mounted.InvalidateMeasure();}))throw new InvalidOperationException("Modal could not queue content measurement.");
    }
    private void ObserveContentRoot()
    {
        var presenter=contentSlot?.Children.OfType<ContentPresenter>().Single();
        var next=presenter is not null&&presenter.Visibility==Visibility.Visible&&VisualTreeHelper.GetChildrenCount(presenter)>0?VisualTreeHelper.GetChild(presenter,0)as FrameworkElement:null;
        if(ReferenceEquals(next,contentRoot))return;DisconnectContentRoot();contentRoot=next;
        if(next is null)return;next.SizeChanged+=ContentSizeChanged;
        foreach(var property in new[]{WidthProperty,HeightProperty,MinWidthProperty,MaxWidthProperty,MinHeightProperty,MaxHeightProperty,MarginProperty,VisibilityProperty})contentRootTokens.Add((property,next.RegisterPropertyChangedCallback(property,(_,_)=>QueueRemeasure())));
        QueueRemeasure();
    }
    private void ContentSizeChanged(object sender,SizeChangedEventArgs args)=>QueueRemeasure();
    private void DisconnectContentRoot(){if(contentRoot is not null){contentRoot.SizeChanged-=ContentSizeChanged;foreach(var(property,token)in contentRootTokens)contentRoot.UnregisterPropertyChangedCallback(property,token);}contentRootTokens.Clear();contentRoot=null;}
    private void MountPresentation()
    {
        if(registration is{IsAttached:true}){if(!registration.Layer.Active)registration.Host.Activate(registration);return;}
        ClearPresentation();palette=new(){Source=new Uri("ms-appx:///Orchard/Themes/Modal.xaml")};defaultSurfaceStyle=(Style)palette["BleeckerModalSurfaceStyle"];
        titleText=new("ModalTitle","BleeckerPrimarySemiboldFontFamily",20,28,"BleeckerTextPrimaryBrush"){ConstrainLayoutWidth=true};titleText.Text.FontWeight=Microsoft.UI.Text.FontWeights.SemiBold;titleText.Text.CharacterSpacing=-20;
        contentSlot=new("ModalContent","BleeckerPrimaryFontFamily",16,24,"BleeckerTextPrimaryBrush",stretchContent:true);
        contentSlot.SetBinding(DataContextProperty,new Binding{Source=this,Path=new PropertyPath(nameof(DataContext))});
        closeButton=new(){Name="ModalClose",UseLayoutRounding=false,MinWidth=0};AutomationProperties.SetName(closeButton,"Close");closeButton.Click+=CloseClick;
        header=(Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' BorderBrush='{ThemeResource ModalHeaderBorderBrush}' BorderThickness='0,0,0,1' UseLayoutRounding='False'/>");header.Name="ModalHeader";
        var row=new HeaderPanel(this){UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};row.Children.Add(titleText);row.Children.Add(closeButton);header.Child=row;
        inner=new(this){Name="ModalInner",UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};inner.Children.Add(header);inner.Children.Add(contentSlot);
        viewport=new(){Name="ModalViewport",IsTabStop=false,UseLayoutRounding=false,Content=inner,ContentOrientation=ScrollingContentOrientation.Vertical,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Top,HorizontalScrollMode=ScrollingScrollMode.Disabled,HorizontalScrollBarVisibility=ScrollingScrollBarVisibility.Hidden,VerticalScrollMode=ScrollingScrollMode.Enabled,VerticalScrollBarVisibility=ScrollingScrollBarVisibility.Auto,ZoomMode=ScrollingZoomMode.Disabled,Style=BleeckerModalViewport.CreateStyle()};
        paint=new(){Name="ModalPaintSurface",Content=viewport,Style=SurfaceStyle??defaultSurfaceStyle,UseLayoutRounding=false};
        focusSurface=new(this){Name="ModalSurface",Content=paint,IsTabStop=true,UseSystemFocusVisuals=false,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch,UseLayoutRounding=false,RenderTransform=surfaceTransform,RenderTransformOrigin=new Point(.5,.5),TabFocusNavigation=KeyboardNavigationMode.Cycle};
        AutomationProperties.SetIsDialog(focusSurface,true);AutomationProperties.SetLabeledBy(focusSurface,titleText.Text);
        backdrop=new(){Name="ModalBackdrop",Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent),UseLayoutRounding=false};blurHost=new();backdrop.Children.Add(blurHost);backdrop.Children.Add((Border)XamlReader.Load("<Border xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource AlertDialogBackdropBrush}'/>"));backdrop.Tapped+=BackdropTapped;
        overlay=new(this){Name="ModalOverlay",UseLayoutRounding=false,FlowDirection=FlowDirection.LeftToRight};overlay.Children.Add(backdrop);overlay.Children.Add(focusSurface);
        RenderContent();var host=BleeckerModalHost.Require(this);registration=host.Attach(this,focusSurface,overlay,RequestClose);host.Activate(registration);
    }
    private void CloseClick(object sender,RoutedEventArgs args)=>RequestClose();
    private void BackdropTapped(object sender,TappedRoutedEventArgs args){RequestClose();args.Handled=true;}
    private void RenderContent()
    {
        if(paint is null||focusSurface is null||titleText is null||contentSlot is null||closeButton is null||viewport is null)return;
        if(!ReferenceEquals(paint.Style,SurfaceStyle??defaultSurfaceStyle))paint.Style=SurfaceStyle??defaultSurfaceStyle;
        titleText.Set(Title,null);contentSlot.Set(Content,ContentTemplate,selector:ContentTemplateSelector);
        foreach(var slot in new[]{titleText,contentSlot}){slot.FlowDirection=slot.Text.FlowDirection=FlowDirection;slot.Text.Language=Language;slot.Text.IsTextScaleFactorEnabled=IsTextScaleFactorEnabled;}
        focusSurface.RequestedTheme=ActualTheme;focusSurface.IsEnabled=IsEnabled;viewport.FlowDirection=paint.FlowDirection=FlowDirection;
        closeButton.IsEnabled=IsEnabled&&CloseCommand?.CanExecute(CloseCommandParameter)!=false;AutomationProperties.SetName(focusSurface,Title??"");
        FrameworkElementAutomationPeer.FromElement(focusSurface)?.InvalidatePeer();overlay?.InvalidateMeasure();
    }
    private Size MeasurePresentation(Size available)
    {
        if(titleText is null||contentSlot is null||paint is null||focusSurface is null||inner is null)return available;
        titleText.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));var maximum=titleText.NaturalWidth;var minimum=titleText.MinimumWidth;
        var next=BleeckerModalModel.Measure(available.Width,available.Height,maximum,minimum,width=>{titleText.Measure(new Size(width,double.PositiveInfinity));return titleText.NaturalHeight;},width=>{contentSlot.Measure(new Size(width,double.PositiveInfinity));return contentSlot.DesiredSize.Height;},FlowDirection==FlowDirection.RightToLeft,Math.Min(paint.MaxWidth,Math.Max(2,available.Width-32)));
        if(layout!=next){layout=next;inner.InvalidateMeasure();header?.InvalidateMeasure();}
        focusSurface.Measure(new Size(layout.Surface.Width,layout.Surface.Height));UpdateBlur(available);return available;
    }
    private void ApplyMotion(BleeckerAlertDialogModel.Presentation value)
    {
        if(focusSurface is null||backdrop is null)return;focusSurface.Opacity=value.Opacity;backdrop.Opacity=value.BackdropOpacity;surfaceTransform.ScaleX=surfaceTransform.ScaleY=value.Scale;surfaceTransform.TranslateY=value.TranslateY;
    }
    private void UpdateBlur(Size size)
    {
        if(blurHost is null)return;
        if(blurVisual is null)
        {
            var compositor=ElementCompositionPreview.GetElementVisual(blurHost).Compositor;var source=compositor.CreateBackdropBrush();using var blur=new GaussianBlurEffect{BlurAmount=2,BorderMode=EffectBorderMode.Hard,Optimization=EffectOptimization.Quality,Source=new CompositionEffectSourceParameter("backdrop")};var factory=compositor.CreateEffectFactory(blur);var brush=factory.CreateBrush();brush.SetSourceParameter("backdrop",source);blurVisual=compositor.CreateSpriteVisual();blurVisual.Brush=brush;effects.Add(source);effects.Add(factory);effects.Add(brush);effects.Add(blurVisual);ElementCompositionPreview.SetElementChildVisual(blurHost,blurVisual);
        }
        blurVisual.Size=new Vector2((float)size.Width,(float)size.Height);
    }
    private void ClearPresentation()
    {
        DisconnectContentRoot();if(backdrop is not null)backdrop.Tapped-=BackdropTapped;if(closeButton is not null)closeButton.Click-=CloseClick;if(blurHost is not null)ElementCompositionPreview.SetElementChildVisual(blurHost,null);
        for(var i=effects.Count-1;i>=0;i--)effects[i].Dispose();effects.Clear();blurVisual=null;contentSlot?.Release();titleText?.Release();
        if(viewport is not null)viewport.Content=null;if(paint is not null)paint.Content=null;if(focusSurface is not null)focusSurface.Content=null;overlay?.Children.Clear();
        overlay=null;backdrop=null;blurHost=null;header=null;focusSurface=null;paint=null;viewport=null;inner=null;titleText=null;contentSlot=null;closeButton=null;palette=null;defaultSurfaceStyle=null;
    }
    private static void Place(FrameworkElement? element,BleeckerModalModel.Rect box,double x,double y){element?.Measure(new Size(box.Width,box.Height));element?.Arrange(new Rect(box.X-x,box.Y-y,box.Width,box.Height));}
    private sealed partial class OverlayPanel(BleeckerModal owner):Panel
    {
        protected override Size MeasureOverride(Size availableSize)=>owner.MeasurePresentation(availableSize);
        protected override Size ArrangeOverride(Size finalSize){owner.backdrop?.Arrange(new Rect(0,0,finalSize.Width,finalSize.Height));Place(owner.focusSurface,owner.layout.Surface,0,0);return finalSize;}
    }
    private sealed partial class InnerPanel(BleeckerModal owner):Panel
    {
        protected override Size MeasureOverride(Size availableSize){var l=owner.layout;owner.header?.Measure(new Size(l.Header.Width,l.Header.Height));owner.contentSlot?.Measure(new Size(l.Content.Width,l.Content.Height));return new(l.ViewportWidth,l.ExtentHeight);}
        protected override Size ArrangeOverride(Size finalSize){var l=owner.layout;Place(owner.header,l.Header,l.Inner.X,l.Inner.Y);Place(owner.contentSlot,l.Content,l.Inner.X,l.Inner.Y);return finalSize;}
    }
    private sealed partial class HeaderPanel(BleeckerModal owner):Panel
    {
        protected override Size MeasureOverride(Size availableSize){var l=owner.layout;owner.titleText?.Measure(new Size(l.Title.Width,l.Title.Height));if(owner.closeButton is not null){owner.closeButton.Width=l.Close.Width;owner.closeButton.Measure(new Size(l.Close.Width,l.Close.Height));}return new(l.Header.Width,l.Header.Height-1);}
        protected override Size ArrangeOverride(Size finalSize){var l=owner.layout;Place(owner.titleText,l.Title,l.Header.X,l.Header.Y);Place(owner.closeButton,l.Close,l.Header.X,l.Header.Y);return finalSize;}
    }
    private sealed class FocusSurface(BleeckerModal owner):ContentControl{protected override AutomationPeer OnCreateAutomationPeer()=>new DialogPeer(this,owner);}
    private sealed class DialogPeer(FocusSurface surface,BleeckerModal owner):FrameworkElementAutomationPeer(surface),IWindowProvider
    {
        protected override string GetClassNameCore()=>nameof(BleeckerModal);protected override AutomationControlType GetAutomationControlTypeCore()=>AutomationControlType.Window;protected override bool IsDialogCore()=>true;protected override string GetNameCore()=>owner.Title??"";
        protected override object GetPatternCore(PatternInterface pattern)=>pattern==PatternInterface.Window?this:base.GetPatternCore(pattern);
        public bool IsModal=>true;public bool IsTopmost=>ReferenceEquals(owner.registration?.Host.ActiveSurface,surface);public bool Maximizable=>false;public bool Minimizable=>false;public WindowInteractionState InteractionState=>owner.IsOpen?WindowInteractionState.ReadyForUserInteraction:WindowInteractionState.Closing;public WindowVisualState VisualState=>WindowVisualState.Normal;
        public void Close()=>owner.RequestClose();public void SetVisualState(WindowVisualState state){if(state!=WindowVisualState.Normal)throw new InvalidOperationException("Modal cannot be minimized or maximized.");}public bool WaitForInputIdle(int milliseconds){if(milliseconds<0)throw new ArgumentOutOfRangeException(nameof(milliseconds));return owner.IsOpen;}
    }
}
