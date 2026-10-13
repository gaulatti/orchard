using System.Reflection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

// Catalog-only observation. These fields explain the native boundary without
// replacing the actual displayed-cursor assertion or changing publication.
internal static class NativeCursorDiagnostics
{
    private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    internal static object Describe(FrameworkElement control)
    {
        var tracker=control.GetType().GetField("disabledPointer",Instance)?.GetValue(control);
        var observer=tracker?.GetType().GetField("observer",Instance)?.GetValue(tracker);
        var type=typeof(BleeckerInput).Assembly.GetType("Orchard.BleeckerPointerCursor",throwOnError:true)!;
        var active=type.GetField("active",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null);
        var roots=type.GetField("roots",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
        object? selection=null;
        if(control.XamlRoot?.Content is UIElement root)
        {
            object?[] arguments=[root,null];
            if((bool)roots.GetType().GetMethod("TryGetValue")!.Invoke(roots,arguments)!)selection=arguments[1];
        }
        object? Field(string name)=>selection?.GetType().GetField(name,Instance)?.GetValue(selection);
        var selected=Field("selected") as InputCursor;
        return new
        {
            refreshCalls=observer?.GetType().GetField("refreshCalls",Instance)?.GetValue(observer),
            blockCalls=Field("blockCalls"),republishCalls=Field("republishCalls"),restoreCalls=Field("restoreCalls"),
            lastReadWasBlocked=Field("lastReadWasBlocked"),lastVisibleHandle=Field("lastVisibleHandle"),
            retainedProducerType=selected?.GetType().Name,retainedProducerShape=(selected as InputSystemCursor)?.CursorShape.ToString(),
            lastRestoredType=Field("lastRestoredType"),lastRestoredShape=Field("lastRestoredShape")?.ToString(),
            active=selection is not null&&ReferenceEquals(active,selection)
        };
    }
}
