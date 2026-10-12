using Orchard;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Orchard.Tests;

[TestClass]
public sealed class TokenContractTests
{
    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";
    private static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);

    [TestMethod]
    public void BothNativePalettesMatchTheAuthoritativeSnapshot()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Fixture("bleecker.tokens.json")));
        var xaml = XDocument.Load(Fixture("Tokens.g.xaml"));
        foreach (var (name, key, scheme) in new[] { ("light", "Light", BleeckerColorScheme.Light), ("dark", "Default", BleeckerColorScheme.Dark) })
        {
            var colors = json.RootElement.GetProperty("themes").GetProperty(name);
            var native = BleeckerTokens.Colors(scheme);
            Assert.AreEqual(colors.EnumerateObject().Count(), native.Count);
            var dictionary = xaml.Descendants(Xaml + "ResourceDictionary").Single(element => (string?)element.Attribute(X + "Key") == key);
            foreach (var token in colors.EnumerateObject())
            {
                var expected = Argb(token.Value.GetString()!);
                Assert.AreEqual(expected, native[token.Name], $"C# {name}.{token.Name}");
                var brush = dictionary.Elements(Xaml + "SolidColorBrush").Single(element => (string?)element.Attribute(X + "Key") == $"Bleecker{Pascal(token.Name)}Brush");
                Assert.AreEqual(expected, (string?)brush.Attribute("Color"), $"XAML {name}.{token.Name}");
            }
        }
    }

    [TestMethod]
    public void GeometryAndTimingKeepTheBleeckerScale()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Fixture("bleecker.tokens.json")));
        foreach (var (group, native) in new[] { ("spacing", BleeckerTokens.Spacing), ("radii", BleeckerTokens.Radii), ("durations", BleeckerTokens.Durations), ("letterSpacing", BleeckerTokens.LetterSpacing) })
        {
            var tokens = json.RootElement.GetProperty(group);
            Assert.AreEqual(tokens.EnumerateObject().Count(), native.Count);
            foreach (var token in tokens.EnumerateObject()) Assert.AreEqual(token.Value.GetDouble(), native[token.Name], $"{group}.{token.Name}");
        }
        var xaml = XDocument.Load(Fixture("Tokens.g.xaml"));
        foreach (var token in BleeckerTokens.Spacing)
            Assert.AreEqual(token.Value, double.Parse(xaml.Descendants(X + "Double").Single(element => (string?)element.Attribute(X + "Key") == $"BleeckerSpacing{Pascal(token.Key)}").Value, CultureInfo.InvariantCulture));
        foreach (var token in BleeckerTokens.Radii)
            Assert.AreEqual(token.Value, double.Parse(xaml.Descendants(Xaml + "CornerRadius").Single(element => (string?)element.Attribute(X + "Key") == $"Bleecker{Pascal(token.Key)}CornerRadius").Value, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void TypographyNamesAndWeightsMatchBleecker()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Fixture("bleecker.tokens.json")));
        foreach (var (name, native) in new[] { ("fontFamilies", BleeckerTokens.FontFamilies), ("fontWeights", BleeckerTokens.FontWeights) })
            foreach (var token in json.RootElement.GetProperty(name).EnumerateObject()) Assert.AreEqual(token.Value.GetString(), native[token.Name]);
        var xaml = XDocument.Load(Fixture("Tokens.g.xaml"));
        foreach (var token in BleeckerTokens.FontFamilies)
        {
            var uri = xaml.Descendants(Xaml + "FontFamily").Single(element => (string?)element.Attribute(X + "Key") == $"Bleecker{Pascal(token.Key)}FontFamily").Value;
            Assert.AreEqual($"ms-appx:///Orchard/Assets/Fonts/{token.Value.Replace(" ", "")}-Regular.ttf#{token.Value}", uri);
        }
    }

    [TestMethod]
    public void HighContrastProvidesEveryResourceUsingWindowsAccessibilityColors()
    {
        var xaml = XDocument.Load(Fixture("Tokens.g.xaml"));
        var themes = xaml.Descendants(Xaml + "ResourceDictionary").Where(element => element.Attribute(X + "Key") is not null).ToDictionary(element => (string)element.Attribute(X + "Key")!);
        var expected = themes["Light"].Elements().Select(element => (string)element.Attribute(X + "Key")!).Order().ToArray();
        foreach (var theme in themes.Values) CollectionAssert.AreEqual(expected, theme.Elements().Select(element => (string)element.Attribute(X + "Key")!).Order().ToArray());
        foreach (var brush in themes["HighContrast"].Elements(Xaml + "SolidColorBrush"))
        {
            StringAssert.StartsWith((string)brush.Attribute("Color")!, "{ThemeResource SystemColor");
            Assert.AreEqual("1", (string?)brush.Attribute("Opacity") ?? "1");
        }
        foreach (var prefix in new[] { "Button", "AccentButton" })
        {
            var brushes = themes["HighContrast"].Elements(Xaml + "SolidColorBrush").ToDictionary(element => (string)element.Attribute(X + "Key")!);
            Assert.AreNotEqual((string?)brushes[$"{prefix}BackgroundDisabled"].Attribute("Color"),
                (string?)brushes[$"{prefix}ForegroundDisabled"].Attribute("Color"), "Disabled text must remain distinct from its surface.");
        }
    }

    [TestMethod]
    public void ConsumerStylesResolveTheirBleeckerResources()
    {
        var tokens = XDocument.Load(Fixture("Tokens.g.xaml"));
        var keys = tokens.Descendants().Select(element => (string?)element.Attribute(X + "Key")).Where(key => key is not null).ToHashSet();
        var styles = File.ReadAllText(Fixture("Orchard.xaml"));
        keys.UnionWith(XDocument.Parse(styles).Descendants().Select(element => (string?)element.Attribute(X + "Key")).Where(key => key is not null));
        foreach (Match reference in Regex.Matches(styles, @"\{(?:StaticResource|ThemeResource) (Bleecker\w+)\}"))
            Assert.IsTrue(keys.Contains(reference.Groups[1].Value), $"Missing resource {reference.Groups[1].Value}");
        var dictionary = XDocument.Parse(styles);
        foreach (var name in new[] { "Heading", "Body", "PrimaryButton", "SecondaryButton", "TextBox", "ComboBox" })
            Assert.IsTrue(dictionary.Descendants(Xaml + "Style").Any(style => (string?)style.Attribute(X + "Key") == $"Bleecker{name}Style"));
    }

    [TestMethod]
    public void ButtonStylesExplicitlyBindColorsAtTheConsumerScope()
    {
        var xaml = XDocument.Load(Fixture("Orchard.xaml"));
        foreach (var (name, prefix) in new[] { ("Primary", "AccentButton"), ("Secondary", "Button") })
        {
            var style = xaml.Descendants(Xaml + "Style").Single(element => (string?)element.Attribute(X + "Key") == $"Bleecker{name}ButtonStyle");
            foreach (var property in new[] { "Background", "Foreground", "BorderBrush" })
            {
                var setter = style.Elements(Xaml + "Setter").Single(element => (string?)element.Attribute("Property") == property);
                Assert.AreEqual($"{{ThemeResource {prefix}{property}}}", (string?)setter.Attribute("Value"),
                    "Inherited WinUI setters resolve in the SDK dictionary scope and may retain the system accent.");
            }
        }
    }

    [TestMethod]
    public void CoreTextAndPrimaryActionsHaveAccessibleContrastInBothThemes()
    {
        foreach (var scheme in Enum.GetValues<BleeckerColorScheme>())
        {
            var colors = BleeckerTokens.Colors(scheme);
            foreach (var (foreground, background) in new[] { ("textPrimary", "background"), ("textSecondary", "background"), ("primaryForeground", "primary") })
            {
                var a = Luminance(colors[foreground]); var b = Luminance(colors[background]);
                Assert.IsTrue((Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05) >= 4.5, $"Insufficient contrast: {scheme} {foreground}/{background}");
            }
        }
    }

    [TestMethod]
    public void UnknownColorSchemeIsRejected() => Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => BleeckerTokens.Colors((BleeckerColorScheme)99));

    private static string Pascal(string text) => char.ToUpperInvariant(text[0]) + text[1..];
    private static string Argb(string value)
    {
        if (value.StartsWith('#')) return "#FF" + value[1..].ToUpperInvariant();
        var match = Regex.Match(value, @"^rgba\((\d+), (\d+), (\d+), ([\d.]+)\)$");
        Assert.IsTrue(match.Success);
        var alpha = (int)Math.Round(double.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) * 255, MidpointRounding.AwayFromZero);
        return $"#{alpha:X2}{int.Parse(match.Groups[1].Value):X2}{int.Parse(match.Groups[2].Value):X2}{int.Parse(match.Groups[3].Value):X2}";
    }
    private static double Luminance(string value)
    {
        var channels = new[] { 3, 5, 7 }.Select(offset => int.Parse(value.Substring(offset, 2), NumberStyles.HexNumber) / 255.0)
            .Select(channel => channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4)).ToArray();
        return channels[0] * 0.2126 + channels[1] * 0.7152 + channels[2] * 0.0722;
    }
}
