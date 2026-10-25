using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

// Read current UIA3 properties directly. The older managed UIA client does not
// register IsDataValidForForm/DescribedBy, so those properties must not be skipped.
internal static class FieldUiaProbe
{
    [ComImport, Guid("352ffba8-0973-437c-a61f-f64cafd81df9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface Condition { }
    [ComImport, Guid("14314595-b4bc-4055-95f2-58f2e42c9855"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ElementArray
    {
        int Length { get; }
        Element GetElement(int index);
    }
    [ComImport, Guid("d22108aa-8ac5-49a5-837b-37bbb3d7591e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface Element
    {
        void SetFocus();
        [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[] GetRuntimeId();
        Element FindFirst(int scope, Condition condition);
        [PreserveSig] int FindAll(); [PreserveSig] int FindFirstBuildCache(); [PreserveSig] int FindAllBuildCache(); [PreserveSig] int BuildUpdatedCache();
        [return: MarshalAs(UnmanagedType.Struct)] object GetCurrentPropertyValue(int property);
        [return: MarshalAs(UnmanagedType.Struct)] object GetCurrentPropertyValueEx(int property, [MarshalAs(UnmanagedType.Bool)] bool ignoreDefault);
    }
    [ComImport, Guid("30cbe57d-d9d0-452a-ab13-7ac5ac4825ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface Automation
    {
        [PreserveSig] int CompareElements(); [PreserveSig] int CompareRuntimeIds(); [PreserveSig] int GetRootElement();
        Element ElementFromHandle(IntPtr window);
        [PreserveSig] int ElementFromPoint(); [PreserveSig] int GetFocusedElement(); [PreserveSig] int GetRootElementBuildCache();
        [PreserveSig] int ElementFromHandleBuildCache(); [PreserveSig] int ElementFromPointBuildCache(); [PreserveSig] int GetFocusedElementBuildCache();
        [PreserveSig] int CreateTreeWalker(); [PreserveSig] int ControlViewWalker(); [PreserveSig] int ContentViewWalker(); [PreserveSig] int RawViewWalker();
        [PreserveSig] int RawViewCondition(); [PreserveSig] int ControlViewCondition(); [PreserveSig] int ContentViewCondition();
        [PreserveSig] int CreateCacheRequest(); [PreserveSig] int CreateTrueCondition(); [PreserveSig] int CreateFalseCondition();
        Condition CreatePropertyCondition(int property, [MarshalAs(UnmanagedType.Struct)] object value);
    }
    [DataContract] private sealed class Reference
    {
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "id")] public string Id;
    }
    [DataContract] private sealed class Report
    {
        [DataMember(Name = "process")] public int Process;
        [DataMember(Name = "clientProcess")] public int ClientProcess;
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "id")] public string Id;
        [DataMember(Name = "enabled")] public bool Enabled;
        [DataMember(Name = "valid")] public bool Valid;
        [DataMember(Name = "required")] public bool Required;
        [DataMember(Name = "labeledBy")] public Reference LabeledBy;
        [DataMember(Name = "describedBy")] public List<Reference> DescribedBy = new List<Reference>();
        [DataMember(Name = "runtimeId")] public int[] RuntimeId;
    }
    private static Reference ReadReference(Element element, int process)
    {
        if ((int)element.GetCurrentPropertyValue(30002) != process) throw new InvalidOperationException("Field UIA reference crossed the owned process boundary.");
        return new Reference {Name = (string)element.GetCurrentPropertyValue(30005), Id = (string)element.GetCurrentPropertyValue(30011)};
    }
    [MTAThread] private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3) throw new ArgumentException("Pass owned window, process and output.");
            var process = int.Parse(args[1]);
            var automation = (Automation)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("ff48dba4-60ef-4201-aa87-54103eef594e")));
            var root = automation.ElementFromHandle(new IntPtr(long.Parse(args[0])));
            if ((int)root.GetCurrentPropertyValue(30002) != process) throw new InvalidOperationException("Field UIA window/process boundary differs.");
            var editor = root.FindFirst(4, automation.CreatePropertyCondition(30003, 50004));
            if (editor == null || (int)editor.GetCurrentPropertyValue(30002) != process) throw new InvalidOperationException("Owned Field editor is missing.");
            var valid = editor.GetCurrentPropertyValueEx(30103, true);
            if (!(valid is bool)) throw new InvalidOperationException("Field editor does not expose IsDataValidForForm.");
            var current = ReadReference(editor, process);
            var result = new Report {Process = process, ClientProcess = System.Diagnostics.Process.GetCurrentProcess().Id, Name = current.Name, Id = current.Id,
                Enabled = (bool)editor.GetCurrentPropertyValue(30010), Valid = (bool)valid, Required = (bool)editor.GetCurrentPropertyValue(30025), RuntimeId = editor.GetRuntimeId()};
            var label = editor.GetCurrentPropertyValue(30018) as Element;
            if (label != null) result.LabeledBy = ReadReference(label, process);
            var descriptions = editor.GetCurrentPropertyValue(30105);
            if (descriptions != null)
            {
                var array = descriptions as ElementArray;
                if (array == null) throw new InvalidOperationException("Field DescribedBy did not return IUIAutomationElementArray.");
                for (var index = 0; index < array.Length; index++) result.DescribedBy.Add(ReadReference(array.GetElement(index), process));
            }
            using (var stream = File.Create(args[2])) new DataContractJsonSerializer(typeof(Report)).WriteObject(stream, result);
            return 0;
        }
        catch (Exception error) {Console.Error.WriteLine(error); return 1;}
    }
}
