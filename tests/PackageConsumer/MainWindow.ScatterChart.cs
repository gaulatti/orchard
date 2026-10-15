using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Orchard;

namespace PackageConsumer;

public sealed partial class MainWindow
{
    private void ResetScatterData()
    {
        PackageScatterChart.Data.Clear();
        PackageScatterChart.Data.Add(new Dictionary<string,object?> { ["x"]=10d,["y"]=20d,["z"]=5d });
        PackageScatterChart.Data.Add(new Dictionary<string,object?> { ["x"]=20d,["y"]=40d,["z"]=20d });
        PackageScatterChart.Series[0].Color=null;
        PackageScatterChart.Series[1].Data=new(){new Dictionary<string,object?> { ["x"]=40d,["y"]=50d,["z"]=10d }};
    }
    private async Task VerifyScatterChartAsync()
    {
        ResetScatterData();var owner=PackageScatterChart;var parent=(Panel)VisualTreeHelper.GetParent(owner);var position=parent.Children.IndexOf(owner);
        parent.Children.Remove(owner);await AwaitAsync(()=>!owner.IsLoaded,"Packaged Scatter did not unmount.");Root.Children.Add(owner);owner.VerticalAlignment=VerticalAlignment.Top;
        try
        {
            await AwaitAsync(()=>owner.IsLoaded,"Packaged Scatter did not remount.");owner.ApplyTemplate();Root.UpdateLayout();
            var chart=BrandDescendants<BleeckerLineChart>(owner).Single();var plot=BrandDescendants<Canvas>(chart).Single(canvas=>canvas.Name=="PART_Plot");Microsoft.UI.Xaml.Shapes.Path[] Points()=>plot.Children.OfType<Microsoft.UI.Xaml.Shapes.Path>().ToArray();
            await AwaitAsync(()=>Points().Length==3,"Packaged Scatter lost independent series data.");
            Require(Math.Abs(Canvas.GetLeft(Points()[0])-120)<.001&&Math.Abs(Canvas.GetTop(Points()[0])-113.33333333333333)<.001&&((PathGeometry)Points()[0].Data).Figures[0].StartPoint.X==6.794,"Packaged Scatter lost source numeric or bubble geometry.");
            Require(ReferenceEquals(chart.Data,owner.Data)&&!owner.IsTabStop&&chart.IsTabStop&&AutomationProperties.GetName(chart)=="Independent points"&&FrameworkElementAutomationPeer.CreatePeerForElement(chart).GetClassName()==nameof(BleeckerScatterChart),"Packaged Scatter lost original data or its single named inspection boundary.");
            await AcquireConsumerInputAsync();Require(chart.Focus(FocusState.Keyboard),"Could not focus packaged Scatter.");await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)=="x : 10; y : 20; z : 5","Packaged Scatter did not inspect its initial point.");
            var keys=new[]{new ConsumerInput { Type=1,Data=new ConsumerInputData { Keyboard=new ConsumerKeyboard { VirtualKey=0x27 } } },new ConsumerInput { Type=1,Data=new ConsumerInputData { Keyboard=new ConsumerKeyboard { VirtualKey=0x27,Flags=2 } } }};
            Require(SendInput(2,keys,Marshal.SizeOf<ConsumerInput>())==2,"Packaged Scatter Right did not enter native input.");await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)=="x : 20; y : 40; z : 20","Packaged Scatter did not inspect through native Right.");
            owner.Data[1]=new Dictionary<string,object?> { ["x"]=20d,["y"]=45d,["z"]=15d };await AwaitAsync(()=>AutomationProperties.GetHelpText(chart)=="x : 20; y : 45; z : 15","Packaged Scatter ignored live root records.");
            var previousX=Canvas.GetLeft(Points()[2]);var independent=owner.Series[1].Data!;independent[0]=new Dictionary<string,object?> { ["x"]=30d,["y"]=55d,["z"]=11d };await AwaitAsync(()=>Canvas.GetLeft(Points()[2])!=previousX,"Packaged Scatter ignored independent data notifications.");
            var paint=new SolidColorBrush(Windows.UI.Color.FromArgb(0,0,0,0));owner.Series[0].Color=paint;await AwaitAsync(()=>ReferenceEquals(Points()[0].Fill,paint),"Packaged Scatter replaced a transparent consumer brush.");
            owner.Data.Clear();await AwaitAsync(()=>Points().Length==1,"Packaged Scatter retained cleared root points.");independent.Clear();await AwaitAsync(()=>Points().Length==0,"Packaged Scatter retained empty point shapes.");
        }
        finally{Root.Children.Remove(owner);parent.Children.Insert(position,owner);}
    }
}
