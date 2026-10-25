using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Orchard.Testing;

internal static class NativeRenderReadiness
{
    internal static async Task RenderAsync(UIElement target, Action<WeakReference<RenderTargetBitmap>>? afterStarted = null)
    {
        // RenderAsync's action does not keep this WinUI owner alive. The fixture
        // needs the owner through completion even when it only wants Loaded.
        var bitmap = new RenderTargetBitmap();
        var operation = bitmap.RenderAsync(target);
        afterStarted?.Invoke(new WeakReference<RenderTargetBitmap>(bitmap));
        await operation;
        GC.KeepAlive(bitmap);
    }
}
