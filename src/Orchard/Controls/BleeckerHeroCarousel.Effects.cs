using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard;

public sealed partial class BleeckerHeroCarousel
{
    private sealed class HeroBackdrop:IDisposable
    {
        internal Border Host{get;}
        private CompositionBackdropBrush? source;
        private CompositionEffectFactory? factory;
        private CompositionEffectBrush? brush;
        private SpriteVisual? visual;
        private CompositionPathGeometry? geometry;
        private CompositionGeometricClip? clip;
        private CanvasGeometry? contour;
        private (double Width,double Height,CornerRadius Corners)? shape;
        private readonly long cornerToken;
        private bool disposed;
        internal HeroBackdrop(string name,Border? host=null)
        {
            Host=host??new Border{Name=name,IsHitTestVisible=false,UseLayoutRounding=false};AutomationProperties.SetAccessibilityView(Host,AccessibilityView.Raw);Host.Loaded+=Loaded;Host.Unloaded+=Unloaded;Host.SizeChanged+=Resized;cornerToken=Host.RegisterPropertyChangedCallback(Border.CornerRadiusProperty,(_,_)=>Update());
        }
        private void Loaded(object sender,RoutedEventArgs args)=>Update();
        private void Unloaded(object sender,RoutedEventArgs args)=>Clear();
        private void Resized(object sender,SizeChangedEventArgs args)=>Update();
        internal void Update()
        {
            if(disposed||!Host.IsLoaded||Host.ActualWidth<=0||Host.ActualHeight<=0)return;
            if(visual is null){var compositor=ElementCompositionPreview.GetElementVisual(Host).Compositor;source=compositor.CreateBackdropBrush();using var blur=new GaussianBlurEffect{Name="HeroBlur",BlurAmount=2,BorderMode=EffectBorderMode.Hard,Optimization=EffectOptimization.Quality,Source=new CompositionEffectSourceParameter("backdrop")};factory=compositor.CreateEffectFactory(blur,new[]{"HeroBlur.BlurAmount"});brush=factory.CreateBrush();brush.SetSourceParameter("backdrop",source);visual=compositor.CreateSpriteVisual();visual.Comment="OrchardHeroBackdropBlur2";visual.Brush=brush;geometry=compositor.CreatePathGeometry();clip=compositor.CreateGeometricClip(geometry);visual.Clip=clip;ElementCompositionPreview.SetElementChildVisual(Host,visual);}
            visual.Size=new((float)Host.ActualWidth,(float)Host.ActualHeight);var next=(Host.ActualWidth,Host.ActualHeight,Host.CornerRadius);if(shape==next)return;
            using var builder=new CanvasPathBuilder(CanvasDevice.GetSharedDevice());var figure=BleeckerShadowGeometry.RoundedMask(next.ActualWidth,next.ActualHeight,next.CornerRadius,0).Figures[0];builder.BeginFigure(new((float)figure.StartPoint.X,(float)figure.StartPoint.Y));
            foreach(var segment in figure.Segments){if(segment is LineSegment line)builder.AddLine(new Vector2((float)line.Point.X,(float)line.Point.Y));else if(segment is ArcSegment arc)builder.AddArc(new Vector2((float)arc.Point.X,(float)arc.Point.Y),(float)arc.Size.Width,(float)arc.Size.Height,0,CanvasSweepDirection.Clockwise,CanvasArcSize.Small);}
            builder.EndFigure(CanvasFigureLoop.Closed);var nextContour=CanvasGeometry.CreatePath(builder);geometry!.Path=new CompositionPath(nextContour);contour?.Dispose();contour=nextContour;shape=next;
        }
        internal void Clear(){ElementCompositionPreview.SetElementChildVisual(Host,null);visual?.Dispose();brush?.Dispose();factory?.Dispose();source?.Dispose();clip?.Dispose();geometry?.Dispose();contour?.Dispose();visual=null;brush=null;factory=null;source=null;clip=null;geometry=null;contour=null;shape=null;}
        public void Dispose(){if(disposed)return;disposed=true;Host.Loaded-=Loaded;Host.Unloaded-=Unloaded;Host.SizeChanged-=Resized;Host.UnregisterPropertyChangedCallback(Border.CornerRadiusProperty,cornerToken);Host.ClearValue(Border.CornerRadiusProperty);Clear();}
    }
    private sealed class HeroChrome:Panel,IDisposable
    {
        internal Border Chrome{get;}
        private readonly HeroBackdrop backdrop;
        private readonly HeroTextShadow shadow;
        internal HeroChrome(string name,BleeckerContentText text)
        {
            Name=name+"Surface";UseLayoutRounding=false;IsHitTestVisible=false;var content=new Grid{UseLayoutRounding=false};shadow=new(content,text);content.Children.Add(shadow.Host);content.Children.Add(text);Chrome=new(){Name=name,Child=content,UseLayoutRounding=false,IsHitTestVisible=false};backdrop=new(name+"Backdrop");backdrop.Host.SetBinding(Border.CornerRadiusProperty,new Binding{Source=Chrome,Path=new PropertyPath("CornerRadius")});backdrop.Host.SetBinding(OpacityProperty,new Binding{Source=Chrome,Path=new PropertyPath("Opacity")});backdrop.Host.SetBinding(VisibilityProperty,new Binding{Source=Chrome,Path=new PropertyPath("Visibility")});Canvas.SetZIndex(backdrop.Host,int.MinValue);Children.Add(backdrop.Host);Children.Add(Chrome);
        }
        protected override Size MeasureOverride(Size available){Chrome.Measure(available);return Chrome.DesiredSize;}
        protected override Size ArrangeOverride(Size final){Chrome.Arrange(new(0,0,final.Width,final.Height));var bounds=Chrome.TransformToVisual(this).TransformBounds(new Rect(0,0,Chrome.ActualWidth,Chrome.ActualHeight));backdrop.Host.Measure(new(bounds.Width,bounds.Height));backdrop.Host.Arrange(bounds);backdrop.Update();shadow.Update();return final;}
        public void Dispose(){backdrop.Host.ClearValue(OpacityProperty);backdrop.Host.ClearValue(VisibilityProperty);backdrop.Dispose();shadow.Dispose();Chrome.Child=null;Children.Clear();}
    }
    private sealed class HeroTextShadow : IDisposable
    {
        private sealed record Layer(CompositionSurfaceBrush Mask, SpriteVisual Caster, CompositionVisualSurface Source, SpriteVisual Output, float Extent, float Y);
        private readonly FrameworkElement owner;
        private readonly BleeckerContentText text;
        private readonly List<IDisposable> resources = [];
        private readonly List<Layer> layers = [];
        private ContainerVisual? container;
        private object? maskState;
        private bool pending, disposed;
        internal Border Host { get; } = new() { IsHitTestVisible = false, UseLayoutRounding = false };

