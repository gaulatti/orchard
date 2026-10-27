using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Orchard;
using Windows.Graphics;

namespace PackageConsumer;

/// <summary>Visible, caller-owned Hero Carousel input in a restored NuGet consumer.</summary>
public sealed class HeroCarouselForegroundWindow : Window
{
    public HeroCarouselForegroundWindow(ElementTheme theme)
    {
        if (theme is not (ElementTheme.Light or ElementTheme.Dark))
            throw new ArgumentOutOfRangeException(nameof(theme));

        Title = "Orchard package Hero Carousel foreground";
        var root = (Grid)XamlReader.Load("<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' Background='{ThemeResource BleeckerBackgroundBrush}' />");
        root.RequestedTheme = theme;
        var stack = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        stack.Children.Add(new TextBlock { Text = $"Independent package Hero Carousel · {theme}", FontSize = 24 });
        var outside = new Button { Content = "Outside carousel focus" };
        AutomationProperties.SetName(outside, "Outside carousel focus");
        stack.Children.Add(outside);
        var editor = new TextBox { Header = "Native input excludes global arrows", Text = "Original local edit" };
        AutomationProperties.SetName(editor, "Native input excludes global arrows");
        stack.Children.Add(editor);
        var index = new TextBlock { Name = "HeroIndex", Text = "Current index: 0" };
        var result = new TextBlock { Name = "HeroResult", Text = "No original item invoked." };
        stack.Children.Add(index);
        stack.Children.Add(result);

        BleeckerHeroCarouselItem[] original =
        [
            new("/fictional/coast", "Fictional coastal project", "Caller-owned first item") { Id = "first" },
            new("/fictional/article#original", "Fictional article", "Caller-owned second item") { Id = "second", External = true },
            new("/fictional/mountain", "Fictional mountain project", "Caller-owned third item") { Id = "third" }
        ];
        var hero = new BleeckerHeroCarousel
        {
            Name = "PackageHero",
            Items = new BleeckerHeroCarouselItems(original),
            AutoAdvanceMilliseconds = 1_000_000,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        hero.CurrentIndexChanged += (_, _) => index.Text = $"Current index: {hero.CurrentIndex}";
        hero.ItemInvoked += (_, item) => result.Text = $"Original item retained: {original.Contains(item)}; destination {item.Href}; external {item.External}";
        stack.Children.Add(hero);
        root.Children.Add(stack);
        Content = root;
        AppWindow.Resize(new SizeInt32(1100, 920));
    }
}
