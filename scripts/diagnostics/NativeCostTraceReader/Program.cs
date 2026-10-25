using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Diagnostics.Tracing;

if(args.Length!=2)throw new ArgumentException("Expected input.nettrace output.json");
var markers=new List<object>();var allocations=new List<object>();var providerErrors=new List<string>();
using(var source=new EventPipeEventSource(args[0]))
{
    source.Dynamic.All+=data=>
    {
        if(data.ProviderName!="Orchard-DataTable-NativeCost")return;
        if((int)data.ID==0){providerErrors.Add(data.FormattedMessage??data.EventName);return;}
        if((int)data.ID!=1)throw new InvalidOperationException("Unexpected native-cost event ID");
        var names=data.PayloadNames;
        object Value(string name){var index=Array.IndexOf(names,name);if(index<0)throw new InvalidOperationException("Missing marker payload "+name);return data.PayloadValue(index);}
        string Text(string name)=>Convert.ToString(Value(name),CultureInfo.InvariantCulture)??throw new InvalidOperationException("Null marker text");
        long Integer(string name)=>Convert.ToInt64(Value(name),CultureInfo.InvariantCulture);
        markers.Add(new{milliseconds=data.TimeStampRelativeMSec,threadId=data.ThreadID,processId=data.ProcessID,eventName=data.EventName,stage=Text("stage"),phase=Text("phase"),boundary=Integer("boundary"),sourceRows=Integer("sourceRows"),pageSize=Integer("pageSize"),allocatedBytes=Integer("allocatedBytes"),pauseTicks=Integer("pauseTicks")});
    };
    source.Clr.GCAllocationTick+=data=>allocations.Add(new{milliseconds=data.TimeStampRelativeMSec,threadId=data.ThreadID,processId=data.ProcessID,typeName=data.TypeName,estimatedBytes=data.AllocationAmount64,allocationKind=data.AllocationKind.ToString()});
    source.Process();
}
using var input=File.OpenRead(args[0]);
var report=new{format="orchard-data-table-native-cost-eventpipe-v1",recordedAt=DateTime.UtcNow,trace=args[0],traceSha256=Convert.ToHexStringLower(SHA256.HashData(input)),traceReaderAssembly=typeof(EventPipeEventSource).Assembly.GetName().FullName,markers,allocations,providerErrors,allocationTicksAreSampled=true,traceLossCounterAvailable=false,normalTimingRun=false,nativeComponentAccepted=false};
await File.WriteAllTextAsync(args[1],JsonSerializer.Serialize(report));
Console.WriteLine(JsonSerializer.Serialize(new{rawTraceDecoded=true,markerEvents=markers.Count,allocationTicks=allocations.Count,providerErrors=providerErrors.Count,normalTimingRun=false}));
