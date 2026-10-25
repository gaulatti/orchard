using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Automation;

internal static class ProgressStackUiaProbe
{
    [DataContract] private sealed class Report
    {
        [DataMember(Name="process")] public int Process;
        [DataMember(Name="clientProcess")] public int ClientProcess;
        [DataMember(Name="group")] public Element Group;
        [DataMember(Name="elements")] public List<Element> Elements = new List<Element>();
        [DataMember(Name="command")] public string Command;
        [DataMember(Name="retained")] public Element Retained;
        [DataMember(Name="retainedOutcome")] public string RetainedOutcome;
        [DataMember(Name="nameEvents")] public string[] NameEvents;
    }
    [DataContract] private sealed class Element
    {
        [DataMember(Name="name")] public string Name;
        [DataMember(Name="type")] public string Type;
        [DataMember(Name="className")] public string ClassName;
        [DataMember(Name="id")] public string Id;
        [DataMember(Name="control")] public bool Control;
        [DataMember(Name="content")] public bool Content;
        [DataMember(Name="focusable")] public bool Focusable;
        [DataMember(Name="hasText")] public bool HasText;
        [DataMember(Name="hasInvoke")] public bool HasInvoke;
        [DataMember(Name="hasRange")] public bool HasRange;
        [DataMember(Name="text")] public string Text;
        [DataMember(Name="runtimeId")] public int[] RuntimeId;
    }
    private static Element Read(AutomationElement element, int process)
    {
        var current = element.Current;
        if (current.ProcessId != process) throw new InvalidOperationException("ProgressStack UIA crossed the owned process boundary.");
        var result = new Element {
            Name=current.Name, Type=current.ControlType.ProgrammaticName, ClassName=current.ClassName,
            Id=current.AutomationId, Control=current.IsControlElement, Content=current.IsContentElement,
            Focusable=current.IsKeyboardFocusable, RuntimeId=element.GetRuntimeId(),
            HasText=(bool)element.GetCurrentPropertyValue(AutomationElement.IsTextPatternAvailableProperty),
            HasInvoke=(bool)element.GetCurrentPropertyValue(AutomationElement.IsInvokePatternAvailableProperty),
            HasRange=(bool)element.GetCurrentPropertyValue(AutomationElement.IsRangeValuePatternAvailableProperty)
        };
        if (result.HasText) result.Text=((TextPattern)element.GetCurrentPattern(TextPattern.Pattern)).DocumentRange.GetText(-1);
        return result;
    }
    [MTAThread] private static int Main(string[] args)
    {
        try {
            if (args.Length != 2 && args.Length != 3) throw new ArgumentException("Pass owned window, process and optional output.");
            var process=int.Parse(args[1]); var root=AutomationElement.FromHandle(new IntPtr(long.Parse(args[0])));
            if (root == null || root.Current.ProcessId != process) throw new InvalidOperationException("UIA window/process boundary differs.");
            var scope=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"ProgressStackA11y-Scope"));
            if (scope == null) throw new InvalidOperationException("Owned ProgressStack scope is missing.");
            if (args.Length == 3) {
                using (var stream=File.Create(args[2])) new DataContractJsonSerializer(typeof(Report)).WriteObject(stream,Snapshot(scope,process));
            } else {
                Console.OutputEncoding=new UTF8Encoding(false);
                var retained=scope.FindFirst(TreeScope.Descendants,new AndCondition(Automation.ControlViewCondition,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Text)));
                if (retained == null) throw new InvalidOperationException("No text to retain.");
                var events=new List<string>(); var gate=new object();
                AutomationPropertyChangedEventHandler handler=delegate(object sender,AutomationPropertyChangedEventArgs change) { lock(gate)events.Add((string)change.NewValue); };
                Automation.AddAutomationPropertyChangedEventHandler(retained,TreeScope.Element,handler,AutomationElement.NameProperty);
                try {
                    var command="ready";
                    do {
                        var report=Snapshot(scope,process); report.Command=command;
                        try {report.Retained=Read(retained,process);report.RetainedOutcome="available";} catch(ElementNotAvailableException) {report.RetainedOutcome="element-not-available";}
                        lock(gate)report.NameEvents=events.ToArray();
                        using(var stream=new MemoryStream()) {new DataContractJsonSerializer(typeof(Report)).WriteObject(stream,report);Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));}
                        if(command=="quit")break;
                        command=Console.ReadLine();
                        if(command!=null&&command!="state"&&command!="quit")throw new InvalidOperationException("Unknown session command.");
                    } while(command!=null);
                } finally {Automation.RemoveAllEventHandlers();}
            }
            return 0;
        } catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static Report Snapshot(AutomationElement scope,int process)
    {
        var report=new Report { Process=process, ClientProcess=System.Diagnostics.Process.GetCurrentProcess().Id, Group=Read(scope,process) };
        foreach(AutomationElement element in scope.FindAll(TreeScope.Descendants,Condition.TrueCondition))report.Elements.Add(Read(element,process));
        return report;
    }
}
