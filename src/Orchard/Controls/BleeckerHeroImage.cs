using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;

namespace Orchard;

// The native Image remains the live authoritative decoder, including caller
// SVG/animated/mutable sources. Only the source's decorative layers are ours.
internal sealed class BleeckerHeroImage : Grid, IDisposable
{
    private readonly Image original = new() { Name="HeroOriginalImage",Stretch=Stretch.UniformToFill,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,IsHitTestVisible=false };
    private readonly Border originalViewport = new() { Name="HeroOriginalImageViewport",IsHitTestVisible=false,UseLayoutRounding=false };
    private readonly Grid sourceHost = new() { IsHitTestVisible=false };
    private readonly Border effectHost = new() { Name="HeroImageEffect",IsHitTestVisible=false };
    private readonly Dictionary<string,Image> layers=[];
    private readonly Dictionary<string,CompositionVisualSurface> surfaces=[];
    private readonly Dictionary<string,CompositionSurfaceBrush> brushes=[];
    private readonly ImageBrush gradient=new(){Stretch=Stretch.Fill};
    private CompositionEffectFactory? factory;
    private CompositionEffectBrush? effect;
    private CompositionEffectFactory? baseFactory;
    private CompositionEffectBrush? baseEffect;
    private SpriteVisual? visual;
    private XamlRoot? observedRoot;
    private (double Width,double Height,double Scale)? paintedLayers;
    private (double Width,double Height,double Scale,Color First,Color Middle,Color Last)? paintedGradient;
    private Color first,middle,last;
    private Brush? defaultBackground;
    private bool disposed;
    internal event ExceptionRoutedEventHandler? ImageFailed;
    internal ImageSource? Source {get=>original.Source;set{if(ReferenceEquals(original.Source,value))return;original.Source=value;Update();}}
    internal Brush? DefaultBackground {get=>defaultBackground;set{defaultBackground=value;Update();}}
    internal BleeckerHeroImage()
    {
        FlowDirection=FlowDirection.LeftToRight;IsHitTestVisible=false;UseLayoutRounding=false;AutomationProperties.SetAccessibilityView(this,AccessibilityView.Raw);
        originalViewport.Child=original;sourceHost.Children.Add(originalViewport);foreach(var name in new[]{"vertical","horizontal","tint","grain","vignette"}){var image=new Image{Name="HeroLayer_"+name,Stretch=Stretch.Fill,IsHitTestVisible=false};layers.Add(name,image);sourceHost.Children.Add(image);}
        Children.Add(sourceHost);Children.Add(effectHost);ElementCompositionPreview.SetIsTranslationEnabled(sourceHost,true);
        Loaded+=LoadedImage;Unloaded+=UnloadedImage;SizeChanged+=(_,_)=>Update();original.SizeChanged+=(_,_)=>Update();original.ImageOpened+=(_,_)=>Update();original.ImageFailed+=(_,args)=>ImageFailed?.Invoke(original,args);
    }
    private void LoadedImage(object sender,RoutedEventArgs args){observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;Update();}
    private void UnloadedImage(object sender,RoutedEventArgs args){if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;Disconnect();}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>Update();
    internal void SetPalette(SolidColorBrush start,SolidColorBrush via,SolidColorBrush end){first=start.Color;middle=via.Color;last=end.Color;Update();}
    private void Update()
    {
        if(disposed||ActualWidth<=0||ActualHeight<=0)return;
        var scale=XamlRoot?.RasterizationScale??1;
        if(Source is null)
        {
            sourceHost.Visibility=effectHost.Visibility=Visibility.Collapsed;Disconnect();
            if(defaultBackground is not null){Background=defaultBackground;return;}
            var key=(ActualWidth,ActualHeight,scale,first,middle,last);if(paintedGradient!=key)
            {
                var width=checked((int)Math.Ceiling(ActualWidth*scale));var height=checked((int)Math.Ceiling(ActualHeight*scale));
                var bytes=BleeckerHeroGradient.Paint(width,height,ActualWidth,scale,(first.R,first.G,first.B),(middle.R,middle.G,middle.B),(last.R,last.G,last.B));
                gradient.ImageSource=Bitmap(width,height,bytes);paintedGradient=key;
            }
            Background=gradient;return;
        }
        Background=null;sourceHost.Visibility=effectHost.Visibility=Visibility.Visible;
        if(!IsLoaded||XamlRoot is null)return;
        var sizeKey=(ActualWidth,ActualHeight,scale);
        if(paintedLayers!=sizeKey){GenerateLayers(scale);paintedLayers=sizeKey;}
        if(visual is null)CreateEffects();
        var size=new Vector2((float)ActualWidth,(float)ActualHeight);foreach(var surface in surfaces.Values)surface.SourceSize=size;visual!.Size=size;
        // Capture the centered, clipped native viewport. Offsetting a surface
        // over the Image itself would crop its existing layout clip a second
        // time and lose pixels from the right/bottom of oversized sources.
        originalViewport.Clip=new RectangleGeometry{Rect=new(0,0,ActualWidth,ActualHeight)};
        var translation=-(float)(Math.Ceiling((XamlRoot.Size.Width+ActualWidth+1)*scale)/scale);ElementCompositionPreview.GetElementVisual(sourceHost).Properties.InsertVector3("Translation",new(translation,0,0));
    }
    private void GenerateLayers(double scale)
    {
        var width=checked((int)Math.Ceiling(ActualWidth*scale));var height=checked((int)Math.Ceiling(ActualHeight*scale));
        var vertical=new byte[checked(height*4)];for(var y=0;y<height;y++){var p=(y+.5)/(ActualHeight*scale);var alpha=p<.5?.7+(.25-.7)*p*2:.25+(.85-.25)*(p-.5)*2;Put(vertical,y,0,0,0,alpha);}layers["vertical"].Source=Bitmap(1,height,vertical);
        var horizontal=new byte[checked(width*4)];var grain=new byte[checked(width*4)];for(var x=0;x<width;x++){var p=(x+.5)/(ActualWidth*scale);Put(horizontal,x,0,0,0,.6*Math.Abs(2*p-1));Put(grain,x,255,255,255,((x+.5)/scale%3)<1?.28:0);}layers["horizontal"].Source=Bitmap(width,1,horizontal);layers["grain"].Source=Bitmap(width,1,grain);
        var tint=new byte[checked(width*height*4)];var vignette=new byte[tint.Length];
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        {
            var px=(x+.5)/scale;var py=(y+.5)/scale;var radius=Math.Sqrt(Math.Pow((px-.5*ActualWidth)/(1.2*ActualWidth),2)+Math.Pow((py-.55*ActualHeight)/(.9*ActualHeight),2));
            var a=radius<.45?(R:26d,G:55d,B:77d,A:.9):(R:193d,G:129d,B:77d,A:.45);var b=radius<.45?(R:193d,G:129d,B:77d,A:.45):(R:0d,G:0d,B:0d,A:0d);var progress=Math.Clamp(radius<.45?radius/.45:(radius-.45)/.3,0,1);var alpha=a.A+(b.A-a.A)*progress;var red=alpha>0?(a.R*a.A+(b.R*b.A-a.R*a.A)*progress)/alpha:0;var green=alpha>0?(a.G*a.A+(b.G*b.A-a.G*a.A)*progress)/alpha:0;var blue=alpha>0?(a.B*a.A+(b.B*b.A-a.B*a.A)*progress)/alpha:0;Put(tint,y*width+x,red,green,blue,alpha);
            var outer=Math.Sqrt(Math.Pow((px-.5*ActualWidth)/(ActualWidth/Math.Sqrt(2)),2)+Math.Pow((py-.5*ActualHeight)/(ActualHeight/Math.Sqrt(2)),2));var shade=outer<.55?.18+(.44-.18)*outer/.55:.44+(.72-.44)*Math.Clamp((outer-.55)/.45,0,1);Put(vignette,y*width+x,0,0,0,shade);
        }
        layers["tint"].Source=Bitmap(width,height,tint);layers["vignette"].Source=Bitmap(width,height,vignette);
    }
    private void CreateEffects()
    {
        var compositor=ElementCompositionPreview.GetElementVisual(effectHost).Compositor;
        void Add(string name,FrameworkElement image){var surface=compositor.CreateVisualSurface();surface.Comment="OrchardHeroSource_"+name;surface.SourceVisual=ElementCompositionPreview.GetElementVisual(image);surfaces.Add(name,surface);brushes.Add(name,compositor.CreateSurfaceBrush(surface));}
        Add("original",originalViewport);foreach(var (name,image)in layers)Add(name,image);
        using var black=new ColorSourceEffect{Color=Color.FromArgb(51,0,0,0)};
        using var darken=new CompositeEffect{Mode=CanvasComposite.SourceOver};darken.Sources.Add(new CompositionEffectSourceParameter("original"));darken.Sources.Add(black);
        using var vertical=new CompositeEffect{Mode=CanvasComposite.SourceOver};vertical.Sources.Add(darken);vertical.Sources.Add(new CompositionEffectSourceParameter("vertical"));
        using var horizontal=new CompositeEffect{Mode=CanvasComposite.SourceOver};horizontal.Sources.Add(vertical);horizontal.Sources.Add(new CompositionEffectSourceParameter("horizontal"));
        using var tintOpacity=new OpacityEffect{Opacity=.45f,Source=new CompositionEffectSourceParameter("tint")};
        using var tint=new BlendEffect{Name="HeroSoftLight",Mode=BlendEffectMode.SoftLight,Background=horizontal,Foreground=tintOpacity};
        // Windows accepts at most four source parameters in one graph. Keep
        // the live image and first three overlays in a chained brush; the
        // final graph adds grain and vignette without flattening the image.
        baseFactory=compositor.CreateEffectFactory(tint);baseEffect=baseFactory.CreateBrush();baseEffect.Comment="OrchardHero_black20_vertical_horizontal_softlight45";
        foreach(var name in new[]{"original","vertical","horizontal","tint"})baseEffect.SetSourceParameter(name,brushes[name]);
        using var grainOpacity=new OpacityEffect{Opacity=.07f,Source=new CompositionEffectSourceParameter("grain")};
        using var grain=new BlendEffect{Name="HeroGrainOverlay",Mode=BlendEffectMode.Overlay,Background=new CompositionEffectSourceParameter("base"),Foreground=grainOpacity};
        using var final=new CompositeEffect{Name="HeroVignette",Mode=CanvasComposite.SourceOver};final.Sources.Add(grain);final.Sources.Add(new CompositionEffectSourceParameter("vignette"));
        factory=compositor.CreateEffectFactory(final);effect=factory.CreateBrush();effect.Comment="OrchardHero_black20_vertical_horizontal_softlight45_grain07_vignette";effect.SetSourceParameter("base",baseEffect);foreach(var name in new[]{"grain","vignette"})effect.SetSourceParameter(name,brushes[name]);
        visual=compositor.CreateSpriteVisual();visual.Comment="OrchardHeroLiveImageLayers";visual.Brush=effect;ElementCompositionPreview.SetElementChildVisual(effectHost,visual);
    }
    private static void Put(byte[] bytes,int pixel,double red,double green,double blue,double alpha){var offset=pixel*4;bytes[offset]=Channel(blue*alpha);bytes[offset+1]=Channel(green*alpha);bytes[offset+2]=Channel(red*alpha);bytes[offset+3]=Channel(255*alpha);}
    private static byte Channel(double value)=>(byte)Math.Clamp(Math.Round(value),0,255);
    private static WriteableBitmap Bitmap(int width,int height,byte[] bytes){var bitmap=new WriteableBitmap(width,height);using(var stream=bitmap.PixelBuffer.AsStream())stream.Write(bytes);bitmap.Invalidate();return bitmap;}
    private void Disconnect(){ElementCompositionPreview.SetElementChildVisual(effectHost,null);visual?.Dispose();effect?.Dispose();factory?.Dispose();baseEffect?.Dispose();baseFactory?.Dispose();visual=null;effect=null;factory=null;baseEffect=null;baseFactory=null;foreach(var surface in surfaces.Values){surface.SourceVisual=null;surface.Dispose();}foreach(var brush in brushes.Values)brush.Dispose();surfaces.Clear();brushes.Clear();}
    public void Dispose(){if(disposed)return;disposed=true;if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;Disconnect();original.Source=null;foreach(var layer in layers.Values)layer.Source=null;gradient.ImageSource=null;Background=null;paintedLayers=null;paintedGradient=null;}
}
