using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Orchard.Tests;

[TestClass]
public sealed class DialogCloseButtonTests
{
    [TestMethod]
    public void OriginalSourceAndRenderedStatesRemainPinned()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("a1614f004198702d1325af4dac2e2ab18fe6cf227574df2b1caf22a2e341c7e8", root.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(16, root.GetProperty("cases").GetArrayLength());
        foreach (var (file, field) in new[] { ("CaptureDialogCloseButton.cjs", "scriptSha256"), ("DialogCloseButtonClient.tsx", "clientSha256") })
            Assert.AreEqual(root.GetProperty(field).GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", file)));
        foreach (var row in root.GetProperty("cases").EnumerateArray())
        {
            Assert.AreEqual(row.GetProperty("pngSha256").GetString(), Hash(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DialogCloseButton", row.GetProperty("file").GetString()!)));
            var paint = row.GetProperty("measurement");
            Assert.AreEqual("1", paint.GetProperty("opacity").GetString());
            Assert.AreEqual("2", paint.GetProperty("strokeWidth").GetString());
            Assert.AreEqual(row.GetProperty("id").GetString() is "focus" or "keyboard-pressed", paint.GetProperty("focusVisible").GetBoolean());
        }
    }

    [TestMethod]
    public void ActivationRequestsDoNotDismissConsumerContent()
    {
        using var document = Read();
        foreach (var row in document.RootElement.GetProperty("interactions").EnumerateArray())
        {
            var requests = row.GetProperty("requests").EnumerateArray().Select(item => item.GetString()!).ToArray();
            CollectionAssert.AreEqual(new[] { "consumer-request", "consumer-request", "consumer-request" }, requests);
            CollectionAssert.AreEqual(requests, row.GetProperty("afterDisabled").EnumerateArray().Select(item => item.GetString()!).ToArray());
            Assert.IsTrue(row.GetProperty("remainsMounted").GetBoolean());
        }
    }

    [TestMethod]
    public void ProductionPaintMatchesAllBrowserTimelines()
    {
        using var document = Read(); var samples = 0;
        Assert.AreEqual(20, document.RootElement.GetProperty("transitions").GetArrayLength());
        foreach (var row in document.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var reduced = row.GetProperty("reduced").GetBoolean(); var animation = new BleeckerDialogCloseAnimation();
            animation.Retarget(Frame(row.GetProperty("before")), 0, false);
            animation.Retarget(Frame(row.GetProperty("after")), 0, !reduced);
            foreach (var definition in row.GetProperty("definitions").EnumerateArray())
                if (!reduced) Assert.AreEqual(190, definition.GetProperty("timing").GetProperty("duration").GetDouble());
            foreach (var sample in row.GetProperty("samples").EnumerateArray())
            {
                var time = sample.GetProperty("milliseconds").GetDouble(); if (reduced && time == 0) continue;
                var frame = animation.At(time);
                foreach (var (name, value) in new[] { ("foreground", frame.Foreground), ("background", frame.Background), ("border", frame.Border) })
                {
                    var wanted = Colour(sample.GetProperty(name).GetString()!); var expected = wanted.Encode(); var actual = value.Encode();
                    Assert.AreEqual(expected.R, actual.R, row.GetProperty("kind").GetString() + "/" + name + "/R/" + time);
                    Assert.AreEqual(expected.G, actual.G); Assert.AreEqual(expected.B, actual.B);
                    if (wanted.Legacy) Assert.AreEqual(Math.Round(expected.Alpha * 255), Math.Round(actual.Alpha * 255));
                    else Assert.AreEqual(expected.Alpha, actual.Alpha, .000001);
                }
                var shadow = Shadow(sample.GetProperty("shadow").GetString()!);
                Assert.AreEqual(shadow.Y, 3 * frame.Shadow, .00001); Assert.AreEqual(shadow.Blur, 9 * frame.Shadow, .00001);
                Assert.AreEqual(shadow.Spread, -6 * frame.Shadow, .00001);
                Assert.AreEqual(row.GetProperty("after").GetProperty("translate").GetString(), sample.GetProperty("translate").GetString());
                Assert.AreEqual(row.GetProperty("after").GetProperty("scale").GetString(), sample.GetProperty("scale").GetString());
                samples++;
            }
        }
        Assert.AreEqual(110, samples);
    }

    [TestMethod]
    public void IconSizesRetainZeroAndRejectInvalidGeometry()
    {
        foreach (var size in new[] { 0d, 18, 24, .5 }) BleeckerDialogCloseAnimation.ValidateIconSize(size);
        foreach (var size in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerDialogCloseAnimation.ValidateIconSize(size));
    }

    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DialogCloseButton.json")));
    private static string Hash(string file) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    private static BleeckerDialogCloseAnimation.Frame Frame(JsonElement value) => new(NativeColour(value.GetProperty("foreground").GetString()!),
        NativeColour(value.GetProperty("background").GetString()!), NativeColour(value.GetProperty("border").GetString()!), Shadow(value.GetProperty("shadow").GetString()!).Y / 3);
    private static BleeckerCssColor NativeColour(string css)
    { var colour = Colour(css); return BleeckerCssColor.FromRgba(colour.Encode(), colour.Legacy); }
    private static BleeckerCssColor Colour(string css)
    {
        var values = Regex.Matches(css, @"[-+]?(?:\d*\.)?\d+(?:[eE][-+]?\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        if (css.StartsWith("oklab", StringComparison.Ordinal)) { var alpha = values.Length == 4 ? values[3] : 1; return new(false, values[0] * alpha, values[1] * alpha, values[2] * alpha, alpha); }
        return BleeckerCssColor.FromRgba(((byte)values[0], (byte)values[1], (byte)values[2], values.Length == 4 ? values[3] : 1), true);
    }
    private static (double Y, double Blur, double Spread) Shadow(string css)
    {
        if (css == "none") return (0, 0, 0);
        var match = Regex.Matches(css, @"rgba?\([^)]+\)\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+(-?[\d.]+)px\s+(-?[\d.]+)px").Last();
        return (double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture), double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture));
    }
}
