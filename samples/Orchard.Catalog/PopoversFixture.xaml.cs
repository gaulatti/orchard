using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class PopoversFixture : StackPanel
{
    public PopoversFixture() { InitializeComponent(); Unloaded += (_, _) => PopupConfirmation.IsOpen = false; }
    private void Requested(object? sender, bool open) => Status.Text = "Requested " + (open ? "open" : "closed");
    private void ControlledRequested(object? sender, bool open) { Controlled.Open = open; Status.Text = "Host accepted " + (open ? "open" : "closed"); }
    private void CloseDefault(object sender, RoutedEventArgs args) => Default.Close();
    private void CloseControlled(object sender, RoutedEventArgs args) => Controlled.Close();
    private void OpenConfirmation(object sender, RoutedEventArgs args) => PopupConfirmation.IsOpen = true;
    private void CloseConfirmation(object sender, RoutedEventArgs args) => PopupConfirmation.IsOpen = false;
    private void NestedRequested(object? sender, bool open) => Status.Text =
        $"{(ReferenceEquals(sender, NestedChild) ? "Child" : "Parent")} requested {(open ? "open" : "closed")}";
}
