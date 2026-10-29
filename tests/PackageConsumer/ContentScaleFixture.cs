using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Orchard.Testing;

internal static class ContentScaleFixture
{
    private sealed class RetryCommand : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) { }
    }
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static T? Find<T>(DependencyObject root,string name) where T:FrameworkElement {if(root is T match&&match.Name==name)return match;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(Find<T>(VisualTreeHelper.GetChild(root,i),name)is{ } found)return found;return null;}
    private static IEnumerable<T> FindAll<T>(DependencyObject root,string name) where T:FrameworkElement {if(root is T match&&match.Name==name)yield return match;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)foreach(var found in FindAll<T>(VisualTreeHelper.GetChild(root,i),name))yield return found;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var observations=new List<object>();var frames=new List<object>();var toggleObservations=new List<object>();var toggleFrames=new List<object>();var inputOverrides=new List<object>();var contextMenuScale=new List<object>();var brandMarkScale=new List<object>();var eyebrowScale=new List<object>();var labelScale=new List<object>();var iconBadgeScale=new List<object>();var progressScale=new List<object>();var errorStateScale=new List<object>();var sectionHeaderScale=new List<object>();var pageHeaderScale=new List<object>();var dashboardSectionScale=new List<object>();var dashboardGridScale=new List<object>();var cardScale=new List<object>();var emptyScale=new List<object>();var progressStackScale=new List<object>();var metricScale=new List<object>();var sparklineScale=new List<object>();var feedGridScale=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
        void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        try
        {
            var root=new Canvas{Width=400,Height=160,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,68,85,102))};
            root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Content scale fixture left its hidden boundary.");}
            Guard();using var island=new DesktopWindowXamlSource();island.Initialize(window.AppWindow.Id);island.Content=root;Guard();
            var separator=new BleeckerSeparator{Width=320};Canvas.SetLeft(separator,24);Canvas.SetTop(separator,24);root.Children.Add(separator);
            var roundingProducer=new Border{Tag=true};
            var rounded=new BleeckerSeparator{Width=320};rounded.SetBinding(FrameworkElement.UseLayoutRoundingProperty,new Binding{Source=roundingProducer,Path=new PropertyPath("Tag")});Canvas.SetLeft(rounded,24);Canvas.SetTop(rounded,32);root.Children.Add(rounded);
            var vertical=new BleeckerSeparator{Orientation=Orientation.Vertical,Height=64};Canvas.SetLeft(vertical,368);Canvas.SetTop(vertical,24);root.Children.Add(vertical);
            var roundedVertical=new BleeckerSeparator{Orientation=Orientation.Vertical,Height=64};roundedVertical.SetBinding(FrameworkElement.UseLayoutRoundingProperty,new Binding{Source=roundingProducer,Path=new PropertyPath("Tag")});Canvas.SetLeft(roundedVertical,376);Canvas.SetTop(roundedVertical,24);root.Children.Add(roundedVertical);
            var badge=new BleeckerStatusBadge{Label="LOCAL",Description="Fictional local status",Variant=BleeckerStatusBadgeVariant.Live};AutomationProperties.SetName(badge,"");AutomationProperties.SetHelpText(badge,"");Canvas.SetLeft(badge,24);Canvas.SetTop(badge,48);root.Children.Add(badge);
            const string switchLabel="Enable fictional editorial notifications while reviewing a long draft";
            var scaledSwitch=new BleeckerSwitch{Width=320,Label=switchLabel,IsOn=true};var toggles=0;scaledSwitch.Toggled+=(_,_)=>toggles++;Canvas.SetLeft(scaledSwitch,24);Canvas.SetTop(scaledSwitch,104);root.Children.Add(scaledSwitch);
            var spinner=new BleeckerLoadingSpinner{Size=BleeckerLoadingSize.Large};AutomationProperties.SetName(spinner,"Loading fictional preview");Canvas.SetLeft(spinner,350);Canvas.SetTop(spinner,104);root.Children.Add(spinner);
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(root.Width,root.Height));root.Arrange(new Rect(0,0,root.Width,root.Height));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var scale in new[]{1f,1.25f,1.5f,2f})
            {
                stage=$"{theme}/{scale}";root.RequestedTheme=theme;island.SiteBridge.OverrideScale=scale;var width=(int)Math.Round(400*scale);var height=(int)Math.Round(160*scale);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width,height));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,width,height));await Layout();
                var actual=root.XamlRoot.RasterizationScale;var peer=FrameworkElementAutomationPeer.CreatePeerForElement(badge);
                var badgeSurface=Find<Border>(badge,"PART_Badge")??throw new InvalidOperationException("Scaled Status Badge surface is missing.");
                var badgeLabel=Find<TextBlock>(badge,"PART_Label")??throw new InvalidOperationException("Scaled Status Badge label is missing.");
                var badgeDescription=Find<TextBlock>(badge,"PART_Description")??throw new InvalidOperationException("Scaled Status Badge description is missing.");
                var badgeDot=Find<Microsoft.UI.Xaml.Shapes.Ellipse>(badge,"PART_Dot")??throw new InvalidOperationException("Scaled Status Badge dot is missing.");
                var badgeBounds=badge.TransformToVisual(root).TransformBounds(new Rect(0,0,badge.ActualWidth,badge.ActualHeight));
                var badgeSurfaceBounds=badgeSurface.TransformToVisual(root).TransformBounds(new Rect(0,0,badgeSurface.ActualWidth,badgeSurface.ActualHeight));
                var switchTrack=Find<Border>(scaledSwitch,"PART_Track")??throw new InvalidOperationException("Scaled Switch track is missing.");
                var switchThumb=Find<Microsoft.UI.Xaml.Shapes.Ellipse>(scaledSwitch,"PART_Thumb")??throw new InvalidOperationException("Scaled Switch thumb is missing.");
                var switchText=Find<TextBlock>(scaledSwitch,"PART_Label")??throw new InvalidOperationException("Scaled Switch label is missing.");
                var switchName=FrameworkElementAutomationPeer.CreatePeerForElement(scaledSwitch).GetName();
                var spinnerCanvas=Find<Canvas>(spinner,"PART_Canvas")??throw new InvalidOperationException("Scaled Loading Spinner canvas is missing.");
                var spinnerArc=Find<Microsoft.UI.Xaml.Shapes.Path>(spinner,"PART_Arc")??throw new InvalidOperationException("Scaled Loading Spinner arc is missing.");
                var spinnerBounds=spinner.TransformToVisual(root).TransformBounds(new Rect(0,0,spinner.ActualWidth,spinner.ActualHeight));
                var spinnerPeer=FrameworkElementAutomationPeer.CreatePeerForElement(spinner);
                var trackBounds=switchTrack.TransformToVisual(root).TransformBounds(new Rect(0,0,switchTrack.ActualWidth,switchTrack.ActualHeight));
                var labelBounds=switchText.TransformToVisual(root).TransformBounds(new Rect(0,0,switchText.ActualWidth,switchText.ActualHeight));
                var bindingsRetained=rounded.GetBindingExpression(FrameworkElement.UseLayoutRoundingProperty)is not null&&roundedVertical.GetBindingExpression(FrameworkElement.UseLayoutRoundingProperty)is not null;
                observations.Add(new{theme=theme.ToString(),requestedScale=scale,actualRasterizationScale=actual,islandScale=island.SiteBridge.OverrideScale,loaded=root.IsLoaded,separatorWidth=separator.ActualWidth,separatorHeight=separator.ActualHeight,separatorLayoutRounding=separator.UseLayoutRounding,verticalWidth=vertical.ActualWidth,verticalHeight=vertical.ActualHeight,verticalLayoutRounding=vertical.UseLayoutRounding,callerWidth=rounded.ActualWidth,callerHeight=rounded.ActualHeight,callerVerticalWidth=roundedVertical.ActualWidth,callerVerticalHeight=roundedVertical.ActualHeight,callerLayoutRounding=rounded.UseLayoutRounding,callerVerticalLayoutRounding=roundedVertical.UseLayoutRounding,bindingsRetained,badgeWidth=badge.ActualWidth,badgeHeight=badge.ActualHeight,badgeBounds=new{x=badgeBounds.X,y=badgeBounds.Y,width=badgeBounds.Width,height=badgeBounds.Height},badgeSurfaceBounds=new{x=badgeSurfaceBounds.X,y=badgeSurfaceBounds.Y,width=badgeSurfaceBounds.Width,height=badgeSurfaceBounds.Height},badgeLabel=badgeLabel.Text,badgeLabelHeight=badgeLabel.ActualHeight,badgeDescription=badgeDescription.Text,badgeDescriptionHeight=badgeDescription.ActualHeight,badgeDotWidth=badgeDot.ActualWidth,badgeDotHeight=badgeDot.ActualHeight,badgeTabStop=badge.IsTabStop,name=peer.GetName(),help=peer.GetHelpText(),switchWidth=scaledSwitch.ActualWidth,switchHeight=scaledSwitch.ActualHeight,switchTrackWidth=switchTrack.ActualWidth,switchTrackHeight=switchTrack.ActualHeight,switchThumbWidth=switchThumb.ActualWidth,switchThumbHeight=switchThumb.ActualHeight,switchLabel=switchText.Text,switchName,switchLabelBounds=new{x=labelBounds.X,y=labelBounds.Y,width=labelBounds.Width,height=labelBounds.Height},switchTrackBounds=new{x=trackBounds.X,y=trackBounds.Y,width=trackBounds.Width,height=trackBounds.Height},switchToggles=toggles,spinnerWidth=spinner.ActualWidth,spinnerHeight=spinner.ActualHeight,spinnerCanvasWidth=spinnerCanvas.ActualWidth,spinnerCanvasHeight=spinnerCanvas.ActualHeight,spinnerStrokeThickness=spinnerArc.StrokeThickness,spinnerName=spinnerPeer.GetName(),spinnerRole=spinnerPeer.GetAutomationControlType().ToString(),spinnerTabStop=spinner.IsTabStop,spinnerBounds=new{x=spinnerBounds.X,y=spinnerBounds.Y,width=spinnerBounds.Width,height=spinnerBounds.Height}});
                Require(Math.Abs(actual-scale)<.0001&&root.IsLoaded,"Owned XAML island did not apply the requested native rasterization scale.");Require(peer.GetName()==""&&peer.GetHelpText()=="","Scaled native island lost caller metadata.");
                Require(badge.IsLoaded&&!badge.IsTabStop&&badgeLabel.Text=="LOCAL"&&badgeDescription.Text=="Fictional local status"&&badgeLabel.ActualHeight>0&&badgeDescription.ActualHeight>0&&badgeDot.ActualWidth==6&&badgeDot.ActualHeight==6&&badgeSurface.ActualHeight==24&&badgeBounds.X>=24&&badgeBounds.Y>=48&&badgeBounds.Right<=400&&badgeBounds.Bottom<=104,"Scaled Status Badge lost its visible content, geometry or display-only semantics.");
                Require(!separator.UseLayoutRounding&&!vertical.UseLayoutRounding&&rounded.UseLayoutRounding&&roundedVertical.UseLayoutRounding&&bindingsRetained,"Separator defaults replaced caller pixel-snapping bindings or retained rounded defaults.");
                Require(separator.ActualWidth==320&&separator.ActualHeight==1&&vertical.ActualWidth==1&&vertical.ActualHeight==64,"Scaled Separator lost its logical dimensions.");
                Require(scaledSwitch.ActualWidth==320&&switchTrack.ActualWidth==36&&switchTrack.ActualHeight==20&&switchThumb.ActualWidth==16&&switchThumb.ActualHeight==16&&switchText.Text==switchLabel&&switchName==switchLabel&&toggles==0,"Scaled Switch lost its geometry, label, name or toggle isolation.");
                Require(spinner.IsLoaded&&!spinner.IsTabStop&&spinner.ActualWidth==48&&spinner.ActualHeight==48&&spinnerCanvas.ActualWidth==48&&spinnerCanvas.ActualHeight==48&&spinnerArc.StrokeThickness==3&&spinnerBounds.X>=350&&spinnerBounds.Y>=104&&spinnerBounds.Right<=400&&spinnerBounds.Bottom<=160&&spinnerPeer.GetName()=="Loading fictional preview"&&spinnerPeer.GetAutomationControlType()==AutomationControlType.ProgressBar,"Scaled Loading Spinner lost its geometry or progress semantics.");
                Require(labelBounds.X>=24&&labelBounds.Y>=104&&labelBounds.Right<=344&&labelBounds.Bottom<=160&&labelBounds.Height>=20,"Scaled Switch label escaped its available row or clipped the island.");
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());var file=$"content-scale-{theme}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png";
                using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),png);frames.Add(new{theme=theme.ToString(),requestedScale=scale,width=bitmap.PixelWidth,height=bitmap.PixelHeight,file,sha256=Convert.ToHexStringLower(SHA256.HashData(png))});
                Require(bitmap.PixelWidth==width&&bitmap.PixelHeight==height,"Native island capture did not use its actual rasterization scale.");
            }
            root.Children.Clear();
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                stage=$"context-menu/{theme}/150-percent";
                root.RequestedTheme=theme;
                island.SiteBridge.OverrideScale=1.5f;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(600,240));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,600,240));
                var target=new Button{Content="Fictional context target",Width=180,Height=36};
                Canvas.SetLeft(target,80);Canvas.SetTop(target,32);root.Children.Add(target);
                var menu=new BleeckerContextMenu();
                var constrainedStyle=new Style{TargetType=typeof(MenuFlyoutPresenter),BasedOn=menu.MenuFlyoutPresenterStyle};
                constrainedStyle.Setters.Add(new Setter(FrameworkElement.MaxWidthProperty,200d));
                menu.MenuFlyoutPresenterStyle=constrainedStyle;
                var item=new MenuFlyoutItem{Text="Inspect a fictional local item with a wrapped label",Style=(Style)Application.Current.Resources["BleeckerContextMenuItemStyle"]};
                menu.Items.Add(item);target.ContextFlyout=menu;
                await Layout();
                ((FlyoutBase)menu).ShowAt(target,new FlyoutShowOptions{ShowMode=FlyoutShowMode.Transient,Position=new Point(12,12)});
                await Layout();
                var popup=VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Single(p=>p.Child is MenuFlyoutPresenter);
                var presenter=(MenuFlyoutPresenter)popup.Child;
                var itemBounds=item.TransformToVisual(presenter).TransformBounds(new Rect(0,0,item.ActualWidth,item.ActualHeight));
                var label=Find<TextBlock>(item,"TextBlock")??throw new InvalidOperationException("Scaled Context Menu label is missing.");
                var labelBounds=label.TransformToVisual(presenter).TransformBounds(new Rect(0,0,label.ActualWidth,label.ActualHeight));
                Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&menu.IsOpen&&item.IsLoaded,
                    "Context Menu did not render in the native 150% island.");
                Require(presenter.ActualWidth>=160&&presenter.ActualWidth<=200&&presenter.ActualHeight>=36&&itemBounds.X>=-.5&&itemBounds.Y>=-.5&&
                    itemBounds.Right<=presenter.ActualWidth+.5&&itemBounds.Bottom<=presenter.ActualHeight+.5,
                    "Context Menu item clipped its native presenter at 150% scaling.");
                Require(label.Text==item.Text&&label.ActualHeight>19.5&&labelBounds.X>=itemBounds.X&&labelBounds.Y>=itemBounds.Y&&
                    labelBounds.Right<=itemBounds.Right+.5&&labelBounds.Bottom<=itemBounds.Bottom+.5,
                    $"Context Menu wrapped label clipped its item at 150% scaling: label={labelBounds}, item={itemBounds}, presenter={presenter.ActualWidth}x{presenter.ActualHeight}.");
                Require(!menu.AreOpenCloseAnimationsEnabled,"Context Menu retained an open/close animation under reduced motion.");
                contextMenuScale.Add(new{theme=theme.ToString(),scale=root.XamlRoot.RasterizationScale,passed=true,
                    presenterWidth=presenter.ActualWidth,presenterHeight=presenter.ActualHeight,
                    itemBounds=new{x=itemBounds.X,y=itemBounds.Y,width=itemBounds.Width,height=itemBounds.Height},
                    labelBounds=new{x=labelBounds.X,y=labelBounds.Y,width=labelBounds.Width,height=labelBounds.Height},
                    openCloseAnimationsEnabled=menu.AreOpenCloseAnimationsEnabled});
                menu.Hide();await Layout();root.Children.Clear();
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                stage=$"brand-mark/{theme}/150-percent";
                root.RequestedTheme=theme;
                var sizes=new[]{16d,32d,64d,96d};
                var positions=new[]{16d,56d,112d,200d};
                for(var index=0;index<sizes.Length;index++)
                {
                    var mark=new BleeckerBrandMark(sizes[index]);
                    Canvas.SetLeft(mark,positions[index]);Canvas.SetTop(mark,24);
                    root.Children.Add(mark);
                }
                await Layout();
                Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Brand Mark did not render at native 150% scale.");
                var marks=root.Children.OfType<BleeckerBrandMark>().ToArray();
                for(var index=0;index<marks.Length;index++)
                {
                    var mark=marks[index];
                    var icon=Find<BitmapIcon>(mark,"PART_Mark")??throw new InvalidOperationException("Scaled Brand Mark image is missing.");
                    var markBounds=mark.TransformToVisual(root).TransformBounds(new Rect(0,0,mark.ActualWidth,mark.ActualHeight));
                    var iconBounds=icon.TransformToVisual(root).TransformBounds(new Rect(0,0,icon.ActualWidth,icon.ActualHeight));
                    Require(mark.IsLoaded&&icon.IsLoaded&&!mark.IsTabStop&&mark.ActualWidth==sizes[index]&&mark.ActualHeight==sizes[index]&&
                        markBounds.X>=0&&markBounds.Y>=0&&markBounds.Right<=400&&markBounds.Bottom<=160&&
                        iconBounds.X>=markBounds.X-.5&&iconBounds.Y>=markBounds.Y-.5&&
                        iconBounds.Right<=markBounds.Right+.5&&iconBounds.Bottom<=markBounds.Bottom+.5,
                        $"Brand Mark {sizes[index]} DIP image clipped its frame at native 150% scale: mark={markBounds}, image={iconBounds}.");
                    brandMarkScale.Add(new{theme=theme.ToString(),scale=root.XamlRoot.RasterizationScale,size=sizes[index],
                        markBounds=new{x=markBounds.X,y=markBounds.Y,width=markBounds.Width,height=markBounds.Height},
                        iconBounds=new{x=iconBounds.X,y=iconBounds.Y,width=iconBounds.Width,height=iconBounds.Height},passed=true});
                }
                Guard();var brandFrame=new RenderTargetBitmap();await brandFrame.RenderAsync(root);Guard();
                Require(brandFrame.PixelWidth==600&&brandFrame.PixelHeight==240,"Brand Mark capture did not use native 150% rasterization.");
                var brandPixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await brandFrame.GetPixelsAsync());
                using var brandStream=new InMemoryRandomAccessStream();
                var brandEncoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,brandStream);
                brandEncoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,600,240,96,96,brandPixels);
                await brandEncoder.FlushAsync();brandStream.Seek(0);
                using var brandReader=new DataReader(brandStream);await brandReader.LoadAsync((uint)brandStream.Size);
                var brandPng=new byte[(int)brandStream.Size];brandReader.ReadBytes(brandPng);
                await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, $"brand-mark-scale-{theme}-1.5.png"),brandPng);
                root.Children.Clear();await Layout();
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                stage=$"eyebrow/{theme}/150-percent";
                root.RequestedTheme=theme;
                var plain=new BleeckerEyebrow{Width=180,Rule=true,Content="A longer section label wraps within the available width"};
                var richText=new TextBlock{Text="A longer rich section label wraps within the available width",TextWrapping=TextWrapping.Wrap};
                var richLabel=new BleeckerEyebrow{Width=180,Rule=true,Content=richText};
                Canvas.SetLeft(plain,24);Canvas.SetTop(plain,16);Canvas.SetLeft(richLabel,24);Canvas.SetTop(richLabel,88);
                root.Children.Add(plain);root.Children.Add(richLabel);await Layout();
                Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Eyebrow did not render at native 150% scale.");
                foreach(var (owner,content,id) in new (BleeckerEyebrow,FrameworkElement,string)[]{(plain,Find<TextBlock>(plain,"PART_Text")!,"plain"),(richLabel,richText,"rich")})
                {
                    var ownerBounds=owner.TransformToVisual(root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));
                    var contentBounds=content.TransformToVisual(root).TransformBounds(new Rect(0,0,content.ActualWidth,content.ActualHeight));
                    Require(owner.IsLoaded&&content.IsLoaded&&!owner.IsTabStop&&owner.ActualWidth==180&&content.ActualHeight>16&&
                        ownerBounds.X>=0&&ownerBounds.Y>=0&&ownerBounds.Right<=400&&ownerBounds.Bottom<=160&&
                        contentBounds.X>=ownerBounds.X-.5&&contentBounds.Y>=ownerBounds.Y-.5&&
                        contentBounds.Right<=ownerBounds.Right+.5&&contentBounds.Bottom<=ownerBounds.Bottom+.5,
                        $"Eyebrow {id} content clipped at native 150% scale: owner={ownerBounds}, content={contentBounds}.");
                    eyebrowScale.Add(new{theme=theme.ToString(),kind=id,scale=root.XamlRoot.RasterizationScale,
                        ownerBounds=new{x=ownerBounds.X,y=ownerBounds.Y,width=ownerBounds.Width,height=ownerBounds.Height},
                        contentBounds=new{x=contentBounds.X,y=contentBounds.Y,width=contentBounds.Width,height=contentBounds.Height},passed=true});
                }
                Guard();var eyebrowFrame=new RenderTargetBitmap();await eyebrowFrame.RenderAsync(root);Guard();
                Require(eyebrowFrame.PixelWidth==600&&eyebrowFrame.PixelHeight==240,"Eyebrow capture did not use native 150% rasterization.");
                var eyebrowPixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await eyebrowFrame.GetPixelsAsync());
                using var eyebrowStream=new InMemoryRandomAccessStream();
                var eyebrowEncoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,eyebrowStream);
                eyebrowEncoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,600,240,96,96,eyebrowPixels);
                await eyebrowEncoder.FlushAsync();eyebrowStream.Seek(0);
                using var eyebrowReader=new DataReader(eyebrowStream);await eyebrowReader.LoadAsync((uint)eyebrowStream.Size);
                var eyebrowPng=new byte[(int)eyebrowStream.Size];eyebrowReader.ReadBytes(eyebrowPng);
                await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!, $"eyebrow-scale-{theme}-1.5.png"),eyebrowPng);
                root.Children.Clear();await Layout();
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                stage=$"label/{theme}/150-percent";
                root.RequestedTheme=theme;
                var plainLabel=new BleeckerLabel{Width=140,Content="A longer coverage label wraps within its available width"};
                var richText=new TextBlock{Text="Rich coverage label wraps within its available width",TextWrapping=TextWrapping.Wrap};
                var richLabel=new BleeckerLabel{Width=160,Content=richText};
                var disabledLabel=new BleeckerLabel{Width=140,Content="Unavailable field",IsEnabled=false};
                var emptyLabel=new BleeckerLabel{Width=140,Content=""};
                Canvas.SetLeft(plainLabel,24);Canvas.SetTop(plainLabel,16);
                Canvas.SetLeft(richLabel,200);Canvas.SetTop(richLabel,16);
                Canvas.SetLeft(disabledLabel,24);Canvas.SetTop(disabledLabel,104);
                Canvas.SetLeft(emptyLabel,24);Canvas.SetTop(emptyLabel,132);
                root.Children.Add(plainLabel);root.Children.Add(richLabel);root.Children.Add(disabledLabel);root.Children.Add(emptyLabel);
                await Layout();
                Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Label did not render at native 150% scale.");
                foreach(var (owner,content,id) in new (BleeckerLabel,FrameworkElement,string)[]{
                    (plainLabel,Find<TextBlock>(plainLabel,"PART_Text")!,"plain"),
                    (richLabel,richText,"rich"),
                    (disabledLabel,Find<TextBlock>(disabledLabel,"PART_Text")!,"disabled"),
                    (emptyLabel,Find<TextBlock>(emptyLabel,"PART_Text")!,"empty")})
                {
                    var ownerBounds=owner.TransformToVisual(root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));
                    var contentBounds=content.TransformToVisual(root).TransformBounds(new Rect(0,0,content.ActualWidth,content.ActualHeight));
                    Require(owner.IsLoaded&&content.IsLoaded&&!owner.IsTabStop&&owner.ActualWidth>0&&owner.ActualHeight>0&&
                        ownerBounds.X>=0&&ownerBounds.Y>=0&&ownerBounds.Right<=400&&ownerBounds.Bottom<=160&&
                        contentBounds.X>=ownerBounds.X-.5&&contentBounds.Y>=ownerBounds.Y-.5&&
                        contentBounds.Right<=ownerBounds.Right+.5&&contentBounds.Bottom<=ownerBounds.Bottom+.5,
                        $"Label {id} clipped at native 150% scale: owner={ownerBounds}, content={contentBounds}.");
                    labelScale.Add(new{theme=theme.ToString(),kind=id,scale=root.XamlRoot.RasterizationScale,
                        ownerBounds=new{x=ownerBounds.X,y=ownerBounds.Y,width=ownerBounds.Width,height=ownerBounds.Height},
                        contentBounds=new{x=contentBounds.X,y=contentBounds.Y,width=contentBounds.Width,height=contentBounds.Height},passed=true});
                }
                root.Children.Clear();await Layout();
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                stage=$"icon-badge/{theme}/150-percent";
                root.RequestedTheme=theme;
                var variants=Enum.GetValues<BleeckerIconBadgeVariant>();
                var sizes=Enum.GetValues<BleeckerIconBadgeSize>();
                var badges=new List<(BleeckerIconBadge iconBadge,TextBlock child,string id)>();
                for(var index=0;index<variants.Length*sizes.Length;index++)
                {
                    var size=sizes[index%sizes.Length];var variant=variants[index/sizes.Length];
                    var child=new TextBlock{Text="A",Width=24,Height=24};
                    var iconBadge=new BleeckerIconBadge{Size=size,Variant=variant,Content=child};
                    Canvas.SetLeft(iconBadge,12+(index%3)*120);Canvas.SetTop(iconBadge,12+(index/3)*80);
                    root.Children.Add(iconBadge);badges.Add((iconBadge,child,$"{variant}/{size}"));
                }
                await Layout();
                Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Icon Badge did not render at native 150% scale.");
                foreach(var(iconBadge,child,id) in badges)
                {
                    var extent=iconBadge.Size==BleeckerIconBadgeSize.Medium?56:64;
                    var bounds=iconBadge.TransformToVisual(root).TransformBounds(new Rect(0,0,iconBadge.ActualWidth,iconBadge.ActualHeight));
                    var childBounds=child.TransformToVisual(root).TransformBounds(new Rect(0,0,child.ActualWidth,child.ActualHeight));
                    Require(iconBadge.IsLoaded&&child.IsLoaded&&!iconBadge.IsTabStop&&iconBadge.ActualWidth==extent&&iconBadge.ActualHeight==extent&&
                        bounds.X>=0&&bounds.Y>=0&&bounds.Right<=400&&bounds.Bottom<=160&&
                        childBounds.X>=bounds.X-.5&&childBounds.Y>=bounds.Y-.5&&
                        childBounds.Right<=bounds.Right+.5&&childBounds.Bottom<=bounds.Bottom+.5,
                        $"Icon Badge {id} child clipped at native 150% scale: badge={bounds}, child={childBounds}.");
                    iconBadgeScale.Add(new{theme=theme.ToString(),kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                }
                root.Children.Clear();await Layout();
                var overflowChild=new Border{Width=100,Height=80,Background=new SolidColorBrush(Microsoft.UI.Colors.Red)};
                var overflowBadge=new BleeckerIconBadge{Size=BleeckerIconBadgeSize.Medium,Content=overflowChild};
                Canvas.SetLeft(overflowBadge,24);Canvas.SetTop(overflowBadge,24);root.Children.Add(overflowBadge);await Layout();
                var ownerBounds=overflowBadge.TransformToVisual(root).TransformBounds(new Rect(0,0,overflowBadge.ActualWidth,overflowBadge.ActualHeight));
                var overflowBounds=overflowChild.TransformToVisual(root).TransformBounds(new Rect(0,0,overflowChild.ActualWidth,overflowChild.ActualHeight));
                Require(overflowBounds.Right>ownerBounds.Right+6&&overflowBounds.X>=0&&overflowBounds.Y>=0&&overflowBounds.Right<=400&&overflowBounds.Bottom<=160,
                    $"Icon Badge overflow child did not extend within the native root at 150%: badge={ownerBounds}, child={overflowBounds}.");
                Guard();var overflowFrame=new RenderTargetBitmap();await overflowFrame.RenderAsync(root);Guard();
                Require(overflowFrame.PixelWidth==600&&overflowFrame.PixelHeight==240,"Icon Badge native overflow capture used the wrong scale.");
                var overflowPixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await overflowFrame.GetPixelsAsync());
                var sampleX=(int)Math.Round((ownerBounds.Right+5)*1.5);
                var sampleY=(int)Math.Round((ownerBounds.Y+ownerBounds.Height/2)*1.5);
                var pixel=(sampleY*overflowFrame.PixelWidth+sampleX)*4;
                Require(overflowPixels[pixel]==0&&overflowPixels[pixel+1]==0&&overflowPixels[pixel+2]==255&&overflowPixels[pixel+3]==255,
                    "Icon Badge clipped its red child outside the badge at native 150% scale.");
                iconBadgeScale.Add(new{theme=theme.ToString(),kind="overflow",scale=root.XamlRoot.RasterizationScale,passed=true,
                    badgeBounds=new{x=ownerBounds.X,y=ownerBounds.Y,width=ownerBounds.Width,height=ownerBounds.Height},
                    childBounds=new{x=overflowBounds.X,y=overflowBounds.Y,width=overflowBounds.Width,height=overflowBounds.Height},
                    sampledRedOutsideBadge=true});
                root.Children.Clear();await Layout();
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                stage=$"progress/{theme}/150-percent";
                root.RequestedTheme=theme;
                var cases=new List<(BleeckerProgress control,string id)>();
                var sizes=Enum.GetValues<BleeckerProgressSize>();
                var variants=Enum.GetValues<BleeckerProgressVariant>();
                for(var index=0;index<sizes.Length*variants.Length;index++)
                {
                    var size=sizes[index%3];var variant=variants[index/3];
                    var control=new BleeckerProgress{Width=180,ProgressSize=size,Variant=variant,Value=65,ShowLabel=true};
                    Canvas.SetLeft(control,8+(index/6)*192);Canvas.SetTop(control,6+(index%6)*24);
                    root.Children.Add(control);cases.Add((control,$"{variant}/{size}"));
                }
                await Layout();
                Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Progress did not render at native 150% scale.");
                foreach(var(control,id) in cases)
                {
                    var owner=control.TransformToVisual(root).TransformBounds(new Rect(0,0,control.ActualWidth,control.ActualHeight));
                    var track=Find<Border>(control,"PART_Track")!;
                    var fill=Find<Border>(control,"PART_Fill")!;
                    var label=Find<TextBlock>(control,"PART_Label")!;
                    var trackBounds=track.TransformToVisual(root).TransformBounds(new Rect(0,0,track.ActualWidth,track.ActualHeight));
                    var fillBounds=fill.TransformToVisual(root).TransformBounds(new Rect(0,0,fill.ActualWidth,fill.ActualHeight));
                    var labelBounds=label.TransformToVisual(root).TransformBounds(new Rect(0,0,label.ActualWidth,label.ActualHeight));
                    Require(control.IsLoaded&&!control.IsTabStop&&control.ActualWidth==180&&control.ActualHeight==16&&
                        owner.X>=0&&owner.Y>=0&&owner.Right<=400&&owner.Bottom<=160&&
                        trackBounds.X>=owner.X-.5&&trackBounds.Right<=owner.Right+.5&&
                        fillBounds.X>=trackBounds.X-.5&&fillBounds.Right<=trackBounds.Right+.5&&
                        labelBounds.X>=owner.X-.5&&labelBounds.Right<=owner.Right+.5&&
                        trackBounds.Y>=owner.Y-.5&&trackBounds.Bottom<=owner.Bottom+.5&&
                        fillBounds.Y>=owner.Y-.5&&fillBounds.Bottom<=owner.Bottom+.5&&
                        labelBounds.Y>=owner.Y-.5&&labelBounds.Bottom<=owner.Bottom+.5,
                        $"Progress {id} clipped at native 150% scale: owner={owner}, track={trackBounds}, fill={fillBounds}, label={labelBounds}.");
                    progressScale.Add(new{theme=theme.ToString(),kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                }
                Guard();var progressFrame=new RenderTargetBitmap();await progressFrame.RenderAsync(root);Guard();
                Require(progressFrame.PixelWidth==600&&progressFrame.PixelHeight==240,"Progress native capture used the wrong scale.");
                root.Children.Clear();await Layout();
            }
            root.Height=520;island.SiteBridge.OverrideScale=1.5f;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(600,780));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,600,780));
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=theme;
                var cases=new (string id,BleeckerErrorState control)[]{
                    ("default",new BleeckerErrorState{Width=320}),
                    ("retry",new BleeckerErrorState{Width=320,Title="Fictional archive unavailable",Description="Retry the consumer-owned archive request.",RetryCommand=new RetryCommand()}),
                    ("custom",new BleeckerErrorState{Width=320,Title="Custom icon",Description="",Icon=new Border{Width=28,Height=28}}),
                    ("long",new BleeckerErrorState{Width=280,Title="A longer fictional failure title wraps within the consumer's chosen width",Description="The consumer retains this complete description. No service or recovery policy is inferred by the component."}),
                    ("rtl",new BleeckerErrorState{Width=320,FlowDirection=FlowDirection.RightToLeft,Title="العربية · archive",Description="Consumer-owned right-to-left summary."}),
                    ("empty-title",new BleeckerErrorState{Width=320,Title="",Description="Consumer description"}),
                    ("empty-text",new BleeckerErrorState{Width=320,Title="",Description=""})
                };
                foreach(var(id,control) in cases)
                {
                    stage=$"error-state/{theme}/{id}/150-percent";
                    Canvas.SetLeft(control,32);Canvas.SetTop(control,16);root.Children.Add(control);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Error State did not render at native 150% scale.");
                    var owner=control.TransformToVisual(root).TransformBounds(new Rect(0,0,control.ActualWidth,control.ActualHeight));
                    Require(control.IsLoaded&&control.ActualWidth==control.Width&&control.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Error State {id} escaped its native 150% host: {owner}.");
                    foreach(var name in new[]{"PART_IconBox","PART_Title","PART_Description","PART_Retry"})
                    {
                        var part=Find<FrameworkElement>(control,name)??throw new InvalidOperationException($"Error State {id} lost {name}.");
                        if(part.Visibility==Visibility.Collapsed||part.ActualHeight==0)continue;
                        var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                        Require(bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5&&
                            part.ActualHeight+.5>=part.DesiredSize.Height,
                            $"Error State {id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                    }
                    errorStateScale.Add(new{theme=theme.ToString(),kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=theme;
                var cases=new (string id,BleeckerSectionHeader control)[]{
                    ("full",new BleeckerSectionHeader{Width=320,Eyebrow="Workspace",Title="Editorial",Description="Review fictional stories before publication."}),
                    ("title",new BleeckerSectionHeader{Width=320,Title="Editorial"}),
                    ("eyebrow",new BleeckerSectionHeader{Width=320,Eyebrow="Workspace"}),
                    ("summary",new BleeckerSectionHeader{Width=320,Description="Consumer-owned section summary."}),
                    ("wrapped",new BleeckerSectionHeader{Width=280,Eyebrow="Workspace",Title="A longer fictional editorial workspace title",Description="A consumer-owned description wraps within the chosen space."}),
                    ("rtl",new BleeckerSectionHeader{Width=320,FlowDirection=FlowDirection.RightToLeft,Eyebrow="Workspace",Title="Editorial",Description="Consumer-owned right-to-left description."}),
                    ("disabled",new BleeckerSectionHeader{Width=320,Title="Unavailable section",IsEnabled=false}),
                    ("empty",new BleeckerSectionHeader{Width=320,Title="",Eyebrow="",Description=""})
                };
                foreach(var(id,control) in cases)
                {
                    stage=$"section-header/{theme}/{id}/150-percent";
                    Canvas.SetLeft(control,32);Canvas.SetTop(control,16);root.Children.Add(control);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001,"Section Header did not render at native 150% scale.");
                    var owner=control.TransformToVisual(root).TransformBounds(new Rect(0,0,control.ActualWidth,control.ActualHeight));
                    Require(control.IsLoaded&&!control.IsTabStop&&control.ActualWidth==control.Width&&
                        owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height&&
                        (id=="empty"?control.ActualHeight==0:control.ActualHeight>0),
                        $"Section Header {id} escaped its native 150% host: {owner}.");
                    foreach(var name in new[]{"PART_Eyebrow","PART_Title","PART_Description"})
                    {
                        var part=Find<FrameworkElement>(control,name)??throw new InvalidOperationException($"Section Header {id} lost {name}.");
                        if(part.Visibility==Visibility.Collapsed||part.ActualHeight==0)continue;
                        var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                        Require(bounds.X>=owner.X-.5&&bounds.Right<=owner.Right+.5&&
                            bounds.Y>=owner.Y-4&&bounds.Bottom<=owner.Bottom+4&&part.ActualHeight+.5>=part.DesiredSize.Height,
                            $"Section Header {id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                    }
                    sectionHeaderScale.Add(new{theme=theme.ToString(),kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                {
                    root.RequestedTheme=theme;
                    BleeckerPageHeader Full(double width)=>new(){Width=width,Title="Fictional editorial workspace",Description="Review consumer-owned drafts and local status before publishing.",Breadcrumbs=new TextBlock{Text="Workspace / Editorial"},BackHref="#fictional-editorial",Actions=new UIElement[]{new BleeckerButton{Content="Save draft",Size=BleeckerButtonSize.Small},new BleeckerButton{Content="Preview",Size=BleeckerButtonSize.Small}}};
                    var cases=viewport==400?new (string id,BleeckerPageHeader control)[]{
                        ("full",Full(320)),
                        ("title",new BleeckerPageHeader{Width=320,Title="Editorial"}),
                        ("breadcrumbs",new BleeckerPageHeader{Width=320,Breadcrumbs=new TextBlock{Text="Workspace / Editorial"}}),
                        ("actions",new BleeckerPageHeader{Width=320,Actions=new UIElement[]{new BleeckerButton{Content="Save draft",Size=BleeckerButtonSize.Small},new BleeckerButton{Content="Preview",Size=BleeckerButtonSize.Small}}}),
                        ("back",new BleeckerPageHeader{Width=320,Title="Editorial",BackHref="#fictional-editorial"}),
                        ("wrapped",new BleeckerPageHeader{Width=280,Title="A longer fictional editorial workspace title",Description="A consumer-owned description wraps within this constrained page header while retaining every word."}),
                        ("rtl",new BleeckerPageHeader{Width=320,FlowDirection=FlowDirection.RightToLeft,Title="Editorial",Description="Consumer-owned right-to-left summary.",BackHref="#fictional-editorial"}),
                        ("disabled",new BleeckerPageHeader{Width=320,Title="Unavailable section",BackHref="#fictional-editorial",IsEnabled=false}),
                        ("empty",new BleeckerPageHeader{Width=320})
                    }:new (string id,BleeckerPageHeader control)[]{("wide",Full(600)),("wide-narrow-container",Full(360))};
                    foreach(var(id,control) in cases)
                    {
                        stage=$"page-header/{theme}/{viewport}/{id}/150-percent";
                        Canvas.SetLeft(control,32);Canvas.SetTop(control,16);root.Children.Add(control);await Layout();
                        Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Page Header did not use the requested native 150% scale and viewport.");
                        var owner=control.TransformToVisual(root).TransformBounds(new Rect(0,0,control.ActualWidth,control.ActualHeight));
                        Require(control.IsLoaded&&!control.IsTabStop&&control.ActualWidth==control.Width&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height&&
                            (id=="empty"?control.ActualHeight==0:control.ActualHeight>0),$"Page Header {id} escaped its native 150% host: {owner}.");
                        foreach(var name in new[]{"PART_Breadcrumbs","PART_Title","PART_Description","PART_Back","PART_Actions"})
                        {
                            var part=Find<FrameworkElement>(control,name)??throw new InvalidOperationException($"Page Header {id} lost {name}.");
                            if(part.Visibility==Visibility.Collapsed||part.ActualHeight==0)continue;
                            var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                            Require(bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5&&
                                (part is TextBlock text?part.ActualHeight+4>=part.DesiredSize.Height-text.Margin.Top-text.Margin.Bottom:true),
                                $"Page Header {id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                        }
                        if(control.Actions is UIElement[] actions)foreach(var action in actions)
                        {
                            var element=(FrameworkElement)action;
                            var bounds=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
                            Require(element.IsLoaded&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                                $"Page Header {id} action clipped at native 150% scale: owner={owner}, action={bounds}.");
                        }
                        pageHeaderScale.Add(new{theme=theme.ToString(),viewport,kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                        root.Children.Clear();await Layout();
                    }
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                {
                    root.RequestedTheme=theme;
                    BleeckerDashboardSection Full(double width)=>new(){Width=width,Title="Monitoring",Description="Fictional local queue summary.",Action=new Border{Width=80,Height=32,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,166,93,87))},Content=new Border{Height=64,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,185,199,209))}};
                    var cases=viewport==400?new (string id,BleeckerDashboardSection control)[]{
                        ("full",Full(320)),
                        ("wrapped",new BleeckerDashboardSection{Width=280,Title="A longer fictional monitoring section title",Description="Consumer-owned description wraps inside this constrained section.",Content=new Border{Height=64}}),
                        ("empty",new BleeckerDashboardSection{Width=320})
                    }:new (string id,BleeckerDashboardSection control)[]{
                        ("wide",Full(600)),
                        ("overflow",new BleeckerDashboardSection{Width=160,Title="Monitoring",Description="Consumer-owned section summary.",Action=new Border{Width=80,Height=32,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,166,93,87))},Content=new Border{Height=64}}),
                        ("rtl",new BleeckerDashboardSection{Width=320,FlowDirection=FlowDirection.RightToLeft,Title="Monitoring",Description="Consumer-owned right-to-left summary.",Action=new Border{Width=80,Height=32},Content=new Border{Height=64}})
                    };
                    foreach(var(id,control) in cases)
                    {
                        stage=$"dashboard-section/{theme}/{viewport}/{id}/150-percent";
                        Canvas.SetLeft(control,32);Canvas.SetTop(control,16);root.Children.Add(control);await Layout();
                        Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Dashboard Section did not use native 150% scale and viewport.");
                        var owner=control.TransformToVisual(root).TransformBounds(new Rect(0,0,control.ActualWidth,control.ActualHeight));
                        Require(control.IsLoaded&&!control.IsTabStop&&control.ActualWidth==control.Width&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height&&
                            (id=="empty"?control.ActualHeight==0:control.ActualHeight>0),$"Dashboard Section {id} escaped its native 150% host: {owner}.");
                        foreach(var name in new[]{"PART_Title","PART_Description","PART_Action","PART_Content"})
                        {
                            var part=Find<FrameworkElement>(control,name)??throw new InvalidOperationException($"Dashboard Section {id} lost {name}.");
                            if(part.Visibility==Visibility.Collapsed||part.ActualHeight==0)continue;
                            var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                            Require(bounds.X>=0&&bounds.Y>=0&&bounds.Right<=root.Width+.5&&bounds.Bottom<=root.Height+.5&&
                                (id=="overflow"||bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5)&&
                                (part is TextBlock text?part.ActualHeight+4>=part.DesiredSize.Height-text.Margin.Top-text.Margin.Bottom:true),
                                $"Dashboard Section {id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                        }
                        if(id=="overflow")
                        {
                            var action=control.Action as Border??throw new InvalidOperationException("Dashboard Section overflow action is missing.");
                            var bounds=action.TransformToVisual(root).TransformBounds(new Rect(0,0,action.ActualWidth,action.ActualHeight));
                            Require(bounds.X<owner.Right&&bounds.Right>owner.Right+1&&bounds.Right<=root.Width,$"Dashboard Section action did not retain visible overflow at native 150% scale: owner={owner}, action={bounds}.");
                            Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();
                            var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());
                            var x=(int)(((owner.Right+bounds.Right)/2)*1.5);
                            var y=(int)((bounds.Y+bounds.Height/2)*1.5);
                            var offset=(y*bitmap.PixelWidth+x)*4;
                            Require(bitmap.PixelWidth==(int)(viewport*1.5)&&offset+3<pixels.Length&&pixels[offset]==87&&pixels[offset+1]==93&&pixels[offset+2]==166,
                                "Dashboard Section's intentional action overflow was not painted at native 150% scale.");
                        }
                        dashboardSectionScale.Add(new{theme=theme.ToString(),viewport,kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                        root.Children.Clear();await Layout();
                    }
                }
            }
            foreach(var viewport in new[]{400,900,1280})
            {
                root.Width=viewport;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                foreach(var rtl in viewport==900?new[]{false,true}:new[]{false})
                {
                    stage=$"dashboard-grid/{theme}/{viewport}/{(rtl?"rtl":"default")}/150-percent";
                    root.RequestedTheme=theme;
                    var grid=new BleeckerDashboardGrid{Width=viewport==400?320:viewport==900?600:960,FlowDirection=rtl?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                    foreach(var span in new[]{BleeckerDashboardSpanSize.One,BleeckerDashboardSpanSize.Two,BleeckerDashboardSpanSize.Full,BleeckerDashboardSpanSize.One})
                        grid.Children.Add(new BleeckerDashboardSpan{Span=span,Content=new Border{Height=40,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,185,199,209))}});
                    Canvas.SetLeft(grid,32);Canvas.SetTop(grid,16);root.Children.Add(grid);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Dashboard Grid did not use native 150% scale and viewport.");
                    var owner=grid.TransformToVisual(root).TransformBounds(new Rect(0,0,grid.ActualWidth,grid.ActualHeight));
                    Require(grid.IsLoaded&&!grid.IsTabStop&&grid.ActualWidth==grid.Width&&grid.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Dashboard Grid escaped its native 150% host: {owner}.");
                    foreach(var span in grid.Children.OfType<BleeckerDashboardSpan>())
                    {
                        var bounds=span.TransformToVisual(root).TransformBounds(new Rect(0,0,span.ActualWidth,span.ActualHeight));
                        var content=(FrameworkElement)span.Content;
                        var body=content.TransformToVisual(root).TransformBounds(new Rect(0,0,content.ActualWidth,content.ActualHeight));
                        Require(span.IsLoaded&&!span.IsTabStop&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5&&
                            body.X>=bounds.X-.5&&body.Y>=bounds.Y-.5&&body.Right<=bounds.Right+.5&&body.Bottom<=bounds.Bottom+.5&&content.ActualHeight>0,
                            $"Dashboard Grid span/content clipped at native 150% scale: owner={owner}, span={bounds}, content={body}.");
                    }
                    dashboardGridScale.Add(new{theme=theme.ToString(),viewport,rtl,scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                foreach(var variant in Enum.GetValues<BleeckerCardVariant>())
                {
                    stage=$"card/{theme}/{viewport}/{variant}/150-percent";
                    root.RequestedTheme=theme;
                    var heading=new TextBlock{Text="Fictional editorial review",TextWrapping=TextWrapping.Wrap};
                    var description=new TextBlock{Text="Consumer-owned notes wrap inside this card without losing any content at native display scale.",TextWrapping=TextWrapping.Wrap};
                    var action=new Button{Content="Open review",Width=144,HorizontalAlignment=HorizontalAlignment.Left};
                    var body=new StackPanel{Spacing=12,Children={heading,description,action}};
                    var card=new BleeckerCard{Width=viewport==400?320:600,Variant=variant,PaddingSize=(BleeckerCardPadding)((int)variant%4),Content=body};
                    Canvas.SetLeft(card,32);Canvas.SetTop(card,16);root.Children.Add(card);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Card did not use native 150% scale and viewport.");
                    var owner=card.TransformToVisual(root).TransformBounds(new Rect(0,0,card.ActualWidth,card.ActualHeight));
                    Require(card.IsLoaded&&!card.IsTabStop&&card.ActualWidth==card.Width&&card.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Card {variant} escaped its native 150% host: {owner}.");
                    foreach(var element in new FrameworkElement[]{heading,description,action})
                    {
                        var bounds=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
                        Require(element.IsLoaded&&element.ActualHeight>0&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5&&
                            element.ActualHeight+4>=element.DesiredSize.Height,
                            $"Card {variant} consumer content clipped at native 150% scale: card={owner}, content={bounds}.");
                    }
                    cardScale.Add(new{theme=theme.ToString(),viewport,variant=variant.ToString(),padding=card.PaddingSize.ToString(),scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                {
                    root.RequestedTheme=theme;
                    BleeckerEmpty Full(double width)=>new(){Width=width,Title="No fictional items",Description="Create a local item to begin this editorial review.",Action=new Button{Content="Create item",Width=120}};
                    var cases=viewport==400?new (string id,BleeckerEmpty control)[]{
                        ("full",Full(320)),
                        ("wrapped",new BleeckerEmpty{Width=280,Title="A longer fictional monitoring empty state",Description="Consumer-owned guidance wraps inside this constrained empty surface without losing words.",Action=new Button{Content="Open review",Width=120}}),
                        ("custom-icon",new BleeckerEmpty{Width=320,Title="Local archive",Icon=new Border{Width=24,Height=24,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,180,55,90))}}),
                        ("empty-text",new BleeckerEmpty{Width=320,Title="",Description=null})
                    }:new (string id,BleeckerEmpty control)[]{("wide",Full(600)),("wide-wrapped",new BleeckerEmpty{Width=600,Title="A longer fictional editorial empty state",Description="Consumer-owned description remains centered and readable at a wide viewport.",Action=new Button{Content="Create item",Width=120}})};
                    foreach(var(id,control) in cases)
                    {
                        stage=$"empty/{theme}/{viewport}/{id}/150-percent";
                        Canvas.SetLeft(control,32);Canvas.SetTop(control,16);root.Children.Add(control);await Layout();
                        Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Empty did not use native 150% scale and viewport.");
                        var owner=control.TransformToVisual(root).TransformBounds(new Rect(0,0,control.ActualWidth,control.ActualHeight));
                        Require(control.IsLoaded&&!control.IsTabStop&&control.ActualWidth==control.Width&&control.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                            $"Empty {id} escaped its native 150% host: {owner}.");
                        foreach(var name in new[]{"EmptyIcon","EmptyBody","EmptyTitle","EmptyDescription","EmptyAction"})
                        {
                            var part=Find<FrameworkElement>(control,name)??throw new InvalidOperationException($"Empty {id} lost {name}.");
                            if(part.Visibility==Visibility.Collapsed||part.ActualHeight==0)continue;
                            var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                            Require(bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                                $"Empty {id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                        }
                        if(control.Action is FrameworkElement action)
                        {
                            var bounds=action.TransformToVisual(root).TransformBounds(new Rect(0,0,action.ActualWidth,action.ActualHeight));
                            Require(action.IsLoaded&&action.ActualHeight>0&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                                $"Empty {id} consumer action clipped at native 150% scale: owner={owner}, action={bounds}.");
                        }
                        emptyScale.Add(new{theme=theme.ToString(),viewport,kind=id,scale=root.XamlRoot.RasterizationScale,passed=true});
                        root.Children.Clear();await Layout();
                    }
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;root.Height=520;island.SiteBridge.OverrideScale=1.5f;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                foreach(var sample in new[]{
                    (id:"normal",labels:new[]{"Published","Draft"},values:new[]{6d,4d},showLegend:true),
                    (id:"wrapped",labels:new[]{"Fictional editorial items awaiting approval","Draft"},values:new[]{6d,4d},showLegend:true),
                    (id:"hidden",labels:new[]{"Published","Draft"},values:new[]{6d,4d},showLegend:false),
                    (id:"empty",labels:Array.Empty<string>(),values:Array.Empty<double>(),showLegend:true)})
                {
                    stage=$"progress-stack/{theme}/{viewport}/{sample.id}/150-percent";root.RequestedTheme=theme;
                    var stack=new BleeckerProgressStack{Width=viewport==400?280:600,ShowLegend=sample.showLegend};
                    for(var i=0;i<sample.labels.Length;i++)stack.Items.Add(new BleeckerProgressStackItem{Label=sample.labels[i],Value=sample.values[i]});
                    Canvas.SetLeft(stack,32);Canvas.SetTop(stack,16);root.Children.Add(stack);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Progress Stack did not use native 150% scale and viewport.");
                    var owner=stack.TransformToVisual(root).TransformBounds(new Rect(0,0,stack.ActualWidth,stack.ActualHeight));
                    Require(stack.IsLoaded&&!stack.IsTabStop&&stack.ActualWidth==stack.Width&&stack.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Progress Stack {sample.id} escaped its native 150% host: {owner}.");
                    foreach(var name in new[]{"ProgressStackTrack","ProgressStackLegend","ProgressStackSegment","ProgressStackItem","ProgressStackLabel","ProgressStackValue"})
                    foreach(var part in FindAll<FrameworkElement>(stack,name))
                    {
                        if(part.Visibility==Visibility.Collapsed||part.ActualHeight==0||part.ActualWidth==0)continue;
                        var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                        Require(bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                            $"Progress Stack {sample.id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                    }
                    progressStackScale.Add(new{theme=theme.ToString(),viewport,kind=sample.id,scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;root.Height=520;island.SiteBridge.OverrideScale=1.5f;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                foreach(var kind in new[]{"number","currency","wrapped","rich","missing"})
                {
                    stage=$"metric/{theme}/{viewport}/{kind}/150-percent";root.RequestedTheme=theme;
                    var metric=new BleeckerMetric{Width=viewport==400?280:600,Language="en-US",Value=1248};
                    Button? child=null;
                    if(kind=="currency"){metric.Format=BleeckerMetricFormat.Currency;metric.Currency="EUR";metric.Prefix="About ";}
                    if(kind=="wrapped")metric.Suffix=" fictional editorial reviews awaiting a local decision";
                    if(kind=="rich"){child=new Button{Content="Details",Width=80};metric.Suffix=child;}
                    if(kind=="missing"){metric.Value=null;metric.Prefix="No local data";}
                    Canvas.SetLeft(metric,32);Canvas.SetTop(metric,16);root.Children.Add(metric);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Metric did not use native 150% scale and viewport.");
                    var owner=metric.TransformToVisual(root).TransformBounds(new Rect(0,0,metric.ActualWidth,metric.ActualHeight));
                    Require(metric.IsLoaded&&!metric.IsTabStop&&metric.ActualWidth==metric.Width&&metric.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Metric {kind} escaped its native 150% host: {owner}.");
                    foreach(var name in new[]{"PART_Text","PART_Slots"})
                    {
                        var part=Find<FrameworkElement>(metric,name)??throw new InvalidOperationException($"Metric {kind} lost {name}.");
                        var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                        Require(part.IsLoaded&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                            $"Metric {kind} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                    }
                    if(child is not null)
                    {
                        var bounds=child.TransformToVisual(root).TransformBounds(new Rect(0,0,child.ActualWidth,child.ActualHeight));
                        Require(child.IsLoaded&&child.ActualHeight>0&&metric.Clip is null&&bounds.X>=0&&bounds.Y>=0&&bounds.Right<=root.Width&&bounds.Bottom<=root.Height,
                            $"Metric consumer slot clipped by its native 150% host: owner={owner}, child={bounds}.");
                    }
                    metricScale.Add(new{theme=theme.ToString(),viewport,kind,scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;root.Height=520;island.SiteBridge.OverrideScale=1.5f;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                foreach(var sample in new[]{
                    (id:"normal",values:new double?[]{10,30,20,40},showArea:false),
                    (id:"empty",values:Array.Empty<double?>(),showArea:false),
                    (id:"gaps",values:new double?[]{10,null,30},showArea:false),
                    (id:"area",values:new double?[]{10,30,20,40},showArea:true),
                    (id:"negative",values:new double?[]{-12,-8,-15,-4},showArea:false)})
                {
                    stage=$"sparkline/{theme}/{viewport}/{sample.id}/150-percent";root.RequestedTheme=theme;
                    var sparkline=new BleeckerSparkline{Width=viewport==400?280:600,Height=40,DataKey="v",ShowArea=sample.showArea};
                    foreach(var value in sample.values)sparkline.Data.Add(new Dictionary<string,object?>{{"v",value}});
                    Canvas.SetLeft(sparkline,32);Canvas.SetTop(sparkline,16);root.Children.Add(sparkline);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Sparkline did not use native 150% scale and viewport.");
                    var owner=sparkline.TransformToVisual(root).TransformBounds(new Rect(0,0,sparkline.ActualWidth,sparkline.ActualHeight));
                    Require(sparkline.IsLoaded&&!sparkline.IsTabStop&&sparkline.ActualWidth==sparkline.Width&&sparkline.ActualHeight==40&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Sparkline {sample.id} escaped its native 150% host: {owner}.");
                    foreach(var name in new[]{"PART_Chart","PART_Plot"})
                    {
                        var part=Find<FrameworkElement>(sparkline,name)??throw new InvalidOperationException($"Sparkline {sample.id} lost {name}.");
                        var bounds=part.TransformToVisual(root).TransformBounds(new Rect(0,0,part.ActualWidth,part.ActualHeight));
                        Require(part.IsLoaded&&part.ActualWidth>0&&part.ActualHeight>0&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                            $"Sparkline {sample.id} {name} clipped at native 150% scale: owner={owner}, part={bounds}.");
                    }
                    sparklineScale.Add(new{theme=theme.ToString(),viewport,kind=sample.id,scale=root.XamlRoot.RasterizationScale,passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            foreach(var viewport in new[]{400,900})
            {
                root.Width=viewport;root.Height=520;island.SiteBridge.OverrideScale=1.5f;
                window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((int)(viewport*1.5),780));
                island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,(int)(viewport*1.5),780));
                foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
                foreach(var kind in new[]{"normal","wrapped","collapsed","empty"})
                {
                    stage=$"feed-grid/{theme}/{viewport}/{kind}/150-percent";root.RequestedTheme=theme;
                    var grid=new BleeckerFeedGrid{Width=viewport==400?320:724};
                    if(kind!="empty")
                    {
                        grid.Children.Add(new TextBlock{Text=kind=="wrapped"?"A long fictional editorial label wraps within the available grid column at native scale.":"Fictional update",TextWrapping=TextWrapping.Wrap});
                        grid.Children.Add(new Button{Content="Open fictional update"});
                        grid.Children.Add(new Border{Height=24,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,120,140,160))});
                        if(kind=="collapsed")grid.Children[1].Visibility=Visibility.Collapsed;
                    }
                    Canvas.SetLeft(grid,32);Canvas.SetTop(grid,16);root.Children.Add(grid);await Layout();
                    Require(Math.Abs(root.XamlRoot.RasterizationScale-1.5)<.0001&&Math.Abs(root.XamlRoot.Size.Width-viewport)<.01,"Feed Grid did not use native 150% scale and viewport.");
                    var owner=grid.TransformToVisual(root).TransformBounds(new Rect(0,0,grid.ActualWidth,grid.ActualHeight));
                    Require(grid.IsLoaded&&!grid.IsTabStop&&grid.ActualWidth==grid.Width&&grid.ActualHeight>0&&owner.X>=0&&owner.Y>=0&&owner.Right<=root.Width&&owner.Bottom<=root.Height,
                        $"Feed Grid {kind} escaped its native 150% host: {owner}.");
                    foreach(var child in grid.Children.OfType<FrameworkElement>().Where(item=>item.Visibility==Visibility.Visible))
                    {
                        var bounds=child.TransformToVisual(root).TransformBounds(new Rect(0,0,child.ActualWidth,child.ActualHeight));
                        Require(child.IsLoaded&&child.ActualWidth>0&&child.ActualHeight>0&&bounds.X>=owner.X-.5&&bounds.Y>=owner.Y-.5&&bounds.Right<=owner.Right+.5&&bounds.Bottom<=owner.Bottom+.5,
                            $"Feed Grid {kind} child clipped at native 150% scale: owner={owner}, child={bounds}.");
                    }
                    feedGridScale.Add(new{theme=theme.ToString(),viewport,kind,scale=root.XamlRoot.RasterizationScale,visibleChildren=grid.Children.Count(item=>item.Visibility==Visibility.Visible),passed=true});
                    root.Children.Clear();await Layout();
                }
            }
            root.Width=400;
            root.Height=160;
            island.SiteBridge.OverrideScale=1;
            window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(400,160));
            island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,400,160));
            var styleNames=new[]{"BleeckerSmallToggleStyle","BleeckerToggleStyle","BleeckerLargeToggleStyle","BleeckerSmallOutlineToggleStyle","BleeckerOutlineToggleStyle","BleeckerLargeOutlineToggleStyle"};
            var labels=new[]{"Small","Medium","Large","S outline","M outline","L outline"};
            var scaledToggles=new ToggleButton[styleNames.Length];var toggleChanges=0;
            for(var i=0;i<styleNames.Length;i++)
            {
                var toggle=new ToggleButton{Width=108,Content=labels[i],IsChecked=true,Style=(Style)Application.Current.Resources[styleNames[i]]};
                AutomationProperties.SetName(toggle,$"Fictional {labels[i]} toggle");
                toggle.Checked+=(_,_)=>toggleChanges++;toggle.Unchecked+=(_,_)=>toggleChanges++;
                Canvas.SetLeft(toggle,24+(i%3)*122);Canvas.SetTop(toggle,12+(i/3)*68);root.Children.Add(toggle);scaledToggles[i]=toggle;
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var scale in new[]{1f,1.25f,1.5f,2f})
            {
                stage=$"toggle/{theme}/{scale}";root.RequestedTheme=theme;island.SiteBridge.OverrideScale=scale;var width=(int)Math.Round(400*scale);var height=(int)Math.Round(160*scale);window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(width,height));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,width,height));await Layout();
                var states=scaledToggles.Select((toggle,i)=>
                {
                    var peer=FrameworkElementAutomationPeer.CreatePeerForElement(toggle);var bounds=toggle.TransformToVisual(root).TransformBounds(new Rect(0,0,toggle.ActualWidth,toggle.ActualHeight));
                    return new{id=styleNames[i],label=labels[i],name=peer.GetName(),width=toggle.ActualWidth,height=toggle.ActualHeight,bounds=new{x=bounds.X,y=bounds.Y,width=bounds.Width,height=bounds.Height},checkedState=toggle.IsChecked,loaded=toggle.IsLoaded,tabStop=toggle.IsTabStop};
                }).ToArray();
                Require(states.All((state)=>state.loaded&&state.checkedState==true&&state.tabStop&&state.width==108&&state.height>=32&&state.bounds.x>=24&&state.bounds.y>=12&&state.bounds.x+state.bounds.width<=400&&state.bounds.y+state.bounds.height<=160),"Scaled native Toggle style lost geometry or selection.");
                Require(toggleChanges==0,"Theme or rasterization scale changed a caller Toggle selection.");
                toggleObservations.Add(new{theme=theme.ToString(),requestedScale=scale,actualRasterizationScale=root.XamlRoot.RasterizationScale,states,toggleChanges});
                Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var bytes=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());var file=$"toggle-scale-{theme}-{scale.ToString(System.Globalization.CultureInfo.InvariantCulture)}.png";
                using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,bytes);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream);await reader.LoadAsync((uint)stream.Size);var png=new byte[(int)stream.Size];reader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),png);toggleFrames.Add(new{theme=theme.ToString(),requestedScale=scale,width=bitmap.PixelWidth,height=bitmap.PixelHeight,file,sha256=Convert.ToHexStringLower(SHA256.HashData(png))});
                Require(bitmap.PixelWidth==width&&bitmap.PixelHeight==height&&Math.Abs(root.XamlRoot.RasterizationScale-scale)<.0001,"Toggle capture did not use the requested native rasterization scale.");
            }
            root.Children.Clear();root.RequestedTheme=ElementTheme.Light;island.SiteBridge.OverrideScale=1;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(400,160));island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,400,160));
            var defaultInput=new BleeckerInput{Width=240,InputSize=BleeckerInputSize.Small};root.Children.Add(defaultInput);stage="input/default-small";await Layout();
            Require(defaultInput.FontSize==13&&defaultInput.Padding==new Thickness(12,0,12,0)&&defaultInput.MinHeight==36,"Default small Input lost its size recipe.");
            defaultInput.InputSize=BleeckerInputSize.Large;await Layout();Require(defaultInput.FontSize==15&&defaultInput.Padding==new Thickness(16,0,16,0)&&defaultInput.MinHeight==44,"Default large Input lost its live size recipe.");inputOverrides.Add(new{id="default-size",passed=true});
            root.Children.Clear();var localInput=new BleeckerInput{Width=240,Height=36,FontSize=23,Padding=new Thickness(21,0,27,0),MinHeight=18,InputSize=BleeckerInputSize.Large};root.Children.Add(localInput);stage="input/local-overrides";await Layout();
            Require(localInput.FontSize==23&&localInput.Padding==new Thickness(21,0,27,0)&&localInput.MinHeight==18,"Input size recipe replaced caller local font, padding or minimum height.");
            localInput.InputSize=BleeckerInputSize.Small;localInput.StartIcon="S";localInput.EndIcon="E";await Layout();Require(localInput.FontSize==23&&localInput.Padding==new Thickness(21,0,27,0)&&localInput.MinHeight==18,"Live Input size or adornments replaced caller local geometry.");inputOverrides.Add(new{id="local-overrides",passed=true});
            root.Children.Clear();var producer=new TextBlock{FontSize=20,Padding=new Thickness(19,0,25,0),MinHeight=17};var boundInput=new BleeckerInput{Width=240};boundInput.SetBinding(Control.FontSizeProperty,new Binding{Source=producer,Path=new PropertyPath(nameof(TextBlock.FontSize))});boundInput.SetBinding(Control.PaddingProperty,new Binding{Source=producer,Path=new PropertyPath(nameof(TextBlock.Padding))});boundInput.SetBinding(FrameworkElement.MinHeightProperty,new Binding{Source=producer,Path=new PropertyPath(nameof(TextBlock.MinHeight))});root.Children.Add(boundInput);stage="input/bound-overrides";await Layout();
            producer.FontSize=22;producer.Padding=new Thickness(17,0,31,0);producer.MinHeight=19;boundInput.InputSize=BleeckerInputSize.Large;boundInput.StartIcon="S";await Layout();Require(boundInput.FontSize==22&&boundInput.Padding==new Thickness(17,0,31,0)&&boundInput.MinHeight==19&&boundInput.GetBindingExpression(Control.FontSizeProperty)is not null&&boundInput.GetBindingExpression(Control.PaddingProperty)is not null&&boundInput.GetBindingExpression(FrameworkElement.MinHeightProperty)is not null,"Input size recipe replaced caller geometry bindings.");inputOverrides.Add(new{id="bound-overrides",passed=true});
            root.Children.Clear();var sameInput=new BleeckerInput{Width=240};root.Children.Add(sameInput);stage="input/same-valued-local";await Layout();sameInput.FontSize=14;sameInput.InputSize=BleeckerInputSize.Large;await Layout();Require(sameInput.FontSize==14&&sameInput.GetBindingExpression(Control.FontSizeProperty)is null,"Input replaced a caller font value equal to the former default.");inputOverrides.Add(new{id="same-valued-local",passed=true});
            root.Children.Clear();var baseStyle=new Style{TargetType=typeof(BleeckerInput)};baseStyle.Setters.Add(new Setter(Control.TemplateProperty,defaultInput.Template));baseStyle.Setters.Add(new Setter(Control.FontSizeProperty,19d));baseStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(22,0,28,0)));baseStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty,31d));var callerStyle=new Style{TargetType=typeof(BleeckerInput),BasedOn=baseStyle};var styledInput=new BleeckerInput{Width=240,InputSize=BleeckerInputSize.Large,Style=callerStyle};root.Children.Add(styledInput);stage="input/basedon-style";await Layout();Require(styledInput.FontSize==19&&styledInput.Padding==new Thickness(22,0,28,0)&&styledInput.MinHeight==31,"Input size recipe replaced a BasedOn caller style.");styledInput.InputSize=BleeckerInputSize.Small;styledInput.EndIcon="E";await Layout();Require(styledInput.FontSize==19&&styledInput.Padding==new Thickness(22,0,28,0)&&styledInput.MinHeight==31,"Live Input recipe replaced the BasedOn style.");inputOverrides.Add(new{id="basedon-style",passed=true});
            root.Children.Clear();var textBaseStyle=new Style{TargetType=typeof(TextBox)};textBaseStyle.Setters.Add(new Setter(Control.FontSizeProperty,21d));textBaseStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(20,0,26,0)));textBaseStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty,29d));var derivedStyle=new Style{TargetType=typeof(BleeckerInput),BasedOn=textBaseStyle};derivedStyle.Setters.Add(new Setter(Control.TemplateProperty,defaultInput.Template));var baseStyledInput=new BleeckerInput{Width=240,InputSize=BleeckerInputSize.Large,Style=derivedStyle};root.Children.Add(baseStyledInput);stage="input/textbox-base-style";await Layout();Require(baseStyledInput.FontSize==21&&baseStyledInput.Padding==new Thickness(20,0,26,0)&&baseStyledInput.MinHeight==29,"Input size recipe replaced caller geometry inherited from a TextBox style.");baseStyledInput.InputSize=BleeckerInputSize.Small;await Layout();Require(baseStyledInput.FontSize==21&&baseStyledInput.Padding==new Thickness(20,0,26,0)&&baseStyledInput.MinHeight==29,"Live Input size replaced caller TextBox base-style geometry.");inputOverrides.Add(new{id="textbox-base-style",passed=true});
            root.Children.Clear();var implicitBaseHost=new Grid();var implicitBaseStyle=new Style{TargetType=typeof(BleeckerInput),BasedOn=textBaseStyle};implicitBaseStyle.Setters.Add(new Setter(Control.TemplateProperty,defaultInput.Template));implicitBaseHost.Resources[typeof(BleeckerInput)]=implicitBaseStyle;var implicitBaseInput=new BleeckerInput{Width=240,InputSize=BleeckerInputSize.Large};implicitBaseHost.Children.Add(implicitBaseInput);root.Children.Add(implicitBaseHost);stage="input/implicit-textbox-base-style";await Layout();Require(implicitBaseInput.FontSize==21&&implicitBaseInput.Padding==new Thickness(20,0,26,0)&&implicitBaseInput.MinHeight==29,"Input size recipe replaced ancestor style geometry inherited from TextBox.");implicitBaseInput.InputSize=BleeckerInputSize.Small;await Layout();Require(implicitBaseInput.FontSize==21&&implicitBaseInput.Padding==new Thickness(20,0,26,0)&&implicitBaseInput.MinHeight==29,"Live Input size replaced ancestor TextBox base-style geometry.");inputOverrides.Add(new{id="implicit-textbox-base-style",passed=true});
            root.Children.Clear();root.Children.Add(localInput);stage="input/template-and-remount";var inputTemplate=localInput.Template;localInput.Template=null;await Layout();localInput.Template=inputTemplate;await Layout();root.Children.Remove(localInput);await Layout();root.Children.Add(localInput);await Layout();Require(localInput.FontSize==23&&localInput.Padding==new Thickness(21,0,27,0)&&localInput.MinHeight==18,"Input template replacement or remount lost caller geometry.");inputOverrides.Add(new{id="template-and-remount",passed=true});
            stage="input/clear-restores-default";localInput.ClearValue(Control.FontSizeProperty);localInput.ClearValue(Control.PaddingProperty);localInput.ClearValue(FrameworkElement.MinHeightProperty);await Layout();Require(localInput.FontSize==13&&localInput.Padding==new Thickness(40,0,40,0)&&localInput.MinHeight==36,"Clearing Input caller geometry did not restore the live small/adorned recipe.");inputOverrides.Add(new{id="clear-restores-default",passed=true});
            root.Children.Clear();var implicitHost=new Grid();var implicitStyle=new Style{TargetType=typeof(BleeckerInput)};implicitStyle.Setters.Add(new Setter(Control.TemplateProperty,defaultInput.Template));implicitStyle.Setters.Add(new Setter(Control.FontSizeProperty,18d));implicitStyle.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(23,0,29,0)));implicitStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty,30d));implicitHost.Resources[typeof(BleeckerInput)]=implicitStyle;var implicitInput=new BleeckerInput{Width=240,InputSize=BleeckerInputSize.Large};implicitHost.Children.Add(implicitInput);root.Children.Add(implicitHost);stage="input/ancestor-implicit-style";await Layout();Require(implicitInput.FontSize==18&&implicitInput.Padding==new Thickness(23,0,29,0)&&implicitInput.MinHeight==30,"Input size recipe replaced an ancestor implicit style.");implicitInput.InputSize=BleeckerInputSize.Small;await Layout();Require(implicitInput.FontSize==18&&implicitInput.Padding==new Thickness(23,0,29,0)&&implicitInput.MinHeight==30,"Live Input size replaced ancestor style geometry.");inputOverrides.Add(new{id="ancestor-implicit-style",passed=true});
            Guard();GC.KeepAlive(island);await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v3",passed=true,scope="owned hidden DesktopWindowXamlSource SiteBridge.OverrideScale for Separator, Status Badge, Switch, Loading Spinner, Context Menu, Brand Mark, Eyebrow, Label, Icon Badge, Progress, Error State, Section Header, Page Header, Dashboard Section, Dashboard Grid, Card, Empty, Progress Stack, Metric, Sparkline, Feed Grid and native Toggle styles; Input caller geometry contracts; no physical monitor DPI or system text-scale change",observations,frames,toggleObservations,toggleFrames,inputOverrides,contextMenuScale,brandMarkScale,eyebrowScale,labelScale,iconBadgeScale,progressScale,errorStateScale,sectionHeaderScale,pageHeaderScale,dashboardSectionScale,dashboardGridScale,cardScale,emptyScale,progressStackScale,metricScale,sparklineScale,feedGridScale,guards,focus,activations,isolatedRasterizationScaleVerified=true,isolatedStatusBadgeRasterizationVerified=true,isolatedSwitchRasterizationVerified=true,isolatedSpinnerRasterizationVerified=true,isolatedToggleRasterizationVerified=true,inputOverrideContractsVerified=true,displayScalingVerified=false,systemTextScalingVerified=false,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v3",passed=false,stage,error=error.ToString(),observations,frames,toggleObservations,toggleFrames,inputOverrides,labelScale,iconBadgeScale,sectionHeaderScale,pageHeaderScale,dashboardSectionScale,dashboardGridScale,cardScale,emptyScale,progressStackScale,metricScale,sparklineScale,feedGridScale,guards,focus,activations,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)}));Environment.Exit(1);}
    }
}
