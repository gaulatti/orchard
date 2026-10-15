using System.Collections;
using System.Collections.Specialized;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Orchard;

public sealed partial class BleeckerDataTableToolbar
{
    private sealed class Slot(BleeckerDataTableToolbar owner)
    {
        private object? previous;
        private DataTemplate? template;
        private bool dirty=true;
        private readonly HashSet<INotifyCollectionChanged> collections=[];
        private readonly List<ContentPresenter> presenters=[];
        internal IReadOnlyList<UIElement> Elements{get;private set;}=[];
        internal void Update(object? value,DataTemplate? nextTemplate)
        {
            if(!dirty&&ReferenceEquals(value,previous)&&ReferenceEquals(template,nextTemplate))return;
            Disconnect();foreach(var presenter in presenters)presenter.Content=null;presenters.Clear();
            previous=value;template=nextTemplate;var elements=new List<UIElement>();var path=new HashSet<object>(ReferenceEqualityComparer.Instance);
            if(nextTemplate is not null){var presenter=new ContentPresenter{Content=value,ContentTemplate=nextTemplate,UseLayoutRounding=false};presenters.Add(presenter);elements.Add(presenter);}
            else Add(value);
            Elements=elements;dirty=false;
            void Add(object? item)
            {
                if(item is null||item is bool||item is string empty&&empty.Length==0)return;
                if(item is UIElement element){elements.Add(element);return;}
                if(item is IEnumerable items&&item is not string&&item is not IDictionary)
                {
                    if(!path.Add(item))throw new ArgumentException("Toolbar content contains a cyclic collection.");
                    if(owner.IsLoaded&&item is INotifyCollectionChanged collection&&collections.Add(collection))collection.CollectionChanged+=Changed;
                    foreach(var child in items)Add(child);path.Remove(item);return;
                }
                if(item is not(string or char or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal))throw new ArgumentException("Toolbar content models require a DataTemplate.");
                var text=new TextBlock{Text=BleeckerDataTableValue.InputText(item),TextWrapping=TextWrapping.Wrap,LineHeight=24,LineStackingStrategy=LineStackingStrategy.BlockLineHeight,UseLayoutRounding=false};
                text.SetBinding(TextBlock.FontFamilyProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(FontFamily))});text.SetBinding(TextBlock.FontSizeProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(FontSize))});text.SetBinding(TextBlock.ForegroundProperty,new Binding{Source=owner,Path=new PropertyPath(nameof(Foreground))});elements.Add(text);
            }
        }
        private void Changed(object? sender,NotifyCollectionChangedEventArgs args){dirty=true;owner.Refresh();}
        internal void Disconnect(){foreach(var collection in collections)collection.CollectionChanged-=Changed;collections.Clear();dirty=true;}
    }
}
