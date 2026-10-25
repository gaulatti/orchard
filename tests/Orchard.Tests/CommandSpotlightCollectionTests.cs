using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class CommandSpotlightCollectionTests
{
    private sealed record Item(string Id, string Title, string Group, bool Disabled)
    {
        internal BleeckerCommandSpotlightData Describe() => new(Id, Title, null, Group, [], null, Disabled);
    }
    private static Item[] Items(int count, string grouping) => Enumerable.Range(0,count)
        .Select(i=>new Item(i.ToString(),$"Fictional item {i:D6}",grouping=="unique"?$"Group {i:D6}":grouping=="ten"?$"Group {i%10:D6}":"Group",i%17==0)).ToArray();
    [TestMethod]
    public void CollectionModelRetainsCallerOrderAndReportsMeasuredCosts()
    {
        var warm=new BleeckerCommandSpotlightState<Item>(i=>i.Describe());warm.SetActions(Items(100,"unique"),true);
        var results=new List<object>();
        foreach(var grouping in new[]{"single","ten","unique"})foreach(var count in new[]{1000,2000,4000,8000,16000})
        {
            var originals=Items(count,grouping);var samples=new List<object>();
            for(var sample=0;sample<3;sample++)
            {
                var model=new BleeckerCommandSpotlightState<Item>(i=>i.Describe());
                var allocated=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();model.SetActions(originals,true);clock.Stop();
                var elapsed=clock.Elapsed.TotalMilliseconds;var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
                Assert.AreSame(originals,model.Filtered);Assert.AreEqual(grouping=="unique"?count:grouping=="ten"?10:1,model.Groups.Count);
                Assert.AreSame(originals[1],model.EnterTarget);Assert.IsTrue(model.Groups.SelectMany(g=>g.Actions).All(i=>ReferenceEquals(originals[int.Parse(i.Id)],i)));
                clock.Restart();model.SetQuery("000042");clock.Stop();var queryElapsed=clock.Elapsed.TotalMilliseconds;
                Assert.AreEqual(1,model.Filtered.Count);Assert.AreSame(originals[42],model.EnterTarget);
                samples.Add(new{sample,setMilliseconds=elapsed,setAllocatedBytes=bytes,queryMilliseconds=queryElapsed});
            }
            results.Add(new{grouping,count,samples});
        }
        Console.WriteLine("ORCHARD_SPOTLIGHT_COLLECTION_MODEL "+JsonSerializer.Serialize(new{format="orchard-command-spotlight-collection-model-v1",framework=RuntimeInformation.FrameworkDescription,os=RuntimeInformation.OSDescription,architecture=RuntimeInformation.ProcessArchitecture.ToString(),processorCount=Environment.ProcessorCount,frequency=Stopwatch.Frequency,behaviorPassed=true,latencyAcceptanceClaimed=false,results}));
    }
    [TestMethod]
    public void ManyGroupsPreserveFirstOccurrenceAndOriginalValuesAcrossReorderAndQuery()
    {
        var originals=Items(4096,"unique");var model=new BleeckerCommandSpotlightState<Item>(i=>i.Describe());model.SetActions(originals,true);
        CollectionAssert.AreEqual(originals.Select(i=>i.Group).ToArray(),model.Groups.Select(g=>g.Name).ToArray());
        model.SetActions(originals.Reverse().ToArray(),true);
        CollectionAssert.AreEqual(originals.Reverse().Select(i=>i.Group).ToArray(),model.Groups.Select(g=>g.Name).ToArray());
        Assert.AreSame(originals[1],model.EnterTarget);model.SetQuery("000042");Assert.AreSame(originals[42],model.EnterTarget);
        model.SetQuery("");Assert.AreSame(originals[42],model.EnterTarget);Assert.AreEqual(4096,model.Filtered.Count);
    }
}
