using System.Diagnostics;

namespace Orchard.Text.Tests;

public sealed partial class ProgressStackTextTests
{
    [TestMethod]
    public void LongUnicodeMinimumAndWrapAllocationRemainBounded()
    {
        var warm=Inline(new string('日',64),false,false);_=warm.MinimumWidth;_=warm.Wrap(80);
        (long Bytes,double CpuMilliseconds) Sample(int length,bool mixed)
        {
            // Prepare the actual native shaping result before measuring the
            // repeated min-content and wrap queries that previously grew N².
            var text=mixed?string.Concat(Enumerable.Range(0,length).Select(index=>index%2==0?"日":"あ")):new string('日',length);var inline=Inline(text,false,false);
            using var process=Process.GetCurrentProcess();var cpu=process.TotalProcessorTime;
            var allocated=GC.GetAllocatedBytesForCurrentThread();var timer=Stopwatch.StartNew();
            var minimum=inline.MinimumWidth;var lines=inline.Wrap(80);timer.Stop();
            var bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            var cpuMilliseconds=(process.TotalProcessorTime-cpu).TotalMilliseconds;
            Assert.AreEqual(12d,minimum);Assert.AreEqual((length+5)/6,lines.Count);Assert.AreEqual(text,string.Concat(lines));
            Console.WriteLine($"CJK characters={length}, mixed fonts={mixed}, min/wrap managed bytes={bytes}, CPU milliseconds={cpuMilliseconds:R}, elapsed milliseconds={timer.Elapsed.TotalMilliseconds:R}");
            return(bytes,cpuMilliseconds);
        }
        foreach(var mixed in new[]{false,true})
        {
            var small=Sample(2000,mixed);var large=Sample(8000,mixed);
            Assert.IsLessThan(1_048_576L,large.Bytes,"8,000-character layout allocated more than 1 MiB.");
            Assert.IsLessThan(small.Bytes*6,large.Bytes,"Four times the text must not cause quadratic allocation growth.");
            Assert.IsLessThan(1000d,large.CpuMilliseconds,"8,000-character min/wrap queries consumed one second of CPU.");
        }
    }
}
