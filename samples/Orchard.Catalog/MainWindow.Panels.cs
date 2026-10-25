using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Ellipse = Microsoft.UI.Xaml.Shapes.Ellipse;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class MainWindow
{
    private async Task RunPanelsAsync(string folder)
    {
        using var stream = CatalogReferenceResources.Open(typeof(MainWindow).Assembly, "Orchard.Catalog.Reference.Panel.json") ?? throw new InvalidOperationException("Panel reference is missing.");
        using var reference = await JsonDocument.ParseAsync(stream);
        var previous = Content; var originalSize = AppWindow.ClientSize;
        var canvas = (Canvas)XamlReader.Load("<Canvas xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' UseLayoutRounding='False'/>");
        var anchor = new Button { Width = 20, Height = 20, Opacity = 0 }; Canvas.SetLeft(anchor, 840); Canvas.SetTop(anchor, 620); canvas.Children.Add(anchor);
        var frames = new List<object>(); var interactions = new List<object>(); var captures = new Dictionary<string,string>();
        object Box(FrameworkElement node)
        { var bounds = node.TransformToVisual(canvas).TransformBounds(new Rect(0, 0, node.ActualWidth, node.ActualHeight)); return new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height }; }
        static object Paint(Brush? brush) => brush is SolidColorBrush solid ? new { color = $"#{solid.Color.A:X2}{solid.Color.R:X2}{solid.Color.G:X2}{solid.Color.B:X2}", opacity = solid.Opacity } : new { color = "#00000000", opacity = 1d };
        static bool Flag(JsonElement spec, string key) => spec.TryGetProperty(key, out var value) && value.GetBoolean();
        static double Number(JsonElement spec, string key, double fallback) => spec.TryGetProperty(key, out var value) ? value.GetDouble() : fallback;
        static object? Scalar(JsonElement spec, string key) => !spec.TryGetProperty(key, out var value) ? null : value.ValueKind switch
        { JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false, JsonValueKind.Number => value.GetDouble(), JsonValueKind.Null => null, _ => throw new InvalidOperationException("Unsupported Panel scalar.") };
        static SolidColorBrush Accent(JsonElement spec)
        {
            var text = spec.GetProperty("accent").GetString();
            return text == "rgba(24, 90, 130, .4)" ? new SolidColorBrush(Windows.UI.Color.FromArgb(255,24,90,130)) { Opacity = .4 }
                : new SolidColorBrush(Windows.UI.Color.FromArgb(255,64,127,163));
        }
        async Task Scroll(ScrollViewer viewer, double offset)
        {
            viewer.ChangeView(null, offset, null, true); var target = Math.Min(offset, viewer.ScrollableHeight); var clock = Stopwatch.StartNew();
            while (Math.Abs(viewer.VerticalOffset - target) >= .001)
            { if (clock.ElapsedMilliseconds > 3000) throw new InvalidOperationException("Panel scroll position did not settle."); await Task.Delay(10); }
            await AwaitPresentationAsync(viewer);
        }
        try
        {
            Content = canvas; AppWindow.ResizeClient(new SizeInt32(900,680)); await AwaitPresentationAsync(canvas);
            using var input = new NativeInputSession(canvas.XamlRoot, folder, "panel"); await input.AcquireAsync(canvas); AppWindow.ResizeClient(new SizeInt32(900,680)); await AwaitPresentationAsync(canvas); await input.MoveAsync(anchor);
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray())
            {
                var spec = item.GetProperty("spec"); var theme = Enum.Parse<ElementTheme>(item.GetProperty("scheme").GetString()!); canvas.RequestedTheme = theme;
                var body = new PageFrameBody { Height = Number(spec,"bodyHeight",600), VerticalAlignment = VerticalAlignment.Top };
                var filterBody = new PageFrameBody(Windows.UI.Color.FromArgb(255,141,182,156)) { Height = 32 };
                var dragBody = new PageFrameBody(Windows.UI.Color.FromArgb(255,161,165,192)) { Width = 18, Height = 18 };
                var toolA = new PageFrameBody(Windows.UI.Color.FromArgb(255,199,158,114)) { Height = 24, VerticalAlignment = VerticalAlignment.Center };
                var toolB = new PageFrameBody(Windows.UI.Color.FromArgb(255,166,93,87)) { Height = 32, VerticalAlignment = VerticalAlignment.Center };
                var tools = new PanelToolsBody(toolA,toolB) { Height = 32, HorizontalAlignment = HorizontalAlignment.Center, UseLayoutRounding = false };
                var slots = spec.TryGetProperty("slots",out var slotValue) ? slotValue.GetString() : "none"; var hasFilter = slots is "filter" or "all"; var hasToolbar = slots is "toolbar" or "all";
                object? callerContent = Flag(spec,"empty") ? null : spec.TryGetProperty("scalar",out var scalar) ? scalar.GetString() : body;
                var panel = new BleeckerPanel { Title = spec.TryGetProperty("title",out var titleValue) ? titleValue.GetString()! : "Live coverage",
                    Width = Number(spec,"panelWidth",spec.GetProperty("width").GetDouble()), Height = spec.GetProperty("height").GetDouble(),
                    Filter = hasFilter ? filterBody : Scalar(spec,"filter"), Toolbar = hasToolbar ? tools : Scalar(spec,"toolbar"), DragHandle = Flag(spec,"drag") ? dragBody : Scalar(spec,"dragValue"),
                    Content = callerContent,
                    FlowDirection = Flag(spec,"rtl") ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Grow = Flag(spec,"grow"), IsDragging = Flag(spec,"isDragging") };
                if (spec.TryGetProperty("countKind",out var kind)) panel.Count = kind.GetString() switch { "nan" => double.NaN, "infinity" => double.PositiveInfinity, "negative-infinity" => double.NegativeInfinity, _ => throw new InvalidOperationException("Unknown Panel count kind.") };
                else if (spec.TryGetProperty("count",out var countValue) && countValue.ValueKind != JsonValueKind.Null) panel.Count = countValue.GetDouble();
                if (spec.TryGetProperty("accent",out _)) panel.Accent = Accent(spec);
                Canvas.SetLeft(panel,20); Canvas.SetTop(panel,20); canvas.Children.Add(panel); await AwaitPresentationAsync(panel);
                var parts = Desc<FrameworkElement>(panel).ToArray(); FrameworkElement Part(string name) => parts.Single(node=>node.Name==name);
                var feed = (ScrollViewer)Part("PanelFeed"); await Scroll(feed,item.GetProperty("measurement").GetProperty("scroll").GetProperty("offset").GetDouble());
                var count = (Border)Part("PanelCount"); var filter = (Border)Part("PanelFilter"); var toolbar = (Border)Part("PanelToolbar"); var handle = Part("PanelHandle"); var header = (Border)Part("PanelHeader");
                var text = parts.OfType<TextBlock>().Single(node=>node.Name=="PanelTitleText"); var countText = parts.OfType<TextBlock>().Single(node=>node.Name=="PanelCountTextText");
                var titleSlot = Part("PanelTitle"); var countSlot = Part("PanelCountText");
                double Intrinsic(FrameworkElement slot,string name) => (double)(slot.GetType().GetProperty(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(slot)!);
                var bodies = new Dictionary<string,object>(); if (ReferenceEquals(panel.Content,body)) bodies["content"] = Box(body);
                if (hasFilter) bodies["filter"] = Box(filterBody); if (Flag(spec,"drag")) bodies["drag"] = Box(dragBody); if (hasToolbar) { bodies["tool-a"]=Box(toolA); bodies["tool-b"]=Box(toolB); }
                var peer = FrameworkElementAutomationPeer.CreatePeerForElement(panel) ?? throw new InvalidOperationException("Panel group peer is missing.");
                frames.Add(new { id = item.GetProperty("id").GetString(), scheme = theme.ToString(), measurement = new {
                    panel=Box(panel),header=Box(header),row=Box(Part("PanelRow")),title=new{value=panel.Title,painted=text.Text,box=Box(titleSlot),minimumWidth=Intrinsic(titleSlot,"MinimumWidth"),naturalWidth=Intrinsic(titleSlot,"NaturalWidth"),fontFamily=text.FontFamily.Source,fontSize=text.FontSize,lineHeight=text.LineHeight,tracking=text.CharacterSpacing},
                    count=count.Visibility==Visibility.Visible?new{value=countText.Text,box=Box(count),textBox=Box(countSlot)}:null,dot=Box(Part("PanelDot")),drag=handle.Visibility==Visibility.Visible?Box(handle):null,
                    filter=filter.Visibility==Visibility.Visible?Box(filter):null,toolbar=toolbar.Visibility==Visibility.Visible?Box(toolbar):null,feed=Box(feed),bodies,
                    scroll=new{offset=feed.VerticalOffset,clientWidth=Math.Floor(feed.ViewportWidth+.5),clientHeight=Math.Floor(feed.ViewportHeight+.5),extent=Math.Floor(Math.Max(feed.ExtentHeight,feed.ViewportHeight)+.5),nativeExtent=feed.ExtentHeight,nativeViewport=feed.ViewportHeight},
                    paint=new{headerBackground=Paint(header.Background),headerBorder=Paint(header.BorderBrush),title=Paint(text.Foreground),countBackground=Paint(count.Background),countBorder=Paint(count.BorderBrush),count=Paint(countText.Foreground),toolbarBackground=Paint(toolbar.Background),toolbarBorder=Paint(toolbar.BorderBrush),feedBackground=Paint(feed.Background),left=Paint(((Border)Part("PART_LeftAccent")).Background),right=Paint(((Grid)Part("PART_Root")).BorderBrush),dot=Paint(((Ellipse)Part("PanelDot")).Fill),dotOpacity=((Ellipse)Part("PanelDot")).Opacity},
                    passive=!panel.IsTabStop,viewportFocusable=feed.IsTabStop,group=peer.GetAutomationControlType()==AutomationControlType.Group,name=peer.GetName(),contentRetained=callerContent is UIElement?ReferenceEquals(panel.Content,callerContent):Equals(panel.Content,callerContent),filterRetained=!hasFilter||ReferenceEquals(panel.Filter,filterBody),toolbarRetained=!hasToolbar||ReferenceEquals(panel.Toolbar,tools),dragRetained=!Flag(spec,"drag")||ReferenceEquals(panel.DragHandle,dragBody) } });
                if(item.TryGetProperty("file",out var file))
                {
                    var capturePath = Path.Combine(folder,file.GetString()!); await NativeScreenCapture.CaptureClientAsync(input.Window,capturePath);
                    captures[file.GetString()!] = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(capturePath)));
                }
                canvas.Children.Remove(panel);
                if(frames.Count%32==0) await File.WriteAllTextAsync(Path.Combine(folder,"panel-progress.json"),JsonSerializer.Serialize(new{passed=false,frames},new JsonSerializerOptions{WriteIndented=true}));
            }
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=theme;
                var filter = new BleeckerInput { Text="Filter value" }; var editor=new BleeckerInput{Text="Body value",VerticalAlignment=VerticalAlignment.Top};
                var body = new Grid { Height=600 }; body.Children.Add(editor);
                var action=new BleeckerButton{Content="Consumer action"};var drag=new BleeckerButton{Content="Consumer handle"};var actions=0;var handles=0;action.Click+=(_,_)=>actions++;drag.Click+=(_,_)=>handles++;
                var accent=new SolidColorBrush(Windows.UI.Color.FromArgb(255,64,127,163));var producer=new TextBlock{Tag=12};
                var panel=new BleeckerPanel{Title="Local panel",Width=400,Height=360,Filter=filter,Toolbar=action,DragHandle=drag,Content=body,Accent=accent};
                panel.SetBinding(BleeckerPanel.CountProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
                Canvas.SetLeft(panel,20);Canvas.SetTop(panel,20);canvas.Children.Add(panel);await AwaitPresentationAsync(panel);
                await input.ClickAsync(filter);await input.KeyAsync(0x41,filter);await input.ClickAsync(editor);await input.KeyAsync(0x42,editor);var filterValue=filter.Text;var bodyValue=editor.Text;
                panel.Title="Updated panel";producer.Tag=0;accent.Color=Windows.UI.Color.FromArgb(255,184,74,80);panel.Grow=true;panel.IsDragging=true;await AwaitPresentationAsync(panel);
                Require(ReferenceEquals(panel.Filter,filter)&&ReferenceEquals(panel.Content,body)&&filter.Text==filterValue&&editor.Text==bodyValue&&panel.Count==0&&panel.GetBindingExpression(BleeckerPanel.CountProperty)is not null,"Panel update replaced input values, identity or count binding.");
                var toolbarButton=Desc<Button>(action).Single();var handleButton=Desc<Button>(drag).Single();await input.ClickAsync(toolbarButton);await input.KeyAsync(0x20,toolbarButton);await input.ClickAsync(handleButton);
                var handleHost=Desc<Control>(panel).Single(node=>node.Name=="PanelHandle");await input.MoveAsync(anchor);await AwaitPresentationAsync(panel);var idleHandlePaint=Paint(handleHost.Foreground);
                await input.MoveAsync(handleButton);await AwaitPresentationAsync(panel);var hoverHandlePaint=Paint(handleHost.Foreground);
                Require(actions==2&&handles==1,"Panel duplicated or suppressed caller actions.");
                filter.Focus(FocusState.Keyboard);input.Key(0x09);await AwaitPresentationAsync(panel);Require(ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),toolbarButton),"Panel changed filter-before-toolbar document Tab order.");
                var viewer=Desc<ScrollViewer>(panel).Single(node=>node.Name=="PanelFeed");await Scroll(viewer,120);var scrollOffset=viewer.VerticalOffset;
                panel.Title="Scrolled panel";producer.Tag=7;await AwaitPresentationAsync(panel);Require(viewer.VerticalOffset==scrollOffset,"Panel reset body scrolling during a live header update.");var template=panel.Template;
                panel.Template=null;await AwaitPresentationAsync(canvas);panel.Template=template;await AwaitPresentationAsync(panel);
                Require(filter.Text==filterValue&&editor.Text==bodyValue&&ReferenceEquals(panel.Toolbar,action),"Panel template replacement lost original consumer content.");
                canvas.Children.Remove(panel);await AwaitPresentationAsync(canvas);canvas.Children.Add(panel);await AwaitPresentationAsync(panel);Require(filter.Text==filterValue&&editor.Text==bodyValue,"Panel remount reset consumer input.");
                var slotTemplate=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}'/></DataTemplate>");
                panel.FilterTemplate=slotTemplate;panel.Filter=42;panel.ToolbarTemplate=slotTemplate;panel.Toolbar=false;panel.DragHandleTemplate=slotTemplate;panel.DragHandle=0;await AwaitPresentationAsync(panel);
                var nativeModels=Desc<ContentPresenter>(panel).Select(node=>node.Content).ToArray();Require(nativeModels.Any(value=>value is int number&&number==42)&&nativeModels.Any(value=>value is bool flag&&!flag),"Panel converted native template models.");
                var selector=new PageFrameSelector(slotTemplate);panel.FilterTemplate=null;panel.FilterTemplateSelector=selector;panel.Filter="Selected filter";await AwaitPresentationAsync(panel);Require(selector.Selections>0&&Desc<TextBlock>(panel).Any(node=>node.Text=="Selected filter"),"Panel lost a consumer template selector.");
                AutomationProperties.SetName(panel,"Consumer caption");Require(FrameworkElementAutomationPeer.CreatePeerForElement(panel).GetName()=="Consumer caption","Panel overwrote explicit caller metadata.");
                panel.FilterTemplateSelector=null;panel.ToolbarTemplate=null;panel.DragHandleTemplate=null;panel.Filter=null;panel.Toolbar=null;panel.DragHandle=null;await AwaitPresentationAsync(panel);
                Require(Desc<FrameworkElement>(panel).Where(node=>node.Name is "PanelFilter" or "PanelToolbar" or "PanelHandle").All(node=>node.Visibility==Visibility.Collapsed),"Panel retained absent slot spacing.");
                var editorViewer=Desc<ScrollViewer>(panel).Single(node=>node.Name=="PanelFeed");Require(!editorViewer.IsTabStop,"Panel inserted a viewport Tab stop ahead of a focusable consumer child.");
                panel.Content=new PageFrameBody{Height=600};await AwaitPresentationAsync(panel);var keyboardViewer=Desc<ScrollViewer>(panel).Single(node=>node.Name=="PanelFeed");Require(keyboardViewer.IsTabStop,"Panel's plain overflow viewport cannot receive keyboard focus.");
                anchor.Focus(FocusState.Keyboard);input.Key(0x09);await AwaitPresentationAsync(panel);Require(ReferenceEquals(FocusManager.GetFocusedElement(canvas.XamlRoot),keyboardViewer),"Tab did not reach Panel's plain scroll viewport.");
                await input.KeyAsync(0x22,keyboardViewer);await AwaitPresentationAsync(panel);var pageDown=keyboardViewer.VerticalOffset;Require(pageDown>0,"Panel PageDown did not scroll its focused viewport.");
                await input.KeyAsync(0x24,keyboardViewer);await AwaitPresentationAsync(panel);Require(keyboardViewer.VerticalOffset==0,"Panel Home did not return its viewport to the start.");
                panel.Content=body;await AwaitPresentationAsync(panel);Require(!keyboardViewer.IsTabStop&&editor.Text==bodyValue,"Panel failed to restore the consumer's focus path and value.");
                var intrinsic = new BleeckerPanel { Title="Intrinsic width",Height=200,Content=new Border{Width=120,Height=20} };var horizontal=new StackPanel{Orientation=Orientation.Horizontal};horizontal.Children.Add(intrinsic);Canvas.SetLeft(horizontal,500);Canvas.SetTop(horizontal,20);canvas.Children.Add(horizontal);await AwaitPresentationAsync(intrinsic);
                Require(double.IsFinite(intrinsic.ActualWidth)&&intrinsic.ActualWidth>0,"Panel did not measure in an unbounded horizontal parent.");canvas.Children.Remove(horizontal);
                interactions.Add(new{theme=theme.ToString(),passed=true,filterValue,bodyValue,inputsRetained=true,countBindingRetained=true,actions,handles,sourceTabOrder=true,scrollOffset,liveScrollRetained=true,templateReplacement=true,remount=true,originalTemplateValues=true,selectorRetained=true,metadataPrecedence=true,absentSlots=true,idleHandlePaint,hoverHandlePaint,viewportKeyboard=true,pageDown,home=keyboardViewer.VerticalOffset,consumerFocusRestored=true,intrinsicWidth=true});canvas.Children.Remove(panel);
            }
            await File.WriteAllTextAsync(Path.Combine(folder,"panel.json"),JsonSerializer.Serialize(new{passed=true,frames,interactions,captures},new JsonSerializerOptions{WriteIndented=true}));
            await File.WriteAllTextAsync(Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="panel",environment=RuntimeEnvironment()}));
        }
        finally{Content=previous;AppWindow.ResizeClient(originalSize);await AwaitPresentationAsync(CatalogRoot);}
    }
    // The caller's two CSS boxes shrink proportionally within the toolbar.
    // This specimen supplies that caller layout; Panel itself keeps its content.
    private sealed partial class PanelToolsBody(FrameworkElement first, FrameworkElement second) : Panel
    {
        private static double First(double width) => Math.Floor(Math.Max(0,width-8)*80/144*64+.5)/64;
        protected override Size MeasureOverride(Size available)
        {
            if(Children.Count==0){Children.Add(first);Children.Add(second);}
            var width=Math.Min(152,available.Width);var firstWidth=First(width);
            first.Measure(new Size(firstWidth,24));second.Measure(new Size(Math.Max(0,width-8-firstWidth),32));return new Size(width,32);
        }
        protected override Size ArrangeOverride(Size final)
        {
            var firstWidth=First(final.Width);first.Arrange(new Rect(0,4,firstWidth,24));second.Arrange(new Rect(firstWidth+8,0,Math.Max(0,final.Width-8-firstWidth),32));return final;
        }
    }
}
