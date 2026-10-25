using System.Security.Cryptography;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
namespace PackageConsumer;

public partial class App
{
    private static async Task<object> CaptureHiddenProgressStackAsync(FrameworkElement root,string folder,string file,Action guard,int width=640,int height=400)
    {
        if(Path.GetFileName(file)!=file||!file.EndsWith(".png",StringComparison.Ordinal))throw new InvalidOperationException("Invalid hidden capture filename.");
        guard();var bitmap=new RenderTargetBitmap();var operation=bitmap.RenderAsync(root);var render=operation.AsTask();
        if(await Task.WhenAny(render,Task.Delay(2000))!=render){operation.Cancel();throw new TimeoutException("Hidden XAML rendering exceeded two seconds.");}
        await render;guard();
        if(bitmap.PixelWidth!=width||bitmap.PixelHeight!=height)throw new InvalidOperationException("Hidden capture must retain the source's unscaled frame dimensions.");
        var buffer=await bitmap.GetPixelsAsync();var pixels=new byte[checked((int)buffer.Length)];using(var reader=DataReader.FromBuffer(buffer))reader.ReadBytes(pixels);
        if(pixels.Length!=checked(width*height*4))throw new InvalidOperationException("Hidden capture returned an incomplete pixel buffer.");
        guard();using var stream=new InMemoryRandomAccessStream();var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)bitmap.PixelWidth,(uint)bitmap.PixelHeight,96,96,pixels);await encoder.FlushAsync();
        using var output=new DataReader(stream.GetInputStreamAt(0));await output.LoadAsync(checked((uint)stream.Size));var png=new byte[checked((int)stream.Size)];output.ReadBytes(png);
        await File.WriteAllBytesAsync(Path.Combine(folder,file),png);guard();
        return new{file,pngSha256=Convert.ToHexStringLower(SHA256.HashData(png)),pixelWidth=bitmap.PixelWidth,pixelHeight=bitmap.PixelHeight};
    }
}
