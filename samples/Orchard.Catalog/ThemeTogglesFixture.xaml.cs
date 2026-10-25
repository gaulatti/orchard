using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class ThemeTogglesFixture : UserControl
{
    public ThemeTogglesFixture()
    {
        InitializeComponent();
        var callerStyle = new Style(typeof(BleeckerThemeToggle)) { BasedOn = StyledMetadata.Style };
        callerStyle.Setters.Add(new Setter(AutomationProperties.NameProperty, "Styled appearance — 東京"));
        callerStyle.Setters.Add(new Setter(ToolTipService.ToolTipProperty, "Caller style tooltip"));
        StyledMetadata.Style = callerStyle;
    }
    private void AcceptTheme(object sender, BleeckerThemeRequestedEventArgs args)
    {
        Specimen.Mode = args.Mode;
        Preview.RequestedTheme = args.Mode switch { BleeckerThemeMode.Light => ElementTheme.Light, BleeckerThemeMode.Dark => ElementTheme.Dark, _ => ElementTheme.Default };
        Status.Text = $"Consumer accepted {args.Mode} theme";
    }
}
