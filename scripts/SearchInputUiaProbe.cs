using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Automation;

internal static class SearchInputUiaProbe
{
    [DataContract] private sealed class Report
    {
        [DataMember(Name="process")] public int Process;
        [DataMember(Name="clientProcess")] public int ClientProcess;
        [DataMember(Name="editors")] public List<Element> Editors=new List<Element>();
        [DataMember(Name="buttons")] public List<Element> Buttons=new List<Element>();
    }
    [DataContract] private sealed class Element
    {
        [DataMember(Name="name")] public string Name;
        [DataMember(Name="type")] public string Type;
        [DataMember(Name="enabled")] public bool Enabled;
        [DataMember(Name="focusable")] public bool Focusable;
        [DataMember(Name="hasValue")] public bool HasValue;
        [DataMember(Name="hasText")] public bool HasText;
        [DataMember(Name="hasInvoke")] public bool HasInvoke;
        [DataMember(Name="value")] public string Value;
        [DataMember(Name="readOnly")] public bool ReadOnly;
    }
    [MTAThread] private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=3)throw new ArgumentException("Pass owned window, process and output.");
            var process=int.Parse(args[1]);var root=AutomationElement.FromHandle(new IntPtr(long.Parse(args[0])));
            if(root==null||root.Current.ProcessId!=process)throw new InvalidOperationException("UIA window/process boundary differs.");
            var scope=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"SearchA11y-Scope"));
            if(scope==null||scope.Current.ProcessId!=process)throw new InvalidOperationException("Owned search scope is missing.");
            var report=new Report{Process=process,ClientProcess=System.Diagnostics.Process.GetCurrentProcess().Id};
            foreach(AutomationElement element in scope.FindAll(TreeScope.Descendants,Automation.ControlViewCondition))
            {
                var current=element.Current;if(current.ProcessId!=process)throw new InvalidOperationException("UIA crossed the process boundary.");
                if(current.ControlType!=ControlType.Edit&&current.ControlType!=ControlType.Button)continue;
                var item=new Element{Name=current.Name,Type=current.ControlType.ProgrammaticName,Enabled=current.IsEnabled,Focusable=current.IsKeyboardFocusable,
                    HasValue=(bool)element.GetCurrentPropertyValue(AutomationElement.IsValuePatternAvailableProperty),HasText=(bool)element.GetCurrentPropertyValue(AutomationElement.IsTextPatternAvailableProperty),HasInvoke=(bool)element.GetCurrentPropertyValue(AutomationElement.IsInvokePatternAvailableProperty)};
                if(item.HasValue){var value=(ValuePattern)element.GetCurrentPattern(ValuePattern.Pattern);item.Value=value.Current.Value;item.ReadOnly=value.Current.IsReadOnly;}
                if(current.ControlType==ControlType.Edit)report.Editors.Add(item);else report.Buttons.Add(item);
            }
            using(var stream=File.Create(args[2]))new DataContractJsonSerializer(typeof(Report)).WriteObject(stream,report);return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
