using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
namespace Orchard.Tests;
[TestClass]
public sealed class ProgressStackReferenceTests
{
 private static string Fixture(string name)=>Path.Combine(AppContext.BaseDirectory,"Fixtures",name);
 [TestMethod]
 public void SourceProgressStackAccessibleNamesRetainLogicalUnicodeAcrossVisualLineBreaks()
 {
  using var accessibility=JsonDocument.Parse(File.ReadAllBytes(Fixture("ProgressStackAccessibility.json")));
  using var text=JsonDocument.Parse(File.ReadAllBytes(Fixture("ProgressStackText.json")));
  var root=accessibility.RootElement;
  Assert.AreEqual("orchard-progress-stack-accessibility-reference-v1",root.GetProperty("format").GetString());
  Assert.AreEqual(text.RootElement.GetProperty("sourceSha256").GetString(),root.GetProperty("sourceSha256").GetString());
  foreach(var(file,field)in new[]{("CaptureProgressStackAccessibility.cjs","scriptSha256"),("ProgressStackText.json","textReferenceSha256"),("ProgressStackClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
  var cases=root.GetProperty("cases").EnumerateArray().ToArray();Assert.AreEqual(36,cases.Length);
  foreach(var row in cases)
  {
   var source=text.RootElement.GetProperty("cases").EnumerateArray().Single(source=>source.GetProperty("id").GetString()==row.GetProperty("id").GetString()&&source.GetProperty("scheme").GetString()==row.GetProperty("scheme").GetString()&&source.GetProperty("motion").GetString()=="reduce");
   var expected=source.GetProperty("spec").GetProperty("items").EnumerateArray().SelectMany(item=>new[]{item.GetProperty("label").GetString(),item.GetProperty("value").GetString()}).ToArray();
   CollectionAssert.AreEqual(expected,row.GetProperty("texts").EnumerateArray().Select(value=>value.GetString()).ToArray());
   Assert.AreEqual(source.GetProperty("pngSha256").GetString(),row.GetProperty("sourcePngSha256").GetString());
  }
  foreach(var scheme in new[]{"Light","Dark"})
  {
   var row=cases.Single(row=>row.GetProperty("scheme").GetString()==scheme&&row.GetProperty("id").GetString()=="soft-hyphen");
   Assert.AreEqual("publi\u00adcation\u00adworkflow",row.GetProperty("texts")[0].GetString());
  }
 }
 [TestMethod]
 public void SourceUnicodeProgressStackRetainsEveryRasterAndShapingReceipt()
 {
  using var document=JsonDocument.Parse(File.ReadAllBytes(Fixture("ProgressStackText.json")));var root=document.RootElement;
  Assert.AreEqual("101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254",root.GetProperty("sourceSha256").GetString());
  Assert.AreEqual(72,root.GetProperty("cases").GetArrayLength());Assert.AreEqual(16,root.GetProperty("updates").GetArrayLength());
  foreach(var(file,field)in new[]{("CaptureProgressStackText.cjs","scriptSha256"),("ProgressStackClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
  foreach(var row in root.GetProperty("cases").EnumerateArray())
  {
   Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(Path.Combine("ProgressStackText",row.GetProperty("file").GetString()!))))));
   Assert.AreEqual(4,row.GetProperty("platformFonts").GetArrayLength());
   foreach(var item in row.GetProperty("measurement").GetProperty("items").EnumerateArray())foreach(var role in new[]{"label","value"})
   {
    var text=item.GetProperty(role);Assert.IsGreaterThan(0,text.GetProperty("graphemes").GetArrayLength());
    Assert.IsLessThanOrEqualTo(text.GetProperty("intrinsic").GetProperty("maximum").GetDouble(),text.GetProperty("intrinsic").GetProperty("minimum").GetDouble());
   }
  }
  Assert.AreEqual("155.0.4283.62",root.GetProperty("browser").GetString());Assert.AreEqual(2,root.GetProperty("fonts").GetArrayLength());
 }
 [TestMethod]
 public void SourceProgressStackRetainsCollectorFontsAndCompleteRasterProvenance()
 {
  using var document=JsonDocument.Parse(File.ReadAllBytes(Fixture("ProgressStack.json")));var root=document.RootElement;
  Assert.AreEqual("101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254",root.GetProperty("sourceSha256").GetString());Assert.AreEqual(140,root.GetProperty("cases").GetArrayLength());Assert.AreEqual(16,root.GetProperty("updates").GetArrayLength());
  foreach(var(file,field)in new[]{("CaptureProgressStack.cjs","scriptSha256"),("ProgressStackClient.tsx","clientSha256")})Assert.AreEqual(root.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
  foreach(var row in root.GetProperty("cases").EnumerateArray())Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(Path.Combine("ProgressStack",row.GetProperty("file").GetString()!))))));
  Assert.AreEqual("155.0.4283.62",root.GetProperty("browser").GetString());Assert.AreEqual(2,root.GetProperty("fonts").GetArrayLength());
 }
 [TestMethod]
 public void SourceProgressStackPreservesFlexShrinkingAndNonfiniteNumericSemantics()
 {
  using var document=JsonDocument.Parse(File.ReadAllBytes(Fixture("ProgressStack.json")));
  foreach(var scheme in new[]{"Light","Dark"})foreach(var motion in new[]{"reduce","no-preference"})
  {
   var rows=document.RootElement.GetProperty("cases").EnumerateArray().Where(row=>row.GetProperty("scheme").GetString()==scheme&&row.GetProperty("motion").GetString()==motion).ToDictionary(row=>row.GetProperty("id").GetString()!,row=>row.GetProperty("measurement"));Assert.AreEqual(35,rows.Count);
   var expected=rows["default"].GetProperty("segments").EnumerateArray().Select(row=>row.GetProperty("box").GetProperty("width").GetDouble()).ToArray();CollectionAssert.AreEqual(new[]{168d,84d,28d},expected);
   CollectionAssert.AreEqual(expected,rows["max-5"].GetProperty("segments").EnumerateArray().Select(row=>row.GetProperty("box").GetProperty("width").GetDouble()).ToArray());
   foreach(var id in new[]{"max-0","max--1","max-NaN","max-Infinity","max--Infinity","zeros","negative-total","value-NaN","value-Infinity","value--Infinity"})foreach(var item in rows[id].GetProperty("segments").EnumerateArray())Assert.AreEqual(0d,item.GetProperty("box").GetProperty("width").GetDouble());
   Assert.AreEqual("Published: 0",rows["value--0"].GetProperty("segments")[0].GetProperty("title").GetString());Assert.AreEqual("Published: 1e+21",rows["value-1e+21"].GetProperty("segments")[0].GetProperty("title").GetString());
   Assert.AreEqual("\u00a0Published\u00a0",rows["nonbreaking-space"].GetProperty("items")[0].GetProperty("label").GetProperty("text").GetString());Assert.AreEqual(1,rows["single"].GetProperty("segments").GetArrayLength());Assert.AreEqual(280d,rows["single"].GetProperty("segments")[0].GetProperty("box").GetProperty("width").GetDouble());
   Assert.AreEqual(24d,rows["empty"].GetProperty("root").GetProperty("box").GetProperty("height").GetDouble());Assert.AreEqual(12d,rows["hidden"].GetProperty("root").GetProperty("box").GetProperty("height").GetDouble());
   Assert.AreEqual(0d,rows["width-0"].GetProperty("root").GetProperty("box").GetProperty("width").GetDouble());Assert.AreEqual(96d,rows["width-0"].GetProperty("root").GetProperty("box").GetProperty("height").GetDouble());Assert.IsGreaterThan(0d,rows["width-0"].GetProperty("items")[0].GetProperty("box").GetProperty("width").GetDouble());
   foreach(var row in rows.Values){Assert.AreEqual(0,row.GetProperty("animations").GetInt32());Assert.AreEqual(-1,row.GetProperty("tabIndex").GetInt32());}
  }
 }
 [TestMethod]
 public void ProgressStackTrackPaletteRetainsSourceOpacityAndPlatformContrastColors()
 {
  XNamespace ns="http://schemas.microsoft.com/winfx/2006/xaml/presentation",x="http://schemas.microsoft.com/winfx/2006/xaml";var palette=XDocument.Load(Fixture("Library/Themes/ProgressStackPalette.g.xaml"));
  foreach(var theme in new[]{"Light","Default","HighContrast"}){var brush=palette.Descendants(ns+"ResourceDictionary").Single(element=>(string?)element.Attribute(x+"Key")==theme).Element(ns+"SolidColorBrush")!;Assert.AreEqual("1",(string?)brush.Attribute("Opacity"));if(theme=="HighContrast")StringAssert.StartsWith((string)brush.Attribute("Color")!,"{ThemeResource SystemColor");else Assert.AreEqual(theme=="Light"?"#33E6D5B8":"#331A2332",(string?)brush.Attribute("Color"));}
 }
 [TestMethod]
 public void TrackPaintProbeRetainsFrozenCssAndCompleteSquarePngs()
 {
  using var document=JsonDocument.Parse(File.ReadAllBytes(Fixture("ProgressStackPaint.json")));var root=document.RootElement;
  Assert.AreEqual("101df66952162d978a45935604e468e7b465e8a57afdf873a48e0867a46b6254",root.GetProperty("sourceSha256").GetString());
  foreach(var(file,field)in new[]{("ProgressStack.json","trackReferenceSha256"),("CaptureProgressStackPaint.cjs","scriptSha256")})Assert.AreEqual(root.GetProperty(field).GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(file)))));
  Assert.AreEqual(10,root.GetProperty("cases").GetArrayLength());
  foreach(var row in root.GetProperty("cases").EnumerateArray()){Assert.AreEqual(40,row.GetProperty("width").GetInt32());Assert.AreEqual(40,row.GetProperty("height").GetInt32());Assert.AreEqual(640,row.GetProperty("viewportWidth").GetInt32());Assert.AreEqual(400,row.GetProperty("viewportHeight").GetInt32());Assert.AreEqual(row.GetProperty("pngSha256").GetString(),Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Fixture(Path.Combine("ProgressStackPaint",row.GetProperty("file").GetString()!))))));}
 }
}
