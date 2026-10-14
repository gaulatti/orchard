using Microsoft.UI.Xaml;

namespace Orchard;

public enum BleeckerStatTrendDirection { Up, Down, Neutral }

/// <summary>Consumer-owned trend text, direction and optional context.</summary>
public sealed class BleeckerStatTrend : DependencyObject
{
    internal event EventHandler? Changed;
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerStatTrend),new PropertyMetadata(value,(owner,args)=>
    {
        var trend=(BleeckerStatTrend)owner;
        if(args.Property==DirectionProperty&&!Enum.IsDefined((BleeckerStatTrendDirection)args.NewValue))throw new ArgumentOutOfRangeException(nameof(Direction));
        trend.Changed?.Invoke(trend,EventArgs.Empty);
    }));
    public static readonly DependencyProperty ValueProperty=Property(nameof(Value),typeof(string),null);
    public static readonly DependencyProperty DirectionProperty=Property(nameof(Direction),typeof(BleeckerStatTrendDirection),(BleeckerStatTrendDirection)(-1));
    public static readonly DependencyProperty LabelProperty=Property(nameof(Label),typeof(string),null);
    public string? Value {get=>(string?)GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public BleeckerStatTrendDirection Direction
    {
        get=>GetValue(DirectionProperty) is BleeckerStatTrendDirection direction&&Enum.IsDefined(direction)?direction:throw new InvalidOperationException("Stat trend Direction must be supplied before consumption.");
        set{if(!Enum.IsDefined(value))throw new ArgumentOutOfRangeException(nameof(value));SetValue(DirectionProperty,value);}
    }
    public string? Label {get=>(string?)GetValue(LabelProperty);set=>SetValue(LabelProperty,value);}
}
