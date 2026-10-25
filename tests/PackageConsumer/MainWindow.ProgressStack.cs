using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using Orchard;
namespace PackageConsumer;
public sealed partial class MainWindow
{
 private async Task VerifyProgressStackAsync()
 {
  var brush=new SolidColorBrush(Microsoft.UI.Colors.Red);var item=new BleeckerProgressStackItem{Label="Published",Value=6,Color=brush};var control=new BleeckerProgressStack{Width=280};control.Items.Add(item);control.Items.Add(new(){Label="Draft",Value=4});var host=new Border{Margin=new(32),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top,Child=control};Root.Children.Add(host);var passed=false;var phases=new List<object>();
  async Task PresentAsync(){Root.UpdateLayout();var completion=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var frames=0;void Rendering(object? sender,object args){if(control.IsLoaded&&control.ActualWidth>0&&++frames>=2)completion.TrySetResult();}CompositionTarget.Rendering+=Rendering;try{await completion.Task.WaitAsync(TimeSpan.FromSeconds(2));}finally{CompositionTarget.Rendering-=Rendering;}}
  try
  {
   await PresentAsync();var peer=FrameworkElementAutomationPeer.CreatePeerForElement(control);Require(!control.IsTabStop&&peer.GetAutomationControlType()==AutomationControlType.Group&&peer.GetPattern(PatternInterface.Invoke)is null&&peer.GetPattern(PatternInterface.RangeValue)is null,"Packaged ProgressStack gained an interactive pattern.");Require(control.ActualHeight==40,"Packaged ProgressStack default height changed.");var declarative=(BleeckerProgressStack)XamlReader.Load("<orchard:BleeckerProgressStack xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:orchard='using:Orchard' Max='20' />");Require(declarative.Max==20,"Packaged ProgressStack XAML maximum was not numeric.");
   item.Label="Updated";item.Value=8;control.Max=20;await PresentAsync();Require(ReferenceEquals(control.Items[0],item)&&ReferenceEquals(item.Color,brush),"Packaged ProgressStack replaced consumer data or brush.");item.Value=0;item.Value=-0d;Require(BitConverter.DoubleToInt64Bits(item.Value)==long.MinValue,"Packaged ProgressStack replaced the original negative zero.");phases.Add(new{phase="live-data",retained=true});
   var old=control.Items;control.Items=new ObservableCollection<BleeckerProgressStackItem>(old.Reverse());old.Clear();await PresentAsync();Require(control.Items.Count==2&&ReferenceEquals(control.Items[^1],item),"Packaged ProgressStack lost reordered data.");phases.Add(new{phase="replace-reorder",retained=true});
   control.ShowLegend=false;await PresentAsync();Require(control.ActualHeight==12,"Packaged ProgressStack hidden legend retained spacing.");control.Items.Clear();control.ShowLegend=true;await PresentAsync();Require(control.ActualHeight==24,"Packaged ProgressStack empty legend lost spacing.");phases.Add(new{phase="presence",retained=true});
   control.Items.Add(item);host.Child=null;item.Value=12;host.Child=control;AutomationProperties.SetName(control,"Consumer distribution");await PresentAsync();Require(control.Items.Single().Value==12&&FrameworkElementAutomationPeer.CreatePeerForElement(control).GetName()=="Consumer distribution","Packaged ProgressStack remount or name failed.");phases.Add(new{phase="remount-name",retained=true});
   var focus=Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot);control.Width=100;control.Max=null;item.Value=6;item.Label="日本語の公開ワークフロー";control.Items=new(){item,new(){Label="Draft",Value=3}};await PresentAsync();
   var label=BrandDescendants<FrameworkElement>(control).First(element=>element.Name=="ProgressStackLabel");var text=BrandDescendants<TextBlock>(label).Single();
   Require(control.ActualHeight==84&&label.ActualWidth==75.46875&&label.ActualHeight==32&&text.Text=="日本語の公開\nワークフロー"&&ReferenceEquals(focus,Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot)),"Packaged ProgressStack CJK wrap, min-content allocation or focus changed.");phases.Add(new{phase="unicode-cjk",retained=true});
   Require(FrameworkElementAutomationPeer.CreatePeerForElement(text).GetName()==item.Label,"Packaged ProgressStack exposed a painted line break as its accessible name.");phases.Add(new{phase="unicode-cjk-accessible-name",retained=true});
   const string original="publi\u00adcation\u00adworkflow";item.Label=original;await PresentAsync();
   var segment=BrandDescendants<FrameworkElement>(control).First(element=>element.Name=="ProgressStackSegment");
   Require(item.Label==original&&ToolTipService.GetToolTip(segment)?.ToString()==original+": 6"&&control.ActualHeight==84&&text.Text=="publication\u2010\nworkflow"&&ReferenceEquals(focus,Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(Root.XamlRoot)),"Packaged ProgressStack discretionary hyphen changed the original label, tooltip, layout or focus.");phases.Add(new{phase="unicode-soft-hyphen",retained=true});
   Require(FrameworkElementAutomationPeer.CreatePeerForElement(text).GetName()==original,"Packaged ProgressStack replaced the logical accessible soft hyphens.");phases.Add(new{phase="unicode-soft-hyphen-accessible-name",retained=true});passed=true;
  }
  finally{Root.Children.Remove(host);var args=Environment.GetCommandLineArgs();var output=args[Array.IndexOf(args,"--verify-output")+1];await File.WriteAllTextAsync(Path.Combine(Path.GetDirectoryName(output)!,"consumer-progress-stack-"+Root.RequestedTheme+".json"),JsonSerializer.Serialize(new{passed,phases}));}
 }
}
