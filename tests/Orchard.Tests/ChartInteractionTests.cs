using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class ChartInteractionTests
{
    [TestMethod]
    public void NativeChannelsMatchTheFrozenFocusKeyboardAndPointerSequence()
    {
        using var reference=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","LineInteraction.json")));
        Assert.AreEqual("673539e9a2c5a264fbfb6c35f09e7d956d2dbde9670f3ae6cf1888e752ec403a", reference.RootElement.GetProperty("sourceSha256").GetString());
        Assert.AreEqual("3.8.1", reference.RootElement.GetProperty("recharts").GetString());
        var categories=new[]{"Harbor","Reed","Vale","North"};
        foreach(var result in reference.RootElement.GetProperty("results").EnumerateArray())
        {
            var state=new BleeckerChartInteraction();var count=4;
            foreach(var step in result.GetProperty("steps").EnumerateArray())
            {
                var action=step.GetProperty("action").GetString()!;
                switch(action)
                {
                    case "focus":case "refocus":state.Focus();break;
                    case "blur":state.Blur();break;
                    case "ArrowRight":state.Key(1,count);break;
                    case "ArrowLeft":state.Key(-1,count);break;
                    case "Enter":state.Key(0,count);break;
                    case "pointer-midpoint":state.Pointer(1,100);break;
                    case "pointer-exit":state.Exit();break;
                    case "single-right":count=1;state.Key(1,count);break;
                    case "single-enter":state.Key(0,count);break;
                    case "empty-left":count=0;state.Key(-1,count);break;
                    case "empty-enter":state.Key(0,count);break;
                    default:throw new InvalidOperationException("Unknown frozen interaction.");
                }
                var index=state.Index(count);Assert.AreEqual(step.GetProperty("visible").GetBoolean(),index is not null,action);
                Assert.AreEqual(step.GetProperty("label").GetString(),index is int selected?categories[selected]:"",action);
            }
        }
    }
}
