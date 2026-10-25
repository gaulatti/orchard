using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Microsoft.UI.Input;
using Windows.UI.Core;

namespace Orchard.Catalog;

public sealed partial class DataTablesFixture : StackPanel
{
    private sealed record Item(string Id,string Name,int Count,string Status,BleeckerButton Action);
    private readonly Dictionary<string,BleeckerButton> headings=new(StringComparer.Ordinal);
    private readonly Dictionary<string,BleeckerCheckbox> checks=new(StringComparer.Ordinal);
    private BleeckerCheckbox? all;
    public DataTablesFixture()
    {
        InitializeComponent();
        Data.Columns.Add(new BleeckerDataTableColumn(id:"select",headerContentSelector:HeaderSelection,cellContentSelector:CellSelection));
        Data.Columns.Add(new BleeckerDataTableColumn(accessorKey:"Name",header:"Fictional item",headerContentSelector:SortHeader));
        Data.Columns.Add(new BleeckerDataTableColumn(accessorKey:"Count",header:"Count",headerContentSelector:SortHeader));
        Data.Columns.Add(new BleeckerDataTableColumn(accessorKey:"Status",header:"Status",cellContentSelector:OriginalAction));
        var items=Enumerable.Range(0,23).Select(i=>
        {
            var id="fictional-"+i.ToString("D2");var action=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content=i%2==0?"Queued":"Ready"};
            action.Click+=(_,_)=>Status.Text="Original action: "+id;
            return new Item(id,new[]{"Cedar","Birch","Ash"}[i%3]+" "+(i+1),(i*7)%29,i%2==0?"Queued":"Ready",action);
        }).ToArray();
        Data.ItemsSource=items;Data.Model.Changed+=ModelChanged;
    }
    private object HeaderSelection(BleeckerDataTableHeader header)
    {
        if(all is null){all=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(all,"Select page");all.Checked+=AllChanged;all.Unchecked+=AllChanged;}
        all.IsChecked=header.Table.AllPageRowsSelected;return all;
    }
    private void AllChanged(object sender,RoutedEventArgs args){if(all?.IsChecked!=Data.Model.AllPageRowsSelected)Data.Model.ToggleAllPageRowsSelected(all?.IsChecked==true);}
    private object SortHeader(BleeckerDataTableHeader header)
    {
        if(!headings.TryGetValue(header.Column.Id,out var button))
        {
            var id=header.Column.Id;button=new BleeckerButton{Variant=BleeckerButtonVariant.Ghost,Content=header.Column.Header};
            button.Click+=(_,_)=>Data.Model.GetColumn(id)?.ToggleSorting(multi:(InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)&CoreVirtualKeyStates.Down)!=0);headings.Add(id,button);
        }
        return button;
    }
    private object CellSelection(BleeckerDataTableCell cell)=>RowSelection(cell.Row);
    private object RowSelection(BleeckerDataTableRow row)
    {
        if(!checks.TryGetValue(row.Id,out var check))
        {
            var id=row.Id;check=new BleeckerCheckbox{Width=20,Height=20,MinWidth=0,MinHeight=0};Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(check,"Select row "+id);
            void Changed(object sender,RoutedEventArgs args){var current=Data.Model.CoreRows.FirstOrDefault(r=>r.Id==id);if(current is not null&&check.IsChecked!=current.IsSelected)current.ToggleSelected(check.IsChecked==true);}
            check.Checked+=Changed;check.Unchecked+=Changed;checks.Add(id,check);
        }
        check.IsChecked=row.IsSelected;return check;
    }
    private static object OriginalAction(BleeckerDataTableCell cell)=>((Item)cell.Row.Original).Action;
    private void ModelChanged(object? sender,EventArgs args)
    {
        BleeckerDataTableModel model=Data.Model;BleeckerDataTableColumnContext? column=model.GetColumn("Name");
        Status.Text="Page "+(model.PageIndex+1)+" / "+model.PageCount+"; "+model.FilteredRowCount+" fictional rows; "+model.FilteredSelectedRowCount+" selected. Search: "+BleeckerDataTableModel.CellText(column?.GetFilterValue());
    }
}
