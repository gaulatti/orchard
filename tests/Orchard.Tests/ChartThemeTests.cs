using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Orchard.Tests;

[TestClass]
public sealed class ChartThemeTests
{
    [TestMethod]
    public void PublicMetadataCannotBeMutatedByAChartConsumer()
    {
        Assert.IsTrue(typeof(BleeckerChartTheme).IsPublic);
        Assert.IsTrue(typeof(BleeckerChartGridStyle).IsPublic);
        Assert.IsTrue(typeof(BleeckerChartAxisStyle).IsPublic);
        Assert.IsTrue(typeof(BleeckerChartTooltipStyle).IsPublic);
        var colors=(IDictionary<string,string>)BleeckerChartTheme.Colors;
        var palette=(IList<string>)BleeckerChartTheme.Palette;
        var dashes=(IList<double>)BleeckerChartTheme.Grid.StrokeDashes;
        Assert.ThrowsExactly<NotSupportedException>(()=>colors["sea"]="ConsumerReplacement");
        Assert.ThrowsExactly<NotSupportedException>(()=>palette[0]="ConsumerReplacement");
        Assert.ThrowsExactly<NotSupportedException>(()=>dashes[0]=99);
        Assert.AreEqual("BleeckerSeaBrush",BleeckerChartTheme.PickColor(0));
        CollectionAssert.AreEqual(new[]{3d,3d},BleeckerChartTheme.Grid.StrokeDashes.ToArray());
    }
    [TestMethod]
    public void FrozenPaletteAndRemainderCasesPreserveProducerReferences()
    {
        using var source=Reference();
        var theme=source.RootElement.GetProperty("theme");
        CollectionAssert.AreEqual(theme.GetProperty("palette").EnumerateArray().Select(value=>ResourceKey(value.GetString()!)).ToArray(),BleeckerChartTheme.Palette.ToArray());
        Assert.AreEqual(theme.GetProperty("colors").EnumerateObject().Count(),BleeckerChartTheme.Colors.Count);
        foreach(var role in theme.GetProperty("colors").EnumerateObject())
            Assert.AreEqual(ResourceKey(role.Value.GetString()!),BleeckerChartTheme.Colors[role.Name],role.Name);
        foreach(var sample in source.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input=sample.GetProperty("index");
            var index=input.ValueKind==JsonValueKind.Number?input.GetDouble():input.GetString() switch
            {"NaN"=>double.NaN,"Infinity"=>double.PositiveInfinity,"-Infinity"=>double.NegativeInfinity,"-0"=>-0d,_=>throw new InvalidOperationException("Unknown source number.")};
            var expected=sample.GetProperty("value");
            Assert.AreEqual(expected.ValueKind==JsonValueKind.Null?null:ResourceKey(expected.GetString()!),BleeckerChartTheme.PickColor(index),input.ToString());
        }
    }

    [TestMethod]
    public void FrozenGridAxisAndTooltipValuesRemainChartSpecific()
    {
        using var source=Reference();var theme=source.RootElement.GetProperty("theme");
        var grid=theme.GetProperty("grid");var axis=theme.GetProperty("axis");var tooltip=theme.GetProperty("tooltip");var content=tooltip.GetProperty("contentStyle");
        Assert.AreEqual(ResourceKey(grid.GetProperty("stroke").GetString()!),BleeckerChartTheme.Grid.StrokeResourceKey);
        CollectionAssert.AreEqual(Numbers(grid.GetProperty("strokeDasharray").GetString()!),BleeckerChartTheme.Grid.StrokeDashes.ToArray());
        Assert.AreEqual(ResourceKey(axis.GetProperty("stroke").GetString()!),BleeckerChartTheme.Axis.StrokeResourceKey);
        Assert.AreEqual(ResourceKey(axis.GetProperty("tick").GetProperty("fill").GetString()!),BleeckerChartTheme.Axis.TickForegroundResourceKey);
        Assert.AreEqual(axis.GetProperty("tick").GetProperty("fontSize").GetDouble(),BleeckerChartTheme.Axis.TickFontSize);
        var actual=BleeckerChartTheme.Tooltip;
        Assert.AreEqual(ResourceKey(content.GetProperty("backgroundColor").GetString()!),actual.BackgroundResourceKey);
        Assert.AreEqual(Numbers(content.GetProperty("borderRadius").GetString()!)[0],actual.CornerRadius);
        var border=content.GetProperty("border").GetString()!;
        Assert.AreEqual(Numbers(border)[0],actual.BorderThickness);
        Assert.AreEqual(ResourceKey(Regex.Match(border,@"var\([^)]+\)").Value),actual.BorderResourceKey);
        CollectionAssert.AreEqual(Numbers(content.GetProperty("boxShadow").GetString()!),new[]{actual.ShadowX,actual.ShadowY,actual.ShadowBlurCssPixels,
            (double)actual.ShadowRed,actual.ShadowGreen,actual.ShadowBlue,actual.ShadowOpacity});
        Assert.AreEqual(Numbers(content.GetProperty("backdropFilter").GetString()!)[0],actual.BackdropBlurCssPixels);
        Assert.AreEqual(ResourceKey(tooltip.GetProperty("itemStyle").GetProperty("color").GetString()!),actual.ItemForegroundResourceKey);
        Assert.AreEqual(ResourceKey(tooltip.GetProperty("labelStyle").GetProperty("color").GetString()!),actual.LabelForegroundResourceKey);
        Assert.AreEqual(tooltip.GetProperty("labelStyle").GetProperty("fontWeight").GetInt32(),actual.LabelFontWeight);
    }

    [TestMethod]
    public void EveryChartResourceReferenceExistsInEachNativeTheme()
    {
        XNamespace x="http://schemas.microsoft.com/winfx/2006/xaml";XNamespace presentation="http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var tokens=XDocument.Load(Path.Combine(AppContext.BaseDirectory,"Fixtures","Tokens.g.xaml"));
        var dictionaries=tokens.Descendants(presentation+"ResourceDictionary").Where(element=>element.Attribute(x+"Key") is not null).ToArray();
        CollectionAssert.AreEquivalent(new[]{"Light","Default","HighContrast"},dictionaries.Select(element=>element.Attribute(x+"Key")!.Value).ToArray());
        foreach(var dictionary in dictionaries)
        {
            var keys=dictionary.Elements().Attributes(x+"Key").Select(value=>value.Value).ToHashSet(StringComparer.Ordinal);
            foreach(var key in BleeckerChartTheme.Colors.Values.Concat(BleeckerChartTheme.Palette).Distinct())
                Assert.IsTrue(keys.Contains(key),$"{dictionary.Attribute(x+"Key")!.Value} has no chart resource {key}.");
        }
    }
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","ChartTheme.json")));
    private static string ResourceKey(string css)=>"Bleecker"+string.Concat(Regex.Match(css,@"var\(--color-([a-z-]+)\)").Groups[1].Value.Split('-').Select(part=>char.ToUpperInvariant(part[0])+part[1..]))+"Brush";
    private static double[] Numbers(string css)=>Regex.Matches(css,@"\d+(?:\.\d+)?").Select(match=>double.Parse(match.Value,CultureInfo.InvariantCulture)).ToArray();
}
