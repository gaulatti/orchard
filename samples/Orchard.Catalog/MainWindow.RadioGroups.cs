using System.Reflection;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Catalog;
public sealed partial class MainWindow
{
    private async Task RunRadioGroupsAsync(string folder)
    {
        using var resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Orchard.Catalog.Reference.RadioGroup.json")??throw new InvalidOperationException("Missing radio reference.");using var source=await JsonDocument.ParseAsync(resource);
        var original=Content;var canvas=new Canvas{Background=new SolidColorBrush(Microsoft.UI.Colors.White)};var frames=new List<object>();var errors=new List<object>();
        try
        {
            Content=canvas;await AwaitPresentationAsync(canvas);using var input=new NativeInputSession(canvas.XamlRoot,folder,"radio-reference");await input.AcquireAsync(canvas);
            var width=(int)canvas.XamlRoot.Size.Width;Require(width is 320 or 640,"Radio reference requires a 320 or 640px client.");
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                canvas.RequestedTheme=theme;canvas.Background=new SolidColorBrush(theme==ElementTheme.Light?Microsoft.UI.Colors.White:Windows.UI.Color.FromArgb(255,13,24,33));
                foreach(var frame in source.RootElement.GetProperty("cases").EnumerateArray().Where(row=>row.GetProperty("width").GetInt32()==width&&row.GetProperty("scheme").GetString()==theme.ToString()))
                {
                    canvas.Children.Clear();var spec=frame.GetProperty("spec");var group=new BleeckerRadioGroup{Width=width-40,Value=spec.TryGetProperty("value",out var value)?value.GetString():null,IsEnabled=!spec.TryGetProperty("disabled",out var disabled)||!disabled.GetBoolean(),Orientation=spec.TryGetProperty("orientation",out var orientation)&&orientation.GetString()=="horizontal"?Orientation.Horizontal:Orientation.Vertical,FlowDirection=spec.TryGetProperty("dir",out var dir)&&dir.GetString()=="rtl"?FlowDirection.RightToLeft:FlowDirection.LeftToRight};
                    foreach(var option in spec.GetProperty("options").EnumerateArray())group.Options.Add(new(option.GetProperty("value").GetString()!,option.GetProperty("label").GetString()!,option.TryGetProperty("description",out var description)?description.GetString():null,option.TryGetProperty("disabled",out var off)&&off.GetBoolean()));
                    Canvas.SetLeft(group,20);Canvas.SetTop(group,20);canvas.Children.Add(group);var outside=new Border{Background=new SolidColorBrush(Microsoft.UI.Colors.Transparent),Width=80,Height=24};Canvas.SetLeft(outside,20);Canvas.SetTop(outside,606);canvas.Children.Add(outside);await AwaitPresentationAsync(group);await input.MoveAsync(outside);
                    if(spec.TryGetProperty("hover",out var hover))await input.MoveAsync(group.OptionButtons[group.Options.ToList().FindIndex(option=>option.Value==hover.GetString())]);
                    await AwaitPresentationAsync(group);
                    object Box(FrameworkElement element){var b=element.TransformToVisual(canvas).TransformBounds(new Rect(0,0,element.ActualSize.X,element.ActualSize.Y));return new{x=b.X,y=b.Y,width=b.Width,height=b.Height};}
                    static object Paint(Brush brush){var b=(SolidColorBrush)brush;return new{color=b.Color.ToString(),opacity=b.Opacity};}
                    var actual=new{id=frame.GetProperty("id").GetString(),width,scheme=theme.ToString(),box=Box(group),options=group.OptionButtons.Select((button,index)=>
                    {
                        var ring=Desc<Microsoft.UI.Xaml.Shapes.Ellipse>(button).Single(x=>x.Name=="RadioRing");var dot=Desc<Microsoft.UI.Xaml.Shapes.Ellipse>(button).Single(x=>x.Name=="RadioDot");var label=Desc<TextBlock>(button).Single(x=>x.Name=="RadioLabel");var description=Desc<TextBlock>(button).Single(x=>x.Name=="RadioDescription");var option=group.Options[index];var root=Desc<Grid>(button).Single(x=>x.Name=="OptionRoot");
                        return new{textMeasurements=((BleeckerRadioGroupOptionBase)button).TextMeasurements,option.Value,selected=group.Value==option.Value,disabled=!button.IsEnabled,box=Box(button),opacity=root.Opacity,ring=new{box=Box(ring),stroke=Paint(ring.Stroke),ring.StrokeThickness},dot=new{box=Box(dot),fill=Paint(dot.Fill),scale=((ScaleTransform)dot.RenderTransform).ScaleX},label=new{box=Box(label),label.FontSize,label.LineHeight,weight=label.FontWeight.Weight,fontFamily=label.FontFamily.Source,foreground=Paint(label.Foreground),inkWidth=label.ActualWidth,inkHeight=label.ActualHeight},description=description.Visibility==Visibility.Collapsed?null:new{box=Box(description),description.FontSize,description.LineHeight,weight=description.FontWeight.Weight,fontFamily=description.FontFamily.Source,foreground=Paint(description.Foreground),inkWidth=description.ActualWidth,inkHeight=description.ActualHeight}};
                    }).ToArray()};
                    frames.Add(actual);await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"radio-reference.json"),JsonSerializer.Serialize(new{passed=false,frames,errors}));var background=((SolidColorBrush)canvas.Background).Color;await NativeScreenCapture.CaptureClientAsync(input.Window,System.IO.Path.Combine(folder,frame.GetProperty("file").GetString()!),group.Options.Count==0?(background.R,background.G,background.B):null);
                }
                await NativeScreenCapture.CaptureClientAsync(input.Window,System.IO.Path.Combine(folder,"catalog-"+theme.ToString().ToLowerInvariant()+".png"));
            }
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"radio-reference.json"),JsonSerializer.Serialize(new{passed=true,frames,errors}));
            await File.WriteAllTextAsync(System.IO.Path.Combine(folder,"result.json"),JsonSerializer.Serialize(new{passed=true,component="radio-reference",environment=RuntimeEnvironment()}));
        }
        finally{Content=original;await AwaitPresentationAsync(CatalogRoot);}
    }
    internal static IEnumerable<T> Desc<T>(DependencyObject root)where T:DependencyObject
    {for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++){var child=VisualTreeHelper.GetChild(root,i);if(child is T item)yield return item;foreach(var item2 in Desc<T>(child))yield return item2;}}
}
