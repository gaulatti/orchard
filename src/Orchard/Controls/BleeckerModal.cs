using System.Diagnostics;
using System.Windows.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace Orchard;

/// <summary>A caller-controlled modal with an original arbitrary content slot.</summary>
public sealed partial class BleeckerModal : ContentControl
{
    private readonly Stopwatch clock=Stopwatch.StartNew();
    private readonly UISettings settings=new();
    private BleeckerAlertDialogPresence? presence;
    private BleeckerAlertDialogMotion? motion;
    private BleeckerModalHost.Registration? registration;
    private DispatcherQueueTimer? timer;
    private ICommand? observedCommand;
    private XamlRoot? observedRoot;
    private Grid? declaration;
    private bool mountReady,rendering,renderAgain,queued,subscribed,lastVisible;
    private int generation;
    private double visibleDeadline,removeDeadline,motionDeadline;
    public BleeckerModal()
    {
        DefaultStyleKey=typeof(BleeckerModal);IsTabStop=false;UseLayoutRounding=false;Loaded+=LoadedOwner;Unloaded+=UnloadedOwner;ActualThemeChanged+=(_,_)=>Refresh();
        RegisterPropertyChangedCallback(TemplateProperty,(_,_)=>{if(Template is null)DetachTemplate();});
        foreach(var property in new[]{ContentProperty,ContentTemplateProperty,ContentTemplateSelectorProperty})RegisterPropertyChangedCallback(property,(_,_)=>QueueRefresh());
        foreach(var property in new[]{IsEnabledProperty,FlowDirectionProperty,LanguageProperty,IsTextScaleFactorEnabledProperty})RegisterPropertyChangedCallback(property,(_,_)=>Refresh());
    }
    private static DependencyProperty Property(string name,Type type,object? value)=>DependencyProperty.Register(name,type,typeof(BleeckerModal),new PropertyMetadata(value,(owner,_)=>((BleeckerModal)owner).Refresh()));
    public static readonly DependencyProperty IsOpenProperty=Property(nameof(IsOpen),typeof(bool),false);
    public static readonly DependencyProperty TitleProperty=Property(nameof(Title),typeof(string),null);
    public static readonly DependencyProperty SurfaceStyleProperty=Property(nameof(SurfaceStyle),typeof(Style),null);
    public static readonly DependencyProperty CloseCommandProperty=Property(nameof(CloseCommand),typeof(ICommand),null);
    public static readonly DependencyProperty CloseCommandParameterProperty=Property(nameof(CloseCommandParameter),typeof(object),null);
    public bool IsOpen{get=>(bool)GetValue(IsOpenProperty);set=>SetValue(IsOpenProperty,value);}
    public string? Title{get=>(string?)GetValue(TitleProperty);set=>SetValue(TitleProperty,value);}
    public Style? SurfaceStyle{get=>(Style?)GetValue(SurfaceStyleProperty);set=>SetValue(SurfaceStyleProperty,value);}
    public ICommand? CloseCommand{get=>(ICommand?)GetValue(CloseCommandProperty);set=>SetValue(CloseCommandProperty,value);}
    public object? CloseCommandParameter{get=>GetValue(CloseCommandParameterProperty);set=>SetValue(CloseCommandParameterProperty,value);}
    public event RoutedEventHandler? CloseRequested;
    public void RequestClose()
    {
        if(!IsLoaded||!IsOpen||!IsEnabled||registration?.Layer.Active!=true||!ReferenceEquals(registration.Host.ActiveSurface,focusSurface)||CloseCommand?.CanExecute(CloseCommandParameter)==false)return;
        var command=CloseCommand;var parameter=CloseCommandParameter;CloseRequested?.Invoke(this,new RoutedEventArgs());command?.Execute(parameter);
    }
    protected override void OnApplyTemplate(){DetachTemplate();base.OnApplyTemplate();if(Template is null)return;declaration=GetTemplateChild("PART_Layout") as Grid??throw new InvalidOperationException("Modal requires a Grid named PART_Layout.");QueueRefresh();}
    private void DetachTemplate(){generation++;mountReady=false;StopPresentation();declaration=null;}
    private void LoadedOwner(object sender,RoutedEventArgs args)
    {
        if(!subscribed&&OperatingSystem.IsWindowsVersionAtLeast(10,0,19041)){settings.AnimationsEnabledChanged+=AnimationsChanged;subscribed=true;}
        LayoutUpdated+=LayoutOwner;QueueRefresh();
    }
    private void UnloadedOwner(object sender,RoutedEventArgs args)
    {
        generation++;mountReady=false;LayoutUpdated-=LayoutOwner;StopPresentation();DisconnectRoot();UnhookCommand();
        if(subscribed&&OperatingSystem.IsWindowsVersionAtLeast(10,0,19041))settings.AnimationsEnabledChanged-=AnimationsChanged;subscribed=false;
    }
    private void LayoutOwner(object? sender,object args)
    {
        if(!IsLoaded)return;if(!ReferenceEquals(observedRoot,XamlRoot))QueueRefresh();ObserveContentRoot();
        // A stretched automatic root can keep its old ActualHeight after its
        // descendants shrink. Its measured desired height is authoritative.
        if(overlay is not null&&contentSlot is{IsLoaded:true} slot&&slot.DesiredSize.Height<layout.Content.Height)QueueRemeasure();
    }
    private void QueueRefresh()
    {
        if(queued)return;queued=true;var current=generation;
        if(!DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low,()=>{queued=false;if(current!=generation){if(IsLoaded)QueueRefresh();return;}mountReady=true;Refresh();}))throw new InvalidOperationException("Modal could not queue its presentation.");
    }
    private void DisconnectRoot(){if(observedRoot is not null)observedRoot.Changed-=RootChanged;observedRoot=null;}
    private void RootChanged(XamlRoot sender,XamlRootChangedEventArgs args)=>overlay?.InvalidateMeasure();
    private void AnimationsChanged(UISettings sender,UISettingsAnimationsEnabledChangedEventArgs args)=>DispatcherQueue.TryEnqueue(()=>{if(motion is null)return;var now=clock.Elapsed.TotalMilliseconds;motion.SetVisible(lastVisible,now,reduced:!settings.AnimationsEnabled);motionDeadline=now+Math.Max(motion.Durations.Surface,motion.Durations.Backdrop);UpdatePresentation();});
    private void UnhookCommand(){if(observedCommand is not null)observedCommand.CanExecuteChanged-=CanExecuteChanged;observedCommand=null;}
    private void CanExecuteChanged(object? sender,EventArgs args){if(!DispatcherQueue.TryEnqueue(Refresh))throw new InvalidOperationException("Modal could not update command state.");}
    private void Refresh()
    {
        var command=IsLoaded?CloseCommand:null;if(!ReferenceEquals(command,observedCommand)){UnhookCommand();observedCommand=command;if(command is not null)command.CanExecuteChanged+=CanExecuteChanged;}
        if(!mountReady||!IsLoaded||declaration is null)return;if(rendering){renderAgain=true;return;}rendering=true;
        try
        {
            if(!ReferenceEquals(observedRoot,XamlRoot)){StopPresentation();DisconnectRoot();observedRoot=XamlRoot;if(observedRoot is not null)observedRoot.Changed+=RootChanged;}
            if(IsOpen&&Title is null)throw new InvalidOperationException("Modal requires Title before opening; an empty title is allowed.");
            if(IsOpen&&presence is null){presence=new();motion=new();lastVisible=false;}
            if(presence is not null)
            {
                var now=clock.Elapsed.TotalMilliseconds;if(presence.DesiredOpen!=IsOpen){presence.SetOpen(IsOpen,now);if(IsOpen)visibleDeadline=now+20;else removeDeadline=now+150;if(IsOpen)MountPresentation();else if(registration is{IsAttached:true,Layer.Active:true})registration.Host.Deactivate(registration);}
                RenderContent();UpdatePresentation();
            }
        }finally{rendering=false;}
        if(renderAgain){renderAgain=false;Refresh();}
    }
    private void UpdatePresentation()
    {
        if(presence is null||motion is null)return;var now=clock.Elapsed.TotalMilliseconds;presence.Advance(now);
        if(presence.Visible!=lastVisible){lastVisible=presence.Visible;motion.SetVisible(lastVisible,now,reduced:!settings.AnimationsEnabled);motionDeadline=now+Math.Max(motion.Durations.Surface,motion.Durations.Backdrop);}
        if(!presence.Present){StopPresentation();return;}ApplyMotion(motion.SampleAt(now));if(presence.DesiredOpen&&presence.Visible&&now>=motionDeadline){timer?.Stop();return;}
        var remaining=presence.DesiredOpen&&!presence.Visible?visibleDeadline-now:!presence.DesiredOpen?removeDeadline-now:motionDeadline-now;
        timer??=DispatcherQueue.CreateTimer();timer.IsRepeating=false;timer.Tick-=Tick;timer.Tick+=Tick;timer.Interval=TimeSpan.FromMilliseconds(Math.Max(1,Math.Min(16,remaining)));timer.Start();
    }
    private void Tick(DispatcherQueueTimer sender,object args)=>UpdatePresentation();
    private void StopPresentation(){timer?.Stop();if(timer is not null)timer.Tick-=Tick;timer=null;presence?.Dispose();presence=null;motion=null;registration?.Dispose();registration=null;ClearPresentation();}
    protected override AutomationPeer OnCreateAutomationPeer()=>new DeclarationPeer(this);
    private sealed class DeclarationPeer(BleeckerModal owner):FrameworkElementAutomationPeer(owner)
    {
        protected override bool IsControlElementCore()=>false;protected override bool IsContentElementCore()=>false;protected override IList<AutomationPeer> GetChildrenCore()=>[];
    }
}
