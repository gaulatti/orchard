using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
namespace Orchard.Catalog.Demos;
internal static class WorkspaceDemoUi
{
    internal static TextBlock Text(string value)=>new(){Text=value,Style=(Style)Application.Current.Resources["BleeckerBodyStyle"],TextWrapping=TextWrapping.Wrap};
    internal static BleeckerButton Button(string label,Action action,BleeckerButtonVariant variant=BleeckerButtonVariant.Secondary)
        =>new(){Content=label,Variant=variant,Size=BleeckerButtonSize.Small,Command=new DemoCommand(action)};
    internal static void Name(DependencyObject element,string value)=>AutomationProperties.SetName(element,value);
    internal static void Reconcile(Panel parent,IReadOnlyList<UIElement> children)
    {
        for(var i=parent.Children.Count-1;i>=0;i--)if(!children.Contains(parent.Children[i]))parent.Children.RemoveAt(i);
        for(var i=0;i<children.Count;i++){var item=children[i];var current=parent.Children.IndexOf(item);if(current==i)continue;if(current>=0)parent.Children.RemoveAt(current);parent.Children.Insert(i,item);}
    }
}
