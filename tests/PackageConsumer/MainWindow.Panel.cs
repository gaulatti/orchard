using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private async Task VerifyPackagePanelAsync()
    {
        Require(ReferenceEquals(PackagePanel.Content,PackagePanelBody)&&ReferenceEquals(PackagePanel.Filter,PackagePanelFilter)&&PackagePanel.Count==0&&
            PackagePanelInput.Text=="Independent Panel"&&!PackagePanel.IsTabStop,"Packaged Panel lost its content, count or passive behavior.");
        var producer=new TextBlock{Tag=12};PackagePanel.SetBinding(BleeckerPanel.CountProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});
        var accent=new SolidColorBrush(Windows.UI.Color.FromArgb(255,64,127,163));PackagePanel.Accent=accent;PackagePanel.Title="Updated package panel";producer.Tag=7;Root.UpdateLayout();
        Require(PackagePanel.Count==7&&PackagePanel.GetBindingExpression(BleeckerPanel.CountProperty)is not null&&ReferenceEquals(PackagePanel.Accent,accent),"Packaged Panel replaced its count binding or accent producer.");
        var actions=0;RoutedEventHandler requested=(_,_)=>actions++;PackagePanelAction.Click+=requested;
        var button=BrandDescendants<Button>(PackagePanelAction).Single();((IInvokeProvider)FrameworkElementAutomationPeer.CreatePeerForElement(button).GetPattern(PatternInterface.Invoke)).Invoke();
        await AwaitAsync(()=>actions==1,"Packaged Panel did not deliver its consumer action.");PackagePanelAction.Click-=requested;
        var template=PackagePanel.Template;PackagePanel.Template=null;Root.UpdateLayout();PackagePanel.Template=template;Root.UpdateLayout();
        Require(ReferenceEquals(PackagePanel.Content,PackagePanelBody)&&PackagePanelInput.Text=="Independent Panel"&&ReferenceEquals(PackagePanel.Toolbar,PackagePanelAction),"Packaged Panel template replacement lost consumer state.");
        var modelTemplate=(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><TextBlock Text='{Binding}'/></DataTemplate>");
        PackagePanel.FilterTemplate=modelTemplate;PackagePanel.Filter=42;Root.UpdateLayout();
        Require(BrandDescendants<ContentPresenter>(PackagePanel).Any(node=>node.Content is int number&&number==42),"Packaged Panel converted a consumer template model.");
        PackagePanel.FilterTemplate=null;PackagePanel.Filter=PackagePanelFilter;PackagePanel.ClearValue(BleeckerPanel.CountProperty);Root.UpdateLayout();
        Require(BrandDescendants<Border>(PackagePanel).Single(node=>node.Name=="PanelCount").Visibility==Visibility.Collapsed,"Packaged Panel kept an absent count badge.");
        PackagePanel.Count=0;PackagePanel.Title="Package panel";Root.UpdateLayout();
    }
}
