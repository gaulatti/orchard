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
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static T? Find<T>(DependencyObject root,string name) where T:FrameworkElement {if(root is T match&&match.Name==name)return match;for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(Find<T>(VisualTreeHelper.GetChild(root,i),name)is{ } found)return found;return null;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var observations=new List<object>();var frames=new List<object>();var toggleObservations=new List<object>();var toggleFrames=new List<object>();var inputOverrides=new List<object>();var contextMenuScale=new List<object>();var brandMarkScale=new List<object>();var eyebrowScale=new List<object>();var labelScale=new List<object>();var stage="initial";var guards=0;var focus=0;var activations=0;
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
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(400,160));root.Arrange(new Rect(0,0,400,160));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
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
            Guard();GC.KeepAlive(island);await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v3",passed=true,scope="owned hidden DesktopWindowXamlSource SiteBridge.OverrideScale for Separator, Status Badge, Switch, Loading Spinner, Context Menu, Brand Mark, Eyebrow, Label and native Toggle styles; Input caller geometry contracts; no physical monitor DPI or system text-scale change",observations,frames,toggleObservations,toggleFrames,inputOverrides,contextMenuScale,brandMarkScale,eyebrowScale,labelScale,guards,focus,activations,isolatedRasterizationScaleVerified=true,isolatedStatusBadgeRasterizationVerified=true,isolatedSwitchRasterizationVerified=true,isolatedSpinnerRasterizationVerified=true,isolatedToggleRasterizationVerified=true,inputOverrideContractsVerified=true,displayScalingVerified=false,systemTextScalingVerified=false,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-content-scale-native-v3",passed=false,stage,error=error.ToString(),observations,frames,toggleObservations,toggleFrames,inputOverrides,labelScale,guards,focus,activations,librarySha256=Hash(typeof(BleeckerSeparator).Assembly.Location),hostSha256=Hash(hostAssembly)}));Environment.Exit(1);}
    }
}
