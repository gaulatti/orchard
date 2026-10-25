using System.Globalization;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Orchard;
namespace PackageConsumer;

public partial class App
{
    private static async Task<List<object>> VerifyHiddenProgressStackUpdatesAsync(Canvas root,string references,Func<FrameworkElement,Task> layout,Func<BleeckerProgressStack,object> snapshot,Action guard)
    {
        using var source=JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(references,"ProgressStack.json")));
        var baseline=source.RootElement.GetProperty("cases").EnumerateArray().First(row=>row.GetProperty("id").GetString()=="default").GetProperty("spec");
        var result=new List<object>();
        foreach(var motion in new[]{"reduce","no-preference"})foreach(var theme in new[]{ElementTheme.Light,ElementTheme.Dark})
        {
            root.RequestedTheme=theme;var control=new BleeckerProgressStack{Width=280,UseLayoutRounding=false};
            foreach(var item in baseline.GetProperty("items").EnumerateArray())control.Items.Add(new(){Label=item.GetProperty("label").GetString()!,Value=double.Parse(item.GetProperty("value").GetString()!,CultureInfo.InvariantCulture),Color=HiddenColor(item)});
            var originals=control.Items.ToArray();var collection=control.Items;root.Children.Clear();Canvas.SetLeft(control,20);Canvas.SetTop(control,20);root.Children.Add(control);await layout(control);
            async Task Record(string id,BleeckerProgressStackItem[] expected)
            {
                await layout(control);guard();
                if(!ReferenceEquals(collection,control.Items)||control.Items.Count!=expected.Length||control.Items.Where((item,i)=>!ReferenceEquals(item,expected[i])).Any())throw new InvalidOperationException("Collection mutation replaced consumer-owned data: "+id);
                result.Add(new{id,scheme=theme.ToString(),motion,retainedOriginalCollection=true,retainedOriginalItems=true,labels=control.Items.Select(item=>item.Label).ToArray(),measurement=snapshot(control)});
            }
            control.Items.Move(0,2);control.Items.Move(0,1);await Record("reorder",[originals[2],originals[1],originals[0]]);
            control.Items.Remove(originals[0]);control.Items.Move(1,0);await Record("remove",[originals[1],originals[2]]);
            control.Items.Add(originals[0]);control.Items.Move(2,0);control.ShowLegend=false;await Record("hide",originals);
            control.Items.Clear();control.ShowLegend=true;await Record("clear",[]);
        }
        return result;
    }
}
