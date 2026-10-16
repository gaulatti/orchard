using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Orchard.Catalog;

public sealed partial class PieChartsFixture
{
    private async Task<object> VerifyPointerKeyboardAsync(BleeckerLineChart chart,Grid host,NativeInputSession input,JsonElement original,JsonElement pointerCase,string id,bool donut,BleeckerColorScheme scheme,string folder,Action<Action> check)
    {
        var states=new Dictionary<string,object>();var trace=new List<object>();var phase="hover";
        void Moved(object sender,PointerRoutedEventArgs args){var point=args.GetCurrentPoint(chart);trace.Add(new{point.Position,point.Timestamp,args.IsGenerated});}
        chart.PointerMoved+=Moved;
        object Snapshot()=>new{tooltip=InspectionText(chart),tooltipVisible=Part<Border>(chart,"PART_Tooltip").Visibility==Visibility.Visible,activeCount=Part<Canvas>(chart,"PART_Active").Children.Count,chart.FocusState,focused=ReferenceEquals(FocusManager.GetFocusedElement(XamlRoot),chart),help=AutomationProperties.GetHelpText(chart)};
        async Task StateAsync(string name,JsonElement expected)
        {
            phase=name;
            await AwaitAsync(()=>InspectionText(chart)==expected.GetProperty("tooltip").GetString()&&(Part<Border>(chart,"PART_Tooltip").Visibility==Visibility.Visible)==expected.GetProperty("tooltipVisible").GetBoolean(),"Pie "+name+" payload/visibility did not settle.");
            check(()=>Inspection(chart,expected));states[name]=Snapshot();
        }
        try
        {
            var box=pointerCase.GetProperty("outside");
            var after=new BleeckerButton{Name="CircularChartAfter",Content="After circular chart",Width=box.GetProperty("width").GetDouble(),Height=box.GetProperty("height").GetDouble(),Margin=new Thickness(box.GetProperty("x").GetDouble(),box.GetProperty("y").GetDouble(),0,0),UseLayoutRounding=false,HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Top};host.Children.Add(after);
            var point=pointerCase.GetProperty("point");await input.MoveAsync(chart,localPoint:new Point(point.GetProperty("x").GetDouble(),point.GetProperty("y").GetDouble()));await StateAsync("hover",pointerCase.GetProperty("hover"));
            await input.MoveAsync(after);await StateAsync("exit",pointerCase.GetProperty("exit"));Require(AutomationProperties.GetHelpText(chart)=="","Pie exit retained accessible inspection.");
            after.Visibility=Visibility.Collapsed;await StateAsync("normal",original.GetProperty("normal"));
            if(id is "default" or "narrow" or "center" or "mixed-sign" or "long-labels" or "ring-large"){await MainWindow.AwaitPresentationAsync(host);await NativeScreenCapture.CaptureClientAsync(input.Window,Path.Combine(folder,$"pie-chart-{id}-{donut}-{scheme}.png"));}
            Require(chart.Focus(FocusState.Keyboard),"Could not focus native Pie inspection.");await StateAsync("focus",original.GetProperty("focus"));
            foreach(var (state,key)in new[]{("right",(ushort)0x27),("left",(ushort)0x25),("enter",(ushort)0x0D)}){phase=state;await input.KeyAsync(key,chart);await StateAsync(state,original.GetProperty(state));}
            return new{states,pointerTrace=trace};
        }
        catch
        {
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var interaction=typeof(BleeckerLineChart).GetField("interaction",flags)!.GetValue(chart)!;
            await File.WriteAllTextAsync(Path.Combine(folder,$"pie-input-failure-{id}-{donut}-{scheme}.json"),JsonSerializer.Serialize(new{id,donut,phase,actual=Snapshot(),states,pointerTrace=trace,input.OwnsForeground,screenPointer=input.ReadPointerPosition(),screenBounds=input.ScreenBounds(chart),interaction=interaction.GetType().GetFields(flags).ToDictionary(field=>field.Name,field=>field.GetValue(interaction))}));throw;
        }
        finally{chart.PointerMoved-=Moved;}
    }
}
