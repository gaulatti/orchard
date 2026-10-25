using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace Orchard.Testing;

internal static class BrandMarkFixture
{
 private delegate bool Visitor(nint handle,nint value);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint value);
 [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
 [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
 private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var child in Elements(VisualTreeHelper.GetChild(node,i)))yield return child;}
 private sealed class Caller : INotifyPropertyChanged
 {
  public double Size{get;private set;}=48;
  public SolidColorBrush Foreground{get;private set;}=new(Color.FromArgb(255,44,87,132));
  public event PropertyChangedEventHandler? PropertyChanged;
  internal void Resize(double value){Size=value;PropertyChanged?.Invoke(this,new(nameof(Size)));}
  internal void Replace(SolidColorBrush value){Foreground=value;PropertyChanged?.Invoke(this,new(nameof(Foreground)));}
 }
 internal static async Task RunAsync(Window window,string output,string hostAssembly,Func<FrameworkElement>? catalogFactory=null)
 {
  var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='640' Height='480' Background='{ThemeResource BleeckerBackgroundBrush}'/>");root.Clip=new RectangleGeometry{Rect=new Rect(0,0,640,480)};window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(640,480));
  var guards=0;var focus=0;var activations=0;var stage="initial";var states=new List<object>();var contracts=new List<object>();var pngs=new List<object>();
  root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
  void Require(bool valid,string message){if(!valid)throw new InvalidOperationException(message);}
  void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Brand mark probe left its hidden boundary.");}
  async Task Layout(){for(var i=0;i<5;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused brand layout.");await idle.Task;root.Measure(new Size(640,480));root.Arrange(new Rect(0,0,640,480));root.UpdateLayout();Guard();await Task.Delay(12);}await Task.Delay(100);Guard();}
  object Box(FrameworkElement element){var transform=element.TransformToVisual(root);var a=transform.TransformPoint(new Point(0,0));var b=transform.TransformPoint(new Point(element.ActualWidth,element.ActualHeight));return new{x=Math.Min(a.X,b.X),y=Math.Min(a.Y,b.Y),width=Math.Abs(b.X-a.X),height=Math.Abs(b.Y-a.Y)};}
  static object ColorData(Color c)=>new{a=c.A,r=c.R,g=c.G,b=c.B};
  async Task<string> Capture(string name){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);var file="brand-mark-"+name+".png";await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});return file;}
  async Task Observe(BleeckerBrandMark mark,string scheme,string kind,object? sizeBinding=null,object? brushBinding=null){stage=scheme+"-"+kind;await Layout();var icon=Elements(mark).OfType<BitmapIcon>().SingleOrDefault(e=>e.Name=="PART_Mark");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(mark);var brush=(SolidColorBrush)mark.Foreground;var file=await Capture(stage);states.Add(new{scheme,kind,size=mark.Size,box=Box(mark),iconBox=icon is null?null:Box(icon),iconWidth=icon?.Width,iconHeight=icon?.Height,iconOpacity=icon?.Opacity,uri=icon?.UriSource?.ToString(),monochrome=icon?.ShowAsMonochrome,imageLoaded=icon?.IsLoaded??false,foreground=ColorData(brush.Color),brushOpacity=brush.Opacity,opacity=mark.Opacity,background=ColorData(((SolidColorBrush)root.Background).Color),enabled=mark.IsEnabled,tabStop=mark.IsTabStop,controlType=peer.GetAutomationControlType().ToString(),accessibleName=peer.GetName(),invokeAvailable=peer.GetPattern(PatternInterface.Invoke)is not null,selectionAvailable=peer.GetPattern(PatternInterface.SelectionItem)is not null,sizeBindingRetained=sizeBinding is null||mark.GetBindingExpression(BleeckerBrandMark.SizeProperty)is not null&&ReferenceEquals(sizeBinding,mark.ReadLocalValue(BleeckerBrandMark.SizeProperty)),brushBindingRetained=brushBinding is null||mark.GetBindingExpression(Control.ForegroundProperty)is not null&&ReferenceEquals(brushBinding,mark.ReadLocalValue(Control.ForegroundProperty)),file});}
  BleeckerBrandMark Add(double? size=null){var m=size.HasValue?new BleeckerBrandMark(size.Value):new BleeckerBrandMark();m.Margin=new Thickness(20,20,0,0);m.VerticalAlignment=VerticalAlignment.Top;AutomationProperties.SetName(m,"Fictional studio logo");root.Children.Add(m);return m;}
  try{
   foreach(var scheme in new[]{"Light","Dark"}){
    root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
    foreach(var size in new[]{16d,32,64,96,0}){var m=Add(size==64?null:size);await Observe(m,scheme,"size-"+size);root.Children.Clear();await Layout();}
    foreach(var kind in new[]{"disabled","rtl","font-large","constrained"}){var m=Add();if(kind=="disabled")m.IsEnabled=false;if(kind=="rtl")m.FlowDirection=FlowDirection.RightToLeft;if(kind=="font-large")m.FontSize=200;if(kind=="constrained")m.MaxWidth=32;await Observe(m,scheme,kind);root.Children.Clear();await Layout();}
    var caller=new Caller();var live=Add();live.SetBinding(BleeckerBrandMark.SizeProperty,new Binding{Source=caller,Path=new PropertyPath(nameof(Caller.Size)),Mode=BindingMode.OneWay});live.SetBinding(Control.ForegroundProperty,new Binding{Source=caller,Path=new PropertyPath(nameof(Caller.Foreground)),Mode=BindingMode.OneWay});await Layout();var sizeExpression=live.ReadLocalValue(BleeckerBrandMark.SizeProperty);var brushExpression=live.ReadLocalValue(Control.ForegroundProperty);var defaultTemplate=live.Template;var original=caller.Foreground;
    foreach(var kind in new[]{"bound","bound-size","brush-color","brush-opacity","brush-replaced","caller-template","template-restored","reload"}){
     if(kind=="bound-size")caller.Resize(96);if(kind=="brush-color")original.Color=Color.FromArgb(255,150,60,180);if(kind=="brush-opacity")original.Opacity=.5;if(kind=="brush-replaced")caller.Replace(new SolidColorBrush(Color.FromArgb(128,90,200,10)){Opacity=.7});if(kind=="caller-template")live.Template=(ControlTemplate)XamlReader.Load("<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:o='using:Orchard' TargetType='o:BleeckerBrandMark'><Border x:Name='CallerFace' Width='43' Height='29' Background='{TemplateBinding Foreground}'/></ControlTemplate>");if(kind=="template-restored")live.Template=defaultTemplate;if(kind=="reload"){root.Children.Clear();await Layout();Require(!live.IsLoaded,"Removed brand mark stayed loaded.");caller.Resize(32);caller.Foreground.Color=Color.FromArgb(255,180,90,40);caller.Foreground.Opacity=1;root.Children.Add(live);}
     await Observe(live,scheme,kind,sizeExpression,brushExpression);Require(ReferenceEquals(live.Foreground,caller.Foreground),"The caller's brush was replaced.");
    }
    root.Children.Clear();await Layout();contracts.Add(new{scheme,callerBrushRetained=ReferenceEquals(live.Foreground,caller.Foreground),bindingsRetained=ReferenceEquals(sizeExpression,live.ReadLocalValue(BleeckerBrandMark.SizeProperty))&&ReferenceEquals(brushExpression,live.ReadLocalValue(Control.ForegroundProperty)),reloadRetainsOriginalControl=live.Size==32,unloaded=!live.IsLoaded});
   }
   root.RequestedTheme=ElementTheme.Light;var themed=Add();foreach(var (value,kind) in new[]{(ElementTheme.Light,"theme-initial"),(ElementTheme.Dark,"theme-dark"),(ElementTheme.Light,"theme-restored")}){root.RequestedTheme=value;await Observe(themed,value.ToString(),kind);}root.Children.Clear();await Layout();
   var invalid=new BleeckerBrandMark();var rejected=0;foreach(var value in new[]{-1d,double.NaN,double.PositiveInfinity,double.NegativeInfinity}){try{invalid.Size=value;}catch(ArgumentOutOfRangeException){rejected++;}}Require(rejected==4&&invalid.Size==64,"Invalid sizes were accepted or changed the original value.");
   var catalog=new List<object>();if(catalogFactory is not null)foreach(var scheme in new[]{"Light","Dark"}){root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var fixture=catalogFactory();root.Children.Add(fixture);await Layout();var marks=Elements(fixture).OfType<BleeckerBrandMark>().ToArray();Require(marks.Length==6&&marks.All(m=>m.IsLoaded),"Actual catalog marks did not load.");var file=await Capture("catalog-"+scheme);catalog.Add(new{scheme,marks=marks.Length,defaultSize=marks.Single(m=>m.Name=="Default").Size,registeredFixture=fixture.GetType().FullName,file});root.Children.Clear();await Layout();}
   string Hash(string p)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-brand-mark-native-v1",passed=true,states,contracts,catalog,pngs,invalidSizesRejected=rejected,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRuntimeVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerBrandMark).Assembly.Location),hostSha256=Hash(hostAssembly),assetSha256=Hash(Path.Combine(Path.GetDirectoryName(typeof(BleeckerBrandMark).Assembly.Location)!,"Orchard","Assets","Images","BleeckerLogo.png"))},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
  }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-brand-mark-native-v1",passed=false,stage,error=error.ToString(),states,contracts,pngs,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
 }
}
