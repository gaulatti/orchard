using Orchard.Catalog;
using System.Globalization;

namespace Orchard.Tests;

[TestClass]
public sealed class DataTableConsumerTests
{
    [TestMethod]
    public void OriginalColumnWritesUpdateOriginalRowsAndResetPreservesGlobalState()
    {
        var table=new ToolbarFilterTable();var title=table.GetColumn("title")!;title.SetFilterValue("Reed");
        Assert.AreEqual("Reed",table.VisibleText);Assert.AreSame(ToolbarFilterTable.OriginalRows[1],table.VisibleRows.Single());Assert.AreSame(title,table.Writes.Single().Column);
        table.SetInitial(new[]{new KeyValuePair<string,object?>("title","Harbor")});table.SetGlobal("Harbor");table.ResetColumnFilters();
        Assert.AreEqual("Harbor",title.GetFilterValue());Assert.AreEqual("Harbor",table.GlobalFilter);Assert.AreEqual("Harbor",table.VisibleText);Assert.AreSame(ToolbarFilterTable.OriginalRows[0],table.VisibleRows.Single());Assert.IsNull(table.GetColumn("absent"));
    }
    [TestMethod]
    public void SourceTruthinessAndDomStringsRemainIndependentOfNativeCulture()
    {
        foreach(var value in new object?[]{null,false,0,-0d,double.NaN,""})Assert.IsFalse(BleeckerDataTableValue.Truthy(value));
        foreach(var value in new object[]{" ","0",1,true,double.PositiveInfinity,Array.Empty<object>(),new Dictionary<string,object?>()})Assert.IsTrue(BleeckerDataTableValue.Truthy(value));
        var culture=CultureInfo.CurrentCulture;try
        {
            CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("pt-BR");
            foreach(var(value,text)in new(object?,string)[]{(null,""),(false,"false"),(42,"42"),(0,"0"),(-0d,"0"),(double.NaN,"NaN"),(double.PositiveInfinity,"Infinity"),(new[]{"Harbor","Reed"},"Harbor,Reed"),(new Dictionary<string,object?>{{"query","Harbor"}},"[object Object]"),(1.25,"1.25"),(1e-6,"0.000001"),(1e-7,"1e-7"),(1e21,"1e+21")})Assert.AreEqual(text,BleeckerDataTableValue.InputText(value));
        }finally{CultureInfo.CurrentCulture=culture;}
    }
    [TestMethod]
    public void NestedFilterArraysRetainSharedBranchesAndRejectCycles()
    {
        var branch=new object?[]{"Harbor",null,42};
        var values=new object?[]{branch,branch,new object?[]{false,new[]{"Reed","Bay"}}};
        Assert.AreEqual("Harbor,,42,Harbor,,42,false,Reed,Bay",BleeckerDataTableValue.InputText(values));
        var cyclic=new List<object?>();cyclic.Add(new object?[]{"Harbor",cyclic});
        Assert.Throws<ArgumentException>(()=>BleeckerDataTableValue.InputText(cyclic));
        cyclic.Clear();cyclic.Add(branch);
        Assert.AreEqual("Harbor,,42",BleeckerDataTableValue.InputText(cyclic));
    }
}
