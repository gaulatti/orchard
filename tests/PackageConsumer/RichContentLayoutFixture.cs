using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class RichContentLayoutFixture
{
    private delegate bool Visitor(nint handle,nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor,nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var metricCases=new List<object>();var cardCases=new List<object>();var guards=0;var focus=0;var activations=0;var stage="initial";var passed=true;
        try
        {
            var root=new Canvas{Width=520,Height=800,Background=new SolidColorBrush(Windows.UI.Color.FromArgb(255,68,85,102))};using var island=new DesktopWindowXamlSource();island.Initialize(window.AppWindow.Id);island.Content=root;island.SiteBridge.MoveAndResize(new Windows.Graphics.RectInt32(0,0,520,800));window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(520,800));root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);if(visible||window.Visible||window.AppWindow.IsVisible||foreground==Environment.ProcessId||focus!=0||activations!=0)throw new InvalidOperationException("Rich content layout probe left its hidden boundary.");}
            async Task Layout(){for(var i=0;i<3;i++){root.Measure(new Size(520,800));root.Arrange(new Rect(0,0,520,800));root.UpdateLayout();Guard();await Task.Delay(16);}await NativeRenderReadiness.RenderAsync(root);Guard();}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})foreach(var flow in new[]{FlowDirection.LeftToRight,FlowDirection.RightToLeft})foreach(var width in new[]{72d,140d,320d})
            {
                root.RequestedTheme=theme;var prefix=new Border{Width=28,Height=18,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral),Tag="prefix-state"};var suffix=new Border{Width=40,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Gold),Tag="suffix-state"};var prefixSource=new Border{Tag=prefix};var suffixSource=new Border{Tag=suffix};var metric=new BleeckerMetric{Width=width,FlowDirection=flow,Value=12};metric.SetBinding(BleeckerMetric.PrefixProperty,new Binding{Source=prefixSource,Path=new PropertyPath("Tag")});metric.SetBinding(BleeckerMetric.SuffixProperty,new Binding{Source=suffixSource,Path=new PropertyPath("Tag")});var loads=0;var unloads=0;prefix.Loaded+=(_,_)=>loads++;prefix.Unloaded+=(_,_)=>unloads++;suffix.Loaded+=(_,_)=>loads++;suffix.Unloaded+=(_,_)=>unloads++;root.Children.Add(metric);await Layout();var initialLoads=loads;loads=0;unloads=0;
                async Task Metric(string id)
                {
                    stage=$"{theme}/{flow}/{width}/metric/{id}";await Layout();var text=Elements(metric).OfType<RichTextBlock>().Single();var paragraph=(Paragraph)text.Blocks[0];var inline=paragraph.Inlines.OfType<InlineUIContainer>().ToArray();var hosts=Elements(metric).OfType<ContentPresenter>().Where(h=>ReferenceEquals(h.Content,prefix)||ReferenceEquals(h.Content,suffix)).ToArray();var maximumError=0d;
                    for(var i=0;i<2;i++){var child=i==0?prefix:suffix;var host=hosts.Single(h=>ReferenceEquals(h.Content,child));var spacer=(FrameworkElement)inline[i].Child;var a=host.TransformToVisual(metric).TransformBounds(new Rect(0,0,host.ActualWidth,host.ActualHeight));var b=spacer.TransformToVisual(metric).TransformBounds(new Rect(0,0,spacer.ActualWidth,spacer.ActualHeight));maximumError=Math.Max(maximumError,new[]{Math.Abs(a.X-b.X),Math.Abs(a.Y-b.Y),Math.Abs(a.Width-b.Width),Math.Abs(a.Height-b.Height)}.Max());}
                    var samePrefix=ReferenceEquals(metric.Prefix,prefix);var sameSuffix=ReferenceEquals(metric.Suffix,suffix);var prefixBound=metric.GetBindingExpression(BleeckerMetric.PrefixProperty)is not null;var suffixBound=metric.GetBindingExpression(BleeckerMetric.SuffixProperty)is not null;var retained=samePrefix&&sameSuffix&&prefixBound&&suffixBound&&prefix.IsLoaded&&suffix.IsLoaded&&loads==0&&unloads==0;var ok=maximumError<=.05&&retained;passed&=ok;metricCases.Add(new{theme=theme.ToString(),flow=flow.ToString(),width,id,maximumError,retained,loads,unloads,initialLoads,prefixInTree=Elements(metric).Any(e=>ReferenceEquals(e,prefix)),suffixInTree=Elements(metric).Any(e=>ReferenceEquals(e,suffix)),samePrefix,sameSuffix,prefixBound,suffixBound,ownerLoaded=metric.IsLoaded,rootLoaded=root.IsLoaded,prefixLoaded=prefix.IsLoaded,suffixLoaded=suffix.IsLoaded,metricHeight=metric.ActualHeight,passed=ok});
                }
                await Metric("initial");metric.Value=123456789;await Metric("long-value");metric.Value=null;await Metric("empty-value");prefix.Width=52;prefix.Height=30;suffix.Width=60;await Metric("resized-slots");metric.Value=42;await Metric("restored-value");root.Children.Clear();await Layout();
                var value=new Border{Width=80,Height=32,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral),Tag="value-state"};var icon=new Border{Width=12,Height=12,Background=new SolidColorBrush(Microsoft.UI.Colors.Gold),Tag="icon-state"};var footer=new Border{Width=96,Height=24,Background=new SolidColorBrush(Microsoft.UI.Colors.Coral),Tag="footer-state"};var card=new BleeckerStatCard{Width=width,FlowDirection=flow,Title="Audience",Value=value,Icon=icon,Footer=footer};var cardLoads=0;var cardUnloads=0;foreach(var child in new[]{value,icon,footer}){child.Loaded+=(_,_)=>cardLoads++;child.Unloaded+=(_,_)=>cardUnloads++;}root.Children.Add(card);await Layout();var initialCardLoads=cardLoads;cardLoads=0;cardUnloads=0;
                async Task Card(string id)
                {
                    stage=$"{theme}/{flow}/{width}/card/{id}";await Layout();var sameValue=ReferenceEquals(card.Value,value);var sameIcon=ReferenceEquals(card.Icon,icon);var sameFooter=ReferenceEquals(card.Footer,footer);var retained=sameValue&&sameIcon&&sameFooter&&value.IsLoaded&&icon.IsLoaded&&footer.IsLoaded&&cardLoads==0&&cardUnloads==0;passed&=retained;cardCases.Add(new{theme=theme.ToString(),flow=flow.ToString(),width=card.Width,id,retained,loads=cardLoads,unloads=cardUnloads,initialCardLoads,valueInTree=Elements(card).Any(e=>ReferenceEquals(e,value)),iconInTree=Elements(card).Any(e=>ReferenceEquals(e,icon)),footerInTree=Elements(card).Any(e=>ReferenceEquals(e,footer)),sameValue,sameIcon,sameFooter,ownerLoaded=card.IsLoaded,rootLoaded=root.IsLoaded,valueLoaded=value.IsLoaded,iconLoaded=icon.IsLoaded,footerLoaded=footer.IsLoaded,height=card.ActualHeight,valueWidth=value.ActualWidth,footerWidth=footer.ActualWidth,passed=retained});
                }
                await Card("initial");card.Title="A longer title that wraps across several lines";await Card("long-title");card.Description="A fictional longer summary for constrained component layout";await Card("description");value.Width=120;footer.Height=40;await Card("resized-content");card.Title="Audience";card.Description=null;await Card("restored-title");root.Children.Clear();await Layout();
            }
            Guard();GC.KeepAlive(island);await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-rich-content-layout-v1",passed=true,layoutContractPassed=passed,metricCases,cardCases,guards,focus,activations,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerMetric).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=false,stage,error=error.ToString(),metricCases,cardCases,guards,focus,activations}));Environment.Exit(1);}
    }
}

