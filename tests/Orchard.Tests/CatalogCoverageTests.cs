using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Orchard.Tests;

[TestClass]
public sealed class CatalogCoverageTests
{
    [TestMethod]
    public void CatalogBleeckerResourceReferencesHaveDeclaredKeys()
    {
        var (library, catalog) = FixturePaths();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var files = Directory.GetFiles(library, "*.xaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(catalog, "*.xaml", SearchOption.AllDirectories)).ToArray();
        var declared = files.SelectMany(path => XDocument.Load(path).Descendants().Attributes(x + "Key"))
            .Select(attribute => attribute.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(catalog, "*.xaml", SearchOption.AllDirectories))
            foreach (Match reference in Regex.Matches(File.ReadAllText(path), @"\{(?:StaticResource|ThemeResource)\s+(Bleecker\w+)\s*\}"))
                Assert.IsTrue(declared.Contains(reference.Groups[1].Value),
                    $"{Path.GetFileName(path)} uses undeclared resource {reference.Groups[1].Value}; the published catalog cannot start.");
    }

    [TestMethod]
    public void EveryPublicStyleHasARegisteredRenderedFixture()
    {
        var (library, catalog) = FixturePaths();
        using var coverage = JsonDocument.Parse(File.ReadAllText(Path.Combine(catalog, "catalog-coverage.json")));
        var registrations = coverage.RootElement.GetProperty("styles");
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        XNamespace presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        var styles = Directory.GetFiles(library, "*.xaml", SearchOption.AllDirectories)
            .SelectMany(path => XDocument.Load(path).Descendants(presentation + "Style"))
            .Where(style => style.Attribute(x + "Key") is not null)
            .Select(style => (string)style.Attribute(x + "Key")!).ToHashSet(StringComparer.Ordinal);
        CollectionAssert.AreEquivalent(styles.ToArray(), registrations.EnumerateObject().Select(item => item.Name).ToArray());
        foreach (var registration in registrations.EnumerateObject())
        {
            var fixture = XDocument.Load(Path.Combine(catalog, registration.Value.GetString()!));
            Assert.IsTrue(fixture.Descendants().Attributes().Any(attribute =>
                (attribute.Name.LocalName == "Style" || attribute.Name.LocalName == "MenuFlyoutPresenterStyle") &&
                (attribute.Value == $"{{StaticResource {registration.Name}}}" || attribute.Value == $"{{ThemeResource {registration.Name}}}")),
                $"Registered fixture does not render {registration.Name}.");
        }
    }

    [TestMethod]
    public void PublicControlDeclarationsCannotBypassCatalogRegistration()
    {
        var (library, catalog) = FixturePaths();
        using var coverage = JsonDocument.Parse(File.ReadAllText(Path.Combine(catalog, "catalog-coverage.json")));
        var registrations = coverage.RootElement.GetProperty("controls");
        var utilities = coverage.RootElement.GetProperty("utilities");
        // All public library classes except the token container require a fixture.
        // This deliberately includes view models/helpers until explicitly classified,
        // so an unfamiliar base class cannot silently evade visual coverage.
        var classes = Directory.GetFiles(library, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"public\s+(?:(?:sealed|abstract|partial|static)\s+)*class\s+(\w+)"))
            .Select(match => match.Groups[1].Value).Where(name => name != "BleeckerTokens").Distinct().ToArray();
        CollectionAssert.AreEquivalent(classes, registrations.EnumerateObject().Concat(utilities.EnumerateObject()).Select(item => item.Name).ToArray());
        // The source chart theme is a static configuration module, not an
        // instantiable control. Its dedicated native fixture must consume every
        // public entry point and is exercised by the catalog runtime gate.
        CollectionAssert.AreEquivalent(new[]{"BleeckerChartTheme"},utilities.EnumerateObject().Select(item=>item.Name).ToArray());
        foreach(var utility in utilities.EnumerateObject())
        {
            var source=File.ReadAllText(Path.Combine(catalog,utility.Value.GetString()!+".cs"));
            foreach(var member in new[]{"Colors","Palette","PickColor","Grid","Axis","Tooltip"})
                StringAssert.Contains(source,utility.Name+"."+member);
            var fixture=XDocument.Load(Path.Combine(catalog,utility.Value.GetString()!));
            Assert.IsTrue(fixture.Descendants().Any(element=>element.Name.LocalName=="Grid"),"Chart theme fixture has no rendered consumer host.");
        }
        foreach (var registration in registrations.EnumerateObject())
        {
            var fixture = XDocument.Load(Path.Combine(catalog, registration.Value.GetString()!));
            // The brand renderer receives its framework-created context in a
            // typed template. Its internal constructor is deliberately not a
            // second consumer factory. Require all four source-contract bindings
            // in that rendered fixture instead of inventing a XAML instance.
            if (registration.Name is "BleeckerBrandLinkContext" or "BleeckerSidebarLinkContext")
            {
                XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
                var template = fixture.Descendants().Single(element => element.Name.LocalName == "DataTemplate" &&
                    ((string?)element.Attribute(x + "DataType"))?.EndsWith(":" + registration.Name, StringComparison.Ordinal) == true);
                foreach (var field in registration.Name == "BleeckerSidebarLinkContext" ? new[] { "Href", "Label", "Content", "Command", "Style", "Item" } : new[] { "Href", "Label", "Content", "Command" })
                    Assert.IsTrue(template.Descendants().Attributes().Any(attribute => attribute.Value == "{x:Bind " + field + "}" ||
                        attribute.Value.StartsWith("{x:Bind " + field + ",", StringComparison.Ordinal)),
                        $"Brand renderer fixture does not bind context {field}.");
                continue;
            }
            Assert.IsTrue(fixture.Descendants().Any(element => element.Name.LocalName == registration.Name ||
                (element.Name.LocalName.StartsWith(registration.Name + ".", StringComparison.Ordinal) && element.HasElements) ||
                element.Attributes().Any(attribute => attribute.Name.LocalName.StartsWith(registration.Name + ".", StringComparison.Ordinal))),
                $"Registered fixture does not instantiate {registration.Name}.");
        }
    }

    private static (string Library, string Catalog) FixturePaths() =>
        (Path.Combine(AppContext.BaseDirectory, "Fixtures", "Library"), Path.Combine(AppContext.BaseDirectory, "Fixtures", "Catalog"));
}
