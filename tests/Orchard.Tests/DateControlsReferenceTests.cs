using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class DateControlsReferenceTests
{
    [TestMethod]
    public void FrozenDateShadowsRetainSelectedDaysAndRangeHoverOverride()
    {
        using var document = Read(); var root = document.RootElement;
        foreach (var frame in root.GetProperty("cases").EnumerateArray())
        {
            if (frame.TryGetProperty("calendar", out var calendar) && calendar.ValueKind == JsonValueKind.Object)
                foreach (var day in calendar.GetProperty("days").EnumerateArray().Where(day => day.GetProperty("pressed").GetBoolean()))
                    StringAssert.Contains(Paint(root, day).GetProperty("boxShadow").GetString()!, "rgba(21, 48, 66, 0.12) 0px 1px 2px 0px");
            if (frame.GetProperty("kind").GetString() != "range") continue;
            var triggers = frame.GetProperty("triggers").EnumerateArray().ToArray();
            foreach (var (trigger, index) in triggers.Select((trigger, index) => (trigger, index)))
            {
                var shadow = Paint(root, trigger).GetProperty("boxShadow").GetString()!;
                var active = frame.GetProperty("state").GetString() == "open" && index == (frame.GetProperty("id").GetString() == "to-limit" ? 1 : 0);
                Assert.AreEqual(active, shadow.Contains("rgba(26, 55, 77, 0.25) 0px 3px 10px -8px", StringComparison.Ordinal));
                Assert.IsFalse(shadow.Contains("rgba(26, 55, 77, 0.024) 0px 1px 2px", StringComparison.Ordinal));
            }
        }
    }
    [TestMethod]
    public void SelectedPopupsRetainInlineClearActionDescent()
    {
        using var document = Read();
        var frames = document.RootElement.GetProperty("cases").EnumerateArray().Where(frame => frame.TryGetProperty("clearAction", out _)).ToArray();
        Assert.AreEqual(12, frames.Length);
        foreach (var frame in frames)
        {
            var action = frame.GetProperty("clearAction");
            Assert.AreEqual(31d, action.GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(296d, action.GetProperty("parent").GetProperty("height").GetDouble());
            Assert.AreEqual(1d, action.GetProperty("bottomGap").GetDouble());
            Assert.AreEqual(330d, frame.GetProperty("popup").GetProperty("box").GetProperty("height").GetDouble());
        }
    }
    private static JsonDocument Read() => JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "DateControls.json")));
    private static JsonElement[] Cases(JsonElement root, string kind) => root.GetProperty("cases").EnumerateArray().Where(x => x.GetProperty("kind").GetString() == kind).ToArray();
    private static string Digest(string fixture) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture)).Replace("\r\n", "\n"))));
    private static JsonElement Paint(JsonElement root, JsonElement element) => root.GetProperty("paints").GetProperty(element.GetProperty("paintRef").GetString()!);

    [TestMethod]
    public void ReferenceExecutesPinnedComponentsAndLocksCollectorInputs()
    {
        using var document = Read(); var root = document.RootElement;
        Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066", root.GetProperty("sourceCommit").GetString());
        var sources = root.GetProperty("sourceFiles").EnumerateArray().ToDictionary(x => x.GetProperty("path").GetString()!, x => x.GetProperty("sha256").GetString());
        Assert.AreEqual(7, sources.Count);
        Assert.AreEqual("479daea966f3e6432b2527be53ab87f2ec16cda55da7ed9945f6387338e8a6df", sources["src/components/date-picker.tsx"]);
        Assert.AreEqual("3757594c4ecd17bba365b4ce582e4c60d40d8972df2e2149931ac2b965dd48f9", sources["src/components/date-range-picker.tsx"]);
        Assert.AreEqual("045511fc776cb635818eb786f8c7c722de2f167bc1934493617c1ed5ae222c6d", sources["src/components/popover.tsx"]);
        Assert.AreEqual(Digest("CaptureDateControls.cjs"), root.GetProperty("scriptSha256").GetString());
        Assert.AreEqual(Digest("DateControlsClient.tsx"), root.GetProperty("clientSha256").GetString());
        Assert.AreEqual(Digest("DateControlsToolingLock.json"), root.GetProperty("compilerLockSha256").GetString());
        Assert.AreEqual("19.2.4", root.GetProperty("react").GetString());
        Assert.AreEqual("1.1.23", root.GetProperty("radixPopover").GetString());
        Assert.IsTrue(root.GetProperty("endpointMeasurementsOnly").GetBoolean());
        Assert.IsTrue(root.GetProperty("freshMountPerState").GetBoolean());
        Assert.AreEqual(188, root.GetProperty("cases").GetArrayLength());
        Assert.AreEqual(188, root.GetProperty("cases").EnumerateArray().Select(x => x.GetProperty("file").GetString()).Distinct().Count());
        Assert.AreEqual(132, Cases(root, "calendar").Length);
        Assert.AreEqual(28, Cases(root, "picker").Length);
        Assert.AreEqual(28, Cases(root, "range").Length);
        foreach (var frame in root.GetProperty("cases").EnumerateArray())
            Assert.AreEqual("CSS1Compat", frame.GetProperty("viewport").GetProperty("compatMode").GetString());
    }

    [TestMethod]
    public void CalendarRetainsActualMonthGeometryLeapDaysAndSourcePaint()
    {
        using var document = Read(); var root = document.RootElement;
        foreach (var frame in Cases(root, "calendar"))
        {
            var id = frame.GetProperty("id").GetString(); var calendar = frame.GetProperty("calendar");
            var days = calendar.GetProperty("days").EnumerateArray().ToArray();
            Assert.AreEqual(id == "leap" ? 29 : 31, days.Length);
            Assert.AreEqual(id == "six-rows" ? 37 : id == "leap" ? 33 : 35, calendar.GetProperty("cellCount").GetInt32());
            Assert.AreEqual(id == "six-rows" ? 286d : 252d, calendar.GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(frame.GetProperty("width").GetDouble() - 40, calendar.GetProperty("box").GetProperty("width").GetDouble());
            Assert.AreEqual(7, calendar.GetProperty("weekdays").GetArrayLength());
            var pressedDay = frame.GetProperty("state").GetString() == "pressed" ? days.First(x => !x.GetProperty("disabled").GetBoolean()).GetProperty("day").GetInt32() : -1;
            foreach (var day in days)
            {
                Assert.AreEqual(day.GetProperty("day").GetInt32() == pressedDay ? 30.08d : 32d, day.GetProperty("box").GetProperty("height").GetDouble(), .0001);
                var paint = Paint(root, day);
                Assert.AreEqual("13px", paint.GetProperty("fontSize").GetString());
                Assert.AreEqual("6px", paint.GetProperty("borderRadius").GetString());
                if (day.GetProperty("pressed").GetBoolean())
                {
                    Assert.AreEqual("600", paint.GetProperty("fontWeight").GetString());
                    Assert.AreEqual(frame.GetProperty("scheme").GetString() == "Light" ? "rgb(44, 87, 132)" : "rgb(91, 163, 245)", paint.GetProperty("backgroundColor").GetString());
                }
            }
        }
    }

    [TestMethod]
    public void CalendarBoundsCompareTimestampsAndKeepDisabledSelection()
    {
        using var document = Read(); var root = document.RootElement;
        foreach (var frame in Cases(root, "calendar"))
        {
            var id = frame.GetProperty("id").GetString(); var days = frame.GetProperty("calendar").GetProperty("days").EnumerateArray().ToArray();
            foreach (var day in days)
            {
                var number = day.GetProperty("day").GetInt32();
                var disabled = id switch
                {
                    "bounded" => number < 15 || number > 20,
                    "min-noon" => number <= 17,
                    "weekends" => new[] { 3, 4, 10, 11, 17, 18, 24, 25, 31 }.Contains(number),
                    "inverted" => true,
                    _ => false
                };
                Assert.AreEqual(disabled, day.GetProperty("disabled").GetBoolean(), $"{id} day {number}");
                if (disabled) Assert.AreEqual("0.3", Paint(root, day).GetProperty("opacity").GetString());
            }
            if (id is "inverted" or "min-noon" or "weekends")
                Assert.IsTrue(days.Single(x => x.GetProperty("day").GetInt32() == 17).GetProperty("pressed").GetBoolean());
        }
    }

    [TestMethod]
    public void PickerPreservesMeasuredPopupGeometryAndNamedInnerDialog()
    {
        using var document = Read(); var root = document.RootElement;
        foreach (var frame in Cases(root, "picker"))
        {
            var trigger = frame.GetProperty("triggers")[0];
            Assert.AreEqual(40d, trigger.GetProperty("box").GetProperty("height").GetDouble());
            Assert.AreEqual(frame.GetProperty("id").GetString() == "disabled", trigger.GetProperty("disabled").GetBoolean());
            if (frame.GetProperty("state").GetString() != "open") continue;
            Assert.AreEqual(288d, frame.GetProperty("popup").GetProperty("box").GetProperty("width").GetDouble());
            Assert.AreEqual(254d, frame.GetProperty("calendar").GetProperty("box").GetProperty("width").GetDouble());
            var expected = frame.GetProperty("id").GetString() == "long-placeholder" ? "Choose the editorial publication date for this collection" : "Pick a date";
            Assert.IsTrue(frame.GetProperty("dialogs").EnumerateArray().Any(x => x.GetProperty("label").GetString() == expected));
        }
    }

    [TestMethod]
    public void ConstrainedRangeRetainsSourceMinimumWidthAndOverflow()
    {
        using var document = Read();
        foreach (var frame in Cases(document.RootElement, "range").Where(x => x.GetProperty("state").GetString() == "closed"))
        {
            var width = frame.GetProperty("width").GetInt32();
            Assert.AreEqual(width - 40d, frame.GetProperty("root").GetProperty("box").GetProperty("width").GetDouble());
            var cardWidth = frame.GetProperty("rangeCard").GetProperty("box").GetProperty("width").GetDouble();
            Assert.IsTrue(cardWidth >= 360);
            Assert.AreEqual(width == 320, frame.GetProperty("horizontalOverflow").GetBoolean());
            if (width == 320)
            {
                Assert.AreEqual(360d, cardWidth);
                Assert.AreEqual(0d, frame.GetProperty("viewport").GetProperty("scrollX").GetDouble());
                Assert.IsTrue(frame.GetProperty("viewport").GetProperty("scrollWidth").GetDouble() >= 380);
            }
        }
    }

    [TestMethod]
    public void TrustedRequestsPreserveConsumerOwnershipFocusAndLocalDatePresets()
    {
        using var document = Read(); var root = document.RootElement;
        var interactions = root.GetProperty("interactions").EnumerateArray().ToArray();
        Assert.AreEqual(48, interactions.Length);
        Assert.AreEqual(12, interactions.Select(x => x.GetProperty("id").GetString()).Distinct().Count());
        foreach (var interaction in interactions)
        {
            var events = interaction.GetProperty("trustedEvents").EnumerateArray().ToArray();
            Assert.IsTrue(events.Length > 0 && events.All(x => x.GetProperty("trusted").GetBoolean()));
            switch (interaction.GetProperty("id").GetString())
            {
                case "calendar-controlled-and-retained-month":
                    Assert.AreEqual("2026-10-19", interaction.GetProperty("requested").GetString());
                    Assert.AreEqual("October 2026", interaction.GetProperty("viewAfterExternalValue").GetString()); break;
                case "picker-controlled-request": Assert.IsTrue(interaction.GetProperty("closedWithoutConsumerAcceptance").GetBoolean()); break;
                case "picker-escape-and-outside-focus":
                    Assert.AreEqual("Previous month", interaction.GetProperty("initialFocus").GetString());
                    Assert.AreEqual("Publication date", interaction.GetProperty("escapeFocus").GetString());
                    Assert.AreEqual("outside", interaction.GetProperty("outsideFocus").GetString()); break;
                case "range-presets":
                    var presets = interaction.GetProperty("presets");
                    Assert.AreEqual("2026-10-10", presets[0].GetProperty("value").GetProperty("from").GetString());
                    Assert.AreEqual("2026-10-17", presets[0].GetProperty("value").GetProperty("to").GetString());
                    Assert.AreEqual("2026-10-20", presets[2].GetProperty("value").GetProperty("from").GetString()); break;
                case "range-preset-spring-midnight":
                    Assert.AreEqual("2026-03-02", interaction.GetProperty("from").GetString());
                    Assert.AreEqual("2026-03-01", interaction.GetProperty("utcDurationFrom").GetString()); break;
            }
        }
    }
}
