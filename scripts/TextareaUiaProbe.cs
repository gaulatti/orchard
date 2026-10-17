using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Automation;

internal static class TextareaUiaProbe
{
    [DataContract] private sealed class Report
    {
        [DataMember(Name="process")] public int Process;
        [DataMember(Name="clientProcess")] public int ClientProcess;
        [DataMember(Name="name")] public string Name;
        [DataMember(Name="type")] public string Type;
        [DataMember(Name="enabled")] public bool Enabled;
        [DataMember(Name="focusable")] public bool Focusable;
        [DataMember(Name="valid")] public bool? Valid;
        [DataMember(Name="hasText")] public bool HasText;
        [DataMember(Name="hasValue")] public bool HasValue;
        [DataMember(Name="text")] public string Text;
        [DataMember(Name="readOnly")] public bool ReadOnly;
        [DataMember(Name="selectionCount")] public int SelectionCount;
        [DataMember(Name="resizeName")] public string ResizeName;
        [DataMember(Name="resizeType")] public string ResizeType;
        [DataMember(Name="resizeFocusable")] public bool ResizeFocusable;
    }
    [MTAThread] private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=3)throw new ArgumentException("Pass owned window, process and output.");
            var process=int.Parse(args[1]);var root=AutomationElement.FromHandle(new IntPtr(long.Parse(args[0])));
            if(root==null||root.Current.ProcessId!=process)throw new InvalidOperationException("UIA window/process boundary differs.");
            var editor=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"TextareaA11y-Editor"));
            if(editor==null||editor.Current.ProcessId!=process)throw new InvalidOperationException("Owned textarea editor is missing.");
            var current=editor.Current;
            var report=new Report{Process=process,ClientProcess=System.Diagnostics.Process.GetCurrentProcess().Id,Name=current.Name,Type=current.ControlType.ProgrammaticName,Enabled=current.IsEnabled,Focusable=current.IsKeyboardFocusable,
                HasText=(bool)editor.GetCurrentPropertyValue(AutomationElement.IsTextPatternAvailableProperty),HasValue=(bool)editor.GetCurrentPropertyValue(AutomationElement.IsValuePatternAvailableProperty)};
            var validity=AutomationProperty.LookupById(30103); // UIA_IsDataValidForFormPropertyId.
            if(validity!=null)report.Valid=(bool)editor.GetCurrentPropertyValue(validity);
            if(report.HasText){var text=(TextPattern)editor.GetCurrentPattern(TextPattern.Pattern);report.Text=text.DocumentRange.GetText(-1);report.ReadOnly=(bool)text.DocumentRange.GetAttributeValue(TextPattern.IsReadOnlyAttribute);report.SelectionCount=text.GetSelection().Length;}
            var grip=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.NameProperty,"Resize text area"));
            if(grip==null||grip.Current.ProcessId!=process)throw new InvalidOperationException("Owned resize grip is missing from UIA.");
            report.ResizeName=grip.Current.Name;report.ResizeType=grip.Current.ControlType.ProgrammaticName;report.ResizeFocusable=grip.Current.IsKeyboardFocusable;
            using(var stream=File.Create(args[2]))new DataContractJsonSerializer(typeof(Report)).WriteObject(stream,report);return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
