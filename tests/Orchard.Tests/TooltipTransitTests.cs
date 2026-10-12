using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class TooltipTransitTests
{
    [TestMethod]
    public void TransitKeepsTheGapOpenInBothDirectionsOnEverySide()
    {
        var owner = new TooltipBounds(100, 100, 180, 140);
        foreach (var (content, exit, reverseExit, gap, outside) in new[]
        {
            (new TooltipBounds(90, 52, 190, 88), new TooltipPoint(140, 99), new TooltipPoint(140, 89), new TooltipPoint(140, 94), new TooltipPoint(40, 94)),
            (new TooltipBounds(90, 152, 190, 188), new TooltipPoint(140, 141), new TooltipPoint(140, 151), new TooltipPoint(140, 146), new TooltipPoint(240, 146)),
            (new TooltipBounds(-12, 102, 88, 138), new TooltipPoint(99, 120), new TooltipPoint(89, 120), new TooltipPoint(94, 120), new TooltipPoint(94, 200)),
            (new TooltipBounds(192, 102, 292, 138), new TooltipPoint(181, 120), new TooltipPoint(191, 120), new TooltipPoint(186, 120), new TooltipPoint(186, 40))
        })
        {
            var forward = BleeckerPointerTransit.Create(exit, owner, content);
            var reverse = BleeckerPointerTransit.Create(reverseExit, content, owner);
            Assert.IsTrue(BleeckerPointerTransit.Contains(forward, gap));
            Assert.IsTrue(BleeckerPointerTransit.Contains(reverse, gap));
            Assert.IsFalse(BleeckerPointerTransit.Contains(forward, outside));
            Assert.IsFalse(BleeckerPointerTransit.Contains(reverse, outside));
        }
    }
}
