using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Path = System.IO.Path;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class TooltipModalBranchProbe
{
    private delegate bool WindowVisitor(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumWindows(WindowVisitor visitor, nint parameter);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node)
    {
        if (node is FrameworkElement element) yield return element;
        for (var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++) foreach(var child in Elements(VisualTreeHelper.GetChild(node,i))) yield return child;
    }
    internal static async Task RunAsync(Window window, string reference, string output, string hostPath)
    {
        var bytes=await File.ReadAllBytesAsync(reference);using var document=JsonDocument.Parse(bytes);
        var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}'/>");
        root.UseLayoutRounding=false;
        var canvas=new Canvas {Width=900,Height=760,UseLayoutRounding=false};root.Children.Add(canvas);window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(900,760));
        var guards=0;var focus=0;var activations=0;var frames=new List<object>();var cases=new List<object>();var captures=new List<object>();
        var windowClosed=false;
        window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};root.GotFocus+=(_,_)=>focus++;
        void Require(bool value,string message){if(!value){File.WriteAllText(Path.Combine(Path.GetDirectoryName(output)!,"partial-tooltip-state.json"),JsonSerializer.Serialize(new{message,guards,focus,activations,frames,cases}));throw new InvalidOperationException(message);}}
        void Guard()
        {
            guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((handle,_)=>{GetWindowThreadProcessId(handle,out var owner);visible|=owner==Environment.ProcessId&&IsWindowVisible(handle);return true;},0);
            Require(!visible&&(windowClosed||!window.Visible&&!window.AppWindow.IsVisible)&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Tooltip fixture acquired visibility, activation or focus.");
        }
        async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Tooltip layout could not drain.");await idle.Task;await Task.Delay(12);root.Measure(new Size(900,760));root.Arrange(new Rect(0,0,900,760));root.UpdateLayout();Guard();}}
        object Box(FrameworkElement element){var b=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
        string Color(Brush brush){var c=((SolidColorBrush)brush).Color;return $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";}
        async Task Close(BleeckerTooltip tip){tip.IsOpen=false;await Task.Delay(180);await Layout();Require(!tip.PresentationMounted&&!tip.IsLoaded,"Tooltip retained a closed presentation.");}
        async Task Capture(string file)
        {
            var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();Require(bitmap.PixelWidth==900&&bitmap.PixelHeight==760,"Tooltip capture was resized.");var buffer=await bitmap.GetPixelsAsync();var pixels=new byte[buffer.Length];using(var reader=Windows.Storage.Streams.DataReader.FromBuffer(buffer))reader.ReadBytes(pixels);
            using var stream=new Windows.Storage.Streams.InMemoryRandomAccessStream();var encoder=await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Premultiplied,900,760,96,96,pixels);await encoder.FlushAsync();using var pngReader=new Windows.Storage.Streams.DataReader(stream.GetInputStreamAt(0));await pngReader.LoadAsync((uint)stream.Size);var png=new byte[stream.Size];pngReader.ReadBytes(png);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),png);captures.Add(new{file,sha256=Convert.ToHexStringLower(SHA256.HashData(png)),width=900,height=760});
        }
        BleeckerModalHost? host=null;
        foreach(var mode in new[]{"native","hosted"})
        {
            if(mode=="hosted"){root.Children.Remove(canvas);host=new BleeckerModalHost{Content=canvas};root.Children.Add(host);await Layout();}
            foreach(var source in document.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("spec").GetProperty("scope").GetString()=="background"))
            {
                var scheme=source.GetProperty("scheme").GetString()!;var spec=source.GetProperty("spec");root.RequestedTheme=scheme=="Dark"?ElementTheme.Dark:ElementTheme.Light;
                var target=new Button{Content="Fictional help",Width=120,Height=36,UseLayoutRounding=false};Canvas.SetLeft(target,spec.GetProperty("x").GetDouble());Canvas.SetTop(target,spec.GetProperty("y").GetDouble());canvas.Children.Add(target);await Layout();
                object originalContent=spec.GetProperty("slot").GetString()=="fixed"?new Border{Width=120,Height=39}:"Fictional library guidance";
                var tip=new BleeckerTooltip{Content=originalContent,Placement=Enum.Parse<PlacementMode>(spec.GetProperty("side").GetString()!,true)};BleeckerTooltipService.SetToolTip(target,tip);var opens=0;var closes=0;tip.Opened+=(_,_)=>opens++;tip.Closed+=(_,_)=>closes++;tip.IsOpen=true;await Layout();
                var surface=Elements(tip).OfType<BleeckerOverlaySurface>().Single();var arrow=Elements(tip).OfType<Polygon>().Single();var popups=VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot);
                Require(tip.PresentationMounted&&tip.IsLoaded&&tip.IsOpen&&ReferenceEquals(tip.Content,originalContent)&&AutomationProperties.GetDescribedBy(target).Contains(tip),"Tooltip lost original content, mounting or description.");Require(popups.Count==(mode=="native"?1:0),"Tooltip used the wrong presentation path.");
                var privateRoot=VisualTreeHelper.GetParent(tip) as Border;var frame=privateRoot is null?null:VisualTreeHelper.GetParent(privateRoot) as Border;if(mode=="hosted")Require(frame is not null&&Canvas.GetZIndex(frame)==180&&frame.IsHitTestVisible,"Unowned Tooltip lost source z-180 ordering.");
                frames.Add(new{mode,scheme,spec=JsonSerializer.Deserialize<object>(spec.GetRawText()),body=Box(surface),arrow=Box(arrow),trigger=Box(target),side=tip.ActualPlacement.ToString().ToLowerInvariant(),background=Color(surface.Background),foreground=Color(tip.Foreground),arrowFill=Color(arrow.Fill),role=FrameworkElementAutomationPeer.CreatePeerForElement(tip).GetAutomationControlType().ToString(),sourceFile=source.GetProperty("file").GetString(),originalContentRetained=true,describedBy=true,popupCount=popups.Count,zIndex=frame is null?(int?)null:Canvas.GetZIndex(frame)});
                if(spec.GetProperty("slot").GetString()=="fixed"&&spec.GetProperty("side").GetString()=="top"&&!spec.GetProperty("edge").GetBoolean())await Capture($"tooltip-{mode}-{scheme}.png");
                await Close(tip);Require(opens==1&&closes==1,"Tooltip lifecycle duplicated notifications.");BleeckerTooltipService.SetToolTip(target,null);canvas.Children.Remove(target);await Layout();
            }
        }
        foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            root.RequestedTheme=scheme;var declaration=new Border{Width=0,Height=0};canvas.Children.Add(declaration);var ownerCanvas=new Canvas{Width=900,Height=760,UseLayoutRounding=false};var surface=new Grid{UseLayoutRounding=false};surface.Children.Add(ownerCanvas);var overlay=new Grid{UseLayoutRounding=false};overlay.Children.Add(surface);var owner=host!.Attach(declaration,surface,overlay,()=>{});host.Activate(owner);await Layout();
            foreach(var source in document.RootElement.GetProperty("cases").EnumerateArray().Where(c=>c.GetProperty("scheme").GetString()==scheme.ToString()&&c.GetProperty("spec").GetProperty("scope").GetString()=="modal"))
            {
                var spec=source.GetProperty("spec");var trigger=source.GetProperty("measurement").GetProperty("trigger");var target=new Button{Content="Fictional help",Width=120,Height=36,UseLayoutRounding=false};Canvas.SetLeft(target,trigger.GetProperty("x").GetDouble());Canvas.SetTop(target,trigger.GetProperty("y").GetDouble());ownerCanvas.Children.Add(target);await Layout();
                var content=new Border{Width=120,Height=39};var tip=new BleeckerTooltip{Content=content,Placement=Enum.Parse<PlacementMode>(spec.GetProperty("side").GetString()!,true)};BleeckerTooltipService.SetToolTip(target,tip);tip.IsOpen=true;await Layout();var body=Elements(tip).OfType<BleeckerOverlaySurface>().Single();var arrow=Elements(tip).OfType<Polygon>().Single();
                Require(owner.Layer.Branches.Count==1&&ReferenceEquals(owner.Layer.Branches[0].Root,VisualTreeHelper.GetParent(tip))&&Canvas.GetZIndex(owner.Layer.Branches[0].Frame)==51&&host.AllowsFocusTarget(tip),"Tooltip did not join its actual modal owner.");Require(VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Count==0,"Owned Tooltip opened a native Popup.");
                frames.Add(new{mode="owned",scheme=scheme.ToString(),spec=JsonSerializer.Deserialize<object>(spec.GetRawText()),body=Box(body),arrow=Box(arrow),trigger=Box(target),side=tip.ActualPlacement.ToString().ToLowerInvariant(),background=Color(body.Background),foreground=Color(tip.Foreground),arrowFill=Color(arrow.Fill),role=FrameworkElementAutomationPeer.CreatePeerForElement(tip).GetAutomationControlType().ToString(),sourceFile=source.GetProperty("file").GetString(),originalContentRetained=true,describedBy=AutomationProperties.GetDescribedBy(target).Contains(tip),popupCount=0,zIndex=51});
                if(spec.GetProperty("side").GetString()=="top")
                {
                    var otherDeclaration=new Border{Width=0,Height=0};canvas.Children.Add(otherDeclaration);var otherSurface=new Grid();var otherOverlay=new Grid();otherOverlay.Children.Add(otherSurface);var other=host.Attach(otherDeclaration,otherSurface,otherOverlay,()=>{});host.Activate(other);await Layout();
                    Require(tip.IsOpen&&tip.IsLoaded&&!owner.Layer.Branches[0].Frame.IsHitTestVisible&&!host.AllowsFocusTarget(tip)&&!BleeckerTooltip.DismissForEscape(root.XamlRoot),"Inactive Tooltip handled the active modal's Escape.");cases.Add(new{name="inactive-owned-tooltip",scheme=scheme.ToString(),requestRetained=true,inactiveEscapeRejected=true,callerEnabled=target.IsEnabled});
                    host.Deactivate(other);host.Detach(other);canvas.Children.Remove(otherDeclaration);await Layout();Require(tip.IsOpen&&owner.Layer.Branches[0].Frame.IsHitTestVisible&&ReferenceEquals(tip.Content,content),"Tooltip did not resume the original content.");Require(BleeckerTooltip.DismissForEscape(root.XamlRoot)&&owner.Layer.Active,"Escape planning dismissed the tooltip's modal.");await Task.Delay(180);await Layout();Require(!tip.PresentationMounted&&owner.Layer.Branches.Count==0,"Tooltip Escape left a portaled branch.");cases.Add(new{name="resume-and-escape-priority",scheme=scheme.ToString(),originalContentRetained=true,parentRetained=true,actualKeyboardDeliveryVerified=false});
                }
                else await Close(tip);
                BleeckerTooltipService.SetToolTip(target,null);ownerCanvas.Children.Remove(target);await Layout();
            }
            var hintTarget=new Button{Content="Fictional help",Width=120,Height=36};ownerCanvas.Children.Add(hintTarget);
            var select=new BleeckerSelect{Options=new[]{new BleeckerSelectOption("a","Fictional Alpha"),new BleeckerSelectOption("b","Fictional Beta")},Value="a",Width=180};Canvas.SetLeft(select,240);ownerCanvas.Children.Add(select);await Layout();
            var hint=new BleeckerTooltip{Content="Fictional modal guidance"};BleeckerTooltipService.SetToolTip(hintTarget,hint);hint.IsOpen=true;await Layout();
            var hintRoot=(Border)VisualTreeHelper.GetParent(hint);var chooser=Elements(select).OfType<BleeckerSelectSurface>().Single();chooser.IsDropDownOpen=true;await Layout();
            var core=Elements(host).OfType<BleeckerModalLayerHostCore>().Single();var corePeer=FrameworkElementAutomationPeer.CreatePeerForElement(core);
            bool HasTooltip(AutomationPeer peer)=>peer.GetAutomationControlType()==AutomationControlType.ToolTip||(peer.GetChildren()?.Any(HasTooltip)??false);
            Require(hint.IsOpen&&!hintRoot.IsHitTestVisible&&!HasTooltip(corePeer)&&!BleeckerTooltip.DismissForEscape(root.XamlRoot),"Select did not pause the older tooltip's input and peer scope.");
            chooser.IsDropDownOpen=false;await Layout();Require(hint.IsOpen&&hintRoot.IsHitTestVisible&&HasTooltip(corePeer),"Closing Select did not resume the original tooltip and peer.");
            cases.Add(new{name="select-suspends-and-resumes-tooltip",scheme=scheme.ToString(),requestRetained=true,inactivePointerBlocked=true,inactivePeerExcluded=true,resumedPeerPresent=true});await Close(hint);BleeckerTooltipService.SetToolTip(hintTarget,null);
            chooser.IsDropDownOpen=true;await Layout();var option=chooser.Items.OfType<BleeckerSelectItem>().First();var childHint=new BleeckerTooltip{Content="Fictional option guidance"};var childClosed=0;childHint.Closed+=(_,_)=>childClosed++;BleeckerTooltipService.SetToolTip(option,childHint);childHint.IsOpen=true;await Layout();
            Require(((Border)VisualTreeHelper.GetParent(childHint)).IsHitTestVisible&&HasTooltip(corePeer)&&BleeckerPopupScope.Contains(root.XamlRoot,chooser,childHint),"Tooltip declared in Select did not join its logical active scope.");
            chooser.IsDropDownOpen=false;await Layout();Require(!childHint.IsOpen&&!childHint.PresentationMounted&&childClosed==1,"Unloading an original option did not close its tooltip once.");BleeckerTooltipService.SetToolTip(option,null);cases.Add(new{name="tooltip-in-select-and-owner-unload",scheme=scheme.ToString(),activePointerAllowed=true,activePeerIncluded=true,logicalContainment=true,closedOnce=true});
            ownerCanvas.Children.Remove(hintTarget);ownerCanvas.Children.Remove(select);host.Deactivate(owner);host.Detach(owner);canvas.Children.Remove(declaration);await Layout();
        }
        foreach(var scheme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            root.RequestedTheme=scheme;var help=new BleeckerFieldHelpTooltip{Text="Fictional field guidance"};canvas.Children.Add(help);await Layout();var helpTip=BleeckerTooltipService.GetToolTip(help)!;var helpClosed=0;helpTip.Closed+=(_,_)=>helpClosed++;helpTip.IsOpen=true;await Layout();Require(helpTip.IsLoaded&&AutomationProperties.GetDescribedBy(help).Contains(helpTip),"Field help did not use its original shared Tooltip.");help.Text="";await Layout();Require(help.Visibility==Visibility.Collapsed&&!helpTip.IsOpen&&!helpTip.PresentationMounted&&helpClosed==1&&!AutomationProperties.GetDescribedBy(help).Contains(helpTip),"Empty field help did not release its original tooltip once.");cases.Add(new{name="field-help-service-lifetime",scheme=scheme.ToString(),originalTooltip=true,descriptionLinked=true,emptyHelpClosedOnce=true,descriptionReleased=true});canvas.Children.Remove(help);await Layout();
            root.RequestedTheme=scheme;var fixture=new Orchard.Catalog.TooltipsFixture{Width=900};canvas.Children.Add(fixture);await Layout();var popup=Elements(fixture).OfType<BleeckerPopover>().Single(p=>p.Name=="TooltipPopover");popup.Open=true;await Layout();var target=Elements(root).OfType<Button>().Single(b=>b.Name=="NestedTooltipTrigger");var tip=BleeckerTooltipService.GetToolTip(target)!;tip.IsOpen=true;await Layout();Require(popup.IsOpen&&tip.IsLoaded&&BleeckerPopupScope.Contains(root.XamlRoot,popup,tip),"Catalog Tooltip/Popover composition lost its original branch.");await Capture($"tooltip-catalog-{scheme}.png");popup.Open=false;await Layout();Require(!tip.IsOpen&&!tip.PresentationMounted,"Catalog popup teardown retained its Tooltip.");cases.Add(new{name="actual-catalog-tooltip-popover",scheme=scheme.ToString(),originalFixture=true,logicalContainment=true,closedWithOwner=true});canvas.Children.Remove(fixture);await Layout();
        }
        var closeTarget=new Button{Content="Fictional close boundary",Width=120,Height=36};canvas.Children.Add(closeTarget);await Layout();var closeTip=new BleeckerTooltip{Content="Fictional close guidance"};BleeckerTooltipService.SetToolTip(closeTarget,closeTip);var closeNotifications=0;closeTip.Closed+=(_,_)=>closeNotifications++;closeTip.IsOpen=true;await Layout();
        var keeper=new Window{Content=new Grid()};window.Close();windowClosed=true;await Task.Delay(180);Guard();Require(!closeTip.IsOpen&&!closeTip.PresentationMounted&&closeNotifications==1,"Closing a loaded root retained Tooltip state or lost its close notification.");BleeckerTooltipService.SetToolTip(closeTarget,null);cases.Add(new{name="window-close-with-live-tooltip",closedOnce=true,requestClosed=true,branchReleased=true});
        await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-tooltip-modal-branch-v1",passed=true,sourceSha256=Convert.ToHexStringLower(SHA256.HashData(bytes)),librarySha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(typeof(BleeckerTooltip).Assembly.Location))),hostSha256=Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(hostPath))),frames,cases,captures,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,nativeInputFocusVerified=false,nativeComponentAccepted=false,sourceRasterParityVerified=false},new JsonSerializerOptions{WriteIndented=true}));keeper.Close();Environment.Exit(0);
    }
}
