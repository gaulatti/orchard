using System.Collections.ObjectModel;
using System.Text.Json;
using Orchard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;

namespace PackageConsumer;

internal sealed class FooterReferenceItem(string href, string label, bool external, string marker) : BleeckerNavItem(href, label, external)
{
    internal string Marker { get; } = marker;
}

internal static class FooterReferenceFactory
{
    private static object? Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(), JsonValueKind.True => true, JsonValueKind.False => false,
        JsonValueKind.Number => value.GetDouble(), JsonValueKind.Null => null,
        _ => throw new InvalidOperationException("Unsupported source Footer content.")
    };
    internal static BleeckerFooter Create(JsonElement spec)
    {
        var brand = spec.GetProperty("brand");
        var control = new BleeckerFooter
        {
            Width = spec.TryGetProperty("width", out var width) ? width.GetDouble() : spec.GetProperty("viewportWidth").GetDouble(),
            Brand = new() { Href = brand.GetProperty("href").GetString()!, BrandName = brand.TryGetProperty("name", out var name) ? name.GetString() : null, LogoAlt = brand.TryGetProperty("logoAlt", out var alt) ? alt.GetString()! : "gaulatti", Description = brand.TryGetProperty("description", out var description) ? description.GetString() : null },
            FlowDirection = spec.TryGetProperty("rtl", out var rtl) && rtl.GetBoolean() ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Sections = new ObservableCollection<BleeckerFooterSection>(spec.GetProperty("sections").EnumerateArray().Select(section => new BleeckerFooterSection
            {
                Title = section.GetProperty("title").GetString()!,
                Items = new ObservableCollection<BleeckerNavItem>(section.GetProperty("items").EnumerateArray().Select(item => new FooterReferenceItem(item.GetProperty("href").GetString()!, item.GetProperty("label").GetString()!, item.TryGetProperty("external", out var external) && external.GetBoolean(), item.GetProperty("marker").GetString()!)))
            }))
        };
        if (spec.TryGetProperty("bottomLeft", out var left)) control.BottomLeft = Scalar(left);
        if (spec.TryGetProperty("bottomRight", out var right)) control.BottomRight = Scalar(right);
        if (spec.TryGetProperty("showBottomAccent", out var accent)) control.ShowBottomAccent = accent.GetBoolean();
        if (spec.TryGetProperty("custom", out var custom) && custom.GetBoolean()) control.LinkTemplate = (DataTemplate)XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><HyperlinkButton x:Name='CustomFooterLink' Content='{Binding Content}' Style='{Binding Style}' Command='{Binding Command}' Foreground='{Binding Foreground}'/></DataTemplate>");
        return control;
    }
    internal static void Mutate(BleeckerFooter footer, JsonElement mutation)
    {
        switch (mutation.GetProperty("kind").GetString())
        {
            case "reverse-sections":
                var sections = (ObservableCollection<BleeckerFooterSection>)footer.Sections; var ordered = sections.Reverse().ToArray();
                for (var index = 0; index < ordered.Length; index++) { var current = sections.IndexOf(ordered[index]); if (current != index) sections.Move(current, index); }
                break;
            case "description": footer.Brand.Description = mutation.GetProperty("value").GetString(); break;
            case "item-label": footer.Sections.ElementAt(mutation.GetProperty("section").GetInt32()).Items.ElementAt(mutation.GetProperty("index").GetInt32()).Label = mutation.GetProperty("value").GetString()!; break;
            case "section-title": footer.Sections.ElementAt(mutation.GetProperty("section").GetInt32()).Title = mutation.GetProperty("value").GetString()!; break;
            default: throw new InvalidOperationException("Unknown source Footer mutation.");
        }
    }
}
