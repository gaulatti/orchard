using System.Numerics;
using System.Security.Cryptography;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;

namespace Orchard.Testing;

internal static class OverlayShadowSamplingTrace
{
    internal static async Task<object> CaptureAsync(PixelShaderEffect actual, int width, int height, string folder, string id)
    {
        using var resource = typeof(OverlayShadowSamplingTrace).Assembly.GetManifestResourceStream("Orchard.Testing.ShadowSampling.bin")
            ?? throw new InvalidOperationException("Missing diagnostic sampling shader");
        using var memory = new MemoryStream(); await resource.CopyToAsync(memory); var bytecode = memory.ToArray();
        using var shader = new PixelShaderEffect(bytecode)
        {
            BufferPrecision = CanvasBufferPrecision.Precision32Float, Source1 = actual.Source1,
            Source1Interpolation = actual.Source1Interpolation, Source1BorderMode = actual.Source1BorderMode, Source1Mapping = actual.Source1Mapping
        };
        foreach (var property in new[] { "Box", "CasterBox", "Radius", "Dimension" }) shader.Properties[property] = actual.Properties[property];
        using var target = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96,
            Windows.Graphics.DirectX.DirectXPixelFormat.R32G32B32A32Float, CanvasAlphaMode.Premultiplied);
        var rows = new List<object>(); string[] names = ["input-coordinates", "sample-and-exterior", "legacy-and-reordered-uv", "warp-and-position", "scene-and-normalized"];
        for (var mode = 0; mode < names.Length; mode++)
        {
            shader.Properties["Mode"] = (float)mode;
            using (var drawing = target.CreateDrawingSession())
            {
                drawing.Clear(Microsoft.UI.Colors.Transparent); drawing.Blend = CanvasBlend.Copy;
                drawing.EffectBufferPrecision = CanvasBufferPrecision.Precision32Float;
                drawing.DrawImage(shader, Vector2.Zero, new Windows.Foundation.Rect(0, 0, width, height));
            }
            var bytes = target.GetPixelBytes(); if (bytes.Length != width * height * 16) throw new InvalidOperationException("Incomplete sampling trace");
            var file = id + "-sampling-" + names[mode] + ".rgba32f"; await File.WriteAllBytesAsync(Path.Combine(folder, file), bytes);
            rows.Add(new { name = names[mode], file, sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)), width, height, bytesPerPixel = 16, format = "R32G32B32A32Float", dataOnly = true });
        }
        return new { diagnosticOnly = true, rows, shaderSha256 = Convert.ToHexStringLower(SHA256.HashData(bytecode)), libraryShaderChanged = false, libraryPropertiesChanged = false, sourcePixelsUsed = false, readbackPixelsUsedAsInputs = false, nativeComponentAccepted = false };
    }
}
