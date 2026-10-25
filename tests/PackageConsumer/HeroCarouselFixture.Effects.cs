using System.Collections.ObjectModel;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;

namespace Orchard.Testing;

internal static partial class HeroCarouselFixture
{
    private static async Task<object> VerifyEffects(BleeckerHeroCarousel host,FrameworkElement root,string scheme,Func<int,Task> layout,Func<string,Task<string>> capture)
    {
        static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        Rect Bounds(FrameworkElement element)=>element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
        static object Box(Rect r)=>new{x=r.X,y=r.Y,width=r.Width,height=r.Height};
        static bool Equal(Rect a,Rect b)=>Math.Abs(a.X-b.X)<=1d/1024&&Math.Abs(a.Y-b.Y)<=1d/1024&&Math.Abs(a.Width-b.Width)<=1d/1024&&Math.Abs(a.Height-b.Height)<=1d/1024;
        var pattern=new WriteableBitmap(1000,600);var bytes=new byte[1000*600*4];for(var y=0;y<600;y++)for(var x=0;x<1000;x++){var i=(y*1000+x)*4;var value=(byte)(x%8<4?255:0);bytes[i]=bytes[i+1]=bytes[i+2]=value;bytes[i+3]=255;}using(var buffer=pattern.PixelBuffer.AsStream())buffer.Write(bytes);pattern.Invalidate();
        host.DefaultBackground=new ImageBrush{ImageSource=pattern,Stretch=Stretch.Fill};var transparent=new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        Style ClearBackground(Style original,Type target,DependencyProperty background,DependencyProperty border){var style=new Style{TargetType=target,BasedOn=original};style.Setters.Add(new Setter(background,transparent));style.Setters.Add(new Setter(border,transparent));return style;}
        host.TagStyle=ClearBackground(host.TagStyle!,typeof(Border),Border.BackgroundProperty,Border.BorderBrushProperty);host.CtaStyle=ClearBackground(host.CtaStyle!,typeof(Border),Border.BackgroundProperty,Border.BorderBrushProperty);host.ArrowStyle=ClearBackground(host.ArrowStyle!,typeof(Button),Control.BackgroundProperty,Control.BorderBrushProperty);Check(VisualStateManager.GoToState(host,"PointerOver",false),"Hero effects hover state missing.");await layout(300);
        var original=host.Items.First();var slide=Elements(host).OfType<ContentControl>().Single(e=>e.Name=="HeroSlide"&&ReferenceEquals(e.Tag,original));var cta=Elements(slide).OfType<Border>().Single(e=>e.Name=="HeroCta");var tags=Elements(slide).OfType<Border>().Where(e=>e.Name=="HeroTag").ToArray();Check(tags.Length==2,"Hero effects requires original tags.");
        var all=Elements(host).OfType<Border>().Where(e=>e.Name is "HeroCtaBackdrop" or "HeroTagBackdrop" or "HeroArrowBackdrop" or "HeroNavigationBackdrop").ToArray();Check(all.Length==9,"Hero effects is missing owned backdrop hosts.");var nodes=new List<(Border Host,Visual Visual)>();var bindings=new List<object>();
        foreach(var item in all)
        {
            var sprite=ElementCompositionPreview.GetElementChildVisual(item)as SpriteVisual;Check(sprite?.Brush is CompositionEffectBrush,"Hero backdrop effect is absent.");var brush=(CompositionEffectBrush)sprite!.Brush;Check(brush.GetSourceParameter("backdrop")is CompositionBackdropBrush,"Hero backdrop source is not the live backdrop.");var status=brush.Properties.TryGetScalar("HeroBlur.BlurAmount",out var sigma);Check(status==CompositionGetValueStatus.Succeeded&&sigma==2,"Hero backdrop sigma differs from source.");Check(sprite.Clip is CompositionGeometricClip{Geometry:CompositionPathGeometry},"Hero backdrop corner clip is absent.");
            var role=item.Name;var target=role=="HeroNavigationBackdrop"?Elements(host).OfType<Border>().Single(e=>e.Name=="HeroIndicators"):Elements((FrameworkElement)VisualTreeHelper.GetParent(item)).OfType<Border>().Single(e=>e.Name==(role=="HeroArrowBackdrop"?"ArrowChrome":role=="HeroCtaBackdrop"?"HeroCta":"HeroTag"));Check(Equal(Bounds(item),Bounds(target)),"Hero backdrop is detached from its chrome bounds.");Check(Math.Abs(sprite.Size.X-item.ActualWidth)<.001&&Math.Abs(sprite.Size.Y-item.ActualHeight)<.001,"Hero backdrop sprite size is stale.");nodes.Add((item,sprite));bindings.Add(new{name=role,liveBackdrop=true,sigma,clipType="path",box=Box(Bounds(item)),targetBox=Box(Bounds(target)),sourceRasterParityVerified=false});
        }
        var shadowNodes=new List<(Border Host,Visual Visual)>();var shadowBindings=new List<object>();foreach(var item in Elements(host).OfType<Border>().Where(e=>e.Name=="HeroGlyphShadow_HeroTagText"))
        {
            var container=ElementCompositionPreview.GetElementChildVisual(item)as ContainerVisual;Check(container is not null&&container.Children.Count==1,"Hero tag glyph shadow is absent.");var output=container!.Children.First()as SpriteVisual;var effect=output?.Brush as CompositionEffectBrush;var glyph=effect?.GetSourceParameter("glyph");var parent=(FrameworkElement)VisualTreeHelper.GetParent(item);var text=Elements(parent).OfType<TextBlock>().Single(e=>e.Name=="HeroTagTextText");Check(glyph is CompositionSurfaceBrush{Surface:CompositionVisualSurface{SourceVisual:SpriteVisual{Brush:CompositionSurfaceBrush owned}}}&&text.GetAlphaMask()is CompositionSurfaceBrush originalGlyph&&ReferenceEquals(owned.Surface,originalGlyph.Surface),"Hero tag shadow does not retain its original glyph source.");Check(effect!.Properties.TryGetScalar("HeroGlyphBlur.BlurAmount",out var blurSigma)==CompositionGetValueStatus.Succeeded&&blurSigma==2,"Hero tag shadow blur sigma differs.");shadowNodes.Add((item,container));shadowBindings.Add(new{originalGlyphRetained=true,layers=container.Children.Count,blurSigma,hostWidth=item.ActualWidth,hostHeight=item.ActualHeight,outputWidth=output!.Size.X,outputHeight=output.Size.Y,outputOffsetX=output.Offset.X,outputOffsetY=output.Offset.Y});
        }
        Check(shadowNodes.Count==3,"Hero tag shadow count differs.");var regions=new List<object>();void Region(string role,FrameworkElement e,bool navigation=false){var b=Bounds(e);regions.Add(new{role,box=Box(navigation?new Rect(b.X+12,b.Y+3,b.Width-24,5):new Rect(b.X+4,b.Y+4,Math.Min(6,b.Width-8),b.Height-8))});}
        Region("cta",cta);foreach(var tag in tags)Region("tag",tag);foreach(var arrow in Elements(host).OfType<Button>().Where(e=>e.Name is "HeroPrevious" or "HeroNext"))Region("arrow",Elements(arrow).Single(e=>e.Name=="ArrowChrome"));Region("navigation",Elements(host).Single(e=>e.Name=="HeroIndicators"),true);
        var stimulus=new RenderTargetBitmap();await stimulus.RenderAsync(root);var stimulusPixels=(await stimulus.GetPixelsAsync()).ToArray();Check(stimulus.PixelWidth==1000&&stimulusPixels[(100*1000+2)*4]>=240&&stimulusPixels[(100*1000+6)*4]<=15,"Hero stripe stimulus did not render its explicit white/black pixels.");await layout(20);
        var shadowBrushes=shadowNodes.Select(n=>((ContainerVisual)n.Visual).Children.Cast<SpriteVisual>().Single()).Select(v=>(Visual:v,Brush:v.Brush,Opacity:v.Opacity)).ToArray();var diagnostics=new List<CompositionColorBrush>();foreach(var node in shadowBrushes){var marker=node.Visual.Compositor.CreateColorBrush(Microsoft.UI.Colors.Red);diagnostics.Add(marker);node.Visual.Brush=marker;}await layout(70);var shadowPlacement=await capture("effects-shadow-placement-"+scheme);foreach(var node in shadowBrushes)node.Visual.Brush=((CompositionEffectBrush)node.Brush).GetSourceParameter("glyph");await layout(70);var shadowMask=await capture("effects-shadow-mask-"+scheme);foreach(var node in shadowBrushes)node.Visual.Brush=node.Brush;foreach(var marker in diagnostics)marker.Dispose();await layout(70);
        var allEffects=await capture("effects-on-"+scheme);foreach(var node in nodes)ElementCompositionPreview.SetElementChildVisual(node.Host,null);await layout(70);var noBackdrops=await capture("effects-without-backdrops-"+scheme);foreach(var node in nodes)ElementCompositionPreview.SetElementChildVisual(node.Host,node.Visual);await layout(70);foreach(var node in shadowBrushes)node.Visual.Opacity=0;await layout(70);Check(shadowBrushes.All(node=>node.Visual.Opacity==0),"Hero shadow opacity suppression was overwritten.");var noTagShadows=await capture("effects-without-tag-shadows-"+scheme);foreach(var node in shadowBrushes)node.Visual.Opacity=node.Opacity;await layout(70);
        var shadowRegions=tags.Select(e=>Box(Bounds(e))).ToArray();
        var retainedTagText=Elements(tags[0]).OfType<TextBlock>().Single(e=>e.Name=="HeroTagTextText");
        var retainedTagShadow=Elements(tags[0]).OfType<Border>().Single(e=>e.Name=="HeroGlyphShadow_HeroTagText");
        var retainedTagBackdrop=Elements(tags[0].Parent).OfType<Border>().Single(e=>e.Name=="HeroTagBackdrop");
        var retainedBackdropVisual=(SpriteVisual)ElementCompositionPreview.GetElementChildVisual(retainedTagBackdrop);
        var oldClip=((CompositionPathGeometry)((CompositionGeometricClip)retainedBackdropVisual.Clip).Geometry).Path;
        var changedStyle=new Style{TargetType=typeof(Border),BasedOn=host.TagStyle};
        changedStyle.Setters.Add(new Setter(Border.CornerRadiusProperty,new CornerRadius(2,5,9,13)));
        changedStyle.Setters.Add(new Setter(Border.PaddingProperty,new Thickness(13,6,9,4)));
        host.TagStyle=changedStyle;host.Width=800;host.FlowDirection=FlowDirection.RightToLeft;host.FontFamily=new FontFamily("Consolas");
        ((ObservableCollection<string>)original.Tags)[0]="Updated original glyph";await layout(120);
        Check(ReferenceEquals(retainedTagText,Elements(tags[0]).OfType<TextBlock>().Single(e=>e.Name=="HeroTagTextText"))&&retainedTagText.Text=="Updated original glyph"&&retainedTagText.FontFamily.Source=="Consolas","Hero live glyph update replaced or failed to update its original text.");
        Check(ReferenceEquals(retainedBackdropVisual,ElementCompositionPreview.GetElementChildVisual(retainedTagBackdrop)),"Hero live style replaced its retained tag backdrop.");
        Check(tags[0].CornerRadius==new CornerRadius(2,5,9,13)&&retainedTagBackdrop.CornerRadius==tags[0].CornerRadius&&!ReferenceEquals(oldClip,((CompositionPathGeometry)((CompositionGeometricClip)retainedBackdropVisual.Clip).Geometry).Path),"Hero four-corner style did not update its retained clip.");
        foreach(var item in Elements(host).OfType<Border>().Where(e=>e.Name is "HeroCtaBackdrop" or "HeroTagBackdrop" or "HeroArrowBackdrop" or "HeroNavigationBackdrop"))
        {
            var target=item.Name=="HeroNavigationBackdrop"?Elements(host).OfType<Border>().Single(e=>e.Name=="HeroIndicators"):Elements((FrameworkElement)VisualTreeHelper.GetParent(item)).OfType<Border>().Single(e=>e.Name==(item.Name=="HeroArrowBackdrop"?"ArrowChrome":item.Name=="HeroCtaBackdrop"?"HeroCta":"HeroTag"));
            var sprite=ElementCompositionPreview.GetElementChildVisual(item)as SpriteVisual;
            Check(sprite is not null&&Equal(Bounds(item),Bounds(target))&&Math.Abs(sprite.Size.X-item.ActualWidth)<.001&&Math.Abs(sprite.Size.Y-item.ActualHeight)<.001,"Hero resized RTL backdrop retained stale bounds.");
        }
        var liveShadowNodes=Elements(host).OfType<Border>().Where(e=>e.Name=="HeroGlyphShadow_HeroTagText").Select(e=>(Host:e,Visual:(ContainerVisual)ElementCompositionPreview.GetElementChildVisual(e))).ToArray();
        foreach(var node in liveShadowNodes)
        {
            var sprite=(SpriteVisual)node.Visual.Children.Single();var effect=(CompositionEffectBrush)sprite.Brush;
            var source=(CompositionVisualSurface)((CompositionSurfaceBrush)effect.GetSourceParameter("glyph")).Surface;
            var mask=(CompositionSurfaceBrush)((SpriteVisual)source.SourceVisual).Brush;
            var text=Elements((FrameworkElement)VisualTreeHelper.GetParent(node.Host)).OfType<TextBlock>().Single(e=>e.Name=="HeroTagTextText");
            Check(ReferenceEquals(mask.Surface,((CompositionSurfaceBrush)text.GetAlphaMask()).Surface),"Hero live glyph update kept a stale mask surface.");
        }
        var liveOn=await capture("effects-live-on-"+scheme);
        foreach(var node in liveShadowNodes)foreach(var sprite in node.Visual.Children.Cast<SpriteVisual>())sprite.Opacity=0;
        await layout(70);Check(liveShadowNodes.All(node=>node.Visual.Children.Cast<SpriteVisual>().All(sprite=>sprite.Opacity==0)),"Hero live shadow suppression was overwritten.");
        var liveOff=await capture("effects-live-without-shadows-"+scheme);
        foreach(var node in liveShadowNodes)foreach(var sprite in node.Visual.Children.Cast<SpriteVisual>())sprite.Opacity=.5f;
        await layout(70);var liveRegions=tags.Select(e=>Box(Bounds(e))).ToArray();
        ((ObservableCollection<string>)original.Tags)[0]="";await layout(70);
        Check(retainedTagText.Text==""&&ElementCompositionPreview.GetElementChildVisual(retainedTagShadow)is null,"Hero empty tag retained a glyph shadow.");
        ((ObservableCollection<string>)original.Tags)[0]="Restored original glyph";await layout(70);
        Check(ElementCompositionPreview.GetElementChildVisual(retainedTagShadow)is ContainerVisual,"Hero restored tag did not remount its shadow.");
        var titleShadow=Elements(slide).OfType<Border>().Single(e=>e.Name=="HeroGlyphShadow_HeroTitle");var originalTitle=original.Title;
        original.Title="";await layout(70);Check(ElementCompositionPreview.GetElementChildVisual(titleShadow)is null,"Hero empty title retained a glyph shadow.");
        original.Title=originalTitle;await layout(70);Check(ElementCompositionPreview.GetElementChildVisual(titleShadow)is ContainerVisual,"Hero restored title did not remount its shadow.");
        var live=new{on=liveOn,off=liveOff,regions=liveRegions,originalTextRetained=true,originalGlyphSurfaceRetained=true,fourCornerClipUpdated=true,resizedRtlBoundsMatched=true,emptyTagReleased=true,restoredTagMounted=true,emptyTitleReleased=true,restoredTitleMounted=true};
        var oldTagBackdrop=Elements(tags[^1].Parent).OfType<Border>().Single(e=>e.Name=="HeroTagBackdrop");var oldTagShadow=Elements(tags[^1]).OfType<Border>().Single(e=>e.Name=="HeroGlyphShadow_HeroTagText");((ObservableCollection<string>)original.Tags).RemoveAt(1);await layout(70);Check(ElementCompositionPreview.GetElementChildVisual(oldTagBackdrop)is null&&ElementCompositionPreview.GetElementChildVisual(oldTagShadow)is null,"Removed Hero tag retained an effect graph.");
        var oldArrows=Elements(host).OfType<Border>().Where(e=>e.Name=="HeroArrowBackdrop").ToArray();var originalArrowStyle=host.ArrowStyle;var replacement=new Style{TargetType=typeof(Button),BasedOn=originalArrowStyle};replacement.Setters.Add(new Setter(Control.TemplateProperty,(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Button'><ContentPresenter Content='{TemplateBinding Content}'/></ControlTemplate>")));host.ArrowStyle=replacement;await layout(70);Check(oldArrows.All(e=>ElementCompositionPreview.GetElementChildVisual(e)is null),"Replaced Hero arrow template retained a backdrop.");
        var remaining=Elements(host).OfType<Border>().Where(e=>e.Name.EndsWith("Backdrop",StringComparison.Ordinal)||e.Name.StartsWith("HeroGlyphShadow_",StringComparison.Ordinal)).ToArray();Check(host.Parent is Panel,"Hero effects host has no owning panel.");((Panel)host.Parent).Children.Remove(host);await layout(70);Check(remaining.All(e=>ElementCompositionPreview.GetElementChildVisual(e)is null),"Unloaded Hero retained an effect graph.");
        return new{scheme,allEffects,noBackdrops,noTagShadows,shadowPlacement,shadowMask,live,tagShadowSuppression="sprite-opacity-zero",backdrops=bindings,tagShadows=shadowBindings,regions,shadowRegions,removedTagReleased=true,replacedArrowReleased=true,unloadedGraphsReleased=true,physicalInputVerified=false,sourceRasterParityVerified=false};
    }
}

