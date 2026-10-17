using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Automation;

// Independent UIA client, restricted to the window, process and ScrollViewer
// supplied by the catalog. It uses the public Control view and Invoke pattern.
internal static class FilterChipUiaProbe
{
    [DataContract] private sealed class Report
    {
        [DataMember(Name="process")] public int Process;
        [DataMember(Name="buttons")] public List<ButtonInfo> Buttons=new List<ButtonInfo>();
        [DataMember(Name="texts")] public List<string> Texts=new List<string>();
        [DataMember(Name="invokeId")] public string InvokeId;
        [DataMember(Name="invokeOutcome")] public string InvokeOutcome="not-requested";
    }
    [DataContract] private sealed class ButtonInfo
    {
        [DataMember(Name="id")] public string Id;
        [DataMember(Name="name")] public string Name;
        [DataMember(Name="className")] public string ClassName;
        [DataMember(Name="controlType")] public string ControlType;
        [DataMember(Name="enabled")] public bool Enabled;
        [DataMember(Name="focusable")] public bool Focusable;
        [DataMember(Name="focused")] public bool Focused;
        [DataMember(Name="offscreen")] public bool Offscreen;
        [DataMember(Name="hasInvoke")] public bool HasInvoke;
        [DataMember(Name="labeledBy")] public string LabeledBy;
        [DataMember(Name="runtimeId")] public int[] RuntimeId;
        [DataMember(Name="bounds")] public Bounds Bounds;
    }
    [DataContract] private sealed class Bounds
    {
        [DataMember(Name="x")] public double X;
        [DataMember(Name="y")] public double Y;
        [DataMember(Name="width")] public double Width;
        [DataMember(Name="height")] public double Height;
    }
    [MTAThread] private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=3&&args.Length!=4)throw new ArgumentException("Pass window, process, output and optional Invoke target ID.");
            var process=int.Parse(args[1]);var root=AutomationElement.FromHandle(new IntPtr(long.Parse(args[0])));
            if(root==null||root.Current.ProcessId!=process)throw new InvalidOperationException("UIA window/process boundary differs.");
            var scope=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"FilterA11y-Scope"));
            if(scope==null||scope.Current.ProcessId!=process)throw new InvalidOperationException("Owned UIA fixture scope is missing.");
            var report=new Report{Process=process,InvokeId=args.Length==4?args[3]:null};
            var buttons=scope.FindAll(TreeScope.Descendants,new AndCondition(Automation.ControlViewCondition,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Button)));
            AutomationElement invokeTarget=null;
            foreach(AutomationElement element in buttons)
            {
                var current=element.Current;if(current.ProcessId!=process)throw new InvalidOperationException("UIA crossed the process boundary.");
                var box=current.BoundingRectangle;var label=current.LabeledBy;
                report.Buttons.Add(new ButtonInfo{Id=current.AutomationId,Name=current.Name,ClassName=current.ClassName,ControlType=current.ControlType.ProgrammaticName,
                    Enabled=current.IsEnabled,Focusable=current.IsKeyboardFocusable,Focused=current.HasKeyboardFocus,Offscreen=current.IsOffscreen,
                    HasInvoke=(bool)element.GetCurrentPropertyValue(AutomationElement.IsInvokePatternAvailableProperty),LabeledBy=label==null?null:label.Current.Name,RuntimeId=element.GetRuntimeId(),
                    Bounds=new Bounds{X=box.X,Y=box.Y,Width=box.Width,Height=box.Height}});
                if(current.AutomationId==report.InvokeId){if(invokeTarget!=null)throw new InvalidOperationException("Invoke target is not unique.");invokeTarget=element;}
            }
            var texts=scope.FindAll(TreeScope.Descendants,new AndCondition(Automation.ControlViewCondition,new PropertyCondition(AutomationElement.ControlTypeProperty,ControlType.Text)));
            foreach(AutomationElement text in texts){if(text.Current.ProcessId!=process)throw new InvalidOperationException("UIA text crossed process boundary.");report.Texts.Add(text.Current.Name);}
            if(report.InvokeId!=null)
            {
                if(invokeTarget==null)throw new InvalidOperationException("Requested Invoke target is absent.");
                object pattern;if(!invokeTarget.TryGetCurrentPattern(InvokePattern.Pattern,out pattern))throw new InvalidOperationException("Native button lost Invoke.");
                try{((InvokePattern)pattern).Invoke();report.InvokeOutcome="invoked";}
                catch(ElementNotEnabledException){report.InvokeOutcome="element-not-enabled";}
            }
            using(var stream=File.Create(args[2]))new DataContractJsonSerializer(typeof(Report)).WriteObject(stream,report);
            return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
    }
}
