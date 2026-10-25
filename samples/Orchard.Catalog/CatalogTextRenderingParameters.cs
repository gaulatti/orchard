using System.Runtime.InteropServices;
using Microsoft.Graphics.Canvas.Text;

namespace Orchard.Catalog;

// Catalog-only DirectWrite/Win2D interop. These objects configure one drawing
// session and never update monitor, registry or application text settings.
internal static class CatalogTextRenderingParameters
{
    internal sealed record Settings(float Gamma, float EnhancedContrast, float ClearTypeLevel, uint PixelGeometry, uint RenderingMode);
    private static readonly Guid factoryId = new("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
    private static readonly Guid paramsId = new("2f0da53a-2add-47cd-82ee-d9ec34688e75");
    private static readonly Guid canvasFactoryId = new("695c440d-04b3-4edd-bfd9-63e51e9f7202");
    private static readonly Guid canvasResourceId = new("5f10688d-ea55-4d55-a3b0-4ddb55c0c20a");
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int CreateParams(nint self, float gamma, float contrast, float level, uint geometry, uint mode, out nint parameters);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Wrap(nint self, nint device, nint resource, float dpi, out nint wrapper);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate int Unwrap(nint self, nint device, float dpi, ref Guid id, out nint resource);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate float FloatValue(nint self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate uint UIntValue(nint self);
    [DllImport("dwrite.dll", ExactSpelling = true)] private static extern int DWriteCreateFactory(uint type, ref Guid id, out nint factory);
    [DllImport("combase.dll", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern int WindowsCreateString(string value, uint length, out nint text);
    [DllImport("combase.dll", ExactSpelling = true)] private static extern int WindowsDeleteString(nint text);
    [DllImport("Microsoft.Graphics.Canvas.dll", EntryPoint = "DllGetActivationFactory", ExactSpelling = true)] private static extern int CanvasActivationFactory(nint name, out nint factory);
    private static T Method<T>(nint self, int index) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(self), index * nint.Size));
    private static void Release(nint value) { if (value != 0) Marshal.Release(value); }

    internal static CanvasTextRenderingParameters Create(float gamma, float contrast, uint mode)
    {
        nint factory = 0, parameters = 0, name = 0, activation = 0, canvasFactory = 0, wrapper = 0;
        try
        {
            var id = factoryId; Marshal.ThrowExceptionForHR(DWriteCreateFactory(0, ref id, out factory));
            // IDWriteFactory v1, after IUnknown and the nine preceding methods.
            Marshal.ThrowExceptionForHR(Method<CreateParams>(factory, 12)(factory, gamma, contrast, 1, 1, mode, out parameters));
            const string runtimeClass = "Microsoft.Graphics.Canvas.CanvasDevice";
            Marshal.ThrowExceptionForHR(WindowsCreateString(runtimeClass, (uint)runtimeClass.Length, out name));
            Marshal.ThrowExceptionForHR(CanvasActivationFactory(name, out activation));
            id = canvasFactoryId; Marshal.ThrowExceptionForHR(Marshal.QueryInterface(activation, in id, out canvasFactory));
            // ICanvasFactoryNative derives from IInspectable: GetOrCreate is 6.
            Marshal.ThrowExceptionForHR(Method<Wrap>(canvasFactory, 6)(canvasFactory, 0, parameters, 0, out wrapper));
            return WinRT.MarshalInspectable<CanvasTextRenderingParameters>.FromAbi(wrapper) ?? throw new InvalidOperationException("Win2D did not wrap the native rendering parameters.");
        }
        finally { Release(wrapper); Release(canvasFactory); Release(activation); if (name != 0) Marshal.ThrowExceptionForHR(WindowsDeleteString(name)); Release(parameters); Release(factory); }
    }

    internal static Settings Read(CanvasTextRenderingParameters value)
    {
        nint inspectable = 0, resourceWrapper = 0, parameters = 0;
        try
        {
            inspectable = WinRT.MarshalInspectable<CanvasTextRenderingParameters>.FromManaged(value);
            var id = canvasResourceId; Marshal.ThrowExceptionForHR(Marshal.QueryInterface(inspectable, in id, out resourceWrapper));
            id = paramsId; Marshal.ThrowExceptionForHR(Method<Unwrap>(resourceWrapper, 3)(resourceWrapper, 0, 0, ref id, out parameters));
            return new(Method<FloatValue>(parameters, 3)(parameters), Method<FloatValue>(parameters, 4)(parameters), Method<FloatValue>(parameters, 5)(parameters), Method<UIntValue>(parameters, 6)(parameters), Method<UIntValue>(parameters, 7)(parameters));
        }
        finally { Release(parameters); Release(resourceWrapper); Release(inspectable); }
    }
}
