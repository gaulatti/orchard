using System.Diagnostics;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.Graphics;

namespace Orchard.Catalog;

public sealed partial class DashboardSectionsFixture : UserControl
{
    public DashboardSectionsFixture() => InitializeComponent();
    internal async Task VerifyAsync(BleeckerColorScheme scheme, string folder)
    {
        Require(XamlRoot.RasterizationScale == 1 && new Windows.UI.ViewManagement.UISettings().TextScaleFactor == 1,
            "Frozen Dashboard Section comparison requires display/text scale 1; scaling acceptance remains open.");
        using var input = new NativeInputSession(XamlRoot, folder, $"dashboard-section-{scheme}");
        await input.AcquireAsync(Specimen);
        var window = AppWindow.GetFromWindowId(XamlRoot.ContentIslandEnvironment.AppWindowId);
        var original = window.ClientSize; var presenter = (OverlappedPresenter)window.Presenter;
        var maximized = presenter.State == OverlappedPresenterState.Maximized;
        var root = (Grid)XamlRoot.Content;
        var host = new Grid { Margin = new Thickness(20), Background = root.Background };
        Grid.SetRowSpan(host, root.RowDefinitions.Count); Grid.SetColumnSpan(host, root.ColumnDefinitions.Count); root.Children.Add(host);
        using var reference = JsonDocument.Parse(typeof(DashboardSectionsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.DashboardSectionLayout.json")!);
        var reports = new List<object>();
        try
        {
            presenter.Restore(false);
            foreach (var item in reference.RootElement.GetProperty("cases").EnumerateArray().Where(x => x.GetProperty("scheme").GetString() == scheme.ToString()))
            {
                var id = item.GetProperty("id").GetString()!; var viewport = item.GetProperty("viewport").GetInt32();
                window.ResizeClient(new SizeInt32(viewport, 745));
                await AwaitAsync(() => Math.Abs(XamlRoot.Size.Width - viewport) < .001, "Dashboard viewport did not resize.");
                var props = item.GetProperty("props");
                var action = new Border { Width = 80, Height = 32, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255,166,93,87)) };
                var content = new Border { Height = 64, Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255,185,199,209)) };
                var control = new BleeckerDashboardSection { Width = item.GetProperty("width").GetDouble(), HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top, Background = root.Background,
                    Title = props.TryGetProperty("title", out var t) ? t.GetString() : null,
                    Description = props.TryGetProperty("description", out var d) ? d.GetString() : null,
                    Action = props.TryGetProperty("action", out var a) && a.GetBoolean() ? action : null,
                    Content = props.TryGetProperty("content", out var c) && !c.GetBoolean() ? null : content,
                    FlowDirection = item.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
                host.Children.Clear(); host.Children.Add(control);
                await AwaitAsync(() => control.IsLoaded, "Dashboard Section did not mount."); control.ApplyTemplate(); UpdateLayout();
                await FrameAsync(); UpdateLayout();
                var expected = item.GetProperty("layout"); var title = Part<TextBlock>(control,"PART_Title"); var description = Part<TextBlock>(control,"PART_Description");
                reports.Add(new { id, viewport, height = control.ActualHeight, expectedHeight = expected.GetProperty("section").GetProperty("height").GetDouble(),
                    titleHeight = title.ActualHeight, descriptionHeight = description.ActualHeight,
                    titleWidth=title.ActualWidth,descriptionWidth=description.ActualWidth,titleDirection=title.FlowDirection.ToString(),descriptionDirection=description.FlowDirection.ToString(),readingOrder=description.TextReadingOrder.ToString(),
                    textGroupLeftDeltaPx=control.FlowDirection==FlowDirection.RightToLeft ? PhysicalBounds(Part<StackPanel>(control,"PART_Lead"),control).X-(expected.GetProperty("title").GetProperty("x").GetDouble()-expected.GetProperty("section").GetProperty("x").GetDouble()) : (double?)null,
                    action = Bounds(action,control), content = Bounds(content,control) });
                await File.WriteAllTextAsync(Path.Combine(folder,$"dashboard-section-layout-{scheme}.json"),JsonSerializer.Serialize(reports,new JsonSerializerOptions { WriteIndented=true }));
                Near(control.ActualHeight,expected.GetProperty("section").GetProperty("height").GetDouble(),$"{id} section height");
                Check(control,Part<Grid>(control,"PART_Header"),expected,"header",id);
                Check(control,title,expected,"title",id); Check(control,description,expected,"description",id);
                if (control.Action is not null) Check(control,action,expected,"action",id);
                if (control.Content is not null) Check(control,content,expected,"content",id);
                Require(title.FontFamily.Source.Contains("EncodeSans-Medium.ttf") && title.FontSize==18 && title.LineHeight==28 && title.CharacterSpacing==-20 &&
                    description.FontFamily.Source.Contains("LibreFranklin-Regular.ttf") && description.FontSize==14 && description.LineHeight==20,
                    "Dashboard Section typography differs.");
                if(expected.GetProperty("title").ValueKind!=JsonValueKind.Null) Require(CssColor(title.Foreground)==expected.GetProperty("title").GetProperty("color").GetString(),"Dashboard title palette differs.");
                if(expected.GetProperty("description").ValueKind!=JsonValueKind.Null) Require(CssColor(description.Foreground)==expected.GetProperty("description").GetProperty("color").GetString(),"Dashboard description palette differs.");
                Require(!control.IsTabStop && FrameworkElementAutomationPeer.CreatePeerForElement(control).GetAutomationControlType()==AutomationControlType.Group,
                    "Dashboard Section lost its passive group semantics.");
                if (!string.IsNullOrEmpty(control.Title)) Require(FrameworkElementAutomationPeer.CreatePeerForElement(title).GetHeadingLevel()==AutomationHeadingLevel.Level2,
                    "Dashboard Section title lost its real level-two heading peer.");
                if (control.ActualHeight>0) await CaptureAsync(control,Path.Combine(folder,$"dashboard-section-{id}-{scheme}.png"));
                if (id=="full") await MutationAsync(control,host,action,content);
            }
            host.Children.Clear(); window.ResizeClient(new SizeInt32(1000,745));
            await AwaitAsync(()=>Math.Abs(XamlRoot.Size.Width-1000)<.001,"Keyboard viewport did not restore.");
            await KeyboardAsync(host,input);
        }
        finally
        {
            root.Children.Remove(host); window.ResizeClient(original); if(maximized)presenter.Maximize();
            await AwaitAsync(()=>maximized || Math.Abs(XamlRoot.Size.Width-original.Width)<.001,"Dashboard viewport did not restore.");
        }
    }
    private async Task MutationAsync(BleeckerDashboardSection control,Grid host,UIElement action,UIElement content)
    {
        var source = new TextBlock { Text="Live consumer heading" };
        control.SetBinding(BleeckerDashboardSection.TitleProperty,new Binding {Source=source,Path=new PropertyPath("Text"),Mode=BindingMode.OneWay});
        source.Text="Changed consumer heading"; UpdateLayout();
        Require(Part<TextBlock>(control,"PART_Title").Text==source.Text,"Dashboard title binding did not update.");
        control.Title=null; control.Description=null; control.Action=null; UpdateLayout(); Near(control.ActualHeight,64,"header removal");
        control.Content=null; UpdateLayout(); Near(control.ActualHeight,0,"empty section");
        control.Title="Monitoring"; control.Description="Consumer-owned section summary."; control.Action=action; control.Content=content;
        host.Children.Remove(control); host.Children.Add(control); await AwaitAsync(()=>control.IsLoaded,"Dashboard remount failed."); UpdateLayout(); Near(control.ActualHeight,136,"remounted section");
        var original=control.Template;
        control.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Height='17'/></ControlTemplate>");
        control.ApplyTemplate(); UpdateLayout(); Near(control.ActualHeight,17,"consumer template");
        control.Template=original; control.ApplyTemplate(); UpdateLayout(); Near(control.ActualHeight,136,"restored template");
        Require(ReferenceEquals(Part<ContentPresenter>(control,"PART_Action").Content,action)&&ReferenceEquals(Part<ContentPresenter>(control,"PART_Content").Content,content),"Consumer children were replaced.");
        control.ActionTemplate=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock x:Name='ActionData' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Text='{Binding}' Width='80' Height='32'/></DataTemplate>");
        control.ContentTemplate=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock x:Name='BodyData' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' Text='{Binding}' Height='64'/></DataTemplate>");
        control.Action="Consumer data action";control.Content="Consumer data body";UpdateLayout();await FrameAsync();UpdateLayout();
        Require(Part<TextBlock>(control,"ActionData").Text=="Consumer data action"&&Part<TextBlock>(control,"BodyData").Text=="Consumer data body","Dashboard data templates did not render consumer data.");
        control.ActionTemplate=null;control.ContentTemplate=null;control.Action=action;control.Content=content;UpdateLayout();
        control.HeaderStyle=new Style(typeof(Grid)){Setters={new Setter(Grid.PaddingProperty,new Thickness(3))}};UpdateLayout();Near(control.ActualHeight,142,"header style override");
        control.HeaderStyle=null;control.Padding=new Thickness(6);UpdateLayout();Near(control.ActualHeight,148,"surface padding override");
        control.Padding=new Thickness();UpdateLayout();Near(control.ActualHeight,136,"restored surface");
    }
    private async Task KeyboardAsync(Grid host,NativeInputSession input)
    {
        var before=new BleeckerButton {Content="Before section"}; var action=new BleeckerButton {Content="Refresh"};
        var child=new BleeckerButton {Content="Open review"}; var after=new BleeckerButton {Content="After section"};
        var clicks=0; action.Click+=(_,_)=>clicks++;
        var section=new BleeckerDashboardSection {Title="Monitoring",Action=action,Content=child};
        var stack=new StackPanel {VerticalAlignment=VerticalAlignment.Top}; stack.Children.Add(before); stack.Children.Add(section); stack.Children.Add(after);host.Children.Add(stack);
        await AwaitAsync(()=>after.IsLoaded,"Dashboard keyboard fixture did not mount."); UpdateLayout(); await input.AcquireAsync(before);
        var first=Part<Button>(before,"PART_Action");var target=Part<Button>(action,"PART_Action");var body=Part<Button>(child,"PART_Action");var last=Part<Button>(after,"PART_Action");
        Require(first.Focus(FocusState.Keyboard),"Could not focus before Dashboard Section.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),target),"Tab skipped dashboard action.");
        input.Key(0x20);await AwaitAsync(()=>clicks==1,"Dashboard action did not receive Space.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),body),"Tab skipped dashboard content.");
        input.Key(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),last),"Tab did not leave dashboard content.");
        input.ShiftKey(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),body),"Reverse Tab skipped dashboard content.");
        input.ShiftKey(0x09);await AwaitAsync(()=>ReferenceEquals(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(XamlRoot),target),"Reverse Tab skipped dashboard action.");
        section.IsEnabled=false;UpdateLayout();
        Require(!target.IsEnabled&&!body.IsEnabled&&!FrameworkElementAutomationPeer.CreatePeerForElement(target).IsEnabled(),"Disabled Dashboard Section left consumer actions enabled.");
        section.IsEnabled=true;UpdateLayout();Require(target.IsEnabled&&body.IsEnabled,"Dashboard Section did not restore child actions.");
    }
    private static Rect PhysicalBounds(FrameworkElement element,FrameworkElement owner)
    {
        var root=owner.XamlRoot.Content;
        var bounds=element.TransformToVisual(root).TransformBounds(new Rect(0,0,element.ActualWidth,element.ActualHeight));
        var origin=owner.TransformToVisual(root).TransformBounds(new Rect(0,0,owner.ActualWidth,owner.ActualHeight));
        return new Rect(bounds.X-origin.X,bounds.Y-origin.Y,bounds.Width,bounds.Height);
    }
    private static object? Bounds(FrameworkElement element,FrameworkElement owner)=>element.IsLoaded?PhysicalBounds(element,owner):null;
    private static string CssColor(Brush brush){var c=((SolidColorBrush)brush).Color;return $"rgb({c.R}, {c.G}, {c.B})";}
    private static async Task CaptureAsync(BleeckerDashboardSection control,string path)
    {
        // Action-only/body-only specimens intentionally contain flat colors.
        // Verify their actual fill instead of using the catalog's glyph-rich gate.
        var bitmap=new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();await bitmap.RenderAsync(control);
        Require(bitmap.PixelWidth>0&&bitmap.PixelHeight>0,"Dashboard Section did not render.");
        var buffer=await bitmap.GetPixelsAsync();var bytes=new byte[checked((int)buffer.Length)];
        using(var reader=Windows.Storage.Streams.DataReader.FromBuffer(buffer))reader.ReadBytes(bytes);
        foreach(var block in new[]{control.Content as Border,control.Action as Border}.OfType<Border>())
        {
            var bounds=PhysicalBounds(block,control);var point=new Point(bounds.X+bounds.Width/2,bounds.Y+bounds.Height/2);
            var offset=((int)point.Y*bitmap.PixelWidth+(int)point.X)*4;var color=((SolidColorBrush)block.Background).Color;
            Require(bytes[offset]==color.B&&bytes[offset+1]==color.G&&bytes[offset+2]==color.R&&bytes[offset+3]==255,"Dashboard Section consumer fill did not render.");
        }
        await MainWindow.SavePngAsync(path,bytes,bitmap.PixelWidth,bitmap.PixelHeight);
    }
    private static void Check(FrameworkElement owner,FrameworkElement actual,JsonElement layout,string name,string id)
    {
        var expected=layout.GetProperty(name); if(expected.ValueKind==JsonValueKind.Null){Require(actual.Visibility==Visibility.Collapsed,$"{id} {name} did not collapse.");return;}
        var origin=layout.GetProperty("section");var bounds=PhysicalBounds(actual,owner);var point=new Point(bounds.X,bounds.Y);
        if(owner.FlowDirection==FlowDirection.RightToLeft && name is "title" or "description")
        {
            // Compare the source's trailing allocation edge independently of
            // native glyph shaping. Preserve the measured left-edge residual
            // in the report; exact shaping/raster acceptance remains open.
            var group=PhysicalBounds((FrameworkElement)VisualTreeHelper.GetParent(actual),owner);
            Near(group.Right,expected.GetProperty("x").GetDouble()-origin.GetProperty("x").GetDouble()+expected.GetProperty("width").GetDouble(),$"{id} {name} trailing edge");
        }
        else Near(point.X,expected.GetProperty("x").GetDouble()-origin.GetProperty("x").GetDouble(),$"{id} {name} x");
        Near(point.Y,expected.GetProperty("y").GetDouble()-origin.GetProperty("y").GetDouble(),$"{id} {name} y");
        var height=actual is TextBlock ? Microsoft.UI.Xaml.Controls.Primitives.LayoutInformation.GetLayoutSlot(actual).Height-actual.Margin.Top-actual.Margin.Bottom : actual.ActualHeight;
        Near(height,expected.GetProperty("height").GetDouble(),$"{id} {name} height");
        if(name is "header" or "content" or "action")Near(bounds.Width,expected.GetProperty("width").GetDouble(),$"{id} {name} width");
    }
    private static void Near(double actual,double expected,string label)=>Require(Math.Abs(actual-expected)<.001,$"Dashboard Section {label} differs: {actual:R}/{expected:R}.");
    private static T Part<T>(DependencyObject owner,string name) where T:FrameworkElement
    {
        if(owner is T match&&match.Name==name)return match;
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){try{return Part<T>(VisualTreeHelper.GetChild(owner,i),name);}catch(InvalidOperationException){}}
        throw new InvalidOperationException($"Missing Dashboard Section part {name}.");
    }
    private static async Task FrameAsync()
    {
        var frame=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void Presented(object? sender,object args)=>frame.TrySetResult();
        CompositionTarget.Rendering+=Presented;try{await frame.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Presented;}
    }
    private static async Task AwaitAsync(Func<bool> ready,string message){var watch=Stopwatch.StartNew();while(!ready()&&watch.Elapsed<TimeSpan.FromSeconds(2))await Task.Delay(16);Require(ready(),message);}
    private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
}
