using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;
namespace PackageConsumer;
public partial class App
{
    private static async Task<List<object>> CaptureHiddenProgressStackPaintAsync(Canvas root,string references,string output,Func<FrameworkElement,Task> layout,Action guard)
    {
        using var reference=JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(references,"ProgressStackPaint.json")));var frames=new List<object>();
        foreach(var row in reference.RootElement.GetProperty("cases").EnumerateArray())
        {
            var scheme=row.GetProperty("scheme").GetString()!;root.RequestedTheme=Enum.Parse<ElementTheme>(scheme);
            var control=new BleeckerProgressStack{Width=280};root.Children.Clear();root.Children.Add(control);await layout(control);
            var brush=control.Background as SolidColorBrush??throw new InvalidOperationException("Default track brush must be solid.");
            var rgba=row.GetProperty("backdrop").EnumerateArray().Select(channel=>channel.GetByte()).ToArray();
            root.Background=new SolidColorBrush(Windows.UI.Color.FromArgb(rgba[3],rgba[0],rgba[1],rgba[2]));
            // A separate square isolates the actual control brush. The full
            // component captures retain all original rounded edges and text.
            var square=new Border{Width=40,Height=40,UseLayoutRounding=false,Background=brush};
            root.Children.Clear();Canvas.SetLeft(square,20);Canvas.SetTop(square,20);root.Children.Add(square);await layout(square);
            var capture=await CaptureHiddenProgressStackAsync(root,output,row.GetProperty("file").GetString()!,guard);
            frames.Add(new{id=row.GetProperty("id").GetString(),scheme,brush=new{r=brush.Color.R,g=brush.Color.G,b=brush.Color.B,a=brush.Color.A,opacity=brush.Opacity},capture});
        }
        return frames;
    }
}
