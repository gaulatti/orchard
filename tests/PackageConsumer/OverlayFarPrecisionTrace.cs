using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Windows.Graphics.DirectX;

namespace Orchard.Testing;

internal static class OverlayFarPrecisionTrace
{
    internal static async Task<object> CaptureAsync(PixelShaderEffect actual, ColorMatrixEffect actualPaint,
        ArithmeticCompositeEffect[] arithmetic, int width, int height, Vector2 origin, Windows.UI.Color backdrop, string folder, string id)
    {
        if (arithmetic.Length != 2) throw new InvalidOperationException("Far trace needs both actual decoder operations");
        async Task<byte[]> Resource(System.Reflection.Assembly assembly, string name)
        {
            using var input = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Missing far trace resource " + name);
            using var output = new MemoryStream(); await input.CopyToAsync(output); return output.ToArray();
        }
        var libraryBytes = await Resource(typeof(BleeckerOverlaySurface).Assembly, "Orchard.FarShadow.rectangle.bin");
        var rawBytes = await Resource(typeof(OverlayFarPrecisionTrace).Assembly, "Orchard.Testing.FarCoverage.bin");
        PixelShaderEffect Shader(byte[] bytes)
        {
            var shader = new PixelShaderEffect(bytes) { BufferPrecision = CanvasBufferPrecision.Precision32Float, Source1 = actual.Source1, Source1Interpolation = actual.Source1Interpolation, Source1Mapping = actual.Source1Mapping, Source1BorderMode = actual.Source1BorderMode };
            foreach (var name in new[] { "Box", "Caster", "Radius", "Sigma", "Dimension", "Fast" }) shader.Properties[name] = actual.Properties[name];
            return shader;
        }
        CanvasRenderTarget Target(DirectXPixelFormat format) => new(CanvasDevice.GetSharedDevice(), width, height, 96, format, CanvasAlphaMode.Premultiplied);
        void Draw(CanvasRenderTarget target, ICanvasImage image, bool withBackdrop = false, CanvasImageInterpolation interpolation = CanvasImageInterpolation.Linear)
        {
            using var drawing = target.CreateDrawingSession(); drawing.Clear(withBackdrop ? backdrop : Microsoft.UI.Colors.Transparent);
            drawing.Blend = withBackdrop ? CanvasBlend.SourceOver : CanvasBlend.Copy; drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision32Float;
            drawing.DrawImage(image, Vector2.Zero, new Windows.Foundation.Rect(0, 0, width, height), 1, interpolation);
        }
        var rows = new List<object>();
        async Task Save(string name, CanvasRenderTarget target, DirectXPixelFormat format)
        {
            var stride = format == DirectXPixelFormat.R32G32B32A32Float ? 16 : format == DirectXPixelFormat.R16G16B16A16Float ? 8 : 4;
            var bytes = target.GetPixelBytes(); if (bytes.Length != width * height * stride) throw new InvalidOperationException("Incomplete far precision target");
            var extension = stride == 16 ? ".rgba32f" : stride == 8 ? ".rgba16f" : ".bgra8"; var file = id + "-precision-" + name + extension;
            await File.WriteAllBytesAsync(Path.Combine(folder, file), bytes); rows.Add(new { name, file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width, height, bytesPerPixel = stride, format = format.ToString(), dpi = 96, alphaMode = "Premultiplied" });
        }
        using var raw = Shader(rawBytes); using var rawTarget = Target(DirectXPixelFormat.R32G32B32A32Float); Draw(rawTarget, raw); await Save("raw-coverage", rawTarget, DirectXPixelFormat.R32G32B32A32Float);
        var planes = new List<CanvasRenderTarget>(); var originalPlane = actual.Properties["Plane"]; var originalTable = actual.Source1;
        try
        {
            using var encoded = Shader(libraryBytes);
            for (var plane = 0; plane < 3; plane++)
            {
                encoded.Properties["Plane"] = (float)plane; var target = Target(DirectXPixelFormat.R16G16B16A16Float); planes.Add(target);
                Draw(target, encoded); await Save("plane" + plane, target, DirectXPixelFormat.R16G16B16A16Float);
            }
            ArithmeticCompositeEffect Clone(ArithmeticCompositeEffect source, ICanvasImage first, ICanvasImage second) => new() { Source1 = first, Source2 = second, MultiplyAmount = source.MultiplyAmount, Source1Amount = source.Source1Amount, Source2Amount = source.Source2Amount, Offset = source.Offset, ClampOutput = source.ClampOutput, BufferPrecision = CanvasBufferPrecision.Precision32Float };
            using var coarse = Clone(arithmetic[0], planes[0], planes[1]); using var coverage = Clone(arithmetic[1], coarse, planes[2]);
            using var coverageTarget = Target(DirectXPixelFormat.R32G32B32A32Float); Draw(coverageTarget, coverage); await Save("decoded-coverage", coverageTarget, DirectXPixelFormat.R32G32B32A32Float);
            using var paint = new ColorMatrixEffect { Source = coverage, AlphaMode = actualPaint.AlphaMode, ClampOutput = actualPaint.ClampOutput, ColorMatrix = actualPaint.ColorMatrix, BufferPrecision = CanvasBufferPrecision.Precision32Float };
            using var paintFloat = Target(DirectXPixelFormat.R32G32B32A32Float); Draw(paintFloat, paint); await Save("decoded-paint-float", paintFloat, DirectXPixelFormat.R32G32B32A32Float);
            using var output = Target(DirectXPixelFormat.B8G8R8A8UIntNormalized); Draw(output, paint); await Save("decoded-paint-byte", output, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            Draw(output, paintFloat); await Save("copied-paint-byte", output, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            Draw(output, paint, true); await Save("decoded-backdrop-byte", output, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            Draw(output, paintFloat, true); await Save("copied-backdrop-byte", output, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            Draw(output, paintFloat, false, CanvasImageInterpolation.NearestNeighbor); await Save("nearest-paint-byte", output, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            Draw(output, paintFloat, true, CanvasImageInterpolation.NearestNeighbor); await Save("nearest-backdrop-byte", output, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            if (!Equals(originalPlane, actual.Properties["Plane"]) || !ReferenceEquals(originalTable, actual.Source1)) throw new InvalidOperationException("Far trace changed actual shader properties");
            var matrix = actualPaint.ColorMatrix;
            return new { format = "orchard-far-precision-v2", diagnosticOnly = true, rows, width, height, textureOrigin = new { x = origin.X, y = origin.Y }, backdrop = new[] { backdrop.R, backdrop.G, backdrop.B, backdrop.A },
                properties = new { box = JsonSerializer.SerializeToElement(actual.Properties["Box"], new JsonSerializerOptions { IncludeFields = true }), caster = JsonSerializer.SerializeToElement(actual.Properties["Caster"], new JsonSerializerOptions { IncludeFields = true }), radius = actual.Properties["Radius"], sigma = actual.Properties["Sigma"], dimension = actual.Properties["Dimension"], fast = actual.Properties["Fast"] },
                matrixRows = new[] { new[] { matrix.M11, matrix.M12, matrix.M13, matrix.M14 }, new[] { matrix.M21, matrix.M22, matrix.M23, matrix.M24 }, new[] { matrix.M31, matrix.M32, matrix.M33, matrix.M34 }, new[] { matrix.M41, matrix.M42, matrix.M43, matrix.M44 }, new[] { matrix.M51, matrix.M52, matrix.M53, matrix.M54 } },
                shaderSha256 = Convert.ToHexStringLower(SHA256.HashData(libraryBytes)), rawShaderSha256 = Convert.ToHexStringLower(SHA256.HashData(rawBytes)), actualShaderPropertiesRetained = true, actualSurfaceDrawnByDriver = false, sourcePixelsUsed = false, readbackPixelsUsedAsInputs = false, sharedCanvasDeviceDisposed = false, nativeComponentAccepted = false };
        }
        finally { foreach (var plane in planes) plane.Dispose(); }
    }
}
