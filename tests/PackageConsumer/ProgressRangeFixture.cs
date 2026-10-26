using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Orchard.Testing;

internal static class ProgressRangeFixture
{
    private static string Hash(string file)=>Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
    internal static async Task RunAsync(Window window,string output,string hostAssembly)
    {
        var states=new List<object>();var invalid=new List<object>();var contracts=new List<object>();var invalidBindings=new List<object>();var guards=0;var focus=0;var activations=0;var valid=true;
        try
        {
            var root=new Canvas{Width=400,Height=160};window.Content=root;
            root.GotFocus+=(_,_)=>focus++;window.Activated+=(_,e)=>{if(e.WindowActivationState!=WindowActivationState.Deactivated)activations++;};
            void Guard(){guards++;if(window.Visible||window.AppWindow.IsVisible||focus!=0||activations!=0)throw new InvalidOperationException("Progress range probe left its hidden boundary.");}
            async Task Layout(){root.Measure(new Size(400,160));root.Arrange(new Rect(0,0,400,160));root.UpdateLayout();await NativeRenderReadiness.RenderAsync(root);Guard();}
            static T Part<T>(DependencyObject owner,string name)where T:FrameworkElement {if(owner is T found&&found.Name==name)return found;for(var i=0;i<VisualTreeHelper.GetChildrenCount(owner);i++){try{return Part<T>(VisualTreeHelper.GetChild(owner,i),name);}catch(KeyNotFoundException){}}throw new KeyNotFoundException(name);}
            foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
            {
                root.RequestedTheme=theme;
                foreach(var (id,value,max) in new[]{("below",-25d,100d),("zero",0d,100d),("quarter",25d,100d),("default",65d,100d),("full",100d,100d),("above",150d,100d),("custom-max",145d,200d)})
                {
                    var control=new BleeckerProgress{Width=320,Max=max,Value=value,ShowLabel=true};AutomationProperties.SetName(control,"Upload");root.Children.Clear();root.Children.Add(control);await Layout();
                    var track=Part<Border>(control,"PART_Track");var fill=Part<Border>(control,"PART_Fill");var label=Part<TextBlock>(control,"PART_Label");var peer=FrameworkElementAutomationPeer.CreatePeerForElement(control);var range=(IRangeValueProvider)peer.GetPattern(PatternInterface.RangeValue);var effective=Math.Clamp(value,0,max);var expected=effective/max;
                    var matches=range.Value==effective&&range.Minimum==0&&range.Maximum==max&&range.IsReadOnly&&control.Value==value&&control.Max==max&&Math.Abs(fill.ActualWidth-track.ActualWidth*expected)<.001&&label.Text==$"{Math.Floor(expected*100+.5)}%"&&!control.IsTabStop&&peer.GetName()=="Upload";
                    valid&=matches;states.Add(new{theme=theme.ToString(),id,value,max,providerValue=range.Value,providerMaximum=range.Maximum,range.IsReadOnly,trackWidth=track.ActualWidth,fillWidth=fill.ActualWidth,label=label.Text,consumerValuesRetained=control.Value==value&&control.Max==max,matches});
                }
                foreach(var (id,value,max) in new[]{("zero-max",0d,0d),("negative-max",25d,-100d),("nan-max",25d,double.NaN),("infinite-max",25d,double.PositiveInfinity),("negative-infinite-max",25d,double.NegativeInfinity),("nan-value",double.NaN,100d),("infinite-value",double.PositiveInfinity,100d),("negative-infinite-value",double.NegativeInfinity,100d)})
                foreach(var path in new[]{"clr","dependency-property"})
                {
                    var control=new BleeckerProgress{Width=320,Value=65,ShowLabel=true};root.Children.Clear();root.Children.Add(control);await Layout();var rejected=false;string? error=null;
                    try{if(path=="clr"){control.Max=max;control.Value=value;}else{control.SetValue(BleeckerProgress.MaxProperty,max);control.SetValue(BleeckerProgress.ValueProperty,value);}}catch(Exception e){rejected=e is ArgumentOutOfRangeException||e.InnerException is ArgumentOutOfRangeException;error=e.GetType().Name;}
                    var rawValue=control.Value;var rawMax=control.Max;var label=Part<TextBlock>(control,"PART_Label").Text;var width=Part<Border>(control,"PART_Fill").Width;
                    control.Max=100;control.Value=65;await Layout();var recovered=Part<TextBlock>(control,"PART_Label").Text=="65%"&&Math.Abs(Part<Border>(control,"PART_Fill").ActualWidth-Part<Border>(control,"PART_Track").ActualWidth*.65)<.001;
                    valid&=rejected&&recovered;invalid.Add(new{theme=theme.ToString(),id,path,value,max,rejected,error,rawValue,rawMax,label,width,recovered});
                }
                var producer=new BindingValues();var bound=new BleeckerProgress{Width=320,ShowLabel=true};bound.SetBinding(BleeckerProgress.ValueProperty,new Binding{Source=producer,Path=new PropertyPath(nameof(BindingValues.Value))});bound.SetBinding(BleeckerProgress.MaxProperty,new Binding{Source=producer,Path=new PropertyPath(nameof(BindingValues.Max))});root.Children.Clear();root.Children.Add(bound);await Layout();
                foreach(var (value,max)in new[]{(-20d,100d),(180d,100d),(180d,200d),(65d,100d)}){producer.Max=max;producer.Value=value;await Layout();var range=(IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(bound).GetPattern(PatternInterface.RangeValue);var retained=bound.GetBindingExpression(BleeckerProgress.ValueProperty)is not null&&bound.GetBindingExpression(BleeckerProgress.MaxProperty)is not null&&bound.Value==value&&bound.Max==max&&range.Value==Math.Clamp(value,0,max);valid&=retained;contracts.Add(new{theme=theme.ToString(),value,max,rangeValue=range.Value,bindingsRetained=retained});}
                foreach(var id in new[]{"zero-bound-max","nan-bound-value"})
                {
                    var rejected=false;try{if(id=="zero-bound-max")producer.Max=0;else producer.Value=double.NaN;}catch(ArgumentException){rejected=true;}
                    try{var range=(IRangeValueProvider)FrameworkElementAutomationPeer.CreatePeerForElement(bound).GetPattern(PatternInterface.RangeValue);_ = range.Value;_ = range.Maximum;}catch(ArgumentException){rejected=true;}
                    var producerUnchanged=id=="zero-bound-max"?producer.Max==0:double.IsNaN(producer.Value);producer.Max=100;producer.Value=65;await Layout();
                    var recovered=bound.GetBindingExpression(BleeckerProgress.ValueProperty)is not null&&bound.GetBindingExpression(BleeckerProgress.MaxProperty)is not null&&bound.Value==65&&bound.Max==100&&Part<TextBlock>(bound,"PART_Label").Text=="65%";
                    valid&=rejected&&producerUnchanged&&recovered;invalidBindings.Add(new{theme=theme.ToString(),id,rejected,producerUnchanged,recovered});
                }
            }
            Guard();await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{format="orchard-progress-range-native-v1",passed=true,rangeContractPassed=valid,states,invalid,contracts,invalidBindings,guards,focus,activations,osSettingsChanged=false,hardwareInputSent=false,nativeARM64RuntimeVerified=false,librarySha256=Hash(typeof(BleeckerProgress).Assembly.Location),hostSha256=Hash(hostAssembly)},new JsonSerializerOptions{WriteIndented=true,NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals}));window.Close();Environment.Exit(0);
        }
        catch(Exception error){await File.WriteAllTextAsync(output,JsonSerializer.Serialize(new{passed=false,error=error.ToString(),states,invalid,contracts,guards,focus,activations},new JsonSerializerOptions{NumberHandling=JsonNumberHandling.AllowNamedFloatingPointLiterals}));Environment.Exit(1);}
    }
    private sealed class BindingValues:System.ComponentModel.INotifyPropertyChanged
    {
        private double value=65,max=100;public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        public double Value{get=>value;set{this.value=value;PropertyChanged?.Invoke(this,new(nameof(Value)));}}
        public double Max{get=>max;set{max=value;PropertyChanged?.Invoke(this,new(nameof(Max)));}}
    }
}
