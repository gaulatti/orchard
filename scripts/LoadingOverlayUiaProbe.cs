using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Automation;

// Separate framework UIA client. It queries only the catalog window/process
// explicitly supplied by the catalog smoke harness.
internal static class LoadingOverlayUiaProbe
{
    [DataContract] private sealed class Report
    {
        [DataMember(Name = "process")] public int Process;
        [DataMember(Name = "progress")] public List<Progress> Progress = new List<Progress>();
    }
    [DataContract] private sealed class Progress
    {
        [DataMember(Name = "name")] public string Name;
        [DataMember(Name = "className")] public string ClassName;
        [DataMember(Name = "controlType")] public string ControlType;
        [DataMember(Name = "isKeyboardFocusable")] public bool IsKeyboardFocusable;
        [DataMember(Name = "isOffscreen")] public bool IsOffscreen;
        [DataMember(Name = "hasRangeValuePattern")] public bool HasRangeValuePattern;
        [DataMember(Name = "bounds")] public Bounds Bounds;
    }
    [DataContract] private sealed class Bounds
    {
        [DataMember(Name = "x")] public double X;
        [DataMember(Name = "y")] public double Y;
        [DataMember(Name = "width")] public double Width;
        [DataMember(Name = "height")] public double Height;
    }
    [MTAThread] private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 3) throw new ArgumentException("Pass window, expected process and output file.");
            var process = int.Parse(args[1]); var root = AutomationElement.FromHandle(new IntPtr(long.Parse(args[0])));
            if (root == null || root.Current.ProcessId != process) throw new InvalidOperationException("UIA did not resolve its own catalog window.");
            var report = new Report { Process = process };
            var elements = root.FindAll(TreeScope.Descendants, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.ProgressBar));
            foreach (AutomationElement element in elements)
            {
                var current = element.Current; if (current.ProcessId != process) throw new InvalidOperationException("UIA crossed the catalog process boundary.");
                var box = current.BoundingRectangle;
                report.Progress.Add(new Progress { Name = current.Name, ClassName = current.ClassName, ControlType = current.ControlType.ProgrammaticName,
                    IsKeyboardFocusable = current.IsKeyboardFocusable, IsOffscreen = current.IsOffscreen,
                    HasRangeValuePattern = (bool)element.GetCurrentPropertyValue(AutomationElement.IsRangeValuePatternAvailableProperty),
                    Bounds = new Bounds { X = box.X, Y = box.Y, Width = box.Width, Height = box.Height } });
            }
            using (var stream = File.Create(args[2])) new DataContractJsonSerializer(typeof(Report)).WriteObject(stream, report);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
