using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Tests;

[TestClass]
public sealed class AlertDialogReferenceTests
{
    private static string Fixture(string file)=>Path.Combine(AppContext.BaseDirectory,"Fixtures",file);
    private static JsonDocument Reference()=>JsonDocument.Parse(File.ReadAllBytes(Fixture("AlertDialog.json")));
    private static double N(JsonElement row,string name,double fallback=0)=>row.TryGetProperty(name,out var value)?value.GetDouble():fallback;
    private static bool F(JsonElement row,string name)=>row.TryGetProperty(name,out var value)&&value.GetBoolean();
    private static void EqualBox(JsonElement expected,BleeckerAlertDialogModel.Rect actual,string message,double tolerance=.0001)
    {
        Assert.AreEqual(N(expected,"x"),actual.X,tolerance,message+".x");Assert.AreEqual(N(expected,"y"),actual.Y,tolerance,message+".y");Assert.AreEqual(N(expected,"width"),actual.Width,tolerance,message+".width");Assert.AreEqual(N(expected,"height"),actual.Height,tolerance,message+".height");
    }
    [TestMethod]
    public void FrozenSourceBindsOriginalCodeToolingStylesAndAllUnmodifiedImages()
    {
        using var d=Reference();var r=d.RootElement;Assert.AreEqual("orchard-alert-dialog-reference-v1",r.GetProperty("format").GetString());Assert.AreEqual("a86059013cd59684ce452849278ea81e734f2066",r.GetProperty("commit").GetString());Assert.AreEqual("5a7a05a0306b59817b11e7831ed7d446d1739e2c02362f40d491bfff1297fcaf",r.GetProperty("sourceSha256").GetString());
        Assert.AreEqual(56,r.GetProperty("cases").GetArrayLength());Assert.AreEqual(36,r.GetProperty("interactions").GetArrayLength());Assert.AreEqual(122,r.GetProperty("cssSourceInputs").GetArrayLength());Assert.AreEqual(8,r.GetProperty("localBundleInputs").GetArrayLength());Assert.AreEqual("1.62.1",r.GetProperty("playwrightVersion").GetString());
        foreach(var (file,field)in new[]{("CaptureAlertDialog.cjs","scriptSha256"),("AlertDialogClient.tsx","clientSha256")})Assert.AreEqual(r.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
        foreach(var row in r.GetProperty("cases").EnumerateArray()){var bytes=File.ReadAllBytes(Fixture("AlertDialog/"+row.GetProperty("file").GetString()));Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(bytes)));var spec=row.GetProperty("spec");Assert.AreEqual(N(spec,"viewportWidth",800),BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4)));Assert.AreEqual(N(spec,"viewportHeight",600),BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4)));}
        CollectionAssert.AreEqual(new[]{"--hide-scrollbars"},r.GetProperty("ignoredDefaultArgs").EnumerateArray().Select(x=>x.GetString()!).ToArray());
        Assert.AreEqual("original-input-ownership-focus-and-presence",r.GetProperty("interactionScope").GetString());
    }
    [TestMethod]
    public void LayoutModelMatchesMeasuredTextAndButtonProducersAcrossAllViewportRecipes()
    {
        using var d=Reference();foreach(var row in d.RootElement.GetProperty("cases").EnumerateArray())
        {
            var spec=row.GetProperty("spec");var m=row.GetProperty("measurement");var buttons=m.GetProperty("buttons");var description=m.GetProperty("description");var hasDescription=description.ValueKind!=JsonValueKind.Null;var titleHeight=N(m.GetProperty("title"),"height");var descriptionHeight=hasDescription?N(description,"height"):0;var lastWidth=0d;
            var actual=BleeckerAlertDialogModel.Measure(N(spec,"viewportWidth",800),N(spec,"viewportHeight",600),width=>{lastWidth=width;return new(titleHeight,descriptionHeight);},N(buttons[0].GetProperty("box"),"width"),N(buttons[1].GetProperty("box"),"width"),hasDescription,F(spec,"rtl"),F(spec,"custom")?512:448);var id=row.GetProperty("scheme").GetString()+"/"+row.GetProperty("id").GetString();
            foreach(var (name,box)in new[]{("surface",actual.Surface),("inner",actual.Inner),("header",actual.Header),("icon",actual.Icon),("iconSvg",actual.IconSvg),("title",actual.Title),("footer",actual.Footer)})EqualBox(m.GetProperty(name),box,id+"/"+name);
            if(hasDescription)EqualBox(description,actual.Description!.Value,id+"/description");else Assert.IsNull(actual.Description);
            EqualBox(buttons[0].GetProperty("box"),actual.Cancel,id+"/cancel");EqualBox(buttons[1].GetProperty("box"),actual.Confirm,id+"/confirm");Assert.AreEqual(actual.Title.Width,lastWidth,.000001);
            var client=m.GetProperty("client");var scroll=m.GetProperty("scroll");Assert.AreEqual(N(client,"width"),Math.Floor(actual.ViewportWidth+.5),.000001,id);Assert.AreEqual(N(client,"height"),Math.Floor(actual.ViewportHeight+.5),.000001,id);Assert.AreEqual(N(scroll,"width"),Math.Floor(actual.ExtentWidth+.5),.000001,id);Assert.AreEqual(N(scroll,"height"),Math.Floor(actual.ExtentHeight+.5),.000001,id);
            Assert.AreEqual(m.GetProperty("paint").GetProperty("inner").GetProperty("padding")[0].GetDouble(),actual.Padding);Assert.IsTrue(F(m,"labelledByMatches")&&F(m,"describedByMatches"));
        }
    }
    [TestMethod]
    public void ConsumerOwnsClosureAndLoadingRejectsAllOriginalUserCancellationPaths()
    {
        using var d=Reference();foreach(var scheme in new[]{"Light","Dark"})
        {
            var rows=d.RootElement.GetProperty("interactions").EnumerateArray().Where(x=>x.GetProperty("scheme").GetString()==scheme).ToDictionary(x=>x.GetProperty("id").GetString()!);
            foreach(var loading in new[]{false,true})foreach(var accept in new[]{false,true})
            {
                var state=new BleeckerAlertDialogPresence();state.SetOpen(true,0);Assert.AreEqual(!loading,state.CanRequestCancel(loading));var suffix=(loading?"loading":"idle")+"-"+(accept?"accepted":"controlled");var history=rows["keyboard-"+suffix].GetProperty("samples");var first=history[0].GetProperty("measurement");Assert.AreEqual(loading?"surface":"button-0",first.GetProperty("focused").GetString());var end=history[5].GetProperty("measurement");Assert.AreEqual(loading||!accept,F(end,"present"));Assert.AreEqual(loading?0:1,end.GetProperty("events").GetArrayLength());var backdrop=rows["backdrop-"+suffix].GetProperty("settled");Assert.AreEqual(loading||!accept,F(backdrop,"present"));Assert.AreEqual(loading?0:1,backdrop.GetProperty("events").GetArrayLength());
            }
            foreach(var action in new[]{"confirm","cancel"})foreach(var accept in new[]{false,true}){var row=rows[action+"-"+(accept?"accepted":"controlled")].GetProperty("settled");Assert.AreEqual(!accept,F(row,"present"));Assert.AreEqual(action,row.GetProperty("events")[0].GetString());}
            Assert.IsFalse(F(rows["loading-caller-driven-close"].GetProperty("settled"),"present"));Assert.AreEqual(0,rows["loading-native-button-clicks-rejected"].GetProperty("after").GetProperty("events").GetArrayLength());
        }
    }
    [TestMethod]
    public void PresenceModelMatchesDelayedEntryExitReopenAndDisposalInBothMotionPreferences()
    {
        using var d=Reference();Assert.AreEqual(4,d.RootElement.GetProperty("presence").GetArrayLength());foreach(var row in d.RootElement.GetProperty("presence").EnumerateArray())
        {
            var samples=row.GetProperty("samples");var state=new BleeckerAlertDialogPresence();state.SetOpen(true,0);
            void Sample(int index,double time){state.Advance(time);var s=samples[index];Assert.AreEqual(F(s,"present"),state.Present);if(state.Present)Assert.AreEqual(s.GetProperty("state").GetString()=="open",state.Visible);Assert.AreEqual(state.DesiredOpen,F(s,"rootInert"));Assert.AreEqual(state.DesiredOpen?"hidden":"",s.GetProperty("bodyOverflow").GetString());}
            Sample(0,0);Sample(1,19);Sample(2,20);state.SetOpen(false,20);Sample(3,20);Sample(4,169);Sample(5,170);state.SetOpen(true,170);state.Advance(190);state.SetOpen(false,190);state.Advance(265);state.SetOpen(true,265);Sample(6,265);Sample(7,285);Sample(8,435);state.SetOpen(false,435);state.Advance(585);state.SetOpen(true,585);state.Dispose();Sample(9,835);Assert.ThrowsExactly<ObjectDisposedException>(()=>state.SetOpen(true,835));
        }
    }
    [TestMethod]
    public void EndpointMotionMatchesIndependentSurfaceAndBackdropCurvesAtEveryControlledSample()
    {
        using var d=Reference();var r=d.RootElement;Assert.AreEqual(4,r.GetProperty("transitions").GetArrayLength());foreach(var row in r.GetProperty("transitions").EnumerateArray())
        {
            var entering=row.GetProperty("kind").GetString()=="enter";var baseBox=r.GetProperty("cases").EnumerateArray().First(x=>x.GetProperty("scheme").GetString()==row.GetProperty("scheme").GetString()&&x.GetProperty("id").GetString()=="default-idle-ltr").GetProperty("measurement").GetProperty("surface");var logical=new BleeckerAlertDialogModel.Rect(N(baseBox,"x"),N(baseBox,"y"),N(baseBox,"width"),N(baseBox,"height"));
            foreach(var sample in row.GetProperty("samples").EnumerateArray()){var actual=BleeckerAlertDialogModel.SampleEndpointTransition(entering,N(sample,"milliseconds"));Assert.AreEqual(N(sample,"opacity"),actual.Opacity,.000001);Assert.AreEqual(N(sample,"backdropOpacity"),actual.BackdropOpacity,.000001);EqualBox(sample.GetProperty("surface"),BleeckerAlertDialogModel.Transform(logical,actual),"controlled-motion");}
            var reduced=BleeckerAlertDialogModel.SampleEndpointTransition(entering,.01,true);Assert.AreEqual(entering?1d:0,reduced.Opacity);
        }
    }
    [TestMethod]
    public void OriginalModalAncestryAndDynamicBackgroundStateAreRetainedAcrossTeardown()
    {
        using var d=Reference();foreach(var scheme in new[]{"Light","Dark"})
        {
            var rows=d.RootElement.GetProperty("interactions").EnumerateArray().Where(x=>x.GetProperty("scheme").GetString()==scheme).ToDictionary(x=>x.GetProperty("id").GetString()!);var nested=rows["nested-original-modal-drawer-and-popup"].GetProperty("samples");var layers=nested[0].GetProperty("layers");Assert.AreEqual(3,layers.GetArrayLength());Assert.IsTrue(F(layers[0],"inert")&&F(layers[1],"inert"));Assert.IsFalse(F(layers[2],"inert"));Assert.AreEqual("52",layers[2].GetProperty("zIndex").GetString());Assert.AreEqual("alert-opener",nested[1].GetProperty("focused").GetString());Assert.AreEqual("branch-first",nested[2].GetProperty("focused").GetString());Assert.AreEqual("branch-opener",nested[3].GetProperty("focused").GetString());Assert.AreEqual("opener",nested[6].GetProperty("focused").GetString());Assert.AreEqual(0,nested[6].GetProperty("layers").GetArrayLength());
            var simultaneous=rows["simultaneous-original-modal-ancestry"].GetProperty("samples");Assert.AreEqual("Cancel",simultaneous[0].GetProperty("focused").GetString());Assert.IsFalse(F(simultaneous[0].GetProperty("layers").EnumerateArray().Single(x=>x.GetProperty("role").GetString()=="alertdialog"),"inert"));Assert.AreEqual(0,simultaneous[3].GetProperty("layers").GetArrayLength());var ancestor=rows["ancestor-closes-before-live-child"].GetProperty("samples");Assert.AreEqual("alertdialog",ancestor[1].GetProperty("layers").EnumerateArray().Last(x=>!F(x,"inert")).GetProperty("role").GetString());Assert.AreEqual(0,ancestor[2].GetProperty("layers").GetArrayLength());
            var restored=rows["original-and-dynamic-background-unmount-restoration"];Assert.IsTrue(F(restored.GetProperty("before"),"preexisting")&&F(restored.GetProperty("before"),"dynamic"));var after=restored.GetProperty("after");Assert.IsTrue(F(after,"preexisting"));Assert.IsFalse(F(after,"dynamic"));Assert.IsFalse(F(after,"rootInert"));Assert.AreEqual("clip",after.GetProperty("overflow").GetString());Assert.AreEqual(0,after.GetProperty("modals").GetInt32());
        }
    }
}
