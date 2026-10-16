using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orchard;

namespace Orchard.Tests;

[TestClass]
public sealed class StepperContractTests
{
    [TestMethod]
    public void BrowserBaselinesCoverEveryNonemptyParagraphAndOrdinal()
    {
        using var typography = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "StepperTypography.json")));
        using var normal = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Stepper.json")));
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureStepperTypography.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(typography.RootElement.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        Assert.AreEqual(normal.RootElement.GetProperty("sourceSha256").GetString(), typography.RootElement.GetProperty("sourceSha256").GetString());
        var cases = 0; var baselines = 0;
        foreach (var report in typography.RootElement.GetProperty("reports").EnumerateArray())
        {
            var source = normal.RootElement.GetProperty("cases").EnumerateArray().Single(item => item.GetProperty("id").GetString() == report.GetProperty("id").GetString() && item.GetProperty("scheme").GetString() == report.GetProperty("scheme").GetString());
            var rows = report.GetProperty("rows"); Assert.AreEqual(source.GetProperty("normal").GetProperty("rows").GetArrayLength(), rows.GetArrayLength());
            foreach (var row in rows.EnumerateArray())
                foreach (var key in new[] { "label", "description", "button" })
                {
                    var text = row.GetProperty(key);
                    if (text.ValueKind == JsonValueKind.Null || text.GetProperty("firstBaseline").ValueKind == JsonValueKind.Null) continue;
                    Assert.IsGreaterThan(0, text.GetProperty("rangeAscent").GetDouble());
                    Assert.IsGreaterThan(0, text.GetProperty("rangeDescent").GetDouble());
                    Assert.IsTrue(text.GetProperty("rects").GetArrayLength() > 0);
                    baselines++;
                }
            cases++;
        }
        Assert.AreEqual(44, cases); Assert.AreEqual(324, baselines);
    }
    [TestMethod]
    public void DerivedAndExplicitStatusesMatchAllFrozenBrowserRows()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Stepper.json")));
        var root = document.RootElement;
        Assert.AreEqual("a0cea6e7cc8fcc541d0089682d64ed8589fc4983a482095785477cb01d646c29", root.GetProperty("sourceSha256").GetString());
        var script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CaptureStepper.cjs")).Replace("\r\n", "\n");
        Assert.AreEqual(root.GetProperty("scriptSha256").GetString(), Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(script))));
        var count = 0;
        foreach (var source in root.GetProperty("cases").EnumerateArray())
        {
            var props = source.GetProperty("props");
            var steps = props.GetProperty("steps").EnumerateArray().Select(item => new BleeckerStep(item.GetProperty("id").GetString()!, item.GetProperty("label").GetString()!)
            {
                Status = item.TryGetProperty("status", out var status) ? Enum.Parse<BleeckerStepStatus>(status.GetString()!, true) : null
            }).ToArray();
            var active = props.TryGetProperty("activeStep", out var id) ? id.GetString() : null;
            var rows = source.GetProperty("normal").GetProperty("rows");
            for (var i = 0; i < steps.Length; i++)
            {
                var button = rows[i].GetProperty("button");
                var background = button.GetProperty("backgroundRgba"); var foreground = button.GetProperty("foregroundRgba");
                var expected = button.GetProperty("svg").ValueKind != JsonValueKind.Null ? BleeckerStepStatus.Completed :
                    background[0].GetInt32() is 166 or 212 ? BleeckerStepStatus.Error :
                    foreground[0].GetInt32() is 44 or 91 ? BleeckerStepStatus.Active : BleeckerStepStatus.Pending;
                Assert.AreEqual(expected, BleeckerStepperState.Resolve(steps, i, active), source.GetProperty("id").GetString() + " / " + i);
            }
            count++;
        }
        Assert.AreEqual(44, count);
    }

    [TestMethod]
    public void ConsumerMetadataRemainsObservableWithoutNormalizingIdsOrLabels()
    {
        var step = new BleeckerStep("", " "); var changed = new List<string?>();
        step.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        step.Description = " "; step.Status = BleeckerStepStatus.Error; step.IsDisabled = true;
        step.Status = BleeckerStepStatus.Error;
        CollectionAssert.AreEqual(new[] { "Description", "Status", "IsDisabled" }, changed);
        Assert.AreEqual("", step.Id); Assert.AreEqual(" ", step.Label); Assert.AreEqual(" ", step.Description);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => step.Status = (BleeckerStepStatus)99);
        Assert.AreEqual(BleeckerStepStatus.Error, step.Status);
        Assert.ThrowsExactly<ArgumentNullException>(() => step.Id = null!);
        Assert.ThrowsExactly<ArgumentNullException>(() => step.Label = null!);
    }
}