        internal HeroTextShadow(FrameworkElement owner, BleeckerContentText text)
        {
            this.owner = owner; this.text = text; Host.Name = "HeroGlyphShadow_" + text.Name;
            AutomationProperties.SetAccessibilityView(Host, AccessibilityView.Raw);
            Host.Loaded += (_, _) => Update(); Host.Unloaded += (_, _) => Clear();
        }
        internal void Update()
        {
            if (disposed || pending) return;
            pending = true;
            if (!owner.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () => { pending = false; UpdateLayoutShadow(); }))
                throw new InvalidOperationException("Hero Carousel shadow dispatcher rejected layout.");
        }
        private T Keep<T>(T value) where T : IDisposable { resources.Add(value); return value; }
        private void UpdateLayoutShadow()
        {
            if (disposed) return;
            var glyph = text.Text;
            if (!Host.IsLoaded || glyph.Visibility != Visibility.Visible || string.IsNullOrEmpty(glyph.Text) || glyph.ActualWidth <= 0 || glyph.ActualHeight <= 0)
            { Clear(); return; }
            var next = new
            {
                glyph.Text, Family = glyph.FontFamily.Source, glyph.FontSize, glyph.FontWeight, glyph.FontStyle, glyph.FontStretch,
                glyph.CharacterSpacing, glyph.LineHeight, glyph.LineStackingStrategy, glyph.TextWrapping, glyph.TextAlignment,
                glyph.FlowDirection, glyph.Language, glyph.IsTextScaleFactorEnabled, glyph.ActualWidth, glyph.ActualHeight,
                Dpi = Host.XamlRoot!.RasterizationScale
            };
            if (container is null)
            {
                var compositor = ElementCompositionPreview.GetElementVisual(Host).Compositor;
                container = Keep(compositor.CreateContainerVisual());
                Add(4, 2, .5f);
                if (text.Name == "HeroTitle") Add(8, 2, .35f);
                else if (text.Name == "HeroDescription") Add(10, 0, .45f);
                void Add(float blur, float y, float alpha)
                {
                    // CSS blur length is twice Gaussian sigma. The transparent
                    // three-sigma padding keeps the hard surface edge invisible.
                    var extent = MathF.Ceiling(blur / 2 * 3);
                    var mask = Keep(compositor.CreateSurfaceBrush()); mask.Stretch = CompositionStretch.None;
                    mask.HorizontalAlignmentRatio = mask.VerticalAlignmentRatio = 0;
                    var caster = Keep(compositor.CreateSpriteVisual()); caster.Brush = mask;
                    var source = Keep(compositor.CreateVisualSurface()); source.SourceVisual = caster;
                    var rgba = Keep(compositor.CreateSurfaceBrush(source));
                    using var gaussian = new GaussianBlurEffect { Name = "HeroGlyphBlur", BlurAmount = blur / 2, BorderMode = EffectBorderMode.Hard, Optimization = EffectOptimization.Quality, Source = new CompositionEffectSourceParameter("glyph") };
                    using var ink = new ColorSourceEffect { Color = Microsoft.UI.Colors.Black };
                    using var tinted = new CompositeEffect { Mode = CanvasComposite.SourceIn, Sources = { gaussian, ink } };
                    var factory = Keep(compositor.CreateEffectFactory(tinted, new[] { "HeroGlyphBlur.BlurAmount" }));
                    var effect = Keep(factory.CreateBrush()); effect.SetSourceParameter("glyph", rgba); effect.Properties.InsertScalar("HeroGlyphBlur.BlurAmount", blur / 2);
                    var output = Keep(compositor.CreateSpriteVisual()); output.Comment = "OrchardHeroCssGlyphShadow_" + blur;
                    output.Brush = effect; output.Opacity = alpha; container.Children.InsertAtBottom(output);
                    layers.Add(new(mask, caster, source, output, extent, y));
                }
            }
            if (!Equals(maskState, next))
            {
                // XAML owns its live alpha brush and surface. Only the wrapper
                // brushes belong to Orchard; never modify or dispose the original.
                var original = glyph.GetAlphaMask() as CompositionSurfaceBrush ?? throw new InvalidOperationException("Hero text requires its native glyph surface.");
                foreach (var layer in layers)
                {
                    layer.Mask.Surface = original.Surface; layer.Mask.Scale = original.Scale;
                    layer.Mask.CenterPoint = original.CenterPoint; layer.Mask.RotationAngle = original.RotationAngle;
                    layer.Mask.TransformMatrix = original.TransformMatrix; layer.Mask.BitmapInterpolationMode = original.BitmapInterpolationMode;
                    layer.Mask.Offset = original.Offset + new Vector2(layer.Extent);
                }
                maskState = next;
            }
            var point = glyph.TransformToVisual(owner).TransformPoint(new Point());
            container.Size = new((float)Host.ActualWidth, (float)Host.ActualHeight);
            foreach (var layer in layers)
            {
                var pixels = new Vector2(MathF.Ceiling((float)glyph.ActualWidth + layer.Extent * 2), MathF.Ceiling((float)glyph.ActualHeight + layer.Extent * 2));
                if (layer.Caster.Size != pixels) layer.Caster.Size = pixels;
                if (layer.Source.SourceSize != pixels) layer.Source.SourceSize = pixels;
                layer.Output.Size = pixels; layer.Output.Offset = new((float)point.X - layer.Extent, (float)point.Y + layer.Y - layer.Extent, 0);
            }
            if (!ReferenceEquals(ElementCompositionPreview.GetElementChildVisual(Host), container)) ElementCompositionPreview.SetElementChildVisual(Host, container);
        }
        private void Clear()
        {
            ElementCompositionPreview.SetElementChildVisual(Host, null);
            foreach (var layer in layers) { layer.Source.SourceVisual = null; layer.Caster.Brush = null; layer.Mask.Surface = null; }
            for (var index = resources.Count - 1; index >= 0; index--) resources[index].Dispose();
            resources.Clear(); layers.Clear(); container = null; maskState = null;
        }
        public void Dispose() { disposed = true; Clear(); }
    }
}
