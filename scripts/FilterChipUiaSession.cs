using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Automation;

// All registration, requests and cleanup run on this external MTA thread.
// Retained InvokePattern objects are never replaced by a fresh lookup.
internal static class FilterChipUiaSession
{
    private sealed class Retained
    {internal AutomationElement Element;internal InvokePattern Invoke;internal int[] RuntimeId;internal AutomationPropertyChangedEventHandler Handler;}
    [DataContract] private sealed class EventInfo
    {
        [DataMember(Name="sequence")] public int Sequence;
        [DataMember(Name="slot")] public string Slot;
        [DataMember(Name="property")] public string Property;
        [DataMember(Name="value")] public string Value;
    }
    [DataContract] private sealed class Response
    {
        [DataMember(Name="process")] public int Process;
        [DataMember(Name="clientProcess")] public int ClientProcess;
        [DataMember(Name="command")] public string Command;
        [DataMember(Name="slot")] public string Slot;
        [DataMember(Name="outcome")] public string Outcome="ok";
        [DataMember(Name="name")] public string Name;
        [DataMember(Name="enabled")] public bool Enabled;
        [DataMember(Name="runtimeId")] public int[] RuntimeId;
        [DataMember(Name="events")] public EventInfo[] Events;
    }
    private static readonly object gate=new object();
    private static readonly List<EventInfo> events=new List<EventInfo>();
    [MTAThread] private static int Main(string[] args)
    {
        try
        {
            if(args.Length!=2)throw new ArgumentException("Pass window and expected catalog process.");
            Console.OutputEncoding=new UTF8Encoding(false);
            var process=int.Parse(args[1]);var root=AutomationElement.FromHandle(new IntPtr(long.Parse(args[0])));
            if(root==null||root.Current.ProcessId!=process)throw new InvalidOperationException("UIA window/process boundary differs.");
            var scope=root.FindFirst(TreeScope.Descendants,new PropertyCondition(AutomationElement.AutomationIdProperty,"FilterSession-Scope"));
            if(scope==null||scope.Current.ProcessId!=process)throw new InvalidOperationException("Owned session scope is missing.");
            var retained=new Dictionary<string,Retained>();
            Write(new Response{Process=process,ClientProcess=System.Diagnostics.Process.GetCurrentProcess().Id,Command="ready"});
            string line;
            while((line=Console.ReadLine())!=null)
            {
                var parts=line.Split('\t');var response=new Response{Process=process,ClientProcess=System.Diagnostics.Process.GetCurrentProcess().Id,Command=parts[0],Slot=parts.Length>1?parts[1]:null};
                if(parts[0]=="quit"){Write(response);break;}
                if(parts[0]=="retain")
                {
                    if(parts.Length!=3||retained.ContainsKey(parts[1]))throw new InvalidOperationException("Invalid or repeated retained slot.");
                    var element=scope.FindFirst(TreeScope.Descendants,new AndCondition(Automation.ControlViewCondition,new PropertyCondition(AutomationElement.AutomationIdProperty,parts[2])));
                    if(element==null||element.Current.ProcessId!=process)throw new InvalidOperationException("Retained target is outside the owned scope.");
                    object pattern;if(!element.TryGetCurrentPattern(InvokePattern.Pattern,out pattern))throw new InvalidOperationException("Retained target lacks Invoke.");
                    var item=new Retained{Element=element,Invoke=(InvokePattern)pattern,RuntimeId=element.GetRuntimeId()};retained.Add(parts[1],item);
                    response.Name=element.Current.Name;response.Enabled=element.Current.IsEnabled;response.RuntimeId=item.RuntimeId;
                }
                else if(parts[0]=="watch")
                {
                    var slot=parts[1];var item=retained[slot];if(item.Handler!=null)throw new InvalidOperationException("Duplicate event listener.");
                    item.Handler=delegate(object sender,AutomationPropertyChangedEventArgs change)
                    {
                        lock(gate)events.Add(new EventInfo{Sequence=events.Count+1,Slot=slot,Property=change.Property==AutomationElement.NameProperty?"name":"enabled",Value=Convert.ToString(change.NewValue,CultureInfo.InvariantCulture)});
                    };
                    Automation.AddAutomationPropertyChangedEventHandler(item.Element,TreeScope.Element,item.Handler,AutomationElement.NameProperty,AutomationElement.IsEnabledProperty);
                }
                else if(parts[0]=="invoke")
                {
                    var item=retained[parts[1]];response.RuntimeId=item.RuntimeId;
                    try{item.Invoke.Invoke();response.Outcome="invoked";}
                    catch(ElementNotAvailableException){response.Outcome="element-not-available";}
                    catch(ElementNotEnabledException){response.Outcome="element-not-enabled";}
                }
                else if(parts[0]=="state")
                {
                    var item=retained[parts[1]];response.RuntimeId=item.RuntimeId;
                    try{response.Name=item.Element.Current.Name;response.Enabled=item.Element.Current.IsEnabled;}
                    catch(ElementNotAvailableException){response.Outcome="element-not-available";}
                }
                else if(parts[0]!="events")throw new InvalidOperationException("Unknown UIA session command.");
                Write(response);
            }
            return 0;
        }
        catch(Exception error){Console.Error.WriteLine(error);return 1;}
        finally{Automation.RemoveAllEventHandlers();}
    }
    private static void Write(Response response)
    {
        lock(gate)response.Events=events.ToArray();
        using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(Response)).WriteObject(stream,response);Console.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));}
    }
}
