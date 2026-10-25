// Color-conversion operations adapted from Chromium (Copyright 2012 The
// Chromium Authors) and Skia (Copyright 2011 Google Inc.). BSD notices are
// distributed in licenses/LICENSE-Chromium.txt and licenses/LICENSE-Skia.txt.
namespace Orchard;

// A clipped CSS color-mix fill reaches the renderer as premultiplied BGRA8.
// Keep float stages separate: combining the matrices or rounding to an opaque
// RGB endpoint first changes channels at half-byte boundaries.
internal static class BleeckerOklabFill
{
    private readonly record struct Triple(float X, float Y, float Z);
    private static Triple Apply(float[][] m, Triple v) => new(
        m[0][0] * v.X + m[0][1] * v.Y + m[0][2] * v.Z,
        m[1][0] * v.X + m[1][1] * v.Y + m[1][2] * v.Z,
        m[2][0] * v.X + m[2][1] * v.Y + m[2][2] * v.Z);

    private static readonly float[][] Srgb = [[0x6fa2 / 65536f, 0x6299 / 65536f, 0x24a0 / 65536f], [0x38f5 / 65536f, 0xb785 / 65536f, 0x0f84 / 65536f], [0x0390 / 65536f, 0x18da / 65536f, 0xb6cf / 65536f]];
    private static readonly float[][] SrgbInverse = Invert(Srgb);
    private static readonly float[][] Lms = [[.8190224432164319f, .3619062562801221f, -.12887378261216414f], [.0329836671980271f, .9292868468965546f, .03614466816999844f], [.048177199566046255f, .26423952494422764f, .6335478258136937f]];
    private static readonly float[][] LmsInverse = Invert(Lms);
    private static readonly float[][] LabToLms = [[.9999999984505198f, .39633779217376786f, .2158037580607588f], [1.0000000088817609f, -.10556134232365635f, -.06385417477170591f], [1.0000000546724108f, -.08948418209496575f, -1.2914855378640917f]];
    private static readonly float[][] LmsToLab = Invert(LabToLms);
    private static readonly float[][] Adapt = Adaptation();
    private static readonly float[][] AdaptInverse = Invert(Adapt);
    private static readonly float Gamma = 2.4f, First = (float)(1 / 1.055), Second = (float)(.055 / 1.055), Linear = (float)(1 / 12.92), Threshold = .04045f;
    private static readonly float InverseFirst = MathF.Pow(First, -Gamma), InverseGamma = 1 / Gamma, InverseOffset = 1 - MathF.Pow(InverseFirst * Decode(1), InverseGamma), InverseThreshold = Linear * Threshold, InverseLinear = 1 / Linear;

    internal static (byte R, byte G, byte B, byte A) Encode(byte red, byte green, byte blue, byte sourceAlpha, double opacity)
    {
        if (!double.IsFinite(opacity) || opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity));
        if (opacity == 0 || sourceAlpha == 0) return (0, 0, 0, 0);
        var xyz = Apply(Srgb, new(Decode(red / 255f), Decode(green / 255f), Decode(blue / 255f)));
        var cone = Apply(Lms, Apply(AdaptInverse, xyz));
        var lab = Apply(LmsToLab, new(Root(cone.X), Root(cone.Y), Root(cone.Z)));
        var endpointAlpha = sourceAlpha / 255f;
        var percentage = 1 - (float)opacity;
        var alpha = endpointAlpha + (-endpointAlpha) * percentage;
        if (alpha == 0) return (0, 0, 0, 0);
        float Mix(float v) { v *= endpointAlpha; return (v + (-v) * percentage) / alpha; }
        cone = Apply(LabToLms, new(Mix(lab.X), Mix(lab.Y), Mix(lab.Z)));
        xyz = Apply(Adapt, Apply(LmsInverse, new(cone.X * cone.X * cone.X, cone.Y * cone.Y * cone.Y, cone.Z * cone.Z * cone.Z)));
        var rgb = Apply(SrgbInverse, xyz);
        return (Byte(EncodeChannel(rgb.X) * alpha * 255), Byte(EncodeChannel(rgb.Y) * alpha * 255), Byte(EncodeChannel(rgb.Z) * alpha * 255), Byte(alpha * 255));
    }
    private static byte Byte(float v) => (byte)Math.Clamp(MathF.Floor(v + .5f), 0, 255);
    private static float Root(float v) => (v < 0 ? -1 : 1) * MathF.Pow(Math.Abs(v), 1f / 3f);
    private static float Decode(float v) => v <= Threshold ? Linear * v : MathF.Pow(Math.Max(First * v + Second, 0), Gamma);
    private static float EncodeChannel(float v)
    {
        var sign = v < 0 ? -1f : 1f; var x = Math.Abs(v);
        return Math.Clamp(sign * (x <= InverseThreshold ? InverseLinear * x : MathF.Pow(Math.Max(InverseFirst * x, 0), InverseGamma) + InverseOffset), 0, 1);
    }
    private static float[][] Adaptation()
    {
        float[][] forward = [[.8951f, .2664f, -.1614f], [-.7502f, 1.7135f, .0367f], [.0389f, -.0685f, 1.0296f]], reverse = [[.9869929f, -.1470543f, .1599627f], [.4323053f, .5183603f, .0492912f], [-.0085287f, .0400428f, .9684867f]];
        var wx = .3127f; var wy = .3290f; var cone = Apply(forward, new(wx / wy, 1, (1 - wx - wy) / wy)); var target = Apply(forward, new(.96422f, 1, .82521f));
        float[][] scale = [[target.X / cone.X, 0, 0], [0, target.Y / cone.Y, 0], [0, 0, target.Z / cone.Z]];
        return Concat(reverse, Concat(scale, forward));
    }
    private static float[][] Concat(float[][] a, float[][] b) => Enumerable.Range(0, 3).Select(r => Enumerable.Range(0, 3).Select(c => a[r][0] * b[0][c] + a[r][1] * b[1][c] + a[r][2] * b[2][c]).ToArray()).ToArray();
    private static float[][] Invert(float[][] m)
    {
        double a = m[0][0], b = m[0][1], c = m[0][2], d = m[1][0], e = m[1][1], v = m[1][2], g = m[2][0], h = m[2][1], i = m[2][2]; var det = a * (e * i - v * h) - b * (d * i - v * g) + c * (d * h - e * g);
        return [[(float)((e * i - v * h) / det), (float)((c * h - b * i) / det), (float)((b * v - c * e) / det)], [(float)((v * g - d * i) / det), (float)((a * i - c * g) / det), (float)((c * d - a * v) / det)], [(float)((d * h - e * g) / det), (float)((b * g - a * h) / det), (float)((a * e - b * d) / det)]];
    }
}
