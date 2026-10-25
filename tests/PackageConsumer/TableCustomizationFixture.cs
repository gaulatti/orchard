using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Text;
namespace Orchard.Testing;
internal static class TableCustomizationFixture
{
 private delegate bool Visitor(nint handle,nint value);
 [DllImport("user32.dll")]private static extern bool EnumWindows(Visitor visitor,nint value);
 [DllImport("user32.dll")]private static extern nint GetForegroundWindow();
 [DllImport("user32.dll")]private static extern uint GetWindowThreadProcessId(nint handle,out uint process);
 [DllImport("user32.dll")][return:MarshalAs(UnmanagedType.Bool)]private static extern bool IsWindowVisible(nint handle);
 private static IEnumerable<FrameworkElement> Elements(DependencyObject node){if(node is FrameworkElement e)yield return e;for(var i=0;i<VisualTreeHelper.GetChildrenCount(node);i++)foreach(var c in Elements(VisualTreeHelper.GetChild(node,i)))yield return c;}
 private sealed class Values : INotifyPropertyChanged
 {
  public FontStyle FontStyle {get;private set;}=FontStyle.Oblique;
  public FontStretch FontStretch {get;private set;}=FontStretch.Condensed;
  public int CharacterSpacing {get;private set;}=220;
  public double FontSize {get;private set;}=22;
  public double LineHeight {get;private set;}=32;
  public Thickness BorderThickness {get;private set;}=new(5,4,6,7);
  public Thickness Padding {get;private set;}=new(9,7,13,11);
  public event PropertyChangedEventHandler? PropertyChanged;
  internal void Change(){FontStyle=FontStyle.Normal;FontStretch=FontStretch.Expanded;CharacterSpacing=310;FontSize=26;LineHeight=38;BorderThickness=new(7,6,8,9);Padding=new(11,9,15,13);PropertyChanged?.Invoke(this,new(null));}
 }
 private static readonly DependencyProperty[] Properties=[Control.FontStyleProperty,Control.FontStretchProperty,Control.CharacterSpacingProperty,Control.FontSizeProperty,BleeckerTableCell.LineHeightProperty,Control.BorderThicknessProperty,Control.PaddingProperty];
 internal static async Task RunAsync(Window window,string output,string hostAssembly)
 {
  var root=(Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Width='1000' Height='760' Background='{ThemeResource BleeckerBackgroundBrush}'/>");window.Content=root;window.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(1000,760));var guards=0;var focus=0;var activations=0;var stage="initial";var snapshots=new List<object>();var pngs=new List<object>();
  root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,a)=>{if(a.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
  void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
  void Guard(){guards++;GetWindowThreadProcessId(GetForegroundWindow(),out var foreground);var visible=false;EnumWindows((h,_)=>{GetWindowThreadProcessId(h,out var p);visible|=p==Environment.ProcessId&&IsWindowVisible(h);return true;},0);Require(!visible&&!window.Visible&&!window.AppWindow.IsVisible&&foreground!=Environment.ProcessId&&focus==0&&activations==0,"Table customization probe left its hidden boundary.");}
  async Task Layout(){for(var i=0;i<6;i++){var idle=new TaskCompletionSource();Require(root.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,()=>idle.SetResult()),"Dispatcher refused layout.");await idle.Task;root.Measure(new Size(1000,760));root.Arrange(new Rect(0,0,1000,760));root.UpdateLayout();Guard();await Task.Delay(12);}}
  async Task Capture(string file){Guard();var bitmap=new RenderTargetBitmap();await bitmap.RenderAsync(root);Guard();var pixels=System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await bitmap.GetPixelsAsync());using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();stream.Seek(0);using var reader=new DataReader(stream.GetInputStreamAt(0));await reader.LoadAsync((uint)stream.Size);var bytes=new byte[(int)stream.Size];reader.ReadBytes(bytes);await File.WriteAllBytesAsync(Path.Combine(Path.GetDirectoryName(output)!,file),bytes);pngs.Add(new{file,width=bitmap.PixelWidth,height=bitmap.PixelHeight,sha256=Convert.ToHexStringLower(SHA256.HashData(bytes))});Guard();}
  try{
   foreach(var scheme in new[]{"Light","Dark"}){
    root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);var cells=new List<(string Kind,BleeckerTableCell Cell,BleeckerTable Table)>();var originalRich=new TextBlock{Text="MMMMMMMMMMMM",TextWrapping=TextWrapping.WrapWholeWords};
    foreach(var kind in new[]{"plain","heading","sortable","nonsortable","caption","rich","templated"}){
     BleeckerTableCell cell=kind switch{"heading"=>new BleeckerTableHead(),"sortable"=>new BleeckerSortableTableHeader{Field="item",Label="MMMMMMMMMMMM"},"nonsortable"=>new BleeckerSortableTableHeader{Field="item",Label="MMMMMMMMMMMM",IsSortable=false},"caption"=>new BleeckerTableCaption(),_=>new BleeckerTableCell()};
     if(cell is not BleeckerSortableTableHeader)cell.Content=kind=="rich"?originalRich:"MMMMMMMMMMMM";
     var key=cell is BleeckerSortableTableHeader?"BleeckerSortableTableHeaderStyle":cell is BleeckerTableCaption?"BleeckerTableCaptionStyle":cell is BleeckerTableHead?"BleeckerTableHeadStyle":"BleeckerTableCellStyle";
     var style=new Style{TargetType=cell.GetType(),BasedOn=(Style)Application.Current.Resources[key]};style.Setters.Add(new Setter(Control.FontStyleProperty,FontStyle.Italic));style.Setters.Add(new Setter(Control.FontStretchProperty,FontStretch.Expanded));style.Setters.Add(new Setter(Control.CharacterSpacingProperty,120));style.Setters.Add(new Setter(Control.FontSizeProperty,18d));style.Setters.Add(new Setter(BleeckerTableCell.LineHeightProperty,28d));style.Setters.Add(new Setter(Control.PaddingProperty,new Thickness(7,5,11,9)));style.Setters.Add(new Setter(Control.BorderThicknessProperty,new Thickness(3,2,4,5)));style.Setters.Add(new Setter(Control.CornerRadiusProperty,new CornerRadius(9)));cell.Style=style;await File.WriteAllTextAsync(output+".stage",scheme+"-create-"+kind);
     if(kind=="templated"){cell.MinWidth=500;cell.ContentTemplate=(DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}' TextWrapping='WrapWholeWords'/></DataTemplate>");}
     var table=new BleeckerTable{Width=200,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Margin=new Thickness(20+(cells.Count%3)*320,20+(cells.Count/3)*230,0,0)};
     if(cell is BleeckerTableCaption)table.Sections.Add(cell);else{var section=cell is BleeckerTableHead?(Grid)new BleeckerTableHeader():new BleeckerTableBody();var row=new BleeckerTableRow();row.Cells.Add(cell);section.Children.Add(row);table.Sections.Add(section);}root.Children.Add(table);cells.Add((kind,cell,table));
    }
    await Layout();foreach(var entry in cells)entry.Cell.BorderBrush=entry.Cell.Foreground;
    var templates=cells.Select(e=>e.Cell.Template).ToArray();var values=new Values();var originalBindings=new Dictionary<BleeckerTableCell,object[]>();
    foreach(var step in new[]{"style","binding","notified","template-restored","style-restored"}){
     stage=scheme+"-"+step;await File.WriteAllTextAsync(output+".stage",stage);
     if(step=="binding")foreach(var entry in cells)foreach(var property in Properties)entry.Cell.SetBinding(property,new Binding{Source=values,Path=new PropertyPath(property==BleeckerTableCell.LineHeightProperty?"LineHeight":property==Control.FontStyleProperty?"FontStyle":property==Control.FontStretchProperty?"FontStretch":property==Control.CharacterSpacingProperty?"CharacterSpacing":property==Control.FontSizeProperty?"FontSize":property==Control.BorderThicknessProperty?"BorderThickness":"Padding"),Mode=BindingMode.OneWay});
     if(step=="notified")values.Change();
     if(step=="template-restored"){foreach(var entry in cells)entry.Cell.Template=null;await File.WriteAllTextAsync(output+".stage",stage+"-cleared");await Layout();await File.WriteAllTextAsync(output+".stage",stage+"-restoring");for(var i=0;i<cells.Count;i++)cells[i].Cell.Template=templates[i];}
     if(step=="style-restored")foreach(var entry in cells)foreach(var property in Properties)entry.Cell.ClearValue(property);
     await Layout();if(step=="binding")foreach(var entry in cells)originalBindings[entry.Cell]=Properties.Select(entry.Cell.ReadLocalValue).ToArray();var details=new List<object>();
     foreach(var(kind,cell,table)in cells){
      var target=kind=="sortable"?Elements(cell).OfType<TextBlock>().Single(t=>t.Name=="PART_Label"):kind=="rich"?originalRich:kind=="templated"?Elements(cell).OfType<TextBlock>().Single(t=>t.Text=="MMMMMMMMMMMM"):Elements(cell).OfType<TextBlock>().Single(t=>t.Name=="PART_Text");var border=Elements(cell).OfType<Border>().FirstOrDefault(b=>b.Name=="PART_CellBorder");var text=target.Text;
      var probe=new TextBlock{UseLayoutRounding=false,Text=text,FontFamily=cell.FontFamily,FontSize=cell.FontSize,FontWeight=cell.FontWeight,FontStyle=cell.FontStyle,FontStretch=cell.FontStretch,CharacterSpacing=cell.CharacterSpacing,IsTextScaleFactorEnabled=cell.IsTextScaleFactorEnabled,TextWrapping=TextWrapping.NoWrap};probe.Measure(new Size(double.PositiveInfinity,double.PositiveInfinity));
      var minimum=probe.DesiredSize.Width+(text.Length==0||kind is "rich" or "templated"?0:cell.FontSize*cell.CharacterSpacing/1000)+cell.Padding.Left+cell.Padding.Right+cell.BorderThickness.Left+cell.BorderThickness.Right+(kind=="sortable"?18:0);
      var point=cell.TransformToVisual(root).TransformPoint(new Point());var color=((SolidColorBrush)cell.BorderBrush).Color;var bindings=Properties.Count(p=>cell.GetBindingExpression(p)is not null);var p=cell.Padding;var b=cell.BorderThickness;
      details.Add(new{kind,originalCellRetained=kind=="caption"?ReferenceEquals(table.Sections.OfType<BleeckerTableCaption>().Single(),cell):ReferenceEquals(table.Sections.OfType<Grid>().Single().Children.OfType<BleeckerTableRow>().Single().Cells.Single(),cell),bindingExpressionsRetained=bindings==0||Properties.Select((p,i)=>ReferenceEquals(cell.ReadLocalValue(p),originalBindings[cell][i])).All(v=>v),originalRichContentRetained=kind!="rich"||ReferenceEquals(cell.Content,originalRich),bindings,cellWidth=cell.ActualWidth,cellHeight=cell.ActualHeight,minimumWidth=minimum,renderedFontStyle=target.FontStyle.ToString(),renderedFontStretch=target.FontStretch.ToString(),renderedSpacing=target.CharacterSpacing,renderedFontSize=target.FontSize,renderedLineHeight=target.LineHeight,fontStyle=cell.FontStyle.ToString(),fontStretch=cell.FontStretch.ToString(),spacing=cell.CharacterSpacing,fontSize=cell.FontSize,lineHeight=cell.LineHeight,borderExists=border is not null,borderThickness=border is null?null:new[]{border.BorderThickness.Left,border.BorderThickness.Top,border.BorderThickness.Right,border.BorderThickness.Bottom},padding=new[]{p.Left,p.Top,p.Right,p.Bottom},expectedBorderThickness=new[]{b.Left,b.Top,b.Right,b.Bottom},cornerRadius=border?.CornerRadius.TopLeft,expectedCornerRadius=cell.CornerRadius.TopLeft,paint=new{x=(int)Math.Round(point.X+40),y=(int)Math.Floor(point.Y+1),rgb=new[]{(int)color.R,(int)color.G,(int)color.B}},scrollableWidth=table.ScrollableWidth});
     }
     var file=$"table-customization-{scheme}-{step}.png";await Capture(file);snapshots.Add(new{scheme,stage=step,cells=details,file});
    }
    root.Children.Clear();await Layout();
   }
   string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-customization-native-v1",passed=true,snapshots,pngs,guards,focus,activations,activateCalled=false,showWindowCalled=false,focusCalled=false,hardwareInputSent=false,physicalInputVerified=false,sourceRasterParityVerified=false,nativeComponentAccepted=false,publicApisOnly=true,librarySha256=Hash(typeof(BleeckerTable).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(0);
  }catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-table-customization-native-v1",passed=false,stage,error=error.ToString(),snapshots,guards,focus,activations},new JsonSerializerOptions{WriteIndented=true}));window.Close();Environment.Exit(1);}
 }
}
