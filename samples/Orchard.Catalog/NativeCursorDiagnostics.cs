using System.Reflection;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace Orchard.Catalog;

// Catalog-only observation plus controlled scheduling for the cancellation
// regression. All acceptance still reads the actual displayed Windows cursor.
internal static class NativeCursorDiagnostics
{
    private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    private static object Selection(FrameworkElement control)
    {
        var type=typeof(BleeckerInput).Assembly.GetType("Orchard.BleeckerPointerCursor",throwOnError:true)!;
        var roots=type.GetField("roots",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
        object?[] arguments=[control.XamlRoot.Content,null];
        if(!(bool)roots.GetType().GetMethod("TryGetValue")!.Invoke(roots,arguments)!)throw new InvalidOperationException("Cursor fixture has no native publisher.");
        return arguments[1]!;
    }
    internal static long Counter(FrameworkElement control,string name)
    {
        var selection=Selection(control);
        return (long)selection.GetType().GetField(name,Instance)!.GetValue(selection)!;
    }
    internal static bool Flag(FrameworkElement control,string name)
    {
        var selection=Selection(control);
        return (bool)selection.GetType().GetField(name,Instance)!.GetValue(selection)!;
    }
    internal static IntPtr AcknowledgedCursor(FrameworkElement control)
    {
        var selection = Selection(control);
        var snapshot = selection.GetType().GetField("lastAcknowledgement", Instance)!.GetValue(selection)
            ?? throw new InvalidOperationException("Cursor publication has no native acknowledgement.");
        var type = snapshot.GetType();
        if (!(bool)type.GetProperty("ReadSucceeded", Instance)!.GetValue(snapshot)! ||
            ((uint)type.GetProperty("Flags", Instance)!.GetValue(snapshot)! & 1) == 0)
            throw new InvalidOperationException("Cursor acknowledgement did not observe a visible native cursor.");
        return new IntPtr((long)type.GetProperty("VisibleHandle", Instance)!.GetValue(snapshot)!);
    }
    internal static void QueueOwnedRepublish(FrameworkElement control)
    {
        var tracker=control.GetType().GetField("disabledPointer",Instance)!.GetValue(control)!;
        if(!(bool)tracker.GetType().GetProperty("OwnsCursor",Instance)!.GetValue(tracker)!)
            throw new InvalidOperationException("Only an actual native cursor owner can exercise queued cancellation.");
        var selection=Selection(control);
        if((bool)selection.GetType().GetField("republishQueued",Instance)!.GetValue(selection)!)
            throw new InvalidOperationException("A native publication is already pending.");
        var source=selection.GetType().GetField("source",Instance)!.GetValue(selection)!;
        selection.GetType().GetMethod("QueueRepublish",Instance)!.Invoke(selection,[source]);
    }
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
        var source=Field("source") as InputPointerSource;
        return new
        {
            refreshCalls=observer?.GetType().GetField("refreshCalls",Instance)?.GetValue(observer),
            blockCalls=Field("blockCalls"),republishCalls=Field("republishCalls"),restoreCalls=Field("restoreCalls"),
            queuedRepublishCalls=Field("queuedRepublishCalls"),completedRepublishCalls=Field("completedRepublishCalls"),
            canceledRepublishCalls=Field("canceledRepublishCalls"),republishQueued=Field("republishQueued"),
            awaitingPublicationFrames=Field("awaitingPublicationFrames"),publicationFrames=Field("publicationFrames"),
            acknowledgedPublicationCalls=Field("acknowledgedPublicationCalls"),
            producerReplacementCalls=Field("producerReplacementCalls"),
            lastReadWasBlocked=Field("lastReadWasBlocked"),lastVisibleHandle=Field("lastVisibleHandle"),
            lastInitialPublication=Field("lastInitialPublication"),lastProducerPublication=Field("lastProducerPublication"),
            lastDeferredPublication=Field("lastDeferredPublication"),lastRestoration=Field("lastRestoration"),
            lastSettlementObservation=Field("lastSettlementObservation"),lastAcknowledgement=Field("lastAcknowledgement"),
            retainedProducerType=selected?.GetType().Name,retainedProducerShape=(selected as InputSystemCursor)?.CursorShape.ToString(),
            publishedSourceCursorType=source?.Cursor?.GetType().Name,publishedSourceCursorShape=(source?.Cursor as InputSystemCursor)?.CursorShape.ToString(),
            lastRestoredType=Field("lastRestoredType"),lastRestoredShape=Field("lastRestoredShape")?.ToString(),
            sourceQueueHasThreadAccess=source?.DispatcherQueue.HasThreadAccess,
            rootQueueHasThreadAccess=control.DispatcherQueue.HasThreadAccess,
            sameDispatcherQueue=source is not null&&source.DispatcherQueue.Equals(control.DispatcherQueue),
            active=selection is not null&&ReferenceEquals(active,selection)
        };
    }
}
