using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Orchard.Catalog;

public sealed partial class StatCardsFixture
{
    private async Task VerifyContentAsync(Grid host,NativeInputSession input,string folder,BleeckerColorScheme scheme)
    {
        using var reference=JsonDocument.Parse(typeof(StatCardsFixture).Assembly.GetManifestResourceStream("Orchard.Catalog.Reference.StatCardContent.json")!);
        var reports=new List<object>();var failures=new List<string>();
        foreach(var sample in reference.RootElement.GetProperty("cases").EnumerateArray().Where(item=>item.GetProperty("scheme").GetString()==scheme.ToString()))
        {
            var id=sample.GetProperty("id").GetString()!;var props=sample.GetProperty("props");var expected=sample.GetProperty("layout");
            var card=new BleeckerStatCard{Title=String(props,"title"),Value=props.TryGetProperty("value",out var value)?Scalar(value):null,Description=String(props,"description"),Width=expected.GetProperty("card").GetProperty("width").GetDouble(),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};
            if(props.TryGetProperty("iconValue",out var iconValue))card.Icon=Scalar(iconValue);else if(props.TryGetProperty("icon",out var icon)&&icon.GetBoolean())card.Icon=new Border{Width=12,Height=12,Background=Paint(166,93,87)};
            if(props.TryGetProperty("footerValue",out var footerValue))card.Footer=Scalar(footerValue);
            host.Children.Clear();host.Children.Add(card);await AwaitAsync(()=>card.IsLoaded,"Content Stat Card did not mount.");card.ApplyTemplate();UpdateLayout();await FrameAsync();UpdateLayout();
            var lead=Part<StackPanel>(card,"PART_Lead");var title=Part<TextBlock>(card,"PART_Title");var text=Part<TextBlock>(card,"PART_Value");var footerPart=Part<Border>(card,"PART_Footer");var iconPart=Part<Border>(card,"PART_Icon");
            var footerText=string.Concat(Descendants<TextBlock>(footerPart).Select(element=>element.Text));var iconText=string.Concat(Descendants<TextBlock>(iconPart).Select(element=>element.Text));
            string? error=null;
            try
            {
                Near(card.ActualHeight,expected.GetProperty("card").GetProperty("height").GetDouble(),id+" content height");
                Near(lead.ActualWidth,expected.GetProperty("lead").GetProperty("width").GetDouble(),id+" content lead width");
                Require(title.Text==expected.GetProperty("title").GetProperty("renderedText").GetString()&&text.Text==expected.GetProperty("value").GetProperty("renderedText").GetString(),id+" changed scalar text.");
                var sourceFooter=expected.GetProperty("footer");var sourceIcon=expected.GetProperty("icon");
                Require(footerPart.Visibility==(sourceFooter.ValueKind==JsonValueKind.Null?Visibility.Collapsed:Visibility.Visible),id+" changed footer wrapper presence.");
                Require(iconPart.Visibility==(sourceIcon.ValueKind==JsonValueKind.Null?Visibility.Collapsed:Visibility.Visible),id+" changed icon wrapper presence.");
                if(props.TryGetProperty("footerValue",out _)&&sourceFooter.ValueKind!=JsonValueKind.Null)Require(footerText==sourceFooter.GetProperty("renderedText").GetString(),id+" changed footer scalar text.");
                if(props.TryGetProperty("iconValue",out _)&&sourceIcon.ValueKind!=JsonValueKind.Null)Require(iconText==sourceIcon.GetProperty("renderedText").GetString(),id+" changed icon scalar text.");
                foreach(var (key,name) in new[]{("anonymousFooterText","PART_FooterScalar"),("anonymousIconText","PART_IconScalar")})
                {
                    var expectedText=expected.GetProperty(key).GetString();if(string.IsNullOrEmpty(expectedText))continue;
                    Require(Descendants<TextBlock>(card).Any(element=>element.Name==name&&element.Visibility==Visibility.Visible&&element.Text==expectedText),id+" omitted anonymous scalar content.");
                }
            }
            catch(InvalidOperationException exception){error=exception.Message;failures.Add(error);}
            await MainWindow.CaptureElementAsync(card,System.IO.Path.Combine(folder,$"stat-content-{id}-{scheme}.png"));
            if(id is "title-unbroken" or "title-mixed" or "description-unbroken" or "value-mixed")
                try{await VerifyContentOverflowAsync(input,card,id,folder,scheme);}catch(InvalidOperationException exception){error=exception.Message;failures.Add(error);}
            reports.Add(new{id,error,height=card.ActualHeight,leadWidth=lead.ActualWidth,titleHeight=title.ActualHeight,valueHeight=text.ActualHeight,valueText=text.Text,footer=footerPart.Visibility.ToString(),footerHeight=footerPart.ActualHeight,footerText,icon=iconPart.Visibility.ToString(),iconText});
        }
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"stat-card-content-{scheme}.json"),JsonSerializer.Serialize(new{passed=failures.Count==0,reports},new JsonSerializerOptions{WriteIndented=true}));
        Require(failures.Count==0,"Stat Card content mismatches: "+string.Join(" | ",failures));
    }
    private async Task VerifyContentOverflowAsync(NativeInputSession input,BleeckerStatCard card,string id,string folder,BleeckerColorScheme scheme)
    {
        var path=System.IO.Path.Combine(folder,$"stat-content-desktop-{id}-{scheme}.png");var client=await NativeScreenCapture.CaptureClientAsync(input.Window,path);
        using var stream=new Windows.Storage.Streams.InMemoryRandomAccessStream();
        using(var writer=new Windows.Storage.Streams.DataWriter(stream)){writer.WriteBytes(await File.ReadAllBytesAsync(path));await writer.StoreAsync();writer.DetachStream();}stream.Seek(0);
        var decoder=await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
        var pixels=(await decoder.GetPixelDataAsync(Windows.Graphics.Imaging.BitmapPixelFormat.Bgra8,Windows.Graphics.Imaging.BitmapAlphaMode.Straight,new Windows.Graphics.Imaging.BitmapTransform(),Windows.Graphics.Imaging.ExifOrientationMode.IgnoreExifOrientation,Windows.Graphics.Imaging.ColorManagementMode.DoNotColorManage)).DetachPixelData();
        var name=id.StartsWith("title",StringComparison.Ordinal)?"PART_Title":id.StartsWith("description",StringComparison.Ordinal)?"PART_Description":"PART_Value";
        var text=input.ScreenBounds(Part<TextBlock>(card,name));var lead=input.ScreenBounds(Part<StackPanel>(card,"PART_Lead"));var icon=input.ScreenBounds(Part<Border>(card,"PART_Icon"));var bounds=input.ScreenBounds(card);
        var fill=((SolidColorBrush)card.Background).Color;var outside=((SolidColorBrush)((Grid)XamlRoot.Content).Background).Color;
        int Changed(double left,double right,Windows.UI.Color background)
        {
            var count=0;
            for(var y=(int)Math.Ceiling(text.Y-client.Y);y<Math.Floor(text.Bottom-client.Y);y++)
                for(var x=(int)Math.Ceiling(left-client.X);x<Math.Floor(right-client.X);x++)
                {
                    var offset=(y*(int)decoder.PixelWidth+x)*4;
                    if(Math.Max(Math.Abs(pixels[offset]-background.B),Math.Max(Math.Abs(pixels[offset+1]-background.G),Math.Abs(pixels[offset+2]-background.R)))>20)count++;
                }
            return count;
        }
        var overflowInk=Changed(lead.Right+1,icon.X-2,fill);var leakedInk=Changed(bounds.Right+2,Math.Min(bounds.Right+80,client.Right-20),outside);
        await File.WriteAllTextAsync(System.IO.Path.Combine(folder,$"stat-content-overflow-{id}-{scheme}.json"),JsonSerializer.Serialize(new{overflowInk,leakedInk,text,lead,bounds}));
        Require(overflowInk>0,$"Stat Card {id} clips text at its leading column.");
        Require(leakedInk==0,$"Stat Card {id} leaks text beyond its outer surface.");
    }
    private static object? Scalar(JsonElement element)=>element.ValueKind switch{JsonValueKind.String=>element.GetString(),JsonValueKind.Number=>element.GetDouble(),JsonValueKind.True=>true,JsonValueKind.False=>false,JsonValueKind.Null=>null,_=>throw new InvalidOperationException("Unsupported frozen scalar.")};
}
