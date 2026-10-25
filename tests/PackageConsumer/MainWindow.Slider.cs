using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Orchard;
namespace PackageConsumer;
public sealed partial class MainWindow
{
    private async Task VerifyPackageSliderAsync()
    {
        Root.UpdateLayout();var originalName=AutomationProperties.GetName(PackageSlider);var peer=FrameworkElementAutomationPeer.CreatePeerForElement(PackageSlider);var provider=(IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);
        Require(PackageSlider.Value==25.3&&provider.Value==30&&provider.Minimum==0&&provider.Maximum==100&&provider.SmallChange==10&&peer.GetAutomationControlType()==AutomationControlType.Slider,"Packaged Slider lost its authoritative/sanitized value or native range metadata.");
        var producer=new TextBlock{Tag=25.3};PackageSlider.SetBinding(BleeckerSlider.ValueProperty,new Binding{Source=producer,Path=new PropertyPath("Tag"),Mode=BindingMode.OneWay});var requests=new List<double>();EventHandler<BleeckerSliderValueRequestedEventArgs> requested=(_,args)=>requests.Add(args.Value);PackageSlider.ValueRequested+=requested;
        provider.SetValue(60);Require(requests.SequenceEqual(new[]{60d})&&PackageSlider.Value==25.3&&provider.Value==30,"Packaged Slider mutated the consumer's controlled binding.");producer.Tag=60d;Root.UpdateLayout();Require(provider.Value==60&&PackageSlider.GetBindingExpression(BleeckerSlider.ValueProperty)is not null,"Packaged Slider did not retain live binding updates.");
        PackageSlider.IsEnabled=false;var disabledRejected=false;try{provider.SetValue(70);}catch(ElementNotEnabledException){disabledRejected=true;}Require(disabledRejected&&requests.Count==1,"Packaged disabled Slider accepted a request.");PackageSlider.IsEnabled=true;
        var originalStyle=PackageSlider.Style;var namingBase=new Style(typeof(BleeckerSlider)){BasedOn=originalStyle};namingBase.Setters.Add(new Setter(AutomationProperties.NameProperty,""));PackageSlider.ClearValue(AutomationProperties.NameProperty);PackageSlider.Style=new Style(typeof(BleeckerSlider)){BasedOn=namingBase};Root.UpdateLayout();Require(peer.GetName()=="","Packaged Slider replaced an empty styled name.");PackageSlider.Style=originalStyle;Root.UpdateLayout();
        var template=PackageSlider.Template;PackageSlider.Template=null;Root.UpdateLayout();PackageSlider.Template=template;Root.UpdateLayout();producer.Tag=80d;Root.UpdateLayout();await AwaitAsync(()=>((IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(PackageSlider).GetPattern(PatternInterface.RangeValue)).Value==80,"Packaged Slider template replacement lost its live value.");
        PackageSlider.ValueRequested-=requested;PackageSlider.ClearValue(BleeckerSlider.ValueProperty);PackageSlider.Value=25.3;AutomationProperties.SetName(PackageSlider,originalName);Root.UpdateLayout();
    }
}
