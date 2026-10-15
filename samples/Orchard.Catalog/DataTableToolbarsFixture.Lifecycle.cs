using System.Collections.ObjectModel;
using System.Text.Json;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Orchard.Catalog;

public sealed partial class DataTableToolbarsFixture
{
    private async Task VerifyLifecycleAsync(BleeckerColorScheme scheme,string folder)
    {
        var search=Descendants<BleeckerInput>(Preview).Single();var originalWrites=table.Writes.Count;
        var raw=new Dictionary<string,object?>{{"query","Harbor"}};table.SetFilters(new[]{new KeyValuePair<string,object?>("title",raw)});await MainWindow.AwaitPresentationAsync(Preview);
        Require(search.Text=="[object Object]"&&ReferenceEquals(table.GetColumn("title")!.GetFilterValue(),raw)&&table.Writes.Count==originalWrites,"Controlled rendering replaced the consumer's raw filter value.");
        var emptyArray=Array.Empty<object>();table.SetFilters([]);table.SetGlobal(emptyArray);await MainWindow.AwaitPresentationAsync(Preview);
        Require(Descendants<Button>(Preview).Any(button=>button.Name=="PART_Reset")&&ReferenceEquals(table.GlobalFilter,emptyArray),"An empty-array global filter lost source truthiness or identity.");
        table.SetGlobal(null);
        var second=new ToolbarFilterTable(second:true);second.Changed+=(_,_)=>{if(ReferenceEquals(activeTable,second))Results.Text=second.VisibleText;};
        activeTable=second;Preview.Table=second;Preview.SearchColumn="category";Results.Text=second.VisibleText;await MainWindow.AwaitPresentationAsync(Preview);
        Require(table.Subscribers==1&&second.Subscribers==2&&search.Text=="","Table replacement did not detach/reconnect the original sources.");
        table.GetColumn("title")!.SetFilterValue("Reed");await MainWindow.AwaitPresentationAsync(Preview);Require(search.Text==""&&Results.Text==second.VisibleText,"Detached table repainted the active consumer.");
        second.GetColumn("category")!.SetFilterValue("Vale");await MainWindow.AwaitPresentationAsync(Preview);Require(search.Text=="Vale"&&Results.Text=="Vale","Second table updates did not reach its original column and view.");
        activeTable=table;Preview.Table=table;Preview.SearchColumn="title";Results.Text=table.VisibleText;await MainWindow.AwaitPresentationAsync(Preview);Require(search.Text=="Reed"&&Results.Text=="Reed"&&second.Subscribers==1,"Restoring the first table lost its own state or retained the second source.");
        var parent=(Panel)Preview.Parent;var index=parent.Children.IndexOf(Preview);parent.Children.Remove(Preview);await AwaitAsync(()=>!Preview.IsLoaded&&table.Subscribers==1,"Toolbar did not finish unloading its table observers.");
        table.GetColumn("title")!.SetFilterValue("Harbor");parent.Children.Insert(index,Preview);await AwaitAsync(()=>Preview.IsLoaded,"Toolbar did not remount.");await MainWindow.AwaitPresentationAsync(Preview);Require(table.Subscribers==2&&search.Text=="Harbor","Remounted toolbar did not reconnect to current state.");
        var template=Preview.Template;Preview.Template=null;table.GetColumn("title")!.SetFilterValue("Vale");Preview.Template=template;Preview.ApplyTemplate();await MainWindow.AwaitPresentationAsync(Preview);
        search=Descendants<BleeckerInput>(Preview).Single();Require(search.Text=="Vale"&&ReferenceEquals(Preview.Table,table)&&ReferenceEquals(Filter.Parent,search.Parent),"Template replacement lost current state or original slot content.");
        var old=new ObservableCollection<object?>{Filter};Preview.FilterRenderers=old;var extra=new Button{Content="Second filter",Width=80,Height=36};old.Add(extra);await MainWindow.AwaitPresentationAsync(Preview);Require(ReferenceEquals(extra.Parent,search.Parent),"Live consumer collection did not add its original node.");
        var replacement=new ObservableCollection<object?>{Filter};Preview.FilterRenderers=replacement;await MainWindow.AwaitPresentationAsync(Preview);Require(extra.Parent is null,"Replaced filter collection retained its old node.");old.Clear();old.Add(new Button{Content="Detached source"});await MainWindow.AwaitPresentationAsync(Preview);Require(ReferenceEquals(Filter.Parent,search.Parent)&&!Descendants<Button>(Preview).Any(button=>Equals(button.Content,"Detached source")),"Stale collection repainted the active toolbar.");
        var payload=new object();var contentTemplate=(DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load("<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Width='80' Height='24' Tag='{Binding}'/></DataTemplate>");
        Preview.FilterTemplate=contentTemplate;Preview.FilterRenderers=payload;await MainWindow.AwaitPresentationAsync(Preview);Require(Descendants<Border>(Preview).Any(border=>ReferenceEquals(border.Tag,payload)),"Filter template lost its original model.");
        Preview.FilterRenderers=Filter;Preview.FilterTemplate=null;table.SetInitial([]);table.SetFilters([]);table.SetGlobal(null);await MainWindow.AwaitPresentationAsync(Preview);
        await File.WriteAllTextAsync(Path.Combine(folder,$"data-table-toolbar-lifecycle-{scheme}.json"),JsonSerializer.Serialize(new{passed=true,rawValuePreserved=true,tableReplacement=true,detachedObservers=true,reparent=true,templateReplacement=true,originalCollectionNodes=true,staleCollectionDetached=true,originalTemplateModel=true,table.Subscribers}));
    }
}
