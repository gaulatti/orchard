using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Orchard;

/// <summary>Consumer-owned keyed content and independent actions in an accordion.</summary>
public sealed class BleeckerAccordionItem : INotifyPropertyChanged
{
    private string id="";
    private object? title,subtitle,content,actions;
    public BleeckerAccordionItem() { }
    public BleeckerAccordionItem(string id,object? title,object? content){Id=id;Title=title;Content=content;}
    public string Id {get=>id;set{ArgumentNullException.ThrowIfNull(value);Set(ref id,value);}}
    public object? Title {get=>title;set=>Set(ref title,value);}
    public object? Subtitle {get=>subtitle;set=>Set(ref subtitle,value);}
    public object? Content {get=>content;set=>Set(ref content,value);}
    public object? Actions {get=>actions;set=>Set(ref actions,value);}
    public event PropertyChangedEventHandler? PropertyChanged;
    private void Set<T>(ref T field,T value,[CallerMemberName]string? name=null){if(EqualityComparer<T>.Default.Equals(field,value))return;field=value;PropertyChanged?.Invoke(this,new(name));}
}
