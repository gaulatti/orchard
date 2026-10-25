using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog;
public sealed partial class FeedItemsFixture:UserControl
{
    public FeedItemsFixture(){InitializeComponent();First.PostedAt=DateTimeOffset.Now.AddMinutes(-10);First.Categories=new[]{"  LOCAL ","Review"};Second.PostedAt=DateTimeOffset.Now.AddDays(-2);Second.Categories=new[]{"İSTANBUL","ΟΣ","CAFÉ"};}
    private void SourceRequested(object? sender,BleeckerFeedItemNavigationEventArgs args)=>Status.Text="Consumer requested: "+args.SourceUrl;
}
