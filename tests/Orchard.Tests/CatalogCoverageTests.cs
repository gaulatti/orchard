using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Orchard.Tests;

[TestClass]
public sealed class CatalogCoverageTests
{
    [TestMethod]
    public void EveryDeclaredFixtureIsReachableThroughCatalogNavigation()
    {
        var (_, catalog) = FixturePaths();
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var window = XDocument.Load(Path.Combine(catalog, "MainWindow.xaml"));
        var host = window.Descendants().Single(element => (string?)element.Attribute(x + "Name") == "FixtureHost");
        var declared = host.Elements().Select(element => (string?)element.Attribute(x + "Name") ?? throw new AssertFailedException("A catalog fixture has no navigation identity.")).ToArray();
        var source = File.ReadAllText(Path.Combine(catalog, "MainWindow.Navigation.cs"));
        var list = Regex.Match(source, @"components\.AddRange\(\s*\[(.*?)\]\s*\)", RegexOptions.Singleline);
        Assert.IsTrue(list.Success, "The catalog navigation registry was not found.");
        var registered = Regex.Matches(list.Groups[1].Value, "\\(\\s*\"[^\"]+\"\\s*,\\s*(\\w+)\\s*\\)").Select(match => match.Groups[1].Value).ToArray();
        CollectionAssert.AreEquivalent(declared, registered, "Every declared fixture, including Header and both fictional demos, must be reachable exactly once.");
    }

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
                (attribute.Name.LocalName is "Style" or "MenuFlyoutPresenterStyle" or "SurfaceStyle" or "TriggerStyle" or "ItemStyle" or "SubmenuSurfaceStyle" or "TopLinkStyle" or "GroupLinkStyle" or "DismissButtonStyle" or "LinkActionStyle" or "ButtonActionStyle" or "LinkStyle" or "ArrowStyle" or "IndicatorStyle" or "TagStyle" or "CtaStyle") &&
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
        var contexts = coverage.RootElement.GetProperty("contexts");
        var models = coverage.RootElement.GetProperty("models");
        // All public library classes except the token container require a fixture.
        // This deliberately includes view models/helpers until explicitly classified,
        // so an unfamiliar base class cannot silently evade visual coverage.
        var classes = Directory.GetFiles(library, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => Regex.Matches(File.ReadAllText(path), @"public\s+(?:(?:sealed|abstract|partial|static)\s+)*class\s+(\w+)"))
            .Select(match => match.Groups[1].Value).Where(name => name != "BleeckerTokens").Distinct().ToArray();
        CollectionAssert.AreEquivalent(classes, registrations.EnumerateObject().Concat(utilities.EnumerateObject()).Concat(contexts.EnumerateObject()).Concat(models.EnumerateObject()).Select(item => item.Name).ToArray());
        CollectionAssert.AreEquivalent(new[] { "BleeckerSonnerOptions", "BleeckerSonnerAction", "BleeckerSonnerToast", "BleeckerHeroCarouselItem", "BleeckerMediaLibraryItem", "BleeckerCommandSpotlightAction" }, models.EnumerateObject().Select(item => item.Name).ToArray());
        foreach (var model in models.EnumerateObject())
        {
            var file = Path.Combine(catalog, model.Value.GetString()!); var fixture = XDocument.Load(file);
            Assert.IsTrue(fixture.Descendants().Any(element => element.Name.LocalName == (model.Name == "BleeckerCommandSpotlightAction" ? "BleeckerCommandSpotlight" : model.Name == "BleeckerMediaLibraryItem" ? "BleeckerMediaLibrary" : model.Name == "BleeckerHeroCarouselItem" ? "BleeckerHeroCarousel" : "BleeckerSonner")));
            StringAssert.Contains(File.ReadAllText(file + ".cs"), "new " + model.Name + "(");
        }
        // Framework-created Data Table contexts are consumed by actual native
        // content callbacks; they cannot be independently instantiated in XAML.
        CollectionAssert.AreEquivalent(new[]{"BleeckerDataTableColumn","BleeckerDataTableColumnContext","BleeckerDataTableRow","BleeckerDataTableCell","BleeckerDataTableHeader","BleeckerDataTableModel"},contexts.EnumerateObject().Select(item=>item.Name).ToArray());
        foreach(var context in contexts.EnumerateObject())
        {
            var file=Path.Combine(catalog,context.Value.GetString()!);var fixture=XDocument.Load(file);var source=File.ReadAllText(file+".cs");
            Assert.IsTrue(fixture.Descendants().Any(element=>element.Name.LocalName=="BleeckerDataTable"));
            StringAssert.Contains(source,context.Name);
            foreach(var member in new[]{"headerContentSelector:HeaderSelection","headerContentSelector:SortHeader","cellContentSelector:CellSelection","cellContentSelector:OriginalAction","Data.Model.Changed+=ModelChanged","cell.Row.Original"})StringAssert.Contains(source,member);
        }
        // Static configuration and attached-layout utilities are not controls.
        // Dedicated fixtures consume their public entry points and the native
        // catalog gate exercises the rendered consumers.
        CollectionAssert.AreEquivalent(new[]{"BleeckerChartTheme","BleeckerTextLayout","BleeckerToast"},utilities.EnumerateObject().Select(item=>item.Name).ToArray());
        foreach(var utility in utilities.EnumerateObject())
        {
            var source=File.ReadAllText(Path.Combine(catalog,utility.Value.GetString()!+".cs"));
            var members = utility.Name == "BleeckerChartTheme" ? new[]{"Colors","Palette","PickColor","Grid","Axis","Tooltip"} : utility.Name == "BleeckerToast" ? new[]{"Show","Success","Error","Warning","Info","Dismiss"} : new[]{"GetUseReferenceLineBox","SetUseReferenceLineBox","UseReferenceLineBoxProperty"};
            foreach(var member in members)
                StringAssert.Contains(source,utility.Name+"."+member);
            var fixture=XDocument.Load(Path.Combine(catalog,utility.Value.GetString()!));
            if(utility.Name == "BleeckerChartTheme") Assert.IsTrue(fixture.Descendants().Any(element=>element.Name.LocalName=="Grid"),"Chart theme fixture has no rendered consumer host.");
            else if (utility.Name == "BleeckerToast") Assert.IsTrue(fixture.Descendants().Any(element => element.Name.LocalName == "BleeckerSonner"), "Toast API fixture has no mounted Sonner host.");
            else Assert.IsTrue(fixture.Descendants().Attributes().Any(attribute=>attribute.Name.LocalName=="BleeckerTextLayout.UseReferenceLineBox"&&attribute.Value=="True"),"Text layout fixture does not render the attached behavior.");
        }
        foreach (var registration in registrations.EnumerateObject())
        {
            var fixture = XDocument.Load(Path.Combine(catalog, registration.Value.GetString()!));
            // The brand renderer receives its framework-created context in a
            // typed template. Its internal constructor is deliberately not a
            // second consumer factory. Require all four source-contract bindings
            // in that rendered fixture instead of inventing a XAML instance.
            if (registration.Name == "BleeckerEdgeDialog")
            {
                // The abstract shared contract is exercised through both public
                // concrete controls and the caller's common close handler.
                foreach (var derived in new[] { "BleeckerDrawer", "BleeckerSheet" })
                    Assert.IsTrue(fixture.Descendants().Any(element => element.Name.LocalName == derived));
                StringAssert.Contains(File.ReadAllText(Path.Combine(catalog, registration.Value.GetString()! + ".cs")), "((BleeckerEdgeDialog)sender).IsOpen=false");
                continue;
            }
            if (registration.Name is "BleeckerBrandLinkContext" or "BleeckerSidebarLinkContext" or "BleeckerNavLinkContext" or "BleeckerNavigationLinkContext" or "BleeckerHeroCarouselLinkContext")
            {
                XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
                var template = fixture.Descendants().Single(element => element.Name.LocalName == "DataTemplate" &&
                    ((string?)element.Attribute(x + "DataType"))?.EndsWith(":" + registration.Name, StringComparison.Ordinal) == true);
                foreach (var field in registration.Name == "BleeckerHeroCarouselLinkContext" ? new[] { "Content", "Command", "Style", "Item", "AccessibleName" } : registration.Name == "BleeckerNavigationLinkContext" ? new[] { "Content", "Command", "Style", "Item", "Foreground", "IsGrouped", "CloseCommand" } : registration.Name == "BleeckerNavLinkContext" ? new[] { "Href", "Label", "Content", "Command", "Style", "Item", "Foreground" } : registration.Name == "BleeckerSidebarLinkContext" ? new[] { "Href", "Label", "Content", "Command", "Style", "Item" } : new[] { "Href", "Label", "Content", "Command" })
                    Assert.IsTrue(template.Descendants().Attributes().Any(attribute => attribute.Value == "{x:Bind " + field + "}" ||
                        attribute.Value.StartsWith("{x:Bind " + field + ",", StringComparison.Ordinal)),
                        $"Link renderer fixture does not bind context {field}.");
                if (registration.Name == "BleeckerHeroCarouselLinkContext")
                    Assert.IsFalse(template.Descendants().Attributes().Any(attribute =>
                        attribute.Name.LocalName is "IsEnabled" or "IsHitTestVisible" or "IsTabStop" && attribute.Value.Contains("IsActive", StringComparison.Ordinal)),
                        "Hero renderer must retain the source's inactive-slide input.");
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
