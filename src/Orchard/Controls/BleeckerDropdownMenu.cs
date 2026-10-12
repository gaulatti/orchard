using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard;

/// <summary>A native menu with distinct Bleecker root and submenu geometry.</summary>
public sealed class BleeckerDropdownMenu : MenuFlyout
{
    private readonly Style defaultPresenterStyle;
    private readonly Style rootPresenterStyle;
    private Control? rootPresenter;

    public BleeckerDropdownMenu()
    {
        var resources = new ResourceDictionary
        {
            Source = new Uri("ms-appx:///Orchard/Themes/Orchard.xaml")
        };
        defaultPresenterStyle = new Style(typeof(MenuFlyoutPresenter))
        {
            BasedOn = (Style)resources["BleeckerMenuPresenterStyle"]
        };
        defaultPresenterStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 144d));
        rootPresenterStyle = new Style(typeof(MenuFlyoutPresenter)) { BasedOn = defaultPresenterStyle };
        rootPresenterStyle.Setters.Add(new Setter(FrameworkElement.MinWidthProperty, 176d));
        MenuFlyoutPresenterStyle = defaultPresenterStyle;
        AreOpenCloseAnimationsEnabled = false;
        Opening += (_, _) => ApplyRootStyle();
    }

    protected override Control CreatePresenter()
    {
        rootPresenter = base.CreatePresenter();
        // WinUI forwards the shared style to nested presenters. Only the root is
        // created through this override; consumer styles retain their own geometry.
        ApplyRootStyle();
        return rootPresenter;
    }

    private void ApplyRootStyle()
    {
        if (rootPresenter is not null)
            rootPresenter.Style = ReferenceEquals(MenuFlyoutPresenterStyle, defaultPresenterStyle)
                ? rootPresenterStyle : MenuFlyoutPresenterStyle;
    }
}
